using UnityEngine;
using Fusion;

/// <summary>
/// オンライン／オフライン両モード対応版。
/// NetworkObject.IsValid を見て、オンライン時のみ HasStateAuthority チェック。
/// PlayerStatus.TakeDamage / TakeShieldDamage を呼び、これらが内部で RPC/直適用を切替。
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
        bool isNetworked = false;

        // オンライン中なら攻撃側の StateAuthority だけが当たり判定処理を行う
        if (owner != null)
        {
            var ownerNo = owner.GetComponent<NetworkObject>();
            if (ownerNo != null && ownerNo.IsValid)
            {
                isNetworked = true;
                if (!ownerNo.HasStateAuthority) return;
            }
        }
        else
        {
            var selfNo = GetComponent<NetworkObject>();
            if (selfNo != null && selfNo.IsValid)
            {
                isNetworked = true;
                if (!selfNo.HasStateAuthority) return;
            }
        }

        // 自分自身への命中を防ぐ
        if (owner != null && (collision.gameObject == owner || collision.transform.IsChildOf(owner.transform))) return;

        // --- シールド命中 ---
        if (collision.gameObject.layer == LayerMask.NameToLayer("Shield"))
        {
            var shieldedPlayer = collision.GetComponentInParent<PlayerStatus>();
            if (shieldedPlayer != null)
            {
                shieldedPlayer.TakeShieldDamage(damageValue, guardDamageMultiplier);
                Debug.Log("シールドに命中！");
                if (transform.parent == null) DespawnSelf(isNetworked);
                return;
            }
        }

        // --- 本体命中 ---
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
            targetStatus.TakeDamage(damageValue, baseKnockback, finalDirection);

            Debug.Log($"本体に命中! damage={damageValue}, dir={finalDirection}");
            if (transform.parent == null) DespawnSelf(isNetworked);
        }
    }

    private void DespawnSelf(bool isNetworked)
    {
        if (isNetworked)
        {
            var no = GetComponent<NetworkObject>();
            if (no != null && no.Runner != null && no.HasStateAuthority)
            {
                no.Runner.Despawn(no);
                return;
            }
        }
        Destroy(gameObject);
    }
}