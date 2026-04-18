using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(HealthManager))]
public class Enemy : Character, IHasProgress {
    public event EventHandler<IHasProgress.OnProgressChangedEventArgs> OnProgressChanged;

    private EnemyState currentEnemyState;
    private NavMeshAgent navMeshAgent;
    private NavMeshObstacle navMeshObstacle;
    private TableCounter targetTable;
    private Transform targetTableSeat;
    private EnemyDataSO EnemyData => DataAs<EnemyDataSO>();
    
    private float patienceTimer;
    private float attackCooldownTimer;

    [SerializeField] private RecipeListSO recipeListSO;
    private RecipeSO waitingRecipeSO;
    
    private float knockbackDuration = 0.4f;
    private float knockbackForce = 10f;
    private float knockbackDrag = 3.5f;

    protected override void Awake() {
        base.Awake();
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    public bool IsAttackingPlayer() => currentEnemyState == EnemyState.AttackingPlayer;

    public void Setup(TableCounter table) {
        targetTable = table;
        targetTableSeat = table.GetSeatPoint();

        navMeshAgent.speed = MoveSpeed;
        navMeshAgent.stoppingDistance = 0f;
        
        healthManager.OnDied += HealthManager_OnDied;

        currentEnemyState = EnemyState.WalkingToTable;
    }

    private void HealthManager_OnDied(object sender, EventArgs e) {
        if (currentEnemyState == EnemyState.WalkingToTable || currentEnemyState == EnemyState.WaitingForFood) {
            EnemySpawnManager.Instance.FreeTable(targetTable);
        }

        healthManager.OnDied -= HealthManager_OnDied;
        Destroy(gameObject);
    }

    private void Update() {
        if (!navMeshAgent.enabled && currentEnemyState != EnemyState.WaitingForFood) return;

        switch (currentEnemyState) {
            case EnemyState.WalkingToTable:
                HandleWalkingToTable();
                break;
            case EnemyState.WaitingForFood:
                HandleWaitingForFood();
                break;
            case EnemyState.AttackingPlayer:
                HandleAttackingPlayer();
                break;
            case EnemyState.Leaving:
                break;
        }
    }

    private void HandleWalkingToTable() {
        if (targetTableSeat == null) return;

        navMeshAgent.SetDestination(targetTableSeat.position);

        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance) {
            navMeshAgent.isStopped = true;
            navMeshAgent.enabled = false;

            transform.position = targetTableSeat.position;
            transform.rotation = targetTableSeat.rotation;

            if (navMeshObstacle == null) {
                navMeshObstacle = gameObject.AddComponent<NavMeshObstacle>();
                navMeshObstacle.shape = NavMeshObstacleShape.Box;
                navMeshObstacle.carving = true;
            } else {
                navMeshObstacle.enabled = true;
            }
            
            rb.constraints = RigidbodyConstraints.FreezeAll;


            currentEnemyState = EnemyState.WaitingForFood;
            patienceTimer = EnemyData.patienceMax;

            waitingRecipeSO = recipeListSO.recipeSOList[UnityEngine.Random.Range(0, recipeListSO.recipeSOList.Count)];
            DeliveryManager.Instance.AddWaitingRecipe(this, waitingRecipeSO);

            targetTable.SeatEnemy(this, waitingRecipeSO);
        }
    }

    private void HandleWaitingForFood() {
        patienceTimer -= Time.deltaTime;
        float patienceNormalized = patienceTimer / EnemyData.patienceMax;

        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = patienceNormalized
        });

        if (patienceTimer <= 0f) {
            Enrage();
        }
    }

    private void HandleAttackingPlayer() {
        navMeshAgent.isStopped = false;
        navMeshAgent.stoppingDistance = 0.8f;
        Vector3 playerPosition = Player.Instance.transform.position;
        navMeshAgent.SetDestination(playerPosition);

        if (attackCooldownTimer > 0) {
            attackCooldownTimer -= Time.deltaTime;
        }

        if (Vector3.Distance(transform.position, playerPosition) <= AttackRange) {
            navMeshAgent.isStopped = true;

            Vector3 lookAtPosition = playerPosition;
            lookAtPosition.y = transform.position.y; 
            transform.LookAt(lookAtPosition);

            if (attackCooldownTimer <= 0f) {
                AttackPlayer();
                attackCooldownTimer = AttackCooldown;
            }
        }
    }

    private void AttackPlayer() {
        RaiseAttackPerformed();
        Vector3 damageDirection = Player.Instance.transform.position - transform.position;
        Player.Instance.GetHealthManager().TakeDamage(AttackDamage, damageDirection);
    }

    public void Leave() {
        currentEnemyState = EnemyState.Leaving;

        if (navMeshObstacle != null) {
            navMeshObstacle.enabled = false;
        }

        navMeshAgent.enabled = true;
        
        rb.constraints = RigidbodyConstraints.FreezeRotation;


        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = 0f
        });

        DeliveryManager.Instance.RemoveWaitingRecipe(this);
        targetTable.ClearTable();
        EnemySpawnManager.Instance.FreeTable(targetTable);
        Destroy(gameObject);
    }

    public void Enrage() {
        currentEnemyState = EnemyState.AttackingPlayer;
        if (navMeshObstacle != null) {
            navMeshObstacle.enabled = false;
        }
        navMeshAgent.enabled = true;
        navMeshAgent.stoppingDistance = 0.8f; // Trả lại phanh 0.8 mét

        rb.constraints = RigidbodyConstraints.FreezeRotation;

        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = 0f
        });


        DeliveryManager.Instance.RemoveWaitingRecipe(this);
        targetTable.ClearTable();
        EnemySpawnManager.Instance.FreeTable(targetTable);
    }

    public bool TryDeliverFood(PlateKitchenObject plateKitchenObject) {
        if (currentEnemyState != EnemyState.WaitingForFood) return false;
        
        if(DeliveryManager.Instance.IsRecipeMatching(plateKitchenObject, waitingRecipeSO)) {
            DeliveryManager.Instance.AddSuccessfulDelivery();
            DeliveryManager.Instance.RemoveWaitingRecipe(this); // XÓA ORDER
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
        return currentEnemyState == EnemyState.WaitingForFood;
    }

    public override bool IsWalking() {
        if (currentEnemyState == EnemyState.WalkingToTable || currentEnemyState == EnemyState.AttackingPlayer) {
            return navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.velocity.sqrMagnitude > 0.01f;
        }

        return false;
    }

    public override bool IsAttacking() => currentEnemyState == EnemyState.AttackingPlayer;

    protected override void ReceiveKnockback(Vector3 knockbackDir) {
        // Chạy luồng xử lý thời gian độc lập
        StartCoroutine(KnockbackRoutine(knockbackDir));
    }

    private IEnumerator KnockbackRoutine(Vector3 knockbackDir) {
        
        navMeshAgent.enabled = false;
        rb.isKinematic = false;
        rb.velocity = Vector3.zero;
        rb.drag = knockbackDrag;
        rb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);

        yield return new WaitForSeconds(knockbackDuration);
        
        rb.isKinematic = true; 
        
        rb.drag = 0f;

        if (this != null) {
            navMeshAgent.enabled = true;
            if (navMeshAgent.isOnNavMesh) {
                navMeshAgent.ResetPath();
            }
        }
    }
}