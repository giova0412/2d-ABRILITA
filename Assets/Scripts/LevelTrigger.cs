using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LevelTrigger : MonoBehaviour
{
    [SerializeField] private LevelProgress progress;
    [SerializeField] private bool finishLine;
    private bool collected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || other.attachedRigidbody == null) return;
        if (!other.attachedRigidbody.TryGetComponent<PlayerController>(out var player)) return;
        if (finishLine) progress.ReachFinish(player);
        else if (progress.CollectCoin(player))
        {
            collected = true;
            gameObject.SetActive(false);
        }
    }
}
