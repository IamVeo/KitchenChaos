using System.Collections.Generic;
using UnityEngine;

public class CategoryVerticalTabsUI : MonoBehaviour
{
    [SerializeField] private Transform categoryButtonBaseUI;
    
    private List<CategoryButtonUI> categoryButtonUIList = new();
    
    private CategoryButtonUI selectedCategoryButtonUI;

    public void InitCategoryTabs(Dictionary<ShopItemCategory, RectTransform> categoryContentDict)
    {
        categoryButtonUIList.Clear();

        foreach (Transform child in transform) {
            if (child != categoryButtonBaseUI) {
                Destroy(child.gameObject);
            }
        }

        categoryButtonBaseUI.gameObject.SetActive(true);
        
        foreach (var (category, contentRectTransform) in categoryContentDict) {
            if (contentRectTransform == null)
            {
                continue;
            }

            Transform categoryButtonTransform = Instantiate(categoryButtonBaseUI, transform);
            CategoryButtonUI categoryButtonUI = categoryButtonTransform.GetComponent<CategoryButtonUI>();
            categoryButtonUIList.Add(categoryButtonUI);
            
            categoryButtonUI.InitCategoryButton(category, contentRectTransform);
        }
        
        categoryButtonBaseUI.gameObject.SetActive(false);
    }
    
    public void SetSelectedCategory(ShopItemCategory category)
    {
        selectedCategoryButtonUI = null;

        foreach (CategoryButtonUI button in categoryButtonUIList)
        {
            if (button.GetButtonCategory() == category)
            {
                button.SetSelected(true);
                selectedCategoryButtonUI = button;
            }
            else button.SetSelected(false);
        }
    }

    public CategoryButtonUI GetSelectedCategoryButtonUI()
    {
        return selectedCategoryButtonUI;
    }
}
