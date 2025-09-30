using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageManger : MonoBehaviour
{
    public static StageManger Instance { get; private set; }

    [SerializeField] int width = 500;
    [SerializeField] int height = 300;
    [SerializeField] Image stageImage;
    [SerializeField] TextMeshProUGUI moneyText;
    [SerializeField] StageLayerDefinition[] layerDefinitions;
    [SerializeField] LayerDepthRule[] layerDepthRule;
    [SerializeField] int miningRadius = 6;
    [SerializeField] float miningPower = 1f;

    StageBuilder builder;
    StageMiner miner;
    int money;

    void Awake()
    {
        Instance = this;

        // ステージ構築
        builder = new StageBuilder(width, height, layerDefinitions, layerDepthRule);
        builder.Build();

        // 画像へ反映
        stageImage.sprite = builder.CreateSprite();
        stageImage.rectTransform.sizeDelta = new Vector2(width, height);


        moneyUpdate();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 採掘担当生成
            miner = new StageMiner(builder, miningRadius, miningPower);
            int earned = miner.MineAtScreenPosition(Input.mousePosition, stageImage, Camera.main);
            if (earned > 0) AddMoney(earned);
        }
    }

    public void AddMoney(int amount)
    {
        money += amount;
        moneyUpdate();
    }

    void moneyUpdate() => moneyText.text = $"{money}";
}


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
[System.Serializable]
public class LayerDepthRule
{
    public int startRow; // 上から何ピクセル目から
    public int endRow;   // 上から何ピクセル目まで
    public LayerProbability[] probabilities;
}
[System.Serializable]
public class LayerProbability
{
    public string layerName;   // layerDefinitions の名前で参照
    public float probability; // 0~1
}