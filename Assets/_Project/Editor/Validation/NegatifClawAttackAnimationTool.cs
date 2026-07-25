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
    internal static class NegatifClawAttackAnimationTool
    {
        private const string PlacementRootName = "Approved Negatif Enemy Placement";
        private const string AttackSlotName = "Negatif_03_Claw_Attack";
        private const string ModelName = "Negatif_Model";
        private const string PlayerName = "Player";
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string AnimationFolder =
            "Assets/_Project/Art/Enemies/Negatif/Animations";
        private const string ControllerFolder =
            "Assets/_Project/Art/Enemies/Negatif/Controllers";
        private const string AttackClipPath =
            AnimationFolder + "/Negatif_03_Claw_Attack_Alternating.anim";
        private const string AttackControllerPath =
            ControllerFolder + "/Negatif_03_Claw_Attack_Alternating.controller";
        private const string AttackStateName = "ClawAttack";
        private const float AttackSeconds = 3.9f;
        // One set preserves the approved left-then-right timing; five starts are spaced 0.5 seconds apart.
        private const int AttackSetCount = 5;
        private const float FirstAttackStartSeconds = 0.75f;
        private const float AttackSetIntervalSeconds = 0.5f;
        private const float UprightAngle = 48f;
        private const float CrouchDropFactor = 0.18f;
        private const float RearSettleFactor = 0.06f;
        private const float RearHipCompensation = 0.72f;
        private const float RearKneeBend = 0.36f;
        private const float RearLowerLegCounterBend = 0.18f;
        private const int PanelWidth = 520;
        private const int PanelHeight = 600;

        private static readonly PoseKey[] PoseKeys = BuildPoseKeys();

        private static PoseKey[] BuildPoseKeys()
        {
            var keys = new List<PoseKey>
            {
                new PoseKey(0f, 0f, 0f, 0f),
                new PoseKey(0.35f, 0.55f, 0f, 0f),
                new PoseKey(0.65f, 1f, 0f, 0f)
            };
            for (var setIndex = 0; setIndex < AttackSetCount; setIndex++)
            {
                var start =
                    FirstAttackStartSeconds +
                    setIndex * AttackSetIntervalSeconds;
                keys.Add(new PoseKey(start, 1f, -1f, 0f));
                keys.Add(new PoseKey(start + 0.075f, 1f, 1f, 0f));
                keys.Add(new PoseKey(start + 0.15f, 1f, 0f, 0f));
                keys.Add(new PoseKey(start + 0.25f, 1f, 0f, -1f));
                keys.Add(new PoseKey(start + 0.325f, 1f, 0f, 1f));
                keys.Add(new PoseKey(start + 0.4f, 1f, 0f, 0f));
            }

            keys.Add(new PoseKey(3.4f, 1f, 0f, 0f));
            keys.Add(new PoseKey(3.65f, 0.55f, 0f, 0f));
            keys.Add(new PoseKey(AttackSeconds, 0f, 0f, 0f));
            return keys.ToArray();
        }

        [MenuItem("Bellerophon/Enemies/Negatif/Apply Claw Attack Animation")]
        public static void ApplyClawAttackAnimation()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the current active scene.");
            }

            var placementRoot = GameObject.Find(PlacementRootName) ??
                                throw new InvalidOperationException(
                                    PlacementRootName + " is missing.");
            var slot = placementRoot.transform.Find(AttackSlotName) ??
                       throw new InvalidOperationException(
                           AttackSlotName + " is missing.");
            var model = slot.Find(ModelName) ??
                        throw new InvalidOperationException(
                            ModelName + " is missing under " + AttackSlotName + ".");

            EnsureFolder(AnimationFolder);
            EnsureFolder(ControllerFolder);
            var clip = CreateAttackClip(slot, model);
            var controller = CreateAttackController(clip);
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
                    "CargoRunMvp could not be saved after Negatif claw attack application.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "NegatifClawAttackAnimationApplied " +
                "Slot=" + AttackSlotName +
                ", Clip=" + AttackClipPath +
                ", Controller=" + AttackControllerPath +
                ", Duration=" + AttackSeconds.ToString("0.###") +
                ", Sequence=RearSupport_Upright_FiveLeftRightClawSets_QuadrupedReturn" +
                ", ClawSetCount=" + AttackSetCount +
                ", RearLegs=Bone_006/Bone_011" +
                ", FrontLegs=Bone_030/Bone_035" +
                ", RearMotion=RotationOnly" +
                ", RearCcd=False" +
                ", RearLocalPositionCurves=0" +
                ", RearFootWorldLock=Bone_003/Bone_002+Bone_008/Bone_007" +
                ", FrontProximalCurves=0" +
                ", FrontCcd=False" +
                ", FrontAnimatedHinges=Bone_030+Bone_035" +
                ", FrontDescendants=RigidInheritedLocal" +
                ", FrontClawMotion=ShoulderHinge" +
                ", FrontReadyAngle=0" +
                ", FrontClawSwingDegrees=60" +
                ", FrontClawDirection=ForwardDown" +
                ", BodyRenderers=1" +
                ", SeparateWhiskerObject=False" +
                ", UprightAngle=" + UprightAngle.ToString("0.###") +
                ", CrouchDropFactor=" + CrouchDropFactor.ToString("0.###") +
                ", RigidMeshPose=Negatif_Model" +
                ", Bone001Curves=0" +
                ", FaceWhiskerCurves=0" +
                ", ClawSpeedMultiplier=2" +
                ", TailCurves=0" +
                ", RootMotion=False" +
                ", SceneSaved=True.");
        }

        internal static void CaptureRuntimeFrame(string path)
        {
            var placementRoot = GameObject.Find(PlacementRootName) ??
                                throw new InvalidOperationException(
                                    PlacementRootName + " is missing in Play Mode.");
            var slot = placementRoot.transform.Find(AttackSlotName) ??
                       throw new InvalidOperationException(
                           AttackSlotName + " is missing in Play Mode.");
            CapturePanel(slot, path);
        }

        internal static void ComposeRuntimeReview(
            IReadOnlyList<string> panelPaths,
            string outputPath)
        {
            const int columns = 4;
            var rows = Mathf.CeilToInt(panelPaths.Count / (float)columns);
            var sheet = new Texture2D(
                PanelWidth * columns,
                PanelHeight * rows,
                TextureFormat.RGBA32,
                false);
            var background = Enumerable.Repeat(
                    new Color32(4, 6, 8, 255),
                    sheet.width * sheet.height)
                .ToArray();
            sheet.SetPixels32(background);

            try
            {
                for (var index = 0; index < panelPaths.Count; index++)
                {
                    var bytes = File.ReadAllBytes(panelPaths[index]);
                    var panel = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    try
                    {
                        if (!panel.LoadImage(bytes))
                        {
                            throw new InvalidOperationException(
                                "Could not decode attack panel " + panelPaths[index] + ".");
                        }

                        var column = index % columns;
                        var rowFromTop = index / columns;
                        var y = (rows - rowFromTop - 1) * PanelHeight;
                        sheet.SetPixels(
                            column * PanelWidth,
                            y,
                            PanelWidth,
                            PanelHeight,
                            panel.GetPixels());
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(panel);
                    }
                }

                sheet.Apply(false, false);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static AnimationClip CreateAttackClip(
            Transform slot,
            Transform model)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AttackClipPath) != null)
            {
                AssetDatabase.DeleteAsset(AttackClipPath);
            }

            var frontLeft = CreateFrontLeg(
                model,
                "FrontLeft",
                false,
                "Bone_030", "Bone_029", "Bone_028", "Bone_027", "Bone_026");
            var frontRight = CreateFrontLeg(
                model,
                "FrontRight",
                true,
                "Bone_035", "Bone_034", "Bone_033", "Bone_032", "Bone_031");
            var rearLeft = CreateRearLeg(
                model,
                "RearLeft",
                "Bone_006", "Bone_005", "Bone_004", "Bone_003", "Bone_002");
            var rearRight = CreateRearLeg(
                model,
                "RearRight",
                "Bone_011", "Bone_010", "Bone_009", "Bone_008", "Bone_007");
            var legs = new[] { frontLeft, frontRight, rearLeft, rearRight };
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var modelBaseWorldPosition = model.position;
            var modelBaseWorldRotation = model.rotation;
            var frontCenter =
                (frontLeft.RestFootWorldPosition + frontRight.RestFootWorldPosition) *
                0.5f;
            var rearCenter =
                (rearLeft.RestFootWorldPosition + rearRight.RestFootWorldPosition) *
                0.5f;
            var groundForward = Vector3.ProjectOnPlane(
                slot.forward,
                Vector3.up).normalized;
            if (groundForward.sqrMagnitude < 0.5f)
            {
                throw new InvalidOperationException(
                    "Negatif front-to-rear rig direction could not be resolved.");
            }

            var pitchAxis = Vector3.Cross(groundForward, Vector3.up).normalized;
            var bodyLength = Vector3.Distance(frontCenter, rearCenter);
            var modelPositionKeys = new List<VectorKey>();
            var modelRotationKeys = new List<QuaternionKey>();
            var rotationKeys = new Dictionary<Transform, List<QuaternionKey>>();
            // The malformed right-claw connector stays collapsed by recording its locked terminal position.
            var lockedFootPositionKeys = new Dictionary<Transform, List<VectorKey>>
            {
                { frontRight.Foot, new List<VectorKey>() }
            };
            foreach (var leg in legs)
            {
                foreach (var bone in leg.AllBones)
                {
                    if (!rotationKeys.ContainsKey(bone))
                    {
                        rotationKeys.Add(bone, new List<QuaternionKey>());
                    }
                }
            }

            try
            {
                foreach (var pose in PoseKeys)
                {
                    foreach (var snapshot in snapshots)
                    {
                        snapshot.Restore();
                    }

                    var rise = Smooth01(pose.Rise);
                    var strikeWeight = Mathf.Max(
                        Mathf.Max(0f, pose.LeftClaw),
                        Mathf.Max(0f, pose.RightClaw));
                    var windupWeight = Mathf.Max(
                        Mathf.Max(0f, -pose.LeftClaw),
                        Mathf.Max(0f, -pose.RightClaw));
                    var rigidPitch = Quaternion.AngleAxis(
                        (UprightAngle * rise) -
                        (6f * strikeWeight) +
                        (2f * windupWeight),
                        pitchAxis);
                    model.rotation =
                        rigidPitch * modelBaseWorldRotation;
                    model.position =
                        rearCenter +
                        rigidPitch *
                        (modelBaseWorldPosition - rearCenter) -
                        Vector3.up *
                        (bodyLength * CrouchDropFactor * rise) -
                        groundForward *
                        (bodyLength * RearSettleFactor * rise) +
                        groundForward *
                        (bodyLength * 0.05f * strikeWeight);

                    PoseRearLeg(rearLeft, rise, pitchAxis);
                    PoseRearLeg(rearRight, rise, pitchAxis);

                    PoseFrontLeg(
                        frontLeft,
                        rise,
                        pose.LeftClaw,
                        pitchAxis);
                    PoseFrontLeg(
                        frontRight,
                        rise,
                        pose.RightClaw,
                        pitchAxis);

                    modelPositionKeys.Add(
                        new VectorKey(pose.Time, model.localPosition));
                    modelRotationKeys.Add(
                        new QuaternionKey(pose.Time, model.localRotation));
                    foreach (var pair in rotationKeys)
                    {
                        pair.Value.Add(
                            new QuaternionKey(pose.Time, pair.Key.localRotation));
                    }

                    foreach (var pair in lockedFootPositionKeys)
                    {
                        pair.Value.Add(
                            new VectorKey(pose.Time, pair.Key.localPosition));
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
                name = "Negatif_03_Claw_Attack_Alternating",
                frameRate = 60f
            };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            var modelPath = AnimationUtility.CalculateTransformPath(model, slot);
            SetVectorCurves(clip, modelPath, "m_LocalPosition", modelPositionKeys);
            SetQuaternionCurves(clip, modelPath, modelRotationKeys);
            foreach (var pair in rotationKeys)
            {
                SetQuaternionCurves(
                    clip,
                    AnimationUtility.CalculateTransformPath(pair.Key, slot),
                    pair.Value);
            }
            foreach (var pair in lockedFootPositionKeys)
            {
                SetVectorCurves(
                    clip,
                    AnimationUtility.CalculateTransformPath(pair.Key, slot),
                    "m_LocalPosition",
                    pair.Value);
            }
            clip.EnsureQuaternionContinuity();
            AssetDatabase.CreateAsset(clip, AttackClipPath);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void PoseFrontLeg(
            LegChain leg,
            float rise,
            float clawPhase,
            Vector3 pitchAxis)
        {
            if (leg.Joints.Length != 3)
            {
                throw new InvalidOperationException(
                    leg.Label + " must distribute its swing across three claw joints.");
            }

            var actionAngle = -30f * clawPhase;
            var jointAngle = actionAngle / leg.Joints.Length;
            var lockedFootPosition = leg.Foot.position;
            var lockedFootRotation = leg.Foot.rotation;
            foreach (var joint in leg.Joints)
            {
                joint.rotation =
                    Quaternion.AngleAxis(jointAngle, pitchAxis) *
                    joint.rotation;
            }
            if (leg.LockFootDuringSwing)
            {
                leg.Foot.position = lockedFootPosition;
                leg.Foot.rotation = lockedFootRotation;
            }
        }

        private static void PoseRearLeg(
            LegChain leg,
            float rise,
            Vector3 pitchAxis)
        {
            if (leg.Ankle == null || leg.Joints.Length < 3)
            {
                throw new InvalidOperationException(
                    leg.Label + " is missing its rotation-only rear chain.");
            }

            var compensation = UprightAngle * rise;
            leg.Joints[0].rotation =
                Quaternion.AngleAxis(
                    -compensation * RearHipCompensation,
                    pitchAxis) *
                leg.Joints[0].rotation;
            leg.Joints[1].rotation =
                Quaternion.AngleAxis(
                    compensation * RearKneeBend,
                    pitchAxis) *
                leg.Joints[1].rotation;
            leg.Joints[2].rotation =
                Quaternion.AngleAxis(
                    -compensation * RearLowerLegCounterBend,
                    pitchAxis) *
                leg.Joints[2].rotation;
            leg.Ankle.rotation = leg.RestAnkleWorldRotation;
            leg.Foot.rotation = leg.RestFootWorldRotation;
        }

        private static LegChain CreateFrontLeg(
            Transform model,
            string label,
            bool lockFootDuringSwing,
            params string[] boneNames)
        {
            var bones = boneNames
                .Select(name => RequireDescendant(model, name))
                .ToArray();
            var joints = bones.Take(3).ToArray();

            return new LegChain(
                label,
                joints,
                bones[0],
                lockFootDuringSwing,
                null,
                bones[bones.Length - 1],
                Quaternion.identity,
                bones[bones.Length - 1].position,
                bones[bones.Length - 1].rotation);
        }

        private static LegChain CreateRearLeg(
            Transform model,
            string label,
            params string[] boneNames)
        {
            var bones = boneNames
                .Select(name => RequireDescendant(model, name))
                .ToArray();
            var ankle = bones[bones.Length - 2];
            var foot = bones[bones.Length - 1];
            return new LegChain(
                label,
                bones.Take(bones.Length - 2).ToArray(),
                null,
                false,
                ankle,
                foot,
                ankle.rotation,
                foot.position,
                foot.rotation);
        }

        private static AnimatorController CreateAttackController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AttackControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(AttackControllerPath);
            }

            var controller =
                AnimatorController.CreateAnimatorControllerAtPath(AttackControllerPath);
            var state = controller.layers[0].stateMachine.AddState(AttackStateName);
            state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void SetVectorCurves(
            AnimationClip clip,
            string path,
            string propertyPrefix,
            IReadOnlyList<VectorKey> values)
        {
            SetLinearCurve(
                clip,
                path,
                propertyPrefix + ".x",
                values.Select(value => new Keyframe(value.Time, value.Value.x)).ToList());
            SetLinearCurve(
                clip,
                path,
                propertyPrefix + ".y",
                values.Select(value => new Keyframe(value.Time, value.Value.y)).ToList());
            SetLinearCurve(
                clip,
                path,
                propertyPrefix + ".z",
                values.Select(value => new Keyframe(value.Time, value.Value.z)).ToList());
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

                continuityValues.Add(new QuaternionKey(value.Time, rotation));
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

        private static Transform RequireDescendant(Transform root, string name)
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

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
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
                "NegatifClawAttackCaptureCamera",
                typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var keyLightObject = new GameObject(
                "NegatifClawAttackKeyLight",
                typeof(Light))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var fillLightObject = new GameObject(
                "NegatifClawAttackFillLight",
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
                camera.aspect = PanelWidth / (float)PanelHeight;
                camera.orthographic = true;
                camera.orthographicSize =
                    Mathf.Max(0.26f, bounds.extents.y * 0.9f);
                camera.nearClipPlane = 0.005f;
                camera.farClipPlane = 100f;

                var front = slot.forward.normalized;
                var right = slot.right.normalized;
                var distance = Mathf.Max(1f, bounds.extents.magnitude * 4f);
                var focus =
                    bounds.center +
                    Vector3.up * (bounds.extents.y * 0.25f);
                camera.transform.position =
                    focus +
                    front * distance * 0.75f +
                    right * distance * 0.45f +
                    Vector3.up * bounds.extents.y * 0.12f;
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
                    focus -
                    front * distance * 0.35f -
                    right * distance * 0.25f +
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
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    root.name + " has no renderers.");
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static void Capture(
            Camera camera,
            string path,
            int width,
            int height)
        {
            var renderTexture = RenderTexture.GetTemporary(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            var previousTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                var texture = new Texture2D(
                    width,
                    height,
                    TextureFormat.RGBA32,
                    false);
                try
                {
                    texture.ReadPixels(
                        new Rect(0f, 0f, width, height),
                        0,
                        0,
                        false);
                    texture.Apply(false, false);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
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
            public readonly Transform[] Joints;
            public readonly Transform SwingPivot;
            public readonly bool LockFootDuringSwing;
            public readonly Transform Ankle;
            public readonly Transform Foot;
            public readonly Quaternion RestAnkleWorldRotation;
            public readonly Vector3 RestFootWorldPosition;
            public readonly Quaternion RestFootWorldRotation;

            public LegChain(
                string label,
                Transform[] joints,
                Transform swingPivot,
                bool lockFootDuringSwing,
                Transform ankle,
                Transform foot,
                Quaternion restAnkleWorldRotation,
                Vector3 restFootWorldPosition,
                Quaternion restFootWorldRotation)
            {
                Label = label;
                Joints = joints;
                SwingPivot = swingPivot;
                LockFootDuringSwing = lockFootDuringSwing;
                Ankle = ankle;
                Foot = foot;
                RestAnkleWorldRotation = restAnkleWorldRotation;
                RestFootWorldPosition = restFootWorldPosition;
                RestFootWorldRotation = restFootWorldRotation;
            }

            public IEnumerable<Transform> AllBones
            {
                get
                {
                    foreach (var joint in Joints)
                    {
                        yield return joint;
                    }

                    if (Ankle != null)
                    {
                        yield return Ankle;
                    }

                    yield return Foot;
                }
            }
        }

        private readonly struct PoseKey
        {
            public readonly float Time;
            public readonly float Rise;
            public readonly float LeftClaw;
            public readonly float RightClaw;

            public PoseKey(
                float time,
                float rise,
                float leftClaw,
                float rightClaw)
            {
                Time = time;
                Rise = rise;
                LeftClaw = leftClaw;
                RightClaw = rightClaw;
            }
        }

        private readonly struct VectorKey
        {
            public readonly float Time;
            public readonly Vector3 Value;

            public VectorKey(float time, Vector3 value)
            {
                Time = time;
                Value = value;
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
