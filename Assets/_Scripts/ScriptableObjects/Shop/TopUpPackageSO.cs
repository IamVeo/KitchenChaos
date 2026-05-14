using UnityEngine;

[CreateAssetMenu(fileName = "TopUpPackageSo", menuName = "ScriptableObjects/Shop/TopUpPackageSo")]
public class TopUpPackageSo : ShopItemSO
{
    [Header("Value")]
    public int coinAmount;
    public long amountVnd;

    [Header("Payment")]
    public string orderInfo = "Nap tien cho game KitchenChaos";
}


