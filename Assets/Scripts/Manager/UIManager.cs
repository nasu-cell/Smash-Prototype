using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    // 各プレイヤーのUI要素をまとめる構造体
    [System.Serializable]
    public struct PlayerUIComponents
    {
        public PlayerStatus status;           // 参照するStatus
        public TextMeshProUGUI damageText;    // ダメージテキスト
        public Image faceIconImage;           // 顔アイコン画像
        public GameObject[] stockIcons;       // ストック画像（3つなら要素数3の配列）
    }

    [Header("プレイヤーUI設定")]
    public PlayerUIComponents p1UI;
    public PlayerUIComponents p2UI;

    void Start()
    {
        // ゲーム開始時に顔アイコンを設定
        InitializeUI(p1UI);
        InitializeUI(p2UI);
    }

    void Update()
    {
        // 毎フレーム表示を更新
        UpdateUI(p1UI);
        UpdateUI(p2UI);
    }

    public void SetPlayerStatus(int playerNum, PlayerStatus status)
    {
        if (playerNum == 1) p1UI.status = status;
        else p2UI.status = status;
        
        // アイコンなどの初期化を走らせる
        if (playerNum == 1) InitializeUI(p1UI);
        else InitializeUI(p2UI);
    }

    private void InitializeUI(PlayerUIComponents ui)
    {
        if (ui.status != null)
        {
            /* 1. メインの顔アイコンをセット*/
            if (ui.faceIconImage != null)
            {
                ui.faceIconImage.sprite = ui.status.faceIcon;
            }

            /* 2. ストックアイコンをすべてキャラのアイコンに差し替える (追加)*/
            if (ui.stockIcons != null)
            {
                foreach (GameObject iconObj in ui.stockIcons)
                {
                    if (iconObj == null) continue;
                
                    // ストックアイコンのオブジェクトについている Image コンポーネントを取得
                    Image iconImage = iconObj.GetComponent<Image>();
                    if (iconImage != null)
                    {
                        iconImage.sprite = ui.status.faceIcon; // Statusに設定された顔画像にする
                    }
                }
            }
        }
    }

    private void UpdateUI(PlayerUIComponents ui)
    {
        if (ui.status == null) return;

        // 1. ダメージ表示の更新
        if (ui.damageText != null)
        {
            ui.damageText.text = ui.status.totalDamage.ToString("F1") + "%";
            // ダメージが増えるほど白 -> 赤に変化
            ui.damageText.color = Color.Lerp(Color.white, Color.red, ui.status.totalDamage / 200f);
        }

        // 2. ストック表示の更新
        if (ui.stockIcons != null)
        {
            for (int i = 0; i < ui.stockIcons.Length; i++)
            {
                if (ui.stockIcons[i] != null)
                {
                    // 現在のストック数より小さいインデックスのアイコンだけ表示
                    // 例：ストック2なら、アイコン0と1が表示され、2が消える
                    ui.stockIcons[i].SetActive(i < ui.status.currentStock);
                }
            }
        }
    }
}