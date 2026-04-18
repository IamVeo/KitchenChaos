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
        EnsureScrollRect();

        if (contentTransformBase != null)
        {
            contentTransformBase.gameObject.SetActive(false);
        }

        if (shopSlotUIBase != null)
        {
            shopSlotUIBase.gameObject.SetActive(false);
        }
    }
    
    public Transform SetUpContent(List<ShopItemSO> shopItemList)
    {
        Transform contentTransform = CreateContentRoot();
        if (contentTransform == null)
        {
            return null;
        }

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
        if (contentTransform == null)
        {
            return null;
        }

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
        if (viewport == null || contentTransformBase == null)
        {
            Debug.LogError("[ShopSlotScrollViewUI] Missing viewport/contentTransformBase reference.");
            return null;
        }

        Transform contentTransform = Instantiate(contentTransformBase, viewport);
        contentTransform.gameObject.SetActive(true);

        // Ensure placeholder/template children from the base content are hidden in every clone.
        foreach (Transform child in contentTransform)
        {
            child.gameObject.SetActive(false);
        }

        return contentTransform;
    }
    
    public void SetScrollViewContent(RectTransform contentRectTransform)
    {
        EnsureScrollRect();
        if (scrollRect == null)
        {
            Debug.LogError("[ShopSlotScrollViewUI] Missing ScrollRect component.");
            return;
        }

        scrollRect.content = contentRectTransform;
    }

    public void ClearGeneratedContents()
    {
        EnsureScrollRect();

        if (scrollRect != null)
        {
            scrollRect.content = null;
        }

        if (viewport == null)
        {
            return;
        }

        foreach (Transform child in viewport)
        {
            if (child == contentTransformBase)
            {
                continue;
            }

            Destroy(child.gameObject);
        }
    }

    private void EnsureScrollRect()
    {
        if (scrollRect == null)
        {
            scrollRect = GetComponent<ScrollRect>();
        }
    }
}
