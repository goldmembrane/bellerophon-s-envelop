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

namespace Bellerophon.Editor.ResistanceCargoRunScene
{
    internal static class ResistanceApprovedAppearanceApplyAndReview
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Resistance Enemy Placement";
        private const string ModelName = "Resistance_Model";
        private const string ModelPath =
            "Assets/_Project/Art/Enemies/Resistance/Models/Resistance.fbx";
        private const string ModelSha256 =
            "282B05DA51DEE72291B865940AA84E83CD0F90E49BCABA0A480996AA21A7C303";
        private const string ShaderName =
            "Bellerophon/Resistance/ApprovedAppearance";
        private const string ShaderPath =
            "Assets/_Project/Art/Enemies/Resistance/Shaders/ResistanceApprovedAppearance.shader";
        private const string TextureRoot =
            "Assets/_Project/Art/Enemies/Resistance/Textures";
        private const string MaterialRoot =
            "Assets/_Project/Art/Enemies/Resistance/Materials";
        private const string MaterialPath =
            MaterialRoot + "/M_Resistance_ApprovedAppearance.mat";
        private const string ApprovedFaceMaskSourcePath =
            "artSample/enemies/resistance/textures/resistance_approved_face_material_mask.png";
        private const string FaceMapJsonPath =
            "artSample/enemies/resistance/geometry/approved_face_material_map.json";
        private const string FaceMaskPath =
            TextureRoot + "/resistance_approved_face_material_mask.png";
        private const string FaceMaskSha256 =
            "B679BB6CFC01309C7ACBCF271AD47D3F75797E365C26E7D6C9B13BB05999ABCF";
        private const string TriangleMapAPath =
            TextureRoot + "/T_Resistance_TriangleMapA.asset";
        private const string TriangleMapBPath =
            TextureRoot + "/T_Resistance_TriangleMapB.asset";
        private const string TriangleMapCPath =
            TextureRoot + "/T_Resistance_TriangleMapC.asset";
        private const string TriangleMaterialMapPath =
            TextureRoot + "/T_Resistance_TriangleMaterialMap.asset";
        private const string TrianglePanelAtlasPath =
            TextureRoot + "/resistance_approved_unity_panel_atlas.png";
        private const string TrianglePanelAtlasSha256 =
            "5322FE2D460F176128B72B1A158516AE1F5A85BDA75EB0EBB1DE8B6DE7949011";
        private const string InspectionPath =
            "docs/validation/resistance_appearance_2026-07-26/Resistance_Appearance_Inspection.txt";
        private const string CapturePath =
            "docs/validation/resistance_appearance_2026-07-26/Resistance_Appearance_VisualReview.png";
        private const int SlotCount = 14;
        // Unity splits the FBX's 3,004 authored control points at UV/normal seams.
        // The unchanged imported render mesh therefore exposes 4,565 vertices.
        private const int ExpectedVertexCount = 4565;
        private const int ExpectedTriangleCount = 6037;
        private const int ExpectedBoneCount = 24;
        private const float PositionTolerance = 0.0001f;
        private static readonly Vector3 ExpectedRootPosition =
            new Vector3(57.86535f, 0f, -141.252f);
        private static readonly Vector3 ExpectedPlayerPosition =
            new Vector3(72.54027f, 0f, -145.2395f);
        private static readonly Vector3 ExpectedCameraPosition =
            new Vector3(72.54027f, 1.62f, -145.2395f);
        private const float ExpectedSlotSpacing = 2.444763f;

        private static readonly TextureContract[] TextureContracts =
        {
            new TextureContract(
                "_SilverTex",
                "resistance_worn_silver_albedo.png",
                "0DD4EFE31B7A5D689793EFDA1DC5DB70C7316A622902676223B0B4F3567722E3",
                true),
            new TextureContract(
                "_DarkTex",
                "resistance_dark_mechanics_albedo.png",
                "B427EED17D575DC02C8F28F4B8A165C7A1114E2F94B1DB057385C197224368F7",
                true),
            new TextureContract(
                "_CyanTex",
                "resistance_cyan_emission_albedo.png",
                "41733BB2151121C24FD4125F8B14B4281B5E3A2E2B7020787708F0B3917CFF27",
                true),
            new TextureContract(
                "_BronzeTex",
                "resistance_bronze_accents_albedo.png",
                "D26036F5452B87697D2E709307A40325ACD1B31289962C4BF9F570814BFA789D",
                true),
            new TextureContract(
                "_OliveTex",
                "resistance_bandana_olive_albedo.png",
                "9CD863CCAEE6D9C2D70A90C5AC798D2936885DC1C186C93C5C3A17DC8D66FB2C",
                true),
            new TextureContract(
                "_RoughnessTex",
                "resistance_surface_roughness.png",
                "1A5E57350822BCD0CDB03F4DC88A26B4A281BA170756106CF0E2DF2B298A2CD2",
                false),
            new TextureContract(
                "_BumpTex",
                "resistance_surface_micro_bump.png",
                "1A5E57350822BCD0CDB03F4DC88A26B4A281BA170756106CF0E2DF2B298A2CD2",
                false),
            new TextureContract(
                "_ApprovedAlbedoTex",
                "resistance_approved_unity_albedo.png",
                "0EA64921E221E76FDE6CDC164C79858304D33C2243D6B10F553680E3672E1C35",
                true),
            new TextureContract(
                "_ApprovedEmissionTex",
                "resistance_approved_unity_emission.png",
                "66C33410482CB6A4C4E8481BA73A927D7F6610EF165C722718F2972DF780FA5B",
                true),
            new TextureContract(
                "_TriangleAtlasAlbedo",
                "resistance_approved_triangle_albedo.png",
                "972D1775CDB363E8393DBA6D50C87B297C2F952CF1EE048DC65DCE56522E4520",
                true),
            new TextureContract(
                "_TriangleAtlasEmission",
                "resistance_approved_triangle_emission.png",
                "8C7E76835B9ECCC409E90A003C8A33367F0ED9748A3B96443E9EA58BD5392060",
                true)
        };

        [MenuItem("Bellerophon/Enemies/Resistance/Apply Approved Appearance")]
        public static void ApplyApprovedAppearance()
        {
            RequireEditMode();
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                RequireModelHash();
                RequireTextureHashes();
                var interruptedMaterial = RequireApprovedMaterial();
                var interruptedInspection =
                    InspectCore(scene, interruptedMaterial);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException(
                        "CargoRunMvp could not be saved while completing the interrupted approved Resistance appearance apply.");
                }

