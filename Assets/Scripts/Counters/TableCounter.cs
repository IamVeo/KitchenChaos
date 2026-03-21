using System;
using UnityEngine;

public class TableCounter : BaseCounter {
    public event EventHandler<OnOrderPlacedEventArgs> OnOrderPlaced;
    public class OnOrderPlacedEventArgs : EventArgs {
        public RecipeSO recipeSO;
    }
    public event EventHandler OnOrderCompleted;

    [SerializeField] private Transform enemySeatPoint;

    private Enemy currentEnemy;
    private RecipeSO waitingRecipeSO;

    public bool IsOccupied() => currentEnemy != null;

    public Transform GetSeatPoint() => enemySeatPoint;

    public void SeatEnemy(Enemy enemy, RecipeSO recipeSO) {
        currentEnemy = enemy;
        waitingRecipeSO = recipeSO;

        OnOrderPlaced?.Invoke(this, new OnOrderPlacedEventArgs {
            recipeSO = waitingRecipeSO,
        });
    }

    public override void Interact(Player player) {
        if (!IsOccupied()) {
            return;
        }

        if (!currentEnemy.IsWaitingForFood()) {
            return;
        }

        if (player.HasKitchenObject()) {
            if (player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject)) {

                if (DeliveryManager.Instance.IsRecipeMatching(plateKitchenObject, waitingRecipeSO)) {
                    currentEnemy.Leave();
                } else {
                    currentEnemy.Enrage();
                }

                player.GetKitchenObject().DestroySelf();
                ClearTable();
            }
        }
    }

    public void ClearTable() {
        currentEnemy = null;
        waitingRecipeSO = null;

        OnOrderCompleted?.Invoke(this, EventArgs.Empty);
    }
}