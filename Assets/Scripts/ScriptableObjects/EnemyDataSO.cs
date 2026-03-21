using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/EnemyDataSO", fileName = "New_EnemyDataSO")]
public class EnemyDataSO : ScriptableObject {
    public string enemyName;
    public Transform prefab;

    public float moveSpeed = 3f;
    public float patienceMax = 30f;

    public int maxHealth = 100;
    public int attackDamage = 10;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.5f;
}