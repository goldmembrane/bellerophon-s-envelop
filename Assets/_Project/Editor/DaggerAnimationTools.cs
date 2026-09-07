using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Bellerophon.PlayerAnimation;

namespace Bellerophon.Editor
{
    internal static class DaggerAnimationTools
    {
        internal const string Output = "docs/validation/dagger_throw_forward_shield_draw_start_2026-09-07";
        internal const string StabSource = "Assets/_Project/Art/Player/Animations/Dagger_Stab_Mixamo.fbx";
        internal static readonly string[] Targets = { "Dagger_Idle", "Dagger_Stab", "Dagger_Throw_Mode", "Dagger_Throw_Release", "Dagger_Throw_Cancel" };
        internal const string AlignmentOutput = Output;
        private static readonly string[] AlignmentTargets = { "Dagger_Idle", "Dagger_Throw_Release", "Dagger_Throw_Cancel" };
        // Frame 34 begins the unwanted arm-back arc; frame 69 restores the hand-at-torso pose.
        private const float StabBackArcStartNormalizedTime = 34f / 81f;
        // Request the skip early enough that Animator never evaluates frame 34 under a low editor frame rate.
        private const float StabSkipTriggerNormalizedTime = 31f / 81f;
        private const float StabResumeNormalizedTime = 69f / 81f;
        private const float StabAccelerationDurationSeconds = 4f;
        private const float StabPatternDurationSeconds = 5f;
        private const float StabInitialAttackIntervalSeconds = 2f;
        private const float StabMinimumAttackIntervalSeconds = 1.5f;
        private const float StabPoseTransitionDurationSeconds = 0.25f;
        // The source hand path reaches its greatest forward speed before descending at this instant.
        private const float ThrowTrajectoryReleaseTime = 46f / 60f;
        private const string FinalMotionCorrectionsPath = Output + "/final.png";

        [Serializable]
        private sealed class HistoricalIdleRotation
        {
            public string commit = "47f3900";
            public Quaternion rootRelativeRotation;
        }

