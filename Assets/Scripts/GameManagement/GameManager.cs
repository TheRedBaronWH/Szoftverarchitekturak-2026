using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool IsTransitioning { get; private set; }
    public int CurrentSceneIndex => currentScene;
    public event Action<int> SceneReady;

    private const int NoScene = -1;
    private int currentScene = NoScene;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        BeginTransition((int)SceneID.MainMenu);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void MenuLoadLevel(int sceneID)
    {
        if (currentScene == (int)SceneID.MainMenu && IsPlayableLevel(sceneID))
            BeginTransition(sceneID);
    }

    public void GameLoadLevel(int sceneID)
    {
        if (IsPlayableLevel(currentScene) && IsPlayableLevel(sceneID))
            BeginTransition(sceneID);
    }

    public void ExitToMainMenu()
    {
        if (IsPlayableLevel(currentScene))
            BeginTransition((int)SceneID.MainMenu);
    }

    private static bool IsPlayableLevel(int sceneID)
    {
        return sceneID >= 0
            && sceneID < SceneManager.sceneCountInBuildSettings
            && sceneID != (int)SceneID.GameManagement
            && sceneID != (int)SceneID.Player
            && sceneID != (int)SceneID.MainMenu;
    }

    private void BeginTransition(int destination)
    {
        if (IsTransitioning)
            return;

        IsTransitioning = true;
        StartCoroutine(ChangeScene(destination));
    }

    private IEnumerator ChangeScene(int destination)
    {
        bool wasPlaying = IsPlayableLevel(currentScene);
        bool willPlay = IsPlayableLevel(destination);

        // GameManagement stays loaded throughout the transition.
        if (currentScene != NoScene)
            yield return SceneManager.UnloadSceneAsync(currentScene);

        if (wasPlaying && !willPlay)
            yield return SceneManager.UnloadSceneAsync((int)SceneID.Player);

        if (!wasPlaying && willPlay)
            yield return SceneManager.LoadSceneAsync((int)SceneID.Player, LoadSceneMode.Additive);

        yield return SceneManager.LoadSceneAsync(destination, LoadSceneMode.Additive);

        currentScene = destination;
        IsTransitioning = false;
        SceneReady?.Invoke(destination);
    }
}
