using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class StageProgressionManager : MonoBehaviour
{
    [Serializable]
    public class GameStage
    {
        public string stageName = "Fire Stage";
        public Element element = Element.Fire;
        public AudioClip stageMusic;
        [Tooltip("Optional Volume Profile to swap in, or leave null to automatically tint Bloom using ElementSO.Color")]
        public VolumeProfile stageVolumeProfile;
    }

    [Header("Timing Settings")]
    [SerializeField] private float stageIntervalSeconds = 25f;
    [SerializeField] private bool loopStages = true;

    [Header("Stage Definitions")]
    [SerializeField] private List<GameStage> stages = new List<GameStage>();

    [Header("Scene References")]
    [SerializeField] private Volume globalVolume;
    [SerializeField] private AudioSource musicSourceA;
    [SerializeField] private AudioSource musicSourceB;

    [Header("Stage Transition Events (Hook Card Modifier UI Here!)")]
    public UnityEvent OnStageChanged;
    public UnityEvent<Element> OnElementStageChanged;

    private int _currentStageIndex = -1;
    private float _timer;
    private bool _usingSourceA = true;
    private Bloom _bloom;

    private void Start()
    {
        if (stages.Count > 0)
        {
            AdvanceStage();
        }
    }

    private void Update()
    {
        if (stages.Count == 0) return;

        _timer += Time.deltaTime;
        if (_timer >= stageIntervalSeconds)
        {
            _timer = 0f;
            AdvanceStage();
        }
    }

    [ContextMenu("Force Next Stage")]
    public void AdvanceStage()
    {
        int nextIndex = _currentStageIndex + 1;
        if (nextIndex >= stages.Count)
        {
            if (loopStages) nextIndex = 0;
            else return;
        }

        _currentStageIndex = nextIndex;
        ApplyStage(stages[_currentStageIndex]);

        // Fire UnityEvents for your Card Modifier Roll UI
        OnStageChanged?.Invoke();
        OnElementStageChanged?.Invoke(stages[_currentStageIndex].element);
    }

    private void ApplyStage(GameStage stage)
    {
        // 1. Switch Player Element via your teammate's ElementManager!
        ElementManager.SetElement(stage.element);
        ElementSO elementData = ElementManager.GetData(stage.element);

        // 2. Shift Global Volume Profile or Bloom Tint to match the Element color
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

        // 3. Crossfade Stage Music
        if (stage.stageMusic != null)
        {
            StopAllCoroutines();
            StartCoroutine(CrossfadeMusicRoutine(stage.stageMusic, 1.25f));
        }
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