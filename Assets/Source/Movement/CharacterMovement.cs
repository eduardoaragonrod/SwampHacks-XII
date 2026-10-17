using System;
using System.Collections;
using DG.Tweening;
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

    [Header("Wall Slam Juice (Hit-Stop, Shake & GamepadFX)")]
    [SerializeField] private float wallImpactSpeedThreshold = 8.5f;
    [SerializeField] private float hitStopDuration = 0.065f;
    [SerializeField] private float shakeDuration = 0.16f;
    [SerializeField] private float shakeStrength = 0.45f;
    [SerializeField, Range(0f, 1f)] private float rumbleFrequency = 0.85f;
    [SerializeField] private float rumbleDuration = 0.15f;

    public event Action OnDashEnd;

    private Vector2 _knockback;
    private Coroutine _hitStopRoutine;

    // Dash state tracking
    private bool _isDashing;
    private Vector2 _dashDirection;
    private float _dashSpeed;
    private float _dashRemainingDistance;

    public float CurrentKnockbackSpeed => _knockback.magnitude;

    protected override void Awake()
    {
        base.Awake();
        GetComponent<Health>().OnDamaged += OnDamaged;
    }

    protected override void LateUpdate()
    {
        if (_isDashing)
        {
            float step = _dashSpeed * Time.deltaTime;
            if (step >= _dashRemainingDistance)
            {
                AddVelocity(_dashDirection * (_dashRemainingDistance / Mathf.Max(0.0001f, Time.deltaTime)));
                StopDash();
            }
            else
            {
                AddVelocity(_dashDirection * _dashSpeed);
                _dashRemainingDistance -= step;
            }
        }

        AddVelocity(_knockback);
        _knockback = Vector2.MoveTowards(_knockback, Vector2.zero, KnockbacDecayRate * Time.deltaTime);

        base.LateUpdate();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == Layers.ObstacleLayer)
        {
            if (_isDashing)
            {
                StopDash();
            }

            if (BounceScale <= 0f) return;

            ContactPoint2D contact = collision.GetContact(0);

            if (Vector2.Dot(_knockback, -contact.normal) > 0f)
            {
                float impactSpeed = _knockback.magnitude;

                // Reflect velocity off the wall normal
                _knockback = Vector2.Reflect(_knockback, contact.normal) * BounceScale;

                // Trigger Mario/Smash-style Hit-Stop, Screen Shake & Controller Rumble at high speed!
                if (impactSpeed >= wallImpactSpeedThreshold)
                {
                    float intensityRatio = Mathf.Clamp(impactSpeed / (wallImpactSpeedThreshold * 1.5f), 0.8f, 1.8f);
                    TriggerWallSlamJuice(intensityRatio);
                }
            }
        }
    }

    private void TriggerWallSlamJuice(float multiplier)
    {
        GamepadFX.Pulse(Mathf.Clamp01(rumbleFrequency * multiplier), rumbleDuration);

        if (Camera.main != null)
        {
            Camera.main.transform.DOComplete();
            Camera.main.transform.DOShakePosition(
                shakeDuration, 
                shakeStrength * multiplier, 
                vibrato: 25, 
                randomness: 90f, 
                fadeOut: true
            ).SetUpdate(true);
        }

        if (_hitStopRoutine != null) StopCoroutine(_hitStopRoutine);
        _hitStopRoutine = StartCoroutine(HitStopRoutine(hitStopDuration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
        _hitStopRoutine = null;
    }

    private void OnDisable()
    {
        if (Time.timeScale == 0f) Time.timeScale = 1f;
    }

    public void Walk(Vector2 direction)
    {
        if (_isDashing) return;
        AddVelocity(direction.normalized * WalkSpeed);
    }

    public void Dash(Vector2 direction, float speed, float distance)
    {
        if (direction.sqrMagnitude <= 0f || speed <= 0f || distance <= 0f) return;

        _isDashing = true;
        _dashDirection = direction.normalized;
        _dashSpeed = speed;
        _dashRemainingDistance = distance;
    }

    public void StopDash()
    {
        if (!_isDashing) return;

        _isDashing = false;
        _dashRemainingDistance = 0f;
        OnDashEnd?.Invoke();
    }

    public void ApplyKnockback(Vector2 velocity)
    {
        _knockback += velocity;
    }

    private void OnDamaged(DamageInfo info)
    {
        StopDash();
        ApplyKnockback(info.Direction.normalized * info.Knockback);
    }
}