using UnityEngine;

[RequireComponent(typeof(Health))]
public class CharacterMovement : Movement
{
    [field: SerializeField]
    public float WalkSpeed { get; set; } = 5f;
    [SerializeField, Tooltip("Rate at which current knockback velocity approaches 0.")]
    private float KnockbacDecayRate = 64f;

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
