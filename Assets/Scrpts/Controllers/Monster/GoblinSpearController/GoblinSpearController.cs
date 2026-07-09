using UnityEngine;
using System.Collections;

public class GoblinSpearController : BaseMonsterController
{
    float AttackCount = 0f;
    private float lastdistance;
    private bool _isWaiting = false;

    public override void OnHitEvent()
    {
        if (_lockTarget == null) return;

        if (detection.playerDetected)
        {
            Stat targetStat = _lockTarget.GetComponent<Stat>();
            if (targetStat != null)
            {
                targetStat.OnAttacked(_stat);
            }
        }
        else
        {
            
        }
    }

    protected override IEnumerator AttackRoutine()
    {
        _isAttacking = true;
        _isWaiting = true;
        AttackCount = 0f;

        lastdistance = Vector2.Distance(transform.position, _lockTarget.transform.position);
        while (AttackCount < 1.0f && detection.playerDetected) 
        {
            FollowPlayerSlowly();
            AttackCount += Time.deltaTime;
            lastdistance = Vector2.Distance(transform.position, _lockTarget.transform.position);
            yield return null;
        }

        if (AttackCount < 1.0f)
        {
            _stat.add_MoveSpeed = 0;
            _isAttacking = false;
            _isWaiting = false;
            _attackCoroutine = null;
            yield break;
        }

        State = Define.State.Skill;
        _spum.PlayAnimation(PlayerState.ATTACK, 0);
        AttackCount = 0f;

        yield return new WaitForSeconds(2.0f);
        _stat.add_MoveSpeed = 0;
        State = Define.State.Moving;
        _spum.PlayAnimation(PlayerState.MOVE, 0);
        _isAttacking = false;
        _isWaiting = false;
        _attackCoroutine = null;
    }
    void FollowPlayerSlowly()
    {
        if (_lockTarget == null) return;

        _stat.add_MoveSpeed = -1.0f;

        float currentdistance = Vector2.Distance(transform.position, _lockTarget.transform.position);
        Vector3 dirToPlayer = (_lockTarget.transform.position - transform.position).normalized;
        
        if (detection.playerDetected)
        {
            _spum.PlayAnimation(PlayerState.MOVE, 0);

            float xTargetScale = (_lockTarget.transform.position.x < transform.position.x) ? 1f : -1f;
            transform.localScale = new Vector3(xTargetScale * _initialScale.x, _initialScale.y, _initialScale.z);
           
            if (currentdistance > lastdistance + 0.05f)
            {
                _rb.linearVelocity = dirToPlayer * _stat.Total_MoveSpeed;
            }

           else if (currentdistance <= lastdistance - 0.05f)
            {
                _rb.linearVelocity = -dirToPlayer * _stat.Total_MoveSpeed;
            }
            
            else
            {
                _rb.linearVelocity = Vector2.zero;
            }
        }
        else
        {
             _rb.linearVelocity = Vector2.zero;
        }
    }
}

