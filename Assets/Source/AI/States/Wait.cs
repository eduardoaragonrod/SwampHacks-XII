using UnityEngine;

public class Wait : State
{
    private float _endTime, _duration, _devation;

    public Wait(float duration, float deviation = 0f)
    {
        _duration = duration;
        _devation = deviation;
    }

	public override void OnEnter()
	{
        _endTime = Time.time + _duration + (Random.Range(-_devation, _devation));
	}

	public override bool OnUpdate()
	{
        return Time.time >= _endTime;
	}
}
