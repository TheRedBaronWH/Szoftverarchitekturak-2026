using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private int currentLevel = (int)SceneID.GameManagement;

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
        currentLevel = sceneID;

        SceneManager.UnloadSceneAsync((int)SceneID.MainMenu);
        SceneManager.LoadSceneAsync((int)SceneID.Player, LoadSceneMode.Additive);
        SceneManager.LoadSceneAsync(sceneID, LoadSceneMode.Additive);
    }

    public void GameLoadLevel(int sceneID)
    {
        SceneManager.UnloadSceneAsync(currentLevel);
        SceneManager.LoadSceneAsync(sceneID, LoadSceneMode.Additive);
    }

    public void ExitToMainMenu()
    {
        SceneManager.UnloadSceneAsync(currentLevel);
        SceneManager.UnloadSceneAsync((int)SceneID.Player);

        LoadMainMenu();
    }
}
