using UnityEngine;

[CreateAssetMenu(fileName = "TopUpPackageSo", menuName = "ScriptableObjects/Shop/TopUpPackageSo")]
public class TopUpPackageSo : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;

    [Header("Value")]
    public int coinAmount;
    public long amountVnd;

    [Header("Payment")]
    public string orderInfo = "Nap tien cho game KitchenChaos";

    [Header("Optional UI")]
    public Sprite icon;
}


