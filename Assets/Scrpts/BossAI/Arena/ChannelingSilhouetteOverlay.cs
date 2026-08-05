using UnityEngine;

// 명세서 8장: 공격 예고 시작과 동일 시점에 채널링 원본 몬스터의 실루엣이 보스 몸에 겹쳐 보임.
// 판정(회피 기준)과는 완전히 별개 레이어의 순수 장식 - 꺼져 있어도 전투에 영향 없음.
public static class ChannelingSilhouetteOverlay
{
    private const float BASE_DURATION = 0.3f;
    private static bool _subscribed;
    private static Sprite _placeholderSprite;

    public static void Initialize()
    {
        if (_subscribed) return;
        _subscribed = true;
        ChannelingSignal.OnChannelingStart += HandleChannelingStart;
    }

    private static void HandleChannelingStart(ChanneledMonster monster, float telegraphDuration)
    {
        float opacity = OpacityForStage(EmotionCurveStateMachine.CurrentStage);
        if (opacity <= 0f) return; // 명세서: 방심 단계엔 표시 안 함

        // 예고시간 0.3초 미만이면 실루엣 지속시간을 예고시간의 80%로 축소.
        float duration = telegraphDuration > 0f && telegraphDuration < BASE_DURATION
            ? telegraphDuration * 0.8f
            : BASE_DURATION;

        GameObject bossGO = GameObject.Find("MukunSangun");
        if (bossGO == null) return;

        GameObject overlay = new GameObject($"Silhouette_{monster}");
        overlay.transform.SetParent(bossGO.transform);
        overlay.transform.localPosition = Vector3.zero;
        overlay.transform.localScale = Vector3.one * 1.2f;

        var sr = overlay.AddComponent<SpriteRenderer>();
        sr.sprite = GetPlaceholderSprite();
        // 명세서는 곱하기(multiply) 블렌드를 요구하지만, 이 프로젝트에 멀티플라이 셰이더가
        // 준비돼있는지 확인이 안 돼서 지금은 표준 알파 블렌드로 근사(판단/단순화, 보고 대상).
        // 실제 아트+셰이더가 붙을 때 교체하면 됨 - 판정과 무관한 순수 장식이라 기능엔 영향 없음.
        sr.color = new Color(0.05f, 0.05f, 0.05f, opacity);
        sr.sortingOrder = 4; // 보스 본체(3)보다 위

        var autoDestroy = overlay.AddComponent<SilhouetteAutoDestroy>();
        autoDestroy.lifeTime = duration;
    }

    private static float OpacityForStage(EmotionStage stage)
    {
        switch (stage)
        {
            case EmotionStage.Unease: return 0.3f;
            case EmotionStage.Awareness: return 0.45f;
            case EmotionStage.Chill: return 0.55f;
            default: return 0f; // Complacency
        }
    }

    private static Sprite GetPlaceholderSprite()
    {
        if (_placeholderSprite != null) return _placeholderSprite;

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                tex.SetPixel(x, y, dist <= size / 2f ? Color.white : new Color(0f, 0f, 0f, 0f));
            }
        }
        tex.Apply();

        _placeholderSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return _placeholderSprite;
    }
}

public class SilhouetteAutoDestroy : MonoBehaviour
{
    public float lifeTime;
    private void Start() => Destroy(gameObject, lifeTime);
}
