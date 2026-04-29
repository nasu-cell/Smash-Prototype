using UnityEngine;
using TMPro; // TextMeshProを使う場合

public class UIManager : MonoBehaviour
{
    [Header("プレイヤーのStatus参照")]
    public PlayerStatus player1Status;
    public PlayerStatus player2Status;

    [Header("UIテキスト参照")]
    public TextMeshProUGUI p1Text;
    public TextMeshProUGUI p2Text;

    void Update()
    {
        // 毎フレーム、Statusの数値をテキストに反映する
        if (player1Status != null && p1Text != null)
        {
            // 「12.5%」のように小数点第1位まで表示
            p1Text.text = player1Status.totalDamage.ToString("F1") + "%";
            
            // ダメージが溜まるほど赤くする演出（お好みで）
            p1Text.color = Color.Lerp(Color.white, Color.red, player1Status.totalDamage / 200f);
        }

        if (player2Status != null && p2Text != null)
        {
            p2Text.text = player2Status.totalDamage.ToString("F1") + "%";
            p2Text.color = Color.Lerp(Color.white, Color.red, player2Status.totalDamage / 200f);
        }
    }
}