using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Stat _attackerStat;

    public void Init(Stat stat)
    {
        _attackerStat = stat;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. 플레이어인지 확인 (태그나 컴포너늩로)
        if (collision.CompareTag("Player"))
        {
            Stat targetStat = collision.GetComponent<Stat>();
            if ( targetStat != null && _attackerStat != null)
            {
                targetStat.OnAttacked(_attackerStat);
                Destroy(gameObject);
            }
        }
    }
}
