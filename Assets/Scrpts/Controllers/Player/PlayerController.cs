using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerController : BaseController
{
    Stat _stat;
    public Vector3 _moveDir;

    Rigidbody2D _rb;

    public float speed = 5f;
    private Animator _animator;
    public float bounceSpeed = 20f;
    public float bounceHeight = 0.2f;
    bool _isDying = false;
    private bool _isReversed = false;
    private Coroutine _reverseCoroutine;
    private Vector3 _originalScale;
    [SerializeField] IWeapon _currentWeapon;
    [SerializeField] TextMeshProUGUI _swapButtonText;

    //나중에 작업 할 예정 (아트분 그림 나오면)
    /*[SerializeField] Image _swapButtonImage; 
    [SerializeField] Sprite _swordSprite;
    [SerializeField] Sprite _bowSprite;*/

    [SerializeField] SpriteRenderer _spriteRenderer;

    [SerializeField] Joystick _joystick;

    [SerializeField] float _attackRange = 0.8f;

    [SerializeField] float _interactRange = 0.5f;
    [SerializeField] float _attackCooltime = 0.3f;
    [SerializeField] private float _lastAttackTime = 0f;

    [SerializeField] IIdentifiable _interactTarget;

    public override void Init()
    {
        WorldObjectType = Define.WorldObject.Player;
        _stat = gameObject.GetComponent<Stat>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        _animator = GetComponent<Animator>();
        State = Define.State.Idle;

        _rb = GetComponent<Rigidbody2D>();
        
        _currentWeapon = GetComponent<SwordWeapon>();
        //_swapButtonText.text = "활";
        //_swapButtonImage.sprite = _bowSprite;

        _originalScale = transform.localScale;
    }
    public void EquipSword()
    {
        _currentWeapon = GetComponent<SwordWeapon>();
    }

    public void EquipBow()
    {
        _currentWeapon = GetComponent<BowWeapon>();
    }

    protected override void UpdateIdle()
    {
        MonsterLockTarget();
        if (_lockTarget != null)
        {
            float distance = Vector2.Distance(transform.position, _lockTarget.transform.position);
            if (distance > _attackRange)
            {
                _lockTarget = null;
            }
        }

        // 사망 처리
        if (_stat.Hp <= 0)
        {
            State = Define.State.Die;
            _animator.SetTrigger("Death");

            return;
        }

        // 상호작용 및 공격
        if (_interactTarget != null && Input.GetKeyDown(KeyCode.Space))
        {
            //_interactTarget.Interact();
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            OnAttack();
        }
        
        // 이동 좌표 계산
        GetMoveInput();

        // 실제 이동
        if (_moveDir.magnitude > 0)
        {
            State = Define.State.Moving;
            _animator.SetBool("IsMoving", true);
        }
    }


    protected override void UpdateMoving()
    {
        MonsterLockTarget();
        if (_lockTarget != null)
        {
            float distance = Vector2.Distance(transform.position, _lockTarget.transform.position);
            if (distance > _attackRange)
            {
                _lockTarget = null;
            }
        }

        // 사망 처리
        if (_stat.Hp <= 0)
        {
            State = Define.State.Die;
            _animator.SetTrigger("Death");
            return;
        }


        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnAttack();
        }

        GetMoveInput();

        if (_moveDir.magnitude == 0)
        {
            State = Define.State.Idle;
            _animator.SetBool("IsMoving", false);
            
            return;
        }

        _rb.MovePosition(_rb.position + (Vector2)_moveDir * _stat.MoveSpeed * Time.deltaTime);

       if (_spriteRenderer != null)
        {
            if (_moveDir.x != 0)
            {
                float xTargetScale = (_moveDir.x < 0) ? -1f : 1f;
                _spriteRenderer.transform.localScale = new Vector3(
                    xTargetScale * Mathf.Abs(_originalScale.x),
                    _originalScale.y,
                    _originalScale.z
                );
            }
        }
    }



    protected override void OnDie()
    {
        if (_isDying) return;
        StartCoroutine(DeadAction());
    }

    // 이동 좌표 계산
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

        if (_isReversed)
        {
            h = -h;
            v = -v;
        }
        _moveDir = new Vector3(h, v, 0).normalized;
    }

    public void OnAttack()
    {
        if (Time.time - _lastAttackTime < _attackCooltime) return; // 쿨타임 체크
        _lastAttackTime = Time.time;
        
        if (State != Define.State.Skill && _currentWeapon != null)
        {
            MonsterLockTarget();

            State = Define.State.Skill;
            _animator.SetTrigger("Attack");

            // 직접 공격
            _currentWeapon.Attack(_lockTarget, _stat);
            // 칼 발향 설정
            SwordController sword = GetComponentInChildren<SwordController>(true);
            if (sword != null)
            {
                if (_lockTarget != null)
                {
                    sword.LookAtTarget(_lockTarget.transform.position);

                    DisguisedGoblin disguisedGoblin = _lockTarget.GetComponent<DisguisedGoblin>();
                    if (disguisedGoblin != null)
                    {
                        disguisedGoblin.OnHitEvent();
                    }

                    HiddenObjectController hiddenObject = _lockTarget.GetComponent<HiddenObjectController>();
                    if (hiddenObject != null)
                    {
                        hiddenObject.OnHitEvent();
                    }
                }
                else
                {
                    sword.LookAtMoveDir(_moveDir);
                }

                StartCoroutine(sword.Swing());
            }
            if (_currentWeapon is BowWeapon)
            {
                _currentWeapon.Attack(_lockTarget, _stat);
            }
            StartCoroutine(CoReturnToIdle());
        }
    }

    void MonsterLockTarget()
    {
        Collider2D[] phtocells = Physics2D.OverlapCircleAll(transform.position, _attackRange);
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

    // 플레이어가 "Interactable" 태그 오브젝트 범위 안에 들어왔을 때
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

        if (collision.CompareTag("Door"))
        {
            //collision.GetComponent<DoorController>()?.TriggerDoor();
        }

        if (collision.CompareTag("DokkaebiTile"))
        {
            if (_reverseCoroutine != null)
            {
                StopCoroutine(_reverseCoroutine);
            }
            _reverseCoroutine = StartCoroutine(SetReverse(true));
        }
    }

    // 조작 반대 코루틴
    IEnumerator SetReverse(bool reverse)
    {
        yield return new WaitForSeconds(1f);
        _isReversed = reverse;
    }

    // 플레이어가 범위 밖으로 나갔을 때
    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Interactable"))
        {
            _interactTarget = null;
        }

        if (collision.CompareTag("DokkaebiTile"))
        {
                if (_reverseCoroutine != null)
                {
                    StopCoroutine(_reverseCoroutine);
                }
                _reverseCoroutine = StartCoroutine(SetReverse(false));
        }
    }

    // 공격 후 잠시 경직
    IEnumerator CoReturnToIdle()
    {
        yield return new WaitForSeconds(0f);
        if (State == Define.State.Skill)
        {
            if (_moveDir.magnitude > 0)
            {
                State = Define.State.Moving;
                _animator.SetBool("IsMoving", true);
            }
            else
            {
                State = Define.State.Idle;
                _animator.SetBool("IsMoving", false);
            }
        }
    }
    public override void OnHitEvent()
    {
        if (_currentWeapon is SwordWeapon && _lockTarget != null)
        {
            _currentWeapon.Attack(_lockTarget, _stat);
        }
    }

    public void SwapWeapon()
    {
        if (_currentWeapon is SwordWeapon)
        {
            EquipBow();
            _swapButtonText.text = "검";
            //_swapButtonImage.sprite = _swordSprite;
        }
        else
        {
            EquipSword();
             _swapButtonText.text = "활";
             //_swapButtonImage.sprite = _bowSprite;
        }
    }


    IEnumerator DeadAction()
    {
        if (_lockTarget != null) _lockTarget = null;

        _isDying = true;

        _rb.linearVelocity = Vector2.zero;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        _animator.SetTrigger("Death");

        yield return new WaitForSeconds(3.0f);

        _isDying = false;

        // 보스방에서 죽은 거면 사망 기록을 남김 - 재도전 시 클로드 API에 death_history로 반영됨
        // (지금까지는 F12 디버그 도구로만 채워지고 실제 플레이 사망은 한 번도 기록된 적이 없었음).
        if (SceneManager.GetActiveScene().name == "Stage5_BossScene")
        {
            DeathHistoryTracker.RecordDeath(_stat.LastAttackerName, transform.position, "none");
        }

        GameOverUI.CreateInstance().Show();
        gameObject.SetActive(false);
    }
}