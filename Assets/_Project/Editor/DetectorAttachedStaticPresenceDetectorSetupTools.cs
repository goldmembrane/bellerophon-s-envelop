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
    internal static class DetectorAttachedStaticPresenceDetectorSetupTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string ExternalModelPath = "item model/presence detector.fbx";
        private const string ItemFolder = "Assets/_Project/Art/Items/PresenceDetector";
        private const string ModelPath = ItemFolder + "/PresenceDetector.fbx";
        private const string TextureFolder = ItemFolder + "/Textures";
        private const string MaterialFolder = ItemFolder + "/Materials";
        private const string PackedMetallicSmoothnessPath =
            TextureFolder + "/PresenceDetector_MetallicSmoothness.png";
        private const string ValidationFolder =
            "docs/validation/DetectorAttachedStaticPresenceDetector";
        private const string ApplicationReportPath = ValidationFolder + "/application.txt";
        private const string InspectionReportPath = ValidationFolder + "/inspection.txt";
        private const string FinalImagePath =
            ValidationFolder + "/DetectorAttachedStaticPresenceDetector_Final.png";
        private const string FinalReportPath =
            ValidationFolder + "/DetectorAttachedStaticPresenceDetector_Final.txt";
        private const string ReferenceName = "Armor_Head_Idle";
        private const string TargetName = "Detector_Attached_Static";
        private const string ReferenceAttachmentName = "ArmorHelmet_Attached";
        private const string TargetAttachmentName = "PresenceDetector_Attached";
        private const float PositionTolerance = 0.00001f;
        private const float RotationTolerance = 0.01f;
        private const int PanelSize = 768;
        private const int PanelGap = 12;

        private static readonly string[] EmbeddedTextureFileNames =
        {
            "base_color.jpg",
            "normal.jpg",
            "texture_0_metallic.png",
            "texture_0_roughness.png"
        };

        [MenuItem("Bellerophon/Player/Inspect Detector Attached Static Presence Detector Sources")]
        internal static void InspectSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            string sourcePath = Absolute(ExternalModelPath);
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Presence detector source FBX is missing.", sourcePath);

            GameObject reference = FindUnique(scene, ReferenceName);
            GameObject target = FindUnique(scene, TargetName);
            Transform referenceHead = FindHead(reference);
            Transform targetHead = FindHead(target);
            Transform attachment = RequireDirectChild(referenceHead, ReferenceAttachmentName);

            var report = new StringBuilder()
                .AppendLine("Detector_Attached_Static presence-detector source inspection")
                .AppendLine("sourceModel=" + ExternalModelPath)
                .AppendLine("sourceModelSha256=" + Sha256File(sourcePath))
                .AppendLine("reference=" + ReferenceName)
                .AppendLine("referenceHeadPath=" + BonePath(referenceHead, reference.transform))
                .AppendLine("targetHeadPath=" + BonePath(targetHead, target.transform))
                .AppendLine("referenceAttachment=" + ReferenceAttachmentName)
                .AppendLine("referenceAttachmentLocalPosition=" + Vec(attachment.localPosition))
                .AppendLine("referenceAttachmentLocalRotation=" + Quat(attachment.localRotation))
                .AppendLine("referenceAttachmentLocalScale=" + Vec(attachment.localScale))
                .AppendLine("embeddedTextureManifest=" +
                    string.Join(",", EmbeddedTextureFileNames))
                .AppendLine("referenceChanged=False")
                .AppendLine("targetChanged=False");

            RequireEqual(
                BonePath(referenceHead, reference.transform),
                BonePath(targetHead, target.transform),
                "reference and target head hierarchy paths");
            Debug.Log(report.ToString());
        }

        [MenuItem("Bellerophon/Player/Apply Detector Attached Static Presence Detector")]
        internal static void ImportAndApply()
        {
            RequireEditMode();
            EnsureFolder(ItemFolder);
            EnsureFolder(TextureFolder);
            EnsureFolder(MaterialFolder);
            Directory.CreateDirectory(Absolute(ValidationFolder));

            ImportExactFbxAndEmbeddedAppearance();
            ApplyReferenceAttachmentToTarget();
            InspectApplied();
        }

        [MenuItem("Bellerophon/Player/Inspect Detector Attached Static Presence Detector")]
        internal static void InspectApplied()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject reference = FindUnique(scene, ReferenceName);
            GameObject target = FindUnique(scene, TargetName);
            Transform referenceHead = FindHead(reference);
            Transform targetHead = FindHead(target);
            Transform referenceAttachment =
                RequireDirectChild(referenceHead, ReferenceAttachmentName);
            Transform targetAttachment =
                RequireDirectChild(targetHead, TargetAttachmentName);

            RequireEqual(
                BonePath(referenceHead, reference.transform),
                BonePath(targetHead, target.transform),
                "reference and target head hierarchy paths");
            RequireNear(
                targetAttachment.localPosition,
                referenceAttachment.localPosition,
                "attachment local position");
            RequireNear(
                targetAttachment.localRotation,
                referenceAttachment.localRotation,
                "attachment local rotation");
            RequireNear(
                targetAttachment.localScale,
                referenceAttachment.localScale,
                "attachment local scale");

            AppearanceInspection appearance = InspectImportedAppearance();
            Renderer[] instanceRenderers =
                targetAttachment.GetComponentsInChildren<Renderer>(true);
            if (instanceRenderers.Length == 0)
                throw new InvalidOperationException("Applied presence detector has no renderer.");
            InspectRendererMaterials(instanceRenderers);

            string sourceHash = Sha256File(Absolute(ExternalModelPath));
            string importedHash = Sha256File(Absolute(ModelPath));
            RequireEqual(sourceHash, importedHash, "source/imported FBX SHA-256");

            var report = new StringBuilder()
                .AppendLine("Detector_Attached_Static presence-detector inspection")
                .AppendLine("directSceneObjectInspection=True")
                .AppendLine("verificationTargetTransformManipulated=False")
                .AppendLine("sourceModelSha256=" + sourceHash)
                .AppendLine("importedModelSha256=" + importedHash)
                .AppendLine("referenceHeadPath=" +
                    BonePath(referenceHead, reference.transform))
                .AppendLine("targetHeadPath=" + BonePath(targetHead, target.transform))
                .AppendLine("attachmentParentMatchesReference=True")
                .AppendLine("attachmentLocalPosition=" + Vec(targetAttachment.localPosition))
                .AppendLine("attachmentLocalRotation=" + Quat(targetAttachment.localRotation))
                .AppendLine("attachmentLocalScale=" + Vec(targetAttachment.localScale))
                .AppendLine("rendererCount=" + instanceRenderers.Length)
                .AppendLine("externalizedMaterialCount=" + appearance.MaterialCount)
                .AppendLine("embeddedTextureCount=" + EmbeddedTextureFileNames.Length)
                .AppendLine("allEmbeddedTexturesImported=True")
                .AppendLine("allRendererMaterialSlotsAssigned=True")
                .AppendLine("urpLitMaterialApplied=True")
                .AppendLine("meshModified=False")
                .AppendLine("uvModified=False")
                .AppendLine("referenceChanged=False");
            File.WriteAllText(
                Absolute(InspectionReportPath), report.ToString(), new UTF8Encoding(false));
            Debug.Log(report.ToString());
        }

        [MenuItem("Bellerophon/Player/Capture Detector Attached Static Presence Detector Final")]
        internal static void CaptureFinal()
        {
            RequireEditMode();
            InspectApplied();
            string imagePath = Absolute(FinalImagePath);
            string reportPath = Absolute(FinalReportPath);
            if (File.Exists(imagePath) || File.Exists(reportPath))
                throw new InvalidOperationException(
                    "The one final presence-detector comparison already exists.");

            Scene scene = RequireScene();
            GameObject reference = FindUnique(scene, ReferenceName);
            GameObject target = FindUnique(scene, TargetName);
            Texture2D[] panels =
            {
                RenderHeadAttachment(reference, reference.transform.forward),
                RenderHeadAttachment(target, target.transform.forward),
                RenderHeadAttachment(reference, -reference.transform.right),
                RenderHeadAttachment(target, -target.transform.right)
            };

            try
            {
                Texture2D comparison = ComposePanels(panels);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(imagePath) ??
                        throw new InvalidOperationException("Final comparison folder is unavailable."));
                    File.WriteAllBytes(imagePath, comparison.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(comparison);
                }
            }
            finally
            {
                foreach (Texture2D panel in panels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            }

            var report = new StringBuilder()
                .AppendLine("Detector_Attached_Static presence-detector final direct comparison")
                .AppendLine("topLeft=Armor_Head_Idle front")
                .AppendLine("topRight=Detector_Attached_Static front")
                .AppendLine("bottomLeft=Armor_Head_Idle left-side")
                .AppendLine("bottomRight=Detector_Attached_Static left-side")
                .AppendLine("samePoseAndMatchedViewpoint=True")
                .AppendLine("verificationTargetTransformManipulated=False")
                .AppendLine("comparisonSha256=" + Sha256File(imagePath));
            File.WriteAllText(reportPath, report.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("Detector_Attached_Static final direct comparison captured once.");
        }

        private static void ImportExactFbxAndEmbeddedAppearance()
        {
            string sourcePath = Absolute(ExternalModelPath);
            string destinationPath = Absolute(ModelPath);
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Presence detector source FBX is missing.", sourcePath);
            if (!File.Exists(destinationPath) ||
                !string.Equals(
                    Sha256File(sourcePath), Sha256File(destinationPath),
                    StringComparison.Ordinal))
            {
                File.Copy(sourcePath, destinationPath, true);
            }

            AssetDatabase.ImportAsset(
                ModelPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter ??
                throw new InvalidOperationException("Presence detector ModelImporter is unavailable.");
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();

            Material[] embeddedMaterials = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<Material>()
                .Where(item => !AssetDatabase.IsMainAsset(item))
                .GroupBy(item => item.name, StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .ToArray();
            if (embeddedMaterials.Length == 0 &&
                AssetDatabase.FindAssets("t:Material", new[] { MaterialFolder }).Length == 0)
                throw new InvalidOperationException(
                    "Presence detector contains no importable embedded material.");

            if (!HasEveryEmbeddedTexture())
            {
                bool extracted = importer.ExtractTextures(Absolute(TextureFolder));
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                if (!extracted && !HasEveryEmbeddedTexture())
                    throw new InvalidOperationException(
                        "Presence detector embedded texture extraction failed.");
            }

            Texture2D baseColor = RequireTexture("base_color.jpg");
            Texture2D normal = RequireTexture("normal.jpg");
            Texture2D metallic = RequireTexture("texture_0_metallic.png");
            Texture2D roughness = RequireTexture("texture_0_roughness.png");
            ConfigureTexture(baseColor, TextureImporterType.Default, true, false);
            ConfigureTexture(normal, TextureImporterType.NormalMap, false, false);
            ConfigureTexture(metallic, TextureImporterType.Default, false, true);
            ConfigureTexture(roughness, TextureImporterType.Default, false, true);
            Texture2D metallicSmoothness =
                CreateMetallicSmoothness(metallic, roughness);
            ConfigureTexture(
                metallicSmoothness, TextureImporterType.Default, false, false);
            ConfigureTexture(RequireTexture("texture_0_metallic.png"),
                TextureImporterType.Default, false, false);
            ConfigureTexture(RequireTexture("texture_0_roughness.png"),
                TextureImporterType.Default, false, false);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                throw new InvalidOperationException("URP Lit shader is unavailable.");
            var materialPaths = new List<string>();
            if (embeddedMaterials.Length > 0)
            {
                foreach (Material embedded in embeddedMaterials)
                {
                    string materialPath = MaterialFolder + "/" +
                        SanitizeFileName(embedded.name) + ".mat";
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (material == null)
                    {
                        material = new Material(embedded) { name = embedded.name };
                        AssetDatabase.CreateAsset(material, materialPath);
                    }

                    ConfigureUrpMaterial(
                        material, shader, baseColor, normal, metallicSmoothness);
                    importer.AddRemap(
                        new AssetImporter.SourceAssetIdentifier(
                            typeof(Material), embedded.name),
                        material);
                    materialPaths.Add(materialPath);
                }
            }
            else
            {
                materialPaths.AddRange(AssetDatabase.FindAssets(
                        "t:Material", new[] { MaterialFolder })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .OrderBy(path => path, StringComparer.Ordinal));
                foreach (string materialPath in materialPaths)
                {
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath) ??
                        throw new InvalidOperationException(
                            "Externalized presence detector material is missing: " + materialPath);
                    ConfigureUrpMaterial(
                        material, shader, baseColor, normal, metallicSmoothness);
                }
            }

            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.materialSearch = ModelImporterMaterialSearch.Local;
            importer.SaveAndReimport();
            AssetDatabase.SaveAssets();
            RequireEqual(
                Sha256File(sourcePath), Sha256File(destinationPath),
                "source/imported presence detector FBX hash");
            InspectImportedAppearance();
        }

        private static void ConfigureUrpMaterial(
            Material material,
            Shader shader,
            Texture2D baseColor,
            Texture2D normal,
            Texture2D metallicSmoothness)
        {
            material.shader = shader;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", baseColor);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", baseColor);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", normal);
            if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", 1f);
            if (material.HasProperty("_MetallicGlossMap"))
                material.SetTexture("_MetallicGlossMap", metallicSmoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 1f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 1f);
            if (material.HasProperty("_SmoothnessTextureChannel"))
                material.SetFloat("_SmoothnessTextureChannel", 0f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
        }

        private static void ApplyReferenceAttachmentToTarget()
        {
            Scene scene = RequireScene();
            GameObject reference = FindUnique(scene, ReferenceName);
            GameObject target = FindUnique(scene, TargetName);
            Transform referenceHead = FindHead(reference);
            Transform targetHead = FindHead(target);
            Transform referenceAttachment =
                RequireDirectChild(referenceHead, ReferenceAttachmentName);

            RequireEqual(
                BonePath(referenceHead, reference.transform),
                BonePath(targetHead, target.transform),
                "reference and target head hierarchy paths");

            string referenceBefore = ObjectSignature(reference.transform, null);
            Transform existing = targetHead.Cast<Transform>()
                .SingleOrDefault(item => item.name == TargetAttachmentName);
            string targetBefore = ObjectSignature(target.transform, existing);
            Vector3 targetRootPosition = target.transform.position;
            Quaternion targetRootRotation = target.transform.rotation;
            Vector3 targetRootScale = target.transform.localScale;

            if (existing != null)
                Undo.DestroyObjectImmediate(existing.gameObject);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Imported presence detector model is missing.");
            GameObject instance = PrefabUtility.InstantiatePrefab(model, scene) as GameObject ??
                throw new InvalidOperationException(
                    "Presence detector scene instance could not be created.");
            Undo.RegisterCreatedObjectUndo(instance, "Attach exact presence detector model");
            instance.name = TargetAttachmentName;
            instance.transform.SetParent(targetHead, false);
            instance.transform.localPosition = referenceAttachment.localPosition;
            instance.transform.localRotation = referenceAttachment.localRotation;
            instance.transform.localScale = referenceAttachment.localScale;
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            EditorUtility.SetDirty(instance);

            RequireEqual(referenceBefore, ObjectSignature(reference.transform, null),
                ReferenceName + " protected signature");
            RequireEqual(targetBefore, ObjectSignature(target.transform, instance.transform),
                TargetName + " existing hierarchy signature");
            RequireNear(target.transform.position, targetRootPosition, TargetName + " root position");
            RequireNear(target.transform.rotation, targetRootRotation, TargetName + " root rotation");
            RequireNear(target.transform.localScale, targetRootScale, TargetName + " root scale");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            AssetDatabase.SaveAssets();

            var report = new StringBuilder()
                .AppendLine("Detector_Attached_Static exact presence-detector application")
                .AppendLine("sourceModel=" + ExternalModelPath)
                .AppendLine("sourceModelSha256=" + Sha256File(Absolute(ExternalModelPath)))
                .AppendLine("importedModelSha256=" + Sha256File(Absolute(ModelPath)))
                .AppendLine("target=" + TargetName)
                .AppendLine("attachment=" + TargetAttachmentName)
                .AppendLine("attachmentParent=" + RelativePath(targetHead, target.transform))
                .AppendLine("referenceAttachment=" + ReferenceName + "/" +
                    RelativePath(referenceAttachment, reference.transform))
                .AppendLine("localPositionCopiedExactly=True")
                .AppendLine("localRotationCopiedExactly=True")
                .AppendLine("localScaleCopiedExactly=True")
                .AppendLine("embeddedMaterialsAndTexturesApplied=True")
                .AppendLine("meshModified=False")
                .AppendLine("uvModified=False")
                .AppendLine("referenceChanged=False")
                .AppendLine("existingTargetHierarchyChanged=False")
                .AppendLine("sceneSaved=True");
            File.WriteAllText(
                Absolute(ApplicationReportPath), report.ToString(), new UTF8Encoding(false));
            Debug.Log(report.ToString());
        }

        private static AppearanceInspection InspectImportedAppearance()
        {
            foreach (string fileName in EmbeddedTextureFileNames)
                RequireTexture(fileName);
            Texture2D packed = AssetDatabase.LoadAssetAtPath<Texture2D>(
                PackedMetallicSmoothnessPath) ??
                throw new InvalidOperationException(
                    "Presence detector metallic/smoothness channel map is missing.");
            if (packed == null)
                throw new InvalidOperationException(
                    "Presence detector metallic/smoothness channel map is missing.");

            string[] materialPaths = AssetDatabase.FindAssets(
                    "t:Material", new[] { MaterialFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            if (materialPaths.Length == 0)
                throw new InvalidOperationException(
                    "Presence detector externalized embedded material is missing.");

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Imported presence detector model is missing.");
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException("Presence detector model has no renderer.");
            InspectRendererMaterials(renderers);
            return new AppearanceInspection(materialPaths.Length, renderers.Length);
        }

        private static void InspectRendererMaterials(IEnumerable<Renderer> renderers)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer.sharedMaterials.Length == 0)
                    throw new InvalidOperationException(
                        renderer.name + " has no presence detector material slot.");
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null)
                        throw new InvalidOperationException(
                            renderer.name + " has an empty presence detector material slot.");
                    string materialPath = AssetDatabase.GetAssetPath(material);
                    if (!materialPath.StartsWith(MaterialFolder + "/", StringComparison.Ordinal))
                        throw new InvalidOperationException(
                            "Presence detector renderer material was not externalized: " +
                            materialPath);
                    if (material.shader == null ||
                        material.shader.name != "Universal Render Pipeline/Lit" ||
                        material.GetTexture("_BaseMap") == null ||
                        material.GetTexture("_BumpMap") == null ||
                        material.GetTexture("_MetallicGlossMap") == null)
                        throw new InvalidOperationException(
                            "Presence detector material assignment is incomplete: " +
                            materialPath);
                }
            }
        }

        private static Texture2D CreateMetallicSmoothness(
            Texture2D metallic,
            Texture2D roughness)
        {
            metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(
                AssetDatabase.GetAssetPath(metallic));
            roughness = AssetDatabase.LoadAssetAtPath<Texture2D>(
                AssetDatabase.GetAssetPath(roughness));
            if (metallic.width != roughness.width || metallic.height != roughness.height)
                throw new InvalidOperationException(
                    "Presence detector metallic and roughness texture sizes differ.");
            Color32[] metalPixels = metallic.GetPixels32();
            Color32[] roughPixels = roughness.GetPixels32();
            var packedPixels = new Color32[metalPixels.Length];
            for (int index = 0; index < packedPixels.Length; index++)
            {
                byte metal = metalPixels[index].r;
                packedPixels[index] = new Color32(
                    metal, metal, metal, (byte)(255 - roughPixels[index].r));
            }

            var output = new Texture2D(
                metallic.width, metallic.height, TextureFormat.RGBA32, false, true);
            try
            {
                output.SetPixels32(packedPixels);
                output.Apply(false, false);
                File.WriteAllBytes(
                    Absolute(PackedMetallicSmoothnessPath), output.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(output);
            }
            AssetDatabase.ImportAsset(
                PackedMetallicSmoothnessPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(
                PackedMetallicSmoothnessPath) ??
                throw new InvalidOperationException(
                    "Presence detector metallic/smoothness import failed.");
        }

        private static void ConfigureTexture(
            Texture2D texture,
            TextureImporterType type,
            bool srgb,
            bool readable)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter ??
                throw new InvalidOperationException("TextureImporter is missing: " + path);
            if (importer.textureType == type && importer.sRGBTexture == srgb &&
                importer.isReadable == readable)
                return;
            importer.textureType = type;
            importer.sRGBTexture = srgb;
            importer.isReadable = readable;
            importer.SaveAndReimport();
        }

        private static bool HasEveryEmbeddedTexture()
        {
            return EmbeddedTextureFileNames.All(fileName =>
                File.Exists(Absolute(TextureFolder + "/" + fileName)));
        }

        private static Texture2D RequireTexture(string fileName)
        {
            string expectedPath = TextureFolder + "/" + fileName;
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(expectedPath);
            if (texture != null) return texture;
            string foundPath = AssetDatabase.FindAssets("t:Texture2D", new[] { TextureFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .SingleOrDefault(path => string.Equals(
                    Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase));
            return !string.IsNullOrWhiteSpace(foundPath)
                ? AssetDatabase.LoadAssetAtPath<Texture2D>(foundPath)
                : throw new InvalidOperationException(
                    "Presence detector embedded texture is missing: " + fileName);
        }

        private static Texture2D RenderHeadAttachment(
            GameObject source,
            Vector3 sourceViewDirection)
        {
            var preview = new PreviewRenderUtility(true);
            try
            {
                GameObject clone = UnityEngine.Object.Instantiate(source);
                clone.name = source.name + "_DirectInspectionClone";
                preview.AddSingleGO(clone);
                Transform head = FindHead(clone);
                Bounds bodyBounds = RendererBounds(clone);
                Vector3 up = clone.transform.up.normalized;
                Vector3 center = head.position - up * (bodyBounds.size.y * 0.10f);
                float verticalExtent = Mathf.Max(bodyBounds.size.y * 0.34f, 0.25f);
                float fieldOfView = 32f;
                float distance = verticalExtent /
                    Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
                Vector3 viewDirection = sourceViewDirection.normalized;

                preview.cameraFieldOfView = fieldOfView;
                preview.camera.clearFlags = CameraClearFlags.SolidColor;
                preview.camera.backgroundColor = new Color(0.08f, 0.10f, 0.13f, 1f);
                preview.camera.nearClipPlane = Mathf.Max(0.001f, distance * 0.01f);
                preview.camera.farClipPlane = Mathf.Max(100f, distance * 8f);
                preview.camera.transform.position = center + viewDirection * distance;
                preview.camera.transform.rotation = Quaternion.LookRotation(
                    center - preview.camera.transform.position, up);
                preview.lights[0].intensity = 1.35f;
                preview.lights[0].transform.rotation = Quaternion.Euler(35f, 35f, 0f);
                preview.lights[1].intensity = 1.0f;
                preview.lights[1].transform.rotation = Quaternion.Euler(340f, 210f, 0f);
                preview.ambientColor = new Color(0.48f, 0.48f, 0.48f, 1f);

                preview.BeginStaticPreview(new Rect(0, 0, PanelSize, PanelSize));
                preview.camera.Render();
                Texture2D rendered = preview.EndStaticPreview();
                if (rendered == null)
                    throw new InvalidOperationException(
                        source.name + " direct comparison render failed.");
                var copy = new Texture2D(
                    rendered.width, rendered.height, TextureFormat.RGBA32, false);
                copy.SetPixels32(rendered.GetPixels32());
                copy.Apply(false, false);
                return copy;
            }
            finally
            {
                preview.Cleanup();
            }
        }

        private static Texture2D ComposePanels(IReadOnlyList<Texture2D> panels)
        {
            int width = PanelSize * 2 + PanelGap;
            int height = PanelSize * 2 + PanelGap;
            var comparison = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var background = Enumerable.Repeat(
                new Color32(8, 10, 14, 255), width * height).ToArray();
            comparison.SetPixels32(background);
            comparison.SetPixels32(0, PanelSize + PanelGap, PanelSize, PanelSize,
                panels[0].GetPixels32());
            comparison.SetPixels32(PanelSize + PanelGap, PanelSize + PanelGap,
                PanelSize, PanelSize, panels[1].GetPixels32());
            comparison.SetPixels32(0, 0, PanelSize, PanelSize, panels[2].GetPixels32());
            comparison.SetPixels32(PanelSize + PanelGap, 0,
                PanelSize, PanelSize, panels[3].GetPixels32());
            comparison.Apply(false, false);
            return comparison;
        }

        private static Bounds RendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled).ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(root.name + " has no enabled renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static string ObjectSignature(Transform root, Transform ignoredRoot)
        {
            var lines = new List<string>();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                if (ignoredRoot != null &&
                    (item == ignoredRoot || item.IsChildOf(ignoredRoot)))
                    continue;
                lines.Add(RelativePath(item, root) + "|" +
                    Vec(item.localPosition) + "|" + Quat(item.localRotation) + "|" +
                    Vec(item.localScale) + "|" + item.gameObject.activeSelf);
                Renderer renderer = item.GetComponent<Renderer>();
                if (renderer != null)
                {
                    lines.Add(RelativePath(item, root) + "|materials=" + string.Join(",",
                        renderer.sharedMaterials.Select(material =>
                            material == null ? "<null>" : AssetDatabase.GetAssetPath(material))));
                }
            }
            lines.Sort(StringComparer.Ordinal);
            return Sha256Text(string.Join("\n", lines));
        }

        private static Transform FindHead(GameObject root)
        {
            Animator animator = root.GetComponentInChildren<Animator>(true);
            if (animator != null && animator.isHuman)
            {
                Transform humanoidHead = animator.GetBoneTransform(HumanBodyBones.Head);
                if (humanoidHead != null) return humanoidHead;
            }
            return root.GetComponentsInChildren<Transform>(true)
                       .FirstOrDefault(item =>
                           item.name.Equals("Head", StringComparison.OrdinalIgnoreCase) ||
                           item.name.EndsWith(":Head", StringComparison.OrdinalIgnoreCase)) ??
                   throw new InvalidOperationException(root.name + " head bone is missing.");
        }

        private static Transform RequireDirectChild(Transform parent, string name)
        {
            Transform[] matches = parent.Cast<Transform>()
                .Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    parent.name + " expected one direct child " + name +
                    "; found " + matches.Length + ".");
            return matches[0];
        }

        private static GameObject FindUnique(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name)
                .Select(item => item.gameObject)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected one " + name + "; found " + matches.Length + ".");
            return matches[0];
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must be active. ActiveScene=" + scene.path);
            return scene;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Presence detector setup requires Edit Mode.");
        }

        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }

        private static string Absolute(string assetOrProjectPath)
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Project root is unavailable.");
            return Path.GetFullPath(Path.Combine(
                root, assetOrProjectPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string RelativePath(Transform item, Transform root)
        {
            var parts = new List<string>();
            Transform current = item;
            while (current != null)
            {
                parts.Add(current.name);
                if (current == root) break;
                current = current.parent;
            }
            if (current == null)
                throw new InvalidOperationException(item.name + " is outside " + root.name + ".");
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string BonePath(Transform item, Transform root)
        {
            return AnimationUtility.CalculateTransformPath(item, root);
        }

        private static string SanitizeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return string.IsNullOrWhiteSpace(value) ? "PresenceDetector_Original" : value;
        }

        private static string Sha256File(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Sha256Text(string value)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(
                    sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", string.Empty);
        }

        private static string Vec(Vector3 value)
        {
            return string.Format(
                CultureInfo.InvariantCulture, "({0:R},{1:R},{2:R})", value.x, value.y, value.z);
        }

        private static string Quat(Quaternion value)
        {
            return string.Format(
                CultureInfo.InvariantCulture, "({0:R},{1:R},{2:R},{3:R})",
                value.x, value.y, value.z, value.w);
        }

        private static void RequireNear(Vector3 actual, Vector3 expected, string label)
        {
            if ((actual - expected).sqrMagnitude > PositionTolerance * PositionTolerance)
                throw new InvalidOperationException(
                    label + " differs. Actual=" + Vec(actual) + ", expected=" + Vec(expected));
        }

        private static void RequireNear(
            Quaternion actual,
            Quaternion expected,
            string label)
        {
            if (Quaternion.Angle(actual, expected) > RotationTolerance)
                throw new InvalidOperationException(
                    label + " differs. Actual=" + Quat(actual) +
                    ", expected=" + Quat(expected));
        }

        private static void RequireEqual(string actual, string expected, string label)
        {
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    label + " differs. Actual=" + actual + ", expected=" + expected);
        }

        private readonly struct AppearanceInspection
        {
            internal AppearanceInspection(int materialCount, int rendererCount)
            {
                MaterialCount = materialCount;
                RendererCount = rendererCount;
            }

            internal int MaterialCount { get; }
            internal int RendererCount { get; }
        }
    }
}
