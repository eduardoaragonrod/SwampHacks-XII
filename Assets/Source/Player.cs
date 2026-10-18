using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.VFX;

[RequireComponent(typeof(CharacterMovement))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(BoxCollider2D))]
public class Player : MonoBehaviour
{
	private static readonly int HitHash = Animator.StringToHash("Hit");

	public static Player Instance { get; private set; }

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

	[Header("Critical Speed & GPU Trail Manager")]
	[SerializeField] private float criticalSpeedBonusThreshold = 3.5f;
	[SerializeField] private float maxExpectedSpeed = 22f;
	[SerializeField] private float dashTrailSpawnRate = 64f;
	[SerializeField] private float minSuperSpeedTrailRate = 60f;
	[SerializeField] private float maxTrailSpawnRate = 260f;
	[SerializeField] private float criticalRamDamage = 12f;
	[SerializeField] private float criticalRamKnockback = 16f;
	[SerializeField] private float ramHitCooldownPerEnemy = 0.25f;

	[Space]

	[SerializeField]
	private GameObject FloatingOrbPrefab;
	[SerializeField]
	private float FloatingOrbCatchupRate = 0.1f;
	[SerializeField]
	private float FloatingOrbRotationRate = 0.5f;
	[SerializeField]
	private float FloatingOrbDistance = 0.5f;
	[SerializeField]
	private float ShootInterval = 0.2f;
	[SerializeField]
	private AudioResource CatchOrbSound;

	public bool IsCriticalSpeed { get; private set; }
	public float CurrentSpeed { get; private set; }

	private PlayerInputMap _input;
	private CharacterMovement _movement;
	private Health _health;
	private float _baseWalkSpeed;
	private readonly List<WispOrb> _orbs = new();
	private ElementSO _currentElement;
	private BoxCollider2D _hitbox;

	private bool _wantsToShoot;
	private float _nextAllowedDashTime;
	private bool _isDashing;
	private Vector2 _lastMoveDir = Vector2.up;
	private readonly object _dashDamageImmunityBlocker = new();

	private Vector2 _fallbackAimDir = Vector2.down;
	private Vector2 _lastFixedPosition;
	private float _baseAuraLightIntensity = 1f;
	private readonly Dictionary<GameObject, float> _recentlyRammedEnemies = new();

	private readonly List<Transform> _floatingOrbs = new();
	private float _nextShootAllowedTime;

