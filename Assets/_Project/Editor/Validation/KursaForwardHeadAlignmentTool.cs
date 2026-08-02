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

namespace Bellerophon.Editor.KursaCargoRunScene
{
    internal static class KursaForwardHeadAlignmentTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Kursa Enemy Placement";
        private const string ModelName = "Kursa_Model";
        private const string MoveSlotName = "Kursa_03_Move";
        private const string FaceMaterialName = "Kursa_face_metal_Approved";
        private const string ApprovedShaderName =
            "Bellerophon/Kursa/ApprovedAppearance";
        private const string ReportPath = "docs/validation/kursa_forward_head_alignment_2026-08-03/Kursa_ForwardHead_Inspection.txt";
        private const string CapturePath = "docs/validation/kursa_forward_head_alignment_2026-08-03/Kursa_ForwardHead_Review.png";
        private const float PositionTolerance = 0.000001f;
        private const float HeadAngleTolerance = 0.05f;
        private const float AnimatedSampleRate = 120f;

        private static readonly string[] SlotNames =
        {
            "Kursa_01_Static_Review", "Kursa_02_Idle", "Kursa_03_Move",
            "Kursa_04_ShieldBash", "Kursa_05_ToShieldStance", "Kursa_06_ShieldStance",
            "Kursa_07_ShieldStanceMove", "Kursa_08_FromShieldStance", "Kursa_09_Stop",
            "Kursa_10_Hit", "Kursa_11_Death", "Kursa_12_ShieldBreakReaction"
        };

