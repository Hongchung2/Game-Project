using System;
using System.IO;
using UnityEngine;

// 세이브 슬롯 단위 저장 데이터. player_profile은 명세서 10장 스키마와 맞춘 참고용 파생값이고,
// 실제 다음 세션 복원은 playerProfileRaw(원본 카운터)로 한다 — 파생값만 저장하면 스테이지별
// 세부 표본이 사라져서 로드 후 가중평균(4.4)이 어긋난다.
[Serializable]
public class BossSaveData
{
    public PlayerProfile playerProfile = new PlayerProfile();
    public PlayerProfileRawState playerProfileRaw = new PlayerProfileRawState();
    public DeathHistory deathHistory = new DeathHistory();
}

// 이 프로젝트엔 기존 세이브 시스템이 없어서(PlayerPrefs/SaveManager 등 미발견) 독립 JSON 파일로 구현.
// 슬롯 단위 파일: {persistentDataPath}/BossAI/slot_{n}.json — 계정 전체가 아니라 슬롯별로 백지 시작.
public static class BossDataPersistence
{
    private const string SAVE_FOLDER = "BossAI";
    public const int DEFAULT_SLOT = 0;

    // 이번 세션에서 쓸 슬롯 번호. 이 프로젝트에 슬롯 선택 UI가 아직 없어서 기본 0 —
    // 나중에 슬롯 선택 화면이 생기면 그쪽에서 이 값만 바꿔주면 된다(순수 추가 지점).
    public static int CurrentSlot = DEFAULT_SLOT;

    private static bool _quitHookRegistered = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterQuitHook()
    {
        if (_quitHookRegistered) return;
        _quitHookRegistered = true;
        Application.quitting += () => Save(CurrentSlot);
    }

    private static string SavePath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, SAVE_FOLDER, $"slot_{slot}.json");
    }

    public static void Save(int slot = DEFAULT_SLOT)
    {
        try
        {
            var data = new BossSaveData
            {
                playerProfile = PlayerProfileAggregator.GetCurrentProfile(),
                playerProfileRaw = PlayerProfileAggregator.ExportRawState(),
                deathHistory = DeathHistoryTracker.Current,
            };

            string json = JsonUtility.ToJson(data, true);
            string path = SavePath(slot);
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, json);
        }
        catch (Exception e)
        {
            // 저장 실패가 전투/씬 진행을 절대 멈추지 않게 한다.
            Debug.LogWarning($"[BossDataPersistence] 저장 실패 (게임 진행에는 영향 없음): {e.Message}");
        }
    }

    // 파일 손상/부재 시 예외 없이 백지로 폴백.
    public static void Load(int slot = DEFAULT_SLOT)
    {
        try
        {
            string path = SavePath(slot);
            if (!File.Exists(path))
            {
                ResetToBlank();
                return;
            }

            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<BossSaveData>(json);
            if (data == null)
            {
                Debug.LogWarning("[BossDataPersistence] 세이브 파일이 손상되어 백지로 시작합니다.");
                ResetToBlank();
                return;
            }

            PlayerProfileAggregator.ImportRawState(data.playerProfileRaw);
            DeathHistoryTracker.RestoreFrom(data.deathHistory);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[BossDataPersistence] 로드 실패, 백지로 시작합니다: {e.Message}");
            ResetToBlank();
        }
    }

    public static void ResetToBlank()
    {
        PlayerProfileAggregator.ResetAll();
        DeathHistoryTracker.ResetAll();
    }

    // 새 게임 시작 시 호출 — 백지로 되돌리고 그 상태를 바로 슬롯에 덮어쓴다(이전 슬롯 데이터와 섞이지 않게).
    public static void NewGame(int slot = DEFAULT_SLOT)
    {
        ResetToBlank();
        Save(slot);
    }
}
