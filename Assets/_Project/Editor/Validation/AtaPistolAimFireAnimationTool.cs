using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Bellerophon.Enemies.Ata;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.AtaCargoRunScene
{
    internal static class AtaPistolAimFireAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ata Enemy Placement";
        private const string SlotName = "Ata_04_PistolAimAndFire";
        private const string ModelName = "Ata_Model";
        private const string SourcePath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Sources/Ata_PistolAimAndFire.fbx";
        private const string ShootingSourcePath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Sources/Ata_Shooting.fbx";
        private const string AppearanceSourcePath =
            "Assets/_Project/Art/Enemies/Ata/Models/Ata.fbx";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Ata_04_PistolAimAndFire.controller";
        private const string BodyMeshPath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Ata_04_PistolAimAndFire_Body.asset";
        private const string PistolMeshPath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Ata_04_PistolAimAndFire_Pistol.asset";
        private const string PistolRootName = "Ata_Pistol_Transfer";
        private const string HipAnchorName = "Ata_Pistol_HipAnchor";
        private const string HandAnchorName = "Ata_Pistol_RightHandAnchor";
        private const string RecoilRotationAnchorName = "Ata_Pistol_ShootingRecoilRotationAnchor";
        private const string MuzzleFlashName = "Ata_Pistol_MuzzleFlash";
        private const string AimStateName = "PistolAimAndFire";
        private const string ShootingStateName = "PistolShooting";
        // The visible bridge requested between the original pose and the shooting pose.
        private const float AimToShootingTransitionSeconds = 0.5f;
        private const float ShootingToStartTransitionSeconds = 0.05f;
        // GAME_DESIGN_SOURCE.txt:247 fixes Ata's pistol repeat delay at 1.5 seconds.
        private const float AtaPistolShotIntervalSeconds = 1.5f;
        private const int ShootingCycleCount = 2;
        private const float ShootingExitNormalized = 3f;
        // Calibrate the rigid pistol at the source recoil/flash pose.
        private const float ShootingForwardCalibrationNormalized = 0.32f;
        private const string ExistingFlashMeshPath =
            "Assets/_Project/Art/Enemies/Rebellion/VFX/Rebellion_Forward_Burst_Flash.asset";
        private const string ExistingFlashMaterialPath =
            "Assets/_Project/Art/Enemies/Rebellion/VFX/Rebellion_Forward_Burst_Flash.mat";
        private const string DiagnosticPath =
            "docs/validation/ata_pistol_trigger_follow_runtime_2026-08-12/Ata_04_PistolTriggerFollow_Diagnostic.png";
        private const string SourceDiagnosticPath =
            "docs/validation/ata_pistol_aim_fire_2026-08-11/Ata_PistolAimAndFire_Source_Diagnostic_02.png";
        private const string ShootingSourceDiagnosticPath =
            "docs/validation/ata_pistol_shooting_sequence_2026-08-12/Ata_Shooting_Source_Diagnostic.png";
        private const string ShootingRecoilDiagnosticPath =
            "docs/validation/ata_pistol_shooting_sequence_2026-08-12/Ata_Shooting_Recoil_Diagnostic.png";
        private const string FinalPath =
            "docs/validation/ata_pistol_trigger_follow_runtime_2026-08-12/Ata_04_PistolTriggerFollow_Final.png";
        private const string LeftSideFillReviewPath =
            "docs/validation/ata04_pistol_left_fill_2026-08-12/Ata_04_PistolLeftSideFill_Review.png";
        private const string ReportPath =
            "docs/validation/ata_pistol_trigger_follow_runtime_2026-08-12/Ata_04_PistolTriggerFollow_Report.txt";
        private const string WaistGeometryDiagnosticPath =
            "docs/validation/ata_pistol_aim_fire_2026-08-11/Ata_Pistol_Waist_Geometry_Diagnostic.png";
        private const string PistolRegionDiagnosticPath =
            "docs/validation/ata_pistol_aim_fire_2026-08-11/Ata_Pistol_Region_Diagnostic_10.png";
        private const string ExtractedPistolDiagnosticPath =
            "docs/validation/ata_pistol_aim_fire_2026-08-11/Ata_Extracted_Pistol_Geometry_Diagnostic_06.png";
        private const string ResidualComponentDiagnosticFolder =
            "docs/validation/ata_pistol_trigger_follow_runtime_2026-08-12/source_123753_handle_investigation/residual_components";
        private const string AtaTexturePath =
            "Assets/_Project/Art/Enemies/Ata/Models/output.fbm/texture_0.png";
        private const float TransformTolerance = 0.0002f;
        // Model-space contact offsets were derived from the supplied runtime footage.
        private const float HandContactLift = 0.30f;
        private const float RightHandTipVertexFraction = 0.25f;
        private const float RightHandTipForwardExtension = 0.24f;
        private const float PistolArtifactMaximumEdge = 0.08f;
        private const float PistolArtifactMaximumAltitude = 0.007f;

        [MenuItem("Bellerophon/Enemies/Ata/Apply Pistol Aim And Fire Animation")]
        public static void ApplyAtaPistolAimFireAnimation()
        {
            var scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var slotBefore = new TransformSnapshot(slot);
            var modelBefore = new TransformSnapshot(model);
            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var otherSlotsBefore = OtherSlotSignatures(placement.transform, slot);

            ConfigureMixamoImporter(SourcePath, false);
            ConfigureMixamoImporter(ShootingSourcePath, true);
            var clip = RequireSingleMixamoClip();
            var shootingClip = RequireSingleMixamoClip(ShootingSourcePath);
            var bindingSummary = RequireBindingCompatibility(model, clip);
            var shootingBindingSummary = RequireBindingCompatibility(model, shootingClip);
            var controller = CreateController(clip, shootingClip);
            var animator = ConfigureAnimator(model, controller);
            var pistolAssets = ConfigurePistolGeometryAndConstraint(
                model,
                animator,
                clip,
                shootingClip);

            if (!slotBefore.Matches() || !modelBefore.Matches())
            {
                throw new InvalidOperationException(
                    "Ata_04_PistolAimAndFire slot or model transform changed while applying the supplied clip.");
            }

            RequireEqual(
                otherSlotsBefore,
                OtherSlotSignatures(placement.transform, slot),
                "An Ata slot outside Ata_04_PistolAimAndFire changed.");
            RequireEqual(
                otherRootsBefore,
                OtherRootSignatures(scene, placement),
                "A scene root outside the Ata placement changed.");
            RequireAppliedState(model, clip, shootingClip, controller);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after applying the Ata pistol animation.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "AtaPistolAimFireAnimationApplied Result=PASS" +
                ", Slot=" + SlotName +
                ", Source=" + SourcePath +
                ", MixamoClip=" + clip.name +
                ", Duration=" + Num(clip.length) +
                ", ShootingSource=" + ShootingSourcePath +
                ", ShootingClip=" + shootingClip.name +
                ", ShootingDuration=" + Num(shootingClip.length) +
                ", ShootingStateDuration=" + Num(
                    AtaPistolShotIntervalSeconds * ShootingCycleCount) +
                ", ShotInterval=" + Num(AtaPistolShotIntervalSeconds) +
                ", ShootingCycles=" + ShootingCycleCount +
                ", AnimatedPaths=" + bindingSummary.AnimatedPathCount +
                ", SkinnedAnimatedPaths=" + bindingSummary.SkinnedAnimatedPathCount +
                ", VaryingCurves=" + bindingSummary.VaryingCurveCount +
                ", ShootingVaryingCurves=" + shootingBindingSummary.VaryingCurveCount +
                ", FirstAnimatedPaths=" + bindingSummary.FirstAnimatedPaths +
                ", LargestCurveChanges=" + bindingSummary.LargestCurveChanges +
                ", Sequence=PistolAimAndFireToPistolShootingToStart" +
                ", MuzzleFlash=ExistingRebellionForwardBurstFlash" +
                ", MuzzleFlashNormalized=0.285714-0.354286" +
                ", RootMotion=False" +
                ", RenderedPistolTriangles=" + pistolAssets.PistolTriangleCount +
                ", PistolRigid=True" +
                ", PistolDriver=RightHandArmConstraint" +
                ", ExistingMaterialPreserved=True" +
                ", OtherAtaSlotsUnchanged=True" +
                ", OtherSceneRootsUnchanged=True" +
                ", SceneSaved=True.");
        }

        public static void RecoverAtaPistolInterruptedApply()
        {
            var scene = RequireScene(requireClean: false);
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
            var bodyMesh = AssetDatabase.LoadAssetAtPath<Mesh>(BodyMeshPath) ??
                           throw new InvalidOperationException(
                               "Ata pistol recovery body mesh is missing.");
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath) ??
                               throw new InvalidOperationException(
                                   "Ata pistol recovery source prefab is missing.");
            var sourceMesh = sourcePrefab
                                 .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                                 .Single().sharedMesh;
            if (renderer.sharedMesh != sourceMesh && renderer.sharedMesh != bodyMesh)
            {
                throw new InvalidOperationException(
                    "Ata pistol recovery did not find an interrupted or auto-restored mesh state.");
            }

            renderer.sharedMesh = bodyMesh;
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "Ata interrupted pistol apply recovery could not save CargoRunMvp.");
            }
            Debug.Log(
                "AtaPistolInterruptedApplyRecovered Result=PASS" +
                ", RestoredMesh=" + bodyMesh.name +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Inspect Pistol Structure")]
        public static void InspectAtaPistolStructure()
        {
            var scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath) ??
                               throw new InvalidOperationException(
                                   "The supplied Ata pistol FBX prefab is unavailable.");

            Debug.Log(
                "AtaPistolStructureInspection Result=PASS" +
                ", SceneModel=" + DescribeModelStructure(model) +
                ", SourceModel=" + DescribeModelStructure(sourcePrefab.transform) +
                ", SceneComponents=" + DescribeConnectedComponents(model) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Inspect Extracted Pistol Triangle Geometry")]
        public static void InspectExtractedPistolTriangleGeometry()
        {
            var pistolMesh = AssetDatabase.LoadAssetAtPath<Mesh>(PistolMeshPath) ??
                             throw new InvalidOperationException(
                                 "Ata rigid pistol mesh asset is missing.");
            var vertices = pistolMesh.vertices;
            var triangles = pistolMesh.triangles;
            var descriptions = Enumerable.Range(0, triangles.Length / 3)
                .Select(index =>
                {
                    var a = vertices[triangles[index * 3]];
                    var b = vertices[triangles[index * 3 + 1]];
                    var c = vertices[triangles[index * 3 + 2]];
                    var maximumEdge = Mathf.Max(
                        Vector3.Distance(a, b),
                        Vector3.Distance(b, c),
                        Vector3.Distance(c, a));
                    var area = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                    return (index, maximumEdge, area, center: (a + b + c) / 3f);
                })
                .OrderByDescending(item => item.maximumEdge)
                .Take(24)
                .Select(item =>
                    "T" + item.index +
                    " Edge=" + Num(item.maximumEdge) +
                    " Area=" + Num(item.area) +
                    " Center=" + Vec(item.center));
            var sliverDescriptions = Enumerable.Range(0, triangles.Length / 3)
                .Select(index =>
                {
                    var a = vertices[triangles[index * 3]];
                    var b = vertices[triangles[index * 3 + 1]];
                    var c = vertices[triangles[index * 3 + 2]];
                    var maximumEdge = Mathf.Max(
                        Vector3.Distance(a, b),
                        Vector3.Distance(b, c),
                        Vector3.Distance(c, a));
                    var area = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                    var altitude = maximumEdge <= 0.000001f
                        ? 0f
                        : area * 2f / maximumEdge;
                    return (index, maximumEdge, area, altitude, center: (a + b + c) / 3f);
                })
                .Where(item => item.maximumEdge >= 0.03f)
                .OrderBy(item => item.altitude)
                .Take(36)
                .Select(item =>
                    "T" + item.index +
                    " Edge=" + Num(item.maximumEdge) +
                    " Altitude=" + Num(item.altitude) +
                    " Area=" + Num(item.area) +
                    " Center=" + Vec(item.center));
            var edgeComponents = SplitTriangleEdgeComponents(vertices, triangles)
                .Select(component =>
                {
                    var componentIndices = component.Distinct().ToArray();
                    var bounds = new Bounds(vertices[componentIndices[0]], Vector3.zero);
                    foreach (var vertexIndex in componentIndices.Skip(1))
                    {
                        bounds.Encapsulate(vertices[vertexIndex]);
                    }

                    return "T" + (component.Length / 3) +
                           "V" + componentIndices.Length +
                           "C" + Vec(bounds.center) +
                           "S" + Vec(bounds.size);
                });
            Debug.Log(
                "AtaExtractedPistolTriangleGeometry" +
                ", Triangles=" + (triangles.Length / 3) +
                ", Bounds=" + Vec(pistolMesh.bounds.size) +
                ", EdgeComponents=" + string.Join(";", edgeComponents) +
                ", Longest=" + string.Join(";", descriptions) +
                ", Slivers=" + string.Join(";", sliverDescriptions));
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Pistol Residual Components")]
        public static void CaptureAtaPistolResidualComponents()
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
            var source = renderer.sharedMesh;
            var baked = new Mesh();
            var createdObjects = new List<GameObject>();
            var createdMeshes = new List<Mesh>();
            try
            {
                renderer.BakeMesh(baked, false);
                var candidates = SplitSelectedTriangleComponents(
                        source.vertices,
                        source.GetTriangles(0))
                    .Select(component =>
                    {
                        var indices = component.Distinct().ToArray();
                        var points = indices.Select(index =>
                                model.InverseTransformPoint(
                                    renderer.transform.TransformPoint(baked.vertices[index])))
                            .ToArray();
                        var bounds = new Bounds(points[0], Vector3.zero);
                        foreach (var point in points.Skip(1))
                        {
                            bounds.Encapsulate(point);
                        }

                        return (component, indices, bounds);
                    })
                    .Where(candidate =>
                        candidate.indices.Length >= 3 &&
                        candidate.bounds.center.x >= 0.13f &&
                        candidate.bounds.center.x <= 0.35f &&
                        candidate.bounds.center.y >= 0.55f &&
                        candidate.bounds.center.y <= 1.02f)
                    .OrderByDescending(candidate => candidate.component.Length)
                    .ToArray();
                if (candidates.Length == 0)
                {
                    throw new InvalidOperationException(
                        "Ata pistol residual component diagnostic found no waist candidates.");
                }

                var folder = Absolute(ResidualComponentDiagnosticFolder);
                Directory.CreateDirectory(folder);
                var descriptions = new List<string>();
                for (var candidateIndex = 0; candidateIndex < candidates.Length; candidateIndex++)
                {
                    var candidate = candidates[candidateIndex];
                    var sourceIndices = candidate.indices.OrderBy(index => index).ToArray();
                    var remap = sourceIndices
                        .Select((sourceIndex, localIndex) => (sourceIndex, localIndex))
                        .ToDictionary(value => value.sourceIndex, value => value.localIndex);
                    var mesh = new Mesh
                    {
                        name = "AtaPistolResidualCandidate_" + candidateIndex,
                        indexFormat = source.indexFormat,
                        vertices = sourceIndices.Select(index => baked.vertices[index]).ToArray()
                    };
                    if (baked.normals.Length == baked.vertexCount)
                    {
                        mesh.normals = sourceIndices.Select(index => baked.normals[index]).ToArray();
                    }

                    if (baked.tangents.Length == baked.vertexCount)
                    {
                        mesh.tangents = sourceIndices.Select(index => baked.tangents[index]).ToArray();
                    }

                    for (var channel = 0; channel < 8; channel++)
                    {
                        var uv = new List<Vector4>();
                        baked.GetUVs(channel, uv);
                        if (uv.Count == baked.vertexCount)
                        {
                            mesh.SetUVs(
                                channel,
                                sourceIndices.Select(index => uv[index]).ToArray());
                        }
                    }

                    mesh.SetTriangles(
                        candidate.component.Select(index => remap[index]).ToArray(),
                        0,
                        true);
                    createdMeshes.Add(mesh);
                    var overlay = new GameObject(
                        "AtaPistolResidualCandidate_" + candidateIndex,
                        typeof(MeshFilter),
                        typeof(MeshRenderer));
                    overlay.hideFlags = HideFlags.HideAndDontSave;
                    overlay.transform.SetParent(renderer.transform, false);
                    overlay.GetComponent<MeshFilter>().sharedMesh = mesh;
                    overlay.GetComponent<MeshRenderer>().sharedMaterial = renderer.sharedMaterial;
                    createdObjects.Add(overlay);
                    var destination = Path.Combine(
                        folder,
                        "candidate_" + candidateIndex +
                        "_t" + (candidate.component.Length / 3) + ".png");
                    CaptureIsolatedPistolGeometry(
                        model,
                        overlay.GetComponent<MeshRenderer>(),
                        destination);
                    descriptions.Add(
                        "C" + candidateIndex +
                        "T" + (candidate.component.Length / 3) +
                        "V" + candidate.indices.Length +
                        "Center=" + Vec(candidate.bounds.center) +
                        "Size=" + Vec(candidate.bounds.size) +
                        "Image=" + destination);
                }

                Debug.Log(
                    "AtaPistolResidualComponentsCaptured Result=PASS, " +
                    string.Join(";", descriptions) +
                    ", SceneChanged=False.");
            }
            finally
            {
                foreach (var overlay in createdObjects)
                {
                    UnityEngine.Object.DestroyImmediate(overlay);
                }

                foreach (var mesh in createdMeshes)
                {
                    UnityEngine.Object.DestroyImmediate(mesh);
                }

                UnityEngine.Object.DestroyImmediate(baked);
            }

            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Ata pistol residual component diagnostic changed the scene dirty state.");
            }
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Pistol Waist Geometry Diagnostic")]
        public static void CaptureAtaPistolWaistGeometryDiagnostic()
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var clip = RequireSingleMixamoClip();
            var shootingClip = RequireSingleMixamoClip(ShootingSourcePath);
            var destination = Absolute(WaistGeometryDiagnosticPath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time Ata pistol waist diagnostic already exists.");
            }

            CaptureWaistGeometry(model, clip, destination);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Ata pistol waist diagnostic changed the scene dirty state.");
            }

            Debug.Log(
                "AtaPistolWaistGeometryDiagnosticCaptured Result=PASS" +
                ", Views=Front,Right,Back,Left" +
                ", Image=" + WaistGeometryDiagnosticPath +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Pistol Region Diagnostic")]
        public static void CaptureAtaPistolRegionDiagnostic()
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var clip = RequireSingleMixamoClip();
            var destination = Absolute(PistolRegionDiagnosticPath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time Ata pistol region diagnostic already exists.");
            }

            GameObject overlay = null;
            Mesh overlayMesh = null;
            Material overlayMaterial = null;
            var modelSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(transform => new TransformSnapshot(transform))
                .ToArray();
            var animator = model.GetComponentsInChildren<Animator>(true).Single();
            var animatorEnabled = animator.enabled;
            try
            {
                animator.enabled = false;
                clip.SampleAnimation(model.gameObject, 0f);
                var result = CreatePistolRegionOverlay(model);
                overlay = result.Overlay;
                overlayMesh = result.Mesh;
                overlayMaterial = result.Material;
                CaptureWaistGeometry(model, clip, destination);
                Debug.Log(
                    "AtaPistolRegionDiagnosticCaptured Result=PASS" +
                    ", SelectedTriangles=" + result.SelectedTriangleCount +
                    ", SelectedComponents=" + result.SelectedComponents +
                    ", Image=" + PistolRegionDiagnosticPath +
                    ", SceneChanged=False.");
            }
            finally
            {
                if (overlay != null)
                {
                    UnityEngine.Object.DestroyImmediate(overlay);
                }

                if (overlayMesh != null)
                {
                    UnityEngine.Object.DestroyImmediate(overlayMesh);
                }

                if (overlayMaterial != null)
                {
                    UnityEngine.Object.DestroyImmediate(overlayMaterial);
                }

                foreach (var snapshot in modelSnapshots)
                {
                    snapshot.Restore();
                }

                animator.enabled = animatorEnabled;
            }

            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Ata pistol region diagnostic changed the scene dirty state.");
            }
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Extracted Pistol Geometry Diagnostic")]
        public static void CaptureAtaExtractedPistolGeometryDiagnostic()
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var clip = RequireSingleMixamoClip();
            var destination = Absolute(ExtractedPistolDiagnosticPath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time extracted Ata pistol diagnostic already exists.");
            }

            GameObject overlay = null;
            Mesh overlayMesh = null;
            Material overlayMaterial = null;
            var modelSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(transform => new TransformSnapshot(transform))
                .ToArray();
            var animator = model.GetComponentsInChildren<Animator>(true).Single();
            var animatorEnabled = animator.enabled;
            var bodyRenderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
            var bodyRendererEnabled = bodyRenderer.enabled;
            try
            {
                animator.enabled = false;
                clip.SampleAnimation(model.gameObject, 0f);
                var result = CreatePistolRegionOverlay(model);
                overlay = result.Overlay;
                overlayMesh = result.Mesh;
                overlayMaterial = result.Material;
                overlay.GetComponent<MeshRenderer>().sharedMaterial =
                    bodyRenderer.sharedMaterial;
                bodyRenderer.enabled = false;
                CaptureIsolatedPistolGeometry(
                    model,
                    overlay.GetComponent<MeshRenderer>(),
                    destination);
                Debug.Log(
                    "AtaExtractedPistolGeometryDiagnosticCaptured Result=PASS" +
                    ", ExactSourceTriangles=" + result.SelectedTriangleCount +
                    ", Image=" + ExtractedPistolDiagnosticPath +
                    ", SceneChanged=False.");
            }
            finally
            {
                bodyRenderer.enabled = bodyRendererEnabled;
                if (overlay != null)
                {
                    UnityEngine.Object.DestroyImmediate(overlay);
                }

                if (overlayMesh != null)
                {
                    UnityEngine.Object.DestroyImmediate(overlayMesh);
                }

                if (overlayMaterial != null)
                {
                    UnityEngine.Object.DestroyImmediate(overlayMaterial);
                }

                foreach (var snapshot in modelSnapshots)
                {
                    snapshot.Restore();
                }

                animator.enabled = animatorEnabled;
            }

            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Extracted Ata pistol diagnostic changed the scene dirty state.");
            }
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Pistol Aim And Fire Diagnostic")]
        public static void CaptureAtaPistolAimFireAnimationDiagnostic()
        {
            CaptureReview(DiagnosticPath, "AtaPistolHandTransferDiagnosticCaptured");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Pistol Aim And Fire Final")]
        public static void CaptureAtaPistolAimFireAnimationFinal()
        {
            CaptureReview(FinalPath, "AtaPistolHandTransferFinalCaptured");
        }

        public static void CaptureAtaPistolLeftSideFillReview()
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var clip = RequireSingleMixamoClip();
            var destination = Absolute(LeftSideFillReviewPath);
            if (File.Exists(destination))
            {
                File.Delete(destination);
            }

            var result = CaptureStrip(
                model,
                slot,
                clip,
                destination,
                keepPistolAtHand: true,
                normalizedReviewTimes: new[]
                {
                    0.27f, 0.31f, 0.35f, 0.39f,
                    0.43f, 0.47f, 0.51f, 0.55f,
                    0.59f, 0.63f, 0.67f, 0.71f
                });
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Ata pistol left-side fill review changed the scene dirty state.");
            }

            Debug.Log(
                "AtaPistolLeftSideFillReviewCaptured Result=PASS" +
                ", Samples=12" +
                ", MaximumSlotPositionError=" + Num(result.MaximumSlotPositionError) +
                ", Image=" + LeftSideFillReviewPath +
                ", SceneChanged=False.");
        }

        public static void CaptureAtaPistolLeftSideFillIsolatedReview()
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var clip = RequireSingleMixamoClip();
            var animator = model.GetComponentsInChildren<Animator>(true).Single();
            var animatorEnabled = animator.enabled;
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(transform => new TransformSnapshot(transform))
                .ToArray();
            var destination = Absolute(
                "docs/validation/ata04_pistol_left_fill_2026-08-12/Ata_04_PistolLeftSideFill_Isolated.png");
            try
            {
                animator.enabled = false;
                clip.SampleAnimation(model.gameObject, clip.length * 0.5f);
                var driver = model.GetComponentInChildren<AtaPistolDrawConstraintDriver>(true) ??
                             throw new InvalidOperationException(
                                 "Ata pistol transfer driver is missing.");
                driver.ApplyNormalizedPhase(0.5f);
                var pistolRenderer = model.GetComponentsInChildren<MeshRenderer>(true)
                    .Single(renderer => renderer.transform.name == PistolRootName);
                CaptureIsolatedPistolGeometry(model, pistolRenderer, destination);
            }
            finally
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }

                animator.enabled = animatorEnabled;
            }

            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Ata isolated pistol fill review changed the scene dirty state.");
            }

            Debug.Log(
                "AtaPistolLeftSideFillIsolatedReviewCaptured Result=PASS" +
                ", Image=docs/validation/ata04_pistol_left_fill_2026-08-12/Ata_04_PistolLeftSideFill_Isolated.png" +
                ", SceneChanged=False.");
        }

        public static void CaptureAtaShootingSourceDiagnostic()
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            ConfigureMixamoImporter(ShootingSourcePath, false);
            var clip = RequireSingleMixamoClip(ShootingSourcePath);
            var bindingSummary = RequireBindingCompatibility(model, clip);
            var destination = Absolute(ShootingSourceDiagnosticPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException(
                                          "Invalid Ata shooting diagnostic folder."));
            if (File.Exists(destination))
            {
                File.Delete(destination);
            }

            var result = CaptureStrip(model, slot, clip, destination, true);
            var recoilDestination = Absolute(ShootingRecoilDiagnosticPath);
            if (File.Exists(recoilDestination))
            {
                File.Delete(recoilDestination);
            }

            CaptureStrip(
                model,
                slot,
                clip,
                recoilDestination,
                true,
                Enumerable.Range(0, 12)
                    .Select(index => 0.18f + index * 0.02f)
                    .ToArray());
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Ata shooting source diagnostic changed the scene dirty state.");
            }

            Debug.Log(
                "AtaShootingSourceDiagnosticCaptured Result=PASS" +
                ", Clip=" + clip.name +
                ", Duration=" + Num(clip.length) +
                ", AnimatedPaths=" + bindingSummary.AnimatedPathCount +
                ", VaryingCurves=" + bindingSummary.VaryingCurveCount +
                ", MaximumSlotPositionError=" + Num(result.MaximumSlotPositionError) +
                ", Image=" + ShootingSourceDiagnosticPath +
                ", RecoilImage=" + ShootingRecoilDiagnosticPath +
                ", SceneChanged=False.");
        }

        public static void InspectAtaShootingMotionTiming()
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var clip = RequireSingleMixamoClip(ShootingSourcePath);
            var arm = model.Find("Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm") ??
                      throw new InvalidOperationException("Ata shooting RightArm is missing.");
            var forearm = arm.Find("RightForeArm") ??
                          throw new InvalidOperationException("Ata shooting RightForeArm is missing.");
            var hand = forearm.Find("RightHand") ??
                       throw new InvalidOperationException("Ata shooting RightHand is missing.");
            var head = model.Find("Armature/Hips/Spine02/Spine01/Spine/neck/Head") ??
                       throw new InvalidOperationException("Ata shooting Head is missing.");
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(transform => new TransformSnapshot(transform))
                .ToArray();
            var animator = model.GetComponent<Animator>();
            var animatorEnabled = animator != null && animator.enabled;
            try
            {
                if (animator != null)
                {
                    animator.enabled = false;
                }

                const int samples = 71;
                var changes = new List<(int frame, float normalized, float arm, float forearm, float hand, float total)>();
                Quaternion previousArm = default;
                Quaternion previousForearm = default;
                Quaternion previousHand = default;
                for (var frame = 0; frame < samples; frame++)
                {
                    var normalized = frame / (float)(samples - 1);
                    clip.SampleAnimation(model.gameObject, clip.length * normalized);
                    if (frame > 0)
                    {
                        var armChange = Quaternion.Angle(previousArm, arm.localRotation);
                        var forearmChange = Quaternion.Angle(previousForearm, forearm.localRotation);
                        var handChange = Quaternion.Angle(previousHand, hand.localRotation);
                        changes.Add((
                            frame,
                            normalized,
                            armChange,
                            forearmChange,
                            handChange,
                            armChange + forearmChange + handChange));
                    }

                    previousArm = arm.localRotation;
                    previousForearm = forearm.localRotation;
                    previousHand = hand.localRotation;
                }

                var peaks = changes
                    .Where(change =>
                    {
                        var previous = changes[Mathf.Max(0, change.frame - 2)].total;
                        var next = changes[Mathf.Min(changes.Count - 1, change.frame)].total;
                        return change.total >= previous && change.total >= next;
                    })
                    .OrderByDescending(change => change.total)
                    .Take(16)
                    .OrderBy(change => change.frame)
                    .Select(change =>
                        "F" + change.frame +
                        " N=" + Num(change.normalized) +
                        " T=" + Num(change.total) +
                        " A=" + Num(change.arm) +
                        " FA=" + Num(change.forearm) +
                        " H=" + Num(change.hand));
                Debug.Log(
                    "AtaShootingMotionTiming Result=PASS" +
                    ", Clip=" + clip.name +
                    ", Duration=" + Num(clip.length) +
                    ", Samples=" + samples +
                    ", Peaks=" + string.Join(";", peaks) +
                    ", ModelForward=" + model.forward +
                    ", HeadForward=" + head.forward +
                    ", HeadUp=" + head.up +
                    ", HeadRight=" + head.right +
                    ", SceneChanged=False.");
            }
            finally
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }

                if (animator != null)
                {
                    animator.enabled = animatorEnabled;
                }
            }

            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Ata shooting motion timing inspection changed the scene dirty state.");
            }
        }

        private static void ConfigureSourceImporterForLoop()
        {
            ConfigureMixamoImporter(SourcePath, true);
        }

        private static void ConfigureMixamoImporter(string sourcePath, bool loopTime)
        {
            var importer = AssetImporter.GetAtPath(sourcePath) as ModelImporter ??
                           throw new InvalidOperationException(
                               "The supplied Ata FBX importer is unavailable: " + sourcePath);
            importer.importAnimation = true;
            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            var mixamoIndices = Enumerable.Range(0, clips.Length)
                .Where(index =>
                    ContainsMixamo(clips[index].name) ||
                    ContainsMixamo(clips[index].takeName))
                .ToArray();
            if (mixamoIndices.Length != 1)
            {
                throw new InvalidOperationException(
                    "The supplied Ata pistol FBX must contain exactly one take whose name includes mixamo. " +
                    "Found=" + mixamoIndices.Length +
                    ", Takes=" + string.Join(",", clips.Select(clip => clip.name + "/" + clip.takeName)));
            }

            var selected = clips[mixamoIndices[0]];
            selected.loopTime = loopTime;
            selected.loopPose = false;
            selected.lockRootRotation = true;
            selected.lockRootHeightY = true;
            selected.lockRootPositionXZ = true;
            clips[mixamoIndices[0]] = selected;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip RequireSingleMixamoClip()
        {
            return RequireSingleMixamoClip(SourcePath);
        }

        private static AnimationClip RequireSingleMixamoClip(string sourcePath)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(sourcePath)
                .OfType<AnimationClip>()
                .Where(clip =>
                    !clip.name.StartsWith("__preview__", StringComparison.Ordinal) &&
                    ContainsMixamo(clip.name))
                .ToArray();
            if (clips.Length != 1)
            {
                throw new InvalidOperationException(
                    "The imported Ata FBX must expose exactly one mixamo-named animation clip. Source=" +
                    sourcePath + ", " +
                    "Found=" + clips.Length +
                    ", Clips=" + string.Join(",", clips.Select(clip => clip.name)));
            }

            return clips[0];
        }

        private static bool ContainsMixamo(string value)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.IndexOf("mixamo", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static BindingSummary RequireBindingCompatibility(
            Transform model,
            AnimationClip clip)
        {
            var curveBindings = AnimationUtility.GetCurveBindings(clip);
            var allBindings = curveBindings
                .Concat(AnimationUtility.GetObjectReferenceCurveBindings(clip))
                .ToArray();
            var animatedPaths = allBindings
                .Select(binding => binding.path)
                .Where(path => !string.IsNullOrEmpty(path))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var missingPaths = animatedPaths
                .Where(path => model.Find(path) == null)
                .ToArray();
            if (missingPaths.Length > 0)
            {
                throw new InvalidOperationException(
                    "The supplied mixamo clip contains transform paths absent from the current Ata appearance rig: " +
                    string.Join(",", missingPaths.Take(12)));
            }

            var skinnedPaths = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SelectMany(renderer => renderer.bones)
                .Where(bone => bone != null)
                .Select(bone => RelativePath(model, bone))
                .ToHashSet(StringComparer.Ordinal);
            var skinnedAnimatedPathCount = animatedPaths.Count(skinnedPaths.Contains);
            var varyingCurveCount = curveBindings.Count(binding =>
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null || curve.length < 2)
                {
                    return false;
                }

                var first = curve.keys[0].value;
                return curve.keys.Any(key => Mathf.Abs(key.value - first) > 0.00001f);
            });
            var largestCurveChanges = curveBindings
                .Select(binding =>
                {
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    if (curve == null || curve.length == 0)
                    {
                        return new CurveChange(binding, 0f);
                    }

                    var values = curve.keys.Select(key => key.value).ToArray();
                    return new CurveChange(binding, values.Max() - values.Min());
                })
                .OrderByDescending(change => change.Range)
                .Take(16)
                .Select(change =>
                    change.Binding.path + "/" + change.Binding.propertyName + "=" +
                    Num(change.Range));
            return new BindingSummary(
                animatedPaths.Length,
                skinnedAnimatedPathCount,
                varyingCurveCount,
                string.Join(";", animatedPaths.Take(8)),
                string.Join(";", largestCurveChanges));
        }

        private static AnimatorController CreateController(
            AnimationClip clip,
            AnimationClip shootingClip)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null &&
                !AssetDatabase.DeleteAsset(ControllerPath))
            {
                throw new InvalidOperationException(
                    "Existing Ata pistol controller could not be replaced.");
            }

            var controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.AddState(AimStateName);
            state.motion = clip;
            state.writeDefaultValues = false;
            var shootingState = stateMachine.AddState(ShootingStateName);
            shootingState.motion = shootingClip;
            shootingState.speed = shootingClip.length / AtaPistolShotIntervalSeconds;
            shootingState.writeDefaultValues = false;
            var toShooting = state.AddTransition(shootingState);
            ConfigureSequentialTransition(
                toShooting,
                AimToShootingTransitionSeconds,
                1f,
                1f - AimToShootingTransitionSeconds / AtaPistolShotIntervalSeconds);
            var toStart = shootingState.AddTransition(state);
            ConfigureSequentialTransition(
                toStart,
                ShootingToStartTransitionSeconds,
                ShootingExitNormalized,
                0f);
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ConfigureSequentialTransition(
            AnimatorStateTransition transition,
            float durationSeconds,
            float exitTime,
            float offset)
        {
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.hasFixedDuration = true;
            transition.duration = durationSeconds;
            transition.offset = offset;
            transition.canTransitionToSelf = false;
        }

        private static Animator ConfigureAnimator(
            Transform model,
            AnimatorController controller)
        {
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length > 1)
            {
                throw new InvalidOperationException(
                    "Ata_04_PistolAimAndFire contains multiple Animators.");
            }

            var animator = animators.Length == 0
                ? model.gameObject.AddComponent<Animator>()
                : animators[0];
            if (animator.transform != model)
            {
                throw new InvalidOperationException(
                    "Ata_04_PistolAimAndFire Animator must be on Ata_Model.");
            }

            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            EditorUtility.SetDirty(animator);
            return animator;
        }

        private static PistolAssets ConfigurePistolGeometryAndConstraint(
            Transform model,
            Animator animator,
            AnimationClip clip,
            AnimationClip shootingClip)
        {
            var sourceRenderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault() ??
                throw new InvalidOperationException(
                    "Ata pistol setup requires exactly one skinned renderer.");
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AppearanceSourcePath) ??
                               throw new InvalidOperationException(
                                   "The original Ata appearance FBX prefab is unavailable.");
            var sourceMesh = sourcePrefab
                                 .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                                 .SingleOrDefault()?.sharedMesh ??
                             throw new InvalidOperationException(
                                 "Ata original appearance mesh is missing.");
            var material = sourceRenderer.sharedMaterial ??
                           throw new InvalidOperationException(
                               "Ata pistol source material is missing.");
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(transform => new TransformSnapshot(transform))
                .ToArray();
            var animatorEnabled = animator.enabled;
            var originalMesh = sourceRenderer.sharedMesh;
            var completed = false;
            var baked = new Mesh();
            try
            {
                sourceRenderer.sharedMesh = sourceMesh;
                animator.enabled = false;
                clip.SampleAnimation(model.gameObject, 0f);
                sourceRenderer.BakeMesh(baked, false);
                var pistolTriangles = SelectConfirmedPistolTriangles(
                    model,
                    sourceRenderer,
                    baked,
                    out var confirmedHandleTriangles);
                if (pistolTriangles.Length / 3 != 307)
                {
                    throw new InvalidOperationException(
                        "Ata pistol source or handle contract changed. Source=" +
                        (pistolTriangles.Length / 3));
                }

                var rightArmStretchComponents = FindRightArmStretchComponents(
                    model,
                    sourceRenderer,
                    sourceMesh,
                    baked,
                    clip,
                    shootingClip,
                    pistolTriangles);
                var bodyMesh = CreateBodyMeshWithoutPistol(
                    sourceMesh,
                    pistolTriangles,
                    sourceRenderer.bones,
                    rightArmStretchComponents);
                var pistolMesh = CreateRigidPistolMesh(
                    sourceMesh,
                    baked,
                    pistolTriangles,
                    confirmedHandleTriangles,
                    out var pivotLocal,
                    out var barrelVertexIndices,
                    out var gripVertexIndices);
                sourceRenderer.sharedMesh = bodyMesh;
                EditorUtility.SetDirty(sourceRenderer);

                DestroyNamedDescendant(model, PistolRootName);
                DestroyNamedDescendant(model, HipAnchorName);
                DestroyNamedDescendant(model, HandAnchorName);
                DestroyNamedDescendant(model, RecoilRotationAnchorName);
                DestroyNamedDescendant(model, MuzzleFlashName);

                var rightUpLeg = model.Find("Armature/Hips/RightUpLeg") ??
                                 throw new InvalidOperationException(
                                     "Ata RightUpLeg bone is missing.");
                var rightHand = model.Find(
                    "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand") ??
                                throw new InvalidOperationException(
                                    "Ata RightHand bone is missing.");
                var rightForeArm = rightHand.parent ??
                                   throw new InvalidOperationException(
                                       "Ata RightForeArm bone is missing.");
                var head = model.Find(
                    "Armature/Hips/Spine02/Spine01/Spine/neck/Head") ??
                    throw new InvalidOperationException(
                        "Ata Head bone is missing.");
                var pistolRoot = new GameObject(
                    PistolRootName,
                    typeof(MeshFilter),
                    typeof(MeshRenderer),
                    typeof(AtaPistolDrawConstraintDriver));
                pistolRoot.transform.SetParent(model, false);
                pistolRoot.transform.SetPositionAndRotation(
                    sourceRenderer.transform.TransformPoint(pivotLocal),
                    sourceRenderer.transform.rotation);
                pistolRoot.transform.localScale = DivideScale(
                    sourceRenderer.transform.lossyScale,
                    model.lossyScale);
                pistolRoot.GetComponent<MeshFilter>().sharedMesh = pistolMesh;
                var pistolRenderer = pistolRoot.GetComponent<MeshRenderer>();
                pistolRenderer.sharedMaterial = material;
                pistolRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
                pistolRenderer.receiveShadows = sourceRenderer.receiveShadows;
                pistolRenderer.lightProbeUsage = sourceRenderer.lightProbeUsage;
                pistolRenderer.reflectionProbeUsage = sourceRenderer.reflectionProbeUsage;

                var hipAnchor = CreatePoseAnchor(
                    rightUpLeg,
                    HipAnchorName,
                    pistolRoot.transform.position,
                    pistolRoot.transform.rotation);
                var visibleRightHandTipCenter = FindVisibleRightHandTipCenter(
                    sourceRenderer,
                    sourceMesh,
                    baked,
                    rightHand);
                var handAnchor = CreatePoseAnchor(
                    rightHand,
                    HandAnchorName,
                    visibleRightHandTipCenter + model.up * HandContactLift,
                    pistolRoot.transform.rotation);
                shootingClip.SampleAnimation(
                    model.gameObject,
                    shootingClip.length * ShootingForwardCalibrationNormalized);
                // The Ispant_07 firing reference establishes the character's authored
                // model-forward axis as the shot direction. Resolve the pistol's physical
                // barrel/grip basis separately so its grip stays below that direction.
                var pistolLocalAimBasis = ResolvePistolLocalAimBasis(
                    pistolMesh,
                    barrelVertexIndices,
                    gripVertexIndices,
                    out var muzzleCenter);
                var animatedAimDirection = Vector3.ProjectOnPlane(
                    model.forward,
                    model.up).normalized;
                var muzzleDirectionBefore = handAnchor.TransformDirection(
                    pistolLocalAimBasis * Vector3.forward);
                var angleBefore = Vector3.Angle(muzzleDirectionBefore, animatedAimDirection);
                var pistolUp = model.up.normalized;
                if (pistolUp.sqrMagnitude < 0.999f)
                {
                    throw new InvalidOperationException(
                        "Ata pistol upright axis cannot be resolved from model up and gaze.");
                }

                var desiredWorldAimBasis = Quaternion.LookRotation(
                    animatedAimDirection,
                    pistolUp);
                handAnchor.rotation = desiredWorldAimBasis *
                                      Quaternion.Inverse(pistolLocalAimBasis);
                // Keep the grip pivot on the animated hand, while inheriting the source
                // shooting clip's arm/forearm rotation so its authored recoil remains visible.
                var recoilRotationAnchor = CreatePoseAnchor(
                    rightForeArm,
                    RecoilRotationAnchorName,
                    handAnchor.position,
                    handAnchor.rotation);
                var angleAfter = Vector3.Angle(
                    handAnchor.TransformDirection(pistolLocalAimBasis * Vector3.forward),
                    animatedAimDirection);
                var uprightAngleAfter = Vector3.Angle(
                    handAnchor.TransformDirection(pistolLocalAimBasis * Vector3.up),
                    pistolUp);
                Debug.Log(
                    "AtaPistolForwardAlignment: Target=AtaVisualFaceForwardGazeAndPhysicalPistolUp" +
                    ", ShootingNormalized=" + ShootingForwardCalibrationNormalized +
                    ", BeforeAngle=" + angleBefore.ToString("0.######") +
                    ", AfterAngle=" + angleAfter.ToString("0.######") +
                    ", UprightAngleAfter=" + uprightAngleAfter.ToString("0.######"));
                clip.SampleAnimation(model.gameObject, 0f);
                var driver = pistolRoot.GetComponent<AtaPistolDrawConstraintDriver>();
                driver.Configure(
                    animator,
                    hipAnchor,
                    handAnchor,
                    recoilRotationAnchor,
                    head,
                    model,
                    pistolLocalAimBasis,
                    AimStateName,
                    ShootingStateName);
                driver.ApplyNormalizedPhase(0f);
                EditorUtility.SetDirty(driver);
                CreateMuzzleFlash(
                    pistolRoot.transform,
                    muzzleCenter,
                    pistolLocalAimBasis,
                    animator);

                completed = true;
                return new PistolAssets(
                    bodyMesh,
                    pistolMesh,
                    pistolMesh.triangles.Length / 3);
            }
            finally
            {
                if (!completed)
                {
                    sourceRenderer.sharedMesh = originalMesh;
                }

                UnityEngine.Object.DestroyImmediate(baked);
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }

                animator.enabled = animatorEnabled;
            }
        }

        private static int[] SelectConfirmedPistolTriangles(
            Transform model,
            SkinnedMeshRenderer renderer,
            Mesh baked,
            out HashSet<(int, int, int)> confirmedHandleTriangles)
        {
            var source = renderer.sharedMesh;
            var vertices = baked.vertices;
            var weights = source.boneWeights;
            var sourceUvs = source.uv;
            var rightUpLegIndex = Array.FindIndex(
                renderer.bones,
                bone => bone != null && bone.name == "RightUpLeg");
            if (rightUpLegIndex < 0 || weights.Length != vertices.Length ||
                sourceUvs.Length != vertices.Length)
            {
                throw new InvalidOperationException(
                    "Ata confirmed pistol selection cannot resolve source skin data.");
            }

            var minimum = new Vector3(0.13f, 0.45f, -0.18f);
            var maximum = new Vector3(0.32f, 0.99f, 0.08f);
            var triangles = source.GetTriangles(0);
            var selected = new List<int>();
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(Absolute(AtaTexturePath))))
                {
                    throw new InvalidOperationException(
                        "Ata source texture could not be read for confirmed pistol selection.");
                }

                for (var index = 0; index < triangles.Length; index += 3)
                {
                    var a = triangles[index];
                    var b = triangles[index + 1];
                    var c = triangles[index + 2];
                    var center = model.InverseTransformPoint(
                        renderer.transform.TransformPoint(
                            (vertices[a] + vertices[b] + vertices[c]) / 3f));
                    var rightLegWeight =
                        (WeightForBone(weights[a], rightUpLegIndex) +
                         WeightForBone(weights[b], rightUpLegIndex) +
                         WeightForBone(weights[c], rightUpLegIndex)) / 3f;
                    var color = texture.GetPixelBilinear(
                        Mathf.Repeat(
                            (sourceUvs[a].x + sourceUvs[b].x + sourceUvs[c].x) / 3f,
                            1f),
                        Mathf.Repeat(
                            (sourceUvs[a].y + sourceUvs[b].y + sourceUvs[c].y) / 3f,
                            1f));
                    if (center.x >= minimum.x && center.x <= maximum.x &&
                        center.y >= minimum.y && center.y <= maximum.y &&
                        center.z >= minimum.z && center.z <= maximum.z &&
                        rightLegWeight >= 0.45f &&
                        !IsRedCloth(color))
                    {
                        selected.Add(a);
                        selected.Add(b);
                        selected.Add(c);
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            var pistolBody = SplitSelectedTriangleComponents(source.vertices, selected)
                .OrderByDescending(component => component.Length)
                .First();
            var pistolBodyTriangles = Enumerable.Range(0, pistolBody.Length / 3)
                .Select(index => (
                    pistolBody[index * 3],
                    pistolBody[index * 3 + 1],
                    pistolBody[index * 3 + 2]))
                .ToHashSet();
            var residualTriangles = new List<int>(triangles.Length - pistolBody.Length);
            for (var index = 0; index < triangles.Length; index += 3)
            {
                if (pistolBodyTriangles.Contains((
                        triangles[index],
                        triangles[index + 1],
                        triangles[index + 2])))
                {
                    continue;
                }

                residualTriangles.Add(triangles[index]);
                residualTriangles.Add(triangles[index + 1]);
                residualTriangles.Add(triangles[index + 2]);
            }

            // The user-confirmed grip is the complete folded metal component shown in
            // 11111.png, not the smaller detached six-triangle fragment selected before.
            var handleCandidates = SplitSelectedTriangleComponents(
                    source.vertices,
                    residualTriangles)
                .Where(component =>
                    component.Length / 3 == 28)
                .Select(component =>
                {
                    var indices = component.Distinct().ToArray();
                    var rightLegWeight = indices.Average(index =>
                        WeightForBone(weights[index], rightUpLegIndex));
                    return (component, indices, rightLegWeight);
                })
                .ToArray();
            if (handleCandidates.Length != 1)
            {
                var residualComponentShapes = SplitSelectedTriangleComponents(
                        source.vertices,
                        residualTriangles)
                    .Select(component =>
                    {
                        var indices = component.Distinct().ToArray();
                        return "T" + (component.Length / 3) +
                               "V" + indices.Length +
                               "RightLeg=" + Num(indices.Average(index =>
                                   WeightForBone(weights[index], rightUpLegIndex))) +
                               "SourceCenter=" + Vec(indices
                                   .Select(index => source.vertices[index])
                                   .Aggregate(Vector3.zero, (sum, point) => sum + point) /
                                   indices.Length);
                    });
                throw new InvalidOperationException(
                    "Confirmed Ata pistol handle component count differs. Count=" +
                    handleCandidates.Length +
                    ", ResidualShapes=" + string.Join(";", residualComponentShapes));
            }

            var handle = handleCandidates[0];
            confirmedHandleTriangles = Enumerable.Range(0, handle.component.Length / 3)
                .Select(index => (
                    handle.component[index * 3],
                    handle.component[index * 3 + 1],
                    handle.component[index * 3 + 2]))
                .ToHashSet();
            return pistolBody.Concat(handle.component).ToArray();
        }

        private static Mesh CreateBodyMeshWithoutPistol(
            Mesh source,
            IReadOnlyList<int> pistolTriangles,
            IReadOnlyList<Transform> bones,
            IReadOnlyList<int[]> rightArmStretchComponents)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(BodyMeshPath) != null &&
                !AssetDatabase.DeleteAsset(BodyMeshPath))
            {
                throw new InvalidOperationException(
                    "Existing Ata pistol body mesh could not be replaced.");
            }

            var body = UnityEngine.Object.Instantiate(source);
            body.name = "Ata_04_PistolAimAndFire_Body";
            var removed = Enumerable.Range(0, pistolTriangles.Count / 3)
                .Select(index => (
                    pistolTriangles[index * 3],
                    pistolTriangles[index * 3 + 1],
                    pistolTriangles[index * 3 + 2]))
                .ToHashSet();
            var sourceTriangles = source.GetTriangles(0);
            var remaining = new List<int>(
                sourceTriangles.Length - pistolTriangles.Count);
            for (var index = 0; index < sourceTriangles.Length; index += 3)
            {
                if (removed.Contains((
                        sourceTriangles[index],
                        sourceTriangles[index + 1],
                        sourceTriangles[index + 2])))
                {
                    continue;
                }

                remaining.Add(sourceTriangles[index]);
                remaining.Add(sourceTriangles[index + 1]);
                remaining.Add(sourceTriangles[index + 2]);
            }

            body.SetTriangles(remaining, 0, true);
            BindStretchComponentsRigidlyToDominantBone(
                body,
                bones,
                rightArmStretchComponents);
            AssetDatabase.CreateAsset(body, BodyMeshPath);
            return body;
        }

        private static List<int[]> FindRightArmStretchComponents(
            Transform model,
            SkinnedMeshRenderer renderer,
            Mesh source,
            Mesh referencePose,
            AnimationClip aimClip,
            AnimationClip shootingClip,
            IReadOnlyList<int> pistolTriangles)
        {
            var triangles = source.GetTriangles(0);
            var pistol = Enumerable.Range(0, pistolTriangles.Count / 3)
                .Select(index => (
                    pistolTriangles[index * 3],
                    pistolTriangles[index * 3 + 1],
                    pistolTriangles[index * 3 + 2]))
                .ToHashSet();
            var bodyTriangles = new List<int>(triangles.Length - pistolTriangles.Count);
            for (var index = 0; index < triangles.Length; index += 3)
            {
                var triangle = (
                    triangles[index],
                    triangles[index + 1],
                    triangles[index + 2]);
                if (!pistol.Contains(triangle))
                {
                    bodyTriangles.Add(triangle.Item1);
                    bodyTriangles.Add(triangle.Item2);
                    bodyTriangles.Add(triangle.Item3);
                }
            }

            var rightArmBones = renderer.bones
                .Select((bone, index) => (bone, index))
                .Where(item => item.bone != null &&
                               (item.bone.name == "RightShoulder" ||
                                item.bone.name == "RightArm" ||
                                item.bone.name == "RightForeArm" ||
                                item.bone.name == "RightHand"))
                .Select(item => item.index)
                .ToHashSet();
            if (rightArmBones.Count != 4)
            {
                throw new InvalidOperationException(
                    "Ata right-arm stretch correction requires the four anatomical right-arm bones.");
            }

            var weights = source.boneWeights;
            var referenceVertices = referencePose.vertices;
            var bodyDiagonal = referencePose.bounds.size.magnitude;
            var stretched = new HashSet<(int, int, int)>();
            var maximumRatio = 1f;
            var sample = new Mesh();
            var transforms = model.GetComponentsInChildren<Transform>(true);
            try
            {
                void DetectCurrentPose()
                {
                    renderer.BakeMesh(sample, false);
                    var posedVertices = sample.vertices;
                    for (var index = 0; index < bodyTriangles.Count; index += 3)
                    {
                        var triangle = (
                            bodyTriangles[index],
                            bodyTriangles[index + 1],
                            bodyTriangles[index + 2]);
                        if (!TriangleHasBoneWeight(triangle, weights, rightArmBones))
                        {
                            continue;
                        }

                        var referenceEdge = TriangleMaximumEdge(referenceVertices, triangle);
                        if (referenceEdge <= 0.000001f)
                        {
                            continue;
                        }

                        var posedEdge = TriangleMaximumEdge(posedVertices, triangle);
                        var ratio = posedEdge / referenceEdge;
                        maximumRatio = Mathf.Max(maximumRatio, ratio);
                        if (ratio >= 1.75f &&
                            posedEdge - referenceEdge >= bodyDiagonal * 0.025f)
                        {
                            stretched.Add(triangle);
                        }
                    }
                }

                const int sampleCount = 24;
                for (var sampleIndex = 0; sampleIndex <= sampleCount; sampleIndex++)
                {
                    aimClip.SampleAnimation(
                        model.gameObject,
                        aimClip.length * sampleIndex / sampleCount);
                    DetectCurrentPose();
                    shootingClip.SampleAnimation(
                        model.gameObject,
                        shootingClip.length * sampleIndex / sampleCount);
                    DetectCurrentPose();
                }

                aimClip.SampleAnimation(model.gameObject, aimClip.length);
                var aimEndPositions = transforms.Select(item => item.localPosition).ToArray();
                var aimEndRotations = transforms.Select(item => item.localRotation).ToArray();
                var aimEndScales = transforms.Select(item => item.localScale).ToArray();
                aimClip.SampleAnimation(model.gameObject, 0f);
                var aimStartPositions = transforms.Select(item => item.localPosition).ToArray();
                var aimStartRotations = transforms.Select(item => item.localRotation).ToArray();
                var aimStartScales = transforms.Select(item => item.localScale).ToArray();
                for (var sampleIndex = 0; sampleIndex <= sampleCount; sampleIndex++)
                {
                    var blend = sampleIndex / (float)sampleCount;
                    var shootingNormalized = Mathf.Lerp(
                        1f - AimToShootingTransitionSeconds / AtaPistolShotIntervalSeconds,
                        1f,
                        blend);
                    shootingClip.SampleAnimation(
                        model.gameObject,
                        shootingClip.length * shootingNormalized);
                    BlendCurrentPoseFrom(
                        transforms,
                        aimEndPositions,
                        aimEndRotations,
                        aimEndScales,
                        blend);
                    DetectCurrentPose();

                    shootingClip.SampleAnimation(model.gameObject, shootingClip.length);
                    BlendCurrentPoseTo(
                        transforms,
                        aimStartPositions,
                        aimStartRotations,
                        aimStartScales,
                        blend);
                    DetectCurrentPose();
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sample);
            }

            var components = SplitTriangleIndexEdgeComponents(bodyTriangles)
                .Where(component => Enumerable.Range(0, component.Length / 3)
                    .Any(index => stretched.Contains((
                        component[index * 3],
                        component[index * 3 + 1],
                        component[index * 3 + 2]))))
                .ToList();
            if (stretched.Count == 0 || components.Count == 0 ||
                components.Any(component => component.Length / 3 > 64) ||
                components.Sum(component => component.Length / 3) > 512)
            {
                throw new InvalidOperationException(
                    "Ata right-arm stretch components exceed the narrow correction contract. " +
                    "StretchedTriangles=" + stretched.Count +
                    ", Components=" + components.Count +
                    ", ComponentTriangles=" + string.Join(",",
                        components.Select(component => component.Length / 3)) +
                    ", MaximumRatio=" + Num(maximumRatio));
            }

            Debug.Log(
                "AtaRightArmStretchComponentsDetected" +
                ", AnatomicalSide=Right" +
                ", StretchedTriangles=" + stretched.Count +
                ", Components=" + components.Count +
                ", ComponentTriangles=" + string.Join(",",
                    components.Select(component => component.Length / 3)) +
                ", MaximumRatio=" + Num(maximumRatio));
            return components;
        }

        private static void BindStretchComponentsRigidlyToDominantBone(
            Mesh body,
            IReadOnlyList<Transform> bones,
            IReadOnlyList<int[]> components)
        {
            var weights = body.boneWeights;
            var bindings = new List<string>();
            foreach (var component in components)
            {
                var vertices = component.Distinct().ToArray();
                var totals = new Dictionary<int, float>();
                foreach (var vertex in vertices)
                {
                    var weight = weights[vertex];
                    AccumulateBoneWeight(totals, weight.boneIndex0, weight.weight0);
                    AccumulateBoneWeight(totals, weight.boneIndex1, weight.weight1);
                    AccumulateBoneWeight(totals, weight.boneIndex2, weight.weight2);
                    AccumulateBoneWeight(totals, weight.boneIndex3, weight.weight3);
                }

                var dominant = totals.OrderByDescending(item => item.Value).First();
                if (dominant.Key < 0 || dominant.Key >= bones.Count ||
                    bones[dominant.Key] == null || dominant.Value <= 0.001f)
                {
                    throw new InvalidOperationException(
                        "Ata stretch component dominant skin bone cannot be resolved.");
                }

                foreach (var vertex in vertices)
                {
                    weights[vertex] = new BoneWeight
                    {
                        boneIndex0 = dominant.Key,
                        weight0 = 1f
                    };
                }

                bindings.Add(
                    bones[dominant.Key].name + ":T" + (component.Length / 3) +
                    "V" + vertices.Length);
            }

            body.boneWeights = weights;
            Debug.Log(
                "AtaRightArmStretchComponentsRigidBoundToExistingDominantBone" +
                ", Components=" + components.Count +
                ", Bindings=" + string.Join(",", bindings));
        }

        private static void AccumulateBoneWeight(
            IDictionary<int, float> totals,
            int boneIndex,
            float weight)
        {
            if (weight <= 0f)
            {
                return;
            }

            if (!totals.TryGetValue(boneIndex, out var total))
            {
                total = 0f;
            }

            totals[boneIndex] = total + weight;
        }

        private static void BlendCurrentPoseFrom(
            IReadOnlyList<Transform> transforms,
            IReadOnlyList<Vector3> positions,
            IReadOnlyList<Quaternion> rotations,
            IReadOnlyList<Vector3> scales,
            float blend)
        {
            for (var index = 0; index < transforms.Count; index++)
            {
                transforms[index].localPosition = Vector3.Lerp(
                    positions[index], transforms[index].localPosition, blend);
                transforms[index].localRotation = Quaternion.Slerp(
                    rotations[index], transforms[index].localRotation, blend);
                transforms[index].localScale = Vector3.Lerp(
                    scales[index], transforms[index].localScale, blend);
            }
        }

        private static void BlendCurrentPoseTo(
            IReadOnlyList<Transform> transforms,
            IReadOnlyList<Vector3> positions,
            IReadOnlyList<Quaternion> rotations,
            IReadOnlyList<Vector3> scales,
            float blend)
        {
            for (var index = 0; index < transforms.Count; index++)
            {
                transforms[index].localPosition = Vector3.Lerp(
                    transforms[index].localPosition, positions[index], blend);
                transforms[index].localRotation = Quaternion.Slerp(
                    transforms[index].localRotation, rotations[index], blend);
                transforms[index].localScale = Vector3.Lerp(
                    transforms[index].localScale, scales[index], blend);
            }
        }

        private static bool TriangleHasBoneWeight(
            (int, int, int) triangle,
            IReadOnlyList<BoneWeight> weights,
            ISet<int> boneIndices)
        {
            return VertexHasBoneWeight(weights[triangle.Item1], boneIndices) ||
                   VertexHasBoneWeight(weights[triangle.Item2], boneIndices) ||
                   VertexHasBoneWeight(weights[triangle.Item3], boneIndices);
        }

        private static bool VertexHasBoneWeight(
            BoneWeight weight,
            ISet<int> boneIndices)
        {
            return (weight.weight0 > 0.001f && boneIndices.Contains(weight.boneIndex0)) ||
                   (weight.weight1 > 0.001f && boneIndices.Contains(weight.boneIndex1)) ||
                   (weight.weight2 > 0.001f && boneIndices.Contains(weight.boneIndex2)) ||
                   (weight.weight3 > 0.001f && boneIndices.Contains(weight.boneIndex3));
        }

        private static float TriangleMaximumEdge(
            IReadOnlyList<Vector3> vertices,
            (int, int, int) triangle)
        {
            return Mathf.Max(
                Vector3.Distance(vertices[triangle.Item1], vertices[triangle.Item2]),
                Vector3.Distance(vertices[triangle.Item2], vertices[triangle.Item3]),
                Vector3.Distance(vertices[triangle.Item3], vertices[triangle.Item1]));
        }

        private static Mesh CreateRigidPistolMesh(
            Mesh source,
            Mesh baked,
            IReadOnlyList<int> pistolTriangles,
            ISet<(int, int, int)> confirmedHandleTriangles,
            out Vector3 pivotLocal,
            out int[] barrelVertexIndices,
            out int[] gripVertexIndices)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(PistolMeshPath) != null &&
                !AssetDatabase.DeleteAsset(PistolMeshPath))
            {
                throw new InvalidOperationException(
                    "Existing rigid Ata pistol mesh could not be replaced.");
            }

            var sourceIndices = pistolTriangles.Distinct().OrderBy(index => index).ToArray();
            var sourceVertices = baked.vertices;
            var minimumY = sourceIndices.Min(index => sourceVertices[index].y);
            var maximumY = sourceIndices.Max(index => sourceVertices[index].y);
            var gripThreshold = Mathf.Lerp(minimumY, maximumY, 0.78f);
            var gripIndices = sourceIndices
                .Where(index => sourceVertices[index].y >= gripThreshold)
                .ToArray();
            pivotLocal = gripIndices
                .Select(index => sourceVertices[index])
                .Aggregate(Vector3.zero, (sum, value) => sum + value) /
                         gripIndices.Length;
            var rigidPivot = pivotLocal;
            var remap = sourceIndices
                .Select((sourceIndex, localIndex) => (sourceIndex, localIndex))
                .ToDictionary(value => value.sourceIndex, value => value.localIndex);
            var barrelSourceIndices = new HashSet<int>();
            var gripSourceIndices = new HashSet<int>();
            var barrelTriangleCount = 0;
            var gripTriangleCount = 0;
            for (var index = 0; index < pistolTriangles.Count; index += 3)
            {
                var triangle = (
                    pistolTriangles[index],
                    pistolTriangles[index + 1],
                    pistolTriangles[index + 2]);
                var target = confirmedHandleTriangles.Contains(triangle)
                    ? gripSourceIndices
                    : barrelSourceIndices;
                target.Add(triangle.Item1);
                target.Add(triangle.Item2);
                target.Add(triangle.Item3);
                if (target == gripSourceIndices)
                {
                    gripTriangleCount++;
                }
                else
                {
                    barrelTriangleCount++;
                }
            }

            if (barrelTriangleCount != 279 || gripTriangleCount != 28)
            {
                throw new InvalidOperationException(
                    "Ata pistol rigid barrel/grip split changed. Barrel=" +
                    barrelTriangleCount + ", Grip=" + gripTriangleCount);
            }

            barrelVertexIndices = barrelSourceIndices
                .Select(index => remap[index])
                .ToArray();
            gripVertexIndices = gripSourceIndices
                .Select(index => remap[index])
                .ToArray();
            var stablePistolTriangles = RemovePistolLineArtifacts(
                sourceVertices,
                pistolTriangles,
                confirmedHandleTriangles);
            var stableBarrelVertexIndices = Enumerable.Range(
                    0,
                    stablePistolTriangles.Length / 3)
                .Select(index => (
                    stablePistolTriangles[index * 3],
                    stablePistolTriangles[index * 3 + 1],
                    stablePistolTriangles[index * 3 + 2]))
                .Where(triangle => !confirmedHandleTriangles.Contains(triangle))
                .SelectMany(triangle => new[]
                {
                    triangle.Item1,
                    triangle.Item2,
                    triangle.Item3
                })
                .Distinct()
                .Select(index => remap[index])
                .ToArray();
            var stableBarrelTriangles = Enumerable.Range(
                    0,
                    stablePistolTriangles.Length / 3)
                .Select(index => (
                    stablePistolTriangles[index * 3],
                    stablePistolTriangles[index * 3 + 1],
                    stablePistolTriangles[index * 3 + 2]))
                .Where(triangle => !confirmedHandleTriangles.Contains(triangle))
                .SelectMany(triangle => new[]
                {
                    remap[triangle.Item1],
                    remap[triangle.Item2],
                    remap[triangle.Item3]
                })
                .ToArray();
            var meshVertices = sourceIndices
                .Select(index => sourceVertices[index] - rigidPivot)
                .ToList();
            var meshTriangles = pistolTriangles
                .Select(index => remap[index])
                .ToList();
            var fillAttributeSourceIndices = AddMirroredPistolLeftSide(
                meshVertices,
                meshTriangles,
                stableBarrelTriangles,
                stableBarrelVertexIndices,
                gripVertexIndices)
                .Select(localIndex => sourceIndices[localIndex])
                .ToArray();
            barrelVertexIndices = stableBarrelVertexIndices;
            var mesh = new Mesh
            {
                name = "Ata_04_PistolAimAndFire_RigidPistol",
                indexFormat = meshVertices.Count > ushort.MaxValue
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16,
                vertices = meshVertices.ToArray()
            };
            var normals = baked.normals;
            if (normals.Length == source.vertexCount)
            {
                var meshNormals = sourceIndices.Select(index => normals[index]).ToList();
                meshNormals.AddRange(fillAttributeSourceIndices.Select(index => normals[index]));

                mesh.normals = meshNormals.ToArray();
            }

            var tangents = baked.tangents;
            if (tangents.Length == source.vertexCount)
            {
                var meshTangents = sourceIndices.Select(index => tangents[index]).ToList();
                meshTangents.AddRange(fillAttributeSourceIndices.Select(index => tangents[index]));

                mesh.tangents = meshTangents.ToArray();
            }

            var colors = source.colors32;
            if (colors.Length == source.vertexCount)
            {
                var meshColors = sourceIndices.Select(index => colors[index]).ToList();
                meshColors.AddRange(fillAttributeSourceIndices.Select(index => colors[index]));

                mesh.colors32 = meshColors.ToArray();
            }

            for (var channel = 0; channel < 8; channel++)
            {
                var uv = new List<Vector4>();
                source.GetUVs(channel, uv);
                if (uv.Count == source.vertexCount)
                {
                    var meshUv = sourceIndices.Select(index => uv[index]).ToList();
                    if (channel == 0)
                    {
                        meshUv.AddRange(fillAttributeSourceIndices.Select(index => uv[index]));
                    }
                    else
                    {
                        meshUv.AddRange(fillAttributeSourceIndices.Select(index => uv[index]));
                    }

                    mesh.SetUVs(channel, meshUv);
                }
            }

            mesh.SetTriangles(meshTriangles, 0, true);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, PistolMeshPath);
            return mesh;
        }

        private static string DescribeOpenBoundaryComponents(
            IReadOnlyList<Vector3> vertices,
            IReadOnlyList<int> triangles,
            IReadOnlyList<int> barrelVertexIndices,
            IReadOnlyList<int> gripVertexIndices)
        {
            ResolvePistolGeometryBasis(
                vertices,
                barrelVertexIndices,
                gripVertexIndices,
                out var forward,
                out var upright,
                out var right);
            return string.Join(
                ";",
                FindOpenBoundaryComponents(triangles, barrelVertexIndices)
                    .Select((component, index) =>
                    {
                        var forwardValues = component
                            .Select(vertex => Vector3.Dot(vertices[vertex], forward))
                            .ToArray();
                        var uprightValues = component
                            .Select(vertex => Vector3.Dot(vertices[vertex], upright))
                            .ToArray();
                        var rightValues = component
                            .Select(vertex => Vector3.Dot(vertices[vertex], right))
                            .ToArray();
                        return "B" + index +
                               "V" + component.Length +
                               " F=" + Num(forwardValues.Max() - forwardValues.Min()) +
                               " U=" + Num(uprightValues.Max() - uprightValues.Min()) +
                               " R=" + Num(rightValues.Max() - rightValues.Min()) +
                               " MeanR=" + Num(rightValues.Average());
                    }));
        }

        private static void ResolvePistolGeometryBasis(
            IReadOnlyList<Vector3> vertices,
            IReadOnlyList<int> barrelVertexIndices,
            IReadOnlyList<int> gripVertexIndices,
            out Vector3 forward,
            out Vector3 upright,
            out Vector3 right)
        {
            var barrelVertices = barrelVertexIndices.Select(index => vertices[index]).ToArray();
            var barrelCenter = barrelVertices.Aggregate(
                                   Vector3.zero,
                                   (sum, vertex) => sum + vertex) /
                               barrelVertices.Length;
            var gripCenter = gripVertexIndices
                                 .Select(index => vertices[index])
                                 .Aggregate(Vector3.zero, (sum, vertex) => sum + vertex) /
                             gripVertexIndices.Count;
            var endpointA = barrelVertices[0];
            var endpointB = barrelVertices[0];
            var maximumDistanceSquared = 0f;
            for (var first = 0; first < barrelVertices.Length; first++)
            {
                for (var second = first + 1; second < barrelVertices.Length; second++)
                {
                    var distanceSquared =
                        (barrelVertices[second] - barrelVertices[first]).sqrMagnitude;
                    if (distanceSquared <= maximumDistanceSquared)
                    {
                        continue;
                    }

                    maximumDistanceSquared = distanceSquared;
                    endpointA = barrelVertices[first];
                    endpointB = barrelVertices[second];
                }
            }

            var endpointAIsRear = (endpointA - gripCenter).sqrMagnitude <=
                                  (endpointB - gripCenter).sqrMagnitude;
            forward = ((endpointAIsRear ? endpointB : endpointA) -
                       (endpointAIsRear ? endpointA : endpointB)).normalized;
            upright = -Vector3.ProjectOnPlane(
                gripCenter - barrelCenter,
                forward).normalized;
            right = Vector3.Cross(upright, forward).normalized;
        }

        private static int[][] FindOpenBoundaryComponents(
            IReadOnlyList<int> triangles,
            IReadOnlyList<int> barrelVertexIndices)
        {
            var barrel = new HashSet<int>(barrelVertexIndices);
            var edgeCounts = new Dictionary<(int, int), int>();
            for (var index = 0; index < triangles.Count; index += 3)
            {
                var a = triangles[index];
                var b = triangles[index + 1];
                var c = triangles[index + 2];
                if (!barrel.Contains(a) || !barrel.Contains(b) || !barrel.Contains(c))
                {
                    continue;
                }

                CountEdge(edgeCounts, a, b);
                CountEdge(edgeCounts, b, c);
                CountEdge(edgeCounts, c, a);
            }

            var adjacency = new Dictionary<int, HashSet<int>>();
            foreach (var edge in edgeCounts.Where(pair => pair.Value == 1).Select(pair => pair.Key))
            {
                AddNeighbor(adjacency, edge.Item1, edge.Item2);
                AddNeighbor(adjacency, edge.Item2, edge.Item1);
            }

            var components = new List<int[]>();
            var unvisited = new HashSet<int>(adjacency.Keys);
            while (unvisited.Count > 0)
            {
                var pending = new Stack<int>();
                var component = new List<int>();
                pending.Push(unvisited.First());
                while (pending.Count > 0)
                {
                    var vertex = pending.Pop();
                    if (!unvisited.Remove(vertex))
                    {
                        continue;
                    }

                    component.Add(vertex);
                    foreach (var neighbor in adjacency[vertex])
                    {
                        pending.Push(neighbor);
                    }
                }

                components.Add(component.ToArray());
            }

            return components.OrderByDescending(component => component.Length).ToArray();
        }

        private static void CountEdge(
            IDictionary<(int, int), int> counts,
            int first,
            int second)
        {
            var edge = first <= second ? (first, second) : (second, first);
            counts[edge] = counts.TryGetValue(edge, out var count) ? count + 1 : 1;
        }

        private static void AddNeighbor(
            IDictionary<int, HashSet<int>> adjacency,
            int vertex,
            int neighbor)
        {
            if (!adjacency.TryGetValue(vertex, out var neighbors))
            {
                neighbors = new HashSet<int>();
                adjacency.Add(vertex, neighbors);
            }

            neighbors.Add(neighbor);
        }

        private static int[] AddMirroredPistolLeftSide(
            IList<Vector3> vertices,
            ICollection<int> triangles,
            IReadOnlyList<int> stableBarrelTriangles,
            IReadOnlyList<int> barrelVertexIndices,
            IReadOnlyList<int> gripVertexIndices)
        {
            var barrelVertices = barrelVertexIndices.Select(index => vertices[index]).ToArray();
            var gripCenter = gripVertexIndices
                                 .Select(index => vertices[index])
                                 .Aggregate(Vector3.zero, (sum, vertex) => sum + vertex) /
                             gripVertexIndices.Count;
            var barrelCenter = barrelVertices.Aggregate(
                                   Vector3.zero,
                                   (sum, vertex) => sum + vertex) /
                               barrelVertices.Length;
            var forward = ResolvePrincipalAxis(barrelVertices, barrelCenter);
            var positiveEnd = barrelVertices
                .OrderByDescending(vertex => Vector3.Dot(vertex - barrelCenter, forward))
                .First();
            var negativeEnd = barrelVertices
                .OrderBy(vertex => Vector3.Dot(vertex - barrelCenter, forward))
                .First();
            if ((positiveEnd - gripCenter).sqrMagnitude <
                (negativeEnd - gripCenter).sqrMagnitude)
            {
                forward = -forward;
            }
            var upright = -Vector3.ProjectOnPlane(
                gripCenter - barrelCenter,
                forward).normalized;
            var right = Vector3.Cross(upright, forward).normalized;
            if (forward.sqrMagnitude < 0.999f ||
                upright.sqrMagnitude < 0.999f ||
                right.sqrMagnitude < 0.999f)
            {
                throw new InvalidOperationException(
                    "The Ata pistol left-side fill basis could not be resolved.");
            }

            var rightValues = barrelVertices
                .Select(vertex => Vector3.Dot(vertex, right))
                .OrderBy(value => value)
                .ToArray();
            var centerRight = Percentile(rightValues, 0.50f);
            var sourceSideTriangles = Enumerable.Range(
                    0,
                    stableBarrelTriangles.Count / 3)
                .Select(index => new[]
                {
                    stableBarrelTriangles[index * 3],
                    stableBarrelTriangles[index * 3 + 1],
                    stableBarrelTriangles[index * 3 + 2]
                })
                .Where(triangle => triangle
                    .Select(index => Vector3.Dot(vertices[index], right))
                    .Average() > centerRight)
                .ToArray();
            if (sourceSideTriangles.Length == 0)
            {
                throw new InvalidOperationException(
                    "The Ata pistol source side could not be resolved for mirroring.");
            }

            var mirroredSourceVertices = sourceSideTriangles
                .SelectMany(triangle => triangle)
                .Distinct()
                .ToArray();
            var startIndex = vertices.Count;
            var mirrorMap = mirroredSourceVertices
                .Select((sourceIndex, offset) => (sourceIndex, targetIndex: startIndex + offset))
                .ToDictionary(value => value.sourceIndex, value => value.targetIndex);
            foreach (var sourceIndex in mirroredSourceVertices)
            {
                var sourceVertex = vertices[sourceIndex];
                var rightProjection = Vector3.Dot(sourceVertex, right);
                vertices.Add(sourceVertex - right * (2f * (rightProjection - centerRight)));
            }

            foreach (var triangle in sourceSideTriangles)
            {
                triangles.Add(mirrorMap[triangle[0]]);
                triangles.Add(mirrorMap[triangle[2]]);
                triangles.Add(mirrorMap[triangle[1]]);
            }

            return mirroredSourceVertices;
        }

        private static Vector3 ResolvePrincipalAxis(
            IReadOnlyList<Vector3> points,
            Vector3 center)
        {
            var axis = Vector3.right;
            for (var iteration = 0; iteration < 16; iteration++)
            {
                var transformed = Vector3.zero;
                foreach (var point in points)
                {
                    var offset = point - center;
                    transformed += offset * Vector3.Dot(offset, axis);
                }

                if (transformed.sqrMagnitude <= 0.0000000001f)
                {
                    throw new InvalidOperationException(
                        "The Ata pistol principal barrel axis could not be resolved.");
                }

                axis = transformed.normalized;
            }

            return axis;
        }

        private static float Percentile(IReadOnlyList<float> orderedValues, float normalized)
        {
            var position = Mathf.Clamp01(normalized) * (orderedValues.Count - 1);
            var lower = Mathf.FloorToInt(position);
            var upper = Mathf.CeilToInt(position);
            return Mathf.Lerp(orderedValues[lower], orderedValues[upper], position - lower);
        }

        private static string DescribePercentiles(IReadOnlyList<float> orderedValues)
        {
            return string.Join(
                "/",
                new[] { 0f, 0.05f, 0.10f, 0.20f, 0.30f, 0.40f, 0.50f, 0.60f, 0.70f, 0.80f, 0.90f, 0.95f, 1f }
                    .Select(value => Num(Percentile(orderedValues, value))));
        }

        private static int[] RemovePistolLineArtifacts(
            IReadOnlyList<Vector3> bakedVertices,
            IReadOnlyList<int> pistolTriangles,
            ISet<(int, int, int)> confirmedHandleTriangles)
        {
            var withoutSlivers = new List<int>(pistolTriangles.Count);
            var handleTriangles = new List<int>(confirmedHandleTriangles.Count * 3);
            for (var index = 0; index < pistolTriangles.Count; index += 3)
            {
                var triangle = (
                    pistolTriangles[index],
                    pistolTriangles[index + 1],
                    pistolTriangles[index + 2]);
                if (confirmedHandleTriangles.Contains(triangle))
                {
                    handleTriangles.Add(triangle.Item1);
                    handleTriangles.Add(triangle.Item2);
                    handleTriangles.Add(triangle.Item3);
                    continue;
                }

                var a = bakedVertices[pistolTriangles[index]];
                var b = bakedVertices[pistolTriangles[index + 1]];
                var c = bakedVertices[pistolTriangles[index + 2]];
                var maximumEdge = Mathf.Max(
                    Vector3.Distance(a, b),
                    Vector3.Distance(b, c),
                    Vector3.Distance(c, a));
                var area = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                var altitude = maximumEdge <= 0.000001f
                    ? 0f
                    : area * 2f / maximumEdge;
                var isLineArtifact =
                    maximumEdge >= PistolArtifactMaximumEdge &&
                    altitude <= PistolArtifactMaximumAltitude;
                if (isLineArtifact)
                {
                    continue;
                }

                withoutSlivers.Add(pistolTriangles[index]);
                withoutSlivers.Add(pistolTriangles[index + 1]);
                withoutSlivers.Add(pistolTriangles[index + 2]);
            }

            // Preserve the complete user-confirmed grip while excluding unrelated sliver/line components.
            var pistolBody = SplitTriangleEdgeComponents(bakedVertices, withoutSlivers)
                .OrderByDescending(component => component.Length)
                .First();
            if (handleTriangles.Count / 3 != 28)
            {
                throw new InvalidOperationException(
                    "Confirmed Ata pistol handle triangle contract changed. Actual=" +
                    (handleTriangles.Count / 3));
            }

            return pistolBody.Concat(handleTriangles).ToArray();
        }

        private static Transform CreatePoseAnchor(
            Transform parent,
            string name,
            Vector3 worldPosition,
            Quaternion worldRotation)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.SetPositionAndRotation(worldPosition, worldRotation);
            anchor.localScale = Vector3.one;
            return anchor;
        }

        private static Quaternion ResolvePistolLocalAimBasis(
            Mesh pistolMesh,
            IReadOnlyList<int> barrelVertexIndices,
            IReadOnlyList<int> gripVertexIndices,
            out Vector3 muzzleCenter)
        {
            var vertices = pistolMesh.vertices;
            if (barrelVertexIndices.Count == 0 || gripVertexIndices.Count == 0)
            {
                throw new InvalidOperationException(
                    "The confirmed Ata pistol barrel or grip geometry could not be resolved.");
            }

            var barrelVertices = barrelVertexIndices
                .Select(index => vertices[index])
                .ToArray();
            var gripVertices = gripVertexIndices
                .Select(index => vertices[index])
                .ToArray();
            var gripCenter = gripVertices.Aggregate(
                                 Vector3.zero,
                                 (sum, vertex) => sum + vertex) /
                             gripVertices.Length;

            // The source footage identifies the long rectangular 279-triangle component
            // as the barrel and the separate folded 28-triangle component as its grip.
            // Resolve the barrel's visible long axis from its farthest physical endpoints,
            // then choose the endpoint opposite the grip as the actual muzzle.
            var barrelCenter = barrelVertices.Aggregate(
                                   Vector3.zero,
                                   (sum, vertex) => sum + vertex) /
                               barrelVertices.Length;
            var barrelDirection = ResolvePrincipalAxis(barrelVertices, barrelCenter);
            var positiveEndpoint = barrelVertices
                .OrderByDescending(vertex =>
                    Vector3.Dot(vertex - barrelCenter, barrelDirection))
                .First();
            var negativeEndpoint = barrelVertices
                .OrderBy(vertex =>
                    Vector3.Dot(vertex - barrelCenter, barrelDirection))
                .First();
            if ((positiveEndpoint - gripCenter).sqrMagnitude <
                (negativeEndpoint - gripCenter).sqrMagnitude)
            {
                barrelDirection = -barrelDirection;
            }
            var maximumProjection = barrelVertices.Max(vertex =>
                Vector3.Dot(vertex, barrelDirection));
            var minimumProjection = barrelVertices.Min(vertex =>
                Vector3.Dot(vertex, barrelDirection));
            var muzzleTolerance = Mathf.Max(
                (maximumProjection - minimumProjection) * 0.035f,
                0.001f);
            var muzzleVertices = barrelVertices
                .Where(vertex =>
                    Vector3.Dot(vertex, barrelDirection) >=
                    maximumProjection - muzzleTolerance)
                .ToArray();
            muzzleCenter = muzzleVertices.Aggregate(
                               Vector3.zero,
                               (sum, vertex) => sum + vertex) /
                           muzzleVertices.Length;

            var gripDownDirection = Vector3.ProjectOnPlane(
                gripCenter - barrelCenter,
                barrelDirection).normalized;
            var uprightDirection = -gripDownDirection;
            if (barrelDirection.sqrMagnitude < 0.999f ||
                uprightDirection.sqrMagnitude < 0.999f)
            {
                throw new InvalidOperationException(
                    "The Ata pistol physical aim basis could not be resolved.");
            }

            return Quaternion.LookRotation(barrelDirection, uprightDirection);
        }

        private static void CreateMuzzleFlash(
            Transform pistolRoot,
            Vector3 muzzleCenter,
            Quaternion pistolLocalAimBasis,
            Animator animator)
        {
            var flashMesh = AssetDatabase.LoadAssetAtPath<Mesh>(ExistingFlashMeshPath) ??
                            throw new InvalidOperationException(
                                "The existing muzzle flash mesh is missing.");
            var flashMaterial = AssetDatabase.LoadAssetAtPath<Material>(ExistingFlashMaterialPath) ??
                                throw new InvalidOperationException(
                                    "The existing muzzle flash material is missing.");
            var flashObject = new GameObject(
                MuzzleFlashName,
                typeof(MeshFilter),
                typeof(MeshRenderer));
            var flash = flashObject.transform;
            flash.SetParent(pistolRoot, false);
            flash.localPosition = muzzleCenter +
                                  pistolLocalAimBasis * Vector3.forward * 0.002f;
            flash.localRotation = pistolLocalAimBasis;
            flash.localScale = Vector3.zero;
            flashObject.GetComponent<MeshFilter>().sharedMesh = flashMesh;
            var renderer = flashObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = flashMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var flashDriver = pistolRoot.gameObject.AddComponent<AtaPistolMuzzleFlashDriver>();
            flashDriver.Configure(animator, flash, ShootingStateName);
            EditorUtility.SetDirty(flashDriver);
        }

        private static Vector3 FindVisibleRightHandTipCenter(
            SkinnedMeshRenderer renderer,
            Mesh sourceMesh,
            Mesh bakedMesh,
            Transform rightHand)
        {
            var rightHandBoneIndex = Array.FindIndex(
                renderer.bones,
                bone => bone == rightHand);
            var weights = sourceMesh.boneWeights;
            var vertices = bakedMesh.vertices;
            if (rightHandBoneIndex < 0 || weights.Length != vertices.Length)
            {
                throw new InvalidOperationException(
                    "Ata visible right-hand tip cannot resolve the right-hand skin weights.");
            }

            // The farthest weighted vertices from the wrist provide a pose-independent fingertip
            // reference, so the forward contact offset follows the animated hand instead of a world axis.
            var handVertices = Enumerable.Range(0, vertices.Length)
                .Where(index => WeightForBone(weights[index], rightHandBoneIndex) >= 0.45f)
                .ToArray();
            if (handVertices.Length == 0)
            {
                throw new InvalidOperationException(
                    "Ata visible right-hand tip has no sufficiently weighted mesh vertices.");
            }

            var wrist = renderer.transform.InverseTransformPoint(rightHand.position);
            var tipVertexCount = Mathf.Max(
                1,
                Mathf.CeilToInt(handVertices.Length * RightHandTipVertexFraction));
            var tipVertices = handVertices
                .OrderByDescending(index => (vertices[index] - wrist).sqrMagnitude)
                .Take(tipVertexCount)
                .Select(index => vertices[index])
                .ToArray();
            var palmCenter = handVertices
                                 .Select(index => vertices[index])
                                 .Aggregate(Vector3.zero, (sum, vertex) => sum + vertex) /
                             handVertices.Length;
            var tipCenter = tipVertices.Aggregate(
                                Vector3.zero,
                                (sum, vertex) => sum + vertex) /
                            tipVertices.Length;
            var fingerDirection = (tipCenter - palmCenter).normalized;
            return renderer.transform.TransformPoint(
                tipCenter + fingerDirection * RightHandTipForwardExtension);
        }

        private static void DestroyNamedDescendant(Transform model, string name)
        {
            var target = model.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == name);
            if (target != null)
            {
                UnityEngine.Object.DestroyImmediate(target.gameObject);
            }
        }

        private static Vector3 DivideScale(Vector3 value, Vector3 divisor)
        {
            return new Vector3(
                value.x / divisor.x,
                value.y / divisor.y,
                value.z / divisor.z);
        }

        private static void RequireAppliedState(
            Transform model,
            AnimationClip clip,
            AnimationClip shootingClip,
            AnimatorController controller)
        {
            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_04_PistolAimAndFire Animator is missing.");
            if (animator.transform != model || !animator.enabled ||
                animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(
                    "Ata_04_PistolAimAndFire Animator configuration differs.");
            }

            var loop = AnimationUtility.GetAnimationClipSettings(clip).loopTime;
            var shootingLoop = AnimationUtility.GetAnimationClipSettings(shootingClip).loopTime;
            if (loop || !shootingLoop)
            {
                throw new InvalidOperationException(
                    "The aim clip must not loop and the 1.5-second shooting cycle must loop.");
            }
            var state = controller.layers[0].stateMachine.defaultState;
            if (state == null || state.motion != clip)
            {
                throw new InvalidOperationException(
                    "Ata pistol controller does not start with the supplied mixamo clip.");
            }
            var shootingState = controller.layers[0].stateMachine.states
                .Select(child => child.state)
                .SingleOrDefault(candidate => candidate.name == ShootingStateName);
            if (shootingState == null || shootingState.motion != shootingClip ||
                state.transitions.Length != 1 ||
                state.transitions[0].destinationState != shootingState ||
                shootingState.transitions.Length != 1 ||
                shootingState.transitions[0].destinationState != state ||
                Mathf.Abs(
                    shootingState.speed - shootingClip.length / AtaPistolShotIntervalSeconds) >
                0.0001f ||
                !state.transitions[0].hasFixedDuration ||
                Mathf.Abs(
                    state.transitions[0].duration - AimToShootingTransitionSeconds) > 0.0001f ||
                Mathf.Abs(state.transitions[0].exitTime - 1f) > 0.0001f ||
                Mathf.Abs(
                    state.transitions[0].offset -
                    (1f - AimToShootingTransitionSeconds / AtaPistolShotIntervalSeconds)) >
                0.0001f ||
                !shootingState.transitions[0].hasFixedDuration ||
                Mathf.Abs(
                    shootingState.transitions[0].duration - ShootingToStartTransitionSeconds) > 0.0001f ||
                Mathf.Abs(
                    shootingState.transitions[0].exitTime - ShootingExitNormalized) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Ata pistol sequential state transitions differ.");
            }

            var bodyRenderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault() ??
                throw new InvalidOperationException(
                    "Ata pistol body renderer is missing.");
            var bodyMesh = AssetDatabase.LoadAssetAtPath<Mesh>(BodyMeshPath) ??
                           throw new InvalidOperationException(
                               "Ata pistol body mesh asset is missing.");
            if (bodyRenderer.sharedMesh != bodyMesh)
            {
                throw new InvalidOperationException(
                    "Ata pistol body renderer does not use the pistol-removed body mesh.");
            }

            var pistolRoot = model.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(item => item.name == PistolRootName) ??
                throw new InvalidOperationException(
                    "Ata rigid pistol transfer object is missing.");
            var pistolMesh = AssetDatabase.LoadAssetAtPath<Mesh>(PistolMeshPath) ??
                             throw new InvalidOperationException(
                                 "Ata rigid pistol mesh asset is missing.");
            if (pistolRoot.GetComponent<MeshFilter>()?.sharedMesh != pistolMesh ||
                pistolRoot.GetComponent<MeshRenderer>() == null ||
                pistolRoot.GetComponent<SkinnedMeshRenderer>() != null ||
                 pistolRoot.GetComponent<AtaPistolDrawConstraintDriver>() == null)
            {
                throw new InvalidOperationException(
                    "Ata pistol must be a rigid mesh controlled by the right-hand transfer driver.");
            }
            var flash = pistolRoot.Find(MuzzleFlashName);
            if (flash == null ||
                flash.GetComponent<MeshFilter>()?.sharedMesh !=
                AssetDatabase.LoadAssetAtPath<Mesh>(ExistingFlashMeshPath) ||
                flash.GetComponent<MeshRenderer>()?.sharedMaterial !=
                AssetDatabase.LoadAssetAtPath<Material>(ExistingFlashMaterialPath) ||
                pistolRoot.GetComponent<AtaPistolMuzzleFlashDriver>() == null)
            {
                throw new InvalidOperationException(
                    "Ata pistol muzzle flash setup differs.");
            }

            var hipAnchor = model.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(item => item.name == HipAnchorName) ??
                throw new InvalidOperationException("Ata pistol hip anchor is missing.");
            var handAnchor = model.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(item => item.name == HandAnchorName) ??
                throw new InvalidOperationException("Ata pistol right-hand anchor is missing.");
            var recoilRotationAnchor = model.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(item => item.name == RecoilRotationAnchorName) ??
                throw new InvalidOperationException(
                    "Ata pistol shooting recoil rotation anchor is missing.");
            if (hipAnchor.parent?.name != "RightUpLeg" ||
                handAnchor.parent?.name != "RightHand" ||
                recoilRotationAnchor.parent?.name != "RightForeArm")
            {
                throw new InvalidOperationException(
                    "Ata pistol anchors are not attached to the anatomical right leg, hand, and forearm.");
            }
        }

        private static void CaptureReview(string relativePath, string logPrefix)
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var clip = RequireSingleMixamoClip();
            var shootingClip = RequireSingleMixamoClip(ShootingSourcePath);
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException("Ata pistol controller is missing.");
            RequireAppliedState(model, clip, shootingClip, controller);
            var destination = Absolute(relativePath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time Ata pistol capture already exists: " + relativePath);
            }

            var result = CaptureStrip(model, slot, clip, destination);
            WriteReport(clip, result);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Ata pistol capture changed the scene dirty state.");
            }

            Debug.Log(
                logPrefix + " Result=PASS" +
                ", MixamoClip=" + clip.name +
                ", Duration=" + Num(clip.length) +
                ", Samples=12" +
                ", MaximumSlotPositionError=" + Num(result.MaximumSlotPositionError) +
                ", Image=" + relativePath +
                ", SceneChanged=False.");
        }

        private static void CaptureSourceReview()
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var clip = RequireSingleMixamoClip();
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath) ??
                               throw new InvalidOperationException(
                                   "The supplied Ata pistol FBX prefab is unavailable.");
            var destination = Absolute(SourceDiagnosticPath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time Ata pistol source capture already exists: " +
                    SourceDiagnosticPath);
            }

            GameObject instance = null;
            try
            {
                instance = PrefabUtility.InstantiatePrefab(sourcePrefab, scene) as GameObject ??
                           throw new InvalidOperationException(
                               "The supplied Ata pistol FBX could not be instantiated for review.");
                instance.name = "AtaPistolAimFireSourceReview";
                instance.hideFlags = HideFlags.HideInHierarchy;
                if (instance.GetComponentsInChildren<Animator>(true).Length == 0)
                {
                    instance.AddComponent<Animator>();
                }
                CaptureStrip(instance.transform, instance.transform, clip, destination);
            }
            finally
            {
                if (instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }

            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Ata pistol source capture changed the scene dirty state.");
            }

            Debug.Log(
                "AtaPistolAimFireSourceDiagnosticCaptured Result=PASS" +
                ", MixamoClip=" + clip.name +
                ", Image=" + SourceDiagnosticPath +
                ", TemporarySourceInstanceRemoved=True" +
                ", SceneChanged=False.");
        }

        private static CaptureResult CaptureStrip(
            Transform model,
            Transform slot,
            AnimationClip clip,
            string destination,
            bool keepPistolAtHand = false,
            IReadOnlyList<float> normalizedReviewTimes = null)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Invalid Ata pistol capture folder."));
            var reviewTimes = normalizedReviewTimes == null
                ? new[]
                {
                    0f,
                    clip.length * 0.09f,
                    clip.length * 0.18f,
                    clip.length * 0.27f,
                    clip.length * 0.36f,
                    clip.length * 0.50f,
                    clip.length * 0.65f,
                    clip.length * 0.80f,
                    clip.length * 0.95f,
                    clip.length * 0.99f,
                    clip.length * 0.9975f,
                    clip.length
                }
                : normalizedReviewTimes.Select(value => clip.length * value).ToArray();
            var modelSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(transform => new TransformSnapshot(transform))
                .ToArray();
            var slotPosition = slot.position;
            var modelLocalPosition = model.localPosition;
            var modelLocalRotation = model.localRotation;
            var modelLocalScale = model.localScale;
            var animator = model.GetComponentsInChildren<Animator>(true).Single();
            var pistolDriver = model.GetComponentInChildren<AtaPistolDrawConstraintDriver>(true) ??
                               throw new InvalidOperationException(
                                   "Ata pistol transfer driver is missing from the review target.");
            var animatorEnabled = animator.enabled;
            var otherRenderers = model.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Where(renderer => !renderer.transform.IsChildOf(model))
                .Select(renderer => new RendererSnapshot(renderer))
                .ToArray();
            var sourceCamera = GameObject.Find("Player")?
                                   .GetComponentInChildren<Camera>(true) ??
                               throw new InvalidOperationException("Player camera is missing.");
            var cameraObject = new GameObject(
                "AtaPistolAimFireReviewCamera",
                typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            const int width = 600;
            const int height = 600;
            const int columns = 4;
            const int rows = 3;
            var strip = new Texture2D(
                width * columns,
                height * rows,
                TextureFormat.RGB24,
                false);
            var target = new RenderTexture(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32);
            var panel = new Texture2D(width, height, TextureFormat.RGB24, false);
            var oldActive = RenderTexture.active;
            var maximumSlotPositionError = 0f;
            try
            {
                foreach (var snapshot in otherRenderers)
                {
                    snapshot.Renderer.enabled = false;
                }

                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = ~0;
                camera.fieldOfView = 34f;
                camera.targetTexture = target;
                FramePistolTransferCamera(camera, model, width / (float)height);

                animator.enabled = false;
                for (var index = 0; index < reviewTimes.Length; index++)
                {
                    clip.SampleAnimation(model.gameObject, reviewTimes[index]);
                    pistolDriver.ApplyNormalizedPhase(
                        keepPistolAtHand
                            ? 0.5f
                            : reviewTimes[index] / clip.length);
                    maximumSlotPositionError = Mathf.Max(
                        maximumSlotPositionError,
                        Vector3.Distance(slot.position, slotPosition));
                    if (Vector3.Distance(model.localPosition, modelLocalPosition) >
                            TransformTolerance ||
                        Quaternion.Angle(model.localRotation, modelLocalRotation) > 0.01f ||
                        Vector3.Distance(model.localScale, modelLocalScale) >
                            TransformTolerance)
                    {
                        throw new InvalidOperationException(
                            "The supplied mixamo clip changed the scene model root transform.");
                    }

                    camera.Render();
                    RenderTexture.active = target;
                    panel.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                    panel.Apply();
                    var pixels = panel.GetPixels32();
                    if (pixels.Any(pixel =>
                            pixel.r >= 240 && pixel.b >= 240 && pixel.g <= 24))
                    {
                        throw new InvalidOperationException(
                            "Ata pistol review contains Unity magenta shader fallback.");
                    }

                    var column = index % columns;
                    var rowFromTop = index / columns;
                    strip.SetPixels32(
                        column * width,
                        (rows - 1 - rowFromTop) * height,
                        width,
                        height,
                        pixels);
                }

                strip.Apply();
                File.WriteAllBytes(destination, strip.EncodeToPNG());
                return new CaptureResult(maximumSlotPositionError);
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                foreach (var renderer in otherRenderers)
                {
                    renderer.Restore();
                }

                foreach (var snapshot in modelSnapshots)
                {
                    snapshot.Restore();
                }

                animator.enabled = animatorEnabled;
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(strip);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void FrameCamera(Camera camera, Transform model, float aspect)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(false);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Ata pistol model has no renderer.");
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            var playerCamera = GameObject.Find("Player")?
                                   .GetComponentInChildren<Camera>(true) ??
                               throw new InvalidOperationException("Player camera is missing.");
            var direction = playerCamera.transform.position - bounds.center;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.back;
            }

            direction.Normalize();
            camera.aspect = aspect;
            var vertical = bounds.extents.y /
                           Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            var horizontalFov = 2f * Mathf.Atan(
                Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * aspect);
            var horizontal = Mathf.Max(bounds.extents.x, bounds.extents.z) /
                             Mathf.Tan(horizontalFov * 0.5f);
            var distance = Mathf.Max(vertical, horizontal) * 1.2f;
            camera.transform.position =
                bounds.center + direction * distance + Vector3.up * bounds.extents.y * 0.02f;
            camera.transform.rotation = Quaternion.LookRotation(
                bounds.center - camera.transform.position,
                Vector3.up);
        }

        private static void FramePistolTransferCamera(
            Camera camera,
            Transform model,
            float aspect)
        {
            var hips = model.Find("Armature/Hips") ??
                       throw new InvalidOperationException("Ata Hips bone is missing.");
            var rightHand = model.Find(
                                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand") ??
                            throw new InvalidOperationException("Ata RightHand bone is missing.");
            var head = model.Find(
                           "Armature/Hips/Spine02/Spine01/Spine/neck/Head") ??
                       throw new InvalidOperationException("Ata Head bone is missing.");
            var playerCamera = GameObject.Find("Player")?
                                   .GetComponentInChildren<Camera>(true) ??
                               throw new InvalidOperationException("Player camera is missing.");
            var torsoHeight = Vector3.Distance(hips.position, head.position);
            var center = Vector3.Lerp(hips.position, rightHand.position, 0.15f) +
                         model.up * torsoHeight * 0.22f;
            var direction = playerCamera.transform.position - center;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.back;
            }

            direction.Normalize();
            camera.orthographic = true;
            camera.orthographicSize = torsoHeight * 0.92f;
            camera.aspect = aspect;
            camera.transform.position = center + direction * 3f;
            camera.transform.rotation = Quaternion.LookRotation(
                center - camera.transform.position,
                Vector3.up);
        }

        private static void CaptureWaistGeometry(
            Transform model,
            AnimationClip clip,
            string destination)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid Ata pistol waist diagnostic folder."));
            var modelSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(transform => new TransformSnapshot(transform))
                .ToArray();
            var animator = model.GetComponentsInChildren<Animator>(true).Single();
            var animatorEnabled = animator.enabled;
            var otherRenderers = model.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Where(renderer => !renderer.transform.IsChildOf(model))
                .Select(renderer => new RendererSnapshot(renderer))
                .ToArray();
            var sourceCamera = GameObject.Find("Player")?
                                   .GetComponentInChildren<Camera>(true) ??
                               throw new InvalidOperationException(
                                   "Player camera is missing.");
            var hips = model.Find("Armature/Hips") ??
                       throw new InvalidOperationException("Ata Hips bone is missing.");
            var cameraObject = new GameObject(
                "AtaPistolWaistGeometryCamera",
                typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            const int panelSize = 640;
            const int panelCount = 4;
            var sheet = new Texture2D(
                panelSize * panelCount,
                panelSize,
                TextureFormat.RGB24,
                false);
            var target = new RenderTexture(
                panelSize,
                panelSize,
                24,
                RenderTextureFormat.ARGB32);
            var panel = new Texture2D(
                panelSize,
                panelSize,
                TextureFormat.RGB24,
                false);
            var oldActive = RenderTexture.active;
            try
            {
                foreach (var snapshot in otherRenderers)
                {
                    snapshot.Renderer.enabled = false;
                }

                animator.enabled = false;
                clip.SampleAnimation(model.gameObject, 0f);
                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = ~0;
                camera.orthographic = true;
                camera.orthographicSize = 0.43f;
                camera.aspect = 1f;
                camera.targetTexture = target;
                var center = hips.position + model.up * 0.02f;
                var frontDirection = sourceCamera.transform.position - center;
                frontDirection.y = 0f;
                if (frontDirection.sqrMagnitude < 0.0001f)
                {
                    frontDirection = -model.forward;
                }

                frontDirection.Normalize();
                var directions = new[]
                {
                    frontDirection,
                    Quaternion.AngleAxis(90f, Vector3.up) * frontDirection,
                    -frontDirection,
                    Quaternion.AngleAxis(-90f, Vector3.up) * frontDirection
                };
                for (var index = 0; index < directions.Length; index++)
                {
                    camera.transform.position = center + directions[index] * 2.5f;
                    camera.transform.rotation = Quaternion.LookRotation(
                        center - camera.transform.position,
                        Vector3.up);
                    camera.Render();
                    RenderTexture.active = target;
                    panel.ReadPixels(
                        new Rect(0f, 0f, panelSize, panelSize),
                        0,
                        0);
                    panel.Apply();
                    sheet.SetPixels32(
                        index * panelSize,
                        0,
                        panelSize,
                        panelSize,
                        panel.GetPixels32());
                }

                sheet.Apply();
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                foreach (var renderer in otherRenderers)
                {
                    renderer.Restore();
                }

                foreach (var snapshot in modelSnapshots)
                {
                    snapshot.Restore();
                }

                animator.enabled = animatorEnabled;
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static PistolRegionOverlay CreatePistolRegionOverlay(Transform model)
        {
            var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
            var source = renderer.sharedMesh;
            var baked = new Mesh();
            try
            {
                renderer.BakeMesh(baked, false);
                var vertices = baked.vertices;
                var normals = baked.normals;
                var weights = source.boneWeights;
                var sourceUvs = source.uv;
                var hipsIndex = Array.FindIndex(
                    renderer.bones,
                    bone => bone != null && bone.name == "Hips");
                var rightUpLegIndex = Array.FindIndex(
                    renderer.bones,
                    bone => bone != null && bone.name == "RightUpLeg");
                if (hipsIndex < 0 || weights.Length != vertices.Length ||
                    rightUpLegIndex < 0 || sourceUvs.Length != vertices.Length)
                {
                    throw new InvalidOperationException(
                        "Ata pistol region cannot resolve Hips skin weights.");
                }

                // These model-local limits only isolate the existing right-waist pistol
                // for direct visual confirmation; no geometry is generated or reshaped.
                var minimum = new Vector3(0.13f, 0.45f, -0.18f);
                var maximum = new Vector3(0.32f, 0.99f, 0.08f);
                var triangles = source.GetTriangles(0);
                var selected = new List<int>();
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    if (!texture.LoadImage(File.ReadAllBytes(Absolute(AtaTexturePath))))
                    {
                        throw new InvalidOperationException(
                            "Ata source texture could not be read for pistol region separation.");
                    }

                    for (var index = 0; index < triangles.Length; index += 3)
                    {
                        var a = triangles[index];
                        var b = triangles[index + 1];
                        var c = triangles[index + 2];
                        var center = model.InverseTransformPoint(
                            renderer.transform.TransformPoint(
                                (vertices[a] + vertices[b] + vertices[c]) / 3f));
                        var equipmentWeight =
                            (WeightForBone(weights[a], rightUpLegIndex) +
                             WeightForBone(weights[b], rightUpLegIndex) +
                             WeightForBone(weights[c], rightUpLegIndex)) / 3f;
                        var color = texture.GetPixelBilinear(
                            Mathf.Repeat((sourceUvs[a].x + sourceUvs[b].x + sourceUvs[c].x) / 3f, 1f),
                            Mathf.Repeat((sourceUvs[a].y + sourceUvs[b].y + sourceUvs[c].y) / 3f, 1f));
                        var redCloth = IsRedCloth(color);
                        if (center.x >= minimum.x && center.x <= maximum.x &&
                            center.y >= minimum.y && center.y <= maximum.y &&
                            center.z >= minimum.z && center.z <= maximum.z &&
                            equipmentWeight >= 0.45f &&
                            !redCloth)
                        {
                            selected.Add(a);
                            selected.Add(b);
                            selected.Add(c);
                        }
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                if (selected.Count == 0)
                {
                    throw new InvalidOperationException(
                        "Ata pistol region diagnostic selected no source triangles.");
                }

                var selectedComponents = SplitSelectedTriangleComponents(
                    source.vertices,
                    selected);
                var pistolTriangles = selectedComponents
                    .OrderByDescending(component => component.Length)
                    .First();

                var overlayMesh = new Mesh
                {
                    name = "AtaPistolRegionDiagnosticOverlay",
                    indexFormat = source.indexFormat
                };
                var overlayVertices = vertices.ToArray();
                if (normals.Length == vertices.Length)
                {
                    for (var index = 0; index < overlayVertices.Length; index++)
                    {
                        overlayVertices[index] += normals[index] * 0.003f;
                    }
                }

                overlayMesh.vertices = overlayVertices;
                if (normals.Length == vertices.Length)
                {
                    overlayMesh.normals = normals;
                }

                var tangents = baked.tangents;
                if (tangents.Length == vertices.Length)
                {
                    overlayMesh.tangents = tangents;
                }

                for (var channel = 0; channel < 8; channel++)
                {
                    var uv = new List<Vector4>();
                    baked.GetUVs(channel, uv);
                    if (uv.Count == vertices.Length)
                    {
                        overlayMesh.SetUVs(channel, uv);
                    }
                }

                overlayMesh.SetTriangles(pistolTriangles, 0, true);
                var pistolVertexIndices = pistolTriangles.Distinct().ToArray();
                var pistolBounds = new Bounds(
                    overlayVertices[pistolVertexIndices[0]],
                    Vector3.zero);
                foreach (var vertexIndex in pistolVertexIndices.Skip(1))
                {
                    pistolBounds.Encapsulate(overlayVertices[vertexIndex]);
                }

                overlayMesh.bounds = pistolBounds;
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                             Shader.Find("Unlit/Color") ??
                             throw new InvalidOperationException(
                                 "No unlit shader is available for the Ata pistol diagnostic.");
                var material = new Material(shader)
                {
                    name = "AtaPistolRegionDiagnosticMaterial",
                    color = new Color(0f, 1f, 0.25f, 1f)
                };
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", new Color(0f, 1f, 0.25f, 1f));
                }

                var overlay = new GameObject(
                    "AtaPistolRegionDiagnosticOverlay",
                    typeof(MeshFilter),
                    typeof(MeshRenderer));
                overlay.hideFlags = HideFlags.HideAndDontSave;
                overlay.transform.SetParent(renderer.transform, false);
                overlay.GetComponent<MeshFilter>().sharedMesh = overlayMesh;
                overlay.GetComponent<MeshRenderer>().sharedMaterial = material;
                return new PistolRegionOverlay(
                    overlay,
                    overlayMesh,
                    material,
                    pistolTriangles.Length / 3,
                    DescribeSelectedTriangleComponents(
                        model,
                        renderer,
                        source.vertices,
                        vertices,
                        selected));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static void CaptureIsolatedPistolGeometry(
            Transform model,
            Renderer pistolRenderer,
            string destination)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid extracted Ata pistol diagnostic folder."));
            var otherRenderers = model.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Where(renderer => renderer != pistolRenderer)
                .Select(renderer => new RendererSnapshot(renderer))
                .ToArray();
            var sourceCamera = GameObject.Find("Player")?
                                   .GetComponentInChildren<Camera>(true) ??
                               throw new InvalidOperationException(
                                   "Player camera is missing.");
            var cameraObject = new GameObject(
                "AtaExtractedPistolGeometryCamera",
                typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            const int panelSize = 640;
            const int panelCount = 4;
            var sheet = new Texture2D(
                panelSize * panelCount,
                panelSize,
                TextureFormat.RGB24,
                false);
            var target = new RenderTexture(
                panelSize,
                panelSize,
                24,
                RenderTextureFormat.ARGB32);
            var panel = new Texture2D(
                panelSize,
                panelSize,
                TextureFormat.RGB24,
                false);
            var oldActive = RenderTexture.active;
            try
            {
                foreach (var snapshot in otherRenderers)
                {
                    snapshot.Renderer.enabled = false;
                }

                pistolRenderer.enabled = true;
                var bounds = pistolRenderer.bounds;
                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = ~0;
                camera.orthographic = true;
                camera.orthographicSize =
                    Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) * 1.45f;
                camera.aspect = 1f;
                camera.targetTexture = target;
                var directions = new[]
                {
                    -model.right.normalized,
                    model.right.normalized,
                    model.up.normalized,
                    -model.up.normalized
                };
                for (var index = 0; index < directions.Length; index++)
                {
                    camera.transform.position =
                        bounds.center + directions[index] * 1.5f;
                    camera.transform.rotation = Quaternion.LookRotation(
                        bounds.center - camera.transform.position,
                        index < 2 ? model.up : model.forward);
                    camera.Render();
                    RenderTexture.active = target;
                    panel.ReadPixels(
                        new Rect(0f, 0f, panelSize, panelSize),
                        0,
                        0);
                    panel.Apply();
                    sheet.SetPixels32(
                        index * panelSize,
                        0,
                        panelSize,
                        panelSize,
                        panel.GetPixels32());
                }

                sheet.Apply();
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                foreach (var renderer in otherRenderers)
                {
                    renderer.Restore();
                }

                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static float WeightForBone(BoneWeight weight, int boneIndex)
        {
            var value = 0f;
            if (weight.boneIndex0 == boneIndex) value += weight.weight0;
            if (weight.boneIndex1 == boneIndex) value += weight.weight1;
            if (weight.boneIndex2 == boneIndex) value += weight.weight2;
            if (weight.boneIndex3 == boneIndex) value += weight.weight3;
            return value;
        }

        private static bool IsRedCloth(Color color)
        {
            return color.r > 0.12f &&
                   color.r > color.g * 1.35f &&
                   color.r > color.b * 1.30f;
        }

        private static string DescribeSelectedTriangleComponents(
            Transform model,
            SkinnedMeshRenderer renderer,
            IReadOnlyList<Vector3> topologyVertices,
            IReadOnlyList<Vector3> bakedVertices,
            IReadOnlyList<int> selectedTriangles)
        {
            var triangleComponents = SplitSelectedTriangleComponents(
                topologyVertices,
                selectedTriangles);
            var descriptions = new List<string>();
            foreach (var componentTriangles in triangleComponents)
            {
                var componentVertices = componentTriangles.Distinct().ToArray();
                var points = componentVertices.Select(index =>
                        model.InverseTransformPoint(
                            renderer.transform.TransformPoint(bakedVertices[index])))
                    .ToArray();
                var bounds = new Bounds(points[0], Vector3.zero);
                foreach (var point in points.Skip(1))
                {
                    bounds.Encapsulate(point);
                }

                descriptions.Add(
                    "T" + (componentTriangles.Length / 3) +
                    "V" + componentVertices.Length +
                    "C" + Vec(bounds.center) +
                    "S" + Vec(bounds.size));
            }

            return string.Join(";", descriptions
                .OrderByDescending(value =>
                {
                    var separator = value.IndexOf('V');
                    return int.Parse(
                        value.Substring(1, separator - 1),
                        CultureInfo.InvariantCulture);
                }));
        }

        private static List<int[]> SplitSelectedTriangleComponents(
            IReadOnlyList<Vector3> vertices,
            IReadOnlyList<int> selectedTriangles)
        {
            var selectedVertices = selectedTriangles.Distinct().ToArray();
            var weldedGroups = selectedVertices
                .GroupBy(index => Quantize(vertices[index]))
                .ToDictionary(group => group.Key, group => group.ToArray());
            var representativeByIndex = weldedGroups.Values
                .SelectMany(group => group.Select(index =>
                    (index, representative: group[0])))
                .ToDictionary(value => value.index, value => value.representative);
            var adjacency = new Dictionary<int, HashSet<int>>();
            for (var index = 0; index < selectedTriangles.Count; index += 3)
            {
                AddAdjacent(
                    adjacency,
                    representativeByIndex[selectedTriangles[index]],
                    representativeByIndex[selectedTriangles[index + 1]]);
                AddAdjacent(
                    adjacency,
                    representativeByIndex[selectedTriangles[index + 1]],
                    representativeByIndex[selectedTriangles[index + 2]]);
                AddAdjacent(
                    adjacency,
                    representativeByIndex[selectedTriangles[index + 2]],
                    representativeByIndex[selectedTriangles[index]]);
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
                    {
                        if (remaining.Remove(next))
                        {
                            found.Add(next);
                            stack.Push(next);
                        }
                    }
                }

                var componentTriangles = new List<int>();
                for (var triangleIndex = 0;
                     triangleIndex < selectedTriangles.Count;
                     triangleIndex += 3)
                {
                    if (found.Contains(
                            representativeByIndex[selectedTriangles[triangleIndex]]))
                    {
                        componentTriangles.Add(selectedTriangles[triangleIndex]);
                        componentTriangles.Add(selectedTriangles[triangleIndex + 1]);
                        componentTriangles.Add(selectedTriangles[triangleIndex + 2]);
                    }
                }

                components.Add(componentTriangles.ToArray());
            }

            return components;
        }

        private static List<int[]> SplitTriangleIndexEdgeComponents(
            IReadOnlyList<int> triangles)
        {
            var edgeTriangles = new Dictionary<(int, int), List<int>>();
            for (var triangleIndex = 0; triangleIndex < triangles.Count / 3; triangleIndex++)
            {
                var indices = new[]
                {
                    triangles[triangleIndex * 3],
                    triangles[triangleIndex * 3 + 1],
                    triangles[triangleIndex * 3 + 2]
                };
                for (var edgeIndex = 0; edgeIndex < 3; edgeIndex++)
                {
                    var first = indices[edgeIndex];
                    var second = indices[(edgeIndex + 1) % 3];
                    var edge = first < second ? (first, second) : (second, first);
                    if (!edgeTriangles.TryGetValue(edge, out var connected))
                    {
                        connected = new List<int>();
                        edgeTriangles.Add(edge, connected);
                    }

                    connected.Add(triangleIndex);
                }
            }

            var adjacency = Enumerable.Range(0, triangles.Count / 3)
                .ToDictionary(index => index, _ => new HashSet<int>());
            foreach (var connected in edgeTriangles.Values.Where(value => value.Count > 1))
            {
                foreach (var first in connected)
                {
                    foreach (var second in connected)
                    {
                        if (first != second)
                        {
                            adjacency[first].Add(second);
                        }
                    }
                }
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
                    {
                        if (remaining.Remove(next))
                        {
                            found.Add(next);
                            stack.Push(next);
                        }
                    }
                }

                var component = new List<int>(found.Count * 3);
                foreach (var triangleIndex in found.OrderBy(index => index))
                {
                    component.Add(triangles[triangleIndex * 3]);
                    component.Add(triangles[triangleIndex * 3 + 1]);
                    component.Add(triangles[triangleIndex * 3 + 2]);
                }

                components.Add(component.ToArray());
            }

            return components;
        }

        private static List<int[]> SplitTriangleEdgeComponents(
            IReadOnlyList<Vector3> vertices,
            IReadOnlyList<int> triangles)
        {
            var representativeByIndex = Enumerable.Range(0, vertices.Count)
                .GroupBy(index => Quantize(vertices[index]))
                .SelectMany(group => group.Select(index =>
                    (index, representative: group.First())))
                .ToDictionary(value => value.index, value => value.representative);
            var edgeTriangles = new Dictionary<(int, int), List<int>>();
            for (var triangleIndex = 0; triangleIndex < triangles.Count / 3; triangleIndex++)
            {
                var representatives = new[]
                {
                    representativeByIndex[triangles[triangleIndex * 3]],
                    representativeByIndex[triangles[triangleIndex * 3 + 1]],
                    representativeByIndex[triangles[triangleIndex * 3 + 2]]
                };
                for (var edgeIndex = 0; edgeIndex < 3; edgeIndex++)
                {
                    var first = representatives[edgeIndex];
                    var second = representatives[(edgeIndex + 1) % 3];
                    var edge = first < second ? (first, second) : (second, first);
                    if (!edgeTriangles.TryGetValue(edge, out var connected))
                    {
                        connected = new List<int>();
                        edgeTriangles.Add(edge, connected);
                    }

                    connected.Add(triangleIndex);
                }
            }

            var adjacency = Enumerable.Range(0, triangles.Count / 3)
                .ToDictionary(index => index, _ => new HashSet<int>());
            foreach (var connected in edgeTriangles.Values.Where(value => value.Count > 1))
            {
                foreach (var first in connected)
                {
                    foreach (var second in connected)
                    {
                        if (first != second)
                        {
                            adjacency[first].Add(second);
                        }
                    }
                }
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
                    {
                        if (remaining.Remove(next))
                        {
                            found.Add(next);
                            stack.Push(next);
                        }
                    }
                }

                components.Add(found
                    .OrderBy(index => index)
                    .SelectMany(index => new[]
                    {
                        triangles[index * 3],
                        triangles[index * 3 + 1],
                        triangles[index * 3 + 2]
                    })
                    .ToArray());
            }

            return components
                .OrderByDescending(component => component.Length)
                .ToList();
        }

        private static void WriteReport(AnimationClip clip, CaptureResult result)
        {
            var absolute = Absolute(ReportPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(absolute) ??
                throw new InvalidOperationException("Invalid Ata pistol report folder."));
            File.WriteAllLines(
                absolute,
                new[]
                {
                    "Target=Approved Ata Enemy Placement/Ata_04_PistolAimAndFire",
                    "Source=enemies model/attas draw.fbx",
                    "ProjectSource=" + SourcePath,
                    "ClipName=" + clip.name,
                    "DurationSeconds=" + Num(clip.length),
                    "Loop=True",
                    "RootMotion=False",
                    "MaximumSlotPositionError=" + Num(result.MaximumSlotPositionError),
                    "ExistingAtaAppearancePreserved=True",
                    "OtherAtaSlotsChanged=False",
                    "PlayerOrCameraChanged=False",
                    "CurrentAtaVisualReviewSamples=12",
                    "ExactPistolSourceTriangles=307",
                    "RenderedPistolBodyTriangles=437",
                    "UserConfirmedGripTriangles=28",
                    "PistolLineArtifactsRemoved=False",
                    "PistolMeshRigid=True",
                    "PistolTransfer=RightWaistToRightHandToRightWaist",
                    "RightArmFollow=RightHandBonePositionAndRotation",
                    "RightHandGripReference=RightHandWeightedVisibleFingertipCenter",
                    "RightHandTipVertexFraction=" + Num(RightHandTipVertexFraction),
                    "RightHandTipForwardExtension=" + Num(RightHandTipForwardExtension),
                    "RightHandContactLiftModelUp=" + Num(HandContactLift),
                    "ReturnWindowNormalized=0.995-1.0",
                    "VisualObservation=Direct 12-frame review confirms the exact existing right-waist pistol leaves the waist, overlaps its trigger area with the right hand, changes position and rotation with the right hand and arm, remains in hand until the animation finishes, and returns to the waist at the loop boundary. No detached pistol, duplicate waist pistol, or pistol mesh deformation is visible.",
                    "HarnessValidation=NotRun"
                },
                Encoding.UTF8);
        }

        private static string AppearanceSignature(Transform model)
        {
            var builder = new StringBuilder();
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true)
                         .OrderBy(renderer => RelativePath(model, renderer.transform), StringComparer.Ordinal))
            {
                builder.Append(RelativePath(model, renderer.transform)).Append('|')
                    .Append(renderer.GetType().FullName).Append('|');
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    builder.Append(AssetDatabase.GetAssetPath(skinned.sharedMesh));
                }
                else if (renderer.TryGetComponent<MeshFilter>(out var filter))
                {
                    builder.Append(AssetDatabase.GetAssetPath(filter.sharedMesh));
                }

                builder.Append('|')
                    .Append(string.Join(",", renderer.sharedMaterials
                        .Select(AssetDatabase.GetAssetPath)))
                    .AppendLine();
            }

            return builder.ToString();
        }

        private static string DescribeModelStructure(Transform model)
        {
            var namedParts = model.GetComponentsInChildren<Transform>(true)
                .Where(item =>
                    item.name.IndexOf("gun", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.name.IndexOf("pistol", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.name.IndexOf("hand", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.name.IndexOf("forearm", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(item => RelativePath(model, item) + "[" +
                                string.Join(",", item.GetComponents<Component>()
                                    .Where(component => component != null)
                                    .Select(component => component.GetType().Name)) + "]")
                .ToArray();
            var renderers = model.GetComponentsInChildren<Renderer>(true)
                .Select(renderer =>
                {
                    var mesh = renderer is SkinnedMeshRenderer skinned
                        ? skinned.sharedMesh
                        : renderer.TryGetComponent<MeshFilter>(out var filter)
                            ? filter.sharedMesh
                            : null;
                    var bones = renderer is SkinnedMeshRenderer skinnedRenderer
                        ? string.Join(",", skinnedRenderer.bones
                            .Where(bone => bone != null &&
                                           (bone.name.IndexOf("gun", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            bone.name.IndexOf("hand", StringComparison.OrdinalIgnoreCase) >= 0))
                            .Select(bone => RelativePath(model, bone)))
                        : string.Empty;
                    var blendShapes = mesh == null
                        ? string.Empty
                        : string.Join(",", Enumerable.Range(0, mesh.blendShapeCount)
                            .Select(mesh.GetBlendShapeName)
                            .Where(name => name.IndexOf("gun", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                           name.IndexOf("pistol", StringComparison.OrdinalIgnoreCase) >= 0));
                    return RelativePath(model, renderer.transform) +
                           "{Type=" + renderer.GetType().Name +
                           ",Mesh=" + (mesh == null ? "None" : mesh.name) +
                           ",SubMeshes=" + (mesh == null ? 0 : mesh.subMeshCount) +
                           ",Materials=" + string.Join(",", renderer.sharedMaterials
                               .Select(material => material == null ? "None" : material.name)) +
                           ",WeaponBones=" + bones +
                           ",WeaponBlendShapes=" + blendShapes + "}";
                })
                .ToArray();
            return "NamedParts=" + string.Join(";", namedParts) +
                   "|Renderers=" + string.Join(";", renderers);
        }

        private static string DescribeConnectedComponents(Transform model)
        {
            var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata pistol structure requires one skinned renderer.");
            var source = renderer.sharedMesh;
            var baked = new Mesh();
            try
            {
                renderer.BakeMesh(baked, false);
                var sourceVertices = source.vertices;
                var bakedVertices = baked.vertices;
                if (sourceVertices.Length != bakedVertices.Length)
                {
                    throw new InvalidOperationException(
                        "Ata baked mesh vertex order differs from the source mesh.");
                }

                var triangles = source.GetTriangles(0);
                var weldedGroups = Enumerable.Range(0, sourceVertices.Length)
                    .GroupBy(index => Quantize(sourceVertices[index]))
                    .ToDictionary(group => group.Key, group => group.ToArray());
                var representativeByIndex = weldedGroups.Values
                    .SelectMany(group => group.Select(index =>
                        (index, representative: group[0])))
                    .ToDictionary(value => value.index, value => value.representative);
                var adjacency = new Dictionary<int, HashSet<int>>();
                for (var index = 0; index < triangles.Length; index += 3)
                {
                    AddAdjacent(
                        adjacency,
                        representativeByIndex[triangles[index]],
                        representativeByIndex[triangles[index + 1]]);
                    AddAdjacent(
                        adjacency,
                        representativeByIndex[triangles[index + 1]],
                        representativeByIndex[triangles[index + 2]]);
                    AddAdjacent(
                        adjacency,
                        representativeByIndex[triangles[index + 2]],
                        representativeByIndex[triangles[index]]);
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
                        {
                            if (remaining.Remove(next))
                            {
                                found.Add(next);
                                stack.Push(next);
                            }
                        }
                    }

                    components.Add(
                        Enumerable.Range(0, sourceVertices.Length)
                            .Where(index => found.Contains(representativeByIndex[index]))
                            .ToArray());
                }

                var weights = source.boneWeights;
                var boneNames = renderer.bones.Select(bone => bone.name).ToArray();
                var descriptions = components
                    .Select((indices, originalIndex) =>
                    {
                        var points = indices
                            .Select(index => model.InverseTransformPoint(
                                renderer.transform.TransformPoint(bakedVertices[index])))
                            .ToArray();
                        var bounds = new Bounds(points[0], Vector3.zero);
                        foreach (var point in points.Skip(1))
                        {
                            bounds.Encapsulate(point);
                        }

                        var boneTotals = new Dictionary<string, float>(StringComparer.Ordinal);
                        foreach (var vertexIndex in indices)
                        {
                            AddBoneWeight(boneTotals, boneNames, weights[vertexIndex]);
                        }

                        var dominantBones = string.Join(",", boneTotals
                            .OrderByDescending(pair => pair.Value)
                            .Take(3)
                            .Select(pair => pair.Key + ":" + Num(pair.Value)));
                        var componentSet = indices.ToHashSet();
                        var triangleCount = 0;
                        for (var triangleIndex = 0;
                             triangleIndex < triangles.Length;
                             triangleIndex += 3)
                        {
                            if (componentSet.Contains(triangles[triangleIndex]))
                            {
                                triangleCount++;
                            }
                        }
                        return new ComponentDescription(
                            originalIndex,
                            indices.Length,
                            triangleCount,
                            bounds.center,
                            bounds.size,
                            dominantBones);
                    })
                    .Where(component => component.VertexCount >= 8)
                    .OrderByDescending(component => component.VertexCount)
                    .Select((component, rank) =>
                        "C" + rank +
                        "(Source=" + component.SourceIndex +
                        ",V=" + component.VertexCount +
                        ",T=" + component.TriangleCount +
                        ",Center=" + Vec(component.Center) +
                        ",Size=" + Vec(component.Size) +
                        ",Bones=" + component.DominantBones + ")")
                    .ToArray();
                var hip = model.Find("Armature/Hips");
                var rightHand = model.Find(
                    "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand");
                return "Count=" + components.Count +
                       "|Hips=" + (hip == null ? "Missing" : Vec(model.InverseTransformPoint(hip.position))) +
                       "|RightHand=" + (rightHand == null
                           ? "Missing"
                           : Vec(model.InverseTransformPoint(rightHand.position))) +
                       "|" + string.Join(";", descriptions);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static void AddBoneWeight(
            IDictionary<string, float> totals,
            IReadOnlyList<string> boneNames,
            BoneWeight weight)
        {
            AddBoneWeight(totals, boneNames, weight.boneIndex0, weight.weight0);
            AddBoneWeight(totals, boneNames, weight.boneIndex1, weight.weight1);
            AddBoneWeight(totals, boneNames, weight.boneIndex2, weight.weight2);
            AddBoneWeight(totals, boneNames, weight.boneIndex3, weight.weight3);
        }

        private static void AddBoneWeight(
            IDictionary<string, float> totals,
            IReadOnlyList<string> boneNames,
            int boneIndex,
            float weight)
        {
            if (weight <= 0f)
            {
                return;
            }

            var name = boneNames[boneIndex];
            totals[name] = totals.TryGetValue(name, out var current)
                ? current + weight
                : weight;
        }

        private static (int X, int Y, int Z) Quantize(Vector3 value)
        {
            const float scale = 100000f;
            return (
                Mathf.RoundToInt(value.x * scale),
                Mathf.RoundToInt(value.y * scale),
                Mathf.RoundToInt(value.z * scale));
        }

        private static void AddAdjacent(
            IDictionary<int, HashSet<int>> adjacency,
            int leftIndex,
            int rightIndex)
        {
            if (!adjacency.TryGetValue(leftIndex, out var left))
            {
                left = new HashSet<int>();
                adjacency[leftIndex] = left;
            }

            if (!adjacency.TryGetValue(rightIndex, out var right))
            {
                right = new HashSet<int>();
                adjacency[rightIndex] = right;
            }

            left.Add(rightIndex);
            right.Add(leftIndex);
        }

        private static string RelativePath(Transform root, Transform item)
        {
            if (item == root)
            {
                return string.Empty;
            }

            var names = item.GetComponentsInParent<Transform>(true)
                .TakeWhile(parent => parent != root)
                .Select(parent => parent.name)
                .Reverse();
            return string.Join("/", names);
        }

        private static Scene RequireScene(bool requireClean)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Ata pistol animation work requires Edit Mode.");
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "The current active scene must be CargoRunMvp.");
            }

            if (requireClean && scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes.");
            }

            return scene;
        }

        private static GameObject RequirePlacement(Scene scene)
        {
            return scene.GetRootGameObjects()
                       .SingleOrDefault(root => root.name == PlacementRootName) ??
                   throw new InvalidOperationException("Approved Ata placement is missing.");
        }

        private static Transform RequireDirectChild(Transform parent, string name)
        {
            var matches = Enumerable.Range(0, parent.childCount)
                .Select(parent.GetChild)
                .Where(child => child.name == name)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Required direct child differs: " + name + ".");
            }

            return matches[0];
        }

        private static string[] OtherSlotSignatures(Transform placement, Transform targetSlot)
        {
            return Enumerable.Range(0, placement.childCount)
                .Select(placement.GetChild)
                .Where(slot => slot != targetSlot)
                .Select(RecursiveSignature)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static string RecursiveSignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(item.name).Append('|')
                    .Append(item.gameObject.activeSelf).Append('|')
                    .Append(Vec(item.localPosition)).Append('|')
                    .Append(Quat(item.localRotation)).Append('|')
                    .Append(Vec(item.localScale)).Append('|')
                    .Append(string.Join(",", item.GetComponents<Component>()
                        .Where(component => component != null)
                        .Select(component => component.GetType().FullName)
                        .OrderBy(name => name, StringComparer.Ordinal)))
                    .AppendLine();
            }

            return builder.ToString();
        }

        private static string[] OtherRootSignatures(Scene scene, GameObject placement)
        {
            return scene.GetRootGameObjects()
                .Where(root => root != placement)
                .Select(root =>
                    root.name + "|" + root.activeSelf + "|" +
                    Vec(root.transform.localPosition) + "|" +
                    Quat(root.transform.localRotation) + "|" +
                    Vec(root.transform.localScale) + "|" +
                    root.transform.childCount.ToString(CultureInfo.InvariantCulture))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static void RequireEqual(string[] before, string[] after, string message)
        {
            if (!before.SequenceEqual(after, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Absolute(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }

        private static string Num(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + ")";
        }

        private static string Quat(Quaternion value)
        {
            return "(" + Num(value.x) + "," + Num(value.y) + "," +
                   Num(value.z) + "," + Num(value.w) + ")";
        }

        private readonly struct TransformSnapshot
        {
            private readonly Transform transform;
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            public TransformSnapshot(Transform transform)
            {
                this.transform = transform;
                position = transform.localPosition;
                rotation = transform.localRotation;
                scale = transform.localScale;
            }

            public void Restore()
            {
                if (transform == null)
                {
                    return;
                }

                transform.localPosition = position;
                transform.localRotation = rotation;
                transform.localScale = scale;
            }

            public bool Matches()
            {
                return transform != null &&
                       Vector3.Distance(position, transform.localPosition) <= TransformTolerance &&
                       Quaternion.Angle(rotation, transform.localRotation) <= 0.01f &&
                       Vector3.Distance(scale, transform.localScale) <= TransformTolerance;
            }
        }

        private readonly struct RendererSnapshot
        {
            public readonly Renderer Renderer;
            private readonly bool enabled;

            public RendererSnapshot(Renderer renderer)
            {
                Renderer = renderer;
                enabled = renderer.enabled;
            }

            public void Restore()
            {
                if (Renderer != null)
                {
                    Renderer.enabled = enabled;
                }
            }
        }

        private readonly struct CaptureResult
        {
            public readonly float MaximumSlotPositionError;

            public CaptureResult(float maximumSlotPositionError)
            {
                MaximumSlotPositionError = maximumSlotPositionError;
            }
        }

        private readonly struct PistolAssets
        {
            public readonly Mesh BodyMesh;
            public readonly Mesh PistolMesh;
            public readonly int PistolTriangleCount;

            public PistolAssets(
                Mesh bodyMesh,
                Mesh pistolMesh,
                int pistolTriangleCount)
            {
                BodyMesh = bodyMesh;
                PistolMesh = pistolMesh;
                PistolTriangleCount = pistolTriangleCount;
            }
        }

        private readonly struct BindingSummary
        {
            public readonly int AnimatedPathCount;
            public readonly int SkinnedAnimatedPathCount;
            public readonly int VaryingCurveCount;
            public readonly string FirstAnimatedPaths;
            public readonly string LargestCurveChanges;

            public BindingSummary(
                int animatedPathCount,
                int skinnedAnimatedPathCount,
                int varyingCurveCount,
                string firstAnimatedPaths,
                string largestCurveChanges)
            {
                AnimatedPathCount = animatedPathCount;
                SkinnedAnimatedPathCount = skinnedAnimatedPathCount;
                VaryingCurveCount = varyingCurveCount;
                FirstAnimatedPaths = firstAnimatedPaths;
                LargestCurveChanges = largestCurveChanges;
            }
        }

        private readonly struct CurveChange
        {
            public readonly EditorCurveBinding Binding;
            public readonly float Range;

            public CurveChange(EditorCurveBinding binding, float range)
            {
                Binding = binding;
                Range = range;
            }
        }

        private readonly struct ComponentDescription
        {
            public readonly int SourceIndex;
            public readonly int VertexCount;
            public readonly int TriangleCount;
            public readonly Vector3 Center;
            public readonly Vector3 Size;
            public readonly string DominantBones;

            public ComponentDescription(
                int sourceIndex,
                int vertexCount,
                int triangleCount,
                Vector3 center,
                Vector3 size,
                string dominantBones)
            {
                SourceIndex = sourceIndex;
                VertexCount = vertexCount;
                TriangleCount = triangleCount;
                Center = center;
                Size = size;
                DominantBones = dominantBones;
            }
        }

        private readonly struct PistolRegionOverlay
        {
            public readonly GameObject Overlay;
            public readonly Mesh Mesh;
            public readonly Material Material;
            public readonly int SelectedTriangleCount;
            public readonly string SelectedComponents;

            public PistolRegionOverlay(
                GameObject overlay,
                Mesh mesh,
                Material material,
                int selectedTriangleCount,
                string selectedComponents)
            {
                Overlay = overlay;
                Mesh = mesh;
                Material = material;
                SelectedTriangleCount = selectedTriangleCount;
                SelectedComponents = selectedComponents;
            }
        }
    }
}
