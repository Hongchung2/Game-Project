using System.Collections;
using UnityEngine;

// 작업지시서 #09 - 엔딩 연출(페이드아웃 + "탈출에 성공했습니다")을 한 곳에 모음.
// 원래 Grandfather.cs 안에 있던 즉시 엔딩 로직을 그대로 옮긴 것 - 이제는 보스 격파 시점에서도 재사용.
public static class GameEndingTrigger
{
    public static void Trigger(MonoBehaviour host)
    {
        host.StartCoroutine(EndingSequence());
    }

    private static IEnumerator EndingSequence()
    {
        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeOut(Color.black, 1f);

        if (CenterMessageUI.Instance != null)
            CenterMessageUI.Instance.Show("탈출에 성공했습니다", 9999f);

        Debug.Log("게임 클리어! 탈출에 성공했습니다.");
    }
}
