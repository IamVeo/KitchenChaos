using System;
using UnityEngine;

public class Player : MonoBehaviour, IKitchenObjectParent {


    public static Player Instance { get; private set; }

    [SerializeField] private PlayerDataSO playerDataSO;
    [SerializeField] private GameInput gameInput;
    [SerializeField] private LayerMask countersLayerMask;
    [SerializeField] private Transform kitchenObjectHoldPoint;
    [SerializeField] private LayerMask enemyLayerMask;

    public event EventHandler OnPickedSomething;
    public event EventHandler OnAttacking;

    public event EventHandler<OnSelectedCounterChangedEventArgs> OnSelectedCounterChanged;
    public class OnSelectedCounterChangedEventArgs : EventArgs {
        public BaseCounter selectedCounter;
    }

    // ---------- movement
    public float MoveSpeed => playerDataSO.moveSpeed;
    public float PlayerHeight => playerDataSO.playerHeight;
    public float PlayerRadius => playerDataSO.playerRadius;
    public float AttackDelay => playerDataSO.attackDelay;
    
    // ----------- combat
    public int AttackDamage => playerDataSO.attackDamage;
    public float AttackRange => playerDataSO.attackRange;
    public float HitRadius => playerDataSO.hitRadius;
    
    // ----------- interaction
    public float InteractDistance => playerDataSO.interactDistance;

    private bool isWalking;
    private bool isAttacking;
    private Vector3 lastInteractDir;
    private BaseCounter selectedCounter;
    private KitchenObject kitchenObject;

