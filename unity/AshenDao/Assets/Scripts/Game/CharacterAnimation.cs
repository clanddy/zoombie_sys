using UnityEngine;

namespace AshenDao.Game
{
    public enum CharacterAnimState
    {
        Idle,
        Walk,
        Run,
        Attack,
        Bite,
        Hit,
        Death,
        Sit,
        Talk,
        Meditate,
        Aim,
        Reload,
        Cast
    }

    /// <summary>
    /// Animation contract for every humanoid. The same component drives:
    ///  - a future imported Animator (parameters below), or
    ///  - the current primitive greybox rig (procedural limb poses),
    /// so gameplay code can be written once and art can be swapped later.
    ///
    /// Animator parameters:
    ///   Float  Speed, StateTime
    ///   Bool   Moving, Running, Dead, Sitting, Talking, Meditating, Aiming
    ///   Trigger Attack, Bite, Hit, Cast, Reload
    /// States: Idle / Walk / Run / Attack / Bite / Hit / Death / Sit / Talk / Meditate / Aim / Reload
    /// </summary>
    public class CharacterAnimation : MonoBehaviour
    {
        public CharacterAnimState State { get; private set; } = CharacterAnimState.Idle;
        public Animator Animator { get; private set; }

        private Transform visual;
        private Transform torso;
        private Transform head;
        private Transform armL;
        private Transform armR;
        private Transform legL;
        private Transform legR;

        private Vector3 visualBase;
        private Vector3 torsoBase;
        private Vector3 headBase;
        private Quaternion torsoBaseRot;
        private Quaternion headBaseRot;
        private Quaternion armLBaseRot;
        private Quaternion armRBaseRot;
        private Quaternion legLBaseRot;
        private Quaternion legRBaseRot;

        private bool locomotionMoving;
        private bool locomotionRunning;
        private float locomotionSpeed;
        private bool dead;
        private bool sitting;
        private bool talking;
        private bool meditating;
        private bool aiming;
        private float stateTime;
        private float stateDuration;
        private float phase;

        public void Setup(Transform visualRoot, Transform torsoBone, Transform headBone, Transform leftArm,
            Transform rightArm, Transform leftLeg, Transform rightLeg)
        {
            visual = visualRoot;
            torso = torsoBone;
            head = headBone;
            armL = leftArm;
            armR = rightArm;
            legL = leftLeg;
            legR = rightLeg;

            if (visual != null)
            {
                Animator = visual.GetComponentInChildren<Animator>();
                visualBase = visual.localPosition;
            }
            if (torso != null)
            {
                torsoBase = torso.localPosition;
                torsoBaseRot = torso.localRotation;
            }
            if (head != null)
            {
                headBase = head.localPosition;
                headBaseRot = head.localRotation;
            }
            if (armL != null) armLBaseRot = armL.localRotation;
            if (armR != null) armRBaseRot = armR.localRotation;
            if (legL != null) legLBaseRot = legL.localRotation;
            if (legR != null) legRBaseRot = legR.localRotation;
        }

        public void SetLocomotion(float speed, bool moving, bool running)
        {
            locomotionSpeed = speed;
            locomotionMoving = moving;
            locomotionRunning = running;
            if (stateTime < stateDuration) return;
            if (dead) return;
            State = moving ? (running ? CharacterAnimState.Run : CharacterAnimState.Walk) : CharacterAnimState.Idle;
            PushAnimator();
        }

        public void Play(CharacterAnimState next, float duration = 0.35f)
        {
            if (dead) return;
            State = next;
            stateTime = 0f;
            stateDuration = Mathf.Max(0.05f, duration);
            PushAnimator();
            if (Animator != null)
            {
                switch (next)
                {
                    case CharacterAnimState.Attack: Animator.SetTrigger("Attack"); break;
                    case CharacterAnimState.Bite: Animator.SetTrigger("Bite"); break;
                    case CharacterAnimState.Hit: Animator.SetTrigger("Hit"); break;
                    case CharacterAnimState.Cast: Animator.SetTrigger("Cast"); break;
                    case CharacterAnimState.Reload: Animator.SetTrigger("Reload"); break;
                }
            }
        }

