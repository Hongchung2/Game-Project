using System;

// 명세서 5.3 응답 스키마. JsonUtility는 필드명을 JSON 키와 똑같이 매핑하고 별칭(rename)을
// 지원하지 않으므로, 프록시가 실제로 내려주는 snake_case 키와 이름을 그대로 맞춘다
// (여기를 카멜케이스로 바꾸면 파싱이 조용히 실패해서 전부 기본값 0/null로 들어옴).
[Serializable]
public class MarkWeight
{
    public float mountain;
    public float cloud;
}

[Serializable]
public class Phase2PatternWeight
{
    public float punch;
    public float slam;
}

[Serializable]
public class BossDecision
{
    public MarkWeight mark_weight = new MarkWeight();
    public string mountain_target = "current_position";
    public Phase2PatternWeight phase2_pattern_weight = new Phase2PatternWeight();
    public bool phase2_recovery_exposed = true;
    public string boss_line_p1 = "";
    public string boss_line_transition = "";
    public string confidence = "low";
}

[Serializable]
public class BossDecisionMeta
{
    public int cache_read_tokens;
    public int cache_creation_tokens;
    public int cache_creation_ephemeral_1h_tokens;
    public int cache_creation_ephemeral_5m_tokens;
    public int latency_ms;
    public bool fallback;
    public string reason; // "timeout" | "schema_violation" | "api_error" | "kill_switch" 또는 없으면 빈 문자열
}

[Serializable]
public class BossDecisionResponseBody
{
    public bool ok;

    // 일부러 기본값(= new BossDecision())을 안 줌 — JSON의 "decision":null이 왔을 때 필드가
    // 그대로 null로 남아있어야 킬스위치/폴백 응답을 구분할 수 있다(JsonUtility의 null 처리
    // 동작을 믿기보다, "값을 안 주면 원래 null이었다"는 C#의 기본 동작에 기대는 쪽이 더 안전함).
    public BossDecision decision;

    public BossDecisionMeta meta = new BossDecisionMeta();
}
