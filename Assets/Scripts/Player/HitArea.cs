using UnityEngine;
using Fusion;

/// <summary>
/// Step 4 版：被弾通知を RPC_TakeDamage 経由にして、
/// ダメージ蓄積・ノックバックを被弾側の StateAuthority で実行する。
/// </summary>
public class HitArea : MonoBehaviour
{
    [Header("技の性能設定")]
    public float damageValue;
    public float baseKnockback;
    public float guardDamageMultiplier;

    [Header("吹っ飛び方向（右向き基準のベクトル）")]
    public Vector2 knockbackAngle;

    public GameObject owner;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // --- 0. 当たり判定は攻撃側の所有者だけで処理 ---
        if (owner != null)
        {
            var ownerNo = owner.GetComponent<NetworkObject>();
            if (ownerNo != null && !ownerNo.HasStateAuthority) return;
        }
        else
        {
            var selfNo = GetComponent<NetworkObject>();
            if (selfNo != null && !selfNo.HasStateAuthority) return;
        }

        // 自分自身にはヒットしない
        if (owner != null && (collision.gameObject == owner || collision.transform.IsChildOf(owner.transform))) return;

        // --- 1. シールド（Shieldレイヤー）への衝突 ---
        if (collision.gameObject.layer == LayerMask.NameToLayer("Shield"))
        {
            // シールド本体に当たった場合、その親キャラの PlayerStatus に通知する
            var shieldedPlayer = collision.GetComponentInParent<PlayerStatus>();
            if (shieldedPlayer != null)
            {
                shieldedPlayer.RPC_TakeShieldHit(damageValue, guardDamageMultiplier);
                Debug.Log("シールドに命中！");

                if (transform.parent == null) DespawnSelf();
                return;
            }
        }

        // --- 2. 本体（PlayerStatus）への衝突 ---
        PlayerStatus targetStatus = collision.GetComponent<PlayerStatus>();
        if (targetStatus != null && !targetStatus.isStunned)
        {
            float facingDir;
            if (transform.parent != null)
                facingDir = (transform.localPosition.x >= 0) ? 1f : -1f;
            else
            {
                Rigidbody2D rb = GetComponent<Rigidbody2D>();
                facingDir = (rb != null && rb.linearVelocity.x < 0) ? -1f : 1f;
            }

            Vector2 finalDirection = new Vector2(knockbackAngle.x * facingDir, knockbackAngle.y);

            // 被弾側の StateAuthority に通知（RPC）
            targetStatus.RPC_TakeDamage(damageValue, baseKnockback, finalDirection);

            Debug.Log($"本体に命中! damage={damageValue}, dir={finalDirection}");

            if (transform.parent == null) DespawnSelf();
        }
    }

    private void DespawnSelf()
    {
        var no = GetComponent<NetworkObject>();
        if (no != null && no.Runner != null && no.HasStateAuthority)
        {
            no.Runner.Despawn(no);
        }
        else if (no == null)
        {
            Destroy(gameObject);
        }
    }
}