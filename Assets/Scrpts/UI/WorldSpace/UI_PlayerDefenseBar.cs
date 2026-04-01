using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerDefenseBar : UI_Scene
{
    enum GameObjects
    {
        DefenseBar
    }

    [SerializeField]
    Stat _stat;

    public override void Init()
    {
        Bind<GameObject>(typeof(GameObjects));

        if (transform != null)
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
            float ratio = _stat.Defense / _stat.MaxDefense;
            SetDefense(ratio);
        }
    }

    public void SetDefense(float ratio)
    {
        var slider = GetObject((int)GameObjects.DefenseBar).GetComponent<Slider>();
        if (slider != null)
        {
            slider.value = ratio;
        }
    }

}
