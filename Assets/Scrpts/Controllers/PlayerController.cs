using UnityEngine;

public class PlayerController : BaseController
{
    PlayerStat _stat;
    Vector3 _moveDir;
    public Joystick joystick;
    public float speed = 5f;
    Animator _anim;


    public override void Init()
    {
        WorldObjectType = Define.WorldObject.Player;
        _stat = gameObject.GetComponent<PlayerStat>();
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
            return;
        }

        transform.position += _moveDir * speed * Time.deltaTime;

        SpriteRenderer sp = GetComponent<SpriteRenderer>();
        if (sp != null)
        {
            if (_moveDir.x > 0) sp.flipX = false;
            else if (_moveDir.x < 0) sp.flipX = true;
        }
    }

    void GetMoveInput()
    {
        
        float h = 0;
        float v = 0;

        if (joystick != null && (joystick.Horizontal != 0 || joystick.Vertical != 0))
        {
            h = joystick.Horizontal;
            v = joystick.Vertical;
        }

        else
        {
            h = Input.GetAxisRaw("Horizontal");
            v = Input.GetAxisRaw("Vertical");
        }

        _moveDir = new Vector3(h, v, 0).normalized;
    }

  
}