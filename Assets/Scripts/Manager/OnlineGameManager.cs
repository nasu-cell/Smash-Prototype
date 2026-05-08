using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class OnlineGameManager : MonoBehaviour
{
    [Header("スポーン位置")]
    public Transform spawnPoint1;
    public Transform spawnPoint2;

    [Header("プレハブ設定")]
    public GameObject[] characterPrefabs;

    [Header("システム参照")]
    public DynamicCamera dynamicCamera;
    public UIManager uiManager;

    [Header("撃墜ラインの設定")]
    public float limitLeft = -20f;
    public float limitRight = 20f;
    public float limitUp = 15f;
    public float limitDown = -10f;

    // 生成されたプレイヤーの参照を保持
    private Transform player1;
    private Transform player2;

    void Start()
    {
        SpawnPlayers();
    }

    void Update()
    {
        // プレイヤーが生成されていれば撃墜判定を行う
        if (player1 != null) CheckBlastZone(player1, "Player 1");
        if (player2 != null) CheckBlastZone(player2, "Player 2");
    }

    void SpawnPlayers()
    {
        if (GameDataContainer.instance == null) return;

        // 1. 自分の生成 (1P側)
        int myID = GameDataContainer.instance.mySelectedCharID;
        GameObject myObj = Instantiate(characterPrefabs[myID], spawnPoint1.position, Quaternion.identity);
        myObj.GetComponent<ActorController>().isMine = true;
        player1 = myObj.transform;

        // 2. 相手の生成 (2P側)
        GameObject enemyObj = Instantiate(characterPrefabs[0], spawnPoint2.position, Quaternion.identity);
        enemyObj.GetComponent<ActorController>().isMine = false;
        player2 = enemyObj.transform;

        // 3. 各システムへ通知
        dynamicCamera.SetTargets(player1, player2);
        uiManager.SetPlayerStatus(1, myObj.GetComponent<PlayerStatus>());
        uiManager.SetPlayerStatus(2, enemyObj.GetComponent<PlayerStatus>());
    }

    void CheckBlastZone(Transform player, string playerName)
    {
        Vector3 pos = player.position;

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

            // OnPlayerKilled メソッド内の status.currentStock <= 0 の処理中
            if (status.currentStock <= 0)
            {
                Debug.Log("GAME OVER");

                if (GameDataContainer.instance != null)
                {
                    // 1. 勝敗を記録
                    GameDataContainer.instance.winnerPlayerNum = (player == player1) ? 2 : 1;

                    // 2. キャラクターのStatusから「画像」と「名前」を抜き出してContainerへ保存
                    PlayerStatus p1Status = player1.GetComponent<PlayerStatus>();
                    PlayerStatus p2Status = player2.GetComponent<PlayerStatus>();

                    GameDataContainer.instance.p1Icon = p1Status.faceIcon;
                    GameDataContainer.instance.p1Name = p1Status.playerName;
                    GameDataContainer.instance.p2Icon = p2Status.faceIcon;
                    GameDataContainer.instance.p2Name = p2Status.playerName;
                }

                StartCoroutine(WaitAndTransition());
                return;
            }
        }
        // リスポーン（とりあえず中央上空へ）
        player.position = new Vector3(0, 5f, 0);
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    private IEnumerator WaitAndTransition()
    {
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene("OnlineResultScene");
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(limitLeft, limitUp, 0), new Vector3(limitRight, limitUp, 0));
        Gizmos.DrawLine(new Vector3(limitRight, limitUp, 0), new Vector3(limitRight, limitDown, 0));
        Gizmos.DrawLine(new Vector3(limitRight, limitDown, 0), new Vector3(limitLeft, limitDown, 0));
        Gizmos.DrawLine(new Vector3(limitLeft, limitDown, 0), new Vector3(limitLeft, limitUp, 0));
    }
#endif
}