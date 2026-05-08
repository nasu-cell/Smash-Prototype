using UnityEngine;
using Fusion;

/// <summary>
/// SpriteRenderer.flipX をネットワーク同期する。
/// 自分が InputAuthority を持つキャラだけ書き込み、リモート視点では同期値を反映する。
/// 各キャラ Prefab のルート（SpriteRenderer がある場所）にアタッチ。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class NetworkSpriteFlipSync : NetworkBehaviour
{
    [Networked] public NetworkBool FlipX { get; set; }

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public override void FixedUpdateNetwork()
    {
        // 所有者だけ書き込む（Shared Mode では InputAuthority == StateAuthority）
        if (HasStateAuthority && sr != null)
        {
            FlipX = sr.flipX;
        }
    }

    public override void Render()
    {
        // リモート視点ではネットワーク側の値を反映
        if (!HasStateAuthority && sr != null)
        {
            sr.flipX = FlipX;
        }
    }
}