using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 전투 밸런스 작업: 플레이어 체력이 30% 이하로 떨어지면 화면 가장자리가 붉게 맥동하는 긴급 경고 연출.
// 씬마다 손으로 배치할 필요 없이, 씬이 로드될 때마다 그 씬의 플레이어 Stat을 찾아서 자동으로 연결한다
// (플레이어가 없는 씬 - 타이틀/DialogScene 등 - 에서는 그냥 아무것도 안 함).
public static class LowHpVignetteLoader
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 타이틀/DialogScene처럼 "@Managers"가 아예 없는 씬에서 Managers.Game을 건드리면
        // 싱글턴이 초기화 전이라 NullReferenceException이 남 - 있는지부터 확인.
        if (GameObject.Find("@Managers") == null) return;

        GameObject player = Managers.Game.GetPlayer();
        if (player == null) return;

        Stat stat = player.GetComponent<Stat>();
        if (stat == null) return;

        LowHpVignette.Bind(stat);
    }
}

public class LowHpVignette : MonoBehaviour
{
    private const float THRESHOLD_RATIO = 0.3f;
    private const float PULSE_SPEED = 4f;

    private static LowHpVignette _instance;

    private Stat _stat;
    private Image _vignetteImage;

    public static void Bind(Stat stat)
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("LowHpVignette");
            Object.DontDestroyOnLoad(go);
            _instance = go.AddComponent<LowHpVignette>();
            _instance.BuildUI();
        }

        _instance._stat = stat;
    }

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("LowHpVignetteCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900; // 대사창/전투 UI보다는 아래, 배경보다는 위
        canvasGO.AddComponent<CanvasScaler>();

        GameObject imgGO = new GameObject("Vignette");
        imgGO.transform.SetParent(canvasGO.transform, false);
        _vignetteImage = imgGO.AddComponent<Image>();
        _vignetteImage.sprite = CreateVignetteSprite();
        _vignetteImage.color = new Color(1f, 0f, 0f, 0f);
        _vignetteImage.raycastTarget = false;
        var rt = _vignetteImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void Update()
    {
        if (_vignetteImage == null) return;

        if (_stat == null || _stat.MaxHp <= 0)
        {
            _vignetteImage.color = new Color(1f, 0f, 0f, 0f);
            return;
        }

        float ratio = (float)_stat.Hp / _stat.MaxHp;
        if (ratio > 0f && ratio <= THRESHOLD_RATIO)
        {
            // 체력이 낮을수록 더 진하게, 그 위에 맥동(pulse)을 얹어서 긴급함을 강조.
            float base_alpha = Mathf.Lerp(0.55f, 0.2f, ratio / THRESHOLD_RATIO);
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * PULSE_SPEED);
            _vignetteImage.color = new Color(1f, 0f, 0f, base_alpha * pulse);
        }
        else
        {
            _vignetteImage.color = new Color(1f, 0f, 0f, 0f);
        }
    }

    private static Sprite _vignetteSpriteCache;
    private static Sprite CreateVignetteSprite()
    {
        if (_vignetteSpriteCache != null) return _vignetteSpriteCache;

        const int size = 256;
        var tex = new Texture2D(size, size);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = center.magnitude;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                // 중심부는 완전 투명, 가장자리로 갈수록 불투명(부드럽게).
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((dist - 0.35f) / 0.65f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();

        _vignetteSpriteCache = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _vignetteSpriteCache;
    }
}
