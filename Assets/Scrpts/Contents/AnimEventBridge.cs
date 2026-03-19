using UnityEngine;

public class AnimEventBridge : MonoBehaviour
{
    public ScarecrowController mainController;

    public void OnHitEvent()
    {
        if (mainController != null)
            mainController.OnHitEvent();
    }
}
