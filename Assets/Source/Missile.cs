using UnityEngine;

public class Missile : MonoBehaviour
{
    public MissileSO MissileData { get; private set;  }
    public Vector2 BaseDirection { get; private set; }

    public void Init(MissileSO missile, Vector2 direction)
    {
        MissileData = missile;
        BaseDirection = direction.normalized;
    }

    public void Tick()
    {

    }
}
