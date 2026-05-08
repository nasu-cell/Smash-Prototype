using UnityEngine;

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
        if (owner != null && (collision.gameObject == owner || collision.transform.IsChildOf(owner.transform))) return;

        // --- 1. シールド（Shieldレイヤー）への衝突を最優先でチェック ---
        if (collision.gameObject.layer == LayerMask.NameToLayer("Shield"))
        {
            GuardShield shield = collision.GetComponent<GuardShield>();
            if (shield != null)
            {
                shield.TakeShieldDamage(damageValue, guardDamageMultiplier);
                Debug.Log("シールドに命中！ガード成功");
                
                // 弾なら消滅させる
                if (transform.parent == null) Destroy(gameObject);
                return; // ここで終了することで本体へのダメージを防ぐ
            }
        }

        // --- 2. シールドをすり抜けて本体（PlayerStatus）に衝突した場合 ---
        PlayerStatus targetStatus = collision.GetComponent<PlayerStatus>();
        if (targetStatus != null)
        {
            if (!targetStatus.isStunned) 
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
                
                Debug.Log("本体に命中！");

                if (transform.parent == null) Destroy(gameObject);
            }
        }
    }
}