public interface IDamageable
{
    public Team Team { get; }

    /// <returns>True, if the damage was applied. False, if the damage was ignored.</returns>
    public bool TryTakeDamage(DamageInfo info);
}
