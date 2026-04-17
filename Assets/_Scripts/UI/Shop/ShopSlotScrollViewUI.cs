using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopSlotScrollViewUI : MonoBehaviour
{
    [SerializeField] private Transform viewport;
    [SerializeField] private Transform contentTransformBase;
    [SerializeField] private ShopSlotUI shopSlotUIBase;
    [SerializeField] private TMP_FontAsset messageFont;

    private ScrollRect scrollRect;
    
    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        
        contentTransformBase.gameObject.SetActive(false);
        shopSlotUIBase.gameObject.SetActive(false);
    }
    
    public Transform SetUpContent(List<ShopItemSO> shopItemList)
    {
        Transform contentTransform = CreateContentRoot();

        foreach (ShopItemSO shopItem in shopItemList)
        {
            ShopSlotUI shopSlotUI = Instantiate(shopSlotUIBase, contentTransform);
            shopSlotUI.gameObject.SetActive(true);
            shopSlotUI.InitSlot(shopItem);
        }
        
        return contentTransform;
    }

    public RectTransform SetUpCoinContent(List<TopUpPackageSo> packageList)
    {
        Transform contentTransform = CreateContentRoot();

        if (packageList == null)
        {
            return contentTransform.GetComponent<RectTransform>();
        }

        foreach (TopUpPackageSo packageData in packageList)
        {
            if (packageData == null)
            {
                continue;
            }

            ShopSlotUI coinSlotUI = Instantiate(shopSlotUIBase, contentTransform);
            coinSlotUI.gameObject.SetActive(true);
            coinSlotUI.InitTopUpSlot(packageData);
        }

        return contentTransform.GetComponent<RectTransform>();
    }

    public RectTransform SetUpCenteredMessageContent(string message)
    {
        GameObject contentGameObject = new GameObject("MessageContent", typeof(RectTransform));
        contentGameObject.transform.SetParent(viewport, false);

        RectTransform contentRectTransform = contentGameObject.GetComponent<RectTransform>();
        contentRectTransform.anchorMin = new Vector2(0f, 0f);
        contentRectTransform.anchorMax = new Vector2(1f, 1f);
        contentRectTransform.offsetMin = Vector2.zero;
        contentRectTransform.offsetMax = Vector2.zero;

        GameObject textGameObject = new GameObject("MessageText", typeof(RectTransform));
        textGameObject.transform.SetParent(contentRectTransform, false);

        RectTransform textRectTransform = textGameObject.GetComponent<RectTransform>();
        textRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        textRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        textRectTransform.pivot = new Vector2(0.5f, 0.5f);
        textRectTransform.anchoredPosition = Vector2.zero;
        textRectTransform.sizeDelta = new Vector2(700f, 220f);

        TextMeshProUGUI messageText = textGameObject.AddComponent<TextMeshProUGUI>();
        messageText.text = message;
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.fontSize = 42f;
        messageText.enableWordWrapping = true;
        messageText.color = Color.white;

        if (messageFont != null)
        {
            messageText.font = messageFont;
        }

        return contentRectTransform;
    }

    private Transform CreateContentRoot()
    {
        Transform contentTransform = Instantiate(contentTransformBase, viewport);
        contentTransform.gameObject.SetActive(true);
        return contentTransform;
    }
    
    public void SetScrollViewContent(RectTransform contentRectTransform)
    {
        scrollRect.content = contentRectTransform;
    }
}
