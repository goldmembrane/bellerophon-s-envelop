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

namespace Bellerophon.Editor.ParvumCargoRunScene
{
    internal static class ParvumIdleBreathingTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string ParvumRootName = "Approved Parvum Enemy Placement";
        private const string IdleSlotName = "Parvum_01_Idle";
        private const string ModelName = "Parvum_Model";
        private const string SourceModelPath = "Assets/_Project/Art/Enemies/Parvum/Models/parvum.glb";
        private const string GeneratedMeshPath =
            "Assets/_Project/Art/Enemies/Parvum/Models/parvum_idle_breathing_mesh.asset";
        private const string ClipPath =
            "Assets/_Project/Art/Enemies/Parvum/Animations/Parvum_Idle_Breathing.anim";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Parvum/Animations/Controllers/Parvum_Idle_Breathing_Controller.controller";
        private const string BlendShapeName = "Idle_Breathe_FullBody_2_5pct";
        private const string OutputFolder = "docs/validation/parvum_idle_breathing_2026-08-14";
        private const string ReportPath = OutputFolder + "/Parvum_Idle_Breathing_Report.txt";
        private const string CapturePath = OutputFolder + "/Parvum_Idle_Breathing_Final_Comparison.png";
        private const string ExpectedSourceSha256 =
            "E27840896F1DFA15BEE6F45F2BA943D28375A485E141907283CF79446B5640AB";
        private const float CycleSeconds = 2f;
        private const float ExpansionRatio = 0.025f;
        private const float GeometryTolerance = 0.0001f;
        private const float GroundTolerance = 0.002f;
        private const int ReviewLayer = 31;
        private const int PanelWidth = 480;
        private const int CaptureHeight = 720;

        private static readonly float[] CaptureTimes = { 0f, 0.5f, 1f, 1.5f, 2f };
        private static readonly float[] CaptureWeights = { 0f, 50f, 100f, 50f, 0f };

