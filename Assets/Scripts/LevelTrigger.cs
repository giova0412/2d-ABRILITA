using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LevelTrigger : MonoBehaviour
{
    [SerializeField] private LevelProgress progress;
    [SerializeField] private bool finishLine;
    private bool collected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || progress == null || other.attachedRigidbody == null) return;
        // Funciona también con colliders hijos del jugador.
        if (!other.attachedRigidbody.CompareTag("Player")) return;
        if (!other.attachedRigidbody.TryGetComponent<PlayerController>(out var player)) return;
        if (finishLine) progress.ReachFinish(player);
        else if (CompareTag("Coin") && progress.CollectCoin(player))
        {
            // Un jugador con varios colliders solo suma una moneda.
            collected = true;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
