using System;
using UnityEngine;

public class Character : MonoBehaviour
{
    [SerializeField] protected CharacterDataSO characterDataSO;
    [SerializeField] protected CharacterVisual characterVisual;

    protected Rigidbody rb;
    protected HealthManager healthManager;
    
    // ----------- identity
    public string CharacterName => characterDataSO.characterName;
    
    // ----------- movement
    public float MoveSpeed => characterDataSO.moveSpeed;
    
    // ----------- combat
    public int MaxHealth => characterDataSO.maxHealth;
    public int AttackDamage => characterDataSO.attackDamage;
    public float AttackRange => characterDataSO.attackRange;
    public float HitRadius => characterDataSO.hitRadius;
    public float AttackCooldown => characterDataSO.attackCooldown;

    public event EventHandler OnAttackPerformed;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        healthManager = GetComponent<HealthManager>();

        characterVisual.Bind(this);
        healthManager.SetMaxHealth(MaxHealth);
    }

    private void Start()
    {
        healthManager.OnDamageTaken += HealthManager_OnDamageTaken;
    }
    
    private void HealthManager_OnDamageTaken(int damageAmount, Vector3 damageDirection)
    {
        ReceiveKnockback(damageDirection);
    }
    
    public virtual bool IsWalking() => false;
    public virtual bool IsAttacking() => false;
    protected virtual void ReceiveKnockback(Vector3 direction) {}

    protected void RaiseAttackPerformed()
    {
        OnAttackPerformed?.Invoke(this, EventArgs.Empty);
    }

    protected T DataAs<T>() where T : CharacterDataSO
    {
        return characterDataSO as T;
    }
}
