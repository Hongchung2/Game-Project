using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 작업지시서 #09 - 기둥 시퀀스 완료 후 나타나는 포탈. 밟으면 인터미션 씬(#08)으로 이동.
// 건영님: 이 스크립트를 ClearPortal 오브젝트에 붙이고, Collider2D를 추가한 뒤
// Is Trigger를 체크해주세요. ClearPortal은 씬 시작 시 비활성화 상태로 둡니다
// (WitheredTreePortalReveal이 기둥 완료 시 자동으로 켜줍니다).
public class ClearPortalTrigger : MonoBehaviour
{
    public string nextSceneName = "IntermissionScene";
    public float fadeDuration = 0.5f;

    private bool _triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;

        _triggered = true;
        StartCoroutine(EnterSequence());
    }

    private IEnumerator EnterSequence()
    {
        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeOut(Color.black, fadeDuration);

        GameProgress.SetCheckpoint(nextSceneName);
        SceneManager.LoadScene(nextSceneName);
    }
}
