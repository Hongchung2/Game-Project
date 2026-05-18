using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Stat _attackerStat;
    private bool _isPlayerBullet = false;

    public void Init(Stat stat, bool isPlayerBullet = false)
    {
        _attackerStat = stat;
        _isPlayerBullet = isPlayerBullet;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_isPlayerBullet)
        {
            if (collision.CompareTag("Monster"))
            {
                Hit(collision);
            }
        }
        else
        {
            if (collision.CompareTag("Player"))
            {
                Hit(collision);
            }
        }
    }

    void Hit(Collider2D collision)
    {
        Stat targetStat = collision.GetComponent<Stat>();
        if (targetStat != null && _attackerStat != null)
        {
            targetStat.OnAttacked(_attackerStat);
            Destroy(gameObject);
        }
    }
}
