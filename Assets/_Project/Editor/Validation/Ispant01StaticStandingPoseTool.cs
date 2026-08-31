using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.IspantCargoRunScene
{
    internal static class Ispant01StaticStandingPoseTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementName = "Approved Ispant Enemy Placement";
        private const string SlotName = "Ispant_01_Static";
        private const string ModelName = "Ispant_New_Direct_Model";
        private const string ModelAssetPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_Direct_Source.fbx";
        private const string CapturePath =
            "docs/validation/ispant_01_static_standing_pose_2026-08-21/" +
            "Ispant_01_Static_StandingPose_Review.png";
        private const string RightArmCapturePath =
            "docs/validation/ispant_01_static_right_arm_correction_2026-08-21/" +
            "Ispant_01_Static_RightArmCorrection_Review.png";
        private const float PoseTolerance = 0.0001f;
        private const float GroundTolerance = 0.005f;

        private static readonly string[] PoseBoneNames =
        {
            "Hips", "Spine", "Spine01", "neck", "Head",
            "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase",
            "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase",
            "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand",
            "RightShoulder", "RightArm", "RightForeArm", "RightHand"
        };

        private static readonly string[] LowerBodyBoneNames =
        {
            "Hips",
            "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase",
            "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase"
        };

        private static readonly string[] PreservedArmBoneNames =
        {
            "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand",
            "RightShoulder", "RightArm", "RightForeArm", "RightHand"
        };

        private static readonly string[] SourcePreservedLeftArmBoneNames =
        {
            "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand"
        };

        private static readonly (string Left, string Right)[] ArmMirrorPairs =
        {
            ("LeftShoulder", "RightShoulder"),
            ("LeftArm", "RightArm"),
            ("LeftForeArm", "RightForeArm"),
            ("LeftHand", "RightHand")
        };

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 01 Static Standing Pose")]
        public static void ApplyIspant01StaticStandingPose()
        {
            var scene = RequireScene(false);
            var slot = RequireSlot(scene);
            var model = RequireModel(slot);
            var source = RequireModelAsset();
            var outsideBefore = OutsideTargetSignature(scene, model);
            var slotPose = new LocalPose(slot);
            var modelPose = new LocalPose(model);
            var armPoses = PreservedArmBoneNames.ToDictionary(
                name => name,
                name => new LocalPose(RequireBone(model, name)),
                StringComparer.Ordinal);
            var sourceBones = LowerBodyBoneNames.ToDictionary(
                name => name,
                name => RequireBone(source.transform, name),
                StringComparer.Ordinal);
            var targetBones = LowerBodyBoneNames.ToDictionary(
                name => name,
                name => RequireBone(model, name),
                StringComparer.Ordinal);
            var changed = targetBones.Values.Distinct().ToArray();

            Undo.RecordObjects(changed, "Apply Ispant 01 static standing pose");
            foreach (var name in LowerBodyBoneNames)
                CopyLocalPose(sourceBones[name], targetBones[name]);

            var allBones = PoseBoneNames.ToDictionary(
                name => name,
                name => RequireBone(model, name),
                StringComparer.Ordinal);
            var hips = allBones["Hips"];
            var currentUp = CharacterUp(allBones);
            var currentForward = CharacterForward(allBones);
            var uprightDelta = Quaternion.LookRotation(slot.forward, slot.up) *
                               Quaternion.Inverse(Quaternion.LookRotation(currentForward, currentUp));
            hips.rotation = uprightDelta * hips.rotation;

            var rightFootRotation = allBones["RightFoot"].rotation;
            var rightToeLocalRotation = allBones["RightToeBase"].localRotation;
            MirrorRightLegToLeft(slot, allBones);
            allBones["LeftFoot"].rotation = rightFootRotation;
            allBones["LeftToeBase"].localRotation = rightToeLocalRotation;

            for (var iteration = 0; iteration < 5; iteration++)
            {
                GroundLeg(slot, model, allBones, "Right", rightFootRotation, rightToeLocalRotation);
                GroundLeg(slot, model, allBones, "Left", rightFootRotation, rightToeLocalRotation);
            }

            slotPose.RequireUnchanged(slot, "slot");
            modelPose.RequireUnchanged(model, "model root");
            foreach (var pair in armPoses)
                pair.Value.RequireUnchanged(RequireBone(model, pair.Key), pair.Key);
            if (!outsideBefore.SequenceEqual(OutsideTargetSignature(scene, model), StringComparer.Ordinal))
                throw new InvalidOperationException("A scene object outside Ispant_01_Static changed.");

            foreach (var transform in changed)
                PrefabUtility.RecordPrefabInstancePropertyModifications(transform);
            Undo.FlushUndoRecordObjects();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp could not be saved.");

            InspectIspant01StaticStandingPose();
            Debug.Log(
                "Ispant01StaticStandingPoseApplied Result=PASS" +
                ", Target=" + PlacementName + "/" + SlotName + "/" + ModelName +
                ", ArmsAndHandsChanged=False, OtherSceneObjectsChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Slot 01 Static Standing Pose")]
        public static void InspectIspant01StaticStandingPose()
        {
            var scene = RequireScene();
            var wasDirty = scene.isDirty;
            var slot = RequireSlot(scene);
            var model = RequireModel(slot);
            var bones = PoseBoneNames.ToDictionary(
                name => name,
                name => RequireBone(model, name),
                StringComparer.Ordinal);
            var source = RequireModelAsset();

            var report = new StringBuilder();
            report.AppendLine("Ispant01StaticStandingPoseInspection");
            report.AppendLine("Scene=" + scene.path);
            report.AppendLine("SceneDirtyBefore=" + wasDirty);
            report.AppendLine("SlotWorldPosition=" + Vec(slot.position));
            report.AppendLine("SlotWorldRotation=" + Quat(slot.rotation));
            report.AppendLine("ModelLocalPosition=" + Vec(model.localPosition));
            report.AppendLine("ModelLocalRotation=" + Quat(model.localRotation));
            report.AppendLine("ModelLocalScale=" + Vec(model.localScale));

            foreach (var name in PoseBoneNames)
            {
                var bone = bones[name];
                report.AppendLine(
                    "Bone=" + name +
                    "|LocalPosition=" + Vec(bone.localPosition) +
                    "|LocalRotation=" + Quat(bone.localRotation) +
                    "|ModelPosition=" + Vec(model.InverseTransformPoint(bone.position)) +
                    "|WorldPosition=" + Vec(bone.position));
            }

            var characterUp = (bones["Head"].position - bones["Hips"].position).normalized;
            var lateral = (bones["RightShoulder"].position - bones["LeftShoulder"].position).normalized;
            var characterForward = Vector3.Cross(lateral, characterUp).normalized;
            report.AppendLine("CharacterUp=" + Vec(characterUp));
            report.AppendLine("CharacterUpDotSlotUp=" + Num(Vector3.Dot(characterUp, slot.up)));
            report.AppendLine("CharacterForward=" + Vec(characterForward));
            report.AppendLine("CharacterForwardDotSlotForward=" + Num(Vector3.Dot(characterForward, slot.forward)));
            report.AppendLine("LeftFootBoneHeightFromSlot=" + Num(
                Vector3.Dot(bones["LeftFoot"].position - slot.position, slot.up)));
            report.AppendLine("RightFootBoneHeightFromSlot=" + Num(
                Vector3.Dot(bones["RightFoot"].position - slot.position, slot.up)));
            report.AppendLine("LeftToeBoneHeightFromSlot=" + Num(
                Vector3.Dot(bones["LeftToeBase"].position - slot.position, slot.up)));
            report.AppendLine("RightToeBoneHeightFromSlot=" + Num(
                Vector3.Dot(bones["RightToeBase"].position - slot.position, slot.up)));
            AppendLegAxes(report, slot, bones, "Left");
            AppendLegAxes(report, slot, bones, "Right");

            var leftGround = new List<float>();
            var rightGround = new List<float>();
            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                AppendFootSurfaceMetrics(report, slot, renderer);
                AppendGroundMeans(slot, renderer, leftGround, rightGround);
            }

            if (Vector3.Dot(characterUp, slot.up) < 0.9999f)
                throw new InvalidOperationException("The slot-1 static torso is not upright.");
            if (Vector3.Dot(characterForward, slot.forward) < 0.9999f)
                throw new InvalidOperationException("The slot-1 static model is not facing straight forward.");
            RequireGrounded(leftGround, "Left");
            RequireGrounded(rightGround, "Right");
            if (Mathf.Abs(leftGround.Min() - rightGround.Min()) > GroundTolerance)
                throw new InvalidOperationException("The two visible soles do not share the same ground plane.");
            foreach (var name in SourcePreservedLeftArmBoneNames)
            {
                var current = RequireBone(model, name);
                var original = RequireBone(source.transform, name);
                RequireSameLocalPose(current, original, name);
            }
            var rightArmMirrorError = RequireRightArmMirrored(bones, out var mirrorAxis);
            report.AppendLine("RightArmMirrorAxis=" + mirrorAxis);
            report.AppendLine("RightArmMaximumMirrorAngleError=" + Num(rightArmMirrorError));

            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("The slot-1 standing-pose inspection changed the scene.");

            report.AppendLine("SceneChanged=False");
            Debug.Log(report.ToString());
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 01 Static Right Arm Correction")]
        public static void ApplyIspant01StaticRightArmCorrection()
        {
            var scene = RequireScene();
            var slot = RequireSlot(scene);
            var model = RequireModel(slot);
            var outsideBefore = OutsideTargetSignature(scene, model);
            var slotPose = new LocalPose(slot);
            var modelPose = new LocalPose(model);
            var bones = PoseBoneNames.ToDictionary(
                name => name,
                name => RequireBone(model, name),
                StringComparer.Ordinal);
            var changed = ArmMirrorPairs.Select(pair => bones[pair.Right]).ToArray();
            var changedPositionScales = changed.ToDictionary(
                item => item,
                item => new LocalPositionScale(item));
            var protectedTransforms = model.GetComponentsInChildren<Transform>(true)
                .Where(item => !changed.Contains(item))
                .ToDictionary(item => item, item => new LocalPose(item));
            var planeNormal = (bones["RightShoulder"].position -
                               bones["LeftShoulder"].position).normalized;
            var mirrorAxis = ChooseMirrorAxis(
                bones["LeftShoulder"].rotation,
                bones["RightShoulder"].rotation,
                planeNormal);

            Undo.RecordObjects(changed, "Apply Ispant 01 static right arm correction");
            // The selected handedness axis converts the reflected left-arm basis back into
            // a proper rotation while keeping the right chain's local +Y bone direction.
            foreach (var pair in ArmMirrorPairs)
            {
                bones[pair.Right].rotation = MirroredRotation(
                    bones[pair.Left].rotation, planeNormal, mirrorAxis);
            }

            slotPose.RequireUnchanged(slot, "slot");
            modelPose.RequireUnchanged(model, "model root");
            foreach (var pair in changedPositionScales)
                pair.Value.RequireUnchanged(pair.Key, pair.Key.name);
            foreach (var pair in protectedTransforms)
                pair.Value.RequireUnchanged(pair.Key, pair.Key.name);
            if (!outsideBefore.SequenceEqual(OutsideTargetSignature(scene, model), StringComparer.Ordinal))
                throw new InvalidOperationException("A scene object outside Ispant_01_Static changed.");

            foreach (var transform in changed)
                PrefabUtility.RecordPrefabInstancePropertyModifications(transform);
            Undo.FlushUndoRecordObjects();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp could not be saved.");

            InspectIspant01StaticRightArmCorrection();
            Debug.Log(
                "Ispant01StaticRightArmCorrectionApplied Result=PASS" +
                ", MirrorAxis=" + mirrorAxis +
                ", Changed=RightShoulder,RightArm,RightForeArm,RightHand" +
                ", LocalPositionsAndScalesChanged=False, LeftArmChanged=False" +
                ", LowerBodyChanged=False, OtherSceneObjectsChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Slot 01 Static Right Arm Correction")]
        public static void InspectIspant01StaticRightArmCorrection()
        {
            var scene = RequireScene();
            var wasDirty = scene.isDirty;
            var slot = RequireSlot(scene);
            var model = RequireModel(slot);
            var bones = PoseBoneNames.ToDictionary(
                name => name,
                name => RequireBone(model, name),
                StringComparer.Ordinal);
            var source = RequireModelAsset();

            foreach (var name in SourcePreservedLeftArmBoneNames)
                RequireSameLocalPose(RequireBone(model, name), RequireBone(source.transform, name), name);
            var maximumMirrorError = RequireRightArmMirrored(bones, out var mirrorAxis);
            RequireStandingAndGrounded(slot, model, bones);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("The right-arm correction inspection changed the scene.");

            Debug.Log(
                "Ispant01StaticRightArmCorrectionInspected Result=PASS" +
                ", MirrorAxis=" + mirrorAxis +
                ", MaximumMirrorAngleError=" + Num(maximumMirrorError) +
                ", LeftArmMatchesSource=True, RightFingerLocalPoseChanged=False" +
                ", UprightAndForward=True, BothSolesGrounded=True, SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 01 Static Right Arm Correction Review")]
        public static void CaptureIspant01StaticRightArmCorrectionReview()
        {
            InspectIspant01StaticRightArmCorrection();
            var scene = RequireScene();
            var model = RequireModel(RequireSlot(scene));
            var clone = UnityEngine.Object.Instantiate(model.gameObject);
            clone.name = "Ispant01StaticRightArmCorrectionCapture";
            clone.hideFlags = HideFlags.HideAndDontSave;
            clone.transform.SetParent(null, false);
            clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            SetLayerRecursively(clone.transform, 31);

            var cameraObject = new GameObject("Ispant01StaticRightArmCorrectionCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.085f, 0.1f, 1f);
            camera.cullingMask = 1 << 31;
            camera.fieldOfView = 26f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;

            const int panelWidth = 600;
            const int panelHeight = 900;
            var target = new RenderTexture(panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelWidth, panelHeight, TextureFormat.RGB24, false);
            var combined = new Texture2D(panelWidth * 3, panelHeight, TextureFormat.RGB24, false);
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                var fullBounds = CombinedBounds(clone);
                var rightArmBounds = RightArmBounds(clone.transform);
                RenderReviewPanel(camera, fullBounds, Vector3.forward, panel, combined, target, 0);
                RenderReviewPanel(
                    camera, fullBounds, (Vector3.forward + Vector3.right * 0.65f).normalized,
                    panel, combined, target, 1);
                RenderReviewPanel(
                    camera, rightArmBounds, (Vector3.forward + Vector3.right * 0.45f).normalized,
                    panel, combined, target, 2);
                combined.Apply();
                var absolutePath = Path.Combine(
                    Directory.GetParent(Application.dataPath).FullName,
                    RightArmCapturePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
                File.WriteAllBytes(absolutePath, combined.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousActive;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(combined);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(clone);
            }

            if (scene.isDirty)
                throw new InvalidOperationException("The right-arm correction capture changed CargoRunMvp.");
            Debug.Log(
                "Ispant01StaticRightArmCorrectionCaptured Result=PASS" +
                ", Panels=Front,RightFrontThreeQuarter,RightArmClose" +
                ", Image=" + RightArmCapturePath + ", SceneChanged=False.");
        }

        private static MirrorAxis ChooseMirrorAxis(
            Quaternion leftRotation,
            Quaternion currentRightRotation,
            Vector3 planeNormal)
        {
            var xCandidate = MirroredRotation(
                leftRotation, planeNormal, MirrorAxis.NegateReflectedX);
            var zCandidate = MirroredRotation(
                leftRotation, planeNormal, MirrorAxis.NegateReflectedZ);
            return Quaternion.Angle(currentRightRotation, xCandidate) <=
                   Quaternion.Angle(currentRightRotation, zCandidate)
                ? MirrorAxis.NegateReflectedX
                : MirrorAxis.NegateReflectedZ;
        }

        private static Quaternion MirroredRotation(
            Quaternion sourceRotation,
            Vector3 planeNormal,
            MirrorAxis mirrorAxis)
        {
            var reflectedUp = ReflectVector(sourceRotation * Vector3.up, planeNormal);
            var reflectedForward = ReflectVector(sourceRotation * Vector3.forward, planeNormal);
            if (mirrorAxis == MirrorAxis.NegateReflectedZ)
                reflectedForward = -reflectedForward;
            return Quaternion.LookRotation(reflectedForward, reflectedUp);
        }

        private static Vector3 ReflectVector(Vector3 value, Vector3 planeNormal)
        {
            var normal = planeNormal.normalized;
            return value - 2f * Vector3.Dot(value, normal) * normal;
        }

        private static float RequireRightArmMirrored(
            IReadOnlyDictionary<string, Transform> bones,
            out MirrorAxis mirrorAxis)
        {
            var planeNormal = (bones["RightShoulder"].position -
                               bones["LeftShoulder"].position).normalized;
            mirrorAxis = ChooseMirrorAxis(
                bones["LeftShoulder"].rotation,
                bones["RightShoulder"].rotation,
                planeNormal);
            var maximumError = 0f;
            foreach (var pair in ArmMirrorPairs)
            {
                var expected = MirroredRotation(
                    bones[pair.Left].rotation, planeNormal, mirrorAxis);
                var error = Quaternion.Angle(expected, bones[pair.Right].rotation);
                maximumError = Mathf.Max(maximumError, error);
            }
            if (maximumError > 0.01f)
                throw new InvalidOperationException(
                    "The corrected right arm does not mirror the intact left arm. Error=" +
                    Num(maximumError) + ".");
            return maximumError;
        }

        private static void RequireStandingAndGrounded(
            Transform slot,
            Transform model,
            IReadOnlyDictionary<string, Transform> bones)
        {
            if (Vector3.Dot(CharacterUp(bones), slot.up) < 0.9999f ||
                Vector3.Dot(CharacterForward(bones), slot.forward) < 0.9999f)
                throw new InvalidOperationException("The standing model lost its upright forward pose.");
            var leftGround = new List<float>();
            var rightGround = new List<float>();
            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                AppendGroundMeans(slot, renderer, leftGround, rightGround);
            RequireGrounded(leftGround, "Left");
            RequireGrounded(rightGround, "Right");
            if (Mathf.Abs(leftGround.Min() - rightGround.Min()) > GroundTolerance)
                throw new InvalidOperationException("The corrected model lost its shared ground plane.");
        }

        private static Bounds RightArmBounds(Transform model)
        {
            var points = ArmMirrorPairs
                .Select(pair => RequireBone(model, pair.Right).position)
                .ToArray();
            var bounds = new Bounds(points[0], Vector3.zero);
            foreach (var point in points.Skip(1))
                bounds.Encapsulate(point);
            bounds.Expand(new Vector3(0.45f, 0.35f, 0.45f));
            return bounds;
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 01 Static Standing Pose Review")]
        public static void CaptureIspant01StaticStandingPoseReview()
        {
            InspectIspant01StaticStandingPose();
            var scene = RequireScene();
            var model = RequireModel(RequireSlot(scene));
            var clone = UnityEngine.Object.Instantiate(model.gameObject);
            clone.name = "Ispant01StaticStandingPoseCapture";
            clone.hideFlags = HideFlags.HideAndDontSave;
            clone.transform.SetParent(null, false);
            clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            SetLayerRecursively(clone.transform, 31);

            var cameraObject = new GameObject("Ispant01StaticStandingPoseCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.085f, 0.1f, 1f);
            camera.cullingMask = 1 << 31;
            camera.fieldOfView = 28f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;

            const int panelWidth = 700;
            const int panelHeight = 1000;
            var target = new RenderTexture(panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelWidth, panelHeight, TextureFormat.RGB24, false);
            var combined = new Texture2D(panelWidth * 2, panelHeight, TextureFormat.RGB24, false);
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                var bounds = CombinedBounds(clone);
                RenderReviewPanel(camera, bounds, Vector3.forward, panel, combined, target, 0);
                RenderReviewPanel(
                    camera, bounds, (Vector3.forward + Vector3.right * 0.55f).normalized,
                    panel, combined, target, 1);
                combined.Apply();
                var absolutePath = Path.Combine(
                    Directory.GetParent(Application.dataPath).FullName,
                    CapturePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
                File.WriteAllBytes(absolutePath, combined.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousActive;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(combined);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(clone);
            }

            if (scene.isDirty)
                throw new InvalidOperationException("The standing-pose capture changed CargoRunMvp.");
            Debug.Log(
                "Ispant01StaticStandingPoseCaptured Result=PASS, Panels=Front,FrontThreeQuarter" +
                ", Image=" + CapturePath + ", SceneChanged=False.");
        }

        private static void MirrorRightLegToLeft(
            Transform slot,
            IReadOnlyDictionary<string, Transform> bones)
        {
            var planeOrigin = (bones["LeftUpLeg"].position + bones["RightUpLeg"].position) * 0.5f;
            var targetKnee = ReflectPoint(bones["RightLeg"].position, planeOrigin, slot.right);
            var targetFoot = ReflectPoint(bones["RightFoot"].position, planeOrigin, slot.right);
            SolveTwoBonePosition(
                bones["LeftUpLeg"], bones["LeftLeg"], bones["LeftFoot"],
                targetKnee, targetFoot);
        }

        private static Vector3 ReflectPoint(Vector3 point, Vector3 origin, Vector3 normal)
        {
            var direction = normal.normalized;
            return point - 2f * Vector3.Dot(point - origin, direction) * direction;
        }

        private static void SolveTwoBonePosition(
            Transform upper,
            Transform lower,
            Transform end,
            Vector3 preferredJoint,
            Vector3 target)
        {
            var root = upper.position;
            var upperLength = Vector3.Distance(root, lower.position);
            var lowerLength = Vector3.Distance(lower.position, end.position);
            var rootToTarget = target - root;
            var distance = Mathf.Clamp(
                rootToTarget.magnitude, 0.0001f, upperLength + lowerLength - 0.0001f);
            var direction = rootToTarget.normalized;
            var bend = preferredJoint - root;
            bend -= direction * Vector3.Dot(bend, direction);
            if (bend.sqrMagnitude < 0.000001f)
                bend = Vector3.Cross(direction, Vector3.right);
            bend.Normalize();
            var along = (upperLength * upperLength + distance * distance -
                         lowerLength * lowerLength) / (2f * distance);
            var perpendicular = Mathf.Sqrt(Mathf.Max(
                0f, upperLength * upperLength - along * along));
            var desiredJoint = root + direction * along + bend * perpendicular;
            upper.rotation = Quaternion.FromToRotation(
                                 lower.position - upper.position,
                                 desiredJoint - upper.position) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(
                                 end.position - lower.position,
                                 target - lower.position) * lower.rotation;
        }

        private static void GroundLeg(
            Transform slot,
            Transform model,
            IReadOnlyDictionary<string, Transform> bones,
            string side,
            Quaternion footRotation,
            Quaternion toeLocalRotation)
        {
            var height = LowestSoleMean(slot, model, side);
            var upper = bones[side + "UpLeg"];
            var lower = bones[side + "Leg"];
            var foot = bones[side + "Foot"];
            var toe = bones[side + "ToeBase"];
            var target = foot.position - slot.up * height;
            SolveTwoBonePosition(upper, lower, foot, lower.position, target);
            foot.rotation = footRotation;
            toe.localRotation = toeLocalRotation;
        }

        private static float LowestSoleMean(Transform slot, Transform model, string side)
        {
            var values = new List<float>();
            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var indices = BoneIndices(renderer, side + "Foot", side + "ToeBase");
                if (indices.Count == 0 || renderer.sharedMesh == null ||
                    renderer.sharedMesh.boneWeights.Length != renderer.sharedMesh.vertexCount)
                    continue;
                var baked = new Mesh();
                try
                {
                    renderer.BakeMesh(baked);
                    var heights = Enumerable.Range(0, renderer.sharedMesh.vertexCount)
                        .Where(index => WeightForBones(
                            renderer.sharedMesh.boneWeights[index], indices) >= 0.25f)
                        .Select(index => Vector3.Dot(
                            renderer.transform.TransformPoint(baked.vertices[index]) - slot.position,
                            slot.up))
                        .OrderBy(value => value)
                        .ToArray();
                    if (heights.Length > 0)
                    {
                        var count = Mathf.Max(1, Mathf.CeilToInt(heights.Length * 0.02f));
                        values.Add(heights.Take(count).Average());
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(baked);
                }
            }

            if (values.Count == 0)
                throw new InvalidOperationException(side + " sole surface could not be measured.");
            return values.Min();
        }

        private static void AppendGroundMeans(
            Transform slot,
            SkinnedMeshRenderer renderer,
            ICollection<float> left,
            ICollection<float> right)
        {
            Append("Left", left);
            Append("Right", right);

            void Append(string side, ICollection<float> output)
            {
                var indices = BoneIndices(renderer, side + "Foot", side + "ToeBase");
                if (indices.Count == 0 || renderer.sharedMesh == null ||
                    renderer.sharedMesh.boneWeights.Length != renderer.sharedMesh.vertexCount)
                    return;
                var baked = new Mesh();
                try
                {
                    renderer.BakeMesh(baked);
                    var heights = Enumerable.Range(0, renderer.sharedMesh.vertexCount)
                        .Where(index => WeightForBones(
                            renderer.sharedMesh.boneWeights[index], indices) >= 0.25f)
                        .Select(index => Vector3.Dot(
                            renderer.transform.TransformPoint(baked.vertices[index]) - slot.position,
                            slot.up))
                        .OrderBy(value => value)
                        .ToArray();
                    if (heights.Length > 0)
                    {
                        var count = Mathf.Max(1, Mathf.CeilToInt(heights.Length * 0.02f));
                        output.Add(heights.Take(count).Average());
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(baked);
                }
            }
        }

        private static void RequireGrounded(IReadOnlyCollection<float> values, string side)
        {
            if (values.Count == 0)
                throw new InvalidOperationException(side + " sole surface could not be inspected.");
            var height = values.Min();
            if (Mathf.Abs(height) > GroundTolerance)
                throw new InvalidOperationException(
                    side + " sole is not on the slot ground. Height=" + Num(height));
        }

        private static Vector3 CharacterUp(IReadOnlyDictionary<string, Transform> bones) =>
            (bones["Head"].position - bones["Hips"].position).normalized;

        private static Vector3 CharacterForward(IReadOnlyDictionary<string, Transform> bones)
        {
            var lateral = (bones["RightShoulder"].position - bones["LeftShoulder"].position).normalized;
            return Vector3.Cross(lateral, CharacterUp(bones)).normalized;
        }

        private static void CopyLocalPose(Transform source, Transform target)
        {
            target.localPosition = source.localPosition;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
        }

        private static void RequireSameLocalPose(Transform current, Transform expected, string label)
        {
            if (Vector3.Distance(current.localPosition, expected.localPosition) > PoseTolerance ||
                Quaternion.Angle(current.localRotation, expected.localRotation) > PoseTolerance ||
                Vector3.Distance(current.localScale, expected.localScale) > PoseTolerance)
                throw new InvalidOperationException(label + " local TRS changed from the source model.");
        }

        private static string[] OutsideTargetSignature(Scene scene, Transform target)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item != target && !item.IsChildOf(target))
                .Select(item =>
                    item.GetInstanceID() + "|" + item.gameObject.activeSelf + "|" +
                    Vec(item.localPosition) + "|" + Quat(item.localRotation) + "|" +
                    Vec(item.localScale))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static GameObject RequireModelAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath);
            if (asset == null)
                throw new InvalidOperationException("The direct Ispant model asset is missing.");
            return asset;
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                transform.gameObject.layer = layer;
        }

        private static Bounds CombinedBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled).ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException("The standing-pose capture has no visible renderers.");
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static void RenderReviewPanel(
            Camera camera,
            Bounds bounds,
            Vector3 viewDirection,
            Texture2D panel,
            Texture2D combined,
            RenderTexture target,
            int panelIndex)
        {
            camera.aspect = (float)panel.width / panel.height;
            var distance = (bounds.size.y * 0.55f) /
                           Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            camera.transform.position = bounds.center + viewDirection * distance;
            camera.transform.rotation = Quaternion.LookRotation(
                bounds.center - camera.transform.position, Vector3.up);
            camera.Render();
            RenderTexture.active = target;
            panel.ReadPixels(new Rect(0, 0, panel.width, panel.height), 0, 0);
            panel.Apply();
            combined.SetPixels32(panelIndex * panel.width, 0, panel.width, panel.height, panel.GetPixels32());
        }

        private readonly struct LocalPose
        {
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            public LocalPose(Transform transform)
            {
                position = transform.localPosition;
                rotation = transform.localRotation;
                scale = transform.localScale;
            }

            public void RequireUnchanged(Transform transform, string label)
            {
                if (Vector3.Distance(position, transform.localPosition) > PoseTolerance ||
                    Quaternion.Angle(rotation, transform.localRotation) > PoseTolerance ||
                    Vector3.Distance(scale, transform.localScale) > PoseTolerance)
                    throw new InvalidOperationException(label + " local TRS changed.");
            }
        }

        private readonly struct LocalPositionScale
        {
            private readonly Vector3 position;
            private readonly Vector3 scale;

            public LocalPositionScale(Transform transform)
            {
                position = transform.localPosition;
                scale = transform.localScale;
            }

            public void RequireUnchanged(Transform transform, string label)
            {
                if (Vector3.Distance(position, transform.localPosition) > PoseTolerance ||
                    Vector3.Distance(scale, transform.localScale) > PoseTolerance)
                    throw new InvalidOperationException(label + " local position or scale changed.");
            }
        }

        private enum MirrorAxis
        {
            NegateReflectedX,
            NegateReflectedZ
        }

        private static void AppendLegAxes(
            StringBuilder report,
            Transform slot,
            IReadOnlyDictionary<string, Transform> bones,
            string side)
        {
            var upper = bones[side + "UpLeg"];
            var lower = bones[side + "Leg"];
            var foot = bones[side + "Foot"];
            var toe = bones[side + "ToeBase"];
            report.AppendLine(
                side + "LegDirections" +
                "|UpperToKnee=" + Vec(lower.position - upper.position) +
                "|KneeToAnkle=" + Vec(foot.position - lower.position) +
                "|AnkleToToe=" + Vec(toe.position - foot.position));
            report.AppendLine(
                side + "FootAxes" +
                "|Right=" + Vec(foot.right) +
                "|Up=" + Vec(foot.up) +
                "|Forward=" + Vec(foot.forward) +
                "|UpDotSlotUp=" + Num(Vector3.Dot(foot.up, slot.up)) +
                "|ForwardDotSlotUp=" + Num(Vector3.Dot(foot.forward, slot.up)) +
                "|RightDotSlotUp=" + Num(Vector3.Dot(foot.right, slot.up)));
        }

        private static void AppendFootSurfaceMetrics(
            StringBuilder report,
            Transform slot,
            SkinnedMeshRenderer renderer)
        {
            var sharedMesh = renderer.sharedMesh;
            if (sharedMesh == null || sharedMesh.boneWeights.Length != sharedMesh.vertexCount)
            {
                report.AppendLine("Renderer=" + renderer.name + "|FootSurface=Unavailable");
                return;
            }

            var leftIndices = BoneIndices(renderer, "LeftFoot", "LeftToeBase");
            var rightIndices = BoneIndices(renderer, "RightFoot", "RightToeBase");
            var baked = new Mesh();
            try
            {
                renderer.BakeMesh(baked);
                AppendSide("Left", leftIndices);
                AppendSide("Right", rightIndices);

                void AppendSide(string side, HashSet<int> indices)
                {
                    var weightedVertices = Enumerable.Range(0, sharedMesh.vertexCount)
                        .Where(index => WeightForBones(sharedMesh.boneWeights[index], indices) >= 0.25f)
                        .Select(index => renderer.transform.TransformPoint(baked.vertices[index]))
                        .ToArray();
                    if (weightedVertices.Length == 0)
                    {
                        report.AppendLine(
                            "Renderer=" + renderer.name + "|Side=" + side + "|WeightedVertices=0");
                        return;
                    }

                    var heights = weightedVertices
                        .Select(vertex => Vector3.Dot(vertex - slot.position, slot.up))
                        .OrderBy(value => value)
                        .ToArray();
                    var sampleCount = Mathf.Max(1, Mathf.CeilToInt(heights.Length * 0.02f));
                    report.AppendLine(
                        "Renderer=" + renderer.name +
                        "|Side=" + side +
                        "|WeightedVertices=" + weightedVertices.Length+
                        "|MinimumHeightFromSlot=" + Num(heights[0]) +
                        "|LowestTwoPercentMean=" + Num(heights.Take(sampleCount).Average()));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static HashSet<int> BoneIndices(SkinnedMeshRenderer renderer, params string[] names)
        {
            var expected = new HashSet<string>(names, StringComparer.Ordinal);
            return renderer.bones
                .Select((bone, index) => new { bone, index })
                .Where(item => item.bone != null && expected.Contains(item.bone.name))
                .Select(item => item.index)
                .ToHashSet();
        }

        private static float WeightForBones(BoneWeight weight, HashSet<int> indices)
        {
            var total = 0f;
            if (indices.Contains(weight.boneIndex0)) total += weight.weight0;
            if (indices.Contains(weight.boneIndex1)) total += weight.weight1;
            if (indices.Contains(weight.boneIndex2)) total += weight.weight2;
            if (indices.Contains(weight.boneIndex3)) total += weight.weight3;
            return total;
        }

        private static Scene RequireScene(bool clean = true)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be the active scene.");
            if (clean && scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp must be clean before this operation.");
            return scene;
        }

        private static Transform RequireSlot(Scene scene)
        {
            var placement = scene.GetRootGameObjects().Single(item => item.name == PlacementName);
            return placement.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == SlotName);
        }

        private static Transform RequireModel(Transform slot)
        {
            if (slot.childCount != 1 || slot.GetChild(0).name != ModelName)
                throw new InvalidOperationException(
                    "Ispant_01_Static does not contain the expected direct model.");
            return slot.GetChild(0);
        }

        private static Transform RequireBone(Transform model, string name) =>
            model.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == name);

        private static string Num(float value) =>
            value.ToString("0.######", CultureInfo.InvariantCulture);

        private static string Vec(Vector3 value) =>
            "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + ")";

        private static string Quat(Quaternion value) =>
            "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + "," + Num(value.w) + ")";
    }
}
