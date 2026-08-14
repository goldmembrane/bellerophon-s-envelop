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

namespace Bellerophon.Editor.ParvumCargoRunScene
{
    internal static class ParvumGlbReplacementTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SourceModelPath = "D:/Bellerophon2/Bellerophon/enemies model/parvum.glb";
        private const string ImportedModelPath = "Assets/_Project/Art/Enemies/Parvum/Models/parvum.glb";
        private const string ExpectedSourceSha256 = "E27840896F1DFA15BEE6F45F2BA943D28375A485E141907283CF79446B5640AB";
        private const string ParvumRootName = "Approved Parvum Enemy Placement";
        private const string IspantRootName = "Approved Ispant Enemy Placement";
        private const string TergoRootName = "Approved Tergo Enemy Placement";
        private const string StaticTergoSlotName = "Tergo_00_Static_Review";
        private const string PlayerName = "Player";
        private const string ReplacementModelName = "Parvum_Model";
        private const string OutputFolder = "docs/validation/parvum_model_replacement_2026-08-14";
        private const string ReportPath = OutputFolder + "/Parvum_Model_Replacement_Report.txt";
        private const string CapturePath = OutputFolder + "/Parvum_Model_Replacement_Final.png";
        private const string AlignmentReportPath = OutputFolder + "/Parvum_Static_Tergo_VisibleGap_And_Y_Report.txt";
        private const string AlignmentCapturePath = OutputFolder + "/Parvum_Static_Tergo_VisibleGap_And_Y_Final.png";
        private const float SpacingTolerance = 0.01f;
        private const float MinimumPlayerDistance = 2.5f;
        private const float CameraMargin = 0.8f;
        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;

        private static readonly string[] ParvumSlotNames =
        {
            "Parvum_00_Static",
            "Parvum_01_Idle",
            "Parvum_02_Move",
            "Parvum_03_Attack",
            "Parvum_04_Hit",
            "Parvum_05_Death",
        };

        [MenuItem("Bellerophon/Enemies/Parvum/Apply Supplied GLB Replacement")]
        public static void ApplyParvumGlbReplacement()
        {
            var scene = RequireCurrentScene();
            RequireSourceAndImportedModel();
            var importedModel = AssetDatabase.LoadAssetAtPath<GameObject>(ImportedModelPath) ??
                                throw new InvalidOperationException(
                                    "The supplied Parvum GLB was not imported as a GameObject asset.");
            RequireVisibleGeometry(importedModel.transform);

            var parvumRoot = RequireRoot(ParvumRootName).transform;
            var ispantRoot = RequireRoot(IspantRootName).transform;
            var parvumSlots = RequireParvumSlots(parvumRoot);
            if (scene.isDirty && !IsRecognizedPartialReplacement(parvumSlots))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes that are not the recognized incomplete Parvum GLB replacement.");
            }

            var ispantSpacing = MeasureUniformIspantSpacing(ispantRoot);
            var protectedBefore = ProtectedRootSignatures(scene);
            var ispantBefore = HierarchySignature(ispantRoot);

            var orderedSlots = parvumSlots.OrderBy(slot => slot.position.x).ToArray();
            var centerX = orderedSlots.Average(slot => slot.position.x);
            for (var index = 0; index < orderedSlots.Length; index++)
            {
                var slot = orderedSlots[index];
                var groundY = slot.position.y;
                slot.localScale = Vector3.one;
                ReplaceVisibleModel(
                    slot,
                    importedModel,
                    scene,
                    groundY);

                var position = slot.position;
                position.x = centerX +
                             (index - (orderedSlots.Length - 1) * 0.5f) *
                             ispantSpacing;
                slot.position = position;
                EditorUtility.SetDirty(slot);
            }

            ConfigurePlayer(parvumRoot);
            var result = InspectState(parvumRoot, ispantRoot, ispantSpacing);

