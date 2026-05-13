using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerSlash : MonoBehaviour
{
    [SerializeField] private Player player;
    private float swingDir;

    private void Start()
    {
        swingDir = 1f;
        gameObject.SetActive(false);
        player.OnAttackPerformed += Player_OnAttackPerformed;
    }
    
    private void Player_OnAttackPerformed(object sender, EventArgs e)
    {
        // Kill any active tweens on this transform to avoid overlapping animations
        transform.DOKill();
        
        // Enable the weapon for the swing
        gameObject.SetActive(true);
        
        // Randomize swing direction (1 for right-to-left, -1 for left-to-right)
        swingDir *= -1;
        
        // Large starting and ending angles 
        float startAngle = -89f * swingDir;
        float endAngle = 89f * swingDir;

        // Snap to the starting side of the swing (Y-axis for horizontal swing)
        transform.localRotation = Quaternion.Euler(0, startAngle, 0);

        Sequence attackSequence = DOTween.Sequence();
        
        // Fast, broad slash to the opposite side
        attackSequence.Append(transform.DOLocalRotate(new Vector3(0, endAngle, 0), 0.12f).SetEase(Ease.OutBack));
        
        // Quick return towards the center before disappearing
        attackSequence.Append(transform.DOLocalRotate(Vector3.zero, 0.05f).SetEase(Ease.InQuad));
        
        // Disable the weapon when the animation is completely finished
        attackSequence.OnComplete(() => 
        {
            gameObject.SetActive(false);
        });
    }
}
