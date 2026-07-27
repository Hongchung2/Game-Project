using UnityEngine;

// 서쪽/동쪽 호수. 정화수 퍼즐을 풀고, 그 호수 구역의 몬스터를 전부 처치해야만
// F키 상호작용이 가능해진다(그 전엔 Layer를 Interactable이 아닌 상태로 둬서 감지 자체가 안 되게 함).
public class LakeController : MonoBehaviour, IInteractable
{
    public LakeMonsterZone monsterZone;
    public bool isWestLake; // true = 서쪽 호수, false = 동쪽 호수

    private bool _purified = false;
    private bool _activated = false;

    private void Update()
    {
        if (_activated || _purified) return;
        if (!WaterPuzzleState.WaterSplit) return;
        if (monsterZone != null && !monsterZone.AllDefeated) return;

        _activated = true;
        gameObject.layer = LayerMask.NameToLayer("Interactable");
        Debug.Log($"{(isWestLake ? "서쪽" : "동쪽")} 호수 정화 가능 (F키 활성화)");
    }

    public string GetInteractText()
    {
        return "F - 정화수 뿌리기";
    }

    public void OnInteract()
    {
        if (_purified) return;
        _purified = true;

        if (isWestLake) LakePurifyState.WestPurified = true;
        else LakePurifyState.EastPurified = true;

        if (CenterMessageUI.Instance != null)
            CenterMessageUI.Instance.Show("정화수를 호수에 뿌렸다!");

        Debug.Log($"{(isWestLake ? "서쪽" : "동쪽")} 호수 정화 완료!");
    }
}
