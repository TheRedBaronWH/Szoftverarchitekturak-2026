using UnityEditor;
using UnityEngine;

public static class BoardCheckMenu
{
    [MenuItem("Tools/OLE/Check all level boards (Play Mode)")]
    private static void CheckBoards()
    {
        TutorialBoard board = Object.FindAnyObjectByType<TutorialBoard>();
        if (board == null) Debug.LogError("Open GameManagement, press Play and choose Új játék first.");
        else board.ValidateBoardsInPlayMode();
    }
}
