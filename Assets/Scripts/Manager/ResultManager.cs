using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class ResultManager : MonoBehaviour
{
    [Header("UI要素（勝者用）")]
    public Image winnerIcon;
    public TextMeshProUGUI winnerText;

    [Header("UI要素（敗者用）")]
    public Image loserIcon;
    public TextMeshProUGUI loserText;

    [Header("遷移先設定")]
    public string nextSceneName; // インスペクターで ModeSelectScene か WaitingRoomScene を指定

    void Start()
    {
        SetupResult();
    }

    void SetupResult()
    {
        var data = GameDataContainer.instance;
        if (data == null) return;

        // P1が勝った場合
        if (data.winnerPlayerNum == 1)
        {
            // 勝者にP1の情報を代入
            winnerIcon.sprite = data.p1Icon;
            winnerText.text = "1st " + data.p1Name;
            // 敗者にP2の情報を代入
            loserIcon.sprite = data.p2Icon;
            loserText.text = "2nd " + data.p2Name;
        }
        // P2が勝った場合
        else
        {
            // 勝者にP2の情報を代入
            winnerIcon.sprite = data.p2Icon;
            winnerText.text = "1st " + data.p2Name;
            // 敗者にP1の情報を代入
            loserIcon.sprite = data.p1Icon;
            loserText.text = "2nd " + data.p1Name;
        }
    }

    void SetUI(Image img, TextMeshProUGUI txt, Sprite icon, string label)
    {
        if (img != null) img.sprite = icon;
        if (txt != null) txt.text = label;
    }

    // OKボタンに割り当てる関数
    public void OnOkButtonClicked()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}