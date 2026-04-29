using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    [Header("ステータス")]
    public float totalDamage = 0f; // 蓄積ダメージ(%)
    public bool isGuarding = false;
    public bool isStunned = false; // 吹っ飛び中で操作不能か
    
    // 吹っ飛び計算クラス（後述）を参照
    private KnockbackCalculator calculator;
    private Rigidbody2D rb;

    void Start()
    {
        calculator = new KnockbackCalculator();
        rb = GetComponent<Rigidbody2D>();
    }

    // ダメージを受ける処理
    public void TakeDamage(float baseDamage, float baseKnockback, Vector2 direction)
    {
        if (isGuarding)
        {
            // ガード成功時はダメージ軽減や吹っ飛び無効など（今はリターン）
            return;
        }

        // 1. ダメージ蓄積
        totalDamage += baseDamage;

        // 2. 最終的な吹っ飛び強度を計算クラスから取得
        float finalForce = calculator.Calculate(totalDamage, baseKnockback);

        isStunned = true;

        // 3. 実際に吹っ飛ばす (ActorControllerに任せても良いが、物理挙動なのでここで直接AddForce)
        rb.linearVelocity = Vector2.zero; // 一度速度をリセットして正確に飛ばす
        transform.position += new Vector3(0, 0.1f, 0);
        rb.AddForce(direction.normalized * finalForce, ForceMode2D.Impulse);

        Invoke("Recover", 0.5f); // 0.5秒後に復帰
    }

    void Recover()
    {
        isStunned = false;
    }
}