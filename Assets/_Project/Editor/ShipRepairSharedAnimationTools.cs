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

namespace Bellerophon.Editor.Validation
{
    internal static class ShipRepairSharedAnimationTools
    {
        internal const string FinalCommand =
            "CaptureShipRepairSharedAnimationCorrectionFinal";
        internal const string ShipTargetName = "ShipRepair";
        internal const string SabotageTargetName = "SabotageRepair";
        internal const string TempFolder = "Temp/ShipRepairSharedAnimation";
        internal const string ValidationFolder =
            "docs/validation/ShipRepairSharedAnimationCorrection";
        internal const string FinalImageRelativePath = ValidationFolder + "/Final.png";
        internal const string FinalReportRelativePath = ValidationFolder + "/Final.txt";

        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string ExternalSourceRelativePath =
            "player model/transfer repairing.fbx";
        private const string ExpectedSourceSha256 =
            "FBDF21C7ED8581D76C3710FACCD5375939B02D1EFA1E5AB6BFFC94FFB70F72D0";
        private const string IdleClipPath =
            "Assets/_Project/Art/Player/Animations/Player_Idle.anim";
        private const string PlayerModelPath =
            "Assets/_Project/Art/Player/player.fbx";
        private const string PreApplicationSceneSha256 =
            "45FCA6FA988674EF7CEE52553E7E29A9D248F26C1D73D3DE39F5AC4F7BE3D9FB";
        private const string AssetFolder =
            "Assets/_Project/Animations/RepairingShared";
        private const string SourceFolder = AssetFolder + "/Source";
        private const string SourceAssetPath = SourceFolder + "/TransferRepairing.fbx";
        private const string PlayerProxyAssetPath =
            SourceFolder + "/PlayerHumanoidProxy.fbx";
        private const string OldBadClipPath =
            AssetFolder + "/RepairingShared_UpperSource_LowerPlayerIdle.anim";
        private const string ClipPath =
            AssetFolder + "/RepairingShared_HumanoidRetargetedUpper.anim";
        private const string MaskPath =
            AssetFolder + "/RepairingShared_UpperBody.mask";
        private const string ControllerPath =
            AssetFolder + "/RepairingShared.controller";
        private const string HipsPath = "Armature/Hips";
        private const string LeftUpLegPath = HipsPath + "/LeftUpLeg";
        private const string RightUpLegPath = HipsPath + "/RightUpLeg";
        private const float PositionTolerance = 0.00001f;
        private const float RotationTolerance = 0.01f;
        private const float ScaleTolerance = 0.00001f;

        internal static string FinalAbsolutePath => Absolute(FinalImageRelativePath);

        [MenuItem("Bellerophon/Player/Inspect Ship Repair Shared Animation Correction Sources")]
        internal static void InspectSources()
        {
            RequireEditMode();
            int consoleErrorsBefore = LightsaberSetupTools.ConsoleErrorCount();
            Scene scene = RequireScene();
            GameObject ship = FindUnique(scene, ShipTargetName);
            GameObject sabotage = FindUnique(scene, SabotageTargetName);
            Animator shipAnimator = RequireAnimator(ship);
            Animator sabotageAnimator = RequireAnimator(sabotage);
            RequireAsset<AnimationClip>(IdleClipPath);
            string external = Absolute(ExternalSourceRelativePath);
            if (!File.Exists(external))
                throw new FileNotFoundException("Repairing FBX is missing.", external);
            RequireEqual(ExpectedSourceSha256, Sha256File(external),
                "supplied repairing FBX");
            if (shipAnimator.avatar == null || sabotageAnimator.avatar == null ||
                shipAnimator.avatar != sabotageAnimator.avatar)
                throw new InvalidOperationException(
                    "ShipRepair and SabotageRepair must share the same Avatar.");
            RequireLowerBodyHierarchy(ship.transform);
            RequireLowerBodyHierarchy(sabotage.transform);
            string playerHash = Sha256File(Absolute(PlayerModelPath));

            var report = new StringBuilder()
                .AppendLine("ShipRepair/SabotageRepair correction source inspection")
                .AppendLine("verificationTargetsManipulated=False")
                .AppendLine("source=" + ExternalSourceRelativePath)
                .AppendLine("sourceSha256=" + ExpectedSourceSha256)
                .AppendLine("playerProxySource=" + PlayerModelPath)
                .AppendLine("playerProxySourceSha256=" + playerHash)
                .AppendLine("idleClip=" + IdleClipPath)
                .AppendLine("idleFirstFrameSeconds=0")
                .AppendLine("correctionMethod=Humanoid retarget to player proxy, then Generic upper-body bake")
                .AppendLine("shipAvatar=" + AssetDatabase.GetAssetPath(shipAnimator.avatar))
                .AppendLine("sabotageAvatar=" + AssetDatabase.GetAssetPath(sabotageAnimator.avatar))
                .AppendLine("sharedAvatar=True");
            Write(TempFolder + "/CorrectionSourceInspection.txt", report.ToString());
            LightsaberSetupTools.RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log("[RepairingSharedCorrection] Sources inspected read-only.\n" + report);
        }

        [MenuItem("Bellerophon/Player/Apply Ship Repair Shared Animation Correction")]
        internal static void Apply()
        {
            RequireEditMode();
            int consoleErrorsBefore = LightsaberSetupTools.ConsoleErrorCount();
            Scene scene = RequireScene();
            RecoverKnownTransientInspectionDirtiness(scene);
            if (scene.isDirty)
                throw new InvalidOperationException(
                    "Repairing correction requires a clean CargoRunMvp scene.");
            GameObject ship = FindUnique(scene, ShipTargetName);
            GameObject sabotage = FindUnique(scene, SabotageTargetName);
            Animator shipAnimator = RequireAnimator(ship);
            Animator sabotageAnimator = RequireAnimator(sabotage);
            if (shipAnimator.avatar == null || shipAnimator.avatar != sabotageAnimator.avatar)
                throw new InvalidOperationException(
                    "Repair targets do not share the same non-null Avatar.");
            AnimationClip idle = RequireAsset<AnimationClip>(IdleClipPath);
            var shipRoot = new TransformSnapshot(ship.transform);
            var sabotageRoot = new TransformSnapshot(sabotage.transform);
            string shipRenderer = RendererSignature(ship.transform);
            string sabotageRenderer = RendererSignature(sabotage.transform);
            string shipAvatar = ObjectIdentity(shipAnimator.avatar);
            string sabotageAvatar = ObjectIdentity(sabotageAnimator.avatar);
            string outsideTransforms = OutsideTransformSignature(scene, ship, sabotage);

            EnsureFolder(AssetFolder);
            EnsureFolder(SourceFolder);
            CopySourceExactly();
            CopyPlayerProxyExactly();
            ConfigureHumanoidImporter(SourceAssetPath, true);
            ConfigureHumanoidImporter(PlayerProxyAssetPath, false);
            AnimationClip source = RequireSingleEmbeddedClip();
            RequireHumanAvatar(SourceAssetPath, "repairing source");
            RequireHumanAvatar(PlayerProxyAssetPath, "player Humanoid proxy");
            AnimationClip shared = CreateHumanoidRetargetedUpperClip(source);
            AvatarMask mask = CreateUpperBodyMask();
            AnimatorController controller = CreateLayeredController(idle, shared, mask);
            ConnectAnimator(shipAnimator, controller, ShipTargetName);
            ConnectAnimator(sabotageAnimator, controller, SabotageTargetName);
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(OldBadClipPath) != null &&
                !AssetDatabase.DeleteAsset(OldBadClipPath))
                throw new InvalidOperationException(
                    "The invalid Generic local-Transform clip could not be removed.");
            AssetDatabase.SaveAssets();

