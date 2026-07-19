using UnityEngine;
using System.Collections;

public class ScarecrowController : BaseMonsterController
{
    public override void OnHitEvent()
    {   
        if (_lockTarget == null) return;

        
            Stat targetStat = _lockTarget.GetComponent<Stat>();
            float distToPlayer = (_lockTarget.transform.position - transform.position).sqrMagnitude;
            if (targetStat != null && distToPlayer < _attackRange * _attackRange)
            {
                targetStat.OnAttacked(_stat);
            }
        
    }

    protected override IEnumerator AttackRoutine()
    {
        _isAttacking = true;

        State = Define.State.Idle;
        _animator.SetBool("IsMoving", false);

        yield return new WaitForSeconds(StopTime);

        if (_lockTarget != null)
        {
            State = Define.State.Skill;
            _animator.SetTrigger("Attack");
        }

        yield return new WaitForSeconds(StopTime);
        State = Define.State.Moving;
        _animator.SetBool("IsMoving", true);
        
        _isAttacking = false;
        _attackCoroutine = null;
    }
}
