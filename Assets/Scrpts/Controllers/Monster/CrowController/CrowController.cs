using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
public class CrowController : BaseController
{
    Stat _stat;
    Rigidbody2D _rb;
    Vector3 _spawnPos;
    Vector3 _initialScale;
    private SPUM_Prefabs _spum;
    private bool _isAttacking = false;
    private bool _isDeath = false;
    private Coroutine _attackCoroutine;
    float StopTime = 1.0f;


    [SerializeField]
    float _scanRange = 5f;

    [SerializeField]
    float _attackRange = 2.0f;

    [SerializeField]
    float _moveRange = 20f;

    [SerializeField]
    float bulletSpeed = 5f;

    [SerializeField]
    SpriteRenderer _spriteRenderer;

    [SerializeField]
    private GameObject bulletPrefab;
    public override void Init()
    {
        _initialScale = transform.localScale;

        WorldObjectType = Define.WorldObject.Monster;

        _stat = gameObject.GetComponent<Stat>();

        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        _rb = gameObject.GetOrAddComponent<Rigidbody2D>();
        _rb.gravityScale = 0;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        _spawnPos = transform.position;

        _spum = GetComponent<SPUM_Prefabs>();
        if (_spum != null)
        {
            _spum.PopulateAnimationLists();
            _spum.OverrideControllerInit();

            State = Define.State.Idle;
            _spum.PlayAnimation(PlayerState.IDLE, 0);
        }
    }

    protected override void UpdateIdle()
    {
        if (_isAttacking) return;

        if (_destPos == _spawnPos)
        {
            float disToHomeSqr = (transform.position - _spawnPos).sqrMagnitude;
            if (disToHomeSqr > 0.01f) return;
        }

        GameObject player = Managers.Game.GetPlayer();

        if (player == null)
        {
            return;
        }

        float distanceSqr = (player.transform.position - transform.position).sqrMagnitude;
        if (distanceSqr < _scanRange * _scanRange)
        {
            _lockTarget = player;

            State = Define.State.Moving;
            _spum.PlayAnimation(PlayerState.MOVE, 0);
            return;
        }
    }

    protected override void UpdateMoving()
    {
        if (_lockTarget == null)
        {
            State = Define.State.Idle;
            _spum.PlayAnimation(PlayerState.IDLE, 0);
            return;
        }

        if (_isAttacking) return;

        _destPos = _lockTarget.transform.position;

        float distance = Vector2.Distance(transform.position, _destPos);
        if (distance <= _attackRange)
        {
            _rb.linearVelocity = Vector2.zero;
            if (!_isAttacking && _attackCoroutine == null)
            {
                _attackCoroutine = StartCoroutine(AttackRoutine());
            }
            return;
        }

        Vector3 dir = (_destPos - transform.position).normalized;
        _rb.linearVelocity = dir * _stat.MoveSpeed;

        if (dir.x != 0)
        {
            float xTargetScale = (dir.x < 0) ? 1f : -1f;
            transform.localScale = new Vector3(xTargetScale * _initialScale.x, _initialScale.y, _initialScale.z);
        }

        float disFromHomeSqr = (transform.position - _spawnPos).sqrMagnitude;
        if (disFromHomeSqr > _moveRange * _moveRange)
        {
            _lockTarget = null;
            State = Define.State.Return;
        }
    }

    protected override void UpdateSkill()
    {
        if (_lockTarget == null)
        {
            State = Define.State.Idle;
            _spum.PlayAnimation(PlayerState.IDLE, 0);
            return;
        }

        Stat targetStat = _lockTarget.GetComponent<Stat>();
        if (targetStat != null && targetStat.Hp <= 0)
        {
            _lockTarget = null;
            State = Define.State.Return;
            return;
        }

        float distanceSqr = (_lockTarget.transform.position - transform.position).sqrMagnitude;
        if (distanceSqr > _attackRange * _attackRange)
        {
            State = Define.State.Moving;
            _spum.PlayAnimation(PlayerState.MOVE, 0);
            return;
        }

        Vector3 dir = (_lockTarget.transform.position - transform.position).normalized;
        if (dir.x != 0)
        {
            float xTargetScale = (dir.x < 0) ? 1f : -1f;
            transform.localScale = new Vector3(xTargetScale * _initialScale.x, _initialScale.y, _initialScale.z);
        }
    }

