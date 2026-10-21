using System.Collections.Generic;
using UnityEngine;
using AshenDao.Sim;

namespace AshenDao.Game
{
    public class StreetLight : MonoBehaviour
    {
        public Light Light;
        public float NightIntensity = 1.2f;
    }

    public class BuildingInfo
    {
        public string Name;
        public Vector2 Center;
        public Vector2 Size;
    }

    public class WorldBuilder
    {
        public readonly List<BuildingController> Buildings = new List<BuildingController>();
        public readonly List<ZombieAI> Zombies = new List<ZombieAI>();
        public readonly List<NpcController> Npcs = new List<NpcController>();
        public readonly List<BuildingInfo> MapBuildings = new List<BuildingInfo>();
        public readonly List<StreetLight> StreetLights = new List<StreetLight>();
        public SpiritVein Vein;
        public Vector3 PlayerSpawn = new Vector3(10f, 0.2f, 8f);

        public Transform Root => root;

        private Transform root;
        private Material grass;
        private Material asphalt;
        private Material sidewalk;
        private Material wood;
        private Material metal;
        private Material wall;
        private Material roof;
        private Material glass;

        public void Build(PlayerController player)
        {
            var town = new GameObject("Town").transform;
            root = town;

            grass = MaterialFactory.Lit(new Color(0.16f, 0.23f, 0.14f), 0.02f);
            asphalt = MaterialFactory.Lit(new Color(0.14f, 0.15f, 0.16f), 0.12f);
            sidewalk = MaterialFactory.Lit(new Color(0.33f, 0.32f, 0.29f), 0.08f);
            wood = MaterialFactory.Lit(new Color(0.28f, 0.2f, 0.13f), 0.05f);
            metal = MaterialFactory.Lit(new Color(0.3f, 0.31f, 0.32f), 0.5f, 0.6f);
            wall = MaterialFactory.Lit(new Color(0.38f, 0.35f, 0.29f), 0.05f);
            roof = MaterialFactory.TransparentLit(new Color(0.12f, 0.14f, 0.13f));
            glass = MaterialFactory.Emissive(new Color(0.12f, 0.16f, 0.18f), new Color(0.25f, 0.45f, 0.5f));

            BuildGround();
            BuildRoads();
            BuildTown(player);
            BuildSpiritVein();
            BuildTrees();
            BuildZombies(player);
            BuildNpcs(player);
        }

        private void BuildGround()
        {
            PrimitiveFactory.Box(root, "Ground", new Vector3(0f, -0.1f, 0f), new Vector3(90f, 0.2f, 90f), grass);

            var bounds = MaterialFactory.Lit(new Color(0.1f, 0.12f, 0.1f));
            for (var i = 0; i < 4; i++)
            {
                var horizontal = i < 2;
                var sign = i % 2 == 0 ? 1f : -1f;
                var go = PrimitiveFactory.Box(root, "Boundary", horizontal
                        ? new Vector3(0f, 2f, sign * 44.5f)
                        : new Vector3(sign * 44.5f, 2f, 0f),
                    horizontal ? new Vector3(90f, 4f, 0.5f) : new Vector3(0.5f, 4f, 90f), bounds);
                go.GetComponent<Renderer>().enabled = false;
            }
        }

        private void BuildRoads()
        {
            var eastWest = PrimitiveFactory.Box(root, "Road_EW", new Vector3(0f, 0.005f, 0f),
                new Vector3(90f, 0.02f, 7f), asphalt, false);
            var northSouth = PrimitiveFactory.Box(root, "Road_NS", new Vector3(0f, 0.006f, 0f),
                new Vector3(7f, 0.02f, 90f), asphalt, false);
            eastWest.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            northSouth.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            for (var i = -4; i <= 4; i++)
            {
                PrimitiveFactory.Box(root, "Sidewalk", new Vector3(i * 10f, 0.008f, 4.2f),
                    new Vector3(9f, 0.02f, 1.2f), sidewalk, false);
                PrimitiveFactory.Box(root, "Sidewalk", new Vector3(4.2f, 0.008f, i * 10f),
                    new Vector3(1.2f, 0.02f, 9f), sidewalk, false);
            }

            for (var x = -30f; x <= 30f; x += 4f)
            {
                PrimitiveFactory.Box(root, "LaneMark", new Vector3(x, 0.02f, 0f),
                    new Vector3(1.4f, 0.01f, 0.16f), sidewalk, false);
            }
        }

