using UnityEngine;
using UnityEngine.SceneManagement;

// 묵운산군(최종 보스) 학습 시스템의 "각성" 관리자.
//
// 원래 구조상 PlayerProfileAggregator는 첫 씬 로드 전에 Telemetry를 구독하도록 되어 있었지만,
// 정작 BossDataPersistence.Load()/Save()를 호출하는 곳이 게임 어디에도 없었다. 그래서
//   - 이전 세션에서 쌓인 플레이어 프로필/사망 이력이 전혀 복원되지 않았고,
//   - 게임 종료(Application.quitting) 때만 저장돼서 크래시 한 번이면 전부 날아갔다.
//
// 이 클래스가 그 구멍을 메운다:
//   1) 게임을 켜면(어느 스테이지에서 시작하든) 곧바로 깨어나 이전 데이터를 복원한다.
//   2) 씬(스테이지)이 바뀔 때마다 아직 안 깨어났으면 계속 깨우기를 재시도한다.
//   3) 깨어 있으면 그때까지 쌓인 데이터를 저장해서 크래시로 잃지 않게 한다.
//
// 데이터 수집 자체는 스테이지1·2 몬스터들이 Telemetry로 발행 → PlayerProfileAggregator가
// 구독하는 기존 경로를 그대로 쓴다(수집 로직은 건드리지 않음).
public static class MukunSangunWakeUp
{
    private static bool _awake;
    private static bool _hooked;

    public static bool IsAwake => _awake;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (!_hooked)
        {
            _hooked = true;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        TryWake("게임 시작");
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_awake)
        {
            // 아직 안 깨어났으면 스테이지가 넘어갈 때마다 계속 두드린다.
            TryWake($"씬 전환 - {scene.name}");
            return;
        }

        // 이미 깨어 있으면 지금까지 모은 걸 디스크에 남긴다(종료 훅만 믿으면 크래시 때 전부 손실).
        Persist($"{scene.name} 진입");
    }

    // 실패해도 예외를 밖으로 내보내지 않는다 - 학습 시스템 문제로 게임이 멈추면 안 됨.
    // 실패 시 _awake를 false로 남겨서 다음 씬 전환 때 자동으로 재시도된다.
    private static void TryWake(string reason)
    {
        if (_awake) return;

        try
        {
            Telemetry.TELEMETRY_ENABLED = true;

            // 이전 세션까지 쌓인 프로필 원본 카운터 + 사망 이력 복원.
            BossDataPersistence.Load(BossDataPersistence.CurrentSlot);

            _awake = true;
            Debug.Log($"[묵운산군] 일어나! 일할 시간이야!  ({reason})");
        }
        catch (System.Exception e)
        {
            _awake = false;
            Debug.LogWarning($"[묵운산군] 아직 안 일어남 ({reason}): {e.Message} - 다음 스테이지에서 다시 깨웁니다.");
        }
    }

    private static void Persist(string reason)
    {
        try
        {
            BossDataPersistence.Save(BossDataPersistence.CurrentSlot);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[묵운산군] 학습 데이터 저장 실패 ({reason}): {e.Message}");
        }
    }

    // 새 게임 시작 시 호출 - 이전 판의 학습 내용을 물려받지 않도록 백지로 되돌리고 다시 깨운다.
    public static void ResetForNewGame()
    {
        BossDataPersistence.NewGame(BossDataPersistence.CurrentSlot);
        _awake = true;
        Debug.Log("[묵운산군] 일어나! 일할 시간이야!  (새 게임 - 백지에서 다시 배웁니다)");
    }
}
