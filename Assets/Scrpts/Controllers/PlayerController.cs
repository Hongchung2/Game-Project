using UnityEngine;

public class PlayerController : BaseController
{
    PlayerStat _stat;
    Vector3 _moveDir;
    
    public float speed = 5f;
    Animator _anim;

    //콩콩 뛰는 모션
    float _bounceTime = 0;
    public float bounceSpeed = 20f;
    public float bounceHeight = 0.2f;

    [SerializeField]
    SpriteRenderer _spriteRenderer;
    [SerializeField]
    Joystick _joystick;


    public override void Init()
    {
        WorldObjectType = Define.WorldObject.Player;
        _stat = gameObject.GetComponent<PlayerStat>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        State = Define.State.Idle;
       
        // _anim = GetComponent<Animator>();
    }

    protected override void UpdateIdle()
    {
        GetMoveInput();
        if (_moveDir.magnitude > 0)
        {

            State = Define.State.Moving;
        }
    }
    

    protected override void UpdateMoving()
    {
        GetMoveInput();

        if (_moveDir.magnitude == 0)
        {
            State = Define.State.Idle;

            if (_spriteRenderer != null)
                _spriteRenderer.transform.localPosition = Vector3.zero;
            return;
        }

        transform.position += _moveDir * speed * Time.deltaTime;
        State = Define.State.Moving;

        _bounceTime += Time.deltaTime * bounceSpeed;
        float yOffest = Mathf.Abs(Mathf.Sin(_bounceTime)) * bounceHeight;

        if (_spriteRenderer != null)
        {
            float currentX = _spriteRenderer.transform.localPosition.x;
            _spriteRenderer.transform.localPosition = new Vector3(currentX, yOffest, 0);
            
            if (_moveDir.x != 0)
            {
                float xTargetScale = (_moveDir.x < 0) ? -1f : 1f;
                _spriteRenderer.transform.parent.localScale = new Vector3(xTargetScale, 1f, 1f);
            }
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


}