        private void BuildTown(PlayerController player)
        {
            CreateBuilding(player, "守夜人小屋", new Vector3(10f, 0f, 10f), 10f, 8f,
                new Color(0.36f, 0.28f, 0.2f), new Color(0.33f, 0.25f, 0.16f), true);
            CreateBuilding(player, "杂货铺", new Vector3(-12f, 0f, 10f), 10f, 8f,
                new Color(0.4f, 0.32f, 0.26f), new Color(0.22f, 0.22f, 0.2f), false);
            CreateBuilding(player, "社区诊所", new Vector3(12f, 0f, -12f), 10f, 9f,
                new Color(0.36f, 0.38f, 0.38f), new Color(0.3f, 0.32f, 0.32f), false);
            CreateBuilding(player, "旧仓库", new Vector3(-14f, 0f, -13f), 14f, 10f,
                new Color(0.3f, 0.27f, 0.24f), new Color(0.2f, 0.2f, 0.19f), false);

            CreateCar(new Vector3(8f, 0f, 1.5f), 0f);
            CreateCar(new Vector3(-9f, 0f, -2.2f), 180f);
            CreateCar(new Vector3(1.8f, 0f, -18f), 90f);

            for (var i = 0; i < 8; i++)
            {
                var angle = i * Mathf.PI * 2f / 8f;
                var pos = new Vector3(Mathf.Cos(angle) * 26f, 0f, Mathf.Sin(angle) * 26f);
                CreateStreetLight(pos);
            }
        }

        private void CreateBuilding(PlayerController player, string name, Vector3 center, float width, float depth,
            Color wallColor, Color floorColor, bool safehouse)
        {
            var building = new GameObject("Building_" + name);
            building.transform.SetParent(root, false);
            building.transform.position = center;

            var floor = PrimitiveFactory.Box(building.transform, "Floor", new Vector3(0f, 0.05f, 0f),
                new Vector3(width, 0.1f, depth), MaterialFactory.Lit(floorColor), true);
            floor.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var wallMat = MaterialFactory.Lit(wallColor);
            var frontMat = MaterialFactory.TransparentLit(wallColor);
            var height = 3f;
            var thickness = 0.22f;
            var halfW = width * 0.5f;
            var halfD = depth * 0.5f;
            var doorWidth = 1.7f;

            // 北墙与东西墙
            PrimitiveFactory.Box(building.transform, "WallN", new Vector3(0f, height * 0.5f, halfD),
                new Vector3(width, height, thickness), wallMat);
            PrimitiveFactory.Box(building.transform, "WallE", new Vector3(halfW, height * 0.5f, 0f),
                new Vector3(thickness, height, depth), wallMat);
            PrimitiveFactory.Box(building.transform, "WallW", new Vector3(-halfW, height * 0.5f, 0f),
                new Vector3(thickness, height, depth), wallMat);

            // 南墙留门洞，两段墙参与淡出
            var segment = (width - doorWidth) * 0.5f;
            var left = PrimitiveFactory.Box(building.transform, "WallS_L",
                new Vector3(-(doorWidth * 0.5f + segment * 0.5f), height * 0.5f, -halfD),
                new Vector3(segment, height, thickness), frontMat);
            var right = PrimitiveFactory.Box(building.transform, "WallS_R",
                new Vector3(doorWidth * 0.5f + segment * 0.5f, height * 0.5f, -halfD),
                new Vector3(segment, height, thickness), frontMat);

            // 门框
            PrimitiveFactory.Box(building.transform, "DoorPostL", new Vector3(-doorWidth * 0.5f, 1.15f, -halfD),
                new Vector3(0.12f, 2.3f, 0.26f), wood);
            PrimitiveFactory.Box(building.transform, "DoorPostR", new Vector3(doorWidth * 0.5f, 1.15f, -halfD),
                new Vector3(0.12f, 2.3f, 0.26f), wood);
            PrimitiveFactory.Box(building.transform, "DoorLintel", new Vector3(0f, 2.34f, -halfD),
                new Vector3(doorWidth, 0.14f, 0.26f), wood);
            PrimitiveFactory.Box(building.transform, "DoorPanel",
                new Vector3(-doorWidth * 0.5f + 0.04f, 1.1f, -halfD + 0.8f),
                new Vector3(0.06f, 2.1f, 1.55f), wood, false);

            // 窗（简单发光面）
            for (var i = -1; i <= 1; i += 2)
            {
                PrimitiveFactory.Box(building.transform, "Window", new Vector3(i * width * 0.24f, 1.85f, halfD - 0.13f),
                    new Vector3(1.4f, 0.95f, 0.04f), glass, false);
            }

            var roofGo = PrimitiveFactory.Box(building.transform, "Roof", new Vector3(0f, height + 0.14f, 0f),
                new Vector3(width + 0.3f, 0.16f, depth + 0.3f), roof, false);
            var roofRenderer = roofGo.GetComponent<Renderer>();

            var lightGo = new GameObject("InteriorLight");
            lightGo.transform.SetParent(building.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 8f;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.76f, 0.5f);
            light.shadows = LightShadows.None;

            var trigger = building.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.3f, 0f);
            trigger.size = new Vector3(width - 0.8f, 2.6f, depth - 0.8f);

