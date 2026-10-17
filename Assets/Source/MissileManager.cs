using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

/// <summary>
/// Usage: MissileManager.Spawn();
/// </summary>
public partial class MissileManager : MonoBehaviour
{
    [SerializeField]
    private GameObject MissilePrefab;

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
    }

    public static void Spawn(MissileSO misileData, Vector2 origin, Vector2 directon)
    {
        var instance = Instantiate(_instance.MissilePrefab, origin, Quaternion.identity, _instance.transform);
        var missile = instance.GetComponent<Missile>();

        _missiles.Add(missile);
        missile.Init(misileData, directon);
    }
    public static void Spawn(MissileSO missileData, Vector2 origin, Vector2 direction, MissilePattern spawnPattern)
    {
        throw new System.NotImplementedException("Uh oh. Not implemented yet sry!");
    }

    public static void Kill(Missile missile)
    {
        _missiles.Remove(missile);
    }
}
