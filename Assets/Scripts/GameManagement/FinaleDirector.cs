using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static MenuUI;

// A short in-engine ending. The assigned clip is capped at one minute and never loops.
public sealed class FinaleDirector : MonoBehaviour
{
    private float duration;
    private GameplayController game;
    private AudioSource music;
    private Camera sceneCamera;
    private Vector3 cameraPosition, focus;
    private Quaternion cameraRotation;
    private float cameraSize, elapsed;
    private bool cameraCaptured, finished;
    private RectTransform crawl;
    private Text title, status;
    private Button skip, replay, menu;
    private CelebrationEffect confetti;
    public bool IsPlaying { get; private set; }

    public static FinaleDirector Create(GameplayController game, Transform canvas)
    {
        RectTransform root = CreateRect("Finale", canvas);
        Stretch(root);
        root.gameObject.AddComponent<Image>().color = new Color(0.015f,0.025f,0.06f,0.94f);
        var director = root.gameObject.AddComponent<FinaleDirector>();
        director.Build(game);
        director.confetti = CelebrationEffect.Create(root);
        director.gameObject.SetActive(false);
        return director;
    }

    private void Build(GameplayController game)
    {
        this.game = game;
        PresentationSettings settings = PresentationSettings.Load();
        music = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = false; music.spatialBlend = 0; music.loop = false;
        music.clip = settings.finaleMusic;
        var random = new System.Random(42);
        for (int i = 0; i < 64; i++)
        {
            RectTransform star = CreateRect("Star",transform);
            star.anchoredPosition = new Vector2((float)random.NextDouble()*1240-620,(float)random.NextDouble()*680-340);
            star.sizeDelta = Vector2.one * (i%4 == 0 ? 3 : 1.5f);
            Image dot = star.gameObject.AddComponent<Image>();
            dot.color = new Color(0.75f,0.86f,1,0.2f+(float)random.NextDouble()*0.45f);
            dot.raycastTarget = false;
        }
        RectTransform heading = CreateRect("Congratulations",transform);
        heading.anchoredPosition = new Vector2(0,268); heading.sizeDelta = new Vector2(1120,56);
        title = AddText(heading,"Gratulálunk!",38);
        title.color = new Color(1,0.8f,0.36f);
        RectTransform subtitle = CreateRect("Message",transform);
        subtitle.anchoredPosition = new Vector2(0,215); subtitle.sizeDelta = new Vector2(1100,40);
        AddText(subtitle,"Minden pályát teljesítettél. Köszönjük, hogy végigjátszottad!",21);
        RectTransform viewport = CreateRect("Credits viewport",transform);
        viewport.anchoredPosition = new Vector2(0,0); viewport.sizeDelta = new Vector2(1000,400);
        viewport.gameObject.AddComponent<RectMask2D>();
        crawl = CreateRect("Creators",viewport); crawl.sizeDelta = new Vector2(900,620);
        string names = "NECKER'S PARADOX\n\nALKOTÓK\n\n" + string.Join("\n", settings.creators ?? Array.Empty<string>());
        Text credits = AddText(crawl,names,30); credits.lineSpacing = 1.4f; credits.supportRichText = false;
        skip = CreateButton("Skip","Kihagyás",transform,-270); skip.onClick.AddListener(Skip);
        replay = CreateButton("Replay","Új játék",transform,-270); replay.onClick.AddListener(game.ReplayCampaign);
        menu = CreateButton("Main Menu","Főmenü",transform,-270); menu.onClick.AddListener(game.ExitToMenu);
        replay.GetComponent<RectTransform>().anchoredPosition = new Vector2(-160,-270);
        menu.GetComponent<RectTransform>().anchoredPosition = new Vector2(160,-270);
        RectTransform feedback = CreateRect("Save status",transform);
        feedback.anchoredPosition = new Vector2(0,-326); feedback.sizeDelta = new Vector2(1100,36);
        status = AddText(feedback,"",13);
    }

    public void Play()
    {
        if (IsPlaying) return;
        elapsed = 0; finished = false; IsPlaying = true;
        duration = Mathf.Clamp(PresentationSettings.Load().finaleDuration, 10, 60);
        if (music.clip != null) duration = Mathf.Min(duration, music.clip.length);
        sceneCamera = Camera.main;
        if (sceneCamera != null)
        {
            cameraPosition = sceneCamera.transform.position; cameraRotation = sceneCamera.transform.rotation;
            cameraSize = sceneCamera.orthographicSize; cameraCaptured = true;
        }
        focus = game.LevelCenter;
        title.text = "Gratulálunk!";
        skip.gameObject.SetActive(true); replay.gameObject.SetActive(false); menu.gameObject.SetActive(false);
        crawl.anchoredPosition = new Vector2(0,-450);
        status.text = game.SaveFeedback;
        gameObject.SetActive(true);
        confetti.Play(true);
        music.volume = 0;
        if (music.clip != null) music.Play();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(skip.gameObject);
    }

    private void Update()
    {
        if (!IsPlaying || finished) return;
        elapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(elapsed / duration);
        // Read all four names early, then leave them visible while the music finishes.
        crawl.anchoredPosition = new Vector2(0,Mathf.Lerp(-450,0,Mathf.Clamp01(elapsed/18)));
        if (sceneCamera != null)
        {
            Vector3 offset = Quaternion.AngleAxis(progress*10,Vector3.up) * (cameraPosition-focus);
            sceneCamera.transform.position = focus + offset * Mathf.Lerp(1,1.08f,progress);
            sceneCamera.transform.LookAt(focus);
            sceneCamera.orthographicSize = Mathf.Lerp(cameraSize,cameraSize*1.12f,progress);
        }
        float fade = Mathf.Min(Mathf.Clamp01(elapsed/1.5f),Mathf.Clamp01((duration-elapsed)/3));
        music.volume = PresentationSettings.Load().musicVolume * fade;
        if (elapsed >= duration) Skip();
    }

    public void Skip()
    {
        if (!IsPlaying || finished) return;
        finished = true; music.Stop();
        ClearSelection(transform);
        skip.gameObject.SetActive(false); replay.gameObject.SetActive(true); menu.gameObject.SetActive(true);
        crawl.anchoredPosition = Vector2.zero;
        title.text = "Köszönjük, hogy játszottál!";
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(menu.gameObject);
    }

    public void Stop()
    {
        ClearSelection(transform);
        music.Stop(); RestoreCamera(); IsPlaying = false;
        gameObject.SetActive(false);
    }

    private void RestoreCamera()
    {
        if (cameraCaptured && sceneCamera != null)
        {
            sceneCamera.transform.SetPositionAndRotation(cameraPosition,cameraRotation);
            sceneCamera.orthographicSize = cameraSize;
        }
        cameraCaptured = false;
    }

    private void OnDisable()
    {
        if (music != null) music.Stop();
        RestoreCamera(); IsPlaying = false;
    }
}
