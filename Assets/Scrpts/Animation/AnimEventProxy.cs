using UnityEngine;

public class AnimEventProxy : MonoBehaviour
{
    public BaseController mainController;
    public void OnHitEvent()
    {
        if (mainController != null)
        {
            mainController.OnHitEvent();
        }
    }
}