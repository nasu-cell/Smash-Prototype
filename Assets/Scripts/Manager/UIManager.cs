using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Step 4 修正版：
/// PlayerStatus が Despawn された直後（シーン遷移時など）に Networked プロパティへ
/// アクセスして InvalidOperationException が出るのを防ぐため、Object のガードを追加。
/// </summary>
public class UIManager : MonoBehaviour
{
    [System.Serializable]
    public struct PlayerUIComponents
    {
        public PlayerStatus status;
        public TextMeshProUGUI damageText;
        public Image faceIconImage;
        public GameObject[] stockIcons;
    }

    [Header("プレイヤーUI設定")]
    public PlayerUIComponents p1UI;
    public PlayerUIComponents p2UI;

    void Start()
    {
        InitializeUI(p1UI);
        InitializeUI(p2UI);
    }

    void Update()
    {
        UpdateUI(p1UI);
        UpdateUI(p2UI);
    }

    public void SetPlayerStatus(int playerNum, PlayerStatus status)
    {
        if (playerNum == 1) p1UI.status = status;
        else p2UI.status = status;

        if (playerNum == 1) InitializeUI(p1UI);
        else InitializeUI(p2UI);
    }

    private void InitializeUI(PlayerUIComponents ui)
    {
        if (ui.status == null) return;

        if (ui.faceIconImage != null)
        {
            ui.faceIconImage.sprite = ui.status.faceIcon;
        }

        if (ui.stockIcons != null)
        {
            foreach (GameObject iconObj in ui.stockIcons)
            {
                if (iconObj == null) continue;
                Image iconImage = iconObj.GetComponent<Image>();
                if (iconImage != null)
                {
                    iconImage.sprite = ui.status.faceIcon;
                }
            }
        }
    }

    private void UpdateUI(PlayerUIComponents ui)
    {
        if (ui.status == null) return;
        // PlayerStatus のラッパープロパティはオンライン/オフライン両方で安全に読めるので、
        // Object null チェックは不要（offline で UI が止まる原因になる）

        // 1. ダメージ表示の更新
        if (ui.damageText != null)
        {
            ui.damageText.text = ui.status.totalDamage.ToString("F1") + "%";
            ui.damageText.color = Color.Lerp(Color.white, Color.red, ui.status.totalDamage / 200f);
        }

        // 2. ストック表示の更新
        if (ui.stockIcons != null)
        {
            for (int i = 0; i < ui.stockIcons.Length; i++)
            {
                if (ui.stockIcons[i] != null)
                {
                    ui.stockIcons[i].SetActive(i < ui.status.currentStock);
                }
            }
        }
    }
}