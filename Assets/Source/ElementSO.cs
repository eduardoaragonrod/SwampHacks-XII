using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Element")]
public class ElementSO : ScriptableObject
{
    public Color Color = Color.white;
    [GradientUsage(true)]
    public Gradient DashTrail;
    public Sprite EnergyFrame;
    public float Damage = 10f;
    public float MoveSpeedMultiplier = 1f;
    public float LaunchSpeed = 18f;
    public float PullAcceleration = 45f;
    public float KnockbackScale = 1.5f;

    [Header("Dashing")]

    public AudioResource DashSound;
    public float DashSpeed = 15f;
    public float DashDistance = 4.5f;
    public float DashCooldown = 0.5f;
}
