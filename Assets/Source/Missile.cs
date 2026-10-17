using UnityEngine;

public class Missile : MonoBehaviour
{
    public MissileSO MissileData { get; private set;  }
    public Vector2 BaseDirection { get; private set; }

    public float Speed { get; private set; }

    public void Init(MissileSO missile, Vector2 direction)
    {
        MissileData = missile;
        BaseDirection = direction.normalized;

        Speed = missile.BaseSpeed;
    }

    public void Tick()
    {
        transform.Translate(Speed * Time.deltaTime * BaseDirection);
        Speed += Time.deltaTime * MissileData.Acceleration;
        Speed = Mathf.Clamp(Speed, MissileData.MinSpeed, MissileData.MaxSpeed);
    }

    public void Kill()
    {
        MissileManager.Kill(this);
    }
}
