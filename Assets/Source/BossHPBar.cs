using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;

public class BossTimeBarUI : MonoBehaviour
{
    [Header("UI & Artist Animation References")]
    [Tooltip("The inner Fill Image (set Image Type to Filled -> Horizontal)")]
    [SerializeField] private Image primaryFillImage;
    [Tooltip("Optional trailing white/red back-bar Image for eye-candy drain delay")]
    [SerializeField] private Image ghostTrailFillImage;
    [Tooltip("The Animator on your artist's Boss-Bar outline that has the 'Blink' animation")]
    [SerializeField] private Animator bossBarOutlineAnimator;
    [SerializeField] private string blinkAnimationTrigger = "Blink";

    [Header("Eye Candy Colors (Phase 1 -> Phase 2)")]
    [SerializeField] private Color phase1FillColor = new Color(1.4f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color phase2FillColor = new Color(1.8f, 0.15f, 0.25f, 1f);
    [SerializeField] private Color phaseTransitionFlashColor = Color.white;

    [Header("Phase Shift Threshold (0.5 = 50% Bar Remaining)")]
    [SerializeField, Range(0.1f, 0.9f)] private float phase2Threshold = 0.5f;

    [Header("Phase Events")]
    public UnityEvent OnBossPhase2Started;
    public UnityEvent OnBossBarDepleted;

    private float _totalDuration = 35f;
    private float _remainingTime;
    private bool _isActive;
    private bool _inPhase2;
    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        if (!TryGetComponent(out _canvasGroup))
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _canvasGroup.alpha = 0f;
    }

    public void StartBossBarCountdown(float durationSeconds)
    {
        _totalDuration = Mathf.Max(1f, durationSeconds);
        _remainingTime = _totalDuration;
        _isActive = true;
        _inPhase2 = false;

        if (primaryFillImage != null)
        {
            primaryFillImage.fillAmount = 1f;
            primaryFillImage.color = phase1FillColor;
        }
        if (ghostTrailFillImage != null)
        {
            ghostTrailFillImage.fillAmount = 1f;
        }

        _canvasGroup.DOKill();
        _canvasGroup.DOFade(1f, 0.75f).SetUpdate(true);

        TriggerOutlineBlink();
    }

    private void Update()
    {
        if (!_isActive) return;

        _remainingTime -= Time.unscaledDeltaTime;
        float normalized = Mathf.Clamp01(_remainingTime / _totalDuration);

        if (primaryFillImage != null)
        {
            primaryFillImage.fillAmount = normalized;
            float pulseSpeed = _inPhase2 ? 10f : 4f;
            Color baseCol = _inPhase2 ? phase2FillColor : phase1FillColor;
            primaryFillImage.color = Color.Lerp(baseCol, Color.white, (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed)) * 0.25f);
        }

        if (ghostTrailFillImage != null)
        {
            ghostTrailFillImage.fillAmount = Mathf.Lerp(ghostTrailFillImage.fillAmount, normalized, Time.unscaledDeltaTime * 2.5f);
        }

        if (!_inPhase2 && normalized <= phase2Threshold)
        {
            _inPhase2 = true;
            TriggerPhase2Transition();
        }

        if (_remainingTime <= 0f)
        {
            _isActive = false;
            OnBossBarDepleted?.Invoke();
            _canvasGroup.DOFade(0f, 1.0f).SetUpdate(true);
        }
    }

    private void TriggerPhase2Transition()
    {
        TriggerOutlineBlink();

        transform.DOComplete();
        transform.DOShakePosition(0.45f, 12f, vibrato: 30).SetUpdate(true);
        GamepadFX.Pulse(0.85f, 0.3f);

        OnBossPhase2Started?.Invoke();
    }

    public void TriggerOutlineBlink()
    {
        if (bossBarOutlineAnimator != null)
        {
            bossBarOutlineAnimator.Play(blinkAnimationTrigger, 0, 0f);
        }
    }
}