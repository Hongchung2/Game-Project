using UnityEngine;

public class InvincibleStat : Stat
{
    public override void OnAttacked(Stat attacker)
    {
        if (attacker == null) return;
    }
}
