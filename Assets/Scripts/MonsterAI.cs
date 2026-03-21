using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MonsterAI : MonoBehaviour, IHasProgress {

    public event EventHandler<IHasProgress.OnProgressChangedEventArgs> onProgressChanged;

    private enum State {
        WalkingToTable,
        WaitingForFood,
        AttackingPlayer
    }

    private NavMeshAgent navMeshAgent;
    private State currentState;
    private Transform targetTransform;

    [SerializeField] private float patienceMax = 20f;
    private float patienceTimer;

    private void Awake() {
        currentState = State.WalkingToTable;
        patienceTimer = patienceMax;

    }
}
