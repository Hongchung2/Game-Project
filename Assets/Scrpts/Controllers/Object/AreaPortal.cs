using System.Collections;
using UnityEngine;

// 겨울방 내부 지역 이동용 포탈 (중앙 <-> 서쪽호수, 중앙 <-> 동쪽호수).
// 밟으면 지정된 위치로 검은 페이드와 함께 이동한다.
public class AreaPortal : MonoBehaviour
{
    public Vector2 destination;
    public float fadeDuration = 0.3f;

    private bool _isMoving = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isMoving) return;
        if (!other.CompareTag("Player")) return;

        StartCoroutine(MoveSequence(other.transform));
    }

    private IEnumerator MoveSequence(Transform player)
    {
        _isMoving = true;

        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeOut(Color.black, fadeDuration);

        player.position = destination;

        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeIn(fadeDuration);

        yield return new WaitForSeconds(0.3f); // 도착 지점에서 바로 재발동 방지
        _isMoving = false;
    }
}
