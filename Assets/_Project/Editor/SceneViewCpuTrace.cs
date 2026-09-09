using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;

namespace Bellerophon.Editor
{
    // Explicit, bounded editor CPU tracing. No scene changes or automatic startup capture.
    internal static class SceneViewCpuTrace
    {
        private const string Root = "docs/validation/scene_view_performance_2026-09-07";
        private static bool armed, running, savedEditor, savedCpu, savedBinary;
        private static string savedLog, directory;
        private static double started;
        private static int firstFrame;
        // Arm until real, foreground Scene navigation so user response timing cannot miss capture.
        private static SceneView observedView;
        private static Vector3 lastPivot;
        private static Quaternion lastRotation;
        private static float lastSize;
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        private static readonly StringBuilder gui = new StringBuilder();
        private static bool updateProbe;
        private static int probePhase;
        private static int lastProbeFrame;
        private static readonly StringBuilder probeFrames = new StringBuilder();
        private static readonly Dictionary<MonoBehaviour, bool> savedPreview = new Dictionary<MonoBehaviour, bool>();
        private static FieldInfo previewField;

        internal static void Begin()
        {
            if (armed || running) throw new InvalidOperationException("CPU trace already armed or running.");
            if (ProfilerDriver.enabled || Profiler.enableBinaryLog)
                throw new InvalidOperationException("An existing profiler recording is active; it will not be interrupted.");
            directory = Root + "/cpu_" + DateTime.Now.ToString("HHmmss");
            Directory.CreateDirectory(directory);
            observedView = SceneView.lastActiveSceneView;
            if (observedView != null) RememberCamera(observedView);
            gui.Clear(); gui.AppendLine("editor_time_s,event,application_active,scene_focused,pivot,rotation");
            armed = true;
            SceneView.duringSceneGui += ObserveGui;
            EditorApplication.update += Tick;
            AssemblyReloadEvents.beforeAssemblyReload += End;
            Status("armed; waiting for actual foreground Scene navigation; directory=" + directory);
        }

        private static void StartRecording()
        {
            if (ProfilerDriver.enabled || Profiler.enableBinaryLog)
            {
                End();
                Status("cancelled: another profiler recording became active; left untouched.");
                return;
            }
            savedEditor = ProfilerDriver.profileEditor;
            savedCpu = ProfilerDriver.IsAreaEnabled(ProfilerArea.CPU);
            savedBinary = Profiler.enableBinaryLog;
            savedLog = Profiler.logFile;
            firstFrame = ProfilerDriver.lastFrameIndex;
            File.WriteAllText(directory + "/initial_state.txt", "UTC=" + DateTime.UtcNow.ToString("O") +
                "\nprofileEditor=" + savedEditor + " cpuArea=" + savedCpu + " binary=" + savedBinary +
                "\nconnection=" + ProfilerDriver.GetConnectionIdentifier(ProfilerDriver.connectedProfiler) +
                "\nfirstFrame=" + firstFrame + " deepProfiling=" + ProfilerDriver.deepProfiling, Encoding.UTF8);
            ProfilerDriver.profileEditor = true;
            ProfilerDriver.SetAreaEnabled(ProfilerArea.CPU, true);
            Profiler.logFile = Path.GetFullPath(directory + "/stream.raw");
            Profiler.enableBinaryLog = true;
            started = EditorApplication.timeSinceStartup;
            armed = false;
            running = true;
            ProfilerDriver.enabled = true;
            Status("recording actual foreground Scene navigation; timeout 30s; directory=" + directory);
            if (updateProbe)
            {
                probePhase = 0; lastProbeFrame = firstFrame;
                probeFrames.Clear(); probeFrames.AppendLine("phase,frame,frame_start_ms,frame_ms,flush_ms");
                File.WriteAllText(directory + "/probe_phases.csv", "phase,editor_time_s,last_frame\n0," +
                    started.ToString("F6", Invariant) + "," + firstFrame + "\n");
                Status("recording actual-navigation A/B/A; 30s; " + directory);
            }
        }

