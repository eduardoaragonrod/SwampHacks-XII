public class MoveToPlayer : State
{
    private readonly float _minDst;

    public MoveToPlayer(float minDistance = 0.5f)
    {
        _minDst = minDistance;
    }

    public override bool OnUpdate()
    {
        var playerPos = Player.Instance.transform.position;
        Owner.Movement.Walk((playerPos - Owner.transform.position).normalized);

        return (Owner.transform.position - playerPos).magnitude <= _minDst;
    }
}
