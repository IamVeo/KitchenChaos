using System;
using UnityEngine;
using UnityEngine.AI;

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
    private EnemyDataSO monsterData;

    private float patienceTimer;
    private float attackCooldownTimer;
    
    private void Awake() {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    public void Setup(TableCounter table, EnemyDataSO data) {
        targetTable = table;
        targetTableSeat = table.getSeatPoint();
        monsterData = data;

        navMeshAgent.speed = monsterData.moveSpeed;

        currentState = State.WalkingToTable;
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
            patienceTimer = monsterData.patienceMax;
        }
    }

    private void HandleWaitingForFood() {
        patienceTimer -= Time.deltaTime;
        float patienceNormalized = patienceTimer / monsterData.patienceMax;

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

        if (Vector3.Distance(transform.position, playerPosition) <= monsterData.attackRange) {
            navMeshAgent.isStopped = true;
            transform.LookAt(playerPosition);

            if (attackCooldownTimer <= 0f) {
                AttackPlayer();
                attackCooldownTimer = monsterData.attackCooldown;
            }
        }
    }

    private void AttackPlayer() {
        if (Player.Instance.TryGetComponent<IDamageable>(out IDamageable playerDamageable)) {
            Vector3 damageDirection = Player.Instance.transform.position - transform.position;
            playerDamageable.TakeDamage(monsterData.attackDamage, damageDirection);
        }
    }

    public void Leave() {
        currentState = State.Leaving;

        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = 0f
        });

        MonsterSpawnManager.Instance.FreeTable(targetTable);
        Destroy(gameObject);
    }

    public void Enrage() {
        currentState = State.AttackingPlayer;

        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = 0f
        });

        MonsterSpawnManager.Instance.FreeTable(targetTable);
    }

    public bool IsWaitingForFood() {
        return currentState == State.WaitingForFood;
    }
}