using System.Collections;
using UnityEngine;

public class SummerResetBell : MonoBehaviour, IInteractable
{
    [Header("연출")]
    public float fadeDuration = 0.5f;

    private bool _isResetting = false;

    public string GetInteractText()
    {
        return "F - 초기화";
    }

    public void OnInteract()
    {
        if (_isResetting) return;
        _isResetting = true;
        StartCoroutine(ResetSequence());
    }

    private IEnumerator ResetSequence()
    {
        Debug.Log("초기화 종 울림! 석상이 원위치로 돌아갑니다.");

        // 1) 화면을 검게 페이드아웃
        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeOut(Color.black, fadeDuration);
        else
            yield return new WaitForSeconds(fadeDuration);

        // 2) 검은 화면 동안 퍼즐 리셋 (되돌아가는 과정을 가려줌)
        if (SummerPuzzleManager.Instance != null)
            SummerPuzzleManager.Instance.ResetAll();
        else
            Debug.LogError("[종] SummerPuzzleManager.Instance가 null입니다!");

        // 3) 다시 화면 밝아짐 (처음 입장 상태로)
        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeIn(fadeDuration);

        _isResetting = false;
    }
}
