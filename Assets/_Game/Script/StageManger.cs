using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StageManger : MonoBehaviour
{
    [SerializeField] int width = 500;
    [SerializeField] int height = 300;
    [SerializeField] Image stageImage;
    [SerializeField] Sprite[] candySprites;
    [SerializeField] float spawnInterval = 5f;
    [SerializeField] float growDuration = 2f;

    Texture2D stageTexture;

    void Awake()
    {
        stageTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        var pixels = new Color32[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;
        stageTexture.SetPixels32(pixels);
        stageTexture.Apply();

        stageImage.sprite = Sprite.Create(stageTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            SpawnCandy();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnCandy()
    {
        if (candySprites.Length == 0) return;
        Sprite source = candySprites[Random.Range(0, candySprites.Length)];

        Sprite sprite = CreateSpriteCopy(source, out Texture2D tex);

        GameObject go = new GameObject("Candy", typeof(Image), typeof(Candy));
        go.transform.SetParent(stageImage.transform, false);

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Vertical;
        img.fillOrigin = (int)Image.OriginVertical.Bottom;
        img.fillAmount = 0f;
        img.SetNativeSize();

        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0.5f, 0f);
        float maxX = Mathf.Max(0f, width - rt.rect.width);
        rt.anchoredPosition = new Vector2(Random.Range(0f, maxX), 0f);

        Candy candy = go.GetComponent<Candy>();
        candy.Initialize(tex, img);

        StartCoroutine(Grow(img));
    }

    Sprite CreateSpriteCopy(Sprite source, out Texture2D tex)
    {
        Rect r = source.rect;
        tex = new Texture2D((int)r.width, (int)r.height, TextureFormat.ARGB32, false);
        Color[] pixels = source.texture.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), source.pixelsPerUnit);
    }

    IEnumerator Grow(Image img)
    {
        float t = 0f;
        while (t < growDuration)
        {
            t += Time.deltaTime;
            img.fillAmount = t / growDuration;
            yield return null;
        }
        img.fillAmount = 1f;
    }
}
