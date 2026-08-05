#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

// 명세서 M9: 적응형 시스템 재현 테스트용 디버그 도구. 릴리즈 빌드에서는 이 파일 전체가
// 컴파일에서 빠진다(#if UNITY_EDITOR || DEVELOPMENT_BUILD). F12로 패널 토글.
// 별도 UI 프리팹 없이 OnGUI(IMGUI)로 구현 - 개발/QA 전용이라 비주얼 품질보다 즉시 사용 가능함이 우선.
public class BossDevTools : MonoBehaviour
{
    private bool _visible;
    private Vector2 _scroll;
    private string _attemptInput = "1";
    private string _diedByInput = "묵운산군";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (FindAnyObjectByType<BossDevTools>() != null) return;
        var go = new GameObject("BossDevTools");
        go.AddComponent<BossDevTools>();
        DontDestroyOnLoad(go);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12)) _visible = !_visible;
    }

    private void OnGUI()
    {
        if (!_visible) return;

        GUILayout.BeginArea(new Rect(10, 10, 420, Screen.height - 20), GUI.skin.box);
        _scroll = GUILayout.BeginScrollView(_scroll);

        GUILayout.Label("보스 디버그 도구 (F12로 닫기)", GUI.skin.box);

        DrawProfileSection();
        DrawDecisionSection();
        DrawEmotionSection();
        DrawPhaseSection();
        DrawAttemptSection();
        DrawDeathHistorySection();
        DrawCanvasSection();
        DrawLlmSection();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private static MukunSangunController FindBoss() => FindAnyObjectByType<MukunSangunController>();

    // ===== PlayerProfile 강제 주입 (프리셋) =====
    private void DrawProfileSection()
    {
        GUILayout.Label("-- PlayerProfile 프리셋 강제 주입 --");
        if (GUILayout.Button("반응느림 + 원거리 + 좌편향"))
        {
            PlayerProfileAggregator.ImportRawState(BuildPreset(reactionHigh: false, meleeHeavy: false, leftBias: true));
            RegenerateBossDecision();
        }
        if (GUILayout.Button("반응빠름 + 근접 + 편향없음"))
        {
            PlayerProfileAggregator.ImportRawState(BuildPreset(reactionHigh: true, meleeHeavy: true, leftBias: false));
            RegenerateBossDecision();
        }
    }

    private static PlayerProfileRawState BuildPreset(bool reactionHigh, bool meleeHeavy, bool leftBias)
    {
        var state = new PlayerProfileRawState();

        // 스테이지1(index 0)만 채움 - 프리셋 목적상 스테이지2는 굳이 안 채워도 됨.
        int hitsPerBucket = reactionHigh ? 2 : 8;
        int dodgesPerBucket = reactionHigh ? 8 : 2;
        for (int bucket = 0; bucket < 3; bucket++)
        {
            state.reactionHits[bucket] = hitsPerBucket;
            state.reactionDodges[bucket] = dodgesPerBucket;
        }

        state.rangeMeleeCount[0] = meleeHeavy ? 16 : 2;
        state.rangeTotal[0] = 20;

        state.dodgeBiasLeft[0] = leftBias ? 16 : 10;
        state.dodgeBiasRight[0] = leftBias ? 4 : 10;

        state.recoveryEntriesByStage[0] = 30;
        state.punishCountByStage[0] = 6;

        state.scarecrowHits = 1;
        state.scarecrowDodges = 1;
        state.byeorugeHits = 1;
        state.byeorugeDodges = 1;

        return state;
    }

    private static void RegenerateBossDecision()
    {
        var boss = FindBoss();
        if (boss == null)
        {
            Debug.Log("[BossDevTools] 씬에 보스가 없어서 프로필만 갱신됨(다음 진입 시 반영)");
            return;
        }

        var profile = PlayerProfileAggregator.GetCurrentProfile();
        var decision = BossDecisionFallback.Generate(profile, DeathHistoryTracker.Current);
        boss.DebugInjectDecision(decision);
        Debug.Log("[BossDevTools] 프로필 갱신 + 현재 보스에 즉시 재적용 완료");
    }

    // ===== BossDecision 강제 주입 =====
    private void DrawDecisionSection()
    {
        GUILayout.Label("-- BossDecision 강제 주입 (LLM 스킵) --");
        if (GUILayout.Button("mark_weight=산0.7/구름0.3, habitual_position 주입"))
        {
            InjectDecision(mountain: 0.7f, cloud: 0.3f, mountainTarget: "habitual_position", punch: 0.5f, slam: 0.5f, recoveryExposed: true);
        }
        if (GUILayout.Button("mark_weight=산0.3/구름0.7, current_position 주입"))
        {
            InjectDecision(mountain: 0.3f, cloud: 0.7f, mountainTarget: "current_position", punch: 0.5f, slam: 0.5f, recoveryExposed: true);
        }
        if (GUILayout.Button("phase2=돌주먹0.8/양손0.2, 후딜짧게 주입"))
        {
            InjectDecision(mountain: 0.5f, cloud: 0.5f, mountainTarget: "current_position", punch: 0.8f, slam: 0.2f, recoveryExposed: false);
        }
    }

    private static void InjectDecision(float mountain, float cloud, string mountainTarget, float punch, float slam, bool recoveryExposed)
    {
        var boss = FindBoss();
        if (boss == null) { Debug.LogWarning("[BossDevTools] 씬에 보스가 없음"); return; }

        var decision = new BossDecision
        {
            mark_weight = new MarkWeight { mountain = mountain, cloud = cloud },
            mountain_target = mountainTarget,
            phase2_pattern_weight = new Phase2PatternWeight { punch = punch, slam = slam },
            phase2_recovery_exposed = recoveryExposed,
            boss_line_p1 = "…시작하지.",
            boss_line_transition = "이제 알겠지.",
            confidence = "low",
        };
        boss.DebugInjectDecision(decision);
        Debug.Log("[BossDevTools] BossDecision 강제 주입 완료");
    }

    // ===== 감정 단계 강제 전환 =====
    private void DrawEmotionSection()
    {
        GUILayout.Label($"-- 감정 단계 강제 전환 (현재: {EmotionCurveStateMachine.CurrentStage}) --");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("방심")) EmotionCurveStateMachine.DebugForceStage(EmotionStage.Complacency);
        if (GUILayout.Button("위화감")) EmotionCurveStateMachine.DebugForceStage(EmotionStage.Unease);
        if (GUILayout.Button("자각")) EmotionCurveStateMachine.DebugForceStage(EmotionStage.Awareness);
        if (GUILayout.Button("소름")) EmotionCurveStateMachine.DebugForceStage(EmotionStage.Chill);
        GUILayout.EndHorizontal();
    }

    // ===== 페이즈 강제 전환 =====
    private void DrawPhaseSection()
    {
        GUILayout.Label("-- 페이즈 강제 전환 (1↔2) --");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("2페이즈로 (운무강림 즉시 발동)"))
        {
            var boss = FindBoss();
            if (boss != null) boss.DebugForcePhase2();
            else Debug.LogWarning("[BossDevTools] 씬에 보스가 없음");
        }
        if (GUILayout.Button("1페이즈로 복귀(테스트용)"))
        {
            var boss = FindBoss();
            if (boss != null) boss.DebugRevertToPhase1();
            else Debug.LogWarning("[BossDevTools] 씬에 보스가 없음");
        }
        GUILayout.EndHorizontal();
    }

    // ===== 시도 횟수(attempt) 강제 설정 =====
    private void DrawAttemptSection()
    {
        GUILayout.Label("-- 시도 횟수(attempt) 강제 설정 --");
        GUILayout.BeginHorizontal();
        _attemptInput = GUILayout.TextField(_attemptInput, GUILayout.Width(60));
        if (GUILayout.Button("적용"))
        {
            if (int.TryParse(_attemptInput, out int attempt) && attempt >= 1)
            {
                var boss = FindBoss();
                if (boss != null) boss.DebugSetAttempt(attempt);
                else EmotionCurveStateMachine.Begin(attempt);
                Debug.Log($"[BossDevTools] attempt={attempt}로 강제 설정, 문턱={EmotionCurveStateMachine.GetThresholds(attempt)}");
            }
        }
        GUILayout.EndHorizontal();
    }

    // ===== death_history 강제 주입 =====
    private void DrawDeathHistorySection()
    {
        GUILayout.Label("-- death_history 강제 주입 --");
        GUILayout.BeginHorizontal();
        GUILayout.Label("died_by:", GUILayout.Width(60));
        _diedByInput = GUILayout.TextField(_diedByInput);
        GUILayout.EndHorizontal();
        if (GUILayout.Button("가짜 사망 기록 1건 추가"))
        {
            Vector2 pos = Vector2.zero;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) pos = player.transform.position;
            DeathHistoryTracker.RecordDeath(_diedByInput, pos, "left");
            Debug.Log("[BossDevTools] death_history에 가짜 사망 기록 추가됨");
        }
        if (GUILayout.Button("death_history 초기화"))
        {
            DeathHistoryTracker.ResetAll();
            Debug.Log("[BossDevTools] death_history 초기화됨");
        }
    }

    // ===== 화선지 초기화 =====
    private void DrawCanvasSection()
    {
        GUILayout.Label("-- 화선지(채널2) --");
        if (GUILayout.Button("화선지 초기화"))
        {
            if (InkCanvas.Instance != null)
            {
                InkCanvas.Instance.ResetCanvas();
                Debug.Log("[BossDevTools] 화선지 초기화됨");
            }
            else
            {
                Debug.LogWarning("[BossDevTools] InkCanvas 인스턴스가 아직 없음(보스방 진입 후 사용)");
            }
        }
    }

    // ===== LLM 호출 ON/OFF =====
    private void DrawLlmSection()
    {
        GUILayout.Label("-- LLM 호출 --");
        bool forced = BossDecisionClient.ForceFallbackForDebug;
        bool newForced = GUILayout.Toggle(forced, "LLM 호출 강제 OFF(항상 폴백 규칙 사용)");
        if (newForced != forced) BossDecisionClient.ForceFallbackForDebug = newForced;

        GUILayout.Space(4);
        GUILayout.Label("프록시 URL:");
        BossDecisionClient.ProxyBaseUrl = GUILayout.TextField(BossDecisionClient.ProxyBaseUrl);
        GUILayout.Label("공유 시크릿(로컬 dev용, 커밋 금지):");
        BossDecisionClient.SharedSecret = GUILayout.TextField(BossDecisionClient.SharedSecret);

        if (GUILayout.Button("로컬 wrangler dev로 채우기 (http://127.0.0.1:8787)"))
        {
            BossDecisionClient.ProxyBaseUrl = "http://127.0.0.1:8787";
#if UNITY_EDITOR
            BossDecisionClient.SharedSecret = ReadLocalDevSharedSecret();
#endif
            Debug.Log("[BossDevTools] 로컬 프록시 설정 적용됨(wrangler dev가 떠 있어야 실제 호출됨)");
        }
    }

#if UNITY_EDITOR
    // boss-proxy/.dev.vars에서 로컬 전용 시크릿을 읽어옴(소스에 하드코딩 안 함, 배포용 아님).
    private static string ReadLocalDevSharedSecret()
    {
        try
        {
            string path = System.IO.Path.Combine(Application.dataPath, "..", "boss-proxy", ".dev.vars");
            foreach (var line in System.IO.File.ReadAllLines(path))
            {
                if (line.StartsWith("SHARED_SECRET="))
                    return line.Substring("SHARED_SECRET=".Length);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[BossDevTools] .dev.vars 읽기 실패: {e.Message}");
        }
        return "";
    }
#endif
}
#endif
