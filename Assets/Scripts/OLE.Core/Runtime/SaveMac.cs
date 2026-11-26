using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace OLE.Core
{
    // A local integrity check for the optional challenge slot.
    internal sealed class SaveMac
    {
        private const int KeySize = 32;
        private readonly string keyPath;

        internal SaveMac(string keyPath) { this.keyPath = keyPath; }

        internal byte[] GetKey(bool allowCreation)
        {
            if (allowCreation && !File.Exists(keyPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(keyPath));
                string temp = keyPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                var key = new byte[KeySize];
                using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(key);
                try
                {
                    using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        file.Write(key, 0, key.Length);
                        file.Flush(true);
                    }
                    try { File.Move(temp, keyPath); }
                    catch (IOException) when (File.Exists(keyPath)) { } // another writer published its key first
                }
                finally
                {
                    try { File.Delete(temp); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
            try
            {
                using (var file = File.OpenRead(keyPath))
                using (var reader = new BinaryReader(file))
                {
                    if (file.Length != KeySize) return null;
                    byte[] key = reader.ReadBytes(KeySize);
                    return key.Length == KeySize ? key : null;
                }
            }
            catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }
        }

        internal static byte[] Compute(byte[] key, string level, string platform, string mode)
        {
            // BinaryWriter prefixes each UTF-8 string with its byte length, so field boundaries are unambiguous.
            using (var bytes = new MemoryStream())
            using (var writer = new BinaryWriter(bytes, Encoding.UTF8, true))
            using (var hmac = new HMACSHA256(key))
            {
                writer.Write("OLE/challenge-save/2");
                writer.Write(level);
                writer.Write(platform);
                writer.Write(mode);
                writer.Flush();
                return hmac.ComputeHash(bytes.ToArray());
            }
        }

        internal static bool Matches(string tag, byte[] key, string level, string platform, string mode)
        {
            if (tag == null || tag.Length != 44) return false;
            try
            {
                return CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(tag),
                    Compute(key, level, platform, mode));
            }
            catch (FormatException) { return false; }
        }
    }
}
