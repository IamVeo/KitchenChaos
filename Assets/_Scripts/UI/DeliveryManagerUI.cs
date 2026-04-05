using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryManagerUI : MonoBehaviour {

    [SerializeField] private Transform container;
    [SerializeField] private Transform recipeTemplate;

    private void Awake() {
        recipeTemplate.gameObject.SetActive(false);
    }

    private void Start() {
        // Lắng nghe sự kiện từ DeliveryManager
        DeliveryManager.Instance.OnRecipeSpawned += DeliveryManager_OnRecipeSpawned;
        DeliveryManager.Instance.OnRecipeCompleted += DeliveryManager_OnRecipeCompleted;

        UpdateVisual();
    }

    private void DeliveryManager_OnRecipeSpawned(object sender, EventArgs e) {
        UpdateVisual();
    }

    private void DeliveryManager_OnRecipeCompleted(object sender, EventArgs e) {
        UpdateVisual();
    }

    private void UpdateVisual() {
        foreach (Transform child in container) {
            if (child == recipeTemplate) continue;
            Destroy(child.gameObject);
        }

        foreach (DeliveryManager.RecipeOrder order in DeliveryManager.Instance.GetWaitingOrderList()) {
            Transform recipeTransform = Instantiate(recipeTemplate, container);
            recipeTransform.gameObject.SetActive(true);

            // Gửi toàn bộ Order sang cho thẻ SingleUI
            recipeTransform.GetComponent<DeliveryManagerSingleUI>().SetRecipeOrder(order);
        }
    }
}