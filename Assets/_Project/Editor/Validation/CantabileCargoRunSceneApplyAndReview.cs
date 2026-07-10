using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.CantabileCargoRunScene
{
    internal static class CantabileCargoRunSceneApplyAndReview
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string LongaArmaPlacementRootName = "Approved Longa Arma Enemy Placement";
        private const string TergoPlacementRootName = "Approved Tergo Enemy Placement";
        private const string MonstrumPlacementRootName = "Approved Monstrum Enemy Placement";
        private const string PlacementRootName = "Approved Cantabile Enemy Placement";
        private const string PlacementObjectName = "Cantabile_00_Static_Review";
        private const string ModelChildName = "CantabilePrepared_Model";
        private const string MotionRootChildName = "CantabileAnimationMotion";
        private const string LeftWingRigName = "Fuga2_Left_Wing_Root_For_Pose";
        private const string RightWingRigName = "Fuga2_Right_Wing_Root_For_Pose";
        private const string PlayerRootName = "Player";

        private const string SourceModelAbsolutePath = "D:/Bellerophon2/Bellerophon/enemies model/cantabille.glb";
        private const string CantabileArtRoot = "Assets/_Project/Art/Enemies/Cantabile";
        private const string UnityModelFolder = CantabileArtRoot + "/Models";
        private const string UnityModelAssetPath = UnityModelFolder + "/cantabille.glb";
        private const string UnityMaterialFolder = CantabileArtRoot + "/Materials";
        private const string DefaultMaterialAssetPath = UnityMaterialFolder + "/M_Cantabile_GLB_Default_URP.mat";
        private const string UnityAnimationFolder = CantabileArtRoot + "/Animations";

        private const float CantabileTargetHeightMeters = 0.50f;
        private const float CantabileFacingYawDegrees = 180f;
        private const float CantabilePlayerFrontDistance = 4.00f;
        private const float LongaTergoFallbackSpacing = 4.00f;
        private const float PlacementToleranceMeters = 0.05f;
        private const float HeightToleranceMeters = 0.08f;
        private const float AnimationReviewMinimumSlotSpacingMeters = 1.80f;
        private const float AnimationReviewSpacingMarginMeters = 0.45f;
        private const float AnimationReviewOverlapToleranceMeters = 0.05f;
        private const float IdleHoverClipDurationSeconds = 1.60f;
        private const float IdleWingFlapsPerSecond = 10.00f;
        private const float IdleWingFlapRollAmplitudeDegrees = 42.00f;
        private const float IdleWingFlapPitchAmplitudeDegrees = 14.00f;
        private const float MoveFlightClipDurationSeconds = 1.40f;
        private const float MoveWingFlapsPerSecond = 10.00f;
        private const float MoveWingFlapRollAmplitudeDegrees = 50.00f;
        private const float MoveWingFlapPitchAmplitudeDegrees = 18.00f;
        private const float AttackWingStrikeClipDurationSeconds = 1.10f;
        private const float AttackWingFlapsPerSecond = 10.00f;
        private const float AttackWingFlapRollAmplitudeDegrees = 72.00f;
        private const float AttackWingFlapPitchAmplitudeDegrees = 26.00f;
        private const float AttackBodyForwardDiagonalStrikeDegrees = 38.00f;
        private const float AttackBodySideDiagonalStrikeDegrees = 14.00f;
        private const float AttackBodyWingForwardYawDegrees = 68.00f;
        private const float DeathFallClipDurationSeconds = 2.60f;
        private const float DeathCorpseHoldStartSeconds = 1.70f;
        private const float DeathFallStartLocalYOffset = 1.40f;
        private const float DeathCorpseLocalYOffset = -0.22f;
        private const float DeathVisibleFallDistanceMeters = 1.50f;
        private const float DeathCorpsePitchDegrees = 88.00f;
        private const float DeathCorpseRollDegrees = -30.00f;
        private const float CantabileDefaultMetallic = 0.00f;
        private const float CantabileDefaultSmoothness = 0.35f;
        private static readonly Color CantabileDefaultBaseColor = Color.white;

        private static readonly AnimationReviewSpec[] AnimationReviewSpecs =
        {
            new AnimationReviewSpec("Cantabile_02_Idle_Hover", "Cantabile_02_Idle_Hover", 1, CreateIdleHoverClip),
            new AnimationReviewSpec("Cantabile_03_Move_Flight", "Cantabile_03_Move_Flight", 2, CreateMoveFlightClip),
            new AnimationReviewSpec("Cantabile_07_Wing_MeleeAttack", "Cantabile_07_Wing_MeleeAttack", 3, CreateWingMeleeAttackClip),
            new AnimationReviewSpec("Cantabile_10_Death", "Cantabile_10_Death", 4, CreateDeathClip)
        };

        private static readonly float[] RuntimeSpacingSampleFractions = { 0f, 0.25f, 0.50f, 0.75f, 0.99f };

        [MenuItem("Bellerophon/Enemies/Cantabile/Apply Prepared Model To CargoRunMvp")]
        public static void ApplyPreparedModelToCurrentCargoRunScene()
        {
            RequirePreparedModelFile();
            EnsureUnityFolders();
            CopyPreparedModelAsset();
            ConfigureImportedModelAsset();

            var modelAsset = LoadPreparedModelAsset();
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = PlacePreparedModel(modelAsset, scene);
            ConfigureInitialPlayerStart(placementRoot.transform);
            InspectSceneState(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Prepared Cantabile model applied to CargoRunMvp scene.");
        }

        public static void InspectAppliedSceneState()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectSceneState(placementRoot.transform);
            Debug.Log("Prepared Cantabile CargoRunMvp scene state inspected.");
        }

        [MenuItem("Bellerophon/Enemies/Cantabile/Apply Animation Review Objects")]
        public static void ApplyAnimationReviewObjects()
        {
            RequirePreparedModelFile();
            EnsureUnityFolders();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(UnityModelAssetPath) == null)
            {
                CopyPreparedModelAsset();
                ConfigureImportedModelAsset();
            }

            var modelAsset = LoadPreparedModelAsset();
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing. Apply the base Cantabile placement first.");
            }

            RequirePlacementObject(placementRoot.transform);
            var rigPaths = ResolveAnimationRigPaths(modelAsset);
            var clips = EnsureAnimationClips(0f, rigPaths);
            var controllers = EnsureAnimatorControllers(clips);

            foreach (var spec in AnimationReviewSpecs)
            {
                PlaceAnimationReviewObject(modelAsset, placementRoot.transform, spec, controllers[spec.ClipName]);
            }

            ArrangeReviewObjectsInSingleLine(placementRoot.transform);
            InspectSceneState(placementRoot.transform);
            InspectAnimationReviewObjects(placementRoot.transform, controllers, rigPaths);
            InspectReviewObjectSpacing(placementRoot.transform);
            InspectReviewObjectRuntimeSpacingSamples(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log(
                "Prepared Cantabile animation review objects applied. " +
                "Objects=" + string.Join(", ", Array.ConvertAll(AnimationReviewSpecs, spec => spec.ObjectName)) + ".");
        }

        private static void RequirePreparedModelFile()
        {
            if (!File.Exists(SourceModelAbsolutePath))
            {
                throw new FileNotFoundException("Prepared Cantabile GLB model is missing.", SourceModelAbsolutePath);
            }
        }

        private static void EnsureUnityFolders()
        {
            EnsureUnityFolder(CantabileArtRoot);
            EnsureUnityFolder(UnityModelFolder);
            EnsureUnityFolder(UnityMaterialFolder);
            EnsureUnityFolder(UnityAnimationFolder);
        }

        private static void CopyPreparedModelAsset()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var destinationPath = Path.GetFullPath(Path.Combine(projectRoot, UnityModelAssetPath));
            var destinationFolder = Path.GetDirectoryName(destinationPath);
            if (string.IsNullOrEmpty(destinationFolder))
            {
                throw new InvalidOperationException("Cantabile model destination folder could not be resolved.");
            }

            Directory.CreateDirectory(destinationFolder);
            File.Copy(SourceModelAbsolutePath, destinationPath, true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(UnityModelAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigureImportedModelAsset()
        {
            var importer = AssetImporter.GetAtPath(UnityModelAssetPath) as ModelImporter;
            if (importer == null)
            {
                return;
            }

            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = true;
            importer.importAnimation = false;
            importer.importVisibility = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.globalScale = 1f;
            importer.SaveAndReimport();
        }

        private static GameObject LoadPreparedModelAsset()
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(UnityModelAssetPath);
            if (modelAsset == null)
            {
                throw new InvalidOperationException($"Could not load Cantabile GLB as a Unity model asset. GLB path={UnityModelAssetPath}.");
            }

            return modelAsset;
        }

        private static GameObject PlacePreparedModel(GameObject modelAsset, Scene scene)
        {
            var longaRoot = RequireSceneRoot(LongaArmaPlacementRootName);
            var tergoRoot = RequireSceneRoot(TergoPlacementRootName);
            var monstrumRoot = RequireSceneRoot(MonstrumPlacementRootName);
            var spacing = CalculateLongaTergoSpacing(longaRoot.transform, tergoRoot.transform);
            var placementPosition = new Vector3(
                monstrumRoot.transform.position.x,
                monstrumRoot.transform.position.y,
                monstrumRoot.transform.position.z - spacing);

            var existingRoot = GameObject.Find(PlacementRootName);
            if (existingRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(existingRoot);
            }

            var placementRoot = new GameObject(PlacementRootName);
            SceneManager.MoveGameObjectToScene(placementRoot, scene);
            placementRoot.transform.position = placementPosition;
            placementRoot.transform.rotation = Quaternion.identity;
            placementRoot.transform.localScale = Vector3.one;

            var reviewRoot = new GameObject(PlacementObjectName);
            reviewRoot.transform.SetParent(placementRoot.transform, false);
            reviewRoot.transform.localPosition = Vector3.zero;
            reviewRoot.transform.localRotation = Quaternion.Euler(0f, CantabileFacingYawDegrees, 0f);
            reviewRoot.transform.localScale = Vector3.one;

            var modelInstance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            if (modelInstance == null)
            {
                modelInstance = UnityEngine.Object.Instantiate(modelAsset);
            }

            modelInstance.name = ModelChildName;
            modelInstance.transform.SetParent(reviewRoot.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            ApplyCantabileUrpMaterials(reviewRoot.transform);
            DisableImportedAnimationPlayback(reviewRoot.transform);
            RequireRenderers(reviewRoot.transform);
            ScaleToTargetHeightAndAlignToGround(reviewRoot.transform, placementRoot.transform.position.y);

            EditorUtility.SetDirty(placementRoot);
            EditorUtility.SetDirty(reviewRoot);
            return placementRoot;
        }

        private static void ConfigureInitialPlayerStart(Transform placementRoot)
        {
            var player = FindPlayerStartTransform();
            if (player == null)
            {
                throw new InvalidOperationException("Could not find Player start transform in CargoRunMvp scene.");
            }

            var focus = RequirePlacementObject(placementRoot);
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.08f, 0.04f, 0.18f);
            var frontDirection = CalculateCantabileVisualFrontDirection(focus);
            var startPosition = new Vector3(
                lookAt.x + frontDirection.x * CantabilePlayerFrontDistance,
                0f,
                lookAt.z + frontDirection.z * CantabilePlayerFrontDistance);

            player.SetPositionAndRotation(startPosition, CalculateYawRotationToward(startPosition, lookAt));
            EditorUtility.SetDirty(player);
        }

        private static void InspectSceneState(Transform placementRoot)
        {
            var longaRoot = RequireSceneRoot(LongaArmaPlacementRootName);
            var tergoRoot = RequireSceneRoot(TergoPlacementRootName);
            var monstrumRoot = RequireSceneRoot(MonstrumPlacementRootName);
            var reviewObject = RequirePlacementObject(placementRoot);
            var modelObject = reviewObject.Find(ModelChildName);
            if (modelObject == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under {PlacementObjectName}.");
            }

            RequireRenderers(reviewObject);
            InspectCantabileMaterials(reviewObject);
            InspectTargetHeight(reviewObject);
            InspectPlacementPosition(placementRoot, monstrumRoot.transform, longaRoot.transform, tergoRoot.transform);
            InspectPlayerStart(placementRoot);

            var spacing = CalculateLongaTergoSpacing(longaRoot.transform, tergoRoot.transform);
            Debug.Log(
                "CantabilePlacementInspection " +
                $"Root={PlacementRootName}, Object={PlacementObjectName}, Model={ModelChildName}, " +
                $"Source={SourceModelAbsolutePath}, UnityAsset={UnityModelAssetPath}, " +
                $"LongaZ={longaRoot.transform.position.z:0.###}, TergoZ={tergoRoot.transform.position.z:0.###}, " +
                $"LongaTergoSpacing={spacing:0.###}, MonstrumZ={monstrumRoot.transform.position.z:0.###}, " +
                $"CantabileZ={placementRoot.position.z:0.###}.");
        }

        private static void ApplyCantabileUrpMaterials(Transform root)
        {
            var materialCache = new Dictionary<string, Material>(StringComparer.Ordinal);
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var assignedSlots = 0;
            foreach (var renderer in renderers)
            {
                var sourceMaterials = renderer.sharedMaterials;
                var slotCount = Math.Max(1, sourceMaterials.Length);
                var replacementMaterials = new Material[slotCount];
                for (var i = 0; i < slotCount; i++)
                {
                    var sourceMaterial = i < sourceMaterials.Length ? sourceMaterials[i] : null;
                    var cacheKey = BuildMaterialCacheKey(sourceMaterial, i);
                    if (!materialCache.TryGetValue(cacheKey, out var replacementMaterial))
                    {
                        replacementMaterial = GetOrCreateCantabileUrpMaterial(sourceMaterial, i);
                        materialCache.Add(cacheKey, replacementMaterial);
                    }

                    replacementMaterials[i] = replacementMaterial;
                    assignedSlots++;
                }

                renderer.sharedMaterials = replacementMaterials;
                EditorUtility.SetDirty(renderer);
            }

            Debug.Log(
                $"CantabileMaterialApply Renderers={renderers.Length}, Slots={assignedSlots}, " +
                $"MaterialFolder={UnityMaterialFolder}, DefaultMaterial={DefaultMaterialAssetPath}.");
        }

        private static Material GetOrCreateCantabileUrpMaterial(Material sourceMaterial, int slotIndex)
        {
            var materialPath = BuildMaterialAssetPath(sourceMaterial, slotIndex);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(FindLitShader())
                {
                    name = Path.GetFileNameWithoutExtension(materialPath)
                };
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.shader = FindLitShader();
            ApplySourceMaterialProperties(material, sourceMaterial);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplySourceMaterialProperties(Material targetMaterial, Material sourceMaterial)
        {
            var baseColor = TryGetSourceColor(sourceMaterial, out var sourceColor)
                ? sourceColor
                : CantabileDefaultBaseColor;
            SetMaterialColor(targetMaterial, "_BaseColor", baseColor);
            SetMaterialColor(targetMaterial, "_Color", baseColor);

            var baseTexture = TryGetSourceTexture(sourceMaterial);
            SetMaterialTexture(targetMaterial, "_BaseMap", baseTexture);
            SetMaterialTexture(targetMaterial, "_MainTex", baseTexture);

            SetMaterialFloat(targetMaterial, "_Metallic", TryGetSourceFloat(sourceMaterial, CantabileDefaultMetallic, "_Metallic"));
            SetMaterialFloat(
                targetMaterial,
                "_Smoothness",
                TryGetSourceFloat(sourceMaterial, CantabileDefaultSmoothness, "_Smoothness", "_Glossiness"));
            SetMaterialFloat(targetMaterial, "_Surface", 0f);
            SetMaterialFloat(targetMaterial, "_Blend", 0f);
            SetMaterialFloat(targetMaterial, "_AlphaClip", 0f);
            SetMaterialFloat(targetMaterial, "_ZWrite", 1f);
            SetMaterialFloat(targetMaterial, "_SrcBlend", 1f);
            SetMaterialFloat(targetMaterial, "_DstBlend", 0f);
            targetMaterial.SetOverrideTag("RenderType", "Opaque");
            targetMaterial.renderQueue = -1;
            targetMaterial.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            targetMaterial.DisableKeyword("_ALPHATEST_ON");
        }

        private static string BuildMaterialCacheKey(Material sourceMaterial, int slotIndex)
        {
            if (sourceMaterial == null)
            {
                return "Default";
            }

            var assetPath = AssetDatabase.GetAssetPath(sourceMaterial);
            if (!string.IsNullOrEmpty(assetPath))
            {
                return assetPath + "#" + sourceMaterial.GetInstanceID().ToString(CultureInfo.InvariantCulture);
            }

            return sourceMaterial.name + "#" + slotIndex.ToString(CultureInfo.InvariantCulture);
        }

        private static string BuildMaterialAssetPath(Material sourceMaterial, int slotIndex)
        {
            if (sourceMaterial == null)
            {
                return DefaultMaterialAssetPath;
            }

            var sourceName = string.IsNullOrWhiteSpace(sourceMaterial.name)
                ? "Slot_" + slotIndex.ToString(CultureInfo.InvariantCulture)
                : sourceMaterial.name;
            return UnityMaterialFolder + "/M_Cantabile_GLB_" + SanitizeAssetName(sourceName) + "_URP.mat";
        }

        private static string SanitizeAssetName(string value)
        {
            var chars = value.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                {
                    chars[i] = '_';
                }
            }

            var sanitized = new string(chars).Trim('_');
            return string.IsNullOrEmpty(sanitized) ? "Default" : sanitized;
        }

        private static bool TryGetSourceColor(Material sourceMaterial, out Color color)
        {
            color = CantabileDefaultBaseColor;
            if (sourceMaterial == null)
            {
                return false;
            }

            return TryGetColor(sourceMaterial, "_BaseColor", out color) ||
                TryGetColor(sourceMaterial, "_Color", out color);
        }

        private static bool TryGetColor(Material material, string propertyName, out Color color)
        {
            color = CantabileDefaultBaseColor;
            if (material == null || !material.HasProperty(propertyName))
            {
                return false;
            }

            color = material.GetColor(propertyName);
            return true;
        }

        private static Texture TryGetSourceTexture(Material sourceMaterial)
        {
            if (sourceMaterial == null)
            {
                return null;
            }

            var texture = TryGetTexture(sourceMaterial, "_BaseMap");
            if (texture != null)
            {
                return texture;
            }

            return TryGetTexture(sourceMaterial, "_MainTex");
        }

        private static Texture TryGetTexture(Material material, string propertyName)
        {
            return material != null && material.HasProperty(propertyName) ? material.GetTexture(propertyName) : null;
        }

        private static float TryGetSourceFloat(Material sourceMaterial, float fallback, params string[] propertyNames)
        {
            if (sourceMaterial == null)
            {
                return fallback;
            }

            foreach (var propertyName in propertyNames)
            {
                if (sourceMaterial.HasProperty(propertyName))
                {
                    return sourceMaterial.GetFloat(propertyName);
                }
            }

            return fallback;
        }

        private static void SetMaterialColor(Material material, string propertyName, Color color)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, color);
            }
        }

        private static void SetMaterialTexture(Material material, string propertyName, Texture texture)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }

        private static void SetMaterialFloat(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static Shader FindLitShader()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException("Could not find a supported Lit shader for Cantabile materials.");
            }

            return shader;
        }

        private static void InspectCantabileMaterials(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var materialSlots = 0;
            var invalidMaterials = 0;
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                if (materials.Length == 0)
                {
                    invalidMaterials++;
                    continue;
                }

                foreach (var material in materials)
                {
                    materialSlots++;
                    if (!IsUsableMaterial(material))
                    {
                        invalidMaterials++;
                    }
                }
            }

            if (invalidMaterials > 0)
            {
                throw new InvalidOperationException(
                    $"Cantabile contains missing or unsupported materials. Renderers={renderers.Length}, Slots={materialSlots}, Invalid={invalidMaterials}.");
            }

            Debug.Log(
                $"CantabileMaterialInspection Renderers={renderers.Length}, Slots={materialSlots}, " +
                $"Invalid={invalidMaterials}, Shader={FindLitShader().name}.");
        }

        private static bool IsUsableMaterial(Material material)
        {
            if (material == null || material.shader == null)
            {
                return false;
            }

            return material.shader.isSupported &&
                !string.Equals(material.shader.name, "Hidden/InternalErrorShader", StringComparison.Ordinal);
        }

        private static AnimationRigPaths ResolveAnimationRigPaths(GameObject modelAsset)
        {
            var leftWing = FindChildRecursive(modelAsset.transform, LeftWingRigName);
            var rightWing = FindChildRecursive(modelAsset.transform, RightWingRigName);
            if (leftWing == null || rightWing == null)
            {
                leftWing = FindWingRigTransform(modelAsset.transform, true);
                rightWing = FindWingRigTransform(modelAsset.transform, false);
            }

            if (leftWing == null || rightWing == null)
            {
                leftWing = FindLateralBoneRigTransform(modelAsset.transform, true);
                rightWing = FindLateralBoneRigTransform(modelAsset.transform, false);
            }

            if (leftWing == null || rightWing == null)
            {
                throw new InvalidOperationException(
                    "Cantabile wing rig transforms are missing. " +
                    $"PreferredLeft={LeftWingRigName}, PreferredRight={RightWingRigName}, " +
                    "Candidates=" + BuildWingCandidateReport(modelAsset.transform) + ".");
            }

            var rigPaths = new AnimationRigPaths(
                BuildModelChildAnimationPath(modelAsset.transform, leftWing),
                BuildModelChildAnimationPath(modelAsset.transform, rightWing),
                leftWing.localEulerAngles,
                rightWing.localEulerAngles);

            Debug.Log(
                "CantabileWingRigPaths " +
                "Left=" + rigPaths.LeftWing +
                ", Right=" + rigPaths.RightWing +
                ", FlapsPerSecond=" + IdleWingFlapsPerSecond.ToString("0.##", CultureInfo.InvariantCulture) + ".");
            return rigPaths;
        }

        private static Transform FindWingRigTransform(Transform modelRoot, bool leftSide)
        {
            var sideMatches = new List<Transform>();
            var anyWingMatches = new List<Transform>();
            foreach (var child in modelRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child == modelRoot || !IsUsableWingCandidate(child.name))
                {
                    continue;
                }

                anyWingMatches.Add(child);
                if (IsWingSideMatch(child.name, leftSide))
                {
                    sideMatches.Add(child);
                }
            }

            if (sideMatches.Count > 0)
            {
                sideMatches.Sort(CompareWingCandidates);
                return sideMatches[0];
            }

            if (anyWingMatches.Count >= 2)
            {
                anyWingMatches.Sort((a, b) => a.localPosition.x.CompareTo(b.localPosition.x));
                return leftSide ? anyWingMatches[0] : anyWingMatches[anyWingMatches.Count - 1];
            }

            return null;
        }

        private static Transform FindLateralBoneRigTransform(Transform modelRoot, bool leftSide)
        {
            Transform best = null;
            var bestScore = float.NegativeInfinity;
            foreach (var child in modelRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child == modelRoot || child.name.Equals("Mesh1.0", StringComparison.Ordinal))
                {
                    continue;
                }

                var x = child.localPosition.x;
                if (leftSide ? x >= -0.05f : x <= 0.05f)
                {
                    continue;
                }

                var score = Mathf.Abs(x) * 100f + CountChildTransforms(child) * 0.5f;
                if (score > bestScore)
                {
                    best = child;
                    bestScore = score;
                }
            }

            return best;
        }

        private static int CountChildTransforms(Transform root)
        {
            var count = 0;
            foreach (Transform unused in root.GetComponentsInChildren<Transform>(true))
            {
                count++;
            }

            return count;
        }

        private static bool IsUsableWingCandidate(string objectName)
        {
            var lower = objectName.ToLowerInvariant();
            return lower.Contains("wing") &&
                !lower.Contains("motionpath") &&
                !lower.Contains("motion_path") &&
                !lower.Contains("goal") &&
                !lower.Contains("hidden");
        }

        private static bool IsWingSideMatch(string objectName, bool leftSide)
        {
            var lower = objectName.ToLowerInvariant();
            if (leftSide)
            {
                return lower.Contains("left") ||
                    lower.Contains("_l") ||
                    lower.EndsWith(".l", StringComparison.Ordinal) ||
                    lower.EndsWith("-l", StringComparison.Ordinal);
            }

            return lower.Contains("right") ||
                lower.Contains("_r") ||
                lower.EndsWith(".r", StringComparison.Ordinal) ||
                lower.EndsWith("-r", StringComparison.Ordinal);
        }

        private static int CompareWingCandidates(Transform left, Transform right)
        {
            var scoreDelta = ScoreWingCandidate(right.name).CompareTo(ScoreWingCandidate(left.name));
            if (scoreDelta != 0)
            {
                return scoreDelta;
            }

            return string.Compare(left.name, right.name, StringComparison.Ordinal);
        }

        private static int ScoreWingCandidate(string objectName)
        {
            var lower = objectName.ToLowerInvariant();
            var score = 0;
            if (lower.Contains("root"))
            {
                score += 8;
            }

            if (lower.Contains("rig") || lower.Contains("bone") || lower.Contains("joint") || lower.Contains("pose"))
            {
                score += 4;
            }

            if (lower.Contains("mesh") || lower.Contains("surface") || lower.Contains("panel"))
            {
                score -= 3;
            }

            return score;
        }

        private static string BuildWingCandidateReport(Transform modelRoot)
        {
            var candidates = new List<string>();
            foreach (var child in modelRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child == modelRoot)
                {
                    continue;
                }

                var lower = child.name.ToLowerInvariant();
                if (lower.Contains("wing") || lower.Contains("left") || lower.Contains("right"))
                {
                    candidates.Add(AnimationUtility.CalculateTransformPath(child, modelRoot));
                }
            }

            candidates.Sort(StringComparer.Ordinal);
            if (candidates.Count > 0)
            {
                return string.Join(" | ", candidates);
            }

            var hierarchy = new List<string>();
            foreach (var child in modelRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child == modelRoot)
                {
                    continue;
                }

                hierarchy.Add(
                    AnimationUtility.CalculateTransformPath(child, modelRoot) +
                    "@local=" + FormatVector(child.localPosition));
                if (hierarchy.Count >= 120)
                {
                    break;
                }
            }

            return hierarchy.Count == 0 ? "none" : "no wing-named candidates; hierarchy=" + string.Join(" | ", hierarchy);
        }

        private static string BuildModelChildAnimationPath(Transform modelRoot, Transform target)
        {
            var relativePath = AnimationUtility.CalculateTransformPath(target, modelRoot);
            if (string.IsNullOrEmpty(relativePath))
            {
                throw new InvalidOperationException($"{target.name} cannot be the model root for a wing rig animation path.");
            }

            return ModelChildName + "/" + relativePath;
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(child.name, childName, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static Dictionary<string, AnimationClip> EnsureAnimationClips(
            float baseLocalY,
            AnimationRigPaths rigPaths)
        {
            var clips = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);
            foreach (var spec in AnimationReviewSpecs)
            {
                var clipPath = BuildAnimationClipPath(spec.ClipName);
                AssetDatabase.DeleteAsset(clipPath);
                var clip = spec.CreateClip(baseLocalY, rigPaths);
                clip.name = spec.ClipName;
                clip.frameRate = 30f;
                clip.wrapMode = WrapMode.Loop;

                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                settings.loopBlend = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                AssetDatabase.CreateAsset(clip, clipPath);
                clips.Add(spec.ClipName, AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath));
            }

            AssetDatabase.SaveAssets();
            return clips;
        }

        private static Dictionary<string, AnimatorController> EnsureAnimatorControllers(
            Dictionary<string, AnimationClip> clips)
        {
            var controllers = new Dictionary<string, AnimatorController>(StringComparer.Ordinal);
            foreach (var spec in AnimationReviewSpecs)
            {
                var controllerPath = BuildAnimatorControllerPath(spec.ClipName);
                AssetDatabase.DeleteAsset(controllerPath);

                var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                var state = controller.layers[0].stateMachine.AddState(spec.ClipName);
                state.motion = clips[spec.ClipName];
                state.writeDefaultValues = true;
                controller.layers[0].stateMachine.defaultState = state;
                EditorUtility.SetDirty(controller);
                controllers.Add(spec.ClipName, controller);
            }

            AssetDatabase.SaveAssets();
            return controllers;
        }

        private static void PlaceAnimationReviewObject(
            GameObject modelAsset,
            Transform placementRoot,
            AnimationReviewSpec spec,
            AnimatorController controller)
        {
            var existing = placementRoot.Find(spec.ObjectName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            var reviewRoot = new GameObject(spec.ObjectName);
            reviewRoot.transform.SetParent(placementRoot, false);
            reviewRoot.transform.localPosition = new Vector3(
                spec.SlotIndex * AnimationReviewMinimumSlotSpacingMeters,
                0f,
                0f);
            reviewRoot.transform.localRotation = Quaternion.Euler(0f, CantabileFacingYawDegrees, 0f);
            reviewRoot.transform.localScale = Vector3.one;

            var motionRoot = new GameObject(MotionRootChildName);
            motionRoot.transform.SetParent(reviewRoot.transform, false);
            motionRoot.transform.localPosition = Vector3.zero;
            motionRoot.transform.localRotation = Quaternion.identity;
            motionRoot.transform.localScale = Vector3.one;

            var modelInstance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            if (modelInstance == null)
            {
                modelInstance = UnityEngine.Object.Instantiate(modelAsset);
            }

            modelInstance.name = ModelChildName;
            modelInstance.transform.SetParent(motionRoot.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            ApplyCantabileUrpMaterials(reviewRoot.transform);
            DisableImportedAnimationPlayback(reviewRoot.transform);
            RequireRenderers(reviewRoot.transform);
            ScaleToTargetHeightAndAlignToGround(reviewRoot.transform, placementRoot.position.y);
            ConfigureLoopingAnimator(motionRoot, controller);

            EditorUtility.SetDirty(reviewRoot);
        }

        private static void ArrangeReviewObjectsInSingleLine(Transform placementRoot)
        {
            var orderedObjects = GetReviewObjectsInOrder(placementRoot);
            var spacing = CalculateReviewSlotSpacing(orderedObjects);

            for (var i = 0; i < orderedObjects.Count; i++)
            {
                var reviewObject = orderedObjects[i];
                var current = reviewObject.localPosition;
                reviewObject.localPosition = new Vector3(i * spacing, current.y, 0f);
                reviewObject.localRotation = Quaternion.Euler(0f, CantabileFacingYawDegrees, 0f);
                EditorUtility.SetDirty(reviewObject);
            }

            Debug.Log(
                "CantabileReviewObjectArrangement Count=" + orderedObjects.Count.ToString(CultureInfo.InvariantCulture) +
                ", Spacing=" + spacing.ToString("0.###", CultureInfo.InvariantCulture) +
                ", Order=" + string.Join(", ", orderedObjects.ConvertAll(item => item.name)) + ".");
        }

        private static float CalculateReviewSlotSpacing(List<Transform> orderedObjects)
        {
            var maxWidth = 0f;
            foreach (var reviewObject in orderedObjects)
            {
                var bounds = CalculateRendererBounds(reviewObject, new Bounds(reviewObject.position, Vector3.one));
                maxWidth = Mathf.Max(maxWidth, bounds.size.x);
            }

            return Mathf.Max(AnimationReviewMinimumSlotSpacingMeters, maxWidth + AnimationReviewSpacingMarginMeters);
        }

        private static List<Transform> GetReviewObjectsInOrder(Transform placementRoot)
        {
            var orderedObjects = new List<Transform>();
            var staticObject = placementRoot.Find(PlacementObjectName);
            if (staticObject == null)
            {
                throw new InvalidOperationException($"{PlacementObjectName} is missing under {PlacementRootName}.");
            }

            orderedObjects.Add(staticObject);
            foreach (var spec in AnimationReviewSpecs)
            {
                var reviewObject = placementRoot.Find(spec.ObjectName);
                if (reviewObject == null)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} is missing under {PlacementRootName}.");
                }

                orderedObjects.Add(reviewObject);
            }

            return orderedObjects;
        }

        private static void ConfigureLoopingAnimator(GameObject reviewRoot, AnimatorController controller)
        {
            var animator = reviewRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = reviewRoot.AddComponent<Animator>();
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.runtimeAnimatorController = controller;
            EditorUtility.SetDirty(animator);
        }

        private static void InspectAnimationReviewObjects(
            Transform placementRoot,
            Dictionary<string, AnimatorController> controllers,
            AnimationRigPaths rigPaths)
        {
            var objectNames = new List<string>();
            foreach (var spec in AnimationReviewSpecs)
            {
                var reviewObject = placementRoot.Find(spec.ObjectName);
                if (reviewObject == null)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} is missing under {PlacementRootName}.");
                }

                var slotAnimator = reviewObject.GetComponent<Animator>();
                if (slotAnimator != null && slotAnimator.enabled)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} slot root must stay free of enabled Animator components.");
                }

                var motionRoot = RequireAnimationMotionRoot(reviewObject);
                var animator = motionRoot.GetComponent<Animator>();
                if (animator == null || !animator.enabled || animator.runtimeAnimatorController == null)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} motion root must have an enabled Animator with a controller.");
                }

                if (animator.runtimeAnimatorController != controllers[spec.ClipName])
                {
                    throw new InvalidOperationException($"{spec.ObjectName} uses an unexpected AnimatorController.");
                }

                var clip = GetControllerClip(animator, spec.ClipName);
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                if (!settings.loopTime)
                {
                    throw new InvalidOperationException($"{spec.ClipName} must loop for review playback.");
                }

                if (AnimationUtility.GetCurveBindings(clip).Length == 0)
                {
                    throw new InvalidOperationException($"{spec.ClipName} must contain authored animation curves.");
                }

                if (string.Equals(spec.ClipName, "Cantabile_02_Idle_Hover", StringComparison.Ordinal))
                {
                    InspectIdleWingRigCurves(clip, rigPaths);
                }
                else if (string.Equals(spec.ClipName, "Cantabile_03_Move_Flight", StringComparison.Ordinal))
                {
                    InspectMoveFlightRigCurves(clip, rigPaths);
                }
                else if (string.Equals(spec.ClipName, "Cantabile_07_Wing_MeleeAttack", StringComparison.Ordinal))
                {
                    InspectWingMeleeAttackRigCurves(clip, rigPaths);
                }
                else if (string.Equals(spec.ClipName, "Cantabile_10_Death", StringComparison.Ordinal))
                {
                    InspectDeathFallCorpseHoldCurves(clip, rigPaths);
                }

                RequireRenderers(reviewObject);
                InspectCantabileMaterials(reviewObject);
                objectNames.Add(spec.ObjectName + "=" + spec.ClipName);
            }

            Debug.Log(
                "CantabileAnimationReviewInspection Count=" + AnimationReviewSpecs.Length.ToString(CultureInfo.InvariantCulture) +
                ", " + string.Join(", ", objectNames) + ".");
        }

        private static void InspectReviewObjectSpacing(Transform placementRoot)
        {
            var orderedObjects = GetReviewObjectsInOrder(placementRoot);
            var previousMaxX = float.NegativeInfinity;
            var report = new List<string>();

            foreach (var reviewObject in orderedObjects)
            {
                var bounds = CalculateRendererBounds(reviewObject, new Bounds(reviewObject.position, Vector3.one));
                if (bounds.min.x < previousMaxX - AnimationReviewOverlapToleranceMeters)
                {
                    throw new InvalidOperationException(
                        $"Cantabile review objects overlap on X axis near {reviewObject.name}. PreviousMaxX={previousMaxX:0.###}, CurrentMinX={bounds.min.x:0.###}.");
                }

                previousMaxX = bounds.max.x;
                report.Add(reviewObject.name + "@LocalX=" + reviewObject.localPosition.x.ToString("0.###", CultureInfo.InvariantCulture));
            }

            Debug.Log("CantabileReviewObjectSpacingInspection " + string.Join(", ", report) + ".");
        }

        private static void InspectIdleWingRigCurves(AnimationClip clip, AnimationRigPaths rigPaths)
        {
            RequireTransformCurve(clip, rigPaths.LeftWing, "localEulerAnglesRaw.z");
            RequireTransformCurve(clip, rigPaths.RightWing, "localEulerAnglesRaw.z");
            RequireTransformCurve(clip, rigPaths.LeftWing, "localEulerAnglesRaw.x");
            RequireTransformCurve(clip, rigPaths.RightWing, "localEulerAnglesRaw.x");

            Debug.Log(
                "CantabileIdleWingRigInspection Clip=" + clip.name +
                ", LeftWing=" + rigPaths.LeftWing +
                ", RightWing=" + rigPaths.RightWing +
                ", FlapsPerSecond=" + IdleWingFlapsPerSecond.ToString("0.##", CultureInfo.InvariantCulture) + ".");
        }

        private static void InspectMoveFlightRigCurves(AnimationClip clip, AnimationRigPaths rigPaths)
        {
            RequireTransformCurve(clip, string.Empty, "localEulerAnglesRaw.x");
            RequireTransformCurve(clip, rigPaths.LeftWing, "localEulerAnglesRaw.z");
            RequireTransformCurve(clip, rigPaths.RightWing, "localEulerAnglesRaw.z");
            RequireTransformCurve(clip, rigPaths.LeftWing, "localEulerAnglesRaw.x");
            RequireTransformCurve(clip, rigPaths.RightWing, "localEulerAnglesRaw.x");

            var bodyLeanCurve = AnimationUtility.GetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.x"));
            if (bodyLeanCurve == null || bodyLeanCurve.length == 0 || bodyLeanCurve.keys[0].value < 10f)
            {
                throw new InvalidOperationException($"{clip.name} must lean the body forward during flight.");
            }

            Debug.Log(
                "CantabileMoveWingRigInspection Clip=" + clip.name +
                ", LeftWing=" + rigPaths.LeftWing +
                ", RightWing=" + rigPaths.RightWing +
                ", BodyForwardLeanDegrees=" + bodyLeanCurve.keys[0].value.ToString("0.##", CultureInfo.InvariantCulture) +
                ", FlapsPerSecond=" + MoveWingFlapsPerSecond.ToString("0.##", CultureInfo.InvariantCulture) + ".");
        }

        private static void InspectWingMeleeAttackRigCurves(AnimationClip clip, AnimationRigPaths rigPaths)
        {
            RequireTransformCurve(clip, string.Empty, "localEulerAnglesRaw.x");
            RequireTransformCurve(clip, string.Empty, "localEulerAnglesRaw.y");
            RequireTransformCurve(clip, string.Empty, "localEulerAnglesRaw.z");
            RequireTransformCurve(clip, rigPaths.LeftWing, "localEulerAnglesRaw.z");
            RequireTransformCurve(clip, rigPaths.RightWing, "localEulerAnglesRaw.z");
            RequireTransformCurve(clip, rigPaths.LeftWing, "localEulerAnglesRaw.x");
            RequireTransformCurve(clip, rigPaths.RightWing, "localEulerAnglesRaw.x");

            var forwardStrikeCurve = AnimationUtility.GetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.x"));
            var wingForwardYawCurve = AnimationUtility.GetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.y"));
            var sideStrikeCurve = AnimationUtility.GetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.z"));
            var maxForwardStrike = 0f;
            var maxWingForwardYaw = 0f;
            var maxSideStrike = 0f;
            if (forwardStrikeCurve != null)
            {
                foreach (var key in forwardStrikeCurve.keys)
                {
                    maxForwardStrike = Mathf.Max(maxForwardStrike, Mathf.Abs(key.value));
                }
            }

            if (wingForwardYawCurve != null)
            {
                foreach (var key in wingForwardYawCurve.keys)
                {
                    maxWingForwardYaw = Mathf.Max(maxWingForwardYaw, Mathf.Abs(key.value));
                }
            }

            if (sideStrikeCurve != null)
            {
                foreach (var key in sideStrikeCurve.keys)
                {
                    maxSideStrike = Mathf.Max(maxSideStrike, Mathf.Abs(key.value));
                }
            }

            if (maxForwardStrike < AttackBodyForwardDiagonalStrikeDegrees - 1f)
            {
                throw new InvalidOperationException($"{clip.name} must lean forward for a visible diagonal wing melee strike.");
            }

            if (maxSideStrike > AttackBodySideDiagonalStrikeDegrees + 1f || maxForwardStrike < maxSideStrike * 2f)
            {
                throw new InvalidOperationException($"{clip.name} must use forward-diagonal lean instead of a side-tilt strike.");
            }

            if (maxWingForwardYaw < AttackBodyWingForwardYawDegrees - 1f)
            {
                throw new InvalidOperationException($"{clip.name} must rotate the body so a wing faces the forward target during the strike.");
            }

            Debug.Log(
                "CantabileAttackForwardWingRotationInspection Clip=" + clip.name +
                ", LeftWing=" + rigPaths.LeftWing +
                ", RightWing=" + rigPaths.RightWing +
                ", BodyForwardStrikeDegrees=" + maxForwardStrike.ToString("0.##", CultureInfo.InvariantCulture) +
                ", BodyWingForwardYawDegrees=" + maxWingForwardYaw.ToString("0.##", CultureInfo.InvariantCulture) +
                ", BodyDiagonalSideDegrees=" + maxSideStrike.ToString("0.##", CultureInfo.InvariantCulture) +
                ", FlapsPerSecond=" + AttackWingFlapsPerSecond.ToString("0.##", CultureInfo.InvariantCulture) + ".");
        }

        private static void InspectDeathFallCorpseHoldCurves(AnimationClip clip, AnimationRigPaths rigPaths)
        {
            RequireAnyTransformCurve(clip, string.Empty, "localPosition.y", "m_LocalPosition.y");
            RequireTransformCurve(clip, string.Empty, "localEulerAnglesRaw.x");
            RequireTransformCurve(clip, string.Empty, "localEulerAnglesRaw.z");

            RequireMissingTransformCurve(clip, ModelChildName, "localScale.x");
            RequireMissingTransformCurve(clip, ModelChildName, "localScale.y");
            RequireMissingTransformCurve(clip, ModelChildName, "localScale.z");
            RequireMissingTransformCurve(clip, rigPaths.LeftWing, "localEulerAnglesRaw.z");
            RequireMissingTransformCurve(clip, rigPaths.RightWing, "localEulerAnglesRaw.z");
            RequireMissingTransformCurve(clip, rigPaths.LeftWing, "localEulerAnglesRaw.x");
            RequireMissingTransformCurve(clip, rigPaths.RightWing, "localEulerAnglesRaw.x");

            var positionCurve = GetAnyTransformCurve(clip, string.Empty, "localPosition.y", "m_LocalPosition.y");
            var pitchCurve = AnimationUtility.GetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.x"));
            if (positionCurve == null || positionCurve.length == 0 || pitchCurve == null || pitchCurve.length == 0)
            {
                throw new InvalidOperationException($"{clip.name} must contain death fall position and corpse pose rotation curves.");
            }

            var finalYOffset = positionCurve.keys[positionCurve.length - 1].value - positionCurve.keys[0].value;
            var visibleFallDistance = Mathf.Abs(finalYOffset);
            var finalPitch = pitchCurve.keys[pitchCurve.length - 1].value;
            if (visibleFallDistance < DeathVisibleFallDistanceMeters || finalPitch < DeathCorpsePitchDegrees - 1f)
            {
                throw new InvalidOperationException($"{clip.name} must visibly fall from the air and hold the corpse pose.");
            }

            Debug.Log(
                "CantabileDeathFallCorpseHoldInspection Clip=" + clip.name +
                ", FinalYOffset=" + finalYOffset.ToString("0.###", CultureInfo.InvariantCulture) +
                ", VisibleFallDistance=" + visibleFallDistance.ToString("0.###", CultureInfo.InvariantCulture) +
                ", FinalPitchDegrees=" + finalPitch.ToString("0.##", CultureInfo.InvariantCulture) +
                ", ScaleMeltCurves=0, WingFlapCurves=0.");
        }

        private static void RequireTransformCurve(AnimationClip clip, string path, string propertyName)
        {
            var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName);
            if (AnimationUtility.GetEditorCurve(clip, binding) == null)
            {
                throw new InvalidOperationException(
                    $"{clip.name} is missing required wing rig curve. Path={path}, Property={propertyName}.");
            }
        }

        private static void RequireAnyTransformCurve(AnimationClip clip, string path, params string[] propertyNames)
        {
            if (GetAnyTransformCurve(clip, path, propertyNames) == null)
            {
                throw new InvalidOperationException(
                    $"{clip.name} is missing required transform curve. Path={path}, Properties={string.Join("/", propertyNames)}.");
            }
        }

        private static AnimationCurve GetAnyTransformCurve(AnimationClip clip, string path, params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                var curve = AnimationUtility.GetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName));
                if (curve != null)
                {
                    return curve;
                }
            }

            return null;
        }

        private static void RequireMissingTransformCurve(AnimationClip clip, string path, string propertyName)
        {
            var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName);
            if (AnimationUtility.GetEditorCurve(clip, binding) != null)
            {
                throw new InvalidOperationException(
                    $"{clip.name} must not contain this curve. Path={path}, Property={propertyName}.");
            }
        }

        private static void InspectReviewObjectRuntimeSpacingSamples(Transform placementRoot)
        {
            var orderedObjects = GetReviewObjectsInOrder(placementRoot);
            var sampleTargets = new List<RuntimeAnimationSampleTarget>();
            foreach (var spec in AnimationReviewSpecs)
            {
                var reviewObject = placementRoot.Find(spec.ObjectName);
                if (reviewObject == null)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} is missing under {PlacementRootName}.");
                }

                var motionRoot = RequireAnimationMotionRoot(reviewObject);
                var animator = motionRoot.GetComponent<Animator>();
                if (animator == null || animator.runtimeAnimatorController == null)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} motion root has no AnimatorController for runtime spacing inspection.");
                }

                sampleTargets.Add(new RuntimeAnimationSampleTarget(
                    spec.ObjectName,
                    motionRoot,
                    GetControllerClip(animator, spec.ClipName)));
            }

            var snapshots = CaptureTransformSnapshots(orderedObjects);
            try
            {
                foreach (var fraction in RuntimeSpacingSampleFractions)
                {
                    foreach (var target in sampleTargets)
                    {
                        var sampleTime = Mathf.Clamp(
                            target.Clip.length * fraction,
                            0f,
                            Mathf.Max(0f, target.Clip.length - 0.001f));
                        target.Clip.SampleAnimation(target.MotionRoot.gameObject, sampleTime);
                    }

                    InspectReviewObjectSpacingAtRuntimeSample(orderedObjects, fraction);
                }
            }
            finally
            {
                RestoreTransformSnapshots(snapshots);
            }

            Debug.Log(
                "CantabileRuntimeSpacingInspection Samples=" +
                RuntimeSpacingSampleFractions.Length.ToString(CultureInfo.InvariantCulture) +
                ", Objects=" + orderedObjects.Count.ToString(CultureInfo.InvariantCulture) +
                ", AnimatedObjects=" + sampleTargets.Count.ToString(CultureInfo.InvariantCulture) + ".");
        }

        private static void InspectReviewObjectSpacingAtRuntimeSample(
            List<Transform> orderedObjects,
            float sampleFraction)
        {
            var previousMaxX = float.NegativeInfinity;
            foreach (var reviewObject in orderedObjects)
            {
                var bounds = CalculateRendererBounds(reviewObject, new Bounds(reviewObject.position, Vector3.one));
                if (bounds.min.x < previousMaxX - AnimationReviewOverlapToleranceMeters)
                {
                    throw new InvalidOperationException(
                        $"Cantabile review objects overlap while animation is sampled. Sample={sampleFraction:0.##}, Object={reviewObject.name}, PreviousMaxX={previousMaxX:0.###}, CurrentMinX={bounds.min.x:0.###}.");
                }

                previousMaxX = bounds.max.x;
            }
        }

        private static Transform RequireAnimationMotionRoot(Transform reviewObject)
        {
            var motionRoot = reviewObject.Find(MotionRootChildName);
            if (motionRoot == null)
            {
                throw new InvalidOperationException($"{reviewObject.name} is missing {MotionRootChildName}.");
            }

            return motionRoot;
        }

        private static List<TransformSnapshot> CaptureTransformSnapshots(List<Transform> roots)
        {
            var snapshots = new List<TransformSnapshot>();
            foreach (var root in roots)
            {
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                {
                    snapshots.Add(new TransformSnapshot(child));
                }
            }

            return snapshots;
        }

        private static void RestoreTransformSnapshots(List<TransformSnapshot> snapshots)
        {
            foreach (var snapshot in snapshots)
            {
                snapshot.Restore();
            }
        }

        private static AnimationClip GetControllerClip(Animator animator, string expectedClipName)
        {
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip != null && string.Equals(clip.name, expectedClipName, StringComparison.Ordinal))
                {
                    return clip;
                }
            }

            throw new InvalidOperationException($"{animator.gameObject.name} controller does not contain {expectedClipName}.");
        }

        private static AnimationClip CreateIdleHoverClip(float baseLocalY, AnimationRigPaths rigPaths)
        {
            var clip = new AnimationClip();
            SetTransformCurve(clip, string.Empty, "localPosition.y",
                Key(0.00f, baseLocalY),
                Key(0.40f, baseLocalY + 0.055f),
                Key(0.80f, baseLocalY),
                Key(1.20f, baseLocalY - 0.030f),
                Key(1.60f, baseLocalY));
            SetTransformCurve(clip, string.Empty, "localEulerAnglesRaw.z",
                Key(0.00f, 0f),
                Key(0.40f, 2.5f),
                Key(0.80f, 0f),
                Key(1.20f, -2.5f),
                Key(1.60f, 0f));
            AddIdleMothWingFlap(clip, rigPaths);
            return clip;
        }

        private static AnimationClip CreateMoveFlightClip(float baseLocalY, AnimationRigPaths rigPaths)
        {
            var clip = new AnimationClip();
            SetTransformCurve(clip, string.Empty, "localPosition.y",
                Key(0.00f, baseLocalY + 0.015f),
                Key(0.35f, baseLocalY + 0.090f),
                Key(0.70f, baseLocalY + 0.020f),
                Key(1.05f, baseLocalY + 0.070f),
                Key(1.40f, baseLocalY + 0.015f));
            SetTransformCurve(clip, string.Empty, "localPosition.z",
                Key(0.00f, 0f),
                Key(0.35f, -0.22f),
                Key(0.70f, 0f),
                Key(1.05f, 0.22f),
                Key(1.40f, 0f));
            SetTransformCurve(clip, string.Empty, "localEulerAnglesRaw.x",
                Key(0.00f, 14f),
                Key(0.35f, 21f),
                Key(0.70f, 16f),
                Key(1.05f, 22f),
                Key(1.40f, 14f));
            AddMoveMothWingFlap(clip, rigPaths);
            return clip;
        }

        private static AnimationClip CreateWingMeleeAttackClip(float baseLocalY, AnimationRigPaths rigPaths)
        {
            var clip = new AnimationClip();
            SetTransformCurve(clip, string.Empty, "localPosition.y",
                Key(0.00f, baseLocalY),
                Key(0.18f, baseLocalY + 0.040f),
                Key(0.42f, baseLocalY + 0.010f),
                Key(0.68f, baseLocalY + 0.030f),
                Key(AttackWingStrikeClipDurationSeconds, baseLocalY));
            SetTransformCurve(clip, string.Empty, "localPosition.z",
                Key(0.00f, 0f),
                Key(0.18f, -0.10f),
                Key(0.42f, -0.48f),
                Key(0.60f, -0.28f),
                Key(AttackWingStrikeClipDurationSeconds, 0f));
            SetTransformCurve(clip, string.Empty, "localEulerAnglesRaw.x",
                Key(0.00f, 0f),
                Key(0.18f, 16f),
                Key(0.42f, AttackBodyForwardDiagonalStrikeDegrees),
                Key(0.60f, 28f),
                Key(0.82f, 12f),
                Key(AttackWingStrikeClipDurationSeconds, 0f));
            SetTransformCurve(clip, string.Empty, "localEulerAnglesRaw.y",
                Key(0.00f, 0f),
                Key(0.18f, 28f),
                Key(0.42f, AttackBodyWingForwardYawDegrees),
                Key(0.60f, 48f),
                Key(0.82f, 18f),
                Key(AttackWingStrikeClipDurationSeconds, 0f));
            SetTransformCurve(clip, string.Empty, "localEulerAnglesRaw.z",
                Key(0.00f, 0f),
                Key(0.18f, 6f),
                Key(0.42f, -AttackBodySideDiagonalStrikeDegrees),
                Key(0.60f, -10f),
                Key(0.82f, 4f),
                Key(AttackWingStrikeClipDurationSeconds, 0f));
            AddAttackMothWingStrike(clip, rigPaths);
            return clip;
        }

        private static AnimationClip CreateDeathClip(float baseLocalY, AnimationRigPaths rigPaths)
        {
            var clip = new AnimationClip();
            SetTransformCurve(clip, string.Empty, "localPosition.y",
                Key(0.00f, baseLocalY + DeathFallStartLocalYOffset),
                Key(0.35f, baseLocalY + DeathFallStartLocalYOffset),
                Key(0.70f, baseLocalY + 0.950f),
                Key(1.10f, baseLocalY + 0.350f),
                Key(DeathCorpseHoldStartSeconds, baseLocalY + DeathCorpseLocalYOffset),
                Key(DeathFallClipDurationSeconds, baseLocalY + DeathCorpseLocalYOffset));
            SetTransformCurve(clip, string.Empty, "localEulerAnglesRaw.x",
                Key(0.00f, 0f),
                Key(0.35f, -8f),
                Key(0.80f, 28f),
                Key(1.20f, 64f),
                Key(DeathCorpseHoldStartSeconds, DeathCorpsePitchDegrees),
                Key(DeathFallClipDurationSeconds, DeathCorpsePitchDegrees));
            SetTransformCurve(clip, string.Empty, "localEulerAnglesRaw.z",
                Key(0.00f, 0f),
                Key(0.35f, 6f),
                Key(0.80f, -14f),
                Key(1.20f, DeathCorpseRollDegrees),
                Key(DeathCorpseHoldStartSeconds, DeathCorpseRollDegrees),
                Key(DeathFallClipDurationSeconds, DeathCorpseRollDegrees));
            return clip;
        }

        private static void AddIdleMothWingFlap(AnimationClip clip, AnimationRigPaths rigPaths)
        {
            SetTransformCurve(
                clip,
                rigPaths.LeftWing,
                "localEulerAnglesRaw.z",
                BuildSineKeys(
                    IdleHoverClipDurationSeconds,
                    IdleWingFlapsPerSecond,
                    rigPaths.LeftWingRestEuler.z,
                    IdleWingFlapRollAmplitudeDegrees,
                    1f,
                    0f));
            SetTransformCurve(
                clip,
                rigPaths.RightWing,
                "localEulerAnglesRaw.z",
                BuildSineKeys(
                    IdleHoverClipDurationSeconds,
                    IdleWingFlapsPerSecond,
                    rigPaths.RightWingRestEuler.z,
                    IdleWingFlapRollAmplitudeDegrees,
                    -1f,
                    0f));
            SetTransformCurve(
                clip,
                rigPaths.LeftWing,
                "localEulerAnglesRaw.x",
                BuildSineKeys(
                    IdleHoverClipDurationSeconds,
                    IdleWingFlapsPerSecond,
                    rigPaths.LeftWingRestEuler.x,
                    IdleWingFlapPitchAmplitudeDegrees,
                    1f,
                    Mathf.PI * 0.5f));
            SetTransformCurve(
                clip,
                rigPaths.RightWing,
                "localEulerAnglesRaw.x",
                BuildSineKeys(
                    IdleHoverClipDurationSeconds,
                    IdleWingFlapsPerSecond,
                    rigPaths.RightWingRestEuler.x,
                    IdleWingFlapPitchAmplitudeDegrees,
                    1f,
                    Mathf.PI * 0.5f));
        }

        private static void AddMoveMothWingFlap(AnimationClip clip, AnimationRigPaths rigPaths)
        {
            SetTransformCurve(
                clip,
                rigPaths.LeftWing,
                "localEulerAnglesRaw.z",
                BuildSineKeys(
                    MoveFlightClipDurationSeconds,
                    MoveWingFlapsPerSecond,
                    rigPaths.LeftWingRestEuler.z,
                    MoveWingFlapRollAmplitudeDegrees,
                    1f,
                    0f));
            SetTransformCurve(
                clip,
                rigPaths.RightWing,
                "localEulerAnglesRaw.z",
                BuildSineKeys(
                    MoveFlightClipDurationSeconds,
                    MoveWingFlapsPerSecond,
                    rigPaths.RightWingRestEuler.z,
                    MoveWingFlapRollAmplitudeDegrees,
                    -1f,
                    0f));
            SetTransformCurve(
                clip,
                rigPaths.LeftWing,
                "localEulerAnglesRaw.x",
                BuildSineKeys(
                    MoveFlightClipDurationSeconds,
                    MoveWingFlapsPerSecond,
                    rigPaths.LeftWingRestEuler.x,
                    MoveWingFlapPitchAmplitudeDegrees,
                    1f,
                    Mathf.PI * 0.5f));
            SetTransformCurve(
                clip,
                rigPaths.RightWing,
                "localEulerAnglesRaw.x",
                BuildSineKeys(
                    MoveFlightClipDurationSeconds,
                    MoveWingFlapsPerSecond,
                    rigPaths.RightWingRestEuler.x,
                    MoveWingFlapPitchAmplitudeDegrees,
                    1f,
                    Mathf.PI * 0.5f));
        }

        private static void AddAttackMothWingStrike(AnimationClip clip, AnimationRigPaths rigPaths)
        {
            SetTransformCurve(
                clip,
                rigPaths.LeftWing,
                "localEulerAnglesRaw.z",
                BuildSineKeys(
                    AttackWingStrikeClipDurationSeconds,
                    AttackWingFlapsPerSecond,
                    rigPaths.LeftWingRestEuler.z,
                    AttackWingFlapRollAmplitudeDegrees,
                    1f,
                    0f));
            SetTransformCurve(
                clip,
                rigPaths.RightWing,
                "localEulerAnglesRaw.z",
                BuildSineKeys(
                    AttackWingStrikeClipDurationSeconds,
                    AttackWingFlapsPerSecond,
                    rigPaths.RightWingRestEuler.z,
                    AttackWingFlapRollAmplitudeDegrees,
                    -1f,
                    0f));
            SetTransformCurve(
                clip,
                rigPaths.LeftWing,
                "localEulerAnglesRaw.x",
                BuildSineKeys(
                    AttackWingStrikeClipDurationSeconds,
                    AttackWingFlapsPerSecond,
                    rigPaths.LeftWingRestEuler.x,
                    AttackWingFlapPitchAmplitudeDegrees,
                    1f,
                    Mathf.PI * 0.5f));
            SetTransformCurve(
                clip,
                rigPaths.RightWing,
                "localEulerAnglesRaw.x",
                BuildSineKeys(
                    AttackWingStrikeClipDurationSeconds,
                    AttackWingFlapsPerSecond,
                    rigPaths.RightWingRestEuler.x,
                    AttackWingFlapPitchAmplitudeDegrees,
                    1f,
                    Mathf.PI * 0.5f));
        }

        private static Keyframe[] BuildSineKeys(
            float duration,
            float cyclesPerSecond,
            float baseValue,
            float amplitude,
            float directionSign,
            float phaseOffset)
        {
            var sampleCount = Mathf.CeilToInt(duration * cyclesPerSecond * 4f);
            var keys = new Keyframe[sampleCount + 1];
            for (var i = 0; i <= sampleCount; i++)
            {
                var time = duration * i / sampleCount;
                var phase = (time * cyclesPerSecond * Mathf.PI * 2f) + phaseOffset;
                keys[i] = Key(time, baseValue + Mathf.Sin(phase) * amplitude * directionSign);
            }

            return keys;
        }

        private static void SetTransformCurve(
            AnimationClip clip,
            string path,
            string propertyName,
            params Keyframe[] keys)
        {
            clip.SetCurve(path, typeof(Transform), propertyName, new AnimationCurve(keys));
        }

        private static Keyframe Key(float time, float value)
        {
            return new Keyframe(time, value);
        }

        private static string BuildAnimationClipPath(string clipName)
        {
            return UnityAnimationFolder + "/" + clipName + ".anim";
        }

        private static string BuildAnimatorControllerPath(string clipName)
        {
            return UnityAnimationFolder + "/" + clipName + ".controller";
        }

        private static void InspectTargetHeight(Transform reviewObject)
        {
            var bounds = CalculateRendererBounds(reviewObject, new Bounds(reviewObject.position, Vector3.one));
            var delta = Mathf.Abs(bounds.size.y - CantabileTargetHeightMeters);
            if (delta > HeightToleranceMeters)
            {
                throw new InvalidOperationException(
                    $"Cantabile height must stay near {CantabileTargetHeightMeters:0.###}m. Height={bounds.size.y:0.###}, Delta={delta:0.###}.");
            }
        }

        private static void InspectPlacementPosition(
            Transform placementRoot,
            Transform monstrumRoot,
            Transform longaRoot,
            Transform tergoRoot)
        {
            var spacing = CalculateLongaTergoSpacing(longaRoot, tergoRoot);
            var expectedPosition = new Vector3(
                monstrumRoot.position.x,
                monstrumRoot.position.y,
                monstrumRoot.position.z - spacing);
            var delta = Vector3.Distance(placementRoot.position, expectedPosition);
            if (delta > PlacementToleranceMeters)
            {
                throw new InvalidOperationException(
                    $"Cantabile placement must use Monstrum minus Longa/Tergo Z spacing. Expected={FormatVector(expectedPosition)}, Actual={FormatVector(placementRoot.position)}, Delta={delta:0.###}.");
            }
        }

        private static void InspectPlayerStart(Transform placementRoot)
        {
            var player = FindPlayerStartTransform();
            if (player == null)
            {
                throw new InvalidOperationException("Player start transform is missing.");
            }

            var focus = RequirePlacementObject(placementRoot);
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.08f, 0.04f, 0.18f);
            var frontDirection = CalculateCantabileVisualFrontDirection(focus);
            var playerFromFocus = player.position - lookAt;
            playerFromFocus.y = 0f;
            if (playerFromFocus.sqrMagnitude < 0.001f || Vector3.Dot(playerFromFocus.normalized, frontDirection) < 0.94f)
            {
                throw new InvalidOperationException("Player start is not placed in front of Cantabile.");
            }

            var toFocus = lookAt - player.position;
            toFocus.y = 0f;
            var playerForward = player.forward;
            playerForward.y = 0f;
            if (toFocus.sqrMagnitude < 0.001f || playerForward.sqrMagnitude < 0.001f ||
                Vector3.Dot(playerForward.normalized, toFocus.normalized) < 0.94f)
            {
                throw new InvalidOperationException("Player start is not facing Cantabile.");
            }
        }

        private static Transform RequirePlacementObject(Transform placementRoot)
        {
            var reviewObject = placementRoot.Find(PlacementObjectName);
            if (reviewObject == null)
            {
                throw new InvalidOperationException($"{PlacementObjectName} is missing under {PlacementRootName}.");
            }

            return reviewObject;
        }

        private static void DisableImportedAnimationPlayback(Transform root)
        {
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
                animator.runtimeAnimatorController = null;
                EditorUtility.SetDirty(animator);
            }

            foreach (var animation in root.GetComponentsInChildren<Animation>(true))
            {
                animation.enabled = false;
                EditorUtility.SetDirty(animation);
            }
        }

        private static int RequireRenderers(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException($"{root.name} contains no renderers.");
            }

            return renderers.Length;
        }

        private static void ScaleToTargetHeightAndAlignToGround(Transform root, float groundY)
        {
            var bounds = CalculateRendererBounds(root, new Bounds(root.position, Vector3.one));
            if (bounds.size.y > 0.0001f)
            {
                var scaleFactor = Mathf.Clamp(CantabileTargetHeightMeters / bounds.size.y, 0.001f, 100f);
                root.localScale = Vector3.one * scaleFactor;
            }

            bounds = CalculateRendererBounds(root, new Bounds(root.position, Vector3.one));
            root.position += Vector3.up * (groundY - bounds.min.y);
        }

        private static GameObject RequireSceneRoot(string objectName)
        {
            var root = GameObject.Find(objectName);
            if (root == null)
            {
                throw new InvalidOperationException($"{objectName} is missing in CargoRunMvp scene.");
            }

            return root;
        }

        private static float CalculateLongaTergoSpacing(Transform longaRoot, Transform tergoRoot)
        {
            var zSpacing = Mathf.Abs(longaRoot.position.z - tergoRoot.position.z);
            if (zSpacing > 0.10f)
            {
                return zSpacing;
            }

            return Mathf.Max(Vector3.Distance(longaRoot.position, tergoRoot.position), LongaTergoFallbackSpacing);
        }

        private static Bounds CalculateRendererBounds(Transform root, Bounds fallback)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return fallback;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static Vector3 CalculateCantabileVisualFrontDirection(Transform focus)
        {
            var frontDirection = Quaternion.Euler(0f, focus.eulerAngles.y, 0f) * Vector3.forward;
            frontDirection.y = 0f;
            return frontDirection.sqrMagnitude > 0.001f ? frontDirection.normalized : Vector3.back;
        }

        private static Quaternion CalculateYawRotationToward(Vector3 position, Vector3 target)
        {
            var facing = target - position;
            facing.y = 0f;
            return facing.sqrMagnitude > 0.001f ? Quaternion.LookRotation(facing.normalized, Vector3.up) : Quaternion.identity;
        }

        private static Transform FindPlayerStartTransform()
        {
            var player = GameObject.Find(PlayerRootName);
            if (player != null)
            {
                return player.transform;
            }

            var characterController = UnityEngine.Object.FindFirstObjectByType<CharacterController>();
            return characterController != null ? characterController.transform : null;
        }

        private static void EnsureUnityFolder(string folderPath)
        {
            var parts = folderPath.Split('/');
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

        private static string FormatVector(Vector3 value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0:0.###}, {1:0.###}, {2:0.###})",
                value.x,
                value.y,
                value.z);
        }

        private sealed class RuntimeAnimationSampleTarget
        {
            public RuntimeAnimationSampleTarget(string objectName, Transform motionRoot, AnimationClip clip)
            {
                ObjectName = objectName;
                MotionRoot = motionRoot;
                Clip = clip;
            }

            public string ObjectName { get; }
            public Transform MotionRoot { get; }
            public AnimationClip Clip { get; }
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

            public void Restore()
            {
                target.localPosition = localPosition;
                target.localRotation = localRotation;
                target.localScale = localScale;
                EditorUtility.SetDirty(target);
            }
        }

        private sealed class AnimationRigPaths
        {
            public AnimationRigPaths(
                string leftWing,
                string rightWing,
                Vector3 leftWingRestEuler,
                Vector3 rightWingRestEuler)
            {
                LeftWing = leftWing;
                RightWing = rightWing;
                LeftWingRestEuler = leftWingRestEuler;
                RightWingRestEuler = rightWingRestEuler;
            }

            public string LeftWing { get; }
            public string RightWing { get; }
            public Vector3 LeftWingRestEuler { get; }
            public Vector3 RightWingRestEuler { get; }
        }

        private sealed class AnimationReviewSpec
        {
            public AnimationReviewSpec(
                string objectName,
                string clipName,
                int slotIndex,
                Func<float, AnimationRigPaths, AnimationClip> createClip)
            {
                ObjectName = objectName;
                ClipName = clipName;
                SlotIndex = slotIndex;
                CreateClip = createClip;
            }

            public string ObjectName { get; }
            public string ClipName { get; }
            public int SlotIndex { get; }
            public Func<float, AnimationRigPaths, AnimationClip> CreateClip { get; }
        }
    }
}
