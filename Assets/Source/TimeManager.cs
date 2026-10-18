using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

[AutoStaticsCleanup]
public partial class TimeManager : MonoBehaviour
{
	private static readonly Dictionary<object, float> _timeScalers = new();

	private void LateUpdate()
	{
		float scale = 1f;

		foreach (var factor in _timeScalers.Values)
		{
			scale *= factor;
		}

		Time.timeScale = scale;
	}

	public static void ApplyFactor(object key, float factor)
	{
		if (_timeScalers.ContainsKey(key))
		{
			_timeScalers[key] = factor;
		}
		else
		{
			_timeScalers.Add(key, factor);
		}
	}

	public static void RemoveFactor(object key)
	{
		_timeScalers.Remove(key);
	}
}
