using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Dolore04TentacleStabAnimation
{
    [InitializeOnLoad]
    internal static class Dolore04TentacleStabFullMotionPlayModeCapture
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Dolore Enemy Placement";
        private const string SlotName = "Dolore_04_Tentacle_Stab_Attack";
        private const string ModelName = "Dolore_Model";
        private const string AttachmentName = "Dolore_Attack_Attachment";
        private const string StateFileName = "Dolore04TentacleStabFullMotionCapture.state";
        private const string ResultFileName = "Dolore04TentacleStabFullMotionCapture.result";
        private const string StateEnteringPlayMode = "EnteringPlayMode";
        private const string StateExitingPlayMode = "ExitingPlayMode";
        private const string StateFailedExitingPlayMode = "FailedExitingPlayMode";
        private const int CaptureWidth = 1280;
        private const int CaptureHeight = 720;
        private const int CaptureLayer = 31;

        private static Action<string> complete;
        private static Action<Exception> fail;

        static Dolore04TentacleStabFullMotionPlayModeCapture()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        public static void Start(bool finalCapture, Action<string> completeCallback, Action<Exception> failCallback)
        {
            complete = completeCallback;
            fail = failCallback;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Cannot start the Dolore capture while Unity is entering or running Play Mode.");

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("Current active scene must be CargoRunMvp. ActiveScene=" + scene.path);
            if (scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp must be clean before the actual Animator capture.");

            DeleteStateFiles();
            var folderName = finalCapture
                ? "Dolore_04_TentacleStab_FullMotion_Final"
                : "Dolore_04_TentacleStab_FullMotion_Diagnostic";
            var captureDir = Path.Combine(ProjectRoot, "Assets", "_Project", "Art", "Generated", "Enemies", "Dolore",
                "AttackAttachment", "Review", folderName);
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
                FinalCapture = finalCapture,
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
                throw new InvalidOperationException("Play Mode active scene must stay CargoRunMvp. ActiveScene=" + scene.path);

            var placement = scene.GetRootGameObjects().SingleOrDefault(item => item.name == PlacementRootName) ??
                            throw new InvalidOperationException("Missing placement root: " + PlacementRootName);
            var slot = placement.transform.Find(SlotName) ??
                       throw new InvalidOperationException("Missing Dolore motion 3 slot: " + SlotName);
            var model = slot.Find(ModelName) ?? throw new InvalidOperationException("Missing " + ModelName + ".");
            var attachment = model.Find(AttachmentName) ?? throw new InvalidOperationException("Missing " + AttachmentName + ".");
            var animator = attachment.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
                throw new InvalidOperationException("The actual motion 3 attachment Animator is not configured.");
            var renderer = attachment.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault(item => item.sharedMesh != null && item.bones.Length == 13) ??
                           throw new InvalidOperationException("The approved 13-bone tentacle renderer is missing.");
            var tip = renderer.bones.Single(item => item.name == "Bone_001");
            var fixedAnchor = renderer.bones.Single(item => item.name == "Bone_010");
            var player = scene.GetRootGameObjects().SingleOrDefault(item => item.name == "Player") ??
                         throw new InvalidOperationException("The CargoRunMvp Player root is missing.");
            var outward = Vector3.ProjectOnPlane(player.transform.position - fixedAnchor.position, Vector3.up).normalized;
            if (outward.sqrMagnitude < 0.9f) throw new InvalidOperationException("The frame outward direction is unavailable.");
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
                skinnedState.Renderer.localBounds = skinnedState.LocalBounds;
            }

            var cameraObject = new GameObject("Dolore Full Motion Actual Animator Camera") { hideFlags = HideFlags.DontSave };
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.cullingMask = 1 << CaptureLayer;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.045f, 0.045f, 0.055f, 1f);
            camera.fieldOfView = 35f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            var lightObject = new GameObject("Dolore Full Motion Actual Animator Light") { hideFlags = HideFlags.DontSave };
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
                CapturePose(camera, slot, animator, renderer, skinnedStates, "Intro", 0f, "00_intro_hidden", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "Intro", 0.25f / 2f, "01_ring_generating", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "Intro", 0.5f / 2f, "02_ring_ready", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "Intro", 0.65f / 2f, "03_tip_entering_ring", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "Intro", 0.8f / 2f, "04_tip_passing_ring", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "Intro", 1.05f / 2f, "05_tip_front_clear", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "Intro", 1.18f / 2f, "06_diagonal_exit", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "Intro", 1.30f / 2f, "07_early_rise", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "Intro", 1.55f / 2f, "08_mid_rise", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "Intro", 1.78f / 2f, "09_full_chain_clear", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "Intro", 1f, "10_intro_prepared", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "AttackLoop", 0f, "11_loop_prepared", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "AttackLoop", 0.28f / 2.6f, "12_first_loaded", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "AttackLoop", 0.38f / 2.6f, "13_first_acceleration", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "AttackLoop", 0.5f / 2.6f, "14_first_downstrike", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "AttackLoop", 0.58f / 2.6f, "15_first_impact_hold", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "AttackLoop", 1.3f / 2.6f, "16_first_return", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "AttackLoop", 1.58f / 2.6f, "17_second_loaded", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "AttackLoop", 1.68f / 2.6f, "18_second_acceleration", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "AttackLoop", 1.8f / 2.6f, "19_second_downstrike", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "AttackLoop", 1.88f / 2.6f, "20_second_impact_hold", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "AttackLoop", 2.2f / 2.6f, "21_body_hiding_upward", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "AttackLoop", 2.475f / 2.6f, "22_ring_hiding_in_place", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "AttackLoop", 1f, "23_cycle_hidden", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "Intro", 0.25f / 2f, "24_repeat_ring_generating", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "Intro", 0.5f / 2f, "25_repeat_ring_ready", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
                CapturePose(camera, slot, animator, renderer, skinnedStates, "Intro", 0.8f / 2f, "26_repeat_tip_passing", state.CaptureDir, outward, lateral, tip, fixedAnchor, captures, ref metrics);
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
            var initialRiseDelta = metrics.InitialRiseTip - metrics.SourceReadyTip;
            var initialRiseUp = initialRiseDelta.y;
            var initialRiseLateral = Mathf.Abs(Vector3.Dot(initialRiseDelta, lateral));
            var firstStrikeDelta = metrics.FirstStrikeTip - metrics.LoopPreparedTip;
            var firstStrikeOutward = Vector3.Dot(firstStrikeDelta, outward);
            var firstStrikeLateral = Mathf.Abs(Vector3.Dot(firstStrikeDelta, lateral));
            var firstStrikeDrop = metrics.LoopPreparedTip.y - metrics.FirstStrikeTip.y;
            var firstReturnError = Vector3.Distance(metrics.FirstReturnTip, metrics.LoopPreparedTip);
            var secondStrikeDelta = metrics.SecondStrikeTip - metrics.FirstReturnTip;
            var secondStrikeOutward = Vector3.Dot(secondStrikeDelta, outward);
            var secondStrikeLateral = Mathf.Abs(Vector3.Dot(secondStrikeDelta, lateral));
            var secondStrikeDrop = metrics.FirstReturnTip.y - metrics.SecondStrikeTip.y;
            var hiddenReturnError = Vector3.Distance(metrics.HiddenTip, metrics.IntroStartTip);
            var anchorDrift = metrics.MaximumAnchorDrift;
            if (introRise < 0.25f || initialRiseUp < 0.03f || initialRiseLateral > 0.03f ||
                firstStrikeOutward < 0.12f || firstStrikeDrop < 0.20f ||
                firstStrikeLateral > 0.20f || firstReturnError > 0.01f ||
                secondStrikeOutward < 0.12f || secondStrikeDrop < 0.20f ||
                secondStrikeLateral > 0.20f || anchorDrift > 0.002f ||
                metrics.IntroHiddenVisible || !metrics.SourceRevealVisible ||
                metrics.CycleHiddenVisible || !metrics.RepeatRevealVisible)
                throw new InvalidOperationException("Actual Animator motion metrics failed. IntroRise=" + Num(introRise) +
                    " InitialRiseUp=" + Num(initialRiseUp) + " InitialRiseLateral=" + Num(initialRiseLateral) +
                    " FirstStrikeOutward=" + Num(firstStrikeOutward) + " FirstStrikeDrop=" + Num(firstStrikeDrop) +
                    " FirstStrikeLateral=" + Num(firstStrikeLateral) + " FirstReturnError=" + Num(firstReturnError) +
                    " SecondStrikeOutward=" + Num(secondStrikeOutward) + " SecondStrikeDrop=" + Num(secondStrikeDrop) +
                    " SecondStrikeLateral=" + Num(secondStrikeLateral) + " HiddenReturnError=" + Num(hiddenReturnError) +
                    " FixedAnchorDrift=" + Num(anchorDrift) +
                    " IntroHiddenVisible=" + metrics.IntroHiddenVisible +
                    " SourceRevealVisible=" + metrics.SourceRevealVisible +
                    " CycleHiddenVisible=" + metrics.CycleHiddenVisible +
                    " RepeatRevealVisible=" + metrics.RepeatRevealVisible +
                    " SourceReadyChainSpread=" + Num(metrics.SourceReadyChainSpread) +
                    " CycleHiddenChainSpread=" + Num(metrics.CycleHiddenChainSpread) +
                    " RepeatSourceReadyChainSpread=" + Num(metrics.RepeatSourceReadyChainSpread));

            state.ImagePaths = string.Join("|", captures);
            state.Summary = "Mode=" + (state.FinalCapture ? "Final" : "Diagnostic") +
                " ActualAnimator=True Images=" + captures.Count + " CaptureDir=" + state.CaptureDir +
                " IntroRise=" + Num(introRise) + " InitialRiseUp=" + Num(initialRiseUp) +
                " InitialRiseLateral=" + Num(initialRiseLateral) +
                " FirstStrikeOutward=" + Num(firstStrikeOutward) +
                " FirstStrikeDrop=" + Num(firstStrikeDrop) + " FirstStrikeLateral=" + Num(firstStrikeLateral) +
                " FirstReturnError=" + Num(firstReturnError) + " SecondStrikeOutward=" + Num(secondStrikeOutward) +
                " SecondStrikeDrop=" + Num(secondStrikeDrop) + " SecondStrikeLateral=" + Num(secondStrikeLateral) +
                " HiddenReturnError=" + Num(hiddenReturnError) + " FixedAnchorDrift=" + Num(anchorDrift) +
                " IntroHiddenVisible=" + metrics.IntroHiddenVisible +
                " SourceRevealVisible=" + metrics.SourceRevealVisible +
                " CycleHiddenVisible=" + metrics.CycleHiddenVisible +
                " RepeatRevealVisible=" + metrics.RepeatRevealVisible +
                " SourceReadyChainSpread=" + Num(metrics.SourceReadyChainSpread) +
                " CycleHiddenChainSpread=" + Num(metrics.CycleHiddenChainSpread) +
                " RepeatSourceReadyChainSpread=" + Num(metrics.RepeatSourceReadyChainSpread);
            File.WriteAllText(ResultPath, state.Summary);
            Debug.Log("Dolore04TentacleStabFullMotionActualAnimatorCaptured Result=PASS " + state.Summary);
        }

        private static void CapturePose(Camera camera, Transform slot, Animator animator,
            SkinnedMeshRenderer tentacleRenderer, SkinnedState[] skinnedStates, string stateName,
            float normalizedTime, string label, string captureDir, Vector3 outward, Vector3 lateral,
            Transform tip, Transform fixedAnchor, List<string> captures, ref PoseMetrics metrics)
        {
            animator.speed = 1f;
            animator.Play(stateName, 0, normalizedTime);
            animator.Update(0f);
            animator.speed = 0f;
            foreach (var skinnedState in skinnedStates)
                if (skinnedState.Renderer != null) skinnedState.Renderer.localBounds = skinnedState.LocalBounds;
            RecordMetrics(label, tip, fixedAnchor.position, tentacleRenderer.enabled, ref metrics);
            CaptureView(camera, slot, label + "_front", captureDir, captures, outward);
            CaptureView(camera, slot, label + "_side", captureDir, captures, lateral);
            CaptureView(camera, slot, label + "_front3q", captureDir, captures, (outward + lateral * 0.55f).normalized);
        }

        private static void RecordMetrics(
            string label,
            Transform tip,
            Vector3 anchor,
            bool rendererVisible,
            ref PoseMetrics metrics)
        {
            if (label == "02_ring_ready") { metrics.HasAnchor = true; metrics.Anchor = anchor; }
            if (metrics.HasAnchor && label != "21_body_hiding_upward" &&
                label != "22_ring_hiding_in_place" && label != "23_cycle_hidden" &&
                !label.StartsWith("24_repeat", StringComparison.Ordinal) &&
                !label.StartsWith("25_repeat", StringComparison.Ordinal) &&
                !label.StartsWith("26_repeat", StringComparison.Ordinal))
                metrics.MaximumAnchorDrift = Mathf.Max(
                    metrics.MaximumAnchorDrift, Vector3.Distance(metrics.Anchor, anchor));
            var tipPosition = tip.position;
            if (label == "00_intro_hidden") { metrics.IntroStartTip = tipPosition; metrics.IntroHiddenVisible = rendererVisible; }
            else if (label == "01_ring_generating") metrics.SourceRevealVisible = rendererVisible;
            else if (label == "02_ring_ready")
            {
                metrics.SourceReadyTip = tipPosition;
                metrics.SourceReadyChainSpread = MovingChainSpread(tip);
            }
            else if (label == "04_tip_passing_ring") metrics.InitialRiseTip = tipPosition;
            else if (label == "10_intro_prepared") metrics.IntroPreparedTip = tipPosition;
            else if (label == "11_loop_prepared") metrics.LoopPreparedTip = tipPosition;
            else if (label == "14_first_downstrike") metrics.FirstStrikeTip = tipPosition;
            else if (label == "16_first_return") metrics.FirstReturnTip = tipPosition;
            else if (label == "19_second_downstrike") metrics.SecondStrikeTip = tipPosition;
            else if (label == "23_cycle_hidden")
            {
                metrics.HiddenTip = tipPosition;
                metrics.CycleHiddenVisible = rendererVisible;
                metrics.CycleHiddenChainSpread = MovingChainSpread(tip);
            }
            else if (label == "24_repeat_ring_generating") metrics.RepeatRevealVisible = rendererVisible;
            else if (label == "25_repeat_ring_ready") metrics.RepeatSourceReadyChainSpread = MovingChainSpread(tip);
        }

        private static float MovingChainSpread(Transform tip)
        {
            var chain = new List<Transform>();
            var current = tip;
            while (current != null)
            {
                chain.Add(current);
                if (current.name == "Bone_009") break;
                current = current.parent;
            }
            if (current == null) throw new InvalidOperationException("The moving tentacle chain no longer reaches Bone_009.");
            return chain.Max(item => Vector3.Distance(item.position, current.position));
        }

        private static void CaptureView(Camera camera, Transform target, string label, string captureDir,
            List<string> captures, Vector3 viewDirection)
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

        private static void RestoreOtherSlots(IEnumerable<ActiveState> states)
        {
            foreach (var state in states) if (state.GameObject != null) state.GameObject.SetActive(state.Value);
        }

        private static LayerState[] SetLayerRecursively(Transform root, int layer)
        {
            var states = root.GetComponentsInChildren<Transform>(true)
                .Select(item => new LayerState(item.gameObject, item.gameObject.layer)).ToArray();
            foreach (var state in states) state.GameObject.layer = layer;
            return states;
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
            callback?.Invoke("Dolore motion 3 actual Animator full-motion capture completed. " + summary);
        }

        private static void FailFromState(CaptureState state)
        {
            var callback = fail;
            var error = string.IsNullOrWhiteSpace(state.Error) ? "Dolore actual Animator capture failed." : state.Error;
            CleanupCallbacks();
            DeleteStateFiles();
            callback?.Invoke(new InvalidOperationException(error));
        }

        private static void CleanupCallbacks() { complete = null; fail = null; }
        private static void DeleteStateFiles() { TryDelete(StatePath); TryDelete(ResultPath); }

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
                Phase = Get(values, "phase"), CaptureDir = Get(values, "captureDir"),
                ImagePaths = Get(values, "imagePaths"), Summary = Get(values, "summary"), Error = Get(values, "error"),
                FinalCapture = bool.TryParse(Get(values, "finalCapture"), out var finalCapture) && finalCapture,
                StartedUtcTicks = long.TryParse(Get(values, "startedUtcTicks"), out var ticks) ? ticks : 0L
            };
        }

        private static void WriteState(CaptureState state)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StatePath));
            File.WriteAllLines(StatePath, new[] { "phase=" + state.Phase, "captureDir=" + state.CaptureDir,
                "imagePaths=" + state.ImagePaths, "summary=" + state.Summary, "error=" + state.Error,
                "finalCapture=" + state.FinalCapture, "startedUtcTicks=" + state.StartedUtcTicks.ToString(CultureInfo.InvariantCulture) });
        }

        private static string Get(IDictionary<string, string> values, string key) =>
            values.TryGetValue(key, out var value) ? value : string.Empty;
        private static void TryDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
        private static string Num(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);
        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string StatePath => Path.Combine(ProjectRoot, "Logs", StateFileName);
        private static string ResultPath => Path.Combine(ProjectRoot, "Logs", ResultFileName);

        private sealed class CaptureState
        {
            public string Phase; public string CaptureDir; public string ImagePaths; public string Summary; public string Error;
            public bool FinalCapture; public long StartedUtcTicks;
        }

        private struct PoseMetrics
        {
            public bool HasAnchor; public Vector3 Anchor; public float MaximumAnchorDrift;
            public bool IntroHiddenVisible; public bool SourceRevealVisible;
            public bool CycleHiddenVisible; public bool RepeatRevealVisible;
            public float SourceReadyChainSpread; public float CycleHiddenChainSpread;
            public float RepeatSourceReadyChainSpread;
            public Vector3 IntroStartTip; public Vector3 SourceReadyTip; public Vector3 InitialRiseTip;
            public Vector3 IntroPreparedTip; public Vector3 LoopPreparedTip;
            public Vector3 FirstStrikeTip; public Vector3 FirstReturnTip; public Vector3 SecondStrikeTip; public Vector3 HiddenTip;
        }

        private readonly struct ActiveState
        {
            public ActiveState(GameObject gameObject, bool value) { GameObject = gameObject; Value = value; }
            public GameObject GameObject { get; } public bool Value { get; }
        }

        private readonly struct LayerState
        {
            public LayerState(GameObject gameObject, int value) { GameObject = gameObject; Value = value; }
            public GameObject GameObject { get; } public int Value { get; }
        }

        private readonly struct SkinnedState
        {
            public SkinnedState(SkinnedMeshRenderer renderer, bool updateWhenOffscreen,
                bool forceMatrixRecalculation, Bounds localBounds)
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
