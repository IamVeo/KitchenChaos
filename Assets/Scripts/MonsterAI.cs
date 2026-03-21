using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MonsterAI : MonoBehaviour, IHasProgress {

    public event EventHandler<IHasProgress.OnProgressChangedEventArgs> OnProgressChanged;

    private enum State {
        WalkingToTable,
        WaitingForFood,
        AttackingPlayer,
        Leaving
    }
    private State currentState;

    private NavMeshAgent navMeshAgent;
    private Transform targetTransform;

    [SerializeField] private float patienceMax = 20f;
    private float patienceTimer;



    private void Awake() {
        navMeshAgent = GetComponent<NavMeshAgent>();
        currentState = State.WalkingToTable;
    }

    private void Setup(Transform seatPoint) {
        targetTransform = seatPoint;
        currentState = State.WalkingToTable;
    }

    private void Update() {
        switch (currentState) {
            case State.WalkingToTable:
                //HandleWalkingToTable();
                break;
            case State.WaitingForFood:
                //HandleWaitingForFood();
                break;
            case State.AttackingPlayer:
                //HandleAttackingPlayer();
                break;
            case State.Leaving:
                break;
        }
    }

    private void HandleWalkingToTable() {
        navMeshAgent.SetDestination(targetTransform.position);
        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance) {

            navMeshAgent.isStopped = true;

            currentState = State.WaitingForFood;
            patienceTimer = patienceMax;

            // animator.SetBool("IsWalking", false);
        }
    }

    private void HandleWaitingForFood() {
        patienceTimer -= Time.deltaTime;
        float patienceNormalized = patienceTimer / patienceMax;

        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = patienceNormalized
        });

        if (patienceTimer <= 0f) {
            //fight player
        }
    }

    private void HandleAttackingPlayer() {
        navMeshAgent.isStopped = false;

        Vector3 playerPosition = Player.Instance.transform.position;
        navMeshAgent.SetDestination(playerPosition);

        
    }

    private void AttackPlayer() {

    } 
}