using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(HealthManager))]
public class Enemy : MonoBehaviour, IHasProgress {
    public event EventHandler<IHasProgress.OnProgressChangedEventArgs> OnProgressChanged;

    private enum State {
        WalkingToTable,
        WaitingForFood,
        AttackingPlayer,
        Leaving
    }

    private State currentState;
    private NavMeshAgent navMeshAgent;
    private TableCounter targetTable;
    private Transform targetTableSeat;
    private EnemyDataSO enemyData;

    private float patienceTimer;
    private float attackCooldownTimer;
    private HealthManager healthManager;

    [SerializeField] private RecipeListSO recipeListSO;
    private RecipeSO waitingRecipeSO;

    private void Awake() {
        navMeshAgent = GetComponent<NavMeshAgent>();
        healthManager = GetComponent<HealthManager>();
    }

    public void Setup(TableCounter table, EnemyDataSO data) {
        targetTable = table;
        targetTableSeat = table.GetSeatPoint();
        enemyData = data;

        navMeshAgent.speed = enemyData.moveSpeed;

        healthManager.SetMaxHealth(enemyData.maxHealth);
        healthManager.OnDied += HealthManager_OnDied;

        currentState = State.WalkingToTable;
    }

    private void HealthManager_OnDied(object sender, EventArgs e) {
        if (currentState == State.WalkingToTable || currentState == State.WaitingForFood) {
            EnemySpawnManager.Instance.FreeTable(targetTable);
        }

        healthManager.OnDied -= HealthManager_OnDied;
        Destroy(gameObject);
    }

    private void Update() {
        switch (currentState) {
            case State.WalkingToTable:
                HandleWalkingToTable();
                break;
            case State.WaitingForFood:
                HandleWaitingForFood();
                break;
            case State.AttackingPlayer:
                HandleAttackingPlayer();
                break;
            case State.Leaving:
                break;
        }
    }

    private void HandleWalkingToTable() {
        if (targetTableSeat == null) return;

        navMeshAgent.SetDestination(targetTableSeat.position);

        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance) {
            navMeshAgent.isStopped = true;
            currentState = State.WaitingForFood;
            patienceTimer = enemyData.patienceMax;

            waitingRecipeSO = recipeListSO.recipeSOList[UnityEngine.Random.Range(0, recipeListSO.recipeSOList.Count)];

            targetTable.SeatEnemy(this, waitingRecipeSO);
        }
    }

    private void HandleWaitingForFood() {
        patienceTimer -= Time.deltaTime;
        float patienceNormalized = patienceTimer / enemyData.patienceMax;

        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = patienceNormalized
        });

        if (patienceTimer <= 0f) {
            Enrage();
        }
    }

    private void HandleAttackingPlayer() {
        navMeshAgent.isStopped = false;
        Vector3 playerPosition = Player.Instance.transform.position;
        navMeshAgent.SetDestination(playerPosition);

        if (attackCooldownTimer > 0) {
            attackCooldownTimer -= Time.deltaTime;
        }

        if (Vector3.Distance(transform.position, playerPosition) <= enemyData.attackRange) {
            navMeshAgent.isStopped = true;
            transform.LookAt(playerPosition);

            if (attackCooldownTimer <= 0f) {
                AttackPlayer();
                attackCooldownTimer = enemyData.attackCooldown;
            }
        }
    }

    private void AttackPlayer() {
        if (Player.Instance.TryGetComponent<IDamageable>(out IDamageable playerDamageable)) {
            Vector3 damageDirection = Player.Instance.transform.position - transform.position;
            playerDamageable.TakeDamage(enemyData.attackDamage, damageDirection);
        }
    }

    public void Leave() {
        currentState = State.Leaving;

        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = 0f
        });

        targetTable.ClearTable();
        EnemySpawnManager.Instance.FreeTable(targetTable);
        Destroy(gameObject);
    }

    public void Enrage() {
        currentState = State.AttackingPlayer;

        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = 0f
        });

        targetTable.ClearTable();
        EnemySpawnManager.Instance.FreeTable(targetTable);
    }

    public bool TryDeliverFood(PlateKitchenObject plateKitchenObject) {
        if (currentState != State.WaitingForFood) return false;
        
        if(DeliveryManager.Instance.IsRecipeMatching(plateKitchenObject, waitingRecipeSO)) {
            DeliveryManager.Instance.AddSuccessfulDelivery();
            targetTable.ClearTable();

            Leave();
            return true;
        } else {
            DeliveryManager.Instance.AddFailedDelivery();

            Enrage();
            return false;
        }
    }

    public bool IsWaitingForFood() {
        return currentState == State.WaitingForFood;
    }
}