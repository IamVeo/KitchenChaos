using UnityEngine;

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
        }
    }

    private void OnDisable() {
        if (character != null) {
            character.OnAttackPerformed -= Character_OnAttackPerformed;
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
        }

        character = owner;

        if (isActiveAndEnabled && character != null) {
            character.OnAttackPerformed += Character_OnAttackPerformed;
        }
    }

    private void Character_OnAttackPerformed(object sender, System.EventArgs e) {
        if (hasAttackTriggerParameter) {
            animator.SetTrigger(CONST.CHAR_VISUAL_ATTACK_TRIGGER);
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
