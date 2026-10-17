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

    [Space]

    [SerializeField]
    private float DashSpeed = 10f;
    [SerializeField]
    private float DashDistance = 3f;

    public bool IsDashing { get; private set; }
    public event System.Action OnDashEnd;

    private Vector2 _knockback;
    private float _dashEndTime;
    private Vector2 _dashDir;

    protected override void Awake()
    {
        base.Awake();
        GetComponent<Health>().OnDamaged += OnDamaged;
    }

    private void Update()
    {
        if (IsDashing)
        {
            if (Time.time < _dashEndTime)
            {
                SetVelocity(_dashDir * DashSpeed);
            }
            else
            {
                IsDashing = false;
                OnDashEnd?.Invoke();
            }
        }
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

    public void Dash(Vector2 direction)
    {
        IsDashing = true;
        _dashEndTime = Time.time + (DashDistance / DashSpeed);
        _dashDir = direction.normalized;
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