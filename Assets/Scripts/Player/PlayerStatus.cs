using UnityEngine;
using Fusion;

/// <summary>
/// Step 4 版：NetworkBehaviour 化。
/// ダメージ %、ストック、各種フラグ、シールド耐久値をネットワーク同期。
/// 被弾は HitArea から RPC_TakeDamage 経由で被弾側の StateAuthority に通知。
/// </summary>
public class PlayerStatus : NetworkBehaviour
{
    [Header("基本ステータス（Inspector で設定）")]
    public string playerName = "P1";
    public Sprite faceIcon;
    public int initialStock = 3;

    // ===== Networked state（全クライアントで同じ値が見える） =====
    [Networked] public float totalDamage { get; set; }
    [Networked] public int currentStock { get; set; }
    [Networked] public NetworkBool isStunned { get; set; }
    [Networked] public NetworkBool isGuarding { get; set; }
    [Networked] public NetworkBool isShieldBroken { get; set; }
    [Networked] public NetworkBool isFallingHelpless { get; set; }
    [Networked] public float shieldScale { get; set; }

    [Header("シールド設定")]
    public float shrinkSpeed;
    public float recoverSpeed;

    [Header("摩擦設定")]
    public PhysicsMaterial2D frictionlessMaterial;
    public PhysicsMaterial2D highFrictionMaterial;

    private KnockbackCalculator calculator;
    private Rigidbody2D rb;
    private GuardShield shield;
    private CapsuleCollider2D myCollider;
    private ActorController actorController;
    private SpriteRenderer sr;

    // Fusion 流のタイマー（Invoke の代わり）
    private TickTimer breakRecoverTimer;
    private TickTimer stunRecoverTimer;

    public override void Spawned()
    {
        calculator = new KnockbackCalculator();
        rb = GetComponent<Rigidbody2D>();
        shield = GetComponentInChildren<GuardShield>(true);
        myCollider = GetComponent<CapsuleCollider2D>();
        actorController = GetComponent<ActorController>();
        sr = GetComponent<SpriteRenderer>();

        if (myCollider != null && frictionlessMaterial != null)
        {
            myCollider.sharedMaterial = frictionlessMaterial;
        }

        // 初期値は StateAuthority だけが設定
        if (HasStateAuthority)
        {
            currentStock = initialStock;
            totalDamage = 0;
            shieldScale = shield != null ? shield.maxScale : 1f;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (shield == null) return;

        // シールドサイズの更新
        if (isGuarding && !isShieldBroken)
        {
            shieldScale = Mathf.Clamp(shieldScale - shrinkSpeed * Runner.DeltaTime, shield.minScale, shield.maxScale);
            if (shieldScale <= shield.minScale)
            {
                ShieldBreak();
            }
        }
        else if (!isShieldBroken)
        {
            shieldScale = Mathf.Clamp(shieldScale + recoverSpeed * Runner.DeltaTime, shield.minScale, shield.maxScale);
        }

        // タイマー満了による状態復帰
        if (breakRecoverTimer.Expired(Runner))
        {
            isShieldBroken = false;
            isStunned = false;
            shieldScale = shield.maxScale;
            breakRecoverTimer = TickTimer.None;
        }

        if (stunRecoverTimer.Expired(Runner))
        {
            if (!isShieldBroken)
            {
                isStunned = false;
            }
            stunRecoverTimer = TickTimer.None;
        }
    }

    public override void Render()
    {
        // 視覚更新は全クライアントで実行
        UpdatePhysicsMaterial();
        UpdateShieldVisual();
        UpdateBodyColor();
    }

    private void UpdateShieldVisual()
    {
        if (shield == null) return;
        shield.gameObject.SetActive(isGuarding && !isShieldBroken);
        shield.transform.localScale = Vector3.one * shieldScale;
        shield.currentScale = shieldScale;
    }

    private void UpdateBodyColor()
    {
        if (sr == null) return;
        sr.color = isShieldBroken ? Color.gray : Color.white;
    }

    private void UpdatePhysicsMaterial()
    {
        if (myCollider == null || actorController == null) return;

        if (actorController.isGround && (isGuarding || isShieldBroken))
        {
            myCollider.sharedMaterial = highFrictionMaterial;
        }
        else
        {
            myCollider.sharedMaterial = frictionlessMaterial;
        }
    }

    public void ShieldBreak()
    {
        if (!HasStateAuthority) return;
        isShieldBroken = true;
        isGuarding = false;
        isStunned = true;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        breakRecoverTimer = TickTimer.CreateFromSeconds(Runner, 3.0f);
    }

    /// <summary>
    /// 攻撃側の HitArea から呼ばれる。被弾側の StateAuthority で実行される。
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(float baseDamage, float baseKnockback, Vector2 direction)
    {
        if (isShieldBroken) return;

        totalDamage += baseDamage;
        float finalForce = calculator.Calculate(totalDamage, baseKnockback);
        isStunned = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            transform.position += new Vector3(0, 0.1f, 0);
            rb.AddForce(direction.normalized * finalForce, ForceMode2D.Impulse);
        }

        stunRecoverTimer = TickTimer.CreateFromSeconds(Runner, 0.5f);
    }

    /// <summary>
    /// シールドに攻撃が当たった時。被弾側の StateAuthority で実行される。
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeShieldHit(float damage, float multiplier)
    {
        if (shield == null) return;
        shieldScale = Mathf.Clamp(shieldScale - damage * multiplier, shield.minScale, shield.maxScale);
    }

    /// <summary>
    /// 撃墜（場外）時に OnlineGameManager から呼ばれる。
    /// 必ず player の StateAuthority クライアントで呼ぶこと。
    /// </summary>
    public void OnKilled()
    {
        if (!HasStateAuthority) return;
        currentStock--;
        totalDamage = 0;
        isFallingHelpless = false;
        isStunned = false;
    }
}