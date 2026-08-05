using System.Collections;
using UnityEngine;

// 먹등불의 '도깨비불' 투사체. 직선으로 빠르게 날아가며, 맞으면 즉시 피해 + 화상 도트 피해.
// 스탯 밸런싱 작업에서 변경: 예전엔 "플레이어 최대체력의 비율"로 방어력을 거치지 않고 직접
// Hp를 깎았음(몬스터마다 방어막 적용 여부가 달라 일관성이 깨져 있었음) - 이제 다른 몬스터들과
// 동일하게 Stat.OnAttacked()를 거치는 고정 정수 데미지로 통일. 명세서 수치(1/8, 1/7 × MaxHp 10)를
// 반올림한 고정값을 그대로 씀. 화상 도트도 원래 6틱으로 쪼개다 보니 "틱당 최소 1" 바닥값 때문에
// 총합이 의도한 1점이 아니라 6점이 되던 버그가 있었음 - 총합 그대로 한 번에 적용하게 단순화.
public class DokkaebiFireProjectile : MonoBehaviour
{
    public float speed = 12f;
    public float lifeTime = 3f;

    public const int HIT_DAMAGE = 1;   // 명세서 1/8 × MaxHp(10) ≈ 1
    public const int BURN_DAMAGE = 1;  // 명세서 1/7 × MaxHp(10) ≈ 1 (도트 총합, 한 번에 적용)
    public float burnDelay = 3f;       // 맞은 뒤 이 시간 후 화상 데미지 적용(잔류 연출용 대기)

    private Vector2 _dir;
    private bool _hasHit = false;
    private Stat _sourceStat; // 이 볼트를 쏜 먹등불의 Stat - OnAttacked의 attacker로 사용

    public void Init(Vector2 direction, Stat sourceStat)
    {
        _dir = direction.normalized;
        _sourceStat = sourceStat;
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (_hasHit) return;
        transform.position += (Vector3)(_dir * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasHit) return;
        if (!other.CompareTag("Player")) return;

        Stat playerStat = other.GetComponent<Stat>();
        if (playerStat == null || _sourceStat == null) return;

        _hasHit = true;

        _sourceStat.Attack = HIT_DAMAGE;
        playerStat.OnAttacked(_sourceStat);

        // 시각적으로는 사라지지만, 화상 도트가 끝날 때까지 오브젝트는 유지
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        StartCoroutine(BurnDot(playerStat));
    }

    private IEnumerator BurnDot(Stat playerStat)
    {
        yield return new WaitForSeconds(burnDelay);
        if (playerStat == null || _sourceStat == null) yield break;

        _sourceStat.Attack = BURN_DAMAGE;
        playerStat.OnAttacked(_sourceStat);

        Destroy(gameObject);
    }
}