        [MenuItem("Bellerophon/Enemies/Kursa/Apply Forward Head Alignment")]
        public static void ApplyKursaForwardHeadAlignment()
        {
            KursaGroundedIdleAnimationTool.ApplyKursaIdleAnimation();
            KursaMoveAnimationTool.ApplyKursaMoveAnimation();
            var scene = RequireScene(true);
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var commonY = RequireOtherModelCommonY(placement.transform);
            var moveModel = RequireModel(RequireChild(placement.transform, MoveSlotName));
            var movePosition = moveModel.localPosition;
            movePosition.y = commonY;
            moveModel.localPosition = movePosition;
            PrefabUtility.RecordPrefabInstancePropertyModifications(moveModel);
            EditorUtility.SetDirty(moveModel);

            var maximumAppliedAngle = 0f;
            foreach (var slotName in SlotNames)
            {
                var model = RequireModel(RequireChild(placement.transform, slotName));
                var renderer = RequireRenderer(model, slotName);
                RequireEyeAttachmentContract(renderer, slotName);
                var animator = model.GetComponent<Animator>();
                var animatorEnabled = animator != null && animator.enabled;
                try
                {
                    if (animator != null) animator.enabled = false;
                    maximumAppliedAngle = Mathf.Max(
                        maximumAppliedAngle,
                        AlignHeadToModelLocalForward(model, renderer));
                    var head = RequireBone(renderer, "Head");
                    PrefabUtility.RecordPrefabInstancePropertyModifications(head);
                    EditorUtility.SetDirty(head);
                }
                finally
                {
                    if (animator != null) animator.enabled = animatorEnabled;
                }
            }

            RequireEqual(otherRootsBefore, OtherRootSignatures(scene, placement),
                "A scene root outside the Kursa placement changed.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after Kursa head alignment.");
            AssetDatabase.SaveAssets();
            var capture = Absolute(CapturePath);
            if (File.Exists(capture)) File.Delete(capture);
            Debug.Log("KursaForwardHeadAlignmentApplied Result=PASS, Slots=12, MoveModelLocalY=" +
                Num(moveModel.localPosition.y) + ", OtherModelCommonY=" + Num(commonY) +
                ", MaximumAppliedHeadAngle=" + Num(maximumAppliedAngle) +
                ", DirectionBasis=HeadToHeadFrontAlignedToModelLocalPositiveZ" +
                ", UpBasis=HeadToHeadEndAlignedToModelLocalPositiveY" +
                ", EyeAttachment=PerVertexUvChannelsOnSkinnedFaceMesh" +
                ", OtherSceneRootsUnchanged=True, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Inspect Forward Head Alignment")]
        public static void InspectKursaForwardHeadAlignment()
        {
            KursaGroundedIdleAnimationTool.InspectKursaIdleAnimation();
            var scene = RequireScene(true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var commonY = RequireOtherModelCommonY(placement.transform);
            var moveModel = RequireModel(RequireChild(placement.transform, MoveSlotName));
            var yError = Mathf.Abs(moveModel.localPosition.y - commonY);
            if (yError > PositionTolerance)
                throw new InvalidOperationException(
                    "Kursa move model Y differs from the other models. Error=" +
                    Num(yError) + ".");

            var staticModel = RequireModel(RequireChild(
                placement.transform,
                "Kursa_01_Static_Review"));
            Debug.Log(
                "KursaHeadMarkerContract Static=" +
                HeadMarkerContract(
                    staticModel,
                    RequireRenderer(staticModel, "Kursa_01_Static_Review")) +
                ", Move=" +
                HeadMarkerContract(
                    moveModel,
                    RequireRenderer(moveModel, MoveSlotName)) + ".");

            var slotAngles = new Dictionary<string, float>(StringComparer.Ordinal);
            foreach (var slotName in SlotNames)
            {
                var model = RequireModel(RequireChild(placement.transform, slotName));
                var clip = SlotClip(slotName);
                RequireEyeAttachmentContract(
                    RequireRenderer(model, slotName),
                    slotName);
                slotAngles[slotName] = InspectSlotHead(
                    model,
                    slotName,
                    clip);
            }
            var maximumAngle = slotAngles.Values.Max();
            if (maximumAngle > HeadAngleTolerance)
                throw new InvalidOperationException(
                    "A Kursa face does not match its model-local forward/up axes. MaximumError=" +
                    Num(maximumAngle) + ", Slots=" +
                    string.Join("|", slotAngles.Select(item =>
                        item.Key + "=" + Num(item.Value))) + ".");
            WriteReport(commonY, moveModel.localPosition.y, yError, slotAngles);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Kursa forward-head inspection changed the scene dirty state.");
            Debug.Log("KursaForwardHeadAlignmentInspected Result=PASS, Slots=12, MoveModelLocalY=" +
                Num(moveModel.localPosition.y) + ", OtherModelCommonY=" + Num(commonY) +
                ", ModelYError=" + Num(yError) + ", MaximumHeadLocalFrameError=" +
                Num(maximumAngle) + ", AnimatedSampleRate=" + Num(AnimatedSampleRate) +
                ", DirectionBasis=HeadToHeadFrontAlignedToModelLocalPositiveZ" +
                ", UpBasis=HeadToHeadEndAlignedToModelLocalPositiveY" +
                ", EyeAttachment=PerVertexUvChannelsOnSkinnedFaceMesh" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture Forward Head Alignment Review")]
        public static void CaptureKursaForwardHeadAlignmentReview()
        {
            InspectKursaForwardHeadAlignment();
            var scene = RequireScene(true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var destination = Absolute(CapturePath);
            if (File.Exists(destination))
                throw new InvalidOperationException(
                    "The one-time Kursa forward-head review already exists: " +
                    CapturePath);
            CaptureGrid(placement.transform, destination);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Kursa forward-head capture changed the scene dirty state.");
            Debug.Log("KursaForwardHeadAlignmentReviewCaptured Result=PASS, Slots=12, Image=" +
                CapturePath + ", SceneChanged=False.");
        }

        internal static float AlignHeadToModelLocalForward(
            Transform model,
            SkinnedMeshRenderer renderer)
        {
            var frame = RequireHeadMarkerFrame(renderer);
            var before = Mathf.Max(
                Vector3.Angle(frame.Forward, model.forward),
                Vector3.Angle(frame.Up, model.up));
            var currentFrame = Quaternion.LookRotation(frame.Forward, frame.Up);
            var targetFrame = Quaternion.LookRotation(model.forward, model.up);
            frame.Head.rotation = targetFrame * Quaternion.Inverse(currentFrame) *
                frame.Head.rotation;
            var remaining = MeasureHeadLocalFrameError(model, renderer);
            if (remaining > HeadAngleTolerance)
                throw new InvalidOperationException(
                    "Kursa Head marker frame did not align to model-local +Z/+Y. RemainingError=" +
                    Num(remaining) + ".");
            return before;
        }

        internal static float MeasureHeadLocalFrameError(
            Transform model,
            SkinnedMeshRenderer renderer)
        {
            var frame = RequireHeadMarkerFrame(renderer);
            return Mathf.Max(
                Vector3.Angle(frame.Forward, model.forward),
                Vector3.Angle(frame.Up, model.up));
        }

        internal static float AddModelLocalForwardHeadCurves(
            AnimationClip clip,
            Transform model,
            float sampleRate)
        {
            if (clip == null || sampleRate <= 0f)
                throw new ArgumentException(
                    "A clip and positive sample rate are required for Kursa Head curves.");
            var renderer = RequireRenderer(model, "Kursa idle Head curves");
            var head = RequireBone(renderer, "Head");
            var path = AnimationUtility.CalculateTransformPath(head, model);
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var animator = model.GetComponent<Animator>();
            var animatorEnabled = animator != null && animator.enabled;
            var x = new List<Keyframe>();
            var y = new List<Keyframe>();
            var z = new List<Keyframe>();
            var w = new List<Keyframe>();
            var maximumCorrection = 0f;
            var hasPrevious = false;
            var previous = Quaternion.identity;
            try
            {
                if (animator != null) animator.enabled = false;
                var samples = Mathf.CeilToInt(clip.length * sampleRate);
                for (var sample = 0; sample <= samples; sample++)
                {
                    foreach (var snapshot in snapshots) snapshot.Restore();
                    var time = clip.length * sample / samples;
                    clip.SampleAnimation(model.gameObject, time);
                    maximumCorrection = Mathf.Max(
                        maximumCorrection,
                        AlignHeadToModelLocalForward(model, renderer));
                    var rotation = head.localRotation;
                    if (hasPrevious && Quaternion.Dot(previous, rotation) < 0f)
                        rotation = new Quaternion(
                            -rotation.x,
                            -rotation.y,
                            -rotation.z,
                            -rotation.w);
                    previous = rotation;
                    hasPrevious = true;
                    x.Add(new Keyframe(time, rotation.x));
                    y.Add(new Keyframe(time, rotation.y));
                    z.Add(new Keyframe(time, rotation.z));
                    w.Add(new Keyframe(time, rotation.w));
                }
            }
            finally
            {
                foreach (var snapshot in snapshots) snapshot.Restore();
                if (animator != null) animator.enabled = animatorEnabled;
            }
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    "m_LocalRotation.x"),
                LinearCurve(x));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    "m_LocalRotation.y"),
                LinearCurve(y));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    "m_LocalRotation.z"),
                LinearCurve(z));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    "m_LocalRotation.w"),
                LinearCurve(w));
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            return maximumCorrection;
        }

        private static AnimationCurve LinearCurve(List<Keyframe> keys)
        {
            var curve = new AnimationCurve(keys.ToArray());
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve,
                    index,
                    AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(
                    curve,
                    index,
                    AnimationUtility.TangentMode.Linear);
            }
            return curve;
        }

        private static float InspectSlotHead(
            Transform model,
            string slotName,
            AnimationClip clip)
        {
            var renderer = RequireRenderer(model, slotName);
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var animator = model.GetComponent<Animator>();
            var animatorEnabled = animator != null && animator.enabled;
            var maximum = 0f;
            try
            {
                if (animator != null) animator.enabled = false;
                if (clip == null)
                    return MeasureHeadLocalFrameError(model, renderer);
                var samples = Mathf.CeilToInt(clip.length * AnimatedSampleRate);
                for (var sample = 0; sample <= samples; sample++)
                {
                    clip.SampleAnimation(
                        model.gameObject,
                        clip.length * sample / samples);
                    maximum = Mathf.Max(
                        maximum,
                        MeasureHeadLocalFrameError(model, renderer));
                }
                return maximum;
            }
            finally
            {
                foreach (var snapshot in snapshots) snapshot.Restore();
                if (animator != null) animator.enabled = animatorEnabled;
            }
        }

        private static AnimationClip SlotClip(string slotName)
        {
            if (slotName == "Kursa_02_Idle")
                return AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_02_GroundedIdle.anim") ??
                    throw new InvalidOperationException("Kursa idle clip is missing.");
            if (slotName == MoveSlotName)
                return AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    KursaMoveAnimationTool.ClipPath) ??
                    throw new InvalidOperationException("Kursa move clip is missing.");
            return null;
        }

        private static HeadMarkerFrame RequireHeadMarkerFrame(
            SkinnedMeshRenderer renderer)
        {
            var head = RequireBone(renderer, "Head");
            var headFront = RequireBone(renderer, "headfront");
            var headEnd = RequireBone(renderer, "head_end");
            if (headFront.parent != head || headEnd.parent != head)
                throw new InvalidOperationException(
                    renderer.name +
                    " must keep headfront and head_end directly under Head.");
            var forward = headFront.position - head.position;
            if (forward.sqrMagnitude < 0.000001f)
                throw new InvalidOperationException(
                    renderer.name + " has a zero-length Head-to-headfront marker.");
            forward.Normalize();
            var up = Vector3.ProjectOnPlane(
                headEnd.position - head.position,
                forward);
            if (up.sqrMagnitude < 0.000001f)
                throw new InvalidOperationException(
                    renderer.name + " has an invalid Head-to-head_end marker.");
            return new HeadMarkerFrame(head, forward, up.normalized);
        }

        private static string HeadMarkerContract(
            Transform model,
            SkinnedMeshRenderer renderer)
        {
            var head = RequireBone(renderer, "Head");
            var headFront = RequireBone(renderer, "headfront");
            var headEnd = RequireBone(renderer, "head_end");
            var frame = RequireHeadMarkerFrame(renderer);
            return "HeadFrontParent=" +
                (headFront.parent == null ? "<none>" : headFront.parent.name) +
                "|HeadEndParent=" +
                (headEnd.parent == null ? "<none>" : headEnd.parent.name) +
                "|HeadFrontChild=" + headFront.IsChildOf(head) +
                "|HeadEndChild=" + headEnd.IsChildOf(head) +
                "|ForwardAngle=" + Num(Vector3.Angle(frame.Forward, model.forward)) +
                "|UpAngle=" + Num(Vector3.Angle(frame.Up, model.up));
        }

        private static Transform RequireBone(
            SkinnedMeshRenderer renderer,
            string name)
        {
            var rigRoot = renderer.rootBone ??
                throw new InvalidOperationException(
                    "Kursa renderer root bone is missing.");
            var matches = rigRoot.GetComponentsInChildren<Transform>(true).Where(item =>
                item != null && item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Kursa rig hierarchy contract differs: " + name + ".");
            return matches[0];
        }

        private static void RequireEyeAttachmentContract(
            SkinnedMeshRenderer renderer,
            string context)
        {
            var mesh = renderer.sharedMesh ??
                throw new InvalidOperationException(
                    context + " skinned face mesh is missing.");
            var left = mesh.uv2;
            var right = mesh.uv3;
            var depth = mesh.uv4;
            if (left.Length != mesh.vertexCount ||
                right.Length != mesh.vertexCount ||
                depth.Length != mesh.vertexCount)
                throw new InvalidOperationException(
                    context + " eye projection channels are not vertex-attached.");
            var faceMaterials = renderer.sharedMaterials.Where(item =>
                item != null && item.name == FaceMaterialName).ToArray();
            if (faceMaterials.Length != 1 ||
                faceMaterials[0].shader == null ||
                faceMaterials[0].shader.name != ApprovedShaderName ||
                faceMaterials[0].GetTexture("_EyeLeft") == null ||
                faceMaterials[0].GetTexture("_EyeRight") == null)
                throw new InvalidOperationException(
                    context + " approved face-eye material contract differs.");
            var leftVertices = 0;
            var rightVertices = 0;
            for (var index = 0; index < mesh.vertexCount; index++)
            {
                if (InUnitSquare(left[index]) && Mathf.Abs(depth[index].x) < 1f)
                    leftVertices++;
                if (InUnitSquare(right[index]) && Mathf.Abs(depth[index].y) < 1f)
                    rightVertices++;
            }
            if (leftVertices == 0 || rightVertices == 0)
                throw new InvalidOperationException(
                    context + " has no attached approved eye vertices.");
        }

        private static bool InUnitSquare(Vector2 value) =>
            value.x >= 0f && value.x <= 1f &&
            value.y >= 0f && value.y <= 1f;

        private static float RequireOtherModelCommonY(Transform placement)
        {
            var values = SlotNames.Where(item => item != MoveSlotName)
                .Select(item => new
                {
                    Slot = item,
                    Value = RequireModel(RequireChild(placement, item)).localPosition.y
                }).ToArray();
            var minimum = values.Min(item => item.Value);
            var maximum = values.Max(item => item.Value);
            if (maximum - minimum > PositionTolerance)
                throw new InvalidOperationException(
                    "Other Kursa model Y positions do not share one value: " +
                    string.Join("|", values.Select(item =>
                        item.Slot + "=" + Num(item.Value))) + ".");
            return values.Average(item => item.Value);
        }

        private static void WriteReport(
            float commonY,
            float moveY,
            float yError,
            IReadOnlyDictionary<string, float> slotAngles)
        {
            var destination = Absolute(ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Invalid report folder."));
            var lines = new List<string>
            {
                "Result=PASS",
                "Target=Approved Kursa Enemy Placement",
                "Slots=12",
                "MoveModelLocalY=" + Num(moveY),
                "OtherModelCommonY=" + Num(commonY),
                "ModelYError=" + Num(yError),
                "AnimatedSampleRate=" + Num(AnimatedSampleRate),
                "DirectionBasis=HeadToHeadFrontAlignedToModelLocalPositiveZ",
                "UpBasis=HeadToHeadEndAlignedToModelLocalPositiveY",
                "FaceSurfaceNormalsUsed=False",
                "EyeObjectsUvMaterialsUsedForDirection=False",
                "EyeAttachment=PerVertexUvChannelsOnSkinnedFaceMesh",
                "MaximumHeadLocalFrameError=" + Num(slotAngles.Values.Max())
            };
            lines.AddRange(slotAngles.Select(item =>
                item.Key + "HeadLocalFrameError=" + Num(item.Value)));
            lines.Add("ArmsShieldBodyLegsChanged=False");
            lines.Add("AppearanceEyesMaterialsChanged=False");
            lines.Add("OtherSceneRootsChanged=False");
            File.WriteAllLines(destination, lines, Encoding.UTF8);
        }

        private static void CaptureGrid(Transform placement, string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Invalid capture folder."));
            var models = SlotNames.Select(item =>
                RequireModel(RequireChild(placement, item))).ToArray();
            var sceneRenderers = placement.gameObject.scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Renderer>(true))
                .ToArray();
            var states = sceneRenderers.Select(item =>
                new RendererState(item)).ToArray();
            var sourceCamera = GameObject.Find("Player")?
                .GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException("Player camera is missing.");
            const int panelWidth = 320;
            const int panelHeight = 480;
            const int columns = 4;
            const int rows = 3;
            var cameraObject = new GameObject(
                "KursaForwardHeadReviewCamera",
                typeof(Camera)) { hideFlags = HideFlags.HideAndDontSave };
            var target = new RenderTexture(
                panelWidth,
                panelHeight,
                24,
                RenderTextureFormat.ARGB32);
            var panel = new Texture2D(
                panelWidth,
                panelHeight,
                TextureFormat.RGB24,
                false);
            var grid = new Texture2D(
                panelWidth * columns,
                panelHeight * rows,
                TextureFormat.RGB24,
                false);
            var oldActive = RenderTexture.active;
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = ~0;
                camera.fieldOfView = 30f;
                camera.targetTexture = target;
                for (var index = 0; index < models.Length; index++)
                {
                    foreach (var renderer in sceneRenderers)
                        renderer.enabled = renderer.transform.IsChildOf(models[index]);
                    FrameCamera(camera, models[index], panelWidth / (float)panelHeight);
                    camera.Render();
                    RenderTexture.active = target;
                    panel.ReadPixels(
                        new Rect(0f, 0f, panelWidth, panelHeight),
                        0,
                        0);
                    panel.Apply();
                    var pixels = panel.GetPixels32();
                    if (pixels.Any(pixel =>
                        pixel.r >= 240 && pixel.b >= 240 && pixel.g <= 24))
                        throw new InvalidOperationException(
                            "Kursa forward-head review contains Unity magenta fallback.");
                    var column = index % columns;
                    var row = rows - 1 - index / columns;
                    grid.SetPixels32(
                        column * panelWidth,
                        row * panelHeight,
                        panelWidth,
                        panelHeight,
                        pixels);
                }
                grid.Apply();
                File.WriteAllBytes(destination, grid.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                foreach (var state in states) state.Restore();
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(grid);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void FrameCamera(Camera camera, Transform model, float aspect)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled).ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException("Kursa model has no visible renderer.");
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            var direction = model.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.forward;
            direction.Normalize();
            camera.aspect = aspect;
            var vertical = bounds.extents.y /
                Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            var horizontalFov = 2f * Mathf.Atan(
                Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * aspect);
            var horizontal = Mathf.Max(bounds.extents.x, bounds.extents.z) /
                Mathf.Tan(horizontalFov * 0.5f);
            var distance = Mathf.Max(vertical, horizontal) * 1.2f;
            camera.transform.position = bounds.center + direction * distance;
            camera.transform.rotation = Quaternion.LookRotation(
                bounds.center - camera.transform.position,
                Vector3.up);
        }

        private static Scene RequireScene(bool clean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "Open CargoRunMvp before working on Kursa head alignment.");
            if (clean && scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp has unsaved changes.");
            return scene;
        }

        private static GameObject RequirePlacement(Scene scene) =>
            scene.GetRootGameObjects().SingleOrDefault(item =>
                item.name == PlacementRootName) ??
            throw new InvalidOperationException("Approved Kursa placement is missing.");

        private static void RequireSlotContract(Transform placement)
        {
            if (placement.childCount != SlotNames.Length)
                throw new InvalidOperationException("Kursa slot count differs.");
            for (var index = 0; index < SlotNames.Length; index++)
            {
                var slot = placement.GetChild(index);
                if (slot.name != SlotNames[index] || slot.childCount != 1 ||
                    slot.GetChild(0).name != ModelName)
                    throw new InvalidOperationException(
                        "Kursa slot contract differs at " + index + ".");
            }
        }

        private static Transform RequireChild(Transform parent, string name)
        {
            var matches = Enumerable.Range(0, parent.childCount)
                .Select(parent.GetChild).Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Required direct child differs: " + name + ".");
            return matches[0];
        }

        private static Transform RequireModel(Transform slot)
        {
            if (slot.childCount != 1 || slot.GetChild(0).name != ModelName)
                throw new InvalidOperationException(
                    slot.name + " model contract differs.");
            return slot.GetChild(0);
        }

        private static SkinnedMeshRenderer RequireRenderer(
            Transform model,
            string context) =>
            model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault() ??
            throw new InvalidOperationException(
                context + " must contain one skinned renderer.");

        private static string[] OtherRootSignatures(
            Scene scene,
            GameObject placement) =>
            scene.GetRootGameObjects().Where(item => item != placement)
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .Select(item => RecursiveSignature(item.transform)).ToArray();

        private static string RecursiveSignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
                builder.Append(item.name).Append('|').Append(item.gameObject.activeSelf)
                    .Append('|').Append(item.localPosition).Append('|')
                    .Append(item.localRotation).Append('|').Append(item.localScale);
            return builder.ToString();
        }

        private static void RequireEqual(
            string[] before,
            string[] after,
            string message)
        {
            if (!before.SequenceEqual(after, StringComparer.Ordinal))
                throw new InvalidOperationException(message);
        }

        private static string Absolute(string relative) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", relative));

        private static string Num(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture);

        private readonly struct HeadMarkerFrame
        {
            public readonly Transform Head;
            public readonly Vector3 Forward;
            public readonly Vector3 Up;

            public HeadMarkerFrame(
                Transform head,
                Vector3 forward,
                Vector3 up)
            {
                Head = head;
                Forward = forward;
                Up = up;
            }
        }

        private readonly struct TransformSnapshot
        {
            private readonly Transform item;
            private readonly Vector3 position;
            private readonly Vector3 scale;
            private readonly Quaternion rotation;

            public TransformSnapshot(Transform value)
            {
                item = value;
                position = value.localPosition;
                scale = value.localScale;
                rotation = value.localRotation;
            }

            public void Restore()
            {
                if (item == null) return;
                item.localPosition = position;
                item.localScale = scale;
                item.localRotation = rotation;
            }
        }

        private readonly struct RendererState
        {
            private readonly Renderer renderer;
            private readonly bool enabled;

            public RendererState(Renderer value)
            {
                renderer = value;
                enabled = value.enabled;
            }

            public void Restore()
            {
                if (renderer != null) renderer.enabled = enabled;
            }
        }
    }
}
