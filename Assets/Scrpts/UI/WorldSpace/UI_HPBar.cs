using System.ComponentModel;
using UnityEngine.UI;
using UnityEngine;
<<<<<<< Updated upstream
using Unity.VisualScripting;

public class UI_HPBar : UI_Scene
=======
using UnityEngine.UI;


public class UI_HPBar : UI_Base
>>>>>>> Stashed changes
{
    enum GameObjects
    {
        HPBar
<<<<<<< Updated upstream
=======
    }

    Stat _stat;
    public override void Init()
    {
        Bind<GameObject>(typeof(GameObjects));
        _stat = transform.parent.GetComponent<Stat>();
>>>>>>> Stashed changes
    }

    [SerializeField]
    Stat _stat;

    public override void Init()
    {
        Bind<GameObject>(typeof(GameObjects));

        if (_stat == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                _stat = player.GetComponent<Stat>();
        }
    }
    void Update()
    {
        if (_stat != null)
        {
            float ratio = _stat.Hp / (float)_stat.MaxHp;
            SetHpRatio(ratio);
        }
    }

    public void SetHpRatio(float ratio)
    {
        var slider = GetObject((int)GameObjects.HPBar).GetComponent<Slider>();
        if (slider != null)
            slider.value = ratio;
    }
}
