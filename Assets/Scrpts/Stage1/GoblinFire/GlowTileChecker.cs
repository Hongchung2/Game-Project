using Unity.VisualScripting;
using UnityEngine;

public class GlowTileChecker : MonoBehaviour
{
    public Transform respawnPoint;
    public float checkRadius = 0.3f;
    public bool IsGoblinFire = false; // 안마당 구역 안에 있을 때만 체크

    void Update()
    {
        if (!IsGoblinFire) return;

        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, checkRadius);
        bool onGlowTile = false;

        foreach (var col in cols)
        {
            if (col.CompareTag("GlowTile"))
            {
                onGlowTile = true;
                break;
            }
        }

        if (!onGlowTile)
        {
            transform.position = respawnPoint.position;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("GoblinFireArea"))
        {
            IsGoblinFire = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("GoblinFireArea"))
        {
            IsGoblinFire = false;
        }
    }
}
