using System;
using System.Collections.Generic;
using UnityEngine;

// 명세서 4.5: 최근 3회는 상세, 그 이전은 요약만(토큰 절약).
[Serializable]
public class SerializableVector2
{
    public float x;
    public float y;

    public SerializableVector2() { }
    public SerializableVector2(Vector2 v) { x = v.x; y = v.y; }
    public Vector2 ToVector2() => new Vector2(x, y);
}

[Serializable]
public class DeathRecord
{
    public int attempt;
    public string diedBy;
    public SerializableVector2 position = new SerializableVector2();
    public string dodgeDirectionLast;
}

[Serializable]
public class DeathHistorySummary
{
    public int totalAttempts;
    public string mostCommonCause = "";
    public int mostCommonCauseCount;
}

[Serializable]
public class DeathHistory
{
    public List<DeathRecord> recent = new List<DeathRecord>();
    public DeathHistorySummary summaryOlder = new DeathHistorySummary();
}

// 보스전 사망 기록 누적. "older" 버킷의 최다 원인 집계는 실제 raw 카운트를 저장하지 않고
// (명세서 스키마에 없음) 요약값(원인+횟수)만 이어받으므로, 로드 직후 동률 원인이 바뀌는
// 경우의 수는 근사치다 — LLM 프롬프트용 참고 데이터라 정밀도보다 단순함을 택함.
public static class DeathHistoryTracker
{
    private const int MAX_RECENT = 3;
    private static readonly Dictionary<string, int> _causeCounts = new Dictionary<string, int>();

    public static DeathHistory Current { get; private set; } = new DeathHistory();

    public static void RecordDeath(string diedBy, Vector2 position, string dodgeDirectionLast)
    {
        int attemptNumber = Current.summaryOlder.totalAttempts + Current.recent.Count + 1;
        Current.recent.Add(new DeathRecord
        {
            attempt = attemptNumber,
            diedBy = diedBy,
            position = new SerializableVector2(position),
            dodgeDirectionLast = dodgeDirectionLast,
        });

        while (Current.recent.Count > MAX_RECENT)
        {
            var oldest = Current.recent[0];
            Current.recent.RemoveAt(0);
            FoldIntoSummary(oldest);
        }
    }

    private static void FoldIntoSummary(DeathRecord record)
    {
        Current.summaryOlder.totalAttempts++;

        string cause = string.IsNullOrEmpty(record.diedBy) ? "알수없음" : record.diedBy;
        _causeCounts.TryGetValue(cause, out int count);
        _causeCounts[cause] = count + 1;

        string bestCause = Current.summaryOlder.mostCommonCause;
        int bestCount = Current.summaryOlder.mostCommonCauseCount;
        foreach (var kv in _causeCounts)
        {
            if (kv.Value > bestCount)
            {
                bestCause = kv.Key;
                bestCount = kv.Value;
            }
        }
        Current.summaryOlder.mostCommonCause = bestCause;
        Current.summaryOlder.mostCommonCauseCount = bestCount;
    }

    public static void RestoreFrom(DeathHistory history)
    {
        if (history == null)
        {
            ResetAll();
            return;
        }

        Current = history;
        if (Current.recent == null) Current.recent = new List<DeathRecord>();
        if (Current.summaryOlder == null) Current.summaryOlder = new DeathHistorySummary();

        // raw 카운트는 저장되지 않으므로, 이어서 집계할 수 있도록 알려진 최다원인만 시드로 되살린다.
        _causeCounts.Clear();
        if (!string.IsNullOrEmpty(Current.summaryOlder.mostCommonCause))
            _causeCounts[Current.summaryOlder.mostCommonCause] = Current.summaryOlder.mostCommonCauseCount;
    }

    public static void ResetAll()
    {
        Current = new DeathHistory();
        _causeCounts.Clear();
    }
}
