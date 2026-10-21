using UnityEngine;
using AshenDao.Sim;

namespace AshenDao.Game
{
    [RequireComponent(typeof(CharacterController))]
    public class ZombieAI : MonoBehaviour
    {
        public ZombieDef Def { get; private set; }
        public string ZombieType => Def != null ? Def.Kind : "walker";
        public bool IsInvestigating => Time.time < investigateUntil;
        public bool IsDead => dead;

        public float Hp = 55f;
        public float Speed = 1.45f;
        public float Damage = 7f;
        public float Sight = 16f;
        public float AttackRange = 1.55f;

        private PlayerController player;
        private CharacterController controller;
        private CharacterAnimation anim;
        private Transform visual;
        private Renderer bodyRenderer;
        private Material bodyMaterial;
        private Color baseColor;
        private Vector3 home;
        private Vector3 wanderTarget;
        private Vector3 investigate;
        private float investigateUntil;
        private float nextWander;
        private float nextAttack;
        private float verticalVelocity;
        private bool dead;
        private float hitFlash;
        private bool chasing;
        private int lastNoiseId;
        private float nextNoiseScan;

        public static ZombieAI Create(Transform parent, Vector3 position, PlayerController target, ZombieDef def,
            int variant)
        {
            var go = new GameObject("Zombie_" + (def != null ? def.Kind : "walker"));
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var ai = go.AddComponent<ZombieAI>();
            ai.Setup(target, position, def, variant);
            return ai;
        }

        private void Setup(PlayerController target, Vector3 position, ZombieDef def, int variant)
        {
            player = target;
            Def = def;
            Hp = def != null ? def.Hp : 55f;
            Speed = def != null ? def.Speed : 1.45f;
            Damage = def != null ? def.Damage : 7f;
            Sight = def != null ? def.Sight : 16f;
            home = position;
            wanderTarget = position;

            controller = GetComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.34f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.3f;

            var skin = MaterialFactory.Lit(new Color(0.43f, 0.52f, 0.36f));
            var cloth = MaterialFactory.Lit(new Color(0.28f, 0.27f, 0.2f));
            var dark = MaterialFactory.Lit(new Color(0.09f, 0.08f, 0.07f));
            var blood = MaterialFactory.Lit(new Color(0.35f, 0.08f, 0.07f));
            var tint = cloth;
            if (def != null)
            {
                ColorUtility.TryParseHtmlString(def.Tint, out var parsedTint);
                tint = MaterialFactory.Lit(parsedTint);
            }

            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            var torso = PrimitiveFactory.Capsule(visual, "Torso", new Vector3(0f, 0.9f, 0f),
                new Vector3(0.6f, 0.86f, 0.6f), variant % 3 == 0 ? blood : tint, false).transform;
            bodyRenderer = torso.GetComponent<Renderer>();
            if (bodyRenderer != null)
            {
                bodyMaterial = bodyRenderer.material;
                baseColor = MaterialFactory.GetColor(bodyMaterial);
            }
            var head = PrimitiveFactory.Sphere(visual, "Head", new Vector3(0f, 1.62f, 0.04f),
                new Vector3(0.36f, 0.36f, 0.36f), skin, false).transform;
            var armL = PrimitiveFactory.Capsule(visual, "ArmL", new Vector3(-0.45f, 1.18f, 0.25f),
                new Vector3(0.14f, 0.36f, 0.14f), skin, false).transform;
            var armR = PrimitiveFactory.Capsule(visual, "ArmR", new Vector3(0.45f, 1.18f, 0.25f),
                new Vector3(0.14f, 0.36f, 0.14f), skin, false).transform;
            armL.localRotation = Quaternion.Euler(70f, 0f, 0f);
            armR.localRotation = Quaternion.Euler(70f, 0f, 0f);
            var legL = PrimitiveFactory.Capsule(visual, "LegL", new Vector3(-0.16f, 0.38f, 0f),
                new Vector3(0.18f, 0.42f, 0.18f), dark, false).transform;
            var legR = PrimitiveFactory.Capsule(visual, "LegR", new Vector3(0.16f, 0.38f, 0f),
                new Vector3(0.18f, 0.42f, 0.18f), dark, false).transform;
            PrimitiveFactory.Box(visual, "Shoulder", new Vector3(0f, 1.3f, -0.18f),
                new Vector3(0.7f, 0.16f, 0.25f), tint, false);

            anim = gameObject.AddComponent<CharacterAnimation>();
            anim.Setup(visual, torso, head, armL, armR, legL, legR);
        }

