using System;
using System.Collections.Generic;
using OLE.Core;

internal static class TraversalMatrixTests
{
    public static void Run()
    {
        GameSession campaign=TutorialCampaign.CreateSession();
        int levels=0,transitions=0,denied=0;
        while(true)
        {
            LevelDefinition level=campaign.Level;levels++;
            var queue=new Queue<(string,BoardMode)>();
            var seen=new HashSet<(string,BoardMode)> { (level.Start,BoardMode.A) };
            queue.Enqueue((level.Start,BoardMode.A));
            while(queue.Count>0)
            {
                var state=queue.Dequeue();
                var game=new GameSession(new[] {level});
                Require(game.TryRestore(level.Id,state.Item1,state.Item2),"Restore a reachable state for "+level.Id);
                foreach(string target in level.Platforms)
                {
                    bool expected=state.Item1!=level.Goal && level.CanTraverse(state.Item1,target,state.Item2);
                    bool accepted=game.TryStartMove(target,out Move move);
                    Require(accepted==expected,level.Id+": wrong move decision "+state.Item1+" -> "+target+" in "+state.Item2);
                    if(accepted)
                    {
                        Require(!game.TryToggleMode(),"A moving avatar cannot switch the mesh pose");
                        Require(game.CompleteMove(move)&&game.Platform==target,"Finish every accepted transition");
                        Add((target,state.Item2),seen,queue);transitions++;
                        if(target!=level.Goal)
                            Require(game.TryStartMove(state.Item1,out Move back)&&game.CompleteMove(back),"Every nonterminal open edge is reversible");
                    }
                    else denied++;
                    Require(game.TryRestore(level.Id,state.Item1,state.Item2),"Reset the probe without touching the live session");
                }
                if(state.Item1!=level.Goal && level.CanToggleAt(state.Item1))
                    Add((state.Item1,state.Item2==BoardMode.A?BoardMode.B:BoardMode.A),seen,queue);
            }
            for(int n=0;!campaign.IsLevelComplete&&n<256;n++)
            {
                HintStep hint=campaign.GetHint();Require(hint!=null,"Every level retains a solution");
                if(hint.Kind==HintKind.Toggle)Require(campaign.TryToggleMode(),"Valid switch hint");
                else Require(campaign.TryStartMove(hint.Target,out Move move)&&campaign.CompleteMove(move),"Valid movement hint");
            }
            Require(campaign.IsLevelComplete,"Finish the probe level");
            if(!campaign.HasNextLevel)break;
            Require(campaign.TryNextLevel(),"Advance the probe level");
        }
        Console.WriteLine("Traversal matrix: "+levels+" levels, "+transitions+" allowed transitions, "+denied+" blocked destinations checked.");
    }
    private static void Add((string,BoardMode) state,HashSet<(string,BoardMode)> seen,Queue<(string,BoardMode)> queue)
    { if(seen.Add(state))queue.Enqueue(state); }
    private static void Require(bool value,string message) { if(!value)throw new InvalidOperationException(message); }
}