        public void SetDead()
        {
            dead = true;
            State = CharacterAnimState.Death;
            stateDuration = 0f;
            if (Animator != null) Animator.SetBool("Dead", true);
        }

        public void SetSitting(bool value)
        {
            sitting = value;
            if (value) State = CharacterAnimState.Sit;
            PushAnimator();
        }

        public void SetTalking(bool value)
        {
            talking = value;
            if (value && !dead && stateTime >= stateDuration) State = CharacterAnimState.Talk;
            PushAnimator();
        }

        public void SetMeditating(bool value)
        {
            meditating = value;
            if (value && !dead) State = CharacterAnimState.Meditate;
            else if (!value && State == CharacterAnimState.Meditate) State = CharacterAnimState.Idle;
            PushAnimator();
        }

        public void SetAiming(bool value)
        {
            aiming = value;
            if (value && !dead && stateTime >= stateDuration) State = CharacterAnimState.Aim;
            PushAnimator();
        }

        private void Update()
        {
            if (visual == null) return;
            stateTime += Time.deltaTime;
            if (!dead && stateDuration > 0f && stateTime >= stateDuration)
            {
                stateDuration = 0f;
                if (meditating) State = CharacterAnimState.Meditate;
                else if (sitting) State = CharacterAnimState.Sit;
                else if (talking) State = CharacterAnimState.Talk;
                else if (aiming) State = CharacterAnimState.Aim;
                else if (locomotionMoving) State = locomotionRunning ? CharacterAnimState.Run : CharacterAnimState.Walk;
                else State = CharacterAnimState.Idle;
            }

            if (Animator != null)
            {
                Animator.SetFloat("Speed", locomotionSpeed);
                Animator.SetFloat("StateTime", stateTime);
                Animator.SetBool("Moving", locomotionMoving);
                Animator.SetBool("Running", locomotionRunning);
                Animator.SetBool("Sitting", sitting);
                Animator.SetBool("Talking", talking);
                Animator.SetBool("Meditating", meditating);
                Animator.SetBool("Aiming", aiming);
                return;
            }
            AnimatePrimitive();
        }

        private void PushAnimator()
        {
            if (Animator == null) return;
            Animator.SetBool("Dead", dead);
            Animator.SetBool("Sitting", sitting);
            Animator.SetBool("Talking", talking);
            Animator.SetBool("Meditating", meditating);
            Animator.SetBool("Aiming", aiming);
        }

