using System;
using UnityEngine;

public class Player : Character, IKitchenObjectParent {
    
    public static Player Instance { get; private set; }
    
    [SerializeField] private GameInput gameInput;
    [SerializeField] private LayerMask countersLayerMask;
    [SerializeField] private Transform kitchenObjectHoldPoint;
    [SerializeField] private LayerMask enemyLayerMask;

    public event EventHandler OnPickedSomething;
    public event EventHandler<OnSelectedCounterChangedEventArgs> OnSelectedCounterChanged;
    public class OnSelectedCounterChangedEventArgs : EventArgs {
        public BaseCounter selectedCounter;
    }

    private PlayerDataSO PlayerData => DataAs<PlayerDataSO>();

    // ----------- body
    public float PlayerHeight => PlayerData.playerHeight;
    public float PlayerRadius => PlayerData.playerRadius;
    

    // ----------- interactions
    public float InteractDistance => PlayerData.interactDistance;
    
    private bool isWalking;
    private bool isAttacking;
    private Vector3 lastInteractDir;
    private Vector3 currentCollisionNormal;
    private bool isTouchingCollider;
    private BaseCounter selectedCounter;
    private KitchenObject kitchenObject;

    private float knockbackTimer;
    private float knockbackDuration = 0.4f;
    private float knockbackForce = 10f;
    
    protected override void Awake()
    {
        base.Awake();

        if (Instance != null) {
            Debug.LogError("There is more than one Player instance");
        }
        Instance = this;
    }

    protected override void Start()
    {
        base.Start();
        
        gameInput.OnInteractAction += GameInput_OnInteractAction;
        gameInput.OnInteractAlternateAction += GameInput_OnInteractAlternateAction;
        gameInput.OnAttackAction += GameInput_OnAttackAction;
        
        healthManager.OnDied += HealthManager_OnDied;
    }

    private void HealthManager_OnDied(object sender, EventArgs e) {
        KitchenGameManager.Instance.SetGameOver();
    }

