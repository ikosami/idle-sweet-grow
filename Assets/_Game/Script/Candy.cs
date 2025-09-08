using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Candy : MonoBehaviour, IPointerDownHandler
{
    Texture2D texture;
    Image image;

    public void Initialize(Texture2D tex, Image img)
    {
        texture = tex;
        image = img;
    }

    public void OnPointerDown(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform, e.position, e.pressEventCamera, out var local);

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
}