	private void Awake()
	{
		Instance = this;

		_input = new();
		_movement = GetComponent<CharacterMovement>();
		_baseWalkSpeed = _movement.WalkSpeed;
		if (AuraLight != null) _baseAuraLightIntensity = AuraLight.intensity;

		_input.Player.Shoot.performed += OnShoot;
		_input.Player.Shoot.canceled += OnStopShoot;

		_input.Player.Pull.performed += OnPull;
		_input.Player.Dash.performed += OnDash;

		ElementManager.OnChanged += OnElementChanged;
		ElementManager.SetElement(DefaultElement);

		_health = GetComponent<Health>();
		_health.OnDamaged += OnDamaged;

		_movement.OnDashEnd += OnDashEnd;

		_hitbox = GetComponent<BoxCollider2D>();
		_lastFixedPosition = transform.position;

		for (int i = 0; i < _MaxOrbs; i++)
		{
			_floatingOrbs.Add(Instantiate(FloatingOrbPrefab).transform);
		}

		var data = ElementManager.CurrentData;
		foreach (var floatingOrb in _floatingOrbs)
		{
			floatingOrb.GetComponent<SpriteRenderer>().color = data.Color;
		}
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

		if (_wantsToShoot && Time.time > _nextShootAllowedTime)
		{
			_nextShootAllowedTime = Time.time + ShootInterval;
			Shoot();
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
		else if (Keyboard.current.digit4Key.wasPressedThisFrame)
		{
			ElementManager.SetElement(Element.Blood);
		}
		else if (Keyboard.current.digit5Key.wasPressedThisFrame)
		{
			ElementManager.SetElement(Element.Poison);
		}
#endif
	}

	private void FixedUpdate()
	{
		Vector2 currentPos = _movement.Rigidbody != null ? _movement.Rigidbody.position : (Vector2)transform.position;
		float displacementSpeed = Vector2.Distance(currentPos, _lastFixedPosition) / Mathf.Max(0.0001f, Time.fixedDeltaTime);
		_lastFixedPosition = currentPos;

		float rbSpeed = _movement.Rigidbody != null ? _movement.Rigidbody.linearVelocity.magnitude : 0f;
		CurrentSpeed = Mathf.Max(displacementSpeed, rbSpeed);

		float criticalThreshold = _movement.WalkSpeed + criticalSpeedBonusThreshold;
		IsCriticalSpeed = CurrentSpeed >= criticalThreshold;

		if (IsCriticalSpeed)
		{
			CheckCriticalSpeedRamming(criticalThreshold);
		}
	}

	private void LateUpdate()
	{
		if (TrailVFX != null)
		{
			float criticalThreshold = _movement.WalkSpeed + criticalSpeedBonusThreshold;
			float spawnRate = 0f;

			// Emit particles when either in Critical Speed OR actively Dashing
			if (IsCriticalSpeed)
			{
				float speedRatio = Mathf.InverseLerp(criticalThreshold, maxExpectedSpeed, CurrentSpeed);
				spawnRate = Mathf.Lerp(minSuperSpeedTrailRate, maxTrailSpawnRate, speedRatio);
			}
			else if (_isDashing)
			{
				spawnRate = dashTrailSpawnRate;
			}

			TrailVFX.SetFloat("SpawnRate", spawnRate);
		}

		if (AuraLight != null)
		{
			float targetIntensity = (IsCriticalSpeed || _isDashing) ? _baseAuraLightIntensity * 2.2f : _baseAuraLightIntensity;
			AuraLight.intensity = Mathf.Lerp(AuraLight.intensity, targetIntensity, Time.deltaTime * 12f);
		}

		UpdateFloatingOrbs();
	}

	private void OnDestroy()
	{
		ElementManager.OnChanged -= OnElementChanged;
		Instance = null;

		foreach (var orb in _floatingOrbs)
		{
			if (orb != null)
			{
				Destroy(orb.gameObject);
			}
		}
	}

	private void CheckCriticalSpeedRamming(float criticalThreshold)
	{
		Vector2 boxSize = _hitbox.bounds.size * 1.25f;
		Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(_hitbox.bounds.center, boxSize, 0f, Layers.EnemyMask);

		foreach (var enemy in hitEnemies)
		{
			GameObject enemyObj = enemy.gameObject;
			if (_recentlyRammedEnemies.TryGetValue(enemyObj, out float lastHitTime) && Time.time - lastHitTime < ramHitCooldownPerEnemy)
			{
				continue;
			}

			if (enemy.TryGetComponent(out IDamageable dmg))
			{
				_recentlyRammedEnemies[enemyObj] = Time.time;

				Vector2 hitDir = ((Vector2)(enemy.transform.position - transform.position)).normalized;
				if (hitDir.sqrMagnitude == 0f) hitDir = _lastMoveDir;

				float speedMultiplier = CurrentSpeed / Mathf.Max(1f, criticalThreshold);

				dmg.TryTakeDamage(new DamageInfo(
					criticalRamDamage * speedMultiplier,
					Team.Friendly,
					hitDir,
					criticalRamKnockback * speedMultiplier
				));
			}
		}
	}

	private void OnElementChanged(Element element)
	{
		var data = ElementManager.GetData(element);
		_currentElement = data;
		_movement.WalkSpeed = _baseWalkSpeed * data.MoveSpeedMultiplier;
		AuraLight.color = data.Color;
		Animator.SetTrigger(element.ToString());
		TrailVFX.SetGradient("Color", data.DashTrail);

		foreach (var floatingOrb in _floatingOrbs)
		{
			floatingOrb.GetComponent<SpriteRenderer>().color = data.Color;
		}
	}

	private void OnShoot(InputAction.CallbackContext context)
	{
		_wantsToShoot = true;
	}

	private void OnStopShoot(InputAction.CallbackContext context)
	{
		_wantsToShoot = false;
	}

	private async void Shoot()
	{
		if (_orbs.Count >= _MaxOrbs)
		{
			return;
		}

		Audio.Play(ElementManager.CurrentData.ShootSound, transform.position);

		Vector2 aimDir = GetAimDirection();
		Transform bestFloatingOrb = _floatingOrbs[Random.Range(0, _floatingOrbs.Count)];
		float bestDotProduct = float.NegativeInfinity;

		foreach (var floatingOrb in _floatingOrbs)
		{
			var dirToOrb = (floatingOrb.position - transform.position).normalized;
			float dot = Vector2.Dot(dirToOrb, aimDir);

			if (dot > bestDotProduct)
			{
				bestDotProduct = dot;
				bestFloatingOrb = floatingOrb;
			}
		}

		_floatingOrbs.Remove(bestFloatingOrb);
		await bestFloatingOrb.DOMove(transform.position, 0.2f).AsyncWaitForCompletion();

		Destroy(bestFloatingOrb.gameObject);
		WispOrb orb = Instantiate(ElementManager.CurrentData.OrbPrefab, bestFloatingOrb.position, Quaternion.identity).GetComponent<WispOrb>();
		orb.Init(this, _currentElement, aimDir);
		_orbs.Add(orb);
	}

	private void OnPull(InputAction.CallbackContext context)
	{
		_wantsToShoot = false;

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
					_isDashing = true;
					_health.RegisterDamageBlocker(_dashDamageImmunityBlocker);
					_movement.Dash(_lastMoveDir, data.DashSpeed, data.DashDistance);
					break;
				}
			}
		}
	}

	private void OnDashEnd()
	{
		_isDashing = false;
		_health.UnregisterDamageBlocker(_dashDamageImmunityBlocker);
	}

	public void CatchOrb(WispOrb orb, Vector2 impactDir, float knockbackForce)
	{
		_orbs.Remove(orb);
		_movement.ApplyKnockback(impactDir * knockbackForce);
		_floatingOrbs.Add(Instantiate(FloatingOrbPrefab, transform.position, Quaternion.identity).transform);
		_floatingOrbs[^1].GetComponent<SpriteRenderer>().color = ElementManager.CurrentData.Color;

		Audio.Play(CatchOrbSound, transform.position);
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

	private async void OnDamaged(DamageInfo info)
	{
		_isDashing = false;
		Animator.SetTrigger(HitHash);
		_movement.StopDash();
	}

	private void UpdateFloatingOrbs()
	{
		var data = ElementManager.CurrentData;

		for (int i = 0; i < _floatingOrbs.Count; i++)
		{
			float angle = Mathf.PI * 2f / _floatingOrbs.Count * i + (Time.time * FloatingOrbRotationRate * data.FloatingOrbsRotationFactor);
			var dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle));

			var desired = transform.position + (dir * FloatingOrbDistance);
			_floatingOrbs[i].position = Vector3.Lerp(_floatingOrbs[i].position, desired, Time.deltaTime * FloatingOrbCatchupRate);
		}
	}
}
