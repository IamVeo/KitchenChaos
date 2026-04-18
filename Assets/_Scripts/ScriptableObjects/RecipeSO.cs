using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RecipeSO", menuName = "ScriptableObjects/RecipeSO")]
public class RecipeSO : ShopItemSO {


    public List<KitchenObjectSO> kitchenObjectSOList;
    public string recipeName;

    [Header("Gameplay")]
    public int deliveryRewardCoin = 10;
    public int deliveryRewardScore = 10;

    [Tooltip("Required: must be > 0")]
    public float orderPatienceSeconds;
}