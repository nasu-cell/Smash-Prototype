using UnityEngine;
using System.Collections;

public class FighterCombat : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Sprite normalSprite; 

    [Header("通常攻撃設定")]
    public Sprite attackSprite; 
    public GameObject attackHitbox; 

    [Header("必殺技設定")]
    public GameObject specialPrefab; 
    public Transform firePoint; 


    private bool isAttacking = false;

    void Start()
    {
        if (attackHitbox != null) 
        {
            attackHitbox.SetActive(false);
        }
        // 現在の画像を通常時として保存
        if (normalSprite == null) 
        {
            normalSprite = spriteRenderer.sprite;
        }
    }

    // 引数 isRight で方向を受け取る
    public void PerformAttack(bool isRight)
    {
        if (isAttacking) 
        {
            return;
        }
        StartCoroutine(AttackRoutine(isRight));
    }

    private IEnumerator AttackRoutine(bool isRight)
    {
        isAttacking = true;

        // 1. 画像切り替えと反転設定
        spriteRenderer.sprite = attackSprite;
        spriteRenderer.flipX = !isRight; // 左攻撃なら反転(true)

        HitArea hit = attackHitbox.GetComponent<HitArea>();
        if (hit != null)
        {
            hit.owner = this.gameObject; // 自分をオーナーとして登録
        }

        // 2. 当たり判定を有効化
        if (attackHitbox != null) 
        {
            attackHitbox.SetActive(true);
        }

        yield return new WaitForSeconds(0.2f);

        // 3. 元に戻す
        if (attackHitbox != null) 
        {
            attackHitbox.SetActive(false);
        }
        spriteRenderer.sprite = normalSprite;
        
        
        // 戻した直後のflipXは現在の移動方向に合わせるためActorControllerのMoveUpdateに任せるか
        // ここで暫定的に戻す
        isAttacking = false;
    }

    public void PerformSpecial(bool isRight)
    {
        if (specialPrefab != null && firePoint != null)
        {
            GameObject bullet = Instantiate(specialPrefab, firePoint.position, Quaternion.identity);
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();

            HitArea hit = bullet.GetComponent<HitArea>();
            if (hit != null)
            {
                hit.owner = this.gameObject;
            }
            
            if (rb != null)
            {
                float speed = 15f;
                // 右なら正の方向、左なら負の方向に飛ばす
                rb.linearVelocity = new Vector2(isRight ? speed : -speed, 0f);
            }
        }
    }
}