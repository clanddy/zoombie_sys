using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AshenDao.Game
{
    public class BuildingController : MonoBehaviour
    {
        public string BuildingName = "";
        public Renderer Roof;
        public List<Renderer> FrontWalls = new List<Renderer>();
        public Light InteriorLight;
        public bool IsInside { get; private set; }

        private readonly List<Material> roofMaterials = new List<Material>();
        private readonly List<Material> wallMaterials = new List<Material>();
        private Coroutine fadeRoutine;
        private float roofAlpha = 1f;
        private float wallAlpha = 1f;
        private PlayerController player;
        private Collider trigger;

        public void Setup(string buildingName, Renderer roof, IEnumerable<Renderer> frontWalls, Light interiorLight)
        {
            BuildingName = buildingName;
            Roof = roof;
            FrontWalls.Clear();
            FrontWalls.AddRange(frontWalls);
            InteriorLight = interiorLight;

            CopyMaterials(Roof, roofMaterials);
            foreach (var wall in FrontWalls) CopyMaterials(wall, wallMaterials);

            if (InteriorLight != null) InteriorLight.enabled = false;
        }

        private static void CopyMaterials(Renderer renderer, List<Material> target)
        {
            if (renderer == null) return;
            foreach (var material in renderer.materials)
            {
                if (material != null) target.Add(material);
            }
        }

        private void Start()
        {
            player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            trigger = GetComponent<Collider>();
            if (player != null && trigger != null && trigger.bounds.Contains(player.transform.position))
            {
                SetInside(true);
            }
        }

        private void Update()
        {
            if (trigger == null) return;
            if (player == null) player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (player == null) return;
            var inside = trigger.bounds.Contains(player.transform.position);
            if (inside != IsInside) SetInside(inside);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            SetInside(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            SetInside(false);
        }

        public void SetInside(bool inside)
        {
            if (IsInside == inside) return;
            IsInside = inside;
            if (InteriorLight != null) InteriorLight.enabled = inside;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeRoutine(inside));
        }

        private IEnumerator FadeRoutine(bool inside)
        {
            if (!inside)
            {
                if (Roof != null) Roof.enabled = true;
                foreach (var wall in FrontWalls) if (wall != null) wall.enabled = true;
            }

            var duration = 0.24f;
            var time = 0f;
            var roofStart = roofAlpha;
            var wallStart = wallAlpha;
            var roofTarget = inside ? 0.04f : 1f;
            var wallTarget = inside ? 0.16f : 1f;

            while (time < duration)
            {
                time += Time.deltaTime;
                var t = Mathf.Clamp01(time / duration);
                roofAlpha = Mathf.Lerp(roofStart, roofTarget, t);
                wallAlpha = Mathf.Lerp(wallStart, wallTarget, t);
                Apply();
                yield return null;
            }

            roofAlpha = roofTarget;
            wallAlpha = wallTarget;
            Apply();

            if (inside)
            {
                if (Roof != null)
                {
                    Roof.shadowCastingMode = ShadowCastingMode.Off;
                    Roof.enabled = false;
                }
                foreach (var wall in FrontWalls)
                {
                    if (wall == null) continue;
                    wall.shadowCastingMode = ShadowCastingMode.Off;
                    wall.enabled = false;
                }
            }
            else
            {
                if (Roof != null) Roof.shadowCastingMode = ShadowCastingMode.On;
                foreach (var wall in FrontWalls) if (wall != null) wall.shadowCastingMode = ShadowCastingMode.On;
            }
            fadeRoutine = null;
        }

        private void Apply()
        {
            foreach (var material in roofMaterials) MaterialFactory.SetAlpha(material, roofAlpha);
            foreach (var material in wallMaterials) MaterialFactory.SetAlpha(material, wallAlpha);
        }
    }
}
