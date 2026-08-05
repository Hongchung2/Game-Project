using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

// 스테이지1·2 몬스터 + 보스 전투 계측 이벤트 발행 모듈.
// 이 파일은 몬스터 스크립트나 PlayerProfileAggregator를 import하지 않는다 — 호출부(몬스터 스크립트)가
// 이 모듈을 부르고, 구독부(PlayerProfileAggregator)가 아래 C# 이벤트를 구독하는 방향으로만
// 의존성이 흐른다(Observer 패턴). 모든 발행은 try-catch로 감싸서 계측 오류가 전투를 절대
// 멈추지 않게 한다. TELEMETRY_ENABLED로 언제든 전체를 끌 수 있다.
//
// stage: 1=스테이지1(창병/까마귀/허수아비), 2=스테이지2(벼루게/먹등불/묵령), 3=보스(묵운산군) 자신.
// 명세서 4.4의 스테이지1·2 가중평균 정책에 쓰인다.
public static class Telemetry
{
    public static bool TELEMETRY_ENABLED = true;

    // PlayerProfileAggregator 등 구독부가 붙는 이벤트. 발행 실패가 구독자에게 전파되지 않도록
    // Publish() 내부에서 try-catch로 감싼 뒤에 호출한다.
    public static event Action<string, float, int> OnAttackTelegraphStart;
    public static event Action<string, float, int> OnAttackHit;
    public static event Action<string, float, string, int> OnAttackDodged;
    public static event Action<string, int> OnPlayerPunish;
    public static event Action<string, float, bool, int> OnPlayerPositionSample;

    public static void AttackTelegraphStart(string monsterType, float telegraphDuration, int stage)
    {
        Publish("attack_telegraph_start", new Dictionary<string, object>
        {
            { "monster_type", monsterType },
            { "telegraph_duration", telegraphDuration },
            { "stage", stage },
        });
        InvokeSafely(() => OnAttackTelegraphStart?.Invoke(monsterType, telegraphDuration, stage));
    }

    public static void AttackHit(string monsterType, float telegraphDuration, int stage)
    {
        Publish("attack_hit", new Dictionary<string, object>
        {
            { "monster_type", monsterType },
            { "telegraph_duration", telegraphDuration },
            { "stage", stage },
        });
        InvokeSafely(() => OnAttackHit?.Invoke(monsterType, telegraphDuration, stage));
    }

    public static void AttackDodged(string monsterType, float telegraphDuration, string dodgeDirection, int stage)
    {
        Publish("attack_dodged", new Dictionary<string, object>
        {
            { "monster_type", monsterType },
            { "telegraph_duration", telegraphDuration },
            { "dodge_direction", dodgeDirection },
            { "stage", stage },
        });
        InvokeSafely(() => OnAttackDodged?.Invoke(monsterType, telegraphDuration, dodgeDirection, stage));
    }

    public static void PlayerPunish(string monsterType, int stage)
    {
        Publish("player_punish", new Dictionary<string, object>
        {
            { "monster_type", monsterType },
            { "stage", stage },
        });
        InvokeSafely(() => OnPlayerPunish?.Invoke(monsterType, stage));
    }

    public static void PlayerPositionSample(string monsterType, float distance, bool inMeleeRange, int stage)
    {
        Publish("player_position_sample", new Dictionary<string, object>
        {
            { "monster_type", monsterType },
            { "distance", distance },
            { "in_melee_range", inMeleeRange },
            { "stage", stage },
        });
        InvokeSafely(() => OnPlayerPositionSample?.Invoke(monsterType, distance, inMeleeRange, stage));
    }

    // 회피 방향 계산(위치 변화량 기반): 예고 시작 시점 위치 대비 판정 시점 위치의 x 변화로 좌/우/none 판정.
    // (Rigidbody2D.linearVelocity 기반이었으나, 플레이어가 MovePosition()으로 이동해서
    //  linearVelocity가 갱신 안 되는 문제가 있어 위치 변화량 방식으로 교체함)
    public static string ComputeDodgeDirectionFromPositions(Vector2 previousPosition, Vector2 currentPosition, float threshold = 0.01f)
    {
        float dx = currentPosition.x - previousPosition.x;
        if (dx < -threshold) return "left";
        if (dx > threshold) return "right";
        return "none";
    }

    private static void InvokeSafely(Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Telemetry] 구독자(PlayerProfileAggregator 등) 처리 실패 (전투 로직에는 영향 없음): {e.Message}");
        }
    }

    private static void Publish(string eventName, Dictionary<string, object> payload)
    {
        if (!TELEMETRY_ENABLED) return;

        try
        {
            Debug.Log($"[Telemetry] {ToJson(eventName, payload)}");
            // TODO: 실제 영속 저장/서버 전송이 필요해지면 여기에 추가. 지금은 콘솔 로그 + 구독 이벤트로 소비.
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Telemetry] 이벤트 발행 실패 (전투 로직에는 영향 없음): {e.Message}");
        }
    }

    private static string ToJson(string eventName, Dictionary<string, object> payload)
    {
        var sb = new StringBuilder();
        sb.Append("{\"event\":\"").Append(eventName).Append('"');

        foreach (var kv in payload)
        {
            sb.Append(",\"").Append(kv.Key).Append("\":");
            switch (kv.Value)
            {
                case string s:
                    sb.Append('"').Append(s).Append('"');
                    break;
                case bool b:
                    sb.Append(b ? "true" : "false");
                    break;
                case float f:
                    sb.Append(f.ToString("0.###", CultureInfo.InvariantCulture));
                    break;
                default:
                    sb.Append(Convert.ToString(kv.Value, CultureInfo.InvariantCulture));
                    break;
            }
        }

        sb.Append('}');
        return sb.ToString();
    }
}