        private void AnimatePrimitive()
        {
            if (torso == null || head == null) return;
            var t = Time.time;
            phase += Time.deltaTime * Mathf.Max(2f, locomotionSpeed * 2.1f);
            var swing = Mathf.Sin(phase) * (locomotionRunning ? 42f : 27f);
            var bob = locomotionMoving ? Mathf.Abs(Mathf.Sin(phase)) * (locomotionRunning ? 0.055f : 0.03f) : 0f;

            // Reset to bind pose first so states do not accumulate.
            visual.localPosition = visualBase + new Vector3(0f, bob, 0f);
            visual.localRotation = Quaternion.identity;
            torso.localPosition = torsoBase;
            torso.localRotation = torsoBaseRot;
            head.localPosition = headBase;
            head.localRotation = headBaseRot;
            if (armL != null) armL.localRotation = armLBaseRot;
            if (armR != null) armR.localRotation = armRBaseRot;
            if (legL != null) legL.localRotation = legLBaseRot;
            if (legR != null) legR.localRotation = legRBaseRot;

            switch (State)
            {
                case CharacterAnimState.Walk:
                case CharacterAnimState.Run:
                    if (legL != null) legL.localRotation = legLBaseRot * Quaternion.Euler(swing, 0f, 0f);
                    if (legR != null) legR.localRotation = legRBaseRot * Quaternion.Euler(-swing, 0f, 0f);
                    if (armL != null) armL.localRotation = armLBaseRot * Quaternion.Euler(-swing * 0.7f, 0f, 0f);
                    if (armR != null) armR.localRotation = armRBaseRot * Quaternion.Euler(swing * 0.7f, 0f, 0f);
                    torso.localRotation = torsoBaseRot * Quaternion.Euler(locomotionRunning ? 10f : 3f, 0f, 0f);
                    break;
                case CharacterAnimState.Attack:
                    if (armR != null) armR.localRotation = armRBaseRot * Quaternion.Euler(-110f, 0f, -15f);
                    if (armL != null) armL.localRotation = armLBaseRot * Quaternion.Euler(-25f, 0f, 15f);
                    torso.localRotation = torsoBaseRot * Quaternion.Euler(0f, -18f, 0f);
                    break;
                case CharacterAnimState.Bite:
                    torso.localPosition = torsoBase + new Vector3(0f, 0f, 0.18f);
                    torso.localRotation = torsoBaseRot * Quaternion.Euler(24f, 0f, 0f);
                    head.localRotation = headBaseRot * Quaternion.Euler(22f, 0f, 0f);
                    if (armL != null) armL.localRotation = armLBaseRot * Quaternion.Euler(-55f, 0f, 25f);
                    if (armR != null) armR.localRotation = armRBaseRot * Quaternion.Euler(-55f, 0f, -25f);
                    break;
                case CharacterAnimState.Hit:
                    torso.localRotation = torsoBaseRot * Quaternion.Euler(-18f, 0f, 8f);
                    head.localRotation = headBaseRot * Quaternion.Euler(-15f, 0f, 0f);
                    break;
                case CharacterAnimState.Death:
                    visual.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    visual.localPosition = visualBase + new Vector3(0f, 0.18f, 0f);
                    break;
                case CharacterAnimState.Sit:
                    if (legL != null) legL.localRotation = legLBaseRot * Quaternion.Euler(82f, 0f, 0f);
                    if (legR != null) legR.localRotation = legRBaseRot * Quaternion.Euler(82f, 0f, 0f);
                    visual.localPosition = visualBase + new Vector3(0f, -0.22f, 0f);
                    torso.localRotation = torsoBaseRot * Quaternion.Euler(4f, 0f, 0f);
                    break;
                case CharacterAnimState.Talk:
                    head.localRotation = headBaseRot * Quaternion.Euler(Mathf.Sin(t * 3.4f) * 5f, 0f, 0f);
                    torso.localRotation = torsoBaseRot * Quaternion.Euler(0f, Mathf.Sin(t * 1.8f) * 3f, 0f);
                    break;
                case CharacterAnimState.Meditate:
                    if (legL != null) legL.localRotation = legLBaseRot * Quaternion.Euler(88f, 0f, 18f);
                    if (legR != null) legR.localRotation = legRBaseRot * Quaternion.Euler(88f, 0f, -18f);
                    if (armL != null) armL.localRotation = armLBaseRot * Quaternion.Euler(40f, 0f, 30f);
                    if (armR != null) armR.localRotation = armRBaseRot * Quaternion.Euler(40f, 0f, -30f);
                    visual.localPosition = visualBase + new Vector3(0f, -0.26f, 0f);
                    break;
                case CharacterAnimState.Aim:
                    if (armR != null) armR.localRotation = armRBaseRot * Quaternion.Euler(-82f, 0f, 0f);
                    if (armL != null) armL.localRotation = armLBaseRot * Quaternion.Euler(-70f, 0f, 20f);
                    break;
                case CharacterAnimState.Reload:
                    if (armL != null) armL.localRotation = armLBaseRot * Quaternion.Euler(-60f, 0f, 25f);
                    if (armR != null) armR.localRotation = armRBaseRot * Quaternion.Euler(-45f, 0f, -20f);
                    break;
            }
        }
    }
}