            if (!string.Equals(ispantBefore, HierarchySignature(ispantRoot), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The Ispant hierarchy changed during Parvum replacement.");
            }

            var protectedAfter = ProtectedRootSignatures(scene);
            if (!protectedBefore.SequenceEqual(protectedAfter, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "A scene root outside Parvum and Player changed during replacement.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after replacing Parvum.");
            }

            AssetDatabase.SaveAssets();
            RequireSameHash(ExpectedSourceSha256, Sha256(SourceModelPath), "source GLB");
            RequireSameHash(ExpectedSourceSha256, Sha256(Absolute(ImportedModelPath)), "imported GLB copy");
            WriteReport(result, captureCreated: false);

            Debug.Log(
                "ParvumGlbReplacementApplied Result=PASS" +
                ", SourceSha256=" + ExpectedSourceSha256 +
                ", Slots=" + result.SlotCount.ToString(CultureInfo.InvariantCulture) +
                ", IspantXSpacing=" + Num(result.Spacing) +
                ", PlayerFacesParvum=True" +
                ", IspantChanged=False" +
                ", OtherSceneRootsChanged=False" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Parvum/Capture Supplied GLB Replacement")]
        public static void CaptureParvumGlbReplacement()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be saved before the final Parvum capture.");
            }

            RequireSourceAndImportedModel();
            var parvumRoot = RequireRoot(ParvumRootName).transform;
            var ispantRoot = RequireRoot(IspantRootName).transform;
            var spacing = MeasureUniformIspantSpacing(ispantRoot);
            var result = InspectState(parvumRoot, ispantRoot, spacing);
            var camera = RequirePlayer().GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            Capture(camera, Absolute(CapturePath));
            WriteReport(result, captureCreated: true);
            AssetDatabase.Refresh();

            Debug.Log(
                "ParvumGlbReplacementCaptured Result=PASS" +
                ", Image=" + CapturePath +
                ", Slots=" + result.SlotCount.ToString(CultureInfo.InvariantCulture) +
                ", IspantXSpacing=" + Num(result.Spacing) +
                ", CompleteLineupVisible=True" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Parvum/Apply Tergo Visible Gap And Ground Alignment")]
        public static void ApplyParvumTergoVisibleGapAndYAlignment()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes; Parvum alignment was not changed.");
            }

            var parvumRoot = RequireRoot(ParvumRootName).transform;
            var tergoRoot = RequireRoot(TergoRootName).transform;
            var reference = MeasureTergoVisibleGapAndGround(tergoRoot);
            var protectedBefore = ProtectedRootSignatures(scene);
            var tergoBefore = HierarchySignature(tergoRoot);
            var slots = RequireCurrentParvumModels(parvumRoot)
                .OrderBy(slot => ActiveBoundsOf(slot, new Bounds(slot.position, Vector3.one)).center.x)
                .ToArray();
            var originalLineupCenterX = ActiveBoundsOf(
                parvumRoot,
                new Bounds(parvumRoot.position, Vector3.one)).center.x;

            foreach (var slot in slots)
            {
                var bounds = ActiveBoundsOf(slot, new Bounds(slot.position, Vector3.one));
                slot.position += Vector3.up * (reference.GroundY - bounds.min.y);
                EditorUtility.SetDirty(slot);
            }

            var alignedBounds = slots
                .Select(slot => ActiveBoundsOf(slot, new Bounds(slot.position, Vector3.one)))
                .ToArray();
            var lineupWidth = alignedBounds.Sum(bounds => bounds.size.x) +
                              reference.VisibleGap * (slots.Length - 1);
            var nextLeftEdge = originalLineupCenterX - lineupWidth * 0.5f;
            for (var index = 0; index < slots.Length; index++)
            {
                var desiredCenterX = nextLeftEdge + alignedBounds[index].extents.x;
                var position = slots[index].position;
                position.x += desiredCenterX - alignedBounds[index].center.x;
                slots[index].position = position;
                nextLeftEdge += alignedBounds[index].size.x + reference.VisibleGap;
                EditorUtility.SetDirty(slots[index]);
            }

            ConfigurePlayer(parvumRoot);
            var result = InspectVisibleGapAndYAlignment(parvumRoot, tergoRoot, reference);
            if (!string.Equals(tergoBefore, HierarchySignature(tergoRoot), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The Tergo hierarchy changed during Parvum alignment.");
            }

            var protectedAfter = ProtectedRootSignatures(scene);
            if (!protectedBefore.SequenceEqual(protectedAfter, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "A scene root outside Parvum and Player changed during alignment.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after aligning Parvum.");
            }

            WriteAlignmentReport(result, captureCreated: false);
            Debug.Log(
                "ParvumVisibleGapAndYAlignmentApplied Result=PASS" +
                ", Slots=" + result.SlotCount.ToString(CultureInfo.InvariantCulture) +
                ", TergoCenterSpacing=" + Num(result.CenterSpacing) +
                ", StaticTergoRendererWidth=" + Num(result.StaticWidth) +
                ", TergoVisibleGap=" + Num(result.VisibleGap) +
                ", TergoGroundY=" + Num(result.GroundY) +
                ", PlayerFacesParvum=True" +
                ", TergoChanged=False" +
                ", OtherSceneRootsChanged=False" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Parvum/Capture Tergo Visible Gap And Ground Alignment")]
        public static void CaptureParvumTergoVisibleGapAndYAlignment()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be saved before the final Parvum alignment capture.");
            }

            var parvumRoot = RequireRoot(ParvumRootName).transform;
            var tergoRoot = RequireRoot(TergoRootName).transform;
            var reference = MeasureTergoVisibleGapAndGround(tergoRoot);
            var result = InspectVisibleGapAndYAlignment(parvumRoot, tergoRoot, reference);
            var camera = RequirePlayer().GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            Capture(camera, Absolute(AlignmentCapturePath));
            WriteAlignmentReport(result, captureCreated: true);
            AssetDatabase.Refresh();

            Debug.Log(
                "ParvumVisibleGapAndYAlignmentCaptured Result=PASS" +
                ", Image=" + AlignmentCapturePath +
                ", Slots=" + result.SlotCount.ToString(CultureInfo.InvariantCulture) +
                ", VisibleGap=" + Num(result.VisibleGap) +
                ", GroundY=" + Num(result.GroundY) +
                ", CompleteLineupVisible=True" +
                ", SceneChanged=False.");
        }

        private static void RequireSourceAndImportedModel()
        {
            if (!File.Exists(SourceModelPath))
            {
                throw new InvalidOperationException("Missing supplied Parvum GLB: " + SourceModelPath);
            }

            if (!File.Exists(Absolute(ImportedModelPath)))
            {
                throw new InvalidOperationException("Missing imported Parvum GLB copy: " + ImportedModelPath);
            }

            RequireSameHash(ExpectedSourceSha256, Sha256(SourceModelPath), "source GLB");
            RequireSameHash(ExpectedSourceSha256, Sha256(Absolute(ImportedModelPath)), "imported GLB copy");
            AssetDatabase.ImportAsset(
                ImportedModelPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static Transform[] RequireParvumSlots(Transform root)
        {
            if (root.childCount != ParvumSlotNames.Length)
            {
                throw new InvalidOperationException(
                    "The current Parvum placement must contain exactly " +
                    ParvumSlotNames.Length.ToString(CultureInfo.InvariantCulture) +
                    " direct slots.");
            }

            var slots = new Transform[ParvumSlotNames.Length];
            for (var index = 0; index < ParvumSlotNames.Length; index++)
            {
                slots[index] = root.Find(ParvumSlotNames[index]) ??
                               throw new InvalidOperationException(
                                   "Missing current Parvum slot: " + ParvumSlotNames[index]);
            }

            return slots;
        }

        private static bool IsRecognizedPartialReplacement(Transform[] slots)
        {
            return slots.All(slot =>
            {
                var visibleChildren = slot.Cast<Transform>()
                    .Where(child => child.GetComponentsInChildren<Renderer>(true).Length > 0)
                    .ToArray();
                return visibleChildren.Length == 1 &&
                       visibleChildren[0].name == ReplacementModelName;
            });
        }

        private static float MeasureUniformIspantSpacing(Transform root)
        {
            var slots = root.Cast<Transform>()
                .Where(slot => slot.name.StartsWith("Ispant_", StringComparison.Ordinal))
                .OrderBy(slot => slot.position.x)
                .ToArray();
            if (slots.Length < 2)
            {
                throw new InvalidOperationException("Ispant needs at least two slots for X spacing.");
            }

            var spacings = new List<float>();
            for (var index = 1; index < slots.Length; index++)
            {
                var spacing = Mathf.Abs(slots[index].position.x - slots[index - 1].position.x);
                if (spacing <= 0.1f)
                {
                    throw new InvalidOperationException("The current Ispant X spacing is unusable.");
                }

                spacings.Add(spacing);
            }

            var reference = spacings[0];
            if (spacings.Any(spacing => Mathf.Abs(spacing - reference) > SpacingTolerance))
            {
                throw new InvalidOperationException(
                    "The current Ispant X spacing is not uniform; Parvum spacing was not changed.");
            }

            return reference;
        }

        private static AlignmentReference MeasureTergoVisibleGapAndGround(Transform root)
        {
            var slots = root.Cast<Transform>()
                .Where(slot => slot.name.StartsWith("Tergo_", StringComparison.Ordinal) &&
                               !slot.name.Contains("_Approved_", StringComparison.Ordinal) &&
                               slot.gameObject.activeInHierarchy)
                .OrderBy(slot => slot.position.x)
                .ToArray();
            if (slots.Length < 2)
            {
                throw new InvalidOperationException(
                    "Tergo needs at least two active primary slots for center-spacing measurement.");
            }

            var spacings = new List<float>();
            for (var index = 1; index < slots.Length; index++)
            {
                var spacing = Mathf.Abs(slots[index].position.x - slots[index - 1].position.x);
                if (spacing <= 0.1f)
                {
                    throw new InvalidOperationException(
                        "The current Tergo primary-slot center spacing is unusable between " +
                        slots[index - 1].name + " and " + slots[index].name + ".");
                }

                spacings.Add(spacing);
            }

            var referenceSpacing = spacings[0];
            if (spacings.Any(spacing => Mathf.Abs(spacing - referenceSpacing) > SpacingTolerance))
            {
                throw new InvalidOperationException(
                    "The current Tergo primary-slot center spacing is not uniform; Parvum was not changed.");
            }

            var staticSlot = root.Find(StaticTergoSlotName) ??
                             throw new InvalidOperationException("Missing static Tergo slot: " + StaticTergoSlotName + ".");
            if (!staticSlot.gameObject.activeInHierarchy)
            {
                throw new InvalidOperationException(
                    "The static Tergo reference slot is not active: " + StaticTergoSlotName + ".");
            }

            var staticBounds = ActiveBoundsOf(
                staticSlot,
                new Bounds(staticSlot.position, Vector3.one));
            var visibleGap = referenceSpacing - staticBounds.size.x;
            if (visibleGap <= SpacingTolerance)
            {
                throw new InvalidOperationException(
                    "The static Tergo reference produces no positive renderer-bound X gap. " +
                    "CenterSpacing=" + Num(referenceSpacing) +
                    ", StaticRendererWidth=" + Num(staticBounds.size.x) + ".");
            }

            // The approved reference treats each Tergo slot as if it used the static model bounds.
            return new AlignmentReference(
                visibleGap,
                staticBounds.min.y,
                referenceSpacing,
                staticBounds.size.x);
        }

        private static Transform[] RequireCurrentParvumModels(Transform root)
        {
            var slots = RequireParvumSlots(root);
            foreach (var slot in slots)
            {
                var models = slot.Cast<Transform>()
                    .Where(child => child.GetComponentsInChildren<Renderer>(true).Length > 0)
                    .ToArray();
                if (models.Length != 1 || models[0].name != ReplacementModelName)
                {
                    throw new InvalidOperationException(
                        slot.name + " must contain exactly one supplied Parvum GLB model child.");
                }

                RequireVisibleGeometry(models[0]);
            }

            return slots;
        }

        private static void ReplaceVisibleModel(
            Transform slot,
            GameObject importedModel,
            Scene scene,
            float targetGroundY)
        {
            var visibleChildren = slot.Cast<Transform>()
                .Where(child => child.GetComponentsInChildren<Renderer>(true).Length > 0)
                .ToArray();
            if (visibleChildren.Length == 0)
            {
                throw new InvalidOperationException(slot.name + " has no existing visible model child.");
            }

            foreach (var child in visibleChildren)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }

            var instance = PrefabUtility.InstantiatePrefab(importedModel, scene) as GameObject ??
                           throw new InvalidOperationException(
                               "The supplied Parvum GLB could not be instantiated for " + slot.name + ".");
            instance.name = ReplacementModelName;
            instance.transform.SetParent(slot, false);
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;

            var bounds = BoundsOf(instance.transform, new Bounds(instance.transform.position, Vector3.one));
            if (bounds.size.y <= 0.00001f)
            {
                throw new InvalidOperationException("The supplied Parvum GLB has no usable visible height.");
            }

            instance.transform.position += Vector3.up * (targetGroundY - bounds.min.y);
            EditorUtility.SetDirty(instance);
        }

        private static ReplacementResult InspectState(
            Transform parvumRoot,
            Transform ispantRoot,
            float expectedSpacing)
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                throw new InvalidOperationException("Unity reports script compilation errors.");
            }

            var slots = RequireParvumSlots(parvumRoot).OrderBy(slot => slot.position.x).ToArray();
            for (var index = 0; index < slots.Length; index++)
            {
                var models = slots[index].Cast<Transform>()
                    .Where(child => child.GetComponentsInChildren<Renderer>(true).Length > 0)
                    .ToArray();
                if (models.Length != 1 || models[0].name != ReplacementModelName)
                {
                    throw new InvalidOperationException(
                        slots[index].name + " must contain exactly one supplied Parvum GLB model child.");
                }

                RequireVisibleGeometry(models[0]);
                if (index > 0)
                {
                    var spacing = Mathf.Abs(slots[index].position.x - slots[index - 1].position.x);
                    if (Mathf.Abs(spacing - expectedSpacing) > SpacingTolerance)
                    {
                        throw new InvalidOperationException(
                            slots[index].name + " does not match the current Ispant X spacing.");
                    }
                }
            }

            var measuredIspantSpacing = MeasureUniformIspantSpacing(ispantRoot);
            if (Mathf.Abs(measuredIspantSpacing - expectedSpacing) > SpacingTolerance)
            {
                throw new InvalidOperationException("Ispant spacing changed during Parvum inspection.");
            }

            InspectPlayerStart(parvumRoot);
            return new ReplacementResult(slots.Length, expectedSpacing, BoundsOf(parvumRoot, new Bounds(parvumRoot.position, Vector3.one)));
        }

