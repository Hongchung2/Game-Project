using System.Collections;
using UnityEngine;

public class SummerResetBell : MonoBehaviour, IInteractable
{
    public string GetInteractText()
    {
        return "F - 초기화";
    }

    public void OnInteract()
    {
        StartCoroutine(ResetSequence());
    }

    private IEnumerator ResetSequence()
    {
        // 화면 0.5초 페이드아웃 후 리셋
        // TODO: 페이드아웃 연출 추가
        yield return new WaitForSeconds(0.5f);

        if (SummerPuzzleManager.Instance == null)
        {
            Debug.LogError("[종] SummerPuzzleManager.Instance가 null입니다!");
            yield break;
        }

        SummerPuzzleManager.Instance.ResetAll();
        Debug.Log("초기화 종 울림! 석상이 원위치로 돌아갑니다.");
    }
}
