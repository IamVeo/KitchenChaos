using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopSlotScrollViewUI : MonoBehaviour
{
    [SerializeField] private Transform viewport;
    [SerializeField] private Transform contentTransformBase;
    [SerializeField] private ShopSlotUI shopSlotUIBase;

    private ScrollRect scrollRect;
    
    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        
        contentTransformBase.gameObject.SetActive(false);
        shopSlotUIBase.gameObject.SetActive(false);
    }
    
    public Transform SetUpContent(List<ShopItemSO> shopItemList)
    {
        Transform contentTransform = Instantiate(contentTransformBase, viewport);
        contentTransform.gameObject.SetActive(true);

        foreach (ShopItemSO shopItem in shopItemList)
        {
            ShopSlotUI shopSlotUI = Instantiate(shopSlotUIBase, contentTransform);
            shopSlotUI.gameObject.SetActive(true);
            shopSlotUI.InitSlot(shopItem);
        }
        
        return contentTransform;
    }
    
    public void SetScrollViewContent(RectTransform contentRectTransform)
    {
        scrollRect.content = contentRectTransform;
    }
}
