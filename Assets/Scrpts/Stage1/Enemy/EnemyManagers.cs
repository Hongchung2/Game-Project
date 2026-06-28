using UnityEngine;

public class EnemyManagers : MonoBehaviour
{
    [SerializeField] private DoorController targetdoor;
    private int enemyCount;
    private void Start()
    {   
        enemyCount = transform.childCount;
    }

    public void OnEnemyDead()
    {
        enemyCount--;
        if (enemyCount <= 0)
        {
            targetdoor.UnlockDoor();
        }
    }
}
