using Unity.VisualScripting;
using UnityEngine;
using System.Collections;
public class CrowController : BaseMonsterController
{
    [SerializeField]
    float bulletSpeed = 5f;

    [SerializeField]
    private GameObject bulletPrefab;

    public void Shoot()
    {
        if (_lockTarget == null || !detection.playerDetected) return;

        Vector2 dir = (_lockTarget.transform.position - transform.position).normalized;

        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);

        Bullet bulletComponent = bullet.GetOrAddComponent<Bullet>();
        bulletComponent.Init(_stat);

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        rb.linearVelocity = dir * bulletSpeed;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        Destroy(bullet, 5.0f);
    }
    
    Vector2 GetDiagonalDirection()
    {
        if (_lockTarget == null) return Vector2.zero;

        Vector2 dirToPlayer = (_lockTarget.transform.position - transform.position).normalized;
        
        float baseAngle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;

        float randomOffset = Random.Range(-17.5f, 17.5f);
        float finalAngle = (baseAngle + 180f) + randomOffset;

        float rad = finalAngle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
    protected override IEnumerator AttackRoutine()
    {
        _isAttacking = true;

        while (_lockTarget != null)
        {
            Stat targetStat = _lockTarget.GetComponent<Stat>();
            if (targetStat != null && targetStat.Hp <= 0)
            {
                _lockTarget = null;
                break;
            }

            State = Define.State.Skill;
            _spum.PlayAnimation(PlayerState.ATTACK, 0);
            yield return new WaitForSeconds(0.3f);
            Shoot();
            yield return new WaitForSeconds(StopTime - 0.3f);

            State = Define.State.Moving;
            _spum.PlayAnimation(PlayerState.MOVE, 0);

            Vector2 moveDir = GetDiagonalDirection();
            float moveTime = 0.8f;

            while (moveTime > 0)
            {
                float disFromHome = (transform.position - _spawnPos).sqrMagnitude;
                if (disFromHome > _moveRange * _moveRange)
                {
                    _rb.linearVelocity = Vector2.zero;
                    _isAttacking = false;
                    _attackCoroutine = null;
                    State = Define.State.Return;
                    yield break;
                }

                _rb.linearVelocity = moveDir * _stat.Total_MoveSpeed;

                Vector3 dir = (_lockTarget.transform.position - transform.position).normalized;
                if (dir.x != 0)
                {
                    float xTargetScale = (dir.x < 0) ? 1f : -1f;
                    transform.localScale = new Vector3(xTargetScale * _initialScale.x, _initialScale.y, _initialScale.z);
                }

                moveTime -= Time.deltaTime;
                yield return null;
            }

            _rb.linearVelocity = Vector2.zero;
            State = Define.State.Idle;
            _spum.PlayAnimation(PlayerState.IDLE, 0);
            yield return new WaitForSeconds(StopTime + 0.2f);

        }

        _isAttacking = false;
        _attackCoroutine = null;
        yield return null;
    }


}
