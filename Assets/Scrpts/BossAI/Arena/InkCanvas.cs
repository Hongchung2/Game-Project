using System;
using System.IO;
using UnityEngine;

// 명세서 7.2 채널2: "바닥 자체가 화선지" - 플레이어 실측 좌표를 따라 붓터치가 누적된다.
// 셰이더/RenderTexture 대신 CPU 측 Texture2D에 직접 SetPixels로 블렌딩한다
// (셰이더 없이 구현 가능하고, 붓 반경이 작아서(반지름 14px) 0.15초 간격 정도면 부담 적음 -
// 실측은 InkCanvasPerfSelfTest 참고, 실제 프레임드랍 여부는 플레이테스트 필요).
public class InkCanvas : MonoBehaviour
{
    public static InkCanvas Instance { get; private set; }

    public const int CANVAS_SIZE = 1024; // 명세서 9.1-2 권장 범위(960~1920) 내에서 성능/용량 절충
    private const float SAMPLE_INTERVAL = 0.15f;
    private const float MIN_MOVE_DISTANCE = 0.3f; // 제자리에 서 있으면 안 찍히게
    private const float MAX_ALPHA = 0.85f; // 완전 검정으로 뭉개지지 않게 하는 상한
    private const float STROKE_ALPHA = 0.15f;
    private const int BRUSH_RADIUS_PX = 14;
    private const float FADE_HOURS = 48f;
    private const float FADED_ALPHA_MULTIPLIER = 0.4f; // 명세서: 30~50% 하향, 중간값
    private const float AUTOSAVE_INTERVAL = 45f; // 명세서: 30~60초 권장, 중간값

    // 보스방(M3) 월드 좌표 범위 그대로.
    private static readonly Vector2 RoomWorldMin = Vector2.zero;
    private static readonly Vector2 RoomWorldMax = new Vector2(30f, 30f);

    private Texture2D _recentLayer;
    private Texture2D _fadedLayer;
    private Color[] _recentPixels;
    private Color[] _fadedPixels;

    private Transform _player;
    private Vector3 _lastStampWorldPos;
    private bool _hasStamped;
    private float _sampleTimer;
    private float _autosaveTimer;

    public static InkCanvas CreateInstance()
    {
        if (Instance != null) return Instance;

        GameObject go = new GameObject("InkCanvas");
        var canvas = go.AddComponent<InkCanvas>();
        Instance = canvas;
        canvas.Initialize();
        return canvas;
    }

