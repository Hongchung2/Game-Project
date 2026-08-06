using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 가을방 "먹그림자 미로" 총괄 매니저.
// 미로 안에서는 플레이어의 자유이동(PlayerController)을 잠시 끄고 칸 단위 이동으로 전환해
// 다음 규칙을 실시간으로 강제한다:
// U턴/역주행 금지, 재방문 금지, 갈림길에서 직진 금지(반드시 회전), 정확히 7번 방향전환,
// 3번째 전환은 좌회전, 6번째 전환은 우회전.
// 미로 통로는 Wall 타일맵 콜라이더로 자동 판정하므로 미로 모양은 코드에 없다 - 타일로 그리면 그대로 작동.
// 플레이어 오브젝트/스크립트는 수정하지 않고 이 매니저가 외부에서 잠시 조종한다.
public class FallMazeManager : MonoBehaviour
{
    public static FallMazeManager Instance;

    [Header("좌표")]
    public Transform mazeEntrance;          // 미로 입구 - 리셋/시작 위치
    public Vector2 monsterRoomEntry;        // 몬스터방 플레이어 시작 위치 (같은 씬인 경우)
    public string monsterRoomSceneName = ""; // 몬스터방이 별도 씬이면 이름 지정, 같은 씬이면 비워둠

    [Header("격자 설정")]
    public float cellSize = 1f;
    public LayerMask wallLayer;   // Wall 타일맵 콜라이더가 속한 레이어
    public float moveTime = 0.12f;
    public float fadeDuration = 0.5f;

    [Header("규칙 - 임시 조정용 (기획서 기준 7)")]
    public int maxTurns = 8; // TODO: 미로 그림 수정 후 7로 되돌리기

    private Transform _player;
    private PlayerController _playerController;
    private Rigidbody2D _playerRb;

    private bool _active = false;
    private bool _stepping = false;

