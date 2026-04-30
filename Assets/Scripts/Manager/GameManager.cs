using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("撃墜ラインの設定")]
    public float limitLeft = -20f;
    public float limitRight = 20f;
    public float limitUp = 15f;
    public float limitDown = -10f;

    [Header("対象プレイヤー")]
    public Transform player1;
    public Transform player2;

    void Awake()
    {
        instance = this;
    }

    void Update()
    {
        if (player1 != null) CheckBlastZone(player1, "Player 1");
        if (player2 != null) CheckBlastZone(player2, "Player 2");
    }

    void CheckBlastZone(Transform player, string playerName)
    {
        Vector3 pos = player.position;

        // 四方のラインを一つでも超えたら撃墜
        if (pos.x < limitLeft || pos.x > limitRight || pos.y > limitUp || pos.y < limitDown)
        {
            OnPlayerKilled(player, playerName);
        }
    }

    void OnPlayerKilled(Transform player, string playerName)
    {
        Debug.Log(playerName + " が撃墜されました！");

        PlayerStatus status = player.GetComponent<PlayerStatus>();
        
        if (status != null)
        {
            status.currentStock--;      // ストック減少（UIに自動反映）
            status.totalDamage = 0f;    // ダメージリセット
            status.isFallingHelpless = false; // 状態異常リセット
            
            if (status.currentStock <= 0)
            {
                Debug.Log("GAME OVER");
                // ここに勝利リザルトへの遷移などを追加
            }
        }

        // 物理リセットとリスポーン
        player.position = Vector3.up * 5f; // 空中から出現
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 p1 = new Vector3(limitLeft, limitUp, 0);
        Vector3 p2 = new Vector3(limitRight, limitUp, 0);
        Vector3 p3 = new Vector3(limitRight, limitDown, 0);
        Vector3 p4 = new Vector3(limitLeft, limitDown, 0);

        Gizmos.DrawLine(p1, p2); Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4); Gizmos.DrawLine(p4, p1);
    }
#endif
}