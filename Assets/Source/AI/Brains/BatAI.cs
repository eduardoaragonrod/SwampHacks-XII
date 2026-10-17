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

		var targetPos = playerPos + (dirFromPlayer * Mathf.Lerp(2f, 4f, (Mathf.Sin(Time.time * 0.2f) + 1f) / 2f));
		var dirToDest = (targetPos - transform.position).normalized;

		if ((transform.position - targetPos).magnitude > 0.2f)
		{
			Movement.Walk(dirToDest);
		}

		if (Time.time > _nextShootTime)
		{
			Shoot();
			_nextShootTime = Time.time + 3f;
		}
	}

	private void Shoot()
	{
		var target = Player.Instance.transform.position;
		MissileManager.Spawn(Missile, MissileOrigin.position, (target - transform.position).normalized, MissileCount, MissileSpawnPattern);
	}
}
