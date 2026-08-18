using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.IspantCargoRunScene
{
    internal static class IspantModelReplacementTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ispant Enemy Placement";
        private const string DirectSourcePath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_Direct_Source.fbx";
        private const string DirectSourceSha256 =
            "7B682C49CDB2F4A563B736857E544DD7FCCD7213A5C375DEB92F56CE3A2E51B7";
        private const string DirectTextureFolder =
            "Assets/_Project/Art/Enemies/Ispant/Models/Textures";
        private const string DirectTextureExtractionFolder =
            DirectTextureFolder + "/Ispant_New_Direct_Source_Extracted";
        private const string DirectTexturePath =
            DirectTextureFolder + "/Ispant_New_Direct_Source_BaseColor.png";
        private const string DirectTextureSha256 =
            "7DE6705FB7BD60E2D347023EFE51E96598845D611631F40D01C64EACC5249570";
        private const string DirectInstanceName = "Ispant_New_Direct_Model";
        private const string ValidationFolder =
            "docs/validation/ispant_new_direct_replacement_2026-08-18";
        private const string AppearanceDiagnosticPath =
            ValidationFolder + "/Ispant_Unity_Appearance_Diagnostic.txt";
        private const string CleanupInspectionPath =
            ValidationFolder + "/Ispant_Unity_Side_Cleanup_Inspection.txt";

        private static readonly string[] UnauthorizedAssetPaths =
        {
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_Source.fbx",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_CustomRig.fbx",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_MixamoRig.fbx",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_DeathRig.fbx",
            "Assets/_Project/Art/Enemies/Ispant/Models/Generated/Ispant_New_CustomRig_Mesh.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Generated/Ispant_New_DeathRig_Mesh.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Materials/Ispant_New_Model.mat",
            "Assets/_Project/Art/Enemies/Ispant/Models/Textures/Ispant_New_BaseColor.jpg",
            "Assets/_Project/Art/Enemies/Ispant/Models/Textures/Ispant_New_Normal.jpg",
            "Assets/_Project/Art/Enemies/Ispant/Models/Textures/Ispant_New_Metallic.png",
            "Assets/_Project/Art/Enemies/Ispant/Models/Textures/Ispant_New_Roughness.png",
        };

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Unity Side Appearance")]
        public static void InspectUnitySideAppearance()
        {
            var scene = RequireScene();
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var source = RequireSource();
            WriteText(AppearanceDiagnosticPath, BuildAppearanceReport(scene, placement, source));
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Unity-side appearance inspection changed the scene dirty state.");
            Debug.Log(
                "IspantUnitySideAppearanceInspected Result=PASS, SceneChanged=False, Report=" +
                AppearanceDiagnosticPath + ".");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Unity Side Cleanup")]
        public static void ApplyUnitySideCleanup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Unity Play Mode must remain stopped for Ispant cleanup.");
            var scene = RequireScene();
            if (scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp has unsaved changes before Ispant cleanup.");
            var placement = RequirePlacement(scene);
            var otherRootsBefore = ProtectedRootSignatures(scene);
            var preservedSlotsBefore = PreservedSlotContract(placement);

            ConfigureSourceFaithfulImporter();
            var source = RequireSource();
            RequireSourceFaithfulMaterial(source);

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Remove unauthorized Ispant Unity-side modifications");
            try
            {
                foreach (Transform slot in placement)
                {
                    var visualRoots = VisualRoots(slot);
                    if (visualRoots.Length != 1)
                        throw new InvalidOperationException(slot.name + " must contain exactly one visual root before cleanup.");
                    Undo.DestroyObjectImmediate(visualRoots[0].gameObject);
                    var instance = PrefabUtility.InstantiatePrefab(source, scene) as GameObject ??
                        throw new InvalidOperationException("The source-faithful Ispant FBX could not be instantiated.");
                    Undo.RegisterCreatedObjectUndo(instance, "Place source-faithful Ispant FBX");
                    instance.name = DirectInstanceName;
                    instance.transform.SetParent(slot, false);
                    instance.transform.localPosition = source.transform.localPosition;
                    instance.transform.localRotation = source.transform.localRotation;
                    instance.transform.localScale = source.transform.localScale;
                    ConfigureStaticInstance(instance.transform);
                    EditorUtility.SetDirty(instance);
                    EditorUtility.SetDirty(slot);
                }

                if (!otherRootsBefore.SequenceEqual(ProtectedRootSignatures(scene), StringComparer.Ordinal))
                    throw new InvalidOperationException("A scene root outside the Ispant placement changed.");
                if (!preservedSlotsBefore.SequenceEqual(PreservedSlotContract(placement), StringComparer.Ordinal))
                    throw new InvalidOperationException("A preserved Ispant slot transform or non-visual component changed.");

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new InvalidOperationException("CargoRunMvp could not be saved after Ispant cleanup.");
                Undo.CollapseUndoOperations(undoGroup);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }

            DeleteUnauthorizedAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            source = RequireSource();
            WriteText(CleanupInspectionPath, InspectCleanupState(scene, RequirePlacement(scene), source));
            Debug.Log(
                "IspantUnitySideCleanupApplied Result=PASS, Slots=12, WrongAtaTextureReferences=0" +
                ", SourcePackedTextureHashMatched=True, UnauthorizedDerivedAssets=0" +
                ", AnimationConnections=0, OtherSceneRootsChanged=False, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Unity Side Cleanup")]
        public static void InspectUnitySideCleanup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Unity Play Mode must remain stopped for Ispant cleanup inspection.");
            var scene = RequireScene();
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var source = RequireSource();
            WriteText(CleanupInspectionPath, InspectCleanupState(scene, placement, source));
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Ispant cleanup inspection changed the scene dirty state.");
            Debug.Log(
                "IspantUnitySideCleanupInspected Result=PASS, Slots=12, WrongAtaTextureReferences=0" +
                ", SourcePackedTextureHashMatched=True, UnauthorizedDerivedAssets=0" +
                ", AnimationConnections=0, SceneChanged=False.");
        }

        private static void ConfigureSourceFaithfulImporter()
        {
            RequireHash(DirectSourcePath, DirectSourceSha256);
            EnsureAssetFolder(DirectTextureFolder);
            if (AssetDatabase.IsValidFolder(DirectTextureExtractionFolder) &&
                !AssetDatabase.DeleteAsset(DirectTextureExtractionFolder))
                throw new InvalidOperationException("The temporary source-texture extraction folder could not be cleared.");
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DirectTexturePath) != null &&
                !AssetDatabase.DeleteAsset(DirectTexturePath))
                throw new InvalidOperationException("The previous direct source texture could not be cleared.");

            var importer = RequireImporter();
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
            importer.materialSearch = ModelImporterMaterialSearch.Local;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();

            EnsureAssetFolder(DirectTextureExtractionFolder);
            importer = RequireImporter();
            if (!importer.ExtractTextures(DirectTextureExtractionFolder))
                throw new InvalidOperationException("Unity could not extract the FBX-packed Ispant texture.");
            AssetDatabase.Refresh();
            var extracted = AssetDatabase.FindAssets("t:Texture2D", new[] { DirectTextureExtractionFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .ToArray();
            if (extracted.Length != 1)
                throw new InvalidOperationException(
                    "Expected one FBX-packed texture but extracted " + extracted.Length + ".");
            RequireHash(extracted[0], DirectTextureSha256);
            var moveError = AssetDatabase.MoveAsset(extracted[0], DirectTexturePath);
            if (!string.IsNullOrEmpty(moveError))
                throw new InvalidOperationException("The exact extracted Ispant texture could not be moved: " + moveError);
            if (AssetDatabase.IsValidFolder(DirectTextureExtractionFolder) &&
                !AssetDatabase.DeleteAsset(DirectTextureExtractionFolder))
                throw new InvalidOperationException("The empty source-texture extraction folder could not be removed.");

            importer = RequireImporter();
            var importedSource = AssetDatabase.LoadAssetAtPath<GameObject>(DirectSourcePath) ??
                throw new InvalidOperationException("The direct Ispant source could not be loaded for texture remapping.");
            var importedMaterial = importedSource.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single().sharedMaterial ??
                throw new InvalidOperationException("The direct Ispant source material is missing during texture remapping.");
            var currentlyBoundTexture = importedMaterial.GetTexture("_BaseMap") ??
                throw new InvalidOperationException("The imported Ispant material has no current base texture identifier.");
            var exactPackedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(DirectTexturePath) ??
                throw new InvalidOperationException("The exact extracted Ispant texture could not be loaded.");
            importer.AddRemap(
                new AssetImporter.SourceAssetIdentifier(currentlyBoundTexture.GetType(), currentlyBoundTexture.name),
                exactPackedTexture);
            importer.SaveAndReimport();
            RequireHash(DirectTexturePath, DirectTextureSha256);
        }

        private static void DeleteUnauthorizedAssets()
        {
            foreach (var path in UnauthorizedAssetPaths)
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null && !AssetDatabase.DeleteAsset(path))
                    throw new InvalidOperationException("Unauthorized Ispant asset could not be removed: " + path);
            }
            foreach (var folder in new[]
                     {
                         "Assets/_Project/Art/Enemies/Ispant/Models/Generated",
                         "Assets/_Project/Art/Enemies/Ispant/Models/Materials",
                     })
            {
                if (AssetDatabase.IsValidFolder(folder) && AssetDatabase.FindAssets(string.Empty, new[] { folder }).Length == 0 &&
                    !AssetDatabase.DeleteAsset(folder))
                    throw new InvalidOperationException("Empty unauthorized Ispant folder could not be removed: " + folder);
            }
        }

        private static string InspectCleanupState(Scene scene, Transform placement, GameObject source)
        {
            RequireHash(DirectSourcePath, DirectSourceSha256);
            RequireHash(DirectTexturePath, DirectTextureSha256);
            var importer = RequireImporter();
            if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription ||
                importer.materialLocation != ModelImporterMaterialLocation.InPrefab ||
                importer.materialName != ModelImporterMaterialName.BasedOnMaterialName ||
                importer.materialSearch != ModelImporterMaterialSearch.Local || importer.importAnimation ||
                importer.importCameras || importer.importLights)
                throw new InvalidOperationException("The direct Ispant importer still contains non-source-faithful settings.");
            foreach (var path in UnauthorizedAssetPaths)
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
                    throw new InvalidOperationException("Unauthorized Ispant asset remains: " + path);

            var sourceRenderer = source.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
            RequireSourceFaithfulMaterial(source);
            var report = new StringBuilder();
            report.AppendLine("Ispant Unity-side cleanup inspection");
            report.AppendLine("Scene=" + scene.path);
            report.AppendLine("SourcePath=" + DirectSourcePath);
            report.AppendLine("SourceSha256=" + DirectSourceSha256);
            report.AppendLine("ExtractedPackedTexture=" + DirectTexturePath);
            report.AppendLine("ExtractedPackedTextureSha256=" + DirectTextureSha256);
            report.AppendLine(
                "Importer|MaterialImportMode=" + importer.materialImportMode +
                "|MaterialLocation=" + importer.materialLocation +
                "|MaterialName=" + importer.materialName +
                "|MaterialSearch=" + importer.materialSearch +
                "|ImportAnimation=" + importer.importAnimation +
                "|ImportCameras=" + importer.importCameras +
                "|ImportLights=" + importer.importLights);

            var clipCount = AssetDatabase.LoadAllAssetsAtPath(DirectSourcePath).OfType<AnimationClip>()
                .Count(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal));
            if (clipCount != 0)
                throw new InvalidOperationException("The direct Ispant FBX still imports an animation clip.");
            var instanceCount = 0;
            foreach (Transform slot in placement)
            {
                var model = VisualRoots(slot).Single();
                if (!string.Equals(model.name, DirectInstanceName, StringComparison.Ordinal) ||
                    !string.Equals(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(model.gameObject),
                                   DirectSourcePath, StringComparison.Ordinal))
                    throw new InvalidOperationException(slot.name + " does not directly instantiate the supplied FBX.");
                var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
                if (renderer.sharedMesh == null ||
                    !string.Equals(AssetDatabase.GetAssetPath(renderer.sharedMesh), DirectSourcePath, StringComparison.Ordinal) ||
                    !renderer.sharedMaterials.SequenceEqual(sourceRenderer.sharedMaterials))
                    throw new InvalidOperationException(slot.name + " has a derived mesh or material binding.");
                var materialOverrides = (PrefabUtility.GetPropertyModifications(model.gameObject) ??
                                         Array.Empty<PropertyModification>())
                    .Count(modification => modification != null && modification.propertyPath != null &&
                                           modification.propertyPath.IndexOf("material", StringComparison.OrdinalIgnoreCase) >= 0);
                if (materialOverrides != 0)
                    throw new InvalidOperationException(slot.name + " still has a material override.");
                foreach (var animator in model.GetComponentsInChildren<Animator>(true))
                    if (animator.enabled || animator.runtimeAnimatorController != null || animator.applyRootMotion)
                        throw new InvalidOperationException(slot.name + " still has an animation connection.");
                if (model.GetComponentsInChildren<Animation>(true).Any(animation => animation.enabled))
                    throw new InvalidOperationException(slot.name + " still has an enabled legacy animation component.");
                instanceCount++;
                report.AppendLine(
                    "Slot=" + slot.name + "|Prefab=" + DirectSourcePath +
                    "|Mesh=" + AssetDatabase.GetAssetPath(renderer.sharedMesh) +
                    "|Material=" + renderer.sharedMaterial.name + "@" +
                    AssetDatabase.GetAssetPath(renderer.sharedMaterial) +
                    "|MaterialOverrides=0|AnimationConnections=0");
            }
            if (instanceCount != 12)
                throw new InvalidOperationException("Expected 12 direct Ispant instances.");
            report.AppendLine("DirectInstances=12");
            report.AppendLine("WrongAtaTextureReferences=0");
            report.AppendLine("UnauthorizedDerivedAssets=0");
            report.AppendLine("ImportedAnimationClips=0");
            report.AppendLine("Result=PASS");
            return report.ToString();
        }

        private static string BuildAppearanceReport(Scene scene, Transform placement, GameObject source)
        {
            var importer = RequireImporter();
            var report = new StringBuilder();
            report.AppendLine("Ispant Unity-side appearance diagnostic");
            report.AppendLine("SourcePath=" + DirectSourcePath);
            report.AppendLine("SourceSha256=" + DirectSourceSha256);
            report.AppendLine(
                "Importer|MaterialImportMode=" + importer.materialImportMode +
                "|MaterialLocation=" + importer.materialLocation +
                "|MaterialName=" + importer.materialName +
                "|MaterialSearch=" + importer.materialSearch +
                "|ImportAnimation=" + importer.importAnimation +
                "|ImportCameras=" + importer.importCameras +
                "|ImportLights=" + importer.importLights);
            foreach (var mapping in importer.GetExternalObjectMap())
                report.AppendLine(
                    "ExternalObject|Type=" + mapping.Key.type + "|Name=" + mapping.Key.name +
                    "|Asset=" + AssetDatabase.GetAssetPath(mapping.Value));
            foreach (var material in AssetDatabase.LoadAllAssetsAtPath(DirectSourcePath).OfType<Material>())
            {
                report.AppendLine("Material|Name=" + material.name + "|Shader=" + material.shader.name);
                foreach (var property in material.GetTexturePropertyNames())
                {
                    var texture = material.GetTexture(property);
                    if (texture != null)
                        report.AppendLine(
                            "MaterialTexture|Property=" + property + "|Texture=" + texture.name +
                            "|Asset=" + AssetDatabase.GetAssetPath(texture));
                }
            }
            var sourceRenderer = source.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
            foreach (Transform slot in placement)
            {
                var model = VisualRoots(slot).Single();
                var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
                var materialOverrides = (PrefabUtility.GetPropertyModifications(model.gameObject) ??
                                         Array.Empty<PropertyModification>())
                    .Count(modification => modification != null && modification.propertyPath != null &&
                                           modification.propertyPath.IndexOf("material", StringComparison.OrdinalIgnoreCase) >= 0);
                report.AppendLine(
                    "Slot=" + slot.name + "|Materials=" + string.Join(",", renderer.sharedMaterials.Select(material =>
                        material.name + "@" + AssetDatabase.GetAssetPath(material))) +
                    "|MatchesSourceMaterials=" + renderer.sharedMaterials.SequenceEqual(sourceRenderer.sharedMaterials) +
                    "|MaterialOverrides=" + materialOverrides);
            }
            return report.ToString();
        }

        private static void RequireSourceFaithfulMaterial(GameObject source)
        {
            var renderer = source.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
            if (renderer.sharedMaterials.Length != 1 || renderer.sharedMaterial == null)
                throw new InvalidOperationException("The direct Ispant source does not contain one material.");
            var material = renderer.sharedMaterial;
            var texturePaths = material.GetTexturePropertyNames()
                .Select(property => material.GetTexture(property))
                .Where(texture => texture != null)
                .Select(AssetDatabase.GetAssetPath)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (texturePaths.Length != 1 ||
                !string.Equals(texturePaths[0], DirectTexturePath, StringComparison.Ordinal) ||
                texturePaths.Any(path => path.IndexOf("/Ata/", StringComparison.OrdinalIgnoreCase) >= 0))
                throw new InvalidOperationException("The direct Ispant material is not bound only to its packed texture.");
        }

        private static Scene RequireScene()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("CargoRunMvp is not loaded.");
            return scene;
        }

        private static Transform RequirePlacement(Scene scene)
        {
            var matches = scene.GetRootGameObjects()
                .Where(root => string.Equals(root.name, PlacementRootName, StringComparison.Ordinal))
                .ToArray();
            if (matches.Length != 1 || matches[0].transform.childCount != 12)
                throw new InvalidOperationException("Expected one Ispant placement root with 12 slots.");
            return matches[0].transform;
        }

        private static GameObject RequireSource()
        {
            RequireHash(DirectSourcePath, DirectSourceSha256);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(DirectSourcePath) ??
                throw new InvalidOperationException("The direct Ispant source FBX is missing.");
            var renderer = source.GetComponentsInChildren<SkinnedMeshRenderer>(true).SingleOrDefault();
            if (renderer == null || renderer.sharedMesh == null || renderer.sharedMesh.triangles.Length / 3 != 10028 ||
                renderer.rootBone == null || renderer.bones.Length != 24 || renderer.bones.Any(bone => bone == null))
                throw new InvalidOperationException("The direct Ispant source mesh or 24-bone rig differs from the FBX.");
            return source;
        }

        private static ModelImporter RequireImporter()
        {
            return AssetImporter.GetAtPath(DirectSourcePath) as ModelImporter ??
                throw new InvalidOperationException("The direct Ispant FBX has no ModelImporter.");
        }

        private static Transform[] VisualRoots(Transform slot)
        {
            return Enumerable.Range(0, slot.childCount)
                .Select(slot.GetChild)
                .Where(child => child.GetComponentsInChildren<Renderer>(true).Length > 0)
                .ToArray();
        }

        private static void ConfigureStaticInstance(Transform instance)
        {
            foreach (var animator in instance.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
                animator.runtimeAnimatorController = null;
                animator.applyRootMotion = false;
                EditorUtility.SetDirty(animator);
            }
            foreach (var animation in instance.GetComponentsInChildren<Animation>(true))
            {
                animation.enabled = false;
                EditorUtility.SetDirty(animation);
            }
        }

        private static string[] ProtectedRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(root => !string.Equals(root.name, PlacementRootName, StringComparison.Ordinal))
                .SelectMany(root => HierarchySignature(root.transform, root.transform))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static string[] PreservedSlotContract(Transform placement)
        {
            var values = new System.Collections.Generic.List<string>();
            foreach (Transform slot in placement)
            {
                var visualRoots = VisualRoots(slot);
                foreach (var transform in slot.GetComponentsInChildren<Transform>(true))
                {
                    if (visualRoots.Any(root => transform == root || transform.IsChildOf(root)))
                        continue;
                    values.Add("Transform|" + slot.name + "|" + RelativePath(slot, transform) + "|" +
                               transform.gameObject.activeSelf + "|" + TransformValue(transform));
                    foreach (var component in transform.GetComponents<Component>())
                    {
                        if (component == null || component is Transform || component is Animator || component is Animation)
                            continue;
                        values.Add("Component|" + slot.name + "|" + RelativePath(slot, transform) + "|" +
                                   component.GetType().FullName + "|" + EditorJsonUtility.ToJson(component));
                    }
                }
            }
            values.Sort(StringComparer.Ordinal);
            return values.ToArray();
        }

        private static string[] HierarchySignature(Transform root, Transform current)
        {
            var values = new System.Collections.Generic.List<string>
            {
                "Transform|" + root.name + "|" + RelativePath(root, current) + "|" +
                current.gameObject.activeSelf + "|" + TransformValue(current)
            };
            foreach (var component in current.GetComponents<Component>())
            {
                if (component == null || component is Transform)
                    continue;
                values.Add("Component|" + root.name + "|" + RelativePath(root, current) + "|" +
                           component.GetType().FullName + "|" + EditorJsonUtility.ToJson(component));
            }
            foreach (Transform child in current)
                values.AddRange(HierarchySignature(root, child));
            return values.ToArray();
        }

        private static string RelativePath(Transform root, Transform target)
        {
            if (target == root)
                return ".";
            var path = target.name;
            while (target.parent != null && target.parent != root)
            {
                target = target.parent;
                path = target.name + "/" + path;
            }
            return path;
        }

        private static string TransformValue(Transform transform)
        {
            return "Position=" + transform.localPosition.ToString("F6") +
                   "|Rotation=" + transform.localEulerAngles.ToString("F6") +
                   "|Scale=" + transform.localScale.ToString("F6");
        }

        private static void EnsureAssetFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        private static void RequireHash(string assetPath, string expected)
        {
            var absolute = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            using var stream = File.OpenRead(absolute);
            using var sha = SHA256.Create();
            var actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(assetPath + " hash differs from the exact supplied data.");
        }

        private static void WriteText(string path, string contents)
        {
            var absolute = Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, contents, new UTF8Encoding(false));
            AssetDatabase.Refresh();
        }
    }
}
