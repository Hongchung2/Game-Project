using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 명세서 7.1 채널1: 테두리 먹물 프레임. 정적 원화 4장(F0~F3) 알파 크로스페이드,
// 감정 단계 전이에 맞춰 전환된다. 아트가 아직 없어서 색만 다른 placeholder Sprite를
// 런타임 생성해서 씀 - frames 배열을 나중에 실제 아트로 교체 가능한 구조로 노출해둠.
public class BorderInkFrame : MonoBehaviour
{
    public static BorderInkFrame Instance { get; private set; }

    public float crossfadeDuration = 1.2f; // 명세서: 1~1.5초
    public Sprite[] frames = new Sprite[4]; // F0~F3, 비워두면 placeholder 자동 생성

    private Image[] _layers; // 크로스페이드용 2장을 번갈아 쓰지 않고, 프레임마다 개별 Image로 겹쳐 쌓음
    private int _currentFrameIndex = -1;

    public static BorderInkFrame CreateInstance()
    {
        if (Instance != null) return Instance;

        GameObject canvasGO = new GameObject("BorderInkFrameCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; // 대사 캔버스보다 더 위(테두리는 항상 보여야 함)
        canvasGO.AddComponent<CanvasScaler>();

        var frame = canvasGO.AddComponent<BorderInkFrame>();
        frame.BuildLayers(canvasGO.transform);
        Instance = frame;
        return frame;
    }

    private void BuildLayers(Transform parent)
    {
        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] == null) frames[i] = CreatePlaceholderFrameSprite(i);
        }

        _layers = new Image[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            GameObject go = new GameObject($"Frame_F{i}");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.sprite = frames[i];
            img.type = Image.Type.Sliced; // 9-slice - 화면 비율과 무관하게 테두리 두께 유지
            img.color = new Color(1f, 1f, 1f, i == 0 ? 1f : 0f); // F0(방심=백지)만 처음에 보임
            _layers[i] = img;
        }
        _currentFrameIndex = 0;
    }

    public void OnEnable()
    {
        EmotionCurveStateMachine.OnStageChanged += HandleStageChanged;
    }

    public void OnDisable()
    {
        EmotionCurveStateMachine.OnStageChanged -= HandleStageChanged;
    }

    private void HandleStageChanged(EmotionStage from, EmotionStage to)
    {
        int targetFrame = (int)to; // enum 순서가 F0~F3과 정확히 대응(Complacency=0...Chill=3)
        SetFrame(targetFrame);
    }

    // 재도전 시작 시 호출 - 명세서: "재도전 시작 프레임은 F1부터(F0 생략)".
    public void SetInitialFrameForAttempt(int attempt)
    {
        SetFrameImmediate(attempt <= 1 ? 0 : 1);
    }

    private void SetFrameImmediate(int frameIndex)
    {
        if (_layers == null) return;
        for (int i = 0; i < _layers.Length; i++)
        {
            Color c = _layers[i].color;
            c.a = i == frameIndex ? 1f : 0f;
            _layers[i].color = c;
        }
        _currentFrameIndex = frameIndex;
    }

    private void SetFrame(int frameIndex)
    {
        if (_layers == null || frameIndex == _currentFrameIndex || frameIndex < 0 || frameIndex >= _layers.Length) return;
        StartCoroutine(CrossfadeTo(frameIndex));
    }

    private IEnumerator CrossfadeTo(int frameIndex)
    {
        int fromIndex = _currentFrameIndex;
        _currentFrameIndex = frameIndex;

        float elapsed = 0f;
        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / crossfadeDuration);

            if (fromIndex >= 0) SetLayerAlpha(fromIndex, 1f - t);
            SetLayerAlpha(frameIndex, t);
            yield return null;
        }

        if (fromIndex >= 0) SetLayerAlpha(fromIndex, 0f);
        SetLayerAlpha(frameIndex, 1f);
    }

    private void SetLayerAlpha(int index, float alpha)
    {
        Color c = _layers[index].color;
        c.a = alpha;
        _layers[index].color = c;
    }

    // 아트 나오기 전까지 쓰는 최소 placeholder: 화면 테두리에 색 프레임(가운데는 투명).
    private static Sprite CreatePlaceholderFrameSprite(int frameIndex)
    {
        Color[] colors = { new Color(0, 0, 0, 0), new Color(0.2f, 0.2f, 0.2f), new Color(0.4f, 0.35f, 0.3f), new Color(0.05f, 0.05f, 0.05f) };
        Color borderColor = colors[Mathf.Clamp(frameIndex, 0, colors.Length - 1)];

        const int size = 256;
        int thickness = 8 + frameIndex * 16; // 프레임 번호가 커질수록 두꺼워지는 것으로 "채워짐" 표현

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = x < thickness || x >= size - thickness || y < thickness || y >= size - thickness;
                tex.SetPixel(x, y, isBorder ? borderColor : new Color(0, 0, 0, 0));
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / 2f, 0, SpriteMeshType.FullRect, new Vector4(thickness, thickness, thickness, thickness));
    }
}
