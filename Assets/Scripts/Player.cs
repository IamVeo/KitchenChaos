using System;
using UnityEngine;

public class Player : MonoBehaviour, IKitchenObjectParent {


    public static Player Instance { get; private set; }

    [SerializeField] private PlayerDataSO playerDataSO;
    [SerializeField] private GameInput gameInput;
    [SerializeField] private LayerMask countersLayerMask;
    [SerializeField] private Transform kitchenObjectHoldPoint;
    public float InteractDistance => playerDataSO.interactDistance;

    [Header("Combat Stats")]
    [SerializeField] private LayerMask enemyLayerMask;
    public int AttackDamage => playerDataSO.attackDamage;
    public float AttackRange => playerDataSO.attackRange;
    public float HitRadius => playerDataSO.hitRadius; 

    public event EventHandler OnPickedSomething;
    public event EventHandler OnAttacking;

    public event EventHandler<OnSelectedCounterChangedEventArgs> OnSelectedCounterChanged;
    public class OnSelectedCounterChangedEventArgs : EventArgs {
        public BaseCounter selectedCounter;
    }

    public float MoveSpeed => playerDataSO.moveSpeed;
    public float AttackDelay => playerDataSO.attackDelay;

    private bool isWalking;
    private bool isAttacking;
    private Vector3 lastInteractDir;
    private BaseCounter selectedCounter;
    private KitchenObject kitchenObject;
    private HealthManager healthManager;

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There is more than one Player instance");
        }
        Instance = this;
        healthManager = GetComponent<HealthManager>();
    }

    private void Start() {
        gameInput.OnInteractAction += GameInput_OnInteractAction;
        gameInput.OnInteractAlternateAction += GameInput_OnInteractAlternateAction;
        gameInput.OnAttackAction += GameInput_OnAttackAction;
        healthManager.OnDied += HealthManager_OnDied;

    }

    private void HealthManager_OnDied(object sender, EventArgs e) {
        Destroy(gameObject);
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

    private void Update()
    {
        HandleMovement();
        HandleInteractions();
    }

    public bool IsWalking() => isWalking;
    public bool IsAttacking() => isAttacking;

    private void HandleInteractions() {
        Vector2 inputVector = gameInput.GetMovementVectorNormalized();

        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        if (moveDir != Vector3.zero) {
            lastInteractDir = moveDir;
        }
        
        if (Physics.Raycast(transform.position, lastInteractDir, out RaycastHit raycastHit, InteractDistance, countersLayerMask)) {
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
        Vector2 inputVector = gameInput.GetMovementVectorNormalized();

        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        float moveDistance = MoveSpeed * Time.deltaTime;
        float playerRadius = .7f;
        float playerHeight = 2f;
        bool canMove = !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, moveDir, moveDistance);

        if (!canMove) {
            // Cannot move towards moveDir

            // Attempt only X movement
            Vector3 moveDirX = new Vector3(moveDir.x, 0, 0).normalized;
            canMove = (moveDir.x < -.5f || moveDir.x > +.5f) && !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, moveDirX, moveDistance);

            if (canMove) {
                // Can move only on the X
                moveDir = moveDirX;
            } else {
                // Cannot move only on the X

                // Attempt only Z movement
                Vector3 moveDirZ = new Vector3(0, 0, moveDir.z).normalized;
                canMove = (moveDir.z < -.5f || moveDir.z > +.5f) && !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, moveDirZ, moveDistance);

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

        float rotateSpeed = 10f;
        transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
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
            if (hitCollider.TryGetComponent(out IDamageable damageableTarget)) {
                Vector3 knockbackDirection = hitCollider.transform.position - transform.position;

                damageableTarget.TakeDamage(AttackDamage, knockbackDirection);
            }
        }
    }

    private void OnDrawGizmosSelected() {
        Vector3 direction = lastInteractDir == Vector3.zero ? transform.forward : lastInteractDir;
        Vector3 hitCenter = transform.position + direction * AttackRange;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(hitCenter, HitRadius);
    }


    private void OnDestroy()
    {
        gameInput.OnInteractAction -= GameInput_OnInteractAction;
        gameInput.OnInteractAlternateAction -= GameInput_OnInteractAlternateAction;
        gameInput.OnAttackAction -= GameInput_OnAttackAction;
        healthManager.OnDied -= HealthManager_OnDied;
    }
}