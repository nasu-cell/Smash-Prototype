using UnityEngine;
using Fusion;

/// <summary>
/// オンライン（NetworkRunner あり）／オフライン（Training）両モード対応版。
/// - Spawned() が呼ばれた場合 → useNetworked = true → [Networked] プロパティで同期
/// - そうでない場合 → useNetworked = false → ローカルフィールドのみ使用
/// </summary>
public class PlayerStatus : NetworkBehaviour
{
    [Header("基本ステータス（Inspector で設定）")]
    public string playerName = "P1";
    public Sprite faceIcon;
    public int initialStock = 3;

    [Header("シールド設定")]
    public float shrinkSpeed;
    public float recoverSpeed;

    [Header("摩擦設定")]
    public PhysicsMaterial2D frictionlessMaterial;
    public PhysicsMaterial2D highFrictionMaterial;

    // ===== オフライン用ローカルフィールド =====
    private float _localTotalDamage;
    private int _localCurrentStock = 3;
    private bool _localIsStunned;
    private bool _localIsGuarding;
    private bool _localIsShieldBroken;
    private bool _localIsFallingHelpless;
    private float _localShieldScale;

    // ===== オンライン用 Networked =====
    [Networked] private float NetTotalDamage { get; set; }
    [Networked] private int NetCurrentStock { get; set; }
    [Networked] private NetworkBool NetIsStunned { get; set; }
    [Networked] private NetworkBool NetIsGuarding { get; set; }
    [Networked] private NetworkBool NetIsShieldBroken { get; set; }
    [Networked] private NetworkBool NetIsFallingHelpless { get; set; }
    [Networked] private float NetShieldScale { get; set; }

    private bool useNetworked = false;

    // Networked プロパティが安全に読めるかを判定
    private bool IsNetActive => useNetworked && Object != null && Object.IsValid;

    // ===== 公開プロパティ =====
    public float totalDamage
    {
        get => IsNetActive ? NetTotalDamage : _localTotalDamage;
        set { _localTotalDamage = value; if (IsNetActive && HasStateAuthority) NetTotalDamage = value; }
    }

    public int currentStock
    {
        get => IsNetActive ? NetCurrentStock : _localCurrentStock;
        set { _localCurrentStock = value; if (IsNetActive && HasStateAuthority) NetCurrentStock = value; }
    }

    public bool isStunned
    {
        get => IsNetActive ? (bool)NetIsStunned : _localIsStunned;
        set { _localIsStunned = value; if (IsNetActive && HasStateAuthority) NetIsStunned = value; }
    }

    public bool isGuarding
    {
        get => IsNetActive ? (bool)NetIsGuarding : _localIsGuarding;
        set { _localIsGuarding = value; if (IsNetActive && HasStateAuthority) NetIsGuarding = value; }
    }

    public bool isShieldBroken
    {
        get => IsNetActive ? (bool)NetIsShieldBroken : _localIsShieldBroken;
        set { _localIsShieldBroken = value; if (IsNetActive && HasStateAuthority) NetIsShieldBroken = value; }
    }

    public bool isFallingHelpless
    {
        get => IsNetActive ? (bool)NetIsFallingHelpless : _localIsFallingHelpless;
        set { _localIsFallingHelpless = value; if (IsNetActive && HasStateAuthority) NetIsFallingHelpless = value; }
    }

    public float shieldScale
    {
        get => IsNetActive ? NetShieldScale : _localShieldScale;
        set { _localShieldScale = value; if (IsNetActive && HasStateAuthority) NetShieldScale = value; }
    }

    // ===== 内部参照 =====
    private KnockbackCalculator calculator;
    private Rigidbody2D rb;
    private GuardShield shield;
    private CapsuleCollider2D myCollider;
    private ActorController actorController;
    private SpriteRenderer sr;

    private TickTimer breakRecoverTimer;
    private TickTimer stunRecoverTimer;
    private bool refsInitialized = false;

    private void InitializeReferences()
    {
        if (refsInitialized) return;
        refsInitialized = true;
        calculator = new KnockbackCalculator();
        rb = GetComponent<Rigidbody2D>();
        shield = GetComponentInChildren<GuardShield>(true);
        myCollider = GetComponent<CapsuleCollider2D>();
        actorController = GetComponent<ActorController>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        InitializeReferences();

        // Spawned() がまだ呼ばれてなければオフライン初期化
        if (!useNetworked)
        {
            _localCurrentStock = initialStock;
            _localTotalDamage = 0;
            _localShieldScale = shield != null ? shield.maxScale : 1f;
        }

        if (myCollider != null && frictionlessMaterial != null)
        {
            myCollider.sharedMaterial = frictionlessMaterial;
        }
    }

