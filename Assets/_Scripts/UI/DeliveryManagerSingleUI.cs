using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DeliveryManagerSingleUI : MonoBehaviour {

    [SerializeField] private TextMeshProUGUI recipeNameText;
    [SerializeField] private Transform iconContainer;
    [SerializeField] private Transform iconTemplate;
    [SerializeField] private Image backgroundImage;

    private Enemy currentEnemy;

    private void Awake() {
        iconTemplate.gameObject.SetActive(false);
    }

    public void SetRecipeOrder(DeliveryManager.RecipeOrder order) {
        RecipeSO recipeSO = order.recipeSO;
        currentEnemy = order.enemy;

        recipeNameText.text = recipeSO.recipeName;

        foreach (Transform child in iconContainer) {
            if (child == iconTemplate) continue;
            Destroy(child.gameObject);
        }

        foreach (KitchenObjectSO kitchenObjectSO in recipeSO.kitchenObjectSOList) {
            Transform iconTransform = Instantiate(iconTemplate, iconContainer);
            iconTransform.gameObject.SetActive(true);
            iconTransform.GetComponent<Image>().sprite = kitchenObjectSO.sprite;
        }

        if (currentEnemy != null) {
            currentEnemy.OnProgressChanged += Enemy_OnProgressChanged;

            backgroundImage.fillAmount = 1f;
            backgroundImage.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);
        }
    }

    private void Enemy_OnProgressChanged(object sender, IHasProgress.OnProgressChangedEventArgs e) {
        backgroundImage.fillAmount = e.progressNormalized;

        if (e.progressNormalized > 0.6f) {
            backgroundImage.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);
        } else if (e.progressNormalized > 0.3f) {
            backgroundImage.color = new Color(0.8f, 0.8f, 0.2f, 0.8f);
        } else {
            backgroundImage.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);
        }
    }

    private void OnDestroy() {
        if (currentEnemy != null) {
            currentEnemy.OnProgressChanged -= Enemy_OnProgressChanged;
        }
    }
}