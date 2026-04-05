using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable {

    void TakeDamage(int damageAmount, Vector3 damageDirection);

    void Die();

    Vector3 GetPosition();
}
