using System;
using System.Collections.Generic;
using System.Globalization;
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
    internal static class SmorzandoInstalledTransformApplyAndReview
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PersonModelAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Models/Smorzando_Person.fbx";
        private const string BakePoseMeshDataPath =
            "Assets/_Project/Art/Enemies/Smorzando/Models/Derived/Smorzando_TransformBakePose.meshdata";
        private const string SculptSurfaceMeshDataPath =
            "Assets/_Project/Art/Enemies/Smorzando/Models/Derived/Smorzando_TransformSurface.meshdata";
        private const string SculptSurfaceAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Models/Derived/Smorzando_TransformSurface.asset";
        private const string SmorzandoRootName = "Approved Smorzando Enemy Placement";
        private const string TransformSlotName = "Smorzando_Installed_03";
        private const string InstalledModelName = "Smorzando_Installed_Model";
        private const string ReferencePersonSlotName = "Smorzando_Person_01";
        private const string ReferencePersonModelName = "Smorzando_Person_Model";
        private const string PersonTargetName = "Smorzando_Transform_PersonTarget";
        private const string SculptSurfaceTargetName = "Smorzando_Transform_SculptSurfaceTarget";
        private const string FlameObjectName = "Smorzando_Installed_Flame";
        private const string ValidationRelativeFolder =
            "docs/validation/smorzando_installed_transform_2026-07-17";
        private const string CaptureRelativeFolder =
            "docs/validation/smorzando_installed_transform_2026-07-17/automated_visual_capture";
        private const int CaptureLayer = 30;
        private const float DurationSeconds = 3f;
        private const float HumanSizedWaxBlobReadySeconds = 1.35f;
        private const float FinalHoldSeconds = 1f;

        private static readonly float[] CaptureTimes =
            { 0f, 0.35f, 0.65f, 0.8f, 1f, 1.1f, 1.2f, 1.3f, 1.34f, 1.35f, 1.55f, 1.8f, 2.1f, 2.4f, 2.65f, 2.85f, 2.95f, 2.99f, 3f, 3.5f, 3.95f, 4.05f, 4.35f };
        private static readonly string[] CaptureLabels =
            { "000", "035", "065", "080", "100", "110", "120", "130", "134", "135", "155", "180", "210", "240", "265", "285", "295", "299", "300", "350", "395", "405", "435" };

        [MenuItem("Bellerophon/Enemies/Smorzando/Export Transform Bake Mesh")]
        public static void ExportSmorzandoTransformBakeMesh()
        {
            var scene = RequireOpenCargoRunScene();
            var root = RequireRoot(scene, SmorzandoRootName);
            var transformTarget = root.transform.Find(TransformSlotName + "/" + PersonTargetName) ??
                throw new InvalidOperationException("Smorzando transform person target is missing.");
            var personRenderer = transformTarget.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                throw new InvalidOperationException("Smorzando transform person target has no SkinnedMeshRenderer.");
            var bakedMesh = new Mesh
            {
                name = "Smorzando_TransformBakePose",
                hideFlags = HideFlags.HideAndDontSave
            };
            try
            {
                var rendererWasEnabled = personRenderer.enabled;
                personRenderer.enabled = true;
                personRenderer.BakeMesh(bakedMesh, false);
                personRenderer.enabled = rendererWasEnabled;
                WriteMeshData(ProjectAbsolutePath(BakePoseMeshDataPath), bakedMesh);

                var folder = ProjectAbsolutePath(ValidationRelativeFolder);
                Directory.CreateDirectory(folder);
                var calculatedBounds = CalculateBounds(bakedMesh.vertices);
                var reportLines = new List<string>
                {
                    "Scene=" + CargoRunScenePath,
                    "Target=" + TransformSlotName + "/" + PersonTargetName,
                    "Renderer=" + personRenderer.name,
                    "Output=" + BakePoseMeshDataPath,
                    "VertexCount=" + bakedMesh.vertexCount,
                    "TriangleIndexCount=" + CountTriangleIndices(bakedMesh),
                    "CalculatedBoundsCenter=" + FormatVector(calculatedBounds.center),
                    "CalculatedBoundsSize=" + FormatVector(calculatedBounds.size),
                    "RendererLocalPosition=" + FormatVector(personRenderer.transform.localPosition),
                    "RendererLocalRotation=" + FormatQuaternion(personRenderer.transform.localRotation),
                    "RendererLocalScale=" + FormatVector(personRenderer.transform.localScale)
                };
                for (var subMesh = 0; subMesh < bakedMesh.subMeshCount; subMesh++)
                {
                    var material = subMesh < personRenderer.sharedMaterials.Length
                        ? personRenderer.sharedMaterials[subMesh]
                        : null;
                    var materialColor = material != null && material.HasProperty("_BaseColor")
                        ? material.GetColor("_BaseColor")
                        : Color.white;
                    reportLines.Add(
                        $"SubMesh{subMesh}=Indices:{bakedMesh.GetIndexCount(subMesh)}," +
                        $"Material:{material?.name ?? "None"},Shader:{material?.shader?.name ?? "None"}," +
                        $"Color:({materialColor.r:0.######},{materialColor.g:0.######}," +
                        $"{materialColor.b:0.######},{materialColor.a:0.######})," +
                        $"RenderQueue:{material?.renderQueue ?? -1}");
                }
                reportLines.Add("SelectionCleared=True");
                File.WriteAllLines(
                    Path.Combine(folder, "Smorzando_TransformBakeExport.txt"),
                    reportLines);
                Selection.activeObject = null;
                Debug.Log(
                    $"SmorzandoTransformBakeMeshExported Vertices={bakedMesh.vertexCount}, " +
                    $"TriangleIndices={CountTriangleIndices(bakedMesh)}, SelectionCleared=True");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bakedMesh);
                Selection.activeObject = null;
            }
        }

        [MenuItem("Bellerophon/Enemies/Smorzando/Apply Installed Transform")]
        public static void ApplySmorzandoInstalledTransform()
        {
            var scene = RequireOpenCargoRunScene();
            var root = RequireRoot(scene, SmorzandoRootName);
            var transformSlot = root.transform.Find(TransformSlotName) ??
                throw new InvalidOperationException("Third installed Smorzando slot is missing.");
            var installedModel = transformSlot.Find(InstalledModelName) ??
                throw new InvalidOperationException("Third installed Smorzando model is missing.");
            var referenceModel = root.transform.Find(
                ReferencePersonSlotName + "/" + ReferencePersonModelName) ??
                throw new InvalidOperationException("First Smorzando person reference model is missing.");
            var referenceRenderer = referenceModel.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                throw new InvalidOperationException("Smorzando person reference has no SkinnedMeshRenderer.");
            var personAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PersonModelAssetPath) ??
                throw new InvalidOperationException("Smorzando person FBX has not been imported.");
            var existingTarget = transformSlot.Find(PersonTargetName);
            var existingSculptSurfaceTarget = transformSlot.Find(SculptSurfaceTargetName);
            var flameRoot = installedModel.Find(FlameObjectName) ??
                throw new InvalidOperationException("Third installed Smorzando flame is missing.");
            var flameLight = flameRoot.GetComponent<Light>() ??
                throw new InvalidOperationException("Third installed Smorzando flame light is missing.");
            var flameMotion = installedModel.GetComponent<SmorzandoInstalledFlameMotion>() ??
                throw new InvalidOperationException("Third installed Smorzando flame motion is missing.");
            var preservedRootTransforms = scene.GetRootGameObjects()
                .Select(sceneRoot => new TransformSnapshot(sceneRoot.transform))
                .ToArray();
            var preservedSmorzandoTransforms = root.GetComponentsInChildren<Transform>(true)
                .Where(target => target != flameRoot)
                .Where(target => existingTarget == null || (target != existingTarget && !target.IsChildOf(existingTarget)))
                .Where(target => existingSculptSurfaceTarget == null ||
                    (target != existingSculptSurfaceTarget && !target.IsChildOf(existingSculptSurfaceTarget)))
                .Select(target => new TransformSnapshot(target))
                .ToArray();

            var existingMotion = installedModel.GetComponent<SmorzandoInstalledTransformMotion>();
            existingMotion?.RestoreInitialState();
            RestoreThirdInstalledFlameBaseline(flameRoot, flameLight, flameMotion);
            if (existingTarget != null)
            {
                UnityEngine.Object.DestroyImmediate(existingTarget.gameObject);
            }
            if (existingSculptSurfaceTarget != null)
            {
                UnityEngine.Object.DestroyImmediate(existingSculptSurfaceTarget.gameObject);
            }
            var sculptSurfaceMesh = ImportSculptSurfaceMeshAsset();

            var targetObject = PrefabUtility.InstantiatePrefab(personAsset, scene) as GameObject ??
                throw new InvalidOperationException("Smorzando transform person target could not be instantiated.");
            targetObject.name = PersonTargetName;
            targetObject.transform.SetParent(transformSlot, false);
            targetObject.transform.localPosition = referenceModel.localPosition;
            targetObject.transform.localRotation = referenceModel.localRotation;
            targetObject.transform.localScale = referenceModel.localScale;
            foreach (var animator in targetObject.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
            }
            foreach (var helperCamera in targetObject.GetComponentsInChildren<Camera>(true))
            {
                helperCamera.enabled = false;
            }
            foreach (var helperLight in targetObject.GetComponentsInChildren<Light>(true))
            {
                helperLight.enabled = false;
            }

            var personRenderer = targetObject.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                throw new InvalidOperationException("Smorzando transform person target has no SkinnedMeshRenderer.");
            personRenderer.sharedMaterials = referenceRenderer.sharedMaterials;
            personRenderer.enabled = false;
            personRenderer.updateWhenOffscreen = true;

            var sculptSurfaceObject = new GameObject(SculptSurfaceTargetName);
            sculptSurfaceObject.transform.SetParent(personRenderer.transform.parent, false);
            sculptSurfaceObject.transform.localPosition = personRenderer.transform.localPosition;
            sculptSurfaceObject.transform.localRotation = personRenderer.transform.localRotation;
            sculptSurfaceObject.transform.localScale = personRenderer.transform.localScale;
            var sculptSurfaceFilter = sculptSurfaceObject.AddComponent<MeshFilter>();
            sculptSurfaceFilter.sharedMesh = sculptSurfaceMesh;
            var sculptSurfaceRenderer = sculptSurfaceObject.AddComponent<MeshRenderer>();
            sculptSurfaceRenderer.enabled = false;

            var comparisonMesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
            var personWasEnabled = personRenderer.enabled;
            personRenderer.enabled = true;
            personRenderer.BakeMesh(comparisonMesh, false);
            personRenderer.enabled = personWasEnabled;
            var comparisonNormals = comparisonMesh.normals;
            if (comparisonMesh.vertexCount != sculptSurfaceMesh.vertexCount ||
                comparisonNormals.Length != sculptSurfaceMesh.vertexCount)
            {
                UnityEngine.Object.DestroyImmediate(comparisonMesh);
                throw new InvalidOperationException(
                    "Smorzando exact-detail sculpt surface no longer matches the baked person vertex layout.");
            }
            sculptSurfaceMesh.normals = comparisonNormals;
            EditorUtility.SetDirty(sculptSurfaceMesh);
            AssetDatabase.SaveAssets();
            var finalPersonBounds = TransformBounds(
                CalculateBounds(comparisonMesh.vertices),
                personRenderer.transform.localToWorldMatrix);
            UnityEngine.Object.DestroyImmediate(comparisonMesh);

            var sculptSurfaceBounds = TransformBounds(
                sculptSurfaceFilter.sharedMesh.bounds,
                sculptSurfaceFilter.transform.localToWorldMatrix);
            var sculptAlignmentScale = Vector3.one;

            var sculptHeightRatio = sculptSurfaceBounds.size.y /
                Mathf.Max(finalPersonBounds.size.y, 0.000001f);
            var sculptWidthRatio = sculptSurfaceBounds.size.x /
                Mathf.Max(finalPersonBounds.size.x, 0.000001f);
            var sculptDepthRatio = sculptSurfaceBounds.size.z /
                Mathf.Max(finalPersonBounds.size.z, 0.000001f);
            var sculptCenterOffset = Vector3.Distance(sculptSurfaceBounds.center, finalPersonBounds.center);
            if (sculptHeightRatio < 0.99f || sculptHeightRatio > 1.01f ||
                sculptWidthRatio < 0.90f || sculptWidthRatio > 1.10f ||
                sculptDepthRatio < 0.90f || sculptDepthRatio > 1.10f ||
                sculptCenterOffset > finalPersonBounds.size.y * 0.04f)
            {
                throw new InvalidOperationException(
                    $"Connected sculpt surface is not aligned to the final person. " +
                    $"HeightRatio={sculptHeightRatio:0.######}, WidthRatio={sculptWidthRatio:0.######}, " +
                    $"DepthRatio={sculptDepthRatio:0.######}, CenterOffset={sculptCenterOffset:0.######}");
            }

            var installedMeshFilter = installedModel.GetComponent<MeshFilter>() ??
                throw new InvalidOperationException("Third installed Smorzando has no MeshFilter.");
            var installedRenderer = installedModel.GetComponent<Renderer>() ??
                throw new InvalidOperationException("Third installed Smorzando has no Renderer.");
            var motion = existingMotion != null
                ? existingMotion
                : Undo.AddComponent<SmorzandoInstalledTransformMotion>(installedModel.gameObject);
            motion.Configure(
                installedMeshFilter,
                installedRenderer,
                flameRoot,
                flameLight,
                flameMotion,
                personRenderer,
                sculptSurfaceFilter,
                sculptSurfaceRenderer,
                HumanSizedWaxBlobReadySeconds,
                true,
                FinalHoldSeconds);

            foreach (var snapshot in preservedRootTransforms)
            {
                snapshot.AssertUnchanged();
            }
            foreach (var snapshot in preservedSmorzandoTransforms)
            {
                snapshot.AssertUnchanged();
            }

            EditorUtility.SetDirty(targetObject);
            EditorUtility.SetDirty(personRenderer);
            EditorUtility.SetDirty(sculptSurfaceObject);
            EditorUtility.SetDirty(sculptSurfaceRenderer);
            EditorUtility.SetDirty(motion);
            EditorUtility.SetDirty(installedModel);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Selection.activeObject = null;

            var folder = ProjectAbsolutePath(ValidationRelativeFolder);
            Directory.CreateDirectory(folder);
            File.WriteAllLines(
                Path.Combine(folder, "Smorzando_InstalledTransformApply.txt"),
                new[]
                {
                    "Scene=" + CargoRunScenePath,
                    "TargetSlot=" + TransformSlotName,
                    "InstalledModel=" + InstalledModelName,
                    "PersonTarget=" + PersonTargetName,
                    "SculptSurfaceTarget=" + SculptSurfaceTargetName,
                    "SculptSurfaceAsset=" + SculptSurfaceAssetPath,
                    "SculptSurfaceSource=" + SculptSurfaceMeshDataPath,
                    "SculptSurfacePoseSource=" + BakePoseMeshDataPath,
                    "PersonReference=" + ReferencePersonSlotName + "/" + ReferencePersonModelName,
                    "DurationSeconds=3",
                    "FlameOutSeconds=0.5",
                    "EmberEndSeconds=0.65",
                    "WaxGatherToHumanSizedBlobSeconds=0.5-1.35",
                    "HumanSizedWaxBlobReadySeconds=1.35",
                    "InstalledWaxClayCompression=True",
                    "InstalledWaxBodyCompressionRatio=0.56",
                    "InstalledWaxPoolCompressionRatio=0.44",
                    "InstalledWaxBottomAnchored=True",
                    "InstalledWaxCompressionDrivenVerticalStretch=True",
                    "InstalledWaxHandoffCompressionSeconds=1.17-1.35",
                    "InstalledWaxHandoffFinalRadiusRatio=0.08",
                    "WaxBlobToPersonMorphSeconds=1.35-3.0",
                    "WaxBlobSilhouetteWriggle=True",
                    "RegionalZombieSculpting=True",
                    "FullHeightPersonMorph=True",
                    "SingleVisibleTransformSurface=True",
                    "ConnectedSculptSurface=True",
                    "ConnectedSculptSurfaceVertexCount=" + sculptSurfaceFilter.sharedMesh.vertexCount,
                    "ConnectedSculptSurfaceAlignmentScale=" + FormatVector(sculptAlignmentScale),
                    "ConnectedSculptSurfaceHeightRatio=" + sculptHeightRatio.ToString("0.######"),
                    "ConnectedSculptSurfaceWidthRatio=" + sculptWidthRatio.ToString("0.######"),
                    "ConnectedSculptSurfaceDepthRatio=" + sculptDepthRatio.ToString("0.######"),
                    "ConnectedSculptSurfaceCenterOffset=" + sculptCenterOffset.ToString("0.######"),
                    "CoherentVerticalSculptDelay=True",
                    "TransformSurfaceDoubleSided=True",
                    "StrongRadialCollapse=False",
                    "SeparateWaxBlobRenderer=False",
                    "SameVertexSculpting=True",
                    "ScaleDisappearTransition=False",
                    "Loop=True",
                    "FinalHoldSeconds=1",
                    "CycleDurationSeconds=4",
                    "FlameBaselineRestored=True",
                    "FlameRootLocalScale=" + FormatVector(flameRoot.localScale),
                    "FlameLightEnabled=" + flameLight.enabled,
                    "FlameLightIntensity=" + flameLight.intensity,
                    "PersonTargetLocalPosition=" + FormatVector(targetObject.transform.localPosition),
                    "PersonTargetLocalRotation=" + FormatQuaternion(targetObject.transform.localRotation),
                    "PersonTargetLocalScale=" + FormatVector(targetObject.transform.localScale),
                    "ReferenceLocalPosition=" + FormatVector(referenceModel.localPosition),
                    "ReferenceLocalRotation=" + FormatQuaternion(referenceModel.localRotation),
                    "ReferenceLocalScale=" + FormatVector(referenceModel.localScale),
                    "OriginalFbxChanged=False",
                    "OtherTransformsChanged=False",
                    "SelectionCleared=True"
                });
            Debug.Log(
                "SmorzandoInstalledTransformApplied Slot=03, Duration=3s, Loop=True, FinalHold=1s, Cycle=4s, " +
                "FlameOut=0.5s, EmberEnd=0.65s, HumanSizedWaxBlob=1.35s, BlobToPersonMorph=1.35-3.0s, " +
                "InstalledWaxClayCompression=True, CompressionDrivenVerticalStretch=True, BottomAnchored=True, " +
                "SingleVisibleTransformSurface=True, ConnectedSculptSurface=True, SameVertexSculpting=True, " +
                "ReferencePoseMatched=True, OtherTransformsChanged=False, SelectionCleared=True");
        }

        private static void RestoreThirdInstalledFlameBaseline(
            Transform flameRoot,
            Light flameLight,
            SmorzandoInstalledFlameMotion flameMotion)
        {
            // Slot 03 repeats from this authored flame baseline after each four-second review cycle.
            flameRoot.localScale = Vector3.one;
            foreach (var flameRenderer in flameRoot.GetComponentsInChildren<Renderer>(true))
            {
                flameRenderer.enabled = true;
            }

            flameLight.enabled = true;
            flameLight.color = new Color(1f, 0.42f, 0.14f, 1f);
            flameLight.intensity = 0.45f;
            flameLight.range = 2f;
            flameLight.shadows = LightShadows.None;
            flameMotion.enabled = true;
        }

        [MenuItem("Bellerophon/Enemies/Smorzando/Capture Installed Transform Frames")]
        public static void CaptureSmorzandoInstalledTransformFrames()
        {
            var scene = RequireOpenCargoRunScene();
            var sceneWasDirty = scene.isDirty;
            var root = RequireRoot(scene, SmorzandoRootName);
            var transformSlot = root.transform.Find(TransformSlotName) ??
                throw new InvalidOperationException("Third installed Smorzando slot is missing.");
            var referenceSlot = root.transform.Find(ReferencePersonSlotName) ??
                throw new InvalidOperationException("First Smorzando person reference slot is missing.");
            var outputFolder = ProjectAbsolutePath(CaptureRelativeFolder);
            Directory.CreateDirectory(outputFolder);

            var cameraObject = new GameObject("Smorzando_Transform_CaptureCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var lightObject = new GameObject("Smorzando_Transform_CaptureLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            GameObject transformClone = null;
            GameObject referenceClone = null;
            GameObject floor = null;
            Material floorMaterial = null;
            SmorzandoInstalledTransformMotion motion = null;
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.018f, 0.014f, 0.012f, 1f);
                camera.orthographic = true;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 100f;
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 3.4f;
                light.color = new Color(1f, 0.82f, 0.68f, 1f);
                light.cullingMask = 1 << CaptureLayer;
                light.shadows = LightShadows.None;
                lightObject.transform.rotation = Quaternion.Euler(38f, -28f, 0f);

                transformClone = UnityEngine.Object.Instantiate(transformSlot.gameObject);
                transformClone.name = "Smorzando_InstalledTransform_CaptureClone";
                SetCaptureOnly(transformClone);
                ConfigureCaptureLights(transformClone);
                motion = transformClone.GetComponentInChildren<SmorzandoInstalledTransformMotion>(true) ??
                    throw new InvalidOperationException("Smorzando transform motion is missing on capture clone.");
                motion.PreparePreview();
                motion.SampleAtTime(0f);
                var framingBounds = CalculateVisibleBounds(transformClone.transform);
                motion.SampleAtTime(DurationSeconds);
                framingBounds.Encapsulate(CalculateVisibleBounds(transformClone.transform));
                motion.SampleAtTime(0f);
                floor = CreateCaptureFloor(framingBounds, out floorMaterial);

                var target = framingBounds.center + Vector3.up * 0.05f;
                var orthographicSize = Mathf.Max(framingBounds.extents.y + 0.3f, framingBounds.extents.x + 0.3f);
                for (var index = 0; index < CaptureTimes.Length; index++)
                {
                    motion.SampleAtTime(CaptureTimes[index]);
                    CapturePng(
                        camera,
                        target + Vector3.back * 35f,
                        target,
                        Vector3.up,
                        orthographicSize,
                        720,
                        720,
                        Path.Combine(outputFolder, $"Smorzando_Transform_Front_{CaptureLabels[index]}.png"));
                    var obliqueDirection = (Vector3.back + Vector3.right * 0.48f).normalized;
                    CapturePng(
                        camera,
                        target + obliqueDirection * 35f,
                        target,
                        Vector3.up,
                        orthographicSize,
                        720,
                        720,
                        Path.Combine(outputFolder, $"Smorzando_Transform_Oblique_{CaptureLabels[index]}.png"));
                }

                referenceClone = UnityEngine.Object.Instantiate(referenceSlot.gameObject);
                referenceClone.name = "Smorzando_Transform_ReferencePersonClone";
                SetCaptureOnly(referenceClone);
                motion.SampleAtTime(DurationSeconds);
                var comparisonRoot = new Bounds(CalculateVisibleBounds(transformClone.transform).center, Vector3.zero);
                comparisonRoot.Encapsulate(CalculateVisibleBounds(transformClone.transform));
                comparisonRoot.Encapsulate(CalculateVisibleBounds(referenceClone.transform));
                CapturePng(
                    camera,
                    comparisonRoot.center + Vector3.back * 40f,
                    comparisonRoot.center,
                    Vector3.up,
                    Mathf.Max(comparisonRoot.extents.y + 0.3f, comparisonRoot.extents.x / (16f / 9f) + 0.3f),
                    1280,
                    720,
                    Path.Combine(outputFolder, "Smorzando_Transform_FinalVsReference.png"));

                File.WriteAllLines(
                    Path.Combine(outputFolder, "Smorzando_InstalledTransform_CaptureManifest.txt"),
                    new[]
                    {
                        "DurationSeconds=3",
                        "CaptureTimesSeconds=0|0.35|0.65|0.8|1.0|1.1|1.2|1.3|1.34|1.35|1.55|1.8|2.1|2.4|2.65|2.85|2.95|2.99|3.0|3.5|3.95|4.05|4.35",
                        "Views=Front|Oblique|FinalVsReference",
                        "TargetSlot=Smorzando_Installed_03",
                        "ReferencePerson=Smorzando_Person_01",
                        "Loop=True",
                        "FinalHoldSeconds=1",
                        "CycleDurationSeconds=4",
                        "SceneViewFocused=False",
                        "SceneSaved=False",
                        "SelectionCleared=True"
                    });
                Selection.activeObject = null;
                Debug.Log(
                    $"SmorzandoInstalledTransformFramesCaptured Folder={outputFolder}, " +
                    "Times=0|0.35|0.65|0.8|1.0|1.1|1.2|1.3|1.34|1.35|1.55|1.8|2.1|2.4|2.65|2.85|2.95|2.99|3.0|3.5|3.95|4.05|4.35, " +
                    "Views=Front|Oblique|FinalVsReference, " +
                    "SceneViewFocused=False, SceneSaved=False, SelectionCleared=True");
            }
            finally
            {
                motion?.RestoreInitialState();
                UnityEngine.Object.DestroyImmediate(referenceClone);
                UnityEngine.Object.DestroyImmediate(transformClone);
                UnityEngine.Object.DestroyImmediate(floor);
                UnityEngine.Object.DestroyImmediate(floorMaterial);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
                Selection.activeObject = null;
                if (scene.isDirty != sceneWasDirty)
                {
                    throw new InvalidOperationException("Smorzando transform capture changed the scene dirty state.");
                }
            }
        }

        private static GameObject CreateCaptureFloor(Bounds bounds, out Material material)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Smorzando_Transform_CaptureFloor";
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
                color = new Color(0.11f, 0.085f, 0.07f, 1f)
            };
            floor.GetComponent<MeshRenderer>().sharedMaterial = material;
            return floor;
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
            var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                try
                {
                    texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    texture.Apply();
                    Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ProjectAbsolutePath(CaptureRelativeFolder));
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

        private static Bounds CalculateVisibleBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Smorzando transform capture has no visible renderers.");
            }
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }
            return bounds;
        }

        private static void SetCaptureOnly(GameObject root)
        {
            foreach (var target in root.GetComponentsInChildren<Transform>(true))
            {
                target.gameObject.layer = CaptureLayer;
                target.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        private static void ConfigureCaptureLights(GameObject root)
        {
            foreach (var light in root.GetComponentsInChildren<Light>(true))
            {
                light.cullingMask = 1 << CaptureLayer;
                light.shadows = LightShadows.None;
            }
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

        private static string ProjectAbsolutePath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }

        private static string FormatVector(Vector3 value)
        {
            return $"({value.x:0.######},{value.y:0.######},{value.z:0.######})";
        }

        private static string FormatQuaternion(Quaternion value)
        {
            return $"({value.x:0.######},{value.y:0.######},{value.z:0.######},{value.w:0.######})";
        }

        private static Bounds TransformBounds(Bounds localBounds, Matrix4x4 localToWorld)
        {
            var worldCenter = localToWorld.MultiplyPoint3x4(localBounds.center);
            var axisX = localToWorld.MultiplyVector(new Vector3(localBounds.extents.x, 0f, 0f));
            var axisY = localToWorld.MultiplyVector(new Vector3(0f, localBounds.extents.y, 0f));
            var axisZ = localToWorld.MultiplyVector(new Vector3(0f, 0f, localBounds.extents.z));
            var worldExtents = new Vector3(
                Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
                Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
                Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z));
            return new Bounds(worldCenter, worldExtents * 2f);
        }

        private static Mesh ImportSculptSurfaceMeshAsset()
        {
            var sourcePath = ProjectAbsolutePath(SculptSurfaceMeshDataPath);
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("Smorzando connected sculpt mesh data is missing.", sourcePath);
            }

            ReadMeshData(sourcePath, out var vertices, out var triangles);
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(SculptSurfaceAssetPath);
            if (mesh == null)
            {
                mesh = new Mesh { name = "Smorzando_TransformSurface" };
                AssetDatabase.CreateAsset(mesh, SculptSurfaceAssetPath);
            }
            else
            {
                mesh.Clear(false);
            }

            mesh.indexFormat = vertices.Count > ushort.MaxValue
                ? IndexFormat.UInt32
                : IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            EditorUtility.SetDirty(mesh);
            AssetDatabase.SaveAssets();
            return mesh;
        }

        private static void WriteMeshData(string path, Mesh mesh)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var culture = CultureInfo.InvariantCulture;
            using var writer = new StreamWriter(path, false, new System.Text.UTF8Encoding(false));
            writer.WriteLine("SMORZANDO_MESH_DATA_V1");
            foreach (var vertex in mesh.vertices)
            {
                writer.WriteLine(string.Format(
                    culture,
                    "v {0:R} {1:R} {2:R}",
                    vertex.x,
                    vertex.y,
                    vertex.z));
            }

            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                var indices = mesh.GetTriangles(subMesh);
                for (var index = 0; index < indices.Length; index += 3)
                {
                    writer.WriteLine(
                        $"t {subMesh} {indices[index]} {indices[index + 1]} {indices[index + 2]}");
                }
            }
        }

        private static void ReadMeshData(string path, out List<Vector3> vertices, out List<int> triangles)
        {
            vertices = new List<Vector3>();
            triangles = new List<int>();
            var culture = CultureInfo.InvariantCulture;
            foreach (var rawLine in File.ReadLines(path))
            {
                var parts = rawLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 4 && parts[0] == "v")
                {
                    vertices.Add(new Vector3(
                        float.Parse(parts[1], culture),
                        float.Parse(parts[2], culture),
                        float.Parse(parts[3], culture)));
                }
                else if (parts.Length == 4 && parts[0] == "t")
                {
                    triangles.Add(int.Parse(parts[1], culture));
                    triangles.Add(int.Parse(parts[2], culture));
                    triangles.Add(int.Parse(parts[3], culture));
                }
            }

            if (vertices.Count == 0 || triangles.Count == 0)
            {
                throw new InvalidDataException("Smorzando sculpt mesh data contains no usable geometry.");
            }
        }

        private static int CountTriangleIndices(Mesh mesh)
        {
            var count = 0;
            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                count += (int)mesh.GetIndexCount(subMesh);
            }
            return count;
        }

        private static Bounds CalculateBounds(IReadOnlyList<Vector3> vertices)
        {
            if (vertices.Count == 0)
            {
                return new Bounds();
            }

            var bounds = new Bounds(vertices[0], Vector3.zero);
            for (var index = 1; index < vertices.Count; index++)
            {
                bounds.Encapsulate(vertices[index]);
            }
            return bounds;
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
                if (target == null || target.position != position || target.rotation != rotation ||
                    target.localScale != scale)
                {
                    throw new InvalidOperationException("Existing Transform changed while applying Smorzando transform motion: " + target?.name);
                }
            }
        }
    }
}