    // 시계방향 순서: 위→오른쪽→아래→왼쪽. 인덱스 +1 = 우회전, -1(=+3) = 좌회전, +2 = U턴.
    private static readonly Vector2Int[] Clockwise =
    {
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0),
    };

    private int _dirIndex = -1;   // 현재 진행 방향, -1 = 첫 걸음 전
    private int _turnCount = 0;
    private readonly HashSet<Vector2Int> _visited = new HashSet<Vector2Int>();
    private Vector2Int _currentCell;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!_active || _stepping) return;

        Vector2Int input = ReadInput();
        if (input == Vector2Int.zero) return;

        if (ValidateMove(input, out bool isTurn, out int newDirIndex))
            StartCoroutine(StepTo(input, isTurn, newDirIndex));
    }

    private Vector2Int ReadInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) return new Vector2Int(0, 1);
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) return new Vector2Int(0, -1);
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) return new Vector2Int(-1, 0);
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) return new Vector2Int(1, 0);
        return Vector2Int.zero;
    }

    // 미로 입구 트리거가 호출: 플레이어를 그리드 이동 모드로 전환
    public void EnterMaze(GameObject player)
    {
        if (_active) return;

        _player = player.transform;
        _playerController = player.GetComponent<PlayerController>();
        _playerRb = player.GetComponent<Rigidbody2D>();

        _active = true;
        if (_playerController != null) _playerController.enabled = false;
        if (_playerRb != null) _playerRb.linearVelocity = Vector2.zero;

        ResetState(mazeEntrance.position);
    }

    private void ResetState(Vector2 pos)
    {
        _player.position = pos;
        _currentCell = WorldToCell(pos);
        _visited.Clear();
        _visited.Add(_currentCell);
        _dirIndex = -1;
        _turnCount = 0;
    }

    // 타일 중심이 정수가 아니라 (칸 + 0.5) 지점에 있으므로(예: -9.5, 9.5) 0.5 보정을 거쳐 변환한다.
    private Vector2Int WorldToCell(Vector2 worldPos) =>
        new Vector2Int(Mathf.RoundToInt(worldPos.x / cellSize - 0.5f), Mathf.RoundToInt(worldPos.y / cellSize - 0.5f));

    private Vector2 CellToWorld(Vector2Int cell) =>
        new Vector2((cell.x + 0.5f) * cellSize, (cell.y + 0.5f) * cellSize);

    private bool IsWall(Vector2Int cell) =>
        Physics2D.OverlapBox(CellToWorld(cell), Vector2.one * (cellSize * 0.8f), 0f, wallLayer);

    private int IndexOf(Vector2Int dir)
    {
        for (int i = 0; i < 4; i++)
            if (Clockwise[i] == dir) return i;
        return -1;
    }

    // 현재 칸에서, 특정 방향(주로 온 방향)을 제외하고 열려있는(벽 아니고 미방문) 선택지 개수.
    // 2개 이상이면 "진짜 갈림길" - 직진 금지 규칙이 여기서 적용됨.
    private int CountOpenChoices(Vector2Int cell, int excludeDirIndex)
    {
        int count = 0;
        for (int i = 0; i < 4; i++)
        {
            if (i == excludeDirIndex) continue;
            Vector2Int neighbor = cell + Clockwise[i];
            if (!IsWall(neighbor) && !_visited.Contains(neighbor))
                count++;
        }
        return count;
    }

    // 상태를 바꾸지 않고 "이 방향으로 이동 가능한가"만 검증 (입력 처리 + 막힘 판정 양쪽에서 재사용)
    private bool ValidateMove(Vector2Int inputDir, out bool isTurn, out int newDirIndex)
    {
        isTurn = false;
        newDirIndex = _dirIndex;

        int inputIndex = IndexOf(inputDir);
        Vector2Int targetCell = _currentCell + inputDir;

        if (IsWall(targetCell)) return false;
        if (_visited.Contains(targetCell)) return false; // 재방문 금지 = U턴/역주행도 결과적으로 차단

        if (_dirIndex == -1)
        {
            // 첫 걸음은 규칙 적용 전이라 제한 없음
            newDirIndex = inputIndex;
            return true;
        }

        if (inputIndex == _dirIndex)
        {
            // 직진 시도: 진짜 갈림길이면 금지, 외길(강제 경로)이면 허용
            int behindIndex = (_dirIndex + 2) % 4;
            int openChoices = CountOpenChoices(_currentCell, behindIndex);
            return openChoices < 2;
        }

        int diff = ((inputIndex - _dirIndex) % 4 + 4) % 4;
        if (diff == 2) return false; // U턴 금지

        int nextTurnNumber = _turnCount + 1;
        if (nextTurnNumber > maxTurns) return false; // 정확히 maxTurns번까지만 전환 가능

        bool isRightTurn = diff == 1; // 시계방향 인덱스 +1 = 우회전
        if (nextTurnNumber == 3 && isRightTurn) return false;  // 3번째 전환은 반드시 좌회전
        if (nextTurnNumber == 6 && !isRightTurn) return false; // 6번째 전환은 반드시 우회전

        isTurn = true;
        newDirIndex = inputIndex;
        return true;
    }

    private bool HasAnyLegalMove()
    {
        foreach (var dir in Clockwise)
        {
            if (ValidateMove(dir, out _, out _)) return true;
        }
        return false;
    }

    private IEnumerator StepTo(Vector2Int inputDir, bool isTurn, int newDirIndex)
    {
        _stepping = true;

        Vector2 start = _player.position;
        Vector2Int targetCell = _currentCell + inputDir;
        Vector2 end = CellToWorld(targetCell);

        float elapsed = 0f;
        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            _playerRb.MovePosition(Vector2.Lerp(start, end, elapsed / moveTime));
            yield return null;
        }
        _playerRb.MovePosition(end);

        _currentCell = targetCell;
        _visited.Add(_currentCell);
        if (isTurn) _turnCount++;
        _dirIndex = newDirIndex;

        _stepping = false;

        if (!HasAnyLegalMove())
        {
            Debug.Log("[먹그림자 미로] 더 이상 갈 곳이 없습니다 (규칙 위반 없이는 진행 불가). 입구로 리셋.");
            if (CenterMessageUI.Instance != null)
                CenterMessageUI.Instance.Show("더 이상 갈 곳이 없다... 다시 처음부터.", 2f);
            FailAndReset();
        }
    }

    // 오답 출구 도달 시 (MazeExitZone이 호출)
    public void OnWrongExit(string exitId)
    {
        Debug.Log($"[먹그림자 미로] 오답 출구 '{exitId}'. 입구로 리셋.");
        if (CenterMessageUI.Instance != null)
            CenterMessageUI.Instance.Show("틀린 출구다... 다시 처음부터.", 2f);
        FailAndReset();
    }

    // 정답 출구('바') 도달 시
    public void OnCorrectExit()
    {
        Debug.Log("[먹그림자 미로] 정답 출구 도달! 몬스터방으로 이동.");
        if (CenterMessageUI.Instance != null)
            CenterMessageUI.Instance.Show("정답이다!", 1.5f);
        StartCoroutine(SuccessSequence());
    }

    private void FailAndReset()
    {
        StartCoroutine(FailSequence());
    }

    private IEnumerator FailSequence()
    {
        _stepping = true; // 페이드 중 입력 차단

        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeOut(Color.black, fadeDuration);

        ResetState(mazeEntrance.position);

        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeIn(fadeDuration);

        _stepping = false;
    }

    private IEnumerator SuccessSequence()
    {
        _active = false; // 그리드 모드 종료

        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeOut(Color.black, fadeDuration);

        if (!string.IsNullOrEmpty(monsterRoomSceneName))
        {
            GameProgress.SetCheckpoint(monsterRoomSceneName);
            SceneManager.LoadScene(monsterRoomSceneName);
            yield break;
        }

        // 같은 씬 안에 몬스터방이 있는 경우: 위치만 이동하고 자유이동 복구
        _player.position = monsterRoomEntry;
        if (_playerController != null) _playerController.enabled = true;

        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeIn(fadeDuration);
    }
}
