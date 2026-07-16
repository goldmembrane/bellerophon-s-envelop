using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.GraveCargoRunScene
{
    internal static class GraveCargoRunSceneApplyAndReview
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string GraveModelAssetPath = "Assets/_Project/Art/Enemies/Grave/Models/grave.fbx";
        private const string GraveSourceRelativePath = "enemies model/grave.fbx";
        private const string GraveSampleRelativePath = "artSample/enemies/grave/grave.fbx";
        private const string ValidationRelativeFolder = "docs/validation/grave_static_placement_2026-07-15";
        private const string LongaRootName = "Approved Longa Arma Enemy Placement";
        private const string TergoRootName = "Approved Tergo Enemy Placement";
        private const string AccelerandoRootName = "Approved Accelerando Enemy Placement";
        private const string GraveRootName = "Approved Grave Enemy Placement";
        private const string GraveObjectName = "Grave_00_Static_Review";
        private const string LegacyGraveObjectName = "Grave_00_Static";
        private const string GraveModelName = "Grave_Model";
        private const string PlayerName = "Player";
        private const string MainCameraName = "Main Camera";
        private const float PlayerFrontDistance = 4.2f;
        private const float AnimationSlotSpacing = 5f;
        private const int CaptureLayer = 30;
        private const string ExpectedSha256 = "D6B44E97909B8B3D40A87E008A9E1916B4656BAC54A6DD55B438E890406750A6";
        private const string AnimationLayoutValidationRelativeFolder = "docs/validation/grave_animation_slot_layout_2026-07-15";
        private const string ApprovedReproductionModelAssetPath = "Assets/_Project/Art/Enemies/Grave/Models/Grave_Approved_Reproduction.fbx";
        private const string ApprovedFrontAlbedoAssetPath = "Assets/_Project/Art/Enemies/Grave/Textures/grave_front_albedo.png";
        private const string ApprovedTextileAlbedoAssetPath = "Assets/_Project/Art/Enemies/Grave/Textures/grave_textile_albedo.png";
        private const string ApprovedNormalAssetPath = "Assets/_Project/Art/Enemies/Grave/Textures/grave_fabric_normal.png";
        private const string ApprovedRoughnessAssetPath = "Assets/_Project/Art/Enemies/Grave/Textures/grave_fabric_roughness.png";
        private const string ApprovedFrontMaterialAssetPath = "Assets/_Project/Art/Enemies/Grave/Materials/Grave_Suit_Front_Mat.mat";
        private const string ApprovedTextileMaterialAssetPath = "Assets/_Project/Art/Enemies/Grave/Materials/Grave_Textile_BackSide_Mat.mat";
        private const string ApprovedReproductionValidationRelativeFolder = "docs/validation/grave_approved_reproduction_2026-07-15";
        private const string ApprovedModelSha256 = "8A43446A89A45A91082724CCA9026F87DC9B13F755CA93E16C2705460B312B70";
        private const string ApprovedFrontAlbedoSha256 = "443F93A21ED3DEC740B0166276B9E5083D9FEFEBD0896C1C5EDB9EB3280E1FD7";
        private const string ApprovedTextileAlbedoSha256 = "64FB548EBFBEA450F7BCDE518A3D1413BEA63B2E28EC410D7D425FF1980D4DD4";
        private const string ApprovedNormalSha256 = "071BB89B47A4A1A12D4027D3A34D514FB0F163F33F08671A4840A47D2071EDFC";
        private const string ApprovedRoughnessSha256 = "4C2DCF4A9E4A88D887DEE780F64E38167C35755455165846E1FA5336FDE68756";
        private const float ApprovedSmoothness = 0.22f;
        private const float ApprovedNormalStrength = 0.16f;
        private static readonly Quaternion ApprovedBackFacingModelRotation = Quaternion.Euler(0f, 180f, 0f);
        // User-approved opposite-side Player start; Grave rotation must not move it.
        private static readonly Vector3 ApprovedUnchangedPlayerPosition = new Vector3(58.02991f, 0f, -101.22584f);
        // Grave idle breathing is a simple rig-driven loop: no model/slot root motion and no generated art changes.
        private const string GraveIdleSlotName = "Grave_01_Idle";
        private const string GraveIdleClipAssetPath = "Assets/_Project/Art/Enemies/Grave/Animations/Grave_Idle_Breathing.anim";
        private const string GraveIdleControllerAssetPath = "Assets/_Project/Art/Enemies/Grave/Controllers/Grave_Idle_Breathing.controller";
        private const string GraveIdleValidationRelativeFolder = "docs/validation/grave_idle_breathing_2026-07-15";
        private const float GraveIdleDuration = 3f;
        private const float GraveIdleMaxBodyRise = 0.015f;
        private const float GraveIdleSpine02CrossExpansion = 0.006f;
        private const float GraveIdleSpine01CrossExpansion = 0.005f;
        private const float GraveIdleSpineCrossExpansion = 0.004f;
        private const float GraveIdleLengthExpansionPerBone = 0.002f;
        // Grave walk is an ordinary in-place review loop. Runtime translation remains reserved for Rigidbody movement.
        private const string GraveWalkSlotName = "Grave_02_Walk_Slow";
        private const string GraveWalkClipAssetPath = "Assets/_Project/Art/Enemies/Grave/Animations/Grave_Walk_Slow.anim";
        private const string GraveWalkControllerAssetPath = "Assets/_Project/Art/Enemies/Grave/Controllers/Grave_Walk_Slow.controller";
        private const string GraveWalkValidationRelativeFolder = "docs/validation/grave_walk_slow_2026-07-15";
        private const float GraveWalkDuration = 2f;
        private const float GraveWalkBodyRise = 0.016f;
        private const float GraveWalkRigGroundClearance = 0.0033f;
        private static readonly float[] GraveWalkKeyTimes =
            { 0f, 0.25f, 0.5f, 0.75f, 1f, 1.25f, 1.5f, 1.75f, 2f };
        // Preserve the user-selected walk action while reproducing the already approved Grave mesh and materials.
        private const string GraveWalkSourceRelativePath = "enemies model/grave walk.fbx";
        private const string GraveWalkRawAssetPath = "Assets/_Project/Art/Enemies/Grave/Models/Grave_Walk_Imported.fbx";
        private const string GraveWalkApprovedModelAssetPath = "Assets/_Project/Art/Enemies/Grave/Models/Grave_Walk_Approved_Reproduction.fbx";
        private const string GraveWalkImportedControllerAssetPath = "Assets/_Project/Art/Enemies/Grave/Controllers/Grave_Walk_Imported.controller";
        private const string GraveWalkImportedClipName = "Grave_Walk_Imported";
        private const string GraveWalkFbxValidationRelativeFolder = "docs/validation/grave_walk_fbx_replacement_2026-07-15";
        // Grave attack keeps the approved rig motion and adds one right-arm-only scythe-blade BlendShape.
        private const string GraveAttackSlotName = "Grave_03_Attack_RightArm_GiantSweep";
        private const string GraveAttackClipAssetPath = "Assets/_Project/Art/Enemies/Grave/Animations/Grave_Attack_CurtainCall_Sweep.anim";
        private const string GraveAttackControllerAssetPath = "Assets/_Project/Art/Enemies/Grave/Controllers/Grave_Attack_CurtainCall_Sweep.controller";
        private const string GraveAttackBladeMeshAssetPath = "Assets/_Project/Art/Enemies/Grave/Models/Grave_Attack_ScytheBlade_Body.asset";
        private const string GraveAttackBladeBlendShapeName = "GraveRightArmScytheBlade";
        private const string GraveAttackValidationRelativeFolder = "docs/validation/grave_scythe_blade_accelerated_attack_2026-07-16";
        private const float GraveAttackDuration = 3f;
        // The preserved motion reaches its full side extension at 1.2 seconds; the blade must be complete there.
        private const float GraveAttackBladeFullTime = 1.2f;
        // Rebuild the arm as the reference scythe blade: narrow heel, convex lower edge, curved pointed tip.
        private const float GraveAttackBladeDepth = 0.24f;
        private const float GraveAttackUpperThickness = 0.02f;
        private const float GraveAttackBladeStartProgress = 0.12f;
        private const float GraveAttackBladeFullProgress = 0.24f;
        // The blade centerline bends downward; it does not lift into the former leaf-shaped silhouette.
        private const float GraveAttackScytheExtension = 0.14f;
        private const float GraveAttackScytheTipDrop = 0.10f;
        private const float GraveAttackScytheBellyDrop = 0.06f;
        private const float GraveAttackScytheFrontThicknessScale = 0.06f;
        // Preserve the accepted pre-restart curtain-call arc through its original lowering guide.
        private const float GraveAttackSlashHoldTime = 1.28f;
        private const float GraveAttackSlashEndTime = 1.58f;
        private const float GraveAttackCurtainCallHoldTime = 2.35f;
        private const int GraveAttackPreviewCycles = 4;
        private static readonly float[] GraveAttackKeyTimes =
            { 0f, 0.35f, 0.85f, 1.2f, 1.65f, 2f, 2.35f, 2.65f, 3f };
        // Review the actual slash interval instead of skipping from side extension directly to the held pose.
        private static readonly float[] GraveAttackCaptureTimes =
            { 0f, 0.85f, 1.2f, 1.28f, 1.38f, 1.48f, 1.58f, 2f, 2.35f, 2.65f, 3f };
        private static LocalPoseState[] graveAttackPreviewNormalPoses;
        private static Transform graveAttackPreviewModel;
        private static Animator graveAttackPreviewAnimator;
        private static AnimationClip graveAttackPreviewClip;
        private static SkinnedMeshRenderer graveAttackPreviewRenderer;
        private static double graveAttackPreviewStartedAt;
        private static bool graveAttackPreviewAnimatorWasEnabled;
        private static bool graveAttackPreviewSceneWasDirty;
        private static Scene graveAttackPreviewScene;
        // Grave hit reaction loops for scene review; hips, legs, model, and slot remain fixed at every boundary.
        private const string GraveHitSlotName = "Grave_04_Hit_Recoil";
        private const string GraveHitClipAssetPath = "Assets/_Project/Art/Enemies/Grave/Animations/Grave_Hit_Recoil.anim";
        private const string GraveHitControllerAssetPath = "Assets/_Project/Art/Enemies/Grave/Controllers/Grave_Hit_Recoil.controller";
        private const string GraveHitValidationRelativeFolder = "docs/validation/grave_hit_recoil_2026-07-16";
        private const float GraveHitDuration = 1.1f;
        private static readonly float[] GraveHitKeyTimes = { 0f, 0.07f, 0.18f, 0.34f, 0.5f, 0.78f, 1.1f };
        private static readonly float[] GraveHitBodyFactors = { 0f, 0.42f, 1f, -0.18f, 0.14f, 0.025f, 0f };
        private static readonly float[] GraveHitHeadLagFactors = { 0f, 0.08f, 0.72f, 1f, -0.15f, 0.05f, 0f };

        private static readonly string[] AnimationSlotNames =
        {
            "Grave_00_Static_Review",
            "Grave_01_Idle",
            "Grave_02_Walk_Slow",
            "Grave_03_Attack_RightArm_GiantSweep",
            "Grave_04_Hit_Recoil",
            "Grave_05_Death",
            "Grave_06_Speaker_PowerOff"
        };

        [MenuItem("Bellerophon/Enemies/Grave/Apply Approved Static Placement")]
        public static void ApplyApprovedGravePlacement()
        {
            ValidateSourceCopies();
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GraveModelAssetPath);
            if (modelAsset == null)
            {
                throw new InvalidOperationException("Grave FBX has not been imported: " + GraveModelAssetPath);
            }

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var longa = RequireSceneObject(scene, LongaRootName);
            var tergo = RequireSceneObject(scene, TergoRootName);
            var accelerando = RequireSceneObject(scene, AccelerandoRootName);
            var player = RequireSceneObject(scene, PlayerName);
            var preservedRoots = CapturePreservedRoots(scene, player);

            var existing = FindSceneObject(scene, GraveRootName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }

            var spacing = Mathf.Abs(longa.transform.position.z - tergo.transform.position.z);
            if (spacing <= 0.1f)
            {
                spacing = Vector3.Distance(longa.transform.position, tergo.transform.position);
            }

            var graveRoot = new GameObject(GraveRootName);
            SceneManager.MoveGameObjectToScene(graveRoot, scene);
            graveRoot.transform.SetPositionAndRotation(
                new Vector3(accelerando.transform.position.x, accelerando.transform.position.y, accelerando.transform.position.z - spacing),
                Quaternion.identity);

            var graveObject = new GameObject(GraveObjectName);
            graveObject.transform.SetParent(graveRoot.transform, false);

            var model = PrefabUtility.InstantiatePrefab(modelAsset, scene) as GameObject;
            if (model == null)
            {
                model = UnityEngine.Object.Instantiate(modelAsset);
                SceneManager.MoveGameObjectToScene(model, scene);
            }

            model.name = GraveModelName;
            model.transform.SetParent(graveObject.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            DisableImportedHelpers(model.transform);
            AlignCharacterToGround(model.transform, graveRoot.transform.position.y);
            ArrangeAnimationSlotCopies(graveRoot.transform, graveObject.transform);

            var bounds = CalculateVisibleBounds(graveObject.transform);
            var lookAt = bounds.center;
            var front = CalculateVisualFront(model.transform);
            var start = lookAt - front * PlayerFrontDistance;
            start.y = player.transform.position.y;
            player.transform.SetPositionAndRotation(start, YawToward(start, lookAt));

            var mainCamera = FindSceneObject(scene, MainCameraName);
            if (mainCamera != null && !mainCamera.transform.IsChildOf(player.transform))
            {
                var cameraHeight = Mathf.Max(bounds.extents.y * 0.8f, 1.35f);
                var cameraPosition = start + Vector3.up * cameraHeight;
                mainCamera.transform.SetPositionAndRotation(cameraPosition, Quaternion.LookRotation(lookAt - cameraPosition, Vector3.up));
                EditorUtility.SetDirty(mainCamera.transform);
            }

            AssertPreservedRoots(preservedRoots);
            EditorUtility.SetDirty(graveRoot);
            EditorUtility.SetDirty(player.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            var metrics = ValidateScene(scene, writeReport: true);
            Debug.Log("GravePlacementApplied " + metrics);
        }

        [MenuItem("Bellerophon/Enemies/Grave/Arrange Approved Animation Slots")]
        public static void ArrangeApprovedGraveAnimationSlots()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var graveRoot = RequireSceneObject(scene, GraveRootName);
            var preservedRoots = scene.GetRootGameObjects()
                .Where(root => root != graveRoot)
                .Select(root => new RootState(root))
                .ToList();
            var rootPosition = graveRoot.transform.position;
            var rootRotation = graveRoot.transform.rotation;
            var rootScale = graveRoot.transform.localScale;
            var rootActive = graveRoot.activeSelf;

            var staticSlot = graveRoot.transform.Find(GraveObjectName) ?? graveRoot.transform.Find(LegacyGraveObjectName);
            if (staticSlot == null)
            {
                throw new InvalidOperationException("The current Grave static slot is missing.");
            }

            staticSlot.name = GraveObjectName;
            staticSlot.localPosition = Vector3.zero;
            staticSlot.localRotation = Quaternion.identity;
            staticSlot.localScale = Vector3.one;
            ArrangeAnimationSlotCopies(graveRoot.transform, staticSlot);

            AssertPreservedRoots(preservedRoots);
            if (graveRoot.transform.position != rootPosition || graveRoot.transform.rotation != rootRotation ||
                graveRoot.transform.localScale != rootScale || graveRoot.activeSelf != rootActive)
            {
                throw new InvalidOperationException("The Approved Grave Enemy Placement root Transform changed while arranging slots.");
            }

            EditorUtility.SetDirty(graveRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            var metrics = ValidateAnimationSlotLayout(scene, writeReport: true);
            Debug.Log("GraveAnimationSlotsArranged " + metrics);
        }

        [MenuItem("Bellerophon/Enemies/Grave/Validate Approved Animation Slots")]
        public static void ValidateApprovedGraveAnimationSlots()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var metrics = ValidateAnimationSlotLayout(scene, writeReport: true);
            Debug.Log("GraveAnimationSlotValidationPassed " + metrics);
        }

        [MenuItem("Bellerophon/Enemies/Grave/Capture Approved Animation Slot Layout")]
        public static void CaptureApprovedGraveAnimationSlotLayout()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            ValidateAnimationSlotLayout(scene, writeReport: false);
            var graveRoot = RequireSceneObject(scene, GraveRootName).transform;
            var slots = AnimationSlotNames.Select(name => graveRoot.Find(name)).ToArray();
            var bounds = CalculateCombinedVisibleBounds(slots);
            var staticModel = slots[0].Find(GraveModelName);
            var front = -CalculateVisualFront(staticModel);
            var transforms = graveRoot.GetComponentsInChildren<Transform>(true);
            var originalLayers = transforms.Select(target => target.gameObject.layer).ToArray();
            var cameraObject = new GameObject("Grave_AnimationLayoutCaptureCamera");
            var lightObject = new GameObject("Grave_AnimationLayoutCaptureLight");
            try
            {
                foreach (var target in transforms)
                {
                    target.gameObject.layer = CaptureLayer;
                }

                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.065f, 0.075f, 1f);
                camera.cullingMask = 1 << CaptureLayer;
                camera.orthographic = true;
                camera.aspect = 1280f / 720f;
                camera.orthographicSize = Mathf.Max(bounds.extents.y * 1.35f, bounds.extents.x / camera.aspect * 1.12f);
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 80f;
                var cameraPosition = bounds.center + front * 25f + Vector3.up * bounds.extents.y * 0.08f;
                camera.transform.SetPositionAndRotation(cameraPosition, Quaternion.LookRotation(bounds.center - cameraPosition, Vector3.up));

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2.2f;
                light.cullingMask = 1 << CaptureLayer;
                light.transform.rotation = Quaternion.Euler(48f, graveRoot.eulerAngles.y + 25f, 0f);

                var folder = ProjectAbsolutePath(AnimationLayoutValidationRelativeFolder);
                Directory.CreateDirectory(folder);
                var outputPath = Path.Combine(folder, "Grave_AnimationSlotLayout.png");
                SaveCameraPng(camera, outputPath, 1280, 720);
                Debug.Log($"GraveAnimationSlotLayoutCapturePassed Path={outputPath}, SlotCount={slots.Length}");
            }
            finally
            {
                for (var i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i] != null)
                    {
                        transforms[i].gameObject.layer = originalLayers[i];
                    }
                }

                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        [MenuItem("Bellerophon/Enemies/Grave/Move Player Start To Opposite Side")]
        public static void MoveApprovedGravePlayerStartToOppositeSide()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var graveRoot = RequireSceneObject(scene, GraveRootName);
            var graveObject = graveRoot.transform.Find(GraveObjectName) ??
                throw new InvalidOperationException("Grave_00_Static is missing.");
            var model = graveObject.Find(GraveModelName) ??
                throw new InvalidOperationException("Grave_Model is missing.");
            var player = RequireSceneObject(scene, PlayerName);
            var preservedRoots = scene.GetRootGameObjects()
                .Where(root => root != player)
                .Select(root => new RootState(root))
                .ToList();

            var bounds = CalculateVisibleBounds(graveObject);
            var front = CalculateVisualFront(model);
            var start = bounds.center - front * PlayerFrontDistance;
            start.y = player.transform.position.y;
            player.transform.SetPositionAndRotation(start, YawToward(start, bounds.center));

            AssertPreservedRoots(preservedRoots);
            EditorUtility.SetDirty(player.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            var metrics = ValidateScene(scene, writeReport: true);
            Debug.Log("GravePlayerStartMovedToOppositeSide " + metrics);
        }

        [MenuItem("Bellerophon/Enemies/Grave/Validate Approved Static Placement")]
        public static void ValidateApprovedGravePlacement()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var metrics = ValidateScene(scene, writeReport: true);
            Debug.Log("GravePlacementValidationPassed " + metrics);
        }

        [MenuItem("Bellerophon/Enemies/Grave/Capture Approved Static Placement")]
        public static void CaptureApprovedGravePlacement()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            ValidateScene(scene, writeReport: false);
            var graveObject = RequireSceneObject(scene, GraveRootName).transform.Find(GraveObjectName);
            var player = RequireSceneObject(scene, PlayerName).transform;
            var model = graveObject.Find(GraveModelName);
            var bounds = CalculateVisibleBounds(graveObject);

            var transforms = graveObject.GetComponentsInChildren<Transform>(true);
            var originalLayers = transforms.Select(t => t.gameObject.layer).ToArray();
            var cameraObject = new GameObject("Grave_FinalCaptureCamera");
            var lightObject = new GameObject("Grave_FinalCaptureLight");
            try
            {
                foreach (var target in transforms)
                {
                    target.gameObject.layer = CaptureLayer;
                }

                var camera = cameraObject.AddComponent<Camera>();
                var playerCamera = player.GetComponentInChildren<Camera>(true);
                if (playerCamera != null)
                {
                    camera.CopyFrom(playerCamera);
                    camera.transform.SetPositionAndRotation(playerCamera.transform.position, playerCamera.transform.rotation);
                }
                else
                {
                    camera.fieldOfView = 60f;
                    var eye = player.position + Vector3.up * 1.55f;
                    camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(bounds.center - eye, Vector3.up));
                }

                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.065f, 0.075f, 1f);
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 50f;

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2.2f;
                light.transform.rotation = Quaternion.Euler(48f, player.eulerAngles.y + 25f, 0f);

                var outputDirectory = ProjectAbsolutePath(ValidationRelativeFolder);
                Directory.CreateDirectory(outputDirectory);
                var outputPath = Path.Combine(outputDirectory, "Grave_PlayerStart_OppositeSide.png");
                SaveCameraPng(camera, outputPath, 1280, 720);
                Debug.Log($"GravePlacementCapturePassed Path={outputPath}, Front={FormatVector(CalculateVisualFront(model))}");
            }
            finally
            {
                for (var i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i] != null)
                    {
                        transforms[i].gameObject.layer = originalLayers[i];
                    }
                }

                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        [MenuItem("Bellerophon/Enemies/Grave/Apply Approved Reproduction To All Slots")]
        public static void ApplyApprovedGraveReproduction()
        {
            ValidateApprovedArtifactCopies();
            ConfigureApprovedModelImporter();
            ConfigureApprovedTextureImporters();
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ApprovedReproductionModelAssetPath) ??
                throw new InvalidOperationException("Approved Grave reproduction FBX has not been imported.");
            var frontMaterial = EnsureApprovedMaterial(
                ApprovedFrontMaterialAssetPath,
                ApprovedFrontAlbedoAssetPath);
            var textileMaterial = EnsureApprovedMaterial(
                ApprovedTextileMaterialAssetPath,
                ApprovedTextileAlbedoAssetPath);

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var graveRoot = RequireSceneObject(scene, GraveRootName).transform;
            var preservedRoots = scene.GetRootGameObjects()
                .Where(root => root != graveRoot.gameObject)
                .Select(root => new RootState(root))
                .ToList();
            var graveRootState = new RootState(graveRoot.gameObject);
            var slotStates = AnimationSlotNames
                .Select(name => graveRoot.Find(name) ?? throw new InvalidOperationException(name + " is missing."))
                .Select(slot => new RootState(slot.gameObject))
                .ToList();

            foreach (var slotName in AnimationSlotNames)
            {
                var slot = graveRoot.Find(slotName) ?? throw new InvalidOperationException(slotName + " is missing.");
                var previousModel = slot.Find(GraveModelName);
                if (previousModel == null)
                {
                    throw new InvalidOperationException(slotName + "/" + GraveModelName + " is missing.");
                }

                UnityEngine.Object.DestroyImmediate(previousModel.gameObject);
                var model = PrefabUtility.InstantiatePrefab(modelAsset, scene) as GameObject;
                if (model == null)
                {
                    throw new InvalidOperationException("Failed to instantiate the approved Grave reproduction FBX.");
                }

                model.name = GraveModelName;
                model.transform.SetParent(slot, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = ApprovedBackFacingModelRotation;
                model.transform.localScale = Vector3.one;
                DisableImportedHelpers(model.transform);
                ApplyApprovedMaterials(model.transform, frontMaterial, textileMaterial);
                AlignCharacterToGround(model.transform, graveRoot.position.y);
                EditorUtility.SetDirty(model);
            }

            graveRootState.AssertUnchanged();
            foreach (var slotState in slotStates)
            {
                slotState.AssertUnchanged();
            }

            AssertPreservedRoots(preservedRoots);
            EditorUtility.SetDirty(graveRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            var metrics = ValidateApprovedReproductionScene(scene, writeReport: true);
            Debug.Log("GraveApprovedReproductionApplied " + metrics);
        }

        [MenuItem("Bellerophon/Enemies/Grave/Apply Approved Back-Facing 180 Rotation")]
        public static void ApplyApprovedGraveBackFacingRotation()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var graveRoot = RequireSceneObject(scene, GraveRootName).transform;
            var preservedRoots = scene.GetRootGameObjects()
                .Where(root => root != graveRoot.gameObject)
                .Select(root => new RootState(root))
                .ToList();
            var graveRootState = new RootState(graveRoot.gameObject);
            var slotStates = AnimationSlotNames
                .Select(name => graveRoot.Find(name) ?? throw new InvalidOperationException(name + " is missing."))
                .Select(slot => new RootState(slot.gameObject))
                .ToList();

            foreach (var slotName in AnimationSlotNames)
            {
                var slot = graveRoot.Find(slotName) ?? throw new InvalidOperationException(slotName + " is missing.");
                var model = slot.Find(GraveModelName) ??
                    throw new InvalidOperationException(slotName + "/" + GraveModelName + " is missing.");
                var preservedPosition = model.localPosition;
                var preservedScale = model.localScale;
                model.localRotation = ApprovedBackFacingModelRotation;
                if (model.localPosition != preservedPosition || model.localScale != preservedScale)
                {
                    throw new InvalidOperationException(slotName + " position or scale changed while rotating Grave 180 degrees.");
                }

                PrefabUtility.RecordPrefabInstancePropertyModifications(model);
                EditorUtility.SetDirty(model);
            }

            graveRootState.AssertUnchanged();
            foreach (var slotState in slotStates)
            {
                slotState.AssertUnchanged();
            }

            AssertPreservedRoots(preservedRoots);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            var metrics = ValidateApprovedReproductionScene(scene, writeReport: true);
            Debug.Log("GraveApprovedBackFacingRotationApplied " + metrics);
        }

        [MenuItem("Bellerophon/Enemies/Grave/Inspect Approved Idle Rig")]
        public static void InspectApprovedGraveIdleRig()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var graveRoot = RequireSceneObject(scene, GraveRootName).transform;
            var idleSlot = graveRoot.Find("Grave_01_Idle") ??
                throw new InvalidOperationException("Grave_01_Idle is missing.");
            var model = idleSlot.Find(GraveModelName) ??
                throw new InvalidOperationException("Grave_01_Idle/" + GraveModelName + " is missing.");
            var body = FindDescendant(model, "Grave_Body");
            var renderer = body != null ? body.GetComponent<SkinnedMeshRenderer>() : null;
            if (renderer == null || renderer.sharedMesh == null)
            {
                throw new InvalidOperationException("Grave_01_Idle does not contain Grave_Body SkinnedMeshRenderer.");
            }

            var hierarchy = string.Join(
                " | ",
                model.GetComponentsInChildren<Transform>(true).Select(target =>
                    $"{AnimationUtility.CalculateTransformPath(target, model)}:" +
                    $"P={FormatVector(target.localPosition)},R={FormatVector(target.localEulerAngles)},S={FormatVector(target.localScale)}"));
            var animator = model.GetComponent<Animator>();
            Debug.Log(
                $"GraveIdleRigInspection BlendShapes={renderer.sharedMesh.blendShapeCount}, " +
                $"Animator={(animator != null ? "Present" : "Missing")}, Hierarchy={hierarchy}");
        }

        [MenuItem("Bellerophon/Enemies/Grave/Apply Approved Idle Breathing")]
        public static void ApplyApprovedGraveIdleBreathing()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            ValidateApprovedReproductionScene(scene, writeReport: false);
            var graveRoot = RequireSceneObject(scene, GraveRootName).transform;
            var preservedRoots = scene.GetRootGameObjects()
                .Where(root => root != graveRoot.gameObject)
                .Select(root => new RootState(root))
                .ToList();
            var graveRootState = new RootState(graveRoot.gameObject);
            var slotStates = AnimationSlotNames
                .Select(name => graveRoot.Find(name) ?? throw new InvalidOperationException(name + " is missing."))
                .Select(slot => new RootState(slot.gameObject))
                .ToList();
            var idleSlot = graveRoot.Find(GraveIdleSlotName) ??
                throw new InvalidOperationException(GraveIdleSlotName + " is missing.");
            var idleModel = idleSlot.Find(GraveModelName) ??
                throw new InvalidOperationException(GraveIdleSlotName + "/" + GraveModelName + " is missing.");

            var clip = EnsureApprovedGraveIdleClip(idleModel);
            var controller = EnsureApprovedGraveIdleController(clip);
            var animator = idleModel.GetComponent<Animator>();
            if (animator == null)
            {
                animator = idleModel.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            RebindApprovedGraveCurtainCallAnimator(animator, controller, clip);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorUtility.SetDirty(animator);

            graveRootState.AssertUnchanged();
            foreach (var slotState in slotStates)
            {
                slotState.AssertUnchanged();
            }

            AssertPreservedRoots(preservedRoots);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            var metrics = ValidateApprovedGraveIdleBreathingScene(scene, writeReport: true);
            Debug.Log("GraveApprovedIdleBreathingApplied " + metrics);
        }

        [MenuItem("Bellerophon/Enemies/Grave/Validate Approved Idle Breathing")]
        public static void ValidateApprovedGraveIdleBreathing()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var metrics = ValidateApprovedGraveIdleBreathingScene(scene, writeReport: true);
            Debug.Log("GraveApprovedIdleBreathingValidationPassed " + metrics);
        }

        [MenuItem("Bellerophon/Enemies/Grave/Capture Approved Idle Breathing")]
        public static void CaptureApprovedGraveIdleBreathing()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            ValidateApprovedGraveIdleBreathingScene(scene, writeReport: false);
            var graveRoot = RequireSceneObject(scene, GraveRootName).transform;
            var idleSlot = graveRoot.Find(GraveIdleSlotName) ??
                throw new InvalidOperationException(GraveIdleSlotName + " is missing.");
            var idleModel = idleSlot.Find(GraveModelName) ??
                throw new InvalidOperationException(GraveIdleSlotName + "/" + GraveModelName + " is missing.");
            var animator = idleModel.GetComponent<Animator>();
            if (animator == null)
            {
                throw new InvalidOperationException("Grave idle Animator is missing.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(GraveIdleClipAssetPath) ??
                throw new InvalidOperationException("Grave idle breathing clip is missing.");
            var originalAnimatorEnabled = animator.enabled;
            var poses = CaptureLocalPoses(idleModel);
            var transforms = idleSlot.GetComponentsInChildren<Transform>(true);
            var originalLayers = transforms.Select(target => target.gameObject.layer).ToArray();
            var cameraObject = new GameObject("Grave_IdleBreathing_CaptureCamera");
            var lightObject = new GameObject("Grave_IdleBreathing_CaptureLight");
            var frames = new Texture2D[5];
            Texture2D sheet = null;
            try
            {
                animator.enabled = false;
                foreach (var target in transforms)
                {
                    target.gameObject.layer = CaptureLayer;
                }

                RestoreLocalPoses(poses);
                var baseBounds = CalculateVisibleBounds(idleSlot);
                var front = CalculateVisualFront(idleModel);
                var cameraPosition = baseBounds.center + front * 5f;
                var camera = cameraObject.AddComponent<Camera>();
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.96f, 0.965f, 0.97f, 1f);
                camera.orthographic = true;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 80f;
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2.2f;
                light.cullingMask = 1 << CaptureLayer;
                var reviewTimes = new[] { 0f, 0.75f, 1.5f, 2.25f, GraveIdleDuration };
                for (var i = 0; i < reviewTimes.Length; i++)
                {
                    RestoreLocalPoses(poses);
                    clip.SampleAnimation(idleModel.gameObject, reviewTimes[i]);
                    frames[i] = RenderReviewView(
                        camera,
                        light,
                        baseBounds,
                        cameraPosition,
                        Mathf.Max(baseBounds.extents.y * 1.18f, 1.05f),
                        480,
                        720);
                }

                sheet = new Texture2D(2400, 720, TextureFormat.RGB24, false);
                sheet.SetPixels(Enumerable.Repeat(new Color(0.08f, 0.09f, 0.10f, 1f), 2400 * 720).ToArray());
                for (var i = 0; i < frames.Length; i++)
                {
                    sheet.SetPixels(i * 480, 0, 480, 720, frames[i].GetPixels());
                }

                sheet.Apply();
                var folder = ProjectAbsolutePath(GraveIdleValidationRelativeFolder);
                Directory.CreateDirectory(folder);
                var outputPath = Path.Combine(folder, "Grave_Idle_Breathing_Review.png");
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
                Debug.Log(
                    $"GraveApprovedIdleBreathingCapturePassed Path={outputPath}, " +
                    $"Times=0|0.75|1.5|2.25|3, Duration={GraveIdleDuration:0.###}, " +
                    $"MaxRise={GraveIdleMaxBodyRise:0.###}");
            }
            finally
            {
                RestoreLocalPoses(poses);
                animator.enabled = originalAnimatorEnabled;
                for (var i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i] != null)
                    {
                        transforms[i].gameObject.layer = originalLayers[i];
                    }
                }

                foreach (var frame in frames)
                {
                    UnityEngine.Object.DestroyImmediate(frame);
                }

                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        [MenuItem("Bellerophon/Enemies/Grave/Apply Approved Slow Walk")]
        public static void ApplyApprovedGraveSlowWalk()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            ValidateApprovedReproductionScene(scene, writeReport: false);
            var graveRoot = RequireSceneObject(scene, GraveRootName).transform;
            var preservedRoots = scene.GetRootGameObjects()
                .Where(root => root != graveRoot.gameObject)
                .Select(root => new RootState(root))
                .ToList();
            var graveRootState = new RootState(graveRoot.gameObject);
            var slotStates = AnimationSlotNames
                .Select(name => graveRoot.Find(name) ?? throw new InvalidOperationException(name + " is missing."))
                .Select(slot => new RootState(slot.gameObject))
                .ToList();
            var walkSlot = graveRoot.Find(GraveWalkSlotName) ??
                throw new InvalidOperationException(GraveWalkSlotName + " is missing.");
            var walkModel = walkSlot.Find(GraveModelName) ??
                throw new InvalidOperationException(GraveWalkSlotName + "/" + GraveModelName + " is missing.");

            var clip = EnsureApprovedGraveSlowWalkClip(walkModel);
            var controller = EnsureApprovedGraveSlowWalkController(clip);
            var animator = walkModel.GetComponent<Animator>();
            if (animator == null)
            {
                animator = walkModel.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorUtility.SetDirty(animator);

            graveRootState.AssertUnchanged();
            foreach (var slotState in slotStates)
            {
                slotState.AssertUnchanged();
            }

            AssertPreservedRoots(preservedRoots);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            var metrics = ValidateApprovedGraveSlowWalkScene(scene, writeReport: true);
            Debug.Log("GraveApprovedSlowWalkApplied " + metrics);
        }

        [MenuItem("Bellerophon/Enemies/Grave/Validate Approved Slow Walk")]
        public static void ValidateApprovedGraveSlowWalk()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var metrics = ValidateApprovedGraveSlowWalkScene(scene, writeReport: true);
            Debug.Log("GraveApprovedSlowWalkValidationPassed " + metrics);
        }

        [MenuItem("Bellerophon/Enemies/Grave/Capture Approved Slow Walk")]
        public static void CaptureApprovedGraveSlowWalk()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            ValidateApprovedGraveSlowWalkScene(scene, writeReport: false);
            var graveRoot = RequireSceneObject(scene, GraveRootName).transform;
            var walkSlot = graveRoot.Find(GraveWalkSlotName) ??
                throw new InvalidOperationException(GraveWalkSlotName + " is missing.");
            var walkModel = walkSlot.Find(GraveModelName) ??
                throw new InvalidOperationException(GraveWalkSlotName + "/" + GraveModelName + " is missing.");
            var animator = walkModel.GetComponent<Animator>() ??
                throw new InvalidOperationException("Grave slow-walk Animator is missing.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(GraveWalkClipAssetPath) ??
                throw new InvalidOperationException("Grave slow-walk clip is missing.");
            var originalAnimatorEnabled = animator.enabled;
            var poses = CaptureLocalPoses(walkModel);
            var transforms = walkSlot.GetComponentsInChildren<Transform>(true);
            var originalLayers = transforms.Select(target => target.gameObject.layer).ToArray();
            var cameraObject = new GameObject("Grave_SlowWalk_CaptureCamera");
            var lightObject = new GameObject("Grave_SlowWalk_CaptureLight");
            var frames = new Texture2D[GraveWalkKeyTimes.Length];
            Texture2D sheet = null;
            try
            {
                animator.enabled = false;
                foreach (var target in transforms)
                {
                    target.gameObject.layer = CaptureLayer;
                }

                RestoreLocalPoses(poses);
                var baseBounds = CalculateVisibleBounds(walkSlot);
                var front = CalculateVisualFront(walkModel);
                var cameraPosition = baseBounds.center + front * 5f;
                var camera = cameraObject.AddComponent<Camera>();
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.96f, 0.965f, 0.97f, 1f);
                camera.orthographic = true;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 80f;
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2.2f;
                light.cullingMask = 1 << CaptureLayer;
                for (var i = 0; i < GraveWalkKeyTimes.Length; i++)
                {
                    RestoreLocalPoses(poses);
                    clip.SampleAnimation(walkModel.gameObject, GraveWalkKeyTimes[i]);
                    frames[i] = RenderReviewView(
                        camera,
                        light,
                        baseBounds,
                        cameraPosition,
                        Mathf.Max(baseBounds.extents.y * 1.18f, 1.05f),
                        360,
                        640);
                }

                var sheetWidth = 360 * frames.Length;
                sheet = new Texture2D(sheetWidth, 640, TextureFormat.RGB24, false);
                sheet.SetPixels(Enumerable.Repeat(new Color(0.08f, 0.09f, 0.10f, 1f), sheetWidth * 640).ToArray());
                for (var i = 0; i < frames.Length; i++)
                {
                    sheet.SetPixels(i * 360, 0, 360, 640, frames[i].GetPixels());
                }

                sheet.Apply();
                var folder = ProjectAbsolutePath(GraveWalkValidationRelativeFolder);
                Directory.CreateDirectory(folder);
                var outputPath = Path.Combine(folder, "Grave_Walk_Slow_Review.png");
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
                Debug.Log(
                    $"GraveApprovedSlowWalkCapturePassed Path={outputPath}, " +
                    $"Times=0|0.25|0.5|0.75|1|1.25|1.5|1.75|2, Duration={GraveWalkDuration:0.###}");
            }
            finally
            {
                RestoreLocalPoses(poses);
                animator.enabled = originalAnimatorEnabled;
                for (var i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i] != null)
                    {
                        transforms[i].gameObject.layer = originalLayers[i];
                    }
                }

                foreach (var frame in frames)
                {
                    UnityEngine.Object.DestroyImmediate(frame);
                }

                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        [MenuItem("Bellerophon/Enemies/Grave/Apply Walk FBX Replacement")]
        public static void ApplyGraveWalkFbxReplacement()
        {
            ValidateGraveWalkSourceCopy();
            var clip = ConfigureImportedGraveWalkModel();
            var controller = EnsureImportedGraveWalkController(clip);
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GraveWalkApprovedModelAssetPath) ??
                throw new InvalidOperationException("Approved Grave walk FBX has not been imported.");
            var frontMaterial = AssetDatabase.LoadAssetAtPath<Material>(ApprovedFrontMaterialAssetPath) ??
                throw new InvalidOperationException("Approved Grave front material is missing.");
            var textileMaterial = AssetDatabase.LoadAssetAtPath<Material>(ApprovedTextileMaterialAssetPath) ??
                throw new InvalidOperationException("Approved Grave textile material is missing.");

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            ValidateApprovedReproductionScene(scene, writeReport: false);
            var graveRoot = RequireSceneObject(scene, GraveRootName).transform;
            var preservedRoots = scene.GetRootGameObjects()
                .Where(root => root != graveRoot.gameObject)
                .Select(root => new RootState(root))
                .ToList();
            var graveRootState = new RootState(graveRoot.gameObject);
            var preservedSlots = AnimationSlotNames
                .Where(name => name != GraveWalkSlotName)
                .Select(name => graveRoot.Find(name) ?? throw new InvalidOperationException(name + " is missing."))
                .Select(slot => new RootState(slot.gameObject))
                .ToList();
            var oldSlot = graveRoot.Find(GraveWalkSlotName) ??
                throw new InvalidOperationException(GraveWalkSlotName + " is missing.");
            var oldModel = oldSlot.Find(GraveModelName) ??
                throw new InvalidOperationException(GraveWalkSlotName + "/" + GraveModelName + " is missing.");
            var slotPosition = oldSlot.localPosition;
            var slotRotation = oldSlot.localRotation;
            var slotScale = oldSlot.localScale;
            var slotSiblingIndex = oldSlot.GetSiblingIndex();
            var slotActive = oldSlot.gameObject.activeSelf;
            var modelPosition = oldModel.localPosition;
            var modelRotation = oldModel.localRotation;
            var modelScale = oldModel.localScale;

            UnityEngine.Object.DestroyImmediate(oldSlot.gameObject);
            var walkSlot = new GameObject(GraveWalkSlotName);
            SceneManager.MoveGameObjectToScene(walkSlot, scene);
            walkSlot.transform.SetParent(graveRoot, false);
            walkSlot.transform.SetSiblingIndex(slotSiblingIndex);
            walkSlot.transform.localPosition = slotPosition;
            walkSlot.transform.localRotation = slotRotation;
            walkSlot.transform.localScale = slotScale;
            walkSlot.SetActive(slotActive);

            var model = PrefabUtility.InstantiatePrefab(modelAsset, scene) as GameObject;
            if (model == null)
            {
                throw new InvalidOperationException("Failed to instantiate the approved Grave walk FBX.");
            }

            model.name = GraveModelName;
            model.transform.SetParent(walkSlot.transform, false);
            model.transform.localPosition = modelPosition;
            model.transform.localRotation = modelRotation;
            model.transform.localScale = modelScale;
            DisableImportedHelpers(model.transform);
            ApplyApprovedMaterials(model.transform, frontMaterial, textileMaterial);
            var renderer = FindDescendant(model.transform, "Grave_Body")?.GetComponent<SkinnedMeshRenderer>() ??
                throw new InvalidOperationException("Imported Grave walk model is missing Grave_Body renderer.");
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);

            var animator = model.GetComponent<Animator>();
            if (animator == null)
            {
                animator = model.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorUtility.SetDirty(animator);

            graveRootState.AssertUnchanged();
            foreach (var slotState in preservedSlots)
            {
                slotState.AssertUnchanged();
            }

            AssertPreservedRoots(preservedRoots);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            var metrics = ValidateGraveWalkFbxReplacementScene(scene, writeReport: true);
            Debug.Log("GraveWalkFbxReplacementApplied " + metrics);
        }

        [MenuItem("Bellerophon/Enemies/Grave/Validate Walk FBX Replacement")]
        public static void ValidateGraveWalkFbxReplacement()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var metrics = ValidateGraveWalkFbxReplacementScene(scene, writeReport: true);
            Debug.Log("GraveWalkFbxReplacementValidationPassed " + metrics);
        }

        [MenuItem("Bellerophon/Enemies/Grave/Capture Walk FBX Replacement")]
        public static void CaptureGraveWalkFbxReplacement()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            ValidateGraveWalkFbxReplacementScene(scene, writeReport: false);
            var graveRoot = RequireSceneObject(scene, GraveRootName).transform;
            var staticSlot = graveRoot.Find(AnimationSlotNames[0]) ??
                throw new InvalidOperationException(AnimationSlotNames[0] + " is missing.");
            var walkSlot = graveRoot.Find(GraveWalkSlotName) ??
                throw new InvalidOperationException(GraveWalkSlotName + " is missing.");
            var staticModel = staticSlot.Find(GraveModelName) ??
                throw new InvalidOperationException(AnimationSlotNames[0] + "/" + GraveModelName + " is missing.");
            var walkModel = walkSlot.Find(GraveModelName) ??
                throw new InvalidOperationException(GraveWalkSlotName + "/" + GraveModelName + " is missing.");
            var animator = walkModel.GetComponent<Animator>() ??
                throw new InvalidOperationException("Imported Grave walk Animator is missing.");
            var clip = LoadImportedGraveWalkClip();
            var originalAnimatorEnabled = animator.enabled;
            var walkPoses = CaptureLocalPoses(walkModel);
            var walkSlotPosition = walkSlot.localPosition;
            var transforms = staticSlot.GetComponentsInChildren<Transform>(true)
                .Concat(walkSlot.GetComponentsInChildren<Transform>(true))
                .Distinct()
                .ToArray();
            var originalLayers = transforms.Select(target => target.gameObject.layer).ToArray();
            var cameraObject = new GameObject("Grave_WalkFbx_CaptureCamera");
            var lightObject = new GameObject("Grave_WalkFbx_CaptureLight");
            var reviewTimes = new[] { 0f, clip.length * 0.25f, clip.length * 0.5f, clip.length * 0.75f, clip.length };
            var frames = new Texture2D[reviewTimes.Length];
            Texture2D sheet = null;
            try
            {
                animator.enabled = false;
                walkSlot.localPosition = staticSlot.localPosition + Vector3.right * 1.15f;
                foreach (var target in transforms)
                {
                    target.gameObject.layer = CaptureLayer;
                }

                var camera = cameraObject.AddComponent<Camera>();
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.96f, 0.965f, 0.97f, 1f);
                camera.orthographic = true;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 80f;
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2.2f;
                light.cullingMask = 1 << CaptureLayer;
                var front = CalculateVisualFront(staticModel);
                for (var i = 0; i < reviewTimes.Length; i++)
                {
                    RestoreLocalPoses(walkPoses);
                    clip.SampleAnimation(walkModel.gameObject, reviewTimes[i]);
                    var bounds = CalculateCombinedVisibleBounds(new[] { staticSlot, walkSlot });
                    var cameraPosition = bounds.center + front * 5f;
                    frames[i] = RenderReviewView(
                        camera,
                        light,
                        bounds,
                        cameraPosition,
                        Mathf.Max(bounds.extents.y * 1.2f, bounds.extents.x * 0.72f),
                        480,
                        720);
                }

                sheet = new Texture2D(2400, 720, TextureFormat.RGB24, false);
                sheet.SetPixels(Enumerable.Repeat(new Color(0.08f, 0.09f, 0.10f, 1f), 2400 * 720).ToArray());
                for (var i = 0; i < frames.Length; i++)
                {
                    sheet.SetPixels(i * 480, 0, 480, 720, frames[i].GetPixels());
                }

                sheet.Apply();
                var folder = ProjectAbsolutePath(GraveWalkFbxValidationRelativeFolder);
                Directory.CreateDirectory(folder);
                var outputPath = Path.Combine(folder, "Grave_Walk_Fbx_Replacement_Comparison.png");
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
                Debug.Log(
                    $"GraveWalkFbxReplacementCapturePassed Path={outputPath}, " +
                    $"Times=0|25|50|75|100%, Duration={clip.length:0.###}");
            }
            finally
            {
                RestoreLocalPoses(walkPoses);
                walkSlot.localPosition = walkSlotPosition;
                animator.enabled = originalAnimatorEnabled;
                for (var i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i] != null)
                    {
                        transforms[i].gameObject.layer = originalLayers[i];
                    }
                }

                foreach (var frame in frames)
                {
                    UnityEngine.Object.DestroyImmediate(frame);
                }

                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        [MenuItem("Bellerophon/Enemies/Grave/Preview and Inspect Approved Attack Rig")]
        public static void InspectApprovedGraveAttackRig()
        {
            var scene = RequireOpenCargoRunScene();
            var sceneWasDirty = scene.isDirty;
            var graveRoot = RequireRootSceneObject(scene, GraveRootName).transform;
            var attackSlot = graveRoot.Find(GraveAttackSlotName) ??
                throw new InvalidOperationException(GraveAttackSlotName + " is missing.");
            var attackModel = attackSlot.Find(GraveModelName) ??
                throw new InvalidOperationException(GraveAttackSlotName + "/" + GraveModelName + " is missing.");
            var renderer = RequireGraveAttackBodyRenderer(attackModel);
            var animator = attackModel.GetComponent<Animator>();
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(GraveAttackClipAssetPath);
            if (animator == null || clip == null)
            {
                throw new InvalidOperationException("Grave attack preview requires the approved Animator and clip.");
            }

            var blendShapeIndex = renderer.sharedMesh.GetBlendShapeIndex(GraveAttackBladeBlendShapeName);
            if (blendShapeIndex < 0)
            {
                throw new InvalidOperationException("Grave attack preview BlendShape is missing.");
            }

            StartGraveAttackEditModePreview(scene, attackModel, animator, clip, renderer, blendShapeIndex);
            var controllerClips = animator?.runtimeAnimatorController == null
                ? "None"
                : string.Join("|", animator.runtimeAnimatorController.animationClips.Select(item => item.name));
            var blendShapeBindings = clip == null
                ? "None"
                : string.Join(
                    "|",
                    AnimationUtility.GetCurveBindings(clip)
                        .Where(binding => binding.propertyName.StartsWith("blendShape.", StringComparison.Ordinal))
                        .Select(binding => $"{binding.path}:{binding.propertyName}"));
            var runtimeState = "EditMode";
            if (EditorApplication.isPlaying && animator != null && animator.isActiveAndEnabled)
            {
                var clipInfo = animator.GetCurrentAnimatorClipInfo(0);
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                runtimeState =
                    $"PlayMode:Normalized={stateInfo.normalizedTime:0.###}," +
                    $"Clips={string.Join("|", clipInfo.Select(item => item.clip.name))}";
            }
            var names = new[]
            {
                "Hips", "Spine02", "Spine01", "Spine", "neck", "Head",
                "RightShoulder", "RightArm", "RightForeArm", "RightHand",
                "LeftShoulder", "LeftArm"
            };
            var hierarchy = string.Join(
                " | ",
                names.Select(name =>
                {
                    var bone = RequireGraveAttackBone(attackModel, name);
                    return $"{name}:Path={AnimationUtility.CalculateTransformPath(bone, attackModel)}," +
                           $"P={FormatVector(bone.localPosition)},R={FormatVector(bone.localEulerAngles)}," +
                           $"S={FormatVector(bone.localScale)}";
                }));
            Debug.Log(
                $"GraveAttackRigInspection Front={FormatVector(CalculateVisualFront(attackModel))}, " +
                $"Right={FormatVector(attackModel.right)}, Up={FormatVector(attackModel.up)}, " +
                $"Mesh={renderer.sharedMesh.name}, Vertices={renderer.sharedMesh.vertexCount}, " +
                $"BlendShapes={renderer.sharedMesh.blendShapeCount}, BladeIndex={blendShapeIndex}, " +
                $"BladeWeight={(blendShapeIndex >= 0 ? renderer.GetBlendShapeWeight(blendShapeIndex) : -1f):0.###}, " +
                $"AnimatorEnabled={animator != null && animator.enabled}, " +
                $"Controller={animator?.runtimeAnimatorController?.name ?? "None"}, ControllerClips={controllerClips}, " +
                $"BlendShapeBindings={blendShapeBindings}, RuntimeState={runtimeState}, " +
                $"PreviewCycles={GraveAttackPreviewCycles}, PreviewSaved=False, " +
                $"SceneDirtyPreserved={scene.isDirty == sceneWasDirty}, Hierarchy={hierarchy}");
        }

        private static void StartGraveAttackEditModePreview(
            Scene scene,
            Transform attackModel,
            Animator animator,
            AnimationClip clip,
            SkinnedMeshRenderer renderer,
            int blendShapeIndex)
        {
            StopGraveAttackEditModePreview();
            graveAttackPreviewAnimatorWasEnabled = animator.enabled;
            animator.enabled = false;
            clip.SampleAnimation(attackModel.gameObject, 0f);
            renderer.SetBlendShapeWeight(blendShapeIndex, 0f);
            graveAttackPreviewNormalPoses = CaptureLocalPoses(attackModel);
            graveAttackPreviewModel = attackModel;
            graveAttackPreviewAnimator = animator;
            graveAttackPreviewClip = clip;
            graveAttackPreviewRenderer = renderer;
            graveAttackPreviewStartedAt = EditorApplication.timeSinceStartup;
            graveAttackPreviewSceneWasDirty = scene.isDirty;
            graveAttackPreviewScene = scene;
            Selection.activeGameObject = attackModel.gameObject;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }

            EditorApplication.update -= UpdateGraveAttackEditModePreview;
            EditorApplication.update += UpdateGraveAttackEditModePreview;
            SceneView.RepaintAll();
        }

        private static void UpdateGraveAttackEditModePreview()
        {
            if (graveAttackPreviewModel == null || graveAttackPreviewClip == null ||
                graveAttackPreviewAnimator == null || graveAttackPreviewRenderer == null)
            {
                StopGraveAttackEditModePreview();
                return;
            }

            var elapsed = EditorApplication.timeSinceStartup - graveAttackPreviewStartedAt;
            var previewDuration = GraveAttackDuration * GraveAttackPreviewCycles;
            if (elapsed >= previewDuration)
            {
                var sceneDirtyPreserved =
                    graveAttackPreviewScene.IsValid() &&
                    graveAttackPreviewScene.isDirty == graveAttackPreviewSceneWasDirty;
                StopGraveAttackEditModePreview();
                Debug.Log(
                    $"GraveAttackEditModePreviewFinished Cycles={GraveAttackPreviewCycles}, " +
                    $"RestoredNormalArm=True, PreviewSaved=False, SceneDirtyPreserved={sceneDirtyPreserved}");
                return;
            }

            RestoreLocalPoses(graveAttackPreviewNormalPoses);
            var sampleTime = (float)(elapsed % GraveAttackDuration);
            graveAttackPreviewClip.SampleAnimation(graveAttackPreviewModel.gameObject, sampleTime);
            SceneView.RepaintAll();
        }

        private static void StopGraveAttackEditModePreview()
        {
            EditorApplication.update -= UpdateGraveAttackEditModePreview;
            if (graveAttackPreviewNormalPoses != null)
            {
                RestoreLocalPoses(graveAttackPreviewNormalPoses);
            }

            if (graveAttackPreviewRenderer != null && graveAttackPreviewRenderer.sharedMesh != null)
            {
                var blendShapeIndex =
                    graveAttackPreviewRenderer.sharedMesh.GetBlendShapeIndex(GraveAttackBladeBlendShapeName);
                if (blendShapeIndex >= 0)
                {
                    graveAttackPreviewRenderer.SetBlendShapeWeight(blendShapeIndex, 0f);
                }
            }

            if (graveAttackPreviewAnimator != null)
            {
                graveAttackPreviewAnimator.enabled = graveAttackPreviewAnimatorWasEnabled;
            }

            graveAttackPreviewNormalPoses = null;
            graveAttackPreviewModel = null;
            graveAttackPreviewAnimator = null;
            graveAttackPreviewClip = null;
            graveAttackPreviewRenderer = null;
            SceneView.RepaintAll();
        }

        [MenuItem("Bellerophon/Enemies/Grave/Apply Approved Curtain Call Attack")]
        public static void ApplyApprovedGraveCurtainCallAttack()
        {
            var scene = RequireOpenCargoRunScene();
            var graveRoot = RequireRootSceneObject(scene, GraveRootName).transform;
            var attackSlot = graveRoot.Find(GraveAttackSlotName) ??
                throw new InvalidOperationException(GraveAttackSlotName + " is missing.");
            var attackModel = attackSlot.Find(GraveModelName) ??
                throw new InvalidOperationException(GraveAttackSlotName + "/" + GraveModelName + " is missing.");
            var slotState = new RootState(attackSlot.gameObject);
            var modelState = new RootState(attackModel.gameObject);
            var existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(GraveAttackClipAssetPath) ??
                throw new InvalidOperationException("Existing Grave curtain-call attack clip is missing.");
            RestoreGraveAttackApprovedRestPose(attackModel);
            var attackRenderer = RequireGraveAttackBodyRenderer(attackModel);
            var existingBladeIndex = attackRenderer.sharedMesh.GetBlendShapeIndex(GraveAttackBladeBlendShapeName);
            if (existingBladeIndex >= 0)
            {
                attackRenderer.SetBlendShapeWeight(existingBladeIndex, 0f);
            }

            EnsureApprovedGraveScytheBladeMesh(attackModel, existingClip);
            var clip = EnsureApprovedGraveCurtainCallAttackClip(attackModel);
            var controller = EnsureApprovedGraveCurtainCallAttackController(clip);
            var animator = attackModel.GetComponent<Animator>();
            if (animator == null)
            {
                animator = attackModel.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorUtility.SetDirty(animator);

            slotState.AssertUnchanged();
            modelState.AssertUnchanged();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            var metrics = ValidateApprovedGraveCurtainCallAttackScene(scene, writeReport: true);
            Debug.Log("GraveApprovedCurtainCallAttackApplied " + metrics);
        }

        [MenuItem("Bellerophon/Enemies/Grave/Validate Approved Curtain Call Attack")]
        public static void ValidateApprovedGraveCurtainCallAttack()
        {
            var scene = RequireOpenCargoRunScene();
            var metrics = ValidateApprovedGraveCurtainCallAttackScene(scene, writeReport: true);
            Debug.Log("GraveApprovedCurtainCallAttackValidationPassed " + metrics);
        }

        [MenuItem("Bellerophon/Enemies/Grave/Capture Approved Curtain Call Attack")]
        public static void CaptureApprovedGraveCurtainCallAttack()
        {
            var scene = RequireOpenCargoRunScene();
            ValidateApprovedGraveCurtainCallAttackScene(scene, writeReport: false);
            var graveRoot = RequireRootSceneObject(scene, GraveRootName).transform;
            var attackSlot = graveRoot.Find(GraveAttackSlotName) ??
                throw new InvalidOperationException(GraveAttackSlotName + " is missing.");
            var attackModel = attackSlot.Find(GraveModelName) ??
                throw new InvalidOperationException(GraveAttackSlotName + "/" + GraveModelName + " is missing.");
            var animator = attackModel.GetComponent<Animator>() ??
                throw new InvalidOperationException("Grave curtain-call attack Animator is missing.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(GraveAttackClipAssetPath) ??
                throw new InvalidOperationException("Grave curtain-call attack clip is missing.");
            var attackRenderer = RequireGraveAttackBodyRenderer(attackModel);
            var originalAnimatorEnabled = animator.enabled;
            var poses = CaptureLocalPoses(attackModel);
            var player = RequireSceneObject(scene, PlayerName);
            // The user's reference recording uses the enabled Model Cam Game View framing.
            var gameViewCamera = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                .FirstOrDefault(candidate => candidate.enabled && candidate.gameObject.name == "Model Cam") ??
                player.GetComponentsInChildren<Camera>(true)
                .FirstOrDefault(candidate => candidate.enabled) ??
                throw new InvalidOperationException("CargoRunMvp enabled Game View camera is missing.");
            var gameViewCameraObject = gameViewCamera.gameObject;
            var cameraObject = new GameObject("Grave_CurtainCallAttack_CaptureCamera");
            var frames = new Texture2D[GraveAttackCaptureTimes.Length];
            Texture2D sheet = null;
            try
            {
                animator.enabled = false;
                var camera = cameraObject.AddComponent<Camera>();
                camera.CopyFrom(gameViewCamera);
                camera.enabled = false;
                camera.transform.SetPositionAndRotation(
                    gameViewCamera.transform.position,
                    gameViewCamera.transform.rotation);
                var frameWidth = 640;
                var frameHeight = 360;
                for (var i = 0; i < GraveAttackCaptureTimes.Length; i++)
                {
                    RestoreLocalPoses(poses);
                    clip.SampleAnimation(attackModel.gameObject, GraveAttackCaptureTimes[i]);
                    frames[i] = RenderCameraFrame(camera, frameWidth, frameHeight);
                }

                var sheetWidth = frameWidth * frames.Length;
                sheet = new Texture2D(sheetWidth, frameHeight, TextureFormat.RGB24, false);
                sheet.SetPixels(Enumerable.Repeat(new Color(0.08f, 0.09f, 0.10f, 1f), sheetWidth * frameHeight).ToArray());
                for (var i = 0; i < frames.Length; i++)
                {
                    sheet.SetPixels(i * frameWidth, 0, frameWidth, frameHeight, frames[i].GetPixels());
                }

                sheet.Apply();
                var folder = ProjectAbsolutePath(GraveAttackValidationRelativeFolder);
                Directory.CreateDirectory(folder);
                var outputPath = Path.Combine(folder, "Grave_CurtainCall_Attack_Review.png");
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
                Debug.Log(
                    $"GraveApprovedCurtainCallAttackCapturePassed Path={outputPath}, " +
                    $"Times={string.Join("|", GraveAttackCaptureTimes.Select(time => time.ToString("0.##")))}, " +
                    $"Duration={GraveAttackDuration:0.###}, Camera={gameViewCameraObject.name}, " +
                    $"Projection={(gameViewCamera.orthographic ? "Orthographic" : "Perspective")}, ArmScaleChanged=False");
            }
            finally
            {
                RestoreLocalPoses(poses);
                var bladeIndex = attackRenderer.sharedMesh.GetBlendShapeIndex(GraveAttackBladeBlendShapeName);
                if (bladeIndex >= 0)
                {
                    attackRenderer.SetBlendShapeWeight(bladeIndex, 0f);
                }

                animator.enabled = originalAnimatorEnabled;
                foreach (var frame in frames)
                {
                    UnityEngine.Object.DestroyImmediate(frame);
                }

                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        [MenuItem("Bellerophon/Enemies/Grave/Apply Approved Hit Recoil")]
        public static void ApplyApprovedGraveHitRecoil()
        {
            var scene = RequireOpenCargoRunScene();
            var graveRoot = RequireRootSceneObject(scene, GraveRootName).transform;
            var hitSlot = graveRoot.Find(GraveHitSlotName) ??
                throw new InvalidOperationException(GraveHitSlotName + " is missing.");
            var hitModel = hitSlot.Find(GraveModelName) ??
                throw new InvalidOperationException(GraveHitSlotName + "/" + GraveModelName + " is missing.");
            var slotState = new RootState(hitSlot.gameObject);
            var modelState = new RootState(hitModel.gameObject);

            var clip = EnsureApprovedGraveHitRecoilClip(hitModel);
            var controller = EnsureApprovedGraveHitRecoilController(clip);
            var animator = hitModel.GetComponent<Animator>();
            if (animator == null)
            {
                animator = hitModel.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorUtility.SetDirty(animator);

            slotState.AssertUnchanged();
            modelState.AssertUnchanged();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            var metrics = ValidateApprovedGraveHitRecoilScene(scene, writeReport: true);
            Debug.Log("GraveApprovedHitRecoilApplied " + metrics);
        }

        [MenuItem("Bellerophon/Enemies/Grave/Validate Approved Hit Recoil")]
        public static void ValidateApprovedGraveHitRecoil()
        {
            var scene = RequireOpenCargoRunScene();
            var metrics = ValidateApprovedGraveHitRecoilScene(scene, writeReport: true);
            Debug.Log("GraveApprovedHitRecoilValidationPassed " + metrics);
        }

        [MenuItem("Bellerophon/Enemies/Grave/Capture Approved Hit Recoil")]
        public static void CaptureApprovedGraveHitRecoil()
        {
            var scene = RequireOpenCargoRunScene();
            ValidateApprovedGraveHitRecoilScene(scene, writeReport: false);
            var graveRoot = RequireRootSceneObject(scene, GraveRootName).transform;
            var hitSlot = graveRoot.Find(GraveHitSlotName) ??
                throw new InvalidOperationException(GraveHitSlotName + " is missing.");
            var hitModel = hitSlot.Find(GraveModelName) ??
                throw new InvalidOperationException(GraveHitSlotName + "/" + GraveModelName + " is missing.");
            var animator = hitModel.GetComponent<Animator>() ??
                throw new InvalidOperationException("Grave hit-recoil Animator is missing.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(GraveHitClipAssetPath) ??
                throw new InvalidOperationException("Grave hit-recoil clip is missing.");
            var animatorWasEnabled = animator.enabled;
            var poses = CaptureLocalPoses(hitModel);
            var transforms = hitSlot.GetComponentsInChildren<Transform>(true);
            var originalLayers = transforms.Select(target => target.gameObject.layer).ToArray();
            var cameraObject = new GameObject("Grave_HitRecoil_CaptureCamera");
            var lightObject = new GameObject("Grave_HitRecoil_CaptureLight");
            var frames = new Texture2D[GraveHitKeyTimes.Length];
            Texture2D sheet = null;
            try
            {
                animator.enabled = false;
                foreach (var target in transforms)
                {
                    target.gameObject.layer = CaptureLayer;
                }

                RestoreLocalPoses(poses);
                var baseBounds = CalculateVisibleBounds(hitSlot);
                var front = CalculateVisualFront(hitModel);
                var cameraPosition = baseBounds.center + front * 5f;
                var camera = cameraObject.AddComponent<Camera>();
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.96f, 0.965f, 0.97f, 1f);
                camera.orthographic = true;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 80f;
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2.2f;
                light.cullingMask = 1 << CaptureLayer;
                light.transform.rotation = Quaternion.Euler(42f, 28f, 0f);
                const int frameWidth = 420;
                const int frameHeight = 720;
                for (var i = 0; i < GraveHitKeyTimes.Length; i++)
                {
                    RestoreLocalPoses(poses);
                    clip.SampleAnimation(hitModel.gameObject, GraveHitKeyTimes[i]);
                    frames[i] = RenderReviewView(
                        camera,
                        light,
                        baseBounds,
                        cameraPosition,
                        Mathf.Max(baseBounds.extents.y * 1.22f, 1.05f),
                        frameWidth,
                        frameHeight);
                }

                var sheetWidth = frameWidth * frames.Length;
                sheet = new Texture2D(sheetWidth, frameHeight, TextureFormat.RGB24, false);
                sheet.SetPixels(Enumerable.Repeat(new Color(0.08f, 0.09f, 0.10f, 1f), sheetWidth * frameHeight).ToArray());
                for (var i = 0; i < frames.Length; i++)
                {
                    sheet.SetPixels(i * frameWidth, 0, frameWidth, frameHeight, frames[i].GetPixels());
                }

                sheet.Apply();
                var folder = ProjectAbsolutePath(GraveHitValidationRelativeFolder);
                Directory.CreateDirectory(folder);
                var outputPath = Path.Combine(folder, "Grave_Hit_Recoil_Review.png");
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
                Debug.Log(
                    $"GraveApprovedHitRecoilCapturePassed Path={outputPath}, " +
                    $"Times={string.Join("|", GraveHitKeyTimes.Select(time => time.ToString("0.##")))}, " +
                    $"Duration={GraveHitDuration:0.###}, Projection=Orthographic");
            }
            finally
            {
                RestoreLocalPoses(poses);
                animator.enabled = animatorWasEnabled;
                for (var i = 0; i < transforms.Length; i++)
                {
                    transforms[i].gameObject.layer = originalLayers[i];
                }

                foreach (var frame in frames)
                {
                    UnityEngine.Object.DestroyImmediate(frame);
                }

                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        [MenuItem("Bellerophon/Enemies/Grave/Validate Approved Reproduction")]
        public static void ValidateApprovedGraveReproduction()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var metrics = ValidateApprovedReproductionScene(scene, writeReport: true);
            Debug.Log("GraveApprovedReproductionValidationPassed " + metrics);
        }

        [MenuItem("Bellerophon/Enemies/Grave/Capture Approved Reproduction")]
        public static void CaptureApprovedGraveReproduction()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            ValidateApprovedReproductionScene(scene, writeReport: false);
            var graveRoot = RequireSceneObject(scene, GraveRootName).transform;
            var staticSlot = graveRoot.Find(AnimationSlotNames[0]) ??
                throw new InvalidOperationException(AnimationSlotNames[0] + " is missing.");
            var staticModel = staticSlot.Find(GraveModelName) ??
                throw new InvalidOperationException(AnimationSlotNames[0] + "/" + GraveModelName + " is missing.");
            var staticBounds = CalculateVisibleBounds(staticSlot);
            var layoutBounds = CalculateCombinedVisibleBounds(
                AnimationSlotNames.Select(name => graveRoot.Find(name)));
            // The approved reproduction camera must review the textured suit face directly.
            // Player placement remains on the user-approved opposite side and is not changed here.
            var front = CalculateVisualFront(staticModel);
            var right = Vector3.Cross(Vector3.up, front).normalized;
            var transforms = graveRoot.GetComponentsInChildren<Transform>(true);
            var originalLayers = transforms.Select(target => target.gameObject.layer).ToArray();
            var cameraObject = new GameObject("Grave_ApprovedReproduction_CaptureCamera");
            var lightObject = new GameObject("Grave_ApprovedReproduction_CaptureLight");
            Texture2D frontTexture = null;
            Texture2D threeQuarterTexture = null;
            Texture2D layoutTexture = null;
            Texture2D sheet = null;
            try
            {
                foreach (var target in transforms)
                {
                    target.gameObject.layer = CaptureLayer;
                }

                var camera = cameraObject.AddComponent<Camera>();
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.96f, 0.965f, 0.97f, 1f);
                camera.orthographic = true;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 80f;

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2.2f;
                light.cullingMask = 1 << CaptureLayer;
                light.transform.rotation = Quaternion.Euler(42f, 28f, 0f);

                frontTexture = RenderReviewView(
                    camera,
                    light,
                    staticBounds,
                    staticBounds.center + front * 5f,
                    Mathf.Max(staticBounds.extents.y * 1.18f, 1.05f),
                    960,
                    720);
                threeQuarterTexture = RenderReviewView(
                    camera,
                    light,
                    staticBounds,
                    staticBounds.center + (front + right).normalized * 5f + Vector3.up * 0.35f,
                    Mathf.Max(staticBounds.extents.y * 1.22f, 1.08f),
                    960,
                    720);
                layoutTexture = RenderReviewView(
                    camera,
                    light,
                    layoutBounds,
                    layoutBounds.center + front * 28f + Vector3.up * 0.2f,
                    Mathf.Max(layoutBounds.extents.y * 1.32f, layoutBounds.extents.x / (1920f / 540f) * 1.08f),
                    1920,
                    540);

                sheet = new Texture2D(1920, 1260, TextureFormat.RGB24, false);
                var background = Enumerable.Repeat(new Color(0.08f, 0.09f, 0.10f, 1f), 1920 * 1260).ToArray();
                sheet.SetPixels(background);
                sheet.SetPixels(0, 540, 960, 720, frontTexture.GetPixels());
                sheet.SetPixels(960, 540, 960, 720, threeQuarterTexture.GetPixels());
                sheet.SetPixels(0, 0, 1920, 540, layoutTexture.GetPixels());
                sheet.Apply();

                var folder = ProjectAbsolutePath(ApprovedReproductionValidationRelativeFolder);
                Directory.CreateDirectory(folder);
                var outputPath = Path.Combine(folder, "Grave_Approved_Reproduction_Unity_Review.png");
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
                Debug.Log(
                    $"GraveApprovedReproductionCapturePassed Path={outputPath}, Front={FormatVector(front)}, " +
                    $"StaticBounds={FormatVector(staticBounds.size)}, LayoutBounds={FormatVector(layoutBounds.size)}");
            }
            finally
            {
                for (var i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i] != null)
                    {
                        transforms[i].gameObject.layer = originalLayers[i];
                    }
                }

                UnityEngine.Object.DestroyImmediate(frontTexture);
                UnityEngine.Object.DestroyImmediate(threeQuarterTexture);
                UnityEngine.Object.DestroyImmediate(layoutTexture);
                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        [MenuItem("Bellerophon/Enemies/Grave/Capture Approved Back-Facing 180 Rotation")]
        public static void CaptureApprovedGraveBackFacingRotation()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            ValidateApprovedReproductionScene(scene, writeReport: false);
            var graveRoot = RequireSceneObject(scene, GraveRootName).transform;
            var player = RequireSceneObject(scene, PlayerName).transform;
            var staticSlot = graveRoot.Find(AnimationSlotNames[0]) ??
                throw new InvalidOperationException(AnimationSlotNames[0] + " is missing.");
            var staticModel = staticSlot.Find(GraveModelName) ??
                throw new InvalidOperationException(AnimationSlotNames[0] + "/" + GraveModelName + " is missing.");
            var staticBounds = CalculateVisibleBounds(staticSlot);
            var layoutBounds = CalculateCombinedVisibleBounds(
                AnimationSlotNames.Select(name => graveRoot.Find(name)));
            var playerSide = player.position - staticBounds.center;
            playerSide.y = 0f;
            if (playerSide.sqrMagnitude < 0.0001f)
            {
                throw new InvalidOperationException("Player-side capture direction is undefined.");
            }

            playerSide.Normalize();
            var visualFront = CalculateVisualFront(staticModel);
            var frontToPlayerDot = Vector3.Dot(visualFront, playerSide);
            if (frontToPlayerDot < 0.985f)
            {
                throw new InvalidOperationException(
                    $"Grave does not face the unchanged Player after 180-degree rotation. Dot={frontToPlayerDot:0.######}");
            }

            var right = Vector3.Cross(Vector3.up, playerSide).normalized;
            var transforms = graveRoot.GetComponentsInChildren<Transform>(true);
            var originalLayers = transforms.Select(target => target.gameObject.layer).ToArray();
            var cameraObject = new GameObject("Grave_BackFacing180_CaptureCamera");
            var lightObject = new GameObject("Grave_BackFacing180_CaptureLight");
            Texture2D playerViewTexture = null;
            Texture2D threeQuarterTexture = null;
            Texture2D layoutTexture = null;
            Texture2D sheet = null;
            try
            {
                foreach (var target in transforms)
                {
                    target.gameObject.layer = CaptureLayer;
                }

                var camera = cameraObject.AddComponent<Camera>();
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.96f, 0.965f, 0.97f, 1f);
                camera.orthographic = true;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 80f;

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2.2f;
                light.cullingMask = 1 << CaptureLayer;

                playerViewTexture = RenderReviewView(
                    camera,
                    light,
                    staticBounds,
                    staticBounds.center + playerSide * 5f,
                    Mathf.Max(staticBounds.extents.y * 1.18f, 1.05f),
                    960,
                    720);
                threeQuarterTexture = RenderReviewView(
                    camera,
                    light,
                    staticBounds,
                    staticBounds.center + (playerSide + right).normalized * 5f + Vector3.up * 0.35f,
                    Mathf.Max(staticBounds.extents.y * 1.22f, 1.08f),
                    960,
                    720);
                layoutTexture = RenderReviewView(
                    camera,
                    light,
                    layoutBounds,
                    layoutBounds.center + playerSide * 28f + Vector3.up * 0.2f,
                    Mathf.Max(layoutBounds.extents.y * 1.32f, layoutBounds.extents.x / (1920f / 540f) * 1.08f),
                    1920,
                    540);

                sheet = new Texture2D(1920, 1260, TextureFormat.RGB24, false);
                var background = Enumerable.Repeat(new Color(0.08f, 0.09f, 0.10f, 1f), 1920 * 1260).ToArray();
                sheet.SetPixels(background);
                sheet.SetPixels(0, 540, 960, 720, playerViewTexture.GetPixels());
                sheet.SetPixels(960, 540, 960, 720, threeQuarterTexture.GetPixels());
                sheet.SetPixels(0, 0, 1920, 540, layoutTexture.GetPixels());
                sheet.Apply();

                var folder = ProjectAbsolutePath(ApprovedReproductionValidationRelativeFolder);
                Directory.CreateDirectory(folder);
                var outputPath = Path.Combine(folder, "Grave_BackFacing_180_Unity_Review.png");
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
                Debug.Log(
                    $"GraveApprovedBackFacingRotationCapturePassed Path={outputPath}, " +
                    $"ExpectedModelYaw=180, FrontToPlayerDot={frontToPlayerDot:0.######}, " +
                    $"StaticBounds={FormatVector(staticBounds.size)}, LayoutBounds={FormatVector(layoutBounds.size)}");
            }
            finally
            {
                for (var i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i] != null)
                    {
                        transforms[i].gameObject.layer = originalLayers[i];
                    }
                }

                UnityEngine.Object.DestroyImmediate(playerViewTexture);
                UnityEngine.Object.DestroyImmediate(threeQuarterTexture);
                UnityEngine.Object.DestroyImmediate(layoutTexture);
                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private static string ValidateScene(Scene scene, bool writeReport)
        {
            ValidateSourceCopies();
            var longa = RequireSceneObject(scene, LongaRootName);
            var tergo = RequireSceneObject(scene, TergoRootName);
            var accelerando = RequireSceneObject(scene, AccelerandoRootName);
            var graveRoot = RequireSceneObject(scene, GraveRootName);
            var player = RequireSceneObject(scene, PlayerName);
            var graveObject = graveRoot.transform.Find(GraveObjectName);
            if (graveObject == null)
            {
                throw new InvalidOperationException("Grave_00_Static_Review is missing.");
            }

            var model = graveObject.Find(GraveModelName);
            if (model == null)
            {
                throw new InvalidOperationException("Grave_Model is missing.");
            }

            var spacing = Mathf.Abs(longa.transform.position.z - tergo.transform.position.z);
            var expected = new Vector3(accelerando.transform.position.x, accelerando.transform.position.y, accelerando.transform.position.z - spacing);
            var placementError = Vector3.Distance(graveRoot.transform.position, expected);
            if (placementError > 0.002f)
            {
                throw new InvalidOperationException($"Grave Z placement is incorrect. Expected={FormatVector(expected)}, Actual={FormatVector(graveRoot.transform.position)}, Error={placementError:0.######}");
            }

            var cube = FindDescendant(model, "Cube");
            if (cube != null && cube.gameObject.activeInHierarchy && cube.GetComponent<Renderer>()?.enabled != false)
            {
                throw new InvalidOperationException("The non-character Cube helper is visible.");
            }

            var character = FindDescendant(model, "Grave_Body") ?? FindDescendant(model, "char1");
            var renderer = character != null ? character.GetComponent<Renderer>() : null;
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
            {
                throw new InvalidOperationException("The Grave character renderer is not visible.");
            }

            var bounds = CalculateVisibleBounds(graveObject);
            if (bounds.size.y < 1.55f || bounds.size.y > 1.80f)
            {
                throw new InvalidOperationException($"Grave height is outside the approved approximate 1.6m range: {bounds.size.y:0.###}m.");
            }

            var groundError = Mathf.Abs(bounds.min.y - graveRoot.transform.position.y);
            if (groundError > 0.015f)
            {
                throw new InvalidOperationException($"Grave is not aligned to ground. Error={groundError:0.######}m.");
            }

            var front = CalculateVisualFront(model);
            var viewerDirection = player.transform.position - bounds.center;
            viewerDirection.y = 0f;
            var frontDot = Vector3.Dot(front, viewerDirection.normalized);
            var horizontalDistance = viewerDirection.magnitude;
            var distanceError = Mathf.Abs(horizontalDistance - PlayerFrontDistance);
            var playerPositionError = Vector3.Distance(player.transform.position, ApprovedUnchangedPlayerPosition);
            var facingDirection = bounds.center - player.transform.position;
            facingDirection.y = 0f;
            var facingDot = Vector3.Dot(player.transform.forward, facingDirection.normalized);
            if (frontDot < 0.985f || facingDot < 0.985f || playerPositionError > 0.001f)
            {
                throw new InvalidOperationException(
                    $"Grave 180-degree back-facing rotation does not face the unchanged Player start. FrontDot={frontDot:0.######}, " +
                    $"FacingDot={facingDot:0.######}, PlayerPositionError={playerPositionError:0.######}.");
            }

            var metrics =
                $"LongaZ={longa.transform.position.z:0.###}, TergoZ={tergo.transform.position.z:0.###}, Spacing={spacing:0.###}, " +
                $"AccelerandoZ={accelerando.transform.position.z:0.###}, GravePosition={FormatVector(graveRoot.transform.position)}, " +
                $"PlacementError={placementError:0.######}, BoundsSize={FormatVector(bounds.size)}, GroundError={groundError:0.######}, " +
                $"PlayerPosition={FormatVector(player.transform.position)}, FrontDot={frontDot:0.######}, FacingDot={facingDot:0.######}, " +
                $"PlayerPositionError={playerPositionError:0.######}, HorizontalDistance={horizontalDistance:0.######}, " +
                $"DistanceFromLegacy4p2={distanceError:0.######}, Sha256={ExpectedSha256}";

            if (writeReport)
            {
                var folder = ProjectAbsolutePath(ValidationRelativeFolder);
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "GraveStaticPlacementValidation.txt"), metrics + Environment.NewLine);
            }

            return metrics;
        }

        private static void ArrangeAnimationSlotCopies(Transform graveRoot, Transform staticSlot)
        {
            for (var i = 1; i < AnimationSlotNames.Length; i++)
            {
                var existing = graveRoot.Find(AnimationSlotNames[i]);
                if (existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
                }
            }

            staticSlot.name = AnimationSlotNames[0];
            staticSlot.localPosition = Vector3.zero;
            staticSlot.localRotation = Quaternion.identity;
            staticSlot.localScale = Vector3.one;

            for (var i = 1; i < AnimationSlotNames.Length; i++)
            {
                var copy = UnityEngine.Object.Instantiate(staticSlot.gameObject, graveRoot);
                copy.name = AnimationSlotNames[i];
                copy.transform.localPosition = new Vector3(i * AnimationSlotSpacing, 0f, 0f);
                copy.transform.localRotation = staticSlot.localRotation;
                copy.transform.localScale = staticSlot.localScale;
                EditorUtility.SetDirty(copy);
            }
        }

        private static string ValidateAnimationSlotLayout(Scene scene, bool writeReport)
        {
            var placementMetrics = ValidateScene(scene, writeReport: false);
            var graveRoot = RequireSceneObject(scene, GraveRootName).transform;
            if (graveRoot.childCount != AnimationSlotNames.Length)
            {
                throw new InvalidOperationException($"Grave animation slot count mismatch. Expected={AnimationSlotNames.Length}, Actual={graveRoot.childCount}.");
            }

            var staticSlot = graveRoot.Find(AnimationSlotNames[0]) ??
                throw new InvalidOperationException(AnimationSlotNames[0] + " is missing.");
            var staticModel = staticSlot.Find(GraveModelName) ??
                throw new InvalidOperationException(AnimationSlotNames[0] + "/" + GraveModelName + " is missing.");
            var referenceTransformCount = staticModel.GetComponentsInChildren<Transform>(true).Length;
            var referenceRendererCount = staticModel.GetComponentsInChildren<Renderer>(true).Length;
            var referenceCharacter = FindDescendant(staticModel, "Grave_Body") ?? FindDescendant(staticModel, "char1");
            var referenceMesh = referenceCharacter != null ? referenceCharacter.GetComponent<SkinnedMeshRenderer>()?.sharedMesh : null;
            var maxPositionError = 0f;

            for (var i = 0; i < AnimationSlotNames.Length; i++)
            {
                var slot = graveRoot.Find(AnimationSlotNames[i]);
                if (slot == null)
                {
                    throw new InvalidOperationException(AnimationSlotNames[i] + " is missing.");
                }

                var expectedPosition = new Vector3(i * AnimationSlotSpacing, 0f, 0f);
                var positionError = Vector3.Distance(slot.localPosition, expectedPosition);
                maxPositionError = Mathf.Max(maxPositionError, positionError);
                if (positionError > 0.001f || Quaternion.Angle(slot.localRotation, Quaternion.identity) > 0.001f ||
                    Vector3.Distance(slot.localScale, Vector3.one) > 0.001f)
                {
                    throw new InvalidOperationException(
                        $"{AnimationSlotNames[i]} Transform mismatch. ExpectedPosition={FormatVector(expectedPosition)}, " +
                        $"ActualPosition={FormatVector(slot.localPosition)}, PositionError={positionError:0.######}.");
                }

                var model = slot.Find(GraveModelName) ??
                    throw new InvalidOperationException(AnimationSlotNames[i] + "/" + GraveModelName + " is missing.");
                var character = FindDescendant(model, "Grave_Body") ?? FindDescendant(model, "char1");
                var skinnedRenderer = character != null ? character.GetComponent<SkinnedMeshRenderer>() : null;
                if (model.GetComponentsInChildren<Transform>(true).Length != referenceTransformCount ||
                    model.GetComponentsInChildren<Renderer>(true).Length != referenceRendererCount ||
                    skinnedRenderer == null || skinnedRenderer.sharedMesh != referenceMesh)
                {
                    throw new InvalidOperationException(AnimationSlotNames[i] + " is not an exact structural copy of the static Grave model.");
                }

                var cube = FindDescendant(model, "Cube");
                if (cube != null && cube.gameObject.activeSelf)
                {
                    throw new InvalidOperationException(AnimationSlotNames[i] + " has a visible non-character Cube helper.");
                }

                var bounds = CalculateVisibleBounds(slot);
                if (Mathf.Abs(bounds.min.y - graveRoot.position.y) > 0.015f)
                {
                    throw new InvalidOperationException(AnimationSlotNames[i] + " is not aligned to the Grave ground height.");
                }
            }

            var metrics =
                $"SlotCount={AnimationSlotNames.Length}, SlotSpacing={AnimationSlotSpacing:0.###}, " +
                $"MaxPositionError={maxPositionError:0.######}, TransformCountPerModel={referenceTransformCount}, " +
                $"RendererCountPerModel={referenceRendererCount}, SlotNames={string.Join("|", AnimationSlotNames)}, {placementMetrics}";

            if (writeReport)
            {
                var folder = ProjectAbsolutePath(AnimationLayoutValidationRelativeFolder);
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "GraveAnimationSlotLayoutValidation.txt"), metrics + Environment.NewLine);
            }

            return metrics;
        }

        private static string ValidateApprovedReproductionScene(Scene scene, bool writeReport)
        {
            ValidateApprovedArtifactCopies();
            var placementMetrics = ValidateScene(scene, writeReport: false);
            var graveRoot = RequireSceneObject(scene, GraveRootName).transform;
            var approvedModelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ApprovedReproductionModelAssetPath) ??
                throw new InvalidOperationException("Approved Grave reproduction FBX is missing.");
            var approvedBodyAsset = FindDescendant(approvedModelAsset.transform, "Grave_Body");
            var approvedRendererAsset = approvedBodyAsset != null
                ? approvedBodyAsset.GetComponent<SkinnedMeshRenderer>()
                : null;
            if (approvedRendererAsset == null || approvedRendererAsset.sharedMesh == null)
            {
                throw new InvalidOperationException("Approved Grave reproduction FBX does not contain the Grave_Body SkinnedMeshRenderer.");
            }

            var frontMaterial = AssetDatabase.LoadAssetAtPath<Material>(ApprovedFrontMaterialAssetPath) ??
                throw new InvalidOperationException("Approved Grave front material is missing.");
            var textileMaterial = AssetDatabase.LoadAssetAtPath<Material>(ApprovedTextileMaterialAssetPath) ??
                throw new InvalidOperationException("Approved Grave textile material is missing.");
            var frontAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(ApprovedFrontAlbedoAssetPath) ??
                throw new InvalidOperationException("Approved Grave front albedo is missing.");
            var textileAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(ApprovedTextileAlbedoAssetPath) ??
                throw new InvalidOperationException("Approved Grave textile albedo is missing.");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(ApprovedNormalAssetPath) ??
                throw new InvalidOperationException("Approved Grave normal texture is missing.");
            var roughness = AssetDatabase.LoadAssetAtPath<Texture2D>(ApprovedRoughnessAssetPath) ??
                throw new InvalidOperationException("Approved Grave roughness texture is missing.");

            ValidateApprovedMaterial(frontMaterial, frontAlbedo, normal, ApprovedFrontMaterialAssetPath);
            ValidateApprovedMaterial(textileMaterial, textileAlbedo, normal, ApprovedTextileMaterialAssetPath);
            var modelImporter = AssetImporter.GetAtPath(ApprovedReproductionModelAssetPath) as ModelImporter;
            var normalImporter = AssetImporter.GetAtPath(ApprovedNormalAssetPath) as TextureImporter;
            var roughnessImporter = AssetImporter.GetAtPath(ApprovedRoughnessAssetPath) as TextureImporter;
            if (modelImporter == null || !modelImporter.swapUVChannels)
            {
                throw new InvalidOperationException(
                    "Approved Grave model must swap UV channels so GraveReferenceUV is TEXCOORD_0 for URP Lit.");
            }

            if (normalImporter == null || normalImporter.textureType != TextureImporterType.NormalMap)
            {
                throw new InvalidOperationException("Approved Grave fabric normal is not imported as a Normal Map.");
            }

            if (roughnessImporter == null || roughnessImporter.sRGBTexture)
            {
                throw new InvalidOperationException("Approved Grave roughness texture must be imported as linear data.");
            }

            var referenceTransformCount = -1;
            var referenceRendererCount = -1;
            var maxPositionError = 0f;
            var maxGroundError = 0f;
            var maxModelRotationError = 0f;
            var minBounds = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var maxBounds = Vector3.zero;
            foreach (var (slotName, index) in AnimationSlotNames.Select((name, index) => (name, index)))
            {
                var slot = graveRoot.Find(slotName) ?? throw new InvalidOperationException(slotName + " is missing.");
                var expectedPosition = new Vector3(index * AnimationSlotSpacing, 0f, 0f);
                var positionError = Vector3.Distance(slot.localPosition, expectedPosition);
                maxPositionError = Mathf.Max(maxPositionError, positionError);
                if (positionError > 0.001f || Quaternion.Angle(slot.localRotation, Quaternion.identity) > 0.001f ||
                    Vector3.Distance(slot.localScale, Vector3.one) > 0.001f)
                {
                    throw new InvalidOperationException(slotName + " Transform changed during approved model replacement.");
                }

                var model = slot.Find(GraveModelName) ??
                    throw new InvalidOperationException(slotName + "/" + GraveModelName + " is missing.");
                var modelRotationError = Quaternion.Angle(model.localRotation, ApprovedBackFacingModelRotation);
                maxModelRotationError = Mathf.Max(maxModelRotationError, modelRotationError);
                if (Mathf.Abs(model.localPosition.x) > 0.001f ||
                    Mathf.Abs(model.localPosition.z) > 0.001f ||
                    modelRotationError > 0.001f ||
                    Vector3.Distance(model.localScale, Vector3.one) > 0.001f)
                {
                    throw new InvalidOperationException(
                        $"{slotName}/{GraveModelName} must keep grounded Y, zero X/Z, unit scale, and approved 180-degree Y rotation. " +
                        $"Position={FormatVector(model.localPosition)}, RotationError={modelRotationError:0.######}, Scale={FormatVector(model.localScale)}");
                }

                var sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(model.gameObject);
                var usesImportedWalkModel = slotName == GraveWalkSlotName &&
                    string.Equals(sourcePath, GraveWalkApprovedModelAssetPath, StringComparison.Ordinal);
                if (!usesImportedWalkModel &&
                    !string.Equals(sourcePath, ApprovedReproductionModelAssetPath, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"{slotName} does not use the approved Grave FBX. Source={sourcePath}");
                }

                if (FindDescendant(model, "char1") != null)
                {
                    throw new InvalidOperationException(slotName + " still contains the previous char1 Grave model.");
                }

                var body = FindDescendant(model, "Grave_Body");
                var renderer = body != null ? body.GetComponent<SkinnedMeshRenderer>() : null;
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                    (!usesImportedWalkModel && renderer.sharedMesh != approvedRendererAsset.sharedMesh) ||
                    (usesImportedWalkModel &&
                     (renderer.sharedMesh == null ||
                      renderer.sharedMesh.vertexCount != approvedRendererAsset.sharedMesh.vertexCount ||
                      renderer.sharedMesh.subMeshCount != approvedRendererAsset.sharedMesh.subMeshCount)))
                {
                    throw new InvalidOperationException(slotName + " does not use the approved Grave_Body mesh.");
                }

                if (renderer.sharedMesh.subMeshCount != 2 || renderer.sharedMaterials.Length != 2 ||
                    renderer.sharedMaterials[0] != frontMaterial || renderer.sharedMaterials[1] != textileMaterial)
                {
                    throw new InvalidOperationException(slotName + " material slots do not match the approved front/back material mapping.");
                }

                var transformCount = model.GetComponentsInChildren<Transform>(true).Length;
                var rendererCount = model.GetComponentsInChildren<Renderer>(true).Count(candidate => candidate.enabled);
                if (referenceTransformCount < 0)
                {
                    referenceTransformCount = transformCount;
                    referenceRendererCount = rendererCount;
                }
                else if (transformCount != referenceTransformCount || rendererCount != referenceRendererCount)
                {
                    throw new InvalidOperationException(slotName + " is not an exact structural copy of the approved static model.");
                }

                if (rendererCount != 1)
                {
                    throw new InvalidOperationException(slotName + $" must have exactly one visible renderer. Actual={rendererCount}");
                }

                var bounds = CalculateVisibleBounds(slot);
                var groundError = Mathf.Abs(bounds.min.y - graveRoot.position.y);
                maxGroundError = Mathf.Max(maxGroundError, groundError);
                if (bounds.size.x < 0.70f || bounds.size.x > 0.82f ||
                    bounds.size.y < 1.55f || bounds.size.y > 1.65f ||
                    bounds.size.z < 0.45f || bounds.size.z > 0.60f ||
                    groundError > 0.015f)
                {
                    throw new InvalidOperationException(
                        $"{slotName} approved bounds mismatch. Size={FormatVector(bounds.size)}, GroundError={groundError:0.######}");
                }

                minBounds = Vector3.Min(minBounds, bounds.size);
                maxBounds = Vector3.Max(maxBounds, bounds.size);
            }

            if (graveRoot.childCount != AnimationSlotNames.Length)
            {
                throw new InvalidOperationException(
                    $"Grave slot count changed. Expected={AnimationSlotNames.Length}, Actual={graveRoot.childCount}");
            }

            var metrics =
                $"SlotCount={AnimationSlotNames.Length}, MaxPositionError={maxPositionError:0.######}, " +
                $"ExpectedModelYaw=180, MaxModelRotationError={maxModelRotationError:0.######}, " +
                $"MaxGroundError={maxGroundError:0.######}, BoundsMin={FormatVector(minBounds)}, BoundsMax={FormatVector(maxBounds)}, " +
                $"TransformsPerModel={referenceTransformCount}, VisibleRenderersPerModel={referenceRendererCount}, " +
                $"Mesh={approvedRendererAsset.sharedMesh.name}, Vertices={approvedRendererAsset.sharedMesh.vertexCount}, " +
                $"SubMeshes={approvedRendererAsset.sharedMesh.subMeshCount}, Materials={frontMaterial.name}|{textileMaterial.name}, " +
                $"SwapUVChannels={modelImporter.swapUVChannels}, NormalImport={normalImporter.textureType}, RoughnessLinear={!roughnessImporter.sRGBTexture}, " +
                $"Smoothness={ApprovedSmoothness:0.##}, NormalStrength={ApprovedNormalStrength:0.##}, {placementMetrics}";

            if (writeReport)
            {
                var folder = ProjectAbsolutePath(ApprovedReproductionValidationRelativeFolder);
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "GraveApprovedReproductionValidation.txt"), metrics + Environment.NewLine);
            }

            return metrics;
        }

        private static void ValidateGraveWalkSourceCopy()
        {
            var sourcePath = ProjectAbsolutePath(GraveWalkSourceRelativePath);
            var rawAssetPath = ProjectAbsolutePath(GraveWalkRawAssetPath);
            var approvedWalkPath = ProjectAbsolutePath(GraveWalkApprovedModelAssetPath);
            if (!File.Exists(sourcePath) || !File.Exists(rawAssetPath) || !File.Exists(approvedWalkPath))
            {
                throw new FileNotFoundException("Required Grave walk FBX source or combined asset is missing.");
            }

            var sourceHash = ComputeSha256(sourcePath);
            var rawAssetHash = ComputeSha256(rawAssetPath);
            if (!string.Equals(sourceHash, rawAssetHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Raw Grave walk FBX copy differs from the user-selected source. Source={sourceHash}, Asset={rawAssetHash}");
            }
        }

        private static AnimationClip ConfigureImportedGraveWalkModel()
        {
            AssetDatabase.ImportAsset(GraveWalkApprovedModelAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(GraveWalkApprovedModelAssetPath) as ModelImporter ??
                throw new InvalidOperationException("Imported Grave walk ModelImporter is unavailable.");
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.swapUVChannels = true;
            importer.isReadable = true;
            importer.optimizeGameObjects = false;
            importer.resampleCurves = true;
            importer.SaveAndReimport();

            var definitions = importer.defaultClipAnimations;
            if (definitions == null || definitions.Length == 0)
            {
                throw new InvalidOperationException("Imported Grave walk FBX contains no animation clip definitions.");
            }

            var definition = definitions
                .OrderByDescending(candidate => candidate.lastFrame - candidate.firstFrame)
                .First();
            definition.name = GraveWalkImportedClipName;
            definition.loopTime = true;
            definition.loopPose = true;
            definition.cycleOffset = 0f;
            definition.lockRootRotation = true;
            definition.lockRootPositionXZ = true;
            definition.lockRootHeightY = false;
            definition.keepOriginalOrientation = true;
            definition.keepOriginalPositionXZ = true;
            definition.keepOriginalPositionY = true;
            importer.clipAnimations = new[] { definition };
            importer.SaveAndReimport();
            return LoadImportedGraveWalkClip();
        }

        private static AnimationClip LoadImportedGraveWalkClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(GraveWalkApprovedModelAssetPath)
                .OfType<AnimationClip>()
                .Where(candidate => !candidate.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            var clip = clips.FirstOrDefault(candidate => candidate.name == GraveWalkImportedClipName) ??
                clips.OrderByDescending(candidate => candidate.length).FirstOrDefault();
            return clip ?? throw new InvalidOperationException("Imported Grave walk AnimationClip is missing.");
        }

        private static AnimatorController EnsureImportedGraveWalkController(AnimationClip clip)
        {
            EnsureAssetDirectory(GraveWalkImportedControllerAssetPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(GraveWalkImportedControllerAssetPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(GraveWalkImportedControllerAssetPath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states.Select(child => child.state)
                .FirstOrDefault(candidate => candidate.name == "ImportedWalk");
            if (state == null)
            {
                state = stateMachine.AddState("ImportedWalk");
            }

            foreach (var child in stateMachine.states.ToArray())
            {
                if (child.state != state)
                {
                    stateMachine.RemoveState(child.state);
                }
            }

            foreach (var transition in state.transitions.ToArray())
            {
                state.RemoveTransition(transition);
            }

            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static string ValidateGraveWalkFbxReplacementScene(Scene scene, bool writeReport)
        {
            ValidateGraveWalkSourceCopy();
            ValidateApprovedReproductionScene(scene, writeReport: false);
            var importer = AssetImporter.GetAtPath(GraveWalkApprovedModelAssetPath) as ModelImporter ??
                throw new InvalidOperationException("Imported Grave walk ModelImporter is unavailable.");
            if (!importer.importAnimation || importer.animationType != ModelImporterAnimationType.Generic ||
                importer.materialImportMode != ModelImporterMaterialImportMode.None || !importer.swapUVChannels ||
                !importer.isReadable || importer.optimizeGameObjects)
            {
                throw new InvalidOperationException("Imported Grave walk ModelImporter configuration mismatch.");
            }

            var graveRoot = RequireSceneObject(scene, GraveRootName).transform;
            var staticSlot = graveRoot.Find(AnimationSlotNames[0]) ??
                throw new InvalidOperationException(AnimationSlotNames[0] + " is missing.");
            var walkSlot = graveRoot.Find(GraveWalkSlotName) ??
                throw new InvalidOperationException(GraveWalkSlotName + " is missing.");
            var staticModel = staticSlot.Find(GraveModelName) ??
                throw new InvalidOperationException(AnimationSlotNames[0] + "/" + GraveModelName + " is missing.");
            var walkModel = walkSlot.Find(GraveModelName) ??
                throw new InvalidOperationException(GraveWalkSlotName + "/" + GraveModelName + " is missing.");
            var sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(walkModel.gameObject);
            if (!string.Equals(sourcePath, GraveWalkApprovedModelAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Walk slot does not use the combined Grave walk FBX. Source=" + sourcePath);
            }

            var clip = LoadImportedGraveWalkClip();
            if (!string.Equals(AssetDatabase.GetAssetPath(clip), GraveWalkApprovedModelAssetPath, StringComparison.Ordinal) ||
                clip.length < 1.9f || clip.length > 2.2f)
            {
                throw new InvalidOperationException(
                    $"Imported Grave walk clip source or duration mismatch. Path={AssetDatabase.GetAssetPath(clip)}, Length={clip.length:0.######}");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || !settings.loopBlend)
            {
                throw new InvalidOperationException(
                    $"Imported Grave walk clip must loop. LoopTime={settings.loopTime}, LoopBlend={settings.loopBlend}");
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(GraveWalkImportedControllerAssetPath) ??
                throw new InvalidOperationException("Imported Grave walk Animator Controller is missing.");
            var animator = walkModel.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != controller || animator.applyRootMotion ||
                !animator.enabled || animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException("Imported Grave walk Animator configuration mismatch.");
            }

            var defaultState = controller.layers.Length > 0 ? controller.layers[0].stateMachine.defaultState : null;
            if (defaultState == null || defaultState.name != "ImportedWalk" || defaultState.motion != clip)
            {
                throw new InvalidOperationException("Imported Grave walk controller default state mismatch.");
            }

            var frontMaterial = AssetDatabase.LoadAssetAtPath<Material>(ApprovedFrontMaterialAssetPath) ??
                throw new InvalidOperationException("Approved Grave front material is missing.");
            var textileMaterial = AssetDatabase.LoadAssetAtPath<Material>(ApprovedTextileMaterialAssetPath) ??
                throw new InvalidOperationException("Approved Grave textile material is missing.");
            var staticRenderer = FindDescendant(staticModel, "Grave_Body")?.GetComponent<SkinnedMeshRenderer>() ??
                throw new InvalidOperationException("Static Grave comparison renderer is missing.");
            var walkRenderer = FindDescendant(walkModel, "Grave_Body")?.GetComponent<SkinnedMeshRenderer>() ??
                throw new InvalidOperationException("Imported Grave walk renderer is missing.");
            if (walkRenderer.sharedMesh == null || staticRenderer.sharedMesh == null ||
                walkRenderer.sharedMesh.vertexCount != staticRenderer.sharedMesh.vertexCount ||
                walkRenderer.sharedMesh.subMeshCount != 2 ||
                walkRenderer.sharedMaterials.Length != 2 ||
                walkRenderer.sharedMaterials[0] != frontMaterial || walkRenderer.sharedMaterials[1] != textileMaterial)
            {
                throw new InvalidOperationException("Imported Grave walk appearance does not match the approved Grave renderer structure.");
            }

            var restBoundsError = Vector3.Distance(walkRenderer.sharedMesh.bounds.size, staticRenderer.sharedMesh.bounds.size);
            if (restBoundsError > 0.005f)
            {
                throw new InvalidOperationException(
                    $"Imported Grave walk rest mesh differs from the approved Grave mesh. BoundsError={restBoundsError:0.######}");
            }

            var leftFoot = FindDescendant(walkModel, "LeftFoot") ??
                throw new InvalidOperationException("Imported Grave walk rig is missing LeftFoot.");
            var rightFoot = FindDescendant(walkModel, "RightFoot") ??
                throw new InvalidOperationException("Imported Grave walk rig is missing RightFoot.");
            var poses = CaptureLocalPoses(walkModel);
            var rigTransforms = FindDescendant(walkModel, "Grave_Rig")?.GetComponentsInChildren<Transform>(true) ??
                throw new InvalidOperationException("Imported Grave walk rig hierarchy is missing.");
            var animatorWasEnabled = animator.enabled;
            var slotPosition = walkSlot.localPosition;
            var slotRotation = walkSlot.localRotation;
            var slotScale = walkSlot.localScale;
            var modelPosition = walkModel.localPosition;
            var modelRotation = walkModel.localRotation;
            var modelScale = walkModel.localScale;
            var reviewTimes = new[] { 0f, clip.length * 0.25f, clip.length * 0.5f, clip.length * 0.75f, clip.length };
            var minBounds = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var maxBounds = Vector3.zero;
            var maxGroundPenetration = 0f;
            var leftStart = Vector3.zero;
            var rightStart = Vector3.zero;
            var maxLeftFootTravel = 0f;
            var maxRightFootTravel = 0f;
            var startLocalPositions = new Vector3[rigTransforms.Length];
            var startLocalRotations = new Quaternion[rigTransforms.Length];
            var loopPoseError = 0f;
            try
            {
                animator.enabled = false;
                for (var i = 0; i < reviewTimes.Length; i++)
                {
                    RestoreLocalPoses(poses);
                    clip.SampleAnimation(walkModel.gameObject, reviewTimes[i]);
                    if (walkSlot.localPosition != slotPosition || walkSlot.localRotation != slotRotation || walkSlot.localScale != slotScale ||
                        walkModel.localPosition != modelPosition || walkModel.localRotation != modelRotation || walkModel.localScale != modelScale)
                    {
                        throw new InvalidOperationException("Imported Grave walk clip changes the slot or model root Transform.");
                    }

                    var bounds = CalculateVisibleBounds(walkSlot);
                    minBounds = Vector3.Min(minBounds, bounds.size);
                    maxBounds = Vector3.Max(maxBounds, bounds.size);
                    maxGroundPenetration = Mathf.Max(maxGroundPenetration, graveRoot.position.y - bounds.min.y);
                    if (i == 0)
                    {
                        leftStart = leftFoot.position;
                        rightStart = rightFoot.position;
                        for (var transformIndex = 0; transformIndex < rigTransforms.Length; transformIndex++)
                        {
                            startLocalPositions[transformIndex] = rigTransforms[transformIndex].localPosition;
                            startLocalRotations[transformIndex] = rigTransforms[transformIndex].localRotation;
                        }
                    }
                    else
                    {
                        maxLeftFootTravel = Mathf.Max(maxLeftFootTravel, Vector3.Distance(leftStart, leftFoot.position));
                        maxRightFootTravel = Mathf.Max(maxRightFootTravel, Vector3.Distance(rightStart, rightFoot.position));
                    }

                    if (i == reviewTimes.Length - 1)
                    {
                        for (var transformIndex = 0; transformIndex < rigTransforms.Length; transformIndex++)
                        {
                            loopPoseError = Mathf.Max(
                                loopPoseError,
                                Mathf.Max(
                                    Vector3.Distance(startLocalPositions[transformIndex], rigTransforms[transformIndex].localPosition),
                                    Quaternion.Angle(startLocalRotations[transformIndex], rigTransforms[transformIndex].localRotation) * 0.001f));
                        }
                    }
                }
            }
            finally
            {
                RestoreLocalPoses(poses);
                animator.enabled = animatorWasEnabled;
            }

            if (maxLeftFootTravel < 0.04f || maxRightFootTravel < 0.04f ||
                minBounds.y < 1.25f || maxBounds.y > 2.1f ||
                maxGroundPenetration > 0.03f || loopPoseError > 0.03f)
            {
                throw new InvalidOperationException(
                    $"Imported Grave walk sampled motion mismatch. LeftFootTravel={maxLeftFootTravel:0.######}, " +
                    $"RightFootTravel={maxRightFootTravel:0.######}, BoundsMin={FormatVector(minBounds)}, " +
                    $"BoundsMax={FormatVector(maxBounds)}, GroundPenetration={maxGroundPenetration:0.######}, " +
                    $"LoopPoseError={loopPoseError:0.######}");
            }

            var sourceHash = ComputeSha256(ProjectAbsolutePath(GraveWalkSourceRelativePath));
            var combinedHash = ComputeSha256(ProjectAbsolutePath(GraveWalkApprovedModelAssetPath));
            var metrics =
                $"Target={GraveWalkSlotName}/{GraveModelName}, SourceAsset={GraveWalkApprovedModelAssetPath}, " +
                $"SourceHash={sourceHash}, CombinedHash={combinedHash}, Clip={clip.name}, Duration={clip.length:0.###}, " +
                $"CurveCount={AnimationUtility.GetCurveBindings(clip).Length}, LoopTime={settings.loopTime}, " +
                $"LoopBlend={settings.loopBlend}, Vertices={walkRenderer.sharedMesh.vertexCount}, " +
                $"SubMeshes={walkRenderer.sharedMesh.subMeshCount}, RestBoundsError={restBoundsError:0.######}, " +
                $"LeftFootTravel={maxLeftFootTravel:0.######}, RightFootTravel={maxRightFootTravel:0.######}, " +
                $"BoundsMin={FormatVector(minBounds)}, BoundsMax={FormatVector(maxBounds)}, " +
                $"GroundPenetration={maxGroundPenetration:0.######}, LoopPoseError={loopPoseError:0.######}, " +
                $"AnimatorController={controller.name}, ModelRootMotion=0, SlotRootMotion=0";
            if (writeReport)
            {
                var folder = ProjectAbsolutePath(GraveWalkFbxValidationRelativeFolder);
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "GraveWalkFbxReplacementValidation.txt"), metrics + Environment.NewLine);
            }

            return metrics;
        }

        private static AnimationClip EnsureApprovedGraveCurtainCallAttackClip(Transform attackModel)
        {
            var hips = RequireGraveAttackBone(attackModel, "Hips");
            var spine02 = RequireGraveAttackBone(attackModel, "Spine02");
            var spine01 = RequireGraveAttackBone(attackModel, "Spine01");
            var spine = RequireGraveAttackBone(attackModel, "Spine");
            var neck = RequireGraveAttackBone(attackModel, "neck");
            var head = RequireGraveAttackBone(attackModel, "Head");
            var rightShoulder = RequireGraveAttackBone(attackModel, "RightShoulder");
            var rightArm = RequireGraveAttackBone(attackModel, "RightArm");
            var rightForeArm = RequireGraveAttackBone(attackModel, "RightForeArm");
            var rightHand = RequireGraveAttackBone(attackModel, "RightHand");
            var leftShoulder = RequireGraveAttackBone(attackModel, "LeftShoulder");
            var leftArm = RequireGraveAttackBone(attackModel, "LeftArm");
            EnsureAssetDirectory(GraveAttackClipAssetPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(GraveAttackClipAssetPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = "Grave_Attack_CurtainCall_Sweep", frameRate = 60f };
                AssetDatabase.CreateAsset(clip, GraveAttackClipAssetPath);
            }

            clip.ClearCurves();
            clip.legacy = false;
            clip.wrapMode = WrapMode.Loop;
            clip.frameRate = 60f;
            SetGraveAttackRotationCurves(clip, hips, attackModel, new[]
            {
                Vector3.zero, new Vector3(-1f, 3f, -1f), new Vector3(-2f, 7f, -2f),
                new Vector3(-2f, 9f, -2f), new Vector3(1f, 2f, 1f),
                new Vector3(3f, -2f, 1f), new Vector3(4f, -3f, 1.5f),
                new Vector3(4f, -3f, 1.5f), new Vector3(4f, -3f, 1.5f)
            });
            SetGraveAttackRotationCurves(clip, spine02, attackModel, new[]
            {
                Vector3.zero, new Vector3(-2f, 4f, -2f), new Vector3(-3f, 10f, -4f),
                new Vector3(-3f, 12f, -5f), new Vector3(3f, 2f, 1.5f),
                new Vector3(6f, -3f, 2f), new Vector3(7f, -4f, 2.5f),
                new Vector3(7f, -4f, 2.5f), new Vector3(7f, -4f, 2.5f)
            });
            SetGraveAttackRotationCurves(clip, spine01, attackModel, new[]
            {
                Vector3.zero, new Vector3(-1f, 3f, -1f), new Vector3(-2f, 7f, -3f),
                new Vector3(-2f, 9f, -4f), new Vector3(2f, 1f, 1f),
                new Vector3(4f, -2f, 1.5f), new Vector3(5f, -3f, 2f),
                new Vector3(5f, -3f, 2f), new Vector3(5f, -3f, 2f)
            });
            SetGraveAttackRotationCurves(clip, spine, attackModel, new[]
            {
                Vector3.zero, new Vector3(0f, 2f, -1f), new Vector3(-1f, 5f, -2f),
                new Vector3(-1f, 7f, -3f), new Vector3(1f, 1f, 0.5f),
                new Vector3(3f, -1f, 1f), new Vector3(4f, -2f, 1.2f),
                new Vector3(4f, -2f, 1.2f), new Vector3(4f, -2f, 1.2f)
            });
            SetGraveAttackRotationCurves(clip, neck, attackModel, new[]
            {
                Vector3.zero, new Vector3(1f, -1f, 1f), new Vector3(2f, -3f, 2f),
                new Vector3(2f, -4f, 2f), new Vector3(-1f, -1f, -0.5f),
                new Vector3(-2f, 1f, -1f), new Vector3(-3f, 2f, -1f),
                new Vector3(-3f, 2f, -1f), new Vector3(-3f, 2f, -1f)
            });
            SetGraveAttackRotationCurves(clip, head, attackModel, new[]
            {
                Vector3.zero, new Vector3(1f, -1f, 0f), new Vector3(2f, -2f, 1f),
                new Vector3(2f, -3f, 1f), Vector3.zero,
                new Vector3(-1f, 1f, 0f), new Vector3(-1f, 1f, -0.5f),
                new Vector3(-1f, 1f, -0.5f), new Vector3(-1f, 1f, -0.5f)
            });
            SetGraveAttackRotationCurves(clip, rightShoulder, attackModel, new[]
            {
                Vector3.zero, new Vector3(-2f, 1f, 6f), new Vector3(-5f, 3f, 12f),
                new Vector3(-6f, 4f, 14f), new Vector3(15f, -2f, 10f),
                new Vector3(32f, -6f, 5f), new Vector3(22f, -8f, 2f),
                new Vector3(22f, -8f, 2f), new Vector3(22f, -8f, 2f)
            });
            var baseUpperDirection = GraveAttackModelDirection(attackModel, rightForeArm.position - rightArm.position);
            var baseForeArmDirection = GraveAttackModelDirection(attackModel, rightHand.position - rightForeArm.position);
            var presentedUpperDirection = new Vector3(1f, 0.08f, 0.04f).normalized;
            var presentedForeArmDirection = new Vector3(1f, 0.03f, 0.08f).normalized;
            var loweringUpperDirection = new Vector3(0.72f, -0.69f, 0f).normalized;
            var loweringForeArmDirection = new Vector3(0.68f, -0.72f, 0f).normalized;
            var sweepingUpperDirection = new Vector3(0.28f, -0.96f, 0f).normalized;
            var sweepingForeArmDirection = new Vector3(0.22f, -0.97f, 0f).normalized;
            // Preserve the original bent curtain-call silhouette; front clearance is handled without straightening the elbow.
            var pullingUpperDirection = new Vector3(-0.45f, -0.86f, 0.24f).normalized;
            var pullingForeArmDirection = new Vector3(-0.82f, 0.18f, 0.54f).normalized;
            SetGraveAttackAimRotationCurves(clip, rightArm, rightForeArm, attackModel, new[]
            {
                baseUpperDirection,
                Vector3.Lerp(baseUpperDirection, presentedUpperDirection, 0.45f).normalized,
                Vector3.Lerp(baseUpperDirection, presentedUpperDirection, 0.9f).normalized,
                presentedUpperDirection,
                loweringUpperDirection,
                sweepingUpperDirection,
                pullingUpperDirection,
                pullingUpperDirection,
                pullingUpperDirection
            });
            SetGraveAttackAimRotationCurves(clip, rightForeArm, rightHand, attackModel, new[]
            {
                baseForeArmDirection,
                Vector3.Lerp(baseForeArmDirection, presentedForeArmDirection, 0.45f).normalized,
                Vector3.Lerp(baseForeArmDirection, presentedForeArmDirection, 0.9f).normalized,
                presentedForeArmDirection,
                loweringForeArmDirection,
                sweepingForeArmDirection,
                pullingForeArmDirection,
                pullingForeArmDirection,
                pullingForeArmDirection
            });
            SetGraveAttackRotationCurves(clip, rightHand, attackModel, new[]
            {
                Vector3.zero, new Vector3(0f, 0f, 2f), new Vector3(2f, 0f, 5f),
                new Vector3(3f, 0f, 7f), new Vector3(-8f, 2f, 4f),
                new Vector3(-18f, 4f, 1f), new Vector3(-26f, 5f, -2f),
                new Vector3(-26f, 5f, -2f), new Vector3(-26f, 5f, -2f)
            });
            SetGraveAttackRotationCurves(clip, leftShoulder, attackModel, new[]
            {
                Vector3.zero, new Vector3(-2f, -1f, -2f), new Vector3(-4f, -2f, -5f),
                new Vector3(-5f, -3f, -7f), new Vector3(-8f, 1f, -8f),
                new Vector3(-12f, 3f, -10f), new Vector3(-10f, 4f, -8f),
                new Vector3(-10f, 4f, -8f), new Vector3(-10f, 4f, -8f)
            });
            SetGraveAttackRotationCurves(clip, leftArm, attackModel, new[]
            {
                Vector3.zero, new Vector3(-3f, -1f, -3f), new Vector3(-7f, -2f, -7f),
                new Vector3(-8f, -3f, -10f), new Vector3(-12f, 1f, -12f),
                new Vector3(-18f, 4f, -16f), new Vector3(-14f, 5f, -12f),
                new Vector3(-14f, 5f, -12f), new Vector3(-14f, 5f, -12f)
            });
            var preservedTransformCurves = CaptureGraveAttackTransformCurves(clip);
            AddGraveAttackContinuousCurtainCallSlashKeys(
                clip, attackModel, rightShoulder, rightArm, rightForeArm, rightHand);
            AssertGraveAttackUnaffectedTransformCurvesUnchanged(
                preservedTransformCurves, clip, attackModel);
            SetGraveAttackScytheBladeCurve(clip, attackModel);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            settings.cycleOffset = 0f;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimatorController EnsureApprovedGraveCurtainCallAttackController(AnimationClip clip)
        {
            EnsureAssetDirectory(GraveAttackControllerAssetPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(GraveAttackControllerAssetPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(GraveAttackControllerAssetPath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states.Select(child => child.state)
                .FirstOrDefault(candidate => candidate.name == "CurtainCallAttack");
            if (state == null)
            {
                state = stateMachine.AddState("CurtainCallAttack");
            }

            foreach (var child in stateMachine.states.ToArray())
            {
                if (child.state != state)
                {
                    stateMachine.RemoveState(child.state);
                }
            }

            foreach (var transition in state.transitions.ToArray())
            {
                state.RemoveTransition(transition);
            }

            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void RebindApprovedGraveCurtainCallAnimator(
            Animator animator,
            AnimatorController controller,
            AnimationClip clip,
            bool forceControllerReload = true)
        {
            if (controller.layers.Length == 0)
            {
                throw new InvalidOperationException("Grave curtain-call attack controller has no layers.");
            }

            if (forceControllerReload)
            {
                // Reassign through null so the live Animator cannot retain the previously generated clip curves.
                animator.enabled = false;
                animator.runtimeAnimatorController = null;
                animator.runtimeAnimatorController = controller;
            }

            animator.enabled = true;
            animator.Rebind();
            var statePath = controller.layers[0].name + ".CurtainCallAttack";
            animator.Play(Animator.StringToHash(statePath), 0, 0f);
            animator.Update(0f);
            var liveClipMatches = animator.GetCurrentAnimatorClipInfo(0)
                .Any(info => info.clip == clip);
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName(statePath) || !liveClipMatches)
            {
                throw new InvalidOperationException(
                    "Grave curtain-call attack Animator did not bind the regenerated clip.");
            }
        }

        private static SkinnedMeshRenderer RequireGraveAttackBodyRenderer(Transform attackModel)
        {
            var body = FindDescendant(attackModel, "Grave_Body") ??
                throw new InvalidOperationException("Grave curtain-call attack is missing Grave_Body.");
            var renderer = body.GetComponent<SkinnedMeshRenderer>();
            if (renderer == null || renderer.sharedMesh == null)
            {
                throw new InvalidOperationException("Grave curtain-call attack body renderer or mesh is missing.");
            }

            return renderer;
        }

        private static void RestoreGraveAttackApprovedRestPose(Transform attackModel)
        {
            var approvedModel = AssetDatabase.LoadAssetAtPath<GameObject>(ApprovedReproductionModelAssetPath) ??
                throw new InvalidOperationException("Approved Grave reproduction FBX is missing.");
            var boneNames = new[]
            {
                "Hips", "Spine02", "Spine01", "Spine", "neck", "Head",
                "RightShoulder", "RightArm", "RightForeArm", "RightHand",
                "LeftShoulder", "LeftArm"
            };
            foreach (var boneName in boneNames)
            {
                var target = RequireGraveAttackBone(attackModel, boneName);
                var source = FindDescendant(approvedModel.transform, boneName) ??
                    throw new InvalidOperationException("Approved Grave reproduction rig is missing " + boneName + ".");
                target.localRotation = source.localRotation;
            }
        }

        private static void EnsureApprovedGraveScytheBladeMesh(Transform attackModel, AnimationClip clip)
        {
            var renderer = RequireGraveAttackBodyRenderer(attackModel);
            var approvedModel = AssetDatabase.LoadAssetAtPath<GameObject>(ApprovedReproductionModelAssetPath) ??
                throw new InvalidOperationException("Approved Grave reproduction FBX is missing.");
            var approvedRenderer = FindDescendant(approvedModel.transform, "Grave_Body")?.GetComponent<SkinnedMeshRenderer>() ??
                throw new InvalidOperationException("Approved Grave reproduction body renderer is missing.");
            var sourceMesh = approvedRenderer.sharedMesh ??
                throw new InvalidOperationException("Approved Grave reproduction body mesh is missing.");
            if (renderer.bones.Length != sourceMesh.bindposes.Length || sourceMesh.vertexCount == 0)
            {
                throw new InvalidOperationException("Grave attack renderer bones do not match the approved body mesh.");
            }

            var rightArmIndex = Array.FindIndex(renderer.bones, bone => bone != null && bone.name == "RightArm");
            var rightForeArmIndex = Array.FindIndex(renderer.bones, bone => bone != null && bone.name == "RightForeArm");
            var rightHandIndex = Array.FindIndex(renderer.bones, bone => bone != null && bone.name == "RightHand");
            if (rightArmIndex < 0 || rightForeArmIndex < 0 || rightHandIndex < 0)
            {
                throw new InvalidOperationException("Grave attack body mesh is missing right-arm skin bones.");
            }

            var originalMesh = renderer.sharedMesh;
            var animator = attackModel.GetComponent<Animator>();
            var animatorWasEnabled = animator != null && animator.enabled;
            var poses = CaptureLocalPoses(attackModel);
            var generated = UnityEngine.Object.Instantiate(sourceMesh);
            generated.name = "Grave_Attack_ScytheBlade_Body";
            try
            {
                if (animator != null)
                {
                    animator.enabled = false;
                }

                renderer.sharedMesh = sourceMesh;
                RestoreLocalPoses(poses);
                clip.SampleAnimation(attackModel.gameObject, GraveAttackBladeFullTime);
                var baked = new Mesh();
                renderer.BakeMesh(baked, false);
                var posedVertices = baked.vertices;
                var posedNormals = baked.normals;
                UnityEngine.Object.DestroyImmediate(baked);

                var sourceVertices = sourceMesh.vertices;
                var boneWeights = sourceMesh.boneWeights;
                var bindPoses = sourceMesh.bindposes;
                var deltaVertices = new Vector3[sourceVertices.Length];
                var deltaNormals = new Vector3[sourceVertices.Length];
                var deltaTangents = new Vector3[sourceVertices.Length];
                var up = renderer.transform.InverseTransformDirection(attackModel.up).normalized;
                var down = -up;
                var armOrigin = renderer.transform.InverseTransformPoint(renderer.bones[rightArmIndex].position);
                var elbowOrigin = renderer.transform.InverseTransformPoint(renderer.bones[rightForeArmIndex].position);
                var handOrigin = renderer.transform.InverseTransformPoint(renderer.bones[rightHandIndex].position);
                var handDirection = (handOrigin - elbowOrigin).normalized;
                var visualFront = renderer.transform.InverseTransformDirection(
                    CalculateGraveAttackStableFront(attackModel)).normalized;
                var bladeAxis = Vector3.ProjectOnPlane(handDirection, visualFront).normalized;
                if (bladeAxis.sqrMagnitude < 0.5f)
                {
                    bladeAxis = handDirection;
                }
                var tipOrigin = handOrigin + handDirection * 0.16f;
                var changedVertices = 0;
                var maxRequestedDepth = 0f;
                var maxRequestedUpperThickness = 0f;
                var maxRequestedScytheExtension = 0f;
                var maxRequestedScytheTipDrop = 0f;
                var targetBounds = sourceMesh.bounds;
                var lowestArmSurface = float.MaxValue;
                var highestArmSurface = float.MinValue;
                for (var i = 0; i < sourceVertices.Length; i++)
                {
                    var weight = boneWeights[i];
                    var armInfluence =
                        WeightForBone(weight, rightArmIndex) +
                        WeightForBone(weight, rightForeArmIndex) +
                        WeightForBone(weight, rightHandIndex);
                    if (armInfluence < 0.45f)
                    {
                        continue;
                    }

                    CalculatePolylineProgress(
                        posedVertices[i], armOrigin, elbowOrigin, handOrigin, tipOrigin, out var centerLinePoint);
                    var signedHeight = Vector3.Dot(posedVertices[i] - centerLinePoint, up);
                    lowestArmSurface = Mathf.Min(lowestArmSurface, signedHeight);
                    highestArmSurface = Mathf.Max(highestArmSurface, signedHeight);
                }

                var armSurfaceRange = Mathf.Max(highestArmSurface - lowestArmSurface, 0.0001f);
                for (var i = 0; i < sourceVertices.Length; i++)
                {
                    var weight = boneWeights[i];
                    var armInfluence =
                        WeightForBone(weight, rightArmIndex) +
                        WeightForBone(weight, rightForeArmIndex) +
                        WeightForBone(weight, rightHandIndex);
                    if (armInfluence < 0.45f)
                    {
                        targetBounds.Encapsulate(sourceVertices[i]);
                        continue;
                    }

                    var progress = CalculatePolylineProgress(
                        posedVertices[i], armOrigin, elbowOrigin, handOrigin, tipOrigin, out var centerLinePoint);
                    var signedHeight = Vector3.Dot(posedVertices[i] - centerLinePoint, up);
                    var signedFront = Vector3.Dot(posedVertices[i] - centerLinePoint, visualFront);
                    var normalizedHeight = Mathf.Clamp01(
                        (signedHeight - lowestArmSurface) / armSurfaceRange);
                    var bladeForm = Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.InverseLerp(GraveAttackBladeStartProgress, GraveAttackBladeFullProgress, progress));
                    var bladeProgress = Mathf.Clamp01(
                        Mathf.InverseLerp(GraveAttackBladeStartProgress, 0.99f, progress));
                    var heelTaper = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.18f, bladeProgress));
                    var scytheTaperProgress =
                        Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.78f, 0.995f, progress));
                    var bladeBelly = Mathf.Sin(Mathf.PI * bladeProgress);
                    var widthCurve =
                        bladeForm *
                        heelTaper *
                        Mathf.Lerp(0.78f, 1f, Mathf.Max(bladeBelly, 0f)) *
                        (1f - scytheTaperProgress);
                    var requestedDepth =
                        GraveAttackBladeDepth * widthCurve * Mathf.Clamp01(armInfluence);
                    var requestedUpperThickness =
                        GraveAttackUpperThickness * widthCurve * Mathf.Clamp01(armInfluence);
                    var scytheExtensionProgress =
                        Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.48f, 0.99f, progress));
                    var requestedScytheExtension =
                        GraveAttackScytheExtension * scytheExtensionProgress * scytheExtensionProgress *
                        Mathf.Clamp01(armInfluence);
                    var requestedScytheTipDrop =
                        GraveAttackScytheTipDrop * Mathf.Pow(bladeProgress, 1.65f) * bladeForm *
                        Mathf.Clamp01(armInfluence);
                    var requestedBellyDrop =
                        GraveAttackScytheBellyDrop * Mathf.Max(bladeBelly, 0f) * widthCurve *
                        Mathf.Clamp01(armInfluence);
                    var spineHeight = -requestedScytheTipDrop + requestedUpperThickness;
                    var cuttingEdgeHeight =
                        -requestedScytheTipDrop - requestedBellyDrop - requestedDepth;
                    var targetHeight = Mathf.Lerp(
                        cuttingEdgeHeight,
                        spineHeight,
                        Mathf.Pow(normalizedHeight, 0.55f));
                    var verticalDelta =
                        (targetHeight - signedHeight) * bladeForm * Mathf.Clamp01(armInfluence);
                    var frontThicknessScale = Mathf.Lerp(
                        1f,
                        GraveAttackScytheFrontThicknessScale,
                        bladeForm);
                    var frontDelta =
                        -signedFront * (1f - frontThicknessScale) * Mathf.Clamp01(armInfluence);
                    if (Mathf.Abs(verticalDelta) <= 0.0001f && requestedScytheExtension <= 0.0001f &&
                        Mathf.Abs(frontDelta) <= 0.0001f)
                    {
                        targetBounds.Encapsulate(sourceVertices[i]);
                        continue;
                    }

                    var skinMatrix = CalculateBlendedSkinMatrix(renderer, bindPoses, weight);
                    var posedDelta =
                        up * verticalDelta +
                        bladeAxis * requestedScytheExtension +
                        visualFront * frontDelta;
                    deltaVertices[i] = skinMatrix.inverse.MultiplyVector(posedDelta);
                    var posedNormal = posedNormals[i].sqrMagnitude > 0.000001f
                        ? posedNormals[i].normalized
                        : visualFront;
                    var normalFrontDot = Vector3.Dot(posedNormal, visualFront);
                    var normalFlatten =
                        Mathf.SmoothStep(0.15f, 0.7f, Mathf.Abs(normalFrontDot)) *
                        bladeForm * Mathf.Clamp01(armInfluence);
                    var flatNormal = normalFrontDot >= 0f ? visualFront : -visualFront;
                    var targetNormal = Vector3.Slerp(posedNormal, flatNormal, normalFlatten).normalized;
                    deltaNormals[i] = skinMatrix.inverse.MultiplyVector(targetNormal - posedNormal);
                    targetBounds.Encapsulate(sourceVertices[i] + deltaVertices[i]);
                    changedVertices++;
                    maxRequestedDepth = Mathf.Max(maxRequestedDepth, requestedDepth);
                    maxRequestedUpperThickness = Mathf.Max(maxRequestedUpperThickness, requestedUpperThickness);
                    maxRequestedScytheExtension = Mathf.Max(
                        maxRequestedScytheExtension, requestedScytheExtension);
                    maxRequestedScytheTipDrop = Mathf.Max(
                        maxRequestedScytheTipDrop, requestedScytheTipDrop);
                }

                if (changedVertices < 80 || maxRequestedDepth < 0.18f ||
                    maxRequestedUpperThickness < 0.015f || maxRequestedScytheExtension < 0.04f ||
                    maxRequestedScytheTipDrop < 0.04f)
                {
                    throw new InvalidOperationException(
                        $"Grave right-arm scythe-blade deformation is too small. Vertices={changedVertices}, " +
                        $"Depth={maxRequestedDepth:0.######}, UpperThickness={maxRequestedUpperThickness:0.######}, " +
                        $"ScytheExtension={maxRequestedScytheExtension:0.######}, " +
                        $"ScytheTipDrop={maxRequestedScytheTipDrop:0.######}");
                }

                generated.AddBlendShapeFrame(
                    GraveAttackBladeBlendShapeName,
                    100f,
                    deltaVertices,
                    deltaNormals,
                    deltaTangents);
                generated.bounds = targetBounds;
                EnsureAssetDirectory(GraveAttackBladeMeshAssetPath);
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(GraveAttackBladeMeshAssetPath);
                Mesh savedMesh;
                if (existing == null)
                {
                    AssetDatabase.CreateAsset(generated, GraveAttackBladeMeshAssetPath);
                    savedMesh = generated;
                    generated = null;
                }
                else
                {
                    EditorUtility.CopySerialized(generated, existing);
                    EditorUtility.SetDirty(existing);
                    savedMesh = existing;
                }

                renderer.sharedMesh = savedMesh;
                renderer.localBounds = savedMesh.bounds;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                EditorUtility.SetDirty(renderer);
                AssetDatabase.SaveAssets();
            }
            catch
            {
                renderer.sharedMesh = originalMesh;
                throw;
            }
            finally
            {
                RestoreLocalPoses(poses);
                if (animator != null)
                {
                    animator.enabled = animatorWasEnabled;
                }

                if (generated != null)
                {
                    UnityEngine.Object.DestroyImmediate(generated);
                }
            }
        }

        private static void AddGraveAttackContinuousCurtainCallSlashKeys(
            AnimationClip clip,
            Transform attackModel,
            params Transform[] rightLimbBones)
        {
            var poses = CaptureLocalPoses(attackModel);
            var sideRotations = new Quaternion[rightLimbBones.Length];
            var loweringRotations = new Quaternion[rightLimbBones.Length];
            var curtainCallRotations = new Quaternion[rightLimbBones.Length];
            try
            {
                clip.SampleAnimation(attackModel.gameObject, GraveAttackBladeFullTime);
                for (var i = 0; i < rightLimbBones.Length; i++)
                {
                    sideRotations[i] = rightLimbBones[i].localRotation;
                }

                RestoreLocalPoses(poses);
                clip.SampleAnimation(attackModel.gameObject, GraveAttackKeyTimes[4]);
                for (var i = 0; i < rightLimbBones.Length; i++)
                {
                    loweringRotations[i] = rightLimbBones[i].localRotation;
                }

                RestoreLocalPoses(poses);
                clip.SampleAnimation(attackModel.gameObject, GraveAttackCurtainCallHoldTime);
                for (var i = 0; i < rightLimbBones.Length; i++)
                {
                    curtainCallRotations[i] = rightLimbBones[i].localRotation;
                }
            }
            finally
            {
                RestoreLocalPoses(poses);
            }

            var slashTimes = new[]
            {
                GraveAttackBladeFullTime,
                GraveAttackSlashHoldTime,
                Mathf.Lerp(GraveAttackSlashHoldTime, GraveAttackSlashEndTime, 0.25f),
                Mathf.Lerp(GraveAttackSlashHoldTime, GraveAttackSlashEndTime, 0.5f),
                Mathf.Lerp(GraveAttackSlashHoldTime, GraveAttackSlashEndTime, 0.75f),
                GraveAttackSlashEndTime,
                GraveAttackCurtainCallHoldTime
            };
            var slashRotations = slashTimes
                .Select(_ => new Quaternion[rightLimbBones.Length])
                .ToArray();
            for (var poseIndex = 0; poseIndex < slashTimes.Length - 1; poseIndex++)
            {
                var progress = Mathf.InverseLerp(
                    GraveAttackSlashHoldTime,
                    GraveAttackSlashEndTime,
                    slashTimes[poseIndex]);
                for (var boneIndex = 0; boneIndex < rightLimbBones.Length; boneIndex++)
                {
                    slashRotations[poseIndex][boneIndex] = progress <= 0.5f
                        ? Quaternion.Slerp(
                            sideRotations[boneIndex],
                            loweringRotations[boneIndex],
                            progress * 2f)
                        : Quaternion.Slerp(
                            loweringRotations[boneIndex],
                            curtainCallRotations[boneIndex],
                            (progress - 0.5f) * 2f);
                }

            }

            for (var boneIndex = 0; boneIndex < rightLimbBones.Length; boneIndex++)
            {
                slashRotations[slashTimes.Length - 1][boneIndex] =
                    slashRotations[slashTimes.Length - 2][boneIndex];
                for (var poseIndex = 1; poseIndex < slashTimes.Length; poseIndex++)
                {
                    if (Quaternion.Dot(
                            slashRotations[poseIndex - 1][boneIndex],
                            slashRotations[poseIndex][boneIndex]) < 0f)
                    {
                        var rotation = slashRotations[poseIndex][boneIndex];
                        slashRotations[poseIndex][boneIndex] = new Quaternion(
                            -rotation.x, -rotation.y, -rotation.z, -rotation.w);
                    }
                }

                AddGraveAttackContinuousSlashQuaternionKeys(
                    clip,
                    rightLimbBones[boneIndex],
                    attackModel,
                    slashTimes,
                    slashRotations.Select(rotations => rotations[boneIndex]).ToArray());
            }
        }

        private static void AddGraveAttackContinuousSlashQuaternionKeys(
            AnimationClip clip,
            Transform target,
            Transform attackModel,
            IReadOnlyList<float> times,
            IReadOnlyList<Quaternion> rotations)
        {
            var path = AnimationUtility.CalculateTransformPath(target, attackModel);
            var properties = new[]
            {
                "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w"
            };
            for (var component = 0; component < properties.Length; component++)
            {
                var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), properties[component]);
                var curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                    throw new InvalidOperationException("Grave right-limb rotation curve is missing: " + properties[component]);
                for (var keyIndex = curve.length - 1; keyIndex >= 0; keyIndex--)
                {
                    var time = curve.keys[keyIndex].time;
                    if (time >= GraveAttackBladeFullTime - 0.0001f &&
                        time <= GraveAttackCurtainCallHoldTime + 0.0001f)
                    {
                        curve.RemoveKey(keyIndex);
                    }
                }

                for (var poseIndex = 0; poseIndex < times.Count; poseIndex++)
                {
                    var rotation = rotations[poseIndex];
                    var value = component == 0 ? rotation.x :
                        component == 1 ? rotation.y :
                        component == 2 ? rotation.z : rotation.w;
                    curve.AddKey(new Keyframe(times[poseIndex], value));
                }
                for (var keyIndex = 0; keyIndex < curve.length; keyIndex++)
                {
                    var time = curve.keys[keyIndex].time;
                    if (time < GraveAttackBladeFullTime - 0.0001f ||
                        time > GraveAttackCurtainCallHoldTime + 0.0001f)
                    {
                        continue;
                    }

                    AnimationUtility.SetKeyLeftTangentMode(curve, keyIndex, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyRightTangentMode(curve, keyIndex, AnimationUtility.TangentMode.Linear);
                }

                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
        }

        private static void SetGraveAttackScytheBladeCurve(AnimationClip clip, Transform attackModel)
        {
            var renderer = RequireGraveAttackBodyRenderer(attackModel);
            if (renderer.sharedMesh.GetBlendShapeIndex(GraveAttackBladeBlendShapeName) < 0)
            {
                throw new InvalidOperationException("Grave right-arm scythe-blade BlendShape is missing.");
            }

            var curve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.35f, 0f),
                new Keyframe(0.85f, 55f),
                new Keyframe(GraveAttackBladeFullTime, 100f),
                new Keyframe(GraveAttackDuration, 100f));
            for (var i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }

            var path = AnimationUtility.CalculateTransformPath(renderer.transform, attackModel);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(SkinnedMeshRenderer),
                    "blendShape." + GraveAttackBladeBlendShapeName),
                curve);
        }

        private static Dictionary<string, AnimationCurve> CaptureGraveAttackTransformCurves(AnimationClip clip)
        {
            return AnimationUtility.GetCurveBindings(clip)
                .Where(binding => binding.type == typeof(Transform))
                .ToDictionary(
                    binding => binding.path + "\n" + binding.propertyName,
                    binding => CloneCurve(AnimationUtility.GetEditorCurve(clip, binding)));
        }

        private static void AssertGraveAttackUnaffectedTransformCurvesUnchanged(
            IReadOnlyDictionary<string, AnimationCurve> expected,
            AnimationClip clip,
            Transform attackModel)
        {
            var actual = CaptureGraveAttackTransformCurves(clip);
            if (expected.Count != actual.Count || expected.Keys.Any(key => !actual.ContainsKey(key)))
            {
                throw new InvalidOperationException(
                    $"Grave attack Transform curve bindings changed. Before={expected.Count}, After={actual.Count}");
            }

            var allowedPaths = new HashSet<string>
            {
                AnimationUtility.CalculateTransformPath(RequireGraveAttackBone(attackModel, "RightShoulder"), attackModel),
                AnimationUtility.CalculateTransformPath(RequireGraveAttackBone(attackModel, "RightArm"), attackModel),
                AnimationUtility.CalculateTransformPath(RequireGraveAttackBone(attackModel, "RightForeArm"), attackModel),
                AnimationUtility.CalculateTransformPath(RequireGraveAttackBone(attackModel, "RightHand"), attackModel)
            };
            foreach (var pair in expected)
            {
                var path = pair.Key.Substring(0, pair.Key.IndexOf('\n'));
                if (allowedPaths.Contains(path))
                {
                    continue;
                }

                if (!CurvesEqual(pair.Value, actual[pair.Key]))
                {
                    throw new InvalidOperationException("Grave attack Transform curve changed: " + pair.Key.Replace('\n', '/'));
                }
            }
        }

        private static AnimationCurve CloneCurve(AnimationCurve source)
        {
            return new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }

        private static bool CurvesEqual(AnimationCurve left, AnimationCurve right)
        {
            if (left.length != right.length || left.preWrapMode != right.preWrapMode ||
                left.postWrapMode != right.postWrapMode)
            {
                return false;
            }

            for (var i = 0; i < left.length; i++)
            {
                var a = left.keys[i];
                var b = right.keys[i];
                if (Mathf.Abs(a.time - b.time) > 0.000001f ||
                    Mathf.Abs(a.value - b.value) > 0.000001f ||
                    Mathf.Abs(a.inTangent - b.inTangent) > 0.00001f ||
                    Mathf.Abs(a.outTangent - b.outTangent) > 0.00001f ||
                    Mathf.Abs(a.inWeight - b.inWeight) > 0.00001f ||
                    Mathf.Abs(a.outWeight - b.outWeight) > 0.00001f ||
                    a.weightedMode != b.weightedMode)
                {
                    return false;
                }
            }

            return true;
        }

        private static float WeightForBone(BoneWeight weight, int boneIndex)
        {
            var result = 0f;
            if (weight.boneIndex0 == boneIndex) result += weight.weight0;
            if (weight.boneIndex1 == boneIndex) result += weight.weight1;
            if (weight.boneIndex2 == boneIndex) result += weight.weight2;
            if (weight.boneIndex3 == boneIndex) result += weight.weight3;
            return result;
        }

        private static int DominantBoneIndex(BoneWeight weight)
        {
            var index = weight.boneIndex0;
            var value = weight.weight0;
            if (weight.weight1 > value) { index = weight.boneIndex1; value = weight.weight1; }
            if (weight.weight2 > value) { index = weight.boneIndex2; value = weight.weight2; }
            if (weight.weight3 > value) { index = weight.boneIndex3; }
            return index;
        }

        private static Matrix4x4 CalculateBlendedSkinMatrix(
            SkinnedMeshRenderer renderer,
            Matrix4x4[] bindPoses,
            BoneWeight weight)
        {
            var blended = new Matrix4x4();
            AddWeightedSkinMatrix(ref blended, renderer, bindPoses, weight.boneIndex0, weight.weight0);
            AddWeightedSkinMatrix(ref blended, renderer, bindPoses, weight.boneIndex1, weight.weight1);
            AddWeightedSkinMatrix(ref blended, renderer, bindPoses, weight.boneIndex2, weight.weight2);
            AddWeightedSkinMatrix(ref blended, renderer, bindPoses, weight.boneIndex3, weight.weight3);
            return blended;
        }

        private static void AddWeightedSkinMatrix(
            ref Matrix4x4 blended,
            SkinnedMeshRenderer renderer,
            Matrix4x4[] bindPoses,
            int boneIndex,
            float weight)
        {
            if (weight <= 0f)
            {
                return;
            }

            var matrix =
                renderer.transform.worldToLocalMatrix *
                renderer.bones[boneIndex].localToWorldMatrix *
                bindPoses[boneIndex];
            for (var row = 0; row < 4; row++)
            {
                for (var column = 0; column < 4; column++)
                {
                    blended[row, column] += matrix[row, column] * weight;
                }
            }
        }

        private static float CalculatePolylineProgress(
            Vector3 point,
            Vector3 first,
            Vector3 second,
            Vector3 third,
            Vector3 fourth,
            out Vector3 closest)
        {
            var segments = new[] { first, second, third, fourth };
            var lengths = new[]
            {
                Vector3.Distance(first, second),
                Vector3.Distance(second, third),
                Vector3.Distance(third, fourth)
            };
            var totalLength = lengths.Sum();
            var bestDistance = float.MaxValue;
            var bestProgress = 0f;
            closest = first;
            var accumulated = 0f;
            for (var i = 0; i < 3; i++)
            {
                var direction = segments[i + 1] - segments[i];
                var denominator = Mathf.Max(direction.sqrMagnitude, 0.000001f);
                var segmentTime = Mathf.Clamp01(Vector3.Dot(point - segments[i], direction) / denominator);
                var candidate = Vector3.Lerp(segments[i], segments[i + 1], segmentTime);
                var distance = (point - candidate).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    closest = candidate;
                    bestProgress = (accumulated + lengths[i] * segmentTime) / Mathf.Max(totalLength, 0.0001f);
                }

                accumulated += lengths[i];
            }

            return bestProgress;
        }

        private static void SetGraveAttackRotationCurves(
            AnimationClip clip,
            Transform target,
            Transform model,
            IReadOnlyList<Vector3> additiveEulerAngles)
        {
            if (additiveEulerAngles.Count != GraveAttackKeyTimes.Length)
            {
                throw new ArgumentException("Grave attack rotation key count must match attack key times.", nameof(additiveEulerAngles));
            }

            var quaternions = new Quaternion[GraveAttackKeyTimes.Length];
            for (var i = 0; i < quaternions.Length; i++)
            {
                var angles = additiveEulerAngles[i];
                var modelSpaceDelta =
                    Quaternion.AngleAxis(angles.y, model.up) *
                    Quaternion.AngleAxis(angles.x, model.right) *
                    Quaternion.AngleAxis(angles.z, model.forward);
                var value = Quaternion.Inverse(target.parent.rotation) * modelSpaceDelta * target.rotation;
                if (i > 0 && Quaternion.Dot(quaternions[i - 1], value) < 0f)
                {
                    value = new Quaternion(-value.x, -value.y, -value.z, -value.w);
                }

                quaternions[i] = value;
            }

            var path = AnimationUtility.CalculateTransformPath(target, model);
            SetGraveAttackFloatCurve(clip, path, "m_LocalRotation.x", quaternions.Select(value => value.x).ToArray());
            SetGraveAttackFloatCurve(clip, path, "m_LocalRotation.y", quaternions.Select(value => value.y).ToArray());
            SetGraveAttackFloatCurve(clip, path, "m_LocalRotation.z", quaternions.Select(value => value.z).ToArray());
            SetGraveAttackFloatCurve(clip, path, "m_LocalRotation.w", quaternions.Select(value => value.w).ToArray());
        }

        private static void SetGraveAttackAimRotationCurves(
            AnimationClip clip,
            Transform target,
            Transform child,
            Transform model,
            IReadOnlyList<Vector3> modelSpaceDirections)
        {
            if (modelSpaceDirections.Count != GraveAttackKeyTimes.Length)
            {
                throw new ArgumentException("Grave attack aim key count must match attack key times.", nameof(modelSpaceDirections));
            }

            var poses = CaptureLocalPoses(model);
            var quaternions = new Quaternion[GraveAttackKeyTimes.Length];
            try
            {
                for (var i = 0; i < quaternions.Length; i++)
                {
                    RestoreLocalPoses(poses);
                    clip.SampleAnimation(model.gameObject, GraveAttackKeyTimes[i]);
                    var currentDirection = (child.position - target.position).normalized;
                    var requestedDirection = modelSpaceDirections[i];
                    var desiredDirection =
                        (model.right * requestedDirection.x +
                         model.up * requestedDirection.y +
                         model.forward * requestedDirection.z).normalized;
                    var worldRotation = Quaternion.FromToRotation(currentDirection, desiredDirection) * target.rotation;
                    var value = Quaternion.Inverse(target.parent.rotation) * worldRotation;
                    if (i > 0 && Quaternion.Dot(quaternions[i - 1], value) < 0f)
                    {
                        value = new Quaternion(-value.x, -value.y, -value.z, -value.w);
                    }

                    quaternions[i] = value;
                }
            }
            finally
            {
                RestoreLocalPoses(poses);
            }

            var path = AnimationUtility.CalculateTransformPath(target, model);
            SetGraveAttackFloatCurve(clip, path, "m_LocalRotation.x", quaternions.Select(value => value.x).ToArray());
            SetGraveAttackFloatCurve(clip, path, "m_LocalRotation.y", quaternions.Select(value => value.y).ToArray());
            SetGraveAttackFloatCurve(clip, path, "m_LocalRotation.z", quaternions.Select(value => value.z).ToArray());
            SetGraveAttackFloatCurve(clip, path, "m_LocalRotation.w", quaternions.Select(value => value.w).ToArray());
        }

        private static Vector3 GraveAttackModelDirection(Transform model, Vector3 worldDirection)
        {
            var normalized = worldDirection.normalized;
            return new Vector3(
                Vector3.Dot(normalized, model.right),
                Vector3.Dot(normalized, model.up),
                Vector3.Dot(normalized, model.forward)).normalized;
        }

        private static void SetGraveAttackFloatCurve(
            AnimationClip clip,
            string path,
            string propertyName,
            IReadOnlyList<float> values)
        {
            var keys = new Keyframe[GraveAttackKeyTimes.Length];
            for (var i = 0; i < keys.Length; i++)
            {
                keys[i] = new Keyframe(GraveAttackKeyTimes[i], values[i]);
            }

            var curve = new AnimationCurve(keys);
            for (var i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            }

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName),
                curve);
        }

        private static Transform RequireGraveAttackBone(Transform model, string name)
        {
            return FindDescendant(model, name) ??
                throw new InvalidOperationException("Grave curtain-call attack rig is missing " + name + ".");
        }

        private static Vector4 MeasureGraveAttackDropSpeed(
            AnimationClip clip,
            Transform attackModel,
            Transform rightHand,
            Animator animator)
        {
            const float startTime = GraveAttackBladeFullTime;
            const float endTime = 1.65f;
            const float sampleStep = 1f / 240f;
            var poses = CaptureLocalPoses(attackModel);
            var animatorWasEnabled = animator.enabled;
            var burstPeakSpeed = 0f;
            var preBurstDrop = 0f;
            var postBurstDrop = 0f;
            Vector3 startPosition;
            Vector3 previousPosition;
            var previousTime = startTime;
            try
            {
                animator.enabled = false;
                RestoreLocalPoses(poses);
                clip.SampleAnimation(attackModel.gameObject, startTime);
                startPosition = rightHand.position;
                previousPosition = startPosition;
                for (var time = startTime + sampleStep; time <= endTime + 0.0001f; time += sampleStep)
                {
                    RestoreLocalPoses(poses);
                    clip.SampleAnimation(attackModel.gameObject, Mathf.Min(time, endTime));
                    var currentPosition = rightHand.position;
                    var interval = Mathf.Min(time, endTime) - previousTime;
                    var downwardDistance = Mathf.Max(
                        0f,
                        Vector3.Dot(previousPosition - currentPosition, attackModel.up));
                    var downwardSpeed = downwardDistance / Mathf.Max(interval, 0.0001f);
                    var midpoint = previousTime + interval * 0.5f;
                    if (midpoint < GraveAttackSlashHoldTime)
                    {
                        preBurstDrop += downwardDistance;
                    }
                    else if (midpoint <= GraveAttackSlashEndTime)
                    {
                        burstPeakSpeed = Mathf.Max(burstPeakSpeed, downwardSpeed);
                    }
                    else
                    {
                        postBurstDrop += downwardDistance;
                    }

                    previousPosition = currentPosition;
                    previousTime = Mathf.Min(time, endTime);
                }

                var totalDrop = Mathf.Max(0f, Vector3.Dot(startPosition - previousPosition, attackModel.up));
                var preBurstAverage =
                    preBurstDrop / (GraveAttackSlashHoldTime - startTime);
                var postBurstAverage =
                    postBurstDrop / (endTime - GraveAttackSlashEndTime);
                var overallAverage = totalDrop / (endTime - startTime);
                return new Vector4(burstPeakSpeed, preBurstAverage, postBurstAverage, overallAverage);
            }
            finally
            {
                RestoreLocalPoses(poses);
                animator.enabled = animatorWasEnabled;
            }
        }

        private static Vector4 MeasureGraveAttackSlashContinuity(
            AnimationClip clip,
            Transform attackModel,
            Transform rightHand,
            Animator animator)
        {
            const float sampleStep = 1f / 240f;
            var poses = CaptureLocalPoses(attackModel);
            var animatorWasEnabled = animator.enabled;
            var pathLength = 0f;
            var minimumSpeed = float.PositiveInfinity;
            var peakSpeed = 0f;
            try
            {
                animator.enabled = false;
                RestoreLocalPoses(poses);
                clip.SampleAnimation(attackModel.gameObject, GraveAttackSlashHoldTime);
                var startPosition = rightHand.position;
                var previousPosition = startPosition;
                var previousTime = GraveAttackSlashHoldTime;
                for (var time = GraveAttackSlashHoldTime + sampleStep;
                     time <= GraveAttackSlashEndTime + 0.0001f;
                     time += sampleStep)
                {
                    var sampleTime = Mathf.Min(time, GraveAttackSlashEndTime);
                    RestoreLocalPoses(poses);
                    clip.SampleAnimation(attackModel.gameObject, sampleTime);
                    var currentPosition = rightHand.position;
                    var interval = sampleTime - previousTime;
                    var distance = Vector3.Distance(previousPosition, currentPosition);
                    var speed = distance / Mathf.Max(interval, 0.0001f);
                    pathLength += distance;
                    minimumSpeed = Mathf.Min(minimumSpeed, speed);
                    peakSpeed = Mathf.Max(peakSpeed, speed);
                    previousPosition = currentPosition;
                    previousTime = sampleTime;
                }

                var duration = GraveAttackSlashEndTime - GraveAttackSlashHoldTime;
                var averageSpeed = pathLength / duration;
                var directDistance = Vector3.Distance(startPosition, previousPosition);
                var pathEfficiency = directDistance / Mathf.Max(pathLength, 0.0001f);
                return new Vector4(peakSpeed, minimumSpeed, averageSpeed, pathEfficiency);
            }
            finally
            {
                RestoreLocalPoses(poses);
                animator.enabled = animatorWasEnabled;
            }
        }

        private static Vector2 MeasureGraveAttackArmIntegrity(
            AnimationClip clip,
            Transform attackModel,
            Transform rightArm,
            Transform rightForeArm,
            Transform rightHand,
            SkinnedMeshRenderer renderer,
            Animator animator,
            int bladeIndex)
        {
            const int sampleCount = 24;
            var poses = CaptureLocalPoses(attackModel);
            var animatorWasEnabled = animator.enabled;
            var originalBladeWeight = renderer.GetBlendShapeWeight(bladeIndex);
            var weights = renderer.sharedMesh.boneWeights;
            var rightForeArmIndex = Array.FindIndex(renderer.bones, bone => bone != null && bone.name == "RightForeArm");
            var rightHandIndex = Array.FindIndex(renderer.bones, bone => bone != null && bone.name == "RightHand");
            var torsoNames = new HashSet<string> { "Hips", "Spine02", "Spine01", "Spine", "neck" };
            var torsoIndices = renderer.bones
                .Select((bone, index) => new { bone, index })
                .Where(item => item.bone != null && torsoNames.Contains(item.bone.name))
                .Select(item => item.index)
                .ToArray();
            var front = renderer.transform.InverseTransformDirection(
                CalculateGraveAttackStableFront(attackModel)).normalized;
            var right = renderer.transform.InverseTransformDirection(attackModel.right).normalized;
            var up = renderer.transform.InverseTransformDirection(attackModel.up).normalized;
            var maxElbowBend = 0f;
            var minimumTorsoFrontClearance = float.PositiveInfinity;
            var baked = new Mesh();
            try
            {
                animator.enabled = false;
                renderer.SetBlendShapeWeight(bladeIndex, 100f);
                for (var sampleIndex = 0; sampleIndex <= sampleCount; sampleIndex++)
                {
                    var sampleTime = Mathf.Lerp(
                        GraveAttackBladeFullTime,
                        GraveAttackCurtainCallHoldTime,
                        sampleIndex / (float)sampleCount);
                    RestoreLocalPoses(poses);
                    clip.SampleAnimation(attackModel.gameObject, sampleTime);
                    var upperDirection = (rightForeArm.position - rightArm.position).normalized;
                    var foreArmDirection = (rightHand.position - rightForeArm.position).normalized;
                    maxElbowBend = Mathf.Max(
                        maxElbowBend,
                        Vector3.Angle(upperDirection, foreArmDirection));

                    renderer.BakeMesh(baked, false);
                    var vertices = baked.vertices;
                    var torsoRightMin = float.PositiveInfinity;
                    var torsoRightMax = float.NegativeInfinity;
                    var torsoUpMin = float.PositiveInfinity;
                    var torsoUpMax = float.NegativeInfinity;
                    var torsoFrontMax = float.NegativeInfinity;
                    for (var vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
                    {
                        var torsoInfluence = 0f;
                        foreach (var torsoIndex in torsoIndices)
                        {
                            torsoInfluence += WeightForBone(weights[vertexIndex], torsoIndex);
                        }

                        if (torsoInfluence < 0.65f)
                        {
                            continue;
                        }

                        var vertex = vertices[vertexIndex];
                        var lateral = Vector3.Dot(vertex, right);
                        var vertical = Vector3.Dot(vertex, up);
                        torsoRightMin = Mathf.Min(torsoRightMin, lateral);
                        torsoRightMax = Mathf.Max(torsoRightMax, lateral);
                        torsoUpMin = Mathf.Min(torsoUpMin, vertical);
                        torsoUpMax = Mathf.Max(torsoUpMax, vertical);
                        torsoFrontMax = Mathf.Max(torsoFrontMax, Vector3.Dot(vertex, front));
                    }

                    for (var vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
                    {
                        var distalInfluence =
                            WeightForBone(weights[vertexIndex], rightForeArmIndex) +
                            WeightForBone(weights[vertexIndex], rightHandIndex);
                        if (distalInfluence < 0.55f)
                        {
                            continue;
                        }

                        var vertex = vertices[vertexIndex];
                        var lateral = Vector3.Dot(vertex, right);
                        var vertical = Vector3.Dot(vertex, up);
                        if (lateral < torsoRightMin || lateral > torsoRightMax ||
                            vertical < torsoUpMin || vertical > torsoUpMax)
                        {
                            continue;
                        }

                        minimumTorsoFrontClearance = Mathf.Min(
                            minimumTorsoFrontClearance,
                            Vector3.Dot(vertex, front) - torsoFrontMax);
                    }
                }

                if (float.IsPositiveInfinity(minimumTorsoFrontClearance))
                {
                    minimumTorsoFrontClearance = 1f;
                }

                return new Vector2(maxElbowBend, minimumTorsoFrontClearance);
            }
            finally
            {
                RestoreLocalPoses(poses);
                renderer.SetBlendShapeWeight(bladeIndex, originalBladeWeight);
                animator.enabled = animatorWasEnabled;
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static Vector4 MeasureGraveAttackBladeFrontFacing(
            AnimationClip clip,
            Transform attackModel,
            SkinnedMeshRenderer renderer,
            Animator animator,
            int bladeIndex)
        {
            var sampleTimes = new[]
            {
                GraveAttackSlashHoldTime,
                (GraveAttackSlashHoldTime + GraveAttackSlashEndTime) * 0.5f,
                GraveAttackSlashEndTime,
                GraveAttackCurtainCallHoldTime
            };
            var poses = CaptureLocalPoses(attackModel);
            var animatorWasEnabled = animator.enabled;
            var originalBladeWeight = renderer.GetBlendShapeWeight(bladeIndex);
            var weights = renderer.sharedMesh.boneWeights;
            var rightArmIndex = Array.FindIndex(renderer.bones, bone => bone != null && bone.name == "RightArm");
            var rightForeArmIndex = Array.FindIndex(renderer.bones, bone => bone != null && bone.name == "RightForeArm");
            var rightHandIndex = Array.FindIndex(renderer.bones, bone => bone != null && bone.name == "RightHand");
            var front = renderer.transform.InverseTransformDirection(
                CalculateGraveAttackStableFront(attackModel)).normalized;
            var sampleFacings = new float[sampleTimes.Length];
            try
            {
                animator.enabled = false;
                for (var sampleIndex = 0; sampleIndex < sampleTimes.Length; sampleIndex++)
                {
                    var sampleTime = sampleTimes[sampleIndex];
                    RestoreLocalPoses(poses);
                    clip.SampleAnimation(attackModel.gameObject, sampleTime);
                    renderer.SetBlendShapeWeight(bladeIndex, 100f);
                    var baked = new Mesh();
                    renderer.BakeMesh(baked, false);
                    var normals = baked.normals;
                    var sampleFacingSum = 0f;
                    var sampleCount = 0;
                    for (var vertexIndex = 0; vertexIndex < normals.Length; vertexIndex++)
                    {
                        var armInfluence =
                            WeightForBone(weights[vertexIndex], rightArmIndex) +
                            WeightForBone(weights[vertexIndex], rightForeArmIndex) +
                            WeightForBone(weights[vertexIndex], rightHandIndex);
                        if (armInfluence < 0.45f)
                        {
                            continue;
                        }

                        sampleFacingSum += Mathf.Abs(Vector3.Dot(normals[vertexIndex].normalized, front));
                        sampleCount++;
                    }

                    UnityEngine.Object.DestroyImmediate(baked);
                    if (sampleCount == 0)
                    {
                        throw new InvalidOperationException("Grave scythe front-facing sample has no right-arm vertices.");
                    }

                    var sampleFacing = sampleFacingSum / sampleCount;
                    sampleFacings[sampleIndex] = sampleFacing;
                }

                return new Vector4(
                    sampleFacings[0], sampleFacings[1], sampleFacings[2], sampleFacings[3]);
            }
            finally
            {
                RestoreLocalPoses(poses);
                renderer.SetBlendShapeWeight(bladeIndex, originalBladeWeight);
                animator.enabled = animatorWasEnabled;
            }
        }

        private static string ValidateApprovedGraveCurtainCallAttackScene(Scene scene, bool writeReport)
        {
            var graveRoot = RequireRootSceneObject(scene, GraveRootName).transform;
            var attackSlot = graveRoot.Find(GraveAttackSlotName) ??
                throw new InvalidOperationException(GraveAttackSlotName + " is missing.");
            var attackModel = attackSlot.Find(GraveModelName) ??
                throw new InvalidOperationException(GraveAttackSlotName + "/" + GraveModelName + " is missing.");
            var renderer = RequireGraveAttackBodyRenderer(attackModel);
            var bladeMesh = AssetDatabase.LoadAssetAtPath<Mesh>(GraveAttackBladeMeshAssetPath) ??
                throw new InvalidOperationException("Grave right-arm scythe-blade mesh asset is missing.");
            if (renderer.sharedMesh != bladeMesh || bladeMesh.subMeshCount != 2)
            {
                throw new InvalidOperationException("Grave attack slot does not use the approved scythe-blade body mesh.");
            }

            var bladeIndex = bladeMesh.GetBlendShapeIndex(GraveAttackBladeBlendShapeName);
            if (bladeIndex < 0 || bladeMesh.blendShapeCount != 1)
            {
                throw new InvalidOperationException(
                    $"Grave attack mesh BlendShape mismatch. Count={bladeMesh.blendShapeCount}, Index={bladeIndex}");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(GraveAttackClipAssetPath) ??
                throw new InvalidOperationException("Grave curtain-call attack clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(GraveAttackControllerAssetPath) ??
                throw new InvalidOperationException("Grave curtain-call attack controller is missing.");
            var animator = attackModel.GetComponent<Animator>() ??
                throw new InvalidOperationException("Grave curtain-call attack Animator is missing.");
            if (animator.runtimeAnimatorController != controller || animator.applyRootMotion || !animator.enabled ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException("Grave curtain-call attack Animator configuration mismatch.");
            }

            var defaultState = controller.layers.Length > 0 ? controller.layers[0].stateMachine.defaultState : null;
            if (defaultState == null || defaultState.name != "CurtainCallAttack" || defaultState.motion != clip ||
                Mathf.Abs(defaultState.speed - 1f) > 0.0001f)
            {
                throw new InvalidOperationException("Grave curtain-call attack controller default state mismatch.");
            }

            RebindApprovedGraveCurtainCallAnimator(animator, controller, clip, forceControllerReload: false);
            var liveStatePath = controller.layers[0].name + ".CurtainCallAttack";
            var liveClipMatches = animator.GetCurrentAnimatorClipInfo(0)
                .Any(info => info.clip == clip);
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName(liveStatePath) || !liveClipMatches)
            {
                throw new InvalidOperationException(
                    "Grave curtain-call attack live Animator is not playing the saved attack clip.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || !settings.loopBlend || Mathf.Abs(clip.length - GraveAttackDuration) > 0.001f)
            {
                throw new InvalidOperationException(
                    $"Grave curtain-call attack loop configuration mismatch. Length={clip.length:0.######}, " +
                    $"LoopTime={settings.loopTime}, LoopBlend={settings.loopBlend}");
            }

            var animatedBones = new[]
            {
                RequireGraveAttackBone(attackModel, "Hips"),
                RequireGraveAttackBone(attackModel, "Spine02"),
                RequireGraveAttackBone(attackModel, "Spine01"),
                RequireGraveAttackBone(attackModel, "Spine"),
                RequireGraveAttackBone(attackModel, "neck"),
                RequireGraveAttackBone(attackModel, "Head"),
                RequireGraveAttackBone(attackModel, "RightShoulder"),
                RequireGraveAttackBone(attackModel, "RightArm"),
                RequireGraveAttackBone(attackModel, "RightForeArm"),
                RequireGraveAttackBone(attackModel, "RightHand"),
                RequireGraveAttackBone(attackModel, "LeftShoulder"),
                RequireGraveAttackBone(attackModel, "LeftArm")
            };
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var transformBindings = bindings.Where(binding => binding.type == typeof(Transform)).ToArray();
            var bladePath = AnimationUtility.CalculateTransformPath(renderer.transform, attackModel);
            var bladeBindings = bindings.Where(binding =>
                binding.type == typeof(SkinnedMeshRenderer) &&
                binding.path == bladePath &&
                binding.propertyName == "blendShape." + GraveAttackBladeBlendShapeName).ToArray();
            if (bindings.Length != animatedBones.Length * 4 + 1 ||
                transformBindings.Length != animatedBones.Length * 4 || bladeBindings.Length != 1 ||
                transformBindings.Any(binding =>
                    !binding.propertyName.StartsWith("m_LocalRotation.", StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    $"Grave curtain-call attack must contain the preserved rig curves and one blade curve. " +
                    $"CurveCount={bindings.Length}, Transform={transformBindings.Length}, Blade={bladeBindings.Length}");
            }

            var expectedPaths = new HashSet<string>(animatedBones.Select(bone =>
                AnimationUtility.CalculateTransformPath(bone, attackModel)));
            if (transformBindings.Any(binding => !expectedPaths.Contains(binding.path)))
            {
                throw new InvalidOperationException("Grave curtain-call attack contains a curve outside the approved rig bones.");
            }

            var bladeCurve = AnimationUtility.GetEditorCurve(clip, bladeBindings[0]);
            if (bladeCurve == null || Mathf.Abs(bladeCurve.Evaluate(0f)) > 0.001f ||
                Mathf.Abs(bladeCurve.Evaluate(0.35f)) > 0.001f ||
                bladeCurve.Evaluate(0.85f) < 50f || bladeCurve.Evaluate(GraveAttackBladeFullTime) < 99.9f ||
                bladeCurve.Evaluate(GraveAttackSlashEndTime) < 99.9f ||
                bladeCurve.Evaluate(1.65f) < 99.9f || bladeCurve.Evaluate(2.35f) < 99.9f ||
                bladeCurve.Evaluate(GraveAttackDuration) < 99.9f)
            {
                throw new InvalidOperationException(
                    "Grave right-arm blade must form during side extension and stay fully formed through the final frame.");
            }

            var rightArm = animatedBones[7];
            var rightForeArm = animatedBones[8];
            var rightHand = animatedBones[9];
            var chest = animatedBones[3];
            var poses = CaptureLocalPoses(attackModel);
            var animatorWasEnabled = animator.enabled;
            var slotPosition = attackSlot.localPosition;
            var slotRotation = attackSlot.localRotation;
            var slotScale = attackSlot.localScale;
            var modelPosition = attackModel.localPosition;
            var modelRotation = attackModel.localRotation;
            var modelScale = attackModel.localScale;
            var baseScales = animatedBones.Select(bone => bone.localScale).ToArray();
            var startRotations = new Quaternion[animatedBones.Length];
            // The final frame must preserve the authored 2.35-second curtain-call pose instead of returning upward.
            var curtainCallRotations = new Quaternion[animatedBones.Length];
            var elbowPositions = new Vector3[GraveAttackKeyTimes.Length];
            var handPositions = new Vector3[GraveAttackKeyTimes.Length];
            var chestPositions = new Vector3[GraveAttackKeyTimes.Length];
            var elbowChestLocalPositions = new Vector3[GraveAttackKeyTimes.Length];
            var handChestLocalPositions = new Vector3[GraveAttackKeyTimes.Length];
            var minBounds = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var maxBounds = Vector3.zero;
            var maxGroundPenetration = 0f;
            var maxArmScaleError = 0f;
            var maxRightArmAngle = 0f;
            var maxBodyAngle = 0f;
            var finalCurtainCallPoseError = 0f;
            var maxBladeDownward = 0f;
            var maxUpperThicknessRise = 0f;
            var maxBladeBelowAxis = 0f;
            var maxSpineAboveAxis = 0f;
            var maxScytheTipDrop = 0f;
            var maxScytheExtension = 0f;
            var scytheAxialSpan = float.MinValue;
            var scytheExtensionDistance = 0f;
            var scytheTipAverageDrop = 0f;
            var scytheTipThicknessRatio = float.MaxValue;
            var scytheFrontThicknessRatio = float.MaxValue;
            var scytheFrontCenterShift = float.MaxValue;
            var scytheBladePlaneAspect = 0f;
            var maxFrontBackDelta = 0f;
            var maxProximalArmDelta = 0f;
            var maxNonArmDelta = 0f;
            var bladeChangedVertices = 0;
            var visualFront = CalculateGraveAttackStableFront(attackModel);
            try
            {
                animator.enabled = false;
                for (var i = 0; i < GraveAttackKeyTimes.Length; i++)
                {
                    RestoreLocalPoses(poses);
                    clip.SampleAnimation(attackModel.gameObject, GraveAttackKeyTimes[i]);
                    if (attackSlot.localPosition != slotPosition || attackSlot.localRotation != slotRotation ||
                        attackSlot.localScale != slotScale || attackModel.localPosition != modelPosition ||
                        attackModel.localRotation != modelRotation || attackModel.localScale != modelScale)
                    {
                        throw new InvalidOperationException("Grave curtain-call attack changes the slot or model root Transform.");
                    }

                    var sampledMesh = new Mesh();
                    renderer.BakeMesh(sampledMesh, false);
                    var bounds = CalculateBakedWorldBounds(renderer, sampledMesh);
                    UnityEngine.Object.DestroyImmediate(sampledMesh);
                    minBounds = Vector3.Min(minBounds, bounds.size);
                    maxBounds = Vector3.Max(maxBounds, bounds.size);
                    maxGroundPenetration = Mathf.Max(maxGroundPenetration, graveRoot.position.y - bounds.min.y);
                    elbowPositions[i] = rightForeArm.position;
                    handPositions[i] = rightHand.position;
                    chestPositions[i] = chest.position;
                    elbowChestLocalPositions[i] = chest.InverseTransformPoint(rightForeArm.position);
                    handChestLocalPositions[i] = chest.InverseTransformPoint(rightHand.position);
                    for (var boneIndex = 0; boneIndex < animatedBones.Length; boneIndex++)
                    {
                        maxArmScaleError = Mathf.Max(
                            maxArmScaleError,
                            Vector3.Distance(baseScales[boneIndex], animatedBones[boneIndex].localScale));
                    }

                    if (i == 0)
                    {
                        for (var boneIndex = 0; boneIndex < animatedBones.Length; boneIndex++)
                        {
                            startRotations[boneIndex] = animatedBones[boneIndex].localRotation;
                        }
                    }
                    else
                    {
                        maxRightArmAngle = Mathf.Max(
                            maxRightArmAngle,
                            Quaternion.Angle(startRotations[7], rightArm.localRotation));
                        maxBodyAngle = Mathf.Max(
                            maxBodyAngle,
                            Quaternion.Angle(startRotations[1], animatedBones[1].localRotation));
                    }

                    if (i == 6)
                    {
                        for (var boneIndex = 0; boneIndex < animatedBones.Length; boneIndex++)
                        {
                            curtainCallRotations[boneIndex] = animatedBones[boneIndex].localRotation;
                        }
                    }

                    if (i == GraveAttackKeyTimes.Length - 1)
                    {
                        for (var boneIndex = 0; boneIndex < animatedBones.Length; boneIndex++)
                        {
                            finalCurtainCallPoseError = Mathf.Max(
                                finalCurtainCallPoseError,
                                Quaternion.Angle(curtainCallRotations[boneIndex], animatedBones[boneIndex].localRotation));
                        }
                    }

                    if (Mathf.Abs(GraveAttackKeyTimes[i] - GraveAttackBladeFullTime) < 0.0001f)
                    {
                        renderer.SetBlendShapeWeight(bladeIndex, 0f);
                        var baseBaked = new Mesh();
                        renderer.BakeMesh(baseBaked, false);
                        renderer.SetBlendShapeWeight(bladeIndex, 100f);
                        var bladeBaked = new Mesh();
                        renderer.BakeMesh(bladeBaked, false);
                        var baseVertices = baseBaked.vertices;
                        var bladeVertices = bladeBaked.vertices;
                        var weights = bladeMesh.boneWeights;
                        var rightArmIndex = Array.FindIndex(renderer.bones, bone => bone != null && bone.name == "RightArm");
                        var rightForeArmIndex = Array.FindIndex(renderer.bones, bone => bone != null && bone.name == "RightForeArm");
                        var rightHandIndex = Array.FindIndex(renderer.bones, bone => bone != null && bone.name == "RightHand");
                        var up = renderer.transform.InverseTransformDirection(attackModel.up).normalized;
                        var front = renderer.transform.InverseTransformDirection(visualFront).normalized;
                        var armOrigin = renderer.transform.InverseTransformPoint(renderer.bones[rightArmIndex].position);
                        var elbowOrigin = renderer.transform.InverseTransformPoint(renderer.bones[rightForeArmIndex].position);
                        var handOrigin = renderer.transform.InverseTransformPoint(renderer.bones[rightHandIndex].position);
                        var handDirection = (handOrigin - elbowOrigin).normalized;
                        var bladeAxis = Vector3.ProjectOnPlane(handDirection, front).normalized;
                        if (bladeAxis.sqrMagnitude < 0.5f)
                        {
                            bladeAxis = handDirection;
                        }

                        var tipOrigin = handOrigin + handDirection * 0.16f;
                        var bladeNeckBaseSum = Vector3.zero;
                        var bladeNeckDeformedSum = Vector3.zero;
                        var bladeTipBaseSum = Vector3.zero;
                        var bladeTipDeformedSum = Vector3.zero;
                        var bladeNeckCount = 0;
                        var bladeTipCount = 0;
                        var bladeTipBaseMin = float.MaxValue;
                        var bladeTipBaseMax = float.MinValue;
                        var bladeTipDeformedMin = float.MaxValue;
                        var bladeTipDeformedMax = float.MinValue;
                        var bladePlaneBaseFrontMin = float.MaxValue;
                        var bladePlaneBaseFrontMax = float.MinValue;
                        var bladePlaneDeformedFrontMin = float.MaxValue;
                        var bladePlaneDeformedFrontMax = float.MinValue;
                        var bladePlaneFrontCount = 0;
                        for (var vertexIndex = 0; vertexIndex < baseVertices.Length; vertexIndex++)
                        {
                            var delta = bladeVertices[vertexIndex] - baseVertices[vertexIndex];
                            var armInfluence =
                                WeightForBone(weights[vertexIndex], rightArmIndex) +
                                WeightForBone(weights[vertexIndex], rightForeArmIndex) +
                                WeightForBone(weights[vertexIndex], rightHandIndex);
                            if (armInfluence >= 0.45f)
                            {
                                var progress = CalculatePolylineProgress(
                                    baseVertices[vertexIndex],
                                    armOrigin,
                                    elbowOrigin,
                                    handOrigin,
                                    tipOrigin,
                                    out var centerLinePoint);
                                var signedHeight = Vector3.Dot(baseVertices[vertexIndex] - centerLinePoint, up);
                                var verticalDelta = Vector3.Dot(delta, up);
                                var deformedSignedHeight = Vector3.Dot(
                                    bladeVertices[vertexIndex] - centerLinePoint,
                                    up);
                                if (delta.sqrMagnitude > 0.000001f)
                                {
                                    bladeChangedVertices++;
                                }

                                if (progress <= GraveAttackBladeStartProgress - 0.02f)
                                {
                                    maxProximalArmDelta = Mathf.Max(maxProximalArmDelta, delta.magnitude);
                                }

                                if (signedHeight < 0f && progress > GraveAttackBladeFullProgress && progress < 0.86f)
                                {
                                    maxBladeDownward = Mathf.Max(maxBladeDownward, -verticalDelta);
                                }

                                if (progress > GraveAttackBladeFullProgress && progress < 0.86f)
                                {
                                    maxBladeBelowAxis = Mathf.Max(maxBladeBelowAxis, -deformedSignedHeight);
                                    maxSpineAboveAxis = Mathf.Max(maxSpineAboveAxis, deformedSignedHeight);
                                }

                                if (signedHeight > 0f && progress > GraveAttackBladeFullProgress && progress < 0.74f)
                                {
                                    maxUpperThicknessRise = Mathf.Max(maxUpperThicknessRise, verticalDelta);
                                }

                                if (progress >= 0.78f)
                                {
                                    maxScytheTipDrop = Mathf.Max(maxScytheTipDrop, -verticalDelta);
                                    maxScytheExtension = Mathf.Max(
                                        maxScytheExtension,
                                        Vector3.Dot(delta, bladeAxis));
                                }

                                if (progress >= 0.58f && progress <= 0.68f)
                                {
                                    bladeNeckBaseSum += baseVertices[vertexIndex];
                                    bladeNeckDeformedSum += bladeVertices[vertexIndex];
                                    bladeNeckCount++;
                                }

                                if (progress >= GraveAttackBladeFullProgress && progress <= 0.78f)
                                {
                                    var baseFrontDepth = Vector3.Dot(
                                        baseVertices[vertexIndex] - centerLinePoint,
                                        front);
                                    var bladeFrontDepth = Vector3.Dot(
                                        bladeVertices[vertexIndex] - centerLinePoint,
                                        front);
                                    bladePlaneBaseFrontMin = Mathf.Min(
                                        bladePlaneBaseFrontMin, baseFrontDepth);
                                    bladePlaneBaseFrontMax = Mathf.Max(
                                        bladePlaneBaseFrontMax, baseFrontDepth);
                                    bladePlaneDeformedFrontMin = Mathf.Min(
                                        bladePlaneDeformedFrontMin, bladeFrontDepth);
                                    bladePlaneDeformedFrontMax = Mathf.Max(
                                        bladePlaneDeformedFrontMax, bladeFrontDepth);
                                    bladePlaneFrontCount++;
                                }

                                if (progress >= 0.92f)
                                {
                                    bladeTipBaseSum += baseVertices[vertexIndex];
                                    bladeTipDeformedSum += bladeVertices[vertexIndex];
                                    bladeTipCount++;
                                    var baseHeight = Vector3.Dot(baseVertices[vertexIndex], up);
                                    var bladeHeight = Vector3.Dot(bladeVertices[vertexIndex], up);
                                    bladeTipBaseMin = Mathf.Min(bladeTipBaseMin, baseHeight);
                                    bladeTipBaseMax = Mathf.Max(bladeTipBaseMax, baseHeight);
                                    bladeTipDeformedMin = Mathf.Min(bladeTipDeformedMin, bladeHeight);
                                    bladeTipDeformedMax = Mathf.Max(bladeTipDeformedMax, bladeHeight);
                                }

                                maxFrontBackDelta = Mathf.Max(
                                    maxFrontBackDelta,
                                    Mathf.Abs(Vector3.Dot(delta, front)));
                            }
                            else
                            {
                                maxNonArmDelta = Mathf.Max(maxNonArmDelta, delta.magnitude);
                            }
                        }

                        if (bladeNeckCount > 0 && bladeTipCount > 0)
                        {
                            var baseNeckCenter = bladeNeckBaseSum / bladeNeckCount;
                            var deformedNeckCenter = bladeNeckDeformedSum / bladeNeckCount;
                            var baseTipCenter = bladeTipBaseSum / bladeTipCount;
                            var deformedTipCenter = bladeTipDeformedSum / bladeTipCount;
                            var baseAxialSpan = Vector3.Dot(baseTipCenter - baseNeckCenter, bladeAxis);
                            scytheAxialSpan = Vector3.Dot(
                                deformedTipCenter - deformedNeckCenter,
                                bladeAxis);
                            scytheExtensionDistance = scytheAxialSpan - baseAxialSpan;
                            scytheTipAverageDrop = -Vector3.Dot(deformedTipCenter - baseTipCenter, up);
                            var baseTipThickness = bladeTipBaseMax - bladeTipBaseMin;
                            var deformedTipThickness = bladeTipDeformedMax - bladeTipDeformedMin;
                            scytheTipThicknessRatio =
                                deformedTipThickness / Mathf.Max(baseTipThickness, 0.0001f);
                        }

                        if (bladePlaneFrontCount > 0)
                        {
                            var baseFrontThickness =
                                bladePlaneBaseFrontMax - bladePlaneBaseFrontMin;
                            var deformedFrontThickness =
                                bladePlaneDeformedFrontMax - bladePlaneDeformedFrontMin;
                            scytheFrontThicknessRatio =
                                deformedFrontThickness / Mathf.Max(baseFrontThickness, 0.0001f);
                            var baseFrontCenter =
                                (bladePlaneBaseFrontMax + bladePlaneBaseFrontMin) * 0.5f;
                            var deformedFrontCenter =
                                (bladePlaneDeformedFrontMax + bladePlaneDeformedFrontMin) * 0.5f;
                            scytheFrontCenterShift = Mathf.Abs(deformedFrontCenter - baseFrontCenter);
                            scytheBladePlaneAspect =
                                (maxBladeBelowAxis + maxSpineAboveAxis) /
                                Mathf.Max(deformedFrontThickness, 0.0001f);
                        }

                        renderer.SetBlendShapeWeight(bladeIndex, bladeCurve.Evaluate(GraveAttackKeyTimes[i]));
                        UnityEngine.Object.DestroyImmediate(baseBaked);
                        UnityEngine.Object.DestroyImmediate(bladeBaked);
                    }
                }
            }
            finally
            {
                RestoreLocalPoses(poses);
                renderer.SetBlendShapeWeight(bladeIndex, 0f);
                animator.enabled = animatorWasEnabled;
            }

            var postAttackResetWeight = renderer.GetBlendShapeWeight(bladeIndex);
            if (Mathf.Abs(postAttackResetWeight) > 0.001f)
            {
                throw new InvalidOperationException(
                    $"Grave right arm did not restore after the attack. BladeWeight={postAttackResetWeight:0.###}");
            }

            var dropSpeedMetrics = MeasureGraveAttackDropSpeed(clip, attackModel, rightHand, animator);
            var peakDropSpeed = dropSpeedMetrics.x;
            var preBurstDropSpeed = dropSpeedMetrics.y;
            var postBurstDropSpeed = dropSpeedMetrics.z;
            var averageDropSpeed = dropSpeedMetrics.w;
            var dropImpulseRatio = peakDropSpeed /
                Mathf.Max(Mathf.Max(preBurstDropSpeed, postBurstDropSpeed), 0.001f);
            var acceleratedDropFrames =
                (GraveAttackSlashEndTime - GraveAttackSlashHoldTime) * clip.frameRate;
            var slashContinuityMetrics = MeasureGraveAttackSlashContinuity(
                clip, attackModel, rightHand, animator);
            var peakSlashSpeed = slashContinuityMetrics.x;
            var minimumSlashSpeed = slashContinuityMetrics.y;
            var averageSlashSpeed = slashContinuityMetrics.z;
            var slashPathEfficiency = slashContinuityMetrics.w;
            var slashSpeedUniformity = minimumSlashSpeed / Mathf.Max(averageSlashSpeed, 0.001f);
            var armIntegrityMetrics = MeasureGraveAttackArmIntegrity(
                clip, attackModel, rightArm, rightForeArm, rightHand, renderer, animator, bladeIndex);
            var maxElbowBend = armIntegrityMetrics.x;
            var minimumTorsoFrontClearance = armIntegrityMetrics.y;
            var bladeFrontFacingMetrics = MeasureGraveAttackBladeFrontFacing(
                clip, attackModel, renderer, animator, bladeIndex);
            var minimumBladeFrontFacing = Mathf.Min(
                Mathf.Min(bladeFrontFacingMetrics.x, bladeFrontFacingMetrics.y),
                Mathf.Min(bladeFrontFacingMetrics.z, bladeFrontFacingMetrics.w));
            var averageBladeFrontFacing =
                (bladeFrontFacingMetrics.x + bladeFrontFacingMetrics.y +
                 bladeFrontFacingMetrics.z + bladeFrontFacingMetrics.w) * 0.25f;
            var lowerBladeDominance = maxBladeBelowAxis / Mathf.Max(maxSpineAboveAxis, 0.001f);
            // Reference scythe blades are long arcs, so judge cutting depth relative to axial length.
            var scytheArcDepthRatio = maxBladeBelowAxis / Mathf.Max(scytheAxialSpan, 0.001f);

            var extendSide = Vector3.Dot(handPositions[3] - handPositions[0], attackModel.right);
            var extendRise = Vector3.Dot(handPositions[3] - handPositions[0], attackModel.up);
            var midSweepDrop = Vector3.Dot(handPositions[3] - handPositions[4], attackModel.up);
            var sweepDrop = Vector3.Dot(handPositions[3] - handPositions[5], attackModel.up);
            var sweepInward = Vector3.Dot(handPositions[3] - handPositions[5], attackModel.right);
            var sweepForwardFromExtension = Mathf.Abs(Vector3.Dot(handPositions[5] - handPositions[3], visualFront));
            var descentDominance = sweepDrop / Mathf.Max(sweepForwardFromExtension, 0.001f);
            var finalHandFromChest = handPositions[6] - chestPositions[6];
            var finalElbowFromChest = elbowPositions[6] - chestPositions[6];
            var finalElbowLateral = Mathf.Abs(Vector3.Dot(finalElbowFromChest, attackModel.right));
            var finalElbowForward = Vector3.Dot(finalElbowFromChest, visualFront);
            var finalLateral = Vector3.Dot(finalHandFromChest, attackModel.right);
            var finalForward = Vector3.Dot(finalHandFromChest, visualFront);
            var finalVertical = Vector3.Dot(finalHandFromChest, attackModel.up);
            var finalForeArm = handPositions[6] - elbowPositions[6];
            var finalForeArmLeftReach = -Vector3.Dot(finalForeArm, attackModel.right);
            var finalForeArmForward = Vector3.Dot(finalForeArm, visualFront);
            var finalLeftwardDominance = finalForeArmLeftReach / Mathf.Max(Mathf.Abs(finalForeArmForward), 0.001f);
            var pullDistanceReduction =
                Vector3.Distance(handPositions[5], chestPositions[5]) -
                Vector3.Distance(handPositions[6], chestPositions[6]);
            var curtainCallHoldDrift = Mathf.Max(
                Vector3.Distance(handChestLocalPositions[4], handChestLocalPositions[5]),
                Mathf.Max(
                    Vector3.Distance(handChestLocalPositions[5], handChestLocalPositions[6]),
                    Vector3.Distance(elbowChestLocalPositions[4], elbowChestLocalPositions[6])));
            if (extendSide < 0.22f || extendRise < 0.18f || midSweepDrop < 0.1f || sweepDrop < 0.18f ||
                sweepInward < 0.8f || sweepForwardFromExtension < 0.35f || sweepForwardFromExtension > 0.7f ||
                finalElbowLateral > 0.12f || finalElbowForward < 0.03f || finalElbowForward > 0.4f ||
                finalLateral > -0.1f || finalLateral < -0.35f || finalForward < 0.08f || finalForward > 0.55f ||
                finalForeArmLeftReach < 0.18f || finalForeArmForward < 0.03f || finalForeArmForward > 0.32f ||
                finalLeftwardDominance < 1.1f ||
                Mathf.Abs(finalVertical) > 0.45f || curtainCallHoldDrift > 0.03f ||
                maxRightArmAngle < 70f || maxBodyAngle < 4f ||
                maxArmScaleError > 0.00001f || maxGroundPenetration > 0.08f || finalCurtainCallPoseError > 0.01f ||
                maxBladeBelowAxis < 0.11f || lowerBladeDominance < 2.5f ||
                scytheArcDepthRatio < 0.22f || scytheArcDepthRatio > 0.45f ||
                maxScytheTipDrop < 0.035f || maxScytheTipDrop > 0.22f || maxScytheExtension < 0.04f ||
                scytheAxialSpan < 0.28f || scytheExtensionDistance < 0.035f ||
                scytheTipAverageDrop < 0.025f || scytheTipAverageDrop > 0.2f ||
                scytheTipThicknessRatio > 0.65f ||
                scytheFrontThicknessRatio > 0.35f || scytheFrontCenterShift > 0.03f ||
                scytheBladePlaneAspect < 6f ||
                maxProximalArmDelta > 0.015f || maxNonArmDelta > 0.00001f || bladeChangedVertices < 80 ||
                acceleratedDropFrames < 14f || acceleratedDropFrames > 20f ||
                peakSlashSpeed < 6f || averageSlashSpeed < 2f || dropImpulseRatio < 6f ||
                slashSpeedUniformity < 0.6f || slashPathEfficiency < 0.55f ||
                maxElbowBend < 55f || maxElbowBend > 90f || minimumTorsoFrontClearance < 0.015f ||
                minimumBladeFrontFacing < 0.22f || averageBladeFrontFacing < 0.35f)
            {
                throw new InvalidOperationException(
                    $"Grave curtain-call attack sampled motion mismatch. ExtendSide={extendSide:0.######}, " +
                    $"ExtendRise={extendRise:0.######}, MidSweepDrop={midSweepDrop:0.######}, " +
                    $"SweepDrop={sweepDrop:0.######}, SweepInward={sweepInward:0.######}, " +
                    $"SweepForwardFromExtension={sweepForwardFromExtension:0.######}, " +
                    $"DescentDominance={descentDominance:0.######}, " +
                    $"FinalElbowLateral={finalElbowLateral:0.######}, FinalElbowForward={finalElbowForward:0.######}, " +
                    $"FinalLateral={finalLateral:0.######}, " +
                    $"FinalForward={finalForward:0.######}, FinalVertical={finalVertical:0.######}, " +
                    $"FinalForeArmLeftReach={finalForeArmLeftReach:0.######}, " +
                    $"FinalForeArmForward={finalForeArmForward:0.######}, " +
                    $"FinalLeftwardDominance={finalLeftwardDominance:0.######}, " +
                    $"PullReduction={pullDistanceReduction:0.######}, " +
                    $"CurtainCallHoldDrift={curtainCallHoldDrift:0.######}, " +
                    $"RightArmAngle={maxRightArmAngle:0.######}, BodyAngle={maxBodyAngle:0.######}, " +
                    $"ArmScaleError={maxArmScaleError:0.######}, GroundPenetration={maxGroundPenetration:0.######}, " +
                    $"FinalCurtainCallPoseError={finalCurtainCallPoseError:0.######}, BladeDown={maxBladeDownward:0.######}, " +
                    $"UpperThickness={maxUpperThicknessRise:0.######}, " +
                    $"BladeBelowAxis={maxBladeBelowAxis:0.######}, " +
                    $"SpineAboveAxis={maxSpineAboveAxis:0.######}, " +
                    $"LowerBladeDominance={lowerBladeDominance:0.######}, " +
                    $"ScytheArcDepthRatio={scytheArcDepthRatio:0.######}, " +
                    $"ScytheTipDrop={maxScytheTipDrop:0.######}, " +
                    $"ScytheExtension={maxScytheExtension:0.######}, " +
                    $"ScytheAxialSpan={scytheAxialSpan:0.######}, " +
                    $"ScytheExtensionDistance={scytheExtensionDistance:0.######}, " +
                    $"ScytheTipAverageDrop={scytheTipAverageDrop:0.######}, " +
                    $"ScytheTipThicknessRatio={scytheTipThicknessRatio:0.######}, " +
                    $"ScytheFrontThicknessRatio={scytheFrontThicknessRatio:0.######}, " +
                    $"ScytheFrontCenterShift={scytheFrontCenterShift:0.######}, " +
                    $"ScytheBladePlaneAspect={scytheBladePlaneAspect:0.######}, " +
                    $"FrontBackCompressionDelta={maxFrontBackDelta:0.######}, " +
                    $"ProximalArmDelta={maxProximalArmDelta:0.######}, " +
                    $"NonArmDelta={maxNonArmDelta:0.######}, " +
                    $"BladeVertices={bladeChangedVertices}, PeakDropSpeed={peakDropSpeed:0.######}, " +
                    $"PreBurstDropSpeed={preBurstDropSpeed:0.######}, " +
                    $"PostBurstDropSpeed={postBurstDropSpeed:0.######}, " +
                    $"AverageDropSpeed={averageDropSpeed:0.######}, DropImpulseRatio={dropImpulseRatio:0.######}, " +
                    $"AcceleratedDropFrames={acceleratedDropFrames:0.###}, " +
                    $"PeakSlashSpeed={peakSlashSpeed:0.######}, MinimumSlashSpeed={minimumSlashSpeed:0.######}, " +
                    $"AverageSlashSpeed={averageSlashSpeed:0.######}, " +
                    $"SlashSpeedUniformity={slashSpeedUniformity:0.######}, " +
                    $"SlashPathEfficiency={slashPathEfficiency:0.######}, " +
                    $"MaxElbowBend={maxElbowBend:0.######}, " +
                    $"TorsoFrontClearance={minimumTorsoFrontClearance:0.######}, " +
                    $"BladeFrontFacingMin={minimumBladeFrontFacing:0.######}, " +
                    $"BladeFrontFacingAverage={averageBladeFrontFacing:0.######}, " +
                    $"BladeFrontFacingSamples={bladeFrontFacingMetrics.x:0.######}|" +
                    $"{bladeFrontFacingMetrics.y:0.######}|{bladeFrontFacingMetrics.z:0.######}|" +
                    $"{bladeFrontFacingMetrics.w:0.######}");
            }

            // Measurement helpers temporarily disable the Animator, so leave the actual review slot rebound too.
            RebindApprovedGraveCurtainCallAnimator(animator, controller, clip, forceControllerReload: false);
            var metrics =
                $"Target={GraveAttackSlotName}/{GraveModelName}, Duration={clip.length:0.###}, " +
                $"LoopTime={settings.loopTime}, LoopBlend={settings.loopBlend}, CurveCount={bindings.Length}, " +
                $"LiveAnimatorBound=True, " +
                $"ExtendSide={extendSide:0.######}, ExtendRise={extendRise:0.######}, " +
                $"MidSweepDrop={midSweepDrop:0.######}, SweepDrop={sweepDrop:0.######}, " +
                $"SweepInward={sweepInward:0.######}, " +
                $"SweepForwardFromExtension={sweepForwardFromExtension:0.######}, " +
                $"DescentDominance={descentDominance:0.######}, " +
                $"FinalElbowLateral={finalElbowLateral:0.######}, FinalElbowForward={finalElbowForward:0.######}, " +
                $"FinalLateral={finalLateral:0.######}, " +
                $"FinalForward={finalForward:0.######}, FinalVertical={finalVertical:0.######}, " +
                $"FinalForeArmLeftReach={finalForeArmLeftReach:0.######}, " +
                $"FinalForeArmForward={finalForeArmForward:0.######}, " +
                $"FinalLeftwardDominance={finalLeftwardDominance:0.######}, " +
                $"PullReduction={pullDistanceReduction:0.######}, " +
                $"CurtainCallHoldDrift={curtainCallHoldDrift:0.######}, " +
                $"RightArmAngle={maxRightArmAngle:0.######}, " +
                $"BodyAngle={maxBodyAngle:0.######}, ArmScaleError={maxArmScaleError:0.######}, " +
                $"BoundsMin={FormatVector(minBounds)}, BoundsMax={FormatVector(maxBounds)}, " +
                $"GroundPenetration={maxGroundPenetration:0.######}, " +
                $"FinalCurtainCallPoseError={finalCurtainCallPoseError:0.######}, " +
                $"Blade={GraveAttackBladeBlendShapeName}, BladeStart={bladeCurve.Evaluate(0f):0.###}, " +
                $"BladeExtend={bladeCurve.Evaluate(GraveAttackBladeFullTime):0.###}, " +
                $"BladeFastDrop={bladeCurve.Evaluate(GraveAttackSlashEndTime):0.###}, " +
                $"BladeLowered={bladeCurve.Evaluate(1.65f):0.###}, " +
                $"BladeLateAttack={bladeCurve.Evaluate(2.35f):0.###}, " +
                $"BladeFinal={bladeCurve.Evaluate(GraveAttackDuration):0.###}, " +
                $"BladeAfterAttack={postAttackResetWeight:0.###}, " +
                $"BladeDown={maxBladeDownward:0.######}, UpperThickness={maxUpperThicknessRise:0.######}, " +
                $"BladeBelowAxis={maxBladeBelowAxis:0.######}, " +
                $"SpineAboveAxis={maxSpineAboveAxis:0.######}, " +
                $"LowerBladeDominance={lowerBladeDominance:0.######}, " +
                $"ScytheArcDepthRatio={scytheArcDepthRatio:0.######}, " +
                $"ScytheTipDrop={maxScytheTipDrop:0.######}, " +
                $"ScytheExtension={maxScytheExtension:0.######}, " +
                $"ScytheAxialSpan={scytheAxialSpan:0.######}, " +
                $"ScytheExtensionDistance={scytheExtensionDistance:0.######}, " +
                $"ScytheTipAverageDrop={scytheTipAverageDrop:0.######}, " +
                $"ScytheTipThicknessRatio={scytheTipThicknessRatio:0.######}, " +
                $"ScytheFrontThicknessRatio={scytheFrontThicknessRatio:0.######}, " +
                $"ScytheFrontCenterShift={scytheFrontCenterShift:0.######}, " +
                $"ScytheBladePlaneAspect={scytheBladePlaneAspect:0.######}, " +
                $"FrontBackCompressionDelta={maxFrontBackDelta:0.######}, " +
                $"ProximalArmDelta={maxProximalArmDelta:0.######}, " +
                $"NonArmDelta={maxNonArmDelta:0.######}, BladeVertices={bladeChangedVertices}, " +
                $"PeakDropSpeed={peakDropSpeed:0.######}, PreBurstDropSpeed={preBurstDropSpeed:0.######}, " +
                $"PostBurstDropSpeed={postBurstDropSpeed:0.######}, " +
                $"AverageDropSpeed={averageDropSpeed:0.######}, " +
                $"DropImpulseRatio={dropImpulseRatio:0.######}, " +
                $"AcceleratedDropFrames={acceleratedDropFrames:0.###}, " +
                $"PeakSlashSpeed={peakSlashSpeed:0.######}, MinimumSlashSpeed={minimumSlashSpeed:0.######}, " +
                $"AverageSlashSpeed={averageSlashSpeed:0.######}, " +
                $"SlashSpeedUniformity={slashSpeedUniformity:0.######}, " +
                $"SlashPathEfficiency={slashPathEfficiency:0.######}, " +
                $"MaxElbowBend={maxElbowBend:0.######}, " +
                $"TorsoFrontClearance={minimumTorsoFrontClearance:0.######}, " +
                $"BladeFrontFacingMin={minimumBladeFrontFacing:0.######}, " +
                $"BladeFrontFacingAverage={averageBladeFrontFacing:0.######}, " +
                $"BladeFrontFacingSamples={bladeFrontFacingMetrics.x:0.######}|" +
                $"{bladeFrontFacingMetrics.y:0.######}|{bladeFrontFacingMetrics.z:0.######}|" +
                $"{bladeFrontFacingMetrics.w:0.######}, " +
                $"ModelRootMotion=0, SlotRootMotion=0";
            if (writeReport)
            {
                var folder = ProjectAbsolutePath(GraveAttackValidationRelativeFolder);
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "GraveCurtainCallAttackValidation.txt"), metrics + Environment.NewLine);
            }

            return metrics;
        }

        private static AnimationClip EnsureApprovedGraveSlowWalkClip(Transform walkModel)
        {
            var hips = RequireGraveWalkBone(walkModel, "Hips");
            var spine02 = RequireGraveWalkBone(walkModel, "Spine02");
            var spine01 = RequireGraveWalkBone(walkModel, "Spine01");
            var spine = RequireGraveWalkBone(walkModel, "Spine");
            var neck = RequireGraveWalkBone(walkModel, "neck");
            var head = RequireGraveWalkBone(walkModel, "Head");
            var leftShoulder = RequireGraveWalkBone(walkModel, "LeftShoulder");
            var leftUpLeg = RequireGraveWalkBone(walkModel, "LeftUpLeg");
            var leftLeg = RequireGraveWalkBone(walkModel, "LeftLeg");
            var leftFoot = RequireGraveWalkBone(walkModel, "LeftFoot");
            var leftToe = RequireGraveWalkBone(walkModel, "LeftToeBase");
            var leftForeArm = RequireGraveWalkBone(walkModel, "LeftForeArm");
            var rightShoulder = RequireGraveWalkBone(walkModel, "RightShoulder");
            var rightUpLeg = RequireGraveWalkBone(walkModel, "RightUpLeg");
            var rightLeg = RequireGraveWalkBone(walkModel, "RightLeg");
            var rightFoot = RequireGraveWalkBone(walkModel, "RightFoot");
            var rightToe = RequireGraveWalkBone(walkModel, "RightToeBase");
            var rightForeArm = RequireGraveWalkBone(walkModel, "RightForeArm");
            var leftArm = RequireGraveWalkBone(walkModel, "LeftArm");
            var rightArm = RequireGraveWalkBone(walkModel, "RightArm");
            EnsureAssetDirectory(GraveWalkClipAssetPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(GraveWalkClipAssetPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = "Grave_Walk_Slow", frameRate = 60f };
                AssetDatabase.CreateAsset(clip, GraveWalkClipAssetPath);
            }

            clip.ClearCurves();
            clip.legacy = false;
            clip.wrapMode = WrapMode.Loop;
            clip.frameRate = 60f;
            SetWalkModelSpacePositionCurves(
                clip,
                hips,
                walkModel,
                new[]
                {
                    new Vector3(0f, GraveWalkRigGroundClearance, 0f),
                    new Vector3(-0.014f, 0.002f + GraveWalkRigGroundClearance, -0.002f),
                    new Vector3(-0.012f, GraveWalkBodyRise + GraveWalkRigGroundClearance, 0f),
                    new Vector3(-0.006f, 0.008f + GraveWalkRigGroundClearance, 0.002f),
                    new Vector3(0f, GraveWalkRigGroundClearance, 0f),
                    new Vector3(0.014f, 0.002f + GraveWalkRigGroundClearance, -0.002f),
                    new Vector3(0.012f, GraveWalkBodyRise + GraveWalkRigGroundClearance, 0f),
                    new Vector3(0.006f, 0.008f + GraveWalkRigGroundClearance, 0.002f),
                    new Vector3(0f, GraveWalkRigGroundClearance, 0f)
                });
            SetWalkRotationCurves(clip, hips, walkModel, new[]
            {
                new Vector3(0f, -3f, 0f), new Vector3(0.4f, -2f, -1.2f), Vector3.zero,
                new Vector3(-0.4f, 2f, 1.2f), new Vector3(0f, 3f, 0f),
                new Vector3(0.4f, 2f, 1.2f), Vector3.zero,
                new Vector3(-0.4f, -2f, -1.2f), new Vector3(0f, -3f, 0f)
            });
            SetWalkRotationCurves(clip, spine02, walkModel, new[]
            {
                new Vector3(0f, 2.4f, 0.8f), new Vector3(-0.5f, 1.6f, 1.2f),
                new Vector3(-0.8f, 0f, 0.5f), new Vector3(-0.5f, -1.6f, -0.6f),
                new Vector3(0f, -2.4f, -0.8f), new Vector3(-0.5f, -1.6f, -1.2f),
                new Vector3(-0.8f, 0f, -0.5f), new Vector3(-0.5f, 1.6f, 0.6f),
                new Vector3(0f, 2.4f, 0.8f)
            });
            SetWalkRotationCurves(clip, spine01, walkModel, WalkCounterTwist(1.7f, 0.45f));
            SetWalkRotationCurves(clip, spine, walkModel, WalkCounterTwist(1.2f, 0.35f));
            SetWalkRotationCurves(clip, neck, walkModel, WalkCounterTwist(-0.8f, -0.3f));
            SetWalkRotationCurves(clip, head, walkModel, WalkCounterTwist(-0.6f, -0.25f));
            SetWalkRotationCurves(clip, leftUpLeg, walkModel, WalkSwingNine(-14f));
            SetWalkRotationCurves(clip, rightUpLeg, walkModel, WalkSwingNine(14f));
            SetWalkRotationCurves(clip, leftLeg, walkModel, WalkLeftKnee());
            SetWalkRotationCurves(clip, rightLeg, walkModel, WalkRightKnee());
            SetWalkRotationCurves(clip, leftFoot, walkModel, WalkLeftFoot());
            SetWalkRotationCurves(clip, rightFoot, walkModel, WalkRightFoot());
            SetWalkRotationCurves(clip, leftToe, walkModel, WalkLeftToe());
            SetWalkRotationCurves(clip, rightToe, walkModel, WalkRightToe());
            SetWalkRotationCurves(clip, leftShoulder, walkModel, WalkArmSwingNine(3f));
            SetWalkRotationCurves(clip, rightShoulder, walkModel, WalkArmSwingNine(-3f));
            SetWalkRotationCurves(clip, leftArm, walkModel, WalkArmSwingNine(10f));
            SetWalkRotationCurves(clip, rightArm, walkModel, WalkArmSwingNine(-10f));
            SetWalkRotationCurves(clip, leftForeArm, walkModel, WalkLeftForeArm());
            SetWalkRotationCurves(clip, rightForeArm, walkModel, WalkRightForeArm());
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            settings.cycleOffset = 0f;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimatorController EnsureApprovedGraveSlowWalkController(AnimationClip clip)
        {
            EnsureAssetDirectory(GraveWalkControllerAssetPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(GraveWalkControllerAssetPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(GraveWalkControllerAssetPath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states.Select(child => child.state)
                .FirstOrDefault(candidate => candidate.name == "SlowWalk");
            if (state == null)
            {
                state = stateMachine.AddState("SlowWalk");
            }

            foreach (var child in stateMachine.states.ToArray())
            {
                if (child.state != state)
                {
                    stateMachine.RemoveState(child.state);
                }
            }

            foreach (var transition in state.transitions.ToArray())
            {
                state.RemoveTransition(transition);
            }

            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static Vector3[] WalkSwingNine(float amplitude)
        {
            return new[]
            {
                new Vector3(amplitude, 0f, 0f), new Vector3(amplitude * 0.72f, 0f, 0f), Vector3.zero,
                new Vector3(-amplitude * 0.72f, 0f, 0f), new Vector3(-amplitude, 0f, 0f),
                new Vector3(-amplitude * 0.72f, 0f, 0f), Vector3.zero,
                new Vector3(amplitude * 0.72f, 0f, 0f), new Vector3(amplitude, 0f, 0f)
            };
        }

        private static Vector3[] WalkArmSwingNine(float amplitude)
        {
            return WalkSwingNine(amplitude)
                .Select(value => new Vector3(value.x, value.x * 0.08f, -value.x * 0.05f))
                .ToArray();
        }

        private static Vector3[] WalkCounterTwist(float yawAmplitude, float rollAmplitude)
        {
            return new[]
            {
                new Vector3(0f, yawAmplitude, 0f), new Vector3(-0.2f, yawAmplitude * 0.7f, rollAmplitude),
                new Vector3(-0.35f, 0f, rollAmplitude * 0.35f),
                new Vector3(-0.2f, -yawAmplitude * 0.7f, -rollAmplitude),
                new Vector3(0f, -yawAmplitude, 0f),
                new Vector3(-0.2f, -yawAmplitude * 0.7f, -rollAmplitude),
                new Vector3(-0.35f, 0f, -rollAmplitude * 0.35f),
                new Vector3(-0.2f, yawAmplitude * 0.7f, rollAmplitude),
                new Vector3(0f, yawAmplitude, 0f)
            };
        }

        private static Vector3[] WalkLeftKnee()
        {
            return WalkPitchValues(4f, 10f, 6f, 4f, 18f, 28f, 24f, 10f, 4f);
        }

        private static Vector3[] WalkRightKnee()
        {
            return WalkPitchValues(-18f, -28f, -24f, -10f, -4f, -10f, -6f, -4f, -18f);
        }

        private static Vector3[] WalkLeftFoot()
        {
            return WalkPitchValues(-8f, -4f, 0f, 6f, 12f, 4f, -6f, -9f, -8f);
        }

        private static Vector3[] WalkRightFoot()
        {
            return WalkPitchValues(12f, 4f, -6f, -9f, -8f, -4f, 0f, 6f, 12f);
        }

        private static Vector3[] WalkLeftToe()
        {
            return WalkPitchValues(0f, 0f, 0f, 8f, 16f, 10f, 2f, 0f, 0f);
        }

        private static Vector3[] WalkRightToe()
        {
            return WalkPitchValues(-16f, -10f, -2f, 0f, 0f, 0f, 0f, -8f, -16f);
        }

        private static Vector3[] WalkLeftForeArm()
        {
            return WalkPitchValues(12f, 10f, 8f, 10f, 14f, 17f, 18f, 16f, 12f);
        }

        private static Vector3[] WalkRightForeArm()
        {
            return WalkPitchValues(-14f, -17f, -18f, -16f, -12f, -10f, -8f, -10f, -14f);
        }

        private static Vector3[] WalkPitchValues(params float[] values)
        {
            if (values.Length != GraveWalkKeyTimes.Length)
            {
                throw new ArgumentException("Grave walk pitch key count must match the gait phase count.", nameof(values));
            }

            return values.Select(value => new Vector3(value, 0f, 0f)).ToArray();
        }

        private static void SetWalkRotationCurves(
            AnimationClip clip,
            Transform target,
            Transform model,
            IReadOnlyList<Vector3> additiveEulerAngles)
        {
            var quaternions = new Quaternion[GraveWalkKeyTimes.Length];
            for (var i = 0; i < quaternions.Length; i++)
            {
                var angles = additiveEulerAngles[i];
                var modelSpaceDelta =
                    Quaternion.AngleAxis(angles.y, model.up) *
                    Quaternion.AngleAxis(angles.x, model.right) *
                    Quaternion.AngleAxis(angles.z, model.forward);
                var value = Quaternion.Inverse(target.parent.rotation) * modelSpaceDelta * target.rotation;
                if (i > 0 && Quaternion.Dot(quaternions[i - 1], value) < 0f)
                {
                    value = new Quaternion(-value.x, -value.y, -value.z, -value.w);
                }

                quaternions[i] = value;
            }

            var path = AnimationUtility.CalculateTransformPath(target, model);
            SetWalkFloatCurve(clip, path, "m_LocalRotation.x", quaternions.Select(value => value.x).ToArray());
            SetWalkFloatCurve(clip, path, "m_LocalRotation.y", quaternions.Select(value => value.y).ToArray());
            SetWalkFloatCurve(clip, path, "m_LocalRotation.z", quaternions.Select(value => value.z).ToArray());
            SetWalkFloatCurve(clip, path, "m_LocalRotation.w", quaternions.Select(value => value.w).ToArray());
        }

        private static void SetWalkModelSpacePositionCurves(
            AnimationClip clip,
            Transform target,
            Transform model,
            IReadOnlyList<Vector3> modelSpaceOffsets)
        {
            var path = AnimationUtility.CalculateTransformPath(target, model);
            var localRight = target.parent.InverseTransformDirection(model.right).normalized;
            var localUp = target.parent.InverseTransformDirection(model.up).normalized;
            var localForward = target.parent.InverseTransformDirection(model.forward).normalized;
            var positions = modelSpaceOffsets.Select(offset =>
                    target.localPosition + localRight * offset.x + localUp * offset.y + localForward * offset.z)
                .ToArray();
            SetWalkFloatCurve(clip, path, "m_LocalPosition.x", positions.Select(value => value.x).ToArray());
            SetWalkFloatCurve(clip, path, "m_LocalPosition.y", positions.Select(value => value.y).ToArray());
            SetWalkFloatCurve(clip, path, "m_LocalPosition.z", positions.Select(value => value.z).ToArray());
        }

        private static void SetWalkFloatCurve(AnimationClip clip, string path, string propertyName, IReadOnlyList<float> values)
        {
            var keys = new Keyframe[GraveWalkKeyTimes.Length];
            for (var i = 0; i < keys.Length; i++)
            {
                keys[i] = new Keyframe(GraveWalkKeyTimes[i], values[i]);
            }

            var curve = new AnimationCurve(keys);
            for (var i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            }

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName),
                curve);
        }

        private static Transform RequireGraveWalkBone(Transform model, string name)
        {
            return FindDescendant(model, name) ??
                throw new InvalidOperationException("Grave slow-walk rig is missing " + name + ".");
        }

        private static string ValidateApprovedGraveSlowWalkScene(Scene scene, bool writeReport)
        {
            ValidateApprovedReproductionScene(scene, writeReport: false);
            var graveRoot = RequireSceneObject(scene, GraveRootName).transform;
            var walkSlot = graveRoot.Find(GraveWalkSlotName) ??
                throw new InvalidOperationException(GraveWalkSlotName + " is missing.");
            var walkModel = walkSlot.Find(GraveModelName) ??
                throw new InvalidOperationException(GraveWalkSlotName + "/" + GraveModelName + " is missing.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(GraveWalkClipAssetPath) ??
                throw new InvalidOperationException("Grave slow-walk clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(GraveWalkControllerAssetPath) ??
                throw new InvalidOperationException("Grave slow-walk controller is missing.");
            var animator = walkModel.GetComponent<Animator>() ??
                throw new InvalidOperationException("Grave slow-walk Animator is missing.");
            if (animator.runtimeAnimatorController != controller || animator.applyRootMotion || !animator.enabled ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException("Grave slow-walk Animator configuration mismatch.");
            }

            var defaultState = controller.layers.Length > 0 ? controller.layers[0].stateMachine.defaultState : null;
            if (defaultState == null || defaultState.name != "SlowWalk" || defaultState.motion != clip ||
                Mathf.Abs(defaultState.speed - 1f) > 0.0001f)
            {
                throw new InvalidOperationException("Grave slow-walk controller default state mismatch.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || !settings.loopBlend || Mathf.Abs(clip.length - GraveWalkDuration) > 0.001f)
            {
                throw new InvalidOperationException(
                    $"Grave slow-walk loop configuration mismatch. Length={clip.length:0.######}, " +
                    $"LoopTime={settings.loopTime}, LoopBlend={settings.loopBlend}");
            }

            var animatedBones = new[]
            {
                RequireGraveWalkBone(walkModel, "Hips"),
                RequireGraveWalkBone(walkModel, "Spine02"), RequireGraveWalkBone(walkModel, "Spine01"),
                RequireGraveWalkBone(walkModel, "Spine"), RequireGraveWalkBone(walkModel, "neck"),
                RequireGraveWalkBone(walkModel, "Head"),
                RequireGraveWalkBone(walkModel, "LeftShoulder"), RequireGraveWalkBone(walkModel, "LeftArm"),
                RequireGraveWalkBone(walkModel, "LeftForeArm"),
                RequireGraveWalkBone(walkModel, "RightShoulder"), RequireGraveWalkBone(walkModel, "RightArm"),
                RequireGraveWalkBone(walkModel, "RightForeArm"),
                RequireGraveWalkBone(walkModel, "LeftUpLeg"), RequireGraveWalkBone(walkModel, "LeftLeg"),
                RequireGraveWalkBone(walkModel, "LeftFoot"), RequireGraveWalkBone(walkModel, "LeftToeBase"),
                RequireGraveWalkBone(walkModel, "RightUpLeg"), RequireGraveWalkBone(walkModel, "RightLeg"),
                RequireGraveWalkBone(walkModel, "RightFoot"), RequireGraveWalkBone(walkModel, "RightToeBase")
            };
            var expectedBindings = new HashSet<string>();
            foreach (var bone in animatedBones)
            {
                var path = AnimationUtility.CalculateTransformPath(bone, walkModel);
                foreach (var component in new[] { "x", "y", "z", "w" })
                {
                    expectedBindings.Add(path + "|m_LocalRotation." + component);
                }
            }

            var hipsPath = AnimationUtility.CalculateTransformPath(animatedBones[0], walkModel);
            expectedBindings.Add(hipsPath + "|m_LocalPosition.x");
            expectedBindings.Add(hipsPath + "|m_LocalPosition.y");
            expectedBindings.Add(hipsPath + "|m_LocalPosition.z");
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var actualBindings = bindings.Select(binding => binding.path + "|" + binding.propertyName).ToHashSet();
            if (bindings.Length != expectedBindings.Count || !actualBindings.SetEquals(expectedBindings) ||
                bindings.Any(binding => string.IsNullOrEmpty(binding.path)) ||
                AnimationUtility.GetObjectReferenceCurveBindings(clip).Length != 0)
            {
                throw new InvalidOperationException("Grave slow-walk clip binding or root-motion scope mismatch.");
            }

            var idleSlot = graveRoot.Find(GraveIdleSlotName) ??
                throw new InvalidOperationException(GraveIdleSlotName + " is missing.");
            var idleModel = idleSlot.Find(GraveModelName) ??
                throw new InvalidOperationException(GraveIdleSlotName + "/" + GraveModelName + " is missing.");
            var graveAnimators = graveRoot.GetComponentsInChildren<Animator>(true);
            if (graveAnimators.Length != 2 || !graveAnimators.Contains(animator) ||
                !graveAnimators.Contains(idleModel.GetComponent<Animator>()))
            {
                throw new InvalidOperationException(
                    $"Only idle and slow-walk Grave slots may contain Animators. Actual={graveAnimators.Length}");
            }

            var hips = animatedBones[0];
            var leftToe = animatedBones[15];
            var rightToe = animatedBones[19];
            var poses = CaptureLocalPoses(walkModel);
            var animatorWasEnabled = animator.enabled;
            var slotPosition = walkSlot.localPosition;
            var slotRotation = walkSlot.localRotation;
            var slotScale = walkSlot.localScale;
            var modelPosition = walkModel.localPosition;
            var modelRotation = walkModel.localRotation;
            var modelScale = walkModel.localScale;
            var minBounds = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var maxBounds = Vector3.zero;
            var maxGroundPenetration = 0f;
            var maxGroundPenetrationTime = 0f;
            var leftToeHeights = new float[GraveWalkKeyTimes.Length];
            var rightToeHeights = new float[GraveWalkKeyTimes.Length];
            var startRotations = new Quaternion[animatedBones.Length];
            var startHipsWorldPosition = Vector3.zero;
            var loopPoseError = 0f;
            var leftStrideAngle = 0f;
            var rightStrideAngle = 0f;
            var leftToeRollAngle = 0f;
            var rightToeRollAngle = 0f;
            var maxHipsRise = 0f;
            var maxHipsLateralShift = 0f;
            try
            {
                animator.enabled = false;
                for (var i = 0; i < GraveWalkKeyTimes.Length; i++)
                {
                    RestoreLocalPoses(poses);
                    clip.SampleAnimation(walkModel.gameObject, GraveWalkKeyTimes[i]);
                    if (walkSlot.localPosition != slotPosition || walkSlot.localRotation != slotRotation || walkSlot.localScale != slotScale ||
                        walkModel.localPosition != modelPosition || walkModel.localRotation != modelRotation || walkModel.localScale != modelScale)
                    {
                        throw new InvalidOperationException("Grave slow-walk clip contains slot or model root motion.");
                    }

                    var bounds = CalculateVisibleBounds(walkSlot);
                    minBounds = Vector3.Min(minBounds, bounds.size);
                    maxBounds = Vector3.Max(maxBounds, bounds.size);
                    var groundPenetration = graveRoot.position.y - bounds.min.y;
                    if (groundPenetration > maxGroundPenetration)
                    {
                        maxGroundPenetration = groundPenetration;
                        maxGroundPenetrationTime = GraveWalkKeyTimes[i];
                    }
                    leftToeHeights[i] = leftToe.position.y - graveRoot.position.y;
                    rightToeHeights[i] = rightToe.position.y - graveRoot.position.y;
                    if (i == 0)
                    {
                        startHipsWorldPosition = hips.position;
                        for (var boneIndex = 0; boneIndex < animatedBones.Length; boneIndex++)
                        {
                            startRotations[boneIndex] = animatedBones[boneIndex].localRotation;
                        }
                    }
                    else
                    {
                        var hipsDelta = hips.position - startHipsWorldPosition;
                        maxHipsRise = Mathf.Max(maxHipsRise, Vector3.Dot(hipsDelta, walkModel.up));
                        maxHipsLateralShift = Mathf.Max(
                            maxHipsLateralShift,
                            Mathf.Abs(Vector3.Dot(hipsDelta, walkModel.right)));
                    }

                    if (i == 4)
                    {
                        leftStrideAngle = Quaternion.Angle(startRotations[12], animatedBones[12].localRotation);
                        rightStrideAngle = Quaternion.Angle(startRotations[16], animatedBones[16].localRotation);
                        leftToeRollAngle = Quaternion.Angle(startRotations[15], animatedBones[15].localRotation);
                        rightToeRollAngle = Quaternion.Angle(startRotations[19], animatedBones[19].localRotation);
                    }
                    else if (i == GraveWalkKeyTimes.Length - 1)
                    {
                        for (var boneIndex = 0; boneIndex < animatedBones.Length; boneIndex++)
                        {
                            loopPoseError = Mathf.Max(
                                loopPoseError,
                                Quaternion.Angle(startRotations[boneIndex], animatedBones[boneIndex].localRotation));
                        }

                        loopPoseError = Mathf.Max(loopPoseError, Vector3.Distance(startHipsWorldPosition, hips.position));
                    }
                }
            }
            finally
            {
                RestoreLocalPoses(poses);
                animator.enabled = animatorWasEnabled;
            }

            var rightPassingLift = rightToeHeights[2] - Mathf.Min(rightToeHeights[0], rightToeHeights[4]);
            var leftPassingLift = leftToeHeights[6] - Mathf.Min(leftToeHeights[4], leftToeHeights[8]);
            if (leftStrideAngle < 25f || rightStrideAngle < 25f ||
                rightPassingLift < 0.015f || leftPassingLift < 0.015f ||
                leftToeRollAngle < 12f || rightToeRollAngle < 12f ||
                maxHipsRise < 0.014f || maxHipsLateralShift < 0.01f ||
                maxGroundPenetration > 0.012f || loopPoseError > 0.01f)
            {
                throw new InvalidOperationException(
                    $"Grave slow-walk sampled motion mismatch. LeftStride={leftStrideAngle:0.######}, " +
                    $"RightStride={rightStrideAngle:0.######}, RightPassingLift={rightPassingLift:0.######}, " +
                    $"LeftPassingLift={leftPassingLift:0.######}, LeftToeRoll={leftToeRollAngle:0.######}, " +
                    $"RightToeRoll={rightToeRollAngle:0.######}, HipsRise={maxHipsRise:0.######}, " +
                    $"HipsLateral={maxHipsLateralShift:0.######}, GroundPenetration={maxGroundPenetration:0.######}, " +
                    $"GroundPenetrationTime={maxGroundPenetrationTime:0.###}, " +
                    $"LoopPoseError={loopPoseError:0.######}");
            }

            var metrics =
                $"Target={GraveWalkSlotName}/{GraveModelName}, Duration={clip.length:0.###}, " +
                $"StepInterval={GraveWalkDuration * 0.5f:0.###}, LoopTime={settings.loopTime}, LoopBlend={settings.loopBlend}, " +
                $"CurveCount={bindings.Length}, ModelRootMotion=0, SlotRootMotion=0, MaxBodyRise={GraveWalkBodyRise:0.###}, " +
                $"LeftStrideAngle={leftStrideAngle:0.######}, RightStrideAngle={rightStrideAngle:0.######}, " +
                $"RightPassingLift={rightPassingLift:0.######}, LeftPassingLift={leftPassingLift:0.######}, " +
                $"LeftToeRoll={leftToeRollAngle:0.######}, RightToeRoll={rightToeRollAngle:0.######}, " +
                $"HipsRise={maxHipsRise:0.######}, HipsLateral={maxHipsLateralShift:0.######}, " +
                $"BoundsMin={FormatVector(minBounds)}, BoundsMax={FormatVector(maxBounds)}, " +
                $"GroundPenetration={maxGroundPenetration:0.######}, " +
                $"GroundPenetrationTime={maxGroundPenetrationTime:0.###}, LoopPoseError={loopPoseError:0.######}, " +
                $"GraveAnimatorCount={graveAnimators.Length}";
            if (writeReport)
            {
                var folder = ProjectAbsolutePath(GraveWalkValidationRelativeFolder);
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "GraveSlowWalkValidation.txt"), metrics + Environment.NewLine);
            }

            return metrics;
        }

        private static AnimationClip EnsureApprovedGraveHitRecoilClip(Transform hitModel)
        {
            var spine02 = RequireGraveHitBone(hitModel, "Spine02");
            var spine01 = RequireGraveHitBone(hitModel, "Spine01");
            var spine = RequireGraveHitBone(hitModel, "Spine");
            var neck = RequireGraveHitBone(hitModel, "neck");
            var head = RequireGraveHitBone(hitModel, "Head");
            EnsureAssetDirectory(GraveHitClipAssetPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(GraveHitClipAssetPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = "Grave_Hit_Recoil", frameRate = 60f };
                AssetDatabase.CreateAsset(clip, GraveHitClipAssetPath);
            }

            clip.ClearCurves();
            clip.legacy = false;
            clip.wrapMode = WrapMode.Loop;
            clip.frameRate = 60f;
            SetGraveHitRotationCurves(clip, spine02, hitModel, 14f, GraveHitBodyFactors);
            SetGraveHitRotationCurves(clip, spine01, hitModel, 10f, GraveHitBodyFactors);
            SetGraveHitRotationCurves(clip, spine, hitModel, 7f, GraveHitBodyFactors);
            SetGraveHitRotationCurves(clip, neck, hitModel, 5f, GraveHitHeadLagFactors);
            SetGraveHitRotationCurves(clip, head, hitModel, 3f, GraveHitHeadLagFactors);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            settings.cycleOffset = 0f;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimatorController EnsureApprovedGraveHitRecoilController(AnimationClip clip)
        {
            EnsureAssetDirectory(GraveHitControllerAssetPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(GraveHitControllerAssetPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(GraveHitControllerAssetPath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states.Select(child => child.state)
                .FirstOrDefault(candidate => candidate.name == "HitRecoil");
            if (state == null)
            {
                state = stateMachine.AddState("HitRecoil");
            }

            foreach (var child in stateMachine.states.ToArray())
            {
                if (child.state != state)
                {
                    stateMachine.RemoveState(child.state);
                }
            }

            foreach (var transition in state.transitions.ToArray())
            {
                state.RemoveTransition(transition);
            }

            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void SetGraveHitRotationCurves(
            AnimationClip clip,
            Transform target,
            Transform model,
            float backwardAngle,
            IReadOnlyList<float> factors)
        {
            if (factors.Count != GraveHitKeyTimes.Length)
            {
                throw new ArgumentException("Grave hit-recoil factor count must match hit key times.", nameof(factors));
            }

            var visualFront = CalculateVisualFront(model);
            var recoilAxis = Vector3.Cross(model.up, visualFront).normalized;
            var quaternions = new Quaternion[GraveHitKeyTimes.Length];
            for (var i = 0; i < quaternions.Length; i++)
            {
                var worldDelta = Quaternion.AngleAxis(-backwardAngle * factors[i], recoilAxis);
                var value = Quaternion.Inverse(target.parent.rotation) * worldDelta * target.rotation;
                if (i > 0 && Quaternion.Dot(quaternions[i - 1], value) < 0f)
                {
                    value = new Quaternion(-value.x, -value.y, -value.z, -value.w);
                }

                quaternions[i] = value;
            }

            var path = AnimationUtility.CalculateTransformPath(target, model);
            SetGraveHitFloatCurve(clip, path, "m_LocalRotation.x", quaternions.Select(value => value.x).ToArray());
            SetGraveHitFloatCurve(clip, path, "m_LocalRotation.y", quaternions.Select(value => value.y).ToArray());
            SetGraveHitFloatCurve(clip, path, "m_LocalRotation.z", quaternions.Select(value => value.z).ToArray());
            SetGraveHitFloatCurve(clip, path, "m_LocalRotation.w", quaternions.Select(value => value.w).ToArray());
        }

        private static void SetGraveHitFloatCurve(
            AnimationClip clip,
            string path,
            string propertyName,
            IReadOnlyList<float> values)
        {
            var keys = new Keyframe[GraveHitKeyTimes.Length];
            for (var i = 0; i < keys.Length; i++)
            {
                keys[i] = new Keyframe(GraveHitKeyTimes[i], values[i]);
            }

            var curve = new AnimationCurve(keys);
            for (var i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            }

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName),
                curve);
        }

        private static Transform RequireGraveHitBone(Transform model, string name)
        {
            return FindDescendant(model, name) ??
                throw new InvalidOperationException("Grave hit-recoil rig is missing " + name + ".");
        }

        private static string ValidateApprovedGraveHitRecoilScene(Scene scene, bool writeReport)
        {
            var graveRoot = RequireRootSceneObject(scene, GraveRootName).transform;
            var hitSlot = graveRoot.Find(GraveHitSlotName) ??
                throw new InvalidOperationException(GraveHitSlotName + " is missing.");
            var hitModel = hitSlot.Find(GraveModelName) ??
                throw new InvalidOperationException(GraveHitSlotName + "/" + GraveModelName + " is missing.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(GraveHitClipAssetPath) ??
                throw new InvalidOperationException("Grave hit-recoil clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(GraveHitControllerAssetPath) ??
                throw new InvalidOperationException("Grave hit-recoil controller is missing.");
            var animator = hitModel.GetComponent<Animator>() ??
                throw new InvalidOperationException("Grave hit-recoil Animator is missing.");
            if (animator.runtimeAnimatorController != controller || animator.applyRootMotion || !animator.enabled ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException("Grave hit-recoil Animator configuration mismatch.");
            }

            var defaultState = controller.layers.Length > 0 ? controller.layers[0].stateMachine.defaultState : null;
            if (defaultState == null || defaultState.name != "HitRecoil" || defaultState.motion != clip ||
                Mathf.Abs(defaultState.speed - 1f) > 0.0001f)
            {
                throw new InvalidOperationException("Grave hit-recoil controller default state mismatch.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || !settings.loopBlend || Mathf.Abs(clip.length - GraveHitDuration) > 0.001f)
            {
                throw new InvalidOperationException(
                    $"Grave hit-recoil clip configuration mismatch. Length={clip.length:0.######}, " +
                    $"LoopTime={settings.loopTime}, LoopBlend={settings.loopBlend}");
            }

            var animatedBones = new[]
            {
                RequireGraveHitBone(hitModel, "Spine02"),
                RequireGraveHitBone(hitModel, "Spine01"),
                RequireGraveHitBone(hitModel, "Spine"),
                RequireGraveHitBone(hitModel, "neck"),
                RequireGraveHitBone(hitModel, "Head")
            };
            var expectedBindings = new HashSet<string>();
            foreach (var bone in animatedBones)
            {
                var path = AnimationUtility.CalculateTransformPath(bone, hitModel);
                foreach (var component in new[] { "x", "y", "z", "w" })
                {
                    expectedBindings.Add(path + "|m_LocalRotation." + component);
                }
            }

            var bindings = AnimationUtility.GetCurveBindings(clip);
            var actualBindings = bindings.Select(binding => binding.path + "|" + binding.propertyName).ToHashSet();
            if (bindings.Length != expectedBindings.Count || !actualBindings.SetEquals(expectedBindings) ||
                bindings.Any(binding => string.IsNullOrEmpty(binding.path)) ||
                AnimationUtility.GetObjectReferenceCurveBindings(clip).Length != 0)
            {
                throw new InvalidOperationException(
                    "Grave hit-recoil clip must contain only upper-body rotation curves.");
            }

            var hips = RequireGraveHitBone(hitModel, "Hips");
            var lowerBodyBones = new[]
            {
                hips,
                RequireGraveHitBone(hitModel, "LeftUpLeg"), RequireGraveHitBone(hitModel, "LeftLeg"),
                RequireGraveHitBone(hitModel, "LeftFoot"), RequireGraveHitBone(hitModel, "LeftToeBase"),
                RequireGraveHitBone(hitModel, "RightUpLeg"), RequireGraveHitBone(hitModel, "RightLeg"),
                RequireGraveHitBone(hitModel, "RightFoot"), RequireGraveHitBone(hitModel, "RightToeBase")
            };
            var allTransforms = hitModel.GetComponentsInChildren<Transform>(true);
            var baseScales = allTransforms.Select(target => target.localScale).ToArray();
            var poses = CaptureLocalPoses(hitModel);
            var animatorWasEnabled = animator.enabled;
            var slotPosition = hitSlot.localPosition;
            var slotRotation = hitSlot.localRotation;
            var slotScale = hitSlot.localScale;
            var modelPosition = hitModel.localPosition;
            var modelRotation = hitModel.localRotation;
            var modelScale = hitModel.localScale;
            var visualFront = CalculateVisualFront(hitModel);
            var startAnimatedRotations = new Quaternion[animatedBones.Length];
            var startLowerBodyRotations = new Quaternion[lowerBodyBones.Length];
            var startNeckPosition = Vector3.zero;
            var startHeadPosition = Vector3.zero;
            var startTorsoDirection = Vector3.up;
            var maxBodyBackward = 0f;
            var maxHeadBackward = 0f;
            var maxTorsoLeanAngle = 0f;
            var maxBackwardTime = 0f;
            var maxLowerBodyRotationError = 0f;
            var maxScaleError = 0f;
            var maxGroundPenetration = 0f;
            var returnPoseError = 0f;
            var returnPositionError = 0f;
            try
            {
                animator.enabled = false;
                for (var sampleIndex = 0; sampleIndex < GraveHitKeyTimes.Length; sampleIndex++)
                {
                    RestoreLocalPoses(poses);
                    clip.SampleAnimation(hitModel.gameObject, GraveHitKeyTimes[sampleIndex]);
                    if (hitSlot.localPosition != slotPosition || hitSlot.localRotation != slotRotation || hitSlot.localScale != slotScale ||
                        hitModel.localPosition != modelPosition || hitModel.localRotation != modelRotation || hitModel.localScale != modelScale)
                    {
                        throw new InvalidOperationException("Grave hit-recoil clip contains slot or model root motion.");
                    }

                    var bounds = CalculateVisibleBounds(hitSlot);
                    maxGroundPenetration = Mathf.Max(maxGroundPenetration, graveRoot.position.y - bounds.min.y);
                    for (var transformIndex = 0; transformIndex < allTransforms.Length; transformIndex++)
                    {
                        maxScaleError = Mathf.Max(
                            maxScaleError,
                            Vector3.Distance(baseScales[transformIndex], allTransforms[transformIndex].localScale));
                    }

                    if (sampleIndex == 0)
                    {
                        for (var boneIndex = 0; boneIndex < animatedBones.Length; boneIndex++)
                        {
                            startAnimatedRotations[boneIndex] = animatedBones[boneIndex].localRotation;
                        }

                        for (var boneIndex = 0; boneIndex < lowerBodyBones.Length; boneIndex++)
                        {
                            startLowerBodyRotations[boneIndex] = lowerBodyBones[boneIndex].localRotation;
                        }

                        startNeckPosition = animatedBones[3].position;
                        startHeadPosition = animatedBones[4].position;
                        startTorsoDirection = (animatedBones[3].position - animatedBones[0].position).normalized;
                        continue;
                    }

                    for (var boneIndex = 0; boneIndex < lowerBodyBones.Length; boneIndex++)
                    {
                        maxLowerBodyRotationError = Mathf.Max(
                            maxLowerBodyRotationError,
                            Quaternion.Angle(startLowerBodyRotations[boneIndex], lowerBodyBones[boneIndex].localRotation));
                    }

                    var bodyBackward = Vector3.Dot(startNeckPosition - animatedBones[3].position, visualFront);
                    var headBackward = Vector3.Dot(startHeadPosition - animatedBones[4].position, visualFront);
                    var torsoDirection = (animatedBones[3].position - animatedBones[0].position).normalized;
                    var torsoLeanAngle = Vector3.Angle(startTorsoDirection, torsoDirection);
                    if (bodyBackward > maxBodyBackward)
                    {
                        maxBodyBackward = bodyBackward;
                        maxBackwardTime = GraveHitKeyTimes[sampleIndex];
                    }

                    maxHeadBackward = Mathf.Max(maxHeadBackward, headBackward);
                    maxTorsoLeanAngle = Mathf.Max(maxTorsoLeanAngle, torsoLeanAngle);
                    if (sampleIndex == GraveHitKeyTimes.Length - 1)
                    {
                        for (var boneIndex = 0; boneIndex < animatedBones.Length; boneIndex++)
                        {
                            returnPoseError = Mathf.Max(
                                returnPoseError,
                                Quaternion.Angle(startAnimatedRotations[boneIndex], animatedBones[boneIndex].localRotation));
                        }

                        returnPositionError = Mathf.Max(
                            Vector3.Distance(startNeckPosition, animatedBones[3].position),
                            Vector3.Distance(startHeadPosition, animatedBones[4].position));
                    }
                }
            }
            finally
            {
                RestoreLocalPoses(poses);
                animator.enabled = animatorWasEnabled;
            }

            if (maxBodyBackward < 0.08f || maxHeadBackward < 0.12f || maxTorsoLeanAngle < 22f ||
                maxBackwardTime < 0.12f || maxBackwardTime > 0.24f ||
                maxLowerBodyRotationError > 0.001f || maxScaleError > 0.00001f ||
                maxGroundPenetration > 0.012f || returnPoseError > 0.01f || returnPositionError > 0.001f)
            {
                throw new InvalidOperationException(
                    $"Grave hit-recoil sampled motion mismatch. BodyBack={maxBodyBackward:0.######}, " +
                    $"HeadBack={maxHeadBackward:0.######}, TorsoLean={maxTorsoLeanAngle:0.######}, " +
                    $"PeakTime={maxBackwardTime:0.###}, LowerBodyRotationError={maxLowerBodyRotationError:0.######}, " +
                    $"ScaleError={maxScaleError:0.######}, GroundPenetration={maxGroundPenetration:0.######}, " +
                    $"ReturnPoseError={returnPoseError:0.######}, ReturnPositionError={returnPositionError:0.######}");
            }

            var metrics =
                $"Target={GraveHitSlotName}/{GraveModelName}, Duration={clip.length:0.###}, " +
                $"LoopTime={settings.loopTime}, LoopBlend={settings.loopBlend}, CurveCount={bindings.Length}, " +
                $"BodyBack={maxBodyBackward:0.######}, HeadBack={maxHeadBackward:0.######}, " +
                $"TorsoLean={maxTorsoLeanAngle:0.######}, PeakTime={maxBackwardTime:0.###}, " +
                $"LowerBodyRotationError={maxLowerBodyRotationError:0.######}, ScaleError={maxScaleError:0.######}, " +
                $"GroundPenetration={maxGroundPenetration:0.######}, ReturnPoseError={returnPoseError:0.######}, " +
                $"ReturnPositionError={returnPositionError:0.######}, ModelRootMotion=0, SlotRootMotion=0";
            if (writeReport)
            {
                var folder = ProjectAbsolutePath(GraveHitValidationRelativeFolder);
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "GraveHitRecoilValidation.txt"), metrics + Environment.NewLine);
            }

            return metrics;
        }

        private static AnimationClip EnsureApprovedGraveIdleClip(Transform idleModel)
        {
            var spine02 = FindDescendant(idleModel, "Spine02") ??
                throw new InvalidOperationException("Grave idle rig is missing Spine02.");
            var spine01 = FindDescendant(idleModel, "Spine01") ??
                throw new InvalidOperationException("Grave idle rig is missing Spine01.");
            var spine = FindDescendant(idleModel, "Spine") ??
                throw new InvalidOperationException("Grave idle rig is missing Spine.");
            EnsureAssetDirectory(GraveIdleClipAssetPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(GraveIdleClipAssetPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = "Grave_Idle_Breathing", frameRate = 60f };
                AssetDatabase.CreateAsset(clip, GraveIdleClipAssetPath);
            }

            clip.ClearCurves();
            clip.legacy = false;
            clip.wrapMode = WrapMode.Loop;
            clip.frameRate = 60f;
            var spine02Path = AnimationUtility.CalculateTransformPath(spine02, idleModel);
            var spine01Path = AnimationUtility.CalculateTransformPath(spine01, idleModel);
            var spinePath = AnimationUtility.CalculateTransformPath(spine, idleModel);
            SetIdleFloatCurve(
                clip,
                spine02Path,
                "m_LocalPosition.y",
                CreateIdleBreathingCurve(spine02.localPosition.y, spine02.localPosition.y + GraveIdleMaxBodyRise));
            SetIdleScaleCurves(clip, spine02Path, spine02.localScale, GraveIdleSpine02CrossExpansion);
            SetIdleScaleCurves(clip, spine01Path, spine01.localScale, GraveIdleSpine01CrossExpansion);
            SetIdleScaleCurves(clip, spinePath, spine.localScale, GraveIdleSpineCrossExpansion);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            settings.cycleOffset = 0f;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimatorController EnsureApprovedGraveIdleController(AnimationClip clip)
        {
            EnsureAssetDirectory(GraveIdleControllerAssetPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(GraveIdleControllerAssetPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(GraveIdleControllerAssetPath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(candidate => candidate.name == "IdleBreathing");
            if (state == null)
            {
                state = stateMachine.AddState("IdleBreathing");
            }

            foreach (var child in stateMachine.states.ToArray())
            {
                if (child.state != state)
                {
                    stateMachine.RemoveState(child.state);
                }
            }

            foreach (var transition in state.transitions.ToArray())
            {
                state.RemoveTransition(transition);
            }

            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void SetIdleScaleCurves(
            AnimationClip clip,
            string path,
            Vector3 baseScale,
            float crossExpansion)
        {
            SetIdleFloatCurve(
                clip,
                path,
                "m_LocalScale.x",
                CreateIdleBreathingCurve(baseScale.x, baseScale.x * (1f + crossExpansion)));
            SetIdleFloatCurve(
                clip,
                path,
                "m_LocalScale.y",
                CreateIdleBreathingCurve(baseScale.y, baseScale.y * (1f + GraveIdleLengthExpansionPerBone)));
            SetIdleFloatCurve(
                clip,
                path,
                "m_LocalScale.z",
                CreateIdleBreathingCurve(baseScale.z, baseScale.z * (1f + crossExpansion)));
        }

        private static AnimationCurve CreateIdleBreathingCurve(float baseValue, float peakValue)
        {
            var midpoint = (baseValue + peakValue) * 0.5f;
            var curve = new AnimationCurve(
                new Keyframe(0f, baseValue),
                new Keyframe(GraveIdleDuration * 0.25f, midpoint),
                new Keyframe(GraveIdleDuration * 0.5f, peakValue),
                new Keyframe(GraveIdleDuration * 0.75f, midpoint),
                new Keyframe(GraveIdleDuration, baseValue));
            for (var i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            }

            return curve;
        }

        private static void SetIdleFloatCurve(AnimationClip clip, string path, string propertyName, AnimationCurve curve)
        {
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName),
                curve);
        }

        private static string ValidateApprovedGraveIdleBreathingScene(Scene scene, bool writeReport)
        {
            ValidateApprovedReproductionScene(scene, writeReport: false);
            var graveRoot = RequireSceneObject(scene, GraveRootName).transform;
            var idleSlot = graveRoot.Find(GraveIdleSlotName) ??
                throw new InvalidOperationException(GraveIdleSlotName + " is missing.");
            var idleModel = idleSlot.Find(GraveModelName) ??
                throw new InvalidOperationException(GraveIdleSlotName + "/" + GraveModelName + " is missing.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(GraveIdleClipAssetPath) ??
                throw new InvalidOperationException("Grave idle breathing clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(GraveIdleControllerAssetPath) ??
                throw new InvalidOperationException("Grave idle breathing controller is missing.");
            var animator = idleModel.GetComponent<Animator>();
            if (animator == null)
            {
                throw new InvalidOperationException("Grave idle Animator is missing.");
            }

            if (animator.runtimeAnimatorController != controller || animator.applyRootMotion || !animator.enabled ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException("Grave idle Animator configuration mismatch.");
            }

            var defaultState = controller.layers.Length > 0 ? controller.layers[0].stateMachine.defaultState : null;
            if (defaultState == null || defaultState.name != "IdleBreathing" || defaultState.motion != clip ||
                Mathf.Abs(defaultState.speed - 1f) > 0.0001f)
            {
                throw new InvalidOperationException("Grave idle controller default state mismatch.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || !settings.loopBlend || Mathf.Abs(clip.length - GraveIdleDuration) > 0.001f)
            {
                throw new InvalidOperationException(
                    $"Grave idle clip loop configuration mismatch. Length={clip.length:0.######}, " +
                    $"LoopTime={settings.loopTime}, LoopBlend={settings.loopBlend}");
            }

            var spine02 = FindDescendant(idleModel, "Spine02") ??
                throw new InvalidOperationException("Grave idle rig is missing Spine02.");
            var spine01 = FindDescendant(idleModel, "Spine01") ??
                throw new InvalidOperationException("Grave idle rig is missing Spine01.");
            var spine = FindDescendant(idleModel, "Spine") ??
                throw new InvalidOperationException("Grave idle rig is missing Spine.");
            var spine02Path = AnimationUtility.CalculateTransformPath(spine02, idleModel);
            var spine01Path = AnimationUtility.CalculateTransformPath(spine01, idleModel);
            var spinePath = AnimationUtility.CalculateTransformPath(spine, idleModel);
            var expectedBindings = new HashSet<string>
            {
                spine02Path + "|m_LocalPosition.y",
                spine02Path + "|m_LocalScale.x",
                spine02Path + "|m_LocalScale.y",
                spine02Path + "|m_LocalScale.z",
                spine01Path + "|m_LocalScale.x",
                spine01Path + "|m_LocalScale.y",
                spine01Path + "|m_LocalScale.z",
                spinePath + "|m_LocalScale.x",
                spinePath + "|m_LocalScale.y",
                spinePath + "|m_LocalScale.z"
            };
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var actualBindings = bindings.Select(binding => binding.path + "|" + binding.propertyName).ToHashSet();
            if (bindings.Length != expectedBindings.Count || !actualBindings.SetEquals(expectedBindings) ||
                AnimationUtility.GetObjectReferenceCurveBindings(clip).Length != 0)
            {
                throw new InvalidOperationException(
                    "Grave idle clip bindings must contain only Spine02 position and Spine02/Spine01/Spine scale curves.");
            }

            var spine02PositionCurve = GetRequiredIdleCurve(clip, spine02Path, "m_LocalPosition.y");
            var spine02ScaleXCurve = GetRequiredIdleCurve(clip, spine02Path, "m_LocalScale.x");
            var spine02ScaleYCurve = GetRequiredIdleCurve(clip, spine02Path, "m_LocalScale.y");
            var spine01ScaleXCurve = GetRequiredIdleCurve(clip, spine01Path, "m_LocalScale.x");
            var spine01ScaleYCurve = GetRequiredIdleCurve(clip, spine01Path, "m_LocalScale.y");
            var spineScaleXCurve = GetRequiredIdleCurve(clip, spinePath, "m_LocalScale.x");
            var spineScaleYCurve = GetRequiredIdleCurve(clip, spinePath, "m_LocalScale.y");
            var baseSpine02Y = spine02PositionCurve.Evaluate(0f);
            var measuredRise = spine02PositionCurve.Evaluate(GraveIdleDuration * 0.5f) - baseSpine02Y;
            var combinedCrossExpansion =
                (spine02ScaleXCurve.Evaluate(GraveIdleDuration * 0.5f) / spine02ScaleXCurve.Evaluate(0f)) *
                (spine01ScaleXCurve.Evaluate(GraveIdleDuration * 0.5f) / spine01ScaleXCurve.Evaluate(0f)) *
                (spineScaleXCurve.Evaluate(GraveIdleDuration * 0.5f) / spineScaleXCurve.Evaluate(0f)) - 1f;
            var combinedLengthExpansion =
                (spine02ScaleYCurve.Evaluate(GraveIdleDuration * 0.5f) / spine02ScaleYCurve.Evaluate(0f)) *
                (spine01ScaleYCurve.Evaluate(GraveIdleDuration * 0.5f) / spine01ScaleYCurve.Evaluate(0f)) *
                (spineScaleYCurve.Evaluate(GraveIdleDuration * 0.5f) / spineScaleYCurve.Evaluate(0f)) - 1f;
            if (measuredRise < 0.014f || measuredRise > GraveIdleMaxBodyRise + 0.0001f ||
                combinedCrossExpansion < 0.014f || combinedCrossExpansion > 0.0152f ||
                combinedLengthExpansion < 0.0055f || combinedLengthExpansion > 0.0062f)
            {
                throw new InvalidOperationException(
                    $"Grave idle motion amplitudes are outside the approved subtle range. Rise={measuredRise:0.######}, " +
                    $"CrossExpansion={combinedCrossExpansion:0.######}, LengthExpansion={combinedLengthExpansion:0.######}");
            }

            var graveAnimators = graveRoot.GetComponentsInChildren<Animator>(true);
            if (graveAnimators.Length != 1 || graveAnimators[0] != animator)
            {
                throw new InvalidOperationException(
                    $"Only {GraveIdleSlotName} may contain a Grave Animator. Actual={graveAnimators.Length}");
            }

            var animatorWasEnabled = animator.enabled;
            var poses = CaptureLocalPoses(idleModel);
            var slotPosition = idleSlot.localPosition;
            var slotRotation = idleSlot.localRotation;
            var slotScale = idleSlot.localScale;
            var modelPosition = idleModel.localPosition;
            var modelRotation = idleModel.localRotation;
            var modelScale = idleModel.localScale;
            var reviewTimes = new[] { 0f, 0.75f, 1.5f, 2.25f, GraveIdleDuration };
            var minBounds = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var maxBounds = Vector3.zero;
            var maxGroundPenetration = 0f;
            var maxSampledRise = 0f;
            Vector3 startSpine02Position = Vector3.zero;
            Vector3 startSpine02Scale = Vector3.one;
            Vector3 startSpine01Scale = Vector3.one;
            Vector3 startSpineScale = Vector3.one;
            var loopPoseError = 0f;
            try
            {
                animator.enabled = false;
                for (var i = 0; i < reviewTimes.Length; i++)
                {
                    RestoreLocalPoses(poses);
                    clip.SampleAnimation(idleModel.gameObject, reviewTimes[i]);
                    if (idleSlot.localPosition != slotPosition || idleSlot.localRotation != slotRotation || idleSlot.localScale != slotScale ||
                        idleModel.localPosition != modelPosition || idleModel.localRotation != modelRotation || idleModel.localScale != modelScale)
                    {
                        throw new InvalidOperationException("Grave idle clip contains slot or model root motion.");
                    }

                    var bounds = CalculateVisibleBounds(idleSlot);
                    minBounds = Vector3.Min(minBounds, bounds.size);
                    maxBounds = Vector3.Max(maxBounds, bounds.size);
                    maxGroundPenetration = Mathf.Max(maxGroundPenetration, graveRoot.position.y - bounds.min.y);
                    maxSampledRise = Mathf.Max(maxSampledRise, spine02.localPosition.y - baseSpine02Y);
                    if (i == 0)
                    {
                        startSpine02Position = spine02.localPosition;
                        startSpine02Scale = spine02.localScale;
                        startSpine01Scale = spine01.localScale;
                        startSpineScale = spine.localScale;
                    }
                    else if (i == reviewTimes.Length - 1)
                    {
                        loopPoseError = Mathf.Max(
                            Vector3.Distance(startSpine02Position, spine02.localPosition),
                            Mathf.Max(
                                Vector3.Distance(startSpine02Scale, spine02.localScale),
                                Mathf.Max(
                                    Vector3.Distance(startSpine01Scale, spine01.localScale),
                                    Vector3.Distance(startSpineScale, spine.localScale))));
                    }
                }
            }
            finally
            {
                RestoreLocalPoses(poses);
                animator.enabled = animatorWasEnabled;
            }

            var boundsWidthChange = maxBounds.x - minBounds.x;
            var boundsHeightChange = maxBounds.y - minBounds.y;
            if (maxGroundPenetration > 0.002f || loopPoseError > 0.0001f ||
                maxSampledRise > GraveIdleMaxBodyRise + 0.0001f ||
                boundsWidthChange < 0.002f || boundsWidthChange > 0.025f || boundsHeightChange > 0.04f)
            {
                throw new InvalidOperationException(
                    $"Grave idle sampled deformation mismatch. GroundPenetration={maxGroundPenetration:0.######}, " +
                    $"LoopPoseError={loopPoseError:0.######}, SampledRise={maxSampledRise:0.######}, " +
                    $"BoundsWidthChange={boundsWidthChange:0.######}, BoundsHeightChange={boundsHeightChange:0.######}");
            }

            var metrics =
                $"Target={GraveIdleSlotName}/{GraveModelName}, Duration={clip.length:0.###}, LoopTime={settings.loopTime}, " +
                $"LoopBlend={settings.loopBlend}, CurveCount={bindings.Length}, ModelRootMotion=0, SlotRootMotion=0, " +
                $"MaxBodyRise={measuredRise:0.######}, CombinedCrossExpansion={combinedCrossExpansion:0.######}, " +
                $"CombinedLengthExpansion={combinedLengthExpansion:0.######}, BoundsMin={FormatVector(minBounds)}, " +
                $"BoundsMax={FormatVector(maxBounds)}, BoundsWidthChange={boundsWidthChange:0.######}, " +
                $"BoundsHeightChange={boundsHeightChange:0.######}, GroundPenetration={maxGroundPenetration:0.######}, " +
                $"LoopPoseError={loopPoseError:0.######}, GraveAnimatorCount={graveAnimators.Length}";
            if (writeReport)
            {
                var folder = ProjectAbsolutePath(GraveIdleValidationRelativeFolder);
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "GraveIdleBreathingValidation.txt"), metrics + Environment.NewLine);
            }

            return metrics;
        }

        private static AnimationCurve GetRequiredIdleCurve(AnimationClip clip, string path, string propertyName)
        {
            var binding = AnimationUtility.GetCurveBindings(clip)
                .FirstOrDefault(candidate => candidate.path == path && candidate.propertyName == propertyName);
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            return curve ?? throw new InvalidOperationException($"Missing Grave idle curve: {path}/{propertyName}");
        }

        private static void EnsureAssetDirectory(string assetPath)
        {
            var directory = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(directory) || AssetDatabase.IsValidFolder(directory))
            {
                return;
            }

            Directory.CreateDirectory(ProjectAbsolutePath(directory));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void ValidateApprovedArtifactCopies()
        {
            var expectedFiles = new[]
            {
                ("artSample/enemies/grave/reproduction/grave_reproduction.fbx", ApprovedModelSha256),
                (ApprovedReproductionModelAssetPath, ApprovedModelSha256),
                ("artSample/enemies/grave/textures/grave_front_albedo.png", ApprovedFrontAlbedoSha256),
                (ApprovedFrontAlbedoAssetPath, ApprovedFrontAlbedoSha256),
                ("artSample/enemies/grave/textures/grave_textile_albedo.png", ApprovedTextileAlbedoSha256),
                (ApprovedTextileAlbedoAssetPath, ApprovedTextileAlbedoSha256),
                ("artSample/enemies/grave/textures/grave_fabric_normal.png", ApprovedNormalSha256),
                (ApprovedNormalAssetPath, ApprovedNormalSha256),
                ("artSample/enemies/grave/textures/grave_fabric_roughness.png", ApprovedRoughnessSha256),
                (ApprovedRoughnessAssetPath, ApprovedRoughnessSha256)
            };

            foreach (var (relativePath, expectedHash) in expectedFiles)
            {
                var absolutePath = ProjectAbsolutePath(relativePath);
                if (!File.Exists(absolutePath))
                {
                    throw new FileNotFoundException("Approved Grave reproduction artifact is missing.", absolutePath);
                }

                var actualHash = ComputeSha256(absolutePath);
                if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Approved Grave artifact hash mismatch: {relativePath}={actualHash}");
                }
            }
        }

        private static void ConfigureApprovedTextureImporters()
        {
            ConfigureApprovedTextureImporter(ApprovedFrontAlbedoAssetPath, false, true);
            ConfigureApprovedTextureImporter(ApprovedTextileAlbedoAssetPath, false, true);
            ConfigureApprovedTextureImporter(ApprovedNormalAssetPath, true, false);
            ConfigureApprovedTextureImporter(ApprovedRoughnessAssetPath, false, false);
        }

        private static void ConfigureApprovedModelImporter()
        {
            var importer = AssetImporter.GetAtPath(ApprovedReproductionModelAssetPath) as ModelImporter ??
                throw new InvalidOperationException("Approved Grave model importer is unavailable: " + ApprovedReproductionModelAssetPath);
            if (!importer.swapUVChannels)
            {
                // The approved Blender material explicitly uses GraveReferenceUV, exported as TEXCOORD_1.
                // URP Lit samples TEXCOORD_0, so make that approved projection the runtime primary UV.
                importer.swapUVChannels = true;
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureApprovedTextureImporter(string assetPath, bool normalMap, bool sRgb)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter ??
                throw new InvalidOperationException("Texture importer is unavailable: " + assetPath);
            var expectedType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            var changed = false;
            if (importer.textureType != expectedType)
            {
                importer.textureType = expectedType;
                changed = true;
            }

            if (importer.sRGBTexture != sRgb)
            {
                importer.sRGBTexture = sRgb;
                changed = true;
            }

            if (importer.wrapMode != TextureWrapMode.Repeat)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                changed = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }

            if (importer.maxTextureSize != 2048)
            {
                importer.maxTextureSize = 2048;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static Material EnsureApprovedMaterial(string materialPath, string albedoPath)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ??
                throw new InvalidOperationException("No supported Lit shader is available for the approved Grave materials.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                material.shader = shader;
            }

            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath) ??
                throw new InvalidOperationException("Approved Grave albedo texture is missing: " + albedoPath);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(ApprovedNormalAssetPath) ??
                throw new InvalidOperationException("Approved Grave normal texture is missing.");
            SetMaterialTexture(material, "_BaseMap", albedo);
            SetMaterialTexture(material, "_MainTex", albedo);
            SetMaterialTexture(material, "_BumpMap", normal);
            SetMaterialColor(material, "_BaseColor", Color.white);
            SetMaterialColor(material, "_Color", Color.white);
            SetMaterialFloat(material, "_Metallic", 0f);
            SetMaterialFloat(material, "_Smoothness", ApprovedSmoothness);
            SetMaterialFloat(material, "_Glossiness", ApprovedSmoothness);
            SetMaterialFloat(material, "_BumpScale", ApprovedNormalStrength);
            SetMaterialFloat(material, "_Surface", 0f);
            SetMaterialFloat(material, "_AlphaClip", 0f);
            material.EnableKeyword("_NORMALMAP");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = -1;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void ApplyApprovedMaterials(Transform model, Material frontMaterial, Material textileMaterial)
        {
            var body = FindDescendant(model, "Grave_Body");
            var renderer = body != null ? body.GetComponent<SkinnedMeshRenderer>() : null;
            if (renderer == null || renderer.sharedMesh == null || renderer.sharedMesh.subMeshCount != 2)
            {
                throw new InvalidOperationException("Approved Grave model must contain a two-submesh Grave_Body renderer.");
            }

            renderer.sharedMaterials = new[] { frontMaterial, textileMaterial };
            EditorUtility.SetDirty(renderer);
        }

        private static void ValidateApprovedMaterial(Material material, Texture2D albedo, Texture2D normal, string expectedPath)
        {
            if (!string.Equals(AssetDatabase.GetAssetPath(material), expectedPath, StringComparison.Ordinal) ||
                GetMaterialTexture(material, "_BaseMap", "_MainTex") != albedo ||
                GetMaterialTexture(material, "_BumpMap") != normal ||
                Mathf.Abs(GetMaterialFloat(material, "_Metallic") - 0f) > 0.0001f ||
                Mathf.Abs(GetMaterialFloat(material, "_Smoothness", "_Glossiness") - ApprovedSmoothness) > 0.0001f ||
                Mathf.Abs(GetMaterialFloat(material, "_BumpScale") - ApprovedNormalStrength) > 0.0001f ||
                !material.IsKeywordEnabled("_NORMALMAP"))
            {
                throw new InvalidOperationException("Approved Grave material configuration mismatch: " + expectedPath);
            }
        }

        private static void SetMaterialTexture(Material material, string propertyName, Texture texture)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }

        private static void SetMaterialColor(Material material, string propertyName, Color color)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, color);
            }
        }

        private static void SetMaterialFloat(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static Texture GetMaterialTexture(Material material, params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                if (material.HasProperty(propertyName))
                {
                    var texture = material.GetTexture(propertyName);
                    if (texture != null)
                    {
                        return texture;
                    }
                }
            }

            return null;
        }

        private static float GetMaterialFloat(Material material, params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                if (material.HasProperty(propertyName))
                {
                    return material.GetFloat(propertyName);
                }
            }

            return 0f;
        }

        private static Texture2D RenderReviewView(
            Camera camera,
            Light light,
            Bounds bounds,
            Vector3 cameraPosition,
            float orthographicSize,
            int width,
            int height)
        {
            camera.aspect = width / (float)height;
            camera.orthographicSize = orthographicSize;
            camera.transform.SetPositionAndRotation(
                cameraPosition,
                Quaternion.LookRotation(bounds.center - cameraPosition, Vector3.up));
            light.transform.rotation = Quaternion.LookRotation(bounds.center - cameraPosition, Vector3.up);
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                return texture;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static Bounds CalculateCombinedVisibleBounds(IEnumerable<Transform> roots)
        {
            var hasBounds = false;
            var combined = new Bounds();
            foreach (var root in roots)
            {
                var bounds = CalculateVisibleBounds(root);
                if (!hasBounds)
                {
                    combined = bounds;
                    hasBounds = true;
                }
                else
                {
                    combined.Encapsulate(bounds);
                }
            }

            if (!hasBounds)
            {
                throw new InvalidOperationException("No Grave animation slot bounds were found.");
            }

            return combined;
        }

        private static void DisableImportedHelpers(Transform model)
        {
            foreach (var target in model.GetComponentsInChildren<Transform>(true))
            {
                if (target.name == "Cube")
                {
                    target.gameObject.SetActive(false);
                }

                foreach (var camera in target.GetComponents<Camera>())
                {
                    camera.enabled = false;
                }

                foreach (var light in target.GetComponents<Light>())
                {
                    light.enabled = false;
                }
            }
        }

        private static void AlignCharacterToGround(Transform model, float groundY)
        {
            var bounds = CalculateVisibleBounds(model);
            model.position += Vector3.up * (groundY - bounds.min.y);
        }

        private static Vector3 CalculateVisualFront(Transform model)
        {
            var head = FindDescendant(model, "Head");
            var headFront = FindDescendant(model, "headfront");
            var front = head != null && headFront != null ? headFront.position - head.position : model.forward;
            front.y = 0f;
            return front.sqrMagnitude > 0.0001f ? front.normalized : model.forward;
        }

        private static Vector3 CalculateGraveAttackStableFront(Transform attackModel)
        {
            var front = Vector3.ProjectOnPlane(attackModel.forward, attackModel.up);
            return front.sqrMagnitude > 0.0001f ? front.normalized : attackModel.forward;
        }

        private static Bounds CalculateVisibleBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(false).Where(r => r.enabled).ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Grave has no visible renderers.");
            }

            var hasBounds = false;
            var bounds = new Bounds(root.position, Vector3.zero);
            foreach (var renderer in renderers)
            {
                Mesh mesh = null;
                var ownsMesh = false;
                if (renderer is SkinnedMeshRenderer skinnedRenderer)
                {
                    mesh = new Mesh();
                    skinnedRenderer.BakeMesh(mesh, false);
                    ownsMesh = true;
                }
                else
                {
                    mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                }

                try
                {
                    if (mesh == null || mesh.vertexCount == 0)
                    {
                        IncludeBounds(ref bounds, ref hasBounds, renderer.bounds);
                        continue;
                    }

                    foreach (var vertex in mesh.vertices)
                    {
                        var worldVertex = renderer.transform.TransformPoint(vertex);
                        if (!hasBounds)
                        {
                            bounds = new Bounds(worldVertex, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            bounds.Encapsulate(worldVertex);
                        }
                    }
                }
                finally
                {
                    if (ownsMesh)
                    {
                        UnityEngine.Object.DestroyImmediate(mesh);
                    }
                }
            }

            if (!hasBounds)
            {
                throw new InvalidOperationException("Grave visible renderers contain no geometry.");
            }

            return bounds;
        }

        private static void IncludeBounds(ref Bounds combined, ref bool hasBounds, Bounds addition)
        {
            if (!hasBounds)
            {
                combined = addition;
                hasBounds = true;
            }
            else
            {
                combined.Encapsulate(addition);
            }
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
        }

        private static Quaternion YawToward(Vector3 from, Vector3 to)
        {
            var direction = to - from;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction.normalized, Vector3.up) : Quaternion.identity;
        }

        private static GameObject RequireSceneObject(Scene scene, string name)
        {
            return FindSceneObject(scene, name) ?? throw new InvalidOperationException($"{name} is missing in CargoRunMvp scene.");
        }

        private static Scene RequireOpenCargoRunScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(scene.path, CargoRunScenePath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must already be the active scene. The attack workflow does not open or replace scenes.");
            }

            return scene;
        }

        private static GameObject RequireRootSceneObject(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name) ??
                throw new InvalidOperationException($"Root {name} is missing in CargoRunMvp scene.");
        }

        private static GameObject FindSceneObject(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var match = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
                if (match != null)
                {
                    return match.gameObject;
                }
            }

            return null;
        }

        private static List<RootState> CapturePreservedRoots(Scene scene, GameObject player)
        {
            return scene.GetRootGameObjects()
                .Where(root => root != player && root.name != GraveRootName)
                .Select(root => new RootState(root))
                .ToList();
        }

        private static void AssertPreservedRoots(IEnumerable<RootState> states)
        {
            foreach (var state in states)
            {
                state.AssertUnchanged();
            }
        }

        private static void ValidateSourceCopies()
        {
            foreach (var relativePath in new[] { GraveSourceRelativePath, GraveSampleRelativePath, GraveModelAssetPath })
            {
                var absolutePath = ProjectAbsolutePath(relativePath);
                if (!File.Exists(absolutePath))
                {
                    throw new FileNotFoundException("Required Grave FBX is missing.", absolutePath);
                }

                var hash = ComputeSha256(absolutePath);
                if (!string.Equals(hash, ExpectedSha256, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Grave FBX hash mismatch: {relativePath}={hash}");
                }
            }
        }

        private static string ComputeSha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string ProjectAbsolutePath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }

        private static void SaveCameraPng(Camera camera, string outputPath, int width, int height)
        {
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(texture);
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static Texture2D RenderCameraFrame(Camera camera, int width, int height)
        {
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                return texture;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static string FormatVector(Vector3 value)
        {
            return $"({value.x:0.###},{value.y:0.###},{value.z:0.###})";
        }

        private static LocalPoseState[] CaptureLocalPoses(Transform root)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .Select(target => new LocalPoseState(target))
                .ToArray();
        }

        private static void RestoreLocalPoses(IEnumerable<LocalPoseState> poses)
        {
            foreach (var pose in poses)
            {
                pose.Restore();
            }
        }

        private static void ForceSkinnedMeshRefresh(Transform root)
        {
            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var previousUpdateWhenOffscreen = renderer.updateWhenOffscreen;
                renderer.updateWhenOffscreen = true;
                var baked = new Mesh();
                renderer.BakeMesh(baked, false);
                UnityEngine.Object.DestroyImmediate(baked);
                renderer.updateWhenOffscreen = previousUpdateWhenOffscreen;
            }
        }

        private static Bounds CalculateBakedWorldBounds(SkinnedMeshRenderer renderer, Mesh baked)
        {
            var vertices = baked.vertices;
            if (vertices.Length == 0)
            {
                return new Bounds(renderer.transform.position, Vector3.zero);
            }

            var bounds = new Bounds(renderer.transform.TransformPoint(vertices[0]), Vector3.zero);
            for (var i = 1; i < vertices.Length; i++)
            {
                bounds.Encapsulate(renderer.transform.TransformPoint(vertices[i]));
            }

            return bounds;
        }

        private static Texture2D RenderGraveAttackBakedFrame(
            Camera camera,
            Light light,
            SkinnedMeshRenderer sourceRenderer,
            Bounds reviewBounds,
            Vector3 cameraPosition,
            float orthographicSize,
            int width,
            int height)
        {
            var baked = new Mesh();
            sourceRenderer.BakeMesh(baked, false);
            var proxy = new GameObject("Grave_Attack_BakedCaptureProxy");
            var sourceWasEnabled = sourceRenderer.enabled;
            try
            {
                proxy.transform.SetParent(sourceRenderer.transform.parent, false);
                proxy.transform.localPosition = sourceRenderer.transform.localPosition;
                proxy.transform.localRotation = sourceRenderer.transform.localRotation;
                proxy.transform.localScale = sourceRenderer.transform.localScale;
                proxy.layer = CaptureLayer;
                proxy.AddComponent<MeshFilter>().sharedMesh = baked;
                proxy.AddComponent<MeshRenderer>().sharedMaterials = sourceRenderer.sharedMaterials;
                sourceRenderer.enabled = false;
                return RenderReviewView(
                    camera,
                    light,
                    reviewBounds,
                    cameraPosition,
                    orthographicSize,
                    width,
                    height);
            }
            finally
            {
                sourceRenderer.enabled = sourceWasEnabled;
                UnityEngine.Object.DestroyImmediate(proxy);
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private sealed class LocalPoseState
        {
            private readonly Transform target;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            public LocalPoseState(Transform transform)
            {
                target = transform;
                localPosition = transform.localPosition;
                localRotation = transform.localRotation;
                localScale = transform.localScale;
            }

            public void Restore()
            {
                if (target == null)
                {
                    return;
                }

                target.localPosition = localPosition;
                target.localRotation = localRotation;
                target.localScale = localScale;
            }
        }

        private sealed class RootState
        {
            private readonly GameObject root;
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;
            private readonly bool active;
            private readonly int childCount;

            public RootState(GameObject rootObject)
            {
                root = rootObject;
                position = root.transform.position;
                rotation = root.transform.rotation;
                scale = root.transform.localScale;
                active = root.activeSelf;
                childCount = root.transform.childCount;
            }

            public void AssertUnchanged()
            {
                if (root == null || root.transform.position != position || root.transform.rotation != rotation ||
                    root.transform.localScale != scale || root.activeSelf != active || root.transform.childCount != childCount)
                {
                    throw new InvalidOperationException("Unapproved scene root changed while placing Grave: " + (root != null ? root.name : "destroyed root"));
                }
            }
        }
    }
}
