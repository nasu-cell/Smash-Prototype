using UnityEngine;

public class DynamicCamera : MonoBehaviour
{
    [Header("追従対象")]
    public Transform player1;
    public Transform player2;

    [Header("ズーム設定")]
    public float minSize;      // 最も寄ったときのサイズ
    public float maxSize;     // 最も引いたときのサイズ
    public float zoomLimiter; // ズームの感度（大きいほど引きにくくなる）

    [Header("スムース設定")]
    public float smoothTime;
    private Vector3 velocity;
    private Camera cam;

    [Header("カメラの限界値")]
     // GameManagerで設定した撃墜ラインより少し内側で止まるようにする
    public float camLimitX; // ステージに合わせて調整
    public float camLimitY;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate() // プレイヤーの移動確定後にカメラを動かすためLateUpdate推奨
    {
        if (player1 == null || player2 == null) return;

        Move();
        Zoom();
    }

    public void SetTargets(Transform p1, Transform p2)
    {
        player1 = p1;
        player2 = p2;
    }

    void Move()
    {
        // 1. 二人の中間地点を計算
        Vector3 centerPoint = (player1.position + player2.position) / 2f;
        // --- カメラの制限を追加 ---

        centerPoint.x = Mathf.Clamp(centerPoint.x, -camLimitX, camLimitX);
        centerPoint.y = Mathf.Clamp(centerPoint.y, -camLimitY, camLimitY);
        // -------------------------
        
        // Z座標はカメラ自身のものを維持
        centerPoint.z = transform.position.z;

        // 2. 滑らかに移動
        transform.position = Vector3.SmoothDamp(transform.position, centerPoint, ref velocity, smoothTime);
    }

    void Zoom()
    {
        // 1. 二人の距離を測る
        float distance = Vector2.Distance(player1.position, player2.position);

        // 2. 距離に基づいてズームサイズを計算
        // 距離をzoomLimiterで割り、minSizeを足すことで可変させる
        float newSize = Mathf.Lerp(minSize, maxSize, distance / zoomLimiter);

        // 3. 滑らかにズーム（2DのOrthographicカメラ想定）
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, newSize, Time.deltaTime * 5f);
    }
}