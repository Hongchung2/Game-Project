using System.Collections;
using UnityEngine;

public class SwordController : MonoBehaviour
{
    public Transform swordSprite; // 플레이어 무기 오브젝트
    public float swingSpeed = 10f; // 휘두르는 속도
    public float swingAngle = 120f; // 휘두르는 각도

    PlayerController _player;
    bool _isSwinging = false; 

    void Start()
    {
        _player = GetComponent<PlayerController>();
    }

    void Update()
    {
        PlayerController player = GetComponentInParent<PlayerController>();

        // 타겟 있으면 타겟 방향
        if (player._lockTarget != null)
        {
            LookAtTarget(player._lockTarget.transform.position);
        }
        // 타겟 없으면 이동 방향
        else
        {
            LookAtMoveDir(player._moveDir);
        }
    }
    // 타겟 방향으로 칼 끝 향하게
    public void LookAtTarget(Vector3 targetPos)
    {
        /*Vector3 dir = (targetPos - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        angle -= 60f;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);*/
    }

    // 이동 방향으로 칼 끝 향하게
    public void LookAtMoveDir(Vector3 moveDir)
    {
        if (moveDir.magnitude == 0) return;
        float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;

        // 부모 스케일이 -1이면 (왼쪽 볼 때) 각도 반전
        if (transform.parent.localScale.x < 0)
        {
            angle -= 120f; // 왼쪽 볼 때 보정
        }
        else
        {
            angle -= 60f; // 오른 쪽 볼 때 보정
        }

        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    // 공격 휘두르기
    public IEnumerator Swing()
    {
        if (_isSwinging) yield break;
        _isSwinging = true;
        gameObject.SetActive(true);

        float startAngle, endAngle;
        if (transform.parent.localScale.x < 0)
        {
            startAngle = transform.eulerAngles.z - swingAngle / 2f;
            endAngle = startAngle + swingAngle;
        }
        else
        {
            startAngle = transform.eulerAngles.z + swingAngle / 2f;
            endAngle = startAngle - swingAngle;
        }
        float returnAngle = transform.eulerAngles.z;
        float elapsed = 0f;
        float duration = 1f / swingSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float angle = Mathf.Lerp(startAngle, endAngle, t);
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            yield return null;
        }

        // 원래 각도로 복귀
        transform.rotation = Quaternion.AngleAxis(returnAngle, Vector3.forward);

        _isSwinging = false;
    }

    
}
