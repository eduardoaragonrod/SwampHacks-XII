using UnityEngine;

public class WispOrb : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private float _Drag = 6f;
    [SerializeField] private float _CatchRadius = 0.6f;

    private Player _owner;
    private ElementSO _element;
    private Vector2 _velocity;
    private bool _isPulling;
    private float _pullSpeed;
    private float _distancePulled;

    public void Init(Player owner, ElementSO element, Vector2 direction)
    {
        _owner = owner;
        _element = element;
        _velocity = direction * element.LaunchSpeed;

        if (_renderer != null)
        {
            _renderer.color = element.Color;
        }
    }

    public void Pull()
    {
        if (_isPulling) return;

        _isPulling = true;
        _pullSpeed = _velocity.magnitude;
        _distancePulled = 0f;
    }

    private void Update()
    {
        if (!_isPulling)
        {
            transform.position += (Vector3)(_velocity * Time.deltaTime);
            _velocity = Vector2.MoveTowards(_velocity, Vector2.zero, _Drag * Time.deltaTime);
            return;
        }

        Vector2 toOwner = _owner.transform.position - transform.position;
        float distance = toOwner.magnitude;
        Vector2 dir = toOwner.normalized;

        _pullSpeed += _element.PullAcceleration * Time.deltaTime;
        float step = _pullSpeed * Time.deltaTime;
        _distancePulled += Mathf.Min(step, distance);

        transform.position += ((Vector3)(dir * step) * 3) / 2;

        if (distance <= _CatchRadius)
        {
            _element.OnReturned(_owner, this, _distancePulled);
            _owner.CatchOrb(this, dir, _distancePulled * _element.KnockbackScale);
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out IDamageable target)) return;

        DamageInfo info = new()
        {
            Damage = _element.Damage,
            TargetTeam = _owner.GetComponent<Health>().Team
        };

        if (target.TryTakeDamage(info))
        {
            _element.OnHitEnemy(_owner, this, target);
        }
    }
}