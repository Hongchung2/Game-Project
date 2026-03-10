using UnityEngine;

public class Stat : MonoBehaviour
{
    [SerializeField]
    protected int _level;
    [SerializeField]
    protected int _hp;
    [SerializeField]
    protected int _maxhp;
    [SerializeField]
    protected int _attack;
    [SerializeField]
    protected float _attackSpeed;
    [SerializeField]
    protected int _defense;
    [SerializeField]
    protected float _moveSpeed;

    public int Level { get { return _level; } set { _level = value; } }
    public int Hp { get { return _hp; } set { _hp = value; } }
    public int MaxHp { get { return _maxhp; } set { _maxhp = value; } }
    public int Attack { get { return _attack; } set { _attack = value; } }
    public float AttackSpeed { get { return _attackSpeed; } set { _attackSpeed = value; } }
    public int Defense { get { return _defense; } set { _defense = value; } }
    public float MoveSpeed { get { return _moveSpeed; } set { _moveSpeed = value; } }

    void Start()
    {
        _level = 1;
        _hp = 100;
        _maxhp = 100;
        _attack = 10;
        _attackSpeed = 1.0f;
        _defense = 5;
        _moveSpeed = 5.0f;

    }

    public virtual void OnAttacked(Stat attacker)
    {
        int damage = Mathf.Max(0, attacker.Attack - Defense);
        Hp -= damage;

        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            if (Hp <= 0)
            {
                Hp = 0;
                BaseController controller = GetComponent<BaseController>();
                if (controller != null)
                {
                    controller.State = Define.State.Die; 
                }
                anim.SetTrigger("Dead");

                foreach (Transform child in anim.transform)
                {
                    child.gameObject.SetActive(false);
                }
                //OnDead(attacker);
            }
            Debug.Log($"{gameObject.name}맞음! 현재 hp {Hp}");
        }
    }


    /*protected virtual void OnDead(Stat attacker)
    {
        PlayerStat playerStat = attacker as PlayerStat;
        if (playerStat != null)
        {
            playerStat.Exp += 5;
        }

        Managers.Game.Despawn(gameObject);

        // 부모 오브젝트에 붙은 컨트롤러 찾기
        BaseController controller = GetComponent<BaseController>();
        if (controller != null)
        {
            controller.State = Define.State.Die; // 🚩 여기서 상태 변경!
            Debug.Log("상태를 Die로 변경 시도함");
        }
        else
        {
            Debug.LogError("BaseController를 찾을 수 없습니다!");
        }

        // 자식(Visual)에 있는 애니메이터 찾기
        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.CrossFade("Dead", 0.1f);
        }

        // 물리/충돌 끄기
        if (GetComponent<Collider2D>() != null) GetComponent<Collider2D>().enabled = false;
        if (GetComponent<Rigidbody2D>() != null) GetComponent<Rigidbody2D>().simulated = false;

        Debug.Log($"{gameObject.name} 사망 처리 완료");
    }*/

}


    
