using System.Collections.Generic;
using UnityEngine;

namespace PlanB
{
    /// <summary>Small synthesized clips, generated once and played through one persistent source.</summary>
    public static class AudioFactory
    {
        private const int SampleRate = 22050;
        private static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();
        private static AudioSource source;

        public static void Play(string name, float volume = 1f)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (source == null)
            {
                GameObject audio = new GameObject("PlanB Procedural Audio");
                Object.DontDestroyOnLoad(audio);
                source = audio.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.volume = 0.5f;
            }
            AudioClip clip;
            name = name.ToLowerInvariant();
            if (!Clips.TryGetValue(name, out clip) || clip == null)
            {
                clip = Create(name);
                Clips[name] = clip;
            }
            source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private static AudioClip Create(string name)
        {
            float length = 0.2f;
            switch (name)
            {
                case "shoot": length = 0.14f; break;
                case "reload": length = 0.48f; break;
                case "pickup": length = 0.25f; break;
                case "hurt": case "death": length = 0.32f; break;
                case "elevator": length = 1.2f; break;
                case "generator": length = 0.9f; break;
                case "win": length = 1.5f; break;
                case "alarm": length = 0.8f; break;
            }
            int count = Mathf.CeilToInt(length * SampleRate);
            float[] samples = new float[count];
            uint random = 0x4B2A61u;
            double phase = 0;
            float lowNoise = 0;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float p = t / length;
                random ^= random << 13; random ^= random >> 17; random ^= random << 5;
                float noise = ((random & 65535u) / 32767.5f) - 1f;
                lowNoise = Mathf.Lerp(lowNoise, noise, 0.12f);
                float sample;
                switch (name)
                {
                    case "shoot":
                        sample = (noise * Mathf.Exp(-t * 55f) * 0.8f + Mathf.Sin(t * (1800f - t * 6500f)) * Mathf.Exp(-t * 38f) * 0.5f);
                        break;
                    case "hit": case "bash": case "melee":
                        sample = (lowNoise * 0.8f + Mathf.Sin(t * (650f - t * 1300f)) * 0.45f) * Mathf.Exp(-t * 24f);
                        break;
                    case "hurt": case "death":
                        sample = (Mathf.Sin(t * (720f - t * 1400f)) * 0.45f + lowNoise * 0.6f) * Mathf.Exp(-t * 11f);
                        break;
                    case "reload":
                        sample = noise * (Pulse(t, 0.02f, 0.018f) * 0.55f + Pulse(t, 0.22f, 0.015f) * 0.8f + Pulse(t, 0.36f, 0.024f) * 0.7f);
                        sample += Mathf.Sin(t * 1900f) * Pulse(t, 0.37f, 0.03f) * 0.25f;
                        break;
                    case "pickup": case "select": case "confirm":
                        phase += (p < 0.4f ? 659.25 : 987.77) * 2.0 * System.Math.PI / SampleRate;
                        sample = (float)System.Math.Sin(phase) * 0.35f * Mathf.Sin(p * Mathf.PI) * (1f - p);
                        break;
                    case "elevator":
                        sample = Mathf.Sin(t * 110f * Mathf.PI * 2f) * 0.12f * Mathf.Sin(p * Mathf.PI) + lowNoise * 0.3f * (1f - p);
                        if (p > 0.65f) sample += Mathf.Sin(t * 1046.5f * Mathf.PI * 2f) * 0.3f * Mathf.Exp(-(p - 0.65f) * 12f);
                        break;
                    case "generator":
                        phase += (40.0 + p * 45.0) * 2.0 * System.Math.PI / SampleRate;
                        sample = ((float)System.Math.Sin(phase) * 0.18f + lowNoise * 0.8f) * Mathf.Sin(p * Mathf.PI);
                        sample += noise * Pulse(t, 0.1f, 0.01f) * 0.6f;
                        break;
                    case "win":
                        int note = Mathf.Min(3, Mathf.FloorToInt(t / 0.28f));
                        float frequency = note == 0 ? 523.25f : note == 1 ? 659.25f : note == 2 ? 783.99f : 1046.5f;
                        phase += frequency * 2.0 * System.Math.PI / SampleRate;
                        sample = (float)System.Math.Sin(phase) * 0.28f * Mathf.Exp(-(t - note * 0.28f) * 5f) * Mathf.Clamp01((length - t) * 6f);
                        break;
                    case "alarm":
                        phase += (600.0 + System.Math.Sin(t * 14f) * 180.0) * 2.0 * System.Math.PI / SampleRate;
                        sample = (float)System.Math.Sin(phase) * 0.22f * Mathf.Sin(p * Mathf.PI);
                        break;
                    default:
                        sample = Mathf.Sin(t * 1200f) * 0.25f * Mathf.Exp(-t * 20f);
                        break;
                }
                // Click-free fades also constrain the intentionally noisy percussion peaks.
                float fade = Mathf.Min(1f, t * 1000f) * Mathf.Clamp01((length - t) * 160f);
                samples[i] = Mathf.Clamp(sample * fade, -0.95f, 0.95f);
            }
            AudioClip clip = AudioClip.Create("PlanB " + name, count, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float Pulse(float t, float start, float width)
        {
            if (t < start || t > start + width * 5f) return 0f;
            return Mathf.Exp(-(t - start) / width);
        }
    }
}
