using System;
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

namespace Bellerophon.Editor
{
    internal static class OstinatoWalkingAnimation
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ostinato Enemy Placement";
        private const string StaticSlotName = "Ostinato_03_Static_Review";
        private const string WalkingSlotName = "Ostinato_03_Walking";
        private const string WalkingModelName = "Ostinato_Walking_Model";
        private const string SourceAbsoluteRelativePath = "enemies model/ostinato walking.fbx";
        private const string WalkingModelAssetPath = "Assets/_Project/Art/Enemies/Ostinato/Models/Ostinato_Walking.fbx";
        private const string ApprovedModelAssetPath = "Assets/_Project/Art/Enemies/Ostinato/Models/Ostinato_ApprovedUnity.fbx";
        private const string ApprovedChitinMaterialPath = "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_Chitin.mat";
        private const string AnimationFolderPath = "Assets/_Project/Art/Enemies/Ostinato/Animations";
        private const string WalkingMaterialPath = AnimationFolderPath + "/Ostinato_Walking_BodyTone.mat";
        private const string WalkingControllerPath = AnimationFolderPath + "/Ostinato_03_Walking.controller";
        private const string WalkingClipNameFragment = "walking_man";
        private const string StateName = "Ostinato_03_Walking_Loop";
        private const string ValidationFolderPath = "docs/validation/ostinato_walking_2026-07-19";
        private const string InspectionReportPath = ValidationFolderPath + "/Ostinato_WalkingSourceInspection.txt";
        private const string ApplyReportPath = ValidationFolderPath + "/Ostinato_WalkingApply.txt";
        private const int PlacementCount = 9;

        [MenuItem("Bellerophon/Enemies/Ostinato/Inspect Walking Source")]
        public static void InspectOstinatoWalkingSource()
        {
            var walkingAsset = RequireAsset<GameObject>(WalkingModelAssetPath);
            var approvedAsset = RequireAsset<GameObject>(ApprovedModelAssetPath);
            var walkingRenderer = RequireSingleRenderer(walkingAsset, "Walking");
            var approvedRenderer = RequireSingleRenderer(approvedAsset, "Approved");
            var walkingMesh = walkingRenderer.sharedMesh ??
                throw new InvalidOperationException("Walking Ostinato renderer has no mesh.");
            var approvedMesh = approvedRenderer.sharedMesh ??
                throw new InvalidOperationException("Approved Ostinato renderer has no mesh.");
            var clips = LoadWalkingClips();
            var walkingUv = walkingMesh.uv;
            var walkingBoneNames = walkingRenderer.bones
                .Select(bone => bone != null ? bone.name : "None")
                .ToArray();
            var approvedBoneNames = approvedRenderer.bones
                .Select(bone => bone != null ? bone.name : "None")
                .ToArray();
            var approvedMaterialNames = new[]
            {
                "Ostinato_Approved_Chitin",
                "Ostinato_Approved_SoftTissue",
                "Ostinato_Approved_HookBlade",
                "Ostinato_Approved_CompoundEye",
            };
            var materialSlotCompatible =
                walkingMesh.subMeshCount == approvedMaterialNames.Length &&
                walkingRenderer.sharedMaterials.Length == approvedMaterialNames.Length &&
                walkingUv.Length == walkingMesh.vertexCount;
            var boneHierarchyCompatible = walkingBoneNames.SequenceEqual(approvedBoneNames);
            var sourceHash = ComputeSha256(ProjectAbsolutePath(SourceAbsoluteRelativePath));
            var importedCopyHash = ComputeSha256(ProjectAbsolutePath(WalkingModelAssetPath));

            Directory.CreateDirectory(ProjectAbsolutePath(ValidationFolderPath));
            var report = new StringBuilder();
            report.AppendLine("Source=" + SourceAbsoluteRelativePath);
            report.AppendLine("UnityCopy=" + WalkingModelAssetPath);
            report.AppendLine("SourceSha256=" + sourceHash);
            report.AppendLine("UnityCopySha256=" + importedCopyHash);
            report.AppendLine("SourceCopyHashesMatch=" + (sourceHash == importedCopyHash));
            AppendRendererReport(report, "Walking", walkingRenderer, walkingMesh);
            AppendRendererReport(report, "Approved", approvedRenderer, approvedMesh);
            report.AppendLine("WalkingBoneNames=" + string.Join("|", walkingBoneNames));
            report.AppendLine("ApprovedBoneNames=" + string.Join("|", approvedBoneNames));
            report.AppendLine("BoneHierarchyCompatible=" + boneHierarchyCompatible);
            report.AppendLine("ApprovedMaterialOrder=" + string.Join("|", approvedMaterialNames));
            report.AppendLine("MaterialSlotCompatibleWithoutMeshChange=" + materialSlotCompatible);
            report.AppendLine("AnimationClipCount=" + clips.Length.ToString(CultureInfo.InvariantCulture));
            foreach (var clip in clips)
            {
                var bindings = AnimationUtility.GetCurveBindings(clip);
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                report.AppendLine(
                    "Clip=" + clip.name +
                    ",Length=" + clip.length.ToString("0.######", CultureInfo.InvariantCulture) +
                    ",FrameRate=" + clip.frameRate.ToString("0.###", CultureInfo.InvariantCulture) +
                    ",CurveBindings=" + bindings.Length.ToString(CultureInfo.InvariantCulture) +
                    ",LoopTime=" + settings.loopTime +
                    ",Legacy=" + clip.legacy +
                    ",HumanMotion=" + clip.humanMotion);
            }

            report.AppendLine("RequestedAppearanceSync=Overall body tone and chitin surface response only");
            report.AppendLine("PerPartMaterialSyncRequired=False");
            report.AppendLine("MeshMutationPerformed=False");
            File.WriteAllText(ProjectAbsolutePath(InspectionReportPath), report.ToString(), new UTF8Encoding(false));

            Debug.Log(
                "OstinatoWalkingSourceInspected" +
                ", SourceCopyHashesMatch=" + (sourceHash == importedCopyHash) +
                ", WalkingVertices=" + walkingMesh.vertexCount.ToString(CultureInfo.InvariantCulture) +
                ", WalkingSubMeshes=" + walkingMesh.subMeshCount.ToString(CultureInfo.InvariantCulture) +
                ", BoneHierarchyCompatible=" + boneHierarchyCompatible +
                ", ClipCount=" + clips.Length.ToString(CultureInfo.InvariantCulture) +
                ", MeshMutationPerformed=False");
        }

