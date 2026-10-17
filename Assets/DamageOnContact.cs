using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DamageOnContact : MonoBehaviour
{
	[SerializeField]
	private DamageInfo DamageInfo;

	private BoxCollider2D _hitbox;

	private void Awake()
	{
		_hitbox = GetComponent<BoxCollider2D>();
	}

	private void FixedUpdate()
	{
		var colliders = Physics2D.OverlapBoxAll(_hitbox.bounds.center, _hitbox.bounds.size, 0f, Layers.CharacterMask);

		foreach (var collider in colliders)
		{
			if (collider.TryGetComponent(out IDamageable dmg))
			{
				DamageInfo.Direction = (collider.bounds.center - _hitbox.bounds.center).normalized;
				dmg.TryTakeDamage(DamageInfo);
			}
		}
	}
}
