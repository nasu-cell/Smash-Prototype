using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Step 5 版：切断検知イベントと、安全な Shutdown 経路を追加。
/// </summary>
public class NetworkLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkLauncher Instance { get; private set; }

    [Header("Build Settings に登録したバトルシーン名")]
    public string battleSceneName = "OnlineBattleScene";

    [Header("キャラクター Prefab（CharacterSelect の ID 順に並べる）")]
    public NetworkObject[] characterPrefabs;

    [Header("待機室で使う PlayerLobbyInfo Prefab")]
    public NetworkObject lobbyInfoPrefab;

    [Header("スポーン位置（ワールド座標・フォールバック）")]
    public Vector3 spawnPos1 = new Vector3(-3f, 1f, 0f);
    public Vector3 spawnPos2 = new Vector3(3f, 1f, 0f);

    private NetworkRunner runner;
    public NetworkRunner Runner => runner;

    public event Action<int> OnPlayerCountChanged;
    /// <summary>相手プレイヤーが退出（切断）したときに発火</summary>
    public event Action OnOpponentDisconnected;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task<bool> StartGame(string roomName)
    {
        if (runner != null)
        {
            Debug.LogWarning("[NetworkLauncher] Runner はすでに起動中です。");
            return false;
        }

        runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;
        runner.AddCallbacks(this);

        var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = roomName,
            PlayerCount = 2,
            SceneManager = sceneManager,
        });

        if (!result.Ok)
        {
            Debug.LogError($"[NetworkLauncher] StartGame Failed: {result.ShutdownReason}");
            Destroy(runner);
            runner = null;
            return false;
        }

        Debug.Log($"[NetworkLauncher] Connected to room: {roomName} (LocalPlayer: {runner.LocalPlayer})");

        // ★ 待機室で相手にキャラ ID を伝えるための PlayerLobbyInfo を Spawn
        if (lobbyInfoPrefab != null)
        {
            runner.Spawn(lobbyInfoPrefab, Vector3.zero, Quaternion.identity, runner.LocalPlayer);
            Debug.Log("[NetworkLauncher] PlayerLobbyInfo を Spawn");
        }
        else
        {
            Debug.LogWarning("[NetworkLauncher] lobbyInfoPrefab が null。Inspector で設定してください。");
        }

        return true;
    }

    /// <summary>
    /// セッションから抜ける。リザルト画面の OK ボタン等から呼ばれる。
    /// destroyGameObject: false を指定しないと NetworkLauncher 自体が破棄される。
    /// </summary>
    public async Task ShutdownAsync()
    {
        if (runner != null)
        {
            await runner.Shutdown(destroyGameObject: false);
            runner = null;
        }
    }

    public void Shutdown()
    {
        _ = ShutdownAsync();
    }

    /// <summary>
    /// セッションを Shutdown してから指定シーンへ遷移するコルーチン。
    /// DontDestroyOnLoad の NetworkLauncher 上で動くので、
    /// 呼び出し元の GameObject（OnlineGameManager 等）が途中で破棄されても
    /// 最後まで実行される。
    /// </summary>
    public System.Collections.IEnumerator ShutdownAndLoadScene(string sceneName, float delayBefore = 0f)
    {
        Debug.Log($"[NetworkLauncher] ShutdownAndLoadScene 開始 (target='{sceneName}', delay={delayBefore})");

        if (delayBefore > 0f)
        {
            Debug.Log($"[NetworkLauncher] WaitForSeconds({delayBefore}) 待機中...");
            yield return new UnityEngine.WaitForSeconds(delayBefore);
            Debug.Log("[NetworkLauncher] WaitForSeconds 完了");
        }

        if (runner != null)
        {
            Debug.Log("[NetworkLauncher] ShutdownAsync 呼出し...");
            var task = ShutdownAsync();
            while (!task.IsCompleted) yield return null;
            Debug.Log("[NetworkLauncher] ShutdownAsync 完了");
        }
        else
        {
            Debug.Log("[NetworkLauncher] runner が既に null なので Shutdown スキップ");
        }

        yield return null;

        int idx = SceneUtility.GetBuildIndexByScenePath(sceneName);
        if (idx < 0)
        {
            Debug.LogError($"[NetworkLauncher] シーン '{sceneName}' が Build Settings に未登録！");
            yield break;
        }

        Debug.Log($"[NetworkLauncher] SceneManager.LoadScene('{sceneName}') 実行");
        SceneManager.LoadScene(sceneName);
    }

    // ---- INetworkRunnerCallbacks ----

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        int count = runner.SessionInfo.PlayerCount;
        Debug.Log($"[NetworkLauncher] Player {player} joined. Count = {count}");
        OnPlayerCountChanged?.Invoke(count);
        // ★ 自動シーン遷移は行わない。WaitingRoomManager が両者 READY を確認してから
        //   RequestBattleSceneLoad() を呼ぶ。
    }

    /// <summary>
    /// 両者 READY が確認された後、マスターから呼ばれてバトルシーンへ遷移する。
    /// </summary>
    public void RequestBattleSceneLoad()
    {
        if (runner == null || !runner.IsSharedModeMasterClient) return;

        int idx = SceneUtility.GetBuildIndexByScenePath(battleSceneName);
        if (idx < 0)
        {
            Debug.LogError($"[NetworkLauncher] Scene not in Build Settings: {battleSceneName}");
            return;
        }
        Debug.Log("[NetworkLauncher] Runner.LoadScene 実行");
        runner.LoadScene(SceneRef.FromIndex(idx));
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (SceneManager.GetActiveScene().name != battleSceneName) return;

        int charId = (GameDataContainer.instance != null)
            ? GameDataContainer.instance.mySelectedCharID
            : 0;
        if (charId < 0 || charId >= characterPrefabs.Length) charId = 0;

        bool isP1 = runner.IsSharedModeMasterClient;
        if (GameDataContainer.instance != null) GameDataContainer.instance.isP1 = isP1;

        Vector3 pos = isP1 ? spawnPos1 : spawnPos2;
        var ogm = OnlineGameManager.Instance;
        if (ogm != null)
        {
            Transform sp = isP1 ? ogm.spawnPoint1 : ogm.spawnPoint2;
            if (sp != null) pos = sp.position;
        }

        var obj = runner.Spawn(characterPrefabs[charId], pos, Quaternion.identity, runner.LocalPlayer);
        Debug.Log($"[NetworkLauncher] Spawned local player ({(isP1 ? "P1" : "P2")}) at {pos} as {obj.name}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        int count = runner.SessionInfo.PlayerCount;
        Debug.Log($"[NetworkLauncher] Player {player} left. Count = {count}");
        OnPlayerCountChanged?.Invoke(count);
        // 自分以外が抜けた場合に通知
        if (player != runner.LocalPlayer)
        {
            OnOpponentDisconnected?.Invoke();
        }
    }

    // ---- 残りは空実装 ----
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[NetworkLauncher] OnShutdown: {shutdownReason}");
        this.runner = null;
    }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}