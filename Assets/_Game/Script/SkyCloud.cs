using UnityEngine;
using UnityEngine.UI;

public class SkyCloud : MonoBehaviour
{
    [SerializeField] Cloud[] prefabs;
    [SerializeField] RectTransform parent;

    private float timer = 0f;
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private int initialCloudCount = 5; // 開始時の雲の数

    private void Start()
    {
        for (int i = 0; i < initialCloudCount; i++)
        {
            SpawnCloud(Random.Range(0, parent.rect.width));
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnCloud(parent.rect.width + 50); // 右端から出現
            timer = 0f;
        }
    }

    private void SpawnCloud(float x)
    {
        if (prefabs.Length == 0) return;

        Cloud prefab = prefabs[Random.Range(0, prefabs.Length)];
        Cloud instance = Instantiate(prefab, parent);

        float y = Random.Range(parent.rect.height / 2, parent.rect.height);
        instance.Rect.anchoredPosition = new Vector2(x, y);
    }
}
