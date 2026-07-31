// 서쪽/동쪽 호수 정화 여부.
public static class LakePurifyState
{
    public static bool WestPurified = false;
    public static bool EastPurified = false;
    public static bool AllPurified => WestPurified && EastPurified;
}
