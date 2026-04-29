using UnityEngine;

public class HitArea : MonoBehaviour
{
    [Header("技の性能設定")]
    public float damageValue;     // 相手に与えるダメージ(%)
    public float baseKnockback; // 吹っ飛ばす力の基礎値
    [Header("吹っ飛び方向（右向き基準）")]
    public Vector2 knockbackAngle;

    // 自分の親（プレイヤー自身）を登録して、自分への誤爆を防ぐ
    public GameObject owner;

    // トリガー判定（Is Triggerがオンのコライダーが必要）
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (owner != null && (collision.gameObject == owner || collision.transform.IsChildOf(owner.transform)))
        {
            return;
        }
        // 相手のPlayerStatusを取得
        PlayerStatus targetStatus = collision.GetComponent<PlayerStatus>();

        if (targetStatus != null)
        {
            // 攻撃者の向き（flipX）を確認して、飛ばす方向を反転させる
            float facingDir = (owner.GetComponent<SpriteRenderer>().flipX) ? -1f : 1f;
            
            // 設定された角度に、向きを掛け合わせる
            Vector2 finalDirection = new Vector2(knockbackAngle.x * facingDir, knockbackAngle.y);

            // 相手にダメージと吹っ飛ばしを通知
            targetStatus.TakeDamage(damageValue, baseKnockback, finalDirection);

            if (transform.parent == null) // 親がいない＝独立したPrefab（弾）の場合
            {
                Destroy(gameObject);
            }
        }
    }
}