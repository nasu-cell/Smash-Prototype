using UnityEngine;

public class GameDataContainer : MonoBehaviour
{
    public static GameDataContainer instance;

    [Header("共有データ")]
    public string roomName;      // 部屋番号
    public int mySelectedCharID; // 自分が選んだキャラID
    public bool isP1;            // 自分が1P（ホスト）か2P（ゲスト）か
    [Header("リザルト用データ")]
    public int winnerPlayerNum; 
    public Sprite p1Icon;       
    public Sprite p2Icon;       
    public string p1Name; 
    public string p2Name; 
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}