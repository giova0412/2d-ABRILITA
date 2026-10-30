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
    }

    private void Update()
    {
        GatherInput();

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
        ApplyMovement();
        ApplyDynamicGravity();
    }

    private void GatherInput()
    {
        horizontalInput = 0f;
        bool jumpPressedThisFrame = false;
        jumpHeld = false;

        // New Input System: Keyboard
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                horizontalInput -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                horizontalInput += 1f;

            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
                jumpPressedThisFrame = true;

            if (keyboard.spaceKey.isPressed || keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                jumpHeld = true;
        }

        // New Input System: Gamepad (optional support)
        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            float stickX = gamepad.leftStick.x.ReadValue();
            if (Mathf.Abs(stickX) > 0.15f)
                horizontalInput += stickX;

            if (gamepad.dpad.left.isPressed)
                horizontalInput -= 1f;
            if (gamepad.dpad.right.isPressed)
                horizontalInput += 1f;

            if (gamepad.buttonSouth.wasPressedThisFrame)
                jumpPressedThisFrame = true;

            if (gamepad.buttonSouth.isPressed)
                jumpHeld = true;
        }

        horizontalInput = Mathf.Clamp(horizontalInput, -1f, 1f);

        if (jumpPressedThisFrame)
        {
            jumpBufferCounter = jumpBufferTime;
        }
    }

    private void CheckGround()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
        else
        {
            // Fallback ground detection using bounds
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                Vector2 bottom = new Vector2(col.bounds.center.x, col.bounds.min.y);
                isGrounded = Physics2D.OverlapCircle(bottom, groundCheckRadius, groundLayer);
            }
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
