using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopItemListSO", menuName = "ScriptableObjects/ShopItemListSO")]
public class ShopItemListSO : ScriptableObject
{
    public List<ShopItemSO> shopItemSOList;
}
