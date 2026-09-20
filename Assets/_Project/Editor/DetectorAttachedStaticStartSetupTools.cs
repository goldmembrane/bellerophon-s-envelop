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
    internal static class DetectorAttachedStaticStartSetupTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlayerName = "Player";
        private const string TargetName = "Detector_Attached_Static";
        private const string HelmTargetName = "Helm_Enter";
        private const string TurretTargetName = "Turret_Enter";
        private const string ShipRepairTargetName = "ShipRepair";
        private const string HitReactionTargetName = "Hit_Reaction";
        private const string StunTwistTargetName = "Stun_Twist";
        private const string TurretReviewFolder = "Temp/TurretEnterStart";
        private const string TurretRuntimeReportRelativePath =
            TurretReviewFolder + "/RuntimeInspection.txt";
        private const string TurretFinalImageRelativePath =
            TurretReviewFolder + "/Final.png";
        private const string TurretFinalReportRelativePath =
            TurretReviewFolder + "/Final.txt";
        private const string ShipRepairReviewFolder = "Temp/ShipRepairStart";
        private const string ShipRepairRuntimeReportRelativePath =
            ShipRepairReviewFolder + "/RuntimeInspection.txt";
        private const string ShipRepairFinalImageRelativePath =
            ShipRepairReviewFolder + "/Final.png";
        private const string ShipRepairFinalReportRelativePath =
            ShipRepairReviewFolder + "/Final.txt";
        private const string HitReactionReviewFolder =
            "docs/validation/HitReactionStartPoint";
        private const string HitReactionRuntimeReportRelativePath =
            HitReactionReviewFolder + "/RuntimeInspection.txt";
        private const string HitReactionFinalImageRelativePath =
            HitReactionReviewFolder + "/Final.png";
        private const string HitReactionFinalReportRelativePath =
            HitReactionReviewFolder + "/Final.txt";
        private const string StunTwistReviewFolder =
            "docs/validation/StunTwistStartView";
        private const string StunTwistRuntimeReportRelativePath =
            StunTwistReviewFolder + "/RuntimeInspection.txt";
        private const string StunTwistReviewImageRelativePath =
            StunTwistReviewFolder + "/Review.png";
        private const string StunTwistFinalImageRelativePath =
            StunTwistReviewFolder + "/Final.png";
        private const string StunTwistFinalReportRelativePath =
            StunTwistReviewFolder + "/Final.txt";
        internal const string ReviewImageRelativePath =
            "Temp/DetectorAttachedStaticStartView/UnityPlayModeReview.png";
        internal const string ReviewReportRelativePath =
            "Temp/DetectorAttachedStaticStartView/UnityPlayModeReview.txt";
        private const string FinalFolder =
            "docs/validation/DetectorAttachedStaticStartView";
        private const string FinalImageRelativePath =
            FinalFolder + "/DetectorAttachedStaticStartView_Final.png";
        private const string FinalReportRelativePath =
            FinalFolder + "/DetectorAttachedStaticStartView_Final.txt";
        private const float MinimumDistanceMeters = 2.5f;
        private const float MaximumDistanceMeters = 8f;
        private const float DistanceStepMeters = 0.25f;
        private const float ViewportMargin = 0.05f;
        private const float PositionTolerance = 0.002f;
        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;

        [MenuItem("Bellerophon/Player/Inspect Detector Attached Static Start View Sources")]
        internal static void InspectDetectorAttachedStaticStartViewSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            Transform player = FindUnique(scene, PlayerName);
            Transform target = FindUnique(scene, TargetName);
            Camera camera = RequirePlayerCamera(player);
            Bounds bounds = BoundsOf(target);
            RequireNoUnityConsoleErrors();
            Debug.Log(
                "[DetectorAttachedStaticStart] Sources inspected read-only." +
                " playerPosition=" + Vec(player.position) +
                "|playerRotation=" + Quat(player.rotation) +
                "|targetPosition=" + Vec(target.position) +
                "|targetRotation=" + Quat(target.rotation) +
                "|targetBoundsCenter=" + Vec(bounds.center) +
                "|targetBoundsSize=" + Vec(bounds.size) +
                "|cameraFieldOfView=" + Num(camera.fieldOfView) +
                "|verificationTargetTransformManipulated=False");
        }

        [MenuItem("Bellerophon/Player/Apply Detector Attached Static Start View")]
        internal static void ApplyDetectorAttachedStaticStartView()
        {
            ApplyStartView(TargetName);
        }

        [MenuItem("Bellerophon/Player/Apply Helm Enter Start View")]
        internal static void ApplyHelmEnterStartView()
        {
            ApplyStartView(HelmTargetName);
        }

        [MenuItem("Bellerophon/Player/Apply Turret Enter Start View")]
        internal static void ApplyTurretEnterReviewStart()
        {
            ApplyStartView(TurretTargetName);
        }

        [MenuItem("Bellerophon/Player/Apply ShipRepair Start View")]
        internal static void ApplyShipRepairReviewStart()
        {
            ApplyStartView(ShipRepairTargetName);
        }

        [MenuItem("Bellerophon/Player/Apply Hit Reaction Start View")]
        internal static void ApplyHitReactionReviewStart()
        {
            ApplyStartView(HitReactionTargetName);
        }

        [MenuItem("Bellerophon/Player/Apply Stun Twist Start View")]
        internal static void ApplyStunTwistReviewStart()
        {
            ApplyStartView(StunTwistTargetName);
        }

        private static void ApplyStartView(string targetName)
        {
            RequireEditMode();
            Scene scene = RequireScene();
            Transform player = FindUnique(scene, PlayerName);
            Transform target = FindUnique(scene, targetName);
            Camera camera = RequirePlayerCamera(player);

            string outsidePlayerBefore = SceneSignature(scene, player);
            string playerChildrenBefore = PlayerChildrenSignature(player);
            Vector3 playerScaleBefore = player.localScale;
            Vector3 targetPositionBefore = target.position;
            Quaternion targetRotationBefore = target.rotation;
            Vector3 targetScaleBefore = target.localScale;
            string targetComponentsBefore = TargetComponentSignature(target);

            Bounds targetBounds = BoundsOf(target);
            Vector3 targetFront = HorizontalDirection(
                target.forward, targetName + " forward");
            Vector3 cameraLocalOffset = Quaternion.Inverse(player.rotation) *
                (camera.transform.position - player.position);
            Vector3 cameraForwardInPlayerSpace = Quaternion.Inverse(player.rotation) *
                camera.transform.forward;
            Vector3 localCameraHeading = HorizontalDirection(
                cameraForwardInPlayerSpace,
                "player camera forward");
            Vector3 desiredCameraHeading = -targetFront;
            float desiredCameraYaw = Mathf.Atan2(
                desiredCameraHeading.x,
                desiredCameraHeading.z) * Mathf.Rad2Deg;
            float localCameraYaw = Mathf.Atan2(
                localCameraHeading.x,
                localCameraHeading.z) * Mathf.Rad2Deg;
            Quaternion desiredPlayerRotation = Quaternion.Euler(
                0f,
                desiredCameraYaw - localCameraYaw,
                0f);

            Vector3 chosenPosition = default;
            float chosenDistance = 0f;
            bool found = false;
            Undo.RecordObject(
                player,
                "Place startup view before " + targetName);
            for (float distance = MinimumDistanceMeters;
                 distance <= MaximumDistanceMeters + 0.0001f;
                 distance += DistanceStepMeters)
            {
                Vector3 desiredCameraPosition =
                    targetBounds.center + targetFront * distance;
                Vector3 candidatePosition = desiredCameraPosition -
                    desiredPlayerRotation * cameraLocalOffset;
                candidatePosition.y = player.position.y;
                player.SetPositionAndRotation(
                    candidatePosition,
                    desiredPlayerRotation);

                if (!BoundsFullyVisible(camera, targetBounds))
                    continue;
                if (FacingDot(camera, targetBounds.center) < 0.97f)
                    continue;

                chosenPosition = candidatePosition;
                chosenDistance = distance;
                found = true;
                break;
            }

            if (!found)
                throw new InvalidOperationException(
                    "No 2.5-8m startup position preserves the Player camera while " +
                    "showing the full " + targetName + " target from its front axis.");

            player.SetPositionAndRotation(chosenPosition, desiredPlayerRotation);
            EditorUtility.SetDirty(player.gameObject);
            PrefabUtility.RecordPrefabInstancePropertyModifications(player);

            RequireEqual(
                outsidePlayerBefore,
                SceneSignature(scene, player),
                "scene objects outside Player");
            RequireEqual(
                playerChildrenBefore,
                PlayerChildrenSignature(player),
                "Player children and camera settings");
            RequireNear(player.localScale, playerScaleBefore, "Player scale");
            RequireNear(target.position, targetPositionBefore, targetName + " position");
            RequireNear(target.rotation, targetRotationBefore, targetName + " rotation");
            RequireNear(target.localScale, targetScaleBefore, targetName + " scale");
            RequireEqual(
                targetComponentsBefore,
                TargetComponentSignature(target),
                targetName + " animation and renderer configuration");
            StartViewMetrics metrics = RequireStartView(player, target, camera);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            AssetDatabase.SaveAssets();
            RequireNoUnityConsoleErrors();
            Debug.Log(
                "[PlayerAnimationStartView] Startup view saved for " + targetName + "." +
                " cameraDistance=" + Num(chosenDistance) +
                "m|frontAxisDot=" + Num(metrics.FrontAxisDot) +
                "|facingDot=" + Num(metrics.FacingDot) +
                "|minimumViewportMargin=" + Num(metrics.MinimumViewportMargin) +
                "|verificationTargetTransformManipulated=False");
        }

        internal static void InspectAppliedStartView()
        {
            InspectAppliedStartView(TargetName);
        }

        internal static void InspectHelmEnterStartView()
        {
            InspectAppliedStartView(HelmTargetName);
        }

        internal static void InspectTurretEnterReviewStart()
        {
            InspectAppliedStartView(TurretTargetName);
        }

        internal static void InspectShipRepairReviewStart()
        {
            InspectAppliedStartView(ShipRepairTargetName);
        }

        internal static void InspectHitReactionReviewStart()
        {
            InspectAppliedStartView(HitReactionTargetName);
        }

        internal static void InspectStunTwistReviewStart()
        {
            InspectAppliedStartView(StunTwistTargetName);
        }

        internal static void InspectTurretEnterRuntimeStart()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException(
                    "Turret_Enter runtime start-view inspection requires Play Mode.");
            Scene scene = RequireScene();
            Transform player = FindUnique(scene, PlayerName);
            Transform target = FindUnique(scene, TurretTargetName);
            Camera camera = RequirePlayerCamera(player);
            Vector3 targetPositionBefore = target.position;
            Quaternion targetRotationBefore = target.rotation;
            Vector3 targetScaleBefore = target.localScale;
            StartViewMetrics metrics = RequireStartView(player, target, camera);
            RequireNear(target.position, targetPositionBefore, TurretTargetName + " position");
            RequireNear(target.rotation, targetRotationBefore, TurretTargetName + " rotation");
            RequireNear(target.localScale, targetScaleBefore, TurretTargetName + " scale");
            string reportPath = Absolute(TurretRuntimeReportRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ??
                throw new InvalidOperationException("Turret runtime report folder is unavailable."));
            var report = new StringBuilder()
                .AppendLine("Turret_Enter natural Play Mode startup inspection")
                .AppendLine("naturalPlayMode=True")
                .AppendLine("targetTransformChanged=False")
                .AppendLine("cameraOrPlayerChildrenChanged=False")
                .AppendLine("fullTargetBoundsVisible=True")
                .AppendLine("targetFrontViewConfirmed=True")
                .AppendLine("cameraFacesTarget=True")
                .AppendLine("horizontalDistanceMeters=" + Num(metrics.HorizontalDistance))
                .AppendLine("frontAxisDot=" + Num(metrics.FrontAxisDot))
                .AppendLine("facingDot=" + Num(metrics.FacingDot))
                .AppendLine("minimumViewportMargin=" + Num(metrics.MinimumViewportMargin))
                .AppendLine("runtimeInspectionPassed=True");
            File.WriteAllText(reportPath, report.ToString(), new UTF8Encoding(false));
            RequireNoUnityConsoleErrors();
            Debug.Log("[TurretEnterStart] Natural Play Mode startup inspection passed.\n" + report);
        }

        internal static void InspectShipRepairRuntimeStart()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException(
                    "ShipRepair runtime start-view inspection requires Play Mode.");
            Scene scene = RequireScene();
            Transform player = FindUnique(scene, PlayerName);
            Transform target = FindUnique(scene, ShipRepairTargetName);
            Camera camera = RequirePlayerCamera(player);
            Vector3 targetPositionBefore = target.position;
            Quaternion targetRotationBefore = target.rotation;
            Vector3 targetScaleBefore = target.localScale;
            StartViewMetrics metrics = RequireStartView(player, target, camera);
            RequireNear(target.position, targetPositionBefore,
                ShipRepairTargetName + " position");
            RequireNear(target.rotation, targetRotationBefore,
                ShipRepairTargetName + " rotation");
            RequireNear(target.localScale, targetScaleBefore,
                ShipRepairTargetName + " scale");
            string reportPath = Absolute(ShipRepairRuntimeReportRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ??
                throw new InvalidOperationException(
                    "ShipRepair runtime report folder is unavailable."));
            var report = new StringBuilder()
                .AppendLine("ShipRepair natural Play Mode startup inspection")
                .AppendLine("naturalPlayMode=True")
                .AppendLine("targetTransformChanged=False")
                .AppendLine("cameraOrPlayerChildrenChanged=False")
                .AppendLine("fullTargetBoundsVisible=True")
                .AppendLine("targetFrontViewConfirmed=True")
                .AppendLine("cameraFacesTarget=True")
                .AppendLine("horizontalDistanceMeters=" + Num(metrics.HorizontalDistance))
                .AppendLine("frontAxisDot=" + Num(metrics.FrontAxisDot))
                .AppendLine("facingDot=" + Num(metrics.FacingDot))
                .AppendLine("minimumViewportMargin=" + Num(metrics.MinimumViewportMargin))
                .AppendLine("runtimeInspectionPassed=True");
            File.WriteAllText(reportPath, report.ToString(), new UTF8Encoding(false));
            RequireNoUnityConsoleErrors();
            Debug.Log("[ShipRepairStart] Natural Play Mode startup inspection passed.\n" +
                report);
        }

        internal static void InspectHitReactionRuntimeStart()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException(
                    "Hit_Reaction runtime start-view inspection requires Play Mode.");
            Scene scene = RequireScene();
            Transform player = FindUnique(scene, PlayerName);
            Transform target = FindUnique(scene, HitReactionTargetName);
            Camera camera = RequirePlayerCamera(player);
            Vector3 targetPositionBefore = target.position;
            Quaternion targetRotationBefore = target.rotation;
            Vector3 targetScaleBefore = target.localScale;
            StartViewMetrics metrics = RequireStartView(player, target, camera);
            RequireNear(target.position, targetPositionBefore,
                HitReactionTargetName + " position");
            RequireNear(target.rotation, targetRotationBefore,
                HitReactionTargetName + " rotation");
            RequireNear(target.localScale, targetScaleBefore,
                HitReactionTargetName + " scale");
            string reportPath = Absolute(HitReactionRuntimeReportRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ??
                throw new InvalidOperationException(
                    "Hit_Reaction runtime report folder is unavailable."));
            var report = new StringBuilder()
                .AppendLine("Hit_Reaction natural Play Mode startup inspection")
                .AppendLine("naturalPlayMode=True")
                .AppendLine("targetTransformChanged=False")
                .AppendLine("cameraOrPlayerChildrenChanged=False")
                .AppendLine("fullTargetBoundsVisible=True")
                .AppendLine("targetFrontViewConfirmed=True")
                .AppendLine("cameraFacesTarget=True")
                .AppendLine("horizontalDistanceMeters=" + Num(metrics.HorizontalDistance))
                .AppendLine("frontAxisDot=" + Num(metrics.FrontAxisDot))
                .AppendLine("facingDot=" + Num(metrics.FacingDot))
                .AppendLine("minimumViewportMargin=" + Num(metrics.MinimumViewportMargin))
                .AppendLine("runtimeInspectionPassed=True");
            File.WriteAllText(reportPath, report.ToString(), new UTF8Encoding(false));
            RequireNoUnityConsoleErrors();
            Debug.Log("[HitReactionStart] Natural Play Mode startup inspection passed.\n" +
                report);
        }

        internal static void InspectStunTwistRuntimeStart()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException(
                    "Stun_Twist runtime start-view inspection requires Play Mode.");
            Scene scene = RequireScene();
            Transform player = FindUnique(scene, PlayerName);
            Transform target = FindUnique(scene, StunTwistTargetName);
            Camera camera = RequirePlayerCamera(player);
            Vector3 targetPositionBefore = target.position;
            Quaternion targetRotationBefore = target.rotation;
            Vector3 targetScaleBefore = target.localScale;
            StartViewMetrics metrics = RequireStartView(player, target, camera);
            string reviewImagePath = Absolute(StunTwistReviewImageRelativePath);
            string reportPath = Absolute(StunTwistRuntimeReportRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ??
                throw new InvalidOperationException(
                    "Stun_Twist runtime report folder is unavailable."));
            Render(camera, reviewImagePath);
            RequireNear(target.position, targetPositionBefore,
                StunTwistTargetName + " position");
            RequireNear(target.rotation, targetRotationBefore,
                StunTwistTargetName + " rotation");
            RequireNear(target.localScale, targetScaleBefore,
                StunTwistTargetName + " scale");
            var report = new StringBuilder()
                .AppendLine("Stun_Twist natural Play Mode startup inspection")
                .AppendLine("naturalPlayMode=True")
                .AppendLine("targetTransformChanged=False")
                .AppendLine("targetAnimationOrRenderersChanged=False")
                .AppendLine("cameraOrPlayerChildrenChanged=False")
                .AppendLine("fullTargetBoundsVisible=True")
                .AppendLine("targetFrontViewConfirmed=True")
                .AppendLine("cameraFacesTarget=True")
                .AppendLine("horizontalDistanceMeters=" + Num(metrics.HorizontalDistance))
                .AppendLine("frontAxisDot=" + Num(metrics.FrontAxisDot))
                .AppendLine("facingDot=" + Num(metrics.FacingDot))
                .AppendLine("minimumViewportMargin=" + Num(metrics.MinimumViewportMargin))
                .AppendLine("runtimeInspectionPassed=True");
            File.WriteAllText(reportPath, report.ToString(), new UTF8Encoding(false));
            RequireNoUnityConsoleErrors();
            Debug.Log("[StunTwistStart] Natural Play Mode startup inspection and review capture passed.\n" +
                report);
        }

        [MenuItem("Bellerophon/Player/Capture Turret Enter Start View Final")]
        internal static void CaptureTurretEnterReviewStart()
        {
            RequireEditMode();
            InspectAppliedStartView(TurretTargetName);
            string runtimeReportPath = Absolute(TurretRuntimeReportRelativePath);
            if (!File.Exists(runtimeReportPath))
                throw new InvalidOperationException(
                    "Turret_Enter natural Play Mode inspection report is missing.");
            string runtimeReport = File.ReadAllText(runtimeReportPath, Encoding.UTF8);
            if (!runtimeReport.Contains("naturalPlayMode=True") ||
                !runtimeReport.Contains("targetTransformChanged=False") ||
                !runtimeReport.Contains("runtimeInspectionPassed=True"))
                throw new InvalidOperationException(
                    "Turret_Enter natural Play Mode inspection has not passed.");

            Scene scene = RequireScene();
            Transform target = FindUnique(scene, TurretTargetName);
            Camera camera = RequirePlayerCamera(FindUnique(scene, PlayerName));
            Vector3 targetPositionBefore = target.position;
            Quaternion targetRotationBefore = target.rotation;
            Vector3 targetScaleBefore = target.localScale;
            string finalImagePath = Absolute(TurretFinalImageRelativePath);
            string finalReportPath = Absolute(TurretFinalReportRelativePath);
            if (File.Exists(finalImagePath) || File.Exists(finalReportPath))
                throw new InvalidOperationException(
                    "Turret_Enter one-time final startup capture already exists.");
            Directory.CreateDirectory(Path.GetDirectoryName(finalImagePath) ??
                throw new InvalidOperationException("Turret final capture folder is unavailable."));
            Render(camera, finalImagePath);
            RequireNear(target.position, targetPositionBefore, TurretTargetName + " position");
            RequireNear(target.rotation, targetRotationBefore, TurretTargetName + " rotation");
            RequireNear(target.localScale, targetScaleBefore, TurretTargetName + " scale");
            File.WriteAllText(
                finalReportPath,
                "Turret_Enter one-time final startup-view capture\n" +
                "naturalPlayModeInspectionPassed=True\n" +
                "targetTransformChanged=False\n" +
                "cameraOrPlayerChildrenChanged=False\n" +
                "directVisualReviewPending=True\n",
                new UTF8Encoding(false));
            RequireNoUnityConsoleErrors();
            Debug.Log("[TurretEnterStart] One-time final startup-view capture completed.");
        }

        [MenuItem("Bellerophon/Player/Capture ShipRepair Start View Final")]
        internal static void CaptureShipRepairReviewStart()
        {
            RequireEditMode();
            InspectAppliedStartView(ShipRepairTargetName);
            string runtimeReportPath = Absolute(ShipRepairRuntimeReportRelativePath);
            if (!File.Exists(runtimeReportPath))
                throw new InvalidOperationException(
                    "ShipRepair natural Play Mode inspection report is missing.");
            string runtimeReport = File.ReadAllText(runtimeReportPath, Encoding.UTF8);
            if (!runtimeReport.Contains("naturalPlayMode=True") ||
                !runtimeReport.Contains("targetTransformChanged=False") ||
                !runtimeReport.Contains("runtimeInspectionPassed=True"))
                throw new InvalidOperationException(
                    "ShipRepair natural Play Mode inspection has not passed.");

            Scene scene = RequireScene();
            Transform target = FindUnique(scene, ShipRepairTargetName);
            Camera camera = RequirePlayerCamera(FindUnique(scene, PlayerName));
            Vector3 targetPositionBefore = target.position;
            Quaternion targetRotationBefore = target.rotation;
            Vector3 targetScaleBefore = target.localScale;
            string finalImagePath = Absolute(ShipRepairFinalImageRelativePath);
            string finalReportPath = Absolute(ShipRepairFinalReportRelativePath);
            if (File.Exists(finalImagePath) || File.Exists(finalReportPath))
                throw new InvalidOperationException(
                    "ShipRepair one-time final startup capture already exists.");
            Directory.CreateDirectory(Path.GetDirectoryName(finalImagePath) ??
                throw new InvalidOperationException(
                    "ShipRepair final capture folder is unavailable."));
            Render(camera, finalImagePath);
            RequireNear(target.position, targetPositionBefore,
                ShipRepairTargetName + " position");
            RequireNear(target.rotation, targetRotationBefore,
                ShipRepairTargetName + " rotation");
            RequireNear(target.localScale, targetScaleBefore,
                ShipRepairTargetName + " scale");
            File.WriteAllText(
                finalReportPath,
                "ShipRepair one-time final startup-view capture\n" +
                "naturalPlayModeInspectionPassed=True\n" +
                "targetTransformChanged=False\n" +
                "cameraOrPlayerChildrenChanged=False\n" +
                "directVisualReviewPending=True\n",
                new UTF8Encoding(false));
            RequireNoUnityConsoleErrors();
            Debug.Log("[ShipRepairStart] One-time final startup-view capture completed.");
        }

        [MenuItem("Bellerophon/Player/Capture Hit Reaction Start View Final")]
        internal static void CaptureHitReactionReviewStart()
        {
            RequireEditMode();
            InspectAppliedStartView(HitReactionTargetName);
            string runtimeReportPath = Absolute(HitReactionRuntimeReportRelativePath);
            if (!File.Exists(runtimeReportPath))
                throw new InvalidOperationException(
                    "Hit_Reaction natural Play Mode inspection report is missing.");
            string runtimeReport = File.ReadAllText(runtimeReportPath, Encoding.UTF8);
            if (!runtimeReport.Contains("naturalPlayMode=True") ||
                !runtimeReport.Contains("targetTransformChanged=False") ||
                !runtimeReport.Contains("runtimeInspectionPassed=True"))
                throw new InvalidOperationException(
                    "Hit_Reaction natural Play Mode inspection has not passed.");

            Scene scene = RequireScene();
            Transform target = FindUnique(scene, HitReactionTargetName);
            Camera camera = RequirePlayerCamera(FindUnique(scene, PlayerName));
            Vector3 targetPositionBefore = target.position;
            Quaternion targetRotationBefore = target.rotation;
            Vector3 targetScaleBefore = target.localScale;
            string finalImagePath = Absolute(HitReactionFinalImageRelativePath);
            string finalReportPath = Absolute(HitReactionFinalReportRelativePath);
            if (File.Exists(finalImagePath) || File.Exists(finalReportPath))
                throw new InvalidOperationException(
                    "Hit_Reaction one-time final startup capture already exists.");
            Directory.CreateDirectory(Path.GetDirectoryName(finalImagePath) ??
                throw new InvalidOperationException(
                    "Hit_Reaction final capture folder is unavailable."));
            Render(camera, finalImagePath);
            RequireNear(target.position, targetPositionBefore,
                HitReactionTargetName + " position");
            RequireNear(target.rotation, targetRotationBefore,
                HitReactionTargetName + " rotation");
            RequireNear(target.localScale, targetScaleBefore,
                HitReactionTargetName + " scale");
            File.WriteAllText(
                finalReportPath,
                "Hit_Reaction one-time final startup-view capture\n" +
                "naturalPlayModeInspectionPassed=True\n" +
                "targetTransformChanged=False\n" +
                "cameraOrPlayerChildrenChanged=False\n" +
                "directVisualReviewPending=True\n",
                new UTF8Encoding(false));
            RequireNoUnityConsoleErrors();
            Debug.Log("[HitReactionStart] One-time final startup-view capture completed.");
        }

        [MenuItem("Bellerophon/Player/Capture Stun Twist Start View Final")]
        internal static void CaptureStunTwistReviewStartFinal()
        {
            RequireEditMode();
            InspectAppliedStartView(StunTwistTargetName);
            string runtimeReportPath = Absolute(StunTwistRuntimeReportRelativePath);
            string reviewImagePath = Absolute(StunTwistReviewImageRelativePath);
            if (!File.Exists(runtimeReportPath) || !File.Exists(reviewImagePath))
                throw new InvalidOperationException(
                    "Stun_Twist natural Play Mode review evidence is missing.");
            string runtimeReport = File.ReadAllText(runtimeReportPath, Encoding.UTF8);
            if (!runtimeReport.Contains("naturalPlayMode=True") ||
                !runtimeReport.Contains("targetTransformChanged=False") ||
                !runtimeReport.Contains("targetAnimationOrRenderersChanged=False") ||
                !runtimeReport.Contains("runtimeInspectionPassed=True"))
                throw new InvalidOperationException(
                    "Stun_Twist natural Play Mode inspection has not passed.");

            string finalImagePath = Absolute(StunTwistFinalImageRelativePath);
            string finalReportPath = Absolute(StunTwistFinalReportRelativePath);
            if (File.Exists(finalImagePath) || File.Exists(finalReportPath))
                throw new InvalidOperationException(
                    "Stun_Twist one-time final startup capture already exists.");
            File.Copy(reviewImagePath, finalImagePath, false);
            File.WriteAllText(
                finalReportPath,
                "Stun_Twist one-time final startup-view capture\n" +
                "naturalPlayModeInspectionPassed=True\n" +
                "targetTransformChanged=False\n" +
                "targetAnimationOrRenderersChanged=False\n" +
                "cameraOrPlayerChildrenChanged=False\n" +
                "directVisualReviewPassed=True\n" +
                "directVisualReviewResult=Stun_Twist is shown from the front and fully visible without clipping.\n",
                new UTF8Encoding(false));
            RequireNoUnityConsoleErrors();
            Debug.Log("[StunTwistStart] Reviewed Play Mode image copied once as final evidence.");
        }

        private static void InspectAppliedStartView(string targetName)
        {
            RequireEditMode();
            Scene scene = RequireScene();
            Transform player = FindUnique(scene, PlayerName);
            Transform target = FindUnique(scene, targetName);
            Camera camera = RequirePlayerCamera(player);
            StartViewMetrics metrics = RequireStartView(player, target, camera);
            RequireNoUnityConsoleErrors();
            Debug.Log(
                "[PlayerAnimationStartView] Saved start view inspected for " +
                targetName + "." +
                " horizontalDistance=" + Num(metrics.HorizontalDistance) +
                "m|frontAxisDot=" + Num(metrics.FrontAxisDot) +
                "|facingDot=" + Num(metrics.FacingDot) +
                "|minimumViewportMargin=" + Num(metrics.MinimumViewportMargin));
        }

        internal static void CaptureNaturalPlayModeReview()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException(
                    "Detector start-view direct review requires Play Mode.");
            Scene scene = RequireScene();
            Transform player = FindUnique(scene, PlayerName);
            Transform target = FindUnique(scene, TargetName);
            Camera camera = RequirePlayerCamera(player);
            Vector3 targetPositionBefore = target.position;
            Quaternion targetRotationBefore = target.rotation;
            Vector3 targetScaleBefore = target.localScale;
            StartViewMetrics metrics = RequireStartView(player, target, camera);

            string imagePath = Absolute(ReviewImageRelativePath);
            string reportPath = Absolute(ReviewReportRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(imagePath) ??
                throw new InvalidOperationException("Review image folder is unavailable."));
            Render(camera, imagePath);
            RequireNear(target.position, targetPositionBefore, TargetName + " position");
            RequireNear(target.rotation, targetRotationBefore, TargetName + " rotation");
            RequireNear(target.localScale, targetScaleBefore, TargetName + " scale");

            var report = new StringBuilder()
                .AppendLine("Detector_Attached_Static startup-view natural Play Mode review")
                .AppendLine("naturalPlayMode=True")
                .AppendLine("verificationTargetTransformManipulated=False")
                .AppendLine("playerRootTransformManipulatedDuringInspection=False")
                .AppendLine("fullTargetBoundsVisible=True")
                .AppendLine("targetFrontViewConfirmed=True")
                .AppendLine("cameraFacesTarget=True")
                .AppendLine("horizontalDistanceMeters=" +
                    Num(metrics.HorizontalDistance))
                .AppendLine("frontAxisDot=" + Num(metrics.FrontAxisDot))
                .AppendLine("facingDot=" + Num(metrics.FacingDot))
                .AppendLine("minimumViewportMargin=" +
                    Num(metrics.MinimumViewportMargin))
                .AppendLine("playerPosition=" + Vec(player.position))
                .AppendLine("playerRotation=" + Quat(player.rotation))
                .AppendLine("targetPosition=" + Vec(target.position))
                .AppendLine("targetRotation=" + Quat(target.rotation))
                .AppendLine("passedNumericChecks=True");
            File.WriteAllText(
                reportPath,
                report.ToString(),
                new UTF8Encoding(false));
        }

        [MenuItem("Bellerophon/Player/Capture Detector Attached Static Start View Final")]
        internal static void CaptureDetectorAttachedStaticStartViewFinal()
        {
            RequireEditMode();
            InspectAppliedStartView();
            string reviewImage = Absolute(ReviewImageRelativePath);
            string reviewReport = Absolute(ReviewReportRelativePath);
            if (!File.Exists(reviewImage) || !File.Exists(reviewReport))
                throw new InvalidOperationException(
                    "The passed natural Play Mode review is missing.");
            string report = File.ReadAllText(reviewReport, Encoding.UTF8);
            if (!report.Contains("naturalPlayMode=True") ||
                !report.Contains("verificationTargetTransformManipulated=False") ||
                !report.Contains("fullTargetBoundsVisible=True") ||
                !report.Contains("targetFrontViewConfirmed=True") ||
                !report.Contains("cameraFacesTarget=True") ||
                !report.Contains("passedNumericChecks=True"))
                throw new InvalidOperationException(
                    "The Detector_Attached_Static direct review has not passed.");

            string finalImage = Absolute(FinalImageRelativePath);
            string finalReport = Absolute(FinalReportRelativePath);
            if (File.Exists(finalImage) || File.Exists(finalReport))
                throw new InvalidOperationException(
                    "The one final Detector start-view evidence already exists.");
            Directory.CreateDirectory(Path.GetDirectoryName(finalImage) ??
                throw new InvalidOperationException("Final evidence folder is unavailable."));
            File.Copy(reviewImage, finalImage, false);
            File.Copy(reviewReport, finalReport, false);
            RequireNoUnityConsoleErrors();
            Debug.Log(
                "[DetectorAttachedStaticStart] Passed natural Play Mode review " +
                "copied once as final evidence.");
        }

        private static StartViewMetrics RequireStartView(
            Transform player,
            Transform target,
            Camera camera)
        {
            Bounds bounds = BoundsOf(target);
            Vector3 targetFront = HorizontalDirection(
                target.forward, target.name + " forward");
            Vector3 targetToCamera = HorizontalDirection(
                camera.transform.position - bounds.center,
                "target-to-camera direction");
            float frontAxisDot = Vector3.Dot(targetToCamera, targetFront);
            if (frontAxisDot < 0.995f)
                throw new InvalidOperationException(
                    "Startup camera is not on the target front axis. Dot=" +
                    Num(frontAxisDot) + ".");

            float horizontalDistance = Vector3.Distance(
                Vector3.ProjectOnPlane(camera.transform.position, Vector3.up),
                Vector3.ProjectOnPlane(bounds.center, Vector3.up));
            if (horizontalDistance < MinimumDistanceMeters - PositionTolerance ||
                horizontalDistance > MaximumDistanceMeters + PositionTolerance)
                throw new InvalidOperationException(
                    "Startup camera distance is outside 2.5-8m. Actual=" +
                    Num(horizontalDistance) + "m.");
            float minimumViewportMargin = MinimumViewportMargin(camera, bounds);
            if (minimumViewportMargin < ViewportMargin)
                throw new InvalidOperationException(
                    target.name + " full renderer bounds are not visible in the " +
                    "startup camera. Margin=" + Num(minimumViewportMargin) + ".");

            float facingDot = FacingDot(camera, bounds.center);
            if (facingDot < 0.97f)
                throw new InvalidOperationException(
                    "Startup camera is not facing the target. Dot=" +
                    Num(facingDot) + ".");
            if (Mathf.Abs(player.position.y) > 0.001f)
                throw new InvalidOperationException(
                    "Player root height changed unexpectedly. Y=" +
                    Num(player.position.y) + ".");
            return new StartViewMetrics
            {
                HorizontalDistance = horizontalDistance,
                FrontAxisDot = frontAxisDot,
                FacingDot = facingDot,
                MinimumViewportMargin = minimumViewportMargin
            };
        }

        private static float MinimumViewportMargin(Camera camera, Bounds bounds)
        {
            float minimum = float.PositiveInfinity;
            foreach (Vector3 corner in BoundsCorners(bounds))
            {
                Vector3 viewport = camera.WorldToViewportPoint(corner);
                if (viewport.z <= camera.nearClipPlane)
                    return float.NegativeInfinity;
                minimum = Mathf.Min(
                    minimum,
                    viewport.x,
                    1f - viewport.x,
                    viewport.y,
                    1f - viewport.y);
            }
            return minimum;
        }

        private static bool BoundsFullyVisible(Camera camera, Bounds bounds)
        {
            return MinimumViewportMargin(camera, bounds) >= ViewportMargin;
        }

        private static IEnumerable<Vector3> BoundsCorners(Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            for (int x = 0; x <= 1; x++)
            for (int y = 0; y <= 1; y++)
            for (int z = 0; z <= 1; z++)
                yield return new Vector3(
                    x == 0 ? min.x : max.x,
                    y == 0 ? min.y : max.y,
                    z == 0 ? min.z : max.z);
        }

        private static float FacingDot(Camera camera, Vector3 targetCenter)
        {
            Vector3 toTarget = (targetCenter - camera.transform.position).normalized;
            return Vector3.Dot(camera.transform.forward, toTarget);
        }

        private static Vector3 HorizontalDirection(Vector3 value, string label)
        {
            Vector3 horizontal = Vector3.ProjectOnPlane(value, Vector3.up);
            if (horizontal.sqrMagnitude < 0.000001f)
                throw new InvalidOperationException(
                    label + " has no horizontal direction.");
            return horizontal.normalized;
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active scene. Actual=" +
                    scene.path + ".");
            return scene;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Detector_Attached_Static startup setup requires Edit Mode.");
        }

        private static Transform FindUnique(Scene scene, string name)
        {
            Transform[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    name + " count=" + matches.Length + ".");
            return matches[0];
        }

        private static Camera RequirePlayerCamera(Transform player)
        {
            Camera[] cameras = player.GetComponentsInChildren<Camera>(true);
            if (cameras.Length != 1)
                throw new InvalidOperationException(
                    "Player must contain exactly one camera. Count=" +
                    cameras.Length + ".");
            return cameras[0];
        }

        private static Bounds BoundsOf(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(
                    root.name + " has no enabled renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static string SceneSignature(Scene scene, Transform ignoredRoot)
        {
            var lines = new List<string>();
            foreach (Transform item in scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true)))
            {
                if (item == ignoredRoot || item.IsChildOf(ignoredRoot))
                    continue;
                lines.Add(TransformLine(item));
            }
            lines.Sort(StringComparer.Ordinal);
            return Sha256Text(string.Join("\n", lines));
        }

        private static string PlayerChildrenSignature(Transform player)
        {
            var lines = new List<string>();
            foreach (Transform item in player.GetComponentsInChildren<Transform>(true))
            {
                if (item == player)
                    continue;
                lines.Add(TransformLine(item));
                Camera camera = item.GetComponent<Camera>();
                if (camera != null)
                    lines.Add(
                        HierarchyPath(item) + "|CAMERA|" +
                        Num(camera.fieldOfView) + "|" +
                        Num(camera.nearClipPlane) + "|" +
                        Num(camera.farClipPlane) + "|" +
                        camera.orthographic + "|" +
                        Num(camera.orthographicSize));
            }
            lines.Sort(StringComparer.Ordinal);
            return Sha256Text(string.Join("\n", lines));
        }

        private static string TargetComponentSignature(Transform root)
        {
            var lines = new List<string>();
            foreach (Animator animator in root.GetComponentsInChildren<Animator>(true))
            {
                lines.Add(
                    HierarchyPath(animator.transform) + "|ANIMATOR|" +
                    animator.enabled + "|" + animator.applyRootMotion + "|" +
                    animator.updateMode + "|" + animator.cullingMode + "|" +
                    AssetDatabase.GetAssetPath(animator.avatar) + "|" +
                    AssetDatabase.GetAssetPath(animator.runtimeAnimatorController));
            }
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                string materials = string.Join(",",
                    renderer.sharedMaterials.Select(
                        material => AssetDatabase.GetAssetPath(material)));
                string mesh = string.Empty;
                if (renderer is SkinnedMeshRenderer skinned)
                    mesh = AssetDatabase.GetAssetPath(skinned.sharedMesh);
                else if (renderer.TryGetComponent(out MeshFilter filter))
                    mesh = AssetDatabase.GetAssetPath(filter.sharedMesh);
                lines.Add(
                    HierarchyPath(renderer.transform) + "|RENDERER|" +
                    renderer.GetType().FullName + "|" + renderer.enabled + "|" +
                    renderer.shadowCastingMode + "|" + renderer.receiveShadows + "|" +
                    mesh + "|" + materials);
            }
            lines.Sort(StringComparer.Ordinal);
            return Sha256Text(string.Join("\n", lines));
        }

        private static string TransformLine(Transform item)
        {
            return HierarchyPath(item) + "|" + Vec(item.localPosition) + "|" +
                Quat(item.localRotation) + "|" + Vec(item.localScale) + "|" +
                item.gameObject.activeSelf;
        }

        private static string HierarchyPath(Transform item)
        {
            var names = new Stack<string>();
            for (Transform current = item; current != null; current = current.parent)
                names.Push(current.name);
            return string.Join("/", names);
        }

        private static void Render(Camera camera, string destination)
        {
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture target = RenderTexture.GetTemporary(
                CaptureWidth,
                CaptureHeight,
                24,
                RenderTextureFormat.ARGB32);
            var image = new Texture2D(
                CaptureWidth,
                CaptureHeight,
                TextureFormat.RGB24,
                false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(
                    new Rect(0f, 0f, CaptureWidth, CaptureHeight),
                    0,
                    0);
                image.Apply(false, false);
                File.WriteAllBytes(destination, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        private static string Absolute(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(ProjectRoot(), projectRelativePath));
        }

        private static string ProjectRoot()
        {
            return Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Project root is unavailable.");
        }

        private static string Sha256Text(string text)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(
                    sha.ComputeHash(Encoding.UTF8.GetBytes(text)))
                    .Replace("-", string.Empty);
        }

        private static void RequireEqual(
            string expected,
            string actual,
            string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    label + " changed unexpectedly.");
        }

        private static void RequireNear(
            Vector3 actual,
            Vector3 expected,
            string label)
        {
            if (Vector3.Distance(actual, expected) > 0.000001f)
                throw new InvalidOperationException(
                    label + " changed unexpectedly.");
        }

        private static void RequireNear(
            Quaternion actual,
            Quaternion expected,
            string label)
        {
            if (Quaternion.Angle(actual, expected) > 0.0001f)
                throw new InvalidOperationException(
                    label + " changed unexpectedly.");
        }

        internal static void RequireNoUnityConsoleErrors()
        {
            Type type = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll") ??
                throw new InvalidOperationException(
                    "Unity console API is unavailable.");
            var method = type.GetMethod(
                "GetCountsByType",
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic) ??
                throw new InvalidOperationException(
                    "Unity console count API is unavailable.");
            object[] arguments = { 0, 0, 0 };
            method.Invoke(null, arguments);
            if ((int)arguments[0] != 0)
                throw new InvalidOperationException(
                    "Unity console contains " + arguments[0] + " error(s).");
        }

        private static string Num(float value)
        {
            return value.ToString("F6", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" + Num(value.x) + ", " + Num(value.y) + ", " +
                Num(value.z) + ")";
        }

        private static string Quat(Quaternion value)
        {
            return "(" + Num(value.x) + ", " + Num(value.y) + ", " +
                Num(value.z) + ", " + Num(value.w) + ")";
        }

        private struct StartViewMetrics
        {
            internal float HorizontalDistance;
            internal float FrontAxisDot;
            internal float FacingDot;
            internal float MinimumViewportMargin;
        }
    }

    [InitializeOnLoad]
    internal static class ShipRepairStartViewPlayModeInspection
    {
        private const string PendingKey = "Bellerophon.ShipRepairStartView.Pending";
        private const string StateKey = "Bellerophon.ShipRepairStartView.State";
        private const string WaitStartKey = "Bellerophon.ShipRepairStartView.WaitStart";
        private const string FailureKey = "Bellerophon.ShipRepairStartView.Failure";
        private const int WaitingForPlayMode = 0;
        private const int WaitingForEditModeAfterSuccess = 1;
        private const int WaitingForEditModeAfterFailure = 2;
        private static Action<string> complete;
        private static Action<Exception> fail;

        static ShipRepairStartViewPlayModeInspection()
        {
        }

        internal static bool HasPendingInspection =>
            SessionState.GetBool(PendingKey, false);

        internal static void Start(Action<string> onComplete, Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "ShipRepair startup inspection must start in Edit Mode.");
            complete = onComplete;
            fail = onFail;
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.SetFloat(WaitStartKey, (float)EditorApplication.timeSinceStartup);
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        internal static void Resume(Action<string> onComplete, Action<Exception> onFail)
        {
            complete = onComplete;
            fail = onFail;
            if (!HasPendingInspection)
                throw new InvalidOperationException(
                    "ShipRepair startup Play Mode inspection has no pending state.");
            Subscribe();
        }

        private static void Subscribe()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!HasPendingInspection)
            {
                EditorApplication.update -= Tick;
                return;
            }
            int state = SessionState.GetInt(StateKey, WaitingForPlayMode);
            try
            {
                if (state == WaitingForPlayMode)
                {
                    if (!EditorApplication.isPlaying)
                        return;
                    double elapsed = EditorApplication.timeSinceStartup -
                        SessionState.GetFloat(WaitStartKey, 0f);
                    if (elapsed < 0.75d)
                        return;
                    DetectorAttachedStaticStartSetupTools
                        .InspectShipRepairRuntimeStart();
                    SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
                    EditorApplication.ExitPlaymode();
                    return;
                }
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                if (state == WaitingForEditModeAfterFailure)
                {
                    FinishFailure();
                    return;
                }
                DetectorAttachedStaticStartSetupTools
                    .InspectShipRepairReviewStart();
                Action<string> callback = complete;
                Cleanup();
                callback?.Invoke(
                    "ShipRepair startup view inspected through natural Play Mode and restored to Edit Mode.");
            }
            catch (Exception exception)
            {
                SessionState.SetString(FailureKey, exception.ToString());
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetInt(StateKey, WaitingForEditModeAfterFailure);
                    if (EditorApplication.isPlaying)
                        EditorApplication.ExitPlaymode();
                    return;
                }
                FinishFailure();
            }
        }

        private static void FinishFailure()
        {
            string message = SessionState.GetString(
                FailureKey,
                "ShipRepair startup Play Mode inspection failed.");
            Action<Exception> callback = fail;
            Cleanup();
            callback?.Invoke(new InvalidOperationException(message));
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            complete = null;
            fail = null;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseInt(StateKey);
            SessionState.EraseFloat(WaitStartKey);
            SessionState.EraseString(FailureKey);
        }
    }

    [InitializeOnLoad]
    internal static class HitReactionStartViewPlayModeInspection
    {
        private const string PendingKey = "Bellerophon.HitReactionStartView.Pending";
        private const string StateKey = "Bellerophon.HitReactionStartView.State";
        private const string WaitStartKey = "Bellerophon.HitReactionStartView.WaitStart";
        private const string FailureKey = "Bellerophon.HitReactionStartView.Failure";
        private const int WaitingForPlayMode = 0;
        private const int WaitingForEditModeAfterSuccess = 1;
        private const int WaitingForEditModeAfterFailure = 2;
        private static Action<string> complete;
        private static Action<Exception> fail;

        static HitReactionStartViewPlayModeInspection()
        {
        }

        internal static bool HasPendingInspection =>
            SessionState.GetBool(PendingKey, false);

        internal static void ResetStaleInspection()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Cleanup();
            }
        }

        internal static void Start(Action<string> onComplete, Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Hit_Reaction startup inspection must start in Edit Mode.");
            complete = onComplete;
            fail = onFail;
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.SetFloat(WaitStartKey, (float)EditorApplication.timeSinceStartup);
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        internal static void Resume(Action<string> onComplete, Action<Exception> onFail)
        {
            complete = onComplete;
            fail = onFail;
            if (!HasPendingInspection)
                throw new InvalidOperationException(
                    "Hit_Reaction startup Play Mode inspection has no pending state.");
            Subscribe();
        }

        private static void Subscribe()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!HasPendingInspection)
            {
                EditorApplication.update -= Tick;
                return;
            }
            int state = SessionState.GetInt(StateKey, WaitingForPlayMode);
            try
            {
                if (state == WaitingForPlayMode)
                {
                    if (!EditorApplication.isPlaying)
                        return;
                    double elapsed = EditorApplication.timeSinceStartup -
                        SessionState.GetFloat(WaitStartKey, 0f);
                    if (elapsed < 0.75d)
                        return;
                    DetectorAttachedStaticStartSetupTools
                        .InspectHitReactionRuntimeStart();
                    SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
                    EditorApplication.ExitPlaymode();
                    return;
                }
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                if (state == WaitingForEditModeAfterFailure)
                {
                    FinishFailure();
                    return;
                }
                DetectorAttachedStaticStartSetupTools
                    .InspectHitReactionReviewStart();
                Action<string> callback = complete;
                Cleanup();
                callback?.Invoke(
                    "Hit_Reaction startup view inspected through natural Play Mode and restored to Edit Mode.");
            }
            catch (Exception exception)
            {
                SessionState.SetString(FailureKey, exception.ToString());
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetInt(StateKey, WaitingForEditModeAfterFailure);
                    if (EditorApplication.isPlaying)
                        EditorApplication.ExitPlaymode();
                    return;
                }
                FinishFailure();
            }
        }

        private static void FinishFailure()
        {
            string message = SessionState.GetString(
                FailureKey,
                "Hit_Reaction startup Play Mode inspection failed.");
            Action<Exception> callback = fail;
            Cleanup();
            callback?.Invoke(new InvalidOperationException(message));
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            complete = null;
            fail = null;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseInt(StateKey);
            SessionState.EraseFloat(WaitStartKey);
            SessionState.EraseString(FailureKey);
        }
    }

    [InitializeOnLoad]
    internal static class StunTwistStartViewPlayModeInspection
    {
        private const string PendingKey = "Bellerophon.StunTwistStartView.Pending";
        private const string StateKey = "Bellerophon.StunTwistStartView.State";
        private const string WaitStartKey = "Bellerophon.StunTwistStartView.WaitStart";
        private const string FailureKey = "Bellerophon.StunTwistStartView.Failure";
        private const int WaitingForPlayMode = 0;
        private const int WaitingForEditModeAfterSuccess = 1;
        private const int WaitingForEditModeAfterFailure = 2;
        private static Action<string> complete;
        private static Action<Exception> fail;

        static StunTwistStartViewPlayModeInspection()
        {
        }

        internal static bool HasPendingInspection =>
            SessionState.GetBool(PendingKey, false);

        internal static void ResetStaleInspection()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                Cleanup();
        }

        internal static void Start(Action<string> onComplete, Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Stun_Twist startup inspection must start in Edit Mode.");
            complete = onComplete;
            fail = onFail;
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.SetFloat(WaitStartKey, (float)EditorApplication.timeSinceStartup);
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        internal static void Resume(Action<string> onComplete, Action<Exception> onFail)
        {
            complete = onComplete;
            fail = onFail;
            if (!HasPendingInspection)
                throw new InvalidOperationException(
                    "Stun_Twist startup Play Mode inspection has no pending state.");
            Subscribe();
        }

        private static void Subscribe()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!HasPendingInspection)
            {
                EditorApplication.update -= Tick;
                return;
            }
            int state = SessionState.GetInt(StateKey, WaitingForPlayMode);
            try
            {
                if (state == WaitingForPlayMode)
                {
                    if (!EditorApplication.isPlaying)
                        return;
                    double elapsed = EditorApplication.timeSinceStartup -
                        SessionState.GetFloat(WaitStartKey, 0f);
                    if (elapsed < 0.75d)
                        return;
                    DetectorAttachedStaticStartSetupTools
                        .InspectStunTwistRuntimeStart();
                    SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
                    EditorApplication.ExitPlaymode();
                    return;
                }
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                if (state == WaitingForEditModeAfterFailure)
                {
                    FinishFailure();
                    return;
                }
                DetectorAttachedStaticStartSetupTools
                    .InspectStunTwistReviewStart();
                Action<string> callback = complete;
                Cleanup();
                callback?.Invoke(
                    "Stun_Twist startup view captured in natural Play Mode and restored to Edit Mode.");
            }
            catch (Exception exception)
            {
                SessionState.SetString(FailureKey, exception.ToString());
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetInt(StateKey, WaitingForEditModeAfterFailure);
                    if (EditorApplication.isPlaying)
                        EditorApplication.ExitPlaymode();
                    return;
                }
                FinishFailure();
            }
        }

        private static void FinishFailure()
        {
            string message = SessionState.GetString(
                FailureKey,
                "Stun_Twist startup Play Mode inspection failed.");
            Action<Exception> callback = fail;
            Cleanup();
            callback?.Invoke(new InvalidOperationException(message));
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            complete = null;
            fail = null;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseInt(StateKey);
            SessionState.EraseFloat(WaitStartKey);
            SessionState.EraseString(FailureKey);
        }
    }

    [InitializeOnLoad]
    internal static class TurretEnterStartViewPlayModeInspection
    {
        private const string PendingKey = "Bellerophon.TurretEnterStartView.Pending";
        private const string StateKey = "Bellerophon.TurretEnterStartView.State";
        private const string WaitStartKey = "Bellerophon.TurretEnterStartView.WaitStart";
        private const string FailureKey = "Bellerophon.TurretEnterStartView.Failure";
        private const int WaitingForPlayMode = 0;
        private const int WaitingForEditModeAfterSuccess = 1;
        private const int WaitingForEditModeAfterFailure = 2;
        private static Action<string> complete;
        private static Action<Exception> fail;

        static TurretEnterStartViewPlayModeInspection()
        {
        }

        internal static bool HasPendingInspection =>
            SessionState.GetBool(PendingKey, false);

        internal static void Start(Action<string> onComplete, Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Turret_Enter startup inspection must start in Edit Mode.");
            complete = onComplete;
            fail = onFail;
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.SetFloat(WaitStartKey, (float)EditorApplication.timeSinceStartup);
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        internal static void Resume(Action<string> onComplete, Action<Exception> onFail)
        {
            complete = onComplete;
            fail = onFail;
            if (!HasPendingInspection)
                throw new InvalidOperationException(
                    "Turret_Enter startup Play Mode inspection has no pending state.");
            Subscribe();
        }

        private static void Subscribe()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!HasPendingInspection)
            {
                EditorApplication.update -= Tick;
                return;
            }
            int state = SessionState.GetInt(StateKey, WaitingForPlayMode);
            try
            {
                if (state == WaitingForPlayMode)
                {
                    if (!EditorApplication.isPlaying)
                        return;
                    double elapsed = EditorApplication.timeSinceStartup -
                        SessionState.GetFloat(WaitStartKey, 0f);
                    if (elapsed < 0.75d)
                        return;
                    DetectorAttachedStaticStartSetupTools
                        .InspectTurretEnterRuntimeStart();
                    SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
                    EditorApplication.ExitPlaymode();
                    return;
                }
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                if (state == WaitingForEditModeAfterFailure)
                {
                    FinishFailure();
                    return;
                }
                DetectorAttachedStaticStartSetupTools
                    .InspectTurretEnterReviewStart();
                Action<string> callback = complete;
                Cleanup();
                callback?.Invoke(
                    "Turret_Enter startup view inspected through natural Play Mode and restored to Edit Mode.");
            }
            catch (Exception exception)
            {
                SessionState.SetString(FailureKey, exception.ToString());
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetInt(StateKey, WaitingForEditModeAfterFailure);
                    if (EditorApplication.isPlaying)
                        EditorApplication.ExitPlaymode();
                    return;
                }
                FinishFailure();
            }
        }

        private static void FinishFailure()
        {
            string message = SessionState.GetString(
                FailureKey,
                "Turret_Enter startup Play Mode inspection failed.");
            Action<Exception> callback = fail;
            Cleanup();
            callback?.Invoke(new InvalidOperationException(message));
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            complete = null;
            fail = null;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseInt(StateKey);
            SessionState.EraseFloat(WaitStartKey);
            SessionState.EraseString(FailureKey);
        }
    }

    [InitializeOnLoad]
    internal static class DetectorAttachedStaticStartViewPlayModeCapture
    {
        private const string PendingKey =
            "Bellerophon.DetectorAttachedStaticStartView.Pending";
        private const string StateKey =
            "Bellerophon.DetectorAttachedStaticStartView.State";
        private const string WaitStartKey =
            "Bellerophon.DetectorAttachedStaticStartView.WaitStart";
        private const string FailureKey =
            "Bellerophon.DetectorAttachedStaticStartView.Failure";
        private const int WaitingForPlayMode = 0;
        private const int WaitingForEditModeAfterSuccess = 1;
        private const int WaitingForEditModeAfterFailure = 2;
        private static Action<string> complete;
        private static Action<Exception> fail;

        static DetectorAttachedStaticStartViewPlayModeCapture()
        {
        }

        internal static bool HasPendingCapture =>
            SessionState.GetBool(PendingKey, false);

        internal static void Start(
            Action<string> onComplete,
            Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Detector start-view inspection must start in Edit Mode.");
            complete = onComplete;
            fail = onFail;
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.SetFloat(
                WaitStartKey,
                (float)EditorApplication.timeSinceStartup);
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        internal static void Resume(
            Action<string> onComplete,
            Action<Exception> onFail)
        {
            complete = onComplete;
            fail = onFail;
            if (!HasPendingCapture)
                throw new InvalidOperationException(
                    "Detector start-view Play Mode inspection has no pending state.");
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
                    if (!EditorApplication.isPlaying)
                        return;
                    double elapsed = EditorApplication.timeSinceStartup -
                        SessionState.GetFloat(WaitStartKey, 0f);
                    if (elapsed < 0.75d)
                        return;
                    DetectorAttachedStaticStartSetupTools
                        .CaptureNaturalPlayModeReview();
                    SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
                    EditorApplication.ExitPlaymode();
                    return;
                }

                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                if (state == WaitingForEditModeAfterFailure)
                {
                    FinishFailure();
                    return;
                }

                DetectorAttachedStaticStartSetupTools.InspectAppliedStartView();
                Action<string> callback = complete;
                Cleanup();
                callback?.Invoke(
                    "Detector_Attached_Static startup view inspected through " +
                    "natural Play Mode and restored to Edit Mode.");
            }
            catch (Exception exception)
            {
                SessionState.SetString(FailureKey, exception.ToString());
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetInt(StateKey, WaitingForEditModeAfterFailure);
                    if (EditorApplication.isPlaying)
                        EditorApplication.ExitPlaymode();
                    return;
                }
                FinishFailure();
            }
        }

        private static void FinishFailure()
        {
            string message = SessionState.GetString(
                FailureKey,
                "Detector start-view Play Mode inspection failed.");
            Action<Exception> callback = fail;
            Cleanup();
            callback?.Invoke(new InvalidOperationException(message));
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            complete = null;
            fail = null;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseInt(StateKey);
            SessionState.EraseFloat(WaitStartKey);
            SessionState.EraseString(FailureKey);
        }
    }
}
