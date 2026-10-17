using UnityEngine;
using UnityEngine.Audio;

public static class Audio
{
    /// <summary>
    /// Fire and forget sound. Play it. It auto destroys itself when it's done. Doesn't support looping audio.
    /// </summary>
    public static void Play(AudioResource audio, Vector2 position)
    {
        if (audio == null) return;

        var instance = new GameObject("Audio Clip");
        instance.transform.position = position;
        var source = instance.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.resource = audio;
        source.Play();
        
        instance.AddComponent<DestroyWhenAudioFinished>();
    }
    /// <inheritdoc cref="Audio.Play(AudioResource, Vector2)"/>
    public static void Play(AudioResource audio, Transform parent)
    {
        if (audio == null) return;

        var instance = new GameObject("Audio Clip");
        instance.transform.SetParent(parent, false);
        var source = instance.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.resource = audio;
        source.Play();

        instance.AddComponent<DestroyWhenAudioFinished>();
    }

    public static AudioSource Create(AudioResource audio, Vector2 position)
    {
        var instance = new GameObject("Audio Clip");
        instance.transform.position = position;
        var source = instance.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.resource = audio;

        return source;
    }
    public static AudioSource Create(AudioResource audio, Transform parent)
    {
        var instance = new GameObject("Audio Clip");
        instance.transform.SetParent(parent, false);
        var source = instance.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.resource = audio;

        return source;
    }
}
