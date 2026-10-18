using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider2D))]
public class PlayerTriggerEvents : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("If true, this trigger will automatically disable itself after being entered once.")]
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private string playerTag = "Player";

    [Header("Trigger Events")]
    [Tooltip("Fires immediately when the player enters the trigger zone.")]
    public UnityEvent OnPlayerEnter;
    [Tooltip("Fires immediately when the player exits the trigger zone.")]
    public UnityEvent OnPlayerExit;

    private bool _hasTriggered;

    private void Awake()
    {
        if (TryGetComponent<Collider2D>(out var col))
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasTriggered && triggerOnce) return;

        if (other.CompareTag(playerTag) || other.GetComponent<Player>() != null)
        {
            _hasTriggered = true;
            OnPlayerEnter?.Invoke();

            if (triggerOnce)
            {
                if (TryGetComponent<Collider2D>(out var col))
                {
                    col.enabled = false;
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag) || other.GetComponent<Player>() != null)
        {
            OnPlayerExit?.Invoke();
        }
    }
}