    private void GameInput_OnAttackAction(object sender, EventArgs e) {
        // if (!KitchenGameManager.Instance.IsGamePlaying()) return;
        if (isAttacking) return;
        
        isAttacking = true;
        Invoke(nameof(ResetAttacking), AttackCooldown);
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

    private void Update() {
        // 1. Cập nhật Timer
        if (knockbackTimer > 0) {
            knockbackTimer -= Time.deltaTime;
            isWalking = false; // NGĂN LỖI KẸT ANIMATION ĐI BỘ
            return;
        }

        if (isAttacking) {
            isWalking = false; // NGĂN LỖI KẸT ANIMATION ĐI BỘ
            return;
        }

        // 2. Xử lý logic tương tác (Raycast không ảnh hưởng đến di chuyển vật lý)
        HandleInteractions();
    }

    private void FixedUpdate()
    {

        HandleRotation();
        
        // Chỉ chạy vật lý di chuyển khi không bị choáng và không đánh nhau
        if (knockbackTimer <= 0 && !isAttacking)
            HandleMovement();
    }

    private Vector3 GetMovementDirection() {
        Vector2 inputVector = gameInput.GetMovementVectorNormalized();

        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        return moveDir;
    }

    private void HandleInteractions() {
        Vector3 moveDir = GetMovementDirection();

        if (moveDir != Vector3.zero) {
            lastInteractDir = moveDir;
        }

        Vector3 interactDir = lastInteractDir == Vector3.zero ? transform.forward : lastInteractDir;

        float interactDistance = InteractDistance;

        float sphereRadius = 0.3f;

        // Bắn SphereCast (Gọn gàng hơn BoxCast rất nhiều vì không cần tính góc xoay Quaternion)
        if (Physics.SphereCast(transform.position, sphereRadius, interactDir, out RaycastHit raycastHit, interactDistance, countersLayerMask)) {
            if (raycastHit.transform.TryGetComponent(out BaseCounter baseCounter)) {
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

        if (isTouchingCollider && moveDir != Vector3.zero)
        {
            float intoCollider = Vector3.Dot(moveDir, currentCollisionNormal);

            if (intoCollider < 0f)
                moveDir = (moveDir - currentCollisionNormal * intoCollider).normalized;
        }        
        
        Vector3 targetVelocity = moveDir * MoveSpeed;
        rb.velocity = new Vector3(targetVelocity.x, rb.velocity.y, targetVelocity.z);
    }

    private void HandleRotation()
    {
        Vector3 moveDir = GetMovementDirection();
        isWalking = moveDir != Vector3.zero;

        if (moveDir != Vector3.zero) {
            float rotateSpeed = 10f;
            Quaternion targetRotation = Quaternion.LookRotation(moveDir, Vector3.up);
            Quaternion smoothedRotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * rotateSpeed);
            rb.MoveRotation(smoothedRotation);
        }
    }
    
    private void Attack() {
        RaiseAttackPerformed();

        Vector3 hitCenter = transform.position + lastInteractDir * AttackRange;
        Collider[] hitColliders = Physics.OverlapSphere(hitCenter, HitRadius, enemyLayerMask);

        foreach (Collider hitCollider in hitColliders) {
            if (hitCollider.TryGetComponent<IDamageable>(out IDamageable damageableTarget)) {

                // THÊM KIỂM TRA QUÁI Ở ĐÂY:
                if (hitCollider.TryGetComponent<Enemy>(out Enemy enemy)) {
                    // Nếu khách đang không đánh mình (tức là đang đi tìm bàn hoặc đang chờ món)
                    if (!enemy.IsAttackingPlayer()) {
                        enemy.Enrage(); // Làm nó nổi điên hủy đơn luôn
                        continue;       // Bỏ qua lực đẩy lùi vì nó đang bị khóa FreezeAll
                    }
                }

                Vector3 knockbackDirection = hitCollider.transform.position - transform.position;
                damageableTarget.TakeDamage(AttackDamage, knockbackDirection.normalized);
            }
        }
    }

    private void OnDrawGizmosSelected() {
        Vector3 direction = lastInteractDir == Vector3.zero ? transform.forward : lastInteractDir;
        Vector3 hitCenter = transform.position + direction * AttackRange;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(hitCenter, HitRadius);
    }
    
    private void SetSelectedCounter(BaseCounter selectedCounter) {
        this.selectedCounter = selectedCounter;

        OnSelectedCounterChanged?.Invoke(this, new OnSelectedCounterChangedEventArgs {
            selectedCounter = selectedCounter
        });
    }

    private void OnCollisionStay(Collision collision)
    {
        Vector3 bestNormal = Vector3.zero;
        float bestScore = -1f;
        bool foundCollider = false;
        
        foreach (ContactPoint contact in collision.contacts)
        {
            if (Mathf.Abs(contact.normal.y) < 0.2f)
            {
                float score = Mathf.Abs(Vector3.Dot(contact.normal, GetMovementDirection()));
                if (score > bestScore)
                {
                    bestScore = score;
                    bestNormal = contact.normal;
                    foundCollider = true;
                }
            }
        }

        if (foundCollider)
        {
            currentCollisionNormal = bestNormal;
            isTouchingCollider = true;
        }
    }

    private void OnCollisionExit(Collision _)
    {
        isTouchingCollider = false;
        currentCollisionNormal = Vector3.zero;
    }
    
    
    
    
    public override bool IsWalking() => isWalking;
    public override bool IsAttacking() => isAttacking;
    public HealthManager GetHealthManager() => healthManager;

    protected override void ReceiveKnockback(Vector3 knockbackDir) {
        // Chỉ nhận lực mới nếu lực cũ đã hết (tránh cộng dồn)
        if (knockbackTimer <= 0) {
            knockbackTimer = knockbackDuration;

            // Xóa đà di chuyển cũ (nếu có) để lực đẩy được chính xác
            rb.velocity = Vector3.zero;

            // Bắn ra một lực Impulse
            rb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
        }
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
}