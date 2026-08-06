using UnityEngine;
using UnityEngine.Tilemaps;

public class PathHider : MonoBehaviour
{
    public float hideRadius = 1.5f;
    Transform _player;
    SpriteRenderer[] _renderers;

    void Start()
    {
        _renderers = GetComponentsInChildren<SpriteRenderer>();
        // 플레이어가 없는 씬에서 바로 널 참조로 죽지 않게 - 또 FindWithTag는 Stat이 없는 자식
        // "WallCollider"를 돌려줄 수 있어서 본체를 보장하는 PlayerLocator를 쓴다.
        GameObject player = PlayerLocator.Find();
        if (player != null) _player = player.transform;
    }

    void Update()
    {
        if (_player == null) return;

        foreach (var sr in _renderers)
        {
            float distance = Vector2.Distance(sr.transform.position, _player.position);
            sr.enabled = distance > hideRadius;
        }
    }
}
