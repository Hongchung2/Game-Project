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
        _player = GameObject.FindWithTag("Player").transform;
    }

    void Update()
    {
        foreach (var sr in _renderers)
        {
            float distance = Vector2.Distance(sr.transform.position, _player.position);
            sr.enabled = distance > hideRadius;
        }
    }
}
