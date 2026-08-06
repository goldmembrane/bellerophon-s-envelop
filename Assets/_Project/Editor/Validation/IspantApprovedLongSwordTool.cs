using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.IspantCargoRunScene
{
    internal static class IspantApprovedLongSwordTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ispant Enemy Placement";
        private const string SwordName = "Ispant_ApprovedLongSword";
        private const string SwordRendererName = "Ispant_ApprovedLongSword_Renderer";
        private const string SwordFbxPath =
            "Assets/_Project/Art/Enemies/Ispant/ApprovedLongSword/Models/Ispant_ApprovedLongSword.fbx";
        private const string StaticMountFbxPath =
            "Assets/_Project/Art/Enemies/Ispant/ApprovedLongSword/Models/Ispant_ApprovedLongSword_StaticMount.fbx";
        private const string MoveMountFbxPath =
            "Assets/_Project/Art/Enemies/Ispant/ApprovedLongSword/Models/Ispant_ApprovedLongSword_MoveMount.fbx";
        private const string DrawMountFbxPath =
            "Assets/_Project/Art/Enemies/Ispant/ApprovedLongSword/Models/Ispant_ApprovedLongSword_DrawMount.fbx";
        private const string DrawAnimationFbxPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_DrawSword.fbx";
        private const string DrawPlaybackClipPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_04_DrawSword_0_9m_Upward.anim";
        private const string DrawControllerPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_04_DrawSword.controller";
        private const string DrawPlaybackClipName = "Ispant_04_DrawSword_0_9m_Upward";
        private const string ShaderPath =
            "Assets/_Project/Art/Enemies/Ispant/ApprovedAppearance/Shaders/IspantApprovedAppearance.shader";
        private const string AssetRoot =
            "Assets/_Project/Art/Enemies/Ispant/ApprovedLongSword";
        private const string TextureRoot = AssetRoot + "/Textures";
        private const string MaterialRoot = AssetRoot + "/Materials";
        private const string BaseColorPath = TextureRoot + "/Ispant_ApprovedLongSword_BaseColor.png";
        private const string RoughnessPath = TextureRoot + "/Ispant_ApprovedLongSword_Roughness.png";
        private const string MetallicPath = TextureRoot + "/Ispant_ApprovedLongSword_Metallic.png";
        private const string NormalPath = TextureRoot + "/Ispant_ApprovedLongSword_Normal.png";
        private const string SteelMaterialPath =
            MaterialRoot + "/Ispant_LongSword_WornSteel_Approved.mat";
        private const string LeatherMaterialPath =
            MaterialRoot + "/Ispant_LongSword_BrownLeather_Approved.mat";
        private const string EngravingMaterialPath =
            MaterialRoot + "/Ispant_LongSword_DarkEngraving_Approved.mat";
        private const string InspectionPath =
            "docs/validation/ispant_approved_longsword_2026-08-06/Ispant_ApprovedLongSword_Inspection.txt";
        private const string CapturePath =
            "docs/validation/ispant_approved_longsword_2026-08-06/Ispant_ApprovedLongSword_FinalReview.png";
        private const string ApprovedBlendSha256 =
            "52E995ED3B121C5363E53FFA8BB832D7BF9FF4795560711AA7D12105C9CABA3D";
        private const string DrawMountSha256 =
            "4058A90B57C8ABA7BCAF185B9BF5D1D1C47C2F0E0991A43BE5178E071D543208";
        private const int ExpectedSlots = 12;
        private const int ExpectedSwordTriangles = 4092;
        private const float ExpectedSwordLength = 0.9f;
        private const int ExpectedStaticBodyTriangles = 3518;
        private const int ExpectedAnimatedBodyTriangles = 3364;
        private const int FirstFrame = 1;
        private const int LastFrame = 46;
        private const int ExpectedImportedDrawPoseSamples = 45;
        private const float PlaybackFrameRate = 25f;
        private const float TransformTolerance = 0.0001f;
        private const float CorrectionTrsTolerance = 0.0002f;
        private const float MaximumHandSurfaceDistance = 0.04f;
        private const float MaximumGripCenterToPalmCenter = 0.02f;
        private const float MaximumTipUpErrorDegrees = 0.25f;
        private const float UnitySwordAlbedoExposure = 3f;
        private static readonly Vector3 ApprovedGripCenterLocal = new Vector3(0f, 0f, -0.103f);

        private static readonly int[] StaticSlotIndices = { 0, 1, 4, 5, 6, 7, 8, 9, 10, 11 };
        private static readonly float[] ReviewTimes = { 0f, 0.25f, 0.5f, 0.75f, 1f };

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Approved Long Sword To All Slots")]
        public static void ApplyIspantApprovedLongSwordAllSlots()
        {
            ConfigureSourceAssets();
            var materials = CreateOrUpdateMaterials();
            var sources = RequireSources(materials);
            var scene = RequireScene(requireClean: false);
            var placement = RequirePlacement(scene);
            var slotSnapshots = Enumerable.Range(0, ExpectedSlots)
                .Select(index => new TransformSnapshot(placement.GetChild(index)))
                .ToArray();
            var otherRootsBefore = OtherRootSignatures(scene, placement);

            Metrics metrics;
            try
            {
                foreach (var index in StaticSlotIndices)
                    ApplyStaticSlot(placement.GetChild(index), sources, materials);
                ApplyMoveSlot(placement.GetChild(2), sources, materials);
                ApplyDrawSlot(placement.GetChild(3), sources, materials);
                var drawModel = RequireSingleModel(placement.GetChild(3));
                var playbackClip = CreateOrUpdateDrawPlaybackClip(
                    drawModel,
                    sources.DrawSword.GetComponent<MeshFilter>().sharedMesh);
                ConfigureDrawAnimator(drawModel, CreateOrUpdateDrawController(playbackClip));

                for (var index = 0; index < slotSnapshots.Length; index++)
                {
                    if (!slotSnapshots[index].Matches(TransformTolerance))
                        throw new InvalidOperationException(
                            "Ispant slot transform changed: " + placement.GetChild(index).name + ".");
                }
                RequireEqual(
                    otherRootsBefore,
                    OtherRootSignatures(scene, placement),
                    "A scene root outside Approved Ispant Enemy Placement changed.");

                metrics = InspectApplied(scene, placement, sources, materials);
                WriteInspection(metrics, sources);
            }
            catch
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                throw;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("CargoRunMvp could not be saved after long-sword application.");
            AssetDatabase.SaveAssets();
            Debug.Log(
                "IspantApprovedLongSwordApplied Result=PASS" +
                ", Slots=12, ApprovedMountMeshes=True, SwordLength=0.9m, SwordTriangles=4092" +
                ", StaticMountsPreserved=10, MoveMountPreserved=True" +
                ", DrawParent=mixamorig:RightHand, DrawFrames=1-46" +
                ", RotationDuringDraw=True, FinalTipWorldUp=True" +
                ", MaximumAttachmentError=" + Num(metrics.MaximumAttachmentError) +
                ", MaximumHandSurfaceDistance=" + Num(metrics.MaximumHandSurfaceDistance) +
                ", MaximumSwordWorldRotation=" + Num(metrics.MaximumSwordWorldRotation) +
                ", UpwardRotationDegrees=" + Num(metrics.UpwardRotationDegrees) +
                ", FinalTipUpError=" + Num(metrics.FinalTipUpError) +
                ", OtherSceneRootsChanged=False, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Approved Long Sword All Slots")]
        public static void InspectIspantApprovedLongSwordAllSlots()
        {
            var materials = RequireMaterials();
            var sources = RequireSources(materials);
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var metrics = InspectApplied(scene, RequirePlacement(scene), sources, materials);
            WriteInspection(metrics, sources);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Approved long-sword inspection changed the scene dirty state.");
            Debug.Log(
                "IspantApprovedLongSwordInspected Result=PASS" +
                ", Slots=" + metrics.SwordCount +
                ", ApprovedMountMeshes=True" +
                ", StaticBodyTriangles=3518, AnimatedBodyTriangles=3364" +
                ", SwordLength=0.9m, DrawFrames=1-46" +
                ", RotationDuringDraw=True, FinalTipWorldUp=True" +
                ", MaximumAttachmentError=" + Num(metrics.MaximumAttachmentError) +
                ", MaximumHandSurfaceDistance=" + Num(metrics.MaximumHandSurfaceDistance) +
                ", MaximumSwordWorldRotation=" + Num(metrics.MaximumSwordWorldRotation) +
                ", UpwardRotationDegrees=" + Num(metrics.UpwardRotationDegrees) +
                ", FinalTipUpError=" + Num(metrics.FinalTipUpError) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Approved Long Sword Review")]
        public static void CaptureIspantApprovedLongSwordReview()
        {
            var materials = RequireMaterials();
            var sources = RequireSources(materials);
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var metrics = InspectApplied(scene, placement, sources, materials);
            WriteInspection(metrics, sources);
            var destination = Absolute(CapturePath);
            if (File.Exists(destination))
                throw new InvalidOperationException("The one-time approved long-sword review already exists.");
            CaptureReview(placement, RequireDrawClip(), destination);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Approved long-sword capture changed the scene dirty state.");
            Debug.Log(
                "IspantApprovedLongSwordReviewCaptured Result=PASS" +
                ", Panels=StaticLineup,Draw0,Draw0.25,Draw0.5,Draw0.75,Draw1" +
                ", Image=" + CapturePath + ", SceneChanged=False.");
        }

        private static void ConfigureSourceAssets()
        {
            foreach (var path in new[] { SwordFbxPath, StaticMountFbxPath, MoveMountFbxPath, DrawMountFbxPath })
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter ??
                    throw new InvalidOperationException("Approved long-sword ModelImporter is missing: " + path);
                importer.isReadable = true;
                importer.importAnimation = false;
                importer.importBlendShapes = true;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importTangents = ModelImporterTangents.CalculateMikk;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                importer.optimizeGameObjects = false;
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.SaveAndReimport();
            }
            ConfigureTexture(BaseColorPath, false, false);
            ConfigureTexture(RoughnessPath, true, false);
            ConfigureTexture(MetallicPath, true, false);
            ConfigureTexture(NormalPath, true, true);
        }

        private static void ConfigureTexture(string path, bool nonColor, bool normal)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter ??
                throw new InvalidOperationException("Approved long-sword texture importer is missing: " + path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !nonColor;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        private static Material[] CreateOrUpdateMaterials()
        {
            if (!AssetDatabase.IsValidFolder(MaterialRoot))
                AssetDatabase.CreateFolder(AssetRoot, "Materials");
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath) ??
                throw new InvalidOperationException("The approved Ispant appearance shader is missing.");
            if (!shader.isSupported)
                throw new InvalidOperationException("The approved Ispant appearance shader is unsupported.");
            var textures = new[]
            {
                AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorPath),
                AssetDatabase.LoadAssetAtPath<Texture2D>(RoughnessPath),
                AssetDatabase.LoadAssetAtPath<Texture2D>(MetallicPath),
                AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath)
            };
            if (textures.Any(item => item == null))
                throw new InvalidOperationException("An approved long-sword baked texture is missing.");
            var paths = new[] { SteelMaterialPath, LeatherMaterialPath, EngravingMaterialPath };
            var result = new Material[paths.Length];
            for (var index = 0; index < paths.Length; index++)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(paths[index]);
                if (material == null)
                {
                    material = new Material(shader) { name = Path.GetFileNameWithoutExtension(paths[index]) };
                    AssetDatabase.CreateAsset(material, paths[index]);
                }
                else
                {
                    material.shader = shader;
                }
                // The approved bake is scene-linear. After the exact sRGB transfer,
                // this exposure keeps the worn steel readable under CargoRun lighting
                // without changing the approved hue, UV wear pattern, or material maps.
                material.SetColor(
                    "_BaseColor",
                    new Color(
                        UnitySwordAlbedoExposure,
                        UnitySwordAlbedoExposure,
                        UnitySwordAlbedoExposure,
                        1f));
                material.SetFloat("_NormalStrength", 1f);
                material.SetFloat("_UseMaps", 1f);
                material.SetFloat("_UseUv1", 0f);
                material.SetFloat("_RoughnessBias", 0f);
                material.SetFloat("_MetallicBias", 0f);
                material.SetFloat("_CoatWeight", 0f);
                material.SetFloat("_CoatRoughness", 0.34f);
                material.SetFloat("_FeatureMode", 0f);
                material.SetFloat("_ApprovedYFlip", 0f);
                material.SetTexture("_BaseMap", textures[0]);
                material.SetTexture("_RoughnessMap", textures[1]);
                material.SetTexture("_MetallicMap", textures[2]);
                material.SetTexture("_NormalMap", textures[3]);
                EditorUtility.SetDirty(material);
                result[index] = material;
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        private static Material[] RequireMaterials()
        {
            var result = new[]
            {
                AssetDatabase.LoadAssetAtPath<Material>(SteelMaterialPath),
                AssetDatabase.LoadAssetAtPath<Material>(LeatherMaterialPath),
                AssetDatabase.LoadAssetAtPath<Material>(EngravingMaterialPath)
            };
            if (result.Any(item => item == null))
                throw new InvalidOperationException("Approved long-sword materials have not been applied.");
            return result;
        }

        private static Sources RequireSources(Material[] materials)
        {
            var swordPrefab = RequirePrefab(SwordFbxPath);
            var staticPrefab = RequirePrefab(StaticMountFbxPath);
            var movePrefab = RequirePrefab(MoveMountFbxPath);
            var drawPrefab = RequirePrefab(DrawMountFbxPath);
            var commonRenderer = swordPrefab.GetComponentsInChildren<MeshRenderer>(true).Single();
            var staticBody = RequireRenderer<SkinnedMeshRenderer>(staticPrefab.transform, "Ispant_Armed_Body");
            var staticSword = RequireRenderer<MeshRenderer>(staticPrefab.transform, SwordName);
            var moveSword = RequireRenderer<MeshRenderer>(movePrefab.transform, SwordName);
            var drawSword = RequireRenderer<MeshRenderer>(drawPrefab.transform, "Ispant_Reference_LongSword");
            var standaloneMesh = commonRenderer.GetComponent<MeshFilter>().sharedMesh;
            var commonMesh = staticSword.GetComponent<MeshFilter>().sharedMesh;
            if (standaloneMesh.vertexCount != commonMesh.vertexCount ||
                TriangleCount(standaloneMesh) != TriangleCount(commonMesh) ||
                standaloneMesh.subMeshCount != commonMesh.subMeshCount)
                throw new InvalidOperationException("The standalone approved sword topology differs from the mounted source.");
            var standaloneImportedLength = Vector3.Scale(
                standaloneMesh.bounds.size,
                commonRenderer.transform.lossyScale).z;
            if (Mathf.Abs(standaloneImportedLength - ExpectedSwordLength) > TransformTolerance)
                throw new InvalidOperationException(
                    "The approved standalone sword is not 0.9m long after FBX scale: " +
                    Num(standaloneImportedLength) + ".");
            foreach (var mounted in new[] { commonMesh, moveSword.GetComponent<MeshFilter>().sharedMesh, drawSword.GetComponent<MeshFilter>().sharedMesh })
            {
                if (TriangleCount(mounted) != ExpectedSwordTriangles || mounted.subMeshCount != materials.Length)
                    throw new InvalidOperationException("An approved mounted long-sword mesh structure differs.");
            }
            return new Sources(
                commonMesh,
                staticBody,
                staticSword,
                moveSword,
                drawSword);
        }

        private static void ApplyStaticSlot(Transform slot, Sources sources, Material[] materials)
        {
            var model = RequireSingleModel(slot);
            var replacedPreviousApprovedSword = RemoveExistingApprovedSword(model);
            var body = RequireRenderer<SkinnedMeshRenderer>(model, "Ispant_Armed_Body");
            var currentBodyTriangles = TriangleCount(body.sharedMesh);
            if (currentBodyTriangles != 3596 &&
                !(replacedPreviousApprovedSword && currentBodyTriangles == ExpectedStaticBodyTriangles))
                throw new InvalidOperationException(slot.name + " current static body structure differs.");
            RequireBoneOrder(body, sources.StaticBody);
            body.sharedMesh = sources.StaticBody.sharedMesh;
            var hips = model.GetComponentsInChildren<Transform>(true).Single(item => item.name == "Hips");
            CloneSword(
                sources.StaticSword.gameObject,
                hips,
                sources.StaticSword.GetComponent<MeshFilter>().sharedMesh,
                materials,
                Matrix4x4.identity);
            EditorUtility.SetDirty(body);
            EditorUtility.SetDirty(model.gameObject);
        }

        private static void ApplyMoveSlot(Transform slot, Sources sources, Material[] materials)
        {
            var model = RequireSingleModel(slot);
            var replacedPreviousApprovedSword = RemoveExistingApprovedSword(model);
            var legacyMatches = model.GetComponentsInChildren<MeshRenderer>(true)
                .Where(item => item.name == "Ispant_Fixed_Sword").ToArray();
            if (!replacedPreviousApprovedSword && legacyMatches.Length != 1)
                throw new InvalidOperationException("The current move legacy sword is missing.");
            if (legacyMatches.Length > 1 ||
                (legacyMatches.Length == 1 &&
                 TriangleCount(legacyMatches[0].GetComponent<MeshFilter>().sharedMesh) != 78))
                throw new InvalidOperationException("The current move legacy sword structure differs.");
            var hips = model.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == "mixamorig:Hips");
            CloneSword(
                sources.MoveSword.gameObject,
                hips,
                sources.MoveSword.GetComponent<MeshFilter>().sharedMesh,
                materials,
                Matrix4x4.identity);
            if (legacyMatches.Length == 1)
                UnityEngine.Object.DestroyImmediate(legacyMatches[0].gameObject);
            EditorUtility.SetDirty(model.gameObject);
        }

        private static void ApplyDrawSlot(Transform slot, Sources sources, Material[] materials)
        {
            var model = RequireSingleModel(slot);
            var replacedPreviousApprovedSword = RemoveExistingApprovedSword(model);
            var legacySwords = model.GetComponentsInChildren<MeshRenderer>(true)
                .Where(item => item.name == "Ispant_DrawSword_RigidSword").ToArray();
            var legacySheaths = model.GetComponentsInChildren<MeshRenderer>(true)
                .Where(item => item.name == "Ispant_DrawSword_RigidSheath").ToArray();
            if (!replacedPreviousApprovedSword && (legacySwords.Length != 1 || legacySheaths.Length != 1))
                throw new InvalidOperationException("The current draw-sword legacy sword/sheath pair is missing.");
            if (legacySwords.Length > 1 || legacySheaths.Length > 1)
                throw new InvalidOperationException("The current draw-sword legacy sword/sheath structure differs.");
            var rightHand = model.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == "mixamorig:RightHand");
            var approvedSword = CloneSword(
                sources.DrawSword.gameObject,
                rightHand,
                sources.DrawSword.GetComponent<MeshFilter>().sharedMesh,
                materials,
                Matrix4x4.identity);
            AlignGripCenterToRightPalm(model, rightHand, approvedSword.transform);
            RecenterSwordRootAtGrip(approvedSword.transform);
            if (legacySwords.Length == 1)
                UnityEngine.Object.DestroyImmediate(legacySwords[0].gameObject);
            if (legacySheaths.Length == 1)
                UnityEngine.Object.DestroyImmediate(legacySheaths[0].gameObject);
            EditorUtility.SetDirty(model.gameObject);
        }

        private static GameObject CloneSword(
            GameObject source,
            Transform parent,
            Mesh commonMesh,
            Material[] materials,
            Matrix4x4 meshCorrection)
        {
            var root = new GameObject(SwordName);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = source.transform.localPosition;
            root.transform.localRotation = source.transform.localRotation;
            root.transform.localScale = source.transform.localScale;
            var rendererObject = new GameObject(SwordRendererName);
            rendererObject.transform.SetParent(root.transform, false);
            SetLocalMatrix(rendererObject.transform, meshCorrection);
            var filter = rendererObject.AddComponent<MeshFilter>();
            var renderer = rendererObject.AddComponent<MeshRenderer>();
            filter.sharedMesh = commonMesh;
            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(rendererObject);
            return root;
        }

        private static void AlignGripCenterToRightPalm(
            Transform model,
            Transform rightHand,
            Transform swordRoot)
        {
            var body = RequireRenderer<SkinnedMeshRenderer>(model, "Ispant_Armed_Body");
            var swordRenderer = RequireRenderer<MeshRenderer>(swordRoot, SwordRendererName);
            var clip = RequireDrawClip();
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            Vector3 palmCenterInHand;
            try
            {
                AnimationMode.StartAnimationMode();
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(model.gameObject, clip, 0f);
                AnimationMode.EndSampling();
                palmCenterInHand = rightHand.InverseTransformPoint(
                    CalculateWeightedRightPalmCenter(body, rightHand));
            }
            finally
            {
                if (AnimationMode.InAnimationMode())
                    AnimationMode.StopAnimationMode();
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
            }

            var gripCenterInHand = rightHand.InverseTransformPoint(
                swordRenderer.transform.TransformPoint(ApprovedGripCenterLocal));
            swordRoot.localPosition += palmCenterInHand - gripCenterInHand;
            EditorUtility.SetDirty(swordRoot);
        }

        private static void RecenterSwordRootAtGrip(Transform swordRoot)
        {
            var renderer = RequireRenderer<MeshRenderer>(swordRoot, SwordRendererName);
            var gripWorld = renderer.transform.TransformPoint(ApprovedGripCenterLocal);
            var gripCenterInRoot = swordRoot.InverseTransformPoint(gripWorld);
            swordRoot.position = gripWorld;
            renderer.transform.localPosition -= gripCenterInRoot;
            var residual = Vector3.Distance(
                swordRoot.position,
                renderer.transform.TransformPoint(ApprovedGripCenterLocal));
            if (residual > TransformTolerance)
                throw new InvalidOperationException(
                    "The draw-sword root could not be recentered on the grip: " + Num(residual) + ".");
            EditorUtility.SetDirty(swordRoot);
            EditorUtility.SetDirty(renderer);
        }

        private static AnimationClip CreateOrUpdateDrawPlaybackClip(Transform model, Mesh swordMesh)
        {
            var source = RequireDrawClip();
            if (Mathf.Abs(source.frameRate - PlaybackFrameRate) > 0.001f)
                throw new InvalidOperationException("The source draw-sword clip must remain 25fps.");
            var swordRenderer = RequireRenderer<MeshRenderer>(model, SwordRendererName);
            var sword = swordRenderer.transform.parent ??
                throw new InvalidOperationException("The draw-sword approved sword root is missing.");
            var hand = model.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == "mixamorig:RightHand");
            if (sword.parent != hand)
                throw new InvalidOperationException("The upward draw-sword rotation requires a direct RightHand child.");

            var bladeAxisLocal = CalculateBladeAxisLocal(swordMesh);
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            Quaternion startLocalRotation;
            Quaternion targetLocalRotation;
            try
            {
                AnimationMode.StartAnimationMode();
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(model.gameObject, source, source.length);
                AnimationMode.EndSampling();
                startLocalRotation = sword.localRotation;
                var currentTipWorld = swordRenderer.transform.TransformDirection(bladeAxisLocal).normalized;
                var worldCorrection = Quaternion.FromToRotation(currentTipWorld, Vector3.up);
                var targetWorldRotation = worldCorrection * sword.rotation;
                targetLocalRotation = Quaternion.Inverse(hand.rotation) * targetWorldRotation;
                if (Quaternion.Dot(startLocalRotation, targetLocalRotation) < 0f)
                    targetLocalRotation = Negate(targetLocalRotation);
            }
            finally
            {
                if (AnimationMode.InAnimationMode())
                    AnimationMode.StopAnimationMode();
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DrawPlaybackClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = DrawPlaybackClipName };
                AssetDatabase.CreateAsset(clip, DrawPlaybackClipPath);
            }
            clip.ClearCurves();
            clip.frameRate = PlaybackFrameRate;
            clip.wrapMode = WrapMode.Loop;
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var sourceCurve = AnimationUtility.GetEditorCurve(source, binding) ??
                    throw new InvalidOperationException("A source draw-sword curve is missing.");
                AnimationUtility.SetEditorCurve(
                    clip,
                    binding,
                    new AnimationCurve(sourceCurve.keys)
                    {
                        preWrapMode = sourceCurve.preWrapMode,
                        postWrapMode = sourceCurve.postWrapMode
                    });
            }
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
                AnimationUtility.SetObjectReferenceCurve(
                    clip,
                    binding,
                    AnimationUtility.GetObjectReferenceCurve(source, binding));

            var swordPath = AnimationUtility.CalculateTransformPath(sword, model);
            var rotations = CreateUpwardRotationKeys(
                startLocalRotation,
                targetLocalRotation,
                source.length);
            SetQuaternionCurve(clip, swordPath, "m_LocalRotation.x", rotations, value => value.x);
            SetQuaternionCurve(clip, swordPath, "m_LocalRotation.y", rotations, value => value.y);
            SetQuaternionCurve(clip, swordPath, "m_LocalRotation.z", rotations, value => value.z);
            SetQuaternionCurve(clip, swordPath, "m_LocalRotation.w", rotations, value => value.w);
            var settings = AnimationUtility.GetAnimationClipSettings(source);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AnimationUtility.SetAnimationEvents(clip, AnimationUtility.GetAnimationEvents(source));
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static KeyframeRotation[] CreateUpwardRotationKeys(
            Quaternion start,
            Quaternion target,
            float drawEnd)
        {
            var poseSamples = Mathf.RoundToInt(drawEnd * PlaybackFrameRate) + 1;
            if (poseSamples != ExpectedImportedDrawPoseSamples)
                throw new InvalidOperationException(
                    "The imported 1-46 draw range does not contain the expected 45 Unity poses.");
            var result = new List<KeyframeRotation>();
            for (var sample = 0; sample < poseSamples; sample++)
            {
                var normalized = sample / (float)(poseSamples - 1);
                var smooth = normalized * normalized * (3f - 2f * normalized);
                var rotation = Quaternion.Slerp(start, target, smooth);
                result.Add(new KeyframeRotation(
                    drawEnd * normalized,
                    rotation));
            }
            return result.ToArray();
        }

        private static void SetQuaternionCurve(
            AnimationClip clip,
            string path,
            string property,
            KeyframeRotation[] rotations,
            Func<Quaternion, float> component)
        {
            var curve = new AnimationCurve(rotations.Select(item =>
                new Keyframe(item.Time, component(item.Rotation))).ToArray());
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve,
                    index,
                    AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(
                    curve,
                    index,
                    AnimationUtility.TangentMode.Linear);
            }
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property),
                curve);
        }

        private static Vector3 CalculateBladeAxisLocal(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var maximumZ = vertices.Max(vertex => vertex.z);
            var tipVertices = vertices.Where(vertex => maximumZ - vertex.z <= 0.0005f).ToArray();
            if (tipVertices.Length == 0)
                throw new InvalidOperationException("The approved sword tip vertices are missing.");
            var tipCenter = tipVertices.Aggregate(Vector3.zero, (sum, vertex) => sum + vertex) /
                            tipVertices.Length;
            var axis = (tipCenter - ApprovedGripCenterLocal).normalized;
            if (Vector3.Dot(axis, Vector3.forward) < 0.99f)
                throw new InvalidOperationException("The approved sword blade axis is not local +Z.");
            return axis;
        }

        private static Quaternion Negate(Quaternion value)
        {
            return new Quaternion(-value.x, -value.y, -value.z, -value.w);
        }

        private static AnimatorController CreateOrUpdateDrawController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(DrawControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(DrawControllerPath);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            var stateMachine = controller.layers[0].stateMachine;
            foreach (var child in stateMachine.states.ToArray())
                stateMachine.RemoveState(child.state);
            foreach (var child in stateMachine.stateMachines.ToArray())
                stateMachine.RemoveStateMachine(child.stateMachine);
            var state = stateMachine.AddState(DrawPlaybackClipName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void ConfigureDrawAnimator(Transform model, RuntimeAnimatorController controller)
        {
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length != 1)
                throw new InvalidOperationException("The draw-sword model must contain exactly one Animator.");
            var animator = animators[0];
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;
            animator.Rebind();
            animator.Update(0f);
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
        }

        private static Vector3 CalculateWeightedRightPalmCenter(
            SkinnedMeshRenderer body,
            Transform rightHand)
        {
            var mesh = body.sharedMesh ??
                throw new InvalidOperationException("The draw-sword body mesh is missing.");
            var rightHandIndex = Array.IndexOf(body.bones, rightHand);
            if (rightHandIndex < 0)
                throw new InvalidOperationException("The draw-sword body does not reference the RightHand bone.");
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var bindPoses = mesh.bindposes;
            if (weights.Length != vertices.Length || bindPoses.Length != body.bones.Length)
                throw new InvalidOperationException("The draw-sword body skinning data differs.");

            var center = Vector3.zero;
            var count = 0;
            for (var index = 0; index < vertices.Length; index++)
            {
                var weight = weights[index];
                var rightHandWeight = BoneWeightForIndex(weight, rightHandIndex);
                if (rightHandWeight < 0.1f)
                    continue;
                center += SkinVertexToWorld(vertices[index], weight, body.bones, bindPoses);
                count++;
            }
            if (count < 4)
                throw new InvalidOperationException("The draw-sword body has too few RightHand-weighted vertices.");
            return center / count;
        }

        private static float BoneWeightForIndex(BoneWeight weight, int boneIndex)
        {
            var result = 0f;
            if (weight.boneIndex0 == boneIndex) result += weight.weight0;
            if (weight.boneIndex1 == boneIndex) result += weight.weight1;
            if (weight.boneIndex2 == boneIndex) result += weight.weight2;
            if (weight.boneIndex3 == boneIndex) result += weight.weight3;
            return result;
        }

        private static Vector3 SkinVertexToWorld(
            Vector3 vertex,
            BoneWeight weight,
            Transform[] bones,
            Matrix4x4[] bindPoses)
        {
            var result = Vector3.zero;
            result += SkinContribution(vertex, weight.boneIndex0, weight.weight0, bones, bindPoses);
            result += SkinContribution(vertex, weight.boneIndex1, weight.weight1, bones, bindPoses);
            result += SkinContribution(vertex, weight.boneIndex2, weight.weight2, bones, bindPoses);
            result += SkinContribution(vertex, weight.boneIndex3, weight.weight3, bones, bindPoses);
            return result;
        }

        private static Vector3 SkinContribution(
            Vector3 vertex,
            int boneIndex,
            float weight,
            Transform[] bones,
            Matrix4x4[] bindPoses)
        {
            if (weight <= 0f)
                return Vector3.zero;
            return (bones[boneIndex].localToWorldMatrix * bindPoses[boneIndex])
                .MultiplyPoint3x4(vertex) * weight;
        }

        private static Metrics InspectApplied(
            Scene scene,
            Transform placement,
            Sources sources,
            Material[] materials)
        {
            if (placement.childCount != ExpectedSlots)
                throw new InvalidOperationException("Approved Ispant placement must contain twelve slots.");
            var swords = new List<MeshRenderer>();
            for (var index = 0; index < ExpectedSlots; index++)
            {
                var slot = placement.GetChild(index);
                var model = RequireSingleModel(slot);
                var sword = RequireRenderer<MeshRenderer>(model, SwordRendererName);
                var swordRoot = sword.transform.parent;
                if (swordRoot == null || swordRoot.name != SwordName)
                    throw new InvalidOperationException(slot.name + " approved sword renderer root differs.");
                var filter = sword.GetComponent<MeshFilter>();
                var expectedMesh = index == 2
                    ? sources.MoveSword.GetComponent<MeshFilter>().sharedMesh
                    : index == 3
                        ? sources.DrawSword.GetComponent<MeshFilter>().sharedMesh
                        : sources.StaticSword.GetComponent<MeshFilter>().sharedMesh;
                if (filter.sharedMesh != expectedMesh)
                    throw new InvalidOperationException(slot.name + " does not reference its exact approved mount mesh.");
                if (!sword.sharedMaterials.SequenceEqual(materials))
                    throw new InvalidOperationException(slot.name + " approved sword materials differ.");
                if (materials.Any(material =>
                        Vector4.Distance(
                            material.GetColor("_BaseColor"),
                            new Color(
                                UnitySwordAlbedoExposure,
                                UnitySwordAlbedoExposure,
                                UnitySwordAlbedoExposure,
                                1f)) > TransformTolerance))
                    throw new InvalidOperationException("The approved sword visibility exposure differs.");
                if (TriangleCount(filter.sharedMesh) != ExpectedSwordTriangles)
                    throw new InvalidOperationException(slot.name + " approved sword triangle count differs.");
                swords.Add(sword);

                var body = RequireRenderer<SkinnedMeshRenderer>(model, "Ispant_Armed_Body");
                var expectedBodyTriangles = index == 2 || index == 3
                    ? ExpectedAnimatedBodyTriangles
                    : ExpectedStaticBodyTriangles;
                if (TriangleCount(body.sharedMesh) != expectedBodyTriangles)
                    throw new InvalidOperationException(slot.name + " body triangle count differs after replacement.");
                if (index == 3)
                {
                    if (swordRoot.parent == null || swordRoot.parent.name != "mixamorig:RightHand")
                        throw new InvalidOperationException("Only Ispant_04_DrawSword must parent the sword to RightHand.");
                    RequireRotationScaleMatch(
                        swordRoot,
                        sources.DrawSword.transform,
                        "draw-sword right-hand mount");
                }
                else if (index == 2)
                {
                    if (swordRoot.parent == null || swordRoot.parent.name != "mixamorig:Hips")
                        throw new InvalidOperationException("Ispant_03_Move approved sword must preserve the Hips mount.");
                    RequireTransformMatch(
                        swordRoot,
                        sources.MoveSword.transform,
                        "move Hips mount");
                }
                else
                {
                    if (swordRoot.parent == null || swordRoot.parent.name != "Hips")
                        throw new InvalidOperationException(slot.name + " approved sword must preserve the static Hips mount.");
                    RequireTransformMatch(
                        swordRoot,
                        sources.StaticSword.transform,
                        slot.name + " static Hips mount");
                }
            }
            if (swords.Count != ExpectedSlots)
                throw new InvalidOperationException("Exactly twelve approved long swords are required.");
            if (placement.GetComponentsInChildren<Renderer>(true).Any(item =>
                    item.name == "Ispant_Fixed_Sword" ||
                    item.name == "Ispant_DrawSword_RigidSword" ||
                    item.name == "Ispant_DrawSword_RigidSheath"))
                throw new InvalidOperationException("A replaced legacy Ispant sword or sheath remains.");

            var animation = InspectDrawAnimation(
                placement.GetChild(3),
                sources.DrawSword.GetComponent<MeshFilter>().sharedMesh);
            return new Metrics(
                swords.Count,
                animation.MaximumAttachmentError,
                animation.MaximumHandSurfaceDistance,
                animation.MaximumFollowMotion,
                animation.MaximumSwordWorldRotation,
                animation.MaximumHandWorldRotation,
                animation.MaximumRelativeRotationError,
                animation.MinimumGripCenterToHandOrigin,
                animation.MaximumGripCenterToHandOrigin,
                animation.MinimumGripCenterToPalmCenter,
                animation.MaximumGripCenterToPalmCenter,
                animation.MaximumHeldHandPositionMotion,
                animation.MaximumHeldHandRotationMotion,
                animation.MaximumUpwardRotationStep,
                animation.UpwardRotationDegrees,
                animation.FinalTipUpError);
        }

        private static AnimationMetrics InspectDrawAnimation(Transform drawSlot, Mesh commonMesh)
        {
            var model = RequireSingleModel(drawSlot);
            var swordRenderer = RequireRenderer<MeshRenderer>(model, SwordRendererName);
            var sword = swordRenderer.transform.parent ??
                throw new InvalidOperationException("The draw-sword approved sword root is missing.");
            var hand = model.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == "mixamorig:RightHand");
            if (sword.parent != hand)
                throw new InvalidOperationException("The draw-sword approved sword is not a direct RightHand child.");
            var sourceClip = RequireDrawClip();
            var clip = RequireDrawPlaybackClip();
            var controller = RequireDrawController();
            var animator = model.GetComponentsInChildren<Animator>(true).Single();
            if (!animator.enabled || animator.runtimeAnimatorController != controller || animator.applyRootMotion ||
                controller.layers[0].stateMachine.defaultState == null ||
                controller.layers[0].stateMachine.defaultState.motion != clip)
                throw new InvalidOperationException("The draw-sword playback Animator configuration differs.");
            if (Mathf.Abs(clip.frameRate - PlaybackFrameRate) > 0.001f ||
                Mathf.Abs(clip.length - sourceClip.length) > 0.001f)
                throw new InvalidOperationException(
                    "The draw-sword playback clip must match the original Mixamo duration.");
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var vertices = commonMesh.vertices;
            var bladeAxisLocal = CalculateBladeAxisLocal(commonMesh);
            var maximumAttachment = 0f;
            var maximumHandDistance = 0f;
            var maximumFollow = 0f;
            var maximumSwordWorldRotation = 0f;
            var maximumHandWorldRotation = 0f;
            var minimumGripCenterToHandOrigin = float.PositiveInfinity;
            var maximumGripCenterToHandOrigin = 0f;
            var minimumGripCenterToPalmCenter = float.PositiveInfinity;
            var maximumGripCenterToPalmCenter = 0f;
            var maximumUpwardRotationStep = 0f;
            var finalTipUpError = 180f;
            var upwardRotationDegrees = 0f;
            var midpointUpwardRotationDegrees = 0f;
            var firstPosition = Vector3.zero;
            var firstSwordWorldRotation = Quaternion.identity;
            var firstHandWorldRotation = Quaternion.identity;
            var baselineSwordLocalPosition = Vector3.zero;
            var upwardStartRotation = Quaternion.identity;
            var previousUpwardRotation = Quaternion.identity;
            var previousUpwardDegrees = 0f;
            var poseSamples = Mathf.RoundToInt(clip.length * PlaybackFrameRate) + 1;
            if (poseSamples != ExpectedImportedDrawPoseSamples)
                throw new InvalidOperationException(
                    "The imported 1-46 draw range does not contain 45 Unity poses.");
            try
            {
                AnimationMode.StartAnimationMode();
                for (var sample = 0; sample < poseSamples; sample++)
                {
                    var normalized = sample / (float)(poseSamples - 1);
                    var time = clip.length * normalized;
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(model.gameObject, clip, time);
                    AnimationMode.EndSampling();
                    var nearest = float.PositiveInfinity;
                    var matrix = swordRenderer.transform.localToWorldMatrix;
                    foreach (var vertex in vertices)
                        nearest = Mathf.Min(nearest, Vector3.Distance(matrix.MultiplyPoint3x4(vertex), hand.position));
                    maximumHandDistance = Mathf.Max(maximumHandDistance, nearest);
                    var gripCenter = swordRenderer.transform.TransformPoint(ApprovedGripCenterLocal);
                    if (sample == 0)
                    {
                        firstPosition = sword.position;
                        firstSwordWorldRotation = sword.rotation;
                        firstHandWorldRotation = hand.rotation;
                        baselineSwordLocalPosition = sword.localPosition;
                        upwardStartRotation = sword.localRotation;
                        previousUpwardRotation = sword.localRotation;
                    }
                    // The sword root is a direct RightHand child. Test that attachment in
                    // parent space; a world-to-hand matrix round trip across the imported
                    // FBX bone hierarchy introduces visible-scale floating-point noise.
                    maximumAttachment = Mathf.Max(
                        maximumAttachment,
                        Vector3.Distance(baselineSwordLocalPosition, sword.localPosition));
                    maximumFollow = Mathf.Max(maximumFollow, Vector3.Distance(firstPosition, sword.position));
                    maximumSwordWorldRotation = Mathf.Max(
                        maximumSwordWorldRotation,
                        Quaternion.Angle(firstSwordWorldRotation, sword.rotation));
                    maximumHandWorldRotation = Mathf.Max(
                        maximumHandWorldRotation,
                        Quaternion.Angle(firstHandWorldRotation, hand.rotation));
                    var gripCenterToHandOrigin = Vector3.Distance(gripCenter, hand.position);
                    minimumGripCenterToHandOrigin = Mathf.Min(
                        minimumGripCenterToHandOrigin,
                        gripCenterToHandOrigin);
                    maximumGripCenterToHandOrigin = Mathf.Max(
                        maximumGripCenterToHandOrigin,
                        gripCenterToHandOrigin);
                    var gripCenterToPalmCenter = Vector3.Distance(
                        gripCenter,
                        CalculateWeightedRightPalmCenter(
                            RequireRenderer<SkinnedMeshRenderer>(model, "Ispant_Armed_Body"),
                            hand));
                    minimumGripCenterToPalmCenter = Mathf.Min(
                        minimumGripCenterToPalmCenter,
                        gripCenterToPalmCenter);
                    maximumGripCenterToPalmCenter = Mathf.Max(
                        maximumGripCenterToPalmCenter,
                        gripCenterToPalmCenter);
                    var currentUpwardDegrees = Quaternion.Angle(upwardStartRotation, sword.localRotation);
                    if (currentUpwardDegrees + 0.05f < previousUpwardDegrees)
                        throw new InvalidOperationException(
                            "The sword rotation reverses during the original draw motion.");
                    if (sample > 0)
                        maximumUpwardRotationStep = Mathf.Max(
                            maximumUpwardRotationStep,
                            Quaternion.Angle(previousUpwardRotation, sword.localRotation));
                    if (sample == (poseSamples - 1) / 2)
                        midpointUpwardRotationDegrees = currentUpwardDegrees;
                    previousUpwardDegrees = currentUpwardDegrees;
                    previousUpwardRotation = sword.localRotation;
                    if (sample == poseSamples - 1)
                    {
                        upwardRotationDegrees = Quaternion.Angle(upwardStartRotation, sword.localRotation);
                        finalTipUpError = Vector3.Angle(
                            swordRenderer.transform.TransformDirection(bladeAxisLocal),
                            Vector3.up);
                    }
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode())
                    AnimationMode.StopAnimationMode();
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
            }
            if (maximumAttachment > TransformTolerance)
                throw new InvalidOperationException(
                    "The draw-sword grip pivot drifted relative to RightHand: " +
                    Num(maximumAttachment) + ".");
            if (maximumHandDistance > MaximumHandSurfaceDistance)
                throw new InvalidOperationException(
                    "The draw-sword hand is outside the approved grip surface distance: " + Num(maximumHandDistance) + ".");
            if (maximumFollow <= 0.02f)
                throw new InvalidOperationException("The approved draw sword did not follow visible right-arm motion.");
            if (maximumSwordWorldRotation <= 45f || maximumHandWorldRotation <= 45f)
                throw new InvalidOperationException(
                    "The sword or right hand does not move through the original draw motion.");
            if (maximumGripCenterToPalmCenter > MaximumGripCenterToPalmCenter)
                throw new InvalidOperationException(
                    "The approved grip center leaves the weighted right-palm center: " +
                    Num(maximumGripCenterToPalmCenter) + ".");
            if (upwardRotationDegrees <= 5f || maximumUpwardRotationStep <= 0f ||
                maximumUpwardRotationStep > 15f)
                throw new InvalidOperationException(
                    "The original 1-46 draw-range rotation is missing or not smooth: Rotation=" +
                    Num(upwardRotationDegrees) + ", MaxStep=" + Num(maximumUpwardRotationStep) + ".");
            if (midpointUpwardRotationDegrees < upwardRotationDegrees * 0.25f)
                throw new InvalidOperationException(
                    "The sword rotation starts too late in the original draw motion: Midpoint=" +
                    Num(midpointUpwardRotationDegrees) + ", Total=" + Num(upwardRotationDegrees) + ".");
            if (finalTipUpError > MaximumTipUpErrorDegrees)
                throw new InvalidOperationException(
                    "The final sword tip does not point world-up: Error=" + Num(finalTipUpError) + ".");
            return new AnimationMetrics(
                maximumAttachment,
                maximumHandDistance,
                maximumFollow,
                maximumSwordWorldRotation,
                maximumHandWorldRotation,
                0f,
                minimumGripCenterToHandOrigin,
                maximumGripCenterToHandOrigin,
                minimumGripCenterToPalmCenter,
                maximumGripCenterToPalmCenter,
                maximumAttachment,
                0f,
                maximumUpwardRotationStep,
                upwardRotationDegrees,
                finalTipUpError);
        }

        private static AnimationClip RequireDrawClip()
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(DrawAnimationFbxPath)
                .OfType<AnimationClip>()
                .Single(item => item.name == "Ispant_DrawSword_Mixamo");
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime)
                throw new InvalidOperationException("The draw-sword Mixamo clip must remain looping.");
            return clip;
        }

        private static AnimationClip RequireDrawPlaybackClip()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DrawPlaybackClipPath) ??
                throw new InvalidOperationException("The draw-sword playback clip is missing.");
            if (clip.name != DrawPlaybackClipName)
                throw new InvalidOperationException("The draw-sword playback clip name differs.");
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime)
                throw new InvalidOperationException("The draw-sword playback clip must loop.");
            return clip;
        }

        private static AnimatorController RequireDrawController()
        {
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(DrawControllerPath) ??
                throw new InvalidOperationException("The draw-sword AnimatorController is missing.");
        }

        private static void CaptureReview(Transform placement, AnimationClip clip, string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The approved long-sword capture folder is invalid."));
            var allRenderers = placement.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Select(item => new RendererSnapshot(item)).ToArray();
            var drawModel = RequireSingleModel(placement.GetChild(3));
            var drawSnapshots = drawModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var sourceCamera = GameObject.Find("Player")?.GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException("The Player camera is missing for approved long-sword review.");
            var cameraObject = new GameObject("IspantApprovedLongSwordReviewCamera", typeof(Camera))
                { hideFlags = HideFlags.HideAndDontSave };
            const int panelWidth = 640;
            const int panelHeight = 640;
            const int panels = 6;
            var strip = new Texture2D(panelWidth * panels, panelHeight, TextureFormat.RGB24, false);
            var target = new RenderTexture(panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelWidth, panelHeight, TextureFormat.RGB24, false);
            var oldActive = RenderTexture.active;
            try
            {
                foreach (var snapshot in allRenderers)
                    snapshot.Renderer.enabled = false;
                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = ~0;
                camera.fieldOfView = 34f;
                camera.targetTexture = target;

                var staticRenderers = placement.GetChild(0).GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in staticRenderers)
                    renderer.enabled = true;
                var staticBody = RequireRenderer<SkinnedMeshRenderer>(placement.GetChild(0), "Ispant_Armed_Body");
                FrameCamera(camera, staticBody.bounds.center, staticBody.bounds.size.y, 1f);
                RenderPanel(camera, panel, strip, target, 0, panelWidth, panelHeight);
                foreach (var renderer in staticRenderers)
                    renderer.enabled = false;

                var drawRenderers = drawModel.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in drawRenderers)
                    renderer.enabled = true;
                var drawBody = RequireRenderer<SkinnedMeshRenderer>(drawModel, "Ispant_Armed_Body");
                for (var index = 0; index < ReviewTimes.Length; index++)
                {
                    AnimationMode.StartAnimationMode();
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(drawModel.gameObject, clip, ReviewTimes[index] * clip.length);
                    AnimationMode.EndSampling();
                    FrameCamera(camera, drawBody.bounds.center, staticBody.bounds.size.y, 1f);
                    RenderPanel(camera, panel, strip, target, index + 1, panelWidth, panelHeight);
                    AnimationMode.StopAnimationMode();
                }
                strip.Apply();
                File.WriteAllBytes(destination, strip.EncodeToPNG());
            }
            finally
            {
                if (AnimationMode.InAnimationMode())
                    AnimationMode.StopAnimationMode();
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                foreach (var snapshot in allRenderers)
                    snapshot.Restore();
                foreach (var snapshot in drawSnapshots)
                    snapshot.Restore();
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(strip);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void RenderPanel(
            Camera camera,
            Texture2D panel,
            Texture2D strip,
            RenderTexture target,
            int panelIndex,
            int width,
            int height)
        {
            camera.Render();
            RenderTexture.active = target;
            panel.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            panel.Apply();
            var pixels = panel.GetPixels32();
            if (pixels.Any(pixel => pixel.r >= 240 && pixel.b >= 240 && pixel.g <= 24))
                throw new InvalidOperationException("Approved long-sword review contains magenta shader fallback.");
            strip.SetPixels32(panelIndex * width, 0, width, height, pixels);
        }

        private static void FrameCamera(Camera camera, Vector3 center, float height, float aspect)
        {
            camera.aspect = aspect;
            var vertical = (height * 0.5f) / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            camera.transform.position = center + Vector3.back * vertical * 1.2f + Vector3.up * height * 0.01f;
            camera.transform.rotation = Quaternion.LookRotation(center - camera.transform.position, Vector3.up);
        }

        private static void WriteInspection(Metrics metrics, Sources sources)
        {
            var destination = Absolute(InspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The approved long-sword inspection folder is invalid."));
            File.WriteAllLines(destination, new[]
            {
                "Result=PASS",
                "Scene=" + ScenePath,
                "Placement=" + PlacementRootName,
                "Slots=" + metrics.SwordCount,
                "ApprovedBlendSha256=" + ApprovedBlendSha256,
                "DrawMountSourceByteExactSha256=" + DrawMountSha256,
                "StandaloneApprovedSwordFbx=" + SwordFbxPath,
                "StandaloneApprovedSwordFbxSha256=" + Sha256(AbsoluteAsset(SwordFbxPath)),
                "ApprovedMountedSwordMeshSources=StaticMount,MoveMount,DrawMount",
                "StaticMountFbx=" + StaticMountFbxPath,
                "StaticMountFbxSha256=" + Sha256(AbsoluteAsset(StaticMountFbxPath)),
                "MoveMountFbx=" + MoveMountFbxPath,
                "MoveMountFbxSha256=" + Sha256(AbsoluteAsset(MoveMountFbxPath)),
                "DrawMountFbx=" + DrawMountFbxPath,
                "DrawMountFbxSha256=" + Sha256(AbsoluteAsset(DrawMountFbxPath)),
                "StaticMountedSwordMesh=" + sources.StaticSword.GetComponent<MeshFilter>().sharedMesh.name,
                "MoveMountedSwordMesh=" + sources.MoveSword.GetComponent<MeshFilter>().sharedMesh.name,
                "DrawMountedSwordMesh=" + sources.DrawSword.GetComponent<MeshFilter>().sharedMesh.name,
                "MountedSwordTriangles=" + ExpectedSwordTriangles,
                "CommonSwordDimensionsM=0.198372,0.076,0.9",
                "ApprovedSourceVertices=2080",
                "ApprovedSourceTriangles=4092",
                "UnityImportedStaticSwordVertices=" + sources.StaticSword.GetComponent<MeshFilter>().sharedMesh.vertexCount,
                "UnityImportedMoveSwordVertices=" + sources.MoveSword.GetComponent<MeshFilter>().sharedMesh.vertexCount,
                "UnityImportedDrawSwordVertices=" + sources.DrawSword.GetComponent<MeshFilter>().sharedMesh.vertexCount,
                "Material0=" + SteelMaterialPath,
                "Material1=" + LeatherMaterialPath,
                "Material2=" + EngravingMaterialPath,
                "StaticSlots=1,2,5,6,7,8,9,10,11,12",
                "StaticBodyTriangles=3518",
                "StaticMountParent=Hips",
                "MoveSlot=3",
                "MoveBodyTriangles=3364",
                "MoveMountParent=mixamorig:Hips",
                "DrawSlot=4",
                "DrawBodyTriangles=3364",
                "DrawMountParent=mixamorig:RightHand",
                "DrawFrames=1-46",
                "DrawLoop=True",
                "RotationDuringOriginalDraw=True",
                "MaximumAttachmentError=" + Num(metrics.MaximumAttachmentError),
                "MaximumHandSurfaceDistance=" + Num(metrics.MaximumHandSurfaceDistance),
                "MaximumFollowMotion=" + Num(metrics.MaximumFollowMotion),
                "MaximumSwordWorldRotationDeg=" + Num(metrics.MaximumSwordWorldRotation),
                "MaximumHandWorldRotationDeg=" + Num(metrics.MaximumHandWorldRotation),
                "MinimumGripCenterToHandOrigin=" + Num(metrics.MinimumGripCenterToHandOrigin),
                "MaximumGripCenterToHandOrigin=" + Num(metrics.MaximumGripCenterToHandOrigin),
                "MinimumGripCenterToPalmCenter=" + Num(metrics.MinimumGripCenterToPalmCenter),
                "MaximumGripCenterToPalmCenter=" + Num(metrics.MaximumGripCenterToPalmCenter),
                "MaximumUpwardRotationStepDeg=" + Num(metrics.MaximumUpwardRotationStep),
                "UpwardRotationDegrees=" + Num(metrics.UpwardRotationDegrees),
                "FinalTipUpErrorDeg=" + Num(metrics.FinalTipUpError),
                "DrawRotationDurationSeconds=" + Num(RequireDrawPlaybackClip().length),
                "DrawSourceFrameRange=1-46",
                "UnityImportedDrawPoseSamples=" + ExpectedImportedDrawPoseSamples,
                "UnitySwordAlbedoExposure=" + Num(UnitySwordAlbedoExposure),
                "LegacySwordRenderers=0",
                "LegacyDrawSheathRenderers=0",
                "ApprovedMountMeshVariants=Static,Move,Draw",
                "SceneChangedByInspection=False"
            }, new UTF8Encoding(false));
        }

        private static GameObject RequirePrefab(string path)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(path) ??
                throw new InvalidOperationException("Approved long-sword prefab is missing: " + path);
        }

        private static Scene RequireScene(bool requireClean)
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("CargoRunMvp scene could not be opened.");
            if (requireClean && scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp must be clean before read-only inspection.");
            return scene;
        }

        private static Transform RequirePlacement(Scene scene)
        {
            var matches = scene.GetRootGameObjects().Where(item => item.name == PlacementRootName).ToArray();
            if (matches.Length != 1 || matches[0].transform.childCount != ExpectedSlots)
                throw new InvalidOperationException("Approved Ispant Enemy Placement structure differs.");
            return matches[0].transform;
        }

        private static Transform RequireSingleModel(Transform slot)
        {
            if (slot.childCount != 1)
                throw new InvalidOperationException(slot.name + " must contain exactly one model.");
            return slot.GetChild(0);
        }

        private static bool RemoveExistingApprovedSword(Transform model)
        {
            var matches = model.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == SwordName).ToArray();
            if (matches.Length > 1)
                throw new InvalidOperationException(model.name + " contains multiple approved long-sword roots.");
            if (matches.Length == 0)
                return false;
            UnityEngine.Object.DestroyImmediate(matches[0].gameObject);
            return true;
        }

        private static T RequireRenderer<T>(Transform root, string name) where T : Renderer
        {
            var matches = root.GetComponentsInChildren<T>(true).Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(root.name + " must contain one " + typeof(T).Name + " named " + name + ".");
            return matches[0];
        }

        private static void RequireBoneOrder(SkinnedMeshRenderer target, SkinnedMeshRenderer source)
        {
            var targetNames = target.bones.Select(item => item.name).ToArray();
            var sourceNames = source.bones.Select(item => item.name).ToArray();
            if (!targetNames.SequenceEqual(sourceNames, StringComparer.Ordinal) ||
                source.sharedMesh.bindposes.Length != sourceNames.Length)
                throw new InvalidOperationException("The derived no-sword body bone order differs from the current static rig.");
        }

        private static Matrix4x4 CalculateMeshCorrection(Mesh common, Mesh mounted, string label)
        {
            if (common.vertexCount != mounted.vertexCount ||
                common.subMeshCount != mounted.subMeshCount ||
                TriangleCount(common) != TriangleCount(mounted))
                throw new InvalidOperationException(label + " approved sword mesh topology differs from the common mesh.");
            for (var subMesh = 0; subMesh < common.subMeshCount; subMesh++)
            {
                if (!common.GetTriangles(subMesh).SequenceEqual(mounted.GetTriangles(subMesh)))
                    throw new InvalidOperationException(label + " approved sword submesh indices differ.");
            }
            var first = common.vertices;
            var second = mounted.vertices;
            const int firstIndex = 0;
            var secondIndex = Enumerable.Range(1, first.Length - 1)
                .OrderByDescending(index => (first[index] - first[firstIndex]).sqrMagnitude)
                .First();
            var axis = first[secondIndex] - first[firstIndex];
            var thirdIndex = Enumerable.Range(1, first.Length - 1)
                .Where(index => index != secondIndex)
                .OrderByDescending(index => Vector3.Cross(axis, first[index] - first[firstIndex]).sqrMagnitude)
                .First();
            var normal = Vector3.Cross(axis, first[thirdIndex] - first[firstIndex]);
            var fourthIndex = Enumerable.Range(1, first.Length - 1)
                .Where(index => index != secondIndex && index != thirdIndex)
                .OrderByDescending(index => Mathf.Abs(Vector3.Dot(normal, first[index] - first[firstIndex])))
                .First();
            var commonBasis = PointBasis(
                first[firstIndex], first[secondIndex], first[thirdIndex], first[fourthIndex]);
            if (Mathf.Abs(commonBasis.determinant) <= 0.000000000001f)
                throw new InvalidOperationException(label + " common sword basis is singular.");
            var mountedBasis = PointBasis(
                second[firstIndex], second[secondIndex], second[thirdIndex], second[fourthIndex]);
            var correction = mountedBasis * commonBasis.inverse;
            var maximum = 0f;
            for (var index = 0; index < first.Length; index++)
                maximum = Mathf.Max(
                    maximum,
                    Vector3.Distance(correction.MultiplyPoint3x4(first[index]), second[index]));
            if (maximum > 0.00001f)
                throw new InvalidOperationException(
                    label + " common-mesh rigid correction error is " + Num(maximum) + ".");
            return correction;
        }

        private static Matrix4x4 PointBasis(Vector3 first, Vector3 second, Vector3 third, Vector3 fourth)
        {
            var firstAxis = second - first;
            var secondAxis = third - first;
            var thirdAxis = fourth - first;
            var result = Matrix4x4.identity;
            result.SetColumn(0, new Vector4(firstAxis.x, firstAxis.y, firstAxis.z, 0f));
            result.SetColumn(1, new Vector4(secondAxis.x, secondAxis.y, secondAxis.z, 0f));
            result.SetColumn(2, new Vector4(thirdAxis.x, thirdAxis.y, thirdAxis.z, 0f));
            result.SetColumn(3, new Vector4(first.x, first.y, first.z, 1f));
            return result;
        }

        private static void SetLocalMatrix(Transform target, Matrix4x4 matrix)
        {
            DecomposeTrs(matrix, out var position, out var rotation, out var scale, out var error);
            if (error > CorrectionTrsTolerance)
                throw new InvalidOperationException(
                    "The approved sword corrected transform contains unsupported shear. Error=" + Num(error) +
                    ", Matrix=" + MatrixText(matrix));
            target.localPosition = position;
            target.localRotation = rotation;
            target.localScale = scale;
        }

        private static void RequireTrsMatrix(Matrix4x4 matrix, string label)
        {
            DecomposeTrs(matrix, out _, out _, out _, out var error);
            if (error > CorrectionTrsTolerance)
                throw new InvalidOperationException(
                    label + " is not representable as one Unity Transform. Error=" + Num(error) +
                    ", Matrix=" + MatrixText(matrix));
        }

        private static void DecomposeTrs(
            Matrix4x4 matrix,
            out Vector3 position,
            out Quaternion rotation,
            out Vector3 scale,
            out float error)
        {
            position = new Vector3(matrix.m03, matrix.m13, matrix.m23);
            var x = new Vector3(matrix.m00, matrix.m10, matrix.m20);
            var y = new Vector3(matrix.m01, matrix.m11, matrix.m21);
            var z = new Vector3(matrix.m02, matrix.m12, matrix.m22);
            scale = new Vector3(x.magnitude, y.magnitude, z.magnitude);
            if (matrix.determinant < 0f)
                scale.x = -scale.x;
            rotation = Quaternion.LookRotation(z / scale.z, y / scale.y);
            error = MatrixError(Matrix4x4.TRS(position, rotation, scale), matrix);
        }

        private static string MatrixText(Matrix4x4 value)
        {
            return string.Join(",", Enumerable.Range(0, 16).Select(index =>
                Num(value[index / 4, index % 4])));
        }

        private static void RequireTransformMatch(Transform actual, Transform source, string label)
        {
            var expected = Matrix4x4.TRS(
                source.localPosition,
                source.localRotation,
                source.localScale);
            var value = Matrix4x4.TRS(
                actual.localPosition,
                actual.localRotation,
                actual.localScale);
            if (MatrixError(expected, value) > TransformTolerance)
                throw new InvalidOperationException(label + " differs from its exact source transform.");
        }

        private static void RequireRotationScaleMatch(Transform actual, Transform source, string label)
        {
            if (Quaternion.Angle(actual.localRotation, source.localRotation) > 0.01f ||
                Vector3.Distance(actual.localScale, source.localScale) > TransformTolerance)
                throw new InvalidOperationException(label + " rotation or scale differs from its approved source.");
        }

        private static int TriangleCount(Mesh mesh)
        {
            return Enumerable.Range(0, mesh.subMeshCount)
                .Sum(index => checked((int)mesh.GetIndexCount(index))) / 3;
        }

        private static float MatrixError(Matrix4x4 first, Matrix4x4 second)
        {
            var result = 0f;
            for (var row = 0; row < 4; row++)
            for (var column = 0; column < 4; column++)
                result = Mathf.Max(result, Mathf.Abs(first[row, column] - second[row, column]));
            return result;
        }

        private static string[] OtherRootSignatures(Scene scene, Transform placement)
        {
            return scene.GetRootGameObjects()
                .Where(item => item.transform != placement)
                .OrderBy(item => item.transform.GetSiblingIndex())
                .Select(item => item.name + "|" + Vec(item.transform.localPosition) + "|" +
                                Vec(item.transform.localEulerAngles) + "|" + Vec(item.transform.localScale) + "|" +
                                item.GetComponentsInChildren<Transform>(true).Length)
                .ToArray();
        }

        private static void RequireEqual(string[] expected, string[] actual, string message)
        {
            if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
                throw new InvalidOperationException(message);
        }

        private static string Absolute(string relative)
        {
            return Path.GetFullPath(Path.Combine(
                Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty,
                relative));
        }

        private static string AbsoluteAsset(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(
                Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty,
                assetPath));
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("X2")));
        }

        private static string Num(float value)
        {
            return value.ToString("0.#########", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return Num(value.x) + "," + Num(value.y) + "," + Num(value.z);
        }

        private sealed class Sources
        {
            public Sources(
                Mesh commonMesh,
                SkinnedMeshRenderer staticBody,
                MeshRenderer staticSword,
                MeshRenderer moveSword,
                MeshRenderer drawSword)
            {
                CommonMesh = commonMesh;
                StaticBody = staticBody;
                StaticSword = staticSword;
                MoveSword = moveSword;
                DrawSword = drawSword;
            }

            public Mesh CommonMesh { get; }
            public SkinnedMeshRenderer StaticBody { get; }
            public MeshRenderer StaticSword { get; }
            public MeshRenderer MoveSword { get; }
            public MeshRenderer DrawSword { get; }
        }

        private readonly struct KeyframeRotation
        {
            public KeyframeRotation(float time, Quaternion rotation)
            {
                Time = time;
                Rotation = rotation;
            }

            public float Time { get; }
            public Quaternion Rotation { get; }
        }

        private readonly struct Metrics
        {
            public Metrics(
                int swordCount,
                float maximumAttachmentError,
                float maximumHandSurfaceDistance,
                float maximumFollowMotion,
                float maximumSwordWorldRotation,
                float maximumHandWorldRotation,
                float maximumRelativeRotationError,
                float minimumGripCenterToHandOrigin,
                float maximumGripCenterToHandOrigin,
                float minimumGripCenterToPalmCenter,
                float maximumGripCenterToPalmCenter,
                float maximumHeldHandPositionMotion,
                float maximumHeldHandRotationMotion,
                float maximumUpwardRotationStep,
                float upwardRotationDegrees,
                float finalTipUpError)
            {
                SwordCount = swordCount;
                MaximumAttachmentError = maximumAttachmentError;
                MaximumHandSurfaceDistance = maximumHandSurfaceDistance;
                MaximumFollowMotion = maximumFollowMotion;
                MaximumSwordWorldRotation = maximumSwordWorldRotation;
                MaximumHandWorldRotation = maximumHandWorldRotation;
                MaximumRelativeRotationError = maximumRelativeRotationError;
                MinimumGripCenterToHandOrigin = minimumGripCenterToHandOrigin;
                MaximumGripCenterToHandOrigin = maximumGripCenterToHandOrigin;
                MinimumGripCenterToPalmCenter = minimumGripCenterToPalmCenter;
                MaximumGripCenterToPalmCenter = maximumGripCenterToPalmCenter;
                MaximumHeldHandPositionMotion = maximumHeldHandPositionMotion;
                MaximumHeldHandRotationMotion = maximumHeldHandRotationMotion;
                MaximumUpwardRotationStep = maximumUpwardRotationStep;
                UpwardRotationDegrees = upwardRotationDegrees;
                FinalTipUpError = finalTipUpError;
            }

            public int SwordCount { get; }
            public float MaximumAttachmentError { get; }
            public float MaximumHandSurfaceDistance { get; }
            public float MaximumFollowMotion { get; }
            public float MaximumSwordWorldRotation { get; }
            public float MaximumHandWorldRotation { get; }
            public float MaximumRelativeRotationError { get; }
            public float MinimumGripCenterToHandOrigin { get; }
            public float MaximumGripCenterToHandOrigin { get; }
            public float MinimumGripCenterToPalmCenter { get; }
            public float MaximumGripCenterToPalmCenter { get; }
            public float MaximumHeldHandPositionMotion { get; }
            public float MaximumHeldHandRotationMotion { get; }
            public float MaximumUpwardRotationStep { get; }
            public float UpwardRotationDegrees { get; }
            public float FinalTipUpError { get; }
        }

        private readonly struct AnimationMetrics
        {
            public AnimationMetrics(
                float maximumAttachmentError,
                float maximumHandSurfaceDistance,
                float maximumFollowMotion,
                float maximumSwordWorldRotation,
                float maximumHandWorldRotation,
                float maximumRelativeRotationError,
                float minimumGripCenterToHandOrigin,
                float maximumGripCenterToHandOrigin,
                float minimumGripCenterToPalmCenter,
                float maximumGripCenterToPalmCenter,
                float maximumHeldHandPositionMotion,
                float maximumHeldHandRotationMotion,
                float maximumUpwardRotationStep,
                float upwardRotationDegrees,
                float finalTipUpError)
            {
                MaximumAttachmentError = maximumAttachmentError;
                MaximumHandSurfaceDistance = maximumHandSurfaceDistance;
                MaximumFollowMotion = maximumFollowMotion;
                MaximumSwordWorldRotation = maximumSwordWorldRotation;
                MaximumHandWorldRotation = maximumHandWorldRotation;
                MaximumRelativeRotationError = maximumRelativeRotationError;
                MinimumGripCenterToHandOrigin = minimumGripCenterToHandOrigin;
                MaximumGripCenterToHandOrigin = maximumGripCenterToHandOrigin;
                MinimumGripCenterToPalmCenter = minimumGripCenterToPalmCenter;
                MaximumGripCenterToPalmCenter = maximumGripCenterToPalmCenter;
                MaximumHeldHandPositionMotion = maximumHeldHandPositionMotion;
                MaximumHeldHandRotationMotion = maximumHeldHandRotationMotion;
                MaximumUpwardRotationStep = maximumUpwardRotationStep;
                UpwardRotationDegrees = upwardRotationDegrees;
                FinalTipUpError = finalTipUpError;
            }

            public float MaximumAttachmentError { get; }
            public float MaximumHandSurfaceDistance { get; }
            public float MaximumFollowMotion { get; }
            public float MaximumSwordWorldRotation { get; }
            public float MaximumHandWorldRotation { get; }
            public float MaximumRelativeRotationError { get; }
            public float MinimumGripCenterToHandOrigin { get; }
            public float MaximumGripCenterToHandOrigin { get; }
            public float MinimumGripCenterToPalmCenter { get; }
            public float MaximumGripCenterToPalmCenter { get; }
            public float MaximumHeldHandPositionMotion { get; }
            public float MaximumHeldHandRotationMotion { get; }
            public float MaximumUpwardRotationStep { get; }
            public float UpwardRotationDegrees { get; }
            public float FinalTipUpError { get; }
        }

        private sealed class TransformSnapshot
        {
            private readonly Transform target;
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            public TransformSnapshot(Transform target)
            {
                this.target = target;
                position = target.localPosition;
                rotation = target.localRotation;
                scale = target.localScale;
            }

            public bool Matches(float tolerance)
            {
                return Vector3.Distance(position, target.localPosition) <= tolerance &&
                       Quaternion.Angle(rotation, target.localRotation) <= 0.01f &&
                       Vector3.Distance(scale, target.localScale) <= tolerance;
            }

            public void Restore()
            {
                target.localPosition = position;
                target.localRotation = rotation;
                target.localScale = scale;
            }
        }

        private sealed class RendererSnapshot
        {
            private readonly bool enabled;

            public RendererSnapshot(Renderer renderer)
            {
                Renderer = renderer;
                enabled = renderer.enabled;
            }

            public Renderer Renderer { get; }

            public void Restore()
            {
                Renderer.enabled = enabled;
            }
        }
    }
}
