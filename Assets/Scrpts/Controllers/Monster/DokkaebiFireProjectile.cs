using System.Collections;
using UnityEngine;

// 먹등불의 '도깨비불' 투사체. 직선으로 빠르게 날아가며, 맞으면 즉시 피해 + 화상 도트 피해.
// 데미지는 플레이어 최대체력의 고정 비율(기획서 수치)이라 방어력을 거치지 않고 직접 Hp를 깎는다.
public class DokkaebiFireProjectile : MonoBehaviour
{
    public float speed = 12f;
    public float lifeTime = 3f;

    public float hitDamageFraction = 1f / 8f;   // 즉시 피해: 최대체력의 1/8
    public float burnTotalFraction = 1f / 7f;   // 화상 도트 총합: 최대체력의 1/7
    public float burnDuration = 3f;
    public int burnTicks = 6;

    private Vector2 _dir;
    private bool _hasHit = false;

    public void Init(Vector2 direction)
    {
        _dir = direction.normalized;
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
        if (playerStat == null) return;

        _hasHit = true;

        int hitDmg = Mathf.RoundToInt(playerStat.MaxHp * hitDamageFraction);
        playerStat.Hp = Mathf.Max(0, playerStat.Hp - hitDmg);

        // 시각적으로는 사라지지만, 화상 도트가 끝날 때까지 오브젝트는 유지
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        StartCoroutine(BurnDot(playerStat));
    }

    private IEnumerator BurnDot(Stat playerStat)
    {
        int totalBurn = Mathf.RoundToInt(playerStat.MaxHp * burnTotalFraction);
        int perTick = Mathf.Max(1, totalBurn / burnTicks);
        float tickInterval = burnDuration / burnTicks;

        for (int i = 0; i < burnTicks; i++)
        {
            yield return new WaitForSeconds(tickInterval);
            if (playerStat == null) yield break;
            playerStat.Hp = Mathf.Max(0, playerStat.Hp - perTick);
        }

        Destroy(gameObject);
    }
}
