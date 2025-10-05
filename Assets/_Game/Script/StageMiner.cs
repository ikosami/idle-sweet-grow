using UnityEngine;

public class StageMiner
{
    StageBuilder builder;
    int stageWidth;
    int stageHeight;
    int miningRadius;
    float miningPower;

    public StageMiner(StageBuilder builder, int radius, float power)
    {
        this.builder = builder;
        stageWidth = builder.StageTexture.width;
        stageHeight = builder.StageTexture.height;
        miningRadius = radius;
        miningPower = power;
    }
    public int MineAtWorldPosition(Vector3 worldPos)
    {
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, worldPos);
        return MineAtScreenPosition(screenPos);
    }
    public int MineAtScreenPosition(Vector2 screenPos)
    {
        var stageImage = builder.Image;
        RectTransform rt = stageImage.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPos, null, out Vector2 localPoint))
            return 0;

        Rect rect = rt.rect;
        float u = (localPoint.x - rect.xMin) / rect.width;
        float v = (localPoint.y - rect.yMin) / rect.height;

        int px = Mathf.Clamp(Mathf.RoundToInt(u * (stageWidth - 1)), 0, stageWidth - 1);
        int py = Mathf.Clamp(Mathf.RoundToInt(v * (stageHeight - 1)), 0, stageHeight - 1);

        return MineAtPixel(px, py);
    }

    public int MineAtPixel(int centerX, int centerY)
    {
        var pixels = builder.StagePixels;
        var durability = builder.DurabilityMap;
        var layerIndexMap = builder.LayerIndexMap;
        var layerInfos = builder.LayerInfos;
        var tex = builder.StageTexture;

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

                if (durability[index] <= 0f) continue;

                durability[index] = Mathf.Max(0f, durability[index] - miningPower);

                if (durability[index] <= 0f)
                {
                    pixels[index] = new Color32(0, 0, 0, 0);
                    layerIndexMap[index] = -1;
                    earned += layerInfos[layerIndex].OreValue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (maxX >= minX && maxY >= minY)
        {
            int updateWidth = maxX - minX + 1;
            int updateHeight = maxY - minY + 1;
            Color32[] segment = new Color32[updateWidth * updateHeight];
            for (int y = 0; y < updateHeight; y++)
            {
                int sourceIndex = (minY + y) * stageWidth + minX;
                System.Array.Copy(pixels, sourceIndex, segment, y * updateWidth, updateWidth);
            }
            tex.SetPixels32(minX, minY, updateWidth, updateHeight, segment);
            tex.Apply();
        }

        return earned;
    }
}
