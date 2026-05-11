using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 新しい流れ：
/// - シーン入室で自動接続
/// - 両者の PlayerLobbyInfo を表示
/// - READY ボタンで自分の IsReady をトグル
/// - 両者の IsReady が true になったらマスターがバトル開始
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
    public TextMeshProUGUI statusText;

    [Header("キャラ画像リスト")]
    public Sprite[] characterSprites;

    private PlayerLobbyInfo myInfo;
    private PlayerLobbyInfo opponentInfo;
    private bool battleStarted = false;
    private bool isConnecting = false;
    private bool myReady = false;

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

        if (enemyReadyBadge != null) enemyReadyBadge.color = Color.gray;
        if (myReadyBadge != null) myReadyBadge.color = Color.gray;
        if (readyButton != null) readyButton.interactable = false; // 接続まで無効
        if (readyButtonText != null) readyButtonText.text = "READY?";

        // NetworkLauncher のイベントを購読
        if (NetworkLauncher.Instance != null)
        {
            NetworkLauncher.Instance.OnOpponentDisconnected += HandleOpponentDisconnected;
        }

        // ★ 入室と同時に自動接続
        AutoConnect();
    }

    void OnDestroy()
    {
        if (NetworkLauncher.Instance != null)
        {
            NetworkLauncher.Instance.OnOpponentDisconnected -= HandleOpponentDisconnected;
        }
    }

    private async void AutoConnect()
    {
        if (isConnecting) return;
        isConnecting = true;
        SetStatus("接続中...");

        if (NetworkLauncher.Instance == null)
        {
            Debug.LogError("NetworkLauncher が見つかりません。");
            SetStatus("接続失敗：NetworkLauncher 未配置");
            return;
        }

        string roomName = (GameDataContainer.instance != null)
            ? GameDataContainer.instance.roomName
            : "default";

        bool ok = await NetworkLauncher.Instance.StartGame(roomName);
        if (!ok)
        {
            SetStatus("接続失敗。タイトルに戻ってもう一度試してください。");
            return;
        }

        SetStatus("相手を待っています...");
        // 接続完了したので READY ボタンを有効化
        if (readyButton != null) readyButton.interactable = true;
    }

    public void OnClickReady()
    {
        if (myInfo == null)
        {
            Debug.LogWarning("[WaitingRoomManager] まだ自分の PlayerLobbyInfo が未登録です。");
            return;
        }

        myReady = !myReady;
        myInfo.SetReady(myReady);

        if (myReadyBadge != null) myReadyBadge.color = myReady ? Color.green : Color.gray;
        if (readyButtonText != null) readyButtonText.text = myReady ? "READY OK!" : "READY?";

        Debug.Log($"[WaitingRoomManager] 自分の READY = {myReady}");
    }

    /// <summary>
    /// PlayerLobbyInfo.Spawned() から呼ばれる。
    /// 自分のものなら myInfo に、相手のものなら opponentInfo に保存。
    /// </summary>
    public void RegisterLobbyInfo(PlayerLobbyInfo info)
    {
        if (info == null) return;

        if (info.HasInputAuthority)
        {
            myInfo = info;
            Debug.Log("[WaitingRoomManager] 自分の LobbyInfo を保持");
        }
        else
        {
            opponentInfo = info;
            int id = info.CharacterId;
            if (id >= 0 && id < characterSprites.Length && enemyCharImage != null)
            {
                enemyCharImage.sprite = characterSprites[id];
                Debug.Log($"[WaitingRoomManager] 相手アイコン表示 (CharId={id})");
            }
            SetStatus("対戦相手が入室しました。両者 READY でバトル開始！");
        }
    }

    void Update()
    {
        // 相手の READY バッジ更新
        if (opponentInfo != null && opponentInfo.Object != null && opponentInfo.Object.IsValid)
        {
            bool oppReady = (bool)opponentInfo.IsReady;
            if (enemyReadyBadge != null)
            {
                enemyReadyBadge.color = oppReady ? Color.green : Color.gray;
            }
        }

        // 両者 READY 確認（マスターのみシーン遷移発火）
        if (battleStarted) return;
        if (NetworkLauncher.Instance?.Runner == null) return;
        if (!NetworkLauncher.Instance.Runner.IsSharedModeMasterClient) return;
        if (myInfo == null || opponentInfo == null) return;
        if (myInfo.Object == null || !myInfo.Object.IsValid) return;
        if (opponentInfo.Object == null || !opponentInfo.Object.IsValid) return;

        if ((bool)myInfo.IsReady && (bool)opponentInfo.IsReady)
        {
            battleStarted = true;
            Debug.Log("[WaitingRoomManager] 両者 READY → バトル遷移リクエスト");
            NetworkLauncher.Instance.RequestBattleSceneLoad();
        }
    }

    private void HandleOpponentDisconnected()
    {
        SetStatus("相手が切断しました。タイトルに戻って再接続してください。");
        if (enemyReadyBadge != null) enemyReadyBadge.color = Color.gray;
        if (enemyCharImage != null) enemyCharImage.sprite = null;
        opponentInfo = null;
        myReady = false;
        if (myReadyBadge != null) myReadyBadge.color = Color.gray;
        if (readyButtonText != null) readyButtonText.text = "READY?";
        if (myInfo != null) myInfo.SetReady(false);
    }

    private void SetStatus(string s)
    {
        if (statusText != null) statusText.text = s;
    }
}