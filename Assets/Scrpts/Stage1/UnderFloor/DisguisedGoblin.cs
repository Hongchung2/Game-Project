using System.Collections;
using UnityEngine;

public class DisguisedGoblin : MonoBehaviour
{
    public DoorController door; // 열릴 문
    public float keyShowTime = 1.5f; // 열쇠 획득 연출 시간

    public void OnHitEvent()
    {
        StartCoroutine(RevealAndUnlock());
    }

    IEnumerator RevealAndUnlock()
    {
        // 흰 연기 연출
        yield return StartCoroutine(SmokeEffect());

        // 열쇠 획득 연출
        Debug.Log("열쇠 획득!");
        if (CenterMessageUI.Instance != null)
            CenterMessageUI.Instance.Show("열쇠를 획득했다.", 1.5f);

        // 문 열기
        door.SetDoorLocked(false);

        // 오브젝트 사라짐
        gameObject.SetActive(false);
    }

    IEnumerator SmokeEffect()
    {
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
}
