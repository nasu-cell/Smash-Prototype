using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

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

    [Header("キャラ画像リスト")]
    public Sprite[] characterSprites; // インスペクターでID順に登録

    private bool isReady = false;

    void Start()
    {
        // 自分のキャラ画像を表示
        if (GameDataContainer.instance != null)
        {
            int id = GameDataContainer.instance.mySelectedCharID;
            if (id >= 0 && id < characterSprites.Length)
            {
                myCharImage.sprite = characterSprites[id];
            }
        }
    }

    public void OnClickReady()
    {
        isReady = !isReady;
        
        // 自分のバッジの色を変える
        myReadyBadge.color = isReady ? Color.green : Color.gray;
        readyButtonText.text = isReady ? "READY OK!" : "READY?";

        if (isReady)
        {
            // 本来は相手を待つが、今はテスト用に1秒後にバトルへ
            Invoke("StartBattle", 1.0f);
        }
    }

    void StartBattle()
    {
        SceneManager.LoadScene("OnlineBattleScene");
    }
}