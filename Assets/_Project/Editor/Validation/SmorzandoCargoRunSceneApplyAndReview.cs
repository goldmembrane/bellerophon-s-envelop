using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.SmorzandoCargoRunScene
{
    internal static class SmorzandoCargoRunSceneApplyAndReview
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string InstalledModelAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Models/Smorzando_Installed.fbx";
        private const string PersonModelAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Models/Smorzando_Person.fbx";
        private const string LongaRootName = "Approved Longa Arma Enemy Placement";
        private const string TergoRootName = "Approved Tergo Enemy Placement";
        private const string GraveRootName = "Approved Grave Enemy Placement";
        private const string SmorzandoRootName = "Approved Smorzando Enemy Placement";
        private const string PlayerRootName = "Player";
        private const string GraveStaticSlotName = "Grave_00_Static_Review";
        private const string GraveModelName = "Grave_Model";
        private const string InstalledSlotPrefix = "Smorzando_Installed_";
        private const string PersonSlotPrefix = "Smorzando_Person_";
        private const string InstalledModelName = "Smorzando_Installed_Model";
        private const string PersonModelName = "Smorzando_Person_Model";
        private const string CaptureRelativeFolder =
            "docs/validation/smorzando_scene_placement_2026-07-17/automated_visual_capture";
        private const string PlayerStartCaptureRelativeFolder =
            "docs/validation/smorzando_scene_placement_2026-07-17/player_start_view";
        private const string ReferenceColorRelativeFolder =
            "docs/validation/smorzando_reference_colors_2026-07-17";
        private const string InstalledReferenceImageRelativePath = "image/smorzando(스모르찬도).png";
        private const string PersonReferenceImageRelativePath = "image/smorzando-person.png";
        private const string PersonWaxAlbedoAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Textures/Smorzando_Person_Wax_Albedo.png";
        private const string InstalledReferenceMaterialAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Materials/Smorzando_Installed_Reference.mat";
        private const string PersonReferenceMaterialAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Materials/Smorzando_Person_Reference.mat";
        private const string ReferenceColorCaptureRelativeFolder =
            "docs/validation/smorzando_reference_colors_2026-07-17/automated_visual_capture";
        private const string InstalledIdleValidationRelativeFolder =
            "docs/validation/smorzando_installed_idle_2026-07-17";
        private const int InstalledCount = 3;
        private const int PersonCount = 5;
        private const int CaptureLayer = 30;
        // A small visible clearance makes the approved no-overlap requirement unambiguous in top/front captures.
        private const float HorizontalClearance = 0.5f;
        private const float GroundTolerance = 0.02f;
        // Extra framing distance keeps the outermost models clear of the runtime camera edges.
        private const float PlayerViewPadding = 1.25f;
        // The installed source FBX arrives at one-hundredth scale with its broad wax plane vertical.
        private const float InstalledImportScaleCorrection = 100f;
        private static readonly Quaternion InstalledImportAxisCorrection = Quaternion.Euler(-90f, 0f, 0f);

        [MenuItem("Bellerophon/Enemies/Smorzando/Inspect Material And UV State")]
        public static void InspectSmorzandoMaterialUvState()
        {
            var installedAsset = AssetDatabase.LoadAssetAtPath<GameObject>(InstalledModelAssetPath) ??
                throw new InvalidOperationException("Smorzando installed FBX has not been imported.");
            var personAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PersonModelAssetPath) ??
                throw new InvalidOperationException("Smorzando person FBX has not been imported.");
            var lines = new List<string>();
            AppendModelMaterialUvState(lines, "Installed", installedAsset);
            AppendModelMaterialUvState(lines, "Person", personAsset);
            var folder = ProjectAbsolutePath(ReferenceColorRelativeFolder);
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, "Smorzando_MaterialUvState.txt");
            File.WriteAllLines(path, lines);
            Selection.activeObject = null;
            Debug.Log(
                $"SmorzandoMaterialUvStateInspected Path={path}, " +
                $"InstalledRenderers={installedAsset.GetComponentsInChildren<Renderer>(true).Length}, " +
                $"PersonRenderers={personAsset.GetComponentsInChildren<Renderer>(true).Length}, " +
                "SceneChanged=False, SelectionCleared=True");
        }

        [MenuItem("Bellerophon/Enemies/Smorzando/Inspect Installed Idle Geometry")]
        public static void InspectSmorzandoInstalledIdleGeometry()
        {
            var installedAsset = AssetDatabase.LoadAssetAtPath<GameObject>(InstalledModelAssetPath) ??
                throw new InvalidOperationException("Smorzando installed FBX has not been imported.");
            var meshFilter = installedAsset.GetComponentInChildren<MeshFilter>(true) ??
                throw new InvalidOperationException("Smorzando installed FBX has no MeshFilter.");
            var mesh = meshFilter.sharedMesh ??
                throw new InvalidOperationException("Smorzando installed FBX MeshFilter has no mesh.");
            var vertices = mesh.vertices;
            if (vertices.Length == 0)
            {
                throw new InvalidOperationException("Smorzando installed mesh has no vertices.");
            }

            var x = vertices.Select(vertex => vertex.x).OrderBy(value => value).ToArray();
            var y = vertices.Select(vertex => vertex.y).OrderBy(value => value).ToArray();
            var z = vertices.Select(vertex => vertex.z).OrderBy(value => value).ToArray();
            var topThreshold = z[QuantileIndex(z.Length, 0.985f)];
            var topVertices = vertices.Where(vertex => vertex.z >= topThreshold).ToArray();
            var topCenter = topVertices.Aggregate(Vector3.zero, (sum, vertex) => sum + vertex) /
                Mathf.Max(topVertices.Length, 1);
            var lowerBandMaximum = Mathf.Lerp(z[0], z[z.Length - 1], 0.18f);
            var lowerBandCount = vertices.Count(vertex => vertex.z <= lowerBandMaximum);
            var lines = new[]
            {
                "Asset=" + InstalledModelAssetPath,
                "Mesh=" + mesh.name,
                "VertexCount=" + mesh.vertexCount,
                "TriangleCount=" + mesh.triangles.Length / 3,
                "SubMeshCount=" + mesh.subMeshCount,
                "BoundsCenter=" + FormatVector(mesh.bounds.center),
                "BoundsSize=" + FormatVector(mesh.bounds.size),
                DescribeAxisDistribution("X", x),
                DescribeAxisDistribution("Y", y),
                DescribeAxisDistribution("Z", z),
                "ExpectedWorldVerticalAxis=LocalZ after instance X=-90 degrees",
                "LowerBandMaximumZ=" + lowerBandMaximum.ToString("0.########"),
                "LowerBandVertexCount=" + lowerBandCount,
                "TopThresholdZ=" + topThreshold.ToString("0.########"),
                "TopVertexCount=" + topVertices.Length,
                "TopVertexCenter=" + FormatVector(topCenter),
                "SceneChanged=False",
                "SelectionCleared=True"
            };
            var folder = ProjectAbsolutePath(InstalledIdleValidationRelativeFolder);
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, "Smorzando_InstalledIdleGeometry.txt");
            File.WriteAllLines(path, lines);
            Selection.activeObject = null;
            Debug.Log(
                $"SmorzandoInstalledIdleGeometryInspected Path={path}, Mesh={mesh.name}, " +
                $"Vertices={mesh.vertexCount}, TopCenter={FormatVector(topCenter)}, " +
                "SceneChanged=False, SelectionCleared=True");
        }

        [MenuItem("Bellerophon/Enemies/Smorzando/Apply Reference Colors")]
        public static void ApplySmorzandoReferenceColors()
        {
            var scene = RequireOpenCargoRunScene();
            var root = RequireRoot(scene, SmorzandoRootName);
            var player = RequireRoot(scene, PlayerRootName);
            var preservedRoots = scene.GetRootGameObjects()
                .Select(sceneRoot => new RootSnapshot(sceneRoot))
                .ToArray();
            var preservedTransforms = root.GetComponentsInChildren<Transform>(true)
                .Select(target => new TransformSnapshot(target))
                .ToArray();
            var installedPalette = ExtractWaxPalette(InstalledReferenceImageRelativePath);
            var personPalette = ExtractWaxPalette(PersonReferenceImageRelativePath);
            WritePersonWaxAlbedo(personPalette);
            AssetDatabase.ImportAsset(PersonWaxAlbedoAssetPath, ImportAssetOptions.ForceSynchronousImport);
            ConfigureWaxTextureImporter(PersonWaxAlbedoAssetPath);
            var personTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(PersonWaxAlbedoAssetPath) ??
                throw new InvalidOperationException("Smorzando person wax albedo was not imported.");
            var installedMaterial = CreateOrUpdateWaxMaterial(
                InstalledReferenceMaterialAssetPath,
                installedPalette,
                null,
                0.64f);
            var personMaterial = CreateOrUpdateWaxMaterial(
                PersonReferenceMaterialAssetPath,
                personPalette,
                personTexture,
                0.58f);
            var installedRendererCount = ApplyMaterialToSlots(
                root.transform,
                InstalledSlotPrefix,
                InstalledCount,
                InstalledModelName,
                installedMaterial);
            var personRendererCount = ApplyMaterialToSlots(
                root.transform,
                PersonSlotPrefix,
                PersonCount,
                PersonModelName,
                personMaterial);

            foreach (var snapshot in preservedRoots)
            {
                snapshot.AssertUnchanged();
            }

            foreach (var snapshot in preservedTransforms)
            {
                snapshot.AssertUnchanged();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Selection.activeObject = null;
            var reportFolder = ProjectAbsolutePath(ReferenceColorRelativeFolder);
            Directory.CreateDirectory(reportFolder);
            File.WriteAllLines(
                Path.Combine(reportFolder, "Smorzando_ReferenceColorApply.txt"),
                new[]
                {
                    "InstalledPalette=" + installedPalette,
                    "PersonPalette=" + personPalette,
                    "InstalledMaterial=" + InstalledReferenceMaterialAssetPath,
                    "PersonMaterial=" + PersonReferenceMaterialAssetPath,
                    "PersonAlbedo=" + PersonWaxAlbedoAssetPath,
                    "InstalledUV0=0",
                    "InstalledTextureMapApplied=False",
                    "PersonTextureMapApplied=True",
                    "InstalledRendererCount=" + installedRendererCount,
                    "PersonRendererCount=" + personRendererCount,
                    "PlayerPosition=" + FormatVector(player.transform.position),
                    "TransformsChanged=False",
                    "SelectionCleared=True"
                });
            Debug.Log(
                $"SmorzandoReferenceColorsApplied InstalledRenderers={installedRendererCount}, " +
                $"PersonRenderers={personRendererCount}, InstalledPalette={installedPalette}, " +
                $"PersonPalette={personPalette}, InstalledUV0=0, InstalledTextureMapApplied=False, " +
                "PersonTextureMapApplied=True, TransformsChanged=False, SelectionCleared=True");
        }

        [MenuItem("Bellerophon/Enemies/Smorzando/Capture Reference Color Frames")]
        public static void CaptureSmorzandoReferenceColorFrames()
        {
            var scene = RequireOpenCargoRunScene();
            var sceneWasDirty = scene.isDirty;
            var root = RequireRoot(scene, SmorzandoRootName);
            var player = RequireRoot(scene, PlayerRootName);
            var runtimeCamera = FindPlayerCamera(scene, player.transform);
            var cameraObject = new GameObject("Smorzando_ReferenceColor_CaptureCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var lightObject = new GameObject("Smorzando_ReferenceColor_CaptureLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            GameObject captureClone = null;
            GameObject floorObject = null;
            Material floorMaterial = null;
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.955f, 0.96f, 0.965f, 1f);
                camera.orthographic = true;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 100f;
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2.5f;
                light.cullingMask = 1 << CaptureLayer;
                lightObject.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
                var outputFolder = ProjectAbsolutePath(ReferenceColorCaptureRelativeFolder);
                Directory.CreateDirectory(outputFolder);

                CaptureReferenceColorObject(
                    root.transform.Find(InstalledSlotPrefix + "01") ??
                    throw new InvalidOperationException("First installed Smorzando slot is missing."),
                    camera,
                    ref captureClone,
                    ref floorObject,
                    ref floorMaterial,
                    (Vector3.back + Vector3.right).normalized,
                    Path.Combine(outputFolder, "Smorzando_Installed_ReferenceColor.png"));
                CaptureReferenceColorObject(
                    root.transform.Find(PersonSlotPrefix + "01") ??
                    throw new InvalidOperationException("First person Smorzando slot is missing."),
                    camera,
                    ref captureClone,
                    ref floorObject,
                    ref floorMaterial,
                    Vector3.back,
                    Path.Combine(outputFolder, "Smorzando_Person_ReferenceColor.png"));
                CaptureReferenceColorObject(
                    root.transform,
                    camera,
                    ref captureClone,
                    ref floorObject,
                    ref floorMaterial,
                    Vector3.back,
                    Path.Combine(outputFolder, "Smorzando_ReferenceColor_Row.png"));
                SaveCurrentCameraPng(
                    runtimeCamera,
                    Path.Combine(outputFolder, "Smorzando_ReferenceColor_PlayerView.png"),
                    1280,
                    720);
                File.WriteAllLines(
                    Path.Combine(outputFolder, "Smorzando_ReferenceColor_CaptureManifest.txt"),
                    new[]
                    {
                        "InstalledReference=" + InstalledReferenceImageRelativePath,
                        "PersonReference=" + PersonReferenceImageRelativePath,
                        "Views=InstalledThreeQuarter|PersonFront|RowFront|PlayerMainCamera",
                        "InstalledUV0=0",
                        "PersonTextureMapApplied=True",
                        "SceneViewFocused=False",
                        "SelectionCleared=True"
                    });
                Debug.Log(
                    $"SmorzandoReferenceColorFramesCaptured Folder={outputFolder}, " +
                    "Views=InstalledThreeQuarter|PersonFront|RowFront|PlayerMainCamera, " +
                    "SceneViewFocused=False, SceneSaved=False, SelectionCleared=True");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(captureClone);
                UnityEngine.Object.DestroyImmediate(floorObject);
                UnityEngine.Object.DestroyImmediate(floorMaterial);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
                Selection.activeObject = null;
                if (scene.isDirty != sceneWasDirty)
                {
                    throw new InvalidOperationException("Smorzando reference-color capture changed the scene dirty state.");
                }
            }
        }

        [MenuItem("Bellerophon/Enemies/Smorzando/Apply Scene Placement")]
        public static void ApplySmorzandoScenePlacement()
        {
            var installedAsset = AssetDatabase.LoadAssetAtPath<GameObject>(InstalledModelAssetPath) ??
                throw new InvalidOperationException("Smorzando installed FBX has not been imported.");
            var personAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PersonModelAssetPath) ??
                throw new InvalidOperationException("Smorzando person FBX has not been imported.");
            var scene = RequireOpenCargoRunScene();
            var longa = RequireRoot(scene, LongaRootName);
            var tergo = RequireRoot(scene, TergoRootName);
            var grave = RequireRoot(scene, GraveRootName);
            var graveModel = grave.transform.Find(GraveStaticSlotName + "/" + GraveModelName) ??
                throw new InvalidOperationException("Grave static review model is missing.");
            var preservedRoots = scene.GetRootGameObjects()
                .Where(root => root.name != SmorzandoRootName)
                .Select(root => new RootSnapshot(root))
                .ToArray();
            var existing = FindRoot(scene, SmorzandoRootName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }

            var zSpacing = Mathf.Abs(longa.transform.position.z - tergo.transform.position.z);
            if (zSpacing <= 0.1f)
            {
                zSpacing = Vector3.Distance(longa.transform.position, tergo.transform.position);
            }

            if (zSpacing <= 0.1f)
            {
                throw new InvalidOperationException("Longa Arma-Tergo spacing is too small.");
            }

            var anchor = new Vector3(
                grave.transform.position.x,
                grave.transform.position.y,
                grave.transform.position.z - zSpacing);
            var root = new GameObject(SmorzandoRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetPositionAndRotation(anchor, Quaternion.identity);
            var facing = graveModel.rotation;
            var orderedSlots = new List<Transform>(InstalledCount + PersonCount);

            var firstInstalled = CreateSlotWithModel(
                scene,
                root.transform,
                InstalledSlotPrefix + "01",
                InstalledModelName,
                installedAsset,
                facing * InstalledImportAxisCorrection,
                InstalledImportScaleCorrection,
                anchor.y);
            orderedSlots.Add(firstInstalled);
            var previousBounds = CalculateVisibleBounds(firstInstalled);
            for (var i = 1; i < InstalledCount; i++)
            {
                var slot = CreateSlotWithModel(
                    scene,
                    root.transform,
                    InstalledSlotPrefix + (i + 1).ToString("00"),
                    InstalledModelName,
                    installedAsset,
                    facing * InstalledImportAxisCorrection,
                    InstalledImportScaleCorrection,
                    anchor.y);
                MoveSlotLeftEdgeTo(slot, previousBounds.max.x + HorizontalClearance);
                orderedSlots.Add(slot);
                previousBounds = CalculateVisibleBounds(slot);
            }

            for (var i = 0; i < PersonCount; i++)
            {
                var slot = CreateSlotWithModel(
                    scene,
                    root.transform,
                    PersonSlotPrefix + (i + 1).ToString("00"),
                    PersonModelName,
                    personAsset,
                    facing,
                    1f,
                    anchor.y);
                MoveSlotLeftEdgeTo(slot, previousBounds.max.x + HorizontalClearance);
                orderedSlots.Add(slot);
                previousBounds = CalculateVisibleBounds(slot);
            }

            var metrics = InspectPlacement(root.transform, longa.transform, tergo.transform, grave.transform);
            foreach (var snapshot in preservedRoots)
            {
                snapshot.AssertUnchanged();
            }

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Selection.activeObject = null;
            Debug.Log("SmorzandoScenePlacementApplied " + metrics + ", SelectionCleared=True");
        }

        [MenuItem("Bellerophon/Enemies/Smorzando/Capture Scene Placement Frames")]
        public static void CaptureSmorzandoScenePlacementFrames()
        {
            var scene = RequireOpenCargoRunScene();
            var sceneWasDirty = scene.isDirty;
            var longa = RequireRoot(scene, LongaRootName);
            var tergo = RequireRoot(scene, TergoRootName);
            var grave = RequireRoot(scene, GraveRootName);
            var root = RequireRoot(scene, SmorzandoRootName);
            var metrics = InspectPlacement(root.transform, longa.transform, tergo.transform, grave.transform);
            var clone = UnityEngine.Object.Instantiate(root);
            clone.name = "Smorzando_Placement_CaptureClone";
            clone.hideFlags = HideFlags.HideAndDontSave;
            var cameraObject = new GameObject("Smorzando_Placement_CaptureCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var lightObject = new GameObject("Smorzando_Placement_CaptureLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            GameObject floorObject = null;
            Material floorMaterial = null;
            try
            {
                foreach (var target in clone.GetComponentsInChildren<Transform>(true))
                {
                    target.gameObject.layer = CaptureLayer;
                    target.gameObject.hideFlags = HideFlags.HideAndDontSave;
                }

                foreach (var helperCamera in clone.GetComponentsInChildren<Camera>(true))
                {
                    helperCamera.enabled = false;
                }

                foreach (var helperLight in clone.GetComponentsInChildren<Light>(true))
                {
                    helperLight.enabled = false;
                }

                var bounds = CalculateVisibleBounds(clone.transform);
                var camera = cameraObject.AddComponent<Camera>();
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.955f, 0.96f, 0.965f, 1f);
                camera.orthographic = true;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 100f;
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2.1f;
                light.cullingMask = 1 << CaptureLayer;
                lightObject.transform.rotation = Quaternion.Euler(42f, -35f, 0f);

                floorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorObject.name = "Smorzando_Placement_CaptureFloor";
                floorObject.hideFlags = HideFlags.HideAndDontSave;
                floorObject.layer = CaptureLayer;
                floorObject.transform.position = new Vector3(
                    bounds.center.x,
                    root.transform.position.y - 0.025f,
                    bounds.center.z);
                floorObject.transform.localScale = new Vector3(
                    bounds.size.x + 3f,
                    0.05f,
                    Mathf.Max(bounds.size.z + 3f, 6f));
                var collider = floorObject.GetComponent<Collider>();
                if (collider != null)
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }

                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader == null)
                {
                    throw new InvalidOperationException("Smorzando capture floor shader is missing.");
                }

                floorMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    color = new Color(0.65f, 0.67f, 0.69f, 1f)
                };
                floorObject.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;

                const int width = 1280;
                const int height = 720;
                var aspect = width / (float)height;
                var frontSize = Mathf.Max(bounds.extents.y + 0.6f, bounds.extents.x / aspect + 0.6f);
                var topSize = Mathf.Max(bounds.extents.z + 1f, bounds.extents.x / aspect + 0.6f);
                var outputFolder = ProjectAbsolutePath(CaptureRelativeFolder);
                Directory.CreateDirectory(outputFolder);
                CapturePng(
                    camera,
                    bounds.center + Vector3.back * 40f,
                    bounds.center,
                    Vector3.up,
                    frontSize,
                    width,
                    height,
                    Path.Combine(outputFolder, "Smorzando_Placement_Front.png"));
                CapturePng(
                    camera,
                    bounds.center + (Vector3.back + Vector3.right).normalized * 40f,
                    bounds.center,
                    Vector3.up,
                    frontSize,
                    width,
                    height,
                    Path.Combine(outputFolder, "Smorzando_Placement_ThreeQuarter.png"));
                CapturePng(
                    camera,
                    bounds.center + Vector3.up * 40f,
                    bounds.center,
                    Vector3.forward,
                    topSize,
                    width,
                    height,
                    Path.Combine(outputFolder, "Smorzando_Placement_Top.png"));
                File.WriteAllText(
                    Path.Combine(outputFolder, "Smorzando_Placement_CaptureManifest.txt"),
                    metrics + Environment.NewLine +
                    "Views=Front|ThreeQuarter|Top" + Environment.NewLine +
                    "SceneViewFocused=False" + Environment.NewLine +
                    "SelectionCleared=True" + Environment.NewLine);
                Debug.Log(
                    "SmorzandoScenePlacementFramesCaptured " + metrics +
                    $", Folder={outputFolder}, Views=Front|ThreeQuarter|Top, " +
                    "SceneViewFocused=False, SceneSaved=False, SelectionCleared=True");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(floorObject);
                UnityEngine.Object.DestroyImmediate(floorMaterial);
                Selection.activeObject = null;
                if (scene.isDirty != sceneWasDirty)
                {
                    throw new InvalidOperationException(
                        "Smorzando automated visual capture changed the CargoRunMvp dirty state.");
                }
            }
        }

        [MenuItem("Bellerophon/Enemies/Smorzando/Move Player Start To Front")]
        public static void MoveSmorzandoPlayerStartToFront()
        {
            var scene = RequireOpenCargoRunScene();
            var smorzandoRoot = RequireRoot(scene, SmorzandoRootName);
            var player = RequireRoot(scene, PlayerRootName);
            var camera = FindPlayerCamera(scene, player.transform);
            var cameraObject = camera.gameObject;
            var cameraIsPlayerChild = camera.transform.IsChildOf(player.transform);
            var preservedRoots = scene.GetRootGameObjects()
                .Where(root => root != player && (cameraIsPlayerChild || root != cameraObject))
                .Select(root => new RootSnapshot(root))
                .ToArray();
            var bounds = CalculateVisibleBounds(smorzandoRoot.transform);
            var personModel = smorzandoRoot.transform.Find(PersonSlotPrefix + "01/" + PersonModelName) ??
                throw new InvalidOperationException("First Smorzando person model is missing.");
            var visualFront = CalculateVisualFront(personModel);
            var distance = CalculatePlayerViewDistance(camera, bounds);
            var target = bounds.center;
            var start = target + visualFront * distance;
            start.y = player.transform.position.y;
            player.transform.SetPositionAndRotation(start, YawToward(start, target));

            if (!cameraIsPlayerChild)
            {
                var cameraHeight = camera.transform.position.y - player.transform.position.y;
                var cameraPosition = start + Vector3.up * Mathf.Max(cameraHeight, 1.35f);
                camera.transform.SetPositionAndRotation(
                    cameraPosition,
                    Quaternion.LookRotation(target - cameraPosition, Vector3.up));
                EditorUtility.SetDirty(camera.transform);
            }

            foreach (var snapshot in preservedRoots)
            {
                snapshot.AssertUnchanged();
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
            EditorUtility.SetDirty(player.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeObject = null;
            var horizontalFov = CalculateHorizontalFieldOfView(camera);
            Debug.Log(
                $"SmorzandoPlayerStartMoved Player={FormatVector(player.transform.position)}, " +
                $"Target={FormatVector(target)}, VisualFront={FormatVector(visualFront)}, " +
                $"Distance={distance:0.######}, VerticalFov={camera.fieldOfView:0.###}, " +
                $"HorizontalFov={horizontalFov:0.###}, CameraIsPlayerChild={cameraIsPlayerChild}, " +
                "SmorzandoTransformChanged=False, SelectionCleared=True");
        }

        [MenuItem("Bellerophon/Enemies/Smorzando/Capture Player Start View")]
        public static void CaptureSmorzandoPlayerStartView()
        {
            var scene = RequireOpenCargoRunScene();
            var sceneWasDirty = scene.isDirty;
            var smorzandoRoot = RequireRoot(scene, SmorzandoRootName);
            var player = RequireRoot(scene, PlayerRootName);
            var camera = FindPlayerCamera(scene, player.transform);
            var bounds = CalculateVisibleBounds(smorzandoRoot.transform);
            var personModel = smorzandoRoot.transform.Find(PersonSlotPrefix + "01/" + PersonModelName) ??
                throw new InvalidOperationException("First Smorzando person model is missing.");
            var visualFront = CalculateVisualFront(personModel);
            var playerSide = player.transform.position - bounds.center;
            playerSide.y = 0f;
            if (playerSide.sqrMagnitude < 0.0001f || Vector3.Dot(playerSide.normalized, visualFront) < 0.99f)
            {
                throw new InvalidOperationException("Player is not positioned on the Smorzando front side.");
            }

            var outputFolder = ProjectAbsolutePath(PlayerStartCaptureRelativeFolder);
            Directory.CreateDirectory(outputFolder);
            var outputPath = Path.Combine(outputFolder, "Smorzando_PlayerStart_MainCamera.png");
            SaveCurrentCameraPng(camera, outputPath, 1280, 720);
            var metrics =
                $"Player={FormatVector(player.transform.position)}, Camera={FormatVector(camera.transform.position)}, " +
                $"CameraForward={FormatVector(camera.transform.forward)}, Target={FormatVector(bounds.center)}, " +
                $"Distance={playerSide.magnitude:0.######}, VerticalFov={camera.fieldOfView:0.###}, " +
                $"HorizontalFov={CalculateHorizontalFieldOfView(camera):0.###}, " +
                $"CameraIsPlayerChild={camera.transform.IsChildOf(player.transform)}";
            File.WriteAllText(
                Path.Combine(outputFolder, "Smorzando_PlayerStart_CaptureManifest.txt"),
                metrics + Environment.NewLine +
                "MainCameraRender=True" + Environment.NewLine +
                "SceneViewFocused=False" + Environment.NewLine +
                "SelectionCleared=True" + Environment.NewLine);
            Selection.activeObject = null;
            if (scene.isDirty != sceneWasDirty)
            {
                throw new InvalidOperationException("Smorzando player-start capture changed the scene dirty state.");
            }

            Debug.Log(
                $"SmorzandoPlayerStartViewCaptured Path={outputPath}, {metrics}, " +
                "MainCameraRender=True, SceneViewFocused=False, SceneSaved=False, SelectionCleared=True");
        }

        private static Transform CreateSlotWithModel(
            Scene scene,
            Transform root,
            string slotName,
            string modelName,
            GameObject modelAsset,
            Quaternion facing,
            float uniformScale,
            float groundY)
        {
            var slot = new GameObject(slotName).transform;
            slot.SetParent(root, false);
            slot.localPosition = Vector3.zero;
            slot.localRotation = Quaternion.identity;
            slot.localScale = Vector3.one;
            var model = PrefabUtility.InstantiatePrefab(modelAsset, scene) as GameObject;
            if (model == null)
            {
                model = UnityEngine.Object.Instantiate(modelAsset);
                SceneManager.MoveGameObjectToScene(model, scene);
            }

            model.name = modelName;
            model.transform.SetParent(slot, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.rotation = facing;
            model.transform.localScale = Vector3.one * uniformScale;
            foreach (var helperCamera in model.GetComponentsInChildren<Camera>(true))
            {
                helperCamera.enabled = false;
            }

            foreach (var helperLight in model.GetComponentsInChildren<Light>(true))
            {
                helperLight.enabled = false;
            }

            var renderers = model.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(modelName + " has no visible renderer.");
            }

            if (renderers.Any(renderer => renderer.sharedMaterials.Length == 0 || renderer.sharedMaterials.Any(material => material == null)))
            {
                throw new InvalidOperationException(modelName + " has a missing material reference.");
            }

            var bounds = CalculateVisibleBounds(slot);
            model.transform.position += Vector3.up * (groundY - bounds.min.y);
            EditorUtility.SetDirty(slot.gameObject);
            EditorUtility.SetDirty(model);
            return slot;
        }

        private static void AppendModelMaterialUvState(List<string> lines, string label, GameObject asset)
        {
            lines.Add("Model=" + label);
            lines.Add("Asset=" + AssetDatabase.GetAssetPath(asset));
            var renderers = asset.GetComponentsInChildren<Renderer>(true);
            lines.Add("RendererCount=" + renderers.Length);
            foreach (var renderer in renderers)
            {
                Mesh mesh = null;
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    mesh = skinned.sharedMesh;
                }
                else
                {
                    var filter = renderer.GetComponent<MeshFilter>();
                    mesh = filter != null ? filter.sharedMesh : null;
                }

                var materials = renderer.sharedMaterials;
                lines.Add(
                    $"Renderer={renderer.name},Type={renderer.GetType().Name}," +
                    $"Mesh={mesh?.name ?? "None"},Vertices={mesh?.vertexCount ?? 0}," +
                    $"SubMeshes={mesh?.subMeshCount ?? 0},Materials={materials.Length}");
                if (mesh != null)
                {
                    var uv = mesh.uv;
                    var uvMin = uv.Length > 0 ? uv[0] : Vector2.zero;
                    var uvMax = uvMin;
                    for (var i = 1; i < uv.Length; i++)
                    {
                        uvMin = Vector2.Min(uvMin, uv[i]);
                        uvMax = Vector2.Max(uvMax, uv[i]);
                    }

                    lines.Add(
                        $"UV0Count={uv.Length},UV0Min=({uvMin.x:0.######},{uvMin.y:0.######})," +
                        $"UV0Max=({uvMax.x:0.######},{uvMax.y:0.######})");
                }

                for (var i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    var mainTexture = material != null && material.HasProperty("_BaseMap")
                        ? material.GetTexture("_BaseMap")
                        : material != null ? material.mainTexture : null;
                    var color = material != null && material.HasProperty("_BaseColor")
                        ? material.GetColor("_BaseColor")
                        : material != null && material.HasProperty("_Color")
                            ? material.color
                            : Color.white;
                    lines.Add(
                        $"Material[{i}]={material?.name ?? "None"},Shader={material?.shader?.name ?? "None"}," +
                        $"Color=({color.r:0.######},{color.g:0.######},{color.b:0.######},{color.a:0.######})," +
                        $"MainTexture={mainTexture?.name ?? "None"}");
                }
            }

            lines.Add(string.Empty);
        }

        private static WaxPalette ExtractWaxPalette(string relativePath)
        {
            var bytes = File.ReadAllBytes(ProjectAbsolutePath(relativePath));
            var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try
            {
                if (!texture.LoadImage(bytes, false))
                {
                    throw new InvalidOperationException("Failed to load Smorzando reference image: " + relativePath);
                }

                var candidates = texture.GetPixels32()
                    .Select(pixel => (Color)pixel)
                    .Where(color =>
                        color.r > 0.12f && color.r < 0.92f &&
                        color.r - color.g > 0.055f && color.g - color.b > 0.005f &&
                        Mathf.Max(color.r, color.g, color.b) - Mathf.Min(color.r, color.g, color.b) > 0.11f)
                    .OrderBy(color => color.grayscale)
                    .ToArray();
                if (candidates.Length < 100)
                {
                    throw new InvalidOperationException("Smorzando reference image has too few wax-colored pixels: " + relativePath);
                }

                var third = candidates.Length / 3;
                return new WaxPalette(
                    AverageColors(candidates, 0, third),
                    AverageColors(candidates, third, third * 2),
                    AverageColors(candidates, third * 2, candidates.Length),
                    candidates.Length);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static Color AverageColors(IReadOnlyList<Color> colors, int start, int end)
        {
            var sum = Vector3.zero;
            var count = Mathf.Max(end - start, 1);
            for (var i = start; i < end; i++)
            {
                sum += new Vector3(colors[i].r, colors[i].g, colors[i].b);
            }

            sum /= count;
            return new Color(sum.x, sum.y, sum.z, 1f);
        }

        private static int QuantileIndex(int count, float normalized)
        {
            return Mathf.Clamp(Mathf.RoundToInt((count - 1) * normalized), 0, count - 1);
        }

        private static string DescribeAxisDistribution(string label, IReadOnlyList<float> sorted)
        {
            var quantiles = new[] { 0f, 0.01f, 0.05f, 0.10f, 0.25f, 0.50f, 0.75f, 0.90f, 0.95f, 0.99f, 1f };
            return label + "Quantiles=" + string.Join(
                ",",
                quantiles.Select(quantile =>
                    $"{quantile:0.##}:{sorted[QuantileIndex(sorted.Count, quantile)]:0.########}"));
        }

        private static void WritePersonWaxAlbedo(WaxPalette palette)
        {
            EnsureAssetFolder(PersonWaxAlbedoAssetPath);
            const int size = 512;
            var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
            try
            {
                var pixels = new Color[size * size];
                for (var y = 0; y < size; y++)
                {
                    var v = y / (float)size;
                    for (var x = 0; x < size; x++)
                    {
                        var u = x / (float)size;
                        var broad = Mathf.Sin(Mathf.PI * 2f * (u * 3f + Mathf.Sin(v * Mathf.PI * 4f) * 0.18f));
                        var fine = Mathf.Sin(Mathf.PI * 2f * (v * 11f + Mathf.Sin(u * Mathf.PI * 6f) * 0.12f));
                        var drip = Mathf.Pow(Mathf.Abs(Mathf.Sin(Mathf.PI * 2f * u * 7f)), 6f) *
                                   (0.5f + 0.5f * Mathf.Sin(Mathf.PI * 2f * v * 2f));
                        var factor = Mathf.Clamp01(0.5f + broad * 0.13f + fine * 0.08f - drip * 0.11f);
                        var color = factor < 0.5f
                            ? Color.Lerp(palette.shadow, palette.mid, factor * 2f)
                            : Color.Lerp(palette.mid, palette.highlight, (factor - 0.5f) * 2f);
                        // Encode the sampled linear palette back to display space before the
                        // generated PNG is imported as sRGB. Without this conversion the wax
                        // receives the transfer curve twice and renders much darker than the reference.
                        pixels[y * size + x] = color.gamma;
                    }
                }

                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(ProjectAbsolutePath(PersonWaxAlbedoAssetPath), texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void ConfigureWaxTextureImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter ??
                throw new InvalidOperationException("Smorzando wax texture importer is missing.");
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static Material CreateOrUpdateWaxMaterial(
            string assetPath,
            WaxPalette palette,
            Texture2D albedo,
            float smoothness)
        {
            EnsureAssetFolder(assetPath);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                throw new InvalidOperationException("URP Lit shader is missing.");
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

            var baseColor = albedo == null
                ? palette.shadow
                : new Color(0.42f, 0.36f, 0.32f, 1f);
            material.SetColor("_BaseColor", baseColor);
            material.SetTexture("_BaseMap", albedo != null ? albedo : Texture2D.whiteTexture);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_EnvironmentReflections", 1f);
            material.SetFloat("_SpecularHighlights", 1f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", palette.mid * (albedo == null ? 0.32f : 0.28f));
            EditorUtility.SetDirty(material);
            return material;
        }

        private static int ApplyMaterialToSlots(
            Transform root,
            string slotPrefix,
            int count,
            string modelName,
            Material material)
        {
            var rendererCount = 0;
            for (var i = 1; i <= count; i++)
            {
                var model = root.Find(slotPrefix + i.ToString("00") + "/" + modelName) ??
                    throw new InvalidOperationException(slotPrefix + i.ToString("00") + " model is missing.");
                foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    var materials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
                    renderer.sharedMaterials = materials;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    EditorUtility.SetDirty(renderer);
                    rendererCount++;
                }
            }

            return rendererCount;
        }

        private static void CaptureReferenceColorObject(
            Transform source,
            Camera camera,
            ref GameObject captureClone,
            ref GameObject floorObject,
            ref Material floorMaterial,
            Vector3 viewDirection,
            string outputPath)
        {
            UnityEngine.Object.DestroyImmediate(captureClone);
            UnityEngine.Object.DestroyImmediate(floorObject);
            UnityEngine.Object.DestroyImmediate(floorMaterial);
            captureClone = UnityEngine.Object.Instantiate(source.gameObject);
            captureClone.name = source.name + "_ReferenceColorCaptureClone";
            captureClone.hideFlags = HideFlags.HideAndDontSave;
            foreach (var target in captureClone.GetComponentsInChildren<Transform>(true))
            {
                target.gameObject.layer = CaptureLayer;
                target.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }

            var bounds = CalculateVisibleBounds(captureClone.transform);
            floorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floorObject.name = "Smorzando_ReferenceColor_CaptureFloor";
            floorObject.hideFlags = HideFlags.HideAndDontSave;
            floorObject.layer = CaptureLayer;
            floorObject.transform.position = new Vector3(bounds.center.x, bounds.min.y - 0.025f, bounds.center.z);
            floorObject.transform.localScale = new Vector3(
                Mathf.Max(bounds.size.x + 2f, 4f),
                0.05f,
                Mathf.Max(bounds.size.z + 2f, 4f));
            var collider = floorObject.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            floorMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave,
                color = new Color(0.68f, 0.70f, 0.72f, 1f)
            };
            floorObject.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;
            const float aspect = 1f;
            var size = Mathf.Max(bounds.extents.y + 0.35f, bounds.extents.x / aspect + 0.35f);
            CapturePng(
                camera,
                bounds.center + viewDirection.normalized * 40f,
                bounds.center,
                Vector3.up,
                size,
                720,
                720,
                outputPath);
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

        private static void MoveSlotLeftEdgeTo(Transform slot, float desiredLeftEdge)
        {
            var bounds = CalculateVisibleBounds(slot);
            slot.position += Vector3.right * (desiredLeftEdge - bounds.min.x);
        }

        private static string InspectPlacement(
            Transform root,
            Transform longa,
            Transform tergo,
            Transform grave)
        {
            var installed = Enumerable.Range(1, InstalledCount)
                .Select(index => root.Find(InstalledSlotPrefix + index.ToString("00")))
                .ToArray();
            var persons = Enumerable.Range(1, PersonCount)
                .Select(index => root.Find(PersonSlotPrefix + index.ToString("00")))
                .ToArray();
            if (installed.Any(slot => slot == null) || persons.Any(slot => slot == null))
            {
                throw new InvalidOperationException("Smorzando placement slot count does not match 3 installed + 5 person.");
            }

            if (root.childCount != InstalledCount + PersonCount)
            {
                throw new InvalidOperationException("Smorzando placement root contains unexpected children.");
            }

            var expectedSpacing = Mathf.Abs(longa.position.z - tergo.position.z);
            if (expectedSpacing <= 0.1f)
            {
                expectedSpacing = Vector3.Distance(longa.position, tergo.position);
            }

            var actualSpacing = Mathf.Abs(grave.position.z - root.position.z);
            if (Mathf.Abs(expectedSpacing - actualSpacing) > 0.001f ||
                Mathf.Abs(root.position.x - grave.position.x) > 0.001f ||
                root.position.z >= grave.position.z)
            {
                throw new InvalidOperationException(
                    $"Smorzando anchor mismatch. ExpectedZSpacing={expectedSpacing:0.######}, " +
                    $"ActualZSpacing={actualSpacing:0.######}, Root={FormatVector(root.position)}, " +
                    $"Grave={FormatVector(grave.position)}");
            }

            var ordered = installed.Concat(persons).ToArray();
            var bounds = ordered.Select(CalculateVisibleBounds).ToArray();
            var minimumGap = float.MaxValue;
            for (var i = 0; i < bounds.Length; i++)
            {
                if (Mathf.Abs(bounds[i].min.y - root.position.y) > GroundTolerance)
                {
                    throw new InvalidOperationException(
                        $"{ordered[i].name} is not grounded. Ground={root.position.y:0.######}, " +
                        $"BoundsMinY={bounds[i].min.y:0.######}");
                }

                if (i == 0)
                {
                    continue;
                }

                var gap = bounds[i].min.x - bounds[i - 1].max.x;
                minimumGap = Mathf.Min(minimumGap, gap);
                if (gap < -0.0001f)
                {
                    throw new InvalidOperationException(
                        $"Smorzando X bounds overlap between {ordered[i - 1].name} and {ordered[i].name}: {gap:0.######}");
                }
            }

            var rendererCount = root.GetComponentsInChildren<Renderer>(true).Count(renderer => renderer.enabled);
            var missingMaterialCount = root.GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .Count(material => material == null);
            if (missingMaterialCount != 0)
            {
                throw new InvalidOperationException("Smorzando placement has missing material references.");
            }

            var slotBounds = string.Join(
                "|",
                ordered.Select((slot, index) => slot.name + ":" + FormatVector(bounds[index].size)));

            return
                $"Root={SmorzandoRootName}, InstalledCount={installed.Length}, PersonCount={persons.Length}, " +
                $"TotalCount={ordered.Length}, Anchor={FormatVector(root.position)}, " +
                $"LongaTergoZSpacing={expectedSpacing:0.######}, GraveToSmorzandoZSpacing={actualSpacing:0.######}, " +
                $"MinimumXBoundsGap={minimumGap:0.######}, RendererCount={rendererCount}, " +
                $"MissingMaterialCount={missingMaterialCount}, SlotBounds={slotBounds}";
        }

        private static Bounds CalculateVisibleBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(root.name + " has no visible renderer bounds.");
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static void CapturePng(
            Camera camera,
            Vector3 position,
            Vector3 target,
            Vector3 up,
            float orthographicSize,
            int width,
            int height,
            string path)
        {
            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position, up));
            camera.orthographicSize = Mathf.Max(orthographicSize, 0.5f);
            var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            var previousTarget = camera.targetTexture;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void SaveCurrentCameraPng(Camera camera, string path, int width, int height)
        {
            var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            var previousTarget = camera.targetTexture;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static float CalculatePlayerViewDistance(Camera camera, Bounds bounds)
        {
            var verticalHalfRadians = camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            var horizontalHalfRadians = CalculateHorizontalFieldOfView(camera) * 0.5f * Mathf.Deg2Rad;
            var horizontalDistance = (bounds.extents.x + PlayerViewPadding) / Mathf.Tan(horizontalHalfRadians);
            var verticalDistance = (bounds.extents.y + PlayerViewPadding) / Mathf.Tan(verticalHalfRadians);
            return Mathf.Max(horizontalDistance, verticalDistance) + bounds.extents.z + PlayerViewPadding;
        }

        private static float CalculateHorizontalFieldOfView(Camera camera)
        {
            var aspect = camera.aspect > 0.1f ? camera.aspect : 16f / 9f;
            var verticalHalfRadians = camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            return 2f * Mathf.Atan(Mathf.Tan(verticalHalfRadians) * aspect) * Mathf.Rad2Deg;
        }

        private static Vector3 CalculateVisualFront(Transform model)
        {
            var head = model.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(target => string.Equals(target.name, "Head", StringComparison.OrdinalIgnoreCase));
            var headFront = model.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(target => string.Equals(target.name, "headfront", StringComparison.OrdinalIgnoreCase));
            var front = head != null && headFront != null ? headFront.position - head.position : model.forward;
            front.y = 0f;
            return front.sqrMagnitude > 0.0001f ? front.normalized : model.forward;
        }

        private static Quaternion YawToward(Vector3 from, Vector3 to)
        {
            var direction = to - from;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : Quaternion.identity;
        }

        private static Scene RequireOpenCargoRunScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != CargoRunScenePath)
            {
                throw new InvalidOperationException("CargoRunMvp must be the open active scene.");
            }

            return scene;
        }

        private static GameObject RequireRoot(Scene scene, string name)
        {
            return FindRoot(scene, name) ?? throw new InvalidOperationException(name + " is missing.");
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        }

        private static GameObject FindSceneObject(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var match = root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(target => target.name == name);
                if (match != null)
                {
                    return match.gameObject;
                }
            }

            return null;
        }

        private static Camera FindPlayerCamera(Scene scene, Transform player)
        {
            var playerCameras = player.GetComponentsInChildren<Camera>(true);
            var camera = playerCameras.FirstOrDefault(candidate =>
                    candidate.gameObject.activeInHierarchy && candidate.enabled && candidate.CompareTag("MainCamera")) ??
                playerCameras.FirstOrDefault(candidate => candidate.gameObject.activeInHierarchy && candidate.enabled) ??
                playerCameras.FirstOrDefault();
            if (camera != null)
            {
                return camera;
            }

            camera = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                .FirstOrDefault(candidate =>
                    candidate.gameObject.activeInHierarchy && candidate.enabled && candidate.CompareTag("MainCamera"));
            return camera ?? throw new InvalidOperationException("Player runtime camera is missing.");
        }

        private static string ProjectAbsolutePath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }

        private static string FormatVector(Vector3 value)
        {
            return $"({value.x:0.######},{value.y:0.######},{value.z:0.######})";
        }

        private sealed class RootSnapshot
        {
            private readonly GameObject root;
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;
            private readonly int childCount;

            public RootSnapshot(GameObject root)
            {
                this.root = root;
                position = root.transform.position;
                rotation = root.transform.rotation;
                scale = root.transform.localScale;
                childCount = root.transform.childCount;
            }

            public void AssertUnchanged()
            {
                if (root == null || root.transform.position != position || root.transform.rotation != rotation ||
                    root.transform.localScale != scale || root.transform.childCount != childCount)
                {
                    throw new InvalidOperationException("Existing scene root changed while placing Smorzando: " + root?.name);
                }
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

            public void AssertUnchanged()
            {
                if (target == null || target.localPosition != localPosition || target.localRotation != localRotation ||
                    target.localScale != localScale)
                {
                    throw new InvalidOperationException("Smorzando Transform changed while applying reference colors: " + target?.name);
                }
            }
        }

        private readonly struct WaxPalette
        {
            public readonly Color shadow;
            public readonly Color mid;
            public readonly Color highlight;
            public readonly int pixelCount;

            public WaxPalette(Color shadow, Color mid, Color highlight, int pixelCount)
            {
                this.shadow = shadow;
                this.mid = mid;
                this.highlight = highlight;
                this.pixelCount = pixelCount;
            }

            public override string ToString()
            {
                return
                    $"Shadow={FormatColor(shadow)},Mid={FormatColor(mid)}," +
                    $"Highlight={FormatColor(highlight)},Pixels={pixelCount}";
            }

            private static string FormatColor(Color value)
            {
                return $"({value.r:0.######},{value.g:0.######},{value.b:0.######},{value.a:0.######})";
            }
        }
    }
}
