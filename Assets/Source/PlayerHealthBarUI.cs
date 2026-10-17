using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthBarUI : MonoBehaviour
{
    [Header("Health Reference")]
    [SerializeField] private Health playerHealth;

    [Header("UI References")]
    [SerializeField] private Image barFillImage;
    [SerializeField] private TMP_Text percentText;

    [Header("Smoothing")]
    [SerializeField] private float fillSmoothSpeed = 10f;

    private float _targetFill = 1f;

    private void Start()
    {
        if (playerHealth == null && Player.Instance != null)
        {
            playerHealth = Player.Instance.GetComponent<Health>();
        }
    }

    private void Update()
    {
        if (playerHealth != null)
        {
            float current = playerHealth.Current;
            float max = Mathf.Max(1f, playerHealth.Max);
            _targetFill = Mathf.Clamp01(current / max);
        }

        if (barFillImage != null)
        {
            barFillImage.fillAmount = Mathf.Lerp(
                barFillImage.fillAmount,
                _targetFill,
                Time.unscaledDeltaTime * fillSmoothSpeed
            );
        }

        if (percentText != null)
        {
            int pct = Mathf.RoundToInt(_targetFill * 100f);
            percentText.text = $"{pct}%";
        }
    }

    public void SetHealthNormalized(float normalizedValue)
    {
        _targetFill = Mathf.Clamp01(normalizedValue);
    }
}