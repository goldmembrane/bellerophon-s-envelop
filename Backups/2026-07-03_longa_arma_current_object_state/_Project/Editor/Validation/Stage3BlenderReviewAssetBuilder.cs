using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Bellerophon.Editor.Validation
{
    internal static class Stage3BlenderReviewAssetBuilder
    {
        public const string RootDirectory = "Assets/_Project/Art/Props/Stage3Rework";
        public const string BlenderSourcePath = RootDirectory + "/BlenderSource/Stage3Rework_All.blend";
        public const string FbxDirectory = RootDirectory + "/Fbx";
        public const string TextureDirectory = RootDirectory + "/Textures";
        public const string MeshLibraryFbxPath = FbxDirectory + "/FBX_Stage3Rework_MeshLibrary.fbx";

        public const string BeveledBoxMeshName = "BM_BeveledBox_Unit";
        public const string CylinderMeshName = "BM_Cylinder_Unit";
        public const string HookedCrowbarBodyMeshName = "BM_HookedCrowbar_Body";

        private const string DarkMetalTexturePath = TextureDirectory + "/HD_Stage3_DarkWornMetal_Albedo.png";
        private const string BrightMetalTexturePath = TextureDirectory + "/HD_Stage3_BrightScratchedMetal_Albedo.png";
        private const string RubberTexturePath = TextureDirectory + "/HD_Stage3_BlackRibbedRubber_Albedo.png";
        private const string ScreenTexturePath = TextureDirectory + "/HD_Stage3_GreenCrtScreen_Albedo.png";
        private const string HazardTexturePath = TextureDirectory + "/HD_Stage3_RedBlackHazard_Albedo.png";
        private const string YellowTexturePath = TextureDirectory + "/HD_Stage3_WornYellowPaint_Albedo.png";
        private const string RedTexturePath = TextureDirectory + "/HD_Stage3_WornRedPaint_Albedo.png";
        private const string CargoTexturePath = TextureDirectory + "/HD_Stage3_BlueGrayCargoMetal_Albedo.png";
        private const string WoodTexturePath = TextureDirectory + "/HD_Stage3_WornWeaponWood_Albedo.png";

        public static string[] GetRequiredFbxPaths()
        {
            return new[]
            {
                MeshLibraryFbxPath,
                FbxDirectory + "/FBX_01_CockpitHelmAndStatus.fbx",
                FbxDirectory + "/FBX_02_ControlRoomCCTV.fbx",
                FbxDirectory + "/FBX_03_EnginePowerTerminal.fbx",
                FbxDirectory + "/FBX_04_SupplyStorageCabinet.fbx",
                FbxDirectory + "/FBX_05_CargoHoldPropsAndTerminal.fbx",
                FbxDirectory + "/FBX_06_ArmoryTurretGripMount.fbx",
                FbxDirectory + "/FBX_07_FirstPersonEquipment.fbx",
            };
        }

        public static string[] GetRequiredTexturePaths()
        {
            return new[]
            {
                DarkMetalTexturePath,
                BrightMetalTexturePath,
                RubberTexturePath,
                ScreenTexturePath,
                HazardTexturePath,
                YellowTexturePath,
                RedTexturePath,
                CargoTexturePath,
                WoodTexturePath,
            };
        }

        public static void EnsureAssets()
        {
            AssertGeneratedBlenderAssetsExist();
            ConfigureFbxImporters();
            ConfigureTextures();
            ApplyHighDefinitionMaterialTextures();
            ApplyHighDefinitionTexturesToExistingShipMaterials();
            _ = LoadNamedMesh(BeveledBoxMeshName);
            _ = LoadNamedMesh(CylinderMeshName);
            _ = LoadNamedMesh(HookedCrowbarBodyMeshName);
        }

        public static Mesh LoadPrimitiveMesh(PrimitiveType primitiveType)
        {
            switch (primitiveType)
            {
                case PrimitiveType.Cube:
                    return LoadNamedMesh(BeveledBoxMeshName);
                case PrimitiveType.Cylinder:
                    return LoadNamedMesh(CylinderMeshName);
                default:
                    throw new InvalidOperationException("Stage 3 Blender review mesh library does not support primitive type: " + primitiveType);
            }
        }

        public static Mesh LoadNamedMesh(string meshName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(MeshLibraryFbxPath);
            for (var i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Mesh mesh && mesh.name == meshName)
                {
                    return mesh;
                }
            }

            throw new InvalidOperationException("Missing Stage 3 Blender FBX mesh '" + meshName + "' in " + MeshLibraryFbxPath);
        }

        public static bool IsBlenderFbxMesh(Mesh mesh)
        {
            if (mesh == null)
            {
                return false;
            }

            var path = AssetDatabase.GetAssetPath(mesh).Replace('\\', '/');
            return path.StartsWith(FbxDirectory + "/", StringComparison.Ordinal);
        }

        private static void AssertGeneratedBlenderAssetsExist()
        {
            AssertAssetFile(BlenderSourcePath);
            var fbxPaths = GetRequiredFbxPaths();
            for (var i = 0; i < fbxPaths.Length; i++)
            {
                AssertAssetFile(fbxPaths[i]);
            }

            var texturePaths = GetRequiredTexturePaths();
            for (var i = 0; i < texturePaths.Length; i++)
            {
                AssertAssetFile(texturePaths[i]);
            }
        }

        private static void AssertAssetFile(string assetPath)
        {
            var fullPath = ToFullPath(assetPath);
            if (!File.Exists(fullPath))
            {
                throw new InvalidOperationException("Missing Stage 3 Blender-generated asset: " + assetPath);
            }
        }

        private static void ConfigureTextures()
        {
            var texturePaths = GetRequiredTexturePaths();
            for (var i = 0; i < texturePaths.Length; i++)
            {
                var importer = AssetImporter.GetAtPath(texturePaths[i]) as TextureImporter;
                if (importer == null)
                {
                    AssetDatabase.ImportAsset(texturePaths[i], ImportAssetOptions.ForceUpdate);
                    importer = AssetImporter.GetAtPath(texturePaths[i]) as TextureImporter;
                }

                if (importer == null)
                {
                    throw new InvalidOperationException("Could not configure Stage 3 Blender texture import settings: " + texturePaths[i]);
                }

                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = true;
                importer.sRGBTexture = true;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureFbxImporters()
        {
            var fbxPaths = GetRequiredFbxPaths();
            for (var i = 0; i < fbxPaths.Length; i++)
            {
                var importer = AssetImporter.GetAtPath(fbxPaths[i]) as ModelImporter;
                if (importer == null)
                {
                    AssetDatabase.ImportAsset(fbxPaths[i], ImportAssetOptions.ForceUpdate);
                    importer = AssetImporter.GetAtPath(fbxPaths[i]) as ModelImporter;
                }

                if (importer == null)
                {
                    throw new InvalidOperationException("Could not configure Stage 3 Blender FBX importer: " + fbxPaths[i]);
                }

                importer.globalScale = 100f;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
        }

        private static void ApplyHighDefinitionMaterialTextures()
        {
            ApplyMaterialTexture(PostDetailedStage3GameplayPropsBootstrap.MetalMaterialPath, DarkMetalTexturePath, new Color(0.075f, 0.076f, 0.068f, 1f), false, 0.52f, 0.24f);
            ApplyMaterialTexture(PostDetailedStage3GameplayPropsBootstrap.DarkRubberMaterialPath, RubberTexturePath, new Color(0.014f, 0.014f, 0.013f, 1f), false, 0.02f, 0.44f);
            ApplyMaterialTexture(PostDetailedStage3GameplayPropsBootstrap.ScreenMaterialPath, ScreenTexturePath, new Color(0.035f, 0.46f, 0.23f, 1f), true, 0f, 0.30f);
            ApplyMaterialTexture(PostDetailedStage3GameplayPropsBootstrap.WarningMaterialPath, RedTexturePath, new Color(0.55f, 0.04f, 0.028f, 1f), false, 0.14f, 0.32f);
            ApplyMaterialTexture(PostDetailedStage3GameplayPropsBootstrap.YellowMaterialPath, YellowTexturePath, new Color(0.68f, 0.49f, 0.075f, 1f), false, 0.14f, 0.32f);
            ApplyMaterialTexture(PostDetailedStage3GameplayPropsBootstrap.WoodMaterialPath, WoodTexturePath, new Color(0.32f, 0.19f, 0.1f, 1f), false, 0f, 0.6f);
            ApplyMaterialTexture(PostDetailedStage3GameplayPropsBootstrap.CrowbarSteelMaterialPath, BrightMetalTexturePath, new Color(0.48f, 0.49f, 0.44f, 1f), false, 0.64f, 0.22f);
            ApplyMaterialTexture(PostDetailedStage3GameplayPropsBootstrap.DamagedMaterialPath, DarkMetalTexturePath, new Color(0.055f, 0.047f, 0.038f, 1f), false, 0.34f, 0.28f);
            ApplyMaterialTexture(PostDetailedStage3GameplayPropsBootstrap.LightMaterialPath, ScreenTexturePath, new Color(0.12f, 0.62f, 0.46f, 1f), true, 0f, 0.26f);
            ApplyMaterialTexture(PostDetailedStage3GameplayPropsBootstrap.CargoMaterialPath, CargoTexturePath, new Color(0.11f, 0.125f, 0.12f, 1f), false, 0.42f, 0.26f);
        }

        private static void ApplyHighDefinitionTexturesToExistingShipMaterials()
        {
            ApplyExistingMaterialTexture("Assets/_Project/Settings/Ship/GrayboxFloorMaterial.mat", DarkMetalTexturePath, new Color(0.045f, 0.048f, 0.043f, 1f), 0.34f, 0.28f);
            ApplyExistingMaterialTexture("Assets/_Project/Settings/Ship/GrayboxCorridorMaterial.mat", DarkMetalTexturePath, new Color(0.035f, 0.038f, 0.036f, 1f), 0.34f, 0.28f);
            ApplyExistingMaterialTexture("Assets/_Project/Settings/Ship/GrayboxWallMaterial.mat", DarkMetalTexturePath, new Color(0.052f, 0.054f, 0.049f, 1f), 0.40f, 0.26f);
            ApplyExistingMaterialTexture("Assets/_Project/Settings/Ship/GrayboxConsoleMaterial.mat", DarkMetalTexturePath, new Color(0.025f, 0.030f, 0.028f, 1f), 0.40f, 0.24f);
            ApplyExistingMaterialTexture("Assets/_Project/Settings/Ship/GrayboxCargoMaterial.mat", CargoTexturePath, new Color(0.13f, 0.13f, 0.105f, 1f), 0.34f, 0.28f);
            ApplyExistingMaterialTexture("Assets/_Project/Settings/Ship/GrayboxInteractableMaterial.mat", YellowTexturePath, new Color(0.34f, 0.25f, 0.065f, 1f), 0.12f, 0.30f);
            ApplyExistingMaterialTexture("Assets/_Project/Settings/Ship/Phase20AccentMaterial.mat", YellowTexturePath, new Color(0.44f, 0.30f, 0.07f, 1f), 0.14f, 0.30f);
            ApplyExistingMaterialTexture("Assets/_Project/Settings/Ship/Phase20GlassFrameMaterial.mat", BrightMetalTexturePath, new Color(0.13f, 0.13f, 0.115f, 1f), 0.52f, 0.26f);
            ApplyExistingMaterialTexture("Assets/_Project/Settings/Ship/Phase20ScreenMaterial.mat", ScreenTexturePath, new Color(0.02f, 0.20f, 0.13f, 1f), 0f, 0.34f);
            ApplyExistingMaterialTexture("Assets/_Project/Settings/Ship/Phase20WarningMaterial.mat", RedTexturePath, new Color(0.42f, 0.045f, 0.03f, 1f), 0.14f, 0.32f);
            ApplyExistingMaterialTexture("Assets/_Project/Art/Ship/Materials/ShipInteriorFloor_Rough.mat", DarkMetalTexturePath, new Color(0.065f, 0.068f, 0.062f, 1f), 0.34f, 0.26f);
            ApplyExistingMaterialTexture("Assets/_Project/Art/Ship/Materials/ShipInteriorCorridorFloor_Rough.mat", DarkMetalTexturePath, new Color(0.052f, 0.055f, 0.052f, 1f), 0.34f, 0.26f);
            ApplyExistingMaterialTexture("Assets/_Project/Art/Ship/Materials/ShipInteriorWall_Rough.mat", DarkMetalTexturePath, new Color(0.072f, 0.073f, 0.066f, 1f), 0.40f, 0.26f);
            ApplyExistingMaterialTexture("Assets/_Project/Art/Ship/Materials/ShipInteriorCeiling_Rough.mat", DarkMetalTexturePath, new Color(0.042f, 0.045f, 0.042f, 1f), 0.34f, 0.24f);
            ApplyExistingMaterialTexture("Assets/_Project/Art/Ship/Materials/ShipInteriorDoorFrame_Worn.mat", BrightMetalTexturePath, new Color(0.16f, 0.155f, 0.135f, 1f), 0.52f, 0.24f);
            ApplyExistingMaterialTexture("Assets/_Project/Art/Ship/Materials/ShipInteriorCableTray_Dark.mat", RubberTexturePath, new Color(0.020f, 0.020f, 0.018f, 1f), 0.02f, 0.46f);
            ApplyExistingMaterialTexture("Assets/_Project/Art/Ship/Materials/ShipInteriorConsole_Aged.mat", DarkMetalTexturePath, new Color(0.045f, 0.047f, 0.042f, 1f), 0.40f, 0.24f);
            ApplyExistingMaterialTexture("Assets/_Project/Art/Ship/Materials/ShipInteriorCargo_Worn.mat", CargoTexturePath, new Color(0.17f, 0.18f, 0.16f, 1f), 0.34f, 0.26f);
            ApplyExistingMaterialTexture("Assets/_Project/Art/Ship/Materials/ShipInteriorDamageState_Warning.mat", HazardTexturePath, new Color(0.30f, 0.10f, 0.04f, 1f), 0.14f, 0.30f);
            ApplyExistingMaterialTexture("Assets/_Project/Art/Ship/Materials/ShipInteriorInteractable_WornYellow.mat", YellowTexturePath, new Color(0.44f, 0.31f, 0.08f, 1f), 0.12f, 0.30f);
        }

        private static void ApplyMaterialTexture(
            string materialPath,
            string texturePath,
            Color tint,
            bool emissive,
            float metallic,
            float smoothness)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            var shaderToUse = Shader.Find("Universal Render Pipeline/Lit");
            if (shaderToUse != null && material.shader != shaderToUse)
            {
                material.shader = shaderToUse;
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                throw new InvalidOperationException("Missing Stage 3 Blender texture for material: " + texturePath);
            }

            material.color = tint;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", tint);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", tint);
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", tint * 2.6f);
                }

                if (material.HasProperty("_EmissionMap"))
                {
                    material.SetTexture("_EmissionMap", texture);
                }
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(material);
        }

        private static void ApplyExistingMaterialTexture(
            string materialPath,
            string texturePath,
            Color tint,
            float metallic,
            float smoothness)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                return;
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                throw new InvalidOperationException("Missing Stage 3 texture for existing ship material: " + texturePath);
            }

            material.color = tint;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", tint);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", tint);
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            EditorUtility.SetDirty(material);
        }

        private static string ToFullPath(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for Stage 3 Blender asset validation.");
            }

            return Path.Combine(projectRoot.FullName, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
