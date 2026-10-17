using UnityEngine;

[RequireComponent(typeof(CharacterMovement))]
public class Player : MonoBehaviour
{
    private PlayerInputMap _input;
    private CharacterMovement _movement;

    private void Awake()
    {
        _input = new();
        _movement = GetComponent<CharacterMovement>();
    }

    private void OnEnable()
    {
        _input.Enable();
    }

    private void Update()
    {
        _movement.Walk(_input.Player.Move.ReadValue<Vector2>());
    }

    private void OnDisable()
    {
        _input.Disable();
    }
}
