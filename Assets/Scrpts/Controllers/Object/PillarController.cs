using UnityEngine;

public class PillarController : MonoBehaviour
{
    private Collider2D _col;
    private SpriteRenderer _sr;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _sr = GetComponent<SpriteRenderer>();
    }

    // 스위치 작동 시 기둥 비활성화 (석상이 통과 가능)
    public void Deactivate()
    {
        if (_col != null) _col.enabled = false;
        if (_sr != null) _sr.enabled = false;
    }

    // 리셋 시 기둥 다시 활성화
    public void Activate()
    {
        if (_col != null) _col.enabled = true;
        if (_sr != null) _sr.enabled = true;
    }
}
