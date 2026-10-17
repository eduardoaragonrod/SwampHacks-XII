using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DestroyWhenAudioFinished : MonoBehaviour
{
    private AudioSource _source;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
    }

    private void LateUpdate()
    {
        if (!_source.isPlaying)
        {
            Destroy(gameObject);
        }
    }
}