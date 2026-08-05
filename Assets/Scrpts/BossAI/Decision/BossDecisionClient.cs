using System;
using System.Collections;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// 명세서 5장: boss-proxy(Cloudflare Workers)를 HMAC-SHA256 서명으로 호출해 BossDecision을 받는다.
// 실패/타임아웃/킬스위치/스키마검증실패 등 모든 경우에 반드시 BossDecisionFallback으로 대체해서
// onComplete가 항상 유효한 BossDecision 하나로 콜백되게 보장한다(5초 안에 결정 확정, 명세서 5.1).
public static class BossDecisionClient
{
    // TODO(배포 전): 실제 배포된 Cloudflare Workers URL로 교체.
    // 에디터에서는 로컬 wrangler dev(127.0.0.1:8787)를 기본값으로 자동 설정함 - F12로 매번
    // 켜지 않아도 Play할 때마다 실제 프록시를 타도록(플레이테스트 편의, 빌드에는 영향 없음).
    public static string ProxyBaseUrl =
#if UNITY_EDITOR
        "http://127.0.0.1:8787";
#else
        "";
#endif

    // TODO(배포 전): 하드코딩 금지 — 빌드 설정/환경변수로 주입할 것. 지금은 개발용 placeholder.
    // 에디터에서는 boss-proxy/.dev.vars에서 로컬 전용 시크릿을 자동으로 읽어옴(소스에 실제
    // 값을 적지 않음 - 배포용 시크릿이 아니라 로컬 wrangler dev 전용 개발값이라 안전).
    public static string SharedSecret =
#if UNITY_EDITOR
        ReadLocalDevSharedSecretForEditor();
#else
        "";
#endif

#if UNITY_EDITOR
    private static string ReadLocalDevSharedSecretForEditor()
    {
        try
        {
            string path = System.IO.Path.Combine(Application.dataPath, "..", "boss-proxy", ".dev.vars");
            foreach (var line in System.IO.File.ReadAllLines(path))
            {
                if (line.StartsWith("SHARED_SECRET="))
                    return line.Substring("SHARED_SECRET=".Length);
            }
        }
        catch
        {
            // 로컬 프록시가 아직 세팅 안 된 환경일 수 있음 - 조용히 빈 값으로 폴백(정상 폴백 경로 유지).
        }
        return "";
    }
#endif

    private const string ENDPOINT_PATH = "/boss-decision";
    // 실측 결과 프록시 쪽 예산(1차 5초 + 재시도 1.5초 = 최대 6.5초)이 명세서 5.1의 3.5+1.5=5초보다
    // 늘어났는데 클라이언트 타임아웃이 5초 그대로였음 - 정상 응답이 도착하기 전에 클라이언트가
    // 먼저 끊어버려서 폴백으로 넘어가는 경합이 실제 플레이테스트 로그에서 확인됨(Request timeout).
    // 프록시 쪽 최대 예산보다 여유 있게 잡음.
    private const int CLIENT_TIMEOUT_SECONDS = 8;
    private const float WEIGHT_MIN = 0.3f;
    private const float WEIGHT_MAX = 0.7f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // M9 디버그 도구 전용 - LLM 호출 ON/OFF. true면 프록시 호출을 아예 건너뛰고 폴백 규칙 강제 적용.
    public static bool ForceFallbackForDebug = false;

    // M9 디버그 도구 전용 - BossDecision 강제 주입(LLM 호출 스킵). null이 아니면 이 값을 그대로 콜백.
    public static BossDecision DebugInjectedDecision = null;
#endif

    public static IEnumerator RequestDecision(PlayerProfile profile, DeathHistory deathHistory, Action<BossDecision> onComplete)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (DebugInjectedDecision != null)
        {
            onComplete?.Invoke(DebugInjectedDecision);
            yield break;
        }

        if (ForceFallbackForDebug)
        {
            onComplete?.Invoke(BossDecisionFallback.Generate(profile, deathHistory));
            yield break;
        }
#endif

        if (string.IsNullOrEmpty(ProxyBaseUrl))
        {
            Debug.LogWarning("[BossDecisionClient] ProxyBaseUrl이 비어있어 폴백 규칙으로 대체합니다.");
            onComplete?.Invoke(BossDecisionFallback.Generate(profile, deathHistory));
            yield break;
        }

        string body = BuildRequestBody(profile, deathHistory);

        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string signature = ComputeHmacSha256Hex(SharedSecret, timestamp + "." + body);

        string url = ProxyBaseUrl.TrimEnd('/') + ENDPOINT_PATH;
        Debug.Log($"[BossDecisionClient] 요청 전송 → {url}");

