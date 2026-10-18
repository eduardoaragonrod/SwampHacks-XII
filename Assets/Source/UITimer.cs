using UnityEngine;
using UnityEngine.Events;
using TMPro;
using DG.Tweening;

public class BossActivationTimerUI : MonoBehaviour
{
    [Header("Timer Settings")]
    [Tooltip("80 seconds = 1:20 on the clock")]
    [SerializeField] private float totalSeconds = 80f;
    [SerializeField] private bool startOnEnable = true;

    [Header("UI References")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Color normalTextColor = new Color(0.35f, 0.12f, 0.65f, 1f);
    [SerializeField] private Color urgentWarningColor = new Color(1f, 0.15f, 0.25f, 1f);

    [Header("Timer Events (Trigger Boss & Unlock Elements Here!)")]
    public UnityEvent OnTimerStarted;
    public UnityEvent OnTenSecondsWarning;
    public UnityEvent OnTimerFinished;

    private float _remainingTime;
    private bool _isRunning;
    private bool _warningFired;
    private int _lastDisplayedSecond = -1;

    private void Awake()
    {
        if (timerText == null) timerText = GetComponentInChildren<TMP_Text>();
        _remainingTime = totalSeconds;
        UpdateTimerDisplay(_remainingTime);
    }

    private void OnEnable()
    {
        if (startOnEnable)
        {
            StartTimer();
        }
    }

    public void StartTimer()
    {
        _remainingTime = totalSeconds;
        _isRunning = true;
        _warningFired = false;
        OnTimerStarted?.Invoke();
    }

    private void Update()
    {
        if (!_isRunning) return;

        // Real-time countdown so hit-stop / slow-mo never desyncs the clock!
        _remainingTime -= Time.unscaledDeltaTime;

        if (_remainingTime <= 10f && !_warningFired)
        {
            _warningFired = true;
            OnTenSecondsWarning?.Invoke();
        }

        if (_remainingTime <= 0f)
        {
            _remainingTime = 0f;
            _isRunning = false;
            UpdateTimerDisplay(0f);
            OnTimerFinished?.Invoke();
            return;
        }

        UpdateTimerDisplay(_remainingTime);
    }

    private void UpdateTimerDisplay(float timeInSeconds)
    {
        if (timerText == null) return;

        int totalCeil = Mathf.CeilToInt(timeInSeconds);
        if (totalCeil == _lastDisplayedSecond) return;
        _lastDisplayedSecond = totalCeil;

        int minutes = totalCeil / 60;
        int seconds = totalCeil % 60;
        timerText.text = $"{minutes}:{seconds:00}";

        if (totalCeil <= 10 && totalCeil > 0)
        {
            timerText.color = urgentWarningColor;
            timerText.transform.DOComplete();
            timerText.transform.DOPunchScale(Vector3.one * 0.22f, 0.25f, 6, 0.5f).SetUpdate(true);
        }
        else
        {
            timerText.color = normalTextColor;
        }
    }
}