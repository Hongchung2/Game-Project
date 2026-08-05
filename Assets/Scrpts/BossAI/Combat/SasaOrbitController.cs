using System.Collections;
using UnityEngine;

public enum SasaSide { Left, Right } // 좌사(구름 표식 발동) / 우사(산 표식 발동)

// 좌사/우사의 궤도 순환 이동. 궤도 반경/속도는 명세서 미기재라 임의값(판단 필요 항목, 보고 대상).
public class SasaOrbitController : MonoBehaviour
{
    public SasaSide Side { get; private set; }
    public Transform OrbitCenter { get; private set; }
    public float orbitRadius = 2f;
    public float orbitSpeedDegPerSec = 60f;

    private float _angle;
    private Coroutine _cueRoutine;

    public void Init(Transform orbitCenter, SasaSide side)
    {
        OrbitCenter = orbitCenter;
        Side = side;
        _angle = Random.Range(0f, 360f);
    }

    private void Update()
    {
        if (OrbitCenter == null) return;

        _angle += orbitSpeedDegPerSec * Time.deltaTime;
        float rad = _angle * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * orbitRadius;
        transform.position = (Vector2)OrbitCenter.position + offset;
    }

    // 표식 발동 시 짧은 시각 피드백(확대+발광). 아트 없는 상태의 최소 placeholder.
    public void PlayActivateCue()
    {
        if (_cueRoutine != null) StopCoroutine(_cueRoutine);
        _cueRoutine = StartCoroutine(ActivateCueRoutine());
    }

    private IEnumerator ActivateCueRoutine()
    {
        Vector3 originalScale = transform.localScale;
        transform.localScale = originalScale * 1.4f;
        yield return new WaitForSeconds(0.3f);
        transform.localScale = originalScale;
    }
}
