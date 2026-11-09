using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Checkpoint2D : MonoBehaviour
{
    [Header("Reaparición")]
    [Tooltip("Posición del Transform del jugador al reaparecer, teniendo en cuenta el offset del collider.")]
    [SerializeField] private Transform _respawnPoint;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D body = other.attachedRigidbody;
        if (body == null || !body.CompareTag("Player")) return;
        if (!body.TryGetComponent<PlayerController>(out var player)) return;
        // La próxima muerte usará este punto en lugar del inicio del nivel.
        if (_respawnPoint != null) player.SetCheckpoint(_respawnPoint.position);
    }
}
