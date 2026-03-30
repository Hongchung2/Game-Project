using System.ComponentModel;
using UnityEngine.UI;
using UnityEngine;
using Unity.VisualScripting;



public class UI_PlayerHPBar : UI_Scene
{
    enum GameObjects
    {
        HPBar
    }

    [SerializeField]
    Stat _stat;
    public override void Init()
    {
        Bind<GameObject>(typeof(GameObjects));
        
        if (transform.parent != null)
        {
            _stat = transform.parent.GetComponent<Stat>();
        }

        if (_stat == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                _stat = player.GetComponent<Stat>();
            }
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
