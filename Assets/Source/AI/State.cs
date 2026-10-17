public abstract class State
{
    public EnemyAI Owner
    {
        get
        {
            if (_owner == null)
            {
                throw new System.NullReferenceException("State doesn't have owner set! You probablby forgot to call Register for this state!");
            }

            return _owner;
        }
    }
    private EnemyAI _owner;

    public virtual void OnEnter() { }
    public virtual bool OnUpdate() => true;
    public virtual void OnExit() { }

    public void SetOwner(EnemyAI owner)
    {
        _owner = owner;
    }
}
