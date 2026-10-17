public interface IDamageable
{
    public Team Team { get; }
    public bool TryTakeDamage(DamageInfo info);
}
