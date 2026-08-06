using System.Collections;
using UnityEngine;

// 페이즈2에서 헤롱헤롱 타이밍에 맞았는데 진짜 본체가 아니었던 분신이 변신하는 좀비.
// Enemy 0.png(Undead Survivor 에셋)의 Run 0~3을 순환시켜 걷는 것처럼 보이게 하고,
// 본체가 죽으면(Director.ClearSequence) Hit -> Dead 애니메이션을 재생하고 사라진다.
public class ZombieChaser : MonoBehaviour
{
    private const float MOVE_SPEED = 1.3f;
    private const int CONTACT_DAMAGE = 10;
    private const float CONTACT_INTERVAL = 1f;
    private const float CONTACT_RADIUS = 0.6f;
    private const float RUN_FRAME_INTERVAL = 0.15f;

    private GameObject _target;
    private Stat _sourceStat;
    private SpriteRenderer _spriteRenderer;
    private Sprite[] _runSprites;
    private Sprite _hitSprite;
    private Sprite _deadSprite;
    private float _contactCooldown;
    private float _runFrameTimer;
    private int _runFrameIndex;
    private bool _dying;

    public void Init(GameObject target, GoblinGeneralBossDirector director)
    {
        _target = target;
        _spriteRenderer = GetComponent<SpriteRenderer>();

        if (director != null)
        {
            _runSprites = director.zombieRunSprites;
            _hitSprite = director.zombieHitSprite;
            _deadSprite = director.zombieDeadSprite;
            director.RegisterZombie(this);
        }

        // 이 좀비는 이미 "죽고 나서" 변신한 존재라 다시 Stat.OnAttacked/사망 경로를 태우지 않는다
        // (BaseController가 이미 Destroy된 상태라 bc.State 접근 시 MissingReferenceException 위험).
        // 기존 Stat(이미 Hp 0으로 죽어있는 상태)을 그대로 재사용해서 공격력 값만 꺼내 쓴다.
        _sourceStat = GetComponent<Stat>();
        if (_sourceStat != null) _sourceStat.Attack = CONTACT_DAMAGE;
    }

    private void Update()
    {
        if (_dying || _target == null) return;

        Vector3 dir = (_target.transform.position - transform.position).normalized;
        transform.position += dir * MOVE_SPEED * Time.deltaTime;

        if (dir.x != 0)
        {
            float xScale = Mathf.Abs(transform.localScale.x) * (dir.x < 0 ? 1f : -1f);
            transform.localScale = new Vector3(xScale, transform.localScale.y, transform.localScale.z);
        }

        AnimateRun();

        _contactCooldown -= Time.deltaTime;
        if (_contactCooldown <= 0f)
        {
            float dist = Vector2.Distance(transform.position, _target.transform.position);
            if (dist <= CONTACT_RADIUS)
            {
                Stat playerStat = _target.GetComponent<Stat>();
                if (playerStat != null) playerStat.OnAttacked(_sourceStat);
                _contactCooldown = CONTACT_INTERVAL;
            }
        }
    }

    private void AnimateRun()
    {
        if (_spriteRenderer == null || _runSprites == null || _runSprites.Length == 0) return;

        _runFrameTimer += Time.deltaTime;
        if (_runFrameTimer < RUN_FRAME_INTERVAL) return;
        _runFrameTimer = 0f;

        _runFrameIndex = (_runFrameIndex + 1) % _runSprites.Length;
        _spriteRenderer.sprite = _runSprites[_runFrameIndex];
    }

    // 본체(진짜 도깨비 장군)가 쓰러졌을 때 Director가 호출.
    public void Die()
    {
        if (_dying) return;
        _dying = true;
        StartCoroutine(DieSequence());
    }

    private IEnumerator DieSequence()
    {
        if (_spriteRenderer != null && _hitSprite != null) _spriteRenderer.sprite = _hitSprite;
        yield return new WaitForSeconds(0.25f);

        if (_spriteRenderer != null && _deadSprite != null) _spriteRenderer.sprite = _deadSprite;
        yield return new WaitForSeconds(0.6f);

        Destroy(gameObject);
    }
}
