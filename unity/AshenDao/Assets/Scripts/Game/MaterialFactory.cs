using UnityEngine;
using UnityEngine.Rendering;

namespace AshenDao.Game
{
    public static class MaterialFactory
    {
        private static Shader litShader;
        private static Shader unlitShader;

        private static Shader LitShader
        {
            get
            {
                if (litShader == null)
                {
                    litShader = Shader.Find("Universal Render Pipeline/Lit");
                    if (litShader == null) litShader = Shader.Find("Standard");
                }
                return litShader;
            }
        }

        private static Shader UnlitShader
        {
            get
            {
                if (unlitShader == null)
                {
                    unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                    if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");
                    if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
                }
                return unlitShader;
            }
        }

        public static Material Lit(Color color, float smoothness = 0.05f, float metallic = 0f)
        {
            var material = new Material(LitShader) { name = "Lit_" + color };
            SetColor(material, color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            return material;
        }

        public static Material TransparentLit(Color color, float alpha = 1f)
        {
            var material = Lit(new Color(color.r, color.g, color.b, alpha));
            MakeTransparent(material, false);
            return material;
        }

        public static Material Unlit(Color color, bool additive = false)
        {
            var material = new Material(UnlitShader) { name = "Unlit_" + color };
            SetColor(material, color);
            MakeTransparent(material, additive);
            return material;
        }

        public static Material Emissive(Color color, Color emission)
        {
            var material = Lit(color);
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", emission);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return material;
        }

        public static void SetColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }

        public static Color GetColor(Material material)
        {
            if (material.HasProperty("_BaseColor")) return material.GetColor("_BaseColor");
            return material.color;
        }

        public static void SetAlpha(Material material, float alpha)
        {
            var color = GetColor(material);
            color.a = alpha;
            SetColor(material, color);
        }

        private static void MakeTransparent(Material material, bool additive)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
    }

    public static class PrimitiveFactory
    {
        public static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material,
            bool collider = true)
        {
            return Primitive(parent, name, PrimitiveType.Cube, position, size, material, collider, Quaternion.identity);
        }

        public static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material,
            Quaternion rotation, bool collider = true)
        {
            return Primitive(parent, name, PrimitiveType.Cube, position, size, material, collider, rotation);
        }

        public static GameObject Cylinder(Transform parent, string name, Vector3 position, Vector3 size,
            Material material, bool collider = true)
        {
            return Primitive(parent, name, PrimitiveType.Cylinder, position, size, material, collider, Quaternion.identity);
        }

        public static GameObject Sphere(Transform parent, string name, Vector3 position, Vector3 size, Material material,
            bool collider = true)
        {
            return Primitive(parent, name, PrimitiveType.Sphere, position, size, material, collider, Quaternion.identity);
        }

        public static GameObject Capsule(Transform parent, string name, Vector3 position, Vector3 size,
            Material material, bool collider = true)
        {
            return Primitive(parent, name, PrimitiveType.Capsule, position, size, material, collider, Quaternion.identity);
        }

        private static GameObject Primitive(Transform parent, string name, PrimitiveType type, Vector3 position,
            Vector3 size, Material material, bool collider, Quaternion rotation)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            go.transform.localScale = size;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
            }
            if (!collider)
            {
                var existing = go.GetComponent<Collider>();
                if (existing != null) Object.Destroy(existing);
            }
            return go;
        }

        public static void RemoveCollider(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
        }

        public static void SetLayer(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayer(child.gameObject, layer);
        }
    }
}
