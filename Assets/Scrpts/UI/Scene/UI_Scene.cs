using UnityEngine;

public class UI_Scene : UI_Base
{
    // UI 생성 시 맨 뒤로 이동
    public override void Init()
    {
        Managers.UI.SetCanvas(gameObject, false);
    }
}
