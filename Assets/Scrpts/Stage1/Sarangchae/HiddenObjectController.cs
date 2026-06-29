using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class HiddenObjectController : MonoBehaviour
{
    public HiddenObjectManager hiddenObjectManager;
    public void OnHitEvent()
    {
        hiddenObjectManager?.OnObjectDestroyed();
        StartCoroutine(DisappearEffect());
    }

    IEnumerator DisappearEffect()
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
        Destroy(gameObject); 
    }
}
