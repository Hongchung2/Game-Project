using System.Collections;
using UnityEngine;

public class SpearController : MonoBehaviour
{
    public Transform spearSprite; // 창 오브젝트
    public float thrustDistance = 0.5f; // 찌르는 거리
    public float thrustSpeed = 10f; // 찌르는 속도

    bool _isThrusting = false;

    // 타겟 방향으로 창 끝 향하게
    public void LookAtTarget(Vector3 targetPos)
    {
        Vector2 dir = (targetPos - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    // 찌르기 모션
    public IEnumerator Thrust()
    {
        if (_isThrusting) yield break;
        _isThrusting = true;

        Vector3 startPos = spearSprite.localPosition;
        Vector3 thrustPos = startPos + Vector3.right * thrustDistance;

        // 찌르기
        float elapsed = 0f;
        float duration = 1f / thrustSpeed;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            spearSprite.localPosition = Vector3.Lerp(startPos, thrustPos, elapsed / duration);
            yield return null;
        }

        // 복귀
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            spearSprite.localPosition = Vector3.Lerp(thrustPos, startPos, elapsed / duration);
            yield return null;
        }

        spearSprite.localPosition = startPos;
        _isThrusting = false;
    }
}
