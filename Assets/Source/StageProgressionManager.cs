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
        public VolumeProfile stageVolumeProfile;

        [Header("Stage UI (Top HUD)")]
        public GameObject elementIconObject;
        [TextArea(2, 3)]
        public string elementDescription = "FIRE: Recall orbs to slingshot into a blazing Fireball and ram through enemies!";
    }

    [Header("Timing Settings")]
    [SerializeField] private float stageIntervalSeconds = 20f;
    [SerializeField] private bool loopStages = true;

    [Header("Stage Definitions")]
    [SerializeField] private List<GameStage> stages = new List<GameStage>();

    [Header("Scene & UI References")]
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

    private void Start()
    {
        ElementManager.OnChanged += SyncUIWithElement;

        if (stages.Count > 0)
        {
            AdvanceStage();
        }
    }

    private void OnDestroy()
    {
        ElementManager.OnChanged -= SyncUIWithElement;
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

        OnStageChanged?.Invoke();
        OnElementStageChanged?.Invoke(stages[_currentStageIndex].element);
    }

    private void ApplyStage(GameStage stage)
    {
        ElementManager.SetElement(stage.element);
        SyncUIWithElement(stage.element);

        ElementSO elementData = ElementManager.GetData(stage.element);

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
            StopAllCoroutines();
            StartCoroutine(CrossfadeMusicRoutine(stage.stageMusic, 5f));
        }
    }

    private void SyncUIWithElement(Element activeElement)
    {
        foreach (var s in stages)
        {
            bool isMatch = (s.element == activeElement);

            if (s.elementIconObject != null)
            {
                s.elementIconObject.SetActive(isMatch);
            }

            if (isMatch && elementTutorialText != null)
            {
                elementTutorialText.text = s.elementDescription;
                var data = ElementManager.GetData(activeElement);
                if (data != null)
                {
                    elementTutorialText.color = Color.Lerp(Color.white, data.Color, 0.45f);
                }
            }
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