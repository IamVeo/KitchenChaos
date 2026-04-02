using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class EnemyDataSO : ScriptableObject {

    [Header("Identity")]
    public string enemyName;
    public Transform prefab;

    [Header("Movement & Patience")]
    public float moveSpeed = 3f;
    public float patienceMax = 30f;

    [Header("Combat Stats")]
    public int maxHealth = 100;
    public int attackDamage = 10;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.5f;
}