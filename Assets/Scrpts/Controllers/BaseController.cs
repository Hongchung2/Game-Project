using UnityEngine;

public abstract class BaseController : MonoBehaviour
{

    [SerializeField]
    protected Vector3 _destPos;

    [SerializeField]
    protected Define.State _state = Define.State.Idle;

    [SerializeField]
    public GameObject _lockTarget;

    public Define.WorldObject WorldObjectType { get; protected set; } = Define.WorldObject.Unknown;

    public virtual Define.State State
    {
        get { return _state; }
        set
        {
            _state = value;
            switch (_state)
            {
                case Define.State.Die:
                    OnDie();
                    break;
                case Define.State.Idle:
                    break;
                case Define.State.Moving:
                    break;
                case Define.State.Skill:
                    break;
                case Define.State.Return:
                    break;
            }
        }
    }
    void Start()
    {
        Init();
    }

    void Update()
    {
        switch (State)
        {
            case Define.State.Die:
                UpdateDie();
                break;
            case Define.State.Moving:
                UpdateMoving();
                break;
            case Define.State.Idle:
                UpdateIdle();
                break;
            case Define.State.Skill:
                UpdateSkill();
                break;
            case Define.State.Return:
                UpdateReturn();
                break;
        }
    }

    public abstract void Init();
    protected virtual void UpdateDie() { }
    protected virtual void UpdateMoving() { }
    protected virtual void UpdateIdle() { }
    protected virtual void UpdateSkill() { }
    protected virtual void UpdateReturn() { }
    protected virtual void OnDie() {}
    public virtual void OnHitEvent() { }
}
/*
 protected override void UpdateReturn()
    {
        // ����� �Ÿ� ���
        Vector3 dir = (_spawnPos - transform.position).normalized;
        float distToThomeSqr = (_spawnPos - transform.position).sqrMagnitude;

        // ���� ����
        if (distToThomeSqr < 0.01f)
        {
            _rb.linearVelocity = Vector2.zero;
            transform.position = _spawnPos;
            State = Define.State.Idle;
            _lockTarget = null;
            return;
        }
// Rigidbody�� �̵� (�̵� ��� ����)
        _rb.linearVelocity = dir * _stat.MoveSpeed;

        // ���ư� ���� ���� ��ȯ
        if (dir.x != 0)
        {
            float xTargetScale = (dir.x < 0) ? -1f : 1f;
            transform.localScale = new Vector3(xTargetScale * _initialScale.x, _initialScale.y, _initialScale.z);
        }
 */