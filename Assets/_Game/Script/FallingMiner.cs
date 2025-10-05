using UnityEngine;
using UnityEngine.UI;

public class FallingMiner : MonoBehaviour
{
    public float gravity = -9.8f;
    public Vector2 velocity;
    public float miningPower = 1f;
    public int miningRadius = 1;

    RectTransform rectTransform;
    StageBuilder builder;
    StageMiner miner;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        builder = StageManager.Instance.builder;
        miner = new StageMiner(builder, miningRadius, miningPower);
    }

    void Update()
    {
        ApplyGravity();
        HandleFalling();
        Mine();
    }

    /// <summary>
    /// 重力を速度に加算
    /// </summary>
    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
    }

    /// <summary>
    /// 垂直方向（落下）のみの物理移動を処理
    /// </summary>
    void HandleFalling()
    {
        float dt = Time.deltaTime;

        // 落下方向のみの移動量
        Vector2 moveDelta = new Vector2(0, velocity.y * dt);

        // 共通の移動処理を呼び出す
        bool collided = MoveCharacter(moveDelta);

        // 地面衝突で落下停止
        if (collided)
            velocity.y = 0f;
    }

    /// <summary>
    /// Canvas座標とステージピクセル座標を相互変換しつつキャラを移動させる
    /// 衝突がある場合は true を返す
    /// </summary>
    bool MoveCharacter(Vector2 moveDelta)
    {
        RectTransform stageRT = builder.Image.rectTransform;
        Rect stageRect = stageRT.rect;

        // 現在位置をステージローカル座標に変換
        Vector2 charLocalOnStage = stageRT.InverseTransformPoint(rectTransform.position);

        // ローカル→UV
        float u = (charLocalOnStage.x - stageRect.xMin) / stageRect.width;
        float v = (charLocalOnStage.y - stageRect.yMin) / stageRect.height;

        // UV→ピクセル
        float px = u * (builder.StageTexture.width - 1);
        float py = v * (builder.StageTexture.height - 1);
        float pw = rectTransform.sizeDelta.x / stageRect.width * builder.StageTexture.width;
        float ph = rectTransform.sizeDelta.y / stageRect.height * builder.StageTexture.height;

        // 移動量をピクセル換算
        Vector2 velPx;
        velPx.x = moveDelta.x / stageRect.width * builder.StageTexture.width;
        velPx.y = moveDelta.y / stageRect.height * builder.StageTexture.height;

        // 衝突判定
        Rect charRectPx = new Rect(px, py, pw, ph);
        Vector2 newPosPx = builder.MoveCharacterOptimized(charRectPx, velPx);

        // ピクセル→UV→Canvas座標
        float newU = newPosPx.x / (builder.StageTexture.width - 1);
        float newV = newPosPx.y / (builder.StageTexture.height - 1);
        Vector2 newLocalOnStage = new Vector2(
            Mathf.Lerp(stageRect.xMin, stageRect.xMax, newU),
            Mathf.Lerp(stageRect.yMin, stageRect.yMax, newV)
        );

        // 新しいワールド位置を反映
        rectTransform.position = stageRT.TransformPoint(newLocalOnStage);

        // 衝突判定（移動前後のY座標が同じなら停止）
        return Mathf.Approximately(newPosPx.y, py);
    }

    /// <summary>
    /// 現在位置の真下を掘る
    /// </summary>
    void Mine()
    {
        RectTransform stageRT = builder.Image.rectTransform;
        Rect stageRect = stageRT.rect;

        Vector2 charLocalOnStage = stageRT.InverseTransformPoint(rectTransform.position);
        Vector2 bottomCenterLocal = charLocalOnStage + new Vector2(0f, -rectTransform.sizeDelta.y * 0.5f);
        Vector3 worldBottom = stageRT.TransformPoint(bottomCenterLocal);

        miner.MineAtWorldPosition(worldBottom);
    }
}