            var controller = building.AddComponent<BuildingController>();
            controller.Setup(name, roofRenderer, new[] { left.GetComponent<Renderer>(), right.GetComponent<Renderer>() }, light);
            Buildings.Add(controller);
            MapBuildings.Add(new BuildingInfo
            {
                Name = name,
                Center = new Vector2(center.x, center.z),
                Size = new Vector2(width, depth)
            });

            PopulateInterior(building.transform, name, width, depth, safehouse);
        }

        private void PopulateInterior(Transform building, string name, float width, float depth, bool safehouse)
        {
            var furniture = MaterialFactory.Lit(new Color(0.32f, 0.24f, 0.16f));
            var cloth = MaterialFactory.Lit(new Color(0.42f, 0.42f, 0.38f));
            var metalDark = MaterialFactory.Lit(new Color(0.22f, 0.23f, 0.23f), 0.4f, 0.4f);

            if (safehouse)
            {
                PrimitiveFactory.Box(building, "BedBase", new Vector3(-width * 0.3f, 0.25f, depth * 0.28f),
                    new Vector3(1.2f, 0.5f, 2f), furniture);
                PrimitiveFactory.Box(building, "Mattress", new Vector3(-width * 0.3f, 0.55f, depth * 0.28f),
                    new Vector3(1.1f, 0.16f, 1.9f), cloth);
                PrimitiveFactory.Box(building, "Table", new Vector3(width * 0.28f, 0.4f, depth * 0.1f),
                    new Vector3(1.3f, 0.08f, 0.9f), furniture);
                PrimitiveFactory.Box(building, "TableLeg", new Vector3(width * 0.28f, 0.2f, depth * 0.1f),
                    new Vector3(0.12f, 0.4f, 0.12f), furniture);
                CreateCrate(building, new Vector3(width * 0.16f, 0.45f, -depth * 0.25f), "木箱",
                    new[] { "water_bottle", "bandage", "scrap" }, new[] { 1, 2, 1 });
            }
            else if (name.Contains("杂货"))
            {
                for (var i = -1; i <= 1; i++)
                {
                    PrimitiveFactory.Box(building, "Shelf", new Vector3(i * 2.1f, 1.1f, depth * 0.28f),
                        new Vector3(1.6f, 2.2f, 0.4f), furniture);
                }
                PrimitiveFactory.Box(building, "Counter", new Vector3(0f, 0.55f, -depth * 0.18f),
                    new Vector3(3.6f, 1.1f, 0.7f), furniture);
                CreateCrate(building, new Vector3(-width * 0.3f, 0.45f, -depth * 0.28f), "货箱",
                    new[] { "canned_food", "energy_bar", "water_bottle", "scrap" }, new[] { 2, 2, 1, 1 });
            }
            else if (name.Contains("诊所"))
            {
                PrimitiveFactory.Box(building, "Bed", new Vector3(-width * 0.28f, 0.35f, depth * 0.2f),
                    new Vector3(1.2f, 0.7f, 2f), metalDark);
                PrimitiveFactory.Box(building, "Cabinet", new Vector3(width * 0.3f, 1.1f, depth * 0.25f),
                    new Vector3(1.4f, 2.2f, 0.5f), cloth);
                PrimitiveFactory.Box(building, "Desk", new Vector3(width * 0.15f, 0.4f, -depth * 0.15f),
                    new Vector3(1.6f, 0.08f, 0.8f), furniture);
                CreateCrate(building, new Vector3(-width * 0.28f, 0.45f, -depth * 0.3f), "医疗柜",
                    new[] { "bandage", "bandage", "spirit_herb" }, new[] { 3, 2, 1 });
                CreateCrate(building, new Vector3(width * 0.28f, 0.45f, -depth * 0.3f), "药品柜",
                    new[] { "ammo_9mm", "ammo_9mm", "pistol" }, new[] { 8, 8, 1 });
            }
            else
            {
                for (var i = -1; i <= 1; i++)
                {
                    CreateCrate(building, new Vector3(i * 2.4f, 0.45f, depth * 0.2f), "货箱",
                        new[] { "scrap", "scrap", "spirit_stone" }, new[] { 2, 2, 1 });
                }
                CreateCrate(building, new Vector3(0f, 0.45f, -depth * 0.28f), "军械箱",
                    new[] { "ammo_762", "ammo_762", "hunting_rifle" }, new[] { 8, 8, 1 });
                PrimitiveFactory.Box(building, "Bench", new Vector3(0f, 0.4f, -depth * 0.28f),
                    new Vector3(3f, 0.8f, 0.6f), furniture);
            }
        }

