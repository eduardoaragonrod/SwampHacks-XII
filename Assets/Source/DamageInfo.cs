using UnityEngine;

[System.Serializable]
public record DamageInfo
{
    public float Damage;
    public Team Team;
    public Vector2 Direction;
    public float Knockback;

    public DamageInfo() { }
    public DamageInfo(float damage, Team team, Vector2 direction, float knockback)
    {
        Damage = damage;
        Team = team;
        Direction = direction.normalized;
        Knockback = knockback;
    }
}
