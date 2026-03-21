using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RecipeSO", menuName = "ScriptableObjects/RecipeSO")]
public class RecipeSO : ScriptableObject {


    public List<KitchenObjectSO> kitchenObjectSOList;
    public string recipeName;


}