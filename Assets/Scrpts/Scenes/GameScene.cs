using UnityEngine;

public class GameScene : MonoBehaviour
{
    void Start()
    {
        Managers.UI.ShowSceneUI<UI_HPBar>();
        Managers.UI.ShowSceneUI<UI_Joystick>();
    }

    void Update()
    {
        
    }
}
