using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor
{
    // On-demand diagnostics only: no scene, material, quality or animation changes.
    internal static class SceneViewPerformanceDiagnostics
    {
        private const string DirectoryPath = "docs/validation/scene_view_performance_2026-09-07";
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        private static SceneView view;
        private static EditorWindow previousWindow;
        private static Vector3 savedPivot;
        private static Quaternion savedRotation;
        private static float savedSize;
        private static bool savedOrtho;
        private static double started, lastUpdate, lastRepaint, paintStart;
        private static int phase = -1;
        private static int lastObservedPhase = -1;
        private static string runDirectory;
        private static readonly StringBuilder observations = new StringBuilder();
        private static readonly List<Sample> samples = new List<Sample>();
        private static readonly List<Counter> counters = new List<Counter>();
        private static readonly string[] Phases = { "warmup", "background", "foreground_stationary", "foreground_navigation", "foreground_other_window" };
        // Passive capture distinguishes real user navigation from an artificial repaint loop.
        private static Vector3 lastPivot;
        private static Quaternion lastRotation;
        private static float lastSize;
        private static double lastNavigation;
        // Track actual navigation independently of whether editor update callbacks run.
        private static double navigationStarted;
        private static readonly List<GuiEventSample> guiEvents = new List<GuiEventSample>();
        private static EventType guiEventType;
        private static double guiEventStart;
        private sealed class GuiEventSample
        {
            internal string Type;
            internal double Elapsed, Milliseconds, RepaintIntervalMs;
            internal int Phase;
            internal long[] Values;
        }
        private sealed class Counter { internal string Name, Unit; internal ProfilerRecorder Recorder; }
        private sealed class Sample
        {
            internal int Phase;
            internal double Elapsed, UpdateMs, RepaintMs, GuiMs;
            internal long[] Values;
            internal int Batches, DrawCalls, Triangles, Vertices;
        }

        internal static void Inspect()
        {
            Directory.CreateDirectory(DirectoryPath);
            Scene scene = SceneManager.GetActiveScene();
            SceneView active = SceneView.lastActiveSceneView;
            StringBuilder report = new StringBuilder();
            report.AppendLine("UTC=" + DateTime.UtcNow.ToString("O"));
            report.AppendLine("scene=" + scene.path + " dirty=" + scene.isDirty + " playing=" + EditorApplication.isPlaying);
            report.AppendLine("editorApplicationActive=" + UnityEditorInternal.InternalEditorUtility.isApplicationActive + " editorUpdating=" + EditorApplication.isUpdating + " compiling=" + EditorApplication.isCompiling);
            System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
            report.AppendLine("windowHandle=" + process.MainWindowHandle + " title=" + process.MainWindowTitle);
            report.AppendLine("GPU=" + SystemInfo.graphicsDeviceName + " VRAM_MB=" + SystemInfo.graphicsMemorySize + " API=" + SystemInfo.graphicsDeviceType);
            report.AppendLine("CPU=" + SystemInfo.processorType + " threads=" + SystemInfo.processorCount);
            report.AppendLine("Quality=" + QualitySettings.names[QualitySettings.GetQualityLevel()] + " textureStreaming=" + QualitySettings.streamingMipmapsActive);
            report.AppendLine("vSyncCount=" + QualitySettings.vSyncCount + " applicationTargetFrameRate=" + Application.targetFrameRate + " recorderRunning=" + (view != null));
            StringBuilder pacing = new StringBuilder();
            foreach (Type type in typeof(EditorWindow).Assembly.GetTypes().Where(t =>
                t.Name.IndexOf("InteractionMode", StringComparison.OrdinalIgnoreCase) >= 0 ||
                t.Name == "EditorApplication" || t.Name == "SceneViewMotion" || t.Name == "PreferencesWindow" || t.Name == "PreferencesProvider"))
            {
                pacing.AppendLine("TYPE " + type.FullName);
                if (type.IsEnum)
                {
                    pacing.AppendLine(" VALUES " + string.Join(",", Enum.GetValues(type).Cast<object>().Select(v => v + "=" + Convert.ToInt32(v))));
                    if (type.FullName == "UnityEditor.PreferencesProvider+InteractionMode" && EditorPrefs.HasKey("InteractionMode"))
                        pacing.AppendLine(" CURRENT " + Enum.GetName(type, EditorPrefs.GetInt("InteractionMode")));
                }
                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                {
                    string lower = field.Name.ToLowerInvariant();
                    if (!type.Name.Contains("InteractionMode") && !new[] { "frame", "idle", "sleep", "thrott", "interaction", "update", "fps", "speed" }.Any(lower.Contains)) continue;
                    pacing.Append(" FIELD " + field.FieldType.FullName + " " + field.Name);
                    if (field.IsStatic && (field.FieldType.IsPrimitive || field.FieldType.IsEnum || field.FieldType == typeof(string)))
                    {
                        try { pacing.Append(" = " + field.GetValue(null)); } catch (Exception ex) { pacing.Append(" unreadable=" + ex.GetType().Name); }
                    }
                    pacing.AppendLine();
                }
                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                    if (type.Name.Contains("InteractionMode") || new[] { "frame", "idle", "sleep", "thrott", "interaction", "fps", "speed" }.Any(property.Name.ToLowerInvariant().Contains))
                        pacing.AppendLine(" PROPERTY " + property.PropertyType.FullName + " " + property.Name);
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
                    if (type.Name.Contains("InteractionMode") || new[] { "idle", "sleep", "thrott", "interaction", "framerate" }.Any(method.Name.ToLowerInvariant().Contains))
                        pacing.AppendLine(" METHOD " + method);
            }
            // Read preference key literals from the loaded editor's own General-preferences methods.
            foreach (Type type in typeof(EditorWindow).Assembly.GetTypes().Where(t => t.FullName.StartsWith("UnityEditor.PreferencesProvider", StringComparison.Ordinal)))
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(m => m.Name.IndexOf("General", StringComparison.OrdinalIgnoreCase) >= 0 || m.Name.IndexOf("Interaction", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    pacing.AppendLine("GENERAL METHOD " + type.FullName + "." + method.Name);
                    byte[] code = method.GetMethodBody()?.GetILAsByteArray();
                    if (code == null) continue;
                    for (int i = 0; i + 4 < code.Length; i++)
                    {
                        if (code[i] != 0x72 || code[i + 4] != 0x70) continue;
                        string literal;
                        try { literal = method.Module.ResolveString(BitConverter.ToInt32(code, i + 1)); } catch { continue; }
                        if (!new[] { "interaction", "frame", "idle", "sleep", "thrott" }.Any(literal.ToLowerInvariant().Contains)) continue;
                        pacing.AppendLine(" LITERAL " + literal);
                        if (EditorPrefs.HasKey(literal)) pacing.AppendLine("  EXISTING PREF int=" + EditorPrefs.GetInt(literal, int.MinValue) + " float=" + EditorPrefs.GetFloat(literal, float.NaN) + " string=" + EditorPrefs.GetString(literal, "<not-string>"));
                    }
                }
            File.WriteAllText(DirectoryPath + "/pacing_state_members.txt", pacing.ToString(), Encoding.UTF8);
            report.AppendLine("TotalAllocatedMB=" + MB(Profiler.GetTotalAllocatedMemoryLong()) + " reservedMB=" + MB(Profiler.GetTotalReservedMemoryLong()));
            Type logEntries = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries");
            MethodInfo getCounts = logEntries?.GetMethod("GetCountsByType", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (getCounts != null)
            {
                object[] counts = { 0, 0, 0 };
                getCounts.Invoke(null, counts);
                report.AppendLine("Console errors=" + counts[0] + " warnings=" + counts[1] + " logs=" + counts[2]);
            }
            if (active != null)
            {
                report.AppendLine("SceneView=" + active.position + " pivot=" + active.pivot + " size=" + active.size + " rotation=" + active.rotation);
                report.AppendLine("camera=" + active.camera.pixelWidth + "x" + active.camera.pixelHeight + " near=" + active.camera.nearClipPlane + " far=" + active.camera.farClipPlane);
                report.AppendLine("SceneLighting=" + active.sceneLighting + " Gizmos=" + active.drawGizmos + " mode=" + active.cameraMode.name + " Focus=" + EditorWindow.focusedWindow);
            }
            Renderer[] renderers = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)).ToArray();
            Renderer[] enabled = renderers.Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray();
            Plane[] planes = active != null ? GeometryUtility.CalculateFrustumPlanes(active.camera) : null;
            report.AppendLine("Renderers total=" + renderers.Length + " active=" + enabled.Length + " Skinned=" + enabled.OfType<SkinnedMeshRenderer>().Count());
            Animator[] animators = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Animator>(true)).ToArray();
            report.AppendLine("Animators total=" + animators.Length + " active=" + animators.Count(a => a.enabled && a.gameObject.activeInHierarchy));
            foreach (var group in animators.Where(a => a.enabled && a.gameObject.activeInHierarchy).GroupBy(a => a.cullingMode))
                report.AppendLine("  animatorCulling=" + group.Key + " count=" + group.Count());
            report.AppendLine("frustum counts are diagnostic candidates only; SceneView temporarily modifies camera matrices outside rendering, so these are not actual draw counts.");
            report.AppendLine("Enabled renderers grouped by top-level root:");
            foreach (var group in enabled.GroupBy(r => r.transform.root.name).OrderByDescending(g => g.Count()))
                report.AppendLine("  " + group.Key + " count=" + group.Count() + " frustum=" + group.Count(r => planes == null || GeometryUtility.TestPlanesAABB(planes, r.bounds)) + " triangles=" + group.Sum(Triangles));
            foreach (Renderer item in enabled.Where(r => r.name == "Dagger_RightHand"))
                report.AppendLine("Dagger instance=" + item.transform.parent.name + " meshTriangles=" + Triangles(item) + " materialIDs=" + string.Join(",", item.sharedMaterials.Select(m => m == null ? 0 : m.GetInstanceID())));
            Texture[] loaded = Resources.FindObjectsOfTypeAll<Texture>();
            report.AppendLine("Loaded texture objects=" + loaded.Length + " runtimeMB=" + MB(loaded.Sum(t => Profiler.GetRuntimeMemorySizeLong(t))));
            report.AppendLine("Loaded Dagger/ElectricBaton textures:");
            foreach (Texture texture in loaded.Where(t => AssetDatabase.GetAssetPath(t).Contains("/Items/Dagger/") || AssetDatabase.GetAssetPath(t).Contains("/Items/ElectricBaton/")))
                DescribeTexture(report, texture);
            report.AppendLine("Largest loaded textures:");
            foreach (Texture texture in loaded.OrderByDescending(t => Profiler.GetRuntimeMemorySizeLong(t)).Take(25)) DescribeTexture(report, texture);
            report.AppendLine("Enabled edit-mode behaviours:");
            foreach (var group in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true))
                .Where(b => b != null && b.isActiveAndEnabled && (Attribute.IsDefined(b.GetType(), typeof(ExecuteAlways)) || Attribute.IsDefined(b.GetType(), typeof(ExecuteInEditMode)))).GroupBy(b => b.GetType().FullName))
                report.AppendLine("  " + group.Key + "=" + group.Count());
            foreach (MonoBehaviour behaviour in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true))
                .Where(b => b != null && b.GetType().Name == "BatonElectricVfxUnitySample" && b.isActiveAndEnabled))
            {
                report.AppendLine("Baton VFX existing serialized state: " + behaviour.name);
                SerializedProperty property = new SerializedObject(behaviour).GetIterator();
                while (property.NextVisible(true))
                    if (property.propertyType == SerializedPropertyType.Boolean || property.propertyType == SerializedPropertyType.Float || property.propertyType == SerializedPropertyType.Integer || property.propertyType == SerializedPropertyType.Enum)
                        report.AppendLine("  " + property.propertyPath + "=" + (property.propertyType == SerializedPropertyType.Float ? property.floatValue.ToString(Invariant) : property.propertyType == SerializedPropertyType.Boolean ? property.boolValue.ToString() : property.intValue.ToString(Invariant)));
            }
            FieldInfo update = typeof(EditorApplication).GetField("update", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (update?.GetValue(null) is Delegate callbacks)
            {
                report.AppendLine("Editor update callbacks:");
                foreach (Delegate callback in callbacks.GetInvocationList())
                    report.AppendLine("  " + callback.Method.DeclaringType?.FullName + "." + callback.Method.Name);
            }
            List<ProfilerRecorderHandle> handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            List<string> markerNames = handles.Select(h => ProfilerRecorderHandle.GetDescription(h))
                .Where(d => SelectCounter(d.Name)).Select(d => d.Category.Name + "|" + d.Name + "|" + d.UnitType).ToList();
            File.WriteAllText(DirectoryPath + "/available_markers.txt", string.Join("\n", markerNames), Encoding.UTF8);
            File.WriteAllText(DirectoryPath + "/inspection.txt", report.ToString(), Encoding.UTF8);
            Debug.Log("SceneView performance inspection written: " + DirectoryPath);
        }

        private static long Triangles(Renderer renderer)
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh : (filter != null ? filter.sharedMesh : null);
            if (mesh == null) return 0;
            long count = 0;
            for (int i = 0; i < mesh.subMeshCount; i++) count += (long)mesh.GetIndexCount(i) / 3;
            return count;
        }
        private static string MB(long bytes) => (bytes / 1048576d).ToString("F2", Invariant);
        private static void DescribeTexture(StringBuilder report, Texture texture)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            report.AppendLine("  " + texture.name + " " + texture.width + "x" + texture.height + " format=" + texture.graphicsFormat + " MB=" + MB(Profiler.GetRuntimeMemorySizeLong(texture)) + " path=" + path +
                (importer == null ? "" : " max=" + importer.maxTextureSize + " compression=" + importer.textureCompression + " readable=" + importer.isReadable));
        }
        private static bool SelectCounter(string name)
        {
            return name == "Main Thread" || name == "Render Thread" || name == "EditorLoop" || name == "PlayerLoop" ||
                name.Contains("SceneView") || name == "Camera.Render" || name == "Gfx.WaitForPresentOnGfxThread" ||
                name == "Gfx.WaitForGfxCommandsFromMainThread" || name == "Gfx.PresentFrame" || name == "Gfx.ProcessCommands" ||
                name == "RenderPipelineManager.DoRenderLoop_Internal" || name == "GC.Alloc" || name.Contains("GC.Collect") ||
                name == "Batches Count" || name == "Draw Calls Count" || name == "SetPass Calls Count" ||
                name == "Triangles Count" || name == "GPU Frame Time" || name == "CPU Main Thread Frame Time" ||
                name == "CPU Render Thread Frame Time" || name == "Texture Memory" || name == "Gfx Used Memory" ||
                name == "Shadow Casters Count" || name == "RenderLoop.Draw" || name == "GC Allocated In Frame" ||
                name.StartsWith("Gfx.Wait", StringComparison.Ordinal) || name == "GUIView.Repaint" ||
                name == "EditorApplication.update" || name == "UniversalRenderPipeline.RenderSingleCameraInternal" ||
                name.Contains("Gizmo") || name.Contains("Picking") || name.Contains("Handles.") ||
                name.Contains("CullResults") || name.Contains("UniversalRenderPipeline") || name.Contains("RenderLoop") ||
                name == "RenderPipeline.Render" || name == "ScriptableRenderContext.Submit" || name.Contains("ShadowMaps");
        }

        internal static void Capture()
        {
            if (view != null) { Finish(); return; }
            view = SceneView.lastActiveSceneView;
            if (view == null) throw new InvalidOperationException("No existing SceneView is open.");
            Directory.CreateDirectory(DirectoryPath);
            runDirectory = DirectoryPath + "/run_" + DateTime.Now.ToString("HHmmss");
            Directory.CreateDirectory(runDirectory);
            observations.Clear(); lastObservedPhase = -1;
            previousWindow = EditorWindow.focusedWindow;
            savedPivot = view.pivot; savedRotation = view.rotation; savedSize = view.size; savedOrtho = view.orthographic;
            samples.Clear(); counters.Clear(); guiEvents.Clear();
            lastPivot = view.pivot; lastRotation = view.rotation; lastSize = view.size;
            lastNavigation = -10;
            navigationStarted = -1;
            List<ProfilerRecorderHandle> handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            foreach (ProfilerRecorderHandle handle in handles)
            {
                ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handle);
                if (!SelectCounter(description.Name)) continue;
                ProfilerRecorder recorder = ProfilerRecorder.StartNew(description.Category, description.Name, 1,
                    ProfilerRecorderOptions.Default | ProfilerRecorderOptions.SumAllSamplesInFrame);
                if (recorder.Valid) counters.Add(new Counter { Name = description.Name, Unit = description.UnitType.ToString(), Recorder = recorder });
            }
            started = lastUpdate = EditorApplication.timeSinceStartup;
            lastRepaint = 0; phase = -1;
            SceneView.beforeSceneGui += BeforeGui;
            SceneView.duringSceneGui += DuringGui;
            EditorApplication.update += Tick;
            AssemblyReloadEvents.beforeAssemblyReload += Abort;
            File.WriteAllText(DirectoryPath + "/status.txt", "recording actual GUI events for up to 180s; invoke capture again to finish early; " + DateTime.UtcNow.ToString("O"));
            Debug.Log("Passive SceneView recording started for up to 180s, including initial movement. Camera and focus are untouched. Invoke CaptureSceneViewPerformance again to finish early.");
        }

        private static void Tick()
        {
            // EditorApplication may already hold an invocation-list snapshot when a bridge request stops recording.
            if (view == null) return;
            try
            {
                double now = EditorApplication.timeSinceStartup;
                double elapsed = now - started;
                if (elapsed >= 180) { Finish(); return; }
                if (view.pivot != lastPivot || Quaternion.Angle(view.rotation, lastRotation) > 0.0001f || view.size != lastSize)
                {
                    lastNavigation = now;
                    if (navigationStarted < 0 && UnityEditorInternal.InternalEditorUtility.isApplicationActive && EditorWindow.focusedWindow == view)
                    {
                        navigationStarted = now;
                        observations.AppendLine("First actual foreground navigation elapsed=" + elapsed.ToString("F3", Invariant));
                    }
                }
                lastPivot = view.pivot; lastRotation = view.rotation; lastSize = view.size;
                int nextPhase = !UnityEditorInternal.InternalEditorUtility.isApplicationActive ? 1 :
                    EditorWindow.focusedWindow != view ? 4 : now - lastNavigation < 0.15 ? 3 : 2;
                if (nextPhase != phase)
                {
                    phase = nextPhase; lastRepaint = 0;
                    File.WriteAllText(DirectoryPath + "/status.txt", (navigationStarted < 0 ? "waiting for foreground navigation; " : "recording actual navigation; ") + Phases[phase] + " " + elapsed.ToString("F2", Invariant));
                }
                Sample sample = new Sample { Phase = phase, Elapsed = elapsed, UpdateMs = (now - lastUpdate) * 1000,
                    Values = counters.Select(c => c.Recorder.LastValue).ToArray(), Batches = UnityStats.batches,
                    DrawCalls = UnityStats.drawCalls, Triangles = UnityStats.triangles, Vertices = UnityStats.vertices };
                samples.Add(sample); lastUpdate = now;
            }
            catch (Exception exception)
            {
                Restore();
                File.WriteAllText(DirectoryPath + "/status.txt", "failed " + exception);
                Debug.LogException(exception);
            }
        }
        private static void BeforeGui(SceneView sceneView)
        {
            if (sceneView != view) return;
            guiEventType = Event.current.rawType;
            guiEventStart = EditorApplication.timeSinceStartup;
            // GUI renders may occur repeatedly without EditorApplication.update in between.
            if (view.pivot != lastPivot || Quaternion.Angle(view.rotation, lastRotation) > 0.0001f || view.size != lastSize)
            {
                lastNavigation = guiEventStart;
                if (navigationStarted < 0 && UnityEditorInternal.InternalEditorUtility.isApplicationActive && EditorWindow.focusedWindow == view)
                {
                    navigationStarted = guiEventStart;
                    observations.AppendLine("First actual foreground GUI navigation elapsed=" + (guiEventStart - started).ToString("F3", Invariant));
                }
            }
            lastPivot = view.pivot; lastRotation = view.rotation; lastSize = view.size;
            if (Event.current.type == EventType.Repaint) paintStart = guiEventStart;
        }
        private static void DuringGui(SceneView sceneView)
        {
            if (sceneView != view || samples.Count == 0) return;
            double now = EditorApplication.timeSinceStartup;
            int eventPhase = !UnityEditorInternal.InternalEditorUtility.isApplicationActive ? 1 : EditorWindow.focusedWindow != view ? 4 : now - lastNavigation < 0.15 ? 3 : 2;
            bool repaint = Event.current.type == EventType.Repaint;
            guiEvents.Add(new GuiEventSample { Type = guiEventType.ToString(), Elapsed = now - started, Milliseconds = (now - guiEventStart) * 1000, Phase = eventPhase,
                RepaintIntervalMs = repaint && lastRepaint > 0 ? (now - lastRepaint) * 1000 : 0,
                Values = repaint ? counters.Select(c => c.Recorder.LastValue).ToArray() : null });
            if (Event.current.type != EventType.Repaint) return;
            if (phase != lastObservedPhase)
            {
                lastObservedPhase = phase;
                Camera camera = sceneView.camera;
                observations.AppendLine(Phases[phase] + " applicationActive=" + UnityEditorInternal.InternalEditorUtility.isApplicationActive +
                    " focused=" + EditorWindow.focusedWindow + " position=" + camera.transform.position + " pixels=" + camera.pixelWidth + "x" + camera.pixelHeight +
                    " selected=" + Selection.activeObject + " gizmos=" + sceneView.drawGizmos + " pivot=" + sceneView.pivot + " size=" + sceneView.size);
            }
            Sample sample = samples[samples.Count - 1];
            sample.GuiMs = (now - paintStart) * 1000;
            sample.RepaintMs = lastRepaint == 0 ? 0 : (now - lastRepaint) * 1000;
            lastRepaint = now;
        }
        private static void Finish()
        {
            Restore();
            StringBuilder csv = new StringBuilder("phase,elapsed_s,editor_update_ms,scene_repaint_ms,before_to_during_gui_ms,batches,drawCalls,triangles,vertices");
            foreach (Counter counter in counters) csv.Append(",\"" + counter.Name + " [" + counter.Unit + "]\"");
            csv.AppendLine();
            foreach (Sample sample in samples)
            {
                csv.Append(Phases[sample.Phase] + "," + sample.Elapsed.ToString("F5", Invariant) + "," + sample.UpdateMs.ToString("F5", Invariant) + "," + sample.RepaintMs.ToString("F5", Invariant) + "," + sample.GuiMs.ToString("F5", Invariant) + "," + sample.Batches + "," + sample.DrawCalls + "," + sample.Triangles + "," + sample.Vertices);
                foreach (long value in sample.Values) csv.Append("," + value);
                csv.AppendLine();
            }
            File.WriteAllText(DirectoryPath + "/samples.csv", csv.ToString(), Encoding.UTF8);
            File.WriteAllText(runDirectory + "/samples.csv", csv.ToString(), Encoding.UTF8);
            StringBuilder summary = new StringBuilder();
            for (int p = 1; p < Phases.Length; p++)
            {
                Sample[] rows = samples.Where(s => s.Phase == p).ToArray();
                summary.AppendLine(Phases[p] + " samples=" + rows.Length);
                Summary(summary, "editor_update_ms", rows.Select(s => s.UpdateMs));
                Summary(summary, "scene_repaint_ms", rows.Where(s => s.RepaintMs > 0).Select(s => s.RepaintMs));
                Summary(summary, "scene_gui_ms", rows.Where(s => s.GuiMs > 0).Select(s => s.GuiMs));
                Summary(summary, "drawcalls", rows.Select(s => (double)s.DrawCalls));
                Summary(summary, "triangles", rows.Select(s => (double)s.Triangles));
                for (int i = 0; i < counters.Count; i++)
                {
                    int index = i;
                    double divisor = counters[i].Unit == "TimeNanoseconds" ? 1000000d : 1d;
                    Summary(summary, counters[i].Name + (divisor == 1 ? " [" + counters[i].Unit + "]" : " [ms]"), rows.Select(s => s.Values[index] / divisor));
                }
            }
            File.WriteAllText(DirectoryPath + "/summary.txt", summary.ToString(), Encoding.UTF8);
            File.WriteAllText(runDirectory + "/summary.txt", summary.ToString(), Encoding.UTF8);
            File.WriteAllText(runDirectory + "/observations.txt", observations.ToString(), Encoding.UTF8);
            StringBuilder eventSummary = new StringBuilder();
            StringBuilder eventCsv = new StringBuilder("phase,type,elapsed_s,before_to_during_gui_ms,repaint_interval_ms");
            foreach (Counter counter in counters) eventCsv.Append(",\"" + counter.Name + " [" + counter.Unit + "]\"");
            eventCsv.AppendLine();
            foreach (GuiEventSample entry in guiEvents)
            {
                eventCsv.Append(Phases[entry.Phase] + "," + entry.Type + "," + entry.Elapsed.ToString("F6", Invariant) + "," + entry.Milliseconds.ToString("F6", Invariant) + "," + entry.RepaintIntervalMs.ToString("F6", Invariant));
                foreach (long value in entry.Values ?? new long[counters.Count]) eventCsv.Append("," + value);
                eventCsv.AppendLine();
            }
            File.WriteAllText(runDirectory + "/gui_events.csv", eventCsv.ToString(), Encoding.UTF8);
            foreach (var group in guiEvents.GroupBy(e => Phases[e.Phase] + "/" + e.Type))
            {
                eventSummary.AppendLine(group.Key + " count=" + group.Count());
                Summary(eventSummary, "before_to_during_gui_ms", group.Select(e => e.Milliseconds));
                Summary(eventSummary, "repaint_interval_ms", group.Where(e => e.RepaintIntervalMs > 0).Select(e => e.RepaintIntervalMs));
            }
            File.WriteAllText(runDirectory + "/gui_events_summary.txt", eventSummary.ToString(), Encoding.UTF8);
            File.WriteAllText(DirectoryPath + "/status.txt", (navigationStarted < 0 ? "finished without foreground navigation; " : "completed actual navigation capture; ") + "user camera/focus left untouched; scene not saved; " + runDirectory + "; " + DateTime.UtcNow.ToString("O"));
            Debug.Log("SceneView performance capture completed: " + DirectoryPath);
        }
        private static void Summary(StringBuilder output, string label, IEnumerable<double> sequence)
        {
            double[] values = sequence.OrderBy(x => x).ToArray();
            if (values.Length == 0) { output.AppendLine("  " + label + " unavailable"); return; }
            output.AppendLine("  " + label + " mean=" + values.Average().ToString("F3", Invariant) + " median=" + values[values.Length / 2].ToString("F3", Invariant) + " p95=" + values[Math.Min(values.Length - 1, (int)(values.Length * 0.95))].ToString("F3", Invariant) + " max=" + values.Last().ToString("F3", Invariant));
        }
        private static void Abort()
        {
            Restore();
            File.WriteAllText(DirectoryPath + "/status.txt", "passive capture aborted for assembly reload; camera not changed");
        }
        private static void Restore()
        {
            EditorApplication.update -= Tick;
            SceneView.beforeSceneGui -= BeforeGui;
            SceneView.duringSceneGui -= DuringGui;
            AssemblyReloadEvents.beforeAssemblyReload -= Abort;
            foreach (Counter counter in counters) counter.Recorder.Dispose();
            view = null;
        }
    }
}
