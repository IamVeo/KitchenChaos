using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CharacterDataSO : ScriptableObject
{
    [Header("Identity")]
    public string characterName;
    public Transform prefab;
    
    [Header("Movement")]
    public float moveSpeed;

    [Header("Combat")] 
    public int maxHealth;
    public int attackDamage;
    public float attackRange;
    public float hitRadius;
    public float attackCooldown;
}
