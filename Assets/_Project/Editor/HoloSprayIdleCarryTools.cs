using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bellerophon.PlayerAnimation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class HoloSprayIdleCarryTools
    {
        internal const string OutputFolder =
            "docs/validation/holo_spray_idle_grip_2026-09-11";
        internal const string ModelViewsPath = OutputFolder + "/model_views.png";
        internal const string DiagnosticImagePath = OutputFolder + "/diagnostic.png";
        internal const string FinalImagePath = OutputFolder + "/final.png";

        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string TargetName = "HoloSpray_Idle";
        private const string ModelAssetPath =
            "Assets/_Project/Art/Items/HoloSpray/HolographicSpray.fbx";
        private const string TextureFolder =
            "Assets/_Project/Art/Items/HoloSpray/Textures";
        private const string MaterialFolder =
            "Assets/_Project/Art/Items/HoloSpray/Materials";
        private const string MaterialAssetPath =
            MaterialFolder + "/HolographicSpray.mat";
        private const string EmbeddedMaterialName = "Material.001";
        private const string BaseColorTexturePath = TextureFolder + "/base_color.jpg";
        private const string NormalTexturePath = TextureFolder + "/normal.jpg";
        private const string MetallicTexturePath =
            TextureFolder + "/texture_0_metallic.png";
        private const string RoughnessTexturePath =
            TextureFolder + "/texture_0_roughness.png";
        // URP Lit reads smoothness from the metallic texture alpha channel.
        private const string MetallicSmoothnessTexturePath =
            TextureFolder + "/HolographicSpray_MetallicSmoothness.png";
        private const string ProfileFolder =
            "Assets/_Project/Art/Player/Animations/HoloSprayIdle";
        private const string ProfileAssetPath =
            ProfileFolder + "/HoloSprayIdleCarryProfile.asset";
        private const string RightShoulderPath =
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder";
        private const string RightArmPath = RightShoulderPath + "/RightArm";
        private const string RightForeArmPath = RightArmPath + "/RightForeArm";
        private const string RightHandPath = RightForeArmPath + "/RightHand";
        private const float PositionTolerance = 0.00001f;
        private const float RotationTolerance = 0.05f;
        private const float DesiredCanHeight = 0.28f;
        // Keep the carried arm nearly extended so the sleeve does not collapse at the elbow.
        private const float HandTargetForwardDistance = 0.455f;
        private const float HandTargetRightOffset = 0.02f;
        private const float HandTargetDownOffset = 0.025f;
        // Share palm roll with the forearm instead of concentrating it at the wrist skin seam.
        private const float ForeArmPalmTwistShare = 0.65f;

        internal static string DiagnosticAbsolutePath => Absolute(DiagnosticImagePath);
        internal static string FinalAbsolutePath => Absolute(FinalImagePath);

        internal static void InspectSource()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath) ??
                throw new InvalidOperationException(
                    "Holographic spray FBX did not import as a model.");
            ModelImporter importer = AssetImporter.GetAtPath(ModelAssetPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "Holographic spray ModelImporter is missing.");
            UnityEngine.Object[] representations =
                AssetDatabase.LoadAllAssetsAtPath(ModelAssetPath);
            Renderer[] renderers = modelAsset.GetComponentsInChildren<Renderer>(true);
            Material[] materials = renderers.SelectMany(item => item.sharedMaterials)
                .Where(item => item != null).Distinct().ToArray();

            var report = new StringBuilder()
                .AppendLine("HoloSpray_Idle source inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("scene=" + scene.path)
                .AppendLine("sceneDirty=" + scene.isDirty)
                .AppendLine("target=" + target.name)
                .AppendLine("targetGlobalId=" + GlobalObjectId.GetGlobalObjectIdSlow(target))
                .AppendLine("modelAssetPath=" + ModelAssetPath)
                .AppendLine("modelHash=" + ComputeAssetHash(ModelAssetPath))
                .AppendLine("modelRepresentationCount=" + representations.Length)
                .AppendLine("rendererCount=" + renderers.Length)
                .AppendLine("meshFilterCount=" +
                    modelAsset.GetComponentsInChildren<MeshFilter>(true).Length)
                .AppendLine("skinnedRendererCount=" +
                    modelAsset.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length)
                .AppendLine("materialCount=" + materials.Length)
                .AppendLine("materialImportMode=" + importer.materialImportMode)
                .AppendLine("materialLocation=" + importer.materialLocation)
                .AppendLine("materialSearch=" + importer.materialSearch);

            foreach (UnityEngine.Object representation in representations
                         .OrderBy(item => item.GetType().Name, StringComparer.Ordinal)
                         .ThenBy(item => item.name, StringComparer.Ordinal))
                report.AppendLine("representation=" + representation.GetType().Name + "|" +
                    representation.name + "|" + AssetDatabase.GetAssetPath(representation));
            foreach (MeshFilter filter in modelAsset.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                report.AppendLine("mesh=" + TransformPath(filter.transform, modelAsset.transform) +
                    "|name=" + (mesh == null ? "null" : mesh.name) +
                    "|vertices=" + (mesh == null ? 0 : mesh.vertexCount) +
                    "|subMeshes=" + (mesh == null ? 0 : mesh.subMeshCount) +
                    "|boundsCenter=" + (mesh == null ? "null" : Vec(mesh.bounds.center)) +
                    "|boundsSize=" + (mesh == null ? "null" : Vec(mesh.bounds.size)));
            }
            foreach (Material material in materials.OrderBy(
                         item => item.name, StringComparer.Ordinal))
            {
                report.AppendLine("material=" + material.name + "|path=" +
                    AssetDatabase.GetAssetPath(material) + "|shader=" +
                    (material.shader == null ? "null" : material.shader.name));
                foreach (string propertyName in material.GetTexturePropertyNames())
                {
                    Texture texture = material.GetTexture(propertyName);
                    if (texture != null)
                        report.AppendLine("materialTexture=" + material.name + "|property=" +
                            propertyName + "|texture=" + texture.name + "|path=" +
                            AssetDatabase.GetAssetPath(texture));
                }
            }
            foreach (Transform item in target.transform.Find(RightShoulderPath)
                         .GetComponentsInChildren<Transform>(true)
                         .OrderBy(item => TransformPath(item, target.transform),
                             StringComparer.Ordinal))
                report.AppendLine("rightArmTransform=" + TransformPath(item, target.transform) +
                    "|localPosition=" + Vec(item.localPosition) +
                    "|localRotation=" + Quat(item.localRotation) +
                    "|localScale=" + Vec(item.localScale) +
                    "|worldPosition=" + Vec(item.position));

            WriteText("source_inspection.txt", report.ToString());
            CaptureModelViews(modelAsset);
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[HoloSprayIdle] Sources inspected read-only.");
        }

        internal static void ImportAssets()
        {
            RequireEditMode();
            EnsureAssetFolder(TextureFolder);
            EnsureAssetFolder(MaterialFolder);
            ModelImporter importer = AssetImporter.GetAtPath(ModelAssetPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "Holographic spray ModelImporter is missing.");
            importer.ExtractTextures(Absolute(TextureFolder));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureTextureImporter(
                BaseColorTexturePath, TextureImporterType.Default, true);
            ConfigureTextureImporter(
                NormalTexturePath, TextureImporterType.NormalMap, false);
            ConfigureTextureImporter(
                MetallicTexturePath, TextureImporterType.Default, false);
            ConfigureTextureImporter(
                RoughnessTexturePath, TextureImporterType.Default, false);
            WriteMetallicSmoothnessTexture();

            Material embedded = AssetDatabase.LoadAllAssetsAtPath(ModelAssetPath)
                .OfType<Material>().FirstOrDefault();

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ??
                throw new InvalidOperationException("URP Lit shader is unavailable.");
            Material external = AssetDatabase.LoadAssetAtPath<Material>(MaterialAssetPath);
            if (external == null)
            {
                external = embedded == null
                    ? new Material(urpLit) { name = "HolographicSpray" }
                    : new Material(embedded) { name = "HolographicSpray" };
                AssetDatabase.CreateAsset(external, MaterialAssetPath);
            }
            else
            {
                external.name = "HolographicSpray";
            }

            external.shader = urpLit;
            Texture baseColor = RequireTexture(BaseColorTexturePath);
            Texture normal = RequireTexture(NormalTexturePath);
            Texture metallic = RequireTexture(MetallicTexturePath);
            Texture roughness = RequireTexture(RoughnessTexturePath);
            Texture metallicSmoothness = RequireTexture(MetallicSmoothnessTexturePath);
            if (external.HasProperty("_BaseMap")) external.SetTexture("_BaseMap", baseColor);
            if (external.HasProperty("_MainTex")) external.SetTexture("_MainTex", baseColor);
            if (external.HasProperty("_BaseColor")) external.SetColor("_BaseColor", Color.white);
            if (external.HasProperty("_Color")) external.SetColor("_Color", Color.white);
            if (external.HasProperty("_BumpMap"))
            {
                external.SetTexture("_BumpMap", normal);
                if (external.HasProperty("_BumpScale")) external.SetFloat("_BumpScale", 1f);
                external.EnableKeyword("_NORMALMAP");
            }
            if (external.HasProperty("_MetallicGlossMap"))
            {
                external.SetTexture("_MetallicGlossMap", metallicSmoothness);
                if (external.HasProperty("_Metallic")) external.SetFloat("_Metallic", 1f);
                if (external.HasProperty("_Smoothness")) external.SetFloat("_Smoothness", 1f);
                if (external.HasProperty("_SmoothnessTextureChannel"))
                    external.SetFloat("_SmoothnessTextureChannel", 0f);
                external.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            EditorUtility.SetDirty(external);
            AssetDatabase.SaveAssets();

            importer = AssetImporter.GetAtPath(ModelAssetPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "Holographic spray ModelImporter disappeared.");
            importer.materialSearch = ModelImporterMaterialSearch.Local;
            importer.AddRemap(
                new AssetImporter.SourceAssetIdentifier(
                    typeof(Material),
                    embedded == null ? EmbeddedMaterialName : embedded.name),
                external);
            importer.SaveAndReimport();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            string[] textures = TexturePaths();
            if (textures.Length != 4)
                throw new InvalidOperationException(
                "Expected four holographic spray textures; found " + textures.Length + ".");
            var report = new StringBuilder()
                .AppendLine("Holographic spray embedded asset import")
                .AppendLine("sourceModelChanged=False")
                .AppendLine("textureCount=" + textures.Length)
                .AppendLine("materialCount=1")
                .AppendLine("materialPath=" + MaterialAssetPath)
                .AppendLine("baseColorTexture=" + AssetDatabase.GetAssetPath(baseColor))
                .AppendLine("normalTexture=" + AssetDatabase.GetAssetPath(normal))
                .AppendLine("metallicTexture=" + AssetDatabase.GetAssetPath(metallic))
                .AppendLine("roughnessTexture=" + AssetDatabase.GetAssetPath(roughness))
                .AppendLine("metallicSmoothnessTexture=" +
                    AssetDatabase.GetAssetPath(metallicSmoothness))
                .AppendLine("roughnessUsage=Inverted into metallic alpha as smoothness");
            foreach (string path in textures) report.AppendLine("extractedTexture=" + path);
            foreach (string dependency in AssetDatabase.GetDependencies(ModelAssetPath, true)
                         .OrderBy(path => path, StringComparer.Ordinal))
                report.AppendLine("dependency=" + dependency);
            Debug.Log(report.ToString());
            AssertMaterialPipelineComplete();
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[HoloSprayIdle] Embedded textures and material imported.");
        }

        internal static void ApplyGrip()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            GameObject vacuumIdle = FindUnique(scene, "Vacuum_Idle");
            GameObject speakerIdle = FindUnique(scene, "Speaker_Idle");
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath) ??
                throw new InvalidOperationException("Holographic spray model is missing.");
            Transform rightShoulder = RequirePath(target.transform, RightShoulderPath);
            Transform upperArm = RequirePath(target.transform, RightArmPath);
            Transform foreArm = RequirePath(target.transform, RightForeArmPath);
            Transform hand = RequirePath(target.transform, RightHandPath);
            HoloSprayRightHandFollowBehaviour existingBehaviour =
                target.GetComponent<HoloSprayRightHandFollowBehaviour>();
            if (existingBehaviour != null && existingBehaviour.Profile != null &&
                existingBehaviour.Profile.SourceRightArmPose.Length > 0)
                ApplyBonePose(target.transform,
                    existingBehaviour.Profile.SourceRightArmPose);
            PoseState shoulderBefore = new PoseState(rightShoulder);
            string protectedBefore = ProtectedHierarchySignature(target.transform);
            string speakerBefore = FullHierarchySignature(speakerIdle.transform);
            string modelHash = ComputeAssetHash(ModelAssetPath);
            string materialHash = ComputeAssetHash(MaterialAssetPath);
            string[] texturePaths = TexturePaths();
            string[] textureHashes = texturePaths.Select(ComputeAssetHash).ToArray();
            HoloSprayBoneRotation[] sourcePose = CaptureBonePose(
                upperArm, target.transform);

            Vector3 handTarget = upperArm.position +
                target.transform.forward * HandTargetForwardDistance +
                target.transform.right * HandTargetRightOffset -
                target.transform.up * HandTargetDownOffset;
            Vector3 elbowPole = upperArm.position +
                target.transform.right * 0.02f -
                target.transform.up * 0.16f +
                target.transform.forward * 0.12f;
            SolveTwoBone(upperArm, foreArm, hand, handTarget, elbowPole);
            CopyVacuumFingerPose(vacuumIdle.transform, target.transform);
            AlignRightHand(target.transform);
            HoloSprayBoneRotation[] authoredPose = CaptureBonePose(
                upperArm, target.transform);

            Vector3 modelLocalPosition;
            Quaternion modelLocalRotation;
            Vector3 modelLocalScale;
            float scaledRadius;
            GameObject measurement = UnityEngine.Object.Instantiate(modelAsset);
            measurement.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                Bounds bounds = CalculateBounds(measurement);
                float scaleFactor = DesiredCanHeight / bounds.size.y;
                modelLocalPosition = measurement.transform.localPosition;
                modelLocalRotation = measurement.transform.localRotation;
                modelLocalScale = measurement.transform.localScale * scaleFactor;
                scaledRadius = Mathf.Max(bounds.size.x, bounds.size.z) * scaleFactor * 0.5f;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(measurement);
            }

            Vector3 palmNormal = RightPalmNormal(target.transform);
            Vector3 holderWorldPosition =
                RightPalmCenter(target.transform) + palmNormal * (scaledRadius * 0.78f);
            Quaternion holderWorldRotation = Quaternion.LookRotation(
                target.transform.forward, target.transform.up);
            Vector3 positionOffset = hand.InverseTransformPoint(holderWorldPosition);
            Quaternion rotationOffset = Quaternion.Inverse(hand.rotation) * holderWorldRotation;

            EnsureAssetFolder(ProfileFolder);
            HoloSprayIdleCarryProfile profile =
                AssetDatabase.LoadAssetAtPath<HoloSprayIdleCarryProfile>(ProfileAssetPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<HoloSprayIdleCarryProfile>();
                AssetDatabase.CreateAsset(profile, ProfileAssetPath);
            }
            profile.Configure(
                modelAsset,
                RightHandPath,
                positionOffset,
                rotationOffset,
                Vector3.one,
                modelLocalPosition,
                modelLocalRotation,
                modelLocalScale,
                sourcePose,
                authoredPose);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            HoloSprayRightHandFollowBehaviour behaviour =
                target.GetComponent<HoloSprayRightHandFollowBehaviour>() ??
                Undo.AddComponent<HoloSprayRightHandFollowBehaviour>(target);
            Undo.RecordObject(behaviour, "Configure HoloSpray right-hand carry");
            behaviour.Configure(profile);
            EditorUtility.SetDirty(behaviour);
            EditorSceneManager.MarkSceneDirty(scene);

            shoulderBefore.RequireExact(rightShoulder, "HoloSpray_Idle RightShoulder");
            RequireEqual(protectedBefore, ProtectedHierarchySignature(target.transform),
                "HoloSpray_Idle protected hierarchy");
            RequireEqual(speakerBefore, FullHierarchySignature(speakerIdle.transform),
                "Speaker_Idle hierarchy");
            RequireEqual(modelHash, ComputeAssetHash(ModelAssetPath), "HoloSpray FBX hash");
            RequireEqual(materialHash, ComputeAssetHash(MaterialAssetPath),
                "HoloSpray material hash");
            for (int index = 0; index < texturePaths.Length; index++)
                RequireEqual(textureHashes[index], ComputeAssetHash(texturePaths[index]),
                    "HoloSpray texture hash " + texturePaths[index]);

            WriteText("baseline.txt",
                "protectedSignature=" + ComputeStringHash(protectedBefore) + Environment.NewLine +
                "speakerSignature=" + ComputeStringHash(speakerBefore) + Environment.NewLine +
                "modelHash=" + modelHash + Environment.NewLine +
                "materialHash=" + materialHash + Environment.NewLine +
                string.Join(Environment.NewLine, texturePaths.Select((path, index) =>
                    "textureHash=" + path + "|" + textureHashes[index])) + Environment.NewLine);
            var report = new StringBuilder()
                .AppendLine("HoloSpray_Idle forward carry application")
                .AppendLine("preexistingSceneDirty=" + sceneWasDirty)
                .AppendLine("sceneSaved=False")
                .AppendLine("rightShoulderChanged=False")
                .AppendLine("torsoHeadLeftArmLowerBodyChanged=False")
                .AppendLine("speakerIdleChanged=False")
                .AppendLine("rightArmPoseCount=" + authoredPose.Length)
                .AppendLine("rightHandPath=" + RightHandPath)
                .AppendLine("positionOffsetInHandSpace=" + Vec(positionOffset))
                .AppendLine("rotationOffsetFromHand=" + Quat(rotationOffset))
                .AppendLine("modelLocalPosition=" + Vec(modelLocalPosition))
                .AppendLine("modelLocalRotation=" + Quat(modelLocalRotation))
                .AppendLine("modelLocalScale=" + Vec(modelLocalScale))
                .AppendLine("canHeightMeters=" + Num(DesiredCanHeight))
                .AppendLine("canUpAngleDegrees=" + Num(Vector3.Angle(
                    behaviour.SprayHolder.transform.up, target.transform.up)))
                .AppendLine("canFrontAngleDegrees=" + Num(Vector3.Angle(
                    behaviour.SprayHolder.transform.forward, target.transform.forward)))
                .AppendLine("armForwardAngleDegrees=" + Num(ArmForwardAngle(target.transform)))
                .AppendLine("elbowBendAngleDegrees=" + Num(
                    RightElbowBendAngle(target.transform)))
                .AppendLine("wristStraightnessDegrees=" + Num(
                    RightWristStraightness(target.transform)))
                .AppendLine("rightPalmToPlayerLeftAngleDegrees=" + Num(
                    RightPalmToPlayerLeftAngle(target.transform)))
                .AppendLine("palmToCanBoundsDistance=" + Num(
                    PalmToCanBoundsDistance(target.transform, behaviour)))
                .AppendLine("embeddedTextureCount=" + texturePaths.Length)
                .AppendLine("externalMaterialPresent=True");
            WriteText("application.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[HoloSprayIdle] Forward right-hand carry applied.");
        }

        internal static void Inspect()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "HoloSpray inspection requires restored Edit Mode.");
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject speakerIdle = FindUnique(scene, "Speaker_Idle");
            HoloSprayRightHandFollowBehaviour behaviour =
                target.GetComponent<HoloSprayRightHandFollowBehaviour>() ??
                throw new InvalidOperationException("HoloSpray follower is missing.");
            HoloSprayIdleCarryProfile profile = behaviour.Profile ??
                throw new InvalidOperationException("HoloSpray profile is missing.");
            string[] baseline = File.ReadAllLines(
                Absolute(OutputFolder + "/baseline.txt"), Encoding.UTF8);
            RequireEqual(BaselineValue(baseline, "protectedSignature"),
                ComputeStringHash(ProtectedHierarchySignature(target.transform)),
                "HoloSpray protected hierarchy");
            RequireEqual(BaselineValue(baseline, "speakerSignature"),
                ComputeStringHash(FullHierarchySignature(speakerIdle.transform)),
                "Speaker_Idle hierarchy");
            RequireEqual(BaselineValue(baseline, "modelHash"),
                ComputeAssetHash(ModelAssetPath), "HoloSpray FBX hash");
            RequireEqual(BaselineValue(baseline, "materialHash"),
                ComputeAssetHash(MaterialAssetPath), "HoloSpray material hash");
            if (profile.RightHandPath != RightHandPath || profile.RightArmPose.Length < 18)
                throw new InvalidOperationException("HoloSpray carry profile changed.");
            float poseError = MaximumPoseError(target.transform, profile.RightArmPose);
            float positionError = Vector3.Distance(
                behaviour.SprayHolder.transform.position, behaviour.ExpectedWorldPosition());
            float rotationError = Quaternion.Angle(
                behaviour.SprayHolder.transform.rotation, behaviour.ExpectedWorldRotation());
            float upAngle = Vector3.Angle(
                behaviour.SprayHolder.transform.up, target.transform.up);
            float frontAngle = Vector3.Angle(
                behaviour.SprayHolder.transform.forward, target.transform.forward);
            float forwardAngle = ArmForwardAngle(target.transform);
            float elbowBendAngle = RightElbowBendAngle(target.transform);
            float wristAngle = RightWristStraightness(target.transform);
            float palmLeftAngle = RightPalmToPlayerLeftAngle(target.transform);
            float gripDistance = PalmToCanBoundsDistance(target.transform, behaviour);
            if (poseError > RotationTolerance || positionError > PositionTolerance ||
                rotationError > RotationTolerance || upAngle > RotationTolerance ||
                frontAngle > RotationTolerance || forwardAngle > 20f ||
                elbowBendAngle > 35f || wristAngle > 5f ||
                palmLeftAngle > 5f || gripDistance > 0.04f || TexturePaths().Length != 4)
                throw new InvalidOperationException(
                    "HoloSpray carry inspection failed. pose=" + Num(poseError) +
                    ", position=" + Num(positionError) + ", rotation=" +
                    Num(rotationError) + ", up=" + Num(upAngle) + ", front=" +
                    Num(frontAngle) + ", arm=" + Num(forwardAngle) + ", elbow=" +
                    Num(elbowBendAngle) + ", wrist=" + Num(wristAngle) +
                    ", palmLeft=" + Num(palmLeftAngle) +
                    ", grip=" + Num(gripDistance) + ".");
            var report = new StringBuilder()
                .AppendLine("HoloSpray_Idle read-only inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("rightShoulderChanged=False")
                .AppendLine("torsoHeadLeftArmLowerBodyChanged=False")
                .AppendLine("speakerIdleChanged=False")
                .AppendLine("canVertical=True")
                .AppendLine("nozzleFront=True")
                .AppendLine("rightHandFollowConfigured=True")
                .AppendLine("rightArmPoseErrorDegrees=" + Num(poseError))
                .AppendLine("followPositionError=" + Num(positionError))
                .AppendLine("followRotationErrorDegrees=" + Num(rotationError))
                .AppendLine("canUpAngleDegrees=" + Num(upAngle))
                .AppendLine("canFrontAngleDegrees=" + Num(frontAngle))
                .AppendLine("armForwardAngleDegrees=" + Num(forwardAngle))
                .AppendLine("elbowBendAngleDegrees=" + Num(elbowBendAngle))
                .AppendLine("wristStraightnessDegrees=" + Num(wristAngle))
                .AppendLine("rightPalmToPlayerLeftAngleDegrees=" +
                    Num(palmLeftAngle))
                .AppendLine("palmToCanBoundsDistance=" + Num(gripDistance))
                .AppendLine("embeddedTextureCount=" + TexturePaths().Length)
                .AppendLine("externalMaterialPresent=" +
                    (AssetDatabase.LoadAssetAtPath<Material>(MaterialAssetPath) != null))
                .AppendLine("sceneSaved=False")
                .AppendLine("sceneDirty=" + scene.isDirty);
            WriteText("inspection.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[HoloSprayIdle] Read-only inspection passed.");
        }

        internal static void EnterReview()
        {
            RequireEditMode();
            Inspect();
            EditorApplication.EnterPlaymode();
        }

        internal static void StopReview()
        {
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }

        internal static void CaptureDiagnostic() => CaptureReview(false);

        internal static void CaptureFinal() => CaptureReview(true);

        private static void CaptureReview(bool final)
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException(
                    "HoloSpray visual review requires Play Mode.");
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            HoloSprayRightHandFollowBehaviour behaviour =
                target.GetComponent<HoloSprayRightHandFollowBehaviour>() ??
                throw new InvalidOperationException("Runtime HoloSpray follower is missing.");
            float positionError = Vector3.Distance(
                behaviour.SprayHolder.transform.position, behaviour.ExpectedWorldPosition());
            float rotationError = Quaternion.Angle(
                behaviour.SprayHolder.transform.rotation, behaviour.ExpectedWorldRotation());
            if (positionError > PositionTolerance || rotationError > RotationTolerance)
                throw new InvalidOperationException(
                    "Runtime HoloSpray follow error exceeded tolerance.");

            Vector3 front = target.transform.forward.normalized;
            Vector3 side = target.transform.right.normalized;
            Vector3 threeQuarter = (front + side * 0.72f).normalized;
            Vector3 elevatedGrip = (
                side + target.transform.up * 0.65f - front * 0.25f).normalized;
            Bounds fullBounds = FullCharacterBounds(target.transform, behaviour);
            Bounds gripBounds = GripBounds(target.transform, behaviour);
            Texture2D[] panels = new Texture2D[6];
            Texture2D composite = null;
            try
            {
                panels[0] = CaptureTargetView(scene, fullBounds, front);
                panels[1] = CaptureTargetView(scene, fullBounds, side);
                panels[2] = CaptureTargetView(scene, fullBounds, threeQuarter);
                panels[3] = CaptureTargetView(scene, gripBounds, front);
                panels[4] = CaptureTargetView(scene, gripBounds, side);
                panels[5] = CaptureTargetView(scene, gripBounds, elevatedGrip);
                composite = new Texture2D(1536, 1024, TextureFormat.RGB24, false);
                for (int index = 0; index < panels.Length; index++)
                {
                    int column = index % 3;
                    int row = 1 - index / 3;
                    composite.SetPixels(
                        column * 512, row * 512, 512, 512, panels[index].GetPixels());
                }
                composite.Apply(false, false);
                string destination = final ? FinalAbsolutePath : DiagnosticAbsolutePath;
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, composite.EncodeToPNG());
            }
            finally
            {
                foreach (Texture2D panel in panels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
                if (composite != null) UnityEngine.Object.DestroyImmediate(composite);
            }

            var report = new StringBuilder()
                .AppendLine("HoloSpray_Idle natural Play Mode carry review")
                .AppendLine("captureKind=" + (final ? "Final" : "Diagnostic"))
                .AppendLine("panelOrder=front,side,threeQuarter,gripFront,gripPlayerRight,gripElevatedPlayerRight")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("canVertical=True")
                .AppendLine("nozzleFront=True")
                .AppendLine("rightArmExtendedForward=True")
                .AppendLine("rightHandGrip=True")
                .AppendLine("rightHandPositionFollowError=" + Num(positionError))
                .AppendLine("rightHandRotationFollowErrorDegrees=" + Num(rotationError))
                .AppendLine("armForwardAngleDegrees=" + Num(ArmForwardAngle(target.transform)))
                .AppendLine("elbowBendAngleDegrees=" +
                    Num(RightElbowBendAngle(target.transform)))
                .AppendLine("wristStraightnessDegrees=" +
                    Num(RightWristStraightness(target.transform)))
                .AppendLine("rightPalmToPlayerLeftAngleDegrees=" +
                    Num(RightPalmToPlayerLeftAngle(target.transform)))
                .AppendLine("palmToCanBoundsDistance=" +
                    Num(PalmToCanBoundsDistance(target.transform, behaviour)))
                .AppendLine("rightShoulderTorsoHeadLeftArmLowerBodyChanged=False")
                .AppendLine("embeddedTextureCount=" + TexturePaths().Length)
                .AppendLine("externalMaterialPresent=True");
            WriteText(final ? "final_runtime.txt" : "diagnostic_runtime.txt",
                report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[HoloSprayIdle] " + (final ? "Final" : "Diagnostic") +
                " visual review captured.");
        }

        private static void CaptureModelViews(GameObject modelAsset)
        {
            GameObject instance = UnityEngine.Object.Instantiate(modelAsset);
            instance.name = "HolographicSpray_ModelInspection";
            instance.hideFlags = HideFlags.HideAndDontSave;
            SetLayerRecursively(instance, 31);
            try
            {
                Bounds bounds = CalculateBounds(instance);
                Vector3[] directions =
                {
                    Vector3.forward, Vector3.back, Vector3.right,
                    Vector3.left, Vector3.down, Vector3.up
                };
                Vector3[] upVectors =
                {
                    Vector3.up, Vector3.up, Vector3.up,
                    Vector3.up, Vector3.forward, Vector3.forward
                };
                Texture2D[] panels = new Texture2D[directions.Length];
                try
                {
                    for (int index = 0; index < panels.Length; index++)
                        panels[index] = CaptureIsolatedView(
                            bounds, directions[index], upVectors[index]);
                    var composite = new Texture2D(
                        panels[0].width * 3,
                        panels[0].height * 2,
                        TextureFormat.RGB24,
                        false);
                    try
                    {
                        for (int index = 0; index < panels.Length; index++)
                        {
                            int column = index % 3;
                            int row = 1 - index / 3;
                            composite.SetPixels(
                                column * panels[index].width,
                                row * panels[index].height,
                                panels[index].width,
                                panels[index].height,
                                panels[index].GetPixels());
                        }
                        composite.Apply(false, false);
                        Directory.CreateDirectory(Absolute(OutputFolder));
                        File.WriteAllBytes(Absolute(ModelViewsPath), composite.EncodeToPNG());
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(composite);
                    }
                }
                finally
                {
                    foreach (Texture2D panel in panels)
                        if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
                }
                WriteText("model_views.txt",
                    "panelOrder=front(+Z),back(-Z),right(+X),left(-X),top(-Y),bottom(+Y)" +
                    Environment.NewLine +
                    "verificationTargetManipulated=False" + Environment.NewLine +
                    "modelBoundsCenter=" + Vec(bounds.center) + Environment.NewLine +
                    "modelBoundsSize=" + Vec(bounds.size) + Environment.NewLine);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static Texture2D CaptureIsolatedView(
            Bounds bounds,
            Vector3 direction,
            Vector3 up)
        {
            var cameraObject = new GameObject("HoloSpray_ModelCamera");
            var lightObject = new GameObject("HoloSpray_ModelLight");
            var camera = cameraObject.AddComponent<Camera>();
            var light = lightObject.AddComponent<Light>();
            var render = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
            try
            {
                camera.cullingMask = 1 << 31;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.04f, 0.045f, 0.055f, 1f);
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(bounds.extents.x,
                    Mathf.Max(bounds.extents.y, bounds.extents.z)) * 1.2f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                float distance = Mathf.Max(bounds.size.magnitude * 1.5f, 1f);
                camera.transform.SetPositionAndRotation(
                    bounds.center + direction.normalized * distance,
                    Quaternion.LookRotation(-direction.normalized, up));
                camera.targetTexture = render;
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                light.transform.rotation = Quaternion.LookRotation(
                    -direction.normalized + up.normalized * -0.35f);
                RenderTexture previous = RenderTexture.active;
                camera.Render();
                RenderTexture.active = render;
                var image = new Texture2D(512, 512, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                image.Apply(false, false);
                RenderTexture.active = previous;
                return image;
            }
            finally
            {
                camera.targetTexture = null;
                render.Release();
                UnityEngine.Object.DestroyImmediate(render);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException("Holographic spray has no renderer.");
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                item.gameObject.layer = layer;
        }

        private static HoloSprayBoneRotation[] CaptureBonePose(
            Transform root,
            Transform targetRoot)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .OrderBy(item => TransformPath(item, targetRoot), StringComparer.Ordinal)
                .Select(item => new HoloSprayBoneRotation(
                    TransformPath(item, targetRoot), item.localRotation))
                .ToArray();
        }

        private static void ApplyBonePose(
            Transform targetRoot,
            IEnumerable<HoloSprayBoneRotation> pose)
        {
            foreach (HoloSprayBoneRotation item in pose)
                RequirePath(targetRoot, item.Path).localRotation = item.LocalRotation;
        }

        private static void CopyVacuumFingerPose(
            Transform vacuumRoot,
            Transform targetRoot)
        {
            Transform vacuumHand = RequirePath(vacuumRoot, RightHandPath);
            Transform targetHand = RequirePath(targetRoot, RightHandPath);
            foreach (Transform source in vacuumHand.GetComponentsInChildren<Transform>(true))
            {
                if (source == vacuumHand) continue;
                string relativePath = TransformPath(source, vacuumHand);
                Transform destination = targetHand.Find(relativePath) ??
                    throw new InvalidOperationException(
                        "HoloSpray finger path is missing: " + relativePath);
                destination.localRotation = source.localRotation;
            }
        }

        private static void AlignRightHand(Transform targetRoot)
        {
            Transform foreArm = RequirePath(targetRoot, RightForeArmPath);
            Transform hand = RequirePath(targetRoot, RightHandPath);
            Vector3 desiredFingerDirection =
                (hand.position - foreArm.position).normalized;
            hand.rotation = Quaternion.FromToRotation(
                RightFingerDirection(targetRoot), desiredFingerDirection) * hand.rotation;
            Vector3 currentPalm = Vector3.ProjectOnPlane(
                RightPalmNormal(targetRoot), desiredFingerDirection).normalized;
            Vector3 desiredPalm = Vector3.ProjectOnPlane(
                -targetRoot.right, desiredFingerDirection).normalized;
            if (currentPalm.sqrMagnitude < 0.99f || desiredPalm.sqrMagnitude < 0.99f)
                throw new InvalidOperationException(
                    "HoloSpray right-palm alignment became degenerate.");
            float twist = Vector3.SignedAngle(
                currentPalm, desiredPalm, desiredFingerDirection);
            foreArm.rotation = Quaternion.AngleAxis(
                twist * ForeArmPalmTwistShare, desiredFingerDirection) * foreArm.rotation;

            currentPalm = Vector3.ProjectOnPlane(
                RightPalmNormal(targetRoot), desiredFingerDirection).normalized;
            float remainingTwist = Vector3.SignedAngle(
                currentPalm, desiredPalm, desiredFingerDirection);
            hand.rotation = Quaternion.AngleAxis(
                remainingTwist, desiredFingerDirection) * hand.rotation;
        }

        private static Vector3 RightPalmCenter(Transform targetRoot)
        {
            string[] names =
            {
                "RightIndexProximal", "RightMiddleProximal",
                "RightRingProximal", "RightLittleProximal"
            };
            Transform hand = RequirePath(targetRoot, RightHandPath);
            return names.Select(name => hand.Find(name)?.position ??
                    throw new InvalidOperationException(
                        "HoloSpray right-hand knuckle is missing: " + name))
                .Aggregate(Vector3.zero, (sum, point) => sum + point) / names.Length;
        }

        private static Vector3 RightFingerDirection(Transform targetRoot)
        {
            Transform hand = RequirePath(targetRoot, RightHandPath);
            Transform middle = hand.Find("RightMiddleProximal") ??
                throw new InvalidOperationException("RightMiddleProximal is missing.");
            return (middle.position - hand.position).normalized;
        }

        private static Vector3 RightPalmNormal(Transform targetRoot)
        {
            Transform hand = RequirePath(targetRoot, RightHandPath);
            Transform index = hand.Find("RightIndexProximal") ??
                throw new InvalidOperationException("RightIndexProximal is missing.");
            Transform little = hand.Find("RightLittleProximal") ??
                throw new InvalidOperationException("RightLittleProximal is missing.");
            Vector3 width = (little.position - index.position).normalized;
            Vector3 normal = Vector3.Cross(
                width, RightFingerDirection(targetRoot)).normalized;
            if (normal.sqrMagnitude < 0.99f)
                throw new InvalidOperationException("Right palm normal is degenerate.");
            return normal;
        }

        private static float RightPalmToPlayerLeftAngle(Transform targetRoot) =>
            Vector3.Angle(
                RightPalmNormal(targetRoot),
                -targetRoot.right.normalized);

        private static float RightWristStraightness(Transform targetRoot)
        {
            Transform foreArm = RequirePath(targetRoot, RightForeArmPath);
            Transform hand = RequirePath(targetRoot, RightHandPath);
            return Vector3.Angle(
                (hand.position - foreArm.position).normalized,
                RightFingerDirection(targetRoot));
        }

        private static float ArmForwardAngle(Transform targetRoot)
        {
            Transform upperArm = RequirePath(targetRoot, RightArmPath);
            Transform hand = RequirePath(targetRoot, RightHandPath);
            return Vector3.Angle(
                (hand.position - upperArm.position).normalized,
                targetRoot.forward.normalized);
        }

        private static float RightElbowBendAngle(Transform targetRoot)
        {
            Transform upperArm = RequirePath(targetRoot, RightArmPath);
            Transform foreArm = RequirePath(targetRoot, RightForeArmPath);
            Transform hand = RequirePath(targetRoot, RightHandPath);
            return Vector3.Angle(
                (foreArm.position - upperArm.position).normalized,
                (hand.position - foreArm.position).normalized);
        }

        private static float PalmToCanBoundsDistance(
            Transform targetRoot,
            HoloSprayRightHandFollowBehaviour behaviour)
        {
            Bounds bounds = CalculateBounds(behaviour.SprayHolder);
            Vector3 palm = RightPalmCenter(targetRoot);
            return Vector3.Distance(palm, bounds.ClosestPoint(palm));
        }

        private static float MaximumPoseError(
            Transform targetRoot,
            IEnumerable<HoloSprayBoneRotation> pose)
        {
            float maximum = 0f;
            foreach (HoloSprayBoneRotation item in pose)
                maximum = Mathf.Max(maximum, Quaternion.Angle(
                    RequirePath(targetRoot, item.Path).localRotation,
                    item.LocalRotation));
            return maximum;
        }

        private static void SolveTwoBone(
            Transform upperArm,
            Transform foreArm,
            Transform hand,
            Vector3 target,
            Vector3 pole)
        {
            Vector3 shoulderPosition = upperArm.position;
            float upperLength = Vector3.Distance(upperArm.position, foreArm.position);
            float lowerLength = Vector3.Distance(foreArm.position, hand.position);
            Vector3 toTarget = target - shoulderPosition;
            float targetDistance = toTarget.magnitude;
            float minimumDistance = Mathf.Abs(upperLength - lowerLength) + 0.0001f;
            float maximumDistance = upperLength + lowerLength - 0.0001f;
            float solvedDistance = Mathf.Clamp(
                targetDistance, minimumDistance, maximumDistance);
            Vector3 targetDirection = toTarget.normalized;
            Vector3 poleDirection = Vector3.ProjectOnPlane(
                pole - shoulderPosition, targetDirection).normalized;
            if (poleDirection.sqrMagnitude < 0.0001f)
                poleDirection = Vector3.ProjectOnPlane(
                    Vector3.down, targetDirection).normalized;
            float cosine = Mathf.Clamp(
                (upperLength * upperLength + solvedDistance * solvedDistance -
                 lowerLength * lowerLength) /
                (2f * upperLength * solvedDistance), -1f, 1f);
            float sine = Mathf.Sqrt(Mathf.Max(0f, 1f - cosine * cosine));
            Vector3 desiredElbow = shoulderPosition +
                targetDirection * (cosine * upperLength) +
                poleDirection * (sine * upperLength);
            upperArm.rotation = Quaternion.FromToRotation(
                foreArm.position - shoulderPosition,
                desiredElbow - shoulderPosition) * upperArm.rotation;
            foreArm.rotation = Quaternion.FromToRotation(
                hand.position - foreArm.position,
                target - foreArm.position) * foreArm.rotation;
        }

        private static Bounds GripBounds(
            Transform targetRoot,
            HoloSprayRightHandFollowBehaviour behaviour)
        {
            Transform upperArm = RequirePath(targetRoot, RightArmPath);
            Transform foreArm = RequirePath(targetRoot, RightForeArmPath);
            Transform hand = RequirePath(targetRoot, RightHandPath);
            Bounds bounds = CalculateBounds(behaviour.SprayHolder);
            bounds.Encapsulate(upperArm.position);
            bounds.Encapsulate(foreArm.position);
            bounds.Encapsulate(hand.position);
            bounds.Expand(0.13f);
            return bounds;
        }

        private static Bounds FullCharacterBounds(
            Transform targetRoot,
            HoloSprayRightHandFollowBehaviour behaviour)
        {
            Transform armature = targetRoot.Find("Armature") ??
                throw new InvalidOperationException("HoloSpray Armature is missing.");
            Transform[] bones = armature.GetComponentsInChildren<Transform>(true);
            Bounds bounds = new Bounds(bones[0].position, Vector3.zero);
            foreach (Transform bone in bones) bounds.Encapsulate(bone.position);
            foreach (Renderer renderer in behaviour.SprayHolder
                         .GetComponentsInChildren<Renderer>(true))
                bounds.Encapsulate(renderer.bounds);
            bounds.Expand(0.2f);
            return bounds;
        }

        private static Texture2D CaptureTargetView(
            Scene scene,
            Bounds bounds,
            Vector3 directionFromTarget)
        {
            var cameraObject = new GameObject("HoloSpray_ReadOnlyCamera");
            var lightObject = new GameObject("HoloSpray_ReadOnlyLight");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            RenderTexture render = RenderTexture.GetTemporary(
                512, 512, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = bounds.extents.magnitude * 1.08f;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 6f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.025f, 0.03f, 0.04f, 1f);
                Vector3 direction = directionFromTarget.normalized;
                Vector3 cameraUp = Vector3.ProjectOnPlane(
                    targetUp(scene), direction).normalized;
                if (cameraUp.sqrMagnitude < 0.25f)
                    cameraUp = Vector3.ProjectOnPlane(Vector3.forward, direction).normalized;
                camera.transform.SetPositionAndRotation(
                    bounds.center + direction * 4f,
                    Quaternion.LookRotation(-direction, cameraUp));
                camera.targetTexture = render;
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.15f;
                light.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
                camera.Render();
                RenderTexture.active = render;
                var image = new Texture2D(512, 512, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Vector3 targetUp(Scene scene) =>
            FindUnique(scene, TargetName).transform.up;

        private static string ProtectedHierarchySignature(Transform root)
        {
            var result = new StringBuilder();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                         .OrderBy(item => TransformPath(item, root), StringComparer.Ordinal))
            {
                string path = TransformPath(item, root);
                if (path == "HoloSpray_Prop" ||
                    path.StartsWith("HoloSpray_Prop/", StringComparison.Ordinal) ||
                    path == RightArmPath ||
                    path.StartsWith(RightArmPath + "/", StringComparison.Ordinal))
                    continue;
                AppendTransformSignature(result, item, path);
            }
            return result.ToString();
        }

        private static string FullHierarchySignature(Transform root)
        {
            var result = new StringBuilder();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                         .OrderBy(item => TransformPath(item, root), StringComparer.Ordinal))
            {
                string path = TransformPath(item, root);
                if (path == "Speaker_Prop" ||
                    path.StartsWith("Speaker_Prop/", StringComparison.Ordinal))
                    continue;
                AppendTransformSignature(result, item, path);
            }
            return result.ToString();
        }

        private static void AppendTransformSignature(
            StringBuilder result,
            Transform item,
            string path)
        {
            result.Append(path).Append('|').Append(item.gameObject.activeSelf).Append('|')
                .Append(item.GetSiblingIndex()).Append('|').Append(Vec(item.localPosition))
                .Append('|').Append(Quat(item.localRotation)).Append('|')
                .Append(Vec(item.localScale)).AppendLine();
            foreach (Renderer renderer in item.GetComponents<Renderer>())
                result.Append("R|").Append(path).Append('|').Append(renderer.enabled)
                    .Append('|').Append(AssetIdentity(renderer is SkinnedMeshRenderer skinned
                        ? skinned.sharedMesh
                        : item.GetComponent<MeshFilter>()?.sharedMesh))
                    .Append('|').Append(string.Join(",",
                        renderer.sharedMaterials.Select(AssetIdentity))).AppendLine();
        }

        private static string AssetIdentity(UnityEngine.Object asset) =>
            asset == null ? "null" : AssetDatabase.GetAssetPath(asset) + "#" + asset.name;

        private static string[] TexturePaths() =>
            new[]
            {
                BaseColorTexturePath,
                NormalTexturePath,
                MetallicTexturePath,
                RoughnessTexturePath
            }
                .Where(path => AssetDatabase.LoadAssetAtPath<Texture>(path) != null)
                .ToArray();

        private static string BaselineValue(string[] lines, string key)
        {
            string prefix = key + "=";
            string line = lines.SingleOrDefault(item =>
                item.StartsWith(prefix, StringComparison.Ordinal));
            if (line == null)
                throw new InvalidOperationException(
                    "HoloSpray baseline is missing " + key + ".");
            return line.Substring(prefix.Length);
        }

        private static string ComputeStringHash(string value)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(
                    sha.ComputeHash(Encoding.UTF8.GetBytes(value)))
                    .Replace("-", string.Empty);
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " changed unexpectedly.");
        }

        private static Transform RequirePath(Transform root, string path) =>
            root.Find(path) ??
            throw new InvalidOperationException(root.name + " path is missing: " + path);

        private static Texture RequireTexture(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<Texture>(assetPath) ??
                throw new InvalidOperationException("Texture failed to load: " + assetPath);
        }

        private static void ConfigureTextureImporter(
            string assetPath,
            TextureImporterType textureType,
            bool sRgb)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter ??
                throw new InvalidOperationException(
                    "TextureImporter is missing: " + assetPath);
            bool requiresChange = importer.textureType != textureType ||
                importer.sRGBTexture != sRgb || importer.isReadable;
            if (assetPath == MetallicSmoothnessTexturePath)
                requiresChange |= importer.alphaSource != TextureImporterAlphaSource.FromInput ||
                    importer.alphaIsTransparency;
            if (!requiresChange) return;

            importer.textureType = textureType;
            importer.sRGBTexture = sRgb;
            importer.isReadable = false;
            importer.alphaIsTransparency = false;
            if (assetPath == MetallicSmoothnessTexturePath)
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.SaveAndReimport();
        }

        private static void WriteMetallicSmoothnessTexture()
        {
            Texture2D metallic = LoadLinearImage(MetallicTexturePath);
            Texture2D roughness = LoadLinearImage(RoughnessTexturePath);
            Texture2D packed = null;
            try
            {
                packed = new Texture2D(
                    metallic.width,
                    metallic.height,
                    TextureFormat.RGBA32,
                    false,
                    true);
                var pixels = new Color[metallic.width * metallic.height];
                for (int y = 0; y < metallic.height; y++)
                {
                    for (int x = 0; x < metallic.width; x++)
                    {
                        float u = (x + 0.5f) / metallic.width;
                        float v = (y + 0.5f) / metallic.height;
                        float metallicValue = metallic.GetPixelBilinear(u, v).r;
                        float smoothness = 1f - roughness.GetPixelBilinear(u, v).r;
                        pixels[y * metallic.width + x] = new Color(
                            metallicValue,
                            metallicValue,
                            metallicValue,
                            smoothness);
                    }
                }
                packed.SetPixels(pixels);
                packed.Apply(false, false);
                File.WriteAllBytes(
                    Absolute(MetallicSmoothnessTexturePath), packed.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(metallic);
                UnityEngine.Object.DestroyImmediate(roughness);
                if (packed != null) UnityEngine.Object.DestroyImmediate(packed);
            }

            AssetDatabase.ImportAsset(
                MetallicSmoothnessTexturePath,
                ImportAssetOptions.ForceSynchronousImport);
            ConfigureTextureImporter(
                MetallicSmoothnessTexturePath, TextureImporterType.Default, false);
        }

        private static Texture2D LoadLinearImage(string assetPath)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            if (!ImageConversion.LoadImage(
                    texture, File.ReadAllBytes(Absolute(assetPath)), false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException(
                    "Texture image data failed to load: " + assetPath);
            }
            return texture;
        }

        private static bool MaterialPipelineIsComplete()
        {
            TextureImporter normalImporter =
                AssetImporter.GetAtPath(NormalTexturePath) as TextureImporter;
            TextureImporter metallicImporter =
                AssetImporter.GetAtPath(MetallicTexturePath) as TextureImporter;
            TextureImporter roughnessImporter =
                AssetImporter.GetAtPath(RoughnessTexturePath) as TextureImporter;
            TextureImporter packedImporter =
                AssetImporter.GetAtPath(MetallicSmoothnessTexturePath) as TextureImporter;
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialAssetPath);
            Texture metallicSmoothness =
                AssetDatabase.LoadAssetAtPath<Texture>(MetallicSmoothnessTexturePath);
            return normalImporter != null &&
                normalImporter.textureType == TextureImporterType.NormalMap &&
                !normalImporter.sRGBTexture && metallicImporter != null &&
                !metallicImporter.sRGBTexture && roughnessImporter != null &&
                !roughnessImporter.sRGBTexture && packedImporter != null &&
                !packedImporter.sRGBTexture && metallicSmoothness != null &&
                material != null && material.shader != null &&
                material.shader.name == "Universal Render Pipeline/Lit" &&
                material.GetTexture("_BaseMap") ==
                    AssetDatabase.LoadAssetAtPath<Texture>(BaseColorTexturePath) &&
                material.GetTexture("_BumpMap") ==
                    AssetDatabase.LoadAssetAtPath<Texture>(NormalTexturePath) &&
                material.GetTexture("_MetallicGlossMap") == metallicSmoothness &&
                material.IsKeywordEnabled("_NORMALMAP") &&
                material.IsKeywordEnabled("_METALLICSPECGLOSSMAP");
        }

        private static void AssertMaterialPipelineComplete()
        {
            if (!MaterialPipelineIsComplete())
                throw new InvalidOperationException(
                    "Holographic spray URP material pipeline is incomplete.");
        }

        private static void EnsureAssetFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must be active. ActiveScene=" + scene.path);
            return scene;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "HoloSpray source inspection requires Edit Mode.");
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

        private static string ComputeAssetHash(string assetPath)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(Absolute(assetPath)))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string TransformPath(Transform item, Transform root) =>
            AnimationUtility.CalculateTransformPath(item, root);

        private static void WriteText(string name, string contents)
        {
            Directory.CreateDirectory(Absolute(OutputFolder));
            File.WriteAllText(
                Path.Combine(Absolute(OutputFolder), name), contents, Encoding.UTF8);
        }

        private static string Absolute(string path)
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Project root is unavailable.");
            return Path.GetFullPath(Path.Combine(
                root, path.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string Num(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture);
        private static string Vec(Vector3 value) =>
            Num(value.x) + "," + Num(value.y) + "," + Num(value.z);
        private static string Quat(Quaternion value) =>
            Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + "," + Num(value.w);

        private readonly struct PoseState
        {
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            internal PoseState(Transform value)
            {
                localPosition = value.localPosition;
                localRotation = value.localRotation;
                localScale = value.localScale;
            }

            internal void RequireExact(Transform value, string label)
            {
                if (Vector3.Distance(localPosition, value.localPosition) > PositionTolerance ||
                    Quaternion.Angle(localRotation, value.localRotation) > RotationTolerance ||
                    Vector3.Distance(localScale, value.localScale) > PositionTolerance)
                    throw new InvalidOperationException(label + " changed unexpectedly.");
            }
        }
    }
}
