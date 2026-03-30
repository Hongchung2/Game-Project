using UnityEngine;
using UnityEngine.UI;

public class UI_NormalMonsterHPBar : UI_Base
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
    }

    public void SetTarget(Stat stat)
    {
        _stat = stat;
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
        {
            slider.value = ratio;
        }
    }
}
