using UnityEngine;
using UnityEngine.Audio;

/// <summary>Bounded one-shot voices independent of projectile lifetime. Never steals an active voice.</summary>
public sealed class SpellAudioPool : MonoBehaviour
{
    const int VoiceCount = 12;
    static SpellAudioPool instance;
    readonly AudioSource[] voices = new AudioSource[VoiceCount];
    readonly float[] nextAllowed = new float[3];
    readonly float[] stopAt = new float[VoiceCount];
    readonly float[] fadeDuration = new float[VoiceCount];
    readonly float[] startVolume = new float[VoiceCount];
    readonly int[] groups = new int[VoiceCount];
    static int groupSequence;
    CharacterPanelUI panel;
    bool paused;

    void Awake()
    {
        instance = this;
        panel = Object.FindAnyObjectByType<CharacterPanelUI>();
        for (int i = 0; i < voices.Length; i++)
        {
            var holder = new GameObject("Spell voice " + i);
            holder.transform.SetParent(transform);
            var source = holder.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.dopplerLevel = 0f;
            source.minDistance = 4f;
            source.maxDistance = 35f;
            source.priority = 160;
            voices[i] = source;
        }
    }

    void Update()
    {
        bool shouldPause = Time.timeScale <= 0f || (panel && panel.panel && panel.panel.activeInHierarchy);
        if (shouldPause != paused)
        {
            paused = shouldPause;
            foreach (var source in voices)
                if (paused) source.Pause(); else source.UnPause();
        }
        if (paused) return;
        AdvanceVoices(Time.deltaTime);
    }

    void AdvanceVoices(float deltaTime)
    {
        for (int i = 0; i < voices.Length; i++)
        {
            if (!voices[i].isPlaying || stopAt[i] <= 0f) continue;
            stopAt[i] -= deltaTime;
            if (stopAt[i] <= 0f) { voices[i].Stop(); groups[i] = 0; continue; }
            voices[i].volume = startVolume[i] * Mathf.Clamp01(stopAt[i] / fadeDuration[i]);
        }
    }

    public static int NewGroup() => ++groupSequence;

    public static void FadeGroup(int group, float seconds = .025f)
    {
        if (!instance || group == 0) return;
        for (int i = 0; i < 4; i++)
        {
            if (instance.groups[i] != group || !instance.voices[i].isPlaying) continue;
            float duration = Mathf.Max(.005f, seconds);
            instance.startVolume[i] = instance.voices[i].volume;
            instance.fadeDuration[i] = duration;
            instance.stopAt[i] = instance.stopAt[i] > 0f ? Mathf.Min(instance.stopAt[i], duration) : duration;
        }
    }

    public static bool Play(AudioClip clip, Vector3 position, float volume, float spatialBlend,
        int channel, float minimumInterval, AudioMixerGroup mixer = null, int group = 0, float maxDuration = 0f)
    {
        if (!clip || volume <= 0f) return false;
        if (!instance) new GameObject("Spell audio pool").AddComponent<SpellAudioPool>();
        channel = Mathf.Clamp(channel, 0, 2);
        if (instance.paused || Time.time < instance.nextAllowed[channel]) return false;
        int first = channel == 0 ? 0 : channel == 1 ? 4 : 10;
        int end = channel == 0 ? 4 : channel == 1 ? 10 : VoiceCount;
        for (int i = first; i < end; i++)
        {
            var source = instance.voices[i];
            if (source.isPlaying) continue;
            source.transform.position = position;
            source.outputAudioMixerGroup = mixer;
            source.clip = clip;
            // Headroom when several impacts overlap.
            source.volume = Mathf.Clamp01(volume) * .7f;
            source.spatialBlend = Mathf.Clamp01(spatialBlend);
            instance.groups[i] = group;
            instance.stopAt[i] = Mathf.Max(0f, maxDuration);
            instance.fadeDuration[i] = .035f;
            instance.startVolume[i] = source.volume;
            source.Play();
            instance.nextAllowed[channel] = Time.time + Mathf.Max(0f, minimumInterval);
            return true;
        }
        return false;
    }

    void OnDestroy() { if (instance == this) instance = null; }
}
