using UnityEngine;

// 명세서 M7 폴백 대사 풀. 감정 단계별 + 재도전 전용 풀 그대로.
public static class FallbackDialoguePool
{
    private static readonly string[] Complacency = { "…시작하지.", "…", "와 봐라." };
    private static readonly string[] Unease = { "또 그 자리군.", "익숙한 냄새군.", "봄에도 그러더니." };
    private static readonly string[] Awareness = { "짚 인형 흉내, 여전히 서투르지.", "벼루보다 못한 방어로군.", "먹물 냄새가 익숙하군." };
    private static readonly string[] Chill = { "이제 알겠지.", "다르지 않다." };
    private static readonly string[] Retry = { "저번엔 오른쪽이었지.", "또 같은 자리인가.", "달라진 게 없군." };

    // 명세서 5.5: "death_history 있으면 전용 풀 사용" — 감정 단계와 무관하게 재도전 풀 우선.
    public static string GetLine(EmotionStage stage, bool hasDeathHistory)
    {
        if (hasDeathHistory) return Pick(Retry);

        switch (stage)
        {
            case EmotionStage.Complacency: return Pick(Complacency);
            case EmotionStage.Unease: return Pick(Unease);
            case EmotionStage.Awareness: return Pick(Awareness);
            case EmotionStage.Chill: return Pick(Chill);
            default: return Pick(Unease);
        }
    }

    private static string Pick(string[] pool) => pool[Random.Range(0, pool.Length)];
}
