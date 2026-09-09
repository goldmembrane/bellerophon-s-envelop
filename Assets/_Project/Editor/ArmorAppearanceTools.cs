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

namespace Bellerophon.Editor.Validation
{
    internal static class ArmorAppearanceTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlayerModelPath = "Assets/_Project/Art/Player/player.fbx";
        private const string HelmetModelPath = "Assets/_Project/Art/Items/Armor/Helmet/helmet.fbx";
        private const string SourceTexturePath = "Assets/_Project/Art/Player/Textures/texture_0.png";
        private const string RuntimeTextureFolder = "Assets/_Project/Art/Items/Armor/Textures";
        private const string RuntimeMaterialFolder = "Assets/_Project/Art/Items/Armor/Materials";
        private const string ValidationFolder = "docs/validation/armor_appearance_2026-09-08";
        private const string LeftUpperArmValidationFolder =
            "docs/validation/armor_left_upper_arm_cleanup_2026-09-08";
        private const string PlayerMaterialPath = "Assets/_Project/Art/Player/Materials/Material_1.mat";
        private const string HelmetChildName = "ArmorHelmet_Attached";
        private const int RenderWidth = 1024;
        private const int RenderHeight = 768;
        private const int PreviewLayer = 30;
        private const float HelmetHeightToBodyRatio = 0.235f;

        private static readonly ArmorVariant[] Variants =
        {
            new ArmorVariant("Armor_Protective_Idle", "protective clothing.png", "armor_protective_sample.png"),
            new ArmorVariant("Armor_Insulated_Idle", "electric insulating suit.png", "armor_insulated_sample.png"),
            new ArmorVariant("Armor_Fireproof_Idle", "Flame Resistant Clothing.png", "armor_fireproof_sample.png"),
            new ArmorVariant("Armor_Physical_Idle", "physical protective clothing.png", "armor_physical_sample.png")
        };

        private static readonly string[] TargetNames =
        {
            "Armor_Protective_Idle",
            "Armor_Insulated_Idle",
            "Armor_Fireproof_Idle",
            "Armor_Physical_Idle",
            "Armor_Head_Idle"
        };

        private static readonly string[] LeftUpperArmTexturePaths =
        {
            SourceTexturePath,
            RuntimeTextureFolder + "/Armor_Protective_Idle_BaseColor.png",
            RuntimeTextureFolder + "/Armor_Insulated_Idle_BaseColor.png",
            RuntimeTextureFolder + "/Armor_Fireproof_Idle_BaseColor.png",
            RuntimeTextureFolder + "/Armor_Physical_Idle_BaseColor.png",
            RuntimeTextureFolder + "/Armor_Head_Idle_BaseColor.png"
        };

        // These palettes are measured from the supplied reference images and compensated against
        // the same Unity preview lighting used by the direct visual comparison.
        private static readonly Dictionary<string, Palette> ReferenceMatchedPalettes =
            new Dictionary<string, Palette>(StringComparer.Ordinal)
            {
                { "Armor_Protective_Idle", new Palette(0.705328f, 0.250000f, 0.559215f) },
                { "Armor_Insulated_Idle", new Palette(0.122001f, 0.410000f, 0.750000f) }
            };

