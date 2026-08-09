using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.IspantCargoRunScene
{
    internal static class IspantRunningSwordAttackAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ispant Enemy Placement";
        private const string StaticSlotName = "Ispant_01_Static";
        private const string DrawSwordSlotName = "Ispant_04_DrawSword";
        private const string AttackSlotName = "Ispant_05_RunningOneHandedSwordAttack";
        private const string StaticModelName = "Ispant_Model";
        private const string DrawSwordModelName = "Ispant_DrawSword_Model";
        private const string AttackModelName = "Ispant_RunningSwordAttack_Model";
        private const string SwordRootName = "Ispant_ApprovedLongSword";
        private const string SwordRendererName = "Ispant_ApprovedLongSword_Renderer";
        private const string MusketName = "Ispant_RunningAttack_RigidMusket";
        private const string SourceFbxPath = "enemies model/išpant slash.fbx";
        private const string RunSourceFbxPath = "enemies model/išpant running.fbx";
        private const string StaticFbxPath = "enemies model/Ispant_Static.fbx";
        private const string ProjectSourceFbxPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_RunningSwordAttack_Source.fbx";
        private const string DerivedFbxPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_RunningSwordAttack.fbx";
        private const string InPlaceClipPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_05_RunningSwordAttack_InPlace.anim";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_05_RunningSwordAttack.controller";
        private const string InspectionPath =
            "docs/validation/ispant_running_sword_attack_unified_body_2026-08-09/Ispant_05_RunningSwordAttack_UnifiedBody_Inspection.txt";
        private const string CapturePath =
            "docs/validation/ispant_running_sword_attack_unified_body_2026-08-09/Ispant_05_RunningSwordAttack_UnifiedBody_FinalReview.png";
        private const string SourceSha256 =
            "8170211F11E64D5D1BA0D74DA680CB29EEA8068AA28989EE37A28221A8A35467";
        private const string StaticSha256 =
            "28EBF3FC2EE9441478389477FE56547DF11C74CEBD152553F5F7B5FCD235A8BE";
        private const string RunSourceSha256 =
            "88AA87A62FA5F0D26382A0BD9B928A0D6AA5A836289EE22A4B238202ADE11FE5";
        private const string DerivedSha256 =
            "71FD6407AEF7B4AACC331C712B676881C74A1A1788A0A28067B685493F04DDB2";
        private const string ImportedClipName = "Ispant_RunningSwordAttack_Mixamo";
        private const string InPlaceClipName = "Ispant_05_RunningSwordAttack_InPlace";
        private const string StateName = "Ispant_RunningSwordAttack_Mixamo";
        private const int ExpectedSlots = 12;
        private const int ExpectedBones = 33;
        private const int ExpectedBodyTriangles = 3364;
        private const int ExpectedMusketTriangles = 154;
        private const int ExpectedCrescentTriangles = 1253;
        private const int ExpectedEyeTriangles = 312;
        private const int ExpectedSwordTriangles = 4092;
        private const int FirstFrame = 1;
        private const int LastFrame = 91;
        private const float TransformTolerance = 0.0001f;
        private const float AttachmentTolerance = 0.0001f;
        private const float InPlaceTolerance = 0.0001f;
        private const float SizeRatioTolerance = 0.01f;
        private const float MinimumVerticalMotion = 0.03f;
        private const float MinimumHandMotion = 0.25f;
        private const float MaximumHandMotion = 1f;
        private const float MinimumFootMotion = 0.2f;
        private const float MaximumFootMotion = 1.5f;
        private const float MaximumFootLateralRange = 0.08f;
        private const float MaximumToeLateralDirection = 0.4f;
        private const float MinimumUpperLegAngularMotion = 30f;
        private const float MaximumLowerBodyLoopError = 0.002f;
        private const float MaximumSpineLocalPositionRange = 0.001f;
        private const float MinimumHipsToSpineDistance = 0.05f;
        private const float MaximumHipsToSpineDistance = 0.08f;
        private const float MaximumSwordVertexToHandDistance = 0.04f;
        private const float MaximumSwordForearmAngle = 0.25f;
        private const float MaximumMusketBackContactDistance = 0.012f;
        private const float ExpectedSwordLength = 1.4374533f;
        private const float TargetWorldBladeLength = 0.6f;
        private const float SwordDimensionTolerance = 0.0001f;
        private static readonly Vector3 ApprovedGripCenterLocal = new Vector3(0f, 0f, -0.103f);
        private static readonly float[] ReviewNormalizedTimes = { 0f, 0.25f, 0.5f, 0.75f, 1f };

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Running Sword Attack Animation")]
        public static void ApplyIspantRunningSwordAttackAnimation()
        {
            RequireHashes();
            ConfigureImporter();
            RequireHashes();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DerivedFbxPath) ??
                throw new InvalidOperationException("The derived Ispant running-attack FBX is unavailable.");
            var clip = CreateOrUpdateInPlaceClip(RequireImportedClip());
            var controller = CreateOrUpdateController(clip);

            var scene = RequireScene(requireClean: false);
            var placement = RequirePlacement(scene);
            var staticSlot = RequireSlot(placement.transform, StaticSlotName, 0);
            var drawSlot = RequireSlot(placement.transform, DrawSwordSlotName, 3);
            var attackSlot = RequireSlot(placement.transform, AttackSlotName, 4);
            var staticModel = RequireDirectChild(staticSlot, StaticModelName);
            var drawModel = RequireDirectChild(drawSlot, DrawSwordModelName);
            if (attackSlot.childCount != 1)
                throw new InvalidOperationException("Ispant_05_RunningOneHandedSwordAttack must contain exactly one model before replacement.");

            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var otherSlotsBefore = OtherSlotSignatures(placement.transform, attackSlot);
            var slotBefore = new TransformSnapshot(attackSlot);
            var previous = attackSlot.GetChild(0);
            var replacement = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject ??
                throw new InvalidOperationException("The Ispant running-attack FBX could not be instantiated.");
            replacement.name = AttackModelName;
            replacement.transform.SetParent(attackSlot, false);
            replacement.transform.SetLocalPositionAndRotation(previous.localPosition, previous.localRotation);
            replacement.transform.localScale = Vector3.one;

            try
            {
                ApplyStaticAppearance(staticModel, replacement.transform);
                FitToStaticReference(replacement.transform, staticModel);
                CloneApprovedSword(staticModel, drawModel, replacement.transform);
                ApplySwordForearmAlignment(replacement.transform, clip);
                var animator = ConfigureAnimator(replacement.transform, controller);
                var metrics = InspectModel(
                    replacement.transform, staticModel, drawModel, animator, clip, controller);
                WriteInspection(metrics);
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(replacement);
                throw;
            }

            UnityEngine.Object.DestroyImmediate(previous.gameObject);
            if (attackSlot.childCount != 1 || attackSlot.GetChild(0) != replacement.transform)
                throw new InvalidOperationException("The running-attack replacement did not leave exactly one model.");
            if (!slotBefore.Matches(TransformTolerance))
                throw new InvalidOperationException("The running-attack slot transform changed during replacement.");
            RequireEqual(otherRootsBefore, OtherRootSignatures(scene, placement),
                "A scene root outside the Ispant placement changed.");
            RequireEqual(otherSlotsBefore, OtherSlotSignatures(placement.transform, attackSlot),
                "An Ispant slot outside slot 5 changed.");

            EditorUtility.SetDirty(replacement);
            EditorUtility.SetDirty(attackSlot.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("CargoRunMvp could not be saved after the running-attack replacement.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = attackSlot.gameObject;
            Debug.Log(
                "IspantRunningSwordAttackAnimationApplied Result=PASS" +
                ", Target=" + PlacementRootName + "/" + AttackSlotName +
                ", Clip=" + InPlaceClipName +
                ", Loop=True, HorizontalRootMotion=0, IspantLowerBodyRunCycles=2" +
                ", UnifiedBodyHierarchy=True, UpperBodyLocalAttackPreserved=True" +
                ", SwordParent=mixamorig:RightHand, SwordForearmAligned=True, SwordWorldBladeLength=0.6m" +
                ", MusketParent=mixamorig:Spine2, MusketRestBound=True, MusketRigid=True" +
                ", OtherSlotsChanged=False, OtherSceneRootsChanged=False, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Running Sword Attack Animation")]
        public static void InspectIspantRunningSwordAttackAnimation()
        {
            RequireHashes();
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var drawModel = RequireDirectChild(
                RequireSlot(placement.transform, DrawSwordSlotName, 3), DrawSwordModelName);
            var model = RequireDirectChild(
                RequireSlot(placement.transform, AttackSlotName, 4), AttackModelName);
            var animator = model.GetComponentsInChildren<Animator>(true).Single();
            var metrics = InspectModel(
                model, staticModel, drawModel, animator, RequireInPlaceClip(), RequireController());
            WriteInspection(metrics);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Running-attack inspection changed the scene dirty state.");
            Debug.Log(
                "IspantRunningSwordAttackAnimationInspected Result=PASS" +
                ", HorizontalHipsRange=" + Num(metrics.HorizontalHipsRange) +
                ", VerticalHipsRange=" + Num(metrics.VerticalHipsRange) +
                ", LeftFootMotion=" + Num(metrics.MaximumLeftFootMotion) +
                ", RightFootMotion=" + Num(metrics.MaximumRightFootMotion) +
                ", LeftFootLateralRange=" + Num(metrics.LeftFootLateralRange) +
                ", RightFootLateralRange=" + Num(metrics.RightFootLateralRange) +
                ", SpineLocalPositionRange=" + Num(metrics.SpineLocalPositionRange) +
                ", HipsToSpineDistance=" + Num(metrics.MinimumHipsToSpineDistance) +
                "-" + Num(metrics.MaximumHipsToSpineDistance) +
                ", LeftUpLegAngle=" + Num(metrics.MaximumLeftUpLegAngularMotion) +
                ", RightUpLegAngle=" + Num(metrics.MaximumRightUpLegAngularMotion) +
                ", SwordAttachmentError=" + Num(metrics.MaximumSwordAttachmentError) +
                ", SwordForearmAngle=" + Num(metrics.MaximumSwordForearmAngle) +
                ", MusketAttachmentError=" + Num(metrics.MaximumMusketAttachmentError) +
                ", MusketBackContact=" + Num(metrics.MaximumMusketBackContactDistance) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Running Sword Attack Animation Review")]
        public static void CaptureIspantRunningSwordAttackAnimationReview()
        {
            RequireHashes();
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var drawModel = RequireDirectChild(
                RequireSlot(placement.transform, DrawSwordSlotName, 3), DrawSwordModelName);
            var model = RequireDirectChild(
                RequireSlot(placement.transform, AttackSlotName, 4), AttackModelName);
            var clip = RequireInPlaceClip();
            var metrics = InspectModel(
                model, staticModel, drawModel,
                model.GetComponentsInChildren<Animator>(true).Single(), clip, RequireController());
            WriteInspection(metrics);
            var destination = Absolute(CapturePath);
            if (File.Exists(destination))
                throw new InvalidOperationException("The one-time running-attack final review already exists.");
            CaptureReview(staticModel, model, clip, destination);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Running-attack review capture changed the scene dirty state.");
            Debug.Log(
                "IspantRunningSwordAttackAnimationReviewCaptured Result=PASS" +
                ", Panels=Static,0,0.25,0.5,0.75,1, Image=" + CapturePath +
                ", SceneChanged=False.");
        }

        private static void ConfigureImporter()
        {
            AssetDatabase.ImportAsset(
                DerivedFbxPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(DerivedFbxPath) as ModelImporter ??
                throw new InvalidOperationException("The running-attack ModelImporter is missing.");
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.optimizeGameObjects = false;
            importer.isReadable = true;
            importer.importBlendShapes = true;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            var clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1)
                throw new InvalidOperationException("The running-attack FBX must expose exactly one Mixamo take.");
            if (clips[0].takeName.IndexOf("mixamo.com", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(
                    "The sole running-attack take is not the supplied Mixamo action: " + clips[0].takeName + ".");
            clips[0].name = ImportedClipName;
            clips[0].firstFrame = FirstFrame;
            clips[0].lastFrame = LastFrame;
            clips[0].loopTime = true;
            clips[0].loopPose = false;
            clips[0].lockRootRotation = false;
            clips[0].lockRootPositionXZ = false;
            clips[0].lockRootHeightY = false;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip RequireImportedClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(DerivedFbxPath)
                .OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1 || clips[0].name != ImportedClipName)
                throw new InvalidOperationException("The imported running-attack Mixamo clip differs.");
            return clips[0];
        }

        private static AnimationClip CreateOrUpdateInPlaceClip(AnimationClip source)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(InPlaceClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = InPlaceClipName };
                AssetDatabase.CreateAsset(clip, InPlaceClipPath);
            }
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, binding, null);
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);

            var flattened = new HashSet<string>(StringComparer.Ordinal);
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var curve = AnimationUtility.GetEditorCurve(source, binding);
                CloseLoopBoundary(curve, source.length);
                if (IsHorizontalHipsPositionCurve(binding))
                {
                    if (curve == null || curve.length == 0)
                        throw new InvalidOperationException("A Mixamo horizontal hips curve is empty.");
                    curve = AnimationCurve.Constant(0f, source.length, curve.keys[0].value);
                    flattened.Add(binding.propertyName);
                }
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
                AnimationUtility.SetObjectReferenceCurve(
                    clip, binding, AnimationUtility.GetObjectReferenceCurve(source, binding));
            if (!flattened.SetEquals(new[] { "m_LocalPosition.x", "m_LocalPosition.z" }))
                throw new InvalidOperationException(
                    "Exactly the two Mixamo hips horizontal position curves must be flattened: " +
                    string.Join(",", flattened) + ".");

            clip.frameRate = source.frameRate;
            clip.wrapMode = WrapMode.Loop;
            AnimationUtility.SetAnimationEvents(clip, AnimationUtility.GetAnimationEvents(source));
            var settings = AnimationUtility.GetAnimationClipSettings(source);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static void CloseLoopBoundary(AnimationCurve curve, float length)
        {
            if (curve == null || curve.length == 0)
                throw new InvalidOperationException("A supplied Mixamo animation curve is empty.");
            var keys = curve.keys;
            var first = keys[0];
            var lastIndex = keys.Length - 1;
            if (Mathf.Abs(keys[lastIndex].time - length) <= 0.0001f)
            {
                var last = keys[lastIndex];
                last.value = first.value;
                last.inTangent = first.inTangent;
                last.outTangent = first.outTangent;
                keys[lastIndex] = last;
                curve.keys = keys;
            }
            else
            {
                var closing = first;
                closing.time = length;
                curve.AddKey(closing);
            }
        }

        private static bool IsHorizontalHipsPositionCurve(EditorCurveBinding binding)
        {
            return (binding.path == "mixamorig:Hips" ||
                    binding.path.EndsWith("/mixamorig:Hips", StringComparison.Ordinal)) &&
                   (binding.propertyName == "m_LocalPosition.x" ||
                    binding.propertyName == "m_LocalPosition.z");
        }

        private static AnimatorController CreateOrUpdateController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            var stateMachine = controller.layers[0].stateMachine;
            foreach (var child in stateMachine.states.ToArray())
                stateMachine.RemoveState(child.state);
            foreach (var child in stateMachine.stateMachines.ToArray())
                stateMachine.RemoveStateMachine(child.stateMachine);
            var state = stateMachine.AddState(StateName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimatorController RequireController()
        {
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException("The running-attack AnimatorController is missing.");
        }

        private static AnimationClip RequireInPlaceClip()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(InPlaceClipPath) ??
                throw new InvalidOperationException("The running-attack in-place clip is missing.");
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (clip.name != InPlaceClipName || !settings.loopTime)
                throw new InvalidOperationException("The running-attack in-place loop configuration differs.");
            return clip;
        }

        private static Animator ConfigureAnimator(Transform model, RuntimeAnimatorController controller)
        {
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length != 1)
                throw new InvalidOperationException("The running-attack model must contain exactly one Animator.");
            var animator = animators[0];
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            return animator;
        }

        private static void ApplyStaticAppearance(Transform staticModel, Transform model)
        {
            var approved = staticModel.GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .GroupBy(material => NormalizeMaterialName(material.name), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length != 4)
                throw new InvalidOperationException(
                    "The derived running-attack FBX must contain body, crescent, eyes, and rigid musket renderers.");
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(material =>
                {
                    if (material == null)
                        throw new InvalidOperationException("A running-attack material slot is null.");
                    var key = NormalizeMaterialName(material.name);
                    return approved.TryGetValue(key, out var exact)
                        ? exact
                        : throw new InvalidOperationException(
                            "No exact static Ispant material matches " + material.name + ".");
                }).ToArray();
                if (renderer is SkinnedMeshRenderer skinned)
                    skinned.updateWhenOffscreen = true;
                EditorUtility.SetDirty(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }

        private static void CloneApprovedSword(
            Transform staticModel, Transform drawModel, Transform targetModel)
        {
            var staticRenderer = RequireRenderer<MeshRenderer>(staticModel, SwordRendererName);
            var sourceRenderer = RequireRenderer<MeshRenderer>(drawModel, SwordRendererName);
            var sourceRoot = sourceRenderer.transform.parent;
            if (sourceRoot == null || sourceRoot.name != SwordRootName ||
                sourceRoot.parent == null || sourceRoot.parent.name != "mixamorig:RightHand")
                throw new InvalidOperationException("The approved draw-slot right-hand sword mount differs.");
            var rightHand = RequireDescendant(targetModel, "mixamorig:RightHand");
            var root = new GameObject(SwordRootName);
            root.transform.SetParent(rightHand, false);
            CopyLocalTransform(sourceRoot, root.transform);
            var rendererObject = new GameObject(SwordRendererName);
            rendererObject.transform.SetParent(root.transform, false);
            CopyLocalTransform(sourceRenderer.transform, rendererObject.transform);
            var filter = rendererObject.AddComponent<MeshFilter>();
            var renderer = rendererObject.AddComponent<MeshRenderer>();
            filter.sharedMesh = staticRenderer.GetComponent<MeshFilter>().sharedMesh;
            renderer.sharedMaterials = staticRenderer.sharedMaterials;
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(rendererObject);
        }

        private static void CopyLocalTransform(Transform source, Transform target)
        {
            target.localPosition = source.localPosition;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
        }

        private static void ApplySwordForearmAlignment(Transform model, AnimationClip clip)
        {
            var sword = RequireRenderer<MeshRenderer>(model, SwordRendererName);
            var swordRoot = sword.transform.parent ??
                throw new InvalidOperationException("The approved running-attack sword root is missing.");
            var rightHand = RequireDescendant(model, "mixamorig:RightHand");
            var rightForeArm = RequireDescendant(model, "mixamorig:RightForeArm");
            if (swordRoot.parent != rightHand)
                throw new InvalidOperationException("The sword alignment target is not under the right hand.");
            var path = AnimationUtility.CalculateTransformPath(swordRoot, model);
            var localBladeAxis = CalculateSwordLocalBladeAxis(sword);
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var rotations = new Quaternion[LastFrame - FirstFrame + 1];
            Quaternion? previous = null;
            try
            {
                for (var frame = FirstFrame; frame <= LastFrame; frame++)
                {
                    var normalized = (frame - FirstFrame) / (float)(LastFrame - FirstFrame);
                    SampleClip(model.gameObject, clip, normalized * clip.length);
                    var forearmAxis = rightHand.position - rightForeArm.position;
                    if (forearmAxis.sqrMagnitude <= 0.000001f)
                        throw new InvalidOperationException("The right forearm axis collapsed during the attack.");
                    var bladeAxis = sword.transform.TransformVector(localBladeAxis);
                    if (bladeAxis.sqrMagnitude <= 0.000001f)
                        throw new InvalidOperationException("The approved sword blade axis collapsed.");
                    var desiredWorldRotation =
                        Quaternion.FromToRotation(bladeAxis.normalized, forearmAxis.normalized) *
                        swordRoot.rotation;
                    var desiredLocalRotation =
                        Quaternion.Inverse(swordRoot.parent.rotation) * desiredWorldRotation;
                    if (previous.HasValue && Quaternion.Dot(previous.Value, desiredLocalRotation) < 0f)
                    {
                        desiredLocalRotation = new Quaternion(
                            -desiredLocalRotation.x,
                            -desiredLocalRotation.y,
                            -desiredLocalRotation.z,
                            -desiredLocalRotation.w);
                    }
                    rotations[frame - FirstFrame] = desiredLocalRotation;
                    previous = desiredLocalRotation;
                }
            }
            finally
            {
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
                StopSampling();
            }

            // The supplied action is cyclic; closing the sword correction to the first
            // rotation prevents a quaternion sign flip at the 91-to-1 frame boundary.
            rotations[rotations.Length - 1] = rotations[0];
            var propertyNames = new[]
            {
                "m_LocalRotation.x",
                "m_LocalRotation.y",
                "m_LocalRotation.z",
                "m_LocalRotation.w"
            };
            for (var component = 0; component < propertyNames.Length; component++)
            {
                var keys = new Keyframe[rotations.Length];
                for (var index = 0; index < rotations.Length; index++)
                {
                    var time = index / (float)(rotations.Length - 1) * clip.length;
                    keys[index] = new Keyframe(time, rotations[index][component]);
                }
                var curve = new AnimationCurve(keys);
                for (var index = 0; index < keys.Length; index++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(
                        curve, index, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyRightTangentMode(
                        curve, index, AnimationUtility.TangentMode.Linear);
                }
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyNames[component]),
                    curve);
            }
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
        }

        private static Vector3 CalculateSwordLocalBladeAxis(MeshRenderer renderer)
        {
            var mesh = SharedMesh(renderer);
            var vertices = mesh.vertices;
            var grip = ApprovedGripCenterLocal * (mesh.bounds.size.z / ExpectedSwordLength);
            var maximumZ = vertices.Max(vertex => vertex.z);
            var tipVertices = vertices.Where(vertex => maximumZ - vertex.z <= 0.000005f).ToArray();
            if (tipVertices.Length == 0)
                throw new InvalidOperationException("The approved sword tip vertices are missing.");
            var tip = tipVertices.Aggregate(Vector3.zero, (sum, vertex) => sum + vertex) /
                      tipVertices.Length;
            return tip - grip;
        }

        private static void FitToStaticReference(Transform model, Transform staticModel)
        {
            var staticBody = RequireRenderer<SkinnedMeshRenderer>(staticModel, "Ispant_Armed_Body");
            var body = RequireRenderer<SkinnedMeshRenderer>(model, "Ispant_Armed_Body");
            var staticBounds = BindWorldBounds(staticBody);
            var bounds = BindWorldBounds(body);
            if (bounds.size.y <= 0.0001f)
                throw new InvalidOperationException("The running-attack bind bounds are invalid.");
            var scale = staticBounds.size.y / bounds.size.y;
            if (scale < 0.5f || scale > 2f)
                throw new InvalidOperationException("The running-attack size ratio is unsafe: " + Num(scale) + ".");
            model.localScale *= scale;
            bounds = BindWorldBounds(body);
            model.position += Vector3.up * (staticBounds.min.y - bounds.min.y);
            EditorUtility.SetDirty(model);
            PrefabUtility.RecordPrefabInstancePropertyModifications(model);
        }

        private static Metrics InspectModel(
            Transform model,
            Transform staticModel,
            Transform drawModel,
            Animator animator,
            AnimationClip clip,
            AnimatorController controller)
        {
            if (!animator.enabled || animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion || animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                throw new InvalidOperationException("The running-attack Animator configuration differs.");
            if (controller.layers[0].stateMachine.defaultState == null ||
                controller.layers[0].stateMachine.defaultState.name != StateName ||
                controller.layers[0].stateMachine.defaultState.motion != clip)
                throw new InvalidOperationException("The running-attack default Mixamo state differs.");

            var body = RequireRenderer<SkinnedMeshRenderer>(model, "Ispant_Armed_Body");
            var crescent = RequireRenderer<SkinnedMeshRenderer>(model, "Ispant_Crescent_Ornament");
            var eyes = RequireRenderer<SkinnedMeshRenderer>(model, "Ispant_Reference_Eye_Slits");
            var musket = RequireRenderer<MeshRenderer>(model, MusketName);
            var sword = RequireRenderer<MeshRenderer>(model, SwordRendererName);
            if (model.GetComponentsInChildren<Renderer>(true).Length != 5)
                throw new InvalidOperationException("The running-attack renderer set differs.");
            if (body.bones.Length != ExpectedBones || crescent.bones.Length != ExpectedBones ||
                eyes.bones.Length != ExpectedBones)
                throw new InvalidOperationException("The running-attack Mixamo bone count differs.");
            if (TriangleCount(SharedMesh(body)) != ExpectedBodyTriangles ||
                TriangleCount(SharedMesh(musket)) != ExpectedMusketTriangles ||
                TriangleCount(SharedMesh(crescent)) != ExpectedCrescentTriangles ||
                TriangleCount(SharedMesh(eyes)) != ExpectedEyeTriangles ||
                TriangleCount(SharedMesh(sword)) != ExpectedSwordTriangles)
                throw new InvalidOperationException("The running-attack synchronized mesh topology differs.");
            if (musket.GetComponent<SkinnedMeshRenderer>() != null ||
                sword.GetComponent<SkinnedMeshRenderer>() != null)
                throw new InvalidOperationException("The running-attack weapons must be rigid MeshRenderers.");

            var spine = RequireDescendant(model, "mixamorig:Spine");
            var spine2 = RequireDescendant(model, "mixamorig:Spine2");
            var rightHand = RequireDescendant(model, "mixamorig:RightHand");
            var rightForeArm = RequireDescendant(model, "mixamorig:RightForeArm");
            if (musket.transform.parent != spine2)
                throw new InvalidOperationException("The rigid musket is not directly parented to mixamorig:Spine2.");
            if (sword.transform.parent == null || sword.transform.parent.parent != rightHand ||
                sword.transform.parent.name != SwordRootName)
                throw new InvalidOperationException("The approved sword is not directly mounted under the right hand.");
            RequireExactStaticMaterials(staticModel, model);
            RequireExactSword(staticModel, drawModel, sword);

            var staticBounds = BindWorldBounds(
                RequireRenderer<SkinnedMeshRenderer>(staticModel, "Ispant_Armed_Body"));
            var modelBounds = BindWorldBounds(body);
            var heightRatio = modelBounds.size.y / staticBounds.size.y;
            var groundDifference = Mathf.Abs(modelBounds.min.y - staticBounds.min.y);
            if (Mathf.Abs(heightRatio - 1f) > SizeRatioTolerance || groundDifference > 0.005f)
                throw new InvalidOperationException(
                    "The running-attack model does not match the static size and ground level.");

            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var modelPosition = model.localPosition;
            var modelRotation = model.localRotation;
            var modelScale = model.localScale;
            var initialMusketLocal = LocalMatrix(musket.transform);
            var swordRoot = sword.transform.parent;
            var initialSwordLocalPosition = swordRoot.localPosition;
            var initialSwordLocalScale = swordRoot.localScale;
            var initialMusketWorldPosition = musket.transform.position;
            var initialSwordWorldPosition = sword.transform.position;
            var swordMesh = SharedMesh(sword);
            var musketMesh = SharedMesh(musket);
            var localBladeAxis = CalculateSwordLocalBladeAxis(sword);
            var hips = RequireDescendant(model, "mixamorig:Hips");
            var leftUpLeg = RequireDescendant(model, "mixamorig:LeftUpLeg");
            var rightUpLeg = RequireDescendant(model, "mixamorig:RightUpLeg");
            var leftFoot = RequireDescendant(model, "mixamorig:LeftFoot");
            var rightFoot = RequireDescendant(model, "mixamorig:RightFoot");
            var leftToe = RequireDescendant(model, "mixamorig:LeftToeBase");
            var rightToe = RequireDescendant(model, "mixamorig:RightToeBase");
            var horizontal = new List<Vector2>();
            var vertical = new List<float>();
            var leftFootModelPositions = new List<Vector3>();
            var rightFootModelPositions = new List<Vector3>();
            var spineLocalPositions = new List<Vector3>();
            var hipsToSpineDistances = new List<float>();
            var maximumMusketAttachmentError = 0f;
            var maximumSwordAttachmentError = 0f;
            var maximumMusketFollowMotion = 0f;
            var maximumSwordFollowMotion = 0f;
            var maximumHandMotion = 0f;
            var maximumNearestSwordVertexToHand = 0f;
            var maximumSwordForearmAngle = 0f;
            var maximumSwordAngularStep = 0f;
            var maximumMusketBackContactDistance = 0f;
            var minimumMusketBackContactDistance = float.PositiveInfinity;
            var maximumLeftFootMotion = 0f;
            var maximumRightFootMotion = 0f;
            var maximumLeftUpLegAngularMotion = 0f;
            var maximumRightUpLegAngularMotion = 0f;
            var maximumLeftToeLateralDirection = 0f;
            var maximumRightToeLateralDirection = 0f;
            Vector3? initialHand = null;
            Vector3? firstHand = null;
            Vector3? lastHand = null;
            Vector3? initialLeftFoot = null;
            Vector3? initialRightFoot = null;
            Vector3? firstLeftFoot = null;
            Vector3? firstRightFoot = null;
            Vector3? lastLeftFoot = null;
            Vector3? lastRightFoot = null;
            Quaternion? initialLeftUpLeg = null;
            Quaternion? initialRightUpLeg = null;
            Vector3? previousBladeAxis = null;
            try
            {
                for (var frame = FirstFrame; frame <= LastFrame; frame++)
                {
                    var normalized = (frame - FirstFrame) / (float)(LastFrame - FirstFrame);
                    SampleClip(model.gameObject, clip, normalized * clip.length);
                    var hipsInModel = model.InverseTransformPoint(hips.position);
                    horizontal.Add(new Vector2(hipsInModel.x, hipsInModel.z));
                    vertical.Add(hipsInModel.y);
                    var leftFootInModel = model.InverseTransformPoint(leftFoot.position);
                    var rightFootInModel = model.InverseTransformPoint(rightFoot.position);
                    leftFootModelPositions.Add(leftFootInModel);
                    rightFootModelPositions.Add(rightFootInModel);
                    spineLocalPositions.Add(spine.localPosition);
                    hipsToSpineDistances.Add(Vector3.Distance(hips.position, spine.position));
                    var leftToeDirection = model.InverseTransformDirection(
                        leftToe.position - leftFoot.position).normalized;
                    var rightToeDirection = model.InverseTransformDirection(
                        rightToe.position - rightFoot.position).normalized;
                    maximumLeftToeLateralDirection = Mathf.Max(
                        maximumLeftToeLateralDirection, Mathf.Abs(leftToeDirection.x));
                    maximumRightToeLateralDirection = Mathf.Max(
                        maximumRightToeLateralDirection, Mathf.Abs(rightToeDirection.x));
                    maximumMusketAttachmentError = Mathf.Max(
                        maximumMusketAttachmentError,
                        MatrixError(initialMusketLocal, LocalMatrix(musket.transform)));
                    maximumSwordAttachmentError = Mathf.Max(
                        maximumSwordAttachmentError,
                        Mathf.Max(
                            Vector3.Distance(initialSwordLocalPosition, swordRoot.localPosition),
                            Vector3.Distance(initialSwordLocalScale, swordRoot.localScale)));
                    maximumMusketFollowMotion = Mathf.Max(
                        maximumMusketFollowMotion,
                        Vector3.Distance(initialMusketWorldPosition, musket.transform.position));
                    maximumSwordFollowMotion = Mathf.Max(
                        maximumSwordFollowMotion,
                        Vector3.Distance(initialSwordWorldPosition, sword.transform.position));
                    var handPosition = rightHand.position;
                    if (!initialHand.HasValue)
                    {
                        initialHand = handPosition;
                        firstHand = handPosition;
                    }
                    lastHand = handPosition;
                    maximumHandMotion = Mathf.Max(
                        maximumHandMotion, Vector3.Distance(initialHand.Value, handPosition));
                    if (!initialLeftFoot.HasValue)
                    {
                        initialLeftFoot = leftFoot.position;
                        initialRightFoot = rightFoot.position;
                        firstLeftFoot = leftFoot.position;
                        firstRightFoot = rightFoot.position;
                        initialLeftUpLeg = leftUpLeg.rotation;
                        initialRightUpLeg = rightUpLeg.rotation;
                    }
                    lastLeftFoot = leftFoot.position;
                    lastRightFoot = rightFoot.position;
                    maximumLeftFootMotion = Mathf.Max(
                        maximumLeftFootMotion,
                        Vector3.Distance(initialLeftFoot.Value, leftFoot.position));
                    maximumRightFootMotion = Mathf.Max(
                        maximumRightFootMotion,
                        Vector3.Distance(initialRightFoot.Value, rightFoot.position));
                    maximumLeftUpLegAngularMotion = Mathf.Max(
                        maximumLeftUpLegAngularMotion,
                        Quaternion.Angle(initialLeftUpLeg.Value, leftUpLeg.rotation));
                    maximumRightUpLegAngularMotion = Mathf.Max(
                        maximumRightUpLegAngularMotion,
                        Quaternion.Angle(initialRightUpLeg.Value, rightUpLeg.rotation));
                    maximumNearestSwordVertexToHand = Mathf.Max(
                        maximumNearestSwordVertexToHand,
                        swordMesh.vertices.Min(vertex =>
                            Vector3.Distance(sword.transform.TransformPoint(vertex), handPosition)));
                    var bladeAxis = sword.transform.TransformVector(localBladeAxis).normalized;
                    var forearmAxis = (rightHand.position - rightForeArm.position).normalized;
                    maximumSwordForearmAngle = Mathf.Max(
                        maximumSwordForearmAngle, Vector3.Angle(bladeAxis, forearmAxis));
                    if (previousBladeAxis.HasValue)
                        maximumSwordAngularStep = Mathf.Max(
                            maximumSwordAngularStep,
                            Vector3.Angle(previousBladeAxis.Value, bladeAxis));
                    previousBladeAxis = bladeAxis;

                    var bodyVertices = SkinnedWorldVertices(body);
                    var minimumBackDistanceSquared = float.PositiveInfinity;
                    foreach (var musketVertex in musketMesh.vertices)
                    {
                        var musketWorld = musket.transform.TransformPoint(musketVertex);
                        foreach (var bodyWorld in bodyVertices)
                        {
                            minimumBackDistanceSquared = Mathf.Min(
                                minimumBackDistanceSquared,
                                (musketWorld - bodyWorld).sqrMagnitude);
                        }
                    }
                    var backContactDistance = Mathf.Sqrt(minimumBackDistanceSquared);
                    minimumMusketBackContactDistance = Mathf.Min(
                        minimumMusketBackContactDistance, backContactDistance);
                    maximumMusketBackContactDistance = Mathf.Max(
                        maximumMusketBackContactDistance, backContactDistance);
                    if (Vector3.Distance(model.localPosition, modelPosition) > TransformTolerance ||
                        Quaternion.Angle(model.localRotation, modelRotation) > TransformTolerance ||
                        Vector3.Distance(model.localScale, modelScale) > TransformTolerance)
                        throw new InvalidOperationException("The in-place clip changed the slot model root transform.");
                }
            }
            finally
            {
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
                StopSampling();
            }

            var horizontalRange = horizontal.Max(value => value.x) - horizontal.Min(value => value.x) +
                                  horizontal.Max(value => value.y) - horizontal.Min(value => value.y);
            var verticalRange = vertical.Max() - vertical.Min();
            var leftFootLateralRange =
                leftFootModelPositions.Max(value => value.x) -
                leftFootModelPositions.Min(value => value.x);
            var rightFootLateralRange =
                rightFootModelPositions.Max(value => value.x) -
                rightFootModelPositions.Min(value => value.x);
            var leftFootForwardRange =
                leftFootModelPositions.Max(value => value.z) -
                leftFootModelPositions.Min(value => value.z);
            var rightFootForwardRange =
                rightFootModelPositions.Max(value => value.z) -
                rightFootModelPositions.Min(value => value.z);
            var spineLocalPositionRange = spineLocalPositions.Max(
                value => Vector3.Distance(spineLocalPositions[0], value));
            var minimumHipsToSpineDistance = hipsToSpineDistances.Min();
            var maximumHipsToSpineDistance = hipsToSpineDistances.Max();
            var loopHandError = Vector3.Distance(firstHand ?? Vector3.zero, lastHand ?? Vector3.one);
            var loopLeftFootError = Vector3.Distance(
                firstLeftFoot ?? Vector3.zero, lastLeftFoot ?? Vector3.one);
            var loopRightFootError = Vector3.Distance(
                firstRightFoot ?? Vector3.zero, lastRightFoot ?? Vector3.one);
            if (horizontalRange > InPlaceTolerance)
                throw new InvalidOperationException("The running attack still has horizontal hips travel: " + Num(horizontalRange) + ".");
            if (verticalRange < MinimumVerticalMotion)
                throw new InvalidOperationException("The running attack lost its vertical body motion.");
            if (maximumHandMotion < MinimumHandMotion ||
                maximumHandMotion > MaximumHandMotion ||
                loopHandError > 0.002f)
                throw new InvalidOperationException(
                    "The supplied attack body motion or loop boundary differs. HandMotion=" +
                    Num(maximumHandMotion) + ", LoopHandError=" + Num(loopHandError) + ".");
            if (maximumLeftFootMotion < MinimumFootMotion ||
                maximumLeftFootMotion > MaximumFootMotion ||
                maximumRightFootMotion < MinimumFootMotion ||
                maximumRightFootMotion > MaximumFootMotion ||
                maximumLeftUpLegAngularMotion < MinimumUpperLegAngularMotion ||
                maximumRightUpLegAngularMotion < MinimumUpperLegAngularMotion)
                throw new InvalidOperationException(
                    "The lower body does not contain the supplied running stride. LeftFootMotion=" +
                    Num(maximumLeftFootMotion) + ", RightFootMotion=" +
                    Num(maximumRightFootMotion) + ", LeftUpLegAngle=" +
                    Num(maximumLeftUpLegAngularMotion) + ", RightUpLegAngle=" +
                    Num(maximumRightUpLegAngularMotion) + ".");
            if (loopLeftFootError > MaximumLowerBodyLoopError ||
                loopRightFootError > MaximumLowerBodyLoopError)
                throw new InvalidOperationException(
                    "The running lower-body loop boundary differs. LeftFootError=" +
                    Num(loopLeftFootError) + ", RightFootError=" +
                    Num(loopRightFootError) + ".");
            if (leftFootLateralRange > MaximumFootLateralRange ||
                rightFootLateralRange > MaximumFootLateralRange ||
                maximumLeftToeLateralDirection > MaximumToeLateralDirection ||
                maximumRightToeLateralDirection > MaximumToeLateralDirection)
                throw new InvalidOperationException(
                    "The supplied Ispant running feet turn or travel sideways. LeftLateralRange=" +
                    Num(leftFootLateralRange) + ", RightLateralRange=" +
                    Num(rightFootLateralRange) + ", LeftToeLateral=" +
                    Num(maximumLeftToeLateralDirection) + ", RightToeLateral=" +
                    Num(maximumRightToeLateralDirection) + ".");
            if (spineLocalPositionRange > MaximumSpineLocalPositionRange ||
                minimumHipsToSpineDistance < MinimumHipsToSpineDistance ||
                maximumHipsToSpineDistance > MaximumHipsToSpineDistance)
                throw new InvalidOperationException(
                    "The running hips and attacking upper body are not one continuous hierarchy. " +
                    "SpineLocalPositionRange=" + Num(spineLocalPositionRange) +
                    ", HipsToSpineDistance=" + Num(minimumHipsToSpineDistance) +
                    "-" + Num(maximumHipsToSpineDistance) + ".");
            if (maximumMusketAttachmentError > AttachmentTolerance ||
                maximumSwordAttachmentError > AttachmentTolerance)
                throw new InvalidOperationException("A rigid weapon changed relative to its follow bone.");
            if (maximumMusketFollowMotion < 0.1f || maximumSwordFollowMotion < 0.1f)
                throw new InvalidOperationException("A rigid weapon did not follow the animated body.");
            if (maximumNearestSwordVertexToHand > MaximumSwordVertexToHandDistance)
                throw new InvalidOperationException(
                    "The approved sword handle is too far from the right hand: " +
                    Num(maximumNearestSwordVertexToHand) + ".");
            if (maximumSwordForearmAngle > MaximumSwordForearmAngle)
                throw new InvalidOperationException(
                    "The approved sword blade diverges from the right forearm: " +
                    Num(maximumSwordForearmAngle) + " degrees.");
            if (maximumMusketBackContactDistance > MaximumMusketBackContactDistance)
                throw new InvalidOperationException(
                    "The rigid musket separates from the animated back: " +
                    Num(maximumMusketBackContactDistance) + "m.");

            return new Metrics(
                clip.length, clip.frameRate, horizontalRange, verticalRange,
                maximumMusketAttachmentError, maximumSwordAttachmentError,
                maximumMusketFollowMotion, maximumSwordFollowMotion,
                maximumHandMotion, loopHandError, maximumNearestSwordVertexToHand,
                maximumSwordForearmAngle, maximumSwordAngularStep,
                minimumMusketBackContactDistance, maximumMusketBackContactDistance,
                staticBounds.size.y, modelBounds.size.y, groundDifference,
                MeasureSwordDimensions(sword).BladeLength,
                maximumLeftFootMotion, maximumRightFootMotion,
                maximumLeftUpLegAngularMotion, maximumRightUpLegAngularMotion,
                loopLeftFootError, loopRightFootError,
                leftFootLateralRange, rightFootLateralRange,
                leftFootForwardRange, rightFootForwardRange,
                maximumLeftToeLateralDirection, maximumRightToeLateralDirection,
                spineLocalPositionRange, minimumHipsToSpineDistance,
                maximumHipsToSpineDistance);
        }

        private static void RequireExactSword(
            Transform staticModel, Transform drawModel, MeshRenderer target)
        {
            var staticSword = RequireRenderer<MeshRenderer>(staticModel, SwordRendererName);
            var drawSword = RequireRenderer<MeshRenderer>(drawModel, SwordRendererName);
            if (SharedMesh(target) != SharedMesh(staticSword))
                throw new InvalidOperationException("The running-attack sword is not the exact static shared mesh.");
            if (!target.sharedMaterials.SequenceEqual(staticSword.sharedMaterials))
                throw new InvalidOperationException("The running-attack sword materials differ from the static sword.");
            RequireLocalTransform(target.transform.parent, drawSword.transform.parent,
                "right-hand sword root mount");
            RequireLocalTransform(target.transform, drawSword.transform,
                "right-hand sword renderer correction");
            var staticDimensions = MeasureSwordDimensions(staticSword);
            var targetDimensions = MeasureSwordDimensions(target);
            if (Mathf.Abs(staticDimensions.BladeLength - TargetWorldBladeLength) > SwordDimensionTolerance ||
                Mathf.Abs(targetDimensions.BladeLength - staticDimensions.BladeLength) > SwordDimensionTolerance ||
                Mathf.Abs(targetDimensions.HandleSize - staticDimensions.HandleSize) > SwordDimensionTolerance)
                throw new InvalidOperationException(
                    "The running-attack sword world dimensions differ from the exact 0.6m static sword.");
        }

        private static SwordDimensions MeasureSwordDimensions(MeshRenderer renderer)
        {
            var mesh = SharedMesh(renderer);
            var grip = ApprovedGripCenterLocal * (mesh.bounds.size.z / ExpectedSwordLength);
            var vertices = mesh.vertices;
            var maximumZ = vertices.Max(vertex => vertex.z);
            var tipVertices = vertices.Where(vertex => maximumZ - vertex.z <= 0.000005f).ToArray();
            var tip = tipVertices.Aggregate(Vector3.zero, (sum, vertex) => sum + vertex) /
                      tipVertices.Length;
            var bladeLength = Vector3.Distance(
                renderer.transform.TransformPoint(grip), renderer.transform.TransformPoint(tip));
            var handlePoints = mesh.GetTriangles(1).Distinct()
                .Select(index => renderer.transform.TransformPoint(vertices[index])).ToArray();
            var handleSize = 0f;
            for (var first = 0; first < handlePoints.Length; first++)
            for (var second = first + 1; second < handlePoints.Length; second++)
                handleSize = Mathf.Max(handleSize,
                    Vector3.Distance(handlePoints[first], handlePoints[second]));
            return new SwordDimensions(bladeLength, handleSize);
        }

        private static void RequireLocalTransform(Transform actual, Transform expected, string label)
        {
            if (Vector3.Distance(actual.localPosition, expected.localPosition) > TransformTolerance ||
                Quaternion.Angle(actual.localRotation, expected.localRotation) > TransformTolerance ||
                Vector3.Distance(actual.localScale, expected.localScale) > TransformTolerance)
                throw new InvalidOperationException("The copied " + label + " differs.");
        }

        private static void RequireExactStaticMaterials(Transform staticModel, Transform model)
        {
            var approved = staticModel.GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .GroupBy(material => NormalizeMaterialName(material.name), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            foreach (var material in renderer.sharedMaterials)
            {
                if (material == null ||
                    !approved.TryGetValue(NormalizeMaterialName(material.name), out var exact) ||
                    material != exact)
                    throw new InvalidOperationException(
                        "A running-attack material is not a direct static appearance reference.");
            }
        }

        private static string NormalizeMaterialName(string name)
        {
            var result = name.Replace(" (Instance)", string.Empty);
            var suffix = result.LastIndexOf('.');
            if (suffix >= 0 && result.Length - suffix == 4 &&
                int.TryParse(result.Substring(suffix + 1), out _))
                result = result.Substring(0, suffix);
            return result;
        }

        private static void CaptureReview(
            Transform staticModel, Transform model, AnimationClip clip, string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The running-attack capture folder is invalid."));
            var transformSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var rendererSnapshots = model.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Select(renderer => new RendererSnapshot(renderer)).ToArray();
            var staticRenderers = staticModel.GetComponentsInChildren<Renderer>(true);
            var modelRenderers = model.GetComponentsInChildren<Renderer>(true);
            var sourceCamera = GameObject.Find("Player")?.GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException("The Player camera is missing for running-attack review.");
            var cameraObject = new GameObject("IspantRunningAttackReviewCamera", typeof(Camera))
                { hideFlags = HideFlags.HideAndDontSave };
            const int panelWidth = 640;
            const int panelHeight = 640;
            const int panels = 6;
            var strip = new Texture2D(panelWidth * panels, panelHeight, TextureFormat.RGB24, false);
            var target = new RenderTexture(panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelWidth, panelHeight, TextureFormat.RGB24, false);
            var oldActive = RenderTexture.active;
            try
            {
                foreach (var snapshot in rendererSnapshots)
                    snapshot.Renderer.enabled = false;
                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = ~0;
                camera.fieldOfView = 34f;
                camera.targetTexture = target;
                var staticBody = RequireRenderer<SkinnedMeshRenderer>(staticModel, "Ispant_Armed_Body");
                var body = RequireRenderer<SkinnedMeshRenderer>(model, "Ispant_Armed_Body");
                var referenceHeight = BindWorldBounds(staticBody).size.y;
                foreach (var renderer in staticRenderers)
                    renderer.enabled = true;
                FrameCamera(camera, staticBody.bounds.center, referenceHeight, 1f);
                RenderPanel(camera, panel, strip, target, 0, panelWidth, panelHeight);
                foreach (var renderer in staticRenderers)
                    renderer.enabled = false;
                foreach (var renderer in modelRenderers)
                    renderer.enabled = true;
                for (var index = 0; index < ReviewNormalizedTimes.Length; index++)
                {
                    SampleClip(model.gameObject, clip, ReviewNormalizedTimes[index] * clip.length);
                    FrameCamera(camera, body.bounds.center, referenceHeight, 1f);
                    RenderPanel(camera, panel, strip, target, index + 1, panelWidth, panelHeight);
                }
                strip.Apply();
                File.WriteAllBytes(destination, strip.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                foreach (var snapshot in rendererSnapshots)
                    snapshot.Restore();
                foreach (var snapshot in transformSnapshots)
                    snapshot.Restore();
                StopSampling();
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(strip);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void RenderPanel(
            Camera camera, Texture2D panel, Texture2D strip, RenderTexture target,
            int panelIndex, int width, int height)
        {
            camera.Render();
            RenderTexture.active = target;
            panel.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            panel.Apply();
            var pixels = panel.GetPixels32();
            if (pixels.Any(pixel => pixel.r >= 240 && pixel.b >= 240 && pixel.g <= 24))
                throw new InvalidOperationException("The running-attack review contains magenta shader fallback.");
            strip.SetPixels32(panelIndex * width, 0, width, height, pixels);
        }

        private static void FrameCamera(Camera camera, Vector3 center, float height, float aspect)
        {
            camera.aspect = aspect;
            var vertical = (height * 0.5f) /
                           Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            camera.transform.position = center + Vector3.back * vertical * 1.25f +
                                        Vector3.up * height * 0.01f;
            camera.transform.rotation = Quaternion.LookRotation(
                center - camera.transform.position, Vector3.up);
        }

        private static void WriteInspection(Metrics metrics)
        {
            var destination = Absolute(InspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The running-attack inspection folder is invalid."));
            File.WriteAllLines(destination, new[]
            {
                "Result=PASS",
                "Target=" + PlacementRootName + "/" + AttackSlotName,
                "SourceFbx=" + SourceFbxPath,
                "RunSourceFbx=" + RunSourceFbxPath,
                "ProjectSourceFbx=" + ProjectSourceFbxPath,
                "DerivedFbx=" + DerivedFbxPath,
                "SourceSha256=" + SourceSha256,
                "RunSourceSha256=" + RunSourceSha256,
                "StaticFbxSha256=" + StaticSha256,
                "DerivedSha256=" + DerivedSha256,
                "SourceAction=Armature|mixamo.com|Layer0",
                "RunSourceAction=Armature|mixamo.com|Layer0",
                "RunSourceFrames=1-39",
                "RunCyclesPerAttackLoop=2",
                "RunAndAttackRestRigMaximumMatrixError=0",
                "LowerBodyLocalPoseCopiedExactly=True",
                "LowerBodyCopiedBones=Hips,LeftUpLeg,LeftLeg,LeftFoot,LeftToeBase,RightUpLeg,RightLeg,RightFoot,RightToeBase",
                "BodyComposition=RunningHipsAndLegs+AttackSpineAndUpperBody",
                "UpperBodyLocalAttackCurvesUnchanged=True",
                "UpperBodyInheritsRunningHips=True",
                "BlenderReimportLowerBodyMaximumMatrixError=0.000034949",
                "BlenderReimportUpperBodyLocalMaximumMatrixError=0.000041487",
                "BlenderHipsToSpineDistanceMinimum=0.061837454",
                "BlenderHipsToSpineDistanceMaximum=0.06183771",
                "BlenderSpineLocalPositionSpan=0.000059198",
                "ImportedClip=" + ImportedClipName,
                "PlaybackClip=" + InPlaceClipName,
                "ClipFrames=" + FirstFrame + "-" + LastFrame,
                "ClipLengthSeconds=" + Num(metrics.ClipLength),
                "ClipFrameRate=" + Num(metrics.FrameRate),
                "LoopTime=True",
                "AnimatorApplyRootMotion=False",
                "HorizontalHipsRange=" + Num(metrics.HorizontalHipsRange),
                "VerticalHipsRange=" + Num(metrics.VerticalHipsRange),
                "RunningVerticalBodyMotion=True",
                "MaximumLeftFootMotion=" + Num(metrics.MaximumLeftFootMotion),
                "MaximumRightFootMotion=" + Num(metrics.MaximumRightFootMotion),
                "MaximumLeftUpLegAngularMotionDegrees=" + Num(metrics.MaximumLeftUpLegAngularMotion),
                "MaximumRightUpLegAngularMotionDegrees=" + Num(metrics.MaximumRightUpLegAngularMotion),
                "LeftFootLateralRange=" + Num(metrics.LeftFootLateralRange),
                "RightFootLateralRange=" + Num(metrics.RightFootLateralRange),
                "LeftFootForwardRange=" + Num(metrics.LeftFootForwardRange),
                "RightFootForwardRange=" + Num(metrics.RightFootForwardRange),
                "MaximumLeftToeLateralDirection=" + Num(metrics.MaximumLeftToeLateralDirection),
                "MaximumRightToeLateralDirection=" + Num(metrics.MaximumRightToeLateralDirection),
                "SpineLocalPositionRange=" + Num(metrics.SpineLocalPositionRange),
                "MinimumHipsToSpineDistance=" + Num(metrics.MinimumHipsToSpineDistance),
                "MaximumHipsToSpineDistance=" + Num(metrics.MaximumHipsToSpineDistance),
                "LoopLeftFootError=" + Num(metrics.LoopLeftFootError),
                "LoopRightFootError=" + Num(metrics.LoopRightFootError),
                "RightHandMaximumMotion=" + Num(metrics.MaximumHandMotion),
                "LoopRightHandError=" + Num(metrics.LoopHandError),
                "MixamoBones=" + ExpectedBones,
                "AnimatedBodyTriangles=" + ExpectedBodyTriangles,
                "CrescentTriangles=" + ExpectedCrescentTriangles,
                "EyeTriangles=" + ExpectedEyeTriangles,
                "RigidMusketTriangles=" + ExpectedMusketTriangles,
                "MusketStaticComponents=41,75,76",
                "MusketParent=mixamorig:Spine2",
                "MusketSkinned=False",
                "MusketParentBoundInRestPose=True",
                "MusketBackCorrectionBoneLocal=0.525849342,1.556176186,2.671695709",
                "MusketMaximumAttachmentError=" + Num(metrics.MaximumMusketAttachmentError),
                "MusketMaximumBodyFollowMotion=" + Num(metrics.MaximumMusketFollowMotion),
                "MinimumMusketBackContactDistance=" + Num(metrics.MinimumMusketBackContactDistance),
                "MaximumMusketBackContactDistance=" + Num(metrics.MaximumMusketBackContactDistance),
                "SwordSource=Ispant_01_Static shared mesh and materials",
                "SwordMountSource=Ispant_04_DrawSword exact right-hand local transform",
                "SwordTriangles=" + ExpectedSwordTriangles,
                "SwordParent=mixamorig:RightHand",
                "SwordSkinned=False",
                "SwordWorldBladeLength=" + Num(metrics.SwordBladeLength),
                "SwordMaximumAttachmentError=" + Num(metrics.MaximumSwordAttachmentError),
                "SwordMaximumBodyFollowMotion=" + Num(metrics.MaximumSwordFollowMotion),
                "MaximumNearestSwordVertexToHand=" + Num(metrics.MaximumNearestSwordVertexToHand),
                "MaximumSwordForearmAngleDegrees=" + Num(metrics.MaximumSwordForearmAngle),
                "MaximumSwordAngularStepDegrees=" + Num(metrics.MaximumSwordAngularStep),
                "SwordAngleDriver=RightForeArmToRightHandAxis",
                "StaticBodyHeight=" + Num(metrics.StaticBodyHeight),
                "AttackBodyHeight=" + Num(metrics.AttackBodyHeight),
                "GroundLevelDifference=" + Num(metrics.GroundLevelDifference),
                "StaticAppearanceMaterialsDirectReference=True",
                "SourceStaticGeometryMaximumWorldVertexError=0.0000002",
                "OtherSlotsChanged=False",
                "OtherSceneRootsChanged=False",
                "ReviewImage=" + CapturePath
            }, Encoding.UTF8);
        }

        private static void RequireHashes()
        {
            RequireHash(SourceFbxPath, SourceSha256);
            RequireHash(ProjectSourceFbxPath, SourceSha256);
            RequireHash(RunSourceFbxPath, RunSourceSha256);
            RequireHash(StaticFbxPath, StaticSha256);
            RequireHash(DerivedFbxPath, DerivedSha256);
        }

        private static void RequireHash(string path, string expected)
        {
            using var stream = File.OpenRead(Absolute(path));
            using var sha = SHA256.Create();
            var actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Ispant running-attack asset hash differs: " + path + ".");
        }

        private static Scene RequireScene(bool requireClean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be active for running-attack work.");
            if (requireClean && scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp has unsaved changes.");
            return scene;
        }

        private static GameObject RequirePlacement(Scene scene)
        {
            var roots = scene.GetRootGameObjects().Where(item => item.name == PlacementRootName).ToArray();
            if (roots.Length != 1 || roots[0].transform.childCount != ExpectedSlots)
                throw new InvalidOperationException("The approved Ispant placement contract differs.");
            return roots[0];
        }

        private static Transform RequireSlot(Transform placement, string name, int index)
        {
            if (index < 0 || index >= placement.childCount || placement.GetChild(index).name != name)
                throw new InvalidOperationException("The required Ispant slot differs: " + name + ".");
            return placement.GetChild(index);
        }

        private static Transform RequireDirectChild(Transform parent, string name)
        {
            return parent.Cast<Transform>().SingleOrDefault(child => child.name == name) ??
                   throw new InvalidOperationException(
                       "Required direct child is missing: " + parent.name + "/" + name + ".");
        }

        private static T RequireRenderer<T>(Transform model, string name) where T : Renderer
        {
            return model.GetComponentsInChildren<T>(true).SingleOrDefault(item => item.name == name) ??
                   throw new InvalidOperationException("Required running-attack renderer is missing: " + name + ".");
        }

        private static Transform RequireDescendant(Transform root, string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Required running-attack bone differs: " + name + ".");
            return matches[0];
        }

        private static Mesh SharedMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
                return skinned.sharedMesh;
            var filter = renderer.GetComponent<MeshFilter>();
            return filter != null && filter.sharedMesh != null
                ? filter.sharedMesh
                : throw new InvalidOperationException("A running-attack renderer has no mesh: " + renderer.name + ".");
        }

        private static int TriangleCount(Mesh mesh)
        {
            var result = 0;
            for (var index = 0; index < mesh.subMeshCount; index++)
                result += checked((int)mesh.GetIndexCount(index) / 3);
            return result;
        }

        private static Vector3[] SkinnedWorldVertices(SkinnedMeshRenderer renderer)
        {
            var mesh = SharedMesh(renderer);
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var bindPoses = mesh.bindposes;
            var bones = renderer.bones;
            if (weights.Length != vertices.Length || bindPoses.Length != bones.Length)
                throw new InvalidOperationException("The running-attack skinning data differs.");
            var boneMatrices = Enumerable.Range(0, bones.Length)
                .Select(index => bones[index].localToWorldMatrix * bindPoses[index])
                .ToArray();
            var result = new Vector3[vertices.Length];
            for (var index = 0; index < vertices.Length; index++)
            {
                var weight = weights[index];
                var world = Vector3.zero;
                if (weight.weight0 > 0f)
                    world += boneMatrices[weight.boneIndex0].MultiplyPoint3x4(vertices[index]) *
                             weight.weight0;
                if (weight.weight1 > 0f)
                    world += boneMatrices[weight.boneIndex1].MultiplyPoint3x4(vertices[index]) *
                             weight.weight1;
                if (weight.weight2 > 0f)
                    world += boneMatrices[weight.boneIndex2].MultiplyPoint3x4(vertices[index]) *
                             weight.weight2;
                if (weight.weight3 > 0f)
                    world += boneMatrices[weight.boneIndex3].MultiplyPoint3x4(vertices[index]) *
                             weight.weight3;
                result[index] = world;
            }
            return result;
        }

        private static Bounds BindWorldBounds(SkinnedMeshRenderer renderer)
        {
            var vertices = SharedMesh(renderer).vertices;
            if (vertices.Length == 0)
                throw new InvalidOperationException("A running-attack mesh has no vertices.");
            var bounds = new Bounds(renderer.transform.TransformPoint(vertices[0]), Vector3.zero);
            for (var index = 1; index < vertices.Length; index++)
                bounds.Encapsulate(renderer.transform.TransformPoint(vertices[index]));
            return bounds;
        }

        private static void SampleClip(GameObject model, AnimationClip clip, float time)
        {
            if (!AnimationMode.InAnimationMode())
                AnimationMode.StartAnimationMode();
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(model, clip, time);
            AnimationMode.EndSampling();
        }

        private static void StopSampling()
        {
            if (AnimationMode.InAnimationMode())
                AnimationMode.StopAnimationMode();
        }

        private static Matrix4x4 LocalMatrix(Transform transform)
        {
            return Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale);
        }

        private static float MatrixError(Matrix4x4 expected, Matrix4x4 actual)
        {
            var maximum = 0f;
            for (var row = 0; row < 4; row++)
            for (var column = 0; column < 4; column++)
                maximum = Mathf.Max(maximum, Mathf.Abs(expected[row, column] - actual[row, column]));
            return maximum;
        }

        private static string[] OtherRootSignatures(Scene scene, GameObject placement)
        {
            return scene.GetRootGameObjects().Where(item => item != placement)
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .Select(item => RecursiveSignature(item.transform)).ToArray();
        }

        private static string[] OtherSlotSignatures(Transform placement, Transform targetSlot)
        {
            return Enumerable.Range(0, placement.childCount).Select(placement.GetChild)
                .Where(item => item != targetSlot).Select(RecursiveSignature).ToArray();
        }

        private static string RecursiveSignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(item.name).Append('|').Append(item.gameObject.activeSelf).Append('|')
                    .Append(Vec(item.localPosition)).Append('|')
                    .Append(Num(item.localRotation.x)).Append(',').Append(Num(item.localRotation.y)).Append(',')
                    .Append(Num(item.localRotation.z)).Append(',').Append(Num(item.localRotation.w)).Append('|')
                    .Append(Vec(item.localScale));
                foreach (var renderer in item.GetComponents<Renderer>())
                {
                    Mesh mesh;
                    if (renderer is SkinnedMeshRenderer skinned)
                    {
                        mesh = skinned.sharedMesh;
                    }
                    else
                    {
                        var filter = renderer.GetComponent<MeshFilter>();
                        mesh = filter != null ? filter.sharedMesh : null;
                    }
                    builder.Append("|R:").Append(renderer.enabled).Append(':')
                        .Append(mesh != null
                            ? AssetDatabase.GetAssetPath(mesh)
                            : renderer.GetType().FullName);
                    foreach (var material in renderer.sharedMaterials)
                        builder.Append(':').Append(AssetDatabase.GetAssetPath(material));
                }
            }
            return builder.ToString();
        }

        private static void RequireEqual(string[] expected, string[] actual, string message)
        {
            if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
                throw new InvalidOperationException(message);
        }

        private static string Absolute(string path)
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
        }

        private static string Num(float value)
        {
            return value.ToString("0.#########", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return Num(value.x) + "," + Num(value.y) + "," + Num(value.z);
        }

        private readonly struct SwordDimensions
        {
            public readonly float BladeLength;
            public readonly float HandleSize;
            public SwordDimensions(float bladeLength, float handleSize)
            {
                BladeLength = bladeLength;
                HandleSize = handleSize;
            }
        }

        private readonly struct Metrics
        {
            public readonly float ClipLength;
            public readonly float FrameRate;
            public readonly float HorizontalHipsRange;
            public readonly float VerticalHipsRange;
            public readonly float MaximumMusketAttachmentError;
            public readonly float MaximumSwordAttachmentError;
            public readonly float MaximumMusketFollowMotion;
            public readonly float MaximumSwordFollowMotion;
            public readonly float MaximumHandMotion;
            public readonly float LoopHandError;
            public readonly float MaximumNearestSwordVertexToHand;
            public readonly float MaximumSwordForearmAngle;
            public readonly float MaximumSwordAngularStep;
            public readonly float MinimumMusketBackContactDistance;
            public readonly float MaximumMusketBackContactDistance;
            public readonly float StaticBodyHeight;
            public readonly float AttackBodyHeight;
            public readonly float GroundLevelDifference;
            public readonly float SwordBladeLength;
            public readonly float MaximumLeftFootMotion;
            public readonly float MaximumRightFootMotion;
            public readonly float MaximumLeftUpLegAngularMotion;
            public readonly float MaximumRightUpLegAngularMotion;
            public readonly float LoopLeftFootError;
            public readonly float LoopRightFootError;
            public readonly float LeftFootLateralRange;
            public readonly float RightFootLateralRange;
            public readonly float LeftFootForwardRange;
            public readonly float RightFootForwardRange;
            public readonly float MaximumLeftToeLateralDirection;
            public readonly float MaximumRightToeLateralDirection;
            public readonly float SpineLocalPositionRange;
            public readonly float MinimumHipsToSpineDistance;
            public readonly float MaximumHipsToSpineDistance;

            public Metrics(
                float clipLength, float frameRate, float horizontalHipsRange,
                float verticalHipsRange, float maximumMusketAttachmentError,
                float maximumSwordAttachmentError, float maximumMusketFollowMotion,
                float maximumSwordFollowMotion, float maximumHandMotion,
                float loopHandError, float maximumNearestSwordVertexToHand,
                float maximumSwordForearmAngle, float maximumSwordAngularStep,
                float minimumMusketBackContactDistance,
                float maximumMusketBackContactDistance,
                float staticBodyHeight, float attackBodyHeight,
                float groundLevelDifference, float swordBladeLength,
                float maximumLeftFootMotion, float maximumRightFootMotion,
                float maximumLeftUpLegAngularMotion,
                float maximumRightUpLegAngularMotion,
                float loopLeftFootError, float loopRightFootError,
                float leftFootLateralRange, float rightFootLateralRange,
                float leftFootForwardRange, float rightFootForwardRange,
                float maximumLeftToeLateralDirection,
                float maximumRightToeLateralDirection,
                float spineLocalPositionRange,
                float minimumHipsToSpineDistance,
                float maximumHipsToSpineDistance)
            {
                ClipLength = clipLength;
                FrameRate = frameRate;
                HorizontalHipsRange = horizontalHipsRange;
                VerticalHipsRange = verticalHipsRange;
                MaximumMusketAttachmentError = maximumMusketAttachmentError;
                MaximumSwordAttachmentError = maximumSwordAttachmentError;
                MaximumMusketFollowMotion = maximumMusketFollowMotion;
                MaximumSwordFollowMotion = maximumSwordFollowMotion;
                MaximumHandMotion = maximumHandMotion;
                LoopHandError = loopHandError;
                MaximumNearestSwordVertexToHand = maximumNearestSwordVertexToHand;
                MaximumSwordForearmAngle = maximumSwordForearmAngle;
                MaximumSwordAngularStep = maximumSwordAngularStep;
                MinimumMusketBackContactDistance = minimumMusketBackContactDistance;
                MaximumMusketBackContactDistance = maximumMusketBackContactDistance;
                StaticBodyHeight = staticBodyHeight;
                AttackBodyHeight = attackBodyHeight;
                GroundLevelDifference = groundLevelDifference;
                SwordBladeLength = swordBladeLength;
                MaximumLeftFootMotion = maximumLeftFootMotion;
                MaximumRightFootMotion = maximumRightFootMotion;
                MaximumLeftUpLegAngularMotion = maximumLeftUpLegAngularMotion;
                MaximumRightUpLegAngularMotion = maximumRightUpLegAngularMotion;
                LoopLeftFootError = loopLeftFootError;
                LoopRightFootError = loopRightFootError;
                LeftFootLateralRange = leftFootLateralRange;
                RightFootLateralRange = rightFootLateralRange;
                LeftFootForwardRange = leftFootForwardRange;
                RightFootForwardRange = rightFootForwardRange;
                MaximumLeftToeLateralDirection = maximumLeftToeLateralDirection;
                MaximumRightToeLateralDirection = maximumRightToeLateralDirection;
                SpineLocalPositionRange = spineLocalPositionRange;
                MinimumHipsToSpineDistance = minimumHipsToSpineDistance;
                MaximumHipsToSpineDistance = maximumHipsToSpineDistance;
            }
        }

        private readonly struct TransformSnapshot
        {
            private readonly Transform _transform;
            private readonly Vector3 _position;
            private readonly Quaternion _rotation;
            private readonly Vector3 _scale;

            public TransformSnapshot(Transform transform)
            {
                _transform = transform;
                _position = transform.localPosition;
                _rotation = transform.localRotation;
                _scale = transform.localScale;
            }

            public bool Matches(float tolerance)
            {
                return Vector3.Distance(_transform.localPosition, _position) <= tolerance &&
                       Quaternion.Angle(_transform.localRotation, _rotation) <= tolerance &&
                       Vector3.Distance(_transform.localScale, _scale) <= tolerance;
            }

            public void Restore()
            {
                if (_transform == null)
                    return;
                _transform.localPosition = _position;
                _transform.localRotation = _rotation;
                _transform.localScale = _scale;
            }
        }

        private readonly struct RendererSnapshot
        {
            public readonly Renderer Renderer;
            private readonly bool _enabled;
            public RendererSnapshot(Renderer renderer)
            {
                Renderer = renderer;
                _enabled = renderer.enabled;
            }
            public void Restore()
            {
                if (Renderer != null)
                    Renderer.enabled = _enabled;
            }
        }
    }
}
