using UnityEngine;

[CreateAssetMenu(fileName = "PlayerDataSO", menuName = "ScriptableObjects/PlayerDataSO")]
public class PlayerDataSO : CharacterDataSO
{
    [Header("Body")]
    public float playerHeight;
    public float playerRadius;
    
    [Header("Interactions")]
    public float interactDistance;
}