using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DimensionGateBreak : MonoBehaviour
{
    [Header("Gate & Visual References")]
    [Tooltip("The solid BoxCollider2D blocking the doorway")]
    [SerializeField] private Collider2D solidGateCollider;
    [Tooltip("Optional sprite for the physical gate door that hides while broken and reappears when resealed")]
    [SerializeField] private SpriteRenderer physicalGateSprite;
    [Tooltip("Renderer with the Custom/URP_DimensionShatter material applied")]
    [SerializeField] private Renderer dimensionRiftRenderer;
    [SerializeField] private Volume globalVolume;

    [Header("Anime Impact Timing")]
    [SerializeField] private float impactFreezeDuration = 0.14f;
    [SerializeField] private float shatterOpenDuration = 0.22f;
    [SerializeField] private float timeBeforeReseal = 0.85f;
    [SerializeField] private float resealSnapDuration = 0.35f;
    [SerializeField] private float breakthroughSpeedBoost = 16f;

    [Header("Events (Start Main Arena Spawners & Stage Timer Here!)")]
    public UnityEvent OnGateShattered;
    public UnityEvent OnGateResealedAndArenaStarted;

    private Material _riftMat;
    private bool _isBroken;
    private ChromaticAberration _chroma;
    private LensDistortion _lens;

    private static readonly int ShatterProp = Shader.PropertyToID("_ShatterProgress");
    private static readonly int SeparationProp = Shader.PropertyToID("_ShardSeparation");
    private static readonly int CrackProp = Shader.PropertyToID("_CrackIntensity");

    private void Awake()
    {
        if (solidGateCollider == null) solidGateCollider = GetComponent<Collider2D>();

        if (dimensionRiftRenderer != null)
        {
            _riftMat = dimensionRiftRenderer.material;
            _riftMat.SetFloat(ShatterProp, 0f);
            _riftMat.SetFloat(SeparationProp, 0f);
        }

        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out _chroma);
            globalVolume.profile.TryGet(out _lens);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_isBroken) return;

        if (collision.gameObject.TryGetComponent<Player>(out var player))
        {
            if (player.IsCriticalSpeed)
            {
                StartCoroutine(ShatterAndResealRoutine(player));
            }
            else
            {
                StopAllCoroutines();
                StartCoroutine(WeakBumpTeaseRoutine());
            }
        }
    }

    private IEnumerator WeakBumpTeaseRoutine()
    {
        GamepadFX.Pulse(0.25f, 0.08f);
        if (_riftMat != null)
        {
            _riftMat.SetFloat(ShatterProp, 0.45f);
            _riftMat.SetFloat(SeparationProp, 0.02f);
            yield return new WaitForSeconds(0.18f);

            float t = 0f;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                _riftMat.SetFloat(ShatterProp, Mathf.Lerp(0.45f, 0f, t / 0.2f));
                yield return null;
            }
            _riftMat.SetFloat(ShatterProp, 0f);
        }
    }

    private IEnumerator ShatterAndResealRoutine(Player player)
    {
        _isBroken = true;

        if (_riftMat != null)
        {
            _riftMat.SetFloat(ShatterProp, 1f);
            _riftMat.SetFloat(CrackProp, 0.12f);
            _riftMat.SetFloat(SeparationProp, 0.05f);
        }

        GamepadFX.Pulse(1.0f, 0.35f);

        if (Camera.main != null)
        {
            Camera.main.transform.DOComplete();
            Camera.main.transform.DOShakePosition(0.35f, 0.85f, vibrato: 35, randomness: 90f).SetUpdate(true);
        }

        if (_chroma != null) _chroma.intensity.value = 1.0f;
        if (_lens != null) _lens.intensity.value = -0.55f;

        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(impactFreezeDuration);
        Time.timeScale = 1f;

        if (solidGateCollider != null) solidGateCollider.enabled = false;
        if (physicalGateSprite != null) physicalGateSprite.enabled = false;

        if (player.TryGetComponent<CharacterMovement>(out var movement))
        {
            movement.ApplyKnockback(Vector2.up * breakthroughSpeedBoost);
        }

        OnGateShattered?.Invoke();

        float t = 0f;
        while (t < shatterOpenDuration)
        {
            t += Time.deltaTime;
            float p = t / shatterOpenDuration;
            if (_riftMat != null)
            {
                _riftMat.SetFloat(SeparationProp, Mathf.Lerp(0.05f, 0.32f, p));
                _riftMat.SetFloat(CrackProp, Mathf.Lerp(0.12f, 0.18f, p));
            }
            if (_chroma != null) _chroma.intensity.value = Mathf.Lerp(1.0f, 0.15f, p);
            if (_lens != null) _lens.intensity.value = Mathf.Lerp(-0.55f, 0f, p);
            yield return null;
        }

        yield return new WaitForSeconds(timeBeforeReseal);

        if (player != null && player.transform.position.y < transform.position.y + 1.0f)
        {
            player.transform.position = new Vector3(
                player.transform.position.x,
                transform.position.y + 1.5f,
                player.transform.position.z
            );
        }

        GamepadFX.Pulse(0.7f, 0.2f);
        t = 0f;
        while (t < resealSnapDuration)
        {
            t += Time.deltaTime;
            float p = t / resealSnapDuration;
            float easeIn = p * p * p; 

            if (_riftMat != null)
            {
                _riftMat.SetFloat(SeparationProp, Mathf.Lerp(0.32f, 0f, easeIn));
                _riftMat.SetFloat(CrackProp, Mathf.Lerp(0.18f, 0.02f, easeIn));
                _riftMat.SetFloat(ShatterProp, Mathf.Lerp(1f, 0f, easeIn));
            }
            yield return null;
        }

        if (_riftMat != null)
        {
            _riftMat.SetFloat(ShatterProp, 0f);
            _riftMat.SetFloat(SeparationProp, 0f);
        }

        if (solidGateCollider != null) solidGateCollider.enabled = true;
        if (physicalGateSprite != null) physicalGateSprite.enabled = true;

        if (Camera.main != null)
        {
            Camera.main.transform.DOShakePosition(0.15f, 0.35f, vibrato: 20);
        }

        OnGateResealedAndArenaStarted?.Invoke();
    }
}