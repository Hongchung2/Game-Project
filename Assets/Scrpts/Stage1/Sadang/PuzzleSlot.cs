using UnityEngine;
using UnityEngine.UI;

public class PuzzleSlot : MonoBehaviour
{
    public string slotDirection; // 동, 서, 남, 북
    public Image displayImage; // 슬롯에 현재 표시될 이미지
    public Sprite[] pictures; // 그림 목록 (나무, 장승, 도자기, 장독대, 대문, 모자)
    public string[] pictureNames; // 그림 이름 목록

    int _currentIndex = 0;
    void Start()
    {
       UpdateDisplay();
    }

    public void OnClickedLeft()
    {
        _currentIndex--;
        if (_currentIndex < 0) _currentIndex = pictures.Length - 1;
        UpdateDisplay();
    }

    public void OnClickedRight()
    {
        _currentIndex++;
        if (_currentIndex >= pictures.Length) _currentIndex = 0;
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (pictures.Length == 0) return;
        displayImage.sprite = pictures[_currentIndex];
    }

    public string GetCurrentPictureName()
    {
        return pictureNames[_currentIndex];
    }

    public void ResetSlot()
    {
        _currentIndex = 0;
        UpdateDisplay();
    }
}
