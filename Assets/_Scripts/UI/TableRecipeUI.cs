using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TableRecipeUI : MonoBehaviour {

    [SerializeField] private TableCounter tableCounter;
    [SerializeField] private GameObject containerGO;
    [SerializeField] private Transform iconContainer;
    [SerializeField] private Transform iconTemplate;

    private void Start() {
        iconTemplate.gameObject.SetActive(false);

        tableCounter.OnOrderPlaced += TableCounter_OnOrderPlaced;
        tableCounter.OnOrderCompleted += TableCounter_OnOrderCompleted;
    }

    private void TableCounter_OnOrderPlaced(object sender, TableCounter.OnOrderPlacedEventArgs e) {
        Show(e.recipeSO);
    }

    private void TableCounter_OnOrderCompleted(object sender, System.EventArgs e) {
        Hide();
    }

    public void Show(RecipeSO recipeSO) {
        containerGO.SetActive(true);

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
    }

    private void OnDestroy() {
        tableCounter.OnOrderPlaced -= TableCounter_OnOrderPlaced;
        tableCounter.OnOrderCompleted -= TableCounter_OnOrderCompleted;
    }
}
