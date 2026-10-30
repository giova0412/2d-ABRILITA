using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float deceleration = 40f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private float fallGravityMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.15f;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Visual & Animation")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Death & Respawn")]
    [SerializeField] private AnimationClip deathAnimation;
    [SerializeField] private float respawnPause = 0.6f;
    [SerializeField] private float stompBounceForce = 9f;
    [SerializeField] private float deathBelowY = -8f;

    private Collider2D bodyCollider;
    private Vector2 spawnPosition;
    private bool simulatedBeforeDeath;
    private bool jumpedSinceGrounded;
    private bool hasLeftGround;
    private float jumpStartY;
    private UnityEngine.InputSystem.InputAction moveAction;
    private UnityEngine.InputSystem.InputAction jumpAction;
    public bool IsDead { get; private set; }
    public bool IsJumpingForStomp => jumpedSinceGrounded;
    public Vector2 PreviousVelocity { get; private set; }
    public Bounds PreviousBounds { get; private set; }
    public event System.Action Respawned;
    private static readonly int DeadHash = Animator.StringToHash("Dead");

    // Component references
    private Rigidbody2D rb;

    // State variables
    private float horizontalInput;
    private bool isGrounded;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool jumpHeld;
    private float defaultGravityScale;
    private bool facingRight = true;

    // Animator parameter hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultGravityScale = rb.gravityScale > 0 ? rb.gravityScale : 3f;
        rb.gravityScale = defaultGravityScale;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        bodyCollider = GetComponent<Collider2D>();
        spawnPosition = rb.position;
        PreviousBounds = bodyCollider.bounds;
        moveAction = new UnityEngine.InputSystem.InputAction("Move", UnityEngine.InputSystem.InputActionType.Value);
        moveAction.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
        moveAction.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick/x");
        moveAction.AddCompositeBinding("1DAxis").With("Negative", "<Gamepad>/dpad/left").With("Positive", "<Gamepad>/dpad/right");
        jumpAction = new UnityEngine.InputSystem.InputAction("Jump", UnityEngine.InputSystem.InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.AddBinding("<Keyboard>/w");
        jumpAction.AddBinding("<Keyboard>/upArrow");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");
    }

    private void OnEnable()
    {
        moveAction.performed += OnMove;
        moveAction.canceled += OnMove;
        jumpAction.performed += OnJump;
        jumpAction.canceled += OnJump;
        moveAction.Enable();
        jumpAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        moveAction.performed -= OnMove;
        moveAction.canceled -= OnMove;
        jumpAction.performed -= OnJump;
        jumpAction.canceled -= OnJump;
        horizontalInput = 0;
        jumpHeld = false;
    }

    private void OnDestroy()
    {
        moveAction?.Dispose();
        jumpAction?.Dispose();
    }

    private void OnMove(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        horizontalInput = IsDead ? 0 : Mathf.Clamp(context.ReadValue<float>(), -1, 1);
    }

    private void OnJump(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (IsDead) return;
        jumpHeld = !context.canceled;
        if (context.performed) jumpBufferCounter = jumpBufferTime;
    }

    public void Die()
    {
        if (IsDead) return;
        IsDead = true;
        horizontalInput = 0;
        jumpHeld = false;
        jumpBufferCounter = coyoteTimeCounter = 0;
        jumpedSinceGrounded = false;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = defaultGravityScale;
        simulatedBeforeDeath = rb.simulated;
        rb.simulated = false;
        if (animator != null)
        {
            animator.ResetTrigger(JumpHash);
            animator.SetBool(DeadHash, true);
            animator.Play("Dead", 0, 0);
        }
        StartCoroutine(RespawnAfterDeath());
    }

    private System.Collections.IEnumerator RespawnAfterDeath()
    {
        yield return new WaitForSeconds((deathAnimation != null ? deathAnimation.length : 1f) + respawnPause);
        rb.position = spawnPosition;
        transform.position = new Vector3(spawnPosition.x, spawnPosition.y, transform.position.z);
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = defaultGravityScale;
        rb.simulated = simulatedBeforeDeath;
        horizontalInput = 0;
        jumpHeld = false;
        jumpBufferCounter = coyoteTimeCounter = 0;
        isGrounded = false;
        jumpedSinceGrounded = false;
        PreviousVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        PreviousBounds = bodyCollider.bounds;
        if (animator != null)
        {
            animator.SetBool(DeadHash, false);
            animator.SetFloat(SpeedHash, 0);
            animator.SetBool(IsGroundedHash, true);
            animator.ResetTrigger(JumpHash);
            animator.Play("Idle", 0, 0);
        }
        IsDead = false;
        Respawned?.Invoke();
    }

    public void BounceAfterStomp()
    {
        if (IsDead) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, stompBounceForce);
        rb.gravityScale = defaultGravityScale;
        jumpHeld = true;
        jumpedSinceGrounded = true;
        jumpStartY = rb.position.y;
        isGrounded = false;
        jumpBufferCounter = coyoteTimeCounter = 0;
        if (animator != null) animator.SetTrigger(JumpHash);
    }

    private void Update()
    {
        if (IsDead) return;
        if (rb.position.y < deathBelowY)
        {
            Die();
            return;
        }

        // Ground Check
        CheckGround();

        // Coyote Time
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Jump Buffer
        if (jumpBufferCounter > 0)
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Attempt Jump
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
        {
            ExecuteJump();
        }

        // Flip sprite based on direction
        HandleSpriteFlip();

        // Update Animator
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (IsDead) return;
        ApplyMovement();
        ApplyDynamicGravity();
        PreviousVelocity = rb.linearVelocity;
        PreviousBounds = bodyCollider.bounds;
    }

