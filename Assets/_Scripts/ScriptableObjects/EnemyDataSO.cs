using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDataSO", menuName = "ScriptableObjects/EnemyDataSO")]
public class EnemyDataSO : CharacterDataSO
{

    [Header("Rewards")]
    public int killRewardCoin = 5;
    public int killRewardScore = 5;
}