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
        _input.Player.Recall.performed += OnRecall;
	}

    private void OnEnable()
	{
		_input.Enable();
	}

	private void Update()
	{
		_movement.Walk(_input.Player.Move.ReadValue<Vector2>());

		if (Keyboard.current.hKey.wasPressedThisFrame)
		{
			_movement.ApplyKnockback(Random.onUnitCircle.normalized * Random.Range(10f, 20f));
		}
	}

	private void OnDisable()
	{
		_input.Disable();
	}

	private void OnShoot(InputAction.CallbackContext context)
	{
		MissileManager.Spawn(TempMissile, transform.position, Random.onUnitCircle.normalized);
	}

    private void OnRecall(InputAction.CallbackContext context)
    {
        // TODO: Implement recall.
    }
}
