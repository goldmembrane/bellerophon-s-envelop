using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class VacuumCleanerGripTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string ModelPath =
            "Assets/_Project/Art/Items/Vacuum/vacuum cleaner.fbx";
        private const string TextureFolder =
            "Assets/_Project/Art/Items/Vacuum/Textures";
        private const string MaterialFolder =
            "Assets/_Project/Art/Items/Vacuum/Materials";
        private const string MaterialPath =
            MaterialFolder + "/VacuumCleaner.mat";
        private const string EmbeddedMaterialName = "Material.001";
        private const string BaseColorTexturePath = TextureFolder + "/base_color.jpg";
        private const string NormalTexturePath = TextureFolder + "/normal.jpg";
        private const string MetallicTexturePath =
            TextureFolder + "/texture_0_metallic.png";
        private const string RoughnessTexturePath =
            TextureFolder + "/texture_0_roughness.png";
        private const string MetallicSmoothnessTexturePath =
            TextureFolder + "/metallic_smoothness.png";
        private const string OutputFolder =
            "docs/validation/vacuum_cleaner_grip_2026-09-10";
        // Reference-image wrist alignment results are kept under the current date.
        private const string RightWristOutputFolder =
            "docs/validation/vacuum_cleaner_grip_2026-09-11";
        // Stores the user's current Vacuum_Idle placement before the matching
        // local transforms are persisted and applied to Vacuum_Use.
        private const string TransformSyncOutputFolder =
            "docs/validation/vacuum_cleaner_transform_sync_2026-09-11";
        private const string IdleName = "Vacuum_Idle";
        private const string UseName = "Vacuum_Use";
        private const string PlayerIdleName = "Player_Idle";
        private const string HolderName = "VacuumCleaner_Prop";
        private const string ModelInstanceName = "VacuumCleaner_Model";
        private const string RightGripAnchorName = "RightHandleGripAnchor";
        private const string LeftGripAnchorName = "LeftShaftGripAnchor";
        private const string HipsPath = "Armature/Hips";
        private const string SpinePath = "Armature/Hips/Spine02/Spine01/Spine";
        private const string LeftShoulderPath = SpinePath + "/LeftShoulder";
        private const string LeftArmPath = LeftShoulderPath + "/LeftArm";
        private const string LeftForeArmPath = LeftArmPath + "/LeftForeArm";
        private const string LeftHandPath = LeftForeArmPath + "/LeftHand";
        private const string RightShoulderPath = SpinePath + "/RightShoulder";
        private const string RightArmPath = RightShoulderPath + "/RightArm";
        private const string RightForeArmPath = RightArmPath + "/RightForeArm";
        private const string RightHandPath = RightForeArmPath + "/RightHand";
        private const int CaptureWidth = 1800;
        private const int CaptureHeight = 1000;
        private const float PositionTolerance = 0.003f;
        private const float InitialPropHeight = 1.28f;
        private const float LeanDegrees = 40f;
        private const float FloorYLocal = 0f;
        private const float DesiredPropCenterXLocal = 0f;
        private const float DesiredPropCenterZLocal = 0.30f;
        private const float MinimumPropCenterZLocal = 0.12f;
        private const float MaximumPropCenterZLocal = 1.00f;
        private const float PlannedMaximumElbowDegrees = 155f;
        private const float MaximumElbowDegrees = 175f;
        private const float MaximumShoulderTranslation = 0.36f;
        private const float RightGripHeight01 = 0.78f;
        private const float LeftGripHeight01 = 0.40f;
        // Vacuum_Idle uses the black upper arch handle, while the legacy
        // two-pose setup keeps its existing grip definition unchanged.
        private const float VacuumIdleRightGripHeight01 = 0.91f;
        // Source-pose shoulder locations keep Play Mode verification independent of prefab links.
        private static readonly Vector3 LeftShoulderSourceLocalPosition =
            new Vector3(-0.04182303f, 0.0409803651f, 0.004444579f);
        private static readonly Vector3 RightShoulderSourceLocalPosition =
            new Vector3(0.0416255258f, 0.0453975573f, 0.00267655519f);
        private static readonly Vector3 AuthoredIdleHolderLocalPosition =
            new Vector3(0.0003875196f, 0.6533548f, 0.318965316f);
        private static readonly Quaternion AuthoredIdleHolderLocalRotation =
            new Quaternion(-0.342020154f, 0f, 0f, 0.9396927f);
        private static readonly Vector3 AuthoredIdleHolderLocalScale = Vector3.one;
        private static readonly Vector3 AuthoredIdleModelLocalPosition =
            new Vector3(0.057f, -0.367f, 0.298f);
        private static readonly Quaternion AuthoredIdleModelLocalRotation =
            new Quaternion(-0.7071068f, 0f, 0f, 0.7071068f);
        private static readonly Vector3 AuthoredIdleModelLocalScale =
            new Vector3(140f, 140f, 140f);

        internal static void InspectSources()
        {
            RequireEditMode();
            RequireScene();
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Vacuum cleaner FBX is not imported.");
            Bounds modelBounds = CalculateAssetLocalBounds(model);
            var report = new StringBuilder();
            report.AppendLine("Vacuum cleaner source inspection");
            report.AppendLine("modelPath=" + ModelPath);
            report.AppendLine("modelAssetName=" + model.name);
            report.AppendLine("modelLocalBoundsCenter=" + Vec(modelBounds.center));
            report.AppendLine("modelLocalBoundsSize=" + Vec(modelBounds.size));
            report.AppendLine("meshFilters=" +
                model.GetComponentsInChildren<MeshFilter>(true).Length);
            report.AppendLine("skinnedRenderers=" +
                model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length);
            report.AppendLine("renderers=" +
                model.GetComponentsInChildren<Renderer>(true).Length);
            report.AppendLine("materials=" + string.Join(",", model
                .GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .Select(material => material.name)
                .Distinct(StringComparer.Ordinal)));
            report.AppendLine("hierarchy:");
            foreach (Transform transform in model.GetComponentsInChildren<Transform>(true))
            {
                report.AppendLine("- " +
                    AnimationUtility.CalculateTransformPath(transform, model.transform) +
                    " localPosition=" + Vec(transform.localPosition) +
                    " localEuler=" + Vec(transform.localEulerAngles) +
                    " localScale=" + Vec(transform.localScale));
            }

            foreach (string targetName in new[] { IdleName, UseName })
            {
                Transform target = RequireTarget(targetName);
                report.AppendLine("target=" + targetName +
                    " position=" + Vec(target.position) +
                    " forward=" + Vec(target.forward) +
                    " animatorCount=" +
                    target.GetComponentsInChildren<Animator>(true).Length);
                foreach (string path in new[]
                         {
                             HipsPath,
                             SpinePath,
                             LeftShoulderPath,
                             LeftArmPath,
                             LeftForeArmPath,
                             LeftHandPath,
                             RightShoulderPath,
                             RightArmPath,
                             RightForeArmPath,
                             RightHandPath
                         })
                {
                    Transform bone = FindRequired(target, path);
                    report.AppendLine(targetName + " " + path +
                        " localPosition=" + Vec(bone.localPosition) +
                        " localEuler=" + Vec(bone.localEulerAngles) +
                        " worldPosition=" + Vec(bone.position));
                }

                foreach (string handPath in new[] { LeftHandPath, RightHandPath })
                {
                    Transform hand = FindRequired(target, handPath);
                    report.AppendLine(targetName + " descendants of " + handPath + ":");
                    foreach (Transform descendant in
                             hand.GetComponentsInChildren<Transform>(true))
                    {
                        report.AppendLine("  - " +
                            AnimationUtility.CalculateTransformPath(descendant, hand) +
                            " localPosition=" + Vec(descendant.localPosition) +
                            " localEuler=" + Vec(descendant.localEulerAngles));
                    }
                }
            }

            WriteText("source_inspection.txt", report.ToString());
            Debug.Log("[VacuumCleanerGrip] Source inspection completed. " +
                "Bounds=" + Vec(modelBounds.size) + ".");
        }

        internal static void InspectEmbeddedAppearance()
        {
            RequireEditMode();
            RequireScene();
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter ??
                throw new InvalidOperationException("Vacuum cleaner ModelImporter is unavailable.");
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Vacuum cleaner FBX is not imported.");
            var report = new StringBuilder();
            report.AppendLine("Vacuum cleaner embedded appearance inspection");
            report.AppendLine("modelPath=" + ModelPath);
            report.AppendLine("textureDestination=" + TextureFolder);
            report.AppendLine("materialDestination=" + MaterialFolder);
            report.AppendLine("materialImportMode=" + importer.materialImportMode);
            report.AppendLine("materialLocation=" + importer.materialLocation);
            report.AppendLine("externalRemapCount=" + importer.GetExternalObjectMap().Count);
            foreach (var pair in importer.GetExternalObjectMap())
            {
                report.AppendLine("remap=" + pair.Key.type + ":" + pair.Key.name +
                    " -> " + AssetDatabase.GetAssetPath(pair.Value));
            }

            UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(ModelPath);
            report.AppendLine("subAssetCount=" + subAssets.Length);
            foreach (UnityEngine.Object asset in subAssets)
            {
                report.AppendLine("subAsset=" + asset.GetType().Name + ":" + asset.name +
                    " path=" + AssetDatabase.GetAssetPath(asset));
            }

            Material[] materials = model.GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .Distinct()
                .ToArray();
            report.AppendLine("rendererMaterialCount=" + materials.Length);
            foreach (Material material in materials)
            {
                report.AppendLine("material=" + material.name +
                    " shader=" + (material.shader != null ? material.shader.name : "<null>") +
                    " path=" + AssetDatabase.GetAssetPath(material));
                foreach (string property in material.GetTexturePropertyNames())
                {
                    Texture texture = material.GetTexture(property);
                    report.AppendLine("  texture=" + property + " -> " +
                        (texture != null
                            ? texture.name + "|" + texture.GetType().Name + "|" +
                              AssetDatabase.GetAssetPath(texture)
                            : "<null>"));
                }
            }

            report.AppendLine("dependencies:");
            foreach (string dependency in AssetDatabase.GetDependencies(ModelPath, true))
            {
                report.AppendLine("- " + dependency);
            }

            WriteText("embedded_appearance_inspection.txt", report.ToString());
            Debug.Log("[VacuumCleanerGrip] Embedded appearance inspected. " +
                "Materials=" + materials.Length + " subAssets=" + subAssets.Length + ".");
        }

        internal static void CaptureBefore()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            Transform idle = RequireTarget(IdleName);
            Transform use = RequireTarget(UseName);
            Bounds bounds = BoundsOf(idle, use);
            string destination = Absolute(Path.Combine(OutputFolder, "before.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Capture directory is unavailable."));
            CaptureTargets(scene, idle, use, bounds, destination);
            Debug.Log("[VacuumCleanerGrip] Unmodified before view captured. Output=" +
                destination + ".");
        }

        private static Material ExtractAndRemapEmbeddedAppearance()
        {
            Directory.CreateDirectory(Absolute(TextureFolder));
            Directory.CreateDirectory(Absolute(MaterialFolder));
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter ??
                throw new InvalidOperationException("Vacuum cleaner ModelImporter is unavailable.");
            bool extractedTextures = importer.ExtractTextures(Absolute(TextureFolder));
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Material external = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Material embedded = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<Material>()
                .SingleOrDefault();
            Material appearanceSource = embedded != null ? embedded : external;
            if (appearanceSource == null)
            {
                throw new InvalidOperationException(
                    "Vacuum cleaner embedded or previously extracted material is missing.");
            }

            string[] embeddedTexturePaths = appearanceSource.GetTexturePropertyNames()
                .Select(appearanceSource.GetTexture)
                .Where(texture => texture != null)
                .Select(AssetDatabase.GetAssetPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (embeddedTexturePaths.Length == 0 ||
                embeddedTexturePaths.Any(path =>
                    !path.StartsWith(TextureFolder + "/", StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "Vacuum cleaner embedded textures were not extracted into the approved " +
                    "vacuum texture folder. Paths=" + string.Join(",", embeddedTexturePaths) +
                    " extracted=" + extractedTextures + ".");
            }

            foreach (string requiredPath in new[]
                     {
                         BaseColorTexturePath,
                         NormalTexturePath,
                         MetallicTexturePath,
                         RoughnessTexturePath
                     })
            {
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(requiredPath) == null)
                {
                    throw new InvalidOperationException(
                        "Vacuum cleaner embedded texture is missing after extraction: " +
                        requiredPath + ".");
                }
            }

            ConfigureTextureImport(BaseColorTexturePath, false, true);
            ConfigureTextureImport(NormalTexturePath, true, false);
            ConfigureTextureImport(MetallicTexturePath, false, false);
            ConfigureTextureImport(RoughnessTexturePath, false, false);
            BuildMetallicSmoothnessTexture();

            if (external == null)
            {
                external = new Material(appearanceSource) { name = "VacuumCleaner" };
                AssetDatabase.CreateAsset(external, MaterialPath);
            }
            else if (appearanceSource != external)
            {
                EditorUtility.CopySerialized(appearanceSource, external);
                external.name = "VacuumCleaner";
                EditorUtility.SetDirty(external);
            }

            Texture2D metallicSmoothness =
                AssetDatabase.LoadAssetAtPath<Texture2D>(MetallicSmoothnessTexturePath) ??
                throw new InvalidOperationException(
                    "Vacuum cleaner metallic-smoothness compatibility texture is missing.");
            external.SetTexture("_MetallicGlossMap", metallicSmoothness);
            external.SetFloat("_Metallic", 1f);
            external.SetFloat("_Smoothness", 1f);
            external.SetFloat("_SmoothnessTextureChannel", 0f);
            external.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(external);

            AssetDatabase.SaveAssets();
            importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "Vacuum cleaner ModelImporter was lost after texture extraction.");
            importer.AddRemap(
                new AssetImporter.SourceAssetIdentifier(
                    typeof(Material),
                    embedded != null ? embedded.name : EmbeddedMaterialName),
                external);
            importer.SaveAndReimport();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            GameObject remappedModel = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException(
                    "Vacuum cleaner FBX was lost after material remapping.");
            string[] materialPaths = remappedModel.GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .Select(AssetDatabase.GetAssetPath)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (materialPaths.Length != 1 || materialPaths[0] != MaterialPath)
            {
                throw new InvalidOperationException(
                    "Vacuum cleaner external material remap failed. Paths=" +
                    string.Join(",", materialPaths) + ".");
            }

            var report = new StringBuilder();
            report.AppendLine("Vacuum cleaner embedded appearance application");
            report.AppendLine("texturesExtracted=" + extractedTextures);
            report.AppendLine("materialPath=" + MaterialPath);
            foreach (string texturePath in embeddedTexturePaths)
            {
                report.AppendLine("texturePath=" + texturePath);
            }

            report.AppendLine("metallicSource=" + MetallicTexturePath);
            report.AppendLine("roughnessSource=" + RoughnessTexturePath);
            report.AppendLine("metallicSmoothnessCompatibility=" +
                MetallicSmoothnessTexturePath);
            report.AppendLine("normalMapImport=True");

            report.AppendLine("sourceFbxShapePivotBytesModified=False");
            WriteText("appearance_application.txt", report.ToString());
            return AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) ??
                throw new InvalidOperationException("Vacuum cleaner external material is missing.");
        }

        private static void ConfigureTextureImport(
            string path,
            bool normalMap,
            bool sRgb)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter ??
                throw new InvalidOperationException("TextureImporter is unavailable: " + path + ".");
            importer.textureType = normalMap
                ? TextureImporterType.NormalMap
                : TextureImporterType.Default;
            importer.sRGBTexture = sRgb;
            importer.SaveAndReimport();
        }

        private static void BuildMetallicSmoothnessTexture()
        {
            Texture2D metallic = LoadSourceTexture(MetallicTexturePath);
            Texture2D roughness = LoadSourceTexture(RoughnessTexturePath);
            Texture2D packed = null;
            try
            {
                if (metallic.width != roughness.width || metallic.height != roughness.height)
                {
                    throw new InvalidOperationException(
                        "Vacuum cleaner metallic and roughness texture sizes differ.");
                }

                Color32[] metallicPixels = metallic.GetPixels32();
                Color32[] roughnessPixels = roughness.GetPixels32();
                var packedPixels = new Color32[metallicPixels.Length];
                for (int index = 0; index < packedPixels.Length; index++)
                {
                    byte metallicValue = metallicPixels[index].r;
                    byte smoothnessValue = (byte)(255 - roughnessPixels[index].r);
                    packedPixels[index] = new Color32(
                        metallicValue,
                        metallicValue,
                        metallicValue,
                        smoothnessValue);
                }

                packed = new Texture2D(
                    metallic.width,
                    metallic.height,
                    TextureFormat.RGBA32,
                    false,
                    true);
                packed.SetPixels32(packedPixels);
                packed.Apply(false, false);
                File.WriteAllBytes(
                    Absolute(MetallicSmoothnessTexturePath),
                    packed.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(metallic);
                UnityEngine.Object.DestroyImmediate(roughness);
                if (packed != null) UnityEngine.Object.DestroyImmediate(packed);
            }

            AssetDatabase.ImportAsset(
                MetallicSmoothnessTexturePath,
                ImportAssetOptions.ForceUpdate);
            ConfigureTextureImport(MetallicSmoothnessTexturePath, false, false);
        }

        private static Texture2D LoadSourceTexture(string assetPath)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            if (!texture.LoadImage(File.ReadAllBytes(Absolute(assetPath)), false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException(
                    "Vacuum cleaner source texture decoding failed: " + assetPath + ".");
            }

            return texture;
        }

        internal static void Apply()
        {
            RequireEditMode();
            Material appearance = ExtractAndRemapEmbeddedAppearance();
            ApplySceneGrip(appearance);
        }

        internal static void ApplyFloorContactDiagonalGrip()
        {
            RequireEditMode();
            ApplyPlayerIdleArmPoseWithoutMovingVacuum();
        }

        internal static void CaptureVacuumIdleAuthoredTransform()
        {
            RequireEditMode();
            RequireScene();
            Transform target = RequireTarget(IdleName);
            Transform holder = RequireExistingVacuum(target);
            Transform model = holder.Find(ModelInstanceName) ??
                throw new InvalidOperationException(IdleName + " has no vacuum model.");
            Transform rightAnchor = holder.Find(RightGripAnchorName) ??
                throw new InvalidOperationException(IdleName + " has no right grip anchor.");
            Bounds bounds = BoundsInTargetSpace(target, holder);
            string report =
                "Vacuum_Idle authored transform\n" +
                "holderLocalPosition=" + Vec(holder.localPosition) + "\n" +
                "holderLocalRotation=" + Quat(holder.localRotation) + "\n" +
                "holderLocalEuler=" + Vec(holder.localEulerAngles) + "\n" +
                "holderLocalScale=" + Vec(holder.localScale) + "\n" +
                "modelLocalPosition=" + Vec(model.localPosition) + "\n" +
                "modelLocalRotation=" + Quat(model.localRotation) + "\n" +
                "modelLocalEuler=" + Vec(model.localEulerAngles) + "\n" +
                "modelLocalScale=" + Vec(model.localScale) + "\n" +
                "rightAnchorLocalPosition=" + Vec(rightAnchor.localPosition) + "\n" +
                "boundsCenterInTarget=" + Vec(bounds.center) + "\n" +
                "boundsSizeInTarget=" + Vec(bounds.size) + "\n" +
                "sceneModified=False\n";
            WriteText("vacuum_idle_authored_transform.txt", report);
            Debug.Log("[VacuumCleanerGrip] Captured current Vacuum_Idle authored " +
                "transform without modification. " + report.Replace('\n', ' '));
        }

        internal static void InspectVacuumCleanerTransformSyncSource()
        {
            RequireEditMode();
            RequireScene();
            Transform idle = RequireTarget(IdleName);
            Transform use = RequireTarget(UseName);
            Transform idleHolder = RequireExistingVacuum(idle);
            Transform useHolder = RequireExistingVacuum(use);
            Transform idleModel = idleHolder.Find(ModelInstanceName) ??
                throw new InvalidOperationException(IdleName + " has no vacuum model.");
            Transform useModel = useHolder.Find(ModelInstanceName) ??
                throw new InvalidOperationException(UseName + " has no vacuum model.");
            string report =
                "Vacuum cleaner transform-sync source inspection\n" +
                "verificationTargetManipulated=False\n" +
                "idleHolderLocalPosition=" + Vec(idleHolder.localPosition) + "\n" +
                "idleHolderLocalRotation=" + Quat(idleHolder.localRotation) + "\n" +
                "idleHolderLocalScale=" + Vec(idleHolder.localScale) + "\n" +
                "idleModelLocalPosition=" + Vec(idleModel.localPosition) + "\n" +
                "idleModelLocalRotation=" + Quat(idleModel.localRotation) + "\n" +
                "idleModelLocalScale=" + Vec(idleModel.localScale) + "\n" +
                "useHolderLocalPosition=" + Vec(useHolder.localPosition) + "\n" +
                "useHolderLocalRotation=" + Quat(useHolder.localRotation) + "\n" +
                "useHolderLocalScale=" + Vec(useHolder.localScale) + "\n" +
                "useModelLocalPosition=" + Vec(useModel.localPosition) + "\n" +
                "useModelLocalRotation=" + Quat(useModel.localRotation) + "\n" +
                "useModelLocalScale=" + Vec(useModel.localScale) + "\n";
            WriteText(
                TransformSyncOutputFolder,
                "source_before.txt",
                report);
            Debug.Log(
                "[VacuumCleanerGrip] Current Vacuum_Idle cleaner local transforms " +
                "inspected without modification. " + report.Replace('\n', ' '));
        }

        internal static void ApplyVacuumCleanerTransformSyncToUse()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            Transform idle = RequireTarget(IdleName);
            Transform use = RequireTarget(UseName);
            Transform idleHolder = RequireExistingVacuum(idle);
            Transform useHolder = RequireExistingVacuum(use);
            Transform idleModel = idleHolder.Find(ModelInstanceName) ??
                throw new InvalidOperationException(IdleName + " has no vacuum model.");
            Transform useModel = useHolder.Find(ModelInstanceName) ??
                throw new InvalidOperationException(UseName + " has no vacuum model.");
            RequireAuthoredVacuumIdleTransform(idleHolder, idleModel);
            string idleBefore = BuildTargetStateSignature(idle, false);
            string useProtectedBefore =
                BuildTargetStateSignatureIgnoringVacuumTransformValues(use);
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Match Vacuum_Use cleaner transform to Vacuum_Idle");
            try
            {
                Undo.RecordObjects(
                    new UnityEngine.Object[] { useHolder, useModel },
                    "Apply Vacuum_Idle cleaner local transforms to Vacuum_Use");
                useHolder.localPosition = AuthoredIdleHolderLocalPosition;
                useHolder.localRotation = AuthoredIdleHolderLocalRotation;
                useHolder.localScale = AuthoredIdleHolderLocalScale;
                useModel.localPosition = AuthoredIdleModelLocalPosition;
                useModel.localRotation = AuthoredIdleModelLocalRotation;
                useModel.localScale = AuthoredIdleModelLocalScale;
                EditorUtility.SetDirty(useHolder);
                EditorUtility.SetDirty(useModel);
                PrefabUtility.RecordPrefabInstancePropertyModifications(useHolder);
                PrefabUtility.RecordPrefabInstancePropertyModifications(useModel);

                RequireVacuumCleanerTransformsMatch(
                    idleHolder,
                    idleModel,
                    useHolder,
                    useModel);
                if (!string.Equals(
                        idleBefore,
                        BuildTargetStateSignature(idle, false),
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Vacuum_Idle changed during cleaner transform synchronization.");
                }

                if (!string.Equals(
                        useProtectedBefore,
                        BuildTargetStateSignatureIgnoringVacuumTransformValues(use),
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Vacuum_Use state outside the cleaner local transforms changed.");
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException("CargoRunMvp scene save failed.");
                }

                WriteText(
                    TransformSyncOutputFolder,
                    "application.txt",
                    BuildVacuumCleanerTransformSyncReport(
                        "Vacuum cleaner transform synchronization application",
                        idleHolder,
                        idleModel,
                        useHolder,
                        useModel) +
                    "Vacuum_IdleChanged=False\n" +
                    "Vacuum_UseNonTransformStateChanged=False\n" +
                    "rendererMeshMaterialChanged=False\n" +
                    "sceneSaved=True\n");
                Undo.CollapseUndoOperations(undoGroup);
                Debug.Log(
                    "[VacuumCleanerGrip] Vacuum_Use cleaner local transforms now " +
                    "match the unchanged Vacuum_Idle authored values.");
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
        }

        internal static void InspectVacuumCleanerTransformSync()
        {
            RequireEditMode();
            RequireScene();
            Transform idle = RequireTarget(IdleName);
            Transform use = RequireTarget(UseName);
            Transform idleHolder = RequireExistingVacuum(idle);
            Transform useHolder = RequireExistingVacuum(use);
            Transform idleModel = idleHolder.Find(ModelInstanceName) ??
                throw new InvalidOperationException(IdleName + " has no vacuum model.");
            Transform useModel = useHolder.Find(ModelInstanceName) ??
                throw new InvalidOperationException(UseName + " has no vacuum model.");
            RequireAuthoredVacuumIdleTransform(idleHolder, idleModel);
            RequireVacuumCleanerTransformsMatch(
                idleHolder,
                idleModel,
                useHolder,
                useModel);
            UnityConsoleDiagnostics.AssertNoErrors();
            WriteText(
                TransformSyncOutputFolder,
                "inspection.txt",
                BuildVacuumCleanerTransformSyncReport(
                    "Vacuum cleaner transform synchronization inspection",
                    idleHolder,
                    idleModel,
                    useHolder,
                        useModel) +
                "verificationTargetManipulated=False\n" +
                "exactLocalTransformMatch=True\n" +
                "unityConsoleErrors=0\n");
            Debug.Log(
                "[VacuumCleanerGrip] Vacuum cleaner local transforms inspected " +
                "read-only and match exactly.");
        }

        internal static void CaptureVacuumCleanerTransformSyncDiagnostic()
        {
            CaptureVacuumCleanerTransformSync("diagnostic.png");
        }

        internal static void CaptureVacuumCleanerTransformSyncFinal()
        {
            CaptureVacuumCleanerTransformSync("final.png");
        }

        internal static void ApplyVacuumIdleRightHandGrip()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            Transform source = RequireTarget(PlayerIdleName);
            Transform idle = RequireTarget(IdleName);
            Transform use = RequireTarget(UseName);
            Transform holder = RequireExistingVacuum(idle);
            Transform model = holder.Find(ModelInstanceName) ??
                throw new InvalidOperationException(IdleName + " has no vacuum model.");
            Transform rightAnchor = holder.Find(RightGripAnchorName) ??
                throw new InvalidOperationException(IdleName + " has no right grip anchor.");
            Transform leftAnchor = holder.Find(LeftGripAnchorName) ??
                throw new InvalidOperationException(IdleName + " has no left grip anchor.");
            var holderBefore = new LocalTransformState(holder);
            var modelBefore = new TransformHierarchyState(model);
            var leftAnchorBefore = new LocalTransformState(leftAnchor);
            var leftArmBefore = new TransformHierarchyState(
                FindRequired(idle, LeftShoulderPath));
            var useBefore = new TransformHierarchyState(use);
            var idleProtectedBefore = new TargetProtectionState(idle);
            var useProtectedBefore = new TargetProtectionState(use);
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply Vacuum_Idle right-hand handle grip");
            try
            {
                RequireAuthoredVacuumIdleTransform(holder, model);
                ApplyAuthoredVacuumIdleTransform(holder, model);
                CopyPlayerIdleArmPose(source, idle, RightShoulderPath);
                Bounds localBounds = CalculateVertexBoundsInSpace(holder, holder);
                Vector3 rightGripLocal = GripPointLocal(
                    holder,
                    localBounds,
                    VacuumIdleRightGripHeight01,
                    0.34f);
                Undo.RecordObject(rightAnchor, "Update Vacuum_Idle right grip anchor");
                rightAnchor.localPosition = rightGripLocal;
                rightAnchor.localRotation = Quaternion.identity;
                EditorUtility.SetDirty(rightAnchor);
                PoseArmAtGrip(
                    idle,
                    true,
                    rightAnchor.position,
                    idle.TransformPoint(new Vector3(0.46f, 1.20f, 0.25f)),
                    holder.forward);

                RequireAuthoredVacuumIdleTransform(holder, model);
                RequireVacuumIdleRightHandGrip(source, idle);
                if (!holderBefore.Matches(holder) ||
                    !modelBefore.Matches(model) ||
                    !leftAnchorBefore.Matches(leftAnchor) ||
                    !leftArmBefore.Matches(FindRequired(idle, LeftShoulderPath)) ||
                    !useBefore.Matches(use))
                {
                    throw new InvalidOperationException(
                        "A protected vacuum, left-arm, or Vacuum_Use transform changed.");
                }

                idleProtectedBefore.RequireProtectedStateUnchanged(idle);
                useProtectedBefore.RequireProtectedStateUnchanged(use);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException("CargoRunMvp scene save failed.");
                }

                AssetDatabase.SaveAssets();
                float palmDistance = Vector3.Distance(
                    CalculateHandPalmCenter(idle, FindRequired(idle, RightHandPath)),
                    rightAnchor.position);
                string report =
                    "Vacuum_Idle right-hand grip application\n" +
                    "authoredVacuumTransformApplied=True\n" +
                    "vacuumTransformChangedFromUserState=False\n" +
                    "rightPalmDistance=" + Num(palmDistance) + "\n" +
                    "leftArmChanged=False\n" +
                    "Vacuum_UseChanged=False\n";
                WriteText("vacuum_idle_right_hand_application.txt", report);
                Undo.CollapseUndoOperations(undoGroup);
                Debug.Log("[VacuumCleanerGrip] Vacuum_Idle right hand posed on " +
                    "the current handle without changing the authored vacuum transform. " +
                    "rightPalmDistance=" + Num(palmDistance) + ".");
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
        }

        internal static void InspectVacuumIdleRightHandGrip()
        {
            RequireScene();
            Transform source = RequireTarget(PlayerIdleName);
            Transform idle = RequireTarget(IdleName);
            Transform holder = RequireExistingVacuum(idle);
            Transform model = holder.Find(ModelInstanceName) ??
                throw new InvalidOperationException(IdleName + " has no vacuum model.");
            RequireAuthoredVacuumIdleTransform(holder, model);
            float palmDistance = RequireVacuumIdleRightHandGrip(source, idle);
            WriteText("vacuum_idle_right_hand_inspection.txt",
                "Vacuum_Idle right-hand grip inspection\n" +
                "authoredVacuumTransformMatches=True\n" +
                "rightPalmDistance=" + Num(palmDistance) + "\n" +
                "leftArmMatchesPlayerIdle=True\n" +
                "verificationTargetManipulated=False\n");
            Debug.Log("[VacuumCleanerGrip] Read-only Vacuum_Idle right-hand " +
                "inspection passed. rightPalmDistance=" + Num(palmDistance) + ".");
        }

        internal static void CaptureVacuumIdleRightHandGripDiagnostic()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            Transform source = RequireTarget(PlayerIdleName);
            Transform idle = RequireTarget(IdleName);
            Transform holder = RequireExistingVacuum(idle);
            Transform model = holder.Find(ModelInstanceName) ??
                throw new InvalidOperationException(IdleName + " has no vacuum model.");
            RequireAuthoredVacuumIdleTransform(holder, model);
            RequireVacuumIdleRightHandGrip(source, idle);
            string destination = Absolute(Path.Combine(
                OutputFolder,
                "vacuum_idle_right_hand_final.png"));
            Texture2D side = null;
            Texture2D front = null;
            Texture2D closeFront = null;
            Texture2D closeSide = null;
            Texture2D composite = null;
            try
            {
                side = RenderPanel(scene, BoundsOf(idle), idle.right, 800, 1200);
                front = RenderPanel(scene, BoundsOf(idle), idle.forward, 800, 1200);
                closeFront = RenderGripClosePanel(scene, idle, 800, 1200);
                closeSide = RenderOrthographicPanel(
                    scene,
                    idle.TransformPoint(new Vector3(0f, 1.02f, 0.25f)),
                    idle.right,
                    1.2f,
                    0.64f,
                    800,
                    1200);
                composite = new Texture2D(3200, 1200, TextureFormat.RGB24, false);
                composite.SetPixels32(0, 0, 800, 1200, side.GetPixels32());
                composite.SetPixels32(800, 0, 800, 1200, front.GetPixels32());
                composite.SetPixels32(1600, 0, 800, 1200, closeFront.GetPixels32());
                composite.SetPixels32(2400, 0, 800, 1200, closeSide.GetPixels32());
                composite.Apply(false, false);
                File.WriteAllBytes(destination, composite.EncodeToPNG());
            }
            finally
            {
                if (side != null) UnityEngine.Object.DestroyImmediate(side);
                if (front != null) UnityEngine.Object.DestroyImmediate(front);
                if (closeFront != null) UnityEngine.Object.DestroyImmediate(closeFront);
                if (closeSide != null) UnityEngine.Object.DestroyImmediate(closeSide);
                if (composite != null) UnityEngine.Object.DestroyImmediate(composite);
            }

            Debug.Log("[VacuumCleanerGrip] Vacuum_Idle right-hand direct-review " +
                "composite captured once. Output=" + destination + ".");
        }

        internal static void ApplyVacuumIdleStateToUse()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            Transform idle = RequireTarget(IdleName);
            Transform use = RequireTarget(UseName);
            var usePlacement = new RootScenePlacementState(use);
            string idleBefore = BuildTargetStateSignature(idle, false);
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Copy Vacuum_Idle state to Vacuum_Use");
            try
            {
                GameObject replacement = UnityEngine.Object.Instantiate(idle.gameObject);
                Undo.RegisterCreatedObjectUndo(
                    replacement,
                    "Create Vacuum_Use from Vacuum_Idle");
                replacement.name = UseName + "_Replacement";
                Transform replacementTransform = replacement.transform;
                if (usePlacement.Parent == null)
                {
                    SceneManager.MoveGameObjectToScene(replacement, scene);
                }
                else
                {
                    replacementTransform.SetParent(usePlacement.Parent, false);
                }

                usePlacement.Apply(replacementTransform);
                Undo.DestroyObjectImmediate(use.gameObject);
                replacement.name = UseName;
                replacementTransform.SetSiblingIndex(usePlacement.SiblingIndex);
                EditorUtility.SetDirty(replacement);

                Transform copiedUse = RequireTarget(UseName);
                if (!usePlacement.Matches(copiedUse))
                {
                    throw new InvalidOperationException(
                        "Vacuum_Use root scene placement changed during state copy.");
                }

                StateCopyMetrics metrics = RequireVacuumIdleStateMatchesUse(
                    idle,
                    copiedUse);
                if (!string.Equals(
                        idleBefore,
                        BuildTargetStateSignature(idle, false),
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Vacuum_Idle changed while copying its state.");
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException("CargoRunMvp scene save failed.");
                }

                AssetDatabase.SaveAssets();
                WriteText(
                    "vacuum_idle_state_to_use_application.txt",
                    "Vacuum_Idle state copied to Vacuum_Use\n" +
                    "Vacuum_IdleChanged=False\n" +
                    "Vacuum_UseRootPlacementPreserved=True\n" +
                    "Vacuum_UseRootLocalPosition=" +
                    Vec(copiedUse.localPosition) + "\n" +
                    "Vacuum_UseRootLocalRotation=" +
                    Quat(copiedUse.localRotation) + "\n" +
                    "Vacuum_UseRootLocalScale=" + Vec(copiedUse.localScale) + "\n" +
                    "matchingTransforms=" + metrics.TransformCount + "\n" +
                    "matchingComponents=" + metrics.ComponentCount + "\n" +
                    "matchingGameObjects=" + metrics.GameObjectCount + "\n");
                Undo.CollapseUndoOperations(undoGroup);
                Debug.Log(
                    "[VacuumCleanerGrip] Vacuum_Idle complete state copied to " +
                    "Vacuum_Use while preserving its root scene placement. " +
                    metrics.Describe() + ".");
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
        }

        internal static void InspectVacuumIdleStateToUse()
        {
            RequireEditMode();
            RequireScene();
            Transform idle = RequireTarget(IdleName);
            Transform use = RequireTarget(UseName);
            StateCopyMetrics metrics = RequireVacuumIdleStateMatchesUse(idle, use);
            WriteText(
                "vacuum_idle_state_to_use_inspection.txt",
                "Vacuum_Idle and Vacuum_Use state inspection\n" +
                "subordinateStateMatches=True\n" +
                "verificationTargetManipulated=False\n" +
                "Vacuum_UseRootLocalPosition=" + Vec(use.localPosition) + "\n" +
                "Vacuum_UseRootLocalRotation=" + Quat(use.localRotation) + "\n" +
                "Vacuum_UseRootLocalScale=" + Vec(use.localScale) + "\n" +
                "matchingTransforms=" + metrics.TransformCount + "\n" +
                "matchingComponents=" + metrics.ComponentCount + "\n" +
                "matchingGameObjects=" + metrics.GameObjectCount + "\n");
            Debug.Log(
                "[VacuumCleanerGrip] Read-only Vacuum_Idle to Vacuum_Use state " +
                "inspection passed. " + metrics.Describe() + ".");
        }

        internal static void CaptureVacuumIdleStateToUse()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            Transform idle = RequireTarget(IdleName);
            Transform use = RequireTarget(UseName);
            RequireVacuumIdleStateMatchesUse(idle, use);
            string destination = Absolute(Path.Combine(
                OutputFolder,
                "vacuum_idle_state_to_use_final.png"));
            Texture2D idleFront = null;
            Texture2D useFront = null;
            Texture2D idleSideClose = null;
            Texture2D useSideClose = null;
            Texture2D composite = null;
            try
            {
                Bounds idleBounds = BoundsOf(idle);
                Bounds useBounds = BoundsOf(use);
                float aspect = 800f / 1200f;
                float fullSize = Mathf.Max(
                    Mathf.Max(idleBounds.extents.y, idleBounds.extents.x / aspect),
                    Mathf.Max(useBounds.extents.y, useBounds.extents.x / aspect)) *
                    1.16f;
                idleFront = RenderOrthographicPanel(
                    scene,
                    idleBounds.center,
                    idle.forward,
                    2.5f,
                    fullSize,
                    800,
                    1200);
                useFront = RenderOrthographicPanel(
                    scene,
                    useBounds.center,
                    use.forward,
                    2.5f,
                    fullSize,
                    800,
                    1200);
                idleSideClose = RenderOrthographicPanel(
                    scene,
                    idle.TransformPoint(new Vector3(0f, 1.02f, 0.25f)),
                    idle.right,
                    1.2f,
                    0.64f,
                    800,
                    1200);
                useSideClose = RenderOrthographicPanel(
                    scene,
                    use.TransformPoint(new Vector3(0f, 1.02f, 0.25f)),
                    use.right,
                    1.2f,
                    0.64f,
                    800,
                    1200);
                composite = new Texture2D(3200, 1200, TextureFormat.RGB24, false);
                composite.SetPixels32(0, 0, 800, 1200, idleFront.GetPixels32());
                composite.SetPixels32(800, 0, 800, 1200, useFront.GetPixels32());
                composite.SetPixels32(
                    1600,
                    0,
                    800,
                    1200,
                    idleSideClose.GetPixels32());
                composite.SetPixels32(
                    2400,
                    0,
                    800,
                    1200,
                    useSideClose.GetPixels32());
                composite.Apply(false, false);
                File.WriteAllBytes(destination, composite.EncodeToPNG());
            }
            finally
            {
                if (idleFront != null) UnityEngine.Object.DestroyImmediate(idleFront);
                if (useFront != null) UnityEngine.Object.DestroyImmediate(useFront);
                if (idleSideClose != null)
                    UnityEngine.Object.DestroyImmediate(idleSideClose);
                if (useSideClose != null)
                    UnityEngine.Object.DestroyImmediate(useSideClose);
                if (composite != null) UnityEngine.Object.DestroyImmediate(composite);
            }

            Debug.Log(
                "[VacuumCleanerGrip] Vacuum_Idle and Vacuum_Use matching-state " +
                "direct comparison captured once. Output=" + destination + ".");
        }

        private static StateCopyMetrics RequireVacuumIdleStateMatchesUse(
            Transform idle,
            Transform use)
        {
            Dictionary<string, Transform> idleTransforms = idle
                .GetComponentsInChildren<Transform>(true)
                .ToDictionary(
                    transform => AnimationUtility.CalculateTransformPath(
                        transform,
                        idle),
                    StringComparer.Ordinal);
            Dictionary<string, Transform> useTransforms = use
                .GetComponentsInChildren<Transform>(true)
                .ToDictionary(
                    transform => AnimationUtility.CalculateTransformPath(
                        transform,
                        use),
                    StringComparer.Ordinal);
            if (idleTransforms.Count != useTransforms.Count ||
                !idleTransforms.Keys.OrderBy(path => path, StringComparer.Ordinal)
                    .SequenceEqual(
                        useTransforms.Keys.OrderBy(
                            path => path,
                            StringComparer.Ordinal),
                        StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Vacuum_Idle and Vacuum_Use hierarchy paths differ.");
            }

            int componentCount = 0;
            foreach (string path in idleTransforms.Keys.OrderBy(
                         value => value,
                         StringComparer.Ordinal))
            {
                Transform idleTransform = idleTransforms[path];
                Transform useTransform = useTransforms[path];
                if (!string.IsNullOrEmpty(path) &&
                    (idleTransform.localPosition != useTransform.localPosition ||
                     idleTransform.localRotation != useTransform.localRotation ||
                     idleTransform.localScale != useTransform.localScale))
                {
                    throw new InvalidOperationException(
                        "Vacuum state transform differs at " + path + ".");
                }

                GameObject idleObject = idleTransform.gameObject;
                GameObject useObject = useTransform.gameObject;
                if (idleObject.activeSelf != useObject.activeSelf ||
                    idleObject.layer != useObject.layer ||
                    idleObject.tag != useObject.tag ||
                    GameObjectUtility.GetStaticEditorFlags(idleObject) !=
                    GameObjectUtility.GetStaticEditorFlags(useObject))
                {
                    throw new InvalidOperationException(
                        "Vacuum state GameObject settings differ at " + path + ".");
                }

                Component[] idleComponents = idleObject.GetComponents<Component>();
                Component[] useComponents = useObject.GetComponents<Component>();
                if (idleComponents.Length != useComponents.Length)
                {
                    throw new InvalidOperationException(
                        "Vacuum component count differs at " + path + ".");
                }

                for (int index = 0; index < idleComponents.Length; index++)
                {
                    Component idleComponent = idleComponents[index];
                    Component useComponent = useComponents[index];
                    Type idleType = idleComponent != null
                        ? idleComponent.GetType()
                        : null;
                    Type useType = useComponent != null
                        ? useComponent.GetType()
                        : null;
                    if (idleType != useType)
                    {
                        throw new InvalidOperationException(
                            "Vacuum component type differs at " + path +
                            " index " + index + ".");
                    }

                    if (idleComponent is Transform)
                    {
                        continue;
                    }

                    string idleState = ComparableComponentState(idleComponent);
                    string useState = ComparableComponentState(useComponent);
                    if (!string.Equals(idleState, useState, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            "Vacuum component settings differ at " + path +
                            " component " + idleType?.FullName + ".");
                    }

                    componentCount++;
                }
            }

            return new StateCopyMetrics(
                idleTransforms.Count,
                componentCount,
                idleTransforms.Count);
        }

        private static string ComparableComponentState(Component component)
        {
            if (component == null)
            {
                return "<missing-script>";
            }

            string json = Regex.Replace(
                EditorJsonUtility.ToJson(component),
                "\\\"instanceID\\\"\\s*:\\s*-?\\d+",
                "\"instanceID\":0");
            var result = new StringBuilder();
            result.Append(component.GetType().AssemblyQualifiedName)
                .Append('|')
                .Append(json);
            if (component is Behaviour behaviour)
            {
                result.Append("|enabled=").Append(behaviour.enabled);
            }

            if (component is Animator animator)
            {
                result.Append("|controller=")
                    .Append(AssetDatabase.GetAssetPath(
                        animator.runtimeAnimatorController))
                    .Append("|avatar=")
                    .Append(AssetDatabase.GetAssetPath(animator.avatar))
                    .Append("|rootMotion=")
                    .Append(animator.applyRootMotion)
                    .Append("|updateMode=")
                    .Append(animator.updateMode)
                    .Append("|cullingMode=")
                    .Append(animator.cullingMode);
            }

            if (component is Renderer renderer)
            {
                result.Append("|materials=").Append(string.Join(
                    ",",
                    renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath)))
                    .Append("|shadowCasting=")
                    .Append(renderer.shadowCastingMode)
                    .Append("|receiveShadows=")
                    .Append(renderer.receiveShadows);
            }

            if (component is SkinnedMeshRenderer skinned)
            {
                result.Append("|mesh=")
                    .Append(AssetDatabase.GetAssetPath(skinned.sharedMesh))
                    .Append("|updateWhenOffscreen=")
                    .Append(skinned.updateWhenOffscreen);
            }
            else if (component is MeshFilter meshFilter)
            {
                result.Append("|mesh=")
                    .Append(AssetDatabase.GetAssetPath(meshFilter.sharedMesh));
            }

            return result.ToString();
        }

        private static string BuildTargetStateSignature(
            Transform root,
            bool ignoreRootPlacement)
        {
            var result = new StringBuilder();
            foreach (Transform transform in root
                         .GetComponentsInChildren<Transform>(true)
                         .OrderBy(
                             value => AnimationUtility.CalculateTransformPath(
                                 value,
                                 root),
                             StringComparer.Ordinal))
            {
                string path = AnimationUtility.CalculateTransformPath(transform, root);
                result.Append(path)
                    .Append('|')
                    .Append(transform.gameObject.activeSelf)
                    .Append('|')
                    .Append(transform.gameObject.layer)
                    .Append('|')
                    .Append(transform.gameObject.tag)
                    .Append('|')
                    .Append(GameObjectUtility.GetStaticEditorFlags(transform.gameObject));
                if (!ignoreRootPlacement || !string.IsNullOrEmpty(path))
                {
                    result.Append('|').Append(Vec(transform.localPosition))
                        .Append('|').Append(Quat(transform.localRotation))
                        .Append('|').Append(Vec(transform.localScale));
                }

                foreach (Component component in transform.gameObject
                             .GetComponents<Component>())
                {
                    if (component is Transform) continue;
                    result.Append('|').Append(ComparableComponentState(component));
                }

                result.AppendLine();
            }

            return result.ToString();
        }

        internal static void InspectVacuumTopHandleGeometry()
        {
            RequireEditMode();
            RequireScene();
            var report = new StringBuilder();
            report.AppendLine("Vacuum top-handle geometry inspection");
            report.AppendLine("referenceGrip=C:/Users/gus68/OneDrive/바탕 화면/1111.jfif");
            report.AppendLine("referenceLocation=C:/Users/gus68/OneDrive/바탕 화면/33333.png");
            report.AppendLine("targetDefinition=middle of the long straight upper black handle");
            report.AppendLine("excludedLocations=handle start,bend,canister connector");
            report.AppendLine("verificationTargetManipulated=False");
            foreach (string targetName in new[] { IdleName, UseName })
            {
                Transform target = RequireTarget(targetName);
                Transform holder = RequireExistingVacuum(target);
                Transform anchor = holder.Find(RightGripAnchorName) ??
                    throw new InvalidOperationException(
                        targetName + " has no right grip anchor.");
                Bounds bounds = CalculateVertexBoundsInSpace(holder, holder);
                float targetY = bounds.min.y +
                    bounds.size.y * VacuumIdleRightGripHeight01;
                float targetX = bounds.center.x + bounds.size.x * 0.34f;
                Bounds focusedSlice = CalculateGripSliceBounds(
                    holder,
                    targetX,
                    targetY,
                    bounds.size);
                report.AppendLine(targetName + ":");
                report.AppendLine("  propBoundsCenter=" + Vec(bounds.center));
                report.AppendLine("  propBoundsSize=" + Vec(bounds.size));
                report.AppendLine("  legacyTargetY=" + Num(targetY));
                report.AppendLine("  legacyTargetX=" + Num(targetX));
                report.AppendLine("  legacyFocusedSliceCenter=" +
                    Vec(focusedSlice.center));
                report.AppendLine("  legacyFocusedSliceSize=" +
                    Vec(focusedSlice.size));
                report.AppendLine("  currentAnchorLocal=" + Vec(anchor.localPosition));
                TopHandleGripFrame topHandle = CalculateTopHandleGripFrame(holder);
                report.AppendLine("  topHandleComponentBoundsCenter=" +
                    Vec(topHandle.ComponentBounds.center));
                report.AppendLine("  topHandleComponentBoundsSize=" +
                    Vec(topHandle.ComponentBounds.size));
                report.AppendLine("  straightRegionBoundsCenter=" +
                    Vec(topHandle.StraightRegionBounds.center));
                report.AppendLine("  straightRegionBoundsSize=" +
                    Vec(topHandle.StraightRegionBounds.size));
                report.AppendLine("  straightAxisLocal=" +
                    Vec(topHandle.LocalAxis));
                report.AppendLine("  straightSegmentInnerStartLocal=" +
                    Vec(topHandle.InnerStartLocal));
                report.AppendLine("  straightSegmentInnerEndLocal=" +
                    Vec(topHandle.InnerEndLocal));
                report.AppendLine("  deterministicGripCenterLocal=" +
                    Vec(topHandle.PositionLocal));
                report.AppendLine("  meshFilters:");
                foreach (MeshFilter filter in holder
                             .GetComponentsInChildren<MeshFilter>(true)
                             .Where(value => value.sharedMesh != null)
                             .OrderBy(value => AnimationUtility.CalculateTransformPath(
                                 value.transform,
                                 holder),
                                 StringComparer.Ordinal))
                {
                    Bounds meshBounds = CalculateMeshFilterBoundsInSpace(
                        filter,
                        holder);
                    report.AppendLine("    path=" +
                        AnimationUtility.CalculateTransformPath(
                            filter.transform,
                            holder) +
                        " center=" + Vec(meshBounds.center) +
                        " size=" + Vec(meshBounds.size));
                    AppendUpperConnectedComponentReport(
                        report,
                        filter,
                        holder,
                        bounds);
                }
            }

            WriteText(
                RightWristOutputFolder,
                "vacuum_top_handle_geometry.txt",
                report.ToString());
            Debug.Log(
                "[VacuumCleanerGrip] Top-handle geometry inspected read-only. " +
                "The deterministic target is the straight-segment midpoint.");
        }

        private static TopHandleGripFrame CalculateTopHandleGripFrame(
            Transform holder)
        {
            MeshFilter filter = holder.GetComponentsInChildren<MeshFilter>(true)
                .SingleOrDefault(value => value.sharedMesh != null) ??
                throw new InvalidOperationException(
                    "Vacuum cleaner must contain exactly one readable mesh filter.");
            Mesh mesh = filter.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            var localPoints = new Vector3[vertices.Length];
            Matrix4x4 matrix = holder.worldToLocalMatrix *
                filter.transform.localToWorldMatrix;
            for (int index = 0; index < vertices.Length; index++)
            {
                localPoints[index] = matrix.MultiplyPoint3x4(vertices[index]);
            }

            int[] parent = Enumerable.Range(0, vertices.Length).ToArray();
            var weldedPositions = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < vertices.Length; index++)
            {
                Vector3 vertex = vertices[index];
                string key = Mathf.RoundToInt(vertex.x * 100000f) + ":" +
                    Mathf.RoundToInt(vertex.y * 100000f) + ":" +
                    Mathf.RoundToInt(vertex.z * 100000f);
                int existing;
                if (weldedPositions.TryGetValue(key, out existing))
                {
                    Union(parent, existing, index);
                }
                else
                {
                    weldedPositions.Add(key, index);
                }
            }

            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                int[] indices = mesh.GetTriangles(subMesh);
                for (int index = 0; index + 2 < indices.Length; index += 3)
                {
                    Union(parent, indices[index], indices[index + 1]);
                    Union(parent, indices[index + 1], indices[index + 2]);
                }
            }

            var components = new Dictionary<int, ComponentGeometry>();
            for (int index = 0; index < vertices.Length; index++)
            {
                int root = FindRoot(parent, index);
                ComponentGeometry component;
                if (!components.TryGetValue(root, out component))
                {
                    component = new ComponentGeometry(localPoints[index]);
                }
                else
                {
                    component.Bounds.Encapsulate(localPoints[index]);
                    component.VertexCount++;
                }

                components[root] = component;
            }

            Bounds propBounds = CalculateVertexBoundsInSpace(holder, holder);
            float upperThreshold = propBounds.min.y + propBounds.size.y * 0.72f;
            KeyValuePair<int, ComponentGeometry> dominant = components
                .Where(pair => pair.Value.Bounds.max.y >= upperThreshold)
                .OrderByDescending(pair => pair.Value.VertexCount)
                .First();
            float frontLimitZ = dominant.Value.Bounds.center.z;
            float topBarMinimumY = dominant.Value.Bounds.max.y -
                dominant.Value.Bounds.size.y * 0.25f;
            Vector3[] straightRegion = Enumerable.Range(0, vertices.Length)
                .Where(index => FindRoot(parent, index) == dominant.Key &&
                                localPoints[index].y >= topBarMinimumY &&
                                localPoints[index].z <= frontLimitZ)
                .Select(index => localPoints[index])
                .ToArray();
            if (straightRegion.Length < 20)
            {
                throw new InvalidOperationException(
                    "Vacuum top-handle straight region could not be isolated.");
            }

            var regionBounds = new Bounds(straightRegion[0], Vector3.zero);
            foreach (Vector3 point in straightRegion.Skip(1))
            {
                regionBounds.Encapsulate(point);
            }

            float meanY = straightRegion.Average(point => point.y);
            float meanZ = straightRegion.Average(point => point.z);
            float covarianceYY = 0f;
            float covarianceYZ = 0f;
            float covarianceZZ = 0f;
            foreach (Vector3 point in straightRegion)
            {
                float y = point.y - meanY;
                float z = point.z - meanZ;
                covarianceYY += y * y;
                covarianceYZ += y * z;
                covarianceZZ += z * z;
            }

            float angle = 0.5f * Mathf.Atan2(
                2f * covarianceYZ,
                covarianceYY - covarianceZZ);
            Vector3 localAxis = new Vector3(0f, Mathf.Cos(angle), Mathf.Sin(angle));
            if (localAxis.y < 0f) localAxis = -localAxis;
            Vector3 mean = new Vector3(
                dominant.Value.Bounds.max.x - 0.001f,
                meanY,
                meanZ);
            float[] projections = straightRegion
                .Select(point => Vector3.Dot(point - mean, localAxis))
                .OrderBy(value => value)
                .ToArray();
            float innerStart = projections[Mathf.RoundToInt(
                (projections.Length - 1) * 0.20f)];
            float innerEnd = projections[Mathf.RoundToInt(
                (projections.Length - 1) * 0.80f)];
            float centerProjection = (innerStart + innerEnd) * 0.5f;
            Vector3 idealPosition = mean + localAxis * centerProjection;
            idealPosition.x = dominant.Value.Bounds.max.x - 0.001f;
            Vector3 position = default;
            float nearestSurfaceDistance = float.PositiveInfinity;
            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                int[] indices = mesh.GetTriangles(subMesh);
                for (int index = 0; index + 2 < indices.Length; index += 3)
                {
                    int first = indices[index];
                    int second = indices[index + 1];
                    int third = indices[index + 2];
                    if (FindRoot(parent, first) != dominant.Key ||
                        FindRoot(parent, second) != dominant.Key ||
                        FindRoot(parent, third) != dominant.Key)
                    {
                        continue;
                    }

                    Vector3 centroid =
                        (localPoints[first] + localPoints[second] + localPoints[third]) /
                        3f;
                    float centroidProjection = Vector3.Dot(centroid - mean, localAxis);
                    if (centroid.y < topBarMinimumY ||
                        centroid.z > frontLimitZ ||
                        centroidProjection < innerStart ||
                        centroidProjection > innerEnd)
                    {
                        continue;
                    }

                    Vector3 closest = ClosestPointOnTriangle(
                        idealPosition,
                        localPoints[first],
                        localPoints[second],
                        localPoints[third]);
                    float distance = Vector3.Distance(idealPosition, closest);
                    if (distance < nearestSurfaceDistance)
                    {
                        nearestSurfaceDistance = distance;
                        position = closest;
                    }
                }
            }

            if (float.IsPositiveInfinity(nearestSurfaceDistance))
            {
                throw new InvalidOperationException(
                    "Vacuum top-handle surface projection found no straight-segment triangle.");
            }

            return new TopHandleGripFrame(
                position,
                localAxis,
                mean + localAxis * innerStart,
                mean + localAxis * innerEnd,
                dominant.Value.Bounds,
                regionBounds);
        }

        private static Vector3 ClosestPointOnTriangle(
            Vector3 point,
            Vector3 first,
            Vector3 second,
            Vector3 third)
        {
            Vector3 firstSecond = second - first;
            Vector3 firstThird = third - first;
            Vector3 firstPoint = point - first;
            float d1 = Vector3.Dot(firstSecond, firstPoint);
            float d2 = Vector3.Dot(firstThird, firstPoint);
            if (d1 <= 0f && d2 <= 0f) return first;

            Vector3 secondPoint = point - second;
            float d3 = Vector3.Dot(firstSecond, secondPoint);
            float d4 = Vector3.Dot(firstThird, secondPoint);
            if (d3 >= 0f && d4 <= d3) return second;

            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0f && d1 >= 0f && d3 <= 0f)
            {
                float value = d1 / (d1 - d3);
                return first + value * firstSecond;
            }

            Vector3 thirdPoint = point - third;
            float d5 = Vector3.Dot(firstSecond, thirdPoint);
            float d6 = Vector3.Dot(firstThird, thirdPoint);
            if (d6 >= 0f && d5 <= d6) return third;

            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0f && d2 >= 0f && d6 <= 0f)
            {
                float value = d2 / (d2 - d6);
                return first + value * firstThird;
            }

            float va = d3 * d6 - d5 * d4;
            if (va <= 0f && d4 - d3 >= 0f && d5 - d6 >= 0f)
            {
                float value = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return second + value * (third - second);
            }

            float denominator = 1f / (va + vb + vc);
            float v = vb * denominator;
            float w = vc * denominator;
            return first + firstSecond * v + firstThird * w;
        }

        private static float DistanceToVacuumMeshSurface(
            Transform holder,
            Vector3 pointLocal)
        {
            float nearest = float.PositiveInfinity;
            foreach (MeshFilter filter in holder
                         .GetComponentsInChildren<MeshFilter>(true)
                         .Where(value => value.sharedMesh != null))
            {
                Mesh mesh = filter.sharedMesh;
                Vector3[] vertices = mesh.vertices;
                Matrix4x4 matrix = holder.worldToLocalMatrix *
                    filter.transform.localToWorldMatrix;
                for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                {
                    int[] indices = mesh.GetTriangles(subMesh);
                    for (int index = 0; index + 2 < indices.Length; index += 3)
                    {
                        Vector3 closest = ClosestPointOnTriangle(
                            pointLocal,
                            matrix.MultiplyPoint3x4(vertices[indices[index]]),
                            matrix.MultiplyPoint3x4(vertices[indices[index + 1]]),
                            matrix.MultiplyPoint3x4(vertices[indices[index + 2]]));
                        nearest = Mathf.Min(
                            nearest,
                            Vector3.Distance(pointLocal, closest));
                    }
                }
            }

            return nearest;
        }

        private static Bounds CalculateMeshFilterBoundsInSpace(
            MeshFilter filter,
            Transform space)
        {
            Vector3[] vertices = filter.sharedMesh.vertices;
            if (vertices.Length == 0)
            {
                throw new InvalidOperationException(
                    "Vacuum mesh has no vertices: " + filter.name + ".");
            }

            Matrix4x4 matrix = space.worldToLocalMatrix *
                filter.transform.localToWorldMatrix;
            var bounds = new Bounds(matrix.MultiplyPoint3x4(vertices[0]), Vector3.zero);
            foreach (Vector3 vertex in vertices.Skip(1))
            {
                bounds.Encapsulate(matrix.MultiplyPoint3x4(vertex));
            }

            return bounds;
        }

        private static void AppendUpperConnectedComponentReport(
            StringBuilder report,
            MeshFilter filter,
            Transform space,
            Bounds propBounds)
        {
            Mesh mesh = filter.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            int[] parent = Enumerable.Range(0, vertices.Length).ToArray();
            var weldedPositions = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < vertices.Length; index++)
            {
                Vector3 vertex = vertices[index];
                string key = Mathf.RoundToInt(vertex.x * 100000f) + ":" +
                    Mathf.RoundToInt(vertex.y * 100000f) + ":" +
                    Mathf.RoundToInt(vertex.z * 100000f);
                int existing;
                if (weldedPositions.TryGetValue(key, out existing))
                {
                    Union(parent, existing, index);
                }
                else
                {
                    weldedPositions.Add(key, index);
                }
            }

            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                int[] indices = mesh.GetTriangles(subMesh);
                for (int index = 0; index + 2 < indices.Length; index += 3)
                {
                    Union(parent, indices[index], indices[index + 1]);
                    Union(parent, indices[index + 1], indices[index + 2]);
                }
            }

            Matrix4x4 matrix = space.worldToLocalMatrix *
                filter.transform.localToWorldMatrix;
            var components = new Dictionary<int, ComponentGeometry>();
            var localPoints = new Vector3[vertices.Length];
            for (int index = 0; index < vertices.Length; index++)
            {
                int root = FindRoot(parent, index);
                Vector3 point = matrix.MultiplyPoint3x4(vertices[index]);
                localPoints[index] = point;
                ComponentGeometry component;
                if (!components.TryGetValue(root, out component))
                {
                    component = new ComponentGeometry(point);
                }
                else
                {
                    component.Bounds.Encapsulate(point);
                    component.VertexCount++;
                }

                components[root] = component;
            }

            float upperThreshold = propBounds.min.y + propBounds.size.y * 0.72f;
            ComponentGeometry[] upper = components.Values
                .Where(value => value.Bounds.max.y >= upperThreshold)
                .OrderByDescending(value => value.VertexCount)
                .ThenByDescending(value => value.Bounds.size.sqrMagnitude)
                .Take(40)
                .ToArray();
            report.AppendLine("    connectedComponents=" + components.Count +
                " upperComponents=" + upper.Length +
                " upperThresholdY=" + Num(upperThreshold));
            for (int index = 0; index < upper.Length; index++)
            {
                ComponentGeometry component = upper[index];
                report.AppendLine("      component=" + index +
                    " vertices=" + component.VertexCount +
                    " center=" + Vec(component.Bounds.center) +
                    " size=" + Vec(component.Bounds.size));
            }

            if (upper.Length > 0)
            {
                ComponentGeometry dominant = upper[0];
                int dominantRoot = components.First(pair =>
                    pair.Value.VertexCount == dominant.VertexCount &&
                    pair.Value.Bounds.center == dominant.Bounds.center).Key;
                float outerXThreshold = dominant.Bounds.max.x - 0.015f;
                Vector3[] dominantOuter = Enumerable.Range(0, vertices.Length)
                    .Where(index => FindRoot(parent, index) == dominantRoot &&
                                    localPoints[index].x >= outerXThreshold)
                    .Select(index => localPoints[index])
                    .ToArray();
                report.AppendLine("    dominantUpperOuterProfile=" +
                    " componentVertices=" + dominant.VertexCount +
                    " outerVertexCount=" + dominantOuter.Length +
                    " outerXThreshold=" + Num(outerXThreshold));
                const int binCount = 16;
                float binHeight = dominant.Bounds.size.y / binCount;
                for (int bin = 0; bin < binCount; bin++)
                {
                    float minY = dominant.Bounds.min.y + binHeight * bin;
                    float maxY = minY + binHeight;
                    float[] zValues = dominantOuter
                        .Where(point => point.y >= minY &&
                                        (bin == binCount - 1
                                            ? point.y <= maxY
                                            : point.y < maxY))
                        .Select(point => point.z)
                        .OrderBy(value => value)
                        .ToArray();
                    if (zValues.Length == 0) continue;
                    var clusters = new List<List<float>>();
                    foreach (float z in zValues)
                    {
                        if (clusters.Count == 0 ||
                            z - clusters[clusters.Count - 1]
                                [clusters[clusters.Count - 1].Count - 1] > 0.012f)
                        {
                            clusters.Add(new List<float>());
                        }

                        clusters[clusters.Count - 1].Add(z);
                    }

                    report.AppendLine("      y=" + Num((minY + maxY) * 0.5f) +
                        " zClusters=" + string.Join(",", clusters.Select(cluster =>
                            "[" + Num(cluster.First()) + ".." +
                            Num(cluster.Last()) + ";n=" + cluster.Count + "]")));
                }
            }
        }

        private static int FindRoot(int[] parent, int value)
        {
            while (parent[value] != value)
            {
                parent[value] = parent[parent[value]];
                value = parent[value];
            }

            return value;
        }

        private static void Union(int[] parent, int left, int right)
        {
            int leftRoot = FindRoot(parent, left);
            int rightRoot = FindRoot(parent, right);
            if (leftRoot != rightRoot)
            {
                parent[rightRoot] = leftRoot;
            }
        }

        internal static void ApplyVacuumTopHandleGrip()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            Transform source = RequireTarget(PlayerIdleName);
            Transform idle = RequireTarget(IdleName);
            Transform use = RequireTarget(UseName);
            Transform idleHolder = RequireExistingVacuum(idle);
            Transform useHolder = RequireExistingVacuum(use);
            Transform idleModel = idleHolder.Find(ModelInstanceName) ??
                throw new InvalidOperationException(IdleName + " has no vacuum model.");
            Transform useModel = useHolder.Find(ModelInstanceName) ??
                throw new InvalidOperationException(UseName + " has no vacuum model.");
            Transform idleRightAnchor = idleHolder.Find(RightGripAnchorName) ??
                throw new InvalidOperationException(IdleName + " has no right grip anchor.");
            Transform useRightAnchor = useHolder.Find(RightGripAnchorName) ??
                throw new InvalidOperationException(UseName + " has no right grip anchor.");
            Transform idleLeftAnchor = idleHolder.Find(LeftGripAnchorName) ??
                throw new InvalidOperationException(IdleName + " has no left grip anchor.");
            Transform useLeftAnchor = useHolder.Find(LeftGripAnchorName) ??
                throw new InvalidOperationException(UseName + " has no left grip anchor.");
            var idleRootBefore = new LocalTransformState(idle);
            var useRootBefore = new LocalTransformState(use);
            var idleHolderBefore = new LocalTransformState(idleHolder);
            var useHolderBefore = new LocalTransformState(useHolder);
            var idleModelBefore = new TransformHierarchyState(idleModel);
            var useModelBefore = new TransformHierarchyState(useModel);
            var idleLeftAnchorBefore = new LocalTransformState(idleLeftAnchor);
            var useLeftAnchorBefore = new LocalTransformState(useLeftAnchor);
            var idleLeftBefore = new TransformHierarchyState(
                FindRequired(idle, LeftShoulderPath));
            var useLeftBefore = new TransformHierarchyState(
                FindRequired(use, LeftShoulderPath));
            var idleProtectedBefore = new TargetProtectionState(idle);
            var useProtectedBefore = new TargetProtectionState(use);
            string sourceBefore = BuildTargetStateSignature(source, false);
            TopHandleGripFrame frame = CalculateTopHandleGripFrame(idleHolder);
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply vacuum upper straight handle grip");
            try
            {
                Undo.RecordObjects(
                    new UnityEngine.Object[] { idleRightAnchor, useRightAnchor },
                    "Move right grips to upper straight handle center");
                idleRightAnchor.localPosition = frame.PositionLocal;
                useRightAnchor.localPosition = frame.PositionLocal;
                EditorUtility.SetDirty(idleRightAnchor);
                EditorUtility.SetDirty(useRightAnchor);

                CopyPlayerIdleArmPose(source, idle, RightShoulderPath);
                PoseArmAtGrip(
                    idle,
                    true,
                    idleRightAnchor.position,
                    idle.TransformPoint(new Vector3(0.40f, 1.06f, 0.18f)),
                    idleHolder.TransformDirection(frame.LocalAxis),
                    true);
                CopyTransformHierarchy(
                    FindRequired(idle, RightShoulderPath),
                    FindRequired(use, RightShoulderPath),
                    "Copy upper-handle right grip to Vacuum_Use");

                RequireAuthoredVacuumIdleTransform(idleHolder, idleModel);
                RequireAuthoredVacuumIdleTransform(useHolder, useModel);
                RequirePlayerIdleArmPose(source, idle, LeftShoulderPath);
                RequirePlayerIdleArmPose(source, use, LeftShoulderPath);
                TopHandleGripMetrics idleMetrics = RequireTopHandleGrip(idle);
                TopHandleGripMetrics useMetrics = RequireTopHandleGrip(use);
                RequireVacuumIdleStateMatchesUse(idle, use);
                if (!idleRootBefore.Matches(idle) ||
                    !useRootBefore.Matches(use) ||
                    !idleHolderBefore.Matches(idleHolder) ||
                    !useHolderBefore.Matches(useHolder) ||
                    !idleModelBefore.Matches(idleModel) ||
                    !useModelBefore.Matches(useModel) ||
                    !idleLeftAnchorBefore.Matches(idleLeftAnchor) ||
                    !useLeftAnchorBefore.Matches(useLeftAnchor) ||
                    !idleLeftBefore.Matches(FindRequired(idle, LeftShoulderPath)) ||
                    !useLeftBefore.Matches(FindRequired(use, LeftShoulderPath)))
                {
                    throw new InvalidOperationException(
                        "A protected vacuum, root, model, left-anchor, or left-arm " +
                        "transform changed.");
                }

                idleProtectedBefore.RequireProtectedStateUnchanged(idle);
                useProtectedBefore.RequireProtectedStateUnchanged(use);
                if (!string.Equals(
                        sourceBefore,
                        BuildTargetStateSignature(source, false),
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Player_Idle changed while applying the top-handle grip.");
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException("CargoRunMvp scene save failed.");
                }

                AssetDatabase.SaveAssets();
                WriteText(
                    RightWristOutputFolder,
                    "vacuum_top_handle_grip_application.txt",
                    "Vacuum top-handle grip application\n" +
                    "referenceGrip=C:/Users/gus68/OneDrive/바탕 화면/1111.jfif\n" +
                    "referenceLocation=C:/Users/gus68/OneDrive/바탕 화면/33333.png\n" +
                    "target=middle of long straight upper black handle\n" +
                    "excluded=handle start,bend,canister connector\n" +
                    "gripCenterLocal=" + Vec(frame.PositionLocal) + "\n" +
                    "gripAxisLocal=" + Vec(frame.LocalAxis) + "\n" +
                    "Vacuum_Idle=" + idleMetrics.Describe() + "\n" +
                    "Vacuum_Use=" + useMetrics.Describe() + "\n" +
                    "vacuumTransformsChanged=False\n" +
                    "leftArmsChanged=False\n" +
                    "bodyAndLowerBodyChanged=False\n");
                Undo.CollapseUndoOperations(undoGroup);
                Debug.Log(
                    "[VacuumCleanerGrip] Upper straight-handle center grip " +
                    "applied to Vacuum_Idle and Vacuum_Use. " +
                    idleMetrics.Describe() + " " + useMetrics.Describe() + ".");
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
        }

        internal static void InspectVacuumTopHandleGrip()
        {
            RequireEditMode();
            RequireScene();
            Transform source = RequireTarget(PlayerIdleName);
            Transform idle = RequireTarget(IdleName);
            Transform use = RequireTarget(UseName);
            Transform idleHolder = RequireExistingVacuum(idle);
            Transform useHolder = RequireExistingVacuum(use);
            RequireAuthoredVacuumIdleTransform(
                idleHolder,
                idleHolder.Find(ModelInstanceName) ??
                throw new InvalidOperationException(IdleName + " has no vacuum model."));
            RequireAuthoredVacuumIdleTransform(
                useHolder,
                useHolder.Find(ModelInstanceName) ??
                throw new InvalidOperationException(UseName + " has no vacuum model."));
            RequirePlayerIdleArmPose(source, idle, LeftShoulderPath);
            RequirePlayerIdleArmPose(source, use, LeftShoulderPath);
            TopHandleGripMetrics idleMetrics = RequireTopHandleGrip(idle);
            TopHandleGripMetrics useMetrics = RequireTopHandleGrip(use);
            StateCopyMetrics copyMetrics = RequireVacuumIdleStateMatchesUse(idle, use);
            WriteText(
                RightWristOutputFolder,
                "vacuum_top_handle_grip_inspection.txt",
                "Vacuum top-handle grip read-only inspection\n" +
                "visualMatchRemainsPrimary=True\n" +
                "verificationTargetManipulated=False\n" +
                "Vacuum_Idle=" + idleMetrics.Describe() + "\n" +
                "Vacuum_Use=" + useMetrics.Describe() + "\n" +
                copyMetrics.Describe() + "\n" +
                "vacuumTransformsMatchAuthoredState=True\n" +
                "leftArmsMatchPlayerIdle=True\n");
            Debug.Log(
                "[VacuumCleanerGrip] Read-only top-handle grip inspection passed. " +
                idleMetrics.Describe() + " " + useMetrics.Describe() + ".");
        }

        private static TopHandleGripMetrics RequireTopHandleGrip(Transform target)
        {
            Transform holder = RequireExistingVacuum(target);
            Transform anchor = holder.Find(RightGripAnchorName) ??
                throw new InvalidOperationException(target.name + " has no right grip anchor.");
            TopHandleGripFrame frame = CalculateTopHandleGripFrame(holder);
            float anchorError = Vector3.Distance(anchor.localPosition, frame.PositionLocal);
            float startClearance = Vector3.Dot(
                anchor.localPosition - frame.InnerStartLocal,
                frame.LocalAxis);
            float endClearance = Vector3.Dot(
                frame.InnerEndLocal - anchor.localPosition,
                frame.LocalAxis);
            float nearestSurfaceDistance = DistanceToVacuumMeshSurface(
                holder,
                anchor.localPosition);
            ReferenceWristMetrics wrist = RequireReferenceAlignedRightWrist(
                target,
                holder.TransformDirection(frame.LocalAxis),
                false);
            Transform lower = FindRequired(target, RightForeArmPath);
            Transform hand = FindRequired(target, RightHandPath);
            Vector3 handleAxis = holder.TransformDirection(frame.LocalAxis);
            Vector3 forearmDirection = (hand.position - lower.position).normalized;
            float handleForearmAngle = Mathf.Min(
                Vector3.Angle(handleAxis, forearmDirection),
                Vector3.Angle(-handleAxis, forearmDirection));
            var metrics = new TopHandleGripMetrics(
                target.name,
                anchorError,
                startClearance,
                endClearance,
                nearestSurfaceDistance,
                handleForearmAngle,
                wrist);
            float segmentLength = startClearance + endClearance;
            if (anchorError > 0.00001f ||
                segmentLength < 0.04f ||
                startClearance < segmentLength * 0.45f ||
                endClearance < segmentLength * 0.45f ||
                nearestSurfaceDistance > 0.0001f)
            {
                throw new InvalidOperationException(
                    target.name + " top-handle location failed. " +
                    metrics.Describe() + ".");
            }

            return metrics;
        }

        internal static void ApplyVacuumRightWristGrip()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            Transform source = RequireTarget(PlayerIdleName);
            Transform idle = RequireTarget(IdleName);
            Transform use = RequireTarget(UseName);
            Transform idleHolder = RequireExistingVacuum(idle);
            Transform useHolder = RequireExistingVacuum(use);
            Transform idleModel = idleHolder.Find(ModelInstanceName) ??
                throw new InvalidOperationException(IdleName + " has no vacuum model.");
            Transform useModel = useHolder.Find(ModelInstanceName) ??
                throw new InvalidOperationException(UseName + " has no vacuum model.");
            Transform rightAnchor = idleHolder.Find(RightGripAnchorName) ??
                throw new InvalidOperationException(IdleName + " has no right grip anchor.");
            var idleRootBefore = new LocalTransformState(idle);
            var useRootBefore = new LocalTransformState(use);
            var idleHolderBefore = new TransformHierarchyState(idleHolder);
            var useHolderBefore = new TransformHierarchyState(useHolder);
            var idleLeftBefore = new TransformHierarchyState(
                FindRequired(idle, LeftShoulderPath));
            var useLeftBefore = new TransformHierarchyState(
                FindRequired(use, LeftShoulderPath));
            var idleProtectedBefore = new TargetProtectionState(idle);
            var useProtectedBefore = new TargetProtectionState(use);
            string sourceBefore = BuildTargetStateSignature(source, false);
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply reference-aligned vacuum right wrist grip");
            try
            {
                CopyPlayerIdleArmPose(source, idle, RightShoulderPath);
                PoseArmAtGrip(
                    idle,
                    true,
                    rightAnchor.position,
                    idle.TransformPoint(new Vector3(0.40f, 1.06f, 0.18f)),
                    idleHolder.forward,
                    true);
                CopyTransformHierarchy(
                    FindRequired(idle, RightShoulderPath),
                    FindRequired(use, RightShoulderPath),
                    "Copy reference-aligned right arm to Vacuum_Use");

                RequireAuthoredVacuumIdleTransform(idleHolder, idleModel);
                RequireAuthoredVacuumIdleTransform(useHolder, useModel);
                RequirePlayerIdleArmPose(source, idle, LeftShoulderPath);
                RequirePlayerIdleArmPose(source, use, LeftShoulderPath);
                ReferenceWristMetrics idleMetrics =
                    RequireReferenceAlignedRightWrist(idle);
                ReferenceWristMetrics useMetrics =
                    RequireReferenceAlignedRightWrist(use);
                RequireVacuumIdleStateMatchesUse(idle, use);
                if (!idleRootBefore.Matches(idle) ||
                    !useRootBefore.Matches(use) ||
                    !idleHolderBefore.Matches(idleHolder) ||
                    !useHolderBefore.Matches(useHolder) ||
                    !idleLeftBefore.Matches(FindRequired(idle, LeftShoulderPath)) ||
                    !useLeftBefore.Matches(FindRequired(use, LeftShoulderPath)))
                {
                    throw new InvalidOperationException(
                        "A protected vacuum, root, or left-arm transform changed.");
                }

                idleProtectedBefore.RequireProtectedStateUnchanged(idle);
                useProtectedBefore.RequireProtectedStateUnchanged(use);
                if (!string.Equals(
                        sourceBefore,
                        BuildTargetStateSignature(source, false),
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Player_Idle changed while applying the vacuum wrist grip.");
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException("CargoRunMvp scene save failed.");
                }

                AssetDatabase.SaveAssets();
                WriteText(
                    RightWristOutputFolder,
                    "vacuum_right_wrist_grip_application.txt",
                    "Reference-aligned vacuum right wrist grip application\n" +
                    "referenceImage=C:/Users/gus68/OneDrive/바탕 화면/1111.jfif\n" +
                    "visualMatchRequiredBeforeNumericAcceptance=True\n" +
                    "Vacuum_Idle=" + idleMetrics.Describe() + "\n" +
                    "Vacuum_Use=" + useMetrics.Describe() + "\n" +
                    "vacuumTransformsChanged=False\n" +
                    "leftArmsChanged=False\n" +
                    "bodyAndLowerBodyChanged=False\n");
                Undo.CollapseUndoOperations(undoGroup);
                Debug.Log(
                    "[VacuumCleanerGrip] Reference-aligned right wrist grip " +
                    "applied to Vacuum_Idle and Vacuum_Use. " +
                    idleMetrics.Describe() + " " + useMetrics.Describe() + ".");
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
        }

        internal static void InspectVacuumRightWristGrip()
        {
            RequireEditMode();
            RequireScene();
            Transform source = RequireTarget(PlayerIdleName);
            Transform idle = RequireTarget(IdleName);
            Transform use = RequireTarget(UseName);
            Transform idleHolder = RequireExistingVacuum(idle);
            Transform useHolder = RequireExistingVacuum(use);
            RequireAuthoredVacuumIdleTransform(
                idleHolder,
                idleHolder.Find(ModelInstanceName) ??
                throw new InvalidOperationException(IdleName + " has no vacuum model."));
            RequireAuthoredVacuumIdleTransform(
                useHolder,
                useHolder.Find(ModelInstanceName) ??
                throw new InvalidOperationException(UseName + " has no vacuum model."));
            RequirePlayerIdleArmPose(source, idle, LeftShoulderPath);
            RequirePlayerIdleArmPose(source, use, LeftShoulderPath);
            ReferenceWristMetrics idleMetrics = RequireReferenceAlignedRightWrist(idle);
            ReferenceWristMetrics useMetrics = RequireReferenceAlignedRightWrist(use);
            RequireVacuumIdleStateMatchesUse(idle, use);
            WriteText(
                RightWristOutputFolder,
                "vacuum_right_wrist_grip_inspection.txt",
                "Reference-aligned vacuum right wrist grip inspection\n" +
                "visualMatchRemainsPrimary=True\n" +
                "verificationTargetManipulated=False\n" +
                "Vacuum_Idle=" + idleMetrics.Describe() + "\n" +
                "Vacuum_Use=" + useMetrics.Describe() + "\n" +
                "vacuumTransformsMatchAuthoredState=True\n" +
                "leftArmsMatchPlayerIdle=True\n" +
                "rightArmHierarchiesMatch=True\n");
            Debug.Log(
                "[VacuumCleanerGrip] Read-only reference-aligned wrist " +
                "inspection passed. " + idleMetrics.Describe() + " " +
                useMetrics.Describe() + ".");
        }

        internal static void CaptureVacuumRightWristGrip()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            Transform idle = RequireTarget(IdleName);
            Transform use = RequireTarget(UseName);
            RequireReferenceAlignedRightWrist(idle);
            RequireReferenceAlignedRightWrist(use);
            RequireVacuumIdleStateMatchesUse(idle, use);
            string destination = Absolute(Path.Combine(
                RightWristOutputFolder,
                "vacuum_right_wrist_grip_final.png"));
            Texture2D idleSide = null;
            Texture2D useSide = null;
            Texture2D idleClose = null;
            Texture2D useClose = null;
            Texture2D composite = null;
            try
            {
                Bounds idleBounds = BoundsOf(idle);
                Bounds useBounds = BoundsOf(use);
                float aspect = 800f / 1200f;
                float fullSize = Mathf.Max(
                    Mathf.Max(idleBounds.extents.y, idleBounds.extents.x / aspect),
                    Mathf.Max(useBounds.extents.y, useBounds.extents.x / aspect)) *
                    1.16f;
                idleSide = RenderOrthographicPanel(
                    scene,
                    idleBounds.center,
                    idle.right,
                    2.5f,
                    fullSize,
                    800,
                    1200);
                useSide = RenderOrthographicPanel(
                    scene,
                    useBounds.center,
                    use.right,
                    2.5f,
                    fullSize,
                    800,
                    1200);
                idleClose = RenderRightWristClosePanel(scene, idle, 800, 1200);
                useClose = RenderRightWristClosePanel(scene, use, 800, 1200);
                composite = new Texture2D(3200, 1200, TextureFormat.RGB24, false);
                composite.SetPixels32(0, 0, 800, 1200, idleSide.GetPixels32());
                composite.SetPixels32(800, 0, 800, 1200, useSide.GetPixels32());
                composite.SetPixels32(1600, 0, 800, 1200, idleClose.GetPixels32());
                composite.SetPixels32(2400, 0, 800, 1200, useClose.GetPixels32());
                composite.Apply(false, false);
                Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                    throw new InvalidOperationException(
                        "Right wrist capture directory is unavailable."));
                File.WriteAllBytes(destination, composite.EncodeToPNG());
            }
            finally
            {
                if (idleSide != null) UnityEngine.Object.DestroyImmediate(idleSide);
                if (useSide != null) UnityEngine.Object.DestroyImmediate(useSide);
                if (idleClose != null) UnityEngine.Object.DestroyImmediate(idleClose);
                if (useClose != null) UnityEngine.Object.DestroyImmediate(useClose);
                if (composite != null) UnityEngine.Object.DestroyImmediate(composite);
            }

            Debug.Log(
                "[VacuumCleanerGrip] Reference-image right wrist comparison " +
                "captured once. Output=" + destination + ".");
        }

        internal static void CaptureVacuumTopHandleGrip()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            Transform idle = RequireTarget(IdleName);
            Transform use = RequireTarget(UseName);
            RequireTopHandleGrip(idle);
            RequireTopHandleGrip(use);
            RequireVacuumIdleStateMatchesUse(idle, use);
            const int panelSize = 800;
            string destination = Absolute(Path.Combine(
                RightWristOutputFolder,
                "vacuum_top_handle_grip_final.png"));
            Texture2D referenceGrip = null;
            Texture2D referenceLocation = null;
            Texture2D idleResult = null;
            Texture2D useResult = null;
            Texture2D composite = null;
            try
            {
                string gripReferencePath =
                    "C:/Users/gus68/OneDrive/바탕 화면/1111.jfif";
                string locationReferencePath =
                    "C:/Users/gus68/OneDrive/바탕 화면/33333.png";
                referenceGrip = File.Exists(gripReferencePath)
                    ? LoadReferencePanel(gripReferencePath, panelSize, panelSize)
                    : LoadExistingComparisonPanel(destination, 0, panelSize);
                referenceLocation = File.Exists(locationReferencePath)
                    ? LoadReferencePanel(locationReferencePath, panelSize, panelSize)
                    : LoadExistingComparisonPanel(destination, 1, panelSize);
                idleResult = RenderTopHandleGripClosePanel(
                    scene,
                    idle,
                    panelSize,
                    panelSize);
                useResult = RenderTopHandleGripClosePanel(
                    scene,
                    use,
                    panelSize,
                    panelSize);
                composite = new Texture2D(
                    panelSize * 4,
                    panelSize,
                    TextureFormat.RGB24,
                    false);
                composite.SetPixels32(
                    0,
                    0,
                    panelSize,
                    panelSize,
                    referenceGrip.GetPixels32());
                composite.SetPixels32(
                    panelSize,
                    0,
                    panelSize,
                    panelSize,
                    referenceLocation.GetPixels32());
                composite.SetPixels32(
                    panelSize * 2,
                    0,
                    panelSize,
                    panelSize,
                    idleResult.GetPixels32());
                composite.SetPixels32(
                    panelSize * 3,
                    0,
                    panelSize,
                    panelSize,
                    useResult.GetPixels32());
                composite.Apply(false, false);
                Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                    throw new InvalidOperationException(
                        "Top-handle capture directory is unavailable."));
                File.WriteAllBytes(destination, composite.EncodeToPNG());
                WriteText(
                    RightWristOutputFolder,
                    "vacuum_top_handle_grip_final_layout.txt",
                    "Vacuum top-handle final visual comparison\n" +
                    "panelOrderLeftToRight=reference grip method 1111.jfif," +
                    "reference target location 33333.png," +
                    "Vacuum_Idle result,Vacuum_Use result\n" +
                    "resultViewAngle=target right-side dominant oblique\n" +
                    "resultZoomIdentical=True\n" +
                    "captureCount=approved additional comparison 1\n");
            }
            finally
            {
                if (referenceGrip != null)
                    UnityEngine.Object.DestroyImmediate(referenceGrip);
                if (referenceLocation != null)
                    UnityEngine.Object.DestroyImmediate(referenceLocation);
                if (idleResult != null)
                    UnityEngine.Object.DestroyImmediate(idleResult);
                if (useResult != null)
                    UnityEngine.Object.DestroyImmediate(useResult);
                if (composite != null)
                    UnityEngine.Object.DestroyImmediate(composite);
            }

            Debug.Log(
                "[VacuumCleanerGrip] Top-handle reference/result comparison " +
                "captured once. Output=" + destination + ".");
        }

        private static Texture2D RenderTopHandleGripClosePanel(
            Scene scene,
            Transform target,
            int width,
            int height)
        {
            Transform lower = FindRequired(target, RightForeArmPath);
            Transform hand = FindRequired(target, RightHandPath);
            Transform holder = RequireExistingVacuum(target);
            Transform anchor = holder.Find(RightGripAnchorName) ??
                throw new InvalidOperationException(target.name + " has no right grip anchor.");
            Vector3 lookAt =
                (lower.position + hand.position + anchor.position * 2f) / 4f;
            Vector3 referenceMatchedOblique =
                (target.right + target.forward * 0.35f).normalized;
            return RenderOrthographicPanel(
                scene,
                lookAt,
                referenceMatchedOblique,
                1.2f,
                0.36f,
                width,
                height);
        }

        private static Texture2D LoadReferencePanel(
            string absolutePath,
            int width,
            int height)
        {
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException(
                    "Vacuum grip reference image is missing.",
                    absolutePath);
            }

            var source = new Texture2D(2, 2, TextureFormat.RGB24, false);
            Texture2D cropped = null;
            Texture2D resized = null;
            Texture2D panel = null;
            try
            {
                if (!ImageConversion.LoadImage(source, File.ReadAllBytes(absolutePath), false))
                {
                    throw new InvalidOperationException(
                        "Vacuum grip reference image could not be decoded: " +
                        absolutePath + ".");
                }

                Color32[] pixels = source.GetPixels32();
                int minX = source.width;
                int minY = source.height;
                int maxX = -1;
                int maxY = -1;
                for (int y = 0; y < source.height; y++)
                {
                    for (int x = 0; x < source.width; x++)
                    {
                        Color32 color = pixels[y * source.width + x];
                        if (color.r >= 248 && color.g >= 248 && color.b >= 248)
                        {
                            continue;
                        }

                        minX = Mathf.Min(minX, x);
                        minY = Mathf.Min(minY, y);
                        maxX = Mathf.Max(maxX, x);
                        maxY = Mathf.Max(maxY, y);
                    }
                }

                if (maxX < minX || maxY < minY)
                {
                    minX = 0;
                    minY = 0;
                    maxX = source.width - 1;
                    maxY = source.height - 1;
                }

                minX = Mathf.Max(0, minX - 4);
                minY = Mathf.Max(0, minY - 4);
                maxX = Mathf.Min(source.width - 1, maxX + 4);
                maxY = Mathf.Min(source.height - 1, maxY + 4);
                int cropWidth = maxX - minX + 1;
                int cropHeight = maxY - minY + 1;
                cropped = new Texture2D(
                    cropWidth,
                    cropHeight,
                    TextureFormat.RGB24,
                    false);
                cropped.SetPixels(source.GetPixels(minX, minY, cropWidth, cropHeight));
                cropped.Apply(false, false);

                float scale = Mathf.Min(
                    (float)width / cropWidth,
                    (float)height / cropHeight);
                int resizedWidth = Mathf.Max(1, Mathf.RoundToInt(cropWidth * scale));
                int resizedHeight = Mathf.Max(1, Mathf.RoundToInt(cropHeight * scale));
                resized = ResizeTexture(cropped, resizedWidth, resizedHeight);
                panel = new Texture2D(width, height, TextureFormat.RGB24, false);
                Color32[] background = Enumerable.Repeat(
                    new Color32(238, 238, 238, 255),
                    width * height).ToArray();
                panel.SetPixels32(background);
                panel.SetPixels32(
                    (width - resizedWidth) / 2,
                    (height - resizedHeight) / 2,
                    resizedWidth,
                    resizedHeight,
                    resized.GetPixels32());
                panel.Apply(false, false);
                return panel;
            }
            catch
            {
                if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
                throw;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
                if (cropped != null) UnityEngine.Object.DestroyImmediate(cropped);
                if (resized != null) UnityEngine.Object.DestroyImmediate(resized);
            }
        }

        private static Texture2D LoadExistingComparisonPanel(
            string comparisonPath,
            int panelIndex,
            int panelSize)
        {
            if (!File.Exists(comparisonPath))
            {
                throw new FileNotFoundException(
                    "Vacuum grip reference and previous comparison are both missing.",
                    comparisonPath);
            }

            var source = new Texture2D(2, 2, TextureFormat.RGB24, false);
            Texture2D panel = null;
            try
            {
                if (!ImageConversion.LoadImage(
                        source,
                        File.ReadAllBytes(comparisonPath),
                        false) ||
                    source.width < panelSize * (panelIndex + 1) ||
                    source.height < panelSize)
                {
                    throw new InvalidOperationException(
                        "Previous vacuum comparison cannot supply reference panel " +
                        panelIndex + ".");
                }

                panel = new Texture2D(
                    panelSize,
                    panelSize,
                    TextureFormat.RGB24,
                    false);
                panel.SetPixels(source.GetPixels(
                    panelSize * panelIndex,
                    0,
                    panelSize,
                    panelSize));
                panel.Apply(false, false);
                return panel;
            }
            catch
            {
                if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
                throw;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static Texture2D ResizeTexture(Texture2D source, int width, int height)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture render = RenderTexture.GetTemporary(
                width,
                height,
                0,
                RenderTextureFormat.ARGB32);
            var result = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                source.filterMode = FilterMode.Bilinear;
                Graphics.Blit(source, render);
                RenderTexture.active = render;
                result.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                result.Apply(false, false);
                return result;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(result);
                throw;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
            }
        }

        private static Texture2D RenderRightWristClosePanel(
            Scene scene,
            Transform target,
            int width,
            int height)
        {
            Transform lower = FindRequired(target, RightForeArmPath);
            Transform hand = FindRequired(target, RightHandPath);
            Transform holder = RequireExistingVacuum(target);
            Transform anchor = holder.Find(RightGripAnchorName) ??
                throw new InvalidOperationException(target.name + " has no right grip anchor.");
            Vector3 lookAt = (lower.position + hand.position + anchor.position) / 3f;
            return RenderOrthographicPanel(
                scene,
                lookAt,
                target.right,
                1.2f,
                0.44f,
                width,
                height);
        }

        private static ReferenceWristMetrics RequireReferenceAlignedRightWrist(
            Transform target)
        {
            Transform holder = RequireExistingVacuum(target);
            return RequireReferenceAlignedRightWrist(target, holder.forward, true);
        }

        private static ReferenceWristMetrics RequireReferenceAlignedRightWrist(
            Transform target,
            Vector3 gripAxis,
            bool requireAcrossPalmAxis)
        {
            Transform holder = RequireExistingVacuum(target);
            Transform anchor = holder.Find(RightGripAnchorName) ??
                throw new InvalidOperationException(target.name + " has no right grip anchor.");
            Transform upper = FindRequired(target, RightArmPath);
            Transform lower = FindRequired(target, RightForeArmPath);
            Transform hand = FindRequired(target, RightHandPath);
            Transform middle = RequireDescendant(hand, "RightMiddleProximal");
            Transform index = RequireDescendant(hand, "RightIndexProximal");
            Transform little = RequireDescendant(hand, "RightLittleProximal");
            Vector3 forearmDirection = (hand.position - lower.position).normalized;
            Vector3 handDirection = (middle.position - hand.position).normalized;
            Vector3 acrossPalm = (index.position - little.position).normalized;
            float wristDeviation = Vector3.Angle(forearmDirection, handDirection);
            float gripAxisError = Mathf.Min(
                Vector3.Angle(acrossPalm, gripAxis),
                Vector3.Angle(-acrossPalm, gripAxis));
            float palmDistance = Vector3.Distance(
                CalculateHandPalmCenter(target, hand),
                anchor.position);
            float elbowDegrees = ElbowDegrees(upper, lower, hand);
            var metrics = new ReferenceWristMetrics(
                target.name,
                wristDeviation,
                gripAxisError,
                palmDistance,
                elbowDegrees);
            if (wristDeviation > 12f ||
                (requireAcrossPalmAxis && gripAxisError > 18f) ||
                palmDistance > 0.012f ||
                elbowDegrees < 10f ||
                elbowDegrees > MaximumElbowDegrees + 0.5f)
            {
                throw new InvalidOperationException(
                    target.name + " reference-aligned right wrist failed. " +
                    metrics.Describe() + ".");
            }

            return metrics;
        }

        private static void CopyTransformHierarchy(
            Transform source,
            Transform target,
            string undoName)
        {
            Dictionary<string, Transform> sourceTransforms = source
                .GetComponentsInChildren<Transform>(true)
                .ToDictionary(
                    transform => AnimationUtility.CalculateTransformPath(
                        transform,
                        source),
                    StringComparer.Ordinal);
            Dictionary<string, Transform> targetTransforms = target
                .GetComponentsInChildren<Transform>(true)
                .ToDictionary(
                    transform => AnimationUtility.CalculateTransformPath(
                        transform,
                        target),
                    StringComparer.Ordinal);
            if (sourceTransforms.Count != targetTransforms.Count ||
                sourceTransforms.Keys.Any(path => !targetTransforms.ContainsKey(path)))
            {
                throw new InvalidOperationException(
                    "Right-arm hierarchy differs between vacuum targets.");
            }

            Transform[] changed = targetTransforms.Values.ToArray();
            Undo.RecordObjects(changed.Cast<UnityEngine.Object>().ToArray(), undoName);
            foreach (var pair in sourceTransforms)
            {
                Transform destination = targetTransforms[pair.Key];
                destination.localPosition = pair.Value.localPosition;
                destination.localRotation = pair.Value.localRotation;
                destination.localScale = pair.Value.localScale;
                EditorUtility.SetDirty(destination);
                PrefabUtility.RecordPrefabInstancePropertyModifications(destination);
            }
        }

        private static void ApplyAuthoredVacuumIdleTransform(
            Transform holder,
            Transform model)
        {
            Undo.RecordObjects(
                new UnityEngine.Object[] { holder, model },
                "Apply authored Vacuum_Idle transform");
            holder.localPosition = AuthoredIdleHolderLocalPosition;
            holder.localRotation = AuthoredIdleHolderLocalRotation;
            holder.localScale = AuthoredIdleHolderLocalScale;
            model.localPosition = AuthoredIdleModelLocalPosition;
            model.localRotation = AuthoredIdleModelLocalRotation;
            model.localScale = AuthoredIdleModelLocalScale;
            EditorUtility.SetDirty(holder);
            EditorUtility.SetDirty(model);
            PrefabUtility.RecordPrefabInstancePropertyModifications(model);
        }

        private static void RequireAuthoredVacuumIdleTransform(
            Transform holder,
            Transform model)
        {
            const float positionTolerance = 0.00001f;
            const float rotationTolerance = 0.001f;
            if (Vector3.Distance(
                    holder.localPosition,
                    AuthoredIdleHolderLocalPosition) > positionTolerance ||
                Quaternion.Angle(
                    holder.localRotation,
                    AuthoredIdleHolderLocalRotation) > rotationTolerance ||
                Vector3.Distance(
                    holder.localScale,
                    AuthoredIdleHolderLocalScale) > positionTolerance ||
                Vector3.Distance(
                    model.localPosition,
                    AuthoredIdleModelLocalPosition) > positionTolerance ||
                Quaternion.Angle(
                    model.localRotation,
                    AuthoredIdleModelLocalRotation) > rotationTolerance ||
                Vector3.Distance(
                    model.localScale,
                    AuthoredIdleModelLocalScale) > positionTolerance)
            {
                throw new InvalidOperationException(
                    "Vacuum_Idle no longer matches the user-authored transform.");
            }
        }

        private static void RequireVacuumCleanerTransformsMatch(
            Transform idleHolder,
            Transform idleModel,
            Transform useHolder,
            Transform useModel)
        {
            if (idleHolder.localPosition != useHolder.localPosition ||
                idleHolder.localRotation != useHolder.localRotation ||
                idleHolder.localScale != useHolder.localScale ||
                idleModel.localPosition != useModel.localPosition ||
                idleModel.localRotation != useModel.localRotation ||
                idleModel.localScale != useModel.localScale)
            {
                throw new InvalidOperationException(
                    "Vacuum_Idle and Vacuum_Use cleaner local transforms differ.");
            }
        }

        private static string BuildVacuumCleanerTransformSyncReport(
            string title,
            Transform idleHolder,
            Transform idleModel,
            Transform useHolder,
            Transform useModel)
        {
            return title + "\n" +
                "idleHolderLocalPosition=" + Vec(idleHolder.localPosition) + "\n" +
                "idleHolderLocalRotation=" + Quat(idleHolder.localRotation) + "\n" +
                "idleHolderLocalScale=" + Vec(idleHolder.localScale) + "\n" +
                "idleModelLocalPosition=" + Vec(idleModel.localPosition) + "\n" +
                "idleModelLocalRotation=" + Quat(idleModel.localRotation) + "\n" +
                "idleModelLocalScale=" + Vec(idleModel.localScale) + "\n" +
                "useHolderLocalPosition=" + Vec(useHolder.localPosition) + "\n" +
                "useHolderLocalRotation=" + Quat(useHolder.localRotation) + "\n" +
                "useHolderLocalScale=" + Vec(useHolder.localScale) + "\n" +
                "useModelLocalPosition=" + Vec(useModel.localPosition) + "\n" +
                "useModelLocalRotation=" + Quat(useModel.localRotation) + "\n" +
                "useModelLocalScale=" + Vec(useModel.localScale) + "\n";
        }

        private static string BuildTargetStateSignatureIgnoringVacuumTransformValues(
            Transform root)
        {
            var result = new StringBuilder();
            foreach (Transform transform in root
                         .GetComponentsInChildren<Transform>(true)
                         .OrderBy(
                             value => AnimationUtility.CalculateTransformPath(
                                 value,
                                 root),
                             StringComparer.Ordinal))
            {
                string path = AnimationUtility.CalculateTransformPath(transform, root);
                result.Append(path)
                    .Append('|')
                    .Append(transform.gameObject.activeSelf)
                    .Append('|')
                    .Append(transform.gameObject.layer)
                    .Append('|')
                    .Append(transform.gameObject.tag)
                    .Append('|')
                    .Append(GameObjectUtility.GetStaticEditorFlags(transform.gameObject));
                bool isChangedTransform = path == HolderName ||
                    path == HolderName + "/" + ModelInstanceName;
                if (!isChangedTransform)
                {
                    result.Append('|').Append(Vec(transform.localPosition))
                        .Append('|').Append(Quat(transform.localRotation))
                        .Append('|').Append(Vec(transform.localScale));
                }

                foreach (Component component in transform.gameObject
                             .GetComponents<Component>())
                {
                    if (component is Transform) continue;
                    result.Append('|').Append(ComparableComponentState(component));
                }

                result.AppendLine();
            }

            return result.ToString();
        }

        private static void CaptureVacuumCleanerTransformSync(string fileName)
        {
            RequireEditMode();
            Scene scene = RequireScene();
            Transform idle = RequireTarget(IdleName);
            Transform use = RequireTarget(UseName);
            Transform idleHolder = RequireExistingVacuum(idle);
            Transform useHolder = RequireExistingVacuum(use);
            Transform idleModel = idleHolder.Find(ModelInstanceName) ??
                throw new InvalidOperationException(IdleName + " has no vacuum model.");
            Transform useModel = useHolder.Find(ModelInstanceName) ??
                throw new InvalidOperationException(UseName + " has no vacuum model.");
            RequireVacuumCleanerTransformsMatch(
                idleHolder,
                idleModel,
                useHolder,
                useModel);
            string directory = Absolute(TransformSyncOutputFolder);
            Directory.CreateDirectory(directory);
            string destination = Path.Combine(directory, fileName);
            Vector3 reviewCenterLocal = new Vector3(0f, 0.92f, 0.31f);
            const float reviewSize = 0.78f;
            Texture2D idleFront = null;
            Texture2D useFront = null;
            Texture2D idleSide = null;
            Texture2D useSide = null;
            Texture2D composite = null;
            try
            {
                idleFront = RenderOrthographicPanel(
                    scene,
                    idle.TransformPoint(reviewCenterLocal),
                    idle.forward,
                    1.5f,
                    reviewSize,
                    800,
                    1200);
                useFront = RenderOrthographicPanel(
                    scene,
                    use.TransformPoint(reviewCenterLocal),
                    use.forward,
                    1.5f,
                    reviewSize,
                    800,
                    1200);
                idleSide = RenderOrthographicPanel(
                    scene,
                    idle.TransformPoint(reviewCenterLocal),
                    idle.right,
                    1.5f,
                    reviewSize,
                    800,
                    1200);
                useSide = RenderOrthographicPanel(
                    scene,
                    use.TransformPoint(reviewCenterLocal),
                    use.right,
                    1.5f,
                    reviewSize,
                    800,
                    1200);
                composite = new Texture2D(3200, 1200, TextureFormat.RGB24, false);
                composite.SetPixels32(0, 0, 800, 1200, idleFront.GetPixels32());
                composite.SetPixels32(800, 0, 800, 1200, useFront.GetPixels32());
                composite.SetPixels32(1600, 0, 800, 1200, idleSide.GetPixels32());
                composite.SetPixels32(2400, 0, 800, 1200, useSide.GetPixels32());
                composite.Apply(false, false);
                File.WriteAllBytes(destination, composite.EncodeToPNG());
            }
            finally
            {
                if (idleFront != null) UnityEngine.Object.DestroyImmediate(idleFront);
                if (useFront != null) UnityEngine.Object.DestroyImmediate(useFront);
                if (idleSide != null) UnityEngine.Object.DestroyImmediate(idleSide);
                if (useSide != null) UnityEngine.Object.DestroyImmediate(useSide);
                if (composite != null) UnityEngine.Object.DestroyImmediate(composite);
            }

            Debug.Log(
                "[VacuumCleanerGrip] Vacuum cleaner transform-sync comparison " +
                "captured without target manipulation. Output=" + destination + ".");
        }

        private static float RequireVacuumIdleRightHandGrip(
            Transform source,
            Transform idle)
        {
            RequirePlayerIdleArmPose(source, idle, LeftShoulderPath);
            Transform holder = RequireExistingVacuum(idle);
            Transform rightAnchor = holder.Find(RightGripAnchorName) ??
                throw new InvalidOperationException(IdleName + " has no right grip anchor.");
            Transform rightHand = FindRequired(idle, RightHandPath);
            Bounds localBounds = CalculateVertexBoundsInSpace(holder, holder);
            Vector3 expectedGripLocal = GripPointLocal(
                holder,
                localBounds,
                VacuumIdleRightGripHeight01,
                0.34f);
            float palmDistance = Vector3.Distance(
                CalculateHandPalmCenter(idle, rightHand),
                rightAnchor.position);
            Transform upper = FindRequired(idle, RightArmPath);
            Transform lower = FindRequired(idle, RightForeArmPath);
            float elbowDegrees = ElbowDegrees(upper, lower, rightHand);
            if (Vector3.Distance(
                    rightAnchor.localPosition,
                    expectedGripLocal) > 0.00001f ||
                palmDistance > 0.012f ||
                elbowDegrees < 10f ||
                elbowDegrees > MaximumElbowDegrees + 0.5f)
            {
                throw new InvalidOperationException(
                    IdleName + " right-hand grip failed. palmDistance=" +
                    Num(palmDistance) + " elbowDegrees=" + Num(elbowDegrees) + ".");
            }

            return palmDistance;
        }

        private static void ApplyPlayerIdleArmPoseWithoutMovingVacuum()
        {
            Scene scene = RequireScene();
            Transform source = RequireTarget(PlayerIdleName);
            Transform idle = RequireTarget(IdleName);
            Transform use = RequireTarget(UseName);
            var idleBefore = new TargetProtectionState(idle);
            var useBefore = new TargetProtectionState(use);
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Match vacuum arms to Player_Idle");
            try
            {
                CopyPlayerIdleArmPose(source, idle);
                CopyPlayerIdleArmPose(source, use);
                RequirePlayerIdleArmPose(source, idle);
                RequirePlayerIdleArmPose(source, use);
                idleBefore.RequireProtectedStateUnchanged(idle);
                useBefore.RequireProtectedStateUnchanged(use);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException("CargoRunMvp scene save failed.");
                }

                AssetDatabase.SaveAssets();
                string report =
                    "Vacuum arm pose application\n" +
                    "source=" + PlayerIdleName + "\n" +
                    "targets=" + IdleName + "," + UseName + "\n" +
                    "leftAndRightArmPoseMatchesPlayerIdle=True\n" +
                    "vacuumTransformChanged=False\n" +
                    "protectedBodyLegAnimatorAndAppearanceUnchanged=True\n";
                WriteText("application.txt", report);
                Undo.CollapseUndoOperations(undoGroup);
                Debug.Log("[VacuumCleanerGrip] Player_Idle arm pose applied to " +
                    IdleName + " and " + UseName +
                    " without changing either vacuum transform.");
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
        }

        private static void CopyPlayerIdleArmPose(Transform source, Transform target)
        {
            foreach (string shoulderPath in
                     new[] { LeftShoulderPath, RightShoulderPath })
            {
                CopyPlayerIdleArmPose(source, target, shoulderPath);
            }
        }

        private static void CopyPlayerIdleArmPose(
            Transform source,
            Transform target,
            string shoulderPath)
        {
            Transform sourceShoulder = FindRequired(source, shoulderPath);
            Transform targetShoulder = FindRequired(target, shoulderPath);
            Transform[] sourceBones =
                sourceShoulder.GetComponentsInChildren<Transform>(true);
            Transform[] targetBones = sourceBones
                .Select(sourceBone => FindMatchingDescendant(
                    sourceShoulder,
                    targetShoulder,
                    sourceBone))
                .ToArray();
            Undo.RecordObjects(
                targetBones.Cast<UnityEngine.Object>().ToArray(),
                "Match " + target.name + " arm to Player_Idle");
            for (int index = 0; index < sourceBones.Length; index++)
            {
                Transform sourceBone = sourceBones[index];
                Transform targetBone = targetBones[index];
                targetBone.localPosition = sourceBone.localPosition;
                targetBone.localRotation = sourceBone.localRotation;
                targetBone.localScale = sourceBone.localScale;
                EditorUtility.SetDirty(targetBone);
                PrefabUtility.RecordPrefabInstancePropertyModifications(targetBone);
            }
        }

        private static Transform FindMatchingDescendant(
            Transform sourceRoot,
            Transform targetRoot,
            Transform source)
        {
            string relativePath = AnimationUtility.CalculateTransformPath(
                source,
                sourceRoot);
            Transform result = string.IsNullOrEmpty(relativePath)
                ? targetRoot
                : targetRoot.Find(relativePath);
            return result ?? throw new InvalidOperationException(
                targetRoot.name + " has no arm transform matching " + relativePath + ".");
        }

        private static void RequirePlayerIdleArmPose(
            Transform source,
            Transform target)
        {
            foreach (string shoulderPath in
                     new[] { LeftShoulderPath, RightShoulderPath })
            {
                RequirePlayerIdleArmPose(source, target, shoulderPath);
            }
        }

        private static void RequirePlayerIdleArmPose(
            Transform source,
            Transform target,
            string shoulderPath)
        {
            const float positionTolerance = 0.00001f;
            const float rotationTolerance = 0.01f;
            Transform sourceShoulder = FindRequired(source, shoulderPath);
            Transform targetShoulder = FindRequired(target, shoulderPath);
            foreach (Transform sourceBone in
                     sourceShoulder.GetComponentsInChildren<Transform>(true))
            {
                Transform targetBone = FindMatchingDescendant(
                    sourceShoulder,
                    targetShoulder,
                    sourceBone);
                if (Vector3.Distance(
                        sourceBone.localPosition,
                        targetBone.localPosition) > positionTolerance ||
                    Quaternion.Angle(
                        sourceBone.localRotation,
                        targetBone.localRotation) > rotationTolerance ||
                    Vector3.Distance(
                        sourceBone.localScale,
                        targetBone.localScale) > positionTolerance)
                {
                    throw new InvalidOperationException(
                        target.name + " arm pose differs from Player_Idle at " +
                        AnimationUtility.CalculateTransformPath(sourceBone, source) + ".");
                }
            }
        }

        private static void ApplySceneGrip(Material appearance)
        {
            Scene scene = RequireScene();
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Vacuum cleaner FBX is not imported.");
            Transform idle = RequireTarget(IdleName);
            Transform use = RequireTarget(UseName);
            var idleBefore = new TargetProtectionState(idle);
            var useBefore = new TargetProtectionState(use);
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply Vacuum Cleaner Floor Contact Diagonal Grip");
            try
            {
                RestoreArmDefaults(idle);
                RestoreArmDefaults(use);
                ReachPlan idlePlan = CalculateReachPlan(idle, model);
                ReachPlan usePlan = CalculateReachPlan(use, model);
                float appliedHeight = Mathf.Min(
                    CalculateChestAlignedPropHeight(idle, model),
                    CalculateChestAlignedPropHeight(use, model));
                float appliedCenterX = DesiredPropCenterXLocal;
                float appliedCenterZ = DesiredPropCenterZLocal;
                Debug.Log("[VacuumCleanerGrip] Central body-clearance placement. " +
                    "centerX=" + Num(appliedCenterX) +
                    " centerZ=" + Num(appliedCenterZ) + ".");
                GripMetrics idleMetrics = ApplyTarget(
                    idle,
                    model,
                    appearance,
                    appliedHeight,
                    appliedCenterX,
                    appliedCenterZ);
                GripMetrics useMetrics = ApplyTarget(
                    use,
                    model,
                    appearance,
                    appliedHeight,
                    appliedCenterX,
                    appliedCenterZ);
                idleBefore.RequireProtectedStateUnchanged(idle);
                useBefore.RequireProtectedStateUnchanged(use);
                RequireGripMetrics(idleMetrics);
                RequireGripMetrics(useMetrics);
                RequireMatchingTargetLayout(idleMetrics, useMetrics);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException("CargoRunMvp scene save failed.");
                }

                AssetDatabase.SaveAssets();
                WriteText("application.txt", DescribeMetrics(
                    "Vacuum cleaner grip application",
                    idleMetrics,
                    useMetrics,
                    "initialHeight=" + Num(InitialPropHeight) +
                    "; appliedHeight=" + Num(appliedHeight) +
                    "; appliedCenterZ=" + Num(appliedCenterZ) +
                    "; sizeRule=topHandleAtChestHeight" +
                    "; reachDrivenScale=False" +
                    "; idleInitialRightReach=" + Num(idlePlan.InitialRightDistance) +
                    "; idleInitialLeftReach=" + Num(idlePlan.InitialLeftDistance) +
                    "; useInitialRightReach=" + Num(usePlan.InitialRightDistance) +
                    "; useInitialLeftReach=" + Num(usePlan.InitialLeftDistance) +
                    "; protectedBodyLegAndTimingUnchanged=True"));
                Undo.CollapseUndoOperations(undoGroup);
                Debug.Log("[VacuumCleanerGrip] Applied floor-contact diagonal grip. " +
                    "initialHeight=" + Num(InitialPropHeight) +
                    " appliedHeight=" + Num(appliedHeight) +
                    " appliedCenterZ=" + Num(appliedCenterZ) +
                    " sizeRule=topHandleAtChestHeight" +
                    " reachDrivenScale=False. " +
                    DescribeMetricSummary(idleMetrics) + " " +
                    DescribeMetricSummary(useMetrics));
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
        }

        internal static void EnterReview()
        {
            RequireScene();
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.EnterPlaymode();
            }
        }

        internal static void Inspect()
        {
            RequireScene();
            Transform source = RequireTarget(PlayerIdleName);
            Transform idle = RequireTarget(IdleName);
            Transform use = RequireTarget(UseName);
            RequirePlayerIdleArmPose(source, idle);
            RequirePlayerIdleArmPose(source, use);
            RequireExistingVacuum(idle);
            RequireExistingVacuum(use);
            WriteText("inspection.txt",
                "Vacuum Player_Idle arm pose inspection\n" +
                "source=" + PlayerIdleName + "\n" +
                "targets=" + IdleName + "," + UseName + "\n" +
                "leftAndRightArmPoseMatchesPlayerIdle=True\n" +
                "verificationTargetManipulated=False\n");
            Debug.Log("[VacuumCleanerGrip] Read-only Player_Idle arm-pose " +
                "inspection passed for " + IdleName + " and " + UseName + ".");
        }

        private static Transform RequireExistingVacuum(Transform target)
        {
            Transform holder = Enumerable.Range(0, target.childCount)
                .Select(target.GetChild)
                .SingleOrDefault(child => child.name == HolderName) ??
                throw new InvalidOperationException(target.name + " has no vacuum holder.");
            Transform model = Enumerable.Range(0, holder.childCount)
                .Select(holder.GetChild)
                .SingleOrDefault(child => child.name == ModelInstanceName) ??
                throw new InvalidOperationException(target.name + " has no vacuum model.");
            if (model.GetComponentInChildren<MeshFilter>(true)?.sharedMesh == null)
            {
                throw new InvalidOperationException(target.name + " vacuum mesh is missing.");
            }

            return holder;
        }

        internal static void CaptureFinal()
        {
            CaptureReviewComposite("final.png", "Final direct-review composite captured once");
        }

        internal static void CaptureCorrectionDiagnostic()
        {
            CaptureReviewComposite(
                "correction_diagnostic.png",
                "Correction diagnostic composite captured once");
        }

        private static void CaptureReviewComposite(string fileName, string logDescription)
        {
            Scene scene = RequireScene();
            Transform source = RequireTarget(PlayerIdleName);
            Transform idle = RequireTarget(IdleName);
            Transform use = RequireTarget(UseName);
            RequirePlayerIdleArmPose(source, idle);
            RequirePlayerIdleArmPose(source, use);
            RequireExistingVacuum(idle);
            RequireExistingVacuum(use);
            string destination = Absolute(Path.Combine(OutputFolder, fileName));
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Capture directory is unavailable."));

            Texture2D idleSide = null;
            Texture2D useSide = null;
            Texture2D idleClose = null;
            Texture2D useClose = null;
            Texture2D composite = null;
            try
            {
                idleSide = RenderPanel(scene, BoundsOf(idle), idle.right, 800, 1200);
                useSide = RenderPanel(scene, BoundsOf(use), use.right, 800, 1200);
                idleClose = RenderGripClosePanel(scene, idle, 800, 1200);
                useClose = RenderGripClosePanel(scene, use, 800, 1200);
                composite = new Texture2D(3200, 1200, TextureFormat.RGB24, false);
                composite.SetPixels32(0, 0, 800, 1200, idleSide.GetPixels32());
                composite.SetPixels32(800, 0, 800, 1200, useSide.GetPixels32());
                composite.SetPixels32(1600, 0, 800, 1200, idleClose.GetPixels32());
                composite.SetPixels32(2400, 0, 800, 1200, useClose.GetPixels32());
                composite.Apply(false, false);
                File.WriteAllBytes(destination, composite.EncodeToPNG());
            }
            finally
            {
                if (idleSide != null) UnityEngine.Object.DestroyImmediate(idleSide);
                if (useSide != null) UnityEngine.Object.DestroyImmediate(useSide);
                if (idleClose != null) UnityEngine.Object.DestroyImmediate(idleClose);
                if (useClose != null) UnityEngine.Object.DestroyImmediate(useClose);
                if (composite != null) UnityEngine.Object.DestroyImmediate(composite);
            }

            Debug.Log("[VacuumCleanerGrip] " + logDescription + ". " +
                "Output=" + destination + ".");
        }

        internal static void StopReview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.ExitPlaymode();
            }
        }

        private static ReachPlan CalculateReachPlan(
            Transform target,
            GameObject model)
        {
            var previewObject = new GameObject("VacuumCleaner_ReachPreview")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(previewObject, target.gameObject.scene);
            Transform preview = previewObject.transform;
            preview.SetParent(target, false);
            preview.localPosition = Vector3.zero;
            preview.localRotation = Quaternion.identity;
            preview.localScale = Vector3.one;
            try
            {
                var instance = UnityEngine.Object.Instantiate(model);
                instance.hideFlags = HideFlags.HideAndDontSave;
                SceneManager.MoveGameObjectToScene(instance, target.gameObject.scene);
                instance.transform.SetParent(preview, false);
                instance.transform.localPosition = model.transform.localPosition;
                instance.transform.localRotation = model.transform.localRotation;
                instance.transform.localScale = model.transform.localScale;
                Vector3 sourceScale = instance.transform.localScale;
                float sourceHeight = CalculateVertexBoundsInSpace(preview, preview).size.y;
                if (sourceHeight <= 0.000001f)
                {
                    throw new InvalidOperationException(
                        "Vacuum cleaner source height is zero during reach planning.");
                }

                Vector3 initialRightLocal = EvaluateGripPointAtHeight(
                    preview,
                    instance.transform,
                    sourceScale,
                    sourceHeight,
                    InitialPropHeight,
                    true);
                Vector3 initialLeftLocal = EvaluateGripPointAtHeight(
                    preview,
                    instance.transform,
                    sourceScale,
                    sourceHeight,
                    InitialPropHeight,
                    false);
                Vector3 nextRightLocal = EvaluateGripPointAtHeight(
                    preview,
                    instance.transform,
                    sourceScale,
                    sourceHeight,
                    InitialPropHeight + 1f,
                    true);
                Vector3 nextLeftLocal = EvaluateGripPointAtHeight(
                    preview,
                    instance.transform,
                    sourceScale,
                    sourceHeight,
                    InitialPropHeight + 1f,
                    false);
                Vector3 initialRight = target.TransformPoint(initialRightLocal);
                Vector3 initialLeft = target.TransformPoint(initialLeftLocal);
                var right = CreateArmReachData(
                    target,
                    true,
                    initialRight,
                    target.TransformVector(nextRightLocal - initialRightLocal));
                var left = CreateArmReachData(
                    target,
                    false,
                    initialLeft,
                    target.TransformVector(nextLeftLocal - initialLeftLocal));
                return new ReachPlan
                {
                    InitialRightDistance = Vector3.Distance(
                        right.Shoulder,
                        right.AnchorAtInitialHeight),
                    InitialLeftDistance = Vector3.Distance(
                        left.Shoulder,
                        left.AnchorAtInitialHeight),
                    Right = right,
                    Left = left
                };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(previewObject);
            }
        }

        private static float CalculateChestAlignedPropHeight(
            Transform target,
            GameObject model)
        {
            float chestHeight = target.InverseTransformPoint(
                FindRequired(target, SpinePath).position).y;
            var previewObject = new GameObject("VacuumCleaner_ChestScalePreview")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(previewObject, target.gameObject.scene);
            Transform preview = previewObject.transform;
            preview.SetParent(target, false);
            preview.localPosition = Vector3.zero;
            preview.localRotation = Quaternion.identity;
            preview.localScale = Vector3.one;
            try
            {
                var instance = UnityEngine.Object.Instantiate(model);
                instance.hideFlags = HideFlags.HideAndDontSave;
                SceneManager.MoveGameObjectToScene(instance, target.gameObject.scene);
                instance.transform.SetParent(preview, false);
                instance.transform.localPosition = model.transform.localPosition;
                instance.transform.localRotation = model.transform.localRotation;
                instance.transform.localScale = model.transform.localScale;
                float sourceAxialHeight = CalculateVertexBoundsInSpace(
                    preview,
                    preview).size.y;
                float sourceRotatedHeight = CalculateRotatedVertexBounds(
                    preview,
                    Quaternion.Euler(-LeanDegrees, 0f, 0f)).size.y;
                if (sourceAxialHeight <= 0.000001f ||
                    sourceRotatedHeight <= 0.000001f)
                {
                    throw new InvalidOperationException(
                        "Vacuum cleaner chest scale bounds are invalid.");
                }

                return chestHeight * sourceAxialHeight / sourceRotatedHeight;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(previewObject);
            }
        }

        private static float CalculateCollisionFreePropCenterX(
            Transform target,
            GameObject model,
            float propHeight)
        {
            float hipsHeight = target.InverseTransformPoint(
                FindRequired(target, HipsPath).position).y;
            float chestHeight = target.InverseTransformPoint(
                FindRequired(target, SpinePath).position).y;
            float bandMinimumY = hipsHeight - 0.08f;
            float bandMaximumY = chestHeight + 0.08f;
            var previewObject = new GameObject("VacuumCleaner_ClearancePreview")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(previewObject, target.gameObject.scene);
            Transform preview = previewObject.transform;
            preview.SetParent(target, false);
            preview.localPosition = Vector3.zero;
            preview.localRotation = Quaternion.identity;
            preview.localScale = Vector3.one;
            try
            {
                var instance = UnityEngine.Object.Instantiate(model);
                instance.hideFlags = HideFlags.HideAndDontSave;
                SceneManager.MoveGameObjectToScene(instance, target.gameObject.scene);
                instance.transform.SetParent(preview, false);
                instance.transform.localPosition = model.transform.localPosition;
                instance.transform.localRotation = model.transform.localRotation;
                instance.transform.localScale = model.transform.localScale;
                Bounds sourceBounds = CalculateVertexBoundsInSpace(preview, preview);
                instance.transform.localScale *= propHeight / sourceBounds.size.y;
                Quaternion leanRotation = Quaternion.Euler(-LeanDegrees, 0f, 0f);
                Bounds rotatedBounds = CalculateRotatedVertexBounds(
                    preview,
                    leanRotation);
                preview.localRotation = leanRotation;
                preview.localPosition = new Vector3(
                    -rotatedBounds.center.x,
                    FloorYLocal - rotatedBounds.min.y,
                    DesiredPropCenterZLocal - rotatedBounds.center.z);

                const float cellSize = 0.025f;
                const float torsoHalfWidth = 0.38f;
                var bodyLeftByCell = new Dictionary<Vector2Int, float>();
                foreach (SkinnedMeshRenderer renderer in
                         target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (renderer.sharedMesh == null) continue;
                    var baked = new Mesh();
                    try
                    {
                        renderer.BakeMesh(baked, true);
                        Matrix4x4 matrix = target.worldToLocalMatrix *
                            renderer.transform.localToWorldMatrix;
                        Vector3[] vertices = baked.vertices;
                        BoneWeight[] weights = renderer.sharedMesh.boneWeights;
                        for (int vertexIndex = 0;
                             vertexIndex < vertices.Length;
                             vertexIndex++)
                        {
                            if (weights.Length == vertices.Length &&
                                IsArmWeightedVertex(renderer, weights[vertexIndex], target))
                            {
                                continue;
                            }

                            Vector3 point = matrix.MultiplyPoint3x4(
                                vertices[vertexIndex]);
                            if (point.y < bandMinimumY ||
                                point.y > bandMaximumY ||
                                point.x > 0f ||
                                point.x < -torsoHalfWidth)
                            {
                                continue;
                            }

                            Vector2Int cell = ClearanceCellYz(point, cellSize);
                            if (!bodyLeftByCell.TryGetValue(cell, out float leftX) ||
                                point.x < leftX)
                            {
                                bodyLeftByCell[cell] = point.x;
                            }
                        }
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(baked);
                    }
                }

                var propInnerByCell = new Dictionary<Vector2Int, float>();
                foreach (MeshFilter filter in
                         preview.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh == null) continue;
                    Matrix4x4 matrix = target.worldToLocalMatrix *
                        filter.transform.localToWorldMatrix;
                    foreach (Vector3 vertex in filter.sharedMesh.vertices)
                    {
                        Vector3 point = matrix.MultiplyPoint3x4(vertex);
                        if (point.y < bandMinimumY ||
                            point.y > bandMaximumY)
                        {
                            continue;
                        }

                        Vector2Int cell = ClearanceCellYz(point, cellSize);
                        if (!propInnerByCell.TryGetValue(cell, out float innerX) ||
                            point.x > innerX)
                        {
                            propInnerByCell[cell] = point.x;
                        }
                    }
                }

                if (bodyLeftByCell.Count == 0 || propInnerByCell.Count == 0)
                {
                    throw new InvalidOperationException(
                        "Vacuum cleaner torso-band clearance cells are unavailable.");
                }

                const float torsoClearance = 0.015f;
                float centerX = DesiredPropCenterXLocal;
                foreach (KeyValuePair<Vector2Int, float> propCell in propInnerByCell)
                {
                    bool hasNearbyBody = false;
                    float nearbyBodyLeft = float.PositiveInfinity;
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            var bodyCell = new Vector2Int(
                                propCell.Key.x + y,
                                propCell.Key.y + z);
                            if (bodyLeftByCell.TryGetValue(
                                    bodyCell,
                                    out float bodyLeft))
                            {
                                nearbyBodyLeft = Mathf.Min(
                                    nearbyBodyLeft,
                                    bodyLeft);
                                hasNearbyBody = true;
                            }
                        }
                    }

                    if (hasNearbyBody)
                    {
                        centerX = Mathf.Min(
                            centerX,
                            nearbyBodyLeft - propCell.Value - torsoClearance);
                    }
                }

                if (centerX < -0.60f)
                {
                    throw new InvalidOperationException(
                        "Vacuum cleaner collision-free center exceeds the allowed " +
                        "side placement. centerX=" + Num(centerX) + ".");
                }

                return centerX;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(previewObject);
            }
        }

        private static Vector2Int ClearanceCellYz(Vector3 point, float cellSize)
        {
            return new Vector2Int(
                Mathf.RoundToInt(point.y / cellSize),
                Mathf.RoundToInt(point.z / cellSize));
        }

        private static bool IsArmWeightedVertex(
            SkinnedMeshRenderer renderer,
            BoneWeight weight,
            Transform target)
        {
            float armWeight = 0f;
            armWeight += IsArmBone(renderer, weight.boneIndex0, target)
                ? weight.weight0
                : 0f;
            armWeight += IsArmBone(renderer, weight.boneIndex1, target)
                ? weight.weight1
                : 0f;
            armWeight += IsArmBone(renderer, weight.boneIndex2, target)
                ? weight.weight2
                : 0f;
            armWeight += IsArmBone(renderer, weight.boneIndex3, target)
                ? weight.weight3
                : 0f;
            return armWeight > 0.5f;
        }

        private static bool IsArmBone(
            SkinnedMeshRenderer renderer,
            int boneIndex,
            Transform target)
        {
            if (boneIndex < 0 || boneIndex >= renderer.bones.Length)
            {
                return false;
            }

            Transform bone = renderer.bones[boneIndex];
            if (bone == null)
            {
                return false;
            }

            string path = AnimationUtility.CalculateTransformPath(bone, target);
            return path == LeftShoulderPath ||
                   path.StartsWith(LeftShoulderPath + "/", StringComparison.Ordinal) ||
                   path == RightShoulderPath ||
                   path.StartsWith(RightShoulderPath + "/", StringComparison.Ordinal);
        }

        private static float CalculateTorsoFrontZ(
            Transform target,
            float minimumY,
            float maximumY)
        {
            bool initialized = false;
            float frontZ = 0f;
            foreach (SkinnedMeshRenderer renderer in
                     target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.sharedMesh == null) continue;
                var baked = new Mesh();
                try
                {
                    renderer.BakeMesh(baked, true);
                    Matrix4x4 matrix = target.worldToLocalMatrix *
                        renderer.transform.localToWorldMatrix;
                    foreach (Vector3 vertex in baked.vertices)
                    {
                        Vector3 point = matrix.MultiplyPoint3x4(vertex);
                        if (point.y < minimumY ||
                            point.y > maximumY ||
                            Mathf.Abs(point.x) > 0.30f)
                        {
                            continue;
                        }

                        if (!initialized || point.z > frontZ)
                        {
                            frontZ = point.z;
                            initialized = true;
                        }
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(baked);
                }
            }

            if (!initialized)
            {
                throw new InvalidOperationException(
                    target.name + " has no torso-band vertices.");
            }

            return frontZ;
        }

        private static Vector3 EvaluateGripPointAtHeight(
            Transform preview,
            Transform instance,
            Vector3 sourceScale,
            float sourceHeight,
            float propHeight,
            bool right)
        {
            instance.localScale = sourceScale * (propHeight / sourceHeight);
            Bounds localBounds = CalculateVertexBoundsInSpace(preview, preview);
            Vector3 gripPoint = GripPointLocal(preview, localBounds, right);
            Quaternion leanRotation = Quaternion.Euler(-LeanDegrees, 0f, 0f);
            Bounds rotatedBounds = CalculateRotatedVertexBounds(preview, leanRotation);
            Vector3 placement = new Vector3(
                DesiredPropCenterXLocal - rotatedBounds.center.x,
                FloorYLocal - rotatedBounds.min.y,
                DesiredPropCenterZLocal - rotatedBounds.center.z);
            return placement + leanRotation * gripPoint;
        }

        private static ArmReachData CreateArmReachData(
            Transform target,
            bool right,
            Vector3 anchorAtInitialHeight,
            Vector3 anchorSlope)
        {
            Transform shoulder = FindRequired(
                target,
                right ? RightShoulderPath : LeftShoulderPath);
            Transform upper = FindRequired(target, right ? RightArmPath : LeftArmPath);
            Transform lower = FindRequired(target, right ? RightForeArmPath : LeftForeArmPath);
            Transform hand = FindRequired(target, right ? RightHandPath : LeftHandPath);
            float shoulderLength = Vector3.Distance(shoulder.position, upper.position);
            float upperLength = Vector3.Distance(upper.position, lower.position);
            float lowerLength = Vector3.Distance(lower.position, hand.position);
            Vector3 gripAxis = target.TransformDirection(
                Quaternion.Euler(-LeanDegrees, 0f, 0f) * Vector3.up).normalized;
            Vector3 wristFromPalm = CalculateWristFromPalmForGrip(
                target,
                upper,
                hand,
                right,
                gripAxis);
            return new ArmReachData
            {
                Name = target.name + (right ? "/Right" : "/Left"),
                Shoulder = shoulder.position,
                AnchorAtInitialHeight = anchorAtInitialHeight + wristFromPalm,
                AnchorSlope = anchorSlope,
                DepthDirection = target.TransformVector(Vector3.forward),
                MinimumReach = 0.05f,
                MaximumReach = shoulderLength + ReachForElbowAngle(
                    upperLength,
                    lowerLength,
                    PlannedMaximumElbowDegrees)
            };
        }

        private static Vector3 CalculateWristFromPalmForGrip(
            Transform target,
            Transform upper,
            Transform hand,
            bool right,
            Vector3 gripAxis)
        {
            Transform[] transforms = upper.GetComponentsInChildren<Transform>(true);
            Vector3[] positions = transforms
                .Select(transform => transform.localPosition)
                .ToArray();
            Quaternion[] rotations = transforms
                .Select(transform => transform.localRotation)
                .ToArray();
            Vector3[] scales = transforms
                .Select(transform => transform.localScale)
                .ToArray();
            try
            {
                AlignHandForGripAxis(target, hand, right, gripAxis);
                CurlFingers(hand, right);
                return hand.position - CalculateHandPalmCenter(target, hand);
            }
            finally
            {
                for (int index = 0; index < transforms.Length; index++)
                {
                    transforms[index].localPosition = positions[index];
                    transforms[index].localRotation = rotations[index];
                    transforms[index].localScale = scales[index];
                }
            }
        }

        private static float ReachForElbowAngle(
            float upperLength,
            float lowerLength,
            float elbowDegrees)
        {
            return Mathf.Sqrt(Mathf.Max(
                0f,
                upperLength * upperLength + lowerLength * lowerLength -
                2f * upperLength * lowerLength *
                Mathf.Cos(elbowDegrees * Mathf.Deg2Rad)));
        }

        private static LayoutPlan CalculateMinimumNaturalLayout(
            params ArmReachData[] arms)
        {
            float heightLower = InitialPropHeight;
            float heightUpper = float.PositiveInfinity;
            foreach (ArmReachData arm in arms)
            {
                if (!TryGetHeightIntervalAllowingDepth(arm, out ReachInterval interval))
                {
                    throw new InvalidOperationException(
                        arm.Name + " cannot reach the diagonal vacuum at any scale.");
                }

                heightLower = Mathf.Max(heightLower, interval.Minimum);
                heightUpper = Mathf.Min(heightUpper, interval.Maximum);
            }

            if (heightLower > heightUpper)
            {
                throw new InvalidOperationException(
                    "Vacuum targets have no shared scale range for a natural two-hand grip.");
            }

            float solutionHeight = heightLower;
            if (!TryGetCommonDepthInterval(
                    solutionHeight,
                    arms,
                    out float depthLower,
                    out float depthUpper))
            {
                const float goldenRatio = 1.61803398875f;
                float searchLower = heightLower;
                float searchUpper = heightUpper;
                for (int iteration = 0; iteration < 72; iteration++)
                {
                    float left = searchUpper -
                        (searchUpper - searchLower) / goldenRatio;
                    float right = searchLower +
                        (searchUpper - searchLower) / goldenRatio;
                    if (CommonDepthGap(left, arms) <= CommonDepthGap(right, arms))
                    {
                        searchUpper = right;
                    }
                    else
                    {
                        searchLower = left;
                    }
                }

                float feasibleHeight = (searchLower + searchUpper) * 0.5f;
                if (!TryGetCommonDepthInterval(
                        feasibleHeight,
                        arms,
                        out depthLower,
                        out depthUpper))
                {
                    throw new InvalidOperationException(
                        "Vacuum targets have no common front placement for a natural " +
                        "two-hand grip. " + string.Join("; ", arms.Select(arm =>
                            arm.DescribeAtHeight(InitialPropHeight))));
                }

                float infeasibleHeight = heightLower;
                for (int iteration = 0; iteration < 72; iteration++)
                {
                    float middle = (infeasibleHeight + feasibleHeight) * 0.5f;
                    if (TryGetCommonDepthInterval(
                            middle,
                            arms,
                            out _,
                            out _))
                    {
                        feasibleHeight = middle;
                    }
                    else
                    {
                        infeasibleHeight = middle;
                    }
                }

                solutionHeight = feasibleHeight;
                if (!TryGetCommonDepthInterval(
                        solutionHeight,
                        arms,
                        out depthLower,
                        out depthUpper))
                {
                    throw new InvalidOperationException(
                        "Vacuum grip layout lost its calculated depth interval.");
                }
            }

            float depthOffset = (depthLower + depthUpper) * 0.5f;
            if (arms.Any(arm =>
                    !arm.IsNaturalAtLayout(solutionHeight, depthOffset)))
            {
                throw new InvalidOperationException(
                    "Vacuum grip layout does not satisfy the natural elbow range.");
            }

            return new LayoutPlan
            {
                PropHeight = Mathf.Max(InitialPropHeight, solutionHeight),
                PropCenterZ = DesiredPropCenterZLocal + depthOffset
            };
        }

        private static bool TryGetHeightIntervalAllowingDepth(
            ArmReachData arm,
            out ReachInterval interval)
        {
            Vector3 depth = arm.DepthDirection;
            float depthSquared = Vector3.Dot(depth, depth);
            Vector3 offset = arm.AnchorAtInitialHeight - arm.Shoulder -
                arm.AnchorSlope * InitialPropHeight;
            Vector3 projectedOffset = offset -
                depth * (Vector3.Dot(offset, depth) / depthSquared);
            Vector3 projectedSlope = arm.AnchorSlope -
                depth * (Vector3.Dot(arm.AnchorSlope, depth) / depthSquared);
            return TrySolveWithinRadiusInterval(
                projectedOffset,
                projectedSlope,
                arm.MaximumReach,
                out interval);
        }

        private static bool TryGetCommonDepthInterval(
            float height,
            ArmReachData[] arms,
            out float commonMinimum,
            out float commonMaximum)
        {
            commonMinimum = MinimumPropCenterZLocal - DesiredPropCenterZLocal;
            commonMaximum = MaximumPropCenterZLocal - DesiredPropCenterZLocal;
            foreach (ArmReachData arm in arms)
            {
                Vector3 offset = arm.AnchorAtInitialHeight - arm.Shoulder +
                    arm.AnchorSlope * (height - InitialPropHeight);
                if (!TrySolveWithinRadiusInterval(
                        offset,
                        arm.DepthDirection,
                        arm.MaximumReach,
                        out ReachInterval interval))
                {
                    return false;
                }

                commonMinimum = Mathf.Max(commonMinimum, interval.Minimum);
                commonMaximum = Mathf.Min(commonMaximum, interval.Maximum);
            }

            return commonMinimum <= commonMaximum + 0.00001f;
        }

        private static float CommonDepthGap(float height, ArmReachData[] arms)
        {
            TryGetCommonDepthInterval(
                height,
                arms,
                out float minimum,
                out float maximum);
            return minimum - maximum;
        }

        private static bool TrySolveWithinRadiusInterval(
            Vector3 offset,
            Vector3 slope,
            float radius,
            out ReachInterval interval)
        {
            float a = Vector3.Dot(slope, slope);
            float b = 2f * Vector3.Dot(offset, slope);
            float c = Vector3.Dot(offset, offset) - radius * radius;
            if (a <= 0.00000001f)
            {
                interval = new ReachInterval
                {
                    Minimum = float.NegativeInfinity,
                    Maximum = float.PositiveInfinity
                };
                return c <= 0f;
            }

            float discriminant = b * b - 4f * a * c;
            if (discriminant < 0f)
            {
                interval = default;
                return false;
            }

            float root = Mathf.Sqrt(discriminant);
            interval = new ReachInterval
            {
                Minimum = (-b - root) / (2f * a),
                Maximum = (-b + root) / (2f * a)
            };
            return true;
        }

        private static GripMetrics ApplyTarget(
            Transform target,
            GameObject model,
            Material appearance,
            float propHeight,
            float propCenterX,
            float propCenterZ)
        {
            RestoreArmDefaults(target);
            Transform existing = Enumerable.Range(0, target.childCount)
                .Select(target.GetChild)
                .SingleOrDefault(child => child.name == HolderName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            var holderObject = new GameObject(HolderName);
            Undo.RegisterCreatedObjectUndo(holderObject, "Create vacuum cleaner holder");
            Transform holder = holderObject.transform;
            holder.SetParent(target, false);
            holder.localPosition = Vector3.zero;
            holder.localRotation = Quaternion.identity;
            holder.localScale = Vector3.one;

            var instance = PrefabUtility.InstantiatePrefab(
                model,
                target.gameObject.scene) as GameObject ??
                throw new InvalidOperationException("Vacuum cleaner FBX instantiation failed.");
            Undo.RegisterCreatedObjectUndo(instance, "Create vacuum cleaner model");
            instance.name = ModelInstanceName;
            instance.transform.SetParent(holder, false);
            instance.transform.localPosition = model.transform.localPosition;
            instance.transform.localRotation = model.transform.localRotation;
            instance.transform.localScale = model.transform.localScale;

            Bounds sourceBounds = CalculateVertexBoundsInSpace(holder, holder);
            float uniformScale = propHeight / sourceBounds.size.y;
            instance.transform.localScale *= uniformScale;
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Undo.RecordObject(renderer, "Apply vacuum cleaner embedded material");
                Material[] materials = renderer.sharedMaterials;
                for (int index = 0; index < materials.Length; index++)
                {
                    materials[index] = appearance;
                }

                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }

            Bounds propLocalBounds = CalculateVertexBoundsInSpace(holder, holder);
            Vector3 rightGripLocal = GripPointLocal(
                holder,
                propLocalBounds,
                true);
            Vector3 leftGripLocal = GripPointLocal(
                holder,
                propLocalBounds,
                false);
            Quaternion leanRotation = Quaternion.Euler(-LeanDegrees, 0f, 0f);
            Bounds rotatedBounds = CalculateRotatedVertexBounds(holder, leanRotation);
            holder.localRotation = leanRotation;
            holder.localPosition = new Vector3(
                propCenterX - rotatedBounds.center.x,
                FloorYLocal - rotatedBounds.min.y,
                propCenterZ - rotatedBounds.center.z);
            Transform rightAnchor = CreateLocalAnchor(
                holder,
                rightGripLocal,
                RightGripAnchorName);
            Transform leftAnchor = CreateLocalAnchor(
                holder,
                leftGripLocal,
                LeftGripAnchorName);

            PoseArmAtGrip(
                target,
                true,
                rightAnchor.position,
                target.TransformPoint(new Vector3(0.46f, 1.20f, 0.25f)),
                holder.up);
            PoseArmAtGrip(
                target,
                false,
                leftAnchor.position,
                target.TransformPoint(new Vector3(-0.46f, 0.96f, 0.35f)),
                holder.up);

            EditorUtility.SetDirty(holderObject);
            EditorUtility.SetDirty(instance);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            return InspectTarget(target);
        }

        private static Vector3 GripPointLocal(
            Transform root,
            Bounds bounds,
            bool right)
        {
            float height01 = right ? RightGripHeight01 : LeftGripHeight01;
            float xOffset01 = right ? 0.34f : -0.15f;
            return GripPointLocal(root, bounds, height01, xOffset01);
        }

        private static Vector3 GripPointLocal(
            Transform root,
            Bounds bounds,
            float height01,
            float xOffset01)
        {
            float targetY = bounds.min.y + bounds.size.y * height01;
            float targetX = bounds.center.x + bounds.size.x * xOffset01;
            Bounds slice = CalculateGripSliceBounds(
                root,
                targetX,
                targetY,
                bounds.size);
            return new Vector3(
                slice.center.x,
                targetY,
                Mathf.Lerp(slice.min.z, slice.max.z, 0.30f));
        }

        private static Bounds CalculateGripSliceBounds(
            Transform root,
            float targetX,
            float targetY,
            Vector3 propSize)
        {
            float yTolerance = propSize.y * 0.04f;
            float xTolerance = propSize.x * 0.18f;
            var heightSlice = new System.Collections.Generic.List<Vector3>();
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                Matrix4x4 matrix = root.worldToLocalMatrix *
                    filter.transform.localToWorldMatrix;
                foreach (Vector3 vertex in filter.sharedMesh.vertices)
                {
                    Vector3 point = matrix.MultiplyPoint3x4(vertex);
                    if (Mathf.Abs(point.y - targetY) <= yTolerance)
                    {
                        heightSlice.Add(point);
                    }
                }
            }

            Vector3[] focused = heightSlice
                .Where(point => Mathf.Abs(point.x - targetX) <= xTolerance)
                .ToArray();
            Vector3[] points = focused.Length > 0
                ? focused
                : heightSlice.ToArray();
            if (points.Length == 0)
            {
                throw new InvalidOperationException(
                    "Vacuum cleaner grip slice has no mesh vertices.");
            }

            var result = new Bounds(points[0], Vector3.zero);
            foreach (Vector3 point in points.Skip(1))
            {
                result.Encapsulate(point);
            }

            return result;
        }

        private static void RestoreArmDefaults(Transform target)
        {
            foreach (string path in new[] { LeftShoulderPath, RightShoulderPath })
            {
                Transform arm = FindRequired(target, path);
                Transform[] transforms = arm.GetComponentsInChildren<Transform>(true);
                Undo.RecordObjects(
                    transforms.Cast<UnityEngine.Object>().ToArray(),
                    "Restore source arm pose before vacuum grip");
                foreach (Transform transform in transforms)
                {
                    Transform source =
                        PrefabUtility.GetCorrespondingObjectFromSource(transform) as Transform;
                    if (source == null)
                    {
                        throw new InvalidOperationException(
                            target.name + " arm transform has no source counterpart: " +
                            transform.name + ".");
                    }

                    transform.localPosition = source.localPosition;
                    transform.localRotation = source.localRotation;
                    transform.localScale = source.localScale;
                    EditorUtility.SetDirty(transform);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(transform);
                }
            }
        }

        private static Transform CreateLocalAnchor(
            Transform holder,
            Vector3 localPosition,
            string name)
        {
            var anchorObject = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(anchorObject, "Create " + name);
            Transform anchor = anchorObject.transform;
            anchor.SetParent(holder, false);
            anchor.localPosition = localPosition;
            anchor.localRotation = Quaternion.identity;
            return anchor;
        }

        private static void PoseArmAtGrip(
            Transform root,
            bool right,
            Vector3 gripPoint,
            Vector3 elbowPole,
            Vector3 gripAxis,
            bool alignWristWithForearm = false)
        {
            string upperPath = right ? RightArmPath : LeftArmPath;
            string lowerPath = right ? RightForeArmPath : LeftForeArmPath;
            string handPath = right ? RightHandPath : LeftHandPath;
            string shoulderPath = right ? RightShoulderPath : LeftShoulderPath;
            Transform shoulder = FindRequired(root, shoulderPath);
            Transform upper = FindRequired(root, upperPath);
            Transform lower = FindRequired(root, lowerPath);
            Transform hand = FindRequired(root, handPath);
            Transform shoulderSource =
                PrefabUtility.GetCorrespondingObjectFromSource(shoulder) as Transform ??
                throw new InvalidOperationException(
                    shoulder.name + " has no source counterpart.");
            Vector3 sourceShoulderPosition = shoulder.parent.TransformPoint(
                shoulderSource.localPosition);
            Transform[] changed = shoulder.GetComponentsInChildren<Transform>(true)
                .Concat(new[] { shoulder, upper, lower, hand })
                .Distinct()
                .ToArray();
            Undo.RecordObjects(changed.Cast<UnityEngine.Object>().ToArray(),
                "Pose vacuum cleaner " + (right ? "right" : "left") + " grip");

            AlignHandForGrip(
                root,
                lower,
                hand,
                right,
                gripAxis,
                alignWristWithForearm);
            CurlFingers(hand, right);
            const int maximumPasses = 24;
            for (int pass = 0; pass < maximumPasses; pass++)
            {
                AlignHandForGrip(
                    root,
                    lower,
                    hand,
                    right,
                    gripAxis,
                    alignWristWithForearm);
                Vector3 palmCenter = CalculateHandPalmCenter(root, hand);
                if (Vector3.Distance(palmCenter, gripPoint) <= 0.002f)
                {
                    break;
                }

                Vector3 wristTarget = hand.position + gripPoint - palmCenter;
                MoveShoulderWithinReach(
                    shoulder,
                    upper,
                    lower,
                    hand,
                    wristTarget,
                    sourceShoulderPosition);
                AimShoulderForReach(shoulder, upper, lower, hand, wristTarget);
                SolveTwoBoneIk(upper, lower, hand, wristTarget, elbowPole);
            }

            AlignHandForGrip(
                root,
                lower,
                hand,
                right,
                gripAxis,
                alignWristWithForearm);
            foreach (Transform transform in changed)
            {
                EditorUtility.SetDirty(transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(transform);
            }
        }

        private static void AlignHandForGrip(
            Transform root,
            Transform lower,
            Transform hand,
            bool right,
            Vector3 gripAxis,
            bool alignWristWithForearm)
        {
            if (alignWristWithForearm)
            {
                AlignHandWithForearmForGrip(hand, lower, right, gripAxis);
                return;
            }

            AlignHandForGripAxis(root, hand, right, gripAxis);
        }

        private static void AlignHandWithForearmForGrip(
            Transform hand,
            Transform lower,
            bool right,
            Vector3 requestedGripAxis)
        {
            Transform middle = RequireDescendant(
                hand,
                (right ? "Right" : "Left") + "MiddleProximal");
            Transform index = RequireDescendant(
                hand,
                (right ? "Right" : "Left") + "IndexProximal");
            Transform little = RequireDescendant(
                hand,
                (right ? "Right" : "Left") + "LittleProximal");
            Vector3 localLongitudinal = hand.InverseTransformDirection(
                (middle.position - hand.position).normalized);
            Vector3 localAcross = hand.InverseTransformDirection(
                (index.position - little.position).normalized);
            localAcross = Vector3.ProjectOnPlane(
                localAcross,
                localLongitudinal).normalized;
            Vector3 desiredLongitudinal = (hand.position - lower.position).normalized;
            Vector3 desiredAcross = Vector3.ProjectOnPlane(
                requestedGripAxis,
                desiredLongitudinal).normalized;
            if (localLongitudinal.sqrMagnitude < 0.99f ||
                localAcross.sqrMagnitude < 0.99f ||
                desiredLongitudinal.sqrMagnitude < 0.99f ||
                desiredAcross.sqrMagnitude < 0.99f)
            {
                throw new InvalidOperationException(
                    hand.name + " has no stable neutral grip basis.");
            }

            Vector3 localUp = Vector3.Cross(
                localLongitudinal,
                localAcross).normalized;
            Quaternion localFrame = Quaternion.LookRotation(
                localLongitudinal,
                localUp);
            Quaternion candidateA = Quaternion.LookRotation(
                desiredLongitudinal,
                Vector3.Cross(desiredLongitudinal, desiredAcross).normalized) *
                Quaternion.Inverse(localFrame);
            desiredAcross = -desiredAcross;
            Quaternion candidateB = Quaternion.LookRotation(
                desiredLongitudinal,
                Vector3.Cross(desiredLongitudinal, desiredAcross).normalized) *
                Quaternion.Inverse(localFrame);
            hand.rotation = Quaternion.Angle(hand.rotation, candidateA) <=
                Quaternion.Angle(hand.rotation, candidateB)
                ? candidateA
                : candidateB;
        }

        private static void MoveShoulderWithinReach(
            Transform shoulder,
            Transform upper,
            Transform lower,
            Transform hand,
            Vector3 wristTarget,
            Vector3 sourceShoulderPosition)
        {
            float shoulderLength = Vector3.Distance(shoulder.position, upper.position);
            float upperLength = Vector3.Distance(upper.position, lower.position);
            float lowerLength = Vector3.Distance(lower.position, hand.position);
            float maximumReach = shoulderLength + ReachForElbowAngle(
                upperLength,
                lowerLength,
                MaximumElbowDegrees);
            Vector3 targetVector = wristTarget - shoulder.position;
            float excess = targetVector.magnitude - maximumReach;
            if (excess <= 0f)
            {
                return;
            }

            shoulder.position += targetVector.normalized * (excess + 0.001f);
            float translation = Vector3.Distance(
                sourceShoulderPosition,
                shoulder.position);
            if (translation > MaximumShoulderTranslation)
            {
                throw new InvalidOperationException(
                    shoulder.name + " requires excessive position adjustment. " +
                    "translation=" + Num(translation) + ".");
            }
        }

        private static void AimShoulderForReach(
            Transform shoulder,
            Transform upper,
            Transform lower,
            Transform hand,
            Vector3 wristTarget)
        {
            Vector3 current = upper.position - shoulder.position;
            Vector3 desired = wristTarget - shoulder.position;
            if (current.sqrMagnitude <= 0.0000001f ||
                desired.sqrMagnitude <= 0.0000001f)
            {
                throw new InvalidOperationException(
                    shoulder.name + " has no stable reach direction.");
            }

            shoulder.rotation = Quaternion.FromToRotation(current, desired) *
                shoulder.rotation;
        }

        private static void AlignHandForGripAxis(
            Transform root,
            Transform hand,
            bool right,
            Vector3 requestedGripAxis)
        {
            Vector3 gripAxis = requestedGripAxis.normalized;
            Vector3 inward = Vector3.ProjectOnPlane(
                right ? -root.right : root.right,
                gripAxis).normalized;
            if (inward.sqrMagnitude < 0.99f)
            {
                throw new InvalidOperationException(
                    root.name + " hand has no stable inward direction.");
            }

            Vector3 handRight = right ? inward : -inward;
            Vector3 fingerDirection = Vector3.Cross(gripAxis, handRight).normalized;
            hand.rotation = Quaternion.LookRotation(gripAxis, fingerDirection);
            hand.rotation = Quaternion.AngleAxis(
                right ? -8f : 8f,
                gripAxis) * hand.rotation;
        }

        private static void CurlFingers(Transform hand, bool right)
        {
            string side = right ? "Right" : "Left";
            foreach (string finger in new[] { "Thumb", "Index", "Middle", "Ring", "Little" })
            {
                Transform[] bones = new[] { "Proximal", "Intermediate", "Distal" }
                    .Select(joint => RequireDescendant(hand, side + finger + joint))
                    .ToArray();
                float[] angles = finger == "Thumb"
                    ? new[] { 38f, 58f, 46f }
                    : new[] { 72f, 88f, 68f };
                for (int index = 0; index < bones.Length; index++)
                {
                    Transform bone = bones[index];
                    Vector3 direction = index < bones.Length - 1
                        ? (bones[index + 1].position - bone.position).normalized
                        : bone.up;
                    Vector3 palm = hand.TransformDirection(
                        right ? Vector3.left : Vector3.right);
                    Vector3 axis = Vector3.Cross(direction, palm).normalized;
                    if (axis.sqrMagnitude < 0.99f)
                    {
                        throw new InvalidOperationException(
                            bone.name + " has no stable curl axis.");
                    }

                    bone.rotation = Quaternion.AngleAxis(angles[index], axis) *
                        bone.rotation;
                }
            }
        }

        private static float SolveTwoBoneIk(
            Transform upper,
            Transform lower,
            Transform hand,
            Vector3 requestedTarget,
            Vector3 pole)
        {
            Vector3 rootPosition = upper.position;
            float upperLength = Vector3.Distance(rootPosition, lower.position);
            float lowerLength = Vector3.Distance(lower.position, hand.position);
            float minimumReach = Mathf.Abs(upperLength - lowerLength) + 0.0001f;
            float maximumReach = upperLength + lowerLength - 0.0001f;
            Vector3 targetVector = requestedTarget - rootPosition;
            Vector3 direction = targetVector.sqrMagnitude > 0.0000001f
                ? targetVector.normalized
                : (hand.position - rootPosition).normalized;
            float reach = Mathf.Clamp(targetVector.magnitude, minimumReach, maximumReach);
            Vector3 target = rootPosition + direction * reach;
            Vector3 bendDirection = Vector3.ProjectOnPlane(
                pole - rootPosition,
                direction);
            if (bendDirection.sqrMagnitude < 0.0000001f)
            {
                bendDirection = Vector3.ProjectOnPlane(
                    lower.position - rootPosition,
                    direction);
            }

            bendDirection.Normalize();
            float along =
                (upperLength * upperLength + reach * reach - lowerLength * lowerLength) /
                (2f * reach);
            float perpendicular = Mathf.Sqrt(
                Mathf.Max(0f, upperLength * upperLength - along * along));
            Vector3 elbowTarget =
                rootPosition + direction * along + bendDirection * perpendicular;
            upper.rotation = Quaternion.FromToRotation(
                lower.position - rootPosition,
                elbowTarget - rootPosition) * upper.rotation;
            Vector3 elbowPosition = lower.position;
            lower.rotation = Quaternion.FromToRotation(
                hand.position - elbowPosition,
                target - elbowPosition) * lower.rotation;
            return Vector3.Distance(hand.position, requestedTarget);
        }

        private static Vector3 CalculateHandPalmCenter(
            Transform target,
            Transform hand)
        {
            Vector3 weightedPosition = Vector3.zero;
            float totalWeight = 0f;
            foreach (SkinnedMeshRenderer renderer in
                     target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh mesh = renderer.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                Transform[] bones = renderer.bones;
                int[] indices = Enumerable.Range(0, bones.Length)
                    .Where(index => bones[index] == hand ||
                                    (bones[index] != null && bones[index].IsChildOf(hand)))
                    .ToArray();
                if (indices.Length == 0)
                {
                    continue;
                }

                var indexSet = indices.ToHashSet();
                BoneWeight[] weights = mesh.boneWeights;
                var baked = new Mesh();
                try
                {
                    renderer.BakeMesh(baked, true);
                    Vector3[] vertices = baked.vertices;
                    for (int vertex = 0; vertex < vertices.Length; vertex++)
                    {
                        float weight = BoneWeightForIndices(weights[vertex], indexSet);
                        if (weight < 0.5f)
                        {
                            continue;
                        }

                        Vector3 world = renderer.transform.TransformPoint(vertices[vertex]);
                        weightedPosition += world * weight;
                        totalWeight += weight;
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(baked);
                }
            }

            if (totalWeight <= 0f)
            {
                throw new InvalidOperationException(
                    target.name + " has no hand-weighted visible vertices.");
            }

            return weightedPosition / totalWeight;
        }

        private static float BoneWeightForIndices(
            BoneWeight weight,
            System.Collections.Generic.HashSet<int> indices)
        {
            float result = 0f;
            if (indices.Contains(weight.boneIndex0)) result += weight.weight0;
            if (indices.Contains(weight.boneIndex1)) result += weight.weight1;
            if (indices.Contains(weight.boneIndex2)) result += weight.weight2;
            if (indices.Contains(weight.boneIndex3)) result += weight.weight3;
            return result;
        }

        private static GripMetrics InspectTarget(Transform target)
        {
            Transform holder = Enumerable.Range(0, target.childCount)
                .Select(target.GetChild)
                .SingleOrDefault(child => child.name == HolderName) ??
                throw new InvalidOperationException(target.name + " has no vacuum holder.");
            Transform model = Enumerable.Range(0, holder.childCount)
                .Select(holder.GetChild)
                .SingleOrDefault(child => child.name == ModelInstanceName) ??
                throw new InvalidOperationException(target.name + " has no vacuum model.");
            Transform rightAnchor = holder.Find(RightGripAnchorName) ??
                throw new InvalidOperationException(target.name + " has no right grip anchor.");
            Transform leftAnchor = holder.Find(LeftGripAnchorName) ??
                throw new InvalidOperationException(target.name + " has no left grip anchor.");
            Transform rightHand = FindRequired(target, RightHandPath);
            Transform leftHand = FindRequired(target, LeftHandPath);
            Transform rightArm = FindRequired(target, RightArmPath);
            Transform rightForeArm = FindRequired(target, RightForeArmPath);
            Transform leftArm = FindRequired(target, LeftArmPath);
            Transform leftForeArm = FindRequired(target, LeftForeArmPath);
            Bounds bounds = BoundsInTargetSpace(target, holder);
            Bounds localBounds = CalculateVertexBoundsInSpace(holder, holder);
            string modelAssetPath = AssetDatabase.GetAssetPath(
                model.GetComponentInChildren<MeshFilter>(true)?.sharedMesh);
            Material[] materials = model.GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .Distinct()
                .ToArray();
            string[] materialPaths = materials
                .Select(AssetDatabase.GetAssetPath)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            string[] texturePaths = materials
                .SelectMany(material => material.GetTexturePropertyNames()
                    .Select(material.GetTexture))
                .Where(texture => texture != null)
                .Select(AssetDatabase.GetAssetPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            return new GripMetrics
            {
                Target = target.name,
                ModelAssetPath = modelAssetPath,
                MaterialPaths = materialPaths,
                TexturePaths = texturePaths,
                PropBoundsCenterLocal = bounds.center,
                PropBoundsSizeLocal = bounds.size,
                AxialHeight = localBounds.size.y,
                PropTopYLocal = bounds.max.y,
                ChestHeightLocal = target.InverseTransformPoint(
                    FindRequired(target, SpinePath).position).y,
                FloorGap = bounds.min.y - FloorYLocal,
                LeanDegrees = Vector3.Angle(holder.up, target.up),
                RightPalmDistance = Vector3.Distance(
                    CalculateHandPalmCenter(target, rightHand),
                    rightAnchor.position),
                LeftPalmDistance = Vector3.Distance(
                    CalculateHandPalmCenter(target, leftHand),
                    leftAnchor.position),
                RightElbowDegrees = ElbowDegrees(rightArm, rightForeArm, rightHand),
                LeftElbowDegrees = ElbowDegrees(leftArm, leftForeArm, leftHand),
                RightShoulderTranslation = ShoulderTranslationFromSource(
                    target,
                    RightShoulderPath),
                LeftShoulderTranslation = ShoulderTranslationFromSource(
                    target,
                    LeftShoulderPath),
                RightGripHeight01 = Mathf.InverseLerp(
                    localBounds.min.y,
                    localBounds.max.y,
                    rightAnchor.localPosition.y),
                LeftGripHeight01 = Mathf.InverseLerp(
                    localBounds.min.y,
                    localBounds.max.y,
                    leftAnchor.localPosition.y),
                ModelInstanceCount = holder
                    .GetComponentsInChildren<MeshFilter>(true).Length
            };
        }

        private static void RequireGripMetrics(GripMetrics metrics)
        {
            if (!string.Equals(metrics.ModelAssetPath, ModelPath, StringComparison.Ordinal) ||
                metrics.ModelInstanceCount != 1 ||
                Mathf.Abs(
                    metrics.PropBoundsCenterLocal.x -
                    DesiredPropCenterXLocal) > PositionTolerance ||
                metrics.PropBoundsCenterLocal.z <
                MinimumPropCenterZLocal - PositionTolerance ||
                metrics.PropBoundsCenterLocal.z >
                MaximumPropCenterZLocal + PositionTolerance ||
                Mathf.Abs(metrics.FloorGap) > PositionTolerance ||
                Mathf.Abs(metrics.LeanDegrees - LeanDegrees) > 0.1f ||
                Mathf.Abs(metrics.PropTopYLocal - metrics.ChestHeightLocal) >
                PositionTolerance ||
                metrics.AxialHeight < InitialPropHeight - PositionTolerance ||
                metrics.PropBoundsSizeLocal.x < 0.29f ||
                metrics.RightPalmDistance > 0.012f ||
                metrics.LeftPalmDistance > 0.012f ||
                Mathf.Abs(metrics.RightGripHeight01 - RightGripHeight01) > 0.001f ||
                Mathf.Abs(metrics.LeftGripHeight01 - LeftGripHeight01) > 0.001f ||
                metrics.MaterialPaths.Length != 1 ||
                metrics.MaterialPaths[0] != MaterialPath ||
                metrics.TexturePaths.Length == 0 ||
                metrics.TexturePaths.Any(path =>
                    !path.StartsWith(TextureFolder + "/", StringComparison.Ordinal)) ||
                metrics.RightElbowDegrees < 10f ||
                metrics.RightElbowDegrees > MaximumElbowDegrees + 0.5f ||
                metrics.LeftElbowDegrees < 10f ||
                metrics.LeftElbowDegrees > MaximumElbowDegrees + 0.5f ||
                metrics.RightShoulderTranslation > MaximumShoulderTranslation +
                PositionTolerance ||
                metrics.LeftShoulderTranslation > MaximumShoulderTranslation +
                PositionTolerance)
            {
                throw new InvalidOperationException(
                    metrics.Target + " vacuum grip metrics failed. " +
                    DescribeMetricSummary(metrics));
            }
        }

        private static float ShoulderTranslationFromSource(
            Transform target,
            string path)
        {
            Transform shoulder = FindRequired(target, path);
            Vector3 sourceLocalPosition = path == RightShoulderPath
                ? RightShoulderSourceLocalPosition
                : LeftShoulderSourceLocalPosition;
            return Vector3.Distance(
                shoulder.localPosition,
                sourceLocalPosition);
        }

        private static void RequireMatchingTargetLayout(
            GripMetrics idle,
            GripMetrics use)
        {
            if (Mathf.Abs(idle.AxialHeight - use.AxialHeight) > PositionTolerance ||
                Mathf.Abs(
                    idle.PropBoundsCenterLocal.x -
                    use.PropBoundsCenterLocal.x) > PositionTolerance ||
                Mathf.Abs(
                    idle.PropBoundsCenterLocal.z -
                    use.PropBoundsCenterLocal.z) > PositionTolerance ||
                Mathf.Abs(idle.FloorGap - use.FloorGap) > PositionTolerance ||
                Mathf.Abs(idle.LeanDegrees - use.LeanDegrees) > 0.1f)
            {
                throw new InvalidOperationException(
                    "Vacuum_Idle and Vacuum_Use do not share the same prop layout.");
            }
        }

        private static float ElbowDegrees(
            Transform upper,
            Transform lower,
            Transform hand)
        {
            return Vector3.Angle(upper.position - lower.position, hand.position - lower.position);
        }

        private static string DescribeMetrics(
            string title,
            GripMetrics idle,
            GripMetrics use,
            string finalLine)
        {
            var report = new StringBuilder();
            report.AppendLine(title);
            report.AppendLine(DescribeMetricSummary(idle));
            report.AppendLine(DescribeMetricSummary(use));
            report.AppendLine("sourceFbxShapePivotBytesModified=False");
            report.AppendLine(finalLine);
            return report.ToString();
        }

        private static string DescribeMetricSummary(GripMetrics metrics)
        {
            return "target=" + metrics.Target +
                " model=" + metrics.ModelAssetPath +
                " materials=" + string.Join(",", metrics.MaterialPaths) +
                " textures=" + string.Join(",", metrics.TexturePaths) +
                " boundsCenter=" + Vec(metrics.PropBoundsCenterLocal) +
                " boundsSize=" + Vec(metrics.PropBoundsSizeLocal) +
                " axialHeight=" + Num(metrics.AxialHeight) +
                " propTopY=" + Num(metrics.PropTopYLocal) +
                " chestY=" + Num(metrics.ChestHeightLocal) +
                " floorGap=" + Num(metrics.FloorGap) +
                " leanDegrees=" + Num(metrics.LeanDegrees) +
                " rightPalmDistance=" + Num(metrics.RightPalmDistance) +
                " leftPalmDistance=" + Num(metrics.LeftPalmDistance) +
                " rightGripHeight01=" + Num(metrics.RightGripHeight01) +
                " leftGripHeight01=" + Num(metrics.LeftGripHeight01) +
                " rightElbowDegrees=" + Num(metrics.RightElbowDegrees) +
                " leftElbowDegrees=" + Num(metrics.LeftElbowDegrees) +
                " rightShoulderTranslation=" +
                Num(metrics.RightShoulderTranslation) +
                " leftShoulderTranslation=" +
                Num(metrics.LeftShoulderTranslation);
        }

        private static Texture2D RenderGripClosePanel(
            Scene scene,
            Transform target,
            int width,
            int height)
        {
            Vector3 lookAt = target.TransformPoint(new Vector3(0f, 1.10f, 0.25f));
            Vector3 reviewFront = target.forward;
            return RenderOrthographicPanel(
                scene,
                lookAt,
                reviewFront,
                1.2f,
                0.64f,
                width,
                height);
        }

        private static Texture2D RenderPanel(
            Scene scene,
            Bounds bounds,
            Vector3 front,
            int width,
            int height)
        {
            float aspect = (float)width / height;
            float size = Mathf.Max(bounds.extents.y, bounds.extents.x / aspect) * 1.16f;
            return RenderOrthographicPanel(
                scene,
                bounds.center,
                front,
                2.5f,
                size,
                width,
                height);
        }

        private static Texture2D RenderOrthographicPanel(
            Scene scene,
            Vector3 lookAt,
            Vector3 front,
            float distance,
            float size,
            int width,
            int height)
        {
            var cameraObject = new GameObject("VacuumCleanerGrip_FinalCamera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = size;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 20f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.03f, 0.04f, 1f);
            front = Vector3.ProjectOnPlane(front, Vector3.up).normalized;
            camera.transform.SetPositionAndRotation(
                lookAt + front * distance,
                Quaternion.LookRotation(-front, Vector3.up));
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture render = RenderTexture.GetTemporary(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = render;
                camera.Render();
                RenderTexture.active = render;
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                image.Apply(false, false);
                return image;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(image);
                throw;
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Bounds BoundsInTargetSpace(Transform target, Transform root)
        {
            return CalculateVertexBoundsInSpace(target, root);
        }

        private static Bounds CalculateVertexBoundsInSpace(
            Transform space,
            Transform root)
        {
            bool initialized = false;
            Bounds result = default;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                EncapsulateVertices(
                    ref result,
                    ref initialized,
                    space.worldToLocalMatrix * filter.transform.localToWorldMatrix,
                    filter.sharedMesh.vertices);
            }

            if (!initialized)
            {
                throw new InvalidOperationException(root.name + " has no mesh vertices.");
            }

            return result;
        }

        private static Bounds CalculateRotatedVertexBounds(
            Transform root,
            Quaternion localRotation)
        {
            bool initialized = false;
            Bounds result = default;
            Matrix4x4 rotation = Matrix4x4.Rotate(localRotation);
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                EncapsulateVertices(
                    ref result,
                    ref initialized,
                    rotation * root.worldToLocalMatrix * filter.transform.localToWorldMatrix,
                    filter.sharedMesh.vertices);
            }

            if (!initialized)
            {
                throw new InvalidOperationException(root.name + " has no mesh vertices.");
            }

            return result;
        }

        private static void EncapsulateVertices(
            ref Bounds result,
            ref bool initialized,
            Matrix4x4 matrix,
            Vector3[] vertices)
        {
            foreach (Vector3 vertex in vertices)
            {
                Vector3 point = matrix.MultiplyPoint3x4(vertex);
                if (!initialized)
                {
                    result = new Bounds(point, Vector3.zero);
                    initialized = true;
                }
                else
                {
                    result.Encapsulate(point);
                }
            }
        }

        private static Transform RequireDescendant(Transform root, string name)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true)
                .Where(transform => transform.name == name)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    root.name + " requires exactly one descendant " + name + ".");
            }

            return matches[0];
        }

        private sealed class GripMetrics
        {
            public string Target;
            public string ModelAssetPath;
            public string[] MaterialPaths;
            public string[] TexturePaths;
            public Vector3 PropBoundsCenterLocal;
            public Vector3 PropBoundsSizeLocal;
            public float AxialHeight;
            public float PropTopYLocal;
            public float ChestHeightLocal;
            public float FloorGap;
            public float LeanDegrees;
            public float RightPalmDistance;
            public float LeftPalmDistance;
            public float RightGripHeight01;
            public float LeftGripHeight01;
            public float RightElbowDegrees;
            public float LeftElbowDegrees;
            public float RightShoulderTranslation;
            public float LeftShoulderTranslation;
            public int ModelInstanceCount;
        }

        private sealed class ReachPlan
        {
            public float InitialRightDistance;
            public float InitialLeftDistance;
            public ArmReachData Right;
            public ArmReachData Left;
        }

        private sealed class LayoutPlan
        {
            public float PropHeight;
            public float PropCenterZ;
        }

        private struct ReachInterval
        {
            public float Minimum;
            public float Maximum;
        }

        private sealed class ArmReachData
        {
            public string Name;
            public Vector3 Shoulder;
            public Vector3 AnchorAtInitialHeight;
            public Vector3 AnchorSlope;
            public Vector3 DepthDirection;
            public float MinimumReach;
            public float MaximumReach;

            public bool IsNaturalAtLayout(float height, float depthOffset)
            {
                Vector3 anchor = AnchorAtInitialHeight +
                    AnchorSlope * (height - InitialPropHeight) +
                    DepthDirection * depthOffset;
                float distance = Vector3.Distance(Shoulder, anchor);
                return distance >= MinimumReach - 0.0005f &&
                       distance <= MaximumReach + 0.0005f;
            }

            public string DescribeAtHeight(float height)
            {
                Vector3 anchor = AnchorAtInitialHeight +
                    AnchorSlope * (height - InitialPropHeight);
                return Name + " distance=" + Num(Vector3.Distance(Shoulder, anchor)) +
                    " min=" + Num(MinimumReach) +
                    " max=" + Num(MaximumReach) +
                    " anchor=" + Vec(anchor) +
                    " shoulder=" + Vec(Shoulder) +
                    " slope=" + Vec(AnchorSlope);
            }
        }

        private readonly struct ReferenceWristMetrics
        {
            public ReferenceWristMetrics(
                string target,
                float wristDeviationDegrees,
                float gripAxisErrorDegrees,
                float palmDistance,
                float elbowDegrees)
            {
                Target = target;
                WristDeviationDegrees = wristDeviationDegrees;
                GripAxisErrorDegrees = gripAxisErrorDegrees;
                PalmDistance = palmDistance;
                ElbowDegrees = elbowDegrees;
            }

            public string Target { get; }

            public float WristDeviationDegrees { get; }

            public float GripAxisErrorDegrees { get; }

            public float PalmDistance { get; }

            public float ElbowDegrees { get; }

            public string Describe()
            {
                return "target=" + Target +
                       " wristDeviationDegrees=" + Num(WristDeviationDegrees) +
                       " gripAxisErrorDegrees=" + Num(GripAxisErrorDegrees) +
                       " palmDistance=" + Num(PalmDistance) +
                       " elbowDegrees=" + Num(ElbowDegrees);
            }
        }

        private struct ComponentGeometry
        {
            public ComponentGeometry(Vector3 firstPoint)
            {
                Bounds = new Bounds(firstPoint, Vector3.zero);
                VertexCount = 1;
            }

            public Bounds Bounds;
            public int VertexCount;
        }

        private readonly struct TopHandleGripFrame
        {
            public TopHandleGripFrame(
                Vector3 positionLocal,
                Vector3 localAxis,
                Vector3 innerStartLocal,
                Vector3 innerEndLocal,
                Bounds componentBounds,
                Bounds straightRegionBounds)
            {
                PositionLocal = positionLocal;
                LocalAxis = localAxis;
                InnerStartLocal = innerStartLocal;
                InnerEndLocal = innerEndLocal;
                ComponentBounds = componentBounds;
                StraightRegionBounds = straightRegionBounds;
            }

            public Vector3 PositionLocal { get; }
            public Vector3 LocalAxis { get; }
            public Vector3 InnerStartLocal { get; }
            public Vector3 InnerEndLocal { get; }
            public Bounds ComponentBounds { get; }
            public Bounds StraightRegionBounds { get; }
        }

        private readonly struct TopHandleGripMetrics
        {
            public TopHandleGripMetrics(
                string target,
                float anchorError,
                float startClearance,
                float endClearance,
                float nearestSurfaceDistance,
                float handleForearmAngle,
                ReferenceWristMetrics wrist)
            {
                Target = target;
                AnchorError = anchorError;
                StartClearance = startClearance;
                EndClearance = endClearance;
                NearestSurfaceDistance = nearestSurfaceDistance;
                HandleForearmAngle = handleForearmAngle;
                Wrist = wrist;
            }

            public string Target { get; }
            public float AnchorError { get; }
            public float StartClearance { get; }
            public float EndClearance { get; }
            public float NearestSurfaceDistance { get; }
            public float HandleForearmAngle { get; }
            public ReferenceWristMetrics Wrist { get; }

            public string Describe()
            {
                return "target=" + Target +
                       " anchorError=" + Num(AnchorError) +
                       " startClearance=" + Num(StartClearance) +
                       " endClearance=" + Num(EndClearance) +
                       " nearestSurfaceDistance=" + Num(NearestSurfaceDistance) +
                       " handleForearmAngle=" + Num(HandleForearmAngle) +
                       " " + Wrist.Describe();
            }
        }

        private readonly struct RootScenePlacementState
        {
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            public RootScenePlacementState(Transform transform)
            {
                Parent = transform.parent;
                SiblingIndex = transform.GetSiblingIndex();
                localPosition = transform.localPosition;
                localRotation = transform.localRotation;
                localScale = transform.localScale;
            }

            public Transform Parent { get; }

            public int SiblingIndex { get; }

            public void Apply(Transform transform)
            {
                transform.localPosition = localPosition;
                transform.localRotation = localRotation;
                transform.localScale = localScale;
            }

            public bool Matches(Transform transform)
            {
                return transform.parent == Parent &&
                       transform.GetSiblingIndex() == SiblingIndex &&
                       transform.localPosition == localPosition &&
                       transform.localRotation == localRotation &&
                       transform.localScale == localScale;
            }
        }

        private readonly struct StateCopyMetrics
        {
            public StateCopyMetrics(
                int transformCount,
                int componentCount,
                int gameObjectCount)
            {
                TransformCount = transformCount;
                ComponentCount = componentCount;
                GameObjectCount = gameObjectCount;
            }

            public int TransformCount { get; }

            public int ComponentCount { get; }

            public int GameObjectCount { get; }

            public string Describe()
            {
                return "matchingTransforms=" + TransformCount +
                       " matchingComponents=" + ComponentCount +
                       " matchingGameObjects=" + GameObjectCount;
            }
        }

        private readonly struct LocalTransformState
        {
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            public LocalTransformState(Transform transform)
            {
                position = transform.localPosition;
                rotation = transform.localRotation;
                scale = transform.localScale;
            }

            public bool Matches(Transform transform)
            {
                return position == transform.localPosition &&
                       rotation == transform.localRotation &&
                       scale == transform.localScale;
            }
        }

        private sealed class TransformHierarchyState
        {
            private readonly System.Collections.Generic.Dictionary<string, LocalTransformState>
                transforms;

            public TransformHierarchyState(Transform root)
            {
                transforms = root.GetComponentsInChildren<Transform>(true)
                    .ToDictionary(
                        transform => AnimationUtility.CalculateTransformPath(
                            transform,
                            root),
                        transform => new LocalTransformState(transform),
                        StringComparer.Ordinal);
            }

            public bool Matches(Transform root)
            {
                foreach (var pair in transforms)
                {
                    Transform transform = string.IsNullOrEmpty(pair.Key)
                        ? root
                        : root.Find(pair.Key);
                    if (transform == null || !pair.Value.Matches(transform))
                    {
                        return false;
                    }
                }

                return root.GetComponentsInChildren<Transform>(true).Length ==
                    transforms.Count;
            }
        }

        private sealed class TargetProtectionState
        {
            private readonly System.Collections.Generic.Dictionary<string, LocalTransformState>
                protectedTransforms;
            private readonly System.Collections.Generic.Dictionary<string, SkinnedMeshState>
                protectedSkinnedMeshes;
            private readonly string controllerPath;
            private readonly bool animatorEnabled;
            private readonly bool applyRootMotion;

            public TargetProtectionState(Transform target)
            {
                protectedTransforms = target.GetComponentsInChildren<Transform>(true)
                    .Select(transform => new
                    {
                        Transform = transform,
                        Path = AnimationUtility.CalculateTransformPath(transform, target)
                    })
                    .Where(entry => !IsArmPath(entry.Path) &&
                                    !entry.Path.StartsWith(HolderName, StringComparison.Ordinal))
                    .ToDictionary(
                        entry => entry.Path,
                        entry => new LocalTransformState(entry.Transform),
                        StringComparer.Ordinal);
                protectedSkinnedMeshes = target
                    .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .ToDictionary(
                        renderer => AnimationUtility.CalculateTransformPath(
                            renderer.transform,
                            target),
                        renderer => new SkinnedMeshState(renderer),
                        StringComparer.Ordinal);
                Animator animator = target.GetComponentsInChildren<Animator>(true).Single();
                controllerPath = AssetDatabase.GetAssetPath(animator.runtimeAnimatorController);
                animatorEnabled = animator.enabled;
                applyRootMotion = animator.applyRootMotion;
            }

            public void RequireProtectedStateUnchanged(Transform target)
            {
                foreach (var pair in protectedTransforms)
                {
                    Transform transform = string.IsNullOrEmpty(pair.Key)
                        ? target
                        : target.Find(pair.Key);
                    if (transform == null || !pair.Value.Matches(transform))
                    {
                        throw new InvalidOperationException(
                            target.name + " protected transform changed: " + pair.Key + ".");
                    }
                }

                foreach (var pair in protectedSkinnedMeshes)
                {
                    Transform rendererTransform = string.IsNullOrEmpty(pair.Key)
                        ? target
                        : target.Find(pair.Key);
                    SkinnedMeshRenderer renderer = rendererTransform == null
                        ? null
                        : rendererTransform.GetComponent<SkinnedMeshRenderer>();
                    if (renderer == null || !pair.Value.Matches(renderer))
                    {
                        throw new InvalidOperationException(
                            target.name + " protected skinned mesh changed: " +
                            pair.Key + ".");
                    }
                }

                Animator animator = target.GetComponentsInChildren<Animator>(true).Single();
                if (!string.Equals(
                        controllerPath,
                        AssetDatabase.GetAssetPath(animator.runtimeAnimatorController),
                        StringComparison.Ordinal) ||
                    animatorEnabled != animator.enabled ||
                    applyRootMotion != animator.applyRootMotion)
                {
                    throw new InvalidOperationException(
                        target.name + " Animator state or timing source changed.");
                }
            }

            private static bool IsArmPath(string path)
            {
                return path == LeftShoulderPath ||
                       path.StartsWith(LeftShoulderPath + "/", StringComparison.Ordinal) ||
                       path == RightShoulderPath ||
                       path.StartsWith(RightShoulderPath + "/", StringComparison.Ordinal);
            }
        }

        private sealed class SkinnedMeshState
        {
            private readonly Mesh mesh;
            private readonly Transform rootBone;
            private readonly Transform[] bones;
            private readonly Material[] materials;

            public SkinnedMeshState(SkinnedMeshRenderer renderer)
            {
                mesh = renderer.sharedMesh;
                rootBone = renderer.rootBone;
                bones = renderer.bones.ToArray();
                materials = renderer.sharedMaterials.ToArray();
            }

            public bool Matches(SkinnedMeshRenderer renderer)
            {
                return renderer.sharedMesh == mesh &&
                       renderer.rootBone == rootBone &&
                       renderer.bones.SequenceEqual(bones) &&
                       renderer.sharedMaterials.SequenceEqual(materials);
            }
        }

        private static void CaptureTargets(
            Scene scene,
            Transform idle,
            Transform use,
            Bounds bounds,
            string destination)
        {
            Vector3 front = Vector3.ProjectOnPlane(idle.forward, Vector3.up).normalized;
            float verticalDistance = bounds.extents.y /
                Mathf.Tan(24f * Mathf.Deg2Rad);
            float horizontalDistance = bounds.extents.x /
                (Mathf.Tan(24f * Mathf.Deg2Rad) *
                 ((float)CaptureWidth / CaptureHeight));
            float distance = Mathf.Max(verticalDistance, horizontalDistance) * 1.25f;
            Vector3 lookAt = bounds.center + Vector3.up * bounds.extents.y * 0.03f;
            var cameraObject = new GameObject("VacuumCleanerGrip_ReadOnlyCamera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 48f;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = Mathf.Max(25f, distance + 10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.03f, 0.04f, 1f);
            camera.transform.SetPositionAndRotation(
                lookAt + front * distance,
                Quaternion.LookRotation(-front, Vector3.up));

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture target = RenderTexture.GetTemporary(
                CaptureWidth,
                CaptureHeight,
                24,
                RenderTextureFormat.ARGB32);
            var image = new Texture2D(
                CaptureWidth,
                CaptureHeight,
                TextureFormat.RGB24,
                false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, CaptureWidth, CaptureHeight), 0, 0);
                image.Apply(false, false);
                File.WriteAllBytes(destination, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }

            if (idle.name != IdleName || use.name != UseName)
            {
                throw new InvalidOperationException("Capture targets changed during review.");
            }
        }

        private static Bounds CalculateAssetLocalBounds(GameObject root)
        {
            bool initialized = false;
            Bounds result = default;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                EncapsulateBounds(
                    ref result,
                    ref initialized,
                    root.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix,
                    filter.sharedMesh.bounds);
            }

            foreach (SkinnedMeshRenderer renderer in
                     root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.sharedMesh == null)
                {
                    continue;
                }

                EncapsulateBounds(
                    ref result,
                    ref initialized,
                    root.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix,
                    renderer.localBounds);
            }

            if (!initialized)
            {
                throw new InvalidOperationException("Vacuum cleaner FBX has no mesh bounds.");
            }

            return result;
        }

        private static void EncapsulateBounds(
            ref Bounds result,
            ref bool initialized,
            Matrix4x4 matrix,
            Bounds source)
        {
            Vector3 min = source.min;
            Vector3 max = source.max;
            for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
            for (int z = 0; z < 2; z++)
            {
                Vector3 point = matrix.MultiplyPoint3x4(new Vector3(
                    x == 0 ? min.x : max.x,
                    y == 0 ? min.y : max.y,
                    z == 0 ? min.z : max.z));
                if (!initialized)
                {
                    result = new Bounds(point, Vector3.zero);
                    initialized = true;
                }
                else
                {
                    result.Encapsulate(point);
                }
            }
        }

        private static Bounds BoundsOf(params Transform[] roots)
        {
            Renderer[] renderers = roots
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Vacuum targets have no visible renderers.");
            }

            Bounds result = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
            {
                result.Encapsulate(renderer.bounds);
            }

            return result;
        }

        private static Transform RequireTarget(string name)
        {
            GameObject value = GameObject.Find(name) ??
                throw new InvalidOperationException(name + " is missing.");
            return value.transform;
        }

        private static Transform FindRequired(Transform root, string path)
        {
            return root.Find(path) ??
                throw new InvalidOperationException(
                    root.name + " is missing required transform " + path + ".");
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be active. ActiveScene=" + scene.path + ".");
            }

            return scene;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Vacuum grip authoring requires Edit Mode.");
            }
        }

        private static void WriteText(string fileName, string contents)
        {
            WriteText(OutputFolder, fileName, contents);
        }

        private static void WriteText(
            string folder,
            string fileName,
            string contents)
        {
            string directory = Absolute(folder);
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, fileName), contents, Encoding.UTF8);
        }

        private static string Absolute(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path);
            }

            string root = Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Project root is unavailable.");
            return Path.GetFullPath(Path.Combine(
                root,
                path.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return Num(value.x) + "," + Num(value.y) + "," + Num(value.z);
        }

        private static string Quat(Quaternion value)
        {
            return Num(value.x) + "," + Num(value.y) + "," +
                Num(value.z) + "," + Num(value.w);
        }
    }
}
