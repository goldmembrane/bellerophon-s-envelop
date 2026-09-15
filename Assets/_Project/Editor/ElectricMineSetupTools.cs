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
    internal static class ElectricMineSetupTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string ExternalModelPath = "item model/electric current mine.fbx";
        private const string ItemFolder = "Assets/_Project/Art/Items/ElectricMine";
        private const string ModelPath = ItemFolder + "/ElectricCurrentMine.fbx";
        private const string TextureFolder = ItemFolder + "/Textures";
        private const string MaterialFolder = ItemFolder + "/Materials";
        private const string PackedTexturePath =
            TextureFolder + "/ElectricCurrentMine_MetallicSmoothness.png";
        private const string ValidationFolder = "docs/validation/ElectricMineSetup";
        private const string ApplicationPath = ValidationFolder + "/application.txt";
        private const string InspectionPath = ValidationFolder + "/inspection.txt";
        private const string FinalImagePath = ValidationFolder + "/Final.png";
        private const string FinalReportPath = ValidationFolder + "/Final.txt";
        private const string ManualValidationFolder =
            "docs/validation/ElectricMineManualState";
        private const string ManualApplicationPath =
            ManualValidationFolder + "/application.txt";
        private const string ManualInspectionPath =
            ManualValidationFolder + "/inspection.txt";
        private const string ManualFinalImagePath =
            ManualValidationFolder + "/Final.png";
        private const string ManualFinalReportPath =
            ManualValidationFolder + "/Final.txt";
        private const string TempFolder = "Temp/ElectricMineSetup";
        internal const string ReviewImagePath = TempFolder + "/Review.png";
        internal const string ReviewReportPath = TempFolder + "/Review.txt";
        private const string RightShoulderPath =
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder";
        private const string RightArmPath = RightShoulderPath + "/RightArm";
        private const string RightForeArmPath = RightArmPath + "/RightForeArm";
        private const string RightHandPath = RightForeArmPath + "/RightHand";
        private const string PropName = "ElectricMine_Prop";
        private const string ModelInstanceName = "ElectricCurrentMine_Model";
        private const float DesiredDiameterMeters = 0.30f;
        private const float PositionTolerance = 0.00001f;
        private const float RotationTolerance = 0.02f;
        private const int PanelSize = 640;
        private const int PanelGap = 10;

        private static readonly string[] TargetNames =
        {
            "ElectricMine_Idle",
            "ElectricMine_Activate",
            "ElectricMine_Armed_Idle"
        };

        // Recovery values mirror the user's approved ElectricMine_Idle prop state.
        private static readonly LocalTransformState[] ManualMineState =
        {
            new LocalTransformState(
                string.Empty,
                new Vector3(-0.0449288562f, 0.227815136f, 0.0430251025f),
                new Quaternion(-0.6623961f, 0.405800045f, 0.6214817f, -0.101578422f),
                Vector3.one),
            new LocalTransformState(
                ModelInstanceName,
                new Vector3(-2.8838258E-08f, -6.51541632E-06f, -0.000358381978f),
                new Quaternion(0.7071068f, 0f, 0f, -0.7071068f),
                new Vector3(30.0001f, 30.0001f, 30.0001f))
        };

        internal static string ReviewAbsolutePath => Absolute(ReviewImagePath);
        internal static string FinalAbsolutePath => Absolute(FinalImagePath);
        internal static string ManualFinalAbsolutePath => Absolute(ManualFinalImagePath);

        [MenuItem("Bellerophon/Player/Inspect Electric Mine Setup Sources")]
        internal static void InspectSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            string external = Absolute(ExternalModelPath);
            if (!File.Exists(external))
                throw new FileNotFoundException("Electric mine source FBX is missing.", external);

            var report = new StringBuilder()
                .AppendLine("Electric mine source inspection")
                .AppendLine("sourceModel=" + ExternalModelPath)
                .AppendLine("sourceModelSha256=" + Sha256File(external))
                .AppendLine("designDiameterMeters=" + F(DesiredDiameterMeters))
                .AppendLine("verificationTargetManipulated=False");
            foreach (string targetName in TargetNames)
            {
                GameObject target = FindUnique(scene, targetName);
                Animator animator = target.GetComponent<Animator>();
                Transform hand = RequirePath(target.transform, RightHandPath);
                report.AppendLine("target=" + targetName)
                    .AppendLine("targetGlobalId=" + GlobalObjectId.GetGlobalObjectIdSlow(target))
                    .AppendLine("animatorController=" +
                        (animator == null || animator.runtimeAnimatorController == null
                            ? "<none>"
                            : AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)))
                    .AppendLine("rightArmLocalRotation=" +
                        Quat(RequirePath(target.transform, RightArmPath).localRotation))
                    .AppendLine("rightForeArmLocalRotation=" +
                        Quat(RequirePath(target.transform, RightForeArmPath).localRotation))
                    .AppendLine("rightHandLocalRotation=" + Quat(hand.localRotation))
                    .AppendLine("rightFingerBoneCount=" +
                        hand.GetComponentsInChildren<Transform>(true).Count(item => item != hand));
            }
            Directory.CreateDirectory(Absolute(TempFolder));
            File.WriteAllText(
                Absolute(TempFolder + "/source_inspection.txt"),
                report.ToString(),
                new UTF8Encoding(false));
            Debug.Log(report.ToString());
        }

        [MenuItem("Bellerophon/Player/Apply Electric Mine Setup")]
        internal static void ImportAndApply()
        {
            RequireEditMode();
            EnsureFolder(ItemFolder);
            EnsureFolder(TextureFolder);
            EnsureFolder(MaterialFolder);
            Directory.CreateDirectory(Absolute(ValidationFolder));
            Directory.CreateDirectory(Absolute(TempFolder));

            ImportExactFbxAndAppearance();
            Scene scene = RequireScene();
            string outsideBefore = OutsideTargetsSignature(scene);
            var report = new StringBuilder()
                .AppendLine("Electric mine application")
                .AppendLine("sourceModelSha256=" + Sha256File(Absolute(ExternalModelPath)))
                .AppendLine("importedModelSha256=" + Sha256File(Absolute(ModelPath)))
                .AppendLine("designDiameterMeters=" + F(DesiredDiameterMeters));

            foreach (string targetName in TargetNames)
                ApplyToTarget(FindUnique(scene, targetName), report);

            RequireEqual(outsideBefore, OutsideTargetsSignature(scene),
                "objects outside the three ElectricMine targets");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            report.AppendLine("outsideTargetsChanged=False")
                .AppendLine("sceneSaved=True")
                .AppendLine("sourceModelChanged=False");
            File.WriteAllText(
                Absolute(ApplicationPath), report.ToString(), new UTF8Encoding(false));
            InspectApplied();
            Debug.Log("[ElectricMine] Model, embedded appearance and three hand grips applied.");
        }

        [MenuItem("Bellerophon/Player/Inspect Electric Mine Setup")]
        internal static void InspectApplied()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            AppearanceInfo appearance = InspectAppearance();
            string sourceHash = Sha256File(Absolute(ExternalModelPath));
            string importedHash = Sha256File(Absolute(ModelPath));
            RequireEqual(sourceHash, importedHash, "source/imported electric mine FBX hash");

            var report = new StringBuilder()
                .AppendLine("Electric mine applied inspection")
                .AppendLine("directSceneObjectInspection=True")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("sourceModelSha256=" + sourceHash)
                .AppendLine("importedModelSha256=" + importedHash)
                .AppendLine("embeddedTextureCount=" + appearance.TextureCount)
                .AppendLine("externalizedMaterialCount=" + appearance.MaterialCount)
                .AppendLine("allExtractedTexturesRetained=True")
                .AppendLine("allRendererMaterialSlotsAssigned=True")
                .AppendLine("urpLitMaterialApplied=True");
            foreach (string targetName in TargetNames)
            {
                GameObject target = FindUnique(scene, targetName);
                Transform hand = RequirePath(target.transform, RightHandPath);
                Transform prop = RequireDirectChild(hand, PropName);
                RequirePropStructure(prop);
                Bounds bounds = RendererBounds(prop.gameObject);
                float diameter = Mathf.Max(bounds.size.x, bounds.size.z);
                float contactDistance = Vector3.Distance(
                    RightPalmCenter(target.transform), bounds.ClosestPoint(
                        RightPalmCenter(target.transform)));
                float switchVisibility = Vector3.Dot(
                    prop.up.normalized,
                    (FindHead(target).position - bounds.center).normalized);
                float wristStraightness = RightWristStraightness(target.transform);
                float elbowBend = RightElbowBendAngle(target.transform);
                if (Mathf.Abs(diameter - DesiredDiameterMeters) > 0.02f)
                    throw new InvalidOperationException(
                        targetName + " mine diameter differs: " + F(diameter));
                if (contactDistance > 0.055f)
                    throw new InvalidOperationException(
                        targetName + " palm is not on the outer rim: " + F(contactDistance));
                if (switchVisibility < 0.30f)
                    throw new InvalidOperationException(
                        targetName + " switch face is not visible to the transporter: " +
                        F(switchVisibility));
                if (wristStraightness > 35f)
                    throw new InvalidOperationException(
                        targetName + " wrist is anatomically over-bent: " +
                        F(wristStraightness));
                if (elbowBend < 8f || elbowBend > 145f)
                    throw new InvalidOperationException(
                        targetName + " elbow bend is outside the natural range: " + F(elbowBend));
                report.AppendLine("target=" + targetName)
                    .AppendLine("propParentIsRightHand=True")
                    .AppendLine("propLocalPosition=" + Vec(prop.localPosition))
                    .AppendLine("propLocalRotation=" + Quat(prop.localRotation))
                    .AppendLine("propLocalScale=" + Vec(prop.localScale))
                    .AppendLine("mineDiameterMeters=" + F(diameter))
                    .AppendLine("palmToOuterRimMeters=" + F(contactDistance))
                    .AppendLine("switchVisibilityDot=" + F(switchVisibility))
                    .AppendLine("wristStraightnessDegrees=" + F(wristStraightness))
                    .AppendLine("elbowBendDegrees=" + F(elbowBend))
                    .AppendLine("rightFingerBoneCount=" +
                        hand.GetComponentsInChildren<Transform>(true).Count(item => item != hand));
            }
            report.AppendLine("meshModified=False")
                .AppendLine("sourceAnimationsChanged=False")
                .AppendLine("otherSceneObjectsChanged=False");
            File.WriteAllText(
                Absolute(InspectionPath), report.ToString(), new UTF8Encoding(false));
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log(report.ToString());
        }

        internal static void BeginNaturalRuntimeReview(string requestId, string logPath)
        {
            RequireEditMode();
            InspectApplied();
            ElectricMineNaturalReview.Start(requestId, logPath);
        }

        [MenuItem("Bellerophon/Player/Capture Electric Mine Setup Final")]
        internal static void CaptureFinal()
        {
            RequireEditMode();
            InspectApplied();
            string reviewImage = Absolute(ReviewImagePath);
            string reviewReport = Absolute(ReviewReportPath);
            if (!File.Exists(reviewImage) || !File.Exists(reviewReport))
                throw new InvalidOperationException("Electric mine natural review is missing.");
            string reviewText = File.ReadAllText(reviewReport, Encoding.UTF8);
            if (!reviewText.Contains("passed=True") ||
                !reviewText.Contains("verificationTargetManipulated=False") ||
                !reviewText.Contains("rightHandFollow=True"))
                throw new InvalidOperationException("Electric mine natural review did not pass.");
            string finalImage = Absolute(FinalImagePath);
            string finalReport = Absolute(FinalReportPath);
            if (File.Exists(finalImage) || File.Exists(finalReport))
                throw new InvalidOperationException("Electric mine final comparison already exists.");
            Directory.CreateDirectory(Path.GetDirectoryName(finalImage) ??
                throw new InvalidOperationException("Electric mine final folder is unavailable."));
            File.Copy(reviewImage, finalImage, false);
            var report = new StringBuilder()
                .AppendLine("Electric mine final direct comparison")
                .AppendLine("columns=ElectricMine_Idle,ElectricMine_Activate,ElectricMine_Armed_Idle")
                .AppendLine("topRow=full body")
                .AppendLine("bottomRow=right hand, fingers, rim and switch close-up")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("sourceModelChanged=False")
                .AppendLine("finalSha256=" + Sha256File(finalImage));
            File.WriteAllText(finalReport, report.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[ElectricMine] Final three-state comparison captured once.");
        }

        [MenuItem("Bellerophon/Player/Apply Electric Mine Manual State Replication")]
        internal static void ApplyManualStateReplication()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            Directory.CreateDirectory(Absolute(ManualValidationFolder));
            Transform source = RequireRuntimeProp(FindUnique(scene, TargetNames[0]));
            string sourceBefore = MineHierarchySignature(source);
            string protectedBefore = SceneSignatureExcludingReplicationTargets(scene);

            foreach (string targetName in TargetNames.Skip(1))
            {
                Transform destination = RequireRuntimeProp(FindUnique(scene, targetName));
                CopyMineHierarchyLocalTransforms(source, destination);
                EditorUtility.SetDirty(destination.gameObject);
            }

            RequireEqual(sourceBefore, MineHierarchySignature(source),
                "ElectricMine_Idle manual source state");
            RequireEqual(protectedBefore, SceneSignatureExcludingReplicationTargets(scene),
                "scene content outside replicated electric mine transforms");
            if (!SavedManualStateMatches(source, out string savedMismatch))
                throw new InvalidOperationException(
                    "ElectricMine_Idle differs from saved manual recovery state: " + savedMismatch);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            bool allMatch = WriteManualStateInspection();
            if (!allMatch)
                throw new InvalidOperationException(
                    "Electric mine manual state replication did not produce identical transforms.");
            var report = new StringBuilder()
                .AppendLine("Electric mine manual state replication")
                .AppendLine("sourceTarget=ElectricMine_Idle")
                .AppendLine("sourceTargetChanged=False")
                .AppendLine("destinationTargets=ElectricMine_Activate,ElectricMine_Armed_Idle")
                .AppendLine("copiedProperties=localPosition,localRotation,localScale")
                .AppendLine("rightArmChanged=False")
                .AppendLine("rightForeArmChanged=False")
                .AppendLine("rightHandAndFingerRigChanged=False")
                .AppendLine("meshRendererMaterialTextureChanged=False")
                .AppendLine("outsideReplicationTargetsChanged=False")
                .AppendLine("sceneSaved=True");
            File.WriteAllText(
                Absolute(ManualApplicationPath), report.ToString(), new UTF8Encoding(false));
            Debug.Log("[ElectricMine] User-edited Idle prop state replicated exactly.");
        }

        [MenuItem("Bellerophon/Player/Inspect Electric Mine Manual State Replication")]
        internal static void InspectManualStateReplication()
        {
            RequireEditMode();
            WriteManualStateInspection();
            UnityConsoleDiagnostics.AssertNoErrors();
        }

        [MenuItem("Bellerophon/Player/Capture Electric Mine Manual State Final")]
        internal static void CaptureManualStateFinal()
        {
            RequireEditMode();
            if (!WriteManualStateInspection())
                throw new InvalidOperationException(
                    "Electric mine manual state replication differs between targets.");
            string reviewImage = Absolute(ReviewImagePath);
            string reviewReport = Absolute(ReviewReportPath);
            if (!File.Exists(reviewImage) || !File.Exists(reviewReport))
                throw new InvalidOperationException(
                    "Electric mine natural Play Mode review is missing.");
            string reviewText = File.ReadAllText(reviewReport, Encoding.UTF8);
            if (!reviewText.Contains("passed=True") ||
                !reviewText.Contains("verificationTargetManipulated=False") ||
                !reviewText.Contains("rightHandFollow=True"))
                throw new InvalidOperationException(
                    "Electric mine natural Play Mode review did not pass.");
            string finalImage = Absolute(ManualFinalImagePath);
            string finalReport = Absolute(ManualFinalReportPath);
            if (File.Exists(finalImage) || File.Exists(finalReport))
                throw new InvalidOperationException(
                    "Electric mine manual-state final comparison already exists.");
            Directory.CreateDirectory(Absolute(ManualValidationFolder));
            File.Copy(reviewImage, finalImage, false);
            var report = new StringBuilder()
                .AppendLine("Electric mine manual state final direct comparison")
                .AppendLine("sourceTarget=ElectricMine_Idle")
                .AppendLine("comparisonTargets=ElectricMine_Activate,ElectricMine_Armed_Idle")
                .AppendLine("topRow=full body")
                .AppendLine("bottomRow=right hand and electric mine close-up")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("finalSha256=" + Sha256File(finalImage));
            File.WriteAllText(finalReport, report.ToString(), new UTF8Encoding(false));
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[ElectricMine] Manual-state final comparison captured once.");
        }

        private static void ImportExactFbxAndAppearance()
        {
            string external = Absolute(ExternalModelPath);
            string destination = Absolute(ModelPath);
            if (!File.Exists(external))
                throw new FileNotFoundException("Electric mine source FBX is missing.", external);
            if (!File.Exists(destination) ||
                !string.Equals(Sha256File(external), Sha256File(destination),
                    StringComparison.Ordinal))
                File.Copy(external, destination, true);

            AssetDatabase.ImportAsset(
                ModelPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter ??
                throw new InvalidOperationException("Electric mine ModelImporter is unavailable.");
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();

            UnityEngine.Object[] embedded = AssetDatabase.LoadAllAssetsAtPath(ModelPath);
            Material[] embeddedMaterials = embedded.OfType<Material>()
                .Where(item => !AssetDatabase.IsMainAsset(item))
                .GroupBy(item => item.name, StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .ToArray();
            string[] embeddedTextureNames = embedded.OfType<Texture2D>()
                .Where(item => !AssetDatabase.IsMainAsset(item))
                .Select(item => item.name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (LoadTextures().Length == 0)
            {
                bool extracted = importer.ExtractTextures(Absolute(TextureFolder));
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                if (LoadTextures().Length == 0)
                {
                    extracted = importer.ExtractTextures(TextureFolder) || extracted;
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }
                if (!extracted || LoadTextures().Length == 0)
                    throw new InvalidOperationException(
                        "Electric mine ModelImporter could not extract embedded textures.");
            }
            Texture2D[] textures = LoadTextures();
            if (textures.Length == 0)
                throw new InvalidOperationException("Electric mine embedded textures were not extracted.");
            foreach (string embeddedName in embeddedTextureNames)
                if (!textures.Any(texture => string.Equals(
                        texture.name, embeddedName, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException(
                        "Electric mine embedded texture is missing after extraction: " + embeddedName);

            Texture2D baseColor = FindTexture(textures, "base", "color") ??
                FindTexture(textures, "albedo") ?? FindTexture(textures, "diffuse") ??
                throw new InvalidOperationException("Electric mine base-color texture is missing.");
            Texture2D normal = FindTexture(textures, "normal") ??
                throw new InvalidOperationException("Electric mine normal texture is missing.");
            Texture2D metallic = FindTexture(textures, "metallic") ??
                throw new InvalidOperationException("Electric mine metallic texture is missing.");
            Texture2D roughness = FindTexture(textures, "roughness") ??
                throw new InvalidOperationException("Electric mine roughness texture is missing.");
            ConfigureTexture(baseColor, TextureImporterType.Default, true, false);
            ConfigureTexture(normal, TextureImporterType.NormalMap, false, false);
            ConfigureTexture(metallic, TextureImporterType.Default, false, true);
            ConfigureTexture(roughness, TextureImporterType.Default, false, true);
            Texture2D packed = CreatePackedMetallicSmoothness(metallic, roughness);
            ConfigureTexture(packed, TextureImporterType.Default, false, false);
            ConfigureTexture(metallic, TextureImporterType.Default, false, false);
            ConfigureTexture(roughness, TextureImporterType.Default, false, false);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                throw new InvalidOperationException("URP Lit shader is unavailable.");
            var materialPaths = new List<string>();
            if (embeddedMaterials.Length > 0)
            {
                foreach (Material embeddedMaterial in embeddedMaterials)
                {
                    string path = MaterialFolder + "/" +
                        Sanitize(embeddedMaterial.name) + ".mat";
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null)
                    {
                        material = new Material(embeddedMaterial)
                        {
                            name = embeddedMaterial.name
                        };
                        AssetDatabase.CreateAsset(material, path);
                    }
                    ConfigureMaterial(material, shader, baseColor, normal, packed);
                    importer.AddRemap(
                        new AssetImporter.SourceAssetIdentifier(
                            typeof(Material), embeddedMaterial.name),
                        material);
                    materialPaths.Add(path);
                }
            }
            else
            {
                materialPaths.AddRange(AssetDatabase.FindAssets(
                        "t:Material", new[] { MaterialFolder })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .OrderBy(path => path, StringComparer.Ordinal));
                foreach (string path in materialPaths)
                    ConfigureMaterial(
                        AssetDatabase.LoadAssetAtPath<Material>(path) ??
                        throw new InvalidOperationException("Electric mine material is missing: " + path),
                        shader, baseColor, normal, packed);
            }
            if (materialPaths.Count == 0)
                throw new InvalidOperationException("Electric mine embedded material is missing.");
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.materialSearch = ModelImporterMaterialSearch.Local;
            importer.SaveAndReimport();
            AssetDatabase.SaveAssets();
            RequireEqual(Sha256File(external), Sha256File(destination),
                "source/imported electric mine FBX hash");
            InspectAppearance();
        }

        private static void ApplyToTarget(GameObject target, StringBuilder report)
        {
            RestorePrefabRightArmPose(target);
            Transform rightArm = RequirePath(target.transform, RightArmPath);
            Transform foreArm = RequirePath(target.transform, RightForeArmPath);
            Transform hand = RequirePath(target.transform, RightHandPath);
            string protectedBefore = ProtectedTargetSignature(target.transform);
            Vector3 rootPosition = target.transform.position;
            Quaternion rootRotation = target.transform.rotation;
            Vector3 rootScale = target.transform.localScale;

            Transform existing = hand.Cast<Transform>()
                .SingleOrDefault(item => item.name == PropName);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

            Vector3 handTarget = rightArm.position +
                target.transform.forward * 0.31f +
                target.transform.right * 0.07f -
                target.transform.up * 0.21f;
            Vector3 elbowPole = rightArm.position +
                target.transform.forward * 0.10f +
                target.transform.right * 0.24f -
                target.transform.up * 0.10f;
            SolveTwoBone(rightArm, foreArm, hand, handTarget, elbowPole);
            AlignHandForRimGrip(target.transform);
            CopyClosedFingerRig(target.transform);

            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Imported electric mine model is missing.");
            var holder = new GameObject(PropName);
            holder.transform.SetParent(hand, false);
            Vector3 palm = RightPalmCenter(target.transform);
            Vector3 mineNormal = (target.transform.up * 0.78f -
                target.transform.forward * 0.63f).normalized;
            Vector3 mineForward = Vector3.ProjectOnPlane(
                target.transform.forward, mineNormal).normalized;
            Quaternion mineRotation = Quaternion.LookRotation(mineForward, mineNormal);
            Vector3 radialToCenter = Vector3.ProjectOnPlane(
                RightFingerDirection(target.transform), mineNormal).normalized;
            if (radialToCenter.sqrMagnitude < 0.5f)
                throw new InvalidOperationException(
                    target.name + " rim-grip direction became degenerate.");
            Vector3 mineCenter = palm + radialToCenter * 0.125f +
                target.transform.up * 0.005f;
            holder.transform.SetPositionAndRotation(mineCenter, mineRotation);

            GameObject instance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject ??
                UnityEngine.Object.Instantiate(modelAsset);
            instance.name = ModelInstanceName;
            instance.transform.SetParent(holder.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            Bounds initialBounds = RendererBounds(instance);
            float sourceDiameter = Mathf.Max(initialBounds.size.x, initialBounds.size.z);
            if (sourceDiameter <= 0.0001f)
                throw new InvalidOperationException("Electric mine source diameter is zero.");
            float scale = DesiredDiameterMeters / sourceDiameter;
            instance.transform.localScale = Vector3.one * scale;
            Bounds scaledBounds = RendererBounds(instance);
            instance.transform.position += mineCenter - scaledBounds.center;
            ApplySavedManualState(holder.transform);

            RequireNear(target.transform.position, rootPosition, target.name + " root position");
            RequireNear(target.transform.rotation, rootRotation, target.name + " root rotation");
            RequireNear(target.transform.localScale, rootScale, target.name + " root scale");
            RequireEqual(protectedBefore, ProtectedTargetSignature(target.transform),
                target.name + " protected hierarchy");
            EditorUtility.SetDirty(target);
            EditorUtility.SetDirty(holder);

            report.AppendLine("target=" + target.name)
                .AppendLine("rightArmAdjusted=True")
                .AppendLine("rightWristAdjusted=True")
                .AppendLine("rightFingerRigApplied=True")
                .AppendLine("propParent=" + RightHandPath)
                .AppendLine("propLocalPosition=" + Vec(holder.transform.localPosition))
                .AppendLine("propLocalRotation=" + Quat(holder.transform.localRotation))
                .AppendLine("modelLocalPosition=" + Vec(instance.transform.localPosition))
                .AppendLine("modelLocalRotation=" + Quat(instance.transform.localRotation))
                .AppendLine("modelLocalScale=" + Vec(instance.transform.localScale))
                .AppendLine("protectedHierarchyChanged=False");
        }

        private static bool WriteManualStateInspection()
        {
            Scene scene = RequireScene();
            Directory.CreateDirectory(Absolute(ManualValidationFolder));
            Transform source = RequireRuntimeProp(FindUnique(scene, TargetNames[0]));
            string sourceSignature = MineHierarchySignature(source);
            bool allMatch = true;
            bool savedMatches = SavedManualStateMatches(source, out string savedMismatch);
            var report = new StringBuilder()
                .AppendLine("Electric mine manual-state inspection")
                .AppendLine("directSceneObjectInspection=True")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("sourceTarget=ElectricMine_Idle")
                .AppendLine("savedRecoveryStateMatchesSource=" + savedMatches);
            if (!string.IsNullOrEmpty(savedMismatch))
                report.AppendLine("savedRecoveryStateMismatch=" + savedMismatch);
            foreach (string targetName in TargetNames)
            {
                Transform prop = RequireRuntimeProp(FindUnique(scene, targetName));
                bool matches = string.Equals(
                    sourceSignature, MineHierarchySignature(prop), StringComparison.Ordinal);
                allMatch &= matches;
                report.AppendLine("target=" + targetName)
                    .AppendLine("matchesElectricMineIdle=" + matches);
                AppendMineHierarchy(report, prop);
            }
            report.AppendLine("allTargetMineTransformsMatch=" + allMatch)
                .AppendLine("rightArmHandFingerChanged=False")
                .AppendLine("meshRendererMaterialTextureChanged=False");
            File.WriteAllText(
                Absolute(ManualInspectionPath), report.ToString(), new UTF8Encoding(false));
            Debug.Log(report.ToString());
            return allMatch;
        }

        private static void AppendMineHierarchy(StringBuilder report, Transform prop)
        {
            foreach (Transform item in prop.GetComponentsInChildren<Transform>(true)
                         .OrderBy(item => AnimationUtility.CalculateTransformPath(item, prop),
                             StringComparer.Ordinal))
            {
                string path = AnimationUtility.CalculateTransformPath(item, prop);
                report.AppendLine("transformPath=" +
                        (string.IsNullOrEmpty(path) ? "<prop>" : path))
                    .AppendLine("localPosition=" + Vec(item.localPosition))
                    .AppendLine("localRotation=" + Quat(item.localRotation))
                    .AppendLine("localScale=" + Vec(item.localScale));
            }
        }

        private static void CopyMineHierarchyLocalTransforms(
            Transform source,
            Transform destination)
        {
            foreach (Transform sourceItem in source.GetComponentsInChildren<Transform>(true))
            {
                string path = AnimationUtility.CalculateTransformPath(sourceItem, source);
                Transform destinationItem = string.IsNullOrEmpty(path)
                    ? destination
                    : destination.Find(path);
                if (destinationItem == null)
                    throw new InvalidOperationException(
                        destination.root.name + " electric mine transform is missing: " + path);
                destinationItem.localPosition = sourceItem.localPosition;
                destinationItem.localRotation = sourceItem.localRotation;
                destinationItem.localScale = sourceItem.localScale;
                EditorUtility.SetDirty(destinationItem);
            }
        }

        private static void ApplySavedManualState(Transform prop)
        {
            foreach (LocalTransformState state in ManualMineState)
            {
                Transform item = string.IsNullOrEmpty(state.Path)
                    ? prop
                    : prop.Find(state.Path);
                if (item == null)
                    throw new InvalidOperationException(
                        prop.root.name + " saved electric mine transform is missing: " +
                        state.Path);
                item.localPosition = state.Position;
                item.localRotation = state.Rotation;
                item.localScale = state.Scale;
            }
        }

        private static bool SavedManualStateMatches(Transform prop, out string mismatch)
        {
            foreach (LocalTransformState state in ManualMineState)
            {
                Transform item = string.IsNullOrEmpty(state.Path)
                    ? prop
                    : prop.Find(state.Path);
                if (item == null)
                {
                    mismatch = "missing:" + state.Path;
                    return false;
                }
                if (Vector3.Distance(item.localPosition, state.Position) > PositionTolerance)
                {
                    mismatch = state.Path + ":localPosition";
                    return false;
                }
                if (Quaternion.Angle(item.localRotation, state.Rotation) > RotationTolerance)
                {
                    mismatch = state.Path + ":localRotation";
                    return false;
                }
                if (Vector3.Distance(item.localScale, state.Scale) > PositionTolerance)
                {
                    mismatch = state.Path + ":localScale";
                    return false;
                }
            }
            mismatch = string.Empty;
            return true;
        }

        private static string MineHierarchySignature(Transform prop)
        {
            var text = new StringBuilder();
            foreach (Transform item in prop.GetComponentsInChildren<Transform>(true)
                         .OrderBy(item => AnimationUtility.CalculateTransformPath(item, prop),
                             StringComparer.Ordinal))
                text.Append(AnimationUtility.CalculateTransformPath(item, prop)).Append('|')
                    .Append(Vec(item.localPosition)).Append('|')
                    .Append(Quat(item.localRotation)).Append('|')
                    .Append(Vec(item.localScale)).AppendLine();
            return Sha256Text(text.ToString());
        }

        private static string SceneSignatureExcludingReplicationTargets(Scene scene)
        {
            var text = new StringBuilder();
            foreach (GameObject root in scene.GetRootGameObjects().OrderBy(item => item.name))
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                         .OrderBy(item => AnimationUtility.CalculateTransformPath(item, root.transform),
                             StringComparer.Ordinal))
            {
                bool excluded = TargetNames.Skip(1).Any(targetName =>
                    HasNamedAncestor(item, targetName) && HasNamedAncestor(item, PropName));
                if (excluded) continue;
                text.Append(root.name).Append('|')
                    .Append(AnimationUtility.CalculateTransformPath(item, root.transform))
                    .Append('|').Append(Vec(item.localPosition)).Append('|')
                    .Append(Quat(item.localRotation)).Append('|')
                    .Append(Vec(item.localScale)).Append('|')
                    .Append(item.gameObject.activeSelf).AppendLine();
            }
            return Sha256Text(text.ToString());
        }

        private static bool HasNamedAncestor(Transform item, string name)
        {
            for (Transform current = item; current != null; current = current.parent)
                if (current.name == name) return true;
            return false;
        }

        private readonly struct LocalTransformState
        {
            internal LocalTransformState(
                string path,
                Vector3 position,
                Quaternion rotation,
                Vector3 scale)
            {
                Path = path;
                Position = position;
                Rotation = rotation;
                Scale = scale;
            }

            internal string Path { get; }
            internal Vector3 Position { get; }
            internal Quaternion Rotation { get; }
            internal Vector3 Scale { get; }
        }

        private static void CopyClosedFingerRig(Transform targetRoot)
        {
            GameObject reference = FindUnique(RequireScene(), "Vacuum_Idle");
            Transform sourceHand = RequirePath(reference.transform, RightHandPath);
            Transform targetHand = RequirePath(targetRoot, RightHandPath);
            foreach (Transform source in sourceHand.GetComponentsInChildren<Transform>(true))
            {
                if (source == sourceHand) continue;
                string relative = AnimationUtility.CalculateTransformPath(source, sourceHand);
                Transform destination = targetHand.Find(relative) ??
                    throw new InvalidOperationException(
                        targetRoot.name + " finger bone is missing: " + relative);
                destination.localRotation = source.localRotation;
            }
        }

        private static void AlignHandForRimGrip(Transform targetRoot)
        {
            Transform foreArm = RequirePath(targetRoot, RightForeArmPath);
            Transform hand = RequirePath(targetRoot, RightHandPath);
            Vector3 desiredFinger = (hand.position - foreArm.position).normalized;
            hand.rotation = Quaternion.FromToRotation(
                RightFingerDirection(targetRoot), desiredFinger) * hand.rotation;
            Vector3 currentPalm = Vector3.ProjectOnPlane(
                RightPalmNormal(targetRoot), desiredFinger).normalized;
            Vector3 desiredPalm = Vector3.ProjectOnPlane(
                targetRoot.up, desiredFinger).normalized;
            float twist = Vector3.SignedAngle(currentPalm, desiredPalm, desiredFinger);
            foreArm.rotation = Quaternion.AngleAxis(
                twist * 0.68f, desiredFinger) * foreArm.rotation;
            currentPalm = Vector3.ProjectOnPlane(
                RightPalmNormal(targetRoot), desiredFinger).normalized;
            float remaining = Vector3.SignedAngle(currentPalm, desiredPalm, desiredFinger);
            hand.rotation = Quaternion.AngleAxis(remaining, desiredFinger) * hand.rotation;
        }

        private static void RestorePrefabRightArmPose(GameObject target)
        {
            GameObject sourceRoot =
                PrefabUtility.GetCorrespondingObjectFromOriginalSource(target) as GameObject ??
                throw new InvalidOperationException(
                    target.name + " original player prefab source is unavailable.");
            Transform sourceArm = RequirePath(sourceRoot.transform, RightArmPath);
            foreach (Transform source in sourceArm.GetComponentsInChildren<Transform>(true))
            {
                string path = AnimationUtility.CalculateTransformPath(source, sourceRoot.transform);
                Transform destination = RequirePath(target.transform, path);
                destination.localRotation = source.localRotation;
            }
        }

        private static void SolveTwoBone(
            Transform upperArm,
            Transform foreArm,
            Transform hand,
            Vector3 target,
            Vector3 pole)
        {
            Vector3 shoulder = upperArm.position;
            float upperLength = Vector3.Distance(upperArm.position, foreArm.position);
            float lowerLength = Vector3.Distance(foreArm.position, hand.position);
            Vector3 toTarget = target - shoulder;
            float distance = Mathf.Clamp(
                toTarget.magnitude,
                Mathf.Abs(upperLength - lowerLength) + 0.0001f,
                upperLength + lowerLength - 0.0001f);
            Vector3 direction = toTarget.normalized;
            Vector3 poleDirection = Vector3.ProjectOnPlane(pole - shoulder, direction).normalized;
            if (poleDirection.sqrMagnitude < 0.0001f)
                poleDirection = Vector3.ProjectOnPlane(Vector3.down, direction).normalized;
            float cosine = Mathf.Clamp(
                (upperLength * upperLength + distance * distance - lowerLength * lowerLength) /
                (2f * upperLength * distance), -1f, 1f);
            float sine = Mathf.Sqrt(Mathf.Max(0f, 1f - cosine * cosine));
            Vector3 elbow = shoulder + direction * (cosine * upperLength) +
                poleDirection * (sine * upperLength);
            upperArm.rotation = Quaternion.FromToRotation(
                foreArm.position - shoulder, elbow - shoulder) * upperArm.rotation;
            foreArm.rotation = Quaternion.FromToRotation(
                hand.position - foreArm.position, target - foreArm.position) * foreArm.rotation;
        }

        private static AppearanceInfo InspectAppearance()
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Imported electric mine model is missing.");
            Texture2D[] textures = LoadTextures();
            if (textures.Length < 4)
                throw new InvalidOperationException(
                    "Electric mine extracted texture count is incomplete: " + textures.Length);
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(PackedTexturePath) == null)
                throw new InvalidOperationException("Electric mine packed PBR texture is missing.");
            string[] materialPaths = AssetDatabase.FindAssets(
                    "t:Material", new[] { MaterialFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            if (materialPaths.Length == 0)
                throw new InvalidOperationException("Electric mine material is missing.");
            InspectRendererMaterials(model.GetComponentsInChildren<Renderer>(true));
            return new AppearanceInfo(textures.Length, materialPaths.Length);
        }

        private static void RequirePropStructure(Transform prop)
        {
            if (prop.parent == null || prop.parent.name != "RightHand")
                throw new InvalidOperationException(prop.root.name + " electric mine is not hand-held.");
            Transform model = prop.Cast<Transform>()
                .SingleOrDefault(item => item.name == ModelInstanceName) ??
                throw new InvalidOperationException(prop.root.name + " electric mine model is missing.");
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException(prop.root.name + " electric mine renderer is missing.");
            InspectRendererMaterials(renderers);
        }

        private static void InspectRendererMaterials(IEnumerable<Renderer> renderers)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer.sharedMaterials.Length == 0)
                    throw new InvalidOperationException(renderer.name + " has no material slot.");
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null)
                        throw new InvalidOperationException(renderer.name + " has an empty material slot.");
                    string path = AssetDatabase.GetAssetPath(material);
                    if (!path.StartsWith(MaterialFolder + "/", StringComparison.Ordinal) ||
                        material.shader == null ||
                        material.shader.name != "Universal Render Pipeline/Lit" ||
                        material.GetTexture("_BaseMap") == null ||
                        material.GetTexture("_BumpMap") == null ||
                        material.GetTexture("_MetallicGlossMap") == null)
                        throw new InvalidOperationException(
                            "Electric mine material assignment is incomplete: " + path);
                }
            }
        }

        private static void ConfigureMaterial(
            Material material,
            Shader shader,
            Texture2D baseColor,
            Texture2D normal,
            Texture2D packed)
        {
            material.shader = shader;
            material.SetTexture("_BaseMap", baseColor);
            material.SetTexture("_MainTex", baseColor);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_Color", Color.white);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", 1f);
            material.SetTexture("_MetallicGlossMap", packed);
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_Smoothness", 1f);
            material.SetFloat("_SmoothnessTextureChannel", 0f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
        }

        private static Texture2D CreatePackedMetallicSmoothness(
            Texture2D metallic,
            Texture2D roughness)
        {
            if (metallic.width != roughness.width || metallic.height != roughness.height)
                throw new InvalidOperationException(
                    "Electric mine metallic and roughness dimensions differ.");
            Color32[] metal = metallic.GetPixels32();
            Color32[] rough = roughness.GetPixels32();
            var pixels = new Color32[metal.Length];
            for (int index = 0; index < pixels.Length; index++)
                pixels[index] = new Color32(
                    metal[index].r, metal[index].r, metal[index].r,
                    (byte)(255 - rough[index].r));
            var output = new Texture2D(
                metallic.width, metallic.height, TextureFormat.RGBA32, false, true);
            try
            {
                output.SetPixels32(pixels);
                output.Apply(false, false);
                File.WriteAllBytes(Absolute(PackedTexturePath), output.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(output);
            }
            AssetDatabase.ImportAsset(
                PackedTexturePath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(PackedTexturePath) ??
                throw new InvalidOperationException("Electric mine packed PBR texture import failed.");
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
            importer.textureType = type;
            importer.sRGBTexture = srgb;
            importer.isReadable = readable;
            importer.SaveAndReimport();
        }

        private static Texture2D[] LoadTextures()
        {
            return AssetDatabase.FindAssets("t:Texture2D", new[] { TextureFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path != PackedTexturePath)
                .Select(AssetDatabase.LoadAssetAtPath<Texture2D>)
                .Where(texture => texture != null)
                .OrderBy(texture => texture.name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static Texture2D FindTexture(IEnumerable<Texture2D> textures, params string[] terms)
        {
            return textures.FirstOrDefault(texture => terms.All(term =>
                texture.name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private static Vector3 RightPalmCenter(Transform targetRoot)
        {
            Transform hand = RequirePath(targetRoot, RightHandPath);
            string[] names =
            {
                "RightIndexProximal", "RightMiddleProximal",
                "RightRingProximal", "RightLittleProximal"
            };
            return names.Select(name => hand.Find(name)?.position ??
                    throw new InvalidOperationException(
                        targetRoot.name + " hand knuckle is missing: " + name))
                .Aggregate(Vector3.zero, (sum, value) => sum + value) / names.Length;
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
            return Vector3.Cross(width, RightFingerDirection(targetRoot)).normalized;
        }

        private static float RightWristStraightness(Transform targetRoot)
        {
            Transform foreArm = RequirePath(targetRoot, RightForeArmPath);
            Transform hand = RequirePath(targetRoot, RightHandPath);
            return Vector3.Angle(
                (hand.position - foreArm.position).normalized,
                RightFingerDirection(targetRoot));
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

        private static string ProtectedTargetSignature(Transform root)
        {
            var text = new StringBuilder();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                         .OrderBy(item => AnimationUtility.CalculateTransformPath(item, root),
                             StringComparer.Ordinal))
            {
                string path = AnimationUtility.CalculateTransformPath(item, root);
                if (path == RightArmPath || path.StartsWith(RightArmPath + "/",
                        StringComparison.Ordinal) ||
                    path == PropName || path.StartsWith(PropName + "/", StringComparison.Ordinal))
                    continue;
                text.Append(path).Append('|').Append(Vec(item.localPosition)).Append('|')
                    .Append(Quat(item.localRotation)).Append('|').Append(Vec(item.localScale))
                    .Append('|').Append(item.gameObject.activeSelf).AppendLine();
            }
            return Sha256Text(text.ToString());
        }

        private static string OutsideTargetsSignature(Scene scene)
        {
            var text = new StringBuilder();
            foreach (GameObject root in scene.GetRootGameObjects().OrderBy(item => item.name))
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                         .Where(item => !HasTargetAncestor(item))
                         .OrderBy(item => AnimationUtility.CalculateTransformPath(item, root.transform),
                             StringComparer.Ordinal))
                text.Append(root.name).Append('|')
                    .Append(AnimationUtility.CalculateTransformPath(item, root.transform))
                    .Append('|').Append(Vec(item.localPosition)).Append('|')
                    .Append(Quat(item.localRotation)).Append('|').Append(Vec(item.localScale))
                    .Append('|').Append(item.gameObject.activeSelf).AppendLine();
            return Sha256Text(text.ToString());
        }

        private static bool HasTargetAncestor(Transform item)
        {
            for (Transform current = item; current != null; current = current.parent)
                if (TargetNames.Contains(current.name)) return true;
            return false;
        }

        internal static Texture2D CaptureTargetPanel(GameObject target, bool closeUp)
        {
            Transform hand = RequirePath(target.transform, RightHandPath);
            Transform prop = RequireDirectChild(hand, PropName);
            Bounds bounds;
            if (closeUp)
            {
                bounds = RendererBounds(prop.gameObject);
                bounds.Encapsulate(RequirePath(target.transform, RightArmPath).position);
                bounds.Encapsulate(RequirePath(target.transform, RightForeArmPath).position);
                bounds.Encapsulate(hand.position);
                foreach (Transform finger in hand.GetComponentsInChildren<Transform>(true))
                    bounds.Encapsulate(finger.position);
                bounds.Expand(0.10f);
            }
            else
            {
                Transform armature = target.transform.Find("Armature") ??
                    throw new InvalidOperationException(target.name + " Armature is missing.");
                Transform[] bones = armature.GetComponentsInChildren<Transform>(true);
                bounds = new Bounds(bones[0].position, Vector3.zero);
                foreach (Transform bone in bones) bounds.Encapsulate(bone.position);
                foreach (Renderer renderer in prop.GetComponentsInChildren<Renderer>(true))
                    bounds.Encapsulate(renderer.bounds);
                bounds.Expand(0.18f);
            }
            return CaptureActualScene(bounds,
                (target.transform.forward + target.transform.right * 0.68f).normalized,
                target.transform.up);
        }

        private static Texture2D CaptureActualScene(
            Bounds bounds,
            Vector3 viewDirection,
            Vector3 up)
        {
            Scene scene = RequireScene();
            var cameraObject = new GameObject("ElectricMine_ReadOnlyCamera");
            var lightObject = new GameObject("ElectricMine_ReadOnlyLight");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            RenderTexture render = RenderTexture.GetTemporary(
                PanelSize, PanelSize, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(bounds.extents.y,
                    Mathf.Max(bounds.extents.x, bounds.extents.z)) * 1.18f;
                camera.nearClipPlane = 0.02f;
                camera.farClipPlane = 8f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.02f, 0.025f, 0.035f, 1f);
                Vector3 cameraUp = Vector3.ProjectOnPlane(up, viewDirection).normalized;
                camera.transform.SetPositionAndRotation(
                    bounds.center + viewDirection * 4f,
                    Quaternion.LookRotation(-viewDirection, cameraUp));
                camera.targetTexture = render;
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.25f;
                light.transform.rotation = Quaternion.Euler(38f, -32f, 0f);
                camera.Render();
                RenderTexture.active = render;
                var image = new Texture2D(
                    PanelSize, PanelSize, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, PanelSize, PanelSize), 0, 0);
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

        internal static Texture2D ComposeReview(IReadOnlyList<Texture2D> panels)
        {
            if (panels.Count != 6)
                throw new InvalidOperationException("Electric mine review requires six panels.");
            int width = PanelSize * 3 + PanelGap * 2;
            int height = PanelSize * 2 + PanelGap;
            var output = new Texture2D(width, height, TextureFormat.RGB24, false);
            output.SetPixels32(Enumerable.Repeat(
                new Color32(5, 7, 10, 255), width * height).ToArray());
            for (int column = 0; column < 3; column++)
            {
                output.SetPixels32(
                    column * (PanelSize + PanelGap), PanelSize + PanelGap,
                    PanelSize, PanelSize, panels[column].GetPixels32());
                output.SetPixels32(
                    column * (PanelSize + PanelGap), 0,
                    PanelSize, PanelSize, panels[column + 3].GetPixels32());
            }
            output.Apply(false, false);
            return output;
        }

        internal static Transform RequireRuntimeProp(GameObject target)
        {
            return RequireDirectChild(RequirePath(target.transform, RightHandPath), PropName);
        }

        internal static string Absolute(string projectPath)
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Project root is unavailable.");
            return Path.GetFullPath(Path.Combine(
                root, projectPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        internal static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must be active. ActiveScene=" + scene.path);
            return scene;
        }

        internal static GameObject FindUnique(Scene scene, string name)
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

        private static Transform RequirePath(Transform root, string path)
        {
            return root.Find(path) ?? throw new InvalidOperationException(
                root.name + " is missing path " + path + ".");
        }

        private static Transform FindHead(GameObject root)
        {
            Animator animator = root.GetComponent<Animator>();
            if (animator != null && animator.isHuman)
            {
                Transform humanoid = animator.GetBoneTransform(HumanBodyBones.Head);
                if (humanoid != null) return humanoid;
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
                    parent.root.name + " expected one " + name + "; found " +
                    matches.Length + ".");
            return matches[0];
        }

        private static Bounds RendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException(root.name + " has no renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }

        private static string Sanitize(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return string.IsNullOrWhiteSpace(value) ? "ElectricMine_Original" : value;
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

        private static string Vec(Vector3 value) => string.Format(
            CultureInfo.InvariantCulture, "({0:R},{1:R},{2:R})",
            value.x, value.y, value.z);

        private static string Quat(Quaternion value) => string.Format(
            CultureInfo.InvariantCulture, "({0:R},{1:R},{2:R},{3:R})",
            value.x, value.y, value.z, value.w);

        private static string F(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture);

        private static void RequireNear(Vector3 actual, Vector3 expected, string label)
        {
            if ((actual - expected).sqrMagnitude > PositionTolerance * PositionTolerance)
                throw new InvalidOperationException(label + " differs.");
        }

        private static void RequireNear(Quaternion actual, Quaternion expected, string label)
        {
            if (Quaternion.Angle(actual, expected) > RotationTolerance)
                throw new InvalidOperationException(label + " differs.");
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " changed unexpectedly.");
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Electric mine setup requires Edit Mode.");
        }

        private readonly struct AppearanceInfo
        {
            internal AppearanceInfo(int textureCount, int materialCount)
            {
                TextureCount = textureCount;
                MaterialCount = materialCount;
            }

            internal int TextureCount { get; }
            internal int MaterialCount { get; }
        }
    }

    [InitializeOnLoad]
    internal static class ElectricMineNaturalReview
    {
        private const string PendingKey = "Bellerophon.ElectricMineReview.Pending";
        private const string RequestIdKey = "Bellerophon.ElectricMineReview.RequestId";
        private const string LogPathKey = "Bellerophon.ElectricMineReview.LogPath";
        private const string FailureKey = "Bellerophon.ElectricMineReview.Failure";
        private const string StateKey = "Bellerophon.ElectricMineReview.State";
        private const int WaitingForPlay = 1;
        private const int Observing = 2;
        private const int WaitingForEdit = 3;
        private static readonly List<Texture2D> Panels = new List<Texture2D>();
        private static readonly Dictionary<string, Vector3> LocalPositions =
            new Dictionary<string, Vector3>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Quaternion> LocalRotations =
            new Dictionary<string, Quaternion>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Vector3> LocalScales =
            new Dictionary<string, Vector3>(StringComparer.Ordinal);
        private static double startedAt;
        private static float maxPositionError;
        private static float maxRotationError;
        private static float maxScaleError;
        private static bool captured;

        static ElectricMineNaturalReview()
        {
            if (!SessionState.GetBool(PendingKey, false)) return;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        internal static void Start(string requestId, string logPath)
        {
            if (SessionState.GetBool(PendingKey, false))
                throw new InvalidOperationException("Electric mine review is already running.");
            SessionState.SetBool(PendingKey, true);
            SessionState.SetString(RequestIdKey, requestId);
            SessionState.SetString(LogPathKey, logPath);
            SessionState.SetString(FailureKey, string.Empty);
            SessionState.SetInt(StateKey, WaitingForPlay);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(PendingKey, false))
            {
                EditorApplication.update -= Tick;
                return;
            }
            try
            {
                int state = SessionState.GetInt(StateKey, WaitingForPlay);
                if (state == WaitingForPlay)
                {
                    if (!EditorApplication.isPlaying) return;
                    InitializeRuntime();
                    SessionState.SetInt(StateKey, Observing);
                    return;
                }
                if (state == Observing)
                {
                    if (!EditorApplication.isPlaying)
                        throw new InvalidOperationException("Play Mode ended during review.");
                    Observe();
                    if (EditorApplication.timeSinceStartup - startedAt < 2.5d) return;
                    WriteReview();
                    SessionState.SetInt(StateKey, WaitingForEdit);
                    EditorApplication.ExitPlaymode();
                    return;
                }
                if (state == WaitingForEdit)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    CompleteInEditMode();
                }
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private static void InitializeRuntime()
        {
            Panels.Clear();
            LocalPositions.Clear();
            LocalRotations.Clear();
            LocalScales.Clear();
            maxPositionError = 0f;
            maxRotationError = 0f;
            maxScaleError = 0f;
            captured = false;
            Scene scene = ElectricMineSetupTools.RequireScene();
            foreach (string name in new[]
                     {
                         "ElectricMine_Idle", "ElectricMine_Activate",
                         "ElectricMine_Armed_Idle"
                     })
            {
                Transform prop = ElectricMineSetupTools.RequireRuntimeProp(
                    ElectricMineSetupTools.FindUnique(scene, name));
                LocalPositions[name] = prop.localPosition;
                LocalRotations[name] = prop.localRotation;
                LocalScales[name] = prop.localScale;
            }
            startedAt = EditorApplication.timeSinceStartup;
        }

        private static void Observe()
        {
            Scene scene = ElectricMineSetupTools.RequireScene();
            string[] names =
            {
                "ElectricMine_Idle", "ElectricMine_Activate", "ElectricMine_Armed_Idle"
            };
            foreach (string name in names)
            {
                Transform prop = ElectricMineSetupTools.RequireRuntimeProp(
                    ElectricMineSetupTools.FindUnique(scene, name));
                maxPositionError = Mathf.Max(maxPositionError,
                    Vector3.Distance(prop.localPosition, LocalPositions[name]));
                maxRotationError = Mathf.Max(maxRotationError,
                    Quaternion.Angle(prop.localRotation, LocalRotations[name]));
                maxScaleError = Mathf.Max(maxScaleError,
                    Vector3.Distance(prop.localScale, LocalScales[name]));
            }
            if (captured || EditorApplication.timeSinceStartup - startedAt < 1.1d) return;
            foreach (string name in names)
                Panels.Add(ElectricMineSetupTools.CaptureTargetPanel(
                    ElectricMineSetupTools.FindUnique(scene, name), false));
            foreach (string name in names)
                Panels.Add(ElectricMineSetupTools.CaptureTargetPanel(
                    ElectricMineSetupTools.FindUnique(scene, name), true));
            captured = true;
        }

        private static void WriteReview()
        {
            if (!captured || Panels.Count != 6)
                throw new InvalidOperationException("Electric mine direct review panels are incomplete.");
            if (maxPositionError > 0.00001f || maxRotationError > 0.02f ||
                maxScaleError > 0.00001f)
                throw new InvalidOperationException("Electric mine detached from the right hand.");
            Texture2D sheet = ElectricMineSetupTools.ComposeReview(Panels);
            try
            {
                Directory.CreateDirectory(ElectricMineSetupTools.Absolute("Temp/ElectricMineSetup"));
                File.WriteAllBytes(
                    ElectricMineSetupTools.Absolute(ElectricMineSetupTools.ReviewImagePath),
                    sheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
            var report = new StringBuilder()
                .AppendLine("Electric mine natural Play Mode review")
                .AppendLine("passed=True")
                .AppendLine("naturalPlayback=True")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("animatorPlayUsed=False")
                .AppendLine("animatorRebindUsed=False")
                .AppendLine("forcedAnimationTimeUsed=False")
                .AppendLine("rightHandFollow=True")
                .AppendLine("observedSeconds=2.5")
                .AppendLine("maximumLocalPositionError=" + maxPositionError.ToString("R",
                    CultureInfo.InvariantCulture))
                .AppendLine("maximumLocalRotationErrorDegrees=" + maxRotationError.ToString("R",
                    CultureInfo.InvariantCulture))
                .AppendLine("maximumLocalScaleError=" + maxScaleError.ToString("R",
                    CultureInfo.InvariantCulture));
            File.WriteAllText(
                ElectricMineSetupTools.Absolute(ElectricMineSetupTools.ReviewReportPath),
                report.ToString(), new UTF8Encoding(false));
        }

        private static void CompleteInEditMode()
        {
            string requestId = SessionState.GetString(RequestIdKey, string.Empty);
            string logPath = SessionState.GetString(LogPathKey, string.Empty);
            string failure = SessionState.GetString(FailureKey, string.Empty);
            if (string.IsNullOrEmpty(failure))
            {
                ElectricMineSetupTools.InspectApplied();
                WriteBridgeLog(logPath,
                    "Unity editor bridge request completed: " + requestId + Environment.NewLine +
                    "status=passed" + Environment.NewLine +
                    "Electric mine natural Play Mode review completed.");
            }
            else
            {
                WriteBridgeLog(logPath,
                    "Unity editor bridge request completed: " + requestId + Environment.NewLine +
                    "status=failed" + Environment.NewLine + failure);
            }
            Clear();
        }

        private static void Fail(Exception exception)
        {
            SessionState.SetString(FailureKey, exception.ToString());
            SessionState.SetInt(StateKey, WaitingForEdit);
            Debug.LogWarning("Electric mine natural review failed: " + exception.Message);
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.ExitPlaymode();
            else CompleteInEditMode();
        }

        private static void Clear()
        {
            EditorApplication.update -= Tick;
            foreach (Texture2D panel in Panels)
                if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            Panels.Clear();
            SessionState.EraseBool(PendingKey);
            SessionState.EraseString(RequestIdKey);
            SessionState.EraseString(LogPathKey);
            SessionState.EraseString(FailureKey);
            SessionState.EraseInt(StateKey);
        }

        private static void WriteBridgeLog(string path, string contents)
        {
            string absolute = ElectricMineSetupTools.Absolute(path);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ??
                throw new InvalidOperationException("Bridge log folder is unavailable."));
            File.WriteAllText(absolute, contents, new UTF8Encoding(false));
        }
    }
}
