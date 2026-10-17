using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Movement : MonoBehaviour
{
    protected Rigidbody2D Rigidbody { get; private set; }

    private Vector2? _overrideVelocity;
    private Vector2 _cumulativeVelocity;

    protected virtual void Awake()
    {
        Rigidbody = GetComponent<Rigidbody2D>();
    }

    protected virtual void LateUpdate()
    {
        Rigidbody.linearVelocity = _overrideVelocity ??= _cumulativeVelocity;
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
