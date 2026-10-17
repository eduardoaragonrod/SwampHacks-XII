using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterMovement))]
public class Player : MonoBehaviour
{
	[SerializeField]
	private MissileSO TempMissile;

	private PlayerInputMap _input;
	private CharacterMovement _movement;

	private void Awake()
	{
		_input = new();
		_movement = GetComponent<CharacterMovement>();
		_input.Player.Shoot.performed += OnShoot;
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

	private void OnShoot(InputAction.CallbackContext context)
	{
		MissileManager.Spawn(TempMissile, transform.position, Random.onUnitCircle.normalized);
	}
}
