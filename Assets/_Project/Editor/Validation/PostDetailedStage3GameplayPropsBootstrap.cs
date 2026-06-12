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
        public const string CrowbarGripWrapName = CrowbarModelName + " Two Hand Grip Wrap";
        public const string CrowbarLowerGloveName = CrowbarModelName + " Lower Gloved Hand";
        public const string CrowbarUpperGloveName = CrowbarModelName + " Upper Gloved Hand";
        public const string MusketModelName = "Stage 3 First Person Musket Model";
        public const string ProtectiveSuitReadoutName = "Stage 3 Protective Suit Readout";
        public const string CockpitHelmPropName = "Stage 3 Cockpit Helm Prop";
        public const string CockpitStatusScreensName = "Stage 3 Cockpit Status Screens";
        public const string ControlRoomCctvTerminalName = "Stage 3 Control Room CCTV Terminal";
        public const string ControlRoomCctvMainScreenFrameName = ControlRoomCctvTerminalName + " Single Large Screen Frame";
        public const string ControlRoomCctvMainScreenGlowName = ControlRoomCctvTerminalName + " Single Large Screen Glow";
        public const string ControlRoomCctvHorizontalScreenName = ControlRoomCctvTerminalName + " Upper Left Horizontal Screen";
        public const string ControlRoomCctvVerticalScreenName = ControlRoomCctvTerminalName + " Right Vertical Screen";
        public const string ControlRoomCctvButtonAName = ControlRoomCctvTerminalName + " A Button";
        public const string ControlRoomCctvButtonDName = ControlRoomCctvTerminalName + " D Button";
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
        public const string RoomDressingRootName = "Stage 3 Art Sample Room Dressings";
        public const string CockpitDressingName = "Stage 3 Cockpit Art Sample Dressing";
        public const string ControlRoomDressingName = "Stage 3 Control Room Art Sample Dressing";
        public const string EngineRoomDressingName = "Stage 3 Engine Room Art Sample Dressing";
        public const string SupplyRoomDressingName = "Stage 3 Supply Room Art Sample Dressing";
        public const string CargoHoldDressingName = "Stage 3 Cargo Hold Art Sample Dressing";
        public const string ArmoryDressingName = "Stage 3 Armory Art Sample Dressing";
        public const string CargoStartCorridorDressingName = "Stage 3 Cargo Start Corridor Art Sample Dressing";

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
        public const string WarmLightMaterialPath = MaterialDirectory + "/Stage3WarmLight_Overhead.mat";
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
            Stage3BlenderReviewAssetBuilder.EnsureAssets();
            var stageRoot = new GameObject(Stage3RootName);
            CreateArtSampleRoomDressings(stageRoot.transform, materials);
            CreateShipDevices(stageRoot.transform, materials);
            CreateCargoProps(stageRoot.transform, materials);
            CreateDiegeticTerminalShell(stageRoot.transform, materials);
            CreateFirstPersonEquipmentPreview(materials);
            HideLegacyGrayboxPresentationElements();

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
                EnsureMaterial(MetalMaterialPath, new Color(0.075f, 0.076f, 0.068f, 1f)),
                EnsureMaterial(DarkRubberMaterialPath, new Color(0.014f, 0.014f, 0.013f, 1f)),
                EnsureMaterial(ScreenMaterialPath, new Color(0.035f, 0.46f, 0.23f, 1f), true),
                EnsureMaterial(WarningMaterialPath, new Color(0.55f, 0.04f, 0.028f, 1f)),
                EnsureMaterial(YellowMaterialPath, new Color(0.68f, 0.49f, 0.075f, 1f)),
                EnsureMaterial(WoodMaterialPath, new Color(0.33f, 0.20f, 0.12f, 1f)),
                EnsureMaterial(CrowbarSteelMaterialPath, new Color(0.48f, 0.49f, 0.44f, 1f)),
                EnsureMaterial(DamagedMaterialPath, new Color(0.055f, 0.047f, 0.038f, 1f)),
                EnsureMaterial(LightMaterialPath, new Color(0.12f, 0.62f, 0.46f, 1f), true),
                EnsureMaterial(WarmLightMaterialPath, new Color(0.94f, 0.72f, 0.42f, 1f), true),
                EnsureMaterial(CargoMaterialPath, new Color(0.11f, 0.125f, 0.12f, 1f)));
        }

        private static void CreateArtSampleRoomDressings(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(RoomDressingRootName, parent);
            CreateCargoStartCorridorDressing(root.transform, materials);
            CreateCargoHoldDressing(root.transform, materials);
            CreateCockpitDressing(root.transform, materials);
            CreateControlRoomDressing(root.transform, materials);
            CreateEngineRoomDressing(root.transform, materials);
            CreateSupplyRoomDressing(root.transform, materials);
            CreateArmoryDressing(root.transform, materials);
        }

        private static void CreateCargoStartCorridorDressing(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(CargoStartCorridorDressingName, parent);
            for (var i = 0; i < 8; i++)
            {
                var z = -5.25f + (i * 1.48f);
                CreateBox(CargoStartCorridorDressingName + " Deck Plate " + (i + 1), root.transform, new Vector3(0f, -2.925f, z), new Vector3(2.35f, 0.045f, 1.05f), i % 2 == 0 ? materials.Metal : materials.Damaged);
                CreateBox(CargoStartCorridorDressingName + " Left Rib " + (i + 1), root.transform, new Vector3(-1.32f, -2.82f, z), new Vector3(0.09f, 0.24f, 1.16f), materials.DarkRubber);
                CreateBox(CargoStartCorridorDressingName + " Right Rib " + (i + 1), root.transform, new Vector3(1.32f, -2.82f, z), new Vector3(0.09f, 0.24f, 1.16f), materials.DarkRubber);
            }

            for (var side = -1; side <= 1; side += 2)
            {
                for (var i = 0; i < 6; i++)
                {
                    var z = -4.65f + (i * 1.55f);
                    CreateBox(CargoStartCorridorDressingName + " Side Wall Panel " + side + " " + (i + 1), root.transform, new Vector3(side * 5.82f, -1.55f, z), new Vector3(0.08f, 1.45f, 1.12f), materials.Metal);
                    CreateBox(CargoStartCorridorDressingName + " Side Wall Brace " + side + " " + (i + 1), root.transform, new Vector3(side * 5.72f, -0.78f, z), new Vector3(0.12f, 0.08f, 1.24f), materials.DarkRubber);
                }
            }

            for (var i = 0; i < 5; i++)
            {
                var x = -1.55f + (i * 0.78f);
                CreateBox(CargoStartCorridorDressingName + " Overhead Cable " + (i + 1), root.transform, new Vector3(x, -0.34f, -0.2f), new Vector3(0.055f, 0.055f, 8.7f), i == 2 ? materials.Warning : materials.DarkRubber);
            }

            CreateLightStrip(CargoStartCorridorDressingName + " Start Ceiling Light", root.transform, new Vector3(0f, -0.42f, -4.25f), new Vector3(1.32f, 0.045f, 0.28f), materials);
            CreateLightStrip(CargoStartCorridorDressingName + " Forward Ceiling Light", root.transform, new Vector3(0f, -0.42f, 2.1f), new Vector3(1.32f, 0.045f, 0.28f), materials);

            for (var side = -1; side <= 1; side += 2)
            {
                for (var i = 0; i < 7; i++)
                {
                    var z = -5.55f + (i * 1.42f);
                    CreateBox(CargoStartCorridorDressingName + " Close Corridor Wall Plate " + side + " " + (i + 1), root.transform, new Vector3(side * 2.64f, -1.58f, z), new Vector3(0.065f, 1.34f, 1.02f), i % 3 == 0 ? materials.Damaged : materials.Metal);
                    CreateBox(CargoStartCorridorDressingName + " Close Corridor Inner Gasket " + side + " " + (i + 1), root.transform, new Vector3(side * 2.60f, -1.58f, z), new Vector3(0.04f, 0.82f, 0.66f), materials.DarkRubber);
                    CreateBox(CargoStartCorridorDressingName + " Close Corridor Lower Rail " + side + " " + (i + 1), root.transform, new Vector3(side * 2.56f, -2.34f, z), new Vector3(0.055f, 0.10f, 1.12f), materials.CrowbarSteel);
                }
            }

            for (var i = 0; i < 8; i++)
            {
                var z = -5.85f + (i * 1.18f);
                CreateBox(CargoStartCorridorDressingName + " Center Deck Black Inset " + (i + 1), root.transform, new Vector3(0f, -2.832f, z), new Vector3(1.22f, 0.026f, 0.72f), materials.DarkRubber);
                CreateBox(CargoStartCorridorDressingName + " Left Deck Warning Slash " + (i + 1), root.transform, new Vector3(-1.06f, -2.805f, z + 0.14f), new Vector3(0.42f, 0.025f, 0.06f), i % 2 == 0 ? materials.Yellow : materials.Warning, Quaternion.Euler(0f, 26f, 0f));
                CreateBox(CargoStartCorridorDressingName + " Right Deck Warning Slash " + (i + 1), root.transform, new Vector3(1.06f, -2.805f, z - 0.14f), new Vector3(0.42f, 0.025f, 0.06f), i % 2 == 0 ? materials.Yellow : materials.Warning, Quaternion.Euler(0f, -26f, 0f));
            }

            for (var i = 0; i < 5; i++)
            {
                var z = -5.2f + (i * 1.85f);
                CreateBox(CargoStartCorridorDressingName + " Low Side Cargo Silhouette " + (i + 1), root.transform, new Vector3(i % 2 == 0 ? -1.82f : 1.82f, -2.35f, z), new Vector3(0.78f, 0.62f, 0.84f), materials.Cargo);
                CreateBox(CargoStartCorridorDressingName + " Low Side Cargo Strap " + (i + 1), root.transform, new Vector3(i % 2 == 0 ? -1.82f : 1.82f, -2.16f, z - 0.43f), new Vector3(0.82f, 0.08f, 0.045f), materials.DarkRubber);
            }
        }

        private static void CreateCargoHoldDressing(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(CargoHoldDressingName, parent);
            CreateFloorGrid(CargoHoldDressingName, root.transform, new Vector3(0f, -2.915f, 0f), 4, 4, 2.25f, 2.25f, materials);
            CreateCeilingGrid(CargoHoldDressingName, root.transform, new Vector3(0f, -0.24f, 0f), 4, 4, 2.35f, 2.35f, materials);

            for (var side = -1; side <= 1; side += 2)
            {
                for (var i = 0; i < 4; i++)
                {
                    var z = -3.9f + (i * 2.25f);
                    CreateBox(CargoHoldDressingName + " Wall Crate Rail " + side + " " + (i + 1), root.transform, new Vector3(side * 5.72f, -1.48f, z), new Vector3(0.08f, 1.46f, 1.72f), materials.Metal);
                    CreateBox(CargoHoldDressingName + " Wall Dark Insert " + side + " " + (i + 1), root.transform, new Vector3(side * 5.66f, -1.46f, z), new Vector3(0.055f, 1.06f, 1.28f), materials.DarkRubber);
                    CreateBox(CargoHoldDressingName + " Wall Inner Scratched Plate " + side + " " + (i + 1), root.transform, new Vector3(side * 5.61f, -1.43f, z), new Vector3(0.035f, 0.72f, 0.92f), materials.Damaged);
                }
            }

            CreateBox(CargoHoldDressingName + " Red Floor Route Stripe", root.transform, new Vector3(0f, -2.86f, 2.72f), new Vector3(6.2f, 0.035f, 0.14f), materials.Warning);
            CreateBox(CargoHoldDressingName + " Yellow Black Floor Warning", root.transform, new Vector3(-2.35f, -2.84f, -2.25f), new Vector3(2.4f, 0.035f, 0.24f), materials.Yellow, Quaternion.Euler(0f, 23f, 0f));
            CreateBox(CargoHoldDressingName + " Ceiling Truss Front", root.transform, new Vector3(0f, -0.34f, -3.7f), new Vector3(7.8f, 0.12f, 0.14f), materials.DarkRubber);
            CreateBox(CargoHoldDressingName + " Ceiling Truss Rear", root.transform, new Vector3(0f, -0.34f, 3.7f), new Vector3(7.8f, 0.12f, 0.14f), materials.DarkRubber);
            CreateBox(CargoHoldDressingName + " Overhead Dark Cable Tray A", root.transform, new Vector3(-1.85f, -0.48f, 0f), new Vector3(0.18f, 0.12f, 7.8f), materials.DarkRubber);
            CreateBox(CargoHoldDressingName + " Overhead Dark Cable Tray B", root.transform, new Vector3(1.85f, -0.48f, 0f), new Vector3(0.18f, 0.12f, 7.8f), materials.DarkRubber);
            CreateBox(CargoHoldDressingName + " Rear Wall Pipe Bundle A", root.transform, new Vector3(-2.55f, -0.9f, 4.36f), new Vector3(0.08f, 0.08f, 2.3f), materials.DarkRubber, Quaternion.Euler(0f, 0f, 90f));
            CreateBox(CargoHoldDressingName + " Rear Wall Pipe Bundle B", root.transform, new Vector3(1.9f, -0.92f, 4.36f), new Vector3(0.08f, 0.08f, 2.15f), materials.DarkRubber, Quaternion.Euler(0f, 0f, 90f));
            CreateLightStrip(CargoHoldDressingName + " Cargo Hold Strip Light", root.transform, new Vector3(0f, -0.42f, 0f), new Vector3(1.55f, 0.045f, 0.32f), materials);

            for (var i = 0; i < 3; i++)
            {
                CreateBox(CargoHoldDressingName + " Side Storage Crate " + (i + 1), root.transform, new Vector3(-4.45f, -2.48f, -1.65f + (i * 1.25f)), new Vector3(1.12f, 0.86f, 0.9f), materials.Cargo);
                CreateBox(CargoHoldDressingName + " Side Storage Strap " + (i + 1), root.transform, new Vector3(-4.45f, -2.48f, -2.11f + (i * 1.25f)), new Vector3(1.18f, 0.11f, 0.06f), materials.DarkRubber);
                CreateBox(CargoHoldDressingName + " Right Stack Crate " + (i + 1), root.transform, new Vector3(3.85f, -2.56f, -1.2f + (i * 1.35f)), new Vector3(1.08f, 0.68f, 0.88f), i == 1 ? materials.Metal : materials.Cargo);
            }

            for (var i = 0; i < 5; i++)
            {
                var x = -2.4f + (i * 1.2f);
                CreateBox(CargoHoldDressingName + " Foreground Diamond Plate " + (i + 1), root.transform, new Vector3(x, -2.795f, -4.15f), new Vector3(0.92f, 0.028f, 0.72f), i % 2 == 0 ? materials.Metal : materials.Damaged);
                CreateBox(CargoHoldDressingName + " Foreground Plate Recess " + (i + 1), root.transform, new Vector3(x, -2.765f, -4.15f), new Vector3(0.52f, 0.018f, 0.42f), materials.DarkRubber);
            }

            for (var i = 0; i < 7; i++)
            {
                var x = -3.05f + (i * 1.02f);
                CreateBox(CargoHoldDressingName + " Front Hazard Slash " + (i + 1), root.transform, new Vector3(x, -2.755f, -3.52f), new Vector3(0.48f, 0.024f, 0.07f), i % 2 == 0 ? materials.Yellow : materials.Warning, Quaternion.Euler(0f, 25f, 0f));
            }

            CreateBox(CargoHoldDressingName + " Art Sample Main Cargo Face", root.transform, new Vector3(0f, -2.21f, -0.92f), new Vector3(2.64f, 1.12f, 0.14f), materials.Cargo);
            CreateBox(CargoHoldDressingName + " Art Sample Main Cargo Top Lid", root.transform, new Vector3(0f, -1.62f, -0.58f), new Vector3(2.72f, 0.14f, 0.86f), materials.Metal);
            CreateBox(CargoHoldDressingName + " Art Sample Main Cargo Left Strap", root.transform, new Vector3(-0.92f, -2.08f, -1.01f), new Vector3(0.12f, 1.16f, 0.065f), materials.DarkRubber);
            CreateBox(CargoHoldDressingName + " Art Sample Main Cargo Right Strap", root.transform, new Vector3(0.92f, -2.08f, -1.01f), new Vector3(0.12f, 1.16f, 0.065f), materials.DarkRubber);
            CreateBox(CargoHoldDressingName + " Art Sample Main Cargo Red Latch", root.transform, new Vector3(0f, -2.33f, -1.08f), new Vector3(0.32f, 0.32f, 0.055f), materials.Warning);
            CreateBox(CargoHoldDressingName + " Art Sample Main Cargo Yellow Nameplate", root.transform, new Vector3(-0.72f, -1.82f, -1.08f), new Vector3(0.42f, 0.16f, 0.055f), materials.Yellow);

            CreateBox(CargoHoldDressingName + " Right Pedestal Terminal Column", root.transform, new Vector3(3.32f, -2.22f, -1.72f), new Vector3(0.46f, 1.16f, 0.42f), materials.Metal);
            CreateBox(CargoHoldDressingName + " Right Pedestal Terminal Head", root.transform, new Vector3(3.32f, -1.52f, -1.92f), new Vector3(0.82f, 0.44f, 0.18f), materials.Metal, Quaternion.Euler(-12f, 0f, 0f));
            CreateBox(CargoHoldDressingName + " Right Pedestal Terminal Screen", root.transform, new Vector3(3.32f, -1.46f, -2.025f), new Vector3(0.55f, 0.25f, 0.038f), materials.Screen, Quaternion.Euler(-12f, 0f, 0f));
            for (var i = 0; i < 4; i++)
            {
                CreateBox(CargoHoldDressingName + " Right Pedestal Terminal Button " + (i + 1), root.transform, new Vector3(3.06f + (i * 0.17f), -1.73f, -2.03f), new Vector3(0.105f, 0.065f, 0.04f), i == 0 ? materials.Warning : i == 1 ? materials.Yellow : materials.Metal, Quaternion.Euler(-12f, 0f, 0f));
            }

            for (var i = 0; i < 5; i++)
            {
                CreateBox(CargoHoldDressingName + " Camera Side Pipe Run " + (i + 1), root.transform, new Vector3(-5.58f, -0.82f - (i * 0.14f), -3.2f), new Vector3(0.06f, 0.055f, 2.7f), i == 2 ? materials.Warning : materials.DarkRubber);
                CreateBox(CargoHoldDressingName + " Far Side Pipe Run " + (i + 1), root.transform, new Vector3(5.58f, -0.82f - (i * 0.14f), -2.2f), new Vector3(0.06f, 0.055f, 2.4f), materials.DarkRubber);
            }
        }

        private static void CreateCockpitDressing(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(CockpitDressingName, parent);
            CreateFloorGrid(CockpitDressingName, root.transform, new Vector3(0f, 0.035f, 18f), 4, 3, 1.85f, 1.55f, materials);
            CreateCeilingGrid(CockpitDressingName, root.transform, new Vector3(0f, 2.72f, 18f), 4, 3, 1.85f, 1.55f, materials);

            for (var i = 0; i < 5; i++)
            {
                var x = -3.6f + (i * 1.8f);
                CreateBox(CockpitDressingName + " Forward Window Armor Segment " + (i + 1), root.transform, new Vector3(x, 2.42f, 22.04f), new Vector3(1.18f, 0.2f, 0.18f), materials.Metal);
                CreateBox(CockpitDressingName + " Forward Lower Gasket " + (i + 1), root.transform, new Vector3(x, 0.36f, 22.02f), new Vector3(1.18f, 0.12f, 0.16f), materials.DarkRubber);
            }

            CreateBox(CockpitDressingName + " Left Console Bank", root.transform, new Vector3(-2.82f, 0.72f, 19.2f), new Vector3(2.05f, 0.48f, 0.74f), materials.Metal, Quaternion.Euler(0f, 9f, 0f));
            CreateBox(CockpitDressingName + " Right Console Bank", root.transform, new Vector3(2.82f, 0.72f, 19.2f), new Vector3(2.05f, 0.48f, 0.74f), materials.Metal, Quaternion.Euler(0f, -9f, 0f));
            for (var side = -1; side <= 1; side += 2)
            {
                for (var i = 0; i < 3; i++)
                {
                    CreateScreenInset(CockpitDressingName + " Side CRT " + side + " " + (i + 1), root.transform, new Vector3(side * (2.18f + (i * 0.52f)), 1.03f, 18.78f), new Vector3(0.38f, 0.04f, 0.24f), materials);
                }
            }

            for (var i = 0; i < 4; i++)
            {
                CreateBox(CockpitDressingName + " Rear Wall Pipe " + (i + 1), root.transform, new Vector3(-3.2f + (i * 0.36f), 1.92f, 14.08f), new Vector3(0.08f, 0.08f, 2.15f), materials.DarkRubber, Quaternion.Euler(0f, 0f, 90f));
            }

            CreateLightStrip(CockpitDressingName + " Forward Overhead Light", root.transform, new Vector3(0f, 2.58f, 19.1f), new Vector3(1.5f, 0.045f, 0.32f), materials);
        }

        private static void CreateControlRoomDressing(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(ControlRoomDressingName, parent);
            CreateFloorGrid(ControlRoomDressingName, root.transform, new Vector3(14f, 0.035f, 18f), 3, 3, 1.85f, 1.85f, materials);
            CreateCeilingGrid(ControlRoomDressingName, root.transform, new Vector3(14f, 2.72f, 18f), 3, 3, 1.85f, 1.85f, materials);

            for (var i = 0; i < 5; i++)
            {
                var x = 10.72f + (i * 1.62f);
                CreateBox(ControlRoomDressingName + " Back Wall Panel " + (i + 1), root.transform, new Vector3(x, 1.46f, 21.94f), new Vector3(1.18f, 1.72f, 0.075f), materials.Metal);
                CreateBox(ControlRoomDressingName + " Back Wall Lower Seam " + (i + 1), root.transform, new Vector3(x, 0.48f, 21.88f), new Vector3(1.08f, 0.08f, 0.08f), materials.DarkRubber);
            }

            for (var i = 0; i < 5; i++)
            {
                CreateBox(ControlRoomDressingName + " Upper Cable Rail " + (i + 1), root.transform, new Vector3(14.1f, 2.38f - (i * 0.115f), 21.82f), new Vector3(6.8f, 0.045f, 0.055f), i == 1 ? materials.Warning : materials.DarkRubber);
            }

            for (var i = 0; i < 7; i++)
            {
                CreateBox(ControlRoomDressingName + " CCTV Main Scanline " + (i + 1), root.transform, new Vector3(13.78f, 1.06f + (i * 0.085f), 21.585f), new Vector3(1.26f, 0.01f, 0.012f), materials.Light);
            }

            for (var i = 0; i < 7; i++)
            {
                CreateBox(
                    ControlRoomDressingName + " Floor Hazard Slash " + (i + 1),
                    root.transform,
                    new Vector3(11.35f + (i * 0.52f), 0.07f, 20.18f),
                    new Vector3(0.28f, 0.026f, 0.06f),
                    i % 2 == 0 ? materials.Yellow : materials.Warning,
                    Quaternion.Euler(0f, -17f, 0f));
            }
            CreateBox(ControlRoomDressingName + " Right Wall Pipe Vertical A", root.transform, new Vector3(17.82f, 1.38f, 18.65f), new Vector3(0.07f, 1.95f, 0.07f), materials.DarkRubber);
            CreateBox(ControlRoomDressingName + " Right Wall Pipe Vertical B", root.transform, new Vector3(17.62f, 1.38f, 18.65f), new Vector3(0.06f, 1.95f, 0.06f), materials.Warning);
            CreateLightStrip(ControlRoomDressingName + " CCTV Ceiling Light", root.transform, new Vector3(14f, 2.56f, 20.02f), new Vector3(1.38f, 0.045f, 0.3f), materials);
        }

        private static void CreateEngineRoomDressing(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(EngineRoomDressingName, parent);
            CreateFloorGrid(EngineRoomDressingName, root.transform, new Vector3(-14f, 0.035f, 18f), 3, 3, 1.82f, 1.82f, materials);
            CreateCeilingGrid(EngineRoomDressingName, root.transform, new Vector3(-14f, 2.72f, 18f), 3, 3, 1.82f, 1.82f, materials);

            for (var i = 0; i < 5; i++)
            {
                var x = -16.6f + (i * 0.72f);
                CreateCylinder(EngineRoomDressingName + " Turbine Rib " + (i + 1), root.transform, new Vector3(x, 1.08f, 18f), new Vector3(0.62f, 0.08f, 0.62f), i % 2 == 0 ? materials.Metal : materials.DarkRubber, Quaternion.Euler(0f, 0f, 90f));
            }

            CreateCylinder(EngineRoomDressingName + " Long Power Core", root.transform, new Vector3(-15.25f, 1.08f, 18f), new Vector3(0.42f, 1.92f, 0.42f), materials.Damaged, Quaternion.Euler(0f, 0f, 90f));
            CreateBox(EngineRoomDressingName + " Safety Rail Top", root.transform, new Vector3(-13.9f, 1.1f, 15.28f), new Vector3(4.8f, 0.08f, 0.08f), materials.Yellow);
            CreateBox(EngineRoomDressingName + " Safety Rail Mid", root.transform, new Vector3(-13.9f, 0.66f, 15.28f), new Vector3(4.8f, 0.07f, 0.07f), materials.DarkRubber);
            for (var i = 0; i < 5; i++)
            {
                CreateBox(EngineRoomDressingName + " Rail Post " + (i + 1), root.transform, new Vector3(-16.1f + (i * 1.1f), 0.66f, 15.28f), new Vector3(0.08f, 0.82f, 0.08f), materials.DarkRubber);
            }

            for (var i = 0; i < 4; i++)
            {
                CreateBox(EngineRoomDressingName + " Wall Conduit " + (i + 1), root.transform, new Vector3(-10.08f, 1.2f + (i * 0.18f), 16.24f), new Vector3(0.075f, 0.075f, 2.35f), materials.DarkRubber);
            }

            CreateLightStrip(EngineRoomDressingName + " Engine Blue Work Light", root.transform, new Vector3(-14f, 2.54f, 18f), new Vector3(1.35f, 0.045f, 0.28f), materials);
        }

        private static void CreateSupplyRoomDressing(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(SupplyRoomDressingName, parent);
            CreateFloorGrid(SupplyRoomDressingName, root.transform, new Vector3(14f, 0.035f, -14f), 3, 3, 1.8f, 1.8f, materials);
            CreateCeilingGrid(SupplyRoomDressingName, root.transform, new Vector3(14f, 2.72f, -14f), 3, 3, 1.8f, 1.8f, materials);

            CreateBox(SupplyRoomDressingName + " Locker Backing Wall", root.transform, new Vector3(17.87f, 1.35f, -14f), new Vector3(0.09f, 2.1f, 4.72f), materials.Metal);
            for (var row = 0; row < 2; row++)
            {
                for (var col = 0; col < 3; col++)
                {
                    var z = -15.42f + (col * 0.96f);
                    var y = 0.86f + (row * 0.76f);
                    CreateBox(SupplyRoomDressingName + " Yellow Locker Band " + row + " " + col, root.transform, new Vector3(17.31f, y, z), new Vector3(0.032f, 0.065f, 0.52f), materials.Yellow);
                    CreateBox(SupplyRoomDressingName + " Door Hinge Pair " + row + " " + col, root.transform, new Vector3(17.35f, y, z + 0.31f), new Vector3(0.04f, 0.38f, 0.045f), materials.DarkRubber);
                }
            }

            for (var i = 0; i < 4; i++)
            {
                CreateBox(SupplyRoomDressingName + " Left Shelf Crate " + (i + 1), root.transform, new Vector3(11.05f, 0.42f + (i % 2) * 0.72f, -15.4f + (i / 2) * 1.2f), new Vector3(0.88f, 0.48f, 0.72f), i == 2 ? materials.Cargo : materials.Metal);
            }

            CreateBox(SupplyRoomDressingName + " Overhead Pipe", root.transform, new Vector3(14f, 2.44f, -16.85f), new Vector3(5.8f, 0.08f, 0.08f), materials.DarkRubber);
            CreateLightStrip(SupplyRoomDressingName + " Supply Cage Light", root.transform, new Vector3(14f, 2.54f, -14f), new Vector3(1.5f, 0.045f, 0.28f), materials);
        }

        private static void CreateArmoryDressing(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(ArmoryDressingName, parent);
            CreateFloorGrid(ArmoryDressingName, root.transform, new Vector3(-14f, 0.035f, -14f), 3, 3, 1.82f, 1.82f, materials);
            CreateCeilingGrid(ArmoryDressingName, root.transform, new Vector3(-14f, 2.72f, -14f), 3, 3, 1.82f, 1.82f, materials);

            CreateBox(ArmoryDressingName + " Forward Armor Wall Frame", root.transform, new Vector3(-14f, 1.45f, -10.05f), new Vector3(5.6f, 2.1f, 0.12f), materials.Metal);
            CreateBox(ArmoryDressingName + " Dark Viewport Recess", root.transform, new Vector3(-14f, 1.58f, -10.13f), new Vector3(3.55f, 1.18f, 0.09f), materials.DarkRubber);
            CreateBox(ArmoryDressingName + " Turret Shelf", root.transform, new Vector3(-14f, 0.56f, -10.82f), new Vector3(3.2f, 0.16f, 0.62f), materials.Metal);
            CreateBox(ArmoryDressingName + " Dark Manual Grip Bar", root.transform, new Vector3(-14f, 0.86f, -10.76f), new Vector3(0.72f, 0.065f, 0.065f), materials.DarkRubber);

            for (var i = 0; i < 5; i++)
            {
                var x = -16.2f + (i * 1.1f);
                CreateBox(ArmoryDressingName + " Frame Bolt Plate " + (i + 1), root.transform, new Vector3(x, 2.38f, -10.18f), new Vector3(0.32f, 0.18f, 0.08f), materials.CrowbarSteel);
            }

            CreateBox(ArmoryDressingName + " Right Control Pedestal", root.transform, new Vector3(-10.42f, 1.28f, -12f), new Vector3(0.24f, 1.35f, 0.52f), materials.Metal);
            CreateBox(ArmoryDressingName + " Right Red Button", root.transform, new Vector3(-10.28f, 1.08f, -12f), new Vector3(0.06f, 0.2f, 0.2f), materials.Warning);
            CreateBox(ArmoryDressingName + " Right Dial", root.transform, new Vector3(-10.28f, 1.58f, -12f), new Vector3(0.07f, 0.24f, 0.24f), materials.CrowbarSteel);
            CreateLightStrip(ArmoryDressingName + " Armory Overhead Light", root.transform, new Vector3(-14f, 2.54f, -12.4f), new Vector3(1.32f, 0.045f, 0.28f), materials);
        }

        private static void CreateFloorGrid(
            string prefix,
            Transform parent,
            Vector3 center,
            int columns,
            int rows,
            float cellWidth,
            float cellDepth,
            Stage3Materials materials)
        {
            var startX = center.x - ((columns - 1) * cellWidth * 0.5f);
            var startZ = center.z - ((rows - 1) * cellDepth * 0.5f);
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var index = (row * columns) + column + 1;
                    var x = startX + (column * cellWidth);
                    var z = startZ + (row * cellDepth);
                    CreateBox(
                        prefix + " Deck Tile " + index,
                        parent,
                        new Vector3(x, center.y, z),
                        new Vector3(cellWidth * 0.84f, 0.04f, cellDepth * 0.84f),
                        (index % 3) == 0 ? materials.Damaged : materials.Metal);
                    CreateBox(
                        prefix + " Deck Inset " + index,
                        parent,
                        new Vector3(x, center.y + 0.026f, z),
                        new Vector3(cellWidth * 0.46f, 0.018f, cellDepth * 0.46f),
                        materials.DarkRubber);
                }
            }
        }

        private static void CreateCeilingGrid(
            string prefix,
            Transform parent,
            Vector3 center,
            int columns,
            int rows,
            float cellWidth,
            float cellDepth,
            Stage3Materials materials)
        {
            var startX = center.x - ((columns - 1) * cellWidth * 0.5f);
            var startZ = center.z - ((rows - 1) * cellDepth * 0.5f);
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var index = (row * columns) + column + 1;
                    var x = startX + (column * cellWidth);
                    var z = startZ + (row * cellDepth);
                    CreateBox(
                        prefix + " Ceiling Tile " + index,
                        parent,
                        new Vector3(x, center.y, z),
                        new Vector3(cellWidth * 0.88f, 0.035f, cellDepth * 0.88f),
                        (index % 4) == 0 ? materials.DarkRubber : materials.Damaged);
                    CreateBox(
                        prefix + " Ceiling Rib " + index,
                        parent,
                        new Vector3(x, center.y - 0.035f, z),
                        new Vector3(cellWidth * 0.08f, 0.06f, cellDepth * 0.95f),
                        materials.DarkRubber);
                    CreateBox(
                        prefix + " Ceiling Shadow Insert " + index,
                        parent,
                        new Vector3(x, center.y - 0.058f, z),
                        new Vector3(cellWidth * 0.58f, 0.018f, cellDepth * 0.58f),
                        materials.DarkRubber);
                }
            }
        }

        private static void CreateLightStrip(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Stage3Materials materials)
        {
            CreateBox(name + " Metal Cage", parent, position + new Vector3(0f, 0.012f, 0f), scale + new Vector3(0.22f, 0.035f, 0.11f), materials.Metal);
            CreateBox(name + " Warm Glow", parent, position, scale, materials.WarmLight);
            for (var i = 0; i < 4; i++)
            {
                var x = position.x - (scale.x * 0.42f) + (i * scale.x * 0.28f);
                CreateBox(name + " Cage Rib " + (i + 1), parent, new Vector3(x, position.y - 0.022f, position.z), new Vector3(0.035f, scale.y * 1.35f, scale.z * 1.22f), materials.DarkRubber);
            }

            var lightObject = new GameObject(name + " Actual Warm Fill Light");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = position + new Vector3(0f, -0.34f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.76f, 0.48f, 1f);
            light.intensity = 0.9f;
            light.range = 4.8f;
            light.shadows = LightShadows.None;
        }

        private static void CreateScreenInset(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Stage3Materials materials)
        {
            CreateBox(name + " Frame", parent, position + new Vector3(0f, 0.005f, 0f), scale + new Vector3(0.08f, 0.028f, 0.06f), materials.Metal);
            CreateBox(name + " Green Glass", parent, position + new Vector3(0f, -0.018f, -0.012f), scale, materials.Screen);
        }

        private static void HideLegacyGrayboxPresentationElements()
        {
            var rendererOnlyObjects = new[]
            {
                "Cargo Hold Central Cargo",
                "Interactable - Cargo Hold Cargo Status",
                "Interactable - Cockpit Helm",
                "Interactable - Engine Room Power Screen",
                "Interactable - Control Room Main Screen",
                "Interactable - Armory Turret Handle",
                "Interactable - Supply Room Storage Cabinet",
                "Control Room Horizontal Screen Placeholder",
                "Control Room Vertical Screen Placeholder",
                "Control Room Screen Partition",
                "Cockpit Console Base",
                "Cockpit Worn Button Strip",
                "Engine Room Central Power Cylinder",
                "Armory Central Pillar",
                "Armory Turret Warning Rail",
                "Armory Turret Station Support Frame",
                "Armory Forward Screen Placeholder",
                "Supply Room Ejection Pad Placeholder",
                "Supply Room Ejection Terminal Placeholder",
            };

            for (var i = 0; i < rendererOnlyObjects.Length; i++)
            {
                DisableRenderers(rendererOnlyObjects[i]);
            }

            var activeScene = SceneManager.GetActiveScene();
            var textMeshes = Resources.FindObjectsOfTypeAll<TextMesh>();
            for (var i = 0; i < textMeshes.Length; i++)
            {
                var textMesh = textMeshes[i];
                if (textMesh == null ||
                    textMesh.gameObject.scene != activeScene ||
                    (!textMesh.name.StartsWith("Label - ", StringComparison.Ordinal) &&
                     !textMesh.name.StartsWith("Sign - ", StringComparison.Ordinal)))
                {
                    continue;
                }

                textMesh.color = new Color(textMesh.color.r, textMesh.color.g, textMesh.color.b, 0.04f);
                var renderer = textMesh.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = false;
                    EditorUtility.SetDirty(renderer);
                }

                EditorUtility.SetDirty(textMesh);
            }
        }

        private static void DisableRenderers(string objectName)
        {
            var target = FindSceneObject(objectName);
            if (target == null)
            {
                return;
            }

            var renderers = target.GetComponentsInChildren<MeshRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = false;
                EditorUtility.SetDirty(renderers[i]);
            }
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
            CreateBox(CockpitHelmPropName + " Console Base", root.transform, new Vector3(0f, 0.42f, 17.52f), new Vector3(2.55f, 0.42f, 0.95f), materials.Metal);
            CreateBox(CockpitHelmPropName + " Sloped Screen Housing", root.transform, new Vector3(0f, 0.92f, 17.18f), new Vector3(2.12f, 0.2f, 0.72f), materials.Metal, Quaternion.Euler(-14f, 0f, 0f));
            CreateBox(CockpitHelmPropName + " Readiness Screen", root.transform, new Vector3(0f, 1.07f, 17.0f), new Vector3(1.68f, 0.04f, 0.46f), materials.Screen, Quaternion.Euler(-14f, 0f, 0f));
            CreateCylinder(CockpitHelmPropName + " Helm Column", root.transform, new Vector3(0f, 0.92f, 16.72f), new Vector3(0.075f, 0.46f, 0.075f), materials.Metal, Quaternion.identity);

            var center = new Vector3(0f, 1.26f, 16.72f);
            for (var i = 0; i < 16; i++)
            {
                var angle = i * Mathf.PI * 2f / 16f;
                var position = center + new Vector3(Mathf.Cos(angle) * 0.54f, Mathf.Sin(angle) * 0.54f, 0f);
                CreateBox(
                    CockpitHelmPropName + " Ring Segment " + (i + 1),
                    root.transform,
                    position,
                    new Vector3(0.24f, 0.04f, 0.055f),
                    i % 2 == 0 ? materials.Metal : materials.DarkRubber,
                    Quaternion.Euler(0f, 0f, (angle * Mathf.Rad2Deg) + 90f));
            }

            var reviewCenter = new Vector3(0f, 1.02f, 16.92f);
            for (var i = 0; i < 24; i++)
            {
                var angle = i * Mathf.PI * 2f / 24f;
                var position = reviewCenter + new Vector3(Mathf.Cos(angle) * 0.82f, Mathf.Sin(angle) * 0.54f, 0f);
                CreateBox(
                    CockpitHelmPropName + " Review Bright Ring Segment " + (i + 1),
                    root.transform,
                    position,
                    new Vector3(0.28f, 0.064f, 0.14f),
                    i % 3 == 0 ? materials.CrowbarSteel : materials.Metal,
                    Quaternion.Euler(0f, 0f, (angle * Mathf.Rad2Deg) + 90f));
            }

            for (var i = 0; i < 8; i++)
            {
                var angle = i * Mathf.PI * 2f / 8f;
                var position = reviewCenter + new Vector3(Mathf.Cos(angle) * 0.82f, Mathf.Sin(angle) * 0.54f, -0.075f);
                CreateBox(
                    CockpitHelmPropName + " Review Ring Bolt " + (i + 1),
                    root.transform,
                    position,
                    new Vector3(0.07f, 0.07f, 0.05f),
                    materials.CrowbarSteel,
                    Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg));
            }

            CreateBox(CockpitHelmPropName + " Review Left Control Grip", root.transform, new Vector3(-0.98f, 0.92f, 16.90f), new Vector3(0.18f, 0.58f, 0.15f), materials.DarkRubber, Quaternion.Euler(0f, 0f, -20f));
            CreateBox(CockpitHelmPropName + " Review Right Control Grip", root.transform, new Vector3(0.98f, 0.92f, 16.90f), new Vector3(0.18f, 0.58f, 0.15f), materials.DarkRubber, Quaternion.Euler(0f, 0f, 20f));
            CreateBox(CockpitHelmPropName + " Review Center Green Screen Bezel", root.transform, new Vector3(0f, 1.48f, 17.72f), new Vector3(1.56f, 0.44f, 0.10f), materials.Metal);
            CreateBox(CockpitHelmPropName + " Review Center Green Screen Glass", root.transform, new Vector3(0f, 1.48f, 17.645f), new Vector3(1.24f, 0.26f, 0.035f), materials.Screen);
            for (var i = 0; i < 5; i++)
            {
                CreateBox(CockpitHelmPropName + " Review Main Screen Scanline " + (i + 1), root.transform, new Vector3(0f, 1.38f + (i * 0.045f), 17.62f), new Vector3(1.08f, 0.008f, 0.012f), materials.Light);
            }

            CreateBox(CockpitHelmPropName + " Left Grip", root.transform, new Vector3(-0.67f, 1.18f, 16.72f), new Vector3(0.13f, 0.34f, 0.095f), materials.DarkRubber, Quaternion.Euler(0f, 0f, 18f));
            CreateBox(CockpitHelmPropName + " Right Grip", root.transform, new Vector3(0.67f, 1.18f, 16.72f), new Vector3(0.13f, 0.34f, 0.095f), materials.DarkRubber, Quaternion.Euler(0f, 0f, -18f));
            CreateBox(CockpitHelmPropName + " Lower Bolted Pedestal", root.transform, new Vector3(0f, 0.54f, 16.72f), new Vector3(0.46f, 0.42f, 0.22f), materials.Damaged);
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
            CreateBox(ControlRoomCctvTerminalName + " Wall Back Plate", root.transform, new Vector3(14.05f, 1.43f, 21.86f), new Vector3(5.35f, 1.92f, 0.12f), materials.Metal);
            CreateBox(ControlRoomCctvTerminalName + " Console Lip", root.transform, new Vector3(14f, 0.66f, 21.36f), new Vector3(4.55f, 0.36f, 0.62f), materials.Metal);
            CreateBox(ControlRoomCctvTerminalName + " Lower Recess", root.transform, new Vector3(14f, 0.39f, 21.56f), new Vector3(4.0f, 0.22f, 0.28f), materials.DarkRubber);

            CreateBox(ControlRoomCctvMainScreenFrameName, root.transform, new Vector3(13.68f, 1.34f, 21.72f), new Vector3(2.98f, 1.34f, 0.105f), materials.Metal);
            CreateBox(ControlRoomCctvMainScreenGlowName, root.transform, new Vector3(13.68f, 1.34f, 21.645f), new Vector3(2.46f, 0.96f, 0.038f), materials.Screen);
            CreateBox(ControlRoomCctvTerminalName + " Large Screen Top Clamp", root.transform, new Vector3(13.68f, 1.99f, 21.6f), new Vector3(2.92f, 0.09f, 0.13f), materials.DarkRubber);
            CreateBox(ControlRoomCctvTerminalName + " Large Screen Bottom Clamp", root.transform, new Vector3(13.68f, 0.69f, 21.6f), new Vector3(2.92f, 0.09f, 0.13f), materials.DarkRubber);
            CreateBox(ControlRoomCctvTerminalName + " Large Screen Left Clamp", root.transform, new Vector3(12.27f, 1.34f, 21.6f), new Vector3(0.09f, 1.28f, 0.13f), materials.DarkRubber);
            CreateBox(ControlRoomCctvTerminalName + " Large Screen Right Clamp", root.transform, new Vector3(15.09f, 1.34f, 21.6f), new Vector3(0.09f, 1.28f, 0.13f), materials.DarkRubber);

            for (var i = 0; i < 10; i++)
            {
                CreateBox(
                    ControlRoomCctvTerminalName + " Large Screen Scanline " + (i + 1),
                    root.transform,
                    new Vector3(13.68f, 0.93f + (i * 0.092f), 21.595f),
                    new Vector3(2.18f, 0.007f, 0.014f),
                    i % 3 == 0 ? materials.Light : materials.Screen);
            }

            CreateBox(ControlRoomCctvHorizontalScreenName + " Frame", root.transform, new Vector3(12.38f, 2.1f, 21.68f), new Vector3(1.12f, 0.34f, 0.09f), materials.Metal);
            CreateBox(ControlRoomCctvHorizontalScreenName + " Glow", root.transform, new Vector3(12.38f, 2.1f, 21.615f), new Vector3(0.86f, 0.17f, 0.032f), materials.Screen);
            CreateBox(ControlRoomCctvVerticalScreenName + " Frame", root.transform, new Vector3(15.7f, 1.36f, 21.68f), new Vector3(0.56f, 1.38f, 0.09f), materials.Metal);
            CreateBox(ControlRoomCctvVerticalScreenName + " Glow", root.transform, new Vector3(15.7f, 1.36f, 21.615f), new Vector3(0.34f, 1.08f, 0.032f), materials.Screen);

            for (var i = 0; i < 7; i++)
            {
                CreateBox(
                    ControlRoomCctvVerticalScreenName + " Zone Block " + (i + 1),
                    root.transform,
                    new Vector3(15.7f, 0.89f + (i * 0.15f), 21.58f),
                    new Vector3(0.25f, 0.065f, 0.026f),
                    i == 1 ? materials.Yellow : materials.Light);
            }

            CreateBox(ControlRoomCctvButtonAName, root.transform, new Vector3(14.22f, 0.86f, 21.08f), new Vector3(0.28f, 0.09f, 0.18f), materials.Yellow);
            CreateBox(ControlRoomCctvButtonDName, root.transform, new Vector3(14.66f, 0.86f, 21.08f), new Vector3(0.28f, 0.09f, 0.18f), materials.Warning);
            for (var i = 0; i < 4; i++)
            {
                CreateBox(
                    ControlRoomCctvTerminalName + " Function Button " + (i + 1),
                    root.transform,
                    new Vector3(12.82f + (i * 0.26f), 0.86f, 21.08f),
                    new Vector3(0.15f, 0.065f, 0.12f),
                    i % 2 == 0 ? materials.DarkRubber : materials.Metal);
            }

            for (var i = 0; i < 4; i++)
            {
                CreateBox(
                    ControlRoomCctvTerminalName + " Wall Cable Rail " + (i + 1),
                    root.transform,
                    new Vector3(14.18f, 2.32f - (i * 0.12f), 21.92f),
                    new Vector3(5.35f, 0.04f, 0.055f),
                    i == 1 ? materials.Warning : materials.DarkRubber);
            }

            CreateBox(ControlRoomCctvTerminalName + " Left Cable Bracket", root.transform, new Vector3(11.35f, 2.02f, 21.9f), new Vector3(0.14f, 0.48f, 0.09f), materials.Metal);
            CreateBox(ControlRoomCctvTerminalName + " Right Cable Bracket", root.transform, new Vector3(16.78f, 1.56f, 21.9f), new Vector3(0.14f, 1.24f, 0.09f), materials.Metal);
            for (var i = 0; i < 6; i++)
            {
                var y = 2.42f - (i * 0.115f);
                CreateBox(ControlRoomCctvTerminalName + " Review Upper Pipe " + (i + 1), root.transform, new Vector3(14.05f, y, 21.76f), new Vector3(5.9f, 0.035f, 0.04f), i == 1 || i == 4 ? materials.Warning : materials.DarkRubber);
            }

            for (var i = 0; i < 5; i++)
            {
                CreateBox(ControlRoomCctvTerminalName + " Review Right Vertical Conduit " + (i + 1), root.transform, new Vector3(16.86f - (i * 0.105f), 1.18f, 21.72f), new Vector3(0.04f, 1.72f, 0.04f), i == 2 ? materials.Warning : materials.DarkRubber);
            }

            for (var i = 0; i < 5; i++)
            {
                CreateBox(ControlRoomCctvTerminalName + " Console Warning Paint Chip " + (i + 1), root.transform, new Vector3(12.0f + (i * 0.34f), 0.69f, 21.005f), new Vector3(0.16f, 0.018f, 0.045f), i % 2 == 0 ? materials.Yellow : materials.Warning, Quaternion.Euler(-9f, 0f, 0f));
            }
        }

        private static void CreateEngineRoomPowerTerminal(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(EngineRoomPowerTerminalName, parent);
            CreateBox(EngineRoomPowerTerminalName + " Cabinet", root.transform, new Vector3(-10.2f, 0.96f, 17.72f), new Vector3(0.16f, 1.28f, 0.94f), materials.Metal);
            CreateBox(EngineRoomPowerTerminalName + " Power Screen", root.transform, new Vector3(-10.3f, 1.24f, 17.72f), new Vector3(0.045f, 0.46f, 0.56f), materials.Screen);
            CreateBox(EngineRoomPowerTerminalName + " Overclock Warning", root.transform, new Vector3(-10.32f, 0.63f, 17.72f), new Vector3(0.04f, 0.12f, 0.68f), materials.Warning);
            CreateBox(EngineRoomPowerTerminalName + " Breaker Left", root.transform, new Vector3(-10.33f, 0.28f, 17.5f), new Vector3(0.05f, 0.27f, 0.12f), materials.DarkRubber);
            CreateBox(EngineRoomPowerTerminalName + " Breaker Right", root.transform, new Vector3(-10.33f, 0.28f, 17.94f), new Vector3(0.05f, 0.27f, 0.12f), materials.DarkRubber);
            CreateCylinder(EngineRoomPowerTerminalName + " Top Pipe", root.transform, new Vector3(-10.25f, 1.68f, 17.72f), new Vector3(0.045f, 0.62f, 0.045f), materials.DarkRubber, Quaternion.Euler(90f, 0f, 0f));
            CreateCylinder(EngineRoomPowerTerminalName + " Lower Cable", root.transform, new Vector3(-10.28f, 0.12f, 17.72f), new Vector3(0.025f, 0.52f, 0.025f), materials.Warning, Quaternion.Euler(90f, 0f, 0f));
        }

        private static void CreateSupplyRoomStorageCabinet(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(SupplyRoomStorageCabinetName, parent);
            CreateBox(SupplyRoomStorageCabinetName + " Back Plate", root.transform, new Vector3(17.58f, 1.04f, -14.1f), new Vector3(0.16f, 1.5f, 2.36f), materials.Metal);
            for (var row = 0; row < 2; row++)
            {
                for (var col = 0; col < 3; col++)
                {
                    var index = (row * 3) + col + 1;
                    CreateBox(
                        SupplyRoomStorageCabinetName + " Locker Door " + index,
                        root.transform,
                        new Vector3(17.46f, 0.72f + (row * 0.62f), -14.82f + (col * 0.72f)),
                        new Vector3(0.08f, 0.48f, 0.58f),
                        materials.Cargo);
                    CreateBox(
                        SupplyRoomStorageCabinetName + " Handle " + index,
                        root.transform,
                        new Vector3(17.38f, 0.72f + (row * 0.62f), -14.62f + (col * 0.72f)),
                        new Vector3(0.045f, 0.22f, 0.06f),
                        materials.DarkRubber);
                }
            }
        }

        private static void CreateCargoHoldStatusPanel(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(CargoHoldStatusPanelName, parent);
            CreateBox(CargoHoldStatusPanelName + " Panel Frame", root.transform, new Vector3(-5.54f, -1.22f, 0.55f), new Vector3(0.12f, 0.82f, 1.72f), materials.Metal);
            CreateBox(CargoHoldStatusPanelName + " Load Screen", root.transform, new Vector3(-5.62f, -1.1f, 0.35f), new Vector3(0.038f, 0.42f, 1.02f), materials.Screen);
            CreateBox(CargoHoldStatusPanelName + " Secure Indicator", root.transform, new Vector3(-5.65f, -1.56f, -0.08f), new Vector3(0.035f, 0.12f, 0.22f), materials.Yellow);
            CreateBox(CargoHoldStatusPanelName + " Overload Indicator", root.transform, new Vector3(-5.65f, -1.56f, 0.34f), new Vector3(0.035f, 0.12f, 0.22f), materials.Warning);
            CreateBox(CargoHoldStatusPanelName + " Ready Lamp", root.transform, new Vector3(-5.65f, -1.08f, 1.08f), new Vector3(0.034f, 0.09f, 0.09f), materials.Light);
            CreateBox(CargoHoldStatusPanelName + " Left Clamp", root.transform, new Vector3(-5.48f, -1.22f, -0.38f), new Vector3(0.16f, 0.96f, 0.12f), materials.DarkRubber);
            CreateBox(CargoHoldStatusPanelName + " Right Clamp", root.transform, new Vector3(-5.48f, -1.22f, 1.48f), new Vector3(0.16f, 0.96f, 0.12f), materials.DarkRubber);
            CreateBox(CargoHoldStatusPanelName + " Lower Cable Brace", root.transform, new Vector3(-5.38f, -1.86f, 0.55f), new Vector3(0.08f, 0.08f, 1.56f), materials.DarkRubber);
            CreateBox(CargoHoldStatusPanelName + " Rear Cable Bundle", root.transform, new Vector3(-5.36f, -0.62f, 0.55f), new Vector3(0.07f, 0.07f, 2.15f), materials.DarkRubber);
        }

        private static void CreateArmoryTurretGripMount(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(ArmoryTurretGripMountName, parent);
            CreateBox(ArmoryTurretGripMountName + " Rail", root.transform, new Vector3(-14f, 1.22f, -10.52f), new Vector3(2.05f, 0.18f, 0.2f), materials.Metal);
            CreateCylinder(ArmoryTurretGripMountName + " Pivot", root.transform, new Vector3(-14f, 0.98f, -10.58f), new Vector3(0.18f, 0.18f, 0.18f), materials.Metal, Quaternion.Euler(90f, 0f, 0f));
            CreateBox(ArmoryTurretGripMountName + " Left Grip", root.transform, new Vector3(-14.42f, 0.68f, -10.72f), new Vector3(0.14f, 0.56f, 0.14f), materials.DarkRubber, Quaternion.Euler(0f, 0f, 10f));
            CreateBox(ArmoryTurretGripMountName + " Right Grip", root.transform, new Vector3(-13.58f, 0.68f, -10.72f), new Vector3(0.14f, 0.56f, 0.14f), materials.DarkRubber, Quaternion.Euler(0f, 0f, -10f));
            CreateBox(ArmoryTurretGripMountName + " Sight Hood", root.transform, new Vector3(-14f, 1.5f, -10.72f), new Vector3(0.72f, 0.18f, 0.32f), materials.Metal);
            CreateBox(ArmoryTurretGripMountName + " Trigger Bar", root.transform, new Vector3(-14f, 0.58f, -10.88f), new Vector3(0.52f, 0.065f, 0.065f), materials.DarkRubber);
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
            CreateBox(ContractCargoBodyName, root.transform, new Vector3(0f, -2.3f, 0f), new Vector3(2.62f, 1.38f, 3.02f), materials.Cargo);
            CreateBox(ContractCargoContainerName + " Top Frame", root.transform, new Vector3(0f, -1.56f, 0f), new Vector3(2.82f, 0.08f, 3.18f), materials.Metal);
            CreateBox(ContractCargoContainerName + " Bottom Frame", root.transform, new Vector3(0f, -2.96f, 0f), new Vector3(2.82f, 0.08f, 3.18f), materials.Metal);
            CreateBox(ContractCargoContainerName + " Left Frame", root.transform, new Vector3(-1.38f, -2.3f, 0f), new Vector3(0.08f, 1.38f, 3.18f), materials.Metal);
            CreateBox(ContractCargoContainerName + " Right Frame", root.transform, new Vector3(1.38f, -2.3f, 0f), new Vector3(0.08f, 1.38f, 3.18f), materials.Metal);
            CreateBox(ContractCargoStrapHorizontalName, root.transform, new Vector3(0f, -2.3f, -1.56f), new Vector3(2.95f, 0.18f, 0.11f), materials.DarkRubber);
            CreateBox(ContractCargoStrapVerticalName, root.transform, new Vector3(0f, -2.3f, -1.59f), new Vector3(0.18f, 1.32f, 0.11f), materials.DarkRubber);
            CreateBox(ContractCargoContainerName + " Bracket Left", root.transform, new Vector3(-1.49f, -2.3f, -1.64f), new Vector3(0.15f, 0.34f, 0.12f), materials.Metal);
            CreateBox(ContractCargoContainerName + " Bracket Right", root.transform, new Vector3(1.49f, -2.3f, -1.64f), new Vector3(0.15f, 0.34f, 0.12f), materials.Metal);
            CreateBox(ContractCargoContainerName + " Bracket Top", root.transform, new Vector3(0f, -1.54f, -1.64f), new Vector3(0.38f, 0.14f, 0.12f), materials.Metal);
            CreateBox(ContractCargoContainerName + " Bracket Bottom", root.transform, new Vector3(0f, -2.9f, -1.64f), new Vector3(0.38f, 0.14f, 0.12f), materials.Metal);
            CreateBox(ContractCargoContainerName + " Lock Tag", root.transform, new Vector3(0.36f, -2.04f, -1.68f), new Vector3(0.28f, 0.2f, 0.045f), materials.Yellow);
        }

        private static void CreatePersonalCargoContainer(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(PersonalCargoContainerName, parent);
            CreateBox(PersonalCargoContainerName + " Body", root.transform, new Vector3(3.05f, -2.66f, -0.55f), new Vector3(1.2f, 0.68f, 0.92f), materials.Cargo);
            CreateBox(PersonalCargoContainerName + " Lid Rail", root.transform, new Vector3(3.05f, -2.28f, -0.55f), new Vector3(1.3f, 0.08f, 1.0f), materials.Metal);
            CreateBox(PersonalCargoContainerName + " Front Strap", root.transform, new Vector3(3.05f, -2.66f, -1.05f), new Vector3(1.32f, 0.1f, 0.07f), materials.DarkRubber);
            CreateBox(PersonalCargoContainerName + " Name Plate", root.transform, new Vector3(2.68f, -2.49f, -1.09f), new Vector3(0.32f, 0.16f, 0.04f), materials.Yellow);
        }

        private static void CreateWarningLabelSet(Transform parent, Stage3Materials materials)
        {
            var root = CreateChildRoot(WarningLabelSetName, parent);
            CreateBox(WarningLabelSetName + " Cargo Warning", root.transform, new Vector3(-0.58f, -1.98f, -1.68f), new Vector3(0.34f, 0.18f, 0.035f), materials.Warning);
            CreateBox(WarningLabelSetName + " Cargo Mass Label", root.transform, new Vector3(-0.98f, -1.98f, -1.68f), new Vector3(0.34f, 0.18f, 0.035f), materials.Yellow);
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
            CreateBox(DiegeticTerminalShellName + " Pedestal", root.transform, new Vector3(2.15f, -2.59f, 2.78f), new Vector3(0.58f, 0.82f, 0.46f), materials.Metal);
            CreateBox(DiegeticTerminalShellName + " Angled Shell", root.transform, new Vector3(2.15f, -2.06f, 2.64f), new Vector3(1.15f, 0.42f, 0.32f), materials.Metal, Quaternion.Euler(-16f, 0f, 0f));
            CreateBox(DiegeticTerminalScreenBackingName, root.transform, new Vector3(2.15f, -1.93f, 2.45f), new Vector3(0.86f, 0.26f, 0.035f), materials.Screen, Quaternion.Euler(-16f, 0f, 0f));
            for (var i = 0; i < 6; i++)
            {
                CreateBox(
                    DiegeticTerminalButtonMeshName + " " + (i + 1),
                    root.transform,
                    new Vector3(1.78f + (i * 0.15f), -2.19f, 2.38f),
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
            root.transform.localPosition = new Vector3(0.22f, -0.02f, 0.90f);
            root.transform.localRotation = Quaternion.Euler(20f, -18f, -30f);
            root.transform.localScale = Vector3.one * 0.58f;
            CreateCrowbarContinuousBody(root.transform, materials.CrowbarSteel);
            CreateCrowbarGripWraps(root.transform, materials);
            CreateCrowbarGlovedHands(root.transform, materials);
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

        private static GameObject InstantiateStage3Prefab(string prefabPath, Transform parent, string instanceName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Missing Stage 3 prefab asset: " + prefabPath);
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
            if (instance == null)
            {
                throw new InvalidOperationException("Could not instantiate Stage 3 prefab asset: " + prefabPath);
            }

            instance.name = instanceName;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            return instance;
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
            var gameObject = new GameObject(name);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);

            var filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = Stage3BlenderReviewAssetBuilder.LoadPrimitiveMesh(primitiveType);

            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            return gameObject;
        }

        private static GameObject CreateCrowbarContinuousBody(Transform parent, Material material)
        {
            var body = new GameObject(CrowbarContinuousBodyName);
            body.transform.SetParent(parent, false);

            var filter = body.AddComponent<MeshFilter>();
            filter.sharedMesh = Stage3BlenderReviewAssetBuilder.LoadNamedMesh(Stage3BlenderReviewAssetBuilder.HookedCrowbarBodyMeshName);

            var renderer = body.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return body;
        }

        private static void CreateCrowbarGripWraps(Transform parent, Stage3Materials materials)
        {
            for (var i = 0; i < 12; i++)
            {
                CreateLocalCylinder(
                    CrowbarGripWrapName + " Lower " + (i + 1),
                    parent,
                    new Vector3(0.002f, -0.72f + (i * 0.035f), 0f),
                    new Vector3(0.068f, 0.006f, 0.068f),
                    materials.DarkRubber);
            }

            for (var i = 0; i < 10; i++)
            {
                CreateLocalCylinder(
                    CrowbarGripWrapName + " Upper " + (i + 1),
                    parent,
                    new Vector3(0.003f, -0.18f + (i * 0.035f), 0f),
                    new Vector3(0.064f, 0.006f, 0.064f),
                    materials.DarkRubber);
            }

            CreateLocalCylinder(CrowbarGripWrapName + " Lower Metal Collar", parent, new Vector3(0.002f, -0.82f, 0f), new Vector3(0.074f, 0.012f, 0.074f), materials.Metal);
            CreateLocalCylinder(CrowbarGripWrapName + " Upper Metal Collar", parent, new Vector3(0.003f, 0.21f, 0f), new Vector3(0.07f, 0.012f, 0.07f), materials.Metal);
            CreateLocalCylinder(CrowbarGripWrapName + " Hook Neck Metal Collar", parent, new Vector3(0.05f, 0.53f, 0f), new Vector3(0.068f, 0.011f, 0.068f), materials.Metal);
        }

        private static void CreateCrowbarGlovedHands(Transform parent, Stage3Materials materials)
        {
            CreateLocalBox(CrowbarLowerGloveName + " Forearm Sleeve", parent, new Vector3(-0.26f, -0.86f, -0.07f), new Vector3(0.26f, 0.56f, 0.14f), materials.DarkRubber, Quaternion.Euler(0f, 0f, -24f));
            CreateLocalBox(CrowbarLowerGloveName + " Wrist Cuff", parent, new Vector3(-0.145f, -0.62f, -0.065f), new Vector3(0.25f, 0.085f, 0.12f), materials.Metal, Quaternion.Euler(0f, 0f, -16f));
            CreateLocalBox(CrowbarLowerGloveName + " Wrist Readout Plate", parent, new Vector3(-0.205f, -0.73f, -0.132f), new Vector3(0.205f, 0.095f, 0.024f), materials.Metal, Quaternion.Euler(0f, 0f, -18f));
            CreateLocalBox(CrowbarLowerGloveName + " Wrist Readout Screen", parent, new Vector3(-0.210f, -0.73f, -0.150f), new Vector3(0.155f, 0.058f, 0.014f), materials.Screen, Quaternion.Euler(0f, 0f, -18f));
            for (var i = 0; i < 4; i++)
            {
                CreateLocalBox(CrowbarLowerGloveName + " Wrist Readout Bar " + (i + 1), parent, new Vector3(-0.268f + (i * 0.038f), -0.765f, -0.162f), new Vector3(0.026f, 0.012f, 0.009f), materials.Light, Quaternion.Euler(0f, 0f, -18f));
            }

            CreateLocalBox(CrowbarLowerGloveName + " Palm", parent, new Vector3(-0.046f, -0.50f, -0.05f), new Vector3(0.22f, 0.14f, 0.105f), materials.DarkRubber, Quaternion.Euler(0f, 0f, -8f));
            CreateLocalBox(CrowbarLowerGloveName + " Thumb", parent, new Vector3(0.07f, -0.45f, -0.077f), new Vector3(0.088f, 0.052f, 0.045f), materials.DarkRubber, Quaternion.Euler(0f, 0f, 22f));
            CreateLocalBox(CrowbarLowerGloveName + " Finger Wrap", parent, new Vector3(0.052f, -0.515f, 0.06f), new Vector3(0.085f, 0.158f, 0.038f), materials.DarkRubber, Quaternion.Euler(0f, 0f, -12f));

            CreateLocalBox(CrowbarUpperGloveName + " Forearm Sleeve", parent, new Vector3(-0.22f, 0.08f, -0.065f), new Vector3(0.22f, 0.38f, 0.12f), materials.DarkRubber, Quaternion.Euler(0f, 0f, 22f));
            CreateLocalBox(CrowbarUpperGloveName + " Wrist Cuff", parent, new Vector3(-0.112f, -0.035f, -0.06f), new Vector3(0.21f, 0.076f, 0.105f), materials.Metal, Quaternion.Euler(0f, 0f, 14f));
            CreateLocalBox(CrowbarUpperGloveName + " Palm", parent, new Vector3(-0.045f, -0.145f, -0.05f), new Vector3(0.205f, 0.124f, 0.096f), materials.DarkRubber, Quaternion.Euler(0f, 0f, 10f));
            CreateLocalBox(CrowbarUpperGloveName + " Thumb", parent, new Vector3(0.058f, -0.09f, -0.074f), new Vector3(0.078f, 0.048f, 0.044f), materials.DarkRubber, Quaternion.Euler(0f, 0f, -22f));
            CreateLocalBox(CrowbarUpperGloveName + " Finger Wrap", parent, new Vector3(0.051f, -0.15f, 0.058f), new Vector3(0.078f, 0.148f, 0.036f), materials.DarkRubber, Quaternion.Euler(0f, 0f, 12f));
        }

        private static List<CrowbarSection> CreateCrowbarSections()
        {
            var sections = new List<CrowbarSection>();

            AppendCrowbarLineSections(
                sections,
                new Vector3(0.004f, -0.720f, 0f),
                new Vector3(0.000f, 0.420f, 0f),
                32,
                false,
                0.042f,
                0.036f,
                0.040f,
                0.036f);

            AppendCrowbarBezierSections(
                sections,
                new Vector3(0.000f, 0.420f, 0f),
                new Vector3(0.012f, 0.558f, 0f),
                new Vector3(0.118f, 0.670f, 0f),
                new Vector3(0.216f, 0.646f, 0f),
                18,
                true,
                0.036f,
                0.030f,
                0.036f,
                0.025f);

            AppendCrowbarBezierSections(
                sections,
                new Vector3(0.216f, 0.646f, 0f),
                new Vector3(0.314f, 0.620f, 0f),
                new Vector3(0.318f, 0.482f, 0f),
                new Vector3(0.260f, 0.402f, 0f),
                16,
                true,
                0.030f,
                0.006f,
                0.025f,
                0.003f);

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
                    material.SetColor("_EmissionColor", color * 2.2f);
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

        private static GameObject FindSceneObject(string objectName)
        {
            var existing = GameObject.Find(objectName);
            if (existing != null)
            {
                return existing;
            }

            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                {
                    return roots[i];
                }

                var child = FindChildRecursive(roots[i].transform, objectName);
                if (child != null)
                {
                    return child.gameObject;
                }
            }

            return null;
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
                Material warmLight,
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
                WarmLight = warmLight;
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

            public Material WarmLight { get; }

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
