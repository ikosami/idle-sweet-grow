using System.Linq;
using UnityEngine;

public class StageBuilder
{
    int width;
    int height;
    StageLayerDefinition[] layerDefinitions;
    public LayerDepthRule[] depthRules;

    public Texture2D StageTexture { get; private set; }
    public Color32[] StagePixels { get; private set; }
    public int[] LayerIndexMap { get; private set; }
    public float[] DurabilityMap { get; private set; }
    public StageLayerInfo[] LayerInfos { get; private set; }

    public StageBuilder(int width, int height, StageLayerDefinition[] defs, LayerDepthRule[] depthRules)
    {
        this.width = width;
        this.height = height;
        this.layerDefinitions = defs;
        this.depthRules = depthRules;
    }

    public void Build()
    {
        StageTexture = new Texture2D(width, height, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        StagePixels = new Color32[width * height];
        LayerIndexMap = new int[StagePixels.Length];
        DurabilityMap = new float[StagePixels.Length];

        // 初期化（0番レイヤーで塗りつぶす）
        StageLayerDefinition defaultLayer = layerDefinitions[0];
        Color32[] defaultPixels = defaultLayer.sprite.texture.GetPixels32();
        int texW = defaultLayer.sprite.texture.width;
        int texH = defaultLayer.sprite.texture.height;
        int tileSize = 10; // タイルサイズ
        for (int ty = 0; ty < height; ty += tileSize)
        {
            for (int tx = 0; tx < width; tx += tileSize)
            {
                // タイル座標
                int tileY = (height - ty - tileSize) / tileSize + 1;

                // タイル単位でレイヤーを選択
                StageLayerDefinition selectedLayer = defaultLayer;
                LayerDepthRule rule = depthRules.FirstOrDefault(r => tileY >= r.startRow && tileY <= r.endRow);
                if (rule != null && rule.probabilities.Length > 0)
                {
                    // 重み付き選択
                    float total = rule.probabilities.Sum(p => p.probability);

                    // 全部0の場合は均等にする
                    if (total <= 0f) total = rule.probabilities.Length;

                    float r = Random.value * total;
                    float sum = 0f;

                    foreach (var lp in rule.probabilities)
                    {
                        float weight = lp.probability > 0f ? lp.probability : 1f; // 0なら均等
                        sum += weight;
                        if (r <= sum)
                        {
                            selectedLayer = layerDefinitions.First(d => d.layerName == lp.layerName);
                            break;
                        }
                    }
                }

                // タイル内のピクセルをコピー
                int rotation = 0;
                if (selectedLayer.CanRotate)
                {
                    int[] rotations = new int[] { 0, 90, 180, 270 };
                    rotation = rotations[Random.Range(0, rotations.Length)];
                }
                for (int y = 0; y < tileSize; y++)
                {
                    for (int x = 0; x < tileSize; x++)
                    {
                        int stageX = tx + x;
                        int stageY = ty + y;
                        if (stageX >= width || stageY >= height) continue;

                        int pIndex;
                        switch (rotation)
                        {
                            case 90:
                                pIndex = (tileSize - 1 - x) + y * tileSize;
                                break;
                            case 180:
                                pIndex = (tileSize - 1 - y) * tileSize + (tileSize - 1 - x);
                                break;
                            case 270:
                                pIndex = x + (tileSize - 1 - y) * tileSize;
                                break;
                            default: // 0度
                                pIndex = y * tileSize + x;
                                break;
                        }

                        StagePixels[stageY * width + stageX] = selectedLayer.sprite.texture.GetPixels32()[pIndex];
                        LayerIndexMap[stageY * width + stageX] = System.Array.IndexOf(layerDefinitions, selectedLayer);
                        DurabilityMap[stageY * width + stageX] = selectedLayer.hardness;
                    }
                }
            }
        }

        StageTexture.SetPixels32(StagePixels);
        StageTexture.Apply();

        LayerInfos = new StageLayerInfo[layerDefinitions.Length];
        for (int i = 0; i < layerDefinitions.Length; i++)
        {
            var def = layerDefinitions[i];
            LayerInfos[i] = new StageLayerInfo(def.layerName, i, height, def.oreValue, def.hardness);
        }
    }

    public Sprite CreateSprite()
    {
        return Sprite.Create(StageTexture, new Rect(0, 0, StageTexture.width, StageTexture.height),
                             new Vector2(0.5f, 0.5f));
    }
}


[System.Serializable]
public class StageLayerDefinition
{
    public string layerName;
    public Sprite sprite;
    public float hardness = 1f;
    public int oreValue = 1;
    public float spawnChance = 0f;
    public bool CanRotate = false;
}