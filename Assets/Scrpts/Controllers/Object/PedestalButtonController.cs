using UnityEngine;

public class PedestalButtonController : MonoBehaviour, IInteractable
{
    public string GetInteractText()
    {
        if (AnswerPedestal.Instance != null && AnswerPedestal.Instance.placedJar != null)
        {
            return "정답 제출하기 (단상 가동)";
        }
        return "단상 위에 항아리를 올려주세요";
    }

    public void OnInteract()
    {
        if (AnswerPedestal.Instance != null)
        {
            // 단상의 상승 연출을 발동시킵니다!
            AnswerPedestal.Instance.StartRisingPresentation();
        }
    }
}