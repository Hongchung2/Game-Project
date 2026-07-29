using System.Collections.Generic;

// 플레이어가 모은 계절 족자를 씬 전환에도 유지하며 기억하는 데이터 저장소.
// static이라 플레이 세션 동안 씬을 넘어가도 값이 유지됨 (플레이 중지 시 초기화).
public static class ScrollCollection
{
    // 표시 순서 고정: 춘(spring) → 하(summer) → 추(fall) → 동(winter)
    public static readonly string[] Seasons = { "spring", "summer", "fall", "winter" };

    private static readonly HashSet<string> _collected = new HashSet<string>();

    public static void Collect(string season)
    {
        _collected.Add(season);
        // 화면에 표시된 UI가 있으면 즉시 갱신
        if (ScrollCollectionUI.Instance != null)
            ScrollCollectionUI.Instance.Refresh();
    }

    public static bool IsCollected(string season)
    {
        return _collected.Contains(season);
    }

    // 4개 계절 족자를 전부 모았는지 (겨울방 엔딩 트리거용)
    public static bool AllCollected()
    {
        foreach (var season in Seasons)
        {
            if (!_collected.Contains(season)) return false;
        }
        return true;
    }

    // 테스트/리셋용
    public static void ClearAll()
    {
        _collected.Clear();
        if (ScrollCollectionUI.Instance != null)
            ScrollCollectionUI.Instance.Refresh();
    }
}
