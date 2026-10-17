using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.VFX;

[RequireComponent(typeof(CharacterMovement))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(BoxCollider2D))]
public class Player : MonoBehaviour
{
	private static readonly int HitHash = Animator.StringToHash("Hit");
	private static readonly int SpawnRateProp = Shader.PropertyToID("SpawnRate");

	public static Player Instance { get; private set; }

	[SerializeField] private WispOrb _OrbPrefab;
	[SerializeField] private int _MaxOrbs = 6;
	[SerializeField] private Light2D AuraLight;
	[SerializeField] private Element DefaultElement = Element.Fire;
	[SerializeField] private Animator Animator;
	[SerializeField] private GameObject LightningPrefab;
	[SerializeField] private GameObject LightningBetweenVFXPrefab;
	[SerializeField] private VisualEffect TrailVFX;

	[Space]
	[SerializeField] private float LightningDashDamageRadius = 0.5f;
	[SerializeField] private float LightningDashDamage = 5f, LightningDashKnockback = 10f;

	[Header("Critical Speed & Dynamic Trail Settings")]
	[SerializeField] private float criticalSpeedThreshold = 11f;
	[SerializeField] private float maxExpectedSpeed = 24f;
	[SerializeField] private float minTrailSpawnRate = 0f;
	[SerializeField] private float baseTrailSpawnRate = 32f;
	[SerializeField] private float maxTrailSpawnRate = 250f;
	[SerializeField] private float criticalRamDamage = 15f;
	[SerializeField] private float criticalRamKnockback = 14f;

	public bool IsCriticalSpeed { get; private set; }
	public float CurrentSpeed { get; private set; }

	private PlayerInputMap _input;
	private CharacterMovement _movement;
	private Health _health;
	private float _baseWalkSpeed;
	private readonly List<WispOrb> _orbs = new();
	private ElementSO _currentElement;
	private BoxCollider2D _hitbox;

	private float _nextAllowedDashTime;
	private Vector2 _lastMoveDir = Vector2.up;
	private readonly object _dashDamageImmunityBlocker = new();

	private Vector2 _fallbackAimDir = Vector2.down;

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

		_hitbox = GetComponent<BoxCollider2D>();
	}

	private void OnEnable() => _input.Enable();
	private void OnDisable() => _input.Disable();

	private void Update()
	{
		UpdateSpeedAndTrail();

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

	private void UpdateSpeedAndTrail()
	{
		if (_movement != null && _movement.Rigidbody != null)
		{
			CurrentSpeed = _movement.Rigidbody.linearVelocity.magnitude;
		}

		IsCriticalSpeed = CurrentSpeed >= criticalSpeedThreshold;

		if (TrailVFX != null)
		{
			float spawnRate;
			if (IsCriticalSpeed)
			{
				float critT = Mathf.InverseLerp(criticalSpeedThreshold, maxExpectedSpeed, CurrentSpeed);
				spawnRate = Mathf.Lerp(baseTrailSpawnRate, maxTrailSpawnRate, critT);
			}
			else
			{
				float walkT = Mathf.InverseLerp(0.1f, criticalSpeedThreshold, CurrentSpeed);
				spawnRate = Mathf.Lerp(minTrailSpawnRate, baseTrailSpawnRate, walkT);
			}

			TrailVFX.SetFloat(SpawnRateProp, spawnRate);
		}
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		TryRamEnemy(collision.gameObject);
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		TryRamEnemy(other.gameObject);
	}

	private void TryRamEnemy(GameObject target)
	{
		if (!IsCriticalSpeed) return;

		if (((1 << target.layer) & Layers.EnemyMask) == 0) return;

		if (target.TryGetComponent(out IDamageable dmg))
		{
			Vector2 hitDir = ((Vector2)(target.transform.position - transform.position)).normalized;
			if (hitDir.sqrMagnitude == 0f) hitDir = _lastMoveDir;

			float speedMultiplier = CurrentSpeed / Mathf.Max(1f, criticalSpeedThreshold);

			dmg.TryTakeDamage(new DamageInfo(
				criticalRamDamage * speedMultiplier,
				Team.Friendly,
				hitDir,
				criticalRamKnockback * speedMultiplier
			));
		}
	}

	private void OnDestroy()
	{
		ElementManager.OnChanged -= OnElementChanged;
		Instance = null;
	}

	private void OnElementChanged(Element element)
	{
		var data = ElementManager.GetData(element);
		_currentElement = data;
		_movement.WalkSpeed = _baseWalkSpeed * data.MoveSpeedMultiplier;
		AuraLight.color = data.Color;
		Animator.SetTrigger(element.ToString());
		TrailVFX.SetGradient("Color", data.DashTrail);
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
			var data = ElementManager.CurrentData;

			_nextAllowedDashTime = Time.time + data.DashCooldown;
			Audio.Play(ElementManager.CurrentData.DashSound, transform.position);

			switch (ElementManager.Current)
			{
				case Element.Lightning:
				{
					var instance = Instantiate(LightningPrefab, transform.position, Quaternion.identity);
					instance.GetComponent<LightningStrike>().Strike();

					instance = Instantiate(LightningBetweenVFXPrefab, transform.position, Quaternion.identity);
					var vfx = instance.GetComponent<VisualEffect>();
					vfx.SetVector2("Start", transform.position);

					Vector3 newPos = _movement.Rigidbody.position + _lastMoveDir * data.DashDistance;

					var hitEnemies = new List<Collider2D>();
					hitEnemies.AddRange(Physics2D.OverlapCircleAll(transform.position, LightningDashDamageRadius, Layers.EnemyMask));
					hitEnemies.AddRange(Physics2D.OverlapCircleAll(newPos, LightningDashDamageRadius, Layers.EnemyMask));

					foreach (var enemy in hitEnemies)
					{
						if (enemy.TryGetComponent(out IDamageable dmg))
						{
							dmg.TryTakeDamage(new DamageInfo(LightningDashDamage, Team.Friendly, _lastMoveDir, LightningDashKnockback));
						}
					}

					var hit = Physics2D.BoxCast(_hitbox.bounds.center, _hitbox.bounds.size, 0f, _lastMoveDir, data.DashDistance, Layers.ObstacleMask);
					if (hit.collider != null)
					{
						newPos = hit.point;
					}

					vfx.SetVector2("End", newPos);

					instance = Instantiate(LightningPrefab, newPos, Quaternion.identity);
					instance.GetComponent<LightningStrike>().Strike();

					vfx.Play();
					vfx.gameObject.AddComponent<DestroyOnVFXFinish>();

					_movement.Rigidbody.position = newPos;

					break;
				}
				default:
				{
					_health.RegisterDamageBlocker(_dashDamageImmunityBlocker);
					_movement.Dash(_lastMoveDir, data.DashSpeed, data.DashDistance);
					break;
				}
			}
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
		if (Gamepad.current != null)
		{
			var gamepadAim = _input.Player.Aim.ReadValue<Vector2>();

			if (gamepadAim.sqrMagnitude == 0)
			{
				return _fallbackAimDir;
			}
			else
			{
				_fallbackAimDir = gamepadAim.normalized;
			}

			return gamepadAim.normalized;
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