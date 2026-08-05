#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// C#의 BossDecisionClient가 실제로 만드는 body/message/signature를 로그로 뽑아서, 외부에서
// (openssl 등) 독립적으로 재계산한 HMAC과 바이트 단위로 비교하기 위한 자체 검증 도구.
// bash로 알고리즘을 재현하는 것과 달리, 이건 프로덕션 코드 그 자체를 실행한 결과다 —
// 구분자/인코딩 시점/hex-vs-base64 같은 구현 디테일 버그까지 잡아낼 수 있음.
public static class BossDecisionClientSignatureSelfTest
{
    [MenuItem("Tools/Boss/Self-Test HMAC Signature")]
    public static void Run()
    {
        var profile = new PlayerProfile();
        profile.reaction.shortSuccessRate = 0.35f;
        profile.reaction.midSuccessRate = 0.61f;
        profile.reaction.longSuccessRate = 0.80f;
        profile.reaction.tier = "low";
        profile.reaction.sample = 12;
        profile.range.meleeRatio = 0.22f;
        profile.range.tier = "ranged";
        profile.range.sample = 18;
        profile.tankerComparison.scarecrow.rate = 0.28f;
        profile.tankerComparison.scarecrow.sample = 4;
        profile.tankerComparison.byeoruge.rate = 0.15f;
        profile.tankerComparison.byeoruge.sample = 6;
        profile.tankerComparison.weightedFavor = "허수아비";
        profile.punish.rate = 0.55f;
        profile.punish.tier = "mid";
        profile.punish.sample = 10;
        profile.dodgeBias.leftRatio = 0.68f;
        profile.dodgeBias.tier = "left";
        profile.dodgeBias.sample = 9;

        var deathHistory = new DeathHistory();

        // 실제 서버로 재전송해서 검증할 것까지 고려해 "지금 시각"을 씀(고정값은 시계 허용오차
        // ±5분을 넘기기 쉬움 - Unity 배치모드 기동 시간만으로도 수십 초가 걸림).
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        string secret = ReadSharedSecretFromDevVars();
        if (string.IsNullOrEmpty(secret))
        {
            Debug.LogError("[SelfTest] boss-proxy/.dev.vars에서 SHARED_SECRET을 못 읽음");
            return;
        }

        var result = BossDecisionClient.DebugComputeSignature(profile, deathHistory, secret, timestamp);

        Debug.Log("[SelfTest] BODY_START>>>" + result.body + "<<<BODY_END");
        Debug.Log("[SelfTest] MESSAGE_START>>>" + result.message + "<<<MESSAGE_END");
        Debug.Log("[SelfTest] SIGNATURE=" + result.signature);
    }

    // .dev.vars에 저장된 값을 그대로 읽기만 함(하드코딩 금지, 로컬 dev 전용 값이라 커밋 파일엔 안 남김).
    private static string ReadSharedSecretFromDevVars()
    {
        string path = Path.Combine(Application.dataPath, "..", "boss-proxy", ".dev.vars");
        if (!File.Exists(path)) return null;

        foreach (var line in File.ReadAllLines(path))
        {
            if (line.StartsWith("SHARED_SECRET="))
                return line.Substring("SHARED_SECRET=".Length);
        }
        return null;
    }
}
#endif
