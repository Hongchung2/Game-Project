using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public DoorController door;
    private int enemyCount;

    void Start()
    {
        enemyCount = transform.childCount;
        door.SetDoorLocked(true);
    }

    public void OnEnemyDied()
    {
        // 몬스터 죽을 때 마다 카운트 -1
        enemyCount--;
        if (enemyCount <= 0)
        {
            door.SetDoorLocked(false);
        }
    }
}
