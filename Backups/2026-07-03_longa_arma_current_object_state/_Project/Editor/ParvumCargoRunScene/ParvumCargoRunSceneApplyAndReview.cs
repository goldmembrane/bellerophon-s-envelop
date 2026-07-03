using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Bellerophon.Core.Player;
using Bellerophon.Enemies.Parvum;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.ParvumCargoRunScene
{
    public static class ParvumCargoRunSceneApplyAndReview
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string CorridorRootName = "Approved Ship Corridor Segments";
        private const string PlacementRootName = "Approved Parvum Enemy Placement";
        private const string LegacyParvumVisualName = "Parvum Intruder Visual";
        private const string ReviewCameraName = "Model Cam";
        private const string SampleRootRelativePath = "artSample/enemies/parvum_physics_rig_rework_sample";
        private const string ApprovalStatusRelativePath = SampleRootRelativePath + "/APPROVAL_STATUS.json";
        private const string SourceModelRelativePath = SampleRootRelativePath + "/exports/parvum_physics_rig_rework_sample.fbx";
        private const string SourceTextureRootRelativePath = SampleRootRelativePath + "/textures";
        private const string ArtRootPath = "Assets/_Project/Art/Enemies/Parvum";
        private const string ModelAssetPath = ArtRootPath + "/Models/parvum.fbx";
        private const string RuntimeBlendShapeMeshPath = ArtRootPath + "/Models/parvum_runtime_blendshape_mesh.asset";
        private const string TextureRootPath = ArtRootPath + "/Textures";
        private const string MaterialRootPath = ArtRootPath + "/Materials";
        private const string AnimationRootPath = ArtRootPath + "/Animations";
        private const string AnimatorControllerRootPath = AnimationRootPath + "/Controllers";
        private const string PrefabRootPath = "Assets/_Project/Prefabs/Enemies/Parvum";
        private const string PrefabPath = PrefabRootPath + "/ParvumApproved.prefab";
        private const string ReviewRootRelativePath = "docs/validation/parvum_cargo_run_scene";
        private const string ReviewCaptureRelativePath = ReviewRootRelativePath + "/captures";
        private const string ModelChildPath = "ParvumApproved_Model";
        private const string UnifiedVisibleMeshName = "Unified_Parvum_Reference_Matched_Single_Mesh";
        private const string MotionHelperRootName = "Parvum_Physics_Motion_Helper_Targets";
        private const string MotionPathTargetName = "MotionPath_Target_Rigidbody_Goal";
        private const string MouthIkTargetName = "AnimationRigging_Mouth_IK_Target";
        private const string JointTargetName = "ConfigurableJoint_Mouth_Limit_Target";
        private const string JiggleLeftTargetName = "Jiggle_Surface_Left_Target";
        private const string JiggleRightTargetName = "Jiggle_Surface_Right_Target";
        private const string JiggleRearTargetName = "Jiggle_Rear_Mass_Target";
        private const int RequiredPlacedCount = 6;
        private const float ParvumSceneScale = 200f;
        private const float ParvumPlacementSpacing = 3f;
        private const float MinimumCorridorZGap = 2.35f;
        private const float GroundContactTolerance = 0.04f;
        private const float ParvumOppositeFacingYaw = 180f;
        private const int SerializedRectangleAreaLightType = 2;
        private const int SerializedDiscAreaLightType = 3;
        private const int SerializedBakedLightmappingMode = 2;
        private const string IdleBlendShapeName = "Idle_Pulse_Surface_Jiggle";
        private const string MoveBlendShapeName = "Move_Squash_Forward_Slosh";
        private const string AttackBlendShapeName = "Attack_Bite_Core_Kick";
        private const string HitBlendShapeName = "Hit_Recoil_Side_Wave";
        private const string DeathBlendShapeName = "Death_Flatten_Liquid_Spread";
        private const string AttackTeethChompBlendShapeName = "Attack_Teeth_Chomp";
        private const string AttackMouthWideOpenBlendShapeName = "Attack_Mouth_Wide_Open";
        private const string HitSlowRecoilBlendShapeName = "Hit_Slow_Recoil";
        private const string DeathLiquefyCollapseBlendShapeName = "Death_Liquefy_Collapse";
        private const string DeathMouthDissolveBlendShapeName = "Death_Mouth_Dissolve";

        private static readonly Vector3 ParvumModelFacingEuler = new Vector3(-90f, 0f, 0f);

        private static readonly string[] TextureFileNames =
        {
            "parvum_slime_albedo.png",
            "parvum_slime_roughness.png",
            "parvum_slime_bump.png",
            "parvum_white_fleck_mask.png",
            "parvum_muzzle_scale_albedo.png",
            "parvum_muzzle_scale_bump.png",
            "parvum_mouth_cavity_albedo.png",
            "parvum_tooth_albedo.png",
            "parvum_tongue_albedo.png",
        };

        private static readonly PlacementSpec[] PlacementSpecs =
        {
            new PlacementSpec("Parvum_00_Static", null, null),
            new PlacementSpec("Parvum_01_Idle", "Parvum_Idle", IdleBlendShapeName),
            new PlacementSpec("Parvum_02_Move", "Parvum_Move", MoveBlendShapeName),
            new PlacementSpec("Parvum_03_Attack", "Parvum_Attack", AttackBlendShapeName),
            new PlacementSpec("Parvum_04_Hit", "Parvum_Hit", HitBlendShapeName),
            new PlacementSpec("Parvum_05_Death", "Parvum_Death", DeathBlendShapeName),
        };

        [MenuItem("Bellerophon/Parvum/Apply Approved Sample To Current CargoRun Scene")]
        public static void ApplyApprovedSampleToCurrentCargoRunScene()
        {
            RequireApprovedSampleFiles();
            EnsureAssetFolders();
            CopyApprovedSampleAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            ConfigureTextureImportSettings();
            ConfigureModelImportSettings();

            var materials = EnsureMaterials();
            var prefab = EnsurePrefab(materials);
            var clips = EnsureAnimationClips(prefab);
            var controllers = EnsureAnimatorControllers(clips);

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            ConfigureCargoRunAreaLightsForUrp(scene);
            var corridorRoot = RequireObject(CorridorRootName);
            var corridorBounds = GetRendererBounds(corridorRoot.transform);
            var targetPositions = BuildPlacementPositions(corridorBounds);
            var placementRoot = GetOrCreatePlacementRoot(targetPositions);
            ClearPlacementChildren(placementRoot.transform);
            RemoveLegacyParvumVisuals(placementRoot.transform);

            placementRoot.transform.SetPositionAndRotation(GetAveragePosition(targetPositions), Quaternion.identity);
            placementRoot.transform.localScale = Vector3.one;

            for (var i = 0; i < PlacementSpecs.Length; i++)
            {
                var spec = PlacementSpecs[i];
                var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (instance == null)
                {
                    instance = UnityEngine.Object.Instantiate(prefab);
                }

                instance.name = spec.ObjectName;
                instance.transform.SetParent(placementRoot.transform, true);
                instance.transform.SetPositionAndRotation(targetPositions[i], Quaternion.Euler(0f, ParvumOppositeFacingYaw, 0f));
                instance.transform.localScale = Vector3.one * ParvumSceneScale;
                EnsureScenePhysicsComponents(instance.transform);

                if (string.IsNullOrEmpty(spec.AnimationClipName))
                {
                    RemoveAnimatorController(instance);
                }
                else
                {
                    ConfigureAnimation(instance, controllers[spec.AnimationClipName]);
                    ConfigureMotionTargetForState(instance.transform, spec.AnimationClipName);
                }

                AlignObjectBottomToY(instance.transform, corridorBounds.min.y);
            }

            ConfigureInitialParvumReviewCamera(placementRoot.transform);

            var review = InspectAppliedSceneState(placementRoot.transform, corridorBounds, clips);
            WriteReviewFiles(review);
            CaptureReviewImages(placementRoot.transform, corridorBounds);

            SelectAndFramePlacedObjects(placementRoot.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Current CargoRun Parvum scene application completed. Root=" +
                PlacementRootName +
                "; Count=" +
                RequiredPlacedCount.ToString(CultureInfo.InvariantCulture) +
                "; Static=1; Animations=Idle,Move,Attack,Hit,Death; OldHarnessEditModePlayModeBuild=NotRun; Review=" +
                ReviewRootRelativePath);
        }

        private static void RequireApprovedSampleFiles()
        {
            var approvalPath = ToProjectAbsolutePath(ApprovalStatusRelativePath);
            if (!File.Exists(approvalPath))
            {
                throw new InvalidOperationException("Missing Parvum approval status file: " + ApprovalStatusRelativePath);
            }

            var approvalText = File.ReadAllText(approvalPath);
            if (!approvalText.Contains("\"unityApplicationAllowed\": true", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Parvum sample is not marked as allowed for Unity application.");
            }

            if (!File.Exists(ToProjectAbsolutePath(SourceModelRelativePath)))
            {
                throw new InvalidOperationException("Missing approved Parvum FBX source: " + SourceModelRelativePath);
            }

            for (var i = 0; i < TextureFileNames.Length; i++)
            {
                var sourcePath = SourceTextureRootRelativePath + "/" + TextureFileNames[i];
                if (!File.Exists(ToProjectAbsolutePath(sourcePath)))
                {
                    throw new InvalidOperationException("Missing approved Parvum texture source: " + sourcePath);
                }
            }
        }

        private static void EnsureAssetFolders()
        {
            EnsureAssetDirectory(ArtRootPath);
            EnsureAssetDirectory(ArtRootPath + "/Models");
            EnsureAssetDirectory(TextureRootPath);
            EnsureAssetDirectory(MaterialRootPath);
            EnsureAssetDirectory(AnimationRootPath);
            EnsureAssetDirectory(AnimatorControllerRootPath);
            EnsureAssetDirectory(PrefabRootPath);
            Directory.CreateDirectory(ToProjectAbsolutePath(ReviewRootRelativePath));
            Directory.CreateDirectory(ToProjectAbsolutePath(ReviewCaptureRelativePath));
        }

        private static void EnsureAssetDirectory(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var parts = assetPath.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static void CopyApprovedSampleAssets()
        {
            CopyAssetFile(SourceModelRelativePath, ModelAssetPath);
            for (var i = 0; i < TextureFileNames.Length; i++)
            {
                CopyAssetFile(SourceTextureRootRelativePath + "/" + TextureFileNames[i], TextureRootPath + "/" + TextureFileNames[i]);
            }
        }

        private static void CopyAssetFile(string sourceRelativePath, string targetAssetPath)
        {
            var sourcePath = ToProjectAbsolutePath(sourceRelativePath);
            var targetPath = ToProjectAbsolutePath(targetAssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
            File.Copy(sourcePath, targetPath, true);
        }

        private static void ConfigureTextureImportSettings()
        {
            ConfigureTexture(TextureRootPath + "/parvum_slime_albedo.png", false, true);
            ConfigureTexture(TextureRootPath + "/parvum_slime_roughness.png", false, false);
            ConfigureTexture(TextureRootPath + "/parvum_slime_bump.png", true, false);
            ConfigureTexture(TextureRootPath + "/parvum_white_fleck_mask.png", false, false);
            ConfigureTexture(TextureRootPath + "/parvum_muzzle_scale_albedo.png", false, true);
            ConfigureTexture(TextureRootPath + "/parvum_muzzle_scale_bump.png", true, false);
            ConfigureTexture(TextureRootPath + "/parvum_mouth_cavity_albedo.png", false, true);
            ConfigureTexture(TextureRootPath + "/parvum_tooth_albedo.png", false, true);
            ConfigureTexture(TextureRootPath + "/parvum_tongue_albedo.png", false, true);
        }

        private static void ConfigureTexture(string assetPath, bool normalMap, bool srgb)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Missing TextureImporter for " + assetPath);
            }

            var changed = false;
            var expectedType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType != expectedType)
            {
                importer.textureType = expectedType;
                changed = true;
            }

            if (importer.sRGBTexture != srgb)
            {
                importer.sRGBTexture = srgb;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureModelImportSettings()
        {
            AssetDatabase.ImportAsset(ModelAssetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(ModelAssetPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Missing ModelImporter for " + ModelAssetPath);
            }

            var changed = false;
            if (!importer.importBlendShapes)
            {
                importer.importBlendShapes = true;
                changed = true;
            }

            if (importer.importAnimation)
            {
                importer.importAnimation = false;
                changed = true;
            }

            if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportStandard)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                changed = true;
            }

            if (!importer.isReadable)
            {
                importer.isReadable = true;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static MaterialSet EnsureMaterials()
        {
            var slimeAlbedo = LoadTexture(TextureRootPath + "/parvum_slime_albedo.png");
            var slimeBump = LoadTexture(TextureRootPath + "/parvum_slime_bump.png");
            var muzzleAlbedo = LoadTexture(TextureRootPath + "/parvum_muzzle_scale_albedo.png");
            var muzzleBump = LoadTexture(TextureRootPath + "/parvum_muzzle_scale_bump.png");
            var mouthAlbedo = LoadTexture(TextureRootPath + "/parvum_mouth_cavity_albedo.png");
            var toothAlbedo = LoadTexture(TextureRootPath + "/parvum_tooth_albedo.png");
            var tongueAlbedo = LoadTexture(TextureRootPath + "/parvum_tongue_albedo.png");

            return new MaterialSet(
                EnsureMaterial(
                    MaterialRootPath + "/M_Parvum_Wet_Marbled_Green_Slime_Texture.mat",
                    "M_Parvum_Wet_Marbled_Green_Slime_Texture",
                    new Color(0.16f, 0.92f, 0.48f, 0.62f),
                    slimeAlbedo,
                    slimeBump,
                    true,
                    0.42f),
                EnsureMaterial(
                    MaterialRootPath + "/M_Parvum_Dark_Green_Internal_Marbling.mat",
                    "M_Parvum_Dark_Green_Internal_Marbling",
                    new Color(0.0f, 0.18f, 0.08f, 0.62f),
                    slimeAlbedo,
                    slimeBump,
                    true,
                    0.58f),
                EnsureMaterial(
                    MaterialRootPath + "/M_Parvum_Green_Grey_Muzzle_Edge_Blend.mat",
                    "M_Parvum_Green_Grey_Muzzle_Edge_Blend",
                    new Color(0.36f, 0.52f, 0.36f, 0.92f),
                    muzzleAlbedo,
                    muzzleBump,
                    false,
                    0.76f),
                EnsureMaterial(
                    MaterialRootPath + "/M_Parvum_Embedded_Grey_Green_Muzzle_Texture.mat",
                    "M_Parvum_Embedded_Grey_Green_Muzzle_Texture",
                    new Color(0.52f, 0.64f, 0.52f, 1f),
                    muzzleAlbedo,
                    muzzleBump,
                    false,
                    0.78f),
                EnsureMaterial(
                    MaterialRootPath + "/M_Parvum_Dark_Muzzle_Pores.mat",
                    "M_Parvum_Dark_Muzzle_Pores",
                    new Color(0.03f, 0.04f, 0.03f, 1f),
                    null,
                    null,
                    true,
                    0.9f),
                EnsureMaterial(
                    MaterialRootPath + "/M_Parvum_Deep_Mouth_Cavity_No_Line_Objects.mat",
                    "M_Parvum_Deep_Mouth_Cavity_No_Line_Objects",
                    new Color(0.015f, 0.01f, 0.012f, 1f),
                    mouthAlbedo,
                    null,
                    true,
                    0.82f),
                EnsureMaterial(
                    MaterialRootPath + "/M_Parvum_Irregular_Embedded_Teeth.mat",
                    "M_Parvum_Irregular_Embedded_Teeth",
                    new Color(0.78f, 0.67f, 0.45f, 1f),
                    toothAlbedo,
                    null,
                    true,
                    0.7f),
                EnsureMaterial(
                    MaterialRootPath + "/M_Parvum_Mouth_Tongue_Detail.mat",
                    "M_Parvum_Mouth_Tongue_Detail",
                    new Color(0.72f, 0.08f, 0.04f, 1f),
                    tongueAlbedo,
                    null,
                    true,
                    0.44f));
        }

        private static Texture2D LoadTexture(string assetPath)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                throw new InvalidOperationException("Missing texture asset: " + assetPath);
            }

            return texture;
        }

        private static Material EnsureMaterial(
            string assetPath,
            string materialName,
            Color baseColor,
            Texture2D baseMap,
            Texture2D normalMap,
            bool transparent,
            float roughness)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                material = new Material(FindLitShader());
                AssetDatabase.CreateAsset(material, assetPath);
            }
            else
            {
                material.shader = FindLitShader();
            }

            material.name = materialName;
            SetColor(material, "_BaseColor", baseColor);
            SetColor(material, "_Color", baseColor);
            SetTexture(material, "_BaseMap", baseMap);
            SetTexture(material, "_MainTex", baseMap);
            SetTexture(material, "_BumpMap", normalMap);
            SetFloat(material, "_BumpScale", normalMap == null ? 0f : 0.65f);
            SetFloat(material, "_Smoothness", Mathf.Clamp01(1f - roughness));
            SetFloat(material, "_Metallic", 0f);
            ConfigureSurface(material, transparent);
            if (transparent &&
                materialName.IndexOf("Wet_Marbled", StringComparison.OrdinalIgnoreCase) < 0 &&
                materialName.IndexOf("Dark_Green_Internal", StringComparison.OrdinalIgnoreCase) < 0)
            {
                material.renderQueue = (int)RenderQueue.Transparent + 50;
                SetFloat(material, "_Cull", (float)CullMode.Off);
                SetFloat(material, "_ZTest", (float)CompareFunction.Always);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Shader FindLitShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        }

        private static void ConfigureSurface(Material material, bool transparent)
        {
            if (transparent)
            {
                SetFloat(material, "_Surface", 1f);
                SetFloat(material, "_Blend", 0f);
                SetFloat(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
                SetFloat(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                SetFloat(material, "_ZWrite", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent;
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                SetFloat(material, "_Surface", 0f);
                SetFloat(material, "_SrcBlend", (float)BlendMode.One);
                SetFloat(material, "_DstBlend", (float)BlendMode.Zero);
                SetFloat(material, "_ZWrite", 1f);
                material.SetOverrideTag("RenderType", "Opaque");
                material.renderQueue = -1;
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
        }

        private static void SetColor(Material material, string propertyName, Color value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, value);
            }
        }

        private static void SetTexture(Material material, string propertyName, Texture texture)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }

        private static void SetFloat(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static GameObject EnsurePrefab(MaterialSet materials)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath);
            if (model == null)
            {
                throw new InvalidOperationException("Missing approved Parvum model asset: " + ModelAssetPath);
            }

            var wrapper = new GameObject("ParvumApproved");
            var modelInstance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (modelInstance == null)
            {
                modelInstance = UnityEngine.Object.Instantiate(model);
            }

            modelInstance.name = ModelChildPath;
            modelInstance.transform.SetParent(wrapper.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.Euler(ParvumModelFacingEuler);
            modelInstance.transform.localScale = Vector3.one;

            EnsureSkinnedRendererForBlendShapes(wrapper.transform);
            AssignMaterials(wrapper, materials);
            DisableImportedColliders(wrapper.transform);
            EnsureMotionHelperTargets(wrapper.transform);
            EnsurePhysicsComponents(wrapper.transform);
            EnsureRiggingMarkers(wrapper.transform);

            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(wrapper, PrefabPath);
            UnityEngine.Object.DestroyImmediate(wrapper);
            if (savedPrefab == null)
            {
                throw new InvalidOperationException("Failed to save approved Parvum prefab: " + PrefabPath);
            }

            return savedPrefab;
        }

        private static void EnsureSkinnedRendererForBlendShapes(Transform root)
        {
            var existing = root.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (existing != null)
            {
                existing.sharedMesh = EnsureRuntimeBlendShapes(existing.sharedMesh, existing.sharedMaterials);
                existing.rootBone = existing.transform;
                existing.updateWhenOffscreen = true;
                existing.forceRenderingOff = false;
                existing.allowOcclusionWhenDynamic = false;
                ExpandSkinnedRendererBounds(existing);
                return;
            }

            var meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
            for (var i = 0; i < meshFilters.Length; i++)
            {
                var meshFilter = meshFilters[i];
                if (meshFilter.sharedMesh == null)
                {
                    continue;
                }

                var meshRenderer = meshFilter.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    continue;
                }

                var sharedMaterials = meshRenderer.sharedMaterials;
                var sourceMesh = meshFilter.sharedMesh;
                var skinnedRenderer = meshFilter.gameObject.AddComponent<SkinnedMeshRenderer>();
                skinnedRenderer.sharedMesh = EnsureRuntimeBlendShapes(sourceMesh, sharedMaterials);
                skinnedRenderer.sharedMaterials = sharedMaterials;
                skinnedRenderer.rootBone = skinnedRenderer.transform;
                skinnedRenderer.updateWhenOffscreen = true;
                skinnedRenderer.forceRenderingOff = false;
                skinnedRenderer.allowOcclusionWhenDynamic = false;
                skinnedRenderer.quality = SkinQuality.Bone1;
                ExpandSkinnedRendererBounds(skinnedRenderer);
                UnityEngine.Object.DestroyImmediate(meshRenderer);
                UnityEngine.Object.DestroyImmediate(meshFilter);
                return;
            }

            throw new InvalidOperationException("Approved Parvum prefab must contain a MeshFilter or SkinnedMeshRenderer.");
        }

        private static Mesh EnsureRuntimeBlendShapes(Mesh sourceMesh, Material[] sourceMaterials)
        {
            if (sourceMesh == null)
            {
                throw new InvalidOperationException("Approved Parvum mesh is missing.");
            }

            var generatedMesh = UnityEngine.Object.Instantiate(sourceMesh);
            generatedMesh.name = "parvum_runtime_blendshape_mesh";
            generatedMesh.ClearBlendShapes();
            AddMissingBlendShape(generatedMesh, IdleBlendShapeName, sourceMaterials);
            AddMissingBlendShape(generatedMesh, MoveBlendShapeName, sourceMaterials);
            AddMissingBlendShape(generatedMesh, AttackBlendShapeName, sourceMaterials);
            AddMissingBlendShape(generatedMesh, HitBlendShapeName, sourceMaterials);
            AddMissingBlendShape(generatedMesh, DeathBlendShapeName, sourceMaterials);
            AddMissingBlendShape(generatedMesh, AttackTeethChompBlendShapeName, sourceMaterials);
            AddMissingBlendShape(generatedMesh, AttackMouthWideOpenBlendShapeName, sourceMaterials);
            AddMissingBlendShape(generatedMesh, HitSlowRecoilBlendShapeName, sourceMaterials);
            AddMissingBlendShape(generatedMesh, DeathLiquefyCollapseBlendShapeName, sourceMaterials);
            AddMissingBlendShape(generatedMesh, DeathMouthDissolveBlendShapeName, sourceMaterials);

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(RuntimeBlendShapeMeshPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generatedMesh, RuntimeBlendShapeMeshPath);
                return generatedMesh;
            }

            EditorUtility.CopySerialized(generatedMesh, existing);
            EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(generatedMesh);
            return existing;
        }

        private static bool HasRequiredBlendShapes(Mesh mesh)
        {
            return mesh.GetBlendShapeIndex(IdleBlendShapeName) >= 0 &&
                   mesh.GetBlendShapeIndex(MoveBlendShapeName) >= 0 &&
                   mesh.GetBlendShapeIndex(AttackBlendShapeName) >= 0 &&
                   mesh.GetBlendShapeIndex(HitBlendShapeName) >= 0 &&
                   mesh.GetBlendShapeIndex(DeathBlendShapeName) >= 0 &&
                   mesh.GetBlendShapeIndex(AttackTeethChompBlendShapeName) >= 0 &&
                   mesh.GetBlendShapeIndex(AttackMouthWideOpenBlendShapeName) >= 0 &&
                   mesh.GetBlendShapeIndex(HitSlowRecoilBlendShapeName) >= 0 &&
                   mesh.GetBlendShapeIndex(DeathLiquefyCollapseBlendShapeName) >= 0 &&
                   mesh.GetBlendShapeIndex(DeathMouthDissolveBlendShapeName) >= 0;
        }

        private static void AddMissingBlendShape(Mesh mesh, string shapeName, Material[] sourceMaterials)
        {
            if (mesh.GetBlendShapeIndex(shapeName) >= 0)
            {
                return;
            }

            var vertices = mesh.vertices;
            var deltaVertices = BuildBlendShapeDeltas(mesh, sourceMaterials, vertices, shapeName);
            var deltaNormals = new Vector3[vertices.Length];
            var deltaTangents = new Vector3[vertices.Length];
            mesh.AddBlendShapeFrame(shapeName, 100f, deltaVertices, deltaNormals, deltaTangents);
        }

        private static Vector3[] BuildBlendShapeDeltas(Mesh mesh, Material[] sourceMaterials, Vector3[] vertices, string shapeName)
        {
            var deltas = new Vector3[vertices.Length];
            var bounds = mesh.bounds;
            var extents = new Vector3(
                Mathf.Max(bounds.extents.x, 0.001f),
                Mathf.Max(bounds.extents.y, 0.001f),
                Mathf.Max(bounds.extents.z, 0.001f));
            var toothWeights = shapeName == AttackTeethChompBlendShapeName
                ? BuildMaterialVertexWeights(mesh, sourceMaterials, "tooth", "teeth", "irregular_embedded_teeth")
                : null;
            var muzzleWeights =
                shapeName == AttackBlendShapeName ||
                shapeName == AttackTeethChompBlendShapeName ||
                shapeName == AttackMouthWideOpenBlendShapeName
                    ? BuildMaterialVertexWeights(
                        mesh,
                        sourceMaterials,
                        "muzzle",
                        "snout",
                        "pores",
                        "edge_blend",
                        "green_grey")
                    : null;
            var mouthWeights =
                shapeName == AttackTeethChompBlendShapeName ||
                shapeName == AttackMouthWideOpenBlendShapeName ||
                shapeName == DeathMouthDissolveBlendShapeName
                    ? BuildMaterialVertexWeights(
                        mesh,
                        sourceMaterials,
                        "mouth",
                        "cavity",
                        "tongue",
                        "tooth",
                        "teeth")
                    : null;

            for (var i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                var normalizedX = Mathf.Clamp((vertex.x - bounds.center.x) / extents.x, -1f, 1f);
                var normalizedY = Mathf.Clamp((vertex.y - bounds.center.y) / extents.y, -1f, 1f);
                var normalizedZ = Mathf.Clamp((vertex.z - bounds.center.z) / extents.z, -1f, 1f);
                var front = Mathf.InverseLerp(-0.2f, 1f, normalizedY);
                var height = Mathf.InverseLerp(-1f, 1f, normalizedZ);
                var side = Mathf.Abs(normalizedX);

                if (shapeName == IdleBlendShapeName)
                {
                    deltas[i] = new Vector3(
                        normalizedX * extents.x * 0.025f,
                        normalizedY * extents.y * 0.018f,
                        Mathf.Sin((normalizedZ + 1f) * Mathf.PI) * extents.z * 0.025f);
                }
                else if (shapeName == MoveBlendShapeName)
                {
                    deltas[i] = new Vector3(
                        normalizedX * extents.x * -0.035f,
                        front * extents.y * 0.12f,
                        -Mathf.Abs(normalizedZ) * extents.z * 0.08f);
                }
                else if (shapeName == AttackBlendShapeName)
                {
                    var muzzleInfluence = muzzleWeights == null ? 0f : muzzleWeights[i];
                    var centralBody = Mathf.Clamp01(1f - side * 0.2f);
                    var bodyFocus = Mathf.Clamp01(0.28f + front * 0.72f) * centralBody;
                    var upperBody = bodyFocus * Mathf.Lerp(0.5f, 1.05f, height);
                    deltas[i] = new Vector3(
                        normalizedX * (bodyFocus * 0.08f - muzzleInfluence * 0.035f) * extents.x,
                        (bodyFocus * 0.26f + muzzleInfluence * 0.26f) * extents.y,
                        ((height - 0.4f) * upperBody * 0.2f + Mathf.Lerp(0.02f, 0.24f, height) * muzzleInfluence) * extents.z);
                }
                else if (shapeName == AttackTeethChompBlendShapeName)
                {
                    var mouthFocus = mouthWeights == null ? 0f : mouthWeights[i];
                    var muzzleInfluence = muzzleWeights == null ? 0f : muzzleWeights[i];
                    var centralBody = Mathf.Clamp01(1f - side * 0.18f);
                    var bodyClose = Mathf.Clamp01(0.42f + front * 0.58f) * centralBody;
                    var toothInfluence = Mathf.Max(toothWeights == null ? 0f : toothWeights[i], mouthFocus * 0.25f);
                    var biteDirection = normalizedZ >= 0f ? -1f : 1f;
                    deltas[i] = new Vector3(
                        normalizedX * (bodyClose * 0.2f - toothInfluence * 0.045f - muzzleInfluence * 0.035f) * extents.x,
                        (front * toothInfluence * 0.1f - bodyClose * 0.06f - muzzleInfluence * 0.18f) * extents.y,
                        (biteDirection * toothInfluence * 0.32f - Mathf.Lerp(0.12f, 0.28f, height) * bodyClose - Mathf.Lerp(0.1f, 0.26f, height) * muzzleInfluence) * extents.z);
                }
                else if (shapeName == AttackMouthWideOpenBlendShapeName)
                {
                    var mouthInfluence = mouthWeights == null ? 0f : mouthWeights[i];
                    var muzzleInfluence = muzzleWeights == null ? 0f : muzzleWeights[i];
                    var centralBody = Mathf.Clamp01(1f - side * 0.18f);
                    var bodyOpen = Mathf.Clamp01(0.36f + front * 0.64f) * centralBody;
                    var upperLipFollow = muzzleInfluence * Mathf.Lerp(0.38f, 1.08f, height);
                    var frontBody = Mathf.Clamp01(0.25f + front * 0.75f) * Mathf.Clamp01(1f - side * 0.55f) * Mathf.Lerp(0.45f, 0.95f, height);
                    var openInfluence = Mathf.Max(mouthInfluence, Mathf.Max(muzzleInfluence * 0.96f, frontBody * 0.62f));
                    var openDirection = normalizedZ >= 0f ? 1f : -1f;
                    deltas[i] = new Vector3(
                        normalizedX * (openInfluence * 0.06f - bodyOpen * 0.12f) * extents.x,
                        (front * mouthInfluence * 0.14f + upperLipFollow * 0.32f + frontBody * 0.12f) * extents.y,
                        (openDirection * mouthInfluence * 0.46f + upperLipFollow * 0.34f + (height - 0.34f) * bodyOpen * 0.32f) * extents.z);
                }
                else if (shapeName == HitBlendShapeName)
                {
                    deltas[i] = new Vector3(
                        -Mathf.Sign(normalizedX == 0f ? 1f : normalizedX) * (0.025f + side * 0.015f) * extents.x,
                        -front * extents.y * 0.025f,
                        Mathf.Sin((normalizedX + 1f) * Mathf.PI) * extents.z * 0.015f);
                }
                else if (shapeName == HitSlowRecoilBlendShapeName)
                {
                    var upperMass = Mathf.InverseLerp(-0.75f, 1f, normalizedZ);
                    deltas[i] = new Vector3(
                        normalizedX * extents.x * -0.025f,
                        -Mathf.Lerp(0.08f, 0.22f, front) * extents.y,
                        -upperMass * extents.z * 0.035f);
                }
                else if (shapeName == DeathLiquefyCollapseBlendShapeName)
                {
                    var spread = 0.52f + height * 0.95f;
                    deltas[i] = new Vector3(
                        normalizedX * extents.x * spread,
                        normalizedY * extents.y * (spread + 0.2f),
                        -height * extents.z * 1.02f);
                }
                else if (shapeName == DeathMouthDissolveBlendShapeName)
                {
                    var mouthInfluence = mouthWeights == null ? 0f : mouthWeights[i];
                    deltas[i] = new Vector3(
                        -normalizedX * mouthInfluence * extents.x * 0.45f,
                        -Mathf.Sign(normalizedY == 0f ? 1f : normalizedY) * mouthInfluence * extents.y * 0.55f,
                        -Mathf.Lerp(0.32f, 0.9f, height) * mouthInfluence * extents.z);
                }
                else
                {
                    var spread = 0.34f + height * 0.68f;
                    deltas[i] = new Vector3(
                        normalizedX * extents.x * spread,
                        normalizedY * extents.y * (spread + 0.12f),
                        -height * extents.z * 0.82f);
                }
            }

            return deltas;
        }

        private static void ExpandSkinnedRendererBounds(SkinnedMeshRenderer renderer)
        {
            if (renderer == null || renderer.sharedMesh == null)
            {
                return;
            }

            var expandedBounds = renderer.sharedMesh.bounds;
            var maxAxis = Mathf.Max(expandedBounds.size.x, expandedBounds.size.y, expandedBounds.size.z);
            expandedBounds.Expand(Mathf.Max(0.75f, maxAxis * 4.25f));
            renderer.localBounds = expandedBounds;
        }

        private static float[] BuildMaterialVertexWeights(Mesh mesh, Material[] sourceMaterials, params string[] materialNameTokens)
        {
            var weights = new float[mesh.vertexCount];
            if (sourceMaterials == null || sourceMaterials.Length == 0)
            {
                return weights;
            }

            var subMeshCount = Mathf.Min(mesh.subMeshCount, sourceMaterials.Length);
            for (var subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
            {
                var material = sourceMaterials[subMeshIndex];
                var materialName = material == null ? string.Empty : material.name.ToLowerInvariant();
                var matched = false;
                for (var tokenIndex = 0; tokenIndex < materialNameTokens.Length; tokenIndex++)
                {
                    if (materialName.Contains(materialNameTokens[tokenIndex]))
                    {
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    continue;
                }

                var indices = mesh.GetIndices(subMeshIndex);
                for (var index = 0; index < indices.Length; index++)
                {
                    var vertexIndex = indices[index];
                    if (vertexIndex >= 0 && vertexIndex < weights.Length)
                    {
                        weights[vertexIndex] = 1f;
                    }
                }
            }

            return weights;
        }

        private static void AssignMaterials(GameObject root, MaterialSet materials)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                var assigned = renderer.sharedMaterials;
                if (assigned.Length < 6)
                {
                    renderer.sharedMaterials = materials.ToRendererMaterialArray();
                    continue;
                }

                for (var materialIndex = 0; materialIndex < assigned.Length; materialIndex++)
                {
                    var sourceName = assigned[materialIndex] == null ? string.Empty : assigned[materialIndex].name;
                    assigned[materialIndex] = ResolveParvumMaterial(sourceName, materials);
                }

                renderer.sharedMaterials = assigned;
            }
        }

        private static Material ResolveParvumMaterial(string sourceName, MaterialSet materials)
        {
            if (sourceName.IndexOf("Dark_Green_Internal", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return materials.InternalFlow;
            }

            if (sourceName.IndexOf("Green_Grey_Muzzle_Edge", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return materials.MuzzleBlend;
            }

            if (sourceName.IndexOf("Embedded_Grey_Green_Muzzle", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return materials.Muzzle;
            }

            if (sourceName.IndexOf("Dark_Muzzle_Pores", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return materials.DarkPores;
            }

            if (sourceName.IndexOf("Deep_Mouth_Cavity", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return materials.MouthDark;
            }

            if (sourceName.IndexOf("Irregular_Embedded_Teeth", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return materials.Tooth;
            }

            if (sourceName.IndexOf("Mouth_Tongue_Detail", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return materials.Tongue;
            }

            return materials.Slime;
        }

        private static void DisableImportedColliders(Transform root)
        {
            var colliders = root.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].transform == root)
                {
                    continue;
                }

                colliders[i].enabled = false;
            }
        }

        private static void EnsureMotionHelperTargets(Transform root)
        {
            var helperRoot = GetOrCreateChild(root, MotionHelperRootName);
            helperRoot.localPosition = Vector3.zero;
            helperRoot.localRotation = Quaternion.identity;
            helperRoot.localScale = Vector3.one;

            SetLocal(GetOrCreateChild(helperRoot, MotionPathTargetName), new Vector3(0f, 0f, 0.75f));
            SetLocal(GetOrCreateChild(helperRoot, MouthIkTargetName), new Vector3(0f, 0.15f, 0.42f));
            SetLocal(GetOrCreateChild(helperRoot, JointTargetName), new Vector3(0f, 0.05f, 0.3f));
            SetLocal(GetOrCreateChild(helperRoot, JiggleLeftTargetName), new Vector3(-0.32f, 0.24f, 0.02f));
            SetLocal(GetOrCreateChild(helperRoot, JiggleRightTargetName), new Vector3(0.32f, 0.24f, 0.02f));
            SetLocal(GetOrCreateChild(helperRoot, JiggleRearTargetName), new Vector3(0f, 0.18f, -0.36f));

            var driver = root.GetComponent<ParvumPhysicsMotionDriver>();
            if (driver == null)
            {
                driver = root.gameObject.AddComponent<ParvumPhysicsMotionDriver>();
            }

            driver.MotionPathTarget = helperRoot.Find(MotionPathTargetName);
            driver.LockRootMotionForReview = false;
        }

        private static void SetLocal(Transform target, Vector3 localPosition)
        {
            target.localPosition = localPosition;
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;
        }

        private static Transform GetOrCreateChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
            {
                return child;
            }

            var childObject = new GameObject(childName);
            childObject.transform.SetParent(parent, false);
            return childObject.transform;
        }

        private static void EnsurePhysicsComponents(Transform root)
        {
            var body = root.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = root.gameObject.AddComponent<Rigidbody>();
            }

            body.useGravity = false;
            if (body.isKinematic)
            {
                body.isKinematic = false;
            }

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var collider = root.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = root.gameObject.AddComponent<BoxCollider>();
            }

            RefreshRootColliderBounds(root, collider);
            collider.isTrigger = false;

            var jointTarget = FindChildRecursive(root, JointTargetName);
            if (jointTarget != null)
            {
                var targetBody = jointTarget.GetComponent<Rigidbody>();
                if (targetBody == null)
                {
                    targetBody = jointTarget.gameObject.AddComponent<Rigidbody>();
                }

                targetBody.useGravity = false;
                targetBody.isKinematic = true;

                var joint = jointTarget.GetComponent<ConfigurableJoint>();
                if (joint == null)
                {
                    joint = jointTarget.gameObject.AddComponent<ConfigurableJoint>();
                }

                joint.connectedBody = body;
                joint.xMotion = ConfigurableJointMotion.Limited;
                joint.yMotion = ConfigurableJointMotion.Limited;
                joint.zMotion = ConfigurableJointMotion.Limited;
                joint.angularXMotion = ConfigurableJointMotion.Locked;
                joint.angularYMotion = ConfigurableJointMotion.Locked;
                joint.angularZMotion = ConfigurableJointMotion.Locked;
                joint.linearLimit = new SoftJointLimit { limit = 0.12f };
            }
        }

        private static void EnsureScenePhysicsComponents(Transform root)
        {
            EnsurePhysicsComponents(root);
            var body = root.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.useGravity = false;
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }

                body.isKinematic = true;
                PrefabUtility.RecordPrefabInstancePropertyModifications(body);
            }

            var driver = root.GetComponent<ParvumPhysicsMotionDriver>();
            if (driver == null)
            {
                driver = root.gameObject.AddComponent<ParvumPhysicsMotionDriver>();
            }

            var target = FindChildRecursive(root, MotionPathTargetName);
            if (target != null)
            {
                driver.MotionPathTarget = target;
            }

            driver.LockRootMotionForReview = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(driver);
        }

        private static void RefreshRootColliderBounds(Transform root, BoxCollider collider)
        {
            var bounds = GetRendererBounds(root);
            var localCenter = root.InverseTransformPoint(bounds.center);
            var localSize = root.InverseTransformVector(bounds.size);
            collider.center = localCenter;
            collider.size = new Vector3(
                Mathf.Max(0.01f, Mathf.Abs(localSize.x)),
                Mathf.Max(0.01f, Mathf.Abs(localSize.y)),
                Mathf.Max(0.01f, Mathf.Abs(localSize.z)));
        }

        private static void EnsureRiggingMarkers(Transform root)
        {
            AddComponentIfAvailable(root.gameObject, "UnityEngine.Animations.Rigging.RigBuilder, Unity.Animation.Rigging");

            var rigRoot = GetOrCreateChild(root, "Parvum_AnimationRigging_Mouth_Rig");
            AddComponentIfAvailable(rigRoot.gameObject, "UnityEngine.Animations.Rigging.Rig, Unity.Animation.Rigging");
            ConfigureJiggleRigIfAvailable(root);
        }

        private static void AddComponentIfAvailable(GameObject target, string assemblyQualifiedTypeName)
        {
            var type = Type.GetType(assemblyQualifiedTypeName);
            if (type == null || target.GetComponent(type) != null)
            {
                return;
            }

            target.AddComponent(type);
        }

        private static void ConfigureJiggleRigIfAvailable(Transform root)
        {
            var type = Type.GetType("GatorDragonGames.JigglePhysics.JiggleRig, com.gator-dragon-games.jigglephysics");
            if (type == null)
            {
                return;
            }

            var wasActive = root.gameObject.activeSelf;
            if (wasActive)
            {
                root.gameObject.SetActive(false);
            }

            try
            {
                var component = root.GetComponent(type);
                if (component == null)
                {
                    component = root.gameObject.AddComponent(type);
                }

                var renderer = FindApprovedSkinnedRenderer(root);
                ConfigureJiggleRigSerializedData(component, renderer.transform);
                EditorUtility.SetDirty(component);
            }
            finally
            {
                if (wasActive)
                {
                    root.gameObject.SetActive(true);
                }
            }
        }

        private static void ConfigureJiggleRigSerializedData(Component component, Transform rootBone)
        {
            var serialized = new SerializedObject(component);
            SetSerializedBool(serialized, "jiggleRigData.hasSerializedData", true);
            SetSerializedString(serialized, "jiggleRigData.serializedVersion", "v0.0.2");
            SetSerializedObject(serialized, "jiggleRigData.rootBone", rootBone);
            SetSerializedBool(serialized, "jiggleRigData.excludeRoot", false);
            SetSerializedArraySize(serialized, "jiggleRigData.excludedTransforms", 0);
            SetSerializedArraySize(serialized, "jiggleRigData.transformCachedData", 0);
            SetSerializedArraySize(serialized, "jiggleRigData.jiggleColliders", 0);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedBool(SerializedObject serialized, string propertyPath, bool value)
        {
            var property = serialized.FindProperty(propertyPath);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void SetSerializedString(SerializedObject serialized, string propertyPath, string value)
        {
            var property = serialized.FindProperty(propertyPath);
            if (property != null)
            {
                property.stringValue = value;
            }
        }

        private static void SetSerializedObject(SerializedObject serialized, string propertyPath, UnityEngine.Object value)
        {
            var property = serialized.FindProperty(propertyPath);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void SetSerializedArraySize(SerializedObject serialized, string propertyPath, int size)
        {
            var property = serialized.FindProperty(propertyPath);
            if (property != null && property.isArray)
            {
                property.arraySize = size;
            }
        }

        private static Dictionary<string, AnimationClip> EnsureAnimationClips(GameObject prefab)
        {
            var result = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);
            var renderer = FindApprovedSkinnedRenderer(prefab.transform);
            var rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, prefab.transform);
            var helperRoot = FindChildRecursive(prefab.transform, MotionHelperRootName);
            if (helperRoot == null)
            {
                throw new InvalidOperationException("Missing Parvum motion helper root in prefab.");
            }

            for (var i = 0; i < PlacementSpecs.Length; i++)
            {
                var spec = PlacementSpecs[i];
                if (string.IsNullOrEmpty(spec.AnimationClipName))
                {
                    continue;
                }

                var clip = new AnimationClip
                {
                    frameRate = 30f,
                    name = spec.AnimationClipName,
                    wrapMode = WrapMode.Loop
                };

                AddBlendShapeCurves(clip, rendererPath, renderer, spec.BlendShapeName, spec.AnimationClipName);
                AddMotionHelperCurves(clip, prefab.transform, spec.AnimationClipName);
                var savedClip = SaveAnimationClip(clip, AnimationRootPath + "/" + spec.AnimationClipName + ".anim");
                result.Add(spec.AnimationClipName, savedClip);
            }

            return result;
        }

        private static SkinnedMeshRenderer FindApprovedSkinnedRenderer(Transform root)
        {
            var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].name.IndexOf(UnifiedVisibleMeshName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    renderers[i].sharedMesh != null && renderers[i].sharedMesh.name.IndexOf(UnifiedVisibleMeshName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return renderers[i];
                }
            }

            if (renderers.Length > 0)
            {
                return renderers[0];
            }

            throw new InvalidOperationException("Approved Parvum prefab must contain a SkinnedMeshRenderer for Shape Key animation.");
        }

        private static void AddBlendShapeCurves(
            AnimationClip clip,
            string rendererPath,
            SkinnedMeshRenderer renderer,
            string activeBlendShape,
            string clipName)
        {
            var mesh = renderer.sharedMesh;
            if (mesh == null)
            {
                throw new InvalidOperationException("Approved Parvum SkinnedMeshRenderer has no mesh.");
            }

            var required = new[]
            {
                IdleBlendShapeName,
                MoveBlendShapeName,
                AttackBlendShapeName,
                HitBlendShapeName,
                DeathBlendShapeName,
                AttackTeethChompBlendShapeName,
                AttackMouthWideOpenBlendShapeName,
                HitSlowRecoilBlendShapeName,
                DeathLiquefyCollapseBlendShapeName,
                DeathMouthDissolveBlendShapeName,
            };
            for (var i = 0; i < required.Length; i++)
            {
                if (mesh.GetBlendShapeIndex(required[i]) < 0)
                {
                    throw new InvalidOperationException("Approved Parvum mesh is missing Shape Key: " + required[i]);
                }

                var peak = ResolveBlendShapePeak(clipName, required[i], activeBlendShape);
                var curve = BuildPoseCurve(clipName, required[i], peak);
                var binding = EditorCurveBinding.FloatCurve(rendererPath, typeof(SkinnedMeshRenderer), "blendShape." + required[i]);
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
        }

        private static float ResolveBlendShapePeak(string clipName, string shapeName, string activeBlendShape)
        {
            if (clipName == "Parvum_Attack")
            {
                if (shapeName == AttackBlendShapeName)
                {
                    return 100f;
                }

                if (shapeName == AttackTeethChompBlendShapeName)
                {
                    return 100f;
                }

                if (shapeName == AttackMouthWideOpenBlendShapeName)
                {
                    return 100f;
                }

                if (shapeName == MoveBlendShapeName)
                {
                    return 8f;
                }
            }

            if (clipName == "Parvum_Hit")
            {
                if (shapeName == HitBlendShapeName)
                {
                    return 28f;
                }

                if (shapeName == HitSlowRecoilBlendShapeName)
                {
                    return 100f;
                }
            }

            if (clipName == "Parvum_Death")
            {
                if (shapeName == DeathBlendShapeName)
                {
                    return 100f;
                }

                if (shapeName == DeathLiquefyCollapseBlendShapeName)
                {
                    return 100f;
                }

                if (shapeName == DeathMouthDissolveBlendShapeName)
                {
                    return 100f;
                }
            }

            if (shapeName == activeBlendShape)
            {
                return clipName == "Parvum_Move" ? 70f : 100f;
            }

            return 0f;
        }

        private static AnimationCurve BuildPoseCurve(string clipName, string shapeName, float peak)
        {
            if (peak <= 0f)
            {
                return ConstantCurve(0f, GetClipEndTime(clipName));
            }

            if (clipName == "Parvum_Idle")
            {
                return new AnimationCurve(
                    new Keyframe(0f, peak * 0.35f),
                    new Keyframe(0.45f, peak),
                    new Keyframe(0.9f, peak * 0.35f));
            }

            if (clipName == "Parvum_Move")
            {
                return new AnimationCurve(
                    new Keyframe(0f, peak * 0.15f),
                    new Keyframe(0.25f, peak),
                    new Keyframe(0.5f, peak * 0.35f),
                    new Keyframe(0.75f, peak * 0.9f),
                    new Keyframe(1f, peak * 0.15f));
            }

            if (clipName == "Parvum_Attack")
            {
                if (shapeName == AttackMouthWideOpenBlendShapeName)
                {
                    return new AnimationCurve(
                        new Keyframe(0f, 0f),
                        new Keyframe(0.1f, peak * 0.45f),
                        new Keyframe(0.22f, peak),
                        new Keyframe(0.32f, peak * 0.74f),
                        new Keyframe(0.42f, peak * 0.18f),
                        new Keyframe(0.54f, peak * 0.92f),
                        new Keyframe(0.62f, peak * 0.72f),
                        new Keyframe(0.72f, peak * 0.18f),
                        new Keyframe(0.9f, peak * 0.1f),
                        new Keyframe(1.08f, 0f));
                }

                if (shapeName == AttackTeethChompBlendShapeName)
                {
                    return new AnimationCurve(
                        new Keyframe(0f, 0f),
                        new Keyframe(0.26f, 0f),
                        new Keyframe(0.42f, peak),
                        new Keyframe(0.52f, peak * 0.15f),
                        new Keyframe(0.74f, peak),
                        new Keyframe(0.84f, peak * 0.28f),
                        new Keyframe(1.08f, 0f));
                }

                return new AnimationCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(0.14f, peak * 0.28f),
                    new Keyframe(0.3f, peak),
                    new Keyframe(0.5f, peak * 0.74f),
                    new Keyframe(0.68f, peak),
                    new Keyframe(0.86f, peak * 0.42f),
                    new Keyframe(1.08f, 0f));
            }

            if (clipName == "Parvum_Hit")
            {
                return new AnimationCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(0.14f, peak * 0.85f),
                    new Keyframe(0.38f, peak),
                    new Keyframe(0.82f, peak * 0.32f),
                    new Keyframe(1.25f, 0f));
            }

            if (clipName == "Parvum_Death")
            {
                if (shapeName == DeathMouthDissolveBlendShapeName)
                {
                    return new AnimationCurve(
                        new Keyframe(0f, 0f),
                        new Keyframe(0.32f, peak * 0.08f),
                        new Keyframe(0.62f, peak * 0.52f),
                        new Keyframe(0.95f, peak),
                        new Keyframe(1.8f, peak));
                }

                return new AnimationCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(0.22f, peak * 0.18f),
                    new Keyframe(0.58f, peak * 0.78f),
                    new Keyframe(0.98f, peak),
                    new Keyframe(1.45f, peak),
                    new Keyframe(1.8f, peak));
            }

            return new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.22f, peak * 0.35f),
                new Keyframe(0.56f, peak),
                new Keyframe(1.15f, peak),
                new Keyframe(1.45f, 0f));
        }

        private static AnimationCurve ConstantCurve(float value, float endTime)
        {
            return new AnimationCurve(
                new Keyframe(0f, value),
                new Keyframe(endTime, value));
        }

        private static float GetClipEndTime(string clipName)
        {
            if (clipName == "Parvum_Move")
            {
                return 1f;
            }

            if (clipName == "Parvum_Attack")
            {
                return 1.08f;
            }

            if (clipName == "Parvum_Hit")
            {
                return 1.25f;
            }

            if (clipName == "Parvum_Death")
            {
                return 1.8f;
            }

            return 0.9f;
        }

        private static void AddMotionHelperCurves(AnimationClip clip, Transform prefabRoot, string clipName)
        {
            var motionPath = AnimationUtility.CalculateTransformPath(RequireChild(prefabRoot, MotionPathTargetName), prefabRoot);
            var mouthIk = AnimationUtility.CalculateTransformPath(RequireChild(prefabRoot, MouthIkTargetName), prefabRoot);
            var jointTarget = AnimationUtility.CalculateTransformPath(RequireChild(prefabRoot, JointTargetName), prefabRoot);
            var jiggleLeft = AnimationUtility.CalculateTransformPath(RequireChild(prefabRoot, JiggleLeftTargetName), prefabRoot);
            var jiggleRight = AnimationUtility.CalculateTransformPath(RequireChild(prefabRoot, JiggleRightTargetName), prefabRoot);
            var jiggleRear = AnimationUtility.CalculateTransformPath(RequireChild(prefabRoot, JiggleRearTargetName), prefabRoot);

            if (clipName == "Parvum_Move")
            {
                SetLocalPositionCurves(
                    clip,
                    motionPath,
                    new[]
                    {
                        new PositionKey(0f, -0.08f, 0f, 0.6f),
                        new PositionKey(0.22f, 0f, -0.03f, 0.88f),
                        new PositionKey(0.5f, 0.08f, 0f, 0.62f),
                        new PositionKey(0.78f, 0f, -0.025f, 0.84f),
                        new PositionKey(1f, -0.08f, 0f, 0.6f),
                    });
            }
            else if (clipName == "Parvum_Attack")
            {
                SetLocalPositionCurves(
                    clip,
                    motionPath,
                    new[]
                    {
                        new PositionKey(0f, 0f, 0f, 0.48f),
                        new PositionKey(0.14f, 0f, -0.05f, 0.36f),
                        new PositionKey(0.32f, 0f, 0.08f, 1.2f),
                        new PositionKey(0.48f, 0f, 0.0f, 0.72f),
                        new PositionKey(0.66f, 0f, 0.06f, 1.05f),
                        new PositionKey(1.08f, 0f, 0f, 0.48f),
                    });
                SetLocalPositionCurves(
                    clip,
                    mouthIk,
                    new[]
                    {
                        new PositionKey(0f, 0f, 0.12f, 0.34f),
                        new PositionKey(0.18f, 0f, 0.44f, 0.76f),
                        new PositionKey(0.32f, 0f, -0.08f, 0.98f),
                        new PositionKey(0.48f, 0f, 0.42f, 0.84f),
                        new PositionKey(0.62f, 0f, -0.06f, 0.9f),
                        new PositionKey(0.84f, 0f, 0.18f, 0.5f),
                        new PositionKey(1.08f, 0f, 0.12f, 0.34f),
                    });
                SetLocalPositionCurves(
                    clip,
                    jointTarget,
                    new[]
                    {
                        new PositionKey(0f, 0f, 0.05f, 0.3f),
                        new PositionKey(0.2f, 0f, 0.18f, 0.52f),
                        new PositionKey(0.34f, 0f, -0.08f, 0.42f),
                        new PositionKey(0.5f, 0f, 0.16f, 0.5f),
                        new PositionKey(0.66f, 0f, -0.07f, 0.43f),
                        new PositionKey(1.08f, 0f, 0.05f, 0.3f),
                    });
            }
            else if (clipName == "Parvum_Hit")
            {
                SetLocalPositionCurves(
                    clip,
                    motionPath,
                    new[]
                    {
                        new PositionKey(0f, 0f, 0f, 0.55f),
                        new PositionKey(0.16f, -0.18f, -0.04f, 0.28f),
                        new PositionKey(0.42f, -0.1f, -0.06f, 0.34f),
                        new PositionKey(0.85f, -0.04f, -0.03f, 0.44f),
                        new PositionKey(1.25f, 0f, 0f, 0.55f),
                    });
                SetLocalPositionCurves(
                    clip,
                    jiggleLeft,
                    new[]
                    {
                        new PositionKey(0f, -0.32f, 0.24f, 0.02f),
                        new PositionKey(0.16f, -0.38f, 0.3f, -0.06f),
                        new PositionKey(0.5f, -0.34f, 0.22f, -0.04f),
                        new PositionKey(0.85f, -0.32f, 0.2f, 0f),
                        new PositionKey(1.25f, -0.32f, 0.24f, 0.02f),
                    });
            }
            else if (clipName == "Parvum_Death")
            {
                SetLocalPositionCurves(
                    clip,
                    motionPath,
                    new[]
                    {
                        new PositionKey(0f, 0f, 0f, 0.55f),
                        new PositionKey(0.28f, 0f, -0.3f, 0.5f),
                        new PositionKey(0.62f, 0f, -0.72f, 0.42f),
                        new PositionKey(1.15f, 0f, -0.96f, 0.32f),
                        new PositionKey(1.8f, 0f, -1.02f, 0.28f),
                    });
                SetLocalPositionCurves(
                    clip,
                    jiggleLeft,
                    new[]
                    {
                        new PositionKey(0f, -0.32f, 0.24f, 0.02f),
                        new PositionKey(0.35f, -0.64f, 0.14f, 0.08f),
                        new PositionKey(0.72f, -1.05f, 0.05f, 0.18f),
                        new PositionKey(1.15f, -1.38f, 0.01f, 0.26f),
                        new PositionKey(1.8f, -1.5f, 0.01f, 0.3f),
                    });
                SetLocalPositionCurves(
                    clip,
                    jiggleRight,
                    new[]
                    {
                        new PositionKey(0f, 0.32f, 0.24f, 0.02f),
                        new PositionKey(0.35f, 0.64f, 0.14f, 0.08f),
                        new PositionKey(0.72f, 1.05f, 0.05f, 0.18f),
                        new PositionKey(1.15f, 1.38f, 0.01f, 0.26f),
                        new PositionKey(1.8f, 1.5f, 0.01f, 0.3f),
                    });
                SetLocalPositionCurves(
                    clip,
                    jiggleRear,
                    new[]
                    {
                        new PositionKey(0f, 0f, 0.18f, -0.36f),
                        new PositionKey(0.35f, 0f, 0.12f, -0.7f),
                        new PositionKey(0.72f, 0f, 0.05f, -1.02f),
                        new PositionKey(1.15f, 0f, 0.01f, -1.3f),
                        new PositionKey(1.8f, 0f, 0.01f, -1.45f),
                    });
            }
            else
            {
                SetLocalPositionCurves(
                    clip,
                    jiggleLeft,
                    new[]
                    {
                        new PositionKey(0f, -0.32f, 0.24f, 0.02f),
                        new PositionKey(0.45f, -0.32f, 0.34f, 0.02f),
                        new PositionKey(0.9f, -0.32f, 0.24f, 0.02f),
                    });
                SetLocalPositionCurves(
                    clip,
                    jiggleRight,
                    new[]
                    {
                        new PositionKey(0f, 0.32f, 0.24f, 0.02f),
                        new PositionKey(0.45f, 0.32f, 0.16f, 0.02f),
                        new PositionKey(0.9f, 0.32f, 0.24f, 0.02f),
                    });
            }
        }

        private static Transform RequireChild(Transform root, string childName)
        {
            var child = FindChildRecursive(root, childName);
            if (child == null)
            {
                throw new InvalidOperationException("Missing Parvum helper target: " + childName);
            }

            return child;
        }

        private static void SetLocalPositionCurves(AnimationClip clip, string path, PositionKey[] keys)
        {
            var xCurve = new AnimationCurve();
            var yCurve = new AnimationCurve();
            var zCurve = new AnimationCurve();
            for (var i = 0; i < keys.Length; i++)
            {
                xCurve.AddKey(new Keyframe(keys[i].Time, keys[i].X));
                yCurve.AddKey(new Keyframe(keys[i].Time, keys[i].Y));
                zCurve.AddKey(new Keyframe(keys[i].Time, keys[i].Z));
            }

            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.x"), xCurve);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.y"), yCurve);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.z"), zCurve);
        }

        private static AnimationClip SaveAnimationClip(AnimationClip clip, string path)
        {
            ForceLoopingClipSettings(clip);
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(clip, path);
                ForceLoopingClipSettings(clip);
                return clip;
            }

            EditorUtility.CopySerialized(clip, existing);
            ForceLoopingClipSettings(existing);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static void ForceLoopingClipSettings(AnimationClip clip)
        {
            clip.wrapMode = WrapMode.Loop;
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            settings.loopBlendOrientation = true;
            settings.loopBlendPositionY = true;
            settings.loopBlendPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        private static Dictionary<string, RuntimeAnimatorController> EnsureAnimatorControllers(Dictionary<string, AnimationClip> clips)
        {
            var result = new Dictionary<string, RuntimeAnimatorController>(StringComparer.Ordinal);
            foreach (var pair in clips)
            {
                var controllerPath = AnimatorControllerRootPath + "/" + pair.Key + "_Controller.controller";
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                if (controller == null)
                {
                    controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                }

                var stateMachine = controller.layers[0].stateMachine;
                var states = stateMachine.states;
                for (var i = states.Length - 1; i >= 0; i--)
                {
                    stateMachine.RemoveState(states[i].state);
                }

                var state = stateMachine.AddState(pair.Key);
                state.motion = pair.Value;
                state.writeDefaultValues = false;
                stateMachine.defaultState = state;
                EditorUtility.SetDirty(controller);
                result.Add(pair.Key, controller);
            }

            return result;
        }

        private static void ConfigureAnimation(GameObject instance, RuntimeAnimatorController controller)
        {
            var animator = instance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = instance.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        private static void ConfigureMotionTargetForState(Transform root, string clipName)
        {
            var target = FindChildRecursive(root, MotionPathTargetName);
            if (target == null)
            {
                return;
            }

            if (clipName == "Parvum_Move")
            {
                target.localPosition = new Vector3(0f, 0f, 1.05f);
            }
            else if (clipName == "Parvum_Attack")
            {
                target.localPosition = new Vector3(0f, 0f, 0.72f);
            }
            else if (clipName == "Parvum_Hit")
            {
                target.localPosition = new Vector3(-0.4f, 0f, 0.55f);
            }
            else
            {
                target.localPosition = new Vector3(0f, 0f, 0.55f);
            }
        }

        private static void RemoveAnimatorController(GameObject instance)
        {
            var animator = instance.GetComponent<Animator>();
            if (animator != null)
            {
                animator.runtimeAnimatorController = null;
            }
        }

        private static Vector3[] BuildPlacementPositions(Bounds corridorBounds)
        {
            var result = new Vector3[RequiredPlacedCount];
            var startX = corridorBounds.center.x - (ParvumPlacementSpacing * (RequiredPlacedCount - 1) * 0.5f);
            var z = corridorBounds.min.z - MinimumCorridorZGap;
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = new Vector3(startX + i * ParvumPlacementSpacing, corridorBounds.min.y, z);
            }

            return result;
        }

        private static GameObject GetOrCreatePlacementRoot(Vector3[] targetPositions)
        {
            var existing = GameObject.Find(PlacementRootName);
            if (existing != null)
            {
                return existing;
            }

            var created = new GameObject(PlacementRootName);
            created.transform.position = GetAveragePosition(targetPositions);
            return created;
        }

        private static void ConfigureInitialParvumReviewCamera(Transform placementRoot)
        {
            var focus = placementRoot.Find("Parvum_03_Attack") ?? placementRoot.Find("Parvum_00_Static");
            if (focus == null)
            {
                return;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = GameObject.Find(ReviewCameraName);
                camera = cameraObject == null ? UnityEngine.Object.FindFirstObjectByType<Camera>() : cameraObject.GetComponent<Camera>();
            }

            if (camera == null)
            {
                return;
            }

            var bounds = GetRendererBounds(focus);
            var lookAt = bounds.center + Vector3.up * Mathf.Max(0.15f, bounds.extents.y * 0.12f);
            var front = focus.forward.sqrMagnitude > 0.001f ? focus.forward.normalized : Vector3.back;
            var distance = Mathf.Clamp(bounds.extents.magnitude * 1.6f, 3.2f, 8.5f);
            var position = lookAt + front * distance + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.35f, 0.45f, 1.35f);

            var inspectionCamera = camera.GetComponent<ModelingInspectionFreeCamera>();
            if (inspectionCamera != null)
            {
                inspectionCamera.ResetView(position, lookAt);
                EditorUtility.SetDirty(inspectionCamera);
            }
            else
            {
                camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation((lookAt - position).normalized, Vector3.up));
            }

            EditorUtility.SetDirty(camera.transform);
            EditorUtility.SetDirty(camera);
        }

        private static void ConfigureCargoRunAreaLightsForUrp(Scene scene)
        {
            var changedCount = 0;
            var roots = scene.GetRootGameObjects();
            for (var rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                var lights = roots[rootIndex].GetComponentsInChildren<Light>(true);
                for (var lightIndex = 0; lightIndex < lights.Length; lightIndex++)
                {
                    var light = lights[lightIndex];
                    var serializedLight = new SerializedObject(light);
                    var typeProperty = serializedLight.FindProperty("m_Type");
                    var lightmappingProperty = serializedLight.FindProperty("m_Lightmapping");
                    if (typeProperty == null || lightmappingProperty == null)
                    {
                        continue;
                    }

                    var isUrpAreaLight =
                        typeProperty.intValue == SerializedRectangleAreaLightType ||
                        typeProperty.intValue == SerializedDiscAreaLightType;
                    if (!isUrpAreaLight || lightmappingProperty.intValue == SerializedBakedLightmappingMode)
                    {
                        continue;
                    }

                    lightmappingProperty.intValue = SerializedBakedLightmappingMode;
                    serializedLight.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(light);
                    changedCount++;
                }
            }

            if (changedCount > 0)
            {
                Debug.Log("CargoRun URP area lights set to Baked for Parvum scene review. Count=" + changedCount.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void ClearPlacementChildren(Transform placementRoot)
        {
            for (var i = placementRoot.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(placementRoot.GetChild(i).gameObject);
            }
        }

        private static void RemoveLegacyParvumVisuals(Transform placementRoot)
        {
            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                RemoveLegacyParvumVisualsRecursive(roots[i].transform, placementRoot);
            }
        }

        private static void RemoveLegacyParvumVisualsRecursive(Transform root, Transform placementRoot)
        {
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                RemoveLegacyParvumVisualsRecursive(root.GetChild(i), placementRoot);
            }

            if (root == placementRoot || root.IsChildOf(placementRoot))
            {
                return;
            }

            if (root.name.Equals(LegacyParvumVisualName, StringComparison.OrdinalIgnoreCase))
            {
                UnityEngine.Object.DestroyImmediate(root.gameObject);
            }
        }

        private static void AlignObjectBottomToY(Transform root, float floorY)
        {
            var bounds = GetRendererBounds(root);
            root.position += new Vector3(0f, floorY - bounds.min.y, 0f);
            EnsurePhysicsComponents(root);
        }

        private static Bounds GetRendererBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var hasBounds = false;
            var bounds = new Bounds(root.position, Vector3.zero);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (!renderers[i].enabled)
                {
                    continue;
                }

                var rendererBounds = ResolveRendererGeometryBounds(renderers[i]);
                if (!hasBounds)
                {
                    bounds = rendererBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(rendererBounds);
                }
            }

            if (!hasBounds)
            {
                throw new InvalidOperationException("No enabled renderers found under " + root.name);
            }

            return bounds;
        }

        private static Bounds ResolveRendererGeometryBounds(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinnedRenderer && skinnedRenderer.sharedMesh != null)
            {
                return TransformLocalBounds(skinnedRenderer.sharedMesh.bounds, skinnedRenderer.transform.localToWorldMatrix);
            }

            return renderer.bounds;
        }

        private static Bounds TransformLocalBounds(Bounds localBounds, Matrix4x4 localToWorld)
        {
            var center = localBounds.center;
            var extents = localBounds.extents;
            var hasBounds = false;
            var worldBounds = new Bounds();
            for (var x = -1; x <= 1; x += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    for (var z = -1; z <= 1; z += 2)
                    {
                        var localCorner = center + new Vector3(extents.x * x, extents.y * y, extents.z * z);
                        var worldCorner = localToWorld.MultiplyPoint3x4(localCorner);
                        if (!hasBounds)
                        {
                            worldBounds = new Bounds(worldCorner, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            worldBounds.Encapsulate(worldCorner);
                        }
                    }
                }
            }

            return worldBounds;
        }

        private static GameObject RequireObject(string objectName)
        {
            var target = GameObject.Find(objectName);
            if (target == null)
            {
                throw new InvalidOperationException("Missing object in CargoRunMvp scene: " + objectName);
            }

            return target;
        }

        private static SceneReview InspectAppliedSceneState(
            Transform placementRoot,
            Bounds corridorBounds,
            Dictionary<string, AnimationClip> clips)
        {
            if (placementRoot.childCount != RequiredPlacedCount)
            {
                throw new InvalidOperationException("Parvum placement must contain exactly six objects in the current CargoRunMvp scene.");
            }

            var materialNames = new SortedSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < PlacementSpecs.Length; i++)
            {
                var spec = PlacementSpecs[i];
                var child = placementRoot.Find(spec.ObjectName);
                if (child == null)
                {
                    throw new InvalidOperationException("Missing Parvum scene object: " + spec.ObjectName);
                }

                var bounds = GetRendererBounds(child);
                var zGap = corridorBounds.min.z - bounds.max.z;
                if (zGap < 0.1f)
                {
                    throw new InvalidOperationException(spec.ObjectName + " is not below the corridor object in the current scene.");
                }

                var groundDelta = Mathf.Abs(bounds.min.y - corridorBounds.min.y);
                if (groundDelta > GroundContactTolerance)
                {
                    throw new InvalidOperationException(spec.ObjectName + " is not seated on the current corridor floor.");
                }

                if (child.GetComponent<Rigidbody>() == null || child.GetComponent<BoxCollider>() == null)
                {
                    throw new InvalidOperationException(spec.ObjectName + " must have root Rigidbody and BoxCollider.");
                }

                var motionDriver = child.GetComponent<ParvumPhysicsMotionDriver>();
                if (motionDriver == null)
                {
                    throw new InvalidOperationException(spec.ObjectName + " must have ParvumPhysicsMotionDriver.");
                }

                if (!motionDriver.LockRootMotionForReview)
                {
                    throw new InvalidOperationException(spec.ObjectName + " must lock root Rigidbody movement for CargoRunMvp animation review.");
                }

                var enabledRendererCount = CountEnabledRenderers(child);
                if (enabledRendererCount != 1)
                {
                    throw new InvalidOperationException(spec.ObjectName + " must expose one enabled visible mesh. Count=" + enabledRendererCount);
                }

                CollectMaterialNames(child, materialNames);
                if (!string.IsNullOrEmpty(spec.AnimationClipName))
                {
                    if (!clips.TryGetValue(spec.AnimationClipName, out var clip) || !ClipHasBlendShapeBinding(clip, spec.BlendShapeName))
                    {
                        throw new InvalidOperationException(spec.AnimationClipName + " must drive its approved Parvum Shape Key.");
                    }
                }
            }

            RequireMaterialName(materialNames, "M_Parvum_Wet_Marbled_Green_Slime_Texture");
            RequireMaterialName(materialNames, "M_Parvum_Embedded_Grey_Green_Muzzle_Texture");
            RequireMaterialName(materialNames, "M_Parvum_Dark_Muzzle_Pores");
            RequireMaterialName(materialNames, "M_Parvum_Deep_Mouth_Cavity_No_Line_Objects");
            RequireMaterialName(materialNames, "M_Parvum_Irregular_Embedded_Teeth");
            RequireMaterialName(materialNames, "M_Parvum_Mouth_Tongue_Detail");

            return new SceneReview(
                RequiredPlacedCount,
                corridorBounds.min.y,
                corridorBounds.min.z,
                new List<string>(materialNames));
        }

        private static void RequireMaterialName(SortedSet<string> materialNames, string requiredName)
        {
            if (!materialNames.Contains(requiredName))
            {
                throw new InvalidOperationException("Current Parvum scene review is missing material: " + requiredName);
            }
        }

        private static int CountEnabledRenderers(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var count = 0;
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].enabled)
                {
                    count++;
                }
            }

            return count;
        }

        private static void CollectMaterialNames(Transform root, SortedSet<string> materialNames)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var materials = renderers[rendererIndex].sharedMaterials;
                for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    if (materials[materialIndex] != null)
                    {
                        materialNames.Add(materials[materialIndex].name);
                    }
                }
            }
        }

        private static bool ClipHasBlendShapeBinding(AnimationClip clip, string blendShapeName)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip);
            for (var i = 0; i < bindings.Length; i++)
            {
                if (bindings[i].propertyName.Equals("blendShape." + blendShapeName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void WriteReviewFiles(SceneReview review)
        {
            var reviewRoot = ToProjectAbsolutePath(ReviewRootRelativePath);
            Directory.CreateDirectory(reviewRoot);
            Directory.CreateDirectory(ToProjectAbsolutePath(ReviewCaptureRelativePath));

            File.WriteAllText(Path.Combine(reviewRoot, "ParvumCargoRunSceneReview.md"), BuildReviewMarkdown(review), Encoding.UTF8);
            File.WriteAllText(Path.Combine(reviewRoot, "ParvumCargoRunSceneReview.json"), BuildReviewJson(review), Encoding.UTF8);
        }

        private static string BuildReviewMarkdown(SceneReview review)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# 파르붐 CargoRunMvp 전용 검토");
            builder.AppendLine();
            builder.AppendLine("- 기준 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`");
            builder.AppendLine("- 승인 샘플: `artSample/enemies/parvum_physics_rig_rework_sample/`");
            builder.AppendLine("- 기존 Harness/EditMode/PlayMode/Build 및 기존 Validate/Ensure/Smoke/Run 검증 루프: 실행하지 않음");
            builder.AppendLine("- 검토 산출 위치: `docs/validation/parvum_cargo_run_scene/`");
            builder.AppendLine();
            builder.AppendLine("## 적용 요약");
            builder.AppendLine();
            builder.AppendLine("| 항목 | 내용 |");
            builder.AppendLine("| --- | --- |");
            builder.AppendLine("| 배치 수 | " + review.PlacedCount.ToString(CultureInfo.InvariantCulture) + "개 |");
            builder.AppendLine("| 비교용 정적 개체 | `Parvum_00_Static` |");
            builder.AppendLine("| 애니메이션 개체 | `Parvum_01_Idle`, `Parvum_02_Move`, `Parvum_03_Attack`, `Parvum_04_Hit`, `Parvum_05_Death` |");
            builder.AppendLine("| 표시 메시 | `Unified_Parvum_Reference_Matched_Single_Mesh` 단일 Renderer |");
            builder.AppendLine("| 루트 모션 | `Rigidbody + BoxCollider + ParvumPhysicsMotionDriver`, 현재 검토 배치는 루트 이동 잠금 |");
            builder.AppendLine("| Motion Path 역할 | 목표 Transform만 애니메이션하고 실제 런타임 이동 구조는 유지하되, 현재 씬 검토 배치는 Rigidbody 이동을 잠금 |");
            builder.AppendLine("| IK/Joint/Jiggle 역할 | 비표시 helper target, Animation Rigging marker, ConfigurableJoint, JiggleRig marker 구성 |");
            builder.AppendLine();
            builder.AppendLine("## 텍스처/머티리얼");
            builder.AppendLine();
            builder.AppendLine("| 구분 | 적용 파일 또는 머티리얼 | 용도 |");
            builder.AppendLine("| --- | --- | --- |");
            builder.AppendLine("| 텍스처 | `parvum_slime_albedo.png`, `parvum_slime_roughness.png`, `parvum_slime_bump.png`, `parvum_white_fleck_mask.png` | 젖은 초록 슬라임 표면, 얼룩, 거칠기, 범프 |");
            builder.AppendLine("| 텍스처 | `parvum_muzzle_scale_albedo.png`, `parvum_muzzle_scale_bump.png` | 몸통에서 이어지는 회녹색 주둥이 질감 |");
            builder.AppendLine("| 텍스처 | `parvum_mouth_cavity_albedo.png`, `parvum_tooth_albedo.png`, `parvum_tongue_albedo.png` | 입 내부, 치아, 혀 표면 |");
            for (var i = 0; i < review.MaterialNames.Count; i++)
            {
                builder.AppendLine("| 머티리얼 | `" + review.MaterialNames[i] + "` | 승인 샘플 FBX 슬롯에 재매핑 |");
            }

            builder.AppendLine();
            builder.AppendLine("## 애니메이션 적용 방식");
            builder.AppendLine();
            builder.AppendLine("| 개체 | 방식 |");
            builder.AppendLine("| --- | --- |");
            builder.AppendLine("| `Parvum_01_Idle` | `Idle_Pulse_Surface_Jiggle` Shape Key와 Jiggle helper target 커브 |");
            builder.AppendLine("| `Parvum_02_Move` | 낮은 전진 출렁임 중심. `Move_Squash_Forward_Slosh` Shape Key와 작은 Motion Path target 반복 |");
            builder.AppendLine("| `Parvum_03_Attack` | 이동보다 큰 전방 압축/도약. `Attack_Bite_Core_Kick` + `Attack_Teeth_Chomp`, 입 IK/Joint target 씹기 커브 |");
            builder.AppendLine("| `Parvum_04_Hit` | 깨지는 변형을 줄이고 `Hit_Slow_Recoil` 중심으로 뒤로 물러난 뒤 굼뜨게 복귀 |");
            builder.AppendLine("| `Parvum_05_Death` | `Death_Flatten_Liquid_Spread` + `Death_Liquefy_Collapse`로 아래로 녹아내리고 퍼지는 액체화 |");
            return builder.ToString();
        }

        private static string BuildReviewJson(SceneReview review)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"scene\": \"Assets/_Project/Scenes/CargoRunMvp.unity\",");
            builder.AppendLine("  \"sample\": \"artSample/enemies/parvum_physics_rig_rework_sample\",");
            builder.AppendLine("  \"oldValidationLoopsExecuted\": false,");
            builder.AppendLine("  \"placedCount\": " + review.PlacedCount.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"staticObject\": \"Parvum_00_Static\",");
            builder.AppendLine("  \"animatedObjects\": [\"Parvum_01_Idle\", \"Parvum_02_Move\", \"Parvum_03_Attack\", \"Parvum_04_Hit\", \"Parvum_05_Death\"],");
            builder.AppendLine("  \"rootMotion\": \"Rigidbody + BoxCollider + ParvumPhysicsMotionDriver\",");
            builder.AppendLine("  \"reviewRootMotionLocked\": true,");
            builder.AppendLine("  \"materialNames\": [");
            for (var i = 0; i < review.MaterialNames.Count; i++)
            {
                var suffix = i == review.MaterialNames.Count - 1 ? string.Empty : ",";
                builder.AppendLine("    \"" + review.MaterialNames[i] + "\"" + suffix);
            }

            builder.AppendLine("  ]");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static void CaptureReviewImages(Transform placementRoot, Bounds corridorBounds)
        {
            var staticObject = placementRoot.Find("Parvum_00_Static");
            if (staticObject == null)
            {
                throw new InvalidOperationException("Missing static Parvum object for review captures.");
            }

            var objectBounds = GetRendererBounds(staticObject);
            CaptureView("front.png", objectBounds, new Vector3(0f, 0.2f, -1f), corridorBounds);
            CaptureView("side.png", objectBounds, new Vector3(1f, 0.2f, -0.05f), corridorBounds);
            CaptureView("three_quarter.png", objectBounds, new Vector3(0.7f, 0.32f, -0.7f), corridorBounds);
            CaptureView("placement_group.png", GetRendererBounds(placementRoot), new Vector3(0.7f, 0.42f, -0.7f), corridorBounds);
        }

        private static void CaptureView(string fileName, Bounds bounds, Vector3 direction, Bounds corridorBounds)
        {
            var outputPath = ToProjectAbsolutePath(ReviewCaptureRelativePath + "/" + fileName);
            var cameraObject = new GameObject("Parvum_CargoRun_ReviewCamera");
            var lightObject = new GameObject("Parvum_CargoRun_ReviewLight");
            var renderTexture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.03f, 0.035f, 0.04f, 1f);
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) * 1.35f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 300f;
                camera.targetTexture = renderTexture;

                var normalized = direction.normalized;
                camera.transform.position = bounds.center + normalized * 18f + Vector3.up * 1.2f;
                camera.transform.LookAt(bounds.center + Vector3.up * Mathf.Max(0.1f, corridorBounds.size.y * 0.08f));

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.shadows = LightShadows.None;
                light.intensity = 2.4f;
                light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

                camera.Render();
                RenderTexture.active = renderTexture;
                var texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            finally
            {
                RenderTexture.active = null;
                var camera = cameraObject.GetComponent<Camera>();
                if (camera != null)
                {
                    camera.targetTexture = null;
                }

                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private static void SelectAndFramePlacedObjects(Transform placementRoot)
        {
            var selectedObjects = new UnityEngine.Object[placementRoot.childCount];
            for (var i = 0; i < placementRoot.childCount; i++)
            {
                selectedObjects[i] = placementRoot.GetChild(i).gameObject;
            }

            Selection.objects = selectedObjects;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }
        }

        private static Transform FindChildRecursive(Transform parent, string objectName)
        {
            if (parent.name == objectName)
            {
                return parent;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var child = FindChildRecursive(parent.GetChild(i), objectName);
                if (child != null)
                {
                    return child;
                }
            }

            return null;
        }

        private static Vector3 GetAveragePosition(Vector3[] positions)
        {
            var sum = Vector3.zero;
            for (var i = 0; i < positions.Length; i++)
            {
                sum += positions[i];
            }

            return sum / positions.Length;
        }

        private static string ToProjectAbsolutePath(string relativePath)
        {
            var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(ProjectRoot, normalized);
        }

        private static string ProjectRoot
        {
            get
            {
                var assetsPath = Application.dataPath.Replace('\\', '/');
                return Directory.GetParent(assetsPath)?.FullName ?? throw new InvalidOperationException("Could not resolve project root.");
            }
        }

        private readonly struct PlacementSpec
        {
            public PlacementSpec(string objectName, string animationClipName, string blendShapeName)
            {
                ObjectName = objectName;
                AnimationClipName = animationClipName;
                BlendShapeName = blendShapeName;
            }

            public string ObjectName { get; }

            public string AnimationClipName { get; }

            public string BlendShapeName { get; }
        }

        private readonly struct MaterialSet
        {
            public MaterialSet(
                Material slime,
                Material internalFlow,
                Material muzzleBlend,
                Material muzzle,
                Material darkPores,
                Material mouthDark,
                Material tooth,
                Material tongue)
            {
                Slime = slime;
                InternalFlow = internalFlow;
                MuzzleBlend = muzzleBlend;
                Muzzle = muzzle;
                DarkPores = darkPores;
                MouthDark = mouthDark;
                Tooth = tooth;
                Tongue = tongue;
            }

            public Material Slime { get; }

            public Material InternalFlow { get; }

            public Material MuzzleBlend { get; }

            public Material Muzzle { get; }

            public Material DarkPores { get; }

            public Material MouthDark { get; }

            public Material Tooth { get; }

            public Material Tongue { get; }

            public Material[] ToRendererMaterialArray()
            {
                return new[]
                {
                    Slime,
                    InternalFlow,
                    MuzzleBlend,
                    Muzzle,
                    DarkPores,
                    MouthDark,
                    Tooth,
                    Tongue,
                };
            }
        }

        private readonly struct PositionKey
        {
            public PositionKey(float time, float x, float y, float z)
            {
                Time = time;
                X = x;
                Y = y;
                Z = z;
            }

            public float Time { get; }

            public float X { get; }

            public float Y { get; }

            public float Z { get; }
        }

        private readonly struct SceneReview
        {
            public SceneReview(int placedCount, float floorY, float corridorMinZ, List<string> materialNames)
            {
                PlacedCount = placedCount;
                FloorY = floorY;
                CorridorMinZ = corridorMinZ;
                MaterialNames = materialNames;
            }

            public int PlacedCount { get; }

            public float FloorY { get; }

            public float CorridorMinZ { get; }

            public List<string> MaterialNames { get; }
        }
    }
}