        internal static void InspectAlignment()
        {
            Directory.CreateDirectory(AlignmentOutput);
            // Read the already-present LFS object named by git show; never spawn Git inside the editor.
            string historical = File.ReadAllText(".git/lfs/objects/13/ae/13ae60572c1fd86785b88ac49c73d04b332e818e5319e65aa608eec796bf9c35", Encoding.UTF8);
            var options = System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.Singleline;
            var instances = System.Text.RegularExpressions.Regex.Matches(historical, @"^--- !u!1001 &\d+\r?\n(?<body>.*?)(?=^--- |\z)", options);
            string idleInstance = instances.Cast<System.Text.RegularExpressions.Match>().Select(m => m.Groups["body"].Value).Single(body => body.Contains("value: Dagger_Idle\n") || body.Contains("value: Dagger_Idle\r\n"));
            var overrides = System.Text.RegularExpressions.Regex.Matches(idleInstance, @"- target: \{fileID: (?<id>-?\d+),[^\n]+\n\s+propertyPath: m_LocalRotation\.(?<axis>[xyzw])\r?\n\s+value: (?<value>[^\r\n]+)");
            var values = new System.Collections.Generic.Dictionary<string, float>();
            foreach (System.Text.RegularExpressions.Match value in overrides) values[value.Groups["id"].Value + "." + value.Groups["axis"].Value] = float.Parse(value.Groups["value"].Value, System.Globalization.CultureInfo.InvariantCulture);
            var assetRoot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Player/player.fbx").transform;
            var hand = assetRoot.GetComponentsInChildren<Transform>(true).Single(t => t.name == "RightHand");
            var originalLocal = JsonUtility.FromJson<OriginalDaggerPlacement>(File.ReadAllText("docs/validation/dagger_idle_position_sync_2026-09-07/current_position.json", Encoding.UTF8));
            Quaternion rootRotation = originalLocal.localRotation;
            for (var bone = hand; bone != assetRoot; bone = bone.parent)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(bone, out string guid, out long fileId);
                Quaternion rotation = bone.localRotation;
                for (int component = 0; component < 4; component++)
                    if (values.TryGetValue(fileId + "." + "xyzw"[component], out float value)) rotation[component] = value;
                rootRotation = rotation * rootRotation;
            }
            var historicalRotation = new HistoricalIdleRotation { rootRelativeRotation = rootRotation };
            File.WriteAllText(AlignmentOutput + "/original_idle_rotation.json", JsonUtility.ToJson(historicalRotation, true), Encoding.UTF8);
            var report = new StringBuilder();
            report.AppendLine("Original root-relative dagger rotation=" + historicalRotation.rootRelativeRotation.ToString("F9"));
            report.AppendLine("Original root-relative blade direction=" + (historicalRotation.rootRelativeRotation * Vector3.forward).ToString("F9"));
            foreach (string name in AlignmentTargets)
            {
                var root = Find(name);
                var prop = root.GetComponent<DaggerPropMotion>();
                Vector3 currentBladeDirection = root.InverseTransformDirection(prop.dagger.TransformDirection(prop.bladeAxis));
                report.AppendLine(name + " currentRootRelative=" + (Quaternion.Inverse(root.rotation) * prop.dagger.rotation).ToString("F9") +
                    " currentBladeDirection=" + currentBladeDirection.ToString("F9") +
                    " currentBladeElevation=" + (Mathf.Asin(Mathf.Clamp(currentBladeDirection.normalized.y, -1f, 1f)) * Mathf.Rad2Deg).ToString("F6", System.Globalization.CultureInfo.InvariantCulture) +
                    " storedHandLocal=" + prop.idleLocalRotation.ToString("F9"));
            }
            var release = Find("Dagger_Throw_Release");
            var work = UnityEngine.Object.Instantiate(release.gameObject);
            work.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                work.GetComponent<Animator>().enabled = false;
                work.GetComponent<DaggerPropMotion>().enabled = false;
                work.transform.position = Vector3.zero;
                var prop = work.GetComponent<DaggerPropMotion>();
                var clip = (AnimationClip)((AnimatorController)release.GetComponent<Animator>().runtimeAnimatorController).layers[0].stateMachine.defaultState.motion;
                Vector3 Grip(float time)
                {
                    clip.SampleAnimation(work, time);
                    return prop.rightHand.TransformPoint(prop.idleLocalPosition + prop.idleLocalRotation * Vector3.Scale(prop.originalLocalScale, prop.handleCenter));
                }
                for (int frame = 28; frame <= 56; frame++)
                {
                    float time = frame / 60f;
                    Vector3 a = Grip(time - 1f / 120f);
                    Vector3 b = Grip(time + 1f / 120f);
                    Vector3 velocity = work.transform.InverseTransformDirection((b - a) * 60f);
                    report.AppendLine("sourceTime=" + time.ToString("F6") + " grip=" + Grip(time).ToString("F6") + " localVelocity=" + velocity.ToString("F6"));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(work); }
            var stabWork = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(StabSource));
            stabWork.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                stabWork.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var stabClip = AssetDatabase.LoadAllAssetsAtPath(StabSource).OfType<AnimationClip>()
                    .Single(c => !c.name.StartsWith("__preview__"));
                var stabTransforms = stabWork.GetComponentsInChildren<Transform>(true);
                var torso = stabTransforms.Single(t => t.name == "Spine");
                var stabHand = stabTransforms.Single(t => t.name == "RightHand");
                var upperArm = stabTransforms.Single(t => t.name == "RightArm");
                int lastFrame = Mathf.RoundToInt(stabClip.length * stabClip.frameRate);
                report.AppendLine("STAB source frame samples; positions are source-root local.");
                for (int frame = 0; frame <= lastFrame; frame++)
                {
                    float time = Mathf.Min(frame / stabClip.frameRate, stabClip.length);
                    stabClip.SampleAnimation(stabWork, time);
                    Vector3 handPosition = stabWork.transform.InverseTransformPoint(stabHand.position);
                    Vector3 torsoPosition = stabWork.transform.InverseTransformPoint(torso.position);
                    Vector3 handFromTorso = handPosition - torsoPosition;
                    Vector3 armForward = stabWork.transform.InverseTransformDirection(upperArm.forward);
                    report.AppendLine(
                        "stabFrame=" + frame +
                        " time=" + time.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) +
                        " hand=" + handPosition.ToString("F6") +
                        " torso=" + torsoPosition.ToString("F6") +
                        " handFromTorso=" + handFromTorso.ToString("F6") +
                        " distance=" + handFromTorso.magnitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) +
                        " upperArmForward=" + armForward.ToString("F6"));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(stabWork); }
            File.WriteAllText(AlignmentOutput + "/analysis.txt", report.ToString(), Encoding.UTF8);
            Debug.Log("Original angle and untouched throw-clip path inspected; scene has not been modified.");
        }

        [Serializable]
        private sealed class OriginalDaggerPlacement { public Quaternion localRotation; }

        private static string alignmentCaptureDirectory;
        private static int alignmentCaptureCycle;
        private static int alignmentCaptureFrame;
        private static StringBuilder alignmentCaptureReport;
        private static float previousAlignmentTime;
        private static Vector3 previousAlignmentGrip;
        private static double alignmentCaptureDeadline;

        internal static void CaptureAlignment(string label)
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Actual playback required.");
            if (label != "before" && label != "after") throw new InvalidOperationException("Expected before/after capture label.");
            Directory.CreateDirectory(AlignmentOutput);
            alignmentCaptureDirectory = AlignmentOutput + "/" + label + "_" + DateTime.Now.ToString("HHmmss");
            Directory.CreateDirectory(alignmentCaptureDirectory);
            foreach (string name in new[] { "Dagger_Idle", "Dagger_Throw_Cancel" })
            {
                var root = Find(name);
                Render(root, root.position + root.up * .95f + root.forward * .3f, root.right, 1.4f, alignmentCaptureDirectory + "/" + name + "_side.png");
                Render(root, root.GetComponent<DaggerPropMotion>().rightHand.position, root.right, .5f, alignmentCaptureDirectory + "/" + name + "_hand.png");
            }
            alignmentCaptureCycle = Mathf.FloorToInt(Find("Dagger_Throw_Release").GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime) + 1;
            alignmentCaptureFrame = 0;
            alignmentCaptureReport = new StringBuilder("frame,time,handX,handY,handZ,gripX,gripY,gripZ,daggerX,daggerY,daggerZ,velocityX,velocityY,velocityZ,released\n");
            previousAlignmentTime = -1f;
            alignmentCaptureDeadline = EditorApplication.timeSinceStartup + 30d;
            EditorApplication.update -= AlignmentCaptureTick;
            EditorApplication.update += AlignmentCaptureTick;
            Debug.Log("Unmodified release-boundary capture started: " + alignmentCaptureDirectory);
        }

        private static void AlignmentCaptureTick()
        {
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup > alignmentCaptureDeadline)
            {
                EditorApplication.update -= AlignmentCaptureTick;
                return;
            }
            var root = Find("Dagger_Throw_Release");
            var prop = root.GetComponent<DaggerPropMotion>();
            float normalized = prop.animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            float time = (normalized - alignmentCaptureCycle) * prop.clipLength;
            if (time < 0.45f) return;
            if (time > 1.18f)
            {
                EditorApplication.update -= AlignmentCaptureTick;
                File.WriteAllText(alignmentCaptureDirectory + "/observations.csv", alignmentCaptureReport.ToString(), Encoding.UTF8);
                var shots = Enumerable.Range(0, alignmentCaptureFrame).Select(f => alignmentCaptureDirectory + "/release_" + f.ToString("D2") + "_side.png").ToArray();
                if (shots.Length > 0) Compose(shots, 4, alignmentCaptureDirectory + "/release_sequence.png");
                Debug.Log("Release-boundary capture completed: " + alignmentCaptureDirectory);
                return;
            }
            Vector3 grip = prop.rightHand.TransformPoint(prop.idleLocalPosition + prop.idleLocalRotation * Vector3.Scale(prop.originalLocalScale, prop.handleCenter));
            Vector3 velocity = previousAlignmentTime < 0f ? Vector3.zero : root.InverseTransformDirection((grip - previousAlignmentGrip) / (time - previousAlignmentTime));
            previousAlignmentTime = time;
            previousAlignmentGrip = grip;
            string Vec(Vector3 v) => v.x.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) + "," + v.y.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) + "," + v.z.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
            alignmentCaptureReport.AppendLine(alignmentCaptureFrame + "," + time.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) + "," + Vec(prop.rightHand.position) + "," + Vec(grip) + "," + Vec(prop.dagger.position) + "," + Vec(velocity) + "," + prop.IsReleased);
            string prefix = alignmentCaptureDirectory + "/release_" + alignmentCaptureFrame.ToString("D2");
            Render(root, root.position + root.up * 1.15f + root.forward * .65f, root.right, 1.0f, prefix + "_side.png");
            Render(root, root.position + root.up * 1.15f + root.forward * .65f, root.forward, 1.0f, prefix + "_front.png");
            alignmentCaptureFrame++;
        }
        private static Transform Find(string name)
        {
            var matches = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Where(item => item.name == name).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException(name + " count=" + matches.Length);
            return matches[0];
        }
        internal static void Inspect()
        {
            Directory.CreateDirectory(Output);
            var report = new StringBuilder();
            foreach (string name in Targets.Concat(new[] { "Hands_Carry_OneHand", "Hands_Throw_Ready", "Hands_Throw_Release", "Hands_Throw_Cancel", "Stick_Throw_Release" }))
            {
                var target = Find(name);
                var animator = target.GetComponent<Animator>();
                report.AppendLine("TARGET " + name + " position=" + target.position + " forward=" + target.forward);
                report.AppendLine(" components=" + string.Join(",", target.GetComponents<Component>().Select(c => c == null ? "missing" : c.GetType().Name)));
                report.AppendLine(" animator=" + (animator != null) + " controller=" + (animator == null ? "" : AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)) +
                    " avatar=" + (animator == null ? "" : AssetDatabase.GetAssetPath(animator.avatar)) + " enabled=" + (animator != null && animator.enabled) + " rootMotion=" + (animator != null && animator.applyRootMotion));
                if (animator != null && animator.runtimeAnimatorController is AnimatorController controller)
                    foreach (var layer in controller.layers)
                    {
                        report.AppendLine(" layer=" + layer.name + " weight=" + layer.defaultWeight + " mask=" + AssetDatabase.GetAssetPath(layer.avatarMask));
                        foreach (var entry in layer.stateMachine.states)
                        {
                            var state = entry.state;
                            report.AppendLine(" state=" + state.name + " speed=" + state.speed + " motion=" + AssetDatabase.GetAssetPath(state.motion));
                            if (state.motion is AnimationClip clip)
                            {
                                var bindings = AnimationUtility.GetCurveBindings(clip);
                                report.AppendLine(" clip=" + clip.name + " length=" + clip.length + " fps=" + clip.frameRate + " human=" + clip.humanMotion + " loop=" + clip.isLooping + " bindings=" + bindings.Length);
                                foreach (var binding in bindings.Take(5).Concat(bindings.Where(b => b.type != typeof(Transform))))
                                    report.AppendLine("  binding=" + binding.path + " | " + binding.type.Name + " | " + binding.propertyName);
                            }
                        }
                    }
                foreach (var mesh in target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    report.AppendLine(" skin=" + mesh.name + " mesh=" + AssetDatabase.GetAssetPath(mesh.sharedMesh) + " blendShapes=" + mesh.sharedMesh?.blendShapeCount);
                var dagger = target.GetComponentsInChildren<Transform>(true).SingleOrDefault(t => t.name == "Dagger_RightHand");
                if (dagger != null) report.AppendLine(" dagger parent=" + dagger.parent.name + " pos=" + dagger.localPosition.ToString("F9") + " rot=" + dagger.localRotation.ToString("F9") + " scale=" + dagger.localScale);
            }
            File.WriteAllText(Output + "/sources.txt", report.ToString(), Encoding.UTF8);
            if (Find("Dagger_Idle").GetComponent<DaggerPropMotion>() != null) InspectCopies();
            if (EditorApplication.isPlaying) BeginRuntimeObservation();
            Debug.Log("Dagger source inspection saved without scene changes.");
        }

        private static void InspectCopies()
        {
            var report = new StringBuilder();
            string[] originals = { "Hands_Carry_OneHand", null, "Hands_Throw_Ready", "Hands_Throw_Release", "Hands_Throw_Cancel" };
            for (int index = 0; index < Targets.Length; index++)
            {
                var target = Find(Targets[index]);
                var controller = (AnimatorController)target.GetComponent<Animator>().runtimeAnimatorController;
                for (int layer = 0; layer < controller.layers.Length; layer++)
                {
                    AnimationClip source = index == 1 ? AssetDatabase.LoadAllAssetsAtPath(StabSource).OfType<AnimationClip>().Single(c => !c.name.StartsWith("__preview__")) :
                        (AnimationClip)((AnimatorController)Find(originals[index]).GetComponent<Animator>().runtimeAnimatorController).layers[layer].stateMachine.defaultState.motion;
                    var copy = (AnimationClip)controller.layers[layer].stateMachine.defaultState.motion;
                    var originalBindings = AnimationUtility.GetCurveBindings(source);
                    var copiedBindings = AnimationUtility.GetCurveBindings(copy);
                    bool equal = originalBindings.Length == copiedBindings.Length && source.length == copy.length && source.frameRate == copy.frameRate;
                    foreach (var binding in originalBindings)
                    {
                        var a = AnimationUtility.GetEditorCurve(source, binding);
                        var b = AnimationUtility.GetEditorCurve(copy, binding);
                        equal &= b != null && a.preWrapMode == b.preWrapMode && a.postWrapMode == b.postWrapMode && a.keys.SequenceEqual(b.keys);
                    }
                    var originalObjects = AnimationUtility.GetObjectReferenceCurveBindings(source);
                    equal &= originalObjects.Length == AnimationUtility.GetObjectReferenceCurveBindings(copy).Length;
                    foreach (var binding in originalObjects) equal &= AnimationUtility.GetObjectReferenceCurve(source, binding).SequenceEqual(AnimationUtility.GetObjectReferenceCurve(copy, binding));
                    equal &= AnimationUtility.GetAnimationEvents(source).Length == AnimationUtility.GetAnimationEvents(copy).Length;
                    report.AppendLine(Targets[index] + " layer=" + layer + " originalCurvesExact=" + equal + " curveCount=" + copiedBindings.Length + " length=" + copy.length + " loop=" + copy.isLooping + " speed=" + controller.layers[layer].stateMachine.defaultState.speed);
                }
                var prop = target.GetComponent<DaggerPropMotion>();
                report.AppendLine(" meshes=" + string.Join(",", target.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r => AssetDatabase.GetAssetPath(r.sharedMesh))) + " distance=" + prop.throwDistance + " elevation=" + prop.bladeElevation + " releaseTime=" + prop.releaseTime);
            }
            File.WriteAllText(Output + "/copy_integrity.txt", report.ToString(), Encoding.UTF8);
        }

        internal static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop playback before applying.");
            if (SceneManager.GetActiveScene().path != "Assets/_Project/Scenes/CargoRunMvp.unity") throw new InvalidOperationException("Wrong active scene.");
            if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Unrelated unsaved scene changes must be preserved; refusing to save over them.");
            Directory.CreateDirectory(Output);
            AssetDatabase.ImportAsset(StabSource, ImportAssetOptions.ForceSynchronousImport);
            var importer = (ModelImporter)AssetImporter.GetAtPath(StabSource);
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.resampleCurves = false;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.SaveAndReimport();
            var takes = importer.defaultClipAnimations;
            if (takes.Length != 1) throw new InvalidOperationException("Expected the single original stabbing take.");
            takes[0].loopTime = true;
            takes[0].loopPose = false;
            importer.clipAnimations = takes;
            importer.SaveAndReimport();
            var clip = AssetDatabase.LoadAllAssetsAtPath(StabSource).OfType<AnimationClip>().Single(c => !c.name.StartsWith("__preview__"));
            var report = new StringBuilder();
            report.AppendLine("take=" + takes[0].takeName + " first=" + takes[0].firstFrame + " last=" + takes[0].lastFrame);
            report.AppendLine("clip=" + clip.name + " length=" + clip.length + " fps=" + clip.frameRate);
            foreach (var binding in AnimationUtility.GetCurveBindings(clip)) report.AppendLine(binding.path + " | " + binding.propertyName);
            File.WriteAllText(Output + "/stabbing_import.txt", report.ToString(), Encoding.UTF8);
            const string animationFolder = "Assets/_Project/Art/Player/Animations/Dagger";
            if (!AssetDatabase.IsValidFolder(animationFolder)) AssetDatabase.CreateFolder("Assets/_Project/Art/Player/Animations", "Dagger");
            var daggerAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Items/Dagger/dagger.fbx");
            PlayerHandsObjectAnimationTools.GetDaggerAnimationGeometry(daggerAsset, out var axis, out var handle, out var bounds);
            var idleDagger = Find("Dagger_Idle").GetComponentsInChildren<Transform>(true).Single(t => t.name == "Dagger_RightHand");
            Vector3 idlePosition = idleDagger.localPosition;
            Quaternion idleRotation = idleDagger.localRotation;
            Vector3 scale = idleDagger.localScale;
            string[] sourceNames = { "Hands_Carry_OneHand", null, "Hands_Throw_Ready", "Hands_Throw_Release", "Hands_Throw_Cancel" };
            var applyReport = new StringBuilder();
            for (int index = 0; index < Targets.Length; index++)
            {
                string name = Targets[index];
                Transform target = Find(name);
                Transform source = index == 1 ? AssetDatabase.LoadAssetAtPath<GameObject>(StabSource).transform : Find(sourceNames[index]);
                AnimatorController controller;
                string controllerPath = animationFolder + "/" + name + ".controller";
                AnimationClip mainClip;
                if (index == 1)
                {
                    mainClip = CopyClip(clip, animationFolder + "/" + name + ".anim");
                    controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                    if (controller == null)
                    {
                        controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                        controller.layers[0].stateMachine.AddState(name);
                    }
                    controller.layers[0].stateMachine.defaultState = controller.layers[0].stateMachine.states[0].state;
                    controller.layers[0].stateMachine.defaultState.motion = mainClip;
                    controller.layers[0].stateMachine.defaultState.writeDefaultValues = false;
                    var stabState = controller.layers[0].stateMachine.defaultState;
                    var stabLoops = stabState.behaviours.OfType<DaggerStabLoopStateBehaviour>().ToArray();
                    var stabLoop = stabLoops.FirstOrDefault() ?? stabState.AddStateMachineBehaviour<DaggerStabLoopStateBehaviour>();
                    foreach (var duplicate in stabLoops.Skip(1)) UnityEngine.Object.DestroyImmediate(duplicate, true);
                    stabLoop.Configure(StabSkipTriggerNormalizedTime, StabResumeNormalizedTime);
                    EditorUtility.SetDirty(stabLoop);
                }
                else
                {
                    var sourceController = (AnimatorController)source.GetComponent<Animator>().runtimeAnimatorController;
                    if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) == null && !AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(sourceController), controllerPath))
                        throw new InvalidOperationException("Could not copy " + sourceController.name);
                    controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                    for (int layerIndex = 0; layerIndex < sourceController.layers.Length; layerIndex++)
                    {
                        var originalState = sourceController.layers[layerIndex].stateMachine.defaultState;
                        if (!(originalState.motion is AnimationClip originalClip)) throw new InvalidOperationException("Expected original single-clip state.");
                        var copied = CopyClip(originalClip, animationFolder + "/" + name + "_Layer" + layerIndex + ".anim");
                        controller.layers[layerIndex].stateMachine.defaultState.motion = copied;
                        EditorUtility.SetDirty(controller.layers[layerIndex].stateMachine.defaultState);
                    }
                    mainClip = (AnimationClip)controller.layers[0].stateMachine.defaultState.motion;
                }

                // Generic clips contain sparse tracks. Copy their source skeleton's baseline transforms,
                // not its mesh, skin weights, materials, or attachments; all original curves stay untouched.
                foreach (Transform bone in source.Find("Armature").GetComponentsInChildren<Transform>(true))
                {
                    string path = AnimationUtility.CalculateTransformPath(bone, source);
                    Transform destination = target.Find(path);
                    if (destination == null) continue;
                    destination.localPosition = bone.localPosition;
                    destination.localRotation = bone.localRotation;
                    destination.localScale = bone.localScale;
                    EditorUtility.SetDirty(destination);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(destination);
                }
                var animator = target.GetComponent<Animator>();
                if (animator == null) animator = target.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.enabled = true;
                if (index == 1)
                {
                    var acceleration = target.GetComponent<DaggerStabAccelerationMotion>();
                    if (acceleration == null) acceleration = target.gameObject.AddComponent<DaggerStabAccelerationMotion>();
                    acceleration.Configure(
                        animator,
                        mainClip.length,
                        StabSkipTriggerNormalizedTime,
                        StabResumeNormalizedTime,
                        StabAccelerationDurationSeconds,
                        StabPatternDurationSeconds,
                        StabInitialAttackIntervalSeconds,
                        StabMinimumAttackIntervalSeconds);
                    EditorUtility.SetDirty(acceleration);
                }
                var dagger = target.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Dagger_RightHand");
                if (index == 1)
                {
                    var poseTransition = target.GetComponent<DaggerStabPoseTransitionMotion>();
                    if (poseTransition == null) poseTransition = target.gameObject.AddComponent<DaggerStabPoseTransitionMotion>();
                    poseTransition.Configure(
                        animator,
                        target.Find("Armature"),
                        dagger,
                        StabPoseTransitionDurationSeconds);
                    EditorUtility.SetDirty(poseTransition);
                }
                var motion = target.GetComponent<DaggerPropMotion>();
                if (motion == null) motion = target.gameObject.AddComponent<DaggerPropMotion>();
                motion.animator = animator;
                motion.dagger = dagger;
                motion.rightHand = dagger.parent;
                motion.idleLocalPosition = idlePosition;
                motion.idleLocalRotation = idleRotation;
                motion.originalLocalScale = scale;
                motion.bladeAxis = axis;
                motion.handleCenter = handle;
                motion.localBounds = bounds;
                motion.clipLength = mainClip.length;
                motion.bladeElevation = 10f;
                motion.throwDistance = 6f;
                motion.throwElevation = PlayerHandsObjectAnimationTools.DaggerReferenceThrowElevation;
                motion.action = index == 0 ? DaggerPropMotion.PropAction.Idle : index == 3 ? DaggerPropMotion.PropAction.Throw : index == 4 ? DaggerPropMotion.PropAction.Cancel : DaggerPropMotion.PropAction.Aim;
                motion.releaseTime = index == 3 ? ThrowTrajectoryReleaseTime : 0f;
                motion.throwDirectionLocal = index == 3 ? Vector3.forward : Vector3.zero;
                EditorUtility.SetDirty(motion);
                EditorUtility.SetDirty(animator);
                EditorUtility.SetDirty(controller);
                applyReport.AppendLine(name + " source=" + (sourceNames[index] ?? StabSource) + " length=" + mainClip.length + " release=" + motion.releaseTime + " controller=" + controllerPath);
                applyReport.AppendLine("baseline rootScale=" + source.localScale + " armatureScale=" + source.Find("Armature").localScale + " targetRootScale=" + target.localScale);
                if (index == 1) applyReport.AppendLine("stabBackArcStartNormalizedTime=" + StabBackArcStartNormalizedTime +
                    " skipTriggerNormalizedTime=" + StabSkipTriggerNormalizedTime +
                    " resumeAtHandTorsoNormalizedTime=" + StabResumeNormalizedTime +
                    " accelerationDurationSeconds=" + StabAccelerationDurationSeconds +
                    " patternDurationSeconds=" + StabPatternDurationSeconds +
                    " attackIntervalSeconds=" + StabInitialAttackIntervalSeconds + ".." + StabMinimumAttackIntervalSeconds +
                    " poseTransitionDurationSeconds=" + StabPoseTransitionDurationSeconds +
                    " poseTransitionMode=endpointBlendWithoutBackArcSampling" +
                    " sourceClipCurvesPreserved=True");
                if (index == 1)
                {
                    float playedDuration = mainClip.length * (StabSkipTriggerNormalizedTime + 1f - StabResumeNormalizedTime);
                    applyReport.AppendLine("stabPlayedSourceDurationSeconds=" + playedDuration.ToString("F6") +
                        " initialPlaybackSpeed=" + (playedDuration / StabInitialAttackIntervalSeconds).ToString("F6") +
                        " maximumPlaybackSpeed=" + (playedDuration / StabMinimumAttackIntervalSeconds).ToString("F6"));
                }
                if (index == 3) applyReport.AppendLine("throwHorizontalDirectionLocal=" + motion.throwDirectionLocal.ToString("F9") +
                    " throwElevationDegrees=" + motion.throwElevation.ToString("F6") +
                    " transporterLocalRightComponent=0");
            }
            ApplyShieldDrawPlayerStart(applyReport);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            applyReport.AppendLine("Meshes, blendshapes, weights, materials, source objects and source animation curves were not modified.");
            applyReport.AppendLine("The original blendShape.Breathing curve remains in the copied ready clip; no mesh reference replacement or blendshape creation was performed.");
            File.WriteAllText(Output + "/applied.txt", applyReport.ToString(), Encoding.UTF8);
            Debug.Log("Five dagger animation controllers connected; source curves and all mesh references preserved.");
        }

        private static AnimationClip CopyClip(AnimationClip source, string path)
        {
            var destination = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (destination == null)
            {
                destination = new AnimationClip();
                EditorUtility.CopySerialized(source, destination);
                AssetDatabase.CreateAsset(destination, path);
            }
            else EditorUtility.CopySerialized(source, destination);
            destination.name = Path.GetFileNameWithoutExtension(path);
            EditorUtility.SetDirty(destination);
            return destination;
        }

        private static void ApplyShieldDrawPlayerStart(StringBuilder report)
        {
            var player = Find("Player");
            var shieldDraw = Find("Shield_Draw");
            var playerCamera = player.GetComponentsInChildren<Camera>(true).Single();
            var renderers = shieldDraw.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Shield_Draw has no enabled renderer for start framing.");
            }

            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);

            Vector3 front = Vector3.ProjectOnPlane(shieldDraw.forward, Vector3.up).normalized;
            if (front.sqrMagnitude < 0.99f)
            {
                throw new InvalidOperationException("Shield_Draw has no valid horizontal forward direction.");
            }

            // Match the established player-animation review start distance.
            const float reviewDistance = 2f;
            Vector3 desiredCamera = bounds.center + front * reviewDistance;
            Quaternion desiredYaw = Quaternion.LookRotation(-front, Vector3.up);
            Vector3 cameraOffsetLocal = player.InverseTransformPoint(playerCamera.transform.position);
            Vector3 desiredPlayer = desiredCamera - desiredYaw * cameraOffsetLocal;
            desiredPlayer.y = player.position.y;
            player.SetPositionAndRotation(desiredPlayer, desiredYaw);
            EditorUtility.SetDirty(player);
            PrefabUtility.RecordPrefabInstancePropertyModifications(player);

            Vector3 horizontalView = Vector3.ProjectOnPlane(
                bounds.center - playerCamera.transform.position,
                Vector3.up).normalized;
            Vector3 horizontalCameraForward = Vector3.ProjectOnPlane(
                playerCamera.transform.forward,
                Vector3.up).normalized;
            report.AppendLine("shieldDrawPosition=" + shieldDraw.position.ToString("F6") +
                " shieldDrawForward=" + front.ToString("F6"));
            report.AppendLine("playerStartPosition=" + player.position.ToString("F6") +
                " playerStartForward=" + player.forward.ToString("F6") +
                " cameraHorizontalDistance=" + Vector3.Distance(
                    Vector3.ProjectOnPlane(playerCamera.transform.position, Vector3.up),
                    Vector3.ProjectOnPlane(bounds.center, Vector3.up)).ToString("F6") +
                " cameraHorizontalAlignment=" + Vector3.Dot(horizontalCameraForward, horizontalView).ToString("F6"));
        }

        internal static void StartReview()
        {
            if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
        }

        internal static void StopReview()
        {
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        }

        internal static void Capture(string outputPath = null)
        {
            Directory.CreateDirectory(Output);
            if (!string.IsNullOrEmpty(outputPath))
            {
                CaptureFinal(outputPath);
                return;
            }
            if (EditorApplication.isPlaying)
            {
                captureFinalOutputPath = null;
                BeginLiveCapture();
                return;
            }
            string label = EditorApplication.isPlaying ? "live" : "before";
            foreach (string name in Targets)
            {
                var root = Find(name);
                Render(root, root.position + root.up * 0.9f, root.forward, 1.15f, Output + "/" + label + "_" + name + "_front.png");
                Render(root, root.position + root.up * 0.9f, root.right, 1.15f, Output + "/" + label + "_" + name + "_side.png");
            }
            Debug.Log("Actual current dagger objects captured without posing or modifying them.");
        }

        private static double observationEnd;
        private static DaggerPropMotion[] observedProps;
        private static float[] observedGripError;
        private static float[] observedDirectionError;
        private static float observedLandingDistance;
        private static float observedLandingRightOffset;
        private static float observedMaximumFlightRightOffset;
        private static int observedLandings;
        private static float observedCancelEndError;
        private static float observedFlightTangentError;
        private static float observedFlightDirectionChange;
        private static DaggerStabAccelerationMotion observedStabAcceleration;
        private static DaggerStabPoseTransitionMotion observedStabPoseTransition;
        private static int observedStabTransitionStartedBaseline;
        private static int observedStabTransitionCompletedBaseline;
        private static float observedStabPreviousNormalizedTime;
        private static float observedStabPreviousPatternTime;
        private static float observedStabPreviousPlaybackSpeed;
        private static float observedStabMinimumAttackInterval;
        private static float observedStabMaximumAttackInterval;
        private static float observedStabMinimumPlaybackSpeed;
        private static float observedStabMaximumPlaybackSpeed;
        private static float observedStabLastLoopRealtime;
        private static float observedStabMinimumLoopInterval;
        private static float observedStabMaximumLoopInterval;
        private static int observedStabLoops;
        private static int observedStabMeasuredLoopIntervals;
        private static int observedStabPatternWraps;
        private static int observedStabRampRegressionSamples;
        private static int observedStabBackArcSamples;

        private static void BeginRuntimeObservation()
        {
            observedProps = Targets.Select(Find).Select(t => t.GetComponent<DaggerPropMotion>()).ToArray();
            observedGripError = new float[Targets.Length];
            observedDirectionError = new float[Targets.Length];
            observedLandingDistance = 0f;
            observedLandingRightOffset = 0f;
            observedMaximumFlightRightOffset = 0f;
            observedLandings = 0;
            observedCancelEndError = 0f;
            observedFlightTangentError = 0f;
            observedFlightDirectionChange = 0f;
            observedStabAcceleration = observedProps[1].GetComponent<DaggerStabAccelerationMotion>();
            observedStabPoseTransition = observedProps[1].GetComponent<DaggerStabPoseTransitionMotion>();
            observedStabTransitionStartedBaseline = observedStabPoseTransition.StartedTransitions;
            observedStabTransitionCompletedBaseline = observedStabPoseTransition.CompletedTransitions;
            observedStabPreviousNormalizedTime = Mathf.Repeat(
                observedProps[1].animator.GetCurrentAnimatorStateInfo(0).normalizedTime,
                1f);
            observedStabPreviousPatternTime = observedStabAcceleration.PatternElapsed;
            observedStabPreviousPlaybackSpeed = observedStabAcceleration.CurrentPlaybackSpeed;
            observedStabMinimumAttackInterval = float.PositiveInfinity;
            observedStabMaximumAttackInterval = 0f;
            observedStabMinimumPlaybackSpeed = float.PositiveInfinity;
            observedStabMaximumPlaybackSpeed = 0f;
            observedStabLastLoopRealtime = -1f;
            observedStabMinimumLoopInterval = float.PositiveInfinity;
            observedStabMaximumLoopInterval = 0f;
            observedStabLoops = 0;
            observedStabMeasuredLoopIntervals = 0;
            observedStabPatternWraps = 0;
            observedStabRampRegressionSamples = 0;
            observedStabBackArcSamples = 0;
            observationEnd = EditorApplication.timeSinceStartup + 12d;
            EditorApplication.update -= ObserveTick;
            EditorApplication.update += ObserveTick;
        }

        private static void ObserveTick()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.update -= ObserveTick;
                return;
            }
            for (int index = 0; index < observedProps.Length; index++)
            {
                var prop = observedProps[index];
                float normalizedTime = Mathf.Repeat(prop.animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f);
                if (index == 1)
                {
                    if (normalizedTime + 0.01f < observedStabPreviousNormalizedTime)
                    {
                        observedStabLoops++;
                        float now = Time.realtimeSinceStartup;
                        if (observedStabLastLoopRealtime >= 0f)
                        {
                            float interval = now - observedStabLastLoopRealtime;
                            observedStabMinimumLoopInterval = Mathf.Min(observedStabMinimumLoopInterval, interval);
                            observedStabMaximumLoopInterval = Mathf.Max(observedStabMaximumLoopInterval, interval);
                            observedStabMeasuredLoopIntervals++;
                        }
                        observedStabLastLoopRealtime = now;
                    }
                    observedStabPreviousNormalizedTime = normalizedTime;
                    if (normalizedTime >= StabBackArcStartNormalizedTime && normalizedTime < StabResumeNormalizedTime)
                        observedStabBackArcSamples++;
                    float patternTime = observedStabAcceleration.PatternElapsed;
                    float playbackSpeed = observedStabAcceleration.CurrentPlaybackSpeed;
                    if (patternTime + 0.05f < observedStabPreviousPatternTime)
                        observedStabPatternWraps++;
                    else if (patternTime <= StabAccelerationDurationSeconds &&
                             observedStabPreviousPatternTime <= StabAccelerationDurationSeconds &&
                             playbackSpeed + 0.0001f < observedStabPreviousPlaybackSpeed)
                        observedStabRampRegressionSamples++;
                    observedStabPreviousPatternTime = patternTime;
                    observedStabPreviousPlaybackSpeed = playbackSpeed;
                    observedStabMinimumAttackInterval = Mathf.Min(observedStabMinimumAttackInterval, observedStabAcceleration.CurrentAttackInterval);
                    observedStabMaximumAttackInterval = Mathf.Max(observedStabMaximumAttackInterval, observedStabAcceleration.CurrentAttackInterval);
                    observedStabMinimumPlaybackSpeed = Mathf.Min(observedStabMinimumPlaybackSpeed, playbackSpeed);
                    observedStabMaximumPlaybackSpeed = Mathf.Max(observedStabMaximumPlaybackSpeed, playbackSpeed);
                }
                if (!prop.IsReleased)
                {
                    Vector3 expectedGrip = prop.rightHand.TransformPoint(prop.idleLocalPosition + prop.idleLocalRotation * Vector3.Scale(prop.originalLocalScale, prop.handleCenter));
                    observedGripError[index] = Mathf.Max(observedGripError[index], Vector3.Distance(expectedGrip, prop.dagger.TransformPoint(prop.handleCenter)));
                    float expectedElevation = prop.action == DaggerPropMotion.PropAction.Idle ? 45f : prop.bladeElevation;
                    Vector3 direction = prop.transform.forward * Mathf.Cos(expectedElevation * Mathf.Deg2Rad) + prop.transform.up * Mathf.Sin(expectedElevation * Mathf.Deg2Rad);
                    if (prop.action == DaggerPropMotion.PropAction.Idle || prop.action == DaggerPropMotion.PropAction.Aim)
                        observedDirectionError[index] = Mathf.Max(observedDirectionError[index], Vector3.Angle(prop.dagger.TransformDirection(prop.bladeAxis), direction));
                    if (prop.action == DaggerPropMotion.PropAction.Cancel && normalizedTime > 0.97f)
                    {
                        Vector3 restingDirection = prop.transform.forward * Mathf.Cos(45f * Mathf.Deg2Rad) +
                                                   prop.transform.up * Mathf.Sin(45f * Mathf.Deg2Rad);
                        observedCancelEndError = Mathf.Max(
                            observedCancelEndError,
                            Vector3.Angle(prop.dagger.TransformDirection(prop.bladeAxis), restingDirection));
                        observedDirectionError[index] = Mathf.Max(observedDirectionError[index], observedCancelEndError);
                    }
                }
                else
                {
                    float rightOffset = Vector3.Dot(
                        prop.dagger.position - prop.ReleasePosition,
                        prop.transform.right);
                    observedMaximumFlightRightOffset = Mathf.Max(
                        observedMaximumFlightRightOffset,
                        Mathf.Abs(rightOffset));
                    if (prop.FlightVelocity.sqrMagnitude > 0.01f)
                    {
                        float tangentError = Vector3.Angle(
                            prop.dagger.TransformDirection(prop.bladeAxis),
                            prop.FlightVelocity);
                        observedDirectionError[index] = Mathf.Max(observedDirectionError[index], tangentError);
                        observedFlightTangentError = Mathf.Max(observedFlightTangentError, tangentError);
                        observedFlightDirectionChange = Mathf.Max(
                            observedFlightDirectionChange,
                            Vector3.Angle(prop.LaunchVelocity, prop.FlightVelocity));
                    }
                    if (prop.FlightElapsed >= prop.FlightDuration)
                    {
                        observedLandingDistance = Vector3.Dot(prop.dagger.position - prop.ReleasePosition, prop.transform.forward);
                        observedLandingRightOffset = rightOffset;
                        observedLandings++;
                    }
                }
            }
            if (EditorApplication.timeSinceStartup < observationEnd) return;
            EditorApplication.update -= ObserveTick;
            var report = new StringBuilder("Read-only actual playback observation.\n");
            for (int index = 0; index < Targets.Length; index++) report.AppendLine(Targets[index] + " maxGripErrorMeters=" + observedGripError[index].ToString("F8") + " maxExpectedBladeDirectionErrorDegrees=" + observedDirectionError[index].ToString("F6"));
            report.AppendLine("landingForwardDistanceMeters=" + observedLandingDistance.ToString("F6") +
                " landingRightOffsetMeters=" + observedLandingRightOffset.ToString("F6") +
                " maximumAbsoluteFlightRightOffsetMeters=" + observedMaximumFlightRightOffset.ToString("F6") +
                " landedObservations=" + observedLandings);
            report.AppendLine("throwMaximumInstantaneousTangentErrorDegrees=" + observedFlightTangentError.ToString("F6") +
                " throwObservedDirectionChangeDegrees=" + observedFlightDirectionChange.ToString("F6"));
            report.AppendLine("cancelLastThreePercentMaximumRestoredAngleErrorDegrees=" + observedCancelEndError.ToString("F6"));
            report.AppendLine("stabForbiddenBackArcSamples=" + observedStabBackArcSamples +
                " stabObservedLoops=" + observedStabLoops +
                " forbiddenBackArcNormalizedRange=" + StabBackArcStartNormalizedTime.ToString("F6") + ".." + StabResumeNormalizedTime.ToString("F6"));
            report.AppendLine("stabPatternDurationSeconds=" + StabPatternDurationSeconds.ToString("F6") +
                " stabAccelerationDurationSeconds=" + StabAccelerationDurationSeconds.ToString("F6") +
                " observedPatternWraps=" + observedStabPatternWraps +
                " rampSpeedRegressionSamples=" + observedStabRampRegressionSamples);
            report.AppendLine("stabConfiguredIntervalObservedRangeSeconds=" + observedStabMinimumAttackInterval.ToString("F6") + ".." + observedStabMaximumAttackInterval.ToString("F6") +
                " playbackSpeedObservedRange=" + observedStabMinimumPlaybackSpeed.ToString("F6") + ".." + observedStabMaximumPlaybackSpeed.ToString("F6"));
            report.AppendLine("stabActualLoopIntervalObservedRangeSeconds=" + observedStabMinimumLoopInterval.ToString("F6") + ".." + observedStabMaximumLoopInterval.ToString("F6") +
                " measuredIntervals=" + observedStabMeasuredLoopIntervals);
            report.AppendLine("stabPoseTransitionConfiguredDurationSeconds=" + observedStabPoseTransition.TransitionDurationSeconds.ToString("F6") +
                " observedStarts=" + (observedStabPoseTransition.StartedTransitions - observedStabTransitionStartedBaseline) +
                " observedCompletions=" + (observedStabPoseTransition.CompletedTransitions - observedStabTransitionCompletedBaseline) +
                " lastCompletedDurationSeconds=" + observedStabPoseTransition.LastCompletedDurationSeconds.ToString("F6"));
            File.WriteAllText(Output + "/runtime_observation.txt", report.ToString(), Encoding.UTF8);
        }

        private static void CaptureFinal(string path)
        {
            if (path.Replace('\\', '/') != FinalMotionCorrectionsPath) throw new InvalidOperationException("Unexpected final capture path.");
            if (File.Exists(path)) throw new InvalidOperationException("Final capture already exists; not repeated.");
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Actual animation must be playing.");
            captureFinalOutputPath = path;
            BeginLiveCapture();
            Debug.Log("One final unmodified live-playback capture started: " + path);
        }

        private static readonly float[] CapturePhases = { 0.02f, 0.18f, 0.36f, 0.50f, 0.70f, 0.90f, 0.985f };
        private static Transform[] captureTargets;
        private static int[] captureCycles;
        private static int[] captureFrames;
        private static float[] capturePreviousNormalizedTimes;
        private static string captureDirectory;
        private static string captureFinalOutputPath;
        private static StringBuilder captureReport;
        private static double captureDeadline;

        private static void BeginLiveCapture()
        {
            EditorApplication.update -= CaptureTick;
            captureTargets = Targets.Select(Find).ToArray();
            captureCycles = captureTargets.Select(t => Mathf.FloorToInt(t.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime) + 1).ToArray();
            captureFrames = new int[Targets.Length];
            capturePreviousNormalizedTimes = captureTargets
                .Select(t => Mathf.Repeat(t.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime, 1f))
                .ToArray();
            captureCycles[1] = -1;
            captureDirectory = Output + "/playback_" + DateTime.Now.ToString("HHmmss");
            Directory.CreateDirectory(captureDirectory);
            captureReport = new StringBuilder();
            captureDeadline = EditorApplication.timeSinceStartup + 55d;
            EditorApplication.update += CaptureTick;
            Debug.Log("Unmodified live playback capture started: " + captureDirectory);
        }

        private static void CaptureTick()
        {
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup > captureDeadline)
            {
                EditorApplication.update -= CaptureTick;
                File.WriteAllText(captureDirectory + "/incomplete.txt", captureReport.ToString(), Encoding.UTF8);
                return;
            }
            for (int index = 0; index < captureTargets.Length; index++)
            {
                int frame = captureFrames[index];
                if (frame >= CapturePhases.Length) continue;
                var root = captureTargets[index];
                var animator = root.GetComponent<Animator>();
                float time = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                float capturePhase = CapturePhases[frame];
                if (index == 1)
                {
                    time = Mathf.Repeat(time, 1f);
                    if (time + 0.01f < capturePreviousNormalizedTimes[index]) captureCycles[index]++;
                    capturePreviousNormalizedTimes[index] = time;
                    if (captureCycles[index] < 0) continue;

                    var poseTransition = root.GetComponent<DaggerStabPoseTransitionMotion>();
                    bool ready;
                    switch (frame)
                    {
                        case 0:
                            ready = !poseTransition.IsTransitioning && time >= 0.05f && time <= 0.11f;
                            break;
                        case 1:
                            ready = !poseTransition.IsTransitioning && time >= 0.20f && time <= 0.26f;
                            break;
                        case 2:
                            ready = !poseTransition.IsTransitioning && time >= 0.34f && time < StabSkipTriggerNormalizedTime;
                            break;
                        case 3:
                            ready = poseTransition.IsTransitioning && poseTransition.TransitionProgress >= 0.10f && poseTransition.TransitionProgress <= 0.28f;
                            break;
                        case 4:
                            ready = poseTransition.IsTransitioning && poseTransition.TransitionProgress >= 0.38f && poseTransition.TransitionProgress <= 0.58f;
                            break;
                        case 5:
                            ready = poseTransition.IsTransitioning && poseTransition.TransitionProgress >= 0.68f && poseTransition.TransitionProgress <= 0.88f;
                            break;
                        default:
                            ready = !poseTransition.IsTransitioning && time >= 0.90f && time <= 0.99f;
                            break;
                    }
                    if (!ready) continue;
                }
                else
                {
                    if (Mathf.FloorToInt(time) > captureCycles[index]) captureCycles[index] = Mathf.FloorToInt(time);
                    if (time < captureCycles[index] + capturePhase) continue;
                    if (time - captureCycles[index] > capturePhase + 0.045f)
                    {
                        captureCycles[index]++;
                        continue;
                    }
                }
                string prefix = captureDirectory + "/" + root.name + "_" + frame;
                Render(root, root.position + root.up * 0.9f, root.forward, 1.15f, prefix + "_front.png");
                Render(root, root.position + root.up * 0.9f, root.right, 1.15f, prefix + "_side.png");
                var motion = root.GetComponent<DaggerPropMotion>();
                Render(root, motion.rightHand.position, root.right, 0.39f, prefix + "_hand.png");
                if (index == 3)
                {
                    Vector3 flightCenter = root.position + root.forward * 3.4f + root.up * 1.25f;
                    Render(root, flightCenter, root.right, 4.4f, prefix + "_flight.png");
                    Render(
                        root,
                        flightCenter,
                        root.up,
                        4.4f,
                        prefix + "_flight_top.png",
                        root.forward,
                        6f,
                        12f);
                }
                RenderOriginal(index, time, prefix + "_source.png");
                string stabTiming = index == 1
                    ? " pattern=" + root.GetComponent<DaggerStabAccelerationMotion>().PatternElapsed.ToString("F6") +
                      " interval=" + root.GetComponent<DaggerStabAccelerationMotion>().CurrentAttackInterval.ToString("F6") +
                      " speed=" + root.GetComponent<DaggerStabAccelerationMotion>().CurrentPlaybackSpeed.ToString("F6") +
                      " transitionActive=" + root.GetComponent<DaggerStabPoseTransitionMotion>().IsTransitioning +
                      " transitionProgress=" + root.GetComponent<DaggerStabPoseTransitionMotion>().TransitionProgress.ToString("F6")
                    : string.Empty;
                captureReport.AppendLine(root.name + " frame=" + frame + " normalized=" + time.ToString("F6") + stabTiming + " released=" + motion.IsReleased + " flight=" + motion.FlightElapsed + "/" + motion.FlightDuration + " dagger=" + motion.dagger.position.ToString("F6"));
                captureFrames[index]++;
            }
            if (captureFrames.Any(frame => frame < CapturePhases.Length)) return;
            EditorApplication.update -= CaptureTick;
            File.WriteAllText(captureDirectory + "/complete.txt", captureReport.ToString(), Encoding.UTF8);
            foreach (string name in Targets)
            {
                string[] paths = name == "Dagger_Stab"
                    ? Enumerable.Range(0, CapturePhases.Length).SelectMany(frame => new[]
                    {
                        captureDirectory + "/" + name + "_" + frame + "_source.png",
                        captureDirectory + "/" + name + "_" + frame + "_front.png",
                        captureDirectory + "/" + name + "_" + frame + "_side.png",
                        captureDirectory + "/" + name + "_" + frame + "_hand.png"
                    }).ToArray()
                    : Enumerable.Range(0, CapturePhases.Length).SelectMany(frame => new[]
                    {
                        captureDirectory + "/" + name + "_" + frame + "_source.png",
                        captureDirectory + "/" + name + "_" + frame + "_side.png"
                    }).ToArray();
                Compose(paths, name == "Dagger_Stab" ? 4 : 2, captureDirectory + "/" + name + "_comparison.png");
            }
            Compose(Enumerable.Range(0, CapturePhases.Length).Select(frame => captureDirectory + "/Dagger_Throw_Release_" + frame + "_flight.png").ToArray(), 3, captureDirectory + "/flight_sequence.png");
            Compose(Enumerable.Range(0, CapturePhases.Length).Select(frame => captureDirectory + "/Dagger_Throw_Release_" + frame + "_flight_top.png").ToArray(), 3, captureDirectory + "/flight_top_sequence.png");
            if (!string.IsNullOrEmpty(captureFinalOutputPath))
            {
                string playerStartViewPath = captureDirectory + "/Shield_Draw_player_start.png";
                CapturePlayerCameraView(playerStartViewPath);
                string[] finalPaths =
                {
                    captureDirectory + "/Dagger_Throw_Release_2_flight_top.png",
                    captureDirectory + "/Dagger_Throw_Release_3_flight_top.png",
                    captureDirectory + "/Dagger_Throw_Release_4_flight_top.png",
                    captureDirectory + "/Dagger_Throw_Release_5_flight_top.png",
                    captureDirectory + "/Dagger_Throw_Release_6_flight_top.png",
                    captureDirectory + "/Dagger_Throw_Release_2_flight.png",
                    captureDirectory + "/Dagger_Throw_Release_3_flight.png",
                    captureDirectory + "/Dagger_Throw_Release_4_flight.png",
                    captureDirectory + "/Dagger_Throw_Release_5_flight.png",
                    captureDirectory + "/Dagger_Throw_Release_6_flight.png",
                    playerStartViewPath
                };
                Compose(finalPaths, 5, captureFinalOutputPath);
                Debug.Log("One final unmodified live-playback capture saved: " + captureFinalOutputPath);
                captureFinalOutputPath = null;
            }
            Debug.Log("Actual dagger playback sequence capture complete: " + captureDirectory);
        }

        private static void CapturePlayerCameraView(string path)
        {
            var playerCamera = Find("Player").GetComponentsInChildren<Camera>(true).Single();
            var render = RenderTexture.GetTemporary(640, 720, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(640, 720, TextureFormat.RGB24, false);
            var previousActive = RenderTexture.active;
            var previousTarget = playerCamera.targetTexture;
            try
            {
                playerCamera.targetTexture = render;
                playerCamera.Render();
                RenderTexture.active = render;
                pixels.ReadPixels(new Rect(0, 0, 640, 720), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                playerCamera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(pixels);
            }
        }

        private static void RenderOriginal(int index, float normalizedTime, string path)
        {
            if (index != 1)
            {
                string[] names = { "Hands_Carry_OneHand", null, "Hands_Throw_Ready", "Hands_Throw_Release", "Hands_Throw_Cancel" };
                var source = Find(names[index]);
                Render(source, source.position + source.up * 0.9f, source.right, 1.15f, path);
                return;
            }
            // The comparison reference alone is sampled from its FBX; the actual validation target is never posed.
            var reference = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(StabSource));
            reference.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var target = captureTargets[index];
                reference.transform.SetPositionAndRotation(new Vector3(2000f, 0f, 2000f), target.rotation);
                var clip = AssetDatabase.LoadAllAssetsAtPath(StabSource).OfType<AnimationClip>().Single(c => !c.name.StartsWith("__preview__"));
                clip.SampleAnimation(reference, Mathf.Repeat(normalizedTime, 1f) * clip.length);
                Render(reference.transform, reference.transform.position + reference.transform.up * 0.9f, reference.transform.right, 1.15f, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(reference); }
        }

        private static void Compose(string[] paths, int columns, string outputPath)
        {
            const int width = 400, height = 450;
            int rows = (paths.Length + columns - 1) / columns;
            var sheet = new Texture2D(width * columns, height * rows, TextureFormat.RGB24, false);
            try
            {
                for (int index = 0; index < paths.Length; index++)
                {
                    var input = new Texture2D(2, 2, TextureFormat.RGB24, false);
                    try
                    {
                        input.LoadImage(File.ReadAllBytes(paths[index]));
                        var colors = new Color[width * height];
                        for (int y = 0; y < height; y++)
                            for (int x = 0; x < width; x++) colors[y * width + x] = input.GetPixelBilinear((x + 0.5f) / width, (y + 0.5f) / height);
                        sheet.SetPixels((index % columns) * width, (rows - 1 - index / columns) * height, width, height, colors);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(input); }
                }
                sheet.Apply();
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(sheet); }
        }

        private static Texture2D Render(
            Transform root,
            Vector3 center,
            Vector3 side,
            float size,
            string path,
            Vector3 cameraUp = default(Vector3),
            float cameraDistance = 1.2f,
            float farClip = 2.3f)
        {
            var temporary = new GameObject("DaggerReadOnlyCamera", typeof(Camera));
            temporary.hideFlags = HideFlags.HideAndDontSave;
            var camera = temporary.GetComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = size;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.11f, 0.13f, 0.16f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = farClip;
            if (cameraUp.sqrMagnitude < 0.001f) cameraUp = root.up;
            camera.transform.SetPositionAndRotation(
                center + side * cameraDistance,
                Quaternion.LookRotation(-side, cameraUp));
            var render = RenderTexture.GetTemporary(640, 720, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(640, 720, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = render;
                camera.Render();
                RenderTexture.active = render;
                pixels.ReadPixels(new Rect(0, 0, 640, 720), 0, 0);
                pixels.Apply();
                if (path != null) File.WriteAllBytes(path, pixels.EncodeToPNG());
                return path == null ? pixels : null;
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                if (path != null) UnityEngine.Object.DestroyImmediate(pixels);
                UnityEngine.Object.DestroyImmediate(temporary);
            }
        }
    }
}
