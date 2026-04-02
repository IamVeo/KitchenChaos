using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button purchaseButton;
    
    private ShopItemSO shopItemSO;

    public void InitSlot(ShopItemSO item)
    {
        shopItemSO = item;
        itemNameText.text = item.itemName;
        priceText.text = item.price.ToString();
        iconImage.sprite = item.icon;
        
        purchaseButton.onClick.RemoveAllListeners();
        purchaseButton.onClick.AddListener(OnPurchaseButtonClicked);
    }
    
    private void OnPurchaseButtonClicked()
    {
        ShopManager.Instance.AttemptPurchase(shopItemSO.id);
    }
}
