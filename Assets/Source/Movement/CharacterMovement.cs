using UnityEngine;

[RequireComponent(typeof(Health))]
public class CharacterMovement : Movement
{
    [field: SerializeField]
    public float WalkSpeed { get; set; } = 5f;

    [SerializeField, Tooltip("Rate at which current knockback velocity approaches 0.")]
    private float KnockbacDecayRate = 64f;

    [SerializeField]
    private float BounceScale = 0.8f;

    private Vector2 _knockback;

    protected override void Awake()
    {
        base.Awake();
        GetComponent<Health>().OnDamaged += OnDamaged;
    }

    protected override void LateUpdate()
    {
        AddVelocity(_knockback);
        _knockback = Vector2.MoveTowards(_knockback, Vector2.zero, KnockbacDecayRate * Time.deltaTime);

        base.LateUpdate();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (BounceScale <= 0f) return;

        if (collision.gameObject.layer == Layers.ObstacleLayer)
        {
            ContactPoint2D contact = collision.GetContact(0);

            if (Vector2.Dot(_knockback, -contact.normal) > 0f)
            {
                _knockback = Vector2.Reflect(_knockback, contact.normal) * BounceScale;
            }
        }
    }

    public void Walk(Vector2 direction)
    {
        AddVelocity(direction.normalized * WalkSpeed);
    }

    public void ApplyKnockback(Vector2 velocity)
    {
        _knockback += velocity;
    }

    private void OnDamaged(DamageInfo info)
    {
        ApplyKnockback(info.Direction.normalized * info.Knockback);
    }
}