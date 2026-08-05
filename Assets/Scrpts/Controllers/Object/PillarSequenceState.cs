using System;
using System.Collections.Generic;

// 작업지시서 #09 - 겨울방 기둥 4개(봄/여름/가을/겨울) 진행 상태.
// 왼쪽(seasonIndex 낮은 것)부터 순서대로만 채울 수 있고, 4개 다 채워지면 OnAllPillarsFilled를 쏜다.
public static class PillarSequenceState
{
    public static event Action OnAllPillarsFilled;

    private static readonly HashSet<int> _filled = new HashSet<int>();

    public static bool IsFilled(int seasonIndex) => _filled.Contains(seasonIndex);

    // 다음으로 채워야 하는 인덱스(0~3). 전부 채웠으면 4.
    public static int NextRequiredIndex()
    {
        for (int i = 0; i < 4; i++)
        {
            if (!_filled.Contains(i)) return i;
        }
        return 4;
    }

    public static bool CanFill(int seasonIndex) => seasonIndex == NextRequiredIndex();

    public static void Fill(int seasonIndex)
    {
        if (!CanFill(seasonIndex)) return;

        _filled.Add(seasonIndex);
        if (_filled.Count >= 4) OnAllPillarsFilled?.Invoke();
    }

    // 테스트/재시작용
    public static void ClearAll()
    {
        _filled.Clear();
    }
}
