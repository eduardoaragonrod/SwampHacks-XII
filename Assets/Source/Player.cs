using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterMovement))]
public class Player : MonoBehaviour
{
    [SerializeField] private WispOrb _OrbPrefab;
    [SerializeField] private ElementSO _CurrentElement;
    [SerializeField] private int _MaxOrbs = 6;

    private PlayerInputMap _input;
    private CharacterMovement _movement;
    private float _baseWalkSpeed;
    private readonly List<WispOrb> _orbs = new();

    private void Awake()
    {
        _input = new();
        _movement = GetComponent<CharacterMovement>();
        _baseWalkSpeed = _movement.WalkSpeed;

        _input.Player.Shoot.performed += OnShoot;
        _input.Player.Pull.performed += OnPull;

        if (_CurrentElement != null)
        {
            SetElement(_CurrentElement);
        }
    }

    private void OnEnable() => _input.Enable();
    private void OnDisable() => _input.Disable();

    private void Update()
    {
        _movement.Walk(_input.Player.Move.ReadValue<Vector2>());
    }

    public void SetElement(ElementSO element)
    {
        _CurrentElement = element;
        _movement.WalkSpeed = _baseWalkSpeed * element.MoveSpeedMultiplier;
    }

    private void OnShoot(InputAction.CallbackContext context)
    {
        if (_orbs.Count >= _MaxOrbs) return;

        Vector2 aimDir = GetMouseDirection();
        WispOrb orb = Instantiate(_OrbPrefab, transform.position, Quaternion.identity);
        orb.Init(this, _CurrentElement, aimDir);
        _orbs.Add(orb);
    }

    private void OnPull(InputAction.CallbackContext context)
    {
        for (int i = 0; i < _orbs.Count; i++)
        {
            _orbs[i].Pull();
        }
    }

    public void CatchOrb(WispOrb orb, Vector2 impactDir, float knockbackForce)
    {
        _orbs.Remove(orb);
        _movement.ApplyKnockback(impactDir * knockbackForce);
    }

    private Vector2 GetMouseDirection()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        return ((Vector2)(mouseWorld - transform.position)).normalized;
    }
}