using UnityEngine;
using Fusion;

/// <summary>
/// 各キャラクター Prefab のルートに付ける NetworkBehaviour。
/// Spawned() 時に OnlineGameManager へ自分を「P1か P2 か」付きで登録する。
///
/// Step 3 追加：リモート視点での Rigidbody2D 競合を防ぐため、
/// 自分以外のキャラの Rigidbody2D を Kinematic に切り替える。
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

        // ★ Step 3 追加：リモート視点では Rigidbody2D を Kinematic に
        //   ローカル所有者だけ Dynamic で物理駆動。NetworkTransform で位置追従。
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = isMine ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        }

        // OnlineGameManager に登録 → カメラ/UI に伝わる
        if (OnlineGameManager.Instance != null)
        {
            OnlineGameManager.Instance.RegisterPlayer(transform, isP1);
        }
        else
        {
            Debug.LogWarning("[NetworkPlayerRegistrar] OnlineGameManager.Instance が見つかりません。");
        }

        Debug.Log($"[NetworkPlayerRegistrar] Spawned: isMine={isMine}, isP1={isP1}, InputAuthority={Object.InputAuthority}");
    }
}