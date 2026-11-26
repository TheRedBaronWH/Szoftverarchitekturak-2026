using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using OLE.Examples;

namespace OLE.Core.Tests
{
    internal static class SaveTests
    {
        public static void Run()
        {
            Case("Challenge save round trip and altered fields", ChallengeIntegrity);
            Case("Challenge slot rejects an unsigned legacy save and a foreign key", ChallengeIsolation);
            Case("Missing or damaged key preserves the saved file and the live session", ChallengeKeyLoss);
            Case("MAC matches an independent reference vector", ChallengeVector);
            Case("Challenge save preserves Unicode IDs after reloading", ChallengeUnicode);
            Case("Reject extra fields, nested data and a document type without changing the active move", RejectUnexpectedContent);
            Case("Limit the save size on reading and writing; keep the last valid save", SizeLimit);
            Case("A known platform beyond the terminal goal cannot be restored", UnreachableCheckpoint);
            Case("NFR-13  missing save leaves the current game usable", MissingFile);
            Case("NFR-12  a fresh session recovers the latest successful save", LatestSave);
            Case("NFR-12  an in-flight save resumes at the last reached platform", SaveDuringMotion);
            Case("NFR-12  a successful load cancels an old animation", RestoreCancelsMove);
            Case("NFR-13  malformed data and unknown IDs have no side effects", InvalidFiles);
            Case("NFR-13  future version is reported separately", UnknownVersion);
            Case("NFR-12  completion and campaign progress survive a restart", SavedVictory);
            Case("NFR-12  an I/O exception during staging preserves the old file", FailedWrite);
            Case("NFR-12  abrupt process exit during a replacement preserves the last save", ProcessExitDuringSave);
            Case("NFR-13  failed write reports an error", PublicWriteError);
            Case("NFR-12  Unicode and XML-sensitive IDs round-trip", EscapedIds);
        }

