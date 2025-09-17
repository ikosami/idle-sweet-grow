using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Linq;

public class StageManger : MonoBehaviour
{
    // ステージ全体の幅と高さ（ピクセル）
    [SerializeField] int width = 500;
    [SerializeField] int height = 300;

    // 採掘対象となるUI上のImage
    [SerializeField] Image stageImage;

    // ステージ上に定期的に出現するキャンディのスプライト一覧
    [SerializeField] Sprite[] candySprites;

    // キャンディの出現間隔（秒）
    [SerializeField] float spawnInterval = 5f;

    // 所持金を表示するテキスト
    [SerializeField] TextMeshProUGUI moneyText;

    // ステージ構成情報（複数レイヤーで構築）
    [SerializeField] StageLayerDefinition[] layerDefinitions;

    // 採掘の半径（影響範囲）
    [SerializeField] int miningRadius = 6;

    // 採掘1回あたりの耐久度削り量
    [SerializeField] float miningPower = 1f;

    // 実際の描画用テクスチャ
    Texture2D stageTexture;

    // 各ピクセルの色情報（描画キャッシュ）
    Color32[] stagePixels;

    // 各ピクセルがどのレイヤーに属しているか
    int[] layerIndexMap;

    // 各ピクセルの残り耐久度
    float[] durabilityMap;

    // レイヤーの情報まとめ
    StageLayerInfo[] layerInfos = System.Array.Empty<StageLayerInfo>();

    // 実際のステージサイズ（レイヤーから算出）
    int stageWidth;
    int stageHeight;


    // シングルトン参照
    public static StageManger Instance { get; private set; }

    // プレイヤーの所持金
    int money = 0;

