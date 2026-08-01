using System;

// 명세서 1장 채널링 매핑 - 어느 몬스터의 습성을 빌린 기술인지. Telemetry(실측 데이터 파이프라인)와
// 섞지 않으려고 순수 장식용 신호를 별도 이벤트로 분리함(M8c 실루엣 전용).
public enum ChanneledMonster { Mukryeong, Meokdeungbul, Heosuabi, Byeoruge }

public static class ChannelingSignal
{
    public static event Action<ChanneledMonster, float> OnChannelingStart;

    public static void Raise(ChanneledMonster monster, float telegraphDuration)
    {
        OnChannelingStart?.Invoke(monster, telegraphDuration);
    }
}
