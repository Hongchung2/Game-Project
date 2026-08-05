#if UNITY_EDITOR
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

// M8a/b/c 완료기준 중 Play Mode 없이도 검증 가능한 부분(프레임 매핑, 좌표 변환, 알파 누적/상한,
// 저장/로드 왕복, 손상 파일 폴백, 실루엣 단계별 불투명도)과 M8b 성능 측정(스탬프 1회당 소요시간)을 확인.
// 실제 화면 크로스페이드/붓자국 시각 확인은 Play Mode 필요.
public static class M8SelfTest
{
    private static bool _anyFailure;

    [MenuItem("Tools/Boss/Self-Test M8 Arena")]
    public static void Run()
    {
        _anyFailure = false;

        Test_BorderInkFrame();
        Test_InkCanvasStampAndCap();
        Test_InkCanvasSaveLoadRoundTrip();
        Test_InkCanvasCorruptFileFallback();
        Test_SilhouetteOpacityByStage();
        Test_StampPerformance();

        if (_anyFailure) Debug.LogError("[M8SelfTest] 일부 테스트 실패");
        else Debug.Log("[M8SelfTest] 전부 통과");
    }

    private static void Check(string name, bool condition)
    {
        if (condition) Debug.Log($"[M8SelfTest] PASS: {name}");
        else { Debug.LogError($"[M8SelfTest] FAIL: {name}"); _anyFailure = true; }
    }

    // 완료기준 1: 감정 단계 전이 시 프레임 인덱스가 올바르게 대응되는지 (F0~F3 = enum 순서)
    private static void Test_BorderInkFrame()
    {
        GameObject temp = new GameObject("M8SelfTest_Border");
        try
        {
            var frame = temp.AddComponent<BorderInkFrame>();
            InvokePrivate(frame, "BuildLayers", temp.transform);

            frame.SetInitialFrameForAttempt(1);
            Check("attempt1은 F0(방심)부터 시작", GetLayerAlpha(frame, 0) == 1f);

            frame.SetInitialFrameForAttempt(2);
            Check("attempt2+는 F1(위화감)부터 시작(F0 생략)", GetLayerAlpha(frame, 1) == 1f);
        }
        finally
        {
            Object.DestroyImmediate(temp);
        }
    }

