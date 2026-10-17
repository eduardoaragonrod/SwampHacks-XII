using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [field: SerializeField]
    public Team Team { get; private set; }
    [field: SerializeField]
    public float Default { get; private set; } = 100f;

    public float Current { get; private set; }
    public float Max { get; private set; }

    public bool IsDead { get; private set; }

    public event System.Action<DamageInfo> OnDamaged;
    public event System.Action OnDeath;

    private readonly HashSet<object> _dmgBlockers = new();

    private void Awake()
    {
        Current = Max = Default;
    }

    public bool TryTakeDamage(DamageInfo info)
    {
        if (_dmgBlockers.Count > 0) return false;

        // No friendly fire.
        if (info.Team == Team)
        {
            return false;
        }

        if (IsDead)
        {
            return false;
        }

        Current = Mathf.Clamp(Current - info.Damage, 0f, Max);
        OnDamaged?.Invoke(info);

        if (Current == 0f)
        {
            IsDead = true;
            OnDeath?.Invoke();
        }

        return true;
    }

    public void RegisterDamageBlocker(object key)
    {
        _dmgBlockers.Add(key);
    }

    public void UnregisterDamageBlocker(object key)
    {
        _dmgBlockers.Remove(key);
    }
}
