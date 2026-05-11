using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Fusion;

/// <summary>
/// Step 4 修正版 v2：
/// - 撃墜判定は Update()（全クライアントで動く）
/// - リスポーン直後の連続撃墜を防ぐクールダウン
/// - リスポーン位置を SpawnPoint に変更（場外座標への復活を回避）
/// </summary>
public class OnlineGameManager : NetworkBehaviour
{
    public static OnlineGameManager Instance { get; private set; }

    [Header("システム参照")]
    public DynamicCamera dynamicCamera;
    public UIManager uiManager;

    [Header("スポーン位置（シーン内の SpawnPoint オブジェクト）")]
    public Transform spawnPoint1;
    public Transform spawnPoint2;

    [Header("撃墜ラインの設定")]
    public float limitLeft = -20f;
    public float limitRight = 20f;
    public float limitUp = 15f;
    public float limitDown = -10f;

    [Header("リスポーン直後の無敵時間（秒）")]
    public float respawnInvulnerability = 1.5f;

    [Header("リザルトシーン名（Build Settings に登録）")]
    public string resultSceneName = "OnlineResultScene";

    [Networked] public int WinnerPlayerNum { get; set; }

    private Transform player1;
    private Transform player2;
    private bool transitioning = false;

    // 連続撃墜防止のためのクールダウン（プレイヤーごと）
    private float p1KillCooldownEnd = 0f;
    private float p2KillCooldownEnd = 0f;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void RegisterPlayer(Transform player, bool isP1)
    {
        if (isP1) player1 = player;
        else player2 = player;

        Debug.Log($"[OnlineGameManager] Registered {(isP1 ? "P1" : "P2")}: {player.name}");

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

    void Update()
    {
        if (transitioning) return;

        if (player1 != null) CheckBlastZone(player1, true);
        if (player2 != null) CheckBlastZone(player2, false);

        if (Object != null && Object.IsValid && WinnerPlayerNum != 0)
        {
            transitioning = true;
            TriggerGameOver();
        }
    }

    private void CheckBlastZone(Transform player, bool isP1)
    {
        var no = player.GetComponent<NetworkObject>();
        if (no == null || !no.HasStateAuthority) return;

        // Inspector で 0 にされていた場合の保険
        float invulnTime = respawnInvulnerability > 0f ? respawnInvulnerability : 1.5f;

        // ★ クールダウン中は判定スキップ
        float now = Time.time;
        if (isP1 && now < p1KillCooldownEnd) return;
        if (!isP1 && now < p2KillCooldownEnd) return;

        Vector3 pos = player.position;
        if (pos.x < limitLeft || pos.x > limitRight || pos.y > limitUp || pos.y < limitDown)
        {
            // ★ クールダウン開始
            if (isP1) p1KillCooldownEnd = now + invulnTime;
            else p2KillCooldownEnd = now + invulnTime;

            OnPlayerKilled(player, isP1);
        }
    }

    private void OnPlayerKilled(Transform player, bool isP1)
    {
        var status = player.GetComponent<PlayerStatus>();
        if (status == null) return;

        Debug.Log($"[OnlineGameManager] {player.name} 撃墜！残スト: {status.currentStock - 1}");
        status.OnKilled();

        if (status.currentStock <= 0)
        {
            int winner = isP1 ? 2 : 1;
            Debug.Log($"[OnlineGameManager] {player.name} のストックが尽きた → Winner = P{winner}");
            RPC_SetWinner(winner);
        }
        else
        {
            // ★ リスポーン位置：SpawnPoint があればそちら、無ければ画面中央上空
            Transform sp = isP1 ? spawnPoint1 : spawnPoint2;
            Vector3 respawnPos = (sp != null) ? sp.position : new Vector3(0, 0f, 0);

            // 1) Rigidbody2D.position を直接動かす（物理空間でテレポート）
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.position = respawnPos;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            // 2) Transform も明示的に更新
            player.position = respawnPos;

            // 3) NetworkTransform.Teleport() でネットワーク的にもテレポートを通知
            var nt = player.GetComponent<Fusion.NetworkTransform>();
            if (nt != null)
            {
                nt.Teleport(respawnPos);
            }

            Debug.Log($"[OnlineGameManager] {player.name} リスポーン → {respawnPos}");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetWinner(int winner)
    {
        if (WinnerPlayerNum != 0) return;
        WinnerPlayerNum = winner;
    }

    private void TriggerGameOver()
    {
        Debug.Log($"[OnlineGameManager] Game Over! Winner = P{WinnerPlayerNum}");

        if (GameDataContainer.instance != null && player1 != null && player2 != null)
        {
            GameDataContainer.instance.winnerPlayerNum = WinnerPlayerNum;

            var p1Status = player1.GetComponent<PlayerStatus>();
            var p2Status = player2.GetComponent<PlayerStatus>();
            if (p1Status != null)
            {
                GameDataContainer.instance.p1Icon = p1Status.faceIcon;
                GameDataContainer.instance.p1Name = p1Status.playerName;
            }
            if (p2Status != null)
            {
                GameDataContainer.instance.p2Icon = p2Status.faceIcon;
                GameDataContainer.instance.p2Name = p2Status.playerName;
            }
        }

        if (Runner != null && Runner.IsSharedModeMasterClient)
        {
            StartCoroutine(WaitAndLoadResult());
        }
    }

    private IEnumerator WaitAndLoadResult()
    {
        yield return new WaitForSeconds(1.5f);
        int idx = SceneUtility.GetBuildIndexByScenePath(resultSceneName);
        if (idx < 0)
        {
            Debug.LogError($"Result scene not in Build Settings: {resultSceneName}");
            yield break;
        }
        Runner.LoadScene(SceneRef.FromIndex(idx));
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