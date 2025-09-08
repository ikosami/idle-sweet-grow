using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class StageManger : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] int width = 500;
    [SerializeField] int height = 300;
    [SerializeField] Image image;
    [SerializeField] Texture2D[] candyTextures;
    [SerializeField] Sprite[] candySprites;

    Texture2D texture;

    void Awake()
    {
        texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        var pixels = new Color32[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;
        texture.SetPixels32(pixels);
        texture.Apply();

        image.sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    void Start()
    {
        SpawnCandy();
    }

    public void OnPointerDown(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            image.rectTransform, e.position, e.pressEventCamera, out var local);

        int x = Mathf.RoundToInt(local.x + width / 2f);
        int y = Mathf.RoundToInt(local.y + height / 2f);

        for (int j = -5; j <= 5; j++)
            for (int i = -5; i <= 5; i++)
            {
                int px = x + i;
                int py = y + j;
                if (0 <= px && px < width && 0 <= py && py < height)
                    texture.SetPixel(px, py, Color.clear);
            }

        texture.Apply();
    }

    public void SpawnCandy()
    {
        if (candyTextures.Length + candySprites.Length == 0) return;

        Color[] pixels = GetRandomSource(out int w, out int h);
        if (pixels == null) return;

        int x0 = Random.Range(0, Mathf.Max(1, width - w));
        int y0 = Random.Range(0, Mathf.Max(1, height - h));

        texture.SetPixels(x0, y0, w, h, pixels);
        texture.Apply();
    }

    Color[] GetRandomSource(out int w, out int h)
    {
        int total = candyTextures.Length + candySprites.Length;
        int index = Random.Range(0, total);

        if (index < candyTextures.Length)
        {
            var tex = candyTextures[index];
            w = tex.width;
            h = tex.height;
            return tex.GetPixels();
        }
        else
        {
            var spr = candySprites[index - candyTextures.Length];
            Rect r = spr.textureRect;
            w = (int)r.width;
            h = (int)r.height;
            return spr.texture.GetPixels((int)r.x, (int)r.y, w, h);
        }
    }
}
