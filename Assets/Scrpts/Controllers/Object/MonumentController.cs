using UnityEngine;

public class MonumentController : MonoBehaviour, IInteractable
{
    public string GetInteractText()
    {
        // 매니저를 통해서 현재 남은 계량 횟수를 계산해 텍스트로 보여줍니다!
        if (SpringPuzzleManager.Instance != null)
        {
            int remainCount = SpringPuzzleManager.MAX_WEIGH_COUNT - SpringPuzzleManager.Instance.weighCount;

            if (remainCount > 0)
                return $"저울 작동하기 (남은 횟수: {remainCount})";
            else
                return "작동 불가 (횟수 소진)";
        }

        return "저울 작동하기";
    }

    public void OnInteract()
    {
        Debug.Log("--- 1. 비석 상호작용(버튼 누르기) 실행됨! ---");

        if (SpringPuzzleManager.Instance != null)
        {
            SpringPuzzleManager.Instance.Weigh();
        }
        else
        {
            Debug.LogError("--- [에러] SpringPuzzleManager.Instance가 비어있습니다! ---");
        }
    }
}