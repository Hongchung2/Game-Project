using System.Collections;

public class InvTransGoblinController : BaseMonsterController
{
    // 무적 도깨비

    protected override void InitHPBar()
    {
        
    }
    
    protected override void UpdateIdle()
    {
        _lockTarget = null;
    }

    protected override void UpdateMoving()
    {
        if (State != Define.State.Idle)
        {
            State = Define.State.Idle;
            _spum.PlayAnimation(PlayerState.IDLE, 0);
        }
    }

    protected override IEnumerator AttackRoutine()
    {
        _attackCoroutine = null;
        yield break;
    }
}
