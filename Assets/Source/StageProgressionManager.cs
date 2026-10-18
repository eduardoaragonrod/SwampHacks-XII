using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;

public class StageProgressionManager : MonoBehaviour
{
    [Serializable]
    public class GameStage
    {
        public string stageName = "Fire";
        public Element element = Element.Fire;
        public AudioClip stageMusic;
        public AudioClip customStageSfx;
        public VolumeProfile stageVolumeProfile;

        [Header("Stage UI (Top HUD)")]
        public GameObject elementIconObject;
        [TextArea(2, 3)]
        public string elementDescription = "FIRE: Recall orbs to slingshot into a blazing Fireball and ram through enemies!";
    }

    [Header("Timing Settings (Real-Time Synced to Music)")]
    [SerializeField] private float stageIntervalSeconds = 20f;
    [SerializeField] private bool loopStages = true;
    [SerializeField] private bool syncTimerWithAudioPitch = true;

    [Header("Crossfade Durations")]
    [SerializeField] private float musicCrossfadeDuration = 4.0f;
    [SerializeField] private float uiCrossfadeDuration = 0.85f;
    [SerializeField] private float initialHudFadeInDuration = 1.25f;

    [Header("Stage Transition SFX")]
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip defaultStageChangeSfx;
    [SerializeField, Range(0f, 1f)] private float stageSfxVolume = 1.0f;
    [SerializeField] private bool playSfxOnFirstStage = false;

    [Header("Stage Definitions")]
    [SerializeField] private List<GameStage> stages = new List<GameStage>();

    [Header("Scene & UI References")]
    [SerializeField] private GameObject hudRootObject;
    [SerializeField] private Volume globalVolume;
    [SerializeField] private AudioSource musicSourceA;
    [SerializeField] private AudioSource musicSourceB;
    [SerializeField] private AudioSource songSeperator;
    [SerializeField] private TMP_Text elementTutorialText;

    [Header("Stage Transition Events (Hook Card Modifier UI Here!)")]
    public UnityEvent OnStageChanged;
    public UnityEvent<Element> OnElementStageChanged;

    private int _currentStageIndex = -1;
    private float _timer;
    private bool _usingSourceA = true;
    private Bloom _bloom;

    private CanvasGroup _hudRootCanvasGroup;
    private Coroutine _musicFadeRoutine;
    private Coroutine _uiFadeRoutine;
    private Coroutine _hudRootFadeRoutine;

    private void Awake()
    {
        if (hudRootObject != null)
        {
            _hudRootCanvasGroup = GetOrAddCanvasGroup(hudRootObject);
            _hudRootCanvasGroup.alpha = 0f;
        }

        foreach (var s in stages)
        {
            if (s.elementIconObject != null)
            {
                var cg = GetOrAddCanvasGroup(s.elementIconObject);
                cg.alpha = 0f;
                s.elementIconObject.SetActive(false);
            }
        }

        if (elementTutorialText != null)
        {
            elementTutorialText.alpha = 0f;
        }
    }

    private void OnEnable()
    {
        ElementManager.OnChanged += SyncUIWithElement;

        if (_hudRootCanvasGroup != null)
        {
            if (hudRootObject != null && !hudRootObject.activeSelf)
                hudRootObject.SetActive(true);

            if (_hudRootFadeRoutine != null) StopCoroutine(_hudRootFadeRoutine);
            _hudRootFadeRoutine = StartCoroutine(FadeCanvasGroupRoutine(_hudRootCanvasGroup, 1f, initialHudFadeInDuration));
        }

        if (_currentStageIndex == -1 && stages.Count > 0)
        {
            AdvanceStage(isFirstStage: true);
        }
    }

    private void OnDisable()
    {
        ElementManager.OnChanged -= SyncUIWithElement;
    }

    private void Update()
    {
        if (stages.Count == 0) return;

        float delta = Time.unscaledDeltaTime;

        if (syncTimerWithAudioPitch)
        {
            AudioSource currentActive = _usingSourceA ? musicSourceB : musicSourceA;
            if (currentActive != null && currentActive.isPlaying)
            {
                delta *= Mathf.Abs(currentActive.pitch);
            }
        }

        _timer += delta;
        if (_timer >= stageIntervalSeconds)
        {
            _timer = 0f;
            AdvanceStage(isFirstStage: false);
        }
    }

    [ContextMenu("Force Next Stage")]
    public void AdvanceStage()
    {
        AdvanceStage(isFirstStage: false);
    }

    private void AdvanceStage(bool isFirstStage)
    {
        int nextIndex = _currentStageIndex + 1;
        if (nextIndex >= stages.Count)
        {
            if (loopStages) nextIndex = 0;
            else return;
        }

        _currentStageIndex = nextIndex;
        ApplyStage(stages[_currentStageIndex], isFirstStage);

        OnStageChanged?.Invoke();
        OnElementStageChanged?.Invoke(stages[_currentStageIndex].element);
    }

    public void FadeOutEntireHUD(float duration = 1.0f)
    {
        if (_hudRootCanvasGroup != null && gameObject.activeInHierarchy)
        {
            if (_hudRootFadeRoutine != null) StopCoroutine(_hudRootFadeRoutine);
            _hudRootFadeRoutine = StartCoroutine(FadeCanvasGroupRoutine(_hudRootCanvasGroup, 0f, duration));
        }
    }

