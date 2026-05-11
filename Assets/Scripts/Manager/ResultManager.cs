using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Step 5 版：OK ボタンで NetworkRunner を Shutdown してからシーン遷移する。
/// （シャットダウンしないと次回 StartGame で「Runner はすでに起動中」エラー）
/// </summary>
public class ResultManager : MonoBehaviour
{
    [Header("UI要素（勝者用）")]
    public Image winnerIcon;
    public TextMeshProUGUI winnerText;

    [Header("UI要素（敗者用）")]
    public Image loserIcon;
    public TextMeshProUGUI loserText;

    [Header("遷移先設定")]
    public string nextSceneName;

    [Header("OK ボタン")]
    public Button okButton;

    private bool isReturning = false;

    void Start()
    {
        SetupResult();
    }

    void SetupResult()
    {
        var data = GameDataContainer.instance;
        if (data == null) return;

        if (data.winnerPlayerNum == 1)
        {
            if (winnerIcon != null) winnerIcon.sprite = data.p1Icon;
            if (winnerText != null) winnerText.text = "1st " + data.p1Name;
            if (loserIcon != null) loserIcon.sprite = data.p2Icon;
            if (loserText != null) loserText.text = "2nd " + data.p2Name;
        }
        else
        {
            if (winnerIcon != null) winnerIcon.sprite = data.p2Icon;
            if (winnerText != null) winnerText.text = "1st " + data.p2Name;
            if (loserIcon != null) loserIcon.sprite = data.p1Icon;
            if (loserText != null) loserText.text = "2nd " + data.p1Name;
        }
    }

    public void OnOkButtonClicked()
    {
        if (isReturning) return;
        isReturning = true;
        if (okButton != null) okButton.interactable = false;

        // ★ コルーチンは NetworkLauncher（DDOL）で実行
        if (NetworkLauncher.Instance != null)
        {
            NetworkLauncher.Instance.StartCoroutine(
                NetworkLauncher.Instance.ShutdownAndLoadScene(nextSceneName, 0f)
            );
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}