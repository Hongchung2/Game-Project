using Unity.VisualScripting;
using UnityEngine;

public class BowWeapon : MonoBehaviour, IWeapon
{
    [SerializeField] float ArrowSpeed;
    [SerializeField] private GameObject arrowPrefab;
    public void Attack(GameObject target, Stat attackerStat)
    {
        if (target == null) return;

        Vector2 dir = (target.transform.position - transform.position).normalized;

        GameObject arrow = Instantiate(arrowPrefab, transform.position, Quaternion.identity);

        Bullet arrowComponent = arrow.GetOrAddComponent<Bullet>();
        arrowComponent.Init(attackerStat, true);

        Rigidbody2D rb = arrow.GetComponent<Rigidbody2D>();
        rb.linearVelocity = dir * ArrowSpeed;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        arrow.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        Destroy(arrow, 5.0f);
    }
}