        private static void ObserveGui(SceneView scene)
        {
            if (armed)
            {
                bool cameraChanged = observedView == scene && (scene.pivot != lastPivot ||
                    Quaternion.Angle(scene.rotation, lastRotation) > 0.001f ||
                    Mathf.Abs(scene.size - lastSize) > 0.0001f);
                bool navigation = Event.current.rawType == EventType.MouseDrag ||
                    Event.current.rawType == EventType.ScrollWheel || cameraChanged;
                observedView = scene;
                RememberCamera(scene);
                if (InternalEditorUtility.isApplicationActive && EditorWindow.focusedWindow == scene &&
                    !EditorApplication.isPlaying && navigation) StartRecording();
            }
            if (!running) return;
            gui.Append(EditorApplication.timeSinceStartup.ToString("F6", Invariant)).Append(',')
                .Append(Event.current.rawType).Append(',')
                .Append(InternalEditorUtility.isApplicationActive).Append(',')
                .Append(EditorWindow.focusedWindow == scene).Append(',')
                .Append('"').Append(scene.pivot.ToString("F6")).Append("\",\"")
                .Append(scene.rotation.ToString("F6")).AppendLine("\"");
        }
        private static void RememberCamera(SceneView scene)
        {
            lastPivot = scene.pivot; lastRotation = scene.rotation; lastSize = scene.size;
        }
        private static void Tick()
        {
            if (running && updateProbe)
            {
                double elapsed = EditorApplication.timeSinceStartup - started;
                CollectProbeFrames();
                int nextPhase = (int)(elapsed / 10);
                if (nextPhase >= 3) { End(); return; }
                if (nextPhase != probePhase)
                {
                    probePhase = nextPhase;
                    foreach (var pair in savedPreview)
                        if (pair.Key != null) previewField.SetValue(pair.Key, probePhase == 1 || pair.Value);
                    File.AppendAllText(directory + "/probe_phases.csv", probePhase + "," +
                        EditorApplication.timeSinceStartup.ToString("F6", Invariant) + "," + ProfilerDriver.lastFrameIndex + "\n");
                }
                return;
            }
            if (running && EditorApplication.timeSinceStartup - started >= 30) End();
        }
        internal static void BeginUpdateProbe()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit mode only.");
            Begin();
            try
            {
                savedPreview.Clear();
                foreach (var behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                {
                    if (!behaviour.isActiveAndEnabled || behaviour.GetType().Name != "BatonElectricVfxUnitySample") continue;
                    previewField = behaviour.GetType().GetField("manualPreview", BindingFlags.Instance | BindingFlags.NonPublic);
                    savedPreview.Add(behaviour, (bool)previewField.GetValue(behaviour));
                }
                updateProbe = true;
                File.WriteAllText(directory + "/probe_description.txt", "30s actual-navigation A/B/A: 0=original, 1=temporarily hold existing VFX preview (private nonserialized flag only), 2=original restored.\nActive VFX=" + savedPreview.Count + "\nCamera, selection, serialized settings, meshes and assets unchanged.\n");
                Status("armed actual-navigation A/B/A; waiting for foreground Scene movement; " + directory);
            }
            catch { End(); throw; }
        }
        private static void CollectProbeFrames()
        {
            int end = ProfilerDriver.lastFrameIndex;
            for (int frame = Math.Max(lastProbeFrame + 1, ProfilerDriver.firstFrameIndex); frame <= end; frame++)
            {
                using (var data = ProfilerDriver.GetRawFrameDataView(frame, 0))
                {
                    if (!data.valid) continue;
                    double flush = 0;
                    for (int sample = 0; sample < data.sampleCount;)
                    {
                        string name = data.GetSampleName(sample);
                        if (name == "FlushDirty") flush += data.GetSampleTimeMs(sample);
                        bool descend = name == "Main Thread" || name == "EditorLoop" ||
                            name == "Application.Tick" || name == "Application.UpdateScene" ||
                            name == "UpdateSceneIfNeeded" || name == "UpdateScene";
                        sample += descend ? 1 : data.GetSampleChildrenCountRecursive(sample) + 1;
                    }
                    if (flush > 0)
                        probeFrames.AppendLine(probePhase + "," + frame + "," + F(data.frameStartTimeMs) + "," + F(data.frameTimeMs) + "," + F(flush));
                }
            }
            lastProbeFrame = end;
        }
        internal static void End()
        {
            if (!armed && !running) return;
            bool hadRecording = running;
            if (running && updateProbe)
            {
                CollectProbeFrames();
                File.WriteAllText(directory + "/probe_frames.csv", probeFrames.ToString(), Encoding.UTF8);
            }
            armed = false;
            running = false;
            EditorApplication.update -= Tick;
            SceneView.duringSceneGui -= ObserveGui;
            AssemblyReloadEvents.beforeAssemblyReload -= End;
            observedView = null;
            updateProbe = false;
            foreach (var pair in savedPreview)
                if (pair.Key != null) previewField.SetValue(pair.Key, pair.Value);
            savedPreview.Clear();
            if (!hadRecording)
            {
                Status("cancelled while armed; profiler state unchanged; " + directory);
                return;
            }
            try
            {
                ProfilerDriver.enabled = false;
                Profiler.enableBinaryLog = false;
                ProfilerDriver.SaveProfile(Path.GetFullPath(directory + "/recent.data"));
                File.WriteAllText(directory + "/gui.csv", gui.ToString(), Encoding.UTF8);
                Export();
                Status("completed; " + directory);
            }
            catch (Exception exception)
            {
                Status("failed export; binary trace retained; " + directory + "\n" + exception);
                Debug.LogException(exception);
            }
            finally
            {
                ProfilerDriver.enabled = false;
                Profiler.enableBinaryLog = savedBinary;
                Profiler.logFile = savedLog;
                ProfilerDriver.profileEditor = savedEditor;
                ProfilerDriver.SetAreaEnabled(ProfilerArea.CPU, savedCpu);
            }
        }

