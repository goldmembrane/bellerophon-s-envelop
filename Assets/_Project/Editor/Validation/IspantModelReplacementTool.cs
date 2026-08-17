using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
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
        private const string BaselinePath =
            "docs/validation/ispant_model_replacement_2026-08-17/Ispant_Model_Replacement_Baseline.txt";
        private const string CustomRigPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_CustomRig.fbx";
        private const string MixamoRigPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_MixamoRig.fbx";
        private const string DeathRigPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_DeathRig.fbx";
        private const string CustomReferencePath =
            "Assets/_Project/Art/Enemies/Ispant/ApprovedLongSword/Models/Ispant_ApprovedLongSword_StaticMount.fbx";
        private const string DeathReferencePath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_Death.fbx";
        private const string CustomMappedMeshPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Generated/Ispant_New_CustomRig_Mesh.asset";
        private const string DeathMappedMeshPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Generated/Ispant_New_DeathRig_Mesh.asset";
        private const string GeneratedInspectionPath =
            "docs/validation/ispant_model_replacement_2026-08-17/Ispant_New_Rigged_Models_Inspection.txt";
        private const string SourceCopyPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_Source.fbx";
        private const string MaterialPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Materials/Ispant_New_Model.mat";
        private const string ShaderPath =
            "Assets/_Project/Art/Enemies/Ispant/ApprovedAppearance/Shaders/IspantApprovedAppearance.shader";
        private const string TextureRoot =
            "Assets/_Project/Art/Enemies/Ispant/Models/Textures/";
        private const string FinalInspectionPath =
            "docs/validation/ispant_model_replacement_2026-08-17/Ispant_Model_Replacement_Inspection.txt";
        private const string ScaleDiagnosticPath =
            "docs/validation/ispant_model_replacement_2026-08-17/Ispant_Model_Scale_Diagnostic.txt";
        private const string PlaybackInspectionPath =
            "docs/validation/ispant_model_replacement_2026-08-17/Ispant_Model_Playback_Inspection.txt";
        private const string ExpectedSourceSha256 =
            "EEAF6B319DBF561E562DB8C8CDF6C4797D7F659620EAC9491E3EDFA490649EED";

        [MenuItem("Bellerophon/Enemies/Ispant/Apply New Model Replacement")]
        public static void ApplyNewModelReplacement()
        {
            var scene = RequireScene();
            if (scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp has unsaved changes before Ispant model replacement.");
            RequireHash(SourceCopyPath, ExpectedSourceSha256);
            var placement = GameObject.Find(PlacementRootName) ??
                throw new InvalidOperationException("The Ispant placement root is missing.");
            if (placement.transform.childCount != 12)
                throw new InvalidOperationException("Expected 12 placed Ispant objects.");

            var protectedBefore = ProtectedContract(placement.transform);
            var material = CreateOrUpdateMaterial();
            var customSource = RequireGeneratedRenderer(CustomRigPath);
            var mixamoSource = RequireGeneratedRenderer(MixamoRigPath);
            var deathSource = RequireGeneratedRenderer(DeathRigPath);
            var customMesh = CreateOrUpdateMappedBindposeMesh(
                customSource,
                CustomReferencePath,
                CustomMappedMeshPath,
                "Ispant_New_CustomRig_Mesh");
            var deathMesh = CreateOrUpdateMappedBindposeMesh(
                deathSource,
                DeathReferencePath,
                DeathMappedMeshPath,
                "Ispant_New_DeathRig_Mesh");
            var customCount = 0;
            var mixamoCount = 0;
            var deathCount = 0;

            try
            {
                foreach (Transform slot in placement.transform)
                {
                    var bodies = slot.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                        .Where(renderer => renderer.name == "Ispant_Armed_Body")
                        .ToArray();
                    if (bodies.Length != 1)
                        throw new InvalidOperationException(slot.name + " does not contain one Ispant body renderer.");
                    var body = bodies[0];
                    var isMixamo = body.bones.Any(bone => bone != null && bone.name.StartsWith("mixamorig:", StringComparison.Ordinal));
                    var isDeath = string.Equals(slot.name, "Ispant_12_Death", StringComparison.Ordinal);
                    var source = isDeath ? deathSource : isMixamo ? mixamoSource : customSource;
                    var mesh = isDeath ? deathMesh : isMixamo ? mixamoSource.sharedMesh : customMesh;
                    ApplyBody(slot, body, source, mesh, material);
                    if (isDeath)
                        deathCount++;
                    else if (isMixamo)
                        mixamoCount++;
                    else
                        customCount++;

                    foreach (var renderer in slot.GetComponentsInChildren<Renderer>(true))
                    {
                        if (renderer == body)
                            continue;
                        if (renderer.name == "Ispant_Crescent_Ornament" ||
                            renderer.name == "Ispant_Reference_Eye_Slits")
                        {
                            renderer.enabled = false;
                            EditorUtility.SetDirty(renderer);
                        }
                    }
                }

                var protectedAfter = ProtectedContract(placement.transform);
                if (!protectedBefore.SequenceEqual(protectedAfter, StringComparer.Ordinal))
                    throw new InvalidOperationException("An Animator, slot transform, weapon, physics, collider, or motion driver changed.");
                var inspection = InspectReplacement(
                    placement.transform,
                    material,
                    customMesh,
                    mixamoSource.sharedMesh,
                    deathMesh);
                WriteText(FinalInspectionPath, inspection);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new InvalidOperationException("CargoRunMvp could not be saved after Ispant model replacement.");
                AssetDatabase.SaveAssets();
            }
            catch
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                throw;
            }

            Debug.Log(
                "IspantNewModelReplacementApplied Result=PASS" +
                ", Slots=12, CustomRigSlots=" + customCount + ", MixamoRigSlots=" + mixamoCount +
                ", DeathRigSlots=" + deathCount +
                ", SourceTriangles=10028, UsedBodyBones=22" +
                ", ExistingAnimatorsPreserved=True, ExistingWeaponPathsPreserved=True" +
                ", ExistingPhysicsPreserved=True, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect New Model Replacement")]
        public static void InspectNewModelReplacement()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Unity Play Mode has not stopped after Ispant playback inspection.");
            var scene = RequireScene();
            var wasDirty = scene.isDirty;
            var placement = GameObject.Find(PlacementRootName) ??
                throw new InvalidOperationException("The Ispant placement root is missing.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) ??
                throw new InvalidOperationException("The Ispant replacement material is missing.");
            var mixamo = RequireGeneratedRenderer(MixamoRigPath);
            var customMesh = AssetDatabase.LoadAssetAtPath<Mesh>(CustomMappedMeshPath) ??
                throw new InvalidOperationException("The mapped custom Ispant mesh is missing.");
            var deathMesh = AssetDatabase.LoadAssetAtPath<Mesh>(DeathMappedMeshPath) ??
                throw new InvalidOperationException("The mapped death Ispant mesh is missing.");
            var inspection = InspectReplacement(
                placement.transform,
                material,
                customMesh,
                mixamo.sharedMesh,
                deathMesh);
            WriteText(FinalInspectionPath, inspection);
            RequireHash(SourceCopyPath, ExpectedSourceSha256);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Ispant replacement inspection changed the scene dirty state.");
            Debug.Log(
                "IspantNewModelReplacementInspected Result=PASS" +
                ", Slots=12, NewBodyMeshes=12, UsedBodyBones=22" +
                ", OldAppearanceRenderersDisabled=24, ExistingAnimatorsPreserved=True" +
                ", ExistingWeaponPathsPreserved=True, SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Model Scale Diagnostic")]
        public static void InspectModelScaleDiagnostic()
        {
            var scene = RequireScene();
            var wasDirty = scene.isDirty;
            var placement = GameObject.Find(PlacementRootName) ??
                throw new InvalidOperationException("The Ispant placement root is missing.");
            var references = new[]
            {
                "Assets/_Project/Art/Enemies/Ispant/ApprovedLongSword/Models/Ispant_ApprovedLongSword_StaticMount.fbx",
                "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_Move.fbx",
                "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_DrawSword.fbx",
                "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_RunningSwordAttack.fbx",
                "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_SheathSword.fbx",
                "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_Death.fbx",
            };
            var report = new StringBuilder();
            report.AppendLine("Ispant model scale diagnostic");
            foreach (var path in references)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path) ??
                    throw new InvalidOperationException("Reference FBX is missing: " + path);
                var renderer = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .First(value => value.name == "Ispant_Armed_Body");
                report.AppendLine(
                    "Reference=" + path + "|MeshBounds=" + renderer.sharedMesh.bounds.size.ToString("F6") +
                    "|RendererLocalScale=" + renderer.transform.localScale.ToString("F6") +
                    "|RendererLossyScale=" + renderer.transform.lossyScale.ToString("F6"));
            }
            foreach (Transform slot in placement.transform)
            {
                var body = slot.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Single(renderer => renderer.name == "Ispant_Armed_Body");
                report.AppendLine(
                    "Slot=" + slot.name + "|MeshBounds=" + body.sharedMesh.bounds.size.ToString("F6") +
                    "|RendererLocalScale=" + body.transform.localScale.ToString("F6") +
                    "|RendererLossyScale=" + body.transform.lossyScale.ToString("F6") +
                    "|WorldBounds=" + body.bounds.size.ToString("F6"));
            }
            WriteText(ScaleDiagnosticPath, report.ToString());
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Scale diagnostic changed the scene dirty state.");
            Debug.Log("IspantModelScaleDiagnosticInspected Result=PASS, SceneChanged=False, Report=" + ScaleDiagnosticPath + ".");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Start New Model Playback")]
        public static void StartNewModelPlayback()
        {
            RequireScene();
            if (!EditorApplication.isPlaying)
                EditorApplication.EnterPlaymode();
            Debug.Log("IspantNewModelPlaybackStarted Result=PASS.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect New Model Playback")]
        public static void InspectNewModelPlayback()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Unity is not in Play Mode for Ispant playback inspection.");
            var placement = GameObject.Find(PlacementRootName) ??
                throw new InvalidOperationException("The Ispant placement root is missing in Play Mode.");
            var report = new StringBuilder();
            report.AppendLine("Ispant new model actual playback inspection");
            var animatedSlots = 0;
            var bakedMeshes = 0;
            foreach (Transform slot in placement.transform)
            {
                var body = slot.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Single(renderer => renderer.name == "Ispant_Armed_Body");
                if (!body.enabled || body.sharedMesh == null || body.bones.Any(bone => bone == null))
                    throw new InvalidOperationException(slot.name + " has an invalid replacement renderer during playback.");
                var baked = new Mesh();
                try
                {
                    body.BakeMesh(baked, true);
                    if (baked.vertexCount != body.sharedMesh.vertexCount ||
                        !Finite(baked.bounds.center) || !Finite(baked.bounds.size))
                        throw new InvalidOperationException(slot.name + " produced an invalid skinned playback mesh.");
                    bakedMeshes++;
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(baked);
                }
                var worldSize = body.bounds.size;
                if (!Finite(worldSize) || worldSize.x <= 0.2f || worldSize.y <= 0.2f || worldSize.z <= 0.1f ||
                    worldSize.x >= 4f || worldSize.y >= 4f || worldSize.z >= 4f)
                    throw new InvalidOperationException(slot.name + " playback bounds are invalid: " + worldSize + ".");
                var animator = slot.GetComponentInChildren<Animator>(true);
                if (animator != null)
                {
                    if (!animator.enabled || !animator.isInitialized || animator.layerCount < 1)
                        throw new InvalidOperationException(slot.name + " Animator is not initialized during playback.");
                    var state = animator.GetCurrentAnimatorStateInfo(0);
                    if (state.length <= 0f || animator.GetCurrentAnimatorClipInfo(0).Length < 1)
                        throw new InvalidOperationException(slot.name + " has no active animation clip during playback.");
                    animatedSlots++;
                    report.AppendLine(
                        "Slot=" + slot.name + "|NormalizedTime=" + state.normalizedTime.ToString("F4") +
                        "|Length=" + state.length.ToString("F4") + "|WorldBounds=" + worldSize.ToString("F4"));
                }
                else
                {
                    report.AppendLine("Slot=" + slot.name + "|Static=True|WorldBounds=" + worldSize.ToString("F4"));
                }
            }
            if (animatedSlots != 11 || bakedMeshes != 12)
                throw new InvalidOperationException(
                    "Unexpected playback totals: animatedSlots=" + animatedSlots + ", bakedMeshes=" + bakedMeshes + ".");
            var absolute = Path.GetFullPath(Path.Combine(Application.dataPath, "..", PlaybackInspectionPath));
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, report.ToString(), new UTF8Encoding(false));
            Debug.Log(
                "IspantNewModelPlaybackInspected Result=PASS" +
                ", AnimatedSlots=11, BakedMeshes=12, FiniteSkinning=True, PlaybackBoundsValid=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Stop New Model Playback")]
        public static void StopNewModelPlayback()
        {
            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
            Debug.Log("IspantNewModelPlaybackStopRequested Result=PASS.");
        }

        private static bool Finite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
                   !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
                   !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Generated Rigged Models")]
        public static void InspectGeneratedRiggedModels()
        {
            var report = new StringBuilder();
            report.AppendLine("Ispant generated rigged model inspection");
            AppendGeneratedModel(report, CustomRigPath, 22);
            AppendGeneratedModel(report, MixamoRigPath, 22);
            AppendGeneratedModel(report, DeathRigPath, 22);
            var absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", GeneratedInspectionPath));
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            File.WriteAllText(absolutePath, report.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log(
                "IspantGeneratedRiggedModelsInspected Result=PASS" +
                ", CustomUsedBones=22, MixamoUsedBones=22, DeathUsedBones=22, SourceVertices=4980" +
                ", Report=" + GeneratedInspectionPath + ".");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Model Replacement Baseline")]
        public static void InspectModelReplacementBaseline()
        {
            var scene = RequireScene();
            var wasDirty = scene.isDirty;
            var placement = GameObject.Find(PlacementRootName) ??
                throw new InvalidOperationException("The Ispant placement root is missing.");
            var report = new StringBuilder();
            report.AppendLine("Ispant model replacement baseline");
            report.AppendLine("Scene=" + scene.path);
            report.AppendLine("Placement=" + PlacementRootName);
            report.AppendLine("Slots=" + placement.transform.childCount);

            for (var slotIndex = 0; slotIndex < placement.transform.childCount; slotIndex++)
            {
                var slot = placement.transform.GetChild(slotIndex);
                report.AppendLine();
                report.AppendLine("[Slot " + slotIndex + "] " + slot.name);
                report.AppendLine("SlotTransform=" + TransformValue(slot));
                report.AppendLine("Prefab=" + PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(slot.gameObject));

                foreach (var animator in slot.GetComponentsInChildren<Animator>(true))
                {
                    report.AppendLine(
                        "Animator=" + RelativePath(slot, animator.transform) +
                        "|Controller=" + AssetDatabase.GetAssetPath(animator.runtimeAnimatorController) +
                        "|Avatar=" + AssetDatabase.GetAssetPath(animator.avatar) +
                        "|ApplyRootMotion=" + animator.applyRootMotion +
                        "|Enabled=" + animator.enabled);
                }

                foreach (var renderer in slot.GetComponentsInChildren<Renderer>(true))
                {
                    var mesh = RendererMesh(renderer);
                    report.AppendLine(
                        "Renderer=" + RelativePath(slot, renderer.transform) +
                        "|Type=" + renderer.GetType().Name +
                        "|Enabled=" + renderer.enabled +
                        "|Mesh=" + (mesh != null ? mesh.name : "<none>") +
                        "|MeshAsset=" + AssetDatabase.GetAssetPath(mesh) +
                        "|Vertices=" + (mesh != null ? mesh.vertexCount : 0) +
                        "|Materials=" + string.Join(",", renderer.sharedMaterials.Select(MaterialValue)) +
                        SkinnedValue(renderer as SkinnedMeshRenderer));
                }

                foreach (var rigidbody in slot.GetComponentsInChildren<Rigidbody>(true))
                {
                    report.AppendLine(
                        "Rigidbody=" + RelativePath(slot, rigidbody.transform) +
                        "|Kinematic=" + rigidbody.isKinematic +
                        "|UseGravity=" + rigidbody.useGravity);
                }

                foreach (var collider in slot.GetComponentsInChildren<Collider>(true))
                {
                    report.AppendLine(
                        "Collider=" + RelativePath(slot, collider.transform) +
                        "|Type=" + collider.GetType().Name +
                        "|Enabled=" + collider.enabled +
                        "|Trigger=" + collider.isTrigger);
                }

                foreach (var behaviour in slot.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null)
                        continue;
                    report.AppendLine(
                        "Behaviour=" + RelativePath(slot, behaviour.transform) +
                        "|Type=" + behaviour.GetType().FullName +
                        "|Enabled=" + behaviour.enabled);
                }
            }

            var absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", BaselinePath));
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            File.WriteAllText(absolutePath, report.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Baseline inspection changed the scene dirty state.");
            Debug.Log(
                "IspantModelReplacementBaselineInspected Result=PASS" +
                ", Slots=" + placement.transform.childCount +
                ", SceneChanged=False, Report=" + BaselinePath + ".");
        }

        private static Scene RequireScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("CargoRunMvp is not loaded.");
            return scene;
        }

        private static SkinnedMeshRenderer RequireGeneratedRenderer(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path) ??
                throw new InvalidOperationException("Generated Ispant FBX is missing: " + path);
            var renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length != 1 || renderers[0].sharedMesh == null)
                throw new InvalidOperationException(path + " does not contain one generated skinned mesh.");
            return renderers[0];
        }

        private static void ApplyBody(
            Transform slot,
            SkinnedMeshRenderer target,
            SkinnedMeshRenderer source,
            Mesh mesh,
            Material material)
        {
            var transforms = slot.GetComponentsInChildren<Transform>(true)
                .GroupBy(value => value.name)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var usedIndices = source.sharedMesh.boneWeights
                .SelectMany(weight => new[] { weight.boneIndex0, weight.boneIndex1, weight.boneIndex2, weight.boneIndex3 })
                .Distinct()
                .ToHashSet();
            var mapped = new Transform[source.bones.Length];
            for (var index = 0; index < source.bones.Length; index++)
            {
                var sourceBone = source.bones[index];
                if (sourceBone != null && transforms.TryGetValue(sourceBone.name, out var targetBone))
                    mapped[index] = targetBone;
                else if (usedIndices.Contains(index))
                    throw new InvalidOperationException(slot.name + " is missing used bone " + sourceBone?.name + ".");
                else
                    mapped[index] = target.rootBone;
            }
            if (source.rootBone == null || !transforms.TryGetValue(source.rootBone.name, out var rootBone))
                throw new InvalidOperationException(slot.name + " cannot map the generated root bone.");
            target.sharedMesh = mesh;
            target.bones = mapped;
            target.rootBone = rootBone;
            target.sharedMaterials = new[] { material };
            target.localBounds = mesh.bounds;
            target.enabled = true;
            EditorUtility.SetDirty(target);
        }

        private static Mesh CreateOrUpdateMappedBindposeMesh(
            SkinnedMeshRenderer source,
            string referencePath,
            string assetPath,
            string meshName)
        {
            var referencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(referencePath) ??
                throw new InvalidOperationException("The Ispant bind-pose reference is missing: " + referencePath);
            var reference = referencePrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(renderer => renderer.name == "Ispant_Armed_Body");
            var referenceByName = reference.bones
                .Select((bone, index) => new { bone, index })
                .Where(value => value.bone != null)
                .ToDictionary(value => value.bone.name, value => value.index, StringComparer.Ordinal);
            var usedIndices = source.sharedMesh.boneWeights
                .SelectMany(weight => new[] { weight.boneIndex0, weight.boneIndex1, weight.boneIndex2, weight.boneIndex3 })
                .Distinct()
                .ToHashSet();
            var sourceBindposes = source.sharedMesh.bindposes;
            var referenceBindposes = reference.sharedMesh.bindposes;
            var mappedBindposes = new Matrix4x4[source.bones.Length];
            for (var index = 0; index < source.bones.Length; index++)
            {
                var sourceBone = source.bones[index];
                if (sourceBone != null && referenceByName.TryGetValue(sourceBone.name, out var referenceIndex))
                    mappedBindposes[index] = referenceBindposes[referenceIndex];
                else if (usedIndices.Contains(index))
                    throw new InvalidOperationException(
                        referencePath + " is missing used generated bone " + sourceBone?.name + ".");
                else
                    mappedBindposes[index] = index < sourceBindposes.Length ? sourceBindposes[index] : Matrix4x4.identity;
            }

            EnsureAssetFolder(Path.GetDirectoryName(assetPath).Replace('\\', '/'));
            var target = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (target == null)
            {
                target = new Mesh();
                EditorUtility.CopySerialized(source.sharedMesh, target);
                target.name = meshName;
                target.bindposes = mappedBindposes;
                AssetDatabase.CreateAsset(target, assetPath);
            }
            else
            {
                EditorUtility.CopySerialized(source.sharedMesh, target);
                target.name = meshName;
                target.bindposes = mappedBindposes;
                EditorUtility.SetDirty(target);
            }
            return target;
        }

        private static Material CreateOrUpdateMaterial()
        {
            ConfigureTexture(TextureRoot + "Ispant_New_BaseColor.jpg", false, true);
            ConfigureTexture(TextureRoot + "Ispant_New_Normal.jpg", true, false);
            ConfigureTexture(TextureRoot + "Ispant_New_Metallic.png", false, false);
            ConfigureTexture(TextureRoot + "Ispant_New_Roughness.png", false, false);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath) ??
                throw new InvalidOperationException("The approved Ispant shader is missing.");
            EnsureAssetFolder("Assets/_Project/Art/Enemies/Ispant/Models/Materials");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "Ispant_New_Model" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.shader = shader;
            material.SetTexture("_BaseMap", RequireTexture(TextureRoot + "Ispant_New_BaseColor.jpg"));
            material.SetTexture("_NormalMap", RequireTexture(TextureRoot + "Ispant_New_Normal.jpg"));
            material.SetTexture("_MetallicMap", RequireTexture(TextureRoot + "Ispant_New_Metallic.png"));
            material.SetTexture("_RoughnessMap", RequireTexture(TextureRoot + "Ispant_New_Roughness.png"));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_NormalStrength", 1f);
            material.SetFloat("_UseMaps", 1f);
            material.SetFloat("_UseUv1", 0f);
            material.SetFloat("_RoughnessBias", 0f);
            material.SetFloat("_MetallicBias", 0f);
            material.SetFloat("_CoatWeight", 0f);
            material.SetFloat("_FeatureMode", 0f);
            material.SetFloat("_ApprovedYFlip", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureTexture(string path, bool normalMap, bool srgb)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter ??
                throw new InvalidOperationException("Ispant replacement texture is missing: " + path);
            var changed = importer.textureType != (normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default) ||
                          importer.sRGBTexture != srgb;
            importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = srgb;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            if (changed)
                importer.SaveAndReimport();
        }

        private static Texture2D RequireTexture(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path) ??
                throw new InvalidOperationException("Ispant replacement texture could not be loaded: " + path);
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

        private static string[] ProtectedContract(Transform placement)
        {
            var values = new System.Collections.Generic.List<string>();
            foreach (Transform slot in placement)
            {
                values.Add("Slot|" + slot.name + "|" + TransformValue(slot));
                foreach (var animator in slot.GetComponentsInChildren<Animator>(true))
                    values.Add("Animator|" + slot.name + "|" + RelativePath(slot, animator.transform) + "|" +
                               AssetDatabase.GetAssetPath(animator.runtimeAnimatorController) + "|" +
                               AssetDatabase.GetAssetPath(animator.avatar) + "|" + animator.applyRootMotion + "|" + animator.enabled);
                foreach (var renderer in slot.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.name == "Ispant_Armed_Body" || renderer.name == "Ispant_Crescent_Ornament" ||
                        renderer.name == "Ispant_Reference_Eye_Slits")
                        continue;
                    values.Add("PreservedRenderer|" + slot.name + "|" + RelativePath(slot, renderer.transform) + "|" +
                               renderer.enabled + "|" + AssetDatabase.GetAssetPath(RendererMesh(renderer)) + "|" +
                               string.Join(",", renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath)));
                }
                foreach (var rigidbody in slot.GetComponentsInChildren<Rigidbody>(true))
                    values.Add("Rigidbody|" + slot.name + "|" + RelativePath(slot, rigidbody.transform) + "|" +
                               rigidbody.isKinematic + "|" + rigidbody.useGravity);
                foreach (var collider in slot.GetComponentsInChildren<Collider>(true))
                    values.Add("Collider|" + slot.name + "|" + RelativePath(slot, collider.transform) + "|" +
                               collider.GetType().FullName + "|" + collider.enabled + "|" + collider.isTrigger);
                foreach (var behaviour in slot.GetComponentsInChildren<MonoBehaviour>(true))
                    if (behaviour != null)
                        values.Add("Behaviour|" + slot.name + "|" + RelativePath(slot, behaviour.transform) + "|" +
                                   behaviour.GetType().FullName + "|" + behaviour.enabled);
            }
            values.Sort(StringComparer.Ordinal);
            return values.ToArray();
        }

        private static string InspectReplacement(
            Transform placement,
            Material material,
            Mesh customMesh,
            Mesh mixamoMesh,
            Mesh deathMesh)
        {
            if (placement.childCount != 12)
                throw new InvalidOperationException("Expected 12 Ispant slots.");
            var report = new StringBuilder();
            report.AppendLine("Ispant model replacement inspection");
            report.AppendLine("SourceSha256=" + ExpectedSourceSha256);
            report.AppendLine("Material=" + AssetDatabase.GetAssetPath(material));
            report.AppendLine("Shader=" + material.shader.name);
            var customCount = 0;
            var mixamoCount = 0;
            var deathCount = 0;
            var disabledOldAppearance = 0;
            foreach (Transform slot in placement)
            {
                var body = slot.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Single(renderer => renderer.name == "Ispant_Armed_Body");
                var isDeath = string.Equals(slot.name, "Ispant_12_Death", StringComparison.Ordinal);
                var expected = isDeath
                    ? deathMesh
                    : body.bones.Any(bone => bone != null && bone.name.StartsWith("mixamorig:", StringComparison.Ordinal))
                        ? mixamoMesh
                        : customMesh;
                if (body.sharedMesh != expected || body.sharedMaterials.Length != 1 || body.sharedMaterial != material ||
                    body.bones.Any(bone => bone == null))
                    throw new InvalidOperationException(slot.name + " replacement skin contract failed.");
                if (expected == deathMesh) deathCount++;
                else if (expected == mixamoMesh) mixamoCount++;
                else customCount++;
                var worldSize = body.bounds.size;
                if (worldSize.x <= 0.35f || worldSize.y <= 0.5f || worldSize.z <= 0.2f ||
                    worldSize.x >= 3f || worldSize.y >= 3f || worldSize.z >= 3f)
                    throw new InvalidOperationException(
                        slot.name + " replacement world bounds are invalid: " + worldSize + ".");
                var oldAppearance = slot.GetComponentsInChildren<Renderer>(true)
                    .Where(renderer => renderer.name == "Ispant_Crescent_Ornament" ||
                                       renderer.name == "Ispant_Reference_Eye_Slits")
                    .ToArray();
                if (oldAppearance.Any(renderer => renderer.enabled))
                    throw new InvalidOperationException(slot.name + " still enables an old appearance renderer.");
                disabledOldAppearance += oldAppearance.Length;
                report.AppendLine(
                    "Slot=" + slot.name + "|Mesh=" + AssetDatabase.GetAssetPath(body.sharedMesh) +
                    "|Bones=" + body.bones.Length + "|Bounds=" + body.bounds.size.ToString("F4"));
            }
            if (customCount != 3 || mixamoCount != 8 || deathCount != 1 || disabledOldAppearance != 24)
                throw new InvalidOperationException(
                    "Unexpected replacement totals: custom=" + customCount + ", mixamo=" + mixamoCount +
                    ", death=" + deathCount + ", disabledOldAppearance=" + disabledOldAppearance + ".");
            report.AppendLine("CustomRigSlots=" + customCount);
            report.AppendLine("MixamoRigSlots=" + mixamoCount);
            report.AppendLine("DeathRigSlots=" + deathCount);
            report.AppendLine("DisabledOldAppearanceRenderers=" + disabledOldAppearance);
            report.AppendLine("Result=PASS");
            return report.ToString();
        }

        private static void RequireHash(string assetPath, string expected)
        {
            var absolute = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            using var stream = File.OpenRead(absolute);
            using var sha = SHA256.Create();
            var actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(assetPath + " hash differs from the supplied FBX.");
        }

        private static void WriteText(string assetPath, string contents)
        {
            var absolute = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, contents, new UTF8Encoding(false));
            AssetDatabase.Refresh();
        }

        private static Mesh RendererMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned)
                return skinned.sharedMesh;
            return renderer.GetComponent<MeshFilter>()?.sharedMesh;
        }

        private static string SkinnedValue(SkinnedMeshRenderer renderer)
        {
            if (renderer == null)
                return string.Empty;
            return "|RootBone=" + (renderer.rootBone != null ? renderer.rootBone.name : "<none>") +
                   "|Bones=" + string.Join(",", renderer.bones.Select(bone => bone != null ? bone.name : "<null>"));
        }

        private static string MaterialValue(Material material)
        {
            return material == null
                ? "<null>"
                : material.name + "@" + AssetDatabase.GetAssetPath(material);
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

        private static void AppendGeneratedModel(StringBuilder report, string path, int expectedBones)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path) ??
                throw new InvalidOperationException("Generated Ispant FBX is missing: " + path);
            var renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length != 1)
                throw new InvalidOperationException(path + " must contain exactly one skinned renderer.");
            var renderer = renderers[0];
            var mesh = renderer.sharedMesh ??
                throw new InvalidOperationException(path + " has no shared mesh.");
            if (mesh.vertexCount < 4980)
                throw new InvalidOperationException(
                    path + " has fewer Unity vertices than the supplied FBX: " + mesh.vertexCount + ".");
            var triangleCount = mesh.triangles.Length / 3;
            if (triangleCount != 10028)
                throw new InvalidOperationException(
                    path + " triangle count differs from the supplied FBX: " + triangleCount + ".");
            var usedIndices = mesh.boneWeights
                .SelectMany(weight => new[] { weight.boneIndex0, weight.boneIndex1, weight.boneIndex2, weight.boneIndex3 })
                .Where(index => index >= 0 && index < renderer.bones.Length)
                .Distinct()
                .OrderBy(index => index)
                .ToArray();
            var usedBones = usedIndices.Length;
            if (usedBones != expectedBones)
                throw new InvalidOperationException(
                    path + " uses " + usedBones + " bones instead of " + expectedBones + ".");
            if (renderer.bones.Any(bone => bone == null))
                throw new InvalidOperationException(path + " contains a null renderer bone.");
            report.AppendLine();
            report.AppendLine("Path=" + path);
            report.AppendLine("Renderer=" + renderer.name);
            report.AppendLine("Mesh=" + mesh.name);
            report.AppendLine("Vertices=" + mesh.vertexCount);
            report.AppendLine("Triangles=" + triangleCount);
            report.AppendLine("RendererBones=" + renderer.bones.Length);
            report.AppendLine("UsedBones=" + usedBones);
            report.AppendLine("UsedBoneNames=" + string.Join(",", usedIndices.Select(index => renderer.bones[index].name)));
            report.AppendLine("BoneNames=" + string.Join(",", renderer.bones.Select(bone => bone.name)));
            report.AppendLine("Bounds=" + mesh.bounds);
            report.AppendLine("Materials=" + string.Join(",", renderer.sharedMaterials.Select(MaterialValue)));
            foreach (var material in renderer.sharedMaterials.Where(material => material != null))
            {
                report.AppendLine(
                    "MaterialDetail=" + material.name +
                    "|Shader=" + (material.shader != null ? material.shader.name : "<none>") +
                    "|MainTexture=" + (material.mainTexture != null ? material.mainTexture.name : "<none>") +
                    "|MainTextureAsset=" + AssetDatabase.GetAssetPath(material.mainTexture));
            }
        }
    }
}
