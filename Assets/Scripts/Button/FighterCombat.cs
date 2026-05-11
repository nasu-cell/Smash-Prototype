using UnityEngine;
using System.Collections;
using Fusion;

/// <summary>
/// Step 3 版：NetworkBehaviour 化。
/// - 通常攻撃（横/上）は所有者から RPC を発行し、全クライアントで同じ視覚効果を再生
/// - 必殺技の弾は所有者だけが Runner.Spawn で生成（NetworkObject として全員に伝播）
/// - 当たり判定の発火は HitArea 側で StateAuthority チェック
/// </summary>
public class FighterCombat : NetworkBehaviour
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
    public GameObject upHitboxRight;
    public GameObject upHitboxLeft;

    [Header("必殺技設定")]
    [Tooltip("ルートに NetworkObject + NetworkTransform が付いた弾 Prefab を登録")]
    public NetworkObject specialPrefab;
    public Transform firePoint;

    [Header("上必殺技設定")]
    public Sprite upSpecialSprite;
    public float upSpecialForce = 15f;
    public float sideSpecialForce = 5f;

    private bool isAttacking = false;

    void Start()
    {
        playerStatus = GetComponent<PlayerStatus>();
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

    // ===== 入力エントリポイント（ActorController から呼ばれる） =====

    public void PerformSideAttack(bool isRight)
    {
        if (isAttacking) return;
        if (!Object.HasStateAuthority) return;
        RPC_PlaySideAttack(isRight);
    }

    public void PerformUpAttack(bool isRight)
    {
        if (isAttacking) return;
        if (!Object.HasStateAuthority) return;
        RPC_PlayUpAttack(isRight);
    }

    public void PerformSpecial(bool isRight)
    {
        if (isAttacking || playerStatus.isFallingHelpless) return;
        if (!Object.HasStateAuthority) return;

        if (specialPrefab == null || firePoint == null)
        {
            Debug.LogWarning("specialPrefab または firePoint が未設定");
            return;
        }

        // 弾は NetworkObject として Runner.Spawn → 全員に伝播
        var bullet = Runner.Spawn(specialPrefab, firePoint.position, Quaternion.identity, Object.InputAuthority);

        // 速度設定（所有者側で初速を入れる。NetworkTransform で位置同期される）
        var rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float speed = 15f;
            rb.linearVelocity = new Vector2(isRight ? speed : -speed, 0f);
        }

        // HitArea の owner を設定（自分への命中防止用）
        var hit = bullet.GetComponent<HitArea>();
        if (hit != null) hit.owner = this.gameObject;
    }

    public void PerformUpSpecial(bool isRight)
    {
        if (isAttacking || playerStatus.isFallingHelpless) return;
        if (!Object.HasStateAuthority) return;
        RPC_PlayUpSpecial(isRight);
    }

    // ===== RPC：全クライアントで視覚効果を再生 =====

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlaySideAttack(NetworkBool isRight)
    {
        GameObject targetHitbox = isRight ? sideHitboxRight : sideHitboxLeft;
        StartCoroutine(AttackRoutine(sideAttackSprite, targetHitbox, isRight));
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayUpAttack(NetworkBool isRight)
    {
        GameObject targetHitbox = isRight ? upHitboxRight : upHitboxLeft;
        StartCoroutine(AttackRoutine(upAttackSprite, targetHitbox, isRight));
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayUpSpecial(NetworkBool isRight)
    {
        StartCoroutine(UpSpecialRoutine(isRight));
    }

    // ===== 視覚効果コルーチン（全クライアントで動く） =====

    private IEnumerator AttackRoutine(Sprite attackSprite, GameObject hitbox, bool isRight)
    {
        isAttacking = true;

        spriteRenderer.sprite = attackSprite;
        spriteRenderer.flipX = !isRight;

        if (hitbox != null)
        {
            HitArea hit = hitbox.GetComponent<HitArea>();
            if (hit != null) hit.owner = this.gameObject;
            hitbox.SetActive(true);
        }

        yield return new WaitForSeconds(0.2f);

        if (hitbox != null) hitbox.SetActive(false);
        spriteRenderer.sprite = normalSprite;

        isAttacking = false;
    }

    private IEnumerator UpSpecialRoutine(bool isRight)
    {
        isAttacking = true;

        // しりもち落下フラグは所有者側で立てる
        if (Object.HasStateAuthority)
        {
            playerStatus.isFallingHelpless = true;
        }

        spriteRenderer.sprite = upSpecialSprite;
        spriteRenderer.flipX = !isRight;

        // 物理上昇は所有者側だけ実行（リモートは NetworkTransform で位置追従）
        if (Object.HasStateAuthority)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float xVel = isRight ? sideSpecialForce : -sideSpecialForce;
                rb.linearVelocity = new Vector2(xVel, upSpecialForce);
            }
        }

        yield return new WaitForSeconds(0.3f);

        spriteRenderer.sprite = normalSprite;
        isAttacking = false;
    }
}