            shipRoot.RequireUnchanged(ShipTargetName);
            sabotageRoot.RequireUnchanged(SabotageTargetName);
            RequireEqual(shipRenderer, RendererSignature(ship.transform),
                "ShipRepair renderers");
            RequireEqual(sabotageRenderer, RendererSignature(sabotage.transform),
                "SabotageRepair renderers");
            RequireEqual(shipAvatar, ObjectIdentity(shipAnimator.avatar),
                "ShipRepair Avatar");
            RequireEqual(sabotageAvatar, ObjectIdentity(sabotageAnimator.avatar),
                "SabotageRepair Avatar");
            RequireEqual(outsideTransforms,
                OutsideTransformSignature(scene, ship, sabotage),
                "scene transforms outside the repair targets");
            RequireEqual(ExpectedSourceSha256,
                Sha256File(Absolute(SourceAssetPath)),
                "imported repairing FBX binary copy");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            AssetDatabase.SaveAssets();
            UpperMotionMetrics metrics = MeasureUpperMotion(shared);
            Write(TempFolder + "/CorrectionApplication.txt", new StringBuilder()
                .AppendLine("ShipRepair/SabotageRepair shared animation correction")
                .AppendLine("sceneSaved=True")
                .AppendLine("sourceBinaryCopiedExactly=True")
                .AppendLine("sourceSha256=" + ExpectedSourceSha256)
                .AppendLine("sourceClip=" + SourceAssetPath)
                .AppendLine("sourceDurationSeconds=" + Num(source.length))
                .AppendLine("sourceFrameRate=" + Num(source.frameRate))
                .AppendLine("humanoidRetargetedUpperClip=" + ClipPath)
                .AppendLine("upperBodyMask=" + MaskPath)
                .AppendLine("sharedController=" + ControllerPath)
                .AppendLine("invalidGenericAbsoluteLocalCurveCopyRemoved=True")
                .AppendLine("sourceImportType=Humanoid")
                .AppendLine("retargetTarget=Humanoid player proxy")
                .AppendLine("runtimeTargetAvatarPreserved=True")
                .AppendLine("controllerLayers=PlayerIdle_Frame0_Base,Repairing_UpperBody")
                .AppendLine("lowerBodyPose=Player_Idle frame 0 via frozen base layer")
                .AppendLine(metrics.Describe())
                .AppendLine("loopTime=True")
                .AppendLine("applyRootMotion=False")
                .AppendLine("targets=ShipRepair,SabotageRepair")
                .AppendLine("targetRootTransformsChanged=False")
                .AppendLine("targetRenderersChanged=False")
                .AppendLine("targetAvatarsChanged=False")
                .AppendLine("otherSceneTransformsChanged=False")
                .ToString());
            LightsaberSetupTools.RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log("[RepairingSharedCorrection] Humanoid-retargeted upper-body loop applied.");
        }

        [MenuItem("Bellerophon/Player/Inspect Ship Repair Shared Animation Correction")]
        internal static void InspectAnimation()
        {
            RequireEditMode();
            int consoleErrorsBefore = LightsaberSetupTools.ConsoleErrorCount();
            Scene scene = RequireScene();
            GameObject ship = FindUnique(scene, ShipTargetName);
            GameObject sabotage = FindUnique(scene, SabotageTargetName);
            AnimationClip shared = RequireAsset<AnimationClip>(ClipPath);
            AnimationClip idle = RequireAsset<AnimationClip>(IdleClipPath);
            AvatarMask mask = RequireAsset<AvatarMask>(MaskPath);
            AnimatorController controller = RequireAsset<AnimatorController>(ControllerPath);

            RequireEqual(ExpectedSourceSha256,
                Sha256File(Absolute(ExternalSourceRelativePath)),
                "supplied repairing FBX");
            RequireEqual(ExpectedSourceSha256,
                Sha256File(Absolute(SourceAssetPath)),
                "imported repairing FBX binary copy");
            RequireEqual(Sha256File(Absolute(PlayerModelPath)),
                Sha256File(Absolute(PlayerProxyAssetPath)),
                "Humanoid player proxy binary copy");
            AnimationClip source = RequireSingleEmbeddedClip();
            if (Mathf.Abs(source.length - shared.length) > 0.0001f ||
                Mathf.Abs(source.frameRate - shared.frameRate) > 0.0001f)
                throw new InvalidOperationException("Retargeted clip timing differs from source.");
            RequireHumanAvatar(SourceAssetPath, "repairing source");
            RequireHumanAvatar(PlayerProxyAssetPath, "player Humanoid proxy");
            RequireUpperOnlyClip(shared);
            RequireLayeredController(controller, idle, shared, mask);
            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(shared);
            if (!settings.loopTime)
                throw new InvalidOperationException("Shared repairing clip is not looping.");
            RequireAnimatorConnection(ship, controller);
            RequireAnimatorConnection(sabotage, controller);
            UpperMotionMetrics metrics = MeasureUpperMotion(shared);
            if (metrics.LeftHandTravel < 0.03f || metrics.RightHandTravel < 0.03f)
                throw new InvalidOperationException(
                    "Retargeted typing motion has insufficient bilateral hand travel. " +
                    metrics.Describe());

            var report = new StringBuilder()
                .AppendLine("ShipRepair/SabotageRepair correction inspection")
                .AppendLine("verificationTargetsManipulated=False")
                .AppendLine("sourceBinaryCopiedExactly=True")
                .AppendLine("sourceAndProxyImportType=Humanoid")
                .AppendLine("retargetedUpperBodyOnly=True")
                .AppendLine("sourceDurationSeconds=" + Num(source.length))
                .AppendLine("retargetedDurationSeconds=" + Num(shared.length))
                .AppendLine("sourceFrameRate=" + Num(source.frameRate))
                .AppendLine("retargetedFrameRate=" + Num(shared.frameRate))
                .AppendLine("lowerBodyPose=Player_Idle frame 0")
                .AppendLine("lowerBodyHeldByFrozenBaseLayer=True")
                .AppendLine("upperBodyOverrideMask=True")
                .AppendLine(metrics.Describe())
                .AppendLine("loopTime=True")
                .AppendLine("sharedTwoLayerController=True")
                .AppendLine("applyRootMotion=False")
                .AppendLine("directVisualReviewPending=True");
            Write(TempFolder + "/CorrectionInspection.txt", report.ToString());
            LightsaberSetupTools.RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log("[RepairingSharedCorrection] Structural inspection passed.\n" + report);
        }