    protected override void UpdateDie()
    {
        if (_isDeath) return;
        StartCoroutine(DeadAction());
    }

    protected override void UpdateReturn()
    {
        Return();
    }

    public override void OnHitEvent()
    {

    }

    public void Return()
    {
        Vector3 dir = (_spawnPos - transform.position).normalized;
        float disToHomeSqr = (_spawnPos - transform.position).sqrMagnitude;

        if (disToHomeSqr < 0.01f)
        {
            _rb.linearVelocity = Vector2.zero;
            transform.position = _spawnPos;
            State = Define.State.Idle;
            _spum.PlayAnimation(PlayerState.IDLE, 0);
            _lockTarget = null;
            return;
        }

        _rb.linearVelocity = dir * _stat.MoveSpeed;
        if (dir.x != 0)
        {
            float xTargetScale = (dir.x < 0) ? 1f : -1f;
            transform.localScale = new Vector3(xTargetScale * _initialScale.x, _initialScale.y, _initialScale.z);
        }
        
    }

    public void Shoot()
    {
        if (_lockTarget == null) return;

        // 방향 계산 (목표 지점 - 내 지점)
        Vector2 dir = (_lockTarget.transform.position - transform.position).normalized;

        // 탄환 생성
        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);

        Bullet bulletComponent = bullet.GetOrAddComponent<Bullet>();
        bulletComponent.Init(_stat);
        // 탄환에 속도나 방향 전달
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        rb.linearVelocity = dir * bulletSpeed;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        float distance = Vector2.Distance(_lockTarget.transform.position, transform.position);
        float ArriveBulletTime = distance / bulletSpeed;
        Debug.Log($"불릿 좌표 {bullet.transform.position}");
        Destroy(bullet, 5.0f);
    }
    
    Vector2 GetDiagonalDirection()
    {
        if (_lockTarget == null) return Vector2.zero;

        // 플레이어에서 나를 향하는 방향 또는 나를 기준으로 계산
        Vector2 dirToPlayer = (_lockTarget.transform.position - transform.position).normalized;
        
        // 기본 각도 구하기 (라디안 -> 도(Degree)
        float baseAngle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;

        // 플레이어 기준 대각선
        float randomOffset = Random.Range(-17.5f, 17.5f);
        float finalAngle = (baseAngle + 180f) + randomOffset;

        // 각도를 벡터로 변환
        float rad = finalAngle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
    IEnumerator AttackRoutine()
    {
        _isAttacking = true;

        while (_lockTarget != null)
        {
            State = Define.State.Skill;
            _spum.PlayAnimation(PlayerState.ATTACK, 0);

            yield return new WaitForSeconds(StopTime);


            State = Define.State.Moving;
            _spum.PlayAnimation(PlayerState.MOVE, 0);

            Vector2 moveDir = GetDiagonalDirection();
            float moveTime = 0.8f;

            while (moveTime > 0)
            {
                _rb.linearVelocity = moveDir * _stat.Total_MoveSpeed;
                moveTime -= Time.deltaTime;
                yield return null;
            }

            _rb.linearVelocity = Vector2.zero;
            State = Define.State.Idle;
            _spum.PlayAnimation(PlayerState.IDLE, 0);
            yield return new WaitForSeconds(StopTime + 0.2f);

        }

        _isAttacking = false;
        yield return null;
    }

    IEnumerator DeadAction()
    {
        if (_lockTarget != null) _lockTarget = null;

        _isDeath = true;    

        if (_attackCoroutine != null)
        {
            StopCoroutine(_attackCoroutine);   
            _attackCoroutine = null;
        }

        _rb.linearVelocity = Vector2.zero;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        _spum.PlayAnimation(PlayerState.DEATH, 0);

        yield return new WaitForSeconds(3.0f);

        gameObject.SetActive(false);

        _isDeath = false;
    }

}
