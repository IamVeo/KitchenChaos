using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDataSO", menuName = "ScriptableObjects/EnemyDataSO")]
public class EnemyDataSO : CharacterDataSO
{
    [Header("Mechanics")] 
    public float patienceMax;
}