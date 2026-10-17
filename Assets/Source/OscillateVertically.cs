using UnityEngine;

public class OscillateVertically : MonoBehaviour
{
    [SerializeField]
    private float Frequency = 1f, Amplitude = 1f;

    private float _init;

    private void Awake()
    {
        _init = transform.localPosition.y;
    }

    private void Update()
    {
        var pos = transform.localPosition;
        pos.y = _init + (Amplitude * Mathf.Sin(Frequency * Time.time));
        transform.localPosition = pos;
    }
}
