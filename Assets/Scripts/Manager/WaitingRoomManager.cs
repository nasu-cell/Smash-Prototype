using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// READY を押した時点で NetworkLauncher.StartGame(roomName) を呼んで部屋に参加。
/// 2 人揃ったら NetworkLauncher 側がシーン遷移を行うので、ここでは
/// SceneManager.LoadScene を呼ばないことに注意。
/// </summary>
public class WaitingRoomManager : MonoBehaviour
{
    [Header("1P (自分) UI")]
    public Image myCharImage;
    public Image myReadyBadge;

    [Header("2P (相手) UI")]
    public Image enemyCharImage;
    public Image enemyReadyBadge;

    [Header("共通UI")]
    public TextMeshProUGUI readyButtonText;
    public Button readyButton;
    public TextMeshProUGUI statusText; // 任意：「相手を待っています」など表示

    [Header("キャラ画像リスト")]
    public Sprite[] characterSprites;

    private bool isReady = false;
    private bool isConnecting = false;

    void Start()
    {
        // 自分のキャラ画像を表示
        if (GameDataContainer.instance != null)
        {
            int id = GameDataContainer.instance.mySelectedCharID;
            if (id >= 0 && id < characterSprites.Length && myCharImage != null)
            {
                myCharImage.sprite = characterSprites[id];
            }
        }

        // 初期状態
        if (enemyReadyBadge != null) enemyReadyBadge.color = Color.gray;
        if (myReadyBadge != null) myReadyBadge.color = Color.gray;
        SetStatus("");

        // NetworkLauncher のイベントを購読（接続後の人数更新を受け取る）
        if (NetworkLauncher.Instance != null)
        {
            NetworkLauncher.Instance.OnPlayerCountChanged += HandlePlayerCountChanged;
        }
    }

    void OnDestroy()
    {
        if (NetworkLauncher.Instance != null)
        {
            NetworkLauncher.Instance.OnPlayerCountChanged -= HandlePlayerCountChanged;
        }
    }

    public async void OnClickReady()
    {
        if (isReady || isConnecting) return;
        isReady = true;
        isConnecting = true;

        if (myReadyBadge != null) myReadyBadge.color = Color.green;
        if (readyButtonText != null) readyButtonText.text = "READY OK!";
        if (readyButton != null) readyButton.interactable = false;
        SetStatus("接続中...");

        if (NetworkLauncher.Instance == null)
        {
            Debug.LogError("NetworkLauncher が見つかりません。最初のシーンに 1 つ配置してください。");
            ResetReady();
            return;
        }

        string roomName = (GameDataContainer.instance != null)
            ? GameDataContainer.instance.roomName
            : "default";

        bool ok = await NetworkLauncher.Instance.StartGame(roomName);
        if (!ok)
        {
            SetStatus("接続失敗。もう一度試してください。");
            ResetReady();
            return;
        }

        SetStatus("相手を待っています...");
        // この後、相手が参加→マスターが LoadScene→OnlineBattleScene へ自動遷移
    }

    private void HandlePlayerCountChanged(int count)
    {
        if (enemyReadyBadge != null && count >= 2)
        {
            enemyReadyBadge.color = Color.green;
            SetStatus("対戦相手が見つかりました！");
        }
    }

    private void ResetReady()
    {
        isReady = false;
        isConnecting = false;
        if (myReadyBadge != null) myReadyBadge.color = Color.gray;
        if (readyButtonText != null) readyButtonText.text = "READY?";
        if (readyButton != null) readyButton.interactable = true;
    }

    private void SetStatus(string s)
    {
        if (statusText != null) statusText.text = s;
    }
}