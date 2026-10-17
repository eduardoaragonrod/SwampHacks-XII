using UnityEngine;

public class Missile : MonoBehaviour
{
    public MissileSO MissileData { get; private set; }
    public Vector2 BaseDirection { get; private set; }

    public float Speed { get; private set; }
    public bool IsDead { get; private set; }

    public void Init(MissileSO missile, Vector2 direction)
    {
        MissileData = missile;
        BaseDirection = direction.normalized;

        Speed = missile.BaseSpeed;
    }

    public void Tick()
    {
        var lastPos = transform.position;

        transform.Translate(Speed * Time.deltaTime * BaseDirection);
        Speed += Time.deltaTime * MissileData.Acceleration;
        Speed = Mathf.Clamp(Speed, MissileData.MinSpeed, MissileData.MaxSpeed);

        var deltaPos = transform.position - lastPos;
        var hits = Physics2D.CircleCastAll(lastPos, MissileData.CollisionRadius, deltaPos.normalized, deltaPos.magnitude, Layers.CharacterMask | Layers.ObstacleMask);

        foreach (var hit in hits)
        {
            if (hit.collider.gameObject.layer == Layers.ObstacleLayer)
            {
                Kill();
                return;
            }
            else if (hit.collider.TryGetComponent(out IDamageable dmg))
            {
                if (dmg.TryTakeDamage(new()))
                {
                    Kill();
                    return;
                }
            }
        }
    }

    public void Kill()
    {
        MissileManager.Kill(this);
    }

    public void OnDeath()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;
        Destroy(gameObject);
    }
}
