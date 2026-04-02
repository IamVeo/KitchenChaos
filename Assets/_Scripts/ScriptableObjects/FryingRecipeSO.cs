using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FryingRecipeSO", menuName = "ScriptableObjects/FryingRecipeSO")]
public class FryingRecipeSO : ScriptableObject {


    public KitchenObjectSO input;
    public KitchenObjectSO output;
    public float fryingTimerMax;


}