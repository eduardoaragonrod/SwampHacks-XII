public abstract class State
{
    public virtual void OnEnter() { }
    public virtual bool OnUpdate() => true;
    public virtual void OnExit() { }
}
