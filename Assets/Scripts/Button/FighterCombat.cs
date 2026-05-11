using UnityEngine;
using System.Collections;
using Fusion;

/// <summary>
/// オンライン／オフライン両モード対応版。
/// Spawned() が呼ばれていればオンライン（RPC + Runner.Spawn）、
/// そうでなければオフライン（直接 StartCoroutine + Instantiate）。
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
    [Tooltip("オンライン用：NetworkObject + NetworkTransform が付いた Prefab (Special_online)")]
    public NetworkObject specialOnlinePrefab;

    [Tooltip("ローカル用：通常の弾 Prefab (Special)")]
    public GameObject specialLocalPrefab;

    public Transform firePoint;

    [Header("上必殺技設定")]
    public Sprite upSpecialSprite;
    public float upSpecialForce = 15f;
    public float sideSpecialForce = 5f;

    private bool isAttacking = false;
    private bool useNetworked = false;

    void Start()
    {
        playerStatus = GetComponent<PlayerStatus>();
        DeactivateAllHitboxes();
        if (normalSprite == null && spriteRenderer != null) normalSprite = spriteRenderer.sprite;
    }

    public override void Spawned()
    {
        useNetworked = true;
    }

    private void DeactivateAllHitboxes()
    {
        if (sideHitboxRight) sideHitboxRight.SetActive(false);
        if (sideHitboxLeft) sideHitboxLeft.SetActive(false);
        if (upHitboxRight) upHitboxRight.SetActive(false);
        if (upHitboxLeft) upHitboxLeft.SetActive(false);
    }

    // ===== 入力エントリ =====

    public void PerformSideAttack(bool isRight)
    {
        if (isAttacking) return;
        if (useNetworked)
        {
            if (!HasStateAuthority) return;
            RPC_PlaySideAttack(isRight);
        }
        else
        {
            GameObject hb = isRight ? sideHitboxRight : sideHitboxLeft;
            StartCoroutine(AttackRoutine(sideAttackSprite, hb, isRight));
        }
    }

    public void PerformUpAttack(bool isRight)
    {
        if (isAttacking) return;
        if (useNetworked)
        {
            if (!HasStateAuthority) return;
            RPC_PlayUpAttack(isRight);
        }
        else
        {
            GameObject hb = isRight ? upHitboxRight : upHitboxLeft;
            StartCoroutine(AttackRoutine(upAttackSprite, hb, isRight));
        }
    }

    public void PerformSpecial(bool isRight)
    {
        if (isAttacking || playerStatus.isFallingHelpless) return;

        if (firePoint == null)
        {
            Debug.LogError("firePoint が未設定です");
            return;
        }

        if (useNetworked)
        {
            // オンラインモード
            if (!HasStateAuthority) return;
            if (specialOnlinePrefab == null)
            {
                Debug.LogError("specialOnlinePrefab が未設定です");
                return;
            }

            var bullet = Runner.Spawn(specialOnlinePrefab, firePoint.position, Quaternion.identity, Object.InputAuthority);
            ApplyBulletSettings(bullet.gameObject, isRight);
            Debug.Log("[FighterCombat] オンライン弾 (Special_online) 発射");
        }
        else
        {
            // ローカルモード
            if (specialLocalPrefab == null)
            {
                Debug.LogError("specialLocalPrefab が未設定です");
                return;
            }

            var bulletObj = Instantiate(specialLocalPrefab, firePoint.position, Quaternion.identity);
            ApplyBulletSettings(bulletObj, isRight);
            Debug.Log("[FighterCombat] ローカル弾 (Special) 発射");
        }
    }

    // 弾の共通設定（速度・オーナー）をまとめたメソッド
    private void ApplyBulletSettings(GameObject bullet, bool isRight)
    {
        var rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = new Vector2(isRight ? 15f : -15f, 0f);

        var hit = bullet.GetComponent<HitArea>();
        if (hit != null) hit.owner = this.gameObject;
    }

    public void PerformUpSpecial(bool isRight)
    {
        if (isAttacking || playerStatus.isFallingHelpless) return;
        if (useNetworked)
        {
            if (!HasStateAuthority) return;
            RPC_PlayUpSpecial(isRight);
        }
        else
        {
            StartCoroutine(UpSpecialRoutine(isRight));
        }
    }

    // ===== RPC（オンライン専用） =====

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlaySideAttack(NetworkBool isRight)
    {
        GameObject hb = isRight ? sideHitboxRight : sideHitboxLeft;
        StartCoroutine(AttackRoutine(sideAttackSprite, hb, isRight));
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayUpAttack(NetworkBool isRight)
    {
        GameObject hb = isRight ? upHitboxRight : upHitboxLeft;
        StartCoroutine(AttackRoutine(upAttackSprite, hb, isRight));
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayUpSpecial(NetworkBool isRight)
    {
        StartCoroutine(UpSpecialRoutine(isRight));
    }

    // ===== 共通コルーチン =====

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

        // しりもち落下フラグはオンラインなら StateAuthority のみ、オフラインなら誰でも
        if (!useNetworked || HasStateAuthority)
        {
            playerStatus.isFallingHelpless = true;
        }

        spriteRenderer.sprite = upSpecialSprite;
        spriteRenderer.flipX = !isRight;

        if (!useNetworked || HasStateAuthority)
        {
            var rb = GetComponent<Rigidbody2D>();
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