                AssetDatabase.SaveAssets();
                LogApplyResult(interruptedInspection, true);
                return;
            }

            RequireModelHash();
            RequireTextureHashes();
            var protectedBefore = CaptureProtectedRootSignatures(scene);
            var root = RequirePlacementRoot(scene);
            var placementBefore = CapturePlacementStructure(root.transform);
            var meshesBefore = CaptureMeshSignatures(root.transform);

            ConfigureTextureImporters();
            ConfigureTriangleAtlasImporters();
            var faceMask = EnsureApprovedUvFaceMask();
            var triangleLookup =
                EnsureApprovedTriangleLookup(root.transform);
            var material = EnsureApprovedMaterial(
                faceMask,
                triangleLookup);
            ApplyMaterial(root.transform, material);

            var placementAfter = CapturePlacementStructure(root.transform);
            var meshesAfter = CaptureMeshSignatures(root.transform);
            RequireSequenceEqual(
                placementBefore,
                placementAfter,
                "Resistance placement transforms or hierarchy changed while applying appearance.");
            RequireSequenceEqual(
                meshesBefore,
                meshesAfter,
                "Resistance mesh references or topology changed while applying appearance.");
            RequireSequenceEqual(
                protectedBefore,
                CaptureProtectedRootSignatures(scene),
                "A scene root outside the Resistance placement changed.");

            var inspection = InspectCore(scene, material);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after applying the approved Resistance appearance.");
            }

            AssetDatabase.SaveAssets();
            RequireModelHash();
            RequireTextureHashes();
            LogApplyResult(inspection, false);
        }

        private static void LogApplyResult(
            Inspection inspection,
            bool completedInterruptedApply)
        {
            Debug.Log(
                "ApprovedResistanceAppearanceApplied Result=PASS, Slots=" +
                inspection.SlotCount.ToString(CultureInfo.InvariantCulture) +
                ", RenderersPerSlot=" +
                inspection.RenderersPerSlot.ToString(CultureInfo.InvariantCulture) +
                ", VertexCountPerSlot=" +
                inspection.VertexCountPerSlot.ToString(CultureInfo.InvariantCulture) +
                ", TriangleCountPerSlot=" +
                inspection.TriangleCountPerSlot.ToString(CultureInfo.InvariantCulture) +
                ", BoneCountPerSlot=" +
                inspection.BoneCountPerSlot.ToString(CultureInfo.InvariantCulture) +
                ", Shader=" + ShaderName +
                ", ModelSha256=" + ModelSha256 +
                ", ModelMeshChanged=False, PlacementTransformsPreserved=True, " +
                "PlayerAndCameraPreserved=True, OtherSceneRootsUnchanged=True, SceneSaved=True, " +
                "CompletedInterruptedApply=" + completedInterruptedApply + ".");
        }

        [MenuItem("Bellerophon/Enemies/Resistance/Inspect Approved Appearance")]
        public static void InspectApprovedAppearance()
        {
            RequireEditMode();
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            RequireModelHash();
            RequireTextureHashes();
            var material = RequireApprovedMaterial();
            var inspection = InspectCore(scene, material);
            var placementRoot = RequirePlacementRoot(scene);
            var sourceRenderer = placementRoot.transform
                .GetChild(0)
                .GetChild(0)
                .GetComponentsInChildren<Renderer>(true)
                .Single();
            var sourceMesh = MeshOf(sourceRenderer) ??
                             throw new InvalidOperationException(
                                 "Resistance source mesh is missing.");
            var sourceVertices = sourceMesh.vertices;
            var sourceNormals = sourceMesh.normals;
            var torsoVertexIndices = Enumerable.Range(
                    0,
                    sourceVertices.Length)
                .Where(index =>
                    Mathf.Abs(sourceVertices[index].x) <= 0.13f &&
                    sourceVertices[index].y >= 1.03f &&
                    sourceVertices[index].y <= 1.45f)
                .ToArray();
            var report = new StringBuilder()
                .AppendLine("Resistance Approved Appearance Inspection")
                .AppendLine("Result=PASS")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("PlacementRoot=" + PlacementRootName)
                .AppendLine("Slots=" + inspection.SlotCount)
                .AppendLine("SlotNames=Resistance_01..Resistance_14")
                .AppendLine("ModelAsset=" + ModelPath)
                .AppendLine("ModelSha256=" + ModelSha256)
                .AppendLine("MaterialAsset=" + MaterialPath)
                .AppendLine("Shader=" + ShaderName)
                .AppendLine("RenderersPerSlot=" + inspection.RenderersPerSlot)
                .AppendLine("VertexCountPerSlot=" + inspection.VertexCountPerSlot)
                .AppendLine("TriangleCountPerSlot=" + inspection.TriangleCountPerSlot)
                .AppendLine("SubMeshCountPerSlot=" + inspection.SubMeshCountPerSlot)
                .AppendLine("BoneCountPerSlot=" + inspection.BoneCountPerSlot)
                .AppendLine("UnityMeshBoundsMin=" +
                            Format(sourceMesh.bounds.min))
                .AppendLine("UnityMeshBoundsMax=" +
                            Format(sourceMesh.bounds.max))
                .AppendLine("TorsoDiagnosticVertices=" +
                            torsoVertexIndices.Length)
                .AppendLine("TorsoNormalZPositive=" +
                            torsoVertexIndices.Count(
                                index => sourceNormals[index].z > 0.16f))
                .AppendLine("TorsoNormalZNegative=" +
                            torsoVertexIndices.Count(
                                index => sourceNormals[index].z < -0.16f))
                .AppendLine("TorsoDepthMin=" +
                            Format(
                                torsoVertexIndices.Min(
                                    index => sourceVertices[index].z)))
                .AppendLine("TorsoDepthMax=" +
                            Format(
                                torsoVertexIndices.Max(
                                    index => sourceVertices[index].z)))
                .AppendLine("RootPosition=" + Format(inspection.RootPosition))
                .AppendLine("SlotSpacing=" + Format(inspection.SlotSpacing))
                .AppendLine("PlayerPosition=" + Format(inspection.PlayerPosition))
                .AppendLine("CameraPosition=" + Format(inspection.CameraPosition))
                .AppendLine("CameraForward=" + Format(inspection.CameraForward))
                .AppendLine("TextureHashesVerified=True")
                .AppendLine("ApprovedUvAlbedoSha256=" +
                            "0EA64921E221E76FDE6CDC164C79858304D33C2243D6B10F553680E3672E1C35")
                .AppendLine("ApprovedUvEmissionSha256=" +
                            "66C33410482CB6A4C4E8481BA73A927D7F6610EF165C722718F2972DF780FA5B")
                .AppendLine("ApprovedFaceMaskSha256=" +
                            FaceMaskSha256)
                .AppendLine("BakedDirectlyFromApprovedBlend=True")
                .AppendLine("ApprovedPanelAtlasSha256=" +
                            TrianglePanelAtlasSha256)
                .AppendLine("PanelAtlasUsesOriginalUnityTriangles=True")
                .AppendLine("AllRenderersUseApprovedMaterial=True")
                .AppendLine("DirectOriginalFbxInstances=14")
                .AppendLine("ModelMeshChanged=False")
                .AppendLine("AnimationChanged=False")
                .AppendLine("SceneChanged=False")
                .ToString();
            WriteText(Absolute(InspectionPath), report);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Resistance appearance inspection changed the scene dirty state.");
            }

            Debug.Log(
                "ApprovedResistanceAppearanceInspected Result=PASS, Report=" +
                InspectionPath + ", Slots=" + inspection.SlotCount +
                ", VertexCountPerSlot=" + inspection.VertexCountPerSlot +
                ", TriangleCountPerSlot=" + inspection.TriangleCountPerSlot +
                ", Shader=" + ShaderName + ", ModelMeshChanged=False, SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Resistance/Capture Approved Appearance Final")]
        public static void CaptureApprovedAppearanceFinal()
        {
            RequireEditMode();
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var material = RequireApprovedMaterial();
            InspectCore(scene, material);
            var player = RequireRoot(scene, "Player");
            var camera = player.GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("Player camera is missing.");
            Capture(camera, Absolute(CapturePath), 1920, 1080);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Resistance appearance capture changed the scene dirty state.");
            }

            Debug.Log(
                "ApprovedResistanceAppearanceCaptured Result=PASS, Image=" +
                CapturePath + ", FocusSlot=Resistance_07, CaptureCount=1, SceneChanged=False.");
        }

        private static Inspection InspectCore(Scene scene, Material material)
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                throw new InvalidOperationException(
                    "Unity reports script compilation errors.");
            }

            if (material.shader == null ||
                material.shader.name != ShaderName ||
                material.shader.name.Contains(
                    "InternalErrorShader",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The approved Resistance material shader is missing or invalid.");
            }

            var faceMask = RequireApprovedFaceMask();
            if (!material.HasProperty("_FaceMaterialMask") ||
                material.GetTexture("_FaceMaterialMask") != faceMask)
            {
                throw new InvalidOperationException(
                    "The approved Resistance face-material map is not assigned.");
            }

            var triangleLookup = RequireApprovedTriangleLookup();
            if (material.GetTexture("_TriangleMapA") !=
                    triangleLookup.MapA ||
                material.GetTexture("_TriangleMapB") !=
                    triangleLookup.MapB ||
                material.GetTexture("_TriangleMapC") !=
                    triangleLookup.MapC ||
                material.GetTexture("_TriangleMaterialMap") !=
                    triangleLookup.MaterialMap ||
                material.GetTexture("_TrianglePanelAtlas") !=
                    triangleLookup.PanelAtlas)
            {
                throw new InvalidOperationException(
                    "The approved Resistance per-triangle atlas lookup is not assigned.");
            }

            var root = RequirePlacementRoot(scene);
            if (Vector3.Distance(root.transform.position, ExpectedRootPosition) >
                PositionTolerance ||
                root.transform.childCount != SlotCount)
            {
                throw new InvalidOperationException(
                    "Resistance placement root position or slot count changed.");
            }

            var renderersPerSlot = -1;
            var verticesPerSlot = -1;
            var trianglesPerSlot = -1;
            var subMeshesPerSlot = -1;
            var bonesPerSlot = -1;
            for (var index = 0; index < SlotCount; index++)
            {
                var slot = root.transform.GetChild(index);
                var expectedName = SlotName(index);
                if (slot.name != expectedName ||
                    Vector3.Distance(
                        slot.localPosition,
                        new Vector3(index * ExpectedSlotSpacing, 0f, 0f)) >
                    PositionTolerance ||
                    Quaternion.Angle(
                        slot.localRotation,
                        Quaternion.Euler(0f, 180f, 0f)) > 0.01f ||
                    slot.childCount != 1)
                {
                    throw new InvalidOperationException(
                        expectedName + " placement transform or hierarchy changed.");
                }

                var model = slot.GetChild(0);
                var source =
                    PrefabUtility.GetCorrespondingObjectFromSource(model.gameObject);
                if (model.name != ModelName ||
                    source == null ||
                    AssetDatabase.GetAssetPath(source) != ModelPath)
                {
                    throw new InvalidOperationException(
                        expectedName + " is not a direct instance of the original Resistance FBX.");
                }

                var slotMetrics = InspectModel(model, material);
                SetOrCompare(
                    ref renderersPerSlot,
                    slotMetrics.RendererCount,
                    "renderer");
                SetOrCompare(
                    ref verticesPerSlot,
                    slotMetrics.VertexCount,
                    "vertex");
                SetOrCompare(
                    ref trianglesPerSlot,
                    slotMetrics.TriangleCount,
                    "triangle");
                SetOrCompare(
                    ref subMeshesPerSlot,
                    slotMetrics.SubMeshCount,
                    "submesh");
                SetOrCompare(
                    ref bonesPerSlot,
                    slotMetrics.BoneCount,
                    "bone");
            }

            if (verticesPerSlot != ExpectedVertexCount ||
                trianglesPerSlot != ExpectedTriangleCount ||
                bonesPerSlot != ExpectedBoneCount)
            {
                throw new InvalidOperationException(
                    "Resistance imported mesh topology or rig count differs from the approved unchanged source contract. " +
                    $"Actual vertices={verticesPerSlot}, triangles={trianglesPerSlot}, bones={bonesPerSlot}; " +
                    $"expected vertices={ExpectedVertexCount}, triangles={ExpectedTriangleCount}, bones={ExpectedBoneCount}.");
            }

            var player = RequireRoot(scene, "Player");
            var camera = player.GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("Player camera is missing.");
            if (Vector3.Distance(
                    player.transform.position,
                    ExpectedPlayerPosition) > PositionTolerance ||
                Vector3.Distance(
                    camera.transform.position,
                    ExpectedCameraPosition) > PositionTolerance ||
                Vector3.Dot(
                    camera.transform.forward.normalized,
                    Vector3.forward) < 0.9999f)
            {
                throw new InvalidOperationException(
                    "Player or camera start state changed while applying Resistance appearance.");
            }

            return new Inspection
            {
                SlotCount = SlotCount,
                RenderersPerSlot = renderersPerSlot,
                VertexCountPerSlot = verticesPerSlot,
                TriangleCountPerSlot = trianglesPerSlot,
                SubMeshCountPerSlot = subMeshesPerSlot,
                BoneCountPerSlot = bonesPerSlot,
                RootPosition = root.transform.position,
                SlotSpacing = ExpectedSlotSpacing,
                PlayerPosition = player.transform.position,
                CameraPosition = camera.transform.position,
                CameraForward = camera.transform.forward
            };
        }

        private static ModelMetrics InspectModel(
            Transform model,
            Material approvedMaterial)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled && item.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    model.name + " has no visible renderer.");
            }

            var vertices = 0;
            var triangles = 0;
            var subMeshes = 0;
            var bones = 0;
            foreach (var renderer in renderers)
            {
                var mesh = MeshOf(renderer);
                if (mesh == null)
                {
                    throw new InvalidOperationException(
                        renderer.name + " has no source mesh.");
                }

                vertices += mesh.vertexCount;
                subMeshes += mesh.subMeshCount;
                for (var index = 0; index < mesh.subMeshCount; index++)
                {
                    triangles +=
                        checked((int)mesh.GetIndexCount(index)) / 3;
                }

                if (renderer is SkinnedMeshRenderer skinned)
                {
                    bones += skinned.bones.Length;
                }

                var materials = renderer.sharedMaterials;
                if (materials.Length != mesh.subMeshCount ||
                    materials.Any(item => item != approvedMaterial))
                {
                    throw new InvalidOperationException(
                        renderer.name + " does not use the approved Resistance material for every original submesh.");
                }
            }

            return new ModelMetrics(
                renderers.Length,
                vertices,
                triangles,
                subMeshes,
                bones);
        }

        private static void ApplyMaterial(
            Transform placementRoot,
            Material material)
        {
            for (var slotIndex = 0; slotIndex < SlotCount; slotIndex++)
            {
                var slot = placementRoot.GetChild(slotIndex);
                var model = slot.GetChild(0);
                foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    var mesh = MeshOf(renderer) ??
                               throw new InvalidOperationException(
                                   renderer.name + " has no source mesh.");
                    renderer.sharedMaterials = Enumerable
                        .Repeat(material, mesh.subMeshCount)
                        .ToArray();
                    EditorUtility.SetDirty(renderer);
                }
            }
        }

        private static Material EnsureApprovedMaterial(
            Texture2D faceMask,
            TriangleLookup triangleLookup)
        {
            EnsureFolder(MaterialRoot);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath) ??
                         Shader.Find(ShaderName) ??
                         throw new InvalidOperationException(
                             "The approved Resistance shader is missing.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "M_Resistance_ApprovedAppearance"
                };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            foreach (var contract in TextureContracts)
            {
                var texture =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(contract.AssetPath) ??
                    throw new InvalidOperationException(
                        "Approved Resistance texture is missing: " +
                        contract.AssetPath);
                material.SetTexture(contract.PropertyName, texture);
            }

            material.SetTexture("_FaceMaterialMask", faceMask);
            material.SetTexture(
                "_TriangleMapA",
                triangleLookup.MapA);
            material.SetTexture(
                "_TriangleMapB",
                triangleLookup.MapB);
            material.SetTexture(
                "_TriangleMapC",
                triangleLookup.MapC);
            material.SetTexture(
                "_TriangleMaterialMap",
                triangleLookup.MaterialMap);
            material.SetTexture(
                "_TrianglePanelAtlas",
                triangleLookup.PanelAtlas);
            material.SetFloat("_EmissionStrength", 1.0f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static Material RequireApprovedMaterial()
        {
            return AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) ??
                   throw new InvalidOperationException(
                       "The approved Resistance material asset is missing.");
        }

        private static Texture2D EnsureApprovedUvFaceMask()
        {
            var sourcePath = Absolute(ApprovedFaceMaskSourcePath);
            var assetPath = Absolute(FaceMaskPath);
            if (!File.Exists(sourcePath) ||
                Sha256(sourcePath) != FaceMaskSha256)
            {
                throw new InvalidOperationException(
                    "The UV-baked approved Resistance face-material mask is missing or changed.");
            }

            if (!File.Exists(assetPath) ||
                Sha256(assetPath) != FaceMaskSha256)
            {
                Directory.CreateDirectory(
                    Path.GetDirectoryName(assetPath) ??
                    throw new InvalidOperationException(
                        "Resistance texture directory could not be resolved."));
                File.Copy(sourcePath, assetPath, true);
            }

            AssetDatabase.ImportAsset(
                FaceMaskPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            var importer =
                AssetImporter.GetAtPath(FaceMaskPath) as TextureImporter ??
                throw new InvalidOperationException(
                    "Resistance UV face-material mask importer is missing.");
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.textureCompression =
                TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return RequireApprovedFaceMask();
        }

        private static TriangleLookup EnsureApprovedTriangleLookup(
            Transform placementRoot)
        {
            var renderer = placementRoot
                .GetChild(0)
                .GetChild(0)
                .GetComponentsInChildren<Renderer>(true)
                .Single();
            var mesh = MeshOf(renderer) ??
                       throw new InvalidOperationException(
                           "Resistance Unity mesh is missing while creating the approved panel atlas.");
            var indices = mesh.GetIndices(0);
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var uvs = mesh.uv;
            if (indices.Length != ExpectedTriangleCount * 3 ||
                normals.Length != vertices.Length ||
                uvs.Length != vertices.Length)
            {
                throw new InvalidOperationException(
                    "Resistance Unity source data differs while creating the approved panel atlas.");
            }

            var mapAPixels = new Color[ExpectedTriangleCount];
            var mapBPixels = new Color[ExpectedTriangleCount];
            var mapCPixels = new Color[ExpectedTriangleCount];
            var materialMap = CreateApprovedTriangleMaterialMap(
                renderer,
                mesh);
            const int atlasSize = 2048;
            var panelPixels = new Color32[atlasSize * atlasSize];
            for (var triangle = 0;
                 triangle < ExpectedTriangleCount;
                 triangle++)
            {
                var offset = triangle * 3;
                var uv0 = uvs[indices[offset]];
                var uv1 = uvs[indices[offset + 1]];
                var uv2 = uvs[indices[offset + 2]];
                var atlasCorners = AtlasCorners(triangle);
                mapAPixels[triangle] =
                    new Color(uv0.x, uv0.y, uv1.x, uv1.y);
                mapBPixels[triangle] =
                    new Color(
                        uv2.x,
                        uv2.y,
                        atlasCorners[0].x,
                        atlasCorners[0].y);
                mapCPixels[triangle] =
                    new Color(
                        atlasCorners[1].x,
                        atlasCorners[1].y,
                        atlasCorners[2].x,
                        atlasCorners[2].y);

                RasterizePanelTriangle(
                    panelPixels,
                    atlasSize,
                    atlasCorners,
                    new[]
                    {
                        vertices[indices[offset]],
                        vertices[indices[offset + 1]],
                        vertices[indices[offset + 2]]
                    },
                    new[]
                    {
                        normals[indices[offset]],
                        normals[indices[offset + 1]],
                        normals[indices[offset + 2]]
                    });
            }

            var panelTexture = new Texture2D(
                atlasSize,
                atlasSize,
                TextureFormat.RGBA32,
                false,
                true);
            panelTexture.SetPixels32(panelPixels);
            panelTexture.Apply(false, false);
            File.WriteAllBytes(
                Absolute(TrianglePanelAtlasPath),
                panelTexture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(panelTexture);
            AssetDatabase.ImportAsset(
                TrianglePanelAtlasPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            var panelImporter =
                AssetImporter.GetAtPath(
                    TrianglePanelAtlasPath) as TextureImporter ??
                throw new InvalidOperationException(
                    "Resistance approved panel atlas importer is missing.");
            panelImporter.sRGBTexture = false;
            panelImporter.mipmapEnabled = false;
            panelImporter.wrapMode = TextureWrapMode.Clamp;
            panelImporter.filterMode = FilterMode.Bilinear;
            panelImporter.maxTextureSize = atlasSize;
            panelImporter.textureCompression =
                TextureImporterCompression.Uncompressed;
            panelImporter.SaveAndReimport();

            var mapA = CreateLookupTexture(
                TriangleMapAPath,
                mapAPixels);
            var mapB = CreateLookupTexture(
                TriangleMapBPath,
                mapBPixels);
            var mapC = CreateLookupTexture(
                TriangleMapCPath,
                mapCPixels);
            var panelAtlas =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    TrianglePanelAtlasPath) ??
                throw new InvalidOperationException(
                    "Resistance approved panel atlas could not be loaded.");
            return new TriangleLookup(
                mapA,
                mapB,
                mapC,
                materialMap,
                panelAtlas);
        }

        private static void RasterizePanelTriangle(
            Color32[] pixels,
            int atlasSize,
            IReadOnlyList<Vector2> atlasCorners,
            IReadOnlyList<Vector3> positions,
            IReadOnlyList<Vector3> normals)
        {
            var minimumX = Mathf.Max(
                0,
                Mathf.FloorToInt(
                    atlasCorners.Min(value => value.x) *
                    atlasSize));
            var maximumX = Mathf.Min(
                atlasSize - 1,
                Mathf.CeilToInt(
                    atlasCorners.Max(value => value.x) *
                    atlasSize));
            var minimumY = Mathf.Max(
                0,
                Mathf.FloorToInt(
                    atlasCorners.Min(value => value.y) *
                    atlasSize));
            var maximumY = Mathf.Min(
                atlasSize - 1,
                Mathf.CeilToInt(
                    atlasCorners.Max(value => value.y) *
                    atlasSize));
            for (var y = minimumY; y <= maximumY; y++)
            {
                for (var x = minimumX; x <= maximumX; x++)
                {
                    var atlasUv = new Vector2(
                        (x + 0.5f) / atlasSize,
                        (y + 0.5f) / atlasSize);
                    var barycentric = Barycentric2D(
                        atlasUv,
                        atlasCorners[0],
                        atlasCorners[1],
                        atlasCorners[2]);
                    if (barycentric.x < -0.001f ||
                        barycentric.y < -0.001f ||
                        barycentric.z < -0.001f)
                    {
                        continue;
                    }

                    var position =
                        positions[0] * barycentric.x +
                        positions[1] * barycentric.y +
                        positions[2] * barycentric.z;
                    var normal = (
                        normals[0] * barycentric.x +
                        normals[1] * barycentric.y +
                        normals[2] * barycentric.z).normalized;
                    var masks = EvaluateApprovedPanelMasks(
                        position,
                        normal);
                    pixels[x + y * atlasSize] = new Color(
                        masks.x,
                        masks.y,
                        0f,
                        1f);
                }
            }
        }

        private static Vector3 Barycentric2D(
            Vector2 point,
            Vector2 first,
            Vector2 second,
            Vector2 third)
        {
            var edge0 = second - first;
            var edge1 = third - first;
            var relative = point - first;
            var denominator =
                edge0.x * edge1.y -
                edge1.x * edge0.y;
            if (Mathf.Abs(denominator) < 0.0000001f)
            {
                return new Vector3(-1f, -1f, -1f);
            }

            var secondWeight =
                (relative.x * edge1.y -
                 edge1.x * relative.y) /
                denominator;
            var thirdWeight =
                (edge0.x * relative.y -
                 relative.x * edge0.y) /
                denominator;
            return new Vector3(
                1f - secondWeight - thirdWeight,
                secondWeight,
                thirdWeight);
        }

        private static Vector2 EvaluateApprovedPanelMasks(
            Vector3 position,
            Vector3 normal)
        {
            var horizontal = Mathf.Abs(position.x);
            var vertical = position.y;
            var blenderDepth = -position.z;
            var front = normal.z > 0.16f ? 1f : 0f;
            var panelFacing = normal.z > -0.10f ? 1f : 0f;
            var frame = 0f;
            var core = 0f;

            AddPanel(
                ref frame,
                ref core,
                Ellipse(position.x, vertical, -0.235f, 1.425f, 0.042f, 0.040f),
                Ellipse(position.x, vertical, -0.235f, 1.425f, 0.024f, 0.022f),
                panelFacing);
            AddPanel(
                ref frame,
                ref core,
                Rectangle(horizontal, vertical, 0.115f, 1.292f, 0.040f, 0.020f),
                Rectangle(horizontal, vertical, 0.115f, 1.292f, 0.030f, 0.010f),
                front);
            foreach (var level in new[]
                     {
                         1.205f,
                         1.170f,
                         1.135f,
                         1.100f
                     })
            {
                AddPanel(
                    ref frame,
                    ref core,
                    Rectangle(position.x, vertical, 0f, level, 0.031f, 0.012f),
                    Rectangle(position.x, vertical, 0f, level, 0.021f, 0.005f),
                    front);
            }

            var forearmFrame =
                Rectangle(horizontal, vertical, 0.325f, 1.070f, 0.075f, 0.080f) *
                Range(blenderDepth, -0.18f, 0.02f);
            var forearmCore =
                Rectangle(horizontal, vertical, 0.325f, 1.070f, 0.065f, 0.055f) *
                Range(blenderDepth, -0.18f, -0.08f);
            var nearSide = Range(position.x, -0.40f, -0.25f);
            forearmFrame = Mathf.Max(
                forearmFrame,
                nearSide *
                Range(vertical, 0.99f, 1.15f) *
                Range(blenderDepth, 0.02f, 0.16f));
            forearmCore = Mathf.Max(
                forearmCore,
                nearSide *
                Range(vertical, 1.015f, 1.125f) *
                Range(blenderDepth, 0.08f, 0.16f));
            AddPanel(
                ref frame,
                ref core,
                forearmFrame,
                forearmCore,
                1f);

            var thighFrame = SlopedRectangle(
                horizontal,
                vertical,
                0.170f,
                0.765f,
                0.034f,
                0.092f,
                0.10f);
            var thighCore = 0f;
            foreach (var level in new[]
                     {
                         0.720f,
                         0.765f,
                         0.810f
                     })
            {
                thighCore = Mathf.Max(
                    thighCore,
                    SlopedRectangle(
                        horizontal,
                        vertical,
                        0.170f,
                        level,
                        0.019f,
                        0.006f,
                        0.10f));
            }

            AddPanel(
                ref frame,
                ref core,
                thighFrame,
                thighCore,
                front);
            AddPanel(
                ref frame,
                ref core,
                Rectangle(horizontal, vertical, 0.205f, 0.410f, 0.026f, 0.105f),
                Rectangle(horizontal, vertical, 0.205f, 0.410f, 0.011f, 0.078f),
                front);
            return new Vector2(frame, core);
        }

        private static void AddPanel(
            ref float frame,
            ref float core,
            float panelFrame,
            float panelCore,
            float visibility)
        {
            frame = Mathf.Max(
                frame,
                panelFrame * visibility);
            core = Mathf.Max(
                core,
                panelCore * visibility);
        }

        private static float Range(
            float value,
            float minimum,
            float maximum)
        {
            return value >= minimum && value <= maximum
                ? 1f
                : 0f;
        }

        private static float Rectangle(
            float x,
            float y,
            float centerX,
            float centerY,
            float halfWidth,
            float halfHeight)
        {
            return Range(
                       x,
                       centerX - halfWidth,
                       centerX + halfWidth) *
                   Range(
                       y,
                       centerY - halfHeight,
                       centerY + halfHeight);
        }

        private static float SlopedRectangle(
            float x,
            float y,
            float centerX,
            float centerY,
            float halfWidth,
            float halfHeight,
            float slope)
        {
            return Rectangle(
                x + (y - centerY) * slope,
                y,
                centerX,
                centerY,
                halfWidth,
                halfHeight);
        }

        private static float Ellipse(
            float x,
            float y,
            float centerX,
            float centerY,
            float radiusX,
            float radiusY)
        {
            var normalized = new Vector2(
                (x - centerX) / radiusX,
                (y - centerY) / radiusY);
            return Vector2.Dot(normalized, normalized) <= 1f
                ? 1f
                : 0f;
        }

        private static TriangleLookup EnsureApprovedTriangleLookupFromBlender(
            Transform placementRoot)
        {
            var faceMapPath = Absolute(FaceMapJsonPath);
            if (!File.Exists(faceMapPath))
            {
                throw new InvalidOperationException(
                    "The approved Blender face map is missing.");
            }

            var faceMap = JsonUtility.FromJson<ApprovedFaceMap>(
                File.ReadAllText(faceMapPath, Encoding.UTF8));
            if (faceMap == null ||
                faceMap.faces == null ||
                faceMap.faces.Length != ExpectedTriangleCount)
            {
                throw new InvalidOperationException(
                    "The approved Blender face map has an invalid triangle count.");
            }

            var renderer = placementRoot
                .GetChild(0)
                .GetChild(0)
                .GetComponentsInChildren<Renderer>(true)
                .Single();
            var mesh = MeshOf(renderer) ??
                       throw new InvalidOperationException(
                           "Resistance Unity mesh is missing while creating the triangle atlas lookup.");
            var indices = mesh.GetIndices(0);
            var vertices = mesh.vertices;
            var uvs = mesh.uv;
            if (indices.Length != ExpectedTriangleCount * 3 ||
                uvs.Length != vertices.Length)
            {
                throw new InvalidOperationException(
                    "Resistance Unity triangle or UV data differs from the unchanged source.");
            }

            var sourceByTriangle =
                new Dictionary<string, List<ApprovedFace>>(
                    StringComparer.Ordinal);
            var sourceByVertex =
                new Dictionary<string, List<ApprovedFace>>(
                    StringComparer.Ordinal);
            foreach (var face in faceMap.faces)
            {
                if (face.world_vertices == null ||
                    face.world_vertices.Length != 3)
                {
                    throw new InvalidOperationException(
                        "The approved Blender face map contains a non-triangle.");
                }

                var sourceVertices = SourceUnityVertices(face);
                var triangleKey = TriangleKey(
                    sourceVertices[0],
                    sourceVertices[1],
                    sourceVertices[2]);
                if (!sourceByTriangle.TryGetValue(
                        triangleKey,
                        out var triangleFaces))
                {
                    triangleFaces = new List<ApprovedFace>();
                    sourceByTriangle.Add(
                        triangleKey,
                        triangleFaces);
                }

                triangleFaces.Add(face);
                foreach (var sourceVertex in sourceVertices)
                {
                    var vertexKey = PositionKey(sourceVertex);
                    if (!sourceByVertex.TryGetValue(
                            vertexKey,
                            out var vertexFaces))
                    {
                        vertexFaces = new List<ApprovedFace>();
                        sourceByVertex.Add(
                            vertexKey,
                            vertexFaces);
                    }

                    vertexFaces.Add(face);
                }
            }

            var mapAPixels = new Color[ExpectedTriangleCount];
            var mapBPixels = new Color[ExpectedTriangleCount];
            var mapCPixels = new Color[ExpectedTriangleCount];
            var materialMap = CreateApprovedTriangleMaterialMap(
                renderer,
                mesh);
            var exactMatches = 0;
            var diagonalMatches = 0;
            var maximumFallbackDistance = 0f;
            var fallbackDistanceSum = 0f;
            for (var triangle = 0;
                 triangle < ExpectedTriangleCount;
                 triangle++)
            {
                var indexOffset = triangle * 3;
                var triangleVertices = new[]
                {
                    vertices[indices[indexOffset]],
                    vertices[indices[indexOffset + 1]],
                    vertices[indices[indexOffset + 2]]
                };
                var key = TriangleKey(
                    triangleVertices[0],
                    triangleVertices[1],
                    triangleVertices[2]);
                ApprovedFace sourceFace;
                if (sourceByTriangle.TryGetValue(
                        key,
                        out var exactFaces) &&
                    exactFaces.Count > 0)
                {
                    sourceFace = exactFaces[0];
                    exactMatches++;
                }
                else
                {
                    sourceFace = FindDiagonalSourceFace(
                        triangleVertices,
                        sourceByVertex,
                        faceMap.faces,
                        triangle,
                        out var fallbackDistance);
                    maximumFallbackDistance = Mathf.Max(
                        maximumFallbackDistance,
                        fallbackDistance);
                    fallbackDistanceSum += fallbackDistance;
                    diagonalMatches++;
                }

                var sourceTriangle = SourceUnityVertices(
                    sourceFace);
                var atlasCorners = AtlasCorners(
                    sourceFace.polygon_index);
                var destinationUvs = triangleVertices
                    .Select(vertex =>
                    {
                        var barycentric = Barycentric(
                            vertex,
                            sourceTriangle[0],
                            sourceTriangle[1],
                            sourceTriangle[2]);
                        barycentric = ClampBarycentric(
                            barycentric);
                        return atlasCorners[0] * barycentric.x +
                               atlasCorners[1] * barycentric.y +
                               atlasCorners[2] * barycentric.z;
                    })
                    .ToArray();
                var uv0 = uvs[indices[indexOffset]];
                var uv1 = uvs[indices[indexOffset + 1]];
                var uv2 = uvs[indices[indexOffset + 2]];
                mapAPixels[triangle] =
                    new Color(uv0.x, uv0.y, uv1.x, uv1.y);
                mapBPixels[triangle] =
                    new Color(
                        uv2.x,
                        uv2.y,
                        destinationUvs[0].x,
                        destinationUvs[0].y);
                mapCPixels[triangle] =
                    new Color(
                        destinationUvs[1].x,
                        destinationUvs[1].y,
                        destinationUvs[2].x,
                        destinationUvs[2].y);
            }

            if (exactMatches + diagonalMatches !=
                ExpectedTriangleCount)
            {
                throw new InvalidOperationException(
                    "Not every Unity Resistance triangle mapped to the approved Blender sample.");
            }

            var mapA = CreateLookupTexture(
                TriangleMapAPath,
                mapAPixels);
            var mapB = CreateLookupTexture(
                TriangleMapBPath,
                mapBPixels);
            var mapC = CreateLookupTexture(
                TriangleMapCPath,
                mapCPixels);
            Debug.Log(
                "ApprovedResistanceTriangleLookup Result=PASS, ExactTriangles=" +
                exactMatches + ", RetriangulatedTriangles=" +
                diagonalMatches + ", TotalTriangles=" +
                ExpectedTriangleCount + ", MaximumFallbackDistance=" +
                Format(maximumFallbackDistance) +
                ", AverageFallbackDistance=" +
                Format(
                    diagonalMatches > 0
                        ? fallbackDistanceSum / diagonalMatches
                        : 0f) +
                ".");
            var panelAtlas =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    TrianglePanelAtlasPath) ??
                throw new InvalidOperationException(
                    "Resistance approved panel atlas is missing.");
            return new TriangleLookup(
                mapA,
                mapB,
                mapC,
                materialMap,
                panelAtlas);
        }

        private static ApprovedFace FindDiagonalSourceFace(
            IReadOnlyList<Vector3> triangleVertices,
            IReadOnlyDictionary<string, List<ApprovedFace>>
                sourceByVertex,
            IReadOnlyList<ApprovedFace> approvedFaces,
            int triangleIndex,
            out float matchDistance)
        {
            var sharedCounts = new Dictionary<ApprovedFace, int>();
            foreach (var vertex in triangleVertices)
            {
                if (!sourceByVertex.TryGetValue(
                        PositionKey(vertex),
                        out var vertexFaces))
                {
                    continue;
                }

                foreach (var face in vertexFaces)
                {
                    sharedCounts.TryGetValue(
                        face,
                        out var count);
                    sharedCounts[face] = count + 1;
                }
            }

            var candidates = sharedCounts
                .Where(item => item.Value >= 2)
                .Select(item => item.Key)
                .ToArray();
            if (candidates.Length == 0)
            {
                var triangleCentroid =
                    (triangleVertices[0] +
                     triangleVertices[1] +
                     triangleVertices[2]) / 3f;
                var triangleNormal = Vector3.Cross(
                        triangleVertices[1] - triangleVertices[0],
                        triangleVertices[2] - triangleVertices[0])
                    .normalized;
                var nearest = approvedFaces
                    .Select(face =>
                    {
                        var source = SourceUnityVertices(face);
                        var sourceNormal = Vector3.Cross(
                                source[1] - source[0],
                                source[2] - source[0])
                            .normalized;
                        var distance = Vector3.Distance(
                            ClosestPointOnTriangle(
                                triangleCentroid,
                                source[0],
                                source[1],
                                source[2]),
                            triangleCentroid);
                        var normalPenalty =
                            1f - Mathf.Abs(
                                Vector3.Dot(
                                    sourceNormal,
                                    triangleNormal));
                        return new
                        {
                            Face = face,
                            Distance = distance,
                            Score =
                                distance +
                                normalPenalty * 0.01f
                        };
                    })
                    .OrderBy(item => item.Score)
                    .ThenBy(item => item.Face.polygon_index)
                    .First();
                matchDistance = nearest.Distance;
                return nearest.Face;
            }

            var centroid =
                (triangleVertices[0] +
                 triangleVertices[1] +
                 triangleVertices[2]) / 3f;
            var nearestCandidate = candidates
                .Select(face =>
                {
                    var source = SourceUnityVertices(face);
                    return new
                    {
                        Face = face,
                        Distance = Vector3.Distance(
                            ClosestPointOnTriangle(
                                centroid,
                                source[0],
                                source[1],
                                source[2]),
                            centroid)
                    };
                })
                .OrderBy(item => item.Distance)
                .ThenBy(item => item.Face.polygon_index)
                .First();
            matchDistance = nearestCandidate.Distance;
            return nearestCandidate.Face;
        }

        private static Vector3 ClosestPointOnTriangle(
            Vector3 point,
            Vector3 first,
            Vector3 second,
            Vector3 third)
        {
            var firstToSecond = second - first;
            var firstToThird = third - first;
            var firstToPoint = point - first;
            var d1 = Vector3.Dot(firstToSecond, firstToPoint);
            var d2 = Vector3.Dot(firstToThird, firstToPoint);
            if (d1 <= 0f && d2 <= 0f)
            {
                return first;
            }

            var secondToPoint = point - second;
            var d3 = Vector3.Dot(firstToSecond, secondToPoint);
            var d4 = Vector3.Dot(firstToThird, secondToPoint);
            if (d3 >= 0f && d4 <= d3)
            {
                return second;
            }

            var edgeRegion = d1 * d4 - d3 * d2;
            if (edgeRegion <= 0f && d1 >= 0f && d3 <= 0f)
            {
                var weight = d1 / (d1 - d3);
                return first + weight * firstToSecond;
            }

            var thirdToPoint = point - third;
            var d5 = Vector3.Dot(firstToSecond, thirdToPoint);
            var d6 = Vector3.Dot(firstToThird, thirdToPoint);
            if (d6 >= 0f && d5 <= d6)
            {
                return third;
            }

            var secondEdgeRegion = d5 * d2 - d1 * d6;
            if (secondEdgeRegion <= 0f &&
                d2 >= 0f &&
                d6 <= 0f)
            {
                var weight = d2 / (d2 - d6);
                return first + weight * firstToThird;
            }

            var thirdEdgeRegion =
                d3 * d6 - d5 * d4;
            if (thirdEdgeRegion <= 0f &&
                d4 - d3 >= 0f &&
                d5 - d6 >= 0f)
            {
                var weight =
                    (d4 - d3) /
                    ((d4 - d3) + (d5 - d6));
                return second +
                       weight * (third - second);
            }

            var denominator =
                1f /
                (thirdEdgeRegion +
                 secondEdgeRegion +
                 edgeRegion);
            var secondWeight =
                secondEdgeRegion * denominator;
            var thirdWeight =
                edgeRegion * denominator;
            return first +
                   firstToSecond * secondWeight +
                   firstToThird * thirdWeight;
        }

        private static Vector3[] SourceUnityVertices(
            ApprovedFace face)
        {
            return face.world_vertices
                .Select(item =>
                    new Vector3(item.x, item.z, -item.y))
                .ToArray();
        }

        private static Vector2[] AtlasCorners(int faceIndex)
        {
            const int grid = 78;
            const float margin = 3f / 2048f;
            var tile = 1f / grid;
            var column = faceIndex % grid;
            var row = faceIndex / grid;
            var minimum = new Vector2(
                column * tile + margin,
                row * tile + margin);
            var maximum = new Vector2(
                (column + 1) * tile - margin,
                (row + 1) * tile - margin);
            return new[]
            {
                minimum,
                new Vector2(maximum.x, minimum.y),
                new Vector2(minimum.x, maximum.y)
            };
        }

        private static Vector3 Barycentric(
            Vector3 point,
            Vector3 first,
            Vector3 second,
            Vector3 third)
        {
            var edge0 = second - first;
            var edge1 = third - first;
            var relative = point - first;
            var dot00 = Vector3.Dot(edge0, edge0);
            var dot01 = Vector3.Dot(edge0, edge1);
            var dot11 = Vector3.Dot(edge1, edge1);
            var dot20 = Vector3.Dot(relative, edge0);
            var dot21 = Vector3.Dot(relative, edge1);
            var denominator = dot00 * dot11 - dot01 * dot01;
            if (Mathf.Abs(denominator) < 0.000000001f)
            {
                throw new InvalidOperationException(
                    "The approved Blender sample contains a degenerate triangle.");
            }

            var secondWeight =
                (dot11 * dot20 - dot01 * dot21) /
                denominator;
            var thirdWeight =
                (dot00 * dot21 - dot01 * dot20) /
                denominator;
            return new Vector3(
                1f - secondWeight - thirdWeight,
                secondWeight,
                thirdWeight);
        }

        private static Vector3 ClampBarycentric(Vector3 value)
        {
            value = new Vector3(
                Mathf.Clamp01(value.x),
                Mathf.Clamp01(value.y),
                Mathf.Clamp01(value.z));
            var total = value.x + value.y + value.z;
            return total > 0.000001f
                ? value / total
                : new Vector3(1f, 0f, 0f);
        }

        // Mirrors build_resistance_sample.py classify_polygon directly on the
        // unchanged Unity import, avoiding any approximate Blender-face match.
        private static Texture2D CreateApprovedTriangleMaterialMap(
            Renderer renderer,
            Mesh mesh)
        {
            if (!(renderer is SkinnedMeshRenderer skinnedRenderer))
            {
                throw new InvalidOperationException(
                    "Resistance approved face classification requires its original skinned renderer.");
            }

            var indices = mesh.GetIndices(0);
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var boneWeights = mesh.boneWeights;
            if (indices.Length != ExpectedTriangleCount * 3 ||
                normals.Length != vertices.Length ||
                boneWeights.Length != vertices.Length)
            {
                throw new InvalidOperationException(
                    "Resistance source geometry or skin weights differ while reproducing the approved material zones.");
            }

            var dominantBones = new string[vertices.Length];
            for (var vertexIndex = 0;
                 vertexIndex < vertices.Length;
                 vertexIndex++)
            {
                var weight = boneWeights[vertexIndex];
                var dominantIndex = weight.boneIndex0;
                var dominantWeight = weight.weight0;
                if (weight.weight1 > dominantWeight)
                {
                    dominantIndex = weight.boneIndex1;
                    dominantWeight = weight.weight1;
                }

                if (weight.weight2 > dominantWeight)
                {
                    dominantIndex = weight.boneIndex2;
                    dominantWeight = weight.weight2;
                }

                if (weight.weight3 > dominantWeight)
                {
                    dominantIndex = weight.boneIndex3;
                    dominantWeight = weight.weight3;
                }

                dominantBones[vertexIndex] =
                    dominantWeight > 0f &&
                    dominantIndex >= 0 &&
                    dominantIndex < skinnedRenderer.bones.Length &&
                    skinnedRenderer.bones[dominantIndex] != null
                        ? skinnedRenderer.bones[dominantIndex].name
                        : string.Empty;
            }

            var pixels = new Color[ExpectedTriangleCount];
            var counts = new int[4];
            for (var triangleIndex = 0;
                 triangleIndex < ExpectedTriangleCount;
                 triangleIndex++)
            {
                var offset = triangleIndex * 3;
                var vertexIndices = new[]
                {
                    indices[offset],
                    indices[offset + 1],
                    indices[offset + 2]
                };
                var first = vertices[vertexIndices[0]];
                var second = vertices[vertexIndices[1]];
                var third = vertices[vertexIndices[2]];
                var unityNormal = Vector3.Cross(
                    second - first,
                    third - first).normalized;
                var averagedNormal = (
                    normals[vertexIndices[0]] +
                    normals[vertexIndices[1]] +
                    normals[vertexIndices[2]]).normalized;
                if (Vector3.Dot(unityNormal, averagedNormal) < 0f)
                {
                    unityNormal = -unityNormal;
                }

                var materialIndex = ClassifyApprovedTriangle(
                    (first + second + third) / 3f,
                    unityNormal,
                    Vector3.Cross(
                        second - first,
                        third - first).magnitude * 0.5f,
                    DominantTriangleBone(
                        vertexIndices,
                        dominantBones));
                counts[materialIndex]++;
                pixels[triangleIndex] = new Color(
                    materialIndex / 4f,
                    0f,
                    0f,
                    1f);
            }

            var texture = CreateLookupTexture(
                TriangleMaterialMapPath,
                pixels);
            Debug.Log(
                "ApprovedResistanceMaterialClassification Result=PASS, " +
                "Silver=" + counts[0] +
                ", Dark=" + counts[1] +
                ", Bronze=" + counts[2] +
                ", Olive=" + counts[3] +
                ", Total=" + counts.Sum() + ".");
            return texture;
        }

        private static int ClassifyApprovedTriangle(
            Vector3 unityCenter,
            Vector3 unityNormal,
            float worldArea,
            string bone)
        {
            var x = unityCenter.x;
            var y = -unityCenter.z;
            var z = unityCenter.y;
            var normalX = unityNormal.x;
            var normalY = -unityNormal.z;
            var frontness = -normalY;
            var horizontal = Mathf.Abs(x);

            var isBandana =
                (z >= 1.695f && z <= 1.735f) ||
                (z > 1.54f && y > 0.085f && horizontal > 0.095f);
            if ((bone == "Head" || bone == "neck") && isBandana)
            {
                return 3;
            }

            var side = x >= 0f ? 1f : -1f;
            var isTorsoRecess = frontness > 0.30f &&
                ((z >= 1.03f && z <= 1.23f && horizontal <= 0.13f) ||
                 (z >= 1.24f && z <= 1.45f && horizontal <= 0.032f) ||
                 (z >= 1.205f && z <= 1.245f && horizontal <= 0.23f));
            var isLimbBone =
                bone == "LeftArm" ||
                bone == "RightArm" ||
                bone == "LeftForeArm" ||
                bone == "RightForeArm" ||
                bone == "LeftUpLeg" ||
                bone == "RightUpLeg" ||
                bone == "LeftLeg" ||
                bone == "RightLeg";
            var isInnerLimb =
                isLimbBone &&
                normalX * side < -0.68f &&
                frontness < 0.28f;
            var isJointBand =
                (z >= 0.55f && z <= 0.61f &&
                 horizontal >= 0.15f && horizontal <= 0.29f) ||
                (z >= 0.91f && z <= 0.97f && horizontal <= 0.25f) ||
                (z >= 1.13f && z <= 1.19f && horizontal >= 0.27f) ||
                (z >= 0.15f && z <= 0.20f &&
                 horizontal >= 0.18f && horizontal <= 0.31f) ||
                (z >= 0.96f && z <= 1.00f && horizontal >= 0.31f) ||
                (z >= 1.45f && z <= 1.56f && horizontal <= 0.13f);
            var isDarkBone =
                bone == "neck" ||
                bone == "LeftHand" ||
                bone == "RightHand" ||
                bone == "LeftFoot" ||
                bone == "RightFoot" ||
                bone == "LeftToeBase" ||
                bone == "RightToeBase";
            var isShoulderBone =
                bone == "LeftShoulder" ||
                bone == "RightShoulder" ||
                bone == "LeftArm" ||
                bone == "RightArm";
            var isBronzeBand = worldArea <= 0.00033f &&
                ((isShoulderBone &&
                  z >= 1.37f && z <= 1.47f &&
                  horizontal >= 0.22f &&
                  (frontness > 0.18f || Mathf.Abs(normalX) > 0.38f)) ||
                 (z >= 1.13f && z <= 1.20f &&
                  horizontal >= 0.27f && frontness > 0.10f) ||
                 (z >= 0.91f && z <= 0.99f &&
                  horizontal >= 0.12f && horizontal <= 0.28f &&
                  frontness > 0.05f) ||
                 (z >= 0.55f && z <= 0.62f &&
                  horizontal >= 0.15f && horizontal <= 0.29f &&
                  frontness > 0.05f) ||
                 (z >= 0.15f && z <= 0.20f &&
                  horizontal >= 0.18f && horizontal <= 0.31f &&
                  Mathf.Abs(normalX) > 0.30f) ||
                 ((bone == "Spine01" || bone == "Spine") &&
                  z >= 1.41f && z <= 1.46f &&
                  horizontal >= 0.07f && horizontal <= 0.24f &&
                  frontness > 0.28f) ||
                 ((bone == "Hips" || bone == "Spine02") &&
                  z >= 0.94f && z <= 1.02f &&
                  horizontal <= 0.085f &&
                  frontness > 0.20f));
            if (isBronzeBand)
            {
                return 2;
            }

            return isDarkBone ||
                   isTorsoRecess ||
                   isInnerLimb ||
                   isJointBand
                ? 1
                : 0;
        }

        private static string DominantTriangleBone(
            IReadOnlyList<int> vertexIndices,
            IReadOnlyList<string> dominantBones)
        {
            var orderedNames = new List<string>();
            var counts = new Dictionary<string, int>(
                StringComparer.Ordinal);
            foreach (var vertexIndex in vertexIndices)
            {
                var name = dominantBones[vertexIndex];
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                if (!counts.ContainsKey(name))
                {
                    counts.Add(name, 0);
                    orderedNames.Add(name);
                }

                counts[name]++;
            }

            return orderedNames
                .OrderByDescending(name => counts[name])
                .FirstOrDefault() ??
                string.Empty;
        }

        private static Texture2D CreateLookupTexture(
            string path,
            Color[] pixels)
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) !=
                null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            var texture = new Texture2D(
                ExpectedTriangleCount,
                1,
                TextureFormat.RGBAFloat,
                false,
                true)
            {
                name = Path.GetFileNameWithoutExtension(path),
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        private static TriangleLookup RequireApprovedTriangleLookup()
        {
            var mapA =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    TriangleMapAPath) ??
                throw new InvalidOperationException(
                    "Resistance triangle atlas lookup A is missing.");
            var mapB =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    TriangleMapBPath) ??
                throw new InvalidOperationException(
                    "Resistance triangle atlas lookup B is missing.");
            var mapC =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    TriangleMapCPath) ??
                throw new InvalidOperationException(
                    "Resistance triangle atlas lookup C is missing.");
            var materialMap =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    TriangleMaterialMapPath) ??
                throw new InvalidOperationException(
                    "Resistance approved triangle material map is missing.");
            if (mapA.width != ExpectedTriangleCount ||
                mapB.width != ExpectedTriangleCount ||
                mapC.width != ExpectedTriangleCount ||
                materialMap.width != ExpectedTriangleCount ||
                mapA.height != 1 ||
                mapB.height != 1 ||
                mapC.height != 1 ||
                materialMap.height != 1)
            {
                throw new InvalidOperationException(
                    "Resistance triangle atlas lookup dimensions are invalid.");
            }

            var panelAtlas =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    TrianglePanelAtlasPath) ??
                throw new InvalidOperationException(
                    "Resistance approved panel atlas is missing.");
            if (Sha256(Absolute(TrianglePanelAtlasPath)) !=
                TrianglePanelAtlasSha256)
            {
                throw new InvalidOperationException(
                    "Resistance approved panel atlas hash differs.");
            }
            return new TriangleLookup(
                mapA,
                mapB,
                mapC,
                materialMap,
                panelAtlas);
        }

        private static string TriangleKey(
            Vector3 first,
            Vector3 second,
            Vector3 third)
        {
            var vertices = new[]
            {
                PositionKey(first),
                PositionKey(second),
                PositionKey(third)
            };
            Array.Sort(vertices, StringComparer.Ordinal);
            return string.Join("|", vertices);
        }

        private static string PositionKey(Vector3 value)
        {
            const float precision = 10000f;
            return Mathf.RoundToInt(value.x * precision) + "," +
                   Mathf.RoundToInt(value.y * precision) + "," +
                   Mathf.RoundToInt(value.z * precision);
        }

        private static Texture2D RequireApprovedFaceMask()
        {
            var texture =
                AssetDatabase.LoadAssetAtPath<Texture2D>(FaceMaskPath) ??
                throw new InvalidOperationException(
                    "The transferred approved Resistance face-material mask is missing.");
            if (texture.width != 2048 ||
                texture.height != 2048 ||
                Sha256(Absolute(FaceMaskPath)) != FaceMaskSha256)
            {
                throw new InvalidOperationException(
                    "The transferred approved Resistance face-material mask has invalid dimensions.");
            }

            return texture;
        }

        private static void ConfigureTextureImporters()
        {
            foreach (var contract in TextureContracts)
            {
                var importer =
                    AssetImporter.GetAtPath(contract.AssetPath) as TextureImporter ??
                    throw new InvalidOperationException(
                        "Resistance texture importer is missing: " +
                        contract.AssetPath);
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = contract.Srgb;
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Trilinear;
                importer.maxTextureSize = 2048;
                importer.textureCompression =
                    TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureTriangleAtlasImporters()
        {
            foreach (var path in new[]
                     {
                         TextureRoot +
                         "/resistance_approved_triangle_albedo.png",
                         TextureRoot +
                         "/resistance_approved_triangle_emission.png"
                     })
            {
                var importer =
                    AssetImporter.GetAtPath(path) as TextureImporter ??
                    throw new InvalidOperationException(
                        "Resistance triangle atlas importer is missing: " +
                        path);
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 2048;
                importer.textureCompression =
                    TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static void RequireTextureHashes()
        {
            foreach (var contract in TextureContracts)
            {
                var actual = Sha256(Absolute(contract.AssetPath));
                if (!string.Equals(
                        actual,
                        contract.Sha256,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Unity Resistance texture differs from the approved sample: " +
                        contract.AssetPath);
                }
            }
        }

        private static void RequireModelHash()
        {
            var actual = Sha256(Absolute(ModelPath));
            if (!string.Equals(actual, ModelSha256, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The original Resistance FBX hash changed.");
            }
        }

        private static string[] CapturePlacementStructure(Transform root)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .Select(item =>
                    HierarchyPath(item, root) + "|" +
                    Format(item.localPosition) + "|" +
                    Format(item.localRotation) + "|" +
                    Format(item.localScale) + "|" +
                    item.childCount.ToString(CultureInfo.InvariantCulture))
                .ToArray();
        }

        private static string[] CaptureMeshSignatures(Transform root)
        {
            return root.GetComponentsInChildren<Renderer>(true)
                .Select(renderer =>
                {
                    var mesh = MeshOf(renderer) ??
                               throw new InvalidOperationException(
                                   renderer.name + " has no source mesh.");
                    var indices = 0UL;
                    for (var index = 0; index < mesh.subMeshCount; index++)
                    {
                        indices += mesh.GetIndexCount(index);
                    }

                    return HierarchyPath(renderer.transform, root) + "|" +
                           AssetDatabase.GetAssetPath(mesh) + "|" +
                           mesh.vertexCount + "|" +
                           mesh.subMeshCount + "|" +
                           indices;
                })
                .ToArray();
        }

        private static string[] CaptureProtectedRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(item => item.name != PlacementRootName)
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .Select(item =>
                    item.name + "|" +
                    item.activeSelf + "|" +
                    Format(item.transform.position) + "|" +
                    Format(item.transform.rotation) + "|" +
                    Format(item.transform.localScale) + "|" +
                    item.transform.childCount)
                .ToArray();
        }

        private static Mesh MeshOf(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned)
            {
                return skinned.sharedMesh;
            }

            return renderer.GetComponent<MeshFilter>()?.sharedMesh;
        }

        private static Scene RequireCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must already be the active scene. ActiveScene=" +
                    scene.path);
            }

            return scene;
        }

        private static GameObject RequirePlacementRoot(Scene scene)
        {
            return RequireRoot(scene, PlacementRootName);
        }

        private static GameObject RequireRoot(Scene scene, string name)
        {
            return scene.GetRootGameObjects()
                       .SingleOrDefault(item => item.name == name) ??
                   throw new InvalidOperationException(
                       name + " is missing or duplicated.");
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Approved Resistance appearance work requires Unity Edit Mode.");
            }
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }

        private static void SetOrCompare(
            ref int expected,
            int actual,
            string label)
        {
            if (expected < 0)
            {
                expected = actual;
            }
            else if (expected != actual)
            {
                throw new InvalidOperationException(
                    "Resistance " + label + " count differs between slots.");
            }
        }

        private static void RequireSequenceEqual(
            IEnumerable<string> expected,
            IEnumerable<string> actual,
            string message)
        {
            if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string SlotName(int index)
        {
            return "Resistance_" +
                   (index + 1).ToString("00", CultureInfo.InvariantCulture);
        }

        private static string HierarchyPath(Transform item, Transform root)
        {
            var names = new Stack<string>();
            var current = item;
            while (current != null)
            {
                names.Push(current.name);
                if (current == root)
                {
                    break;
                }

                current = current.parent;
            }

            return string.Join("/", names);
        }

        private static void Capture(
            Camera camera,
            string path,
            int width,
            int height)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(path) ??
                throw new InvalidOperationException("Invalid capture path."));
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var target =
                new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var image =
                new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                UnityEngine.Object.DestroyImmediate(image);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void WriteText(string path, string content)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(path) ??
                throw new InvalidOperationException("Invalid report path."));
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream))
                .Replace("-", string.Empty);
        }

        private static string Absolute(string projectRelativePath)
        {
            return Path.GetFullPath(
                Path.Combine(
                    Directory.GetParent(Application.dataPath)?.FullName ??
                    throw new InvalidOperationException(
                        "Unity project root is unavailable."),
                    projectRelativePath));
        }

        private static string Format(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Format(Vector3 value)
        {
            return "(" + Format(value.x) + ", " + Format(value.y) + ", " +
                   Format(value.z) + ")";
        }

        private static string Format(Quaternion value)
        {
            return "(" + Format(value.x) + ", " + Format(value.y) + ", " +
                   Format(value.z) + ", " + Format(value.w) + ")";
        }

        [Serializable]
        private sealed class ApprovedFaceMap
        {
            public ApprovedFace[] faces;
        }

        [Serializable]
        private sealed class ApprovedFace
        {
            public int polygon_index;
            public ApprovedVector[] world_vertices;
        }

        [Serializable]
        private sealed class ApprovedVector
        {
            public float x;
            public float y;
            public float z;
        }

        private readonly struct TriangleLookup
        {
            public TriangleLookup(
                Texture2D mapA,
                Texture2D mapB,
                Texture2D mapC,
                Texture2D materialMap)
                : this(mapA, mapB, mapC, materialMap, null)
            {
            }

            public TriangleLookup(
                Texture2D mapA,
                Texture2D mapB,
                Texture2D mapC,
                Texture2D materialMap,
                Texture2D panelAtlas)
            {
                MapA = mapA;
                MapB = mapB;
                MapC = mapC;
                MaterialMap = materialMap;
                PanelAtlas = panelAtlas;
            }

            public Texture2D MapA { get; }
            public Texture2D MapB { get; }
            public Texture2D MapC { get; }
            public Texture2D MaterialMap { get; }
            public Texture2D PanelAtlas { get; }
        }

        private readonly struct TextureContract
        {
            public TextureContract(
                string propertyName,
                string fileName,
                string sha256,
                bool srgb)
            {
                PropertyName = propertyName;
                AssetPath = TextureRoot + "/" + fileName;
                Sha256 = sha256;
                Srgb = srgb;
            }

            public string PropertyName { get; }
            public string AssetPath { get; }
            public string Sha256 { get; }
            public bool Srgb { get; }
        }

        private readonly struct ModelMetrics
        {
            public ModelMetrics(
                int rendererCount,
                int vertexCount,
                int triangleCount,
                int subMeshCount,
                int boneCount)
            {
                RendererCount = rendererCount;
                VertexCount = vertexCount;
                TriangleCount = triangleCount;
                SubMeshCount = subMeshCount;
                BoneCount = boneCount;
            }

            public int RendererCount { get; }
            public int VertexCount { get; }
            public int TriangleCount { get; }
            public int SubMeshCount { get; }
            public int BoneCount { get; }
        }

        private sealed class Inspection
        {
            public int SlotCount;
            public int RenderersPerSlot;
            public int VertexCountPerSlot;
            public int TriangleCountPerSlot;
            public int SubMeshCountPerSlot;
            public int BoneCountPerSlot;
            public Vector3 RootPosition;
            public float SlotSpacing;
            public Vector3 PlayerPosition;
            public Vector3 CameraPosition;
            public Vector3 CameraForward;
        }
    }
}
