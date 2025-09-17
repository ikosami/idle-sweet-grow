using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class StageManger : MonoBehaviour, IPointerDownHandler
{
    [System.Serializable]
    class GroundLayer
    {
        public Sprite sprite;
        [Min(1)] public int depth = 50;
        [Min(0)] public int oreValue = 1;
        public Color fallbackColor = Color.gray;
    }

    [SerializeField] int width = 500;
    [SerializeField] int height = 300;
    [SerializeField] Image stageImage;
    [SerializeField] GroundLayer[] groundLayers;
    [SerializeField, Range(0, 8)] int miningRadius = 1;

    [SerializeField] TextMeshProUGUI moneyText;

    Texture2D stageTexture;
    Sprite stageSprite;
    Color32[] stagePixels;
    bool[] minedPixels;
    int[] oreValues;
    Texture2D[] layerTextures;

    public static StageManger Instance { get; private set; }
    int money = 0;

    void Awake()
    {
        Instance = this;

        stageTexture = new Texture2D(width, height, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        stagePixels = new Color32[width * height];
        minedPixels = new bool[stagePixels.Length];
        oreValues = new int[stagePixels.Length];
        layerTextures = groundLayers != null ? new Texture2D[groundLayers.Length] : new Texture2D[0];

        GenerateGround();

        stageTexture.SetPixels32(stagePixels);
        stageTexture.Apply(false);

        stageSprite = Sprite.Create(stageTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        if (stageImage != null)
        {
            stageImage.sprite = stageSprite;
            stageImage.SetNativeSize();
        }

        moneyUpdate();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (stageSprite != null)
        {
            Destroy(stageSprite);
            stageSprite = null;
        }

        if (stageTexture != null)
        {
            Destroy(stageTexture);
            stageTexture = null;
        }

        if (layerTextures != null)
        {
            for (int i = 0; i < layerTextures.Length; i++)
            {
                if (layerTextures[i] != null)
                {
                    Destroy(layerTextures[i]);
                    layerTextures[i] = null;
                }
            }
        }
    }

    void GenerateGround()
    {
        for (int i = 0; i < stagePixels.Length; i++)
        {
            stagePixels[i] = Color.clear;
            oreValues[i] = 0;
            minedPixels[i] = false;
        }

        if (groundLayers == null)
            return;

        int yOffset = 0;
        for (int layerIndex = 0; layerIndex < groundLayers.Length; layerIndex++)
        {
            GroundLayer layer = groundLayers[layerIndex];
            if (layer == null)
                continue;

            int layerHeight = Mathf.Clamp(layer.depth, 0, height - yOffset);
            if (layerHeight <= 0)
                continue;

            Texture2D layerTexture = GetReadableLayerTexture(layerIndex, layer.sprite);

            for (int y = 0; y < layerHeight; y++)
            {
                int globalY = yOffset + y;
                float v = layerHeight <= 1 ? 0f : (float)y / (layerHeight - 1);

                for (int x = 0; x < width; x++)
                {
                    int index = globalY * width + x;
                    float u = width <= 1 ? 0f : (float)x / (width - 1);
                    stagePixels[index] = SampleLayerColor(layer, layerTexture, u, v);
                    oreValues[index] = Mathf.Max(0, layer.oreValue);
                }
            }

            yOffset += layerHeight;
            if (yOffset >= height)
                break;
        }
    }

    Texture2D GetReadableLayerTexture(int index, Sprite sprite)
    {
        if (sprite == null)
            return null;

        if (layerTextures[index] != null)
            return layerTextures[index];

        Rect rect = sprite.rect;
        Texture2D texture = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.ARGB32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        Color[] pixels = sprite.texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
        texture.SetPixels(pixels);
        texture.Apply(false);

        layerTextures[index] = texture;
        return texture;
    }

    Color32 SampleLayerColor(GroundLayer layer, Texture2D texture, float u, float v)
    {
        if (texture == null)
            return layer.fallbackColor;

        return texture.GetPixelBilinear(u, v);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        TryMinePixel(eventData.position, eventData.pressEventCamera);
    }

    public bool TryMinePixel(Vector2 screenPosition, Camera camera)
    {
        if (stageImage == null || stageTexture == null)
            return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(stageImage.rectTransform, screenPosition, camera, out Vector2 localPoint))
            return false;

        Rect rect = stageImage.rectTransform.rect;
        if (!rect.Contains(localPoint))
            return false;

        float normalizedX = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        float normalizedY = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);

        int pixelX = Mathf.Clamp(Mathf.FloorToInt(normalizedX * width), 0, width - 1);
        int pixelY = Mathf.Clamp(Mathf.FloorToInt(normalizedY * height), 0, height - 1);

        int gained = MineArea(pixelX, pixelY, out bool minedAny);
        if (gained > 0)
            AddMoney(gained);

        return minedAny;
    }

    int MineArea(int centerX, int centerY, out bool minedAny)
    {
        int totalValue = 0;
        minedAny = false;

        for (int y = -miningRadius; y <= miningRadius; y++)
        {
            for (int x = -miningRadius; x <= miningRadius; x++)
            {
                totalValue += MineSinglePixel(centerX + x, centerY + y, ref minedAny);
            }
        }

        if (minedAny)
            stageTexture.Apply(false);

        return totalValue;
    }

    int MineSinglePixel(int x, int y, ref bool minedAny)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return 0;

        int index = y * width + x;
        if (minedPixels[index])
            return 0;

        minedPixels[index] = true;
        minedAny = true;

        int value = oreValues[index];
        oreValues[index] = 0;
        stagePixels[index] = Color.clear;
        stageTexture.SetPixel(x, y, Color.clear);
        return value;
    }

    void moneyUpdate()
    {
        if (moneyText != null)
            moneyText.text = $"Money: {money}";
    }

    public void AddMoney(int amount)
    {
        money += amount;
        moneyUpdate();
    }
}
