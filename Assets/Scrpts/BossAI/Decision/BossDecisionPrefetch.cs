using System.Collections;
using UnityEngine;

// 작업지시서 #09 - 인터미션 영상이 재생되는 동안 클로드 API 호출을 미리 시작해서, 보스방
// 입장 시점엔 이미 응답이 와있게 만든다(원래 인터미션을 넣은 목적 그 자체 - API 지연을
// 영상 재생 시간 뒤로 숨기는 것). 씬이 바뀌어도(인터미션 → 보스방) 요청이 끊기지 않도록
// DontDestroyOnLoad 오브젝트에서 코루틴을 돌린다.
public static class BossDecisionPrefetch
{
    private class Runner : MonoBehaviour { }

    private static Runner _runner;
    private static BossDecision _decision;
    private static bool _isReady;
    private static bool _isRequesting;

    public static bool IsReady => _isReady;
    public static bool HasPendingOrReady => _isRequesting || _isReady;

    // 인터미션 씬 시작 시 호출.
    public static void BeginPrefetch()
    {
        if (_isRequesting || _isReady) return;

        Debug.Log("[BossDecisionPrefetch] 인터미션 진입 - 클로드 API 미리 요청 시작");

        if (_runner == null)
        {
            var go = new GameObject("BossDecisionPrefetchRunner");
            Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<Runner>();
        }

        _isRequesting = true;
        _runner.StartCoroutine(PrefetchRoutine());
    }

    private static IEnumerator PrefetchRoutine()
    {
        var profile = PlayerProfileAggregator.GetCurrentProfile();
        var history = DeathHistoryTracker.Current;

        BossDecision decision = null;
        yield return BossDecisionClient.RequestDecision(profile, history, d => decision = d);

        _decision = decision ?? new BossDecision();
        _isReady = true;
        _isRequesting = false;
        Debug.Log("[BossDecisionPrefetch] 프리페치 완료 - 보스방 입장 시 바로 쓸 수 있음");
    }

    // 보스방에서 결과를 가져가면서 상태를 리셋(다음 재도전 때 다시 프리페치하도록).
    public static BossDecision Consume()
    {
        BossDecision d = _decision;
        Reset();
        return d;
    }

    public static void Reset()
    {
        _decision = null;
        _isReady = false;
        _isRequesting = false;
        if (_runner != null)
        {
            Object.Destroy(_runner.gameObject);
            _runner = null;
        }
    }
}
