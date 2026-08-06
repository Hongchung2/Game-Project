using UnityEngine;

// Character 프리팹 안의 자식 "WallCollider"도 태그가 Player라서, GameObject.FindWithTag("Player")가
// 본체가 아니라 그 자식을 돌려줄 수 있다(Unity는 같은 태그를 가진 오브젝트 중 무엇을 먼저 주는지
// 보장하지 않으며, 에디터와 빌드에서 결과가 달라질 수도 있다).
// 자식에는 Stat/PlayerController/Rigidbody2D가 없어서, 그 경우 GetComponent가 전부 null이 되고
// HP UI가 안 갱신되거나 보스가 플레이어에게 데미지를 못 주는 식으로 "에러 없이 조용히" 깨진다.
// 항상 진짜 본체(= Stat을 가진 오브젝트)를 돌려주도록 보정하는 공용 헬퍼.
public static class PlayerLocator
{
    public static GameObject Find()
    {
        GameObject[] tagged = GameObject.FindGameObjectsWithTag("Player");
        GameObject fallback = null;

        foreach (var go in tagged)
        {
            if (go == null) continue;
            if (fallback == null) fallback = go;
            if (go.GetComponent<Stat>() != null) return go;
        }

        // 태그 붙은 자식만 찾아진 경우 - 부모 쪽으로 거슬러 올라가 본체를 찾는다.
        if (fallback != null)
        {
            Stat stat = fallback.GetComponentInParent<Stat>();
            if (stat != null) return stat.gameObject;
        }

        return fallback;
    }
}
