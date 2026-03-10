using UnityEngine;

public class AnimEventProxy : MonoBehaviour
{
    public void OnHitEvent()
    {
        // MonsterController로 직접 캐스팅해서 호출!
        var controller = GetComponentInParent<MonsterController>();
        if (controller != null)
        {
            controller.OnHitEvent(); // public이니까 직접 호출 가능!
            Debug.Log(">>> 자식에서 부모로 공격 신호 배달 완료! <<<");
        }
    }
}