using UnityEngine;

public class UI_Attack : UI_Scene
{
    enum GameObjects
    {
        KeyAttack
    }

    public override void Init()
    {
        base.Init();
        Bind<GameObject>(typeof (GameObjects));
    }
    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
