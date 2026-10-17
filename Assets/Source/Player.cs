using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
[RequireComponent(typeof(CharacterMovement))]

[RequireComponent(typeof(Health))]
public class Player : MonoBehaviour
{
    private static readonly int HitHash = Animator.StringToHash("Hit");

    public static Player Instance { get; private set; }

    [SerializeField] private WispOrb _OrbPrefab;
    [SerializeField] private int _MaxOrbs = 6;
    [SerializeField] private Light2D AuraLight;
    [SerializeField] private Element DefaultElement = Element.Fire;
    [SerializeField] private Animator Animator;

    [Space]

    [SerializeField] private float DashCooldown = 0.5f;

    private PlayerInputMap _input;
    private CharacterMovement _movement;
    private Health _health;
    private float _baseWalkSpeed;
    private readonly List<WispOrb> _orbs = new();
    private ElementSO _currentElement;

    private float _nextAllowedDashTime;
    private Vector2 _lastMoveDir = Vector2.up;
    private readonly object _dashDamageImmunityBlocker = new();

    private void Awake()
    {
        Instance = this;

        _input = new();
        _movement = GetComponent<CharacterMovement>();
        _baseWalkSpeed = _movement.WalkSpeed;

        _input.Player.Shoot.performed += OnShoot;
        _input.Player.Pull.performed += OnPull;
        _input.Player.Dash.performed += OnDash;

        ElementManager.OnChanged += OnElementChanged;
        ElementManager.SetElement(DefaultElement);

        _health = GetComponent<Health>();
        _health.OnDamaged += OnDamaged;

        _movement.OnDashEnd += OnDashEnd;
    }

    private void OnEnable() => _input.Enable();
    private void OnDisable() => _input.Disable();

    private void Update()
    {
        var dir = _input.Player.Move.ReadValue<Vector2>();
        _movement.Walk(dir);
        if (dir.sqrMagnitude > 0f)
        {
            _lastMoveDir = dir.normalized;
        }

#if UNITY_EDITOR
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            ElementManager.SetElement(Element.Fire);
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            ElementManager.SetElement(Element.Lightning);
        }
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            ElementManager.SetElement(Element.Ice);
        }
#endif
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    private void OnElementChanged(Element element)
    {
        var data = ElementManager.GetData(element);
        _currentElement = data;
        _movement.WalkSpeed = _baseWalkSpeed * data.MoveSpeedMultiplier;
        AuraLight.color = data.Color;
        Animator.SetTrigger(element.ToString());
    }

    private void OnShoot(InputAction.CallbackContext context)
    {
        if (_orbs.Count >= _MaxOrbs) return;

        Vector2 aimDir = GetAimDirection();
        WispOrb orb = Instantiate(_OrbPrefab, transform.position, Quaternion.identity);
        orb.Init(this, _currentElement, aimDir);
        _orbs.Add(orb);
    }

    private void OnPull(InputAction.CallbackContext context)
    {
        foreach (WispOrb orb in _orbs)
        {
            orb.Pull();
        }
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        if (Time.time >= _nextAllowedDashTime)
        {
            _nextAllowedDashTime = Time.time + DashCooldown;
            _movement.Dash(_lastMoveDir);
            _health.RegisterDamageBlocker(_dashDamageImmunityBlocker);
            Audio.Play(ElementManager.CurrentData.DashSound, transform.position);
        }
    }

    private void OnDashEnd()
    {
        _health.UnregisterDamageBlocker(_dashDamageImmunityBlocker);
    }

    public void CatchOrb(WispOrb orb, Vector2 impactDir, float knockbackForce)
    {
        _orbs.Remove(orb);
        _movement.ApplyKnockback(impactDir * knockbackForce);
    }

    private Vector2 GetAimDirection()
    {
        // HACK: This would ideally use the aim input for mouse or gamepad.
        if (Gamepad.current != null)
        {
            return _input.Player.Aim.ReadValue<Vector2>().normalized;
        }

        var mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        return ((Vector2)(mouseWorld - transform.position)).normalized;
    }

    private void OnDamaged(DamageInfo info)
    {
        Animator.SetTrigger(HitHash);
        _movement.StopDash();
    }
}
