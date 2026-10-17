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

    public static void Spawn(MissileSO misileData, Vector2 origin, Vector2 directon)
    {
        var instance = Instantiate(_instance.MissilePrefab, origin, Quaternion.identity, _instance.transform);
        var missile = instance.GetComponent<Missile>();

        missile.Init(misileData, directon);
    }

    // TODO: Spawner method with options for patterns.
}
