using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bellerophon.Editor.Validation
{
    public static class AssetDressingStep02SelectedCorridorSampleRenderer
    {
        private const string OutputRootRelativePath = "artSample/asset_dressing_samples/step02_corridor_floor5_wall2_dense_floorbase_unifiedwall_fullwidthfloor_2026-06-14";
        private const string SuccessMarker = "Asset dressing step 02 selected corridor sample renders saved:";

        private const string FloorBasePlateModelPath = "Assets/Heavy Station Kit/BASE/Meshes/Floors/Floor_5_base_Plate.fbx";
        private const string FloorBase1FPrefabPath = "Assets/Heavy Station Kit/BASE/Prefabs/Floors/Floor Base 1 F.prefab";
        private const string Wall2ModelPath = "Assets/ScifiOfficeLite/Meshes/Walls/Wall 2.FBX";
        private const string Wall2HalfModelPath = "Assets/ScifiOfficeLite/Meshes/Walls/Wall 2 Half.FBX";
        private const string Wall2VariantPrefabPath = "Assets/ScifiOfficeLite/Prefabs/Wall/Wall 2 Variant.prefab";
        private const string Wall2HalfVariantPrefabPath = "Assets/ScifiOfficeLite/Prefabs/Wall/Wall 2 Half Variant.prefab";
        private const string WallPillarPrefabPath = "Assets/ScifiOfficeLite/Prefabs/Wall/Wall Pillar.prefab";
        private const string WallPillar3PrefabPath = "Assets/ScifiOfficeLite/Prefabs/Wall/Wall Pillar 3.prefab";
        private const string CeilingPrefabPath = "Assets/Heavy Station Kit/BASE/Prefabs/Top-Bottom/TB_2.prefab";
        private const string CeilingDetailPrefabPath = "Assets/ScifiOfficeLite/Prefabs/Wall/Wall Top Piece.prefab";

        private const string FloorAlbedoPath = "Assets/Heavy Station Kit/BASE/Textures/Floors/B2_Floors_A.png";
        private const string FloorNormalPath = "Assets/Heavy Station Kit/BASE/Textures/Floors/B2_Floors_N.png";
        private const string WallAlbedoPath = "Assets/ScifiOfficeLite/Meshes/Textures/Environment/Wall texture/Wall set 2/Wall_Multiset_2_Diffuse.png";
        private const string WallNormalPath = "Assets/ScifiOfficeLite/Meshes/Textures/Environment/Wall texture/Wall set 2/Wall_Multiset_2_Normal.png";
        private const string CeilingAlbedoPath = "Assets/Heavy Station Kit/BASE/Textures/Top-Bottom/B2_Top_Bottom_A.png";
        private const string CeilingNormalPath = "Assets/Heavy Station Kit/BASE/Textures/Top-Bottom/B2_Top_Bottom_N.png";

        private const int PreviewLayer = 29;

        [MenuItem("Bellerophon/Validation/Capture Asset Dressing Step 02 Selected Corridor Sample")]
        public static void Capture()
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for selected corridor sample output.");
            }

            var outputRoot = Path.Combine(projectRoot.FullName, OutputRootRelativePath);
            Directory.CreateDirectory(outputRoot);

            var previewRoot = BuildPreviewScene();
            try
            {
                CaptureView(
                    Path.Combine(outputRoot, "view_01_player_entry.png"),
                    new Vector3(0f, 1.35f, -5.4f),
                    new Vector3(0f, 1.12f, 3.8f),
                    49f,
                    false,
                    5.2f);
                CaptureView(
                    Path.Combine(outputRoot, "view_02_floor_wall_diagonal.png"),
                    new Vector3(-3.05f, 1.55f, -2.85f),
                    new Vector3(0.3f, 1.05f, 2.55f),
                    56f,
                    false,
                    5.2f);
                CaptureView(
                    Path.Combine(outputRoot, "view_03_ceiling_and_wall_underlook.png"),
                    new Vector3(0.05f, 0.82f, 1.15f),
                    new Vector3(0.15f, 2.42f, 5.25f),
                    63f,
                    false,
                    5.2f);
                SetRendererVisibilityByName(previewRoot.transform, "ceiling", false);
                CaptureView(
                    Path.Combine(outputRoot, "view_04_layout_topdown.png"),
                    new Vector3(0f, 12.8f, 3.6f),
                    new Vector3(0f, 0f, 3.6f),
                    45f,
                    true,
                    5.8f);
                SetRendererVisibilityByName(previewRoot.transform, "ceiling", true);
                CaptureView(
                    Path.Combine(outputRoot, "view_05_floor_stack_detail.png"),
                    new Vector3(0.42f, 0.58f, -0.72f),
                    new Vector3(0.05f, 0.16f, 1.35f),
                    52f,
                    false,
                    4.2f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(previewRoot);
            }

            WriteReadme(outputRoot);
            WriteManifest(outputRoot);
            WriteApprovalStatus(outputRoot);
            WriteIndex(outputRoot);
            AssetDatabase.Refresh();
            Debug.Log(SuccessMarker + " " + outputRoot);
        }

        private static void SetRendererVisibilityByName(Transform root, string namePart, bool visible)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (HasNamePart(renderers[i].transform, root, namePart))
                {
                    renderers[i].enabled = visible;
                }
            }
        }

        private static bool HasNamePart(Transform transform, Transform stopAt, string namePart)
        {
            var current = transform;
            while (current != null)
            {
                if (current.name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                if (current == stopAt)
                {
                    return false;
                }

                current = current.parent;
            }

            return false;
        }

        private static GameObject BuildPreviewScene()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.28f, 0.29f, 0.265f, 1f);
            RenderSettings.fog = false;

            var root = new GameObject("Step02 User Selected Corridor Sample")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            root.layer = PreviewLayer;

            var assets = SelectedAssets.Load();
            var materials = SelectedMaterials.Load();

            BuildStraightModule(root.transform, assets, materials);
            AddUnifiedExitWallClosure(root.transform, assets, materials);
            AddThresholdFrame(root.transform, materials, new Vector3(0f, 0f, -1.55f), Quaternion.identity, "Entry");
            AddThresholdFrame(root.transform, materials, new Vector3(0f, 0f, 8.95f), Quaternion.Euler(0f, 180f, 0f), "Exit");
            AddPreviewLighting(root.transform);
            return root;
        }

        private static void BuildStraightModule(Transform root, SelectedAssets assets, SelectedMaterials materials)
        {
            const int moduleCount = 7;
            const float step = 1.18f;
            const float startZ = 0f;

            for (var i = 0; i < moduleCount; i++)
            {
                var z = startZ + (i * step);
                InstantiateFitted(
                    assets.FloorBasePlate,
                    "Floor_5_base_Plate FBX base " + i,
                    root,
                    new Vector3(0f, 0.04f, z),
                    Quaternion.identity,
                    new Vector3(3.18f, 0.075f, 1.08f),
                    materials.BaseFloor);

                InstantiateFitted(
                    assets.Ceiling,
                    "TB_2 cargo ceiling panel " + i,
                    root,
                    new Vector3(0f, 2.42f, z),
                    Quaternion.identity,
                    new Vector3(3.25f, 0.32f, 1.12f),
                    materials.Ceiling);

                AddCleanWallBackingPair(root, materials, z, i);
                AddCleanWallPanelPair(root, assets, materials, z, i);
                AddCeilingDetailPair(root, assets, materials, z, i);
            }

            for (var i = 0; i <= moduleCount; i++)
            {
                AddWallGapFillerPair(root, assets, materials, startZ - (step * 0.5f) + (i * step), i);
            }

            AddDenseFloorOverlay(root, assets, materials, moduleCount, step);
        }

        private static void AddCleanWallBackingPair(Transform root, SelectedMaterials materials, float z, int index)
        {
            AddBox(
                root,
                "Left hidden no-overlap wall backing " + index,
                new Vector3(-1.69f, 1.16f, z),
                new Vector3(0.04f, 1.78f, 1.02f),
                materials.WallBacker);
            AddBox(
                root,
                "Right hidden no-overlap wall backing " + index,
                new Vector3(1.69f, 1.16f, z),
                new Vector3(0.04f, 1.78f, 1.02f),
                materials.WallBacker);
        }

        private static void AddCleanWallPanelPair(Transform root, SelectedAssets assets, SelectedMaterials materials, float z, int index)
        {
            AddWall(root, assets.Wall2, materials, "Wall 2 unified left " + index, -1.59f, z, false, new Vector3(0.13f, 1.82f, 0.98f));
            AddWall(root, assets.Wall2, materials, "Wall 2 unified right " + index, 1.59f, z, true, new Vector3(0.13f, 1.82f, 0.98f));
        }

        private static void AddWallGapFillerPair(Transform root, SelectedAssets assets, SelectedMaterials materials, float z, int index)
        {
            AddWall(root, assets.WallPillar, materials, "Wall seam filler unified left " + index, -1.59f, z, false, new Vector3(0.11f, 1.9f, 0.12f), 1.15f, materials.Frame);
            AddWall(root, assets.WallPillar, materials, "Wall seam filler unified right " + index, 1.59f, z, true, new Vector3(0.11f, 1.9f, 0.12f), 1.15f, materials.Frame);

            AddBox(root, "Clean floor seam rib " + index, new Vector3(0f, 0.18f, z), new Vector3(3.02f, 0.045f, 0.055f), materials.EdgeWear);
            AddBox(root, "Clean ceiling seam rib " + index, new Vector3(0f, 2.23f, z), new Vector3(3.02f, 0.075f, 0.055f), materials.DarkSeam);
        }

        private static void AddOpaqueWallBackingPair(Transform root, SelectedAssets assets, SelectedMaterials materials, float z, int index)
        {
            AddWall(
                root,
                assets.Wall2Variant,
                materials,
                "Wall 2 Variant opaque backer left " + index,
                -1.82f,
                z,
                false,
                new Vector3(0.16f, 2.05f, 1.16f),
                1.18f,
                materials.Wall);
            AddWall(
                root,
                assets.Wall2Variant,
                materials,
                "Wall 2 Variant inner liner left " + index,
                -1.49f,
                z,
                false,
                new Vector3(0.055f, 1.92f, 1.12f),
                1.16f,
                materials.Wall);
            AddWall(
                root,
                assets.Wall2Variant,
                materials,
                "Wall 2 Variant opaque backer right " + index,
                1.82f,
                z,
                true,
                new Vector3(0.16f, 2.05f, 1.16f),
                1.18f,
                materials.Wall);
            AddWall(
                root,
                assets.Wall2Variant,
                materials,
                "Wall 2 Variant inner liner right " + index,
                1.49f,
                z,
                true,
                new Vector3(0.055f, 1.92f, 1.12f),
                1.16f,
                materials.Wall);
        }

        private static void AddDenseFloorOverlay(Transform root, SelectedAssets assets, SelectedMaterials materials, int moduleCount, float step)
        {
            const int columnCount = 6;
            const int rowCount = 40;
            const float columnSpacing = 0.5f;
            const float rowSpacing = 0.22f;
            for (var row = 0; row < rowCount; row++)
            {
                var z = -0.62f + (row * rowSpacing);
                for (var column = 0; column < columnCount; column++)
                {
                    var x = -1.25f + (column * columnSpacing);
                    InstantiateFitted(
                        assets.FloorBase1F,
                        "Dense Floor Base 1 F overlay r" + row + " c" + column,
                        root,
                        new Vector3(x, 0.145f, z),
                        Quaternion.identity,
                        new Vector3(0.5f, 0.055f, 0.21f),
                        materials.TopFloor);
                }
            }
        }

        private static void AddFullWallPair(Transform root, SelectedAssets assets, SelectedMaterials materials, float z, int index)
        {
            AddWall(root, assets.Wall2, materials, "Wall 2 full left " + index, -1.61f, z, false, new Vector3(0.13f, 1.86f, 1.06f));
            AddWall(root, assets.Wall2, materials, "Wall 2 full right " + index, 1.61f, z, true, new Vector3(0.13f, 1.86f, 1.06f));
        }

        private static void AddHalfWallPair(Transform root, SelectedAssets assets, SelectedMaterials materials, float z, int index)
        {
            AddWall(root, assets.Wall2Half, materials, "Wall 2 Half lower left " + index, -1.61f, z, false, new Vector3(0.13f, 1.02f, 1.06f), 0.82f);
            AddWall(root, assets.Wall2Half, materials, "Wall 2 Half upper left " + index, -1.61f, z, false, new Vector3(0.13f, 0.9f, 1.06f), 1.65f);
            AddWall(root, assets.Wall2Half, materials, "Wall 2 Half lower right " + index, 1.61f, z, true, new Vector3(0.13f, 1.02f, 1.06f), 0.82f);
            AddWall(root, assets.Wall2Half, materials, "Wall 2 Half upper right " + index, 1.61f, z, true, new Vector3(0.13f, 0.9f, 1.06f), 1.65f);
        }

        private static void AddWallFillersPair(Transform root, SelectedAssets assets, SelectedMaterials materials, float z, int index)
        {
            AddWall(root, assets.Wall2HalfVariant, materials, "Wall 2 Half Variant mid fill left " + index, -1.53f, z - 0.29f, false, new Vector3(0.08f, 0.72f, 0.5f), 1.16f, materials.Wall);
            AddWall(root, assets.Wall2HalfVariant, materials, "Wall 2 Half Variant mid fill right " + index, 1.53f, z - 0.29f, true, new Vector3(0.08f, 0.72f, 0.5f), 1.16f, materials.Wall);
            AddWall(root, assets.Wall2HalfVariant, materials, "Wall 2 Half Variant rear fill left " + index, -1.53f, z + 0.29f, false, new Vector3(0.08f, 0.72f, 0.5f), 1.16f, materials.Wall);
            AddWall(root, assets.Wall2HalfVariant, materials, "Wall 2 Half Variant rear fill right " + index, 1.53f, z + 0.29f, true, new Vector3(0.08f, 0.72f, 0.5f), 1.16f, materials.Wall);

            AddWall(root, assets.WallPillar, materials, "Wall Pillar close seam left " + index, -1.46f, z - 0.53f, false, new Vector3(0.08f, 1.92f, 0.12f), 1.15f, materials.Frame);
            AddWall(root, assets.WallPillar, materials, "Wall Pillar close seam right " + index, 1.46f, z - 0.53f, true, new Vector3(0.08f, 1.92f, 0.12f), 1.15f, materials.Frame);
            AddWall(root, assets.WallPillar3, materials, "Wall Pillar rear seam left " + index, -1.46f, z + 0.53f, false, new Vector3(0.08f, 1.92f, 0.12f), 1.15f, materials.Frame);
            AddWall(root, assets.WallPillar3, materials, "Wall Pillar rear seam right " + index, 1.46f, z + 0.53f, true, new Vector3(0.08f, 1.92f, 0.12f), 1.15f, materials.Frame);

            AddWall(root, assets.Wall2Variant, materials, "Wall 2 Variant final solid liner left " + index, -1.38f, z, false, new Vector3(0.045f, 1.76f, 1.08f), 1.12f, materials.Wall);
            AddWall(root, assets.Wall2Variant, materials, "Wall 2 Variant final solid liner right " + index, 1.38f, z, true, new Vector3(0.045f, 1.76f, 1.08f), 1.12f, materials.Wall);
        }

        private static void AddUnifiedExitWallClosure(Transform root, SelectedAssets assets, SelectedMaterials materials)
        {
            const float z = 8.25f;
            for (var i = 0; i < 3; i++)
            {
                var x = -0.92f + (i * 0.92f);
                AddBox(
                    root,
                    "Clean exit hidden backing segment " + i,
                    new Vector3(x, 1.18f, z + 0.04f),
                    new Vector3(0.78f, 1.74f, 0.045f),
                    materials.WallBacker);
                InstantiateFitted(
                    assets.Wall2,
                    "Unified exit Wall 2 segment " + i,
                    root,
                    new Vector3(x, 1.18f, z),
                    Quaternion.Euler(0f, 90f, 0f),
                    new Vector3(0.13f, 1.74f, 0.78f),
                    materials.Wall);
            }

            for (var i = 0; i < 4; i++)
            {
                var x = -1.38f + (i * 0.92f);
                InstantiateFitted(
                    assets.WallPillar,
                    "Unified exit vertical gap filler " + i,
                    root,
                    new Vector3(x, 1.16f, z - 0.02f),
                    Quaternion.Euler(0f, 90f, 0f),
                    new Vector3(0.11f, 1.86f, 0.1f),
                    materials.Frame);
            }

            AddBox(root, "Exit lower kick plate", new Vector3(0f, 0.36f, z - 0.02f), new Vector3(3.18f, 0.22f, 0.08f), materials.EdgeWear);
            AddBox(root, "Exit upper dark header fill", new Vector3(0f, 2.08f, z - 0.02f), new Vector3(3.18f, 0.2f, 0.08f), materials.DarkSeam);
        }

        private static void AddWall(
            Transform root,
            GameObject prefab,
            SelectedMaterials materials,
            string name,
            float x,
            float z,
            bool rightSide,
            Vector3 size,
            float y = 1.2f,
            Material materialOverride = null)
        {
            InstantiateFitted(
                prefab,
                name,
                root,
                new Vector3(x, y, z),
                rightSide ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity,
                size,
                materialOverride ?? materials.Wall);
        }

        private static void AddCeilingDetailPair(Transform root, SelectedAssets assets, SelectedMaterials materials, float z, int index)
        {
            InstantiateFitted(
                assets.CeilingDetail,
                "Wall Top Piece ceiling left rail " + index,
                root,
                new Vector3(-1.18f, 2.18f, z),
                Quaternion.Euler(0f, 90f, 0f),
                new Vector3(0.18f, 0.22f, 1.05f),
                materials.CeilingTrim);
            InstantiateFitted(
                assets.CeilingDetail,
                "Wall Top Piece ceiling right rail " + index,
                root,
                new Vector3(1.18f, 2.18f, z),
                Quaternion.Euler(0f, -90f, 0f),
                new Vector3(0.18f, 0.22f, 1.05f),
                materials.CeilingTrim);
        }

        private static void AddWallSeamPair(Transform root, SelectedMaterials materials, float z, string name)
        {
            AddBox(root, "Left dark vertical " + name, new Vector3(-1.55f, 1.18f, z), new Vector3(0.08f, 2.05f, 0.065f), materials.DarkSeam);
            AddBox(root, "Right dark vertical " + name, new Vector3(1.55f, 1.18f, z), new Vector3(0.08f, 2.05f, 0.065f), materials.DarkSeam);
            AddBox(root, "Ceiling rib " + name, new Vector3(0f, 2.23f, z), new Vector3(3.0f, 0.09f, 0.08f), materials.DarkSeam);
            AddBox(root, "Floor rib " + name, new Vector3(0f, 0.18f, z), new Vector3(3.0f, 0.05f, 0.07f), materials.EdgeWear);
        }

        private static void AddThresholdFrame(Transform root, SelectedMaterials materials, Vector3 position, Quaternion rotation, string label)
        {
            AddBox(root, label + " left side post", position + (rotation * new Vector3(-1.64f, 1.16f, 0f)), new Vector3(0.22f, 2.22f, 0.22f), materials.Frame, rotation);
            AddBox(root, label + " right side post", position + (rotation * new Vector3(1.64f, 1.16f, 0f)), new Vector3(0.22f, 2.22f, 0.22f), materials.Frame, rotation);
            AddBox(root, label + " top lintel", position + (rotation * new Vector3(0f, 2.22f, 0f)), new Vector3(3.38f, 0.18f, 0.24f), materials.Frame, rotation);
            AddBox(root, label + " amber side light L", position + (rotation * new Vector3(-1.4f, 1.48f, -0.03f)), new Vector3(0.045f, 0.42f, 0.045f), materials.AmberLight, rotation);
            AddBox(root, label + " amber side light R", position + (rotation * new Vector3(1.4f, 1.48f, -0.03f)), new Vector3(0.045f, 0.42f, 0.045f), materials.AmberLight, rotation);
        }

        private static void AddPreviewLighting(Transform root)
        {
            AddDirectionalLight(root, "Warm corridor key light", new Vector3(-0.35f, -0.7f, -0.62f), new Color(1f, 0.88f, 0.68f, 1f), 1.1f);
            AddDirectionalLight(root, "Cool corridor rim light", new Vector3(0.35f, -0.5f, 0.7f), new Color(0.44f, 0.64f, 0.76f, 1f), 0.45f);

            for (var i = 0; i < 5; i++)
            {
                var lightObject = new GameObject("Preview ceiling point light " + i)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                lightObject.transform.SetParent(root, false);
                lightObject.transform.position = new Vector3(0f, 2.0f, 0.55f + (i * 1.55f));
                lightObject.layer = PreviewLayer;
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 3.1f;
                light.intensity = 1.25f;
                light.color = new Color(0.95f, 0.74f, 0.46f, 1f);
                light.cullingMask = 1 << PreviewLayer;
            }
        }

        private static void AddDirectionalLight(Transform root, string name, Vector3 forward, Color color, float intensity)
        {
            var lightObject = new GameObject(name)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            lightObject.transform.SetParent(root, false);
            lightObject.transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            lightObject.layer = PreviewLayer;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.cullingMask = 1 << PreviewLayer;
        }

        private static void AddBox(Transform root, string name, Vector3 position, Vector3 scale, Material material, Quaternion rotation = default)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.hideFlags = HideFlags.HideAndDontSave;
            box.transform.SetParent(root, false);
            box.transform.SetPositionAndRotation(position, rotation == default ? Quaternion.identity : rotation);
            box.transform.localScale = scale;
            box.layer = PreviewLayer;
            var collider = box.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            box.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void InstantiateFitted(
            GameObject prefab,
            string name,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            Vector3 targetLocalBounds,
            Material material)
        {
            if (prefab == null)
            {
                throw new InvalidOperationException("Missing selected corridor sample prefab: " + name);
            }

            var anchor = new GameObject(name)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            anchor.transform.SetParent(parent, false);
            anchor.transform.SetPositionAndRotation(position, rotation);
            anchor.layer = PreviewLayer;

            var child = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (child == null)
            {
                child = UnityEngine.Object.Instantiate(prefab);
            }

            child.name = name + " visual";
            child.hideFlags = HideFlags.HideAndDontSave;
            child.transform.SetParent(anchor.transform, false);
            SetLayerRecursive(child.transform, PreviewLayer);
            DisableColliders(child);
            DisableLightsAndCameras(child);
            ApplyMaterial(child, material);
            FitChildToLocalBounds(anchor.transform, child.transform, targetLocalBounds);
        }

        private static void DisableColliders(GameObject instance)
        {
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static void DisableLightsAndCameras(GameObject instance)
        {
            foreach (var light in instance.GetComponentsInChildren<Light>(true))
            {
                light.enabled = false;
            }

            foreach (var camera in instance.GetComponentsInChildren<Camera>(true))
            {
                camera.enabled = false;
            }
        }

        private static void ApplyMaterial(GameObject instance, Material material)
        {
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var sharedMaterials = renderer.sharedMaterials;
                if (sharedMaterials == null || sharedMaterials.Length == 0)
                {
                    renderer.sharedMaterial = material;
                    continue;
                }

                for (var i = 0; i < sharedMaterials.Length; i++)
                {
                    sharedMaterials[i] = material;
                }

                renderer.sharedMaterials = sharedMaterials;
            }
        }

        private static void FitChildToLocalBounds(Transform anchor, Transform child, Vector3 targetLocalBounds)
        {
            var bounds = CalculateLocalRenderBounds(anchor);
            var size = bounds.size;
            child.localScale = Vector3.Scale(
                child.localScale,
                new Vector3(
                    AxisScale(targetLocalBounds.x, size.x),
                    AxisScale(targetLocalBounds.y, size.y),
                    AxisScale(targetLocalBounds.z, size.z)));

            var fittedBounds = CalculateLocalRenderBounds(anchor);
            child.localPosition -= fittedBounds.center;
        }

        private static float AxisScale(float target, float current)
        {
            return current <= 0.001f ? 1f : Mathf.Clamp(target / current, 0.025f, 18f);
        }

        private static Bounds CalculateLocalRenderBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }

            var hasBounds = false;
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            for (var i = 0; i < renderers.Length; i++)
            {
                var rendererBounds = renderers[i].bounds;
                EncapsulateLocalPoint(root, ref bounds, ref hasBounds, rendererBounds.min);
                EncapsulateLocalPoint(root, ref bounds, ref hasBounds, rendererBounds.max);
                EncapsulateLocalPoint(root, ref bounds, ref hasBounds, new Vector3(rendererBounds.min.x, rendererBounds.min.y, rendererBounds.max.z));
                EncapsulateLocalPoint(root, ref bounds, ref hasBounds, new Vector3(rendererBounds.min.x, rendererBounds.max.y, rendererBounds.min.z));
                EncapsulateLocalPoint(root, ref bounds, ref hasBounds, new Vector3(rendererBounds.max.x, rendererBounds.min.y, rendererBounds.min.z));
                EncapsulateLocalPoint(root, ref bounds, ref hasBounds, new Vector3(rendererBounds.max.x, rendererBounds.max.y, rendererBounds.min.z));
                EncapsulateLocalPoint(root, ref bounds, ref hasBounds, new Vector3(rendererBounds.max.x, rendererBounds.min.y, rendererBounds.max.z));
                EncapsulateLocalPoint(root, ref bounds, ref hasBounds, new Vector3(rendererBounds.min.x, rendererBounds.max.y, rendererBounds.max.z));
            }

            return hasBounds ? bounds : new Bounds(Vector3.zero, Vector3.one);
        }

        private static void EncapsulateLocalPoint(Transform root, ref Bounds bounds, ref bool hasBounds, Vector3 worldPoint)
        {
            var localPoint = root.InverseTransformPoint(worldPoint);
            if (!hasBounds)
            {
                bounds = new Bounds(localPoint, Vector3.zero);
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(localPoint);
            }
        }

        private static void SetLayerRecursive(Transform transform, int layer)
        {
            transform.gameObject.layer = layer;
            for (var i = 0; i < transform.childCount; i++)
            {
                SetLayerRecursive(transform.GetChild(i), layer);
            }
        }

        private static void CaptureView(string path, Vector3 cameraPosition, Vector3 lookAt, float fieldOfView, bool orthographic, float orthographicSize)
        {
            var cameraObject = new GameObject("Selected Corridor Sample Camera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = cameraPosition;
            camera.transform.LookAt(lookAt);
            camera.fieldOfView = fieldOfView;
            camera.orthographic = orthographic;
            camera.orthographicSize = orthographicSize;
            camera.nearClipPlane = 0.02f;
            camera.farClipPlane = 60f;
            camera.cullingMask = 1 << PreviewLayer;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.058f, 0.052f, 1f);
            camera.allowHDR = false;
            camera.allowMSAA = true;

            try
            {
                CaptureCamera(camera, path, 1600, 900);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void CaptureCamera(Camera camera, string path, int width, int height)
        {
            var previousTargetTexture = camera.targetTexture;
            var previousActiveTexture = RenderTexture.active;
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);

            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTargetTexture;
                RenderTexture.active = previousActiveTexture;
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void WriteReadme(string outputRoot)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# 2단계 복도 사용자 지정 에셋 조합 시안 - 벽/격벽 패턴 통일 + 전폭 촘촘한 바닥판");
            builder.AppendLine();
            builder.AppendLine("이 시안은 사용자가 지정한 바닥/벽 조합을 실제 `CargoRunMvp` 씬에 적용하기 전 확인하기 위한 `artSample` 렌더입니다.");
            builder.AppendLine("이 수정판은 벽과 끝 격벽의 패턴이 뒤섞여 보이던 문제를 줄이기 위해 측면 벽과 출구 격벽을 모두 `Wall 2.FBX` 반복 패턴으로 통일했습니다.");
            builder.AppendLine("벽 패널은 같은 구간에 겹치지 않게 한 세트씩 배치하고, 이음매는 같은 기둥형 틈새 에셋으로 반복했습니다.");
            builder.AppendLine("벽의 구멍 뒤쪽에는 얇은 숨은 뒤판만 두어 외부 배경이 관통해 보이지 않게 했습니다.");
            builder.AppendLine("이번 버전은 `Floor Base 1 F.prefab`의 표시 크기를 한 번 더 줄이고 6열 고밀도 반복으로 바꿔 좌우 빈 공간이 거의 남지 않게 바닥 상부 패널을 채웠습니다.");
            builder.AppendLine("런타임 씬, 프리팹, 프로젝트 설정, 원본 Asset Store 파일은 수정하지 않았습니다.");
            builder.AppendLine();
            builder.AppendLine("## 구성");
            builder.AppendLine();
            builder.AppendLine("- 바닥 하부: `Floor_5_base_Plate.fbx` 반복");
            builder.AppendLine("- 바닥 상부: `Floor Base 1 F.prefab`를 더 작게 줄여 6열 40행 전폭 고밀도 반복");
            builder.AppendLine("- 벽: `Wall 2.FBX`만 반복해 측면 벽 패턴을 통일");
            builder.AppendLine("- 벽 틈새: `Wall Pillar.prefab`를 각 벽 모듈 사이에 같은 기둥형 이음매로 반복");
            builder.AppendLine("- 구멍 방지: 벽 뒤쪽에 얇은 숨은 뒤판을 배치하되, 표면 벽 패널과 같은 면에 겹쳐 놓지 않음");
            builder.AppendLine("- 격벽: 출구 쪽 폐쇄 패널도 `Wall 2.FBX`를 돌려 배치해 측면 벽과 같은 패턴으로 통일");
            builder.AppendLine("- 천장: 화물선 상부 패널처럼 보이는 `TB_2.prefab` 반복");
            builder.AppendLine("- 보조: 이음매/입구 프레임/렌더 조명은 검토용 임시 요소입니다.");
            builder.AppendLine();
            builder.AppendLine("## 검토 이미지");
            builder.AppendLine();
            builder.AppendLine("- `view_01_player_entry.png`: 플레이어 진입 시점");
            builder.AppendLine("- `view_02_floor_wall_diagonal.png`: 바닥과 벽 연결 대각 구도");
            builder.AppendLine("- `view_03_ceiling_and_wall_underlook.png`: 천장과 상부 벽 구도");
            builder.AppendLine("- `view_04_layout_topdown.png`: 천장을 숨긴 배치/동선 확인용 컷어웨이 상단 구도");
            builder.AppendLine("- `view_05_floor_stack_detail.png`: `Floor_5_base_Plate.fbx` 위에 더 작고 좌우 끝까지 촘촘하게 반복한 `Floor Base 1 F.prefab` 상세");
            File.WriteAllText(Path.Combine(outputRoot, "README.md"), builder.ToString(), new UTF8Encoding(false));
        }

        private static void WriteManifest(string outputRoot)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Asset Manifest");
            builder.AppendLine();
            builder.AppendLine("| 역할 | 파일 | 사용 방식 | 실제 적용 여부 |");
            builder.AppendLine("| --- | --- | --- | --- |");
            builder.AppendLine("| 바닥 하부 | `" + FloorBasePlateModelPath + "` | 복도 전체 바닥 베이스로 반복 배치 | 미적용 |");
            builder.AppendLine("| 바닥 상부 | `" + FloorBase1FPrefabPath + "` | 하부 바닥 위에 크기를 더 줄인 6열 40행 전폭 표면 레이어로 반복 배치 | 미적용 |");
            builder.AppendLine("| 측면 벽 패널 | `" + Wall2ModelPath + "` | 좌우 벽 전체를 같은 패턴의 반복 모듈로 배치 | 미적용 |");
            builder.AppendLine("| 출구 격벽 패널 | `" + Wall2ModelPath + "` | 복도 끝 격벽도 측면 벽과 같은 패턴으로 보이도록 회전 배치 | 미적용 |");
            builder.AppendLine("| 벽 틈새 기둥 | `" + WallPillarPrefabPath + "` | 벽 모듈 사이와 격벽 사이 빈 간격을 같은 세로 이음매 에셋으로 반복 | 미적용 |");
            builder.AppendLine("| 측면 관통 방지 뒤판 | Unity primitive + wall material | 벽 에셋 구멍 사이로 외부 배경이 보이지 않도록 벽 뒤쪽에 얇게 숨겨 배치 | 미적용 |");
            builder.AppendLine("| 천장 후보 | `" + CeilingPrefabPath + "` | 화물선 천장처럼 보이는 Top-Bottom 패널로 반복 배치 | 미적용 |");
            builder.AppendLine("| 천장 보조 레일 | `" + CeilingDetailPrefabPath + "` | 천장 양옆 보강 레일처럼 임시 배치 | 미적용 |");
            builder.AppendLine("| 이음매/문틀/조명 | Unity primitive/light | 구도 검토용 임시 요소 | 미적용 |");
            File.WriteAllText(Path.Combine(outputRoot, "ASSET_MANIFEST.md"), builder.ToString(), new UTF8Encoding(false));
        }

        private static void WriteApprovalStatus(string outputRoot)
        {
            var json = "{\n" +
                "  \"sampleName\": \"Step 02 corridor Floor_5_base_Plate + Wall 2 unified wall full-width tighter Floor Base 1 F sample\",\n" +
                "  \"createdDate\": \"2026-06-14\",\n" +
                "  \"approvalState\": \"미승인\",\n" +
                "  \"unityApplicationAllowed\": false,\n" +
                "  \"runtimeSceneModified\": false,\n" +
                "  \"reviewable\": true,\n" +
                "  \"userSelectedAssets\": true,\n" +
                "  \"nextStep\": \"사용자가 이 벽/격벽 패턴 통일 및 전폭 촘촘한 바닥판을 승인하면 같은 조합을 기준으로 CargoRunMvp 복도 적용안을 만든다.\"\n" +
                "}\n";
            File.WriteAllText(Path.Combine(outputRoot, "APPROVAL_STATUS.json"), json, new UTF8Encoding(false));
        }

        private static void WriteIndex(string outputRoot)
        {
            var builder = new StringBuilder();
            builder.AppendLine("<!doctype html>");
            builder.AppendLine("<html lang=\"ko\">");
            builder.AppendLine("<head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>2단계 복도 사용자 지정 시안</title>");
            builder.AppendLine("<style>body{margin:0;background:#151611;color:#e9e1cf;font-family:Georgia,'Times New Roman',serif}main{max-width:1280px;margin:0 auto;padding:24px}h1{font-size:28px;margin:0 0 8px}.meta{color:#b8ae96;margin:0 0 18px}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(360px,1fr));gap:16px}.card{border:1px solid #504b3c;background:#202219;border-radius:6px;overflow:hidden}.card img{display:block;width:100%;height:auto;background:#0e0f0c}.card p{margin:0;padding:12px 14px;color:#d7ceb8}.links{display:flex;gap:10px;margin:18px 0;flex-wrap:wrap}.links a{color:#f0c36a;border:1px solid #5d563f;padding:7px 10px;border-radius:4px;text-decoration:none}</style>");
            builder.AppendLine("</head><body><main>");
            builder.AppendLine("<h1>2단계 복도 사용자 지정 에셋 조합 시안 - 벽/격벽 패턴 통일 + 전폭 촘촘한 바닥판</h1>");
            builder.AppendLine("<p class=\"meta\">`Floor_5_base_Plate.fbx` 바닥, `Wall 2.FBX` 벽과 격벽, `TB_2.prefab` 천장, 더 작게 줄인 `Floor Base 1 F.prefab` 6열 상부 바닥 레이어를 사용한 승인 검토용 렌더입니다. 좌우 빈 공간이 거의 남지 않도록 바닥 폭 전체를 촘촘히 채웠고, 실제 게임 씬에는 적용하지 않았습니다.</p>");
            builder.AppendLine("<div class=\"links\"><a href=\"README.md\">README</a><a href=\"ASSET_MANIFEST.md\">ASSET_MANIFEST</a><a href=\"APPROVAL_STATUS.json\">APPROVAL_STATUS</a></div>");
            builder.AppendLine("<section class=\"grid\">");
            AddImageCard(builder, "view_01_player_entry.png", "플레이어 진입 시점");
            AddImageCard(builder, "view_02_floor_wall_diagonal.png", "바닥과 벽 연결 대각 구도");
            AddImageCard(builder, "view_03_ceiling_and_wall_underlook.png", "천장과 상부 벽 구도");
            AddImageCard(builder, "view_04_layout_topdown.png", "천장을 숨긴 배치/동선 확인용 컷어웨이 상단 구도");
            AddImageCard(builder, "view_05_floor_stack_detail.png", "작아진 Floor Base 1 F 6열 전폭 고밀도 바닥 상세");
            builder.AppendLine("</section></main></body></html>");
            File.WriteAllText(Path.Combine(outputRoot, "index.html"), builder.ToString(), new UTF8Encoding(false));
        }

        private static void AddImageCard(StringBuilder builder, string fileName, string caption)
        {
            builder.Append("<article class=\"card\"><a href=\"").Append(fileName).Append("\"><img src=\"").Append(fileName).Append("\" alt=\"").Append(caption).Append("\"></a><p>").Append(caption).Append("</p></article>");
            builder.AppendLine();
        }

        private sealed class SelectedAssets
        {
            public GameObject FloorBasePlate { get; private set; }
            public GameObject FloorBase1F { get; private set; }
            public GameObject Wall2 { get; private set; }
            public GameObject Wall2Half { get; private set; }
            public GameObject Wall2Variant { get; private set; }
            public GameObject Wall2HalfVariant { get; private set; }
            public GameObject WallPillar { get; private set; }
            public GameObject WallPillar3 { get; private set; }
            public GameObject Ceiling { get; private set; }
            public GameObject CeilingDetail { get; private set; }

            public static SelectedAssets Load()
            {
                return new SelectedAssets
                {
                    FloorBasePlate = LoadGameObject(FloorBasePlateModelPath),
                    FloorBase1F = LoadGameObject(FloorBase1FPrefabPath),
                    Wall2 = LoadGameObject(Wall2ModelPath),
                    Wall2Half = LoadGameObject(Wall2HalfModelPath),
                    Wall2Variant = LoadGameObject(Wall2VariantPrefabPath),
                    Wall2HalfVariant = LoadGameObject(Wall2HalfVariantPrefabPath),
                    WallPillar = LoadGameObject(WallPillarPrefabPath),
                    WallPillar3 = LoadGameObject(WallPillar3PrefabPath),
                    Ceiling = LoadGameObject(CeilingPrefabPath),
                    CeilingDetail = LoadGameObject(CeilingDetailPrefabPath)
                };
            }

            private static GameObject LoadGameObject(string path)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null)
                {
                    throw new InvalidOperationException("Missing selected corridor sample asset: " + path);
                }

                return asset;
            }
        }

        private sealed class SelectedMaterials
        {
            public Material BaseFloor { get; private set; }
            public Material TopFloor { get; private set; }
            public Material Wall { get; private set; }
            public Material WallBacker { get; private set; }
            public Material Ceiling { get; private set; }
            public Material CeilingTrim { get; private set; }
            public Material Frame { get; private set; }
            public Material DarkSeam { get; private set; }
            public Material EdgeWear { get; private set; }
            public Material AmberLight { get; private set; }

            public static SelectedMaterials Load()
            {
                return new SelectedMaterials
                {
                    BaseFloor = CreateTexturedMaterial("Floor_5_base_Plate preview floor", FloorAlbedoPath, FloorNormalPath, new Color(0.42f, 0.44f, 0.39f, 1f), 0.3f, 0.16f),
                    TopFloor = CreateTexturedMaterial("Floor Base 1 F dense overlay", FloorAlbedoPath, FloorNormalPath, new Color(0.58f, 0.57f, 0.5f, 1f), 0.35f, 0.18f),
                    Wall = CreateTexturedMaterial("Wall 2 URP preview wall", WallAlbedoPath, WallNormalPath, new Color(0.68f, 0.68f, 0.62f, 1f), 0.12f, 0.22f),
                    WallBacker = CreateTexturedMaterial("Wall 2 hidden solid backing preview wall", WallAlbedoPath, WallNormalPath, new Color(0.54f, 0.55f, 0.5f, 1f), 0.12f, 0.16f),
                    Ceiling = CreateTexturedMaterial("TB_2 cargo ceiling preview", CeilingAlbedoPath, CeilingNormalPath, new Color(0.42f, 0.43f, 0.39f, 1f), 0.24f, 0.14f),
                    CeilingTrim = CreateSolidMaterial("Ceiling trim dark metal", new Color(0.09f, 0.095f, 0.085f, 1f), 0.2f, 0.12f),
                    Frame = CreateSolidMaterial("Threshold frame dark metal", new Color(0.075f, 0.08f, 0.075f, 1f), 0.2f, 0.1f),
                    DarkSeam = CreateSolidMaterial("Dark seam mask", new Color(0.018f, 0.018f, 0.015f, 1f), 0f, 0.05f),
                    EdgeWear = CreateSolidMaterial("Floor edge wear", new Color(0.42f, 0.4f, 0.34f, 1f), 0.1f, 0.2f),
                    AmberLight = CreateEmissiveMaterial("Amber review light", new Color(1f, 0.53f, 0.16f, 1f))
                };
            }

            private static Material CreateTexturedMaterial(string name, string albedoPath, string normalPath, Color tint, float metallic, float smoothness)
            {
                var material = new Material(FindLitShader())
                {
                    name = name,
                    hideFlags = HideFlags.HideAndDontSave
                };
                ApplyColor(material, tint);
                ApplyFloat(material, "_Metallic", metallic);
                ApplyFloat(material, "_Smoothness", smoothness);
                ApplyFloat(material, "_Glossiness", smoothness);

                var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
                if (albedo != null)
                {
                    ApplyTexture(material, "_BaseMap", albedo);
                    ApplyTexture(material, "_MainTex", albedo);
                }

                var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                if (normal != null)
                {
                    ApplyTexture(material, "_BumpMap", normal);
                    ApplyFloat(material, "_BumpScale", 0.8f);
                    material.EnableKeyword("_NORMALMAP");
                }

                return material;
            }

            private static Material CreateSolidMaterial(string name, Color color, float metallic, float smoothness)
            {
                var material = new Material(FindLitShader())
                {
                    name = name,
                    hideFlags = HideFlags.HideAndDontSave
                };
                ApplyColor(material, color);
                ApplyFloat(material, "_Metallic", metallic);
                ApplyFloat(material, "_Smoothness", smoothness);
                ApplyFloat(material, "_Glossiness", smoothness);
                return material;
            }

            private static Material CreateEmissiveMaterial(string name, Color color)
            {
                var material = CreateSolidMaterial(name, color, 0f, 0.25f);
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", color * 1.75f);
                    material.EnableKeyword("_EMISSION");
                }

                return material;
            }

            private static Shader FindLitShader()
            {
                return Shader.Find("Universal Render Pipeline/Lit") ??
                    Shader.Find("Standard") ??
                    Shader.Find("Unlit/Texture");
            }

            private static void ApplyColor(Material material, Color color)
            {
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", color);
                }

                if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", color);
                }
            }

            private static void ApplyTexture(Material material, string propertyName, Texture texture)
            {
                if (material.HasProperty(propertyName))
                {
                    material.SetTexture(propertyName, texture);
                }
            }

            private static void ApplyFloat(Material material, string propertyName, float value)
            {
                if (material.HasProperty(propertyName))
                {
                    material.SetFloat(propertyName, value);
                }
            }
        }
    }
}
