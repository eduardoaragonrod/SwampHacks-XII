using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.VFX;

[RequireComponent(typeof(SpriteRenderer))]
public class LightningStrike : MonoBehaviour
{
    [SerializeField]
    private Animator Animator;
    [SerializeField]
    private Light2D Light;
    [SerializeField]
    private SpriteRenderer Crack;
    [SerializeField]
    private VisualEffect VFX;

    [SerializeField]
    private float DelayBeforeImpact, DelayBeforeGone, FadeOutTime;
    [SerializeField]
    private bool StrikeOnPlay;

    private bool _isStriking;

    private void Awake()
    {
        GetComponent<SpriteRenderer>().sprite = null;
        Crack.gameObject.SetActive(false);
        Light.enabled = false;

        if (StrikeOnPlay)
        {
            Strike();
        }
    }

    public async void Strike(Color? color = null)
    {
        if (_isStriking)
        {
            return;
        }

        _isStriking = true;

        if (color.HasValue)
        {
            Light.color = color.Value;
            Crack.color = color.Value;
            VFX.SetVector4("Color", color.Value);
        }

        var rots = new float[]
        {
                0f, 90f, 180f, -90f
        };
        Crack.transform.rotation = Quaternion.AngleAxis(rots[Random.Range(0, 4)], Vector3.forward);

        Animator.enabled = true;
        await Awaitable.WaitForSecondsAsync(DelayBeforeImpact, destroyCancellationToken);

        Light.enabled = true;
        Crack.gameObject.SetActive(true);
        VFX.Play();

        await Awaitable.WaitForSecondsAsync(DelayBeforeGone - DelayBeforeImpact, destroyCancellationToken);

        DOTween.To(() => Light.intensity, x => Light.intensity = x, 0f, FadeOutTime);
        VFX.SendEvent("OnEnd");

        Crack.GetComponent<Animator>().speed = 0f;
        await Crack.DOFade(0f, FadeOutTime).AsyncWaitForCompletion();

        VFX.transform.SetParent(null, true);
        VFX.gameObject.AddComponent<DestroyOnVFXFinish>();

        Destroy(gameObject);
    }
}
