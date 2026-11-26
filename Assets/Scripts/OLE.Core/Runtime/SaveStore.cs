using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace OLE.Core
{
    // The caller uses these outcomes to select the appropriate UI message.
    public enum SaveResult
    {
        Success,
        NotFound,
        InvalidData,
        UnsupportedVersion,
        IoError,
        IntegrityFailed,
        KeyUnavailable
    }

    public sealed class SaveStore
    {
        internal const int MaxSaveBytes = 16 * 1024;
        private readonly SaveMac mac;
        private string FormatVersion => mac == null ? "1" : "2";
        private readonly string path;

        public SaveStore(string path) : this(path, false) { }

        public static SaveStore ForChallenge(string path) => new SaveStore(path, true);

        private SaveStore(string path, bool challenge)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Provide a save file path.", nameof(path));
            this.path = Path.GetFullPath(path);
            if (challenge) mac = new SaveMac(this.path + ".key");
        }

        public SaveResult Save(GameSession game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            try
            {
                // Platform changes only on arrival, also when saving during a move.
                var data = new XElement("ole-save",
                    new XAttribute("version", FormatVersion),
                    new XAttribute("level", game.Level.Id),
                    new XAttribute("platform", game.Platform),
                    new XAttribute("mode", game.Mode.ToString()));
                if (mac != null)
                {
                    byte[] key = mac.GetKey(allowCreation: !File.Exists(path));
                    if (key == null) return SaveResult.KeyUnavailable;
                    data.Add(new XAttribute("mac", Convert.ToBase64String(
                        SaveMac.Compute(key, game.Level.Id, game.Platform, game.Mode.ToString()))));
                }
                WriteAtomically(stream =>
                {
                    data.Save(stream, SaveOptions.DisableFormatting);
                    if (stream.Length > MaxSaveBytes) throw new InvalidDataException("Save exceeds the size limit.");
                });
                return SaveResult.Success;
            }
            catch (InvalidDataException) { return SaveResult.InvalidData; }
            catch (ArgumentException) { return SaveResult.InvalidData; }
            catch (IOException) { return SaveResult.IoError; }
            catch (UnauthorizedAccessException) { return SaveResult.IoError; }
            catch (NotSupportedException) { return SaveResult.IoError; }
        }

        public SaveResult LoadInto(GameSession game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            try
            {
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    MaxCharactersInDocument = MaxSaveBytes
                };
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
                {
                    if (stream.Length > MaxSaveBytes) return SaveResult.InvalidData;
                    using (var reader = XmlReader.Create(stream, settings))
                    {
                        if (reader.MoveToContent() != XmlNodeType.Element || reader.Name != "ole-save" ||
                            reader.NamespaceURI.Length != 0) return SaveResult.InvalidData;
                        string version = reader.GetAttribute("version");
                        if (string.IsNullOrWhiteSpace(version)) return SaveResult.InvalidData;
                        if (version != FormatVersion) return SaveResult.UnsupportedVersion;

                        string level = reader.GetAttribute("level");
                        string platform = reader.GetAttribute("platform");
                        string mode = reader.GetAttribute("mode");
                        string tag = reader.GetAttribute("mac");
                        if (mac != null && tag == null) return SaveResult.IntegrityFailed;
                        if (reader.AttributeCount != (mac == null ? 4 : 5) || level == null || platform == null ||
                            (mode != "A" && mode != "B")) return SaveResult.InvalidData;
                        bool empty = reader.IsEmptyElement;
                        reader.ReadStartElement();
                        if (!empty) reader.ReadEndElement(); // no nested elements or text
                        if (reader.MoveToContent() != XmlNodeType.None) return SaveResult.InvalidData;

                        if (mac != null)
                        {
                            byte[] key = mac.GetKey(allowCreation: false);
                            if (key == null) return SaveResult.KeyUnavailable;
                            if (!SaveMac.Matches(tag, key, level, platform, mode)) return SaveResult.IntegrityFailed;
                        }
                        return game.TryRestore(level, platform, mode == "A" ? BoardMode.A : BoardMode.B)
                            ? SaveResult.Success : SaveResult.InvalidData;
                    }
                }
            }
            catch (FileNotFoundException) { return SaveResult.NotFound; }
            catch (DirectoryNotFoundException) { return SaveResult.NotFound; }
            catch (XmlException) { return SaveResult.InvalidData; }
            catch (IOException) { return SaveResult.IoError; }
            catch (UnauthorizedAccessException) { return SaveResult.IoError; }
            catch (NotSupportedException) { return SaveResult.IoError; }
        }

        // Delete only this slot and its optional verification key, never the save directory.
        public SaveResult Delete()
        {
            try
            {
                File.Delete(path);
                if (mac != null) File.Delete(path + ".key");
                return SaveResult.Success;
            }
            catch (FileNotFoundException) { return SaveResult.Success; }
            catch (DirectoryNotFoundException) { return SaveResult.Success; }
            catch (IOException) { return SaveResult.IoError; }
            catch (UnauthorizedAccessException) { return SaveResult.IoError; }
            catch (NotSupportedException) { return SaveResult.IoError; }
        }

        internal void WriteAtomically(Action<Stream> write)
        {
            string directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);
            string temp = Path.Combine(directory, Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    write(stream);
                    stream.Flush(true);
                }

                // Same directory keeps the rename on one filesystem. The live file is never truncated.
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
            }
            finally
            {
                // Cleanup of a leftover temporary file must not mask the commit outcome.
                try { File.Delete(temp); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
