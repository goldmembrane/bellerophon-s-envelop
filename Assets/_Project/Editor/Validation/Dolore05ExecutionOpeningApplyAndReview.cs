using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Dolore05ExecutionOpening
{
    internal static class Dolore05ExecutionOpeningApplyAndReview
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Dolore Enemy Placement";
        private const string AttackSlotName = "Dolore_04_Tentacle_Stab_Attack";
        private const string ExecutionSlotName = "Dolore_05_Execution_Pull_In";
        private const string ModelName = "Dolore_Model";
        private const string AttachmentName = "Dolore_Attack_Attachment";
        private const string SourceName = "Dolore_Attack_Source";
        private const string RootBoneName = "Bone_000";
        private const string TipBoneName = "Bone_001";
        private const int ExpectedBoneCount = 13;
        private const float IntroDuration = 2f;
        // The execution branch currently ends at the completed first impact hold. Pull-in is authored later.
        private const float PierceHoldEndTime = 0.58f;
        private const float TransformTolerance = 0.00001f;
        private const float CurveTolerance = 0.00001f;

        private const string AssetRoot =
            "Assets/_Project/Art/Generated/Enemies/Dolore/AttackAttachment";
        private const string AnimationFolder = AssetRoot + "/Animations";
        private const string ReviewFolder = AssetRoot + "/Review";
        private const string AttackIntroPath = AnimationFolder + "/Dolore_04_TentacleStab_Intro.anim";
        private const string AttackLoopPath = AnimationFolder + "/Dolore_04_TentacleStab_AttackLoop.anim";
        private const string ExecutionIntroPath = AnimationFolder + "/Dolore_05_ExecutionPullIn_Intro.anim";
        private const string ExecutionPiercePath = AnimationFolder + "/Dolore_05_ExecutionPullIn_PierceHold.anim";
        private const string ExecutionControllerPath = AnimationFolder + "/Dolore_05_ExecutionPullIn.controller";
        private const string InspectionPath = ReviewFolder + "/Dolore_05_ExecutionPullIn_Opening_Inspection.txt";

        private static readonly string[] ExpectedSlotNames =
        {
            "Dolore_01_Static_Review",
            "Dolore_02_Idle",
            "Dolore_03_Move_Quadruped",
            AttackSlotName,
            ExecutionSlotName,
            "Dolore_06_Hit_Reaction",
            "Dolore_07_Death"
        };

        [MenuItem("Bellerophon/Enemies/Dolore/Apply Motion 4 Execution Opening")]
        public static void ApplyAnimation()
        {
            var scene = RequireActiveScene();
            if (scene.isDirty)
                throw new InvalidOperationException(
                    "CargoRunMvp contains unsaved changes. The execution animation tool will not overwrite them.");

            var slots = RequireSlots(scene);
            var attack = RequireTarget(slots[3]);
            var execution = RequireTarget(slots[4]);
            RequireMatchingAttachment(attack, execution);

            var protectedRootsBefore = ProtectedRootSignatures(scene);
            var protectedSlotsBefore = ProtectedSlotSignatures(slots);
            var executionBaseBefore = HierarchySignature(RequireModel(slots[4]), AttachmentName);
            var attachmentBefore = TransformSignature(execution.Attachment);
            var sourceBefore = TransformSignature(execution.Source);
            var rootBoneBefore = TransformSignature(execution.RootBone);

            EnsureFolder(AnimationFolder);
            EnsureFolder(ReviewFolder);
            var attackIntro = RequireAsset<AnimationClip>(AttackIntroPath);
            var attackLoop = RequireAsset<AnimationClip>(AttackLoopPath);
            var intro = CloneClip(attackIntro, ExecutionIntroPath, "Dolore_05_ExecutionPullIn_Intro");
            var pierce = CopyClipPrefix(
                attackLoop,
                ExecutionPiercePath,
                "Dolore_05_ExecutionPullIn_PierceHold",
                PierceHoldEndTime);
            var controller = CreateOrUpdateController(intro, pierce);

            var animator = execution.Attachment.GetComponent<Animator>();
            if (animator == null) animator = execution.Attachment.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);

            execution.Renderer.localBounds = attack.Renderer.localBounds;
            execution.Renderer.updateWhenOffscreen = false;
            EditorUtility.SetDirty(execution.Renderer);

            if (TransformSignature(execution.Attachment) != attachmentBefore ||
                TransformSignature(execution.Source) != sourceBefore ||
                TransformSignature(execution.RootBone) != rootBoneBefore)
                throw new InvalidOperationException("The approved execution tentacle start transform changed.");
            if (HierarchySignature(RequireModel(slots[4]), AttachmentName) != executionBaseBefore)
                throw new InvalidOperationException("The Dolore execution base model changed.");
            if (!protectedRootsBefore.SequenceEqual(ProtectedRootSignatures(scene), StringComparer.Ordinal))
                throw new InvalidOperationException("A scene root outside Approved Dolore Enemy Placement changed.");
            if (!protectedSlotsBefore.SequenceEqual(ProtectedSlotSignatures(slots), StringComparer.Ordinal))
                throw new InvalidOperationException("A Dolore slot outside motion object 4 changed.");
            RequireMatchingAttachment(RequireTarget(slots[3]), RequireTarget(slots[4]));

            var metrics = InspectState(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp could not be saved.");
            AssetDatabase.SaveAssets();
            WriteInspection(metrics, "Apply", true);
            Debug.Log(
                "Dolore05ExecutionOpeningApplied Result=PASS IntroSeconds=2 PierceHoldSeconds=0.58 " +
                "SourceMotion=Dolore04TentacleStab FirstImpactOnly=True PullInAuthored=False " +
                "OtherDoloreSlotsChanged=False OtherSceneRootsChanged=False SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Inspect Motion 4 Execution Opening")]
        public static void InspectAnimation()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var metrics = InspectState(scene);
            WriteInspection(metrics, "Inspect", false);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Execution opening inspection changed CargoRunMvp.");
            Debug.Log(
                "Dolore05ExecutionOpeningInspected Result=PASS IntroSeconds=" + Num(metrics.IntroLength) +
                " PierceHoldSeconds=" + Num(metrics.PierceLength) +
                " IntroCurveDifference=" + Num(metrics.IntroCurveDifference) +
                " PierceCurveDifference=" + Num(metrics.PierceCurveDifference) +
                " IntroRise=" + Num(metrics.IntroRise) +
                " WindupLift=" + Num(metrics.WindupLift) +
                " WindupRetreat=" + Num(metrics.WindupRetreat) +
                " LateStrikeOutward=" + Num(metrics.LateStrikeOutward) +
                " StrikeForward=" + Num(metrics.StrikeForward) +
                " StrikeDrop=" + Num(metrics.StrikeDrop) +
                " StrikeLateral=" + Num(metrics.StrikeLateral) +
                " ImpactHoldError=" + Num(metrics.ImpactHoldError) +
                " RootDrift=" + Num(metrics.RootDrift) +
                " SurfaceAnchorDrift=" + Num(metrics.SurfaceAnchorDrift) +
                " SceneChanged=False.");
        }

        private static AnimationClip CloneClip(AnimationClip source, string path, string clipName)
        {
            var destination = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (destination == null)
            {
                destination = new AnimationClip();
                AssetDatabase.CreateAsset(destination, path);
            }
            EditorUtility.CopySerialized(source, destination);
            destination.name = clipName;
            destination.wrapMode = WrapMode.Once;
            var settings = AnimationUtility.GetAnimationClipSettings(destination);
            settings.loopTime = false;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(destination, settings);
            EditorUtility.SetDirty(destination);
            return destination;
        }

        private static AnimationClip CopyClipPrefix(
            AnimationClip source,
            string path,
            string clipName,
            float endTime)
        {
            var destination = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (destination == null)
            {
                destination = new AnimationClip();
                AssetDatabase.CreateAsset(destination, path);
            }
            destination.ClearCurves();
            destination.name = clipName;
            destination.frameRate = source.frameRate;
            destination.wrapMode = WrapMode.Once;
            var settings = AnimationUtility.GetAnimationClipSettings(source);
            settings.loopTime = false;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(destination, settings);

            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var curve = AnimationUtility.GetEditorCurve(source, binding) ??
                            throw new InvalidOperationException("Source animation curve is missing.");
                AnimationUtility.SetEditorCurve(destination, binding, TruncateCurve(curve, endTime));
            }
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(source, binding)
                    .Where(item => item.time <= endTime + CurveTolerance)
                    .ToArray();
                if (keys.Length > 0) AnimationUtility.SetObjectReferenceCurve(destination, binding, keys);
            }
            AnimationUtility.SetAnimationEvents(
                destination,
                AnimationUtility.GetAnimationEvents(source)
                    .Where(item => item.time <= endTime + CurveTolerance)
                    .ToArray());
            destination.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(destination);
            return destination;
        }

        private static AnimationCurve TruncateCurve(AnimationCurve source, float endTime)
        {
            var keys = source.keys.Where(item => item.time <= endTime + CurveTolerance).ToList();
            if (keys.Count == 0 || keys[0].time > CurveTolerance)
                keys.Insert(0, new Keyframe(0f, source.Evaluate(0f)));
            if (Mathf.Abs(keys[keys.Count - 1].time - endTime) > CurveTolerance)
                keys.Add(new Keyframe(endTime, source.Evaluate(endTime)));
            var result = new AnimationCurve(keys.ToArray())
            {
                preWrapMode = WrapMode.ClampForever,
                postWrapMode = WrapMode.ClampForever
            };
            return result;
        }

        private static AnimatorController CreateOrUpdateController(AnimationClip intro, AnimationClip pierce)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ExecutionControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ExecutionControllerPath);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            var stateMachine = controller.layers[0].stateMachine;
            foreach (var child in stateMachine.states.ToArray()) stateMachine.RemoveState(child.state);
            foreach (var child in stateMachine.stateMachines.ToArray())
                stateMachine.RemoveStateMachine(child.stateMachine);

            var introState = stateMachine.AddState("Intro");
            introState.motion = intro;
            introState.speed = 1f;
            var pierceState = stateMachine.AddState("PierceHold");
            pierceState.motion = pierce;
            pierceState.speed = 1f;
            stateMachine.defaultState = introState;
            var transition = introState.AddTransition(pierceState);
            transition.hasExitTime = true;
            transition.exitTime = 1f;
            transition.hasFixedDuration = true;
            transition.duration = 0f;
            transition.offset = 0f;
            transition.canTransitionToSelf = false;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static Metrics InspectState(Scene scene)
        {
            var slots = RequireSlots(scene);
            var attack = RequireTarget(slots[3]);
            var execution = RequireTarget(slots[4]);
            RequireMatchingAttachment(attack, execution);
            var chain = RequireBoneChain(execution.Renderer);
            var attackIntro = RequireAsset<AnimationClip>(AttackIntroPath);
            var attackLoop = RequireAsset<AnimationClip>(AttackLoopPath);
            var intro = RequireAsset<AnimationClip>(ExecutionIntroPath);
            var pierce = RequireAsset<AnimationClip>(ExecutionPiercePath);
            var controller = RequireAsset<AnimatorController>(ExecutionControllerPath);
            var animator = execution.Attachment.GetComponent<Animator>() ??
                           throw new InvalidOperationException("The motion 4 execution attachment Animator is missing.");

            if (!animator.enabled || animator.runtimeAnimatorController != controller || animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                throw new InvalidOperationException("The motion 4 execution Animator settings changed.");
            if (Mathf.Abs(intro.length - IntroDuration) > CurveTolerance ||
                Mathf.Abs(pierce.length - PierceHoldEndTime) > CurveTolerance)
                throw new InvalidOperationException("The execution opening clip timing changed.");
            if (AnimationUtility.GetAnimationClipSettings(intro).loopTime ||
                AnimationUtility.GetAnimationClipSettings(pierce).loopTime)
                throw new InvalidOperationException("Execution Intro and PierceHold must be non-looping.");
            RequireController(controller, intro, pierce);

            var introCurveDifference = MaximumCurveDifference(attackIntro, intro, IntroDuration);
            var pierceCurveDifference = MaximumCurveDifference(attackLoop, pierce, PierceHoldEndTime);
            if (introCurveDifference > CurveTolerance || pierceCurveDifference > CurveTolerance)
                throw new InvalidOperationException(
                    "Execution opening no longer matches motion 3. Intro=" + Num(introCurveDifference) +
                    " Pierce=" + Num(pierceCurveDifference));

            if (Vector3.Distance(execution.Renderer.localBounds.center, attack.Renderer.localBounds.center) >
                    TransformTolerance ||
                Vector3.Distance(execution.Renderer.localBounds.size, attack.Renderer.localBounds.size) >
                    TransformTolerance)
                throw new InvalidOperationException("The execution renderer does not use the approved animated bounds.");

            // Both review slots share the same frame rotation; reuse motion 3's approved frame-outward axis.
            var outward = ResolveOutwardDirection(scene, attack);
            var lateral = Vector3.Cross(Vector3.up, outward).normalized;
            var saved = chain.Select(BoneState.Capture).ToArray();
            var rendererEnabledBefore = execution.Renderer.enabled;
            var rootPositions = new List<Vector3>();
            var surfacePositions = new List<Vector3>();
            Vector3 introStartTip;
            Vector3 sourceReadyTip;
            Vector3 introPreparedTip;
            Vector3 preparedTip;
            Vector3 windupTip;
            Vector3 accelerationTip;
            Vector3 strikeTip;
            Vector3 impactHoldTip;
            bool introHiddenVisible;
            bool sourceRevealVisible;
            bool pierceVisible;
            try
            {
                Sample(intro, 0f, execution, chain, rootPositions, surfacePositions);
                introStartTip = chain[chain.Count - 1].position;
                introHiddenVisible = execution.Renderer.enabled;
                Sample(intro, 0.25f, execution, chain, rootPositions, surfacePositions);
                sourceRevealVisible = execution.Renderer.enabled;
                Sample(intro, 0.5f, execution, chain, rootPositions, surfacePositions);
                sourceReadyTip = chain[chain.Count - 1].position;
                foreach (var time in new[] { 0.8f, 1.05f, 1.18f, 1.30f, 1.55f, 1.78f })
                    Sample(intro, time, execution, chain, rootPositions, surfacePositions);
                Sample(intro, IntroDuration, execution, chain, rootPositions, surfacePositions);
                introPreparedTip = chain[chain.Count - 1].position;

                Sample(pierce, 0f, execution, chain, rootPositions, surfacePositions);
                preparedTip = chain[chain.Count - 1].position;
                pierceVisible = execution.Renderer.enabled;
                Sample(pierce, 0.28f, execution, chain, rootPositions, surfacePositions);
                windupTip = chain[chain.Count - 1].position;
                Sample(pierce, 0.38f, execution, chain, rootPositions, surfacePositions);
                accelerationTip = chain[chain.Count - 1].position;
                Sample(pierce, 0.5f, execution, chain, rootPositions, surfacePositions);
                strikeTip = chain[chain.Count - 1].position;
                Sample(pierce, PierceHoldEndTime, execution, chain, rootPositions, surfacePositions);
                impactHoldTip = chain[chain.Count - 1].position;
            }
            finally
            {
                Restore(chain, saved);
                execution.Renderer.enabled = rendererEnabledBefore;
            }

            var rootDrift = rootPositions.Max(item => Vector3.Distance(rootPositions[0], item));
            var surfaceAnchorDrift = surfacePositions.Max(item => Vector3.Distance(surfacePositions[0], item));
            var introRise = introPreparedTip.y - introStartTip.y;
            var preparedMatchError = Vector3.Distance(preparedTip, introPreparedTip);
            var windupLift = windupTip.y - preparedTip.y;
            var windupRetreat = Vector3.Dot(preparedTip - windupTip, outward);
            var lateStrikeOutward = Vector3.Dot(strikeTip - accelerationTip, outward);
            var strikeDelta = strikeTip - preparedTip;
            var strikeForward = Vector3.Dot(strikeDelta, outward);
            var strikeDrop = preparedTip.y - strikeTip.y;
            var strikeLateral = Mathf.Abs(Vector3.Dot(strikeDelta, lateral));
            var impactHoldError = Vector3.Distance(strikeTip, impactHoldTip);
            var sourceReadyOffset = Vector3.Distance(sourceReadyTip, AttackRootSurfaceCenter(execution.Renderer));

            if (introHiddenVisible || !sourceRevealVisible || !pierceVisible)
                throw new InvalidOperationException(
                    "Execution renderer visibility timing changed. Intro0=" + introHiddenVisible +
                    " Reveal=" + sourceRevealVisible + " Pierce=" + pierceVisible);
            if (introRise < 0.9f || preparedMatchError > TransformTolerance || windupLift < 0.1f ||
                windupRetreat < 0.05f || lateStrikeOutward < 0.35f || strikeForward < 1f ||
                strikeDrop < 1f || strikeLateral > 0.001f || impactHoldError > TransformTolerance ||
                rootDrift > TransformTolerance || surfaceAnchorDrift > 0.002f || sourceReadyOffset > 0.04f)
                throw new InvalidOperationException(
                    "Execution opening motion metrics failed. IntroRise=" + Num(introRise) +
                    " PreparedMatch=" + Num(preparedMatchError) +
                    " WindupLift=" + Num(windupLift) + " WindupRetreat=" + Num(windupRetreat) +
                    " LateOutward=" + Num(lateStrikeOutward) + " StrikeForward=" + Num(strikeForward) +
                    " StrikeDrop=" + Num(strikeDrop) + " StrikeLateral=" + Num(strikeLateral) +
                    " HoldError=" + Num(impactHoldError) + " RootDrift=" + Num(rootDrift) +
                    " SurfaceDrift=" + Num(surfaceAnchorDrift) + " SourceReadyOffset=" + Num(sourceReadyOffset));

            return new Metrics(
                intro.length,
                pierce.length,
                introCurveDifference,
                pierceCurveDifference,
                introRise,
                preparedMatchError,
                windupLift,
                windupRetreat,
                lateStrikeOutward,
                strikeForward,
                strikeDrop,
                strikeLateral,
                impactHoldError,
                rootDrift,
                surfaceAnchorDrift,
                sourceReadyOffset);
        }

        private static void Sample(
            AnimationClip clip,
            float time,
            Target target,
            IReadOnlyList<Transform> chain,
            ICollection<Vector3> rootPositions,
            ICollection<Vector3> surfacePositions)
        {
            clip.SampleAnimation(target.Attachment.gameObject, time);
            rootPositions.Add(chain[0].position);
            surfacePositions.Add(AttackRootSurfaceCenter(target.Renderer));
        }

        private static void RequireController(
            AnimatorController controller,
            AnimationClip intro,
            AnimationClip pierce)
        {
            if (controller.layers.Length != 1)
                throw new InvalidOperationException("Execution controller must have one layer.");
            var machine = controller.layers[0].stateMachine;
            var states = machine.states;
            var introState = states.SingleOrDefault(item => item.state.name == "Intro").state;
            var pierceState = states.SingleOrDefault(item => item.state.name == "PierceHold").state;
            if (introState == null || pierceState == null || machine.defaultState != introState ||
                introState.motion != intro || pierceState.motion != pierce)
                throw new InvalidOperationException("Execution controller states changed.");
            if (introState.transitions.Length != 1 || introState.transitions[0].destinationState != pierceState ||
                !introState.transitions[0].hasExitTime || Mathf.Abs(introState.transitions[0].exitTime - 1f) > CurveTolerance ||
                pierceState.transitions.Length != 0)
                throw new InvalidOperationException("Execution controller must stop at the first PierceHold impact.");
        }

        private static float MaximumCurveDifference(AnimationClip source, AnimationClip destination, float endTime)
        {
            var sourceBindings = AnimationUtility.GetCurveBindings(source)
                .ToDictionary(BindingKey, item => item, StringComparer.Ordinal);
            var destinationBindings = AnimationUtility.GetCurveBindings(destination)
                .ToDictionary(BindingKey, item => item, StringComparer.Ordinal);
            if (!sourceBindings.Keys.OrderBy(item => item, StringComparer.Ordinal)
                    .SequenceEqual(destinationBindings.Keys.OrderBy(item => item, StringComparer.Ordinal), StringComparer.Ordinal))
                throw new InvalidOperationException("Execution motion curve bindings differ from motion 3.");
            var maximum = 0f;
            foreach (var pair in sourceBindings)
            {
                var sourceCurve = AnimationUtility.GetEditorCurve(source, pair.Value);
                var destinationCurve = AnimationUtility.GetEditorCurve(destination, destinationBindings[pair.Key]);
                var times = sourceCurve.keys.Select(item => item.time)
                    .Concat(destinationCurve.keys.Select(item => item.time))
                    .Where(item => item <= endTime + CurveTolerance)
                    .Append(0f)
                    .Append(endTime)
                    .Distinct()
                    .ToArray();
                foreach (var time in times)
                    maximum = Mathf.Max(maximum, Mathf.Abs(sourceCurve.Evaluate(time) - destinationCurve.Evaluate(time)));
            }
            return maximum;
        }

        private static string BindingKey(EditorCurveBinding binding)
        {
            return binding.path + "|" + binding.type.AssemblyQualifiedName + "|" + binding.propertyName;
        }

        private static void WriteInspection(Metrics metrics, string phase, bool sceneSaved)
        {
            EnsureFolder(ReviewFolder);
            var report = new StringBuilder()
                .AppendLine("Result=PASS")
                .AppendLine("Phase=" + phase)
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("Target=" + PlacementRootName + "/" + ExecutionSlotName)
                .AppendLine("SourceMotion=" + PlacementRootName + "/" + AttackSlotName)
                .AppendLine("IntroClip=" + ExecutionIntroPath)
                .AppendLine("PierceHoldClip=" + ExecutionPiercePath)
                .AppendLine("Controller=" + ExecutionControllerPath)
                .AppendLine("IntroSeconds=" + Num(metrics.IntroLength))
                .AppendLine("PierceHoldSeconds=" + Num(metrics.PierceLength))
                .AppendLine("CopiedThroughFirstImpactHold=True")
                .AppendLine("PullInAuthored=False")
                .AppendLine("IntroCurveDifference=" + Num(metrics.IntroCurveDifference))
                .AppendLine("PierceCurveDifference=" + Num(metrics.PierceCurveDifference))
                .AppendLine("IntroTipRise=" + Num(metrics.IntroRise))
                .AppendLine("IntroToPiercePreparedError=" + Num(metrics.PreparedMatchError))
                .AppendLine("WindupLift=" + Num(metrics.WindupLift))
                .AppendLine("WindupRetreat=" + Num(metrics.WindupRetreat))
                .AppendLine("LateStrikeOutward=" + Num(metrics.LateStrikeOutward))
                .AppendLine("StrikeForwardTravel=" + Num(metrics.StrikeForward))
                .AppendLine("StrikeVerticalDrop=" + Num(metrics.StrikeDrop))
                .AppendLine("StrikeLateralTravel=" + Num(metrics.StrikeLateral))
                .AppendLine("ImpactHoldError=" + Num(metrics.ImpactHoldError))
                .AppendLine("RootDrift=" + Num(metrics.RootDrift))
                .AppendLine("ModeledSurfaceAnchorDrift=" + Num(metrics.SurfaceAnchorDrift))
                .AppendLine("SourceReadyTipOffset=" + Num(metrics.SourceReadyOffset))
                .AppendLine("BuiltInRigBones=13")
                .AppendLine("FixedAttachmentBones=Bone_000,Bone_012,Bone_011,Bone_010")
                .AppendLine("ControllerFlow=Intro->PierceHold")
                .AppendLine("PierceHoldHasOutgoingTransition=False")
                .AppendLine("AttackMotionAssetChanged=False")
                .AppendLine("OtherDoloreSlotsChanged=False")
                .AppendLine("OtherSceneRootsChanged=False")
                .AppendLine("SceneSaved=" + sceneSaved);
            File.WriteAllText(ProjectAbsolutePath(InspectionPath), report.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(
                InspectionPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static Scene RequireActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must already be the active scene.");
            return scene;
        }

        private static Transform[] RequireSlots(Scene scene)
        {
            var placement = scene.GetRootGameObjects().SingleOrDefault(item => item.name == PlacementRootName) ??
                            throw new InvalidOperationException("Approved Dolore placement root is missing.");
            if (placement.transform.childCount != ExpectedSlotNames.Length)
                throw new InvalidOperationException("Approved Dolore placement must contain exactly seven slots.");
            var slots = new Transform[ExpectedSlotNames.Length];
            for (var index = 0; index < slots.Length; index++)
            {
                slots[index] = placement.transform.GetChild(index);
                if (slots[index].name != ExpectedSlotNames[index])
                    throw new InvalidOperationException("Dolore slot order or name changed at index " + index + ".");
            }
            return slots;
        }

        private static Transform RequireModel(Transform slot)
        {
            return Enumerable.Range(0, slot.childCount).Select(slot.GetChild)
                       .SingleOrDefault(item => item.name == ModelName) ??
                   throw new InvalidOperationException(slot.name + " is missing " + ModelName + ".");
        }

        private static Target RequireTarget(Transform slot)
        {
            var model = RequireModel(slot);
            var attachment = model.Find(AttachmentName) ??
                             throw new InvalidOperationException(slot.name + " is missing " + AttachmentName + ".");
            var renderer = attachment.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault(item => item.sharedMesh != null && item.bones.Length == ExpectedBoneCount) ??
                           throw new InvalidOperationException(slot.name + " is missing the approved 13-bone renderer.");
            var source = attachment.Find(SourceName) ??
                         throw new InvalidOperationException(slot.name + " is missing " + SourceName + ".");
            var rootBone = renderer.bones.SingleOrDefault(item => item.name == RootBoneName) ??
                           throw new InvalidOperationException(RootBoneName + " is missing.");
            return new Target(model, attachment, source, renderer, rootBone);
        }

        private static List<Transform> RequireBoneChain(SkinnedMeshRenderer renderer)
        {
            var available = new HashSet<Transform>(renderer.bones);
            var root = renderer.bones.Single(item => item.name == RootBoneName);
            var chain = new List<Transform> { root };
            while (chain.Count < ExpectedBoneCount)
            {
                var next = available.SingleOrDefault(item => item.parent == chain[chain.Count - 1]);
                if (next == null)
                    throw new InvalidOperationException("The execution tentacle rig is not a single 13-bone chain.");
                chain.Add(next);
            }
            if (chain[chain.Count - 1].name != TipBoneName)
                throw new InvalidOperationException("The execution tentacle tip changed.");
            return chain;
        }

        private static void RequireMatchingAttachment(Target attack, Target execution)
        {
            if (!TransformApproximately(attack.Attachment, execution.Attachment) ||
                !TransformApproximately(attack.Source, execution.Source) ||
                !TransformApproximately(attack.RootBone, execution.RootBone))
                throw new InvalidOperationException("Motion 4 no longer matches motion 3's approved attachment start.");
            if (AssetDatabase.GetAssetPath(attack.Renderer.sharedMesh) !=
                AssetDatabase.GetAssetPath(execution.Renderer.sharedMesh))
                throw new InvalidOperationException("Motion 4 execution tentacle mesh differs from motion 3.");
            if (!attack.Renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath)
                    .SequenceEqual(execution.Renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath)))
                throw new InvalidOperationException("Motion 4 execution tentacle materials differ from motion 3.");
            var attackBones = RequireBoneChain(attack.Renderer).Select(item => item.name).ToArray();
            var executionBones = RequireBoneChain(execution.Renderer).Select(item => item.name).ToArray();
            if (!attackBones.SequenceEqual(executionBones, StringComparer.Ordinal))
                throw new InvalidOperationException("Motion 4 execution rig differs from motion 3.");
        }

        private static Vector3 ResolveOutwardDirection(Scene scene, Target target)
        {
            var player = scene.GetRootGameObjects().SingleOrDefault(item => item.name == "Player") ??
                         throw new InvalidOperationException("The CargoRunMvp Player root is missing.");
            var anchor = AttackRootSurfaceCenter(target.Renderer);
            var outward = Vector3.ProjectOnPlane(player.transform.position - anchor, Vector3.up).normalized;
            if (outward.sqrMagnitude < 0.9f)
                throw new InvalidOperationException("The frame outward direction toward Player is unavailable.");
            return outward;
        }

        private static Vector3 AttackRootSurfaceCenter(SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh ?? throw new InvalidOperationException("The execution mesh is missing.");
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var bindPoses = mesh.bindposes;
            var bones = renderer.bones;
            var indices = AttackRootSurfaceVertexIndices(mesh);
            var sum = Vector3.zero;
            foreach (var index in indices)
            {
                var weight = weights[index];
                sum += WeightedWorldVertex(vertices[index], weight.boneIndex0, weight.weight0, bones, bindPoses);
                sum += WeightedWorldVertex(vertices[index], weight.boneIndex1, weight.weight1, bones, bindPoses);
                sum += WeightedWorldVertex(vertices[index], weight.boneIndex2, weight.weight2, bones, bindPoses);
                sum += WeightedWorldVertex(vertices[index], weight.boneIndex3, weight.weight3, bones, bindPoses);
            }
            return sum / indices.Length;
        }

        private static Vector3 WeightedWorldVertex(
            Vector3 vertex,
            int boneIndex,
            float weight,
            IReadOnlyList<Transform> bones,
            IReadOnlyList<Matrix4x4> bindPoses)
        {
            if (weight <= 0f) return Vector3.zero;
            return (bones[boneIndex].localToWorldMatrix * bindPoses[boneIndex])
                   .MultiplyPoint3x4(vertex) * weight;
        }

        private static int[] AttackRootSurfaceVertexIndices(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var weldedByPosition = new Dictionary<Vector3Int, int>();
            var representatives = new List<int>();
            var vertexToWelded = new int[vertices.Length];
            for (var index = 0; index < vertices.Length; index++)
            {
                var vertex = vertices[index];
                var key = new Vector3Int(
                    Mathf.RoundToInt(vertex.x * 100000f),
                    Mathf.RoundToInt(vertex.y * 100000f),
                    Mathf.RoundToInt(vertex.z * 100000f));
                if (!weldedByPosition.TryGetValue(key, out var welded))
                {
                    welded = representatives.Count;
                    weldedByPosition.Add(key, welded);
                    representatives.Add(index);
                }
                vertexToWelded[index] = welded;
            }
            var edgeCounts = new Dictionary<ulong, int>();
            var triangles = mesh.triangles;
            for (var index = 0; index < triangles.Length; index += 3)
            {
                CountEdge(edgeCounts, vertexToWelded[triangles[index]], vertexToWelded[triangles[index + 1]]);
                CountEdge(edgeCounts, vertexToWelded[triangles[index + 1]], vertexToWelded[triangles[index + 2]]);
                CountEdge(edgeCounts, vertexToWelded[triangles[index + 2]], vertexToWelded[triangles[index]]);
            }
            var boundaryVertices = new HashSet<int>();
            foreach (var edge in edgeCounts.Where(item => item.Value == 1).Select(item => item.Key))
            {
                boundaryVertices.Add((int)(edge >> 32));
                boundaryVertices.Add((int)(edge & uint.MaxValue));
            }
            if (boundaryVertices.Count != 5)
                throw new InvalidOperationException("The approved five-vertex attachment boundary changed.");
            return boundaryVertices.Select(index => representatives[index]).ToArray();
        }

        private static void CountEdge(IDictionary<ulong, int> edgeCounts, int first, int second)
        {
            var minimum = (uint)Math.Min(first, second);
            var maximum = (uint)Math.Max(first, second);
            var key = ((ulong)minimum << 32) | maximum;
            edgeCounts[key] = edgeCounts.TryGetValue(key, out var count) ? count + 1 : 1;
        }

        private static string[] ProtectedRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(item => item.name != PlacementRootName)
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .Select(item => HierarchySignature(item.transform, null))
                .ToArray();
        }

        private static string[] ProtectedSlotSignatures(IReadOnlyList<Transform> slots)
        {
            return slots.Where((_, index) => index != 4)
                .Select(item => HierarchySignature(item, null))
                .ToArray();
        }

        private static string HierarchySignature(Transform root, string excludedChildName)
        {
            var builder = new StringBuilder();
            AppendHierarchySignature(builder, root, root, excludedChildName);
            return builder.ToString();
        }

        private static void AppendHierarchySignature(
            StringBuilder builder,
            Transform current,
            Transform root,
            string excludedChildName)
        {
            if (current != root && current.name == excludedChildName) return;
            builder.Append('|').Append(PathFrom(current, root))
                .Append(" T=").Append(TransformSignature(current))
                .Append(" A=").Append(current.gameObject.activeSelf);
            foreach (var renderer in current.GetComponents<Renderer>())
            {
                builder.Append(" Mesh=")
                    .Append(AssetDatabase.GetAssetPath(
                        renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh : null))
                    .Append(" Mats=")
                    .Append(string.Join(",", renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath)));
            }
            for (var index = 0; index < current.childCount; index++)
                AppendHierarchySignature(builder, current.GetChild(index), root, excludedChildName);
        }

        private static string PathFrom(Transform current, Transform root)
        {
            if (current == root) return string.Empty;
            var names = new List<string>();
            while (current != null && current != root)
            {
                names.Add(current.name);
                current = current.parent;
            }
            if (current != root) throw new InvalidOperationException("Transform is outside the requested root.");
            names.Reverse();
            return string.Join("/", names);
        }

        private static bool TransformApproximately(Transform left, Transform right)
        {
            return Vector3.Distance(left.localPosition, right.localPosition) <= TransformTolerance &&
                   Quaternion.Angle(left.localRotation, right.localRotation) <= 0.001f &&
                   Vector3.Distance(left.localScale, right.localScale) <= TransformTolerance;
        }

        private static string TransformSignature(Transform value)
        {
            return Vec(value.localPosition) + "|" + Quat(value.localRotation) + "|" + Vec(value.localScale);
        }

        private static void Restore(IReadOnlyList<Transform> chain, IReadOnlyList<BoneState> values)
        {
            for (var index = 0; index < chain.Count; index++) values[index].Apply(chain[index]);
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path) ??
                   throw new InvalidOperationException(typeof(T).Name + " asset is missing: " + path);
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Replace('\\', '/').Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        private static string ProjectAbsolutePath(string assetPath)
        {
            var root = Directory.GetParent(Application.dataPath)?.FullName ??
                       throw new InvalidOperationException("Unity project root is unavailable.");
            return Path.Combine(root, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string Num(float value) => value.ToString("R", CultureInfo.InvariantCulture);
        private static string Vec(Vector3 value) =>
            "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + ")";
        private static string Quat(Quaternion value) =>
            "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + "," + Num(value.w) + ")";

        private readonly struct Target
        {
            public Target(
                Transform model,
                Transform attachment,
                Transform source,
                SkinnedMeshRenderer renderer,
                Transform rootBone)
            {
                Model = model;
                Attachment = attachment;
                Source = source;
                Renderer = renderer;
                RootBone = rootBone;
            }

            public Transform Model { get; }
            public Transform Attachment { get; }
            public Transform Source { get; }
            public SkinnedMeshRenderer Renderer { get; }
            public Transform RootBone { get; }
        }

        private readonly struct BoneState
        {
            private BoneState(Vector3 position, Quaternion rotation, Vector3 scale)
            {
                Position = position;
                Rotation = rotation;
                Scale = scale;
            }

            private Vector3 Position { get; }
            private Quaternion Rotation { get; }
            private Vector3 Scale { get; }

            public static BoneState Capture(Transform transform) =>
                new BoneState(transform.localPosition, transform.localRotation, transform.localScale);

            public void Apply(Transform transform)
            {
                transform.localPosition = Position;
                transform.localRotation = Rotation;
                transform.localScale = Scale;
            }
        }

        private readonly struct Metrics
        {
            public Metrics(
                float introLength,
                float pierceLength,
                float introCurveDifference,
                float pierceCurveDifference,
                float introRise,
                float preparedMatchError,
                float windupLift,
                float windupRetreat,
                float lateStrikeOutward,
                float strikeForward,
                float strikeDrop,
                float strikeLateral,
                float impactHoldError,
                float rootDrift,
                float surfaceAnchorDrift,
                float sourceReadyOffset)
            {
                IntroLength = introLength;
                PierceLength = pierceLength;
                IntroCurveDifference = introCurveDifference;
                PierceCurveDifference = pierceCurveDifference;
                IntroRise = introRise;
                PreparedMatchError = preparedMatchError;
                WindupLift = windupLift;
                WindupRetreat = windupRetreat;
                LateStrikeOutward = lateStrikeOutward;
                StrikeForward = strikeForward;
                StrikeDrop = strikeDrop;
                StrikeLateral = strikeLateral;
                ImpactHoldError = impactHoldError;
                RootDrift = rootDrift;
                SurfaceAnchorDrift = surfaceAnchorDrift;
                SourceReadyOffset = sourceReadyOffset;
            }

            public float IntroLength { get; }
            public float PierceLength { get; }
            public float IntroCurveDifference { get; }
            public float PierceCurveDifference { get; }
            public float IntroRise { get; }
            public float PreparedMatchError { get; }
            public float WindupLift { get; }
            public float WindupRetreat { get; }
            public float LateStrikeOutward { get; }
            public float StrikeForward { get; }
            public float StrikeDrop { get; }
            public float StrikeLateral { get; }
            public float ImpactHoldError { get; }
            public float RootDrift { get; }
            public float SurfaceAnchorDrift { get; }
            public float SourceReadyOffset { get; }
        }
    }
}
