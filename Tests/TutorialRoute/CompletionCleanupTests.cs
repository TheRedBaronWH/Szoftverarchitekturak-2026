using System;
using System.IO;
using OLE.Core;

internal static class CompletionCleanupTests
{
    private static int checks;
    private static string folder;
    private static string Manual => Path.Combine(folder,"progress.xml");
    private static string Automatic => Path.Combine(folder,"autosave.xml");
    private static GameProgress Progress() => new GameProgress(TutorialCampaign.CreateSession,new SaveStore(Manual),new SaveStore(Automatic));

    public static void Run()
    {
        folder=Path.Combine(Path.GetTempPath(),"ole-completion-cleanup-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder,"settings.xml"),"unrelated data");
            var game=Progress(); game.StartNewGame(); Walk(game,"P2");
            Check(game.Save()==SaveResult.Success,"Create a manual checkpoint");
            Check(game.Session.TryStartMove("P3",out Move interrupted),"Begin an unfinished move");
            game.Session.Pause(); game.Autosave();
            var resumed=Progress();
            Check(resumed.Continue()==SaveResult.Success && resumed.Session.Platform=="P2"
                && resumed.Session.LevelNumber==1,"Quitting during pause/motion preserves the last reached platform");
            Check(File.Exists(Manual)&&File.Exists(Automatic),"An unfinished exit keeps both slots");
            game.Session.Resume(); game.Session.CancelMove();
            Finish(game,checkIntermediate:true);
            Check(game.Session.IsGameComplete,"Keep the final live session for credits");
            Check(game.LastCleanupResult==SaveResult.Success && game.LastAutosaveResult==null,"Final arrival reports cleanup, not a save");
            EmptySlots();
            Check(File.ReadAllText(Path.Combine(folder,"settings.xml"))=="unrelated data","Cleanup does not remove settings or other files");
            game.Autosave(); // starting the finale
            Check(game.Save()==SaveResult.NotFound,"F5 cannot recreate a completed manual save");
            Check(game.Load()==SaveResult.NotFound,"F9 cannot revive a completed game");
            Check(game.CheckManualSave()==SaveResult.NotFound && game.CheckContinuation()==SaveResult.NotFound,
                "Completed-game load and Continue controls are unavailable");
            Check(game.Continue()==SaveResult.NotFound,"Continue cannot restart the finale");
            game.Autosave(); // explicit Quit
            game.Autosave(); // OnApplicationQuit
            EmptySlots();
            game.EnterMainMenu();
            Check(game.Session==null,"Main menu drops the completed in-memory continuation");
            game.Autosave(); EmptySlots();
            var fresh=Progress();
            Check(fresh.CheckContinuation()==SaveResult.NotFound && fresh.Continue()==SaveResult.NotFound,
                "A new process cannot load the completed run");
            fresh.StartNewGame();
            Check(fresh.Session.LevelNumber==1 && fresh.Session.Platform=="P1","The next game starts at tutorial P1");
            Check(!File.Exists(Manual) && File.Exists(Automatic),"Only the new initial autosave is created");

            // Migrate completed files from an older build without falling back to an older manual point.
            GameSession oldFinal=CompletedSession();
            var oldManual=TutorialCampaign.CreateSession();
            Check(new SaveStore(Manual).Save(oldManual)==SaveResult.Success,"Create an older manual fallback");
            Check(new SaveStore(Automatic).Save(oldFinal)==SaveResult.Success,"Create an old final autosave");
            var legacy=Progress();
            Check(legacy.CheckContinuation()==SaveResult.NotFound && legacy.Session==null,"An old completed autosave is discarded before adoption");
            EmptySlots();
            Check(new SaveStore(Manual).Save(oldFinal)==SaveResult.Success,"Create an old final manual-only save");
            Check(Progress().Continue()==SaveResult.NotFound,"An old final manual save is not a continuation either");
            EmptySlots();

            var replay=Progress(); replay.StartNewGame(); Finish(replay,false); replay.RestartLevel();
            Check(replay.Session.LevelNumber==1 && replay.Session.Platform=="P1","Final Retry/New Game starts a fresh campaign");
            Check(!File.Exists(Manual),"Final replay does not restore the erased manual point");

            // A deletion failure must be visible, and Quit must not write the final checkpoint back.
            new SaveStore(Automatic).Delete();
            Directory.CreateDirectory(Manual);
            var failure=Progress(); failure.StartNewGame(); Finish(failure,false);
            Check(failure.LastCleanupResult==SaveResult.IoError,"A blocked manual-slot deletion reports an I/O failure");
            Check(!File.Exists(Automatic),"The automatic slot is still cleared if the other slot fails");
            failure.Autosave();
            Check(!File.Exists(Automatic),"A repeated quit callback cannot recreate the automatic slot after a cleanup error");
            Directory.Delete(Manual); failure.Autosave();
            Check(failure.LastCleanupResult==SaveResult.Success,"Cleanup can retry after the file obstruction is removed");
            EmptySlots();

            var authenticated=SaveStore.ForChallenge(Path.Combine(folder,"signed.xml"));
            Check(authenticated.Save(TutorialCampaign.CreateSession())==SaveResult.Success,"Create an authenticated slot");
            Check(File.Exists(Path.Combine(folder,"signed.xml.key")),"The authenticated slot has a key");
            Check(authenticated.Delete()==SaveResult.Success && !File.Exists(Path.Combine(folder,"signed.xml"))
                && !File.Exists(Path.Combine(folder,"signed.xml.key")),"Deleting a signed slot also removes its own key");
            Check(authenticated.Delete()==SaveResult.Success,"Deleting an absent slot is idempotent");
            Check(new SaveStore(Path.Combine(folder,"missing","slot.xml")).Delete()==SaveResult.Success,
                "A missing save directory does not make cleanup fail");
            Console.WriteLine("Completion cleanup and exit lifecycle: "+checks+" checks passed.");
        }
        finally { Directory.Delete(folder,true); }
    }

