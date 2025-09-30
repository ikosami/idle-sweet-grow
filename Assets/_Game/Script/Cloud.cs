using UnityEngine;

public class Cloud : MonoBehaviour
{
    public RectTransform Rect;
    [SerializeField] float speed = 10;

    private void Start()
    {
        speed *= Random.Range(0.8f, 1.2f);
    }

    void Update()
    {
        Rect.anchoredPosition += Vector2.left * speed * Time.deltaTime;

        if (Rect.anchoredPosition.x < -50)
        {
            Destroy(gameObject);
        }
    }
}
