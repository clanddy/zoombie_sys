using System;
using System.Collections.Generic;

namespace AshenDao.Sim
{
    [Serializable]
    public class NoiseEvent
    {
        public int Id;
        public string Source;
        public float X;
        public float Z;
        public float Radius;
        public float Loudness = 1f;
        public float AtMinutes;

        public bool Hears(float x, float z)
        {
            var dx = x - X;
            var dz = z - Z;
            return dx * dx + dz * dz <= Radius * Radius;
        }
    }

    /// <summary>
    /// Central noise bus. Gameplay never calls zombie AI directly: it emits a noise,
    /// then every listener (zombie, NPC, future animals) decides based on distance/LOS.
    /// </summary>
    public class NoiseSystem
    {
        public readonly List<NoiseEvent> Recent = new List<NoiseEvent>();
        public event Action<NoiseEvent> Emitted;
        private int nextId = 1;

        public NoiseEvent Emit(string source, float x, float z, float radius, float loudness, float minute)
        {
            var evt = new NoiseEvent
            {
                Id = nextId++,
                Source = source,
                X = x,
                Z = z,
                Radius = Math.Max(0f, radius),
                Loudness = loudness,
                AtMinutes = minute
            };
            Recent.Add(evt);
            if (Recent.Count > 40) Recent.RemoveAt(0);
            Emitted?.Invoke(evt);
            return evt;
        }

        public void Clear()
        {
            Recent.Clear();
        }
    }
}
