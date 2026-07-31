using UnityEngine;

// 먹그림자 미로 입구 트리거. 플레이어가 들어오면 FallMazeManager가 그리드 이동 모드로 전환한다.
public class MazeEntranceZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            FallMazeManager.Instance.EnterMaze(other.gameObject);
    }
}
