using System.Collections;
using UnityEngine;

// 작업지시서 #09 - 기둥 4개가 전부 채워지면(PillarSequenceState.OnAllPillarsFilled) 화면이 흔들리고
// 가운데 나무가 쭈그러들며 사라진 뒤, 그 자리에 ClearPortal이 나타난다.
// 건영님: 이 스크립트를 WitheredTree 오브젝트에 붙이고, Inspector의 Clear Portal 필드에
// ClearPortal 오브젝트를 연결해주세요. ClearPortal은 씬 시작 시 비활성화(체크 해제) 상태로 둬야 합니다.
public class WitheredTreePortalReveal : MonoBehaviour
{
    public GameObject clearPortal;
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 0.1f;
    public float shrinkDuration = 1f;

    private void OnEnable()
    {
        PillarSequenceState.OnAllPillarsFilled += HandleAllPillarsFilled;
    }

    private void OnDisable()
    {
        PillarSequenceState.OnAllPillarsFilled -= HandleAllPillarsFilled;
    }

    private void HandleAllPillarsFilled()
    {
        Debug.Log("[WitheredTreePortalReveal] 기둥 4개 완료 신호 받음 - 나무 반응 시작");
        StartCoroutine(RevealSequence());
    }

    private IEnumerator RevealSequence()
    {
        yield return CameraShake(shakeDuration, shakeMagnitude);
        yield return ShrinkTree();

        gameObject.SetActive(false);
        if (clearPortal != null) clearPortal.SetActive(true);
    }

    private IEnumerator CameraShake(float duration, float magnitude)
    {
        if (Camera.main == null) yield break;

        Vector3 originPos = Camera.main.transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = originPos.x + Random.Range(-magnitude, magnitude);
            float y = originPos.y + Random.Range(-magnitude, magnitude);
            Camera.main.transform.position = new Vector3(x, y, originPos.z);
            yield return null;
        }
        Camera.main.transform.position = originPos;
    }

    private IEnumerator ShrinkTree()
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / shrinkDuration);
            yield return null;
        }
        transform.localScale = Vector3.zero;

        // 아트팀 신규 에셋(펑 효과) - 나무가 다 쭈그러든 순간 재생.
        PoofEffect.Spawn(transform.position);
    }
}
