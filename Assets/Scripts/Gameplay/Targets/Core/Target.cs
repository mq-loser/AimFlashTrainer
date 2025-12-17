using UnityEngine;

public class Target : MonoBehaviour
{
    [Header("Physics")]
    public bool makeCollidersTriggers = true;

    [Header("Audio")]
    public bool playHitSound = true;
    [Range(0f, 1f)]
    public float hitVolume = 0.4f;
    public AudioClip hitClipOverride;

    static AudioClip defaultHitClip;
    static readonly System.Random hitClipRng = new System.Random(1337);

    void Awake()
    {
        if (!makeCollidersTriggers) return;

        // Targets shouldn't physically push the player around in an aim trainer.
        foreach (var col in GetComponentsInChildren<Collider>())
        {
            col.isTrigger = true;
        }
    }

    public void Hit()
    {
        TryPlayHitSound();

        // 先简单：被击中就销毁自己，后续可以在这里加特效、计分等
        Destroy(gameObject);
    }

    void TryPlayHitSound()
    {
        if (!playHitSound)
        {
            return;
        }

        AudioClip clip = hitClipOverride != null ? hitClipOverride : GetDefaultHitClip();
        if (clip == null)
        {
            return;
        }

        Vector3 position = transform.position;
        if (Camera.main != null)
        {
            position = Camera.main.transform.position;
        }

        AudioSource.PlayClipAtPoint(clip, position, hitVolume);
    }

    static AudioClip GetDefaultHitClip()
    {
        if (defaultHitClip != null)
        {
            return defaultHitClip;
        }

        int sampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 44100;
        const float durationSeconds = 0.045f;
        int samples = Mathf.Max(1, Mathf.CeilToInt(sampleRate * durationSeconds));

        var clip = AudioClip.Create("Hit_Default", samples, 1, sampleRate, false);
        var data = new float[samples];

        const float frequencyHz = 900f;
        const float decay = 70f;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float env = Mathf.Exp(-t * decay);
            float sine = Mathf.Sin(2f * Mathf.PI * frequencyHz * t);
            float noise = (float)(hitClipRng.NextDouble() * 2.0 - 1.0);

            data[i] = (sine * 0.65f + noise * 0.35f) * env * 0.5f;
        }

        clip.SetData(data, 0);
        defaultHitClip = clip;
        return defaultHitClip;
    }
}
