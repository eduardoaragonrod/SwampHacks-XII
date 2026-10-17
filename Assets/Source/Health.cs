using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [field: SerializeField]
    public Team Team { get; private set; }

    public bool TryTakeDamage(DamageInfo info)
    {
        throw new System.NotImplementedException();
    }
}