using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SunEaterDirector : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private Renderer eyeRenderer;
    [SerializeField] private Light2D eyeLight;
    [SerializeField] private Transform playerTransform;

    [Header("Enemy Consumption Settings")]
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private Color devourHaloColor = new Color(2.5f, 1.6f, 0.3f, 1f);
    [SerializeField] private float vacuumDuration = 1.35f;

    [Header("Post-Inbound Growth Settings")]
    [SerializeField] private float growthDuration = 2.5f;
    [SerializeField] private float targetPupilRadius = 0.23f;
    [SerializeField] private float targetScaleMultiplier = 1.35f;
    [SerializeField] private float awakenedRotationSpeed = 1.5f;

    private Material _eyeMat;

    private static readonly int BlinkProp = Shader.PropertyToID("_BlinkAmount");
    private static readonly int PupilProp = Shader.PropertyToID("_PupilRadius");
    private static readonly int SpeedProp = Shader.PropertyToID("_RotationSpeed");
    private static readonly int ShockwaveProp = Shader.PropertyToID("_ShockwaveRadius");
    private static readonly int OutlineColorProp = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineThickProp = Shader.PropertyToID("_OutlineThickness");

    private void Awake()
    {
        if (eyeRenderer != null)
            _eyeMat = eyeRenderer.material;
    }

    // Call this from your Spawner/Wave Manager when the Boss wave begins!
    [ContextMenu("Test Boss Inbound Cutscene")]
    public void TriggerBossInboundCutscene()
    {
        StopAllCoroutines();
        StartCoroutine(BossInboundRoutine());
    }

    // Hook this up to your Player's Health.OnDeath event!
    [ContextMenu("Test Player Death Cutscene")]
    public void TriggerPlayerDeathSequence()
    {
        StopAllCoroutines();
        StartCoroutine(PlayerDeathRoutine());
    }

    private IEnumerator BossInboundRoutine()
    {
        // 1. Fire Golden Shockwave outward from the core
        float t = 0f;
        while (t < 0.45f)
        {
            t += Time.deltaTime;
            _eyeMat.SetFloat(ShockwaveProp, Mathf.Lerp(0.15f, 0.5f, t / 0.45f));
            yield return null;
        }
        _eyeMat.SetFloat(ShockwaveProp, 0f);

        // 2. Ignite Golden Halo on all active enemies and disable their physics
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        Vector3[] startPositions = new Vector3[enemies.Length];

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null) continue;
            startPositions[i] = enemies[i].transform.position;

            if (enemies[i].TryGetComponent<Collider2D>(out var col)) col.enabled = false;
            if (enemies[i].TryGetComponent<Rigidbody2D>(out var rb)) rb.simulated = false;

            // Use your existing SpriteOutline shader to create the golden devour halo!
            if (enemies[i].TryGetComponent<SpriteRenderer>(out var sr))
            {
                sr.material.SetColor(OutlineColorProp, devourHaloColor);
                sr.material.SetFloat(OutlineThickProp, 2.5f);
            }
        }

        // Brief ominous pause while enemies float trapped in their golden halos
        yield return new WaitForSeconds(0.45f);

        // 3. Swirl and vacuum all haloed enemies into the black hole pupil
        t = 0f;
        _eyeMat.SetFloat(SpeedProp, 4.5f); // Spin accretion rings violently

        while (t < vacuumDuration)
        {
            t += Time.deltaTime;
            float progress = t / vacuumDuration;
            float eased = progress * progress * progress; // Cubic acceleration inward

            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == null) continue;

                // Spiral trajectory into the center of the Eye
                Vector3 currentPos = Vector3.Lerp(startPositions[i], transform.position, eased);
                float angle = progress * 10f;
                float offsetRadius = (1f - eased) * 0.6f;
                currentPos += new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * offsetRadius;

                enemies[i].transform.position = currentPos;
                enemies[i].transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, eased);
            }
            yield return null;
        }

        // Destroy consumed enemies & smoothly fade the feeding flash instead of snapping
        foreach (var enemy in enemies)
        {
            if (enemy != null) Destroy(enemy);
        }

        t = 0f;
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            float p = t / 0.35f;
            if (eyeLight != null) eyeLight.intensity = Mathf.Lerp(4.0f, 1.4f, p);
            _eyeMat.SetFloat(SpeedProp, Mathf.Lerp(4.5f, 0.75f, p));
            yield return null;
        }

        // 4. The Reveal: Heavy Double-Blink (The player realizes it's an Eye)
        yield return new WaitForSeconds(0.3f);
        yield return PerformBlink(0.14f, 0.12f, 0.22f); // First startled blink
        yield return new WaitForSeconds(0.25f);
        yield return PerformBlink(0.20f, 0.35f, 0.45f); // Slow, menacing second blink

        // 5. Slow, Menacing Awakening Growth (Replaces the instant snap!)
        float startPupil = _eyeMat.GetFloat(PupilProp);
        float startSpeed = _eyeMat.GetFloat(SpeedProp);
        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale * targetScaleMultiplier;
        float startLight = eyeLight != null ? eyeLight.intensity : 1.4f;

        t = 0f;
        while (t < growthDuration)
        {
            t += Time.deltaTime;
            float p = t / growthDuration;
            // Smoothstep easing so it starts slow, swells smoothly, and settles gently
            float smooth = p * p * (3f - 2f * p);

            _eyeMat.SetFloat(PupilProp, Mathf.Lerp(startPupil, targetPupilRadius, smooth));
            _eyeMat.SetFloat(SpeedProp, Mathf.Lerp(startSpeed, awakenedRotationSpeed, smooth));
            transform.localScale = Vector3.Lerp(startScale, endScale, smooth);

            if (eyeLight != null)
            {
                eyeLight.intensity = Mathf.Lerp(startLight, startLight * 1.6f, smooth);
            }

            yield return null;
        }
    }

    private IEnumerator PlayerDeathRoutine()
    {
        // Disable player movement & physics so the Eye has total control
        if (playerTransform != null)
        {
            if (playerTransform.TryGetComponent<Rigidbody2D>(out var rb)) rb.simulated = false;
            if (playerTransform.TryGetComponent<CharacterMovement>(out var move)) move.enabled = false;
        }

        Vector3 playerStartPos = playerTransform != null ? playerTransform.position : Vector3.zero;
        Vector3 playerStartScale = playerTransform != null ? playerTransform.localScale : Vector3.one;
        Vector3 eyeStartScale = transform.localScale;
        float startPupil = _eyeMat.GetFloat(PupilProp);

        // Phase 1: Gravitational Pull & Abyssal Dilation (Eye expands to swallow the arena)
        float duration = 1.2f;
        float t = 0f;
        _eyeMat.SetFloat(SpeedProp, 7.0f);

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / duration;
            float easeIn = p * p * p;

            // Expand pupil from its current size and scale the Eye quad to cover the screen
            _eyeMat.SetFloat(PupilProp, Mathf.Lerp(startPupil, 0.44f, easeIn));
            transform.localScale = Vector3.Lerp(eyeStartScale, eyeStartScale * 2.5f, easeIn);

            if (eyeLight != null)
                eyeLight.intensity = Mathf.Lerp(1.4f, 5.5f, easeIn);

            // Drag player into the dead center and crush them into a singularity point
            if (playerTransform != null)
            {
                playerTransform.position = Vector3.Lerp(playerStartPos, transform.position, easeIn);
                playerTransform.localScale = Vector3.Lerp(playerStartScale, Vector3.zero, easeIn);
                playerTransform.Rotate(0f, 0f, 720f * Time.unscaledDeltaTime);
            }

            yield return null;
        }

        // Phase 2: The Final Clamp (The Eye snaps shut into a razor slit, plunging screen to black)
        t = 0f;
        while (t < 0.12f)
        {
            t += Time.unscaledDeltaTime;
            float p = t / 0.12f;
            _eyeMat.SetFloat(BlinkProp, Mathf.Lerp(0f, 1f, p));
            if (eyeLight != null) eyeLight.intensity = Mathf.Lerp(5.5f, 0f, p);
            yield return null;
        }

        _eyeMat.SetFloat(BlinkProp, 1f);
        if (eyeLight != null) eyeLight.intensity = 0f;

        // Hold on the glowing horizontal slit for half a second before triggering Game Over UI
        yield return new WaitForSecondsRealtime(0.8f);
    }

    private IEnumerator PerformBlink(float closeTime, float holdTime, float openTime)
    {
        float t = 0f;
        while (t < closeTime)
        {
            t += Time.deltaTime;
            _eyeMat.SetFloat(BlinkProp, Mathf.Lerp(0f, 1f, t / closeTime));
            yield return null;
        }
        _eyeMat.SetFloat(BlinkProp, 1f);

        yield return new WaitForSeconds(holdTime);

        t = 0f;
        while (t < openTime)
        {
            t += Time.deltaTime;
            _eyeMat.SetFloat(BlinkProp, Mathf.Lerp(1f, 0f, t / openTime));
            yield return null;
        }
        _eyeMat.SetFloat(BlinkProp, 0f);
    }
}