    private static void EmptySlots() => Check(!File.Exists(Manual)&&!File.Exists(Automatic),"Both completed-campaign slots are absent");
    private static void Walk(GameProgress game,string target)
    { Check(game.Session.TryStartMove(target,out Move move)&&game.CompleteMove(move),"Walk to "+target); }
    private static void Finish(GameProgress game,bool checkIntermediate)
    {
        for(int actions=0;!game.Session.IsGameComplete&&actions<256;actions++)
        {
            if(game.Session.IsLevelComplete)
            {
                if(checkIntermediate)Check(File.Exists(Manual)&&File.Exists(Automatic),"An intermediate victory does not clear either save slot");
                Check(game.TryNextLevel(),"Advance an intermediate completion");continue;
            }
            HintStep hint=game.Session.GetHint(); Check(hint!=null,"The completion route is solvable");
            if(hint.Kind==HintKind.Toggle)Check(game.TryToggleMode(),"Switch on the route");
            else Walk(game,hint.Target);
        }
        Check(game.Session.IsGameComplete,"Complete all five levels");
    }
    private static GameSession CompletedSession()
    {
        var game=TutorialCampaign.CreateSession();
        for(int actions=0;!game.IsGameComplete&&actions<256;actions++)
        {
            if(game.IsLevelComplete) { Check(game.TryNextLevel(),"Advance the old-save fixture");continue; }
            HintStep hint=game.GetHint();
            if(hint.Kind==HintKind.Toggle)Check(game.TryToggleMode(),"Switch in the old-save fixture");
            else Check(game.TryStartMove(hint.Target,out Move move)&&game.CompleteMove(move),"Walk in the old-save fixture");
        }
        Check(game.IsGameComplete,"The legacy fixture contains an actual final victory");return game;
    }
    private static void Check(bool value,string message) { checks++;if(!value)throw new InvalidOperationException(message); }
}
