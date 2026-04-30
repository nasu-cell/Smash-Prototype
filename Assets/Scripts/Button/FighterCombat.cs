using UnityEngine;
using System.Collections;

public class FighterCombat : MonoBehaviour
{
    private PlayerStatus playerStatus;
    public SpriteRenderer spriteRenderer;
    public Sprite normalSprite; 

    [Header("横攻撃設定")]
    public Sprite sideAttackSprite; 
    public GameObject sideHitboxRight; 
    public GameObject sideHitboxLeft;

    [Header("上攻撃設定")]
    public Sprite upAttackSprite; 
    public GameObject upHitboxRight; // 右向き時の上判定(真上+右上)
    public GameObject upHitboxLeft;  // 左向き時の上判定(真上+左上)

    [Header("必殺技設定")]
    public GameObject specialPrefab; 
    public Transform firePoint; 

    [Header("上必殺技設定")]
    public Sprite upSpecialSprite;
    public float upSpecialForce = 15f;
    public float sideSpecialForce = 5f; // 斜め上昇させる場合

    private bool isAttacking = false;

    void Start()
    {
        playerStatus = GetComponent<PlayerStatus>();
        // 全判定を初期状態でオフ
        DeactivateAllHitboxes();
        
        if (normalSprite == null && spriteRenderer != null) 
        {
            normalSprite = spriteRenderer.sprite;
        }
    }

    private void DeactivateAllHitboxes()
    {
        if (sideHitboxRight) sideHitboxRight.SetActive(false);
        if (sideHitboxLeft) sideHitboxLeft.SetActive(false);
        if (upHitboxRight) upHitboxRight.SetActive(false);
        if (upHitboxLeft) upHitboxLeft.SetActive(false);
    }

    public void PerformSideAttack(bool isRight)
    {
        if (isAttacking) return;
        GameObject targetHitbox = isRight ? sideHitboxRight : sideHitboxLeft;
        StartCoroutine(AttackRoutine(sideAttackSprite, targetHitbox, isRight));
    }

    public void PerformUpAttack(bool isRight)
    {
        if (isAttacking) return;
        GameObject targetHitbox = isRight ? upHitboxRight : upHitboxLeft;
        StartCoroutine(AttackRoutine(upAttackSprite, targetHitbox, isRight));
    }

    private IEnumerator AttackRoutine(Sprite attackSprite, GameObject hitbox, bool isRight)
    {
        isAttacking = true;

        // 1. 画像切り替えと向き固定
        spriteRenderer.sprite = attackSprite;
        spriteRenderer.flipX = !isRight; 
        
        // 2. 当たり判定を有効化
        if (hitbox != null) 
        {
            HitArea hit = hitbox.GetComponent<HitArea>();
            if (hit != null) hit.owner = this.gameObject;
            hitbox.SetActive(true);
        }

        yield return new WaitForSeconds(0.2f); // 攻撃持続時間

        // 3. 元に戻す
        if (hitbox != null) hitbox.SetActive(false);
        spriteRenderer.sprite = normalSprite;
        
        isAttacking = false;
    }

    public void PerformSpecial(bool isRight)
    {
        if (isAttacking || playerStatus.isFallingHelpless) return;

        if (specialPrefab != null && firePoint != null)
        {
            GameObject bullet = Instantiate(specialPrefab, firePoint.position, Quaternion.identity);
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            HitArea hit = bullet.GetComponent<HitArea>();
            if (hit != null) hit.owner = this.gameObject;
            
            if (rb != null)
            {
                float speed = 15f;
                rb.linearVelocity = new Vector2(isRight ? speed : -speed, 0f);
            }
        }
    }

    public void PerformUpSpecial(bool isRight)
    {
        if (isAttacking || playerStatus.isFallingHelpless) return;
        StartCoroutine(UpSpecialRoutine(isRight));
    }

    private IEnumerator UpSpecialRoutine(bool isRight)
    {
        isAttacking = true;
        playerStatus.isFallingHelpless = true; // しりもち落下フラグを即座に立てる

        // スプライト変更
        spriteRenderer.sprite = upSpecialSprite;
        spriteRenderer.flipX = !isRight;

        // 物理的な上昇処理
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // 瞬間的に上方向（＋少し前方向）へ速度を上書き
            float xVel = isRight ? sideSpecialForce : -sideSpecialForce;
            rb.linearVelocity = new Vector2(xVel, upSpecialForce);
        }

        yield return new WaitForSeconds(0.3f); // 上昇モーション時間

        spriteRenderer.sprite = normalSprite;
        isAttacking = false;
        // ここでは isFallingHelpless は false にしない（接地するまで維持）
    }
}