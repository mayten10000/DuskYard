using UnityEngine;

namespace DuskYard
{
    public sealed class CombatDemo : MonoBehaviour
    {
        public Transform hero, target, blade, cameraRig;
        public Renderer targetRenderer;
        public Light impactLight;
        public ParticleSystem sparks;
        public TrailRenderer trail;
        public Material readyMaterial, hitMaterial, recoveryMaterial, restoredMaterial;
        public Material warmEffect, coldEffect, warmTrail, coldTrail;
        public bool stress;
        public bool automated;
        public bool RecentImpact { get { return flash > .015f; } }
        public int TotalHits { get; private set; }
        public int TotalAttacks { get; private set; }
        public int PeakParticles { get; private set; }
        public string TargetState { get; private set; } = "READY";
        public string Mode { get; private set; } = "Gameplay";
        public int ComboStep { get; private set; }
        public int ComboStepsStarted { get; private set; }
        public int ComboStepsHit { get; private set; }
        public float AttackProgress { get { return swing < 0 ? -1 : swing / AttackDuration; } }
        public float AttackWeight { get { return swing < 0 ? 0 : Mathf.Sin(Mathf.Clamp01(swing / AttackDuration) * Mathf.PI); } }
        public static Quaternion GuardRotation { get { return Quaternion.Euler(-28, 12, -12); } }
        const float Windup = .18f, ActiveEnd = .44f, AttackDuration = .76f;

        const int TargetMask = 1 << 8;
        readonly Collider[] overlap = new Collider[8];
        readonly RaycastHit[] sweptHits = new RaycastHit[8];
        CharacterController controller;
        Quaternion targetRest;
        Vector3 previousTip;
        float yaw, pitch = 18, swing = -1, reaction = -1, flash, lastAttackEnd = -10;
        bool struck, cold, queued;
        Quaternion attackStart;
        BenchmarkRunner benchmark;

        void Start()
        {
            controller = hero.GetComponent<CharacterController>();
            targetRest = target.localRotation;
            blade.localRotation = GuardRotation;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            benchmark = GetComponent<BenchmarkRunner>();
            if (benchmark == null) benchmark = gameObject.AddComponent<BenchmarkRunner>();
            benchmark.demo = this;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (!automated)
            {
                if (DemoInput.GetKeyDown(KeyCode.Escape)) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
                if (DemoInput.GetMouseButtonDown(1)) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    yaw += DemoInput.GetAxis("Mouse X") * 2;
                    pitch = Mathf.Clamp(pitch - DemoInput.GetAxis("Mouse Y") * 2, 5, 45);
                }
                Vector3 move = Quaternion.Euler(0, yaw, 0) * new Vector3(DemoInput.GetAxisRaw("Horizontal"), 0, DemoInput.GetAxisRaw("Vertical"));
                if (move.sqrMagnitude > 1) move.Normalize();
                controller.Move((move * 3.5f + Vector3.down * 9) * dt);
                if (move.sqrMagnitude > .01f && swing < 0)
                    hero.rotation = Quaternion.Slerp(hero.rotation, Quaternion.LookRotation(move), dt * 12);
                if (DemoInput.GetKeyDown(KeyCode.F1)) SetMode(Mode == "Baseline" ? "Gameplay" : "Baseline");
                if (DemoInput.GetKeyDown(KeyCode.F2)) SetMode(stress ? "Gameplay" : "Stress");
                if (DemoInput.GetKeyDown(KeyCode.F3)) benchmark.ExportManual();
                if (DemoInput.GetKeyDown(KeyCode.F4)) SelectCold(!cold);
                if (Mode != "Baseline" && (DemoInput.GetMouseButtonDown(0) || DemoInput.GetKeyDown(KeyCode.Space))) TryAttack();
                if (stress) TryAttack();
            }
            else controller.Move(Vector3.down * 9 * dt);

