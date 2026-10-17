using UnityEngine;

[System.Serializable]
public record DamageInfo
{
    public float Damage;
    public Team TargetTeam;
    public Vector2 Direction;
    public float Knockback;
}
