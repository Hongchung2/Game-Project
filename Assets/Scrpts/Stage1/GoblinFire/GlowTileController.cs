using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GlowTileController : MonoBehaviour
{
    TilemapRenderer _tr;

    void Awake()
    {
        _tr = GetComponent<TilemapRenderer >();
    }

    public void SetVisible(bool visible)
    {
        GetComponent<TilemapRenderer>().enabled = visible;
        GetComponent<TilemapCollider2D>().enabled = visible;
    }

    public IEnumerator Blink(float blinkTime, float interval)
    {
        float elapsed = 0f;
        while (elapsed < blinkTime)
        {
            _tr.enabled = !_tr.enabled;
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
        _tr.enabled = true;
    }
}
