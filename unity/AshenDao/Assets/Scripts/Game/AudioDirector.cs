using System.Collections.Generic;
using UnityEngine;
using AshenDao.Sim;

namespace AshenDao.Game
{
    /// <summary>
    /// Procedural placeholder audio. It reacts to the same NoiseEvent stream as AI,
    /// so replacing these clips with real assets does not change gameplay.
    /// </summary>
    public class AudioDirector : MonoBehaviour
    {
        private WorldSim sim;
        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

        public void Setup(WorldSim world)
        {
            sim = world;
            clips["gunshot"] = MakeNoise("gunshot", 0.18f, 90f, 0.55f, 0.9f);
            clips["melee"] = MakeNoise("melee", 0.12f, 160f, 0.35f, 0.55f);
            clips["footstep"] = MakeNoise("footstep", 0.08f, 80f, 0.5f, 0.18f);
            clips["bite"] = MakeNoise("bite", 0.18f, 120f, 0.4f, 0.5f);
            clips["pickup"] = MakeTone("pickup", 0.12f, 720f, 0.25f);
            clips["ui"] = MakeTone("ui", 0.06f, 1200f, 0.12f);
            clips["zombie"] = MakeNoise("zombie", 0.35f, 70f, 0.25f, 0.35f);
            if (sim != null) sim.Noise.Emitted += OnNoise;
        }

        private void OnDestroy()
        {
            if (sim != null) sim.Noise.Emitted -= OnNoise;
        }

        private void OnNoise(NoiseEvent evt)
        {
            if (evt == null) return;
            var key = evt.Source;
            if (!clips.TryGetValue(key, out var clip))
            {
                if (evt.Source.Contains("gun")) key = "gunshot";
                else if (evt.Source.Contains("hit") || evt.Source.Contains("melee")) key = "melee";
                else if (evt.Source.Contains("step")) key = "footstep";
                else if (evt.Source.Contains("bite")) key = "bite";
                else if (evt.Source.Contains("pick")) key = "pickup";
                else key = "zombie";
                if (!clips.TryGetValue(key, out clip)) return;
            }
            var volume = Mathf.Clamp01(0.2f + evt.Loudness * 0.7f);
            AudioSource.PlayClipAtPoint(clip, new Vector3(evt.X, 1.2f, evt.Z), volume);
        }

        public void PlayUi()
        {
            if (clips.TryGetValue("ui", out var clip)) AudioSource.PlayClipAtPoint(clip, Camera.main != null
                ? Camera.main.transform.position : Vector3.zero, 0.2f);
        }

        private static AudioClip MakeNoise(string name, float duration, float frequency, float noise, float volume)
        {
            const int rate = 22050;
            var samples = Mathf.Max(1, (int)(duration * rate));
            var data = new float[samples];
            var random = new System.Random(name.GetHashCode());
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)rate;
                var envelope = Mathf.Exp(-t * 18f);
                var tone = Mathf.Sin(2f * Mathf.PI * frequency * t);
                var n = ((float)random.NextDouble() * 2f - 1f) * noise;
                data[i] = Mathf.Clamp((tone * (1f - noise) + n) * envelope * volume, -1f, 1f);
            }
            var clip = AudioClip.Create(name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip MakeTone(string name, float duration, float frequency, float volume)
        {
            const int rate = 22050;
            var samples = Mathf.Max(1, (int)(duration * rate));
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)rate;
                var envelope = Mathf.Exp(-t * 26f);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume;
            }
            var clip = AudioClip.Create(name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