    void Awake()
    {
        Instance = this;

        // テクスチャの初期化（透明でクリア）
        stageTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        var pixels = new Color32[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;
        stageTexture.SetPixels32(pixels);
        stageTexture.Apply();

        // レイヤー定義に基づいて地形を生成
        BuildStageTexture();

        // テクスチャが生成できなかった場合はデフォルトをセット
        if (stageTexture == null)
        {
            stageWidth = 1;
            stageHeight = 1;
            stageTexture = new Texture2D(stageWidth, stageHeight, TextureFormat.ARGB32, false);
            stageTexture.filterMode = FilterMode.Point;
            stageTexture.wrapMode = TextureWrapMode.Clamp;
            stageTexture.SetPixel(0, 0, Color.clear);
            stageTexture.Apply();
        }

        // Imageにテクスチャをスプライトとして割り当て
        stageImage.sprite = Sprite.Create(stageTexture, new Rect(0, 0, stageTexture.width, stageTexture.height), new Vector2(0.5f, 0.5f));
        stageImage.rectTransform.sizeDelta = new Vector2(width, height);

        // 初期マネー表示
        moneyUpdate();
    }
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            MineAtScreenPosition(Input.mousePosition, Camera.main);
        }
    }

    // 所持金表示更新
    void moneyUpdate()
    {
        moneyText.text = $"Money: {money}";
    }

    // 元スプライトから新しいテクスチャを複製して返す
    Sprite CreateSpriteCopy(Sprite source, out Texture2D tex)
    {
        Rect r = source.rect;
        tex = new Texture2D((int)r.width, (int)r.height, TextureFormat.ARGB32, false);
        Color[] pixels = source.texture.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), source.pixelsPerUnit);
    }

    // 所持金を加算
    public void AddMoney(int amount)
    {
        money += amount;
        moneyUpdate();
    }

    void BuildStageTexture()
    {
        stageWidth = width;
        stageHeight = height;

        stageTexture = new Texture2D(stageWidth, stageHeight, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        stagePixels = new Color32[stageWidth * stageHeight];
        layerIndexMap = new int[stagePixels.Length];
        durabilityMap = new float[stagePixels.Length];

        // 茶色で塗りつぶす（R=139,G=69,B=19）
        Color32 soilColor = new Color32(139, 69, 19, 255);
        for (int i = 0; i < stagePixels.Length; i++)
        {
            stagePixels[i] = soilColor;
            layerIndexMap[i] = 0;
            durabilityMap[i] = 1f; // 掘削できるように初期耐久を設定
        }

        stageTexture.SetPixels32(stagePixels);
        stageTexture.Apply();

        // レイヤー情報はダミー1個だけ
        layerInfos = new StageLayerInfo[]
        {
        new StageLayerInfo("Soil", 0, stageHeight, 1, 1f)
        };
    }


    // 有効なレイヤー情報を抽出
    StageLayerDefinition[] GetEffectiveLayers()
    {
        List<StageLayerDefinition> list = new List<StageLayerDefinition>();
        if (layerDefinitions != null)
        {
            foreach (StageLayerDefinition definition in layerDefinitions)
            {
                if (definition == null || definition.sprite == null) continue;
                definition.hardness = Mathf.Max(0.1f, definition.hardness);
                list.Add(definition);
            }
        }

        // 空ならデフォルトレイヤーを読み込む
        if (list.Count == 0)
        {
            list.AddRange(LoadDefaultLayerDefinitions());
        }

        return list.ToArray();
    }

    // Resourcesからデフォルトレイヤーをロード
    IEnumerable<StageLayerDefinition> LoadDefaultLayerDefinitions()
    {
        StageLayerDefinition soil = CreateLayerFromResources("layer_soil", "Topsoil", 1, 1f);
        StageLayerDefinition stone = CreateLayerFromResources("layer_stone", "Stone", 2, 2.5f);
        StageLayerDefinition ore = CreateLayerFromResources("layer_ore", "Ore Vein", 5, 3.5f);

        if (soil != null) yield return soil;
        if (stone != null) yield return stone;
        if (ore != null) yield return ore;
    }

    // Resourcesからスプライトをロードしてレイヤー定義を作成
    StageLayerDefinition CreateLayerFromResources(string resourceName, string displayName, int oreValue, float hardness)
    {
        Sprite sprite = Resources.Load<Sprite>($"Stage/{resourceName}");
        if (sprite == null)
        {
            Debug.LogWarning($"Stage layer resource '{resourceName}' could not be found.");
            return null;
        }

        return new StageLayerDefinition
        {
            layerName = displayName,
            sprite = sprite,
            oreValue = oreValue,
            hardness = Mathf.Max(0.1f, hardness)
        };
    }

    // 画面クリック位置から採掘処理を行う
    void MineAtScreenPosition(Vector2 screenPos, Camera cam)
    {
        RectTransform rt = stageImage.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPos, null, out Vector2 localPoint))
            return;

        Rect rect = rt.rect;

        // localPoint を左下基準に直す
        float u = (localPoint.x - rect.xMin) / rect.width;
        float v = (localPoint.y - rect.yMin) / rect.height;

        // テクスチャ座標に変換
        int px = Mathf.Clamp(Mathf.RoundToInt(u * (stageWidth - 1)), 0, stageWidth - 1);
        int py = Mathf.Clamp(Mathf.RoundToInt(v * (stageHeight - 1)), 0, stageHeight - 1);

        MineAtPixel(px, py);
    }

    // ピクセル単位の採掘処理
    void MineAtPixel(int centerX, int centerY)
    {
        if (layerIndexMap == null || durabilityMap == null) return;

        int radius = Mathf.Max(1, miningRadius);
        float radiusSqr = radius * radius;
        int minX = stageWidth, minY = stageHeight, maxX = -1, maxY = -1;
        int earned = 0;

        for (int y = centerY - radius; y <= centerY + radius; y++)
        {
            if (y < 0 || y >= stageHeight) continue;
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                if (x < 0 || x >= stageWidth) continue;
                int dx = x - centerX;
                int dy = y - centerY;
                if (dx * dx + dy * dy > radiusSqr) continue;

                int index = y * stageWidth + x;
                int layerIndex = (index >= 0 && index < layerIndexMap.Length) ? layerIndexMap[index] : -1;
                if (layerIndex < 0 || layerIndex >= layerInfos.Length) continue;

                if (durabilityMap[index] <= 0f)
                    continue;

                // 採掘ダメージを与える
                durabilityMap[index] = Mathf.Max(0f, durabilityMap[index] - miningPower);

                // 耐久度が0になったら破壊して報酬を得る
                if (durabilityMap[index] <= 0f)
                {
                    stagePixels[index] = new Color32(0, 0, 0, 0);
                    layerIndexMap[index] = -1;
                    earned += layerInfos[layerIndex].OreValue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        // 掘削された範囲だけ更新
        if (maxX >= minX && maxY >= minY)
        {
            int updateWidth = maxX - minX + 1;
            int updateHeight = maxY - minY + 1;
            Color32[] segment = new Color32[updateWidth * updateHeight];
            for (int y = 0; y < updateHeight; y++)
            {
                int sourceIndex = (minY + y) * stageWidth + minX;
                System.Array.Copy(stagePixels, sourceIndex, segment, y * updateWidth, updateWidth);
            }
            Debug.LogError($"Updating texture region: ({minX},{minY}) - ({maxX},{maxY})");
            stageTexture.SetPixels32(minX, minY, updateWidth, updateHeight, segment);
            stageTexture.Apply();
        }



        // 報酬加算
        if (earned > 0)
            AddMoney(earned);
    }

    // レイヤー定義（インスペクタに表示可能）
    [System.Serializable]
    public class StageLayerDefinition
    {
        public string layerName;
        public Sprite sprite;
        public int oreValue = 1;
        public float hardness = 1f;
    }

    // レイヤー情報の読み取り専用構造体
    public readonly struct StageLayerInfo
    {
        public StageLayerInfo(string name, float depthStart, float depthEnd, int oreValue, float hardness)
        {
            Name = name;
            DepthStart = depthStart;
            DepthEnd = depthEnd;
            OreValue = oreValue;
            Hardness = hardness;
        }

        public string Name { get; }
        public float DepthStart { get; }
        public float DepthEnd { get; }
        public int OreValue { get; }
        public float Hardness { get; }
    }

    // 外部から読み取り可能なレイヤー情報リスト
    public IReadOnlyList<StageLayerInfo> LayerInfos => layerInfos;
}
