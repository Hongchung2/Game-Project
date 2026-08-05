using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance;

    public PuzzleSlot[] slots; // 동서남북 슬롯 4개
    public GameObject puzzlePanel;
    public DoorController sadangDoor;
    public GameObject gameCanvas; // 기존 게임 UI Canvas
    public GameObject puzzleCanvas; // 퍼즐 canvas

    // 정답
    readonly string[] answers = {"나무", "도자기", "대문", "장독대"}; // 동서남북 순서

    void Awake()
    {
        Instance = this;
        puzzleCanvas.SetActive(false);
    }
    public void OpenPuzzle()
    {
        puzzleCanvas.SetActive(true);
        gameCanvas.SetActive(false);
        Time.timeScale = 0f; // 게임 멈춤
    }
    public void CheckAnswer()
    {
        for (int i=0; i<slots.Length; i++)
        {
            if (slots[i].GetCurrentPictureName() != answers[i])
            {
                Debug.Log("오답");
                if (CenterMessageUI.Instance != null)
                    CenterMessageUI.Instance.Show("오답이다.", 1.5f);
                return;
            }
        }

        Debug.Log("정답");
        if (CenterMessageUI.Instance != null)
            CenterMessageUI.Instance.Show("정답이다!", 1.5f);
        puzzleCanvas.SetActive(false);
        gameCanvas.SetActive(true);
        Time.timeScale = 1f; // 게임 재개
        sadangDoor.SetDoorLocked(false);
    }

    public void ResetPuzzle()
    {
        foreach(var slot in slots)
        {
            slot.ResetSlot();
        }
    }

    public void ClosePuzzle()
    {
        ResetPuzzle();
        puzzleCanvas.SetActive(false);
        gameCanvas.SetActive(true);
        Time.timeScale = 1f;
    }
    
}
