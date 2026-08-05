using UnityEngine;
using System.Collections;
public class GoblinDisappear : MonoBehaviour
{
    [Header("분열 연출")]
    public float splitSpeed = 3f; // 날아가는 속도
    public float disappearTime = 1f; // 페이드 아웃 시간

    [Header("변신한 물건들")]
    public GameObject[] hiddenObjects;

    // 5갈래 방향
    Vector2[] splitDirections = new Vector2[]
    {
        Vector2.up,
        new Vector2(1, 1).normalized,
        new Vector2(-1, 1).normalized,
        new Vector2(1, -1).normalized,
        new Vector2(-1, -1).normalized,
    };

    public IEnumerator DisappearAndSplit(MonoBehaviour runner)
    {
        Debug.Log("DisappearAndSplit 시작");
        // 흰 연기 연출 (스케일 업 후 페이드)
        yield return StartCoroutine(SmokeEffect());
        Debug.Log("SmokeEffect 끝");

        // 5갈래 분열
        SpriteRenderer originalSr = GetComponentInChildren<SpriteRenderer>();
        Debug.Log("originalSr: " + originalSr);
        for (int i=0; i<5; i++)
        {
            GameObject clone = new GameObject("GoblinClone");
            clone.transform.position = transform.position;
            clone.transform.localScale = transform.localScale; // 스케일 복사
            SpriteRenderer sr = clone.AddComponent<SpriteRenderer>();
            Debug.Log("clone 생성: " + i);
            if (originalSr != null)
            {
                sr.sprite = originalSr.sprite;
                sr.color = originalSr.color;
                sr.sortingLayerName = originalSr.sortingLayerName;
                sr.sortingOrder = originalSr.sortingOrder;
            }
            runner.StartCoroutine(FlyAndFade(clone, splitDirections[i]));
        }

        // 물건 활성화
        foreach (var obj in hiddenObjects)
        {
            if (obj != null) obj.SetActive(true);
        }
       // 원본 비활성화
        gameObject.SetActive(false);
    }

    IEnumerator SmokeEffect()
    {
        // 흰 원 생성
        GameObject smoke = new GameObject("Smoke");
        smoke.transform.position = transform.position;
        SpriteRenderer sr = smoke.AddComponent<SpriteRenderer>();
        sr.color = Color.white;

        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.3f;
            smoke.transform.localScale = Vector3.one * Mathf.Lerp(0.1f, 2f, t);
            sr.color = new Color(1, 1, 1, Mathf.Lerp(1, 0, t));
            yield return null;
        }
        Destroy(smoke);
    }

    IEnumerator FlyAndFade(GameObject obj, Vector2 direction)
    {
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        float elapsed = 0f;

        while (elapsed < disappearTime)
        {
            elapsed += Time.deltaTime;
            obj.transform.position += (Vector3)direction * splitSpeed * Time.deltaTime;
            if (sr != null)
            {
                sr.color = new Color(1, 1, 1, Mathf.Lerp(1, 0, elapsed / disappearTime));
            }
            yield return null;
        }
        Destroy(obj);
    }
}
