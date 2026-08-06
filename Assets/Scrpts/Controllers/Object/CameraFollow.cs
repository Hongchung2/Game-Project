using UnityEngine;

// 플레이어를 따라다니는 카메라. Transform.SetParent로 직접 자식을 붙이면 플레이어가 죽어서
// gameObject.SetActive(false)될 때 자식인 카메라까지 같이 비활성화돼서 "No cameras rendering"
// 화면이 뜨는 문제가 있었음 - 그래서 부모-자식 관계 없이 매 프레임 위치만 따라가게 만듦.
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    private float _shakeIntensity;

    // duration<=0이면 명시적으로 StopShake()를 부를 때까지 계속 흔들림(보스방 붕괴 연출용).
    public void StartShake(float intensity)
    {
        _shakeIntensity = intensity;
    }

    public void StopShake()
    {
        _shakeIntensity = 0f;
    }

    private void LateUpdate()
    {
        if (target == null || !target.gameObject.activeInHierarchy) return;

        Vector3 pos = target.position + offset;
        if (_shakeIntensity > 0f)
        {
            pos += (Vector3)(Random.insideUnitCircle * _shakeIntensity);
        }
        transform.position = pos;
    }
}
