using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Usage: MissileManager.Spawn();
/// </summary>
public partial class MissileManager : MonoBehaviour
{
    [SerializeField]
    private GameObject MissilePrefab;
    [SerializeField]
    private MissileSO Temp;

    private static MissileManager _instance;

    [AutoStaticsCleanup]
    private static readonly List<Missile> _missiles = new();

    private void Awake()
    {
        _instance = this;
    }

    private void Update()
    {
        for (int i = _missiles.Count - 1; i >= 0; i--)
        {
            _missiles[i].Tick();
        }

        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            Spawn(Temp, default, Random.onUnitCircle.normalized, Random.Range(1, 32), MissilePattern.OmniExplosion);
        }
    }

    public static void Spawn(MissileSO misileData, Vector2 origin, Vector2 directon)
    {
        var instance = Instantiate(_instance.MissilePrefab, origin, Quaternion.identity, _instance.transform);
        var missile = instance.GetComponent<Missile>();

        _missiles.Add(missile);
        missile.Init(misileData, directon);
    }
    public static void Spawn(MissileSO missileData, Vector2 origin, Vector2 direction, int count, MissilePattern spawnPattern)
    {
        direction.Normalize();
        float deltaAngle = spawnPattern.SpreadAngle / count;

        for (int i = 0; i < count; i++)
        {
            Vector2 dir = Quaternion.AngleAxis(deltaAngle * i, Vector3.forward) * direction;
            Spawn(missileData, origin, dir);
        }
    }

    public static void Kill(Missile missile)
    {
        _missiles.Remove(missile);
        missile.OnDeath();
    }
}
