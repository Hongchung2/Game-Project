using UnityEngine;

// Bullet.cs를 수정하지 않고 까마귀 탄환의 명중/회피 여부를 관찰하기 위한 보조 컴포넌트.
//
// 이전 버전은 Bullet이 파괴되는 시점(명중=즉시, 회피=5초 타임아웃)에 회피 방향을 체크했는데,
// 회피의 경우 실제로 피한 순간과 5초나 떨어져 있어서 플레이어 속도를 전혀 다른 시점에 읽는
// 문제가 있었다. 지금은 매 프레임 "탄환이 플레이어를 막 지나쳤는지"를 기하학적으로 판정해서,
// 회피가 실제로 확정되는 바로 그 프레임에 방향을 읽는다(임계값 튜닝 없이 타이밍만 맞춤).
public class TelemetryBulletObserver : MonoBehaviour
{
    private string _monsterType;
    private float _telegraphDuration;
    private GameObject _player;
    private Rigidbody2D _bulletRb;
    private Vector2 _playerStartPos; // 계측 전용: 발사 시점 플레이어 위치(회피 방향 판정용)
    private int _stage;
    private bool _outcomeReported = false;

    public static void Attach(GameObject bullet, string monsterType, float telegraphDuration, GameObject player, int stage)
    {
        var observer = bullet.AddComponent<TelemetryBulletObserver>();
        observer._monsterType = monsterType;
        observer._telegraphDuration = telegraphDuration;
        observer._player = player;
        observer._bulletRb = bullet.GetComponent<Rigidbody2D>();
        observer._playerStartPos = player != null ? (Vector2)player.transform.position : Vector2.zero;
        observer._stage = stage;
    }

    private void Update()
    {
        if (_outcomeReported || _player == null || _bulletRb == null) return;

        Vector2 velocityDir = _bulletRb.linearVelocity.normalized;
        if (velocityDir == Vector2.zero) return;

        Vector2 toPlayer = (Vector2)_player.transform.position - (Vector2)transform.position;

        // 탄환이 플레이어를 이미 지나쳐서 플레이어가 탄환 진행 방향 "뒤쪽"에 있으면
        // 회피가 확정된 바로 그 프레임으로 본다 (Bullet.cs의 파괴 시점을 기다리지 않음).
        if (Vector2.Dot(toPlayer, velocityDir) < 0f)
        {
            ReportDodged();
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
            // 계측 실패가 전투/씬 정리에 영향을 주지 않도록 무시
        }
    }

    private void OnDestroy()
    {
        if (_outcomeReported) return; // Update()에서 이미 회피로 판정한 경우 중복 발행 방지

        try
        {
            // 회피 판정 없이 파괴됐다면 Bullet.Hit()에서 명중 처리되며 파괴된 것
            Telemetry.AttackHit(_monsterType, _telegraphDuration, _stage);
        }
        catch
        {
            // 계측 실패가 전투/씬 정리에 영향을 주지 않도록 무시
        }
    }
}
