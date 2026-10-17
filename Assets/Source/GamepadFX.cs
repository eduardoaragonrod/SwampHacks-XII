using DG.Tweening;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.InputSystem;

[AutoStaticsCleanup]
public partial class GamepadFX : MonoBehaviour
{
	private static Sequence _sequence;

	public static void PlaySequence(Sequence sequence)
	{
		_sequence?.Kill();
		_sequence = sequence;
		sequence.Play();
	}

	public static void Pulse(float frequency, float duration)
	{
		var seq = DOTween.Sequence();
		seq.AppendCallback(() => SetVibration(frequency));
		seq.AppendInterval(duration);
		seq.AppendCallback(() => SetVibration(0f));
	}
	public static void Pulse(float frequency, float onDuration, float offDuration, int cycles)
	{
		var seq = DOTween.Sequence();

		for (int i = 0; i < cycles; i++)
		{
			seq.AppendCallback(() => SetVibration(frequency));
			seq.AppendInterval(onDuration);
			seq.AppendCallback(() => SetVibration(0f));
			seq.AppendInterval(offDuration);
		}
	}

	public static void SetVibration(float frequency)
	{
		Gamepad.current?.SetMotorSpeeds(frequency, frequency);
	}

	private void OnDestroy()
	{
		SetVibration(0f);
	}
}
