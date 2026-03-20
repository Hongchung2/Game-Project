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
    protected float _moveSpeed;

    public MonsterData data;

    private SPUM_Prefabs spum;
    private BaseController bc;
    bool _IsDeath = false;

    //public int Level { get { return _level; } set { _level = value; } }
    public int Hp { get { return _hp; } set { _hp = value; } }
    public int MaxHp { get { return _maxhp; } set { _maxhp = value; } }
    public int Attack { get { return _attack; } set { _attack = value; } }
    public float AttackSpeed { get { return _attackSpeed; } set { _attackSpeed = value; } }
    public int Defense { get { return _defense; } set { _defense = value; } }
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
            base_Defense = data.baseDefense;
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
        _moveSpeed = Total_MoveSpeed;
    }
    public virtual void OnAttacked(Stat attacker)
    {
        if (_IsDeath || attacker == null) return;

        int damage = Mathf.Max(0, attacker.Attack - Defense);
        Hp -= damage;

        Debug.Log($"<color=red>[Hit]</color>{gameObject.name} | 데미지 {damage}");

        

        if (Hp <= 0)
        {
            Hp = 0;

            if (!_IsDeath)
            {
                StartCoroutine(DeadAction());
            }
            
        }
        else
        {
            if (spum != null)
            {
                spum.PlayAnimation(PlayerState.DAMAGED, 0);
            }
        }
        
    }


    IEnumerator DeadAction()
    {
        _IsDeath = true;


        if (bc != null)
        {
            bc.State = Define.State.Die;
            bc._lockTarget = null;
        }
        if (spum != null) spum.PlayAnimation(PlayerState.DEATH, 0);

        yield return new WaitForSeconds(10.0f);

        Destroy(gameObject);

        _IsDeath=false;
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


    
