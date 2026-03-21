using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawnManager : MonoBehaviour {

    public static MonsterSpawnManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private List<EnemyDataSO> enemyList;
    [SerializeField] private List<Transform> spawnPointList;
    [SerializeField] private List<TableCounter> tableCounterList;

    [Header("Settings")]
    [SerializeField] private float spawnTimerMax = 10f;
    [SerializeField] private int maxMonsterCount = 4;

    private float spawnTimer;
    private int currentEnemyCount;

    private List<TableCounter> availableTableList;

    private void Awake() {
        Instance = this;

        availableTableList = new List<TableCounter>(tableCounterList);
    }

    private void Start() {
        spawnTimer = spawnTimerMax;
    }

    private void Update() {
        if(!KitchenGameManager.Instance.IsGamePlaying()) {
            return;
        }

        spawnTimer -= Time.deltaTime;
        if(spawnTimer <= 0f) {
            spawnTimer = spawnTimerMax;
            if (currentEnemyCount < maxMonsterCount && availableTableList.Count > 0) {
                SpawnMonster();
            }
        }
    }

    private void SpawnMonster() {
        Transform spawnPointTransform = spawnPointList[Random.Range(0, spawnPointList.Count)];
        
        int tableIndex = Random.Range(0, availableTableList.Count);
        TableCounter chosenTable = availableTableList[tableIndex];

        availableTableList.RemoveAt(tableIndex);

        EnemyDataSO chosenEnemyData = enemyList[Random.Range(0, enemyList.Count)];

        Transform enemyTransform = Instantiate(chosenEnemyData.prefab, spawnPointTransform.position, spawnPointTransform.rotation);

        Enemy spawnedEnemy = enemyTransform.GetComponent<Enemy>();

        spawnedEnemy.Setup(chosenTable, chosenEnemyData);

        currentEnemyCount++;
    }

    public void FreeTable(TableCounter freeTable) {
        if (!availableTableList.Contains(freeTable)) {
            availableTableList.Add(freeTable);
            currentEnemyCount--;
        }
    }
}