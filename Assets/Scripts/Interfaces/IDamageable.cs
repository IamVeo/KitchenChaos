
using System;

public interface IDamageable
{
    public event EventHandler OnDamaged;
    
    public void TakeDamage(int damageAmount);
}