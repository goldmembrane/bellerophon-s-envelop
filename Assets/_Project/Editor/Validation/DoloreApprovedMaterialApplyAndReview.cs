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
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.DoloreApprovedMaterial
{
    internal static class DoloreApprovedMaterialApplyAndReview
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SourceModelPath = "D:/Bellerophon2/Bellerophon/enemies model/dolore.fbx";
        private const string SourceModelHash = "0A8DF2A16B881B24A5FC856E2E3534A05D506049CE4C49975BB8433E71A2204E";
        private const string ApprovedSourceModelPath = "artSample/enemies/dolore/exports/Dolore_CurrentModel_ReferenceSync.fbx";
        private const string ApprovedSourceModelHash = "F5FC1F20A88AE5FFB882AF26C45F3278D173157EF1302AFA95A89B062B3F3491";
        private const string ApprovedModelFolder = "Assets/_Project/Art/Enemies/Dolore/ApprovedSample/Models";
        private const string ApprovedModelPath = ApprovedModelFolder + "/Dolore_CurrentModel_ReferenceSync.fbx";
        private const string TextureFolder = "Assets/_Project/Art/Enemies/Dolore/ApprovedSample/Textures";
        private const string MaterialFolder = "Assets/_Project/Art/Enemies/Dolore/ApprovedSample/Materials";
        private const string PlacementRootName = "Approved Dolore Enemy Placement";
        private const string ModelName = "Dolore_Model";
        private const string UrpLitShaderName = "Universal Render Pipeline/Lit";
        private const int ExpectedSourceControlPointCount = 2223;
        private const int ExpectedImportedVertexCount = 5173;
        private const int ExpectedPolygonCount = 4139;
        private const int ExpectedBoneCount = 27;
        private const int ExpectedMaterialCount = 3;
        private const int CaptureLayer = 31;

        private const string BodyAlbedoSource = "artSample/enemies/dolore/textures/dolore_body_albedo.png";
        private const string BodyRoughnessSource = "artSample/enemies/dolore/textures/dolore_body_roughness.png";
        private const string BodyHeightSource = "artSample/enemies/dolore/textures/dolore_body_height.png";
        private const string FrameAlbedoSource = "artSample/enemies/dolore/textures/dolore_frame_albedo.png";
        private const string FrameRoughnessSource = "artSample/enemies/dolore/textures/dolore_frame_roughness.png";
        private const string FrameHeightSource = "artSample/enemies/dolore/textures/dolore_frame_height.png";
        private const string PortraitSource = "artSample/enemies/dolore/textures/dolore_portrait.png";

        private const string BodyAlbedoPath = TextureFolder + "/dolore_body_albedo.png";
        private const string BodyRoughnessPath = TextureFolder + "/dolore_body_roughness.png";
        private const string BodyHeightPath = TextureFolder + "/dolore_body_height.png";
        private const string BodyMaskPath = TextureFolder + "/dolore_body_metallic_smoothness.png";
        private const string FrameAlbedoPath = TextureFolder + "/dolore_frame_albedo.png";
        private const string FrameRoughnessPath = TextureFolder + "/dolore_frame_roughness.png";
        private const string FrameHeightPath = TextureFolder + "/dolore_frame_height.png";
        private const string FrameMaskPath = TextureFolder + "/dolore_frame_metallic_smoothness.png";
        private const string PortraitPath = TextureFolder + "/dolore_portrait.png";

        private const string BodyMaterialPath = MaterialFolder + "/Dolore_Wet_Deep_Teal_Tissue.mat";
        private const string FrameMaterialPath = MaterialFolder + "/Dolore_Oxidized_Brass_Frame.mat";
        private const string PortraitMaterialPath = MaterialFolder + "/Dolore_Faded_Portrait.mat";

        private static readonly string[] SlotNames =
        {
            "Dolore_01_Static_Review",
            "Dolore_02_Idle",
            "Dolore_03_Move_Quadruped",
            "Dolore_04_Tentacle_Stab_Attack",
            "Dolore_05_Execution_Pull_In",
            "Dolore_06_Hit_Reaction",
            "Dolore_07_Death"
        };

        [MenuItem("Bellerophon/Enemies/Dolore/Apply Approved Exact Sample")]
        public static void ApplyApprovedMaterialToCurrentCargoRunScene()
        {
            RequireHash(SourceModelPath, SourceModelHash, "The supplied Dolore source FBX changed.");
            PrepareApprovedAssets();
            var approvedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ApprovedModelPath) ??
                                 throw new InvalidOperationException("Approved Dolore FBX was not imported.");
            RequireApprovedPrefabContract(approvedPrefab);
            var materials = RequireApprovedMaterialsInPrefabOrder(approvedPrefab);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var placementRoot = RequirePlacementRoot(scene);
            var slots = RequireSlots(placementRoot);
            var protectedBefore = ProtectedRootSignatures(scene, placementRoot);
            var slotBefore = slots.Select(CaptureSlotState).ToArray();

            try
            {
                for (var index = 0; index < slots.Length; index++)
                {
                    ReplaceSlotModel(scene, slots[index], approvedPrefab, materials, slotBefore[index]);
                }

                var protectedAfter = ProtectedRootSignatures(scene, placementRoot);
                if (!protectedBefore.SequenceEqual(protectedAfter, StringComparer.Ordinal))
                    throw new InvalidOperationException("A scene root outside Approved Dolore Enemy Placement changed.");
                for (var index = 0; index < slots.Length; index++)
                    RequireSlotStatePreserved(slots[index], slotBefore[index]);
                RequireAppliedState(scene, false);

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("CargoRunMvp could not be saved after exact Dolore sample application.");
                AssetDatabase.SaveAssets();
            }
            catch
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                throw;
            }

            RemoveObsoleteProjectionAssets();
            RequireHash(SourceModelPath, SourceModelHash, "The supplied Dolore source FBX changed during application.");
            RequireHash(ProjectAbsolutePath(ApprovedModelPath), ApprovedSourceModelHash, "The Unity approved FBX is not the approved sample export.");
            Selection.activeObject = null;
            Debug.Log(
                "DoloreApprovedExactSampleApplied Result=PASS, Slots=7, SourceControlPoints=" +
                ExpectedSourceControlPointCount + ", ImportedVertices=" + ExpectedImportedVertexCount +
                ", Polygons=4139, Bones=27, " +
                "UvLayers=1, MaterialSlots=3, BodyTriangles=2697, FrameTriangles=1175, PortraitTriangles=267, " +
                "VertexPositionsChanged=False, TopologyChanged=False, SlotTransformsChanged=False, " +
                "AnimationsChanged=False, CollidersChanged=False, OtherSceneRootsChanged=False, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Inspect Approved Exact Sample")]
        public static void InspectApprovedMaterialState()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RequireAppliedState(scene, true);
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Capture Approved Exact Comparison")]
        public static void CaptureApprovedMaterialReview()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RequireAppliedState(scene, false);
            var model = RequireSlots(RequirePlacementRoot(scene))[0].GetChild(0);
            var outputPath = Path.Combine(Path.GetTempPath(), "Bellerophon_DoloreApprovedExactComparison.png");
            CaptureComparison(scene, model, outputPath);
            var restored = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (restored.isDirty)
                throw new InvalidOperationException("Exact Dolore comparison capture left CargoRunMvp dirty.");
            Debug.Log("DoloreApprovedExactComparisonCaptured Result=PASS, Image=" + outputPath + ", SceneChanged=False.");
        }

        private static void PrepareApprovedAssets()
        {
            EnsureFolder(ApprovedModelFolder);
            EnsureFolder(TextureFolder);
            EnsureFolder(MaterialFolder);
            CopyExact(ApprovedSourceModelPath, ApprovedModelPath);
            CopyExact(BodyAlbedoSource, BodyAlbedoPath);
            CopyExact(BodyRoughnessSource, BodyRoughnessPath);
            CopyExact(BodyHeightSource, BodyHeightPath);
            CopyExact(FrameAlbedoSource, FrameAlbedoPath);
            CopyExact(FrameRoughnessSource, FrameRoughnessPath);
            CopyExact(FrameHeightSource, FrameHeightPath);
            CopyExact(PortraitSource, PortraitPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ConfigureApprovedModelImporter();
            ConfigureTexture(BodyAlbedoPath, true, false, 0f);
            ConfigureTexture(BodyRoughnessPath, false, false, 0f);
            ConfigureTexture(BodyHeightPath, false, true, 0.07f);
            ConfigureTexture(FrameAlbedoPath, true, false, 0f);
            ConfigureTexture(FrameRoughnessPath, false, false, 0f);
            ConfigureTexture(FrameHeightPath, false, true, 0.07f);
            ConfigureTexture(PortraitPath, true, false, 0f);
            CreateMetallicSmoothnessTexture(BodyRoughnessSource, BodyMaskPath, 0f);
            CreateMetallicSmoothnessTexture(FrameRoughnessSource, FrameMaskPath, 0.48f);
            ConfigureTexture(BodyMaskPath, false, false, 0f);
            ConfigureTexture(FrameMaskPath, false, false, 0f);
            CreateOrUpdateApprovedMaterials();
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureApprovedModelImporter()
        {
            AssetDatabase.ImportAsset(ApprovedModelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(ApprovedModelPath) as ModelImporter ??
                           throw new InvalidOperationException("Approved Dolore ModelImporter is missing.");
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.importBlendShapes = true;
            importer.addCollider = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.SaveAndReimport();
        }

        private static void ConfigureTexture(string path, bool srgb, bool normalMap, float heightScale)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter ??
                           throw new InvalidOperationException("TextureImporter is missing: " + path);
            importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = srgb && !normalMap;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;
            if (normalMap)
            {
                importer.convertToNormalmap = true;
                importer.heightmapScale = heightScale;
                importer.normalmapFilter = TextureImporterNormalFilter.Standard;
            }
            importer.SaveAndReimport();
        }

        private static void CreateMetallicSmoothnessTexture(string roughnessSource, string destinationPath, float metallic)
        {
            var source = DecodeTexture(ProjectAbsolutePath(roughnessSource));
            try
            {
                var sourcePixels = source.GetPixels32();
                var outputPixels = new Color32[sourcePixels.Length];
                var metallicByte = (byte)Mathf.RoundToInt(Mathf.Clamp01(metallic) * 255f);
                for (var index = 0; index < sourcePixels.Length; index++)
                {
                    var roughness = sourcePixels[index].r;
                    outputPixels[index] = new Color32(metallicByte, 0, 0, (byte)(255 - roughness));
                }
                var output = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
                try
                {
                    output.SetPixels32(outputPixels);
                    output.Apply(false, false);
                    File.WriteAllBytes(ProjectAbsolutePath(destinationPath), output.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(output);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
            AssetDatabase.ImportAsset(destinationPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static void CreateOrUpdateApprovedMaterials()
        {
            var shader = Shader.Find(UrpLitShaderName) ??
                         throw new InvalidOperationException("URP Lit shader is unavailable.");
            var body = GetOrCreateMaterial(BodyMaterialPath, "Dolore_Wet_Deep_Teal_Tissue", shader);
            ConfigureLitMaterial(body, RequireTexture(BodyAlbedoPath), RequireTexture(BodyMaskPath),
                RequireTexture(BodyHeightPath), 1f, 0.24f, RequireTexture(BodyAlbedoPath),
                Color.white * 0.08f, 0.16f, 0.66f);
            var frame = GetOrCreateMaterial(FrameMaterialPath, "Dolore_Oxidized_Brass_Frame", shader);
            ConfigureLitMaterial(frame, RequireTexture(FrameAlbedoPath), RequireTexture(FrameMaskPath),
                RequireTexture(FrameHeightPath), 1f, 0.18f, RequireTexture(FrameAlbedoPath),
                Color.white * 0.12f, 0f, 0f);
            var portrait = GetOrCreateMaterial(PortraitMaterialPath, "Dolore_Faded_Portrait", shader);
            ConfigureLitMaterial(portrait, RequireTexture(PortraitPath), null, null,
                0.18f, 0f, RequireTexture(PortraitPath), Color.white * 0.14f, 0f, 0f);
        }

        private static Material GetOrCreateMaterial(string path, string name, Shader shader)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            return material;
        }

        private static void ConfigureLitMaterial(
            Material material,
            Texture2D albedo,
            Texture2D metallicSmoothness,
            Texture2D normal,
            float smoothness,
            float normalScale,
            Texture2D emission,
            Color emissionColor,
            float clearCoatMask,
            float clearCoatSmoothness)
        {
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", albedo);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_Blend", 0f);
            // Blender's approved materials render both sides; preserve that surface visibility in URP.
            material.SetFloat("_Cull", 0f);
            material.SetFloat("_ZWrite", 1f);
            material.SetFloat("_Metallic", metallicSmoothness != null ? 1f : 0f);
            material.SetFloat("_Smoothness", smoothness);
            material.SetTexture("_MetallicGlossMap", metallicSmoothness);
            SetKeyword(material, "_METALLICSPECGLOSSMAP", metallicSmoothness != null);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", normalScale);
            SetKeyword(material, "_NORMALMAP", normal != null);
            material.SetTexture("_EmissionMap", emission);
            material.SetColor("_EmissionColor", emissionColor);
            SetKeyword(material, "_EMISSION", emission != null && emissionColor.maxColorComponent > 0f);
            material.SetFloat("_ClearCoatMask", clearCoatMask);
            material.SetFloat("_ClearCoatSmoothness", clearCoatSmoothness);
            SetKeyword(material, "_CLEARCOAT", clearCoatMask > 0f);
            material.renderQueue = -1;
            material.globalIlluminationFlags = emission != null
                ? MaterialGlobalIlluminationFlags.RealtimeEmissive
                : MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);
        }

        private static void SetKeyword(Material material, string keyword, bool enabled)
        {
            if (enabled) material.EnableKeyword(keyword);
            else material.DisableKeyword(keyword);
        }

        private static void ReplaceSlotModel(
            Scene scene,
            Transform slot,
            GameObject approvedPrefab,
            Material[] materials,
            SlotState before)
        {
            var oldModel = slot.GetChild(0);
            var localPosition = oldModel.localPosition;
            var localRotation = oldModel.localRotation;
            var localScale = oldModel.localScale;
            var siblingIndex = oldModel.GetSiblingIndex();
            UnityEngine.Object.DestroyImmediate(oldModel.gameObject);

            var model = PrefabUtility.InstantiatePrefab(approvedPrefab, scene) as GameObject ??
                        throw new InvalidOperationException("Approved Dolore FBX could not be instantiated.");
            model.name = ModelName;
            model.transform.SetParent(slot, false);
            model.transform.SetLocalPositionAndRotation(localPosition, localRotation);
            model.transform.localScale = localScale;
            model.transform.SetSiblingIndex(siblingIndex);
            foreach (var animator in model.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            foreach (var animation in model.GetComponentsInChildren<Animation>(true)) animation.enabled = false;
            var renderer = RequireApprovedRenderer(model.transform);
            renderer.sharedMaterials = materials;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(model);

            RequireMeshContract(renderer);
            if (renderer.bones.Select(bone => bone.name).OrderBy(name => name, StringComparer.Ordinal)
                .SequenceEqual(before.BoneNames, StringComparer.Ordinal) == false)
                throw new InvalidOperationException(slot.name + " approved rig bone names differ from the supplied model.");
            if (model.GetComponentsInChildren<Collider>(true).Length != before.ColliderCount)
                throw new InvalidOperationException(slot.name + " collider count changed.");
            var bounds = renderer.bounds;
            if (!Approximately(bounds.center, before.Bounds.center, 0.035f) ||
                !Approximately(bounds.size, before.Bounds.size, 0.035f))
                throw new InvalidOperationException(slot.name + " approved sample bounds differ from the placed source model.");
        }

        private static void RequireAppliedState(Scene scene, bool logResult)
        {
            var wasDirty = scene.isDirty;
            RequireHash(SourceModelPath, SourceModelHash, "The supplied Dolore source FBX changed.");
            RequireHash(ProjectAbsolutePath(ApprovedModelPath), ApprovedSourceModelHash, "The Unity approved FBX differs from the approved sample export.");
            var approvedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ApprovedModelPath) ??
                                 throw new InvalidOperationException("Approved Dolore FBX was not imported.");
            var materials = RequireApprovedMaterialsInPrefabOrder(approvedPrefab);
            var slots = RequireSlots(RequirePlacementRoot(scene));
            foreach (var slot in slots)
            {
                if (slot.childCount != 1 || slot.GetChild(0).name != ModelName)
                    throw new InvalidOperationException(slot.name + " must contain exactly Dolore_Model.");
                var model = slot.GetChild(0);
                var source = PrefabUtility.GetCorrespondingObjectFromSource(model.gameObject);
                if (source == null || AssetDatabase.GetAssetPath(source) != ApprovedModelPath)
                    throw new InvalidOperationException(slot.name + " is not a direct approved sample FBX instance.");
                var renderer = RequireApprovedRenderer(model);
                RequireMeshContract(renderer);
                if (!renderer.sharedMaterials.SequenceEqual(materials))
                    throw new InvalidOperationException(slot.name + " approved material order changed.");
                if (model.GetComponentsInChildren<Animator>(true).Any(item => item.enabled) ||
                    model.GetComponentsInChildren<Animation>(true).Any(item => item.enabled))
                    throw new InvalidOperationException(slot.name + " must remain a static review placeholder.");
            }
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Exact Dolore inspection changed scene dirty state.");
            Selection.activeObject = null;
            if (logResult)
            {
                Debug.Log(
                    "DoloreApprovedExactSampleInspected Result=PASS, Slots=7, DirectApprovedFbxInstances=7, " +
                    "SourceControlPoints=" + ExpectedSourceControlPointCount + ", ImportedVertices=" +
                    ExpectedImportedVertexCount + ", Polygons=4139, Bones=27, UvLayers=1, MaterialSlots=3, " +
                    "BodyTriangles=2697, FrameTriangles=1175, PortraitTriangles=267, SceneChanged=False.");
            }
        }

        private static void RequireApprovedPrefabContract(GameObject prefab)
        {
            var renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length != 1)
                throw new InvalidOperationException("Approved Dolore FBX must contain exactly one skinned renderer.");
            RequireMeshContract(renderers[0]);
        }

        private static SkinnedMeshRenderer RequireApprovedRenderer(Transform model)
        {
            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(false)
                .Where(item => item.enabled && item.gameObject.activeInHierarchy).ToArray();
            if (renderers.Length != 1)
                throw new InvalidOperationException(model.name + " must display exactly one approved skinned renderer.");
            return renderers[0];
        }

        private static void RequireMeshContract(SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh ?? throw new InvalidOperationException("Approved Dolore mesh is missing.");
            if (mesh.vertexCount != ExpectedImportedVertexCount || mesh.subMeshCount != ExpectedMaterialCount)
                throw new InvalidOperationException(
                    "Approved Dolore vertex or material-slot contract changed. " +
                    "Vertices=" + mesh.vertexCount + ", SubMeshes=" + mesh.subMeshCount + ".");
            if (!mesh.HasVertexAttribute(VertexAttribute.TexCoord0))
                throw new InvalidOperationException("Approved Dolore UV0 is missing.");
            if (!mesh.HasVertexAttribute(VertexAttribute.Normal) || !mesh.HasVertexAttribute(VertexAttribute.Tangent))
                throw new InvalidOperationException("Approved Dolore smooth normals or tangents are missing.");
            if (renderer.bones.Length != ExpectedBoneCount)
                throw new InvalidOperationException("Approved Dolore bone count changed: " + renderer.bones.Length + ".");
            var totalTriangles = 0;
            if (renderer.sharedMaterials.Length != ExpectedMaterialCount)
                throw new InvalidOperationException("Approved Dolore renderer material count changed.");
            var materialNames = renderer.sharedMaterials
                .Select(material => material != null ? material.name : string.Empty).ToArray();
            if (materialNames.Distinct(StringComparer.Ordinal).Count() != ExpectedMaterialCount)
                throw new InvalidOperationException("Approved Dolore renderer material names are missing or duplicated.");
            for (var index = 0; index < mesh.subMeshCount; index++)
            {
                var triangles = checked((int)mesh.GetIndexCount(index) / 3);
                var expectedTriangles = ExpectedTriangleCount(materialNames[index]);
                if (triangles != expectedTriangles)
                    throw new InvalidOperationException(
                        "Approved Dolore material/submesh contract changed at " + index + ": " +
                        materialNames[index] + "=" + triangles + ".");
                totalTriangles += triangles;
            }
            if (totalTriangles != ExpectedPolygonCount)
                throw new InvalidOperationException("Approved Dolore polygon count changed: " + totalTriangles + ".");
        }

        private static Material[] RequireApprovedMaterials()
        {
            return new[]
            {
                AssetDatabase.LoadAssetAtPath<Material>(BodyMaterialPath) ?? throw new InvalidOperationException("Approved body material is missing."),
                AssetDatabase.LoadAssetAtPath<Material>(FrameMaterialPath) ?? throw new InvalidOperationException("Approved frame material is missing."),
                AssetDatabase.LoadAssetAtPath<Material>(PortraitMaterialPath) ?? throw new InvalidOperationException("Approved portrait material is missing.")
            };
        }

        private static Material[] RequireApprovedMaterialsInPrefabOrder(GameObject approvedPrefab)
        {
            var canonical = RequireApprovedMaterials().ToDictionary(material => material.name, StringComparer.Ordinal);
            var renderer = approvedPrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
            return renderer.sharedMaterials.Select(imported =>
            {
                if (imported == null || !canonical.TryGetValue(imported.name, out var approved))
                    throw new InvalidOperationException("Approved FBX material mapping is missing: " +
                                                        (imported != null ? imported.name : "<null>") + ".");
                return approved;
            }).ToArray();
        }

        private static int ExpectedTriangleCount(string materialName)
        {
            return materialName switch
            {
                "Dolore_Wet_Deep_Teal_Tissue" => 2697,
                "Dolore_Oxidized_Brass_Frame" => 1175,
                "Dolore_Faded_Portrait" => 267,
                _ => throw new InvalidOperationException("Unexpected approved FBX material: " + materialName + ".")
            };
        }

        private static Texture2D RequireTexture(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path) ??
                   throw new InvalidOperationException("Approved texture is missing: " + path);
        }

        private static SlotState CaptureSlotState(Transform slot)
        {
            if (slot.childCount != 1)
                throw new InvalidOperationException(slot.name + " must contain exactly one source model before replacement.");
            var renderer = slot.GetChild(0).GetComponentsInChildren<SkinnedMeshRenderer>(false)
                .Single(item => item.enabled && item.gameObject.activeInHierarchy);
            return new SlotState
            {
                SlotLocalPosition = slot.localPosition,
                SlotLocalRotation = slot.localRotation,
                SlotLocalScale = slot.localScale,
                SlotSiblingIndex = slot.GetSiblingIndex(),
                ModelLocalPosition = slot.GetChild(0).localPosition,
                ModelLocalRotation = slot.GetChild(0).localRotation,
                ModelLocalScale = slot.GetChild(0).localScale,
                Bounds = renderer.bounds,
                ColliderCount = slot.GetChild(0).GetComponentsInChildren<Collider>(true).Length,
                BoneNames = renderer.bones.Select(bone => bone.name).OrderBy(name => name, StringComparer.Ordinal).ToArray()
            };
        }

        private static void RequireSlotStatePreserved(Transform slot, SlotState before)
        {
            if (!Approximately(slot.localPosition, before.SlotLocalPosition, 0.00001f) ||
                Quaternion.Angle(slot.localRotation, before.SlotLocalRotation) > 0.001f ||
                !Approximately(slot.localScale, before.SlotLocalScale, 0.00001f) ||
                slot.GetSiblingIndex() != before.SlotSiblingIndex)
                throw new InvalidOperationException(slot.name + " placement transform changed.");
            var model = slot.GetChild(0);
            if (!Approximately(model.localPosition, before.ModelLocalPosition, 0.00001f) ||
                Quaternion.Angle(model.localRotation, before.ModelLocalRotation) > 0.001f ||
                !Approximately(model.localScale, before.ModelLocalScale, 0.00001f))
                throw new InvalidOperationException(slot.name + " model transform changed.");
        }

        private static GameObject RequirePlacementRoot(Scene scene)
        {
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be the active scene.");
            return scene.GetRootGameObjects().SingleOrDefault(item => item.name == PlacementRootName) ??
                   throw new InvalidOperationException("Approved Dolore placement root is missing.");
        }

        private static Transform[] RequireSlots(GameObject root)
        {
            if (root.transform.childCount != SlotNames.Length)
                throw new InvalidOperationException("Approved Dolore placement must contain exactly seven slots.");
            var slots = new Transform[SlotNames.Length];
            for (var index = 0; index < SlotNames.Length; index++)
            {
                var slot = root.transform.GetChild(index);
                if (slot.name != SlotNames[index])
                    throw new InvalidOperationException("Dolore slot order or name changed at index " + index + ".");
                slots[index] = slot;
            }
            return slots;
        }

        private static string[] ProtectedRootSignatures(Scene scene, GameObject excludedRoot)
        {
            return scene.GetRootGameObjects().Where(root => root != excludedRoot)
                .OrderBy(root => root.name, StringComparer.Ordinal)
                .Select(root =>
                {
                    var builder = new StringBuilder(root.name);
                    AppendTransformTree(builder, root.transform, root.transform);
                    return builder.ToString();
                }).ToArray();
        }

        private static void AppendTransformTree(StringBuilder builder, Transform current, Transform root)
        {
            builder.Append('|').Append(TransformPath(current, root))
                .Append(" P=").Append(Vector(current.localPosition))
                .Append(" R=").Append(QuaternionValue(current.localRotation))
                .Append(" S=").Append(Vector(current.localScale))
                .Append(" A=").Append(current.gameObject.activeSelf);
            for (var index = 0; index < current.childCount; index++)
                AppendTransformTree(builder, current.GetChild(index), root);
        }

        private static string TransformPath(Transform current, Transform root)
        {
            if (current == root) return current.name;
            var names = new Stack<string>();
            var cursor = current;
            while (cursor != null && cursor != root)
            {
                names.Push(cursor.name);
                cursor = cursor.parent;
            }
            return root.name + "/" + string.Join("/", names);
        }

        private static void CaptureComparison(Scene scene, Transform sourceModel, string outputPath)
        {
            GameObject clone = null;
            GameObject cameraObject = null;
            var lights = new List<GameObject>();
            var captures = new List<Texture2D>();
            var references = new List<Texture2D>();
            try
            {
                clone = UnityEngine.Object.Instantiate(sourceModel.gameObject);
                clone.name = "Dolore_Approved_Exact_Capture_Model";
                clone.transform.SetParent(null);
                clone.transform.SetPositionAndRotation(Vector3.zero, sourceModel.rotation);
                clone.transform.localScale = sourceModel.lossyScale;
                SetLayerRecursively(clone.transform, CaptureLayer);
                var renderer = RequireApprovedRenderer(clone.transform);
                renderer.updateWhenOffscreen = true;
                var bounds = renderer.bounds;

                cameraObject = new GameObject("Dolore_Approved_Exact_Capture_Camera");
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.018f, 0.024f, 0.023f, 1f);
                camera.orthographic = false;
                // Blender uses a 62 mm lens on a 36 mm horizontal sensor for the approved views.
                camera.fieldOfView = 24.55f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 50f;

                lights.Add(CreateDirectionalLight("Dolore_Capture_Key", new Color(0.78f, 0.90f, 0.87f), 1.25f,
                    Quaternion.LookRotation(new Vector3(-0.45f, -0.65f, -0.62f).normalized)));
                lights.Add(CreateDirectionalLight("Dolore_Capture_WarmFill", new Color(0.95f, 0.66f, 0.38f), 0.48f,
                    Quaternion.LookRotation(new Vector3(0.68f, -0.32f, -0.45f).normalized)));
                lights.Add(CreateDirectionalLight("Dolore_Capture_TealRim", new Color(0.20f, 0.70f, 0.62f), 0.62f,
                    Quaternion.LookRotation(new Vector3(-0.15f, -0.25f, 0.82f).normalized)));

                var slot = sourceModel.parent;
                var front = slot.forward.normalized;
                var side = slot.right.normalized;
                captures.Add(CaptureView(camera, bounds, front, 0.12f, 1024, 768));
                captures.Add(CaptureView(camera, bounds, side, 0.12f, 1024, 768));
                captures.Add(CaptureView(camera, bounds, -front, 0.20f, 1024, 768));
                references.Add(DecodeTexture(ProjectAbsolutePath("artSample/enemies/dolore/renders/02_front.png")));
                references.Add(DecodeTexture(ProjectAbsolutePath("artSample/enemies/dolore/renders/03_side.png")));
                references.Add(DecodeTexture(ProjectAbsolutePath("artSample/enemies/dolore/renders/04_back.png")));
                SaveComparisonSheet(references, captures, outputPath);
            }
            finally
            {
                foreach (var texture in captures.Concat(references))
                    if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
                foreach (var light in lights)
                    if (light != null) UnityEngine.Object.DestroyImmediate(light);
                if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
                if (clone != null) UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static GameObject CreateDirectionalLight(string name, Color color, float intensity, Quaternion rotation)
        {
            var lightObject = new GameObject(name);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.cullingMask = 1 << CaptureLayer;
            lightObject.transform.rotation = rotation;
            return lightObject;
        }

        private static Texture2D CaptureView(
            Camera camera,
            Bounds bounds,
            Vector3 viewDirection,
            float heightFraction,
            int width,
            int height)
        {
            var distance = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z)) * 2.50f;
            camera.transform.position = bounds.center + viewDirection * distance + Vector3.up * bounds.size.y * heightFraction;
            camera.transform.rotation = Quaternion.LookRotation((bounds.center - camera.transform.position).normalized, Vector3.up);
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                var capture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                capture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                capture.Apply(false, false);
                return capture;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
            }
        }

        private static void SaveComparisonSheet(IReadOnlyList<Texture2D> references, IReadOnlyList<Texture2D> captures, string outputPath)
        {
            const int panelWidth = 1024;
            const int panelHeight = 768;
            var sheet = new Texture2D(panelWidth * 2, panelHeight * 3, TextureFormat.RGBA32, false, false);
            try
            {
                var clear = Enumerable.Repeat(new Color32(4, 6, 6, 255), sheet.width * sheet.height).ToArray();
                sheet.SetPixels32(clear);
                for (var row = 0; row < 3; row++)
                {
                    var y = (2 - row) * panelHeight;
                    sheet.SetPixels32(0, y, panelWidth, panelHeight, references[row].GetPixels32());
                    sheet.SetPixels32(panelWidth, y, panelWidth, panelHeight, captures[row].GetPixels32());
                }
                sheet.Apply(false, false);
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (var index = 0; index < root.childCount; index++)
                SetLayerRecursively(root.GetChild(index), layer);
        }

        private static Texture2D DecodeTexture(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException("PNG could not be decoded: " + path);
            }
            return texture;
        }

        private static void RemoveObsoleteProjectionAssets()
        {
            foreach (var path in new[]
                     {
                         MaterialFolder + "/Dolore_ApprovedSample.mat",
                         MaterialFolder + "/DoloreApprovedSampleProjection.shader",
                         TextureFolder + "/Dolore_Approved_Front.png",
                         TextureFolder + "/Dolore_Approved_Side.png",
                         TextureFolder + "/Dolore_Approved_Back.png",
                         TextureFolder + "/Dolore_Approved_Body.png"
                     })
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) != null) AssetDatabase.DeleteAsset(path);
            }
            AssetDatabase.SaveAssets();
        }

        private static void CopyExact(string sourceRelativePath, string destinationAssetPath)
        {
            var source = ProjectAbsolutePath(sourceRelativePath);
            var destination = ProjectAbsolutePath(destinationAssetPath);
            if (!File.Exists(source)) throw new FileNotFoundException("Approved sample asset is missing.", source);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Approved asset destination is invalid."));
            File.Copy(source, destination, true);
            RequireSameHash(Sha256(source), Sha256(destination), "Approved sample asset was not copied byte-for-byte: " + sourceRelativePath);
        }

        private static void EnsureFolder(string path)
        {
            var segments = path.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }

        private static string ProjectAbsolutePath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string Sha256(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Required file is missing.", path);
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static void RequireHash(string path, string expected, string message)
        {
            RequireSameHash(Sha256(path), expected, message);
        }

        private static void RequireSameHash(string left, string right, string message)
        {
            if (!string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(message);
        }

        private static bool Approximately(Vector3 left, Vector3 right, float tolerance)
        {
            return Vector3.Distance(left, right) <= tolerance;
        }

        private static string Vector(Vector3 value)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:R},{1:R},{2:R})", value.x, value.y, value.z);
        }

        private static string QuaternionValue(Quaternion value)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:R},{1:R},{2:R},{3:R})", value.x, value.y, value.z, value.w);
        }

        private sealed class SlotState
        {
            public Vector3 SlotLocalPosition;
            public Quaternion SlotLocalRotation;
            public Vector3 SlotLocalScale;
            public int SlotSiblingIndex;
            public Vector3 ModelLocalPosition;
            public Quaternion ModelLocalRotation;
            public Vector3 ModelLocalScale;
            public Bounds Bounds;
            public int ColliderCount;
            public string[] BoneNames;
        }
    }
}
