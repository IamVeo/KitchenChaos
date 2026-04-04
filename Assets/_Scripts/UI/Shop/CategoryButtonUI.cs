using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CategoryButtonUI : MonoBehaviour
{
    [SerializeField] private GameObject selectedVisual;
    [SerializeField] private GameObject unselectedVisual;
    [SerializeField] private TextMeshProUGUI categoryNameText;
    
    private ShopItemCategory buttonCategory;
    private Transform categoryScrollViewContent;
    
    public void InitCategoryButton(ShopItemCategory category, Transform scrollViewContent)
    {
        categoryNameText.text = category.ToString();
        buttonCategory = category;
        selectedVisual.SetActive(false);
        unselectedVisual.SetActive(true);
        categoryScrollViewContent = scrollViewContent;
    }
    
    public void SetSelected(bool selected)
    {
        selectedVisual.SetActive(selected);
        unselectedVisual.SetActive(!selected);
        categoryScrollViewContent.gameObject.SetActive(selected);
    }
    
    public ShopItemCategory GetButtonCategory()
    {
        return buttonCategory;
    }
    
    public RectTransform GetCategoryScrollViewContent()
    {
        return categoryScrollViewContent.GetComponent<RectTransform>();
    }
}
