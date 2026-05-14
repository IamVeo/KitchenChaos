using UnityEngine;
using DG.Tweening;

public class CharacterVisual : MonoBehaviour
{
    [SerializeField] protected Character character;
    
    private Animator animator;
    private bool hasWalkingParameter;
    private bool hasAttackTriggerParameter;
    
    private void Awake() {
        animator = GetComponent<Animator>();
        ResolveAnimatorParameters();
    }

    private void OnEnable() {
        if (character == null) {
            character = GetComponentInParent<Character>();
        }

        if (character != null) {
            character.OnAttackPerformed += Character_OnAttackPerformed;
            character.OnDamaged += Character_OnDamaged;
        }
    }

    private void OnDisable() {
        if (character != null) {
            character.OnAttackPerformed -= Character_OnAttackPerformed;
            character.OnDamaged -= Character_OnDamaged;
        }
    }

    private void Update() {
        if (!hasWalkingParameter) return;

        animator.SetBool(CONST.CHAR_VISUAL_IS_WALKING, character.IsWalking() && !character.IsAttacking());
    }

    public void Bind(Character owner) {
        if (character == owner) return;

        if (character != null) {
            character.OnAttackPerformed -= Character_OnAttackPerformed;
            character.OnDamaged -= Character_OnDamaged;
        }

        character = owner;

        if (isActiveAndEnabled && character != null) {
            character.OnAttackPerformed += Character_OnAttackPerformed;
            character.OnDamaged += Character_OnDamaged;
        }
    }

    private void Character_OnAttackPerformed(object sender, System.EventArgs e) {
        if (hasAttackTriggerParameter) {
            animator.SetTrigger(CONST.CHAR_VISUAL_ATTACK_TRIGGER);
        }
    }

    protected virtual void Character_OnDamaged(object sender, Vector3 damageDirection) {
        // DOTween hit effect (scale punch)
        transform.DOComplete(); // Finish current tween if playing
        transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 10, 1f).SetEase(Ease.OutCirc);
    }

    protected void OnDamagedColorFlash(Color color)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) {
            foreach (Material m in r.materials) {
                if (m.HasProperty("_BaseColor")) { // URP
                    m.DOColor(color, "_BaseColor", 0.1f).SetLoops(2, LoopType.Yoyo);
                } else if (m.HasProperty("_Color")) { // Standard
                    m.DOColor(color, "_Color", 0.1f).SetLoops(2, LoopType.Yoyo);
                }
            }
        }
    }
    
    private void ResolveAnimatorParameters() {
        if (animator == null) return;

        foreach (AnimatorControllerParameter parameter in animator.parameters) {
            if (parameter.type == AnimatorControllerParameterType.Bool && 
                parameter.name == CONST.CHAR_VISUAL_IS_WALKING) {
                hasWalkingParameter = true;
            }

            if (parameter.type == AnimatorControllerParameterType.Trigger && 
                parameter.name == CONST.CHAR_VISUAL_ATTACK_TRIGGER) {
                hasAttackTriggerParameter = true;
            }
        }
    }
}
