#if UNITY_EDITOR
using System;
using UnityEngine;
using OLE.Core;

public sealed partial class TutorialBoard
{
    // Executes against Unity's real Animator, generated meshes and physics scene.
    // The live GameSession and both save files remain untouched.
    [ContextMenu("Check all five level boards (Play Mode)")]
    public void ValidateBoardsInPlayMode()
    {
        var game = FindAnyObjectByType<GameplayController>();
        if (!Application.isPlaying || game == null || !game.IsReady || game.IsMoving || game.IsPaused || game.HasWon || IsChanging)
        {
            Debug.LogError("Start GameManagement / Új játék and wait until the player is standing before running the board check.");
            return;
        }
        LevelDefinition savedLevel = level;
        BoardMode savedMode = mode;
        string savedPlatform = currentPlatform;
        bool savedLabels = LabelsVisible;
        int levels = 0, clickable = 0, moves = 0, badges = 0;
        try
        {
            GameSession probe = TutorialCampaign.CreateSession();
            LabelsVisible = true;
            while (true)
            {
                Require(ShowLevel(probe.Level),"Missing board for "+probe.Level.Id);
                levels++;
                foreach (BoardMode checkMode in new[] { BoardMode.A, BoardMode.B })
                {
                    ShowMode(checkMode == BoardMode.B,true);
                    Physics.SyncTransforms();
                    int visibleCubes = 0;
                    foreach (NeckerCubeGFXController cube in FindObjectsByType<NeckerCubeGFXController>(FindObjectsSortMode.None))
                        if (cube.gameObject.scene == gameObject.scene) visibleCubes++;
                    Require(visibleCubes == level.Platforms.Count,level.Id+": an unbound cube is still visible");
                    foreach (string id in level.Platforms)
                    {
                        Rect badge = BadgeRect(id);
                        Vector2 point = new Vector2(badge.center.x,Screen.height-badge.center.y);
                        Require(PickTarget(point,level.Start) == id,level.Id+": badge selects the wrong platform "+id);
                        badges++;
                    }
                    foreach (StairBridge bridge in bridges)
                    {
                        bool open = bridge.Connection.IsOpen(checkMode);
                        Require(bridge.IsSelectable == open,level.Id+": selectable state differs from rules for "+bridge.name);
                        if (!open) continue;
                        Vector3 screen = Camera.main.WorldToScreenPoint(bridge.SelectionProbe);
                        Ray ray = Camera.main.ScreenPointToRay(screen);
                        Require(bridge.GetComponent<MeshCollider>().Raycast(ray,out _,1000),level.Id+": raycast misses "+bridge.name);
                        clickable++;
                        foreach (string from in new[] {bridge.Connection.First,bridge.Connection.Second})
                        {
                            string to = from == bridge.Connection.First ? bridge.Connection.Second : bridge.Connection.First;
                            Require(Vector3.Distance(bridge.Sample(from,0).position,surfaces[from].Support.position)<0.001f,"Wrong departure on "+bridge.name);
                            Require(Vector3.Distance(bridge.Sample(from,1).position,surfaces[to].Support.position)<0.001f,"Wrong arrival on "+bridge.name);
                            moves++;
                        }
                    }
                }
                for (int actions=0; !probe.IsLevelComplete && actions<256; actions++)
                {
                    HintStep hint = probe.GetHint(); Require(hint != null,"No solution for "+probe.Level.Id);
                    if (hint.Kind == HintKind.Toggle) Require(probe.TryToggleMode(),"Invalid toggle hint");
                    else Require(probe.TryStartMove(hint.Target,out Move move) && probe.CompleteMove(move),"Invalid move hint");
                }
                Require(probe.IsLevelComplete,"Could not finish "+probe.Level.Id);
                if (!probe.HasNextLevel) break;
                Require(probe.TryNextLevel(),"Could not advance the probe campaign");
            }
            Debug.Log("OLE board check PASS: "+levels+" levels, "+badges+" badge checks, "+clickable+" actual stair raycasts, "+moves+" bidirectional endpoint checks.");
        }
        catch (Exception error) { Debug.LogException(error); }
        finally
        {
            ShowLevel(savedLevel); ShowMode(savedMode == BoardMode.B,true);
            HighlightPlatforms(savedPlatform,savedMode); LabelsVisible = savedLabels;
            Physics.SyncTransforms();
        }
    }

    private static void Require(bool value,string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
#endif
