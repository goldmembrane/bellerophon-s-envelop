using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Bellerophon.Enemies.Ispant;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.IspantCargoRunScene
{
    internal static class IspantNewDrawSwordAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementName = "Approved Ispant Enemy Placement";
        private const string SlotName = "Ispant_04_DrawSword";
        private const string ModelName = "Ispant_New_Direct_Model";
        private const string SourcePath = "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_New_DrawSword_Source.fbx";
        private const string ModelPath = "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_Direct_Source.fbx";
        private const string CorrectedBodyMeshPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_DrawSword_Body.asset";
        private const string ClipPath = "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_New_DrawSword_Loop.anim";
        private const string ControllerFolder = "Assets/_Project/Art/Enemies/Ispant/Controllers";
        private const string ControllerPath = "Assets/_Project/Art/Enemies/Ispant/Controllers/Ispant_New_DrawSword.controller";
        private const string CaptureFolder = "docs/validation/ispant_draw_sword_fix_2026-08-19/captures";
        private const string LeftArmCaptureFolder =
            "docs/validation/ispant_draw_sword_left_arm_fix_2026-08-19/captures";
        private const string ImportedClipName = "Ispant_New_DrawSword_Mixamo";
        private const string LoopClipName = "Ispant_New_DrawSword_Loop";
        private const string SourceHash = "EFF460E3201EFF5749A13705898B019C68036F25A7FEEFC9B18F7503FCEF1F81";
        private const string ModelHash = "5CE54F6117AF08F141BC18A0E46C823AD07877D815DA2906D59CA2967A4974FF";
        private const float TransformTolerance = 0.0001f;
        private const float GripOffsetTolerance = 0.001f;
        private const float RequestedGripOutwardOffset = 0.10f;
        private const float GripContactInset = 0.005f;
        private const int RequiredLoops = 2;
        private const int CaptureLayer = 29;
        private static readonly Vector3 SwordBladeLocalAxis = Vector3.left;
        private static readonly Vector3 SwordRollLocalAxis = Vector3.up;
        private static readonly string[] LeftArmBoneNames =
            { "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand" };
        private static readonly string[] LeftArmAnimatedBoneNames =
            { "LeftShoulder", "LeftArm", "LeftForeArm" };
        private static readonly string[] LeftLegBoneNames =
            { "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase" };

        // The current 10K sword shaft is the narrow region between guard and pommel.
        // These imported local-X limits come from the unchanged hashed mesh's 483 source vertices.
        private const float GripMinimumLocalX = 0.0000487f;
        private const float GripMaximumLocalX = 0.0000974f;

        private static bool reviewActive;
        private static double reviewStart;
        private static TransformSnapshot[] reviewSnapshots;
        private static SceneView reviewView;
        private static bool reviewGizmos;

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect New Mixamo Draw Sword Source")]
        public static void InspectIspantNewDrawSwordSource()
        {
            RequireHashes();
            var importer = RequireImporter();
            var defaults = importer.defaultClipAnimations ?? Array.Empty<ModelImporterClipAnimation>();
            var clips = ImportedClips();
            var mixamoTakes = defaults.Where(IsMixamo).ToArray();
            var mixamoClips = clips.Where(item => item.name.IndexOf("mixamo", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            var sourceBones = BoneDescriptions(RequireAsset<GameObject>(SourcePath).transform);
            var modelBones = BoneDescriptions(RequireAsset<GameObject>(ModelPath).transform);
            var target = RequireTarget(RequireScene(true));
            var body = RequireBody(target.Model);
            var sword = RequireSword(target.Model);
            var mesh = sword.GetComponent<MeshFilter>().sharedMesh;
            var hand = RequireDescendant(target.Model, "RightHand");
            var palm = WeightedPalmCenter(body, hand);
            var seam = mixamoClips.Length == 1 ? InspectSourceSeam(target.Model, mixamoClips[0]) : default;
            Debug.Log(
                "IspantNewDrawSwordSourceInspected Result=PASS" +
                ", AnimationType=" + importer.animationType + ", ImportAnimation=" + importer.importAnimation +
                ", DefaultClipCount=" + defaults.Length +
                ", DefaultClips=" + string.Join("|", defaults.Select(DescribeClip)) +
                ", ImportedClipCount=" + clips.Length +
                ", ImportedClips=" + string.Join("|", clips.Select(item => item.name + "[Length=" + Num(item.length) + ",Fps=" + Num(item.frameRate) + "]")) +
                ", MixamoTakeCount=" + mixamoTakes.Length +
                ", MixamoTakes=" + string.Join("|", mixamoTakes.Select(DescribeClip)) +
                ", MixamoPositionCurves=" + (mixamoClips.Length == 1 ? PositionCurves(mixamoClips[0]) : "<ambiguous>") +
                ", ExactBoneHierarchyMatch=" + sourceBones.SequenceEqual(modelBones, StringComparer.Ordinal) +
                ", SwordVertices=" + mesh.vertexCount + ", SwordTriangles=" + TriangleCount(mesh) +
                ", SwordBoundsCenter=" + Vec(mesh.bounds.center) + ", SwordBoundsSize=" + Vec(mesh.bounds.size) +
                ", SwordSkinned=False, RightPalmWorld=" + Vec(palm) +
                ", RightHandSeamPosition=" + Num(seam.HandPosition) + ", RightHandSeamAngle=" + Num(seam.HandAngle) +
                ", HipsSeamPosition=" + Num(seam.HipsPosition) + ", HipsSeamAngle=" + Num(seam.HipsAngle) +
                ", TargetAnimatorCount=" + target.Model.GetComponentsInChildren<Animator>(true).Length + ", SceneDirty=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Apply New Mixamo Draw Sword Animation")]
        public static void ApplyIspantNewDrawSwordAnimation()
        {
            RequireHashes();
            ConfigureImporter();
            RequireHashes();
            var scene = RequireScene(true);
            var target = RequireTarget(scene);
            var source = RequireImportedClip();
            var clip = CreateLoopClip(source);
            var controller = CreateController(clip);
            var slotBefore = new TransformSnapshot(target.Slot);
            var modelBefore = new TransformSnapshot(target.Model);
            var swordBefore = SwordSignature(target.Model);
            var othersBefore = OtherSlotSignatures(target.Placement, target.Slot);
            var rootsBefore = OtherRootSignatures(scene, target.Placement);
            ConfigureAnimator(target.Model, controller);
            ConfigureSwordFollower(target);
            if (!slotBefore.Matches(TransformTolerance) || !modelBefore.Matches(TransformTolerance))
                throw new InvalidOperationException("The draw-sword slot or direct model transform changed.");
            RequireSame(swordBefore, SwordSignature(target.Model), "The current approved sword asset or material changed.");
            RequireSame(othersBefore, OtherSlotSignatures(target.Placement, target.Slot), "Another Ispant slot changed.");
            RequireSame(rootsBefore, OtherRootSignatures(scene, target.Placement), "A scene root outside the Ispant placement changed.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("CargoRunMvp could not be saved.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = target.Slot.gameObject;
            Debug.Log(
                "IspantNewDrawSwordAnimationApplied Result=PASS" +
                ", Target=" + PlacementName + "/" + SlotName + "/" + ModelName +
                ", SourceClip=" + SourcePath + "/" + ImportedClipName +
                ", LoopClip=" + ClipPath + ", Controller=" + ControllerPath +
                ", SourceLength=" + Num(source.length) + ", LoopLength=" + Num(clip.length) +
                ", FrameRate=" + Num(clip.frameRate) +
                ", LoopMode=MixamoForwardThenImmediateReset, RootMotion=False" +
                ", SwordRigid=True, SwordDriver=RightArmRealtimeLateUpdate" +
                ", RequestedGripOutwardOffset=" + Num(RequestedGripOutwardOffset) +
                ", AppliedGripOutwardOffset=" + Num(target.Slot.GetComponent<IspantRigidSwordFollower>().GripOutwardOffset) +
                ", BladeDirection=SmoothWholeClipToVisibleUp" +
                ", OtherSlotsChanged=False, OtherSceneRootsChanged=False, SwordMeshChanged=False, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Apply New Draw Sword Left Arm Skinning Fix")]
        public static void ApplyIspantNewDrawSwordLeftArmSkinningFix()
        {
            RequireHashes();
            var scene = RequireScene(true);
            var target = RequireTarget(scene);
            var body = RequireBody(target.Model);
            var directBody = RequireAsset<GameObject>(ModelPath)
                .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var directMesh = directBody.sharedMesh;
            if (!body.bones.Select(item => item.name)
                    .SequenceEqual(directBody.bones.Select(item => item.name), StringComparer.Ordinal))
                throw new InvalidOperationException("The direct and target body bone orders differ.");
            if (directMesh.bindposes.Length != body.bones.Length || directMesh.subMeshCount != body.sharedMaterials.Length)
                throw new InvalidOperationException("The direct body mesh is incompatible with the target renderer.");

            var slotBefore = new TransformSnapshot(target.Slot);
            var modelBefore = new TransformSnapshot(target.Model);
            var materialsBefore = string.Join("|", body.sharedMaterials.Select(AssetDatabase.GetAssetPath));
            var swordBefore = SwordSignature(target.Model);
            var othersBefore = OtherSlotSignatures(target.Placement, target.Slot);
            var rootsBefore = OtherRootSignatures(scene, target.Placement);
            var correctedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(CorrectedBodyMeshPath);
            if (correctedMesh == null)
            {
                correctedMesh = UnityEngine.Object.Instantiate(directMesh);
                correctedMesh.name = "Ispant_New_DrawSword_Body";
                AssetDatabase.CreateAsset(correctedMesh, CorrectedBodyMeshPath);
            }
            else
            {
                EditorUtility.CopySerialized(directMesh, correctedMesh);
                correctedMesh.name = "Ispant_New_DrawSword_Body";
            }
            var removedTriangles = RemoveProblemComponentTriangles(correctedMesh);
            var armBones = new HashSet<int>(LeftArmBoneNames.Select(name =>
                Array.FindIndex(body.bones, bone => bone != null && bone.name == name)));
            var legBones = new HashSet<int>(LeftLegBoneNames.Select(name =>
                Array.FindIndex(body.bones, bone => bone != null && bone.name == name)));
            correctedMesh.boneWeights = BuildArmLegComponentSeparatedWeights(
                directMesh, body.bones, MeshConnectedComponents(directMesh), armBones, legBones,
                out var separatedArmComponents,
                out var separatedBodyComponents,
                out var separatedChangedVertices);
            EditorUtility.SetDirty(correctedMesh);

            body.sharedMesh = correctedMesh;
            body.quality = SkinQuality.Bone4;
            EditorUtility.SetDirty(body);
            RequireCorrectedBodyMesh(body, correctedMesh);
            if (!slotBefore.Matches(TransformTolerance) || !modelBefore.Matches(TransformTolerance))
                throw new InvalidOperationException("The draw-sword slot or direct model transform changed.");
            RequireSame(materialsBefore, string.Join("|", body.sharedMaterials.Select(AssetDatabase.GetAssetPath)),
                "The direct model body materials changed.");
            RequireSame(swordBefore, SwordSignature(target.Model), "The current approved sword asset or material changed.");
            RequireSame(othersBefore, OtherSlotSignatures(target.Placement, target.Slot), "Another Ispant slot changed.");
            RequireSame(rootsBefore, OtherRootSignatures(scene, target.Placement),
                "A scene root outside the Ispant placement changed.");
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("CargoRunMvp could not be saved.");
            Selection.activeGameObject = target.Slot.gameObject;
            Debug.Log(
                "IspantNewDrawSwordLeftArmSkinningFixApplied Result=PASS" +
                ", Target=" + PlacementName + "/" + SlotName + "/" + ModelName + "/char1" +
                ", CorrectedMesh=" + CorrectedBodyMeshPath +
                ", Vertices=" + correctedMesh.vertexCount +
                ", Triangles=" + TriangleCount(correctedMesh) +
                ", BoneCount=" + body.bones.Length +
                ", SkinQuality=" + body.quality +
                ", RemovedComponents=9, RemovedVertices=116, RemovedTriangles=" + removedTriangles +
                ", GeometryAlreadySeparated=True" +
                ", SeparatedArmComponents=" + separatedArmComponents +
                ", SeparatedBodyComponents=" + separatedBodyComponents +
                ", SeparatedChangedVertices=" + separatedChangedVertices +
                ", AnimationCurvesChanged=False, BoneTransformsChanged=False, MaterialsChanged=False" +
                ", OtherSlotsChanged=False, OtherSceneRootsChanged=False, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect New Mixamo Draw Sword Animation")]
        public static void InspectIspantNewDrawSwordAnimation()
        {
            var result = InspectApplied(RequireScene(true), true);
            var body = RequireBody(result.Target.Model);
            var leftArmSkinning = InspectLeftArmSkinning(result.Target.Model, result.Clip);
            var leftArmLegCoupling = InspectLeftArmLegCoupling(result.Target.Model, result.Clip);
            var separatedLeftArmComponents = InspectSeparatedLeftArmComponents(result.Target.Model);
            var bodyBinding = InspectBodyBinding(body);
            var sourceSkinning = InspectSourceSkinningCompatibility(result.Target.Model, result.Clip);
            var directRepairCandidates = InspectDirectMeshRepairCandidates(result.Target.Model, result.Clip);
            Debug.Log(
                "IspantNewDrawSwordAnimationInspected Result=PASS" +
                ", Length=" + Num(result.Clip.length) + ", FrameRate=" + Num(result.Clip.frameRate) +
                ", Loop=True, RootMotion=False" +
                ", AppliedGripOutwardOffset=" + Num(result.AppliedGripOutwardOffset) +
                ", MaximumGripOffsetError=" + Num(result.MaximumGripOffsetError) +
                ", MinimumGripContactMargin=" + Num(result.MinimumGripContactMargin) +
                ", StartBladeToUpAngle=" + Num(result.StartBladeToUpAngle) +
                ", FinalBladeToUpAngle=" + Num(result.FinalBladeToUpAngle) +
                ", MaximumBladeAngularStep=" + Num(result.MaximumBladeAngularStep) +
                ", BladeChangingFrames=" + result.BladeChangingFrames +
                ", MaximumSwordMotion=" + Num(result.MaximumSwordMotion) +
                ", MaximumRightHandMotion=" + Num(result.MaximumHandMotion) +
                ", ImmediateResetPosition=" + Num(result.ImmediateResetPosition) +
                ", ImmediateResetAngle=" + Num(result.ImmediateResetAngle) +
                ", BodySkinQuality=" + body.quality +
                ", BodyBinding=" + bodyBinding +
                ", SourceSkinning=" + sourceSkinning +
                 ", DirectRepairCandidates=" + directRepairCandidates +
                 ", LeftArmLegCoupling=" + leftArmLegCoupling +
                 ", SeparatedLeftArmComponents=" + separatedLeftArmComponents +
                 ", LeftArmSkinning=" + leftArmSkinning +
                ", SwordVertices=" + result.SwordVertices + ", SwordTriangles=" + result.SwordTriangles +
                ", SwordSkinned=False, SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture New Mixamo Draw Sword Visual Review")]
        public static void CaptureIspantNewDrawSwordVisualReview()
        {
            var scene = RequireScene(true);
            var dirtyBefore = scene.isDirty;
            var result = InspectApplied(scene, true);
            var cloneObject = UnityEngine.Object.Instantiate(result.Target.Slot.gameObject);
            cloneObject.name = "Ispant_04_DrawSword_VisualReviewClone";
            cloneObject.hideFlags = HideFlags.HideAndDontSave;
            var cameraObject = new GameObject("Ispant_DrawSword_VisualReviewCamera")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = CaptureLayer
            };
            var lightObject = new GameObject("Ispant_DrawSword_VisualReviewLight")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = CaptureLayer
            };
            try
            {
                cloneObject.SetActive(true);
                SetLayer(cloneObject.transform, CaptureLayer);
                var model = cloneObject.transform.GetComponentsInChildren<Transform>(true)
                    .Single(item => item.name == ModelName);
                var follower = cloneObject.GetComponent<IspantRigidSwordFollower>() ??
                    throw new InvalidOperationException("The visual review clone sword follower is missing.");
                foreach (var skinned in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    skinned.updateWhenOffscreen = true;
                    skinned.forceMatrixRecalculationPerRender = true;
                }

                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.04f, 0.055f, 1f);
                camera.fieldOfView = 32f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 200f;
                camera.allowHDR = false;

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2.5f;
                light.color = new Color(1f, 0.94f, 0.86f, 1f);
                light.cullingMask = 1 << CaptureLayer;
                lightObject.transform.rotation = Quaternion.Euler(38f, -32f, 0f);

                var folder = Path.GetFullPath(CaptureFolder);
                Directory.CreateDirectory(folder);
                foreach (var file in Directory.GetFiles(folder, "*.png")) File.Delete(file);

                AnimationMode.StartAnimationMode();
                CapturePose(model, follower, result.Clip, camera, 0f, false, false, false, Path.Combine(folder, "01_Start_Oblique.png"));
                CapturePose(model, follower, result.Clip, camera, 0.25f, false, false, false, Path.Combine(folder, "02_Quarter_Oblique.png"));
                CapturePose(model, follower, result.Clip, camera, 0.5f, false, false, false, Path.Combine(folder, "03_Middle_Oblique.png"));
                CapturePose(model, follower, result.Clip, camera, 0.75f, false, false, false, Path.Combine(folder, "04_ThreeQuarter_Oblique.png"));
                CapturePose(model, follower, result.Clip, camera, 1f, false, false, false, Path.Combine(folder, "05_End_Oblique.png"));
                CapturePose(model, follower, result.Clip, camera, 1f, false, true, false, Path.Combine(folder, "06_End_Grip_Close.png"));
                CapturePose(model, follower, result.Clip, camera, 1f, true, false, false, Path.Combine(folder, "07_End_Side.png"));
                CapturePose(model, follower, result.Clip, camera, 0f, false, false, true, Path.Combine(folder, "08_Start_UserFront.png"));
                CapturePose(model, follower, result.Clip, camera, 0.25f, false, false, true, Path.Combine(folder, "09_Quarter_UserFront.png"));
                CapturePose(model, follower, result.Clip, camera, 0.5f, false, false, true, Path.Combine(folder, "10_Middle_UserFront.png"));
                CapturePose(model, follower, result.Clip, camera, 60f / 90f, false, false, false, Path.Combine(folder, "11_Frame60_Oblique.png"));
                CapturePose(model, follower, result.Clip, camera, 62f / 90f, false, false, false, Path.Combine(folder, "12_Frame62_Oblique.png"));
                CapturePose(model, follower, result.Clip, camera, 60f / 90f, false, false, true, Path.Combine(folder, "13_Frame60_UserFront.png"));
                CapturePose(model, follower, result.Clip, camera, 62f / 90f, false, false, true, Path.Combine(folder, "14_Frame62_UserFront.png"));
                CaptureLeftArmPose(model, follower, result.Clip, camera, 0f, Path.Combine(folder, "15_Start_LeftArmClose.png"));
                CaptureLeftArmPose(model, follower, result.Clip, camera, 0.25f, Path.Combine(folder, "16_Quarter_LeftArmClose.png"));
                CaptureLeftArmPose(model, follower, result.Clip, camera, 40f / 90f, Path.Combine(folder, "17_Frame40_LeftArmClose.png"));
                CaptureLeftArmPose(model, follower, result.Clip, camera, 0.5f, Path.Combine(folder, "18_Middle_LeftArmClose.png"));
                CaptureLeftArmPose(model, follower, result.Clip, camera, 60f / 90f, Path.Combine(folder, "19_Frame60_LeftArmClose.png"));
                CaptureLeftArmPose(model, follower, result.Clip, camera, 62f / 90f, Path.Combine(folder, "20_Frame62_LeftArmClose.png"));
                CaptureLeftArmPose(model, follower, result.Clip, camera, 0.75f, Path.Combine(folder, "21_ThreeQuarter_LeftArmClose.png"));
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(cloneObject);
            }
            if (scene.isDirty != dirtyBefore)
                throw new InvalidOperationException("The visual review capture changed CargoRunMvp.");
            Debug.Log(
                "IspantNewDrawSwordVisualReviewCaptured Result=PASS, Frames=21" +
                ", Folder=" + CaptureFolder +
                ", IsolatedTarget=True, SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Start New Mixamo Draw Sword Review")]
        public static void StartIspantNewDrawSwordReviewPlayback()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || reviewActive || AnimationMode.InAnimationMode())
                throw new InvalidOperationException("The draw-sword review requires idle Edit Mode AnimationMode.");
            var result = InspectApplied(RequireScene(true), false);
            reviewSnapshots = result.Target.Model.GetComponentsInChildren<Transform>(true).Select(item => new TransformSnapshot(item)).ToArray();
            reviewView = SceneView.lastActiveSceneView;
            if (reviewView != null)
            {
                reviewGizmos = reviewView.drawGizmos;
                reviewView.drawGizmos = false;
                Selection.activeGameObject = result.Target.Slot.gameObject;
                reviewView.FrameSelected();
            }
            AnimationMode.StartAnimationMode();
            reviewStart = EditorApplication.timeSinceStartup;
            reviewActive = true;
            EditorApplication.update += UpdateReview;
            Debug.Log("IspantNewDrawSwordReviewStarted Result=PASS, RequiredLoops=2, LiveSceneView=True, CaptureCreated=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Stop New Mixamo Draw Sword Review")]
        public static void StopIspantNewDrawSwordReviewPlayback()
        {
            if (!reviewActive || !AnimationMode.InAnimationMode())
                throw new InvalidOperationException("The draw-sword review is not active.");
            var clip = RequireAsset<AnimationClip>(ClipPath);
            var loops = Mathf.FloorToInt((float)((EditorApplication.timeSinceStartup - reviewStart) / clip.length));
            if (loops < RequiredLoops)
                throw new InvalidOperationException("The draw-sword review has not completed two loops. Completed=" + loops + ".");
            StopReview();
            var result = InspectApplied(RequireScene(true), true);
            Debug.Log(
                "IspantNewDrawSwordReviewStopped Result=PASS, CompletedLoops=" + loops +
                ", AppliedGripOutwardOffset=" + Num(result.AppliedGripOutwardOffset) +
                ", MinimumGripContactMargin=" + Num(result.MinimumGripContactMargin) +
                ", FinalBladeToUpAngle=" + Num(result.FinalBladeToUpAngle) +
                ", ImmediateResetPosition=" + Num(result.ImmediateResetPosition) +
                ", ImmediateResetAngle=" + Num(result.ImmediateResetAngle) +
                ", SceneRestored=True, CaptureCreated=False.");
        }

        private static void ConfigureImporter()
        {
            var importer = RequireImporter();
            var sourceBones = BoneDescriptions(RequireAsset<GameObject>(SourcePath).transform);
            var targetBones = BoneDescriptions(RequireAsset<GameObject>(ModelPath).transform);
            if (!sourceBones.SequenceEqual(targetBones, StringComparer.Ordinal))
                throw new InvalidOperationException("The supplied and target bone hierarchies differ.");
            var takes = (importer.defaultClipAnimations ?? Array.Empty<ModelImporterClipAnimation>()).Where(IsMixamo).ToArray();
            if (takes.Length != 1)
                throw new InvalidOperationException("Exactly one Mixamo take is required. Count=" + takes.Length + ".");
            var selected = takes[0];
            selected.name = ImportedClipName;
            selected.loopTime = false;
            selected.loopPose = false;
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.optimizeGameObjects = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importConstraints = false;
            importer.clipAnimations = new[] { selected };
            importer.SaveAndReimport();
        }

        private static AnimationClip CreateLoopClip(AnimationClip source)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = LoopClipName };
                AssetDatabase.CreateAsset(clip, ClipPath);
            }
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, binding, null);
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var sourceCurve = AnimationUtility.GetEditorCurve(source, binding);
                var copy = new AnimationCurve(sourceCurve.keys)
                {
                    preWrapMode = sourceCurve.preWrapMode,
                    postWrapMode = sourceCurve.postWrapMode
                };
                AnimationUtility.SetEditorCurve(clip, binding, copy);
            }
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
                AnimationUtility.SetObjectReferenceCurve(
                    clip,
                    binding,
                    AnimationUtility.GetObjectReferenceCurve(source, binding));
            clip.frameRate = source.frameRate;
            clip.wrapMode = WrapMode.Loop;
            AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static void ConfigureSwordFollower(Target target)
        {
            var body = RequireBody(target.Model);
            var foreArm = RequireDescendant(target.Model, "RightForeArm");
            var hand = RequireDescendant(target.Model, "RightHand");
            var swordRenderer = RequireSword(target.Model);
            var sword = swordRenderer.transform;
            var grip = CalculateGripCenter(swordRenderer.GetComponent<MeshFilter>().sharedMesh);
            var palm = WeightedPalmCenter(body, hand);
            var palmInHand = hand.InverseTransformPoint(palm);
            var handReach = CalculateRightHandReach(body, hand, palm);
            var handleReach = CalculateHandleReach(sword, swordRenderer.GetComponent<MeshFilter>().sharedMesh, grip);
            var contactAllowance = Mathf.Max(0f, handReach + handleReach - GripContactInset);
            var appliedOutwardOffset = Mathf.Min(RequestedGripOutwardOffset, contactAllowance);
            if (appliedOutwardOffset <= 0.01f)
                throw new InvalidOperationException("The hand and grip geometry cannot support an outward offset.");
            var animator = target.Model.GetComponent<Animator>() ??
                throw new InvalidOperationException("The draw-sword Animator is missing before follower configuration.");
            var followers = target.Slot.GetComponents<IspantRigidSwordFollower>();
            if (followers.Length > 1)
                throw new InvalidOperationException("Ispant_04_DrawSword has multiple rigid sword followers.");
            var follower = followers.SingleOrDefault() ?? target.Slot.gameObject.AddComponent<IspantRigidSwordFollower>();
            follower.Configure(
                foreArm,
                hand,
                sword,
                target.Model,
                animator,
                palmInHand,
                grip,
                SwordBladeLocalAxis,
                SwordRollLocalAxis,
                appliedOutwardOffset);
            EditorUtility.SetDirty(follower);
        }

        private static void ApplyLeftArmStableRestCurves(AnimationClip source, AnimationClip clip)
        {
            var targetModel = RequireAsset<GameObject>(ModelPath).transform;
            foreach (var name in LeftArmAnimatedBoneNames)
            {
                var targetBone = RequireDescendant(targetModel, name);
                var targetPath = AnimationUtility.CalculateTransformPath(targetBone, targetModel);
                var rotation = targetBone.localRotation;
                SetLinearCurve(clip, targetPath, "m_LocalRotation.x",
                    new[] { new Keyframe(0f, rotation.x), new Keyframe(source.length, rotation.x) });
                SetLinearCurve(clip, targetPath, "m_LocalRotation.y",
                    new[] { new Keyframe(0f, rotation.y), new Keyframe(source.length, rotation.y) });
                SetLinearCurve(clip, targetPath, "m_LocalRotation.z",
                    new[] { new Keyframe(0f, rotation.z), new Keyframe(source.length, rotation.z) });
                SetLinearCurve(clip, targetPath, "m_LocalRotation.w",
                    new[] { new Keyframe(0f, rotation.w), new Keyframe(source.length, rotation.w) });
            }
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
        }

        private static Quaternion EvaluateRotation(AnimationClip clip, string path, float time)
        {
            var rotation = new Quaternion(
                RequireCurve(clip, path, "m_LocalRotation.x").Evaluate(time),
                RequireCurve(clip, path, "m_LocalRotation.y").Evaluate(time),
                RequireCurve(clip, path, "m_LocalRotation.z").Evaluate(time),
                RequireCurve(clip, path, "m_LocalRotation.w").Evaluate(time));
            rotation.Normalize();
            return rotation;
        }

        private static AnimationCurve RequireCurve(AnimationClip clip, string path, string property)
        {
            var binding = AnimationUtility.GetCurveBindings(clip)
                .SingleOrDefault(item => item.path == path && item.propertyName == property);
            return AnimationUtility.GetEditorCurve(clip, binding) ??
                   throw new InvalidOperationException("Required rotation curve missing: " + path + "/" + property + ".");
        }

        private static void SetLinearCurve(
            AnimationClip clip,
            string path,
            string property,
            IReadOnlyCollection<Keyframe> keys)
        {
            var curve = new AnimationCurve(keys.ToArray());
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
            }
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property),
                curve);
        }

        private static Vector3 CalculateGripCenter(Mesh mesh)
        {
            var values = mesh.vertices.Where(item => item.x >= GripMinimumLocalX && item.x <= GripMaximumLocalX).ToArray();
            if (values.Length < 100)
                throw new InvalidOperationException("The current 10K sword grip region differs. Count=" + values.Length + ".");
            return values.Aggregate(Vector3.zero, (sum, value) => sum + value) / values.Length;
        }

        private static float CalculateHandleReach(Transform sword, Mesh mesh, Vector3 grip)
        {
            var values = mesh.vertices.Where(item => item.x >= GripMinimumLocalX && item.x <= GripMaximumLocalX).ToArray();
            return values.Max(item => sword.TransformVector(item - grip).magnitude);
        }

        private static float CalculateRightHandReach(
            SkinnedMeshRenderer body,
            Transform hand,
            Vector3 palm)
        {
            var values = RightHandWeightedWorldVertices(body, hand)
                .Select(item => Vector3.Distance(item, palm))
                .OrderBy(item => item)
                .ToArray();
            if (values.Length < 4)
                throw new InvalidOperationException("Too few RightHand-weighted vertices were found for grip reach.");
            return values[Mathf.FloorToInt((values.Length - 1) * 0.9f)];
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            if (!AssetDatabase.IsValidFolder(ControllerFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Art/Enemies/Ispant", "Controllers");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            var machine = controller.layers[0].stateMachine;
            foreach (var child in machine.states.ToArray()) machine.RemoveState(child.state);
            foreach (var child in machine.stateMachines.ToArray()) machine.RemoveStateMachine(child.stateMachine);
            foreach (var transition in machine.anyStateTransitions.ToArray()) machine.RemoveAnyStateTransition(transition);
            var state = machine.AddState(LoopClipName);
            state.motion = clip;
            state.writeDefaultValues = true;
            machine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void ConfigureAnimator(Transform model, AnimatorController controller)
        {
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length > 1 || (animators.Length == 1 && animators[0].transform != model))
                throw new InvalidOperationException("The draw-sword target has a conflicting Animator hierarchy.");
            var animator = animators.SingleOrDefault() ?? model.gameObject.AddComponent<Animator>();
            animator.avatar = null;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
        }

        private static Inspection InspectApplied(Scene scene, bool sample)
        {
            RequireHashes();
            var target = RequireTarget(scene);
            var clip = RequireAsset<AnimationClip>(ClipPath);
            var controller = RequireAsset<AnimatorController>(ControllerPath);
            var animator = target.Model.GetComponent<Animator>() ?? throw new InvalidOperationException("The draw-sword Animator is missing.");
            if (animator.runtimeAnimatorController != controller || animator.avatar != null || animator.applyRootMotion || !animator.enabled)
                throw new InvalidOperationException("The draw-sword Animator configuration differs.");
            if (controller.animationClips.Length != 1 || controller.animationClips[0] != clip ||
                !AnimationUtility.GetAnimationClipSettings(clip).loopTime)
                throw new InvalidOperationException("The draw-sword controller or loop clip differs.");
            var source = RequireImportedClip();
            RequireForwardOnlyClip(source, clip);
            var body = RequireBody(target.Model);
            var correctedBodyMesh = RequireAsset<Mesh>(CorrectedBodyMeshPath);
            if (body.sharedMesh != correctedBodyMesh || body.quality != SkinQuality.Bone4)
                throw new InvalidOperationException("The draw-sword corrected body mesh or four-bone skinning differs.");
            RequireCorrectedBodyMesh(body, correctedBodyMesh);
            var foreArm = RequireDescendant(target.Model, "RightForeArm");
            var hand = RequireDescendant(target.Model, "RightHand");
            var swordRenderer = RequireSword(target.Model);
            var sword = swordRenderer.transform;
            var mesh = sword.GetComponent<MeshFilter>().sharedMesh;
            if (swordRenderer.GetComponent<SkinnedMeshRenderer>() != null || mesh.blendShapeCount != 0 || mesh.boneWeights.Length != 0)
                throw new InvalidOperationException("The approved sword is no longer a rigid mesh.");
            var followers = target.Slot.GetComponents<IspantRigidSwordFollower>();
            if (followers.Length != 1 || !followers[0].Matches(foreArm, hand, sword, target.Model, animator))
                throw new InvalidOperationException("The real-time right-arm sword follower differs.");
            var follower = followers[0];
            var grip = CalculateGripCenter(mesh);
            var palmAtRest = WeightedPalmCenter(body, hand);
            var contactAllowance = CalculateRightHandReach(body, hand, palmAtRest) +
                                   CalculateHandleReach(sword, mesh, grip) - GripContactInset;
            var maxGripOffsetError = 0f;
            var minimumGripContactMargin = float.PositiveInfinity;
            var startBladeToUpAngle = 0f;
            var finalBladeToUpAngle = 0f;
            var maximumBladeAngularStep = 0f;
            var bladeChangingFrames = 0;
            var maxSwordMotion = 0f;
            var maxHandMotion = 0f;
            var seamPosition = 0f;
            var seamAngle = 0f;
            if (sample)
            {
                var snapshots = target.Model.GetComponentsInChildren<Transform>(true).Select(item => new TransformSnapshot(item)).ToArray();
                var firstSwordPosition = Vector3.zero;
                var firstSwordRotation = Quaternion.identity;
                var firstHandPosition = Vector3.zero;
                var previousBladeDirection = Vector3.zero;
                var frames = Mathf.RoundToInt(clip.length * clip.frameRate);
                try
                {
                    AnimationMode.StartAnimationMode();
                    for (var frame = 0; frame <= frames; frame++)
                    {
                        var normalizedTime = frame / (float)frames;
                        AnimationMode.SampleAnimationClip(target.Model.gameObject, clip, frame / clip.frameRate);
                        follower.ApplyFollow(normalizedTime);
                        var palm = WeightedPalmCenter(body, hand);
                        var gripWorld = sword.TransformPoint(grip);
                        var gripDistance = Vector3.Distance(gripWorld, palm);
                        maxGripOffsetError = Mathf.Max(
                            maxGripOffsetError,
                            Mathf.Abs(gripDistance - follower.GripOutwardOffset));
                        minimumGripContactMargin = Mathf.Min(
                            minimumGripContactMargin,
                            contactAllowance - gripDistance);
                        var bladeDirection = sword.TransformDirection(follower.SwordBladeLocalAxis).normalized;
                        var bladeToUpAngle = Vector3.Angle(bladeDirection, -target.Model.up);
                        if (frame == 0)
                        {
                            firstSwordPosition = sword.position;
                            firstSwordRotation = sword.rotation;
                            firstHandPosition = hand.position;
                            startBladeToUpAngle = bladeToUpAngle;
                        }
                        else
                        {
                            maxSwordMotion = Mathf.Max(maxSwordMotion, Vector3.Distance(firstSwordPosition, sword.position));
                            maxHandMotion = Mathf.Max(maxHandMotion, Vector3.Distance(firstHandPosition, hand.position));
                            var angularStep = Vector3.Angle(previousBladeDirection, bladeDirection);
                            maximumBladeAngularStep = Mathf.Max(maximumBladeAngularStep, angularStep);
                            if (angularStep > 0.01f) bladeChangingFrames++;
                        }
                        previousBladeDirection = bladeDirection;
                        if (frame == frames)
                        {
                            finalBladeToUpAngle = bladeToUpAngle;
                            seamPosition = Vector3.Distance(firstSwordPosition, sword.position);
                            seamAngle = Quaternion.Angle(firstSwordRotation, sword.rotation);
                        }
                    }
                }
                finally
                {
                    if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                    foreach (var snapshot in snapshots) snapshot.Restore();
                }
                if (body.quality != SkinQuality.Bone4)
                    throw new InvalidOperationException("The draw-sword body does not use the corrected four-bone skinning.");
                if (maxGripOffsetError > GripOffsetTolerance || minimumGripContactMargin < -TransformTolerance)
                    throw new InvalidOperationException(
                        "The adjusted grip offset loses hand contact. OffsetError=" + Num(maxGripOffsetError) +
                        ", ContactMargin=" + Num(minimumGripContactMargin) + ".");
                if (startBladeToUpAngle < 15f || finalBladeToUpAngle > 0.1f ||
                    maximumBladeAngularStep > 10f || bladeChangingFrames < frames / 2)
                    throw new InvalidOperationException(
                        "The blade does not turn naturally throughout the draw to finish upward. Start=" +
                        Num(startBladeToUpAngle) + ", Final=" + Num(finalBladeToUpAngle) +
                        ", MaxStep=" + Num(maximumBladeAngularStep) + ", ChangingFrames=" + bladeChangingFrames + ".");
                if (maxSwordMotion < 0.1f || maxHandMotion < 0.1f)
                    throw new InvalidOperationException("The sword or right hand does not move through the draw cycle.");
            }
            if (scene.isDirty)
                throw new InvalidOperationException("Inspection changed the scene dirty state.");
            return new Inspection(target, clip, follower.GripOutwardOffset, maxGripOffsetError,
                minimumGripContactMargin, startBladeToUpAngle, finalBladeToUpAngle,
                maximumBladeAngularStep, bladeChangingFrames, maxSwordMotion, maxHandMotion,
                seamPosition, seamAngle, mesh.vertexCount, TriangleCount(mesh));
        }

        private static void UpdateReview()
        {
            if (!reviewActive) return;
            try
            {
                var target = RequireTarget(RequireScene(true));
                var clip = RequireAsset<AnimationClip>(ClipPath);
                var elapsed = (float)((EditorApplication.timeSinceStartup - reviewStart) % clip.length);
                AnimationMode.SampleAnimationClip(target.Model.gameObject, clip, elapsed);
                var follower = target.Slot.GetComponent<IspantRigidSwordFollower>() ??
                    throw new InvalidOperationException("The real-time sword follower is missing during review.");
                follower.ApplyFollow(elapsed / clip.length);
                SceneView.RepaintAll();
            }
            catch (Exception exception)
            {
                StopReview();
                Debug.LogException(exception);
            }
        }

        private static void CapturePose(
            Transform model,
            IspantRigidSwordFollower follower,
            AnimationClip clip,
            Camera camera,
            float normalizedTime,
            bool side,
            bool closeGrip,
            bool userFront,
            string path)
        {
            AnimationMode.SampleAnimationClip(model.gameObject, clip, normalizedTime * clip.length);
            follower.ApplyFollow(normalizedTime);
            var renderers = model.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled && item.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException("The draw-sword visual review clone has no renderer.");
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);

            var focus = closeGrip ? RequireDescendant(model, "RightHand").position : bounds.center;
            var aspect = 1024f / 768f;
            var frameExtent = closeGrip
                ? Mathf.Max(bounds.size.y * 0.16f, 0.35f)
                : Mathf.Max(bounds.extents.y, bounds.extents.x / aspect, bounds.extents.z / aspect);
            var direction = userFront
                ? (model.forward + Vector3.up * 0.04f).normalized
                : side
                    ? (model.right + model.forward * 0.12f + Vector3.up * 0.08f).normalized
                    : (model.forward + model.right * 0.55f + Vector3.up * 0.12f).normalized;
            var distance = frameExtent / Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.18f;
            camera.transform.position = focus + direction * distance;
            camera.transform.LookAt(focus);
            RenderCapture(camera, path);
        }

        private static void CaptureLeftArmPose(
            Transform model,
            IspantRigidSwordFollower follower,
            AnimationClip clip,
            Camera camera,
            float normalizedTime,
            string path)
        {
            AnimationMode.SampleAnimationClip(model.gameObject, clip, normalizedTime * clip.length);
            follower.ApplyFollow(normalizedTime);
            var shoulder = RequireDescendant(model, "LeftShoulder");
            var hand = RequireDescendant(model, "LeftHand");
            var focus = Vector3.Lerp(shoulder.position, hand.position, 0.55f);
            var direction = (model.forward + model.right * 0.45f + Vector3.up * 0.08f).normalized;
            var distance = 0.72f / Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            camera.transform.position = focus + direction * distance;
            camera.transform.LookAt(focus);
            RenderCapture(camera, path);
        }

        private static void RenderCapture(Camera camera, string path)
        {
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var target = new RenderTexture(1024, 768, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(1024, 768, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, 1024f, 768f), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                UnityEngine.Object.DestroyImmediate(image);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void SetLayer(Transform root, int layer)
        {
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
                item.gameObject.layer = layer;
        }

        private static void StopReview()
        {
            EditorApplication.update -= UpdateReview;
            reviewActive = false;
            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
            if (reviewSnapshots != null)
            {
                foreach (var snapshot in reviewSnapshots) snapshot.Restore();
                reviewSnapshots = null;
            }
            if (reviewView != null)
            {
                reviewView.drawGizmos = reviewGizmos;
                reviewView = null;
            }
            SceneView.RepaintAll();
        }

        private static Seam InspectSourceSeam(Transform model, AnimationClip clip)
        {
            var hand = RequireDescendant(model, "RightHand");
            var hips = RequireDescendant(model, "Hips");
            var snapshots = model.GetComponentsInChildren<Transform>(true).Select(item => new TransformSnapshot(item)).ToArray();
            try
            {
                AnimationMode.StartAnimationMode();
                AnimationMode.SampleAnimationClip(model.gameObject, clip, 0f);
                var handPosition = model.InverseTransformPoint(hand.position);
                var handRotation = Quaternion.Inverse(model.rotation) * hand.rotation;
                var hipsPosition = hips.localPosition;
                var hipsRotation = hips.localRotation;
                AnimationMode.SampleAnimationClip(model.gameObject, clip, clip.length);
                return new Seam(
                    Vector3.Distance(handPosition, model.InverseTransformPoint(hand.position)),
                    Quaternion.Angle(handRotation, Quaternion.Inverse(model.rotation) * hand.rotation),
                    Vector3.Distance(hipsPosition, hips.localPosition), Quaternion.Angle(hipsRotation, hips.localRotation));
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                foreach (var snapshot in snapshots) snapshot.Restore();
            }
        }

        private static Vector3 WeightedPalmCenter(SkinnedMeshRenderer body, Transform hand)
        {
            var values = RightHandWeightedWorldVertices(body, hand);
            return values.Aggregate(Vector3.zero, (sum, value) => sum + value) / values.Length;
        }

        private static Vector3[] RightHandWeightedWorldVertices(
            SkinnedMeshRenderer body,
            Transform hand)
        {
            var mesh = body.sharedMesh;
            var bone = Array.IndexOf(body.bones, hand);
            if (mesh == null || bone < 0 || mesh.boneWeights.Length != mesh.vertexCount)
                throw new InvalidOperationException("The RightHand skinning data differs.");
            var values = Enumerable.Range(0, mesh.vertexCount)
                .Where(index => WeightForBone(mesh.boneWeights[index], bone) >= 0.1f)
                .Select(index =>
                {
                    var weight = mesh.boneWeights[index];
                    var value = Vector3.zero;
                    var vertex = mesh.vertices[index];
                    AddSkin(ref value, vertex, weight.boneIndex0, weight.weight0, body.bones, mesh.bindposes);
                    AddSkin(ref value, vertex, weight.boneIndex1, weight.weight1, body.bones, mesh.bindposes);
                    AddSkin(ref value, vertex, weight.boneIndex2, weight.weight2, body.bones, mesh.bindposes);
                    AddSkin(ref value, vertex, weight.boneIndex3, weight.weight3, body.bones, mesh.bindposes);
                    return value;
                }).ToArray();
            if (values.Length < 4)
                throw new InvalidOperationException("Too few RightHand-weighted vertices were found.");
            return values;
        }

        private static void AddSkin(ref Vector3 result, Vector3 vertex, int index, float weight, Transform[] bones, Matrix4x4[] bindposes)
        { if (weight > 0f) result += bones[index].TransformPoint(bindposes[index].MultiplyPoint3x4(vertex)) * weight; }

        private static string InspectLeftArmSkinning(Transform model, AnimationClip clip)
        {
            var body = RequireBody(model);
            var mesh = body.sharedMesh;
            var weights = mesh.boneWeights;
            var bindposes = mesh.bindposes;
            var bones = body.bones;
            var leftArmBoneIndices = new HashSet<int>(LeftArmBoneNames.Select(name =>
                Array.FindIndex(bones, bone => bone != null && bone.name == name)));
            if (leftArmBoneIndices.Contains(-1))
                throw new InvalidOperationException("The body renderer is missing a required left-arm bone binding.");

            var triangles = mesh.triangles;
            var relevantTriangleOffsets = Enumerable.Range(0, triangles.Length / 3)
                .Select(index => index * 3)
                .Where(offset => WeightForBones(weights[triangles[offset]], leftArmBoneIndices) >= 0.1f ||
                                 WeightForBones(weights[triangles[offset + 1]], leftArmBoneIndices) >= 0.1f ||
                                 WeightForBones(weights[triangles[offset + 2]], leftArmBoneIndices) >= 0.1f)
                .ToArray();
            var relevantVertices = relevantTriangleOffsets
                .SelectMany(offset => new[] { triangles[offset], triangles[offset + 1], triangles[offset + 2] })
                .Distinct()
                .ToArray();
            var oneBonePositions = new Vector3[mesh.vertexCount];
            var twoBonePositions = new Vector3[mesh.vertexCount];
            var fourBonePositions = new Vector3[mesh.vertexCount];
            var restOneBonePositions = new Vector3[mesh.vertexCount];
            var restTwoBonePositions = new Vector3[mesh.vertexCount];
            var restFourBonePositions = new Vector3[mesh.vertexCount];
            SkinVertices(mesh, weights, bindposes, bones, relevantVertices,
                oneBonePositions, twoBonePositions, fourBonePositions);
            Array.Copy(oneBonePositions, restOneBonePositions, oneBonePositions.Length);
            Array.Copy(twoBonePositions, restTwoBonePositions, twoBonePositions.Length);
            Array.Copy(fourBonePositions, restFourBonePositions, fourBonePositions.Length);

            var maximumOneBoneRatio = 0f;
            var maximumTwoBoneRatio = 0f;
            var maximumFourBoneRatio = 0f;
            var maximumOneBoneLength = 0f;
            var maximumTwoBoneLength = 0f;
            var maximumFourBoneLength = 0f;
            var worstFrame = 0;
            var worstTriangle = -1;
            var worstEdge = string.Empty;
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            try
            {
                AnimationMode.StartAnimationMode();
                var frames = Mathf.RoundToInt(clip.length * clip.frameRate);
                for (var frame = 0; frame <= frames; frame++)
                {
                    AnimationMode.SampleAnimationClip(model.gameObject, clip, frame / clip.frameRate);
                    SkinVertices(mesh, weights, bindposes, bones, relevantVertices,
                        oneBonePositions, twoBonePositions, fourBonePositions);
                    foreach (var offset in relevantTriangleOffsets)
                    {
                        var indices = new[] { triangles[offset], triangles[offset + 1], triangles[offset + 2] };
                        for (var edge = 0; edge < 3; edge++)
                        {
                            var first = indices[edge];
                            var second = indices[(edge + 1) % 3];
                            var oneLength = Vector3.Distance(oneBonePositions[first], oneBonePositions[second]);
                            var twoLength = Vector3.Distance(twoBonePositions[first], twoBonePositions[second]);
                            var fourLength = Vector3.Distance(fourBonePositions[first], fourBonePositions[second]);
                            var oneRatio = oneLength / Mathf.Max(0.000001f,
                                Vector3.Distance(restOneBonePositions[first], restOneBonePositions[second]));
                            var twoRatio = twoLength / Mathf.Max(0.000001f,
                                Vector3.Distance(restTwoBonePositions[first], restTwoBonePositions[second]));
                            var fourRatio = fourLength / Mathf.Max(0.000001f,
                                Vector3.Distance(restFourBonePositions[first], restFourBonePositions[second]));
                            maximumTwoBoneRatio = Mathf.Max(maximumTwoBoneRatio, twoRatio);
                            maximumTwoBoneLength = Mathf.Max(maximumTwoBoneLength, twoLength);
                            maximumFourBoneRatio = Mathf.Max(maximumFourBoneRatio, fourRatio);
                            maximumFourBoneLength = Mathf.Max(maximumFourBoneLength, fourLength);
                            if (oneRatio <= maximumOneBoneRatio)
                                continue;
                            maximumOneBoneRatio = oneRatio;
                            maximumOneBoneLength = oneLength;
                            worstFrame = frame;
                            worstTriangle = offset / 3;
                            worstEdge = first + "-" + second + "[" +
                                        DominantBoneName(weights[first], bones) + "->" +
                                        DominantBoneName(weights[second], bones) + "]";
                        }
                    }
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                foreach (var snapshot in snapshots) snapshot.Restore();
            }

            return "Vertices=" + relevantVertices.Length +
                   ", Triangles=" + relevantTriangleOffsets.Length +
                   ", Bone1MaxEdgeRatio=" + Num(maximumOneBoneRatio) +
                   ", Bone1MaxEdgeLength=" + Num(maximumOneBoneLength) +
                   ", Bone2MaxEdgeRatio=" + Num(maximumTwoBoneRatio) +
                   ", Bone2MaxEdgeLength=" + Num(maximumTwoBoneLength) +
                   ", Bone4MaxEdgeRatio=" + Num(maximumFourBoneRatio) +
                   ", Bone4MaxEdgeLength=" + Num(maximumFourBoneLength) +
                   ", WorstFrame=" + worstFrame +
                   ", WorstTriangle=" + worstTriangle +
                   ", WorstEdge=" + worstEdge;
        }

        private static string InspectLeftArmLegCoupling(Transform model, AnimationClip clip)
        {
            var body = RequireBody(model);
            var mesh = body.sharedMesh;
            var bones = body.bones;
            var weights = mesh.boneWeights;
            var armBones = new HashSet<int>(LeftArmBoneNames.Select(name =>
                Array.FindIndex(bones, bone => bone != null && bone.name == name)));
            var legBones = new HashSet<int>(LeftLegBoneNames.Select(name =>
                Array.FindIndex(bones, bone => bone != null && bone.name == name)));
            if (armBones.Contains(-1) || legBones.Contains(-1))
                throw new InvalidOperationException("The body renderer is missing a left-arm or left-leg bone binding.");

            var armWeights = weights.Select(weight => WeightForBones(weight, armBones)).ToArray();
            var legWeights = weights.Select(weight => WeightForBones(weight, legBones)).ToArray();
            var meshComponents = MeshConnectedComponents(mesh);
            var separatedWeights = BuildArmLegComponentSeparatedWeights(
                mesh, bones, meshComponents, armBones, legBones,
                out var separatedArmComponents,
                out var separatedBodyComponents,
                out var separatedChangedVertices);
            var separatedArmWeights = separatedWeights.Select(weight => WeightForBones(weight, armBones)).ToArray();
            var separatedLegWeights = separatedWeights.Select(weight => WeightForBones(weight, legBones)).ToArray();
            var mixedVertices = Enumerable.Range(0, mesh.vertexCount)
                .Where(index => armWeights[index] > 0.000001f && legWeights[index] > 0.000001f)
                .ToArray();
            var strongMixedVertices = mixedVertices
                .Where(index => armWeights[index] >= 0.1f && legWeights[index] >= 0.1f)
                .ToArray();
            var triangles = mesh.triangles;
            var coupledTriangleOffsets = Enumerable.Range(0, triangles.Length / 3)
                .Select(index => index * 3)
                .Where(offset =>
                {
                    var first = triangles[offset];
                    var second = triangles[offset + 1];
                    var third = triangles[offset + 2];
                    return Mathf.Max(armWeights[first], armWeights[second], armWeights[third]) >= 0.1f &&
                           Mathf.Max(legWeights[first], legWeights[second], legWeights[third]) >= 0.1f;
                })
                .ToArray();
            var separatedMixedVertices = Enumerable.Range(0, mesh.vertexCount)
                .Count(index => separatedArmWeights[index] > 0.000001f && separatedLegWeights[index] > 0.000001f);
            var separatedCoupledTriangles = Enumerable.Range(0, triangles.Length / 3)
                .Select(index => index * 3)
                .Count(offset =>
                {
                    var first = triangles[offset];
                    var second = triangles[offset + 1];
                    var third = triangles[offset + 2];
                    return Mathf.Max(separatedArmWeights[first], separatedArmWeights[second], separatedArmWeights[third]) >= 0.1f &&
                           Mathf.Max(separatedLegWeights[first], separatedLegWeights[second], separatedLegWeights[third]) >= 0.1f;
                });
            var strongCoupledTriangleOffsets = coupledTriangleOffsets
                .Where(offset =>
                {
                    var first = triangles[offset];
                    var second = triangles[offset + 1];
                    var third = triangles[offset + 2];
                    return Mathf.Max(armWeights[first], armWeights[second], armWeights[third]) >= 0.5f &&
                           Mathf.Max(legWeights[first], legWeights[second], legWeights[third]) >= 0.5f;
                })
                .ToArray();
            var relevantVertices = coupledTriangleOffsets
                .SelectMany(offset => new[] { triangles[offset], triangles[offset + 1], triangles[offset + 2] })
                .Distinct()
                .ToArray();
            var skinnedPositions = new Vector3[mesh.vertexCount];
            var separatedPositions = new Vector3[mesh.vertexCount];
            var maximumEdgeRatio = 0f;
            var maximumEdgeLength = 0f;
            var separatedMaximumEdgeRatio = 0f;
            var separatedMaximumEdgeLength = 0f;
            var worstFrame = -1;
            var worstTriangle = -1;
            var worstFirst = -1;
            var worstSecond = -1;
            var vertices = mesh.vertices;
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            try
            {
                AnimationMode.StartAnimationMode();
                var frames = Mathf.RoundToInt(clip.length * clip.frameRate);
                for (var frame = 0; frame <= frames; frame++)
                {
                    AnimationMode.SampleAnimationClip(model.gameObject, clip, frame / clip.frameRate);
                    SkinFourBoneVertices(mesh, weights, mesh.bindposes, bones, relevantVertices, skinnedPositions);
                    SkinFourBoneVertices(mesh, separatedWeights, mesh.bindposes, bones, relevantVertices, separatedPositions);
                    foreach (var offset in coupledTriangleOffsets)
                    {
                        var indices = new[] { triangles[offset], triangles[offset + 1], triangles[offset + 2] };
                        for (var edge = 0; edge < 3; edge++)
                        {
                            var first = indices[edge];
                            var second = indices[(edge + 1) % 3];
                            var restLength = Vector3.Distance(vertices[first], vertices[second]);
                            var currentLength = Vector3.Distance(skinnedPositions[first], skinnedPositions[second]);
                            var separatedLength = Vector3.Distance(separatedPositions[first], separatedPositions[second]);
                            var ratio = currentLength / Mathf.Max(0.000001f, restLength);
                            var separatedRatio = separatedLength / Mathf.Max(0.000001f, restLength);
                            separatedMaximumEdgeRatio = Mathf.Max(separatedMaximumEdgeRatio, separatedRatio);
                            separatedMaximumEdgeLength = Mathf.Max(separatedMaximumEdgeLength, separatedLength);
                            if (ratio <= maximumEdgeRatio)
                                continue;
                            maximumEdgeRatio = ratio;
                            maximumEdgeLength = currentLength;
                            worstFrame = frame;
                            worstTriangle = offset / 3;
                            worstFirst = first;
                            worstSecond = second;
                        }
                    }
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                foreach (var snapshot in snapshots) snapshot.Restore();
            }

            var topMixed = string.Join("|", mixedVertices
                .OrderByDescending(index => Mathf.Min(armWeights[index], legWeights[index]))
                .Take(12)
                .Select(index => index + "[Position=" + Vec(vertices[index]) +
                                 ",Arm=" + Num(armWeights[index]) +
                                 ",Leg=" + Num(legWeights[index]) +
                                 ",Weights=" + DescribeWeights(weights[index], bones) + "]"));
            var worstDescription = worstFirst >= 0
                ? worstFirst + "[" + DescribeWeights(weights[worstFirst], bones) + "]-" +
                  worstSecond + "[" + DescribeWeights(weights[worstSecond], bones) + "]"
                : "<none>";
            var mixedSet = new HashSet<int>(mixedVertices);
            var mixedComponents = meshComponents
                .Where(component => component.Any(mixedSet.Contains))
                .Select(component =>
                {
                    var bounds = new Bounds(vertices[component.First()], Vector3.zero);
                    var boneTotals = new float[bones.Length];
                    foreach (var vertex in component)
                    {
                        bounds.Encapsulate(vertices[vertex]);
                        var weight = weights[vertex];
                        boneTotals[weight.boneIndex0] += weight.weight0;
                        boneTotals[weight.boneIndex1] += weight.weight1;
                        boneTotals[weight.boneIndex2] += weight.weight2;
                        boneTotals[weight.boneIndex3] += weight.weight3;
                    }
                    var dominant = Enumerable.Range(0, bones.Length)
                        .Where(index => boneTotals[index] > 0f)
                        .OrderByDescending(index => boneTotals[index])
                        .Take(6)
                        .Select(index => bones[index].name + ":" + Num(boneTotals[index] / component.Count));
                    return new
                    {
                        Seed = component.Min(),
                        Count = component.Count,
                        Mixed = component.Count(mixedSet.Contains),
                        Arm = component.Average(index => armWeights[index]),
                        Leg = component.Average(index => legWeights[index]),
                        PureArm = component.Count(index => armWeights[index] >= 0.5f && legWeights[index] < 0.01f),
                        PureLeg = component.Count(index => legWeights[index] >= 0.5f && armWeights[index] < 0.01f),
                        Bounds = bounds,
                        Dominant = string.Join("+", dominant)
                    };
                })
                .OrderByDescending(item => item.Mixed)
                .ThenBy(item => item.Seed)
                .ToArray();
            var mixedComponentDescription = string.Join("|", mixedComponents.Take(24).Select(item =>
                "Seed=" + item.Seed +
                ",Vertices=" + item.Count +
                ",Mixed=" + item.Mixed +
                ",Arm=" + Num(item.Arm) +
                ",Leg=" + Num(item.Leg) +
                ",PureArm=" + item.PureArm +
                ",PureLeg=" + item.PureLeg +
                ",Center=" + Vec(item.Bounds.center) +
                ",Size=" + Vec(item.Bounds.size) +
                ",Weights=" + item.Dominant));
            return "MixedVertices=" + mixedVertices.Length +
                   ", StrongMixedVertices=" + strongMixedVertices.Length +
                   ", CoupledTriangles=" + coupledTriangleOffsets.Length +
                   ", StrongCoupledTriangles=" + strongCoupledTriangleOffsets.Length +
                   ", MaxEdgeRatio=" + Num(maximumEdgeRatio) +
                   ", MaxEdgeLength=" + Num(maximumEdgeLength) +
                   ", WorstFrame=" + worstFrame +
                   ", WorstTriangle=" + worstTriangle +
                   ", WorstEdge=" + worstDescription +
                   ", SeparatedArmComponents=" + separatedArmComponents +
                   ", SeparatedBodyComponents=" + separatedBodyComponents +
                   ", SeparatedChangedVertices=" + separatedChangedVertices +
                   ", SeparatedMixedVertices=" + separatedMixedVertices +
                   ", SeparatedCoupledTriangles=" + separatedCoupledTriangles +
                   ", SeparatedMaxEdgeRatio=" + Num(separatedMaximumEdgeRatio) +
                   ", SeparatedMaxEdgeLength=" + Num(separatedMaximumEdgeLength) +
                   ", MixedComponentCount=" + mixedComponents.Length +
                   ", MixedComponents={" + mixedComponentDescription + "}" +
                   ", TopMixed={" + topMixed + "}";
        }

        private static IReadOnlyList<HashSet<int>> MeshConnectedComponents(Mesh mesh)
        {
            var adjacency = Enumerable.Range(0, mesh.vertexCount).Select(_ => new List<int>()).ToArray();
            var triangles = mesh.triangles;
            for (var offset = 0; offset < triangles.Length; offset += 3)
            {
                var first = triangles[offset];
                var second = triangles[offset + 1];
                var third = triangles[offset + 2];
                adjacency[first].Add(second); adjacency[first].Add(third);
                adjacency[second].Add(first); adjacency[second].Add(third);
                adjacency[third].Add(first); adjacency[third].Add(second);
            }
            var visited = new HashSet<int>();
            var result = new List<HashSet<int>>();
            for (var seed = 0; seed < mesh.vertexCount; seed++)
            {
                if (!visited.Add(seed))
                    continue;
                var component = new HashSet<int> { seed };
                var pending = new Queue<int>();
                pending.Enqueue(seed);
                while (pending.Count > 0)
                {
                    var current = pending.Dequeue();
                    foreach (var neighbor in adjacency[current])
                    {
                        if (!visited.Add(neighbor))
                            continue;
                        component.Add(neighbor);
                        pending.Enqueue(neighbor);
                    }
                }
                result.Add(component);
            }
            return result;
        }

        private static string InspectSeparatedLeftArmComponents(Transform model)
        {
            var body = RequireBody(model);
            var directMesh = RequireAsset<GameObject>(ModelPath)
                .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1")
                .sharedMesh;
            var bones = body.bones;
            var armBones = new HashSet<int>(LeftArmBoneNames.Select(name =>
                Array.FindIndex(bones, bone => bone != null && bone.name == name)));
            var legBones = new HashSet<int>(LeftLegBoneNames.Select(name =>
                Array.FindIndex(bones, bone => bone != null && bone.name == name)));
            var sourceWeights = directMesh.boneWeights;
            var vertices = directMesh.vertices;
            var triangles = directMesh.triangles;
            var components = MeshConnectedComponents(directMesh);
            var descriptions = components
                .Where(component => component.Any(index =>
                    WeightForBones(sourceWeights[index], armBones) > 0.000001f &&
                    WeightForBones(sourceWeights[index], legBones) > 0.000001f))
                .Where(component => component.Average(index => WeightForBones(sourceWeights[index], armBones)) >= 0.5f)
                .Select(component =>
                {
                    var bounds = new Bounds(vertices[component.First()], Vector3.zero);
                    var boneTotals = new float[bones.Length];
                    foreach (var vertex in component)
                    {
                        bounds.Encapsulate(vertices[vertex]);
                        var weight = sourceWeights[vertex];
                        boneTotals[weight.boneIndex0] += weight.weight0;
                        boneTotals[weight.boneIndex1] += weight.weight1;
                        boneTotals[weight.boneIndex2] += weight.weight2;
                        boneTotals[weight.boneIndex3] += weight.weight3;
                    }
                    var triangleCount = Enumerable.Range(0, triangles.Length / 3).Count(index =>
                        component.Contains(triangles[index * 3]));
                    var dominant = Enumerable.Range(0, bones.Length)
                        .Where(index => boneTotals[index] > 0f)
                        .OrderByDescending(index => boneTotals[index])
                        .Take(6)
                        .Select(index => bones[index].name + ":" + Num(boneTotals[index] / component.Count));
                    return new
                    {
                        Seed = component.Min(),
                        Vertices = component.Count,
                        Triangles = triangleCount,
                        Arm = component.Average(index => WeightForBones(sourceWeights[index], armBones)),
                        Leg = component.Average(index => WeightForBones(sourceWeights[index], legBones)),
                        Bounds = bounds,
                        Dominant = string.Join("+", dominant)
                    };
                })
                .OrderBy(item => item.Vertices)
                .ThenBy(item => item.Seed)
                .ToArray();
            return "Count=" + descriptions.Length + "{" + string.Join("|", descriptions.Select(item =>
                "Seed=" + item.Seed +
                ",Vertices=" + item.Vertices +
                ",Triangles=" + item.Triangles +
                ",Arm=" + Num(item.Arm) +
                ",Leg=" + Num(item.Leg) +
                ",Center=" + Vec(item.Bounds.center) +
                ",Size=" + Vec(item.Bounds.size) +
                ",Weights=" + item.Dominant)) + "}";
        }

        private static BoneWeight[] BuildArmLegComponentSeparatedWeights(
            Mesh mesh,
            Transform[] bones,
            IReadOnlyList<HashSet<int>> components,
            HashSet<int> armBones,
            HashSet<int> legBones,
            out int armComponentCount,
            out int bodyComponentCount,
            out int changedVertexCount)
        {
            var sourceWeights = mesh.boneWeights;
            var result = sourceWeights.ToArray();
            var allNonArmBones = new HashSet<int>(Enumerable.Range(0, bones.Length).Where(index => !armBones.Contains(index)));
            armComponentCount = 0;
            bodyComponentCount = 0;
            changedVertexCount = 0;
            foreach (var component in components)
            {
                if (!component.Any(index =>
                        WeightForBones(sourceWeights[index], armBones) > 0.000001f &&
                        WeightForBones(sourceWeights[index], legBones) > 0.000001f))
                    continue;
                var averageArmWeight = component.Average(index => WeightForBones(sourceWeights[index], armBones));
                var isArmComponent = averageArmWeight >= 0.5f;
                var allowedBones = isArmComponent ? armBones : allNonArmBones;
                if (isArmComponent) armComponentCount++; else bodyComponentCount++;
                foreach (var vertex in component)
                {
                    var separated = RestrictBoneWeight(sourceWeights[vertex], allowedBones, component.Min(), vertex, bones);
                    if (!BoneWeightMatches(separated, sourceWeights[vertex]))
                        changedVertexCount++;
                    result[vertex] = separated;
                }
            }
            return result;
        }

        private static BoneWeight RestrictBoneWeight(
            BoneWeight source,
            HashSet<int> allowedBones,
            int componentSeed,
            int vertex,
            Transform[] bones)
        {
            var influences = new[]
            {
                new KeyValuePair<int, float>(source.boneIndex0, source.weight0),
                new KeyValuePair<int, float>(source.boneIndex1, source.weight1),
                new KeyValuePair<int, float>(source.boneIndex2, source.weight2),
                new KeyValuePair<int, float>(source.boneIndex3, source.weight3)
            };
            var kept = influences
                .Where(item => item.Value > 0f && allowedBones.Contains(item.Key))
                .OrderByDescending(item => item.Value)
                .ToArray();
            var total = kept.Sum(item => item.Value);
            if (total <= 0.000001f)
                throw new InvalidOperationException(
                    "Component separation left vertex " + vertex + " in component " + componentSeed +
                    " without an allowed influence. Weights=" + DescribeWeights(source, bones) + ".");
            var normalized = kept
                .Select(item => new KeyValuePair<int, float>(item.Key, item.Value / total))
                .Concat(Enumerable.Repeat(new KeyValuePair<int, float>(0, 0f), 4))
                .Take(4)
                .ToArray();
            return new BoneWeight
            {
                boneIndex0 = normalized[0].Key,
                weight0 = normalized[0].Value,
                boneIndex1 = normalized[1].Key,
                weight1 = normalized[1].Value,
                boneIndex2 = normalized[2].Key,
                weight2 = normalized[2].Value,
                boneIndex3 = normalized[3].Key,
                weight3 = normalized[3].Value
            };
        }

        private static string InspectBodyBinding(SkinnedMeshRenderer body)
        {
            var assetBody = RequireAsset<GameObject>(ModelPath)
                .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var sceneBones = body.bones.Select(item => item != null ? item.name : "<null>").ToArray();
            var assetBones = assetBody.bones.Select(item => item != null ? item.name : "<null>").ToArray();
            return "CorrectedMesh=" + (body.sharedMesh == RequireAsset<Mesh>(CorrectedBodyMeshPath)) +
                   ", DirectMeshReplaced=" + (body.sharedMesh != assetBody.sharedMesh) +
                   ", BoneCount=" + sceneBones.Length +
                   ", BindposeCount=" + body.sharedMesh.bindposes.Length +
                   ", BoneOrderSame=" + sceneBones.SequenceEqual(assetBones, StringComparer.Ordinal) +
                   ", RootBone=" + (body.rootBone != null ? body.rootBone.name : "<null>") +
                   ", AssetRootBone=" + (assetBody.rootBone != null ? assetBody.rootBone.name : "<null>");
        }

        private static void RequireCorrectedBodyMesh(SkinnedMeshRenderer body, Mesh correctedMesh)
        {
            var directBody = RequireAsset<GameObject>(ModelPath)
                .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var directMesh = directBody.sharedMesh;
            if (!body.bones.Select(item => item.name)
                    .SequenceEqual(directBody.bones.Select(item => item.name), StringComparer.Ordinal))
                throw new InvalidOperationException("The corrected and direct body bone orders differ.");
            if (correctedMesh.vertexCount != directMesh.vertexCount ||
                correctedMesh.subMeshCount != directMesh.subMeshCount ||
                !correctedMesh.vertices.SequenceEqual(directMesh.vertices) ||
                !correctedMesh.normals.SequenceEqual(directMesh.normals) ||
                !correctedMesh.tangents.SequenceEqual(directMesh.tangents) ||
                !correctedMesh.uv.SequenceEqual(directMesh.uv) ||
                correctedMesh.bindposes.Length != directMesh.bindposes.Length ||
                Enumerable.Range(0, directMesh.bindposes.Length)
                    .Any(index => MatrixDelta(correctedMesh.bindposes[index], directMesh.bindposes[index]) > 0.000001f))
                throw new InvalidOperationException("The corrected body mesh no longer preserves the direct body vertex data.");
            var problemVertices = BuildProblemVertexSet(directMesh);
            for (var subMesh = 0; subMesh < directMesh.subMeshCount; subMesh++)
            {
                var directTriangles = directMesh.GetTriangles(subMesh);
                var expectedTriangles = Enumerable.Range(0, directTriangles.Length / 3)
                    .SelectMany(triangle => new[]
                    {
                        directTriangles[triangle * 3],
                        directTriangles[triangle * 3 + 1],
                        directTriangles[triangle * 3 + 2]
                    })
                    .Where((_, index) => !problemVertices.Contains(
                        directTriangles[index / 3 * 3]))
                    .ToArray();
                if (!correctedMesh.GetTriangles(subMesh).SequenceEqual(expectedTriangles))
                    throw new InvalidOperationException("The corrected body mesh triangle removal differs in submesh " + subMesh + ".");
            }
            var armBones = new HashSet<int>(LeftArmBoneNames.Select(name =>
                Array.FindIndex(body.bones, bone => bone != null && bone.name == name)));
            var legBones = new HashSet<int>(LeftLegBoneNames.Select(name =>
                Array.FindIndex(body.bones, bone => bone != null && bone.name == name)));
            var expectedWeights = BuildArmLegComponentSeparatedWeights(
                directMesh, body.bones, MeshConnectedComponents(directMesh), armBones, legBones,
                out var separatedArmComponents,
                out var separatedBodyComponents,
                out var separatedChangedVertices);
            if (separatedArmComponents != 26 || separatedBodyComponents != 59 || separatedChangedVertices != 804)
                throw new InvalidOperationException(
                    "The approved arm-leg component separation signature differs: " +
                    separatedArmComponents + "/" + separatedBodyComponents + "/" + separatedChangedVertices + ".");
            var correctedWeights = correctedMesh.boneWeights;
            if (correctedWeights.Length != expectedWeights.Length ||
                Enumerable.Range(0, expectedWeights.Length)
                    .Any(index => !BoneWeightMatches(correctedWeights[index], expectedWeights[index])))
                throw new InvalidOperationException("The corrected body mesh differs from the approved arm-leg component separation weights.");
        }

        private static int RemoveProblemComponentTriangles(Mesh mesh)
        {
            var problemVertices = BuildProblemVertexSet(mesh);
            var removedTriangles = 0;
            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                var triangles = mesh.GetTriangles(subMesh);
                var kept = new List<int>(triangles.Length);
                for (var offset = 0; offset < triangles.Length; offset += 3)
                {
                    var firstRemoved = problemVertices.Contains(triangles[offset]);
                    var secondRemoved = problemVertices.Contains(triangles[offset + 1]);
                    var thirdRemoved = problemVertices.Contains(triangles[offset + 2]);
                    if (firstRemoved != secondRemoved || firstRemoved != thirdRemoved)
                        throw new InvalidOperationException("A problem component is not topologically isolated.");
                    if (firstRemoved)
                    {
                        removedTriangles++;
                        continue;
                    }
                    kept.Add(triangles[offset]);
                    kept.Add(triangles[offset + 1]);
                    kept.Add(triangles[offset + 2]);
                }
                mesh.SetTriangles(kept, subMesh, false);
            }
            return removedTriangles;
        }

        private static HashSet<int> BuildProblemVertexSet(Mesh mesh)
        {
            var result = new HashSet<int>();
            foreach (var item in new[] { (249, 3), (8177, 16), (8322, 34), (8356, 17), (8373, 24),
                          (8397, 7), (8404, 5), (8460, 4), (8729, 6) })
            {
                var component = ConnectedComponentVertices(mesh, item.Item1);
                if (component.Count != item.Item2 || component.Min() != item.Item1)
                    throw new InvalidOperationException("A removable problem component topology differs at seed " + item.Item1 + ".");
                result.UnionWith(component);
            }
            if (result.Count != 116)
                throw new InvalidOperationException("The removable problem vertex count differs.");
            return result;
        }

        private static BoneWeight[] BuildMappedComponentWeights(Mesh mesh, Transform[] bones)
        {
            var weights = mesh.boneWeights.ToArray();
            ApplyRigidComponentBinding(mesh, weights, bones, 8177, 16, "LeftForeArm");
            ApplyRigidComponentBinding(mesh, weights, bones, 8322, 34, "LeftForeArm");
            ApplyRigidComponentBinding(mesh, weights, bones, 8356, 17, "LeftLeg");
            ApplyRigidComponentBinding(mesh, weights, bones, 8373, 24, "LeftHand");
            ApplyRigidComponentBinding(mesh, weights, bones, 8397, 7, "LeftLeg");
            ApplyRigidComponentBinding(mesh, weights, bones, 8404, 5, "LeftLeg");
            ApplyRigidComponentBinding(mesh, weights, bones, 8460, 4, "LeftLeg");
            ApplyRigidComponentBinding(mesh, weights, bones, 8729, 6, "LeftHand");
            return weights;
        }

        private static string InspectDirectMeshRepairCandidates(Transform model, AnimationClip clip)
        {
            var sceneBody = RequireBody(model);
            var directBody = RequireAsset<GameObject>(ModelPath)
                .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            if (!sceneBody.bones.Select(item => item.name)
                    .SequenceEqual(directBody.bones.Select(item => item.name), StringComparer.Ordinal))
                throw new InvalidOperationException("The direct and scene body bone orders differ.");
            var mesh = directBody.sharedMesh;
            var sourceMesh = RequireAsset<GameObject>(SourcePath)
                .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1")
                .sharedMesh;
            var originalWeights = mesh.boneWeights;
            var leftArmIndices = new HashSet<int>(LeftArmBoneNames.Select(name =>
                Array.FindIndex(sceneBody.bones, bone => bone != null && bone.name == name)));
            var relevantVertices = Enumerable.Range(0, mesh.vertexCount)
                .Where(index => WeightForBones(originalWeights[index], leftArmIndices) >= 0.5f)
                .ToArray();
            var legComponentWeights = originalWeights.ToArray();
            var legComponent = ConnectedComponentVertices(mesh, 8363);
            if (legComponent.Count != 17)
                throw new InvalidOperationException("The known left-leg armor component topology differs.");
            var leftLegIndex = Array.FindIndex(sceneBody.bones,
                bone => bone != null && bone.name == "LeftLeg");
            if (leftLegIndex < 0)
                throw new InvalidOperationException("The target body is missing LeftLeg.");
            foreach (var vertex in legComponent)
                legComponentWeights[vertex] = new BoneWeight { boneIndex0 = leftLegIndex, weight0 = 1f };
            var mappedComponentWeights = BuildMappedComponentWeights(mesh, sceneBody.bones);
            var armAssemblyWeights = originalWeights.ToArray();
            ApplyRigidComponentBinding(mesh, armAssemblyWeights, sceneBody.bones, 8177, 16, "LeftForeArm");
            ApplyRigidComponentBinding(mesh, armAssemblyWeights, sceneBody.bones, 8322, 34, "LeftForeArm");
            ApplyRigidComponentBinding(mesh, armAssemblyWeights, sceneBody.bones, 8356, 17, "LeftHand");
            ApplyRigidComponentBinding(mesh, armAssemblyWeights, sceneBody.bones, 8373, 24, "LeftHand");
            ApplyRigidComponentBinding(mesh, armAssemblyWeights, sceneBody.bones, 8397, 7, "LeftHand");
            ApplyRigidComponentBinding(mesh, armAssemblyWeights, sceneBody.bones, 8404, 5, "LeftHand");
            ApplyRigidComponentBinding(mesh, armAssemblyWeights, sceneBody.bones, 8460, 4, "LeftHand");
            ApplyRigidComponentBinding(mesh, armAssemblyWeights, sceneBody.bones, 8729, 6, "LeftHand");
            var allForeArmWeights = originalWeights.ToArray();
            foreach (var component in new[] { (8177, 16), (8322, 34), (8356, 17), (8373, 24),
                         (8397, 7), (8404, 5), (8460, 4), (8729, 6) })
                ApplyRigidComponentBinding(mesh, allForeArmWeights, sceneBody.bones,
                    component.Item1, component.Item2, "LeftForeArm");
            var sourceTransferredComponents = BuildSourceTransferredComponentWeights(mesh, sourceMesh);
            var candidates = new[]
            {
                originalWeights,
                RestrictWeightsToBones(originalWeights, leftArmIndices, 0.25f),
                RestrictWeightsToBones(originalWeights, leftArmIndices, 0.5f),
                RestrictWeightsToBones(originalWeights, leftArmIndices, 0.75f),
                legComponentWeights,
                mappedComponentWeights,
                armAssemblyWeights,
                allForeArmWeights,
                sourceTransferredComponents
            };
            var labels = new[]
                { "Original", "Restrict025", "Restrict050", "Restrict075", "LegComponentToLeftLeg", "MappedComponents", "ArmAssembly", "AllForeArm", "SourceTransferredComponents" };
            var candidateRelevantVertices = candidates.Select(candidate =>
                    Enumerable.Range(0, mesh.vertexCount)
                        .Where(index => WeightForBones(candidate[index], leftArmIndices) >= 0.5f)
                        .ToArray())
                .ToArray();
            var maximumDistances = new float[candidates.Length];
            var worstFrames = new int[candidates.Length];
            var worstVertices = new int[candidates.Length];
            var positions = candidates.Select(_ => new Vector3[mesh.vertexCount]).ToArray();
            var shoulder = RequireDescendant(model, "LeftShoulder");
            var arm = RequireDescendant(model, "LeftArm");
            var foreArm = RequireDescendant(model, "LeftForeArm");
            var hand = RequireDescendant(model, "LeftHand");
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            try
            {
                AnimationMode.StartAnimationMode();
                var frames = Mathf.RoundToInt(clip.length * clip.frameRate);
                for (var frame = 0; frame <= frames; frame++)
                {
                    AnimationMode.SampleAnimationClip(model.gameObject, clip, frame / clip.frameRate);
                    for (var candidateIndex = 0; candidateIndex < candidates.Length; candidateIndex++)
                    {
                        SkinFourBoneVertices(mesh, candidates[candidateIndex], mesh.bindposes, sceneBody.bones,
                            candidateRelevantVertices[candidateIndex], positions[candidateIndex]);
                        foreach (var vertex in candidateRelevantVertices[candidateIndex])
                        {
                            var position = positions[candidateIndex][vertex];
                            var distance = Mathf.Min(
                                DistanceToLeftArmSegment(position, shoulder.position, arm.position),
                                DistanceToLeftArmSegment(position, arm.position, foreArm.position),
                                DistanceToLeftArmSegment(position, foreArm.position, hand.position),
                                Vector3.Distance(position, hand.position));
                            if (distance <= maximumDistances[candidateIndex])
                                continue;
                            maximumDistances[candidateIndex] = distance;
                            worstFrames[candidateIndex] = frame;
                            worstVertices[candidateIndex] = vertex;
                        }
                    }
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                foreach (var snapshot in snapshots) snapshot.Restore();
            }

            var worstComponent = DescribeVertexComponent(
                mesh, originalWeights, sceneBody.bones, worstVertices[0]);
            var suspiciousComponents = DescribeSuspiciousLeftShoulderComponents(
                mesh, originalWeights, sceneBody.bones, sourceMesh);
            return "RelevantVertices=" + relevantVertices.Length +
                   ", WorstComponent=" + worstComponent + ", " +
                   "SuspiciousComponents={" + suspiciousComponents + "}, " +
                   string.Join("|", Enumerable.Range(0, labels.Length).Select(index =>
                       labels[index] + "[MaxDistance=" + Num(maximumDistances[index]) +
                       ",Relevant=" + candidateRelevantVertices[index].Length +
                       ",Frame=" + worstFrames[index] +
                       ",Vertex=" + worstVertices[index] +
                       ",Weights=" + DescribeWeights(candidates[index][worstVertices[index]], sceneBody.bones) + "]"));
        }

        private static void ApplyRigidComponentBinding(
            Mesh mesh,
            BoneWeight[] weights,
            Transform[] bones,
            int seedVertex,
            int expectedVertexCount,
            string boneName)
        {
            var component = ConnectedComponentVertices(mesh, seedVertex);
            if (component.Count != expectedVertexCount || component.Min() != seedVertex)
                throw new InvalidOperationException("A mapped armor component topology differs at seed " + seedVertex + ".");
            var boneIndex = Array.FindIndex(bones, bone => bone != null && bone.name == boneName);
            if (boneIndex < 0)
                throw new InvalidOperationException("The mapped armor bone is missing: " + boneName + ".");
            foreach (var vertex in component)
                weights[vertex] = new BoneWeight { boneIndex0 = boneIndex, weight0 = 1f };
        }

        private static string DescribeSuspiciousLeftShoulderComponents(
            Mesh mesh,
            IReadOnlyList<BoneWeight> weights,
            Transform[] bones,
            Mesh sourceMesh)
        {
            var leftShoulderIndex = Array.FindIndex(bones,
                bone => bone != null && bone.name == "LeftShoulder");
            var components = new List<HashSet<int>>();
            var visited = new HashSet<int>();
            for (var seed = 0; seed < mesh.vertexCount; seed++)
            {
                if (visited.Contains(seed)) continue;
                var component = ConnectedComponentVertices(mesh, seed);
                visited.UnionWith(component);
                components.Add(component);
            }
            var vertices = mesh.vertices;
            var bindposes = mesh.bindposes;
            return string.Join("|", components.Select(component =>
                {
                    var bounds = new Bounds(vertices[component.First()], Vector3.zero);
                    var shoulderWeight = 0f;
                    foreach (var vertex in component)
                    {
                        bounds.Encapsulate(vertices[vertex]);
                        shoulderWeight += WeightForBone(weights[vertex], leftShoulderIndex);
                    }
                    var averageShoulderWeight = shoulderWeight / component.Count;
                    if (averageShoulderWeight < 0.9f || bounds.center.y >= 1f)
                        return null;
                    var closestBones = Enumerable.Range(0, bones.Length)
                        .Select(index => new KeyValuePair<string, float>(
                            bones[index].name,
                            component.Average(vertex =>
                                bindposes[index].MultiplyPoint3x4(vertices[vertex]).magnitude)))
                        .OrderBy(item => item.Value)
                        .Take(3)
                        .Select(item => item.Key + ":" + Num(item.Value));
                    var mirroredCenter = new Vector3(-bounds.center.x, bounds.center.y, bounds.center.z);
                    var mirror = components.Where(candidate => candidate != component)
                        .Select(candidate =>
                        {
                            var candidateBounds = new Bounds(vertices[candidate.First()], Vector3.zero);
                            var candidateTotals = new float[bones.Length];
                            foreach (var vertex in candidate)
                            {
                                candidateBounds.Encapsulate(vertices[vertex]);
                                var weight = weights[vertex];
                                candidateTotals[weight.boneIndex0] += weight.weight0;
                                candidateTotals[weight.boneIndex1] += weight.weight1;
                                candidateTotals[weight.boneIndex2] += weight.weight2;
                                candidateTotals[weight.boneIndex3] += weight.weight3;
                            }
                            var score = Vector3.Distance(candidateBounds.center, mirroredCenter) +
                                        Vector3.Distance(candidateBounds.size, bounds.size) +
                                        Mathf.Abs(candidate.Count - component.Count) * 0.01f;
                            return new
                            {
                                Component = candidate,
                                Bounds = candidateBounds,
                                Totals = candidateTotals,
                                Score = score
                            };
                        })
                        .Where(item => item.Bounds.center.x > 0f)
                        .OrderBy(item => item.Score)
                        .Select(item =>
                        {
                            var candidateWeights = Enumerable.Range(0, bones.Length)
                                .Where(index => item.Totals[index] > 0f)
                                .OrderByDescending(index => item.Totals[index])
                                .Take(3)
                                .Select(index => bones[index].name + ":" +
                                                 Num(item.Totals[index] / item.Component.Count));
                            return "Score=" + Num(item.Score) +
                                   ",Seed=" + item.Component.Min() +
                                   ",Vertices=" + item.Component.Count +
                                   ",Center=" + Vec(item.Bounds.center) +
                                   ",Size=" + Vec(item.Bounds.size) +
                                   ",Weights=" + string.Join("+", candidateWeights);
                        })
                        .First();
                    return "Seed=" + component.Min() +
                           ",Vertices=" + component.Count +
                           ",Center=" + Vec(bounds.center) +
                           ",Size=" + Vec(bounds.size) +
                           ",LeftShoulder=" + Num(averageShoulderWeight) +
                           ",Closest=" + string.Join("+", closestBones) +
                           ",SpatialNeighbors=" + DescribeSpatialNeighborWeights(
                               component, components, vertices, weights, bones, leftShoulderIndex) +
                           ",SourceNeighbors=" + DescribeNearestSourceWeights(
                               component, vertices, sourceMesh, bones) +
                           ",Mirror=[" + mirror + "]";
                })
                .Where(item => item != null));
        }

        private static BoneWeight[] BuildSourceTransferredComponentWeights(Mesh targetMesh, Mesh sourceMesh)
        {
            var result = targetMesh.boneWeights.ToArray();
            var targetVertices = targetMesh.vertices;
            var sourceVertices = sourceMesh.vertices;
            var sourceWeights = sourceMesh.boneWeights;
            foreach (var item in new[] { (8177, 16), (8322, 34), (8356, 17), (8373, 24),
                         (8397, 7), (8404, 5), (8460, 4), (8729, 6) })
            {
                var component = ConnectedComponentVertices(targetMesh, item.Item1);
                if (component.Count != item.Item2 || component.Min() != item.Item1)
                    throw new InvalidOperationException("A source-transfer component topology differs at seed " + item.Item1 + ".");
                foreach (var vertex in component)
                {
                    var nearest = NearestVertexIndex(targetVertices[vertex], sourceVertices, out _);
                    result[vertex] = sourceWeights[nearest];
                }
            }
            return result;
        }

        private static string DescribeNearestSourceWeights(
            IEnumerable<int> component,
            IReadOnlyList<Vector3> targetVertices,
            Mesh sourceMesh,
            Transform[] bones)
        {
            var sourceVertices = sourceMesh.vertices;
            var sourceWeights = sourceMesh.boneWeights;
            var totals = new float[bones.Length];
            var maximumDistance = 0f;
            var count = 0;
            foreach (var vertex in component)
            {
                var nearest = NearestVertexIndex(targetVertices[vertex], sourceVertices, out var distance);
                maximumDistance = Mathf.Max(maximumDistance, distance);
                var weight = sourceWeights[nearest];
                totals[weight.boneIndex0] += weight.weight0;
                totals[weight.boneIndex1] += weight.weight1;
                totals[weight.boneIndex2] += weight.weight2;
                totals[weight.boneIndex3] += weight.weight3;
                count++;
            }
            var dominant = Enumerable.Range(0, bones.Length)
                .Where(index => totals[index] > 0f)
                .OrderByDescending(index => totals[index])
                .Take(4)
                .Select(index => bones[index].name + ":" + Num(totals[index] / count));
            return "MaxDistance=" + Num(maximumDistance) +
                   ",Weights=" + string.Join("+", dominant);
        }

        private static int NearestVertexIndex(
            Vector3 target,
            IReadOnlyList<Vector3> candidates,
            out float distance)
        {
            var nearestIndex = 0;
            var nearestSquaredDistance = float.PositiveInfinity;
            for (var index = 0; index < candidates.Count; index++)
            {
                var squaredDistance = (target - candidates[index]).sqrMagnitude;
                if (squaredDistance >= nearestSquaredDistance)
                    continue;
                nearestSquaredDistance = squaredDistance;
                nearestIndex = index;
            }
            distance = Mathf.Sqrt(nearestSquaredDistance);
            return nearestIndex;
        }

        private static string DescribeSpatialNeighborWeights(
            HashSet<int> component,
            IReadOnlyList<HashSet<int>> components,
            IReadOnlyList<Vector3> vertices,
            IReadOnlyList<BoneWeight> weights,
            Transform[] bones,
            int leftShoulderIndex)
        {
            var excluded = new HashSet<int>();
            foreach (var candidate in components)
            {
                var candidateBounds = new Bounds(vertices[candidate.First()], Vector3.zero);
                var shoulderWeight = 0f;
                foreach (var vertex in candidate)
                {
                    candidateBounds.Encapsulate(vertices[vertex]);
                    shoulderWeight += WeightForBone(weights[vertex], leftShoulderIndex);
                }
                if (shoulderWeight / candidate.Count >= 0.9f && candidateBounds.center.y < 1f)
                    excluded.UnionWith(candidate);
            }
            var neighbors = Enumerable.Range(0, vertices.Count)
                .Where(index => !excluded.Contains(index))
                .Select(index => new
                {
                    Index = index,
                    Distance = component.Min(vertex =>
                        Vector3.Distance(vertices[vertex], vertices[index]))
                })
                .OrderBy(item => item.Distance)
                .Take(32)
                .ToArray();
            var totals = new float[bones.Length];
            foreach (var neighbor in neighbors)
            {
                var weight = weights[neighbor.Index];
                totals[weight.boneIndex0] += weight.weight0;
                totals[weight.boneIndex1] += weight.weight1;
                totals[weight.boneIndex2] += weight.weight2;
                totals[weight.boneIndex3] += weight.weight3;
            }
            var dominant = Enumerable.Range(0, bones.Length)
                .Where(index => totals[index] > 0f)
                .OrderByDescending(index => totals[index])
                .Take(4)
                .Select(index => bones[index].name + ":" + Num(totals[index] / neighbors.Length));
            return "Nearest=" + Num(neighbors[0].Distance) +
                   ",Farthest32=" + Num(neighbors[neighbors.Length - 1].Distance) +
                   ",Weights=" + string.Join("+", dominant);
        }

        private static HashSet<int> ConnectedComponentVertices(Mesh mesh, int seedVertex)
        {
            var adjacency = Enumerable.Range(0, mesh.vertexCount).Select(_ => new List<int>()).ToArray();
            var triangles = mesh.triangles;
            for (var offset = 0; offset < triangles.Length; offset += 3)
            {
                var first = triangles[offset];
                var second = triangles[offset + 1];
                var third = triangles[offset + 2];
                adjacency[first].Add(second); adjacency[first].Add(third);
                adjacency[second].Add(first); adjacency[second].Add(third);
                adjacency[third].Add(first); adjacency[third].Add(second);
            }
            var result = new HashSet<int> { seedVertex };
            var pending = new Queue<int>();
            pending.Enqueue(seedVertex);
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                foreach (var neighbor in adjacency[current])
                    if (result.Add(neighbor)) pending.Enqueue(neighbor);
            }
            return result;
        }

        private static string DescribeVertexComponent(
            Mesh mesh,
            IReadOnlyList<BoneWeight> weights,
            Transform[] bones,
            int seedVertex)
        {
            var adjacency = Enumerable.Range(0, mesh.vertexCount).Select(_ => new List<int>()).ToArray();
            var triangles = mesh.triangles;
            for (var offset = 0; offset < triangles.Length; offset += 3)
            {
                var first = triangles[offset];
                var second = triangles[offset + 1];
                var third = triangles[offset + 2];
                adjacency[first].Add(second); adjacency[first].Add(third);
                adjacency[second].Add(first); adjacency[second].Add(third);
                adjacency[third].Add(first); adjacency[third].Add(second);
            }
            var component = new HashSet<int> { seedVertex };
            var pending = new Queue<int>();
            pending.Enqueue(seedVertex);
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                foreach (var neighbor in adjacency[current])
                    if (component.Add(neighbor)) pending.Enqueue(neighbor);
            }

            var vertices = mesh.vertices;
            var bounds = new Bounds(vertices[seedVertex], Vector3.zero);
            var boneTotals = new float[bones.Length];
            foreach (var vertex in component)
            {
                bounds.Encapsulate(vertices[vertex]);
                var weight = weights[vertex];
                boneTotals[weight.boneIndex0] += weight.weight0;
                boneTotals[weight.boneIndex1] += weight.weight1;
                boneTotals[weight.boneIndex2] += weight.weight2;
                boneTotals[weight.boneIndex3] += weight.weight3;
            }
            var bindposes = mesh.bindposes;
            var closestBones = Enumerable.Range(0, bones.Length)
                .Select(index => new KeyValuePair<string, float>(
                    bones[index].name,
                    bindposes[index].MultiplyPoint3x4(vertices[seedVertex]).magnitude))
                .OrderBy(item => item.Value)
                .Take(5)
                .Select(item => item.Key + ":" + Num(item.Value));
            var dominantBones = Enumerable.Range(0, bones.Length)
                .Where(index => boneTotals[index] > 0f)
                .OrderByDescending(index => boneTotals[index])
                .Take(5)
                .Select(index => bones[index].name + ":" + Num(boneTotals[index] / component.Count));
            var mirroredCenter = new Vector3(-bounds.center.x, bounds.center.y, bounds.center.z);
            var allComponents = new List<HashSet<int>>();
            var visited = new HashSet<int>();
            for (var seed = 0; seed < mesh.vertexCount; seed++)
            {
                if (!visited.Add(seed))
                    continue;
                var candidate = new HashSet<int> { seed };
                pending.Enqueue(seed);
                while (pending.Count > 0)
                {
                    var current = pending.Dequeue();
                    foreach (var neighbor in adjacency[current])
                    {
                        if (!candidate.Add(neighbor)) continue;
                        visited.Add(neighbor);
                        pending.Enqueue(neighbor);
                    }
                }
                allComponents.Add(candidate);
            }
            var mirrorCandidates = allComponents
                .Where(candidate => !candidate.Contains(seedVertex))
                .Select(candidate =>
                {
                    var candidateBounds = new Bounds(vertices[candidate.First()], Vector3.zero);
                    var candidateTotals = new float[bones.Length];
                    foreach (var vertex in candidate)
                    {
                        candidateBounds.Encapsulate(vertices[vertex]);
                        var weight = weights[vertex];
                        candidateTotals[weight.boneIndex0] += weight.weight0;
                        candidateTotals[weight.boneIndex1] += weight.weight1;
                        candidateTotals[weight.boneIndex2] += weight.weight2;
                        candidateTotals[weight.boneIndex3] += weight.weight3;
                    }
                    var score = Vector3.Distance(candidateBounds.center, mirroredCenter) +
                                Vector3.Distance(candidateBounds.size, bounds.size) +
                                Mathf.Abs(candidate.Count - component.Count) * 0.01f;
                    var candidateBones = Enumerable.Range(0, bones.Length)
                        .Where(index => candidateTotals[index] > 0f)
                        .OrderByDescending(index => candidateTotals[index])
                        .Take(3)
                        .Select(index => bones[index].name + ":" + Num(candidateTotals[index] / candidate.Count));
                    return new
                    {
                        Score = score,
                        candidate.Count,
                        Bounds = candidateBounds,
                        Bones = string.Join("+", candidateBones)
                    };
                })
                .OrderBy(item => item.Score)
                .Take(5)
                .Select(item => "Score=" + Num(item.Score) +
                                ",Vertices=" + item.Count +
                                ",Center=" + Vec(item.Bounds.center) +
                                ",Size=" + Vec(item.Bounds.size) +
                                ",Weights=" + item.Bones);
            return "Seed=" + seedVertex +
                   ", Vertices=" + component.Count +
                   ", BoundsCenter=" + Vec(bounds.center) +
                   ", BoundsSize=" + Vec(bounds.size) +
                   ", ClosestBones=" + string.Join("+", closestBones) +
                   ", AverageWeights=" + string.Join("+", dominantBones) +
                   ", MirrorCandidates={" + string.Join("|", mirrorCandidates) + "}";
        }

        private static BoneWeight[] RestrictWeightsToBones(
            IReadOnlyList<BoneWeight> source,
            HashSet<int> allowedBones,
            float minimumAllowedWeight)
        {
            var result = source.ToArray();
            for (var index = 0; index < result.Length; index++)
            {
                var weight = result[index];
                var influences = new[]
                {
                    new KeyValuePair<int, float>(weight.boneIndex0, weight.weight0),
                    new KeyValuePair<int, float>(weight.boneIndex1, weight.weight1),
                    new KeyValuePair<int, float>(weight.boneIndex2, weight.weight2),
                    new KeyValuePair<int, float>(weight.boneIndex3, weight.weight3)
                };
                var allowed = influences.Where(item => item.Value > 0f && allowedBones.Contains(item.Key))
                    .OrderByDescending(item => item.Value)
                    .ToArray();
                var total = allowed.Sum(item => item.Value);
                if (total < minimumAllowedWeight)
                    continue;
                var normalized = allowed.Select(item =>
                        new KeyValuePair<int, float>(item.Key, item.Value / total))
                    .Concat(Enumerable.Repeat(new KeyValuePair<int, float>(0, 0f), 4))
                    .Take(4)
                    .ToArray();
                result[index] = new BoneWeight
                {
                    boneIndex0 = normalized[0].Key,
                    weight0 = normalized[0].Value,
                    boneIndex1 = normalized[1].Key,
                    weight1 = normalized[1].Value,
                    boneIndex2 = normalized[2].Key,
                    weight2 = normalized[2].Value,
                    boneIndex3 = normalized[3].Key,
                    weight3 = normalized[3].Value
                };
            }
            return result;
        }

        private static float DistanceToLeftArmSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            var segment = end - start;
            if (segment.sqrMagnitude <= 0.000001f)
                return Vector3.Distance(point, start);
            var amount = Mathf.Clamp01(Vector3.Dot(point - start, segment) / segment.sqrMagnitude);
            return Vector3.Distance(point, start + segment * amount);
        }

        private static string DescribeWeights(BoneWeight weight, Transform[] bones)
        {
            var values = new[]
            {
                new KeyValuePair<int, float>(weight.boneIndex0, weight.weight0),
                new KeyValuePair<int, float>(weight.boneIndex1, weight.weight1),
                new KeyValuePair<int, float>(weight.boneIndex2, weight.weight2),
                new KeyValuePair<int, float>(weight.boneIndex3, weight.weight3)
            };
            return string.Join("+", values.Where(item => item.Value > 0f)
                .Select(item => bones[item.Key].name + ":" + Num(item.Value)));
        }

        private static string InspectSourceSkinningCompatibility(Transform model, AnimationClip clip)
        {
            var targetBody = RequireBody(model);
            var sourceBody = RequireAsset<GameObject>(SourcePath)
                .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var targetMesh = targetBody.sharedMesh;
            var sourceMesh = sourceBody.sharedMesh;
            var targetVertices = targetMesh.vertices;
            var sourceVertices = sourceMesh.vertices;
            var targetTriangles = targetMesh.triangles;
            var sourceTriangles = sourceMesh.triangles;
            var targetWeights = targetMesh.boneWeights;
            var sourceWeights = sourceMesh.boneWeights;
            var vertexCountSame = targetVertices.Length == sourceVertices.Length;
            var triangleOrderSame = targetTriangles.SequenceEqual(sourceTriangles);
            var boneOrderSame = targetBody.bones.Select(item => item.name)
                .SequenceEqual(sourceBody.bones.Select(item => item.name), StringComparer.Ordinal);
            var maximumVertexDelta = vertexCountSame
                ? Enumerable.Range(0, targetVertices.Length)
                    .Max(index => Vector3.Distance(targetVertices[index], sourceVertices[index]))
                : float.PositiveInfinity;
            var weightsSame = targetWeights.Length == sourceWeights.Length &&
                              Enumerable.Range(0, targetWeights.Length)
                                  .All(index => BoneWeightMatches(targetWeights[index], sourceWeights[index]));
            var bindposeCountSame = targetMesh.bindposes.Length == sourceMesh.bindposes.Length;
            var maximumBindposeDelta = bindposeCountSame
                ? Enumerable.Range(0, targetMesh.bindposes.Length)
                    .Max(index => MatrixDelta(targetMesh.bindposes[index], sourceMesh.bindposes[index]))
                : float.PositiveInfinity;

            var nearestMaximumDistance = float.PositiveInfinity;
            var nearestAverageDistance = float.PositiveInfinity;
            var transferredWeightMaxEdgeRatio = float.PositiveInfinity;
            var targetWeightMaxEdgeRatio = float.PositiveInfinity;
            var worstFrame = -1;
            if (boneOrderSame && bindposeCountSame)
            {
                var transferredWeights = new BoneWeight[targetVertices.Length];
                var distanceSum = 0f;
                nearestMaximumDistance = 0f;
                if (vertexCountSame && triangleOrderSame && maximumVertexDelta <= 0.000001f)
                {
                    Array.Copy(sourceWeights, transferredWeights, sourceWeights.Length);
                }
                else
                {
                    for (var targetIndex = 0; targetIndex < targetVertices.Length; targetIndex++)
                    {
                        var nearestSourceIndex = 0;
                        var nearestSquaredDistance = float.PositiveInfinity;
                        for (var sourceIndex = 0; sourceIndex < sourceVertices.Length; sourceIndex++)
                        {
                            var squaredDistance = (targetVertices[targetIndex] - sourceVertices[sourceIndex]).sqrMagnitude;
                            if (squaredDistance >= nearestSquaredDistance)
                                continue;
                            nearestSquaredDistance = squaredDistance;
                            nearestSourceIndex = sourceIndex;
                        }
                        var distance = Mathf.Sqrt(nearestSquaredDistance);
                        distanceSum += distance;
                        nearestMaximumDistance = Mathf.Max(nearestMaximumDistance, distance);
                        transferredWeights[targetIndex] = sourceWeights[nearestSourceIndex];
                    }
                }
                nearestAverageDistance = distanceSum / targetVertices.Length;
                var leftArmBoneIndices = new HashSet<int>(LeftArmBoneNames.Select(name =>
                    Array.FindIndex(targetBody.bones, bone => bone != null && bone.name == name)));
                var relevantTriangleOffsets = Enumerable.Range(0, targetTriangles.Length / 3)
                    .Select(index => index * 3)
                    .Where(offset => WeightForBones(targetWeights[targetTriangles[offset]], leftArmBoneIndices) >= 0.1f ||
                                     WeightForBones(targetWeights[targetTriangles[offset + 1]], leftArmBoneIndices) >= 0.1f ||
                                     WeightForBones(targetWeights[targetTriangles[offset + 2]], leftArmBoneIndices) >= 0.1f ||
                                     WeightForBones(transferredWeights[targetTriangles[offset]], leftArmBoneIndices) >= 0.1f ||
                                     WeightForBones(transferredWeights[targetTriangles[offset + 1]], leftArmBoneIndices) >= 0.1f ||
                                     WeightForBones(transferredWeights[targetTriangles[offset + 2]], leftArmBoneIndices) >= 0.1f)
                    .ToArray();
                var relevantVertices = relevantTriangleOffsets
                    .SelectMany(offset => new[] { targetTriangles[offset], targetTriangles[offset + 1], targetTriangles[offset + 2] })
                    .Distinct()
                    .ToArray();
                var targetPositions = new Vector3[targetMesh.vertexCount];
                var transferredPositions = new Vector3[targetMesh.vertexCount];
                targetWeightMaxEdgeRatio = 0f;
                transferredWeightMaxEdgeRatio = 0f;
                var snapshots = model.GetComponentsInChildren<Transform>(true)
                    .Select(item => new TransformSnapshot(item))
                    .ToArray();
                try
                {
                    AnimationMode.StartAnimationMode();
                    var frames = Mathf.RoundToInt(clip.length * clip.frameRate);
                    for (var frame = 0; frame <= frames; frame++)
                    {
                        AnimationMode.SampleAnimationClip(model.gameObject, clip, frame / clip.frameRate);
                        SkinFourBoneVertices(targetMesh, targetWeights, targetMesh.bindposes, targetBody.bones,
                            relevantVertices, targetPositions);
                        SkinFourBoneVertices(targetMesh, transferredWeights, targetMesh.bindposes, targetBody.bones,
                            relevantVertices, transferredPositions);
                        foreach (var offset in relevantTriangleOffsets)
                        {
                            var indices = new[]
                                { targetTriangles[offset], targetTriangles[offset + 1], targetTriangles[offset + 2] };
                            for (var edge = 0; edge < 3; edge++)
                            {
                                var first = indices[edge];
                                var second = indices[(edge + 1) % 3];
                                var authoredLength = targetBody.transform.TransformVector(
                                    targetVertices[first] - targetVertices[second]).magnitude;
                                var targetRatio = Vector3.Distance(targetPositions[first], targetPositions[second]) /
                                                  Mathf.Max(0.000001f, authoredLength);
                                var transferredRatio = Vector3.Distance(
                                                           transferredPositions[first], transferredPositions[second]) /
                                                       Mathf.Max(0.000001f, authoredLength);
                                targetWeightMaxEdgeRatio = Mathf.Max(targetWeightMaxEdgeRatio, targetRatio);
                                if (transferredRatio <= transferredWeightMaxEdgeRatio)
                                    continue;
                                transferredWeightMaxEdgeRatio = transferredRatio;
                                worstFrame = frame;
                            }
                        }
                    }
                }
                finally
                {
                    if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                    foreach (var snapshot in snapshots) snapshot.Restore();
                }
            }

            return "VertexCount=" + targetVertices.Length + "/" + sourceVertices.Length +
                   ", VertexOrderMaxDelta=" + Num(maximumVertexDelta) +
                   ", TriangleOrderSame=" + triangleOrderSame +
                   ", BoneOrderSame=" + boneOrderSame +
                   ", WeightsSame=" + weightsSame +
                   ", BindposeCount=" + targetMesh.bindposes.Length + "/" + sourceMesh.bindposes.Length +
                   ", BindposeMaxDelta=" + Num(maximumBindposeDelta) +
                   ", Bounds=" + Vec(targetMesh.bounds.size) + "/" + Vec(sourceMesh.bounds.size) +
                   ", NearestAverageDistance=" + Num(nearestAverageDistance) +
                   ", NearestMaximumDistance=" + Num(nearestMaximumDistance) +
                   ", TargetWeightMaxEdgeRatio=" + Num(targetWeightMaxEdgeRatio) +
                   ", TransferredWeightMaxEdgeRatio=" + Num(transferredWeightMaxEdgeRatio) +
                   ", TransferredWorstFrame=" + worstFrame;
        }

        private static void SkinFourBoneVertices(
            Mesh mesh,
            BoneWeight[] weights,
            Matrix4x4[] bindposes,
            Transform[] bones,
            IReadOnlyCollection<int> relevantVertices,
            Vector3[] positions)
        {
            var vertices = mesh.vertices;
            foreach (var index in relevantVertices)
            {
                var weight = weights[index];
                var position = Vector3.zero;
                AddSkin(ref position, vertices[index], weight.boneIndex0, weight.weight0, bones, bindposes);
                AddSkin(ref position, vertices[index], weight.boneIndex1, weight.weight1, bones, bindposes);
                AddSkin(ref position, vertices[index], weight.boneIndex2, weight.weight2, bones, bindposes);
                AddSkin(ref position, vertices[index], weight.boneIndex3, weight.weight3, bones, bindposes);
                positions[index] = position;
            }
        }

        private static bool BoneWeightMatches(BoneWeight first, BoneWeight second) =>
            first.boneIndex0 == second.boneIndex0 && first.boneIndex1 == second.boneIndex1 &&
            first.boneIndex2 == second.boneIndex2 && first.boneIndex3 == second.boneIndex3 &&
            Mathf.Abs(first.weight0 - second.weight0) <= 0.000001f &&
            Mathf.Abs(first.weight1 - second.weight1) <= 0.000001f &&
            Mathf.Abs(first.weight2 - second.weight2) <= 0.000001f &&
            Mathf.Abs(first.weight3 - second.weight3) <= 0.000001f;

        private static float MatrixDelta(Matrix4x4 first, Matrix4x4 second)
        {
            var result = 0f;
            for (var row = 0; row < 4; row++)
            for (var column = 0; column < 4; column++)
                result = Mathf.Max(result, Mathf.Abs(first[row, column] - second[row, column]));
            return result;
        }

        private static void SkinVertices(
            Mesh mesh,
            BoneWeight[] weights,
            Matrix4x4[] bindposes,
            Transform[] bones,
            IReadOnlyCollection<int> relevantVertices,
            Vector3[] oneBonePositions,
            Vector3[] twoBonePositions,
            Vector3[] fourBonePositions)
        {
            var vertices = mesh.vertices;
            foreach (var index in relevantVertices)
            {
                var weight = weights[index];
                var dominantIndex = weight.boneIndex0;
                var dominantWeight = weight.weight0;
                if (weight.weight1 > dominantWeight) { dominantIndex = weight.boneIndex1; dominantWeight = weight.weight1; }
                if (weight.weight2 > dominantWeight) { dominantIndex = weight.boneIndex2; dominantWeight = weight.weight2; }
                if (weight.weight3 > dominantWeight) dominantIndex = weight.boneIndex3;
                oneBonePositions[index] = bones[dominantIndex].TransformPoint(
                    bindposes[dominantIndex].MultiplyPoint3x4(vertices[index]));
                var firstWeight = weight.weight0;
                var secondWeight = weight.weight1;
                var totalTwoBoneWeight = firstWeight + secondWeight;
                var twoBonePosition = Vector3.zero;
                AddSkin(ref twoBonePosition, vertices[index], weight.boneIndex0,
                    firstWeight / totalTwoBoneWeight, bones, bindposes);
                AddSkin(ref twoBonePosition, vertices[index], weight.boneIndex1,
                    secondWeight / totalTwoBoneWeight, bones, bindposes);
                twoBonePositions[index] = twoBonePosition;
                var fourBonePosition = Vector3.zero;
                AddSkin(ref fourBonePosition, vertices[index], weight.boneIndex0, weight.weight0, bones, bindposes);
                AddSkin(ref fourBonePosition, vertices[index], weight.boneIndex1, weight.weight1, bones, bindposes);
                AddSkin(ref fourBonePosition, vertices[index], weight.boneIndex2, weight.weight2, bones, bindposes);
                AddSkin(ref fourBonePosition, vertices[index], weight.boneIndex3, weight.weight3, bones, bindposes);
                fourBonePositions[index] = fourBonePosition;
            }
        }

        private static string DominantBoneName(BoneWeight weight, Transform[] bones)
        {
            var index = weight.boneIndex0;
            var value = weight.weight0;
            if (weight.weight1 > value) { index = weight.boneIndex1; value = weight.weight1; }
            if (weight.weight2 > value) { index = weight.boneIndex2; value = weight.weight2; }
            if (weight.weight3 > value) index = weight.boneIndex3;
            return bones[index] != null ? bones[index].name : "<null>";
        }

        private static float WeightForBone(BoneWeight weight, int bone)
        {
            var result = 0f;
            if (weight.boneIndex0 == bone) result += weight.weight0;
            if (weight.boneIndex1 == bone) result += weight.weight1;
            if (weight.boneIndex2 == bone) result += weight.weight2;
            if (weight.boneIndex3 == bone) result += weight.weight3;
            return result;
        }

        private static float WeightForBones(BoneWeight weight, HashSet<int> bones)
        {
            var result = 0f;
            if (bones.Contains(weight.boneIndex0)) result += weight.weight0;
            if (bones.Contains(weight.boneIndex1)) result += weight.weight1;
            if (bones.Contains(weight.boneIndex2)) result += weight.weight2;
            if (bones.Contains(weight.boneIndex3)) result += weight.weight3;
            return result;
        }

        private static float ScaleDeviation(Transform value) =>
            Mathf.Max(Mathf.Abs(value.lossyScale.x - 1f), Mathf.Abs(value.lossyScale.y - 1f),
                Mathf.Abs(value.lossyScale.z - 1f));

        private static float DistanceToSegment(Vector3 point, Vector3 first, Vector3 second)
        {
            var direction = second - first;
            if (direction.sqrMagnitude <= 0.0000001f)
                return Vector3.Distance(point, first);
            var amount = Mathf.Clamp01(Vector3.Dot(point - first, direction) / direction.sqrMagnitude);
            return Vector3.Distance(point, first + direction * amount);
        }

        private static Target RequireTarget(Scene scene)
        {
            var placement = scene.GetRootGameObjects().Single(item => item.name == PlacementName).transform;
            if (placement.childCount != 12) throw new InvalidOperationException("The Ispant placement slot count differs.");
            var slot = placement.Find(SlotName) ?? throw new InvalidOperationException("Ispant_04_DrawSword is missing.");
            if (slot.parent != placement || slot.childCount != 1) throw new InvalidOperationException("Ispant_04_DrawSword hierarchy differs.");
            var model = slot.GetChild(0);
            if (model.name != ModelName) throw new InvalidOperationException("The direct model is missing from Ispant_04_DrawSword.");
            var source = PrefabUtility.GetCorrespondingObjectFromSource(model.gameObject);
            if (source == null || AssetDatabase.GetAssetPath(source) != ModelPath)
                throw new InvalidOperationException("Ispant_04_DrawSword no longer references the direct FBX.");
            return new Target(placement, slot, model);
        }

        private static Scene RequireScene(bool clean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath) throw new InvalidOperationException("CargoRunMvp must be active.");
            if (clean && scene.isDirty) throw new InvalidOperationException("CargoRunMvp has unsaved changes.");
            return scene;
        }

        private static ModelImporter RequireImporter() => AssetImporter.GetAtPath(SourcePath) as ModelImporter ?? throw new InvalidOperationException("The draw-sword importer is missing.");
        private static AnimationClip[] ImportedClips() => AssetDatabase.LoadAllAssetsAtPath(SourcePath).OfType<AnimationClip>().Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal)).OrderBy(item => item.name, StringComparer.Ordinal).ToArray();
        private static AnimationClip RequireImportedClip()
        {
            var clips = ImportedClips();
            if (clips.Length != 1 || clips[0].name != ImportedClipName) throw new InvalidOperationException("The selected Mixamo clip differs.");
            return clips[0];
        }
        private static SkinnedMeshRenderer RequireBody(Transform model) => model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(item => item.name == "char1");
        private static MeshRenderer RequireSword(Transform model) => model.GetComponentsInChildren<MeshRenderer>(true).Single(item => item.name == "Ispant_Approved_LongSword_10K");
        private static Transform RequireDescendant(Transform root, string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true).Where(item => item.name == name).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Expected exactly one " + name + ". Count=" + matches.Length + ".");
            return matches[0];
        }
        private static T RequireAsset<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Required asset missing: " + path + ".");
        private static bool IsMixamo(ModelImporterClipAnimation clip) => clip.takeName.IndexOf("mixamo.com", StringComparison.OrdinalIgnoreCase) >= 0;
        private static string[] BoneDescriptions(Transform root)
        {
            var armature = root.Cast<Transform>().Single(item => item.name == "Armature");
            return armature.GetComponentsInChildren<Transform>(true).Where(item => item != armature && item.GetComponent<Renderer>() == null)
                .Select(item => AnimationUtility.CalculateTransformPath(item, armature) + "<-" + (item.parent == armature ? "Armature" : item.parent.name))
                .OrderBy(item => item, StringComparer.Ordinal).ToArray();
        }

        private static void RequireForwardOnlyClip(AnimationClip source, AnimationClip clip)
        {
            if (Mathf.Abs(clip.length - source.length) > 0.001f ||
                Mathf.Abs(clip.frameRate - source.frameRate) > 0.001f)
                throw new InvalidOperationException("The forward-only Mixamo loop duration differs.");
            var sourceBindings = AnimationUtility.GetCurveBindings(source)
                .OrderBy(item => item.path, StringComparer.Ordinal)
                .ThenBy(item => item.propertyName, StringComparer.Ordinal).ToArray();
            var clipBindings = AnimationUtility.GetCurveBindings(clip)
                .OrderBy(item => item.path, StringComparer.Ordinal)
                .ThenBy(item => item.propertyName, StringComparer.Ordinal).ToArray();
            if (sourceBindings.Length != clipBindings.Length)
                throw new InvalidOperationException("The forward-only clip has extra or missing curves.");
            var frames = Mathf.RoundToInt(source.length * source.frameRate);
            for (var index = 0; index < sourceBindings.Length; index++)
            {
                var sourceBinding = sourceBindings[index];
                var clipBinding = clipBindings[index];
                if (sourceBinding.path != clipBinding.path ||
                    sourceBinding.propertyName != clipBinding.propertyName ||
                    sourceBinding.type != clipBinding.type)
                    throw new InvalidOperationException("The forward-only clip binding differs from Mixamo.");
                var sourceCurve = AnimationUtility.GetEditorCurve(source, sourceBinding);
                var clipCurve = AnimationUtility.GetEditorCurve(clip, clipBinding);
                for (var frame = 0; frame <= frames; frame++)
                {
                    var time = frame / source.frameRate;
                    if (Mathf.Abs(sourceCurve.Evaluate(time) - clipCurve.Evaluate(time)) > 0.000001f)
                        throw new InvalidOperationException("The forward-only clip changes a Mixamo frame.");
                }
            }
        }

        private static bool IsLeftArmRotationBinding(EditorCurveBinding binding) =>
            binding.propertyName.StartsWith("m_LocalRotation.", StringComparison.Ordinal) &&
            LeftArmAnimatedBoneNames.Any(name => binding.path.EndsWith("/" + name, StringComparison.Ordinal));

        private static void RequireLeftArmStableClip(AnimationClip source, AnimationClip clip)
        {
            var targetModel = RequireAsset<GameObject>(ModelPath).transform;
            var frames = Mathf.RoundToInt(source.length * source.frameRate);
            foreach (var name in LeftArmAnimatedBoneNames)
            {
                var targetBone = RequireDescendant(targetModel, name);
                var path = AnimationUtility.CalculateTransformPath(targetBone, targetModel);
                for (var frame = 0; frame <= frames; frame++)
                {
                    var time = frame / source.frameRate;
                    var actual = EvaluateRotation(clip, path, time);
                    if (Quaternion.Angle(targetBone.localRotation, actual) > 0.02f)
                        throw new InvalidOperationException(
                            "The stable left-arm rotation differs at " + name + " frame " + frame + ".");
                }
            }
        }

        private static string DescribeClip(ModelImporterClipAnimation clip) => clip.name + "@" + clip.takeName + "[" + Num(clip.firstFrame) + "-" + Num(clip.lastFrame) + "]";
        private static string PositionCurves(AnimationClip clip) => string.Join("|", AnimationUtility.GetCurveBindings(clip).Where(item => item.propertyName.Contains("m_LocalPosition")).OrderBy(item => item.path).ThenBy(item => item.propertyName).Select(item => { var values = AnimationUtility.GetEditorCurve(clip, item).keys.Select(key => key.value).ToArray(); return item.path + ":" + item.propertyName + "[" + Num(values.Min()) + ".." + Num(values.Max()) + "]"; }));
        private static int TriangleCount(Mesh mesh) => Enumerable.Range(0, mesh.subMeshCount).Sum(index => checked((int)mesh.GetIndexCount(index) / 3));
        private static string SwordSignature(Transform model)
        {
            var renderer = RequireSword(model);
            var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            return AssetDatabase.GetAssetPath(mesh) + ":" + mesh.vertexCount + ":" + TriangleCount(mesh) + ":" + mesh.blendShapeCount + ":" + mesh.boneWeights.Length + ":" + string.Join("+", renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath));
        }
        private static string AppearanceSignature(Transform model) =>
            string.Join("|", model.GetComponentsInChildren<Renderer>(true).OrderBy(item => item.name)
                .Select(item => item.name + ":" + item.GetType().FullName + ":" + item.enabled + ":" +
                                string.Join("+", item.sharedMaterials.Select(AssetDatabase.GetAssetPath))));
        private static string OtherSlotSignatures(Transform placement, Transform excluded) => string.Join("|", placement.Cast<Transform>().Where(item => item != excluded).OrderBy(item => item.name).Select(TransformSignature));
        private static string OtherRootSignatures(Scene scene, Transform excluded) => string.Join("|", scene.GetRootGameObjects().Where(item => item.transform != excluded).OrderBy(item => item.name).Select(item => TransformSignature(item.transform)));
        private static string TransformSignature(Transform value) => value.name + ":" + Vec(value.localPosition) + ":" + Quat(value.localRotation) + ":" + Vec(value.localScale) + ":" + value.childCount;
        private static void RequireSame(string before, string after, string message) { if (!string.Equals(before, after, StringComparison.Ordinal)) throw new InvalidOperationException(message); }
        private static void RequireHashes() { RequireHash(SourcePath, SourceHash); RequireHash(ModelPath, ModelHash); }
        private static void RequireHash(string path, string expected)
        {
            using var stream = File.OpenRead(Path.GetFullPath(path));
            using var sha = SHA256.Create();
            var actual = string.Concat(sha.ComputeHash(stream).Select(item => item.ToString("X2", CultureInfo.InvariantCulture)));
            if (actual != expected) throw new InvalidOperationException("Asset hash differs: " + path + ".");
        }
        private static string Num(float value) => value.ToString("0.#########", CultureInfo.InvariantCulture);
        private static string Vec(Vector3 value) => Num(value.x) + "," + Num(value.y) + "," + Num(value.z);
        private static string Quat(Quaternion value) => Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + "," + Num(value.w);

        private readonly struct Target
        {
            public readonly Transform Placement, Slot, Model;
            public Target(Transform placement, Transform slot, Transform model) { Placement = placement; Slot = slot; Model = model; }
        }
        private readonly struct Seam
        {
            public readonly float HandPosition, HandAngle, HipsPosition, HipsAngle;
            public Seam(float handPosition, float handAngle, float hipsPosition, float hipsAngle) { HandPosition = handPosition; HandAngle = handAngle; HipsPosition = hipsPosition; HipsAngle = hipsAngle; }
        }
        private sealed class TransformSnapshot
        {
            private readonly Transform target;
            private readonly Vector3 position, scale;
            private readonly Quaternion rotation;
            public TransformSnapshot(Transform value) { target = value; position = value.localPosition; rotation = value.localRotation; scale = value.localScale; }
            public bool Matches(float tolerance) => target != null && Vector3.Distance(target.localPosition, position) <= tolerance && Quaternion.Angle(target.localRotation, rotation) <= tolerance && Vector3.Distance(target.localScale, scale) <= tolerance;
            public void Restore() { if (target != null) { target.localPosition = position; target.localRotation = rotation; target.localScale = scale; } }
        }
        private readonly struct Inspection
        {
            public readonly Target Target;
            public readonly AnimationClip Clip;
            public readonly float AppliedGripOutwardOffset, MaximumGripOffsetError,
                MinimumGripContactMargin, StartBladeToUpAngle, FinalBladeToUpAngle,
                MaximumBladeAngularStep, MaximumSwordMotion, MaximumHandMotion,
                ImmediateResetPosition, ImmediateResetAngle;
            public readonly int BladeChangingFrames, SwordVertices, SwordTriangles;
            public Inspection(Target target, AnimationClip clip, float gripOffset, float gripError,
                float contactMargin, float startAngle, float finalAngle, float angularStep,
                int changingFrames, float swordMotion, float handMotion, float resetPosition,
                float resetAngle, int vertices, int triangles)
            { Target = target; Clip = clip; AppliedGripOutwardOffset = gripOffset; MaximumGripOffsetError = gripError; MinimumGripContactMargin = contactMargin; StartBladeToUpAngle = startAngle; FinalBladeToUpAngle = finalAngle; MaximumBladeAngularStep = angularStep; BladeChangingFrames = changingFrames; MaximumSwordMotion = swordMotion; MaximumHandMotion = handMotion; ImmediateResetPosition = resetPosition; ImmediateResetAngle = resetAngle; SwordVertices = vertices; SwordTriangles = triangles; }
        }
    }
}
