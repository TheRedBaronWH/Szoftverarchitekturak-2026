using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuActions : MonoBehaviour
{
    private void Awake()
    {
        Text label = GetComponentInChildren<Text>();
        if (label != null)
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    // Called by the New Game button in MainMenu.unity.
    public void StartTutorial()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.MenuLoadLevel((int)SceneID.SampleScene);
    }
}
