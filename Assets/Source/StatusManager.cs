using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(Health))]
public class StatusManager : MonoBehaviour
{
	[SerializeField]
	private VisualEffect BurningVFX, PoisonedVFX;
	[SerializeField]
	private float BurnInterval, BurnDamage, PoisonInterval, PoisonDamage;

	private float _nextBurnTime, _nextPoisonTime;

	private Health _health;
	private readonly Dictionary<StatusEffectType, float> _statusToEndTime = new();

	private void Awake()
	{
		_health = GetComponent<Health>();
	}

	private void Update()
	{
		var toRemove = new HashSet<StatusEffectType>();

		foreach (var entry in _statusToEndTime)
		{
			if (Time.time > entry.Value)
			{
				toRemove.Add(entry.Key);
			}
		}

		foreach (var status in toRemove)
		{
			Remove(status);
		}

		if (Has(StatusEffectType.Burning) && Time.time > _nextBurnTime)
		{
			_health.TryTakeDamage(new(BurnDamage, Team.Environment, Random.onUnitCircle.normalized, Random.Range(5f, 8f)));
			_nextBurnTime = Time.time + BurnInterval;
		}

		if (Has(StatusEffectType.Poisoned) && Time.time > _nextPoisonTime)
		{
			_health.TryTakeDamage(new(PoisonDamage, Team.Environment, Random.onUnitCircle.normalized, Random.Range(3f, 5f)));
			_nextPoisonTime = Time.time + PoisonInterval;
		}
	}

	public bool Has(StatusEffectType type)
	{
		return _statusToEndTime.ContainsKey(type);
	}

	public void Apply(StatusEffectType type, float duration)
	{
		if (_statusToEndTime.ContainsKey(type))
		{
			_statusToEndTime[type] = Mathf.Max(_statusToEndTime[type], Time.time + duration);
		}
		else
		{
			_statusToEndTime.Add(type, Time.time + duration);
			StatusToVFX(type).Play();

			if (type is StatusEffectType.Burning)
			{
				_nextBurnTime = Time.time + BurnInterval;
			}
			else if (type is StatusEffectType.Poisoned)
			{
				_nextPoisonTime = Time.time + PoisonInterval;
			}
		}
	}

	public void Remove(StatusEffectType type)
	{
		if (_statusToEndTime.Remove(type))
		{
			StatusToVFX(type).Stop();
		}
	}

	private VisualEffect StatusToVFX(StatusEffectType type) => type switch
	{
		StatusEffectType.Burning => BurningVFX,
		StatusEffectType.Poisoned => PoisonedVFX,
		_ => throw new System.NotImplementedException(),
	};
}
