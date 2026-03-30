using UnityEngine;

public class GameScene : MonoBehaviour
{
    void Start()
    {
        Managers.UI.ShowSceneUI<UI_PlayerHPBar>();
        //Managers.UI.ShowSceneUI<UI_Joystick>();
    }

}