            if (swing >= 0)
            {
                float previousSwing = swing; swing += dt;
                if (!struck) SweepBlade(previousSwing, swing);
                ApplyBladePose(swing);
                trail.emitting = swing >= Windup && swing <= ActiveEnd;
                previousTip = Tip();
                if (swing >= AttackDuration)
                {
                    swing = -1; lastAttackEnd = Time.time; trail.emitting = false; blade.localRotation = GuardRotation;
                    if (queued) { queued = false; BeginAttack(); }
                }
            }
            UpdateTarget(dt);
            flash = Mathf.Max(0, flash - dt);
            impactLight.intensity = 350 * Mathf.Clamp01(flash / .065f);
            PeakParticles = Mathf.Max(PeakParticles, sparks.particleCount);
        }

        public Vector3 Tip() { return blade.TransformPoint(new Vector3(0, 0, 1.20f)); }
        public void TryAttack()
        {
            if (Mode == "Baseline") return;
            // Only late presses are buffered; early spam does not skip the anticipation.
            if (swing >= 0) { if (swing >= .27f) queued = true; return; }
            BeginAttack();
        }
        void BeginAttack()
        {
            if (!automated) hero.rotation = Quaternion.Euler(0, yaw, 0);
            ComboStep = Time.time - lastAttackEnd > 1.05f ? 0 : (ComboStep + 1) % 3;
            ComboStepsStarted |= 1 << ComboStep;
            attackStart = blade.localRotation;
            swing = 0;
            struck = false;
            trail.Clear();
            previousTip = Tip();
            TotalAttacks++;
        }
        Quaternion WindRotation()
        {
            return ComboStep == 0 ? Quaternion.Euler(-48, -62, -38) : ComboStep == 1 ? Quaternion.Euler(35, 58, 32) : Quaternion.Euler(-98, 4, -6);
        }
        Quaternion FollowRotation()
        {
            return ComboStep == 0 ? Quaternion.Euler(42, 62, -18) : ComboStep == 1 ? Quaternion.Euler(-38, -60, 28) : Quaternion.Euler(66, 4, 8);
        }
        void ApplyBladePose(float time)
        {
            if (time < Windup) blade.localRotation = Quaternion.Slerp(attackStart, WindRotation(), Mathf.SmoothStep(0, 1, time / Windup));
            else if (time < ActiveEnd)
            {
                float t = (time - Windup) / (ActiveEnd - Windup);
                // Pass every cut through the same real striking plane before follow-through.
                Quaternion contact = Quaternion.Euler(-2, 0, ComboStep == 0 ? -28 : ComboStep == 1 ? 28 : 0);
                blade.localRotation = t < .5f ? Quaternion.Slerp(WindRotation(), contact, Mathf.SmoothStep(0, 1, t * 2)) : Quaternion.Slerp(contact, FollowRotation(), Mathf.SmoothStep(0, 1, t * 2 - 1));
            }
            else blade.localRotation = Quaternion.Slerp(FollowRotation(), GuardRotation, Mathf.SmoothStep(0, 1, (time - ActiveEnd) / (AttackDuration - ActiveEnd)));
        }
        void SweepBlade(float from, float to)
        {
            float begin = Mathf.Max(from, Windup), end = Mathf.Min(to, ActiveEnd);
            if (end < begin) return;
            int steps = Mathf.Clamp(Mathf.CeilToInt((end - begin) / .006f), 1, 48);
            ApplyBladePose(begin); previousTip = Tip();
            for (int i = 0; i <= steps && !struck; i++)
            {
                ApplyBladePose(Mathf.Lerp(begin, end, i / (float)steps));
                Vector3 tip = Tip(); CheckBlade(tip); previousTip = tip;
            }
        }
        void CheckBlade(Vector3 tip)
        {
            Collider candidate = null;
            int count = Physics.OverlapCapsuleNonAlloc(blade.position, tip, .065f, overlap, TargetMask, QueryTriggerInteraction.Ignore);
            if (count > 0)
            {
                candidate = overlap[0]; float nearest = (candidate.ClosestPoint(blade.position) - blade.position).sqrMagnitude;
                for (int i = 1; i < count; i++) { float distance = (overlap[i].ClosestPoint(blade.position) - blade.position).sqrMagnitude; if (distance < nearest) { candidate = overlap[i]; nearest = distance; } }
            }
            Vector3 travel = tip - previousTip;
            if (candidate == null && travel.sqrMagnitude > .00001f)
            {
                int n = Physics.SphereCastNonAlloc(previousTip, .065f, travel.normalized, sweptHits, travel.magnitude, TargetMask, QueryTriggerInteraction.Ignore);
                float nearest = float.PositiveInfinity;
                for (int i = 0; i < n; i++) if (sweptHits[i].distance < nearest) { candidate = sweptHits[i].collider; nearest = sweptHits[i].distance; }
            }
            if (candidate == null) return;
            struck = true;
            Vector3 point, normal;
            Vector3 edge = tip - blade.position;
            if (candidate.Raycast(new Ray(blade.position, edge.normalized), out RaycastHit contact, edge.magnitude + .07f)) { point = contact.point; normal = contact.normal; }
            else { Vector3 outside = blade.position - hero.forward * .25f; point = candidate.ClosestPoint(outside); normal = outside - point; }
            if (normal.sqrMagnitude < .0001f) normal = -hero.forward;
            Impact(point, normal.normalized);
        }
        void Impact(Vector3 point, Vector3 normal)
        {
            TotalHits++;
            ComboStepsHit |= 1 << ComboStep;
            reaction = 0;
            sparks.transform.SetPositionAndRotation(point, Quaternion.LookRotation(normal));
            sparks.Emit(stress ? 96 : 18);
            impactLight.transform.position = point + normal * .12f;
            flash = .065f;
        }
        void UpdateTarget(float dt)
        {
            if (reaction < 0) return;
            reaction += dt;
            if (reaction < .10f) { TargetState = "HIT"; targetRenderer.sharedMaterial = hitMaterial; }
            else if (reaction < .55f) { TargetState = "RECOVERING"; targetRenderer.sharedMaterial = recoveryMaterial != null ? recoveryMaterial : readyMaterial; }
            else if (reaction < .80f) { TargetState = "RESTORED"; targetRenderer.sharedMaterial = restoredMaterial != null ? restoredMaterial : readyMaterial; }
            else { TargetState = "READY"; targetRenderer.sharedMaterial = readyMaterial; reaction = -1; }
            float strength = Mathf.Clamp01(1 - reaction / .55f);
            target.localRotation = targetRest * Quaternion.Euler(Mathf.Sin(reaction * 26) * strength * 6, 0, 0);
        }
        public void SetMode(string value)
        {
            Mode = value; stress = value == "Stress"; queued = false;
            if (benchmark != null && !automated) benchmark.ResetManual();
        }
        public void SelectCold(bool value)
        {
            if (cold == value) return;
            cold = value;
            Material effect = cold ? coldEffect : warmEffect;
            if (effect != null) { trail.sharedMaterial = (cold ? coldTrail : warmTrail) != null ? (cold ? coldTrail : warmTrail) : effect; sparks.GetComponent<ParticleSystemRenderer>().sharedMaterial = effect; }
        }
        void LateUpdate()
        {
            cameraRig.rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 focus = hero.position + Vector3.up * 1.4f;
            Vector3 offset = -cameraRig.forward * 4.6f + cameraRig.right * 1.8f;
            Vector3 position = focus + offset;
            if (Physics.SphereCast(focus, .16f, offset.normalized, out RaycastHit hit, offset.magnitude, 1, QueryTriggerInteraction.Ignore)) position = hit.point + hit.normal * .2f;
            cameraRig.position = position;
        }
    }
}
