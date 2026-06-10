using System;
using System.Collections.Generic;
using Bellerophon.Core.Player;
using Bellerophon.Core.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class PostDetailedStage3GameplayPropsBootstrap
    {
        public const string CargoRunScenePath = Phase20PresentationBootstrap.CargoRunScenePath;
        public const string Stage3RootName = "Post Detailed Stage 3 Gameplay Props";
        public const string FirstPersonPreviewRootName = "Stage 3 First Person Equipment Preview";
        public const string CrowbarModelName = "Stage 3 First Person Crowbar Model";
        public const string CrowbarContinuousBodyName = CrowbarModelName + " Continuous Steel Body";
        public const string MusketModelName = "Stage 3 First Person Musket Model";
        public const string ProtectiveSuitReadoutName = "Stage 3 Protective Suit Readout";
        public const string CockpitHelmPropName = "Stage 3 Cockpit Helm Prop";
        public const string CockpitStatusScreensName = "Stage 3 Cockpit Status Screens";
        public const string ControlRoomCctvTerminalName = "Stage 3 Control Room CCTV Terminal";
        public const string EngineRoomPowerTerminalName = "Stage 3 Engine Room Power Terminal";
        public const string SupplyRoomStorageCabinetName = "Stage 3 Supply Room Storage Cabinet";
        public const string CargoHoldStatusPanelName = "Stage 3 Cargo Hold Status Panel";
        public const string ArmoryTurretGripMountName = "Stage 3 Armory Turret Grip Mount";
        public const string ContractCargoContainerName = "Stage 3 Contract Cargo Container";
        public const string ContractCargoBodyName = ContractCargoContainerName + " Body";
        public const string ContractCargoStrapHorizontalName = ContractCargoContainerName + " Strap Horizontal";
        public const string ContractCargoStrapVerticalName = ContractCargoContainerName + " Strap Vertical";
        public const string ContractCargoLowerStrapName = ContractCargoContainerName + " Strap Lower";
        public const string PersonalCargoContainerName = "Stage 3 Personal Cargo Container";
        public const string WarningLabelSetName = "Stage 3 Warning Label Set";
        public const string RepairPanelKitName = "Stage 3 Repair Panel Kit";
        public const string DamagedPanelKitName = "Stage 3 Damaged Panel Kit";
        public const string EscapePodVisualName = "Stage 3 Escape Pod Visual";
        public const string SpecialEquipmentRootName = "Stage 3 Special Equipment Silhouettes";
        public const string PresenceDetectorPropName = "Stage 3 Presence Detector Prop";
        public const string LightBladePropName = "Stage 3 Light Blade Prop";
        public const string ElectricMinePropName = "Stage 3 Electric Mine Prop";
        public const string CorridorPurifierIconName = "Stage 3 Corridor Purifier Maintenance Icon";
        public const string DiegeticTerminalShellName = "Stage 3 Diegetic Terminal Shell";
        public const string DiegeticTerminalScreenBackingName = DiegeticTerminalShellName + " Screen Backing Panel";
        public const string DiegeticTerminalButtonMeshName = DiegeticTerminalShellName + " Button Mesh";
        public const string NormalMaterialVariantPanelName = "Stage 3 Normal Material Variant Panel";
        public const string DamagedMaterialVariantPanelName = "Stage 3 Damaged Material Variant Panel";

        public const string MaterialDirectory = "Assets/_Project/Art/Ship/Materials";
        public const string MetalMaterialPath = MaterialDirectory + "/Stage3PropMetal_Dull.mat";
        public const string DarkRubberMaterialPath = MaterialDirectory + "/Stage3Strap_DarkRubber.mat";
        public const string ScreenMaterialPath = MaterialDirectory + "/Stage3Screen_Green.mat";
        public const string WarningMaterialPath = MaterialDirectory + "/Stage3Warning_Red.mat";
        public const string YellowMaterialPath = MaterialDirectory + "/Stage3Warning_Yellow.mat";
        public const string WoodMaterialPath = MaterialDirectory + "/Stage3WeaponWood_Worn.mat";
        public const string CrowbarSteelMaterialPath = MaterialDirectory + "/Stage3CrowbarSteel_Bright.mat";
        public const string DamagedMaterialPath = MaterialDirectory + "/Stage3DamagedPanel_Scorched.mat";
        public const string LightMaterialPath = MaterialDirectory + "/Stage3Light_Cyan.mat";
        public const string CargoMaterialPath = MaterialDirectory + "/Stage3CargoContainer_Worn.mat";

        [MenuItem("Bellerophon/Bootstrap/Ensure Post-Detailed Stage 3 Gameplay Props")]
        public static void EnsureStage3Assets()
        {
            EnsureStage3Assets(validateAfterCreate: true);
        }

        internal static void EnsureStage3AssetsWithoutValidation()
        {
            EnsureStage3Assets(validateAfterCreate: false);
        }

        private static void EnsureStage3Assets(bool validateAfterCreate)
        {
            PostDetailedStage2ShipInteriorEditorValidation.Run();

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            DeleteGeneratedObject(Stage3RootName);
            DeleteGeneratedObject(FirstPersonPreviewRootName);

            var materials = EnsureMaterials();
            var stageRoot = new GameObject(Stage3RootName);
            CreateShipDevices(stageRoot.transform, materials);
            CreateCargoProps(stageRoot.transform, materials);
            CreateDiegeticTerminalShell(stageRoot.transform, materials);
            CreateFirstPersonEquipmentPreview(materials);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CargoRunScenePath);

            if (validateAfterCreate)
            {
                PostDetailedStage3GameplayPropsEditorValidation.ValidateScene();
            }

            if (!Application.isBatchMode)
            {
                EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Post-detailed stage 3 gameplay props assets are ready.");
        }

        private static Stage3Materials EnsureMaterials()
        {
            return new Stage3Materials(
                EnsureMaterial(MetalMaterialPath, new Color(0.27f, 0.28f, 0.27f, 1f)),
                EnsureMaterial(DarkRubberMaterialPath, new Color(0.035f, 0.04f, 0.04f, 1f)),
                EnsureMaterial(ScreenMaterialPath, new Color(0.02f, 0.44f, 0.34f, 1f), true),
                EnsureMaterial(WarningMaterialPath, new Color(0.76f, 0.08f, 0.05f, 1f)),
                EnsureMaterial(YellowMaterialPath, new Color(0.82f, 0.64f, 0.12f, 1f)),
                EnsureMaterial(WoodMaterialPath, new Color(0.33f, 0.20f, 0.12f, 1f)),
                EnsureMaterial(CrowbarSteelMaterialPath, new Color(0.62f, 0.66f, 0.64f, 1f)),
                EnsureMaterial(DamagedMaterialPath, new Color(0.11f, 0.095f, 0.075f, 1f)),
                EnsureMaterial(LightMaterialPath, new Color(0.18f, 0.72f, 0.78f, 1f), true),
                EnsureMaterial(CargoMaterialPath, new Color(0.39f, 0.36f, 0.29f, 1f)));
        }

        private static void CreateShipDevices(Transform parent, Stage3Materials materials)
        {
            CreateCockpitHelm(parent, materials);
            CreateCockpitStatusScreens(parent, materials);
            CreateControlRoomCctvTerminal(parent, materials);
            CreateEngineRoomPowerTerminal(parent, materials);
            CreateSupplyRoomStorageCabinet(parent, materials);
            CreateCargoHoldStatusPanel(parent, materials);
            CreateArmoryTurretGripMount(parent, materials);
        }

        private static void CreateCockpitHelm(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(CockpitHelmPropName, parent);
            CreateBox(CockpitHelmPropName + " Console Base", root.transform, new Vector3(0f, 0.42f, 18.35f), new Vector3(2.4f, 0.45f, 1.15f), materials.Metal);
            CreateBox(CockpitHelmPropName + " Sloped Screen Housing", root.transform, new Vector3(0f, 0.88f, 18.02f), new Vector3(1.95f, 0.18f, 0.72f), materials.Metal, Quaternion.Euler(-14f, 0f, 0f));
            CreateBox(CockpitHelmPropName + " Readiness Screen", root.transform, new Vector3(0f, 1.02f, 17.86f), new Vector3(1.55f, 0.035f, 0.42f), materials.Screen, Quaternion.Euler(-14f, 0f, 0f));
            CreateCylinder(CockpitHelmPropName + " Helm Column", root.transform, new Vector3(0f, 1.03f, 18.56f), new Vector3(0.08f, 0.32f, 0.08f), materials.Metal, Quaternion.identity);

            var center = new Vector3(0f, 1.36f, 18.56f);
            for (var i = 0; i < 8; i++)
            {
                var angle = i * Mathf.PI * 2f / 8f;
                var position = center + new Vector3(Mathf.Cos(angle) * 0.36f, Mathf.Sin(angle) * 0.36f, 0f);
                CreateBox(
                    CockpitHelmPropName + " Ring Segment " + (i + 1),
                    root.transform,
                    position,
                    new Vector3(0.22f, 0.035f, 0.045f),
                    materials.DarkRubber,
                    Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg));
            }

            CreateBox(CockpitHelmPropName + " Left Grip", root.transform, new Vector3(-0.42f, 1.36f, 18.56f), new Vector3(0.1f, 0.22f, 0.08f), materials.DarkRubber, Quaternion.Euler(0f, 0f, 18f));
            CreateBox(CockpitHelmPropName + " Right Grip", root.transform, new Vector3(0.42f, 1.36f, 18.56f), new Vector3(0.1f, 0.22f, 0.08f), materials.DarkRubber, Quaternion.Euler(0f, 0f, -18f));
        }

        private static void CreateCockpitStatusScreens(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(CockpitStatusScreensName, parent);
            for (var i = 0; i < 4; i++)
            {
                var x = -1.35f + (i * 0.9f);
                CreateBox(CockpitStatusScreensName + " Backing " + (i + 1), root.transform, new Vector3(x, 1.58f, 20.96f), new Vector3(0.72f, 0.08f, 0.46f), materials.Metal);
                CreateBox(CockpitStatusScreensName + " Glow " + (i + 1), root.transform, new Vector3(x, 1.62f, 20.91f), new Vector3(0.58f, 0.035f, 0.32f), materials.Screen);
            }
        }

        private static void CreateControlRoomCctvTerminal(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(ControlRoomCctvTerminalName, parent);
            CreateBox(ControlRoomCctvTerminalName + " Desk", root.transform, new Vector3(14f, 0.52f, 20.12f), new Vector3(2.6f, 0.42f, 0.9f), materials.Metal);
            for (var i = 0; i < 3; i++)
            {
                var x = 13.15f + (i * 0.85f);
                CreateBox(ControlRoomCctvTerminalName + " Monitor Frame " + (i + 1), root.transform, new Vector3(x, 1.28f, 19.76f), new Vector3(0.72f, 0.08f, 0.5f), materials.Metal, Quaternion.Euler(0f, 0f, 0f));
                CreateBox(ControlRoomCctvTerminalName + " Monitor Glow " + (i + 1), root.transform, new Vector3(x, 1.33f, 19.72f), new Vector3(0.56f, 0.035f, 0.34f), materials.Screen);
            }

            for (var i = 0; i < 5; i++)
            {
                CreateBox(ControlRoomCctvTerminalName + " Tactile Button " + (i + 1), root.transform, new Vector3(13.1f + (i * 0.45f), 0.78f, 19.64f), new Vector3(0.18f, 0.08f, 0.12f), i % 2 == 0 ? materials.Warning : materials.Yellow);
            }
        }

        private static void CreateEngineRoomPowerTerminal(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(EngineRoomPowerTerminalName, parent);
            CreateBox(EngineRoomPowerTerminalName + " Cabinet", root.transform, new Vector3(-14.1f, 0.82f, 17.25f), new Vector3(1.05f, 1.35f, 0.28f), materials.Metal);
            CreateBox(EngineRoomPowerTerminalName + " Power Screen", root.transform, new Vector3(-14.1f, 1.18f, 17.08f), new Vector3(0.72f, 0.42f, 0.035f), materials.Screen);
            CreateBox(EngineRoomPowerTerminalName + " Overclock Warning", root.transform, new Vector3(-14.1f, 0.6f, 17.06f), new Vector3(0.8f, 0.12f, 0.035f), materials.Warning);
            CreateBox(EngineRoomPowerTerminalName + " Breaker Left", root.transform, new Vector3(-14.42f, 0.23f, 17.04f), new Vector3(0.13f, 0.28f, 0.04f), materials.DarkRubber);
            CreateBox(EngineRoomPowerTerminalName + " Breaker Right", root.transform, new Vector3(-13.78f, 0.23f, 17.04f), new Vector3(0.13f, 0.28f, 0.04f), materials.DarkRubber);
        }

        private static void CreateSupplyRoomStorageCabinet(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(SupplyRoomStorageCabinetName, parent);
            CreateBox(SupplyRoomStorageCabinetName + " Back Plate", root.transform, new Vector3(12.1f, 1.04f, -15.04f), new Vector3(2.4f, 1.5f, 0.16f), materials.Metal);
            for (var row = 0; row < 2; row++)
            {
                for (var col = 0; col < 3; col++)
                {
                    var index = (row * 3) + col + 1;
                    CreateBox(
                        SupplyRoomStorageCabinetName + " Locker Door " + index,
                        root.transform,
                        new Vector3(11.35f + (col * 0.75f), 0.72f + (row * 0.62f), -15.15f),
                        new Vector3(0.58f, 0.48f, 0.08f),
                        materials.Cargo);
                    CreateBox(
                        SupplyRoomStorageCabinetName + " Handle " + index,
                        root.transform,
                        new Vector3(11.55f + (col * 0.75f), 0.72f + (row * 0.62f), -15.22f),
                        new Vector3(0.06f, 0.22f, 0.045f),
                        materials.DarkRubber);
                }
            }
        }

        private static void CreateCargoHoldStatusPanel(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(CargoHoldStatusPanelName, parent);
            CreateBox(CargoHoldStatusPanelName + " Panel Frame", root.transform, new Vector3(3.2f, -1.12f, 2.15f), new Vector3(1.0f, 0.72f, 0.1f), materials.Metal);
            CreateBox(CargoHoldStatusPanelName + " Load Screen", root.transform, new Vector3(3.2f, -0.98f, 2.08f), new Vector3(0.76f, 0.32f, 0.035f), materials.Screen);
            CreateBox(CargoHoldStatusPanelName + " Secure Indicator", root.transform, new Vector3(2.92f, -1.35f, 2.05f), new Vector3(0.22f, 0.1f, 0.035f), materials.Yellow);
            CreateBox(CargoHoldStatusPanelName + " Overload Indicator", root.transform, new Vector3(3.48f, -1.35f, 2.05f), new Vector3(0.22f, 0.1f, 0.035f), materials.Warning);
        }

        private static void CreateArmoryTurretGripMount(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(ArmoryTurretGripMountName, parent);
            CreateBox(ArmoryTurretGripMountName + " Rail", root.transform, new Vector3(-14f, 1.12f, -11.2f), new Vector3(2.2f, 0.18f, 0.26f), materials.Metal);
            CreateCylinder(ArmoryTurretGripMountName + " Pivot", root.transform, new Vector3(-14f, 0.88f, -11.22f), new Vector3(0.16f, 0.22f, 0.16f), materials.Metal, Quaternion.Euler(90f, 0f, 0f));
            CreateBox(ArmoryTurretGripMountName + " Left Grip", root.transform, new Vector3(-14.38f, 0.58f, -11.18f), new Vector3(0.14f, 0.52f, 0.14f), materials.DarkRubber, Quaternion.Euler(0f, 0f, 10f));
            CreateBox(ArmoryTurretGripMountName + " Right Grip", root.transform, new Vector3(-13.62f, 0.58f, -11.18f), new Vector3(0.14f, 0.52f, 0.14f), materials.DarkRubber, Quaternion.Euler(0f, 0f, -10f));
            CreateBox(ArmoryTurretGripMountName + " Sight Hood", root.transform, new Vector3(-14f, 1.38f, -11.08f), new Vector3(0.72f, 0.18f, 0.32f), materials.Metal);
            CreateBox(ArmoryTurretGripMountName + " Trigger Bar", root.transform, new Vector3(-14f, 0.48f, -11.02f), new Vector3(0.58f, 0.08f, 0.08f), materials.Warning);
        }

        private static void CreateCargoProps(Transform parent, Stage3Materials materials)
        {
            CreateContractCargoContainer(parent, materials);
            CreatePersonalCargoContainer(parent, materials);
            CreateWarningLabelSet(parent, materials);
        }

        private static void CreateContractCargoContainer(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(ContractCargoContainerName, parent);
            CreateBox(ContractCargoBodyName, root.transform, new Vector3(0f, -1.78f, -0.05f), new Vector3(2.6f, 1.18f, 1.55f), materials.Cargo);
            CreateBox(ContractCargoContainerName + " Top Frame", root.transform, new Vector3(0f, -1.16f, -0.05f), new Vector3(2.78f, 0.08f, 1.72f), materials.Metal);
            CreateBox(ContractCargoContainerName + " Bottom Frame", root.transform, new Vector3(0f, -2.4f, -0.05f), new Vector3(2.78f, 0.08f, 1.72f), materials.Metal);
            CreateBox(ContractCargoContainerName + " Left Frame", root.transform, new Vector3(-1.38f, -1.78f, -0.05f), new Vector3(0.08f, 1.22f, 1.72f), materials.Metal);
            CreateBox(ContractCargoContainerName + " Right Frame", root.transform, new Vector3(1.38f, -1.78f, -0.05f), new Vector3(0.08f, 1.22f, 1.72f), materials.Metal);
            CreateBox(ContractCargoStrapHorizontalName, root.transform, new Vector3(0f, -1.78f, -0.86f), new Vector3(2.85f, 0.13f, 0.08f), materials.DarkRubber);
            CreateBox(ContractCargoStrapVerticalName, root.transform, new Vector3(0f, -1.78f, -0.89f), new Vector3(0.13f, 1.18f, 0.08f), materials.DarkRubber);
            CreateBox(ContractCargoContainerName + " Bracket Left", root.transform, new Vector3(-1.47f, -1.78f, -0.92f), new Vector3(0.14f, 0.32f, 0.11f), materials.Metal);
            CreateBox(ContractCargoContainerName + " Bracket Right", root.transform, new Vector3(1.47f, -1.78f, -0.92f), new Vector3(0.14f, 0.32f, 0.11f), materials.Metal);
            CreateBox(ContractCargoContainerName + " Bracket Top", root.transform, new Vector3(0f, -1.09f, -0.92f), new Vector3(0.36f, 0.12f, 0.11f), materials.Metal);
            CreateBox(ContractCargoContainerName + " Bracket Bottom", root.transform, new Vector3(0f, -2.47f, -0.92f), new Vector3(0.36f, 0.12f, 0.11f), materials.Metal);
            CreateBox(ContractCargoContainerName + " Lock Tag", root.transform, new Vector3(0.35f, -1.55f, -0.94f), new Vector3(0.26f, 0.18f, 0.035f), materials.Yellow);
        }

        private static void CreatePersonalCargoContainer(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(PersonalCargoContainerName, parent);
            CreateBox(PersonalCargoContainerName + " Body", root.transform, new Vector3(3.05f, -2.03f, -0.55f), new Vector3(1.2f, 0.68f, 0.92f), materials.Cargo);
            CreateBox(PersonalCargoContainerName + " Lid Rail", root.transform, new Vector3(3.05f, -1.64f, -0.55f), new Vector3(1.3f, 0.08f, 1.0f), materials.Metal);
            CreateBox(PersonalCargoContainerName + " Front Strap", root.transform, new Vector3(3.05f, -2.03f, -1.05f), new Vector3(1.32f, 0.1f, 0.07f), materials.DarkRubber);
            CreateBox(PersonalCargoContainerName + " Name Plate", root.transform, new Vector3(2.68f, -1.86f, -1.09f), new Vector3(0.32f, 0.16f, 0.04f), materials.Yellow);
        }

        private static void CreateWarningLabelSet(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(WarningLabelSetName, parent);
            CreateBox(WarningLabelSetName + " Cargo Warning", root.transform, new Vector3(-0.58f, -1.44f, -0.94f), new Vector3(0.34f, 0.18f, 0.035f), materials.Warning);
            CreateBox(WarningLabelSetName + " Cargo Mass Label", root.transform, new Vector3(-0.98f, -1.44f, -0.94f), new Vector3(0.34f, 0.18f, 0.035f), materials.Yellow);
            CreateBox(WarningLabelSetName + " Supply Caution", root.transform, new Vector3(11.22f, 0.32f, -14.92f), new Vector3(0.44f, 0.2f, 0.04f), materials.Warning);
            CreateBox(WarningLabelSetName + " Armory Hot Surface", root.transform, new Vector3(-13.35f, 1.05f, -11.02f), new Vector3(0.34f, 0.16f, 0.04f), materials.Yellow);
        }

        private static void CreateRepairPanelKit(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(RepairPanelKitName, parent);
            for (var i = 0; i < 4; i++)
            {
                CreateBox(RepairPanelKitName + " Plate " + (i + 1), root.transform, new Vector3(-3.35f + (i * 0.35f), -2.25f, 2.38f), new Vector3(0.28f, 0.05f, 0.42f), materials.Metal);
            }

            CreateBox(RepairPanelKitName + " Seal Tube", root.transform, new Vector3(-2.7f, -2.1f, 2.38f), new Vector3(0.62f, 0.08f, 0.12f), materials.DarkRubber);
            CreateBox(RepairPanelKitName + " Fastener Pack", root.transform, new Vector3(-3.72f, -2.07f, 2.38f), new Vector3(0.24f, 0.12f, 0.18f), materials.Yellow);
        }

        private static void CreateDamagedPanelKit(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(DamagedPanelKitName, parent);
            CreateBox(DamagedPanelKitName + " Bent Plate A", root.transform, new Vector3(-4.25f, -2.22f, 1.75f), new Vector3(0.56f, 0.06f, 0.48f), materials.Damaged, Quaternion.Euler(0f, 14f, 8f));
            CreateBox(DamagedPanelKitName + " Bent Plate B", root.transform, new Vector3(-3.62f, -2.18f, 1.72f), new Vector3(0.46f, 0.06f, 0.42f), materials.Damaged, Quaternion.Euler(0f, -18f, -6f));
            CreateBox(DamagedPanelKitName + " Exposed Cable A", root.transform, new Vector3(-3.98f, -2.08f, 1.38f), new Vector3(0.08f, 0.06f, 0.62f), materials.DarkRubber, Quaternion.Euler(0f, 32f, 0f));
            CreateBox(DamagedPanelKitName + " Exposed Cable B", root.transform, new Vector3(-3.68f, -2.02f, 1.39f), new Vector3(0.06f, 0.05f, 0.52f), materials.Warning, Quaternion.Euler(0f, -28f, 0f));
            CreateBox(DamagedPanelKitName + " Scorch Label", root.transform, new Vector3(-4.2f, -2.09f, 1.48f), new Vector3(0.25f, 0.04f, 0.18f), materials.Warning);
        }

        private static void CreateEscapePodVisual(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(EscapePodVisualName, parent);
            CreateCylinder(EscapePodVisualName + " Hull", root.transform, new Vector3(4.75f, -2.05f, -1.4f), new Vector3(0.38f, 0.78f, 0.38f), materials.Metal, Quaternion.Euler(0f, 0f, 90f));
            CreateBox(EscapePodVisualName + " Hatch", root.transform, new Vector3(4.75f, -1.75f, -1.38f), new Vector3(0.5f, 0.12f, 0.36f), materials.Cargo);
            CreateBox(EscapePodVisualName + " Damaged Rim", root.transform, new Vector3(4.05f, -2.05f, -1.4f), new Vector3(0.12f, 0.7f, 0.7f), materials.Damaged);
            CreateBox(EscapePodVisualName + " Discard Warning Stripe", root.transform, new Vector3(5.28f, -1.77f, -1.4f), new Vector3(0.12f, 0.46f, 0.08f), materials.Warning, Quaternion.Euler(0f, 0f, 22f));
        }

        private static void CreateSpecialEquipment(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(SpecialEquipmentRootName, parent);
            CreatePresenceDetector(root.transform, materials);
            CreateLightBlade(root.transform, materials);
            CreateElectricMine(root.transform, materials);
            CreateCorridorPurifierIcon(root.transform, materials);
        }

        private static void CreatePresenceDetector(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(PresenceDetectorPropName, parent);
            CreateBox(PresenceDetectorPropName + " Grip", root.transform, new Vector3(10.85f, 0.42f, -14.6f), new Vector3(0.16f, 0.48f, 0.16f), materials.DarkRubber);
            CreateBox(PresenceDetectorPropName + " Body", root.transform, new Vector3(10.85f, 0.84f, -14.6f), new Vector3(0.48f, 0.32f, 0.2f), materials.Metal);
            CreateBox(PresenceDetectorPropName + " Scan Screen", root.transform, new Vector3(10.85f, 0.9f, -14.73f), new Vector3(0.34f, 0.16f, 0.035f), materials.Screen);
            CreateCylinder(PresenceDetectorPropName + " Sensor Dish", root.transform, new Vector3(10.85f, 1.08f, -14.6f), new Vector3(0.22f, 0.04f, 0.22f), materials.Light, Quaternion.Euler(90f, 0f, 0f));
        }

        private static void CreateLightBlade(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(LightBladePropName, parent);
            CreateBox(LightBladePropName + " Hilt", root.transform, new Vector3(11.55f, 0.42f, -14.55f), new Vector3(0.14f, 0.5f, 0.14f), materials.DarkRubber, Quaternion.Euler(0f, 0f, -12f));
            CreateBox(LightBladePropName + " Guard", root.transform, new Vector3(11.49f, 0.68f, -14.55f), new Vector3(0.42f, 0.08f, 0.08f), materials.Metal, Quaternion.Euler(0f, 0f, -12f));
            CreateBox(LightBladePropName + " Blade Core", root.transform, new Vector3(11.3f, 1.18f, -14.55f), new Vector3(0.08f, 0.92f, 0.055f), materials.Light, Quaternion.Euler(0f, 0f, -12f));
            CreateBox(LightBladePropName + " Blade Tip", root.transform, new Vector3(11.18f, 1.66f, -14.55f), new Vector3(0.05f, 0.18f, 0.045f), materials.Light, Quaternion.Euler(0f, 0f, -12f));
        }

        private static void CreateElectricMine(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(ElectricMinePropName, parent);
            CreateCylinder(ElectricMinePropName + " Disc Body", root.transform, new Vector3(12.18f, 0.24f, -14.62f), new Vector3(0.32f, 0.08f, 0.32f), materials.Metal);
            CreateCylinder(ElectricMinePropName + " Charge Core", root.transform, new Vector3(12.18f, 0.34f, -14.62f), new Vector3(0.16f, 0.035f, 0.16f), materials.Light);
            for (var i = 0; i < 4; i++)
            {
                var angle = i * Mathf.PI * 0.5f;
                CreateBox(
                    ElectricMinePropName + " Contact Leg " + (i + 1),
                    root.transform,
                    new Vector3(12.18f + Mathf.Cos(angle) * 0.35f, 0.2f, -14.62f + Mathf.Sin(angle) * 0.35f),
                    new Vector3(0.26f, 0.05f, 0.06f),
                    materials.DarkRubber,
                    Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f));
            }
        }

        private static void CreateCorridorPurifierIcon(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(CorridorPurifierIconName, parent);
            CreateBox(CorridorPurifierIconName + " Wall Icon Plate", root.transform, new Vector3(12.95f, 1.12f, -14.92f), new Vector3(0.58f, 0.48f, 0.045f), materials.Metal);
            CreateBox(CorridorPurifierIconName + " Filter Bar", root.transform, new Vector3(12.95f, 1.2f, -14.96f), new Vector3(0.36f, 0.08f, 0.035f), materials.Light);
            CreateBox(CorridorPurifierIconName + " Nozzle Mark", root.transform, new Vector3(12.82f, 1.03f, -14.96f), new Vector3(0.12f, 0.18f, 0.035f), materials.Yellow);
            CreateBox(CorridorPurifierIconName + " Corridor Arrow", root.transform, new Vector3(13.1f, 1.02f, -14.96f), new Vector3(0.22f, 0.08f, 0.035f), materials.Warning, Quaternion.Euler(0f, 0f, -28f));
        }

        private static void CreateDiegeticTerminalShell(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(DiegeticTerminalShellName, parent);
            CreateBox(DiegeticTerminalShellName + " Pedestal", root.transform, new Vector3(2.15f, -1.95f, 2.78f), new Vector3(0.58f, 0.82f, 0.46f), materials.Metal);
            CreateBox(DiegeticTerminalShellName + " Angled Shell", root.transform, new Vector3(2.15f, -1.42f, 2.64f), new Vector3(1.15f, 0.42f, 0.32f), materials.Metal, Quaternion.Euler(-16f, 0f, 0f));
            CreateBox(DiegeticTerminalScreenBackingName, root.transform, new Vector3(2.15f, -1.29f, 2.45f), new Vector3(0.86f, 0.26f, 0.035f), materials.Screen, Quaternion.Euler(-16f, 0f, 0f));
            for (var i = 0; i < 6; i++)
            {
                CreateBox(
                    DiegeticTerminalButtonMeshName + " " + (i + 1),
                    root.transform,
                    new Vector3(1.78f + (i * 0.15f), -1.55f, 2.38f),
                    new Vector3(0.08f, 0.045f, 0.035f),
                    i % 3 == 0 ? materials.Warning : materials.Yellow,
                    Quaternion.Euler(-16f, 0f, 0f));
            }
        }

        private static void CreateFirstPersonEquipmentPreview(Stage3Materials materials)
        {
            var cameraObject = GameObject.Find("Player Camera");
            if (cameraObject == null)
            {
                throw new InvalidOperationException("Stage 3 first-person equipment preview requires Player Camera.");
            }

            var preview = new GameObject(FirstPersonPreviewRootName);
            preview.transform.SetParent(cameraObject.transform, false);
            preview.transform.localPosition = Vector3.zero;
            preview.transform.localRotation = Quaternion.identity;
            preview.transform.localScale = Vector3.one;

            var crowbar = CreateFirstPersonCrowbar(preview.transform, materials);
            var musket = CreateFirstPersonMusket(preview.transform, materials);
            var suitReadout = CreateProtectiveSuitReadout(preview.transform, materials);
            var deviceState = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            if (deviceState == null)
            {
                throw new InvalidOperationException("Stage 3 first-person equipment preview requires ShipDeviceInteractionState.");
            }

            var visualController = preview.AddComponent<FirstPersonEquipmentVisualController>();
            visualController.Configure(
                deviceState,
                crowbar,
                musket,
                suitReadout);
        }

        private static GameObject CreateFirstPersonCrowbar(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(CrowbarModelName, parent);
            root.transform.localPosition = new Vector3(0.44f, -0.54f, 1.06f);
            root.transform.localRotation = Quaternion.Euler(8f, -18f, -27f);
            root.transform.localScale = Vector3.one * 0.92f;

            CreateCrowbarContinuousBody(root.transform, materials.CrowbarSteel);
            return root;
        }

        private static GameObject CreateFirstPersonMusket(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(MusketModelName, parent);
            root.transform.localPosition = new Vector3(0.22f, -0.58f, 1.28f);
            root.transform.localRotation = Quaternion.Euler(3f, -7f, -5f);
            root.transform.localScale = Vector3.one * 0.48f;
            CreateLocalBox(MusketModelName + " Butt Stock", root.transform, new Vector3(-0.38f, -0.065f, 0f), new Vector3(0.14f, 0.15f, 0.055f), materials.Wood, Quaternion.Euler(0f, 0f, -18f));
            CreateLocalBox(MusketModelName + " Shoulder Stock", root.transform, new Vector3(-0.24f, -0.025f, 0f), new Vector3(0.24f, 0.08f, 0.046f), materials.Wood, Quaternion.Euler(0f, 0f, -7f));
            CreateLocalBox(MusketModelName + " Forearm Wood", root.transform, new Vector3(0.04f, -0.02f, 0f), new Vector3(0.42f, 0.036f, 0.038f), materials.Wood);
            CreateLocalCylinder(MusketModelName + " Long Barrel", root.transform, new Vector3(0.16f, 0.023f, 0f), new Vector3(0.013f, 0.46f, 0.013f), materials.Metal, Quaternion.Euler(0f, 0f, 90f));
            CreateLocalCylinder(MusketModelName + " Muzzle Ring", root.transform, new Vector3(0.62f, 0.023f, 0f), new Vector3(0.018f, 0.034f, 0.018f), materials.Metal, Quaternion.Euler(0f, 0f, 90f));
            CreateLocalCylinder(MusketModelName + " Ramrod", root.transform, new Vector3(0.17f, -0.022f, 0f), new Vector3(0.006f, 0.42f, 0.006f), materials.Metal, Quaternion.Euler(0f, 0f, 90f));
            CreateLocalBox(MusketModelName + " Barrel Band Rear", root.transform, new Vector3(-0.03f, 0f, 0f), new Vector3(0.034f, 0.09f, 0.052f), materials.Metal);
            CreateLocalBox(MusketModelName + " Barrel Band Front", root.transform, new Vector3(0.35f, 0f, 0f), new Vector3(0.034f, 0.09f, 0.052f), materials.Metal);
            CreateLocalBox(MusketModelName + " Lock Plate", root.transform, new Vector3(-0.13f, 0.018f, -0.034f), new Vector3(0.12f, 0.045f, 0.014f), materials.Metal);
            CreateLocalBox(MusketModelName + " Hammer", root.transform, new Vector3(-0.16f, 0.07f, -0.04f), new Vector3(0.035f, 0.09f, 0.014f), materials.Metal, Quaternion.Euler(0f, 0f, -28f));
            CreateLocalBox(MusketModelName + " Trigger Guard", root.transform, new Vector3(-0.13f, -0.08f, -0.02f), new Vector3(0.09f, 0.024f, 0.018f), materials.Metal, Quaternion.Euler(0f, 0f, 8f));
            CreateLocalBox(MusketModelName + " Trigger", root.transform, new Vector3(-0.12f, -0.105f, -0.018f), new Vector3(0.025f, 0.065f, 0.014f), materials.DarkRubber, Quaternion.Euler(0f, 0f, -18f));
            root.SetActive(false);
            return root;
        }

        private static GameObject CreateProtectiveSuitReadout(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(ProtectiveSuitReadoutName, parent);
            root.transform.localPosition = new Vector3(-0.5f, -0.34f, 0.96f);
            root.transform.localRotation = Quaternion.Euler(4f, 18f, 0f);
            root.transform.localScale = Vector3.one * 0.7f;
            CreateLocalBox(ProtectiveSuitReadoutName + " Wrist Plate", root.transform, Vector3.zero, new Vector3(0.22f, 0.08f, 0.035f), materials.Metal);
            CreateLocalBox(ProtectiveSuitReadoutName + " Suit Screen", root.transform, new Vector3(0f, 0.015f, -0.024f), new Vector3(0.16f, 0.036f, 0.012f), materials.Screen);
            CreateLocalBox(ProtectiveSuitReadoutName + " Shield Bar", root.transform, new Vector3(-0.04f, 0.043f, -0.026f), new Vector3(0.1f, 0.012f, 0.01f), materials.Light);
            return root;
        }

        private static GameObject CreateChildRoot(string name, Transform parent)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            return root;
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            return CreateBox(name, parent, position, scale, material, Quaternion.identity);
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 position, Vector3 scale, Material material, Quaternion rotation)
        {
            var box = CreatePrimitive(name, parent, PrimitiveType.Cube, material);
            box.transform.position = position;
            box.transform.rotation = rotation;
            box.transform.localScale = scale;
            return box;
        }

        private static GameObject CreateCylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            return CreateCylinder(name, parent, position, scale, material, Quaternion.identity);
        }

        private static GameObject CreateCylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material, Quaternion rotation)
        {
            var cylinder = CreatePrimitive(name, parent, PrimitiveType.Cylinder, material);
            cylinder.transform.position = position;
            cylinder.transform.rotation = rotation;
            cylinder.transform.localScale = scale;
            return cylinder;
        }

        private static GameObject CreateLocalBox(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material)
        {
            return CreateLocalBox(name, parent, localPosition, scale, material, Quaternion.identity);
        }

        private static GameObject CreateLocalBox(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material, Quaternion rotation)
        {
            var box = CreatePrimitive(name, parent, PrimitiveType.Cube, material);
            box.transform.localPosition = localPosition;
            box.transform.localRotation = rotation;
            box.transform.localScale = scale;
            return box;
        }

        private static GameObject CreateLocalCylinder(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material)
        {
            return CreateLocalCylinder(name, parent, localPosition, scale, material, Quaternion.identity);
        }

        private static GameObject CreateLocalCylinder(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material, Quaternion rotation)
        {
            var cylinder = CreatePrimitive(name, parent, PrimitiveType.Cylinder, material);
            cylinder.transform.localPosition = localPosition;
            cylinder.transform.localRotation = rotation;
            cylinder.transform.localScale = scale;
            return cylinder;
        }

        private static GameObject CreatePrimitive(string name, Transform parent, PrimitiveType primitiveType, Material material)
        {
            var gameObject = GameObject.CreatePrimitive(primitiveType);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            var renderer = gameObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            var collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            return gameObject;
        }

        private static GameObject CreateCrowbarContinuousBody(Transform parent, Material material)
        {
            const int radialSegments = 24;
            var sections = CreateCrowbarSections();

            var mesh = BuildCrowbarTubeMesh(sections, radialSegments);
            var body = new GameObject(CrowbarContinuousBodyName);
            body.transform.SetParent(parent, false);

            var filter = body.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            var renderer = body.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return body;
        }

        private static List<CrowbarSection> CreateCrowbarSections()
        {
            var sections = new List<CrowbarSection>();

            AppendCrowbarBezierSections(
                sections,
                new Vector3(0.080f, -0.560f, 0f),
                new Vector3(0.052f, -0.548f, 0f),
                new Vector3(0.018f, -0.486f, 0f),
                new Vector3(0.004f, -0.420f, 0f),
                8,
                false,
                0.012f,
                0.028f,
                0.008f,
                0.026f);

            AppendCrowbarLineSections(
                sections,
                new Vector3(0.004f, -0.420f, 0f),
                new Vector3(-0.002f, 0.390f, 0f),
                22,
                true,
                0.028f,
                0.027f,
                0.026f,
                0.027f);

            AppendCrowbarBezierSections(
                sections,
                new Vector3(-0.002f, 0.390f, 0f),
                new Vector3(-0.010f, 0.456f, 0f),
                new Vector3(-0.064f, 0.502f, 0f),
                new Vector3(-0.130f, 0.504f, 0f),
                14,
                true,
                0.027f,
                0.024f,
                0.027f,
                0.021f);

            AppendCrowbarBezierSections(
                sections,
                new Vector3(-0.130f, 0.504f, 0f),
                new Vector3(-0.170f, 0.504f, 0f),
                new Vector3(-0.204f, 0.480f, 0f),
                new Vector3(-0.218f, 0.444f, 0f),
                10,
                true,
                0.024f,
                0.010f,
                0.021f,
                0.008f);

            return sections;
        }

        private static void AppendCrowbarLineSections(
            List<CrowbarSection> sections,
            Vector3 start,
            Vector3 end,
            int samples,
            bool skipFirst,
            float startInPlaneRadius,
            float endInPlaneRadius,
            float startDepthRadius,
            float endDepthRadius)
        {
            for (var i = skipFirst ? 1 : 0; i <= samples; i++)
            {
                var t = i / (float)samples;
                sections.Add(
                    new CrowbarSection(
                        Vector3.Lerp(start, end, t),
                        Mathf.Lerp(startInPlaneRadius, endInPlaneRadius, t),
                        Mathf.Lerp(startDepthRadius, endDepthRadius, t)));
            }
        }

        private static void AppendCrowbarBezierSections(
            List<CrowbarSection> sections,
            Vector3 start,
            Vector3 controlA,
            Vector3 controlB,
            Vector3 end,
            int samples,
            bool skipFirst,
            float startInPlaneRadius,
            float endInPlaneRadius,
            float startDepthRadius,
            float endDepthRadius)
        {
            for (var i = skipFirst ? 1 : 0; i <= samples; i++)
            {
                var t = i / (float)samples;
                sections.Add(
                    new CrowbarSection(
                        EvaluateCubicBezier(start, controlA, controlB, end, t),
                        Mathf.Lerp(startInPlaneRadius, endInPlaneRadius, t),
                        Mathf.Lerp(startDepthRadius, endDepthRadius, t)));
            }
        }

        private static Vector3 EvaluateCubicBezier(Vector3 start, Vector3 controlA, Vector3 controlB, Vector3 end, float t)
        {
            var inverse = 1f - t;
            return
                (inverse * inverse * inverse * start) +
                (3f * inverse * inverse * t * controlA) +
                (3f * inverse * t * t * controlB) +
                (t * t * t * end);
        }

        private static Mesh BuildCrowbarTubeMesh(IReadOnlyList<CrowbarSection> sections, int radialSegments)
        {
            var vertices = new List<Vector3>(sections.Count * radialSegments + 2);
            var normals = new List<Vector3>(sections.Count * radialSegments + 2);
            var triangles = new List<int>((sections.Count - 1) * radialSegments * 6);

            for (var sectionIndex = 0; sectionIndex < sections.Count; sectionIndex++)
            {
                var tangent = GetCrowbarSectionTangent(sections, sectionIndex);
                var inPlaneNormal = new Vector3(-tangent.y, tangent.x, 0f).normalized;
                var depthNormal = Vector3.forward;

                for (var radialIndex = 0; radialIndex < radialSegments; radialIndex++)
                {
                    var angle = Mathf.PI * 2f * radialIndex / radialSegments;
                    var inPlaneWeight = Mathf.Cos(angle);
                    var depthWeight = Mathf.Sin(angle);
                    var radialOffset =
                        (inPlaneNormal * inPlaneWeight * sections[sectionIndex].InPlaneRadius) +
                        (depthNormal * depthWeight * sections[sectionIndex].DepthRadius);

                    vertices.Add(sections[sectionIndex].Center + radialOffset);
                    normals.Add(((inPlaneNormal * inPlaneWeight) + (depthNormal * depthWeight)).normalized);
                }
            }

            for (var sectionIndex = 0; sectionIndex < sections.Count - 1; sectionIndex++)
            {
                var currentRing = sectionIndex * radialSegments;
                var nextRing = (sectionIndex + 1) * radialSegments;
                for (var radialIndex = 0; radialIndex < radialSegments; radialIndex++)
                {
                    var nextRadialIndex = (radialIndex + 1) % radialSegments;
                    var a = currentRing + radialIndex;
                    var b = currentRing + nextRadialIndex;
                    var c = nextRing + radialIndex;
                    var d = nextRing + nextRadialIndex;

                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(b);
                    triangles.Add(b);
                    triangles.Add(c);
                    triangles.Add(d);
                }
            }

            var startCenterIndex = vertices.Count;
            vertices.Add(sections[0].Center);
            normals.Add(-GetCrowbarSectionTangent(sections, 0));
            for (var radialIndex = 0; radialIndex < radialSegments; radialIndex++)
            {
                var nextRadialIndex = (radialIndex + 1) % radialSegments;
                triangles.Add(startCenterIndex);
                triangles.Add(nextRadialIndex);
                triangles.Add(radialIndex);
            }

            var endCenterIndex = vertices.Count;
            var endRing = (sections.Count - 1) * radialSegments;
            vertices.Add(sections[sections.Count - 1].Center);
            normals.Add(GetCrowbarSectionTangent(sections, sections.Count - 1));
            for (var radialIndex = 0; radialIndex < radialSegments; radialIndex++)
            {
                var nextRadialIndex = (radialIndex + 1) % radialSegments;
                triangles.Add(endCenterIndex);
                triangles.Add(endRing + radialIndex);
                triangles.Add(endRing + nextRadialIndex);
            }

            var mesh = new Mesh
            {
                name = CrowbarContinuousBodyName + " Mesh",
            };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 GetCrowbarSectionTangent(IReadOnlyList<CrowbarSection> sections, int index)
        {
            if (index == 0)
            {
                return (sections[1].Center - sections[0].Center).normalized;
            }

            if (index == sections.Count - 1)
            {
                return (sections[index].Center - sections[index - 1].Center).normalized;
            }

            return (sections[index + 1].Center - sections[index - 1].Center).normalized;
        }

        private static Material EnsureMaterial(string path, Color color)
        {
            return EnsureMaterial(path, color, false);
        }

        private static Material EnsureMaterial(string path, Color color, bool emissive)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader != null && material.shader != shader)
                {
                    material.shader = shader;
                }
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", color * 0.7f);
                }
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void DeleteGeneratedObject(string objectName)
        {
            var existing = GameObject.Find(objectName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
                return;
            }

            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var child = FindChildRecursive(roots[i].transform, objectName);
                if (child == null)
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(child.gameObject);
                return;
            }
        }

        private static Transform FindChildRecursive(Transform parent, string objectName)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == objectName)
                {
                    return child;
                }

                var nested = FindChildRecursive(child, objectName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private readonly struct Stage3Materials
        {
            public Stage3Materials(
                Material metal,
                Material darkRubber,
                Material screen,
                Material warning,
                Material yellow,
                Material wood,
                Material crowbarSteel,
                Material damaged,
                Material light,
                Material cargo)
            {
                Metal = metal;
                DarkRubber = darkRubber;
                Screen = screen;
                Warning = warning;
                Yellow = yellow;
                Wood = wood;
                CrowbarSteel = crowbarSteel;
                Damaged = damaged;
                Light = light;
                Cargo = cargo;
            }

            public Material Metal { get; }

            public Material DarkRubber { get; }

            public Material Screen { get; }

            public Material Warning { get; }

            public Material Yellow { get; }

            public Material Wood { get; }

            public Material CrowbarSteel { get; }

            public Material Damaged { get; }

            public Material Light { get; }

            public Material Cargo { get; }
        }

        private readonly struct CrowbarSection
        {
            public CrowbarSection(Vector3 center, float inPlaneRadius, float depthRadius)
            {
                Center = center;
                InPlaneRadius = inPlaneRadius;
                DepthRadius = depthRadius;
            }

            public Vector3 Center { get; }

            public float InPlaneRadius { get; }

            public float DepthRadius { get; }
        }
    }
}
