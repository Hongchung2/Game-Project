using System;
using System.IO;
using UnityEngine;

// 설치 1회당 하나만 생성해서 재사용하는 식별자. player_profile/death_history와 달리 "새 게임
// 시작 시 백지"의 대상이 아니라서(진행 데이터가 아니라 rate-limit용 식별자) 슬롯 파일
// (slot_{n}.json)과는 별도 파일로 둔다 — NewGame()/ResetToBlank()로 슬롯을 지워도 이 값은 그대로 유지.
// M4의 BossDecisionClient가 프록시 rate limit용 session_id로 이 값을 그대로 씀
// (기기 API 대신 우리가 직접 관리하는 값을 쓰기로 함 — 플랫폼 이식성/일관성 확보).
public static class PlayerInstallId
{
    private const string FILE_NAME = "install_id.txt";
    private static string _cached;

    public static string Get()
    {
        if (!string.IsNullOrEmpty(_cached)) return _cached;

        string path = Path.Combine(Application.persistentDataPath, "BossAI", FILE_NAME);

        try
        {
            if (File.Exists(path))
            {
                string existing = File.ReadAllText(path).Trim();
                if (!string.IsNullOrEmpty(existing))
                {
                    _cached = existing;
                    return _cached;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PlayerInstallId] 기존 install_id 읽기 실패, 새로 생성합니다: {e.Message}");
        }

        string newId = Guid.NewGuid().ToString();
        try
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, newId);
        }
        catch (Exception e)
        {
            // 저장 실패해도 이번 세션 동안은 메모리 캐시로 계속 쓸 수 있게 진행(게임을 막지 않음).
            Debug.LogWarning($"[PlayerInstallId] install_id 저장 실패: {e.Message}");
        }

        _cached = newId;
        return _cached;
    }
}
