using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : BaseController
{
    Stat _stat;
    Vector3 _moveDir;

    Rigidbody2D _rb;

    public float speed = 5f;
    private Animator _animator;
    float _bounceTime = 0;
    public float bounceSpeed = 20f;
    public float bounceHeight = 0.2f;
    bool _isDying = false;
    private bool _isReversed = false;
    private int _dokkaebiTileCount = 0;
    private Coroutine _reverseCoroutine;
    [SerializeField] IWeapon _currentWeapon;
    [SerializeField] Button _swapButton;
    [SerializeField] TextMeshProUGUI _swapButtonText;

    //나중에 작업 할 예정 (아트분 그림 나오면)
    /*[SerializeField] Image _swapButtonImage; 
    [SerializeField] Sprite _swordSprite;
    [SerializeField] Sprite _bowSprite;*/

    [SerializeField] SpriteRenderer _spriteRenderer;

    [SerializeField] Joystick _joystick;

    [SerializeField] float _attackRange = 0.8f;

    [SerializeField] float _interactRange = 0.5f;

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
        _swapButtonText.text = "활";
        //_swapButtonImage.sprite = _bowSprite;
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
        // 공격 중 이동 막기
        if (State == Define.State.Skill) return;
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

        _rb.MovePosition(_rb.position + (Vector2)_moveDir * speed * Time.deltaTime);
        Debug.Log("MovePosition: " + _rb.position + " moveDir: " + _moveDir);
        _bounceTime += Time.deltaTime * bounceSpeed;
        float yOffest = Mathf.Abs(Mathf.Sin(_bounceTime)) * bounceHeight;

        if (_spriteRenderer != null)
        {
            float currentX = _spriteRenderer.transform.localPosition.x;
            _spriteRenderer.transform.localPosition = new Vector3(currentX, yOffest, 0);

            if (_moveDir.x != 0)
            {
                float xTargetScale = (_moveDir.x < 0) ? -1f : 1f;
                _spriteRenderer.transform.localScale = new Vector3(xTargetScale, 1f, 1f);
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
        if (State != Define.State.Skill && _currentWeapon != null)
        {
            MonsterLockTarget();

            State = Define.State.Skill;
            _animator.SetTrigger("Attack");

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
        Debug.Log(reverse);
        yield return new WaitForSeconds(1f);
        _isReversed = reverse;
        Debug.Log(reverse);
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
        yield return new WaitForSeconds(0.5f);
        if (State == Define.State.Skill)
        {
            State = Define.State.Idle;
            _animator.SetBool("IsMoving", false);
        }
    }
    public override void OnHitEvent()
    {
        if (_currentWeapon is SwordWeapon && _lockTarget != null)
        {
            _currentWeapon.Attack(_lockTarget, _stat);

            // 물건이면 OnHitEvent 호출
            HiddenObjectController hiddenObject = _lockTarget.GetComponent<HiddenObjectController>();
            if (hiddenObject != null)
            {
                hiddenObject.OnHitEvent();
            }

            DisguisedGoblin disguisedGoblin = _lockTarget.GetComponent<DisguisedGoblin>();
            if (disguisedGoblin != null)
            {
                disguisedGoblin.OnHitEvent();
            }
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
        Managers.Load.StartFadeAndLoad("GameScene", "YOU DIED");
        gameObject.SetActive(false);
    }

    // 조작 반전
    



}