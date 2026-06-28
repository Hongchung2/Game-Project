using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyManagers enemyManager;

    public void Die()
    {
        enemyManager.OnEnemyDead();
        Destroy(gameObject);
    }
}