    private void ApplyStage(GameStage stage, bool isFirstStage)
    {
        ElementManager.SetElement(stage.element);
        SyncUIWithElement(stage.element);

        ElementSO elementData = ElementManager.GetData(stage.element);

        if (!isFirstStage || playSfxOnFirstStage)
        {
            AudioClip clipToPlay = stage.customStageSfx != null ? stage.customStageSfx : defaultStageChangeSfx;
            if (clipToPlay != null)
            {
                if (sfxAudioSource != null)
                {
                    sfxAudioSource.PlayOneShot(clipToPlay, stageSfxVolume);
                }
                else if (Camera.main != null)
                {
                    AudioSource.PlayClipAtPoint(clipToPlay, Camera.main.transform.position, stageSfxVolume);
                }
            }
        }

        if (globalVolume != null)
        {
            if (stage.stageVolumeProfile != null)
            {
                globalVolume.profile = stage.stageVolumeProfile;
            }

            if (globalVolume.profile != null && globalVolume.profile.TryGet(out _bloom) && elementData != null)
            {
                _bloom.tint.overrideState = true;
                _bloom.tint.value = elementData.Color;
            }
        }

        if (stage.stageMusic != null)
        {
            if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
            _musicFadeRoutine = StartCoroutine(CrossfadeMusicRoutine(stage.stageMusic, musicCrossfadeDuration));
        }
    }

    private void SyncUIWithElement(Element activeElement)
    {
        if (!gameObject.activeInHierarchy) return;

        if (_uiFadeRoutine != null) StopCoroutine(_uiFadeRoutine);
        _uiFadeRoutine = StartCoroutine(CrossfadeStageUIRoutine(activeElement, uiCrossfadeDuration));
    }

    private IEnumerator CrossfadeStageUIRoutine(Element activeElement, float duration)
    {
        GameStage targetStage = null;
        List<CanvasGroup> fadeInGroups = new List<CanvasGroup>();
        List<CanvasGroup> fadeOutGroups = new List<CanvasGroup>();

        foreach (var s in stages)
        {
            bool isMatch = (s.element == activeElement);
            if (isMatch) targetStage = s;

            if (s.elementIconObject != null)
            {
                CanvasGroup cg = GetOrAddCanvasGroup(s.elementIconObject);
                if (isMatch)
                {
                    s.elementIconObject.SetActive(true);
                    fadeInGroups.Add(cg);
                }
                else if (s.elementIconObject.activeSelf && cg.alpha > 0.01f)
                {
                    fadeOutGroups.Add(cg);
                }
            }
        }

        float halfDuration = Mathf.Max(0.05f, duration * 0.5f);
        float startTextAlpha = elementTutorialText != null ? elementTutorialText.alpha : 0f;
        bool swappedText = false;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            float smooth = p * p * (3f - 2f * p);

            foreach (var cg in fadeInGroups)
            {
                if (cg != null) cg.alpha = Mathf.Lerp(cg.alpha, 1f, smooth);
            }
            foreach (var cg in fadeOutGroups)
            {
                if (cg != null) cg.alpha = Mathf.Lerp(1f, 0f, smooth);
            }

            if (elementTutorialText != null && targetStage != null)
            {
                if (t < halfDuration)
                {
                    elementTutorialText.alpha = Mathf.Lerp(startTextAlpha, 0f, t / halfDuration);
                }
                else
                {
                    if (!swappedText)
                    {
                        swappedText = true;
                        elementTutorialText.text = targetStage.elementDescription;
                        var data = ElementManager.GetData(activeElement);
                        if (data != null)
                        {
                            elementTutorialText.color = Color.Lerp(Color.white, data.Color, 0.45f);
                        }
                    }

                    float inProgress = (t - halfDuration) / halfDuration;
                    elementTutorialText.alpha = Mathf.Lerp(0f, 1f, inProgress);
                }
            }

            yield return null;
        }

        foreach (var cg in fadeInGroups)
        {
            if (cg != null) cg.alpha = 1f;
        }
        foreach (var cg in fadeOutGroups)
        {
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.gameObject.SetActive(false);
            }
        }

        if (elementTutorialText != null && targetStage != null)
        {
            elementTutorialText.text = targetStage.elementDescription;
            elementTutorialText.alpha = 1f;
        }
    }

    private IEnumerator FadeCanvasGroupRoutine(CanvasGroup cg, float targetAlpha, float duration)
    {
        float startAlpha = cg.alpha;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, p * p * (3f - 2f * p));
            yield return null;
        }

        cg.alpha = targetAlpha;
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        if (!obj.TryGetComponent<CanvasGroup>(out var cg))
        {
            cg = obj.AddComponent<CanvasGroup>();
        }
        return cg;
    }

    private IEnumerator CrossfadeMusicRoutine(AudioClip nextClip, float duration)
    {
        AudioSource active = _usingSourceA ? musicSourceA : musicSourceB;
        AudioSource incoming = _usingSourceA ? musicSourceB : musicSourceA;
        _usingSourceA = !_usingSourceA;

        if (incoming == null) yield break;

        incoming.clip = nextClip;
        incoming.volume = 0f;
        incoming.Play();

        float t = 0f;
        float startVol = active != null ? active.volume : 1f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / duration;
            if (active != null) active.volume = Mathf.Lerp(startVol, 0f, p);
            incoming.volume = Mathf.Lerp(0f, 1f, p);
            yield return null;
        }

        if (active != null) active.Stop();
        incoming.volume = 1f;
        

        
    }
}