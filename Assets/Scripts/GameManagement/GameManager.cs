using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private int currentLevel = -1;

    void Start()
    {
        LoadMainMenu();
    }

    private void LoadMainMenu()
    {
        SceneManager.LoadSceneAsync((int)SceneID.MainMenu, LoadSceneMode.Additive);
    }

    public void MenuLoadLevel(int sceneID)
    {
        if (currentLevel > 0)
        {
            Debug.LogError("MenuLoadLevel can not be used in game!");
            return;
        }

        currentLevel = sceneID;

        SceneManager.UnloadSceneAsync((int)SceneID.MainMenu);
        SceneManager.LoadSceneAsync((int)SceneID.Player, LoadSceneMode.Additive);
        SceneManager.LoadSceneAsync(sceneID, LoadSceneMode.Additive);
    }

    public void GameLoadLevel(int sceneID)
    {
        if (currentLevel < 0)
        {
            Debug.LogError("GameLoadLevel can not be used in the main menu!", gameObject);
            return;
        }

        SceneManager.UnloadSceneAsync(currentLevel);
        SceneManager.LoadSceneAsync(sceneID, LoadSceneMode.Additive);

        currentLevel = sceneID;
    }

    public void ExitToMainMenu()
    {
        SceneManager.UnloadSceneAsync(currentLevel);
        SceneManager.UnloadSceneAsync((int)SceneID.Player);

        currentLevel = -1;

        LoadMainMenu();
    }
}
