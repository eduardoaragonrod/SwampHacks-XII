using UnityEngine;

public class RandomRotation : MonoBehaviour
{
	[SerializeField]
	private float AngleInterval = 90f;
	[SerializeField]
	private int AngleMultiple = 4;
	[SerializeField]
	private float TimeInterval = 0.1f;

	private float _nextRotateTime;

	private void Awake()
	{
		_nextRotateTime = Time.time + TimeInterval;
	}

	private void Update()
	{
		if (Time.time > _nextRotateTime)
		{
			_nextRotateTime = Time.time + TimeInterval;
			transform.rotation = Quaternion.AngleAxis(AngleInterval * Random.Range(0, AngleMultiple), Vector3.forward);
		}
	}
}
