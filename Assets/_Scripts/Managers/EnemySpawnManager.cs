using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnManager : MonoBehaviour {
    public static EnemySpawnManager Instance { get; private set; }

    [SerializeField] private List<Transform> enemyPrefabList;
    [SerializeField] private List<Transform> spawnPointList;
    [SerializeField] private List<TableCounter> tableCounterList;

    [SerializeField] private float spawnTimerMax = 15f;
    [SerializeField] private int maxEnemies = 4;

    private float spawnTimer;
    private int currentEnemyCount = 0;
    private List<TableCounter> availableTableList;

    private void Awake() {
        Instance = this;
        availableTableList = new List<TableCounter>(tableCounterList);
    }

    private void Start() {
        spawnTimer = spawnTimerMax;
    }
    private void Update() {
        if (KitchenGameManager.Instance != null && !KitchenGameManager.Instance.IsGamePlaying()) {
            return;
        }

        if (currentEnemyCount < maxEnemies && availableTableList.Count > 0) {

            spawnTimer -= Time.deltaTime;

            if (spawnTimer <= 0f) {
                spawnTimer = spawnTimerMax;
                SpawnEnemy();
            }
        }
    }

    private void SpawnEnemy() {
        Transform spawnPoint = spawnPointList[Random.Range(0, spawnPointList.Count)];
        int tableIndex = Random.Range(0, availableTableList.Count);
        TableCounter chosenTable = availableTableList[tableIndex];

        availableTableList.RemoveAt(tableIndex);

        Transform enemyPrefab = enemyPrefabList[Random.Range(0, enemyPrefabList.Count)];
        Transform enemyTransform = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);

        Enemy enemy = enemyTransform.GetComponent<Enemy>();
        enemy.Setup(chosenTable);

        currentEnemyCount++;
    }

    public void FreeTable(TableCounter tableToFree) {
        if (!availableTableList.Contains(tableToFree)) {
            availableTableList.Add(tableToFree);
            tableToFree.ClearTable();
            currentEnemyCount--;
        }
    }
}