using System;
using System.Globalization;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace DuskYard
{
    public sealed class BenchmarkRunner : MonoBehaviour
    {
        [Serializable] public class Result
        {
            public string mode;
            public int observedTargetStateBits;
            public int frames, sampledFrames, attacks, hits, peakParticles, objectCountStart, objectCountEnd, gen0Collections;
            public float seconds, medianFPS, p95ms, p99ms;
            public long memoryStart, memoryEnd, memoryMinimum, memoryMaximum;
            public bool gcRecorderAvailable;
            public double averageGcBytesPerFrame;
        }
        [Serializable] public class Report
        {
            public string unity, cpu, gpu, graphicsApi, operatingSystem, quality, startedUtc;
            public int width, height, systemMemoryMB, gpuMemoryMB, vsync;
            public string note = "Standalone wall-clock frame times. 32768-frame bounded sample. GPU/driver/Editor costs are not interchangeable.";
            public Result[] results;
            public int runtimeErrors, runtimeWarnings, distantAttacks, distantHits;
            public int lightProbeCount, movingProbeSamples;
            public bool movingProbeValuesFinite = true;
            public float maximumAdjacentProbeLuminanceStep;
        }
        public CombatDemo demo;
        readonly float[] frameTimes = new float[32768];
        readonly Result[] results = new Result[3];
        ProfilerRecorder gc;
        int phase = -1, frames, errors, warnings, hitStart, attackStart, objectsStart, gcStart, stateBits;
        long memStart, memMin, memMax, gcBytes;
        float elapsed, measured, duration, memoryTimer, nextStrike;
        bool automatic, preview, measuring, screenshot, coldScreenshot, probeReset, distantReset;
        int distantAttackStart, distantHitStart;
        CharacterController controller;
        readonly Vector3[] probeDirections = { Vector3.up };
        readonly Color[] probeColors = new Color[1];
        float previousLuminance;
        string output;
        Report report;
        static readonly string[] Modes = { "Baseline", "Gameplay", "Stress" };

        void Start()
        {
            Application.runInBackground = true;
            controller = demo.hero.GetComponent<CharacterController>();
            output = Path.Combine(Application.persistentDataPath, "Measurements");
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-benchmark") automatic = true;
                if (args[i] == "-preview") { automatic = true; preview = true; }
                if (args[i] == "-output" && i + 1 < args.Length) output = args[++i];
            }
            gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            report = new Report { unity = Application.unityVersion, cpu = SystemInfo.processorType, gpu = SystemInfo.graphicsDeviceName,
                graphicsApi = SystemInfo.graphicsDeviceType.ToString(), operatingSystem = SystemInfo.operatingSystem,
                quality = QualitySettings.names[QualitySettings.GetQualityLevel()], startedUtc = DateTime.UtcNow.ToString("O"),
                width = Screen.width, height = Screen.height, systemMemoryMB = SystemInfo.systemMemorySize,
                gpuMemoryMB = SystemInfo.graphicsMemorySize, vsync = QualitySettings.vSyncCount, lightProbeCount = LightmapSettings.lightProbes == null ? 0 : LightmapSettings.lightProbes.count, results = results };
            Application.logMessageReceived += OnLog;
            if (automatic) { demo.automated = true; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; if (preview) { phase = 1; distantReset = true; demo.SetMode("Gameplay"); } else NextPhase(); }
            else BeginMeasurement();
        }
        void OnLog(string condition, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
            if (type == LogType.Warning) warnings++;
        }
        void Update()
        {
            if (!automatic) { Sample(); return; }
            if (phase < 0 || phase >= 3) return;
            elapsed += measuring ? Time.unscaledDeltaTime : Mathf.Min(.1f, Time.unscaledDeltaTime);
            if (phase == 1 && elapsed >= 1 && !distantReset)
            {
                report.distantAttacks = demo.TotalAttacks - distantAttackStart; report.distantHits = demo.TotalHits - distantHitStart;
                controller.Move(new Vector3(-demo.hero.position.x, 0, -2 - demo.hero.position.z)); distantReset = true;
            }
            if (phase > 0 && Time.unscaledTime >= nextStrike)
            {
                demo.TryAttack();
                nextStrike = Time.unscaledTime + (phase == 2 ? .05f : 1.25f);
            }
            float warmup = phase == 0 ? 12 : 6;
            if (phase == 0 && elapsed < 5)
            {
                float nextX = Mathf.Sin(elapsed * Mathf.PI * 2 / 5) * 3;
                controller.Move(new Vector3(nextX - demo.hero.position.x, 0, 0));
                LightProbes.GetInterpolatedProbe(demo.hero.position + Vector3.up * 1.2f, null, out var sh);
                sh.Evaluate(probeDirections, probeColors);
                float value = probeColors[0].grayscale;
                if (float.IsNaN(value) || float.IsInfinity(value)) report.movingProbeValuesFinite = false;
                if (report.movingProbeSamples > 0) report.maximumAdjacentProbeLuminanceStep = Mathf.Max(report.maximumAdjacentProbeLuminanceStep, Mathf.Abs(value - previousLuminance));
                previousLuminance = value; report.movingProbeSamples++;
            }
            if (phase == 0 && elapsed >= 5 && !probeReset) { controller.Move(new Vector3(-demo.hero.position.x, 0, -2 - demo.hero.position.z)); probeReset = true; }
            if (phase == 1 && elapsed > 3.25f && elapsed < 5.5f) demo.SelectCold(true);
            if (phase == 1 && !coldScreenshot && elapsed > 3.5f && demo.RecentImpact)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "ColdPreset.png")); coldScreenshot = true;
            }
            if (phase == 1 && elapsed >= 5.5f) demo.SelectCold(false);
            if (!screenshot && (phase == 0 ? elapsed >= 7 : elapsed >= 2 && demo.RecentImpact))
            {
                Directory.CreateDirectory(output);
                ScreenCapture.CaptureScreenshot(Path.Combine(output, Modes[phase] + ".png"));
                screenshot = true;
            }
            if (preview) { if (elapsed >= 7) Application.Quit(0); return; }
            if (!measuring && elapsed >= warmup) BeginMeasurement();
            if (measuring) { Sample(); if (measured >= duration) FinishPhase(); }
        }
        void Sample()
        {
            stateBits |= demo.TargetState == "READY" ? 1 : demo.TargetState == "HIT" ? 2 : demo.TargetState == "RECOVERING" ? 4 : 8;
            frameTimes[frames % frameTimes.Length] = Time.unscaledDeltaTime * 1000;
            frames++;
            measured += Time.unscaledDeltaTime;
            if (gc.Valid) gcBytes += gc.LastValue;
            memoryTimer += Time.unscaledDeltaTime;
            if (memoryTimer >= 1)
            {
                memoryTimer = 0;
                long current = Profiler.GetTotalAllocatedMemoryLong();
                memMin = Math.Min(memMin, current); memMax = Math.Max(memMax, current);
            }
        }
        void BeginMeasurement()
        {
            objectsStart = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;
            frames = 0; measured = 0; gcBytes = 0; memoryTimer = 0; stateBits = 0;
            memStart = memMin = memMax = Profiler.GetTotalAllocatedMemoryLong();
            gcStart = GC.CollectionCount(0); hitStart = demo.TotalHits; attackStart = demo.TotalAttacks;
            measuring = true;
        }
        Result GetResult(string mode)
        {
            long memoryEnd = Profiler.GetTotalAllocatedMemoryLong(); int gcEnd = GC.CollectionCount(0);
            int count = Math.Min(frames, frameTimes.Length);
            float[] sorted = new float[count];
            Array.Copy(frameTimes, sorted, count); Array.Sort(sorted);
            return new Result { mode = mode, observedTargetStateBits = stateBits, frames = frames, sampledFrames = count, seconds = measured,
                medianFPS = count == 0 ? 0 : 1000 / sorted[count / 2], p95ms = count == 0 ? 0 : sorted[Math.Min(count - 1, (int)(count * .95f))],
                p99ms = count == 0 ? 0 : sorted[Math.Min(count - 1, (int)(count * .99f))],
                memoryStart = memStart, memoryEnd = memoryEnd, memoryMinimum = memMin, memoryMaximum = memMax,
                gen0Collections = gcEnd - gcStart, attacks = demo.TotalAttacks - attackStart, hits = demo.TotalHits - hitStart,
                peakParticles = demo.PeakParticles, objectCountStart = objectsStart, gcRecorderAvailable = gc.Valid,
                averageGcBytesPerFrame = !gc.Valid || frames == 0 ? -1 : (double)gcBytes / frames,
                objectCountEnd = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length };
        }
        void FinishPhase()
        {
            measuring = false;
            Result result = GetResult(Modes[phase]);
            results[phase] = result;
            WriteFrames(Modes[phase]);
            Debug.Log("BENCHMARK " + Modes[phase] + " medianFPS=" + result.medianFPS.ToString("F2", CultureInfo.InvariantCulture) + " hits=" + result.hits);
            NextPhase();
        }
        void NextPhase()
        {
            phase++;
            if (phase >= 3)
            {
                report.runtimeErrors = errors; report.runtimeWarnings = warnings;
                Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "benchmark.json"), JsonUtility.ToJson(report, true));
                bool good = errors == 0 && results[1].hits > 0 && results[2].hits > 0 && report.distantAttacks > 0 && report.distantHits == 0 && report.movingProbeValuesFinite && report.lightProbeCount > 0;
                Debug.Log("BENCHMARK_COMPLETE " + output);
                Application.Quit(good ? 0 : 2);
                return;
            }
            demo.SetMode(Modes[phase]);
            if (phase == 1)
            {
                controller.Move(new Vector3(6 - demo.hero.position.x, 0, 0));
                distantAttackStart = demo.TotalAttacks; distantHitStart = demo.TotalHits;
            }
            elapsed = 0; measuring = false; screenshot = false; nextStrike = Time.unscaledTime;
            duration = phase == 2 ? 120 : 30;
        }
        void WriteFrames(string mode)
        {
            Directory.CreateDirectory(output);
            using (var writer = new StreamWriter(Path.Combine(output, mode + "-frames.csv")))
            {
                writer.WriteLine("frame_ms");
                int count = Math.Min(frames, frameTimes.Length);
                for (int i = 0; i < count; i++) writer.WriteLine(frameTimes[i].ToString("F4", CultureInfo.InvariantCulture));
            }
        }
        public void ResetManual() { if (!automatic) BeginMeasurement(); }
        public void ExportManual()
        {
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "manual-" + demo.Mode + ".json"), JsonUtility.ToJson(GetResult(demo.Mode), true));
            WriteFrames("manual-" + demo.Mode);
            ResetManual();
            Debug.Log("Manual benchmark exported: " + output);
        }
        void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
            gc.Dispose();
        }
    }
}
