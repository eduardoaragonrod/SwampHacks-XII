using UnityEngine;

[RequireComponent(typeof(Health))]
public class CharacterMovement : Movement
{
    [field: SerializeField]
    public float WalkSpeed { get; set; } = 5f;

    [SerializeField]
    private float KnockbacDecayRate = 64f;
    [SerializeField]
    private float BounceScale = 0.8f;

    public bool IsDashing { get; private set; }
    public event System.Action OnDashEnd;


    private float _dashEndTime;
    private Vector2 _dashDir;
	private float _dashSpeed;
	private float _dashDistance;
    private Vector2 _knockback;
    public float CurrentKnockbackSpeed => _knockback.magnitude;

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
                SetVelocity(_dashDir * _dashSpeed);
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

    public void Dash(Vector2 direction, float speed, float distance)
    {
        IsDashing = true;
        _dashEndTime = Time.time + (_dashDistance / _dashSpeed);
        _dashDir = direction.normalized;
        _dashDistance = distance;
        _dashSpeed = speed;
    }

    public void StopDash()
    {
        IsDashing = false;
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
