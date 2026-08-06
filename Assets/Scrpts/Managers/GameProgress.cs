using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// 진행상황 저장 데이터. BossSaveData(보스AI 전용 플레이어 프로필/사망이력)와는 별개 파일 -
// 책임이 다름(이쪽은 스테이지 진행도, 그쪽은 재도전마다 갱신되는 전투 프로필).
[Serializable]
public class GameProgressData
{
    public string checkpointScene = "";
    public List<string> collectedScrolls = new List<string>();
    public bool westLakePurified;
    public bool eastLakePurified;
    public bool waterPuzzleSolved;
    public bool hasBronzeBell;
    public bool introDialogueShown;
    public bool springArrivalGuideShown;
    public bool springPuzzleGuideShown;
    public bool summerPuzzleGuideShown;
}

// 게임 전체 진행상황(현재 체크포인트 씬, 모은 족자, 호수 정화, 정화수 퍼즐, 청동방울 소지)을
// 저장/복원한다. 이 프로젝트에 기존 세이브 시스템이 없어서(BossDataPersistence가 유일한 선례)
// 같은 패턴(슬롯별 독립 JSON 파일, 조용한 실패 폴백)을 그대로 따름.
//
// 체크포인트는 지금은 "씬 단위"만 지원(SetCheckpoint를 각 구간 진행 트리거에서 호출).
// 씬 안에서의 세부 리스폰 지점(예: 방 중간의 안전지대)은 게임오버/체크포인트 작업에서
// 이 위에 쌓을 예정 - 절대 사망 처리 경로에서 SetCheckpoint를 호출하면 안 됨(진행을 덮어씀).
public static class GameProgress
{
    private const string SAVE_FOLDER = "GameProgress";
    public const int DEFAULT_SLOT = 0;

    // 이 프로젝트에 슬롯 선택 UI가 아직 없어서 기본 0 - BossDataPersistence.CurrentSlot과 같은 관례.
    public static int CurrentSlot = DEFAULT_SLOT;

    public static string CheckpointScene { get; private set; } = "";
    public static bool IntroDialogueShown { get; private set; } = false;
    // 봄방 도착 인사/사계절 방 소개, 봄방 퍼즐 힌트, 여름방 퍼즐 힌트 - 각각 딱 한 번만.
    public static bool SpringArrivalGuideShown { get; private set; } = false;
    public static bool SpringPuzzleGuideShown { get; private set; } = false;
    public static bool SummerPuzzleGuideShown { get; private set; } = false;

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

    public static bool HasSave(int slot = DEFAULT_SLOT)
    {
        return File.Exists(SavePath(slot));
    }

    // 진행 지점(방 클리어, 구간 이동 등)에 도달했을 때 호출 - 체크포인트 갱신 + 즉시 저장.
    public static void SetCheckpoint(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        CheckpointScene = sceneName;
        Save(CurrentSlot);
    }

    // 인트로에서 딱 한 번만 보여줄 돗가비 대사(IntroDokkaebiLine)를 다시 안 보여주려고 체크하는 용도.
    public static void MarkIntroDialogueShown()
    {
        IntroDialogueShown = true;
        Save(CurrentSlot);
    }

    public static void MarkSpringArrivalGuideShown()
    {
        SpringArrivalGuideShown = true;
        Save(CurrentSlot);
    }

    public static void MarkSpringPuzzleGuideShown()
    {
        SpringPuzzleGuideShown = true;
        Save(CurrentSlot);
    }

    public static void MarkSummerPuzzleGuideShown()
    {
        SummerPuzzleGuideShown = true;
        Save(CurrentSlot);
    }

    public static void Save(int slot = DEFAULT_SLOT)
    {
        try
        {
            var data = new GameProgressData
            {
                checkpointScene = CheckpointScene,
                collectedScrolls = ScrollCollection.Seasons.Where(ScrollCollection.IsCollected).ToList(),
                westLakePurified = LakePurifyState.WestPurified,
                eastLakePurified = LakePurifyState.EastPurified,
                waterPuzzleSolved = WaterPuzzleState.WaterSplit,
                hasBronzeBell = BronzeBellState.HasBell,
                introDialogueShown = IntroDialogueShown,
                springArrivalGuideShown = SpringArrivalGuideShown,
                springPuzzleGuideShown = SpringPuzzleGuideShown,
                summerPuzzleGuideShown = SummerPuzzleGuideShown,
            };

            string json = JsonUtility.ToJson(data, true);
            string path = SavePath(slot);
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, json);
        }
        catch (Exception e)
        {
            // 저장 실패가 게임 진행을 절대 멈추지 않게 한다.
            Debug.LogWarning($"[GameProgress] 저장 실패 (게임 진행에는 영향 없음): {e.Message}");
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
            var data = JsonUtility.FromJson<GameProgressData>(json);
            if (data == null)
            {
                Debug.LogWarning("[GameProgress] 세이브 파일이 손상되어 백지로 시작합니다.");
                ResetToBlank();
                return;
            }

            ApplyData(data);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GameProgress] 로드 실패, 백지로 시작합니다: {e.Message}");
            ResetToBlank();
        }
    }

    private static void ApplyData(GameProgressData data)
    {
        CheckpointScene = data.checkpointScene ?? "";

        ScrollCollection.ClearAll();
        if (data.collectedScrolls != null)
        {
            foreach (var season in data.collectedScrolls)
                ScrollCollection.Collect(season);
        }

        LakePurifyState.WestPurified = data.westLakePurified;
        LakePurifyState.EastPurified = data.eastLakePurified;
        WaterPuzzleState.WaterSplit = data.waterPuzzleSolved;
        BronzeBellState.HasBell = data.hasBronzeBell;
        IntroDialogueShown = data.introDialogueShown;
        SpringArrivalGuideShown = data.springArrivalGuideShown;
        SpringPuzzleGuideShown = data.springPuzzleGuideShown;
        SummerPuzzleGuideShown = data.summerPuzzleGuideShown;
    }

    public static void ResetToBlank()
    {
        CheckpointScene = "";
        ScrollCollection.ClearAll();
        LakePurifyState.WestPurified = false;
        LakePurifyState.EastPurified = false;
        WaterPuzzleState.WaterSplit = false;
        BronzeBellState.HasBell = false;
        IntroDialogueShown = false;
        SpringArrivalGuideShown = false;
        SpringPuzzleGuideShown = false;
        SummerPuzzleGuideShown = false;
    }

    // 새 게임 시작 시 호출 - 백지로 되돌리고 그 상태를 바로 슬롯에 덮어쓴다(이전 세이브와 안 섞이게).
    public static void NewGame(int slot = DEFAULT_SLOT)
    {
        ResetToBlank();
        Save(slot);
    }
}
