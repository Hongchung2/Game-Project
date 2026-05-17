using System.Collections;
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
    protected int _maxdefense;
    [SerializeField]
    protected float _moveSpeed;
    

    public ObjectData data;

    private SPUM_Prefabs spum;
    private BaseController bc;
    bool _IsDeath = false;

    //public int Level { get { return _level; } set { _level = value; } }
    public int Hp { get { return _hp; } set { _hp = value; } }
    public int MaxHp { get { return _maxhp; } set { _maxhp = value; } }
    public int Attack { get { return _attack; } set { _attack = value; } }
    public float AttackSpeed { get { return _attackSpeed; } set { _attackSpeed = value; } }
    public int Defense { get { return _defense; } set { _defense = value; } }
    public int MaxDefense { get { return _maxdefense; } set { _maxdefense = value; } } 
    public float MoveSpeed { get { return _moveSpeed; } set { _moveSpeed = value; } }

    // 기본 스탯
    public int base_Hp;
    public int add_Hp = 0;
    public int Total_Hp => base_Hp + add_Hp;

    public int base_MaxHp;
    public int add_MaxHp = 0;
    public int Total_MaxHp => base_MaxHp + add_MaxHp;

    public int base_Attack;
    public int add_Attack = 0;
    public int Total_Attack => base_Attack + add_Attack;

    public float base_AttackSpeed;
    public float add_AttackSpeed = 0f;
    public float Total_AttackSpeed => base_AttackSpeed + add_AttackSpeed;

    public int base_Defense;
    public int add_Defense = 0;
    public int Total_Defense => base_Defense + add_Defense;

    public int base_MaxDefense;
    public int add_MaxDefense = 0;
    public int Total_MaxDefense => base_MaxDefense + add_MaxDefense;

    public float base_MoveSpeed;
    public float add_MoveSpeed = 0f;
    public float Total_MoveSpeed => base_MoveSpeed + add_MoveSpeed;
    // 기본 스탯

    void Awake()
    {
        Init();
        if (data != null)
        {
            base_Hp = data.baseMaxHp;
            base_MaxHp = data.baseMaxHp;
            base_Attack = data.baseAttack;
            base_AttackSpeed = data.attackSpeed;
            base_Defense = data.baseMaxDefense;
            base_MaxDefense = data.baseMaxDefense;
            base_MoveSpeed = data.moveSpeed;
        }
        InitStats();
    }
    void Start()
    {

    }

    public virtual void Init()
    {
        spum = GetComponent<SPUM_Prefabs>();    
        bc = GetComponent<BaseController>();
    }
    // 스텟 초기화
    public void InitStats()
    {
        _maxhp = Total_MaxHp;
        _hp = _maxhp; 
        _attack = Total_Attack;
        _attackSpeed = Total_AttackSpeed;
        _defense = Total_Defense;
        _maxdefense = Total_MaxDefense;
        _moveSpeed = Total_MoveSpeed;
    }
    public virtual void OnAttacked(Stat attacker)
    {
        if (_IsDeath || attacker == null) return;
        if (_hp <= 0) return;

        int damage = attacker.Attack;
        

        if (_defense > 0)
        {
            if (_defense >= damage)
            {
                _defense -= damage;
                damage = 0;
            }
            else
            {
                damage = damage - _defense;
                _defense = 0;
            }
        }
        Debug.Log($"<color=red>[Hit]</color>{gameObject.name} | 데미지 {damage}");
        Debug.Log($"방어력: {_defense}");
        Hp -= damage;

        if (Hp <= 0)
        {
            Hp = 0;
            bc.State = Define.State.Die;
        }
        else
        {
            spum.PlayAnimation(PlayerState.DAMAGED, 0);
        }
        
    }


    IEnumerator DeadAction()
    {
        _IsDeath = true;

        if (bc != null) bc.State = Define.State.Die;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        
        if (spum != null) spum.PlayAnimation(PlayerState.DEATH, 0);

        yield return new WaitForSeconds(3.0f);

        gameObject.SetActive(false);

        _IsDeath=false;
    }

}


    