    private Vector3 knockbackVector;
    private float knockbackTimer;
    private float knockbackDuration = 0.2f;
    private float knockbackSpeed = 15f;

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one Player instance");
        }
        Instance = this;
    }

    private void Start() {
        gameInput.OnInteractAction += GameInput_OnInteractAction;
        gameInput.OnInteractAlternateAction += GameInput_OnInteractAlternateAction;
        gameInput.OnAttackAction += GameInput_OnAttackAction;

        if (TryGetComponent<HealthManager>(out HealthManager healthManager)){
            healthManager.OnDied += HealthManager_OnDied;
        }
    }

    private void HealthManager_OnDied(object sender, EventArgs e) {
        KitchenGameManager.Instance.SetGameOver();
    }

    private void GameInput_OnAttackAction(object sender, EventArgs e) {
        // if (!KitchenGameManager.Instance.IsGamePlaying()) return;
        if (isAttacking) return;
        
        isAttacking = true;
        Invoke(nameof(ResetAttacking), AttackDelay);
        Attack();
    }

    private void ResetAttacking()
    {
        isAttacking = false;
    }

    private void GameInput_OnInteractAlternateAction(object sender, EventArgs e) {
        if (!KitchenGameManager.Instance.IsGamePlaying()) return;

        if (selectedCounter != null) {
            selectedCounter.InteractAlternate(this);
        }
    }

    private void GameInput_OnInteractAction(object sender, System.EventArgs e) {
        if (!KitchenGameManager.Instance.IsGamePlaying()) return;

        if (selectedCounter != null) {
            selectedCounter.Interact(this);
        }
    }
    
    // ======================= Handle Player Input and Interactions =======================

    public bool IsWalking() => isWalking;
    public bool IsAttacking() => isAttacking;
    
    private void Update() {
        if (knockbackTimer > 0) {
            HandleKnockback();
            return;
        }

        if (isAttacking) return;
        HandleMovement();
        HandleInteractions();
    }
    
    private Vector3 GetMovementDirection()
    {
        Vector2 inputVector = gameInput.GetMovementVectorNormalized();

        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        return moveDir;
    }

    private (bool canMove, RaycastHit raycastHit) ColliderCast(Vector3 dir, float distance, LayerMask layerMask)
    {
        bool colliderCasted = Physics.CapsuleCast(transform.position,
            transform.position + Vector3.up * PlayerHeight,
            PlayerRadius,
            dir, 
            out RaycastHit raycastHit,
            distance,
            layerMask,
            QueryTriggerInteraction.Ignore);
        return (!colliderCasted, raycastHit);
    }

    private void HandleRotation(Vector3 dir)
    {
        float rotateSpeed = 10f;
        transform.forward = Vector3.Slerp(transform.forward, dir, Time.deltaTime * rotateSpeed);
    }
    
    private void HandleInteractions() {
        Vector3 moveDir = GetMovementDirection();

        if (moveDir != Vector3.zero) {
            lastInteractDir = moveDir;
        }
        
        var (canMove, raycastHit) = ColliderCast(lastInteractDir, InteractDistance, countersLayerMask);
        
        if (!canMove) {
            if (raycastHit.transform.TryGetComponent(out BaseCounter baseCounter)) {
                // Has ClearCounter
                if (baseCounter != selectedCounter) {
                    SetSelectedCounter(baseCounter);
                }
            } else {
                SetSelectedCounter(null);

            }
        } else {
            SetSelectedCounter(null);
        }
    }

    private void HandleMovement() {
        Vector3 moveDir = GetMovementDirection();

        float moveDistance = MoveSpeed * Time.deltaTime;

        var (canMove, _) = ColliderCast(moveDir, moveDistance, Physics.DefaultRaycastLayers);

        if (!canMove) {
            // Cannot move towards moveDir

            // Attempt only X movement
            Vector3 moveDirX = new Vector3(moveDir.x, 0, 0).normalized;
            (canMove, _) = ColliderCast(moveDirX, moveDistance, Physics.DefaultRaycastLayers);
            canMove = (moveDir.x < -.5f || moveDir.x > +.5f) && canMove;

            if (canMove) {
                // Can move only on the X
                moveDir = moveDirX;
            } else {
                // Cannot move only on the X

                // Attempt only Z movement
                Vector3 moveDirZ = new Vector3(0, 0, moveDir.z).normalized;
                (canMove, _) = ColliderCast(moveDirZ, moveDistance, Physics.DefaultRaycastLayers);
                canMove = (moveDir.z < -.5f || moveDir.z > +.5f) && canMove;

                if (canMove) {
                    // Can move only on the Z
                    moveDir = moveDirZ;
                } else {
                    // Cannot move in any direction
                }
            }
        }

        if (canMove) {
            transform.position += moveDir * moveDistance;
        }

        isWalking = moveDir != Vector3.zero;

        HandleRotation(moveDir);
    }

    private void HandleKnockback() {
        knockbackTimer -= Time.deltaTime;

        float moveDistance = knockbackSpeed * Time.deltaTime;

        bool canMove = !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * PlayerHeight, PlayerRadius, knockbackVector, moveDistance);

        if (!canMove) {
            transform.position += knockbackVector * moveDistance;
        }
    }

    private void SetSelectedCounter(BaseCounter selectedCounter) {
        this.selectedCounter = selectedCounter;

        OnSelectedCounterChanged?.Invoke(this, new OnSelectedCounterChangedEventArgs {
            selectedCounter = selectedCounter
        });
    }

    public Transform GetKitchenObjectFollowTransform() => kitchenObjectHoldPoint;

    public void SetKitchenObject(KitchenObject kitchenObject) {
        this.kitchenObject = kitchenObject;

        if (kitchenObject != null) {
            OnPickedSomething?.Invoke(this, EventArgs.Empty);
        }
    }

    public KitchenObject GetKitchenObject() => kitchenObject;

    public void ClearKitchenObject() => kitchenObject = null;

    public bool HasKitchenObject() => kitchenObject != null;

    private void Attack() {
        OnAttacking?.Invoke(this, EventArgs.Empty);

        Vector3 hitCenter = transform.position + lastInteractDir * AttackRange;

        Collider[] hitColliders = Physics.OverlapSphere(hitCenter, HitRadius, enemyLayerMask);

        foreach(Collider hitCollider in hitColliders) {
            if (hitCollider.TryGetComponent<IDamageable>(out IDamageable damageableTarget)) {
                Vector3 knockbackDirection = hitCollider.transform.position - transform.position;

                damageableTarget.TakeDamage(AttackDamage, lastInteractDir);
            }
        }
    }
    public void ReceiveKnockback(Vector3 knockbackDir) {
        knockbackVector = knockbackDir;
        knockbackTimer = knockbackDuration;
    }

    private void OnDrawGizmosSelected() {
        Vector3 direction = lastInteractDir == Vector3.zero ? transform.forward : lastInteractDir;
        Vector3 hitCenter = transform.position + direction * AttackRange;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(hitCenter, HitRadius);
    }
}