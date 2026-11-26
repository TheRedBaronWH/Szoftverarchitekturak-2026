using System;

namespace OLE.Core
{
    // Owns the running session across scene changes and restores a save after an app restart.
    public sealed class GameProgress
    {
        private readonly Func<GameSession> createSession;
        private readonly SaveStore saves;
        private readonly SaveStore autosaves;

        public GameSession Session { get; private set; }
        public SaveResult? LastAutosaveResult { get; private set; }
        public SaveResult? LastCleanupResult { get; private set; }
        public bool UsedManualSaveFallback { get; private set; }

        public GameProgress(Func<GameSession> createSession, SaveStore saves, SaveStore autosaves = null)
        {
            this.createSession = createSession ?? throw new ArgumentNullException(nameof(createSession));
            this.saves = saves ?? throw new ArgumentNullException(nameof(saves));
            this.autosaves = autosaves;
        }

        public void StartNewGame()
        {
            if (Session != null && Session.IsGameComplete) ClearCompletedSaves();
            Adopt(createSession());
            LastCleanupResult = null;
            UsedManualSaveFallback = false;
            Autosave();
        }

        public SaveResult CheckContinuation()
        {
            if (Session != null)
            {
                if (!Session.IsGameComplete) return SaveResult.Success;
                ClearCompletedSaves();
                return SaveResult.NotFound;
            }

            return ReadContinuation(out _, out _);
        }

        public SaveResult Continue()
        {
            UsedManualSaveFallback = false;
            if (Session != null && Session.IsGameComplete)
            {
                ClearCompletedSaves();
                return SaveResult.NotFound;
            }
            if (Session == null)
            {
                SaveResult result = ReadContinuation(out GameSession loaded, out bool fallback);
                if (result != SaveResult.Success)
                    return result;
                Adopt(loaded);
                UsedManualSaveFallback = fallback;
            }

            Session.Resume();
            if (Session.IsLevelComplete && Session.HasNextLevel)
                Session.TryNextLevel();
            Autosave();
            return SaveResult.Success;
        }

        // Disk continuation prefers the automatic checkpoint; the manual slot is a fallback.
        private SaveResult ReadContinuation(out GameSession loaded, out bool fallback)
        {
            loaded = createSession();
            fallback = false;
            SaveResult automatic = autosaves == null ? SaveResult.NotFound : autosaves.LoadInto(loaded);
            if (automatic == SaveResult.Success)
                return ValidateContinuation(loaded);

            SaveResult manual = saves.LoadInto(loaded);
            if (manual == SaveResult.Success)
            {
                fallback = automatic != SaveResult.NotFound;
                return ValidateContinuation(loaded);
            }
            return automatic != SaveResult.NotFound ? automatic : manual;
        }

        public SaveResult CheckManualSave() => Session != null && Session.IsGameComplete
            ? SaveResult.NotFound : saves.LoadInto(createSession());

        private SaveResult ValidateContinuation(GameSession loaded)
        {
            if (!loaded.IsGameComplete) return SaveResult.Success;
            // A completed save from an older build must not resurrect the last level.
            ClearCompletedSaves();
            return SaveResult.NotFound;
        }

        private void ClearCompletedSaves()
        {
            SaveResult manual = saves.Delete();
            SaveResult automatic = autosaves == null ? SaveResult.Success : autosaves.Delete();
            LastCleanupResult = manual != SaveResult.Success ? manual : automatic;
            LastAutosaveResult = null;
            UsedManualSaveFallback = false;
        }

        public void EnterMainMenu()
        {
            if (Session == null)
                return;

            Session.CancelMove();
            Session.Resume();
            Autosave();
            if (Session.IsGameComplete) Session = null;
        }

        public bool CompleteMove(Move move)
        {
            if (Session == null || !Session.CompleteMove(move))
                return false;
            Autosave();
            return true;
        }

        public bool TryToggleMode()
        {
            if (Session == null || !Session.TryToggleMode())
                return false;
            Autosave();
            return true;
        }

        public bool TryNextLevel()
        {
            if (Session == null || !Session.TryNextLevel())
                return false;
            Autosave();
            return true;
        }

        public void RestartLevel()
        {
            if (Session == null)
                return;
            if (Session.IsGameComplete) { StartNewGame(); return; }
            Session.RestartLevel();
            Autosave();
        }

        // Unity also calls this before quitting. An unfinished move saves its source platform.
        public void Autosave()
        {
            if (Session != null && Session.IsGameComplete)
            {
                ClearCompletedSaves();
                return;
            }
            if (Session != null && autosaves != null)
            {
                LastCleanupResult = null;
                LastAutosaveResult = autosaves.Save(Session);
            }
        }

        public SaveResult Save()
        {
            if (Session != null && Session.IsGameComplete)
            {
                ClearCompletedSaves();
                return SaveResult.NotFound;
            }
            return Session == null ? SaveResult.NotFound : saves.Save(Session);
        }

        public SaveResult Load(bool keepPaused = false)
        {
            if (Session != null && Session.IsGameComplete) return SaveResult.NotFound;
            var loaded = createSession();
            SaveResult result = saves.LoadInto(loaded);
            if (result == SaveResult.Success)
            {
                if (loaded.IsGameComplete)
                {
                    ClearCompletedSaves();
                    return SaveResult.NotFound;
                }
                Adopt(loaded);
                if (keepPaused)
                    Session.Pause();
                UsedManualSaveFallback = false;
                Autosave();
            }
            return result;
        }

        private void Adopt(GameSession next)
        {
            if (next == null)
                throw new InvalidOperationException("The session factory must return a game.");

            Session?.CancelMove();
            Session = next;
        }
    }
}
