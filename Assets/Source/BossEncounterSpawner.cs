using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BlackHoleEncounterController : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private Transform blackHoleCenter;
    [SerializeField] private Renderer eyeRenderer;
    [SerializeField] private SunEaterDirector sunEaterDirector;
    [SerializeField] private StageProgressionManager stageProgressionManager;
    [SerializeField] private BossTimeBarUI bossTimeBarUI;

    [Header("Phase 1: Enemy Spawner Settings")]
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
    [SerializeField] private float initialSpawnInterval = 2.8f;
    [SerializeField] private float minSpawnInterval = 0.9f;
    [SerializeField] private float spawnAccelerationPerSecond = 0.02f;
    [SerializeField] private int maxActiveEnemies = 22;

    [Header("Black Hole Ejection Animation")]
    [SerializeField] private float minLandingRadius = 3.2f;
    [SerializeField] private float maxLandingRadius = 7.5f;
    [SerializeField] private float ejectionDuration = 0.65f;
    [SerializeField] private float spiralArcRadians = 2.2f;

    [Header("Boss Survival Timer & Phase 2 Settings")]
    [SerializeField] private float bossSurvivalDuration = 36f;
    [SerializeField] private TMP_Text bossCountdownText;
    [SerializeField] private string phase2Announcement = "PHASE 2: THE ABYSS COLLAPSES — KEEP MOVING!";
    [SerializeField] private string victoryMessage = "THE SUN IS REBORN — YOU CONQUERED THE ABYSS!";

    [Header("Shrinking Arena Ring (Anti-Outskirt Camping)")]
    [SerializeField] private float startSafeRadius = 11.5f;
    [SerializeField] private float minSafeRadius = 5.2f;
    [SerializeField] private float outsideZoneDamage = 8f;
    [SerializeField] private float outsideZonePushback = 14f;
    [SerializeField] private float outsideDamageTickRate = 0.4f;

    [Header("Phase 2: Red Telegraph Strike Zones (Keep Player Moving!)")]
    [SerializeField] private float redZoneSpawnInterval = 1.65f;
    [SerializeField] private float redZoneWarningDuration = 0.85f;
    [SerializeField] private float redZoneRadius = 2.1f;
    [SerializeField] private float redZoneExplosionDamage = 18f;
    [SerializeField] private int redZoneShrapnelBullets = 8;

    [Header("Boss Bullet Hell Finale (Uses MissileManager)")]
    [SerializeField] private MissileSO bossMissileData;
    [SerializeField] private bool spawnMinionsDuringBoss = true;
    [SerializeField] private float bossMinionInterval = 6.5f;

    [Header("Bullet Hell Base Pattern Tuning")]
    [SerializeField] private int spiralStreams = 3;
    [SerializeField] private float spiralFireRate = 0.12f;
    [SerializeField] private float spiralRotationSpeed = 110f;
    [SerializeField] private int supernovaRingBulletCount = 16;
    [SerializeField] private int aimedBurstCount = 5;
    [SerializeField] private float aimedBurstSpreadAngle = 40f;

    private Material _eyeMat;
    private float _currentSpawnInterval;
    private bool _isSpawningMobs;
    private bool _isBossBulletHellActive;
    private bool _isPhase2;
    private float _bossRampFactor;
    private float _currentSafeRadius;
    private float _nextOutsideDamageTime;
    private LineRenderer _arenaBorderRing;
    private readonly List<GameObject> _activeEnemies = new List<GameObject>();
    private readonly List<GameObject> _activeTelegraphs = new List<GameObject>();

    private static readonly int ShockwaveProp = Shader.PropertyToID("_ShockwaveRadius");
    private static readonly int SpeedProp = Shader.PropertyToID("_RotationSpeed");
    private static readonly int OutlineColorProp = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineThickProp = Shader.PropertyToID("_OutlineThickness");

    private void Awake()
    {
        if (blackHoleCenter == null) blackHoleCenter = transform;
        if (eyeRenderer != null) _eyeMat = eyeRenderer.material;
        _currentSpawnInterval = initialSpawnInterval;
        _currentSafeRadius = startSafeRadius;
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
        _isPhase2 = false;
        _bossRampFactor = 0f;
        _currentSafeRadius = startSafeRadius;

        // Unlock all elements for the player during the Final Boss!
        if (Player.Instance != null)
        {
            Player.Instance.UnlockAllElementsForBoss();
        }

        // Start the Boss Health/Time Bar UI!
        if (bossTimeBarUI != null)
        {
            bossTimeBarUI.gameObject.SetActive(true);
            bossTimeBarUI.StartBossBarCountdown(bossSurvivalDuration);
        }

        CreateShrinkingArenaRingVisual();

        StopAllCoroutines();
        StartCoroutine(BossSurvivalTimerRoutine());
        StartCoroutine(BossBulletHellMasterRoutine());

        if (spawnMinionsDuringBoss)
        {
            StartCoroutine(BossSupportMinionRoutine());
        }
    }

    // =========================================================================
    // BOSS SURVIVAL COUNTDOWN, SHRINKING RING & PHASE 2 TRIGGER
    // =========================================================================
    private IEnumerator BossSurvivalTimerRoutine()
    {
        float remaining = bossSurvivalDuration;

        while (remaining > 0f && _isBossBulletHellActive)
        {
            remaining -= Time.unscaledDeltaTime;
            _bossRampFactor = Mathf.Clamp01(1f - (remaining / Mathf.Max(1f, bossSurvivalDuration)));

            // Trigger Phase 2 at 50% time remaining!
            if (!_isPhase2 && _bossRampFactor >= 0.5f)
            {
                _isPhase2 = true;
                StartCoroutine(Phase2RedZoneSpawnerRoutine());
                StartCoroutine(PulseEyeShockwaveRoutine(0.5f));
                GamepadFX.Pulse(0.9f, 0.35f);

                if (bossCountdownText != null)
                {
                    bossCountdownText.text = phase2Announcement;
                    bossCountdownText.color = new Color(1f, 0.2f, 0.25f, 1f);
                }
            }

            // Update Shrinking Safe Zone Radius & Check Player Out-of-Bounds
            UpdateShrinkingArenaZone();

            yield return null;
        }

        // PLAYER SURVIVED! Clear all hazards and trigger Sun Rebirth!
        _isBossBulletHellActive = false;
        StopAllCoroutines();

        ClearAllActiveMissilesAndEnemies();

        if (_arenaBorderRing != null)
            Destroy(_arenaBorderRing.gameObject);

        if (bossCountdownText != null)
        {
            bossCountdownText.color = new Color(1f, 0.9f, 0.4f, 1f);
            bossCountdownText.text = victoryMessage;
        }

        if (sunEaterDirector != null)
        {
            sunEaterDirector.TriggerBossWinSequence();
        }
    }

    // =========================================================================
    // SHRINKING ARENA VOID RING (PREVENTS OUTSKIRT CAMPING)
    // =========================================================================
    private void CreateShrinkingArenaRingVisual()
    {
        if (_arenaBorderRing != null) Destroy(_arenaBorderRing.gameObject);

        GameObject ringObj = new GameObject("ShrinkingVoidBorder");
        ringObj.transform.SetParent(blackHoleCenter, false);
        _arenaBorderRing = ringObj.AddComponent<LineRenderer>();
        _arenaBorderRing.useWorldSpace = true;
        _arenaBorderRing.loop = true;
        _arenaBorderRing.positionCount = 64;
        _arenaBorderRing.startWidth = 0.22f;
        _arenaBorderRing.endWidth = 0.22f;
        _arenaBorderRing.material = new Material(Shader.Find("Sprites/Default"));
        _arenaBorderRing.sortingOrder = 5;
    }

    private void UpdateShrinkingArenaZone()
    {
        // Shrinks faster once Phase 2 begins
        float shrinkCurve = _isPhase2 ? Mathf.Pow(_bossRampFactor, 0.85f) : _bossRampFactor * 0.7f;
        _currentSafeRadius = Mathf.Lerp(startSafeRadius, minSafeRadius, shrinkCurve);

        if (_arenaBorderRing != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (_isPhase2 ? 12f : 5f));
            Color ringColor = Color.Lerp(new Color(1f, 0.35f, 0.1f, 0.75f), new Color(1f, 0.08f, 0.2f, 0.95f), pulse);
            _arenaBorderRing.startColor = ringColor;
            _arenaBorderRing.endColor = ringColor;

            Vector2 center = blackHoleCenter.position;
            int segments = _arenaBorderRing.positionCount;
            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f + Time.time * 0.4f;
                Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _currentSafeRadius;
                _arenaBorderRing.SetPosition(i, pos);
            }
        }

        // Check if Player is camping outside the shrinking safe ring!
        if (Player.Instance != null && Time.time >= _nextOutsideDamageTime)
        {
            Vector2 playerPos = Player.Instance.transform.position;
            Vector2 center = blackHoleCenter.position;
            float distFromCenter = Vector2.Distance(playerPos, center);

            if (distFromCenter > _currentSafeRadius)
            {
                _nextOutsideDamageTime = Time.time + outsideDamageTickRate;
                Vector2 pushInwardDir = (center - playerPos).normalized;

                if (Player.Instance.TryGetComponent<IDamageable>(out var dmg))
                {
                    dmg.TryTakeDamage(new DamageInfo(outsideZoneDamage, Team.Hostile, pushInwardDir, outsideZonePushback));
                }
                else if (Player.Instance.TryGetComponent<CharacterMovement>(out var move))
                {
                    move.ApplyKnockback(pushInwardDir * outsideZonePushback);
                }

                GamepadFX.Pulse(0.6f, 0.12f);
            }
        }
    }

    // =========================================================================
    // PHASE 2: RED TELEGRAPH STRIKE ZONES ON PLAYER POSITION
    // =========================================================================
    private IEnumerator Phase2RedZoneSpawnerRoutine()
    {
        while (_isBossBulletHellActive && _isPhase2)
        {
            if (Player.Instance != null)
            {
                Vector2 targetSpot = Player.Instance.transform.position;
                StartCoroutine(SpawnRedStrikeZoneRoutine(targetSpot));
            }

            float currentInterval = Mathf.Lerp(redZoneSpawnInterval, redZoneSpawnInterval * 0.65f, (_bossRampFactor - 0.5f) * 2f);
            yield return new WaitForSeconds(currentInterval);
        }
    }

    private IEnumerator SpawnRedStrikeZoneRoutine(Vector2 targetPos)
    {
        // Create procedural glowing red telegraph circle + inner fill ring
        GameObject zoneObj = new GameObject("BossRedStrikeTelegraph");
        zoneObj.transform.position = targetPos;
        _activeTelegraphs.Add(zoneObj);

        LineRenderer outerRing = zoneObj.AddComponent<LineRenderer>();
        outerRing.useWorldSpace = true;
        outerRing.loop = true;
        outerRing.positionCount = 40;
        outerRing.material = new Material(Shader.Find("Sprites/Default"));
        outerRing.sortingOrder = 4;

        float t = 0f;
        while (t < redZoneWarningDuration && _isBossBulletHellActive)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / redZoneWarningDuration);

            // Radius expands quickly then locks onto the target area while flashing faster
            float currentRad = redZoneRadius * Mathf.Sin(p * Mathf.PI * 0.5f);
            float flash = Mathf.PingPong(t * Mathf.Lerp(6f, 24f, p), 1f);
            Color warnCol = Color.Lerp(new Color(1f, 0.05f, 0.15f, 0.85f), new Color(1f, 0.9f, 0.9f, 1f), flash * p);

            outerRing.startColor = warnCol;
            outerRing.endColor = warnCol;
            outerRing.startWidth = Mathf.Lerp(0.08f, 0.28f, p);
            outerRing.endWidth = outerRing.startWidth;

            for (int i = 0; i < outerRing.positionCount; i++)
            {
                float a = (i / (float)outerRing.positionCount) * Mathf.PI * 2f;
                outerRing.SetPosition(i, targetPos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * currentRad);
            }

            yield return null;
        }

        if (!_isBossBulletHellActive)
        {
            if (zoneObj != null) Destroy(zoneObj);
            yield break;
        }

        // DETONATE STRIKE ZONE!
        GamepadFX.Pulse(0.75f, 0.15f);

        // 1. Damage player if they failed to move out of the red zone
        if (Player.Instance != null)
        {
            float dist = Vector2.Distance(Player.Instance.transform.position, targetPos);
            if (dist <= redZoneRadius)
            {
                Vector2 knockDir = ((Vector2)Player.Instance.transform.position - targetPos).normalized;
                if (knockDir.sqrMagnitude == 0f) knockDir = Vector2.up;

                if (Player.Instance.TryGetComponent<IDamageable>(out var dmg))
                {
                    dmg.TryTakeDamage(new DamageInfo(redZoneExplosionDamage, Team.Hostile, knockDir, 16f));
                }
            }
        }

        // 2. Burst of shrapnel missiles from the strike point
        if (bossMissileData != null)
        {
            for (int i = 0; i < redZoneShrapnelBullets; i++)
            {
                float rad = (i * (360f / redZoneShrapnelBullets)) * Mathf.Deg2Rad;
                MissileManager.Spawn(bossMissileData, targetPos, new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)));
            }
        }

        _activeTelegraphs.Remove(zoneObj);
        if (zoneObj != null) Destroy(zoneObj);
    }

    private void ClearAllActiveMissilesAndEnemies()
    {
        Missile[] activeMissiles = FindObjectsByType<Missile>(FindObjectsSortMode.None);
        foreach (var m in activeMissiles)
        {
            if (m != null) MissileManager.Kill(m);
        }

        foreach (var e in _activeEnemies)
        {
            if (e != null) Destroy(e);
        }
        _activeEnemies.Clear();

        foreach (var z in _activeTelegraphs)
        {
            if (z != null) Destroy(z);
        }
        _activeTelegraphs.Clear();
    }

    // =========================================================================
    // PHASE 1: BLACK HOLE SPIRAL EJECTION MOB SPAWNER
    // =========================================================================
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
        float randomRadius = Random.Range(minLandingRadius, Mathf.Min(maxLandingRadius, _currentSafeRadius - 0.8f));
        Vector2 origin = blackHoleCenter.position;
        Vector2 targetPos = origin + new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * randomRadius;

        if (Player.Instance != null && Vector2.Distance(targetPos, Player.Instance.transform.position) < 2.0f)
        {
            randomAngle += Mathf.PI;
            targetPos = origin + new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * randomRadius;
        }

        GameObject instance = Instantiate(prefab, origin, Quaternion.identity);
        _activeEnemies.Add(instance);

        StartCoroutine(EjectEnemyFromSingularityRoutine(instance, origin, targetPos, randomAngle, randomRadius));
    }

    private IEnumerator EjectEnemyFromSingularityRoutine(GameObject enemy, Vector2 origin, Vector2 targetPos, float endAngle, float endRadius)
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

    // =========================================================================
    // PHASE 2: RAMPING BOSS BULLET HELL FINALE
    // =========================================================================
    private IEnumerator BossBulletHellMasterRoutine()
    {
        if (bossMissileData == null) yield break;

        yield return new WaitForSeconds(0.4f);

        while (_isBossBulletHellActive)
        {
            float breather = Mathf.Lerp(0.75f, 0.22f, _bossRampFactor);

            yield return StartCoroutine(PatternSpiralGalaxyRoutine(4.0f));
            yield return new WaitForSeconds(breather);

            yield return StartCoroutine(PatternSupernovaRingsRoutine(3));
            yield return new WaitForSeconds(breather);

            yield return StartCoroutine(PatternPredatorBarrageRoutine(3.5f));
            yield return new WaitForSeconds(breather);
        }
    }

    private IEnumerator PatternSpiralGalaxyRoutine(float duration)
    {
        if (_eyeMat != null) _eyeMat.SetFloat(SpeedProp, Mathf.Lerp(3.5f, 7.5f, _bossRampFactor));

        float elapsed = 0f;
        float currentAngle = 0f;

        while (elapsed < duration && _isBossBulletHellActive)
        {
            Vector2 origin = blackHoleCenter.position;
            int activeStreams = spiralStreams + (_isPhase2 ? 1 : 0);
            float angleStep = 360f / Mathf.Max(1, activeStreams);

            for (int i = 0; i < activeStreams; i++)
            {
                float rad = (currentAngle + i * angleStep) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                MissileManager.Spawn(bossMissileData, origin, dir);
            }

            float currentFireRate = Mathf.Lerp(spiralFireRate, spiralFireRate * 0.55f, _bossRampFactor);
            float currentRotSpeed = Mathf.Lerp(spiralRotationSpeed, spiralRotationSpeed * 1.65f, _bossRampFactor);

            currentAngle += currentRotSpeed * currentFireRate;
            elapsed += currentFireRate;
            yield return new WaitForSeconds(currentFireRate);
        }

        if (_eyeMat != null) _eyeMat.SetFloat(SpeedProp, Mathf.Lerp(1.5f, 4.0f, _bossRampFactor));
    }

    private IEnumerator PatternSupernovaRingsRoutine(int waveCount)
    {
        for (int w = 0; w < waveCount && _isBossBulletHellActive; w++)
        {
            StartCoroutine(PulseEyeShockwaveRoutine(0.35f));
            GamepadFX.Pulse(0.45f + 0.3f * _bossRampFactor, 0.12f);

            Vector2 origin = blackHoleCenter.position;
            int ringCount = Mathf.RoundToInt(Mathf.Lerp(supernovaRingBulletCount, supernovaRingBulletCount * 1.4f, _bossRampFactor));
            float angleOffset = (w % 2 == 0) ? 0f : (180f / ringCount);

            for (int i = 0; i < ringCount; i++)
            {
                float deg = (i * (360f / ringCount)) + angleOffset;
                float rad = deg * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                MissileManager.Spawn(bossMissileData, origin, dir);
            }

            float waitTime = Mathf.Lerp(0.45f, 0.25f, _bossRampFactor);
            yield return new WaitForSeconds(waitTime);

            if (Player.Instance != null)
            {
                Vector2 toPlayer = ((Vector2)Player.Instance.transform.position - origin).normalized;
                int burstBullets = aimedBurstCount + (_isPhase2 ? 2 : 0);
                FireSpreadAtDirection(origin, toPlayer, burstBullets, aimedBurstSpreadAngle);
            }

            yield return new WaitForSeconds(waitTime);
        }
    }

    private IEnumerator PatternPredatorBarrageRoutine(float duration)
    {
        float elapsed = 0f;
        float spinAngle = 0f;

        while (elapsed < duration && _isBossBulletHellActive)
        {
            Vector2 origin = blackHoleCenter.position;

            for (int i = 0; i < 2; i++)
            {
                float a1 = (spinAngle + i * 180f) * Mathf.Deg2Rad;
                float a2 = (-spinAngle + i * 180f) * Mathf.Deg2Rad;
                MissileManager.Spawn(bossMissileData, origin, new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)));
                MissileManager.Spawn(bossMissileData, origin, new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)));
            }

            float stepDelay = Mathf.Lerp(0.14f, 0.08f, _bossRampFactor);
            spinAngle += Mathf.Lerp(22f, 34f, _bossRampFactor);
            elapsed += stepDelay;
            yield return new WaitForSeconds(stepDelay);
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