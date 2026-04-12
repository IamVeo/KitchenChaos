using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterVisual : MonoBehaviour
{
    [SerializeField] protected Character character;
    
    private Animator animator;
    
    private void Awake() {
        animator = GetComponent<Animator>();
    }
    
    //
    // private void Start()
    // {
    //     player.OnAttacking += Player_OnAttacking;
    // }
    //
    // private void Update() {
    //     animator.SetBool(IS_WALKING, player.IsWalking() && !player.IsAttacking());
    // }
    //
    // private void Player_OnAttacking(object sender, EventArgs e)
    // {
    //     // animator.SetTrigger("Attack");
    // }
}
