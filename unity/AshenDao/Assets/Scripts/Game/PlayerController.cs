using UnityEngine;
using AshenDao.Sim;

namespace AshenDao.Game
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public Vector3 AimDirection { get; private set; } = Vector3.forward;
        public string CurrentPrompt { get; private set; } = "";
        public InteractableBase CurrentInteractable { get; private set; }

        private GameRoot root;
        private CharacterController controller;
        private Camera cam;
        private Transform visual;
        private Transform weaponPivot;
        private GameObject batVisual;
        private GameObject gunVisual;
        private GameObject meditationAura;
        private CharacterAnimation anim;
        private Transform torso;
        private Transform head;
        private Transform armL;
        private Transform armR;
        private Transform legL;
        private Transform legR;
        private float stepTimer;
        private float nextAttack;
        private float weaponSwing;
        private float verticalVelocity;

        public void Setup(GameRoot owner)
        {
            root = owner;
            controller = GetComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.34f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.35f;
            controller.slopeLimit = 50f;
            gameObject.layer = 2;
            cam = Camera.main;
            BuildVisual();
        }

        private void BuildVisual()
        {
            var jacket = MaterialFactory.Lit(new Color(0.22f, 0.42f, 0.34f));
            var cloth = MaterialFactory.Lit(new Color(0.16f, 0.2f, 0.24f));
            var skin = MaterialFactory.Lit(new Color(0.78f, 0.6f, 0.42f));
            var hair = MaterialFactory.Lit(new Color(0.08f, 0.07f, 0.06f));
            var wood = MaterialFactory.Lit(new Color(0.4f, 0.28f, 0.16f));

            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);

            torso = PrimitiveFactory.Capsule(visual, "Torso", new Vector3(0f, 0.95f, 0f), new Vector3(0.62f, 0.9f, 0.62f), jacket, false).transform;
            head = PrimitiveFactory.Sphere(visual, "Head", new Vector3(0f, 1.66f, 0f), new Vector3(0.38f, 0.38f, 0.38f), skin, false).transform;
            PrimitiveFactory.Sphere(visual, "Hair", new Vector3(0f, 1.74f, -0.02f), new Vector3(0.4f, 0.26f, 0.4f), hair, false);
            legL = PrimitiveFactory.Capsule(visual, "LegL", new Vector3(-0.17f, 0.42f, 0f), new Vector3(0.2f, 0.45f, 0.2f), cloth, false).transform;
            legR = PrimitiveFactory.Capsule(visual, "LegR", new Vector3(0.17f, 0.42f, 0f), new Vector3(0.2f, 0.45f, 0.2f), cloth, false).transform;
            armL = PrimitiveFactory.Capsule(visual, "ArmL", new Vector3(-0.4f, 1.12f, 0f), new Vector3(0.16f, 0.38f, 0.16f), jacket, false).transform;
            armR = PrimitiveFactory.Capsule(visual, "ArmR", new Vector3(0.4f, 1.12f, 0f), new Vector3(0.16f, 0.38f, 0.16f), jacket, false).transform;

            anim = gameObject.AddComponent<CharacterAnimation>();
            anim.Setup(visual, torso, head, armL, armR, legL, legR);

            weaponPivot = new GameObject("WeaponPivot").transform;
            weaponPivot.SetParent(visual, false);
            weaponPivot.localPosition = new Vector3(0.43f, 1.05f, 0.1f);
            batVisual = PrimitiveFactory.Box(weaponPivot, "Bat", new Vector3(0f, 0f, 0.32f),
                new Vector3(0.055f, 0.055f, 0.64f), wood, false);
            gunVisual = PrimitiveFactory.Box(weaponPivot, "Gun", new Vector3(0f, 0.02f, 0.42f),
                new Vector3(0.09f, 0.11f, 0.52f), MaterialFactory.Lit(new Color(0.1f, 0.1f, 0.11f), 0.8f, 0.6f), false);
            gunVisual.SetActive(false);

            var auraMat = MaterialFactory.Unlit(new Color(0.35f, 1f, 0.8f, 0.16f), true);
            meditationAura = PrimitiveFactory.Cylinder(transform, "MeditationAura", new Vector3(0f, 0.06f, 0f),
                new Vector3(2.4f, 0.02f, 2.4f), auraMat, false);
            meditationAura.SetActive(false);
        }

        private void Update()
        {
            if (root == null) return;
            if (cam == null) cam = Camera.main;
            var sim = root.Sim;
            if (sim == null) return;

            if (sim.Player.Dead)
            {
                CurrentPrompt = "";
                CurrentInteractable = null;
                if (anim != null) anim.SetDead();
                return;
            }

            var blocked = root.InputBlocked;
            if (!blocked)
            {
                UpdateAim();
                InteractableBase nearest = InteractableBase.FindNearest(transform.position, 2.2f);
                CurrentInteractable = nearest;
                CurrentPrompt = nearest != null ? nearest.Prompt : "";
            }
            else
            {
                CurrentPrompt = "";
            }

            var wantsMove = false;
            Vector3 move = Vector3.zero;

            if (!blocked && !sim.Meditating)
            {
                var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
                if (input.sqrMagnitude > 0.01f)
                {
                    var flatForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
                    var flatRight = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
                    move = (flatForward * input.z + flatRight * input.x).normalized;
                    wantsMove = true;
                }

                var sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                var speed = sprint && sim.Player.Stamina > 1f ? sim.SprintSpeed : sim.MoveSpeed;
                if (sim.Player.Stamina <= 1f) speed *= 0.55f;
                if (sprint && wantsMove)
                {
                    sim.Player.Stamina = Mathf.Max(0f, sim.Player.Stamina - 13f * Time.deltaTime);
                }

                if (!controller.isGrounded) verticalVelocity -= 22f * Time.deltaTime;
                else if (verticalVelocity < 0f) verticalVelocity = -2f;

                var before = transform.position;
                controller.Move((move * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
                var moved = transform.position - before;
                moved.y = 0f;
                sim.RecordMovement(moved.magnitude, sprint);
                if (wantsMove)
                {
                    transform.rotation = Quaternion.LookRotation(move);
                    stepTimer -= Time.deltaTime;
                    if (stepTimer <= 0f)
                    {
                        stepTimer = sprint ? 0.32f : 0.54f;
                        sim.EmitNoise("footstep", transform.position.x, transform.position.z,
                            sprint ? 8f : 4f, sprint ? 0.55f : 0.25f);
                    }
                }
                anim.SetLocomotion(speed, wantsMove, sprint);
                anim.SetAiming(sim.IsFirearmEquipped);
            }
            else
            {
                if (sim.Meditating && Mathf.Abs(Input.GetAxisRaw("Horizontal")) + Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f)
                {
                    sim.StopMeditate("起身走动，入定中断。");
                }
                if (!controller.isGrounded) verticalVelocity -= 22f * Time.deltaTime;
                else verticalVelocity = -2f;
                controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
                anim.SetLocomotion(0f, false, false);
                anim.SetAiming(sim.IsFirearmEquipped);
            }

            UpdateCombat(sim, blocked);
            UpdateMeditationVisual(sim);
            UpdateWeapon();
        }

        private void UpdateAim()
        {
            if (cam == null) return;
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.up, new Vector3(0f, 0.1f, 0f));
            if (plane.Raycast(ray, out var enter))
            {
                var point = ray.GetPoint(enter);
                var direction = point - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.01f)
                {
                    AimDirection = direction.normalized;
                    transform.rotation = Quaternion.LookRotation(AimDirection);
                }
            }
        }

        private void UpdateCombat(WorldSim sim, bool blocked)
        {
            if (blocked) return;

            if (Input.GetMouseButton(0))
            {
                if (sim.IsFirearmEquipped)
                {
                    if (sim.TryFire(out var shot))
                    {
                        FireShot(sim, shot);
                    }
                }
                else if (Time.time >= nextAttack)
                {
                    Attack(sim);
                }
            }

            if (Input.GetKeyDown(KeyCode.F)) ReloadWeapon(sim);
            if ((Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Alpha1)) && root.CastFirstActive())
            {
                anim.Play(CharacterAnimState.Cast, 0.4f);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2) && root.CastTechnique("wind_step"))
            {
                anim.Play(CharacterAnimState.Cast, 0.4f);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3) && root.CastTechnique("cloud_step"))
            {
                anim.Play(CharacterAnimState.Cast, 0.4f);
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (sim.Meditating) sim.StopMeditate("收功。");
                else sim.ToggleMeditate();
            }
            if (Input.GetKeyDown(KeyCode.E) && CurrentInteractable != null)
            {
                CurrentInteractable.Interact(this);
            }
        }

        public void Attack(WorldSim sim)
        {
            if (sim.Meditating || sim.Player.Dead) return;
            var equipped = sim.Player.Equipped != null ? sim.Content.Item(sim.Player.Equipped) : null;
            var weapon = equipped != null ? equipped.Weapon : null;
            var damage = weapon != null ? weapon.Damage : 8f;
            var range = weapon != null ? weapon.Range : 1.2f;
            var cooldown = weapon != null ? weapon.Cooldown : 0.55f;
            var cost = weapon != null ? weapon.StaminaCost : 4f;
            if (sim.Player.Stamina < cost) return;

            sim.Player.Stamina -= cost;
            nextAttack = Time.time + cooldown;
            weaponSwing = 1f;

            var center = transform.position + Vector3.up * 0.9f + AimDirection * (range * 0.6f);
            var hits = Physics.OverlapSphere(center, range * 0.7f, ~0, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                var zombie = hit.GetComponentInParent<ZombieAI>();
                if (zombie != null) zombie.TakeDamage(damage * sim.DamageMultiplier, AimDirection);
            }
            anim.Play(CharacterAnimState.Attack, 0.35f);
            sim.EmitNoise("melee", transform.position.x, transform.position.z,
                weapon != null ? Mathf.Max(2.5f, weapon.NoiseRadius) : 3f, 0.55f);
        }

        private void ReloadWeapon(WorldSim sim)
        {
            if (!sim.IsFirearmEquipped) return;
            anim.Play(CharacterAnimState.Reload, 0.8f);
            if (sim.Reload())
            {
                sim.EmitNoise("reload", transform.position.x, transform.position.z, 5f, 0.4f);
            }
        }

        private void FireShot(WorldSim sim, ShotSpec shot)
        {
            anim.Play(CharacterAnimState.Attack, 0.28f);
            var origin = transform.position + Vector3.up * 1.42f + AimDirection * 0.42f;
            var muzzle = origin + AimDirection * 0.2f;
            sim.EmitNoise("gunshot", transform.position.x, transform.position.z, shot.NoiseRadius, 1f);
            for (var i = 0; i < shot.Pellets; i++)
            {
                var spread = Quaternion.Euler(
                    Random.Range(-shot.Spread, shot.Spread) * 90f,
                    Random.Range(-shot.Spread, shot.Spread) * 90f,
                    0f);
                var direction = spread * AimDirection;
                var end = origin + direction * shot.Range;
                if (Physics.Raycast(origin, direction, out var hit, shot.Range, ~0,
                        QueryTriggerInteraction.Ignore))
                {
                    end = hit.point;
                    var zombie = hit.collider.GetComponentInParent<ZombieAI>();
                    if (zombie != null)
                    {
                        zombie.TakeDamage(shot.Damage, direction);
                    }
                }
                SpellVfx.Tracer(muzzle, end, new Color(1f, 0.82f, 0.45f, 0.85f));
            }
        }

        public bool CastTechnique(WorldSim sim, string id)
        {
            if (!sim.CastTechnique(id)) return false;
            var tech = sim.Content.Technique(id);
            if (tech == null || tech.Active == null) return true;

            if (tech.Active.Kind == "strike")
            {
                SpellVfx.Slash(transform.position, AimDirection, tech.Active.Range, tech.Active.Arc,
                    new Color(0.55f, 0.95f, 1f, 0.55f));
                var hits = Physics.OverlapSphere(transform.position + Vector3.up * 0.9f, tech.Active.Range, ~0,
                    QueryTriggerInteraction.Ignore);
                foreach (var hit in hits)
                {
                    var zombie = hit.GetComponentInParent<ZombieAI>();
                    if (zombie == null) continue;
                    var to = zombie.transform.position - transform.position;
                    to.y = 0f;
                    if (Vector3.Angle(AimDirection, to) > tech.Active.Arc * Mathf.Rad2Deg * 0.5f) continue;
                    zombie.TakeDamage(tech.Active.Damage * sim.DamageMultiplier, to.normalized);
                }
            }
            else if (tech.Active.Kind == "shield")
            {
                sim.Player.Shield = tech.Active.Amount;
                sim.Player.ShieldUntil = sim.Clock.TotalMinutes + tech.Active.Duration;
                SpellVfx.Slash(transform.position, AimDirection, 1.6f, 6.28f, new Color(1f, 0.85f, 0.4f, 0.3f));
            }
            return true;
        }

        public void TakeDamage(float amount)
        {
            var sim = root.Sim;
            if (sim == null || sim.Player.Dead) return;
            sim.DamagePlayer(amount, true);
            anim.Play(CharacterAnimState.Hit, 0.25f);
            if (sim.Player.Dead)
            {
                anim.SetDead();
                root.OnPlayerDead();
            }
        }

        private void UpdateMeditationVisual(WorldSim sim)
        {
            if (meditationAura == null) return;
            meditationAura.SetActive(sim.Meditating);
            anim.SetMeditating(sim.Meditating);
            if (!sim.Meditating) return;
            var pulse = 1f + Mathf.Sin(Time.time * 2.2f) * 0.08f;
            meditationAura.transform.localScale = new Vector3(2.4f * pulse, 0.02f, 2.4f * pulse);
        }

        private void UpdateWeapon()
        {
            if (weaponPivot == null) return;
            var firearm = root != null && root.Sim != null && root.Sim.IsFirearmEquipped;
            if (batVisual != null) batVisual.SetActive(!firearm);
            if (gunVisual != null) gunVisual.SetActive(firearm);
            weaponSwing = Mathf.MoveTowards(weaponSwing, 0f, Time.deltaTime * 5f);
            var angle = Mathf.Sin((1f - weaponSwing) * Mathf.PI) * -80f;
            weaponPivot.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }
    }
}
