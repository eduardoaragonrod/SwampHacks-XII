using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Movement : MonoBehaviour
{
    private Rigidbody2D _rb;

    private Vector2? _overrideVelocity;
    private Vector2 _cumulativeVelocity;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void LateUpdate()
    {
        _rb.linearVelocity = _overrideVelocity ??= _cumulativeVelocity;
        _overrideVelocity = null;
        _cumulativeVelocity = Vector2.zero;
    }

    protected void AddVelocity(Vector2 velocity)
    {
        _cumulativeVelocity += velocity;
    }

    protected void SetVelocity(Vector2 velocity)
    {
        _overrideVelocity = velocity;
    }
}
