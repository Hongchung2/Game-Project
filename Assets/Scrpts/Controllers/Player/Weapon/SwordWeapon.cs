using UnityEngine;

public class SwordWeapon : MonoBehaviour, IWeapon
{
    [SerializeField] float _attackrange = 1.0f;

    public void Attack(GameObject target, Stat AttackerStat)
    {
        if (target == null) return;

        float distance = Vector2.Distance(transform.position, target.transform.position);
        if (distance > _attackrange) return;

        Stat targetStat = target.GetComponent<Stat>();
        if (targetStat != null)
        {
            targetStat.OnAttacked(AttackerStat);
        }
    }
}
