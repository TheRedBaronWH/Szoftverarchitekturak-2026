using UnityEngine;
using UnityEngine.UI;
using static MenuUI;

// A small reusable UI pool: no particle texture, physics object or extra camera is needed.
public sealed class CelebrationEffect : MonoBehaviour
{
    private struct Piece { public RectTransform rect; public Image image; public Vector2 start, velocity; public float spin, tilt; }
    private Piece[] pieces;
    private float elapsed, duration;
    private bool playing;

    public static CelebrationEffect Create(Transform parent)
    {
        RectTransform root = CreateRect("Celebration confetti", parent);
        Stretch(root);
        var effect = root.gameObject.AddComponent<CelebrationEffect>();
        effect.pieces = new Piece[96];
        for (int i = 0; i < effect.pieces.Length; i++)
        {
            RectTransform rect = CreateRect("Confetti", root);
            rect.sizeDelta = new Vector2(7 + i % 3, i % 4 == 0 ? 5 : 13);
            Image image = rect.gameObject.AddComponent<Image>(); image.raycastTarget = false;
            effect.pieces[i] = new Piece { rect = rect, image = image };
            rect.gameObject.SetActive(false);
        }
        return effect;
    }

    public void Play(bool finale)
    {
        var random = new System.Random();
        elapsed = 0; duration = finale ? 7 : 2.3f; playing = true;
        Rect rect = ((RectTransform)transform).rect;
        float width = Mathf.Max(600, rect.width), height = Mathf.Max(400, rect.height);
        Color[] colors = { new Color(1,0.76f,0.25f), new Color(0.2f,0.9f,0.77f),
            new Color(0.52f,0.71f,1), new Color(1,0.48f,0.58f), Color.white };
        for (int i = 0; i < pieces.Length; i++)
        {
            Piece piece = pieces[i];
            piece.start = finale ? new Vector2((float)random.NextDouble()*width-width/2, height/2+40)
                : new Vector2(i%2 == 0 ? -width/2 : width/2, -height*0.25f);
            piece.velocity = finale ? new Vector2((float)random.NextDouble()*110-55, -20-(float)random.NextDouble()*90)
                : new Vector2((i%2 == 0 ? 1 : -1)*(160+(float)random.NextDouble()*180), 260+(float)random.NextDouble()*200);
            piece.spin = (float)random.NextDouble()*360-180; piece.tilt = (float)random.NextDouble()*6;
            piece.image.color = colors[i%colors.Length]; piece.rect.anchoredPosition = piece.start;
            piece.rect.gameObject.SetActive(finale || i < 56);
            pieces[i] = piece;
        }
    }

    private void Update()
    {
        if (!playing) return;
        elapsed += Time.unscaledDeltaTime;
        for (int i = 0; i < pieces.Length; i++)
        {
            Piece piece = pieces[i];
            piece.rect.anchoredPosition = piece.start + piece.velocity*elapsed + Vector2.down*(65*elapsed*elapsed);
            piece.rect.localRotation = Quaternion.Euler(0,0,piece.spin*elapsed);
            piece.rect.localScale = new Vector3(0.25f+0.75f*Mathf.Abs(Mathf.Sin(elapsed*4+piece.tilt)),1,1);
            Color color = piece.image.color; color.a = Mathf.Clamp01((duration-elapsed)/0.7f); piece.image.color = color;
        }
        if (elapsed >= duration) Clear();
    }

    private void Clear()
    {
        playing = false;
        if (pieces != null) foreach (Piece piece in pieces) piece.rect.gameObject.SetActive(false);
    }

    private void OnDisable() => Clear();
}
