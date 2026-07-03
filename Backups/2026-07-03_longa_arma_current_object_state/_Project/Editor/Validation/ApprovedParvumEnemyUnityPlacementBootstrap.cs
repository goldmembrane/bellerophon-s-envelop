using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class ApprovedParvumEnemyUnityPlacementBootstrap
    {
        public const string PlacementRootName = "Approved Parvum Enemy Placement";

        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string CorridorRootName = "Approved Ship Corridor Segments";
        private const string ArtRootPath = "Assets/_Project/Art/Enemies/Parvum";
        private const string ModelAssetPath = ArtRootPath + "/Models/parvum.fbx";
        private const string TextureRootPath = ArtRootPath + "/Textures";
        private const string MaterialRootPath = ArtRootPath + "/Materials";
        private const string AnimationRootPath = ArtRootPath + "/Animations";
        private const string AnimatorControllerRootPath = AnimationRootPath + "/Controllers";
        private const string PrefabRootPath = "Assets/_Project/Prefabs/Enemies/Parvum";
        private const string PrefabPath = PrefabRootPath + "/ParvumApproved.prefab";
        private const string ApprovalStatusRelativePath = "artSample/enemies/parvum/APPROVAL_STATUS.json";
        private const string ModelChildPath = "ParvumApproved_Model";
        private const string PlayerRootName = "Player";
        private const string ModelCameraName = "Model Cam";
        private const int RequiredPlacedCount = 6;
        private const float ParvumSceneScale = 200f;
        private const float MinimumVisibleBoundsHeight = 0.75f;
        private const float MinimumCorridorZGap = 2.0f;
        private const float ParvumPlacementSpacing = 3.0f;
        private const float PlayStartFrontGap = 2.35f;
        private const float PlayCameraFrontGap = 3.3f;
        private const float PlayCameraHeight = 1.55f;
        private const float GroundContactTolerance = 0.03f;
        private const float ModelFacingRotationToleranceDegrees = 1f;
        private const float MaximumAnimatedBoundsGrowthFactor = 1.45f;
        private const int MinimumAnimatedCurveBindings = 18;
        private const int MinimumAnimatedCurvePaths = 5;
        private const int MaximumRotationCurveBindings = 0;

        private static readonly Vector3 ParvumModelFacingEuler = new Vector3(-90f, 0f, 0f);

        private static readonly PlacementSpec[] PlacementSpecs =
        {
            new PlacementSpec("Parvum_00_Static", null),
            new PlacementSpec("Parvum_01_Idle", "Parvum_Idle"),
            new PlacementSpec("Parvum_02_Move", "Parvum_Move"),
            new PlacementSpec("Parvum_03_Attack", "Parvum_Attack"),
            new PlacementSpec("Parvum_04_Hit", "Parvum_Hit"),
            new PlacementSpec("Parvum_05_Death", "Parvum_Death"),
        };

        [MenuItem("Bellerophon/Bootstrap/Apply Approved Parvum Enemy Placement")]
        public static void ApplyApprovedParvumEnemyPlacement()
        {
            RequireApprovedSample();
            EnsureUnityAssetFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            EnsureTextureImportSettings();
            AssetDatabase.ImportAsset(ModelAssetPath, ImportAssetOptions.ForceUpdate);

            var materials = EnsureMaterials();
            var prefab = EnsurePrefab(materials);
            var clips = EnsureAnimationClips(prefab);
            var controllers = EnsureAnimatorControllers(clips);

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var corridorRoot = RequireObject(CorridorRootName);
            DeleteGeneratedObject(PlacementRootName);
            var corridorBounds = GetRendererBounds(corridorRoot.transform);

            var placementRoot = new GameObject(PlacementRootName);
            var targetPositions = BuildPlacementPositions(corridorBounds);
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
                instance.transform.SetPositionAndRotation(targetPositions[i], Quaternion.identity);
                instance.transform.localScale = Vector3.one * ParvumSceneScale;
                instance.transform.SetParent(placementRoot.transform, true);
                DisableAllColliders(instance.transform);

                if (!string.IsNullOrEmpty(spec.AnimationClipName))
                {
                    ConfigureAnimation(instance, controllers[spec.AnimationClipName]);
                }

                AlignObjectBottomToY(instance.transform, corridorBounds.min.y);
            }

            PositionPlayStartInFrontOfParvum(placementRoot.transform, corridorBounds);
            ValidateApprovedParvumEnemyPlacement();

            SelectAndFramePlacedObjects(placementRoot.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Approved Parvum enemy placement applied. Root=" +
                PlacementRootName +
                "; Count=" +
                RequiredPlacedCount.ToString(CultureInfo.InvariantCulture) +
                "; CorridorMinZ=" +
                corridorBounds.min.z.ToString("0.###", CultureInfo.InvariantCulture) +
                "; PlacementZ=" +
                targetPositions[0].z.ToString("0.###", CultureInfo.InvariantCulture) +
                "; Static=1; Animations=Idle,Move,Attack,Hit,Death; ZAxisBelowCorridor=True; PlayStartInFront=True");
        }

        [MenuItem("Bellerophon/Validation/Validate Approved Parvum Enemy Placement")]
        public static void ValidateApprovedParvumEnemyPlacement()
        {
            if (SceneManager.GetActiveScene().path != CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            }

            var corridorRoot = RequireObject(CorridorRootName);
            var placementRoot = RequireObject(PlacementRootName);
            var corridorBounds = GetRendererBounds(corridorRoot.transform);

            if (placementRoot.transform.childCount != RequiredPlacedCount)
            {
                throw new InvalidOperationException(
                    "Approved Parvum placement must contain exactly " +
                    RequiredPlacedCount.ToString(CultureInfo.InvariantCulture) +
                    " direct children.");
            }

            var staticComparisonObject = placementRoot.transform.Find(PlacementSpecs[0].ObjectName);
            if (staticComparisonObject == null)
            {
                throw new InvalidOperationException("Missing static approved Parvum comparison object: " + PlacementSpecs[0].ObjectName);
            }

            var staticComparisonBounds = GetRendererBounds(staticComparisonObject);

            for (var i = 0; i < PlacementSpecs.Length; i++)
            {
                var spec = PlacementSpecs[i];
                var child = placementRoot.transform.Find(spec.ObjectName);
                if (child == null)
                {
                    throw new InvalidOperationException("Missing approved Parvum placement object: " + spec.ObjectName);
                }

                var bounds = GetRendererBounds(child);
                if (i > 0)
                {
                    ValidateAnimatedBoundsCloseToStatic(spec.ObjectName, bounds, staticComparisonBounds);
                }

                var zGap = corridorBounds.min.z - bounds.max.z;
                if (zGap < MinimumCorridorZGap)
                {
                    throw new InvalidOperationException(
                        spec.ObjectName +
                        " must keep a wider gap below the corridor on the Unity Z axis. Gap=" +
                        zGap.ToString("0.###", CultureInfo.InvariantCulture) +
                        "; RequiredMinimum=" +
                        MinimumCorridorZGap.ToString("0.###", CultureInfo.InvariantCulture));
                }

                var renderers = child.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    throw new InvalidOperationException(spec.ObjectName + " must contain the approved Parvum model renderers.");
                }

                if (bounds.size.y < MinimumVisibleBoundsHeight)
                {
                    throw new InvalidOperationException(
                        spec.ObjectName +
                        " is too small to inspect in the scene. BoundsHeight=" +
                        bounds.size.y.ToString("0.###", CultureInfo.InvariantCulture) +
                        "; RequiredMinimum=" +
                        MinimumVisibleBoundsHeight.ToString("0.###", CultureInfo.InvariantCulture));
                }

                var groundDelta = Mathf.Abs(bounds.min.y - corridorBounds.min.y);
                if (groundDelta > GroundContactTolerance)
                {
                    throw new InvalidOperationException(
                        spec.ObjectName +
                        " must sit on the corridor floor on the Unity Y axis. ObjectMinY=" +
                        bounds.min.y.ToString("0.###", CultureInfo.InvariantCulture) +
                        "; FloorY=" +
                        corridorBounds.min.y.ToString("0.###", CultureInfo.InvariantCulture) +
                        "; Delta=" +
                        groundDelta.ToString("0.###", CultureInfo.InvariantCulture));
                }

                if (!ApproximatelyUniformScale(child.localScale, ParvumSceneScale))
                {
                    throw new InvalidOperationException(
                        spec.ObjectName +
                        " must use Parvum scene scale " +
                        ParvumSceneScale.ToString("0.###", CultureInfo.InvariantCulture) +
                        ". ActualScale=" +
                        FormatVector(child.localScale));
                }

                ValidateModelFacing(child, spec.ObjectName);

                var animator = child.GetComponent<Animator>();
                if (string.IsNullOrEmpty(spec.AnimationClipName))
                {
                    if (animator != null && animator.runtimeAnimatorController != null)
                    {
                        throw new InvalidOperationException(spec.ObjectName + " must remain the static comparison object.");
                    }
                }
                else if (animator == null ||
                         animator.runtimeAnimatorController == null ||
                         animator.runtimeAnimatorController.name != spec.AnimationClipName + "_Controller")
                {
                    throw new InvalidOperationException(
                        spec.ObjectName +
                        " must have the required AnimatorController for animation clip: " +
                        spec.AnimationClipName);
                }

                if (!string.IsNullOrEmpty(spec.AnimationClipName))
                {
                    ValidateDetailedAnimationCurves(animator, spec.AnimationClipName);
                }
            }

            ValidatePlayStartInFrontOfParvum(placementRoot.transform, corridorBounds);
            Debug.Log(BuildInspectionSummary(placementRoot.transform, corridorBounds));
            Debug.Log(
                "Approved Parvum enemy placement validation passed. Root=" +
                PlacementRootName +
                "; Count=6; Static=1; Animated=5; CorridorMinZ=" +
                corridorBounds.min.z.ToString("0.###", CultureInfo.InvariantCulture) +
                "; ZAxisBelowCorridor=True; PlayStartInFront=True");
        }

        private static void RequireApprovedSample()
        {
            var approvalPath = ToProjectAbsolutePath(ApprovalStatusRelativePath);
            if (!File.Exists(approvalPath))
            {
                throw new InvalidOperationException("Missing Parvum approval status file: " + ApprovalStatusRelativePath);
            }

            var approvalText = File.ReadAllText(approvalPath);
            if (!approvalText.Contains("\"unityApplicationAllowed\": true"))
            {
                throw new InvalidOperationException("Approved Parvum sample is not marked as allowed for Unity application.");
            }

            if (!File.Exists(ToProjectAbsolutePath(ModelAssetPath)))
            {
                throw new InvalidOperationException("Missing approved Parvum FBX copy: " + ModelAssetPath);
            }
        }

        private static void EnsureUnityAssetFolders()
        {
            EnsureAssetDirectory(ArtRootPath);
            EnsureAssetDirectory(ArtRootPath + "/Models");
            EnsureAssetDirectory(TextureRootPath);
            EnsureAssetDirectory(MaterialRootPath);
            EnsureAssetDirectory(AnimationRootPath);
            EnsureAssetDirectory(AnimatorControllerRootPath);
            EnsureAssetDirectory("Assets/_Project/Prefabs/Enemies");
            EnsureAssetDirectory(PrefabRootPath);
        }

        private static void EnsureAssetDirectory(string assetPath)
        {
            Directory.CreateDirectory(ToProjectAbsolutePath(assetPath));
        }

        private static void EnsureTextureImportSettings()
        {
            EnsureTextureImported(TextureRootPath + "/parvum_slime_albedo.png", false);
            EnsureTextureImported(TextureRootPath + "/parvum_slime_roughness.png", false);
            EnsureTextureImported(TextureRootPath + "/parvum_white_fleck_mask.png", false);
            EnsureTextureImported(TextureRootPath + "/parvum_snout_scale_albedo.png", false);
            EnsureTextureImported(TextureRootPath + "/parvum_snout_scale_bump.png", true);
            EnsureTextureImported(TextureRootPath + "/parvum_tooth_albedo.png", false);
            EnsureTextureImported(TextureRootPath + "/parvum_tongue_albedo.png", false);
        }

        private static void EnsureTextureImported(string assetPath, bool normalMap)
        {
            if (!File.Exists(ToProjectAbsolutePath(assetPath)))
            {
                throw new InvalidOperationException("Missing Parvum texture asset: " + assetPath);
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            var expectedType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            var changed = false;
            if (importer.textureType != expectedType)
            {
                importer.textureType = expectedType;
                changed = true;
            }

            if (!normalMap && !importer.sRGBTexture)
            {
                importer.sRGBTexture = true;
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
            var snoutAlbedo = LoadTexture(TextureRootPath + "/parvum_snout_scale_albedo.png");
            var snoutBump = LoadTexture(TextureRootPath + "/parvum_snout_scale_bump.png");
            var toothAlbedo = LoadTexture(TextureRootPath + "/parvum_tooth_albedo.png");
            var tongueAlbedo = LoadTexture(TextureRootPath + "/parvum_tongue_albedo.png");

            return new MaterialSet(
                EnsureMaterial(
                    MaterialRootPath + "/M_Parvum_Slime.mat",
                    "M_Parvum_Slime",
                    new Color(0.05f, 0.68f, 0.32f, 0.72f),
                    slimeAlbedo,
                    null,
                    0.82f,
                    true),
                EnsureMaterial(
                    MaterialRootPath + "/M_Parvum_OuterSlime.mat",
                    "M_Parvum_OuterSlime",
                    new Color(0.28f, 0.95f, 0.62f, 0.46f),
                    slimeAlbedo,
                    null,
                    0.92f,
                    true),
                EnsureMaterial(
                    MaterialRootPath + "/M_Parvum_Puddle.mat",
                    "M_Parvum_Puddle",
                    new Color(0.03f, 0.45f, 0.22f, 0.34f),
                    slimeAlbedo,
                    null,
                    0.9f,
                    true),
                EnsureMaterial(
                    MaterialRootPath + "/M_Parvum_Snout.mat",
                    "M_Parvum_Snout",
                    new Color(0.52f, 0.64f, 0.52f, 1f),
                    snoutAlbedo,
                    snoutBump,
                    0.18f,
                    false),
                EnsureMaterial(
                    MaterialRootPath + "/M_Parvum_DarkScale.mat",
                    "M_Parvum_DarkScale",
                    new Color(0.03f, 0.04f, 0.03f, 1f),
                    null,
                    null,
                    0.12f,
                    false),
                EnsureMaterial(
                    MaterialRootPath + "/M_Parvum_Tooth.mat",
                    "M_Parvum_Tooth",
                    new Color(0.78f, 0.67f, 0.45f, 1f),
                    toothAlbedo,
                    null,
                    0.2f,
                    false),
                EnsureMaterial(
                    MaterialRootPath + "/M_Parvum_Tongue.mat",
                    "M_Parvum_Tongue",
                    new Color(0.72f, 0.08f, 0.04f, 1f),
                    tongueAlbedo,
                    null,
                    0.62f,
                    false),
                EnsureMaterial(
                    MaterialRootPath + "/M_Parvum_MouthDark.mat",
                    "M_Parvum_MouthDark",
                    new Color(0.015f, 0.01f, 0.012f, 1f),
                    null,
                    null,
                    0.08f,
                    false),
                EnsureMaterial(
                    MaterialRootPath + "/M_Parvum_PaleFleck.mat",
                    "M_Parvum_PaleFleck",
                    new Color(0.82f, 0.84f, 0.76f, 1f),
                    null,
                    null,
                    0.32f,
                    false));
        }

        private static Texture2D LoadTexture(string assetPath)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                throw new InvalidOperationException("Parvum texture failed to import: " + assetPath);
            }

            return texture;
        }

        private static Material EnsureMaterial(
            string assetPath,
            string materialName,
            Color color,
            Texture2D albedo,
            Texture2D normal,
            float smoothness,
            bool transparent)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, assetPath);
            }

            material.name = materialName;
            material.color = color;
            SetColor(material, color);
            SetTexture(material, albedo);
            SetNormalTexture(material, normal);
            SetSmoothness(material, smoothness);
            if (transparent)
            {
                ConfigureTransparent(material);
            }
            else
            {
                ConfigureOpaque(material);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetColor(Material material, Color color)
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

        private static void SetTexture(Material material, Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }
        }

        private static void SetNormalTexture(Material material, Texture2D texture)
        {
            if (texture == null || !material.HasProperty("_BumpMap"))
            {
                return;
            }

            material.SetTexture("_BumpMap", texture);
            material.EnableKeyword("_NORMALMAP");
        }

        private static void SetSmoothness(Material material, float smoothness)
        {
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }
        }

        private static void ConfigureTransparent(Material material)
        {
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_AlphaClip", 0f);
                material.SetFloat("_ZWrite", 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.DisableKeyword("_ALPHATEST_ON");
                material.renderQueue = (int)RenderQueue.Transparent;
                return;
            }

            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static void ConfigureOpaque(Material material)
        {
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 0f);
                material.SetFloat("_ZWrite", 1f);
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 0f);
            }

            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = -1;
        }

        private static GameObject EnsurePrefab(MaterialSet materials)
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath);
            if (modelAsset == null)
            {
                throw new InvalidOperationException("Approved Parvum FBX was not imported as a prefab asset: " + ModelAssetPath);
            }

            var wrapper = new GameObject("ParvumApproved");
            var modelInstance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            if (modelInstance == null)
            {
                modelInstance = UnityEngine.Object.Instantiate(modelAsset);
            }

            modelInstance.name = ModelChildPath;
            modelInstance.transform.SetParent(wrapper.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.Euler(ParvumModelFacingEuler);
            modelInstance.transform.localScale = Vector3.one;

            AssignMaterials(wrapper, materials);
            DisableAllColliders(wrapper.transform);

            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(wrapper, PrefabPath);
            UnityEngine.Object.DestroyImmediate(wrapper);
            if (savedPrefab == null)
            {
                throw new InvalidOperationException("Failed to save approved Parvum prefab: " + PrefabPath);
            }

            return savedPrefab;
        }

        private static void AssignMaterials(GameObject root, MaterialSet materials)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                var sharedMaterials = renderer.sharedMaterials;
                for (var materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
                {
                    sharedMaterials[materialIndex] = ChooseMaterial(renderer, sharedMaterials[materialIndex], materials);
                }

                renderer.sharedMaterials = sharedMaterials;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static Material ChooseMaterial(Renderer renderer, Material original, MaterialSet materials)
        {
            var originalName = original == null ? string.Empty : original.name;
            var key = (renderer.gameObject.name + " " + originalName).ToLowerInvariant();

            if (key.Contains("tooth"))
            {
                return materials.Tooth;
            }

            if (key.Contains("tongue") || key.Contains("inner flesh"))
            {
                return materials.Tongue;
            }

            if (key.Contains("mouth dark") || key.Contains("maw") || key.Contains("inside mouth"))
            {
                return materials.MouthDark;
            }

            if (key.Contains("dark scale") || key.Contains("scale pore") || key.Contains("pit"))
            {
                return materials.DarkScale;
            }

            if (key.Contains("snout") || key.Contains("lip") || key.Contains("gum"))
            {
                return materials.Snout;
            }

            if (key.Contains("puddle") || key.Contains("floor rim") || key.Contains("floor slime"))
            {
                return materials.Puddle;
            }

            if (key.Contains("outer"))
            {
                return materials.OuterSlime;
            }

            if (key.Contains("fleck") || key.Contains("metallic"))
            {
                return materials.PaleFleck;
            }

            return materials.Slime;
        }

        private static Dictionary<string, AnimationClip> EnsureAnimationClips(GameObject prefab)
        {
            var parts = BuildPartPathSet(prefab.transform);
            var clips = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);
            clips["Parvum_Idle"] = CreateIdleClip(parts);
            clips["Parvum_Move"] = CreateMoveClip(parts);
            clips["Parvum_Attack"] = CreateAttackClip(parts);
            clips["Parvum_Hit"] = CreateHitClip(parts);
            clips["Parvum_Death"] = CreateDeathClip(parts);
            return clips;
        }

        private static AnimationClip CreateIdleClip(PartPathSet parts)
        {
            return CreateClip(AnimationRootPath + "/Parvum_Idle.anim", "Parvum_Idle", WrapMode.Loop, clip =>
            {
                SetUniformScale(clip, parts.Body, K(0f, 1f), K(0.42f, 1.012f), K(0.82f, 0.996f), K(1.2f, 1f));
                SetPositionMotion(
                    clip,
                    parts.Body,
                    ZeroCurve(0f, 0.42f, 0.82f, 1.2f),
                    new[] { K(0f, 0f), K(0.42f, 0.00045f), K(0.82f, -0.00025f), K(1.2f, 0f) },
                    new[] { K(0f, 0f), K(0.42f, -0.00025f), K(0.82f, 0.0002f), K(1.2f, 0f) });
                SetUniformScale(clip, parts.OuterSlime, K(0f, 1f), K(0.56f, 1.01f), K(0.96f, 0.997f), K(1.2f, 1f));
                SetPositionXMotion(clip, parts.LeftLobe, K(0f, 0f), K(0.5f, -0.0008f), K(0.95f, 0.00045f), K(1.2f, 0f));
                SetPositionXMotion(clip, parts.RightLobe, K(0f, 0f), K(0.5f, 0.0008f), K(0.95f, -0.00045f), K(1.2f, 0f));
                SetPositionYMotion(clip, parts.UpperCrest, K(0f, 0f), K(0.48f, 0.0007f), K(0.94f, -0.00035f), K(1.2f, 0f));
                SetPositionYMotion(clip, parts.MouthRoot, K(0f, 0f), K(0.48f, 0.00045f), K(0.96f, -0.0003f), K(1.2f, 0f));
            });
        }

        private static AnimationClip CreateMoveClip(PartPathSet parts)
        {
            return CreateClip(AnimationRootPath + "/Parvum_Move.anim", "Parvum_Move", WrapMode.Loop, clip =>
            {
                SetUniformScale(clip, parts.Body, K(0f, 1f), K(0.32f, 1.014f), K(0.7f, 0.996f), K(1.1f, 1f));
                SetPositionMotion(
                    clip,
                    parts.Body,
                    ZeroCurve(0f, 0.24f, 0.5f, 0.82f, 1.1f),
                    new[] { K(0f, 0f), K(0.24f, 0.00065f), K(0.5f, -0.00025f), K(0.82f, 0.0004f), K(1.1f, 0f) },
                    new[] { K(0f, 0f), K(0.24f, -0.00045f), K(0.5f, 0.00025f), K(0.82f, -0.0002f), K(1.1f, 0f) });
                SetPositionXMotion(clip, parts.OuterSlime, K(0f, 0f), K(0.45f, -0.0008f), K(0.82f, 0.0006f), K(1.1f, 0f));
                SetPositionYMotion(clip, parts.FrontCradle, K(0f, 0f), K(0.36f, -0.00045f), K(0.72f, 0.00055f), K(1.1f, 0f));
                SetPositionYMotion(clip, parts.RearMass, K(0f, 0f), K(0.32f, 0.00055f), K(0.72f, -0.00035f), K(1.1f, 0f));
                SetPositionXMotion(clip, parts.LeftLobe, K(0f, 0f), K(0.28f, -0.001f), K(0.64f, 0.00055f), K(1.1f, 0f));
                SetPositionXMotion(clip, parts.RightLobe, K(0f, 0f), K(0.28f, 0.001f), K(0.64f, -0.00055f), K(1.1f, 0f));
                SetPositionXMotion(clip, parts.MouthRoot, K(0f, 0f), K(0.35f, -0.0006f), K(0.75f, 0.00045f), K(1.1f, 0f));
            });
        }

        private static AnimationClip CreateAttackClip(PartPathSet parts)
        {
            return CreateClip(AnimationRootPath + "/Parvum_Attack.anim", "Parvum_Attack", WrapMode.Loop, clip =>
            {
                SetUniformScale(clip, parts.Body, K(0f, 1f), K(0.16f, 1.028f), K(0.38f, 0.992f), K(0.82f, 1f));
                SetPositionMotion(
                    clip,
                    parts.Body,
                    ZeroCurve(0f, 0.16f, 0.38f, 0.82f),
                    new[] { K(0f, 0f), K(0.16f, 0.0008f), K(0.38f, -0.00035f), K(0.82f, 0f) },
                    new[] { K(0f, 0f), K(0.16f, 0.0007f), K(0.38f, -0.00045f), K(0.82f, 0f) });
                SetUniformScale(clip, parts.OuterSlime, K(0f, 1f), K(0.24f, 0.994f), K(0.5f, 1.016f), K(0.82f, 1f));
                SetPositionZMotion(clip, parts.MouthRoot, K(0f, 0f), K(0.16f, 0.00045f), K(0.36f, -0.00018f), K(0.82f, 0f));
                SetPositionZMotion(clip, parts.Snout, K(0f, 0f), K(0.16f, 0.0005f), K(0.36f, -0.00016f), K(0.82f, 0f));
                SetPositionZMotion(clip, parts.LipRing, K(0f, 0f), K(0.16f, 0.00042f), K(0.36f, -0.00014f), K(0.82f, 0f));
                SetPositionZMotion(clip, parts.Tongue, K(0f, 0f), K(0.16f, 0.00038f), K(0.36f, -0.00012f), K(0.82f, 0f));
                SetPositionYMotion(clip, parts.RearMass, K(0f, 0f), K(0.26f, 0.00085f), K(0.54f, -0.00045f), K(0.82f, 0f));
                SetPositionXMotion(clip, parts.LeftLobe, K(0f, 0f), K(0.28f, -0.0009f), K(0.55f, 0.0006f), K(0.82f, 0f));
                SetPositionXMotion(clip, parts.RightLobe, K(0f, 0f), K(0.28f, 0.0009f), K(0.55f, -0.0006f), K(0.82f, 0f));
            });
        }

        private static AnimationClip CreateHitClip(PartPathSet parts)
        {
            return CreateClip(AnimationRootPath + "/Parvum_Hit.anim", "Parvum_Hit", WrapMode.Loop, clip =>
            {
                SetUniformScale(clip, parts.Body, K(0f, 1f), K(0.12f, 0.986f), K(0.28f, 1.018f), K(0.48f, 0.996f), K(0.62f, 1f));
                SetPositionMotion(
                    clip,
                    parts.Body,
                    new[] { K(0f, 0f), K(0.12f, -0.00075f), K(0.28f, 0.0006f), K(0.48f, -0.00025f), K(0.62f, 0f) },
                    new[] { K(0f, 0f), K(0.12f, -0.00035f), K(0.28f, 0.00055f), K(0.48f, -0.00015f), K(0.62f, 0f) },
                    ZeroCurve(0f, 0.12f, 0.28f, 0.48f, 0.62f));
                SetUniformScale(clip, parts.OuterSlime, K(0f, 1f), K(0.18f, 1.014f), K(0.34f, 0.988f), K(0.52f, 1.006f), K(0.62f, 1f));
                SetPositionXMotion(clip, parts.LeftLobe, K(0f, 0f), K(0.16f, -0.0012f), K(0.32f, 0.0009f), K(0.5f, -0.0004f), K(0.62f, 0f));
                SetPositionXMotion(clip, parts.RightLobe, K(0f, 0f), K(0.16f, -0.0007f), K(0.32f, 0.001f), K(0.5f, -0.00045f), K(0.62f, 0f));
                SetPositionMotion(
                    clip,
                    parts.MouthRoot,
                    new[] { K(0f, 0f), K(0.12f, 0.00028f), K(0.28f, -0.00022f), K(0.48f, 0.00012f), K(0.62f, 0f) },
                    new[] { K(0f, 0f), K(0.12f, 0.00032f), K(0.28f, -0.0002f), K(0.48f, 0.00012f), K(0.62f, 0f) },
                    ZeroCurve(0f, 0.12f, 0.28f, 0.48f, 0.62f));
                SetPositionYMotion(clip, parts.UpperCrest, K(0f, 0f), K(0.18f, 0.001f), K(0.35f, -0.00065f), K(0.55f, 0f));
                SetPositionXMotion(clip, parts.FrontCradle, K(0f, 0f), K(0.14f, 0.0006f), K(0.32f, -0.0005f), K(0.62f, 0f));
            });
        }

        private static AnimationClip CreateDeathClip(PartPathSet parts)
        {
            return CreateClip(AnimationRootPath + "/Parvum_Death.anim", "Parvum_Death", WrapMode.Loop, clip =>
            {
                SetUniformScale(clip, parts.Body, K(0f, 1f), K(0.18f, 1.018f), K(0.48f, 0.95f), K(0.92f, 0.928f), K(1.35f, 1f));
                SetPositionMotion(
                    clip,
                    parts.Body,
                    ZeroCurve(0f, 0.18f, 0.48f, 0.92f, 1.35f),
                    new[] { K(0f, 0f), K(0.18f, 0.0007f), K(0.48f, -0.001f), K(0.92f, -0.0014f), K(1.35f, 0f) },
                    new[] { K(0f, 0f), K(0.18f, -0.00035f), K(0.48f, 0.00055f), K(0.92f, 0.0008f), K(1.35f, 0f) });
                SetUniformScale(clip, parts.OuterSlime, K(0f, 1f), K(0.28f, 1.012f), K(0.72f, 1.035f), K(1.35f, 1f));
                SetPositionXMotion(clip, parts.LeftLobe, K(0f, 0f), K(0.45f, -0.001f), K(0.92f, -0.0016f), K(1.35f, 0f));
                SetPositionXMotion(clip, parts.RightLobe, K(0f, 0f), K(0.45f, 0.001f), K(0.92f, 0.0016f), K(1.35f, 0f));
                SetPositionYMotion(clip, parts.RearMass, K(0f, 0f), K(0.45f, -0.00055f), K(0.92f, -0.001f), K(1.35f, 0f));
                SetPositionYMotion(clip, parts.FrontCradle, K(0f, 0f), K(0.45f, -0.00055f), K(0.92f, -0.001f), K(1.35f, 0f));
                SetPositionXMotion(clip, parts.MouthRoot, K(0f, 0f), K(0.42f, -0.00045f), K(0.92f, -0.00075f), K(1.35f, 0f));
                SetPositionYMotion(clip, parts.Tongue, K(0f, 0f), K(0.45f, -0.00035f), K(0.92f, -0.0007f), K(1.35f, 0f));
            });
        }

        private static AnimationClip CreateClip(
            string assetPath,
            string clipName,
            WrapMode wrapMode,
            Action<AnimationClip> configure)
        {
            AssetDatabase.DeleteAsset(assetPath);
            var clip = new AnimationClip
            {
                name = clipName,
                frameRate = 30f,
                legacy = false,
                wrapMode = wrapMode,
            };

            configure(clip);
            var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            clipSettings.loopTime = wrapMode == WrapMode.Loop;
            clipSettings.loopBlend = wrapMode == WrapMode.Loop;
            AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
            clip.EnsureQuaternionContinuity();
            AssetDatabase.CreateAsset(clip, assetPath);
            return clip;
        }

        private static void SetScaleX(AnimationClip clip, MotionTarget target, params CurveKey[] factorKeys)
        {
            SetFactorCurve(clip, target, "localScale.x", target.LocalScale.x, factorKeys);
        }

        private static void SetScaleY(AnimationClip clip, MotionTarget target, params CurveKey[] factorKeys)
        {
            SetFactorCurve(clip, target, "localScale.y", target.LocalScale.y, factorKeys);
        }

        private static void SetScaleZ(AnimationClip clip, MotionTarget target, params CurveKey[] factorKeys)
        {
            SetFactorCurve(clip, target, "localScale.z", target.LocalScale.z, factorKeys);
        }

        private static void SetUniformScale(AnimationClip clip, MotionTarget target, params CurveKey[] factorKeys)
        {
            SetScaleX(clip, target, factorKeys);
            SetScaleY(clip, target, factorKeys);
            SetScaleZ(clip, target, factorKeys);
        }

        private static void SetPositionX(AnimationClip clip, MotionTarget target, params CurveKey[] offsetKeys)
        {
            SetOffsetCurve(clip, target, "localPosition.x", target.LocalPosition.x, offsetKeys);
        }

        private static void SetPositionY(AnimationClip clip, MotionTarget target, params CurveKey[] offsetKeys)
        {
            SetOffsetCurve(clip, target, "localPosition.y", target.LocalPosition.y, offsetKeys);
        }

        private static void SetPositionZ(AnimationClip clip, MotionTarget target, params CurveKey[] offsetKeys)
        {
            SetOffsetCurve(clip, target, "localPosition.z", target.LocalPosition.z, offsetKeys);
        }

        private static void SetPositionXMotion(AnimationClip clip, MotionTarget target, params CurveKey[] offsetKeys)
        {
            var zeroKeys = ZeroCurveLike(offsetKeys);
            SetPositionMotion(clip, target, offsetKeys, zeroKeys, zeroKeys);
        }

        private static void SetPositionYMotion(AnimationClip clip, MotionTarget target, params CurveKey[] offsetKeys)
        {
            var zeroKeys = ZeroCurveLike(offsetKeys);
            SetPositionMotion(clip, target, zeroKeys, offsetKeys, zeroKeys);
        }

        private static void SetPositionZMotion(AnimationClip clip, MotionTarget target, params CurveKey[] offsetKeys)
        {
            var zeroKeys = ZeroCurveLike(offsetKeys);
            SetPositionMotion(clip, target, zeroKeys, zeroKeys, offsetKeys);
        }

        private static void SetPositionMotion(
            AnimationClip clip,
            MotionTarget target,
            CurveKey[] xOffsetKeys,
            CurveKey[] yOffsetKeys,
            CurveKey[] zOffsetKeys)
        {
            SetPositionX(clip, target, xOffsetKeys);
            SetPositionY(clip, target, yOffsetKeys);
            SetPositionZ(clip, target, zOffsetKeys);
        }

        private static void SetRotationZ(AnimationClip clip, MotionTarget target, params CurveKey[] offsetKeys)
        {
            SetOffsetCurve(clip, target, "localEulerAnglesRaw.z", target.LocalEulerAngles.z, offsetKeys);
        }

        private static void SetFactorCurve(
            AnimationClip clip,
            MotionTarget target,
            string propertyName,
            float baseValue,
            params CurveKey[] factorKeys)
        {
            var curve = new AnimationCurve();
            for (var i = 0; i < factorKeys.Length; i++)
            {
                curve.AddKey(new Keyframe(factorKeys[i].Time, baseValue * factorKeys[i].Value));
            }

            SmoothCurve(curve);
            clip.SetCurve(target.Path, typeof(Transform), propertyName, curve);
        }

        private static void SetOffsetCurve(
            AnimationClip clip,
            MotionTarget target,
            string propertyName,
            float baseValue,
            params CurveKey[] offsetKeys)
        {
            var curve = new AnimationCurve();
            for (var i = 0; i < offsetKeys.Length; i++)
            {
                curve.AddKey(new Keyframe(offsetKeys[i].Time, baseValue + offsetKeys[i].Value));
            }

            SmoothCurve(curve);
            clip.SetCurve(target.Path, typeof(Transform), propertyName, curve);
        }

        private static void SmoothCurve(AnimationCurve curve)
        {
            for (var i = 0; i < curve.length; i++)
            {
                curve.SmoothTangents(i, 0f);
            }
        }

        private static CurveKey K(float time, float value)
        {
            return new CurveKey(time, value);
        }

        private static CurveKey[] ZeroCurve(params float[] times)
        {
            var keys = new CurveKey[times.Length];
            for (var i = 0; i < times.Length; i++)
            {
                keys[i] = K(times[i], 0f);
            }

            return keys;
        }

        private static CurveKey[] ZeroCurveLike(CurveKey[] sourceKeys)
        {
            var keys = new CurveKey[sourceKeys.Length];
            for (var i = 0; i < sourceKeys.Length; i++)
            {
                keys[i] = K(sourceKeys[i].Time, 0f);
            }

            return keys;
        }

        private static PartPathSet BuildPartPathSet(Transform prefabRoot)
        {
            var model = RequireMotionTarget(prefabRoot, ModelChildPath);
            return new PartPathSet(
                model,
                FindMotionTarget(prefabRoot, "low broad translucent green slime mound"),
                FindMotionTarget(prefabRoot, "thin glossy outer slime skin"),
                FindMotionTarget(prefabRoot, "left lower sagging slime lobe"),
                FindMotionTarget(prefabRoot, "right lower sagging slime lobe"),
                FindMotionTarget(prefabRoot, "rear rounded slime mass"),
                FindMotionTarget(prefabRoot, "front mouth cradle slime mass"),
                FindMotionTarget(prefabRoot, "upper crest translucent slime dome"),
                FindMotionTarget(prefabRoot, "parvum animated mouth snout root"),
                FindMotionTarget(prefabRoot, "front protruding rough snout"),
                FindMotionTarget(prefabRoot, "large oval fleshy mouth lip ring"),
                FindMotionTarget(prefabRoot, "red tongue"));
        }

        private static MotionTarget RequireMotionTarget(Transform prefabRoot, string objectName)
        {
            var target = FindTransformByName(prefabRoot, objectName);
            if (target == null)
            {
                throw new InvalidOperationException("Missing Parvum animation target: " + objectName);
            }

            return CreateMotionTarget(prefabRoot, target);
        }

        private static MotionTarget FindMotionTarget(Transform prefabRoot, string partialName)
        {
            var transforms = prefabRoot.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return CreateMotionTarget(prefabRoot, transforms[i]);
                }
            }

            throw new InvalidOperationException("Missing Parvum animation target containing: " + partialName);
        }

        private static Transform FindTransformByName(Transform root, string objectName)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (string.Equals(transforms[i].name, objectName, StringComparison.Ordinal))
                {
                    return transforms[i];
                }
            }

            return null;
        }

        private static MotionTarget CreateMotionTarget(Transform prefabRoot, Transform target)
        {
            return new MotionTarget(
                GetRelativePath(prefabRoot, target),
                target.localPosition,
                target.localScale,
                target.localEulerAngles);
        }

        private static string GetRelativePath(Transform root, Transform target)
        {
            if (target == root)
            {
                return string.Empty;
            }

            var names = new List<string>();
            var current = target;
            while (current != null && current != root)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }

        private static Dictionary<string, AnimatorController> EnsureAnimatorControllers(Dictionary<string, AnimationClip> clips)
        {
            var controllers = new Dictionary<string, AnimatorController>(StringComparer.Ordinal);
            foreach (var entry in clips)
            {
                controllers[entry.Key] = EnsureAnimatorController(entry.Key, entry.Value);
            }

            return controllers;
        }

        private static AnimatorController EnsureAnimatorController(string controllerName, AnimationClip clip)
        {
            var controllerPath = AnimatorControllerRootPath + "/" + controllerName + "_Controller.controller";
            AssetDatabase.DeleteAsset(controllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.AddState(controllerName);
            state.motion = clip;
            state.writeDefaultValues = false;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ConfigureAnimation(GameObject instance, AnimatorController controller)
        {
            var legacyAnimation = instance.GetComponent<Animation>();
            if (legacyAnimation != null)
            {
                UnityEngine.Object.DestroyImmediate(legacyAnimation);
            }

            var animator = instance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = instance.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            animator.Rebind();
            animator.Update(0f);
        }

        private static Vector3[] BuildPlacementPositions(Bounds corridorBounds)
        {
            var positions = new Vector3[RequiredPlacedCount];
            var zOffset = Mathf.Max(5.0f, corridorBounds.size.z * 0.32f);
            var placementZ = corridorBounds.min.z - zOffset;
            var center = corridorBounds.center;
            var floorY = corridorBounds.min.y;

            for (var i = 0; i < positions.Length; i++)
            {
                var x = center.x + (i - 2.5f) * ParvumPlacementSpacing;
                positions[i] = new Vector3(x, floorY, placementZ);
            }

            return positions;
        }

        private static void PositionPlayStartInFrontOfParvum(Transform placementRoot, Bounds corridorBounds)
        {
            var parvumBounds = GetRendererBounds(placementRoot);
            var player = RequireObject(PlayerRootName);
            var playerPosition = new Vector3(
                parvumBounds.center.x,
                corridorBounds.min.y,
                GetFrontZ(parvumBounds, corridorBounds, PlayStartFrontGap, 0.6f));
            player.transform.SetPositionAndRotation(
                playerPosition,
                Quaternion.LookRotation(new Vector3(0f, 0f, parvumBounds.center.z - playerPosition.z), Vector3.up));

            var modelCamera = RequireObject(ModelCameraName);
            var cameraPosition = new Vector3(
                parvumBounds.center.x,
                corridorBounds.min.y + PlayCameraHeight,
                GetFrontZ(parvumBounds, corridorBounds, PlayCameraFrontGap, 0.25f));
            modelCamera.transform.SetPositionAndRotation(
                cameraPosition,
                Quaternion.LookRotation(parvumBounds.center - cameraPosition, Vector3.up));
        }

        private static float GetFrontZ(Bounds parvumBounds, Bounds corridorBounds, float preferredGap, float corridorMargin)
        {
            return Mathf.Min(parvumBounds.max.z + preferredGap, corridorBounds.min.z - corridorMargin);
        }

        private static void ValidatePlayStartInFrontOfParvum(Transform placementRoot, Bounds corridorBounds)
        {
            var parvumBounds = GetRendererBounds(placementRoot);
            ValidatePlayStartObject(
                PlayerRootName,
                RequireObject(PlayerRootName).transform,
                new Vector3(
                    parvumBounds.center.x,
                    corridorBounds.min.y,
                    GetFrontZ(parvumBounds, corridorBounds, PlayStartFrontGap, 0.6f)),
                parvumBounds,
                corridorBounds);
            ValidatePlayStartObject(
                ModelCameraName,
                RequireObject(ModelCameraName).transform,
                new Vector3(
                    parvumBounds.center.x,
                    corridorBounds.min.y + PlayCameraHeight,
                    GetFrontZ(parvumBounds, corridorBounds, PlayCameraFrontGap, 0.25f)),
                parvumBounds,
                corridorBounds);
        }

        private static void ValidatePlayStartObject(
            string objectName,
            Transform target,
            Vector3 expectedPosition,
            Bounds parvumBounds,
            Bounds corridorBounds)
        {
            const float PositionTolerance = 0.05f;
            var position = target.position;
            if (Mathf.Abs(position.x - expectedPosition.x) <= PositionTolerance &&
                Mathf.Abs(position.y - expectedPosition.y) <= PositionTolerance &&
                Mathf.Abs(position.z - expectedPosition.z) <= PositionTolerance &&
                position.z > parvumBounds.max.z &&
                position.z < corridorBounds.min.z)
            {
                return;
            }

            throw new InvalidOperationException(
                objectName +
                " must start directly in front of the approved Parvum placement for play inspection. Position=" +
                FormatVector(position) +
                "; Expected=" +
                FormatVector(expectedPosition) +
                "; ParvumFrontZ=" +
                parvumBounds.max.z.ToString("0.###", CultureInfo.InvariantCulture) +
                "; CorridorMinZ=" +
                corridorBounds.min.z.ToString("0.###", CultureInfo.InvariantCulture));
        }

        private static void AlignObjectBottomToY(Transform root, float targetY)
        {
            var bounds = GetRendererBounds(root);
            var offset = targetY - bounds.min.y;
            root.position += new Vector3(0f, offset, 0f);
        }

        private static bool ApproximatelyUniformScale(Vector3 scale, float expected)
        {
            const float Tolerance = 0.001f;
            return Mathf.Abs(scale.x - expected) <= Tolerance &&
                   Mathf.Abs(scale.y - expected) <= Tolerance &&
                   Mathf.Abs(scale.z - expected) <= Tolerance;
        }

        private static Vector3 GetAveragePosition(Vector3[] positions)
        {
            var total = Vector3.zero;
            for (var i = 0; i < positions.Length; i++)
            {
                total += positions[i];
            }

            return total / positions.Length;
        }

        private static Bounds GetRendererBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(root.name + " must have at least one renderer for bounds calculation.");
            }

            var hasBounds = false;
            var bounds = new Bounds(root.position, Vector3.zero);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (!hasBounds)
                {
                    bounds = renderers[i].bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            return bounds;
        }

        private static string BuildInspectionSummary(Transform placementRoot, Bounds corridorBounds)
        {
            var lines = new List<string>
            {
                "Approved Parvum enemy placement inspection:",
                "CorridorBounds=center " + FormatVector(corridorBounds.center) + ", size " + FormatVector(corridorBounds.size) + ", min " + FormatVector(corridorBounds.min) + ", max " + FormatVector(corridorBounds.max),
            };

            for (var i = 0; i < PlacementSpecs.Length; i++)
            {
                var spec = PlacementSpecs[i];
                var child = placementRoot.Find(spec.ObjectName);
                if (child == null)
                {
                    lines.Add(spec.ObjectName + ": missing");
                    continue;
                }

                var bounds = GetRendererBounds(child);
                var renderers = child.GetComponentsInChildren<Renderer>(true);
                var enabledRenderers = 0;
                for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    if (renderers[rendererIndex].enabled)
                    {
                        enabledRenderers++;
                    }
                }

                var animator = child.GetComponent<Animator>();
                var controllerName = animator == null || animator.runtimeAnimatorController == null
                    ? "Static"
                    : animator.runtimeAnimatorController.name;
                var model = child.Find(ModelChildPath);
                var modelRotation = model == null ? "Missing" : FormatVector(model.localEulerAngles);
                var groundDelta = bounds.min.y - corridorBounds.min.y;
                var zGap = corridorBounds.min.z - bounds.max.z;
                lines.Add(
                    spec.ObjectName +
                    ": position " +
                    FormatVector(child.position) +
                    ", scale " +
                    FormatVector(child.localScale) +
                    ", bounds center " +
                    FormatVector(bounds.center) +
                    ", bounds size " +
                    FormatVector(bounds.size) +
                    ", bounds min " +
                    FormatVector(bounds.min) +
                    ", bounds max " +
                    FormatVector(bounds.max) +
                    ", groundDelta " +
                    groundDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", zGap " +
                    zGap.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", renderers " +
                    enabledRenderers.ToString(CultureInfo.InvariantCulture) +
                    "/" +
                    renderers.Length.ToString(CultureInfo.InvariantCulture) +
                    ", controller " +
                    controllerName +
                    ", modelRotation " +
                    modelRotation +
                    ", animationDetail " +
                    BuildAnimationDetailSummary(animator));
            }

            var player = GameObject.Find(PlayerRootName);
            if (player != null)
            {
                lines.Add(PlayerRootName + ": playStartPosition " + FormatVector(player.transform.position));
            }

            var modelCamera = GameObject.Find(ModelCameraName);
            if (modelCamera != null)
            {
                lines.Add(ModelCameraName + ": playStartPosition " + FormatVector(modelCamera.transform.position));
            }

            return string.Join("\n", lines);
        }

        private static void ValidateAnimatedBoundsCloseToStatic(string objectName, Bounds bounds, Bounds staticBounds)
        {
            var maximumSize = staticBounds.size * MaximumAnimatedBoundsGrowthFactor;
            if (bounds.size.x <= maximumSize.x &&
                bounds.size.y <= maximumSize.y &&
                bounds.size.z <= maximumSize.z)
            {
                return;
            }

            throw new InvalidOperationException(
                objectName +
                " must keep the same visual orientation and approximate silhouette as the static Parvum. BoundsSize=" +
                FormatVector(bounds.size) +
                "; StaticBoundsSize=" +
                FormatVector(staticBounds.size) +
                "; MaximumAllowedSize=" +
                FormatVector(maximumSize));
        }

        private static void ValidateModelFacing(Transform instanceRoot, string objectName)
        {
            var model = instanceRoot.Find(ModelChildPath);
            if (model == null)
            {
                throw new InvalidOperationException(objectName + " is missing the Parvum model child.");
            }

            var angle = Quaternion.Angle(model.localRotation, Quaternion.Euler(ParvumModelFacingEuler));
            if (angle > ModelFacingRotationToleranceDegrees)
            {
                throw new InvalidOperationException(
                    objectName +
                    " must rotate the approved Parvum model so the mouth faces forward. RotationAngleDelta=" +
                    angle.ToString("0.###", CultureInfo.InvariantCulture) +
                    "; ExpectedEuler=" +
                    FormatVector(ParvumModelFacingEuler) +
                    "; ActualEuler=" +
                    FormatVector(model.localEulerAngles));
            }
        }

        private static void ValidateDetailedAnimationCurves(Animator animator, string expectedClipName)
        {
            var clip = GetPrimaryClip(animator, expectedClipName);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var paths = CountUniqueBindingPaths(bindings);
            var rotationBindings = CountRotationCurveBindings(bindings);
            if (bindings.Length < MinimumAnimatedCurveBindings || paths < MinimumAnimatedCurvePaths)
            {
                throw new InvalidOperationException(
                    expectedClipName +
                    " must include detailed part animation curves. Bindings=" +
                    bindings.Length.ToString(CultureInfo.InvariantCulture) +
                    "; Paths=" +
                    paths.ToString(CultureInfo.InvariantCulture) +
                    "; RequiredBindings=" +
                    MinimumAnimatedCurveBindings.ToString(CultureInfo.InvariantCulture) +
                    "; RequiredPaths=" +
                    MinimumAnimatedCurvePaths.ToString(CultureInfo.InvariantCulture));
            }

            if (rotationBindings > MaximumRotationCurveBindings)
            {
                throw new InvalidOperationException(
                    expectedClipName +
                    " must not include rotation curves because animated Parvum instances must keep the static sample orientation. RotationBindings=" +
                    rotationBindings.ToString(CultureInfo.InvariantCulture));
            }

            if (string.Equals(expectedClipName, "Parvum_Death", StringComparison.Ordinal))
            {
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                if (!settings.loopTime)
                {
                    throw new InvalidOperationException("Parvum_Death must loop so the death motion remains visible during play inspection.");
                }
            }
        }

        private static string BuildAnimationDetailSummary(Animator animator)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return "Static";
            }

            var clips = animator.runtimeAnimatorController.animationClips;
            if (clips == null || clips.Length == 0)
            {
                return "NoClip";
            }

            var clip = clips[0];
            var bindings = AnimationUtility.GetCurveBindings(clip);
            return clip.name +
                   "/bindings=" +
                   bindings.Length.ToString(CultureInfo.InvariantCulture) +
                   "/paths=" +
                   CountUniqueBindingPaths(bindings).ToString(CultureInfo.InvariantCulture) +
                   "/rotationBindings=" +
                   CountRotationCurveBindings(bindings).ToString(CultureInfo.InvariantCulture);
        }

        private static AnimationClip GetPrimaryClip(Animator animator, string expectedClipName)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                throw new InvalidOperationException(expectedClipName + " animator is missing.");
            }

            var clips = animator.runtimeAnimatorController.animationClips;
            if (clips == null || clips.Length == 0)
            {
                throw new InvalidOperationException(expectedClipName + " controller has no animation clips.");
            }

            for (var i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null && clips[i].name == expectedClipName)
                {
                    return clips[i];
                }
            }

            throw new InvalidOperationException(expectedClipName + " controller does not include the expected clip.");
        }

        private static int CountUniqueBindingPaths(EditorCurveBinding[] bindings)
        {
            var paths = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < bindings.Length; i++)
            {
                paths.Add(bindings[i].path);
            }

            return paths.Count;
        }

        private static int CountRotationCurveBindings(EditorCurveBinding[] bindings)
        {
            var count = 0;
            for (var i = 0; i < bindings.Length; i++)
            {
                var propertyName = bindings[i].propertyName;
                if (propertyName.IndexOf("localEulerAngles", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    propertyName.IndexOf("m_LocalRotation", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static string FormatVector(Vector3 value)
        {
            return "(" +
                   value.x.ToString("0.###", CultureInfo.InvariantCulture) +
                   ", " +
                   value.y.ToString("0.###", CultureInfo.InvariantCulture) +
                   ", " +
                   value.z.ToString("0.###", CultureInfo.InvariantCulture) +
                   ")";
        }

        private static void DisableAllColliders(Transform root)
        {
            var colliders = root.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(colliders[i]);
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
            Selection.activeGameObject = placementRoot.childCount > 0
                ? placementRoot.GetChild(0).gameObject
                : placementRoot.gameObject;
            EditorGUIUtility.PingObject(Selection.activeGameObject);

            if (!Application.isBatchMode && SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
                SceneView.lastActiveSceneView.Repaint();
            }
        }

        private static void DeleteGeneratedObject(string objectName)
        {
            var target = GameObject.Find(objectName);
            if (target != null)
            {
                UnityEngine.Object.DestroyImmediate(target);
                return;
            }

            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                {
                    UnityEngine.Object.DestroyImmediate(roots[i]);
                    return;
                }

                var child = FindChildRecursive(roots[i].transform, objectName);
                if (child != null)
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                    return;
                }
            }
        }

        private static GameObject RequireObject(string objectName)
        {
            var target = GameObject.Find(objectName);
            if (target != null)
            {
                return target;
            }

            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                {
                    return roots[i];
                }

                var child = FindChildRecursive(roots[i].transform, objectName);
                if (child != null)
                {
                    return child.gameObject;
                }
            }

            throw new InvalidOperationException("Missing required scene object: " + objectName);
        }

        private static Transform FindChildRecursive(Transform parent, string objectName)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == objectName)
                {
                    return child;
                }

                var nested = FindChildRecursive(child, objectName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
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
                return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            }
        }

        private readonly struct PartPathSet
        {
            public PartPathSet(
                MotionTarget model,
                MotionTarget body,
                MotionTarget outerSlime,
                MotionTarget leftLobe,
                MotionTarget rightLobe,
                MotionTarget rearMass,
                MotionTarget frontCradle,
                MotionTarget upperCrest,
                MotionTarget mouthRoot,
                MotionTarget snout,
                MotionTarget lipRing,
                MotionTarget tongue)
            {
                Model = model;
                Body = body;
                OuterSlime = outerSlime;
                LeftLobe = leftLobe;
                RightLobe = rightLobe;
                RearMass = rearMass;
                FrontCradle = frontCradle;
                UpperCrest = upperCrest;
                MouthRoot = mouthRoot;
                Snout = snout;
                LipRing = lipRing;
                Tongue = tongue;
            }

            public MotionTarget Model { get; }

            public MotionTarget Body { get; }

            public MotionTarget OuterSlime { get; }

            public MotionTarget LeftLobe { get; }

            public MotionTarget RightLobe { get; }

            public MotionTarget RearMass { get; }

            public MotionTarget FrontCradle { get; }

            public MotionTarget UpperCrest { get; }

            public MotionTarget MouthRoot { get; }

            public MotionTarget Snout { get; }

            public MotionTarget LipRing { get; }

            public MotionTarget Tongue { get; }
        }

        private readonly struct MotionTarget
        {
            public MotionTarget(
                string path,
                Vector3 localPosition,
                Vector3 localScale,
                Vector3 localEulerAngles)
            {
                Path = path;
                LocalPosition = localPosition;
                LocalScale = localScale;
                LocalEulerAngles = localEulerAngles;
            }

            public string Path { get; }

            public Vector3 LocalPosition { get; }

            public Vector3 LocalScale { get; }

            public Vector3 LocalEulerAngles { get; }
        }

        private readonly struct CurveKey
        {
            public CurveKey(float time, float value)
            {
                Time = time;
                Value = value;
            }

            public float Time { get; }

            public float Value { get; }
        }

        private readonly struct PlacementSpec
        {
            public PlacementSpec(string objectName, string animationClipName)
            {
                ObjectName = objectName;
                AnimationClipName = animationClipName;
            }

            public string ObjectName { get; }

            public string AnimationClipName { get; }
        }

        private readonly struct MaterialSet
        {
            public MaterialSet(
                Material slime,
                Material outerSlime,
                Material puddle,
                Material snout,
                Material darkScale,
                Material tooth,
                Material tongue,
                Material mouthDark,
                Material paleFleck)
            {
                Slime = slime;
                OuterSlime = outerSlime;
                Puddle = puddle;
                Snout = snout;
                DarkScale = darkScale;
                Tooth = tooth;
                Tongue = tongue;
                MouthDark = mouthDark;
                PaleFleck = paleFleck;
            }

            public Material Slime { get; }

            public Material OuterSlime { get; }

            public Material Puddle { get; }

            public Material Snout { get; }

            public Material DarkScale { get; }

            public Material Tooth { get; }

            public Material Tongue { get; }

            public Material MouthDark { get; }

            public Material PaleFleck { get; }
        }
    }
}
