using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Photon Fusion 2 の接続・セッション管理を行うシングルトン。
/// CharacterSelectScene など、最初に通るシーンに 1 個だけ配置する。
/// （Awake で DontDestroyOnLoad されるためシーンを跨いで生き残る）
/// </summary>
public class NetworkLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkLauncher Instance { get; private set; }

    [Header("Build Settings に登録したバトルシーン名")]
    public string battleSceneName = "OnlineBattleScene";

    [Header("キャラクター Prefab（CharacterSelect の ID 順に並べる）")]
    [Tooltip("ルートに NetworkObject が付いた Prefab を入れる。インデックス = キャラID。")]
    public NetworkObject[] characterPrefabs;

    [Header("スポーン位置（ワールド座標・フォールバック）")]
    [Tooltip("OnlineGameManager.spawnPoint1 / spawnPoint2 が設定されていれば、そちらが優先される。")]
    public Vector3 spawnPos1 = new Vector3(-3f, 1f, 0f);
    public Vector3 spawnPos2 = new Vector3(3f, 1f, 0f);

    private NetworkRunner runner;
    public NetworkRunner Runner => runner;

    // WaitingRoomManager から購読する用のイベント
    public event Action<int> OnPlayerCountChanged;

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

    /// <summary>
    /// 部屋名（SessionName）で Fusion セッションに参加する。
    /// </summary>
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

        // Fusion 標準のシーン管理（マスターが LoadScene すれば全員追従）
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
        return true;
    }

    /// <summary>
    /// 部屋から抜けたいとき。
    /// </summary>
    public async void Shutdown()
    {
        if (runner != null)
        {
            await runner.Shutdown();
            runner = null;
        }
    }

    // ---- INetworkRunnerCallbacks ----

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        int count = runner.SessionInfo.PlayerCount;
        Debug.Log($"[NetworkLauncher] Player {player} joined. Count = {count}");
        OnPlayerCountChanged?.Invoke(count);

        // 2 人揃ったらマスターがバトルシーンへ遷移（他クライアントは追従）
        if (count >= 2 && runner.IsSharedModeMasterClient)
        {
            int idx = SceneUtility.GetBuildIndexByScenePath(battleSceneName);
            if (idx < 0)
            {
                Debug.LogError($"[NetworkLauncher] Scene not in Build Settings: {battleSceneName}");
                return;
            }
            runner.LoadScene(SceneRef.FromIndex(idx));
        }
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        // バトルシーンに到着したら、自分のキャラだけスポーン
        if (SceneManager.GetActiveScene().name != battleSceneName) return;

        int charId = (GameDataContainer.instance != null)
            ? GameDataContainer.instance.mySelectedCharID
            : 0;
        if (charId < 0 || charId >= characterPrefabs.Length) charId = 0;

        bool isP1 = runner.IsSharedModeMasterClient;
        if (GameDataContainer.instance != null) GameDataContainer.instance.isP1 = isP1;

        // シーン内の OnlineGameManager にある SpawnPoint があればそちらを優先
        Vector3 pos = isP1 ? spawnPos1 : spawnPos2;
        var ogm = OnlineGameManager.Instance;
        if (ogm != null)
        {
            Transform sp = isP1 ? ogm.spawnPoint1 : ogm.spawnPoint2;
            if (sp != null) pos = sp.position;
        }

        // Shared Mode: 自分が InputAuthority を持つキャラを Spawn する
        var obj = runner.Spawn(characterPrefabs[charId], pos, Quaternion.identity, runner.LocalPlayer);
        Debug.Log($"[NetworkLauncher] Spawned local player ({(isP1 ? "P1" : "P2")}) at {pos} as {obj.name}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[NetworkLauncher] Player {player} left.");
        OnPlayerCountChanged?.Invoke(runner.SessionInfo.PlayerCount);
    }

    // ---- 残りは空実装でOK ----
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
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { runner = null; }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}