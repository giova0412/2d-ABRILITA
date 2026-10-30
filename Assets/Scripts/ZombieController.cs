using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Animator))]
public class ZombieController : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float leftLimit = 16f;
    [SerializeField] private float rightLimit = 21f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundProbeDistance = 0.4f;
    [SerializeField] private float wallProbeDistance = 0.15f;

    [Header("Visual & Death")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private AnimationClip deathAnimation;
    [SerializeField] private float deathPause = 0.4f;

    [Header("Stomp Detection")]
    [SerializeField] private float minimumFallSpeed = 0.1f;
    [SerializeField] private float topTolerance = 0.08f;
    [SerializeField] private float minimumTopNormal = 0.65f;
    [SerializeField] private float minimumOverlap = 0.12f;

    private Rigidbody2D rb;
    private BoxCollider2D bodyCollider;
    private Animator animator;
    private Bounds previousBounds;
    private int direction = 1;
    public bool IsDead { get; private set; }
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int DeadHash = Animator.StringToHash("Dead");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
        previousBounds = bodyCollider.bounds;
    }

    private void FixedUpdate()
    {
        if (IsDead) return;
        Bounds bounds = bodyCollider.bounds;
        bool atLimit = direction > 0 ? rb.position.x >= rightLimit : rb.position.x <= leftLimit;
        float frontX = bounds.center.x + direction * (bounds.extents.x + wallProbeDistance);
        bool floorAhead = Physics2D.Raycast(new Vector2(frontX, bounds.min.y + topTolerance), Vector2.down, groundProbeDistance, groundLayer);
        bool wallAhead = Physics2D.Raycast(bounds.center, Vector2.right * direction, bounds.extents.x + wallProbeDistance, groundLayer);
        if (atLimit || !floorAhead || wallAhead) direction *= -1;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        previousBounds = bounds;
        if (spriteRenderer != null) spriteRenderer.flipX = direction < 0;
        animator.SetFloat(SpeedHash, Mathf.Abs(rb.linearVelocity.x));
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        ResolvePlayerContact(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        ResolvePlayerContact(collision);
    }

    private void ResolvePlayerContact(Collision2D collision)
    {
        if (IsDead || collision.rigidbody == null) return;
        if (!collision.rigidbody.TryGetComponent<PlayerController>(out var player) || player.IsDead) return;

        Bounds playerBounds = player.PreviousBounds;
        float overlap = Mathf.Min(playerBounds.max.x, previousBounds.max.x) - Mathf.Max(playerBounds.min.x, previousBounds.min.x);
        bool fromAbove = playerBounds.min.y >= previousBounds.max.y - topTolerance;
        bool descendingFromJump = player.IsJumpingForStomp && player.PreviousVelocity.y < -minimumFallSpeed;
        bool topContact = false;
        for (int i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.GetContact(i);
            Vector2 normal = contact.normal;
            // Orient toward the player, independent of which collider owns the callback.
            Vector2 towardPlayer = playerBounds.center - previousBounds.center;
            if (Vector2.Dot(normal, towardPlayer) < 0) normal = -normal;
            if (normal.y >= minimumTopNormal && contact.point.y >= bodyCollider.bounds.max.y - topTolerance)
                topContact = true;
        }

        if (descendingFromJump && fromAbove && overlap >= minimumOverlap && topContact)
        {
            Die();
            player.BounceAfterStomp();
        }
        else
        {
            player.Die();
        }
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        bodyCollider.enabled = false;
        animator.SetFloat(SpeedHash, 0);
        animator.SetBool(DeadHash, true);
        animator.Play("Dead", 0, 0);
        StartCoroutine(FinishDeath());
    }

    private System.Collections.IEnumerator FinishDeath()
    {
        yield return new WaitForSeconds((deathAnimation != null ? deathAnimation.length : 1f) + deathPause);
        gameObject.SetActive(false);
    }
}
