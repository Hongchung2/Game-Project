using UnityEngine;

// 특정 호수 구역에 배치된 몬스터들을 인스펙터에서 연결해두고, 전부 처치됐는지 확인한다.
public class LakeMonsterZone : MonoBehaviour
{
    public GameObject[] monsters;

    public bool AllDefeated
    {
        get
        {
            foreach (var m in monsters)
            {
                if (m != null && m.activeInHierarchy) return false;
            }
            return true;
        }
    }
}
