using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class ApprovedArmoryShellBootstrap
    {
        public const string RootName = "Approved Armory 01 Shell";

        private const string UnityAssetDirectory = "Assets/_Project/Art/Ship/Armory";
        private const string UnityFbxPath = UnityAssetDirectory + "/armory_shell.fbx";
        private const string SampleFbxRelativePath = "artSample/armory_shell/exports/armory_shell.fbx";
        private const string ApprovalStatusRelativePath = "artSample/armory_shell/APPROVAL_STATUS.json";
        private const string CurrentStateUnityPath = "Assets/_Project/Editor/Validation/ApprovedArmoryShellCurrentState.cs";
        private const string SupplyRoomRootName = "Approved Supply Room 01 Shell";
        private const string SupplyRoomApprovalStatusRelativePath = "artSample/supply_room_shell/APPROVAL_STATUS.json";
        private const string SupplyRoomCurrentStateUnityPath = "Assets/_Project/Editor/Validation/ApprovedSupplyRoomShellCurrentState.cs";
        private const string SupplyRoomHskOpenCloseTexturePath = "Assets/Heavy Station Kit/_common/Textures/GUI/HSK_Open_Close.png";
        private const string SupplyRoomSr07InactiveScreenName = "SR-07 visible ejection terminal inactive screen";
        private const string SupplyRoomSr07HskScreenName = "SR-07 ejection terminal HSK open close screen texture";
        private const string CargoHoldRootName = "Approved Cargo Hold 01 Shell";
        private const string CargoHoldApprovalStatusRelativePath = "artSample/cargo_hold_shell/APPROVAL_STATUS.json";
        private const string CargoHoldCurrentStateUnityPath = "Assets/_Project/Editor/Validation/ApprovedCargoHoldShellCurrentState.cs";
        private const string CargoHoldCh11DisplayTexturePath = "Assets/_Project/Art/Ship/CargoHold/Textures/B2_Eq2_E_bottom_right.png";
        private const float CargoHoldUnityScale = 2.2f;

        private static readonly Vector3 ArmoryCenterBelowControlRoom = new Vector3(13.20795f, -4.6f, 19.265f);

        [MenuItem("Bellerophon/Bootstrap/Ensure Approved Armory 01 Shell")]
        public static void EnsureApprovedArmoryShell()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            RequireApprovedSample();

            var protectedRoots = FindSceneRootObjectsExcept(RootName);
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);

            DeleteGeneratedObject(RootName);
            EnsureImportedSampleAsset();

            var samplePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityFbxPath);
            if (samplePrefab == null)
            {
                throw new InvalidOperationException("Approved armory shell FBX was not imported as a prefab asset: " + UnityFbxPath);
            }

            var root = new GameObject(RootName);
            root.transform.SetPositionAndRotation(ArmoryCenterBelowControlRoom, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            var instanceObject = PrefabUtility.InstantiatePrefab(samplePrefab, scene) as GameObject;
            if (instanceObject == null)
            {
                instanceObject = UnityEngine.Object.Instantiate(samplePrefab);
            }

            instanceObject.name = "AR-01 approved armory shell sample model";
            instanceObject.transform.SetParent(root.transform, false);
            instanceObject.transform.localPosition = Vector3.zero;
            instanceObject.transform.localRotation = Quaternion.identity;
            instanceObject.transform.localScale = Vector3.one;

            DisableAllColliders(root.transform);

            var armoryBounds = GetRendererBounds(root.transform);
            EnsureNoOverlap(armoryBounds, protectedRoots);
            EnsureProtectedObjectsUntouched(protectedSnapshots);

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Approved armory 01 shell applied. Root=" +
                RootName +
                "; Center=" +
                FormatVector(ArmoryCenterBelowControlRoom) +
                "; Bounds=" +
                FormatBounds(armoryBounds) +
                "; ExistingObjectsUntouched=True");
        }

        [MenuItem("Bellerophon/Bootstrap/Create Approved Supply Room 01 Shell")]
        public static void CreateApprovedSupplyRoomShell()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            RequireApprovedSupplyRoomSample();

            var protectedRoots = FindSceneRootObjectsExcept(SupplyRoomRootName);
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);

            DeleteGeneratedObject(SupplyRoomRootName);

            var root = new GameObject(SupplyRoomRootName);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            CreateSupplyRoomFromApprovedSample(root.transform);
            DisableAllColliders(root.transform);

            var targetPosition = FindSupplyRoomPlacement(root.transform, protectedRoots);
            root.transform.SetPositionAndRotation(targetPosition, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            var supplyBounds = GetRendererBounds(root.transform);
            EnsureSupplyRoomNoOverlap(supplyBounds, protectedRoots);
            EnsureProtectedObjectsUntouched(protectedSnapshots);

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Approved supply room 01 shell created. Root=" +
                SupplyRoomRootName +
                "; Center=" +
                FormatVector(targetPosition) +
                "; Bounds=" +
                FormatBounds(supplyBounds) +
                "; ArmoryLineAligned=True; ZBelowEngineRoom=True; ExistingObjectsUntouched=True");
        }

        [MenuItem("Bellerophon/Bootstrap/Create Approved Cargo Hold 01 Shell")]
        public static void CreateApprovedCargoHoldShell()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            RequireApprovedCargoHoldSample();

            var protectedRoots = FindSceneRootObjectsExcept(CargoHoldRootName);
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);

            DeleteGeneratedObject(CargoHoldRootName);

            var root = new GameObject(CargoHoldRootName);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one * CargoHoldUnityScale;

            CreateCargoHoldFromApprovedSample(root.transform);
            DisableAllColliders(root.transform);

            var targetPosition = FindCargoHoldPlacement(root.transform, protectedRoots);
            root.transform.SetPositionAndRotation(targetPosition, Quaternion.identity);
            root.transform.localScale = Vector3.one * CargoHoldUnityScale;

            var cargoBounds = GetRendererBounds(root.transform);
            EnsureCargoHoldPlacementConstraints(root.transform, cargoBounds);
            EnsureProtectedObjectsUntouched(protectedSnapshots);
            WriteCargoHoldCurrentStateScript(CaptureCurrentTransformStates(root.transform));

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Approved cargo hold 01 shell created. Root=" +
                CargoHoldRootName +
                "; Center=" +
                FormatVector(targetPosition) +
                "; Bounds=" +
                FormatBounds(cargoBounds) +
                "; UnityScale=" +
                CargoHoldUnityScale.ToString("0.00", CultureInfo.InvariantCulture) +
                "; XAlignedWithCockpit=True; ZBelowArmory=True; CurrentStateSaved=True; ExistingObjectsUntouched=True");
        }

        [MenuItem("Bellerophon/Bootstrap/Capture Approved Cargo Hold 01 Current State")]
        public static void CaptureApprovedCargoHoldShellCurrentState()
        {
            var scene = RequireCargoRunMvpActiveScene();
            var root = RequireObject(CargoHoldRootName);
            var states = CaptureCurrentTransformStates(root.transform);

            WriteCargoHoldCurrentStateScript(states);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "Approved cargo hold 01 current state capture saved. Root=" +
                CargoHoldRootName +
                "; TransformStates=" +
                states.Count.ToString(CultureInfo.InvariantCulture) +
                "; Output=" +
                CargoHoldCurrentStateUnityPath);
        }

        [MenuItem("Bellerophon/Bootstrap/Restore Approved Cargo Hold 01 Current State")]
        public static void RestoreApprovedCargoHoldShellCurrentState()
        {
            var scene = RequireCargoRunMvpActiveScene();
            var root = RequireObject(CargoHoldRootName);
            var protectedRoots = FindSceneRootObjectsExcept(CargoHoldRootName);
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);

            ApplyCapturedTransformStates(root.transform, ApprovedCargoHoldShellCurrentState.Transforms);
            EnsureExactCapturedHierarchy(root.transform, ApprovedCargoHoldShellCurrentState.Transforms);
            EnsureProtectedObjectsUntouched(protectedSnapshots);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "Approved cargo hold 01 current state restored and saved. Root=" +
                CargoHoldRootName +
                "; TransformStates=" +
                ApprovedCargoHoldShellCurrentState.Transforms.Length.ToString(CultureInfo.InvariantCulture) +
                "; ExistingObjectsUntouched=True");
        }

        [MenuItem("Bellerophon/Bootstrap/Add Approved Cargo Hold CH-10 Direction Markers Only")]
        public static void AddApprovedCargoHoldCh10DirectionMarkersOnly()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            RequireApprovedCargoHoldSample();

            var root = RequireObject(CargoHoldRootName);
            var protectedRoots = FindSceneRootObjectsExcept(CargoHoldRootName);
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);
            var nonCh10CargoSnapshots = CaptureNonCh10CargoHoldSnapshots(root.transform);

            DeleteExistingCargoHoldCh10Objects(root.transform);
            var mats = CreateCargoHoldMaterials();
            CreateCargoHoldCh10DirectionMarkersOnly(root.transform, mats);

            EnsureProtectedObjectsUntouched(nonCh10CargoSnapshots);
            EnsureOnlyCargoHoldCh10ObjectsAdded(root.transform, nonCh10CargoSnapshots);
            EnsureProtectedObjectsUntouched(protectedSnapshots);
            WriteCargoHoldCurrentStateScript(CaptureCurrentTransformStates(root.transform));

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Approved cargo hold CH-10 direction markers added only. Root=" +
                CargoHoldRootName +
                "; Ch10Objects=" +
                CountCargoHoldCh10Objects(root.transform).ToString(CultureInfo.InvariantCulture) +
                "; NonCh10CargoHoldObjectsUntouched=True; ExistingObjectsUntouched=True");
        }

        [MenuItem("Bellerophon/Bootstrap/Update Approved Cargo Hold Entrance Colors Only")]
        public static void UpdateApprovedCargoHoldEntranceColorsOnly()
        {
            var root = RequireObject(CargoHoldRootName);
            var mats = CreateCargoHoldMaterials();

            ApplyEntranceMaterial("CH-07 control connection east wall doorway colored threshold", mats["engine_marker"]);
            ApplyEntranceMaterial("CH-07 control corridor at 3 oclock colored threshold slab", mats["engine_marker"]);
            ApplyEntranceMaterial("CH-06 engine connection west wall doorway colored threshold", mats["control_marker"]);
            ApplyEntranceMaterial("CH-06 engine corridor at 9 oclock colored threshold slab", mats["control_marker"]);
            ApplyEntranceMaterial("CH-08 CH-09 aft connection wall doorway colored threshold 1", mats["armory_marker"]);
            ApplyEntranceMaterial("CH-09 supply corridor at left aft edge colored threshold slab", mats["armory_marker"]);
            ApplyEntranceMaterial("CH-08 CH-09 aft connection wall doorway colored threshold 2", mats["supply_marker"]);
            ApplyEntranceMaterial("CH-08 armory corridor at right aft edge colored threshold slab", mats["supply_marker"]);

            WriteCargoHoldCurrentStateScript(CaptureCurrentTransformStates(root.transform));
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(root.scene);

            Debug.Log(
                "Approved cargo hold entrance colors updated only. Root=" +
                CargoHoldRootName +
                "; Objects=8; CurrentStateSaved=True; ExistingObjectsUntouched=True");

            void ApplyEntranceMaterial(string objectName, Material material)
            {
                var target = FindCargoHoldChildByExactName(objectName);
                var renderer = target.GetComponent<Renderer>();
                if (renderer == null)
                {
                    renderer = target.GetComponentInChildren<Renderer>(true);
                }

                if (renderer == null)
                {
                    throw new InvalidOperationException("Cargo hold entrance color renderer not found: " + objectName);
                }

                renderer.sharedMaterial = material;
            }

            Transform FindCargoHoldChildByExactName(string objectName)
            {
                var transforms = root.transform.GetComponentsInChildren<Transform>(true);
                for (var i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i] != null && transforms[i].name == objectName)
                    {
                        return transforms[i];
                    }
                }

                throw new InvalidOperationException("Cargo hold entrance color object not found: " + objectName);
            }
        }

        [MenuItem("Bellerophon/Bootstrap/Update Approved Cargo Hold CH-11 Display Only")]
        public static void UpdateApprovedCargoHoldCh11DisplayOnly()
        {
            var root = RequireObject(CargoHoldRootName);
            var protectedRoots = FindSceneRootObjectsExcept(CargoHoldRootName);
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);
            var nonCh11DisplaySnapshots = CaptureNonCh11DisplayCargoHoldSnapshots(root.transform);

            AssetDatabase.ImportAsset(CargoHoldCh11DisplayTexturePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            DeleteExistingCargoHoldCh11DisplayObjects(root.transform);
            CreateCargoHoldCh11DisplayOnly(root.transform);

            EnsureProtectedObjectsUntouched(nonCh11DisplaySnapshots);
            EnsureOnlyCargoHoldCh11DisplayObjectsAdded(root.transform, nonCh11DisplaySnapshots);
            EnsureProtectedObjectsUntouched(protectedSnapshots);

            WriteCargoHoldCurrentStateScript(CaptureCurrentTransformStates(root.transform));

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(root.scene);
            EditorSceneManager.SaveScene(root.scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Approved cargo hold CH-11 display updated only. Root=" +
                CargoHoldRootName +
                "; Ch11DisplayObjects=" +
                CountCargoHoldCh11DisplayObjects(root.transform).ToString(CultureInfo.InvariantCulture) +
                "; NonCh11CargoHoldObjectsUntouched=True; ExistingObjectsUntouched=True; CurrentStateSaved=True");
        }

        [MenuItem("Bellerophon/Bootstrap/Move Approved Supply Room 01 Shell Below Engine Room")]
        public static void MoveApprovedSupplyRoomShellBelowEngineRoom()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            var root = RequireObject(SupplyRoomRootName);
            var protectedRoots = FindSceneRootObjectsExcept(SupplyRoomRootName);
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);
            var supplySnapshots = CaptureArmorySnapshots(root.transform);
            var originalPosition = root.transform.position;

            try
            {
                var targetPosition = FindSupplyRoomBelowEngineRoomPosition(root.transform, protectedRoots);
                root.transform.position = targetPosition;

                EnsureOnlySupplyRoomRootPositionChanged(root.transform, supplySnapshots, targetPosition);
                var supplyBounds = GetRendererBounds(root.transform);
                EnsureSupplyRoomNoOverlap(supplyBounds, protectedRoots);
                EnsureProtectedObjectsUntouched(protectedSnapshots);

                EditorUtility.SetDirty(root);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "Approved supply room 01 shell moved below engine room. Root=" +
                    SupplyRoomRootName +
                    "; From=" +
                    FormatVector(originalPosition) +
                    "; To=" +
                    FormatVector(targetPosition) +
                    "; RootRotationUnchanged=True; RootScaleUnchanged=True; ChildrenUntouched=True; ExistingObjectsUntouched=True");
            }
            catch
            {
                root.transform.position = originalPosition;
                throw;
            }
        }

        [MenuItem("Bellerophon/Bootstrap/Swap Approved Supply Room Cabinet And Ejection Bay")]
        public static void SwapApprovedSupplyRoomCabinetAndEjectionBay()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            var root = RequireObject(SupplyRoomRootName);
            var protectedRoots = FindSceneRootObjectsExcept(SupplyRoomRootName);
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);
            var supplySnapshots = CaptureArmorySnapshots(root.transform);
            var swapTargets = CollectSupplyRoomSwapTargets(root.transform);

            if (swapTargets.Count == 0)
            {
                throw new InvalidOperationException("No supply room cabinet or ejection bay objects were found to swap.");
            }

            var cabinetCount = 0;
            var ejectionCount = 0;
            foreach (var target in swapTargets)
            {
                if (IsSupplyRoomCabinetSwapTarget(target.name))
                {
                    cabinetCount++;
                }

                if (IsSupplyRoomEjectionSwapTarget(target.name))
                {
                    ejectionCount++;
                }
            }

            if (cabinetCount == 0 || ejectionCount == 0)
            {
                throw new InvalidOperationException(
                    "Supply room swap requires both cabinet and ejection objects. CabinetCount=" +
                    cabinetCount.ToString(CultureInfo.InvariantCulture) +
                    "; EjectionCount=" +
                    ejectionCount.ToString(CultureInfo.InvariantCulture));
            }

            foreach (var target in swapTargets)
            {
                var localPosition = target.localPosition;
                target.localPosition = new Vector3(localPosition.x, localPosition.y, -localPosition.z);
                target.localRotation = Quaternion.Euler(0f, 180f, 0f) * target.localRotation;
            }

            EnsureOnlySupplyRoomCabinetAndEjectionChanged(supplySnapshots, swapTargets);
            EnsureProtectedObjectsUntouched(protectedSnapshots);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Approved supply room cabinet and ejection bay swapped. Root=" +
                SupplyRoomRootName +
                "; CabinetObjects=" +
                cabinetCount.ToString(CultureInfo.InvariantCulture) +
                "; EjectionObjects=" +
                ejectionCount.ToString(CultureInfo.InvariantCulture) +
                "; NonSwapSupplyRoomObjectsUntouched=True; ExistingObjectsUntouched=True");
        }

        [MenuItem("Bellerophon/Bootstrap/Add Approved Supply Room SR-08 Only")]
        public static void AddApprovedSupplyRoomSr08Only()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            RequireApprovedSupplyRoomSr08Sample();

            var root = RequireObject(SupplyRoomRootName);
            var protectedRoots = FindSceneRootObjectsExcept(SupplyRoomRootName);
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);
            var nonSr08SupplySnapshots = CaptureNonSr08SupplyRoomSnapshots(root.transform);

            DeleteExistingSupplyRoomSr08Objects(root.transform);
            var mats = CreateSupplyRoomMaterials();
            CreateSupplySr08EjectionHazardFloorAtCurrentEjectionSide(root.transform, mats);

            EnsureProtectedObjectsUntouched(nonSr08SupplySnapshots);
            EnsureOnlySr08ObjectsAdded(root.transform, nonSr08SupplySnapshots);
            EnsureProtectedObjectsUntouched(protectedSnapshots);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Approved supply room SR-08 ejection floor panel added only. Root=" +
                SupplyRoomRootName +
                "; Sr08Objects=" +
                CountSupplyRoomSr08Objects(root.transform).ToString(CultureInfo.InvariantCulture) +
                "; NonSr08SupplyRoomObjectsUntouched=True; ExistingObjectsUntouched=True");
        }

        [MenuItem("Bellerophon/Bootstrap/Add Approved Supply Room SR-11 Text Only")]
        public static void AddApprovedSupplyRoomSr11TextOnly()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            RequireApprovedSupplyRoomSr11TextOnlySample();

            var root = RequireObject(SupplyRoomRootName);
            var protectedRoots = FindSceneRootObjectsExcept(SupplyRoomRootName);
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);
            var nonSr11TextSupplySnapshots = CaptureNonSr11TextSupplyRoomSnapshots(root.transform);

            DeleteExistingSupplyRoomSr11TextObjects(root.transform);
            var mats = CreateSupplyRoomMaterials();
            CreateSupplyRoomSr11TextOnly(root.transform, mats);

            EnsureProtectedObjectsUntouched(nonSr11TextSupplySnapshots);
            EnsureOnlySr11TextObjectsAdded(root.transform, nonSr11TextSupplySnapshots);
            EnsureProtectedObjectsUntouched(protectedSnapshots);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Approved supply room SR-11 corridor text added only. Root=" +
                SupplyRoomRootName +
                "; Sr11TextObjects=" +
                CountSupplyRoomSr11TextObjects(root.transform).ToString(CultureInfo.InvariantCulture) +
                "; NonSr11TextSupplyRoomObjectsUntouched=True; ExistingObjectsUntouched=True");
        }

        [MenuItem("Bellerophon/Bootstrap/Add Approved Supply Room SR-12 CCTV Only")]
        public static void AddApprovedSupplyRoomSr12CctvOnly()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            RequireApprovedSupplyRoomSr12CctvSample();

            var root = RequireObject(SupplyRoomRootName);
            var protectedRoots = FindSceneRootObjectsExcept(SupplyRoomRootName);
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);
            var nonSr12SupplySnapshots = CaptureNonSr12SupplyRoomSnapshots(root.transform);

            DeleteExistingSupplyRoomSr12Objects(root.transform);
            var mats = CreateSupplyRoomMaterials();
            CreateSupplyRoomSr12CctvOnly(root.transform, mats);

            EnsureProtectedObjectsUntouched(nonSr12SupplySnapshots);
            EnsureOnlySr12ObjectsAdded(root.transform, nonSr12SupplySnapshots);
            EnsureSupplyRoomSr12DoesNotOverlapLocker(root.transform);
            EnsureProtectedObjectsUntouched(protectedSnapshots);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Approved supply room SR-12 CCTV added only. Root=" +
                SupplyRoomRootName +
                "; Sr12Objects=" +
                CountSupplyRoomSr12Objects(root.transform).ToString(CultureInfo.InvariantCulture) +
                "; LockerOverlap=False; NonSr12SupplyRoomObjectsUntouched=True; ExistingObjectsUntouched=True");
        }

        [MenuItem("Bellerophon/Bootstrap/Add Approved Supply Room SR-07 HSK Screen Only")]
        public static void AddApprovedSupplyRoomSr07HskScreenOnly()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            var approvalPath = Path.Combine(ProjectRoot, SupplyRoomApprovalStatusRelativePath);
            if (!File.Exists(approvalPath))
            {
                throw new InvalidOperationException("Missing approved supply room sample status file: " + approvalPath);
            }

            var approval = File.ReadAllText(approvalPath);
            if (approval.IndexOf("\"objectId\": \"SR-07\"", StringComparison.Ordinal) < 0 ||
                approval.IndexOf("\"approvalState\": \"승인\"", StringComparison.Ordinal) < 0 ||
                approval.IndexOf("\"unityApplicationAllowed\": true", StringComparison.Ordinal) < 0)
            {
                throw new InvalidOperationException("Supply room SR-07 HSK terminal screen sample has not been approved for Unity application: " + approvalPath);
            }

            var texturePath = Path.Combine(ProjectRoot, SupplyRoomHskOpenCloseTexturePath);
            if (!File.Exists(texturePath))
            {
                throw new InvalidOperationException("Missing SR-07 HSK Open/Close texture file: " + texturePath);
            }

            var root = RequireObject(SupplyRoomRootName);
            var protectedRoots = FindSceneRootObjectsExcept(SupplyRoomRootName);
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);
            var nonSr07HskSupplySnapshots = new List<ProtectedTransformSnapshot>();
            var supplyTransforms = root.transform.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < supplyTransforms.Length; i++)
            {
                var transform = supplyTransforms[i];
                if (transform == null || string.Equals(transform.name, SupplyRoomSr07HskScreenName, StringComparison.Ordinal))
                {
                    continue;
                }

                nonSr07HskSupplySnapshots.Add(new ProtectedTransformSnapshot(
                    SupplyRoomRootName + "/" + GetRelativePath(root.transform, transform),
                    transform,
                    transform.localPosition,
                    transform.localRotation,
                    transform.localScale,
                    transform.gameObject.activeSelf));
            }

            var removals = new List<Transform>();
            for (var i = 0; i < supplyTransforms.Length; i++)
            {
                var transform = supplyTransforms[i];
                if (transform != null && transform != root.transform && string.Equals(transform.name, SupplyRoomSr07HskScreenName, StringComparison.Ordinal))
                {
                    removals.Add(transform);
                }
            }

            removals.Sort((left, right) => GetDepth(right).CompareTo(GetDepth(left)));
            for (var i = 0; i < removals.Count; i++)
            {
                if (removals[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(removals[i].gameObject);
                }
            }

            Transform inactiveScreen = null;
            supplyTransforms = root.transform.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < supplyTransforms.Length; i++)
            {
                if (supplyTransforms[i] != null && string.Equals(supplyTransforms[i].name, SupplyRoomSr07InactiveScreenName, StringComparison.Ordinal))
                {
                    inactiveScreen = supplyTransforms[i];
                    break;
                }
            }

            if (inactiveScreen == null)
            {
                throw new InvalidOperationException("Missing supply room child object: " + SupplyRoomSr07InactiveScreenName);
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SupplyRoomHskOpenCloseTexturePath);
            if (texture == null)
            {
                throw new InvalidOperationException("Missing SR-07 HSK Open/Close texture: " + SupplyRoomHskOpenCloseTexturePath);
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Texture");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                throw new InvalidOperationException("No shader was found for SR-07 HSK terminal screen material.");
            }

            var material = new Material(shader)
            {
                name = "SR-07 HSK open close terminal screen material",
                mainTexture = texture,
                color = Color.white
            };

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 0f);
            }

            var visibleForward = -(inactiveScreen.localRotation * Vector3.forward);
            var visibleUp = inactiveScreen.localRotation * Vector3.up;
            var screenObject = new GameObject(SupplyRoomSr07HskScreenName);
            screenObject.transform.SetParent(root.transform, false);
            screenObject.transform.localPosition = inactiveScreen.localPosition + visibleForward.normalized * 0.021f;
            screenObject.transform.localRotation = Quaternion.LookRotation(visibleForward, visibleUp);
            screenObject.transform.localScale = Vector3.one;

            const float screenWidth = 0.59f;
            const float screenHeight = 0.405f;
            const float uvMinX = 0.86f;
            const float uvMinY = 0.075f;
            const float uvMaxX = 0.0f;
            const float uvMaxY = 0.925f;
            var halfWidth = screenWidth * 0.5f;
            var halfHeight = screenHeight * 0.5f;
            var mesh = new Mesh
            {
                name = "SR-07 HSK open close terminal screen mesh",
                vertices = new[]
                {
                    new Vector3(-halfWidth, -halfHeight, 0f),
                    new Vector3(-halfWidth, halfHeight, 0f),
                    new Vector3(halfWidth, halfHeight, 0f),
                    new Vector3(halfWidth, -halfHeight, 0f),
                },
                triangles = new[] { 0, 2, 1, 0, 3, 2 },
                uv = new[]
                {
                    new Vector2(uvMinX, uvMinY),
                    new Vector2(uvMinX, uvMaxY),
                    new Vector2(uvMaxX, uvMaxY),
                    new Vector2(uvMaxX, uvMinY),
                }
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            screenObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            screenObject.AddComponent<MeshRenderer>().sharedMaterial = material;

            EnsureProtectedObjectsUntouched(nonSr07HskSupplySnapshots);
            var protectedTransforms = new HashSet<Transform>();
            for (var i = 0; i < nonSr07HskSupplySnapshots.Count; i++)
            {
                if (nonSr07HskSupplySnapshots[i].Transform != null)
                {
                    protectedTransforms.Add(nonSr07HskSupplySnapshots[i].Transform);
                }
            }

            supplyTransforms = root.transform.GetComponentsInChildren<Transform>(true);
            var hskScreenCount = 0;
            for (var i = 0; i < supplyTransforms.Length; i++)
            {
                var transform = supplyTransforms[i];
                if (transform == null || protectedTransforms.Contains(transform))
                {
                    continue;
                }

                if (!string.Equals(transform.name, SupplyRoomSr07HskScreenName, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Non-SR-07 HSK screen supply room object was added during SR-07-HSK-only update: " + GetRelativePath(root.transform, transform));
                }

                hskScreenCount++;
            }

            EnsureProtectedObjectsUntouched(protectedSnapshots);

            WriteSupplyRoomCurrentStateScript(CaptureCurrentTransformStates(root.transform));

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Approved supply room SR-07 HSK terminal screen added only. Root=" +
                SupplyRoomRootName +
                "; HskScreenObjects=" +
                hskScreenCount.ToString(CultureInfo.InvariantCulture) +
                "; NonSr07HskSupplyRoomObjectsUntouched=True; ExistingObjectsUntouched=True; CurrentStateSaved=True");
        }

        [MenuItem("Bellerophon/Bootstrap/Capture Approved Supply Room 01 Current State")]
        public static void CaptureApprovedSupplyRoomShellCurrentState()
        {
            var scene = RequireCargoRunMvpActiveScene();
            var root = RequireObject(SupplyRoomRootName);
            var states = CaptureCurrentTransformStates(root.transform);

            WriteSupplyRoomCurrentStateScript(states);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "Approved supply room 01 current state capture saved. Root=" +
                SupplyRoomRootName +
                "; TransformStates=" +
                states.Count.ToString(CultureInfo.InvariantCulture) +
                "; Output=" +
                SupplyRoomCurrentStateUnityPath);
        }

        [MenuItem("Bellerophon/Bootstrap/Restore Approved Supply Room 01 Current State")]
        public static void RestoreApprovedSupplyRoomShellCurrentState()
        {
            var scene = RequireCargoRunMvpActiveScene();
            var root = RequireObject(SupplyRoomRootName);
            var protectedRoots = FindSceneRootObjectsExcept(SupplyRoomRootName);
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);

            ApplyCapturedTransformStates(root.transform, ApprovedSupplyRoomShellCurrentState.Transforms);
            EnsureExactCapturedHierarchy(root.transform, ApprovedSupplyRoomShellCurrentState.Transforms);
            EnsureProtectedObjectsUntouched(protectedSnapshots);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "Approved supply room 01 current state restored and saved. Root=" +
                SupplyRoomRootName +
                "; TransformStates=" +
                ApprovedSupplyRoomShellCurrentState.Transforms.Length.ToString(CultureInfo.InvariantCulture) +
                "; ExistingObjectsUntouched=True");
        }

        [MenuItem("Bellerophon/Bootstrap/Restore Approved Supply Room 01 Current State By Name")]
        public static void RestoreApprovedSupplyRoomShellCurrentStateByName()
        {
            var scene = RequireCargoRunMvpActiveScene();
            var root = RequireObject(SupplyRoomRootName);
            var protectedRoots = FindSceneRootObjectsExcept(SupplyRoomRootName);
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);
            var mats = CreateSupplyRoomMaterials();

            DeleteExistingSupplyRoomSr08Objects(root.transform);
            CreateSupplySr08EjectionHazardFloorAtCurrentEjectionSide(root.transform, mats);
            DeleteExistingSupplyRoomSr11TextObjects(root.transform);
            CreateSupplyRoomSr11TextOnly(root.transform, mats);
            DeleteExistingSupplyRoomSr12Objects(root.transform);
            CreateSupplyRoomSr12CctvOnly(root.transform, mats);

            var appliedCount = ApplyCapturedTransformStatesByName(
                root.transform,
                ApprovedSupplyRoomShellCurrentState.Transforms);

            EnsureProtectedObjectsUntouched(protectedSnapshots);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "Approved supply room 01 current state restored by name and saved. Root=" +
                SupplyRoomRootName +
                "; TransformStates=" +
                ApprovedSupplyRoomShellCurrentState.Transforms.Length.ToString(CultureInfo.InvariantCulture) +
                "; Applied=" +
                appliedCount.ToString(CultureInfo.InvariantCulture) +
                "; ExistingObjectsUntouched=True");
        }

        [MenuItem("Bellerophon/Bootstrap/Move Approved Armory 01 Shell To Z Below Control Room")]
        public static void MoveApprovedArmoryShellToZBelowControlRoom()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            var root = RequireObject(RootName);
            var protectedRoots = FindSceneRootObjectsExcept(RootName);
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);
            var armorySnapshots = CaptureArmorySnapshots(root.transform);
            var originalPosition = root.transform.position;

            try
            {
                var targetPosition = FindFirstNonOverlappingZBelowPosition(root.transform, protectedRoots);
                root.transform.position = targetPosition;

                EnsureOnlyArmoryRootPositionChanged(root.transform, armorySnapshots, targetPosition);
                var armoryBounds = GetRendererBounds(root.transform);
                EnsureNoOverlap(armoryBounds, protectedRoots);
                EnsureProtectedObjectsUntouched(protectedSnapshots);

                EditorUtility.SetDirty(root);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "Approved armory 01 shell moved on Z axis only. Root=" +
                    RootName +
                    "; From=" +
                    FormatVector(originalPosition) +
                    "; To=" +
                    FormatVector(targetPosition) +
                    "; RootRotationUnchanged=True; RootScaleUnchanged=True; ChildrenUntouched=True; ExistingObjectsUntouched=True");
            }
            catch
            {
                root.transform.position = originalPosition;
                throw;
            }
        }

        [MenuItem("Bellerophon/Bootstrap/Update Approved Armory AR-03 Only")]
        public static void UpdateApprovedArmoryAr03Only()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            var root = RequireObject(RootName);
            var protectedRoots = FindSceneRootObjectsExcept(RootName);
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);
            var nonAr03ArmorySnapshots = CaptureNonAr03ArmorySnapshots(root.transform);

            var stairMaterial = FindFirstSharedMaterial(root.transform, "AR-03 placeholder rear stair tread");
            var railMaterial = FindFirstSharedMaterial(root.transform, "AR-03 placeholder stair side rail");

            DeleteExistingAr03Objects(root.transform);
            CreateAr03FromApprovedSample(root.transform, stairMaterial, railMaterial);

            EnsureProtectedObjectsUntouched(protectedSnapshots);
            EnsureProtectedObjectsUntouched(nonAr03ArmorySnapshots);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Approved armory AR-03 stairs updated only. Root=" +
                RootName +
                "; Treads=12; Rails=2; ExistingObjectsUntouched=True; NonAr03ArmoryObjectsUntouched=True");
        }

        [MenuItem("Bellerophon/Bootstrap/Update Approved Armory AR-05 Only")]
        public static void UpdateApprovedArmoryAr05Only()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            var root = RequireObject(RootName);
            var protectedRoots = FindSceneRootObjectsExcept(RootName);
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);
            var nonAr05ArmorySnapshots = CaptureNonAr05ArmorySnapshots(root.transform);

            var consoleMaterial = FindFirstSharedMaterial(root.transform, "AR-05 placeholder turret handle console base") ??
                                  FindFirstSharedMaterial(root.transform, "AR-05 U-yoke hinge housing");
            var railMaterial = FindFirstSharedMaterial(root.transform, "AR-05 placeholder handle support column") ??
                               FindFirstSharedMaterial(root.transform, "AR-05 U-yoke angled support column");
            var handleMaterial = FindFirstSharedMaterial(root.transform, "AR-05 placeholder horizontal grip bar") ??
                                 FindFirstSharedMaterial(root.transform, "AR-05 placeholder two-hand turret wheel") ??
                                 FindFirstSharedMaterial(root.transform, "AR-05 U-shaped turret handle lower crossbar");
            var thumbSwitchMaterial = FindFirstSharedMaterial(root.transform, "AR-05 left thumb switch top cap") ??
                                      CreateAr05ThumbSwitchMaterial();

            DeleteExistingAr05Objects(root.transform);
            CreateAr05FromApprovedSample(root.transform, consoleMaterial, railMaterial, handleMaterial, thumbSwitchMaterial);

            EnsureProtectedObjectsUntouched(protectedSnapshots);
            EnsureProtectedObjectsUntouched(nonAr05ArmorySnapshots);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "Approved armory AR-05 turret handle updated only. Root=" +
                RootName +
                "; Shape=U-yoke-thumb-switch; ExistingObjectsUntouched=True; NonAr05ArmoryObjectsUntouched=True");
        }

        [MenuItem("Bellerophon/Bootstrap/Update Approved Armory AR-02 AR-03 Only")]
        public static void UpdateApprovedArmoryAr02Ar03Only()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            var root = RequireObject(RootName);
            var protectedRoots = FindSceneRootObjectsExcept(RootName);
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);
            var nonAr02Ar03ArmorySnapshots = CaptureNonAr02Ar03ArmorySnapshots(root.transform);

            var pillarMaterial = FindFirstSharedMaterial(root.transform, "AR-02 placeholder central turret support pillar");
            var stairMaterial = FindFirstSharedMaterial(root.transform, "AR-03 placeholder rear stair tread");
            var railMaterial = FindFirstSharedMaterial(root.transform, "AR-03 placeholder stair side rail");

            DeleteExistingAr02Objects(root.transform);
            DeleteExistingAr03Objects(root.transform);
            CreateAr02FromApprovedSample(root.transform, pillarMaterial);
            CreateAr03LowEightStepFromApprovedSample(root.transform, stairMaterial, railMaterial);
            MoveAr04PlatformToLowerPillarHeight(root.transform);

            EnsureProtectedObjectsUntouched(protectedSnapshots);
            EnsureProtectedObjectsUntouched(nonAr02Ar03ArmorySnapshots);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "Approved armory AR-02 pillar and AR-03 stairs updated only. Root=" +
                RootName +
                "; PillarHeight=0.48; Treads=5; PlatformHeight=0.48; ExistingObjectsUntouched=True; NonAr02Ar03ArmoryObjectsUntouched=True");
        }

        [MenuItem("Bellerophon/Bootstrap/Capture Approved Armory 01 Current State")]
        public static void CaptureCurrentEditorObjects()
        {
            var scene = RequireCargoRunMvpActiveScene();
            var root = RequireObject(RootName);
            var states = CaptureCurrentTransformStates(root.transform);

            WriteCurrentStateScript(states);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "Approved armory 01 current state captured. Root=" +
                RootName +
                "; TransformCount=" +
                states.Count.ToString(CultureInfo.InvariantCulture) +
                "; Output=" +
                CurrentStateUnityPath);
        }

        [MenuItem("Bellerophon/Bootstrap/Restore Approved Armory 01 Current State")]
        public static void RestoreApprovedArmoryShellCurrentState()
        {
            var scene = RequireCargoRunMvpActiveScene();
            var root = RequireObject(RootName);
            var protectedRoots = FindSceneRootObjectsExcept(RootName);
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);

            ApplyCapturedTransformStates(root.transform, ApprovedArmoryShellCurrentState.Transforms);
            EnsureExactCapturedHierarchy(root.transform, ApprovedArmoryShellCurrentState.Transforms);
            EnsureProtectedObjectsUntouched(protectedSnapshots);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "Approved armory 01 current state restored and saved. Root=" +
                RootName +
                "; TransformCount=" +
                ApprovedArmoryShellCurrentState.Transforms.Length.ToString(CultureInfo.InvariantCulture) +
                "; ExistingObjectsUntouched=True");
        }

        private static void RequireApprovedSupplyRoomSample()
        {
            var approvalPath = Path.Combine(ProjectRoot, SupplyRoomApprovalStatusRelativePath);
            if (!File.Exists(approvalPath))
            {
                throw new InvalidOperationException("Missing approved supply room sample status file: " + approvalPath);
            }

            var approval = File.ReadAllText(approvalPath);
            if (approval.IndexOf("\"approvalState\": \"승인\"", StringComparison.Ordinal) < 0 ||
                approval.IndexOf("\"unityApplicationAllowed\": true", StringComparison.Ordinal) < 0)
            {
                throw new InvalidOperationException("Supply room shell sample has not been approved for Unity application: " + approvalPath);
            }
        }

        private static void CreateSupplyRoomFromApprovedSample(Transform root)
        {
            var mats = CreateSupplyRoomMaterials();

            CreateSupplyBox("SR-01 sealed supply room deck floor", root, new Vector3(0f, 0f, 0f), new Vector3(7.2f, 5.8f, 0.16f), mats["floor"]);
            CreateSupplyPlainWallY(root, "SR-02 north supply storage wall shell", 2.9f, mats);
            CreateSupplyPlainWallY(root, "SR-05 south ejection bay wall shell", -2.9f, mats);
            CreateSupplyPlainWallX(root, "SR-01 west empty wall shell", -3.6f, mats);
            CreateSupplyEastSharedCorridorWall(root, mats);

            CreateSupplyFloorGrid(root, mats);
            CreateSupplyStorageWall(root, mats);
            CreateSupplyEjectionWall(root, mats);
            CreateSupplyEmptyWall(root, mats);
            CreateSupplyCorridorStub(root, "SR-09 armory direction", 1.08f, "ARMORY", mats["armory_marker"], mats);
            CreateSupplyCorridorStub(root, "SR-10 cargo hold direction", -1.18f, "CARGO HOLD", mats["cargo_marker"], mats);
            CreateSupplyOutlineMarkers(root, mats);
            CreateSupplyText("SR-01 room shell title floor label", root, "SUPPLY ROOM SHELL", new Vector3(0f, 0f, 0.245f), Quaternion.Euler(90f, 0f, 0f), 0.20f, mats["label_text"]);
        }

        private static void RequireApprovedCargoHoldSample()
        {
            var approvalPath = Path.Combine(ProjectRoot, CargoHoldApprovalStatusRelativePath);
            if (!File.Exists(approvalPath))
            {
                throw new InvalidOperationException("Missing approved cargo hold sample status file: " + approvalPath);
            }

            var approval = File.ReadAllText(approvalPath);
            var approvedState =
                approval.IndexOf("\"approvalState\": \"approved_with_unity_constraints\"", StringComparison.Ordinal) >= 0 ||
                approval.IndexOf("\"approvalState\": \"approved\"", StringComparison.Ordinal) >= 0 ||
                approval.IndexOf("\"approvalState\": \"승인\"", StringComparison.Ordinal) >= 0;
            if (!approvedState ||
                approval.IndexOf("\"unityApplicationAllowed\": true", StringComparison.Ordinal) < 0)
            {
                throw new InvalidOperationException("Cargo hold shell sample has not been approved for Unity application: " + approvalPath);
            }
        }

        private static void CreateCargoHoldFromApprovedSample(Transform root)
        {
            var mats = CreateCargoHoldMaterials();

            CreateSupplyBox("CH-01 sealed cargo hold deck floor", root, new Vector3(0f, 0f, 0f), new Vector3(9.8f, 8.7f, 0.18f), mats["floor"]);
            CreateCargoWallYWithDoors(root, "CH-05 cockpit connection north wall", 4.35f, new[] { 0f }, new[] { mats["cockpit_marker"] }, mats);
            CreateCargoWallYWithDoors(root, "CH-08 CH-09 aft connection wall", -4.35f, new[] { -3.58f, 3.58f }, new[] { mats["armory_marker"], mats["supply_marker"] }, mats);
            CreateCargoWallXWithDoor(root, "CH-06 engine connection west wall", -4.9f, 0f, mats["control_marker"], mats);
            CreateCargoWallXWithDoor(root, "CH-07 control connection east wall", 4.9f, 0f, mats["engine_marker"], mats);

            CreateCargoCorridorStub(root, "CH-05 cockpit corridor at 12 oclock", new Vector3(0f, 5.40f, 0f), new Vector3(1.87f, 2.10f, 0.18f), false, mats["cockpit_marker"], mats);
            CreateCargoCorridorStub(root, "CH-07 control corridor at 3 oclock", new Vector3(6.00f, 0f, 0f), new Vector3(2.18f, 1.87f, 0.18f), true, mats["engine_marker"], mats);
            CreateCargoCorridorStub(root, "CH-06 engine corridor at 9 oclock", new Vector3(-6.00f, 0f, 0f), new Vector3(2.18f, 1.87f, 0.18f), true, mats["control_marker"], mats);
            CreateCargoCorridorStub(root, "CH-08 armory corridor at right aft edge", new Vector3(3.58f, -5.40f, 0f), new Vector3(1.87f, 2.10f, 0.18f), false, mats["supply_marker"], mats);
            CreateCargoCorridorStub(root, "CH-09 supply corridor at left aft edge", new Vector3(-3.58f, -5.40f, 0f), new Vector3(1.87f, 2.10f, 0.18f), false, mats["armory_marker"], mats);

            CreateCargoFloorGrid(root, mats);
            CreateCargoEdgeWalkway(root, mats);
            CreateCargoContainer(root, mats);
            CreateCargoStatusPanel(root, mats);
            CreateCargoWallDressing(root, mats);
            CreateCargoHoldCh10DirectionMarkersOnly(root, mats);
            CreateSupplyText("CH-01 cargo hold shell title floor label", root, "CARGO HOLD SHELL", new Vector3(0f, 3.05f, 0.245f), Quaternion.Euler(90f, 0f, 0f), 0.22f, mats["label_text"]);
        }

        private static Dictionary<string, Material> CreateCargoHoldMaterials()
        {
            return new Dictionary<string, Material>(StringComparer.Ordinal)
            {
                { "floor", CreateSupplyMaterial("CH-01 worn cargo hold deck", new Color(0.14f, 0.16f, 0.16f, 1f), 0.24f, 0.84f) },
                { "floor_panel", CreateSupplyMaterial("CH-01 removable cargo deck plate", new Color(0.20f, 0.21f, 0.19f, 1f), 0.22f, 0.86f) },
                { "deck_rib", CreateSupplyMaterial("CH-01 dark raised cargo deck rib", new Color(0.07f, 0.08f, 0.08f, 1f), 0.24f, 0.84f) },
                { "wall", CreateSupplyMaterial("CH-01 thick cargo hold armored wall", new Color(0.22f, 0.25f, 0.25f, 1f), 0.18f, 0.82f) },
                { "wall_dark", CreateSupplyMaterial("CH-01 dark cargo corridor wall", new Color(0.09f, 0.11f, 0.12f, 1f), 0.16f, 0.86f) },
                { "door_frame", CreateSupplyMaterial("CH-01 heavy cargo doorway frame", new Color(0.36f, 0.35f, 0.30f, 1f), 0.24f, 0.76f) },
                { "beam", CreateSupplyMaterial("CH-01 cargo wall structural rib", new Color(0.29f, 0.31f, 0.29f, 1f), 0.18f, 0.82f) },
                { "corridor_floor", CreateSupplyMaterial("CH-01 corridor continuation steel", new Color(0.16f, 0.19f, 0.19f, 1f), 0.18f, 0.84f) },
                { "walkway", CreateSupplyMaterial("CH-06 raised edge handling walkway", new Color(0.19f, 0.20f, 0.18f, 1f), 0.24f, 0.82f) },
                { "cargo_zone", CreateSupplyMaterial("CH-02 central cargo locked zone plate", new Color(0.18f, 0.23f, 0.20f, 1f), 0.20f, 0.82f) },
                { "container_body", CreateSupplyMaterial("CH-03 single central container worn body", new Color(0.48f, 0.37f, 0.24f, 1f), 0.18f, 0.86f) },
                { "container_panel", CreateSupplyMaterial("CH-03 single central container darker side panel", new Color(0.34f, 0.27f, 0.20f, 1f), 0.16f, 0.88f) },
                { "container_edge", CreateSupplyMaterial("CH-03 single central container reinforced edge", new Color(0.17f, 0.18f, 0.16f, 1f), 0.28f, 0.78f) },
                { "screen_frame", CreateSupplyMaterial("CH-11 cargo status panel frame", new Color(0.12f, 0.13f, 0.13f, 1f), 0.20f, 0.80f) },
                { "screen_glow", CreateSupplyMaterial("CH-11 cargo status panel green display", new Color(0.08f, 0.44f, 0.32f, 1f), 0f, 0.42f) },
                { "screen_dim", CreateSupplyMaterial("CH-11 cargo status panel dim scan line", new Color(0.02f, 0.09f, 0.08f, 1f), 0f, 0.66f) },
                { "label_text", CreateSupplyMaterial("CH-01 pale cargo hold floor label text", new Color(0.82f, 0.91f, 0.84f, 1f), 0f, 0.66f) },
                { "marker_backing", CreateSupplyMaterial("CH-10 dark direction label backing", new Color(0.030f, 0.036f, 0.038f, 1f), 0f, 0.78f) },
                { "marker_arrow", CreateSupplyMaterial("CH-10 worn pale direction arrow", new Color(0.84f, 0.89f, 0.82f, 1f), 0f, 0.68f) },
                { "marker_wear", CreateSupplyMaterial("CH-10 scraped direction marker trim", new Color(0.70f, 0.62f, 0.46f, 1f), 0.02f, 0.90f) },
                { "cockpit_marker", CreateSupplyMaterial("CH-05 cockpit direction blue marker", new Color(0.13f, 0.30f, 0.58f, 1f), 0.02f, 0.70f) },
                { "control_marker", CreateSupplyMaterial("CH-07 control direction cyan marker", new Color(0.08f, 0.42f, 0.52f, 1f), 0.02f, 0.70f) },
                { "engine_marker", CreateSupplyMaterial("CH-06 engine direction amber marker", new Color(0.70f, 0.43f, 0.12f, 1f), 0.02f, 0.74f) },
                { "armory_marker", CreateSupplyMaterial("CH-08 armory direction red marker", new Color(0.58f, 0.16f, 0.13f, 1f), 0.02f, 0.76f) },
                { "supply_marker", CreateSupplyMaterial("CH-09 supply direction green marker", new Color(0.18f, 0.43f, 0.28f, 1f), 0.02f, 0.74f) },
                { "hazard", CreateSupplyMaterial("CH-11 muted cargo status hazard paint", new Color(0.82f, 0.58f, 0.14f, 1f), 0.02f, 0.82f) },
                { "shadow", CreateSupplyMaterial("CH-01 recessed cargo hold shadow", new Color(0.018f, 0.020f, 0.019f, 1f), 0f, 0.92f) },
            };
        }

        private static void CreateCargoWallYWithDoors(
            Transform root,
            string name,
            float y,
            IReadOnlyList<float> doorCenters,
            IReadOnlyList<Material> doorMarkers,
            Dictionary<string, Material> mats)
        {
            const float wallWidth = 9.8f;
            const float wallThickness = 0.34f;
            const float wallHeight = 3.2f;
            const float doorWidth = 1.55f;
            const float doorHeight = 2.12f;

            var sortedCenters = new List<float>(doorCenters);
            sortedCenters.Sort();
            var cursor = -wallWidth * 0.5f;
            for (var i = 0; i < sortedCenters.Count; i++)
            {
                var left = sortedCenters[i] - (doorWidth * 0.5f);
                if (left > cursor + 0.05f)
                {
                    var length = left - cursor;
                CreateSupplyBox(name + " sealed wall segment " + (i + 1).ToString(CultureInfo.InvariantCulture), root, new Vector3(cursor + (length * 0.5f), y, wallHeight * 0.5f), new Vector3(length, wallThickness, wallHeight), mats["wall"]);
                }

                var marker = doorMarkers[Mathf.Min(i, doorMarkers.Count - 1)];
                CreateSupplyBox(name + " doorway header " + (i + 1).ToString(CultureInfo.InvariantCulture), root, new Vector3(sortedCenters[i], y, doorHeight + ((wallHeight - doorHeight) * 0.5f)), new Vector3(doorWidth + 0.42f, wallThickness, wallHeight - doorHeight), mats["wall"]);
                CreateSupplyBox(name + " doorway left frame " + (i + 1).ToString(CultureInfo.InvariantCulture), root, new Vector3(sortedCenters[i] - (doorWidth * 0.5f), y, doorHeight * 0.5f), new Vector3(0.16f, wallThickness + 0.12f, doorHeight), mats["door_frame"]);
                CreateSupplyBox(name + " doorway right frame " + (i + 1).ToString(CultureInfo.InvariantCulture), root, new Vector3(sortedCenters[i] + (doorWidth * 0.5f), y, doorHeight * 0.5f), new Vector3(0.16f, wallThickness + 0.12f, doorHeight), mats["door_frame"]);
                CreateSupplyBox(name + " doorway colored threshold " + (i + 1).ToString(CultureInfo.InvariantCulture), root, new Vector3(sortedCenters[i], y, 0.22f), new Vector3(doorWidth + 0.28f, 0.62f, 0.055f), marker);
                cursor = sortedCenters[i] + (doorWidth * 0.5f);
            }

            var rightEdge = wallWidth * 0.5f;
            if (rightEdge > cursor + 0.05f)
            {
                var length = rightEdge - cursor;
                CreateSupplyBox(name + " sealed wall segment end", root, new Vector3(cursor + (length * 0.5f), y, wallHeight * 0.5f), new Vector3(length, wallThickness, wallHeight), mats["wall"]);
            }
        }

        private static void CreateCargoWallXWithDoor(
            Transform root,
            string name,
            float x,
            float doorCenterY,
            Material marker,
            Dictionary<string, Material> mats)
        {
            const float wallDepth = 8.7f;
            const float wallThickness = 0.34f;
            const float wallHeight = 3.2f;
            const float doorWidth = 1.55f;
            const float doorHeight = 2.12f;
            var lowerEdge = -wallDepth * 0.5f;
            var upperEdge = wallDepth * 0.5f;
            var doorLower = doorCenterY - (doorWidth * 0.5f);
            var doorUpper = doorCenterY + (doorWidth * 0.5f);

            if (doorLower > lowerEdge + 0.05f)
            {
                var length = doorLower - lowerEdge;
                CreateSupplyBox(name + " lower sealed wall segment", root, new Vector3(x, lowerEdge + (length * 0.5f), wallHeight * 0.5f), new Vector3(wallThickness, length, wallHeight), mats["wall"]);
            }

            CreateSupplyBox(name + " doorway header", root, new Vector3(x, doorCenterY, doorHeight + ((wallHeight - doorHeight) * 0.5f)), new Vector3(wallThickness, doorWidth + 0.42f, wallHeight - doorHeight), mats["wall"]);
            CreateSupplyBox(name + " doorway lower frame", root, new Vector3(x, doorLower, doorHeight * 0.5f), new Vector3(wallThickness + 0.12f, 0.16f, doorHeight), mats["door_frame"]);
            CreateSupplyBox(name + " doorway upper frame", root, new Vector3(x, doorUpper, doorHeight * 0.5f), new Vector3(wallThickness + 0.12f, 0.16f, doorHeight), mats["door_frame"]);
            CreateSupplyBox(name + " doorway colored threshold", root, new Vector3(x, doorCenterY, 0.22f), new Vector3(0.62f, doorWidth + 0.28f, 0.055f), marker);

            if (upperEdge > doorUpper + 0.05f)
            {
                var length = upperEdge - doorUpper;
                CreateSupplyBox(name + " upper sealed wall segment", root, new Vector3(x, doorUpper + (length * 0.5f), wallHeight * 0.5f), new Vector3(wallThickness, length, wallHeight), mats["wall"]);
            }
        }

        private static void CreateCargoCorridorStub(
            Transform root,
            string name,
            Vector3 blenderCenter,
            Vector3 blenderScale,
            bool corridorRunsAlongX,
            Material marker,
            Dictionary<string, Material> mats)
        {
            CreateSupplyBox(name + " floor continuation", root, blenderCenter, blenderScale, mats["corridor_floor"]);
            CreateSupplyBox(name + " colored threshold slab", root, new Vector3(blenderCenter.x, blenderCenter.y, 0.215f), new Vector3(Mathf.Min(blenderScale.x + 0.18f, 2.30f), Mathf.Min(blenderScale.y + 0.18f, 2.30f), 0.055f), marker);

            if (corridorRunsAlongX)
            {
                CreateSupplyBox(name + " upper side wall", root, new Vector3(blenderCenter.x, blenderCenter.y + (blenderScale.y * 0.5f), 1.08f), new Vector3(blenderScale.x, 0.20f, 2.16f), mats["wall_dark"]);
                CreateSupplyBox(name + " lower side wall", root, new Vector3(blenderCenter.x, blenderCenter.y - (blenderScale.y * 0.5f), 1.08f), new Vector3(blenderScale.x, 0.20f, 2.16f), mats["wall_dark"]);
            }
            else
            {
                CreateSupplyBox(name + " left side wall", root, new Vector3(blenderCenter.x - (blenderScale.x * 0.5f), blenderCenter.y, 1.08f), new Vector3(0.20f, blenderScale.y, 2.16f), mats["wall_dark"]);
                CreateSupplyBox(name + " right side wall", root, new Vector3(blenderCenter.x + (blenderScale.x * 0.5f), blenderCenter.y, 1.08f), new Vector3(0.20f, blenderScale.y, 2.16f), mats["wall_dark"]);
            }
        }

        private static void CreateCargoFloorGrid(Transform root, Dictionary<string, Material> mats)
        {
            var floorPlateXs = new[] { -3.85f, -2.55f, -1.25f, 1.25f, 2.55f, 3.85f };
            for (var i = 0; i < floorPlateXs.Length; i++)
            {
                var x = floorPlateXs[i];
                CreateSupplyBox("CH-01 removable cargo floor plate " + x.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture), root, new Vector3(x, 0f, 0.115f), new Vector3(1.05f, 7.8f, 0.042f), mats["floor_panel"]);
            }

            var ribYs = new[] { -3.55f, -2.40f, -1.25f, 0f, 1.25f, 2.40f, 3.55f };
            for (var i = 0; i < ribYs.Length; i++)
            {
                var y = ribYs[i];
                CreateSupplyBox("CH-01 transverse cargo deck rib " + y.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture), root, new Vector3(0f, y, 0.152f), new Vector3(8.98f, 0.046f, 0.048f), mats["deck_rib"]);
            }
        }

        private static void CreateCargoEdgeWalkway(Transform root, Dictionary<string, Material> mats)
        {
            CreateSupplyBox("CH-04 forward raised handling walkway band", root, new Vector3(0f, 2.62f, 0.235f), new Vector3(6.70f, 0.58f, 0.075f), mats["walkway"]);
            CreateSupplyBox("CH-04 aft raised handling walkway band", root, new Vector3(0f, -2.62f, 0.235f), new Vector3(6.70f, 0.58f, 0.075f), mats["walkway"]);
            CreateSupplyBox("CH-04 west raised handling walkway band", root, new Vector3(-3.20f, 0f, 0.235f), new Vector3(0.58f, 4.70f, 0.075f), mats["walkway"]);
            CreateSupplyBox("CH-04 east raised handling walkway band", root, new Vector3(3.20f, 0f, 0.235f), new Vector3(0.58f, 4.70f, 0.075f), mats["walkway"]);
            CreateSupplyBox("CH-02 central cargo zone outline", root, new Vector3(0f, 0f, 0.212f), new Vector3(4.85f, 3.20f, 0.055f), mats["cargo_zone"]);
        }

        private static void CreateCargoContainer(Transform root, Dictionary<string, Material> mats)
        {
            CreateSupplyBox("CH-03 single central cargo container body", root, new Vector3(0f, 0f, 0.92f), new Vector3(3.75f, 2.12f, 1.55f), mats["container_body"]);
            CreateSupplyBox("CH-03 single central cargo container top cap", root, new Vector3(0f, 0f, 1.735f), new Vector3(3.95f, 2.30f, 0.12f), mats["container_edge"]);
            CreateSupplyBox("CH-03 single central cargo container bottom skid", root, new Vector3(0f, 0f, 0.145f), new Vector3(4.05f, 2.34f, 0.16f), mats["container_edge"]);
            CreateSupplyBox("CH-03 single central cargo container left side panel", root, new Vector3(0f, 1.095f, 0.92f), new Vector3(3.56f, 0.065f, 1.34f), mats["container_panel"]);
            CreateSupplyBox("CH-03 single central cargo container right side panel", root, new Vector3(0f, -1.095f, 0.92f), new Vector3(3.56f, 0.065f, 1.34f), mats["container_panel"]);

            var cornerXs = new[] { -1.95f, 1.95f };
            var cornerYs = new[] { -1.16f, 1.16f };
            for (var xIndex = 0; xIndex < cornerXs.Length; xIndex++)
            {
                for (var yIndex = 0; yIndex < cornerYs.Length; yIndex++)
                {
                    CreateSupplyBox(
                        "CH-03 single central cargo container reinforced corner " + (xIndex + 1).ToString(CultureInfo.InvariantCulture) + "-" + (yIndex + 1).ToString(CultureInfo.InvariantCulture),
                        root,
                        new Vector3(cornerXs[xIndex], cornerYs[yIndex], 0.94f),
                        new Vector3(0.13f, 0.13f, 1.62f),
                        mats["container_edge"]);
                }
            }

            var ribXs = new[] { -1.40f, -0.90f, -0.40f, 0.10f, 0.60f, 1.10f, 1.60f };
            for (var i = 0; i < ribXs.Length; i++)
            {
                CreateSupplyBox("CH-03 single central cargo container left corrugation rib " + (i + 1).ToString(CultureInfo.InvariantCulture), root, new Vector3(ribXs[i], 1.137f, 0.96f), new Vector3(0.055f, 0.035f, 1.22f), mats["container_edge"]);
                CreateSupplyBox("CH-03 single central cargo container right corrugation rib " + (i + 1).ToString(CultureInfo.InvariantCulture), root, new Vector3(ribXs[i], -1.137f, 0.96f), new Vector3(0.055f, 0.035f, 1.22f), mats["container_edge"]);
            }

            CreateSupplyBox("CH-03 single central cargo container left end double door seam", root, new Vector3(-1.91f, 0f, 0.96f), new Vector3(0.045f, 1.86f, 1.24f), mats["container_edge"]);
            CreateSupplyBox("CH-03 single central cargo container right end double door seam", root, new Vector3(1.91f, 0f, 0.96f), new Vector3(0.045f, 1.86f, 1.24f), mats["container_edge"]);
            CreateSupplyText("CH-03 single central cargo container floor label", root, "SINGLE CONTAINER", new Vector3(0f, 0f, 1.84f), Quaternion.Euler(90f, 0f, 0f), 0.14f, mats["label_text"]);
        }

        private static void CreateCargoStatusPanel(Transform root, Dictionary<string, Material> mats)
        {
            CreateSupplyBox("CH-11 cargo status panel wall body", root, new Vector3(4.68f, -2.25f, 1.62f), new Vector3(0.16f, 1.42f, 0.96f), mats["screen_frame"]);
            CreateSupplyBox("CH-11 cargo status panel green display", root, new Vector3(4.592f, -2.25f, 1.68f), new Vector3(0.045f, 1.08f, 0.66f), mats["screen_glow"]);
            CreateSupplyBox("CH-11 cargo status panel top scan line", root, new Vector3(4.560f, -2.25f, 1.90f), new Vector3(0.024f, 0.92f, 0.035f), mats["screen_dim"]);
            CreateSupplyBox("CH-11 cargo status panel middle scan line", root, new Vector3(4.560f, -2.25f, 1.68f), new Vector3(0.024f, 0.78f, 0.035f), mats["screen_dim"]);
            CreateSupplyBox("CH-11 cargo status panel bottom scan line", root, new Vector3(4.560f, -2.25f, 1.46f), new Vector3(0.024f, 0.54f, 0.035f), mats["screen_dim"]);
            CreateSupplyBox("CH-11 cargo status panel amber status lamp", root, new Vector3(4.552f, -1.61f, 1.20f), new Vector3(0.035f, 0.16f, 0.12f), mats["hazard"]);
        }

        private static void CreateCargoHoldCh11DisplayOnly(Transform root)
        {
            var material = CreateCargoHoldCh11DisplayMaterial();
            var screenObject = new GameObject("CH-11 B2_Eq2_E bottom right display surface");
            screenObject.transform.SetParent(root, false);
            screenObject.transform.localPosition = BlenderToUnity(new Vector3(4.564f, -2.25f, 1.68f));
            screenObject.transform.localRotation = Quaternion.identity;
            screenObject.transform.localScale = Vector3.one;

            var mesh = CreateCargoHoldCh11DisplayMesh(1.08f, 0.66f);
            screenObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            screenObject.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Mesh CreateCargoHoldCh11DisplayMesh(float width, float height)
        {
            var halfWidth = width * 0.5f;
            var halfHeight = height * 0.5f;
            var mesh = new Mesh
            {
                name = "CH-11 B2_Eq2_E bottom right display mesh",
                vertices = new[]
                {
                    new Vector3(0f, -halfHeight, -halfWidth),
                    new Vector3(0f, halfHeight, -halfWidth),
                    new Vector3(0f, halfHeight, halfWidth),
                    new Vector3(0f, -halfHeight, halfWidth),
                },
                triangles = new[] { 0, 2, 1, 0, 3, 2 },
                uv = new[]
                {
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 0f),
                }
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material CreateCargoHoldCh11DisplayMaterial()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(CargoHoldCh11DisplayTexturePath);
            if (texture == null)
            {
                throw new InvalidOperationException("Missing CH-11 cargo hold display texture: " + CargoHoldCh11DisplayTexturePath);
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Texture");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                throw new InvalidOperationException("No shader was found for CH-11 cargo hold display material.");
            }

            var material = new Material(shader)
            {
                name = "CH-11 B2_Eq2_E bottom right display material",
                mainTexture = texture,
                color = Color.white
            };

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 0f);
            }

            return material;
        }

        private static void CreateCargoWallDressing(Transform root, Dictionary<string, Material> mats)
        {
            var northXs = new[] { -4f, -2f, 0f, 2f, 4f };
            for (var i = 0; i < northXs.Length; i++)
            {
                CreateSupplyBox("CH-01 north cargo wall vertical service rib " + (i + 1).ToString(CultureInfo.InvariantCulture), root, new Vector3(northXs[i], 4.19f, 1.64f), new Vector3(0.10f, 0.14f, 2.50f), mats["beam"]);
            }

            var southXs = new[] { -2f, 0f, 2f };
            for (var i = 0; i < southXs.Length; i++)
            {
                CreateSupplyBox("CH-01 aft cargo wall vertical service rib " + (i + 1).ToString(CultureInfo.InvariantCulture), root, new Vector3(southXs[i], -4.19f, 1.64f), new Vector3(0.10f, 0.14f, 2.50f), mats["beam"]);
            }

            CreateSupplyBox("CH-01 west cargo wall recessed conduit", root, new Vector3(-4.72f, -2.80f, 1.70f), new Vector3(0.08f, 1.85f, 0.10f), mats["shadow"]);
            CreateSupplyBox("CH-01 east cargo wall recessed conduit", root, new Vector3(4.72f, 2.35f, 1.70f), new Vector3(0.08f, 1.85f, 0.10f), mats["shadow"]);
        }

        private static void CreateCargoHoldCh10DirectionMarkersOnly(Transform root, Dictionary<string, Material> mats)
        {
            var markerScale = new Vector3(1.97f, 0.58f, 0.050f);
            CreateCargoHoldCh10DirectionMarker(root, "CH-10 cockpit 12 oclock direction marker", "COCKPIT", new Vector3(0f, 3.89f, 0.225f), markerScale, 0f, 0f, 0f, mats["cockpit_marker"], mats);
            CreateCargoHoldCh10DirectionMarker(root, "CH-10 control 3 oclock direction marker", "ENGINE", new Vector3(4.44f, 0f, 0.225f), markerScale, 90f, -90f, 90f, mats["engine_marker"], mats);
            CreateCargoHoldCh10DirectionMarker(root, "CH-10 engine 9 oclock direction marker", "CONTROL", new Vector3(-4.44f, 0f, 0.225f), markerScale, 90f, 90f, 90f, mats["control_marker"], mats);
            CreateCargoHoldCh10DirectionMarker(root, "CH-10 armory 5 oclock direction marker", "SUPPLY", new Vector3(3.58f, -3.89f, 0.225f), markerScale, 0f, 180f, 0f, mats["supply_marker"], mats);
            CreateCargoHoldCh10DirectionMarker(root, "CH-10 supply 7 oclock direction marker", "ARMORY", new Vector3(-3.58f, -3.89f, 0.225f), markerScale, 0f, 180f, 0f, mats["armory_marker"], mats);
        }

        private static void CreateCargoHoldCh10DirectionMarker(
            Transform root,
            string name,
            string label,
            Vector3 blenderPosition,
            Vector3 blenderScale,
            float plateRotZ,
            float arrowRotZ,
            float textRotZ,
            Material marker,
            Dictionary<string, Material> mats)
        {
            var group = new GameObject(name);
            group.transform.SetParent(root, false);
            group.transform.localPosition = Vector3.zero;
            group.transform.localRotation = Quaternion.identity;
            group.transform.localScale = Vector3.one;

            var labelBackWidth = Mathf.Min(blenderScale.x * 0.72f, 1.34f);
            var trimForward = (blenderScale.y * 0.5f) - 0.035f;
            CreateCargoHoldCh10Box(name + " colored direction plate", group.transform, blenderPosition, blenderScale, marker, plateRotZ);
            CreateCargoHoldCh10Box(name + " dark recessed label backing", group.transform, OffsetCargoHoldCh10Marker(blenderPosition, 0f, -0.13f, plateRotZ, 0.042f), new Vector3(labelBackWidth, 0.18f, 0.034f), mats["marker_backing"], plateRotZ);
            CreateCargoHoldCh10Box(name + " pale arrow stem", group.transform, OffsetCargoHoldCh10Marker(blenderPosition, 0f, 0.13f, arrowRotZ, 0.056f), new Vector3(0.085f, 0.32f, 0.032f), mats["marker_arrow"], arrowRotZ);
            CreateCargoHoldCh10Box(name + " pale arrow head left wing", group.transform, OffsetCargoHoldCh10Marker(blenderPosition, -0.065f, 0.330f, arrowRotZ, 0.058f), new Vector3(0.23f, 0.060f, 0.034f), mats["marker_arrow"], arrowRotZ - 34f);
            CreateCargoHoldCh10Box(name + " pale arrow head right wing", group.transform, OffsetCargoHoldCh10Marker(blenderPosition, 0.065f, 0.330f, arrowRotZ, 0.058f), new Vector3(0.23f, 0.060f, 0.034f), mats["marker_arrow"], arrowRotZ + 34f);
            CreateCargoHoldCh10Box(name + " worn front trim", group.transform, OffsetCargoHoldCh10Marker(blenderPosition, 0f, trimForward, plateRotZ, 0.070f), new Vector3(blenderScale.x * 0.92f, 0.035f, 0.026f), mats["marker_wear"], plateRotZ);
            CreateCargoHoldCh10Box(name + " worn rear trim", group.transform, OffsetCargoHoldCh10Marker(blenderPosition, 0f, -trimForward, plateRotZ, 0.070f), new Vector3(blenderScale.x * 0.92f, 0.035f, 0.026f), mats["marker_wear"], plateRotZ);
            CreateCargoHoldCh10ReadableLabel(
                name + " readable floor label",
                group.transform,
                label,
                OffsetCargoHoldCh10Marker(blenderPosition, 0f, -0.13f, textRotZ, 0.088f),
                textRotZ,
                labelBackWidth);
        }

        private static void CreateCargoHoldCh10ReadableLabel(
            string name,
            Transform parent,
            string label,
            Vector3 blenderPosition,
            float textRotZ,
            float labelBackWidth)
        {
            var fitWidth = Mathf.Min(labelBackWidth * 0.78f, 1.04f);
            var characterSize = label.Length > 6 ? 0.095f : 0.105f;
            const float objectScale = 0.32f;
            CreateSupplyReadableTextGroup(
                name,
                parent,
                label,
                BlenderToUnity(blenderPosition),
                Quaternion.Euler(90f, textRotZ, 0f),
                characterSize,
                fitWidth,
                new Color(0.82f, 0.91f, 0.84f, 1f),
                objectScale);
        }

        private static Vector3 OffsetCargoHoldCh10Marker(Vector3 basePosition, float right, float forward, float rotZDegrees, float zDelta)
        {
            var radians = rotZDegrees * Mathf.Deg2Rad;
            var cos = Mathf.Cos(radians);
            var sin = Mathf.Sin(radians);
            return new Vector3(
                basePosition.x + (right * cos) - (forward * sin),
                basePosition.y + (right * sin) + (forward * cos),
                basePosition.z + zDelta);
        }

        private static void CreateCargoHoldCh10Box(
            string name,
            Transform parent,
            Vector3 blenderPosition,
            Vector3 blenderScale,
            Material material,
            float rotZDegrees)
        {
            CreateSupplyBoxLocal(
                name,
                parent,
                BlenderToUnity(blenderPosition),
                BlenderBoxScaleToUnity(blenderScale),
                material,
                Quaternion.Euler(0f, rotZDegrees, 0f));
        }

        private static Dictionary<string, Material> CreateSupplyRoomMaterials()
        {
            return new Dictionary<string, Material>(StringComparer.Ordinal)
            {
                { "floor", CreateSupplyMaterial("SR-01 worn supply room deck", new Color(0.15f, 0.17f, 0.17f, 1f), 0.22f, 0.84f) },
                { "floor_panel", CreateSupplyMaterial("SR-01 removable dark supply floor panel", new Color(0.18f, 0.19f, 0.18f, 1f), 0.22f, 0.84f) },
                { "deck_rib", CreateSupplyMaterial("SR-01 raised supply deck rib", new Color(0.08f, 0.09f, 0.09f, 1f), 0.22f, 0.84f) },
                { "wall", CreateSupplyMaterial("SR-01 thick supply room armored wall", new Color(0.20f, 0.23f, 0.23f, 1f), 0.18f, 0.82f) },
                { "wall_dark", CreateSupplyMaterial("SR-01 dark corridor wall", new Color(0.10f, 0.12f, 0.13f, 1f), 0.18f, 0.84f) },
                { "door_frame", CreateSupplyMaterial("SR-01 heavy doorway and equipment frame", new Color(0.34f, 0.34f, 0.30f, 1f), 0.22f, 0.78f) },
                { "beam", CreateSupplyMaterial("SR-01 empty wall structural rib", new Color(0.28f, 0.30f, 0.28f, 1f), 0.18f, 0.82f) },
                { "corridor_floor", CreateSupplyMaterial("SR-01 corridor continuation steel", new Color(0.15f, 0.18f, 0.18f, 1f), 0.18f, 0.84f) },
                { "storage_marker", CreateSupplyMaterial("SR-02 supply wall floor cyan marker", new Color(0.10f, 0.45f, 0.50f, 1f), 0.05f, 0.72f) },
                { "storage_cavity", CreateSupplyMaterial("SR-03 dark empty storage cavity", new Color(0.012f, 0.014f, 0.014f, 1f), 0f, 0.92f) },
                { "locker_body", CreateSupplyMaterial("SR-02 reference pale green locker body", new Color(0.48f, 0.51f, 0.42f, 1f), 0.20f, 0.84f) },
                { "locker_door", CreateSupplyMaterial("SR-03 reference pale green locker flat door", new Color(0.60f, 0.63f, 0.52f, 1f), 0.18f, 0.82f) },
                { "locker_frame", CreateSupplyMaterial("SR-03 reference muted metal locker frame", new Color(0.39f, 0.42f, 0.34f, 1f), 0.24f, 0.78f) },
                { "locker_shadow", CreateSupplyMaterial("SR-03 dark inset locker handle and perforation shadow", new Color(0.030f, 0.034f, 0.032f, 1f), 0f, 0.88f) },
                { "ejection_back", CreateSupplyMaterial("SR-05 ejection bay wall dark backplate", new Color(0.16f, 0.13f, 0.12f, 1f), 0.18f, 0.84f) },
                { "ejection_door", CreateSupplyMaterial("SR-06 closed ejection bay door", new Color(0.22f, 0.21f, 0.19f, 1f), 0.20f, 0.82f) },
                { "terminal_frame", CreateSupplyMaterial("SR-07 ejection terminal frame", new Color(0.18f, 0.18f, 0.17f, 1f), 0.22f, 0.80f) },
                { "terminal_screen", CreateSupplyMaterial("SR-07 inactive ejection terminal screen", new Color(0.09f, 0.018f, 0.014f, 1f), 0f, 0.44f) },
                { "hazard", CreateSupplyMaterial("SR-05 muted ejection hazard amber", new Color(0.86f, 0.50f, 0.12f, 1f), 0.02f, 0.82f) },
                { "hazard_dark", CreateSupplyMaterial("SR-08 black warning paint", new Color(0.018f, 0.018f, 0.015f, 1f), 0f, 0.86f) },
                { "ejection_zone_plate", CreateSupplyMaterial("SR-08 scuffed ejection hazard floor steel plate", new Color(0.19f, 0.19f, 0.17f, 1f), 0.22f, 0.84f) },
                { "ejection_zone_seam", CreateSupplyMaterial("SR-08 dark recessed ejection plate seam", new Color(0.045f, 0.047f, 0.044f, 1f), 0.18f, 0.88f) },
                { "ejection_zone_bolt", CreateSupplyMaterial("SR-08 recessed ejection floor bolt heads", new Color(0.08f, 0.08f, 0.075f, 1f), 0.22f, 0.84f) },
                { "label_text", CreateSupplyMaterial("SR-01 pale floor and wall label text", new Color(0.78f, 0.88f, 0.84f, 1f), 0f, 0.70f) },
                { "direction_text", CreateSupplyMaterial("SR-11 bright corridor direction label text", new Color(0.92f, 0.88f, 0.66f, 1f), 0f, 0.64f) },
                { "cctv_mount", CreateSupplyMaterial("SR-12 CCTV dark corner mounting bracket", new Color(0.13f, 0.14f, 0.14f, 1f), 0.22f, 0.84f) },
                { "cctv_body", CreateSupplyMaterial("SR-12 CCTV off-white compact camera housing", new Color(0.62f, 0.64f, 0.58f, 1f), 0.18f, 0.82f) },
                { "cctv_lens", CreateSupplyMaterial("SR-12 CCTV black recessed lens barrel", new Color(0.010f, 0.012f, 0.014f, 1f), 0f, 0.38f) },
                { "cctv_glass", CreateSupplyMaterial("SR-12 CCTV faint blue glass lens", new Color(0.05f, 0.16f, 0.20f, 1f), 0f, 0.25f) },
                { "cctv_cable", CreateSupplyMaterial("SR-12 CCTV black wall cable", new Color(0.018f, 0.018f, 0.016f, 1f), 0f, 0.82f) },
                { "cctv_view", CreateSupplyMaterial("SR-12 CCTV translucent viewing direction ray", new Color(0.08f, 0.40f, 0.42f, 1f), 0f, 0.62f) },
                { "armory_marker", CreateSupplyMaterial("SR-09 armory direction blue marker", new Color(0.13f, 0.28f, 0.58f, 1f), 0.02f, 0.70f) },
                { "cargo_marker", CreateSupplyMaterial("SR-10 cargo hold direction green marker", new Color(0.18f, 0.42f, 0.30f, 1f), 0.02f, 0.72f) },
                { "ejection_marker", CreateSupplyMaterial("SR-05 ejection wall floor orange marker", new Color(0.66f, 0.25f, 0.08f, 1f), 0.02f, 0.76f) },
                { "empty_marker", CreateSupplyMaterial("SR-01 empty wall floor gray marker", new Color(0.32f, 0.34f, 0.32f, 1f), 0.02f, 0.80f) },
            };
        }

        private static Material CreateSupplyMaterial(string name, Color color, float metallic, float roughness)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader)
            {
                name = name,
                color = color
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            var smoothness = Mathf.Clamp01(1f - roughness);
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            return material;
        }

        private static void CreateSupplyPlainWallY(Transform root, string name, float y, Dictionary<string, Material> mats)
        {
            CreateSupplyBox(name, root, new Vector3(0f, y, 1.5f), new Vector3(7.52f, 0.32f, 3.0f), mats["wall"]);
        }

        private static void CreateSupplyPlainWallX(Transform root, string name, float x, Dictionary<string, Material> mats)
        {
            CreateSupplyBox(name, root, new Vector3(x, 0f, 1.5f), new Vector3(0.32f, 6.12f, 3.0f), mats["wall"]);
        }

        private static void CreateSupplyEastSharedCorridorWall(Transform root, Dictionary<string, Material> mats)
        {
            CreateSupplyBox("SR-09 SR-10 east shared corridor wall lower sealed segment", root, new Vector3(3.6f, -2.345f, 1.5f), new Vector3(0.32f, 1.11f, 3.0f), mats["wall"]);
            CreateSupplyBox("SR-09 SR-10 east shared corridor wall center sealed segment", root, new Vector3(3.6f, -0.05f, 1.5f), new Vector3(0.32f, 1.04f, 3.0f), mats["wall"]);
            CreateSupplyBox("SR-09 SR-10 east shared corridor wall upper sealed segment", root, new Vector3(3.6f, 2.295f, 1.5f), new Vector3(0.32f, 1.21f, 3.0f), mats["wall"]);
            CreateSupplyDoorFrame(root, "SR-09 armory", 1.08f, mats["armory_marker"], mats);
            CreateSupplyDoorFrame(root, "SR-10 cargo hold", -1.18f, mats["cargo_marker"], mats);
        }

        private static void CreateSupplyDoorFrame(Transform root, string label, float centerY, Material marker, Dictionary<string, Material> mats)
        {
            CreateSupplyBox(label + " doorway upper header", root, new Vector3(3.6f, centerY, 2.525f), new Vector3(0.32f, 1.22f, 0.95f), mats["wall"]);
            CreateSupplyBox(label + " doorway lower frame", root, new Vector3(3.6f, centerY - 0.61f, 1.025f), new Vector3(0.44f, 0.16f, 2.05f), mats["door_frame"]);
            CreateSupplyBox(label + " doorway upper frame", root, new Vector3(3.6f, centerY + 0.61f, 1.025f), new Vector3(0.44f, 0.16f, 2.05f), mats["door_frame"]);
            CreateSupplyBox(label + " doorway color band", root, new Vector3(3.612f, centerY, 1.42f), new Vector3(0.06f, 1.44f, 0.42f), marker);
        }

        private static void CreateSupplyFloorGrid(Transform root, Dictionary<string, Material> mats)
        {
            var floorPlateXs = new[] { -2.6f, -1.3f, 0f, 1.3f, 2.6f };
            for (var i = 0; i < floorPlateXs.Length; i++)
            {
                var x = floorPlateXs[i];
                CreateSupplyBox("SR-01 removable supply room floor plate " + x.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture), root, new Vector3(x, 0f, 0.112f), new Vector3(1.05f, 5.26f, 0.04f), mats["floor_panel"]);
            }

            var ribYs = new[] { -2.2f, -1.1f, 0f, 1.1f, 2.2f };
            for (var i = 0; i < ribYs.Length; i++)
            {
                var y = ribYs[i];
                CreateSupplyBox("SR-01 transverse deck rib " + y.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture), root, new Vector3(0f, y, 0.145f), new Vector3(6.62f, 0.04f, 0.044f), mats["deck_rib"]);
            }
        }

        private static void CreateSupplyStorageWall(Transform root, Dictionary<string, Material> mats)
        {
            const float wallY = 2.6632f;
            const float lockerY = 2.2132f;
            const float lockerFrontY = 1.8012f;
            const float frontY = 1.7292f;

            CreateSupplyBox("SR-02 supply storage wall placement marker only", root, new Vector3(0f, wallY - 0.03f, 1.46f), new Vector3(5.55f, 0.045f, 2.38f), mats["storage_marker"]);
            CreateSupplyBox("SR-02 visible gap behind freestanding locker", root, new Vector3(0f, wallY - 0.235f, 0.205f), new Vector3(5.50f, 0.18f, 0.045f), mats["storage_cavity"]);
            CreateSupplyBox("SR-02 freestanding locker base plinth", root, new Vector3(0f, lockerY, 0.245f), new Vector3(5.34f, 0.70f, 0.20f), mats["locker_frame"]);
            CreateSupplyBox("SR-02 freestanding supply locker main body", root, new Vector3(0f, lockerY, 1.32f), new Vector3(5.18f, 0.62f, 2.10f), mats["locker_body"]);
            CreateSupplyBox("SR-02 freestanding locker top cap", root, new Vector3(0f, lockerY, 2.42f), new Vector3(5.32f, 0.72f, 0.18f), mats["locker_frame"]);
            CreateSupplyBox("SR-02 freestanding locker left side panel", root, new Vector3(-2.66f, lockerY, 1.32f), new Vector3(0.16f, 0.72f, 2.12f), mats["locker_frame"]);
            CreateSupplyBox("SR-02 freestanding locker right side panel", root, new Vector3(2.66f, lockerY, 1.32f), new Vector3(0.16f, 0.72f, 2.12f), mats["locker_frame"]);

            CreateSupplyBox("SR-03 reference style outer top frame", root, new Vector3(0f, frontY, 2.31f), new Vector3(4.92f, 0.075f, 0.12f), mats["locker_frame"]);
            CreateSupplyBox("SR-03 reference style outer bottom frame", root, new Vector3(0f, frontY, 0.41f), new Vector3(4.92f, 0.075f, 0.12f), mats["locker_frame"]);
            CreateSupplyBox("SR-03 reference style outer left frame", root, new Vector3(-2.46f, frontY, 1.36f), new Vector3(0.11f, 0.075f, 1.90f), mats["locker_frame"]);
            CreateSupplyBox("SR-03 reference style outer right frame", root, new Vector3(2.46f, frontY, 1.36f), new Vector3(0.11f, 0.075f, 1.90f), mats["locker_frame"]);

            CreateSupplyLockerDoor(root, "left", -1.18f, -0.42f, -0.23f, mats);
            CreateSupplyLockerDoor(root, "right", 1.18f, 0.42f, 0.23f, mats);
            CreateSupplyBox("SR-03 closed double door central seam", root, new Vector3(0f, frontY - 0.124f, 1.36f), new Vector3(0.030f, 0.028f, 1.83f), mats["locker_shadow"]);

            CreateSupplyLockerHinges(root, "left", -2.58f, mats);
            CreateSupplyLockerHinges(root, "right", 2.58f, mats);
            CreateSupplyCornerPerforations(root, "upper left corner", -2.10f, 2.12f, 1, -1, mats);
            CreateSupplyCornerPerforations(root, "upper right corner", 2.10f, 2.12f, -1, -1, mats);
            CreateSupplyCornerPerforations(root, "lower left corner", -2.10f, 0.60f, 1, 1, mats);
            CreateSupplyCornerPerforations(root, "lower right corner", 2.10f, 0.60f, -1, 1, mats);
        }

        private static void CreateSupplyLockerDoor(Transform root, string side, float x, float nameplateOffset, float lockX, Dictionary<string, Material> mats)
        {
            const float frontY = 1.7292f;
            CreateSupplyBox("SR-03 closed " + side + " flat metal locker door", root, new Vector3(x, frontY - 0.030f, 1.36f), new Vector3(2.30f, 0.070f, 1.82f), mats["locker_door"]);
            CreateSupplyBox("SR-03 " + side + " inset door border top", root, new Vector3(x, frontY - 0.071f, 2.20f), new Vector3(2.04f, 0.024f, 0.030f), mats["locker_shadow"]);
            CreateSupplyBox("SR-03 " + side + " inset door border bottom", root, new Vector3(x, frontY - 0.071f, 0.52f), new Vector3(2.04f, 0.024f, 0.030f), mats["locker_shadow"]);
            CreateSupplyBox("SR-03 " + side + " inset door border outer", root, new Vector3(x + (side == "left" ? -1.04f : 1.04f), frontY - 0.071f, 1.36f), new Vector3(0.026f, 0.024f, 1.66f), mats["locker_shadow"]);
            CreateSupplyBox("SR-03 " + side + " upper horizontal name plate recess", root, new Vector3(x + nameplateOffset, frontY - 0.096f, 2.02f), new Vector3(0.52f, 0.032f, 0.15f), mats["locker_shadow"]);
            CreateSupplyBox("SR-03 " + side + " upper horizontal name plate metal rim", root, new Vector3(x + nameplateOffset, frontY - 0.116f, 2.02f), new Vector3(0.42f, 0.020f, 0.075f), mats["locker_frame"]);
            CreateSupplyBox("SR-03 " + side + " black recessed vertical pull pocket", root, new Vector3(lockX, frontY - 0.112f, 1.24f), new Vector3(0.22f, 0.040f, 0.48f), mats["locker_shadow"]);
            CreateSupplyBox("SR-03 " + side + " raised lock plate", root, new Vector3(lockX + (side == "left" ? 0.08f : -0.08f), frontY - 0.137f, 1.28f), new Vector3(0.105f, 0.026f, 0.34f), mats["locker_frame"]);
        }

        private static void CreateSupplyLockerHinges(Transform root, string side, float hingeX, Dictionary<string, Material> mats)
        {
            const float frontY = 1.7292f;
            CreateSupplyBox("SR-03 " + side + " exposed side hinge rail", root, new Vector3(hingeX, frontY - 0.020f, 1.36f), new Vector3(0.055f, 0.080f, 1.68f), mats["locker_frame"]);
            var hingeZs = new[] { 0.65f, 1.36f, 2.07f };
            for (var i = 0; i < hingeZs.Length; i++)
            {
                CreateSupplyBox("SR-03 " + side + " exposed hinge barrel " + (i + 1).ToString(CultureInfo.InvariantCulture), root, new Vector3(hingeX, frontY - 0.094f, hingeZs[i]), new Vector3(0.105f, 0.080f, 0.22f), mats["locker_frame"]);
            }
        }

        private static void CreateSupplyCornerPerforations(Transform root, string name, float originX, float originZ, int xDir, int zDir, Dictionary<string, Material> mats)
        {
            const float frontY = 1.7292f;
            for (var row = 0; row < 5; row++)
            {
                for (var column = 0; column <= row; column++)
                {
                    CreateSupplyBox(
                        "SR-03 " + name + " triangular perforation " + (row + 1).ToString(CultureInfo.InvariantCulture) + "-" + (column + 1).ToString(CultureInfo.InvariantCulture),
                        root,
                        new Vector3(originX + (xDir * column * 0.075f), frontY - 0.132f, originZ + (zDir * row * 0.055f)),
                        new Vector3(0.028f, 0.018f, 0.028f),
                        mats["locker_shadow"]);
                }
            }
        }

        private static void CreateSupplyEjectionWall(Transform root, Dictionary<string, Material> mats)
        {
            const float wallY = -2.6632f;
            CreateSupplyBox("SR-05 ejection bay wall position backplate", root, new Vector3(0f, wallY + 0.04f, 1.42f), new Vector3(4.55f, 0.16f, 2.20f), mats["ejection_back"]);
            CreateSupplyBox("SR-05 ejection bay closed frame top", root, new Vector3(0f, wallY + 0.17f, 2.23f), new Vector3(3.52f, 0.20f, 0.28f), mats["door_frame"]);
            CreateSupplyBox("SR-05 ejection bay closed frame bottom", root, new Vector3(0f, wallY + 0.17f, 0.62f), new Vector3(3.52f, 0.20f, 0.28f), mats["door_frame"]);
            CreateSupplyBox("SR-05 ejection bay left frame", root, new Vector3(-1.88f, wallY + 0.17f, 1.42f), new Vector3(0.26f, 0.20f, 1.82f), mats["door_frame"]);
            CreateSupplyBox("SR-05 ejection bay right frame", root, new Vector3(1.88f, wallY + 0.17f, 1.42f), new Vector3(0.26f, 0.20f, 1.82f), mats["door_frame"]);
            CreateSupplyBox("SR-06 ejection bay upper closed door", root, new Vector3(0f, wallY + 0.25f, 1.78f), new Vector3(3.26f, 0.12f, 0.70f), mats["ejection_door"]);
            CreateSupplyBox("SR-06 ejection bay lower closed door", root, new Vector3(0f, wallY + 0.25f, 1.06f), new Vector3(3.26f, 0.12f, 0.70f), mats["ejection_door"]);
            CreateSupplyBox("SR-06 ejection bay center seam", root, new Vector3(0f, wallY + 0.325f, 1.42f), new Vector3(3.18f, 0.035f, 0.035f), mats["hazard"]);

            const float terminalX = 2.48f;
            const float terminalY = -2.0832f;
            CreateSupplyBox("SR-07 visible ejection terminal floor pedestal", root, new Vector3(terminalX, terminalY - 0.10f, 0.68f), new Vector3(0.22f, 0.28f, 1.12f), mats["terminal_frame"]);
            CreateSupplyBox("SR-07 visible ejection terminal angled support arm", root, new Vector3(terminalX, terminalY - 0.02f, 1.20f), new Vector3(0.18f, 0.34f, 0.24f), mats["terminal_frame"]);
            CreateSupplyBox("SR-07 visible ejection terminal screen housing", root, new Vector3(terminalX, terminalY, 1.52f), new Vector3(0.78f, 0.22f, 0.76f), mats["terminal_frame"]);
            CreateSupplyBox("SR-07 visible ejection terminal inactive screen", root, new Vector3(terminalX, terminalY + 0.124f, 1.62f), new Vector3(0.58f, 0.032f, 0.40f), mats["terminal_screen"]);
            CreateSupplyBox("SR-07 visible ejection terminal warning button", root, new Vector3(terminalX, terminalY + 0.130f, 1.14f), new Vector3(0.32f, 0.038f, 0.16f), mats["hazard"]);
            CreateSupplyBox("SR-07 visible ejection terminal status light", root, new Vector3(terminalX + 0.28f, terminalY + 0.132f, 1.94f), new Vector3(0.14f, 0.038f, 0.10f), mats["terminal_screen"]);
        }

        private static void CreateSupplyRoomSr07HskScreenOnly(Transform root)
        {
            var inactiveScreen = RequireSupplyRoomChild(root, SupplyRoomSr07InactiveScreenName);
            var material = CreateSupplyHskScreenMaterial();
            var visibleForward = -(inactiveScreen.localRotation * Vector3.forward);
            var visibleUp = inactiveScreen.localRotation * Vector3.up;

            var obj = new GameObject(SupplyRoomSr07HskScreenName);
            obj.transform.SetParent(root, false);
            obj.transform.localPosition = inactiveScreen.localPosition + visibleForward.normalized * 0.021f;
            obj.transform.localRotation = Quaternion.LookRotation(visibleForward, visibleUp);
            obj.transform.localScale = Vector3.one;

            var meshFilter = obj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateSupplyHskScreenMesh(0.59f, 0.405f, new Vector2(0.0f, 0.075f), new Vector2(0.86f, 0.925f));

            var meshRenderer = obj.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
        }

        private static Mesh CreateSupplyHskScreenMesh(float width, float height, Vector2 uvMin, Vector2 uvMax)
        {
            var halfWidth = width * 0.5f;
            var halfHeight = height * 0.5f;
            var mesh = new Mesh
            {
                name = "SR-07 HSK open close terminal screen mesh",
                vertices = new[]
                {
                    new Vector3(-halfWidth, -halfHeight, 0f),
                    new Vector3(-halfWidth, halfHeight, 0f),
                    new Vector3(halfWidth, halfHeight, 0f),
                    new Vector3(halfWidth, -halfHeight, 0f),
                },
                triangles = new[] { 0, 2, 1, 0, 3, 2 },
                uv = new[]
                {
                    new Vector2(uvMin.x, uvMin.y),
                    new Vector2(uvMin.x, uvMax.y),
                    new Vector2(uvMax.x, uvMax.y),
                    new Vector2(uvMax.x, uvMin.y),
                }
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material CreateSupplyHskScreenMaterial()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SupplyRoomHskOpenCloseTexturePath);
            if (texture == null)
            {
                throw new InvalidOperationException("Missing SR-07 HSK Open/Close texture: " + SupplyRoomHskOpenCloseTexturePath);
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Texture");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader)
            {
                name = "SR-07 HSK open close terminal screen material",
                mainTexture = texture,
                color = Color.white
            };

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 0f);
            }

            return material;
        }

        private static void CreateSupplySr08EjectionHazardFloorAtCurrentEjectionSide(Transform root, Dictionary<string, Material> mats)
        {
            const float zoneCenterZ = -1.40f;
            const float zoneWidth = 3.92f;
            const float zoneDepth = 1.82f;
            const float zoneY = 0.238f;
            const float leftX = -zoneWidth * 0.5f;
            const float rightX = zoneWidth * 0.5f;
            const float ejectionSideZ = zoneCenterZ - zoneDepth * 0.5f;
            const float safeSideZ = zoneCenterZ + zoneDepth * 0.5f;

            CreateSupplyBoxLocal("SR-08 ejection hazard steel floor plate main", root, new Vector3(0f, zoneY, zoneCenterZ), new Vector3(zoneWidth, 0.055f, zoneDepth), mats["ejection_zone_plate"], Quaternion.identity);

            var verticalSeamXs = new[] { -1.30f, 0f, 1.30f };
            for (var i = 0; i < verticalSeamXs.Length; i++)
            {
                var x = verticalSeamXs[i];
                CreateSupplyBoxLocal(
                    "SR-08 ejection hazard plate vertical seam " + x.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture),
                    root,
                    new Vector3(x, zoneY + 0.038f, zoneCenterZ),
                    new Vector3(0.040f, 0.018f, zoneDepth - 0.18f),
                    mats["ejection_zone_seam"],
                    Quaternion.identity);
            }

            var crossSeamZs = new[] { zoneCenterZ - 0.46f, zoneCenterZ + 0.46f };
            for (var i = 0; i < crossSeamZs.Length; i++)
            {
                var z = crossSeamZs[i];
                CreateSupplyBoxLocal(
                    "SR-08 ejection hazard plate cross seam " + z.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture),
                    root,
                    new Vector3(0f, zoneY + 0.040f, z),
                    new Vector3(zoneWidth - 0.22f, 0.018f, 0.040f),
                    mats["ejection_zone_seam"],
                    Quaternion.identity);
            }

            CreateSupplyBoxLocal("SR-08 ejection hazard floor amber ejection-side trim", root, new Vector3(0f, zoneY + 0.056f, ejectionSideZ), new Vector3(zoneWidth, 0.026f, 0.075f), mats["hazard"], Quaternion.identity);
            CreateSupplyBoxLocal("SR-08 ejection hazard floor amber safe-side trim", root, new Vector3(0f, zoneY + 0.056f, safeSideZ), new Vector3(zoneWidth, 0.026f, 0.075f), mats["hazard"], Quaternion.identity);
            CreateSupplyBoxLocal("SR-08 ejection hazard floor amber left trim", root, new Vector3(leftX, zoneY + 0.056f, zoneCenterZ), new Vector3(0.075f, 0.026f, zoneDepth), mats["hazard"], Quaternion.identity);
            CreateSupplyBoxLocal("SR-08 ejection hazard floor amber right trim", root, new Vector3(rightX, zoneY + 0.056f, zoneCenterZ), new Vector3(0.075f, 0.026f, zoneDepth), mats["hazard"], Quaternion.identity);

            var slashXs = new[] { -1.55f, -0.95f, -0.35f, 0.25f, 0.85f, 1.45f };
            for (var i = 0; i < slashXs.Length; i++)
            {
                CreateSupplyBoxLocal(
                    "SR-08 ejection hazard black diagonal caution slash " + (i + 1).ToString(CultureInfo.InvariantCulture),
                    root,
                    new Vector3(slashXs[i], zoneY + 0.078f, safeSideZ),
                    new Vector3(0.075f, 0.020f, 0.56f),
                    mats["hazard_dark"],
                    Quaternion.Euler(0f, -34f, 0f));
            }

            var chevronXs = new[] { -0.42f, 0.42f };
            for (var i = 0; i < chevronXs.Length; i++)
            {
                var x = chevronXs[i];
                CreateSupplyBoxLocal(
                    "SR-08 ejection pull direction chevron " + (i + 1).ToString(CultureInfo.InvariantCulture) + " left stroke",
                    root,
                    new Vector3(x - 0.18f, zoneY + 0.070f, zoneCenterZ - 0.18f),
                    new Vector3(0.070f, 0.022f, 0.74f),
                    mats["hazard"],
                    Quaternion.Euler(0f, -28f, 0f));
                CreateSupplyBoxLocal(
                    "SR-08 ejection pull direction chevron " + (i + 1).ToString(CultureInfo.InvariantCulture) + " right stroke",
                    root,
                    new Vector3(x + 0.18f, zoneY + 0.070f, zoneCenterZ - 0.18f),
                    new Vector3(0.070f, 0.022f, 0.74f),
                    mats["hazard"],
                    Quaternion.Euler(0f, 28f, 0f));
            }

            var boltIndex = 1;
            var boltXs = new[] { -1.68f, -0.56f, 0.56f, 1.68f };
            var boltZs = new[] { ejectionSideZ + 0.18f, zoneCenterZ, safeSideZ - 0.18f };
            for (var xIndex = 0; xIndex < boltXs.Length; xIndex++)
            {
                for (var zIndex = 0; zIndex < boltZs.Length; zIndex++)
                {
                    CreateSupplyBoxLocal(
                        "SR-08 ejection hazard recessed floor bolt " + boltIndex.ToString("00", CultureInfo.InvariantCulture),
                        root,
                        new Vector3(boltXs[xIndex], zoneY + 0.076f, boltZs[zIndex]),
                        new Vector3(0.085f, 0.018f, 0.085f),
                        mats["ejection_zone_bolt"],
                        Quaternion.identity);
                    boltIndex++;
                }
            }

            CreateSupplyTextLocal(
                "SR-08 ejection hazard floor stamped label",
                root,
                "EJECTION ZONE",
                new Vector3(0f, zoneY + 0.090f, zoneCenterZ + 0.44f),
                Quaternion.Euler(90f, 0f, 0f),
                0.16f,
                mats["label_text"]);
        }

        private static void CreateSupplyEmptyWall(Transform root, Dictionary<string, Material> mats)
        {
            var ribYs = new[] { -2.1f, -1.05f, 0f, 1.05f, 2.1f };
            for (var i = 0; i < ribYs.Length; i++)
            {
                CreateSupplyBox("SR-01 empty west wall vertical service rib " + ribYs[i].ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture), root, new Vector3(-3.48f, ribYs[i], 1.50f), new Vector3(0.10f, 0.12f, 2.40f), mats["beam"]);
            }
        }

        private static void CreateSupplyCorridorStub(Transform root, string name, float centerY, string label, Material marker, Dictionary<string, Material> mats)
        {
            const float centerX = 4.62f;
            CreateSupplyBox(name + " corridor floor continuation", root, new Vector3(centerX, centerY, 0f), new Vector3(2.08f, 1.54f, 0.16f), mats["corridor_floor"]);
            CreateSupplyBox(name + " corridor upper side wall", root, new Vector3(centerX, centerY + 0.77f, 1.02f), new Vector3(2.08f, 0.20f, 2.04f), mats["wall_dark"]);
            CreateSupplyBox(name + " corridor lower side wall", root, new Vector3(centerX, centerY - 0.77f, 1.02f), new Vector3(2.08f, 0.20f, 2.04f), mats["wall_dark"]);
            CreateSupplyBox(name + " colored threshold", root, new Vector3(3.63f, centerY, 0.210f), new Vector3(0.62f, 1.64f, 0.055f), marker);
            CreateSupplyText(name + " floor direction label", root, label, new Vector3(3.98f, centerY, 0.255f), Quaternion.Euler(90f, 0f, 0f), 0.15f, mats["label_text"]);
        }

        private static void CreateSupplyRoomSr11TextOnly(Transform root, Dictionary<string, Material> mats)
        {
            CreateSupplyReadableTextGroup(
                "SR-11 armory direction wall text",
                root,
                "ARMORY",
                new Vector3(3.36f, 2.525f, -1.08f),
                Quaternion.Euler(0f, -90f, 0f),
                0.11f,
                0.90f,
                new Color(0.92f, 0.88f, 0.66f, 1f));
            CreateSupplyReadableTextGroup(
                "SR-11 cargo hold direction wall text",
                root,
                "CARGO HOLD",
                new Vector3(3.36f, 2.525f, 1.18f),
                Quaternion.Euler(0f, -90f, 0f),
                0.095f,
                0.90f,
                new Color(0.92f, 0.88f, 0.66f, 1f));
        }

        private static void CreateSupplyRoomSr12CctvOnly(Transform root, Dictionary<string, Material> mats)
        {
            var group = new GameObject("SR-12 CCTV northwest corner anchor");
            group.transform.SetParent(root, false);
            group.transform.localPosition = Vector3.zero;
            group.transform.localRotation = Quaternion.identity;
            group.transform.localScale = Vector3.one;

            const float westX = -3.6f;
            const float northY = 2.9f;
            var cornerX = westX + 0.34f;
            var cornerY = northY - 0.34f;
            var wallJoint = new Vector3(westX + 0.30f, northY - 0.30f, 2.68f);
            var elbow = new Vector3(-3.02f, 2.30f, 2.56f);
            var armEnd = new Vector3(-2.72f, 2.08f, 2.42f);
            var yawDegrees = -37f;
            var bodyCenter = new Vector3(-2.46f, 1.88f, 2.34f);
            var bodyRotation = Quaternion.Euler(0f, -yawDegrees, 0f);

            CreateSupplyBoxLocal(
                "SR-12 CCTV northwest west wall mounting plate",
                group.transform,
                BlenderToUnity(new Vector3(westX + 0.052f, cornerY, 2.48f)),
                BlenderBoxScaleToUnity(new Vector3(0.060f, 0.52f, 0.56f)),
                mats["cctv_mount"],
                Quaternion.identity);
            CreateSupplyBoxLocal(
                "SR-12 CCTV northwest north wall mounting plate",
                group.transform,
                BlenderToUnity(new Vector3(cornerX, northY - 0.052f, 2.50f)),
                BlenderBoxScaleToUnity(new Vector3(0.56f, 0.060f, 0.42f)),
                mats["cctv_mount"],
                Quaternion.identity);
            CreateSupplyBoxLocal(
                "SR-12 CCTV northwest overhead junction block",
                group.transform,
                BlenderToUnity(new Vector3(cornerX, cornerY, 2.76f)),
                BlenderBoxScaleToUnity(new Vector3(0.34f, 0.34f, 0.16f)),
                mats["cctv_mount"],
                Quaternion.identity);
            CreateSupplyCylinderBetween(
                "SR-12 CCTV black cable along north wall",
                group.transform,
                new Vector3(westX + 0.17f, northY - 0.055f, 2.77f),
                new Vector3(-2.55f, northY - 0.055f, 2.77f),
                0.018f,
                mats["cctv_cable"]);
            CreateSupplyCylinderBetween(
                "SR-12 CCTV black cable down west wall",
                group.transform,
                new Vector3(westX + 0.055f, cornerY + 0.20f, 2.77f),
                new Vector3(westX + 0.055f, cornerY + 0.20f, 2.38f),
                0.016f,
                mats["cctv_cable"]);
            CreateSupplySphere("SR-12 CCTV wall swivel ball joint", group.transform, wallJoint, 0.095f, mats["cctv_mount"]);
            CreateSupplyCylinderBetween(
                "SR-12 CCTV short articulated arm upper segment",
                group.transform,
                wallJoint,
                elbow,
                0.045f,
                mats["cctv_mount"]);
            CreateSupplySphere("SR-12 CCTV elbow hinge joint", group.transform, elbow, 0.075f, mats["cctv_mount"]);
            CreateSupplyCylinderBetween(
                "SR-12 CCTV short articulated arm lower segment",
                group.transform,
                elbow,
                armEnd,
                0.040f,
                mats["cctv_mount"]);
            CreateSupplyBoxLocal(
                "SR-12 CCTV angled compact camera body",
                group.transform,
                BlenderToUnity(bodyCenter),
                BlenderBoxScaleToUnity(new Vector3(0.48f, 0.25f, 0.22f)),
                mats["cctv_body"],
                bodyRotation);
            CreateSupplyBoxLocal(
                "SR-12 CCTV protective top hood",
                group.transform,
                BlenderToUnity(new Vector3(bodyCenter.x + 0.02f, bodyCenter.y - 0.02f, bodyCenter.z + 0.13f)),
                BlenderBoxScaleToUnity(new Vector3(0.56f, 0.33f, 0.060f)),
                mats["cctv_mount"],
                bodyRotation);
            CreateSupplyCylinderBetween(
                "SR-12 CCTV rear clamp to camera body",
                group.transform,
                armEnd,
                new Vector3(
                    bodyCenter.x - 0.22f * Mathf.Cos(yawDegrees * Mathf.Deg2Rad),
                    bodyCenter.y - 0.22f * Mathf.Sin(yawDegrees * Mathf.Deg2Rad),
                    bodyCenter.z + 0.01f),
                0.055f,
                mats["cctv_mount"]);

            var forward = new Vector3(
                Mathf.Cos(yawDegrees * Mathf.Deg2Rad),
                Mathf.Sin(yawDegrees * Mathf.Deg2Rad),
                -0.08f).normalized;
            var lensStart = bodyCenter + forward * 0.23f;
            var lensEnd = lensStart + forward * 0.18f;
            CreateSupplyCylinderBetween(
                "SR-12 CCTV dark recessed lens barrel",
                group.transform,
                lensStart,
                lensEnd,
                0.105f,
                mats["cctv_lens"]);

            var glassEnd = lensEnd + forward * 0.025f;
            CreateSupplyCylinderBetween(
                "SR-12 CCTV faint glass lens face",
                group.transform,
                lensEnd,
                glassEnd,
                0.082f,
                mats["cctv_glass"]);

            var rayOrigin = glassEnd + forward * 0.03f;
            CreateSupplyCylinderBetween("SR-12 CCTV viewing direction center ray", group.transform, rayOrigin, new Vector3(-0.45f, 0.18f, 0.42f), 0.014f, mats["cctv_view"]);
            CreateSupplyCylinderBetween("SR-12 CCTV viewing direction left edge ray", group.transform, rayOrigin, new Vector3(-1.38f, 0.92f, 0.40f), 0.010f, mats["cctv_view"]);
            CreateSupplyCylinderBetween("SR-12 CCTV viewing direction right edge ray", group.transform, rayOrigin, new Vector3(0.22f, -0.76f, 0.38f), 0.010f, mats["cctv_view"]);
        }

        private static void CreateSupplyOutlineMarkers(Transform root, Dictionary<string, Material> mats)
        {
            CreateSupplyBox("SR-01 north supply storage wall floor marker", root, new Vector3(0f, 2.44f, 0.205f), new Vector3(5.70f, 0.08f, 0.045f), mats["storage_marker"]);
            CreateSupplyBox("SR-01 south ejection wall floor marker", root, new Vector3(0f, -2.44f, 0.205f), new Vector3(4.80f, 0.08f, 0.045f), mats["ejection_marker"]);
            CreateSupplyBox("SR-01 west empty wall floor marker", root, new Vector3(-3.15f, 0f, 0.205f), new Vector3(0.08f, 4.80f, 0.045f), mats["empty_marker"]);
        }

        private static void CreateSupplyBox(string name, Transform parent, Vector3 blenderPosition, Vector3 blenderScale, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = BlenderToUnity(blenderPosition);
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = BlenderBoxScaleToUnity(blenderScale);
            ApplyMaterial(obj, material);
            DisableCollider(obj);
        }

        private static void CreateSupplyBoxLocal(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            Quaternion localRotation)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = localRotation;
            obj.transform.localScale = localScale;
            ApplyMaterial(obj, material);
            DisableCollider(obj);
        }

        private static void CreateSupplyCylinderBetween(
            string name,
            Transform parent,
            Vector3 blenderStart,
            Vector3 blenderEnd,
            float radius,
            Material material)
        {
            var start = BlenderToUnity(blenderStart);
            var end = BlenderToUnity(blenderEnd);
            var direction = end - start;
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = (start + end) * 0.5f;
            obj.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            obj.transform.localScale = new Vector3(radius * 2f, direction.magnitude * 0.5f, radius * 2f);
            ApplyMaterial(obj, material);
            DisableCollider(obj);
        }

        private static void CreateSupplySphere(
            string name,
            Transform parent,
            Vector3 blenderPosition,
            float radius,
            Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = BlenderToUnity(blenderPosition);
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one * (radius * 2f);
            ApplyMaterial(obj, material);
            DisableCollider(obj);
        }

        private static void CreateSupplyText(
            string name,
            Transform parent,
            string text,
            Vector3 blenderPosition,
            Quaternion localRotation,
            float characterSize,
            Material material)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = BlenderToUnity(blenderPosition);
            obj.transform.localRotation = localRotation;
            obj.transform.localScale = Vector3.one;

            var textMesh = obj.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = characterSize;
            textMesh.fontSize = 64;

            var renderer = obj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void CreateSupplyTextLocal(
            string name,
            Transform parent,
            string text,
            Vector3 localPosition,
            Quaternion localRotation,
            float characterSize,
            Material material)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = localRotation;
            obj.transform.localScale = Vector3.one;

            var textMesh = obj.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = characterSize;
            textMesh.fontSize = 64;

            var renderer = obj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void CreateSupplyReadableTextGroup(
            string name,
            Transform parent,
            string text,
            Vector3 localPosition,
            Quaternion localRotation,
            float characterSize,
            float fitWidth,
            Color color,
            float objectScale = 1f)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, false);
            group.transform.localPosition = localPosition;
            group.transform.localRotation = localRotation;
            group.transform.localScale = Vector3.one;

            var slotCount = Mathf.Max(1, text.Length);
            var spacing = slotCount > 1 ? fitWidth / (slotCount - 1) : 0f;
            var left = slotCount > 1 ? fitWidth * -0.5f : 0f;
            for (var i = 0; i < text.Length; i++)
            {
                var value = text[i];
                if (value == ' ')
                {
                    continue;
                }

                var offset = left + (spacing * i);
                CreateSupplyReadableTextLocal(
                    name + " letter " + (i + 1).ToString("00", CultureInfo.InvariantCulture) + " " + value,
                    group.transform,
                    value.ToString(),
                    new Vector3(offset, 0f, 0f),
                    Quaternion.identity,
                    characterSize,
                    color,
                    objectScale);
            }
        }

        private static void CreateSupplyReadableTextLocal(
            string name,
            Transform parent,
            string text,
            Vector3 localPosition,
            Quaternion localRotation,
            float characterSize,
            Color color,
            float objectScale = 1f)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = localRotation;
            obj.transform.localScale = Vector3.one * objectScale;

            var textMesh = obj.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = characterSize;
            textMesh.fontSize = 96;
            textMesh.richText = false;
            textMesh.color = color;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            if (font != null)
            {
                textMesh.font = font;
            }

            var renderer = obj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateReadableTextMaterial(textMesh.font, name + " readable material", color);
            }
        }

        private static Material CreateReadableTextMaterial(Font font, string name, Color color)
        {
            Material material = null;
            if (font != null && font.material != null)
            {
                material = new Material(font.material);
            }

            if (material == null)
            {
                var shader = Shader.Find("GUI/Text Shader");
                if (shader == null)
                {
                    shader = Shader.Find("Sprites/Default");
                }

                if (shader == null)
                {
                    shader = Shader.Find("Universal Render Pipeline/Unlit");
                }

                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                if (shader != null)
                {
                    material = new Material(shader);
                }
            }

            if (material == null)
            {
                return null;
            }

            material.name = name;
            material.color = color;
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            return material;
        }

        private static Vector3 FindSupplyRoomPlacement(Transform root, IReadOnlyList<GameObject> protectedRoots)
        {
            var engineRoot = FindNamedObject("Approved Engine Room 01 Shell") ?? FindObjectByNameTokens("engine", "room");
            if (engineRoot == null)
            {
                throw new InvalidOperationException("Cannot place supply room Z-below engine room because no engine room root could be found.");
            }

            var armoryRoot = FindNamedObject(RootName) ?? FindObjectByNameTokens("armory");
            var lineRoot = armoryRoot != null ? armoryRoot : engineRoot;
            var originalPosition = root.position;
            var supplyBoundsAtOrigin = GetRendererBounds(root);
            var hasEngineBounds = TryGetRendererBounds(engineRoot.transform, out var engineBounds);
            var firstZ = hasEngineBounds
                ? engineBounds.min.z - supplyBoundsAtOrigin.extents.z - 1.2f
                : engineRoot.transform.position.z - 9.0f;
            const float step = 1.5f;
            const int maxAttempts = 100;

            try
            {
                for (var i = 0; i < maxAttempts; i++)
                {
                    var candidate = new Vector3(lineRoot.transform.position.x, lineRoot.transform.position.y, firstZ - (i * step));
                    root.position = candidate;

                    var candidateBounds = GetRendererBounds(root);
                    var belowEngine = !hasEngineBounds || candidateBounds.max.z < engineBounds.min.z - 0.05f;
                    if (belowEngine && !IntersectsAnyProtectedBounds(candidateBounds, protectedRoots))
                    {
                        return candidate;
                    }
                }
            }
            finally
            {
                root.position = originalPosition;
            }

            throw new InvalidOperationException(
                "Could not find a non-overlapping Z-below-engine-room position for " +
                SupplyRoomRootName +
                " after " +
                maxAttempts.ToString(CultureInfo.InvariantCulture) +
                " attempts.");
        }

        private static Vector3 FindCargoHoldPlacement(Transform root, IReadOnlyList<GameObject> protectedRoots)
        {
            var cockpitRoot =
                FindNamedObject("Room - Cockpit") ??
                FindNamedObject("Approved Cockpit 01 Shell") ??
                FindObjectByNameTokens("cockpit");
            if (cockpitRoot == null)
            {
                throw new InvalidOperationException("Cannot place cargo hold X-aligned with cockpit because no cockpit root could be found.");
            }

            var armoryRoot = FindNamedObject(RootName) ?? FindObjectByNameTokens("armory");
            if (armoryRoot == null)
            {
                throw new InvalidOperationException("Cannot place cargo hold Z-below armory because no armory root could be found.");
            }

            var originalPosition = root.position;
            var cargoBoundsAtOrigin = GetRendererBounds(root);
            var hasArmoryBounds = TryGetRendererBounds(armoryRoot.transform, out var armoryBounds);
            var firstZ = hasArmoryBounds
                ? armoryBounds.min.z - cargoBoundsAtOrigin.extents.z - 2.0f
                : armoryRoot.transform.position.z - 24.0f;
            var targetY = armoryRoot.transform.position.y;
            const float step = 2.5f;
            const int maxAttempts = 160;

            try
            {
                for (var i = 0; i < maxAttempts; i++)
                {
                    var candidate = new Vector3(cockpitRoot.transform.position.x, targetY, firstZ - (i * step));
                    root.position = candidate;

                    var candidateBounds = GetRendererBounds(root);
                    var belowArmory = hasArmoryBounds
                        ? candidateBounds.max.z < armoryBounds.min.z - 0.05f
                        : candidate.z < armoryRoot.transform.position.z;
                    if (belowArmory && !IntersectsAnyProtectedBounds(candidateBounds, protectedRoots))
                    {
                        return candidate;
                    }
                }
            }
            finally
            {
                root.position = originalPosition;
            }

            throw new InvalidOperationException(
                "Could not find a non-overlapping X-aligned and Z-below-armory position for " +
                CargoHoldRootName +
                " after " +
                maxAttempts.ToString(CultureInfo.InvariantCulture) +
                " attempts.");
        }

        private static void EnsureCargoHoldPlacementConstraints(Transform root, Bounds cargoBounds)
        {
            var cockpitRoot =
                FindNamedObject("Room - Cockpit") ??
                FindNamedObject("Approved Cockpit 01 Shell") ??
                FindObjectByNameTokens("cockpit");
            if (cockpitRoot == null)
            {
                throw new InvalidOperationException("Cannot verify cargo hold X alignment because no cockpit root could be found.");
            }

            var armoryRoot = FindNamedObject(RootName) ?? FindObjectByNameTokens("armory");
            if (armoryRoot == null)
            {
                throw new InvalidOperationException("Cannot verify cargo hold Z-below-armory placement because no armory root could be found.");
            }

            if (Mathf.Abs(root.position.x - cockpitRoot.transform.position.x) > 0.001f)
            {
                throw new InvalidOperationException(
                    "Cargo hold root is not X-aligned with cockpit. CargoX=" +
                    root.position.x.ToString("0.###", CultureInfo.InvariantCulture) +
                    "; CockpitX=" +
                    cockpitRoot.transform.position.x.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (root.localScale.x < 2.0f || root.localScale.y < 2.0f || root.localScale.z < 2.0f)
            {
                throw new InvalidOperationException(
                    "Cargo hold approved Unity scale must be at least 2x. Actual=" +
                    FormatVector(root.localScale));
            }

            if (TryGetRendererBounds(armoryRoot.transform, out var armoryBounds))
            {
                if (cargoBounds.max.z >= armoryBounds.min.z - 0.05f)
                {
                    throw new InvalidOperationException(
                        "Cargo hold bounds are not Z-below armory bounds. CargoMaxZ=" +
                        cargoBounds.max.z.ToString("0.###", CultureInfo.InvariantCulture) +
                        "; ArmoryMinZ=" +
                        armoryBounds.min.z.ToString("0.###", CultureInfo.InvariantCulture));
                }
            }
            else if (root.position.z >= armoryRoot.transform.position.z)
            {
                throw new InvalidOperationException(
                    "Cargo hold root is not Z-below armory root. CargoZ=" +
                    root.position.z.ToString("0.###", CultureInfo.InvariantCulture) +
                    "; ArmoryZ=" +
                    armoryRoot.transform.position.z.ToString("0.###", CultureInfo.InvariantCulture));
            }
        }

        private static Vector3 FindSupplyRoomBelowEngineRoomPosition(Transform root, IReadOnlyList<GameObject> protectedRoots)
        {
            var engineRoot = FindNamedObject("Approved Engine Room 01 Shell") ?? FindObjectByNameTokens("engine", "room");
            if (engineRoot == null)
            {
                throw new InvalidOperationException("Cannot move supply room below engine room because no engine room root could be found.");
            }

            var originalPosition = root.position;
            var supplyBoundsAtOrigin = GetRendererBounds(root);
            var hasEngineBounds = TryGetRendererBounds(engineRoot.transform, out var engineBounds);
            var firstZ = hasEngineBounds
                ? engineBounds.min.z - supplyBoundsAtOrigin.extents.z - 1.2f
                : engineRoot.transform.position.z - 9.0f;
            const float step = 1.5f;
            const int maxAttempts = 100;

            try
            {
                for (var i = 0; i < maxAttempts; i++)
                {
                    var candidate = new Vector3(engineRoot.transform.position.x, engineRoot.transform.position.y, firstZ - (i * step));
                    root.position = candidate;

                    var candidateBounds = GetRendererBounds(root);
                    var belowEngine = !hasEngineBounds || candidateBounds.max.z < engineBounds.min.z - 0.05f;
                    if (belowEngine && !IntersectsAnyProtectedBounds(candidateBounds, protectedRoots))
                    {
                        return candidate;
                    }
                }
            }
            finally
            {
                root.position = originalPosition;
            }

            throw new InvalidOperationException(
                "Could not find a non-overlapping position directly below engine room for " +
                SupplyRoomRootName +
                " after " +
                maxAttempts.ToString(CultureInfo.InvariantCulture) +
                " attempts.");
        }

        private static GameObject FindObjectByNameTokens(params string[] tokens)
        {
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null)
                {
                    continue;
                }

                var lowerName = transform.gameObject.name.ToLowerInvariant();
                var matches = true;
                for (var tokenIndex = 0; tokenIndex < tokens.Length; tokenIndex++)
                {
                    if (lowerName.IndexOf(tokens[tokenIndex].ToLowerInvariant(), StringComparison.Ordinal) < 0)
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    return transform.gameObject;
                }
            }

            return null;
        }

        private static void EnsureSupplyRoomNoOverlap(Bounds supplyBounds, IEnumerable<GameObject> protectedRoots)
        {
            foreach (var root in protectedRoots)
            {
                if (root == null || !TryGetRendererBounds(root.transform, out var protectedBounds))
                {
                    continue;
                }

                if (supplyBounds.Intersects(protectedBounds))
                {
                    throw new InvalidOperationException(
                        "Approved supply room shell overlaps existing object root " +
                        root.name +
                        ". SupplyBounds=" +
                        FormatBounds(supplyBounds) +
                        "; ProtectedBounds=" +
                        FormatBounds(protectedBounds));
                }
            }
        }

        private static HashSet<Transform> CollectSupplyRoomSwapTargets(Transform root)
        {
            var targets = new HashSet<Transform>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || transform == root)
                {
                    continue;
                }

                if (IsSupplyRoomCabinetSwapTarget(transform.name) ||
                    IsSupplyRoomEjectionSwapTarget(transform.name))
                {
                    targets.Add(transform);
                }
            }

            return targets;
        }

        private static bool IsSupplyRoomCabinetSwapTarget(string objectName)
        {
            return objectName.StartsWith("SR-02 supply storage wall placement marker only", StringComparison.Ordinal) ||
                   objectName.StartsWith("SR-02 visible gap behind freestanding locker", StringComparison.Ordinal) ||
                   objectName.StartsWith("SR-02 freestanding", StringComparison.Ordinal) ||
                   objectName.StartsWith("SR-03", StringComparison.Ordinal);
        }

        private static bool IsSupplyRoomEjectionSwapTarget(string objectName)
        {
            return objectName.StartsWith("SR-05 ejection", StringComparison.Ordinal) ||
                   objectName.StartsWith("SR-06 ejection", StringComparison.Ordinal) ||
                   objectName.StartsWith("SR-07 visible ejection terminal", StringComparison.Ordinal);
        }

        private static List<ProtectedTransformSnapshot> CaptureNonSr08SupplyRoomSnapshots(Transform root)
        {
            var snapshots = new List<ProtectedTransformSnapshot>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || IsSupplyRoomSr08Transform(root, transform))
                {
                    continue;
                }

                snapshots.Add(new ProtectedTransformSnapshot(
                    SupplyRoomRootName + "/" + GetRelativePath(root, transform),
                    transform,
                    transform.localPosition,
                    transform.localRotation,
                    transform.localScale,
                    transform.gameObject.activeSelf));
            }

            return snapshots;
        }

        private static List<ProtectedTransformSnapshot> CaptureNonSr11TextSupplyRoomSnapshots(Transform root)
        {
            var snapshots = new List<ProtectedTransformSnapshot>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || IsSupplyRoomSr11TextTransform(root, transform))
                {
                    continue;
                }

                snapshots.Add(new ProtectedTransformSnapshot(
                    SupplyRoomRootName + "/" + GetRelativePath(root, transform),
                    transform,
                    transform.localPosition,
                    transform.localRotation,
                    transform.localScale,
                    transform.gameObject.activeSelf));
            }

            return snapshots;
        }

        private static List<ProtectedTransformSnapshot> CaptureNonSr12SupplyRoomSnapshots(Transform root)
        {
            var snapshots = new List<ProtectedTransformSnapshot>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || IsSupplyRoomSr12Transform(root, transform))
                {
                    continue;
                }

                snapshots.Add(new ProtectedTransformSnapshot(
                    SupplyRoomRootName + "/" + GetRelativePath(root, transform),
                    transform,
                    transform.localPosition,
                    transform.localRotation,
                    transform.localScale,
                    transform.gameObject.activeSelf));
            }

            return snapshots;
        }

        private static List<ProtectedTransformSnapshot> CaptureNonSr07HskSupplyRoomSnapshots(Transform root)
        {
            var snapshots = new List<ProtectedTransformSnapshot>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || IsSupplyRoomSr07HskScreenTransform(root, transform))
                {
                    continue;
                }

                snapshots.Add(new ProtectedTransformSnapshot(
                    SupplyRoomRootName + "/" + GetRelativePath(root, transform),
                    transform,
                    transform.localPosition,
                    transform.localRotation,
                    transform.localScale,
                    transform.gameObject.activeSelf));
            }

            return snapshots;
        }

        private static List<ProtectedTransformSnapshot> CaptureNonCh10CargoHoldSnapshots(Transform root)
        {
            var snapshots = new List<ProtectedTransformSnapshot>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || IsCargoHoldCh10Transform(root, transform))
                {
                    continue;
                }

                snapshots.Add(new ProtectedTransformSnapshot(
                    CargoHoldRootName + "/" + GetRelativePath(root, transform),
                    transform,
                    transform.localPosition,
                    transform.localRotation,
                    transform.localScale,
                    transform.gameObject.activeSelf));
            }

            return snapshots;
        }

        private static List<ProtectedTransformSnapshot> CaptureNonCh11DisplayCargoHoldSnapshots(Transform root)
        {
            var snapshots = new List<ProtectedTransformSnapshot>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || IsCargoHoldCh11DisplayTransform(transform))
                {
                    continue;
                }

                snapshots.Add(new ProtectedTransformSnapshot(
                    CargoHoldRootName + "/" + GetRelativePath(root, transform),
                    transform,
                    transform.localPosition,
                    transform.localRotation,
                    transform.localScale,
                    transform.gameObject.activeSelf));
            }

            return snapshots;
        }

        private static bool IsSupplyRoomSr08Transform(Transform root, Transform transform)
        {
            var current = transform;
            while (current != null && current != root)
            {
                if (current.name.StartsWith("SR-08", StringComparison.Ordinal))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool IsCargoHoldCh10Transform(Transform root, Transform transform)
        {
            var current = transform;
            while (current != null && current != root)
            {
                if (current.name.StartsWith("CH-10", StringComparison.Ordinal))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool IsCargoHoldCh11DisplayTransform(Transform transform)
        {
            if (transform == null)
            {
                return false;
            }

            return string.Equals(transform.name, "CH-11 cargo status panel green display", StringComparison.Ordinal) ||
                   string.Equals(transform.name, "CH-11 cargo status panel top scan line", StringComparison.Ordinal) ||
                   string.Equals(transform.name, "CH-11 cargo status panel middle scan line", StringComparison.Ordinal) ||
                   string.Equals(transform.name, "CH-11 cargo status panel bottom scan line", StringComparison.Ordinal) ||
                   string.Equals(transform.name, "CH-11 cargo status panel amber status lamp", StringComparison.Ordinal) ||
                   string.Equals(transform.name, "CH-11 B2_Eq2_E bottom right display surface", StringComparison.Ordinal);
        }

        private static bool IsSupplyRoomSr07HskScreenTransform(Transform root, Transform transform)
        {
            var current = transform;
            while (current != null && current != root)
            {
                if (string.Equals(current.name, SupplyRoomSr07HskScreenName, StringComparison.Ordinal))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool IsSupplyRoomSr12Transform(Transform root, Transform transform)
        {
            var current = transform;
            while (current != null && current != root)
            {
                if (current.name.StartsWith("SR-12", StringComparison.Ordinal))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool IsSupplyRoomSr12ViewingRay(string objectName)
        {
            return objectName.StartsWith("SR-12 CCTV viewing direction", StringComparison.Ordinal);
        }

        private static bool IsSupplyRoomLockerObjectName(string objectName)
        {
            return objectName.StartsWith("SR-02 freestanding", StringComparison.Ordinal) ||
                   objectName.StartsWith("SR-03", StringComparison.Ordinal);
        }

        private static bool IsSupplyRoomSr11TextTransform(Transform root, Transform transform)
        {
            var current = transform;
            while (current != null && current != root)
            {
                if (IsSupplyRoomSr11TextObjectName(current.name))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool IsSupplyRoomSr11TextObjectName(string objectName)
        {
            return string.Equals(objectName, "SR-11 armory direction wall text", StringComparison.Ordinal) ||
                   string.Equals(objectName, "SR-11 cargo hold direction wall text", StringComparison.Ordinal);
        }

        private static void DeleteExistingSupplyRoomSr08Objects(Transform root)
        {
            var removals = new List<Transform>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || transform == root)
                {
                    continue;
                }

                if (transform.name.StartsWith("SR-08", StringComparison.Ordinal))
                {
                    removals.Add(transform);
                }
            }

            removals.Sort((left, right) => GetDepth(right).CompareTo(GetDepth(left)));
            for (var i = 0; i < removals.Count; i++)
            {
                if (removals[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(removals[i].gameObject);
                }
            }
        }

        private static void DeleteExistingSupplyRoomSr11TextObjects(Transform root)
        {
            var removals = new List<Transform>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || transform == root)
                {
                    continue;
                }

                if (IsSupplyRoomSr11TextObjectName(transform.name))
                {
                    removals.Add(transform);
                }
            }

            removals.Sort((left, right) => GetDepth(right).CompareTo(GetDepth(left)));
            for (var i = 0; i < removals.Count; i++)
            {
                if (removals[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(removals[i].gameObject);
                }
            }
        }

        private static void DeleteExistingSupplyRoomSr12Objects(Transform root)
        {
            var removals = new List<Transform>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || transform == root)
                {
                    continue;
                }

                if (transform.name.StartsWith("SR-12", StringComparison.Ordinal))
                {
                    removals.Add(transform);
                }
            }

            removals.Sort((left, right) => GetDepth(right).CompareTo(GetDepth(left)));
            for (var i = 0; i < removals.Count; i++)
            {
                if (removals[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(removals[i].gameObject);
                }
            }
        }

        private static void DeleteExistingCargoHoldCh11DisplayObjects(Transform root)
        {
            var removals = new List<Transform>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || transform == root)
                {
                    continue;
                }

                if (IsCargoHoldCh11DisplayTransform(transform))
                {
                    removals.Add(transform);
                }
            }

            removals.Sort((left, right) => GetDepth(right).CompareTo(GetDepth(left)));
            for (var i = 0; i < removals.Count; i++)
            {
                if (removals[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(removals[i].gameObject);
                }
            }
        }

        private static void DeleteExistingSupplyRoomSr07HskScreenObjects(Transform root)
        {
            var removals = new List<Transform>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || transform == root)
                {
                    continue;
                }

                if (string.Equals(transform.name, SupplyRoomSr07HskScreenName, StringComparison.Ordinal))
                {
                    removals.Add(transform);
                }
            }

            removals.Sort((left, right) => GetDepth(right).CompareTo(GetDepth(left)));
            for (var i = 0; i < removals.Count; i++)
            {
                if (removals[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(removals[i].gameObject);
                }
            }
        }

        private static void DeleteExistingCargoHoldCh10Objects(Transform root)
        {
            var removals = new List<Transform>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || transform == root)
                {
                    continue;
                }

                if (IsCargoHoldCh10Transform(root, transform))
                {
                    removals.Add(transform);
                }
            }

            removals.Sort((left, right) => GetDepth(right).CompareTo(GetDepth(left)));
            for (var i = 0; i < removals.Count; i++)
            {
                if (removals[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(removals[i].gameObject);
                }
            }
        }

        private static void EnsureOnlySr08ObjectsAdded(
            Transform root,
            IReadOnlyList<ProtectedTransformSnapshot> nonSr08Snapshots)
        {
            var protectedTransforms = new HashSet<Transform>();
            for (var i = 0; i < nonSr08Snapshots.Count; i++)
            {
                if (nonSr08Snapshots[i].Transform != null)
                {
                    protectedTransforms.Add(nonSr08Snapshots[i].Transform);
                }
            }

            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || protectedTransforms.Contains(transform))
                {
                    continue;
                }

                if (!IsSupplyRoomSr08Transform(root, transform))
                {
                    throw new InvalidOperationException("Non-SR-08 supply room object was added during SR-08-only update: " + GetRelativePath(root, transform));
                }
            }
        }

        private static void EnsureOnlyCargoHoldCh10ObjectsAdded(
            Transform root,
            IReadOnlyList<ProtectedTransformSnapshot> nonCh10Snapshots)
        {
            var protectedTransforms = new HashSet<Transform>();
            for (var i = 0; i < nonCh10Snapshots.Count; i++)
            {
                if (nonCh10Snapshots[i].Transform != null)
                {
                    protectedTransforms.Add(nonCh10Snapshots[i].Transform);
                }
            }

            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || protectedTransforms.Contains(transform))
                {
                    continue;
                }

                if (!IsCargoHoldCh10Transform(root, transform))
                {
                    throw new InvalidOperationException("Non-CH-10 cargo hold object was added during CH-10-only update: " + GetRelativePath(root, transform));
                }
            }
        }

        private static void EnsureOnlyCargoHoldCh11DisplayObjectsAdded(
            Transform root,
            IReadOnlyList<ProtectedTransformSnapshot> nonCh11DisplaySnapshots)
        {
            var protectedTransforms = new HashSet<Transform>();
            for (var i = 0; i < nonCh11DisplaySnapshots.Count; i++)
            {
                if (nonCh11DisplaySnapshots[i].Transform != null)
                {
                    protectedTransforms.Add(nonCh11DisplaySnapshots[i].Transform);
                }
            }

            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || protectedTransforms.Contains(transform))
                {
                    continue;
                }

                if (!IsCargoHoldCh11DisplayTransform(transform))
                {
                    throw new InvalidOperationException("Non-CH-11-display cargo hold object was added during CH-11-display-only update: " + GetRelativePath(root, transform));
                }
            }
        }

        private static void EnsureOnlySr11TextObjectsAdded(
            Transform root,
            IReadOnlyList<ProtectedTransformSnapshot> nonSr11TextSnapshots)
        {
            var protectedTransforms = new HashSet<Transform>();
            for (var i = 0; i < nonSr11TextSnapshots.Count; i++)
            {
                if (nonSr11TextSnapshots[i].Transform != null)
                {
                    protectedTransforms.Add(nonSr11TextSnapshots[i].Transform);
                }
            }

            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || protectedTransforms.Contains(transform))
                {
                    continue;
                }

                if (!IsSupplyRoomSr11TextTransform(root, transform))
                {
                    throw new InvalidOperationException("Non-SR-11 text supply room object was added during SR-11-text-only update: " + GetRelativePath(root, transform));
                }
            }
        }

        private static void EnsureOnlySr12ObjectsAdded(
            Transform root,
            IReadOnlyList<ProtectedTransformSnapshot> nonSr12Snapshots)
        {
            var protectedTransforms = new HashSet<Transform>();
            for (var i = 0; i < nonSr12Snapshots.Count; i++)
            {
                if (nonSr12Snapshots[i].Transform != null)
                {
                    protectedTransforms.Add(nonSr12Snapshots[i].Transform);
                }
            }

            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || protectedTransforms.Contains(transform))
                {
                    continue;
                }

                if (!IsSupplyRoomSr12Transform(root, transform))
                {
                    throw new InvalidOperationException("Non-SR-12 supply room object was added during SR-12-only update: " + GetRelativePath(root, transform));
                }
            }
        }

        private static void EnsureOnlySr07HskScreenObjectsAdded(
            Transform root,
            IReadOnlyList<ProtectedTransformSnapshot> nonSr07HskSnapshots)
        {
            var protectedTransforms = new HashSet<Transform>();
            for (var i = 0; i < nonSr07HskSnapshots.Count; i++)
            {
                if (nonSr07HskSnapshots[i].Transform != null)
                {
                    protectedTransforms.Add(nonSr07HskSnapshots[i].Transform);
                }
            }

            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || protectedTransforms.Contains(transform))
                {
                    continue;
                }

                if (!IsSupplyRoomSr07HskScreenTransform(root, transform))
                {
                    throw new InvalidOperationException("Non-SR-07 HSK screen supply room object was added during SR-07-HSK-only update: " + GetRelativePath(root, transform));
                }
            }
        }

        private static void EnsureSupplyRoomSr12DoesNotOverlapLocker(Transform root)
        {
            var cctvRenderers = new List<Renderer>();
            var lockerRenderers = new List<Renderer>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null)
                {
                    continue;
                }

                var renderer = transform.GetComponent<Renderer>();
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (IsSupplyRoomSr12Transform(root, transform) && !IsSupplyRoomSr12ViewingRay(transform.name))
                {
                    cctvRenderers.Add(renderer);
                }
                else if (IsSupplyRoomLockerObjectName(transform.name))
                {
                    lockerRenderers.Add(renderer);
                }
            }

            if (cctvRenderers.Count == 0)
            {
                throw new InvalidOperationException("SR-12 CCTV renderer was not created.");
            }

            if (lockerRenderers.Count == 0)
            {
                throw new InvalidOperationException("Cannot verify SR-12 CCTV locker overlap because no locker renderer was found.");
            }

            for (var cctvIndex = 0; cctvIndex < cctvRenderers.Count; cctvIndex++)
            {
                var cctvRenderer = cctvRenderers[cctvIndex];
                for (var lockerIndex = 0; lockerIndex < lockerRenderers.Count; lockerIndex++)
                {
                    var lockerRenderer = lockerRenderers[lockerIndex];
                    if (cctvRenderer.bounds.Intersects(lockerRenderer.bounds))
                    {
                        throw new InvalidOperationException(
                            "SR-12 CCTV overlaps supply locker. Cctv=" +
                            cctvRenderer.transform.name +
                            "; CctvBounds=" +
                            FormatBounds(cctvRenderer.bounds) +
                            "; Locker=" +
                            lockerRenderer.transform.name +
                            "; LockerBounds=" +
                            FormatBounds(lockerRenderer.bounds));
                    }
                }
            }
        }

        private static int CountSupplyRoomSr08Objects(Transform root)
        {
            var count = 0;
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && IsSupplyRoomSr08Transform(root, transforms[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountSupplyRoomSr11TextObjects(Transform root)
        {
            var count = 0;
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && IsSupplyRoomSr11TextObjectName(transforms[i].name))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountSupplyRoomSr12Objects(Transform root)
        {
            var count = 0;
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && IsSupplyRoomSr12Transform(root, transforms[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountSupplyRoomSr07HskScreenObjects(Transform root)
        {
            var count = 0;
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && IsSupplyRoomSr07HskScreenTransform(root, transforms[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountCargoHoldCh10Objects(Transform root)
        {
            var count = 0;
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && IsCargoHoldCh10Transform(root, transforms[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountCargoHoldCh11DisplayObjects(Transform root)
        {
            var count = 0;
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && IsCargoHoldCh11DisplayTransform(transforms[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private static void EnsureOnlySupplyRoomCabinetAndEjectionChanged(
            IReadOnlyList<ProtectedTransformSnapshot> snapshots,
            ISet<Transform> swapTargets)
        {
            for (var i = 0; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                if (snapshot.Transform == null)
                {
                    throw new InvalidOperationException("Supply room object was removed during cabinet/ejection swap: " + snapshot.Path);
                }

                if (snapshot.Transform.gameObject.activeSelf != snapshot.ActiveSelf)
                {
                    throw new InvalidOperationException("Supply room active state changed during cabinet/ejection swap: " + snapshot.Path);
                }

                if (swapTargets.Contains(snapshot.Transform))
                {
                    var expectedPosition = new Vector3(
                        snapshot.LocalPosition.x,
                        snapshot.LocalPosition.y,
                        -snapshot.LocalPosition.z);
                    var expectedRotation = Quaternion.Euler(0f, 180f, 0f) * snapshot.LocalRotation;

                    if (Vector3.Distance(snapshot.Transform.localPosition, expectedPosition) > 0.0001f ||
                        Quaternion.Angle(snapshot.Transform.localRotation, expectedRotation) > 0.001f ||
                        Vector3.Distance(snapshot.Transform.localScale, snapshot.LocalScale) > 0.0001f)
                    {
                        throw new InvalidOperationException("Supply room swap target changed beyond approved position/rotation swap: " + snapshot.Path);
                    }

                    continue;
                }

                if (Vector3.Distance(snapshot.Transform.localPosition, snapshot.LocalPosition) > 0.0001f ||
                    Quaternion.Angle(snapshot.Transform.localRotation, snapshot.LocalRotation) > 0.001f ||
                    Vector3.Distance(snapshot.Transform.localScale, snapshot.LocalScale) > 0.0001f)
                {
                    throw new InvalidOperationException("Non-swap supply room object transform changed: " + snapshot.Path);
                }
            }
        }

        private static void RequireApprovedSample()
        {
            var approvalPath = Path.Combine(ProjectRoot, ApprovalStatusRelativePath);
            if (!File.Exists(approvalPath))
            {
                throw new InvalidOperationException("Missing approved armory sample status file: " + approvalPath);
            }

            var approval = File.ReadAllText(approvalPath);
            if (approval.IndexOf("\"approvalState\": \"승인\"", StringComparison.Ordinal) < 0 ||
                approval.IndexOf("\"unityApplicationAllowed\": true", StringComparison.Ordinal) < 0)
            {
                throw new InvalidOperationException("Armory shell sample has not been approved for Unity application: " + approvalPath);
            }

            var sampleFbxPath = Path.Combine(ProjectRoot, SampleFbxRelativePath);
            if (!File.Exists(sampleFbxPath))
            {
                throw new InvalidOperationException("Missing approved armory shell FBX sample: " + sampleFbxPath);
            }
        }

        private static void RequireApprovedSupplyRoomSr08Sample()
        {
            var approvalPath = Path.Combine(ProjectRoot, SupplyRoomApprovalStatusRelativePath);
            if (!File.Exists(approvalPath))
            {
                throw new InvalidOperationException("Missing approved supply room sample status file: " + approvalPath);
            }

            var approval = File.ReadAllText(approvalPath);
            if (approval.IndexOf("\"objectId\": \"SR-08\"", StringComparison.Ordinal) < 0 ||
                approval.IndexOf("\"approvalState\": \"승인\"", StringComparison.Ordinal) < 0 ||
                approval.IndexOf("\"unityApplicationAllowed\": true", StringComparison.Ordinal) < 0)
            {
                throw new InvalidOperationException("Supply room SR-08 floor panel sample has not been approved for Unity application: " + approvalPath);
            }
        }

        private static void RequireApprovedSupplyRoomSr11TextOnlySample()
        {
            var approvalPath = Path.Combine(ProjectRoot, SupplyRoomApprovalStatusRelativePath);
            if (!File.Exists(approvalPath))
            {
                throw new InvalidOperationException("Missing approved supply room sample status file: " + approvalPath);
            }

            var approval = File.ReadAllText(approvalPath);
            if (approval.IndexOf("\"objectId\": \"SR-11\"", StringComparison.Ordinal) < 0 ||
                approval.IndexOf("\"approvalState\": \"승인\"", StringComparison.Ordinal) < 0 ||
                approval.IndexOf("\"unityApplicationAllowed\": true", StringComparison.Ordinal) < 0 ||
                approval.IndexOf("\"approvedUnityScope\": \"corridor_text_only\"", StringComparison.Ordinal) < 0)
            {
                throw new InvalidOperationException("Supply room SR-11 corridor text sample has not been approved for text-only Unity application: " + approvalPath);
            }
        }

        private static void RequireApprovedSupplyRoomSr12CctvSample()
        {
            var approvalPath = Path.Combine(ProjectRoot, SupplyRoomApprovalStatusRelativePath);
            if (!File.Exists(approvalPath))
            {
                throw new InvalidOperationException("Missing approved supply room sample status file: " + approvalPath);
            }

            var approval = File.ReadAllText(approvalPath);
            if (approval.IndexOf("\"objectId\": \"SR-12\"", StringComparison.Ordinal) < 0 ||
                approval.IndexOf("\"approvalState\": \"조건부 승인\"", StringComparison.Ordinal) < 0 ||
                approval.IndexOf("\"unityApplicationAllowed\": true", StringComparison.Ordinal) < 0 ||
                approval.IndexOf("사물함 오브젝트와 겹치지 않아야", StringComparison.Ordinal) < 0)
            {
                throw new InvalidOperationException("Supply room SR-12 CCTV sample has not been conditionally approved for Unity application: " + approvalPath);
            }
        }

        private static void RequireApprovedSupplyRoomSr07HskScreenSample()
        {
            var approvalPath = Path.Combine(ProjectRoot, SupplyRoomApprovalStatusRelativePath);
            if (!File.Exists(approvalPath))
            {
                throw new InvalidOperationException("Missing approved supply room sample status file: " + approvalPath);
            }

            var approval = File.ReadAllText(approvalPath);
            if (approval.IndexOf("\"objectId\": \"SR-07\"", StringComparison.Ordinal) < 0 ||
                approval.IndexOf("\"approvalState\": \"승인\"", StringComparison.Ordinal) < 0 ||
                approval.IndexOf("\"unityApplicationAllowed\": true", StringComparison.Ordinal) < 0)
            {
                throw new InvalidOperationException("Supply room SR-07 HSK terminal screen sample has not been approved for Unity application: " + approvalPath);
            }

            var texturePath = Path.Combine(ProjectRoot, SupplyRoomHskOpenCloseTexturePath);
            if (!File.Exists(texturePath))
            {
                throw new InvalidOperationException("Missing SR-07 HSK Open/Close texture file: " + texturePath);
            }
        }

        private static void EnsureImportedSampleAsset()
        {
            var sourcePath = Path.Combine(ProjectRoot, SampleFbxRelativePath);
            var targetDirectory = Path.Combine(ProjectRoot, UnityAssetDirectory);
            var targetPath = Path.Combine(ProjectRoot, UnityFbxPath);

            Directory.CreateDirectory(targetDirectory);
            File.Copy(sourcePath, targetPath, true);
            AssetDatabase.ImportAsset(UnityFbxPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static List<GameObject> FindSceneRootObjectsExcept(string excludedRootName)
        {
            var roots = new List<GameObject>();
            var sceneRoots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (var i = 0; i < sceneRoots.Length; i++)
            {
                if (sceneRoots[i] == null || string.Equals(sceneRoots[i].name, excludedRootName, StringComparison.Ordinal))
                {
                    continue;
                }

                roots.Add(sceneRoots[i]);
            }

            return roots;
        }

        private static List<ProtectedTransformSnapshot> CaptureProtectedSnapshots(IEnumerable<GameObject> roots)
        {
            var snapshots = new List<ProtectedTransformSnapshot>();
            foreach (var root in roots)
            {
                if (root == null)
                {
                    continue;
                }

                var transforms = root.GetComponentsInChildren<Transform>(true);
                for (var i = 0; i < transforms.Length; i++)
                {
                    var transform = transforms[i];
                    if (transform == null)
                    {
                        continue;
                    }

                    snapshots.Add(new ProtectedTransformSnapshot(
                        root.name + "/" + GetRelativePath(root.transform, transform),
                        transform,
                        transform.localPosition,
                        transform.localRotation,
                        transform.localScale,
                        transform.gameObject.activeSelf));
                }
            }

            return snapshots;
        }

        private static List<ProtectedTransformSnapshot> CaptureArmorySnapshots(Transform root)
        {
            var snapshots = new List<ProtectedTransformSnapshot>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null)
                {
                    continue;
                }

                snapshots.Add(new ProtectedTransformSnapshot(
                    GetRelativePath(root, transform),
                    transform,
                    transform.localPosition,
                    transform.localRotation,
                    transform.localScale,
                    transform.gameObject.activeSelf));
            }

            return snapshots;
        }

        private static List<CurrentTransformState> CaptureCurrentTransformStates(Transform root)
        {
            var states = new List<CurrentTransformState>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null)
                {
                    continue;
                }

                states.Add(new CurrentTransformState(
                    transform.name,
                    GetSiblingPath(root, transform),
                    transform.gameObject.activeSelf,
                    transform.localPosition,
                    transform.localRotation,
                    transform.localScale));
            }

            return states;
        }

        private static void ApplyCapturedTransformStates(Transform root, IReadOnlyList<CurrentTransformState> states)
        {
            for (var i = 0; i < states.Count; i++)
            {
                var state = states[i];
                var transform = FindTransformBySiblingPath(root, state.SiblingPath);
                if (!string.Equals(transform.name, state.Name, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Armory current state hierarchy mismatch at sibling path " +
                        FormatSiblingPath(state.SiblingPath) +
                        ". Expected=" +
                        state.Name +
                        "; Actual=" +
                        transform.name);
                }

                transform.localPosition = state.LocalPosition;
                transform.localRotation = state.LocalRotation;
                transform.localScale = state.LocalScale;
                transform.gameObject.SetActive(state.ActiveSelf);
            }
        }

        private static int ApplyCapturedTransformStatesByName(Transform root, IReadOnlyList<CurrentTransformState> states)
        {
            var candidatesByName = new Dictionary<string, Queue<Transform>>(StringComparer.Ordinal);
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (!candidatesByName.TryGetValue(transform.name, out var candidates))
                {
                    candidates = new Queue<Transform>();
                    candidatesByName.Add(transform.name, candidates);
                }

                candidates.Enqueue(transform);
            }

            var appliedCount = 0;
            var missingNames = new List<string>();
            for (var i = 0; i < states.Count; i++)
            {
                var state = states[i];
                if (!candidatesByName.TryGetValue(state.Name, out var candidates) || candidates.Count == 0)
                {
                    missingNames.Add(state.Name);
                    continue;
                }

                var transform = candidates.Dequeue();
                transform.localPosition = state.LocalPosition;
                transform.localRotation = state.LocalRotation;
                transform.localScale = state.LocalScale;
                transform.gameObject.SetActive(state.ActiveSelf);
                appliedCount++;
            }

            if (missingNames.Count > 0)
            {
                throw new InvalidOperationException(
                    "Supply room current state restore by name missed objects: " +
                    string.Join(", ", missingNames));
            }

            return appliedCount;
        }

        private static void EnsureExactCapturedHierarchy(Transform root, IReadOnlyList<CurrentTransformState> states)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            if (transforms.Length != states.Count)
            {
                throw new InvalidOperationException(
                    "Armory current state hierarchy count mismatch. Expected=" +
                    states.Count.ToString(CultureInfo.InvariantCulture) +
                    "; Actual=" +
                    transforms.Length.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static Transform FindTransformBySiblingPath(Transform root, IReadOnlyList<int> siblingPath)
        {
            var current = root;
            for (var i = 0; i < siblingPath.Count; i++)
            {
                var childIndex = siblingPath[i];
                if (childIndex < 0 || childIndex >= current.childCount)
                {
                    throw new InvalidOperationException(
                        "Missing armory transform at sibling path " +
                        FormatSiblingPath(siblingPath));
                }

                current = current.GetChild(childIndex);
            }

            return current;
        }

        private static int[] GetSiblingPath(Transform root, Transform transform)
        {
            var reversed = new List<int>();
            var current = transform;
            while (current != null && current != root)
            {
                reversed.Add(current.GetSiblingIndex());
                current = current.parent;
            }

            reversed.Reverse();
            return reversed.ToArray();
        }

        private static void WriteCurrentStateScript(IReadOnlyList<CurrentTransformState> states)
        {
            var outputPath = Path.Combine(ProjectRoot, CurrentStateUnityPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath, BuildCurrentStateScript(states), new UTF8Encoding(false));
        }

        private static void WriteSupplyRoomCurrentStateScript(IReadOnlyList<CurrentTransformState> states)
        {
            var outputPath = Path.Combine(ProjectRoot, SupplyRoomCurrentStateUnityPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath, BuildSupplyRoomCurrentStateScript(states), new UTF8Encoding(false));
        }

        private static void WriteCargoHoldCurrentStateScript(IReadOnlyList<CurrentTransformState> states)
        {
            var outputPath = Path.Combine(ProjectRoot, CargoHoldCurrentStateUnityPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath, BuildCargoHoldCurrentStateScript(states), new UTF8Encoding(false));
        }

        private static string BuildCurrentStateScript(IReadOnlyList<CurrentTransformState> states)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated>");
            builder.AppendLine("// Captured from the current Unity editor armory state.");
            builder.AppendLine("// </auto-generated>");
            builder.AppendLine("using UnityEngine;");
            builder.AppendLine();
            builder.AppendLine("namespace Bellerophon.Editor.Validation");
            builder.AppendLine("{");
            builder.AppendLine("    internal static class ApprovedArmoryShellCurrentState");
            builder.AppendLine("    {");
            builder.AppendLine("        public static readonly ApprovedArmoryShellBootstrap.CurrentTransformState[] Transforms =");
            builder.AppendLine("        {");
            for (var i = 0; i < states.Count; i++)
            {
                builder.Append("            ");
                AppendCurrentTransformState(builder, states[i]);
                builder.AppendLine(",");
            }

            builder.AppendLine("        };");
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string BuildSupplyRoomCurrentStateScript(IReadOnlyList<CurrentTransformState> states)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated>");
            builder.AppendLine("// Captured from the current Unity editor supply room state.");
            builder.AppendLine("// </auto-generated>");
            builder.AppendLine("using UnityEngine;");
            builder.AppendLine();
            builder.AppendLine("namespace Bellerophon.Editor.Validation");
            builder.AppendLine("{");
            builder.AppendLine("    internal static class ApprovedSupplyRoomShellCurrentState");
            builder.AppendLine("    {");
            builder.AppendLine("        public static readonly ApprovedArmoryShellBootstrap.CurrentTransformState[] Transforms =");
            builder.AppendLine("        {");
            for (var i = 0; i < states.Count; i++)
            {
                builder.Append("            ");
                AppendCurrentTransformState(builder, states[i]);
                builder.AppendLine(",");
            }

            builder.AppendLine("        };");
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string BuildCargoHoldCurrentStateScript(IReadOnlyList<CurrentTransformState> states)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated>");
            builder.AppendLine("// Captured from the current Unity editor cargo hold state.");
            builder.AppendLine("// </auto-generated>");
            builder.AppendLine("using UnityEngine;");
            builder.AppendLine();
            builder.AppendLine("namespace Bellerophon.Editor.Validation");
            builder.AppendLine("{");
            builder.AppendLine("    internal static class ApprovedCargoHoldShellCurrentState");
            builder.AppendLine("    {");
            builder.AppendLine("        public static readonly ApprovedArmoryShellBootstrap.CurrentTransformState[] Transforms =");
            builder.AppendLine("        {");
            for (var i = 0; i < states.Count; i++)
            {
                builder.Append("            ");
                AppendCurrentTransformState(builder, states[i]);
                builder.AppendLine(",");
            }

            builder.AppendLine("        };");
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static void AppendCurrentTransformState(StringBuilder builder, CurrentTransformState state)
        {
            builder.Append("new ApprovedArmoryShellBootstrap.CurrentTransformState(");
            builder.Append(ToCSharpStringLiteral(state.Name));
            builder.Append(", ");
            AppendSiblingPath(builder, state.SiblingPath);
            builder.Append(", ");
            builder.Append(state.ActiveSelf ? "true" : "false");
            builder.Append(", ");
            AppendVector3(builder, state.LocalPosition);
            builder.Append(", ");
            AppendQuaternion(builder, state.LocalRotation);
            builder.Append(", ");
            AppendVector3(builder, state.LocalScale);
            builder.Append(")");
        }

        private static void AppendSiblingPath(StringBuilder builder, IReadOnlyList<int> siblingPath)
        {
            builder.Append("new int[] { ");
            for (var i = 0; i < siblingPath.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(siblingPath[i].ToString(CultureInfo.InvariantCulture));
            }

            builder.Append(" }");
        }

        private static void AppendVector3(StringBuilder builder, Vector3 value)
        {
            builder.Append("new Vector3(");
            builder.Append(FormatFloat(value.x));
            builder.Append(", ");
            builder.Append(FormatFloat(value.y));
            builder.Append(", ");
            builder.Append(FormatFloat(value.z));
            builder.Append(")");
        }

        private static void AppendQuaternion(StringBuilder builder, Quaternion value)
        {
            builder.Append("new Quaternion(");
            builder.Append(FormatFloat(value.x));
            builder.Append(", ");
            builder.Append(FormatFloat(value.y));
            builder.Append(", ");
            builder.Append(FormatFloat(value.z));
            builder.Append(", ");
            builder.Append(FormatFloat(value.w));
            builder.Append(")");
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture) + "f";
        }

        private static string ToCSharpStringLiteral(string value)
        {
            return "\"" +
                   value
                       .Replace("\\", "\\\\")
                       .Replace("\"", "\\\"")
                       .Replace("\r", "\\r")
                       .Replace("\n", "\\n")
                       .Replace("\t", "\\t") +
                   "\"";
        }

        private static List<ProtectedTransformSnapshot> CaptureNonAr03ArmorySnapshots(Transform root)
        {
            var snapshots = new List<ProtectedTransformSnapshot>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || IsAr03Transform(root, transform))
                {
                    continue;
                }

                snapshots.Add(new ProtectedTransformSnapshot(
                    RootName + "/" + GetRelativePath(root, transform),
                    transform,
                    transform.localPosition,
                    transform.localRotation,
                    transform.localScale,
                    transform.gameObject.activeSelf));
            }

            return snapshots;
        }

        private static bool IsAr03Transform(Transform root, Transform transform)
        {
            var current = transform;
            while (current != null && current != root)
            {
                if (current.name.StartsWith("AR-03", StringComparison.Ordinal))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static List<ProtectedTransformSnapshot> CaptureNonAr02Ar03ArmorySnapshots(Transform root)
        {
            var snapshots = new List<ProtectedTransformSnapshot>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null ||
                    IsAr02Transform(root, transform) ||
                    IsAr03Transform(root, transform) ||
                    IsAr04PlatformTransform(root, transform))
                {
                    continue;
                }

                snapshots.Add(new ProtectedTransformSnapshot(
                    RootName + "/" + GetRelativePath(root, transform),
                    transform,
                    transform.localPosition,
                    transform.localRotation,
                    transform.localScale,
                    transform.gameObject.activeSelf));
            }

            return snapshots;
        }

        private static bool IsAr02Transform(Transform root, Transform transform)
        {
            var current = transform;
            while (current != null && current != root)
            {
                if (current.name.StartsWith("AR-02", StringComparison.Ordinal))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool IsAr04PlatformTransform(Transform root, Transform transform)
        {
            var current = transform;
            while (current != null && current != root)
            {
                if (string.Equals(current.name, "AR-04 placeholder top operating platform", StringComparison.Ordinal) ||
                    string.Equals(current.name, "AR-04 placeholder platform safety rim", StringComparison.Ordinal))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static List<ProtectedTransformSnapshot> CaptureNonAr05ArmorySnapshots(Transform root)
        {
            var snapshots = new List<ProtectedTransformSnapshot>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || IsAr05Transform(root, transform))
                {
                    continue;
                }

                snapshots.Add(new ProtectedTransformSnapshot(
                    RootName + "/" + GetRelativePath(root, transform),
                    transform,
                    transform.localPosition,
                    transform.localRotation,
                    transform.localScale,
                    transform.gameObject.activeSelf));
            }

            return snapshots;
        }

        private static bool IsAr05Transform(Transform root, Transform transform)
        {
            var current = transform;
            while (current != null && current != root)
            {
                if (current.name.StartsWith("AR-05", StringComparison.Ordinal))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static void DeleteExistingAr03Objects(Transform root)
        {
            var removals = new List<Transform>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || transform == root)
                {
                    continue;
                }

                if (transform.name.StartsWith("AR-03", StringComparison.Ordinal))
                {
                    removals.Add(transform);
                }
            }

            removals.Sort((left, right) => GetDepth(right).CompareTo(GetDepth(left)));
            for (var i = 0; i < removals.Count; i++)
            {
                if (removals[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(removals[i].gameObject);
                }
            }
        }

        private static int GetDepth(Transform transform)
        {
            var depth = 0;
            var current = transform;
            while (current != null)
            {
                depth++;
                current = current.parent;
            }

            return depth;
        }

        private static void CreateAr03FromApprovedSample(Transform root, Material stairMaterial, Material railMaterial)
        {
            var group = new GameObject("AR-03 stair assembly");
            group.transform.SetParent(root, false);
            group.transform.localPosition = Vector3.zero;
            group.transform.localRotation = Quaternion.identity;
            group.transform.localScale = Vector3.one;

            const int stepCount = 12;
            for (var i = 0; i < stepCount; i++)
            {
                var t = i / (float)(stepCount - 1);
                var blenderY = -3.25f + (t * 1.79f);
                var blenderZ = 0.22f + (t * 2.06f);
                var width = 1.42f - (t * 0.18f);
                CreateAr03Box(
                    "AR-03 placeholder rear stair tread " + (i + 1).ToString("00", CultureInfo.InvariantCulture),
                    group.transform,
                    BlenderToUnity(new Vector3(0f, blenderY, blenderZ)),
                    new Vector3(width, 0.16f, 0.28f),
                    stairMaterial);
            }

            CreateAr03CylinderBetween(
                "AR-03 placeholder stair side rail left",
                group.transform,
                BlenderToUnity(new Vector3(-0.86f, -3.32f, 0.46f)),
                BlenderToUnity(new Vector3(-0.86f, -1.40f, 2.46f)),
                0.030f,
                railMaterial);
            CreateAr03CylinderBetween(
                "AR-03 placeholder stair side rail right",
                group.transform,
                BlenderToUnity(new Vector3(0.86f, -3.32f, 0.46f)),
                BlenderToUnity(new Vector3(0.86f, -1.40f, 2.46f)),
                0.030f,
                railMaterial);
        }

        private static void DeleteExistingAr02Objects(Transform root)
        {
            var removals = new List<Transform>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || transform == root)
                {
                    continue;
                }

                if (transform.name.StartsWith("AR-02", StringComparison.Ordinal))
                {
                    removals.Add(transform);
                }
            }

            removals.Sort((left, right) => GetDepth(right).CompareTo(GetDepth(left)));
            for (var i = 0; i < removals.Count; i++)
            {
                if (removals[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(removals[i].gameObject);
                }
            }
        }

        private static void CreateAr02FromApprovedSample(Transform root, Material pillarMaterial)
        {
            var group = new GameObject("AR-02 low central turret support pillar assembly");
            group.transform.SetParent(root, false);
            group.transform.localPosition = Vector3.zero;
            group.transform.localRotation = Quaternion.identity;
            group.transform.localScale = Vector3.one;

            CreateAr03CylinderBetween(
                "AR-02 placeholder central turret support pillar",
                group.transform,
                BlenderToUnity(new Vector3(0f, -0.28f, 0f)),
                BlenderToUnity(new Vector3(0f, -0.28f, 0.48f)),
                0.54f,
                pillarMaterial);
        }

        private static void CreateAr03LowEightStepFromApprovedSample(Transform root, Material stairMaterial, Material railMaterial)
        {
            var group = new GameObject("AR-03 low five-step stair assembly");
            group.transform.SetParent(root, false);
            group.transform.localPosition = Vector3.zero;
            group.transform.localRotation = Quaternion.identity;
            group.transform.localScale = Vector3.one;

            const int stepCount = 5;
            for (var i = 0; i < stepCount; i++)
            {
                var t = i / (float)(stepCount - 1);
                var blenderY = -3.25f + (t * 1.79f);
                var blenderZ = 0.12f + (t * 0.36f);
                var width = 1.42f - (t * 0.18f);
                CreateAr03Box(
                    "AR-03 placeholder rear stair tread " + (i + 1).ToString("00", CultureInfo.InvariantCulture),
                    group.transform,
                    BlenderToUnity(new Vector3(0f, blenderY, blenderZ)),
                    new Vector3(width, 0.16f, 0.28f),
                    stairMaterial);
            }

            CreateAr03CylinderBetween(
                "AR-03 placeholder stair side rail left",
                group.transform,
                BlenderToUnity(new Vector3(-0.86f, -3.32f, 0.26f)),
                BlenderToUnity(new Vector3(-0.86f, -1.40f, 0.66f)),
                0.030f,
                railMaterial);
            CreateAr03CylinderBetween(
                "AR-03 placeholder stair side rail right",
                group.transform,
                BlenderToUnity(new Vector3(0.86f, -3.32f, 0.26f)),
                BlenderToUnity(new Vector3(0.86f, -1.40f, 0.66f)),
                0.030f,
                railMaterial);
        }

        private static void MoveAr04PlatformToLowerPillarHeight(Transform root)
        {
            SetAr04PlatformLocalZ(root, "AR-04 placeholder top operating platform", 0.0048f);
            SetAr04PlatformLocalZ(root, "AR-04 placeholder platform safety rim", 0.0064f);
        }

        private static void SetAr04PlatformLocalZ(Transform root, string objectName, float localZ)
        {
            var target = FindDescendantByName(root, objectName);
            if (target == null)
            {
                throw new InvalidOperationException("Missing armory platform object: " + objectName);
            }

            var position = target.localPosition;
            position.z = localZ;
            target.localPosition = position;
        }

        private static Transform FindDescendantByName(Transform root, string objectName)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform != null && string.Equals(transform.name, objectName, StringComparison.Ordinal))
                {
                    return transform;
                }
            }

            return null;
        }

        private static void DeleteExistingAr05Objects(Transform root)
        {
            var removals = new List<Transform>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || transform == root)
                {
                    continue;
                }

                if (transform.name.StartsWith("AR-05", StringComparison.Ordinal))
                {
                    removals.Add(transform);
                }
            }

            removals.Sort((left, right) => GetDepth(right).CompareTo(GetDepth(left)));
            for (var i = 0; i < removals.Count; i++)
            {
                if (removals[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(removals[i].gameObject);
                }
            }
        }

        private static void CreateAr05FromApprovedSample(
            Transform root,
            Material consoleMaterial,
            Material railMaterial,
            Material handleMaterial,
            Material thumbSwitchMaterial)
        {
            var group = new GameObject("AR-05 U-yoke turret handle assembly");
            group.transform.SetParent(root, false);
            group.transform.localPosition = Vector3.zero;
            group.transform.localRotation = Quaternion.identity;
            group.transform.localScale = Vector3.one;

            CreateAr05Box(
                "AR-05 placeholder turret handle console base",
                group.transform,
                BlenderToUnity(new Vector3(0f, 0.38f, 2.48f)),
                BlenderBoxScaleToUnity(new Vector3(0.78f, 0.46f, 0.22f)),
                consoleMaterial);
            CreateAr05Box(
                "AR-05 U-yoke hinge housing",
                group.transform,
                BlenderToUnity(new Vector3(0f, 0.61f, 2.66f)),
                BlenderBoxScaleToUnity(new Vector3(0.44f, 0.18f, 0.18f)),
                consoleMaterial);
            CreateAr03CylinderBetween(
                "AR-05 U-yoke angled support column",
                group.transform,
                BlenderToUnity(new Vector3(0f, 0.56f, 2.54f)),
                BlenderToUnity(new Vector3(0f, 0.79f, 2.78f)),
                0.046f,
                railMaterial);
            CreateAr03CylinderBetween(
                "AR-05 U-shaped turret handle lower crossbar",
                group.transform,
                BlenderToUnity(new Vector3(-0.42f, 0.84f, 2.78f)),
                BlenderToUnity(new Vector3(0.42f, 0.84f, 2.78f)),
                0.045f,
                handleMaterial);

            CreateAr05SideHandle(group.transform, "left", -0.42f, railMaterial, handleMaterial, thumbSwitchMaterial);
            CreateAr05SideHandle(group.transform, "right", 0.42f, railMaterial, handleMaterial, thumbSwitchMaterial);

            CreateAr03CylinderBetween(
                "AR-05 U-yoke center pivot pin",
                group.transform,
                BlenderToUnity(new Vector3(-0.18f, 0.81f, 2.78f)),
                BlenderToUnity(new Vector3(0.18f, 0.81f, 2.78f)),
                0.032f,
                railMaterial);
        }

        private static void CreateAr05SideHandle(
            Transform parent,
            string sideName,
            float x,
            Material railMaterial,
            Material handleMaterial,
            Material thumbSwitchMaterial)
        {
            CreateAr03CylinderBetween(
                "AR-05 " + sideName + " vertical thumb grip",
                parent,
                BlenderToUnity(new Vector3(x, 0.84f, 2.78f)),
                BlenderToUnity(new Vector3(x, 0.84f, 3.10f)),
                0.058f,
                handleMaterial);
            CreateAr05Box(
                "AR-05 " + sideName + " thumb switch top cap",
                parent,
                BlenderToUnity(new Vector3(x, 0.84f, 3.122f)),
                BlenderBoxScaleToUnity(new Vector3(0.12f, 0.085f, 0.045f)),
                thumbSwitchMaterial);

            var guardX = x < 0f ? x - 0.105f : x + 0.105f;
            CreateAr03CylinderBetween(
                "AR-05 " + sideName + " thumb switch guard rail",
                parent,
                BlenderToUnity(new Vector3(guardX, 0.84f, 3.08f)),
                BlenderToUnity(new Vector3(guardX, 0.84f, 3.135f)),
                0.014f,
                railMaterial);
        }

        private static Vector3 BlenderToUnity(Vector3 blender)
        {
            return new Vector3(blender.x, blender.z, -blender.y);
        }

        private static Vector3 BlenderBoxScaleToUnity(Vector3 blenderScale)
        {
            return new Vector3(blenderScale.x, blenderScale.z, blenderScale.y);
        }

        private static void CreateAr05Box(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = localScale;
            ApplyMaterial(obj, material);
            DisableCollider(obj);
        }

        private static void CreateAr03Box(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = localScale;
            ApplyMaterial(obj, material);
            DisableCollider(obj);
        }

        private static void CreateAr03CylinderBetween(
            string name,
            Transform parent,
            Vector3 start,
            Vector3 end,
            float radius,
            Material material)
        {
            var direction = end - start;
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = (start + end) * 0.5f;
            obj.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            obj.transform.localScale = new Vector3(radius * 2f, direction.magnitude * 0.5f, radius * 2f);
            ApplyMaterial(obj, material);
            DisableCollider(obj);
        }

        private static void ApplyMaterial(GameObject obj, Material material)
        {
            if (material == null)
            {
                return;
            }

            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static Material FindFirstSharedMaterial(Transform root, string namePrefix)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || !transform.name.StartsWith(namePrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                var renderer = transform.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    return renderer.sharedMaterial;
                }
            }

            return null;
        }

        private static Material CreateAr05ThumbSwitchMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader)
            {
                name = "AR-05 turret thumb switch red cap",
                color = new Color(0.82f, 0.08f, 0.045f, 1f)
            };
            return material;
        }

        private static Vector3 FindFirstNonOverlappingZBelowPosition(Transform root, IReadOnlyList<GameObject> protectedRoots)
        {
            var originalPosition = root.position;
            var basePosition = new Vector3(originalPosition.x, 0f, originalPosition.z);
            var firstZ = basePosition.z - 9.0f;
            const float step = 1.5f;
            const int maxAttempts = 80;

            try
            {
                for (var i = 0; i < maxAttempts; i++)
                {
                    var candidate = new Vector3(basePosition.x, basePosition.y, firstZ - (i * step));
                    root.position = candidate;
                    var bounds = GetRendererBounds(root);
                    if (!IntersectsAnyProtectedBounds(bounds, protectedRoots))
                    {
                        return candidate;
                    }
                }
            }
            finally
            {
                root.position = originalPosition;
            }

            throw new InvalidOperationException(
                "Could not find a non-overlapping Z-below-control-room position for " +
                RootName +
                " after " +
                maxAttempts.ToString(CultureInfo.InvariantCulture) +
                " attempts.");
        }

        private static bool IntersectsAnyProtectedBounds(Bounds armoryBounds, IEnumerable<GameObject> protectedRoots)
        {
            foreach (var root in protectedRoots)
            {
                if (root == null || !TryGetRendererBounds(root.transform, out var protectedBounds))
                {
                    continue;
                }

                if (armoryBounds.Intersects(protectedBounds))
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureProtectedObjectsUntouched(IReadOnlyList<ProtectedTransformSnapshot> snapshots)
        {
            for (var i = 0; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                if (snapshot.Transform == null)
                {
                    throw new InvalidOperationException("Protected object was removed: " + snapshot.Path);
                }

                if (snapshot.Transform.gameObject.activeSelf != snapshot.ActiveSelf)
                {
                    throw new InvalidOperationException("Protected object active state changed: " + snapshot.Path);
                }

                if (Vector3.Distance(snapshot.Transform.localPosition, snapshot.LocalPosition) > 0.0001f ||
                    Quaternion.Angle(snapshot.Transform.localRotation, snapshot.LocalRotation) > 0.001f ||
                    Vector3.Distance(snapshot.Transform.localScale, snapshot.LocalScale) > 0.0001f)
                {
                    throw new InvalidOperationException("Protected object transform changed: " + snapshot.Path);
                }
            }
        }

        private static void EnsureOnlyArmoryRootPositionChanged(
            Transform root,
            IReadOnlyList<ProtectedTransformSnapshot> snapshots,
            Vector3 expectedRootPosition)
        {
            for (var i = 0; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                if (snapshot.Transform == null)
                {
                    throw new InvalidOperationException("Armory object was removed during move: " + snapshot.Path);
                }

                if (snapshot.Transform.gameObject.activeSelf != snapshot.ActiveSelf)
                {
                    throw new InvalidOperationException("Armory active state changed during move: " + snapshot.Path);
                }

                if (snapshot.Transform == root)
                {
                    if (Vector3.Distance(root.position, expectedRootPosition) > 0.0001f ||
                        Quaternion.Angle(root.localRotation, snapshot.LocalRotation) > 0.001f ||
                        Vector3.Distance(root.localScale, snapshot.LocalScale) > 0.0001f)
                    {
                        throw new InvalidOperationException("Armory root changed beyond position-only move.");
                    }

                    continue;
                }

                if (Vector3.Distance(snapshot.Transform.localPosition, snapshot.LocalPosition) > 0.0001f ||
                    Quaternion.Angle(snapshot.Transform.localRotation, snapshot.LocalRotation) > 0.001f ||
                    Vector3.Distance(snapshot.Transform.localScale, snapshot.LocalScale) > 0.0001f)
                {
                    throw new InvalidOperationException("Armory child transform changed during root position move: " + snapshot.Path);
                }
            }
        }

        private static void EnsureOnlySupplyRoomRootPositionChanged(
            Transform root,
            IReadOnlyList<ProtectedTransformSnapshot> snapshots,
            Vector3 expectedRootPosition)
        {
            for (var i = 0; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                if (snapshot.Transform == null)
                {
                    throw new InvalidOperationException("Supply room object was removed during move: " + snapshot.Path);
                }

                if (snapshot.Transform.gameObject.activeSelf != snapshot.ActiveSelf)
                {
                    throw new InvalidOperationException("Supply room active state changed during move: " + snapshot.Path);
                }

                if (snapshot.Transform == root)
                {
                    if (Vector3.Distance(root.position, expectedRootPosition) > 0.0001f ||
                        Quaternion.Angle(root.localRotation, snapshot.LocalRotation) > 0.001f ||
                        Vector3.Distance(root.localScale, snapshot.LocalScale) > 0.0001f)
                    {
                        throw new InvalidOperationException("Supply room root changed beyond position-only move.");
                    }

                    continue;
                }

                if (Vector3.Distance(snapshot.Transform.localPosition, snapshot.LocalPosition) > 0.0001f ||
                    Quaternion.Angle(snapshot.Transform.localRotation, snapshot.LocalRotation) > 0.001f ||
                    Vector3.Distance(snapshot.Transform.localScale, snapshot.LocalScale) > 0.0001f)
                {
                    throw new InvalidOperationException("Supply room child transform changed during root position move: " + snapshot.Path);
                }
            }
        }

        private static void EnsureNoOverlap(Bounds armoryBounds, IEnumerable<GameObject> protectedRoots)
        {
            foreach (var root in protectedRoots)
            {
                if (root == null || !TryGetRendererBounds(root.transform, out var protectedBounds))
                {
                    continue;
                }

                if (armoryBounds.Intersects(protectedBounds))
                {
                    throw new InvalidOperationException(
                        "Approved armory shell overlaps existing object root " +
                        root.name +
                        ". ArmoryBounds=" +
                        FormatBounds(armoryBounds) +
                        "; ProtectedBounds=" +
                        FormatBounds(protectedBounds));
                }
            }
        }

        private static Bounds GetRendererBounds(Transform root)
        {
            if (TryGetRendererBounds(root, out var bounds))
            {
                return bounds;
            }

            throw new InvalidOperationException("No renderers found under " + root.name);
        }

        private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var hasBounds = false;
            bounds = new Bounds(root.position, Vector3.zero);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || !renderers[i].enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderers[i].bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(renderers[i].bounds);
            }

            return hasBounds;
        }

        private static void DisableAllColliders(Transform root)
        {
            var colliders = root.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        private static void DisableCollider(GameObject obj)
        {
            var collider = obj.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        private static void DeleteGeneratedObject(string objectName)
        {
            var existing = FindNamedObject(objectName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static Transform RequireSupplyRoomChild(Transform root, string objectName)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && string.Equals(transforms[i].name, objectName, StringComparison.Ordinal))
                {
                    return transforms[i];
                }
            }

            throw new InvalidOperationException("Missing supply room child object: " + objectName);
        }

        private static GameObject FindNamedObject(string objectName)
        {
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].gameObject.name == objectName)
                {
                    return transforms[i].gameObject;
                }
            }

            return null;
        }

        private static GameObject RequireObject(string objectName)
        {
            var found = FindNamedObject(objectName);
            if (found == null)
            {
                throw new InvalidOperationException("Missing object: " + objectName);
            }

            return found;
        }

        private static string GetRelativePath(Transform root, Transform transform)
        {
            if (transform == root)
            {
                return ".";
            }

            var segments = new List<string>();
            var current = transform;
            while (current != null && current != root)
            {
                segments.Add(current.name);
                current = current.parent;
            }

            segments.Reverse();
            return string.Join("/", segments);
        }

        private static string FormatBounds(Bounds bounds)
        {
            return "center=" + FormatVector(bounds.center) + ",size=" + FormatVector(bounds.size);
        }

        private static string FormatVector(Vector3 value)
        {
            return value.x.ToString("0.00", CultureInfo.InvariantCulture) +
                   "," +
                   value.y.ToString("0.00", CultureInfo.InvariantCulture) +
                   "," +
                   value.z.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string FormatSiblingPath(IReadOnlyList<int> siblingPath)
        {
            if (siblingPath.Count == 0)
            {
                return ".";
            }

            var builder = new StringBuilder();
            for (var i = 0; i < siblingPath.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append("/");
                }

                builder.Append(siblingPath[i].ToString(CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private static Scene RequireCargoRunMvpActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded || scene.path != Phase4CargoShipGrayboxBootstrap.CargoRunScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active open scene before capturing or restoring armory current state. ActiveScene=" +
                    scene.path);
            }

            return scene;
        }

        private static string ProjectRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        public readonly struct CurrentTransformState
        {
            public CurrentTransformState(
                string name,
                int[] siblingPath,
                bool activeSelf,
                Vector3 localPosition,
                Quaternion localRotation,
                Vector3 localScale)
            {
                Name = name;
                SiblingPath = siblingPath;
                ActiveSelf = activeSelf;
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
            }

            public string Name { get; }
            public int[] SiblingPath { get; }
            public bool ActiveSelf { get; }
            public Vector3 LocalPosition { get; }
            public Quaternion LocalRotation { get; }
            public Vector3 LocalScale { get; }
        }

        private readonly struct ProtectedTransformSnapshot
        {
            public ProtectedTransformSnapshot(
                string path,
                Transform transform,
                Vector3 localPosition,
                Quaternion localRotation,
                Vector3 localScale,
                bool activeSelf)
            {
                Path = path;
                Transform = transform;
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
                ActiveSelf = activeSelf;
            }

            public string Path { get; }
            public Transform Transform { get; }
            public Vector3 LocalPosition { get; }
            public Quaternion LocalRotation { get; }
            public Vector3 LocalScale { get; }
            public bool ActiveSelf { get; }
        }
    }
}
