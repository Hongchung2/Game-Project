using UnityEngine;
using UnityEngine.UI;

public class UI_BossHPBar : UI_Scene
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
            GameObject Monster = GameObject.FindWithTag("Monster");
            if (Monster != null)
            {
                _stat = Monster.GetComponent<Stat>();
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
