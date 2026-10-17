using UnityEngine;

[CreateAssetMenu(fileName = "Missile")]
public class MissileSO : ScriptableObject
{
    public float BaseSpeed = 5f;
    public float Acceleration = 0f;
    public float MinSpeed = 0f;
    public float MaxSpeed = float.PositiveInfinity;

    [Space]

    public DamageInfo DamageInfo;
    public float CollisionRadius = 0.2f;

    [Space]

    public Sprite OverrideSprite;
    public GameObject AttachedPrefab;
    public bool DetachPrefabOnDeath;
    [Tooltip("In seconds.")]
    public float TimeToExpireAfterDetach = 3f;

    [Space]

    public float LightIntensity = 0f;
    public float LightRadius = 2f;
    public Color LightColor = Color.white;
    public float LightFadeOnDeathDuration = 0.5f;
}
