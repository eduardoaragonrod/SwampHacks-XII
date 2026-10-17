using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class DestroyOnVFXFinish : MonoBehaviour
{
    [field: SerializeField]
    public bool DestroyGameObject { get; set; } = true;

    private VisualEffect _vfx;
    // To avoid frame one destruction.
    private float _delayEndTime;

    private void Awake()
    {
        _vfx = GetComponent<VisualEffect>();
        _delayEndTime = Time.time + 0.1f;
    }

    private void LateUpdate()
    {
        if (Time.time < _delayEndTime)
        {
            return;
        }

        if (_vfx.aliveParticleCount == 0)
        {
            if (DestroyGameObject)
            {
                Destroy(gameObject);
            }
            else
            {
                Destroy(_vfx);
                Destroy(this);
            }
        }
    }
}
