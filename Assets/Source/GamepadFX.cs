using UnityEngine;
using UnityEngine.InputSystem;

public class GamepadFX : MonoBehaviour
{
	public static void SetVibration(float frequency)
	{
		Gamepad.current?.SetMotorSpeeds(frequency, frequency);
	}

	private void OnDestroy()
	{
		SetVibration(0f);
	}
}
