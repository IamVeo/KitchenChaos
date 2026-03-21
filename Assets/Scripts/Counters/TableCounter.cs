using System;
using UnityEngine;

public class TableCounter : BaseCounter {

    public event EventHandler<OnOrderPlacedEventArgs> OnOrderPlaced;
    public class OnOrderPlacedEventArgs : EventArgs {
        public RecipeSO recipeSO;
    }
    public event EventHandler OnOrderCompleted;

    [SerializeField] private Transform monsterSeatPoint;
    private MonsterAI currentMonster;
    private RecipeSO waitingRecipeSO;

    public bool isOccupied() => currentMonster != null;

    public Transform getMonsterSeatPoint() => monsterSeatPoint;

    public void SeatMonster(MonsterAI monster, RecipeSO recipeSO) {
        currentMonster = monster;
        waitingRecipeSO = recipeSO;

        OnOrderPlaced?.Invoke(this, new OnOrderPlacedEventArgs {
            recipeSO = waitingRecipeSO,
        });
    }

    public override void Interact(Player player) {
        if(!isOccupied()) {
            return;
        }
        
        if (player.HasKitchenObject()) {
            if(player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject)) {
                player.GetKitchenObject().DestroySelf();
            }
        }
    }
}
