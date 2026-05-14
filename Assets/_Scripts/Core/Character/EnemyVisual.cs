using UnityEngine;

public class EnemyVisual : CharacterVisual
{
    protected override void Character_OnDamaged(object sender, Vector3 damageDirection)
    {
        base.Character_OnDamaged(sender, damageDirection);
        OnDamagedColorFlash(Color.red);
    }
}
