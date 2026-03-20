using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerData", menuName = "ScriptableObjects/PlayerData")]
public class PlayerData : ScriptableObject
{
    [Header("---Player Info---")]
    public string PlayerName;

    [Header("---Base Stats ---")]
    public int baseMaxHp = 10;
    public int baseAttack = 1;
    public int baseDefense = 1;
    public float attackSpeed = 1.0f;
    public float moveSpeed = 3.0f;

}
