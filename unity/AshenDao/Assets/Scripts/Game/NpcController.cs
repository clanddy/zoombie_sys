using UnityEngine;
using AshenDao.Sim;

namespace AshenDao.Game
{
    public class NpcInteractable : InteractableBase
    {
        public string NpcId;
        public string DisplayName;
        public string Role;

        public override string Prompt => "交谈 · " + DisplayName;

        public override void Interact(PlayerController player)
        {
            GameRoot.Instance.OpenDialogue(NpcId);
        }
    }

    public class NpcController : MonoBehaviour
    {
        public string NpcId { get; private set; }
        public NpcDef Def { get; private set; }

        private WorldSim sim;
        private PlayerController player;
        private CharacterController controller;
        private CharacterAnimation anim;
        private Vector3 home;
        private Vector3 wanderTarget;
        private float nextWander;
        private float verticalVelocity;
        private bool following;
        private NpcOrder order;
        private float alertUntil;
        private Vector3 alertPoint;
        private int lastNoiseId;
        private float nextNoiseScan;
        private float nextAlertNotice;

        public void Setup(WorldSim world, NpcDef def, PlayerController target)
        {
            sim = world;
            Def = def;
            NpcId = def.Id;
            player = target;
            home = new Vector3(def.HomeX, 0.2f, def.HomeZ);

            controller = gameObject.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.32f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.35f;
            controller.slopeLimit = 50f;
            gameObject.layer = 2;

            BuildBody(def);

            var interactable = gameObject.AddComponent<NpcInteractable>();
            interactable.NpcId = def.Id;
            interactable.DisplayName = def.Name;
            interactable.Role = def.Role;

            order = sim.Npc(def.Id).CurrentOrder;
            following = order == NpcOrder.Follow;
            wanderTarget = home;
        }

        private void BuildBody(NpcDef def)
        {
            var tint = new Color(0.31f, 0.56f, 0.47f);
            ColorUtility.TryParseHtmlString(def.Tint, out tint);
            var cloth = MaterialFactory.Lit(tint);
            var dark = MaterialFactory.Lit(new Color(0.12f, 0.12f, 0.1f));
            var skin = MaterialFactory.Lit(new Color(0.74f, 0.56f, 0.4f));

            var visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            var torso = PrimitiveFactory.Capsule(visual, "Torso", new Vector3(0f, 0.95f, 0f),
                new Vector3(0.58f, 0.88f, 0.58f), cloth, false).transform;
            var head = PrimitiveFactory.Sphere(visual, "Head", new Vector3(0f, 1.64f, 0f),
                new Vector3(0.36f, 0.36f, 0.36f), skin, false).transform;
            var armL = PrimitiveFactory.Capsule(visual, "ArmL", new Vector3(-0.38f, 1.12f, 0f),
                new Vector3(0.15f, 0.36f, 0.15f), cloth, false).transform;
            var armR = PrimitiveFactory.Capsule(visual, "ArmR", new Vector3(0.38f, 1.12f, 0f),
                new Vector3(0.15f, 0.36f, 0.15f), cloth, false).transform;
            var legL = PrimitiveFactory.Capsule(visual, "LegL", new Vector3(-0.15f, 0.42f, 0f),
                new Vector3(0.18f, 0.44f, 0.18f), dark, false).transform;
            var legR = PrimitiveFactory.Capsule(visual, "LegR", new Vector3(0.15f, 0.42f, 0f),
                new Vector3(0.18f, 0.44f, 0.18f), dark, false).transform;
            PrimitiveFactory.Box(visual, "Satchel", new Vector3(0f, 0.98f, -0.28f),
                new Vector3(0.4f, 0.35f, 0.16f), MaterialFactory.Lit(new Color(0.35f, 0.28f, 0.18f)), false);

            anim = gameObject.AddComponent<CharacterAnimation>();
            anim.Setup(visual, torso, head, armL, armR, legL, legR);
        }

        private void Update()
        {
            if (sim == null || player == null || Def == null) return;
            var state = sim.Npc(NpcId);
            if (state == null) return;
            following = state.Following;
            order = state.CurrentOrder;
            ScanNoise();

            var move = Vector3.zero;
            var speed = 0f;
            if (following)
            {
                var to = player.transform.position - transform.position;
                to.y = 0f;
                if (to.magnitude > 2.2f)
                {
                    move = to.normalized;
                    speed = to.magnitude > 6f ? 4.6f : 3.1f;
                }
            }
            else if (order == NpcOrder.Idle || order == NpcOrder.Safehouse)
            {
                if (Time.time >= nextWander)
                {
                    nextWander = Time.time + 4f + Random.value * 5f;
                    var offset = new Vector3(Random.Range(-Def.WanderRadius, Def.WanderRadius), 0f,
                        Random.Range(-Def.WanderRadius, Def.WanderRadius));
                    wanderTarget = home + offset;
                }
                var to = wanderTarget - transform.position;
                to.y = 0f;
                if (to.magnitude > 0.4f)
                {
                    move = to.normalized;
                    speed = 1.7f;
                }
            }

            if (!controller.isGrounded) verticalVelocity -= 20f * Time.deltaTime;
            else if (verticalVelocity < 0f) verticalVelocity = -2f;
            controller.Move((move * speed + Vector3.up * verticalVelocity) * Time.deltaTime);

            if (move.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(move);
            }
            else if (following)
            {
                var look = player.transform.position - transform.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(look);
            }
            else if (Time.time < alertUntil)
            {
                var look = alertPoint - transform.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(look);
            }

            var talking = GameRoot.Instance != null && GameRoot.Instance.Dialogue != null &&
                          GameRoot.Instance.Dialogue.IsOpen && GameRoot.Instance.Dialogue.NpcId == NpcId;
            anim.SetLocomotion(speed, speed > 0.1f, speed > 4f);
            anim.SetTalking(talking);
            anim.SetSitting(!talking && !following && speed < 0.1f && order == NpcOrder.Safehouse && Def.AtSafehouse);
        }

        private void ScanNoise()
        {
            if (Time.time < nextNoiseScan) return;
            nextNoiseScan = Time.time + 0.35f;
            var noises = sim.Noise.Recent;
            for (var i = 0; i < noises.Count; i++)
            {
                var noise = noises[i];
                if (noise.Id <= lastNoiseId) continue;
                lastNoiseId = noise.Id;
                if (!noise.Hears(transform.position.x, transform.position.z)) continue;
                alertPoint = new Vector3(noise.X, 0f, noise.Z);
                alertUntil = Time.time + 5f;
                if (noise.Source.Contains("gun") && Time.time >= nextAlertNotice)
                {
                    nextAlertNotice = Time.time + 12f;
                    sim.RecordEvent(GameEventType.Noise, "npc.heard." + NpcId,
                        Def.Name + " 听见了枪声", 1);
                    sim.Notify(Def.Name + "：枪声会把它们都引过来。", NoticeTone.Warn);
                }
            }
        }
    }
}
