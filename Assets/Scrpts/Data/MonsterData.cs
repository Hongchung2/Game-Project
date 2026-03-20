using Unity.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMonsterData", menuName = "ScriptableObjects/MonsterData")]
public class MonsterData : ScriptableObject
{
    [Header("---Monster Info---")]
    public string monsterName;

    [Header("---Base Stats ---")]
    public int baseMaxHp = 10;
    public int baseAttack = 1;
    public int baseDefense = 1;
    public float attackSpeed = 1.0f;
    public float moveSpeed = 3.0f;
    
}
