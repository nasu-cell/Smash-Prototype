using UnityEngine;

public class ActorController : MonoBehaviour
{
    private Rigidbody2D rigidbody2D;
    private SpriteRenderer spriteRenderer;
    private FighterCombat fighterCombat;
    private PlayerStatus playerStatus; // キャッシュ用
    public bool isMine = false;

    [Header("入力設定")]
    public KeyCode leftKey;
    public KeyCode rightKey;
    public KeyCode upKey;
    public KeyCode downKey;
    public KeyCode jumpKey;
    public KeyCode attackKey;
    public KeyCode specialKey;
    public KeyCode guardKey;

    [Header("移動関連変数")]
    public float xSpeed;
    public bool rightFacing;
    public float remainJumpTime;

    [Header("接地判定・空中ジャンプ")]
    public bool isGround;
    public int jumpCount = 0;
    public int jumpMaxCount = 2;

    [Header("ガード処理")]
    public GuardShield guardShield;

    void Start()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        fighterCombat = GetComponent<FighterCombat>();
        playerStatus = GetComponent<PlayerStatus>();

        if (guardShield == null)
        {
            guardShield = GetComponentInChildren<GuardShield>(true);
        }

        rightFacing = true;
    }

    void Update()
    {
        if (!isMine) return;
         // 接地したら「しりもち落下」を解除
         if (isGround && playerStatus.isFallingHelpless)
         {
            playerStatus.isFallingHelpless = false;
         }

         // ガード解除
         if (playerStatus.isGuarding && Input.GetKeyUp(guardKey))
         {
            playerStatus.isGuarding = false;
         }

         // 行動不能チェック（しりもち落下を追加）
         if (playerStatus.isShieldBroken || playerStatus.isStunned || playerStatus.isGuarding || playerStatus.isFallingHelpless)
         {
             // しりもち落下中のみ、空中での左右移動だけは許可する場合
             if (playerStatus.isFallingHelpless)
             {
                AirMoveUpdate(); // 左右移動のみの簡易メソッド（後述）
             }
             else
             {
                xSpeed = 0;
             }
             return; // 他のJumpやCombatは受け付けない
         }

         MoveUpdate();
         JumpUpdate();
         CombatUpdate();
    }

    // しりもち落下中の左右微調整用
    private void AirMoveUpdate()
    {
        if (Input.GetKey(rightKey))
        {
            xSpeed = 4.0f; // 通常より少し遅くしても良い
        }
        else if (Input.GetKey(leftKey))
        {
            xSpeed = -4.0f;
        }
        else
        {
            xSpeed = 0;
        }
    }


    // --- 接地判定の統合窓口 ---
    public void SetGrounded(bool grounded)
    {
        isGround = grounded;
        if (isGround)
        {
            jumpCount = 0;
        }
        else if (jumpCount == 0)
        {
            jumpCount = 1;
        }
    }

    // --- 本体コライダーによる衝突検知 ---
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            SetGrounded(true);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGround = true; // Stay中もフラグを維持
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            SetGrounded(false);
        }
    }

    private void MoveUpdate()
    {
        if (Input.GetKey(rightKey))
        {
            xSpeed = 6.0f;
            rightFacing = true;
            spriteRenderer.flipX = false;
        }
        else if (Input.GetKey(leftKey))
        {
            xSpeed = -6.0f;
            rightFacing = false;
            spriteRenderer.flipX = true;
        }
        else
        {
            xSpeed = 0.0f;
        }
    }

   private void CombatUpdate()
{
    if (Input.GetKeyDown(guardKey) && !playerStatus.isShieldBroken)
    {
        playerStatus.isGuarding = true;
    }

    // 攻撃ボタンが押された瞬間
    if (Input.GetKeyDown(attackKey))
    {
        // 1. 上入力 + 攻撃
        if (Input.GetKey(upKey))
        {
            fighterCombat.PerformUpAttack(rightFacing);
        }
        // 2. 右入力 + 攻撃
        else if (Input.GetKey(rightKey))
        {
            rightFacing = true;
            spriteRenderer.flipX = false;
            fighterCombat.PerformSideAttack(true);
        }
        // 3. 左入力 + 攻撃
        else if (Input.GetKey(leftKey))
        {
            rightFacing = false;
            spriteRenderer.flipX = true;
            fighterCombat.PerformSideAttack(false);
        }
        // 4. ニュートラル（今後実装）
        else
        {
            Debug.Log("Neutral Attack - Pending");
        }
    }

    if (Input.GetKeyDown(specialKey))
    {
        // 上＋必殺技
        if (Input.GetKey(upKey))
        {
            fighterCombat.PerformUpSpecial(rightFacing);
        }
        else
        {
            fighterCombat.PerformSpecial(rightFacing);
        }
    }
}

    private void JumpUpdate()
    {
        if (remainJumpTime > 0.0f) remainJumpTime -= Time.deltaTime;

        if (Input.GetKeyDown(jumpKey))
        {
            if (!isGround && jumpCount >= jumpMaxCount) return;
            float jumpPower = 10.0f;
            rigidbody2D.linearVelocity = new Vector2(rigidbody2D.linearVelocity.x, jumpPower);
            jumpCount++;
            remainJumpTime = 0.25f;
        }
        else if (Input.GetKey(jumpKey))
        {
            if (remainJumpTime <= 0.0f || isGround) return;
            float jumpAddPower = 30.0f * Time.deltaTime;
            rigidbody2D.linearVelocity += new Vector2(0.0f, jumpAddPower);
        }
        else if (Input.GetKeyUp(jumpKey))
        {
            remainJumpTime = -1.0f;
        }
    }

   private void FixedUpdate()
   {
        // ★ Step 2 追加：リモート側はネットワーク同期に任せて何もしない
        if (!isMine) return;

        if (playerStatus.isStunned) return;

        Vector2 velocity = rigidbody2D.linearVelocity;

        if (playerStatus.isFallingHelpless)
        {
            // しりもち落下中の処理
            if (Mathf.Abs(xSpeed) > 0.1f)
            {
                 // キー入力がある場合：現在の勢いを維持しつつ、少しずつ入力方向に補正する
                 // 10.0f の値を大きくすると、空中での制御が効きやすくなります
                 velocity.x = Mathf.MoveTowards(velocity.x, xSpeed, Time.fixedDeltaTime * 10.0f);
            }
            else
            {
                 // キー入力がない場合：何もしない（物理演算の慣性に任せて放物線を描く）
                 // もし急激に止まりすぎるならここは何もしなくてOKです
            }
        }
        else
        {
            // 通常時の処理：地面や通常の空中移動ではキビキビ動くように上書き
            velocity.x = xSpeed;
        }
        rigidbody2D.linearVelocity = velocity;
    }
}