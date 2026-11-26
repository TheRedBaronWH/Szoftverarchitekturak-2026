using UnityEngine;

public sealed class MainMenuActions : MonoBehaviour
{
    // Called by the New Game button in MainMenu.unity.
    public void StartTutorial()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.MenuLoadLevel((int)SceneID.SampleScene);
    }
}
