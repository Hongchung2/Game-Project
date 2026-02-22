using UnityEngine;

public class PlayerController : BaseController
{
    int _mask = (1 << (int)Define.layer.Ground) | (1 << (int)Define.layer.Monster);
    PlayerStat _stat;
    bool _stopSkill = false;

    public override void Init()
    {
        WorldObjectType = Define.WorldObject.Player;
        _stat = gameObject.GetComponent<PlayerStat>();

        Managers.Input.MouseAction -= OnMouseEvent;
        Managers.Input.MouseAction += OnMouseEvent;

    }

    void Update()
    {
        if (State == Define.State.Moving)
        {
            Vector3 dir = _destPos - transform.position;
            if (dir.magnitude < 0.1f)
            {
                State = Define.State.Idle;
            }
            else
            {
                float moveDist = Mathf.Clamp(5.0f * Time.deltaTime, 0, dir.magnitude);
                transform.position += dir.normalized * moveDist;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10 * Time.deltaTime);
            }
        }
    }


    void OnMouseEvent(Define.MouseEvent evt)
    {
        Debug.Log("Click!!");
        if (State == Define.State.Die) return;

        if (evt == Define.MouseEvent.PointerDown)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100.0f, LayerMask.GetMask("Ground")))
            {
                _destPos = hit.point;
                State = Define.State.Moving;
            }
        }
    }
}
