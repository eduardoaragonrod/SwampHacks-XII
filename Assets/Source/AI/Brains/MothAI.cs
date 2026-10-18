using UnityEngine;

public class MothAI : EnemyAI
{
	protected override State OnConstruct()
	{
		Health.OnDeath += OnDeath;

		var chase = new MoveToPlayer();
		Register(chase);

		return chase;
	}

	private void OnDeath()
	{
		
	}
}
