using System.Collections;
using UnityEngine;

public class GlowTileController : MonoBehaviour
{
    SpriteRenderer _sr;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public IEnumerator Blink(float blinkTime, float interval)
    {
        float elapsed = 0f;
        while (elapsed < blinkTime)
        {
            _sr.enabled = !_sr.enabled;
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
        _sr.enabled = true;
    }
}
