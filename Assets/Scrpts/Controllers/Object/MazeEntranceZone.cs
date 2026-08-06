using UnityEngine;

// 먹그림자 미로 입구 트리거. 플레이어가 들어오면 FallMazeManager가 그리드 이동 모드로 전환한다.
public class MazeEntranceZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // 플레이어 프리팹 안의 "WallCollider" 자식도 태그가 Player라 이 트리거에 걸릴 수 있음 -
        // Rigidbody2D는 본체(루트)에만 있으므로 attachedRigidbody로 항상 진짜 루트를 찾는다.
        GameObject root = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        FallMazeManager.Instance.EnterMaze(root);
    }
}
