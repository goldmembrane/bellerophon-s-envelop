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
    [InitializeOnLoad]
    internal static class ArmorIdleAnimationMedicineStartTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlayerName = "Player";
        private const string SourceName = "Hands_Empty_Idle";
        private const string MedicineTargetName = "Medicine_Drink";
        private const string SourceClipPath =
            "Assets/_Project/Art/Player/Animations/Hands_Empty_Idle.anim";
        private const string SourceControllerPath =
            "Assets/_Project/Art/Player/Animations/Hands_Empty_Idle.controller";
        private const string OutputFolder =
            "docs/validation/armor_idle_animation_medicine_start_2026-09-08";
        private const float ReviewDistanceMeters = 2f;
        private const float ApplyPositionToleranceMeters = 0.01f;
        private const float RuntimePositionToleranceMeters = 0.20f;
        private const float FacingDotMinimum = 0.985f;
        private const float NormalizedTimeTolerance = 0.06f;
        private const int StartCaptureWidth = 1920;
        private const int StartCaptureHeight = 1080;
        private const int TileWidth = 320;
        private const int TileHeight = 420;

        private static readonly string[] TargetNames =
        {
            "Armor_Protective_Idle",
            "Armor_Insulated_Idle",
            "Armor_Fireproof_Idle",
            "Armor_Head_Idle",
            "Armor_Physical_Idle"
        };

        private static readonly string[] CaptureNames =
        {
            SourceName,
            "Armor_Protective_Idle",
            "Armor_Insulated_Idle",
            "Armor_Fireproof_Idle",
            "Armor_Head_Idle",
            "Armor_Physical_Idle"
        };

        private static readonly float[] CapturePhases = { 0.10f, 0.50f, 0.90f };

        private static Texture2D captureSheet;
        private static readonly List<float> ObservedCapturePhases = new List<float>();
        private static int captureRow;
        private static float previousSourcePhase;
        private static bool captureInitialized;
        private static bool waitForSourceLoop;

        static ArmorIdleAnimationMedicineStartTools()
        {
            EditorApplication.update -= CompletePendingCapture;
            EditorApplication.update += CompletePendingCapture;
        }

        internal static void InspectArmorIdleAnimationAndMedicineStart()
        {
            Scene scene = RequireScene();
            Transform source = FindUnique(SourceName);
            Animator sourceAnimator = RequireAnimator(source);
            AnimationClip sourceClip = RequireExactSource(sourceAnimator);
            Transform medicine = FindUnique(MedicineTargetName);
            Transform player = FindUnique(PlayerName);
            Camera playerCamera = RequirePlayerCamera(player);

            Directory.CreateDirectory(Absolute(OutputFolder));
            var report = new StringBuilder();
            report.AppendLine("Armor idle animation and Medicine_Drink start-view inspection");
            report.AppendLine("scene=" + scene.path);
            report.AppendLine("playMode=" + EditorApplication.isPlaying);
            report.AppendLine("source=" + SourceName);
            report.AppendLine("sourceAnimator=" + HierarchyPath(sourceAnimator.transform, source));
            report.AppendLine("sourceController=" + AssetDatabase.GetAssetPath(sourceAnimator.runtimeAnimatorController));
            report.AppendLine("sourceClip=" + AssetDatabase.GetAssetPath(sourceClip));
            report.AppendLine("sourceClipLength=" + Num(sourceClip.length));
            report.AppendLine("sourceClipLooping=" + sourceClip.isLooping);
            report.AppendLine("sourceControllerSha256=" + Sha256(Absolute(SourceControllerPath)));
            report.AppendLine("sourceClipSha256=" + Sha256(Absolute(SourceClipPath)));
            AppendAnimatorConfiguration(report, sourceAnimator, "source");

            foreach (string targetName in TargetNames)
            {
                Transform target = FindUnique(targetName);
                Animator[] targetAnimators = target.GetComponentsInChildren<Animator>(true);
                report.AppendLine();
                report.AppendLine("[" + targetName + "]");
                report.AppendLine("rootPosition=" + Vec(target.position));
                report.AppendLine("rootRotation=" + Vec(target.eulerAngles));
                report.AppendLine("rootScale=" + Vec(target.localScale));
                report.AppendLine("animatorCount=" + targetAnimators.Length);
                if (targetAnimators.Length == 0)
                {
                    report.AppendLine("animator=<missing; source root Animator must be copied>");
                }
                else if (targetAnimators.Length == 1)
                {
                    Animator targetAnimator = targetAnimators[0];
                    report.AppendLine("animator=" + HierarchyPath(targetAnimator.transform, target));
                    report.AppendLine("avatar=" + AssetDatabase.GetAssetPath(targetAnimator.avatar));
                    report.AppendLine("controller=" + AssetDatabase.GetAssetPath(targetAnimator.runtimeAnimatorController));
                    AppendAnimatorConfiguration(report, targetAnimator, "target");
                }
                else
                {
                    throw new InvalidOperationException(targetName + " Animator count=" + targetAnimators.Length + ".");
                }
            }

            Bounds medicineBounds = BoundsOf(medicine);
            report.AppendLine();
            report.AppendLine("[Medicine_Drink start view]");
            report.AppendLine("targetPosition=" + Vec(medicine.position));
            report.AppendLine("targetForward=" + Vec(Horizontal(medicine.forward)));
            report.AppendLine("targetBoundsCenter=" + Vec(medicineBounds.center));
            report.AppendLine("playerPosition=" + Vec(player.position));
            report.AppendLine("playerForward=" + Vec(player.forward));
            report.AppendLine("cameraPosition=" + Vec(playerCamera.transform.position));

            if (EditorApplication.isPlaying)
            {
                AppendRuntimeAnimationMetrics(report, sourceAnimator);
                AppendStartViewMetrics(report, InspectStartView(player, medicine, playerCamera, medicineBounds, true));
                report.AppendLine("runtimeInspectionPassed=True");
            }

            File.WriteAllText(
                Absolute(OutputFolder + (EditorApplication.isPlaying
                    ? "/runtime_metrics.txt"
                    : "/source_inspection.txt")),
                report.ToString(),
                new UTF8Encoding(false));

            Debug.Log("Armor idle animation and Medicine_Drink start view inspected without modifying the scene.");
        }

        internal static void ApplyArmorIdleAnimationAndMedicineStart()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Armor idle animation setup must be applied in Edit Mode.");

            Scene scene = RequireScene();
            Transform source = FindUnique(SourceName);
            Animator sourceAnimator = RequireAnimator(source);
            AnimationClip sourceClip = RequireExactSource(sourceAnimator);
            string controllerHashBefore = Sha256(Absolute(SourceControllerPath));
            string clipHashBefore = Sha256(Absolute(SourceClipPath));
            RuntimeAnimatorController sourceController = sourceAnimator.runtimeAnimatorController;

            var targetSnapshots = TargetNames.ToDictionary(
                targetName => targetName,
                targetName => TransformSnapshot.Capture(FindUnique(targetName)),
                StringComparer.Ordinal);
            Transform medicine = FindUnique(MedicineTargetName);
            TransformSnapshot medicineSnapshot = TransformSnapshot.Capture(medicine);
            Transform player = FindUnique(PlayerName);
            Camera playerCamera = RequirePlayerCamera(player);
            float playerHeightBefore = player.position.y;

            foreach (string targetName in TargetNames)
            {
                Transform target = FindUnique(targetName);
                Animator[] targetAnimators = target.GetComponentsInChildren<Animator>(true);
                if (targetAnimators.Length > 1)
                    throw new InvalidOperationException(targetName + " Animator count=" + targetAnimators.Length + ".");
                Animator targetAnimator = targetAnimators.Length == 1
                    ? targetAnimators[0]
                    : target.gameObject.AddComponent<Animator>();

                targetAnimator.avatar = sourceAnimator.avatar;

                targetAnimator.runtimeAnimatorController = sourceController;
                targetAnimator.applyRootMotion = sourceAnimator.applyRootMotion;
                targetAnimator.updateMode = sourceAnimator.updateMode;
                targetAnimator.cullingMode = sourceAnimator.cullingMode;
                targetAnimator.fireEvents = sourceAnimator.fireEvents;
                targetAnimator.keepAnimatorStateOnDisable = sourceAnimator.keepAnimatorStateOnDisable;
                targetAnimator.writeDefaultValuesOnDisable = sourceAnimator.writeDefaultValuesOnDisable;
                targetAnimator.stabilizeFeet = sourceAnimator.stabilizeFeet;
                targetAnimator.enabled = sourceAnimator.enabled;

                if (targetAnimator.avatar != sourceAnimator.avatar)
                    throw new InvalidOperationException(targetName + " Avatar differs from Hands_Empty_Idle after copying.");

                EditorUtility.SetDirty(targetAnimator);
                PrefabUtility.RecordPrefabInstancePropertyModifications(targetAnimator);
            }

            Bounds medicineBounds = BoundsOf(medicine);
            Vector3 front = Horizontal(medicine.forward);
            if (front.sqrMagnitude < 0.99f)
                throw new InvalidOperationException("Medicine_Drink has no valid horizontal forward direction.");

            Vector3 desiredCamera = medicineBounds.center + front * ReviewDistanceMeters;
            Quaternion desiredYaw = Quaternion.LookRotation(-front, Vector3.up);
            Vector3 cameraOffsetInPlayerYaw =
                Quaternion.Inverse(player.rotation) * (playerCamera.transform.position - player.position);
            Vector3 desiredPlayer = desiredCamera - desiredYaw * cameraOffsetInPlayerYaw;
            desiredPlayer.y = playerHeightBefore;
            player.SetPositionAndRotation(desiredPlayer, desiredYaw);
            EditorUtility.SetDirty(player.gameObject);
            PrefabUtility.RecordPrefabInstancePropertyModifications(player);

            foreach (KeyValuePair<string, TransformSnapshot> pair in targetSnapshots)
                pair.Value.RequireUnchanged(FindUnique(pair.Key), pair.Key);
            medicineSnapshot.RequireUnchanged(medicine, MedicineTargetName);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");

            if (controllerHashBefore != Sha256(Absolute(SourceControllerPath)))
                throw new InvalidOperationException("Hands_Empty_Idle controller changed during application.");
            if (clipHashBefore != Sha256(Absolute(SourceClipPath)))
                throw new InvalidOperationException("Hands_Empty_Idle clip changed during application.");

            StartViewMetrics startMetrics = InspectStartView(
                player,
                medicine,
                playerCamera,
                BoundsOf(medicine),
                false);

            var report = new StringBuilder();
            report.AppendLine("Armor idle animation and Medicine_Drink start view applied.");
            report.AppendLine("sourceController=" + SourceControllerPath);
            report.AppendLine("sourceClip=" + SourceClipPath);
            report.AppendLine("sourceClipLength=" + Num(sourceClip.length));
            report.AppendLine("sourceClipLooping=" + sourceClip.isLooping);
            report.AppendLine("controllerSharedDirectly=True");
            report.AppendLine("newAnimationAssetCreated=False");
            report.AppendLine("sourceAssetsUnchanged=True");
            report.AppendLine("targetAvatarsCopiedFromSource=True");
            report.AppendLine("targetRootTransformsPreserved=True");
            report.AppendLine("Medicine_DrinkTransformPreserved=True");
            report.AppendLine("playerHeightPreserved=True");
            report.AppendLine("targets=" + string.Join(",", TargetNames));
            report.AppendLine("playerPosition=" + Vec(player.position));
            report.AppendLine("playerForward=" + Vec(player.forward));
            AppendStartViewMetrics(report, startMetrics);
            report.AppendLine("sceneSaved=True");

            Directory.CreateDirectory(Absolute(OutputFolder));
            File.WriteAllText(
                Absolute(OutputFolder + "/applied.txt"),
                report.ToString(),
                new UTF8Encoding(false));

            Debug.Log("Hands_Empty_Idle was linked unchanged to five Armor targets and the player start view now faces Medicine_Drink.");
        }

        internal static void EnterArmorIdleAnimationAndMedicineStartReview()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.EnterPlaymode();
                Debug.Log("Armor idle animation and Medicine_Drink review entered Play Mode.");
                return;
            }
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Armor idle animation review is waiting for Play Mode.");

            InspectArmorIdleAnimationAndMedicineStart();
        }

        internal static void CaptureArmorIdleAnimationAndMedicineStartReview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Final capture must begin in Edit Mode.");

            string contactSheetPath = Absolute(OutputFolder + "/armor_idle_animation_contact_sheet.png");
            string startViewPath = Absolute(OutputFolder + "/medicine_start_view.png");
            if (File.Exists(contactSheetPath) || File.Exists(startViewPath))
                throw new InvalidOperationException("The one-time final capture already exists.");

            Directory.CreateDirectory(Absolute(OutputFolder));
            File.WriteAllText(
                Absolute(OutputFolder + "/capture_request.txt"),
                "Capture unmodified live playback at three phases and the actual player start view.",
                new UTF8Encoding(false));
            ResetCaptureState();
            EditorApplication.EnterPlaymode();
            Debug.Log("Armor idle animation and Medicine_Drink one-time final capture entered Play Mode.");
        }

        internal static void StopArmorIdleAnimationAndMedicineStartReview()
        {
            string requestPath = Absolute(OutputFolder + "/capture_request.txt");
            if (File.Exists(requestPath))
                File.Delete(requestPath);
            ResetCaptureState();
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.ExitPlaymode();
        }

        private static void CompletePendingCapture()
        {
            string requestPath = Absolute(OutputFolder + "/capture_request.txt");
            if (!EditorApplication.isPlaying || !File.Exists(requestPath))
                return;

            try
            {
                Scene scene = RequireScene();
                Transform source = FindUnique(SourceName);
                Animator sourceAnimator = RequireAnimator(source);
                RequireExactSource(sourceAnimator);

                AnimatorStateInfo sourceState = sourceAnimator.GetCurrentAnimatorStateInfo(0);
                float sourcePhase = Mathf.Repeat(sourceState.normalizedTime, 1f);
                if (!captureInitialized)
                {
                    captureInitialized = true;
                    previousSourcePhase = sourcePhase;
                    waitForSourceLoop = sourcePhase > CapturePhases[0];
                    captureSheet = NewFilledTexture(
                        TileWidth * CaptureNames.Length,
                        TileHeight * CapturePhases.Length,
                        new Color32(18, 21, 27, 255));

                    Transform player = FindUnique(PlayerName);
                    Camera playerCamera = RequirePlayerCamera(player);
                    Transform medicine = FindUnique(MedicineTargetName);
                    InspectStartView(player, medicine, playerCamera, BoundsOf(medicine), true);
                    RenderCameraToPng(
                        playerCamera,
                        Absolute(OutputFolder + "/medicine_start_view.png"),
                        StartCaptureWidth,
                        StartCaptureHeight);
                }

                if (waitForSourceLoop)
                {
                    if (sourcePhase < previousSourcePhase)
                        waitForSourceLoop = false;
                    previousSourcePhase = sourcePhase;
                    return;
                }

                if (captureRow < CapturePhases.Length && sourcePhase >= CapturePhases[captureRow])
                {
                    AppendRuntimeAnimationMetrics(new StringBuilder(), sourceAnimator);
                    CaptureLiveRow(captureRow);
                    ObservedCapturePhases.Add(sourcePhase);
                    captureRow++;
                }

                previousSourcePhase = sourcePhase;
                if (captureRow < CapturePhases.Length)
                    return;

                string contactSheetPath = Absolute(OutputFolder + "/armor_idle_animation_contact_sheet.png");
                File.WriteAllBytes(contactSheetPath, captureSheet.EncodeToPNG());

                var report = new StringBuilder();
                report.AppendLine("One-time direct live animation and start-view capture.");
                report.AppendLine("scene=" + scene.path);
                report.AppendLine("captureMode=unmodified live Play Mode playback");
                report.AppendLine("tileOrderLeftToRight=" + string.Join(",", CaptureNames));
                report.AppendLine("rowsTopToBottom=normalized phases 0.10,0.50,0.90");
                report.AppendLine("observedPhases=" + string.Join(",", ObservedCapturePhases.Select(Num)));
                report.AppendLine("targetsMovedForValidation=False");
                report.AppendLine("animationSampledOrForced=False");
                report.AppendLine("sourceController=" + SourceControllerPath);
                report.AppendLine("sourceClip=" + SourceClipPath);
                report.AppendLine("contactSheetSha256=" + Sha256(contactSheetPath));
                report.AppendLine("startViewSha256=" + Sha256(Absolute(OutputFolder + "/medicine_start_view.png")));
                File.WriteAllText(
                    Absolute(OutputFolder + "/capture.txt"),
                    report.ToString(),
                    new UTF8Encoding(false));

                InspectArmorIdleAnimationAndMedicineStart();
                File.Delete(requestPath);
                Debug.Log("Armor idle animation and Medicine_Drink one-time final capture completed.");
                ResetCaptureState();
                EditorApplication.delayCall += () =>
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                        EditorApplication.ExitPlaymode();
                };
            }
            catch (Exception exception)
            {
                File.WriteAllText(
                    Absolute(OutputFolder + "/capture_failure.txt"),
                    exception.ToString(),
                    new UTF8Encoding(false));
                if (File.Exists(requestPath))
                    File.Delete(requestPath);
                ResetCaptureState();
                Debug.LogException(exception);
                EditorApplication.delayCall += () =>
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                        EditorApplication.ExitPlaymode();
                };
            }
        }

        private static void CaptureLiveRow(int row)
        {
            int destinationY = (CapturePhases.Length - 1 - row) * TileHeight;
            for (int column = 0; column < CaptureNames.Length; column++)
            {
                Transform target = FindUnique(CaptureNames[column]);
                Color32[] pixels = RenderLiveTarget(target, TileWidth, TileHeight);
                captureSheet.SetPixels32(column * TileWidth, destinationY, TileWidth, TileHeight, pixels);
            }
            captureSheet.Apply(false, false);
        }

        private static Color32[] RenderLiveTarget(Transform target, int width, int height)
        {
            Bounds bounds = BoundsOf(target);
            Vector3 front = Horizontal(target.forward);
            if (front.sqrMagnitude < 0.99f)
                front = Vector3.forward;

            var cameraObject = new GameObject("ArmorIdleDirectValidationCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.065f, 0.085f, 1f);
            camera.fieldOfView = 32f;
            camera.aspect = (float)width / height;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 1000f;
            camera.cullingMask = ~0;

            float verticalFov = camera.fieldOfView * Mathf.Deg2Rad;
            float horizontalFov = 2f * Mathf.Atan(Mathf.Tan(verticalFov * 0.5f) * camera.aspect);
            float verticalDistance = bounds.extents.y * 1.18f / Mathf.Tan(verticalFov * 0.5f);
            float horizontalDistance = bounds.extents.x * 1.18f / Mathf.Tan(horizontalFov * 0.5f);
            float distance = Mathf.Max(verticalDistance, horizontalDistance) + bounds.extents.z + 0.15f;
            camera.transform.position = bounds.center + front * distance;
            camera.transform.rotation = Quaternion.LookRotation(bounds.center - camera.transform.position, Vector3.up);

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture render = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = render;
                camera.Render();
                RenderTexture.active = render;
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                image.Apply(false, false);
                return image.GetPixels32();
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void RenderCameraToPng(Camera camera, string destination, int width, int height)
        {
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture render = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = render;
                camera.Render();
                RenderTexture.active = render;
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                image.Apply(false, false);
                File.WriteAllBytes(destination, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        private static Texture2D NewFilledTexture(int width, int height, Color32 color)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color32[] pixels = Enumerable.Repeat(color, width * height).ToArray();
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static void ResetCaptureState()
        {
            if (captureSheet != null)
                UnityEngine.Object.DestroyImmediate(captureSheet);
            captureSheet = null;
            ObservedCapturePhases.Clear();
            captureRow = 0;
            previousSourcePhase = 0f;
            captureInitialized = false;
            waitForSourceLoop = false;
        }

        private static AnimationClip RequireExactSource(Animator sourceAnimator)
        {
            if (sourceAnimator.runtimeAnimatorController == null)
                throw new InvalidOperationException("Hands_Empty_Idle Animator has no controller.");
            string controllerPath = AssetDatabase.GetAssetPath(sourceAnimator.runtimeAnimatorController);
            if (!string.Equals(controllerPath, SourceControllerPath, StringComparison.Ordinal))
                throw new InvalidOperationException("Hands_Empty_Idle uses an unexpected controller: " + controllerPath);

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(SourceControllerPath);
            if (controller == null || controller.layers.Length != 1)
                throw new InvalidOperationException("Hands_Empty_Idle controller must have exactly one layer.");
            AnimatorState state = controller.layers[0].stateMachine.defaultState;
            AnimationClip clip = state != null ? state.motion as AnimationClip : null;
            if (clip == null || AssetDatabase.GetAssetPath(clip) != SourceClipPath)
                throw new InvalidOperationException("Hands_Empty_Idle controller does not point directly to the approved source clip.");
            if (!clip.isLooping)
                throw new InvalidOperationException("Hands_Empty_Idle source clip is not configured to loop.");
            return clip;
        }

        private static void AppendRuntimeAnimationMetrics(StringBuilder report, Animator sourceAnimator)
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Runtime animation inspection requires Play Mode.");
            AnimatorStateInfo sourceState = sourceAnimator.GetCurrentAnimatorStateInfo(0);
            float sourcePhase = Mathf.Repeat(sourceState.normalizedTime, 1f);
            report.AppendLine();
            report.AppendLine("[runtime animation]");
            report.AppendLine("sourceStateHash=" + sourceState.fullPathHash);
            report.AppendLine("sourcePhase=" + Num(sourcePhase));
            foreach (string targetName in TargetNames)
            {
                Animator targetAnimator = RequireAnimator(FindUnique(targetName));
                if (targetAnimator.runtimeAnimatorController != sourceAnimator.runtimeAnimatorController)
                    throw new InvalidOperationException(targetName + " does not share the Hands_Empty_Idle controller.");
                AnimatorStateInfo targetState = targetAnimator.GetCurrentAnimatorStateInfo(0);
                if (targetState.fullPathHash != sourceState.fullPathHash)
                    throw new InvalidOperationException(targetName + " is not playing the same Animator state.");
                float targetPhase = Mathf.Repeat(targetState.normalizedTime, 1f);
                float phaseError = Mathf.Abs(Mathf.DeltaAngle(sourcePhase * 360f, targetPhase * 360f)) / 360f;
                if (phaseError > NormalizedTimeTolerance)
                    throw new InvalidOperationException(targetName + " normalized time differs from Hands_Empty_Idle by " + Num(phaseError) + ".");
                report.AppendLine(targetName + ".stateHash=" + targetState.fullPathHash);
                report.AppendLine(targetName + ".phase=" + Num(targetPhase));
                report.AppendLine(targetName + ".phaseError=" + Num(phaseError));
                report.AppendLine(targetName + ".controllerShared=True");
            }
        }

        private static void AppendAnimatorConfiguration(StringBuilder report, Animator animator, string prefix)
        {
            report.AppendLine(prefix + ".enabled=" + animator.enabled);
            report.AppendLine(prefix + ".applyRootMotion=" + animator.applyRootMotion);
            report.AppendLine(prefix + ".updateMode=" + animator.updateMode);
            report.AppendLine(prefix + ".cullingMode=" + animator.cullingMode);
            report.AppendLine(prefix + ".fireEvents=" + animator.fireEvents);
            report.AppendLine(prefix + ".keepStateOnDisable=" + animator.keepAnimatorStateOnDisable);
            report.AppendLine(prefix + ".writeDefaultsOnDisable=" + animator.writeDefaultValuesOnDisable);
            report.AppendLine(prefix + ".stabilizeFeet=" + animator.stabilizeFeet);
        }

        private static StartViewMetrics InspectStartView(
            Transform player,
            Transform target,
            Camera camera,
            Bounds bounds,
            bool runtime)
        {
            Vector3 front = Horizontal(target.forward);
            Vector3 centerToCamera = Vector3.ProjectOnPlane(camera.transform.position - bounds.center, Vector3.up);
            float cameraDistance = centerToCamera.magnitude;
            float frontAxisDot = Vector3.Dot(centerToCamera.normalized, front);
            Vector3 cameraForward = Horizontal(camera.transform.forward);
            Vector3 cameraToCenter = Horizontal(bounds.center - camera.transform.position);
            float facingDot = Vector3.Dot(cameraForward, cameraToCenter);
            Vector3 centerViewport = camera.WorldToViewportPoint(bounds.center);
            float distanceTolerance = runtime ? RuntimePositionToleranceMeters : ApplyPositionToleranceMeters;

            if (Mathf.Abs(cameraDistance - ReviewDistanceMeters) > distanceTolerance)
                throw new InvalidOperationException("Player camera distance from Medicine_Drink is " + Num(cameraDistance) + ".");
            if (frontAxisDot < FacingDotMinimum)
                throw new InvalidOperationException("Player camera is not on the Medicine_Drink front axis. Dot=" + Num(frontAxisDot) + ".");
            if (facingDot < FacingDotMinimum)
                throw new InvalidOperationException("Player camera is not facing Medicine_Drink. Dot=" + Num(facingDot) + ".");
            if (centerViewport.z <= camera.nearClipPlane ||
                centerViewport.x < 0.15f || centerViewport.x > 0.85f ||
                centerViewport.y < 0.15f || centerViewport.y > 0.85f)
                throw new InvalidOperationException("Medicine_Drink is not centered in the player camera. Viewport=" + Vec(centerViewport) + ".");

            return new StartViewMetrics(cameraDistance, frontAxisDot, facingDot, centerViewport, player.position.y);
        }

        private static void AppendStartViewMetrics(StringBuilder report, StartViewMetrics metrics)
        {
            report.AppendLine("cameraHorizontalDistance=" + Num(metrics.CameraDistance));
            report.AppendLine("cameraFrontAxisDot=" + Num(metrics.FrontAxisDot));
            report.AppendLine("cameraFacingDot=" + Num(metrics.FacingDot));
            report.AppendLine("targetCenterViewport=" + Vec(metrics.CenterViewport));
            report.AppendLine("playerStartHeight=" + Num(metrics.PlayerHeight));
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must already be the active scene. Active=" + scene.path + ".");
            return scene;
        }

        private static Transform FindUnique(string name)
        {
            Transform[] matches = SceneManager.GetActiveScene()
                .GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(name + " count=" + matches.Length + ".");
            return matches[0];
        }

        private static Animator RequireAnimator(Transform root)
        {
            Animator[] animators = root.GetComponentsInChildren<Animator>(true);
            if (animators.Length != 1)
                throw new InvalidOperationException(root.name + " Animator count=" + animators.Length + ".");
            return animators[0];
        }

        private static Camera RequirePlayerCamera(Transform player)
        {
            Camera[] cameras = player.GetComponentsInChildren<Camera>(true);
            if (cameras.Length != 1)
                throw new InvalidOperationException("Player must contain exactly one camera. Count=" + cameras.Length + ".");
            return cameras[0];
        }

        private static Bounds BoundsOf(Transform target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(target.name + " has no enabled renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static Vector3 Horizontal(Vector3 value)
        {
            Vector3 horizontal = Vector3.ProjectOnPlane(value, Vector3.up);
            return horizontal.sqrMagnitude < 0.000001f ? Vector3.zero : horizontal.normalized;
        }

        private static string HierarchyPath(Transform item, Transform root)
        {
            var names = new Stack<string>();
            Transform current = item;
            while (current != null)
            {
                names.Push(current.name);
                if (current == root)
                    break;
                current = current.parent;
            }
            return string.Join("/", names);
        }

        private static string Absolute(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Project root is unavailable."),
                projectRelativePath));
        }

        private static string Sha256(string path)
        {
            using (SHA256 algorithm = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
                return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Num(float value) => value.ToString("F6", CultureInfo.InvariantCulture);

        private static string Vec(Vector3 value) =>
            "(" + Num(value.x) + ", " + Num(value.y) + ", " + Num(value.z) + ")";

        private readonly struct StartViewMetrics
        {
            internal StartViewMetrics(float cameraDistance, float frontAxisDot, float facingDot,
                Vector3 centerViewport, float playerHeight)
            {
                CameraDistance = cameraDistance;
                FrontAxisDot = frontAxisDot;
                FacingDot = facingDot;
                CenterViewport = centerViewport;
                PlayerHeight = playerHeight;
            }

            internal float CameraDistance { get; }
            internal float FrontAxisDot { get; }
            internal float FacingDot { get; }
            internal Vector3 CenterViewport { get; }
            internal float PlayerHeight { get; }
        }

        private readonly struct TransformSnapshot
        {
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            private TransformSnapshot(Vector3 position, Quaternion rotation, Vector3 scale)
            {
                this.position = position;
                this.rotation = rotation;
                this.scale = scale;
            }

            internal static TransformSnapshot Capture(Transform target) =>
                new TransformSnapshot(target.position, target.rotation, target.localScale);

            internal void RequireUnchanged(Transform target, string label)
            {
                if (Vector3.Distance(position, target.position) > 0.000001f ||
                    Quaternion.Angle(rotation, target.rotation) > 0.000001f ||
                    Vector3.Distance(scale, target.localScale) > 0.000001f)
                    throw new InvalidOperationException(label + " root transform changed unexpectedly.");
            }
        }
    }
}