    public override void Spawned()
    {
        useNetworked = true;
        InitializeReferences();

        if (myCollider != null && frictionlessMaterial != null)
        {
            myCollider.sharedMaterial = frictionlessMaterial;
        }

        if (HasStateAuthority)
        {
            currentStock = initialStock;
            totalDamage = 0;
            shieldScale = shield != null ? shield.maxScale : 1f;
        }
    }

    // ===== オンライン用 =====
    public override void FixedUpdateNetwork()
    {
        if (!useNetworked || !HasStateAuthority || shield == null) return;

        UpdateShieldLogic(Runner.DeltaTime);

        if (breakRecoverTimer.Expired(Runner))
        {
            isShieldBroken = false;
            isStunned = false;
            shieldScale = shield.maxScale;
            breakRecoverTimer = TickTimer.None;
        }
        if (stunRecoverTimer.Expired(Runner))
        {
            if (!isShieldBroken) isStunned = false;
            stunRecoverTimer = TickTimer.None;
        }
    }

    public override void Render()
    {
        if (!useNetworked) return;
        UpdatePhysicsMaterial();
        UpdateShieldVisual();
        UpdateBodyColor();
    }

    // ===== オフライン用 =====
    void Update()
    {
        if (useNetworked) return;
        if (shield == null) return;

        UpdatePhysicsMaterial();
        UpdateShieldVisual();
        UpdateBodyColor();
        UpdateShieldLogic(Time.deltaTime);
    }

    // ===== 共通ロジック =====
    private void UpdateShieldLogic(float deltaTime)
    {
        if (isGuarding && !isShieldBroken)
        {
            shieldScale = Mathf.Clamp(shieldScale - shrinkSpeed * deltaTime, shield.minScale, shield.maxScale);
            if (shieldScale <= shield.minScale) ShieldBreak();
        }
        else if (!isShieldBroken)
        {
            shieldScale = Mathf.Clamp(shieldScale + recoverSpeed * deltaTime, shield.minScale, shield.maxScale);
        }
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
            myCollider.sharedMaterial = highFrictionMaterial;
        else
            myCollider.sharedMaterial = frictionlessMaterial;
    }

    public void ShieldBreak()
    {
        isShieldBroken = true;
        isGuarding = false;
        isStunned = true;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (useNetworked)
            breakRecoverTimer = TickTimer.CreateFromSeconds(Runner, 3.0f);
        else
        {
            CancelInvoke("RecoverFromBreak");
            Invoke("RecoverFromBreak", 3.0f);
        }
    }

    void RecoverFromBreak()
    {
        if (useNetworked) return;
        isShieldBroken = false;
        isStunned = false;
        if (shield != null) shieldScale = shield.maxScale;
    }

    /// <summary>HitArea から呼ばれる統一エントリ。モードに応じて RPC/直適用を切替。</summary>
    public void TakeDamage(float baseDamage, float baseKnockback, Vector2 direction)
    {
        if (useNetworked) RPC_TakeDamage(baseDamage, baseKnockback, direction);
        else ApplyDamage(baseDamage, baseKnockback, direction);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(float baseDamage, float baseKnockback, Vector2 direction)
    {
        ApplyDamage(baseDamage, baseKnockback, direction);
    }

    private void ApplyDamage(float baseDamage, float baseKnockback, Vector2 direction)
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

        if (useNetworked)
            stunRecoverTimer = TickTimer.CreateFromSeconds(Runner, 0.5f);
        else
        {
            CancelInvoke("Recover");
            Invoke("Recover", 0.5f);
        }
    }

    void Recover()
    {
        if (useNetworked) return;
        if (!isShieldBroken) isStunned = false;
    }

    public void TakeShieldDamage(float damage, float multiplier)
    {
        if (useNetworked) RPC_TakeShieldHit(damage, multiplier);
        else ApplyShieldDamage(damage, multiplier);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeShieldHit(float damage, float multiplier)
    {
        ApplyShieldDamage(damage, multiplier);
    }

    private void ApplyShieldDamage(float damage, float multiplier)
    {
        if (shield == null) return;
        shieldScale = Mathf.Clamp(shieldScale - damage * multiplier, shield.minScale, shield.maxScale);
    }

    public void OnKilled()
    {
        if (useNetworked && !HasStateAuthority) return;
        currentStock--;
        totalDamage = 0;
        isFallingHelpless = false;
        isStunned = false;
    }
}