    private void Initialize()
    {
        _recentLayer = new Texture2D(CANVAS_SIZE, CANVAS_SIZE, TextureFormat.RGBA32, false);
        _fadedLayer = new Texture2D(CANVAS_SIZE, CANVAS_SIZE, TextureFormat.RGBA32, false);

        LoadFromDiskOrBlank();
        ApplyFadeTransferOnLoad();
        DisplayOnFloor();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
            _lastStampWorldPos = _player.position;
        }
    }

    private void Update()
    {
        if (_player == null) return;

        _sampleTimer += Time.deltaTime;
        _autosaveTimer += Time.deltaTime;

        if (_sampleTimer >= SAMPLE_INTERVAL)
        {
            _sampleTimer = 0f;
            TryStamp();
        }

        if (_autosaveTimer >= AUTOSAVE_INTERVAL)
        {
            _autosaveTimer = 0f;
            SaveToDisk();
        }
    }

    private void TryStamp()
    {
        Vector3 pos = _player.position;
        if (_hasStamped && Vector3.Distance(pos, _lastStampWorldPos) < MIN_MOVE_DISTANCE) return;

        _hasStamped = true;
        _lastStampWorldPos = pos;
        StampAt(pos);
    }

    private void StampAt(Vector3 worldPos)
    {
        Vector2Int center = WorldToPixel(worldPos);
        int r = BRUSH_RADIUS_PX;

        for (int dy = -r; dy <= r; dy++)
        {
            int py = center.y + dy;
            if (py < 0 || py >= CANVAS_SIZE) continue;

            for (int dx = -r; dx <= r; dx++)
            {
                int px = center.x + dx;
                if (px < 0 || px >= CANVAS_SIZE) continue;

                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > r) continue;

                float falloff = 1f - (dist / r); // 중심일수록 진하게
                float addAlpha = STROKE_ALPHA * falloff;

                int idx = py * CANVAS_SIZE + px;
                Color c = _recentPixels[idx];
                float newAlpha = Mathf.Min(MAX_ALPHA, c.a + addAlpha * (1f - c.a));
                _recentPixels[idx] = new Color(0.05f, 0.05f, 0.05f, newAlpha); // 먹색 단색 누적
            }
        }

        _recentLayer.SetPixels(_recentPixels);
        _recentLayer.Apply();
    }

    private Vector2Int WorldToPixel(Vector3 worldPos)
    {
        float u = Mathf.InverseLerp(RoomWorldMin.x, RoomWorldMax.x, worldPos.x);
        float v = Mathf.InverseLerp(RoomWorldMin.y, RoomWorldMax.y, worldPos.y);
        int x = Mathf.Clamp(Mathf.RoundToInt(u * (CANVAS_SIZE - 1)), 0, CANVAS_SIZE - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(v * (CANVAS_SIZE - 1)), 0, CANVAS_SIZE - 1);
        return new Vector2Int(x, y);
    }

    // 화면에 실제로 보이도록 방 크기만큼의 스프라이트 두 장(faded 아래, recent 위)을 바닥 위에 배치.
    private void DisplayOnFloor()
    {
        CreateLayerSprite("InkLayer_Faded", _fadedLayer, sortingOrder: 0);
        CreateLayerSprite("InkLayer_Recent", _recentLayer, sortingOrder: 0);
    }

    private void CreateLayerSprite(string name, Texture2D tex, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.position = new Vector3((RoomWorldMin.x + RoomWorldMax.x) / 2f, (RoomWorldMin.y + RoomWorldMax.y) / 2f, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        float sizeUnits = RoomWorldMax.x - RoomWorldMin.x;
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, CANVAS_SIZE, CANVAS_SIZE), new Vector2(0.5f, 0.5f), CANVAS_SIZE / sizeUnits);
        sr.sortingOrder = sortingOrder;
    }

    private void OnApplicationQuit()
    {
        SaveToDisk();
    }

    // ===== 저장/로드 (PNG, 세이브 슬롯당 1쌍) =====

    private static string LayerPath(string layerName) =>
        Path.Combine(Application.persistentDataPath, "BossAI", $"boss_canvas_{layerName}_slot{BossDataPersistence.CurrentSlot}.png");

    private static string MetaPath() =>
        Path.Combine(Application.persistentDataPath, "BossAI", $"boss_canvas_meta_slot{BossDataPersistence.CurrentSlot}.txt");

    public void SaveToDisk()
    {
        try
        {
            string dir = Path.Combine(Application.persistentDataPath, "BossAI");
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(LayerPath("recent"), _recentLayer.EncodeToPNG());
            File.WriteAllBytes(LayerPath("faded"), _fadedLayer.EncodeToPNG());
            File.WriteAllText(MetaPath(), DateTime.UtcNow.Ticks.ToString());
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[InkCanvas] 저장 실패(게임 진행엔 영향 없음): {e.Message}");
        }
    }

    private void LoadFromDiskOrBlank()
    {
        _recentPixels = LoadLayerOrBlank(LayerPath("recent"), out _recentLayer);
        _fadedPixels = LoadLayerOrBlank(LayerPath("faded"), out _fadedLayer);
    }

    private Color[] LoadLayerOrBlank(string path, out Texture2D tex)
    {
        tex = new Texture2D(CANVAS_SIZE, CANVAS_SIZE, TextureFormat.RGBA32, false);

        try
        {
            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                if (tex.LoadImage(bytes) && tex.width == CANVAS_SIZE && tex.height == CANVAS_SIZE)
                {
                    return tex.GetPixels();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[InkCanvas] 파일 손상, 백지로 폴백: {e.Message}");
        }

        // 백지 폴백. LoadImage가 실패하면(손상 파일) 텍스처 크기 자체가 내부적으로
        // 작은 크기로 바뀌어버리므로, SetPixels 전에 반드시 원래 크기로 되돌려야 함
        // (안 그러면 이후 Sprite.Create에서 크기 불일치 예외 발생 - 자체 테스트로 발견).
        if (tex.width != CANVAS_SIZE || tex.height != CANVAS_SIZE)
            tex.Reinitialize(CANVAS_SIZE, CANVAS_SIZE, TextureFormat.RGBA32, false);

        var blank = new Color[CANVAS_SIZE * CANVAS_SIZE];
        for (int i = 0; i < blank.Length; i++) blank[i] = new Color(0f, 0f, 0f, 0f);
        tex.SetPixels(blank);
        tex.Apply();
        return blank;
    }

    // 게임 켤 때마다 recent -> faded 이관 처리(48시간 경과분). 레이어 전체를 통째로 옮기는 방식
    // (개별 붓자국 타임스탬프를 저장하지 않으므로, "마지막 저장 시각"이 48시간 넘었으면 그 시점의
    // recent 전체를 faded로 흡수 - 판단 필요 항목, 개별 스트로크 단위 이관은 하지 않음).
    private void ApplyFadeTransferOnLoad()
    {
        DateTime lastSave;
        try
        {
            string metaPath = MetaPath();
            if (!File.Exists(metaPath)) return;
            lastSave = new DateTime(long.Parse(File.ReadAllText(metaPath)), DateTimeKind.Utc);
        }
        catch
        {
            return;
        }

        double hoursSince = (DateTime.UtcNow - lastSave).TotalHours;
        if (hoursSince < FADE_HOURS) return;

        for (int i = 0; i < _recentPixels.Length; i++)
        {
            Color recent = _recentPixels[i];
            if (recent.a <= 0f) continue;

            Color faded = _fadedPixels[i];
            float mergedAlpha = Mathf.Min(MAX_ALPHA, faded.a + recent.a * FADED_ALPHA_MULTIPLIER);
            _fadedPixels[i] = new Color(0.05f, 0.05f, 0.05f, mergedAlpha);
            _recentPixels[i] = new Color(0f, 0f, 0f, 0f);
        }

        _fadedLayer.SetPixels(_fadedPixels);
        _fadedLayer.Apply();
        _recentLayer.SetPixels(_recentPixels);
        _recentLayer.Apply();
    }

    // M9 디버그 도구용 - 화선지 초기화.
    public void ResetCanvas()
    {
        var blank = new Color[CANVAS_SIZE * CANVAS_SIZE];
        for (int i = 0; i < blank.Length; i++) blank[i] = new Color(0f, 0f, 0f, 0f);

        _recentPixels = (Color[])blank.Clone();
        _fadedPixels = (Color[])blank.Clone();
        _recentLayer.SetPixels(_recentPixels);
        _recentLayer.Apply();
        _fadedLayer.SetPixels(_fadedPixels);
        _fadedLayer.Apply();
    }
}
