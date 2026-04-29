public class KnockbackCalculator
{
    // スマブラ風の簡略化した計算式
    // (蓄積ダメージと技の基礎吹っ飛び値から、最終的な力を出す)
    public float Calculate(float currentDamage, float baseKnockback)
    {
        // 例: (蓄積 / 10 + 1) * 基礎値
        // 100%溜まっていると、基礎値の11倍の力で飛ぶ
        float multiplier = (currentDamage * 0.1f) + 1.0f;
        return baseKnockback * multiplier;
    }
}