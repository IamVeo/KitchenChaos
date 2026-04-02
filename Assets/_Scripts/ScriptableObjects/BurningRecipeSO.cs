using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BurningRecipeSO", menuName = "ScriptableObjects/BurningRecipeSO")]
public class BurningRecipeSO : ScriptableObject {


    public KitchenObjectSO input;
    public KitchenObjectSO output;
    public float burningTimerMax;


}