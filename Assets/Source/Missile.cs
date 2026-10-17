using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(SpriteRenderer))]
public class Missile : MonoBehaviour
{
	public MissileSO MissileData { get; private set; }
	public Vector2 BaseDirection { get; private set; }

	public float Speed { get; private set; }
	public bool IsDead { get; private set; }

	private GameObject _attached;
	private Light2D _light;

	public void Init(MissileSO missile, Vector2 direction)
	{
		MissileData = missile;
		BaseDirection = direction.normalized;

		Speed = missile.BaseSpeed;

		GetComponent<SpriteRenderer>().sprite = MissileData.Sprite;

		if (MissileData.AttachedPrefab != null)
		{
			_attached = Instantiate(MissileData.AttachedPrefab, transform);
		}

		if (MissileData.LightIntensity > 0f)
		{
			var light = new GameObject("Light");
			light.transform.SetParent(transform, false);
			_light = light.AddComponent<Light2D>();
			_light.intensity = MissileData.LightIntensity;
			_light.pointLightOuterRadius = MissileData.LightRadius;
			_light.color = MissileData.LightColor;
			_light.shadowsEnabled = true;
		}
	}

	public void Tick()
	{
		var lastPos = transform.position;

		transform.Translate(Speed * Time.deltaTime * BaseDirection);
		Speed += Time.deltaTime * MissileData.Acceleration;
		Speed = Mathf.Clamp(Speed, MissileData.MinSpeed, MissileData.MaxSpeed);

		var deltaPos = transform.position - lastPos;
		var hits = Physics2D.CircleCastAll(lastPos, MissileData.CollisionRadius, deltaPos.normalized, deltaPos.magnitude, Layers.CharacterMask | Layers.ObstacleMask);

		bool hitAny = false;

		foreach (var hit in hits)
		{
			if (hit.collider.gameObject.layer == Layers.ObstacleLayer)
			{
				hitAny = true;
			}
			else if (hit.collider.TryGetComponent(out IDamageable dmg))
			{
				if (dmg.TryTakeDamage(MissileData.DamageInfo))
				{
                    hitAny = true;
                }
			}
		}

		if (hitAny)
		{
			Kill();
		}
	}

	public void Kill()
	{
		MissileManager.Kill(this);
	}

	public void OnDeath()
	{
		if (IsDead)
		{
			return;
		}

		if (MissileData.DetachPrefabOnDeath && _attached != null)
		{
			_attached.transform.SetParent(null, true);
			Destroy(_attached, MissileData.TimeToExpireAfterDetach);
		}

		if (_light != null)
		{
			_light.transform.SetParent(null, true);
			var seq = DOTween.Sequence();

			seq.Append(DOTween.To(() => _light.intensity, x => _light.intensity = x, 0f, MissileData.LightFadeOnDeathDuration));
			seq.AppendCallback(() => Destroy(_light.gameObject));
		}

		IsDead = true;
		Destroy(gameObject);
	}
}
