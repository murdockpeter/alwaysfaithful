using System;
using System.Collections.Generic;
using UnityEngine;

namespace AlwaysFaithful.Prototype
{
    public enum TacticalSound
    {
        Select,
        MenuOpen,
        Inspect,
        OrderConfirm,
        OrderCancel,
        FireHit,
        FireSuppressed,
        FireMiss,
        Rally,
        EndTurn,
        MapTransition,
        ContactDetected,
        ContactLost
    }

    public sealed class TacticalAudio : MonoBehaviour
    {
        private const int SampleRate = 44100;

        private AudioSource source;
        private readonly Dictionary<TacticalSound, AudioClip> clips = new Dictionary<TacticalSound, AudioClip>();

        public void Initialize()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            var random = new System.Random(12345);

            clips[TacticalSound.Select] = CreateClip("sfx_select",
                SineSegment(880f, .05f, .22f, 0f));
            clips[TacticalSound.MenuOpen] = CreateClip("sfx_menu_open",
                SineSegment(660f, .04f, .18f, .05f), SineSegment(880f, .05f, .20f, 0f));
            clips[TacticalSound.Inspect] = CreateClip("sfx_inspect",
                SineSegment(740f, .05f, .16f, 0f));
            clips[TacticalSound.OrderConfirm] = CreateClip("sfx_order_confirm",
                SineSegment(990f, .07f, .24f, 0f));
            clips[TacticalSound.OrderCancel] = CreateClip("sfx_order_cancel",
                SineSegment(220f, .09f, .20f, 0f));
            clips[TacticalSound.FireHit] = CreateClip("sfx_fire_hit",
                Mix(NoiseSegment(.14f, .30f, 0f, random), SineSegment(110f, .16f, .26f, 0f)));
            clips[TacticalSound.FireSuppressed] = CreateClip("sfx_fire_suppressed",
                SineSegment(330f, .09f, .18f, .10f), SineSegment(300f, .09f, .18f, .10f), SineSegment(330f, .09f, .16f, 0f));
            clips[TacticalSound.FireMiss] = CreateClip("sfx_fire_miss",
                NoiseSegment(.05f, .12f, 0f, random));
            clips[TacticalSound.Rally] = CreateClip("sfx_rally",
                SineSegment(523f, .08f, .18f, .12f), SineSegment(659f, .10f, .20f, 0f));
            clips[TacticalSound.EndTurn] = CreateClip("sfx_end_turn",
                SineSegment(440f, .10f, .20f, .12f), SineSegment(330f, .12f, .18f, 0f));
            clips[TacticalSound.MapTransition] = CreateClip("sfx_map_transition",
                ChirpSegment(220f, 520f, .22f, .16f, 0f));
            clips[TacticalSound.ContactDetected] = CreateClip("sfx_contact_detected",
                SineSegment(1200f, .05f, .14f, 0f));
            clips[TacticalSound.ContactLost] = CreateClip("sfx_contact_lost",
                ChirpSegment(800f, 380f, .07f, .14f, 0f));
        }

        public void Play(TacticalSound sound, float volume = 1f)
        {
            if (source == null) return;
            if (clips.TryGetValue(sound, out AudioClip clip) && clip != null) source.PlayOneShot(clip, volume);
        }

        private static float[] SineSegment(float frequency, float duration, float startAmplitude, float endAmplitude)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            var samples = new float[count];
            for (int index = 0; index < count; index++)
            {
                float time = index / (float)SampleRate;
                float amplitude = Mathf.Lerp(startAmplitude, endAmplitude, index / (float)Mathf.Max(1, count - 1));
                samples[index] = Mathf.Sin(2f * Mathf.PI * frequency * time) * amplitude;
            }
            return samples;
        }

        private static float[] ChirpSegment(float startFrequency, float endFrequency, float duration, float startAmplitude, float endAmplitude)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            var samples = new float[count];
            double phase = 0d;
            for (int index = 0; index < count; index++)
            {
                float progress = index / (float)Mathf.Max(1, count - 1);
                float frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
                phase += 2d * Math.PI * frequency / SampleRate;
                float amplitude = Mathf.Lerp(startAmplitude, endAmplitude, progress);
                samples[index] = (float)Math.Sin(phase) * amplitude;
            }
            return samples;
        }

        private static float[] NoiseSegment(float duration, float startAmplitude, float endAmplitude, System.Random random)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            var samples = new float[count];
            for (int index = 0; index < count; index++)
            {
                float amplitude = Mathf.Lerp(startAmplitude, endAmplitude, index / (float)Mathf.Max(1, count - 1));
                samples[index] = ((float)random.NextDouble() * 2f - 1f) * amplitude;
            }
            return samples;
        }

        private static float[] Mix(float[] first, float[] second)
        {
            int length = Mathf.Max(first.Length, second.Length);
            var result = new float[length];
            for (int index = 0; index < length; index++)
            {
                float value = (index < first.Length ? first[index] : 0f) + (index < second.Length ? second[index] : 0f);
                result[index] = Mathf.Clamp(value, -1f, 1f);
            }
            return result;
        }

        private static AudioClip CreateClip(string name, params float[][] segments)
        {
            int total = 0;
            foreach (float[] segment in segments) total += segment.Length;
            var buffer = new float[Mathf.Max(1, total)];
            int offset = 0;
            foreach (float[] segment in segments)
            {
                segment.CopyTo(buffer, offset);
                offset += segment.Length;
            }
            AudioClip clip = AudioClip.Create(name, buffer.Length, 1, SampleRate, false);
            clip.SetData(buffer, 0);
            return clip;
        }
    }
}