        private static AlignmentResult InspectVisibleGapAndYAlignment(
            Transform parvumRoot,
            Transform tergoRoot,
            AlignmentReference expected)
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                throw new InvalidOperationException("Unity reports script compilation errors.");
            }

            var slots = RequireCurrentParvumModels(parvumRoot)
                .Select(slot => new
                {
                    Slot = slot,
                    Bounds = ActiveBoundsOf(slot, new Bounds(slot.position, Vector3.one)),
                })
                .OrderBy(item => item.Bounds.center.x)
                .ToArray();
            for (var index = 0; index < slots.Length; index++)
            {
                if (Mathf.Abs(slots[index].Bounds.min.y - expected.GroundY) > SpacingTolerance)
                {
                    throw new InvalidOperationException(
                        slots[index].Slot.name + " does not match the current Tergo active renderer ground height.");
                }

                if (index > 0)
                {
                    var visibleGap = slots[index].Bounds.min.x - slots[index - 1].Bounds.max.x;
                    if (Mathf.Abs(visibleGap - expected.VisibleGap) > SpacingTolerance)
                    {
                        throw new InvalidOperationException(
                            slots[index].Slot.name + " does not match the current Tergo active renderer-bound X gap.");
                    }
                }
            }

            var measuredTergo = MeasureTergoVisibleGapAndGround(tergoRoot);
            if (Mathf.Abs(measuredTergo.VisibleGap - expected.VisibleGap) > SpacingTolerance ||
                Mathf.Abs(measuredTergo.GroundY - expected.GroundY) > SpacingTolerance ||
                Mathf.Abs(measuredTergo.CenterSpacing - expected.CenterSpacing) > SpacingTolerance ||
                Mathf.Abs(measuredTergo.StaticWidth - expected.StaticWidth) > SpacingTolerance)
            {
                throw new InvalidOperationException("Tergo active renderer alignment references changed during inspection.");
            }

            InspectPlayerStart(parvumRoot);
            return new AlignmentResult(
                slots.Length,
                expected.VisibleGap,
                expected.GroundY,
                expected.CenterSpacing,
                expected.StaticWidth,
                ActiveBoundsOf(parvumRoot, new Bounds(parvumRoot.position, Vector3.one)));
        }

        private static void ConfigurePlayer(Transform root)
        {
            var player = RequirePlayer();
            var camera = player.GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            var bounds = BoundsOf(root, new Bounds(root.position, Vector3.one));
            var front = root.GetChild(0).forward;
            front.y = 0f;
            if (front.sqrMagnitude < 0.001f)
            {
                throw new InvalidOperationException("The Parvum front direction is unusable.");
            }

            front.Normalize();
            var distance = PlayerDistance(bounds, camera);
            var desiredCamera = bounds.center + front * distance;
            var yaw = YawToward(desiredCamera, bounds.center);
            var cameraOffsetLocal = player.InverseTransformPoint(camera.transform.position);
            var playerY = player.position.y;
            var framed = false;
            for (var attempt = 0; attempt < 32; attempt++)
            {
                desiredCamera = bounds.center + front * distance;
                var desiredPlayer = desiredCamera - yaw * cameraOffsetLocal;
                desiredPlayer.y = playerY;
                player.SetPositionAndRotation(desiredPlayer, yaw);
                if (AreBoundsVisible(bounds, camera))
                {
                    framed = true;
                    break;
                }

                // Increase only the derived front distance until the actual Player camera contains the complete lineup.
                distance *= 1.15f;
            }

            if (!framed)
            {
                throw new InvalidOperationException(
                    "The Player camera could not frame the complete new Parvum lineup from the front.");
            }

            EditorUtility.SetDirty(player);
        }

        private static void InspectPlayerStart(Transform root)
        {
            var player = RequirePlayer();
            var camera = player.GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            var bounds = BoundsOf(root, new Bounds(root.position, Vector3.one));
            var toFocus = bounds.center - camera.transform.position;
            var cameraForward = camera.transform.forward;
            toFocus.y = 0f;
            cameraForward.y = 0f;
            if (toFocus.sqrMagnitude < 0.001f ||
                cameraForward.sqrMagnitude < 0.001f ||
                Vector3.Dot(toFocus.normalized, cameraForward.normalized) < 0.98f)
            {
                throw new InvalidOperationException("The Player camera does not face the Parvum lineup.");
            }

            if (!AreBoundsVisible(bounds, camera))
            {
                throw new InvalidOperationException(
                    "The complete new Parvum lineup is not visible from the Player start.");
            }
        }

        private static bool AreBoundsVisible(Bounds bounds, Camera camera)
        {
            return Corners(bounds).All(corner =>
            {
                var view = camera.WorldToViewportPoint(corner);
                return view.z > 0f &&
                       view.x >= -0.02f && view.x <= 1.02f &&
                       view.y >= -0.02f && view.y <= 1.02f;
            });
        }

        private static float PlayerDistance(Bounds bounds, Camera camera)
        {
            var vertical = Mathf.Max(1f, camera.fieldOfView * 0.5f) * Mathf.Deg2Rad;
            var currentAspect = Mathf.Max(0.1f, camera.aspect);
            var captureAspect = CaptureWidth / (float)CaptureHeight;
            var framingAspect = Mathf.Min(currentAspect, captureAspect);
            var horizontal = Mathf.Atan(Mathf.Tan(vertical) * framingAspect);
            return Mathf.Max(
                MinimumPlayerDistance,
                bounds.extents.x / Mathf.Max(0.01f, Mathf.Tan(horizontal)) + CameraMargin,
                bounds.extents.y / Mathf.Max(0.01f, Mathf.Tan(vertical)) + CameraMargin);
        }

        private static Quaternion YawToward(Vector3 from, Vector3 to)
        {
            var direction = to - from;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                throw new InvalidOperationException("The Player-to-Parvum direction is unusable.");
            }

            return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private static IEnumerable<Vector3> Corners(Bounds bounds)
        {
            for (var x = -1; x <= 1; x += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    for (var z = -1; z <= 1; z += 2)
                    {
                        yield return bounds.center + Vector3.Scale(
                            bounds.extents,
                            new Vector3(x, y, z));
                    }
                }
            }
        }

        private static Renderer[] RequireVisibleGeometry(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(root.name + " has no visible renderer.");
            }

            return renderers;
        }

        private static Bounds BoundsOf(Transform root, Bounds fallback)
        {
            var renderers = RequireVisibleGeometry(root);
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds.size.sqrMagnitude > 0.000001f ? bounds : fallback;
        }

        // Alignment measurements use only renderers that are currently active in the scene.
        private static Bounds ActiveBoundsOf(Transform root, Bounds fallback)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(root.name + " has no active visible renderer.");
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds.size.sqrMagnitude > 0.000001f ? bounds : fallback;
        }

        private static string[] ProtectedRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(root => root.name != ParvumRootName && root.name != PlayerName)
                .Select(root => HierarchySignature(root.transform))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static string HierarchySignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(item.name).Append('|')
                    .Append(Vec(item.position)).Append('|')
                    .Append(Vec(item.eulerAngles)).Append('|')
                    .Append(Vec(item.lossyScale)).Append('|')
                    .Append(item.gameObject.activeSelf ? '1' : '0').AppendLine();
            }

            return builder.ToString();
        }

        private static Transform RequirePlayer()
        {
            var player = GameObject.Find(PlayerName) ??
                         throw new InvalidOperationException("The Player root is missing.");
            if (player.transform.parent != null)
            {
                throw new InvalidOperationException("The Player object is not a scene root.");
            }

            return player.transform;
        }

        private static GameObject RequireRoot(string name)
        {
            var root = GameObject.Find(name) ??
                       throw new InvalidOperationException(name + " is missing.");
            if (root.transform.parent != null)
            {
                throw new InvalidOperationException(name + " is not a scene root.");
            }

            return root;
        }

        private static Scene RequireCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must already be the active scene. ActiveScene=" + scene.path + ".");
            }

            return scene;
        }

        private static void Capture(Camera camera, string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid capture path."));
            var previousActive = RenderTexture.active;
            var target = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
            var reviewCameraObject = new GameObject(
                "ParvumPlayerStartReviewCamera",
                typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            Camera reviewCamera = null;
            try
            {
                reviewCamera = reviewCameraObject.GetComponent<Camera>();
                reviewCamera.CopyFrom(camera);
                reviewCamera.transform.SetPositionAndRotation(
                    camera.transform.position,
                    camera.transform.rotation);
                reviewCamera.allowHDR = false;
                reviewCamera.targetTexture = target;
                RenderTexture.active = target;
                reviewCamera.Render();
                image.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
                image.Apply();
                File.WriteAllBytes(destination, image.EncodeToPNG());
            }
            finally
            {
                if (reviewCamera != null)
                {
                    reviewCamera.targetTexture = null;
                }

                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(image);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(reviewCameraObject);
            }
        }

        private static void WriteReport(ReplacementResult result, bool captureCreated)
        {
            var report = new StringBuilder()
                .AppendLine("Parvum GLB Replacement Report")
                .AppendLine("Result=PASS")
                .AppendLine("Source=" + SourceModelPath)
                .AppendLine("ImportedAsset=" + ImportedModelPath)
                .AppendLine("SourceSha256=" + ExpectedSourceSha256)
                .AppendLine("ParvumSlots=" + result.SlotCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("IspantXSpacing=" + Num(result.Spacing))
                .AppendLine("LineupBoundsCenter=" + Vec(result.Bounds.center))
                .AppendLine("LineupBoundsSize=" + Vec(result.Bounds.size))
                .AppendLine("PlayerFacesParvum=True")
                .AppendLine("CompleteLineupVisible=True")
                .AppendLine("IspantChanged=False")
                .AppendLine("OtherSceneRootsChanged=False")
                .AppendLine("CaptureCreated=" + (captureCreated ? "True" : "False"))
                .AppendLine("HarnessValidationRun=False")
                .ToString();
            var destination = Absolute(ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid report path."));
            File.WriteAllText(destination, report, new UTF8Encoding(false));
        }

        private static void WriteAlignmentReport(AlignmentResult result, bool captureCreated)
        {
            var report = new StringBuilder()
                .AppendLine("Parvum Static Tergo Visible Gap And Y Alignment Report")
                .AppendLine("Result=PASS")
                .AppendLine("ParvumSlots=" + result.SlotCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("TergoPrimarySlotCenterSpacing=" + Num(result.CenterSpacing))
                .AppendLine("StaticTergoActiveRendererWidthX=" + Num(result.StaticWidth))
                .AppendLine("StaticTergoDerivedRendererBoundXGap=" + Num(result.VisibleGap))
                .AppendLine("ParvumRendererBoundXGap=" + Num(result.VisibleGap))
                .AppendLine("StaticTergoActiveRendererGroundY=" + Num(result.GroundY))
                .AppendLine("ParvumRendererGroundY=" + Num(result.GroundY))
                .AppendLine("LineupBoundsCenter=" + Vec(result.Bounds.center))
                .AppendLine("LineupBoundsSize=" + Vec(result.Bounds.size))
                .AppendLine("PlayerFacesParvum=True")
                .AppendLine("CompleteLineupVisible=True")
                .AppendLine("TergoChanged=False")
                .AppendLine("OtherSceneRootsChanged=False")
                .AppendLine("CaptureCreated=" + (captureCreated ? "True" : "False"))
                .AppendLine("HarnessValidationRun=False")
                .ToString();
            var destination = Absolute(AlignmentReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid alignment report path."));
            File.WriteAllText(destination, report, new UTF8Encoding(false));
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(stream))
                .Replace("-", string.Empty);
        }

        private static void RequireSameHash(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    label + " SHA-256 mismatch. Expected=" + expected + ", Actual=" + actual + ".");
            }
        }

        private static string Absolute(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relativePath));
        }

        private static string Num(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + ")";
        }

        private readonly struct ReplacementResult
        {
            public ReplacementResult(int slotCount, float spacing, Bounds bounds)
            {
                SlotCount = slotCount;
                Spacing = spacing;
                Bounds = bounds;
            }

            public int SlotCount { get; }
            public float Spacing { get; }
            public Bounds Bounds { get; }
        }

        private readonly struct AlignmentReference
        {
            public AlignmentReference(
                float visibleGap,
                float groundY,
                float centerSpacing,
                float staticWidth)
            {
                VisibleGap = visibleGap;
                GroundY = groundY;
                CenterSpacing = centerSpacing;
                StaticWidth = staticWidth;
            }

            public float VisibleGap { get; }
            public float GroundY { get; }
            public float CenterSpacing { get; }
            public float StaticWidth { get; }
        }

        private readonly struct AlignmentResult
        {
            public AlignmentResult(
                int slotCount,
                float visibleGap,
                float groundY,
                float centerSpacing,
                float staticWidth,
                Bounds bounds)
            {
                SlotCount = slotCount;
                VisibleGap = visibleGap;
                GroundY = groundY;
                CenterSpacing = centerSpacing;
                StaticWidth = staticWidth;
                Bounds = bounds;
            }

            public int SlotCount { get; }
            public float VisibleGap { get; }
            public float GroundY { get; }
            public float CenterSpacing { get; }
            public float StaticWidth { get; }
            public Bounds Bounds { get; }
        }
    }
}
