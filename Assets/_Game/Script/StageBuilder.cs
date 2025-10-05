using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class StageBuilder
{
    int width;
    int height;
    StageLayerDefinition[] layerDefinitions;
    public LayerDepthRule[] depthRules;

    public Image Image { get; private set; }
    public Texture2D StageTexture { get; private set; }
    public Color32[] StagePixels { get; private set; }
    public int[] LayerIndexMap { get; private set; }
    public float[] DurabilityMap { get; private set; }
    public StageLayerInfo[] LayerInfos { get; private set; }

    public StageBuilder(Image image, int width, int height, StageLayerDefinition[] defs, LayerDepthRule[] depthRules)
    {
        this.Image = image;
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

    public Vector2 MoveCharacterOptimized(Rect charRect, Vector2 velocity)
    {
        Vector2 newPos = new Vector2(charRect.x, charRect.y);

        // 移動距離からステップ数（1px単位）
        float distance = velocity.magnitude;
        int steps = Mathf.CeilToInt(distance);
        if (steps == 0) steps = 1;

        Vector2 step = velocity / steps;

        for (int i = 0; i < steps; i++)
        {
            // ---- X方向移動 ----
            if (step.x != 0)
            {
                newPos.x += step.x;
                int checkX = step.x > 0 ?
                    Mathf.FloorToInt(newPos.x + charRect.width - 0.001f) :
                    Mathf.FloorToInt(newPos.x);

                int topY = Mathf.CeilToInt(newPos.y + charRect.height - 0.001f);
                int midY = Mathf.FloorToInt(newPos.y + charRect.height / 2f);
                int bottomY = Mathf.FloorToInt(newPos.y);

                // 角と中央優先チェック
                if (IsSolid(checkX, bottomY) || IsSolid(checkX, midY) || IsSolid(checkX, topY))
                {
                    newPos.x = step.x > 0 ? checkX - charRect.width : checkX + 1f;
                    step.x = 0;
                }
            }

            // ---- Y方向移動 ----
            if (step.y != 0)
            {
                newPos.y += step.y;
                int checkY = step.y > 0 ?
                    Mathf.FloorToInt(newPos.y + charRect.height - 0.001f) :
                    Mathf.FloorToInt(newPos.y);

                int leftX = Mathf.FloorToInt(newPos.x);
                int midX = Mathf.FloorToInt(newPos.x + charRect.width / 2f);
                int rightX = Mathf.CeilToInt(newPos.x + charRect.width - 0.001f);

                // 角と中央優先チェック
                if (IsSolid(leftX, checkY) || IsSolid(midX, checkY) || IsSolid(rightX, checkY))
                {
                    newPos.y = step.y > 0 ? checkY - charRect.height : checkY + 1f;
                    step.y = 0;
                }
            }
        }

        return newPos;
    }

    bool IsSolid(int px, int py)
    {
        if (px < 0 || py < 0 || px >= width || py >= height) return true;
        int index = py * width + px;
        return StagePixels[index].a > 0;
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