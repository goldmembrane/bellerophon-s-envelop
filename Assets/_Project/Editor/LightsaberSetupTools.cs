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
    internal static class LightsaberSetupTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SourceRelativePath = "item model/lightsaber.fbx";
        private const string ItemFolder = "Assets/_Project/Art/Items/Lightsaber";
        private const string ModelPath = ItemFolder + "/Lightsaber.fbx";
        private const string TextureFolder = ItemFolder + "/Textures";
        private const string MaterialFolder = ItemFolder + "/Materials";
        private const string ReviewFolder = ItemFolder + "/Review";
        private const string FinalImagePath = ReviewFolder + "/LightsaberGrip_Final.png";
        private const string GripPoseFinalImagePath =
            ReviewFolder + "/LightsaberGripPose_Final.png";
        private const string LoweredGripPoseFinalImagePath =
            ReviewFolder + "/LightsaberLoweredGripPose_Final.png";
        private const string TransformReplicationFinalImagePath =
            ReviewFolder + "/LightsaberPropTransformReplication_Final.png";
        private const string BladePrefabPath =
            "Assets/_Project/ArtSamples/LightsaberBlade/LightsaberBladeSample.prefab";
        private const string BladeCoreMeshPath =
            "Assets/_Project/ArtSamples/LightsaberBlade/LightsaberBladeCoreMesh.asset";
        private const string BladeGlowMeshPath =
            "Assets/_Project/ArtSamples/LightsaberBlade/LightsaberBladeGlowMesh.asset";
        private const string BladeCoreMaterialPath =
            "Assets/_Project/ArtSamples/LightsaberBlade/LightsaberBladeCore.mat";
        private const string BladeGlowMaterialPath =
            "Assets/_Project/ArtSamples/LightsaberBlade/LightsaberBladeGlow.mat";
        private const string BladeShaderPath =
            "Assets/_Project/ArtSamples/LightsaberBlade/LightsaberBladeAdditive.shader";
        private const string BladeApplicationFinalImagePath =
            ReviewFolder + "/LightsaberBladeApplication_Final.png";
        private const string BladeName = "Lightsaber_Blade";
        private const float LightsaberTotalLengthMeters = 0.9f;
        private const float ApprovedBladeOuterDiameterMeters = 0.07f;
        private const string FailurePath = ReviewFolder + "/last_failure.txt";
        private const string GeometryReportPath = ReviewFolder + "/source_geometry.txt";
        private const string MetallicSmoothnessPath =
            TextureFolder + "/Lightsaber_MetallicSmoothness.png";
        private const string PropName = "Lightsaber_Prop";
        private const string RightHandPath =
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand";
        private const string RightShoulderPath =
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder";
        private const string RightArmPath = RightShoulderPath + "/RightArm";
        private const string RightForeArmPath = RightArmPath + "/RightForeArm";
        private const string LeftShoulderPath =
            "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder";
        private const string LeftArmPath = LeftShoulderPath + "/LeftArm";
        private const string LeftForeArmPath = LeftArmPath + "/LeftForeArm";
        private const string LeftHandPath = LeftForeArmPath + "/LeftHand";
        private const float ForwardTiltDegrees = 30f;
        private const float RightTiltDegrees = 25f;
        private const float PositionToleranceMeters = 0.006f;
        private const float AngleToleranceDegrees = 0.15f;
        private const int PanelWidth = 640;
        private const int PanelHeight = 720;

        private static readonly string[] TargetNames =
        {
            "Lightsaber_Off_Idle",
            "Lightsaber_DiagonalSlash",
            "Lightsaber_Grip_OneHand",
            "Lightsaber_ThrustMode_Enter",
            "Lightsaber_Thrust",
            "Lightsaber_ThrustMode_Exit"
        };

        private const string TransformReplicationSourceName = "Lightsaber_Off_Idle";

        private static readonly string[] TransformReplicationTargetNames =
        {
            "Lightsaber_DiagonalSlash",
            "Lightsaber_Grip_OneHand",
            "Lightsaber_ThrustMode_Enter",
            "Lightsaber_Thrust",
            "Lightsaber_ThrustMode_Exit"
        };

        // Recovery baseline captured from the user's current Lightsaber_Off_Idle edit.
        private static readonly Vector3 AuthoredPropLocalPosition =
            new Vector3(-0.047f, 0.12f, -0.056f);
        private static readonly Quaternion AuthoredPropLocalRotation =
            new Quaternion(-0.229460582f, -0.571224749f, 0.5811238f, -0.5323018f);
        private static readonly Vector3 AuthoredPropLocalScale =
            new Vector3(27.3845215f, 27.3845215f, 27.3845215f);

        [MenuItem("Bellerophon/Player/Inspect Lightsaber Prop Transform Replication Source")]
        internal static void InspectLightsaberPropTransformReplicationSource()
        {
            RequireEditMode();
            int consoleErrorsBefore = ConsoleErrorCount();
            Scene scene = RequireScene();
            Transform sourceHand = RequireRightHand(
                FindUnique(scene, TransformReplicationSourceName));
            Transform sourceProp = RequireLightsaberProp(
                sourceHand, TransformReplicationSourceName);
            if (sourceProp.parent != sourceHand)
                throw new InvalidOperationException(
                    TransformReplicationSourceName +
                    " Lightsaber_Prop is not a direct RightHand child.");

            RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log(
                "[Lightsaber] User-authored transform replication source inspected read-only." +
                " target=" + TransformReplicationSourceName +
                "|localPosition=" + Precise(sourceProp.localPosition) +
                "|localRotation=" + Precise(sourceProp.localRotation) +
                "|localEulerAngles=" + Precise(sourceProp.localEulerAngles) +
                "|localScale=" + Precise(sourceProp.localScale) +
                "|parent=" + sourceProp.parent.name +
                "|verificationTargetTransformManipulated=False");
        }

        [MenuItem("Bellerophon/Player/Apply Lightsaber Prop Transform Replication")]
        internal static void ApplyLightsaberPropTransformReplication()
        {
            RequireEditMode();
            int consoleErrorsBefore = ConsoleErrorCount();
            Scene scene = RequireScene();
            var props = TargetNames.ToDictionary(
                targetName => targetName,
                targetName =>
                {
                    Transform hand = RequireRightHand(FindUnique(scene, targetName));
                    Transform prop = RequireLightsaberProp(hand, targetName);
                    if (prop.parent != hand)
                        throw new InvalidOperationException(
                            targetName + " Lightsaber_Prop is not a direct RightHand child.");
                    return prop;
                },
                StringComparer.Ordinal);
            Transform sourceProp = props[TransformReplicationSourceName];
            RequireAuthoredPropTransform(sourceProp, TransformReplicationSourceName);

            List<TransformSnapshot> protectedSceneBefore = CaptureExistingTransforms(scene);
            var modifiableProps = new HashSet<Transform>(
                TransformReplicationTargetNames.Select(name => props[name]));
            List<TransformSnapshot> protectedPropHierarchyBefore = props.Values
                .SelectMany(prop => prop.GetComponentsInChildren<Transform>(true))
                .Where(item => !modifiableProps.Contains(item))
                .Select(item => new TransformSnapshot(item))
                .ToList();
            var animatorBefore = TargetNames.ToDictionary(
                name => name,
                name => AnimatorSignature(FindUnique(scene, name)),
                StringComparer.Ordinal);
            var rendererBefore = TargetNames.ToDictionary(
                name => name,
                name => RendererSignature(FindUnique(scene, name).transform),
                StringComparer.Ordinal);
            string importedModelHash = Sha256File(Absolute(ModelPath));

            foreach (string targetName in TransformReplicationTargetNames)
            {
                Transform prop = props[targetName];
                Undo.RecordObject(
                    prop,
                    "Copy Lightsaber_Off_Idle prop transform to " + targetName);
                prop.localPosition = AuthoredPropLocalPosition;
                prop.localRotation = AuthoredPropLocalRotation;
                prop.localScale = AuthoredPropLocalScale;
                PrefabUtility.RecordPrefabInstancePropertyModifications(prop);
                EditorUtility.SetDirty(prop);
            }

            RequireExistingTransformsUnchanged(protectedSceneBefore);
            foreach (TransformSnapshot snapshot in protectedPropHierarchyBefore)
                snapshot.RequireUnchanged();
            foreach (string targetName in TargetNames)
            {
                RequireAuthoredPropTransform(props[targetName], targetName);
                GameObject target = FindUnique(scene, targetName);
                RequireEqual(
                    animatorBefore[targetName],
                    AnimatorSignature(target),
                    targetName + " Animator");
                RequireEqual(
                    rendererBefore[targetName],
                    RendererSignature(target.transform),
                    targetName + " renderer/mesh/material");
            }
            RequireEqual(
                importedModelHash,
                Sha256File(Absolute(ModelPath)),
                "imported lightsaber FBX hash");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            AssetDatabase.SaveAssets();
            RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log(
                "[Lightsaber] User-authored Lightsaber_Off_Idle local transform stored " +
                "as the recovery baseline and copied to exactly five prop roots." +
                " localPosition=" + Precise(AuthoredPropLocalPosition) +
                "|localRotation=" + Precise(AuthoredPropLocalRotation) +
                "|localScale=" + Precise(AuthoredPropLocalScale) +
                "|sourceChanged=False|armHandBodyChanged=False" +
                "|animatorChanged=False|rendererMeshMaterialChanged=False");
        }

        [MenuItem("Bellerophon/Player/Inspect Lightsaber Prop Transform Replication")]
        internal static void InspectLightsaberPropTransformReplication()
        {
            RequireEditMode();
            int consoleErrorsBefore = ConsoleErrorCount();
            Scene scene = RequireScene();
            var report = new StringBuilder()
                .AppendLine("Lightsaber prop transform replication read-only inspection")
                .AppendLine("verificationTargetTransformManipulated=False")
                .AppendLine("source=" + TransformReplicationSourceName)
                .AppendLine("scriptedLocalPosition=" + Precise(AuthoredPropLocalPosition))
                .AppendLine("scriptedLocalRotation=" + Precise(AuthoredPropLocalRotation))
                .AppendLine("scriptedLocalScale=" + Precise(AuthoredPropLocalScale));
            foreach (string targetName in TargetNames)
            {
                Transform hand = RequireRightHand(FindUnique(scene, targetName));
                Transform prop = RequireLightsaberProp(hand, targetName);
                if (prop.parent != hand)
                    throw new InvalidOperationException(
                        targetName + " Lightsaber_Prop is not a direct RightHand child.");
                RequireAuthoredPropTransform(prop, targetName);
                report.AppendLine(
                    targetName +
                    "|localPosition=" + Precise(prop.localPosition) +
                    "|localRotation=" + Precise(prop.localRotation) +
                    "|localScale=" + Precise(prop.localScale) +
                    "|parent=" + prop.parent.name);
            }
            RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log(report.ToString());
        }

        [MenuItem("Bellerophon/Player/Capture Lightsaber Prop Transform Replication Final")]
        internal static void CaptureLightsaberPropTransformReplicationFinal()
        {
            RequireEditMode();
            int consoleErrorsBefore = ConsoleErrorCount();
            Scene scene = RequireScene();
            InspectLightsaberPropTransformReplication();
            if (File.Exists(Absolute(TransformReplicationFinalImagePath)))
                throw new InvalidOperationException(
                    "The one final transform-replication capture already exists: " +
                    TransformReplicationFinalImagePath);

            EnsureFolder(ReviewFolder);
            List<Color[]> panels = TargetNames
                .Select(name => CaptureLoweredComparisonPanel(
                    FindUnique(scene, name).transform))
                .ToList();
            var composite = new Texture2D(
                PanelWidth * 3,
                PanelHeight * 2,
                TextureFormat.RGB24,
                false);
            try
            {
                Color background = new Color(0.015f, 0.02f, 0.03f, 1f);
                composite.SetPixels(Enumerable.Repeat(
                    background, composite.width * composite.height).ToArray());
                for (int index = 0; index < panels.Count; index++)
                {
                    int x = (index % 3) * PanelWidth;
                    int y = (1 - index / 3) * PanelHeight;
                    composite.SetPixels(x, y, PanelWidth, PanelHeight, panels[index]);
                }
                composite.Apply(false, false);
                File.WriteAllBytes(
                    Absolute(TransformReplicationFinalImagePath), composite.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(composite);
            }
            AssetDatabase.ImportAsset(
                TransformReplicationFinalImagePath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log(
                "[Lightsaber] One final 3x2 front/oblique user-transform " +
                "replication comparison captured.");
        }

        [MenuItem("Bellerophon/Player/Inspect Lightsaber Blade Application Sources")]
        internal static void InspectLightsaberBladeApplicationSources()
        {
            RequireEditMode();
            int consoleErrorsBefore = ConsoleErrorCount();
            Scene scene = RequireScene();
            RequireLightsaberDesignSource();
            GameObject bladePrefab = RequireBladePrefab();
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Imported lightsaber model is missing.");
            ModelGeometry geometry = AnalyzeModelGeometry(model);
            Vector3 emitterLocal = DetermineEmitterLocal(model.transform, geometry);
            Mesh coreMesh = AssetDatabase.LoadAssetAtPath<Mesh>(BladeCoreMeshPath) ??
                throw new InvalidOperationException("Approved blade core mesh is missing.");
            Mesh glowMesh = AssetDatabase.LoadAssetAtPath<Mesh>(BladeGlowMeshPath) ??
                throw new InvalidOperationException("Approved blade glow mesh is missing.");
            RequireBladePrefabStructure(bladePrefab);

            var report = new StringBuilder()
                .AppendLine("Lightsaber blade application source inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("userOverride=All six targets remain continuously lit")
                .AppendLine("designTotalLengthMeters=" + Num(LightsaberTotalLengthMeters))
                .AppendLine("approvedBladeLengthMeters=" + Num(coreMesh.bounds.size.x))
                .AppendLine("approvedOuterDiameterMeters=" + Num(glowMesh.bounds.size.y))
                .AppendLine("handleEmitterLocal=" + Precise(emitterLocal));
            foreach (string targetName in TargetNames)
            {
                Transform hand = RequireRightHand(FindUnique(scene, targetName));
                Transform prop = RequireLightsaberProp(hand, targetName);
                report.AppendLine(
                    targetName +
                    "|propLocalPosition=" + Precise(prop.localPosition) +
                    "|propLocalRotation=" + Precise(prop.localRotation) +
                    "|propLocalScale=" + Precise(prop.localScale) +
                    "|parent=" + prop.parent.name);
            }
            RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log(report.ToString());
        }

        [MenuItem("Bellerophon/Player/Apply Lightsaber Blade To Six Objects")]
        internal static void ApplyLightsaberBladeToSixObjects()
        {
            RequireEditMode();
            int consoleErrorsBefore = ConsoleErrorCount();
            Scene scene = RequireScene();
            RequireLightsaberDesignSource();
            GameObject bladePrefab = RequireBladePrefab();
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Imported lightsaber model is missing.");
            ModelGeometry geometry = AnalyzeModelGeometry(model);
            Vector3 emitterLocal = DetermineEmitterLocal(model.transform, geometry);

            List<TransformSnapshot> protectedTransforms = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .ToList()
                .Select(item => new TransformSnapshot(item))
                .ToList();
            var animatorBefore = TargetNames.ToDictionary(
                name => name,
                name => AnimatorSignature(FindUnique(scene, name)),
                StringComparer.Ordinal);
            var propTransformBefore = TargetNames.ToDictionary(
                name => name,
                name => TransformSignature(RequireLightsaberProp(
                    RequireRightHand(FindUnique(scene, name)), name)),
                StringComparer.Ordinal);
            var handleRendererBefore = TargetNames.ToDictionary(
                name => name,
                name => HandleRendererSignature(RequireLightsaberProp(
                    RequireRightHand(FindUnique(scene, name)), name)),
                StringComparer.Ordinal);
            Dictionary<string, string> approvedAssetHashes = CaptureApprovedBladeAssetHashes();
            string handleModelHash = Sha256File(Absolute(ModelPath));

            foreach (string targetName in TargetNames)
            {
                Transform hand = RequireRightHand(FindUnique(scene, targetName));
                Transform prop = RequireLightsaberProp(hand, targetName);
                Transform[] existingBlades = prop.Cast<Transform>()
                    .Where(item => item.name == BladeName)
                    .ToArray();
                foreach (Transform existingBlade in existingBlades)
                    Undo.DestroyObjectImmediate(existingBlade.gameObject);

                GameObject bladeObject = PrefabUtility.InstantiatePrefab(
                    bladePrefab,
                    scene) as GameObject ?? throw new InvalidOperationException(
                    targetName + " approved blade prefab instantiation failed.");
                Undo.RegisterCreatedObjectUndo(
                    bladeObject,
                    "Attach approved lightsaber blade to " + targetName);
                bladeObject.name = BladeName;
                bladeObject.transform.SetParent(prop, false);
                Transform blade = bladeObject.transform;
                blade.localPosition = emitterLocal;
                blade.localRotation = Quaternion.identity;
                blade.localScale = Vector3.one;

                float handleLength = prop.TransformVector(
                    geometry.BladeAxisLocal * geometry.HandleLengthLocal).magnitude;
                float desiredBladeLength = LightsaberTotalLengthMeters - handleLength;
                if (desiredBladeLength <= 0f)
                    throw new InvalidOperationException(
                        targetName + " handle length exceeds the 0.9m design total.");
                float currentBladeLength = blade.TransformVector(
                    Vector3.right * ApprovedBladeLengthMeters()).magnitude;
                if (currentBladeLength <= 0.000001f)
                    throw new InvalidOperationException(
                        targetName + " inherited blade scale is zero.");
                blade.localScale = Vector3.one *
                    (desiredBladeLength / currentBladeLength);
                bladeObject.SetActive(true);
                PrefabUtility.RecordPrefabInstancePropertyModifications(blade);
                EditorUtility.SetDirty(bladeObject);
            }

            foreach (TransformSnapshot snapshot in protectedTransforms)
                snapshot.RequireUnchanged();
            foreach (string targetName in TargetNames)
            {
                RequireEqual(
                    animatorBefore[targetName],
                    AnimatorSignature(FindUnique(scene, targetName)),
                    targetName + " Animator");
                Transform prop = RequireLightsaberProp(
                    RequireRightHand(FindUnique(scene, targetName)), targetName);
                RequireEqual(
                    propTransformBefore[targetName],
                    TransformSignature(prop),
                    targetName + " handle transform");
                RequireEqual(
                    handleRendererBefore[targetName],
                    HandleRendererSignature(prop),
                    targetName + " handle renderer/mesh/material");
            }
            RequireEqual(
                handleModelHash,
                Sha256File(Absolute(ModelPath)),
                "imported lightsaber handle hash");
            RequireApprovedBladeAssetHashesUnchanged(approvedAssetHashes);
            InspectLightsaberBladeApplicationInternal(scene, geometry, emitterLocal);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            AssetDatabase.SaveAssets();
            RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log(
                "[LightsaberBladeApplication] Approved static-on blade attached to all six " +
                "existing handle emitters. totalLengthMeters=0.900000" +
                "|continuousEmission=True|rightHandFollow=True" +
                "|handleTransformsChanged=False|posesChanged=False" +
                "|animatorsChanged=False|approvedSampleChanged=False");
        }

        [MenuItem("Bellerophon/Player/Inspect Lightsaber Blade Application")]
        internal static void InspectLightsaberBladeApplication()
        {
            RequireEditMode();
            int consoleErrorsBefore = ConsoleErrorCount();
            Scene scene = RequireScene();
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Imported lightsaber model is missing.");
            ModelGeometry geometry = AnalyzeModelGeometry(model);
            Vector3 emitterLocal = DetermineEmitterLocal(model.transform, geometry);
            InspectLightsaberBladeApplicationInternal(scene, geometry, emitterLocal);
            RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log(
                "[LightsaberBladeApplication] Six targets inspected read-only." +
                " verificationTargetManipulated=False|continuousEmission=True" +
                "|rightHandFollow=True|newConsoleErrors=0");
        }

        [MenuItem("Bellerophon/Player/Capture Lightsaber Blade Application Final")]
        internal static void CaptureLightsaberBladeApplicationFinal()
        {
            RequireEditMode();
            int consoleErrorsBefore = ConsoleErrorCount();
            Scene scene = RequireScene();
            InspectLightsaberBladeApplication();
            if (File.Exists(Absolute(BladeApplicationFinalImagePath)))
                throw new InvalidOperationException(
                    "The one final blade-application capture already exists: " +
                    BladeApplicationFinalImagePath);

            EnsureFolder(ReviewFolder);
            List<Color[]> panels = TargetNames
                .Select(name => CaptureTargetPanel(FindUnique(scene, name).transform))
                .ToList();
            var composite = new Texture2D(
                PanelWidth * 3,
                PanelHeight * 2,
                TextureFormat.RGB24,
                false);
            try
            {
                Color background = new Color(0.015f, 0.02f, 0.03f, 1f);
                composite.SetPixels(Enumerable.Repeat(
                    background, composite.width * composite.height).ToArray());
                for (int index = 0; index < panels.Count; index++)
                {
                    int x = index % 3 * PanelWidth;
                    int y = (1 - index / 3) * PanelHeight;
                    composite.SetPixels(x, y, PanelWidth, PanelHeight, panels[index]);
                }
                composite.Apply(false, false);
                File.WriteAllBytes(
                    Absolute(BladeApplicationFinalImagePath),
                    composite.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(composite);
            }
            AssetDatabase.ImportAsset(
                BladeApplicationFinalImagePath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log(
                "[LightsaberBladeApplication] One final 3x2 six-target direct Unity " +
                "render captured in target-list order.");
        }

        [MenuItem("Bellerophon/Player/Inspect Lightsaber Source And Targets")]
        internal static void InspectLightsaberSourceAndTargets()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            string source = Absolute(SourceRelativePath);
            if (!File.Exists(source))
                throw new FileNotFoundException("Supplied lightsaber FBX is missing.", source);
            foreach (string targetName in TargetNames)
            {
                GameObject target = FindUnique(scene, targetName);
                RequireRightHand(target);
                Animator animator = target.GetComponent<Animator>() ??
                    throw new InvalidOperationException(targetName + " Animator is missing.");
                if (animator.avatar == null || !animator.avatar.isValid)
                    throw new InvalidOperationException(targetName + " has no valid Avatar.");
            }
            GameObject imported = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (imported != null)
            {
                EnsureFolder(ReviewFolder);
                WriteSourceGeometryReport(imported);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            Debug.Log(
                "[Lightsaber] Supplied source and six exact target right-hand rigs inspected. " +
                "sourceSha256=" + Sha256File(source));
        }

        [MenuItem("Bellerophon/Player/Inspect Lightsaber Grip Pose Source")]
        internal static void InspectLightsaberGripPoseSource()
        {
            RequireEditMode();
            int consoleErrorsBefore = ConsoleErrorCount();
            Scene scene = RequireScene();
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Imported lightsaber model is missing.");
            ModelGeometry geometry = AnalyzeModelGeometry(model);
            GameObject gripReference = FindUnique(scene, "Vacuum_Idle");
            Transform referenceHand = RequireRightHand(gripReference);
            RequireGripFingerHierarchy(referenceHand, "Vacuum_Idle");

            var report = new StringBuilder()
                .AppendLine("Lightsaber static grip-pose source inspection")
                .AppendLine("animationClipOrControllerRequired=False")
                .AppendLine("fingerRotationReference=Vacuum_Idle current static right-hand grip");
            foreach (string targetName in TargetNames)
            {
                GameObject target = FindUnique(scene, targetName);
                Transform hand = RequireRightHand(target);
                RequireGripFingerHierarchy(hand, targetName);
                Transform prop = RequireLightsaberProp(hand, targetName);
                GripMetrics metrics = MeasureGrip(target.transform, geometry, prop);
                report.AppendLine(targetName + "|" + metrics.Describe());
            }
            RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log(report.ToString());
        }

        [MenuItem("Bellerophon/Player/Apply Lightsaber Grip Pose")]
        internal static void ApplyLightsaberGripPose()
        {
            RequireEditMode();
            int consoleErrorsBefore = ConsoleErrorCount();
            Scene scene = RequireScene();
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Imported lightsaber model is missing.");
            ModelGeometry geometry = AnalyzeModelGeometry(model);
            GameObject gripReference = FindUnique(scene, "Vacuum_Idle");
            Transform referenceHand = RequireRightHand(gripReference);
            RequireGripFingerHierarchy(referenceHand, "Vacuum_Idle");
            Transform referenceShoulder = FindUnique(scene, "Player_Idle").transform.Find(
                RightShoulderPath) ?? throw new InvalidOperationException(
                "Player_Idle RightShoulder is missing.");

            List<TransformSnapshot> protectedBefore = CaptureGripProtectedTransforms(scene);
            List<NonRotationTransformSnapshot> allowedNonRotationBefore =
                CaptureAllowedGripNonRotation(scene);
            List<RotationSnapshot> protectedAllowedRotationsBefore =
                CaptureProtectedAllowedRotations(scene);
            var animatorBefore = TargetNames.ToDictionary(
                name => name,
                name => AnimatorSignature(FindUnique(scene, name)),
                StringComparer.Ordinal);
            var rendererBefore = TargetNames.ToDictionary(
                name => name,
                name => RendererSignature(FindUnique(scene, name).transform),
                StringComparer.Ordinal);
            string importedModelHash = Sha256File(Absolute(ModelPath));
            var report = new StringBuilder()
                .AppendLine("Lightsaber six-target static right-arm grip application")
                .AppendLine("animationClipOrControllerCreatedOrChanged=False")
                .AppendLine("fingerRotationReference=Vacuum_Idle current static right-hand grip")
                .AppendLine("handleWorldRotationPolicy=Preserve full pre-edit quaternion");

            foreach (string targetName in TargetNames)
            {
                GameObject target = FindUnique(scene, targetName);
                Transform shoulder = target.transform.Find(RightShoulderPath) ??
                    throw new InvalidOperationException(targetName + " RightShoulder is missing.");
                Transform upperArm = target.transform.Find(RightArmPath) ??
                    throw new InvalidOperationException(targetName + " RightArm is missing.");
                Transform foreArm = target.transform.Find(RightForeArmPath) ??
                    throw new InvalidOperationException(targetName + " RightForeArm is missing.");
                Transform hand = RequireRightHand(target);
                RequireGripFingerHierarchy(hand, targetName);
                Transform prop = RequireLightsaberProp(hand, targetName);
                Quaternion handleWorldRotationBefore = prop.rotation;

                Undo.RecordObjects(
                    AllowedRotationTransforms(target).Cast<UnityEngine.Object>().ToArray(),
                    "Pose " + targetName + " right arm around lightsaber handle");
                ApplyNaturalOneHandGrip(
                    target.transform,
                    shoulder,
                    upperArm,
                    foreArm,
                    hand,
                    prop,
                    geometry,
                    referenceHand,
                    referenceShoulder);
                prop.rotation = handleWorldRotationBefore;
                prop.position += PalmCenter(target, hand) -
                    prop.TransformPoint(geometry.HandleCenterLocal);
                PrefabUtility.RecordPrefabInstancePropertyModifications(prop);
                foreach (Transform changed in AllowedRotationTransforms(target))
                    EditorUtility.SetDirty(changed);

                float preservedRotationError = Quaternion.Angle(
                    handleWorldRotationBefore, prop.rotation);
                if (preservedRotationError > 0.001f)
                    throw new InvalidOperationException(
                        targetName + " handle world rotation changed by " +
                        Num(preservedRotationError) + " degrees.");
                GripMetrics metrics = RequireNaturalGrip(
                    target.transform, geometry, prop, referenceHand);
                report.AppendLine(targetName + "|" + metrics.Describe() +
                    "|handleWorldRotationPreservationErrorDegrees=" +
                    Num(preservedRotationError));
            }

            RequireExistingTransformsUnchanged(protectedBefore);
            foreach (NonRotationTransformSnapshot snapshot in allowedNonRotationBefore)
                snapshot.RequireUnchanged();
            foreach (RotationSnapshot snapshot in protectedAllowedRotationsBefore)
                snapshot.RequireUnchanged();
            foreach (string targetName in TargetNames)
            {
                GameObject target = FindUnique(scene, targetName);
                RequireEqual(animatorBefore[targetName], AnimatorSignature(target),
                    targetName + " Animator");
                RequireEqual(rendererBefore[targetName], RendererSignature(target.transform),
                    targetName + " renderer/mesh/material");
            }
            RequireEqual(importedModelHash, Sha256File(Absolute(ModelPath)),
                "imported lightsaber FBX hash");
            InspectAppliedInternal(scene, geometry);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            AssetDatabase.SaveAssets();
            RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log(report.ToString());
        }

        [MenuItem("Bellerophon/Player/Inspect Lightsaber Grip Pose")]
        internal static void InspectLightsaberGripPose()
        {
            RequireEditMode();
            int consoleErrorsBefore = ConsoleErrorCount();
            Scene scene = RequireScene();
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Imported lightsaber model is missing.");
            ModelGeometry geometry = AnalyzeModelGeometry(model);
            Transform referenceHand = RequireRightHand(FindUnique(scene, "Vacuum_Idle"));
            var report = new StringBuilder()
                .AppendLine("Lightsaber static grip-pose read-only inspection")
                .AppendLine("verificationTargetTransformManipulated=False")
                .AppendLine("animationClipOrControllerRequired=False");
            InspectAppliedInternal(scene, geometry);
            foreach (string targetName in TargetNames)
            {
                GameObject target = FindUnique(scene, targetName);
                Transform hand = RequireRightHand(target);
                Transform prop = RequireLightsaberProp(hand, targetName);
                GripMetrics metrics = RequireNaturalGrip(
                    target.transform, geometry, prop, referenceHand);
                report.AppendLine(targetName + "|" + metrics.Describe());
            }
            RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log(report.ToString());
        }

        [MenuItem("Bellerophon/Player/Capture Lightsaber Grip Pose Final")]
        internal static void CaptureLightsaberGripPoseFinal()
        {
            RequireEditMode();
            int consoleErrorsBefore = ConsoleErrorCount();
            Scene scene = RequireScene();
            InspectLightsaberGripPose();
            if (File.Exists(Absolute(GripPoseFinalImagePath)))
                throw new InvalidOperationException(
                    "The one final six-target grip-pose capture already exists: " +
                    GripPoseFinalImagePath);

            EnsureFolder(ReviewFolder);
            var panels = TargetNames
                .Select(name => CaptureGripTargetPanel(FindUnique(scene, name).transform))
                .ToList();
            var composite = new Texture2D(
                PanelWidth * 3,
                PanelHeight * 2,
                TextureFormat.RGB24,
                false);
            try
            {
                Color background = new Color(0.015f, 0.02f, 0.03f, 1f);
                composite.SetPixels(Enumerable.Repeat(
                    background, composite.width * composite.height).ToArray());
                for (int index = 0; index < panels.Count; index++)
                {
                    int x = (index % 3) * PanelWidth;
                    int y = (1 - index / 3) * PanelHeight;
                    composite.SetPixels(x, y, PanelWidth, PanelHeight, panels[index]);
                }
                composite.Apply(false, false);
                File.WriteAllBytes(
                    Absolute(GripPoseFinalImagePath), composite.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(composite);
            }
            AssetDatabase.ImportAsset(
                GripPoseFinalImagePath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log(
                "[Lightsaber] One final 3x2 right-arm and hand grip composite captured.");
        }

        [MenuItem("Bellerophon/Player/Inspect Lightsaber Lowered Grip Pose Source")]
        internal static void InspectLightsaberLoweredGripPoseSource()
        {
            RequireEditMode();
            int consoleErrorsBefore = ConsoleErrorCount();
            Scene scene = RequireScene();
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Imported lightsaber model is missing.");
            ModelGeometry geometry = AnalyzeModelGeometry(model);
            var report = new StringBuilder()
                .AppendLine("Lightsaber lowered-right-arm source inspection")
                .AppendLine("poseSource=Each target current LeftShoulder/LeftArm/LeftForeArm/LeftHand")
                .AppendLine("fingerPoseSource=Each target current right-hand fingers")
                .AppendLine("animationClipOrControllerRequired=False");
            foreach (string targetName in TargetNames)
            {
                GameObject target = FindUnique(scene, targetName);
                Transform hand = RequireRightHand(target);
                Transform prop = RequireLightsaberProp(hand, targetName);
                report.AppendLine(targetName + "|" +
                    MeasureLoweredArm(target.transform, geometry, prop).Describe());
            }
            RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log(report.ToString());
        }

        [MenuItem("Bellerophon/Player/Apply Lightsaber Lowered Grip Pose")]
        internal static void ApplyLightsaberLoweredGripPose()
        {
            RequireEditMode();
            int consoleErrorsBefore = ConsoleErrorCount();
            Scene scene = RequireScene();
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Imported lightsaber model is missing.");
            ModelGeometry geometry = AnalyzeModelGeometry(model);
            GameObject idleReference = FindUnique(scene, "Player_Idle");
            Transform referenceRightShoulder = idleReference.transform.Find(
                RightShoulderPath) ?? throw new InvalidOperationException(
                "Player_Idle RightShoulder is missing.");
            Transform referenceRightHand = RequireRightHand(idleReference);
            Transform vacuumHand = RequireRightHand(FindUnique(scene, "Vacuum_Idle"));

            List<TransformSnapshot> protectedBefore = CaptureGripProtectedTransforms(scene);
            List<NonRotationTransformSnapshot> allowedNonRotationBefore =
                CaptureAllowedGripNonRotation(scene);
            List<RotationSnapshot> protectedAllowedRotationsBefore =
                CaptureProtectedAllowedRotations(scene);
            List<RotationSnapshot> fingerRotationsBefore =
                CaptureTargetFingerRotations(scene);
            var animatorBefore = TargetNames.ToDictionary(
                name => name,
                name => AnimatorSignature(FindUnique(scene, name)),
                StringComparer.Ordinal);
            var rendererBefore = TargetNames.ToDictionary(
                name => name,
                name => RendererSignature(FindUnique(scene, name).transform),
                StringComparer.Ordinal);
            string importedModelHash = Sha256File(Absolute(ModelPath));
            var report = new StringBuilder()
                .AppendLine("Lightsaber six-target lowered right-arm grip application")
                .AppendLine("poseSource=Each target current left arm mirrored across local sagittal plane")
                .AppendLine("rightFingerLocalRotationsPreserved=True")
                .AppendLine("handleWorldRotationPolicy=Preserve full pre-edit quaternion")
                .AppendLine("animationClipOrControllerCreatedOrChanged=False");

            foreach (string targetName in TargetNames)
            {
                GameObject target = FindUnique(scene, targetName);
                Transform shoulder = target.transform.Find(RightShoulderPath) ??
                    throw new InvalidOperationException(targetName + " RightShoulder is missing.");
                Transform upperArm = target.transform.Find(RightArmPath) ??
                    throw new InvalidOperationException(targetName + " RightArm is missing.");
                Transform foreArm = target.transform.Find(RightForeArmPath) ??
                    throw new InvalidOperationException(targetName + " RightForeArm is missing.");
                Transform hand = RequireRightHand(target);
                Transform prop = RequireLightsaberProp(hand, targetName);
                Quaternion handleWorldRotationBefore = prop.rotation;
                Vector3 propLocalScaleBefore = prop.localScale;
                Transform propParentBefore = prop.parent;

                Undo.RecordObjects(
                    new UnityEngine.Object[] { shoulder, upperArm, foreArm, hand, prop },
                    "Lower " + targetName + " right arm like its left arm");
                ApplyLoweredArmFromLeft(
                    target.transform,
                    shoulder,
                    upperArm,
                    foreArm,
                    hand,
                    prop,
                    geometry,
                    referenceRightShoulder,
                    referenceRightHand,
                    handleWorldRotationBefore);
                prop.rotation = handleWorldRotationBefore;
                prop.position += PalmCenter(target, hand) -
                    prop.TransformPoint(geometry.HandleCenterLocal);
                PrefabUtility.RecordPrefabInstancePropertyModifications(prop);
                EditorUtility.SetDirty(shoulder);
                EditorUtility.SetDirty(upperArm);
                EditorUtility.SetDirty(foreArm);
                EditorUtility.SetDirty(hand);
                EditorUtility.SetDirty(prop);

                float handleRotationError = Quaternion.Angle(
                    handleWorldRotationBefore, prop.rotation);
                if (handleRotationError > 0.001f || prop.parent != propParentBefore ||
                    Vector3.Distance(prop.localScale, propLocalScaleBefore) > 0.000001f)
                    throw new InvalidOperationException(
                        targetName + " handle parent, scale, or full world rotation changed.");
                LoweredArmMetrics metrics = RequireLoweredArmGrip(
                    target.transform, geometry, prop, vacuumHand);
                report.AppendLine(targetName + "|" + metrics.Describe() +
                    "|handleWorldRotationPreservationErrorDegrees=" +
                    Num(handleRotationError));
            }

            RequireExistingTransformsUnchanged(protectedBefore);
            foreach (NonRotationTransformSnapshot snapshot in allowedNonRotationBefore)
                snapshot.RequireUnchanged();
            foreach (RotationSnapshot snapshot in protectedAllowedRotationsBefore)
                snapshot.RequireUnchanged();
            foreach (RotationSnapshot snapshot in fingerRotationsBefore)
                snapshot.RequireUnchanged();
            foreach (string targetName in TargetNames)
            {
                GameObject target = FindUnique(scene, targetName);
                RequireEqual(animatorBefore[targetName], AnimatorSignature(target),
                    targetName + " Animator");
                RequireEqual(rendererBefore[targetName], RendererSignature(target.transform),
                    targetName + " renderer/mesh/material");
            }
            RequireEqual(importedModelHash, Sha256File(Absolute(ModelPath)),
                "imported lightsaber FBX hash");
            InspectAppliedInternal(scene, geometry);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            AssetDatabase.SaveAssets();
            RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log(report.ToString());
        }

        [MenuItem("Bellerophon/Player/Inspect Lightsaber Lowered Grip Pose")]
        internal static void InspectLightsaberLoweredGripPose()
        {
            RequireEditMode();
            int consoleErrorsBefore = ConsoleErrorCount();
            Scene scene = RequireScene();
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Imported lightsaber model is missing.");
            ModelGeometry geometry = AnalyzeModelGeometry(model);
            Transform vacuumHand = RequireRightHand(FindUnique(scene, "Vacuum_Idle"));
            var report = new StringBuilder()
                .AppendLine("Lightsaber lowered-right-arm read-only inspection")
                .AppendLine("verificationTargetTransformManipulated=False")
                .AppendLine("animationClipOrControllerRequired=False");
            InspectAppliedInternal(scene, geometry);
            foreach (string targetName in TargetNames)
            {
                GameObject target = FindUnique(scene, targetName);
                Transform hand = RequireRightHand(target);
                Transform prop = RequireLightsaberProp(hand, targetName);
                report.AppendLine(targetName + "|" +
                    RequireLoweredArmGrip(
                        target.transform, geometry, prop, vacuumHand).Describe());
            }
            RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log(report.ToString());
        }

        [MenuItem("Bellerophon/Player/Capture Lightsaber Lowered Grip Pose Final")]
        internal static void CaptureLightsaberLoweredGripPoseFinal()
        {
            RequireEditMode();
            int consoleErrorsBefore = ConsoleErrorCount();
            Scene scene = RequireScene();
            InspectLightsaberLoweredGripPose();
            if (File.Exists(Absolute(LoweredGripPoseFinalImagePath)))
                throw new InvalidOperationException(
                    "The one final lowered-grip capture already exists: " +
                    LoweredGripPoseFinalImagePath);
            EnsureFolder(ReviewFolder);
            List<Color[]> panels = TargetNames
                .Select(name => CaptureLoweredComparisonPanel(
                    FindUnique(scene, name).transform))
                .ToList();
            var composite = new Texture2D(
                PanelWidth * 3,
                PanelHeight * 2,
                TextureFormat.RGB24,
                false);
            try
            {
                Color background = new Color(0.015f, 0.02f, 0.03f, 1f);
                composite.SetPixels(Enumerable.Repeat(
                    background, composite.width * composite.height).ToArray());
                for (int index = 0; index < panels.Count; index++)
                {
                    int x = (index % 3) * PanelWidth;
                    int y = (1 - index / 3) * PanelHeight;
                    composite.SetPixels(x, y, PanelWidth, PanelHeight, panels[index]);
                }
                composite.Apply(false, false);
                File.WriteAllBytes(
                    Absolute(LoweredGripPoseFinalImagePath), composite.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(composite);
            }
            AssetDatabase.ImportAsset(
                LoweredGripPoseFinalImagePath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log(
                "[Lightsaber] One final 3x2 front/oblique lowered-arm comparison captured.");
        }

        [MenuItem("Bellerophon/Player/Apply Lightsaber Carry")]
        internal static void ApplyLightsaberCarry()
        {
            EnsureFolder(ItemFolder);
            EnsureFolder(ReviewFolder);
            try
            {
                ApplyLightsaberCarryInternal();
                string failure = Absolute(FailurePath);
                if (File.Exists(failure)) File.Delete(failure);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            catch (Exception exception)
            {
                File.WriteAllText(Absolute(FailurePath), exception.ToString(), Encoding.UTF8);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                throw;
            }
        }

        private static void ApplyLightsaberCarryInternal()
        {
            RequireEditMode();
            int consoleErrorsBefore = ConsoleErrorCount();
            Scene scene = RequireScene();
            InspectLightsaberSourceAndTargets();
            ImportExactSourceAndEmbeddedAppearance();

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Imported lightsaber model is missing.");
            ModelGeometry geometry = AnalyzeModelGeometry(model);
            List<TransformSnapshot> sceneBefore = CaptureExistingTransforms(scene);
            var animatorBefore = TargetNames.ToDictionary(
                name => name,
                name => AnimatorSignature(FindUnique(scene, name)),
                StringComparer.Ordinal);

            foreach (string targetName in TargetNames)
            {
                GameObject target = FindUnique(scene, targetName);
                Transform hand = RequireRightHand(target);
                Transform existing = hand.Cast<Transform>()
                    .SingleOrDefault(item => item.name == PropName);
                GameObject propObject;
                if (existing == null)
                {
                    propObject = PrefabUtility.InstantiatePrefab(model, scene) as GameObject ??
                        throw new InvalidOperationException(
                            targetName + " lightsaber instantiation failed.");
                    Undo.RegisterCreatedObjectUndo(propObject, "Add exact lightsaber to " + targetName);
                    propObject.name = PropName;
                    propObject.transform.SetParent(hand, false);
                }
                else
                {
                    propObject = existing.gameObject;
                    string sourcePath = AssetDatabase.GetAssetPath(
                        PrefabUtility.GetCorrespondingObjectFromSource(propObject));
                    if (!string.Equals(sourcePath, ModelPath, StringComparison.Ordinal))
                    {
                        Undo.DestroyObjectImmediate(propObject);
                        propObject = PrefabUtility.InstantiatePrefab(model, scene) as GameObject ??
                            throw new InvalidOperationException(
                                targetName + " replacement lightsaber instantiation failed.");
                        Undo.RegisterCreatedObjectUndo(
                            propObject,
                            "Replace lightsaber with supplied exact model on " + targetName);
                        propObject.name = PropName;
                        propObject.transform.SetParent(hand, false);
                    }
                    else Undo.RecordObject(propObject.transform, "Align lightsaber on " + targetName);
                }

                Transform prop = propObject.transform;
                prop.localScale = Vector3.one;
                float desiredHandleLength = PalmSpan(target, hand) * 2f;
                float unitWorldLength = prop.TransformVector(
                    geometry.BladeAxisLocal * geometry.HandleLengthLocal).magnitude;
                if (unitWorldLength <= 0.000001f)
                    throw new InvalidOperationException(
                        targetName + " lightsaber unit-scale length is zero.");
                prop.localScale = Vector3.one * (desiredHandleLength / unitWorldLength);
                Vector3 desiredDirection = DesiredBladeDirection(target.transform);
                prop.rotation = Quaternion.FromToRotation(geometry.BladeAxisLocal, desiredDirection);
                Vector3 palmCenter = PalmCenter(target, hand);
                prop.position += palmCenter - prop.TransformPoint(geometry.HandleCenterLocal);
                PrefabUtility.RecordPrefabInstancePropertyModifications(prop);
                EditorUtility.SetDirty(propObject);
            }

            RequireExistingTransformsUnchanged(sceneBefore);
            foreach (string targetName in TargetNames)
            {
                string actual = AnimatorSignature(FindUnique(scene, targetName));
                RequireEqual(animatorBefore[targetName], actual, targetName + " Animator");
            }
            InspectAppliedInternal(scene, geometry);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            AssetDatabase.SaveAssets();
            RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log(
                "[Lightsaber] Exact source appearance and six right-hand-follow placements saved. " +
                "No target skeleton, pose, animation, or root transform was changed.");
        }

        [MenuItem("Bellerophon/Player/Inspect Lightsaber Carry")]
        internal static void InspectLightsaberCarry()
        {
            RequireEditMode();
            int consoleErrorsBefore = ConsoleErrorCount();
            Scene scene = RequireScene();
            InspectImportedAppearance();
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Imported lightsaber model is missing.");
            InspectAppliedInternal(scene, AnalyzeModelGeometry(model));
            RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log(
                "[Lightsaber] Six current poses, right-hand parents, exact appearance, " +
                "and requested blade angles inspected read-only.");
        }

        [MenuItem("Bellerophon/Player/Capture Lightsaber Carry Final")]
        internal static void CaptureLightsaberCarryFinal()
        {
            RequireEditMode();
            int consoleErrorsBefore = ConsoleErrorCount();
            Scene scene = RequireScene();
            InspectLightsaberCarry();
            if (File.Exists(Absolute(FinalImagePath)))
                throw new InvalidOperationException(
                    "The one final six-target capture already exists: " + FinalImagePath);

            EnsureFolder(ReviewFolder);
            var panels = new List<Color[]>(TargetNames.Length);
            foreach (string targetName in TargetNames)
                panels.Add(CaptureTargetPanel(FindUnique(scene, targetName).transform));

            var composite = new Texture2D(
                PanelWidth * 3,
                PanelHeight * 2,
                TextureFormat.RGB24,
                false);
            try
            {
                Color background = new Color(0.015f, 0.02f, 0.03f, 1f);
                var fill = Enumerable.Repeat(
                    background,
                    composite.width * composite.height).ToArray();
                composite.SetPixels(fill);
                for (int index = 0; index < panels.Count; index++)
                {
                    int x = (index % 3) * PanelWidth;
                    int y = (1 - index / 3) * PanelHeight;
                    composite.SetPixels(x, y, PanelWidth, PanelHeight, panels[index]);
                }
                composite.Apply(false, false);
                File.WriteAllBytes(Absolute(FinalImagePath), composite.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(composite);
            }
            AssetDatabase.ImportAsset(
                FinalImagePath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log(
                "[Lightsaber] One final six-target current-pose composite captured in target-list order.");
        }

        private static void ImportExactSourceAndEmbeddedAppearance()
        {
            EnsureFolder(ItemFolder);
            EnsureFolder(TextureFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(ReviewFolder);
            string source = Absolute(SourceRelativePath);
            string destination = Absolute(ModelPath);
            File.Copy(source, destination, true);
            AssetDatabase.ImportAsset(
                ModelPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter ??
                throw new InvalidOperationException("Lightsaber ModelImporter is unavailable.");
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.materialSearch = ModelImporterMaterialSearch.Local;
            importer.isReadable = true;
            importer.SaveAndReimport();

            Material[] embeddedBefore = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<Material>()
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .ToArray();
            Material[] existingExternalMaterials = AssetDatabase.FindAssets(
                    "t:Material",
                    new[] { MaterialFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Material>)
                .Where(item => item != null)
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .ToArray();
            if (embeddedBefore.Length == 0 && existingExternalMaterials.Length == 0)
                throw new InvalidOperationException(
                    "Supplied lightsaber FBX contains no embedded or already-extracted material.");
            string[] materialNames = (embeddedBefore.Length > 0
                    ? embeddedBefore
                    : existingExternalMaterials)
                .Select(item => item.name)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            bool extracted = importer.ExtractTextures(Absolute(TextureFolder));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Texture2D[] textures = LoadTextures();
            if (!extracted && textures.Length == 0)
                throw new InvalidOperationException(
                    "Supplied lightsaber embedded texture extraction failed.");

            Texture2D baseColor = FindTexture(textures, "base", "color");
            Texture2D normal = FindTexture(textures, "normal");
            Texture2D metallic = FindTexture(textures, "metallic");
            Texture2D roughness = FindTexture(textures, "roughness");
            Texture2D metallicSmoothness = CreateMetallicSmoothness(metallic, roughness);
            ConfigureTextureImporters(
                baseColor,
                normal,
                metallic,
                roughness,
                metallicSmoothness);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                throw new InvalidOperationException("URP Lit shader is unavailable.");

            Material[] embeddedAfter = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<Material>()
                .ToArray();
            foreach (string materialName in materialNames)
            {
                Material embedded = embeddedAfter.SingleOrDefault(
                    item => item.name == materialName);
                string materialPath = MaterialFolder + "/" +
                    SanitizeFileName(materialName) + ".mat";
                Material external = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (external == null)
                {
                    if (embedded == null)
                        throw new InvalidOperationException(
                            "Lightsaber material is unavailable after extraction: " + materialName);
                    external = new Material(embedded) { name = materialName };
                    AssetDatabase.CreateAsset(external, materialPath);
                }
                else if (embedded != null)
                {
                    EditorUtility.CopySerialized(embedded, external);
                    external.name = materialName;
                    EditorUtility.SetDirty(external);
                }
                ConfigureUrpMaterial(
                    external,
                    shader,
                    baseColor,
                    normal,
                    metallicSmoothness);
                importer.AddRemap(
                    new AssetImporter.SourceAssetIdentifier(typeof(Material), materialName),
                    external);
            }
            importer.SaveAndReimport();
            AssetDatabase.SaveAssets();
            RequireEqual(
                Sha256File(source),
                Sha256File(destination),
                "supplied/imported lightsaber FBX hash");
            InspectImportedAppearance();
        }

        private static void InspectImportedAppearance()
        {
            string source = Absolute(SourceRelativePath);
            string destination = Absolute(ModelPath);
            if (!File.Exists(source) || !File.Exists(destination))
                throw new InvalidOperationException("Lightsaber source/imported FBX is missing.");
            RequireEqual(
                Sha256File(source),
                Sha256File(destination),
                "supplied/imported lightsaber FBX hash");

            Texture2D[] textures = LoadTextures();
            Texture2D baseColor = FindTexture(textures, "base", "color");
            Texture2D normal = FindTexture(textures, "normal");
            Texture2D metallic = FindTexture(textures, "metallic");
            Texture2D roughness = FindTexture(textures, "roughness");
            Texture2D metallicSmoothness = AssetDatabase.LoadAssetAtPath<Texture2D>(
                MetallicSmoothnessPath) ?? throw new InvalidOperationException(
                "Lightsaber metallic/smoothness packing is missing.");
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Imported lightsaber model is missing.");
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException("Imported lightsaber has no renderer.");

            foreach (Material material in renderers.SelectMany(item => item.sharedMaterials))
            {
                if (material == null)
                    throw new InvalidOperationException("Lightsaber renderer has an empty material slot.");
                string materialPath = AssetDatabase.GetAssetPath(material);
                if (!materialPath.StartsWith(MaterialFolder + "/", StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Lightsaber material was not externalized exactly: " + materialPath);
                if (material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
                    throw new InvalidOperationException(
                        "Lightsaber material shader is unavailable: " + materialPath);
                if (material.shader.name != "Universal Render Pipeline/Lit" ||
                    material.GetTexture("_BaseMap") != baseColor ||
                    material.GetTexture("_BumpMap") != normal ||
                    material.GetTexture("_MetallicGlossMap") != metallicSmoothness)
                    throw new InvalidOperationException(
                        "Lightsaber exact PBR texture mapping is incomplete: " + materialPath);
            }
            if (metallic == null || roughness == null)
                throw new InvalidOperationException(
                    "Lightsaber metallic or roughness source texture is missing.");
        }

        private static void InspectAppliedInternal(Scene scene, ModelGeometry geometry)
        {
            foreach (string targetName in TargetNames)
            {
                GameObject target = FindUnique(scene, targetName);
                Transform hand = RequireRightHand(target);
                Transform[] props = hand.Cast<Transform>()
                    .Where(item => item.name == PropName)
                    .ToArray();
                if (props.Length != 1)
                    throw new InvalidOperationException(
                        targetName + " Lightsaber_Prop count=" + props.Length + ".");
                Transform prop = props[0];
                string sourcePath = AssetDatabase.GetAssetPath(
                    PrefabUtility.GetCorrespondingObjectFromSource(prop.gameObject));
                if (!string.Equals(sourcePath, ModelPath, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        targetName + " does not use the supplied imported lightsaber model.");
                if (prop.parent != hand)
                    throw new InvalidOperationException(
                        targetName + " lightsaber is not directly parented to RightHand.");

                Vector3 palm = PalmCenter(target, hand);
                Vector3 handleCenter = prop.TransformPoint(geometry.HandleCenterLocal);
                float positionError = Vector3.Distance(palm, handleCenter);
                if (positionError > PositionToleranceMeters)
                    throw new InvalidOperationException(
                        targetName + " handle/palm error=" + Num(positionError) + "m.");

                float expectedLength = PalmSpan(target, hand) * 2f;
                float actualLength = prop.TransformVector(
                    geometry.BladeAxisLocal * geometry.HandleLengthLocal).magnitude;
                if (Mathf.Abs(actualLength - expectedLength) > 0.004f)
                    throw new InvalidOperationException(
                        targetName + " grip-scaled handle length expected=" +
                        Num(expectedLength) + "m, actual=" + Num(actualLength) + "m.");

                Vector3 bladeWorld = prop.TransformDirection(geometry.BladeAxisLocal).normalized;
                Vector3 bladeTargetLocal = target.transform.InverseTransformDirection(bladeWorld).normalized;
                float forwardTilt = Mathf.Atan2(
                    bladeTargetLocal.z,
                    bladeTargetLocal.y) * Mathf.Rad2Deg;
                float rightTilt = Mathf.Atan2(
                    bladeTargetLocal.x,
                    bladeTargetLocal.y) * Mathf.Rad2Deg;
                RequireAngle(ForwardTiltDegrees, forwardTilt, targetName + " forward tilt");
                RequireAngle(RightTiltDegrees, rightTilt, targetName + " right tilt");

                Renderer[] renderers = prop.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0 ||
                    renderers.SelectMany(item => item.sharedMaterials).Any(item => item == null))
                    throw new InvalidOperationException(
                        targetName + " lightsaber renderer/material is incomplete.");
            }
        }

        private static ModelGeometry AnalyzeModelGeometry(GameObject model)
        {
            List<SubmeshGeometry> parts = CollectSubmeshGeometry(model.transform);
            if (parts.Count == 0)
                throw new InvalidOperationException("Lightsaber model has no readable mesh geometry.");
            Bounds overall = parts[0].Bounds;
            foreach (SubmeshGeometry part in parts.Skip(1)) overall.Encapsulate(part.Bounds);
            int longestAxis = LongestAxis(overall.size);
            Vector3 unsignedAxis = AxisVector(longestAxis);

            SubmeshGeometry[] bladeParts = parts.Where(item => item.IsBlade).ToArray();
            SubmeshGeometry[] handleParts = parts.Where(item => !item.IsBlade).ToArray();
            if (bladeParts.Length > 0 && handleParts.Length > 0)
            {
                Bounds bladeBounds = CombinedBounds(bladeParts.Select(item => item.Bounds));
                Bounds handleBounds = CombinedBounds(handleParts.Select(item => item.Bounds));
                float bladeCoordinate = Component(bladeBounds.center, longestAxis);
                float handleCoordinate = Component(handleBounds.center, longestAxis);
                if (Mathf.Abs(bladeCoordinate - handleCoordinate) >= 0.0001f)
                {
                    Vector3 bladeAxis = unsignedAxis *
                        Mathf.Sign(bladeCoordinate - handleCoordinate);
                    return new ModelGeometry(
                        bladeAxis,
                        handleBounds.center,
                        Component(overall.size, longestAxis));
                }
            }

            return AnalyzeModelGeometryByCrossSection(
                model.transform,
                longestAxis,
                unsignedAxis,
                overall);
        }

        private static ModelGeometry AnalyzeModelGeometryByCrossSection(
            Transform root,
            int longestAxis,
            Vector3 unsignedAxis,
            Bounds overall)
        {
            Vector3[] vertices = CollectLocalVertices(root).ToArray();
            if (vertices.Length < 8)
                throw new InvalidOperationException(
                    "Lightsaber geometry has too few vertices for exact axis analysis.");
            float minimum = vertices.Min(item => Component(item, longestAxis));
            float maximum = vertices.Max(item => Component(item, longestAxis));
            float length = maximum - minimum;
            if (length <= 0.0001f)
                throw new InvalidOperationException("Lightsaber longest axis has zero length.");

            Vector3[] minimumEnd = vertices.Where(item =>
                Component(item, longestAxis) <= minimum + length * 0.22f).ToArray();
            Vector3[] maximumEnd = vertices.Where(item =>
                Component(item, longestAxis) >= maximum - length * 0.22f).ToArray();
            float minimumThickness = RadialThickness(minimumEnd, longestAxis);
            float maximumThickness = RadialThickness(maximumEnd, longestAxis);
            float thicker = Mathf.Max(minimumThickness, maximumThickness);
            float thinner = Mathf.Min(minimumThickness, maximumThickness);
            if (thicker <= 0.0001f || thicker < thinner * 1.02f)
                throw new InvalidOperationException(
                    "Supplied lightsaber blade/handle ends are not geometrically distinct; " +
                    "placement stopped instead of choosing an arbitrary direction.");

            bool emitterAtMaximum = maximumThickness > minimumThickness;
            Vector3 emitterAxis = unsignedAxis * (emitterAtMaximum ? 1f : -1f);
            return new ModelGeometry(emitterAxis, overall.center, length);
        }

        private static IEnumerable<Vector3> CollectLocalVertices(Transform root)
        {
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || !filter.sharedMesh.isReadable) continue;
                Matrix4x4 toRoot = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                foreach (Vector3 vertex in filter.sharedMesh.vertices)
                    yield return toRoot.MultiplyPoint3x4(vertex);
            }
            foreach (SkinnedMeshRenderer renderer in
                root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.sharedMesh == null || !renderer.sharedMesh.isReadable) continue;
                Matrix4x4 toRoot = root.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                foreach (Vector3 vertex in renderer.sharedMesh.vertices)
                    yield return toRoot.MultiplyPoint3x4(vertex);
            }
        }

        private static float RadialThickness(IEnumerable<Vector3> source, int axis)
        {
            Vector3[] vertices = source.ToArray();
            if (vertices.Length == 0) return 0f;
            Bounds bounds = new Bounds(vertices[0], Vector3.zero);
            foreach (Vector3 vertex in vertices.Skip(1)) bounds.Encapsulate(vertex);
            if (axis == 0) return Mathf.Max(bounds.size.y, bounds.size.z);
            if (axis == 1) return Mathf.Max(bounds.size.x, bounds.size.z);
            return Mathf.Max(bounds.size.x, bounds.size.y);
        }

        private static void WriteSourceGeometryReport(GameObject model)
        {
            Vector3[] vertices = CollectLocalVertices(model.transform).ToArray();
            var report = new StringBuilder()
                .AppendLine("Lightsaber supplied geometry inspection")
                .AppendLine("modelPath=" + ModelPath)
                .AppendLine("modelRoot=" + model.name)
                .AppendLine("sourceSha256=" + Sha256File(Absolute(SourceRelativePath)));
            foreach (Transform item in model.GetComponentsInChildren<Transform>(true))
            {
                MeshFilter filter = item.GetComponent<MeshFilter>();
                SkinnedMeshRenderer skinned = item.GetComponent<SkinnedMeshRenderer>();
                Mesh mesh = filter != null ? filter.sharedMesh :
                    skinned != null ? skinned.sharedMesh : null;
                if (mesh == null) continue;
                Renderer renderer = item.GetComponent<Renderer>();
                report.AppendLine(
                    "mesh=" + HierarchyPath(model.transform, item) +
                    "|meshName=" + mesh.name +
                    "|vertices=" + mesh.vertexCount +
                    "|localBoundsCenter=" + Vec(mesh.bounds.center) +
                    "|localBoundsSize=" + Vec(mesh.bounds.size) +
                    "|materials=" + string.Join(",", renderer != null
                        ? renderer.sharedMaterials.Select(material =>
                            material != null ? material.name : "<null>")
                        : Array.Empty<string>()));
            }
            if (vertices.Length > 0)
            {
                Bounds bounds = new Bounds(vertices[0], Vector3.zero);
                foreach (Vector3 vertex in vertices.Skip(1)) bounds.Encapsulate(vertex);
                int axis = LongestAxis(bounds.size);
                float minimum = vertices.Min(item => Component(item, axis));
                float maximum = vertices.Max(item => Component(item, axis));
                float length = maximum - minimum;
                report.AppendLine("rootBoundsCenter=" + Vec(bounds.center));
                report.AppendLine("rootBoundsSize=" + Vec(bounds.size));
                report.AppendLine("longestAxis=" + axis);
                report.AppendLine("axisMinimum=" + Num(minimum));
                report.AppendLine("axisMaximum=" + Num(maximum));
                report.AppendLine("originDistanceFromMinimum=" + Num(Mathf.Abs(minimum)));
                report.AppendLine("originDistanceFromMaximum=" + Num(Mathf.Abs(maximum)));
                foreach (float fraction in new[] { 0.1f, 0.22f, 0.35f })
                {
                    Vector3[] minimumEnd = vertices.Where(item =>
                        Component(item, axis) <= minimum + length * fraction).ToArray();
                    Vector3[] maximumEnd = vertices.Where(item =>
                        Component(item, axis) >= maximum - length * fraction).ToArray();
                    report.AppendLine(
                        "endFraction=" + Num(fraction) +
                        "|minimumThickness=" + Num(RadialThickness(minimumEnd, axis)) +
                        "|maximumThickness=" + Num(RadialThickness(maximumEnd, axis)) +
                        "|minimumVertices=" + minimumEnd.Length +
                        "|maximumVertices=" + maximumEnd.Length);
                }
            }
            File.WriteAllText(Absolute(GeometryReportPath), report.ToString(), Encoding.UTF8);
        }

        private static string HierarchyPath(Transform root, Transform item)
        {
            var names = new Stack<string>();
            for (Transform current = item; current != null; current = current.parent)
            {
                names.Push(current.name);
                if (current == root) break;
            }
            return string.Join("/", names);
        }

        private static List<SubmeshGeometry> CollectSubmeshGeometry(Transform root)
        {
            var result = new List<SubmeshGeometry>();
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                Renderer renderer = filter.GetComponent<Renderer>();
                if (mesh == null || renderer == null || !mesh.isReadable) continue;
                AddSubmeshes(result, root, filter.transform, mesh, renderer.sharedMaterials);
            }
            foreach (SkinnedMeshRenderer renderer in
                root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh mesh = renderer.sharedMesh;
                if (mesh == null || !mesh.isReadable) continue;
                AddSubmeshes(result, root, renderer.transform, mesh, renderer.sharedMaterials);
            }
            return result;
        }

        private static void AddSubmeshes(
            ICollection<SubmeshGeometry> output,
            Transform root,
            Transform meshTransform,
            Mesh mesh,
            Material[] materials)
        {
            Vector3[] vertices = mesh.vertices;
            Matrix4x4 toRoot = root.worldToLocalMatrix * meshTransform.localToWorldMatrix;
            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                int[] indices = mesh.GetIndices(submesh);
                if (indices.Length == 0) continue;
                Bounds bounds = new Bounds(toRoot.MultiplyPoint3x4(vertices[indices[0]]), Vector3.zero);
                foreach (int index in indices.Skip(1))
                    bounds.Encapsulate(toRoot.MultiplyPoint3x4(vertices[index]));
                Material material = submesh < materials.Length ? materials[submesh] : null;
                output.Add(new SubmeshGeometry(
                    bounds,
                    IsBladeIdentity(meshTransform.name, mesh.name, material)));
            }
        }

        private static bool IsBladeIdentity(string transformName, string meshName, Material material)
        {
            string identity = (transformName + " " + meshName + " " +
                (material != null ? material.name : string.Empty)).ToLowerInvariant();
            if (identity.Contains("blade") || identity.Contains("beam") ||
                identity.Contains("laser") ||
                identity.Contains("glow") || identity.Contains("emiss"))
                return true;
            if (material == null || !material.HasProperty("_EmissionColor")) return false;
            Color emission = material.GetColor("_EmissionColor");
            return emission.maxColorComponent > 0.05f;
        }

        private static Vector3 DesiredBladeDirection(Transform target)
        {
            return (target.up +
                target.forward * Mathf.Tan(ForwardTiltDegrees * Mathf.Deg2Rad) +
                target.right * Mathf.Tan(RightTiltDegrees * Mathf.Deg2Rad)).normalized;
        }

        private static Transform RequireRightHand(GameObject target)
        {
            Animator animator = target.GetComponent<Animator>();
            Transform humanoidHand = animator != null && animator.isHuman
                ? animator.GetBoneTransform(HumanBodyBones.RightHand)
                : null;
            Transform pathHand = target.transform.Find(RightHandPath);
            Transform hand = humanoidHand != null ? humanoidHand : pathHand;
            if (hand == null)
                throw new InvalidOperationException(target.name + " RightHand is missing.");
            if (pathHand != null && pathHand != hand)
                throw new InvalidOperationException(
                    target.name + " humanoid/path RightHand mismatch.");
            return hand;
        }

        private static Transform RequireLightsaberProp(Transform hand, string targetName)
        {
            Transform[] props = hand.Cast<Transform>()
                .Where(item => item.name == PropName)
                .ToArray();
            if (props.Length != 1)
                throw new InvalidOperationException(
                    targetName + " Lightsaber_Prop count=" + props.Length + ".");
            return props[0];
        }

        private static void RequireGripFingerHierarchy(Transform hand, string label)
        {
            string[] paths =
            {
                "RightThumbProximal/RightThumbIntermediate/RightThumbDistal",
                "RightIndexProximal/RightIndexIntermediate/RightIndexDistal",
                "RightMiddleProximal/RightMiddleIntermediate/RightMiddleDistal",
                "RightRingProximal/RightRingIntermediate/RightRingDistal",
                "RightLittleProximal/RightLittleIntermediate/RightLittleDistal"
            };
            foreach (string path in paths)
                if (hand.Find(path) == null)
                    throw new InvalidOperationException(
                        label + " right-hand finger path is missing: " + path);
        }

        private static GripMetrics MeasureGrip(
            Transform target,
            ModelGeometry geometry,
            Transform prop)
        {
            Transform upperArm = target.Find(RightArmPath) ??
                throw new InvalidOperationException(target.name + " RightArm is missing.");
            Transform foreArm = target.Find(RightForeArmPath) ??
                throw new InvalidOperationException(target.name + " RightForeArm is missing.");
            Transform hand = target.Find(RightHandPath) ??
                throw new InvalidOperationException(target.name + " RightHand is missing.");
            Vector3 foreArmDirection = (hand.position - foreArm.position).normalized;
            Vector3 fingerDirection = RightFingerDirection(hand);
            Vector3 acrossPalm = RightAcrossPalm(hand);
            Vector3 handleAxis = prop.TransformDirection(geometry.BladeAxisLocal).normalized;
            return new GripMetrics(
                Vector3.Angle(
                    foreArm.position - upperArm.position,
                    hand.position - foreArm.position),
                Vector3.Angle(foreArmDirection, fingerDirection),
                Mathf.Min(
                    Vector3.Angle(acrossPalm, handleAxis),
                    Vector3.Angle(-acrossPalm, handleAxis)),
                Vector3.Distance(
                    PalmCenter(target.gameObject, hand),
                    prop.TransformPoint(geometry.HandleCenterLocal)));
        }

        private static Vector3 RightFingerDirection(Transform hand)
        {
            Transform middle = hand.Find("RightMiddleProximal") ??
                throw new InvalidOperationException("RightMiddleProximal is missing.");
            return (middle.position - hand.position).normalized;
        }

        private static Vector3 RightAcrossPalm(Transform hand)
        {
            Transform index = hand.Find("RightIndexProximal") ??
                throw new InvalidOperationException("RightIndexProximal is missing.");
            Transform little = hand.Find("RightLittleProximal") ??
                throw new InvalidOperationException("RightLittleProximal is missing.");
            return (index.position - little.position).normalized;
        }

        private static Vector3 PalmCenter(GameObject target, Transform hand)
        {
            return Vector3.Lerp(hand.position, KnuckleCenter(target), 0.5f);
        }

        private static float PalmSpan(GameObject target, Transform hand)
        {
            return Vector3.Distance(hand.position, KnuckleCenter(target));
        }

        private static Vector3 KnuckleCenter(GameObject target)
        {
            Transform hand = RequireRightHand(target);
            string[] knuckleNames =
            {
                "RightIndexProximal",
                "RightMiddleProximal",
                "RightRingProximal",
                "RightLittleProximal"
            };
            Transform[] knuckles = knuckleNames
                .Select(hand.Find)
                .Where(item => item != null)
                .ToArray();
            if (knuckles.Length < 3)
                throw new InvalidOperationException(
                    target.name + " has fewer than three mapped right proximal finger bones.");
            return knuckles.Aggregate(
                Vector3.zero,
                (sum, item) => sum + item.position) / knuckles.Length;
        }

        private static void ApplyNaturalOneHandGrip(
            Transform target,
            Transform shoulder,
            Transform upperArm,
            Transform foreArm,
            Transform hand,
            Transform prop,
            ModelGeometry geometry,
            Transform referenceHand,
            Transform referenceShoulder)
        {
            Vector3 handleAxis = prop.TransformDirection(
                geometry.BladeAxisLocal).normalized;
            Vector3 desiredUpperDirection = (
                target.right * 0.38f -
                target.up * 0.78f +
                target.forward * 0.28f).normalized;
            Vector3 foreArmCandidate =
                target.forward * 0.75f -
                target.right * 0.45f +
                target.up * 0.15f;
            Vector3 desiredForeArmDirection = Vector3.ProjectOnPlane(
                foreArmCandidate, handleAxis).normalized;
            if (desiredForeArmDirection.sqrMagnitude < 0.99f)
                throw new InvalidOperationException(
                    target.name + " lightsaber forearm direction is degenerate.");

            shoulder.localRotation = referenceShoulder.localRotation;
            Vector3 shoulderAxis = (upperArm.position - shoulder.position).normalized;
            if (shoulderAxis.sqrMagnitude < 0.99f)
                throw new InvalidOperationException(
                    target.name + " right shoulder axis is degenerate.");
            shoulder.rotation = Quaternion.AngleAxis(
                6f, shoulderAxis) * shoulder.rotation;
            upperArm.rotation = Quaternion.FromToRotation(
                foreArm.position - upperArm.position,
                desiredUpperDirection) * upperArm.rotation;
            foreArm.rotation = Quaternion.FromToRotation(
                hand.position - foreArm.position,
                desiredForeArmDirection) * foreArm.rotation;

            CopyFingerGripPose(referenceHand, hand);
            hand.rotation = Quaternion.FromToRotation(
                RightFingerDirection(hand),
                desiredForeArmDirection) * hand.rotation;

            Vector3 currentAcross = Vector3.ProjectOnPlane(
                RightAcrossPalm(hand), desiredForeArmDirection).normalized;
            Vector3 desiredAcross = Vector3.ProjectOnPlane(
                handleAxis, desiredForeArmDirection).normalized;
            if (currentAcross.sqrMagnitude < 0.99f ||
                desiredAcross.sqrMagnitude < 0.99f)
                throw new InvalidOperationException(
                    target.name + " lightsaber grip twist is degenerate.");
            float twist = Vector3.SignedAngle(
                currentAcross, desiredAcross, desiredForeArmDirection);
            foreArm.rotation = Quaternion.AngleAxis(
                twist * 0.72f, desiredForeArmDirection) * foreArm.rotation;

            currentAcross = Vector3.ProjectOnPlane(
                RightAcrossPalm(hand), desiredForeArmDirection).normalized;
            float residual = Vector3.SignedAngle(
                currentAcross, desiredAcross, desiredForeArmDirection);
            hand.rotation = Quaternion.AngleAxis(
                residual, desiredForeArmDirection) * hand.rotation;

            Quaternion anatomicalBalance = Quaternion.FromToRotation(
                RightAcrossPalm(hand), handleAxis);
            hand.rotation = Quaternion.Slerp(
                Quaternion.identity, anatomicalBalance, 0.5f) * hand.rotation;
        }

        private static void ApplyLoweredArmFromLeft(
            Transform target,
            Transform shoulder,
            Transform upperArm,
            Transform foreArm,
            Transform hand,
            Transform prop,
            ModelGeometry geometry,
            Transform referenceRightShoulder,
            Transform referenceRightHand,
            Quaternion preservedHandleWorldRotation)
        {
            Transform leftArm = target.Find(LeftArmPath) ??
                throw new InvalidOperationException(target.name + " LeftArm is missing.");
            Transform leftForeArm = target.Find(LeftForeArmPath) ??
                throw new InvalidOperationException(target.name + " LeftForeArm is missing.");
            Transform leftHand = target.Find(LeftHandPath) ??
                throw new InvalidOperationException(target.name + " LeftHand is missing.");
            Vector3 desiredUpperDirection = MirrorDirectionAcrossTarget(
                target, leftForeArm.position - leftArm.position);
            Vector3 desiredForeArmDirection = MirrorDirectionAcrossTarget(
                target, leftHand.position - leftForeArm.position);

            shoulder.localRotation = referenceRightShoulder.localRotation;
            upperArm.rotation = Quaternion.FromToRotation(
                foreArm.position - upperArm.position,
                desiredUpperDirection) * upperArm.rotation;
            foreArm.rotation = Quaternion.FromToRotation(
                hand.position - foreArm.position,
                desiredForeArmDirection) * foreArm.rotation;
            hand.localRotation = referenceRightHand.localRotation;

            Vector3 handleAxis = (
                preservedHandleWorldRotation * geometry.BladeAxisLocal).normalized;
            Vector3 foreArmDirection = (hand.position - foreArm.position).normalized;
            hand.rotation = Quaternion.FromToRotation(
                RightFingerDirection(hand), foreArmDirection) * hand.rotation;

            Vector3 currentAcross = Vector3.ProjectOnPlane(
                RightAcrossPalm(hand), foreArmDirection).normalized;
            Vector3 desiredAcross = Vector3.ProjectOnPlane(
                handleAxis, foreArmDirection).normalized;
            if (currentAcross.sqrMagnitude > 0.5f && desiredAcross.sqrMagnitude > 0.05f)
            {
                float twist = Vector3.SignedAngle(
                    currentAcross, desiredAcross, foreArmDirection);
                foreArm.rotation = Quaternion.AngleAxis(
                    twist * 0.72f, foreArmDirection) * foreArm.rotation;
                currentAcross = Vector3.ProjectOnPlane(
                    RightAcrossPalm(hand), foreArmDirection).normalized;
                float residual = Vector3.SignedAngle(
                    currentAcross, desiredAcross, foreArmDirection);
                hand.rotation = Quaternion.AngleAxis(
                    residual, foreArmDirection) * hand.rotation;
            }

            Quaternion anatomicalBalance = Quaternion.FromToRotation(
                RightAcrossPalm(hand), handleAxis);
            hand.rotation = Quaternion.Slerp(
                Quaternion.identity, anatomicalBalance, 0.5f) * hand.rotation;
        }

        private static Vector3 MirrorDirectionAcrossTarget(
            Transform target,
            Vector3 worldDirection)
        {
            Vector3 local = target.InverseTransformDirection(worldDirection).normalized;
            local.x = -local.x;
            return target.TransformDirection(local).normalized;
        }

        private static Vector3 MirrorPointAcrossTarget(
            Transform target,
            Vector3 worldPoint)
        {
            Vector3 local = target.InverseTransformPoint(worldPoint);
            local.x = -local.x;
            return target.TransformPoint(local);
        }

        private static LoweredArmMetrics MeasureLoweredArm(
            Transform target,
            ModelGeometry geometry,
            Transform prop)
        {
            Transform leftArm = target.Find(LeftArmPath) ??
                throw new InvalidOperationException(target.name + " LeftArm is missing.");
            Transform leftForeArm = target.Find(LeftForeArmPath) ??
                throw new InvalidOperationException(target.name + " LeftForeArm is missing.");
            Transform leftHand = target.Find(LeftHandPath) ??
                throw new InvalidOperationException(target.name + " LeftHand is missing.");
            Transform rightArm = target.Find(RightArmPath) ??
                throw new InvalidOperationException(target.name + " RightArm is missing.");
            Transform rightForeArm = target.Find(RightForeArmPath) ??
                throw new InvalidOperationException(target.name + " RightForeArm is missing.");
            Transform rightHand = target.Find(RightHandPath) ??
                throw new InvalidOperationException(target.name + " RightHand is missing.");

            Vector3 leftUpperDirection = (leftForeArm.position - leftArm.position).normalized;
            Vector3 leftForeArmDirection = (leftHand.position - leftForeArm.position).normalized;
            Vector3 rightUpperDirection = (rightForeArm.position - rightArm.position).normalized;
            Vector3 rightForeArmDirection = (rightHand.position - rightForeArm.position).normalized;
            float leftElbow = Vector3.Angle(leftUpperDirection, leftForeArmDirection);
            float rightElbow = Vector3.Angle(rightUpperDirection, rightForeArmDirection);
            GripMetrics grip = MeasureGrip(target, geometry, prop);
            return new LoweredArmMetrics(
                Vector3.Distance(
                    rightArm.position,
                    MirrorPointAcrossTarget(target, leftArm.position)),
                Vector3.Distance(
                    rightHand.position,
                    MirrorPointAcrossTarget(target, leftHand.position)),
                Mathf.Abs(target.InverseTransformPoint(rightHand.position).y -
                    target.InverseTransformPoint(leftHand.position).y),
                Vector3.Angle(
                    rightUpperDirection,
                    MirrorDirectionAcrossTarget(target, leftUpperDirection)),
                Vector3.Angle(
                    rightForeArmDirection,
                    MirrorDirectionAcrossTarget(target, leftForeArmDirection)),
                Mathf.Abs(rightElbow - leftElbow),
                grip.WristDeviationDegrees,
                grip.HandleAxisErrorDegrees,
                grip.PalmCenterErrorMeters,
                float.NaN);
        }

        private static LoweredArmMetrics RequireLoweredArmGrip(
            Transform target,
            ModelGeometry geometry,
            Transform prop,
            Transform vacuumHand)
        {
            LoweredArmMetrics metrics = MeasureLoweredArm(target, geometry, prop);
            Transform rightHand = target.Find(RightHandPath) ??
                throw new InvalidOperationException(target.name + " RightHand is missing.");
            Transform rightArm = target.Find(RightArmPath) ??
                throw new InvalidOperationException(target.name + " RightArm is missing.");
            float fingerPoseError = MaximumFingerPoseError(vacuumHand, rightHand);
            float handDrop = target.InverseTransformPoint(rightArm.position).y -
                target.InverseTransformPoint(rightHand.position).y;
            if (metrics.RightArmRootMirrorErrorMeters > 0.06f ||
                metrics.RightHandMirrorErrorMeters >
                    metrics.RightArmRootMirrorErrorMeters + 0.01f ||
                metrics.HandHeightDifferenceMeters > 0.025f ||
                metrics.UpperDirectionMirrorErrorDegrees > 0.25f ||
                metrics.ForeArmDirectionMirrorErrorDegrees > 0.25f ||
                metrics.ElbowDifferenceDegrees > 0.25f ||
                metrics.WristDeviationDegrees > 50f ||
                metrics.HandleAxisErrorDegrees > 50f ||
                metrics.PalmCenterErrorMeters > PositionToleranceMeters ||
                fingerPoseError > AngleToleranceDegrees || handDrop < 0.25f)
                throw new InvalidOperationException(
                    target.name + " lowered right-arm grip failed. " +
                    metrics.Describe() + "|fingerPoseErrorDegrees=" +
                    Num(fingerPoseError) + "|rightHandDropMeters=" + Num(handDrop));
            return metrics.WithFingerPoseError(fingerPoseError);
        }

        private static List<RotationSnapshot> CaptureTargetFingerRotations(Scene scene)
        {
            var result = new List<RotationSnapshot>();
            string[] fingerRoots =
            {
                "RightThumbProximal",
                "RightIndexProximal",
                "RightMiddleProximal",
                "RightRingProximal",
                "RightLittleProximal"
            };
            foreach (string targetName in TargetNames)
            {
                Transform hand = RequireRightHand(FindUnique(scene, targetName));
                foreach (string rootName in fingerRoots)
                {
                    Transform root = hand.Find(rootName) ??
                        throw new InvalidOperationException(
                            targetName + " finger root is missing: " + rootName);
                    result.AddRange(root.GetComponentsInChildren<Transform>(true)
                        .Select(item => new RotationSnapshot(item)));
                }
            }
            return result;
        }

        private static void CopyFingerGripPose(
            Transform referenceHand,
            Transform targetHand)
        {
            string[] fingerRoots =
            {
                "RightThumbProximal",
                "RightIndexProximal",
                "RightMiddleProximal",
                "RightRingProximal",
                "RightLittleProximal"
            };
            foreach (string rootName in fingerRoots)
            {
                Transform sourceRoot = referenceHand.Find(rootName) ??
                    throw new InvalidOperationException(
                        "Vacuum_Idle finger root is missing: " + rootName);
                foreach (Transform source in sourceRoot.GetComponentsInChildren<Transform>(true))
                {
                    string relativePath = AnimationUtility.CalculateTransformPath(
                        source, referenceHand);
                    Transform destination = targetHand.Find(relativePath) ??
                        throw new InvalidOperationException(
                            "Lightsaber finger path is missing: " + relativePath);
                    destination.localRotation = source.localRotation;
                }
            }
        }

        private static float MaximumFingerPoseError(
            Transform referenceHand,
            Transform targetHand)
        {
            float maximum = 0f;
            string[] fingerRoots =
            {
                "RightThumbProximal",
                "RightIndexProximal",
                "RightMiddleProximal",
                "RightRingProximal",
                "RightLittleProximal"
            };
            foreach (string rootName in fingerRoots)
            {
                Transform sourceRoot = referenceHand.Find(rootName) ??
                    throw new InvalidOperationException(
                        "Vacuum_Idle finger root is missing: " + rootName);
                foreach (Transform source in sourceRoot.GetComponentsInChildren<Transform>(true))
                {
                    string relativePath = AnimationUtility.CalculateTransformPath(
                        source, referenceHand);
                    Transform destination = targetHand.Find(relativePath) ??
                        throw new InvalidOperationException(
                            "Lightsaber finger path is missing: " + relativePath);
                    maximum = Mathf.Max(maximum, Quaternion.Angle(
                        source.localRotation, destination.localRotation));
                }
            }
            return maximum;
        }

        private static GripMetrics RequireNaturalGrip(
            Transform target,
            ModelGeometry geometry,
            Transform prop,
            Transform referenceHand)
        {
            GripMetrics metrics = MeasureGrip(target, geometry, prop);
            Transform hand = target.Find(RightHandPath) ??
                throw new InvalidOperationException(target.name + " RightHand is missing.");
            float fingerPoseError = MaximumFingerPoseError(referenceHand, hand);
            if (metrics.ElbowBendDegrees < 55f || metrics.ElbowBendDegrees > 120f ||
                metrics.WristDeviationDegrees > 12f ||
                metrics.HandleAxisErrorDegrees > 12f ||
                metrics.PalmCenterErrorMeters > PositionToleranceMeters ||
                fingerPoseError > AngleToleranceDegrees)
                throw new InvalidOperationException(
                    target.name + " natural lightsaber grip failed. " +
                    metrics.Describe() + "|fingerPoseErrorDegrees=" +
                    Num(fingerPoseError));
            return new GripMetrics(
                metrics.ElbowBendDegrees,
                metrics.WristDeviationDegrees,
                metrics.HandleAxisErrorDegrees,
                metrics.PalmCenterErrorMeters,
                fingerPoseError);
        }

        private static IEnumerable<Transform> AllowedRotationTransforms(GameObject target)
        {
            Transform shoulder = target.transform.Find(RightShoulderPath) ??
                throw new InvalidOperationException(target.name + " RightShoulder is missing.");
            Transform upperArm = target.transform.Find(RightArmPath) ??
                throw new InvalidOperationException(target.name + " RightArm is missing.");
            Transform foreArm = target.transform.Find(RightForeArmPath) ??
                throw new InvalidOperationException(target.name + " RightForeArm is missing.");
            Transform hand = RequireRightHand(target);
            var allowed = new HashSet<Transform>
            {
                shoulder,
                upperArm,
                foreArm,
                hand,
                RequireLightsaberProp(hand, target.name)
            };
            string[] fingerRoots =
            {
                "RightThumbProximal",
                "RightIndexProximal",
                "RightMiddleProximal",
                "RightRingProximal",
                "RightLittleProximal"
            };
            foreach (string rootName in fingerRoots)
            {
                Transform root = hand.Find(rootName) ??
                    throw new InvalidOperationException(
                        target.name + " finger root is missing: " + rootName);
                foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                    allowed.Add(item);
            }
            return allowed;
        }

        private static List<TransformSnapshot> CaptureGripProtectedTransforms(Scene scene)
        {
            var allowedSubtrees = new HashSet<Transform>();
            foreach (string targetName in TargetNames)
            {
                Transform shoulder = FindUnique(scene, targetName).transform.Find(
                    RightShoulderPath) ?? throw new InvalidOperationException(
                    targetName + " RightShoulder is missing.");
                foreach (Transform item in shoulder.GetComponentsInChildren<Transform>(true))
                    allowedSubtrees.Add(item);
            }
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => !allowedSubtrees.Contains(item))
                .Select(item => new TransformSnapshot(item))
                .ToList();
        }

        private static List<NonRotationTransformSnapshot> CaptureAllowedGripNonRotation(
            Scene scene)
        {
            var result = new List<NonRotationTransformSnapshot>();
            foreach (string targetName in TargetNames)
            {
                GameObject target = FindUnique(scene, targetName);
                Transform shoulder = target.transform.Find(
                    RightShoulderPath) ?? throw new InvalidOperationException(
                    targetName + " RightShoulder is missing.");
                Transform prop = RequireLightsaberProp(
                    RequireRightHand(target), targetName);
                result.AddRange(shoulder.GetComponentsInChildren<Transform>(true)
                    .Where(item => item != prop)
                    .Select(item => new NonRotationTransformSnapshot(item)));
            }
            return result;
        }

        private static List<RotationSnapshot> CaptureProtectedAllowedRotations(Scene scene)
        {
            var result = new List<RotationSnapshot>();
            foreach (string targetName in TargetNames)
            {
                GameObject target = FindUnique(scene, targetName);
                var allowed = new HashSet<Transform>(AllowedRotationTransforms(target));
                Transform shoulder = target.transform.Find(RightShoulderPath) ??
                    throw new InvalidOperationException(targetName + " RightShoulder is missing.");
                result.AddRange(shoulder.GetComponentsInChildren<Transform>(true)
                    .Where(item => !allowed.Contains(item))
                    .Select(item => new RotationSnapshot(item)));
            }
            return result;
        }

        private static void RequireLightsaberDesignSource()
        {
            const string designPath = "docs/GAME_DESIGN_SOURCE.txt";
            string source = File.ReadAllText(Absolute(designPath), Encoding.UTF8);
            if (!source.Contains("광선검: 양손무기, 길이 90cm"))
                throw new InvalidOperationException(
                    "The original 90cm lightsaber design statement is missing.");
        }

        private static GameObject RequireBladePrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BladePrefabPath) ??
                throw new InvalidOperationException(
                    "Approved lightsaber blade prefab is missing: " + BladePrefabPath);
            RequireBladePrefabStructure(prefab);
            return prefab;
        }

        private static void RequireBladePrefabStructure(GameObject prefab)
        {
            Transform core = prefab.transform.Find("WhiteCore") ??
                throw new InvalidOperationException(
                    "Approved blade prefab WhiteCore is missing.");
            Transform glow = prefab.transform.Find("BlueOuterGlow") ??
                throw new InvalidOperationException(
                    "Approved blade prefab BlueOuterGlow is missing.");
            Transform lightTransform = prefab.transform.Find("BlueBladeLight") ??
                throw new InvalidOperationException(
                    "Approved blade prefab BlueBladeLight is missing.");
            Mesh coreMesh = core.GetComponent<MeshFilter>()?.sharedMesh;
            Mesh glowMesh = glow.GetComponent<MeshFilter>()?.sharedMesh;
            Material coreMaterial = core.GetComponent<MeshRenderer>()?.sharedMaterial;
            Material glowMaterial = glow.GetComponent<MeshRenderer>()?.sharedMaterial;
            Light light = lightTransform.GetComponent<Light>();
            if (AssetDatabase.GetAssetPath(coreMesh) != BladeCoreMeshPath ||
                AssetDatabase.GetAssetPath(glowMesh) != BladeGlowMeshPath ||
                AssetDatabase.GetAssetPath(coreMaterial) != BladeCoreMaterialPath ||
                AssetDatabase.GetAssetPath(glowMaterial) != BladeGlowMaterialPath ||
                light == null || !light.enabled)
                throw new InvalidOperationException(
                    "Approved blade prefab mesh, material, or light linkage is incomplete.");
            RequireNearMetric(
                coreMesh.bounds.size.x,
                0.6261548f,
                0.00001f,
                "approved blade mesh length");
            RequireNearMetric(
                glowMesh.bounds.size.y,
                ApprovedBladeOuterDiameterMeters,
                0.00001f,
                "approved blade outer diameter");
        }

        private static float ApprovedBladeLengthMeters()
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(BladeCoreMeshPath) ??
                throw new InvalidOperationException("Approved blade core mesh is missing.");
            return mesh.bounds.size.x;
        }

        private static Vector3 DetermineEmitterLocal(
            Transform modelRoot,
            ModelGeometry geometry)
        {
            Vector3[] vertices = CollectLocalVertices(modelRoot).ToArray();
            if (vertices.Length == 0)
                throw new InvalidOperationException(
                    "Lightsaber handle has no readable vertices for emitter placement.");
            Bounds bounds = new Bounds(vertices[0], Vector3.zero);
            foreach (Vector3 vertex in vertices.Skip(1)) bounds.Encapsulate(vertex);
            Vector3 axis = geometry.BladeAxisLocal.normalized;
            float maximum = vertices.Max(vertex => Vector3.Dot(vertex, axis));
            return bounds.center + axis *
                (maximum - Vector3.Dot(bounds.center, axis));
        }

        private static Dictionary<string, string> CaptureApprovedBladeAssetHashes()
        {
            return new[]
                {
                    BladePrefabPath,
                    BladeCoreMeshPath,
                    BladeGlowMeshPath,
                    BladeCoreMaterialPath,
                    BladeGlowMaterialPath,
                    BladeShaderPath
                }
                .ToDictionary(
                    path => path,
                    path => Sha256File(Absolute(path)),
                    StringComparer.Ordinal);
        }

        private static void RequireApprovedBladeAssetHashesUnchanged(
            IReadOnlyDictionary<string, string> expected)
        {
            foreach (KeyValuePair<string, string> item in expected)
                RequireEqual(
                    item.Value,
                    Sha256File(Absolute(item.Key)),
                    "approved blade asset " + item.Key);
        }

        private static void InspectLightsaberBladeApplicationInternal(
            Scene scene,
            ModelGeometry geometry,
            Vector3 emitterLocal)
        {
            GameObject prefab = RequireBladePrefab();
            Mesh coreMesh = AssetDatabase.LoadAssetAtPath<Mesh>(BladeCoreMeshPath) ??
                throw new InvalidOperationException("Approved blade core mesh is missing.");
            Mesh glowMesh = AssetDatabase.LoadAssetAtPath<Mesh>(BladeGlowMeshPath) ??
                throw new InvalidOperationException("Approved blade glow mesh is missing.");
            var report = new StringBuilder()
                .AppendLine("Lightsaber blade application read-only inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("userOverride=All six targets continuously lit")
                .AppendLine("approvedPrefab=" + AssetDatabase.GetAssetPath(prefab));

            foreach (string targetName in TargetNames)
            {
                GameObject target = FindUnique(scene, targetName);
                Transform hand = RequireRightHand(target);
                Transform prop = RequireLightsaberProp(hand, targetName);
                Transform[] blades = prop.Cast<Transform>()
                    .Where(item => item.name == BladeName)
                    .ToArray();
                if (blades.Length != 1)
                    throw new InvalidOperationException(
                        targetName + " " + BladeName + " count=" + blades.Length + ".");
                Transform blade = blades[0];
                string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                    blade.gameObject);
                if (!string.Equals(prefabPath, BladePrefabPath, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        targetName + " blade does not use the approved prefab: " + prefabPath);
                if (blade.parent != prop || !blade.IsChildOf(hand))
                    throw new InvalidOperationException(
                        targetName + " blade is not following its RightHand handle hierarchy.");
                RequireNearMetric(
                    Vector3.Distance(blade.localPosition, emitterLocal),
                    0f,
                    0.00001f,
                    targetName + " handle emitter attachment error");
                RequireNearMetric(
                    Quaternion.Angle(blade.localRotation, Quaternion.identity),
                    0f,
                    0.001f,
                    targetName + " blade/handle axis error");

                Transform core = blade.Find("WhiteCore") ??
                    throw new InvalidOperationException(targetName + " WhiteCore is missing.");
                Transform glow = blade.Find("BlueOuterGlow") ??
                    throw new InvalidOperationException(
                        targetName + " BlueOuterGlow is missing.");
                Transform lightTransform = blade.Find("BlueBladeLight") ??
                    throw new InvalidOperationException(
                        targetName + " BlueBladeLight is missing.");
                MeshRenderer coreRenderer = core.GetComponent<MeshRenderer>();
                MeshRenderer glowRenderer = glow.GetComponent<MeshRenderer>();
                Light light = lightTransform.GetComponent<Light>();
                if (!blade.gameObject.activeSelf ||
                    coreRenderer == null || !coreRenderer.enabled ||
                    glowRenderer == null || !glowRenderer.enabled ||
                    light == null || !light.enabled)
                    throw new InvalidOperationException(
                        targetName + " blade is not continuously emitting.");

                float handleLength = prop.TransformVector(
                    geometry.BladeAxisLocal * geometry.HandleLengthLocal).magnitude;
                float bladeLength = core.TransformVector(
                    Vector3.right * coreMesh.bounds.size.x).magnitude;
                float totalLength = handleLength + bladeLength;
                float outerDiameter = Mathf.Max(
                    glow.TransformVector(Vector3.up * glowMesh.bounds.size.y).magnitude,
                    glow.TransformVector(Vector3.forward * glowMesh.bounds.size.z).magnitude);
                RequireNearMetric(
                    totalLength,
                    LightsaberTotalLengthMeters,
                    0.003f,
                    targetName + " total lightsaber length");
                RequireNearMetric(
                    outerDiameter,
                    ApprovedBladeOuterDiameterMeters,
                    0.003f,
                    targetName + " blade outer diameter");
                report.AppendLine(
                    targetName +
                    "|totalLengthMeters=" + Num(totalLength) +
                    "|handleLengthMeters=" + Num(handleLength) +
                    "|bladeLengthMeters=" + Num(bladeLength) +
                    "|outerDiameterMeters=" + Num(outerDiameter) +
                    "|emitterErrorMeters=" + Num(Vector3.Distance(
                        blade.position, prop.TransformPoint(emitterLocal))) +
                    "|continuousEmission=True|rightHandFollow=True");
            }
            Debug.Log(report.ToString());
        }

        private static string TransformSignature(Transform item)
        {
            return Precise(item.localPosition) + "|" +
                Precise(item.localRotation) + "|" +
                Precise(item.localScale);
        }

        private static string HandleRendererSignature(Transform prop)
        {
            return string.Join("\n", prop.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => !HasAncestorNamed(renderer.transform, BladeName))
                .OrderBy(renderer => AnimationUtility.CalculateTransformPath(
                    renderer.transform, prop), StringComparer.Ordinal)
                .Select(renderer =>
                {
                    Mesh mesh = renderer is SkinnedMeshRenderer skinned
                        ? skinned.sharedMesh
                        : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                    return AnimationUtility.CalculateTransformPath(renderer.transform, prop) +
                        "|" + renderer.GetType().FullName +
                        "|mesh=" + ObjectIdentity(mesh) +
                        "|materials=" + string.Join(",", renderer.sharedMaterials
                            .Select(ObjectIdentity));
                }));
        }

        private static void RequireNearMetric(
            float actual,
            float expected,
            float tolerance,
            string label)
        {
            if (Mathf.Abs(actual - expected) > tolerance)
                throw new InvalidOperationException(
                    label + " expected=" + Num(expected) +
                    ", actual=" + Num(actual) + ".");
        }

        private static string RendererSignature(Transform root)
        {
            return string.Join("\n", root.GetComponentsInChildren<Renderer>(true)
                .OrderBy(item => AnimationUtility.CalculateTransformPath(
                    item.transform, root), StringComparer.Ordinal)
                .Select(renderer =>
                {
                    Mesh mesh = renderer is SkinnedMeshRenderer skinned
                        ? skinned.sharedMesh
                        : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                    return AnimationUtility.CalculateTransformPath(renderer.transform, root) +
                        "|" + renderer.GetType().FullName +
                        "|mesh=" + ObjectIdentity(mesh) +
                        "|materials=" + string.Join(",", renderer.sharedMaterials
                            .Select(ObjectIdentity));
                }));
        }

        private static Color[] CaptureGripTargetPanel(Transform target)
        {
            Transform shoulder = target.Find(RightShoulderPath) ??
                throw new InvalidOperationException(target.name + " RightShoulder is missing.");
            Transform upperArm = target.Find(RightArmPath) ??
                throw new InvalidOperationException(target.name + " RightArm is missing.");
            Transform foreArm = target.Find(RightForeArmPath) ??
                throw new InvalidOperationException(target.name + " RightForeArm is missing.");
            Transform hand = target.Find(RightHandPath) ??
                throw new InvalidOperationException(target.name + " RightHand is missing.");
            Transform prop = RequireLightsaberProp(hand, target.name);
            Bounds bounds = new Bounds(shoulder.position, Vector3.zero);
            bounds.Encapsulate(upperArm.position);
            bounds.Encapsulate(foreArm.position);
            bounds.Encapsulate(hand.position);
            foreach (Renderer renderer in prop.GetComponentsInChildren<Renderer>(true))
                if (renderer.enabled) bounds.Encapsulate(renderer.bounds);
            bounds.Expand(0.16f);

            GameObject cameraObject = new GameObject("LightsaberGripPoseReviewCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            GameObject lightObject = new GameObject("LightsaberGripPoseReviewLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            Camera camera = cameraObject.AddComponent<Camera>();
            Light light = lightObject.AddComponent<Light>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.02f, 0.03f, 1f);
            camera.orthographic = true;
            camera.aspect = PanelWidth / (float)PanelHeight;
            camera.orthographicSize = Mathf.Max(
                bounds.extents.y,
                bounds.extents.x / camera.aspect) * 1.18f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            Vector3 viewDirection = (
                target.forward + target.right * 0.48f + target.up * 0.08f).normalized;
            camera.transform.SetPositionAndRotation(
                bounds.center + viewDirection * 3f,
                Quaternion.LookRotation(-viewDirection, target.up));
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.transform.rotation = Quaternion.LookRotation(
                -viewDirection - target.up * 0.25f, target.up);

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture render = RenderTexture.GetTemporary(
                PanelWidth,
                PanelHeight,
                24,
                RenderTextureFormat.ARGB32);
            var texture = new Texture2D(
                PanelWidth,
                PanelHeight,
                TextureFormat.RGB24,
                false);
            try
            {
                camera.targetTexture = render;
                camera.Render();
                RenderTexture.active = render;
                texture.ReadPixels(new Rect(0, 0, PanelWidth, PanelHeight), 0, 0);
                texture.Apply(false, false);
                return texture.GetPixels();
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private static Color[] CaptureLoweredComparisonPanel(Transform target)
        {
            Transform leftShoulder = target.Find(LeftShoulderPath) ??
                throw new InvalidOperationException(target.name + " LeftShoulder is missing.");
            Transform leftHand = target.Find(LeftHandPath) ??
                throw new InvalidOperationException(target.name + " LeftHand is missing.");
            Transform rightShoulder = target.Find(RightShoulderPath) ??
                throw new InvalidOperationException(target.name + " RightShoulder is missing.");
            Transform rightHand = target.Find(RightHandPath) ??
                throw new InvalidOperationException(target.name + " RightHand is missing.");
            Transform prop = RequireLightsaberProp(rightHand, target.name);
            Bounds bounds = new Bounds(leftShoulder.position, Vector3.zero);
            bounds.Encapsulate(rightShoulder.position);
            bounds.Encapsulate(leftHand.position);
            bounds.Encapsulate(rightHand.position);
            foreach (Renderer renderer in prop.GetComponentsInChildren<Renderer>(true))
                if (renderer.enabled) bounds.Encapsulate(renderer.bounds);
            bounds.Expand(0.24f);

            int halfWidth = PanelWidth / 2;
            Color[] front = RenderLoweredArmView(
                target,
                bounds,
                target.forward,
                halfWidth,
                PanelHeight);
            Color[] oblique = RenderLoweredArmView(
                target,
                bounds,
                (target.forward + target.right * 0.62f).normalized,
                PanelWidth - halfWidth,
                PanelHeight);
            var panel = new Color[PanelWidth * PanelHeight];
            for (int y = 0; y < PanelHeight; y++)
            {
                Array.Copy(front, y * halfWidth, panel, y * PanelWidth, halfWidth);
                Array.Copy(
                    oblique,
                    y * (PanelWidth - halfWidth),
                    panel,
                    y * PanelWidth + halfWidth,
                    PanelWidth - halfWidth);
            }
            return panel;
        }

        private static Color[] RenderLoweredArmView(
            Transform target,
            Bounds bounds,
            Vector3 viewDirection,
            int width,
            int height)
        {
            GameObject cameraObject = new GameObject("LightsaberLoweredGripReviewCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            GameObject lightObject = new GameObject("LightsaberLoweredGripReviewLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            Camera camera = cameraObject.AddComponent<Camera>();
            Light light = lightObject.AddComponent<Light>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.02f, 0.03f, 1f);
            camera.orthographic = true;
            camera.aspect = width / (float)height;
            camera.orthographicSize = Mathf.Max(
                bounds.extents.y,
                bounds.extents.x / camera.aspect) * 1.12f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.transform.SetPositionAndRotation(
                bounds.center + viewDirection.normalized * 3f,
                Quaternion.LookRotation(-viewDirection.normalized, target.up));
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.transform.rotation = Quaternion.LookRotation(
                -viewDirection.normalized - target.up * 0.25f, target.up);

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture render = RenderTexture.GetTemporary(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = render;
                camera.Render();
                RenderTexture.active = render;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply(false, false);
                return texture.GetPixels();
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private static Color[] CaptureTargetPanel(Transform target)
        {
            Bounds bounds = BoundsOf(target);
            GameObject cameraObject = new GameObject("LightsaberFinalReviewCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.02f, 0.03f, 1f);
            camera.fieldOfView = 36f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.aspect = PanelWidth / (float)PanelHeight;

            float verticalHalf = camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float horizontalHalf = Mathf.Atan(Mathf.Tan(verticalHalf) * camera.aspect);
            float distance = Mathf.Max(
                bounds.extents.y / Mathf.Tan(verticalHalf),
                bounds.extents.x / Mathf.Tan(horizontalHalf));
            distance += bounds.extents.z + 0.5f;
            camera.transform.position = bounds.center + target.forward * distance;
            camera.transform.rotation = Quaternion.LookRotation(
                bounds.center - camera.transform.position,
                target.up);

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture render = RenderTexture.GetTemporary(
                PanelWidth,
                PanelHeight,
                24,
                RenderTextureFormat.ARGB32);
            var texture = new Texture2D(
                PanelWidth,
                PanelHeight,
                TextureFormat.RGB24,
                false);
            try
            {
                camera.targetTexture = render;
                camera.Render();
                RenderTexture.active = render;
                texture.ReadPixels(new Rect(0, 0, PanelWidth, PanelHeight), 0, 0);
                texture.Apply(false, false);
                return texture.GetPixels();
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Bounds BoundsOf(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(root.name + " has no enabled renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static List<TransformSnapshot> CaptureExistingTransforms(Scene scene)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name != PropName && !HasAncestorNamed(item, PropName))
                .Select(item => new TransformSnapshot(item))
                .ToList();
        }

        private static void RequireExistingTransformsUnchanged(
            IEnumerable<TransformSnapshot> snapshots)
        {
            foreach (TransformSnapshot snapshot in snapshots) snapshot.RequireUnchanged();
        }

        private static bool HasAncestorNamed(Transform item, string name)
        {
            for (Transform current = item.parent; current != null; current = current.parent)
                if (current.name == name) return true;
            return false;
        }

        private static string AnimatorSignature(GameObject target)
        {
            Animator animator = target.GetComponent<Animator>() ??
                throw new InvalidOperationException(target.name + " Animator is missing.");
            return ObjectIdentity(animator.runtimeAnimatorController) + "|" +
                ObjectIdentity(animator.avatar) + "|" + animator.applyRootMotion + "|" +
                animator.enabled + "|" + (int)animator.cullingMode + "|" +
                (int)animator.updateMode;
        }

        private static string ObjectIdentity(UnityEngine.Object item)
        {
            if (item == null) return "null";
            string path = AssetDatabase.GetAssetPath(item);
            return path + "|" + AssetDatabase.AssetPathToGUID(path) + "|" + item.name;
        }

        private static void RequireAuthoredPropTransform(Transform prop, string targetName)
        {
            if (Vector3.Distance(prop.localPosition, AuthoredPropLocalPosition) > 0.000001f ||
                Quaternion.Angle(prop.localRotation, AuthoredPropLocalRotation) > 0.0001f ||
                Vector3.Distance(prop.localScale, AuthoredPropLocalScale) > 0.000001f)
                throw new InvalidOperationException(
                    targetName + " Lightsaber_Prop does not match the user-authored " +
                    "Lightsaber_Off_Idle local transform. actualPosition=" +
                    Precise(prop.localPosition) + "|actualRotation=" +
                    Precise(prop.localRotation) + "|actualScale=" +
                    Precise(prop.localScale));
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must be active. ActiveScene=" + scene.path);
            return scene;
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

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Lightsaber setup requires Edit Mode.");
        }

        private static Texture2D[] LoadTextures()
        {
            if (!AssetDatabase.IsValidFolder(TextureFolder)) return Array.Empty<Texture2D>();
            return AssetDatabase.FindAssets("t:Texture2D", new[] { TextureFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Texture2D>)
                .Where(item => item != null)
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .ToArray();
        }

        private static Texture2D FindTexture(
            IEnumerable<Texture2D> textures,
            params string[] words)
        {
            Texture2D match = textures.SingleOrDefault(texture =>
                AssetDatabase.GetAssetPath(texture) != MetallicSmoothnessPath &&
                words.All(word => texture.name.IndexOf(
                    word,
                    StringComparison.OrdinalIgnoreCase) >= 0));
            return match ?? throw new InvalidOperationException(
                "Lightsaber embedded texture is missing: " + string.Join("+", words));
        }

        private static Texture2D CreateMetallicSmoothness(
            Texture2D metallic,
            Texture2D roughness)
        {
            SetReadable(metallic, true);
            SetReadable(roughness, true);
            if (metallic.width != roughness.width || metallic.height != roughness.height)
                throw new InvalidOperationException(
                    "Lightsaber metallic and roughness texture sizes differ.");
            Color32[] metallicPixels = metallic.GetPixels32();
            Color32[] roughnessPixels = roughness.GetPixels32();
            var outputPixels = new Color32[metallicPixels.Length];
            for (int index = 0; index < outputPixels.Length; index++)
            {
                byte metal = metallicPixels[index].r;
                outputPixels[index] = new Color32(
                    metal,
                    metal,
                    metal,
                    (byte)(255 - roughnessPixels[index].r));
            }
            var output = new Texture2D(
                metallic.width,
                metallic.height,
                TextureFormat.RGBA32,
                false,
                true);
            try
            {
                output.SetPixels32(outputPixels);
                output.Apply(false, false);
                File.WriteAllBytes(Absolute(MetallicSmoothnessPath), output.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(output);
            }
            AssetDatabase.ImportAsset(
                MetallicSmoothnessPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(MetallicSmoothnessPath) ??
                throw new InvalidOperationException(
                    "Lightsaber metallic/smoothness packing import failed.");
        }

        private static void ConfigureTextureImporters(
            Texture2D baseColor,
            Texture2D normal,
            Texture2D metallic,
            Texture2D roughness,
            Texture2D metallicSmoothness)
        {
            SetTextureImporter(baseColor, TextureImporterType.Default, true, false);
            SetTextureImporter(normal, TextureImporterType.NormalMap, false, false);
            SetTextureImporter(metallic, TextureImporterType.Default, false, false);
            SetTextureImporter(roughness, TextureImporterType.Default, false, false);
            SetTextureImporter(
                metallicSmoothness,
                TextureImporterType.Default,
                false,
                false);
        }

        private static void SetReadable(Texture2D texture, bool value)
        {
            TextureImporter importer = AssetImporter.GetAtPath(
                AssetDatabase.GetAssetPath(texture)) as TextureImporter ??
                throw new InvalidOperationException(
                    "Lightsaber TextureImporter is missing: " + texture.name);
            if (importer.isReadable == value) return;
            importer.isReadable = value;
            importer.SaveAndReimport();
        }

        private static void SetTextureImporter(
            Texture2D texture,
            TextureImporterType type,
            bool srgb,
            bool readable)
        {
            TextureImporter importer = AssetImporter.GetAtPath(
                AssetDatabase.GetAssetPath(texture)) as TextureImporter ??
                throw new InvalidOperationException(
                    "Lightsaber TextureImporter is missing: " + texture.name);
            importer.textureType = type;
            importer.sRGBTexture = srgb;
            importer.isReadable = readable;
            importer.SaveAndReimport();
        }

        private static void ConfigureUrpMaterial(
            Material material,
            Shader shader,
            Texture2D baseColor,
            Texture2D normal,
            Texture2D metallicSmoothness)
        {
            material.shader = shader;
            material.SetTexture("_BaseMap", baseColor);
            material.SetTexture("_MainTex", baseColor);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_Color", Color.white);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", 1f);
            material.SetTexture("_MetallicGlossMap", metallicSmoothness);
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_Smoothness", 1f);
            material.SetFloat("_SmoothnessTextureChannel", 0f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material);
        }

        private static Bounds CombinedBounds(IEnumerable<Bounds> source)
        {
            Bounds[] values = source.ToArray();
            if (values.Length == 0)
                throw new InvalidOperationException("No geometry bounds were supplied.");
            Bounds result = values[0];
            foreach (Bounds value in values.Skip(1)) result.Encapsulate(value);
            return result;
        }

        private static int LongestAxis(Vector3 size)
        {
            if (size.x >= size.y && size.x >= size.z) return 0;
            return size.y >= size.z ? 1 : 2;
        }

        private static Vector3 AxisVector(int axis)
        {
            return axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
        }

        private static float Component(Vector3 value, int axis)
        {
            return axis == 0 ? value.x : axis == 1 ? value.y : value.z;
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

        private static string SanitizeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return string.IsNullOrWhiteSpace(value) ? "Lightsaber" : value;
        }

        private static string Absolute(string projectRelativePath)
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Project root is unavailable.");
            return Path.GetFullPath(Path.Combine(
                root,
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string Sha256File(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " changed unexpectedly.");
        }

        private static void RequireAngle(float expected, float actual, string label)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(expected, actual)) > AngleToleranceDegrees)
                throw new InvalidOperationException(
                    label + " expected=" + Num(expected) + ", actual=" + Num(actual) + ".");
        }

        private static int ConsoleErrorCount()
        {
            Type type = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll") ??
                throw new InvalidOperationException("Unity console API is unavailable.");
            var method = type.GetMethod(
                "GetCountsByType",
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic) ??
                throw new InvalidOperationException("Unity console count API is unavailable.");
            object[] arguments = { 0, 0, 0 };
            method.Invoke(null, arguments);
            return (int)arguments[0];
        }

        private static void RequireNoNewUnityConsoleErrors(int before)
        {
            int after = ConsoleErrorCount();
            if (after > before)
                throw new InvalidOperationException(
                    "Unity console gained " + (after - before) + " new error(s).");
        }

        private static string Num(float value)
        {
            return value.ToString("F6", CultureInfo.InvariantCulture);
        }

        private static string Precise(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Precise(Vector3 value)
        {
            return Precise(value.x) + "," + Precise(value.y) + "," + Precise(value.z);
        }

        private static string Precise(Quaternion value)
        {
            return Precise(value.x) + "," + Precise(value.y) + "," +
                Precise(value.z) + "," + Precise(value.w);
        }

        private static string Vec(Vector3 value)
        {
            return Num(value.x) + "," + Num(value.y) + "," + Num(value.z);
        }

        private readonly struct ModelGeometry
        {
            internal ModelGeometry(
                Vector3 bladeAxisLocal,
                Vector3 handleCenterLocal,
                float handleLengthLocal)
            {
                BladeAxisLocal = bladeAxisLocal;
                HandleCenterLocal = handleCenterLocal;
                HandleLengthLocal = handleLengthLocal;
            }

            internal Vector3 BladeAxisLocal { get; }
            internal Vector3 HandleCenterLocal { get; }
            internal float HandleLengthLocal { get; }
        }

        private readonly struct GripMetrics
        {
            internal GripMetrics(
                float elbowBendDegrees,
                float wristDeviationDegrees,
                float handleAxisErrorDegrees,
                float palmCenterErrorMeters)
                : this(
                    elbowBendDegrees,
                    wristDeviationDegrees,
                    handleAxisErrorDegrees,
                    palmCenterErrorMeters,
                    float.NaN)
            {
            }

            internal GripMetrics(
                float elbowBendDegrees,
                float wristDeviationDegrees,
                float handleAxisErrorDegrees,
                float palmCenterErrorMeters,
                float fingerPoseErrorDegrees)
            {
                ElbowBendDegrees = elbowBendDegrees;
                WristDeviationDegrees = wristDeviationDegrees;
                HandleAxisErrorDegrees = handleAxisErrorDegrees;
                PalmCenterErrorMeters = palmCenterErrorMeters;
                FingerPoseErrorDegrees = fingerPoseErrorDegrees;
            }

            internal float ElbowBendDegrees { get; }
            internal float WristDeviationDegrees { get; }
            internal float HandleAxisErrorDegrees { get; }
            internal float PalmCenterErrorMeters { get; }
            internal float FingerPoseErrorDegrees { get; }

            internal string Describe()
            {
                return "elbowBendDegrees=" + Num(ElbowBendDegrees) +
                    "|wristDeviationDegrees=" + Num(WristDeviationDegrees) +
                    "|handleAxisErrorDegrees=" + Num(HandleAxisErrorDegrees) +
                    "|palmCenterErrorMeters=" + Num(PalmCenterErrorMeters) +
                    (float.IsNaN(FingerPoseErrorDegrees)
                        ? string.Empty
                        : "|fingerPoseErrorDegrees=" + Num(FingerPoseErrorDegrees));
            }
        }

        private readonly struct LoweredArmMetrics
        {
            internal LoweredArmMetrics(
                float rightArmRootMirrorErrorMeters,
                float rightHandMirrorErrorMeters,
                float handHeightDifferenceMeters,
                float upperDirectionMirrorErrorDegrees,
                float foreArmDirectionMirrorErrorDegrees,
                float elbowDifferenceDegrees,
                float wristDeviationDegrees,
                float handleAxisErrorDegrees,
                float palmCenterErrorMeters,
                float fingerPoseErrorDegrees)
            {
                RightArmRootMirrorErrorMeters = rightArmRootMirrorErrorMeters;
                RightHandMirrorErrorMeters = rightHandMirrorErrorMeters;
                HandHeightDifferenceMeters = handHeightDifferenceMeters;
                UpperDirectionMirrorErrorDegrees = upperDirectionMirrorErrorDegrees;
                ForeArmDirectionMirrorErrorDegrees = foreArmDirectionMirrorErrorDegrees;
                ElbowDifferenceDegrees = elbowDifferenceDegrees;
                WristDeviationDegrees = wristDeviationDegrees;
                HandleAxisErrorDegrees = handleAxisErrorDegrees;
                PalmCenterErrorMeters = palmCenterErrorMeters;
                FingerPoseErrorDegrees = fingerPoseErrorDegrees;
            }

            internal float RightArmRootMirrorErrorMeters { get; }
            internal float RightHandMirrorErrorMeters { get; }
            internal float HandHeightDifferenceMeters { get; }
            internal float UpperDirectionMirrorErrorDegrees { get; }
            internal float ForeArmDirectionMirrorErrorDegrees { get; }
            internal float ElbowDifferenceDegrees { get; }
            internal float WristDeviationDegrees { get; }
            internal float HandleAxisErrorDegrees { get; }
            internal float PalmCenterErrorMeters { get; }
            internal float FingerPoseErrorDegrees { get; }

            internal LoweredArmMetrics WithFingerPoseError(float value)
            {
                return new LoweredArmMetrics(
                    RightArmRootMirrorErrorMeters,
                    RightHandMirrorErrorMeters,
                    HandHeightDifferenceMeters,
                    UpperDirectionMirrorErrorDegrees,
                    ForeArmDirectionMirrorErrorDegrees,
                    ElbowDifferenceDegrees,
                    WristDeviationDegrees,
                    HandleAxisErrorDegrees,
                    PalmCenterErrorMeters,
                    value);
            }

            internal string Describe()
            {
                return "rightArmRootMirrorErrorMeters=" +
                    Num(RightArmRootMirrorErrorMeters) +
                    "|rightHandMirrorErrorMeters=" + Num(RightHandMirrorErrorMeters) +
                    "|handHeightDifferenceMeters=" + Num(HandHeightDifferenceMeters) +
                    "|upperDirectionMirrorErrorDegrees=" +
                    Num(UpperDirectionMirrorErrorDegrees) +
                    "|foreArmDirectionMirrorErrorDegrees=" +
                    Num(ForeArmDirectionMirrorErrorDegrees) +
                    "|elbowDifferenceDegrees=" + Num(ElbowDifferenceDegrees) +
                    "|wristDeviationDegrees=" + Num(WristDeviationDegrees) +
                    "|handleAxisErrorDegrees=" + Num(HandleAxisErrorDegrees) +
                    "|palmCenterErrorMeters=" + Num(PalmCenterErrorMeters) +
                    (float.IsNaN(FingerPoseErrorDegrees)
                        ? string.Empty
                        : "|fingerPoseErrorDegrees=" + Num(FingerPoseErrorDegrees));
            }
        }

        private readonly struct SubmeshGeometry
        {
            internal SubmeshGeometry(Bounds bounds, bool isBlade)
            {
                Bounds = bounds;
                IsBlade = isBlade;
            }

            internal Bounds Bounds { get; }
            internal bool IsBlade { get; }
        }

        private sealed class TransformSnapshot
        {
            private readonly Transform transform;
            private readonly Transform parent;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;
            private readonly bool activeSelf;

            internal TransformSnapshot(Transform transform)
            {
                this.transform = transform;
                parent = transform.parent;
                localPosition = transform.localPosition;
                localRotation = transform.localRotation;
                localScale = transform.localScale;
                activeSelf = transform.gameObject.activeSelf;
            }

            internal void RequireUnchanged()
            {
                if (transform == null || transform.parent != parent ||
                    Vector3.Distance(transform.localPosition, localPosition) > 0.000001f ||
                    Quaternion.Angle(transform.localRotation, localRotation) > 0.0001f ||
                    Vector3.Distance(transform.localScale, localScale) > 0.000001f ||
                    transform.gameObject.activeSelf != activeSelf)
                    throw new InvalidOperationException(
                        "Existing scene transform changed unexpectedly while placing lightsabers.");
            }
        }

        private sealed class NonRotationTransformSnapshot
        {
            private readonly Transform transform;
            private readonly Transform parent;
            private readonly Vector3 localPosition;
            private readonly Vector3 localScale;
            private readonly bool activeSelf;

            internal NonRotationTransformSnapshot(Transform transform)
            {
                this.transform = transform;
                parent = transform.parent;
                localPosition = transform.localPosition;
                localScale = transform.localScale;
                activeSelf = transform.gameObject.activeSelf;
            }

            internal void RequireUnchanged()
            {
                if (transform == null || transform.parent != parent ||
                    Vector3.Distance(transform.localPosition, localPosition) > 0.000001f ||
                    Vector3.Distance(transform.localScale, localScale) > 0.000001f ||
                    transform.gameObject.activeSelf != activeSelf)
                    throw new InvalidOperationException(
                        "Lightsaber grip changed a protected parent, position, scale, or active state.");
            }
        }

        private sealed class RotationSnapshot
        {
            private readonly Transform transform;
            private readonly Quaternion localRotation;

            internal RotationSnapshot(Transform transform)
            {
                this.transform = transform;
                localRotation = transform.localRotation;
            }

            internal void RequireUnchanged()
            {
                if (transform == null ||
                    Quaternion.Angle(transform.localRotation, localRotation) > 0.0001f)
                    throw new InvalidOperationException(
                        "Lightsaber grip changed a protected rotation under the right arm.");
            }
        }
    }
}
