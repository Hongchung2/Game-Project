using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 작업지시서 #09 - 엔딩 연출(페이드아웃 + "탈출에 성공했습니다" + 타이틀 복귀)을 한 곳에 모음.
// 원래 Grandfather.cs 안에 있던 즉시 엔딩 로직을 그대로 옮긴 것 - 이제는 보스 격파 시점에서도 재사용.
public static class GameEndingTrigger
{
    private const float MESSAGE_DURATION = 6f;

    public static void Trigger(MonoBehaviour host)
    {
        // host(=보스)는 격파 직후 비활성화/파괴될 수 있어서 그 위에서 코루틴을 돌리면 엔딩이
        // 중간에 끊긴다(검은 화면에서 멈춤). 전용 오브젝트를 만들어 씬 전환까지 살아있게 한다.
        GameObject runner = new GameObject("GameEndingRunner");
        Object.DontDestroyOnLoad(runner);
        runner.AddComponent<EndingRunner>().Begin();
    }

    private class EndingRunner : MonoBehaviour
    {
        public void Begin() => StartCoroutine(EndingSequence());

        private IEnumerator EndingSequence()
        {
            if (ScreenFader.Instance != null)
                yield return ScreenFader.Instance.FadeOut(Color.black, 1f);

            if (CenterMessageUI.Instance != null)
                CenterMessageUI.Instance.Show("탈출에 성공했습니다", MESSAGE_DURATION);

            Debug.Log("게임 클리어! 탈출에 성공했습니다.");

            // 대사창 등이 timeScale을 0으로 둔 채 끝났을 수 있어 실시간 대기로 기다린다.
            yield return new WaitForSecondsRealtime(MESSAGE_DURATION);

            // 엔딩이 끝나면 타이틀로 복귀 - 안 그러면 검은 화면에서 영영 못 빠져나온다.
            Time.timeScale = 1f;
            SceneManager.LoadScene("GameTitle");
            Destroy(gameObject);
        }
    }
}
