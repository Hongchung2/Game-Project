using UnityEngine;

// DokkaebiFireProjectile.cs를 수정하지 않고 도깨비불의 명중/회피 여부를 관찰하는 보조 컴포넌트.
//
// 이 투사체는 까마귀의 Bullet.cs와 달리 Rigidbody2D 없이 transform.position을 직접 옮기고
// (속도를 못 읽음), 명중해도 화상 도트 코루틴 때문에 한동안 파괴되지 않는다(Bullet은 명중 즉시 파괴).
// 그래서 TelemetryBulletObserver를 그대로 재사용할 수 없어 전용으로 새로 만들었다.
//
// 감지 방법:
// 1) 명중 — DokkaebiFireProjectile이 명중 시 스스로 Collider2D를 꺼버리는 부수효과를 매 프레임 관찰
// 2) 회피 — 매 프레임 위치 변화로 진행방향을 직접 추정해서, 투사체가 플레이어를 지나친 순간을 판정
public class TelemetryDokkaebiFireObserver : MonoBehaviour
{
    private string _monsterType;
    private float _telegraphDuration;
    private GameObject _player;
    private Collider2D _collider;
    private Vector3 _lastPosition;
    private Vector2 _playerStartPos; // 계측 전용: 발사 시점 플레이어 위치(회피 방향 판정용)
    private int _stage;
    private bool _outcomeReported = false;

    public static void Attach(GameObject projectile, string monsterType, float telegraphDuration, GameObject player, int stage)
    {
        var observer = projectile.AddComponent<TelemetryDokkaebiFireObserver>();
        observer._monsterType = monsterType;
        observer._telegraphDuration = telegraphDuration;
        observer._player = player;
        observer._collider = projectile.GetComponent<Collider2D>();
        observer._lastPosition = projectile.transform.position;
        observer._playerStartPos = player != null ? (Vector2)player.transform.position : Vector2.zero;
        observer._stage = stage;
    }

    private void Update()
    {
        if (_outcomeReported) return;

        if (_collider != null && !_collider.enabled)
        {
            ReportHit();
            return;
        }

        if (_player == null)
        {
            _lastPosition = transform.position;
            return;
        }

        Vector2 moveDir = (Vector2)transform.position - (Vector2)_lastPosition;
        _lastPosition = transform.position;
        if (moveDir == Vector2.zero) return;
        moveDir.Normalize();

        Vector2 toPlayer = (Vector2)_player.transform.position - (Vector2)transform.position;
        if (Vector2.Dot(toPlayer, moveDir) < 0f)
        {
            ReportDodged();
        }
    }

    private void ReportHit()
    {
        _outcomeReported = true;
        try
        {
            Telemetry.AttackHit(_monsterType, _telegraphDuration, _stage);
        }
        catch
        {
            // 계측 실패가 전투에 영향을 주지 않도록 무시
        }
    }

    private void ReportDodged()
    {
        _outcomeReported = true;
        try
        {
            Telemetry.AttackDodged(_monsterType, _telegraphDuration, Telemetry.ComputeDodgeDirectionFromPositions(_playerStartPos, _player.transform.position), _stage);
        }
        catch
        {
            // 계측 실패가 전투에 영향을 주지 않도록 무시
        }
    }
}
