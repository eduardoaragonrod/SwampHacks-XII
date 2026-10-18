using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class FlickerLight : MonoBehaviour
{
	[SerializeField]
	private float Frequency = 1f;
	[SerializeField]
	private float Min = 0.5f, Max = 1f;

	private Light2D _light;

	private void Awake()
	{
		_light = GetComponent<Light2D>();
	}

	private void Update()
	{
		_light.intensity = Mathf.Lerp(Min, Max, Mathf.PerlinNoise1D(Time.time * Frequency));
	}
}
