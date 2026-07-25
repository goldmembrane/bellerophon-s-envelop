using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bellerophon.Editor.NegatifCargoRunScene
{
    internal static class NegatifMoveAnimationTool
    {
        private const string DolorePlacementRootName = "Approved Dolore Enemy Placement";
        private const string DoloreMoveSlotName = "Dolore_03_Move_Quadruped";
        private const string DoloreModelName = "Dolore_Model";
        private const string NegatifPlacementRootName = "Approved Negatif Enemy Placement";
        private const string NegatifMoveSlotName = "Negatif_02_Move";
        private const string NegatifModelName = "Negatif_Model";
        private const string PlayerName = "Player";
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string AnimationFolder =
            "Assets/_Project/Art/Enemies/Negatif/Animations";
        private const string ControllerFolder =
            "Assets/_Project/Art/Enemies/Negatif/Controllers";
        private const string MoveClipPath =
            AnimationFolder + "/Negatif_02_Move_Quadruped.anim";
        private const string MoveControllerPath =
            ControllerFolder + "/Negatif_02_Move_Quadruped.controller";
        private const string MoveStateName = "MoveQuadruped";
        private const string ReviewPath = "Logs/Negatif_Move_VisualReview.png";
        private const int PanelWidth = 640;
        private const int PanelHeight = 640;
        private const float MoveLoopSeconds = 1f;
        private const float StrideLength = 0.75f;
        private const float FootLift = 0.28f;
        private const float BodyBob = 0.05f;

        private static readonly float[] ReferencePhases =
        {
            0f,
            0.25f,
            0.5f,
            0.75f,
            0.999f
        };

        private static readonly float[] PosePhases =
        {
            0f,
            0.125f,
            0.25f,
            0.375f,
            0.5f,
            0.625f,
            0.75f,
            0.875f,
            1f
        };

        [MenuItem("Bellerophon/Enemies/Negatif/Apply Move Animation")]
        public static void ApplyMoveAnimation()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the current active scene.");
            }

            var placementRoot = GameObject.Find(NegatifPlacementRootName) ??
                                throw new InvalidOperationException(
                                    NegatifPlacementRootName + " is missing.");
            var slot = placementRoot.transform.Find(NegatifMoveSlotName) ??
                       throw new InvalidOperationException(
                           NegatifMoveSlotName + " is missing.");
            var model = slot.Find(NegatifModelName) ??
                        throw new InvalidOperationException(
                            NegatifModelName + " is missing under " +
                            NegatifMoveSlotName + ".");

            EnsureFolder(AnimationFolder);
            EnsureFolder(ControllerFolder);
            var clip = CreateMoveClip(slot, model);
            var controller = CreateMoveController(clip);
            var animator = slot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = slot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(slot);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after Negatif move animation application.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "NegatifMoveAnimationApplied " +
                "Slot=" + NegatifMoveSlotName +
                ", Clip=" + MoveClipPath +
                ", Controller=" + MoveControllerPath +
                ", LoopSeconds=" + MoveLoopSeconds.ToString("0.###") +
                ", Gait=DiagonalQuadruped" +
                ", Legs=Bone_030/Bone_035/Bone_006/Bone_011" +
                ", TailCurves=0" +
                ", RootMotion=False" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Negatif/Capture Move Visual Review")]
        public static void CaptureMoveVisualReview()
        {
            var placementRoot = GameObject.Find(DolorePlacementRootName) ??
                                throw new InvalidOperationException(
                                    DolorePlacementRootName + " is missing.");
            var slot = placementRoot.transform.Find(DoloreMoveSlotName) ??
                       throw new InvalidOperationException(
                           DoloreMoveSlotName + " is missing.");
            var model = slot.Find(DoloreModelName) ??
                        throw new InvalidOperationException(
                            DoloreModelName + " is missing under " + DoloreMoveSlotName + ".");
            var animation = model.GetComponentInChildren<Animation>(true) ??
                            throw new InvalidOperationException(
                                DoloreMoveSlotName + " has no legacy Animation component.");
            var clip = animation.clip;
            if (clip == null)
            {
                foreach (AnimationState state in animation)
                {
                    clip = state.clip;
                    break;
                }
            }

            if (clip == null || clip.length <= 0f)
            {
                throw new InvalidOperationException(
                    DoloreMoveSlotName + " has no playable embedded FBX clip.");
            }

            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var panelPaths = new List<string>();
            try
            {
                for (var index = 0; index < ReferencePhases.Length; index++)
                {
                    var time = clip.length * ReferencePhases[index];
                    clip.SampleAnimation(animation.gameObject, time);
                    var panelPath = Absolute(
                        "Logs/Negatif_Move_DoloreReference_" + index + ".png");
                    CapturePanel(slot, panelPath);
                    panelPaths.Add(panelPath);
                }

                var negatifClip =
                    AssetDatabase.LoadAssetAtPath<AnimationClip>(MoveClipPath);
                if (negatifClip != null)
                {
                    var negatifRoot = GameObject.Find(NegatifPlacementRootName) ??
                                      throw new InvalidOperationException(
                                          NegatifPlacementRootName + " is missing.");
                    var negatifSlot =
                        negatifRoot.transform.Find(NegatifMoveSlotName) ??
                        throw new InvalidOperationException(
                            NegatifMoveSlotName + " is missing.");
                    var negatifSnapshots =
                        negatifSlot.GetComponentsInChildren<Transform>(true)
                            .Select(item => new TransformSnapshot(item))
                            .ToArray();
                    try
                    {
                        for (var index = 0; index < ReferencePhases.Length; index++)
                        {
                            var time = negatifClip.length * ReferencePhases[index];
                            negatifClip.SampleAnimation(negatifSlot.gameObject, time);
                            var panelPath = Absolute(
                                "Logs/Negatif_Move_NegatifPreview_" + index + ".png");
                            CapturePanel(negatifSlot, panelPath);
                            panelPaths.Add(panelPath);
                        }
                    }
                    finally
                    {
                        foreach (var snapshot in negatifSnapshots)
                        {
                            snapshot.Restore();
                        }
                    }
                }

                ComposePanels(
                    panelPaths,
                    Absolute(ReviewPath),
                    ReferencePhases.Length);
            }
            finally
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }

                foreach (var panelPath in panelPaths)
                {
                    if (File.Exists(panelPath))
                    {
                        File.Delete(panelPath);
                    }
                }
            }

            Debug.Log(
                "NegatifMoveDoloreReferenceCaptured " +
                "Clip=" + clip.name +
                ", ClipLength=" + clip.length.ToString("0.###") +
                ", Phases=0_0.25_0.5_0.75_0.999" +
                ", NegatifPanels=" +
                (AssetDatabase.LoadAssetAtPath<AnimationClip>(MoveClipPath) != null
                    ? "5"
                    : "0") +
                ", Image=" + ReviewPath +
                ", SceneChanged=False.\n" +
                BuildNegatifRigSummary());
        }

        internal static void CaptureRuntimeFrame(bool dolore, string path)
        {
            var rootName = dolore
                ? DolorePlacementRootName
                : NegatifPlacementRootName;
            var slotName = dolore
                ? DoloreMoveSlotName
                : NegatifMoveSlotName;
            var placementRoot = GameObject.Find(rootName) ??
                                throw new InvalidOperationException(
                                    rootName + " is missing in Play Mode.");
            var slot = placementRoot.transform.Find(slotName) ??
                       throw new InvalidOperationException(
                           slotName + " is missing in Play Mode.");
            CapturePanel(slot, path);
        }

        internal static void ComposeRuntimeReview(
            IReadOnlyList<string> panelPaths,
            string outputPath)
        {
            ComposePanels(
                panelPaths,
                outputPath,
                ReferencePhases.Length);
        }

        private static AnimationClip CreateMoveClip(
            Transform slot,
            Transform model)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MoveClipPath) != null)
            {
                AssetDatabase.DeleteAsset(MoveClipPath);
            }

            var body = RequireDescendant(model, "Bone_001");
            var legs = new[]
            {
                CreateLeg(model, "FrontLeft", 0f,
                    "Bone_030", "Bone_029", "Bone_028", "Bone_027", "Bone_026"),
                CreateLeg(model, "RearRight", 0f,
                    "Bone_011", "Bone_010", "Bone_009", "Bone_008", "Bone_007"),
                CreateLeg(model, "FrontRight", 0.5f,
                    "Bone_035", "Bone_034", "Bone_033", "Bone_032", "Bone_031"),
                CreateLeg(model, "RearLeft", 0.5f,
                    "Bone_006", "Bone_005", "Bone_004", "Bone_003", "Bone_002")
            };
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var bodyBasePosition = body.localPosition;
            var bodyPositionKeys = new List<Keyframe>();
            var rotationKeys = new Dictionary<Transform, List<QuaternionKey>>();
            foreach (var leg in legs)
            {
                foreach (var joint in leg.AllBones)
                {
                    if (!rotationKeys.ContainsKey(joint))
                    {
                        rotationKeys.Add(joint, new List<QuaternionKey>());
                    }
                }
            }

            try
            {
                foreach (var cycle in PosePhases)
                {
                    foreach (var snapshot in snapshots)
                    {
                        snapshot.Restore();
                    }

                    var time = cycle * MoveLoopSeconds;
                    body.localPosition = bodyBasePosition +
                                         Vector3.up *
                                         (Mathf.Abs(Mathf.Sin(cycle * Mathf.PI * 2f)) *
                                          BodyBob);
                    bodyPositionKeys.Add(
                        new Keyframe(time, body.localPosition.y));

                    foreach (var leg in legs)
                    {
                        var phase = Mathf.Repeat(cycle + leg.PhaseOffset, 1f);
                        var targetOffset = FootTrajectory(phase);
                        var target =
                            model.TransformPoint(leg.RestFootModelPosition + targetOffset);
                        SolveCcd(leg.Joints, leg.Foot, target);
                        leg.Foot.rotation =
                            model.rotation * leg.RestFootModelRotation;
                    }

                    foreach (var pair in rotationKeys)
                    {
                        pair.Value.Add(
                            new QuaternionKey(time, pair.Key.localRotation));
                    }
                }
            }
            finally
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }
            }

            var clip = new AnimationClip
            {
                name = "Negatif_02_Move_Quadruped",
                frameRate = 60f
            };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            SetLinearCurve(
                clip,
                AnimationUtility.CalculateTransformPath(body, slot),
                "m_LocalPosition.y",
                bodyPositionKeys);
            foreach (var pair in rotationKeys)
            {
                SetQuaternionCurves(
                    clip,
                    AnimationUtility.CalculateTransformPath(pair.Key, slot),
                    pair.Value);
            }

            clip.EnsureQuaternionContinuity();
            AssetDatabase.CreateAsset(clip, MoveClipPath);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static LegChain CreateLeg(
            Transform model,
            string label,
            float phaseOffset,
            params string[] boneNames)
        {
            var bones = boneNames
                .Select(name => RequireDescendant(model, name))
                .ToArray();
            return new LegChain(
                label,
                phaseOffset,
                bones.Take(bones.Length - 1).ToArray(),
                bones[bones.Length - 1],
                model.InverseTransformPoint(bones[bones.Length - 1].position),
                Quaternion.Inverse(model.rotation) *
                bones[bones.Length - 1].rotation);
        }

        private static Vector3 FootTrajectory(float phase)
        {
            if (phase < 0.6f)
            {
                var stance = phase / 0.6f;
                return new Vector3(
                    0f,
                    0f,
                    Mathf.Lerp(
                        StrideLength * 0.5f,
                        -StrideLength * 0.5f,
                        stance));
            }

            var swing = (phase - 0.6f) / 0.4f;
            var eased = swing * swing * (3f - 2f * swing);
            return new Vector3(
                0f,
                Mathf.Sin(swing * Mathf.PI) * FootLift,
                Mathf.Lerp(
                    -StrideLength * 0.5f,
                    StrideLength * 0.5f,
                    eased));
        }

        private static void SolveCcd(
            IReadOnlyList<Transform> joints,
            Transform foot,
            Vector3 target)
        {
            for (var iteration = 0; iteration < 18; iteration++)
            {
                for (var index = joints.Count - 1; index >= 0; index--)
                {
                    var joint = joints[index];
                    var toFoot = foot.position - joint.position;
                    var toTarget = target - joint.position;
                    if (toFoot.sqrMagnitude < 0.000001f ||
                        toTarget.sqrMagnitude < 0.000001f)
                    {
                        continue;
                    }

                    var delta = Quaternion.FromToRotation(toFoot, toTarget);
                    delta = Quaternion.RotateTowards(
                        Quaternion.identity,
                        delta,
                        12f);
                    joint.rotation = delta * joint.rotation;
                }

                if ((foot.position - target).sqrMagnitude < 0.000004f)
                {
                    break;
                }
            }
        }

        private static AnimatorController CreateMoveController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MoveControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(MoveControllerPath);
            }

            var controller =
                AnimatorController.CreateAnimatorControllerAtPath(MoveControllerPath);
            var state = controller.layers[0].stateMachine.AddState(MoveStateName);
            state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void SetQuaternionCurves(
            AnimationClip clip,
            string path,
            IReadOnlyList<QuaternionKey> values)
        {
            var continuityValues = new List<QuaternionKey>(values.Count);
            Quaternion? previous = null;
            foreach (var value in values)
            {
                var rotation = value.Rotation;
                if (previous.HasValue &&
                    Quaternion.Dot(previous.Value, rotation) < 0f)
                {
                    rotation = new Quaternion(
                        -rotation.x,
                        -rotation.y,
                        -rotation.z,
                        -rotation.w);
                }

                continuityValues.Add(
                    new QuaternionKey(value.Time, rotation));
                previous = rotation;
            }

            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.x",
                continuityValues.Select(
                    value => new Keyframe(value.Time, value.Rotation.x)).ToList());
            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.y",
                continuityValues.Select(
                    value => new Keyframe(value.Time, value.Rotation.y)).ToList());
            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.z",
                continuityValues.Select(
                    value => new Keyframe(value.Time, value.Rotation.z)).ToList());
            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.w",
                continuityValues.Select(
                    value => new Keyframe(value.Time, value.Rotation.w)).ToList());
        }

        private static void SetLinearCurve(
            AnimationClip clip,
            string path,
            string property,
            IList<Keyframe> keys)
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

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    property),
                curve);
        }

        private static Transform RequireDescendant(
            Transform root,
            string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one " + name +
                    " under " + root.name +
                    ", found " + matches.Length + ".");
            }

            return matches[0];
        }

        private static string BuildNegatifRigSummary()
        {
            var placementRoot = GameObject.Find(NegatifPlacementRootName) ??
                                throw new InvalidOperationException(
                                    NegatifPlacementRootName + " is missing.");
            var slot = placementRoot.transform.Find(NegatifMoveSlotName) ??
                       throw new InvalidOperationException(
                           NegatifMoveSlotName + " is missing.");
            var model = slot.Find(NegatifModelName) ??
                        throw new InvalidOperationException(
                            NegatifModelName + " is missing under " +
                            NegatifMoveSlotName + ".");
            var lines = model.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name.StartsWith("Bone_", StringComparison.Ordinal))
                .Select(item =>
                {
                    var position = model.InverseTransformPoint(item.position);
                    var parentName =
                        item.parent != null &&
                        item.parent.name.StartsWith("Bone_", StringComparison.Ordinal)
                            ? item.parent.name
                            : "-";
                    return item.name +
                           "|Parent=" + parentName +
                           "|Position=(" +
                           position.x.ToString("0.###") + "," +
                           position.y.ToString("0.###") + "," +
                           position.z.ToString("0.###") + ")";
                })
                .OrderBy(item => item, StringComparer.Ordinal);
            return "NegatifRigHierarchy\n" + string.Join("\n", lines);
        }

        private static void CapturePanel(Transform slot, string path)
        {
            var bounds = BoundsOf(slot);
            var hiddenRenderers = UnityEngine.Object
                .FindObjectsByType<Renderer>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(item =>
                    item.enabled &&
                    !item.transform.IsChildOf(slot))
                .ToArray();
            foreach (var renderer in hiddenRenderers)
            {
                renderer.enabled = false;
            }

            var player = GameObject.Find(PlayerName);
            var sourceCamera = player != null
                ? player.GetComponentInChildren<Camera>(true)
                : null;
            var cameraObject = new GameObject(
                "NegatifMoveReferenceCaptureCamera",
                typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var keyLightObject = new GameObject(
                "NegatifMoveReferenceKeyLight",
                typeof(Light))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var fillLightObject = new GameObject(
                "NegatifMoveReferenceFillLight",
                typeof(Light))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                if (sourceCamera != null)
                {
                    camera.CopyFrom(sourceCamera);
                }

                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.015f, 0.02f, 0.025f, 1f);
                camera.fieldOfView = 34f;
                camera.aspect = 1f;
                camera.orthographic = true;
                camera.orthographicSize =
                    Mathf.Max(0.2f, bounds.extents.magnitude * 1.12f);
                camera.nearClipPlane = 0.005f;
                camera.farClipPlane = 100f;

                var front = slot.forward.normalized;
                var right = slot.right.normalized;
                var distance = Mathf.Max(
                    1f,
                    bounds.extents.magnitude * 4f);
                var focus = bounds.center + Vector3.up * bounds.extents.y * -0.08f;
                camera.transform.position =
                    focus + front * distance * 0.35f + right * distance +
                    Vector3.up * bounds.extents.y * 0.18f;
                camera.transform.rotation = Quaternion.LookRotation(
                    focus - camera.transform.position,
                    Vector3.up);

                var keyLight = keyLightObject.GetComponent<Light>();
                keyLight.type = LightType.Directional;
                keyLight.color = new Color(0.78f, 0.9f, 1f);
                keyLight.intensity = 2.2f;
                keyLight.transform.rotation = Quaternion.Euler(42f, -32f, 0f);

                var fillLight = fillLightObject.GetComponent<Light>();
                fillLight.type = LightType.Point;
                fillLight.color = new Color(0.35f, 0.75f, 1f);
                fillLight.intensity = 9f;
                fillLight.range = distance * 2.5f;
                fillLight.transform.position =
                    focus - front * distance * 0.35f - right * distance * 0.25f +
                    Vector3.up * bounds.extents.y * 0.4f;

                Capture(camera, path, PanelWidth, PanelHeight);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fillLightObject);
                UnityEngine.Object.DestroyImmediate(keyLightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                foreach (var renderer in hiddenRenderers)
                {
                    if (renderer != null)
                    {
                        renderer.enabled = true;
                    }
                }
            }
        }

        private static Bounds BoundsOf(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled && item.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(root.name + " has no visible renderer.");
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static void Capture(Camera camera, string path, int width, int height)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(path) ??
                throw new InvalidOperationException("Invalid Negatif move capture folder."));
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
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

        private static void ComposePanels(
            IReadOnlyList<string> panelPaths,
            string outputPath,
            int columns)
        {
            var panels = new List<Texture2D>();
            var rows = Mathf.CeilToInt(panelPaths.Count / (float)columns);
            var review = new Texture2D(
                PanelWidth * columns,
                PanelHeight * rows,
                TextureFormat.RGB24,
                false);
            try
            {
                for (var index = 0; index < panelPaths.Count; index++)
                {
                    var panel = new Texture2D(2, 2, TextureFormat.RGB24, false);
                    if (!panel.LoadImage(File.ReadAllBytes(panelPaths[index])))
                    {
                        UnityEngine.Object.DestroyImmediate(panel);
                        throw new InvalidOperationException(
                            "Could not load Negatif move review panel.");
                    }

                    panels.Add(panel);
                    var column = index % columns;
                    var rowFromTop = index / columns;
                    var row = rows - 1 - rowFromTop;
                    review.SetPixels32(
                        column * PanelWidth,
                        row * PanelHeight,
                        PanelWidth,
                        PanelHeight,
                        panel.GetPixels32());
                }

                review.Apply();
                Directory.CreateDirectory(
                    Path.GetDirectoryName(outputPath) ??
                    throw new InvalidOperationException(
                        "Invalid Negatif move review output folder."));
                File.WriteAllBytes(outputPath, review.EncodeToPNG());
            }
            finally
            {
                foreach (var panel in panels)
                {
                    UnityEngine.Object.DestroyImmediate(panel);
                }

                UnityEngine.Object.DestroyImmediate(review);
            }
        }

        private static string Absolute(string relative)
        {
            return Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", relative));
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }

        private sealed class LegChain
        {
            public readonly string Label;
            public readonly float PhaseOffset;
            public readonly Transform[] Joints;
            public readonly Transform Foot;
            public readonly Vector3 RestFootModelPosition;
            public readonly Quaternion RestFootModelRotation;

            public LegChain(
                string label,
                float phaseOffset,
                Transform[] joints,
                Transform foot,
                Vector3 restFootModelPosition,
                Quaternion restFootModelRotation)
            {
                Label = label;
                PhaseOffset = phaseOffset;
                Joints = joints;
                Foot = foot;
                RestFootModelPosition = restFootModelPosition;
                RestFootModelRotation = restFootModelRotation;
            }

            public IEnumerable<Transform> AllBones
            {
                get
                {
                    foreach (var joint in Joints)
                    {
                        yield return joint;
                    }

                    yield return Foot;
                }
            }
        }

        private readonly struct QuaternionKey
        {
            public readonly float Time;
            public readonly Quaternion Rotation;

            public QuaternionKey(float time, Quaternion rotation)
            {
                Time = time;
                Rotation = rotation;
            }
        }

        private sealed class TransformSnapshot
        {
            private readonly Transform target;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            public TransformSnapshot(Transform target)
            {
                this.target = target;
                localPosition = target.localPosition;
                localRotation = target.localRotation;
                localScale = target.localScale;
            }

            public void Restore()
            {
                if (target == null)
                {
                    return;
                }

                target.localPosition = localPosition;
                target.localRotation = localRotation;
                target.localScale = localScale;
            }
        }
    }
}
