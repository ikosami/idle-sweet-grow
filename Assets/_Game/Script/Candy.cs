using UnityEngine;
using UnityEngine.UI;

public class Candy : MonoBehaviour
{
    Texture2D texture;
    Image image;
    bool rising;

    public void Initialize(Texture2D tex, Image img)
    {
        texture = tex;
        image = img;
        var rt = image.rectTransform;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -rt.rect.height);
        rising = true;
    }

    public void Erase(Vector2 screenPos, Camera cam)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform, screenPos, cam, out var local);

        Rect rect = image.rectTransform.rect;
        int px = Mathf.RoundToInt((local.x + rect.width * 0.5f) / rect.width * texture.width);
        int py = Mathf.RoundToInt(local.y / rect.height * texture.height);

        for (int j = -5; j <= 5; j++)
            for (int i = -5; i <= 5; i++)
            {
                int x = px + i;
                int y = py + j;
                if (0 <= x && x < texture.width && 0 <= y && y < texture.height)
                    texture.SetPixel(x, y, Color.clear);
            }

        texture.Apply();

        StageManger.Instance?.AddMoney(1);
    }

    void Update()
    {
        if (!rising) return;
        var rt = image.rectTransform;
        float y = rt.anchoredPosition.y + 1f;
        if (y >= 0f)
        {
            y = 0f;
            rising = false;
        }
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
    }
}
