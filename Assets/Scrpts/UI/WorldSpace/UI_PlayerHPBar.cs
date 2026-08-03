using UnityEngine.UI;
using UnityEngine;
using TMPro;



public class UI_PlayerHPBar : UI_Scene
{
    enum GameObjects
    {
        HPBar,
        DefenseBar
    }

    [SerializeField]
    Stat _stat;

    [SerializeField]
    TMP_Text _HPText;

    [SerializeField]
    Slider _hpbar;

    [SerializeField]
    Slider _defensebar;
    public override void Init()
    {
        Bind<GameObject>(typeof(GameObjects));
        
        if (_stat != null) return; // 이미 스탯이 할당되어 있으면 스킵
        
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
        SetRatio();
    }

    
    public void SetRatio()
    {
        if (_stat == null || _hpbar == null || _defensebar == null) return;

        float Defense_ratio = _stat.Defense / (float)_stat.MaxDefense;
        _defensebar.value = Defense_ratio;
        float HP_ratio = _stat.Hp / (float)_stat.MaxHp;
        _hpbar.value = HP_ratio;

        if (_HPText != null)
        {
            _HPText.text = $"{_stat.Hp} / {_stat.MaxHp}";
        }
    }
}
