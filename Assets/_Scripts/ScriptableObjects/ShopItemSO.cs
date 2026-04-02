using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopItemSO", menuName = "ScriptableObjects/ShopItemSO")]
public class ShopItemSO : ScriptableObject
{
    public string id;
    public string itemName;
    public ShopItemCategory category;
    public int price;
    public Sprite icon;
}
