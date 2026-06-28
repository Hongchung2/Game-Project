using UnityEngine;

public class ResetBellController : MonoBehaviour, IInteractable
{
    public string GetInteractText()
    {
        return "저울 초기화";
    }

    public void OnInteract()
    {
        Debug.Log("땡! 초기화 종을 울렸습니다.");

        if (SpringPuzzleManager.Instance != null)
        {
            // 매니저의 총괄 초기화 함수 호출!
            SpringPuzzleManager.Instance.ResetAll();
        }
    }
}