using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Fusion;

/// <summary>
/// Step 5 版：
/// - 相手が切断したら不戦勝でリザルトへ
/// - リスポーン処理を rb.position + NetworkTransform.Teleport で確実に
/// - クールダウンに保険値あり
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

    private float p1KillCooldownEnd = 0f;
    private float p2KillCooldownEnd = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 相手切断のイベントを購読
        if (NetworkLauncher.Instance != null)
        {
            NetworkLauncher.Instance.OnOpponentDisconnected += HandleOpponentDisconnected;
        }
    }

    void OnDestroy()
    {
        if (NetworkLauncher.Instance != null)
        {
            NetworkLauncher.Instance.OnOpponentDisconnected -= HandleOpponentDisconnected;
        }
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
            TriggerGameOver(viaNetworkSync: true);
        }
    }

    private void CheckBlastZone(Transform player, bool isP1)
    {
        var no = player.GetComponent<NetworkObject>();
        if (no == null || !no.HasStateAuthority) return;

        float invulnTime = respawnInvulnerability > 0f ? respawnInvulnerability : 1.5f;

        float now = Time.time;
        if (isP1 && now < p1KillCooldownEnd) return;
        if (!isP1 && now < p2KillCooldownEnd) return;

        Vector3 pos = player.position;
        if (pos.x < limitLeft || pos.x > limitRight || pos.y > limitUp || pos.y < limitDown)
        {
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
            Transform sp = isP1 ? spawnPoint1 : spawnPoint2;
            Vector3 respawnPos = (sp != null) ? sp.position : new Vector3(0, 0f, 0);

            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.position = respawnPos;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            player.position = respawnPos;

            var nt = player.GetComponent<Fusion.NetworkTransform>();
            if (nt != null) nt.Teleport(respawnPos);

            Debug.Log($"[OnlineGameManager] {player.name} リスポーン → {respawnPos}");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetWinner(int winner)
    {
        if (WinnerPlayerNum != 0) return;
        WinnerPlayerNum = winner;
    }

    /// <summary>
    /// 相手切断時のハンドラー：自分の勝ちにしてローカルでリザルトへ遷移。
    /// 相手がいないので Runner.LoadScene は使えない（自分だけで遷移）。
    /// </summary>
    private void HandleOpponentDisconnected()
    {
        if (transitioning) return;
        transitioning = true;

        bool myIsP1 = (GameDataContainer.instance != null) && GameDataContainer.instance.isP1;
        int winner = myIsP1 ? 1 : 2;

        Debug.Log($"[OnlineGameManager] 相手が切断 → 不戦勝 (P{winner})");

        SaveGameOverInfo(winner);

        // ★ コルーチンは NetworkLauncher（DDOL）で実行。
        //   Shutdown 時に OnlineGameManager が破棄されてもシーン遷移は走り切る。
        if (NetworkLauncher.Instance != null)
        {
            NetworkLauncher.Instance.StartCoroutine(
                NetworkLauncher.Instance.ShutdownAndLoadScene(resultSceneName, 1.5f)
            );
        }
        else
        {
            SceneManager.LoadScene(resultSceneName);
        }
    }

    private void TriggerGameOver(bool viaNetworkSync)
    {
        Debug.Log($"[OnlineGameManager] Game Over! Winner = P{WinnerPlayerNum}");
        SaveGameOverInfo(WinnerPlayerNum);

        // マスターが代表でシーン遷移（全員追従）
        if (viaNetworkSync && Runner != null && Runner.IsSharedModeMasterClient)
        {
            StartCoroutine(WaitAndLoadResult());
        }
    }

    private void SaveGameOverInfo(int winner)
    {
        if (GameDataContainer.instance == null) return;
        GameDataContainer.instance.winnerPlayerNum = winner;

        if (player1 != null)
        {
            var p1Status = player1.GetComponent<PlayerStatus>();
            if (p1Status != null && p1Status.Object != null)
            {
                GameDataContainer.instance.p1Icon = p1Status.faceIcon;
                GameDataContainer.instance.p1Name = p1Status.playerName;
            }
        }
        if (player2 != null)
        {
            var p2Status = player2.GetComponent<PlayerStatus>();
            if (p2Status != null && p2Status.Object != null)
            {
                GameDataContainer.instance.p2Icon = p2Status.faceIcon;
                GameDataContainer.instance.p2Name = p2Status.playerName;
            }
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

// ---- 簡易ヘルパー：Task をコルーチン待ちに変換 ----
internal static class TaskCoroutineExt
{
    public static IEnumerator AsCoroutine(this System.Threading.Tasks.Task task)
    {
        while (!task.IsCompleted) yield return null;
        if (task.IsFaulted) Debug.LogException(task.Exception);
    }
}