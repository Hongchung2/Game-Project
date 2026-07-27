using UnityEngine;

public class SwordWeapon : MonoBehaviour, IWeapon
{
    [SerializeField] float _attackrange = 1.0f;

    public void Attack(GameObject target, Stat AttackerStat)
    {
        if (target == null) return;
        Debug.Log("Attack target: " + target.name);
        float distance = Vector2.Distance(transform.position, target.transform.position);
        if (distance > _attackrange) return;

        Stat targetStat = target.GetComponent<Stat>();
        if (targetStat != null)
        {
            targetStat.OnAttacked(AttackerStat);
        }

        HiddenObjectController hiddenObject = target.GetComponent<HiddenObjectController>();
        if (hiddenObject != null)
        {
            hiddenObject.OnHitEvent();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackrange);
    }
}