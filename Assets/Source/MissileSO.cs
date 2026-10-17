using UnityEngine;

[CreateAssetMenu(fileName = "Missile")]
public class MissileSO : ScriptableObject
{
    public float BaseSpeed = 5f;
    public float Acceleration = 0f;
    public float MinSpeed = 0f;
    public float MaxSpeed = float.PositiveInfinity;

    [Space]

    public Team Team = Team.Hostile;
    public DamageInfo Damage;
    public float CollisionRadius = 0.2f;

    [Space]

    public Sprite Sprite;
    public GameObject AttachedPrefab;
    public bool DetachPrefabOnDeath;
    [Tooltip("In seconds.")]
    public float TimeToExpireAfterDetach = 3f;
}
