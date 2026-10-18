using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

public class ArmageddonDirector : MonoBehaviour
{
	[SerializeField]
	private StageProgressionManager StageManager;

	[SerializeField]
	private GameObject FireVFX;
	[SerializeField]
	private AudioResource FireBoomSound;
	[SerializeField]
	private GameObject LightningBetween, LightningDown;
	[SerializeField]
	private AudioResource LightningStrikeSound;
	[SerializeField]
	private GameObject BloodVFX;
	[SerializeField]
	private GameObject BlizzardVFX, BlizzardDeathVFX;
	[SerializeField]
	private GameObject PoisonPuddle, PoisonPuddleMask;

	private float _startingTime;
	private bool _executing;

	private void OnEnable()
	{
		_startingTime = Time.time;
	}

	private void Update()
	{
		float armageddonStartTime = 20f * (StageManager.CurrentStageIndex + 1) - 5f;
		float time = Time.time - _startingTime;

		if (time >= armageddonStartTime)
		{
			if (!_executing)
			{
				_executing = true;

				BlackHoleEncounterController.PreventSpawning = true;
				DestroyAllEnemies();
			}
		}
		else
		{
			_executing = false;
			BlackHoleEncounterController.PreventSpawning = false;
		}

		if (Keyboard.current.jKey.wasPressedThisFrame)
		{
			ElementManager.SetElement(Element.Poison);
			DestroyAllEnemies();
		}
	}

	private void DestroyAllEnemies()
	{
		var enemies = BlackHoleEncounterController.ActiveEnemies;
		Player player = Player.Instance;

		switch (ElementManager.Current)
		{
			case Element.Fire:
			{
				Audio.Play(FireBoomSound, player.transform.position);

				foreach (var enemy in enemies)
				{
					var vfx = Instantiate(FireVFX, enemy.transform.position, Quaternion.identity);
					vfx.AddComponent<DestroyOnVFXFinish>();
					enemy.GetComponent<Health>().TryTakeDamage(new(1000f, Team.Environment, default, 0f));
				}
				break;
			}
			case Element.Lightning:
			{
				GameObject closest = null;
				float closestDst = float.PositiveInfinity;

				foreach (var enemy in enemies)
				{
					float dst = Vector2.Distance(player.transform.position, enemy.transform.position);
					if (dst < closestDst)
					{
						closest = enemy;
						closestDst = dst;
					}
				}

				if (closest == null)
				{
					closest = enemies.ElementAt(Random.Range(0, enemies.Count()));
				}

				RecursiveElectrocute(closest.GetComponent<Health>(), enemies.ToHashSet(), new());
				break;
			}
			case Element.Ice:
			{
				var vfx = Instantiate(BlizzardVFX, transform.position, Quaternion.identity).GetComponent<VisualEffect>();
				vfx.gameObject.AddComponent<DestroyOnVFXFinish>();

				foreach (var enemy in enemies)
				{
					var v = Instantiate(BlizzardDeathVFX, enemy.transform.position, Quaternion.identity);
					v.AddComponent<DestroyOnVFXFinish>();
					enemy.GetComponent<Health>().TryTakeDamage(new(1000f, Team.Environment, default, 0f));
				}

				break;
			}
			case Element.Blood:
			{
				var bloodVFX = new List<(Transform Enemy, VisualEffect VFX)>();

				foreach (var enemy in enemies)
				{
					var vfx = Instantiate(BloodVFX, enemy.transform.position, Quaternion.identity).GetComponent<VisualEffect>();
					vfx.Play();
					bloodVFX.Add((enemy.transform, vfx));
				}

				StartCoroutine(TrackBlood(bloodVFX));
				break;
			}
			case Element.Poison:
			{
				StartCoroutine(PoisonDeath(enemies));
				break;
			}
		}
	}

