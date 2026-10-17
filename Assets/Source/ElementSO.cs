using UnityEngine;

[CreateAssetMenu(menuName = "Wisp/Element")]
public class ElementSO : ScriptableObject
{
    public Color Color = Color.white;
    public Sprite EnergyFrame;
    public float Damage = 10f;
    public float MoveSpeedMultiplier = 1f;
    public float LaunchSpeed = 18f;
    public float PullAcceleration = 45f;
    public float KnockbackScale = 1.5f;
}
