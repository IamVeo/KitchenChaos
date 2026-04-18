using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthManager : MonoBehaviour, IDamageable {

    public event EventHandler OnDied;
    public event EventHandler<OnHealthChangedEventArgs> OnHealthChanged;
    public class OnHealthChangedEventArgs : EventArgs {
        public float healthNormalized;
    }
    
    public event Action<int, Vector3> OnDamageTaken;

    [SerializeField] private int maxHealth;
    private int currentHealth;

    [SerializeField] private float invulnerabilityDuration = .3f;
    private float invulnerabilityTimer;
    

    private void Awake() {
        currentHealth = maxHealth;
    }

    private void Update() {
        if(invulnerabilityTimer > 0) {
            invulnerabilityTimer -= Time.deltaTime;
        }
    }

    public void SetMaxHealth(int health) {
        maxHealth = health;
        currentHealth = maxHealth;

        OnHealthChanged?.Invoke(this, new OnHealthChangedEventArgs {
            healthNormalized = (float)currentHealth / maxHealth 
        });
    }

    public void TakeDamage(int damageAmount, Vector3 damageDirection) {
        if (invulnerabilityTimer > 0) return;

        currentHealth -= damageAmount;
        if (currentHealth <= 0) {
            currentHealth = 0;
        }
        
        OnHealthChanged?.Invoke(this, new OnHealthChangedEventArgs {
            healthNormalized = (float)currentHealth / maxHealth 
        });
        OnDamageTaken?.Invoke(damageAmount, damageDirection.normalized);

        invulnerabilityTimer = invulnerabilityDuration;

        if (currentHealth == 0) {
            Die();
        }
    }

    public void Die() {
        OnDied?.Invoke(this, EventArgs.Empty);
    }
}