    private static float GetLayerAlpha(BorderInkFrame frame, int index)
    {
        var field = typeof(BorderInkFrame).GetField("_layers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var layers = field.GetValue(frame) as UnityEngine.UI.Image[];
        return layers[index].color.a;
    }

    // 완료기준 2: 이동 시 붓 궤적이 남고, 같은 자리 반복 시 진해지되 최대 농도 상한을 넘지 않는지
    private static void Test_InkCanvasStampAndCap()
    {
        GameObject go = new GameObject("M8SelfTest_InkCanvas_Stamp");
        try
        {
            var canvas = go.AddComponent<InkCanvas>();
            InvokePrivate(canvas, "Initialize");

            Vector3 pos = new Vector3(15f, 15f, 0f);
            InvokePrivate(canvas, "StampAt", pos);
            float alphaAfterOne = ReadPixelAlphaAt(canvas, pos);
            Check("한 번 찍으면 알파가 0보다 커짐", alphaAfterOne > 0f);

            for (int i = 0; i < 50; i++) InvokePrivate(canvas, "StampAt", pos);
            float alphaAfterMany = ReadPixelAlphaAt(canvas, pos);
            Check("여러 번 찍으면 더 진해짐", alphaAfterMany > alphaAfterOne);
            Check("최대 농도 상한(0.85)을 넘지 않음", alphaAfterMany <= 0.85f + 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    // 완료기준 3: PNG 저장 후 새 인스턴스로 로드하면 궤적이 복원되는지
    private static void Test_InkCanvasSaveLoadRoundTrip()
    {
        int originalSlot = BossDataPersistence.CurrentSlot;
        BossDataPersistence.CurrentSlot = 9001; // 테스트 전용 슬롯(실제 진행 데이터와 충돌 방지)

        GameObject go1 = new GameObject("M8SelfTest_InkCanvas_Save");
        try
        {
            var canvas1 = go1.AddComponent<InkCanvas>();
            InvokePrivate(canvas1, "Initialize");

            Vector3 pos = new Vector3(10f, 20f, 0f);
            for (int i = 0; i < 20; i++) InvokePrivate(canvas1, "StampAt", pos);
            float savedAlpha = ReadPixelAlphaAt(canvas1, pos);

            canvas1.SaveToDisk();

            Object.DestroyImmediate(go1);

            GameObject go2 = new GameObject("M8SelfTest_InkCanvas_Load");
            var canvas2 = go2.AddComponent<InkCanvas>();
            InvokePrivate(canvas2, "Initialize");
            float loadedAlpha = ReadPixelAlphaAt(canvas2, pos);

            Check($"저장 후 새 인스턴스에 궤적 복원됨(저장:{savedAlpha:F3}, 로드:{loadedAlpha:F3})",
                Mathf.Abs(savedAlpha - loadedAlpha) < 0.05f && loadedAlpha > 0f);

            Object.DestroyImmediate(go2);
        }
        finally
        {
            BossDataPersistence.CurrentSlot = originalSlot;
        }
    }

    // 파일 손상 시 백지로 폴백하는지
    private static void Test_InkCanvasCorruptFileFallback()
    {
        int originalSlot = BossDataPersistence.CurrentSlot;
        BossDataPersistence.CurrentSlot = 9002;

        string path = System.IO.Path.Combine(Application.persistentDataPath, "BossAI", "boss_canvas_recent_slot9002.png");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        System.IO.File.WriteAllText(path, "이것은 유효한 PNG가 아님");

        GameObject go = new GameObject("M8SelfTest_InkCanvas_Corrupt");
        try
        {
            bool threw = false;
            InkCanvas canvas = null;
            try
            {
                canvas = go.AddComponent<InkCanvas>();
                InvokePrivate(canvas, "Initialize");
            }
            catch (System.Exception e)
            {
                threw = true;
                Debug.LogWarning($"[M8SelfTest] 손상 파일 테스트 중 예외: {e}");
            }

            Check("손상된 PNG여도 예외 없이 초기화됨(백지 폴백)", !threw && canvas != null);
        }
        finally
        {
            Object.DestroyImmediate(go);
            BossDataPersistence.CurrentSlot = originalSlot;
            try { System.IO.File.Delete(path); } catch { }
        }
    }

    // 완료기준 5: 감정 단계별 실루엣 불투명도가 명세서(30/45/55%, 방심 0%)와 일치하는지
    private static void Test_SilhouetteOpacityByStage()
    {
        var method = typeof(ChannelingSilhouetteOverlay).GetMethod("OpacityForStage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        float complacency = (float)method.Invoke(null, new object[] { EmotionStage.Complacency });
        float unease = (float)method.Invoke(null, new object[] { EmotionStage.Unease });
        float awareness = (float)method.Invoke(null, new object[] { EmotionStage.Awareness });
        float chill = (float)method.Invoke(null, new object[] { EmotionStage.Chill });

        Check("Complacency는 0%(미표시)", complacency == 0f);
        Check("Unease는 30%", Mathf.Approximately(unease, 0.3f));
        Check("Awareness는 45%", Mathf.Approximately(awareness, 0.45f));
        Check("Chill은 55%", Mathf.Approximately(chill, 0.55f));
    }

    // M8b 완료기준 4: 스탬프 1회당 소요시간 실측(성능 확인 필수 요구사항에 대한 근사 지표).
    private static void Test_StampPerformance()
    {
        GameObject go = new GameObject("M8SelfTest_InkCanvas_Perf");
        try
        {
            var canvas = go.AddComponent<InkCanvas>();
            InvokePrivate(canvas, "Initialize");

            const int iterations = 100;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                Vector3 pos = new Vector3(5f + i * 0.05f, 5f, 0f);
                InvokePrivate(canvas, "StampAt", pos);
            }
            sw.Stop();

            double msPerStamp = sw.Elapsed.TotalMilliseconds / iterations;
            Debug.Log($"[M8SelfTest] 성능 실측: 스탬프 {iterations}회 총 {sw.Elapsed.TotalMilliseconds:F1}ms, 1회당 평균 {msPerStamp:F3}ms " +
                      "(0.15초 간격으로 찍히므로 1회당 150ms 예산 대비 여유 확인용 - 실제 프레임드랍 여부는 Play Mode 확인 필요)");
            Check("스탬프 1회가 0.15초 예산보다 충분히 빠름(<10ms)", msPerStamp < 10.0);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    private static void InvokePrivate(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(target, args);
    }

    private static float ReadPixelAlphaAt(InkCanvas canvas, Vector3 worldPos)
    {
        var worldToPixel = typeof(InkCanvas).GetMethod("WorldToPixel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var pixel = (Vector2Int)worldToPixel.Invoke(canvas, new object[] { worldPos });

        var field = typeof(InkCanvas).GetField("_recentPixels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var pixels = field.GetValue(canvas) as Color[];
        return pixels[pixel.y * InkCanvas.CANVAS_SIZE + pixel.x].a;
    }
}
#endif
