using UnityEngine;

public class AnimEventProxy : MonoBehaviour
{
    public BaseController mainController;
    public CrowController crow;
    public void OnHitEvent()
    {
        if (mainController != null)
        {
            mainController.OnHitEvent();
        }

        if (crow != null)
        {
            crow.Shoot();
        }
    }
}