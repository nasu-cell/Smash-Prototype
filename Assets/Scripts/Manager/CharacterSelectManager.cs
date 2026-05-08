using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSelectManager : MonoBehaviour
{
    public UnityEngine.UI.Button okButton;
    public TMPro.TMP_InputField roomInputField;
    private int selectedID = -1; // -1は未選択

    void Start() { okButton.interactable = false; } // 最初は押せない

    public void SelectCharacter(int id) {
        selectedID = id;
        GameDataContainer.instance.mySelectedCharID = id;
        CheckConditions();
    }

    void Update() { CheckConditions(); } // 入力欄の変化を監視

    void CheckConditions() {
        // キャラが選ばれていて、かつ部屋番号が空でなければOKボタンを有効化
        okButton.interactable = (selectedID != -1 && !string.IsNullOrEmpty(roomInputField.text));
    }

    public void OnClickOK() {
        GameDataContainer.instance.roomName = roomInputField.text;
        SceneManager.LoadScene("WaitingRoomScene");
    }
}