using UnityEngine;

/// <summary>
/// Spawn danh sach package nap coin tu TopUpPackageListSO.
/// Mỗi item dùng prefab có component VnPayShopPurchaseButton.
/// </summary>
public class TopUpPackageListUI : MonoBehaviour
{
    [SerializeField] private TopUpPackageListSo packageListSo;
    [SerializeField] private Transform itemContainer;
    [SerializeField] private VnPayShopPurchaseButton packageButtonPrefab;

    private void Awake()
    {
        if (itemContainer == null)
        {
            itemContainer = transform;
        }
    }

    private void Start()
    {
        Build();
    }

    public void Build()
    {
        if (packageListSo == null)
        {
            Debug.LogWarning("[TopUpPackageListUI] packageListSo is not assigned.");
            return;
        }

        if (packageButtonPrefab == null)
        {
            Debug.LogWarning("[TopUpPackageListUI] packageButtonPrefab is not assigned.");
            return;
        }

        ClearContainer();

        if (packageListSo.packageList == null)
        {
            return;
        }

        foreach (TopUpPackageSo packageData in packageListSo.packageList)
        {
            if (packageData == null)
            {
                continue;
            }

            VnPayShopPurchaseButton buttonInstance = Instantiate(packageButtonPrefab, itemContainer);
            buttonInstance.gameObject.SetActive(true);
            buttonInstance.BindPackage(packageData);
        }
    }

    private void ClearContainer()
    {
        for (int i = itemContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(itemContainer.GetChild(i).gameObject);
        }
    }
}


