using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : BaseController
{
    Stat _stat;
    Vector3 _moveDir;

    Rigidbody2D _rb;

    public float speed = 5f;
    Animator _anim;
    private SPUM_Prefabs _spum;

    //콩콩 뛰는 모션
    float _bounceTime = 0;
    public float bounceSpeed = 20f;
    public float bounceHeight = 0.2f;

    [SerializeField]
    SpriteRenderer _spriteRenderer;

    [SerializeField]
    Joystick _joystick;

    [SerializeField]
    float _attackRange = 0.8f;


    public override void Init()
    {

        WorldObjectType = Define.WorldObject.Player;
        _stat = gameObject.GetComponent<Stat>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        _spum = GetComponent<SPUM_Prefabs>();
        if (_spum != null)
        {
            _spum.PopulateAnimationLists();

            _spum.OverrideControllerInit();

            State = Define.State.Idle;
            _spum.PlayAnimation(PlayerState.IDLE, 0);
        }

        // _anim = GetComponent<Animator>();
    }
    protected override void UpdateIdle()
    {

        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnAttack();
        } 

        GetMoveInput();
        if (_moveDir.magnitude > 0)
        {
            State = Define.State.Moving;
            _spum.PlayAnimation(PlayerState.MOVE, 0);
        }
    }


    protected override void UpdateMoving()
    {

        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnAttack();
        }

        GetMoveInput();

        if (_moveDir.magnitude == 0)
        {
            State = Define.State.Idle;
            _spum.PlayAnimation(PlayerState.IDLE, 0);

            if (_spriteRenderer != null)
                _spriteRenderer.transform.localPosition = Vector3.zero;
            return;
        }

        transform.position += _moveDir * speed * Time.deltaTime;

        _bounceTime += Time.deltaTime * bounceSpeed;
        float yOffest = Mathf.Abs(Mathf.Sin(_bounceTime)) * bounceHeight;

        if (_spriteRenderer != null)
        {
            float currentX = _spriteRenderer.transform.localPosition.x;
            _spriteRenderer.transform.localPosition = new Vector3(currentX, yOffest, 0);

            if (_moveDir.x != 0)
            {
                float xTargetScale = (_moveDir.x < 0) ? 1f : -1f;
                _spriteRenderer.transform.parent.localScale = new Vector3(xTargetScale, 1f, 1f);
            }
        }
    }



    protected override void UpdateDie()
    {
        if (_stat.Hp <= 0)
        {
            State = Define.State.Die;
            _spum.PlayAnimation(PlayerState.DEATH, 0);
        }
    }

    void GetMoveInput()
    {
        float h = 0;
        float v = 0;

        // [수정] 수동으로 연결한 에셋 조이스틱 값 가져오기
        if (_joystick != null)
        {
            h = _joystick.Horizontal;
            v = _joystick.Vertical;
        }

        // 조이스틱 입력이 없으면 키보드 체크
        if (h == 0 && v == 0)
        {
            h = Input.GetAxisRaw("Horizontal");
            v = Input.GetAxisRaw("Vertical");
        }

        _moveDir = new Vector3(h, v, 0).normalized;
    }

    public void OnAttack()
    {
        if (State != Define.State.Skill)
        {
            LockTarget();

            State = Define.State.Skill;
            _spum.PlayAnimation(PlayerState.ATTACK, 0);
            StartCoroutine(CoReturnToIdle());
        }
    }

    void LockTarget()
    {
        Collider2D[] phtocells = Physics2D.OverlapCircleAll(transform.position, 2.0f);

        float closestDistance = Mathf.Infinity;
        GameObject closestMonster = null;

        foreach (var collider in  phtocells)
        {
            if (collider.CompareTag("Monster"))
            {
                float distance = (transform.position - collider.transform.position).sqrMagnitude;

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestMonster = collider.gameObject;
                }
            }
        }
        _lockTarget = closestMonster;
    }

    IEnumerator CoReturnToIdle()
    {
        yield return new WaitForSeconds(0.5f);
        if (State == Define.State.Skill)
        {
            State = Define.State.Idle;
            _spum.PlayAnimation(PlayerState.IDLE, 0);
        }
    }
    public override void OnHitEvent()
    {
        Debug.Log("player onhitevent 정상적으로 실행");
        if (_lockTarget == null) return;

        float dist = (transform.position - _lockTarget.transform.position).magnitude;
        if (dist > _attackRange)
        {
            Debug.Log("사거리 밖");
            return;
        }

        Stat targetStat = _lockTarget.GetComponent<Stat>();
        if (targetStat != null)
        {
            targetStat.OnAttacked(_stat);
            Debug.Log($"현재 Hp : {targetStat.Hp}");
        }
    }

}