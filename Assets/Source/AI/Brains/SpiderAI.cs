using UnityEngine;

public class SpiderAI : EnemyAI
{
	private System.Action<bool> _running;

	private float _nextIdleEndTime;
	private Vector2 _moveToPos;
	private float _moveToTimeoutTime;

	protected override void Awake()
	{
		base.Awake();
		TransitionTo(Idle);
	}

	private void Update()
	{
		_running?.Invoke(false);
	}

	private void TransitionTo(System.Action<bool> state)
	{
		if (_running != state)
		{
			_running = state;
			_running.Invoke(true);
		}
	}

	private void Idle(bool enter)
	{
		if (enter)
		{
			_nextIdleEndTime = Time.time + Random.Range(0.5f, 3f);
			return;
		}

		if (Time.time > _nextIdleEndTime)
		{
			TransitionTo(MoveTo);
		}
	}

	private void MoveTo(bool enter)
	{
		if (enter)
		{
			_moveToPos = Player.Instance.transform.position;
			var dirFromPlayer = DirectionTo(_moveToPos, transform.position);
			dirFromPlayer = Quaternion.AngleAxis(Random.Range(-45f, 45f), Vector3.forward) * dirFromPlayer;
			_moveToPos += dirFromPlayer * Random.Range(1f, 3f);

			_moveToTimeoutTime = Time.time + 5f;
			return;
		}

		if (Time.time > _moveToTimeoutTime)
		{
			TransitionTo(Idle);
			return;
		}

		if (Vector2.Distance(transform.position, _moveToPos) > 0.5f)
		{
			Movement.Walk(DirectionTo(transform.position, _moveToPos));
		}
		else
		{
			if (Random.value > 0.5f && Vector2.Distance(transform.position, Player.Instance.transform.position) < 5f)
			{
				TransitionTo(WindUp);
			}
			else
			{
				TransitionTo(Idle);
			}
		}
	}

	private void WindUp(bool enter)
	{
		if (enter)
		{
			var dirFromPlayer = DirectionTo(Player.Instance.transform.position, transform.position);
			_moveToPos = (Vector2)Player.Instance.transform.position + (dirFromPlayer * 5f);
			return;
		}

		if (Vector2.Distance(transform.position, _moveToPos) > 0.5f)
		{
			Movement.Walk(DirectionTo(transform.position, _moveToPos), Movement.WalkSpeed * 0.3f);
		}
		else
		{
			TransitionTo(Charge);
		}
	}

	private void Charge(bool enter)
	{
		if (enter)
		{
			var dirToPlayer = DirectionTo(transform.position, Player.Instance.transform.position);
			_moveToPos = (Vector2)Player.Instance.transform.position + (dirToPlayer * Random.Range(2f, 3f));
			_moveToTimeoutTime = Time.time + 5f;
			return;
		}

		if (Time.time > _moveToTimeoutTime)
		{
			TransitionTo(Idle);
			return;
		}

		if (Vector2.Distance(transform.position, _moveToPos) > 1f)
		{
			Movement.Walk(DirectionTo(transform.position, _moveToPos), Movement.WalkSpeed * 3f);
		}
		else
		{
			TransitionTo(Idle);
		}
	}

	private Vector2 DirectionTo(Vector2 origin, Vector2 dest)
	{
		return (dest - origin).normalized;
	}
}
