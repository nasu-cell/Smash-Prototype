using UnityEngine;

public class GuardShield : MonoBehaviour
{
    [Header("サイズ・耐久値設定")]
    public float maxScale;     // 最大サイズ
    public float minScale;     // 限界サイズ
    public float currentScale;        // 現在の耐久値（サイズ）

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        currentScale = maxScale;
    }

    public void UpdateShield(float scaleChange)
    {
        currentScale += scaleChange;
        currentScale = Mathf.Clamp(currentScale, minScale, maxScale);
        
        transform.localScale = Vector3.one * currentScale;

        // 見た目の更新
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 0.4f;
            sr.color = c;
        }
    }

    // 攻撃を受けたときにHitAreaから呼ばれる
    public void TakeShieldDamage(float damage, float multiplier)
    {
        currentScale -= damage * multiplier;
    }
}