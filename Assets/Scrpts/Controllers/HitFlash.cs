using UnityEngine;
using System.Collections;

public class HitFlash : MonoBehaviour
{
    SpriteRenderer _sr;
    public Color flashColor = Color.white;
    public float flashDuration = 0.1f;
    public int flashCount = 3; // 깜빡이는 횟수
    void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    public IEnumerator Flash()
    {
        Color originalColor = _sr.color;
        
        for (int i=0; i<flashCount; i++)
        {
            _sr.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            _sr.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }
    }
}
