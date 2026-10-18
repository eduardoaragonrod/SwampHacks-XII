using System.Collections;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

public class SunEaterDirector : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private Renderer eyeRenderer;
    [SerializeField] private Light2D eyeLight;
    [SerializeField] private Transform playerTransform;

    [Header("Boss Trigger Timer")]
    [SerializeField] private bool startBossOnTimer = true;
    [SerializeField] private float secondsUntilBoss = 60f;

    [Header("Camera Zoom Out Settings")]
    [SerializeField] private CinemachineCamera cinemachineCam;
    [SerializeField] private float targetCameraOrthoSize = 9.5f;
    [SerializeField] private float cameraZoomDuration = 2.5f;

    [Header("Boss Music Settings")]
    [SerializeField] private AudioSource bossMusicSource;
    [SerializeField] private AudioClip bossThemeClip;
    [SerializeField] private AudioClip victoryThemeClip;
    [SerializeField] private AudioSource[] stageMusicSourcesToMute;

    [Header("Enemy Consumption Settings")]
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private Color devourHaloColor = new Color(2.5f, 1.6f, 0.3f, 1f);
    [SerializeField] private float vacuumDuration = 1.35f;

    [Header("Growth Settings")]
    [SerializeField] private float growthDuration = 2.5f;
    [SerializeField] private float targetPupilRadius = 0.23f;
    [SerializeField] private float targetScaleMultiplier = 1.35f;
    [SerializeField] private float awakenedRotationSpeed = 1.5f;

    [Header("Boss Events")]
    public UnityEvent OnBossStarted;
    public UnityEvent OnBossFightActive;
    public UnityEvent OnBossDefeated;

    private Material _eyeMat;
    private bool _bossTriggered;
    private bool _bossDefeated;
    private float _timer;

    private static readonly int BlinkProp = Shader.PropertyToID("_BlinkAmount");
    private static readonly int PupilProp = Shader.PropertyToID("_PupilRadius");
    private static readonly int SpeedProp = Shader.PropertyToID("_RotationSpeed");
    private static readonly int ShockwaveProp = Shader.PropertyToID("_ShockwaveRadius");
    private static readonly int SunRebirthProp = Shader.PropertyToID("_SunRebirth");
    private static readonly int OutlineColorProp = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineThickProp = Shader.PropertyToID("_OutlineThickness");

    private void Awake()
    {
        if (eyeRenderer != null)
        {
            _eyeMat = eyeRenderer.material;
            _eyeMat.SetFloat(SunRebirthProp, 0f);
        }
    }

    private void Update()
{
    if (!startBossOnTimer || _bossTriggered) return;

    
    _timer += Time.unscaledDeltaTime;
    if (_timer >= secondsUntilBoss)
    {
        TriggerBossInboundCutscene();
    }
}

    [ContextMenu("Test Boss Arrival Cutscene")]
    public void TriggerBossInboundCutscene()
    {
        if (_bossTriggered) return;
        _bossTriggered = true;

        StopAllCoroutines();
        StartCoroutine(BossInboundRoutine());
    }

    [ContextMenu("Test Boss Win Cutscene")]
    public void TriggerBossWinSequence()
    {
        if (_bossDefeated) return;
        _bossDefeated = true;

        StopAllCoroutines();
        StartCoroutine(BossWinRoutine());
    }

    [ContextMenu("Test Player Death Cutscene")]
    public void TriggerPlayerDeathSequence()
    {
        if (_bossDefeated) return;
        StopAllCoroutines();
        StartCoroutine(PlayerDeathRoutine());
    }

    private IEnumerator BossInboundRoutine()
    {
        OnBossStarted?.Invoke();

        StartCoroutine(TransitionToBossMusicRoutine(1.5f));
        StartCoroutine(ZoomOutCameraRoutine());

        GamepadFX.Pulse(0.6f, 0.35f);
        float t = 0f;
        while (t < 0.45f)
        {
            t += Time.deltaTime;
            _eyeMat.SetFloat(ShockwaveProp, Mathf.Lerp(0.15f, 0.5f, t / 0.45f));
            yield return null;
        }
        _eyeMat.SetFloat(ShockwaveProp, 0f);

        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        Vector3[] startPositions = new Vector3[enemies.Length];

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null) continue;
            startPositions[i] = enemies[i].transform.position;

            if (enemies[i].TryGetComponent<Collider2D>(out var col)) col.enabled = false;
            if (enemies[i].TryGetComponent<Rigidbody2D>(out var rb)) rb.simulated = false;

            if (enemies[i].TryGetComponent<SpriteRenderer>(out var sr))
            {
                sr.material.SetColor(OutlineColorProp, devourHaloColor);
                sr.material.SetFloat(OutlineThickProp, 2.5f);
            }
        }

        yield return new WaitForSeconds(0.45f);

        t = 0f;
        _eyeMat.SetFloat(SpeedProp, 4.5f);
        GamepadFX.Pulse(0.4f, vacuumDuration);

        while (t < vacuumDuration)
        {
            t += Time.deltaTime;
            float progress = t / vacuumDuration;
            float eased = progress * progress * progress;

            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == null) continue;

                Vector3 currentPos = Vector3.Lerp(startPositions[i], transform.position, eased);
                float angle = progress * 10f;
                float offsetRadius = (1f - eased) * 0.6f;
                currentPos += new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * offsetRadius;

                enemies[i].transform.position = currentPos;
                enemies[i].transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, eased);
            }
            yield return null;
        }

        foreach (var enemy in enemies)
        {
            if (enemy != null) Destroy(enemy);
        }

        GamepadFX.Pulse(0.95f, 0.2f);
        t = 0f;
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            float p = t / 0.35f;
            if (eyeLight != null) eyeLight.intensity = Mathf.Lerp(4.0f, 1.4f, p);
            _eyeMat.SetFloat(SpeedProp, Mathf.Lerp(4.5f, 0.75f, p));
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);
        GamepadFX.Pulse(0.5f, 0.15f);
        yield return PerformBlink(0.14f, 0.12f, 0.22f);
        yield return new WaitForSeconds(0.25f);
        GamepadFX.Pulse(0.75f, 0.25f);
        yield return PerformBlink(0.20f, 0.35f, 0.45f);

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

        OnBossFightActive?.Invoke();
    }


    private IEnumerator BossWinRoutine()
    {
        Vector3 basePos = transform.position;
        Vector3 baseScale = transform.localScale;
        float startPupil = _eyeMat.GetFloat(PupilProp);

        GameObject[] leftoverEnemies = GameObject.FindGameObjectsWithTag(enemyTag);
        foreach (var e in leftoverEnemies)
        {
            if (e != null) Destroy(e);
        }

       
        _eyeMat.SetFloat(SpeedProp, 9.5f);
        GamepadFX.Pulse(0.65f, 0.15f, 0.08f, 6);


        yield return PerformBlink(0.09f, 0.05f, 0.11f);
        yield return PerformBlink(0.08f, 0.08f, 0.12f);

        float t = 0f;
        float convulseDuration = 1.35f;
        while (t < convulseDuration)
        {
            t += Time.deltaTime;
            float p = t / convulseDuration;

     
            Vector2 jitter = Random.insideUnitCircle * (0.35f * p);
            transform.position = basePos + new Vector3(jitter.x, jitter.y, 0f);

            float squash = 1f + Mathf.Sin(t * 38f) * 0.18f * p;
            transform.localScale = new Vector3(baseScale.x * squash, baseScale.y / squash, baseScale.z);

            _eyeMat.SetFloat(PupilProp, Mathf.Lerp(startPupil, 0.03f, p * p));

            _eyeMat.SetFloat(SunRebirthProp, Mathf.PingPong(t * 6f, 0.45f) * p);

            if (eyeLight != null)
                eyeLight.intensity = Mathf.Lerp(2.2f, 5.5f, p);

            yield return null;
        }

        transform.position = basePos;

        GamepadFX.Pulse(1.0f, 0.6f);
        if (Camera.main != null)
        {
            Camera.main.transform.DOComplete();
            Camera.main.transform.DOShakePosition(0.65f, 1.1f, vibrato: 40, randomness: 90f).SetUpdate(true);
        }

        _eyeMat.SetFloat(SunRebirthProp, 1f);
        _eyeMat.SetFloat(PupilProp, 0.28f);
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(0.14f);
        Time.timeScale = 1f;

        if (bossMusicSource != null)
        {
            if (victoryThemeClip != null)
            {
                bossMusicSource.clip = victoryThemeClip;
                bossMusicSource.loop = false;
                bossMusicSource.Play();
            }
            else
            {
                bossMusicSource.DOFade(0f, 2.0f);
            }
        }

        
        for (int wave = 0; wave < 3; wave++)
        {
            t = 0f;
            float waveTime = 0.65f;
            GamepadFX.Pulse(0.8f - wave * 0.2f, 0.25f);

            while (t < waveTime)
            {
                t += Time.deltaTime;
                float p = t / waveTime;
                float easeOut = 1f - Mathf.Pow(1f - p, 3f);

                _eyeMat.SetFloat(ShockwaveProp, Mathf.Lerp(0.08f, 0.49f, easeOut));

                float totalProgress = (wave + p) / 3f;

                _eyeMat.SetFloat(PupilProp, Mathf.Lerp(0.22f, 0.36f, totalProgress));
                _eyeMat.SetFloat(SpeedProp, Mathf.Lerp(6.0f, 0.4f, totalProgress));

                float scaleBounce = 1.15f + Mathf.Sin(totalProgress * Mathf.PI) * 0.35f;
                transform.localScale = baseScale * scaleBounce;

                if (eyeLight != null)
                {
                    eyeLight.intensity = Mathf.Lerp(6.0f, 9.5f, totalProgress);
                    eyeLight.pointLightOuterRadius = Mathf.Lerp(9f, 22f, totalProgress);
                }

                yield return null;
            }
        }

        _eyeMat.SetFloat(ShockwaveProp, 0f);

        yield return new WaitForSeconds(0.8f);
        OnBossDefeated?.Invoke();
    }

    private IEnumerator ZoomOutCameraRoutine()
    {
        float t = 0f;
        float startSize = cinemachineCam != null 
            ? cinemachineCam.Lens.OrthographicSize 
            : (Camera.main != null ? Camera.main.orthographicSize : 5f);

        while (t < cameraZoomDuration)
        {
            t += Time.deltaTime;
            float p = t / cameraZoomDuration;
            float smooth = p * p * (3f - 2f * p);
            float newSize = Mathf.Lerp(startSize, targetCameraOrthoSize, smooth);

            if (cinemachineCam != null)
            {
                var lens = cinemachineCam.Lens;
                lens.OrthographicSize = newSize;
                cinemachineCam.Lens = lens;
            }
            else if (Camera.main != null)
            {
                Camera.main.orthographicSize = newSize;
            }

            yield return null;
        }
    }

    private IEnumerator TransitionToBossMusicRoutine(float fadeDuration)
    {
        if (bossMusicSource != null && bossThemeClip != null)
        {
            bossMusicSource.clip = bossThemeClip;
            bossMusicSource.volume = 0f;
            bossMusicSource.loop = true;
            bossMusicSource.Play();
        }

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / fadeDuration;

            if (bossMusicSource != null)
                bossMusicSource.volume = Mathf.Lerp(0f, 1f, p);

            if (stageMusicSourcesToMute != null)
            {
                foreach (var src in stageMusicSourcesToMute)
                {
                    if (src != null) src.volume = Mathf.Lerp(src.volume, 0f, p);
                }
            }
            yield return null;
        }

        if (stageMusicSourcesToMute != null)
        {
            foreach (var src in stageMusicSourcesToMute)
            {
                if (src != null) src.Stop();
            }
        }
    }

    private IEnumerator PlayerDeathRoutine()
    {
        if (playerTransform != null)
        {
            if (playerTransform.TryGetComponent<Rigidbody2D>(out var rb)) rb.simulated = false;
            if (playerTransform.TryGetComponent<CharacterMovement>(out var move)) move.enabled = false;
        }

        Vector3 playerStartPos = playerTransform != null ? playerTransform.position : Vector3.zero;
        Vector3 playerStartScale = playerTransform != null ? playerTransform.localScale : Vector3.one;
        Vector3 eyeStartScale = transform.localScale;
        float startPupil = _eyeMat.GetFloat(PupilProp);

        float duration = 1.2f;
        float t = 0f;
        _eyeMat.SetFloat(SpeedProp, 7.0f);
        GamepadFX.Pulse(0.9f, duration);

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / duration;
            float easeIn = p * p * p;

            _eyeMat.SetFloat(PupilProp, Mathf.Lerp(startPupil, 0.44f, easeIn));
            transform.localScale = Vector3.Lerp(eyeStartScale, eyeStartScale * 2.5f, easeIn);

            if (eyeLight != null)
                eyeLight.intensity = Mathf.Lerp(1.4f, 5.5f, easeIn);

            if (playerTransform != null)
            {
                playerTransform.position = Vector3.Lerp(playerStartPos, transform.position, easeIn);
                playerTransform.localScale = Vector3.Lerp(playerStartScale, Vector3.zero, easeIn);
                playerTransform.Rotate(0f, 0f, 720f * Time.unscaledDeltaTime);
            }

            yield return null;
        }

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