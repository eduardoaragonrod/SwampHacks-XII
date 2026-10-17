using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class KineticPostProcessing : MonoBehaviour
{
    [SerializeField] private Rigidbody2D playerRb;
    [SerializeField] private Volume globalVolume;
    [SerializeField] private float maxSpeedForEffect = 18f;

    private ChromaticAberration _chromatic;
    private LensDistortion _lensDistortion;
    private Bloom _bloom;

    private void Start()
    {
        globalVolume.profile.TryGet(out _chromatic);
        globalVolume.profile.TryGet(out _lensDistortion);
        globalVolume.profile.TryGet(out _bloom);
    }

    private void Update()
    {
        if (playerRb == null) return;

        float speedFactor = Mathf.Clamp01(playerRb.linearVelocity.magnitude / maxSpeedForEffect);

        if (_chromatic != null)
            _chromatic.intensity.value = Mathf.Lerp(0.12f, 0.75f, speedFactor);

        if (_lensDistortion != null)
            _lensDistortion.intensity.value = Mathf.Lerp(0f, -0.35f, speedFactor);

        if (_bloom != null)
            _bloom.intensity.value = Mathf.Lerp(2.2f, 4.5f, speedFactor);
    }
}