        private void CreateCrate(Transform parent, Vector3 localPosition, string label, string[] itemIds, int[] counts)
        {
            var crate = new GameObject("Crate_" + label);
            crate.transform.SetParent(parent, false);
            crate.transform.localPosition = localPosition;
            var box = PrimitiveFactory.Box(crate.transform, "Box", Vector3.zero,
                new Vector3(0.9f, 0.9f, 0.9f), wood);
            var lid = PrimitiveFactory.Box(crate.transform, "Lid", new Vector3(0f, 0.46f, -0.35f),
                new Vector3(0.94f, 0.06f, 0.34f), wood);
            var interactable = crate.AddComponent<ContainerInteractable>();
            interactable.Label = label;
            interactable.ItemIds.AddRange(itemIds);
            interactable.Counts.AddRange(counts);
            interactable.Lid = lid.transform;
            box.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        private void CreateCar(Vector3 position, float yaw)
        {
            var car = new GameObject("Car");
            car.transform.SetParent(root, false);
            car.transform.position = position;
            car.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var bodyMat = MaterialFactory.Lit(new Color(0.36f, 0.14f, 0.14f), 0.4f, 0.3f);
            PrimitiveFactory.Box(car.transform, "Body", new Vector3(0f, 0.55f, 0f), new Vector3(1.9f, 0.65f, 4f), bodyMat);
            PrimitiveFactory.Box(car.transform, "Cabin", new Vector3(0f, 1.05f, -0.2f), new Vector3(1.7f, 0.55f, 2.1f),
                glass);
            PrimitiveFactory.Cylinder(car.transform, "Wheel", new Vector3(-0.95f, 0.3f, 1.3f),
                new Vector3(0.5f, 0.12f, 0.5f), metal).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            PrimitiveFactory.Cylinder(car.transform, "Wheel", new Vector3(0.95f, 0.3f, 1.3f),
                new Vector3(0.5f, 0.12f, 0.5f), metal).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            PrimitiveFactory.Cylinder(car.transform, "Wheel", new Vector3(-0.95f, 0.3f, -1.3f),
                new Vector3(0.5f, 0.12f, 0.5f), metal).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            PrimitiveFactory.Cylinder(car.transform, "Wheel", new Vector3(0.95f, 0.3f, -1.3f),
                new Vector3(0.5f, 0.12f, 0.5f), metal).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        private void CreateStreetLight(Vector3 position)
        {
            var lightGo = new GameObject("StreetLight");
            lightGo.transform.SetParent(root, false);
            lightGo.transform.position = position;
            PrimitiveFactory.Cylinder(lightGo.transform, "Pole", new Vector3(0f, 2f, 0f),
                new Vector3(0.16f, 2f, 0.16f), metal);
            PrimitiveFactory.Sphere(lightGo.transform, "Lamp", new Vector3(0f, 4.05f, 0f),
                new Vector3(0.5f, 0.5f, 0.5f), MaterialFactory.Emissive(new Color(1f, 0.85f, 0.6f),
                    new Color(1f, 0.7f, 0.35f)), false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 12f;
            light.intensity = 0f;
            light.color = new Color(1f, 0.73f, 0.42f);
            light.shadows = LightShadows.None;
            var streetLight = lightGo.AddComponent<StreetLight>();
            streetLight.Light = light;
            StreetLights.Add(streetLight);
        }

        private void BuildSpiritVein()
        {
            var go = new GameObject("古井灵脉");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(0f, 0f, 24f);
            var glow = MaterialFactory.Emissive(new Color(0.3f, 0.8f, 0.7f), new Color(0.4f, 1f, 0.85f));
            PrimitiveFactory.Cylinder(go.transform, "Aura", new Vector3(0f, 0.04f, 0f),
                new Vector3(2.2f, 0.02f, 2.2f), MaterialFactory.Unlit(new Color(0.4f, 1f, 0.82f, 0.12f), true), false);
            PrimitiveFactory.Cylinder(go.transform, "Well", new Vector3(0f, 0.35f, 0f),
                new Vector3(1.2f, 0.35f, 1.2f), glow);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 10f;
            light.intensity = 1.5f;
            light.color = new Color(0.4f, 1f, 0.82f);

            var vein = go.AddComponent<SpiritVein>();
            vein.VeinName = "古井灵脉";
            vein.Radius = 5f;
            vein.Strength = 1.6f;
            vein.Glow = light;
            vein.Aura = go.transform.Find("Aura");
            Vein = vein;

            for (var i = 0; i < 3; i++)
            {
                var angle = i * Mathf.PI * 2f / 3f;
                var pos = go.transform.position + new Vector3(Mathf.Cos(angle) * 3f, 0.3f, Mathf.Sin(angle) * 3f);
                var herb = PrimitiveFactory.Sphere(root, "Herb", pos, new Vector3(0.35f, 0.35f, 0.35f),
                    MaterialFactory.Emissive(new Color(0.4f, 0.9f, 0.7f), new Color(0.4f, 1f, 0.8f)), false);
                herb.AddComponent<HerbInteractable>();
            }
        }

        private void BuildTrees()
        {
            var trunkMat = MaterialFactory.Lit(new Color(0.22f, 0.15f, 0.09f));
            var leafMat = MaterialFactory.Lit(new Color(0.13f, 0.24f, 0.14f));
            var random = new System.Random(20261021);
            for (var i = 0; i < 26; i++)
            {
                var angle = (float)random.NextDouble() * Mathf.PI * 2f;
                var radius = 28f + (float)random.NextDouble() * 14f;
                var pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                if (Mathf.Abs(pos.x) < 6f || Mathf.Abs(pos.z) < 6f) continue;
                var tree = new GameObject("Tree");
                tree.transform.SetParent(root, false);
                tree.transform.position = pos;
                PrimitiveFactory.Cylinder(tree.transform, "Trunk", new Vector3(0f, 1.2f, 0f),
                    new Vector3(0.32f, 1.2f, 0.32f), trunkMat);
                PrimitiveFactory.Sphere(tree.transform, "Canopy", new Vector3(0f, 2.9f, 0f),
                    new Vector3(2.4f, 2.2f, 2.4f), leafMat, false);
            }
        }

        private void BuildZombies(PlayerController player)
        {
            var content = GameRoot.Instance.Sim.Content;
            var positions = new[]
            {
                new Vector3(3f, 0.2f, 4f),
                new Vector3(-4f, 0.2f, 5f),
                new Vector3(18f, 0.2f, 3f),
                new Vector3(6f, 0.2f, -8f),
                new Vector3(-8f, 0.2f, -6f),
                new Vector3(20f, 0.2f, 16f),
                new Vector3(-20f, 0.2f, 18f),
                new Vector3(2f, 0.2f, -22f),
                new Vector3(12f, 0.2f, -20f),
                new Vector3(-24f, 0.2f, -4f),
                new Vector3(26f, 0.2f, 7f)
            };
            var types = new[] { "walker", "walker", "runner", "walker", "walker", "police", "hunter", "walker", "soldier", "walker", "brute" };
            for (var i = 0; i < positions.Length; i++)
            {
                var def = content.Zombie(types[i]) ?? content.Zombie("walker");
                Zombies.Add(ZombieAI.Create(root, positions[i], player, def, i));
            }
        }

        private void BuildNpcs(PlayerController player)
        {
            var sim = GameRoot.Instance.Sim;
            foreach (var def in sim.Content.Npcs)
            {
                var go = new GameObject("NPC_" + def.Name);
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(def.HomeX, 0.2f, def.HomeZ);
                var npc = go.AddComponent<NpcController>();
                npc.Setup(sim, def, player);
                Npcs.Add(npc);
            }
        }
    }
}
