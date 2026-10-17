using UnityEngine;

public class MothAI : EnemyAI
{
    protected override State OnConstruct()
    {
        var chase = new MoveToPlayer();
        Register(chase);

        return chase;
    }
}
