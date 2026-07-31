using System.Collections;
using UnityEngine;

// 화면 임팩트용 카메라 줌인-줌아웃 연출. ScreenFader와 같은 패턴(빈 오브젝트에 컴포넌트만 붙이면 작동).
public class CameraZoomPulse : MonoBehaviour
{
    public static CameraZoomPulse Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void Pulse(float zoomAmount = 1.5f, float zoomInDuration = 0.3f, float holdDuration = 0.4f, float zoomOutDuration = 0.4f)
    {
        StartCoroutine(PulseRoutine(zoomAmount, zoomInDuration, holdDuration, zoomOutDuration));
    }

    private IEnumerator PulseRoutine(float zoomAmount, float zoomInDuration, float holdDuration, float zoomOutDuration)
    {
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) yield break;

        float originalSize = cam.orthographicSize;
        float targetSize = Mathf.Max(0.5f, originalSize - zoomAmount);

        yield return LerpSize(cam, originalSize, targetSize, zoomInDuration);
        yield return new WaitForSeconds(holdDuration);
        yield return LerpSize(cam, targetSize, originalSize, zoomOutDuration);
    }

    private IEnumerator LerpSize(Camera cam, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cam.orthographicSize = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        cam.orthographicSize = to;
    }
}