        [MenuItem("Bellerophon/Enemies/Ostinato/Apply Walking Animation")]
        public static void ApplyOstinatoWalkingAnimation()
        {
            var scene = RequireOpenScene();
            var placementRoot = scene.GetRootGameObjects()
                .SingleOrDefault(root => root.name == PlacementRootName)?.transform ??
                throw new InvalidOperationException(PlacementRootName + " is missing.");
            if (placementRoot.childCount != PlacementCount)
            {
                throw new InvalidOperationException(
                    PlacementRootName + " must contain exactly " + PlacementCount.ToString(CultureInfo.InvariantCulture) + " slots.");
            }

            var slot = placementRoot.Find(WalkingSlotName) ?? placementRoot.Find(StaticSlotName) ??
                throw new InvalidOperationException("Ostinato slot 03 is missing.");
            if (slot.GetSiblingIndex() != 2)
            {
                throw new InvalidOperationException("Ostinato walking slot must remain the third placement child.");
            }

            var slotStates = placementRoot.Cast<Transform>().Select(TransformState.Capture).ToArray();
            var sourceHashBefore = ComputeSha256(ProjectAbsolutePath(SourceAbsoluteRelativePath));
            var unityCopyHashBefore = ComputeSha256(ProjectAbsolutePath(WalkingModelAssetPath));
            if (sourceHashBefore != unityCopyHashBefore)
            {
                throw new InvalidOperationException("The Unity walking FBX is not byte-identical to the supplied source.");
            }

            ConfigureWalkingClipLoop();
            var walkingAsset = RequireAsset<GameObject>(WalkingModelAssetPath);
            var walkingAssetRenderer = RequireSingleRenderer(walkingAsset, "Walking");
            var sourceMesh = walkingAssetRenderer.sharedMesh ??
                throw new InvalidOperationException("Walking Ostinato renderer has no mesh.");
            var meshSnapshot = MeshSnapshot.Capture(sourceMesh);
            var clip = RequireWalkingClip();
            var material = CreateOrUpdateWalkingMaterial(out var bodyToneSettings);
            var controller = CreateOrUpdateWalkingController(clip);

            var previousModel = slot.Cast<Transform>().SingleOrDefault();
            if (previousModel == null)
            {
                throw new InvalidOperationException("Ostinato slot 03 must contain exactly one model child before replacement.");
            }

            var previousLocalPosition = previousModel.localPosition;
            var previousLocalRotation = previousModel.localRotation;
            var previousLocalScale = previousModel.localScale;
            UnityEngine.Object.DestroyImmediate(previousModel.gameObject);

            var model = PrefabUtility.InstantiatePrefab(walkingAsset, scene) as GameObject ??
                throw new InvalidOperationException("Walking Ostinato FBX could not be instantiated.");
            model.name = WalkingModelName;
            model.transform.SetParent(slot, false);
            model.transform.localPosition = previousLocalPosition;
            model.transform.localRotation = previousLocalRotation;
            model.transform.localScale = previousLocalScale;

            var renderer = RequireSingleRenderer(model, "Walking instance");
            renderer.sharedMaterials = new[] { material };
            renderer.updateWhenOffscreen = true;
            EditorUtility.SetDirty(renderer);

            var animator = model.GetComponent<Animator>();
            if (animator == null)
            {
                animator = model.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);

            slot.name = WalkingSlotName;
            EditorUtility.SetDirty(slot.gameObject);
            VerifyAppliedState(
                placementRoot,
                slot,
                model,
                renderer,
                animator,
                clip,
                controller,
                material,
                bodyToneSettings,
                meshSnapshot,
                slotStates);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after Ostinato walking application.");
            }

            var sourceHashAfter = ComputeSha256(ProjectAbsolutePath(SourceAbsoluteRelativePath));
            var unityCopyHashAfter = ComputeSha256(ProjectAbsolutePath(WalkingModelAssetPath));
            if (sourceHashBefore != sourceHashAfter || unityCopyHashBefore != unityCopyHashAfter || sourceHashAfter != unityCopyHashAfter)
            {
                throw new InvalidOperationException("Walking FBX bytes changed during Unity application.");
            }

