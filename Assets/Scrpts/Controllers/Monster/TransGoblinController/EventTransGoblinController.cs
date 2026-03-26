using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class EventTransGoblinController : BaseController
{
    Stat _stat;
    Rigidbody2D _rb;
    Vector3 _spawnPos;
    Vector3 _initialScale;
    private SPUM_Prefabs _spum;
    public float StopTime = 1f;
    private bool _isAttacking = false; // 공격 루틴 중인지 체크
    private bool _isDeath = false;
    private Coroutine _attackCoroutine;


    public override void Init()
    {
        _initialScale = transform.localScale;

        WorldObjectType = Define.WorldObject.Monster;

        _stat = gameObject.GetComponent<Stat>();

        _rb = gameObject.GetOrAddComponent<Rigidbody2D>();
        _rb.gravityScale = 0;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        _spawnPos = transform.position;

        _spum = GetComponent<SPUM_Prefabs>();
        if (_spum != null)
        {
            _spum.PopulateAnimationLists();
            _spum.OverrideControllerInit();

            State = Define.State.Idle;
            _spum.PlayAnimation(PlayerState.IDLE, 0);
        }
    }

    protected override void UpdateDie()
    {
        if (_isDeath) return;
        StartCoroutine(DeadAction());
    }

    IEnumerator DeadAction()
    {
        _isDeath = true;

        _rb.linearVelocity = Vector2.zero;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        _spum.PlayAnimation(PlayerState.DEATH, 0);

        yield return new WaitForSeconds(3.0f);

        gameObject.SetActive(false);
        _isDeath = false;
    }
}
