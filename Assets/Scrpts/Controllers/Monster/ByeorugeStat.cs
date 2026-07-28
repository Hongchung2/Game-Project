using UnityEngine;

// 벼루게 전용 Stat. '먹물 방패' 중에는 공격 방향에 따라 피해 감소가 다르고(정면 80%, 측후면 20%),
// 방패 중 피격당하면 공격자에게 먹물 도트 데미지를 반사한다.
// Stat.OnAttacked()가 virtual이라 여기서 오버라이드해서 처리한다 (공용 Stat.cs는 건드리지 않음).
public class ByeorugeStat : Stat
{
    public ByeorugeController controller;

    public override void OnAttacked(Stat attacker)
    {
        if (_hp <= 0 || attacker == null) return;

        int damage = attacker.Attack;

        if (controller != null && controller.IsShielding)
        {
            Vector2 toAttacker = (Vector2)attacker.transform.position - (Vector2)transform.position;
            float angle = Vector2.Angle(controller.FacingDir, toAttacker);

            // 정면(각도 작음) = 80% 감소, 측면/후방 = 20%만 감소
            float reduction = (angle <= 60f) ? 0.8f : 0.2f;
            damage = Mathf.RoundToInt(damage * (1f - reduction));

            controller.SplashInkOnAttacker(attacker);
        }

        if (_defense > 0)
        {
            if (_defense >= damage) { _defense -= damage; damage = 0; }
            else { damage -= _defense; _defense = 0; }
        }

        Hp -= damage;

        Debug.Log($"[몬스터 HP] {name}: -{damage} → {Hp}/{MaxHp}");

        HitFlash hitFlash = GetComponent<HitFlash>();
        if (hitFlash != null) hitFlash.StartCoroutine(hitFlash.Flash());

        if (Hp <= 0)
        {
            Hp = 0;
            BaseController bc = GetComponent<BaseController>();
            if (bc != null) bc.State = Define.State.Die;
        }
    }
}
