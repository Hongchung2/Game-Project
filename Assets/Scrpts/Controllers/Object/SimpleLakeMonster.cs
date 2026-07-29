using UnityEngine;

// 겨울방 호수 주변 몬스터(먹등불/벼루게)의 최소 버전.
// 특별한 패턴 없이, 플레이어 공격을 받아 체력이 0이 되면 그냥 사라진다.
// LakeMonsterZone이 이 오브젝트의 activeInHierarchy 여부로 전멸했는지 체크한다.
public class SimpleLakeMonster : BaseController
{
    public override void Init()
    {
        WorldObjectType = Define.WorldObject.Monster;
    }

    protected override void OnDie()
    {
        gameObject.SetActive(false);
    }
}
