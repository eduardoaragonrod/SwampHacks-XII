using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class EndingCutsceneSlideshow : MonoBehaviour
{
    [Serializable]
    public class CutsceneSlide
    {
        public string slideLabel = "Ending Slide";
        public Sprite slideSprite;
        public Texture2D slideTexture;
        [Tooltip("How long this slide stays fully visible before fading to the next (Ignored on the final slide, which stays forever)")]
        public float holdDuration = 4.5f;
        public float fadeDuration = 1.25f;
        public float startScale = 1.0f;
        public float endScale = 1.05f;
        public AudioClip optionalSlideSfx;

        public Sprite GetResolvedSprite()
        {
            if (slideSprite != null) return slideSprite;
            if (slideTexture != null)
            {
                return Sprite.Create(
                    slideTexture,
                    new Rect(0, 0, slideTexture.width, slideTexture.height),
                    new Vector2(0.5f, 0.5f)
                );
            }
            return null;
        }
    }

    [Header("Build-Up & Screen Fade Settings")]
    [SerializeField] private float buildUpDelay = 2.0f;
    [SerializeField] private float initialScreenFadeDuration = 1.8f;
    [SerializeField] private Color screenFadeColor = new Color(0f, 0f, 0f, 1f);

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource cutsceneAudioSource;

    [Header("Slideshow Sequence (Last Slide Continues Indefinitely)")]
    [SerializeField] private List<CutsceneSlide> slides = new List<CutsceneSlide>();

    [Header("Events")]
    public UnityEvent OnCutsceneFadeStarted;
    public UnityEvent OnFinalSlideReached;

    private Canvas _dedicatedOverlayCanvas;
    private Image _backdropImage;
    private Image _slideImageA;
    private Image _slideImageB;
    private bool _hasStarted;
    private bool _usingImageA = true;

    [ContextMenu("Test Ending Cutscene")]
    public void TriggerEndingCutscene()
    {
        if (_hasStarted) return;
        _hasStarted = true;

        gameObject.SetActive(true);
        CreateDedicatedOverlayCanvas();

        StopAllCoroutines();
        StartCoroutine(EndingSequenceRoutine());
    }

    private void CreateDedicatedOverlayCanvas()
    {
        if (_dedicatedOverlayCanvas != null) return;

       
        GameObject canvasObj = new GameObject("EndingCutscene_OverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _dedicatedOverlayCanvas = canvasObj.GetComponent<Canvas>();
        _dedicatedOverlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _dedicatedOverlayCanvas.sortingOrder = 9999;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

   
        Color solidBackdrop = new Color(screenFadeColor.r, screenFadeColor.g, screenFadeColor.b, 0f);
        _backdropImage = CreateFullscreenImage(canvasObj.transform, "CutsceneBackdrop", solidBackdrop, false);
        _slideImageA = CreateFullscreenImage(canvasObj.transform, "CutsceneSlide_A", new Color(1f, 1f, 1f, 0f), true);
        _slideImageB = CreateFullscreenImage(canvasObj.transform, "CutsceneSlide_B", new Color(1f, 1f, 1f, 0f), true);
    }

    private Image CreateFullscreenImage(Transform parentTransform, string objName, Color initColor, bool preserveAspect)
    {
        GameObject child = new GameObject(objName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        child.transform.SetParent(parentTransform, false);

        RectTransform rt = child.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;

        Image img = child.GetComponent<Image>();
        img.color = initColor;
        img.preserveAspect = preserveAspect;
        img.raycastTarget = false;
        return img;
    }

    private IEnumerator EndingSequenceRoutine()
    {
        if (buildUpDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(buildUpDelay);
        }

        OnCutsceneFadeStarted?.Invoke();

    
        float t = 0f;
        while (t < initialScreenFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / Mathf.Max(0.01f, initialScreenFadeDuration));
            float smooth = p * p * (3f - 2f * p);

            if (_backdropImage != null)
            {
                _backdropImage.color = new Color(screenFadeColor.r, screenFadeColor.g, screenFadeColor.b, smooth);
            }
            yield return null;
        }

        if (_backdropImage != null)
        {
            _backdropImage.color = new Color(screenFadeColor.r, screenFadeColor.g, screenFadeColor.b, 1f);
        }

        if (slides == null || slides.Count == 0)
        {
            Debug.LogWarning("EndingCutsceneSlideshow: No slides added to the 'Slides' list in the Inspector!");
            yield break;
        }

        for (int i = 0; i < slides.Count; i++)
        {
            CutsceneSlide currentSlide = slides[i];
            Sprite resolvedSprite = currentSlide.GetResolvedSprite();

            if (resolvedSprite == null)
            {
                Debug.LogWarning($"EndingCutsceneSlideshow: Slide {i} has no Sprite assigned!");
                continue;
            }

            bool isLastSlide = (i == slides.Count - 1);

            Image activeImg = _usingImageA ? _slideImageB : _slideImageA;
            Image incomingImg = _usingImageA ? _slideImageA : _slideImageB;
            _usingImageA = !_usingImageA;

            incomingImg.transform.SetAsLastSibling();
            incomingImg.sprite = resolvedSprite;
            incomingImg.color = new Color(1f, 1f, 1f, 0f);

            float startScale = currentSlide.startScale <= 0.05f ? 1.0f : currentSlide.startScale;
            float endScale = currentSlide.endScale <= 0.05f ? 1.05f : currentSlide.endScale;
            incomingImg.rectTransform.localScale = Vector3.one * startScale;

            if (currentSlide.optionalSlideSfx != null && cutsceneAudioSource != null)
            {
                cutsceneAudioSource.PlayOneShot(currentSlide.optionalSlideSfx);
            }

            float fadeTime = Mathf.Max(0.1f, currentSlide.fadeDuration);
            float holdTime = Mathf.Max(0f, currentSlide.holdDuration);
            float totalSlideTime = isLastSlide ? fadeTime : (fadeTime + holdTime);

            t = 0f;
            float startActiveAlpha = activeImg != null ? activeImg.color.a : 0f;

            while (t < totalSlideTime)
            {
                t += Time.unscaledDeltaTime;

                float fadeProgress = Mathf.Clamp01(t / fadeTime);
                float smoothFade = fadeProgress * fadeProgress * (3f - 2f * fadeProgress);

                incomingImg.color = new Color(1f, 1f, 1f, smoothFade);
                if (activeImg != null && startActiveAlpha > 0f)
                {
                    activeImg.color = new Color(1f, 1f, 1f, Mathf.Lerp(startActiveAlpha, 0f, smoothFade));
                }

                float scaleProgress = Mathf.Clamp01(t / Mathf.Max(0.1f, fadeTime + holdTime));
                incomingImg.rectTransform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, scaleProgress);

                yield return null;
            }

            incomingImg.color = Color.white;
            if (activeImg != null) activeImg.color = new Color(1f, 1f, 1f, 0f);

            if (isLastSlide)
            {
                OnFinalSlideReached?.Invoke();
                TimeManager.ApplyFactor(this, 0f);

                float holdTimer = 0f;
                float currentScale = incomingImg.rectTransform.localScale.x;

                while (holdTimer < holdTime)
                {
                    holdTimer += Time.unscaledDeltaTime;
                    float p = Mathf.Clamp01(holdTimer / Mathf.Max(0.01f, holdTime));
                    incomingImg.rectTransform.localScale = Vector3.one * Mathf.Lerp(currentScale, endScale, p);
                    yield return null;
                }

              
                yield break;
            }
        }
    }
}