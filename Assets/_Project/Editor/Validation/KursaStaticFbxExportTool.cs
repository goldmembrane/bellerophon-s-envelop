using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.KursaCargoRunScene
{
    internal static class KursaStaticFbxExportTool
    {
        private const string FbxPackageName = "com.unity.formats.fbx";
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Kursa Enemy Placement";
        private const string StaticSlotName = "Kursa_01_Static_Review";
        private const string ModelName = "Kursa_Model";
        private const string OutputFilePath = "enemies model/Kursa_Static.fbx";
        private static readonly TimeSpan PackageRequestTimeout = TimeSpan.FromMinutes(3);

        [MenuItem("Bellerophon/Enemies/Kursa/Install FBX Exporter Dependency")]
        public static void InstallKursaFbxExporterDependency()
        {
            var installed = UnityEditor.PackageManager.PackageInfo
                .GetAllRegisteredPackages()
                .FirstOrDefault(info => string.Equals(
                    info.name,
                    FbxPackageName,
                    StringComparison.Ordinal));
            if (installed != null)
            {
                Debug.Log(
                    "KursaFbxExporterDependencyInstalled Result=PASS, Package=" +
                    installed.name + ", Version=" + installed.version +
                    ", AlreadyInstalled=True.");
                return;
            }

            AddRequest request = Client.Add(FbxPackageName);
            var deadline = DateTime.UtcNow + PackageRequestTimeout;
            while (!request.IsCompleted)
            {
                if (DateTime.UtcNow >= deadline)
                {
                    throw new TimeoutException(
                        "Unity Package Manager did not finish installing " +
                        FbxPackageName + " within " +
                        PackageRequestTimeout.TotalMinutes.ToString("0") +
                        " minutes.");
                }

                Thread.Sleep(100);
            }

            if (request.Status != StatusCode.Success || request.Result == null)
            {
                var message = request.Error != null
                    ? request.Error.message
                    : "Unity Package Manager returned no package result.";
                throw new InvalidOperationException(
                    "Could not install " + FbxPackageName + ": " + message);
            }

            Debug.Log(
                "KursaFbxExporterDependencyInstalled Result=PASS, Package=" +
                request.Result.name + ", Version=" + request.Result.version +
                ", AlreadyInstalled=False, DependencyKept=True.");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Export Static Rigged FBX")]
        public static void ExportKursaStaticFbx()
        {
            RequireFbxPackage();
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var sourceModel = RequireSourceModel(scene);
            RequireRiggedModel(sourceModel);
            var outputAbsolutePath = ProjectAbsolutePath(OutputFilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputAbsolutePath));

            var previewScene = EditorSceneManager.NewPreviewScene();
            GameObject exportClone = null;
            try
            {
                exportClone = UnityEngine.Object.Instantiate(sourceModel);
                exportClone.name = "Kursa_Static";
                exportClone.hideFlags = HideFlags.HideAndDontSave;
                SceneManager.MoveGameObjectToScene(exportClone, previewScene);
                exportClone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                DisableAnimationComponents(exportClone);

                var exportedPath = ExportModelOnlyWithInstalledPackage(
                    outputAbsolutePath,
                    exportClone);
                if (string.IsNullOrWhiteSpace(exportedPath) || !File.Exists(outputAbsolutePath))
                {
                    throw new InvalidOperationException(
                        "Unity FBX Exporter did not create " + OutputFilePath + ".");
                }
            }
            finally
            {
                if (exportClone != null)
                    UnityEngine.Object.DestroyImmediate(exportClone);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }

            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Kursa static FBX export changed the CargoRunMvp scene dirty state.");
            }

            Debug.Log(
                "KursaStaticFbxExported Result=PASS, File=" + OutputFilePath +
                ", Source=ApprovedKursaEnemyPlacement/Kursa01StaticReview/KursaModel" +
                ", Rig=True, Skin=True, MaterialSlots=True, Animation=False" +
                ", SceneChanged=False, PackageDependencyKept=True.");
        }

        private static void RequireRiggedModel(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException("Kursa has no SkinnedMeshRenderer.");
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMesh == null || renderer.bones == null ||
                    renderer.bones.Length == 0 || renderer.sharedMaterials.Length == 0)
                {
                    throw new InvalidOperationException(
                        "Kursa is missing its skin, bones, or material slots.");
                }
            }
        }

        private static void DisableAnimationComponents(GameObject root)
        {
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
                animator.enabled = false;
            foreach (var animation in root.GetComponentsInChildren<Animation>(true))
                animation.enabled = false;
        }

        private static GameObject RequireSourceModel(Scene scene)
        {
            var placement = scene.GetRootGameObjects()
                .FirstOrDefault(root => string.Equals(
                    root.name,
                    PlacementRootName,
                    StringComparison.Ordinal));
            if (placement == null)
                throw new InvalidOperationException(
                    "Kursa placement root is missing: " + PlacementRootName);
            var staticSlot = RequireDirectChild(placement.transform, StaticSlotName);
            return RequireDirectChild(staticSlot, ModelName).gameObject;
        }

        private static Transform RequireDirectChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (string.Equals(child.name, childName, StringComparison.Ordinal))
                    return child;
            }
            throw new InvalidOperationException(
                "Required child is missing: " + parent.name + "/" + childName);
        }

        private static Scene RequireCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must already be the active scene. ActiveScene=" +
                    scene.path + ".");
            }
            return scene;
        }

        private static void RequireFbxPackage()
        {
            var installed = UnityEditor.PackageManager.PackageInfo
                .GetAllRegisteredPackages()
                .Any(info => string.Equals(
                    info.name,
                    FbxPackageName,
                    StringComparison.Ordinal));
            if (!installed)
                throw new InvalidOperationException(FbxPackageName + " is not installed.");
        }

        private static string ExportModelOnlyWithInstalledPackage(
            string outputAbsolutePath,
            GameObject exportRoot)
        {
            const string assemblyName = "Unity.Formats.Fbx.Editor";
            var exporterAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => string.Equals(
                    assembly.GetName().Name,
                    assemblyName,
                    StringComparison.Ordinal)) ?? Assembly.Load(assemblyName);
            var exporterType = exporterAssembly.GetType(
                "UnityEditor.Formats.Fbx.Exporter.ModelExporter",
                throwOnError: true);
            var optionsType = exporterAssembly.GetType(
                "UnityEditor.Formats.Fbx.Exporter.ExportModelOptions",
                throwOnError: true);
            var options = Activator.CreateInstance(optionsType);
            SetEnumOption(optionsType, options, "ExportFormat", "Binary");
            SetEnumOption(optionsType, options, "ModelAnimIncludeOption", "Model");
            SetEnumOption(optionsType, options, "ObjectPosition", "Reset");
            SetBooleanOption(optionsType, options, "AnimateSkinnedMesh", false);
            SetBooleanOption(optionsType, options, "UseMayaCompatibleNames", false);
            SetBooleanOption(optionsType, options, "ExportUnrendered", true);
            SetBooleanOption(optionsType, options, "PreserveImportSettings", false);
            SetBooleanOption(optionsType, options, "KeepInstances", true);
            SetBooleanOption(optionsType, options, "EmbedTextures", false);

            var exportMethod = exporterType.GetMethods(
                    BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method =>
                {
                    if (!string.Equals(method.Name, "ExportObject", StringComparison.Ordinal))
                        return false;
                    var parameters = method.GetParameters();
                    return parameters.Length == 3 &&
                        parameters[0].ParameterType == typeof(string) &&
                        parameters[1].ParameterType == typeof(UnityEngine.Object) &&
                        parameters[2].ParameterType == optionsType;
                });
            if (exportMethod == null)
            {
                throw new MissingMethodException(
                    "Unity FBX Exporter ExportObject API was not found.");
            }

            return exportMethod.Invoke(
                null,
                new object[] { outputAbsolutePath, exportRoot, options }) as string;
        }

        private static void SetEnumOption(
            Type optionsType,
            object options,
            string propertyName,
            string enumValue)
        {
            var property = optionsType.GetProperty(propertyName);
            if (property == null || !property.CanWrite || !property.PropertyType.IsEnum)
                throw new MissingMemberException(optionsType.FullName, propertyName);
            property.SetValue(options, Enum.Parse(property.PropertyType, enumValue));
        }

        private static void SetBooleanOption(
            Type optionsType,
            object options,
            string propertyName,
            bool value)
        {
            var property = optionsType.GetProperty(propertyName);
            if (property == null || !property.CanWrite || property.PropertyType != typeof(bool))
                throw new MissingMemberException(optionsType.FullName, propertyName);
            property.SetValue(options, value);
        }

        private static string ProjectAbsolutePath(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                projectRelativePath));
        }
    }
}
