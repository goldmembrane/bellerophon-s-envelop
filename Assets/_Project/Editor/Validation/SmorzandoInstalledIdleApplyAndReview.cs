using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bellerophon.Enemies.Smorzando;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.SmorzandoCargoRunScene
{
    internal static class SmorzandoInstalledIdleApplyAndReview
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string InstalledModelAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Models/Smorzando_Installed.fbx";
        private const string SmorzandoRootName = "Approved Smorzando Enemy Placement";
        private const string PlayerRootName = "Player";
        private const string InstalledSlotPrefix = "Smorzando_Installed_";
        private const string InstalledModelName = "Smorzando_Installed_Model";
        private const string FlameObjectName = "Smorzando_Installed_Flame";
        private const string FlameTextureAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/VFX/Textures/Smorzando_Flame_SoftTeardrop.png";
        private const string FlameOuterMaterialAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/VFX/Materials/Smorzando_Flame_Outer.mat";
        private const string FlameCoreMaterialAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/VFX/Materials/Smorzando_Flame_Core.mat";
        private const string FlamePrefabAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/VFX/Prefabs/Smorzando_Installed_Flame.prefab";
        private const string ModeledFlameMeshAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Models/Derived/Smorzando_Installed_ModeledFlame.asset";
        private const string ModeledWickMeshAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Models/Derived/Smorzando_Installed_ModeledWick.asset";
        private const string ModeledFlameMaterialAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/VFX/Materials/Smorzando_ModeledFlame_Core.mat";
        private const string ModeledWickMaterialAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/VFX/Materials/Smorzando_ModeledFlame_Wick.mat";
        private const string FlameEnvelopeMaterialAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/VFX/Materials/Smorzando_ModeledFlame_Envelope.mat";
        private const string InstalledReferenceMaterialAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Materials/Smorzando_Installed_Reference.mat";
        private const string ValidationRelativeFolder =
            "docs/validation/smorzando_installed_idle_2026-07-17";
        private const string HybridFlameValidationRelativeFolder =
            "docs/validation/smorzando_hybrid_flame_2026-07-17";
        private const string CaptureRelativeFolder =
            "docs/validation/smorzando_installed_idle_2026-07-17/automated_visual_capture";
        private const string HybridFlameCaptureRelativeFolder =
            "docs/validation/smorzando_hybrid_flame_2026-07-17/automated_visual_capture";
        private const int InstalledCount = 3;
        private const int IdleInstalledSlotNumber = 2;
        private const int CaptureLayer = 30;
        private const int CycleFrameCount = 32;
        private const float CycleDurationSeconds = 3.2f;
        private const float FlameAnchorGapMeters = 0.012f;
        private const string FlameEnvelopeObjectName = "FlameEnvelope";
        private static readonly Vector3 HybridFlamePivot = new Vector3(0f, -0.000039f, 0.00215f);

        [MenuItem("Bellerophon/Enemies/Smorzando/Inspect Modeled Flame Geometry")]
        public static void InspectSmorzandoModeledFlameGeometry()
        {
            var installedAsset = AssetDatabase.LoadAssetAtPath<GameObject>(InstalledModelAssetPath) ??
                throw new InvalidOperationException("Smorzando installed FBX has not been imported.");
            var meshFilter = installedAsset.GetComponentInChildren<MeshFilter>(true) ??
                throw new InvalidOperationException("Smorzando installed FBX has no MeshFilter.");
            var mesh = meshFilter.sharedMesh ??
                throw new InvalidOperationException("Smorzando installed FBX MeshFilter has no mesh.");
            if (!mesh.isReadable)
            {
                throw new InvalidOperationException("Smorzando installed mesh must be readable for flame geometry inspection.");
            }

            var components = FindConnectedTriangleComponents(mesh)
                .OrderByDescending(component => component.Bounds.max.z)
                .ThenByDescending(component => component.TriangleIndices.Count)
                .ToArray();
            var lines = new List<string>
            {
                "Asset=" + InstalledModelAssetPath,
                "Mesh=" + mesh.name,
                "MeshBoundsMin=" + FormatVector(mesh.bounds.min),
                "MeshBoundsMax=" + FormatVector(mesh.bounds.max),
                "ConnectedComponentCount=" + components.Length,
                "Sort=BoundsMaxZDescendingThenTriangleCountDescending"
            };
            for (var index = 0; index < components.Length; index++)
            {
                var component = components[index];
                lines.Add(
                    $"Component[{index}]=" +
                    $"Triangles:{component.TriangleIndices.Count}," +
                    $"Vertices:{component.VertexIndices.Count}," +
                    $"Min:{FormatVector(component.Bounds.min)}," +
                    $"Max:{FormatVector(component.Bounds.max)}," +
                    $"Center:{FormatVector(component.Bounds.center)}," +
                    $"Size:{FormatVector(component.Bounds.size)}");
            }

            var normalizedSliceHeights = new[] { 0.72f, 0.78f, 0.82f, 0.86f, 0.90f, 0.94f };
            foreach (var normalizedHeight in normalizedSliceHeights)
            {
                var minimumZ = Mathf.Lerp(mesh.bounds.min.z, mesh.bounds.max.z, normalizedHeight);
                var sliceComponents = FindConnectedTriangleComponents(mesh, minimumZ)
                    .OrderByDescending(component => component.TriangleIndices.Count)
                    .ThenByDescending(component => component.Bounds.max.z)
                    .ToArray();
                lines.Add(
                    $"Slice[{normalizedHeight:0.##}]=MinimumZ:{minimumZ:0.########}," +
                    $"Components:{sliceComponents.Length}");
                for (var index = 0; index < Mathf.Min(sliceComponents.Length, 12); index++)
                {
                    var component = sliceComponents[index];
                    lines.Add(
                        $"Slice[{normalizedHeight:0.##}].Component[{index}]=" +
                        $"Triangles:{component.TriangleIndices.Count}," +
                        $"Vertices:{component.VertexIndices.Count}," +
                        $"Min:{FormatVector(component.Bounds.min)}," +
                        $"Max:{FormatVector(component.Bounds.max)}," +
                        $"Center:{FormatVector(component.Bounds.center)}," +
                        $"Size:{FormatVector(component.Bounds.size)}");
                }
            }

            var modeledFlame = CreateExtractedMesh(
                mesh,
                centroid => IsCentralModeledFlameTriangle(centroid),
                0.000008f,
                "Smorzando_ModeledFlame_Diagnostic");
            var modeledWick = CreateExtractedMesh(
                mesh,
                centroid => IsCentralModeledWickTriangle(centroid),
                0.000008f,
                "Smorzando_ModeledWick_Diagnostic");
            try
            {
                lines.Add(
                    $"ModeledFlameSelection=Triangles:{modeledFlame.triangles.Length / 3}," +
                    $"Vertices:{modeledFlame.vertexCount},Min:{FormatVector(modeledFlame.bounds.min)}," +
                    $"Max:{FormatVector(modeledFlame.bounds.max)},Size:{FormatVector(modeledFlame.bounds.size)}");
                lines.Add(
                    $"ModeledWickSelection=Triangles:{modeledWick.triangles.Length / 3}," +
                    $"Vertices:{modeledWick.vertexCount},Min:{FormatVector(modeledWick.bounds.min)}," +
                    $"Max:{FormatVector(modeledWick.bounds.max)},Size:{FormatVector(modeledWick.bounds.size)}");
                var previewPath = Path.Combine(
                    ProjectAbsolutePath(HybridFlameValidationRelativeFolder),
                    "Smorzando_ModeledFlameSegmentationPreview.png");
                CaptureModeledFlameSegmentationPreview(
                    installedAsset,
                    modeledFlame,
                    modeledWick,
                    previewPath);
                lines.Add("SegmentationPreview=" + previewPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(modeledFlame);
                UnityEngine.Object.DestroyImmediate(modeledWick);
            }

            lines.Add("SceneChanged=False");
            lines.Add("SelectionCleared=True");
            var folder = ProjectAbsolutePath(HybridFlameValidationRelativeFolder);
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, "Smorzando_ModeledFlameGeometry.txt");
            File.WriteAllLines(path, lines);
            Selection.activeObject = null;
            Debug.Log(
                $"SmorzandoModeledFlameGeometryInspected Path={path}, Components={components.Length}, " +
                "SceneChanged=False, SelectionCleared=True");
        }

        [MenuItem("Bellerophon/Enemies/Smorzando/Apply Installed Idle")]
        public static void ApplySmorzandoInstalledIdle()
        {
            ConfigureInstalledMeshReadability();
            var flamePrefab = CreateOrUpdateHybridFlamePrefab();

            var scene = RequireOpenCargoRunScene();
            var smorzandoRoot = RequireRoot(scene, SmorzandoRootName);
            var player = RequireRoot(scene, PlayerRootName);
            var preservedRootTransforms = scene.GetRootGameObjects()
                .Select(root => new TransformSnapshot(root.transform))
                .ToArray();
            var preservedSmorzandoTransforms = smorzandoRoot.GetComponentsInChildren<Transform>(true)
                .Where(target => !IsFlameTransform(target))
                .Select(target => new TransformSnapshot(target))
                .ToArray();
            var phaseOffsets = new float[InstalledCount];
            var topAnchors = new Vector3[InstalledCount];

            for (var index = 0; index < InstalledCount; index++)
            {
                var slot = smorzandoRoot.transform.Find(InstalledSlotPrefix + (index + 1).ToString("00")) ??
                    throw new InvalidOperationException("Smorzando installed review slot is missing: " + (index + 1));
                var model = slot.Find(InstalledModelName) ??
                    throw new InvalidOperationException("Smorzando installed model is missing: " + (index + 1));
                var meshFilter = model.GetComponent<MeshFilter>() ??
                    throw new InvalidOperationException("Smorzando installed model has no MeshFilter: " + (index + 1));
                var existingIdleMotion = model.GetComponent<SmorzandoInstalledIdleMotion>();
                if (existingIdleMotion != null)
                {
                    existingIdleMotion.RestoreSourceMesh();
                }
                var existingFlameMotion = model.GetComponent<SmorzandoInstalledFlameMotion>();
                if (existingFlameMotion != null)
                {
                    existingFlameMotion.RestoreBasePose();
                }

                var existingFlame = model.Find(FlameObjectName);
                if (existingFlame != null)
                {
                    UnityEngine.Object.DestroyImmediate(existingFlame.gameObject);
                }

                var flameInstance = PrefabUtility.InstantiatePrefab(flamePrefab, model) as GameObject ??
                    throw new InvalidOperationException("Smorzando installed flame prefab could not be instantiated.");
                flameInstance.name = FlameObjectName;
                var anchor = HybridFlamePivot;
                flameInstance.transform.localPosition = anchor;
                flameInstance.transform.localRotation = Quaternion.identity;
                flameInstance.transform.localScale = Vector3.one;
                var phaseOffset = index * (CycleDurationSeconds / InstalledCount);
                var flameMotion = existingFlameMotion != null
                    ? existingFlameMotion
                    : Undo.AddComponent<SmorzandoInstalledFlameMotion>(model.gameObject);
                flameMotion.Configure(
                    flameInstance.transform.Find(FlameEnvelopeObjectName),
                    flameInstance.GetComponent<Light>(),
                    phaseOffset);
                EditorUtility.SetDirty(flameMotion);

                if (index + 1 == IdleInstalledSlotNumber)
                {
                    var idleMotion = existingIdleMotion != null
                        ? existingIdleMotion
                        : Undo.AddComponent<SmorzandoInstalledIdleMotion>(model.gameObject);
                    idleMotion.Configure(meshFilter, flameInstance.transform, anchor, phaseOffset);
                    EditorUtility.SetDirty(idleMotion);
                }
                else if (existingIdleMotion != null)
                {
                    Undo.DestroyObjectImmediate(existingIdleMotion);
                }

                phaseOffsets[index] = phaseOffset;
                topAnchors[index] = anchor;
                EditorUtility.SetDirty(model);
            }

            foreach (var snapshot in preservedRootTransforms)
            {
                snapshot.AssertUnchanged();
            }

            foreach (var snapshot in preservedSmorzandoTransforms)
            {
                snapshot.AssertUnchanged();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Selection.activeObject = null;
            var reportFolder = ProjectAbsolutePath(ValidationRelativeFolder);
            Directory.CreateDirectory(reportFolder);
            File.WriteAllLines(
                Path.Combine(reportFolder, "Smorzando_InstalledIdleApply.txt"),
                new[]
                {
                    "Scene=" + CargoRunScenePath,
                    "TargetRoot=" + SmorzandoRootName,
                    "InstalledMotionCount=" + smorzandoRoot.GetComponentsInChildren<SmorzandoInstalledIdleMotion>(true).Length,
                    "InstalledFlameMotionCount=" + smorzandoRoot.GetComponentsInChildren<SmorzandoInstalledFlameMotion>(true).Length,
                    "IdleBodyMotionSlot=Smorzando_Installed_02",
                    "StaticBodySlots=Smorzando_Installed_01|Smorzando_Installed_03",
                    "CycleDurationSeconds=" + CycleDurationSeconds.ToString("0.####"),
                    "PhaseOffsetsSeconds=" + string.Join(",", phaseOffsets.Select(value => value.ToString("0.####"))),
                    "BodyBreathScale=0.008",
                    "PoolBreathScale=0.004",
                    "PoolWaveHeightMeters=0.006",
                    "WholeBodyBobHeightMeters=0.008",
                    "FlameRangeMeters=2",
                    "FlameBaseIntensity=0.45",
                    "FlameShadows=None",
                    "FlameAnchors=" + string.Join("|", topAnchors.Select(FormatVector)),
                    "PlayerPosition=" + FormatVector(player.transform.position),
                    "ExistingTransformsChanged=False",
                    "SelectionCleared=True"
                });
            Debug.Log(
                "SmorzandoInstalledIdleApplied InstalledMotionCount=1, InstalledFlameMotionCount=3, " +
                "IdleBodySlot=02, StaticBodySlots=01|03, Cycle=3.2s, " +
                "FlamePhaseOffsets=0|1.0667|2.1333, PoolWave=0.006m, BodyBob=0.008m, " +
                "FlameRange=2m, FlameIntensity=0.45, ExistingTransformsChanged=False, SelectionCleared=True");
        }

        [MenuItem("Bellerophon/Enemies/Smorzando/Apply Hybrid Flame")]
        public static void ApplySmorzandoHybridFlame()
        {
            ConfigureInstalledMeshReadability();
            var flamePrefab = CreateOrUpdateHybridFlamePrefab();
            var scene = RequireOpenCargoRunScene();
            var smorzandoRoot = RequireRoot(scene, SmorzandoRootName);
            var player = RequireRoot(scene, PlayerRootName);
            var preservedRootTransforms = scene.GetRootGameObjects()
                .Select(root => new TransformSnapshot(root.transform))
                .ToArray();
            var preservedSmorzandoTransforms = smorzandoRoot.GetComponentsInChildren<Transform>(true)
                .Where(target => !IsFlameTransform(target))
                .Select(target => new TransformSnapshot(target))
                .ToArray();
            var phaseOffsets = new List<float>();

            for (var index = 0; index < InstalledCount; index++)
            {
                var slot = smorzandoRoot.transform.Find(InstalledSlotPrefix + (index + 1).ToString("00")) ??
                    throw new InvalidOperationException("Smorzando installed review slot is missing: " + (index + 1));
                var model = slot.Find(InstalledModelName) ??
                    throw new InvalidOperationException("Smorzando installed model is missing: " + (index + 1));
                var meshFilter = model.GetComponent<MeshFilter>() ??
                    throw new InvalidOperationException("Smorzando installed model has no MeshFilter: " + (index + 1));
                var idleMotion = model.GetComponent<SmorzandoInstalledIdleMotion>();
                idleMotion?.RestoreSourceMesh();
                var flameMotion = model.GetComponent<SmorzandoInstalledFlameMotion>();
                flameMotion?.RestoreBasePose();
                var phaseOffset = flameMotion != null
                    ? flameMotion.PhaseOffsetSeconds
                    : index * (CycleDurationSeconds / InstalledCount);
                var existingFlame = model.Find(FlameObjectName);
                if (existingFlame != null)
                {
                    UnityEngine.Object.DestroyImmediate(existingFlame.gameObject);
                }

                var flameInstance = PrefabUtility.InstantiatePrefab(flamePrefab, model) as GameObject ??
                    throw new InvalidOperationException("Smorzando hybrid flame prefab could not be instantiated.");
                flameInstance.name = FlameObjectName;
                flameInstance.transform.localPosition = HybridFlamePivot;
                flameInstance.transform.localRotation = Quaternion.identity;
                flameInstance.transform.localScale = Vector3.one;
                var envelope = flameInstance.transform.Find(FlameEnvelopeObjectName) ??
                    throw new InvalidOperationException("Smorzando hybrid flame envelope is missing.");
                flameMotion = flameMotion != null
                    ? flameMotion
                    : Undo.AddComponent<SmorzandoInstalledFlameMotion>(model.gameObject);
                flameMotion.Configure(envelope, flameInstance.GetComponent<Light>(), phaseOffset);
                EditorUtility.SetDirty(flameMotion);
                if (index + 1 == IdleInstalledSlotNumber)
                {
                    idleMotion = idleMotion != null
                        ? idleMotion
                        : Undo.AddComponent<SmorzandoInstalledIdleMotion>(model.gameObject);
                    idleMotion.Configure(meshFilter, flameInstance.transform, HybridFlamePivot, phaseOffset);
                    EditorUtility.SetDirty(idleMotion);
                }
                else if (idleMotion != null)
                {
                    Undo.DestroyObjectImmediate(idleMotion);
                }

                phaseOffsets.Add(phaseOffset);
                EditorUtility.SetDirty(model);
            }

            foreach (var snapshot in preservedRootTransforms)
            {
                snapshot.AssertUnchanged();
            }

            foreach (var snapshot in preservedSmorzandoTransforms)
            {
                snapshot.AssertUnchanged();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Selection.activeObject = null;
            var reportFolder = ProjectAbsolutePath(HybridFlameValidationRelativeFolder);
            Directory.CreateDirectory(reportFolder);
            File.WriteAllLines(
                Path.Combine(reportFolder, "Smorzando_HybridFlameApply.txt"),
                new[]
                {
                    "Scene=" + CargoRunScenePath,
                    "TargetRoot=" + SmorzandoRootName,
                    "ModeledFlameMesh=" + ModeledFlameMeshAssetPath,
                    "ModeledWickMesh=" + ModeledWickMeshAssetPath,
                    "FlameEnvelopeUsesModeledFlameMesh=True",
                    "HybridFlameInstanceCount=" + InstalledCount,
                    "InstalledBodyMotionCount=" + smorzandoRoot.GetComponentsInChildren<SmorzandoInstalledIdleMotion>(true).Length,
                    "InstalledFlameMotionCount=" + smorzandoRoot.GetComponentsInChildren<SmorzandoInstalledFlameMotion>(true).Length,
                    "IdleBodyMotionSlot=Smorzando_Installed_02",
                    "PhaseOffsetsSeconds=" + string.Join(",", phaseOffsets.Select(value => value.ToString("0.####"))),
                    "HybridFlamePivot=" + FormatVector(HybridFlamePivot),
                    "EnvelopeScale=1.08",
                    "FlameRangeMeters=2",
                    "FlameBaseIntensity=0.45",
                    "FlameFlickerAmount=0.12",
                    "FlameShadows=None",
                    "PlayerPosition=" + FormatVector(player.transform.position),
                    "IdleMotionValuesChanged=False",
                    "ExistingTransformsChanged=False",
                    "SelectionCleared=True"
                });
            Debug.Log(
                "SmorzandoHybridFlameApplied Instances=3, ModeledCore=True, BlackWick=True, " +
                "EnvelopeScale=1.08, FlickerRetained=True, IdleBodySlot=02, StaticBodySlots=01|03, " +
                "IdleMotionValuesChanged=False, " +
                "ExistingTransformsChanged=False, SelectionCleared=True");
        }

        [MenuItem("Bellerophon/Enemies/Smorzando/Capture Hybrid Flame Frames")]
        public static void CaptureSmorzandoHybridFlameFrames()
        {
            CaptureSmorzandoInstalledIdleFrames();
            CopyDirectory(
                ProjectAbsolutePath(CaptureRelativeFolder),
                ProjectAbsolutePath(HybridFlameCaptureRelativeFolder));
            Selection.activeObject = null;
            Debug.Log(
                $"SmorzandoHybridFlameFramesCaptured Folder={ProjectAbsolutePath(HybridFlameCaptureRelativeFolder)}, " +
                "RenderedOnce=True, SceneViewFocused=False, SelectionCleared=True");
        }

        [MenuItem("Bellerophon/Enemies/Smorzando/Capture Installed Idle Frames")]
        public static void CaptureSmorzandoInstalledIdleFrames()
        {
            var scene = RequireOpenCargoRunScene();
            var sceneWasDirty = scene.isDirty;
            var smorzandoRoot = RequireRoot(scene, SmorzandoRootName);
            var player = RequireRoot(scene, PlayerRootName);
            var playerCamera = FindPlayerCamera(scene, player.transform);
            var captureFolder = ProjectAbsolutePath(CaptureRelativeFolder);
            var cycleFolder = Path.Combine(captureFolder, "cycle_frames");
            Directory.CreateDirectory(cycleFolder);
            var cameraObject = new GameObject("Smorzando_InstalledIdle_CaptureCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var keyLightObject = new GameObject("Smorzando_InstalledIdle_CaptureKeyLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            GameObject previewClone = null;
            GameObject floorObject = null;
            Material floorMaterial = null;
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.022f, 0.018f, 0.016f, 1f);
                camera.orthographic = true;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 100f;
                var keyLight = keyLightObject.AddComponent<Light>();
                keyLight.type = LightType.Directional;
                keyLight.intensity = 3.2f;
                keyLight.color = new Color(1f, 0.82f, 0.68f, 1f);
                keyLight.cullingMask = 1 << CaptureLayer;
                keyLight.shadows = LightShadows.None;
                keyLightObject.transform.rotation = Quaternion.Euler(38f, -28f, 0f);

                var idleSlot = smorzandoRoot.transform.Find(
                    InstalledSlotPrefix + IdleInstalledSlotNumber.ToString("00")) ??
                    throw new InvalidOperationException("Smorzando installed idle review slot is missing.");
                previewClone = UnityEngine.Object.Instantiate(idleSlot.gameObject);
                previewClone.name = "Smorzando_InstalledIdle_CaptureClone";
                SetCaptureOnly(previewClone);
                var motion = previewClone.GetComponentInChildren<SmorzandoInstalledIdleMotion>(true) ??
                    throw new InvalidOperationException("Smorzando installed idle motion is missing on capture clone.");
                var flameMotion = previewClone.GetComponentInChildren<SmorzandoInstalledFlameMotion>(true) ??
                    throw new InvalidOperationException("Smorzando installed flame motion is missing on capture clone.");
                motion.PreparePreview();
                flameMotion.PreparePreview();
                motion.SampleAtTime(0f);
                flameMotion.SampleAtTime(0f);
                ConfigureCaptureLights(previewClone);
                var fixedBounds = CalculateVisibleBounds(previewClone.transform);
                floorObject = CreateCaptureFloor(fixedBounds, out floorMaterial);
                var target = fixedBounds.center + Vector3.up * 0.02f;
                var viewDirection = (Vector3.back + Vector3.right * 0.45f).normalized;
                var cameraPosition = target + viewDirection * 40f;
                var orthographicSize = Mathf.Max(fixedBounds.extents.y + 0.22f, fixedBounds.extents.x + 0.22f);

                for (var frame = 0; frame < CycleFrameCount; frame++)
                {
                    var time = frame * CycleDurationSeconds / CycleFrameCount;
                    motion.SampleAtTime(time);
                    flameMotion.SampleAtTime(time);
                    CapturePng(
                        camera,
                        cameraPosition,
                        target,
                        Vector3.up,
                        orthographicSize,
                        640,
                        640,
                        Path.Combine(cycleFolder, $"Smorzando_InstalledIdle_{frame:000}.png"));
                }

                CaptureInstalledScopeRow(
                    smorzandoRoot.transform,
                    camera,
                    captureFolder,
                    0f,
                    "Smorzando_InstalledIdle_BodyScope_T000.png");
                CaptureInstalledScopeRow(
                    smorzandoRoot.transform,
                    camera,
                    captureFolder,
                    0.8f,
                    "Smorzando_InstalledIdle_BodyScope_T080.png");
                SampleActualInstalledMotions(smorzandoRoot.transform, 0f, true);
                SaveCurrentCameraPng(
                    playerCamera,
                    Path.Combine(captureFolder, "Smorzando_InstalledIdle_PlayerView.png"),
                    1280,
                    720);
                SampleActualInstalledMotions(smorzandoRoot.transform, 0f, false);
                File.WriteAllLines(
                    Path.Combine(captureFolder, "Smorzando_InstalledIdle_CaptureManifest.txt"),
                    new[]
                    {
                        "CycleDurationSeconds=3.2",
                        "CycleFrameCount=32",
                        "CycleFramesPerSecond=10",
                        "KeyFrames=000|008|016|024",
                        "IdleBodyMotionSlot=Smorzando_Installed_02",
                        "StaticBodySlots=Smorzando_Installed_01|Smorzando_Installed_03",
                        "FlamePhaseOffsetsSeconds=0|1.0667|2.1333",
                        "Views=SingleIdleCycle|BodyScopeT000|BodyScopeT080|PlayerMainCamera",
                        "SceneViewFocused=False",
                        "SceneSaved=False",
                        "SelectionCleared=True"
                    });
                Selection.activeObject = null;
                Debug.Log(
                    $"SmorzandoInstalledIdleFramesCaptured Folder={captureFolder}, Frames=32, " +
                    "Views=SingleIdleCycle|BodyScopeT000|BodyScopeT080|PlayerMainCamera, SceneViewFocused=False, " +
                    "SceneSaved=False, SelectionCleared=True");
            }
            finally
            {
                SampleActualInstalledMotions(smorzandoRoot.transform, 0f, false);
                if (previewClone != null)
                {
                    var previewMotion = previewClone.GetComponentInChildren<SmorzandoInstalledIdleMotion>(true);
                    previewMotion?.RestoreSourceMesh();
                    var previewFlameMotion = previewClone.GetComponentInChildren<SmorzandoInstalledFlameMotion>(true);
                    previewFlameMotion?.RestoreBasePose();
                }

                UnityEngine.Object.DestroyImmediate(previewClone);
                UnityEngine.Object.DestroyImmediate(floorObject);
                UnityEngine.Object.DestroyImmediate(floorMaterial);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyLightObject);
                Selection.activeObject = null;
                if (scene.isDirty != sceneWasDirty)
                {
                    throw new InvalidOperationException("Smorzando installed idle capture changed the scene dirty state.");
                }
            }
        }

        private static void ConfigureInstalledMeshReadability()
        {
            var importer = AssetImporter.GetAtPath(InstalledModelAssetPath) as ModelImporter ??
                throw new InvalidOperationException("Smorzando installed model importer is missing.");
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
        }

        private static GameObject CreateOrUpdateHybridFlamePrefab()
        {
            var installedAsset = AssetDatabase.LoadAssetAtPath<GameObject>(InstalledModelAssetPath) ??
                throw new InvalidOperationException("Smorzando installed FBX has not been imported.");
            var sourceMesh = installedAsset.GetComponentInChildren<MeshFilter>(true)?.sharedMesh ??
                throw new InvalidOperationException("Smorzando installed FBX has no source mesh.");
            var modeledFlame = CreateExtractedMesh(
                sourceMesh,
                centroid => IsCentralModeledFlameTriangle(centroid),
                0.000008f,
                "Smorzando_Installed_ModeledFlame");
            var modeledWick = CreateExtractedMesh(
                sourceMesh,
                centroid => IsCentralModeledWickTriangle(centroid),
                0.000008f,
                "Smorzando_Installed_ModeledWick");
            RecenterMesh(modeledFlame, HybridFlamePivot);
            RecenterMesh(modeledWick, HybridFlamePivot);
            var modeledFlameAsset = SaveOrUpdateMeshAsset(modeledFlame, ModeledFlameMeshAssetPath);
            var modeledWickAsset = SaveOrUpdateMeshAsset(modeledWick, ModeledWickMeshAssetPath);
            var modeledFlameMaterial = CreateOrUpdateModeledFlameMaterial();
            var modeledWickMaterial = CreateOrUpdateModeledWickMaterial();
            var envelopeMaterial = CreateOrUpdateFlameEnvelopeMaterial();

            EnsureAssetFolder(FlamePrefabAssetPath);
            var root = new GameObject(FlameObjectName);
            try
            {
                var light = root.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.42f, 0.14f, 1f);
                light.intensity = 0.45f;
                light.range = 2f;
                light.shadows = LightShadows.None;
                light.renderMode = LightRenderMode.Auto;
                CreateHybridFlamePart(
                    root.transform,
                    "ModeledFlameCore",
                    modeledFlameAsset,
                    modeledFlameMaterial,
                    Vector3.one);
                CreateHybridFlamePart(
                    root.transform,
                    "ModeledWick",
                    modeledWickAsset,
                    modeledWickMaterial,
                    Vector3.one);
                CreateHybridFlamePart(
                    root.transform,
                    FlameEnvelopeObjectName,
                    modeledFlameAsset,
                    envelopeMaterial,
                    Vector3.one * 1.08f);
                return PrefabUtility.SaveAsPrefabAsset(root, FlamePrefabAssetPath) ??
                    throw new InvalidOperationException("Smorzando hybrid flame prefab could not be saved.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Mesh SaveOrUpdateMeshAsset(Mesh generatedMesh, string assetPath)
        {
            EnsureAssetFolder(assetPath);
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generatedMesh, assetPath);
                return generatedMesh;
            }

            EditorUtility.CopySerialized(generatedMesh, existing);
            EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(generatedMesh);
            return existing;
        }

        private static void RecenterMesh(Mesh mesh, Vector3 pivot)
        {
            var vertices = mesh.vertices;
            for (var index = 0; index < vertices.Length; index++)
            {
                vertices[index] -= pivot;
            }

            mesh.vertices = vertices;
            mesh.RecalculateBounds();
        }

        private static Material CreateOrUpdateModeledFlameMaterial()
        {
            EnsureAssetFolder(ModeledFlameMaterialAssetPath);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                throw new InvalidOperationException("URP Lit shader is missing.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(ModeledFlameMaterialAssetPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, ModeledFlameMaterialAssetPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", new Color(1f, 0.42f, 0.025f, 1f));
            material.SetTexture("_BaseMap", Texture2D.whiteTexture);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.48f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(2.2f, 0.62f, 0.025f, 1f));
            material.renderQueue = (int)RenderQueue.Geometry;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateOrUpdateModeledWickMaterial()
        {
            EnsureAssetFolder(ModeledWickMaterialAssetPath);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                throw new InvalidOperationException("URP Lit shader is missing.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(ModeledWickMaterialAssetPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, ModeledWickMaterialAssetPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", new Color(0.008f, 0.006f, 0.004f, 1f));
            material.SetTexture("_BaseMap", Texture2D.whiteTexture);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.18f);
            material.DisableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
            material.renderQueue = (int)RenderQueue.Geometry;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateOrUpdateFlameEnvelopeMaterial()
        {
            EnsureAssetFolder(FlameEnvelopeMaterialAssetPath);
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                throw new InvalidOperationException("URP Unlit shader is missing.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(FlameEnvelopeMaterialAssetPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, FlameEnvelopeMaterialAssetPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetTexture("_BaseMap", Texture2D.whiteTexture);
            material.SetColor("_BaseColor", new Color(1f, 0.12f, 0.008f, 0.32f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateHybridFlamePart(
            Transform parent,
            string name,
            Mesh mesh,
            Material material,
            Vector3 localScale)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = Vector3.zero;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = localScale;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
        {
            if (!Directory.Exists(sourceDirectory))
            {
                throw new DirectoryNotFoundException(sourceDirectory);
            }

            Directory.CreateDirectory(destinationDirectory);
            foreach (var sourceFile in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                var relativePath = sourceFile.Substring(sourceDirectory.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var destinationFile = Path.Combine(destinationDirectory, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile) ?? destinationDirectory);
                File.Copy(sourceFile, destinationFile, true);
            }
        }

        private static void WriteFlameTexture()
        {
            EnsureAssetFolder(FlameTextureAssetPath);
            const int width = 128;
            const int height = 256;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                var pixels = new Color[width * height];
                for (var y = 0; y < height; y++)
                {
                    var v = y / (float)(height - 1);
                    var bottomTaper = SmoothStep(0f, 0.09f, v);
                    var topTaper = Mathf.Pow(1f - v, 0.62f);
                    var halfWidth = Mathf.Max(0.012f, 0.36f * bottomTaper * topTaper);
                    var curl = Mathf.Sin(v * Mathf.PI * 1.35f) * 0.055f * v;
                    for (var x = 0; x < width; x++)
                    {
                        var u = x / (float)(width - 1);
                        var distance = Mathf.Abs(u - (0.5f + curl)) / halfWidth;
                        var edge = 1f - SmoothStep(0.68f, 1f, distance);
                        var verticalFade = SmoothStep(0f, 0.055f, v) * (1f - SmoothStep(0.84f, 1f, v));
                        var alpha = Mathf.Clamp01(edge * verticalFade);
                        pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                    }
                }

                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(ProjectAbsolutePath(FlameTextureAssetPath), texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void ConfigureFlameTextureImporter()
        {
            var importer = AssetImporter.GetAtPath(FlameTextureAssetPath) as TextureImporter ??
                throw new InvalidOperationException("Smorzando flame texture importer is missing.");
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static Material CreateOrUpdateFlameMaterial(
            string assetPath,
            Texture2D texture,
            Color tint)
        {
            EnsureAssetFolder(assetPath);
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                throw new InvalidOperationException("URP Unlit shader is missing.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, assetPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateOrUpdateFlamePrefab(Material outerMaterial, Material coreMaterial)
        {
            EnsureAssetFolder(FlamePrefabAssetPath);
            var root = new GameObject(FlameObjectName);
            try
            {
                var light = root.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.42f, 0.14f, 1f);
                light.intensity = 0.45f;
                light.range = 2f;
                light.shadows = LightShadows.None;
                light.renderMode = LightRenderMode.Auto;
                CreateFlamePlane(root.transform, "Outer_A", outerMaterial, new Vector2(0.00082f, 0.00138f), 0f);
                CreateFlamePlane(root.transform, "Outer_B", outerMaterial, new Vector2(0.00082f, 0.00138f), 90f);
                CreateFlamePlane(root.transform, "Core_A", coreMaterial, new Vector2(0.00040f, 0.00082f), 0f);
                CreateFlamePlane(root.transform, "Core_B", coreMaterial, new Vector2(0.00040f, 0.00082f), 90f);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, FlamePrefabAssetPath) ??
                    throw new InvalidOperationException("Smorzando installed flame prefab could not be saved.");
                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateFlamePlane(
            Transform parent,
            string name,
            Material material,
            Vector2 size,
            float yaw)
        {
            var plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plane.name = name;
            plane.transform.SetParent(parent, false);
            plane.transform.localPosition = Vector3.zero;
            plane.transform.localRotation = Quaternion.Euler(90f, 0f, yaw);
            plane.transform.localScale = new Vector3(size.x, size.y, 1f);
            var collider = plane.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            var renderer = plane.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static Vector3 CalculateFlameAnchor(Mesh mesh, Transform model)
        {
            if (mesh == null || !mesh.isReadable)
            {
                throw new InvalidOperationException("Smorzando installed mesh must be readable for the idle motion.");
            }

            var vertices = mesh.vertices;
            var sortedZ = vertices.Select(vertex => vertex.z).OrderBy(value => value).ToArray();
            var threshold = sortedZ[Mathf.Clamp(Mathf.RoundToInt((sortedZ.Length - 1) * 0.985f), 0, sortedZ.Length - 1)];
            var top = vertices.Where(vertex => vertex.z >= threshold).ToArray();
            var center = top.Aggregate(Vector3.zero, (sum, vertex) => sum + vertex) / Mathf.Max(top.Length, 1);
            var verticalScale = Mathf.Max(model.TransformVector(Vector3.forward).magnitude, 0.000001f);
            center.z += FlameAnchorGapMeters / verticalScale;
            return center;
        }

        private static void CaptureInstalledScopeRow(
            Transform smorzandoRoot,
            Camera camera,
            string captureFolder,
            float sampleTime,
            string fileName)
        {
            var rowRoot = new GameObject("Smorzando_InstalledIdle_ThreePhaseRow")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            GameObject floor = null;
            Material floorMaterial = null;
            try
            {
                for (var index = 0; index < InstalledCount; index++)
                {
                    var slot = smorzandoRoot.Find(InstalledSlotPrefix + (index + 1).ToString("00")) ??
                        throw new InvalidOperationException("Smorzando installed review slot is missing during row capture.");
                    var clone = UnityEngine.Object.Instantiate(slot.gameObject, rowRoot.transform, true);
                    clone.name = slot.name + "_CaptureClone";
                    SetCaptureOnly(clone);
                    foreach (var motion in clone.GetComponentsInChildren<SmorzandoInstalledIdleMotion>(true))
                    {
                        motion.PreparePreview();
                        motion.SampleAtTime(sampleTime);
                    }
                    var flameMotion = clone.GetComponentInChildren<SmorzandoInstalledFlameMotion>(true) ??
                        throw new InvalidOperationException("Smorzando installed flame motion is missing during row capture.");
                    flameMotion.PreparePreview();
                    flameMotion.SampleAtTime(sampleTime);
                    ConfigureCaptureLights(clone);
                }

                var bounds = CalculateVisibleBounds(rowRoot.transform);
                floor = CreateCaptureFloor(bounds, out floorMaterial);
                var aspect = 16f / 9f;
                var size = Mathf.Max(bounds.extents.y + 0.25f, bounds.extents.x / aspect + 0.25f);
                CapturePng(
                    camera,
                    bounds.center + Vector3.back * 40f,
                    bounds.center,
                    Vector3.up,
                    size,
                    1280,
                    720,
                    Path.Combine(captureFolder, fileName));
            }
            finally
            {
                foreach (var motion in rowRoot.GetComponentsInChildren<SmorzandoInstalledIdleMotion>(true))
                {
                    motion.RestoreSourceMesh();
                }
                foreach (var flameMotion in rowRoot.GetComponentsInChildren<SmorzandoInstalledFlameMotion>(true))
                {
                    flameMotion.RestoreBasePose();
                }

                UnityEngine.Object.DestroyImmediate(rowRoot);
                UnityEngine.Object.DestroyImmediate(floor);
                UnityEngine.Object.DestroyImmediate(floorMaterial);
            }
        }

        private static void SampleActualInstalledMotions(Transform smorzandoRoot, float time, bool sample)
        {
            foreach (var motion in smorzandoRoot.GetComponentsInChildren<SmorzandoInstalledIdleMotion>(true))
            {
                if (sample)
                {
                    motion.PreparePreview();
                    motion.SampleAtTime(time);
                }
                else
                {
                    motion.RestoreSourceMesh();
                }
            }
            foreach (var flameMotion in smorzandoRoot.GetComponentsInChildren<SmorzandoInstalledFlameMotion>(true))
            {
                if (sample)
                {
                    flameMotion.PreparePreview();
                    flameMotion.SampleAtTime(time);
                }
                else
                {
                    flameMotion.RestoreBasePose();
                }
            }
        }

        private static GameObject CreateCaptureFloor(Bounds bounds, out Material material)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Smorzando_InstalledIdle_CaptureFloor";
            floor.hideFlags = HideFlags.HideAndDontSave;
            floor.layer = CaptureLayer;
            floor.transform.position = new Vector3(bounds.center.x, bounds.min.y - 0.025f, bounds.center.z);
            floor.transform.localScale = new Vector3(
                Mathf.Max(bounds.size.x + 2f, 4f),
                0.05f,
                Mathf.Max(bounds.size.z + 2f, 4f));
            var collider = floor.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave,
                color = new Color(0.12f, 0.10f, 0.09f, 1f)
            };
            floor.GetComponent<MeshRenderer>().sharedMaterial = material;
            return floor;
        }

        private static void ConfigureCaptureLights(GameObject root)
        {
            foreach (var light in root.GetComponentsInChildren<Light>(true))
            {
                light.cullingMask = 1 << CaptureLayer;
                light.shadows = LightShadows.None;
            }
        }

        private static void SetCaptureOnly(GameObject root)
        {
            foreach (var target in root.GetComponentsInChildren<Transform>(true))
            {
                target.gameObject.layer = CaptureLayer;
                target.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        private static Bounds CalculateVisibleBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Smorzando installed idle capture has no visible renderers.");
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static void CapturePng(
            Camera camera,
            Vector3 cameraPosition,
            Vector3 target,
            Vector3 up,
            float orthographicSize,
            int width,
            int height,
            string path)
        {
            camera.transform.position = cameraPosition;
            camera.transform.rotation = Quaternion.LookRotation(target - cameraPosition, up);
            camera.orthographicSize = orthographicSize;
            SaveCurrentCameraPng(camera, path, width, height);
        }

        private static void SaveCurrentCameraPng(Camera camera, string path, int width, int height)
        {
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                try
                {
                    texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                    texture.Apply();
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
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static Camera FindPlayerCamera(Scene scene, Transform player)
        {
            var camera = player.GetComponentsInChildren<Camera>(true)
                .FirstOrDefault(candidate => candidate.CompareTag("MainCamera") && candidate.gameObject.activeInHierarchy) ??
                player.GetComponentsInChildren<Camera>(true).FirstOrDefault(candidate => candidate.gameObject.activeInHierarchy);
            if (camera == null || camera.gameObject.scene != scene)
            {
                throw new InvalidOperationException("Active Player camera is missing from CargoRunMvp.");
            }

            return camera;
        }

        private static Scene RequireOpenCargoRunScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != CargoRunScenePath)
            {
                throw new InvalidOperationException("CargoRunMvp must already be the active scene.");
            }

            return scene;
        }

        private static GameObject RequireRoot(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name) ??
                throw new InvalidOperationException(name + " root is missing from CargoRunMvp.");
        }

        private static bool IsFlameTransform(Transform target)
        {
            return target != null && target.GetComponentsInParent<Transform>(true)
                .Any(parent => parent.name == FlameObjectName);
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            var directory = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            var current = "Assets";
            foreach (var segment in directory.Split('/').Skip(1))
            {
                var next = current + "/" + segment;
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segment);
                }

                current = next;
            }
        }

        private static string ProjectAbsolutePath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }

        private static string FormatVector(Vector3 value)
        {
            return $"({value.x:0.######},{value.y:0.######},{value.z:0.######})";
        }

        private static float SmoothStep(float start, float end, float value)
        {
            var normalized = Mathf.Clamp01((value - start) / Mathf.Max(end - start, 0.000001f));
            return normalized * normalized * (3f - 2f * normalized);
        }

        private static IReadOnlyList<MeshTriangleComponent> FindConnectedTriangleComponents(
            Mesh mesh,
            float minimumZ = float.NegativeInfinity)
        {
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            var triangleCount = triangles.Length / 3;
            var active = new bool[triangleCount];
            var trianglesByPosition = new Dictionary<Vector3Int, List<int>>();
            for (var triangleIndex = 0; triangleIndex < triangleCount; triangleIndex++)
            {
                var includeTriangle = true;
                for (var corner = 0; corner < 3; corner++)
                {
                    if (vertices[triangles[triangleIndex * 3 + corner]].z < minimumZ)
                    {
                        includeTriangle = false;
                        break;
                    }
                }

                if (!includeTriangle)
                {
                    continue;
                }

                active[triangleIndex] = true;
                for (var corner = 0; corner < 3; corner++)
                {
                    var vertexIndex = triangles[triangleIndex * 3 + corner];
                    var key = QuantizePosition(vertices[vertexIndex]);
                    if (!trianglesByPosition.TryGetValue(key, out var connectedTriangles))
                    {
                        connectedTriangles = new List<int>();
                        trianglesByPosition.Add(key, connectedTriangles);
                    }

                    connectedTriangles.Add(triangleIndex);
                }
            }

            var visited = new bool[triangleCount];
            var result = new List<MeshTriangleComponent>();
            for (var start = 0; start < triangleCount; start++)
            {
                if (!active[start] || visited[start])
                {
                    continue;
                }

                var triangleIndices = new List<int>();
                var vertexIndices = new HashSet<int>();
                var queue = new Queue<int>();
                queue.Enqueue(start);
                visited[start] = true;
                while (queue.Count > 0)
                {
                    var triangleIndex = queue.Dequeue();
                    triangleIndices.Add(triangleIndex);
                    for (var corner = 0; corner < 3; corner++)
                    {
                        var vertexIndex = triangles[triangleIndex * 3 + corner];
                        vertexIndices.Add(vertexIndex);
                        var key = QuantizePosition(vertices[vertexIndex]);
                        foreach (var neighbor in trianglesByPosition[key])
                        {
                            if (visited[neighbor])
                            {
                                continue;
                            }

                            visited[neighbor] = true;
                            queue.Enqueue(neighbor);
                        }
                    }
                }

                var enumerator = vertexIndices.GetEnumerator();
                enumerator.MoveNext();
                var bounds = new Bounds(vertices[enumerator.Current], Vector3.zero);
                while (enumerator.MoveNext())
                {
                    bounds.Encapsulate(vertices[enumerator.Current]);
                }

                result.Add(new MeshTriangleComponent(triangleIndices, vertexIndices, bounds));
            }

            return result;
        }

        private static bool IsCentralModeledFlameTriangle(Vector3 centroid)
        {
            var radial = Vector2.Distance(
                new Vector2(centroid.x, centroid.y),
                new Vector2(0f, -0.000039f));
            return radial <= 0.00042f && centroid.z >= 0.00215f;
        }

        private static bool IsCentralModeledWickTriangle(Vector3 centroid)
        {
            var radial = Vector2.Distance(
                new Vector2(centroid.x, centroid.y),
                new Vector2(0f, -0.000039f));
            return radial <= 0.00042f && centroid.z >= 0.00172f && centroid.z < 0.00215f;
        }

        private static Mesh CreateExtractedMesh(
            Mesh source,
            Func<Vector3, bool> includeTriangle,
            float normalOffset,
            string name)
        {
            var sourceVertices = source.vertices;
            var sourceNormals = source.normals;
            var sourceTriangles = source.triangles;
            var vertexMap = new Dictionary<int, int>();
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();
            for (var triangle = 0; triangle < sourceTriangles.Length; triangle += 3)
            {
                var a = sourceTriangles[triangle];
                var b = sourceTriangles[triangle + 1];
                var c = sourceTriangles[triangle + 2];
                var centroid = (sourceVertices[a] + sourceVertices[b] + sourceVertices[c]) / 3f;
                if (!includeTriangle(centroid))
                {
                    continue;
                }

                AddExtractedVertex(a);
                AddExtractedVertex(b);
                AddExtractedVertex(c);
            }

            if (triangles.Count == 0)
            {
                throw new InvalidOperationException(name + " selection produced no triangles.");
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;

            void AddExtractedVertex(int sourceIndex)
            {
                if (!vertexMap.TryGetValue(sourceIndex, out var targetIndex))
                {
                    targetIndex = vertices.Count;
                    vertexMap.Add(sourceIndex, targetIndex);
                    var normal = sourceNormals.Length == sourceVertices.Length
                        ? sourceNormals[sourceIndex].normalized
                        : Vector3.up;
                    vertices.Add(sourceVertices[sourceIndex] + normal * normalOffset);
                    normals.Add(normal);
                }

                triangles.Add(targetIndex);
            }
        }

        private static void CaptureModeledFlameSegmentationPreview(
            GameObject installedAsset,
            Mesh modeledFlame,
            Mesh modeledWick,
            string outputPath)
        {
            var clone = UnityEngine.Object.Instantiate(installedAsset);
            clone.name = "Smorzando_ModeledFlameSegmentationPreview";
            clone.hideFlags = HideFlags.HideAndDontSave;
            var cameraObject = new GameObject("Smorzando_ModeledFlameSegmentationCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var lightObject = new GameObject("Smorzando_ModeledFlameSegmentationLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            Material coreMaterial = null;
            Material wickMaterial = null;
            GameObject coreObject = null;
            GameObject wickObject = null;
            try
            {
                clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(-90f, 0f, 0f));
                clone.transform.localScale = Vector3.one * 100f;
                var meshFilter = clone.GetComponentInChildren<MeshFilter>(true) ??
                    throw new InvalidOperationException("Smorzando diagnostic clone has no MeshFilter.");
                var bodyMaterial = AssetDatabase.LoadAssetAtPath<Material>(InstalledReferenceMaterialAssetPath);
                if (bodyMaterial != null)
                {
                    meshFilter.GetComponent<MeshRenderer>().sharedMaterial = bodyMaterial;
                }

                var unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ??
                    throw new InvalidOperationException("URP Unlit shader is missing.");
                coreMaterial = new Material(unlitShader)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    color = new Color(1f, 0.48f, 0.04f, 1f)
                };
                coreMaterial.SetColor("_BaseColor", new Color(1f, 0.48f, 0.04f, 1f));
                wickMaterial = new Material(unlitShader)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    color = new Color(0.015f, 0.012f, 0.01f, 1f)
                };
                wickMaterial.SetColor("_BaseColor", new Color(0.015f, 0.012f, 0.01f, 1f));
                coreObject = CreateDiagnosticOverlay(meshFilter.transform, "ModeledFlame_Core", modeledFlame, coreMaterial);
                wickObject = CreateDiagnosticOverlay(meshFilter.transform, "ModeledFlame_Wick", modeledWick, wickMaterial);
                SetCaptureOnly(clone);

                var camera = cameraObject.AddComponent<Camera>();
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.05f, 0.047f, 1f);
                camera.orthographic = true;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 50f;
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 3.5f;
                light.color = new Color(1f, 0.82f, 0.7f, 1f);
                light.cullingMask = 1 << CaptureLayer;
                light.shadows = LightShadows.None;
                lightObject.transform.rotation = Quaternion.Euler(35f, -35f, 0f);
                var overlayBounds = coreObject.GetComponent<Renderer>().bounds;
                overlayBounds.Encapsulate(wickObject.GetComponent<Renderer>().bounds);
                var target = overlayBounds.center - Vector3.up * 0.015f;
                var viewDirection = (Vector3.back + Vector3.right * 0.55f).normalized;
                CapturePng(
                    camera,
                    target + viewDirection * 10f,
                    target,
                    Vector3.up,
                    Mathf.Max(overlayBounds.extents.y * 3.4f, 0.14f),
                    900,
                    900,
                    outputPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(coreObject);
                UnityEngine.Object.DestroyImmediate(wickObject);
                UnityEngine.Object.DestroyImmediate(coreMaterial);
                UnityEngine.Object.DestroyImmediate(wickMaterial);
                UnityEngine.Object.DestroyImmediate(clone);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private static GameObject CreateDiagnosticOverlay(
            Transform parent,
            string name,
            Mesh mesh,
            Material material)
        {
            var overlay = new GameObject(name)
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = CaptureLayer
            };
            overlay.transform.SetParent(parent, false);
            overlay.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = overlay.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return overlay;
        }

        private static Vector3Int QuantizePosition(Vector3 position)
        {
            const float scale = 10000000f;
            return new Vector3Int(
                Mathf.RoundToInt(position.x * scale),
                Mathf.RoundToInt(position.y * scale),
                Mathf.RoundToInt(position.z * scale));
        }

        private sealed class MeshTriangleComponent
        {
            public MeshTriangleComponent(
                List<int> triangleIndices,
                HashSet<int> vertexIndices,
                Bounds bounds)
            {
                TriangleIndices = triangleIndices;
                VertexIndices = vertexIndices;
                Bounds = bounds;
            }

            public List<int> TriangleIndices { get; }

            public HashSet<int> VertexIndices { get; }

            public Bounds Bounds { get; }
        }

        private sealed class TransformSnapshot
        {
            private readonly Transform target;
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            public TransformSnapshot(Transform target)
            {
                this.target = target;
                position = target.position;
                rotation = target.rotation;
                scale = target.localScale;
            }

            public void AssertUnchanged()
            {
                if (target == null ||
                    Vector3.Distance(target.position, position) > 0.00001f ||
                    Quaternion.Angle(target.rotation, rotation) > 0.0001f ||
                    Vector3.Distance(target.localScale, scale) > 0.00001f)
                {
                    throw new InvalidOperationException("Existing scene Transform changed outside the approved idle attachment scope.");
                }
            }
        }
    }
}
