using UnityEngine;


public class UI_HPBar : UI_Base
{
    enum GameObjects
    {
        HPBar
    }

    Stat _stat;
    public override void Init()
    {
        Bind<GameObject>(typeof(GameObjects));
        _stat = transform.parent.GetComponent<Stat>();
    }

    void Update()
    {
        
    }
}
