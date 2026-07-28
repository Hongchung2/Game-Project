// 정화수 양동이 퍼즐을 풀었는지 여부. 인벤토리 시스템이 없어서 플래그로만 관리한다.
public static class WaterPuzzleState
{
    public static bool WaterSplit = false;

    // 퍼즐을 처음 풀었을 때 한 번 발생 (할아버지가 포탈로 데려가는 연출 트리거용)
    public static event System.Action OnSolved;

    public static void MarkSolved()
    {
        if (WaterSplit) return;
        WaterSplit = true;
        OnSolved?.Invoke();
    }
}
