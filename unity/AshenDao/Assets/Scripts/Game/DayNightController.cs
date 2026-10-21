using UnityEngine;
using AshenDao.Sim;

namespace AshenDao.Game
{
    public class DayNightController : MonoBehaviour
    {
        public Light Sun;
        public VisionController Vision;
        public WorldSim Sim;
        public WorldBuilder World;

        public void Setup(WorldSim sim, Light sun, VisionController vision, WorldBuilder world)
        {
            Sim = sim;
            Sun = sun;
            Vision = vision;
            World = world;
            if (Sun != null)
            {
                Sun.type = LightType.Directional;
                Sun.shadows = LightShadows.Soft;
                Sun.shadowStrength = 0.75f;
                Sun.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
        }

        private void Update()
        {
            if (Sim == null) return;
            var dark = Sim.Clock.Darkness;
            var day = 1f - Mathf.Clamp01(dark / 0.72f);

            if (Sun != null)
            {
                Sun.intensity = Mathf.Lerp(0.04f, 1.15f, day);
                Sun.color = Color.Lerp(new Color(0.42f, 0.55f, 0.8f), new Color(1f, 0.94f, 0.8f), day);
            }

            RenderSettings.ambientLight = Color.Lerp(new Color(0.035f, 0.05f, 0.07f),
                new Color(0.42f, 0.46f, 0.44f), day);
            RenderSettings.fogColor = Color.Lerp(new Color(0.02f, 0.035f, 0.05f),
                new Color(0.35f, 0.4f, 0.38f), day);
            RenderSettings.fogDensity = Mathf.Lerp(0.028f, 0.0035f, day);

            if (Vision != null) Vision.SetLightIntensity(Mathf.Lerp(4.2f, 0.15f, day));
            if (World != null)
            {
                foreach (var lamp in World.StreetLights)
                {
                    if (lamp != null && lamp.Light != null) lamp.Light.intensity = dark * lamp.NightIntensity;
                }
            }
        }
    }

    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (Object.FindAnyObjectByType<GameRoot>() != null) return;
            var go = new GameObject("GameRoot");
            go.AddComponent<GameRoot>();
        }
    }
}
