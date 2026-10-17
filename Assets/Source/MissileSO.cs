using UnityEngine;

[CreateAssetMenu(fileName = "Missile")]
public class MissileSO : ScriptableObject
{
    public float BaseSpeed = 5f;
    public float Acceleration = 0f;
    public float MinSpeed = 0f;
    public float MaxSpeed = float.PositiveInfinity;

    public Team Team = Team.Hostile;
}
