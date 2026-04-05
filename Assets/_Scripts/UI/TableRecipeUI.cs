using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TableRecipeUI : MonoBehaviour {

    [SerializeField] private TableCounter tableCounter;
    [SerializeField] private GameObject containerGO;
    [SerializeField] private Transform iconContainer;
    [SerializeField] private Transform iconTemplate;
    [SerializeField] private Image backgroundImage;
    private Enemy currentEnemy;

    private void Start() {
        iconTemplate.gameObject.SetActive(false);

        Hide();

        tableCounter.OnOrderPlaced += TableCounter_OnOrderPlaced;
        tableCounter.OnOrderCompleted += TableCounter_OnOrderCompleted;
    }

    private void TableCounter_OnOrderPlaced(object sender, TableCounter.OnOrderPlacedEventArgs e) {
        Show(e.recipeSO);

        if (currentEnemy != null) {
            currentEnemy.OnProgressChanged -= Enemy_OnProgressChanged;
        }

        currentEnemy = e.enemy;
        if (currentEnemy != null) {
            currentEnemy.OnProgressChanged += Enemy_OnProgressChanged;

            backgroundImage.fillAmount = 1f;
            backgroundImage.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);
        }
    }

    private void TableCounter_OnOrderCompleted(object sender, System.EventArgs e) {
        Hide();

        if (currentEnemy != null) {
            currentEnemy.OnProgressChanged -= Enemy_OnProgressChanged;
            currentEnemy = null;
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

    public void Show(RecipeSO recipeSO) {
        containerGO.SetActive(true);

        backgroundImage.gameObject.SetActive(true);

        foreach (Transform child in iconContainer) {
            if(child == iconTemplate) continue;
            Destroy(child.gameObject);
        }

        foreach(KitchenObjectSO kitchenObjectSO in recipeSO.kitchenObjectSOList) {
            Transform iconTransform = Instantiate(iconTemplate, iconContainer);
            iconTransform.gameObject.SetActive(true);
            iconTransform.GetComponent<Image>().sprite = kitchenObjectSO.sprite;
        }

    }

    public void Hide() {
        containerGO.SetActive(false);

        backgroundImage.gameObject.SetActive(false);
    }

    private void OnDestroy() {
        tableCounter.OnOrderPlaced -= TableCounter_OnOrderPlaced;
        tableCounter.OnOrderCompleted -= TableCounter_OnOrderCompleted;

        if (currentEnemy != null) {
            currentEnemy.OnProgressChanged -= Enemy_OnProgressChanged;
        }
    }
}
