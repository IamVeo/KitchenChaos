
using System;

public interface  IDamagable
{
    public event EventHandler OnDamaged;
    
    public void TakeDamage(int damageAmount);
}