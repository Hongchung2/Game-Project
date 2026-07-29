using UnityEngine;

// 목령(가을 보스)이 떨어뜨린 청동 방울. 평소엔 비활성 상태로 있다가
// FallBossPlaceholder가 보스 처치 시 위치를 옮기고 활성화한다.
public class BronzeBellPickup : MonoBehaviour, IInteractable
{
    public string GetInteractText()
    {
        return "F - 청동 방울 줍기";
    }

    public void OnInteract()
    {
        BronzeBellState.HasBell = true;

        if (CenterMessageUI.Instance != null)
            CenterMessageUI.Instance.Show("청동 방울을 획득했습니다!");

        Debug.Log("청동 방울을 주웠다!");

        gameObject.SetActive(false);
    }
}
