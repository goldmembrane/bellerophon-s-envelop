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

namespace Bellerophon.Editor
{
    internal static class OstinatoAttackDownstrikeBladeRotation
    {
        private const string CorrectedClipPath = "Assets/_Project/Art/Enemies/Ostinato/Animations/Ostinato_04_Scissor_Attack_DownstrikeBladeRotation.anim";
        private const string DerivedMeshPath = "Assets/_Project/Art/Enemies/Ostinato/Animations/Ostinato_04_Scissor_Attack_RigidBladeRig.asset";
        private const string LeftRigidBladeMeshPath = "Assets/_Project/Art/Enemies/Ostinato/Animations/Ostinato_04_Scissor_Attack_LeftRigidBlade.asset";
        private const string RightRigidBladeMeshPath = "Assets/_Project/Art/Enemies/Ostinato/Animations/Ostinato_04_Scissor_Attack_RightRigidBlade.asset";
        private const string ValidationFolder = "docs/validation/ostinato_attack_downstrike_blade_rotation_2026-07-20";
        private const string ApplyReportPath = ValidationFolder + "/Ostinato_AttackDownstrikeBladeRotationApply.txt";
        private const string InspectionReportPath = ValidationFolder + "/Ostinato_AttackDownstrikeBladeRotationInspection.txt";
        private const string CaptureFolderPath = ValidationFolder + "/exact_frames";
        private const string CaptureSheetPath = ValidationFolder + "/Ostinato_AttackDownstrikeBladeRotationComparison.png";
        private const string CaptureReportPath = ValidationFolder + "/Ostinato_AttackDownstrikeBladeRotationCapture.txt";
        private const string AttackTakeNameFragment = "mixamo.com";
        private const string BindingRootName = "Armature";
        private const string LeftBladeRootName = "LeftBladeRigidRoot";
        private const string RightBladeRootName = "RightBladeRigidRoot";
        private const int RotationFirstFrame = 62;
        private const int RotationFullFrame = 77;
        private const int HoldLastFrame = 93;
        private const int ReturnLastFrame = 99;
        private const int LastShapeFrame = 98;
        private const int LastFrame = 159;
        private const float FrameRate = 60f;
        // Keeps both opposed tips predominantly lateral while moving each rigid blade chord in front of the torso.
        private const float ForwardTipBias = 1.1f;
        private const int ReviewLayer = 30;
        private const int PanelSize = 320;

        private static readonly int[] CaptureFrames = { 50, 61, 62, 67, 72, 77, 78, 83, 93, 94, 99 };
        private static readonly string[] ApprovedMaterialPaths =
        {
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_Chitin.mat",
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_SoftTissue.mat",
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_HookBlade.mat",
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_CompoundEye.mat",
        };

        public static void ApplyOstinatoAttackDownstrikeBladeRotation()
        {
            var scene = RequireOpenScene();
            var sourceClip = RequireSourceClip();
            var sourceHash = ComputeSha256(OstinatoScissorAttackAnimation.ProjectAbsolutePath(OstinatoScissorAttackAnimation.SourceAttackRelativePath));
            var importedHash = ComputeSha256(OstinatoScissorAttackAnimation.ProjectAbsolutePath(OstinatoScissorAttackAnimation.AttackModelPath));
            if (sourceHash != importedHash) throw new InvalidOperationException("Ostinato source and imported attack FBX hashes differ.");

            var sourceRenderer = RequireRenderer(RequireAsset<GameObject>(OstinatoScissorAttackAnimation.ApprovedModelPath));
            var topology = AnalyzeTopology(sourceRenderer);
            var derivedMesh = CreateDerivedMesh(sourceRenderer, topology, sourceClip);
            CreateRigidBladeMeshes(sourceRenderer.sharedMesh, topology);
            var correctedClip = CreateCorrectedClip(sourceClip, topology);
            var sourceNonCorrection = BuildCurveFingerprint(sourceClip, binding => !IsCorrectionBinding(binding));
            var correctedNonCorrection = BuildCurveFingerprint(correctedClip, binding => !IsCorrectionBinding(binding));
            if (sourceNonCorrection != correctedNonCorrection)
                throw new InvalidOperationException("A source animation curve changed while adding blade-only correction curves.");

            var controller = ConfigureController(correctedClip);
            var model = RequireSceneModel(scene);
            var renderer = RequireRenderer(model);
            RestoreApprovedRendererContract(model, renderer, sourceRenderer);
            renderer.sharedMesh = derivedMesh;
            renderer.sharedMaterials = ApprovedMaterialPaths.Select(RequireAsset<Material>).ToArray();
            renderer.updateWhenOffscreen = true;
            ConfigureRigidBladeObjects(model, renderer, topology);
            var animator = model.GetComponent<Animator>() ?? throw new InvalidOperationException("Ostinato attack scene Animator is missing.");
            animator.runtimeAnimatorController = controller;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(animator);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Failed to save the Ostinato downstrike blade rotation scene state.");
            AssetDatabase.SaveAssets();

            var metrics = InspectRigidObjectContract(sourceClip, correctedClip, derivedMesh, topology);
            var report = new StringBuilder();
            report.AppendLine("Scene=" + scene.path);
            report.AppendLine("PlaybackPrefab=" + OstinatoScissorAttackAnimation.AttackModelPath);
            report.AppendLine("SourceClip=" + AssetDatabase.GetAssetPath(sourceClip));
            report.AppendLine("CorrectedClip=" + CorrectedClipPath);
            report.AppendLine("BodyMesh=" + DerivedMeshPath);
            report.AppendLine("LeftRigidBladeMesh=" + LeftRigidBladeMeshPath);
            report.AppendLine("RightRigidBladeMesh=" + RightRigidBladeMeshPath);
            report.AppendLine("RotationFrames=62-77");
            report.AppendLine("HorizontalFrontHoldFrames=78-93");
            report.AppendLine("LeftBladeTipDirection=AnatomicalRight");
            report.AppendLine("RightBladeTipDirection=AnatomicalLeft");
            report.AppendLine("ReturnFrames=94-99");
            report.AppendLine("ChangedBindings=Transform.m_LocalRotation:" + LeftBladeRootName + "|" + RightBladeRootName);
            report.AppendLine("SourceNonCorrectionFingerprint=" + sourceNonCorrection);
            report.AppendLine("CorrectedNonCorrectionFingerprint=" + correctedNonCorrection);
            AppendTopology(report, topology);
            AppendRigidObjectMetrics(report, metrics);
            report.AppendLine("SourceSha256=" + sourceHash);
            report.AppendLine("ImportedSha256=" + importedHash);
            report.AppendLine("SourceFbxModified=False");
            report.AppendLine("OtherSlotsTargeted=False");
            OstinatoScissorAttackAnimation.WriteText(ApplyReportPath, report.ToString());
            Debug.Log("OstinatoAttackDownstrikeBladeRotationApplied, RotationFrames=62-77, HoldFrames=78-93, ReturnFrames=94-99, " +
                      "MaxShapeError=" + Format(metrics.MaxRigidShapeError) + ", MinFrontClearance=" + Format(metrics.MinFrontClearance));
        }

        public static void InspectOstinatoAttackDownstrikeBladeRotation()
        {
            var scene = RequireOpenScene();
            var sourceClip = RequireSourceClip();
            var correctedClip = RequireAsset<AnimationClip>(CorrectedClipPath);
            var derivedMesh = RequireAsset<Mesh>(DerivedMeshPath);
            var sourceRenderer = RequireRenderer(RequireAsset<GameObject>(OstinatoScissorAttackAnimation.ApprovedModelPath));
            var topology = AnalyzeTopology(sourceRenderer);
            InspectDerivedMesh(sourceRenderer.sharedMesh, derivedMesh, topology);
            InspectRigidBladeMeshes(topology);
            var sourceNonCorrection = BuildCurveFingerprint(sourceClip, binding => !IsCorrectionBinding(binding));
            var correctedNonCorrection = BuildCurveFingerprint(correctedClip, binding => !IsCorrectionBinding(binding));
            if (sourceNonCorrection != correctedNonCorrection)
                throw new InvalidOperationException("The corrected clip changed a non-blade animation curve.");
            var metrics = InspectRigidObjectContract(sourceClip, correctedClip, derivedMesh, topology);
            var controller = RequireAsset<AnimatorController>(OstinatoScissorAttackAnimation.ControllerPath);
            var states = controller.layers.SelectMany(layer => layer.stateMachine.states).Select(entry => entry.state)
                .Where(state => state.name == OstinatoScissorAttackAnimation.StateName).ToArray();
            if (states.Length != 1 || states[0].motion != correctedClip || !Mathf.Approximately(states[0].speed, 1f))
                throw new InvalidOperationException("The Ostinato controller does not use the downstrike blade correction clip at speed 1.");
            var model = RequireSceneModel(scene);
            var animator = model.GetComponent<Animator>() ?? throw new InvalidOperationException("Ostinato attack scene Animator is missing.");
            var sceneRenderer = RequireRenderer(model);
            if (sceneRenderer.sharedMesh != derivedMesh || animator.runtimeAnimatorController != controller || animator.applyRootMotion)
                throw new InvalidOperationException("The Ostinato scene downstrike blade contract changed.");
            InspectSceneRigidBladeObjects(model);
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(model) != OstinatoScissorAttackAnimation.AttackModelPath)
                throw new InvalidOperationException("The scene attack object is no longer sourced from the supplied attack FBX.");

            var report = new StringBuilder();
            report.AppendLine("Scene=" + scene.path);
            report.AppendLine("PlaybackPrefab=" + OstinatoScissorAttackAnimation.AttackModelPath);
            report.AppendLine("CorrectedClip=" + CorrectedClipPath);
            report.AppendLine("BodyMesh=" + DerivedMeshPath);
            report.AppendLine("LeftRigidBladeMesh=" + LeftRigidBladeMeshPath);
            report.AppendLine("RightRigidBladeMesh=" + RightRigidBladeMeshPath);
            report.AppendLine("ClipLength=" + Format(correctedClip.length));
            report.AppendLine("ClipFrameRate=" + Format(correctedClip.frameRate));
            report.AppendLine("RotationFrames=62-77");
            report.AppendLine("HorizontalFrontHoldFrames=78-93");
            report.AppendLine("LeftBladeTipDirection=AnatomicalRight");
            report.AppendLine("RightBladeTipDirection=AnatomicalLeft");
            report.AppendLine("ReturnFrames=94-99");
            report.AppendLine("ExistingHandForeArmAndBodyCurvesChanged=False");
            report.AppendLine("SourceAndCorrectedNonCorrectionCurvesMatch=" + (sourceNonCorrection == correctedNonCorrection));
            AppendTopology(report, topology);
            AppendRigidObjectMetrics(report, metrics);
            report.AppendLine("AnimatorStateSpeed=" + Format(states[0].speed));
            report.AppendLine("AnimatorApplyRootMotion=" + animator.applyRootMotion);
            report.AppendLine("SourceFbxModified=False");
            report.AppendLine("OtherSlotsTargeted=False");
            OstinatoScissorAttackAnimation.WriteText(InspectionReportPath, report.ToString());
            Debug.Log("OstinatoAttackDownstrikeBladeRotationInspected, BodyCurvesUnchanged=True, BladeBlendShapes=0, " +
                      "RigidTransformOnly=True, TorsoFrontClearance=True");
        }

