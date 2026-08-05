using System.Collections;
using UnityEngine;

// 아트팀 신규 에셋(펑 효과) - 족자 등장, 오브젝트 소멸 등에 재생하는 짧은 이펙트.
public class PoofEffect : MonoBehaviour
{
    private const string SPRITE_PATH = "Art/펑_효과";
    private const float DURATION = 0.4f;

    public static void Spawn(Vector3 position)
    {
        // Multiple 스프라이트 모드로 임포트된 텍스처는 Resources.Load<Sprite>로 못 찾고
        // LoadAll로만 서브 에셋을 가져올 수 있음.
        Sprite[] sprites = Resources.LoadAll<Sprite>(SPRITE_PATH);
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning($"[PoofEffect] 스프라이트를 찾지 못함: {SPRITE_PATH}");
            return;
        }

        GameObject go = new GameObject("PoofEffect");
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprites[0];
        sr.sortingOrder = 20;

        var effect = go.AddComponent<PoofEffect>();
        effect.StartCoroutine(effect.PlayAndDestroy(sr));
    }

    private IEnumerator PlayAndDestroy(SpriteRenderer sr)
    {
        Vector3 startScale = Vector3.one * 0.6f;
        Vector3 endScale = Vector3.one * 1.2f;
        transform.localScale = startScale;

        float elapsed = 0f;
        while (elapsed < DURATION)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / DURATION;
            transform.localScale = Vector3.Lerp(startScale, endScale, t);

            Color c = sr.color;
            c.a = 1f - t;
            sr.color = c;

            yield return null;
        }

        Destroy(gameObject);
    }
}
