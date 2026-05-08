using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    [Header("基本ステータス")]
    public string playerName = "P1";
    public float totalDamage = 0f;
    public int currentStock = 3;  // 現在のストック
    public Sprite faceIcon;       // キャラクターの顔画像（Inspectorで設定）
    [Header("ステータス")]
    public bool isStunned = false; // 吹っ飛び・硬直中か
    public bool isGuarding = false; // ガード中か
    public bool isShieldBroken = false; // シールドブレイク中か
    public bool isFallingHelpless; // しりもち落下（操作不能）フラグ

    [Header("シールド設定")]
    public float shrinkSpeed; // ガード時の減少速度
    public float recoverSpeed; // 非ガード時の回復速度
    
    [Header("摩擦設定")]
    public PhysicsMaterial2D frictionlessMaterial; // 摩擦 0
    public PhysicsMaterial2D highFrictionMaterial; // 摩擦 1.0 以上

    private KnockbackCalculator calculator;
    private Rigidbody2D rb;
    private GuardShield shield;
    private CapsuleCollider2D myCollider;
    private ActorController actorController;

    void Start()
    {
        calculator = new KnockbackCalculator();
        rb = GetComponent<Rigidbody2D>();
        shield = GetComponentInChildren<GuardShield>(true);
        myCollider = GetComponent<CapsuleCollider2D>();
        actorController = GetComponent<ActorController>();
        
        // 初期状態は摩擦なしに設定
        if (myCollider != null && frictionlessMaterial != null)
        {
            myCollider.sharedMaterial = frictionlessMaterial;
        }
    }

    void Update()
    {
        // 摩擦の動的切り替え
        UpdatePhysicsMaterial();

        if (shield == null) return;

        // 1. シールドの表示切り替え
        shield.gameObject.SetActive(isGuarding && !isShieldBroken);

        // 2. シールドのサイズ管理
        if (isGuarding && !isShieldBroken)
        {
            // ガード中：減少
            shield.UpdateShield(-shrinkSpeed * Time.deltaTime);
            
            // 最小値に達したらブレイク
            if (shield.currentScale <= shield.minScale)
            {
                ShieldBreak();
            }
        }
        else
        {
            // ガードしていない間回復
            if(!isShieldBroken)
            {
                shield.UpdateShield(recoverSpeed * Time.deltaTime);
            }
        }
    }

    /// <summary>
    /// 状況に応じてコライダーの摩擦を切り替える
    /// </summary>
    private void UpdatePhysicsMaterial()
    {
        if (myCollider == null || actorController == null) return;

        // 【条件のポイント】
        // 接地しており、かつ「ガード中」または「ブレイク中」のみ摩擦を強くする。
        // ※ 通常の被弾(isStunnedのみ)の時はここを通らず、摩擦0になる。
        if (actorController.isGround && (isGuarding || isShieldBroken))
        {
            myCollider.sharedMaterial = highFrictionMaterial;
        }
        else
        {
            // 移動中、空中、および「攻撃を受けて吹っ飛んでいる最中」は滑るようにする
            myCollider.sharedMaterial = frictionlessMaterial;
        }
    }

    public void ShieldBreak()
    {
        isShieldBroken = true;
        isGuarding = false; // ブレイクしたらガード強制解除
        isStunned = true;   // ブレイク直後も硬直状態にする
        rb.linearVelocity = Vector2.zero;
        GetComponent<SpriteRenderer>().color = Color.gray;
        
        // 3秒後に復帰
        Invoke("RecoverFromBreak", 3.0f);
    }

    void RecoverFromBreak()
    {
        isShieldBroken = false;
        isStunned = false;
        shield.currentScale = shield.maxScale;
        GetComponent<SpriteRenderer>().color = Color.white;
    }

    // ダメージを受ける処理
    public void TakeDamage(float baseDamage, float baseKnockback, Vector2 direction)
    {
        // ブレイク中の追撃を許さない場合はここでリターン（仕様に合わせて調整）
        if (isShieldBroken) return; 

        // 1. ダメージ蓄積
        totalDamage += baseDamage;

        // 2. 最終的な吹っ飛び強度を計算
        float finalForce = calculator.Calculate(totalDamage, baseKnockback);

        isStunned = true;

        // 3. 実際に吹っ飛ばす
        rb.linearVelocity = Vector2.zero; // 速度をリセットしてノックバックを正確に適用
        transform.position += new Vector3(0, 0.1f, 0); // 地面との摩擦を完全に切るために微浮上
        rb.AddForce(direction.normalized * finalForce, ForceMode2D.Impulse);

        // 0.5秒後に硬直から回復
        CancelInvoke("Recover"); 
        Invoke("Recover", 0.5f);
    }

    void Recover()
    {
        // ガード中やブレイク中でなければスタン解除
        if (!isShieldBroken)
        {
            isStunned = false;
        }
    }
}