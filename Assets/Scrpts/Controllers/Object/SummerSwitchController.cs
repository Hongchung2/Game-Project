using UnityEngine;

public class SummerSwitchController : MonoBehaviour, IInteractable
{
    [Header("연결할 기둥들")]
    public PillarController whitePillar;
    public PillarController blackPillar;

    private bool _isActivated = false;

    public string GetInteractText()
    {
        return _isActivated ? "" : "F - 스위치 작동";
    }

    public void OnInteract()
    {
        if (_isActivated) return;

        _isActivated = true;

        if (whitePillar != null) whitePillar.Deactivate();
        if (blackPillar != null) blackPillar.Deactivate();

        Debug.Log("스위치 작동! 흰 기둥과 검은 기둥이 사라졌습니다.");
    }

    public void ResetSwitch()
    {
        _isActivated = false;

        if (whitePillar != null) whitePillar.Activate();
        if (blackPillar != null) blackPillar.Activate();
    }
}