private void CheckGround()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
        else
        {
            // Fallback ground detection using cached bounds
            if (bodyCollider != null)
            {
                Vector2 bottom = new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.min.y);
                isGrounded = Physics2D.OverlapCircle(bottom, groundCheckRadius, groundLayer);
            }
        }
        if (!isGrounded) hasLeftGround = true;
        if (isGrounded && hasLeftGround && rb.linearVelocity.y <= 0.1f)
        {
            jumpedSinceGrounded = false;
            hasLeftGround = false;
        }
    }

    private void ApplyMovement()
    {
        float targetSpeed = horizontalInput * moveSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float movement = speedDiff * accelRate * Time.fixedDeltaTime;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x + movement, rb.linearVelocity.y);
    }

    private void ApplyDynamicGravity()
    {
        if (rb.linearVelocity.y < 0)
        {
            // Falling down
            rb.gravityScale = defaultGravityScale * fallGravityMultiplier;
        }
        else if (rb.linearVelocity.y > 0 && !jumpHeld)
        {
            // Released jump early (low jump)
            rb.gravityScale = defaultGravityScale * lowJumpMultiplier;
        }
        else
        {
            rb.gravityScale = defaultGravityScale;
        }
    }

    private void ExecuteJump()
    {
        jumpedSinceGrounded = true;
        hasLeftGround = false;
        jumpStartY = rb.position.y;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpBufferCounter = 0;
        coyoteTimeCounter = 0;

        if (animator != null)
        {
            animator.SetTrigger(JumpHash);
        }
    }

    private void HandleSpriteFlip()
    {
        if (horizontalInput > 0.05f && !facingRight)
        {
            facingRight = true;
            if (spriteRenderer != null)
                spriteRenderer.flipX = false;
        }
        else if (horizontalInput < -0.05f && facingRight)
        {
            facingRight = false;
            if (spriteRenderer != null)
                spriteRenderer.flipX = true;
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        float currentSpeed = Mathf.Abs(rb.linearVelocity.x);
        animator.SetFloat(SpeedHash, currentSpeed);
        animator.SetBool(IsGroundedHash, isGrounded);
    }

    public void ConfigureReferences(Transform check, LayerMask layer, Animator anim, SpriteRenderer sr)
    {
        groundCheck = check;
        groundLayer = layer;
        animator = anim;
        spriteRenderer = sr;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
