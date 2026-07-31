using UnityEngine;

// 먹그림자 미로의 출구 하나 (가/나/다/라/마/바/사 중 하나에 붙임).
// '바'만 정답 출구, 나머지는 오답 처리.
public class MazeExitZone : MonoBehaviour
{
    public string exitId = "가";
    public bool isCorrectExit = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (isCorrectExit)
            FallMazeManager.Instance.OnCorrectExit();
        else
            FallMazeManager.Instance.OnWrongExit(exitId);
    }
}
