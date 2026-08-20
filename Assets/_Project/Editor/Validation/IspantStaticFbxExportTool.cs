using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.IspantCargoRunScene
{
    internal static class IspantStaticFbxExportTool
    {
        private const string FbxPackageName = "com.unity.formats.fbx";
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ispant Enemy Placement";
        private const string SourceSlotName = "Ispant_01_Static";
        private const string ModelName = "Ispant_New_Direct_Model";
        private const string ApprovedModelPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_Direct_Source.fbx";
        private const string OutputFilePath = "enemies model/išpant_new_static.fbx";
        private const int ExpectedSlots = 12;
        private const int ExpectedRenderers = 2;
        private const int ExpectedTriangles = 9798 + 19950;

        private static readonly string[] ExpectedMeshNames =
        {
            "char1",
            "Ispant_Approved_LongSword_10K"
        };

        [MenuItem("Bellerophon/Enemies/Ispant/Export Current Static FBX")]
        public static void ExportIspantStaticFbx()
        {
            RequireFbxPackage();
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var sourceModel = RequireSourceModel(scene);
            var sourceRenderers = RequireCurrentRenderers(sourceModel);
            var outputAbsolutePath = ProjectAbsolutePath(OutputFilePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(outputAbsolutePath) ??
                throw new InvalidOperationException("The Ispant FBX output folder is invalid."));

            var previewScene = EditorSceneManager.NewPreviewScene();
            GameObject exportRoot = null;
            var bakedMeshes = new List<Mesh>();
            var bakedBounds = new Bounds();
            var bakedVertexCount = 0;
            try
            {
                exportRoot = new GameObject("ispant_new_static")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                SceneManager.MoveGameObjectToScene(exportRoot, previewScene);
                exportRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                exportRoot.transform.localScale = Vector3.one;

                foreach (var sourceRenderer in sourceRenderers)
                {
                    var mesh = BakeStaticMesh(sourceRenderer, sourceModel.transform);
                    bakedMeshes.Add(mesh);
                    var meshObject = new GameObject(mesh.name);
                    meshObject.transform.SetParent(exportRoot.transform, false);
                    var filter = meshObject.AddComponent<MeshFilter>();
                    filter.sharedMesh = mesh;
                    var renderer = meshObject.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials = sourceRenderer.sharedMaterials;
                }

                RequireStaticExportRoot(exportRoot);
                bakedBounds = CombinedBounds(bakedMeshes);
                bakedVertexCount = bakedMeshes.Sum(mesh => mesh.vertexCount);
                var exportedPath = ExportModelOnlyWithInstalledPackage(
                    outputAbsolutePath,
                    exportRoot);
                if (string.IsNullOrWhiteSpace(exportedPath) ||
                    !File.Exists(outputAbsolutePath))
                {
                    throw new InvalidOperationException(
                        "Unity FBX Exporter did not create " + OutputFilePath + ".");
                }
            }
            finally
            {
                if (exportRoot != null)
                    UnityEngine.Object.DestroyImmediate(exportRoot);
                foreach (var mesh in bakedMeshes)
                    UnityEngine.Object.DestroyImmediate(mesh);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }

            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Ispant static FBX export changed the CargoRunMvp scene dirty state.");
            }

            var outputInfo = new FileInfo(outputAbsolutePath);
            Debug.Log(
                "IspantStaticFbxExported Result=PASS" +
                ", File=" + OutputFilePath +
                ", Source=" + PlacementRootName + "/" + SourceSlotName + "/" + ModelName +
                ", Meshes=" + ExpectedRenderers +
                ", BakedVertices=" + bakedVertexCount +
                ", Triangles=" + ExpectedTriangles +
                ", BakedBoundsCenter=" + VectorText(bakedBounds.center) +
                ", BakedBoundsSize=" + VectorText(bakedBounds.size) +
                ", Rig=False, Bones=0, Skin=False" +
                ", BoneWeights=0, BindPoses=0" +
                ", Animation=False, BlendShapes=0" +
                ", MaterialSlotsPreserved=True" +
                ", FileBytes=" + outputInfo.Length +
                ", SceneChanged=False.");
        }

        private static Renderer[] RequireCurrentRenderers(GameObject sourceModel)
        {
            var renderers = sourceModel.GetComponentsInChildren<Renderer>(true)
                .OrderBy(item => SharedMesh(item).name, StringComparer.Ordinal)
                .ToArray();
            if (renderers.Length != ExpectedRenderers)
            {
                throw new InvalidOperationException(
                    "The current Ispant must contain exactly the direct body and approved long-sword renderers. " +
                    "Actual=" + renderers.Length + ".");
            }

            var meshNames = renderers
                .Select(item => SharedMesh(item).name)
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray();
            var expectedNames = ExpectedMeshNames
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray();
            if (!meshNames.SequenceEqual(expectedNames, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "The current Ispant renderer mesh names differ: " +
                    string.Join(",", meshNames) + ".");
            }
            if (renderers.Sum(item => TriangleCount(SharedMesh(item))) != ExpectedTriangles)
            {
                throw new InvalidOperationException(
                    "The current Ispant triangle total differs from the approved placed model.");
            }
            if (renderers.Any(item =>
                    item.sharedMaterials.Length != SharedMesh(item).subMeshCount ||
                    item.sharedMaterials.Any(material => material == null)))
            {
                throw new InvalidOperationException(
                    "The current Ispant material slots are incomplete.");
            }
            return renderers;
        }

        private static Mesh BakeStaticMesh(Renderer sourceRenderer, Transform sourceRoot)
        {
            Mesh baked;
            if (sourceRenderer is SkinnedMeshRenderer skinned)
            {
                baked = new Mesh();
                skinned.BakeMesh(baked, false);
            }
            else
            {
                baked = UnityEngine.Object.Instantiate(SharedMesh(sourceRenderer));
            }
            baked.name = SharedMesh(sourceRenderer).name;
            RemoveRigAndAnimationData(baked);

            var relative = sourceRoot.worldToLocalMatrix *
                           sourceRenderer.transform.localToWorldMatrix;
            if (sourceRenderer is not SkinnedMeshRenderer)
            {
                var rootScale = sourceRoot.lossyScale;
                if (!Mathf.Approximately(rootScale.x, rootScale.y) ||
                    !Mathf.Approximately(rootScale.y, rootScale.z))
                {
                    UnityEngine.Object.DestroyImmediate(baked);
                    throw new InvalidOperationException(
                        "The current Ispant root must use uniform scale for static mesh baking.");
                }

                // Skinned BakeMesh already contains the placed root scale through its bones.
                // A regular MeshRenderer does not, so preserve that same scale explicitly.
                relative = Matrix4x4.Scale(Vector3.one * rootScale.x) * relative;
            }
            TransformMeshIntoRootSpace(baked, relative);
            if (baked.vertexCount == 0 || baked.subMeshCount == 0)
            {
                UnityEngine.Object.DestroyImmediate(baked);
                throw new InvalidOperationException(
                    "The current Ispant renderer baked an empty mesh: " + sourceRenderer.name + ".");
            }
            RequireStaticMeshData(baked);
            return baked;
        }

        private static void RemoveRigAndAnimationData(Mesh mesh)
        {
            mesh.bindposes = Array.Empty<Matrix4x4>();
            mesh.boneWeights = Array.Empty<BoneWeight>();
            mesh.ClearBlendShapes();
        }

        private static void RequireStaticMeshData(Mesh mesh)
        {
            if (mesh.bindposes.Length != 0 ||
                mesh.boneWeights.Length != 0 ||
                mesh.blendShapeCount != 0)
            {
                throw new InvalidOperationException(
                    "The baked Ispant mesh still contains rig or animation data: " +
                    mesh.name + ".");
            }
        }

        private static void TransformMeshIntoRootSpace(Mesh mesh, Matrix4x4 matrix)
        {
            var vertices = mesh.vertices;
            for (var index = 0; index < vertices.Length; index++)
                vertices[index] = matrix.MultiplyPoint3x4(vertices[index]);
            mesh.vertices = vertices;

            var normalMatrix = matrix.inverse.transpose;
            var normals = mesh.normals;
            for (var index = 0; index < normals.Length; index++)
                normals[index] = normalMatrix.MultiplyVector(normals[index]).normalized;
            mesh.normals = normals;

            var tangents = mesh.tangents;
            var mirrored = matrix.determinant < 0f;
            for (var index = 0; index < tangents.Length; index++)
            {
                var transformed = matrix.MultiplyVector(new Vector3(
                    tangents[index].x,
                    tangents[index].y,
                    tangents[index].z)).normalized;
                tangents[index] = new Vector4(
                    transformed.x,
                    transformed.y,
                    transformed.z,
                    mirrored ? -tangents[index].w : tangents[index].w);
            }
            mesh.tangents = tangents;

            if (mirrored)
            {
                for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                {
                    var triangles = mesh.GetTriangles(subMesh);
                    for (var index = 0; index < triangles.Length; index += 3)
                    {
                        var swap = triangles[index + 1];
                        triangles[index + 1] = triangles[index + 2];
                        triangles[index + 2] = swap;
                    }
                    mesh.SetTriangles(triangles, subMesh, false);
                }
            }
            mesh.RecalculateBounds();
        }

        private static void RequireStaticExportRoot(GameObject exportRoot)
        {
            if (exportRoot.GetComponentsInChildren<MeshFilter>(true).Length !=
                    ExpectedRenderers ||
                exportRoot.GetComponentsInChildren<MeshRenderer>(true).Length !=
                    ExpectedRenderers ||
                exportRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length != 0 ||
                exportRoot.GetComponentsInChildren<Animator>(true).Length != 0 ||
                exportRoot.GetComponentsInChildren<Animation>(true).Length != 0)
            {
                throw new InvalidOperationException(
                    "The Ispant export root is not a pure two-mesh static model.");
            }
            var triangles = exportRoot.GetComponentsInChildren<MeshFilter>(true)
                .Sum(item => TriangleCount(item.sharedMesh));
            if (triangles != ExpectedTriangles)
            {
                throw new InvalidOperationException(
                    "The baked Ispant triangle total differs. Triangles=" + triangles + ".");
            }
            foreach (var filter in exportRoot.GetComponentsInChildren<MeshFilter>(true))
            {
                RequireStaticMeshData(filter.sharedMesh);
            }
        }

        private static Bounds CombinedBounds(IReadOnlyList<Mesh> meshes)
        {
            if (meshes.Count == 0)
                throw new InvalidOperationException("The baked Ispant contains no meshes.");
            var bounds = meshes[0].bounds;
            for (var index = 1; index < meshes.Count; index++)
                bounds.Encapsulate(meshes[index].bounds);
            return bounds;
        }

        private static string VectorText(Vector3 value)
        {
            return "(" +
                   value.x.ToString("R", CultureInfo.InvariantCulture) + "," +
                   value.y.ToString("R", CultureInfo.InvariantCulture) + "," +
                   value.z.ToString("R", CultureInfo.InvariantCulture) + ")";
        }

        private static GameObject RequireSourceModel(Scene scene)
        {
            var placementRoots = scene.GetRootGameObjects()
                .Where(root => string.Equals(
                    root.name,
                    PlacementRootName,
                    StringComparison.Ordinal))
                .ToArray();
            if (placementRoots.Length != 1 ||
                placementRoots[0].transform.childCount != ExpectedSlots)
            {
                throw new InvalidOperationException(
                    "The approved Ispant twelve-slot placement contract differs.");
            }

            var slot = RequireDirectChild(placementRoots[0].transform, SourceSlotName);
            var model = RequireDirectChild(slot, ModelName).gameObject;
            var source = PrefabUtility.GetCorrespondingObjectFromSource(model);
            if (source == null ||
                !string.Equals(
                    AssetDatabase.GetAssetPath(source),
                    ApprovedModelPath,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The first Ispant slot is not the current approved model instance.");
            }
            return model;
        }

        private static Transform RequireDirectChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (string.Equals(child.name, childName, StringComparison.Ordinal))
                    return child;
            }
            throw new InvalidOperationException(
                "Required child is missing: " + parent.name + "/" + childName + ".");
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

        private static Mesh SharedMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
                return skinned.sharedMesh;
            var filter = renderer.GetComponent<MeshFilter>();
            return filter != null && filter.sharedMesh != null
                ? filter.sharedMesh
                : throw new InvalidOperationException(
                    "An Ispant renderer has no mesh: " + renderer.name + ".");
        }

        private static int TriangleCount(Mesh mesh)
        {
            var result = 0;
            for (var index = 0; index < mesh.subMeshCount; index++)
                result += checked((int)mesh.GetIndexCount(index) / 3);
            return result;
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
            SetBooleanOption(optionsType, options, "KeepInstances", false);
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
