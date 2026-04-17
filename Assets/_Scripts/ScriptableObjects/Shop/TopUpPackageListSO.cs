using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TopUpPackageListSo", menuName = "ScriptableObjects/Shop/TopUpPackageListSo")]
public class TopUpPackageListSo : ScriptableObject
{
    public List<TopUpPackageSo> packageList;
}