        public static void CaptureOstinatoAttackDownstrikeBladeRotation()
        {
            RequireOpenScene();
            var sourceClip = RequireSourceClip();
            var correctedClip = RequireAsset<AnimationClip>(CorrectedClipPath);
            var sourceModel = CreatePlaybackModel(false);
            var correctedModel = CreatePlaybackModel(true);
            var cameraObject = new GameObject("Ostinato_DownstrikeBladeReviewCamera", typeof(Camera));
            var keyObject = new GameObject("Ostinato_DownstrikeBladeKeyLight", typeof(Light));
            var fillObject = new GameObject("Ostinato_DownstrikeBladeFillLight", typeof(Light));
            try
            {
                SetLayerRecursively(sourceModel, ReviewLayer);
                SetLayerRecursively(correctedModel, ReviewLayer);
                var camera = cameraObject.GetComponent<Camera>();
                ConfigureCameraAndLights(camera, keyObject.GetComponent<Light>(), fillObject.GetComponent<Light>(), keyObject.transform, fillObject.transform);
                var framesPath = OstinatoScissorAttackAnimation.ProjectAbsolutePath(CaptureFolderPath);
                Directory.CreateDirectory(framesPath);
                var captured = new List<byte[]>();
                foreach (var frame in CaptureFrames)
                {
                    var bounds = GetSampleBounds(sourceModel, sourceClip, frame);
                    bounds.Encapsulate(GetSampleBounds(correctedModel, correctedClip, frame));
                    var panels = new[]
                    {
                        RenderSample(sourceModel, sourceClip, frame, camera, Vector3.forward, bounds),
                        RenderSample(correctedModel, correctedClip, frame, camera, Vector3.forward, bounds),
                        RenderSample(sourceModel, sourceClip, frame, camera, Vector3.left, bounds),
                        RenderSample(correctedModel, correctedClip, frame, camera, Vector3.left, bounds),
                        RenderSample(sourceModel, sourceClip, frame, camera, new Vector3(0.7f, 0f, 1f).normalized, bounds),
                        RenderSample(correctedModel, correctedClip, frame, camera, new Vector3(0.7f, 0f, 1f).normalized, bounds),
                    };
                    var combined = CombineHorizontal(panels);
                    foreach (var panel in panels) UnityEngine.Object.DestroyImmediate(panel);
                    var bytes = combined.EncodeToPNG();
                    UnityEngine.Object.DestroyImmediate(combined);
                    File.WriteAllBytes(Path.Combine(framesPath, "frame_" + frame.ToString("D3", CultureInfo.InvariantCulture) + ".png"), bytes);
                    captured.Add(bytes);
                }
                WriteSheet(captured);
                var report = new StringBuilder();
                report.AppendLine("Panels=SourceFront|CorrectedFront|SourceSide|CorrectedSide|SourceThreeQuarter|CorrectedThreeQuarter");
                report.AppendLine("ExactUnityFrames=" + string.Join("|", CaptureFrames));
                report.AppendLine("RotationFrames=62-77");
                report.AppendLine("HorizontalFrontHoldFrames=78-93");
                report.AppendLine("LeftBladeTipDirection=AnatomicalRight");
                report.AppendLine("RightBladeTipDirection=AnatomicalLeft");
                report.AppendLine("ReturnFrames=94-99");
                report.AppendLine("RenderPath=BakedMeshPerSample");
                report.AppendLine("FrameDirectory=" + CaptureFolderPath);
                report.AppendLine("ComparisonSheet=" + CaptureSheetPath);
                report.AppendLine("SourceFbxModified=False");
                OstinatoScissorAttackAnimation.WriteText(CaptureReportPath, report.ToString());
                Debug.Log("OstinatoAttackDownstrikeBladeRotationCaptured, ExactFrames=" + string.Join("|", CaptureFrames));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceModel);
                UnityEngine.Object.DestroyImmediate(correctedModel);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyObject);
                UnityEngine.Object.DestroyImmediate(fillObject);
            }
        }

        private static Mesh CreateDerivedMesh(SkinnedMeshRenderer sourceRenderer, TopologyContract topology, AnimationClip sourceClip)
        {
            var source = sourceRenderer.sharedMesh;
            var destination = AssetDatabase.LoadAssetAtPath<Mesh>(DerivedMeshPath);
            if (destination == null)
            {
                destination = new Mesh();
                AssetDatabase.CreateAsset(destination, DerivedMeshPath);
            }
            WriteBodyWithoutMainBlades(source, destination, topology);
            destination.name = "Ostinato_04_Scissor_Attack_RigidBladeRig";
            EditorUtility.SetDirty(destination);
            AssetDatabase.SaveAssets();
            InspectDerivedMesh(source, destination, topology);
            return destination;
        }

        private static AnimationClip CreateCorrectedClip(AnimationClip sourceClip, TopologyContract topology)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CorrectedClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, CorrectedClipPath);
            }
            EditorUtility.CopySerialized(sourceClip, clip);
            clip.name = "Ostinato_04_Scissor_Attack_DownstrikeBladeRotation";
            var model = CreatePlaybackModel(true);
            try
            {
                SetRigidBladeRotationCurves(clip, model, sourceClip);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(model);
            }
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static void WriteBodyWithoutMainBlades(Mesh source, Mesh destination, TopologyContract topology)
        {
            if (source.blendShapeCount != 0) throw new InvalidOperationException("The approved Ostinato source mesh unexpectedly contains BlendShapes.");
            destination.Clear();
            destination.indexFormat = source.indexFormat;
            destination.vertices = source.vertices;
            if (source.normals.Length > 0) destination.normals = source.normals;
            if (source.tangents.Length > 0) destination.tangents = source.tangents;
            if (source.colors32.Length > 0) destination.colors32 = source.colors32;
            for (var channel = 0; channel < 8; channel++)
            {
                var uv = new List<Vector4>();
                source.GetUVs(channel, uv);
                if (uv.Count > 0) destination.SetUVs(channel, uv);
            }
            destination.boneWeights = source.boneWeights;
            destination.bindposes = source.bindposes;
            destination.subMeshCount = source.subMeshCount;
            var removed = topology.LeftSources.Concat(topology.RightSources).ToHashSet();
            for (var subMesh = 0; subMesh < source.subMeshCount; subMesh++)
            {
                var sourceIndices = source.GetIndices(subMesh);
                var indices = new List<int>(sourceIndices.Length);
                for (var i = 0; i < sourceIndices.Length; i += 3)
                {
                    if (subMesh == 2 && removed.Contains(sourceIndices[i])) continue;
                    indices.Add(sourceIndices[i]); indices.Add(sourceIndices[i + 1]); indices.Add(sourceIndices[i + 2]);
                }
                destination.SetIndices(indices, source.GetTopology(subMesh), subMesh, false);
            }
            destination.RecalculateBounds();
        }

        private static void CreateRigidBladeMeshes(Mesh source, TopologyContract topology)
        {
            WriteRigidBladeMesh(source, topology.LeftSources, topology.LeftBoundarySources,
                LeftRigidBladeMeshPath, "Ostinato_04_Scissor_Attack_LeftRigidBlade");
            WriteRigidBladeMesh(source, topology.RightSources, topology.RightBoundarySources,
                RightRigidBladeMeshPath, "Ostinato_04_Scissor_Attack_RightRigidBlade");
            AssetDatabase.SaveAssets();
        }

        private static void WriteRigidBladeMesh(Mesh source, IReadOnlyList<int> component,
            IReadOnlyList<int> boundary, string path, string name)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, path);
            }
            var pivot = Average(source.vertices, boundary);
            var componentSet = component.ToHashSet();
            var remap = component.Select((sourceIndex, localIndex) => (sourceIndex, localIndex))
                .ToDictionary(value => value.sourceIndex, value => value.localIndex);
            var sourceTriangles = source.GetIndices(2);
            var triangles = new List<int>();
            for (var i = 0; i < sourceTriangles.Length; i += 3)
            {
                if (!componentSet.Contains(sourceTriangles[i])) continue;
                triangles.Add(remap[sourceTriangles[i]]);
                triangles.Add(remap[sourceTriangles[i + 1]]);
                triangles.Add(remap[sourceTriangles[i + 2]]);
            }
            mesh.Clear();
            mesh.name = name;
            mesh.indexFormat = component.Count > ushort.MaxValue
                ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = component.Select(index => source.vertices[index] - pivot).ToArray();
            if (source.normals.Length == source.vertexCount) mesh.normals = component.Select(index => source.normals[index]).ToArray();
            if (source.tangents.Length == source.vertexCount) mesh.tangents = component.Select(index => source.tangents[index]).ToArray();
            if (source.colors32.Length == source.vertexCount) mesh.colors32 = component.Select(index => source.colors32[index]).ToArray();
            for (var channel = 0; channel < 8; channel++)
            {
                var sourceUv = new List<Vector4>(); source.GetUVs(channel, sourceUv);
                if (sourceUv.Count == source.vertexCount) mesh.SetUVs(channel, component.Select(index => sourceUv[index]).ToArray());
            }
            mesh.subMeshCount = 1;
            mesh.SetTriangles(triangles, 0, false);
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
        }

        private static void SetRigidBladeRotationCurves(AnimationClip clip, GameObject model, AnimationClip sourceClip)
        {
            var leftRoot = FindDescendant(model.transform, LeftBladeRootName);
            var rightRoot = FindDescendant(model.transform, RightBladeRootName);
            var leftBase = leftRoot.localRotation;
            var rightBase = rightRoot.localRotation;
            var leftRotations = new List<Quaternion>(LastFrame + 1);
            var rightRotations = new List<Quaternion>(LastFrame + 1);
            for (var frame = 0; frame <= LastFrame; frame++)
            {
                leftRoot.localRotation = leftBase; rightRoot.localRotation = rightBase;
                Sample(model, sourceClip, frame);
                var bodyRight = Vector3.ProjectOnPlane(model.transform.right, Vector3.up).normalized;
                if (bodyRight.sqrMagnitude < 0.9f) throw new InvalidOperationException("Ostinato lateral body axis could not be measured.");
                var bodyFront = Vector3.ProjectOnPlane(model.transform.forward, Vector3.up).normalized;
                if (bodyFront.sqrMagnitude < 0.5f) throw new InvalidOperationException("Ostinato forward body axis could not be measured.");
                bodyFront = Vector3.ProjectOnPlane(bodyFront, bodyRight).normalized;
                var factor = CorrectionFactor(frame);
                leftRotations.Add(TargetLocalRotation(leftRoot, (bodyRight + bodyFront * ForwardTipBias).normalized, bodyFront, factor));
                rightRotations.Add(TargetLocalRotation(rightRoot, (-bodyRight + bodyFront * ForwardTipBias).normalized, bodyFront, factor));
            }
            SetQuaternionCurves(clip, AnimationUtility.CalculateTransformPath(leftRoot, model.transform), leftRotations);
            SetQuaternionCurves(clip, AnimationUtility.CalculateTransformPath(rightRoot, model.transform), rightRotations);
            clip.EnsureQuaternionContinuity();
        }

        private static Quaternion TargetLocalRotation(Transform root, Vector3 targetLong, Vector3 targetCurve, float factor)
        {
            var mesh = root.GetComponent<MeshFilter>().sharedMesh;
            var points = mesh.vertices.Select(root.TransformPoint).ToArray();
            var fullCorrection = ComputeRigidTargetCorrection(points, targetLong, targetCurve, root.position);
            var correctedWorld = Quaternion.Slerp(Quaternion.identity, fullCorrection, factor) * root.rotation;
            return Quaternion.Inverse(root.parent.rotation) * correctedWorld;
        }

        private static void SetQuaternionCurves(AnimationClip clip, string path, IReadOnlyList<Quaternion> rotations)
        {
            var continuous = new Quaternion[rotations.Count];
            for (var i = 0; i < rotations.Count; i++)
            {
                var value = rotations[i].normalized;
                if (i > 0 && Quaternion.Dot(continuous[i - 1], value) < 0f)
                    value = new Quaternion(-value.x, -value.y, -value.z, -value.w);
                continuous[i] = value;
            }
            SetRotationCurve(clip, path, "m_LocalRotation.x", continuous.Select(value => value.x).ToArray());
            SetRotationCurve(clip, path, "m_LocalRotation.y", continuous.Select(value => value.y).ToArray());
            SetRotationCurve(clip, path, "m_LocalRotation.z", continuous.Select(value => value.z).ToArray());
            SetRotationCurve(clip, path, "m_LocalRotation.w", continuous.Select(value => value.w).ToArray());
        }

        private static void SetRotationCurve(AnimationClip clip, string path, string property, IReadOnlyList<float> values)
        {
            var keys = values.Select((value, frame) => new Keyframe(frame / FrameRate, value)).ToArray();
            var curve = new AnimationCurve(keys) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever };
            for (var i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
        }

        private static TopologyContract AnalyzeTopology(SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh;
            if (mesh.subMeshCount <= 2) throw new InvalidOperationException("The approved Ostinato mesh has no HookBlade submesh.");
            var triangles = mesh.GetIndices(2);
            var bladeIndices = triangles.Distinct().ToArray();
            var weldedGroups = bladeIndices.GroupBy(index => Quantize(mesh.vertices[index]))
                .ToDictionary(group => group.Key, group => group.ToArray());
            var representativeByIndex = weldedGroups.Values.SelectMany(group => group.Select(index => (index, representative: group[0])))
                .ToDictionary(value => value.index, value => value.representative);
            var adjacency = new Dictionary<int, HashSet<int>>();
            for (var i = 0; i < triangles.Length; i += 3)
            {
                AddAdjacent(adjacency, representativeByIndex[triangles[i]], representativeByIndex[triangles[i + 1]]);
                AddAdjacent(adjacency, representativeByIndex[triangles[i + 1]], representativeByIndex[triangles[i + 2]]);
                AddAdjacent(adjacency, representativeByIndex[triangles[i + 2]], representativeByIndex[triangles[i]]);
            }
            var remaining = adjacency.Keys.ToHashSet();
            var components = new List<int[]>();
            while (remaining.Count > 0)
            {
                var seed = remaining.First();
                remaining.Remove(seed);
                var found = new HashSet<int> { seed };
                var stack = new Stack<int>();
                stack.Push(seed);
                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    foreach (var next in adjacency[current])
                        if (remaining.Remove(next)) { found.Add(next); stack.Push(next); }
                }
                components.Add(bladeIndices.Where(index => found.Contains(representativeByIndex[index])).OrderBy(value => value).ToArray());
            }
            var main = components.OrderByDescending(component => component.Length).Take(2).ToArray();
            if (main.Length != 2 || main[1].Length < 100)
                throw new InvalidOperationException("Expected two large Ostinato blade components.");
            var boneNames = renderer.bones.Select(bone => bone.name).ToArray();
            var left = SideWeight(mesh, main[0], boneNames, "Left") > SideWeight(mesh, main[0], boneNames, "Right") ? main[0] : main[1];
            var right = ReferenceEquals(left, main[0]) ? main[1] : main[0];
            if (SideWeight(mesh, left, boneNames, "Left") <= SideWeight(mesh, left, boneNames, "Right") ||
                SideWeight(mesh, right, boneNames, "Right") <= SideWeight(mesh, right, boneNames, "Left"))
                throw new InvalidOperationException("The two large HookBlade components cannot be assigned to left and right rigs.");

            var duplicateSources = left.Concat(right).ToArray();
            var duplicateBySource = new Dictionary<int, int>();
            for (var i = 0; i < duplicateSources.Length; i++) duplicateBySource[duplicateSources[i]] = mesh.vertexCount + i;
            var leftSet = left.ToHashSet();
            var rightSet = right.ToHashSet();
            var leftEdges = FindBoundaryEdges(triangles, leftSet, representativeByIndex);
            var rightEdges = FindBoundaryEdges(triangles, rightSet, representativeByIndex);
            if (leftEdges.Length == 0 || rightEdges.Length == 0)
                throw new InvalidOperationException("A main Ostinato blade has no open wrist boundary.");
            var bodyVertices = Enumerable.Range(0, mesh.subMeshCount).Where(index => index != 2)
                .SelectMany(mesh.GetIndices).Distinct().OrderBy(index => index).ToArray();
            var torsoBones = new HashSet<string>(new[] { "Hips", "Spine", "Spine1", "Spine2", "Neck", "Head" }, StringComparer.Ordinal);
            var torsoVertices = bodyVertices.Where(index => SumNamedWeight(mesh.boneWeights[index], boneNames, torsoBones) >= 0.5f).ToArray();
            if (torsoVertices.Length < 50) throw new InvalidOperationException("The Ostinato torso surface could not be isolated for front-clearance inspection.");
            var leftBoundarySources = leftEdges.SelectMany(edge => new[] { edge.A, edge.B }).Distinct().ToArray();
            var rightBoundarySources = rightEdges.SelectMany(edge => new[] { edge.A, edge.B }).Distinct().ToArray();
            return new TopologyContract(
                left, right,
                left.Select(index => duplicateBySource[index]).ToArray(),
                right.Select(index => duplicateBySource[index]).ToArray(),
                duplicateSources, duplicateBySource, leftEdges, rightEdges,
                leftBoundarySources, rightBoundarySources,
                leftBoundarySources.Select(index => duplicateBySource[index]).ToArray(),
                rightBoundarySources.Select(index => duplicateBySource[index]).ToArray(),
                bodyVertices, torsoVertices,
                components.Count);
        }

        private static Edge[] FindBoundaryEdges(IReadOnlyList<int> triangles, HashSet<int> component,
            IReadOnlyDictionary<int, int> representativeByIndex)
        {
            var counts = new Dictionary<(int, int), (int Count, Edge Oriented)>();
            for (var i = 0; i < triangles.Count; i += 3)
            {
                if (!component.Contains(triangles[i])) continue;
                CountEdge(counts, triangles[i], triangles[i + 1], representativeByIndex);
                CountEdge(counts, triangles[i + 1], triangles[i + 2], representativeByIndex);
                CountEdge(counts, triangles[i + 2], triangles[i], representativeByIndex);
            }
            return counts.Values.Where(value => value.Count == 1).Select(value => value.Oriented).ToArray();
        }

        private static void CountEdge(Dictionary<(int, int), (int Count, Edge Oriented)> counts, int a, int b,
            IReadOnlyDictionary<int, int> representativeByIndex)
        {
            var weldedA = representativeByIndex[a];
            var weldedB = representativeByIndex[b];
            if (weldedA == weldedB) return;
            var key = weldedA < weldedB ? (weldedA, weldedB) : (weldedB, weldedA);
            if (counts.TryGetValue(key, out var value)) counts[key] = (value.Count + 1, value.Oriented);
            else counts[key] = (1, new Edge(a, b));
        }

        private static (int X, int Y, int Z) Quantize(Vector3 value)
        {
            const float scale = 100000f;
            return (Mathf.RoundToInt(value.x * scale), Mathf.RoundToInt(value.y * scale), Mathf.RoundToInt(value.z * scale));
        }

        private static Vector3 Average(IReadOnlyList<Vector3> vertices, IReadOnlyList<int> indices)
        {
            if (indices.Count == 0) throw new InvalidOperationException("Cannot average an empty Ostinato vertex set.");
            var sum = Vector3.zero;
            foreach (var index in indices) sum += vertices[index];
            return sum / indices.Count;
        }

        private static void AddAdjacent(Dictionary<int, HashSet<int>> adjacency, int a, int b)
        {
            if (!adjacency.TryGetValue(a, out var left)) adjacency[a] = left = new HashSet<int>();
            if (!adjacency.TryGetValue(b, out var right)) adjacency[b] = right = new HashSet<int>();
            left.Add(b); right.Add(a);
        }

        private static float SideWeight(Mesh mesh, IEnumerable<int> indices, IReadOnlyList<string> boneNames, string side)
        {
            return indices.Sum(index => SumSideWeight(mesh.boneWeights[index], boneNames, side));
        }

        private static float SumSideWeight(BoneWeight weight, IReadOnlyList<string> boneNames, string side)
        {
            var value = 0f;
            if (weight.weight0 > 0f && boneNames[weight.boneIndex0].StartsWith(side, StringComparison.Ordinal)) value += weight.weight0;
            if (weight.weight1 > 0f && boneNames[weight.boneIndex1].StartsWith(side, StringComparison.Ordinal)) value += weight.weight1;
            if (weight.weight2 > 0f && boneNames[weight.boneIndex2].StartsWith(side, StringComparison.Ordinal)) value += weight.weight2;
            if (weight.weight3 > 0f && boneNames[weight.boneIndex3].StartsWith(side, StringComparison.Ordinal)) value += weight.weight3;
            return value;
        }

        private static float SumNamedWeight(BoneWeight weight, IReadOnlyList<string> boneNames, ISet<string> names)
        {
            var value = 0f;
            if (weight.weight0 > 0f && names.Contains(boneNames[weight.boneIndex0])) value += weight.weight0;
            if (weight.weight1 > 0f && names.Contains(boneNames[weight.boneIndex1])) value += weight.weight1;
            if (weight.weight2 > 0f && names.Contains(boneNames[weight.boneIndex2])) value += weight.weight2;
            if (weight.weight3 > 0f && names.Contains(boneNames[weight.boneIndex3])) value += weight.weight3;
            return value;
        }

        private static Quaternion ComputeTargetCorrection(IReadOnlyList<Vector3> points, Vector3 bodyCenter, Vector3 pivot)
        {
            var longAxis = BladeRootToTipAxis(points, pivot);
            var targetLong = Vector3.ProjectOnPlane(bodyCenter - pivot, Vector3.up).normalized;
            if (targetLong.sqrMagnitude < 0.9f) throw new InvalidOperationException("A blade tip direction toward the body cannot be projected horizontally.");
            return Quaternion.FromToRotation(longAxis, targetLong);
        }

        private static Quaternion ComputeRigidTargetCorrection(IReadOnlyList<Vector3> points, Vector3 targetLong,
            Vector3 targetCurve, Vector3 pivot)
        {
            targetLong = Vector3.ProjectOnPlane(targetLong, Vector3.up).normalized;
            if (targetLong.sqrMagnitude < 0.9f) throw new InvalidOperationException("A rigid blade lateral target axis could not be measured.");
            var longAxis = BladeRootToTipAxis(points, pivot);
            var first = Quaternion.FromToRotation(longAxis, targetLong);
            var center = points.Aggregate(Vector3.zero, (sum, point) => sum + point) / points.Count;
            var curveAxis = Vector3.ProjectOnPlane(center - pivot, longAxis).normalized;
            if (curveAxis.sqrMagnitude < 0.5f) curveAxis = SecondaryAxis(points, longAxis);
            var rotatedCurve = Vector3.ProjectOnPlane(first * curveAxis, targetLong).normalized;
            targetCurve = Vector3.ProjectOnPlane(targetCurve, targetLong).normalized;
            if (targetCurve.sqrMagnitude < 0.5f) throw new InvalidOperationException("A rigid blade forward curve axis could not be measured.");
            var roll = Vector3.SignedAngle(rotatedCurve, targetCurve, targetLong);
            return Quaternion.AngleAxis(roll, targetLong) * first;
        }

        private static Vector3 BladeRootToTipAxis(IReadOnlyList<Vector3> points, Vector3 pivot)
        {
            var distalCount = Mathf.Max(4, Mathf.CeilToInt(points.Count * 0.08f));
            var distalCenter = points.OrderByDescending(point => (point - pivot).sqrMagnitude).Take(distalCount)
                .Aggregate(Vector3.zero, (sum, point) => sum + point) / distalCount;
            var axis = (distalCenter - pivot).normalized;
            if (axis.sqrMagnitude < 0.9f) throw new InvalidOperationException("A blade root-to-tip axis could not be measured.");
            return axis;
        }

        private static Vector3 PrincipalAxis(IReadOnlyList<Vector3> points)
        {
            BuildCovariance(points, out var xx, out var xy, out var xz, out var yy, out var yz, out var zz);
            var axis = Vector3.right;
            for (var i = 0; i < 20; i++) axis = MultiplyCovariance(axis, xx, xy, xz, yy, yz, zz).normalized;
            return axis;
        }

        private static Vector3 SecondaryAxis(IReadOnlyList<Vector3> points, Vector3 primary)
        {
            BuildCovariance(points, out var xx, out var xy, out var xz, out var yy, out var yz, out var zz);
            var axis = Mathf.Abs(Vector3.Dot(primary, Vector3.up)) < 0.8f ? Vector3.up : Vector3.forward;
            for (var i = 0; i < 24; i++)
            {
                axis = Vector3.ProjectOnPlane(MultiplyCovariance(axis, xx, xy, xz, yy, yz, zz), primary).normalized;
                if (axis.sqrMagnitude < 0.5f) axis = Vector3.ProjectOnPlane(Vector3.forward, primary).normalized;
            }
            return axis;
        }

        private static void BuildCovariance(IReadOnlyList<Vector3> points, out float xx, out float xy, out float xz,
            out float yy, out float yz, out float zz)
        {
            var center = points.Aggregate(Vector3.zero, (sum, point) => sum + point) / points.Count;
            xx = xy = xz = yy = yz = zz = 0f;
            foreach (var point in points)
            {
                var p = point - center;
                xx += p.x * p.x; xy += p.x * p.y; xz += p.x * p.z;
                yy += p.y * p.y; yz += p.y * p.z; zz += p.z * p.z;
            }
        }

        private static Vector3 MultiplyCovariance(Vector3 value, float xx, float xy, float xz, float yy, float yz, float zz)
        {
            return new Vector3(xx * value.x + xy * value.y + xz * value.z,
                xy * value.x + yy * value.y + yz * value.z,
                xz * value.x + yz * value.y + zz * value.z);
        }

        private static float CorrectionFactor(int frame)
        {
            if (frame < RotationFirstFrame || frame >= ReturnLastFrame) return 0f;
            if (frame <= RotationFullFrame)
                return Mathf.SmoothStep(0f, 1f, (frame - (RotationFirstFrame - 1f)) / (RotationFullFrame - RotationFirstFrame + 1f));
            if (frame <= HoldLastFrame) return 1f;
            return Mathf.SmoothStep(1f, 0f, (frame - HoldLastFrame) / (float)(ReturnLastFrame - HoldLastFrame));
        }

        private static void SetFrameShapeCurve(AnimationClip clip, string rendererPath, string shapeName, int frame)
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe((frame - 1) / FrameRate, 0f),
                new Keyframe(frame / FrameRate, 100f), new Keyframe((frame + 1) / FrameRate, 0f),
                new Keyframe(LastFrame / FrameRate, 0f))
            { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever };
            for (var i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(rendererPath, typeof(SkinnedMeshRenderer), "blendShape." + shapeName), curve);
        }

        private static RigidObjectMetrics InspectRigidObjectContract(AnimationClip sourceClip, AnimationClip correctedClip,
            Mesh bodyMesh, TopologyContract topology)
        {
            if (Mathf.Abs(sourceClip.length - correctedClip.length) > 0.0001f || Mathf.Abs(sourceClip.frameRate - correctedClip.frameRate) > 0.001f)
                throw new InvalidOperationException("Corrected clip timing differs from the source clip.");
            InspectDerivedMesh(RequireRenderer(RequireAsset<GameObject>(OstinatoScissorAttackAnimation.ApprovedModelPath)).sharedMesh,
                bodyMesh, topology);
            InspectRigidBladeMeshes(topology);
            var correctionBindings = AnimationUtility.GetCurveBindings(correctedClip).Where(IsCorrectionBinding).ToArray();
            if (correctionBindings.Length != 8 || correctionBindings.Any(binding => !binding.propertyName.StartsWith("m_LocalRotation.", StringComparison.Ordinal)))
                throw new InvalidOperationException("Rigid blade correction must contain exactly eight quaternion rotation curves.");
            if (AnimationUtility.GetCurveBindings(correctedClip).Any(binding => binding.type == typeof(SkinnedMeshRenderer) &&
                    binding.propertyName.StartsWith("blendShape.", StringComparison.Ordinal)))
                throw new InvalidOperationException("The corrected clip contains a blade BlendShape curve.");

            var sourceModel = CreatePlaybackModel(false);
            var correctedModel = CreatePlaybackModel(true);
            var sourceBaked = new Mesh();
            var correctedBaked = new Mesh();
            try
            {
                var sourceRenderer = RequireRenderer(sourceModel);
                var correctedRenderer = RequireRenderer(correctedModel);
                var leftRoot = FindDescendant(correctedModel.transform, LeftBladeRootName);
                var rightRoot = FindDescendant(correctedModel.transform, RightBladeRootName);
                InspectRigidBladeObject(leftRoot, RequireAsset<Mesh>(LeftRigidBladeMeshPath), "LeftHand");
                InspectRigidBladeObject(rightRoot, RequireAsset<Mesh>(RightRigidBladeMeshPath), "RightHand");
                var leftBasePosition = leftRoot.localPosition;
                var rightBasePosition = rightRoot.localPosition;
                var maxBody = 0f; var maxShape = 0f; var maxEdge = 0f; var maxPosition = 0f; var maxScale = 0f;
                var maxHorizontal = 0f; var minLeftRight = 1f; var minRightLeft = 1f; var minFront = float.PositiveInfinity;
                var maxHand = 0f; var maxForeArm = 0f;
                var minFrontFrame = -1f; var minBladeFront = 0f; var minTorsoFront = 0f; var minRootFront = 0f;
                var leftEdges = BuildUniqueEdges(Enumerable.Range(0, leftRoot.GetComponent<MeshFilter>().sharedMesh.vertexCount).ToArray());
                var rightEdges = BuildUniqueEdges(Enumerable.Range(0, rightRoot.GetComponent<MeshFilter>().sharedMesh.vertexCount).ToArray());
                for (var halfFrame = 0; halfFrame <= LastFrame * 2; halfFrame++)
                {
                    var time = halfFrame / (FrameRate * 2f);
                    SampleAtTime(sourceModel, sourceClip, time);
                    SampleAtTime(correctedModel, correctedClip, time);
                    sourceRenderer.BakeMesh(sourceBaked); correctedRenderer.BakeMesh(correctedBaked);
                    var sourceVertices = sourceBaked.vertices; var correctedVertices = correctedBaked.vertices;
                    foreach (var index in topology.BodyVertices)
                        maxBody = Mathf.Max(maxBody, Vector3.Distance(sourceVertices[index], correctedVertices[index]));
                    maxPosition = Mathf.Max(maxPosition,
                        Vector3.Distance(leftRoot.localPosition, leftBasePosition),
                        Vector3.Distance(rightRoot.localPosition, rightBasePosition));
                    maxScale = Mathf.Max(maxScale, ScaleDeviation(leftRoot.localScale), ScaleDeviation(rightRoot.localScale));
                    var leftPoints = WorldVertices(leftRoot); var rightPoints = WorldVertices(rightRoot);
                    AccumulateRigidShape(leftRoot, leftPoints, leftEdges, ref maxShape, ref maxEdge);
                    AccumulateRigidShape(rightRoot, rightPoints, rightEdges, ref maxShape, ref maxEdge);
                    var frame = time * FrameRate;
                    if (frame >= 78f && frame <= HoldLastFrame)
                    {
                        var bodyRight = Vector3.ProjectOnPlane(correctedModel.transform.right, Vector3.up).normalized;
                        var leftLong = BladeRootToTipAxis(leftPoints, leftRoot.position);
                        var rightLong = BladeRootToTipAxis(rightPoints, rightRoot.position);
                        maxHorizontal = Mathf.Max(maxHorizontal,
                            HorizontalAngle(leftLong), HorizontalAngle(rightLong));
                        minLeftRight = Mathf.Min(minLeftRight, Vector3.Dot(leftLong, bodyRight));
                        minRightLeft = Mathf.Min(minRightLeft, Vector3.Dot(rightLong, -bodyRight));
                    }
                    if (frame >= RotationFullFrame && frame <= HoldLastFrame)
                    {
                        var spine = FindDescendant(correctedModel.transform, "Spine").position;
                        var front = Vector3.ProjectOnPlane(correctedModel.transform.forward, Vector3.up).normalized;
                        if (front.sqrMagnitude < 0.5f) throw new InvalidOperationException("Ostinato torso-front axis could not be measured.");
                        var bodyRight = Vector3.ProjectOnPlane(correctedModel.transform.right, Vector3.up).normalized;
                        front = Vector3.ProjectOnPlane(front, bodyRight).normalized;
                        var torsoPoints = topology.TorsoVertices.Select(index =>
                            correctedRenderer.transform.TransformPoint(correctedVertices[index])).ToArray();
                        var torsoLateralMin = torsoPoints.Min(point => Vector3.Dot(point - spine, bodyRight));
                        var torsoLateralMax = torsoPoints.Max(point => Vector3.Dot(point - spine, bodyRight));
                        var torsoHeightMin = torsoPoints.Min(point => point.y);
                        var torsoHeightMax = torsoPoints.Max(point => point.y);
                        var leftRootGuard = leftPoints.Max(point => Vector3.Distance(point, leftRoot.position)) * 0.2f;
                        var rightRootGuard = rightPoints.Max(point => Vector3.Distance(point, rightRoot.position)) * 0.2f;
                        var bladeBodyPoints = leftPoints.Where(point => Vector3.Distance(point, leftRoot.position) > leftRootGuard)
                            .Concat(rightPoints.Where(point => Vector3.Distance(point, rightRoot.position) > rightRootGuard));
                        foreach (var point in bladeBodyPoints)
                        {
                            var lateral = Vector3.Dot(point - spine, bodyRight);
                            if (lateral < torsoLateralMin || lateral > torsoLateralMax ||
                                point.y < torsoHeightMin || point.y > torsoHeightMax) continue;
                            var torsoFront = torsoPoints.OrderBy(torsoPoint =>
                                Mathf.Pow(Vector3.Dot(torsoPoint - spine, bodyRight) - lateral, 2f) +
                                Mathf.Pow(torsoPoint.y - point.y, 2f)).Take(16)
                                .Max(torsoPoint => Vector3.Dot(torsoPoint - spine, front));
                            var bladeFront = Vector3.Dot(point - spine, front);
                            var clearance = bladeFront - torsoFront;
                            if (clearance < minFront)
                            {
                                minFront = clearance; minFrontFrame = frame; minBladeFront = bladeFront; minTorsoFront = torsoFront;
                                minRootFront = Mathf.Min(Vector3.Dot(leftRoot.position - spine, front), Vector3.Dot(rightRoot.position - spine, front));
                            }
                        }
                    }
                    maxHand = Mathf.Max(maxHand,
                        Quaternion.Angle(FindDescendant(sourceModel.transform, "LeftHand").localRotation, FindDescendant(correctedModel.transform, "LeftHand").localRotation),
                        Quaternion.Angle(FindDescendant(sourceModel.transform, "RightHand").localRotation, FindDescendant(correctedModel.transform, "RightHand").localRotation));
                    maxForeArm = Mathf.Max(maxForeArm,
                        Quaternion.Angle(FindDescendant(sourceModel.transform, "LeftForeArm").localRotation, FindDescendant(correctedModel.transform, "LeftForeArm").localRotation),
                        Quaternion.Angle(FindDescendant(sourceModel.transform, "RightForeArm").localRotation, FindDescendant(correctedModel.transform, "RightForeArm").localRotation));
                }
                if (maxBody > 0.0001f || maxPosition > 0.000001f || maxScale > 0.000001f)
                    throw new InvalidOperationException("A protected body or rigid blade transform channel changed. Body=" + Format(maxBody) +
                                                        ", Position=" + Format(maxPosition) + ", Scale=" + Format(maxScale));
                if (maxShape > 0.00001f || maxEdge > 0.00001f)
                    throw new InvalidOperationException("A rigid blade changed shape. Shape=" + Format(maxShape) + ", Edge=" + Format(maxEdge));
                if (maxHorizontal > 2f || minLeftRight < 0.6f || minRightLeft < 0.6f)
                    throw new InvalidOperationException("A held rigid blade direction is incorrect. Horizontal=" + Format(maxHorizontal) +
                                                        ", Left=" + Format(minLeftRight) + ", Right=" + Format(minRightLeft));
                if (float.IsPositiveInfinity(minFront)) minFront = 0f;
                if (minFront < 0f) throw new InvalidOperationException("A rigid blade enters the animated torso projection. Clearance=" + Format(minFront) +
                    ", Frame=" + Format(minFrontFrame) + ", BladeFront=" + Format(minBladeFront) +
                    ", TorsoFront=" + Format(minTorsoFront) + ", MinRootFront=" + Format(minRootFront));
                if (maxHand > 0.0001f || maxForeArm > 0.0001f)
                    throw new InvalidOperationException("An existing Hand or ForeArm rotation changed.");
                return new RigidObjectMetrics(maxBody, maxShape, maxEdge, maxPosition, maxScale, maxHorizontal,
                    minLeftRight, minRightLeft, minFront, maxHand, maxForeArm, correctionBindings.Length);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceBaked); UnityEngine.Object.DestroyImmediate(correctedBaked);
                UnityEngine.Object.DestroyImmediate(sourceModel); UnityEngine.Object.DestroyImmediate(correctedModel);
            }
        }

        private static void SampleAtTime(GameObject model, AnimationClip clip, float time)
        {
            var renderer = RequireRenderer(model);
            for (var shape = 0; shape < renderer.sharedMesh.blendShapeCount; shape++) renderer.SetBlendShapeWeight(shape, 0f);
            clip.SampleAnimation(model, time);
        }

        private static Vector3[] WorldVertices(Transform root)
        {
            return root.GetComponent<MeshFilter>().sharedMesh.vertices.Select(root.TransformPoint).ToArray();
        }

        private static void AccumulateRigidShape(Transform root, IReadOnlyList<Vector3> worldPoints,
            IReadOnlyList<(int A, int B)> edges, ref float maxShape, ref float maxEdge)
        {
            var local = root.GetComponent<MeshFilter>().sharedMesh.vertices;
            for (var i = 0; i < local.Length; i++)
                maxShape = Mathf.Max(maxShape, Vector3.Distance(root.InverseTransformPoint(worldPoints[i]), local[i]));
            foreach (var edge in edges)
                maxEdge = Mathf.Max(maxEdge, Mathf.Abs(Vector3.Distance(worldPoints[edge.A], worldPoints[edge.B]) -
                                                        Vector3.Distance(local[edge.A], local[edge.B])));
        }

        private static float ScaleDeviation(Vector3 scale)
        {
            return Mathf.Max(Mathf.Abs(scale.x - 1f), Mathf.Abs(scale.y - 1f), Mathf.Abs(scale.z - 1f));
        }

        private static float HorizontalAngle(Vector3 axis)
        {
            return Mathf.Abs(Mathf.Asin(Mathf.Clamp(axis.normalized.y, -1f, 1f)) * Mathf.Rad2Deg);
        }

        private static InspectionMetrics InspectContract(AnimationClip sourceClip, AnimationClip correctedClip,
            Mesh derivedMesh, TopologyContract topology)
        {
            if (Mathf.Abs(sourceClip.length - correctedClip.length) > 0.0001f || Mathf.Abs(sourceClip.frameRate - correctedClip.frameRate) > 0.001f)
                throw new InvalidOperationException("Corrected clip timing differs from the source clip.");
            var sourceModel = CreatePlaybackModel(false);
            var correctedModel = CreatePlaybackModel(true);
            var sourceBaked = new Mesh();
            var correctedBaked = new Mesh();
            try
            {
                var sourceRenderer = RequireRenderer(sourceModel);
                var correctedRenderer = RequireRenderer(correctedModel);
                var maxBody = 0f; var maxPre = 0f; var maxReturn = 0f; var maxRigid = 0f; var maxEdge = 0f; var maxHorizontal = 0f;
                var maxBoundaryCenter = 0f; var minTipTowardBody = 1f; var maxHand = 0f; var maxForeArm = 0f;
                var leftEdges = BuildUniqueEdges(topology.LeftSources);
                var rightEdges = BuildUniqueEdges(topology.RightSources);
                for (var frame = 0; frame <= LastFrame; frame++)
                {
                    Sample(sourceModel, sourceClip, frame);
                    Sample(correctedModel, correctedClip, frame);
                    sourceRenderer.BakeMesh(sourceBaked);
                    correctedRenderer.BakeMesh(correctedBaked);
                    var sourceVertices = sourceBaked.vertices;
                    var correctedVertices = correctedBaked.vertices;
                    foreach (var index in topology.BodyVertices)
                        maxBody = Mathf.Max(maxBody, Vector3.Distance(sourceVertices[index], correctedVertices[index]));
                    CompareSide(frame, sourceRenderer, correctedRenderer, sourceVertices, correctedVertices,
                        topology.LeftSources, topology.LeftDuplicates, topology.LeftBoundarySources, topology.LeftBoundaryDuplicates,
                        FindDescendant(sourceModel.transform, "Spine").position, leftEdges,
                        ref maxPre, ref maxReturn, ref maxRigid, ref maxEdge, ref maxBoundaryCenter,
                        ref maxHorizontal, ref minTipTowardBody);
                    CompareSide(frame, sourceRenderer, correctedRenderer, sourceVertices, correctedVertices,
                        topology.RightSources, topology.RightDuplicates, topology.RightBoundarySources, topology.RightBoundaryDuplicates,
                        FindDescendant(sourceModel.transform, "Spine").position, rightEdges,
                        ref maxPre, ref maxReturn, ref maxRigid, ref maxEdge, ref maxBoundaryCenter,
                        ref maxHorizontal, ref minTipTowardBody);
                    maxHand = Mathf.Max(maxHand,
                        Quaternion.Angle(FindDescendant(sourceModel.transform, "LeftHand").localRotation, FindDescendant(correctedModel.transform, "LeftHand").localRotation),
                        Quaternion.Angle(FindDescendant(sourceModel.transform, "RightHand").localRotation, FindDescendant(correctedModel.transform, "RightHand").localRotation));
                    maxForeArm = Mathf.Max(maxForeArm,
                        Quaternion.Angle(FindDescendant(sourceModel.transform, "LeftForeArm").localRotation, FindDescendant(correctedModel.transform, "LeftForeArm").localRotation),
                        Quaternion.Angle(FindDescendant(sourceModel.transform, "RightForeArm").localRotation, FindDescendant(correctedModel.transform, "RightForeArm").localRotation));
                }
                if (maxBody > 0.0001f || maxPre > 0.0002f || maxReturn > 0.0002f)
                    throw new InvalidOperationException("A protected source/body vertex changed. Body=" + Format(maxBody) + ", Pre=" + Format(maxPre) + ", Return=" + Format(maxReturn));
                if (maxRigid > 0.001f || maxEdge > 0.001f)
                    throw new InvalidOperationException("Blade correction is not rigid. PositionError=" + Format(maxRigid) + ", EdgeError=" + Format(maxEdge));
                if (maxBoundaryCenter > 0.001f)
                    throw new InvalidOperationException("A blade wrist boundary center moved away from its source anchor. MaxDeviation=" + Format(maxBoundaryCenter));
                if (maxHorizontal > 2f) throw new InvalidOperationException("A held blade is not horizontal. MaxAngle=" + Format(maxHorizontal));
                if (minTipTowardBody < 0.98f) throw new InvalidOperationException("A held blade tip does not point toward the opposite side of the body. MinAlignment=" + Format(minTipTowardBody));
                if (maxHand > 0.0001f || maxForeArm > 0.0001f)
                    throw new InvalidOperationException("An existing Hand or ForeArm rotation changed.");
                return new InspectionMetrics(maxBody, maxPre, maxReturn, maxRigid, maxEdge, maxBoundaryCenter,
                    maxHorizontal, minTipTowardBody, maxHand, maxForeArm);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceBaked);
                UnityEngine.Object.DestroyImmediate(correctedBaked);
                UnityEngine.Object.DestroyImmediate(sourceModel);
                UnityEngine.Object.DestroyImmediate(correctedModel);
            }
        }

        private static void CompareSide(int frame, SkinnedMeshRenderer sourceRenderer, SkinnedMeshRenderer correctedRenderer,
            IReadOnlyList<Vector3> sourceVertices, IReadOnlyList<Vector3> correctedVertices,
            IReadOnlyList<int> sourceIndices, IReadOnlyList<int> duplicateIndices,
            IReadOnlyList<int> boundarySourceIndices, IReadOnlyList<int> boundaryDuplicateIndices, Vector3 bodyCenter,
            IReadOnlyList<(int A, int B)> edges, ref float maxPre, ref float maxReturn, ref float maxRigid,
            ref float maxEdge, ref float maxBoundaryCenter, ref float maxHorizontal, ref float minTipTowardBody)
        {
            var sourceWorld = sourceIndices.Select(index => sourceRenderer.transform.TransformPoint(sourceVertices[index])).ToArray();
            var correctedWorld = duplicateIndices.Select(index => correctedRenderer.transform.TransformPoint(correctedVertices[index])).ToArray();
            var pivotWorld = BoundaryCenter(sourceRenderer, sourceVertices, boundarySourceIndices);
            var correctedPivotWorld = BoundaryCenter(correctedRenderer, correctedVertices, boundaryDuplicateIndices);
            if (frame <= 61)
                for (var i = 0; i < sourceWorld.Length; i++) maxPre = Mathf.Max(maxPre, Vector3.Distance(sourceWorld[i], correctedWorld[i]));
            if (frame >= 99)
                for (var i = 0; i < sourceWorld.Length; i++) maxReturn = Mathf.Max(maxReturn, Vector3.Distance(sourceWorld[i], correctedWorld[i]));
            if (frame >= RotationFirstFrame && frame <= LastShapeFrame)
            {
                var correction = Quaternion.Slerp(Quaternion.identity, ComputeTargetCorrection(sourceWorld, bodyCenter, pivotWorld), CorrectionFactor(frame));
                for (var i = 0; i < sourceWorld.Length; i++)
                {
                    var expected = pivotWorld + correction * (sourceWorld[i] - pivotWorld);
                    maxRigid = Mathf.Max(maxRigid, Vector3.Distance(expected, correctedWorld[i]));
                }
                foreach (var edge in edges)
                    maxEdge = Mathf.Max(maxEdge, Mathf.Abs(Vector3.Distance(sourceWorld[edge.A], sourceWorld[edge.B]) -
                                                            Vector3.Distance(correctedWorld[edge.A], correctedWorld[edge.B])));
                maxBoundaryCenter = Mathf.Max(maxBoundaryCenter, Vector3.Distance(pivotWorld, correctedPivotWorld));
            }
            if (frame >= 78 && frame <= HoldLastFrame)
            {
                var longAxis = BladeRootToTipAxis(correctedWorld, pivotWorld);
                maxHorizontal = Mathf.Max(maxHorizontal, Mathf.Abs(Mathf.Asin(Mathf.Clamp(longAxis.normalized.y, -1f, 1f)) * Mathf.Rad2Deg));
                var towardBody = Vector3.ProjectOnPlane(bodyCenter - pivotWorld, Vector3.up).normalized;
                minTipTowardBody = Mathf.Min(minTipTowardBody, Vector3.Dot(longAxis, towardBody));
            }
        }

        private static Vector3 BoundaryCenter(SkinnedMeshRenderer renderer, IReadOnlyList<Vector3> vertices,
            IReadOnlyList<int> boundaryIndices)
        {
            if (boundaryIndices.Count == 0) throw new InvalidOperationException("A blade wrist boundary has no vertices.");
            var center = Vector3.zero;
            foreach (var index in boundaryIndices) center += renderer.transform.TransformPoint(vertices[index]);
            return center / boundaryIndices.Count;
        }

        private static IReadOnlyList<(int A, int B)> BuildUniqueEdges(IReadOnlyList<int> sourceIndices)
        {
            var count = sourceIndices.Count;
            var edges = new List<(int, int)>();
            for (var i = 0; i < count - 1; i += Math.Max(1, count / 24)) edges.Add((i, i + 1));
            return edges;
        }

        private static void InspectDerivedMesh(Mesh source, Mesh derived, TopologyContract topology)
        {
            if (derived.vertexCount != source.vertexCount || derived.subMeshCount != source.subMeshCount || derived.blendShapeCount != 0)
                throw new InvalidOperationException("Rigid blade body mesh contract changed.");
            var sourceVertices = source.vertices; var derivedVertices = derived.vertices;
            for (var i = 0; i < source.vertexCount; i++)
                if (sourceVertices[i] != derivedVertices[i]) throw new InvalidOperationException("A protected source vertex changed in the derived mesh.");
            var removed = topology.LeftSources.Concat(topology.RightSources).ToHashSet();
            var expectedHookIndices = 0;
            var sourceHook = source.GetIndices(2);
            for (var i = 0; i < sourceHook.Length; i += 3)
                if (!removed.Contains(sourceHook[i])) expectedHookIndices += 3;
            if (derived.GetIndexCount(2) != expectedHookIndices)
                throw new InvalidOperationException("The body mesh still contains a main rigid blade triangle.");
        }

        private static void InspectRigidBladeMeshes(TopologyContract topology)
        {
            var source = RequireRenderer(RequireAsset<GameObject>(OstinatoScissorAttackAnimation.ApprovedModelPath)).sharedMesh;
            InspectRigidBladeMesh(source, RequireAsset<Mesh>(LeftRigidBladeMeshPath), topology.LeftSources, topology.LeftBoundarySources);
            InspectRigidBladeMesh(source, RequireAsset<Mesh>(RightRigidBladeMeshPath), topology.RightSources, topology.RightBoundarySources);
        }

        private static void InspectRigidBladeMesh(Mesh source, Mesh rigid, IReadOnlyList<int> component, IReadOnlyList<int> boundary)
        {
            if (rigid.vertexCount != component.Count || rigid.subMeshCount != 1 || rigid.blendShapeCount != 0 || rigid.boneWeights.Length != 0)
                throw new InvalidOperationException(rigid.name + " is not an unskinned rigid mesh.");
            var pivot = Average(source.vertices, boundary);
            var vertices = rigid.vertices;
            for (var i = 0; i < component.Count; i++)
                if (Vector3.Distance(vertices[i], source.vertices[component[i]] - pivot) > 0.000001f)
                    throw new InvalidOperationException(rigid.name + " contains a modified blade vertex.");
        }

        private static AnimatorController ConfigureController(AnimationClip clip)
        {
            var controller = RequireAsset<AnimatorController>(OstinatoScissorAttackAnimation.ControllerPath);
            var stateMachine = controller.layers[0].stateMachine;
            foreach (var state in stateMachine.states.Select(entry => entry.state).ToArray()) stateMachine.RemoveState(state);
            var attackState = stateMachine.AddState(OstinatoScissorAttackAnimation.StateName);
            attackState.motion = clip; attackState.speed = 1f; attackState.writeDefaultValues = true;
            stateMachine.defaultState = attackState;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static GameObject CreatePlaybackModel(bool useDerivedMesh)
        {
            var mesh = useDerivedMesh ? RequireAsset<Mesh>(DerivedMeshPath) : RequireRenderer(RequireAsset<GameObject>(OstinatoScissorAttackAnimation.ApprovedModelPath)).sharedMesh;
            var model = CreatePlaybackModelWithMesh(mesh);
            if (useDerivedMesh)
            {
                var approvedRenderer = RequireRenderer(RequireAsset<GameObject>(OstinatoScissorAttackAnimation.ApprovedModelPath));
                ConfigureRigidBladeObjects(model, RequireRenderer(model), AnalyzeTopology(approvedRenderer));
            }
            return model;
        }

        private static GameObject CreatePlaybackModelWithMesh(Mesh mesh)
        {
            var attackAsset = RequireAsset<GameObject>(OstinatoScissorAttackAnimation.AttackModelPath);
            var approvedRenderer = RequireRenderer(RequireAsset<GameObject>(OstinatoScissorAttackAnimation.ApprovedModelPath));
            var model = PrefabUtility.InstantiatePrefab(attackAsset) as GameObject ?? throw new InvalidOperationException("Attack FBX playback model could not be instantiated.");
            model.name = "Ostinato_DownstrikeBlade_Temporary";
            model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); model.transform.localScale = Vector3.one;
            var hips = FindDescendant(model.transform, "Hips");
            if (hips.parent != null) hips.parent.name = BindingRootName;
            var renderer = RequireRenderer(model);
            RestoreApprovedRendererContract(model, renderer, approvedRenderer);
            renderer.sharedMesh = mesh;
            renderer.sharedMaterials = ApprovedMaterialPaths.Select(RequireAsset<Material>).ToArray();
            renderer.updateWhenOffscreen = true;
            return model;
        }

        private static void RestoreApprovedRendererContract(GameObject model, SkinnedMeshRenderer renderer, SkinnedMeshRenderer approvedRenderer)
        {
            renderer.sharedMesh = approvedRenderer.sharedMesh;
            renderer.bones = approvedRenderer.bones.Select(bone => FindDescendant(model.transform, bone.name)).ToArray();
            renderer.rootBone = approvedRenderer.rootBone == null ? null : FindDescendant(model.transform, approvedRenderer.rootBone.name);
        }

        private static void ConfigureRigidBladeObjects(GameObject model, SkinnedMeshRenderer renderer, TopologyContract topology)
        {
            CreateRigidBladeObject(model, renderer, "LeftHand", LeftBladeRootName,
                RequireAsset<Mesh>(LeftRigidBladeMeshPath), topology.LeftBoundarySources);
            CreateRigidBladeObject(model, renderer, "RightHand", RightBladeRootName,
                RequireAsset<Mesh>(RightRigidBladeMeshPath), topology.RightBoundarySources);
        }

        private static void CreateRigidBladeObject(GameObject model, SkinnedMeshRenderer bodyRenderer, string handName,
            string rootName, Mesh mesh, IReadOnlyList<int> boundarySources)
        {
            var hand = FindDescendant(model.transform, handName);
            var existing = hand.Find(rootName);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var root = new GameObject(rootName, typeof(MeshFilter), typeof(MeshRenderer));
            root.transform.SetParent(hand, false);
            var pivotLocal = Average(RequireRenderer(RequireAsset<GameObject>(OstinatoScissorAttackAnimation.ApprovedModelPath)).sharedMesh.vertices,
                boundarySources);
            root.transform.SetPositionAndRotation(bodyRenderer.transform.TransformPoint(pivotLocal), bodyRenderer.transform.rotation);
            root.transform.localScale = Vector3.one;
            root.GetComponent<MeshFilter>().sharedMesh = mesh;
            var bladeRenderer = root.GetComponent<MeshRenderer>();
            bladeRenderer.sharedMaterial = RequireAsset<Material>(ApprovedMaterialPaths[2]);
            bladeRenderer.shadowCastingMode = bodyRenderer.shadowCastingMode;
            bladeRenderer.receiveShadows = bodyRenderer.receiveShadows;
            bladeRenderer.lightProbeUsage = bodyRenderer.lightProbeUsage;
            bladeRenderer.reflectionProbeUsage = bodyRenderer.reflectionProbeUsage;
        }

        private static void InspectSceneRigidBladeObjects(GameObject model)
        {
            InspectRigidBladeObject(FindDescendant(model.transform, LeftBladeRootName), RequireAsset<Mesh>(LeftRigidBladeMeshPath), "LeftHand");
            InspectRigidBladeObject(FindDescendant(model.transform, RightBladeRootName), RequireAsset<Mesh>(RightRigidBladeMeshPath), "RightHand");
        }

        private static void InspectRigidBladeObject(Transform root, Mesh expectedMesh, string parentName)
        {
            if (root.parent == null || root.parent.name != parentName || root.localScale != Vector3.one)
                throw new InvalidOperationException(root.name + " is not a unit-scale child of " + parentName + ".");
            var filter = root.GetComponent<MeshFilter>();
            var renderer = root.GetComponent<MeshRenderer>();
            if (filter == null || filter.sharedMesh != expectedMesh || renderer == null ||
                root.GetComponent<SkinnedMeshRenderer>() != null || expectedMesh.blendShapeCount != 0)
                throw new InvalidOperationException(root.name + " is not a transform-only rigid blade object.");
        }

        private static GameObject RequireSceneModel(Scene scene)
        {
            var root = scene.GetRootGameObjects().SingleOrDefault(target => target.name == OstinatoScissorAttackAnimation.PlacementRootName)
                       ?? throw new InvalidOperationException("Approved Ostinato placement root is missing.");
            if (root.transform.childCount != 9) throw new InvalidOperationException("Approved Ostinato placement must contain nine slots.");
            var slot = root.transform.GetChild(3);
            if (slot.name != OstinatoScissorAttackAnimation.AttackSlotName || slot.childCount != 1)
                throw new InvalidOperationException("Ostinato attack slot 04 is not ready.");
            return slot.GetChild(0).gameObject;
        }

        private static AnimationClip RequireSourceClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(OstinatoScissorAttackAnimation.AttackModelPath).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase) &&
                               clip.name.IndexOf(AttackTakeNameFragment, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            if (clips.Length != 1) throw new InvalidOperationException("Expected one supplied Ostinato attack clip. Count=" + clips.Length);
            return clips[0];
        }

        private static Scene RequireOpenScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != OstinatoScissorAttackAnimation.ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be the active scene.");
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Unity must be in Edit Mode.");
            return scene;
        }

        private static void Sample(GameObject model, AnimationClip clip, int frame)
        {
            var renderer = RequireRenderer(model);
            for (var shape = 0; shape < renderer.sharedMesh.blendShapeCount; shape++) renderer.SetBlendShapeWeight(shape, 0f);
            clip.SampleAnimation(model, frame / FrameRate);
        }

        private static Bounds GetSampleBounds(GameObject model, AnimationClip clip, int frame)
        {
            Sample(model, clip, frame);
            var renderers = model.GetComponentsInChildren<Renderer>(true).Where(renderer => renderer.enabled).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException("Ostinato review model has no enabled renderer.");
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static Texture2D RenderSample(GameObject model, AnimationClip clip, int frame, Camera camera, Vector3 direction, Bounds framing)
        {
            Sample(model, clip, frame);
            var skinned = RequireRenderer(model);
            var baked = new Mesh { name = "Ostinato_DownstrikeBladeReviewBakedMesh" };
            skinned.BakeMesh(baked);
            var bakedObject = new GameObject("Ostinato_DownstrikeBladeReviewBakedModel", typeof(MeshFilter), typeof(MeshRenderer));
            bakedObject.layer = ReviewLayer;
            bakedObject.transform.SetParent(skinned.transform.parent, false);
            bakedObject.transform.localPosition = skinned.transform.localPosition;
            bakedObject.transform.localRotation = skinned.transform.localRotation;
            bakedObject.transform.localScale = skinned.transform.localScale;
            bakedObject.GetComponent<MeshFilter>().sharedMesh = baked;
            bakedObject.GetComponent<MeshRenderer>().sharedMaterials = skinned.sharedMaterials;
            var bounds = framing; bounds.Expand(new Vector3(0.15f, 0.12f, 0.12f));
            var target = bounds.center + Vector3.up * bounds.extents.y * 0.02f;
            var distance = Mathf.Max(bounds.extents.y, bounds.extents.x) / Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) + bounds.extents.z + 0.15f;
            camera.transform.position = target + direction * distance;
            camera.transform.rotation = Quaternion.LookRotation(target - camera.transform.position, Vector3.up);
            var rt = RenderTexture.GetTemporary(PanelSize, PanelSize, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            try
            {
                skinned.enabled = false; camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                var texture = new Texture2D(PanelSize, PanelSize, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, PanelSize, PanelSize), 0, 0, false); texture.Apply(false, false);
                return texture;
            }
            finally
            {
                skinned.enabled = true; camera.targetTexture = null; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt);
                UnityEngine.Object.DestroyImmediate(bakedObject); UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static Texture2D CombineHorizontal(IReadOnlyList<Texture2D> panels)
        {
            var result = new Texture2D(PanelSize * panels.Count, PanelSize, TextureFormat.RGBA32, false);
            for (var i = 0; i < panels.Count; i++) result.SetPixels(i * PanelSize, 0, PanelSize, PanelSize, panels[i].GetPixels());
            result.Apply(false, false); return result;
        }

        private static void WriteSheet(IReadOnlyList<byte[]> frames)
        {
            const int columns = 2; var frameWidth = PanelSize * 6; var rows = Mathf.CeilToInt(frames.Count / (float)columns);
            var sheet = new Texture2D(frameWidth * columns, PanelSize * rows, TextureFormat.RGBA32, false);
            sheet.SetPixels32(Enumerable.Repeat(new Color32(9, 12, 14, 255), sheet.width * sheet.height).ToArray());
            for (var i = 0; i < frames.Count; i++)
            {
                var frame = new Texture2D(2, 2, TextureFormat.RGBA32, false); frame.LoadImage(frames[i], false);
                var column = i % columns; var row = rows - 1 - i / columns;
                sheet.SetPixels(column * frameWidth, row * PanelSize, frameWidth, PanelSize, frame.GetPixels());
                UnityEngine.Object.DestroyImmediate(frame);
            }
            sheet.Apply(false, false);
            File.WriteAllBytes(OstinatoScissorAttackAnimation.ProjectAbsolutePath(CaptureSheetPath), sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sheet);
        }

        private static void ConfigureCameraAndLights(Camera camera, Light key, Light fill, Transform keyTransform, Transform fillTransform)
        {
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.035f, 0.045f, 0.055f, 1f);
            camera.fieldOfView = 40f; camera.nearClipPlane = 0.05f; camera.farClipPlane = 100f; camera.cullingMask = 1 << ReviewLayer;
            key.type = LightType.Directional; key.intensity = 1.45f; key.color = new Color(1f, 0.89f, 0.72f); key.cullingMask = 1 << ReviewLayer;
            keyTransform.rotation = Quaternion.Euler(38f, -32f, 0f);
            fill.type = LightType.Directional; fill.intensity = 0.78f; fill.color = new Color(0.46f, 0.66f, 1f); fill.cullingMask = 1 << ReviewLayer;
            fillTransform.rotation = Quaternion.Euler(326f, 148f, 0f);
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            foreach (var target in root.GetComponentsInChildren<Transform>(true)) target.gameObject.layer = layer;
        }

        private static Matrix4x4 BuildSkinMatrix(SkinnedMeshRenderer renderer, Mesh mesh, BoneWeight weight)
        {
            var result = new Matrix4x4();
            AddBoneMatrix(ref result, renderer, mesh, weight.boneIndex0, weight.weight0);
            AddBoneMatrix(ref result, renderer, mesh, weight.boneIndex1, weight.weight1);
            AddBoneMatrix(ref result, renderer, mesh, weight.boneIndex2, weight.weight2);
            AddBoneMatrix(ref result, renderer, mesh, weight.boneIndex3, weight.weight3);
            return result;
        }

        private static void AddBoneMatrix(ref Matrix4x4 result, SkinnedMeshRenderer renderer, Mesh mesh, int index, float weight)
        {
            if (weight <= 0f) return;
            var value = renderer.transform.worldToLocalMatrix * renderer.bones[index].localToWorldMatrix * mesh.bindposes[index];
            for (var row = 0; row < 4; row++) for (var column = 0; column < 4; column++) result[row, column] += value[row, column] * weight;
        }

        private static Matrix4x4 BuildDampedSkinInverse(Matrix4x4 matrix)
        {
            var transpose = Matrix4x4.identity; var normal = Matrix4x4.identity;
            for (var row = 0; row < 3; row++) for (var column = 0; column < 3; column++) transpose[row, column] = matrix[column, row];
            for (var row = 0; row < 3; row++) for (var column = 0; column < 3; column++)
            {
                var value = 0f; for (var axis = 0; axis < 3; axis++) value += transpose[row, axis] * matrix[axis, column];
                normal[row, column] = value + (row == column ? 0.0001f : 0f);
            }
            return normal.inverse * transpose;
        }

        private static Vector3[] AppendVertices(IReadOnlyList<Vector3> source, IReadOnlyList<int> duplicates)
        {
            var result = new Vector3[source.Count + duplicates.Count];
            for (var i = 0; i < source.Count; i++) result[i] = source[i];
            for (var i = 0; i < duplicates.Count; i++) result[source.Count + i] = source[duplicates[i]];
            return result;
        }

        private static T[] AppendChannel<T>(IReadOnlyList<T> source, IReadOnlyList<int> duplicates)
        {
            if (source.Count == 0) return Array.Empty<T>();
            var result = new T[source.Count + duplicates.Count];
            for (var i = 0; i < source.Count; i++) result[i] = source[i];
            for (var i = 0; i < duplicates.Count; i++) result[source.Count + i] = source[duplicates[i]];
            return result;
        }

        private static bool IsCorrectionBinding(EditorCurveBinding binding)
        {
            return binding.type == typeof(Transform) &&
                   binding.propertyName.StartsWith("m_LocalRotation.", StringComparison.Ordinal) &&
                   (binding.path.EndsWith("/" + LeftBladeRootName, StringComparison.Ordinal) ||
                    binding.path.EndsWith("/" + RightBladeRootName, StringComparison.Ordinal));
        }

        private static string ShapeName(string baseName, int frame) => baseName + "_" + frame.ToString("000", CultureInfo.InvariantCulture);

        private static string BuildCurveFingerprint(AnimationClip clip, Func<EditorCurveBinding, bool> include)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(clip.length); writer.Write(clip.frameRate);
                var bindings = AnimationUtility.GetCurveBindings(clip).Where(include).OrderBy(binding => binding.path, StringComparer.Ordinal)
                    .ThenBy(binding => binding.type.FullName, StringComparer.Ordinal).ThenBy(binding => binding.propertyName, StringComparer.Ordinal).ToArray();
                writer.Write(bindings.Length);
                foreach (var binding in bindings)
                {
                    writer.Write(binding.path ?? string.Empty); writer.Write(binding.type.FullName ?? string.Empty); writer.Write(binding.propertyName ?? string.Empty);
                    var curve = AnimationUtility.GetEditorCurve(clip, binding) ?? new AnimationCurve();
                    writer.Write(curve.keys.Length);
                    foreach (var key in curve.keys) { writer.Write(key.time); writer.Write(key.value); writer.Write(key.inTangent); writer.Write(key.outTangent); }
                }
            }
            stream.Position = 0; using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string ComputeSha256(string path)
        {
            using var stream = File.OpenRead(path); using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Required asset is missing: " + path);
        }

        private static SkinnedMeshRenderer RequireRenderer(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length != 1 || renderers[0].sharedMesh == null) throw new InvalidOperationException("Ostinato model must contain one valid SkinnedMeshRenderer.");
            return renderers[0];
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true).SingleOrDefault(target => target.name == name)
                   ?? throw new InvalidOperationException("Required Ostinato transform is missing: " + name);
        }

        private static void AppendTopology(StringBuilder report, TopologyContract topology)
        {
            report.AppendLine("HookBladeConnectedComponents=" + topology.ComponentCount);
            report.AppendLine("LeftRigidBladeVertices=" + topology.LeftSources.Length);
            report.AppendLine("RightRigidBladeVertices=" + topology.RightSources.Length);
            report.AppendLine("LeftWristBoundaryEdges=" + topology.LeftBoundaryEdges.Length);
            report.AppendLine("RightWristBoundaryEdges=" + topology.RightBoundaryEdges.Length);
            report.AppendLine("LeftWristBoundaryVertices=" + topology.LeftBoundarySources.Length);
            report.AppendLine("RightWristBoundaryVertices=" + topology.RightBoundarySources.Length);
            report.AppendLine("WristInteriorPivotCaps=0");
            report.AppendLine("AddedConnectorGeometry=0");
            report.AppendLine("NonBladeBaseVerticesReweighted=0");
        }

        private static void AppendMetrics(StringBuilder report, InspectionMetrics metrics)
        {
            report.AppendLine("MaxBodyVertexDeviation=" + Format(metrics.MaxBodyDeviation));
            report.AppendLine("MaxFrames0To61BladeDeviation=" + Format(metrics.MaxPreDeviation));
            report.AppendLine("MaxFrames99To159BladeDeviation=" + Format(metrics.MaxReturnDeviation));
            report.AppendLine("MaxRigidBladePositionError=" + Format(metrics.MaxRigidPositionError));
            report.AppendLine("MaxRigidBladeEdgeLengthError=" + Format(metrics.MaxEdgeLengthError));
            report.AppendLine("MaxWristBoundaryCenterDeviation=" + Format(metrics.MaxBoundaryCenterDeviation));
            report.AppendLine("MaxHoldBladeHorizontalAngleDegrees=" + Format(metrics.MaxHorizontalAngle));
            report.AppendLine("MinHoldBladeTipTowardBodyAlignment=" + Format(metrics.MinTipTowardBodyAlignment));
            report.AppendLine("MaxExistingHandRotationDeviationDegrees=" + Format(metrics.MaxHandDeviation));
            report.AppendLine("MaxExistingForeArmRotationDeviationDegrees=" + Format(metrics.MaxForeArmDeviation));
            report.AppendLine("RigidBladeRotationOnly=True");
            report.AppendLine("WristAndBodyAnimationUnchanged=True");
            report.AppendLine("ReturnFrame99AndLaterMatchesSource=True");
            report.AppendLine("BladeWristVisualConnectionGuard=True");
        }

        private static void AppendRigidObjectMetrics(StringBuilder report, RigidObjectMetrics metrics)
        {
            report.AppendLine("MaxBodyVertexDeviation=" + Format(metrics.MaxBodyDeviation));
            report.AppendLine("MaxRigidBladeShapeDeviation=" + Format(metrics.MaxRigidShapeError));
            report.AppendLine("MaxRigidBladeEdgeLengthError=" + Format(metrics.MaxRigidEdgeLengthError));
            report.AppendLine("MaxRigidBladeLocalPositionDeviation=" + Format(metrics.MaxLocalPositionDeviation));
            report.AppendLine("MaxRigidBladeLocalScaleDeviation=" + Format(metrics.MaxScaleDeviation));
            report.AppendLine("MaxHoldBladeHorizontalAngleDegrees=" + Format(metrics.MaxHorizontalAngle));
            report.AppendLine("MinLeftBladeAnatomicalRightAlignment=" + Format(metrics.MinLeftRightAlignment));
            report.AppendLine("MinRightBladeAnatomicalLeftAlignment=" + Format(metrics.MinRightLeftAlignment));
            report.AppendLine("MinBladeFrontClearanceFromTorso=" + Format(metrics.MinFrontClearance));
            report.AppendLine("MaxExistingHandRotationDeviationDegrees=" + Format(metrics.MaxHandDeviation));
            report.AppendLine("MaxExistingForeArmRotationDeviationDegrees=" + Format(metrics.MaxForeArmDeviation));
            report.AppendLine("CorrectionRotationCurveCount=" + metrics.CorrectionBindingCount);
            report.AppendLine("BladeBlendShapeCount=0");
            report.AppendLine("BladeSkinWeightCount=0");
            report.AppendLine("BladeSkinnedMeshRendererCount=0");
            report.AppendLine("BladeTransformPositionCurves=0");
            report.AppendLine("BladeTransformScaleCurves=0");
            report.AppendLine("RigidBladeTransformRotationOnly=True");
            report.AppendLine("WristAndBodyAnimationUnchanged=True");
            report.AppendLine("BladeTorsoPenetration=False");
        }

        private static string Format(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);

        private readonly struct Edge
        {
            public Edge(int a, int b) { A = a; B = b; }
            public int A { get; }
            public int B { get; }
        }

        private sealed class TopologyContract
        {
            public TopologyContract(int[] leftSources, int[] rightSources, int[] leftDuplicates, int[] rightDuplicates,
                int[] duplicateSources, Dictionary<int, int> duplicateBySource, Edge[] leftBoundaryEdges, Edge[] rightBoundaryEdges,
                int[] leftBoundarySources, int[] rightBoundarySources, int[] leftBoundaryDuplicates, int[] rightBoundaryDuplicates,
                int[] bodyVertices, int[] torsoVertices, int componentCount)
            {
                LeftSources = leftSources; RightSources = rightSources; LeftDuplicates = leftDuplicates; RightDuplicates = rightDuplicates;
                DuplicateSources = duplicateSources; DuplicateBySource = duplicateBySource; LeftBoundaryEdges = leftBoundaryEdges;
                RightBoundaryEdges = rightBoundaryEdges; LeftBoundarySources = leftBoundarySources;
                RightBoundarySources = rightBoundarySources; LeftBoundaryDuplicates = leftBoundaryDuplicates;
                RightBoundaryDuplicates = rightBoundaryDuplicates; BodyVertices = bodyVertices; TorsoVertices = torsoVertices;
                ComponentCount = componentCount;
            }
            public int[] LeftSources { get; }
            public int[] RightSources { get; }
            public int[] LeftDuplicates { get; }
            public int[] RightDuplicates { get; }
            public int[] DuplicateSources { get; }
            public Dictionary<int, int> DuplicateBySource { get; }
            public Edge[] LeftBoundaryEdges { get; }
            public Edge[] RightBoundaryEdges { get; }
            public int[] LeftBoundarySources { get; }
            public int[] RightBoundarySources { get; }
            public int[] LeftBoundaryDuplicates { get; }
            public int[] RightBoundaryDuplicates { get; }
            public int[] BodyVertices { get; }
            public int[] TorsoVertices { get; }
            public int ComponentCount { get; }
        }

        private readonly struct RigidObjectMetrics
        {
            public RigidObjectMetrics(float body, float shape, float edge, float position, float scale, float horizontal,
                float leftRight, float rightLeft, float front, float hand, float foreArm, int bindings)
            {
                MaxBodyDeviation = body; MaxRigidShapeError = shape; MaxRigidEdgeLengthError = edge;
                MaxLocalPositionDeviation = position; MaxScaleDeviation = scale; MaxHorizontalAngle = horizontal;
                MinLeftRightAlignment = leftRight; MinRightLeftAlignment = rightLeft; MinFrontClearance = front;
                MaxHandDeviation = hand; MaxForeArmDeviation = foreArm; CorrectionBindingCount = bindings;
            }
            public float MaxBodyDeviation { get; }
            public float MaxRigidShapeError { get; }
            public float MaxRigidEdgeLengthError { get; }
            public float MaxLocalPositionDeviation { get; }
            public float MaxScaleDeviation { get; }
            public float MaxHorizontalAngle { get; }
            public float MinLeftRightAlignment { get; }
            public float MinRightLeftAlignment { get; }
            public float MinFrontClearance { get; }
            public float MaxHandDeviation { get; }
            public float MaxForeArmDeviation { get; }
            public int CorrectionBindingCount { get; }
        }

        private readonly struct InspectionMetrics
        {
            public InspectionMetrics(float body, float pre, float returned, float rigid, float edge, float boundaryCenter,
                float horizontal, float tipTowardBody, float hand, float foreArm)
            {
                MaxBodyDeviation = body; MaxPreDeviation = pre; MaxReturnDeviation = returned; MaxRigidPositionError = rigid;
                MaxEdgeLengthError = edge; MaxBoundaryCenterDeviation = boundaryCenter; MaxHorizontalAngle = horizontal;
                MinTipTowardBodyAlignment = tipTowardBody;
                MaxHandDeviation = hand; MaxForeArmDeviation = foreArm;
            }
            public float MaxBodyDeviation { get; }
            public float MaxPreDeviation { get; }
            public float MaxReturnDeviation { get; }
            public float MaxRigidPositionError { get; }
            public float MaxEdgeLengthError { get; }
            public float MaxBoundaryCenterDeviation { get; }
            public float MaxHorizontalAngle { get; }
            public float MinTipTowardBodyAlignment { get; }
            public float MaxHandDeviation { get; }
            public float MaxForeArmDeviation { get; }
        }
    }
}
