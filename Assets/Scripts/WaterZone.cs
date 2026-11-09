using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WaterZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other) { TryKillPlayer(other); }
    private void OnTriggerStay2D(Collider2D other) { TryKillPlayer(other); }

    private void TryKillPlayer(Collider2D other)
    {
        if (!CompareTag("Water")) return;
        Rigidbody2D body = other.attachedRigidbody;
        if (body == null || !body.CompareTag("Player")) return;
        if (!body.TryGetComponent<PlayerController>(out var player)) return;
        // Ignoramos la protección contra enemigos: el vacío siempre es letal.
        if (!player.IsDead && !player.LevelCompleted) player.Die(true);
    }
}
