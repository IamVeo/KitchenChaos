using TMPro;
using UnityEngine;

public class CategoryButtonUI : MonoBehaviour
{
    [SerializeField] private GameObject selectedVisual;
    [SerializeField] private GameObject unselectedVisual;
    [SerializeField] private TextMeshProUGUI categoryNameText;
    
    private ShopItemCategory buttonCategory;
    private RectTransform categoryScrollViewContent;
    
    public void InitCategoryButton(ShopItemCategory category, RectTransform scrollViewContent)
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

        if (categoryScrollViewContent != null)
        {
            categoryScrollViewContent.gameObject.SetActive(selected);
        }
    }
    
    public ShopItemCategory GetButtonCategory()
    {
        return buttonCategory;
    }
    
    public RectTransform GetCategoryScrollViewContent()
    {
        return categoryScrollViewContent;
    }
}
