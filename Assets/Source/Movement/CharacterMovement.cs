using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class CharacterMovement : Movement
{
    [field: SerializeField]
    public float WalkSpeed { get; set; } = 5f;
    [SerializeField, Tooltip("Rate at which current knockback velocity approaches 0.")]
    private float KnockbacDecayRate = 64f;
    [SerializeField]
    private float BounceScale = 0f;

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

    private void FixedUpdate()
    {
        if (BounceScale > 0f)
        {
            var contacts = new List<ContactPoint2D>();
            Rigidbody.GetContacts(contacts);

            bool hitObstacle = false;

            foreach (var contact in contacts)
            {
                Debug.Log(Vector2.Dot(_knockback.normalized, contact.normal));

                if (contact.collider.gameObject.layer == Layers.ObstacleLayer
                    && Vector2.Dot(_knockback.normalized, -contact.normal) > 0f)
                {
                    hitObstacle = true;
                    break;
                }
            }

            if (hitObstacle)
            {
                var vel = _knockback;
                vel *= BounceScale;

                if (Mathf.Abs(vel.x) > Mathf.Abs(vel.y))
                {
                    vel.x = -vel.x;
                }
                else
                {
                    vel.y = -vel.y;
                }

                _knockback = vel;

                Debug.Log(_knockback);
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
