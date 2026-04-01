using Unity.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "NewObjectData", menuName = "ScriptableObjects/ObjectData")]
public class ObjectData : ScriptableObject
{
    [Header("---Object Info---")]
    public string ObjectName;

    [Header("---Base Stats ---")]
    public int baseMaxHp = 10;
    public int baseAttack = 1;
    public int baseMaxDefense = 1;
    public float attackSpeed = 1.0f;
    public float moveSpeed = 3.0f;
    
}
