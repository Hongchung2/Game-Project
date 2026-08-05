using System.Collections.Generic;
using UnityEngine;

// 명세서: "보스방 진입 후 플레이어 위치를 그리드 단위로 누적 카운트해서 최다 체류 타일 사용
// (재도전 시 이전 시도 누적분 포함)". 그리드 해상도는 명세서 미기재라 임의로 2유닛/칸으로
// 정함(30x30 방 기준 15x15칸 — 판단 필요 항목, 보고 대상).
// "재도전 시 누적 유지"만 명시돼 있고 세이브 파일 스키마(10장)엔 이 데이터가 없어서,
// 게임을 완전히 재시작하면 초기화되는 세션 내 누적으로 구현함(디스크 저장은 안 함).
public static class HabitualPositionTracker
{
    public const float CELL_SIZE = 2f; // 판단 필요 항목
    private const float SAMPLE_INTERVAL = 0.5f;

    private static readonly Dictionary<Vector2Int, int> _cellCounts = new Dictionary<Vector2Int, int>();
    private static float _sampleTimer;

    public static void Tick(Vector2 playerPosition, float deltaTime)
    {
        _sampleTimer += deltaTime;
        if (_sampleTimer < SAMPLE_INTERVAL) return;
        _sampleTimer = 0f;

        Vector2Int cell = ToCell(playerPosition);
        _cellCounts.TryGetValue(cell, out int count);
        _cellCounts[cell] = count + 1;
    }

    public static Vector2 GetMostVisitedWorldPosition(Vector2 fallback)
    {
        if (_cellCounts.Count == 0) return fallback;

        Vector2Int best = default;
        int bestCount = -1;
        foreach (var kv in _cellCounts)
        {
            if (kv.Value > bestCount)
            {
                bestCount = kv.Value;
                best = kv.Key;
            }
        }
        return ToWorldCenter(best);
    }

    // 새 게임 시작 시에만 호출할 것(재도전에서는 유지되어야 하므로 BossDataPersistence.NewGame 등에서만 연결).
    public static void ResetAll() => _cellCounts.Clear();

    private static Vector2Int ToCell(Vector2 pos) =>
        new Vector2Int(Mathf.FloorToInt(pos.x / CELL_SIZE), Mathf.FloorToInt(pos.y / CELL_SIZE));

    private static Vector2 ToWorldCenter(Vector2Int cell) =>
        new Vector2(cell.x * CELL_SIZE + CELL_SIZE / 2f, cell.y * CELL_SIZE + CELL_SIZE / 2f);
}
