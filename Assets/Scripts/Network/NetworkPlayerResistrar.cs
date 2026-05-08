using UnityEngine;
using Fusion;

/// <summary>
/// 各キャラクター Prefab のルートに付ける NetworkBehaviour。
/// Spawned() 時に OnlineGameManager へ自分を「P1か P2 か」付きで登録する。
///
/// 判定ロジック：
///   ・自分のキャラ (HasInputAuthority == true) は GameDataContainer.isP1 と一致
///   ・相手のキャラ (HasInputAuthority == false) はその反対
/// 　 NetworkLauncher.OnSceneLoadDone() で isP1 を保存済みである前提。
/// </summary>
public class NetworkPlayerRegistrar : NetworkBehaviour
{
    public override void Spawned()
    {
        bool isMine = Object.HasInputAuthority;

        bool myIsP1 = (GameDataContainer.instance != null) && GameDataContainer.instance.isP1;
        bool isP1 = isMine ? myIsP1 : !myIsP1;

        // ローカル操作キャラだけ ActorController を有効化
        var ac = GetComponent<ActorController>();
        if (ac != null) ac.isMine = isMine;

        // OnlineGameManager に登録 → カメラ/UI に伝わる
        if (OnlineGameManager.Instance != null)
        {
            OnlineGameManager.Instance.RegisterPlayer(transform, isP1);
        }
        else
        {
            Debug.LogWarning("[NetworkPlayerRegistrar] OnlineGameManager.Instance が見つかりません。シーンに OnlineGameManager を 1 つ置いてください。");
        }

        Debug.Log($"[NetworkPlayerRegistrar] Spawned: isMine={isMine}, isP1={isP1}, InputAuthority={Object.InputAuthority}");
    }
}