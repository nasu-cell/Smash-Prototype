using UnityEngine;
using Fusion;

/// <summary>
/// 待機室でキャラ ID と READY 状態を相手に伝える NetworkBehaviour。
/// 各プレイヤーが接続時に 1 個 Spawn し、自分の状態を [Networked] で同期する。
/// </summary>
public class PlayerLobbyInfo : NetworkBehaviour
{
    [Networked] public int CharacterId { get; set; }
    [Networked] public NetworkBool IsReady { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            int id = (GameDataContainer.instance != null)
                ? GameDataContainer.instance.mySelectedCharID
                : 0;
            CharacterId = id;
            IsReady = false;
            Debug.Log($"[PlayerLobbyInfo] 自分の CharId={id} を書き込み");
        }

        var wrm = FindObjectOfType<WaitingRoomManager>();
        if (wrm != null)
        {
            wrm.RegisterLobbyInfo(this);
            Debug.Log($"[PlayerLobbyInfo] WaitingRoomManager に登録 (HasInputAuthority={HasInputAuthority})");
        }
    }

    /// <summary>READY 状態を切り替える。所有者のみ書ける。</summary>
    public void SetReady(bool ready)
    {
        if (HasStateAuthority) IsReady = ready;
    }
}