        [MenuItem("Bellerophon/Enemies/Parvum/Apply Idle Full-Body Breathing")]
        public static void ApplyParvumIdleBreathing()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes; Parvum idle breathing was not applied.");
            }

            RequireSourceHash();
            var parvumRoot = RequireRoot(ParvumRootName).transform;
            var idleSlot = RequireDirectChild(parvumRoot, IdleSlotName);
            var model = RequireDirectChild(idleSlot, ModelName);
            var renderer = RequireSingleBodyRenderer(model);
            var sourceRenderer = RequireSourceRenderer();
            RequireCompatibleSource(renderer, sourceRenderer);

            var protectedBefore = ProtectedRootSignatures(scene);
            var otherSlotsBefore = OtherParvumSlotSignatures(parvumRoot);
            var idleTransformBefore = TransformSignature(idleSlot);
            var modelTransformBefore = TransformSignature(model);

            var generatedMesh = EnsureGeneratedMesh(sourceRenderer.sharedMesh);
            var clip = EnsureClip(idleSlot, renderer);
            var controller = EnsureController(clip);

            renderer.sharedMesh = generatedMesh;
            renderer.localBounds = generatedMesh.bounds;
            var blendShapeIndex = generatedMesh.GetBlendShapeIndex(BlendShapeName);
            renderer.SetBlendShapeWeight(blendShapeIndex, 0f);

            var animator = idleSlot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = idleSlot.gameObject.AddComponent<Animator>();
            }

            var otherConfiguredAnimators = idleSlot.GetComponentsInChildren<Animator>(true)
                .Where(candidate => candidate != animator && candidate.runtimeAnimatorController != null)
                .ToArray();
            if (otherConfiguredAnimators.Length > 0)
            {
                throw new InvalidOperationException(
                    "Parvum idle contains an unexpected additional configured Animator: " +
                    otherConfiguredAnimators[0].name + ".");
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = null;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;

            var result = InspectState(parvumRoot, idleSlot, model, renderer, animator, clip, controller);
            if (!string.Equals(idleTransformBefore, TransformSignature(idleSlot), StringComparison.Ordinal) ||
                !string.Equals(modelTransformBefore, TransformSignature(model), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Parvum idle root or model Transform changed during breathing setup.");
            }

            if (!otherSlotsBefore.SequenceEqual(OtherParvumSlotSignatures(parvumRoot), StringComparer.Ordinal))
            {
                throw new InvalidOperationException("A non-idle Parvum slot changed during idle breathing setup.");
            }

            if (!protectedBefore.SequenceEqual(ProtectedRootSignatures(scene), StringComparer.Ordinal))
            {
                throw new InvalidOperationException("A scene root outside Parvum changed during idle breathing setup.");
            }

            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(animator);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after applying Parvum idle breathing.");
            }

            AssetDatabase.SaveAssets();
            WriteReport(result, captureCreated: false);
            Debug.Log(
                "ParvumIdleBreathingApplied Result=PASS" +
                ", Target=" + ParvumRootName + "/" + IdleSlotName + "/" + ModelName +
                ", Vertices=" + result.VertexCount.ToString(CultureInfo.InvariantCulture) +
                ", BlendShape=" + BlendShapeName +
                ", CycleSeconds=" + Num(result.CycleSeconds) +
                ", ExpansionPercent=" + Num(result.ExpansionPercent) +
                ", GroundDelta=" + Num(result.WorldGroundDelta) +
                ", RootMotion=False" +
                ", OtherParvumSlotsChanged=False" +
                ", OtherSceneRootsChanged=False" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Parvum/Inspect Idle Full-Body Breathing")]
        public static void InspectParvumIdleBreathing()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be saved before inspecting Parvum idle breathing.");
            }

            RequireSourceHash();
            var parvumRoot = RequireRoot(ParvumRootName).transform;
            var idleSlot = RequireDirectChild(parvumRoot, IdleSlotName);
            var model = RequireDirectChild(idleSlot, ModelName);
            var renderer = RequireSingleBodyRenderer(model);
            var animator = idleSlot.GetComponent<Animator>() ??
                           throw new InvalidOperationException("Parvum idle Animator is missing.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException("Parvum idle breathing clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                             throw new InvalidOperationException("Parvum idle breathing controller is missing.");
            var result = InspectState(parvumRoot, idleSlot, model, renderer, animator, clip, controller);
            WriteReport(result, captureCreated: File.Exists(Absolute(CapturePath)));

            Debug.Log(
                "ParvumIdleBreathingInspected Result=PASS" +
                ", Vertices=" + result.VertexCount.ToString(CultureInfo.InvariantCulture) +
                ", AffectedVertices=" + result.AffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", CycleSeconds=" + Num(result.CycleSeconds) +
                ", ExpansionPercent=" + Num(result.ExpansionPercent) +
                ", GroundDelta=" + Num(result.WorldGroundDelta) +
                ", Loop=True" +
                ", RootMotion=False.");
        }

        [MenuItem("Bellerophon/Enemies/Parvum/Capture Idle Full-Body Breathing Comparison")]
        public static void CaptureParvumIdleBreathingComparison()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be saved before the final Parvum idle breathing capture.");
            }

            var parvumRoot = RequireRoot(ParvumRootName).transform;
            var idleSlot = RequireDirectChild(parvumRoot, IdleSlotName);
            var model = RequireDirectChild(idleSlot, ModelName);
            var renderer = RequireSingleBodyRenderer(model);
            var animator = idleSlot.GetComponent<Animator>() ??
                           throw new InvalidOperationException("Parvum idle Animator is missing.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException("Parvum idle breathing clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                             throw new InvalidOperationException("Parvum idle breathing controller is missing.");
            var result = InspectState(parvumRoot, idleSlot, model, renderer, animator, clip, controller);

            CaptureComparison(idleSlot, renderer, animator, Absolute(CapturePath));
            if (scene.isDirty)
            {
                throw new InvalidOperationException("Final Parvum idle capture unexpectedly dirtied CargoRunMvp.");
            }

            WriteReport(result, captureCreated: true);
            AssetDatabase.Refresh();
            Debug.Log(
                "ParvumIdleBreathingCaptured Result=PASS" +
                ", Image=" + CapturePath +
                ", Times=0,0.5,1,1.5,2" +
                ", Weights=0,50,100,50,0" +
                ", SceneChanged=False.");
        }

        private static Mesh EnsureGeneratedMesh(Mesh sourceMesh)
        {
            if (sourceMesh == null)
            {
                throw new InvalidOperationException("The supplied Parvum GLB has no source mesh.");
            }

            var generated = UnityEngine.Object.Instantiate(sourceMesh);
            generated.name = "Parvum_Idle_Breathing_Mesh";
            generated.ClearBlendShapes();
            AddFullBodyBreathingBlendShape(generated);

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(GeneratedMeshPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, GeneratedMeshPath);
                existing = generated;
            }
            else
            {
                EditorUtility.CopySerialized(generated, existing);
                UnityEngine.Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            return existing;
        }

        private static void AddFullBodyBreathingBlendShape(Mesh mesh)
        {
            var vertices = mesh.vertices;
            if (vertices == null || vertices.Length == 0)
            {
                throw new InvalidOperationException("Parvum source mesh has no readable vertices.");
            }

            var sourceBounds = BoundsFromVertices(vertices);
            var targetVertices = new Vector3[vertices.Length];
            var deltaVertices = new Vector3[vertices.Length];
            for (var index = 0; index < vertices.Length; index++)
            {
                var vertex = vertices[index];
                var target = new Vector3(
                    sourceBounds.center.x + (vertex.x - sourceBounds.center.x) * (1f + ExpansionRatio),
                    sourceBounds.min.y + (vertex.y - sourceBounds.min.y) * (1f + ExpansionRatio),
                    sourceBounds.center.z + (vertex.z - sourceBounds.center.z) * (1f + ExpansionRatio));
                targetVertices[index] = target;
                deltaVertices[index] = target - vertex;
            }

            var sourceNormals = mesh.normals;
            var sourceTangents = mesh.tangents;
            var deltaNormals = new Vector3[vertices.Length];
            var deltaTangents = new Vector3[vertices.Length];
            var targetMesh = UnityEngine.Object.Instantiate(mesh);
            try
            {
                targetMesh.vertices = targetVertices;
                targetMesh.RecalculateNormals();
                targetMesh.RecalculateTangents();
                var targetNormals = targetMesh.normals;
                var targetTangents = targetMesh.tangents;
                for (var index = 0; index < vertices.Length; index++)
                {
                    if (sourceNormals.Length == vertices.Length && targetNormals.Length == vertices.Length)
                    {
                        deltaNormals[index] = targetNormals[index] - sourceNormals[index];
                    }

                    if (sourceTangents.Length == vertices.Length && targetTangents.Length == vertices.Length)
                    {
                        deltaTangents[index] =
                            new Vector3(targetTangents[index].x, targetTangents[index].y, targetTangents[index].z) -
                            new Vector3(sourceTangents[index].x, sourceTangents[index].y, sourceTangents[index].z);
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetMesh);
            }

            mesh.AddBlendShapeFrame(BlendShapeName, 100f, deltaVertices, deltaNormals, deltaTangents);
            var targetBounds = BoundsFromVertices(targetVertices);
            sourceBounds.Encapsulate(targetBounds);
            mesh.bounds = sourceBounds;
        }

        private static AnimationClip EnsureClip(Transform idleSlot, SkinnedMeshRenderer renderer)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, ClipPath);
            }

            clip.ClearCurves();
            clip.name = "Parvum_Idle_Breathing";
            clip.frameRate = 60f;
            clip.wrapMode = WrapMode.Loop;
            var rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, idleSlot);
            var curve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(CycleSeconds * 0.5f, 100f, 0f, 0f),
                new Keyframe(CycleSeconds, 0f, 0f, 0f));
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
            }

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    rendererPath,
                    typeof(SkinnedMeshRenderer),
                    "blendShape." + BlendShapeName),
                curve);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimatorController EnsureController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(candidate => candidate.name == "Parvum_Idle_Breathing") ??
                        stateMachine.AddState("Parvum_Idle_Breathing");
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = false;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static InspectionResult InspectState(
            Transform parvumRoot,
            Transform idleSlot,
            Transform model,
            SkinnedMeshRenderer renderer,
            Animator animator,
            AnimationClip clip,
            AnimatorController controller)
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                throw new InvalidOperationException("Unity reports script compilation errors.");
            }

            if (renderer.sharedMesh == null ||
                !string.Equals(AssetDatabase.GetAssetPath(renderer.sharedMesh), GeneratedMeshPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Parvum idle renderer is not using the generated breathing mesh.");
            }

            var sourceMesh = RequireSourceRenderer().sharedMesh;
            var generatedMesh = renderer.sharedMesh;
            if (sourceMesh.vertexCount != generatedMesh.vertexCount)
            {
                throw new InvalidOperationException("Generated Parvum idle mesh changed the source vertex count.");
            }

            if (generatedMesh.blendShapeCount != 1 || generatedMesh.GetBlendShapeIndex(BlendShapeName) != 0)
            {
                throw new InvalidOperationException("Generated Parvum idle mesh must contain only the approved breathing BlendShape.");
            }

            var vertexCount = sourceMesh.vertexCount;
            var deltaVertices = new Vector3[vertexCount];
            var deltaNormals = new Vector3[vertexCount];
            var deltaTangents = new Vector3[vertexCount];
            generatedMesh.GetBlendShapeFrameVertices(0, 0, deltaVertices, deltaNormals, deltaTangents);
            var sourceVertices = sourceMesh.vertices;
            var sourceBounds = BoundsFromVertices(sourceVertices);
            var targetVertices = new Vector3[vertexCount];
            var affectedVertexCount = 0;
            for (var index = 0; index < vertexCount; index++)
            {
                var expectedTarget = new Vector3(
                    sourceBounds.center.x +
                    (sourceVertices[index].x - sourceBounds.center.x) * (1f + ExpansionRatio),
                    sourceBounds.min.y +
                    (sourceVertices[index].y - sourceBounds.min.y) * (1f + ExpansionRatio),
                    sourceBounds.center.z +
                    (sourceVertices[index].z - sourceBounds.center.z) * (1f + ExpansionRatio));
                var actualTarget = sourceVertices[index] + deltaVertices[index];
                if ((actualTarget - expectedTarget).sqrMagnitude > GeometryTolerance * GeometryTolerance)
                {
                    throw new InvalidOperationException(
                        "Generated Parvum breathing delta does not match the approved 2.5% full-body formula at vertex " +
                        index.ToString(CultureInfo.InvariantCulture) + ".");
                }

                targetVertices[index] = actualTarget;
                if (deltaVertices[index].sqrMagnitude > 0.0000000001f)
                {
                    affectedVertexCount++;
                }
            }

            if (affectedVertexCount < Mathf.FloorToInt(vertexCount * 0.99f))
            {
                throw new InvalidOperationException("Parvum breathing BlendShape does not affect the complete body mesh.");
            }

            var targetBounds = BoundsFromVertices(targetVertices);
            var expansionX = (targetBounds.size.x / sourceBounds.size.x - 1f) * 100f;
            var expansionY = (targetBounds.size.y / sourceBounds.size.y - 1f) * 100f;
            var expansionZ = (targetBounds.size.z / sourceBounds.size.z - 1f) * 100f;
            if (Mathf.Abs(expansionX - ExpansionRatio * 100f) > 0.01f ||
                Mathf.Abs(expansionY - ExpansionRatio * 100f) > 0.01f ||
                Mathf.Abs(expansionZ - ExpansionRatio * 100f) > 0.01f ||
                Mathf.Abs(targetBounds.min.y - sourceBounds.min.y) > GeometryTolerance)
            {
                throw new InvalidOperationException("Parvum breathing bounds do not match the approved expansion and ground anchor.");
            }

            if (animator.runtimeAnimatorController != controller || animator.applyRootMotion)
            {
                throw new InvalidOperationException("Parvum idle Animator configuration is invalid.");
            }

            var bindings = AnimationUtility.GetCurveBindings(clip);
            var rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, idleSlot);
            if (bindings.Length != 1 ||
                !string.Equals(bindings[0].path, rendererPath, StringComparison.Ordinal) ||
                bindings[0].type != typeof(SkinnedMeshRenderer) ||
                !string.Equals(bindings[0].propertyName, "blendShape." + BlendShapeName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Parvum idle clip must contain exactly one BlendShape curve and no Transform curves.");
            }

            var curve = AnimationUtility.GetEditorCurve(clip, bindings[0]);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (curve == null || curve.length != 3 ||
                Mathf.Abs(curve.keys[0].time) > 0.0001f || Mathf.Abs(curve.keys[0].value) > 0.01f ||
                Mathf.Abs(curve.keys[1].time - 1f) > 0.0001f || Mathf.Abs(curve.keys[1].value - 100f) > 0.01f ||
                Mathf.Abs(curve.keys[2].time - CycleSeconds) > 0.0001f ||
                Mathf.Abs(curve.keys[2].value) > 0.01f ||
                Mathf.Abs(clip.length - CycleSeconds) > 0.0001f || !settings.loopTime)
            {
                throw new InvalidOperationException("Parvum idle breathing curve is not the approved two-second loop.");
            }

            var worldGroundDelta = MeasureWorldGroundDelta(renderer, animator);
            if (worldGroundDelta > GroundTolerance)
            {
                throw new InvalidOperationException(
                    "Parvum idle breathing lifts the visible ground contact. Delta=" + Num(worldGroundDelta) + ".");
            }

            RequireOnlyIdleConfigured(parvumRoot, idleSlot, animator);
            return new InspectionResult(
                vertexCount,
                affectedVertexCount,
                CycleSeconds,
                ExpansionRatio * 100f,
                expansionX,
                expansionY,
                expansionZ,
                worldGroundDelta,
                rendererPath,
                Sha256(Absolute(SourceModelPath)));
        }

        private static float MeasureWorldGroundDelta(SkinnedMeshRenderer renderer, Animator animator)
        {
            var blendShapeIndex = renderer.sharedMesh.GetBlendShapeIndex(BlendShapeName);
            var originalWeight = renderer.GetBlendShapeWeight(blendShapeIndex);
            var animatorEnabled = animator.enabled;
            try
            {
                animator.enabled = false;
                renderer.SetBlendShapeWeight(blendShapeIndex, 0f);
                var baseBounds = BakedWorldBounds(renderer);
                renderer.SetBlendShapeWeight(blendShapeIndex, 100f);
                var expandedBounds = BakedWorldBounds(renderer);
                return Mathf.Abs(expandedBounds.min.y - baseBounds.min.y);
            }
            finally
            {
                renderer.SetBlendShapeWeight(blendShapeIndex, originalWeight);
                animator.enabled = animatorEnabled;
            }
        }

        private static Bounds BakedWorldBounds(SkinnedMeshRenderer renderer)
        {
            var baked = new Mesh();
            try
            {
                renderer.BakeMesh(baked, false);
                var vertices = baked.vertices;
                if (vertices == null || vertices.Length == 0)
                {
                    throw new InvalidOperationException("Parvum renderer produced no baked vertices.");
                }

                var matrix = renderer.transform.localToWorldMatrix;
                var bounds = new Bounds(matrix.MultiplyPoint3x4(vertices[0]), Vector3.zero);
                for (var index = 1; index < vertices.Length; index++)
                {
                    bounds.Encapsulate(matrix.MultiplyPoint3x4(vertices[index]));
                }

                return bounds;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static void CaptureComparison(
            Transform idleSlot,
            SkinnedMeshRenderer renderer,
            Animator animator,
            string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid Parvum idle capture path."));
            var sourceCamera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>() ??
                               throw new InvalidOperationException("The scene has no camera for Parvum idle review framing.");
            var transforms = idleSlot.GetComponentsInChildren<Transform>(true);
            var originalLayers = transforms.Select(item => item.gameObject.layer).ToArray();
            var blendShapeIndex = renderer.sharedMesh.GetBlendShapeIndex(BlendShapeName);
            var originalWeight = renderer.GetBlendShapeWeight(blendShapeIndex);
            var animatorEnabled = animator.enabled;
            var previousActive = RenderTexture.active;
            var panelTarget = new RenderTexture(PanelWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
            var panelImage = new Texture2D(PanelWidth, CaptureHeight, TextureFormat.RGB24, false);
            var composite = new Texture2D(
                PanelWidth * CaptureWeights.Length,
                CaptureHeight,
                TextureFormat.RGB24,
                false);
            var cameraObject = new GameObject("ParvumIdleBreathingReviewCamera", typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var lightObject = new GameObject("ParvumIdleBreathingReviewLight", typeof(Light))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            try
            {
                animator.enabled = false;
                for (var index = 0; index < transforms.Length; index++)
                {
                    transforms[index].gameObject.layer = ReviewLayer;
                }

                renderer.SetBlendShapeWeight(blendShapeIndex, 100f);
                var reviewBounds = BakedWorldBounds(renderer);
                renderer.SetBlendShapeWeight(blendShapeIndex, 0f);

                var reviewCamera = cameraObject.GetComponent<Camera>();
                reviewCamera.CopyFrom(sourceCamera);
                reviewCamera.clearFlags = CameraClearFlags.SolidColor;
                reviewCamera.backgroundColor = new Color(0.012f, 0.016f, 0.02f, 1f);
                reviewCamera.cullingMask = 1 << ReviewLayer;
                reviewCamera.allowHDR = false;
                reviewCamera.targetTexture = panelTarget;
                reviewCamera.aspect = PanelWidth / (float)CaptureHeight;
                var viewDirection = reviewBounds.center - sourceCamera.transform.position;
                if (viewDirection.sqrMagnitude < 0.001f)
                {
                    viewDirection = idleSlot.forward;
                }

                viewDirection.Normalize();
                var verticalRadians = Mathf.Max(1f, reviewCamera.fieldOfView * 0.5f) * Mathf.Deg2Rad;
                var horizontalRadians = Mathf.Atan(Mathf.Tan(verticalRadians) * reviewCamera.aspect);
                var distance = Mathf.Max(
                    reviewBounds.extents.y / Mathf.Max(0.01f, Mathf.Tan(verticalRadians)),
                    reviewBounds.extents.x / Mathf.Max(0.01f, Mathf.Tan(horizontalRadians))) * 1.3f;
                reviewCamera.transform.SetPositionAndRotation(
                    reviewBounds.center - viewDirection * distance,
                    Quaternion.LookRotation(viewDirection, Vector3.up));

                var reviewLight = lightObject.GetComponent<Light>();
                reviewLight.type = LightType.Directional;
                reviewLight.intensity = 1.35f;
                reviewLight.color = new Color(0.88f, 0.94f, 1f);
                reviewLight.cullingMask = 1 << ReviewLayer;
                reviewLight.shadows = LightShadows.None;
                reviewLight.transform.rotation = Quaternion.LookRotation(
                    viewDirection + new Vector3(-0.45f, -0.55f, 0.2f),
                    Vector3.up);

                for (var panel = 0; panel < CaptureWeights.Length; panel++)
                {
                    renderer.SetBlendShapeWeight(blendShapeIndex, CaptureWeights[panel]);
                    RenderTexture.active = panelTarget;
                    reviewCamera.Render();
                    panelImage.ReadPixels(new Rect(0, 0, PanelWidth, CaptureHeight), 0, 0);
                    panelImage.Apply();
                    composite.SetPixels32(
                        panel * PanelWidth,
                        0,
                        PanelWidth,
                        CaptureHeight,
                        panelImage.GetPixels32());
                }

                composite.Apply();
                File.WriteAllBytes(destination, composite.EncodeToPNG());
            }
            finally
            {
                for (var index = 0; index < transforms.Length; index++)
                {
                    transforms[index].gameObject.layer = originalLayers[index];
                }

                renderer.SetBlendShapeWeight(blendShapeIndex, originalWeight);
                animator.enabled = animatorEnabled;
                RenderTexture.active = previousActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                UnityEngine.Object.DestroyImmediate(panelImage);
                UnityEngine.Object.DestroyImmediate(composite);
                panelTarget.Release();
                UnityEngine.Object.DestroyImmediate(panelTarget);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private static void RequireOnlyIdleConfigured(
            Transform parvumRoot,
            Transform idleSlot,
            Animator idleAnimator)
        {
            for (var index = 0; index < parvumRoot.childCount; index++)
            {
                var slot = parvumRoot.GetChild(index);
                if (slot == idleSlot)
                {
                    if (slot.GetComponentsInChildren<Animator>(true)
                        .Count(candidate => candidate.runtimeAnimatorController != null) != 1)
                    {
                        throw new InvalidOperationException("Parvum idle must have exactly one configured Animator.");
                    }

                    continue;
                }

                var usesIdleController = slot.GetComponentsInChildren<Animator>(true)
                    .Any(candidate => candidate.runtimeAnimatorController == idleAnimator.runtimeAnimatorController);
                if (usesIdleController)
                {
                    throw new InvalidOperationException(slot.name + " unexpectedly uses the Parvum idle breathing controller.");
                }
            }
        }

        private static SkinnedMeshRenderer RequireSourceRenderer()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourceModelPath) ??
                         throw new InvalidOperationException("The supplied Parvum GLB asset is missing.");
            var renderers = source.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(candidate => candidate.sharedMesh != null)
                .ToArray();
            if (renderers.Length != 1)
            {
                throw new InvalidOperationException(
                    "The supplied Parvum GLB must contain exactly one SkinnedMeshRenderer. Count=" +
                    renderers.Length.ToString(CultureInfo.InvariantCulture) + ".");
            }

            return renderers[0];
        }

        private static SkinnedMeshRenderer RequireSingleBodyRenderer(Transform model)
        {
            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(candidate => candidate.sharedMesh != null && candidate.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length != 1)
            {
                throw new InvalidOperationException(
                    "Current Parvum model must contain exactly one active SkinnedMeshRenderer. Count=" +
                    renderers.Length.ToString(CultureInfo.InvariantCulture) + ".");
            }

            return renderers[0];
        }

        private static void RequireCompatibleSource(
            SkinnedMeshRenderer current,
            SkinnedMeshRenderer source)
        {
            if (current.sharedMesh == null || source.sharedMesh == null ||
                current.sharedMesh.vertexCount != source.sharedMesh.vertexCount ||
                current.sharedMesh.subMeshCount != source.sharedMesh.subMeshCount)
            {
                throw new InvalidOperationException("Current Parvum idle renderer does not match the supplied GLB mesh.");
            }
        }

        private static Bounds BoundsFromVertices(IReadOnlyList<Vector3> vertices)
        {
            if (vertices.Count == 0)
            {
                throw new InvalidOperationException("Cannot calculate bounds from an empty vertex collection.");
            }

            var bounds = new Bounds(vertices[0], Vector3.zero);
            for (var index = 1; index < vertices.Count; index++)
            {
                bounds.Encapsulate(vertices[index]);
            }

            return bounds;
        }

        private static string[] OtherParvumSlotSignatures(Transform root)
        {
            return root.Cast<Transform>()
                .Where(slot => !string.Equals(slot.name, IdleSlotName, StringComparison.Ordinal))
                .Select(SlotSignature)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static string[] ProtectedRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(root => !string.Equals(root.name, ParvumRootName, StringComparison.Ordinal))
                .Select(root => SlotSignature(root.transform))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static string SlotSignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(AnimationUtility.CalculateTransformPath(item, root)).Append('|')
                    .Append(TransformSignature(item)).Append('|')
                    .Append(item.gameObject.activeSelf ? '1' : '0').AppendLine();
                foreach (var renderer in item.GetComponents<SkinnedMeshRenderer>())
                {
                    builder.Append("Mesh=").Append(AssetDatabase.GetAssetPath(renderer.sharedMesh)).AppendLine();
                }

                foreach (var animator in item.GetComponents<Animator>())
                {
                    builder.Append("Controller=")
                        .Append(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)).AppendLine();
                }
            }

            return builder.ToString();
        }

        private static string TransformSignature(Transform item)
        {
            return Vec(item.localPosition) + "|" + Vec(item.localEulerAngles) + "|" + Vec(item.localScale);
        }

        private static Transform RequireDirectChild(Transform parent, string childName)
        {
            var child = parent.Find(childName) ??
                        throw new InvalidOperationException("Missing direct child " + childName + " under " + parent.name + ".");
            if (child.parent != parent)
            {
                throw new InvalidOperationException(childName + " is not a direct child of " + parent.name + ".");
            }

            return child;
        }

        private static GameObject RequireRoot(string rootName)
        {
            var root = GameObject.Find(rootName) ??
                       throw new InvalidOperationException("Missing scene root: " + rootName + ".");
            if (root.transform.parent != null)
            {
                throw new InvalidOperationException(rootName + " is not a scene root.");
            }

            return root;
        }

        private static Scene RequireCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("CargoRunMvp must already be the active scene. Active=" + scene.path + ".");
            }

            return scene;
        }

        private static void RequireSourceHash()
        {
            var actual = Sha256(Absolute(SourceModelPath));
            if (!string.Equals(actual, ExpectedSourceSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Supplied Parvum GLB hash changed. Expected=" + ExpectedSourceSha256 + ", Actual=" + actual + ".");
            }
        }

        private static void WriteReport(InspectionResult result, bool captureCreated)
        {
            var report = new StringBuilder()
                .AppendLine("Parvum Idle Full-Body Breathing Report")
                .AppendLine("Result=PASS")
                .AppendLine("Target=" + ParvumRootName + "/" + IdleSlotName + "/" + ModelName)
                .AppendLine("SourceModel=" + SourceModelPath)
                .AppendLine("SourceSha256=" + result.SourceSha256)
                .AppendLine("GeneratedMesh=" + GeneratedMeshPath)
                .AppendLine("AnimationClip=" + ClipPath)
                .AppendLine("AnimatorController=" + ControllerPath)
                .AppendLine("RendererPath=" + result.RendererPath)
                .AppendLine("BlendShape=" + BlendShapeName)
                .AppendLine("VertexCount=" + result.VertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("AffectedVertexCount=" + result.AffectedVertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("CycleSeconds=" + Num(result.CycleSeconds))
                .AppendLine("Loop=True")
                .AppendLine("CurveTimes=0,1,2")
                .AppendLine("CurveWeights=0,100,0")
                .AppendLine("MaximumExpansionPercent=" + Num(result.ExpansionPercent))
                .AppendLine("ExpansionXPercent=" + Num(result.ExpansionXPercent))
                .AppendLine("ExpansionYPercent=" + Num(result.ExpansionYPercent))
                .AppendLine("ExpansionZPercent=" + Num(result.ExpansionZPercent))
                .AppendLine("WorldGroundDelta=" + Num(result.WorldGroundDelta))
                .AppendLine("RootMotion=False")
                .AppendLine("TransformScaleAnimation=False")
                .AppendLine("OriginalGlbModified=False")
                .AppendLine("OtherParvumSlotsChanged=False")
                .AppendLine("OtherSceneRootsChanged=False")
                .AppendLine("CapturePanelsLeftToRight=0s@0,0.5s@50,1s@100,1.5s@50,2s@0")
                .AppendLine("CaptureCreated=" + (captureCreated ? "True" : "False"))
                .AppendLine("HarnessValidationRun=False")
                .ToString();
            var destination = Absolute(ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid Parvum idle report path."));
            File.WriteAllText(destination, report, new UTF8Encoding(false));
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Absolute(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relativePath));
        }

        private static string Num(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + ")";
        }

        private readonly struct InspectionResult
        {
            public InspectionResult(
                int vertexCount,
                int affectedVertexCount,
                float cycleSeconds,
                float expansionPercent,
                float expansionXPercent,
                float expansionYPercent,
                float expansionZPercent,
                float worldGroundDelta,
                string rendererPath,
                string sourceSha256)
            {
                VertexCount = vertexCount;
                AffectedVertexCount = affectedVertexCount;
                CycleSeconds = cycleSeconds;
                ExpansionPercent = expansionPercent;
                ExpansionXPercent = expansionXPercent;
                ExpansionYPercent = expansionYPercent;
                ExpansionZPercent = expansionZPercent;
                WorldGroundDelta = worldGroundDelta;
                RendererPath = rendererPath;
                SourceSha256 = sourceSha256;
            }

            public int VertexCount { get; }
            public int AffectedVertexCount { get; }
            public float CycleSeconds { get; }
            public float ExpansionPercent { get; }
            public float ExpansionXPercent { get; }
            public float ExpansionYPercent { get; }
            public float ExpansionZPercent { get; }
            public float WorldGroundDelta { get; }
            public string RendererPath { get; }
            public string SourceSha256 { get; }
        }
    }
}
