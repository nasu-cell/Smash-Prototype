using UnityEngine;

public class ActorController : MonoBehaviour
{
    private Rigidbody2D rigidbody2D;
    private SpriteRenderer spriteRenderer;
    private FighterCombat fighterCombat; // 追加

    [Header("入力設定")]
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode upKey;
    public KeyCode downKey;
    public KeyCode jumpKey = KeyCode.Space;
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

    void Start()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        fighterCombat = GetComponent<FighterCombat>(); // 追加

        rightFacing = true;
    }

    void Update()
    {
        if (GetComponent<PlayerStatus>().isStunned) 
        {
            xSpeed = 0; // 移動速度もリセット
            return; 
        }

        MoveUpdate();
        JumpUpdate();
        CombatUpdate(); // 追加
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
        // 攻撃ボタンが押されたとき
        if (Input.GetKeyDown(attackKey))
        {
            if (Input.GetKey(rightKey)) 
            {
                fighterCombat.PerformAttack(true);  // 右攻撃
            }
            else if (Input.GetKey(leftKey)) 
            {
                fighterCombat.PerformAttack(false); // 左攻撃
            }
        }

        // 必殺ボタンが押されたとき
        if (Input.GetKeyDown(specialKey))
        {
            if (Input.GetKey(rightKey)) 
            {
                fighterCombat.PerformSpecial(true);  // 右必殺
            }
            else if (Input.GetKey(leftKey)) 
            {
                fighterCombat.PerformSpecial(false); // 左必殺
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
        if (GetComponent<PlayerStatus>().isStunned) 
        {
            return; 
        }
        Vector2 velocity = rigidbody2D.linearVelocity;
        velocity.x = xSpeed;
        rigidbody2D.linearVelocity = velocity;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGround = true;
            jumpCount = 0;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGround = false;
            jumpCount = 1;
        }
    }
}