        private sealed class Aggregate
        {
            internal int Count;
            internal double Total, Self, Max;
        }
        internal static void FinishInvestigation()
        {
            End();
            int first = ProfilerDriver.firstFrameIndex, last = ProfilerDriver.lastFrameIndex;
            // The initial capture started with an empty history (-1). Only release this
            // investigation's final retained range; never clear a newer user recording.
            bool ownHistory = !ProfilerDriver.enabled && first >= 11287 && last <= 13288;
            if (ownHistory) ProfilerDriver.ClearAllFrames();
            var report = new StringBuilder("UTC=" + DateTime.UtcNow.ToString("O") + "\n");
            report.AppendLine("armed=" + armed + " running=" + running + " updateProbe=" + updateProbe + " temporaryPreviewFlags=" + savedPreview.Count);
            report.AppendLine("profilerEnabled=" + ProfilerDriver.enabled + " profileEditor=" + ProfilerDriver.profileEditor +
                " cpuArea=" + ProfilerDriver.IsAreaEnabled(ProfilerArea.CPU) + " binary=" + Profiler.enableBinaryLog);
            report.AppendLine("releasedOwnProfilerHistory=" + ownHistory + " previousRange=" + first + ".." + last +
                " currentRange=" + ProfilerDriver.firstFrameIndex + ".." + ProfilerDriver.lastFrameIndex);
            foreach (var behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (!behaviour.isActiveAndEnabled || behaviour.GetType().Name != "BatonElectricVfxUnitySample") continue;
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                report.AppendLine(behaviour.name + " manualPreview=" + behaviour.GetType().GetField("manualPreview", flags).GetValue(behaviour) +
                    " runtimeTime=" + behaviour.GetType().GetField("runtimeTime", flags).GetValue(behaviour));
            }
            File.WriteAllText(Root + "/final_state.txt", report.ToString(), Encoding.UTF8);
        }
        private static void Export()
        {
            var totals = new Dictionary<string, Aggregate>();
            var frameCsv = new StringBuilder("frame,thread,frame_start_ms,frame_ms,samples,scene_render_ms\n");
            int start = Math.Max(firstFrame + 1, ProfilerDriver.firstFrameIndex);
            int end = ProfilerDriver.lastFrameIndex;
            using (var output = new StreamWriter(directory + "/cpu_samples.csv", false, Encoding.UTF8))
            {
                output.WriteLine("frame,thread,frame_ms,index,start_ms,duration_ms,self_ms,depth,path");
                for (int frame = start; frame <= end; frame++)
                {
                    for (int thread = 0; thread < 64; thread++)
                    {
                        using (RawFrameDataView data = ProfilerDriver.GetRawFrameDataView(frame, thread))
                        {
                            if (!data.valid) break;
                            if (data.threadName != "Main Thread" && !data.threadName.Contains("Render") && !data.threadName.Contains("Gfx")) continue;
                            var names = new List<string>(); var ends = new List<int>();
                            double sceneTime = 0;
                            for (int index = 0; index < data.sampleCount; index++)
                            {
                                while (ends.Count > 0 && ends[ends.Count - 1] < index) { ends.RemoveAt(ends.Count - 1); names.RemoveAt(names.Count - 1); }
                                string name = data.GetSampleName(index);
                                int descendants = data.GetSampleChildrenCountRecursive(index);
                                double duration = data.GetSampleTimeMs(index);
                                if (name == "SceneView.Render") sceneTime += duration;
                                double childrenTime = 0;
                                for (int child = index + 1; child <= index + descendants; child += data.GetSampleChildrenCountRecursive(child) + 1)
                                    childrenTime += data.GetSampleTimeMs(child);
                                double self = Math.Max(0, duration - childrenTime);
                                string path = string.Join("/", names.Concat(new[] { name }));
                                if (duration >= 0.05 || names.Count < 2)
                                {
                                    output.WriteLine(frame + ",\"" + Escape(data.threadName) + "\"," + F(data.frameTimeMs) + "," + index + "," + F(data.GetSampleStartTimeMs(index)) + "," + F(duration) + "," + F(self) + "," + names.Count + ",\"" + Escape(path) + "\"");
                                    string key = data.threadName + "/" + path;
                                    if (!totals.TryGetValue(key, out Aggregate aggregate)) totals[key] = aggregate = new Aggregate();
                                    aggregate.Count++; aggregate.Total += duration; aggregate.Self += self; aggregate.Max = Math.Max(aggregate.Max, duration);
                                }
                                if (descendants > 0) { names.Add(name); ends.Add(index + descendants); }
                            }
                            frameCsv.AppendLine(frame + ",\"" + Escape(data.threadName) + "\"," + F(data.frameStartTimeMs) + "," + F(data.frameTimeMs) + "," + data.sampleCount + "," + F(sceneTime));
                        }
                    }
                }
            }
            File.WriteAllText(directory + "/frames.csv", frameCsv.ToString(), Encoding.UTF8);
            var summary = new StringBuilder("frame range=" + start + ".." + end + "\nTop self-time paths (inclusive times overlap; do not sum):\n");
            foreach (var pair in totals.OrderByDescending(p => p.Value.Self).Take(60))
                summary.AppendLine("self_total_ms=" + F(pair.Value.Self) + " inclusive_total_ms=" + F(pair.Value.Total) + " max_ms=" + F(pair.Value.Max) + " calls=" + pair.Value.Count + " " + pair.Key);
            File.WriteAllText(directory + "/summary.txt", summary.ToString(), Encoding.UTF8);
        }
        private static string F(double value) => value.ToString("F6", Invariant);
        private static string Escape(string value) => value.Replace("\"", "\"\"");
        private static void Status(string text) => File.WriteAllText(Root + "/cpu_trace_status.txt", DateTime.UtcNow.ToString("O") + " " + text, Encoding.UTF8);
    }
}
