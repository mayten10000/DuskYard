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

        const int TargetMask = 1 << 8;
        readonly Collider[] overlap = new Collider[8];
        readonly RaycastHit[] sweptHits = new RaycastHit[8];
        CharacterController controller;
        Quaternion targetRest;
        Vector3 previousTip;
        float yaw, pitch = 18, swing = -1, cooldown, reaction = -1, flash;
        bool struck, cold;
        string hud = "Gameplay";
        BenchmarkRunner benchmark;

        void Start()
        {
            controller = hero.GetComponent<CharacterController>();
            targetRest = target.localRotation;
            blade.localRotation = Quaternion.Euler(-5, -55, -8);
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
            cooldown = Mathf.Max(0, cooldown - dt);
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
                if (swing <= .42f) ApplyBladePose(swing);
                else blade.localRotation = Quaternion.Slerp(Quaternion.Euler(-5, 70, -8), Quaternion.Euler(-5, -55, -8), Mathf.SmoothStep(0, 1, (swing - .42f) / .18f));
                trail.emitting = swing >= .10f && swing <= .33f;
                previousTip = Tip();
                if (swing >= .60f) { swing = -1; trail.emitting = false; blade.localRotation = Quaternion.Euler(-5, -55, -8); }
            }
            UpdateTarget(dt);
            flash = Mathf.Max(0, flash - dt);
            impactLight.intensity = 350 * Mathf.Clamp01(flash / .065f);
            PeakParticles = Mathf.Max(PeakParticles, sparks.particleCount);
        }

        public Vector3 Tip() { return blade.TransformPoint(new Vector3(0, 0, 1.20f)); }
        public void TryAttack()
        {
            if (cooldown > 0 || swing >= 0 || Mode == "Baseline") return;
            if (!automated) hero.rotation = Quaternion.Euler(0, yaw, 0);
            cooldown = stress ? .66f : .85f;
            swing = 0;
            struck = false;
            blade.localRotation = Quaternion.Euler(-5, -70, -8);
            previousTip = Tip();
            TotalAttacks++;
        }
        void ApplyBladePose(float time)
        {
            float turn = Mathf.SmoothStep(0, 1, Mathf.Clamp01(time / .42f));
            blade.localRotation = Quaternion.Euler(-5, Mathf.Lerp(-70, 70, turn), -8);
        }
        void SweepBlade(float from, float to)
        {
            float begin = Mathf.Max(from, .10f), end = Mathf.Min(to, .34f);
            if (end < begin) return;
            int steps = Mathf.Clamp(Mathf.CeilToInt((end - begin) / .012f), 1, 24);
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
                for (int i = 0; i < n; i++) if (candidate == null || sweptHits[i].distance < sweptHits[0].distance) candidate = sweptHits[i].collider;
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
            Mode = value; stress = value == "Stress"; hud = value;
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