	private IEnumerator PoisonDeath(IEnumerable<GameObject> enemies)
	{
		var puddles = new List<GameObject>();
		var masks = new List<GameObject>();

		foreach (var enemy in enemies)
		{
			puddles.Add(Instantiate(PoisonPuddle, enemy.transform.position, Quaternion.identity));
			masks.Add(Instantiate(PoisonPuddleMask, enemy.transform.position, Quaternion.identity));

			enemy.GetComponent<EnemyAI>().enabled = false;
			if (enemy.TryGetComponent(out FacePlayer facePlayer)) facePlayer.enabled = false;
			enemy.GetComponent<Collider2D>().enabled = false;
			if (enemy.TryGetComponent(out DamageOnContact dmgOnContact)) dmgOnContact.enabled = false;
			enemy.GetComponent<CharacterMovement>().enabled = false;
			enemy.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
		}

		foreach (var mask in masks)
		{
			mask.transform.DOMoveY(mask.transform.position.y + 5f, 5f).SetEase(Ease.InCubic);
		}

		yield return new WaitForSeconds(5f);

		foreach (var enemy in enemies)
		{
			enemy.GetComponent<Health>().TryTakeDamage(new(1000f, Team.Environment, default, 0f));
		}

		foreach (var mask in masks)
		{
			Destroy(mask);
		}

		foreach (var puddle in puddles)
		{
			puddle.GetComponent<SpriteRenderer>().DOFade(0f, 0.5f)
				.OnComplete(() => Destroy(puddle));
		}
	}

	private IEnumerator TrackBlood(List<(Transform Enemy, VisualEffect VFX)> bloodVFX)
	{
		var playerHealth = Player.Instance.GetComponent<Health>();

		for (float t = 0f; t < 5f; t += Time.unscaledDeltaTime)
		{
			playerHealth.Heal(10f * Time.unscaledDeltaTime);
			var playerPos = Player.Instance.transform.position;

			foreach (var (Enemy, VFX) in bloodVFX)
			{
				if (Enemy == null) continue;

				Vector2 diff = Enemy.position - playerPos;
				Vector2 perp = diff.normalized;
				perp.x *= -1f;

				VFX.SetVector2("StartBias", Vector2.Lerp(Enemy.position, playerPos, 0.25f) + (0.5f * diff.magnitude * perp));
				VFX.SetVector2("EndBias", Vector2.Lerp(Enemy.position, playerPos, 0.75f) + (0.5f * diff.magnitude * -perp));

				VFX.SetVector2("Start", Enemy.position);
				VFX.SetVector2("End", playerPos);
			}

			yield return null;
		}

		foreach (var (Enemy, VFX) in bloodVFX)
		{
			Enemy.GetComponent<Health>().TryTakeDamage(new(1000f, Team.Environment, default, 0f));
			VFX.Stop();
			VFX.gameObject.AddComponent<DestroyOnVFXFinish>();
		}
	}

	private async void RecursiveElectrocute(Health origin, HashSet<GameObject> all, HashSet<Health> struck)
	{
		struck.Add(origin);
		all.Remove(origin.gameObject);

		Audio.Play(LightningStrikeSound, origin.transform.position);

		var lightning = Instantiate(LightningDown, origin.transform.position, Quaternion.identity).GetComponent<LightningStrike>();
		lightning.Strike();

		GameObject closest = null;
		float closestDst = float.PositiveInfinity;

		await Awaitable.WaitForSecondsAsync(0.1f);

		foreach (var enemy in all)
		{
			float dst = Vector2.Distance(Player.Instance.transform.position, enemy.transform.position);
			if (dst < closestDst)
			{
				closest = enemy;
				closestDst = dst;
			}
		}

		if (all.Count == 0)
		{
			origin.TryTakeDamage(new(1000f, Team.Environment, default, 0f));
			return;
		}

		if (closest == null)
		{
			closest = all.ElementAt(Random.Range(0, all.Count));
		}

		var vfx = Instantiate(LightningBetween).GetComponent<VisualEffect>();
		vfx.SetVector2("Start", origin.transform.position);
		vfx.SetVector2("End", closest.transform.position);
		vfx.Play();
		vfx.gameObject.AddComponent<DestroyOnVFXFinish>();

		origin.TryTakeDamage(new(1000f, Team.Environment, default, 0f));

		RecursiveElectrocute(closest.GetComponent<Health>(), all, struck);
	}
}