        internal static void InspectArmorAppearanceSources()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must already be the active scene for read-only Armor inspection. Active=" +
                    (scene.IsValid() ? scene.path : "<invalid>"));
            }

            var report = new StringBuilder();
            report.AppendLine("Armor appearance source inspection");
            report.AppendLine("scene=" + scene.path);
            report.AppendLine("sceneDirtyBefore=" + scene.isDirty);
            report.AppendLine("playerModel=" + PlayerModelPath);
            report.AppendLine("helmetModel=" + HelmetModelPath);
            report.AppendLine("helmetSha256=" + Sha256(Absolute(HelmetModelPath)));
            report.AppendLine();

            foreach (string targetName in TargetNames)
            {
                GameObject target = RequireSceneObject(scene, targetName);
                report.AppendLine("[" + targetName + "]");
                report.AppendLine("position=" + Format(target.transform.position));
                report.AppendLine("rotation=" + Format(target.transform.rotation.eulerAngles));
                report.AppendLine("scale=" + Format(target.transform.lossyScale));
                AppendRenderers(report, target);
                Transform head = FindHead(target);
                report.AppendLine("headPath=" + (head == null ? "<missing>" : HierarchyPath(head, target.transform)));
                if (head != null)
                {
                    report.AppendLine("headPosition=" + Format(head.position));
                    report.AppendLine("headRotation=" + Format(head.rotation.eulerAngles));
                }
                report.AppendLine();
            }

            GameObject playerModel = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerModelPath);
            if (playerModel == null)
                throw new InvalidOperationException("Player model could not be loaded: " + PlayerModelPath);
            report.AppendLine("[player model asset]");
            AppendRenderers(report, playerModel);
            report.AppendLine();

            GameObject helmetModel = AssetDatabase.LoadAssetAtPath<GameObject>(HelmetModelPath);
            if (helmetModel == null)
                throw new InvalidOperationException("Helmet model could not be loaded: " + HelmetModelPath);
            report.AppendLine("[helmet model asset]");
            AppendRenderers(report, helmetModel);
            AppendHierarchy(report, helmetModel.transform, helmetModel.transform, 0);
            report.AppendLine("helmetSubAssets=" +
                              string.Join(",", AssetDatabase.LoadAllAssetsAtPath(HelmetModelPath)
                                  .Select(item => item.GetType().Name + ":" + item.name)));

            Scene previewScene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject previewHelmet = PrefabUtility.InstantiatePrefab(helmetModel, previewScene) as GameObject;
                if (previewHelmet == null)
                    throw new InvalidOperationException("Helmet could not be instantiated in a preview scene.");
                Renderer[] previewRenderers = previewHelmet.GetComponentsInChildren<Renderer>(true);
                if (previewRenderers.Length == 0)
                    throw new InvalidOperationException("Preview Helmet has no renderer.");
                Bounds previewBounds = previewRenderers[0].bounds;
                for (int index = 1; index < previewRenderers.Length; index++)
                    previewBounds.Encapsulate(previewRenderers[index].bounds);
                report.AppendLine("helmetPreviewBoundsCenter=" + Format(previewBounds.center));
                report.AppendLine("helmetPreviewBoundsSize=" + Format(previewBounds.size));
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(previewScene);
            }

            report.AppendLine();
            report.AppendLine("sceneDirtyAfter=" + scene.isDirty);
            Directory.CreateDirectory(Absolute(ValidationFolder));
            File.WriteAllText(Absolute(ValidationFolder + "/source_inspection.txt"), report.ToString(),
                new UTF8Encoding(false));
            Debug.Log("Armor appearance sources inspected without saving or changing CargoRunMvp.");
        }

        internal static void InspectArmorLeftUpperArmCleanup()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be the active scene.");
            GameObject target = RequireSceneObject(scene, "Armor_Protective_Idle");
            SkinnedMeshRenderer renderer = target.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
            Mesh mesh = renderer.sharedMesh;
            if (mesh == null || mesh.uv == null || mesh.uv.Length != mesh.vertexCount)
                throw new InvalidOperationException("Armor renderer mesh UV data is unavailable.");

            Texture2D source = LoadImage(Absolute(SourceTexturePath), false);
            try
            {
                List<PixelComponent> components = FindPurpleMarkerComponents(source);
                bool[] cleanupMask = BuildLeftUpperArmCleanupPlan(source, renderer, components,
                    out List<CleanupRegion> cleanupRegions);
                var report = new StringBuilder();
                report.AppendLine("Left upper arm marker UV inspection");
                report.AppendLine("scene=" + scene.path);
                report.AppendLine("sceneDirty=" + scene.isDirty);
                report.AppendLine("renderer=" + HierarchyPath(renderer.transform, target.transform));
                report.AppendLine("mesh=" + mesh.name);
                report.AppendLine("vertexCount=" + mesh.vertexCount);
                report.AppendLine("texture=" + SourceTexturePath);
                report.AppendLine("textureSize=" + source.width + "x" + source.height);
                report.AppendLine("purpleComponentCount=" + components.Count);
                report.AppendLine();
                report.AppendLine("Arm bones:");
                for (int index = 0; index < renderer.bones.Length; index++)
                {
                    Transform bone = renderer.bones[index];
                    if (bone != null && bone.name.IndexOf("arm", StringComparison.OrdinalIgnoreCase) >= 0)
                        report.AppendLine("  [" + index + "] " + bone.name);
                }

                Vector2[] uvs = mesh.uv;
                int[] triangles = mesh.triangles;
                BoneWeight[] weights = mesh.boneWeights;
                report.AppendLine();
                report.AppendLine("Purple components and UV triangle bones:");
                foreach (PixelComponent component in components)
                {
                    Vector2 uv = new Vector2((component.CenterX + 0.5f) / source.width,
                        (component.CenterY + 0.5f) / source.height);
                    var boneMatches = new HashSet<string>(StringComparer.Ordinal);
                    for (int triangle = 0; triangle < triangles.Length; triangle += 3)
                    {
                        int a = triangles[triangle];
                        int b = triangles[triangle + 1];
                        int c = triangles[triangle + 2];
                        if (!PointInUvTriangle(uv, uvs[a], uvs[b], uvs[c])) continue;
                        boneMatches.Add(DominantBoneName(weights[a], renderer.bones));
                        boneMatches.Add(DominantBoneName(weights[b], renderer.bones));
                        boneMatches.Add(DominantBoneName(weights[c], renderer.bones));
                    }
                    report.AppendLine("  pixels=" + component.PixelCount +
                                      " bounds=" + component.MinX + "," + component.MinY + "-" +
                                      component.MaxX + "," + component.MaxY +
                                      " centerUv=" + Format(uv) +
                                      " bones=" + string.Join(",", boneMatches.OrderBy(item => item)));
                }
                Directory.CreateDirectory(Absolute(LeftUpperArmValidationFolder));
                report.AppendLine();
                report.AppendLine("Cleanup regions:");
                foreach (CleanupRegion region in cleanupRegions)
                    report.AppendLine("  bounds=" + region.MinX + "," + region.MinY + "-" +
                                      region.MaxX + "," + region.MaxY +
                                      " cloneOffset=" + region.CloneOffsetX + "," + region.CloneOffsetY +
                                      " flagRedPixels=" + region.FlagRedPixelCount);
                report.AppendLine("cleanupMaskPixels=" + cleanupMask.Count(value => value));
                File.WriteAllText(Absolute(LeftUpperArmValidationFolder + "/uv_inspection.txt"),
                    report.ToString(), new UTF8Encoding(false));
                Texture2D maskPreview = BuildCleanupMaskPreview(source, cleanupMask);
                try
                {
                    File.WriteAllBytes(Absolute(LeftUpperArmValidationFolder + "/uv_cleanup_mask.png"),
                        maskPreview.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(maskPreview);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
            Debug.Log("Left upper arm marker UV inspection completed without modifying assets.");
        }

        internal static void ApplyArmorLeftUpperArmCleanup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Armor texture cleanup must be applied in Edit Mode.");
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be the active scene.");
            bool sceneDirtyBefore = scene.isDirty;
            string sceneHashBefore = Sha256(Absolute(ScenePath));
            string[] protectedMaterialPaths =
            {
                PlayerMaterialPath,
                RuntimeMaterialFolder + "/Armor_Protective_Idle.mat",
                RuntimeMaterialFolder + "/Armor_Insulated_Idle.mat",
                RuntimeMaterialFolder + "/Armor_Fireproof_Idle.mat",
                RuntimeMaterialFolder + "/Armor_Physical_Idle.mat",
                RuntimeMaterialFolder + "/Armor_Head_Idle.mat"
            };
            Dictionary<string, string> materialHashesBefore = protectedMaterialPaths.ToDictionary(
                path => path, path => Sha256(Absolute(path)), StringComparer.Ordinal);

            GameObject target = RequireSceneObject(scene, "Armor_Protective_Idle");
            SkinnedMeshRenderer renderer = target.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
            Texture2D sourceBefore = LoadImage(Absolute(SourceTexturePath), false);
            var transient = new List<UnityEngine.Object> { sourceBefore };
            var report = new StringBuilder();
            report.AppendLine("Left upper arm purple marker and United States flag cleanup");
            report.AppendLine("method=nearest exact surrounding clothing pixel clone within confirmed LeftArm UV regions");
            report.AppendLine("meshOrUvChanged=False");
            report.AppendLine("materialChanged=False");
            try
            {
                List<PixelComponent> components = FindPurpleMarkerComponents(sourceBefore);
                bool[] cleanupMask = BuildLeftUpperArmCleanupPlan(sourceBefore, renderer, components,
                    out List<CleanupRegion> regions);
                Color32[] donorValidationPixels = sourceBefore.GetPixels32();
                report.AppendLine("cleanupRegionCount=" + regions.Count);
                report.AppendLine("cleanupMaskPixels=" + cleanupMask.Count(value => value));
                foreach (CleanupRegion region in regions)
                    report.AppendLine("region=" + region.MinX + "," + region.MinY + "-" +
                                      region.MaxX + "," + region.MaxY);

                foreach (string texturePath in LeftUpperArmTexturePaths)
                {
                    string beforeHash = Sha256(Absolute(texturePath));
                    Texture2D texture = LoadImage(Absolute(texturePath), false);
                    transient.Add(texture);
                    if (texture.width != sourceBefore.width || texture.height != sourceBefore.height)
                        throw new InvalidOperationException("Texture dimensions differ: " + texturePath);
                    Texture2D cleaned = CloneCleanupRegions(texture, donorValidationPixels, cleanupMask,
                        regions, out int clonedPixels);
                    transient.Add(cleaned);
                    File.WriteAllBytes(Absolute(texturePath), cleaned.EncodeToPNG());
                    AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
                    string afterHash = Sha256(Absolute(texturePath));
                    if (beforeHash == afterHash)
                        throw new InvalidOperationException("Texture did not change: " + texturePath);
                    report.AppendLine("texture=" + texturePath +
                                      " beforeSha256=" + beforeHash +
                                      " afterSha256=" + afterHash +
                                      " clonedPixels=" + clonedPixels);
                }

                foreach (KeyValuePair<string, string> material in materialHashesBefore)
                    if (Sha256(Absolute(material.Key)) != material.Value)
                        throw new InvalidOperationException("Material changed outside the texture-only scope: " +
                                                            material.Key);
                if (Sha256(Absolute(ScenePath)) != sceneHashBefore || scene.isDirty != sceneDirtyBefore)
                    throw new InvalidOperationException("CargoRunMvp scene changed during texture cleanup.");
                report.AppendLine("materialsUnchanged=True");
                report.AppendLine("sceneUnchanged=True");
                Directory.CreateDirectory(Absolute(LeftUpperArmValidationFolder));
                File.WriteAllText(Absolute(LeftUpperArmValidationFolder + "/application.txt"),
                    report.ToString(), new UTF8Encoding(false));
            }
            finally
            {
                foreach (UnityEngine.Object item in transient)
                    if (item != null) UnityEngine.Object.DestroyImmediate(item);
            }
            Debug.Log("Left upper arm purple marker and United States flag removed from all transporter textures.");
        }

        internal static void ValidateArmorLeftUpperArmCleanup()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be the active scene.");
            GameObject protective = RequireSceneObject(scene, "Armor_Protective_Idle");
            SkinnedMeshRenderer protectiveRenderer = protective
                .GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
            Texture2D source = LoadImage(Absolute(SourceTexturePath), false);
            var transient = new List<UnityEngine.Object> { source };
            try
            {
                bool[] mask = BuildLeftUpperArmCleanupPlan(source, protectiveRenderer,
                    FindPurpleMarkerComponents(source), out List<CleanupRegion> regions);
                var report = new StringBuilder();
                report.AppendLine("Left upper arm cleanup final inspection");
                report.AppendLine("scene=" + scene.path);
                report.AppendLine("sceneDirty=" + scene.isDirty);
                report.AppendLine("cleanupRegionCount=" + regions.Count);
                report.AppendLine("cleanupMaskPixels=" + mask.Count(value => value));
                string[] targetNames =
                {
                    "Armor_Protective_Idle", "Armor_Insulated_Idle", "Armor_Fireproof_Idle",
                    "Armor_Physical_Idle", "Armor_Head_Idle"
                };
                report.AppendLine("baseTexture=" + SourceTexturePath +
                                  " remainingPurpleMarkerPixels=" + CountPurplePixelsInMask(source, mask));
                for (int index = 0; index < targetNames.Length; index++)
                {
                    string targetName = targetNames[index];
                    string texturePath = LeftUpperArmTexturePaths[index + 1];
                    GameObject target = RequireSceneObject(scene, targetName);
                    SkinnedMeshRenderer renderer = target.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
                    Texture assigned = renderer.sharedMaterial.GetTexture("_BaseMap");
                    string assignedPath = AssetDatabase.GetAssetPath(assigned);
                    if (assignedPath != texturePath)
                        throw new InvalidOperationException(targetName + " texture connection differs: " + assignedPath);
                    Texture2D texture = LoadImage(Absolute(texturePath), false);
                    transient.Add(texture);
                    report.AppendLine("target=" + targetName +
                                      " material=" + AssetDatabase.GetAssetPath(renderer.sharedMaterial) +
                                      " texture=" + texturePath +
                                      " textureSha256=" + Sha256(Absolute(texturePath)));
                    foreach (CleanupRegion region in regions)
                    {
                        Color32 sample = texture.GetPixel(region.MinX + (region.MaxX - region.MinX) / 2,
                            region.MinY + (region.MaxY - region.MinY) / 2);
                        report.AppendLine("  regionCenterRgb=" + sample.r + "," + sample.g + "," + sample.b);
                    }
                }
                Directory.CreateDirectory(Absolute(LeftUpperArmValidationFolder));
                File.WriteAllText(Absolute(LeftUpperArmValidationFolder + "/inspection.txt"),
                    report.ToString(), new UTF8Encoding(false));
            }
            finally
            {
                foreach (UnityEngine.Object item in transient)
                    if (item != null) UnityEngine.Object.DestroyImmediate(item);
            }
            Debug.Log("Left upper arm cleanup final inspection completed.");
        }

        internal static void ApplyArmorFireproofPhysicalColors()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Armor colors must be applied in Edit Mode.");
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be the active scene.");

            string[] unchangedPaths =
            {
                RuntimeTextureFolder + "/Armor_Protective_Idle_BaseColor.png",
                RuntimeTextureFolder + "/Armor_Insulated_Idle_BaseColor.png",
                RuntimeTextureFolder + "/Armor_Head_Idle_BaseColor.png",
                HelmetModelPath
            };
            Dictionary<string, string> unchangedHashes = unchangedPaths.ToDictionary(
                path => path, path => Sha256(Absolute(path)), StringComparer.Ordinal);
            Texture2D source = LoadImage(Absolute(SourceTexturePath), false);
            var transient = new List<UnityEngine.Object> { source };
            var report = new StringBuilder();
            report.AppendLine("Fireproof and Physical Armor exact reference RGB transfer.");
            report.AppendLine("method=direct ranked reference RGB pixels; no average, ratio, damping, or generated palette");
            report.AppendLine("materialLighting=URP Unlit direct RGB texture output");
            report.AppendLine("meshUvOrMaterialChanged=False");
            try
            {
                foreach (ArmorVariant variant in Variants.Where(item =>
                             item.TargetName == "Armor_Fireproof_Idle" ||
                             item.TargetName == "Armor_Physical_Idle"))
                {
                    string texturePath = RuntimeTextureFolder + "/" + variant.TargetName + "_BaseColor.png";
                    Texture2D reference = LoadImage(ReferencePath(variant.ReferenceFileName), false);
                    transient.Add(reference);
                    Texture2D transferred = TransferDirectReferenceColors(
                        source, reference, variant.TargetName, out ColorTransferMetrics metrics);
                    transient.Add(transferred);
                    File.WriteAllBytes(Absolute(texturePath), transferred.EncodeToPNG());
                    ImportColorTexture(texturePath);

                    GameObject target = RequireSceneObject(scene, variant.TargetName);
                    SkinnedMeshRenderer renderer = target.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
                    string expectedMaterial = RuntimeMaterialFolder + "/" + variant.TargetName + ".mat";
                    if (AssetDatabase.GetAssetPath(renderer.sharedMaterial) != expectedMaterial)
                        throw new InvalidOperationException(variant.TargetName + " material connection changed.");
                    Texture2D imported = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                    // The supplied reference RGB values must reach the preview without scene-light multiplication.
                    Shader directRgbShader = Shader.Find("Universal Render Pipeline/Unlit");
                    if (directRgbShader == null)
                        throw new InvalidOperationException("URP Unlit shader was not found.");
                    renderer.sharedMaterial.shader = directRgbShader;
                    ApplyBaseTexture(renderer.sharedMaterial, imported);
                    if (renderer.sharedMaterial.HasProperty("_BaseColor"))
                        renderer.sharedMaterial.SetColor("_BaseColor", Color.white);
                    if (renderer.sharedMaterial.HasProperty("_Color"))
                        renderer.sharedMaterial.SetColor("_Color", Color.white);
                    if (renderer.sharedMaterial.HasProperty("_EmissionMap"))
                        renderer.sharedMaterial.SetTexture("_EmissionMap", null);
                    if (renderer.sharedMaterial.HasProperty("_EmissionColor"))
                        renderer.sharedMaterial.SetColor("_EmissionColor", Color.black);
                    renderer.sharedMaterial.DisableKeyword("_EMISSION");
                    renderer.sharedMaterial.globalIlluminationFlags =
                        MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                    AssetDatabase.SaveAssetIfDirty(renderer.sharedMaterial);
                    report.AppendLine();
                    report.AppendLine("[" + variant.TargetName + "]");
                    report.AppendLine("reference=" + ReferencePath(variant.ReferenceFileName).Replace('\\', '/'));
                    report.AppendLine("referenceSha256=" + Sha256(ReferencePath(variant.ReferenceFileName)));
                    report.AppendLine("texture=" + texturePath);
                    report.AppendLine("textureSha256=" + Sha256(Absolute(texturePath)));
                    report.AppendLine("sourceClothingPixels=" + metrics.SourcePixelCount);
                    report.AppendLine("targetReferencePixels=" + metrics.ReferencePixelCount);
                    report.AppendLine("shader=Universal Render Pipeline/Unlit");
                    report.AppendLine("emissionEnabled=False");
                    report.AppendLine("baseBrightness=1.00");
                }
                foreach (KeyValuePair<string, string> unchanged in unchangedHashes)
                    if (Sha256(Absolute(unchanged.Key)) != unchanged.Value)
                        throw new InvalidOperationException("Out-of-scope Armor asset changed: " + unchanged.Key);
                report.AppendLine();
                report.AppendLine("protectiveInsulatedHeadUnchanged=True");
                report.AppendLine("sceneDirty=" + scene.isDirty);
                AssetDatabase.SaveAssets();
                Directory.CreateDirectory(Absolute(ValidationFolder));
                File.WriteAllText(Absolute(ValidationFolder + "/fireproof_physical_application.txt"),
                    report.ToString(), new UTF8Encoding(false));
            }
            finally
            {
                foreach (UnityEngine.Object item in transient)
                    if (item != null) UnityEngine.Object.DestroyImmediate(item);
            }
            Debug.Log("Fireproof and Physical Armor colors transferred from direct reference RGB pixels.");
        }

        internal static void InspectArmorFireproofPhysicalColors()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be the active scene.");
            var report = new StringBuilder();
            foreach (string targetName in new[] { "Armor_Fireproof_Idle", "Armor_Physical_Idle" })
            {
                GameObject target = RequireSceneObject(scene, targetName);
                SkinnedMeshRenderer renderer = target.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
                string materialPath = AssetDatabase.GetAssetPath(renderer.sharedMaterial);
                string texturePath = AssetDatabase.GetAssetPath(renderer.sharedMaterial.GetTexture("_BaseMap"));
                string expectedMaterial = RuntimeMaterialFolder + "/" + targetName + ".mat";
                string expectedTexture = RuntimeTextureFolder + "/" + targetName + "_BaseColor.png";
                if (materialPath != expectedMaterial || texturePath != expectedTexture)
                    throw new InvalidOperationException(targetName + " direct material or texture connection differs.");
                report.AppendLine(targetName + " material=" + materialPath +
                                  " texture=" + texturePath +
                                  " textureSha256=" + Sha256(Absolute(texturePath)));
                string renderPath = ValidationFolder + "/render_exact_" + targetName + ".png";
                ArmorVariant variant = Variants.Single(item => item.TargetName == targetName);
                Texture2D render = LoadImage(Absolute(renderPath), false);
                Texture2D reference = LoadImage(ReferencePath(variant.ReferenceFileName), false);
                try
                {
                    report.AppendLine("  renderQuantiles=" + FormatExactQuantiles(render, targetName));
                    report.AppendLine("  referenceQuantiles=" + FormatExactQuantiles(reference, targetName));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(render);
                    UnityEngine.Object.DestroyImmediate(reference);
                }
            }
            report.AppendLine("sceneDirty=" + scene.isDirty);
            File.WriteAllText(Absolute(ValidationFolder + "/fireproof_physical_inspection.txt"),
                report.ToString(), new UTF8Encoding(false));
            Debug.Log("Fireproof and Physical Armor direct connections inspected.");
        }

        internal static void CaptureArmorFireproofPhysicalColors(string outputPath)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be the active scene.");
            if (string.IsNullOrWhiteSpace(outputPath))
                outputPath = ValidationFolder + "/fireproof_physical_comparison.png";
            Directory.CreateDirectory(Absolute(ValidationFolder));
            ArmorVariant[] variants = Variants.Where(item =>
                item.TargetName == "Armor_Fireproof_Idle" || item.TargetName == "Armor_Physical_Idle").ToArray();
            string[] resultPaths = new string[variants.Length];
            for (int index = 0; index < variants.Length; index++)
            {
                resultPaths[index] = Absolute(ValidationFolder + "/render_exact_" +
                                              variants[index].TargetName + ".png");
                RenderAppliedSceneTarget(RequireSceneObject(scene, variants[index].TargetName), resultPaths[index],
                    true);
            }
            ComposeComparison(variants.Select(item => ReferencePath(item.ReferenceFileName)).ToArray(),
                resultPaths, Absolute(outputPath));
            File.WriteAllText(Absolute(ValidationFolder + "/fireproof_physical_capture.txt"),
                "left=provided reference\nright=actual CargoRunMvp scene object\n" +
                "colorTransfer=direct reference RGB pixels without average, ratio, damping, or emission\n" +
                "previewEncoding=linear render converted to sRGB PNG\n" +
                "comparisonSha256=" + Sha256(Absolute(outputPath)) + "\n",
                new UTF8Encoding(false));
            Debug.Log("Fireproof and Physical Armor direct comparison captured.");
        }

        internal static void CaptureArmorLeftUpperArmCleanup(string outputPath)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be the active scene.");
            if (string.IsNullOrWhiteSpace(outputPath))
                outputPath = LeftUpperArmValidationFolder + "/comparison.png";
            Directory.CreateDirectory(Absolute(LeftUpperArmValidationFolder));
            string[] targetNames =
            {
                "Hands_Empty_Idle", "Armor_Protective_Idle", "Armor_Insulated_Idle",
                "Armor_Fireproof_Idle", "Armor_Physical_Idle", "Armor_Head_Idle"
            };
            var renderPaths = new List<string>();
            foreach (string targetName in targetNames)
            {
                string renderPath = Absolute(LeftUpperArmValidationFolder + "/render_" + targetName + ".png");
                RenderAppliedSceneTarget(RequireSceneObject(scene, targetName), renderPath, true);
                renderPaths.Add(renderPath);
            }
            ComposeLeftUpperArmGrid(renderPaths, Absolute(outputPath));
            File.WriteAllText(Absolute(LeftUpperArmValidationFolder + "/capture.txt"),
                "view=front close-up of transporter left upper arm\n" +
                "tileOrder=Hands_Empty_Idle,Armor_Protective_Idle,Armor_Insulated_Idle," +
                "Armor_Fireproof_Idle,Armor_Physical_Idle,Armor_Head_Idle\n" +
                "comparisonSha256=" + Sha256(Absolute(outputPath)) + "\n" +
                "sceneDirty=" + scene.isDirty + "\n",
                new UTF8Encoding(false));
            Debug.Log("Armor left upper arm cleanup comparison captured.");
        }

        internal static void ApplyArmorAppearance()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Armor appearance must be applied in Edit Mode.");
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be the active scene.");

            EnsureAssetFolder(RuntimeTextureFolder);
            EnsureAssetFolder(RuntimeMaterialFolder);
            Directory.CreateDirectory(Absolute(ValidationFolder));
            Material playerSourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(PlayerMaterialPath);
            Texture2D sourceAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(SourceTexturePath);
            if (playerSourceMaterial == null || sourceAsset == null)
                throw new InvalidOperationException("Player source material or texture is missing.");

            Texture2D source = ReadableCopy(sourceAsset);
            var transient = new List<UnityEngine.Object> { source };
            var report = new StringBuilder();
            report.AppendLine("Armor appearance applied directly to CargoRunMvp.");
            report.AppendLine("referenceImagesAreValidationTargets=True");
            report.AppendLine("generativeImageToolsUsed=False");
            Palette sourcePalette = AnalyzePalette(source, true);
            try
            {
                foreach (ArmorVariant variant in Variants)
                {
                    bool usesDirectReferenceRgb = variant.TargetName == "Armor_Fireproof_Idle" ||
                                                  variant.TargetName == "Armor_Physical_Idle";
                    Texture2D reference = null;
                    Texture2D recolored;
                    if (usesDirectReferenceRgb)
                    {
                        reference = LoadImage(ReferencePath(variant.ReferenceFileName), false);
                        recolored = TransferDirectReferenceColors(source, reference, variant.TargetName, out _);
                    }
                    else
                    {
                        recolored = RecolorClothing(source, sourcePalette,
                            ReferenceMatchedPalettes[variant.TargetName]);
                    }
                    if (reference != null) transient.Add(reference);
                    transient.Add(recolored);
                    string texturePath = RuntimeTextureFolder + "/" + variant.TargetName + "_BaseColor.png";
                    File.WriteAllBytes(Absolute(texturePath), recolored.EncodeToPNG());
                    ImportColorTexture(texturePath);
                    Texture2D textureAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                    string materialPath = RuntimeMaterialFolder + "/" + variant.TargetName + ".mat";
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (material == null)
                    {
                        material = new Material(playerSourceMaterial) { name = variant.TargetName };
                        AssetDatabase.CreateAsset(material, materialPath);
                    }
                    ApplyBaseTexture(material, textureAsset);
                    AssetDatabase.SaveAssetIfDirty(material);

                    GameObject target = RequireSceneObject(scene, variant.TargetName);
                    SkinnedMeshRenderer renderer = target.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                        .Single();
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => material).ToArray();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    EditorUtility.SetDirty(renderer);
                    report.AppendLine(variant.TargetName + " texture=" + texturePath +
                                      " textureSha256=" + Sha256(Absolute(texturePath)) +
                                      " material=" + materialPath);
                }

                Dictionary<string, string> helmetTexturePaths = WriteHelmetEmbeddedTexturesToRuntime();
                Material helmetMaterial = CreateOrUpdateHelmetMaterial(helmetTexturePaths);
                GameObject headTarget = RequireSceneObject(scene, "Armor_Head_Idle");
                Transform head = FindHead(headTarget);
                if (head == null) throw new InvalidOperationException("Armor_Head_Idle Head bone is missing.");
                Transform existing = head.Cast<Transform>().FirstOrDefault(item => item.name == HelmetChildName);
                if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
                GameObject helmetAsset = AssetDatabase.LoadAssetAtPath<GameObject>(HelmetModelPath);
                GameObject helmet = PrefabUtility.InstantiatePrefab(helmetAsset, scene) as GameObject;
                if (helmet == null) throw new InvalidOperationException("Helmet scene instance could not be created.");
                helmet.name = HelmetChildName;
                foreach (Renderer renderer in helmet.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => helmetMaterial).ToArray();
                HelmetFitMetrics fit = FitHelmet(headTarget, helmet, head);
                PrefabUtility.RecordPrefabInstancePropertyModifications(helmet.transform);
                EditorUtility.SetDirty(helmet);

                Texture2D hiddenHeadTexture = BuildHeadHiddenTexture(headTarget, fit.FinalHelmetBounds, head);
                transient.Add(hiddenHeadTexture);
                string hiddenHeadTexturePath = RuntimeTextureFolder + "/Armor_Head_Idle_BaseColor.png";
                File.WriteAllBytes(Absolute(hiddenHeadTexturePath), hiddenHeadTexture.EncodeToPNG());
                ImportColorTexture(hiddenHeadTexturePath, true);
                Texture2D hiddenHeadAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(hiddenHeadTexturePath);
                string headMaterialPath = RuntimeMaterialFolder + "/Armor_Head_Idle.mat";
                Material headMaterial = AssetDatabase.LoadAssetAtPath<Material>(headMaterialPath);
                if (headMaterial == null)
                {
                    headMaterial = new Material(playerSourceMaterial) { name = "Armor_Head_Idle" };
                    AssetDatabase.CreateAsset(headMaterial, headMaterialPath);
                }
                ApplyBaseTexture(headMaterial, hiddenHeadAsset);
                if (headMaterial.HasProperty("_Surface")) headMaterial.SetFloat("_Surface", 0f);
                if (headMaterial.HasProperty("_AlphaClip")) headMaterial.SetFloat("_AlphaClip", 1f);
                if (headMaterial.HasProperty("_Cutoff")) headMaterial.SetFloat("_Cutoff", 0.5f);
                headMaterial.EnableKeyword("_ALPHATEST_ON");
                headMaterial.SetOverrideTag("RenderType", "TransparentCutout");
                headMaterial.renderQueue = 2450;
                EditorUtility.SetDirty(headMaterial);
                AssetDatabase.SaveAssetIfDirty(headMaterial);
                SkinnedMeshRenderer headRenderer = headTarget
                    .GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
                headRenderer.sharedMaterials = headRenderer.sharedMaterials.Select(_ => headMaterial).ToArray();
                PrefabUtility.RecordPrefabInstancePropertyModifications(headRenderer);
                EditorUtility.SetDirty(headRenderer);
                report.AppendLine("Armor_Head_Idle helmet=" + HelmetModelPath);
                report.AppendLine("helmetSourceSha256=" + Sha256(Absolute(HelmetModelPath)));
                report.AppendLine("helmetUniformScale=" + Format(fit.LocalScale));
                report.AppendLine("helmetLocalPosition=" + Format(fit.LocalPosition));
                report.AppendLine("helmetLocalRotation=" + Format(fit.LocalEulerAngles));
                report.AppendLine("helmetMeshOrUvModified=False");
                report.AppendLine("existingPlayerHeadHidden=True");

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("CargoRunMvp scene save failed.");
                AssetDatabase.SaveAssets();
                report.AppendLine("sceneSaved=True");
                File.WriteAllText(Absolute(ValidationFolder + "/application.txt"), report.ToString(),
                    new UTF8Encoding(false));
            }
            finally
            {
                foreach (UnityEngine.Object item in transient)
                    if (item != null) UnityEngine.Object.DestroyImmediate(item);
            }
            Debug.Log("Armor appearance applied directly to the five requested scene objects.");
        }

        internal static void InspectArmorAppearance()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be the active scene.");
            var report = new StringBuilder();
            report.AppendLine("Direct Armor scene inspection.");
            foreach (ArmorVariant variant in Variants)
            {
                GameObject target = RequireSceneObject(scene, variant.TargetName);
                SkinnedMeshRenderer renderer = target.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
                Material material = renderer.sharedMaterial;
                string materialPath = AssetDatabase.GetAssetPath(material);
                string texturePath = AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap"));
                report.AppendLine(variant.TargetName + " material=" + materialPath +
                                  " texture=" + texturePath +
                                  " textureSha256=" + Sha256(Absolute(texturePath)));
            }
            GameObject headTarget = RequireSceneObject(scene, "Armor_Head_Idle");
            Transform head = FindHead(headTarget);
            Transform helmet = head == null ? null : head.Cast<Transform>()
                .SingleOrDefault(item => item.name == HelmetChildName);
            if (helmet == null) throw new InvalidOperationException("Armor_Head_Idle attached Helmet is missing.");
            Vector3 scale = helmet.localScale;
            bool uniformScale = Mathf.Abs(scale.x - scale.y) < 0.0001f &&
                                Mathf.Abs(scale.y - scale.z) < 0.0001f;
            report.AppendLine("Armor_Head_Idle helmetChild=" + HierarchyPath(helmet, headTarget.transform));
            report.AppendLine("helmetUniformScale=" + uniformScale);
            report.AppendLine("helmetModelSha256=" + Sha256(Absolute(HelmetModelPath)));
            report.AppendLine("sceneDirty=" + scene.isDirty);
            File.WriteAllText(Absolute(ValidationFolder + "/inspection.txt"), report.ToString(),
                new UTF8Encoding(false));
            Debug.Log("Armor appearance scene inspection complete. UniformHelmetScale=" + uniformScale + ".");
        }

        internal static void CaptureArmorAppearanceFinal(string outputPath)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be the active scene.");
            if (string.IsNullOrWhiteSpace(outputPath))
                outputPath = ValidationFolder + "/comparison.png";
            Directory.CreateDirectory(Absolute(ValidationFolder));
            var resultPaths = new List<string>();
            foreach (ArmorVariant variant in Variants)
            {
                string resultPath = Absolute(ValidationFolder + "/render_" + variant.TargetName + ".png");
                RenderAppliedSceneTarget(RequireSceneObject(scene, variant.TargetName), resultPath);
                resultPaths.Add(resultPath);
            }
            string headResult = Absolute(ValidationFolder + "/render_Armor_Head_Idle.png");
            RenderAppliedSceneTarget(RequireSceneObject(scene, "Armor_Head_Idle"), headResult);
            resultPaths.Add(headResult);
            string[] references = Variants.Select(item => ReferencePath(item.ReferenceFileName))
                .Concat(new[] { ReferencePath("transfer-helmet.png") }).ToArray();
            ComposeComparison(references, resultPaths.ToArray(), Absolute(outputPath));
            File.WriteAllText(Absolute(ValidationFolder + "/capture.txt"),
                "left=provided reference\nright=actual CargoRunMvp scene object\n" +
                "comparisonSha256=" + Sha256(Absolute(outputPath)) + "\n",
                new UTF8Encoding(false));
            Debug.Log("Direct Armor appearance comparison captured from the applied scene objects.");
        }

        private static void EnsureAssetFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(parent))
                throw new InvalidOperationException("Invalid asset folder: " + path);
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void ImportColorTexture(string path, bool preserveAlpha = false)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Texture importer missing: " + path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = preserveAlpha
                ? TextureImporterAlphaSource.FromInput
                : TextureImporterAlphaSource.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 8192;
            importer.SaveAndReimport();
        }

        private static void ImportDataTexture(string path, bool normalMap)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Texture importer missing: " + path);
            importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 8192;
            importer.SaveAndReimport();
        }

        private static void ApplyBaseTexture(Material material, Texture2D texture)
        {
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_EmissionMap") && material.GetTexture("_EmissionMap") != null)
                material.SetTexture("_EmissionMap", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            EditorUtility.SetDirty(material);
        }

        private static Dictionary<string, string> WriteHelmetEmbeddedTexturesToRuntime()
        {
            Dictionary<string, byte[]> embedded = ExtractEmbeddedTextures(Absolute(HelmetModelPath));
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string sourceName in new[]
                     {
                         "base_color.jpg", "normal.jpg", "texture_0_metallic.png",
                         "texture_0_roughness.png"
                     })
            {
                if (!embedded.TryGetValue(sourceName, out byte[] bytes) || bytes.Length == 0)
                    throw new InvalidOperationException("Helmet embedded texture is missing: " + sourceName);
                string path = RuntimeTextureFolder + "/Helmet_" + sourceName;
                File.WriteAllBytes(Absolute(path), bytes);
                if (sourceName == "base_color.jpg") ImportColorTexture(path);
                else ImportDataTexture(path, sourceName == "normal.jpg");
                if (Sha256(Absolute(path)) != Sha256Bytes(bytes))
                    throw new InvalidOperationException("Helmet embedded texture bytes changed: " + sourceName);
                result[sourceName] = path;
            }
            return result;
        }

        private static Material CreateOrUpdateHelmetMaterial(Dictionary<string, string> texturePaths)
        {
            string packedPath = RuntimeTextureFolder + "/Helmet_metallic_smoothness.png";
            Texture2D metallic = LoadImage(Absolute(texturePaths["texture_0_metallic.png"]), true);
            Texture2D roughness = LoadImage(Absolute(texturePaths["texture_0_roughness.png"]), true);
            Texture2D packed = null;
            try
            {
                packed = CombineMetallicSmoothness(metallic, roughness);
                File.WriteAllBytes(Absolute(packedPath), packed.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(metallic);
                UnityEngine.Object.DestroyImmediate(roughness);
                if (packed != null) UnityEngine.Object.DestroyImmediate(packed);
            }
            ImportDataTexture(packedPath, false);

            string materialPath = RuntimeMaterialFolder + "/Helmet_Original.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                Material embedded = AssetDatabase.LoadAllAssetsAtPath(HelmetModelPath).OfType<Material>()
                    .SingleOrDefault();
                if (embedded == null) throw new InvalidOperationException("Helmet embedded material is missing.");
                material = new Material(embedded) { name = "Helmet_Original" };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePaths["base_color.jpg"]);
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePaths["normal.jpg"]);
            Texture2D metallicSmoothness = AssetDatabase.LoadAssetAtPath<Texture2D>(packedPath);
            ApplyBaseTexture(material, baseColor);
            if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", normal);
            if (material.HasProperty("_MetallicGlossMap"))
                material.SetTexture("_MetallicGlossMap", metallicSmoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.20f);
            Color helmetBrightness = new Color(2.2f, 2.2f, 2.2f, 1f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", helmetBrightness);
            if (material.HasProperty("_Color")) material.SetColor("_Color", helmetBrightness);
            material.EnableKeyword("_NORMALMAP");
            material.DisableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        private static string Sha256Bytes(byte[] bytes)
        {
            using (SHA256 hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", string.Empty);
        }

        private static HelmetFitMetrics FitHelmet(GameObject player, GameObject helmet, Transform head)
        {
            Bounds bodyBounds = RendererBounds(player, helmet.transform);
            Bounds initialHelmetBounds = RendererBounds(helmet);
            float desiredHeight = bodyBounds.size.y * HelmetHeightToBodyRatio;
            float scaleFactor = desiredHeight / Mathf.Max(0.0001f, initialHelmetBounds.size.y);
            helmet.transform.localScale *= scaleFactor;
            GameObject helmetAsset = AssetDatabase.LoadAssetAtPath<GameObject>(HelmetModelPath);
            helmet.transform.rotation = player.transform.rotation * helmetAsset.transform.localRotation;
            Bounds scaledBounds = RendererBounds(helmet);
            Vector3 desiredCenter = head.position;
            desiredCenter.y = bodyBounds.max.y - desiredHeight;
            desiredCenter += player.transform.forward * (scaledBounds.size.z * 0.35f);
            helmet.transform.position += desiredCenter - scaledBounds.center;
            helmet.transform.SetParent(head, true);
            Bounds finalBounds = RendererBounds(helmet);
            return new HelmetFitMetrics(bodyBounds, initialHelmetBounds, finalBounds, desiredHeight,
                scaleFactor, head.position, helmet.transform.localPosition, helmet.transform.localEulerAngles,
                helmet.transform.localScale);
        }

        private static Texture2D BuildHeadHiddenTexture(GameObject player, Bounds helmetBounds, Transform head)
        {
            Texture2D source = LoadImage(Absolute(SourceTexturePath), false);
            try
            {
                Color32[] pixels = source.GetPixels32();
                for (int index = 0; index < pixels.Length; index++) pixels[index].a = 255;
                SkinnedMeshRenderer renderer = player.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
                var baked = new Mesh();
                try
                {
                    renderer.BakeMesh(baked);
                    Vector3[] vertices = baked.vertices;
                    Vector2[] uv = baked.uv;
                    int[] triangles = baked.triangles;
                    if (uv.Length != vertices.Length)
                        throw new InvalidOperationException("Player baked UV and vertex counts differ.");
                    float lowerY = head.position.y + 0.08f;
                    float xMargin = helmetBounds.size.x * 0.02f;
                    float zMargin = helmetBounds.size.z * 0.02f;
                    for (int index = 0; index < triangles.Length; index += 3)
                    {
                        int ia = triangles[index];
                        int ib = triangles[index + 1];
                        int ic = triangles[index + 2];
                        Vector3 a = renderer.transform.TransformPoint(vertices[ia]);
                        Vector3 b = renderer.transform.TransformPoint(vertices[ib]);
                        Vector3 c = renderer.transform.TransformPoint(vertices[ic]);
                        if (Mathf.Min(a.y, Mathf.Min(b.y, c.y)) < lowerY) continue;
                        Vector3 center = (a + b + c) / 3f;
                        if (center.x < helmetBounds.min.x - xMargin ||
                            center.x > helmetBounds.max.x + xMargin ||
                            center.z < helmetBounds.min.z - zMargin ||
                            center.z > helmetBounds.max.z + zMargin) continue;
                        RasterizeAlphaTriangle(pixels, source.width, source.height, uv[ia], uv[ib], uv[ic]);
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(baked);
                }
                var result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false)
                {
                    name = "ArmorHead_ExistingHeadHidden"
                };
                result.SetPixels32(pixels);
                result.Apply(false, false);
                return result;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static void RasterizeAlphaTriangle(Color32[] pixels, int width, int height,
            Vector2 uvA, Vector2 uvB, Vector2 uvC)
        {
            uvA = new Vector2(Mathf.Repeat(uvA.x, 1f), Mathf.Repeat(uvA.y, 1f));
            uvB = new Vector2(Mathf.Repeat(uvB.x, 1f), Mathf.Repeat(uvB.y, 1f));
            uvC = new Vector2(Mathf.Repeat(uvC.x, 1f), Mathf.Repeat(uvC.y, 1f));
            if (Mathf.Max(uvA.x, Mathf.Max(uvB.x, uvC.x)) - Mathf.Min(uvA.x, Mathf.Min(uvB.x, uvC.x)) > 0.5f ||
                Mathf.Max(uvA.y, Mathf.Max(uvB.y, uvC.y)) - Mathf.Min(uvA.y, Mathf.Min(uvB.y, uvC.y)) > 0.5f)
                return;
            Vector2 a = new Vector2(uvA.x * (width - 1), uvA.y * (height - 1));
            Vector2 b = new Vector2(uvB.x * (width - 1), uvB.y * (height - 1));
            Vector2 c = new Vector2(uvC.x * (width - 1), uvC.y * (height - 1));
            int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))), 0, width - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))), 0, width - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))), 0, height - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))), 0, height - 1);
            float area = Edge(a, b, c);
            if (Mathf.Abs(area) < 0.0001f) return;
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                float first = Edge(b, c, point);
                float second = Edge(c, a, point);
                float third = Edge(a, b, point);
                bool inside = area > 0f
                    ? first >= 0f && second >= 0f && third >= 0f
                    : first <= 0f && second <= 0f && third <= 0f;
                if (!inside) continue;
                int pixelIndex = y * width + x;
                Color32 pixel = pixels[pixelIndex];
                pixel.a = 0;
                pixels[pixelIndex] = pixel;
            }
        }

        private static float Edge(Vector2 a, Vector2 b, Vector2 point) =>
            (point.x - a.x) * (b.y - a.y) - (point.y - a.y) * (b.x - a.x);

        private static void RenderAppliedSceneTarget(GameObject source, string outputPath,
            bool encodeLinearPreviewAsSrgb = false)
        {
            var preview = new PreviewRenderUtility(true);
            bool previewBegun = false;
            try
            {
                GameObject instance = UnityEngine.Object.Instantiate(source);
                instance.name = source.name + "_DirectValidation";
                instance.hideFlags = HideFlags.HideAndDontSave;
                instance.SetActive(true);
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
                instance.transform.localScale = Vector3.one;
                SetLayerRecursively(instance, PreviewLayer);
                foreach (SkinnedMeshRenderer renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    renderer.updateWhenOffscreen = true;
                Animator animator = instance.GetComponentInChildren<Animator>(true);
                if (animator != null)
                {
                    animator.Rebind();
                    animator.Update(0f);
                }
                preview.AddSingleGO(instance);
                Bounds bounds = RendererBounds(instance);
                Camera camera = preview.camera;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.white;
                camera.orthographic = true;
                camera.aspect = RenderWidth / (float)RenderHeight;
                camera.orthographicSize = bounds.extents.y * 1.045f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 20f;
                camera.allowHDR = false;
                camera.allowMSAA = true;
                camera.cullingMask = 1 << PreviewLayer;
                Vector3 center = bounds.center;
                float distance = Mathf.Max(4f, bounds.size.magnitude * 2f);
                camera.transform.position = center + instance.transform.forward * distance;
                camera.transform.rotation = Quaternion.LookRotation(center - camera.transform.position, Vector3.up);
                preview.lights[0].color = new Color(1f, 0.98f, 0.95f);
                preview.lights[0].intensity = 1.15f;
                preview.lights[0].transform.rotation = Quaternion.Euler(32f, 145f, 0f);
                preview.lights[1].color = new Color(0.78f, 0.88f, 1f);
                preview.lights[1].intensity = 0.58f;
                preview.lights[1].transform.rotation = Quaternion.Euler(338f, 215f, 0f);
                preview.ambientColor = new Color(0.36f, 0.38f, 0.42f);
                preview.BeginPreview(new Rect(0f, 0f, RenderWidth, RenderHeight), GUIStyle.none);
                previewBegun = true;
                camera.Render();
                RenderTexture rendered = preview.EndPreview() as RenderTexture;
                previewBegun = false;
                if (rendered == null) throw new InvalidOperationException("Preview did not return a RenderTexture.");
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = rendered;
                var pixels = new Texture2D(RenderWidth, RenderHeight, TextureFormat.RGB24, false,
                    encodeLinearPreviewAsSrgb);
                pixels.ReadPixels(new Rect(0, 0, RenderWidth, RenderHeight), 0, 0);
                pixels.Apply(false, false);
                if (encodeLinearPreviewAsSrgb)
                {
                    // PreviewRenderUtility returns linear values; PNG comparison tiles store sRGB bytes.
                    Color[] linearPixels = pixels.GetPixels();
                    for (int index = 0; index < linearPixels.Length; index++)
                        linearPixels[index] = linearPixels[index].gamma;
                    pixels.SetPixels(linearPixels);
                    pixels.Apply(false, false);
                }
                File.WriteAllBytes(outputPath, pixels.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(pixels);
                RenderTexture.active = previous;
            }
            finally
            {
                if (previewBegun) preview.EndPreview();
                preview.Cleanup();
            }
        }

        private static Texture2D CombineMetallicSmoothness(Texture2D metallic, Texture2D roughness)
        {
            if (metallic.width != roughness.width || metallic.height != roughness.height)
                throw new InvalidOperationException("Helmet embedded metallic and roughness texture sizes differ.");
            Color32[] metalPixels = metallic.GetPixels32();
            Color32[] roughPixels = roughness.GetPixels32();
            var output = new Color32[metalPixels.Length];
            for (int index = 0; index < output.Length; index++)
            {
                byte metal = metalPixels[index].r;
                byte smooth = (byte)(255 - roughPixels[index].r);
                output[index] = new Color32(metal, metal, metal, smooth);
            }
            var result = new Texture2D(metallic.width, metallic.height, TextureFormat.RGBA32, false, true)
            {
                name = "OriginalHelmet_MetallicSmoothness_Conversion"
            };
            result.SetPixels32(output);
            result.Apply(false, false);
            return result;
        }

        private static Texture2D TransferDirectReferenceColors(Texture2D source, Texture2D reference,
            string targetName, out ColorTransferMetrics metrics)
        {
            Color32[] sourcePixels = source.GetPixels32();
            var sourceRanks = new List<RankedSourcePixel>();
            for (int index = 0; index < sourcePixels.Length; index++)
            {
                if (!IsSourceClothing(sourcePixels[index], out _)) continue;
                Color color = sourcePixels[index];
                Color.RGBToHSV(color, out float hue, out float saturation, out float value);
                sourceRanks.Add(new RankedSourcePixel(index, hue, saturation, value));
            }
            if (sourceRanks.Count == 0)
                throw new InvalidOperationException("No source clothing pixels were found.");

            List<ExactColorSample> referenceSamples = CollectExactTargetSamples(reference, targetName);
            if (referenceSamples.Count < 256)
                throw new InvalidOperationException(targetName + " exact color distribution has too few pixels.");
            referenceSamples.Sort(ExactColorSample.CompareByValueHueSaturation);
            sourceRanks.Sort(RankedSourcePixel.CompareByValueHueSaturation);

            Color32[] output = new Color32[sourcePixels.Length];
            Array.Copy(sourcePixels, output, sourcePixels.Length);
            for (int rank = 0; rank < sourceRanks.Count; rank++)
            {
                RankedSourcePixel sourcePixel = sourceRanks[rank];
                float quantile = sourceRanks.Count == 1 ? 0f : rank / (float)(sourceRanks.Count - 1);
                ExactColorSample referenceColor = SampleAtQuantile(referenceSamples, quantile);
                output[sourcePixel.Index] = referenceColor.Rgb;
            }

            var result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false)
            {
                name = targetName + "_DirectReferenceRgbTransfer"
            };
            result.SetPixels32(output);
            result.Apply(false, false);
            metrics = new ColorTransferMetrics(sourceRanks.Count, referenceSamples.Count);
            return result;
        }

        private static List<PixelComponent> FindPurpleMarkerComponents(Texture2D texture)
        {
            Color32[] pixels = texture.GetPixels32();
            var candidates = new bool[pixels.Length];
            for (int index = 0; index < pixels.Length; index++)
                candidates[index] = IsPurpleMarkerColor(pixels[index]);

            var visited = new bool[pixels.Length];
            var result = new List<PixelComponent>();
            int width = texture.width;
            int height = texture.height;
            for (int start = 0; start < pixels.Length; start++)
            {
                if (!candidates[start] || visited[start]) continue;
                var queue = new List<int> { start };
                visited[start] = true;
                int minX = width;
                int minY = height;
                int maxX = 0;
                int maxY = 0;
                long sumX = 0;
                long sumY = 0;
                for (int cursor = 0; cursor < queue.Count; cursor++)
                {
                    int pixelIndex = queue[cursor];
                    int x = pixelIndex % width;
                    int y = pixelIndex / width;
                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                    sumX += x;
                    sumY += y;
                    for (int offsetY = -1; offsetY <= 1; offsetY++)
                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        if (offsetX == 0 && offsetY == 0) continue;
                        int neighborX = x + offsetX;
                        int neighborY = y + offsetY;
                        if (neighborX < 0 || neighborX >= width || neighborY < 0 || neighborY >= height)
                            continue;
                        int neighbor = neighborY * width + neighborX;
                        if (!candidates[neighbor] || visited[neighbor]) continue;
                        visited[neighbor] = true;
                        queue.Add(neighbor);
                    }
                }
                if (queue.Count >= 16)
                    result.Add(new PixelComponent(minX, minY, maxX, maxY,
                        (int)(sumX / queue.Count), (int)(sumY / queue.Count), queue.ToArray()));
            }
            return result.OrderByDescending(component => component.PixelCount).ToList();
        }

        private static int CountPurplePixelsInMask(Texture2D texture, bool[] mask)
        {
            Color32[] pixels = texture.GetPixels32();
            int count = 0;
            for (int index = 0; index < pixels.Length; index++)
                if (mask[index] && IsPurpleMarkerColor(pixels[index])) count++;
            return count;
        }

        private static bool IsPurpleMarkerColor(Color32 pixel)
        {
            Color color = pixel;
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            return hue >= 0.72f && hue <= 0.92f && saturation >= 0.18f && value >= 0.45f;
        }

        private static bool[] BuildLeftUpperArmCleanupPlan(Texture2D source, SkinnedMeshRenderer renderer,
            List<PixelComponent> components, out List<CleanupRegion> regions)
        {
            if (source.width != 2048 || source.height != 2048)
                throw new InvalidOperationException("Confirmed LeftArm cleanup regions require 2048x2048 textures.");
            bool[] leftArmCoverage = BuildLeftUpperArmUvCoverage(source.width, source.height, renderer);
            Color32[] sourcePixels = source.GetPixels32();
            if (components == null) throw new ArgumentNullException(nameof(components));
            // These five regions were established by the read-only UV/bone inspection above.
            var rawRegions = new List<CleanupRegion>
            {
                new CleanupRegion(1002, 1409, 1081, 1554, 0, 0, 0),
                new CleanupRegion(1492, 1690, 1551, 1791, 0, 0, 0),
                new CleanupRegion(107, 1449, 136, 1500, 0, 0, 0),
                new CleanupRegion(405, 1729, 442, 1782, 0, 0, 0),
                new CleanupRegion(8, 1467, 32, 1546, 0, 0, 0)
            };
            foreach (CleanupRegion region in rawRegions)
            {
                int leftArmPixels = 0;
                for (int y = region.MinY; y <= region.MaxY; y++)
                for (int x = region.MinX; x <= region.MaxX; x++)
                    if (leftArmCoverage[y * source.width + x]) leftArmPixels++;
                if (leftArmPixels == 0)
                    throw new InvalidOperationException("Confirmed cleanup region no longer overlaps LeftArm UVs.");
            }

            var mask = new bool[sourcePixels.Length];
            foreach (CleanupRegion region in rawRegions)
            {
                for (int y = region.MinY; y <= region.MaxY; y++)
                for (int x = region.MinX; x <= region.MaxX; x++)
                    mask[y * source.width + x] = true;
            }

            regions = rawRegions;
            return mask;
        }

        private static bool[] BuildLeftUpperArmUvCoverage(int width, int height,
            SkinnedMeshRenderer renderer)
        {
            Mesh mesh = renderer.sharedMesh;
            Vector2[] uvs = mesh.uv;
            int[] triangles = mesh.triangles;
            BoneWeight[] weights = mesh.boneWeights;
            int leftArmBone = Array.FindIndex(renderer.bones,
                bone => bone != null && string.Equals(bone.name, "LeftArm", StringComparison.OrdinalIgnoreCase));
            if (leftArmBone < 0) throw new InvalidOperationException("LeftArm bone was not found.");
            if (weights.Length != mesh.vertexCount)
                throw new InvalidOperationException("Mesh bone weights do not match its vertices.");

            var coverage = new bool[width * height];
            for (int triangle = 0; triangle < triangles.Length; triangle += 3)
            {
                int a = triangles[triangle];
                int b = triangles[triangle + 1];
                int c = triangles[triangle + 2];
                float leftArmInfluence = (BoneInfluence(weights[a], leftArmBone) +
                                          BoneInfluence(weights[b], leftArmBone) +
                                          BoneInfluence(weights[c], leftArmBone)) / 3f;
                if (leftArmInfluence < 0.18f) continue;
                RasterizeUvTriangle(coverage, width, height, uvs[a], uvs[b], uvs[c]);
            }
            return coverage;
        }

        private static float BoneInfluence(BoneWeight weight, int boneIndex)
        {
            float result = 0f;
            if (weight.boneIndex0 == boneIndex) result += weight.weight0;
            if (weight.boneIndex1 == boneIndex) result += weight.weight1;
            if (weight.boneIndex2 == boneIndex) result += weight.weight2;
            if (weight.boneIndex3 == boneIndex) result += weight.weight3;
            return result;
        }

        private static void RasterizeUvTriangle(bool[] coverage, int width, int height,
            Vector2 uvA, Vector2 uvB, Vector2 uvC)
        {
            Vector2 a = new Vector2(uvA.x * width, uvA.y * height);
            Vector2 b = new Vector2(uvB.x * width, uvB.y * height);
            Vector2 c = new Vector2(uvC.x * width, uvC.y * height);
            int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))), 0, width - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))), 0, width - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))), 0, height - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))), 0, height - 1);
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
                if (PointInUvTriangle(new Vector2(x + 0.5f, y + 0.5f), a, b, c))
                    coverage[y * width + x] = true;
        }

        private static bool IsFlagRed(Color32 pixel)
        {
            Color color = pixel;
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            return (hue <= 0.075f || hue >= 0.965f) && saturation >= 0.30f && value >= 0.25f;
        }

        private static Vector2Int FindBestCloneOffset(CleanupRegion region, Color32[] sourcePixels,
            bool[] mask, bool[] coverage, int width, int height)
        {
            int regionWidth = region.MaxX - region.MinX + 1;
            int regionHeight = region.MaxY - region.MinY + 1;
            int horizontal = regionWidth + 6;
            int vertical = regionHeight + 6;
            Vector2Int[] candidates =
            {
                new Vector2Int(-horizontal, 0), new Vector2Int(horizontal, 0),
                new Vector2Int(0, -vertical), new Vector2Int(0, vertical),
                new Vector2Int(-Mathf.Max(8, regionWidth / 2), 0),
                new Vector2Int(Mathf.Max(8, regionWidth / 2), 0),
                new Vector2Int(0, -Mathf.Max(8, regionHeight / 2)),
                new Vector2Int(0, Mathf.Max(8, regionHeight / 2))
            };
            Vector2Int best = Vector2Int.zero;
            int bestScore = -1;
            foreach (Vector2Int candidate in candidates)
            {
                int score = 0;
                for (int y = region.MinY; y <= region.MaxY; y++)
                for (int x = region.MinX; x <= region.MaxX; x++)
                {
                    int targetIndex = y * width + x;
                    if (!mask[targetIndex]) continue;
                    int sourceX = x + candidate.x;
                    int sourceY = y + candidate.y;
                    if (sourceX < 0 || sourceX >= width || sourceY < 0 || sourceY >= height) continue;
                    int sourceIndex = sourceY * width + sourceX;
                    if (!mask[sourceIndex] && coverage[sourceIndex] &&
                        IsSourceClothing(sourcePixels[sourceIndex], out _)) score++;
                }
                if (score <= bestScore) continue;
                bestScore = score;
                best = candidate;
            }
            if (bestScore <= 0)
                throw new InvalidOperationException("No adjacent LeftArm clothing clone source was found.");
            return best;
        }

        private static Texture2D BuildCleanupMaskPreview(Texture2D source, bool[] mask)
        {
            Color32[] pixels = source.GetPixels32();
            for (int index = 0; index < pixels.Length; index++)
            {
                if (mask[index])
                    pixels[index] = new Color32(255, 32, 32, 255);
                else
                    pixels[index] = new Color32((byte)(pixels[index].r / 3),
                        (byte)(pixels[index].g / 3), (byte)(pixels[index].b / 3), 255);
            }
            var result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false)
            {
                name = "LeftUpperArmCleanupMaskPreview"
            };
            result.SetPixels32(pixels);
            result.Apply(false, false);
            return result;
        }

        private static Texture2D CloneCleanupRegions(Texture2D texture, Color32[] donorValidationPixels,
            bool[] mask, List<CleanupRegion> regions, out int clonedPixels)
        {
            Color32[] input = texture.GetPixels32();
            Color32[] output = new Color32[input.Length];
            Array.Copy(input, output, input.Length);
            clonedPixels = 0;
            foreach (CleanupRegion region in regions)
            {
                for (int y = region.MinY; y <= region.MaxY; y++)
                for (int x = region.MinX; x <= region.MaxX; x++)
                {
                    int targetIndex = y * texture.width + x;
                    if (!mask[targetIndex]) continue;
                    int donorIndex = FindNearestClothingDonor(x, y, texture.width, texture.height,
                        donorValidationPixels, mask);
                    output[targetIndex] = input[donorIndex];
                    clonedPixels++;
                }
            }
            var result = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false, false)
            {
                name = texture.name + "_LeftUpperArmCleanup"
            };
            result.SetPixels32(output);
            result.Apply(false, false);
            return result;
        }

        private static bool IsValidClothingDonor(int x, int y, int width, int height,
            Color32[] donorValidationPixels, bool[] mask)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return false;
            int index = y * width + x;
            return !mask[index] && IsCleanupFabricDonor(donorValidationPixels[index]);
        }

        private static bool IsCleanupFabricDonor(Color32 pixel)
        {
            Color color = pixel;
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            return hue >= 0.48f && hue <= 0.69f && saturation >= 0.30f &&
                   value >= 0.14f && value <= 0.82f;
        }

        private static int FindNearestClothingDonor(int centerX, int centerY, int width, int height,
            Color32[] donorValidationPixels, bool[] mask)
        {
            centerX = Mathf.Clamp(centerX, 0, width - 1);
            centerY = Mathf.Clamp(centerY, 0, height - 1);
            for (int radius = 0; radius <= 128; radius++)
            {
                int minX = centerX - radius;
                int maxX = centerX + radius;
                int minY = centerY - radius;
                int maxY = centerY + radius;
                for (int x = minX; x <= maxX; x++)
                {
                    if (IsValidClothingDonor(x, minY, width, height, donorValidationPixels, mask))
                        return minY * width + x;
                    if (IsValidClothingDonor(x, maxY, width, height, donorValidationPixels, mask))
                        return maxY * width + x;
                }
                for (int y = minY + 1; y < maxY; y++)
                {
                    if (IsValidClothingDonor(minX, y, width, height, donorValidationPixels, mask))
                        return y * width + minX;
                    if (IsValidClothingDonor(maxX, y, width, height, donorValidationPixels, mask))
                        return y * width + maxX;
                }
            }
            throw new InvalidOperationException("No exact neighboring clothing pixel was found for cleanup.");
        }

        private static bool PointInUvTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            float denominator = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
            if (Mathf.Abs(denominator) < 0.0000001f) return false;
            float first = ((b.y - c.y) * (point.x - c.x) +
                           (c.x - b.x) * (point.y - c.y)) / denominator;
            float second = ((c.y - a.y) * (point.x - c.x) +
                            (a.x - c.x) * (point.y - c.y)) / denominator;
            float third = 1f - first - second;
            return first >= -0.0001f && second >= -0.0001f && third >= -0.0001f;
        }

        private static string DominantBoneName(BoneWeight weight, Transform[] bones)
        {
            int index = weight.boneIndex0;
            float maximum = weight.weight0;
            if (weight.weight1 > maximum) { index = weight.boneIndex1; maximum = weight.weight1; }
            if (weight.weight2 > maximum) { index = weight.boneIndex2; maximum = weight.weight2; }
            if (weight.weight3 > maximum) index = weight.boneIndex3;
            return index >= 0 && index < bones.Length && bones[index] != null ? bones[index].name : "<unknown>";
        }

        private static bool IsSourceClothing(Color32 pixel, out byte valueByte)
        {
            Color color = pixel;
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            valueByte = (byte)Mathf.Clamp(Mathf.RoundToInt(value * 255f), 0, 255);
            return hue >= 0.43f && hue <= 0.74f && saturation >= 0.04f && value >= 0.10f;
        }

        private static List<ExactColorSample> CollectExactTargetSamples(Texture2D texture, string targetName)
        {
            bool fireproof = targetName == "Armor_Fireproof_Idle";
            var result = new List<ExactColorSample>();
            foreach (Color32 pixel in texture.GetPixels32())
            {
                if (pixel.a < 16) continue;
                Color color = pixel;
                Color.RGBToHSV(color, out float hue, out float saturation, out float value);
                bool selected = fireproof
                    ? (hue <= 0.12f || hue >= 0.94f) && saturation >= 0.18f && value >= 0.10f && value <= 0.96f
                    : hue >= 0.43f && hue <= 0.58f && saturation >= 0.12f && value >= 0.08f && value <= 0.90f;
                if (selected) result.Add(new ExactColorSample(pixel, hue, saturation, value));
            }
            return result;
        }

        private static ExactColorSample SampleAtQuantile(List<ExactColorSample> samples, float quantile)
        {
            int index = Mathf.Clamp(Mathf.RoundToInt(quantile * (samples.Count - 1)), 0, samples.Count - 1);
            return samples[index];
        }

        private static string FormatExactQuantiles(Texture2D texture, string targetName)
        {
            List<ExactColorSample> samples = CollectExactTargetSamples(texture, targetName);
            samples.Sort(ExactColorSample.CompareByValueHueSaturation);
            return string.Join(" | ", new[] { 0.10f, 0.25f, 0.50f, 0.75f, 0.90f }.Select(quantile =>
            {
                ExactColorSample sample = SampleAtQuantile(samples, quantile);
                return quantile.ToString("F2", CultureInfo.InvariantCulture) + ":H" +
                       sample.Hue.ToString("F3", CultureInfo.InvariantCulture) + " S" +
                       sample.Saturation.ToString("F3", CultureInfo.InvariantCulture) + " V" +
                       sample.Value.ToString("F3", CultureInfo.InvariantCulture);
            }));
        }

        private static Texture2D RecolorClothing(Texture2D source, Palette sourcePalette, Palette targetPalette)
        {
            Color32[] input = source.GetPixels32();
            var output = new Color32[input.Length];
            float saturationScale = targetPalette.Saturation / Mathf.Max(0.05f, sourcePalette.Saturation);
            float valueScale = targetPalette.Value / Mathf.Max(0.05f, sourcePalette.Value);
            for (int index = 0; index < input.Length; index++)
            {
                Color32 sourcePixel = input[index];
                Color original = new Color(sourcePixel.r / 255f, sourcePixel.g / 255f,
                    sourcePixel.b / 255f, 1f);
                Color.RGBToHSV(original, out float hue, out float saturation, out float value);
                float chromaMask = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 0.34f, saturation));
                float darkProtection = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.17f, 0.31f, value));
                float hueMask = HueRangeMask(hue, 0.43f, 0.47f, 0.69f, 0.74f);
                float mask = chromaMask * darkProtection * hueMask;
                if (mask <= 0.0001f)
                {
                    output[index] = new Color32(sourcePixel.r, sourcePixel.g, sourcePixel.b, 255);
                    continue;
                }
                float shiftedHue = WrapHue(targetPalette.Hue + HueDelta(sourcePalette.Hue, hue) * 0.38f);
                float shiftedSaturation = Mathf.Clamp01(saturation * saturationScale);
                float shiftedValue = Mathf.Clamp01(value * valueScale);
                Color shifted = Color.HSVToRGB(shiftedHue, shiftedSaturation, shiftedValue);
                shifted.a = 1f;
                Color blended = Color.Lerp(original, shifted, mask);
                blended.a = 1f;
                output[index] = blended;
            }

            var result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false)
            {
                name = "ArmorReferencePaletteRecolor"
            };
            result.SetPixels32(output);
            result.Apply(false, false);
            return result;
        }

        private static Palette AnalyzePalette(Texture2D texture, bool sourceTexture)
        {
            const int bins = 72;
            var weights = new double[bins];
            Color32[] pixels = texture.GetPixels32();
            int xMin = 0;
            int xMax = texture.width;
            int yMin = 0;
            int yMax = texture.height;
            for (int y = yMin; y < yMax; y++)
            for (int x = xMin; x < xMax; x++)
            {
                Color color = pixels[y * texture.width + x];
                Color.RGBToHSV(color, out float hue, out float saturation, out float value);
                if (saturation < 0.20f || value < 0.16f || value > 0.96f) continue;
                if (sourceTexture && (hue < 0.43f || hue > 0.74f)) continue;
                int bin = Mathf.Min(bins - 1, Mathf.FloorToInt(hue * bins));
                weights[bin] += saturation * Mathf.Sqrt(value);
            }
            int dominant = 0;
            for (int index = 1; index < bins; index++)
                if (weights[index] > weights[dominant]) dominant = index;
            float center = (dominant + 0.5f) / bins;
            double sumX = 0d;
            double sumY = 0d;
            double sumSaturation = 0d;
            double sumValue = 0d;
            double sumWeight = 0d;
            for (int y = yMin; y < yMax; y++)
            for (int x = xMin; x < xMax; x++)
            {
                Color color = pixels[y * texture.width + x];
                Color.RGBToHSV(color, out float hue, out float saturation, out float value);
                if (saturation < 0.20f || value < 0.16f || value > 0.96f) continue;
                if (sourceTexture && (hue < 0.43f || hue > 0.74f)) continue;
                if (Mathf.Abs(HueDelta(center, hue)) > 0.075f) continue;
                double weight = saturation * Mathf.Sqrt(value);
                double angle = hue * Math.PI * 2d;
                sumX += Math.Cos(angle) * weight;
                sumY += Math.Sin(angle) * weight;
                sumSaturation += saturation * weight;
                sumValue += value * weight;
                sumWeight += weight;
            }
            if (sumWeight <= 0d) throw new InvalidOperationException("No chromatic palette pixels were found.");
            float hueResult = (float)(Math.Atan2(sumY, sumX) / (Math.PI * 2d));
            if (hueResult < 0f) hueResult += 1f;
            return new Palette(hueResult, (float)(sumSaturation / sumWeight),
                (float)(sumValue / sumWeight));
        }

        private static float HueRangeMask(float hue, float outerMin, float innerMin, float innerMax, float outerMax)
        {
            if (hue <= outerMin || hue >= outerMax) return 0f;
            if (hue >= innerMin && hue <= innerMax) return 1f;
            return hue < innerMin ? Mathf.InverseLerp(outerMin, innerMin, hue) :
                Mathf.InverseLerp(outerMax, innerMax, hue);
        }

        private static float HueDelta(float from, float to)
        {
            float delta = to - from;
            if (delta > 0.5f) delta -= 1f;
            if (delta < -0.5f) delta += 1f;
            return delta;
        }

        private static float WrapHue(float value)
        {
            value %= 1f;
            return value < 0f ? value + 1f : value;
        }

        private static Texture2D ReadableCopy(Texture source)
        {
            RenderTexture render = RenderTexture.GetTemporary(source.width, source.height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Graphics.Blit(source, render);
                RenderTexture.active = render;
                var result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false);
                result.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                result.Apply(false, false);
                return result;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
            }
        }

        private static Texture2D LoadImage(string path, bool linear)
        {
            var result = new Texture2D(2, 2, TextureFormat.RGBA32, false, linear);
            if (!result.LoadImage(File.ReadAllBytes(path), false))
            {
                UnityEngine.Object.DestroyImmediate(result);
                throw new InvalidOperationException("Image could not be loaded: " + path);
            }
            return result;
        }

        private static Bounds RendererBounds(GameObject root, Transform excludedRoot = null)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled && item.gameObject.activeInHierarchy &&
                               (excludedRoot == null || !item.transform.IsChildOf(excludedRoot)))
                .ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException(root.name + " has no visible renderer.");
            Bounds result = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++) result.Encapsulate(renderers[index].bounds);
            return result;
        }

        private static void AddDirectionalLight(Scene scene, string name, Color color, float intensity,
            Quaternion rotation)
        {
            var lightObject = new GameObject(name);
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
            light.cullingMask = 1 << PreviewLayer;
            lightObject.transform.rotation = rotation;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                item.gameObject.layer = layer;
        }

        private static void ComposeComparison(string[] references, string[] results, string outputPath)
        {
            if (references.Length != results.Length)
                throw new InvalidOperationException("Reference/result row counts differ.");
            var loaded = new List<Texture2D>();
            try
            {
                var sheet = new Texture2D(RenderWidth * 2, RenderHeight * references.Length,
                    TextureFormat.RGB24, false);
                loaded.Add(sheet);
                Color32[] white = Enumerable.Repeat(new Color32(255, 255, 255, 255),
                    sheet.width * sheet.height).ToArray();
                sheet.SetPixels32(white);
                for (int row = 0; row < references.Length; row++)
                {
                    Texture2D reference = LoadImage(references[row], false);
                    Texture2D result = LoadImage(results[row], false);
                    loaded.Add(reference);
                    loaded.Add(result);
                    Texture2D referenceTile = CenterCropAndScale(reference, RenderWidth, RenderHeight);
                    Texture2D resultTile = CenterCropAndScale(result, RenderWidth, RenderHeight);
                    loaded.Add(referenceTile);
                    loaded.Add(resultTile);
                    int y = (references.Length - row - 1) * RenderHeight;
                    sheet.SetPixels32(0, y, RenderWidth, RenderHeight, referenceTile.GetPixels32());
                    sheet.SetPixels32(RenderWidth, y, RenderWidth, RenderHeight, resultTile.GetPixels32());
                }
                sheet.Apply(false, false);
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
            }
            finally
            {
                foreach (Texture2D texture in loaded)
                    if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void ComposeLeftUpperArmGrid(IReadOnlyList<string> renderPaths, string outputPath)
        {
            const int columns = 3;
            const int rows = 2;
            const int tileSize = 512;
            if (renderPaths.Count != columns * rows)
                throw new InvalidOperationException("Left upper arm grid requires six renders.");
            var loaded = new List<Texture2D>();
            try
            {
                var sheet = new Texture2D(columns * tileSize, rows * tileSize,
                    TextureFormat.RGB24, false, false);
                loaded.Add(sheet);
                sheet.SetPixels32(Enumerable.Repeat(new Color32(255, 255, 255, 255),
                    sheet.width * sheet.height).ToArray());
                for (int index = 0; index < renderPaths.Count; index++)
                {
                    Texture2D render = LoadImage(renderPaths[index], false);
                    Texture2D tile = CropLeftUpperArmAndScale(render, tileSize, tileSize);
                    loaded.Add(render);
                    loaded.Add(tile);
                    int column = index % columns;
                    int row = index / columns;
                    sheet.SetPixels32(column * tileSize, (rows - row - 1) * tileSize,
                        tileSize, tileSize, tile.GetPixels32());
                }
                sheet.Apply(false, false);
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
            }
            finally
            {
                foreach (Texture2D texture in loaded)
                    if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static Texture2D CropLeftUpperArmAndScale(Texture2D source, int width, int height)
        {
            Color32[] input = source.GetPixels32();
            int minX = source.width;
            int maxX = -1;
            int minY = source.height;
            int maxY = -1;
            for (int y = 0; y < source.height; y++)
            for (int x = 0; x < source.width; x++)
            {
                Color32 pixel = input[y * source.width + x];
                if (pixel.a < 16 || (pixel.r > 244 && pixel.g > 244 && pixel.b > 244)) continue;
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }
            if (maxX < minX || maxY < minY)
                throw new InvalidOperationException("Rendered transporter foreground was not found.");
            int bodyWidth = maxX - minX + 1;
            int bodyHeight = maxY - minY + 1;
            int cropWidth = Mathf.Clamp(Mathf.RoundToInt(bodyWidth * 0.42f), 32, source.width);
            int cropHeight = Mathf.Clamp(Mathf.RoundToInt(bodyHeight * 0.46f), 32, source.height);
            int centerX = Mathf.RoundToInt(Mathf.Lerp(minX, maxX, 0.72f));
            int centerY = Mathf.RoundToInt(Mathf.Lerp(minY, maxY, 0.68f));
            int startX = Mathf.Clamp(centerX - cropWidth / 2, 0, source.width - cropWidth);
            int startY = Mathf.Clamp(centerY - cropHeight / 2, 0, source.height - cropHeight);
            var output = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                int sourceY = startY + Mathf.Min(cropHeight - 1, y * cropHeight / height);
                for (int x = 0; x < width; x++)
                {
                    int sourceX = startX + Mathf.Min(cropWidth - 1, x * cropWidth / width);
                    output[y * width + x] = input[sourceY * source.width + sourceX];
                }
            }
            var result = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            result.SetPixels32(output);
            result.Apply(false, false);
            return result;
        }

        private static Texture2D CenterCropAndScale(Texture2D source, int width, int height)
        {
            float targetAspect = width / (float)height;
            int cropWidth = source.width;
            int cropHeight = source.height;
            if (source.width / (float)source.height > targetAspect)
                cropWidth = Mathf.RoundToInt(source.height * targetAspect);
            else
                cropHeight = Mathf.RoundToInt(source.width / targetAspect);
            Color32[] input = source.GetPixels32();
            Vector2 foregroundCenter = ForegroundCenter(input, source.width, source.height);
            int startX = Mathf.Clamp(Mathf.RoundToInt(foregroundCenter.x - cropWidth * 0.5f),
                0, source.width - cropWidth);
            int startY = Mathf.Clamp(Mathf.RoundToInt(foregroundCenter.y - cropHeight * 0.5f),
                0, source.height - cropHeight);
            var output = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                int sourceY = startY + Mathf.Min(cropHeight - 1, y * cropHeight / height);
                for (int x = 0; x < width; x++)
                {
                    int sourceX = startX + Mathf.Min(cropWidth - 1, x * cropWidth / width);
                    output[y * width + x] = input[sourceY * source.width + sourceX];
                }
            }
            var result = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            result.SetPixels32(output);
            result.Apply(false, false);
            return result;
        }

        private static Vector2 ForegroundCenter(Color32[] pixels, int width, int height)
        {
            int minX = width;
            int maxX = -1;
            int minY = height;
            int maxY = -1;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Color32 pixel = pixels[y * width + x];
                if (pixel.a < 16 || (pixel.r > 244 && pixel.g > 244 && pixel.b > 244)) continue;
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }
            return maxX < minX || maxY < minY
                ? new Vector2(width * 0.5f, height * 0.5f)
                : new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        }

        private static string ReferencePath(string fileName) =>
            Absolute("image/item/" + fileName);

        private static Dictionary<string, byte[]> ExtractEmbeddedTextures(string fbxPath)
        {
            var result = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            using (var stream = File.OpenRead(fbxPath))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
            {
                byte[] header = reader.ReadBytes(23);
                string signature = Encoding.ASCII.GetString(header);
                if (!signature.StartsWith("Kaydara FBX Binary", StringComparison.Ordinal))
                    throw new InvalidOperationException("Helmet FBX is not binary FBX.");
                uint version = reader.ReadUInt32();
                while (stream.Position < stream.Length)
                {
                    FbxNode node = ReadFbxNode(reader, version);
                    if (node == null) break;
                    CollectEmbeddedTextures(node, result);
                }
            }
            return result;
        }

        private static FbxNode ReadFbxNode(BinaryReader reader, uint version)
        {
            long endOffset = version >= 7500 ? (long)reader.ReadUInt64() : reader.ReadUInt32();
            long propertyCount = version >= 7500 ? (long)reader.ReadUInt64() : reader.ReadUInt32();
            long propertyListLength = version >= 7500 ? (long)reader.ReadUInt64() : reader.ReadUInt32();
            byte nameLength = reader.ReadByte();
            if (endOffset == 0 && propertyCount == 0 && propertyListLength == 0 && nameLength == 0)
                return null;
            string name = Encoding.UTF8.GetString(reader.ReadBytes(nameLength));
            var node = new FbxNode(name);
            for (long index = 0; index < propertyCount; index++)
                node.Properties.Add(ReadFbxProperty(reader));
            int nullRecordSize = version >= 7500 ? 25 : 13;
            while (reader.BaseStream.Position < endOffset)
            {
                long remaining = endOffset - reader.BaseStream.Position;
                if (remaining <= nullRecordSize)
                {
                    reader.BaseStream.Position = endOffset;
                    break;
                }
                FbxNode child = ReadFbxNode(reader, version);
                if (child == null)
                {
                    reader.BaseStream.Position = endOffset;
                    break;
                }
                node.Children.Add(child);
            }
            if (reader.BaseStream.Position != endOffset) reader.BaseStream.Position = endOffset;
            return node;
        }

        private static object ReadFbxProperty(BinaryReader reader)
        {
            char type = (char)reader.ReadByte();
            switch (type)
            {
                case 'Y': return reader.ReadInt16();
                case 'C': return reader.ReadByte() != 0;
                case 'I': return reader.ReadInt32();
                case 'F': return reader.ReadSingle();
                case 'D': return reader.ReadDouble();
                case 'L': return reader.ReadInt64();
                case 'S': return Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
                case 'R': return reader.ReadBytes(reader.ReadInt32());
                case 'f':
                case 'd':
                case 'l':
                case 'i':
                case 'b':
                case 'c':
                    reader.ReadUInt32();
                    reader.ReadUInt32();
                    uint compressedLength = reader.ReadUInt32();
                    reader.BaseStream.Seek(compressedLength, SeekOrigin.Current);
                    return null;
                default:
                    throw new InvalidOperationException("Unsupported FBX property type: " + type);
            }
        }

        private static void CollectEmbeddedTextures(FbxNode node, Dictionary<string, byte[]> result)
        {
            if (node.Name == "Video")
            {
                FbxNode relative = node.Children.FirstOrDefault(item => item.Name == "RelativeFilename");
                FbxNode content = node.Children.FirstOrDefault(item => item.Name == "Content");
                string relativePath = relative?.Properties.FirstOrDefault() as string;
                byte[] bytes = content?.Properties.FirstOrDefault() as byte[];
                if (!string.IsNullOrWhiteSpace(relativePath) && bytes != null && bytes.Length > 0)
                    result[Path.GetFileName(relativePath)] = bytes;
            }
            foreach (FbxNode child in node.Children) CollectEmbeddedTextures(child, result);
        }

        private static void AppendRenderers(StringBuilder report, GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            report.AppendLine("rendererCount=" + renderers.Length);
            foreach (Renderer renderer in renderers)
            {
                report.AppendLine(" renderer=" + HierarchyPath(renderer.transform, root.transform));
                report.AppendLine(" rendererType=" + renderer.GetType().Name);
                report.AppendLine(" enabled=" + renderer.enabled);
                report.AppendLine(" boundsCenter=" + Format(renderer.bounds.center));
                report.AppendLine(" boundsSize=" + Format(renderer.bounds.size));
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    report.AppendLine(" mesh=" + (skinned.sharedMesh == null ? "<null>" : skinned.sharedMesh.name));
                    report.AppendLine(" bones=" + skinned.bones.Length);
                    report.AppendLine(" rootBone=" + (skinned.rootBone == null ? "<null>" : skinned.rootBone.name));
                }
                else if (renderer is MeshRenderer)
                {
                    MeshFilter filter = renderer.GetComponent<MeshFilter>();
                    report.AppendLine(" mesh=" +
                                      (filter == null || filter.sharedMesh == null ? "<null>" : filter.sharedMesh.name));
                }

                Material[] materials = renderer.sharedMaterials;
                report.AppendLine(" materialSlots=" + materials.Length);
                for (int index = 0; index < materials.Length; index++)
                {
                    Material material = materials[index];
                    if (material == null)
                    {
                        report.AppendLine("  material[" + index + "]=<null>");
                        continue;
                    }

                    report.AppendLine("  material[" + index + "]=" + material.name);
                    report.AppendLine("  materialPath=" + AssetDatabase.GetAssetPath(material));
                    report.AppendLine("  shader=" + (material.shader == null ? "<null>" : material.shader.name));
                    foreach (string property in new[] { "_BaseColor", "_Color" })
                    {
                        if (material.HasProperty(property))
                            report.AppendLine("  " + property + "=" + Format(material.GetColor(property)));
                    }
                    foreach (string property in new[] { "_Metallic", "_Smoothness" })
                    {
                        if (material.HasProperty(property))
                            report.AppendLine("  " + property + "=" +
                                              material.GetFloat(property).ToString("F6", CultureInfo.InvariantCulture));
                    }
                    foreach (string property in new[] { "_BaseMap", "_MainTex", "_BumpMap", "_EmissionMap" })
                    {
                        if (!material.HasProperty(property)) continue;
                        Texture texture = material.GetTexture(property);
                        report.AppendLine("  " + property + "=" +
                                          (texture == null ? "<null>" : AssetDatabase.GetAssetPath(texture)));
                    }
                }
            }
        }

        private static void AppendHierarchy(StringBuilder report, Transform current, Transform root, int depth)
        {
            report.AppendLine(new string(' ', depth * 2) + HierarchyPath(current, root) +
                              " pos=" + Format(current.localPosition) +
                              " rot=" + Format(current.localEulerAngles) +
                              " scale=" + Format(current.localScale));
            for (int index = 0; index < current.childCount; index++)
                AppendHierarchy(report, current.GetChild(index), root, depth + 1);
        }

        private static Transform FindHead(GameObject target)
        {
            Animator animator = target.GetComponentInChildren<Animator>(true);
            if (animator != null && animator.isHuman)
            {
                Transform humanoidHead = animator.GetBoneTransform(HumanBodyBones.Head);
                if (humanoidHead != null) return humanoidHead;
            }

            return target.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name.Equals("Head", StringComparison.OrdinalIgnoreCase) ||
                                        item.name.EndsWith(":Head", StringComparison.OrdinalIgnoreCase));
        }

        private static GameObject RequireSceneObject(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform found = root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(item => item.name == objectName);
                if (found != null) return found.gameObject;
            }
            throw new InvalidOperationException("Scene object is missing: " + objectName);
        }

        private static string HierarchyPath(Transform value, Transform root)
        {
            var parts = new List<string>();
            Transform current = value;
            while (current != null)
            {
                parts.Add(current.name);
                if (current == root) break;
                current = current.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string Sha256(string path)
        {
            using (SHA256 hash = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Format(Vector3 value) => string.Format(CultureInfo.InvariantCulture,
            "({0:F6},{1:F6},{2:F6})", value.x, value.y, value.z);

        private static string Format(Vector2 value) => string.Format(CultureInfo.InvariantCulture,
            "({0:F6},{1:F6})", value.x, value.y);

        private static string Format(Color value) => string.Format(CultureInfo.InvariantCulture,
            "({0:F6},{1:F6},{2:F6},{3:F6})", value.r, value.g, value.b, value.a);

        private static string Absolute(string relativePath) =>
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relativePath));

        private readonly struct ArmorVariant
        {
            internal readonly string TargetName;
            internal readonly string ReferenceFileName;
            internal readonly string TextureFileName;

            internal ArmorVariant(string targetName, string referenceFileName, string textureFileName)
            {
                TargetName = targetName;
                ReferenceFileName = referenceFileName;
                TextureFileName = textureFileName;
            }
        }

        private readonly struct Palette
        {
            internal readonly float Hue;
            internal readonly float Saturation;
            internal readonly float Value;

            internal Palette(float hue, float saturation, float value)
            {
                Hue = hue;
                Saturation = saturation;
                Value = value;
            }

            public override string ToString() => string.Format(CultureInfo.InvariantCulture,
                "h={0:F6},s={1:F6},v={2:F6}", Hue, Saturation, Value);
        }

        private readonly struct ExactColorSample
        {
            internal readonly Color32 Rgb;
            internal readonly float Hue;
            internal readonly float Saturation;
            internal readonly float Value;

            internal ExactColorSample(Color32 rgb, float hue, float saturation, float value)
            {
                Rgb = rgb;
                Hue = hue;
                Saturation = saturation;
                Value = value;
            }

            internal static int CompareByValueHueSaturation(ExactColorSample left, ExactColorSample right)
            {
                int value = left.Value.CompareTo(right.Value);
                if (value != 0) return value;
                int hue = left.Hue.CompareTo(right.Hue);
                return hue != 0 ? hue : left.Saturation.CompareTo(right.Saturation);
            }
        }

        private readonly struct PixelComponent
        {
            internal readonly int MinX;
            internal readonly int MinY;
            internal readonly int MaxX;
            internal readonly int MaxY;
            internal readonly int CenterX;
            internal readonly int CenterY;
            internal readonly int[] Pixels;
            internal int PixelCount => Pixels.Length;

            internal PixelComponent(int minX, int minY, int maxX, int maxY,
                int centerX, int centerY, int[] pixels)
            {
                MinX = minX;
                MinY = minY;
                MaxX = maxX;
                MaxY = maxY;
                CenterX = centerX;
                CenterY = centerY;
                Pixels = pixels;
            }
        }

        private readonly struct CleanupRegion
        {
            internal readonly int MinX;
            internal readonly int MinY;
            internal readonly int MaxX;
            internal readonly int MaxY;
            internal readonly int CloneOffsetX;
            internal readonly int CloneOffsetY;
            internal readonly int FlagRedPixelCount;

            internal CleanupRegion(int minX, int minY, int maxX, int maxY,
                int cloneOffsetX, int cloneOffsetY, int flagRedPixelCount)
            {
                MinX = minX;
                MinY = minY;
                MaxX = maxX;
                MaxY = maxY;
                CloneOffsetX = cloneOffsetX;
                CloneOffsetY = cloneOffsetY;
                FlagRedPixelCount = flagRedPixelCount;
            }
        }

        private readonly struct RankedSourcePixel
        {
            internal readonly int Index;
            private readonly float hue;
            private readonly float saturation;
            private readonly float value;

            internal RankedSourcePixel(int index, float hue, float saturation, float value)
            {
                Index = index;
                this.hue = hue;
                this.saturation = saturation;
                this.value = value;
            }

            internal static int CompareByValueHueSaturation(RankedSourcePixel left, RankedSourcePixel right)
            {
                int valueOrder = left.value.CompareTo(right.value);
                if (valueOrder != 0) return valueOrder;
                int hueOrder = left.hue.CompareTo(right.hue);
                return hueOrder != 0 ? hueOrder : left.saturation.CompareTo(right.saturation);
            }
        }

        private readonly struct ColorTransferMetrics
        {
            internal readonly int SourcePixelCount;
            internal readonly int ReferencePixelCount;

            internal ColorTransferMetrics(int sourcePixelCount, int referencePixelCount)
            {
                SourcePixelCount = sourcePixelCount;
                ReferencePixelCount = referencePixelCount;
            }
        }

        private readonly struct HelmetFitMetrics
        {
            internal readonly Bounds BodyBounds;
            internal readonly Bounds InitialHelmetBounds;
            internal readonly Bounds FinalHelmetBounds;
            internal readonly float DesiredHeight;
            internal readonly float ScaleFactor;
            internal readonly Vector3 HeadPosition;
            internal readonly Vector3 LocalPosition;
            internal readonly Vector3 LocalEulerAngles;
            internal readonly Vector3 LocalScale;

            internal HelmetFitMetrics(Bounds bodyBounds, Bounds initialHelmetBounds, Bounds finalHelmetBounds,
                float desiredHeight, float scaleFactor, Vector3 headPosition, Vector3 localPosition,
                Vector3 localEulerAngles, Vector3 localScale)
            {
                BodyBounds = bodyBounds;
                InitialHelmetBounds = initialHelmetBounds;
                FinalHelmetBounds = finalHelmetBounds;
                DesiredHeight = desiredHeight;
                ScaleFactor = scaleFactor;
                HeadPosition = headPosition;
                LocalPosition = localPosition;
                LocalEulerAngles = localEulerAngles;
                LocalScale = localScale;
            }
        }

        private sealed class FbxNode
        {
            internal readonly string Name;
            internal readonly List<object> Properties = new List<object>();
            internal readonly List<FbxNode> Children = new List<FbxNode>();

            internal FbxNode(string name)
            {
                Name = name;
            }
        }
    }
}
