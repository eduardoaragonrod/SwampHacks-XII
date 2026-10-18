using UnityEngine;
using UnityEngine.VFX;

public class WispOrb : MonoBehaviour
{
	[SerializeField] private SpriteRenderer _renderer;
	[SerializeField] private float _Drag = 6f;
	[SerializeField] private float _CatchRadius = 0.6f;
	[SerializeField] private float Knockback = 5f;
	[SerializeField] private VisualEffect DetachOnDeath;

	private Player _owner;
	private ElementSO _element;
	private Vector2 _velocity;
	private bool _isPulling;
	private float _pullSpeed;
	private float _distancePulled;

	public void Init(Player owner, ElementSO element, Vector2 direction)
	{
		_owner = owner;
		_element = element;
		_velocity = direction * element.LaunchSpeed;

		if (_renderer != null)
		{
			_renderer.color = element.Color;
		}
	}

	public void Pull()
	{
		if (_isPulling) return;

		_isPulling = true;
		_pullSpeed = _velocity.magnitude;
		_distancePulled = 0f;
	}

	private void Update()
	{
		var hit = Physics2D.CircleCast(transform.position, _CatchRadius, _velocity.normalized, _velocity.magnitude * Time.deltaTime, Layers.ObstacleMask);

		if (!_isPulling)
		{
			if (hit.collider == null)
			{
				transform.position += (Vector3)(_velocity * Time.deltaTime);
				_velocity = Vector2.MoveTowards(_velocity, Vector2.zero, _Drag * Time.deltaTime);
			}
			else
			{
				_velocity = Vector2.zero;
			}

			return;
		}

		Vector2 toOwner = _owner.transform.position - transform.position;
		float distance = toOwner.magnitude;
		Vector2 dir = toOwner.normalized;

		_pullSpeed += _element.PullAcceleration * Time.deltaTime;
		float step = _pullSpeed * Time.deltaTime;
		_distancePulled += Mathf.Min(step, distance);

		transform.position += ((Vector3)(dir * step) * 3) / 2;

		if (distance <= _CatchRadius)
		{
			_owner.CatchOrb(this, dir, _distancePulled * _element.KnockbackScale);
			OnDeath();
			Destroy(gameObject);
		}
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.TryGetComponent(out IDamageable target))
		{
			var info = new DamageInfo()
			{
				Damage = _element.Damage,
				Direction = (other.transform.position - transform.position).normalized,
				Knockback = Knockback,
				Team = Team.Friendly
			};

			target.TryTakeDamage(info);

			var data = ElementManager.CurrentData;
			if (data.ApplyStatus && other.TryGetComponent(out StatusManager statusManager))
			{
				statusManager.Apply(data.StatusEffect, data.StatusDuration);
			}
		}
	}

	private void OnDeath()
	{
		if (DetachOnDeath != null)
		{
			DetachOnDeath.transform.SetParent(null, true);
			DetachOnDeath.gameObject.AddComponent<DestroyOnVFXFinish>();
			DetachOnDeath.Stop();
		}
	}
}
