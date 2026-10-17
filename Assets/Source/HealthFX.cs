using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;
[RequireComponent(typeof(Health))]

[RequireComponent(typeof(SpriteRenderer))]
public class HealthFX : MonoBehaviour
{
    public const string FlashShaderKey = "_Flash";

    [SerializeField]
    private AudioResource HurtSound;

    private SpriteRenderer _renderer;
    private Sequence _flashTween;

    private void Awake()
    {
        var health = GetComponent<Health>();
        health.OnDamaged += OnDamaged;

        _renderer = GetComponent<SpriteRenderer>();
    }

    private void OnDestroy()
    {
        _flashTween?.Kill();
    }

    private void OnDamaged(DamageInfo info)
    {
        Audio.Play(HurtSound, transform.position);

        _flashTween.Kill();
        _flashTween = _flashTween = DOTween.Sequence();
        _flashTween.Append(TweenShader(FlashShaderKey, 1f, 0.1f, Ease.Linear));
        _flashTween.Append(TweenShader(FlashShaderKey, 0f, 0.1f, Ease.Linear));
    }

    private Tween TweenShader(string name, float endValue, float duration, Ease ease)
    {
        return DOTween.To(() => _renderer.material.GetFloat(name), x => _renderer.material.SetFloat(name, x), endValue, duration).SetEase(ease);
    }
}
