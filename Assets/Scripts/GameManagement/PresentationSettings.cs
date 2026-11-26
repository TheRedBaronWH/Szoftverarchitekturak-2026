using UnityEngine;

[CreateAssetMenu(menuName = "OLE/Presentation settings")]
public sealed class PresentationSettings : ScriptableObject
{
    public Material solidMaterial;
    public Material unlitMaterial;
    public Color surface = new Color(0.95f, 0.96f, 0.98f);
    public Color ink = new Color(0.09f, 0.14f, 0.22f);
    public Color accent = new Color(0.04f, 0.62f, 0.65f);
    public Color goal = new Color(0.95f, 0.65f, 0.2f);
    public Color background = new Color(0.84f, 0.88f, 0.93f);

    [Header("Finale")]
    public AudioClip finaleMusic;
    [Range(0, 1)] public float musicVolume = 0.2f;
    public string[] creators = { "Kopácsi Artem", "Kohár Zsombor", "Bencze János István", "Béres Mátyás" };

    private static PresentationSettings current;
    public static PresentationSettings Load()
    {
        if (current == null)
        {
            current = Resources.Load<PresentationSettings>("OLE/Presentation");
            if (current == null) current = CreateInstance<PresentationSettings>();
        }
        return current;
    }
}