        private void Update()
        {
            if (dead || player == null) return;
            var sim = GameRoot.Instance != null ? GameRoot.Instance.Sim : null;
            if (sim == null || sim.Player.Dead) return;

            if (hitFlash > 0f)
            {
                hitFlash -= Time.deltaTime;
                if (bodyMaterial != null)
                {
                    MaterialFactory.SetColor(bodyMaterial, hitFlash > 0f ? Color.white : baseColor);
                }
            }

            ScanNoise(sim);

            var toPlayer = player.transform.position - transform.position;
            var flat = new Vector3(toPlayer.x, 0f, toPlayer.z);
            var distance = flat.magnitude;
            chasing = distance <= Sight && HasLineOfSight();

            Vector3 moveDir = Vector3.zero;
            var speed = Speed;
            if (chasing)
            {
                if (distance > AttackRange * 0.85f) moveDir = flat.normalized;
                else if (Time.time >= nextAttack)
                {
                    nextAttack = Time.time + 1.35f;
                    player.TakeDamage(Damage);
                    if (sim.RandomFloat() < 0.22f) sim.AddInfection(9f);
                    anim.Play(CharacterAnimState.Bite, 0.42f);
                    sim.EmitNoise("bite", transform.position.x, transform.position.z, 6f, 0.4f);
                }
            }
            else if (Time.time < investigateUntil)
            {
                var toNoise = investigate - transform.position;
                toNoise.y = 0f;
                if (toNoise.magnitude > 0.8f)
                {
                    moveDir = toNoise.normalized;
                    speed *= 1.15f;
                }
            }
            else
            {
                if (Time.time >= nextWander)
                {
                    nextWander = Time.time + 3f + Random.value * 3f;
                    var offset = new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-4f, 4f));
                    wanderTarget = home + offset;
                }
                var toWander = wanderTarget - transform.position;
                toWander.y = 0f;
                if (toWander.magnitude > 0.5f) moveDir = toWander.normalized;
                speed *= 0.35f;
            }

            if (!controller.isGrounded) verticalVelocity -= 20f * Time.deltaTime;
            else if (verticalVelocity < 0f) verticalVelocity = -2f;
            controller.Move((moveDir * speed + Vector3.up * verticalVelocity) * Time.deltaTime);

            if (moveDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(moveDir);
            }
            anim.SetLocomotion(speed, moveDir.sqrMagnitude > 0.01f, speed > 2f);
        }

        private void ScanNoise(WorldSim sim)
        {
            if (Time.time < nextNoiseScan) return;
            nextNoiseScan = Time.time + 0.25f;
            var noises = sim.Noise.Recent;
            for (var i = 0; i < noises.Count; i++)
            {
                var noise = noises[i];
                if (noise.Id <= lastNoiseId) continue;
                lastNoiseId = noise.Id;
                if (!noise.Hears(transform.position.x, transform.position.z)) continue;
                investigate = new Vector3(noise.X, 0f, noise.Z);
                investigateUntil = Time.time + 7f;
                if (noise.Loudness > 0.7f)
                {
                    anim.Play(CharacterAnimState.Bite, 0.3f);
                }
            }
        }

        private bool HasLineOfSight()
        {
            var eye = transform.position + Vector3.up * 1.5f + transform.forward * 0.35f;
            var target = player.transform.position + Vector3.up * 1.1f;
            var direction = target - eye;
            if (direction.magnitude > Sight) return false;
            if (Physics.Linecast(eye, target, out var hit, ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.collider.GetComponentInParent<PlayerController>() != null;
            }
            return true;
        }

        public void TakeDamage(float amount, Vector3 fromDirection)
        {
            if (dead) return;
            Hp -= amount;
            hitFlash = 0.1f;
            transform.position += fromDirection.normalized * 0.12f;
            anim.Play(CharacterAnimState.Hit, 0.2f);
            var sim = GameRoot.Instance != null ? GameRoot.Instance.Sim : null;
            if (sim != null) sim.RecordDamageToZombie(amount, false);
            if (Hp <= 0f) Die();
        }

        private void Die()
        {
            dead = true;
            if (bodyMaterial != null) MaterialFactory.SetColor(bodyMaterial, new Color(0.16f, 0.14f, 0.12f));
            anim.SetDead();
            var collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            if (controller != null) controller.enabled = false;
            if (GameRoot.Instance != null) GameRoot.Instance.OnZombieKilled(this);
            Destroy(gameObject, 12f);
        }
    }
}