        internal static void RequireRuntimeSetup(
            out GameObject ship,
            out GameObject sabotage,
            out AnimationClip shared)
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException(
                    "Natural repairing playback review requires Play Mode.");
            Scene scene = RequireScene();
            ship = FindUnique(scene, ShipTargetName);
            sabotage = FindUnique(scene, SabotageTargetName);
            shared = RequireAsset<AnimationClip>(ClipPath);
            RuntimeAnimatorController controller =
                RequireAsset<RuntimeAnimatorController>(ControllerPath);
            RequireAnimatorConnection(ship, controller);
            RequireAnimatorConnection(sabotage, controller);
        }

        internal static Animator RequireAnimator(GameObject target)
        {
            return target.GetComponent<Animator>() ??
                throw new InvalidOperationException(target.name + " Animator is missing.");
        }

        internal static Bounds BoundsOf(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled && item.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(root.name + " has no visible renderer.");
            Bounds result = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
                result.Encapsulate(renderers[index].bounds);
            return result;
        }

        internal static Texture2D RenderNaturalTarget(GameObject target)
        {
            const int width = 500;
            const int height = 620;
            Bounds bounds = BoundsOf(target.transform);
            GameObject cameraObject = new GameObject(
                "RepairingShared_ReviewCamera", typeof(Camera));
            GameObject lightObject = new GameObject(
                "RepairingShared_ReviewLight", typeof(Light));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = cameraObject.GetComponent<Camera>();
            Light light = lightObject.GetComponent<Light>();
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            try
            {
                Vector3 front = target.transform.forward.normalized;
                float distance = Mathf.Max(3f, bounds.extents.magnitude * 3f);
                camera.transform.position = bounds.center + front * distance;
                camera.transform.rotation = Quaternion.LookRotation(
                    bounds.center - camera.transform.position, Vector3.up);
                camera.orthographic = true;
                float aspect = width / (float)height;
                camera.orthographicSize = Mathf.Max(
                    bounds.extents.y * 1.50f,
                    bounds.extents.x / aspect * 1.50f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = distance + bounds.extents.magnitude * 4f;
                camera.allowHDR = false;
                camera.allowMSAA = true;

                light.type = LightType.Directional;
                light.intensity = 1.1f;
                light.color = Color.white;
                light.transform.rotation = Quaternion.Euler(35f, 150f, 0f);

                renderTexture = RenderTexture.GetTemporary(
                    width, height, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                RenderTexture previous = RenderTexture.active;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply(false, false);
                RenderTexture.active = previous;
                camera.targetTexture = null;
                return texture;
            }
            catch
            {
                if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
                throw;
            }
            finally
            {
                if (renderTexture != null) RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        internal static Texture2D CombinePanels(
            IReadOnlyList<Texture2D> ship,
            IReadOnlyList<Texture2D> sabotage)
        {
            if (ship.Count != 4 || sabotage.Count != 4)
                throw new InvalidOperationException(
                    "Repairing final review requires four phases per target.");
            const int gap = 8;
            int panelWidth = ship[0].width;
            int panelHeight = ship[0].height;
            var sheet = new Texture2D(
                panelWidth * 4 + gap * 3,
                panelHeight * 2 + gap,
                TextureFormat.RGBA32,
                false);
            Color32[] black = Enumerable.Repeat(
                (Color32)Color.black, sheet.width * sheet.height).ToArray();
            sheet.SetPixels32(black);
            for (int index = 0; index < 4; index++)
            {
                sheet.SetPixels(
                    index * (panelWidth + gap), panelHeight + gap,
                    panelWidth, panelHeight, ship[index].GetPixels());
                sheet.SetPixels(
                    index * (panelWidth + gap), 0,
                    panelWidth, panelHeight, sabotage[index].GetPixels());
            }
            sheet.Apply(false, false);
            return sheet;
        }

        internal static void WriteFinalEvidence(
            Texture2D sheet,
            float duration,
            float maximumNormalizedDifference,
            int consoleErrorsBefore)
        {
            string image = FinalAbsolutePath;
            string report = Absolute(FinalReportRelativePath);
            if (File.Exists(image) || File.Exists(report))
                throw new InvalidOperationException(
                    "Repairing shared one-time final evidence already exists.");
            int consoleErrorsAfter = LightsaberSetupTools.ConsoleErrorCount();
            if (consoleErrorsAfter > consoleErrorsBefore)
                throw new InvalidOperationException(
                    "Unity console gained errors during natural playback review.");
            Directory.CreateDirectory(Path.GetDirectoryName(image) ??
                throw new InvalidOperationException(
                    "Repairing validation output folder is unavailable."));
            File.WriteAllBytes(image, sheet.EncodeToPNG());
            File.WriteAllText(report, new StringBuilder()
                .AppendLine("ShipRepair/SabotageRepair corrected natural Play Mode final review")
                .AppendLine("naturalPlayMode=True")
                .AppendLine("verificationTargetsManipulated=False")
                .AppendLine("targets=ShipRepair,SabotageRepair")
                .AppendLine("panelRows=ShipRepair,SabotageRepair")
                .AppendLine("normalizedPhases=0.10,0.35,0.60,0.85")
                .AppendLine("sourceDurationSeconds=" + Num(duration))
                .AppendLine("bothTargetsCompletedAtLeastOneLoop=True")
                .AppendLine("humanoidRetargetedUpperBodyPlayback=True")
                .AppendLine("playerIdleFrame0LowerBodyLayer=True")
                .AppendLine("maximumNormalizedTimeDifference=" +
                    Num(maximumNormalizedDifference))
                .AppendLine("targetRootTransformsChanged=False")
                .AppendLine("unityConsoleNewErrors=0")
                .AppendLine("directVisualReviewPrimary=True")
                .AppendLine("numericMetricsSecondary=True")
                .ToString(), new UTF8Encoding(false));
        }

        private static void CopyPlayerProxyExactly()
        {
            string source = Absolute(PlayerModelPath);
            string destination = Absolute(PlayerProxyAssetPath);
            if (!File.Exists(source))
                throw new FileNotFoundException("Player FBX is missing.", source);
            string expected = Sha256File(source);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Player proxy destination is unavailable."));
            if (!File.Exists(destination) ||
                !string.Equals(Sha256File(destination), expected,
                    StringComparison.OrdinalIgnoreCase))
                File.Copy(source, destination, true);
            AssetDatabase.ImportAsset(
                PlayerProxyAssetPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            RequireEqual(expected, Sha256File(destination),
                "Humanoid player proxy binary copy");
        }

        private static void ConfigureHumanoidImporter(
            string assetPath,
            bool importAnimation)
        {
            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as
                ModelImporter ?? throw new InvalidOperationException(
                    "Humanoid ModelImporter is unavailable: " + assetPath);
            importer.importAnimation = importAnimation;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.resampleCurves = true;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            if (importAnimation)
            {
                ModelImporterClipAnimation[] clips = importer.clipAnimations;
                if (clips == null || clips.Length == 0)
                    clips = importer.defaultClipAnimations;
                if (clips == null || clips.Length != 1)
                    throw new InvalidOperationException(
                        "Repairing FBX must expose exactly one embedded animation; actual=" +
                        (clips == null ? "0" : clips.Length.ToString(
                            CultureInfo.InvariantCulture)) + ".");
                clips[0].loopTime = true;
                clips[0].loopPose = false;
                clips[0].keepOriginalPositionY = true;
                importer.clipAnimations = clips;
            }
            importer.SaveAndReimport();
        }

        private static Avatar RequireHumanAvatar(string assetPath, string label)
        {
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<Avatar>()
                .FirstOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                throw new InvalidOperationException(
                    label + " did not import as a valid Humanoid Avatar.");
            return avatar;
        }

        private static AnimationClip CreateHumanoidRetargetedUpperClip(
            AnimationClip source)
        {
            if (!source.humanMotion)
                throw new InvalidOperationException(
                    "Repairing source clip is not Humanoid motion.");
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ClipPath) != null &&
                !AssetDatabase.DeleteAsset(ClipPath))
                throw new InvalidOperationException(
                    "Existing retargeted upper-body clip could not be replaced.");

            Scene previewScene = EditorSceneManager.NewPreviewScene();
            GameObject proxyModel = RequireAsset<GameObject>(PlayerProxyAssetPath);
            GameObject probe = PrefabUtility.InstantiatePrefab(
                proxyModel, previewScene) as GameObject ??
                throw new InvalidOperationException(
                    "Humanoid player proxy could not be instantiated.");
            probe.name = "RepairingShared_HumanoidRetargetProbe";
            probe.hideFlags = HideFlags.HideAndDontSave;
            Animator animator = RequireAnimator(probe);
            Avatar proxyAvatar = RequireHumanAvatar(
                PlayerProxyAssetPath, "player Humanoid proxy");
            animator.avatar = proxyAvatar;
            animator.applyRootMotion = false;
            animator.enabled = true;

            RotationTrack[] tracks = probe.GetComponentsInChildren<Transform>(true)
                .Select(item => new
                {
                    Path = RelativePath(probe.transform, item),
                    Transform = item
                })
                .Where(item => IsUpperBodyPath(item.Path))
                .OrderBy(item => item.Path, StringComparer.Ordinal)
                .Select(item => new RotationTrack(item.Path, item.Transform))
                .ToArray();
            if (tracks.Length < 8)
                throw new InvalidOperationException(
                    "Humanoid player proxy upper-body hierarchy is incomplete.");

            bool startedAnimationMode = false;
            try
            {
                if (AnimationMode.InAnimationMode())
                    throw new InvalidOperationException(
                        "Close the active Animation preview before applying repair correction.");
                AnimationMode.StartAnimationMode();
                startedAnimationMode = true;
                int frameCount = Mathf.CeilToInt(source.length * source.frameRate);
                for (int frame = 0; frame <= frameCount; frame++)
                {
                    float time = Mathf.Min(frame / source.frameRate, source.length);
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(probe, source, time);
                    AnimationMode.EndSampling();
                    foreach (RotationTrack track in tracks)
                        track.Add(time);
                }
            }
            finally
            {
                if (startedAnimationMode) AnimationMode.StopAnimationMode();
                UnityEngine.Object.DestroyImmediate(probe);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }

            var output = new AnimationClip
            {
                name = "RepairingShared_HumanoidRetargetedUpper",
                frameRate = source.frameRate,
                wrapMode = WrapMode.Loop,
                legacy = false
            };
            foreach (RotationTrack track in tracks)
                track.Apply(output);
            output.EnsureQuaternionContinuity();
            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(source);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.startTime = 0f;
            settings.stopTime = source.length;
            AnimationUtility.SetAnimationClipSettings(output, settings);
            AssetDatabase.CreateAsset(output, ClipPath);
            AssetDatabase.ImportAsset(
                ClipPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            AnimationClip saved = RequireAsset<AnimationClip>(ClipPath);
            RequireUpperOnlyClip(saved);
            return saved;
        }

        private static AvatarMask CreateUpperBodyMask()
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MaskPath) != null &&
                !AssetDatabase.DeleteAsset(MaskPath))
                throw new InvalidOperationException(
                    "Existing repairing upper-body mask could not be replaced.");
            Scene previewScene = EditorSceneManager.NewPreviewScene();
            GameObject model = RequireAsset<GameObject>(PlayerModelPath);
            GameObject probe = PrefabUtility.InstantiatePrefab(
                model, previewScene) as GameObject ??
                throw new InvalidOperationException(
                    "Player mask probe could not be instantiated.");
            probe.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var mask = new AvatarMask { name = "RepairingShared_UpperBody" };
                for (AvatarMaskBodyPart part = AvatarMaskBodyPart.Root;
                    part < AvatarMaskBodyPart.LastBodyPart;
                    part++)
                    mask.SetHumanoidBodyPartActive(part, false);
                mask.AddTransformPath(probe.transform, true);
                for (int index = 0; index < mask.transformCount; index++)
                    mask.SetTransformActive(
                        index,
                        IsUpperBodyPath(mask.GetTransformPath(index)));
                AssetDatabase.CreateAsset(mask, MaskPath);
                AssetDatabase.SaveAssets();
                return RequireAsset<AvatarMask>(MaskPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(probe);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static AnimatorController CreateLayeredController(
            AnimationClip idle,
            AnimationClip upper,
            AvatarMask mask)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null &&
                !AssetDatabase.DeleteAsset(ControllerPath))
                throw new InvalidOperationException(
                    "Existing shared controller could not be replaced.");
            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorControllerLayer baseLayer = controller.layers[0];
            baseLayer.name = "PlayerIdle_Frame0_Base";
            AnimatorState baseState = baseLayer.stateMachine.AddState(
                "PlayerIdle_Frame0_Hold");
            baseState.motion = idle;
            baseState.speed = 0f;
            baseState.writeDefaultValues = true;
            baseLayer.stateMachine.defaultState = baseState;
            controller.layers = new[] { baseLayer };

            controller.AddLayer("Repairing_UpperBody");
            AnimatorControllerLayer[] layers = controller.layers;
            AnimatorControllerLayer upperLayer = layers[1];
            upperLayer.defaultWeight = 1f;
            upperLayer.blendingMode = AnimatorLayerBlendingMode.Override;
            upperLayer.avatarMask = mask;
            AnimatorState upperState = upperLayer.stateMachine.AddState(
                "Repairing_HumanoidRetargeted_Upper");
            upperState.motion = upper;
            upperState.speed = 1f;
            upperState.writeDefaultValues = true;
            upperLayer.stateMachine.defaultState = upperState;
            layers[1] = upperLayer;
            controller.layers = layers;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void RequireUpperOnlyClip(AnimationClip clip)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            EditorCurveBinding[] transforms = bindings
                .Where(item => item.type == typeof(Transform))
                .ToArray();
            if (transforms.Length == 0)
                throw new InvalidOperationException(
                    "Retargeted upper-body clip has no Transform curves.");
            EditorCurveBinding invalid = transforms.FirstOrDefault(item =>
                !IsUpperBodyPath(item.path));
            if (!string.IsNullOrEmpty(invalid.path))
                throw new InvalidOperationException(
                    "Retargeted clip contains a non-upper-body curve: " + invalid.path);
        }

        private static void RequireLayeredController(
            AnimatorController controller,
            AnimationClip idle,
            AnimationClip upper,
            AvatarMask mask)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            if (layers.Length != 2)
                throw new InvalidOperationException(
                    "Repairing controller must contain exactly two layers.");
            AnimatorState baseState = layers[0].stateMachine.defaultState;
            AnimatorState upperState = layers[1].stateMachine.defaultState;
            if (baseState == null || baseState.motion != idle ||
                Mathf.Abs(baseState.speed) > 0.000001f)
                throw new InvalidOperationException(
                    "Repairing base layer is not frozen at Player_Idle frame 0.");
            if (upperState == null || upperState.motion != upper ||
                Mathf.Abs(upperState.speed - 1f) > 0.000001f ||
                layers[1].avatarMask != mask ||
                layers[1].blendingMode != AnimatorLayerBlendingMode.Override ||
                Mathf.Abs(layers[1].defaultWeight - 1f) > 0.000001f)
                throw new InvalidOperationException(
                    "Repairing upper-body layer differs from the correction design.");
            for (int index = 0; index < mask.transformCount; index++)
            {
                string path = mask.GetTransformPath(index);
                if (mask.GetTransformActive(index) != IsUpperBodyPath(path))
                    throw new InvalidOperationException(
                        "Repairing upper-body mask path differs: " + path);
            }
        }

        private static UpperMotionMetrics MeasureUpperMotion(AnimationClip clip)
        {
            Scene previewScene = EditorSceneManager.NewPreviewScene();
            GameObject model = RequireAsset<GameObject>(PlayerModelPath);
            GameObject probe = PrefabUtility.InstantiatePrefab(
                model, previewScene) as GameObject ??
                throw new InvalidOperationException(
                    "Player motion probe could not be instantiated.");
            probe.hideFlags = HideFlags.HideAndDontSave;
            Animator animator = probe.GetComponent<Animator>();
            if (animator != null) animator.enabled = false;
            try
            {
                Transform left = FindHandOrForearm(probe.transform, "Left");
                Transform right = FindHandOrForearm(probe.transform, "Right");
                Vector3 leftMin = new Vector3(float.PositiveInfinity,
                    float.PositiveInfinity, float.PositiveInfinity);
                Vector3 leftMax = new Vector3(float.NegativeInfinity,
                    float.NegativeInfinity, float.NegativeInfinity);
                Vector3 rightMin = leftMin;
                Vector3 rightMax = leftMax;
                int samples = Mathf.Max(2, Mathf.CeilToInt(clip.length * 30f));
                for (int index = 0; index <= samples; index++)
                {
                    float time = clip.length * index / samples;
                    clip.SampleAnimation(probe, time);
                    Vector3 leftPosition = probe.transform.InverseTransformPoint(left.position);
                    Vector3 rightPosition = probe.transform.InverseTransformPoint(right.position);
                    leftMin = Vector3.Min(leftMin, leftPosition);
                    leftMax = Vector3.Max(leftMax, leftPosition);
                    rightMin = Vector3.Min(rightMin, rightPosition);
                    rightMax = Vector3.Max(rightMax, rightPosition);
                }
                return new UpperMotionMetrics(
                    Vector3.Distance(leftMin, leftMax),
                    Vector3.Distance(rightMin, rightMax));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(probe);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static Transform FindHandOrForearm(Transform root, string side)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            Transform result = transforms.FirstOrDefault(item =>
                string.Equals(item.name, side + "Hand", StringComparison.Ordinal)) ??
                transforms.FirstOrDefault(item =>
                    string.Equals(item.name, side + "ForeArm", StringComparison.Ordinal));
            return result ?? throw new InvalidOperationException(
                side + " hand/forearm transform is missing.");
        }

        private static bool IsUpperBodyPath(string path)
        {
            return path.StartsWith(HipsPath + "/", StringComparison.Ordinal) &&
                !IsLowerBodyPath(path);
        }

        private static AnimationClip CreateSharedClip(
            GameObject target,
            AnimationClip source,
            AnimationClip idle)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ClipPath) != null &&
                !AssetDatabase.DeleteAsset(ClipPath))
                throw new InvalidOperationException("Existing shared clip could not be replaced.");
            Dictionary<string, LocalPose> lower = CaptureLowerBodyPose(target, idle, 0f);
            var output = new AnimationClip
            {
                name = "RepairingShared_UpperSource_LowerPlayerIdle",
                frameRate = source.frameRate,
                wrapMode = WrapMode.Loop,
                legacy = false
            };
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(source))
            {
                if (binding.type == typeof(Transform) && IsLowerBodyPath(binding.path))
                    continue;
                AnimationCurve curve = AnimationUtility.GetEditorCurve(source, binding);
                AnimationUtility.SetEditorCurve(output, binding, CloneCurve(curve));
            }
            foreach (EditorCurveBinding binding in
                AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                if (binding.type == typeof(Transform) && IsLowerBodyPath(binding.path))
                    continue;
                ObjectReferenceKeyframe[] keys =
                    AnimationUtility.GetObjectReferenceCurve(source, binding);
                AnimationUtility.SetObjectReferenceCurve(output, binding, keys);
            }
            foreach (KeyValuePair<string, LocalPose> item in lower)
                AddConstantTransformCurves(output, item.Key, item.Value, source.length);
            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(source);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.startTime = 0f;
            settings.stopTime = source.length;
            AnimationUtility.SetAnimationClipSettings(output, settings);
            AssetDatabase.CreateAsset(output, ClipPath);
            AssetDatabase.ImportAsset(
                ClipPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            AnimationClip saved = RequireAsset<AnimationClip>(ClipPath);
            RequireUpperBodyCurvesExact(source, saved);
            return saved;
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null &&
                !AssetDatabase.DeleteAsset(ControllerPath))
                throw new InvalidOperationException(
                    "Existing shared controller could not be replaced.");
            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState state = machine.AddState("RepairingShared");
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            machine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ConnectAnimator(
            Animator animator,
            RuntimeAnimatorController controller,
            string label)
        {
            Undo.RecordObject(animator, "Connect " + label + " repairing animation");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorUtility.SetDirty(animator);
        }

        private static void RequireAnimatorConnection(
            GameObject target,
            RuntimeAnimatorController expected)
        {
            Animator animator = RequireAnimator(target);
            if (animator.runtimeAnimatorController != expected ||
                animator.applyRootMotion || !animator.enabled ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                throw new InvalidOperationException(
                    target.name + " Animator shared-loop settings differ.");
        }

        private static void CopySourceExactly()
        {
            string source = Absolute(ExternalSourceRelativePath);
            string destination = Absolute(SourceAssetPath);
            if (!File.Exists(source))
                throw new FileNotFoundException("Repairing FBX is missing.", source);
            RequireEqual(ExpectedSourceSha256, Sha256File(source),
                "supplied repairing FBX");
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Source destination is unavailable."));
            if (!File.Exists(destination) ||
                !string.Equals(Sha256File(destination), ExpectedSourceSha256,
                    StringComparison.OrdinalIgnoreCase))
                File.Copy(source, destination, true);
            AssetDatabase.ImportAsset(
                SourceAssetPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            RequireEqual(ExpectedSourceSha256, Sha256File(destination),
                "imported repairing FBX binary copy");
        }

        private static void ConfigureSourceImporter()
        {
            ModelImporter importer = AssetImporter.GetAtPath(SourceAssetPath) as
                ModelImporter ?? throw new InvalidOperationException(
                    "Repairing FBX ModelImporter is unavailable.");
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.resampleCurves = false;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.SaveAndReimport();
            importer = AssetImporter.GetAtPath(SourceAssetPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "Repairing FBX importer disappeared after reimport.");
            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
                clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1)
                throw new InvalidOperationException(
                    "Repairing FBX must expose exactly one embedded animation; actual=" +
                    (clips == null ? "0" : clips.Length.ToString(
                        CultureInfo.InvariantCulture)) + ".");
            clips[0].loopTime = false;
            clips[0].loopPose = false;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip RequireSingleEmbeddedClip()
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(SourceAssetPath)
                .OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__",
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (clips.Length != 1)
                throw new InvalidOperationException(
                    "Repairing FBX must expose one usable animation clip; actual=" +
                    clips.Length.ToString(CultureInfo.InvariantCulture) + ".");
            return clips[0];
        }

        private static void RequireBindingCompatibility(
            Transform target,
            AnimationClip source)
        {
            EditorCurveBinding[] transformBindings =
                AnimationUtility.GetCurveBindings(source)
                    .Where(item => item.type == typeof(Transform))
                    .ToArray();
            if (transformBindings.Length == 0)
                throw new InvalidOperationException(
                    "Repairing source has no Transform animation curves.");
            string[] missing = transformBindings.Select(item => item.path)
                .Where(path => !string.IsNullOrEmpty(path) && target.Find(path) == null)
                .Distinct(StringComparer.Ordinal)
                .Take(12)
                .ToArray();
            if (missing.Length > 0)
                throw new InvalidOperationException(
                    "Repairing source bone paths do not match the player rig: " +
                    string.Join(", ", missing));
            if (!transformBindings.Any(item => IsLowerBodyPath(item.path)) ||
                !transformBindings.Any(item => !IsLowerBodyPath(item.path)))
                throw new InvalidOperationException(
                    "Repairing source must contain both upper- and lower-body curves.");
        }

        private static void RequireUpperBodyCurvesExact(
            AnimationClip source,
            AnimationClip output)
        {
            EditorCurveBinding[] sourceBindings = AnimationUtility
                .GetCurveBindings(source)
                .Where(item => item.type != typeof(Transform) ||
                    !IsLowerBodyPath(item.path))
                .OrderBy(BindingKey, StringComparer.Ordinal)
                .ToArray();
            EditorCurveBinding[] outputBindings = AnimationUtility
                .GetCurveBindings(output)
                .Where(item => item.type != typeof(Transform) ||
                    !IsLowerBodyPath(item.path))
                .OrderBy(BindingKey, StringComparer.Ordinal)
                .ToArray();
            if (sourceBindings.Length != outputBindings.Length)
                throw new InvalidOperationException(
                    "Upper-body curve binding count changed.");
            for (int index = 0; index < sourceBindings.Length; index++)
            {
                if (BindingKey(sourceBindings[index]) != BindingKey(outputBindings[index]))
                    throw new InvalidOperationException(
                        "Upper-body curve binding changed at index " + index + ".");
                RequireCurveEqual(
                    AnimationUtility.GetEditorCurve(source, sourceBindings[index]),
                    AnimationUtility.GetEditorCurve(output, outputBindings[index]),
                    BindingKey(sourceBindings[index]));
            }
        }

        private static void RequireCurveEqual(
            AnimationCurve expected,
            AnimationCurve actual,
            string label)
        {
            Keyframe[] a = expected.keys;
            Keyframe[] b = actual.keys;
            if (a.Length != b.Length || expected.preWrapMode != actual.preWrapMode ||
                expected.postWrapMode != actual.postWrapMode)
                throw new InvalidOperationException(label + " curve structure changed.");
            for (int index = 0; index < a.Length; index++)
            {
                if (Mathf.Abs(a[index].time - b[index].time) > 0.000001f ||
                    Mathf.Abs(a[index].value - b[index].value) > 0.000001f ||
                    Mathf.Abs(a[index].inTangent - b[index].inTangent) > 0.00001f ||
                    Mathf.Abs(a[index].outTangent - b[index].outTangent) > 0.00001f ||
                    a[index].weightedMode != b[index].weightedMode ||
                    Mathf.Abs(a[index].inWeight - b[index].inWeight) > 0.00001f ||
                    Mathf.Abs(a[index].outWeight - b[index].outWeight) > 0.00001f)
                    throw new InvalidOperationException(
                        label + " curve key changed at index " + index + ".");
            }
        }

        private static PoseDelta MeasureLowerBodyPose(
            GameObject target,
            AnimationClip shared,
            AnimationClip idle)
        {
            Dictionary<string, LocalPose> expected =
                CaptureLowerBodyPose(target, idle, 0f);
            var maximum = new PoseDelta();
            float[] times =
            {
                0f,
                shared.length * 0.125f,
                shared.length * 0.25f,
                shared.length * 0.5f,
                shared.length * 0.75f,
                shared.length * 0.875f,
                shared.length
            };
            foreach (float time in times)
            {
                Dictionary<string, LocalPose> actual =
                    CaptureLowerBodyPose(target, shared, time);
                foreach (KeyValuePair<string, LocalPose> item in expected)
                {
                    LocalPose pose = actual[item.Key];
                    maximum.Position = Mathf.Max(maximum.Position,
                        Vector3.Distance(item.Value.Position, pose.Position));
                    maximum.Rotation = Mathf.Max(maximum.Rotation,
                        Quaternion.Angle(item.Value.Rotation, pose.Rotation));
                    maximum.Scale = Mathf.Max(maximum.Scale,
                        Vector3.Distance(item.Value.Scale, pose.Scale));
                }
            }
            return maximum;
        }

        private static Dictionary<string, LocalPose> CaptureLowerBodyPose(
            GameObject target,
            AnimationClip clip,
            float time)
        {
            RequireLowerBodyHierarchy(target.transform);
            Scene previewScene = EditorSceneManager.NewPreviewScene();
            GameObject playerModel = RequireAsset<GameObject>(PlayerModelPath);
            GameObject probe = PrefabUtility.InstantiatePrefab(
                playerModel, previewScene) as GameObject ??
                throw new InvalidOperationException(
                    "Player pose probe could not be instantiated in a Preview Scene.");
            probe.name = "RepairingShared_PoseProbe";
            probe.hideFlags = HideFlags.HideAndDontSave;
            Animator animator = probe.GetComponent<Animator>();
            if (animator != null) animator.enabled = false;
            try
            {
                clip.SampleAnimation(probe, Mathf.Clamp(time, 0f, clip.length));
                return probe.GetComponentsInChildren<Transform>(true)
                    .Select(item => new
                    {
                        Path = RelativePath(probe.transform, item),
                        Transform = item
                    })
                    .Where(item => IsLowerBodyPath(item.Path))
                    .ToDictionary(
                        item => item.Path,
                        item => new LocalPose(item.Transform),
                        StringComparer.Ordinal);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(probe);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static void RecoverKnownTransientInspectionDirtiness(Scene scene)
        {
            if (!scene.isDirty) return;
            string report = Absolute(TempFolder + "/CorrectionSourceInspection.txt");
            string sceneFile = Absolute(ScenePath);
            bool recentInspection = File.Exists(report) &&
                DateTime.UtcNow - File.GetLastWriteTimeUtc(report) <
                    TimeSpan.FromMinutes(15);
            bool savedSceneIsKnown = File.Exists(sceneFile) &&
                string.Equals(
                    Sha256File(sceneFile),
                    PreApplicationSceneSha256,
                    StringComparison.OrdinalIgnoreCase);
            if (!recentInspection || !savedSceneIsKnown)
                return;
            System.Reflection.MethodInfo clearSceneDirtiness =
                typeof(EditorSceneManager).GetMethod(
                    "ClearSceneDirtiness",
                    System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic,
                    null,
                    new[] { typeof(Scene) },
                    null) ?? throw new InvalidOperationException(
                        "Unity scene-dirtiness recovery API is unavailable.");
            clearSceneDirtiness.Invoke(null, new object[] { scene });
            Debug.Log(
                "[RepairingSharedCorrection] Cleared only the transient scene dirty flag " +
                "created by the immediately preceding read-only pose probe; " +
                "the saved CargoRunMvp hash was unchanged.");
        }

        private static void AddConstantTransformCurves(
            AnimationClip clip,
            string path,
            LocalPose pose,
            float duration)
        {
            SetConstant(clip, path, "m_LocalPosition.x", pose.Position.x, duration);
            SetConstant(clip, path, "m_LocalPosition.y", pose.Position.y, duration);
            SetConstant(clip, path, "m_LocalPosition.z", pose.Position.z, duration);
            SetConstant(clip, path, "m_LocalRotation.x", pose.Rotation.x, duration);
            SetConstant(clip, path, "m_LocalRotation.y", pose.Rotation.y, duration);
            SetConstant(clip, path, "m_LocalRotation.z", pose.Rotation.z, duration);
            SetConstant(clip, path, "m_LocalRotation.w", pose.Rotation.w, duration);
            SetConstant(clip, path, "m_LocalScale.x", pose.Scale.x, duration);
            SetConstant(clip, path, "m_LocalScale.y", pose.Scale.y, duration);
            SetConstant(clip, path, "m_LocalScale.z", pose.Scale.z, duration);
        }

        private static void SetConstant(
            AnimationClip clip,
            string path,
            string property,
            float value,
            float duration)
        {
            var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), property);
            AnimationUtility.SetEditorCurve(
                clip,
                binding,
                AnimationCurve.Constant(0f, duration, value));
        }

        private static AnimationCurve CloneCurve(AnimationCurve source)
        {
            return new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }

        private static bool IsLowerBodyPath(string path)
        {
            return path == HipsPath || path == LeftUpLegPath ||
                path.StartsWith(LeftUpLegPath + "/", StringComparison.Ordinal) ||
                path == RightUpLegPath ||
                path.StartsWith(RightUpLegPath + "/", StringComparison.Ordinal);
        }

        private static void RequireLowerBodyHierarchy(Transform root)
        {
            string[] required =
            {
                HipsPath,
                LeftUpLegPath,
                LeftUpLegPath + "/LeftLeg",
                LeftUpLegPath + "/LeftLeg/LeftFoot",
                RightUpLegPath,
                RightUpLegPath + "/RightLeg",
                RightUpLegPath + "/RightLeg/RightFoot"
            };
            foreach (string path in required)
                if (root.Find(path) == null)
                    throw new InvalidOperationException(
                        root.name + " lower-body bone is missing: " + path);
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active scene. Actual=" + scene.path);
            return scene;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("This command requires Edit Mode.");
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
                    "Expected one " + name + "; actual=" + matches.Length + ".");
            return matches[0];
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path) ??
                throw new InvalidOperationException("Asset is missing: " + path);
        }

        private static void EnsureFolder(string path)
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

        private static string BindingKey(EditorCurveBinding binding)
        {
            return binding.path + "|" + binding.type.AssemblyQualifiedName + "|" +
                binding.propertyName;
        }

        private static string RelativePath(Transform root, Transform child)
        {
            if (child == root) return string.Empty;
            var parts = new Stack<string>();
            Transform current = child;
            while (current != null && current != root)
            {
                parts.Push(current.name);
                current = current.parent;
            }
            if (current != root)
                throw new InvalidOperationException(child.name + " is outside " + root.name + ".");
            return string.Join("/", parts);
        }

        private static string RendererSignature(Transform root)
        {
            return string.Join("\n", root.GetComponentsInChildren<Renderer>(true)
                .OrderBy(item => RelativePath(root, item.transform), StringComparer.Ordinal)
                .Select(item => RelativePath(root, item.transform) + "|" +
                    item.GetType().FullName + "|" + item.enabled + "|" +
                    string.Join(",", item.sharedMaterials.Select(ObjectIdentity))));
        }

        private static string OutsideTransformSignature(
            Scene scene,
            GameObject ship,
            GameObject sabotage)
        {
            return string.Join("\n", scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => !item.IsChildOf(ship.transform) &&
                    !item.IsChildOf(sabotage.transform))
                .OrderBy(item => item.GetInstanceID())
                .Select(item => item.GetInstanceID() + "|" +
                    Precise(item.localPosition) + "|" +
                    Precise(item.localRotation) + "|" +
                    Precise(item.localScale)));
        }

        private static string ObjectIdentity(UnityEngine.Object value)
        {
            if (value == null) return "<null>";
            string path = AssetDatabase.GetAssetPath(value);
            return value.GetType().FullName + "|" + value.name + "|" + path;
        }

        private static string Sha256File(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream))
                    .Replace("-", string.Empty);
        }

        private static string Absolute(string relative)
        {
            return Path.GetFullPath(Path.Combine(
                Directory.GetParent(Application.dataPath)?.FullName ??
                    throw new InvalidOperationException("Project root is unavailable."),
                relative.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static void Write(string relative, string content)
        {
            string path = Absolute(relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ??
                throw new InvalidOperationException("Output folder is unavailable."));
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(label + " changed unexpectedly.");
        }

        private static string Num(float value)
        {
            return value.ToString("F6", CultureInfo.InvariantCulture);
        }

        private static string Precise(Vector3 value)
        {
            return value.x.ToString("R", CultureInfo.InvariantCulture) + "," +
                value.y.ToString("R", CultureInfo.InvariantCulture) + "," +
                value.z.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Precise(Quaternion value)
        {
            return value.x.ToString("R", CultureInfo.InvariantCulture) + "," +
                value.y.ToString("R", CultureInfo.InvariantCulture) + "," +
                value.z.ToString("R", CultureInfo.InvariantCulture) + "," +
                value.w.ToString("R", CultureInfo.InvariantCulture);
        }

        private sealed class RotationTrack
        {
            private readonly string path;
            private readonly Transform transform;
            private readonly List<Keyframe> x = new List<Keyframe>();
            private readonly List<Keyframe> y = new List<Keyframe>();
            private readonly List<Keyframe> z = new List<Keyframe>();
            private readonly List<Keyframe> w = new List<Keyframe>();
            private Quaternion previous;
            private bool hasPrevious;

            internal RotationTrack(string valuePath, Transform valueTransform)
            {
                path = valuePath;
                transform = valueTransform;
            }

            internal void Add(float time)
            {
                Quaternion rotation = transform.localRotation;
                if (hasPrevious && Quaternion.Dot(previous, rotation) < 0f)
                    rotation = new Quaternion(
                        -rotation.x, -rotation.y, -rotation.z, -rotation.w);
                previous = rotation;
                hasPrevious = true;
                x.Add(new Keyframe(time, rotation.x));
                y.Add(new Keyframe(time, rotation.y));
                z.Add(new Keyframe(time, rotation.z));
                w.Add(new Keyframe(time, rotation.w));
            }

            internal void Apply(AnimationClip clip)
            {
                SetRotationCurve(clip, path, "m_LocalRotation.x", x);
                SetRotationCurve(clip, path, "m_LocalRotation.y", y);
                SetRotationCurve(clip, path, "m_LocalRotation.z", z);
                SetRotationCurve(clip, path, "m_LocalRotation.w", w);
            }

            private static void SetRotationCurve(
                AnimationClip clip,
                string curvePath,
                string property,
                List<Keyframe> keys)
            {
                var curve = new AnimationCurve(keys.ToArray())
                {
                    preWrapMode = WrapMode.ClampForever,
                    postWrapMode = WrapMode.ClampForever
                };
                for (int index = 0; index < curve.length; index++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(
                        curve, index, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyRightTangentMode(
                        curve, index, AnimationUtility.TangentMode.Linear);
                }
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(
                        curvePath, typeof(Transform), property),
                    curve);
            }
        }

        private readonly struct UpperMotionMetrics
        {
            internal UpperMotionMetrics(float leftHandTravel, float rightHandTravel)
            {
                LeftHandTravel = leftHandTravel;
                RightHandTravel = rightHandTravel;
            }

            internal float LeftHandTravel { get; }
            internal float RightHandTravel { get; }

            internal string Describe()
            {
                return "leftHandTravelMeters=" + Num(LeftHandTravel) + "\n" +
                    "rightHandTravelMeters=" + Num(RightHandTravel);
            }
        }

        private readonly struct LocalPose
        {
            internal LocalPose(Transform transform)
            {
                Position = transform.localPosition;
                Rotation = transform.localRotation;
                Scale = transform.localScale;
            }

            internal Vector3 Position { get; }
            internal Quaternion Rotation { get; }
            internal Vector3 Scale { get; }
        }

        private struct PoseDelta
        {
            internal float Position;
            internal float Rotation;
            internal float Scale;

            internal string Describe()
            {
                return "maximumLowerBodyPositionErrorMeters=" + Num(Position) + "\n" +
                    "maximumLowerBodyRotationErrorDegrees=" + Num(Rotation) + "\n" +
                    "maximumLowerBodyScaleError=" + Num(Scale);
            }
        }

        private readonly struct TransformSnapshot
        {
            private readonly Transform target;
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            internal TransformSnapshot(Transform value)
            {
                target = value;
                position = value.localPosition;
                rotation = value.localRotation;
                scale = value.localScale;
            }

            internal void RequireUnchanged(string label)
            {
                if (Vector3.Distance(position, target.localPosition) > PositionTolerance ||
                    Quaternion.Angle(rotation, target.localRotation) > RotationTolerance ||
                    Vector3.Distance(scale, target.localScale) > ScaleTolerance)
                    throw new InvalidOperationException(label + " root Transform changed.");
            }
        }
    }

    [InitializeOnLoad]
    internal static class ShipRepairSharedAnimationPlayModeCapture
    {
        private const string PendingKey = "Bellerophon.RepairingShared.Pending";
        private const string StateKey = "Bellerophon.RepairingShared.State";
        private const string FailureKey = "Bellerophon.RepairingShared.Failure";
        private const string ConsoleErrorsBeforeKey =
            "Bellerophon.RepairingShared.ConsoleErrorsBefore";
        private const int WaitingForPlayMode = 0;
        private const int CapturingNaturalLoop = 1;
        private const int WaitingForEditModeAfterSuccess = 2;
        private const int WaitingForEditModeAfterFailure = 3;
        private static readonly float[] Phases = { 0.10f, 0.35f, 0.60f, 0.85f };
        private static readonly List<Texture2D> shipPanels = new List<Texture2D>();
        private static readonly List<Texture2D> sabotagePanels = new List<Texture2D>();
        private static Action<string> complete;
        private static Action<Exception> fail;
        private static GameObject ship;
        private static GameObject sabotage;
        private static AnimationClip clip;
        private static Vector3 shipPosition;
        private static Quaternion shipRotation;
        private static Vector3 shipScale;
        private static Vector3 sabotagePosition;
        private static Quaternion sabotageRotation;
        private static Vector3 sabotageScale;
        private static int phaseIndex;
        private static int baseLoop;
        private static float maximumNormalizedDifference;

        static ShipRepairSharedAnimationPlayModeCapture()
        {
        }

        internal static bool HasPendingCapture =>
            SessionState.GetBool(PendingKey, false);

        internal static void Start(Action<string> onComplete, Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Repairing shared final review must start in Edit Mode.");
            ShipRepairSharedAnimationTools.InspectAnimation();
            if (File.Exists(ShipRepairSharedAnimationTools.FinalAbsolutePath))
                throw new InvalidOperationException(
                    "Repairing shared final image already exists.");
            complete = onComplete;
            fail = onFail;
            CleanupPanels();
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.SetInt(ConsoleErrorsBeforeKey,
                LightsaberSetupTools.ConsoleErrorCount());
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        internal static void Resume(Action<string> onComplete, Action<Exception> onFail)
        {
            complete = onComplete;
            fail = onFail;
            if (!HasPendingCapture)
                throw new InvalidOperationException(
                    "Repairing shared Play Mode capture has no pending state.");
            Subscribe();
        }

        private static void Subscribe()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!HasPendingCapture)
            {
                EditorApplication.update -= Tick;
                return;
            }
            int state = SessionState.GetInt(StateKey, WaitingForPlayMode);
            try
            {
                if (state == WaitingForPlayMode)
                {
                    if (!EditorApplication.isPlaying) return;
                    InitializeRuntime();
                    SessionState.SetInt(StateKey, CapturingNaturalLoop);
                    return;
                }
                if (state == CapturingNaturalLoop)
                {
                    if (!EditorApplication.isPlaying)
                        throw new InvalidOperationException(
                            "Play Mode ended before repairing review completed.");
                    CaptureNextPhaseIfReady();
                    return;
                }
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (state == WaitingForEditModeAfterFailure)
                {
                    FinishFailure();
                    return;
                }
                ShipRepairSharedAnimationTools.InspectAnimation();
                Action<string> callback = complete;
                Cleanup();
                callback?.Invoke(
                    "ShipRepair and SabotageRepair natural looping playback captured once and restored to Edit Mode.");
            }
            catch (Exception exception)
            {
                SessionState.SetString(FailureKey, exception.ToString());
                CleanupPanels();
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetInt(StateKey, WaitingForEditModeAfterFailure);
                    if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
                    return;
                }
                FinishFailure();
            }
        }

        private static void InitializeRuntime()
        {
            ShipRepairSharedAnimationTools.RequireRuntimeSetup(
                out ship, out sabotage, out clip);
            Animator shipAnimator = ShipRepairSharedAnimationTools.RequireAnimator(ship);
            Animator sabotageAnimator =
                ShipRepairSharedAnimationTools.RequireAnimator(sabotage);
            AnimatorStateInfo shipState = shipAnimator.GetCurrentAnimatorStateInfo(1);
            AnimatorStateInfo sabotageState =
                sabotageAnimator.GetCurrentAnimatorStateInfo(1);
            baseLoop = Mathf.Max(
                Mathf.FloorToInt(shipState.normalizedTime),
                Mathf.FloorToInt(sabotageState.normalizedTime)) + 1;
            phaseIndex = 0;
            maximumNormalizedDifference = 0f;
            shipPosition = ship.transform.localPosition;
            shipRotation = ship.transform.localRotation;
            shipScale = ship.transform.localScale;
            sabotagePosition = sabotage.transform.localPosition;
            sabotageRotation = sabotage.transform.localRotation;
            sabotageScale = sabotage.transform.localScale;
        }

        private static void CaptureNextPhaseIfReady()
        {
            Animator shipAnimator = ShipRepairSharedAnimationTools.RequireAnimator(ship);
            Animator sabotageAnimator =
                ShipRepairSharedAnimationTools.RequireAnimator(sabotage);
            AnimatorStateInfo shipState = shipAnimator.GetCurrentAnimatorStateInfo(1);
            AnimatorStateInfo sabotageState =
                sabotageAnimator.GetCurrentAnimatorStateInfo(1);
            maximumNormalizedDifference = Mathf.Max(
                maximumNormalizedDifference,
                Mathf.Abs(shipState.normalizedTime - sabotageState.normalizedTime));
            float threshold = baseLoop + Phases[phaseIndex];
            if (shipState.normalizedTime < threshold ||
                sabotageState.normalizedTime < threshold)
                return;
            shipPanels.Add(
                ShipRepairSharedAnimationTools.RenderNaturalTarget(ship));
            sabotagePanels.Add(
                ShipRepairSharedAnimationTools.RenderNaturalTarget(sabotage));
            phaseIndex++;
            if (phaseIndex < Phases.Length) return;

            RequireRootUnchanged(ship, shipPosition, shipRotation, shipScale);
            RequireRootUnchanged(
                sabotage, sabotagePosition, sabotageRotation, sabotageScale);
            if (shipState.normalizedTime < 1f || sabotageState.normalizedTime < 1f)
                throw new InvalidOperationException(
                    "Both repair targets must complete at least one natural loop.");
            Texture2D sheet = ShipRepairSharedAnimationTools.CombinePanels(
                shipPanels, sabotagePanels);
            try
            {
                ShipRepairSharedAnimationTools.WriteFinalEvidence(
                    sheet,
                    clip.length,
                    maximumNormalizedDifference,
                    SessionState.GetInt(ConsoleErrorsBeforeKey, 0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
                CleanupPanels();
            }
            SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
            EditorApplication.ExitPlaymode();
        }

        private static void RequireRootUnchanged(
            GameObject target,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            if (Vector3.Distance(position, target.transform.localPosition) > 0.00001f ||
                Quaternion.Angle(rotation, target.transform.localRotation) > 0.01f ||
                Vector3.Distance(scale, target.transform.localScale) > 0.00001f)
                throw new InvalidOperationException(
                    target.name + " root Transform changed during natural playback.");
        }

        private static void FinishFailure()
        {
            string message = SessionState.GetString(
                FailureKey,
                "Repairing shared natural Play Mode review failed.");
            Action<Exception> callback = fail;
            Cleanup();
            callback?.Invoke(new InvalidOperationException(message));
        }

        private static void CleanupPanels()
        {
            foreach (Texture2D texture in shipPanels)
                if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            foreach (Texture2D texture in sabotagePanels)
                if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            shipPanels.Clear();
            sabotagePanels.Clear();
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            CleanupPanels();
            complete = null;
            fail = null;
            ship = null;
            sabotage = null;
            clip = null;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseInt(StateKey);
            SessionState.EraseInt(ConsoleErrorsBeforeKey);
            SessionState.EraseString(FailureKey);
        }
    }
}
