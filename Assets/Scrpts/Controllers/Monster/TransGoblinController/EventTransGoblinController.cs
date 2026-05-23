using System.Collections;
using UnityEngine;

public class EventTransGoblinController : BaseMonsterController
{
    [SerializeField] GameObject _transObject;
    [SerializeField] GameObject _originObject;

    public override void Init()
    {
        base.Init();
        _transObject.SetActive(true);
        _originObject.SetActive(false);
    }

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
        }
    }

    protected override void OnDie()
    {
        if (_isDeath) return;
        StartCoroutine(TransformAndDie());
    }

    IEnumerator TransformAndDie()
    {
        _isDeath = true;

        _transObject.SetActive(false);
        _originObject.SetActive(true);

        _spum.PlayAnimation(PlayerState.DAMAGED, 0);
        yield return new WaitForSeconds(0.5f);
        _spum.PlayAnimation(PlayerState.DEATH, 0);

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        yield return new WaitForSeconds(2.0f);

        // 죽을 시 발생하는 다양한 이벤트
    }
    protected override IEnumerator AttackRoutine()
    {
        _attackCoroutine = null;
        yield break;
    }
}

/*
1. GameObject에 뭘 넣어야 하나
유니티 에디터에서

EventTransGoblin (부모 오브젝트)
├── DisguiseObject  ← 빈 오브젝트 만들고
│     └── SpriteRenderer로 물건 이미지 넣기
└── GoblinVisual    ← SPUM 프리팹 넣기

그리고 Inspector에서
_disguiseObject = DisguiseObject 드래그
_goblinVisual = GoblinVisual 드래그
2. 상태 안바꿔도 되나
체력 1이라 맞는 순간 바로 Die 상태로 가니까
Idle → (피격) → Die
중간에 다른 상태 필요없어요 👍
3. 애니메이션
물건 모습 → 애니메이션 필요없음
            그냥 SpriteRenderer에 이미지만 넣으면 됨
            (가만히 있는 물건이니까)

고블린 모습 → SPUM 애니메이션 사용
              DAMAGED, DEATH 클립만 있으면 됨
              (맞고 죽는 것만 필요하니까)

*/