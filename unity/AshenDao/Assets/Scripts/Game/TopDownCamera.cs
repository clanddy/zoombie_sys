using UnityEngine;

namespace AshenDao.Game
{
    public class TopDownCamera : MonoBehaviour
    {
        public Transform Target;
        public Vector3 Offset = new Vector3(0f, 15f, -11f);
        public float OrthographicSize = 8.5f;
        public float FollowSharpness = 7f;

        private Camera cam;
        private Vector3 velocity;
        private float targetSize;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = gameObject.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = OrthographicSize;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 120f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.035f, 0.04f);
            targetSize = OrthographicSize;
        }

        public void Setup(Transform target)
        {
            Target = target;
            if (cam == null) cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = OrthographicSize;
            Snap();
        }

        public void Snap()
        {
            if (Target == null) return;
            transform.position = Target.position + Offset;
            transform.rotation = Quaternion.LookRotation(Target.position + Vector3.up * 0.9f - transform.position);
        }

        private void LateUpdate()
        {
            if (Target == null) return;
            var desired = Target.position + Offset;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, 1f / FollowSharpness);
            transform.rotation = Quaternion.LookRotation(Target.position + Vector3.up * 0.9f - transform.position);

            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f) targetSize = Mathf.Clamp(targetSize - scroll * 0.8f, 5.5f, 12f);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, Time.deltaTime * 8f);
        }
    }

    public class VisionController : MonoBehaviour
    {
        public float Range = 14f;
        public float FieldOfView = 78f;
        public int RayCount = 46;

        private PlayerController player;
        private Mesh coneMesh;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Light spot;
        private readonly Vector3[] vertices = new Vector3[256];

        public Vector3 AimDirection => player != null ? player.AimDirection : transform.forward;

        public void Setup(PlayerController owner)
        {
            player = owner;
            BuildCone();
            BuildLight();
        }

        private void BuildCone()
        {
            var go = new GameObject("VisionCone");
            go.transform.SetParent(transform, false);
            meshFilter = go.AddComponent<MeshFilter>();
            meshRenderer = go.AddComponent<MeshRenderer>();
            coneMesh = new Mesh { name = "VisionConeMesh" };
            coneMesh.MarkDynamic();
            meshFilter.sharedMesh = coneMesh;
            var material = MaterialFactory.Unlit(new Color(0.32f, 1f, 0.78f, 0.10f), true);
            material.SetFloat("_Surface", 1f);
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        private void BuildLight()
        {
            var go = new GameObject("VisionLight");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 1.35f, 0.25f);
            spot = go.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.range = Range;
            spot.spotAngle = FieldOfView;
            spot.innerSpotAngle = FieldOfView * 0.45f;
            spot.intensity = 0f;
            spot.color = new Color(1f, 0.94f, 0.82f);
            spot.shadows = LightShadows.Soft;
            spot.shadowStrength = 0.65f;
        }

        public void SetLightIntensity(float value)
        {
            if (spot != null) spot.intensity = value;
        }

        private void LateUpdate()
        {
            if (player == null) return;
            if (spot != null)
            {
                spot.transform.rotation = Quaternion.LookRotation(player.AimDirection);
            }
            UpdateCone();
        }

        private void UpdateCone()
        {
            if (coneMesh == null) return;
            var origin = transform.position + Vector3.up * 0.12f;
            var aim = player.AimDirection;
            var half = FieldOfView * 0.5f;

            var count = 0;
            vertices[count++] = transform.InverseTransformPoint(origin);
            var rayMask = ~(1 << 2);
            for (var i = 0; i <= RayCount; i++)
            {
                var t = i / (float)RayCount;
                var angle = Mathf.Lerp(-half, half, t);
                var dir = Quaternion.Euler(0f, angle, 0f) * aim;
                var distance = Range;
                if (Physics.Raycast(origin + Vector3.up * 0.6f, dir, out var hit, Range, rayMask,
                        QueryTriggerInteraction.Ignore))
                {
                    distance = Mathf.Max(0.4f, hit.distance);
                }
                vertices[count++] = transform.InverseTransformPoint(origin + dir * distance);
            }

            var triangles = new int[(count - 2) * 3];
            for (var i = 0; i < count - 2; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            coneMesh.Clear();
            coneMesh.vertices = vertices;
            coneMesh.triangles = triangles;
            coneMesh.RecalculateNormals();
            coneMesh.RecalculateBounds();
        }

        public bool CanSee(Vector3 worldPoint)
        {
            var eye = transform.position + Vector3.up * 1.55f;
            var to = worldPoint - eye;
            if (to.magnitude > Range) return false;
            var flat = new Vector3(to.x, 0f, to.z);
            if (flat.magnitude > 0.01f && Vector3.Angle(AimDirection, flat) > FieldOfView * 0.5f) return false;
            return !Physics.Linecast(eye, worldPoint, out var hit, ~0, QueryTriggerInteraction.Ignore) ||
                   hit.collider.GetComponentInParent<PlayerController>() != null;
        }
    }

    public static class SpellVfx
    {
        public static void Slash(Vector3 origin, Vector3 forward, float radius, float arc, Color color)
        {
            var go = new GameObject("SpellSlash");
            go.transform.position = origin;
            var mesh = new Mesh { name = "SlashArc" };
            const int segments = 18;
            var verts = new Vector3[segments + 1];
            var tris = new int[segments * 3];
            for (var i = 0; i <= segments; i++)
            {
                var angle = Mathf.Lerp(-arc * 0.5f, arc * 0.5f, i / (float)segments);
                var dir = Quaternion.Euler(0f, angle, 0f) * forward;
                verts[i] = dir * radius + Vector3.up * 0.08f;
            }
            for (var i = 0; i < segments; i++)
            {
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i + 2;
            }
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            var material = MaterialFactory.Unlit(color, true);
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<FadeFx>();
        }

        public static void Tracer(Vector3 from, Vector3 to, Color color)
        {
            var go = new GameObject("Tracer");
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.startWidth = 0.035f;
            line.endWidth = 0.012f;
            line.sharedMaterial = MaterialFactory.Unlit(color, true);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var fade = go.AddComponent<FadeFx>();
            fade.Life = 0.12f;
        }
    }

    public class FadeFx : MonoBehaviour
    {
        public float Life = 0.35f;
        private float age;
        private Renderer rend;
        private Material material;

        private void Awake()
        {
            rend = GetComponent<Renderer>();
            if (rend != null) material = rend.material;
        }

        private void Update()
        {
            age += Time.deltaTime;
            var t = Mathf.Clamp01(age / Life);
            if (material != null) MaterialFactory.SetAlpha(material, Mathf.Lerp(0.6f, 0f, t));
            transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.15f, t);
            if (age >= Life) Destroy(gameObject);
        }
    }
}
