using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(CharacterMovement))]
public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    [SerializeField] private WispOrb _OrbPrefab;
    [SerializeField] private int _MaxOrbs = 6;
    [SerializeField] private Light2D AuraLight;
    [SerializeField] private Element DefaultElement = Element.Fire;

    private PlayerInputMap _input;
    private CharacterMovement _movement;
    private float _baseWalkSpeed;
    private readonly List<WispOrb> _orbs = new();
    private ElementSO _currentElement;

    private void Awake()
    {
        Instance = this;

        _input = new();
        _movement = GetComponent<CharacterMovement>();
        _baseWalkSpeed = _movement.WalkSpeed;

        _input.Player.Shoot.performed += OnShoot;
        _input.Player.Pull.performed += OnPull;

        ElementManager.OnChanged += OnElementChanged;
        ElementManager.SetElement(DefaultElement);
    }

    private void OnEnable() => _input.Enable();
    private void OnDisable() => _input.Disable();

    private void Update()
    {
        _movement.Walk(_input.Player.Move.ReadValue<Vector2>());

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
}
