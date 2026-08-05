using System;
using UnityEngine;

// 묵운산군의 도깨비불 볼트. 먹등불의 DokkaebiFireProjectile과 시각/이동 방식은 같지만
// (직선 이동, 맞으면 사라짐), 자체 데미지를 주지 않고 명중 시 표식 부여 콜백만 호출한다 —
// 명세서 3.3의 고정 데미지 목록에 "도깨비불 피격 자체"는 없고, 표식(산/구름)의 효과만
// 데미지를 가지므로, 먹등불 스크립트를 수정하는 대신 이 전용 컴포넌트를 새로 만듦.
public class BossDokkaebiFireBolt : MonoBehaviour
{
    public float speed = 12f;
    public float lifeTime = 3f;

    private Vector2 _dir;
    private bool _hasHit;
    private Action _onHitPlayer;

    public void Init(Vector2 direction, Action onHitPlayer)
    {
        _dir = direction.normalized;
        _onHitPlayer = onHitPlayer;
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (_hasHit) return;
        transform.position += (Vector3)(_dir * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasHit || !other.CompareTag("Player")) return;
        _hasHit = true;
        _onHitPlayer?.Invoke();
        Destroy(gameObject);
    }
}
