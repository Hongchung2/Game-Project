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

    float _bounceTime = 0;
    public float bounceSpeed = 20f;
    public float bounceHeight = 0.2f;

    [SerializeField]
    SpriteRenderer _spriteRenderer;

    [SerializeField]
    Joystick _joystick;

    [SerializeField]
    float _attackRange = 0.8f;

    [SerializeField]
    float _interactRange = 0.5f;

    [SerializeField]
    IIdentifiable _interactTarget;

    


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

        if (_joystick != null)
        {
            h = _joystick.Horizontal;
            v = _joystick.Vertical;
        }

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
            MonsterLockTarget();

            State = Define.State.Skill;
            _spum.PlayAnimation(PlayerState.ATTACK, 0);
            StartCoroutine(CoReturnToIdle());
        }
    }

    void MonsterLockTarget()
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

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Interactable"))
        {
            IIdentifiable Interact = collision.GetComponent<IIdentifiable>();
            if (Interact != null)
            {
                _interactTarget = Interact;
            }
        }
    }

    void OnTriggerExit2D(Collider2D collsion)
    {
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
        if (_lockTarget == null) return;

        float dist = (transform.position - _lockTarget.transform.position).magnitude;
        if (dist > _attackRange)
        {
            Debug.Log("��Ÿ� ��");
            return;
        }

        Stat targetStat = _lockTarget.GetComponent<Stat>();
        if (targetStat != null)
        {
            targetStat.OnAttacked(_stat);
            Debug.Log($"���� Hp : {targetStat.Hp}");
        }
    }

}