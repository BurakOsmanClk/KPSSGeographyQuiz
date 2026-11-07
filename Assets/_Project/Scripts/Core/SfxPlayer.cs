using System.Collections.Generic;
using UnityEngine;

namespace KPSSGeography.Core
{
    public enum Sfx
    {
        Correct,
        Wrong,
        Tick,
        Go,
        Victory,
        GameOver,
        Click
    }

    /// <summary>
    /// Prototip için ses dosyası gerektirmeyen, kodla üretilmiş basit efektler.
    /// İleride gerçek ses dosyalarıyla değiştirilebilir.
    /// </summary>
    public class SfxPlayer : MonoBehaviour
    {
        const int SampleRate = 44100;

        [Range(0f, 1f)] public float volume = 0.6f;

        AudioSource source;
        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;

            clips[Sfx.Correct] = Notes("Correct", 0.5f, (880f, 0.08f), (1318.5f, 0.18f));
            clips[Sfx.Wrong] = Buzz("Wrong", 0.32f);
            clips[Sfx.Tick] = Notes("Tick", 0.35f, (1500f, 0.04f));
            clips[Sfx.Go] = Notes("Go", 0.45f, (1046.5f, 0.28f));
            clips[Sfx.Victory] = Notes("Victory", 0.45f, (523.3f, 0.12f), (659.3f, 0.12f), (784f, 0.12f), (1046.5f, 0.45f));
            clips[Sfx.GameOver] = Notes("GameOver", 0.45f, (392f, 0.2f), (329.6f, 0.2f), (261.6f, 0.5f));
            clips[Sfx.Click] = Notes("Click", 0.3f, (900f, 0.035f));
        }

        public void Play(Sfx sfx, float volumeScale = 1f)
        {
            if (source != null && clips.TryGetValue(sfx, out var clip))
                source.PlayOneShot(clip, volume * volumeScale);
        }

        static AudioClip Notes(string name, float gain, params (float freq, float dur)[] notes)
        {
            int total = 0;
            foreach (var n in notes) total += Mathf.CeilToInt(n.dur * SampleRate);
            var data = new float[total];
            int offset = 0;
            foreach (var (freq, dur) in notes)
            {
                int len = Mathf.CeilToInt(dur * SampleRate);
                for (int i = 0; i < len; i++)
                {
                    float t = (float)i / SampleRate;
                    float env = Mathf.Min(1f, t / 0.005f) * Mathf.Exp(-t * 6f / dur);
                    float s = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.25f * Mathf.Sin(4f * Mathf.PI * freq * t);
                    data[offset + i] = s * env * gain * 0.8f;
                }
                offset += len;
            }
            return Create(name, data);
        }

        static AudioClip Buzz(string name, float dur)
        {
            int len = Mathf.CeilToInt(dur * SampleRate);
            var data = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Min(1f, t / 0.005f) * Mathf.Exp(-t * 7f);
                float a = Mathf.Sin(2f * Mathf.PI * 160f * t);
                float b = Mathf.Sin(2f * Mathf.PI * 171f * t);
                float s = Mathf.Clamp((a + b) * 1.6f, -1f, 1f);
                data[i] = s * env * 0.35f;
            }
            return Create(name, data);
        }

        static AudioClip Create(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
