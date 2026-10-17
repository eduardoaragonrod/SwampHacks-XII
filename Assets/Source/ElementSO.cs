using UnityEngine;

[CreateAssetMenu(menuName = "Wisp/Element")]
public class ElementSO : ScriptableObject
{
    [field: SerializeField] public Color Color { get; private set; } = Color.white;
    [field: SerializeField] public float Damage { get; private set; } = 10f;
    [field: SerializeField] public float MoveSpeedMultiplier { get; private set; } = 1f;
    [field: SerializeField] public float LaunchSpeed { get; private set; } = 18f;
    [field: SerializeField] public float PullAcceleration { get; private set; } = 45f;
    [field: SerializeField] public float KnockbackScale { get; private set; } = 1.5f;

    public virtual void OnHitEnemy(Player owner, WispOrb orb, IDamageable target) { }
    public virtual void OnReturned(Player owner, WispOrb orb, float distancePulled) { }
}