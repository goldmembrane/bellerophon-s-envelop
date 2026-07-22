using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Dolore05ExecutionOpening
{
    [InitializeOnLoad]
    internal static class Dolore05ExecutionOpeningPlayModeCapture
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Dolore Enemy Placement";
        private const string AttackSlotName = "Dolore_04_Tentacle_Stab_Attack";
        private const string ExecutionSlotName = "Dolore_05_Execution_Pull_In";
        private const string ModelName = "Dolore_Model";
        private const string AttachmentName = "Dolore_Attack_Attachment";
        private const string StateFileName = "Dolore05ExecutionOpeningCapture.state";
        private const string ResultFileName = "Dolore05ExecutionOpeningCapture.result";
        private const string StateEnteringPlayMode = "EnteringPlayMode";
        private const string StateExitingPlayMode = "ExitingPlayMode";
        private const string StateFailedExitingPlayMode = "FailedExitingPlayMode";
        private const int CaptureWidth = 1280;
        private const int CaptureHeight = 720;
        private const int CaptureLayer = 31;

        private static Action<string> complete;
        private static Action<Exception> fail;

        static Dolore05ExecutionOpeningPlayModeCapture()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        public static void Start(Action<string> completeCallback, Action<Exception> failCallback)
        {
            complete = completeCallback;
            fail = failCallback;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Cannot start the execution capture while Unity is entering Play Mode.");
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("Current active scene must be CargoRunMvp.");
            if (scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp must be clean before the execution capture.");

            DeleteStateFiles();
            var captureDir = Path.Combine(
                ProjectRoot,
                "Assets",
                "_Project",
                "Art",
                "Generated",
                "Enemies",
                "Dolore",
                "AttackAttachment",
                "Review",
                "Dolore_05_ExecutionPullIn_Opening_Diagnostic");
            Directory.CreateDirectory(captureDir);
            foreach (var file in Directory.GetFiles(captureDir, "*.png"))
            {
                File.Delete(file);
                TryDelete(file + ".meta");
            }
            WriteState(new CaptureState
            {
                Phase = StateEnteringPlayMode,
                CaptureDir = captureDir,
                StartedUtcTicks = DateTime.UtcNow.Ticks
            });
            EditorApplication.EnterPlaymode();
        }

        public static void Resume(Action<string> completeCallback, Action<Exception> failCallback)
        {
            complete = completeCallback;
            fail = failCallback;
            Tick();
        }

        private static void Tick()
        {
            if (complete == null && fail == null) return;
            var state = ReadState();
            if (state == null) return;
            try
            {
                if (state.Phase == StateEnteringPlayMode)
                {
                    if (!EditorApplication.isPlaying) return;
                    CaptureCurrentPlayModeFrames(state);
                    state.Phase = StateExitingPlayMode;
                    WriteState(state);
                    EditorApplication.ExitPlaymode();
                    return;
                }
                if (state.Phase == StateExitingPlayMode)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    CompleteFromState(state);
                    return;
                }
                if (state.Phase == StateFailedExitingPlayMode && !EditorApplication.isPlayingOrWillChangePlaymode)
                    FailFromState(state);
            }
            catch (Exception exception)
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    state.Phase = StateFailedExitingPlayMode;
                    state.Error = exception.ToString();
                    WriteState(state);
                    if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
                }
                else
                {
                    var callback = fail;
                    CleanupCallbacks();
                    DeleteStateFiles();
                    callback?.Invoke(exception);
                }
            }
        }

        private static void CaptureCurrentPlayModeFrames(CaptureState state)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("Play Mode active scene must stay CargoRunMvp.");
            var placement = scene.GetRootGameObjects().SingleOrDefault(item => item.name == PlacementRootName) ??
                            throw new InvalidOperationException("Approved Dolore placement root is missing.");
            var slot = placement.transform.Find(ExecutionSlotName) ??
                       throw new InvalidOperationException("Dolore motion 4 execution slot is missing.");
            var attackSlot = placement.transform.Find(AttackSlotName) ??
                             throw new InvalidOperationException("Dolore motion 3 attack slot is missing.");
            var model = slot.Find(ModelName) ?? throw new InvalidOperationException("Execution model is missing.");
            var attachment = model.Find(AttachmentName) ??
                             throw new InvalidOperationException("Execution attachment is missing.");
            var animator = attachment.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
                throw new InvalidOperationException("Execution opening Animator is not configured.");
            var renderer = attachment.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault(item => item.sharedMesh != null && item.bones.Length == 13) ??
                           throw new InvalidOperationException("Execution 13-bone renderer is missing.");
            var tip = renderer.bones.Single(item => item.name == "Bone_001");
            var fixedAnchor = renderer.bones.Single(item => item.name == "Bone_010");
            var attackAttachment = attackSlot.Find(ModelName)?.Find(AttachmentName) ??
                                   throw new InvalidOperationException("Attack attachment is missing.");
            var attackRenderer = attackAttachment.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.sharedMesh != null && item.bones.Length == 13);
            var attackAnchor = attackRenderer.bones.Single(item => item.name == "Bone_010");
            var player = scene.GetRootGameObjects().SingleOrDefault(item => item.name == "Player") ??
                         throw new InvalidOperationException("Player root is missing.");
            var outward = Vector3.ProjectOnPlane(player.transform.position - attackAnchor.position, Vector3.up).normalized;
            if (outward.sqrMagnitude < 0.9f)
                throw new InvalidOperationException("The approved frame outward direction is unavailable.");
            var lateral = Vector3.Cross(Vector3.up, outward).normalized;

            var siblingStates = HideOtherSlots(placement.transform, slot);
            var layerStates = SetLayerRecursively(slot, CaptureLayer);
            var skinnedStates = slot.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Select(item => new SkinnedState(
                    item,
                    item.updateWhenOffscreen,
                    item.forceMatrixRecalculationPerRender,
                    item.localBounds))
                .ToArray();
            var slotWasActive = slot.gameObject.activeSelf;
            slot.gameObject.SetActive(true);
            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = false;
            foreach (var skinnedState in skinnedStates)
            {
                skinnedState.Renderer.updateWhenOffscreen = true;
                skinnedState.Renderer.forceMatrixRecalculationPerRender = true;
            }

            var cameraObject = new GameObject("Dolore Execution Opening Camera") { hideFlags = HideFlags.DontSave };
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.cullingMask = 1 << CaptureLayer;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.045f, 0.045f, 0.055f, 1f);
            camera.fieldOfView = 35f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            var lightObject = new GameObject("Dolore Execution Opening Light") { hideFlags = HideFlags.DontSave };
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2.5f;
            light.color = new Color(1f, 0.94f, 0.88f, 1f);
            light.cullingMask = 1 << CaptureLayer;
            lightObject.transform.rotation = Quaternion.LookRotation((-outward + Vector3.down * 0.45f).normalized);

            var captures = new List<string>();
            var metrics = new PoseMetrics();
            try
            {
                CapturePose(camera, slot, animator, renderer, "Intro", 0f, "00_intro_hidden", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, "Intro", 0.25f / 2f, "01_ring_generating", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, "Intro", 0.5f / 2f, "02_ring_ready", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, "Intro", 0.8f / 2f, "03_tip_passing_ring", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, "Intro", 1.05f / 2f, "04_tip_front_clear", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, "Intro", 1.18f / 2f, "05_diagonal_exit", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, "Intro", 1.30f / 2f, "06_early_rise", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, "Intro", 1.55f / 2f, "07_mid_rise", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, "Intro", 1.78f / 2f, "08_full_chain_clear", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, "Intro", 1f, "09_intro_prepared", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, "PierceHold", 0f, "10_pierce_prepared", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, "PierceHold", 0.28f / 0.58f, "11_loaded", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, "PierceHold", 0.38f / 0.58f, "12_acceleration", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, "PierceHold", 0.44f / 0.58f, "13_near_impact", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, "PierceHold", 0.5f / 0.58f, "14_pierce_impact", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, "PierceHold", 1f, "15_pierce_hold", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
            }
            finally
            {
                slot.gameObject.SetActive(slotWasActive);
                foreach (var skinnedState in skinnedStates)
                {
                    if (skinnedState.Renderer == null) continue;
                    skinnedState.Renderer.updateWhenOffscreen = skinnedState.UpdateWhenOffscreen;
                    skinnedState.Renderer.forceMatrixRecalculationPerRender = skinnedState.ForceMatrixRecalculation;
                    skinnedState.Renderer.localBounds = skinnedState.LocalBounds;
                }
                RestoreLayers(layerStates);
                RestoreOtherSlots(siblingStates);
                UnityEngine.Object.Destroy(lightObject);
                UnityEngine.Object.Destroy(cameraObject);
            }

            var introRise = metrics.IntroPreparedTip.y - metrics.IntroStartTip.y;
            var preparedError = Vector3.Distance(metrics.IntroPreparedTip, metrics.PiercePreparedTip);
            var windupLift = metrics.LoadedTip.y - metrics.PiercePreparedTip.y;
            var windupRetreat = Vector3.Dot(metrics.PiercePreparedTip - metrics.LoadedTip, outward);
            var lateOutward = Vector3.Dot(metrics.ImpactTip - metrics.AccelerationTip, outward);
            var strikeDelta = metrics.ImpactTip - metrics.PiercePreparedTip;
            var strikeForward = Vector3.Dot(strikeDelta, outward);
            var strikeDrop = metrics.PiercePreparedTip.y - metrics.ImpactTip.y;
            var strikeLateral = Mathf.Abs(Vector3.Dot(strikeDelta, lateral));
            var holdError = Vector3.Distance(metrics.ImpactTip, metrics.HoldTip);
            if (introRise < 0.9f || preparedError > 0.001f || windupLift < 0.1f || windupRetreat < 0.05f ||
                lateOutward < 0.35f || strikeForward < 1f || strikeDrop < 1f || strikeLateral > 0.001f ||
                holdError > 0.001f || metrics.MaximumAnchorDrift > 0.002f || metrics.IntroHiddenVisible ||
                !metrics.SourceRevealVisible || !metrics.PierceVisible || captures.Count != 48)
                throw new InvalidOperationException(
                    "Actual execution Animator metrics failed. IntroRise=" + Num(introRise) +
                    " PreparedError=" + Num(preparedError) + " WindupLift=" + Num(windupLift) +
                    " WindupRetreat=" + Num(windupRetreat) + " LateOutward=" + Num(lateOutward) +
                    " StrikeForward=" + Num(strikeForward) + " StrikeDrop=" + Num(strikeDrop) +
                    " StrikeLateral=" + Num(strikeLateral) + " HoldError=" + Num(holdError) +
                    " AnchorDrift=" + Num(metrics.MaximumAnchorDrift) + " Images=" + captures.Count);

            state.Summary = "ActualAnimator=True Images=" + captures.Count +
                " IntroRise=" + Num(introRise) + " PreparedError=" + Num(preparedError) +
                " WindupLift=" + Num(windupLift) + " WindupRetreat=" + Num(windupRetreat) +
                " LateOutward=" + Num(lateOutward) + " StrikeForward=" + Num(strikeForward) +
                " StrikeDrop=" + Num(strikeDrop) + " StrikeLateral=" + Num(strikeLateral) +
                " HoldError=" + Num(holdError) + " FixedAnchorDrift=" + Num(metrics.MaximumAnchorDrift) +
                " IntroHiddenVisible=" + metrics.IntroHiddenVisible +
                " SourceRevealVisible=" + metrics.SourceRevealVisible +
                " PierceVisible=" + metrics.PierceVisible + " CaptureDir=" + state.CaptureDir;
            File.WriteAllText(ResultPath, state.Summary);
            Debug.Log("Dolore05ExecutionOpeningActualAnimatorCaptured Result=PASS " + state.Summary);
        }

        private static void CapturePose(
            Camera camera,
            Transform slot,
            Animator animator,
            SkinnedMeshRenderer renderer,
            string stateName,
            float normalizedTime,
            string label,
            string captureDir,
            Vector3 outward,
            Vector3 lateral,
            Transform tip,
            Transform fixedAnchor,
            List<string> captures,
            ref PoseMetrics metrics)
        {
            animator.speed = 1f;
            animator.Play(stateName, 0, normalizedTime);
            animator.Update(0f);
            animator.speed = 0f;
            RecordMetrics(label, tip.position, fixedAnchor.position, renderer.enabled, ref metrics);
            CaptureView(camera, slot, label + "_front", captureDir, captures, outward);
            CaptureView(camera, slot, label + "_side", captureDir, captures, lateral);
            CaptureView(camera, slot, label + "_front3q", captureDir, captures, (outward + lateral * 0.55f).normalized);
        }

        private static void RecordMetrics(
            string label,
            Vector3 tip,
            Vector3 anchor,
            bool rendererVisible,
            ref PoseMetrics metrics)
        {
            if (label == "02_ring_ready")
            {
                metrics.HasAnchor = true;
                metrics.Anchor = anchor;
            }
            if (metrics.HasAnchor)
                metrics.MaximumAnchorDrift = Mathf.Max(
                    metrics.MaximumAnchorDrift,
                    Vector3.Distance(metrics.Anchor, anchor));
            if (label == "00_intro_hidden")
            {
                metrics.IntroStartTip = tip;
                metrics.IntroHiddenVisible = rendererVisible;
            }
            else if (label == "01_ring_generating") metrics.SourceRevealVisible = rendererVisible;
            else if (label == "09_intro_prepared") metrics.IntroPreparedTip = tip;
            else if (label == "10_pierce_prepared")
            {
                metrics.PiercePreparedTip = tip;
                metrics.PierceVisible = rendererVisible;
            }
            else if (label == "11_loaded") metrics.LoadedTip = tip;
            else if (label == "12_acceleration") metrics.AccelerationTip = tip;
            else if (label == "14_pierce_impact") metrics.ImpactTip = tip;
            else if (label == "15_pierce_hold") metrics.HoldTip = tip;
        }

        private static void CaptureView(
            Camera camera,
            Transform target,
            string label,
            string captureDir,
            List<string> captures,
            Vector3 viewDirection)
        {
            var bounds = CalculateBounds(target);
            var size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z, 0.5f);
            var center = bounds.center + Vector3.up * size * 0.06f;
            camera.transform.position = center + viewDirection.normalized * size * 2.55f + Vector3.up * size * 0.18f;
            camera.transform.LookAt(center);
            var renderTexture = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                var texture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
                texture.Apply();
                var path = Path.Combine(captureDir, label + ".png");
                File.WriteAllBytes(path, texture.EncodeToPNG());
                captures.Add(path);
                UnityEngine.Object.DestroyImmediate(texture);
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static Bounds CalculateBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true).Where(item => item.enabled).ToArray();
            if (renderers.Length == 0) return new Bounds(root.position, Vector3.one);
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private static ActiveState[] HideOtherSlots(Transform placement, Transform target)
        {
            var states = new List<ActiveState>();
            for (var index = 0; index < placement.childCount; index++)
            {
                var child = placement.GetChild(index);
                if (child == target) continue;
                states.Add(new ActiveState(child.gameObject, child.gameObject.activeSelf));
                child.gameObject.SetActive(false);
            }
            return states.ToArray();
        }

        private static LayerState[] SetLayerRecursively(Transform root, int layer)
        {
            var states = root.GetComponentsInChildren<Transform>(true)
                .Select(item => new LayerState(item.gameObject, item.gameObject.layer))
                .ToArray();
            foreach (var state in states) state.GameObject.layer = layer;
            return states;
        }

        private static void RestoreOtherSlots(IEnumerable<ActiveState> states)
        {
            foreach (var state in states) if (state.GameObject != null) state.GameObject.SetActive(state.Value);
        }

        private static void RestoreLayers(IEnumerable<LayerState> states)
        {
            foreach (var state in states) if (state.GameObject != null) state.GameObject.layer = state.Value;
        }

        private static void CompleteFromState(CaptureState state)
        {
            var summary = File.Exists(ResultPath) ? File.ReadAllText(ResultPath) : state.Summary;
            var callback = complete;
            CleanupCallbacks();
            DeleteStateFiles();
            AssetDatabase.Refresh();
            callback?.Invoke("Dolore motion 4 actual Animator execution opening capture completed. " + summary);
        }

        private static void FailFromState(CaptureState state)
        {
            var callback = fail;
            var error = string.IsNullOrWhiteSpace(state.Error) ? "Execution opening capture failed." : state.Error;
            CleanupCallbacks();
            DeleteStateFiles();
            callback?.Invoke(new InvalidOperationException(error));
        }

        private static void CleanupCallbacks()
        {
            complete = null;
            fail = null;
        }

        private static void DeleteStateFiles()
        {
            TryDelete(StatePath);
            TryDelete(ResultPath);
        }

        private static CaptureState ReadState()
        {
            if (!File.Exists(StatePath)) return null;
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadAllLines(StatePath))
            {
                var split = line.IndexOf('=');
                if (split >= 0) values[line.Substring(0, split)] = line.Substring(split + 1);
            }
            return new CaptureState
            {
                Phase = Get(values, "phase"),
                CaptureDir = Get(values, "captureDir"),
                Summary = Get(values, "summary"),
                Error = Get(values, "error"),
                StartedUtcTicks = long.TryParse(Get(values, "startedUtcTicks"), out var ticks) ? ticks : 0L
            };
        }

        private static void WriteState(CaptureState state)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StatePath));
            File.WriteAllLines(StatePath, new[]
            {
                "phase=" + state.Phase,
                "captureDir=" + state.CaptureDir,
                "summary=" + state.Summary,
                "error=" + state.Error,
                "startedUtcTicks=" + state.StartedUtcTicks.ToString(CultureInfo.InvariantCulture)
            });
        }

        private static string Get(IDictionary<string, string> values, string key) =>
            values.TryGetValue(key, out var value) ? value : string.Empty;

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static string Num(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);
        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string StatePath => Path.Combine(ProjectRoot, "Logs", StateFileName);
        private static string ResultPath => Path.Combine(ProjectRoot, "Logs", ResultFileName);

        private sealed class CaptureState
        {
            public string Phase;
            public string CaptureDir;
            public string Summary;
            public string Error;
            public long StartedUtcTicks;
        }

        private struct PoseMetrics
        {
            public bool HasAnchor;
            public Vector3 Anchor;
            public float MaximumAnchorDrift;
            public bool IntroHiddenVisible;
            public bool SourceRevealVisible;
            public bool PierceVisible;
            public Vector3 IntroStartTip;
            public Vector3 IntroPreparedTip;
            public Vector3 PiercePreparedTip;
            public Vector3 LoadedTip;
            public Vector3 AccelerationTip;
            public Vector3 ImpactTip;
            public Vector3 HoldTip;
        }

        private readonly struct ActiveState
        {
            public ActiveState(GameObject gameObject, bool value)
            {
                GameObject = gameObject;
                Value = value;
            }

            public GameObject GameObject { get; }
            public bool Value { get; }
        }

        private readonly struct LayerState
        {
            public LayerState(GameObject gameObject, int value)
            {
                GameObject = gameObject;
                Value = value;
            }

            public GameObject GameObject { get; }
            public int Value { get; }
        }

        private readonly struct SkinnedState
        {
            public SkinnedState(
                SkinnedMeshRenderer renderer,
                bool updateWhenOffscreen,
                bool forceMatrixRecalculation,
                Bounds localBounds)
            {
                Renderer = renderer;
                UpdateWhenOffscreen = updateWhenOffscreen;
                ForceMatrixRecalculation = forceMatrixRecalculation;
                LocalBounds = localBounds;
            }

            public SkinnedMeshRenderer Renderer { get; }
            public bool UpdateWhenOffscreen { get; }
            public bool ForceMatrixRecalculation { get; }
            public Bounds LocalBounds { get; }
        }
    }
}
