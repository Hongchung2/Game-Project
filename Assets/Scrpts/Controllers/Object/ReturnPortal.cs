using System.Collections;
using UnityEngine;

// 몬스터방 복귀 포탈. 밟으면 검은 화면 페이드 후 미로 입구로 즉시 복귀.
public class ReturnPortal : MonoBehaviour
{
    public float fadeDuration = 0.5f;

    private bool _triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;

        _triggered = true;
        StartCoroutine(ReturnSequence(other.transform));
    }

    private IEnumerator ReturnSequence(Transform player)
    {
        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeOut(Color.black, fadeDuration);

        if (FallMazeManager.Instance != null && FallMazeManager.Instance.mazeEntrance != null)
            player.position = FallMazeManager.Instance.mazeEntrance.position;

        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeIn(fadeDuration);

        _triggered = false;
    }
}
