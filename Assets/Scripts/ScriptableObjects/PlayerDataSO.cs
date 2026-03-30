using UnityEngine;

[CreateAssetMenu(fileName = "PlayerDataSO", menuName = "ScriptableObjects/PlayerDataSO")]
public class PlayerDataSO : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed;
    public float playerHeight;
    public float playerRadius;
 
    
    [Header("Combat")]
    public float attackDelay;
    public int attackDamage;
    public float attackRange;
    public float hitRadius;
    
    [Header("Interactions")]
    public float interactDistance;
}