            WriteApplyReport(renderer, animator, clip, material, bodyToneSettings, meshSnapshot, sourceHashAfter);
            Selection.activeGameObject = slot.gameObject;
            Debug.Log(
                "OstinatoWalkingAnimationApplied" +
                ", Target=" + PlacementRootName + "/" + WalkingSlotName +
                ", Clip=" + clip.name +
                ", LoopTime=True" +
                ", RootMotion=False" +
                ", Material=" + material.name +
                ", BodyToneSource=Ostinato_Approved_Chitin" +
                ", MeshMutationPerformed=False" +
                ", OtherSlotsUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Ostinato/Capture Walking Runtime Playback")]
        public static void CaptureOstinatoWalkingRuntimePlayback()
        {
            OstinatoWalkingRuntimeCapture.Begin();
        }

        private static void ConfigureWalkingClipLoop()
        {
            var importer = AssetImporter.GetAtPath(WalkingModelAssetPath) as ModelImporter ??
                throw new InvalidOperationException("Walking FBX ModelImporter is unavailable.");
            var clips = importer.clipAnimations != null && importer.clipAnimations.Length > 0
                ? importer.clipAnimations
                : importer.defaultClipAnimations;
            var matchCount = clips.Count(clip =>
                clip.name.IndexOf(WalkingClipNameFragment, StringComparison.OrdinalIgnoreCase) >= 0);
            if (matchCount != 1)
            {
                throw new InvalidOperationException(
                    "Walking FBX must contain exactly one walking_man clip. Count=" +
                    matchCount.ToString(CultureInfo.InvariantCulture));
            }

            foreach (var clip in clips)
            {
                if (clip.name.IndexOf(WalkingClipNameFragment, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                clip.loopTime = true;
                clip.loopPose = true;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static Material CreateOrUpdateWalkingMaterial(out BodyToneSettings settings)
        {
            var approved = RequireAsset<Material>(ApprovedChitinMaterialPath);
            settings = DeriveBodyToneSettings(approved);
            var material = AssetDatabase.LoadAssetAtPath<Material>(WalkingMaterialPath);
            if (material == null)
            {
                material = new Material(approved) { name = "Ostinato_Walking_BodyTone" };
                AssetDatabase.CreateAsset(material, WalkingMaterialPath);
            }
            else
            {
                EditorUtility.CopySerialized(approved, material);
                material.name = "Ostinato_Walking_BodyTone";
                EditorUtility.SetDirty(material);
            }

            // The walking FBX has a different UV layout. Transfer the approved chitin's
            // effective body tone and PBR response without sampling its empty UV regions.
            material.SetTexture("_BaseMap", null);
            material.SetTexture("_MainTex", null);
            material.SetTexture("_BumpMap", null);
            material.SetTexture("_MetallicGlossMap", null);
            material.DisableKeyword("_NORMALMAP");
            material.DisableKeyword("_METALLICSPECGLOSSMAP");
            material.SetColor("_BaseColor", settings.BaseColor);
            material.SetColor("_Color", settings.BaseColor);
            material.SetFloat("_Metallic", settings.Metallic);
            material.SetFloat("_Smoothness", settings.Smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static BodyToneSettings DeriveBodyToneSettings(Material approved)
        {
            var baseMap = approved.GetTexture("_BaseMap") ??
                throw new InvalidOperationException("Approved Chitin material has no Base Map.");
            var metallicMap = approved.GetTexture("_MetallicGlossMap") ??
                throw new InvalidOperationException("Approved Chitin material has no Metallic Smoothness Map.");
            var basePixels = ReadTexturePixels(baseMap);
            var metallicPixels = ReadTexturePixels(metallicMap);
            try
            {
                if (basePixels.Pixels.Length != metallicPixels.Pixels.Length)
                {
                    throw new InvalidOperationException("Approved Chitin PBR textures must have matching dimensions.");
                }

                var baseSum = Vector3.zero;
                var metallicSum = 0f;
                var smoothnessSum = 0f;
                var validPixelCount = 0;
                for (var index = 0; index < basePixels.Pixels.Length; index++)
                {
                    var basePixel = basePixels.Pixels[index];
                    if (Mathf.Max(basePixel.r, Mathf.Max(basePixel.g, basePixel.b)) <= 0.01f)
                    {
                        continue;
                    }

                    var metallicPixel = metallicPixels.Pixels[index];
                    baseSum += new Vector3(basePixel.r, basePixel.g, basePixel.b);
                    metallicSum += metallicPixel.r;
                    smoothnessSum += metallicPixel.a;
                    validPixelCount++;
                }

                if (validPixelCount == 0)
                {
                    throw new InvalidOperationException("Approved Chitin Base Map has no non-empty body pixels.");
                }

                var inverseCount = 1f / validPixelCount;
                var averageBase = baseSum * inverseCount;
                var multiplier = approved.GetColor("_BaseColor");
                var baseColor = new Color(
                    averageBase.x * multiplier.r,
                    averageBase.y * multiplier.g,
                    averageBase.z * multiplier.b,
                    1f);
                return new BodyToneSettings(
                    baseColor,
                    Mathf.Clamp01(metallicSum * inverseCount * approved.GetFloat("_Metallic")),
                    Mathf.Clamp01(smoothnessSum * inverseCount * approved.GetFloat("_Smoothness")),
                    validPixelCount,
                    basePixels.Pixels.Length);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(basePixels.Texture);
                UnityEngine.Object.DestroyImmediate(metallicPixels.Texture);
            }
        }

        private static ReadableTexture ReadTexturePixels(Texture source)
        {
            var renderTexture = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);
            var previousActive = RenderTexture.active;
            try
            {
                Graphics.Blit(source, renderTexture);
                RenderTexture.active = renderTexture;
                var readable = new Texture2D(
                    source.width,
                    source.height,
                    TextureFormat.RGBA32,
                    false,
                    true);
                readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, false);
                readable.Apply(false, false);
                return new ReadableTexture(readable, readable.GetPixels());
            }
            finally
            {
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static AnimatorController CreateOrUpdateWalkingController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(WalkingControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(WalkingControllerPath);
            }

            if (controller.layers == null || controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states.Select(child => child.state)
                .FirstOrDefault(candidate => candidate.name == StateName) ?? stateMachine.AddState(StateName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void VerifyAppliedState(
            Transform placementRoot,
            Transform slot,
            GameObject model,
            SkinnedMeshRenderer renderer,
            Animator animator,
            AnimationClip clip,
            AnimatorController controller,
            Material material,
            BodyToneSettings bodyToneSettings,
            MeshSnapshot meshSnapshot,
            TransformState[] slotStates)
        {
            if (slot.name != WalkingSlotName || slot.GetSiblingIndex() != 2 || model.transform.parent != slot)
            {
                throw new InvalidOperationException("Ostinato walking model is not confined to slot 03.");
            }

            for (var index = 0; index < slotStates.Length; index++)
            {
                var current = placementRoot.GetChild(index);
                if (index == 2)
                {
                    if (!slotStates[index].MatchesTransformOnly(current))
                    {
                        throw new InvalidOperationException("Ostinato slot 03 placement transform changed.");
                    }

                    continue;
                }

                if (!slotStates[index].Matches(current))
                {
                    throw new InvalidOperationException("An Ostinato slot outside slot 03 changed: " + current.name);
                }
            }

            if (!meshSnapshot.Matches(renderer.sharedMesh))
            {
                throw new InvalidOperationException("Walking mesh data changed during scene application.");
            }

            if (renderer.sharedMaterials.Length != 1 || renderer.sharedMaterial != material)
            {
                throw new InvalidOperationException("Walking renderer must use only the dedicated body-tone material.");
            }

            if (!MaterialSettingsMatchApprovedChitinTone(material, bodyToneSettings))
            {
                throw new InvalidOperationException("Walking body-tone material does not match the derived approved Chitin tone.");
            }

            if (animator.runtimeAnimatorController != controller || animator.applyRootMotion || !animator.enabled)
            {
                throw new InvalidOperationException("Walking Animator is not configured for root-locked playback.");
            }

            var state = controller.layers[0].stateMachine.defaultState;
            if (state == null || state.name != StateName || state.motion != clip)
            {
                throw new InvalidOperationException("Walking AnimatorController default state is invalid.");
            }

            if (!AnimationUtility.GetAnimationClipSettings(clip).loopTime)
            {
                throw new InvalidOperationException("Walking clip Loop Time is disabled.");
            }
        }

        private static bool MaterialSettingsMatchApprovedChitinTone(
            Material material,
            BodyToneSettings settings)
        {
            var approved = RequireAsset<Material>(ApprovedChitinMaterialPath);
            return material.shader == approved.shader &&
                   material.GetColor("_BaseColor") == settings.BaseColor &&
                   Mathf.Approximately(material.GetFloat("_Metallic"), settings.Metallic) &&
                   Mathf.Approximately(material.GetFloat("_Smoothness"), settings.Smoothness) &&
                   material.GetTexture("_BaseMap") == null &&
                   material.GetTexture("_BumpMap") == null &&
                   material.GetTexture("_MetallicGlossMap") == null;
        }

        private static void WriteApplyReport(
            SkinnedMeshRenderer renderer,
            Animator animator,
            AnimationClip clip,
            Material material,
            BodyToneSettings bodyToneSettings,
            MeshSnapshot mesh,
            string sourceHash)
        {
            Directory.CreateDirectory(ProjectAbsolutePath(ValidationFolderPath));
            var report = new StringBuilder();
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("Target=" + PlacementRootName + "/" + WalkingSlotName);
            report.AppendLine("Model=" + WalkingModelAssetPath);
            report.AppendLine("SourceSha256=" + sourceHash);
            report.AppendLine("UnityCopySha256=" + ComputeSha256(ProjectAbsolutePath(WalkingModelAssetPath)));
            report.AppendLine("SourceCopyHashesMatch=True");
            report.AppendLine("Clip=" + clip.name);
            report.AppendLine("ClipLength=" + clip.length.ToString("0.######", CultureInfo.InvariantCulture));
            report.AppendLine("ClipFrameRate=" + clip.frameRate.ToString("0.###", CultureInfo.InvariantCulture));
            report.AppendLine("LoopTime=" + AnimationUtility.GetAnimationClipSettings(clip).loopTime);
            report.AppendLine("AnimatorController=" + AssetDatabase.GetAssetPath(animator.runtimeAnimatorController));
            report.AppendLine("AnimatorState=" + StateName);
            report.AppendLine("ApplyRootMotion=False");
            report.AppendLine("Material=" + AssetDatabase.GetAssetPath(material));
            report.AppendLine("BodyToneSource=" + ApprovedChitinMaterialPath);
            report.AppendLine("BodyToneTransfer=Average of non-empty approved Chitin texture pixels");
            report.AppendLine("BodyToneBaseColor=" + FormatColor(bodyToneSettings.BaseColor));
            report.AppendLine("BodyToneMetallic=" + bodyToneSettings.Metallic.ToString("0.######", CultureInfo.InvariantCulture));
            report.AppendLine("BodyToneSmoothness=" + bodyToneSettings.Smoothness.ToString("0.######", CultureInfo.InvariantCulture));
            report.AppendLine("BodyToneValidTexturePixels=" + bodyToneSettings.ValidPixelCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("BodyToneTotalTexturePixels=" + bodyToneSettings.TotalPixelCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("BodyToneAndSurfaceSettingsDerivedFromApprovedChitin=True");
            report.AppendLine("PerPartMaterialSync=False");
            report.AppendLine("Mesh=" + renderer.sharedMesh.name);
            report.AppendLine("VertexCount=" + mesh.VertexCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("SubMeshCount=" + mesh.SubMeshCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("UvCount=" + mesh.UvCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("BoneWeightCount=" + mesh.BoneWeightCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("BindPoseCount=" + mesh.BindPoseCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("MeshMutationPerformed=False");
            report.AppendLine("OtherSlotsUnchanged=True");
            File.WriteAllText(ProjectAbsolutePath(ApplyReportPath), report.ToString(), new UTF8Encoding(false));
        }

        private static string FormatColor(Color color)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0:0.######},{1:0.######},{2:0.######},{3:0.######})",
                color.r,
                color.g,
                color.b,
                color.a);
        }

        private static AnimationClip[] LoadWalkingClips()
        {
            return AssetDatabase.LoadAllAssetsAtPath(WalkingModelAssetPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        private static AnimationClip RequireWalkingClip()
        {
            var matches = LoadWalkingClips()
                .Where(clip => clip.name.IndexOf(WalkingClipNameFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Walking FBX must contain exactly one walking_man AnimationClip. Count=" +
                    matches.Length.ToString(CultureInfo.InvariantCulture));
            }

            return matches[0];
        }

        private static void AppendRendererReport(StringBuilder report, string prefix, SkinnedMeshRenderer renderer, Mesh mesh)
        {
            report.AppendLine(prefix + "Renderer=" + renderer.name);
            report.AppendLine(prefix + "Mesh=" + mesh.name);
            report.AppendLine(prefix + "VertexCount=" + mesh.vertexCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine(prefix + "SubMeshCount=" + mesh.subMeshCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine(prefix + "UvCount=" + mesh.uv.Length.ToString(CultureInfo.InvariantCulture));
            report.AppendLine(prefix + "BoneWeightCount=" + mesh.boneWeights.Length.ToString(CultureInfo.InvariantCulture));
            report.AppendLine(prefix + "BindPoseCount=" + mesh.bindposes.Length.ToString(CultureInfo.InvariantCulture));
            report.AppendLine(prefix + "RendererBoneCount=" + renderer.bones.Length.ToString(CultureInfo.InvariantCulture));
            report.AppendLine(prefix + "Materials=" +
                              string.Join("|", renderer.sharedMaterials.Select(value => value != null ? value.name : "None")));
            report.AppendLine(prefix + "SubMeshIndexCounts=" +
                              string.Join("|", Enumerable.Range(0, mesh.subMeshCount).Select(index =>
                                  mesh.GetIndexCount(index).ToString(CultureInfo.InvariantCulture))));
        }

        private static SkinnedMeshRenderer RequireSingleRenderer(GameObject asset, string label)
        {
            var renderers = asset.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length != 1)
            {
                throw new InvalidOperationException(
                    label + " Ostinato must contain exactly one SkinnedMeshRenderer. Count=" +
                    renderers.Length.ToString(CultureInfo.InvariantCulture));
            }

            return renderers[0];
        }

        private static Scene RequireOpenScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
            {
                throw new InvalidOperationException("CargoRunMvp must already be the active scene.");
            }

            return scene;
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path) ??
                   throw new InvalidOperationException(typeof(T).Name + " asset is missing: " + path);
        }

        internal static string ComputeSha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha256 = SHA256.Create();
            return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty);
        }

        internal static string ProjectAbsolutePath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }

        private readonly struct MeshSnapshot
        {
            private MeshSnapshot(Mesh mesh)
            {
                VertexCount = mesh.vertexCount;
                SubMeshCount = mesh.subMeshCount;
                UvCount = mesh.uv.Length;
                BoneWeightCount = mesh.boneWeights.Length;
                BindPoseCount = mesh.bindposes.Length;
                IndexCounts = new uint[mesh.subMeshCount];
                for (var index = 0; index < mesh.subMeshCount; index++)
                {
                    IndexCounts[index] = mesh.GetIndexCount(index);
                }
            }

            public int VertexCount { get; }
            public int SubMeshCount { get; }
            public int UvCount { get; }
            public int BoneWeightCount { get; }
            public int BindPoseCount { get; }
            private uint[] IndexCounts { get; }

            public static MeshSnapshot Capture(Mesh mesh) => new MeshSnapshot(mesh);

            public bool Matches(Mesh mesh)
            {
                if (mesh == null ||
                    mesh.vertexCount != VertexCount ||
                    mesh.subMeshCount != SubMeshCount ||
                    mesh.uv.Length != UvCount ||
                    mesh.boneWeights.Length != BoneWeightCount ||
                    mesh.bindposes.Length != BindPoseCount)
                {
                    return false;
                }

                for (var index = 0; index < mesh.subMeshCount; index++)
                {
                    if (mesh.GetIndexCount(index) != IndexCounts[index])
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private readonly struct BodyToneSettings
        {
            public BodyToneSettings(
                Color baseColor,
                float metallic,
                float smoothness,
                int validPixelCount,
                int totalPixelCount)
            {
                BaseColor = baseColor;
                Metallic = metallic;
                Smoothness = smoothness;
                ValidPixelCount = validPixelCount;
                TotalPixelCount = totalPixelCount;
            }

            public Color BaseColor { get; }
            public float Metallic { get; }
            public float Smoothness { get; }
            public int ValidPixelCount { get; }
            public int TotalPixelCount { get; }
        }

        private readonly struct ReadableTexture
        {
            public ReadableTexture(Texture2D texture, Color[] pixels)
            {
                Texture = texture;
                Pixels = pixels;
            }

            public Texture2D Texture { get; }
            public Color[] Pixels { get; }
        }

        private readonly struct TransformState
        {
            private TransformState(Transform transform)
            {
                Name = transform.name;
                LocalPosition = transform.localPosition;
                LocalRotation = transform.localRotation;
                LocalScale = transform.localScale;
            }

            private string Name { get; }
            private Vector3 LocalPosition { get; }
            private Quaternion LocalRotation { get; }
            private Vector3 LocalScale { get; }

            public static TransformState Capture(Transform transform) => new TransformState(transform);

            public bool Matches(Transform transform)
            {
                return transform.name == Name && MatchesTransformOnly(transform);
            }

            public bool MatchesTransformOnly(Transform transform)
            {
                return transform.localPosition == LocalPosition &&
                       transform.localRotation == LocalRotation &&
                       transform.localScale == LocalScale;
            }
        }
    }

    [InitializeOnLoad]
    internal static class OstinatoWalkingRuntimeCapture
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ostinato Enemy Placement";
        private const string WalkingSlotName = "Ostinato_03_Walking";
        private const string WalkingModelName = "Ostinato_Walking_Model";
        private const string StateName = "Ostinato_03_Walking_Loop";
        private const string ValidationFolderPath = "docs/validation/ostinato_walking_2026-07-19";
        private const string FrameFolderPath = ValidationFolderPath + "/runtime_frames";
        private const string RuntimeReportPath = ValidationFolderPath + "/Ostinato_WalkingRuntimePlayback.txt";
        private const string RuntimeHtmlPath = ValidationFolderPath + "/index.html";
        private const string CompletionPath = ValidationFolderPath + "/Ostinato_WalkingRuntimePlayback.completed";
        private const string FailurePath = ValidationFolderPath + "/Ostinato_WalkingRuntimePlayback.failed.txt";
        private const string SessionStateKey = "Bellerophon.OstinatoWalkingRuntimeCapture.State";
        private const string SessionFailureKey = "Bellerophon.OstinatoWalkingRuntimeCapture.Failed";
        private const int WaitingForPlayMode = 1;
        private const int Capturing = 2;
        private const int WaitingForEditMode = 3;
        private const int ReviewLayer = 30;
        private const int CaptureFramesPerSecond = 12;
        private const int CaptureImageSize = 480;
        private const float CaptureLoopCount = 2f;

        private static Animator animator;
        private static SkinnedMeshRenderer renderer;
        private static Camera reviewCamera;
        private static GameObject cameraObject;
        private static GameObject keyObject;
        private static GameObject fillObject;
        private static GameObject[] layeredObjects;
        private static int[] originalLayers;
        private static Vector3 modelStartPosition;
        private static Quaternion modelStartRotation;
        private static Bounds framingBounds;
        private static float clipLength;
        private static float startNormalizedTime;
        private static double captureStartEditorTime;
        private static int nextFrameIndex;
        private static int totalFrameCount;
        private static readonly StringBuilder FrameSamples = new StringBuilder();

        static OstinatoWalkingRuntimeCapture()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        public static void Begin()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Unity must be in Edit Mode before Ostinato walking capture begins.");
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath || scene.isDirty)
            {
                throw new InvalidOperationException("Saved CargoRunMvp must already be the active scene.");
            }

            var validationFolder = OstinatoWalkingAnimation.ProjectAbsolutePath(ValidationFolderPath);
            var frameFolder = OstinatoWalkingAnimation.ProjectAbsolutePath(FrameFolderPath);
            Directory.CreateDirectory(validationFolder);
            Directory.CreateDirectory(frameFolder);
            foreach (var framePath in Directory.GetFiles(frameFolder, "frame_*.png"))
            {
                File.Delete(framePath);
            }

            DeleteIfPresent(RuntimeReportPath);
            DeleteIfPresent(RuntimeHtmlPath);
            DeleteIfPresent(CompletionPath);
            DeleteIfPresent(FailurePath);
            FrameSamples.Length = 0;
            SessionState.SetBool(SessionFailureKey, false);
            SessionState.SetInt(SessionStateKey, WaitingForPlayMode);
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            var state = SessionState.GetInt(SessionStateKey, 0);
            if (state == 0)
            {
                return;
            }

            try
            {
                if (state == WaitingForPlayMode)
                {
                    if (EditorApplication.isPlaying)
                    {
                        TryStartCapture();
                    }

                    return;
                }

                if (state == Capturing)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException("Unity left Play Mode before walking capture completed.");
                    }

                    CaptureFrameWhenDue();
                    return;
                }

                if (state == WaitingForEditMode && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    if (!SessionState.GetBool(SessionFailureKey, false))
                    {
                        File.WriteAllText(
                            OstinatoWalkingAnimation.ProjectAbsolutePath(CompletionPath),
                            "Ostinato walking runtime playback capture completed after returning to Edit Mode.",
                            new UTF8Encoding(false));
                        Debug.Log(
                            "OstinatoWalkingRuntimePlaybackCaptured" +
                            ", Frames=" + totalFrameCount.ToString(CultureInfo.InvariantCulture) +
                            ", Loops=2" +
                            ", RootMotion=False" +
                            ", Html=" + RuntimeHtmlPath);
                    }

                    SessionState.EraseInt(SessionStateKey);
                    SessionState.EraseBool(SessionFailureKey);
                }
            }
            catch (Exception exception)
            {
                FailCapture(exception);
            }
        }

        private static void TryStartCapture()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                return;
            }

            var placementRoot = scene.GetRootGameObjects()
                .SingleOrDefault(root => root.name == PlacementRootName)?.transform;
            var slot = placementRoot != null ? placementRoot.Find(WalkingSlotName) : null;
            var model = slot != null ? slot.Find(WalkingModelName) : null;
            if (model == null)
            {
                return;
            }

            animator = model.GetComponent<Animator>();
            renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).SingleOrDefault();
            if (animator == null || renderer == null || !animator.enabled || animator.runtimeAnimatorController == null)
            {
                throw new InvalidOperationException("Ostinato slot 03 runtime Animator or renderer is not active.");
            }

            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!stateInfo.IsName(StateName) && !stateInfo.IsName("Base Layer." + StateName))
            {
                return;
            }

            clipLength = animator.runtimeAnimatorController.animationClips.Single().length;
            layeredObjects = model.GetComponentsInChildren<Transform>(true).Select(target => target.gameObject).ToArray();
            originalLayers = layeredObjects.Select(target => target.layer).ToArray();
            foreach (var target in layeredObjects)
            {
                target.layer = ReviewLayer;
            }

            cameraObject = new GameObject("Ostinato_Walking_ReviewCamera", typeof(Camera));
            keyObject = new GameObject("Ostinato_Walking_KeyLight", typeof(Light));
            fillObject = new GameObject("Ostinato_Walking_FillLight", typeof(Light));
            reviewCamera = cameraObject.GetComponent<Camera>();
            ConfigureReviewCameraAndLights();

            framingBounds = renderer.bounds;
            modelStartPosition = model.position;
            modelStartRotation = model.rotation;
            startNormalizedTime = stateInfo.normalizedTime;
            captureStartEditorTime = EditorApplication.timeSinceStartup;
            nextFrameIndex = 0;
            totalFrameCount = Mathf.CeilToInt(clipLength * CaptureFramesPerSecond * CaptureLoopCount) + 1;
            SessionState.SetInt(SessionStateKey, Capturing);
        }

        private static void CaptureFrameWhenDue()
        {
            if (EditorApplication.timeSinceStartup - captureStartEditorTime > 20d)
            {
                throw new TimeoutException("Ostinato walking Animator did not complete two loops within 20 seconds.");
            }

            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!stateInfo.IsName(StateName) && !stateInfo.IsName("Base Layer." + StateName))
            {
                throw new InvalidOperationException("Ostinato runtime Animator left its walking state.");
            }

            var elapsedNormalizedTime = stateInfo.normalizedTime - startNormalizedTime;
            var targetNormalizedTime = nextFrameIndex / (clipLength * CaptureFramesPerSecond);
            if (elapsedNormalizedTime + 0.002f < targetNormalizedTime)
            {
                return;
            }

            if ((animator.transform.position - modelStartPosition).sqrMagnitude > 0.000001f ||
                Quaternion.Angle(animator.transform.rotation, modelStartRotation) > 0.001f)
            {
                throw new InvalidOperationException("Ostinato walking model root moved despite root-motion lock.");
            }

            var frame = RenderFrame();
            try
            {
                var frameName = "frame_" + nextFrameIndex.ToString("0000", CultureInfo.InvariantCulture) + ".png";
                File.WriteAllBytes(
                    Path.Combine(OstinatoWalkingAnimation.ProjectAbsolutePath(FrameFolderPath), frameName),
                    frame.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(frame);
            }

            FrameSamples.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "Frame{0}=AnimatorNormalized:{1:0.######},RootPositionDelta:{2:0.######},RootRotationDelta:{3:0.######}",
                nextFrameIndex,
                stateInfo.normalizedTime,
                Vector3.Distance(animator.transform.position, modelStartPosition),
                Quaternion.Angle(animator.transform.rotation, modelStartRotation)));
            nextFrameIndex++;
            if (nextFrameIndex >= totalFrameCount)
            {
                FinishCapture();
            }
        }

        private static void FinishCapture()
        {
            WriteRuntimeReport();
            WriteRuntimeHtml();
            CleanupRuntimeObjects();
            SessionState.SetInt(SessionStateKey, WaitingForEditMode);
            EditorApplication.ExitPlaymode();
        }

        private static void WriteRuntimeReport()
        {
            var report = new StringBuilder();
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("Target=" + PlacementRootName + "/" + WalkingSlotName);
            report.AppendLine("PlaybackMode=Unity Editor Play Mode scene Animator");
            report.AppendLine("AnimatorState=" + StateName);
            report.AppendLine("ClipLength=" + clipLength.ToString("0.######", CultureInfo.InvariantCulture));
            report.AppendLine("CapturedLoops=2");
            report.AppendLine("CaptureFramesPerSecond=" + CaptureFramesPerSecond.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("CapturedFrameCount=" + totalFrameCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("ApplyRootMotion=False");
            report.AppendLine("RootStayedFixed=True");
            report.AppendLine("Material=Ostinato_Walking_BodyTone");
            report.AppendLine("BodyToneSource=Ostinato_Approved_Chitin");
            report.AppendLine("MeshMutationPerformed=False");
            report.AppendLine("FrameFolder=" + FrameFolderPath);
            report.AppendLine("MotionReview=" + RuntimeHtmlPath);
            report.Append(FrameSamples);
            File.WriteAllText(
                OstinatoWalkingAnimation.ProjectAbsolutePath(RuntimeReportPath),
                report.ToString(),
                new UTF8Encoding(false));
        }

        private static void WriteRuntimeHtml()
        {
            var html = "<!doctype html>\n" +
                       "<html lang=\"ko\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">" +
                       "<title>오스티나토 일반 보행 적용 요약</title>" +
                       "<style>body{margin:0;background:#0b0f13;color:#edf3f7;font-family:system-ui,sans-serif}main{width:min(1080px,94vw);margin:38px auto 70px}" +
                       "h1{margin:0 0 8px;font-size:28px}p{color:#aab8c4;line-height:1.6}.grid{display:grid;grid-template-columns:1fr 1fr;gap:18px;margin-top:22px}" +
                       "section{background:#121921;border:1px solid #293542;border-radius:14px;padding:16px}img{display:block;width:100%;background:#080b0e;border-radius:9px}" +
                       ".controls{display:flex;gap:10px;align-items:center;margin-top:12px}button{background:#3e5f3b;color:white;border:0;border-radius:8px;padding:9px 14px}input{flex:1}" +
                       "code{color:#b8d596}@media(max-width:800px){.grid{grid-template-columns:1fr}}</style></head><body><main>" +
                       "<h1>오스티나토 일반 보행 적용 요약</h1><p>3번 슬롯에서 Unity Play Mode의 실제 Animator가 보행 클립을 두 주기 반복 재생한 결과입니다. " +
                       "메시는 변경하지 않았고, 전체 몸통 색조와 갑각 표면 반응은 승인된 <code>Ostinato_Approved_Chitin</code> 설정에서 동기화했습니다.</p>" +
                       "<div class=\"grid\"><section><h2>보행 반복 재생</h2><img id=\"frame\" alt=\"오스티나토 보행 모션\">" +
                       "<div class=\"controls\"><button id=\"toggle\">일시정지</button><input id=\"seek\" type=\"range\" min=\"0\" max=\"" +
                       (totalFrameCount - 1).ToString(CultureInfo.InvariantCulture) + "\" value=\"0\"><output id=\"counter\"></output></div></section>" +
                       "<section><h2>승인 정적 외형 기준</h2><img src=\"../ostinato_approved_material_2026-07-18/Ostinato_ApprovedMaterial_FinalComparison.png\" alt=\"승인 정적 오스티나토 비교\">" +
                       "<p>단일 서브메시 제약에 따라 눈·연조직·칼날을 별도 머티리얼로 나누지 않고, 몸통 중심의 색과 재질 반응만 맞췄습니다.</p></section></div>" +
                       "<script>const count=" + totalFrameCount.ToString(CultureInfo.InvariantCulture) + ",fps=" +
                       CaptureFramesPerSecond.ToString(CultureInfo.InvariantCulture) +
                       ";let i=0,playing=true;const img=document.querySelector('#frame'),seek=document.querySelector('#seek'),counter=document.querySelector('#counter'),toggle=document.querySelector('#toggle');" +
                       "function show(){img.src='runtime_frames/frame_'+String(i).padStart(4,'0')+'.png';seek.value=i;counter.value=(i+1)+' / '+count;}" +
                       "setInterval(()=>{if(playing){i=(i+1)%count;show();}},1000/fps);toggle.onclick=()=>{playing=!playing;toggle.textContent=playing?'일시정지':'재생';};" +
                       "seek.oninput=()=>{i=Number(seek.value);show();};show();</script></main></body></html>";
            File.WriteAllText(
                OstinatoWalkingAnimation.ProjectAbsolutePath(RuntimeHtmlPath),
                html,
                new UTF8Encoding(false));
        }

        private static Texture2D RenderFrame()
        {
            PositionReviewCamera();
            var renderTexture = RenderTexture.GetTemporary(
                CaptureImageSize,
                CaptureImageSize,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            var previousActive = RenderTexture.active;
            try
            {
                reviewCamera.targetTexture = renderTexture;
                reviewCamera.Render();
                RenderTexture.active = renderTexture;
                var texture = new Texture2D(CaptureImageSize, CaptureImageSize, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, CaptureImageSize, CaptureImageSize), 0, 0, false);
                texture.Apply(false, false);
                return texture;
            }
            finally
            {
                reviewCamera.targetTexture = null;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static void ConfigureReviewCameraAndLights()
        {
            reviewCamera.clearFlags = CameraClearFlags.SolidColor;
            reviewCamera.backgroundColor = new Color(0.035f, 0.045f, 0.055f, 1f);
            reviewCamera.fieldOfView = 40f;
            reviewCamera.nearClipPlane = 0.05f;
            reviewCamera.farClipPlane = 100f;
            reviewCamera.cullingMask = 1 << ReviewLayer;
            reviewCamera.allowHDR = true;
            reviewCamera.allowMSAA = true;

            var key = keyObject.GetComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.45f;
            key.color = new Color(1.00f, 0.89f, 0.72f);
            key.cullingMask = 1 << ReviewLayer;
            keyObject.transform.rotation = Quaternion.Euler(38f, -32f, 0f);

            var fill = fillObject.GetComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.78f;
            fill.color = new Color(0.46f, 0.66f, 1.00f);
            fill.cullingMask = 1 << ReviewLayer;
            fillObject.transform.rotation = Quaternion.Euler(326f, 148f, 0f);
        }

        private static void PositionReviewCamera()
        {
            framingBounds.Encapsulate(renderer.bounds);
            var target = framingBounds.center + Vector3.up * framingBounds.extents.y * 0.02f;
            var halfFovRadians = reviewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            var distance = Mathf.Max(framingBounds.extents.y, framingBounds.extents.x) /
                           Mathf.Tan(halfFovRadians) + framingBounds.extents.z + 0.45f;
            reviewCamera.transform.position = target + Vector3.back * distance;
            reviewCamera.transform.rotation = Quaternion.LookRotation(target - reviewCamera.transform.position, Vector3.up);
        }

        private static void CleanupRuntimeObjects()
        {
            if (layeredObjects != null && originalLayers != null)
            {
                for (var index = 0; index < Mathf.Min(layeredObjects.Length, originalLayers.Length); index++)
                {
                    if (layeredObjects[index] != null)
                    {
                        layeredObjects[index].layer = originalLayers[index];
                    }
                }
            }

            DestroyIfPresent(cameraObject);
            DestroyIfPresent(keyObject);
            DestroyIfPresent(fillObject);
            animator = null;
            renderer = null;
            reviewCamera = null;
            cameraObject = null;
            keyObject = null;
            fillObject = null;
            layeredObjects = null;
            originalLayers = null;
        }

        private static void FailCapture(Exception exception)
        {
            CleanupRuntimeObjects();
            File.WriteAllText(
                OstinatoWalkingAnimation.ProjectAbsolutePath(FailurePath),
                exception.ToString(),
                new UTF8Encoding(false));
            SessionState.SetBool(SessionFailureKey, true);
            SessionState.SetInt(SessionStateKey, WaitingForEditMode);
            Debug.LogException(exception);
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.ExitPlaymode();
            }
        }

        private static void DeleteIfPresent(string relativePath)
        {
            var path = OstinatoWalkingAnimation.ProjectAbsolutePath(relativePath);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static void DestroyIfPresent(UnityEngine.Object target)
        {
            if (target != null)
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
