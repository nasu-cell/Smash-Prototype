using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
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

    void Start()
    {
        // --- 追加：ローカルプレイ時に操作権を有効化する ---
        if (player1 != null)
        {
            var controller = player1.GetComponent<ActorController>();
            if (controller != null) controller.isMine = true;
        }

        // 2Pもキーボードで動かしたい場合は true に、動かしたくない場合は false に
        if (player2 != null)
        {
            var controller = player2.GetComponent<ActorController>();
            if (controller != null) controller.isMine = true;
        }
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
            status.currentStock--;
            status.totalDamage = 0f;
            status.isFallingHelpless = false;

            if (status.currentStock <= 0)
            {
                Debug.Log("GAME OVER");

                // --- ここから共通の保存処理 ---
                if (GameDataContainer.instance != null)
                {
                    // 1. 勝敗を記録（倒れたのが player1 なら勝者は 2）
                    GameDataContainer.instance.winnerPlayerNum = (player == player1) ? 2 : 1;

                    // 2. プレイヤーたちのステータスを取得して情報をコピー
                    // ※ player1, player2 という変数が定義されている必要があります
                    PlayerStatus p1Status = player1.GetComponent<PlayerStatus>();
                    PlayerStatus p2Status = player2.GetComponent<PlayerStatus>();

                    if (p1Status != null && p2Status != null)
                    {
                        GameDataContainer.instance.p1Icon = p1Status.faceIcon;
                        GameDataContainer.instance.p1Name = p1Status.playerName;
                        GameDataContainer.instance.p2Icon = p2Status.faceIcon;
                        GameDataContainer.instance.p2Name = p2Status.playerName;
                    }
                }

                StartCoroutine(WaitAndTransition());
                return;
            }
        }

        // リスポーン処理（共通の場所）
        player.position = new Vector3(0, 5f, 0);
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    // GameManager.cs
    private IEnumerator WaitAndTransition()
    {
        yield return new WaitForSeconds(1.5f); // 決着後の余韻
        SceneManager.LoadScene("ResultScene");
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