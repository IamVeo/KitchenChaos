using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private ShopUI shopUI;

    private Dictionary<ShopItemCategory, List<ShopItemSO>> shopItems = new();
    
}