        private static void Case(string name, Action<string> test)
        {
            Check.Case(name, () =>
            {
                string directory = Path.Combine(Path.GetTempPath(), "ole-tests-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(directory);
                try { test(Path.Combine(directory, "progress.xml")); }
                finally { Directory.Delete(directory, recursive: true); }
            });
        }

        private static void ChallengeIntegrity(string path)
        {
            var store = SaveStore.ForChallenge(path);
            var game = ExampleCampaign.NewGame();
            Check.Travel(game, "P2");
            Check.Equal(SaveResult.Success, store.Save(game));
            string saved = File.ReadAllText(path);
            game.StartNewGame();
            Check.Equal(SaveResult.Success, SaveStore.ForChallenge(path).LoadInto(game));
            Check.Equal("P2", game.Platform);
            Move pending = Check.Start(game, "P3");
            foreach (var edit in new[] { ("platform", "P4"), ("level", "bridge"), ("mode", "B"), ("mac", "bad") })
            {
                var xml = System.Xml.Linq.XElement.Parse(saved);
                xml.SetAttributeValue(edit.Item1, edit.Item2);
                xml.Save(path);
                Check.Equal(SaveResult.IntegrityFailed, store.LoadInto(game));
                Check.Equal("P2", game.Platform);
                Check.True(game.IsMoving);
            }
            Check.True(game.CompleteMove(pending));
        }

        private static void ChallengeIsolation(string path)
        {
            var game = ExampleCampaign.NewGame();
            var legacy = new SaveStore(path);
            var challenge = SaveStore.ForChallenge(path);
            Check.Equal(SaveResult.Success, legacy.Save(game));
            Check.Equal(SaveResult.UnsupportedVersion, challenge.LoadInto(game));
            string other = path + ".other";
            var otherStore = SaveStore.ForChallenge(other);
            Check.Equal(SaveResult.Success, otherStore.Save(game));
            File.Delete(path);
            Check.Equal(SaveResult.Success, challenge.Save(game));
            Check.Equal(SaveResult.UnsupportedVersion, legacy.LoadInto(game));
            File.Copy(other, path, overwrite: true);
            Check.Equal(SaveResult.IntegrityFailed, challenge.LoadInto(game));
            var xml = System.Xml.Linq.XElement.Load(path);
            xml.Attribute("mac").Remove();
            xml.Save(path);
            Check.Equal(SaveResult.IntegrityFailed, challenge.LoadInto(game));
        }

        private static void ChallengeKeyLoss(string path)
        {
            var game = ExampleCampaign.NewGame();
            var store = SaveStore.ForChallenge(path);
            Check.Equal(SaveResult.Success, store.Save(game));
            string original = File.ReadAllText(path);
            byte[] key = File.ReadAllBytes(path + ".key");
            File.Delete(path + ".key");
            Check.Travel(game, "P2");
            Check.Equal(SaveResult.KeyUnavailable, store.LoadInto(game));
            Check.Equal(SaveResult.KeyUnavailable, store.Save(game));
            Check.False(File.Exists(path + ".key"));
            File.WriteAllBytes(path + ".key", new byte[4]);
            Check.Equal(SaveResult.KeyUnavailable, store.Save(game));
            Check.Equal(original, File.ReadAllText(path));
            Check.Equal("P2", game.Platform);
            File.WriteAllBytes(path + ".key", key);
            Check.Equal(SaveResult.Success, store.LoadInto(game));
            Check.Equal("P1", game.Platform);
        }

        private static void ChallengeVector(string unusedPath)
        {
            var key = new byte[32];
            for (int i = 0; i < key.Length; i++) key[i] = (byte)i;
            Check.Equal("J0nqHDdInJJv0w/dVHxzYp2dTwtj99jMRtGpjSx9NHY=", Convert.ToBase64String(SaveMac.Compute(key, "tutorial", "P2", "A")));
        }

        private static void ChallengeUnicode(string path)
        {
            var level = new LevelDefinition("Első & cél", "Árvíz", "G", new[] { "Árvíz", "G" },
                new[] { new Connection("Árvíz", "G", true, true) });
            var game = new GameSession(new[] { level });
            var store = SaveStore.ForChallenge(path);
            Check.Equal(SaveResult.Success, store.Save(game));
            Check.Travel(game, "G");
            Check.Equal(SaveResult.Success, SaveStore.ForChallenge(path).LoadInto(game));
            Check.Equal("Árvíz", game.Platform);
        }

        private static void RejectUnexpectedContent(string path)
        {
            const string attrs = "version='1' level='tutorial' platform='P2' mode='A'";
            var game = ExampleCampaign.NewGame();
            var store = new SaveStore(path);
            Move original = Check.Start(game, "P2");
            string[] samples = {
                "<ole-save " + attrs + " extra='unused'/>",
                "<ole-save " + attrs + "><extra/></ole-save>",
                "<ole-save " + attrs + ">unexpected</ole-save>",
                "<!DOCTYPE ole-save><ole-save " + attrs + "/>"
            };
            foreach (string sample in samples)
            {
                File.WriteAllText(path, sample);
                Check.Equal(SaveResult.InvalidData, store.LoadInto(game));
                Check.True(game.IsMoving);
                Check.Equal("P1", game.Platform);
            }
            Check.True(game.CompleteMove(original));
        }

        private static void SizeLimit(string path)
        {
            var game = ExampleCampaign.NewGame();
            var store = new SaveStore(path);
            string prefix = "<ole-save version='1' level='tutorial' platform='P2' mode='A'/>";
            File.WriteAllText(path, prefix.PadRight(SaveStore.MaxSaveBytes), new UTF8Encoding(false));
            Check.Equal(SaveResult.Success, store.LoadInto(game));
            File.AppendAllText(path, " ");
            Check.Equal(SaveResult.InvalidData, store.LoadInto(game));
            Check.Equal("P2", game.Platform);

            Check.Equal(SaveResult.Success, store.Save(game));
            var largeLevel = new LevelDefinition(new string('L', SaveStore.MaxSaveBytes), "S", "G",
                new[] { "S", "G" }, new[] { new Connection("S", "G", true, true) });
            Check.Equal(SaveResult.InvalidData, store.Save(new GameSession(new[] { largeLevel })));
            game.RestartLevel();
            Check.Equal(SaveResult.Success, store.LoadInto(game));
            Check.Equal("P2", game.Platform);
        }

        private static void UnreachableCheckpoint(string path)
        {
            var level = new LevelDefinition("demo", "S", "G", new[] { "S", "G", "X" },
                new[] { new Connection("S", "G", true, true), new Connection("G", "X", true, true) });
            var game = new GameSession(new[] { level });
            File.WriteAllText(path, "<ole-save version='1' level='demo' platform='X' mode='A'/>");
            Check.Equal(SaveResult.InvalidData, new SaveStore(path).LoadInto(game));
            Check.Equal("S", game.Platform);
        }

        private static void MissingFile(string path)
        {
            GameSession game = ExampleCampaign.NewGame();
            Check.Travel(game, "P2");
            Check.Equal(SaveResult.NotFound, new SaveStore(path).LoadInto(game));
            Check.Equal(SaveResult.NotFound, new SaveStore(Path.Combine(path, "missing.xml")).LoadInto(game));
            Check.Equal("P2", game.Platform);
            game.StartNewGame();
            Check.Equal("P1", game.Platform);
        }

        private static void LatestSave(string path)
        {
            var store = new SaveStore(path);
            GameSession game = ExampleCampaign.NewGame();
            Check.Equal(SaveResult.Success, store.Save(game));
            Check.Travel(game, "P2");
            Check.True(game.TryToggleMode());
            Check.Equal(SaveResult.Success, store.Save(game));

            GameSession fresh = ExampleCampaign.NewGame();
            Check.Equal(SaveResult.Success, new SaveStore(path).LoadInto(fresh));
            Check.Equal("tutorial", fresh.Level.Id);
            Check.Equal("P2", fresh.Platform);
            Check.Equal(BoardMode.B, fresh.Mode);
            Check.False(fresh.IsMoving);
        }

        private static void SaveDuringMotion(string path)
        {
            GameSession game = ExampleCampaign.NewGame();
            Check.Travel(game, "P2");
            Check.Travel(game, "P3");
            Check.True(game.TryToggleMode());
            Move move = Check.Start(game, "P4");
            var store = new SaveStore(path);
            Check.Equal(SaveResult.Success, store.Save(game));
            Check.True(game.IsMoving);
            Check.True(game.CompleteMove(move));
            GameSession resumed = ExampleCampaign.NewGame();
            Check.Equal(SaveResult.Success, store.LoadInto(resumed));
            Check.Equal("P3", resumed.Platform);
            Check.Equal(BoardMode.B, resumed.Mode);
            Check.False(resumed.IsLevelComplete);
            Check.False(resumed.IsMoving);
            Check.Travel(resumed, "P4");
            Check.True(resumed.IsLevelComplete);
        }

        private static void RestoreCancelsMove(string path)
        {
            GameSession game = ExampleCampaign.NewGame();
            var store = new SaveStore(path);
            Check.Equal(SaveResult.Success, store.Save(game));
            Move beforeLoad = Check.Start(game, "P2");
            Check.Equal(SaveResult.Success, store.LoadInto(game));
            Move afterLoad = Check.Start(game, "P2");
            Check.False(game.CompleteMove(beforeLoad));
            Check.True(game.CompleteMove(afterLoad));
        }

        private static void InvalidFiles(string path)
        {
            string valid = "<ole-save version='1' level='tutorial' platform='P2' mode='A'/>";
            string[] bad =
            {
                "", "<ole-save", valid + "broken", "<other/>", valid.Replace("version='1'", ""),
                valid.Replace("level='tutorial'", ""), valid.Replace("tutorial", "no-such-level"),
                valid.Replace("platform='P2'", ""), valid.Replace("P2", "no-such-platform"),
                valid.Replace("mode='A'", ""), valid.Replace("mode='A'", "mode='C'"),
                valid.Replace("mode='A'", "mode='0'")
            };
            GameSession game = ExampleCampaign.NewGame();
            var store = new SaveStore(path);
            Move requested = Check.Start(game, "P2");
            foreach (string content in bad)
            {
                File.WriteAllText(path, content);
                Check.Equal(SaveResult.InvalidData, store.LoadInto(game));
                Check.Equal("P1", game.Platform);
                Check.Equal(BoardMode.A, game.Mode);
                Check.True(game.IsMoving);
            }
            Check.True(game.CompleteMove(requested));
            game.StartNewGame();
            Check.Equal("P1", game.Platform);
        }

        private static void UnknownVersion(string path)
        {
            File.WriteAllText(path, "<ole-save version='2' level='tutorial' platform='P2' mode='B'/>");
            GameSession game = ExampleCampaign.NewGame();
            Check.Equal(SaveResult.UnsupportedVersion, new SaveStore(path).LoadInto(game));
            Check.Equal("P1", game.Platform);
            Check.Equal(BoardMode.A, game.Mode);
        }

        private static void SavedVictory(string path)
        {
            var store = new SaveStore(path);
            GameSession game = ExampleCampaign.NewGame();
            Check.SolveTutorial(game);
            Check.Equal(SaveResult.Success, store.Save(game));
            GameSession resumed = ExampleCampaign.NewGame();
            Check.Equal(SaveResult.Success, store.LoadInto(resumed));
            Check.True(resumed.IsLevelComplete);
            Check.True(resumed.TryNextLevel());
            Check.True(resumed.TryToggleMode());
            Check.Travel(resumed, "Q2");
            Check.Equal(SaveResult.Success, store.Save(resumed));
            game.StartNewGame();
            Check.Equal(SaveResult.Success, store.LoadInto(game));
            Check.Equal("bridge", game.Level.Id);
            Check.True(game.IsGameComplete);
            Check.False(game.TryNextLevel());
        }

        private static void FailedWrite(string path)
        {
            var store = new SaveStore(path);
            GameSession game = ExampleCampaign.NewGame();
            Check.Travel(game, "P2");
            Check.Equal(SaveResult.Success, store.Save(game));
            byte[] previous = File.ReadAllBytes(path);
            Check.Throws<IOException>(() => store.WriteAtomically(stream =>
            {
                stream.WriteByte(42);
                throw new IOException("Simulated storage failure after a partial write.");
            }));
            Check.Equal(Convert.ToBase64String(previous), Convert.ToBase64String(File.ReadAllBytes(path)));
            game.RestartLevel();
            Check.Equal(SaveResult.Success, store.LoadInto(game));
            Check.Equal("P2", game.Platform);
        }

        internal static int InterruptWriter(string path)
        {
            new SaveStore(path).WriteAtomically(stream =>
            {
                byte[] partial = Encoding.UTF8.GetBytes("<ole-save ");
                stream.Write(partial, 0, partial.Length);
                stream.Flush();
                Environment.Exit(71); // quit before rename, skipping finally blocks
            });
            return 99;
        }

        private static void ProcessExitDuringSave(string path)
        {
            GameSession game = ExampleCampaign.NewGame();
            var store = new SaveStore(path);
            Check.Travel(game, "P2");
            Check.True(game.TryToggleMode());
            Check.Equal(SaveResult.Success, store.Save(game));
            string executable = Environment.ProcessPath;
            var start = new ProcessStartInfo(executable) { UseShellExecute = false };
            if (string.Equals(Path.GetFileNameWithoutExtension(executable), "dotnet", StringComparison.OrdinalIgnoreCase))
                start.ArgumentList.Add(typeof(Program).Assembly.Location);
            start.ArgumentList.Add("--interrupt-save");
            start.ArgumentList.Add(path);
            using (Process child = Process.Start(start))
            {
                if (!child.WaitForExit(15000))
                {
                    child.Kill();
                    throw new Exception("The interrupted writer did not exit.");
                }
                Check.Equal(71, child.ExitCode);
            }
            Check.True(Directory.GetFiles(Path.GetDirectoryName(path), "*.tmp").Length > 0);
            GameSession fresh = ExampleCampaign.NewGame();
            Check.Equal(SaveResult.Success, new SaveStore(path).LoadInto(fresh));
            Check.Equal("P2", fresh.Platform);
            Check.Equal(BoardMode.B, fresh.Mode);
            Check.Equal(SaveResult.Success, store.Save(fresh));
        }

        private static void PublicWriteError(string path)
        {
            Directory.CreateDirectory(path); // a directory occupies the target file name
            GameSession game = ExampleCampaign.NewGame();
            Check.Equal(SaveResult.IoError, new SaveStore(path).Save(game));
            Check.Equal("P1", game.Platform);
        }

        private static void EscapedIds(string path)
        {
            string id = "P<&> Árvíz";
            var level = new LevelDefinition("Első & cél", id, "G", new[] { id, "G" },
                new[] { new Connection(id, "G", true, true) });
            var game = new GameSession(new[] { level });
            var store = new SaveStore(path);
            Check.Equal(SaveResult.Success, store.Save(game));
            Check.Travel(game, "G");
            Check.Equal(SaveResult.Success, store.LoadInto(game));
            Check.Equal(id, game.Platform);
        }
    }
}
