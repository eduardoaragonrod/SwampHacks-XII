using UnityEngine;
using UnityEngine.InputSystem;

public class BatAI : EnemyAI
{
	[SerializeField]
	private MissileSO Missile;
	[SerializeField]
	private Transform MissileOrigin;
	[SerializeField]
	private int MissileCount = 6;
	[SerializeField]
	private MissilePattern MissileSpawnPattern = MissilePattern.StraightShot;

	private float _nextShootTime;

	private void Update()
	{
		var playerPos = Player.Instance.transform.position;
		var dirFromPlayer = (transform.position - playerPos).normalized;

		var targetPos = playerPos + (dirFromPlayer * Mathf.Lerp(4f, 8f, (Mathf.Sin(Time.time * 0.2f) + 1f) / 2f));
		var dirToDest = (targetPos - transform.position).normalized;
		var dstToTarget = (transform.position - targetPos).magnitude;

		if (dstToTarget > 1f)
		{
			Movement.Walk(dirToDest);
		}

		if (Time.time > _nextShootTime && dstToTarget < 3f)
		{
			Shoot();
			_nextShootTime = Time.time + Random.Range(2f, 5f);
		}
	}

	private void Shoot()
	{
		var target = Player.Instance.transform.position;
		MissileManager.Spawn(Missile, MissileOrigin.position, (target - transform.position).normalized, MissileCount, MissileSpawnPattern);
	}
}
