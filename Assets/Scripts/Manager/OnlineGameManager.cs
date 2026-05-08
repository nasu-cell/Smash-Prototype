using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Step 1 版：プレイヤーの生成は NetworkLauncher が行うので、ここではしない。
/// 代わりに、生成されたプレイヤーが自分自身を RegisterPlayer() で登録してくる。
/// 2 人揃ったらカメラと UI に対象を渡す。
/// 撃墜判定・リザルト遷移は Step 4 で再実装するため、本ファイルでは一旦コメントアウト。
/// </summary>
public class OnlineGameManager : MonoBehaviour
{
    public static OnlineGameManager Instance { get; private set; }

    [Header("システム参照")]
    public DynamicCamera dynamicCamera;
    public UIManager uiManager;

    [Header("スポーン位置（シーン内の SpawnPoint オブジェクト）")]
    public Transform spawnPoint1;
    public Transform spawnPoint2;

    [Header("撃墜ラインの設定（Step 4 で使用）")]
    public float limitLeft = -20f;
    public float limitRight = 20f;
    public float limitUp = 15f;
    public float limitDown = -10f;

    private Transform player1;
    private Transform player2;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// NetworkPlayerRegistrar が Spawned() 時に呼んでくる。
    /// isP1 = true なら 1P スロットへ、false なら 2P スロットへ登録。
    /// </summary>
    public void RegisterPlayer(Transform player, bool isP1)
    {
        if (isP1) player1 = player;
        else player2 = player;

        Debug.Log($"[OnlineGameManager] Registered {(isP1 ? "P1" : "P2")}: {player.name}");

        // 両方揃ったらカメラ/UI に通知
        if (player1 != null && player2 != null)
        {
            if (dynamicCamera != null) dynamicCamera.SetTargets(player1, player2);

            if (uiManager != null)
            {
                var p1Status = player1.GetComponent<PlayerStatus>();
                var p2Status = player2.GetComponent<PlayerStatus>();
                if (p1Status != null) uiManager.SetPlayerStatus(1, p1Status);
                if (p2Status != null) uiManager.SetPlayerStatus(2, p2Status);
            }
        }
    }

    // === 以下は Step 4 でネットワーク版に書き換える予定。一旦無効化 ===
    /*
    void Update()
    {
        if (player1 != null) CheckBlastZone(player1, "Player 1");
        if (player2 != null) CheckBlastZone(player2, "Player 2");
    }

    void CheckBlastZone(Transform player, string playerName) { ... }
    void OnPlayerKilled(Transform player, string playerName) { ... }
    private IEnumerator WaitAndTransition() { ... }
    */

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