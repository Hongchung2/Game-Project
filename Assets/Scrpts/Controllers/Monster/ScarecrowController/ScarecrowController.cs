using UnityEngine;
using System.Collections;

public class ScarecrowController : BaseMonsterController
{
    public override void OnHitEvent()
    {
        Debug.Log("onhitevent ���������� ����");
        if (_lockTarget == null) return;

        if (detection.playerDetected)
        {
            Stat targetStat = _lockTarget.GetComponent<Stat>();
            if (targetStat != null)
            {
                targetStat.OnAttacked(_stat);
                Debug.Log($"���� Hp : {targetStat.Hp}");
            }
        }
        else
        {
            Debug.Log("ȸ�� ����");
        }
    }

    protected override IEnumerator AttackRoutine()
    {
        _isAttacking = true;

        State = Define.State.Idle;
        _spum.PlayAnimation(PlayerState.IDLE, 0);

        yield return new WaitForSeconds(StopTime);

        if (_lockTarget != null)
        {
            State = Define.State.Skill;
            _spum.PlayAnimation(PlayerState.ATTACK, 0);
            Debug.Log("���� (�ڷ�ƾ)");
        }

        yield return new WaitForSeconds(StopTime);
        State = Define.State.Moving;
        _spum.PlayAnimation(PlayerState.MOVE, 0);
        
        _isAttacking = false;
        _attackCoroutine = null;
    }
}
