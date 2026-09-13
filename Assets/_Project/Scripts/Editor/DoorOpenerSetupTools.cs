using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bellerophon.PlayerAnimation;
using Bellerophon.PlayerHands;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    [InitializeOnLoad]
    internal static class DoorOpenerSetupTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string TargetName = "DoorOpener_Idle";
        private const string ModelAssetPath =
            "Assets/_Project/Art/Items/DoorOpener/TemporaryOpeningDevice.fbx";
        private const string TextureFolder =
            "Assets/_Project/Art/Items/DoorOpener/Textures";
        private const string MaterialFolder =
            "Assets/_Project/Art/Items/DoorOpener/Materials";
        private const string MaterialAssetPath =
            MaterialFolder + "/TemporaryOpeningDevice.mat";
        private const string MetallicSmoothnessTexturePath =
            TextureFolder + "/TemporaryOpeningDevice_MetallicSmoothness.png";
        private const string BaseColorTexturePath = TextureFolder + "/base_color.jpg";
        private const string NormalTexturePath = TextureFolder + "/normal.jpg";
        private const string MetallicTexturePath =
            TextureFolder + "/texture_0_metallic.png";
        private const string RoughnessTexturePath =
            TextureFolder + "/texture_0_roughness.png";
        private const string GripPosePath =
            "Assets/_Project/Art/Player/HandsRig/SharedBatteryGripPose.asset";
        private const string ReviewFolder =
            "Assets/_Project/Animation/DoorOpener/Review";
        private const string SourceReportPath = ReviewFolder + "/source_inspection.txt";
        private const string ApplyReportPath = ReviewFolder + "/application.txt";
        private const string BaselinePath = ReviewFolder + "/baseline.txt";
        private const string InspectReportPath = ReviewFolder + "/inspection.txt";
        private const string FinalImagePath = ReviewFolder + "/final.png";
        private const string FinalReportPath = ReviewFolder + "/final.txt";
        private const string LocomotionControllerPath =
            "Assets/_Project/Animation/DoorOpener/DoorOpenerIdle_Locomotion.controller";
        private const string IdleClipPath =
            "Assets/_Project/Animation/DoorOpener/DoorOpenerIdle_Idle.anim";
        private const string ForwardClipPath =
            "Assets/_Project/Animation/DoorOpener/DoorOpenerIdle_WalkForward.anim";
        private const string BackwardClipPath =
            "Assets/_Project/Animation/DoorOpener/DoorOpenerIdle_WalkBackward.anim";
        private const string SidestepClipPath =
            "Assets/_Project/Animation/DoorOpener/DoorOpenerIdle_Sidestep.anim";
        private const string RunClipPath =
            "Assets/_Project/Animation/DoorOpener/DoorOpenerIdle_RunForward.anim";
        private const string LocomotionStateName = "DoorOpenerIdleLocomotion2D";
        private const string RootTreeName = "DoorOpenerIdleSixMotion2D";
        private const string DiagonalTreeName =
            "DoorOpenerIdleWalkDiagonalSourceExact";
        private const string LocomotionSourceReportPath =
            ReviewFolder + "/locomotion_sources.txt";
        private const string LocomotionApplicationReportPath =
            ReviewFolder + "/locomotion_application.txt";
        private const string LocomotionInspectionReportPath =
            ReviewFolder + "/locomotion_inspection.txt";
        private const string LocomotionSourceBaselinePath =
            ReviewFolder + "/locomotion_source_baseline.txt";
        private const string LocomotionCarryBaselinePath =
            ReviewFolder + "/locomotion_carry_baseline.txt";
        private const string LocomotionFinalImagePath =
            ReviewFolder + "/locomotion_final.png";
        private const string LocomotionFinalReportPath =
            ReviewFolder + "/locomotion_final.txt";
        private const string LocomotionCompletionPath =
            ReviewFolder + "/locomotion_completion.txt";
        private const string SourceFileRelativePath =
            "item model/temporary opening device.fbx";
        private const string EmbeddedMaterialName = "Material.001";

        private const string RightShoulderPath =
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder";
        private const string RightArmPath = RightShoulderPath + "/RightArm";
        private const string RightForeArmPath = RightArmPath + "/RightForeArm";
        private const string RightHandPath = RightForeArmPath + "/RightHand";
        private const float DesiredDeviceHeight = 0.34f;
        private const float DesiredGripHeightFraction = 0.27f;
        private const float DesiredGripFrontInsetFraction = 0.08f;
        private const float PositionTolerance = 0.00001f;
        private const float RotationTolerance = 0.1f;
        private static readonly float SqrtHalf = Mathf.Sqrt(0.5f);
        private static readonly string[] SourceNames =
        {
            "Player_Idle", "Player_Walk_Forward", "Player_Walk_Backward",
            "Player_Sidestep", "Player_Walk_Diagonal", "Player_Run_Forward"
        };
        private static readonly string[] RightArmPosePaths =
        {
            RightArmPath, RightForeArmPath, RightHandPath
        };
        private static readonly Vector2[] RequiredPositions =
        {
            new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(0f, -1f), new Vector2(1f, 0f),
            new Vector2(0.70710677f, 0.70710677f), new Vector2(0f, 2f)
        };

        static DoorOpenerSetupTools()
        {
            DoorOpenerIdleLocomotionPlayModeCapture.RestoreAfterDomainReload();
        }

        internal static void InspectSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject modelAsset = RequireModelAsset();
            ModelImporter importer = AssetImporter.GetAtPath(ModelAssetPath) as ModelImporter ??
                throw new InvalidOperationException("Door opener ModelImporter is missing.");
            Bounds localBounds = CalculateLocalBounds(modelAsset);
            ModelAxisInfo axes = DetermineModelAxes(localBounds);

            UnityEngine.Object[] representations =
                AssetDatabase.LoadAllAssetsAtPath(ModelAssetPath);
            Renderer[] renderers = modelAsset.GetComponentsInChildren<Renderer>(true);
            Material[] materials = renderers.SelectMany(item => item.sharedMaterials)
                .Where(item => item != null).Distinct().ToArray();
            Transform hand = RequirePath(target.transform, RightHandPath);
            string[] rightFingerNames = hand.GetComponentsInChildren<Transform>(true)
                .Where(item => item != hand && item.name.StartsWith("Right", StringComparison.Ordinal))
                .Select(item => item.name).OrderBy(item => item, StringComparer.Ordinal).ToArray();

            var report = new StringBuilder()
                .AppendLine("DoorOpener_Idle source inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("scene=" + scene.path)
                .AppendLine("sceneDirty=" + scene.isDirty)
                .AppendLine("targetGlobalId=" + GlobalObjectId.GetGlobalObjectIdSlow(target))
                .AppendLine("sourcePath=" + SourceAbsolutePath())
                .AppendLine("sourceHash=" + ComputeFileHash(SourceAbsolutePath()))
                .AppendLine("importedModelPath=" + ModelAssetPath)
                .AppendLine("importedModelHash=" + ComputeAssetHash(ModelAssetPath))
                .AppendLine("sourceAndImportedBytesEqual=" +
                    (ComputeFileHash(SourceAbsolutePath()) == ComputeAssetHash(ModelAssetPath)))
                .AppendLine("modelLocalBoundsCenter=" + Vec(localBounds.center))
                .AppendLine("modelLocalBoundsSize=" + Vec(localBounds.size))
                .AppendLine("antennaSourceAxis=" + Vec(axes.Antenna))
                .AppendLine("frontSourceAxis=" + Vec(axes.Front))
                .AppendLine("widthSourceAxis=" + Vec(axes.Width))
                .AppendLine("antennaAxisLength=" + Num(axes.AntennaLength))
                .AppendLine("frontAxisDepth=" + Num(axes.FrontDepth))
                .AppendLine("rendererCount=" + renderers.Length)
                .AppendLine("meshFilterCount=" +
                    modelAsset.GetComponentsInChildren<MeshFilter>(true).Length)
                .AppendLine("materialCount=" + materials.Length)
                .AppendLine("representationCount=" + representations.Length)
                .AppendLine("materialImportMode=" + importer.materialImportMode)
                .AppendLine("materialLocation=" + importer.materialLocation)
                .AppendLine("materialSearch=" + importer.materialSearch)
                .AppendLine("rightHandPath=" + RightHandPath)
                .AppendLine("rightFingerJointCount=" + rightFingerNames.Length);
            foreach (string fingerName in rightFingerNames)
                report.AppendLine("rightFingerJoint=" + fingerName);
            foreach (UnityEngine.Object representation in representations
                         .OrderBy(item => item.GetType().Name, StringComparer.Ordinal)
                         .ThenBy(item => item.name, StringComparer.Ordinal))
                report.AppendLine("representation=" + representation.GetType().Name + "|" +
                    representation.name + "|" + AssetDatabase.GetAssetPath(representation));
            foreach (MeshFilter filter in modelAsset.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                report.AppendLine("mesh=" + TransformPath(filter.transform, modelAsset.transform) +
                    "|name=" + (mesh == null ? "null" : mesh.name) +
                    "|vertices=" + (mesh == null ? 0 : mesh.vertexCount) +
                    "|subMeshes=" + (mesh == null ? 0 : mesh.subMeshCount));
            }
            foreach (Material material in materials)
            {
                report.AppendLine("material=" + material.name + "|path=" +
                    AssetDatabase.GetAssetPath(material) + "|shader=" +
                    (material.shader == null ? "null" : material.shader.name));
                foreach (string propertyName in material.GetTexturePropertyNames())
                {
                    Texture texture = material.GetTexture(propertyName);
                    if (texture != null)
                        report.AppendLine("materialTexture=" + material.name + "|property=" +
                            propertyName + "|texture=" + texture.name + "|path=" +
                            AssetDatabase.GetAssetPath(texture));
                }
            }

            WriteText(SourceReportPath, report.ToString());
            RequireNoUnityConsoleErrors();
            Debug.Log("[DoorOpenerIdle] Source model and right-hand rig inspected read-only.");
        }

        internal static void ApplyCarry()
        {
            RequireEditMode();
            ImportEmbeddedAssets();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject modelAsset = RequireModelAsset();
            PlayerHandGripPose gripPose =
                AssetDatabase.LoadAssetAtPath<PlayerHandGripPose>(GripPosePath) ??
                throw new InvalidOperationException("Shared right-hand grip pose is missing.");

            string outsideBefore = SceneOutsideTargetSignature(scene, target);
            string protectedBefore = ProtectedTargetSignature(target.transform);
            string sourceHash = ComputeFileHash(SourceAbsolutePath());
            string modelHash = ComputeAssetHash(ModelAssetPath);
            if (sourceHash != modelHash)
                throw new InvalidOperationException(
                    "Imported door opener FBX bytes differ from the provided source.");

            Transform foreArm = RequirePath(target.transform, RightForeArmPath);
            Transform hand = RequirePath(target.transform, RightHandPath);
            ApplyRightFingerPose(hand, gripPose);

            Bounds localBounds = CalculateLocalBounds(modelAsset);
            ModelAxisInfo axes = DetermineModelAxes(localBounds);
            float scale = DesiredDeviceHeight / axes.AntennaLength;
            Vector3 gripLocalPoint = PointAtAxisFractions(
                localBounds,
                axes,
                DesiredGripHeightFraction,
                DesiredGripFrontInsetFraction);
            Vector3 palmCenter = RightPalmCenter(target.transform);
            Vector3 palmNormal = RightPalmNormal(target.transform);
            Vector3 desiredAntenna = DesiredAntennaDirection(target.transform);
            Vector3 desiredFront = Vector3.ProjectOnPlane(-palmNormal, desiredAntenna).normalized;
            if (desiredFront.sqrMagnitude < 0.99f)
                desiredFront = (target.transform.forward - target.transform.up).normalized;
            Quaternion holderWorldRotation =
                Quaternion.LookRotation(desiredFront, desiredAntenna) *
                Quaternion.Inverse(Quaternion.LookRotation(axes.Front, axes.Antenna));
            Vector3 holderWorldPosition = palmCenter;
            Vector3 holderLocalPosition = hand.InverseTransformPoint(holderWorldPosition);
            Quaternion holderLocalRotation =
                Quaternion.Inverse(hand.rotation) * holderWorldRotation;
            Vector3 modelLocalPosition = -gripLocalPoint * scale;
            Quaternion modelLocalRotation = Quaternion.identity;
            Vector3 modelLocalScale = Vector3.one * scale;

            DoorOpenerRightHandFollowBehaviour behaviour =
                target.GetComponent<DoorOpenerRightHandFollowBehaviour>() ??
                Undo.AddComponent<DoorOpenerRightHandFollowBehaviour>(target);
            Undo.RecordObject(behaviour, "Configure DoorOpener_Idle right-hand carry");
            behaviour.Configure(
                modelAsset,
                gripPose,
                RightForeArmPath,
                RightHandPath,
                foreArm.localRotation,
                hand.localRotation,
                holderLocalPosition,
                holderLocalRotation,
                modelLocalPosition,
                modelLocalRotation,
                modelLocalScale,
                axes.Antenna,
                axes.Front);
            EditorUtility.SetDirty(behaviour);
            behaviour.RefreshPreview();

            RequireEqual(outsideBefore, SceneOutsideTargetSignature(scene, target),
                "scene objects outside DoorOpener_Idle");
            RequireEqual(protectedBefore, ProtectedTargetSignature(target.transform),
                "DoorOpener_Idle protected body");
            RequireEqual(sourceHash, ComputeAssetHash(ModelAssetPath),
                "Door opener source model hash");
            AssertMaterialPipelineComplete(modelAsset);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");

            var baseline = new StringBuilder()
                .AppendLine("outsideSignature=" + ComputeStringHash(outsideBefore))
                .AppendLine("protectedSignature=" + ComputeStringHash(protectedBefore))
                .AppendLine("sourceHash=" + sourceHash)
                .AppendLine("modelHash=" + modelHash)
                .AppendLine("materialHash=" + ComputeAssetHash(MaterialAssetPath));
            foreach (string path in TexturePaths())
                baseline.AppendLine("textureHash=" + path + "|" + ComputeAssetHash(path));
            WriteText(BaselinePath, baseline.ToString());

            var report = new StringBuilder()
                .AppendLine("DoorOpener_Idle carry application")
                .AppendLine("sceneSaved=True")
                .AppendLine("sourceModelChanged=False")
                .AppendLine("sourceAndImportedBytesEqual=True")
                .AppendLine("torsoHeadLowerBodyLeftArmChanged=False")
                .AppendLine("rightForeArmPosePreserved=True")
                .AppendLine("rightHandPosePreserved=True")
                .AppendLine("rightFingerRigApplied=True")
                .AppendLine("rightFingerJointCount=" + RightGripJointCount(gripPose))
                .AppendLine("rightHandPath=" + RightHandPath)
                .AppendLine("holderLocalPosition=" + Vec(holderLocalPosition))
                .AppendLine("holderLocalRotation=" + Quat(holderLocalRotation))
                .AppendLine("modelLocalPosition=" + Vec(modelLocalPosition))
                .AppendLine("modelLocalRotation=" + Quat(modelLocalRotation))
                .AppendLine("modelLocalScale=" + Vec(modelLocalScale))
                .AppendLine("modelLocalBoundsCenter=" + Vec(localBounds.center))
                .AppendLine("modelLocalBoundsSize=" + Vec(localBounds.size))
                .AppendLine("antennaSourceAxis=" + Vec(axes.Antenna))
                .AppendLine("frontSourceAxis=" + Vec(axes.Front))
                .AppendLine("desiredDeviceHeightMeters=" + Num(DesiredDeviceHeight))
                .AppendLine("gripHeightFraction=" + Num(DesiredGripHeightFraction))
                .AppendLine("gripFrontInsetFraction=" + Num(DesiredGripFrontInsetFraction))
                .AppendLine("antennaDirection=" + Vec(desiredAntenna))
                .AppendLine("antennaElevationDegrees=45")
                .AppendLine("embeddedTextureCount=4")
                .AppendLine("externalMaterialPath=" + MaterialAssetPath);
            WriteText(ApplyReportPath, report.ToString());
            RequireNoUnityConsoleErrors();
            Debug.Log("[DoorOpenerIdle] Carry application saved.");
        }

        internal static void InspectCarry()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            DoorOpenerRightHandFollowBehaviour behaviour =
                target.GetComponent<DoorOpenerRightHandFollowBehaviour>() ??
                throw new InvalidOperationException(
                    "DoorOpener_Idle right-hand follower is missing.");
            behaviour.RefreshPreview();
            if (behaviour.Holder == null || behaviour.Model == null ||
                behaviour.RightHand == null)
                throw new InvalidOperationException("Door opener preview instance is missing.");

            Vector3 desiredAntenna = DesiredAntennaDirection(target.transform);
            float antennaAngle = Vector3.Angle(
                behaviour.Model.TransformDirection(behaviour.AntennaLocalAxis).normalized,
                desiredAntenna);
            float antennaElevation = Vector3.SignedAngle(
                Vector3.ProjectOnPlane(
                    behaviour.Model.TransformDirection(behaviour.AntennaLocalAxis),
                    target.transform.up).normalized,
                behaviour.Model.TransformDirection(behaviour.AntennaLocalAxis).normalized,
                -target.transform.right.normalized);
            float followPositionError = Vector3.Distance(
                behaviour.Holder.transform.position, behaviour.ExpectedWorldPosition());
            float followRotationError = Quaternion.Angle(
                behaviour.Holder.transform.rotation, behaviour.ExpectedWorldRotation());
            Bounds localBounds = CalculateLocalBounds(behaviour.DevicePrefab);
            ModelAxisInfo axes = new ModelAxisInfo(
                behaviour.AntennaLocalAxis,
                behaviour.FrontLocalAxis,
                Vector3.Cross(behaviour.AntennaLocalAxis,
                    behaviour.FrontLocalAxis).normalized,
                AxisLength(localBounds, behaviour.AntennaLocalAxis),
                AxisLength(localBounds, behaviour.FrontLocalAxis));
            Vector3 palmInModel = behaviour.Model.InverseTransformPoint(
                RightPalmCenter(target.transform));
            float gripHeightFraction = AxisFractionFromMinimum(
                localBounds, palmInModel, axes.Antenna);
            float gripFrontInsetFraction = AxisFractionFromMaximum(
                localBounds, palmInModel, axes.Front);
            Bounds worldBounds = CalculateWorldBounds(behaviour.Holder);
            float palmToModelDistance = Vector3.Distance(
                RightPalmCenter(target.transform),
                worldBounds.ClosestPoint(RightPalmCenter(target.transform)));
            float foreArmPoseError = Quaternion.Angle(
                RequirePath(target.transform, RightForeArmPath).localRotation,
                behaviour.AuthoredForeArmRotation);
            float handPoseError = Quaternion.Angle(
                RequirePath(target.transform, RightHandPath).localRotation,
                behaviour.AuthoredHandRotation);
            float fingerPoseError = MaximumRightFingerPoseError(
                RequirePath(target.transform, RightHandPath), behaviour.GripPose);

            if (antennaAngle > RotationTolerance ||
                Mathf.Abs(antennaElevation - 45f) > 0.2f ||
                followPositionError > PositionTolerance ||
                followRotationError > RotationTolerance ||
                Mathf.Abs(gripHeightFraction - DesiredGripHeightFraction) > 0.01f ||
                Mathf.Abs(gripFrontInsetFraction - DesiredGripFrontInsetFraction) > 0.01f ||
                palmToModelDistance > 0.015f || foreArmPoseError > RotationTolerance ||
                handPoseError > RotationTolerance || fingerPoseError > RotationTolerance)
                throw new InvalidOperationException(
                    "DoorOpener_Idle inspection failed. antenna=" + Num(antennaAngle) +
                    ", elevation=" + Num(antennaElevation) +
                    ", followPosition=" + Num(followPositionError) +
                    ", followRotation=" + Num(followRotationError) +
                    ", gripHeight=" + Num(gripHeightFraction) +
                    ", gripFront=" + Num(gripFrontInsetFraction) +
                    ", palmDistance=" + Num(palmToModelDistance) +
                    ", foreArm=" + Num(foreArmPoseError) +
                    ", hand=" + Num(handPoseError) +
                    ", fingers=" + Num(fingerPoseError) + ".");

            RequireEqual(ComputeFileHash(SourceAbsolutePath()),
                ComputeAssetHash(ModelAssetPath), "Door opener source model hash");
            AssertMaterialPipelineComplete(behaviour.DevicePrefab);
            var report = new StringBuilder()
                .AppendLine("DoorOpener_Idle read-only carry inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("sceneDirty=" + scene.isDirty)
                .AppendLine("sourceModelChanged=False")
                .AppendLine("sourceAndImportedBytesEqual=True")
                .AppendLine("rightHandFollowConfigured=True")
                .AppendLine("rightFingerRigApplied=True")
                .AppendLine("antennaForwardUp45=True")
                .AppendLine("antennaDirectionErrorDegrees=" + Num(antennaAngle))
                .AppendLine("antennaElevationDegrees=" + Num(antennaElevation))
                .AppendLine("followPositionError=" + Num(followPositionError))
                .AppendLine("followRotationErrorDegrees=" + Num(followRotationError))
                .AppendLine("gripHeightFraction=" + Num(gripHeightFraction))
                .AppendLine("gripFrontInsetFraction=" + Num(gripFrontInsetFraction))
                .AppendLine("palmToModelBoundsDistance=" + Num(palmToModelDistance))
                .AppendLine("rightForeArmPoseErrorDegrees=" + Num(foreArmPoseError))
                .AppendLine("rightHandPoseErrorDegrees=" + Num(handPoseError))
                .AppendLine("rightFingerPoseErrorDegrees=" + Num(fingerPoseError))
                .AppendLine("torsoHeadLowerBodyLeftArmChanged=False")
                .AppendLine("embeddedTextureCount=4")
                .AppendLine("externalMaterialApplied=True");
            WriteText(InspectReportPath, report.ToString());
            RequireNoUnityConsoleErrors();
            Debug.Log("[DoorOpenerIdle] Read-only carry inspection passed.");
        }

        internal static void CaptureFinal()
        {
            RequireEditMode();
            InspectCarry();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            DoorOpenerRightHandFollowBehaviour behaviour =
                target.GetComponent<DoorOpenerRightHandFollowBehaviour>() ??
                throw new InvalidOperationException("Door opener follower is missing.");
            behaviour.RefreshPreview();

            Dictionary<Transform, int> originalLayers = target
                .GetComponentsInChildren<Transform>(true)
                .ToDictionary(item => item, item => item.gameObject.layer);
            foreach (Transform item in originalLayers.Keys) item.gameObject.layer = 31;
            Texture2D[] panels = new Texture2D[6];
            Texture2D composite = null;
            try
            {
                Bounds fullBounds = CalculateWorldBounds(target);
                Bounds gripBounds = CalculateWorldBounds(behaviour.Holder);
                gripBounds.Encapsulate(RequirePath(target.transform, RightArmPath).position);
                gripBounds.Encapsulate(RequirePath(target.transform, RightForeArmPath).position);
                gripBounds.Encapsulate(RequirePath(target.transform, RightHandPath).position);
                gripBounds.Expand(0.12f);
                Vector3 front = target.transform.forward.normalized;
                Vector3 side = target.transform.right.normalized;
                Vector3 threeQuarter = (front + side * 0.7f).normalized;
                Vector3 gripOpposite = (front - side * 0.65f).normalized;
                panels[0] = CaptureView(fullBounds, front, target.transform.up);
                panels[1] = CaptureView(fullBounds, side, target.transform.up);
                panels[2] = CaptureView(fullBounds, threeQuarter, target.transform.up);
                panels[3] = CaptureView(gripBounds, front, target.transform.up);
                panels[4] = CaptureView(gripBounds, side, target.transform.up);
                panels[5] = CaptureView(gripBounds, gripOpposite, target.transform.up);
                composite = new Texture2D(1536, 1024, TextureFormat.RGB24, false);
                for (int index = 0; index < panels.Length; index++)
                {
                    int column = index % 3;
                    int row = 1 - index / 3;
                    composite.SetPixels(
                        column * 512, row * 512, 512, 512, panels[index].GetPixels());
                }
                composite.Apply(false, false);
                WriteBytes(FinalImagePath, composite.EncodeToPNG());
            }
            finally
            {
                foreach (KeyValuePair<Transform, int> entry in originalLayers)
                    if (entry.Key != null) entry.Key.gameObject.layer = entry.Value;
                foreach (Texture2D panel in panels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
                if (composite != null) UnityEngine.Object.DestroyImmediate(composite);
            }

            Vector3 antenna = behaviour.Model.TransformDirection(
                behaviour.AntennaLocalAxis).normalized;
            float antennaAngle = Vector3.Angle(
                antenna, DesiredAntennaDirection(target.transform));
            WriteText(FinalReportPath,
                "DoorOpener_Idle final direct visual review" + Environment.NewLine +
                "captureCount=1" + Environment.NewLine +
                "panelOrder=fullFront,fullPlayerRight,fullThreeQuarter,gripFront,gripPlayerRight,gripFrontLeft" + Environment.NewLine +
                "actualTargetRendered=True" + Environment.NewLine +
                "deviceLowerFrontGrip=True" + Environment.NewLine +
                "rightFingerRigApplied=True" + Environment.NewLine +
                "antennaForwardUp45=True" + Environment.NewLine +
                "antennaDirectionErrorDegrees=" + Num(antennaAngle) + Environment.NewLine +
                "rightHandFollowConfigured=True" + Environment.NewLine +
                "embeddedTexturesAndMaterialApplied=True" + Environment.NewLine +
                "torsoHeadLowerBodyLeftArmChanged=False" + Environment.NewLine);
            RequireNoUnityConsoleErrors();
            Debug.Log("[DoorOpenerIdle] Final direct-review composite captured once.");
        }

        internal static void InspectLocomotionSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            DoorOpenerRightHandFollowBehaviour carry = RequireCarry(target);
            SourceMotions sources = RequireSourceMotions(scene);
            var report = new StringBuilder()
                .AppendLine("DoorOpener_Idle locomotion source inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("sourceOrder=" + string.Join(",", SourceNames))
                .AppendLine("targetRightHandFollowPresent=True")
                .AppendLine("targetDevicePrefab=" +
                    AssetDatabase.GetAssetPath(carry.DevicePrefab));
            foreach (string sourceName in SourceNames)
            {
                Animator sourceAnimator = RequireAnimator(FindUnique(scene, sourceName));
                AnimatorController controller = RequireAnimatorController(
                    sourceAnimator, sourceName);
                AnimatorState state = controller.layers[0].stateMachine.defaultState ??
                    throw new InvalidOperationException(sourceName + " has no default state.");
                report.AppendLine("source=" + sourceName)
                    .AppendLine("controllerPath=" + AssetDatabase.GetAssetPath(controller))
                    .AppendLine("controllerSha256=" +
                        ComputeAssetHash(AssetDatabase.GetAssetPath(controller)))
                    .AppendLine("state=" + state.name)
                    .AppendLine("stateSpeed=" + Num(state.speed))
                    .AppendLine("stateMirror=" + state.mirror)
                    .Append(DescribeMotion(state.motion, "motion"));
            }
            report.AppendLine("diagonalBlendType=" + sources.Diagonal.blendType)
                .AppendLine("diagonalChildCount=" + sources.Diagonal.children.Length)
                .AppendLine("diagonalDefaultBlend=0.5")
                .AppendLine("sceneDirty=" + scene.isDirty);
            WriteText(LocomotionSourceReportPath, report.ToString());
            RequireNoUnityConsoleErrors();
            Debug.Log("[DoorOpenerIdleLocomotion] Sources inspected read-only.");
        }

        internal static void ApplyLocomotion()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            if (scene.isDirty)
                throw new InvalidOperationException(
                    "DoorOpener_Idle locomotion requires a clean CargoRunMvp scene so " +
                    "unrelated user edits are not saved.");
            GameObject target = FindUnique(scene, TargetName);
            DoorOpenerRightHandFollowBehaviour carry = RequireCarry(target);
            carry.RefreshPreview();
            string outsideBefore = SceneOutsideTargetSignature(scene, target);
            string carryBefore = CarryConfigurationSignatureWithoutLocomotion(carry);
            string importedModelHash = ComputeAssetHash(ModelAssetPath);
            string materialHash = ComputeAssetHash(MaterialAssetPath);
            Dictionary<string, string> textureHashes = TexturePaths().ToDictionary(
                path => path, ComputeAssetHash, StringComparer.Ordinal);
            Dictionary<string, string> sourceAssetHashes = SourceAssetPaths(scene)
                .ToDictionary(path => path, ComputeAssetHash, StringComparer.Ordinal);
            Dictionary<string, string> sourceObjectHashes = SourceNames.ToDictionary(
                name => name,
                name => TransformHierarchyHash(FindUnique(scene, name).transform),
                StringComparer.Ordinal);
            List<DoorOpenerBoneRotation> authoredPose = CapturePose(
                target.transform, RightArmPosePaths);

            SourceMotions sources = RequireSourceMotions(scene);
            GameObject idleSource = FindUnique(scene, SourceNames[0]);
            Animator idleSourceAnimator = RequireAnimator(idleSource);
            if (idleSourceAnimator.avatar == null)
                throw new InvalidOperationException("Player_Idle avatar is missing.");
            List<DoorOpenerBoneRotation> sourcePose = CapturePose(
                idleSource.transform, RightArmPosePaths);

            EnsureAssetFolder("Assets/_Project/Animation/DoorOpener");
            AnimationClip idle = CopyClip(
                sources.Idle, IdleClipPath, "DoorOpenerIdle_Idle");
            AnimationClip forward = CopyClip(
                sources.Forward, ForwardClipPath, "DoorOpenerIdle_WalkForward");
            AnimationClip backward = CopyClip(
                sources.Backward, BackwardClipPath, "DoorOpenerIdle_WalkBackward");
            AnimationClip sidestep = CopyClip(
                sources.Sidestep, SidestepClipPath, "DoorOpenerIdle_Sidestep");
            AnimationClip run = CopyClip(
                sources.Run, RunClipPath, "DoorOpenerIdle_RunForward");
            AnimatorController controller = CreateLocomotionController(
                sources.Diagonal, idle, forward, backward, sidestep, run);

            Animator animator = target.GetComponent<Animator>() ??
                Undo.AddComponent<Animator>(target);
            Undo.RecordObject(animator, "Configure DoorOpener_Idle locomotion");
            animator.avatar = idleSourceAnimator.avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EditorUtility.SetDirty(animator);
            Undo.RecordObject(carry, "Configure DoorOpener_Idle locomotion carry sway");
            carry.ConfigureLocomotionPose(authoredPose, sourcePose);
            EditorUtility.SetDirty(carry);
            AssetDatabase.SaveAssets();

            RequireCopiedClip(sources.Idle, idle, "Idle");
            RequireCopiedClip(sources.Forward, forward, "WalkForward");
            RequireCopiedClip(sources.Backward, backward, "WalkBackward");
            RequireCopiedClip(sources.Sidestep, sidestep, "Sidestep");
            RequireCopiedClip(sources.Run, run, "RunForward");
            RequireEqual(outsideBefore, SceneOutsideTargetSignature(scene, target),
                "scene objects outside DoorOpener_Idle");
            RequireEqual(carryBefore, CarryConfigurationSignatureWithoutLocomotion(carry),
                "DoorOpener_Idle carry configuration");
            RequireEqual(importedModelHash, ComputeAssetHash(ModelAssetPath),
                "Door opener model");
            RequireEqual(materialHash, ComputeAssetHash(MaterialAssetPath),
                "Door opener material");
            RequireHashes(textureHashes);
            RequireHashes(sourceAssetHashes);
            RequireSourceObjects(sourceObjectHashes, scene);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            WriteSourceBaseline(sourceAssetHashes, sourceObjectHashes);
            WriteText(LocomotionCarryBaselinePath, CarryConfigurationSignature(carry));
            var report = new StringBuilder()
                .AppendLine("DoorOpener_Idle locomotion application")
                .AppendLine("sceneSaved=True")
                .AppendLine("sourceObjectsChanged=False")
                .AppendLine("sourceAnimationAssetsChanged=False")
                .AppendLine("sourceCurvesGenerated=False")
                .AppendLine("originalClipsRetimed=False")
                .AppendLine("currentCarryTransformChanged=False")
                .AppendLine("deviceMeshMaterialTextureChanged=False")
                .AppendLine("secondsPerMotion=1")
                .AppendLine("totalCycleSeconds=6")
                .AppendLine("sequence=Idle,WalkForward,WalkBackward,Sidestep,WalkDiagonal,RunForward")
                .AppendLine("sequenceLoopsAfterRun=True")
                .AppendLine("controller=" + LocomotionControllerPath)
                .AppendLine("blendTreeType=FreeformCartesian2D")
                .AppendLine("blendTreeChildCount=6")
                .AppendLine("diagonalSourceTreeCopiedExactly=True")
                .AppendLine("upperBodySourceMotionEnabled=True")
                .AppendLine("rightShoulderSourceMotionWeight=1")
                .AppendLine("rightArmMotionWeight=" +
                    Num(DoorOpenerRightHandFollowBehaviour.RightArmMotionWeight))
                .AppendLine("rightForeArmMotionWeight=" +
                    Num(DoorOpenerRightHandFollowBehaviour.RightForeArmMotionWeight))
                .AppendLine("rightHandMotionWeight=" +
                    Num(DoorOpenerRightHandFollowBehaviour.RightHandMotionWeight))
                .AppendLine("rightFingerGripLocked=True")
                .AppendLine("deviceFollowsAnimatedRightHand=True");
            WriteText(LocomotionApplicationReportPath, report.ToString());
            RequireNoUnityConsoleErrors();
            Debug.Log("[DoorOpenerIdleLocomotion] Exact source locomotion applied and saved.");
        }

        internal static void InspectLocomotion()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            DoorOpenerRightHandFollowBehaviour carry = RequireCarry(target);
            Animator animator = RequireAnimator(target);
            AnimatorController controller = RequireAnimatorController(animator, TargetName);
            RequireEqual(LocomotionControllerPath,
                AssetDatabase.GetAssetPath(controller), "controller path");
            if (animator.avatar == null || animator.applyRootMotion || !animator.enabled ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                throw new InvalidOperationException(
                    "DoorOpener_Idle Animator settings are invalid.");
            RequireFloatParameter(controller,
                DoorOpenerIdleLocomotionCycleBehaviour.MoveXParameter, 0f);
            RequireFloatParameter(controller,
                DoorOpenerIdleLocomotionCycleBehaviour.MoveYParameter, 0f);
            RequireFloatParameter(controller,
                DoorOpenerIdleLocomotionCycleBehaviour.DiagonalBlendParameter, 0.5f);
            if (controller.layers.Length != 1)
                throw new InvalidOperationException("Controller must have one layer.");
            AnimatorControllerLayer layer = controller.layers[0];
            if (layer.avatarMask != null || layer.blendingMode !=
                    AnimatorLayerBlendingMode.Override ||
                !Mathf.Approximately(layer.defaultWeight, 1f))
                throw new InvalidOperationException("Locomotion base layer is invalid.");
            AnimatorState state = layer.stateMachine.defaultState ??
                throw new InvalidOperationException("Locomotion default state is missing.");
            if (state.name != LocomotionStateName || state.writeDefaultValues ||
                !Mathf.Approximately(state.speed, 1f))
                throw new InvalidOperationException("Locomotion state is invalid.");
            if (state.behaviours.OfType<DoorOpenerIdleLocomotionCycleBehaviour>()
                    .Count() != 1)
                throw new InvalidOperationException(
                    "One one-second DoorOpener cycle behaviour is required.");
            BlendTree root = state.motion as BlendTree ??
                throw new InvalidOperationException("Root 2D Blend Tree is missing.");
            RequireRootTree(root);

            SourceMotions sources = RequireSourceMotions(scene);
            AnimationClip idle = RequireAsset<AnimationClip>(IdleClipPath);
            AnimationClip forward = RequireAsset<AnimationClip>(ForwardClipPath);
            AnimationClip backward = RequireAsset<AnimationClip>(BackwardClipPath);
            AnimationClip sidestep = RequireAsset<AnimationClip>(SidestepClipPath);
            AnimationClip run = RequireAsset<AnimationClip>(RunClipPath);
            RequireCopiedClip(sources.Idle, idle, "Idle");
            RequireCopiedClip(sources.Forward, forward, "WalkForward");
            RequireCopiedClip(sources.Backward, backward, "WalkBackward");
            RequireCopiedClip(sources.Sidestep, sidestep, "Sidestep");
            RequireCopiedClip(sources.Run, run, "RunForward");
            RequireMotionTreeEquivalent(
                sources.Diagonal,
                root.children[4].motion as BlendTree,
                new Dictionary<AnimationClip, AnimationClip>
                {
                    [sources.Forward] = forward,
                    [sources.Sidestep] = sidestep
                },
                "WalkDiagonal");
            RequireSourceBaseline(scene);
            RequireEqual(ReadText(LocomotionCarryBaselinePath),
                CarryConfigurationSignature(carry), "carry locomotion baseline");
            RequirePoseConfiguration(carry, target.transform,
                FindUnique(scene, SourceNames[0]).transform);
            RequireEqual(ComputeFileHash(SourceAbsolutePath()),
                ComputeAssetHash(ModelAssetPath), "Door opener source model hash");
            AssertMaterialPipelineComplete(carry.DevicePrefab);

            var report = new StringBuilder()
                .AppendLine("DoorOpener_Idle locomotion read-only inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("controllerPath=" + LocomotionControllerPath)
                .AppendLine("blendTreeType=FreeformCartesian2D")
                .AppendLine("blendTreeChildCount=" + root.children.Length)
                .AppendLine("motionOrder=Idle,WalkForward,WalkBackward,Sidestep,WalkDiagonal,RunForward")
                .AppendLine("secondsPerMotion=1")
                .AppendLine("totalCycleSeconds=6")
                .AppendLine("sequenceLoopsAfterRun=True")
                .AppendLine("sourceClipCurvesPreserved=True")
                .AppendLine("sourceClipEventsPreserved=True")
                .AppendLine("sourceClipLoopSettingsPreserved=True")
                .AppendLine("diagonalSourceTreePreserved=True")
                .AppendLine("sourceAnimationAssetsChanged=False")
                .AppendLine("sourceObjectsChanged=False")
                .AppendLine("currentCarryTransformChanged=False")
                .AppendLine("deviceMeshMaterialTextureChanged=False")
                .AppendLine("upperBodyNaturalSourceMotionEnabled=True")
                .AppendLine("rightShoulderNaturalSwayEnabled=True")
                .AppendLine("rightArmGripOffsetPreserved=True")
                .AppendLine("rightFingerGripLocked=True")
                .AppendLine("deviceFollowsAnimatedRightHand=True")
                .AppendLine("sceneDirty=" + scene.isDirty);
            WriteText(LocomotionInspectionReportPath, report.ToString());
            RequireNoUnityConsoleErrors();
            Debug.Log("[DoorOpenerIdleLocomotion] Read-only inspection passed.");
        }

        internal static void CaptureLocomotionFinal()
        {
            string finalAbsolutePath = Absolute(LocomotionFinalImagePath);
            if (File.Exists(finalAbsolutePath))
                throw new InvalidOperationException(
                    "DoorOpener locomotion final image already exists; a second final " +
                    "capture is not allowed.");
            if (EditorApplication.isPlaying)
                EditorApplication.isPlaying = false;
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Animator sourceAnimator = RequireAnimator(target);
            AnimatorController controller = RequireAnimatorController(
                sourceAnimator, TargetName);
            RequireEqual(LocomotionControllerPath,
                AssetDatabase.GetAssetPath(controller), "final controller path");

            GameObject clone = UnityEngine.Object.Instantiate(target);
            clone.name = "DoorOpener_Idle_FinalReviewClone";
            clone.hideFlags = HideFlags.HideAndDontSave;
            SetLayerRecursively(clone.transform, 31);
            Animator animator = clone.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "DoorOpener final review clone Animator is missing.");
            DoorOpenerRightHandFollowBehaviour carry =
                clone.GetComponent<DoorOpenerRightHandFollowBehaviour>() ??
                throw new InvalidOperationException(
                    "DoorOpener final review clone carry is missing.");
            var graph = PlayableGraph.Create("DoorOpenerIdle_FinalDirectReview");
            Texture2D[] fullPanels = new Texture2D[6];
            Texture2D[] gripPanels = new Texture2D[6];
            var observations = new List<string>();
            float maximumFollowPosition = 0f;
            float maximumFollowRotation = 0f;
            float maximumArmDeviation = 0f;
            float maximumForeArmDeviation = 0f;
            float maximumHandDeviation = 0f;
            float maximumFingerDeviation = 0f;
            float maximumSpineSway = 0f;
            float maximumShoulderSway = 0f;
            float maximumScaleDrift = 0f;
            Quaternion initialSpine = Quaternion.identity;
            Quaternion initialShoulder = Quaternion.identity;
            Vector3 initialArmScale = Vector3.one;
            Vector3 initialForeArmScale = Vector3.one;
            Vector3 initialHandScale = Vector3.one;
            try
            {
                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                DoorOpenerIdleLocomotionCycleBehaviour.SetValidationOverride(
                    animator, true);
                AnimatorControllerPlayable playable =
                    AnimatorControllerPlayable.Create(graph, controller);
                AnimationPlayableOutput output = AnimationPlayableOutput.Create(
                    graph, "DoorOpenerIdle_FinalOutput", animator);
                output.SetSourcePlayable(playable);
                graph.Play();
                graph.Evaluate(0f);

                for (int phase = 0; phase < 6; phase++)
                {
                    Vector2 position =
                        DoorOpenerIdleLocomotionCycleBehaviour.MotionPosition(phase);
                    playable.SetFloat(
                        DoorOpenerIdleLocomotionCycleBehaviour.MoveXParameter,
                        position.x);
                    playable.SetFloat(
                        DoorOpenerIdleLocomotionCycleBehaviour.MoveYParameter,
                        position.y);
                    playable.SetFloat(
                        DoorOpenerIdleLocomotionCycleBehaviour.DiagonalBlendParameter,
                        0.5f);
                    graph.Evaluate(0.48f);
                    carry.RefreshAnimatedPreviewForValidation();
                    RequireCarryInstance(carry);
                    RuntimePoseMetrics metrics = MeasureClonePose(clone.transform, carry);
                    if (phase == 0)
                    {
                        initialSpine = metrics.Spine;
                        initialShoulder = metrics.Shoulder;
                        initialArmScale = metrics.ArmScale;
                        initialForeArmScale = metrics.ForeArmScale;
                        initialHandScale = metrics.HandScale;
                    }
                    else
                    {
                        maximumSpineSway = Mathf.Max(maximumSpineSway,
                            Quaternion.Angle(initialSpine, metrics.Spine));
                        maximumShoulderSway = Mathf.Max(maximumShoulderSway,
                            Quaternion.Angle(initialShoulder, metrics.Shoulder));
                        maximumScaleDrift = Mathf.Max(maximumScaleDrift,
                            Vector3.Distance(initialArmScale, metrics.ArmScale),
                            Vector3.Distance(initialForeArmScale, metrics.ForeArmScale),
                            Vector3.Distance(initialHandScale, metrics.HandScale));
                    }
                    maximumFollowPosition = Mathf.Max(
                        maximumFollowPosition, metrics.FollowPosition);
                    maximumFollowRotation = Mathf.Max(
                        maximumFollowRotation, metrics.FollowRotation);
                    maximumArmDeviation = Mathf.Max(
                        maximumArmDeviation, metrics.ArmDeviation);
                    maximumForeArmDeviation = Mathf.Max(
                        maximumForeArmDeviation, metrics.ForeArmDeviation);
                    maximumHandDeviation = Mathf.Max(
                        maximumHandDeviation, metrics.HandDeviation);
                    maximumFingerDeviation = Mathf.Max(
                        maximumFingerDeviation, metrics.FingerDeviation);
                    fullPanels[phase] = CaptureCloneLocomotionPanel(
                        clone.transform, carry, false);
                    gripPanels[phase] = CaptureCloneLocomotionPanel(
                        clone.transform, carry, true);
                    DrawPanelBorder(fullPanels[phase],
                        DoorOpenerIdleLocomotionPlayModeCapture.PhaseColor(phase));
                    DrawPanelBorder(gripPanels[phase],
                        DoorOpenerIdleLocomotionPlayModeCapture.PhaseColor(phase));
                    observations.Add(
                        "panel=" + phase + "|motion=" +
                        DoorOpenerIdleLocomotionCycleBehaviour.MotionName(phase) +
                        "|sampleTimeInPhase=0.48" +
                        "|moveX=" + Num(position.x) +
                        "|moveY=" + Num(position.y) +
                        "|followPosition=" + Num(metrics.FollowPosition) +
                        "|followRotation=" + Num(metrics.FollowRotation) +
                        "|armDeviation=" + Num(metrics.ArmDeviation) +
                        "|foreArmDeviation=" + Num(metrics.ForeArmDeviation) +
                        "|handDeviation=" + Num(metrics.HandDeviation));
                    graph.Evaluate(0.52f);
                }

                RequireAtMost(maximumFollowPosition, 0.00005f,
                    "device right-hand follow position error");
                RequireAtMost(maximumFollowRotation, 0.05f,
                    "device right-hand follow rotation error");
                RequireAtMost(maximumArmDeviation, 15f,
                    "right arm carry deviation");
                RequireAtMost(maximumForeArmDeviation, 10f,
                    "right forearm carry deviation");
                RequireAtMost(maximumHandDeviation, 5f,
                    "right hand carry deviation");
                RequireAtMost(maximumFingerDeviation, 0.05f,
                    "right finger grip deviation");
                RequireAtMost(maximumScaleDrift, 0.0001f,
                    "right arm scale drift");
                if (Mathf.Max(maximumSpineSway, maximumShoulderSway) <= 0.1f)
                    throw new InvalidOperationException(
                        "Source upper-body locomotion sway was rigidly removed.");
                ComposeRuntimeLocomotionReview(fullPanels, gripPanels);
                var report = new StringBuilder()
                    .AppendLine("DoorOpener_Idle final direct animation review")
                    .AppendLine("captureKind=Final")
                    .AppendLine("captureCount=1")
                    .AppendLine("actualTargetCloneRendered=True")
                    .AppendLine("sourceAnimatorControllerEvaluated=True")
                    .AppendLine("targetSceneObjectManipulated=False")
                    .AppendLine("playModeTransitionUnavailable=True")
                    .AppendLine("secondsPerMotion=1")
                    .AppendLine("totalCycleSeconds=6")
                    .AppendLine("fullPanelsCaptured=6")
                    .AppendLine("gripPanelsCaptured=6")
                    .AppendLine("panelOrder=Idle,WalkForward,WalkBackward,Sidestep,WalkDiagonal,RunForward")
                    .AppendLine("sequenceLoopConfigured=True")
                    .AppendLine("upperBodyNaturalSwayPreserved=True")
                    .AppendLine("rightShoulderNaturalSwayPreserved=True")
                    .AppendLine("rightArmGripOffsetPreserved=True")
                    .AppendLine("rightFingerGripLocked=True")
                    .AppendLine("deviceFollowsAnimatedRightHand=True")
                    .AppendLine("maximumFollowPositionError=" +
                        Num(maximumFollowPosition))
                    .AppendLine("maximumFollowRotationErrorDegrees=" +
                        Num(maximumFollowRotation))
                    .AppendLine("maximumRightArmCarryDeviationDegrees=" +
                        Num(maximumArmDeviation))
                    .AppendLine("maximumRightForeArmCarryDeviationDegrees=" +
                        Num(maximumForeArmDeviation))
                    .AppendLine("maximumRightHandCarryDeviationDegrees=" +
                        Num(maximumHandDeviation))
                    .AppendLine("maximumRightFingerGripDeviationDegrees=" +
                        Num(maximumFingerDeviation))
                    .AppendLine("maximumSpineSwayDegrees=" + Num(maximumSpineSway))
                    .AppendLine("maximumRightShoulderSwayDegrees=" +
                        Num(maximumShoulderSway))
                    .AppendLine("maximumRightArmScaleDrift=" +
                        Num(maximumScaleDrift));
                foreach (string observation in observations)
                    report.AppendLine(observation);
                WriteText(LocomotionFinalReportPath, report.ToString());
                WriteLocomotionCompletion(
                    "passed", "Final controller-driven clone review completed.");
                RequireNoUnityConsoleErrors();
                Debug.Log(
                    "[DoorOpenerIdleLocomotion] Final direct-review composite captured once.");
            }
            catch (Exception exception)
            {
                WriteLocomotionCompletion(
                    "failed", exception.GetType().Name + ": " + exception.Message);
                throw;
            }
            finally
            {
                DoorOpenerIdleLocomotionCycleBehaviour.SetValidationOverride(
                    animator, false);
                if (graph.IsValid()) graph.Destroy();
                foreach (Texture2D panel in fullPanels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
                foreach (Texture2D panel in gripPanels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static RuntimePoseMetrics MeasureClonePose(
            Transform target,
            DoorOpenerRightHandFollowBehaviour carry)
        {
            Transform arm = RequirePath(target, RightArmPath);
            Transform foreArm = RequirePath(target, RightForeArmPath);
            Transform hand = RequirePath(target, RightHandPath);
            return new RuntimePoseMetrics(
                Vector3.Distance(carry.Holder.transform.position,
                    carry.ExpectedWorldPosition()),
                Quaternion.Angle(carry.Holder.transform.rotation,
                    carry.ExpectedWorldRotation()),
                PoseDeviation(carry.AuthoredRightArmPose, arm, target),
                PoseDeviation(carry.AuthoredRightArmPose, foreArm, target),
                PoseDeviation(carry.AuthoredRightArmPose, hand, target),
                MaximumRightFingerPoseError(hand, carry.GripPose),
                RequirePath(target, "Armature/Hips/Spine02").localRotation,
                RequirePath(target, RightShoulderPath).localRotation,
                arm.localScale,
                foreArm.localScale,
                hand.localScale);
        }

        private static Texture2D CaptureCloneLocomotionPanel(
            Transform target,
            DoorOpenerRightHandFollowBehaviour carry,
            bool gripCloseup)
        {
            Bounds bounds;
            if (gripCloseup)
            {
                bounds = CalculateWorldBounds(carry.Holder);
                bounds.Encapsulate(RequirePath(target, RightArmPath).position);
                bounds.Encapsulate(RequirePath(target, RightForeArmPath).position);
                bounds.Encapsulate(RequirePath(target, RightHandPath).position);
                bounds.Expand(0.14f);
            }
            else
            {
                bounds = CalculateWorldBounds(target.gameObject);
                bounds.Expand(0.12f);
            }
            Vector3 direction =
                (target.forward + target.right * 0.72f).normalized;
            return CaptureView(bounds, direction, target.up);
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                item.gameObject.layer = layer;
        }

        private static void DrawPanelBorder(Texture2D image, Color color)
        {
            const int width = 6;
            for (int y = 0; y < image.height; y++)
            for (int x = 0; x < image.width; x++)
                if (x < width || y < width || x >= image.width - width ||
                    y >= image.height - width)
                    image.SetPixel(x, y, color);
            image.Apply(false, false);
        }

        private static void RequireAtMost(float actual, float maximum, string label)
        {
            if (actual > maximum)
                throw new InvalidOperationException(
                    label + " exceeded. actual=" + Num(actual) +
                    " maximum=" + Num(maximum));
        }

        internal static void StartLocomotionFinalCapture(
            Action<string> complete,
            Action<Exception> fail)
        {
            try
            {
                if (!EditorApplication.isPlaying &&
                    EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.isPlaying = false;
                    DoorOpenerIdleLocomotionPlayModeCapture.Start(complete, fail);
                    return;
                }
                if (!EditorApplication.isPlaying)
                {
                    InspectLocomotion();
                    WriteText(LocomotionCompletionPath,
                        "status=running" + Environment.NewLine);
                }
                DoorOpenerIdleLocomotionPlayModeCapture.Start(complete, fail);
            }
            catch (Exception exception)
            {
                fail(exception);
            }
        }

        internal static void ResumeLocomotionFinalCapture(
            Action<string> complete,
            Action<Exception> fail)
        {
            DoorOpenerIdleLocomotionPlayModeCapture.Resume(complete, fail);
        }

        internal static string LocomotionFinalAbsolutePath =>
            Absolute(LocomotionFinalImagePath);

        internal static GameObject RequireRuntimeLocomotionTarget() =>
            FindUnique(RequireScene(), TargetName);

        internal static RuntimePoseMetrics MeasureRuntimeLocomotionPose()
        {
            GameObject target = RequireRuntimeLocomotionTarget();
            DoorOpenerRightHandFollowBehaviour carry = RequireCarry(target);
            RequireCarryInstance(carry);
            Transform arm = RequirePath(target.transform, RightArmPath);
            Transform foreArm = RequirePath(target.transform, RightForeArmPath);
            Transform hand = RequirePath(target.transform, RightHandPath);
            Transform spine = RequirePath(target.transform, "Armature/Hips/Spine02");
            Transform shoulder = RequirePath(target.transform, RightShoulderPath);
            return new RuntimePoseMetrics(
                Vector3.Distance(carry.Holder.transform.position,
                    carry.ExpectedWorldPosition()),
                Quaternion.Angle(carry.Holder.transform.rotation,
                    carry.ExpectedWorldRotation()),
                PoseDeviation(carry.AuthoredRightArmPose, arm, target.transform),
                PoseDeviation(carry.AuthoredRightArmPose, foreArm, target.transform),
                PoseDeviation(carry.AuthoredRightArmPose, hand, target.transform),
                MaximumRightFingerPoseError(hand, carry.GripPose),
                spine.localRotation,
                shoulder.localRotation,
                arm.localScale,
                foreArm.localScale,
                hand.localScale);
        }

        internal static Texture2D CaptureRuntimeLocomotionPanel(bool gripCloseup)
        {
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            DoorOpenerRightHandFollowBehaviour carry = RequireCarry(target);
            RequireCarryInstance(carry);
            Bounds bounds;
            if (gripCloseup)
            {
                bounds = CalculateWorldBounds(carry.Holder);
                bounds.Encapsulate(RequirePath(target.transform, RightArmPath).position);
                bounds.Encapsulate(RequirePath(target.transform, RightForeArmPath).position);
                bounds.Encapsulate(RequirePath(target.transform, RightHandPath).position);
                bounds.Expand(0.14f);
            }
            else
            {
                bounds = CalculateWorldBounds(target);
                bounds.Encapsulate(CalculateWorldBounds(carry.Holder));
                bounds.Expand(0.12f);
            }
            Vector3 direction =
                (target.transform.forward + target.transform.right * 0.72f).normalized;
            var cameraObject = new GameObject("DoorOpenerLocomotion_ReadOnlyCamera");
            var lightObject = new GameObject("DoorOpenerLocomotion_ReadOnlyLight");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            RenderTexture render = RenderTexture.GetTemporary(
                512, 512, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(
                    bounds.extents.y, Mathf.Max(bounds.extents.x, bounds.extents.z)) *
                    1.12f;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 12f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.025f, 0.03f, 0.04f, 1f);
                Vector3 cameraUp = Vector3.ProjectOnPlane(
                    target.transform.up, direction).normalized;
                camera.transform.SetPositionAndRotation(
                    bounds.center + direction * 5f,
                    Quaternion.LookRotation(-direction, cameraUp));
                camera.targetTexture = render;
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.35f;
                light.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
                camera.Render();
                RenderTexture.active = render;
                var image = new Texture2D(512, 512, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        internal static void ComposeRuntimeLocomotionReview(
            IReadOnlyList<Texture2D> fullPanels,
            IReadOnlyList<Texture2D> gripPanels)
        {
            if (fullPanels.Count != 6 || gripPanels.Count != 6)
                throw new InvalidOperationException(
                    "DoorOpener locomotion review requires six full and six grip panels.");
            var composite = new Texture2D(3072, 1024, TextureFormat.RGB24, false);
            try
            {
                for (int index = 0; index < 6; index++)
                {
                    composite.SetPixels(index * 512, 512, 512, 512,
                        fullPanels[index].GetPixels());
                    composite.SetPixels(index * 512, 0, 512, 512,
                        gripPanels[index].GetPixels());
                }
                composite.Apply(false, false);
                string destination = LocomotionFinalAbsolutePath;
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, composite.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(composite);
            }
        }

        internal static void WriteLocomotionFinalReport(string contents)
        {
            WriteText(LocomotionFinalReportPath, contents);
        }

        internal static void WriteLocomotionCompletion(string status, string detail)
        {
            WriteText(LocomotionCompletionPath,
                "status=" + status + Environment.NewLine +
                "detail=" + detail + Environment.NewLine);
        }

        internal static void AssertNoConsoleErrors() => RequireNoUnityConsoleErrors();

        private static SourceMotions RequireSourceMotions(Scene scene)
        {
            Motion[] motions = SourceNames.Select(name =>
            {
                AnimatorController controller = RequireAnimatorController(
                    RequireAnimator(FindUnique(scene, name)), name);
                return controller.layers[0].stateMachine.defaultState?.motion ??
                    throw new InvalidOperationException(
                        name + " default motion is missing.");
            }).ToArray();
            if (!(motions[0] is AnimationClip idle) ||
                !(motions[1] is AnimationClip forward) ||
                !(motions[2] is AnimationClip backward) ||
                !(motions[3] is AnimationClip sidestep) ||
                !(motions[4] is BlendTree diagonal) ||
                !(motions[5] is AnimationClip run))
                throw new InvalidOperationException("Source motion types changed.");
            if (diagonal.blendType != BlendTreeType.Simple1D ||
                diagonal.children.Length != 2)
                throw new InvalidOperationException("Diagonal source tree changed.");
            AnimatorController diagonalController = RequireAnimatorController(
                RequireAnimator(FindUnique(scene, SourceNames[4])), SourceNames[4]);
            AnimatorControllerParameter parameter = diagonalController.parameters
                .SingleOrDefault(item => item.name == diagonal.blendParameter);
            if (parameter == null || parameter.type !=
                    AnimatorControllerParameterType.Float ||
                !Mathf.Approximately(parameter.defaultFloat, 0.5f))
                throw new InvalidOperationException(
                    "Diagonal source default blend is not 0.5.");
            if (!(diagonal.children[0].motion is AnimationClip) ||
                !(diagonal.children[1].motion is AnimationClip))
                throw new InvalidOperationException(
                    "Diagonal source children must remain animation clips.");
            return new SourceMotions(
                idle, forward, backward, sidestep, diagonal, run);
        }

        private static AnimationClip CopyClip(
            AnimationClip source,
            string path,
            string name)
        {
            AnimationClip copy = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (copy == null)
            {
                copy = new AnimationClip();
                EditorUtility.CopySerialized(source, copy);
                copy.name = name;
                AssetDatabase.CreateAsset(copy, path);
            }
            else
            {
                EditorUtility.CopySerialized(source, copy);
                copy.name = name;
                EditorUtility.SetDirty(copy);
            }
            return copy;
        }

        private static AnimatorController CreateLocomotionController(
            BlendTree sourceDiagonal,
            AnimationClip idle,
            AnimationClip forward,
            AnimationClip backward,
            AnimationClip sidestep,
            AnimationClip run)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    LocomotionControllerPath) != null &&
                !AssetDatabase.DeleteAsset(LocomotionControllerPath))
                throw new InvalidOperationException(
                    "Existing DoorOpener locomotion controller could not be replaced.");
            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(
                    LocomotionControllerPath);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            controller.AddParameter(
                DoorOpenerIdleLocomotionCycleBehaviour.MoveXParameter,
                AnimatorControllerParameterType.Float);
            controller.AddParameter(
                DoorOpenerIdleLocomotionCycleBehaviour.MoveYParameter,
                AnimatorControllerParameterType.Float);
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = DoorOpenerIdleLocomotionCycleBehaviour.DiagonalBlendParameter,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 0.5f
            });
            AnimatorControllerLayer layer = controller.layers[0];
            layer.name = "Source Locomotion";
            layer.defaultWeight = 1f;
            layer.blendingMode = AnimatorLayerBlendingMode.Override;
            layer.avatarMask = null;
            AnimatorStateMachine machine = layer.stateMachine;
            foreach (AnimatorState item in machine.states.Select(item => item.state).ToArray())
                machine.RemoveState(item);

            ChildMotion[] diagonalChildren = sourceDiagonal.children;
            var map = new Dictionary<AnimationClip, AnimationClip>
            {
                [(AnimationClip)diagonalChildren[0].motion] = forward,
                [(AnimationClip)diagonalChildren[1].motion] = sidestep
            };
            BlendTree diagonal = CloneTree(
                sourceDiagonal, DiagonalTreeName, controller, map);
            var root = new BlendTree
            {
                name = RootTreeName,
                blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter = DoorOpenerIdleLocomotionCycleBehaviour.MoveXParameter,
                blendParameterY = DoorOpenerIdleLocomotionCycleBehaviour.MoveYParameter,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(root, controller);
            root.children = new[]
            {
                Child(idle, RequiredPositions[0]),
                Child(forward, RequiredPositions[1]),
                Child(backward, RequiredPositions[2]),
                Child(sidestep, RequiredPositions[3]),
                Child(diagonal, RequiredPositions[4]),
                Child(run, RequiredPositions[5])
            };
            AnimatorState state = machine.AddState(LocomotionStateName);
            state.motion = root;
            state.speed = 1f;
            state.cycleOffset = 0f;
            state.mirror = false;
            state.writeDefaultValues = false;
            state.AddStateMachineBehaviour<DoorOpenerIdleLocomotionCycleBehaviour>();
            machine.defaultState = state;
            controller.layers = new[] { layer };
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(machine);
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(diagonal);
            return controller;
        }

        private static BlendTree CloneTree(
            BlendTree source,
            string name,
            AnimatorController owner,
            IReadOnlyDictionary<AnimationClip, AnimationClip> clipMap)
        {
            var copy = new BlendTree
            {
                name = name,
                blendType = source.blendType,
                blendParameter = source.blendParameter,
                blendParameterY = source.blendParameterY,
                useAutomaticThresholds = source.useAutomaticThresholds,
                minThreshold = source.minThreshold,
                maxThreshold = source.maxThreshold
            };
            AssetDatabase.AddObjectToAsset(copy, owner);
            ChildMotion[] sourceChildren = source.children;
            var children = new ChildMotion[sourceChildren.Length];
            for (int index = 0; index < sourceChildren.Length; index++)
            {
                ChildMotion child = sourceChildren[index];
                Motion motion;
                if (child.motion is AnimationClip sourceClip)
                {
                    if (!clipMap.TryGetValue(sourceClip, out AnimationClip mapped))
                        throw new InvalidOperationException(
                            "Diagonal source clip mapping is missing.");
                    motion = mapped;
                }
                else if (child.motion is BlendTree nested)
                    motion = CloneTree(nested, name + "_" + index, owner, clipMap);
                else
                    throw new InvalidOperationException(
                        "Unsupported diagonal source motion.");
                children[index] = new ChildMotion
                {
                    motion = motion,
                    threshold = child.threshold,
                    position = child.position,
                    timeScale = child.timeScale,
                    cycleOffset = child.cycleOffset,
                    mirror = child.mirror,
                    directBlendParameter = child.directBlendParameter
                };
            }
            copy.children = children;
            return copy;
        }

        private static ChildMotion Child(Motion motion, Vector2 position) =>
            new ChildMotion
            {
                motion = motion,
                position = position,
                timeScale = 1f,
                cycleOffset = 0f,
                mirror = false,
                threshold = 0f,
                directBlendParameter = string.Empty
            };

        private static void RequireRootTree(BlendTree tree)
        {
            if (tree.name != RootTreeName ||
                tree.blendType != BlendTreeType.FreeformCartesian2D ||
                tree.blendParameter !=
                    DoorOpenerIdleLocomotionCycleBehaviour.MoveXParameter ||
                tree.blendParameterY !=
                    DoorOpenerIdleLocomotionCycleBehaviour.MoveYParameter ||
                tree.children.Length != RequiredPositions.Length)
                throw new InvalidOperationException("Root 2D Blend Tree is invalid.");
            string[] paths =
            {
                IdleClipPath, ForwardClipPath, BackwardClipPath,
                SidestepClipPath, LocomotionControllerPath, RunClipPath
            };
            for (int index = 0; index < tree.children.Length; index++)
            {
                ChildMotion child = tree.children[index];
                if ((child.position - RequiredPositions[index]).sqrMagnitude >
                        0.00000001f ||
                    !Mathf.Approximately(child.timeScale, 1f) ||
                    !Mathf.Approximately(child.cycleOffset, 0f) || child.mirror)
                    throw new InvalidOperationException(
                        "Root Blend Tree child differs at " + index + ".");
                RequireEqual(paths[index], AssetDatabase.GetAssetPath(child.motion),
                    "root child path " + index);
            }
        }

        private static void RequireCopiedClip(
            AnimationClip source,
            AnimationClip copy,
            string label)
        {
            RequireEqual(ClipSignature(source), ClipSignature(copy),
                label + " clip content");
        }

        private static string ClipSignature(AnimationClip clip)
        {
            var result = new StringBuilder()
                .AppendLine("length=" + Num(clip.length))
                .AppendLine("frameRate=" + Num(clip.frameRate))
                .AppendLine("legacy=" + clip.legacy)
                .AppendLine("wrapMode=" + clip.wrapMode)
                .AppendLine("settings=" + EditorJsonUtility.ToJson(
                    AnimationUtility.GetAnimationClipSettings(clip)));
            foreach (EditorCurveBinding binding in AnimationUtility
                .GetCurveBindings(clip)
                .OrderBy(item => item.path, StringComparer.Ordinal)
                .ThenBy(item => item.type.FullName, StringComparer.Ordinal)
                .ThenBy(item => item.propertyName, StringComparer.Ordinal))
            {
                result.Append("curve|").Append(binding.path).Append('|')
                    .Append(binding.type.FullName).Append('|')
                    .AppendLine(binding.propertyName);
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                result.AppendLine("wrap=" + curve.preWrapMode + "," + curve.postWrapMode);
                foreach (Keyframe key in curve.keys)
                    result.AppendLine(string.Join(",", new[]
                    {
                        Num(key.time), Num(key.value), Num(key.inTangent),
                        Num(key.outTangent), Num(key.inWeight), Num(key.outWeight),
                        key.weightedMode.ToString()
                    }));
            }
            foreach (EditorCurveBinding binding in AnimationUtility
                .GetObjectReferenceCurveBindings(clip)
                .OrderBy(item => item.path, StringComparer.Ordinal)
                .ThenBy(item => item.type.FullName, StringComparer.Ordinal)
                .ThenBy(item => item.propertyName, StringComparer.Ordinal))
            {
                result.Append("objectCurve|").Append(binding.path).Append('|')
                    .Append(binding.type.FullName).Append('|')
                    .AppendLine(binding.propertyName);
                foreach (ObjectReferenceKeyframe key in AnimationUtility
                    .GetObjectReferenceCurve(clip, binding))
                    result.AppendLine(Num(key.time) + "|" + ObjectIdentity(key.value));
            }
            foreach (AnimationEvent item in AnimationUtility.GetAnimationEvents(clip))
                result.AppendLine("event|" + Num(item.time) + "|" +
                    item.functionName + "|" + item.stringParameter + "|" +
                    item.intParameter + "|" + Num(item.floatParameter) + "|" +
                    ObjectIdentity(item.objectReferenceParameter) + "|" +
                    item.messageOptions);
            return ComputeStringHash(result.ToString());
        }

        private static void RequireMotionTreeEquivalent(
            BlendTree source,
            BlendTree copy,
            IReadOnlyDictionary<AnimationClip, AnimationClip> clipMap,
            string label)
        {
            if (copy == null || source.blendType != copy.blendType ||
                source.blendParameter != copy.blendParameter ||
                source.blendParameterY != copy.blendParameterY ||
                source.useAutomaticThresholds != copy.useAutomaticThresholds ||
                !Mathf.Approximately(source.minThreshold, copy.minThreshold) ||
                !Mathf.Approximately(source.maxThreshold, copy.maxThreshold) ||
                source.children.Length != copy.children.Length)
                throw new InvalidOperationException(label + " tree differs.");
            ChildMotion[] a = source.children;
            ChildMotion[] b = copy.children;
            for (int index = 0; index < a.Length; index++)
            {
                if (!Mathf.Approximately(a[index].threshold, b[index].threshold) ||
                    (a[index].position - b[index].position).sqrMagnitude >
                        0.00000001f ||
                    !Mathf.Approximately(a[index].timeScale, b[index].timeScale) ||
                    !Mathf.Approximately(a[index].cycleOffset, b[index].cycleOffset) ||
                    a[index].mirror != b[index].mirror ||
                    a[index].directBlendParameter != b[index].directBlendParameter)
                    throw new InvalidOperationException(
                        label + " child differs at " + index + ".");
                if (a[index].motion is AnimationClip sourceClip)
                {
                    if (!(b[index].motion is AnimationClip copiedClip) ||
                        !clipMap.TryGetValue(sourceClip, out AnimationClip expected) ||
                        copiedClip != expected)
                        throw new InvalidOperationException(
                            label + " clip differs at " + index + ".");
                    RequireCopiedClip(sourceClip, copiedClip,
                        label + " child " + index);
                }
                else if (a[index].motion is BlendTree sourceTree)
                    RequireMotionTreeEquivalent(sourceTree,
                        b[index].motion as BlendTree, clipMap,
                        label + " child " + index);
                else
                    throw new InvalidOperationException(
                        label + " contains an unsupported motion.");
            }
        }

        private static List<DoorOpenerBoneRotation> CapturePose(
            Transform root,
            IEnumerable<string> paths) =>
            paths.Select(path => new DoorOpenerBoneRotation(
                path, RequirePath(root, path).localRotation)).ToList();

        private static void RequirePoseConfiguration(
            DoorOpenerRightHandFollowBehaviour carry,
            Transform target,
            Transform source)
        {
            if (carry.AuthoredRightArmPose.Count != RightArmPosePaths.Length ||
                carry.SourceRightArmPose.Count != RightArmPosePaths.Length)
                throw new InvalidOperationException(
                    "DoorOpener locomotion pose lists are incomplete.");
            for (int index = 0; index < RightArmPosePaths.Length; index++)
            {
                string path = RightArmPosePaths[index];
                DoorOpenerBoneRotation authored = carry.AuthoredRightArmPose[index];
                DoorOpenerBoneRotation sourcePose = carry.SourceRightArmPose[index];
                if (authored.Path != path || sourcePose.Path != path ||
                    Quaternion.Angle(authored.LocalRotation,
                        RequirePath(target, path).localRotation) > RotationTolerance ||
                    Quaternion.Angle(sourcePose.LocalRotation,
                        RequirePath(source, path).localRotation) > RotationTolerance)
                    throw new InvalidOperationException(
                        "DoorOpener locomotion pose differs at " + path + ".");
            }
        }

        private static float PoseDeviation(
            IReadOnlyList<DoorOpenerBoneRotation> pose,
            Transform bone,
            Transform root)
        {
            string path = TransformPath(bone, root);
            DoorOpenerBoneRotation authored = pose.Single(item => item.Path == path);
            return Quaternion.Angle(authored.LocalRotation, bone.localRotation);
        }

        private static string CarryConfigurationSignature(
            DoorOpenerRightHandFollowBehaviour carry)
        {
            var result = new StringBuilder(CarryConfigurationSignatureWithoutLocomotion(carry));
            foreach (DoorOpenerBoneRotation pose in carry.AuthoredRightArmPose)
                result.AppendLine("authoredPose=" + pose.Path + "|" +
                    Quat(pose.LocalRotation));
            foreach (DoorOpenerBoneRotation pose in carry.SourceRightArmPose)
                result.AppendLine("sourcePose=" + pose.Path + "|" +
                    Quat(pose.LocalRotation));
            return result.ToString();
        }

        private static string CarryConfigurationSignatureWithoutLocomotion(
            DoorOpenerRightHandFollowBehaviour carry) =>
            new StringBuilder()
                .AppendLine("prefab=" + ObjectIdentity(carry.DevicePrefab))
                .AppendLine("gripPose=" + ObjectIdentity(carry.GripPose))
                .AppendLine("foreArmPath=" + carry.RightForeArmPath)
                .AppendLine("handPath=" + carry.RightHandPath)
                .AppendLine("authoredForeArm=" + Quat(carry.AuthoredForeArmRotation))
                .AppendLine("authoredHand=" + Quat(carry.AuthoredHandRotation))
                .AppendLine("holderPosition=" + Vec(carry.HolderLocalPosition))
                .AppendLine("holderRotation=" + Quat(carry.HolderLocalRotation))
                .AppendLine("modelPosition=" + Vec(carry.ModelLocalPosition))
                .AppendLine("modelRotation=" + Quat(carry.ModelLocalRotation))
                .AppendLine("modelScale=" + Vec(carry.ModelLocalScale))
                .AppendLine("antennaAxis=" + Vec(carry.AntennaLocalAxis))
                .AppendLine("frontAxis=" + Vec(carry.FrontLocalAxis))
                .ToString();

        private static IEnumerable<string> SourceAssetPaths(Scene scene)
        {
            var paths = new HashSet<string>(StringComparer.Ordinal);
            foreach (string sourceName in SourceNames)
            {
                AnimatorController controller = RequireAnimatorController(
                    RequireAnimator(FindUnique(scene, sourceName)), sourceName);
                paths.Add(AssetDatabase.GetAssetPath(controller));
                AnimatorState state = controller.layers[0].stateMachine.defaultState;
                CollectMotionAssetPaths(state.motion, paths);
            }
            return paths.Where(path => !string.IsNullOrEmpty(path))
                .OrderBy(path => path, StringComparer.Ordinal);
        }

        private static void CollectMotionAssetPaths(Motion motion, ISet<string> paths)
        {
            string path = AssetDatabase.GetAssetPath(motion);
            if (!string.IsNullOrEmpty(path)) paths.Add(path);
            if (motion is BlendTree tree)
                foreach (ChildMotion child in tree.children)
                    CollectMotionAssetPaths(child.motion, paths);
        }

        private static void WriteSourceBaseline(
            IReadOnlyDictionary<string, string> assets,
            IReadOnlyDictionary<string, string> objects)
        {
            var report = new StringBuilder();
            foreach (KeyValuePair<string, string> item in assets)
                report.AppendLine("asset|" + item.Key + "|" + item.Value);
            foreach (KeyValuePair<string, string> item in objects)
                report.AppendLine("object|" + item.Key + "|" + item.Value);
            WriteText(LocomotionSourceBaselinePath, report.ToString());
        }

        private static void RequireSourceBaseline(Scene scene)
        {
            foreach (string line in ReadText(LocomotionSourceBaselinePath)
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] fields = line.Split('|');
                if (fields.Length != 3)
                    throw new InvalidOperationException(
                        "Invalid DoorOpener locomotion source baseline line.");
                string actual = fields[0] == "asset"
                    ? ComputeAssetHash(fields[1])
                    : fields[0] == "object"
                        ? TransformHierarchyHash(
                            FindUnique(scene, fields[1]).transform)
                        : throw new InvalidOperationException(
                            "Unknown source baseline kind: " + fields[0]);
                RequireEqual(fields[2], actual, fields[1]);
            }
        }

        private static void RequireHashes(
            IReadOnlyDictionary<string, string> hashes)
        {
            foreach (KeyValuePair<string, string> item in hashes)
                RequireEqual(item.Value, ComputeAssetHash(item.Key), item.Key);
        }

        private static void RequireSourceObjects(
            IReadOnlyDictionary<string, string> hashes,
            Scene scene)
        {
            foreach (KeyValuePair<string, string> item in hashes)
                RequireEqual(item.Value,
                    TransformHierarchyHash(FindUnique(scene, item.Key).transform),
                    item.Key);
        }

        private static string TransformHierarchyHash(Transform root)
        {
            var text = new StringBuilder();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                .Where(item => (item.gameObject.hideFlags & HideFlags.DontSaveInEditor) == 0)
                .OrderBy(item => TransformPath(item, root), StringComparer.Ordinal))
                AppendSingleTransformSignature(
                    text, TransformPath(item, root), item);
            return ComputeStringHash(text.ToString());
        }

        private static StringBuilder DescribeMotion(Motion motion, string key)
        {
            var result = new StringBuilder()
                .AppendLine(key + "Type=" + motion.GetType().Name)
                .AppendLine(key + "Name=" + motion.name)
                .AppendLine(key + "Path=" + AssetDatabase.GetAssetPath(motion));
            if (motion is AnimationClip clip)
                result.AppendLine(key + "Length=" + Num(clip.length))
                    .AppendLine(key + "FrameRate=" + Num(clip.frameRate))
                    .AppendLine(key + "Looping=" + clip.isLooping)
                    .AppendLine(key + "Events=" +
                        AnimationUtility.GetAnimationEvents(clip).Length)
                    .AppendLine(key + "FloatCurveCount=" +
                        AnimationUtility.GetCurveBindings(clip).Length)
                    .AppendLine(key + "Signature=" + ClipSignature(clip));
            else if (motion is BlendTree tree)
                result.AppendLine(key + "BlendType=" + tree.blendType)
                    .AppendLine(key + "ChildCount=" + tree.children.Length)
                    .AppendLine(key + "BlendParameter=" + tree.blendParameter);
            return result;
        }

        private static Animator RequireAnimator(GameObject root) =>
            root.GetComponent<Animator>() ??
            throw new InvalidOperationException(root.name + " Animator is missing.");

        private static AnimatorController RequireAnimatorController(
            Animator animator,
            string label) =>
            animator.runtimeAnimatorController as AnimatorController ??
            throw new InvalidOperationException(
                label + " does not use an AnimatorController.");

        private static DoorOpenerRightHandFollowBehaviour RequireCarry(GameObject target)
        {
            DoorOpenerRightHandFollowBehaviour carry =
                target.GetComponent<DoorOpenerRightHandFollowBehaviour>() ??
                throw new InvalidOperationException(
                    "DoorOpener_Idle right-hand follower is missing.");
            if (carry.DevicePrefab == null || carry.GripPose == null)
                throw new InvalidOperationException(
                    "DoorOpener_Idle carry configuration is incomplete.");
            return carry;
        }

        private static void RequireCarryInstance(
            DoorOpenerRightHandFollowBehaviour carry)
        {
            if (carry.Holder == null || carry.Model == null || carry.RightHand == null)
                throw new InvalidOperationException(
                    "DoorOpener_Idle runtime carry instance is incomplete.");
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ??
            throw new InvalidOperationException("Asset is missing: " + path);

        private static void RequireFloatParameter(
            AnimatorController controller,
            string name,
            float expected)
        {
            AnimatorControllerParameter parameter = controller.parameters
                .SingleOrDefault(item => item.name == name);
            if (parameter == null || parameter.type !=
                    AnimatorControllerParameterType.Float ||
                !Mathf.Approximately(parameter.defaultFloat, expected))
                throw new InvalidOperationException(
                    "Invalid float parameter: " + name);
        }

        private static string ObjectIdentity(UnityEngine.Object value)
        {
            if (value == null) return "null";
            return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                value, out string guid, out long localId)
                ? guid + ":" + localId
                : value.GetType().FullName + ":" + value.name;
        }

        private static string ReadText(string assetPath)
        {
            string path = Absolute(assetPath);
            if (!File.Exists(path))
                throw new FileNotFoundException("Required report is missing.", path);
            return File.ReadAllText(path, Encoding.UTF8);
        }

        private static void ImportEmbeddedAssets()
        {
            EnsureAssetFolder(TextureFolder);
            EnsureAssetFolder(MaterialFolder);
            ModelImporter importer = AssetImporter.GetAtPath(ModelAssetPath) as ModelImporter ??
                throw new InvalidOperationException("Door opener ModelImporter is missing.");
            importer.ExtractTextures(Absolute(TextureFolder));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureTextureImporter(BaseColorTexturePath, TextureImporterType.Default, true, false);
            ConfigureTextureImporter(NormalTexturePath, TextureImporterType.NormalMap, false, false);
            ConfigureTextureImporter(MetallicTexturePath, TextureImporterType.Default, false, true);
            ConfigureTextureImporter(RoughnessTexturePath, TextureImporterType.Default, false, true);
            WriteMetallicSmoothnessTexture();

            Material embedded = AssetDatabase.LoadAllAssetsAtPath(ModelAssetPath)
                .OfType<Material>().FirstOrDefault();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                throw new InvalidOperationException("URP Lit shader is unavailable.");
            Material external = AssetDatabase.LoadAssetAtPath<Material>(MaterialAssetPath);
            if (external == null)
            {
                external = embedded == null ? new Material(shader) : new Material(embedded);
                external.name = "TemporaryOpeningDevice";
                AssetDatabase.CreateAsset(external, MaterialAssetPath);
            }
            external.name = "TemporaryOpeningDevice";
            external.shader = shader;
            Texture baseColor = RequireTexture(BaseColorTexturePath);
            Texture normal = RequireTexture(NormalTexturePath);
            Texture metallicSmoothness = RequireTexture(MetallicSmoothnessTexturePath);
            if (external.HasProperty("_BaseMap")) external.SetTexture("_BaseMap", baseColor);
            if (external.HasProperty("_MainTex")) external.SetTexture("_MainTex", baseColor);
            if (external.HasProperty("_BaseColor")) external.SetColor("_BaseColor", Color.white);
            if (external.HasProperty("_Color")) external.SetColor("_Color", Color.white);
            if (external.HasProperty("_BumpMap"))
            {
                external.SetTexture("_BumpMap", normal);
                external.EnableKeyword("_NORMALMAP");
            }
            if (external.HasProperty("_MetallicGlossMap"))
            {
                external.SetTexture("_MetallicGlossMap", metallicSmoothness);
                if (external.HasProperty("_Metallic")) external.SetFloat("_Metallic", 1f);
                if (external.HasProperty("_Smoothness")) external.SetFloat("_Smoothness", 1f);
                external.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            EditorUtility.SetDirty(external);
            AssetDatabase.SaveAssets();

            importer = AssetImporter.GetAtPath(ModelAssetPath) as ModelImporter ??
                throw new InvalidOperationException("Door opener ModelImporter disappeared.");
            importer.materialSearch = ModelImporterMaterialSearch.Local;
            importer.AddRemap(
                new AssetImporter.SourceAssetIdentifier(
                    typeof(Material), embedded == null ? EmbeddedMaterialName : embedded.name),
                external);
            importer.SaveAndReimport();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssertMaterialPipelineComplete(RequireModelAsset());
        }

        private static void WriteMetallicSmoothnessTexture()
        {
            Texture2D metallic = LoadReadableTexture(MetallicTexturePath);
            Texture2D roughness = LoadReadableTexture(RoughnessTexturePath);
            try
            {
                if (metallic.width != roughness.width || metallic.height != roughness.height)
                    throw new InvalidOperationException(
                        "Door opener metallic and roughness texture sizes differ.");
                Color[] metallicPixels = metallic.GetPixels();
                Color[] roughnessPixels = roughness.GetPixels();
                for (int index = 0; index < metallicPixels.Length; index++)
                {
                    float metallicValue = metallicPixels[index].r;
                    float smoothness = 1f - roughnessPixels[index].r;
                    metallicPixels[index] = new Color(
                        metallicValue, metallicValue, metallicValue, smoothness);
                }
                var packed = new Texture2D(
                    metallic.width, metallic.height, TextureFormat.RGBA32, false, true);
                try
                {
                    packed.SetPixels(metallicPixels);
                    packed.Apply(false, false);
                    WriteBytes(MetallicSmoothnessTexturePath, packed.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(packed);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(metallic);
                UnityEngine.Object.DestroyImmediate(roughness);
            }
            ConfigureTextureImporter(
                MetallicSmoothnessTexturePath, TextureImporterType.Default, false, false);
        }

        private static Texture2D LoadReadableTexture(string assetPath)
        {
            byte[] bytes = File.ReadAllBytes(Absolute(assetPath));
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            if (!texture.LoadImage(bytes, false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException("Could not decode texture: " + assetPath);
            }
            return texture;
        }

        private static void ConfigureTextureImporter(
            string path,
            TextureImporterType type,
            bool sRgb,
            bool readable)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter ??
                throw new InvalidOperationException("Texture importer is missing: " + path);
            importer.textureType = type;
            importer.sRGBTexture = sRgb;
            importer.isReadable = readable;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.SaveAndReimport();
        }

        private static void AssertMaterialPipelineComplete(GameObject modelAsset)
        {
            string[] textures = TexturePaths();
            if (textures.Any(path => AssetDatabase.LoadAssetAtPath<Texture>(path) == null))
                throw new InvalidOperationException("One or more embedded textures are missing.");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialAssetPath) ??
                throw new InvalidOperationException("Door opener URP material is missing.");
            if (material.shader == null ||
                material.shader.name != "Universal Render Pipeline/Lit")
                throw new InvalidOperationException("Door opener material is not URP Lit.");
            if (material.GetTexture("_BaseMap") == null ||
                material.GetTexture("_BumpMap") == null ||
                material.GetTexture("_MetallicGlossMap") == null)
                throw new InvalidOperationException(
                    "Door opener PBR texture bindings are incomplete.");
            Renderer[] renderers = modelAsset.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0 || renderers.Any(renderer =>
                    renderer.sharedMaterials.Any(shared => shared != material)))
                throw new InvalidOperationException(
                    "Door opener renderers are not all mapped to the extracted material.");
        }

        private static void ApplyRightFingerPose(
            Transform hand,
            PlayerHandGripPose gripPose)
        {
            foreach (PlayerHandGripPose.JointPose joint in gripPose.Joints)
            {
                if (!joint.BoneName.StartsWith("Right", StringComparison.Ordinal)) continue;
                Transform bone = FindDescendant(hand, joint.BoneName) ??
                    throw new InvalidOperationException(
                        "Door opener grip bone is missing: " + joint.BoneName);
                bone.localRotation = joint.LocalRotation;
            }
        }

        private static float MaximumRightFingerPoseError(
            Transform hand,
            PlayerHandGripPose gripPose)
        {
            float maximum = 0f;
            foreach (PlayerHandGripPose.JointPose joint in gripPose.Joints)
            {
                if (!joint.BoneName.StartsWith("Right", StringComparison.Ordinal)) continue;
                Transform bone = FindDescendant(hand, joint.BoneName) ??
                    throw new InvalidOperationException(
                        "Door opener grip bone is missing: " + joint.BoneName);
                maximum = Mathf.Max(maximum,
                    Quaternion.Angle(bone.localRotation, joint.LocalRotation));
            }
            return maximum;
        }

        private static int RightGripJointCount(PlayerHandGripPose gripPose) =>
            gripPose.Joints.Count(item =>
                item.BoneName.StartsWith("Right", StringComparison.Ordinal));

        private static Vector3 RightPalmCenter(Transform targetRoot)
        {
            Transform hand = RequirePath(targetRoot, RightHandPath);
            string[] names =
            {
                "RightIndexProximal", "RightMiddleProximal",
                "RightRingProximal", "RightLittleProximal"
            };
            return names.Select(name => FindDescendant(hand, name)?.position ??
                    throw new InvalidOperationException(
                        "Door opener palm bone is missing: " + name))
                .Aggregate(Vector3.zero, (sum, point) => sum + point) / names.Length;
        }

        private static Vector3 RightPalmNormal(Transform targetRoot)
        {
            Transform hand = RequirePath(targetRoot, RightHandPath);
            Transform index = FindDescendant(hand, "RightIndexProximal") ??
                throw new InvalidOperationException("RightIndexProximal is missing.");
            Transform little = FindDescendant(hand, "RightLittleProximal") ??
                throw new InvalidOperationException("RightLittleProximal is missing.");
            Transform middle = FindDescendant(hand, "RightMiddleProximal") ??
                throw new InvalidOperationException("RightMiddleProximal is missing.");
            Vector3 width = (little.position - index.position).normalized;
            Vector3 finger = (middle.position - hand.position).normalized;
            Vector3 normal = Vector3.Cross(width, finger).normalized;
            if (normal.sqrMagnitude < 0.99f)
                throw new InvalidOperationException("Door opener right palm normal is degenerate.");
            return normal;
        }

        private static Vector3 DesiredAntennaDirection(Transform targetRoot) =>
            (targetRoot.forward * SqrtHalf + targetRoot.up * SqrtHalf).normalized;

        private static Texture2D CaptureView(
            Bounds bounds,
            Vector3 direction,
            Vector3 up)
        {
            var cameraObject = new GameObject("DoorOpener_FinalCamera");
            var lightObject = new GameObject("DoorOpener_FinalLight");
            var camera = cameraObject.AddComponent<Camera>();
            var light = lightObject.AddComponent<Light>();
            var render = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
            try
            {
                camera.cullingMask = 1 << 31;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.06f, 0.07f, 1f);
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y) * 1.18f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                float distance = Mathf.Max(bounds.size.magnitude * 1.5f, 1f);
                camera.transform.SetPositionAndRotation(
                    bounds.center + direction.normalized * distance,
                    Quaternion.LookRotation(-direction.normalized, up));
                camera.targetTexture = render;
                light.type = LightType.Directional;
                light.intensity = 1.45f;
                light.cullingMask = 1 << 31;
                light.transform.rotation = Quaternion.LookRotation(
                    -direction.normalized - up.normalized * 0.35f);
                RenderTexture previous = RenderTexture.active;
                camera.Render();
                RenderTexture.active = render;
                var image = new Texture2D(512, 512, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                image.Apply(false, false);
                RenderTexture.active = previous;
                return image;
            }
            finally
            {
                camera.targetTexture = null;
                render.Release();
                UnityEngine.Object.DestroyImmediate(render);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private readonly struct SourceMotions
        {
            internal SourceMotions(
                AnimationClip idle,
                AnimationClip forward,
                AnimationClip backward,
                AnimationClip sidestep,
                BlendTree diagonal,
                AnimationClip run)
            {
                Idle = idle;
                Forward = forward;
                Backward = backward;
                Sidestep = sidestep;
                Diagonal = diagonal;
                Run = run;
            }

            internal AnimationClip Idle { get; }
            internal AnimationClip Forward { get; }
            internal AnimationClip Backward { get; }
            internal AnimationClip Sidestep { get; }
            internal BlendTree Diagonal { get; }
            internal AnimationClip Run { get; }
        }

        internal readonly struct RuntimePoseMetrics
        {
            internal RuntimePoseMetrics(
                float followPosition,
                float followRotation,
                float armDeviation,
                float foreArmDeviation,
                float handDeviation,
                float fingerDeviation,
                Quaternion spine,
                Quaternion shoulder,
                Vector3 armScale,
                Vector3 foreArmScale,
                Vector3 handScale)
            {
                FollowPosition = followPosition;
                FollowRotation = followRotation;
                ArmDeviation = armDeviation;
                ForeArmDeviation = foreArmDeviation;
                HandDeviation = handDeviation;
                FingerDeviation = fingerDeviation;
                Spine = spine;
                Shoulder = shoulder;
                ArmScale = armScale;
                ForeArmScale = foreArmScale;
                HandScale = handScale;
            }

            internal float FollowPosition { get; }
            internal float FollowRotation { get; }
            internal float ArmDeviation { get; }
            internal float ForeArmDeviation { get; }
            internal float HandDeviation { get; }
            internal float FingerDeviation { get; }
            internal Quaternion Spine { get; }
            internal Quaternion Shoulder { get; }
            internal Vector3 ArmScale { get; }
            internal Vector3 ForeArmScale { get; }
            internal Vector3 HandScale { get; }
        }

        private readonly struct ModelAxisInfo
        {
            internal ModelAxisInfo(
                Vector3 antenna,
                Vector3 front,
                Vector3 width,
                float antennaLength,
                float frontDepth)
            {
                Antenna = antenna;
                Front = front;
                Width = width;
                AntennaLength = antennaLength;
                FrontDepth = frontDepth;
            }

            internal Vector3 Antenna { get; }
            internal Vector3 Front { get; }
            internal Vector3 Width { get; }
            internal float AntennaLength { get; }
            internal float FrontDepth { get; }
        }

        private static ModelAxisInfo DetermineModelAxes(Bounds bounds)
        {
            float[] sizes = { bounds.size.x, bounds.size.y, bounds.size.z };
            int antennaIndex = 0;
            if (sizes[1] > sizes[antennaIndex]) antennaIndex = 1;
            if (sizes[2] > sizes[antennaIndex]) antennaIndex = 2;
            int[] transverse = Enumerable.Range(0, 3)
                .Where(index => index != antennaIndex).ToArray();
            int frontIndex = sizes[transverse[0]] <= sizes[transverse[1]]
                ? transverse[0]
                : transverse[1];
            int widthIndex = transverse.First(index => index != frontIndex);
            float antennaSign = PreferredTipSign(bounds, antennaIndex);
            Vector3 antenna = CardinalAxis(antennaIndex) * antennaSign;
            Vector3 front = CardinalAxis(frontIndex);
            Vector3 width = CardinalAxis(widthIndex);
            if (Vector3.Dot(Vector3.Cross(front, antenna), width) < 0f)
                width = -width;
            return new ModelAxisInfo(
                antenna,
                front,
                width,
                AxisLength(bounds, antenna),
                AxisLength(bounds, front));
        }

        private static float PreferredTipSign(Bounds bounds, int axisIndex)
        {
            float minimum = AxisCoordinate(bounds.min, axisIndex);
            float maximum = AxisCoordinate(bounds.max, axisIndex);
            float length = maximum - minimum;
            bool minimumAtPivot = Mathf.Abs(minimum) <= length * 0.08f;
            bool maximumAtPivot = Mathf.Abs(maximum) <= length * 0.08f;
            if (minimumAtPivot && !maximumAtPivot) return 1f;
            if (maximumAtPivot && !minimumAtPivot) return -1f;
            return 1f;
        }

        private static Vector3 CardinalAxis(int index)
        {
            if (index == 0) return Vector3.right;
            if (index == 1) return Vector3.up;
            return Vector3.forward;
        }

        private static float AxisCoordinate(Vector3 value, int index)
        {
            if (index == 0) return value.x;
            if (index == 1) return value.y;
            return value.z;
        }

        private static float AxisLength(Bounds bounds, Vector3 axis)
        {
            Vector3 absolute = new Vector3(
                Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z));
            return 2f * Vector3.Dot(bounds.extents, absolute);
        }

        private static float AxisMinimum(Bounds bounds, Vector3 axis) =>
            Vector3.Dot(bounds.center, axis) - AxisLength(bounds, axis) * 0.5f;

        private static float AxisMaximum(Bounds bounds, Vector3 axis) =>
            Vector3.Dot(bounds.center, axis) + AxisLength(bounds, axis) * 0.5f;

        private static Vector3 PointAtAxisFractions(
            Bounds bounds,
            ModelAxisInfo axes,
            float heightFraction,
            float frontInsetFraction)
        {
            Vector3 point = bounds.center;
            float targetAntenna = Mathf.Lerp(
                AxisMinimum(bounds, axes.Antenna),
                AxisMaximum(bounds, axes.Antenna),
                heightFraction);
            point += axes.Antenna *
                (targetAntenna - Vector3.Dot(point, axes.Antenna));
            float targetFront = Mathf.Lerp(
                AxisMaximum(bounds, axes.Front),
                AxisMinimum(bounds, axes.Front),
                frontInsetFraction);
            point += axes.Front *
                (targetFront - Vector3.Dot(point, axes.Front));
            return point;
        }

        private static float AxisFractionFromMinimum(
            Bounds bounds,
            Vector3 point,
            Vector3 axis) =>
            Mathf.InverseLerp(
                AxisMinimum(bounds, axis),
                AxisMaximum(bounds, axis),
                Vector3.Dot(point, axis));

        private static float AxisFractionFromMaximum(
            Bounds bounds,
            Vector3 point,
            Vector3 axis) =>
            Mathf.InverseLerp(
                AxisMaximum(bounds, axis),
                AxisMinimum(bounds, axis),
                Vector3.Dot(point, axis));

        private static Bounds CalculateLocalBounds(GameObject root)
        {
            GameObject instance = UnityEngine.Object.Instantiate(root);
            instance.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                    throw new InvalidOperationException("Door opener model has no renderer.");
                bool initialized = false;
                Bounds localBounds = default;
                foreach (Renderer renderer in renderers)
                {
                    Bounds bounds = renderer.bounds;
                    Vector3 min = bounds.min;
                    Vector3 max = bounds.max;
                    for (int x = 0; x < 2; x++)
                    for (int y = 0; y < 2; y++)
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 world = new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z);
                        Vector3 local = instance.transform.InverseTransformPoint(world);
                        if (!initialized)
                        {
                            localBounds = new Bounds(local, Vector3.zero);
                            initialized = true;
                        }
                        else localBounds.Encapsulate(local);
                    }
                }
                return localBounds;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static Bounds CalculateWorldBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException(root.name + " has no renderer.");
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private static string SceneOutsideTargetSignature(Scene scene, GameObject target)
        {
            var builder = new StringBuilder();
            foreach (GameObject root in scene.GetRootGameObjects()
                         .Where(item => item != target)
                         .OrderBy(item => item.name, StringComparer.Ordinal)
                         .ThenBy(item => item.GetInstanceID()))
                AppendTransformSignature(builder, root.transform, root.transform);
            return builder.ToString();
        }

        private static string ProtectedTargetSignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                         .OrderBy(item => TransformPath(item, root), StringComparer.Ordinal))
            {
                string path = TransformPath(item, root);
                if (path == RightArmPath ||
                    path.StartsWith(RightArmPath + "/", StringComparison.Ordinal) ||
                    path.Contains("DoorOpener_Prop", StringComparison.Ordinal))
                    continue;
                AppendSingleTransformSignature(builder, path, item);
            }
            return builder.ToString();
        }

        private static void AppendTransformSignature(
            StringBuilder builder,
            Transform item,
            Transform root)
        {
            if ((item.gameObject.hideFlags & HideFlags.DontSaveInEditor) != 0) return;
            AppendSingleTransformSignature(builder, TransformPath(item, root), item);
            foreach (Transform child in item.Cast<Transform>()
                         .OrderBy(entry => entry.name, StringComparer.Ordinal)
                         .ThenBy(entry => entry.GetSiblingIndex()))
                AppendTransformSignature(builder, child, root);
        }

        private static void AppendSingleTransformSignature(
            StringBuilder builder,
            string path,
            Transform item)
        {
            builder.Append(path).Append('|')
                .Append(Vec(item.localPosition)).Append('|')
                .Append(Quat(item.localRotation)).Append('|')
                .Append(Vec(item.localScale)).Append('|')
                .Append(item.gameObject.activeSelf).AppendLine();
        }

        private static string ComputeStringHash(string value)
        {
            using var sha = SHA256.Create();
            return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(value))
                .Select(valueByte => valueByte.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static GameObject RequireModelAsset() =>
            AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath) ??
            throw new InvalidOperationException(
                "Temporary opening device FBX did not import as a model.");

        private static Texture RequireTexture(string assetPath) =>
            AssetDatabase.LoadAssetAtPath<Texture>(assetPath) ??
            throw new InvalidOperationException("Texture is missing: " + assetPath);

        private static string[] TexturePaths() => new[]
        {
            BaseColorTexturePath,
            NormalTexturePath,
            MetallicTexturePath,
            RoughnessTexturePath
        };

        private static void EnsureAssetFolder(string path)
        {
            string current = "Assets";
            foreach (string segment in path.Substring("Assets/".Length).Split('/'))
            {
                string next = current + "/" + segment;
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segment);
                current = next;
            }
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active scene; actual=" + scene.path);
            return scene;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Door opener setup and inspection require Edit Mode.");
        }

        private static GameObject FindUnique(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name)
                .Select(item => item.gameObject).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected one " + name + "; found " + matches.Length + ".");
            return matches[0];
        }

        private static Transform RequirePath(Transform root, string path) =>
            root.Find(path) ?? throw new InvalidOperationException(
                root.name + " path is missing: " + path);

        private static Transform FindDescendant(Transform root, string name)
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                if (item.name == name) return item;
            return null;
        }

        private static string TransformPath(Transform item, Transform root) =>
            item == root ? string.Empty : AnimationUtility.CalculateTransformPath(item, root);

        private static string SourceAbsolutePath() =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", SourceFileRelativePath));

        private static string Absolute(string assetPath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));

        private static string ComputeAssetHash(string assetPath) =>
            ComputeFileHash(Absolute(assetPath));

        private static string ComputeFileHash(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return string.Concat(sha.ComputeHash(stream)
                .Select(value => value.ToString("X2", CultureInfo.InvariantCulture)));
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " changed unexpectedly.");
        }

        private static void RequireNoUnityConsoleErrors()
        {
            Type logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll") ??
                throw new InvalidOperationException("Unity console API is unavailable.");
            System.Reflection.MethodInfo method = logEntriesType.GetMethod(
                "GetCountsByType",
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic) ??
                throw new InvalidOperationException(
                    "Unity console count API is unavailable.");
            object[] arguments = { 0, 0, 0 };
            method.Invoke(null, arguments);
            int errorCount = (int)arguments[0];
            if (errorCount != 0)
                throw new InvalidOperationException(
                    "Unity console contains " + errorCount + " error(s).");
        }

        private static void WriteText(string assetPath, string contents)
        {
            string absolute = Absolute(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, contents, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void WriteBytes(string assetPath, byte[] contents)
        {
            string absolute = Absolute(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllBytes(absolute, contents);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static string Num(float value) =>
            value.ToString("0.#########", CultureInfo.InvariantCulture);

        private static string Vec(Vector3 value) =>
            Num(value.x) + "," + Num(value.y) + "," + Num(value.z);

        private static string Quat(Quaternion value) =>
            Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + "," + Num(value.w);
    }

    [InitializeOnLoad]
    internal static class DoorOpenerIdleLocomotionPlayModeCapture
    {
        private const string StateKey =
            "Bellerophon.DoorOpenerIdleLocomotionCapture.State";
        private const int WaitingForPlayMode = 1;
        private const int Capturing = 2;
        private const int MotionCount = 6;
        private const float CapturePhaseTime = 0.48f;
        private const double TimeoutSeconds = 24d;
        private static readonly Color[] PhaseColors =
        {
            new Color(0.35f, 0.75f, 1f),
            new Color(0.3f, 0.9f, 0.45f),
            new Color(1f, 0.65f, 0.25f),
            new Color(0.9f, 0.35f, 0.85f),
            new Color(0.95f, 0.85f, 0.25f),
            new Color(1f, 0.3f, 0.3f)
        };

        private static bool active;
        private static Action<string> complete;
        private static Action<Exception> fail;
        private static double startTime;
        private static int baseAbsolutePhase = -1;
        private static int nextPanel;
        private static Animator animator;
        private static Texture2D[] fullPanels;
        private static Texture2D[] gripPanels;
        private static readonly List<string> Observations = new List<string>();
        private static bool swayBaselineSet;
        private static Quaternion initialSpine;
        private static Quaternion initialShoulder;
        private static Vector3 initialArmScale;
        private static Vector3 initialForeArmScale;
        private static Vector3 initialHandScale;
        private static float maximumSpineSway;
        private static float maximumShoulderSway;
        private static float maximumFollowPosition;
        private static float maximumFollowRotation;
        private static float maximumArmDeviation;
        private static float maximumForeArmDeviation;
        private static float maximumHandDeviation;
        private static float maximumFingerDeviation;
        private static float maximumScaleDrift;

        internal static Color PhaseColor(int phase) => PhaseColors[phase];

        static DoorOpenerIdleLocomotionPlayModeCapture()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        internal static void RestoreAfterDomainReload()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        internal static void Start(
            Action<string> completeCallback,
            Action<Exception> failCallback)
        {
            if (!EditorApplication.isPlaying)
            {
                complete = completeCallback;
                fail = failCallback;
                SessionState.SetInt(StateKey, WaitingForPlayMode);
                RequestPlayMode();
                return;
            }
            BeginRuntimeCapture(completeCallback, failCallback);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode && !active &&
                complete != null && fail != null)
                BeginRuntimeCapture(complete, fail);
        }

        private static void RequestPlayMode()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorApplication.EnterPlaymode();
            };
        }

        internal static void Resume(
            Action<string> completeCallback,
            Action<Exception> failCallback)
        {
            if (active)
            {
                complete = completeCallback;
                fail = failCallback;
                return;
            }
            complete = completeCallback;
            fail = failCallback;
            if (!EditorApplication.isPlaying)
            {
                SessionState.SetInt(StateKey, WaitingForPlayMode);
                RequestPlayMode();
                return;
            }
            BeginRuntimeCapture(completeCallback, failCallback);
        }

        private static void BeginRuntimeCapture(
            Action<string> completeCallback,
            Action<Exception> failCallback)
        {
            try
            {
                active = true;
                SessionState.SetInt(StateKey, Capturing);
                complete = completeCallback;
                fail = failCallback;
                startTime = EditorApplication.timeSinceStartup;
                baseAbsolutePhase = -1;
                nextPanel = 0;
                GameObject target =
                    DoorOpenerSetupTools.RequireRuntimeLocomotionTarget();
                animator = target.GetComponent<Animator>() ??
                    throw new InvalidOperationException(
                        "DoorOpener_Idle runtime Animator is missing.");
                if (!animator.enabled || animator.applyRootMotion ||
                    animator.runtimeAnimatorController == null)
                    throw new InvalidOperationException(
                        "DoorOpener_Idle runtime Animator settings are invalid.");
                fullPanels = new Texture2D[MotionCount];
                gripPanels = new Texture2D[MotionCount];
                Observations.Clear();
                ResetMetrics();
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private static void Tick()
        {
            if (complete == null && fail == null) return;
            try
            {
                if (!active && SessionState.GetInt(StateKey, 0) ==
                        WaitingForPlayMode)
                {
                    if (EditorApplication.isPlaying)
                        BeginRuntimeCapture(complete, fail);
                    else if (!EditorApplication.isPlayingOrWillChangePlaymode)
                        RequestPlayMode();
                    return;
                }
                if (!EditorApplication.isPlaying)
                    throw new InvalidOperationException(
                        "Play Mode ended before DoorOpener capture completed.");
                if (EditorApplication.timeSinceStartup - startTime > TimeoutSeconds)
                    throw new TimeoutException(
                        "DoorOpener natural locomotion capture exceeded 24 seconds.");
                if (!animator.isInitialized ||
                    !DoorOpenerIdleLocomotionCycleBehaviour.TryGetSequenceState(
                        animator, out int absolutePhase, out int phase,
                        out float phaseElapsed))
                    return;
                if (baseAbsolutePhase < 0)
                {
                    if (phase != 0 || phaseElapsed > 0.65f) return;
                    baseAbsolutePhase = absolutePhase;
                }
                if (nextPanel < MotionCount)
                {
                    int expectedAbsolutePhase = baseAbsolutePhase + nextPanel;
                    if (absolutePhase < expectedAbsolutePhase ||
                        phaseElapsed < CapturePhaseTime)
                        return;
                    if (absolutePhase > expectedAbsolutePhase || phase != nextPanel)
                        throw new InvalidOperationException(
                            "DoorOpener natural six-motion phase order changed.");
                    Vector2 expected =
                        DoorOpenerIdleLocomotionCycleBehaviour.MotionPosition(phase);
                    float moveX = animator.GetFloat(
                        DoorOpenerIdleLocomotionCycleBehaviour.MoveXParameter);
                    float moveY = animator.GetFloat(
                        DoorOpenerIdleLocomotionCycleBehaviour.MoveYParameter);
                    if (Mathf.Abs(moveX - expected.x) > 0.001f ||
                        Mathf.Abs(moveY - expected.y) > 0.001f)
                        throw new InvalidOperationException(
                            "DoorOpener Blend Tree parameters differ from the phase.");
                    DoorOpenerSetupTools.RuntimePoseMetrics metrics = AccumulateMetrics();
                    Texture2D full =
                        DoorOpenerSetupTools.CaptureRuntimeLocomotionPanel(false);
                    Texture2D grip =
                        DoorOpenerSetupTools.CaptureRuntimeLocomotionPanel(true);
                    DrawBorder(full, PhaseColors[phase]);
                    DrawBorder(grip, PhaseColors[phase]);
                    fullPanels[nextPanel] = full;
                    gripPanels[nextPanel] = grip;
                    Observations.Add(
                        "panel=" + nextPanel +
                        "|phase=" + phase +
                        "|motion=" +
                            DoorOpenerIdleLocomotionCycleBehaviour.MotionName(phase) +
                        "|phaseElapsed=" + F(phaseElapsed) +
                        "|moveX=" + F(moveX) +
                        "|moveY=" + F(moveY) +
                        "|followPosition=" + F(metrics.FollowPosition) +
                        "|followRotation=" + F(metrics.FollowRotation) +
                        "|armDeviation=" + F(metrics.ArmDeviation) +
                        "|foreArmDeviation=" + F(metrics.ForeArmDeviation) +
                        "|handDeviation=" + F(metrics.HandDeviation));
                    nextPanel++;
                    return;
                }
                if (absolutePhase < baseAbsolutePhase + MotionCount) return;
                if (absolutePhase != baseAbsolutePhase + MotionCount || phase != 0)
                    throw new InvalidOperationException(
                        "RunForward did not naturally return to Idle.");
                Finish();
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private static DoorOpenerSetupTools.RuntimePoseMetrics AccumulateMetrics()
        {
            DoorOpenerSetupTools.RuntimePoseMetrics metrics =
                DoorOpenerSetupTools.MeasureRuntimeLocomotionPose();
            maximumFollowPosition = Mathf.Max(
                maximumFollowPosition, metrics.FollowPosition);
            maximumFollowRotation = Mathf.Max(
                maximumFollowRotation, metrics.FollowRotation);
            maximumArmDeviation = Mathf.Max(
                maximumArmDeviation, metrics.ArmDeviation);
            maximumForeArmDeviation = Mathf.Max(
                maximumForeArmDeviation, metrics.ForeArmDeviation);
            maximumHandDeviation = Mathf.Max(
                maximumHandDeviation, metrics.HandDeviation);
            maximumFingerDeviation = Mathf.Max(
                maximumFingerDeviation, metrics.FingerDeviation);
            if (!swayBaselineSet)
            {
                swayBaselineSet = true;
                initialSpine = metrics.Spine;
                initialShoulder = metrics.Shoulder;
                initialArmScale = metrics.ArmScale;
                initialForeArmScale = metrics.ForeArmScale;
                initialHandScale = metrics.HandScale;
                return metrics;
            }
            maximumSpineSway = Mathf.Max(maximumSpineSway,
                Quaternion.Angle(initialSpine, metrics.Spine));
            maximumShoulderSway = Mathf.Max(maximumShoulderSway,
                Quaternion.Angle(initialShoulder, metrics.Shoulder));
            maximumScaleDrift = Mathf.Max(maximumScaleDrift,
                Vector3.Distance(initialArmScale, metrics.ArmScale),
                Vector3.Distance(initialForeArmScale, metrics.ForeArmScale),
                Vector3.Distance(initialHandScale, metrics.HandScale));
            return metrics;
        }

        private static void Finish()
        {
            RequireAtMost(maximumFollowPosition, 0.00005f,
                "device right-hand follow position error");
            RequireAtMost(maximumFollowRotation, 0.05f,
                "device right-hand follow rotation error");
            RequireAtMost(maximumArmDeviation, 15f,
                "right arm carry deviation");
            RequireAtMost(maximumForeArmDeviation, 10f,
                "right forearm carry deviation");
            RequireAtMost(maximumHandDeviation, 5f,
                "right hand carry deviation");
            RequireAtMost(maximumFingerDeviation, 0.05f,
                "right finger grip deviation");
            RequireAtMost(maximumScaleDrift, 0.0001f,
                "right arm scale drift");
            if (Mathf.Max(maximumSpineSway, maximumShoulderSway) <= 0.1f)
                throw new InvalidOperationException(
                    "Source upper-body locomotion sway was rigidly removed.");
            DoorOpenerSetupTools.ComposeRuntimeLocomotionReview(
                fullPanels, gripPanels);
            var report = new StringBuilder()
                .AppendLine("DoorOpener_Idle natural locomotion final capture")
                .AppendLine("captureKind=Final")
                .AppendLine("captureCount=1")
                .AppendLine("naturalPlayback=True")
                .AppendLine("targetAnimationTimeForced=False")
                .AppendLine("secondsPerMotion=1")
                .AppendLine("cyclesObserved=1")
                .AppendLine("fullPanelsCaptured=6")
                .AppendLine("gripPanelsCaptured=6")
                .AppendLine("panelOrder=Idle,WalkForward,WalkBackward,Sidestep,WalkDiagonal,RunForward")
                .AppendLine("returnedToIdleAfterRun=True")
                .AppendLine("upperBodyNaturalSwayPreserved=True")
                .AppendLine("rightShoulderNaturalSwayPreserved=True")
                .AppendLine("rightArmGripOffsetPreserved=True")
                .AppendLine("rightFingerGripLocked=True")
                .AppendLine("deviceFollowsAnimatedRightHand=True")
                .AppendLine("maximumFollowPositionError=" + F(maximumFollowPosition))
                .AppendLine("maximumFollowRotationErrorDegrees=" +
                    F(maximumFollowRotation))
                .AppendLine("maximumRightArmCarryDeviationDegrees=" +
                    F(maximumArmDeviation))
                .AppendLine("maximumRightForeArmCarryDeviationDegrees=" +
                    F(maximumForeArmDeviation))
                .AppendLine("maximumRightHandCarryDeviationDegrees=" +
                    F(maximumHandDeviation))
                .AppendLine("maximumRightFingerGripDeviationDegrees=" +
                    F(maximumFingerDeviation))
                .AppendLine("maximumSpineSwayDegrees=" + F(maximumSpineSway))
                .AppendLine("maximumRightShoulderSwayDegrees=" +
                    F(maximumShoulderSway))
                .AppendLine("maximumRightArmScaleDrift=" + F(maximumScaleDrift));
            foreach (string observation in Observations)
                report.AppendLine(observation);
            DoorOpenerSetupTools.WriteLocomotionFinalReport(report.ToString());
            DoorOpenerSetupTools.AssertNoConsoleErrors();
            DoorOpenerSetupTools.WriteLocomotionCompletion(
                "passed", "Natural six-motion playback returned to Idle.");
            Action<string> callback = complete;
            Cleanup();
            callback?.Invoke(
                "DoorOpener_Idle natural locomotion final capture completed.");
            EditorApplication.ExitPlaymode();
        }

        private static void Fail(Exception exception)
        {
            try
            {
                DoorOpenerSetupTools.WriteLocomotionCompletion(
                    "failed", exception.GetType().Name + ": " + exception.Message);
            }
            catch
            {
                // Preserve the original capture failure for the bridge response.
            }
            Action<Exception> callback = fail;
            Cleanup();
            callback?.Invoke(exception);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            if (fullPanels != null)
                foreach (Texture2D panel in fullPanels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            if (gripPanels != null)
                foreach (Texture2D panel in gripPanels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            active = false;
            SessionState.EraseInt(StateKey);
            complete = null;
            fail = null;
            animator = null;
            fullPanels = null;
            gripPanels = null;
            Observations.Clear();
        }

        private static void ResetMetrics()
        {
            swayBaselineSet = false;
            maximumSpineSway = 0f;
            maximumShoulderSway = 0f;
            maximumFollowPosition = 0f;
            maximumFollowRotation = 0f;
            maximumArmDeviation = 0f;
            maximumForeArmDeviation = 0f;
            maximumHandDeviation = 0f;
            maximumFingerDeviation = 0f;
            maximumScaleDrift = 0f;
        }

        private static void DrawBorder(Texture2D image, Color color)
        {
            const int width = 6;
            for (int y = 0; y < image.height; y++)
            for (int x = 0; x < image.width; x++)
                if (x < width || y < width || x >= image.width - width ||
                    y >= image.height - width)
                    image.SetPixel(x, y, color);
            image.Apply(false, false);
        }

        private static void RequireAtMost(float actual, float maximum, string label)
        {
            if (actual > maximum)
                throw new InvalidOperationException(
                    label + " exceeded. actual=" + F(actual) +
                    " maximum=" + F(maximum));
        }

        private static string F(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture);
    }
}
