using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 화면 전체를 특정 색으로 덮었다(페이드아웃) 걷어내는(페이드인) 공용 연출 도구.
// 빈 오브젝트에 이 스크립트만 붙이면 캔버스와 이미지를 코드가 자동 생성한다.
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance;

    private Image _image;

    private void Awake()
    {
        // 이미 존재하면 중복 방지
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CreateOverlay();
    }

    // 화면 전체를 덮는 캔버스 + 이미지를 코드로 생성
    private void CreateOverlay()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // 다른 UI보다 항상 위에

        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        GameObject imgObj = new GameObject("FadeImage");
        imgObj.transform.SetParent(transform, false);

        _image = imgObj.AddComponent<Image>();
        _image.color = new Color(0f, 0f, 0f, 0f); // 투명하게 시작
        _image.raycastTarget = false;              // 클릭/터치 막지 않게

        // 화면 전체를 채우도록 앵커 설정
        RectTransform rt = _image.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // 지정한 색으로 서서히 덮기 (투명 → 불투명)
    public IEnumerator FadeOut(Color color, float duration)
    {
        Color from = new Color(color.r, color.g, color.b, 0f);
        Color to = new Color(color.r, color.g, color.b, 1f);
        yield return Fade(from, to, duration);
    }

    // 현재 덮인 화면을 서서히 걷어내기 (불투명 → 투명)
    public IEnumerator FadeIn(float duration)
    {
        Color c = _image.color;
        Color from = new Color(c.r, c.g, c.b, 1f);
        Color to = new Color(c.r, c.g, c.b, 0f);
        yield return Fade(from, to, duration);
    }

    private IEnumerator Fade(Color from, Color to, float duration)
    {
        float elapsed = 0f;
        _image.color = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _image.color = Color.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        _image.color = to;
    }
}