        BossDecision decision;
        using (var request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Timestamp", timestamp.ToString(CultureInfo.InvariantCulture));
            request.SetRequestHeader("X-Signature", signature);
            request.timeout = CLIENT_TIMEOUT_SECONDS;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[BossDecisionClient] ⚠ 프록시 호출 실패({request.error}) → 폴백 규칙으로 대체합니다.");
                decision = BossDecisionFallback.Generate(profile, deathHistory);
            }
            else
            {
                Debug.Log($"[BossDecisionClient] 응답 수신 (HTTP {request.responseCode}): {request.downloadHandler.text}");
                decision = ParseResponse(request.downloadHandler.text, profile, deathHistory);
            }
        }

        LogDecisionSummary(decision);
        onComplete?.Invoke(decision);
    }

    private static void LogDecisionSummary(BossDecision d)
    {
        if (d == null) return;

        float mountain = d.mark_weight != null ? d.mark_weight.mountain : 0f;
        float cloud = d.mark_weight != null ? d.mark_weight.cloud : 0f;
        float punch = d.phase2_pattern_weight != null ? d.phase2_pattern_weight.punch : 0f;
        float slam = d.phase2_pattern_weight != null ? d.phase2_pattern_weight.slam : 0f;

        Debug.Log(
            $"[BossDecisionClient] 최종 판단 - confidence={d.confidence}, " +
            $"mark_weight(산/구름)={mountain:0.00}/{cloud:0.00}, " +
            $"phase2(펀치/슬램)={punch:0.00}/{slam:0.00}, " +
            $"대사1=\"{d.boss_line_p1}\", 대사2=\"{d.boss_line_transition}\"");
    }

    private static BossDecision ParseResponse(string json, PlayerProfile profile, DeathHistory deathHistory)
    {
        try
        {
            var body = JsonUtility.FromJson<BossDecisionResponseBody>(json);

            // decision==null: 킬스위치/그 외 서버측 폴백. meta.fallback: 서버가 명시적으로 폴백했다고 알려주는 신호.
            if (body == null || !body.ok || body.decision == null || (body.meta != null && body.meta.fallback))
            {
                Debug.LogWarning("[BossDecisionClient] ⚠ 서버가 폴백 응답을 보냄(킬스위치 등) → 폴백 규칙으로 대체합니다.");
                return BossDecisionFallback.Generate(profile, deathHistory);
            }

            if (!IsStructurallyValid(body.decision))
            {
                Debug.LogWarning("[BossDecisionClient] ⚠ 응답 구조 검증 실패, 폴백 규칙으로 대체합니다.");
                return BossDecisionFallback.Generate(profile, deathHistory);
            }

            ClampWeights(body.decision);
            Debug.Log("[BossDecisionClient] ✅ 실제 클로드 API 응답 사용");
            return body.decision;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[BossDecisionClient] ⚠ 응답 파싱 실패({e.Message}), 폴백 규칙으로 대체합니다.");
            return BossDecisionFallback.Generate(profile, deathHistory);
        }
    }

    private static bool IsStructurallyValid(BossDecision d)
    {
        if (d.mark_weight == null || d.phase2_pattern_weight == null) return false;
        if (d.mountain_target != "habitual_position" && d.mountain_target != "current_position") return false;
        if (d.confidence != "low" && d.confidence != "mid" && d.confidence != "high") return false;
        if (string.IsNullOrEmpty(d.boss_line_p1) || string.IsNullOrEmpty(d.boss_line_transition)) return false;
        return true;
    }

    // 프록시(clampDecisionWeights)에서도 0.3~0.7로 clamp하지만, 명세서 5.3 요구대로 클라이언트에서도 이중 방어.
    private static void ClampWeights(BossDecision d)
    {
        d.mark_weight.mountain = Mathf.Clamp(d.mark_weight.mountain, WEIGHT_MIN, WEIGHT_MAX);
        d.mark_weight.cloud = Mathf.Clamp(d.mark_weight.cloud, WEIGHT_MIN, WEIGHT_MAX);
        d.phase2_pattern_weight.punch = Mathf.Clamp(d.phase2_pattern_weight.punch, WEIGHT_MIN, WEIGHT_MAX);
        d.phase2_pattern_weight.slam = Mathf.Clamp(d.phase2_pattern_weight.slam, WEIGHT_MIN, WEIGHT_MAX);
    }

    // rate limit용 안정적 식별자. 세션마다 바뀌면 재실행으로 시간당 한도를 우회할 수 있어서
    // 설치 1회당 하나만 생성되는 PlayerInstallId를 씀(M2, 새 게임 시작해도 유지됨).
    private static string GetSessionId() => PlayerInstallId.Get();

    private static string ComputeHmacSha256Hex(string secret, string message)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret ?? "")))
        {
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    // token-check/sample-profile.json 형태 그대로 맞춤(허수아비/벼루게 한글 키, reaction.raw 중첩 등) —
    // 프록시는 player_profile을 unknown으로 그대로 Claude에 전달하므로, 이미 토큰 검증까지 끝낸
    // 이 모양을 그대로 재사용하는 게 가장 안전함. JsonUtility로는 한글 동적 키를 못 만들어서
    // Telemetry.cs와 같은 수동 문자열 조립 방식을 그대로 씀.
    private static string BuildPlayerProfileJson(PlayerProfile profile, DeathHistory deathHistory)
    {
        var sb = new StringBuilder();
        sb.Append('{');

        sb.Append("\"reaction\":{\"raw\":{")
          .Append("\"short\":").Append(F(profile.reaction.shortSuccessRate)).Append(',')
          .Append("\"mid\":").Append(F(profile.reaction.midSuccessRate)).Append(',')
          .Append("\"long\":").Append(F(profile.reaction.longSuccessRate))
          .Append("},\"tier\":\"").Append(profile.reaction.tier).Append("\",\"sample\":").Append(profile.reaction.sample).Append("},");

        sb.Append("\"range\":{\"melee_ratio\":").Append(F(profile.range.meleeRatio))
          .Append(",\"tier\":\"").Append(profile.range.tier).Append("\",\"sample\":").Append(profile.range.sample).Append("},");

        sb.Append("\"tanker_comparison\":{")
          .Append("\"허수아비\":{\"rate\":").Append(F(profile.tankerComparison.scarecrow.rate))
          .Append(",\"sample\":").Append(profile.tankerComparison.scarecrow.sample).Append(",\"stage\":1},")
          .Append("\"벼루게\":{\"rate\":").Append(F(profile.tankerComparison.byeoruge.rate))
          .Append(",\"sample\":").Append(profile.tankerComparison.byeoruge.sample).Append(",\"stage\":2},")
          .Append("\"weighted_favor\":\"").Append(EscapeJson(profile.tankerComparison.weightedFavor)).Append("\"},");

        sb.Append("\"punish\":{\"rate\":").Append(F(profile.punish.rate))
          .Append(",\"tier\":\"").Append(profile.punish.tier).Append("\",\"sample\":").Append(profile.punish.sample).Append("},");

        sb.Append("\"dodge_bias\":{\"left_ratio\":").Append(F(profile.dodgeBias.leftRatio))
          .Append(",\"tier\":\"").Append(profile.dodgeBias.tier).Append("\",\"sample\":").Append(profile.dodgeBias.sample).Append("},");

        sb.Append("\"death_history\":").Append(BuildDeathHistoryJson(deathHistory));

        sb.Append('}');
        return sb.ToString();
    }

    private static string BuildDeathHistoryJson(DeathHistory history)
    {
        var sb = new StringBuilder();
        sb.Append("{\"recent\":[");
        for (int i = 0; i < history.recent.Count; i++)
        {
            if (i > 0) sb.Append(',');
            var r = history.recent[i];
            sb.Append("{\"attempt\":").Append(r.attempt)
              .Append(",\"died_by\":\"").Append(EscapeJson(r.diedBy)).Append('"')
              .Append(",\"position\":{\"x\":").Append(F(r.position.x)).Append(",\"y\":").Append(F(r.position.y)).Append('}')
              .Append(",\"dodge_direction_last\":\"").Append(EscapeJson(r.dodgeDirectionLast)).Append("\"}");
        }
        sb.Append(']');

        if (history.summaryOlder.totalAttempts <= 0)
        {
            sb.Append(",\"summary_older\":null");
        }
        else
        {
            sb.Append(",\"summary_older\":{\"total_attempts\":").Append(history.summaryOlder.totalAttempts)
              .Append(",\"most_common_cause\":\"").Append(EscapeJson(history.summaryOlder.mostCommonCause)).Append('"')
              .Append(",\"most_common_cause_count\":").Append(history.summaryOlder.mostCommonCauseCount).Append('}');
        }
        sb.Append('}');
        return sb.ToString();
    }

    private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string BuildRequestBody(PlayerProfile profile, DeathHistory deathHistory)
    {
        string playerProfileJson = BuildPlayerProfileJson(profile, deathHistory);
        int attempt = deathHistory.summaryOlder.totalAttempts + deathHistory.recent.Count + 1;
        return "{\"session_id\":\"" + EscapeJson(GetSessionId()) + "\",\"player_profile\":" + playerProfileJson + ",\"attempt\":" + attempt + "}";
    }

#if UNITY_EDITOR
    // 실제 네트워크 호출 없이, 프로덕션과 완전히 동일한 본문 조립+서명 경로를 그대로 실행해서
    // 결과를 검사할 수 있게 하는 테스트 전용 훅(BossDecisionClientSignatureSelfTest에서만 사용).
    // bash/openssl로 알고리즘만 재현하는 것과 달리, 이 메서드는 C# 코드가 실제로 만드는
    // body/message/signature 그 자체를 그대로 반환한다.
    public static (string body, string message, string signature) DebugComputeSignature(
        PlayerProfile profile, DeathHistory deathHistory, string secret, long timestamp)
    {
        string body = BuildRequestBody(profile, deathHistory);
        string message = timestamp + "." + body;
        string signature = ComputeHmacSha256Hex(secret, message);
        return (body, message, signature);
    }
#endif
}
