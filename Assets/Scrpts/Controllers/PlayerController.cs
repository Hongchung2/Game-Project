using UnityEngine;

public class PlayerController : BaseController
{
    PlayerStat _stat;
    Vector3 _moveDir;

    public override void Init()
    {
        WorldObjectType = Define.WorldObject.Player;
        _stat = gameObject.GetComponent<PlayerStat>();
        State = Define.State.Idle;
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

        float moveSpeed = 7.5f;
        transform.position += _moveDir * moveSpeed * Time.deltaTime;

        SpriteRenderer sp = GetComponent<SpriteRenderer>();
        if (sp != null)
        {
            if (_moveDir.x > 0) sp.flipX = false;
            else if (_moveDir.x < 0) sp.flipX = true;
        }
    }

    void GetMoveInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        _moveDir = new Vector3(h, v, 0).normalized;
    }

  
}