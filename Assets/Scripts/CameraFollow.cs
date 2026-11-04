using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, -10f);

    [Header("Smoothing")]
    [SerializeField] private float smoothTime = 0.25f;

    [Header("Dead Zone")]
    [SerializeField] private Vector2 deadZone = new Vector2(0.5f, 0.5f);

    [Header("Look Ahead (Horizontal Anticipation)")]
    [SerializeField] private float lookAheadDistance = 2f;
    [SerializeField] private float lookAheadSpeed = 3f;

    [Header("Level Bounds")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private Vector2 minBounds = new Vector2(-10f, -2f);
    [SerializeField] private Vector2 maxBounds = new Vector2(50f, 15f);

    private Vector3 currentVelocity = Vector3.zero;
    private float currentLookAheadX = 0f;
    private Vector3 currentFocusPoint;
    private Vector3 previousTargetPosition;
    private PlayerController player;
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        SnapToTarget();
    }

    private void OnEnable()
    {
        if (target != null && target.TryGetComponent<PlayerController>(out player))
            player.Respawned += SnapToTarget;
    }

    private void OnDisable()
    {
        if (player != null) player.Respawned -= SnapToTarget;
    }

    public void SnapToTarget()
    {
        if (target == null) return;
        currentVelocity = Vector3.zero;
        currentLookAheadX = 0f;
        currentFocusPoint = target.position;
        previousTargetPosition = target.position;
        transform.position = CalculateTargetPosition(currentFocusPoint);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Apply Dead Zone
        Vector3 targetPos = target.position;
        Vector3 delta = targetPos - currentFocusPoint;

        if (Mathf.Abs(delta.x) > deadZone.x)
        {
            currentFocusPoint.x = targetPos.x - Mathf.Sign(delta.x) * deadZone.x;
        }
        if (Mathf.Abs(delta.y) > deadZone.y)
        {
            currentFocusPoint.y = targetPos.y - Mathf.Sign(delta.y) * deadZone.y;
        }

        // Horizontal Look Ahead
        float movementX = targetPos.x - previousTargetPosition.x;
        previousTargetPosition = targetPos;
        float targetLookAhead = 0f;
        if (movementX > 0.001f) targetLookAhead = lookAheadDistance;
        else if (movementX < -0.001f) targetLookAhead = -lookAheadDistance;

        currentLookAheadX = Mathf.MoveTowards(currentLookAheadX, targetLookAhead, lookAheadSpeed * Time.deltaTime);

        // Calculate final desired position
        Vector3 desiredPosition = CalculateTargetPosition(currentFocusPoint);
        desiredPosition.x += currentLookAheadX;

        // Clamp to Bounds if enabled
        if (useBounds && cam != null && cam.orthographic)
        {
            float vertExtent = cam.orthographicSize;
            float horzExtent = vertExtent * cam.aspect;

            float minX = minBounds.x + horzExtent;
            float maxX = maxBounds.x - horzExtent;
            float minY = minBounds.y + vertExtent;
            float maxY = maxBounds.y - vertExtent;

            if (minX <= maxX)
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            if (minY <= maxY)
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }

        // Smooth Damp
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);
    }

    private Vector3 CalculateTargetPosition(Vector3 focusPoint)
    {
        Vector3 position = new Vector3(focusPoint.x + offset.x, focusPoint.y + offset.y, offset.z);
        if (useBounds && cam != null && cam.orthographic)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            position.x = minBounds.x + halfWidth <= maxBounds.x - halfWidth
                ? Mathf.Clamp(position.x, minBounds.x + halfWidth, maxBounds.x - halfWidth)
                : (minBounds.x + maxBounds.x) * 0.5f;
            position.y = minBounds.y + halfHeight <= maxBounds.y - halfHeight
                ? Mathf.Clamp(position.y, minBounds.y + halfHeight, maxBounds.y - halfHeight)
                : (minBounds.y + maxBounds.y) * 0.5f;
        }
        return position;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            currentFocusPoint = target.position;
        }
    }

    public void SetBounds(Vector2 min, Vector2 max)
    {
        useBounds = true;
        minBounds = min;
        maxBounds = max;
    }

    private void OnDrawGizmosSelected()
    {
        if (useBounds)
        {
            Gizmos.color = Color.cyan;
            Vector3 center = new Vector3((minBounds.x + maxBounds.x) * 0.5f, (minBounds.y + maxBounds.y) * 0.5f, 0f);
            Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 1f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
