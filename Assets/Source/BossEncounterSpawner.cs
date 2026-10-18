using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackHoleEncounterController : MonoBehaviour
{
    [Header("Core References")]
    [Tooltip("Center of the Black Hole Eye where enemies and bullets are birthed from")]
    [SerializeField] private Transform blackHoleCenter;
    [SerializeField] private Renderer eyeRenderer;
    [SerializeField] private SunEaterDirector sunEaterDirector;
    [SerializeField] private StageProgressionManager stageProgressionManager;

    [Header("Phase 1: Enemy Spawner Settings")]
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
    [SerializeField] private float initialSpawnInterval = 2.8f;
    [SerializeField] private float minSpawnInterval = 0.9f;
    [SerializeField] private float spawnAccelerationPerSecond = 0.02f;
    [SerializeField] private int maxActiveEnemies = 22;

    [Header("Black Hole Ejection Animation")]
    [Tooltip("Min and Max distance from the Black Hole center where enemies land")]
    [SerializeField] private float minLandingRadius = 3.2f;
    [SerializeField] private float maxLandingRadius = 7.5f;
    [SerializeField] private float ejectionDuration = 0.65f;
    [SerializeField] private float spiralArcRadians = 2.2f;

    [Header("Phase 2: Final Boss Fight")]
    [Tooltip("Drag your MissileSO asset (e.g. TempMissile) here!")]
    [SerializeField] private MissileSO bossMissileData;
    [SerializeField] private bool spawnMinionsDuringBoss = true;
    [SerializeField] private float bossMinionInterval = 6.5f;

    [Header("Bullet Hell Pattern Tuning")]
    [SerializeField] private int spiralStreams = 3;
    [SerializeField] private float spiralFireRate = 0.11f;
    [SerializeField] private float spiralRotationSpeed = 110f;
    [SerializeField] private int supernovaRingBulletCount = 18;
    [SerializeField] private int aimedBurstCount = 5;
    [SerializeField] private float aimedBurstSpreadAngle = 40f;

    private Material _eyeMat;
    private float _currentSpawnInterval;
    private bool _isSpawningMobs;
    private bool _isBossBulletHellActive;
    private readonly List<GameObject> _activeEnemies = new List<GameObject>();

    private static readonly int ShockwaveProp = Shader.PropertyToID("_ShockwaveRadius");
    private static readonly int SpeedProp = Shader.PropertyToID("_RotationSpeed");
    private static readonly int OutlineColorProp = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineThickProp = Shader.PropertyToID("_OutlineThickness");

    private void Awake()
    {
        if (blackHoleCenter == null) blackHoleCenter = transform;
        if (eyeRenderer != null) _eyeMat = eyeRenderer.material;
        _currentSpawnInterval = initialSpawnInterval;
    }

    private void OnEnable()
    {
        if (sunEaterDirector != null)
        {
            sunEaterDirector.OnBossStarted.AddListener(StopMobSpawningForCutscene);
            sunEaterDirector.OnBossFightActive.AddListener(StartBossBulletHellFinale);
        }

        if (stageProgressionManager != null && !stageProgressionManager.enabled)
            stageProgressionManager.enabled = true;

        if (sunEaterDirector != null && !sunEaterDirector.enabled)
            sunEaterDirector.enabled = true;

        StartMobSpawner();
    }

    private void OnDisable()
    {
        if (sunEaterDirector != null)
        {
            sunEaterDirector.OnBossStarted.RemoveListener(StopMobSpawningForCutscene);
            sunEaterDirector.OnBossFightActive.RemoveListener(StartBossBulletHellFinale);
        }
        StopAllCoroutines();
    }

    public void StartMobSpawner()
    {
        if (_isBossBulletHellActive) return;
        _isSpawningMobs = true;
        StopAllCoroutines();
        StartCoroutine(MobSpawnLoopRoutine());
    }

    public void StopMobSpawningForCutscene()
    {
        _isSpawningMobs = false;
        StopAllCoroutines();
    }

    [ContextMenu("Test Boss Bullet Hell Finale")]
    public void StartBossBulletHellFinale()
    {
        _isSpawningMobs = false;
        _isBossBulletHellActive = true;
        StopAllCoroutines();
        StartCoroutine(BossBulletHellMasterRoutine());

        if (spawnMinionsDuringBoss)
        {
            StartCoroutine(BossSupportMinionRoutine());
        }
    }

   
    private IEnumerator MobSpawnLoopRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        while (_isSpawningMobs)
        {
            _activeEnemies.RemoveAll(e => e == null);

            if (_activeEnemies.Count < maxActiveEnemies && enemyPrefabs.Count > 0)
            {
                SpawnEnemyFromBlackHole();
            }

            yield return new WaitForSeconds(_currentSpawnInterval);

            _currentSpawnInterval = Mathf.Max(
                minSpawnInterval,
                _currentSpawnInterval - (spawnAccelerationPerSecond * _currentSpawnInterval)
            );
        }
    }

    private void SpawnEnemyFromBlackHole()
    {
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        if (prefab == null) return;

        float randomAngle = Random.Range(0f, Mathf.PI * 2f);
        float randomRadius = Random.Range(minLandingRadius, maxLandingRadius);
        Vector2 origin = blackHoleCenter.position;
        Vector2 targetPos = origin + new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * randomRadius;

        if (Player.Instance != null && Vector2.Distance(targetPos, Player.Instance.transform.position) < 2.0f)
        {
            randomAngle += Mathf.PI;
            targetPos = origin + new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * randomRadius;
        }

        GameObject instance = Instantiate(prefab, origin, Quaternion.identity);
        _activeEnemies.Add(instance);

        StartCoroutine(EjectEnemyFromSingularirtyRoutine(instance, origin, targetPos, randomAngle, randomRadius));
    }

    private IEnumerator EjectEnemyFromSingularirtyRoutine(GameObject enemy, Vector2 origin, Vector2 targetPos, float endAngle, float endRadius)
    {
        if (enemy == null) yield break;

        Vector3 targetScale = enemy.transform.localScale;
        enemy.transform.localScale = Vector3.zero;

        Collider2D col = enemy.GetComponent<Collider2D>();
        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        MonoBehaviour[] scripts = enemy.GetComponents<MonoBehaviour>();

        if (col != null) col.enabled = false;
        if (rb != null) rb.simulated = false;
        foreach (var s in scripts)
        {
            if (s != null) s.enabled = false;
        }

        SpriteRenderer sr = enemy.GetComponentInChildren<SpriteRenderer>();
        Color originalOutline = Color.black;
        float originalThickness = 0f;

        if (sr != null)
        {
            if (sr.material.HasProperty(OutlineColorProp))
                originalOutline = sr.material.GetColor(OutlineColorProp);
            if (sr.material.HasProperty(OutlineThickProp))
                originalThickness = sr.material.GetFloat(OutlineThickProp);

            Color ejectGlow = ElementManager.Current.HasValue 
                ? ElementManager.GetData(ElementManager.Current.Value).Color * 2.2f 
                : new Color(2.5f, 1.5f, 0.3f, 1f);

            sr.material.SetColor(OutlineColorProp, ejectGlow);
            sr.material.SetFloat(OutlineThickProp, 2.5f);
        }

        float t = 0f;
        float startAngle = endAngle - spiralArcRadians;

        while (t < ejectionDuration)
        {
            if (enemy == null) yield break;

            t += Time.deltaTime;
            float p = t / ejectionDuration;
            float easeOut = 1f - Mathf.Pow(1f - p, 3f);

            float currentAngle = Mathf.Lerp(startAngle, endAngle, easeOut);
            float currentRadius = Mathf.Lerp(0f, endRadius, easeOut);
            Vector2 spiralPos = origin + new Vector2(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle)) * currentRadius;

            enemy.transform.position = spiralPos;
            enemy.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, easeOut);

            yield return null;
        }

        if (enemy == null) yield break;

        enemy.transform.position = targetPos;
        enemy.transform.localScale = targetScale;

        if (sr != null)
        {
            sr.material.SetColor(OutlineColorProp, originalOutline);
            sr.material.SetFloat(OutlineThickProp, originalThickness);
        }

        if (col != null) col.enabled = true;
        if (rb != null) rb.simulated = true;
        foreach (var s in scripts)
        {
            if (s != null) s.enabled = true;
        }
    }

    private IEnumerator BossBulletHellMasterRoutine()
    {
        if (bossMissileData == null)
        {
            Debug.LogWarning("BlackHoleEncounterController: Please assign a MissileSO to 'Boss Missile Data' in the Inspector!");
            yield break;
        }

        yield return new WaitForSeconds(0.5f);

        while (_isBossBulletHellActive)
        {
            yield return StartCoroutine(PatternSpiralGalaxyRoutine(4.5f));
            yield return new WaitForSeconds(0.8f);

            yield return StartCoroutine(PatternSupernovaRingsRoutine(4));
            yield return new WaitForSeconds(0.8f);

            yield return StartCoroutine(PatternPredatorBarrageRoutine(3.5f));
            yield return new WaitForSeconds(1.0f);
        }
    }

    private IEnumerator PatternSpiralGalaxyRoutine(float duration)
    {
        if (_eyeMat != null) _eyeMat.SetFloat(SpeedProp, 3.8f);

        float elapsed = 0f;
        float currentAngle = 0f;

        while (elapsed < duration)
        {
            Vector2 origin = blackHoleCenter.position;
            float angleStep = 360f / Mathf.Max(1, spiralStreams);

            for (int i = 0; i < spiralStreams; i++)
            {
                float rad = (currentAngle + i * angleStep) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                MissileManager.Spawn(bossMissileData, origin, dir);
            }

            currentAngle += spiralRotationSpeed * spiralFireRate;
            elapsed += spiralFireRate;
            yield return new WaitForSeconds(spiralFireRate);
        }

        if (_eyeMat != null) _eyeMat.SetFloat(SpeedProp, 1.5f);
    }

    private IEnumerator PatternSupernovaRingsRoutine(int waveCount)
    {
        for (int w = 0; w < waveCount; w++)
        {
            StartCoroutine(PulseEyeShockwaveRoutine(0.35f));
            GamepadFX.Pulse(0.45f, 0.12f);

            Vector2 origin = blackHoleCenter.position;
            float angleOffset = (w % 2 == 0) ? 0f : (180f / supernovaRingBulletCount);

            for (int i = 0; i < supernovaRingBulletCount; i++)
            {
                float deg = (i * (360f / supernovaRingBulletCount)) + angleOffset;
                float rad = deg * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                MissileManager.Spawn(bossMissileData, origin, dir);
            }

            yield return new WaitForSeconds(0.45f);

            if (Player.Instance != null)
            {
                Vector2 toPlayer = ((Vector2)Player.Instance.transform.position - origin).normalized;
                FireSpreadAtDirection(origin, toPlayer, aimedBurstCount, aimedBurstSpreadAngle);
            }

            yield return new WaitForSeconds(0.55f);
        }
    }

    private IEnumerator PatternPredatorBarrageRoutine(float duration)
    {
        float elapsed = 0f;
        float spinAngle = 0f;

        while (elapsed < duration)
        {
            Vector2 origin = blackHoleCenter.position;

            for (int i = 0; i < 2; i++)
            {
                float a1 = (spinAngle + i * 180f) * Mathf.Deg2Rad;
                float a2 = (-spinAngle + i * 180f) * Mathf.Deg2Rad;
                MissileManager.Spawn(bossMissileData, origin, new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)));
                MissileManager.Spawn(bossMissileData, origin, new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)));
            }

            spinAngle += 22f;
            elapsed += 0.14f;
            yield return new WaitForSeconds(0.14f);
        }
    }

    private void FireSpreadAtDirection(Vector2 origin, Vector2 centerDir, int count, float totalSpreadDegrees)
    {
        if (count <= 1)
        {
            MissileManager.Spawn(bossMissileData, origin, centerDir);
            return;
        }

        float startAngle = -totalSpreadDegrees * 0.5f;
        float step = totalSpreadDegrees / (count - 1);

        for (int i = 0; i < count; i++)
        {
            Vector2 dir = Quaternion.AngleAxis(startAngle + step * i, Vector3.forward) * centerDir;
            MissileManager.Spawn(bossMissileData, origin, dir);
        }
    }

    private IEnumerator PulseEyeShockwaveRoutine(float duration)
    {
        if (_eyeMat == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _eyeMat.SetFloat(ShockwaveProp, Mathf.Lerp(0.1f, 0.48f, t / duration));
            yield return null;
        }
        _eyeMat.SetFloat(ShockwaveProp, 0f);
    }

    private IEnumerator BossSupportMinionRoutine()
    {
        while (_isBossBulletHellActive)
        {
            yield return new WaitForSeconds(bossMinionInterval);
            _activeEnemies.RemoveAll(e => e == null);
            if (_activeEnemies.Count < 6 && enemyPrefabs.Count > 0)
            {
                SpawnEnemyFromBlackHole();
            }
        }
    }
}