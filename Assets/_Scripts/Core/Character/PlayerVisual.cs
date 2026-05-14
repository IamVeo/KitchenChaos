using UnityEngine;

public class PlayerVisual : CharacterVisual
{
    protected override void Character_OnDamaged(object sender, Vector3 damageDirection)
    {
        base.Character_OnDamaged(sender, damageDirection);
        OnDamagedColorFlash(new Color(1f, 0.4f, 0.4f)); // Lighter red
    }
}
