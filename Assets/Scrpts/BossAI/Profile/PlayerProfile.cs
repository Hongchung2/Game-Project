using System;

// 산수원의 주인 LLM에 보낼 플레이어 5축 프로필 데이터 구조 (명세서 4.1/4.2 참고).
// JsonUtility로 직렬화 가능하도록 Dictionary 없이 고정 필드로 구성했다
// (tanker_comparison도 실제로 쓰이는 건 허수아비/벼루게 두 종뿐이라 고정 필드로 충분함, 명세서 4.1 주의사항 참고).
[Serializable]
public class ReactionAxis
{
    public float shortSuccessRate;
    public float midSuccessRate;
    public float longSuccessRate;
    public string tier = "mid";
    public int sample;
}

[Serializable]
public class RangeAxis
{
    public float meleeRatio;
    public string tier = "mid";
    public int sample;
}

[Serializable]
public class MonsterHitRate
{
    public float rate;
    public int sample;
}

[Serializable]
public class TankerComparisonAxis
{
    public MonsterHitRate scarecrow = new MonsterHitRate();  // 허수아비 (스테이지1)
    public MonsterHitRate byeoruge = new MonsterHitRate();   // 벼루게 (스테이지2)
    public string weightedFavor = "데이터 부족";              // "허수아비" | "벼루게" | "데이터 부족"
}

[Serializable]
public class PunishAxis
{
    public float rate;
    public string tier = "mid";
    public int sample;
}

[Serializable]
public class DodgeBiasAxis
{
    public float leftRatio;
    public string tier = "none";
    public int sample;
}

[Serializable]
public class PlayerProfile
{
    public ReactionAxis reaction = new ReactionAxis();
    public RangeAxis range = new RangeAxis();
    public TankerComparisonAxis tankerComparison = new TankerComparisonAxis();
    public PunishAxis punish = new PunishAxis();
    public DodgeBiasAxis dodgeBias = new DodgeBiasAxis();
}
