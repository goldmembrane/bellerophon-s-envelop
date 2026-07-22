using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Dolore05ExecutionTarget
{
    internal static class Dolore05ExecutionTargetTransferApplyAndReview
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Dolore Enemy Placement";
        private const string ExecutionSlotName = "Dolore_05_Execution_Pull_In";
        private const string ModelName = "Dolore_Model";
        private const string AttachmentName = "Dolore_Attack_Attachment";
        private const string TipBoneName = "Bone_001";
        private const string TargetName = "Dolore_05_Execution_Target_Transfer";
        private const int ExpectedTentacleBoneCount = 13;
        private const int CaptureLayer = 31;
        private const float PierceHoldEndTime = 0.58f;
        private const float PositionTolerance = 0.00001f;
        // One-millimeter pointwise depth margin keeps the full tip visible without shortening target distance.
        private const float PointedSectionVisibilityMargin = 0.001f;

        private const string SourceRelativePath = "enemies model/transfer.fbx";
        private const string AssetFolder =
            "Assets/_Project/Art/Generated/Enemies/Dolore/ExecutionTarget";
        private const string AssetPath = AssetFolder + "/transfer.fbx";
        private const string ReviewFolder = AssetFolder + "/Review";
        private const string InspectionPath = ReviewFolder + "/Dolore_05_ExecutionTarget_Inspection.txt";
        private const string CaptureFolder = ReviewFolder + "/Dolore_05_ExecutionTarget_Diagnostic";
        private const string PierceClipPath =
            "Assets/_Project/Art/Generated/Enemies/Dolore/AttackAttachment/Animations/" +
            "Dolore_05_ExecutionPullIn_PierceHold.anim";
        private const string ControllerPath =
            "Assets/_Project/Art/Generated/Enemies/Dolore/AttackAttachment/Animations/" +
            "Dolore_05_ExecutionPullIn.controller";

        private static readonly string[] ExpectedSlotNames =
        {
            "Dolore_01_Static_Review",
            "Dolore_02_Idle",
            "Dolore_03_Move_Quadruped",
            "Dolore_04_Tentacle_Stab_Attack",
            ExecutionSlotName,
            "Dolore_06_Hit_Reaction",
            "Dolore_07_Death"
        };

        [MenuItem("Bellerophon/Enemies/Dolore/Apply Motion 4 Execution Target Transfer")]
        public static void ApplyPlacement()
        {
            var scene = RequireActiveScene();
            if (scene.isDirty)
                throw new InvalidOperationException(
                    "CargoRunMvp contains unsaved changes. The execution target tool will not overwrite them.");
            var slots = RequireSlots(scene);
            var slot = slots[4];
            var protectedRootsBefore = ProtectedRootSignatures(scene);
            var protectedSlotsBefore = ProtectedSlotSignatures(slots);
            var executionBefore = HierarchySignature(slot, TargetName);

            EnsureFolder(AssetFolder);
            EnsureFolder(ReviewFolder);
            CopyApprovedSourceAsset();
            var prefab = RequireAsset<GameObject>(AssetPath);

            var existing = slot.Find(TargetName);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var instance = PrefabUtility.InstantiatePrefab(prefab, slot) as GameObject ??
                           throw new InvalidOperationException("transfer.fbx could not be instantiated.");
            instance.name = TargetName;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            var pierce = SamplePierceLocal(slot);
            var visibilityFit = MaximumVisiblePlacementAtOrigin(instance.transform, slot, pierce);
            var placementZ = visibilityFit.MaximumPlacementZ;
            if (placementZ <= 0f)
                throw new InvalidOperationException(
                    "The transfer cannot be moved into the approved forward positive Z range while exposing the " +
                    "terminal tentacle segment beyond the torso. CalculatedZ=" + Num(placementZ) +
                    " Tip=" + Vec(pierce.Tip) + " PointedSectionRange=[" +
                    Num(pierce.PointedSectionMinimumZ) + ", " + Num(pierce.PointedSectionMaximumZ) +
                    "] OverlappingPointedVertices=" + visibilityFit.OverlappingVertexCount +
                    " PointedSectionDriverBone=" + pierce.PointedSectionDriverBone +
                    " VisibilityMargin=" + Num(PointedSectionVisibilityMargin));
            instance.transform.localPosition = new Vector3(0f, 0f, placementZ);
            EditorUtility.SetDirty(instance.transform);

            if (HierarchySignature(slot, TargetName) != executionBefore)
                throw new InvalidOperationException("The execution slot changed outside the approved target child.");
            if (!protectedRootsBefore.SequenceEqual(ProtectedRootSignatures(scene), StringComparer.Ordinal))
                throw new InvalidOperationException("A scene root outside Approved Dolore Enemy Placement changed.");
            if (!protectedSlotsBefore.SequenceEqual(ProtectedSlotSignatures(slots), StringComparer.Ordinal))
                throw new InvalidOperationException("A Dolore slot outside motion object 4 changed.");

            var metrics = InspectState(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp could not be saved.");
            AssetDatabase.SaveAssets();
            WriteInspection(metrics, "Apply", true);
            Debug.Log(
                "Dolore05ExecutionTargetTransferApplied Result=PASS LocalPosition=" + Vec(metrics.LocalPosition) +
                " EntirePointedSectionVisible=" + metrics.EntirePointedSectionVisible +
                " PointedSectionFrontClearance=" + Num(metrics.PointedSectionFrontClearance) +
                " SourceHash=" + metrics.SourceHash + " SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Inspect Motion 4 Execution Target Transfer")]
        public static void InspectPlacement()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var metrics = InspectState(scene);
            WriteInspection(metrics, "Inspect", false);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Execution target inspection changed CargoRunMvp.");
            Debug.Log(
                "Dolore05ExecutionTargetTransferInspected Result=PASS LocalPosition=" +
                Vec(metrics.LocalPosition) + " TargetBounds=" + BoundsText(metrics.TargetBounds) +
                " PierceTip=" + Vec(metrics.PierceTip) + " MeshEntryZ=" + Num(metrics.MeshEntryZ) +
                " MeshExitZ=" + Num(metrics.MeshExitZ) + " PointedSectionFrontClearance=" +
                Num(metrics.PointedSectionFrontClearance) + " EntirePointedSectionVisible=" +
                metrics.EntirePointedSectionVisible +
                " SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Capture Motion 4 Execution Target Transfer Diagnostic")]
        public static void CapturePlacementDiagnostic()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var metrics = InspectState(scene);
            var slots = RequireSlots(scene);
            var slot = slots[4];
            var placement = slot.parent;
            var activeStates = HideOtherSlots(placement, slot);
            var layerStates = SetLayerRecursively(slot, CaptureLayer);
            var pose = CaptureTentaclePose(slot);
            var cameraObject = new GameObject("Dolore Execution Target Diagnostic Camera")
            {
                hideFlags = HideFlags.DontSave
            };
            var lightObject = new GameObject("Dolore Execution Target Diagnostic Light")
            {
                hideFlags = HideFlags.DontSave
            };
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.045f, 0.045f, 0.055f, 1f);
                camera.fieldOfView = 35f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2.5f;
                light.color = new Color(1f, 0.94f, 0.88f, 1f);
                light.cullingMask = 1 << CaptureLayer;
                lightObject.transform.rotation = Quaternion.LookRotation(new Vector3(-0.45f, -0.55f, -0.70f));

                EnsureFolder(CaptureFolder);
                var absoluteFolder = ProjectAbsolutePath(CaptureFolder);
                Directory.CreateDirectory(absoluteFolder);
                foreach (var file in Directory.GetFiles(absoluteFolder, "*.png")) File.Delete(file);
                var outward = ApprovedFrameOutward(scene, slots[3]);
                var lateral = Vector3.Cross(Vector3.up, outward).normalized;
                CaptureView(camera, slot, outward, Path.Combine(absoluteFolder, "PierceHold_front.png"));
                CaptureView(camera, slot, lateral, Path.Combine(absoluteFolder, "PierceHold_side.png"));
                CaptureView(
                    camera,
                    slot,
                    (outward + lateral * 0.55f).normalized,
                    Path.Combine(absoluteFolder, "PierceHold_front3q.png"));
            }
            finally
            {
                pose.Restore();
                RestoreLayers(layerStates);
                RestoreOtherSlots(activeStates);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
            AssetDatabase.Refresh();
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Execution target capture changed CargoRunMvp.");
            Debug.Log(
                "Dolore05ExecutionTargetTransferCaptured Result=PASS Images=3 EntirePointedSectionVisible=" +
                metrics.EntirePointedSectionVisible + " PointedSectionFrontClearance=" +
                Num(metrics.PointedSectionFrontClearance) +
                " CaptureFolder=" + CaptureFolder + " SceneChanged=False.");
        }

        private static Metrics InspectState(Scene scene)
        {
            var slots = RequireSlots(scene);
            var slot = slots[4];
            var target = slot.Find(TargetName) ??
                         throw new InvalidOperationException("The approved execution target instance is missing.");
            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(target.gameObject);
            if (prefab == null || AssetDatabase.GetAssetPath(prefab) != AssetPath)
                throw new InvalidOperationException("The execution target is not an instance of transfer.fbx.");
            if (Mathf.Abs(target.localPosition.x) > PositionTolerance ||
                Mathf.Abs(target.localPosition.y) > PositionTolerance || target.localPosition.z <= 0f)
                throw new InvalidOperationException(
                    "The execution target must share local X/Y zero and remain in the forward positive Z range. Position=" +
                    Vec(target.localPosition));
            if (Quaternion.Angle(target.localRotation, Quaternion.identity) > 0.001f ||
                Vector3.Distance(target.localScale, Vector3.one) > PositionTolerance)
                throw new InvalidOperationException("The imported transfer transform defaults changed.");

            var sourcePath = Path.Combine(ProjectRoot, SourceRelativePath.Replace('/', Path.DirectorySeparatorChar));
            var assetAbsolutePath = ProjectAbsolutePath(AssetPath);
            var sourceHash = FileHash(sourcePath);
            var assetHash = FileHash(assetAbsolutePath);
            if (sourceHash != assetHash)
                throw new InvalidOperationException("The Unity transfer asset bytes differ from the approved source.");

            var pierce = SamplePierceLocal(slot);
            var tipLocal = pierce.Tip;
            var targetBounds = RendererBoundsInLocalSpace(target, slot);
            var meshCrossing = MeshCrossingAtTip(target, slot, tipLocal);
            var targetFrontmostZ = targetBounds.max.z;
            var pointwiseVisibility = EvaluatePointwiseVisibility(target, slot, pierce);
            var pointedSectionFrontClearance = pointwiseVisibility.MinimumClearance;
            var entirePointedSectionVisible =
                pointedSectionFrontClearance >= PointedSectionVisibilityMargin - PositionTolerance;
            if (!entirePointedSectionVisible)
                throw new InvalidOperationException(
                    "The entire weighted pointed tentacle section is not visible beyond the transfer frontmost " +
                    "renderer surface. " +
                    "PointedSectionRange=[" + Num(pierce.PointedSectionMinimumZ) + ", " +
                    Num(pierce.PointedSectionMaximumZ) + "] PointedSectionVertexCount=" +
                    pierce.PointedSectionVertexCount + " PointedSectionDriverBone=" +
                    pierce.PointedSectionDriverBone +
                    " MeshEntryZ=" + Num(meshCrossing.EntryZ) + " MeshExitZ=" +
                    Num(meshCrossing.ExitZ) + " TargetFrontmostZ=" + Num(targetFrontmostZ) +
                    " PointedSectionFrontClearance=" +
                    Num(pointedSectionFrontClearance) + " RequiredFrontClearance=" +
                    Num(PointedSectionVisibilityMargin) + " OverlappingPointedVertices=" +
                    pointwiseVisibility.OverlappingVertexCount);
            if (pierce.ChainMinimumZ >= meshCrossing.EntryZ - PositionTolerance)
                throw new InvalidOperationException(
                    "The tentacle chain does not enter the transfer torso before the mesh entry surface. ChainMinZ=" +
                    Num(pierce.ChainMinimumZ) + " MeshEntryZ=" + Num(meshCrossing.EntryZ));
            if (target.GetComponentsInChildren<Renderer>(true).Length == 0)
                throw new InvalidOperationException("transfer.fbx has no visible renderer.");
            if (target.GetComponentsInChildren<Collider>(true).Length != 0)
                throw new InvalidOperationException("The execution target unexpectedly contains a Collider.");

            var attachment = RequireAttachment(slot);
            var animator = attachment.GetComponent<Animator>() ??
                           throw new InvalidOperationException("The execution Animator is missing.");
            if (AssetDatabase.GetAssetPath(animator.runtimeAnimatorController) != ControllerPath)
                throw new InvalidOperationException("The execution Animator Controller changed.");
            return new Metrics(
                target.localPosition,
                targetBounds,
                tipLocal,
                meshCrossing.EntryZ,
                meshCrossing.ExitZ,
                targetFrontmostZ,
                pierce.PointedSectionMinimumZ,
                pierce.PointedSectionMaximumZ,
                pierce.PointedSectionVertexCount,
                pierce.PointedSectionDriverBone,
                pointwiseVisibility.OverlappingVertexCount,
                pointedSectionFrontClearance,
                pierce.TerminalSegmentLength,
                pierce.ChainMinimumZ,
                entirePointedSectionVisible,
                sourceHash,
                assetHash,
                target.GetComponentsInChildren<Renderer>(true).Length);
        }

        private static void CopyApprovedSourceAsset()
        {
            var sourcePath = Path.Combine(ProjectRoot, SourceRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(sourcePath))
                throw new InvalidOperationException("Approved source file is missing: " + sourcePath);
            var destinationPath = ProjectAbsolutePath(AssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ??
                                      throw new InvalidOperationException("Execution target asset folder is invalid."));
            if (!File.Exists(destinationPath) || FileHash(sourcePath) != FileHash(destinationPath))
                File.Copy(sourcePath, destinationPath, true);
            AssetDatabase.ImportAsset(
                AssetPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            if (FileHash(sourcePath) != FileHash(destinationPath))
                throw new InvalidOperationException("transfer.fbx import copy hash mismatch.");
        }

        private static PierceSample SamplePierceLocal(Transform slot)
        {
            var attachment = RequireAttachment(slot);
            var renderer = RequireTentacleRenderer(attachment);
            var tip = renderer.bones.Single(item => item.name == TipBoneName);
            var clip = RequireAsset<AnimationClip>(PierceClipPath);
            var boneStates = renderer.bones.Select(BoneState.Capture).ToArray();
            var rendererEnabled = renderer.enabled;
            try
            {
                clip.SampleAnimation(attachment.gameObject, PierceHoldEndTime);
                if (tip.parent == null)
                    throw new InvalidOperationException("The tentacle tip has no parent segment for exit clearance.");
                var tipLocal = slot.InverseTransformPoint(tip.position);
                var parentLocal = slot.InverseTransformPoint(tip.parent.position);
                var terminalSegmentLength = Vector3.Distance(tipLocal, parentLocal);
                if (terminalSegmentLength <= PositionTolerance)
                    throw new InvalidOperationException("The tentacle terminal segment length is zero.");
                var chainMinimumZ = renderer.bones
                    .Select(bone => slot.InverseTransformPoint(bone.position).z)
                    .Min();
                var pointedSection = PointedSectionRange(renderer, tip, slot);
                return new PierceSample(
                    tipLocal,
                    terminalSegmentLength,
                    chainMinimumZ,
                    pointedSection.MinimumZ,
                    pointedSection.MaximumZ,
                    pointedSection.VertexCount,
                    pointedSection.DriverBone,
                    pointedSection.Vertices);
            }
            finally
            {
                for (var index = 0; index < renderer.bones.Length; index++)
                    boneStates[index].Apply(renderer.bones[index]);
                renderer.enabled = rendererEnabled;
            }
        }

        private static PointedSection PointedSectionRange(
            SkinnedMeshRenderer renderer,
            Transform tip,
            Transform reference)
        {
            var mesh = renderer.sharedMesh ??
                       throw new InvalidOperationException("The tentacle renderer has no shared mesh.");
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var bindPoses = mesh.bindposes;
            if (vertices.Length == 0 || weights.Length != vertices.Length)
                throw new InvalidOperationException("The tentacle mesh does not expose one bone weight per vertex.");
            if (bindPoses.Length != renderer.bones.Length)
                throw new InvalidOperationException("The tentacle bind pose count does not match its rig.");

            var skinMatrices = new Matrix4x4[renderer.bones.Length];
            for (var index = 0; index < skinMatrices.Length; index++)
                skinMatrices[index] = renderer.bones[index].localToWorldMatrix * bindPoses[index];

            if (Array.IndexOf(renderer.bones, tip) < 0)
                throw new InvalidOperationException("The pointed tip bone is not part of the tentacle renderer.");
            var dominantBoneIndices = weights.Select(DominantBoneIndex).ToArray();
            var driver = tip;
            var driverBoneIndex = -1;
            while (driver != null)
            {
                var candidateIndex = Array.IndexOf(renderer.bones, driver);
                if (candidateIndex >= 0 && dominantBoneIndices.Contains(candidateIndex))
                {
                    driverBoneIndex = candidateIndex;
                    break;
                }
                driver = driver.parent;
            }
            if (driver == null || driverBoneIndex < 0)
                throw new InvalidOperationException(
                    "No weighted tentacle bone was found from the pointed tip toward its parent chain.");
            var minimumZ = float.PositiveInfinity;
            var maximumZ = float.NegativeInfinity;
            var vertexCount = 0;
            var pointedVertices = new List<Vector3>();
            for (var index = 0; index < vertices.Length; index++)
            {
                var weight = weights[index];
                if (dominantBoneIndices[index] != driverBoneIndex) continue;
                var world = Vector3.zero;
                world += WeightedVertex(skinMatrices, weight.boneIndex0, weight.weight0, vertices[index]);
                world += WeightedVertex(skinMatrices, weight.boneIndex1, weight.weight1, vertices[index]);
                world += WeightedVertex(skinMatrices, weight.boneIndex2, weight.weight2, vertices[index]);
                world += WeightedVertex(skinMatrices, weight.boneIndex3, weight.weight3, vertices[index]);
                var local = reference.InverseTransformPoint(world);
                minimumZ = Mathf.Min(minimumZ, local.z);
                maximumZ = Mathf.Max(maximumZ, local.z);
                pointedVertices.Add(local);
                vertexCount++;
            }
            if (vertexCount == 0)
                throw new InvalidOperationException("The selected pointed section driver bone has no dominant vertices.");
            return new PointedSection(
                minimumZ,
                maximumZ,
                vertexCount,
                driver.name,
                pointedVertices.ToArray());
        }

        private static int DominantBoneIndex(BoneWeight weight)
        {
            var boneIndex = weight.boneIndex0;
            var greatestWeight = weight.weight0;
            if (weight.weight1 > greatestWeight)
            {
                boneIndex = weight.boneIndex1;
                greatestWeight = weight.weight1;
            }
            if (weight.weight2 > greatestWeight)
            {
                boneIndex = weight.boneIndex2;
                greatestWeight = weight.weight2;
            }
            if (weight.weight3 > greatestWeight) boneIndex = weight.boneIndex3;
            return boneIndex;
        }

        private static Vector3 WeightedVertex(
            IReadOnlyList<Matrix4x4> skinMatrices,
            int boneIndex,
            float weight,
            Vector3 vertex)
        {
            if (weight <= 0f) return Vector3.zero;
            if (boneIndex < 0 || boneIndex >= skinMatrices.Count)
                throw new InvalidOperationException("A tentacle vertex references an invalid bone index.");
            return skinMatrices[boneIndex].MultiplyPoint3x4(vertex) * weight;
        }

        private static PoseRestore CaptureTentaclePose(Transform slot)
        {
            var attachment = RequireAttachment(slot);
            var renderer = RequireTentacleRenderer(attachment);
            var states = renderer.bones.Select(BoneState.Capture).ToArray();
            var rendererEnabled = renderer.enabled;
            RequireAsset<AnimationClip>(PierceClipPath).SampleAnimation(attachment.gameObject, PierceHoldEndTime);
            return new PoseRestore(renderer, states, rendererEnabled);
        }

        private static Bounds RendererBoundsInLocalSpace(Transform root, Transform reference)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException(root.name + " has no renderer bounds.");
            var hasBounds = false;
            var localBounds = new Bounds();
            foreach (var renderer in renderers)
            {
                var bounds = renderer.bounds;
                foreach (var corner in BoundsCorners(bounds))
                {
                    var local = reference.InverseTransformPoint(corner);
                    if (!hasBounds)
                    {
                        localBounds = new Bounds(local, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(local);
                    }
                }
            }
            return localBounds;
        }

        private static VisibilityFit MaximumVisiblePlacementAtOrigin(
            Transform target,
            Transform reference,
            PierceSample pierce)
        {
            var triangles = ProjectedTriangles(target, reference);
            var maximumPlacementZ = float.PositiveInfinity;
            var overlappingVertexCount = 0;
            foreach (var vertex in pierce.PointedSectionVertices)
            {
                if (!TryFrontSurfaceZ(triangles, vertex.x, vertex.y, out var frontSurfaceZ)) continue;
                maximumPlacementZ = Mathf.Min(
                    maximumPlacementZ,
                    vertex.z - PointedSectionVisibilityMargin - frontSurfaceZ);
                overlappingVertexCount++;
            }
            if (overlappingVertexCount == 0 || float.IsPositiveInfinity(maximumPlacementZ))
                throw new InvalidOperationException(
                    "The pointed tentacle section does not overlap the transfer projection.");
            return new VisibilityFit(maximumPlacementZ, overlappingVertexCount);
        }

        private static PointwiseVisibility EvaluatePointwiseVisibility(
            Transform target,
            Transform reference,
            PierceSample pierce)
        {
            var triangles = ProjectedTriangles(target, reference);
            var minimumClearance = float.PositiveInfinity;
            var overlappingVertexCount = 0;
            foreach (var vertex in pierce.PointedSectionVertices)
            {
                if (!TryFrontSurfaceZ(triangles, vertex.x, vertex.y, out var frontSurfaceZ)) continue;
                minimumClearance = Mathf.Min(minimumClearance, vertex.z - frontSurfaceZ);
                overlappingVertexCount++;
            }
            if (overlappingVertexCount == 0 || float.IsPositiveInfinity(minimumClearance))
                throw new InvalidOperationException(
                    "The pointed tentacle section does not overlap the placed transfer projection.");
            return new PointwiseVisibility(minimumClearance, overlappingVertexCount);
        }

        private static IReadOnlyList<ProjectedTriangle> ProjectedTriangles(
            Transform root,
            Transform reference)
        {
            var result = new List<ProjectedTriangle>();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    var baked = new Mesh();
                    try
                    {
                        skinned.BakeMesh(baked);
                        AddProjectedTriangles(baked, skinned.transform, reference, result);
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(baked);
                    }
                }
                else if (renderer is MeshRenderer meshRenderer)
                {
                    var filter = meshRenderer.GetComponent<MeshFilter>() ??
                                 throw new InvalidOperationException(meshRenderer.name + " has no MeshFilter.");
                    AddProjectedTriangles(filter.sharedMesh, filter.transform, reference, result);
                }
            }
            if (result.Count == 0)
                throw new InvalidOperationException("The transfer has no projected mesh triangles.");
            return result;
        }

        private static void AddProjectedTriangles(
            Mesh mesh,
            Transform meshTransform,
            Transform reference,
            ICollection<ProjectedTriangle> result)
        {
            if (mesh == null) throw new InvalidOperationException(meshTransform.name + " has no mesh.");
            var sourceVertices = mesh.vertices;
            var vertices = new Vector3[sourceVertices.Length];
            for (var index = 0; index < sourceVertices.Length; index++)
                vertices[index] = reference.InverseTransformPoint(meshTransform.TransformPoint(sourceVertices[index]));
            var triangles = mesh.triangles;
            for (var index = 0; index + 2 < triangles.Length; index += 3)
                result.Add(new ProjectedTriangle(
                    vertices[triangles[index]],
                    vertices[triangles[index + 1]],
                    vertices[triangles[index + 2]]));
        }

        private static bool TryFrontSurfaceZ(
            IReadOnlyList<ProjectedTriangle> triangles,
            float x,
            float y,
            out float frontSurfaceZ)
        {
            frontSurfaceZ = float.NegativeInfinity;
            var found = false;
            foreach (var triangle in triangles)
            {
                if (!triangle.TryZ(x, y, out var z)) continue;
                frontSurfaceZ = Mathf.Max(frontSurfaceZ, z);
                found = true;
            }
            return found;
        }

        private static MeshCrossing MeshCrossingAtTip(Transform root, Transform reference, Vector3 tipLocal)
        {
            var bounds = RendererBoundsInLocalSpace(root, reference);
            var rayOrigin = new Vector3(tipLocal.x, tipLocal.y, bounds.min.z - 1f);
            var intersections = new List<float>();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    var baked = new Mesh();
                    try
                    {
                        skinned.BakeMesh(baked);
                        AddMeshIntersections(baked, skinned.transform, reference, rayOrigin, intersections);
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(baked);
                    }
                }
                else if (renderer is MeshRenderer meshRenderer)
                {
                    var filter = meshRenderer.GetComponent<MeshFilter>() ??
                                 throw new InvalidOperationException(meshRenderer.name + " has no MeshFilter.");
                    AddMeshIntersections(filter.sharedMesh, filter.transform, reference, rayOrigin, intersections);
                }
            }

            intersections.Sort();
            var unique = new List<float>();
            foreach (var value in intersections)
            {
                if (unique.Count == 0 || Mathf.Abs(value - unique[unique.Count - 1]) > 0.0005f)
                    unique.Add(value);
            }
            if (unique.Count < 2)
                throw new InvalidOperationException(
                    "The PierceHold tip line did not cross two transfer mesh surfaces at X/Y=" +
                    Num(tipLocal.x) + "," + Num(tipLocal.y) + ". IntersectionCount=" + unique.Count);
            return new MeshCrossing(unique[0], unique[unique.Count - 1], unique.Count);
        }

        private static void AddMeshIntersections(
            Mesh mesh,
            Transform meshTransform,
            Transform reference,
            Vector3 rayOrigin,
            ICollection<float> intersections)
        {
            if (mesh == null) throw new InvalidOperationException(meshTransform.name + " has no mesh.");
            var sourceVertices = mesh.vertices;
            var vertices = new Vector3[sourceVertices.Length];
            for (var index = 0; index < sourceVertices.Length; index++)
                vertices[index] = reference.InverseTransformPoint(meshTransform.TransformPoint(sourceVertices[index]));
            var triangles = mesh.triangles;
            for (var index = 0; index + 2 < triangles.Length; index += 3)
            {
                if (TryRayTriangle(
                        rayOrigin,
                        Vector3.forward,
                        vertices[triangles[index]],
                        vertices[triangles[index + 1]],
                        vertices[triangles[index + 2]],
                        out var distance))
                    intersections.Add(rayOrigin.z + distance);
            }
        }

        private static bool TryRayTriangle(
            Vector3 origin,
            Vector3 direction,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            out float distance)
        {
            const float epsilon = 0.000001f;
            var edge1 = b - a;
            var edge2 = c - a;
            var cross = Vector3.Cross(direction, edge2);
            var determinant = Vector3.Dot(edge1, cross);
            if (Mathf.Abs(determinant) < epsilon)
            {
                distance = 0f;
                return false;
            }
            var inverse = 1f / determinant;
            var fromA = origin - a;
            var u = inverse * Vector3.Dot(fromA, cross);
            if (u < 0f || u > 1f)
            {
                distance = 0f;
                return false;
            }
            var cross2 = Vector3.Cross(fromA, edge1);
            var v = inverse * Vector3.Dot(direction, cross2);
            if (v < 0f || u + v > 1f)
            {
                distance = 0f;
                return false;
            }
            distance = inverse * Vector3.Dot(edge2, cross2);
            return distance >= 0f;
        }

        private static IEnumerable<Vector3> BoundsCorners(Bounds bounds)
        {
            var minimum = bounds.min;
            var maximum = bounds.max;
            for (var x = 0; x <= 1; x++)
            for (var y = 0; y <= 1; y++)
            for (var z = 0; z <= 1; z++)
                yield return new Vector3(
                    x == 0 ? minimum.x : maximum.x,
                    y == 0 ? minimum.y : maximum.y,
                    z == 0 ? minimum.z : maximum.z);
        }

        private static void CaptureView(Camera camera, Transform root, Vector3 direction, string outputPath)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled && item.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException("No visible renderer is available.");
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            var size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z, 0.5f);
            var center = bounds.center + Vector3.up * size * 0.04f;
            camera.transform.position = center + direction.normalized * size * 2.4f + Vector3.up * size * 0.15f;
            camera.transform.LookAt(center);
            var renderTexture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                texture.Apply();
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static Vector3 ApprovedFrameOutward(Scene scene, Transform attackSlot)
        {
            var renderer = RequireTentacleRenderer(RequireAttachment(attackSlot));
            var anchor = renderer.bones.Single(item => item.name == "Bone_010");
            var player = scene.GetRootGameObjects().SingleOrDefault(item => item.name == "Player") ??
                         throw new InvalidOperationException("Player root is missing.");
            var outward = Vector3.ProjectOnPlane(player.transform.position - anchor.position, Vector3.up).normalized;
            if (outward.sqrMagnitude < 0.9f)
                throw new InvalidOperationException("The approved frame outward direction is unavailable.");
            return outward;
        }

        private static void WriteInspection(Metrics metrics, string phase, bool sceneSaved)
        {
            EnsureFolder(ReviewFolder);
            var report = new StringBuilder()
                .AppendLine("Result=PASS")
                .AppendLine("Phase=" + phase)
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("Target=" + PlacementRootName + "/" + ExecutionSlotName + "/" + TargetName)
                .AppendLine("SourceFile=" + SourceRelativePath)
                .AppendLine("UnityAsset=" + AssetPath)
                .AppendLine("SourceSHA256=" + metrics.SourceHash)
                .AppendLine("UnityAssetSHA256=" + metrics.AssetHash)
                .AppendLine("LocalPosition=" + Vec(metrics.LocalPosition))
                .AppendLine("LocalRotation=(0,0,0,1)")
                .AppendLine("LocalScale=(1,1,1)")
                .AppendLine("SameLocalX=True")
                .AppendLine("SameLocalY=True")
                .AppendLine("ForwardPositiveLocalZ=True")
                .AppendLine("PierceHoldTipLocal=" + Vec(metrics.PierceTip))
                .AppendLine("TargetRendererBoundsLocal=" + BoundsText(metrics.TargetBounds))
                .AppendLine("TorsoMeshEntryZ=" + Num(metrics.MeshEntryZ))
                .AppendLine("TorsoMeshExitZ=" + Num(metrics.MeshExitZ))
                .AppendLine("TargetFrontmostRendererZ=" + Num(metrics.TargetFrontmostZ))
                .AppendLine("TentacleChainMinimumZ=" + Num(metrics.ChainMinimumZ))
                .AppendLine("TerminalSegmentLength=" + Num(metrics.TerminalSegmentLength))
                .AppendLine("PointedSectionMinimumZ=" + Num(metrics.PointedSectionMinimumZ))
                .AppendLine("PointedSectionMaximumZ=" + Num(metrics.PointedSectionMaximumZ))
                .AppendLine("PointedSectionVertexCount=" + metrics.PointedSectionVertexCount)
                .AppendLine("PointedSectionDriverBone=" + metrics.PointedSectionDriverBone)
                .AppendLine("PointedSectionOverlappingVertexCount=" +
                            metrics.PointedSectionOverlappingVertexCount)
                .AppendLine("PointwiseVisibilityMargin=" + Num(PointedSectionVisibilityMargin))
                .AppendLine("PointedSectionFrontClearance=" + Num(metrics.PointedSectionFrontClearance))
                .AppendLine("EntirePointedSectionVisible=" + metrics.EntirePointedSectionVisible)
                .AppendLine("FarthestVisibleLocalZ=True")
                .AppendLine("FullTorsoPenetration=True")
                .AppendLine("RendererCount=" + metrics.RendererCount)
                .AppendLine("ColliderCount=0")
                .AppendLine("ExecutionAnimationChanged=False")
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

        private static Transform RequireAttachment(Transform slot)
        {
            var model = Enumerable.Range(0, slot.childCount).Select(slot.GetChild)
                .SingleOrDefault(item => item.name == ModelName) ??
                        throw new InvalidOperationException(slot.name + " is missing " + ModelName + ".");
            return model.Find(AttachmentName) ??
                   throw new InvalidOperationException(slot.name + " is missing " + AttachmentName + ".");
        }

        private static SkinnedMeshRenderer RequireTentacleRenderer(Transform attachment)
        {
            return attachment.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                       .SingleOrDefault(item => item.sharedMesh != null &&
                                                item.bones.Length == ExpectedTentacleBoneCount) ??
                   throw new InvalidOperationException("The approved 13-bone tentacle renderer is missing.");
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
                .Append(" T=").Append(Vec(current.localPosition)).Append('|')
                .Append(Quat(current.localRotation)).Append('|').Append(Vec(current.localScale))
                .Append(" A=").Append(current.gameObject.activeSelf);
            foreach (var renderer in current.GetComponents<Renderer>())
            {
                builder.Append(" Mesh=").Append(AssetDatabase.GetAssetPath(
                    renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh : null));
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

        private static ActiveState[] HideOtherSlots(Transform placement, Transform target)
        {
            var states = new List<ActiveState>();
            for (var index = 0; index < placement.childCount; index++)
            {
                var child = placement.GetChild(index);
                if (child == target) continue;
                states.Add(new ActiveState(child.gameObject, child.gameObject.activeSelf));
                child.gameObject.SetActive(false);
            }
            return states.ToArray();
        }

        private static LayerState[] SetLayerRecursively(Transform root, int layer)
        {
            var states = root.GetComponentsInChildren<Transform>(true)
                .Select(item => new LayerState(item.gameObject, item.gameObject.layer))
                .ToArray();
            foreach (var state in states) state.GameObject.layer = layer;
            return states;
        }

        private static void RestoreOtherSlots(IEnumerable<ActiveState> states)
        {
            foreach (var state in states) if (state.GameObject != null) state.GameObject.SetActive(state.Value);
        }

        private static void RestoreLayers(IEnumerable<LayerState> states)
        {
            foreach (var state in states) if (state.GameObject != null) state.GameObject.layer = state.Value;
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

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path) ??
                   throw new InvalidOperationException(typeof(T).Name + " asset is missing: " + path);
        }

        private static string FileHash(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string ProjectRoot =>
            Directory.GetParent(Application.dataPath)?.FullName ??
            throw new InvalidOperationException("Unity project root is unavailable.");

        private static string ProjectAbsolutePath(string assetPath) =>
            Path.Combine(ProjectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));

        private static string Num(float value) => value.ToString("R", CultureInfo.InvariantCulture);
        private static string Vec(Vector3 value) =>
            "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + ")";
        private static string Quat(Quaternion value) =>
            "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + "," + Num(value.w) + ")";
        private static string BoundsText(Bounds value) =>
            "Center=" + Vec(value.center) + " Size=" + Vec(value.size);

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

            public static BoneState Capture(Transform value) =>
                new BoneState(value.localPosition, value.localRotation, value.localScale);

            public void Apply(Transform value)
            {
                value.localPosition = Position;
                value.localRotation = Rotation;
                value.localScale = Scale;
            }
        }

        private sealed class PoseRestore
        {
            private readonly SkinnedMeshRenderer renderer;
            private readonly BoneState[] states;
            private readonly bool rendererEnabled;

            public PoseRestore(SkinnedMeshRenderer renderer, BoneState[] states, bool rendererEnabled)
            {
                this.renderer = renderer;
                this.states = states;
                this.rendererEnabled = rendererEnabled;
            }

            public void Restore()
            {
                if (renderer == null) return;
                for (var index = 0; index < renderer.bones.Length; index++)
                    states[index].Apply(renderer.bones[index]);
                renderer.enabled = rendererEnabled;
            }
        }

        private readonly struct ActiveState
        {
            public ActiveState(GameObject gameObject, bool value)
            {
                GameObject = gameObject;
                Value = value;
            }

            public GameObject GameObject { get; }
            public bool Value { get; }
        }

        private readonly struct LayerState
        {
            public LayerState(GameObject gameObject, int value)
            {
                GameObject = gameObject;
                Value = value;
            }

            public GameObject GameObject { get; }
            public int Value { get; }
        }

        private readonly struct Metrics
        {
            public Metrics(
                Vector3 localPosition,
                Bounds targetBounds,
                Vector3 pierceTip,
                float meshEntryZ,
                float meshExitZ,
                float targetFrontmostZ,
                float pointedSectionMinimumZ,
                float pointedSectionMaximumZ,
                int pointedSectionVertexCount,
                string pointedSectionDriverBone,
                int pointedSectionOverlappingVertexCount,
                float pointedSectionFrontClearance,
                float terminalSegmentLength,
                float chainMinimumZ,
                bool entirePointedSectionVisible,
                string sourceHash,
                string assetHash,
                int rendererCount)
            {
                LocalPosition = localPosition;
                TargetBounds = targetBounds;
                PierceTip = pierceTip;
                MeshEntryZ = meshEntryZ;
                MeshExitZ = meshExitZ;
                TargetFrontmostZ = targetFrontmostZ;
                PointedSectionMinimumZ = pointedSectionMinimumZ;
                PointedSectionMaximumZ = pointedSectionMaximumZ;
                PointedSectionVertexCount = pointedSectionVertexCount;
                PointedSectionDriverBone = pointedSectionDriverBone;
                PointedSectionOverlappingVertexCount = pointedSectionOverlappingVertexCount;
                PointedSectionFrontClearance = pointedSectionFrontClearance;
                TerminalSegmentLength = terminalSegmentLength;
                ChainMinimumZ = chainMinimumZ;
                EntirePointedSectionVisible = entirePointedSectionVisible;
                SourceHash = sourceHash;
                AssetHash = assetHash;
                RendererCount = rendererCount;
            }

            public Vector3 LocalPosition { get; }
            public Bounds TargetBounds { get; }
            public Vector3 PierceTip { get; }
            public float MeshEntryZ { get; }
            public float MeshExitZ { get; }
            public float TargetFrontmostZ { get; }
            public float PointedSectionMinimumZ { get; }
            public float PointedSectionMaximumZ { get; }
            public int PointedSectionVertexCount { get; }
            public string PointedSectionDriverBone { get; }
            public int PointedSectionOverlappingVertexCount { get; }
            public float PointedSectionFrontClearance { get; }
            public float TerminalSegmentLength { get; }
            public float ChainMinimumZ { get; }
            public bool EntirePointedSectionVisible { get; }
            public string SourceHash { get; }
            public string AssetHash { get; }
            public int RendererCount { get; }
        }

        private readonly struct PierceSample
        {
            public PierceSample(
                Vector3 tip,
                float terminalSegmentLength,
                float chainMinimumZ,
                float pointedSectionMinimumZ,
                float pointedSectionMaximumZ,
                int pointedSectionVertexCount,
                string pointedSectionDriverBone,
                IReadOnlyList<Vector3> pointedSectionVertices)
            {
                Tip = tip;
                TerminalSegmentLength = terminalSegmentLength;
                ChainMinimumZ = chainMinimumZ;
                PointedSectionMinimumZ = pointedSectionMinimumZ;
                PointedSectionMaximumZ = pointedSectionMaximumZ;
                PointedSectionVertexCount = pointedSectionVertexCount;
                PointedSectionDriverBone = pointedSectionDriverBone;
                PointedSectionVertices = pointedSectionVertices;
            }

            public Vector3 Tip { get; }
            public float TerminalSegmentLength { get; }
            public float ChainMinimumZ { get; }
            public float PointedSectionMinimumZ { get; }
            public float PointedSectionMaximumZ { get; }
            public int PointedSectionVertexCount { get; }
            public string PointedSectionDriverBone { get; }
            public IReadOnlyList<Vector3> PointedSectionVertices { get; }
        }

        private readonly struct PointedSection
        {
            public PointedSection(
                float minimumZ,
                float maximumZ,
                int vertexCount,
                string driverBone,
                IReadOnlyList<Vector3> vertices)
            {
                MinimumZ = minimumZ;
                MaximumZ = maximumZ;
                VertexCount = vertexCount;
                DriverBone = driverBone;
                Vertices = vertices;
            }

            public float MinimumZ { get; }
            public float MaximumZ { get; }
            public int VertexCount { get; }
            public string DriverBone { get; }
            public IReadOnlyList<Vector3> Vertices { get; }
        }

        private readonly struct VisibilityFit
        {
            public VisibilityFit(float maximumPlacementZ, int overlappingVertexCount)
            {
                MaximumPlacementZ = maximumPlacementZ;
                OverlappingVertexCount = overlappingVertexCount;
            }

            public float MaximumPlacementZ { get; }
            public int OverlappingVertexCount { get; }
        }

        private readonly struct PointwiseVisibility
        {
            public PointwiseVisibility(float minimumClearance, int overlappingVertexCount)
            {
                MinimumClearance = minimumClearance;
                OverlappingVertexCount = overlappingVertexCount;
            }

            public float MinimumClearance { get; }
            public int OverlappingVertexCount { get; }
        }

        private readonly struct ProjectedTriangle
        {
            public ProjectedTriangle(Vector3 a, Vector3 b, Vector3 c)
            {
                A = a;
                B = b;
                C = c;
                MinimumX = Mathf.Min(a.x, b.x, c.x);
                MaximumX = Mathf.Max(a.x, b.x, c.x);
                MinimumY = Mathf.Min(a.y, b.y, c.y);
                MaximumY = Mathf.Max(a.y, b.y, c.y);
            }

            public bool TryZ(float x, float y, out float z)
            {
                const float epsilon = 0.000001f;
                if (x < MinimumX - epsilon || x > MaximumX + epsilon ||
                    y < MinimumY - epsilon || y > MaximumY + epsilon)
                {
                    z = 0f;
                    return false;
                }
                var denominator = (B.y - C.y) * (A.x - C.x) +
                                  (C.x - B.x) * (A.y - C.y);
                if (Mathf.Abs(denominator) < epsilon)
                {
                    z = 0f;
                    return false;
                }
                var weightA = ((B.y - C.y) * (x - C.x) +
                               (C.x - B.x) * (y - C.y)) / denominator;
                var weightB = ((C.y - A.y) * (x - C.x) +
                               (A.x - C.x) * (y - C.y)) / denominator;
                var weightC = 1f - weightA - weightB;
                if (weightA < -epsilon || weightB < -epsilon || weightC < -epsilon)
                {
                    z = 0f;
                    return false;
                }
                z = weightA * A.z + weightB * B.z + weightC * C.z;
                return true;
            }

            private Vector3 A { get; }
            private Vector3 B { get; }
            private Vector3 C { get; }
            private float MinimumX { get; }
            private float MaximumX { get; }
            private float MinimumY { get; }
            private float MaximumY { get; }
        }

        private readonly struct MeshCrossing
        {
            public MeshCrossing(float entryZ, float exitZ, int intersectionCount)
            {
                EntryZ = entryZ;
                ExitZ = exitZ;
                IntersectionCount = intersectionCount;
            }

            public float EntryZ { get; }
            public float ExitZ { get; }
            public int IntersectionCount { get; }
        }
    }
}
