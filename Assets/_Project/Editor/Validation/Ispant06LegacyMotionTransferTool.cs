using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.IspantCargoRunScene
{
    internal static class Ispant06LegacyMotionTransferTool
    {
        private const string SlotName = "Ispant_06_SheathSwordDrawMusket";
        private const string CurrentModelName = "Ispant_New_Direct_Model";
        private const string StaticSlotName = "Ispant_01_Static";
        private const string StaticModelName = "Ispant_New_Direct_Model";
        private const string SheathSourcePath = "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_SheathSword.fbx";
        private const string RifleSourcePath = "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_ChangeToRifle.fbx";
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string LegacyRecoveryScenePath = "Assets/_Recovery/0 (4).unity";
        private const string PlacementName = "Approved Ispant Enemy Placement";
        private const string ModelPath = "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_Direct_Source.fbx";
        private const string LegacyHoldPath = "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_06_SheathSword_StaticHold.anim";
        private const string LegacyBridgePath = "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_06_SheathToRifleBridge.anim";
        private const string LegacyRiflePath = "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_06_ChangeToRifle.anim";
        private const string OutputFolder = "Assets/_Project/Art/Enemies/Ispant/Animations";
        private const string ControllerFolder = "Assets/_Project/Art/Enemies/Ispant/Controllers";
        private const string NewSheathPath = OutputFolder + "/Ispant_06_New_SheathSword.anim";
        private const string NewHoldPath = OutputFolder + "/Ispant_06_New_SheathHold.anim";
        private const string NewBridgePath = OutputFolder + "/Ispant_06_New_SheathToRifleBridge.anim";
        private const string NewRiflePath = OutputFolder + "/Ispant_06_New_ChangeToRifle.anim";
        private const string NewControllerPath = ControllerFolder + "/Ispant_New_SheathToRifle.controller";
        private const string BodyWithoutMusketPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyWithoutBackMusket.asset";
        private const string RigidMusketPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_RigidMusket.asset";
        private const string BackMusketName = "Ispant_06_BackMusket";
        private const string HandMusketRootName = "Ispant_06_HandMusket";
        private const string HandMusketRendererName = "Ispant_06_HandMusket_Renderer";
        private const string LegacyHandSwordName = "Ispant_ApprovedLongSword_Renderer";
        private const string LegacyWaistSwordName =
            "Ispant_ApprovedLongSword_LeftWaist_Renderer";
        private const string CurrentHandSwordName = "Ispant_06_LegacyHandSword";
        private const string CurrentWaistSwordName = "Ispant_06_LegacyWaistSword";
        // 모델링 수정 전 장검의 길이와 손잡이 기준점을 현재 메시 인스턴스에 재현할 때만 사용합니다.
        private const float LegacySwordExpectedLength = 1.4374533f;
        private static readonly Vector3 LegacySwordGripCenter = new Vector3(0f, 0f, -0.103f);
        // 같은 현재 장식 장검을 잡는 4번에서 직접 시각 승인된 메시 내부 칼자루 중심입니다.
        private static readonly Vector3 ApprovedCurrentSwordGripLocal =
            new Vector3(0.00006910856f, -0.00027568708f, 0.000027271457f);
        private const string InspectionPath =
            "docs/validation/ispant_06_weapon_split_2026-08-21/Ispant_06_WeaponSplit_Inspection.txt";
        private const string LegacyReviewPath =
            "docs/validation/ispant_sheath_to_rifle_final_aim_arm_lift_revision_2026-08-09/" +
            "Ispant_06_SheathToRifle_FinalAimArmLift_FinalReview.png";
        private const string CapturePath =
            "docs/validation/ispant_06_legacy_sword_control_2026-08-21/" +
            "Ispant_06_LegacySwordControl_Comparison.png";
        private const string SwordGripReviewPath =
            "docs/validation/ispant_06_legacy_sword_control_2026-08-21/" +
            "Ispant_06_RightHandSwordGrip_Review.png";
        private const string SheathLeftArmReviewPath =
            "docs/validation/ispant_06_left_arm_static_pose_2026-08-21/" +
            "Ispant_06_SheathLeftArm_StaticPose_Review.png";
        private const string ComponentDiagnosticPath =
            "docs/validation/ispant_06_weapon_split_2026-08-21/" +
            "Ispant_06_MusketComponentGroups.png";
        private const string WeaponIdentityDiagnosticPath =
            "docs/validation/ispant_06_weapon_split_2026-08-21/" +
            "Ispant_06_WeaponIdentity.png";
        private const float FrameRate = 60f;
        private const float Tolerance = 0.001f;
        private const float GripDistanceFromPommelRatio = 0.13f;
        private const float GripHalfWidthRatio = 0.05f;
        private const float GripHandLongitudinalStartRatio = 0.5f;
        private static readonly int[] ExpectedMusketComponentSeeds =
        {
            183, 225, 231, 4312, 5056, 5502, 5520, 5694, 6290, 6606, 7538
        };
        private static readonly Quaternion LegacyHandSwordLocalRotation =
            new Quaternion(-0.46924952f, -0.2680889f, -0.43365252f, 0.7210261f);
        private static readonly string[] StaticLeftArmBoneNames =
        {
            "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand"
        };
        private static readonly BonePair[] BonePairs =
        {
            new BonePair("mixamorig:Hips", "Hips"),
            new BonePair("mixamorig:Spine", "Spine02"),
            new BonePair("mixamorig:Spine1", "Spine01"),
            new BonePair("mixamorig:Spine2", "Spine"),
            new BonePair("mixamorig:Neck", "neck"),
            new BonePair("mixamorig:Head", "Head"),
            new BonePair("mixamorig:LeftShoulder", "LeftShoulder"),
            new BonePair("mixamorig:LeftArm", "LeftArm"),
            new BonePair("mixamorig:LeftForeArm", "LeftForeArm"),
            new BonePair("mixamorig:LeftHand", "LeftHand"),
            new BonePair("mixamorig:RightShoulder", "RightShoulder"),
            new BonePair("mixamorig:RightArm", "RightArm"),
            new BonePair("mixamorig:RightForeArm", "RightForeArm"),
            new BonePair("mixamorig:RightHand", "RightHand"),
            new BonePair("mixamorig:LeftUpLeg", "LeftUpLeg"),
            new BonePair("mixamorig:LeftLeg", "LeftLeg"),
            new BonePair("mixamorig:LeftFoot", "LeftFoot"),
            new BonePair("mixamorig:LeftToeBase", "LeftToeBase"),
            new BonePair("mixamorig:RightUpLeg", "RightUpLeg"),
            new BonePair("mixamorig:RightLeg", "RightLeg"),
            new BonePair("mixamorig:RightFoot", "RightFoot"),
            new BonePair("mixamorig:RightToeBase", "RightToeBase")
        };
        private static readonly HumanMap[] HumanMaps =
        {
            new HumanMap(HumanBodyBones.Hips, "mixamorig:Hips", "Hips"),
            new HumanMap(HumanBodyBones.Spine, "mixamorig:Spine", "Spine02"),
            new HumanMap(HumanBodyBones.Chest, "mixamorig:Spine1", "Spine01"),
            new HumanMap(HumanBodyBones.UpperChest, "mixamorig:Spine2", "Spine"),
            new HumanMap(HumanBodyBones.Neck, "mixamorig:Neck", "neck"),
            new HumanMap(HumanBodyBones.Head, "mixamorig:Head", "Head"),
            new HumanMap(HumanBodyBones.LeftShoulder, "mixamorig:LeftShoulder", "LeftShoulder"),
            new HumanMap(HumanBodyBones.LeftUpperArm, "mixamorig:LeftArm", "LeftArm"),
            new HumanMap(HumanBodyBones.LeftLowerArm, "mixamorig:LeftForeArm", "LeftForeArm"),
            new HumanMap(HumanBodyBones.LeftHand, "mixamorig:LeftHand", "LeftHand"),
            new HumanMap(HumanBodyBones.RightShoulder, "mixamorig:RightShoulder", "RightShoulder"),
            new HumanMap(HumanBodyBones.RightUpperArm, "mixamorig:RightArm", "RightArm"),
            new HumanMap(HumanBodyBones.RightLowerArm, "mixamorig:RightForeArm", "RightForeArm"),
            new HumanMap(HumanBodyBones.RightHand, "mixamorig:RightHand", "RightHand"),
            new HumanMap(HumanBodyBones.LeftUpperLeg, "mixamorig:LeftUpLeg", "LeftUpLeg"),
            new HumanMap(HumanBodyBones.LeftLowerLeg, "mixamorig:LeftLeg", "LeftLeg"),
            new HumanMap(HumanBodyBones.LeftFoot, "mixamorig:LeftFoot", "LeftFoot"),
            new HumanMap(HumanBodyBones.LeftToes, "mixamorig:LeftToeBase", "LeftToeBase"),
            new HumanMap(HumanBodyBones.RightUpperLeg, "mixamorig:RightUpLeg", "RightUpLeg"),
            new HumanMap(HumanBodyBones.RightLowerLeg, "mixamorig:RightLeg", "RightLeg"),
            new HumanMap(HumanBodyBones.RightFoot, "mixamorig:RightFoot", "RightFoot"),
            new HumanMap(HumanBodyBones.RightToes, "mixamorig:RightToeBase", "RightToeBase")
        };
        private static readonly IReadOnlyDictionary<string, string> DirectionChildren =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Hips"] = "Spine02",
                ["Spine02"] = "Spine01",
                ["Spine01"] = "Spine",
                ["Spine"] = "neck",
                ["neck"] = "Head",
                ["LeftShoulder"] = "LeftArm",
                ["LeftArm"] = "LeftForeArm",
                ["LeftForeArm"] = "LeftHand",
                ["RightShoulder"] = "RightArm",
                ["RightArm"] = "RightForeArm",
                ["RightForeArm"] = "RightHand",
                ["LeftUpLeg"] = "LeftLeg",
                ["LeftLeg"] = "LeftFoot",
                ["LeftFoot"] = "LeftToeBase",
                ["RightUpLeg"] = "RightLeg",
                ["RightLeg"] = "RightFoot",
                ["RightFoot"] = "RightToeBase"
            };

        [MenuItem("Bellerophon/Enemies/Ispant/Diagnose Slot 06 Legacy Motion Transfer")]
        public static void DiagnoseIspant06LegacyMotionTransfer()
        {
            var slot = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(item => item.name == SlotName)?.gameObject ??
                throw new InvalidOperationException("The current scene does not contain " + SlotName + ".");
            if (slot.transform.childCount != 1)
                throw new InvalidOperationException(SlotName + " must contain exactly one model.");
            var current = slot.transform.GetChild(0).gameObject;
            if (current.name != CurrentModelName)
                throw new InvalidOperationException("Unexpected current slot-6 model: " + current.name + ".");

            var report = new StringBuilder();
            report.AppendLine("Ispant06LegacyMotionTransferDiagnosis Result=PASS");
            AppendModel(report, "Current", current);
            AppendAssetModel(report, "LegacySheathSource", SheathSourcePath);
            AppendAssetModel(report, "LegacyRifleSource", RifleSourcePath);
            Debug.Log(report.ToString());
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Diagnose Slot 06 Musket Components")]
        public static void DiagnoseIspant06MusketComponents()
        {
            var scene = RequireScene(true);
            var model = RequireCurrentModel(RequireSlot(scene)).transform;
            var body = RequireBody(RequireAsset<GameObject>(ModelPath).transform);
            var report = new StringBuilder("Ispant06MusketComponentDiagnosis Result=PASS\n");
            AppendMusketComponentCandidates(report, body, body.sharedMesh);
            Debug.Log(report.ToString());
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Diagnose Slot 06 Legacy Recovery Motion")]
        public static void DiagnoseIspant06LegacyRecoveryMotion()
        {
            var current = RequireScene(true);
            var recovery = EditorSceneManager.OpenScene(
                LegacyRecoveryScenePath, OpenSceneMode.Additive);
            try
            {
                var slot = recovery.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Single(item => item.name == SlotName);
                var report = new StringBuilder("Ispant06LegacyRecoveryMotionDiagnosis Result=PASS\n");
                report.AppendLine("RecoverySlotChildren=" + slot.childCount);
                foreach (Transform child in slot)
                {
                    report.AppendLine("RecoveryModel=" + child.name);
                    foreach (var renderer in child.GetComponentsInChildren<Renderer>(true))
                        report.AppendLine(
                            "RecoveryRenderer=" + Path(child, renderer.transform) +
                            "|Type=" + renderer.GetType().Name +
                            "|Enabled=" + renderer.enabled);
                }
                Debug.Log(report.ToString());
            }
            finally
            {
                EditorSceneManager.CloseScene(recovery, true);
                SceneManager.SetActiveScene(current);
            }
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Diagnose Slot 06 Weapon Alignment")]
        public static void DiagnoseIspant06WeaponAlignment()
        {
            var currentScene = RequireScene(true);
            var model = RequireCurrentModel(RequireSlot(currentScene)).transform;
            var body = RequireBody(model);
            var backMusket = model.GetComponentsInChildren<MeshRenderer>(true)
                .Single(item => item.name == BackMusketName);
            var handMusket = model.GetComponentsInChildren<MeshRenderer>(true)
                .Single(item => item.name == HandMusketRendererName);
            var rifle = RequireAsset<AnimationClip>(NewRiflePath);
            var legacyRifle = RequireAsset<AnimationClip>(LegacyRiflePath);
            var grabTime = FindLegacyGrabTime(legacyRifle);
            var recovery = EditorSceneManager.OpenScene(
                LegacyRecoveryScenePath, OpenSceneMode.Additive);
            try
            {
                var legacySlot = recovery.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Single(item => item.name == SlotName);
                var legacyModel = legacySlot.GetChild(0);
                var legacyBody = legacyModel.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Single(item => item.name == "Ispant_Armed_Body");
                var legacyBack = legacyModel.GetComponentsInChildren<MeshRenderer>(true)
                    .Single(item => item.name == "Ispant_Sheath_RigidMusket");
                var legacyHandMusket = legacyModel.GetComponentsInChildren<MeshRenderer>(true)
                    .Single(item => item.name == "Ispant_ChangeToRifle_HandMusket_Renderer");
                var currentRightHand = RequireBone(model, "RightHand");
                var currentLeftHand = RequireBone(model, "LeftHand");
                var legacyRightHand = RequireBone(legacyModel, "mixamorig:RightHand");
                var legacyLeftHand = RequireBone(legacyModel, "mixamorig:LeftHand");
                var currentStates = model.GetComponentsInChildren<Transform>(true)
                    .Select(item => new TransformState(item)).ToArray();
                var legacyStates = legacyModel.GetComponentsInChildren<Transform>(true)
                    .Select(item => new TransformState(item)).ToArray();
                var report = new StringBuilder("Ispant06WeaponAlignmentDiagnosis Result=PASS\n");
                try
                {
                    AnimationMode.StartAnimationMode();
                    foreach (var sample in new[]
                             {
                                 new KeyValuePair<string, float>("Grab", grabTime),
                                 new KeyValuePair<string, float>("Final", rifle.length)
                             })
                    {
                        Restore(currentStates);
                        Restore(legacyStates);
                        AnimationMode.SampleAnimationClip(model.gameObject, rifle, sample.Value);
                        AnimationMode.SampleAnimationClip(
                            legacyModel.gameObject, legacyRifle, sample.Value);
                        AppendWeaponAlignmentSample(
                            report, "Current" + sample.Key, model, body,
                            currentRightHand, currentLeftHand,
                            sample.Key == "Grab" ? backMusket : handMusket);
                        AppendWeaponAlignmentSample(
                            report, "Legacy" + sample.Key, legacyModel, legacyBody,
                            legacyRightHand, legacyLeftHand,
                            sample.Key == "Grab" ? legacyBack : legacyHandMusket);
                    }
                }
                finally
                {
                    if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                    Restore(currentStates);
                    Restore(legacyStates);
                }
                Debug.Log(report.ToString());
            }
            finally
            {
                EditorSceneManager.CloseScene(recovery, true);
                SceneManager.SetActiveScene(currentScene);
            }
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 06 Musket Component Groups")]
        public static void CaptureIspant06MusketComponentGroups()
        {
            var scene = RequireScene(true);
            var wasDirty = scene.isDirty;
            var directBody = RequireBody(RequireAsset<GameObject>(ModelPath).transform);
            var source = directBody.sharedMesh;
            var groups = new[]
            {
                new ComponentGroup("Selected19", ExpectedMusketComponentSeeds),
                new ComponentGroup("UpperLeft", new[] { 171, 5899, 8435 }),
                new ComponentGroup("CentralUpper", new[] { 4442, 5212 }),
                new ComponentGroup("RightUpper", new[] { 129, 3662, 4160 }),
                new ComponentGroup("ShoulderBand", new[] { 4406, 4916 }),
                new ComponentGroup("UpperCombined", new[]
                    { 171, 5899, 8435, 4442, 5212, 129, 3662, 4160, 4406, 4916 })
            };
            const int panelSize = 512;
            var target = new RenderTexture(panelSize, panelSize, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelSize, panelSize, TextureFormat.RGB24, false);
            var strip = new Texture2D(panelSize * groups.Length, panelSize, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("Ispant06MusketComponentCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.085f, 0.1f, 1f);
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.001f;
            camera.farClipPlane = 10f;
            camera.targetTexture = target;
            var oldActive = RenderTexture.active;
            var generated = new List<UnityEngine.Object>();
            try
            {
                for (var index = 0; index < groups.Length; index++)
                {
                    var vertices = ComponentVerticesBySeeds(source, groups[index].Seeds);
                    var mesh = CompactSubsetMesh(source, vertices, groups[index].Name);
                    generated.Add(mesh);
                    var item = new GameObject("Ispant06Component_" + groups[index].Name);
                    generated.Add(item);
                    item.AddComponent<MeshFilter>().sharedMesh = mesh;
                    item.AddComponent<MeshRenderer>().sharedMaterials = directBody.sharedMaterials;
                    FrameCamera(camera, mesh.bounds.center, Mathf.Max(mesh.bounds.size.y, 0.1f));
                    RenderPanel(camera, panel, strip, target, index, 0, panelSize, panelSize);
                    UnityEngine.Object.DestroyImmediate(item);
                    generated.Remove(item);
                }
                strip.Apply();
                var destination = Absolute(ComponentDiagnosticPath);
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, strip.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
                foreach (var item in generated.Where(item => item != null).ToArray())
                    UnityEngine.Object.DestroyImmediate(item);
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(strip);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("The component-group diagnostic changed the scene.");
            Debug.Log(
                "Ispant06MusketComponentGroupsCaptured Result=PASS, Order=" +
                string.Join(",", groups.Select(item => item.Name)) +
                ", Image=" + ComponentDiagnosticPath + ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 06 Weapon Identity")]
        public static void CaptureIspant06WeaponIdentity()
        {
            var scene = RequireScene(true);
            var wasDirty = scene.isDirty;
            var model = RequireCurrentModel(RequireSlot(scene)).transform;
            var clip = RequireAsset<AnimationClip>(NewRiflePath);
            var targets = new Renderer[]
            {
                model.GetComponentsInChildren<MeshRenderer>(true)
                    .Single(item => item.name == BackMusketName),
                model.GetComponentsInChildren<MeshRenderer>(true)
                    .Single(item => item.name == HandMusketRendererName),
                RequireSword(model)
            };
            var transforms = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var rendererStates = model.GetComponentsInChildren<Renderer>(true)
                .Select(item => new RendererState(item)).ToArray();
            const int panelSize = 512;
            var targetTexture = new RenderTexture(panelSize, panelSize, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelSize, panelSize, TextureFormat.RGB24, false);
            var strip = new Texture2D(panelSize * targets.Length, panelSize, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("Ispant06WeaponIdentityCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.085f, 0.1f, 1f);
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.001f;
            camera.farClipPlane = 100f;
            camera.targetTexture = targetTexture;
            var oldActive = RenderTexture.active;
            try
            {
                AnimationMode.StartAnimationMode();
                AnimationMode.SampleAnimationClip(model.gameObject, clip, 2.073506f);
                for (var index = 0; index < targets.Length; index++)
                {
                    foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                        renderer.enabled = renderer == targets[index];
                    FrameCamera(camera, targets[index].bounds.center,
                        Mathf.Max(targets[index].bounds.size.y, 0.1f));
                    RenderPanel(camera, panel, strip, targetTexture, index, 0, panelSize, panelSize);
                }
                strip.Apply();
                var destination = Absolute(WeaponIdentityDiagnosticPath);
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, strip.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(transforms);
                foreach (var state in rendererStates) state.Restore();
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(strip);
                targetTexture.Release();
                UnityEngine.Object.DestroyImmediate(targetTexture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("The weapon-identity diagnostic changed the scene.");
            Debug.Log(
                "Ispant06WeaponIdentityCaptured Result=PASS, Order=BackMusket,HandMusket,Sword" +
                ", Image=" + WeaponIdentityDiagnosticPath + ", SceneChanged=False.");
        }

        public static void StopPlayModeForIspant06Inspection()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.ExitPlaymode();
        }

        private static HashSet<int> ComponentVerticesBySeeds(Mesh mesh, IEnumerable<int> seeds)
        {
            var adjacency = Enumerable.Range(0, mesh.vertexCount)
                .Select(_ => new List<int>()).ToArray();
            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                var triangles = mesh.GetTriangles(subMesh);
                for (var index = 0; index < triangles.Length; index += 3)
                {
                    Connect(adjacency, triangles[index], triangles[index + 1]);
                    Connect(adjacency, triangles[index + 1], triangles[index + 2]);
                    Connect(adjacency, triangles[index + 2], triangles[index]);
                }
            }
            var result = new HashSet<int>();
            foreach (var seed in seeds.Distinct())
            {
                if (seed < 0 || seed >= mesh.vertexCount)
                    throw new InvalidOperationException("A component diagnostic seed is out of range: " + seed + ".");
                var stack = new Stack<int>();
                stack.Push(seed);
                while (stack.Count > 0)
                {
                    var vertex = stack.Pop();
                    if (!result.Add(vertex)) continue;
                    foreach (var neighbor in adjacency[vertex]) stack.Push(neighbor);
                }
            }
            return result;
        }

        private static Mesh CompactSubsetMesh(Mesh source, HashSet<int> selected, string name)
        {
            var trianglesBySubMesh = new List<int>[source.subMeshCount];
            var used = new SortedSet<int>();
            for (var subMesh = 0; subMesh < source.subMeshCount; subMesh++)
            {
                trianglesBySubMesh[subMesh] = new List<int>();
                var triangles = source.GetTriangles(subMesh);
                for (var index = 0; index < triangles.Length; index += 3)
                {
                    if (!selected.Contains(triangles[index]) ||
                        !selected.Contains(triangles[index + 1]) ||
                        !selected.Contains(triangles[index + 2])) continue;
                    trianglesBySubMesh[subMesh].Add(triangles[index]);
                    trianglesBySubMesh[subMesh].Add(triangles[index + 1]);
                    trianglesBySubMesh[subMesh].Add(triangles[index + 2]);
                    used.Add(triangles[index]);
                    used.Add(triangles[index + 1]);
                    used.Add(triangles[index + 2]);
                }
            }
            if (used.Count == 0)
                throw new InvalidOperationException("A component diagnostic group has no triangles: " + name + ".");
            var indices = used.ToArray();
            var remap = indices.Select((value, index) => new { value, index })
                .ToDictionary(item => item.value, item => item.index);
            var mesh = new Mesh
            {
                name = name,
                indexFormat = source.indexFormat,
                subMeshCount = source.subMeshCount,
                vertices = indices.Select(index => source.vertices[index]).ToArray()
            };
            if (source.normals.Length == source.vertexCount)
                mesh.normals = indices.Select(index => source.normals[index]).ToArray();
            if (source.tangents.Length == source.vertexCount)
                mesh.tangents = indices.Select(index => source.tangents[index]).ToArray();
            for (var channel = 0; channel < 8; channel++)
            {
                var values = new List<Vector4>();
                source.GetUVs(channel, values);
                if (values.Count == source.vertexCount)
                    mesh.SetUVs(channel, indices.Select(index => values[index]).ToList());
            }
            for (var subMesh = 0; subMesh < source.subMeshCount; subMesh++)
                mesh.SetTriangles(
                    trianglesBySubMesh[subMesh].Select(index => remap[index]).ToArray(),
                    subMesh,
                    false);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AppendWeaponAlignmentSample(
            StringBuilder report,
            string label,
            Transform model,
            SkinnedMeshRenderer body,
            Transform rightHand,
            Transform leftHand,
            MeshRenderer musket)
        {
            var mesh = musket.GetComponent<MeshFilter>().sharedMesh;
            var muzzleLocal = DetermineMusketLocalMuzzleAxis(mesh);
            var legacy = rightHand.name.StartsWith("mixamorig:", StringComparison.Ordinal);
            var rightPalm = rightHand.position;
            var leftPalm = leftHand.position;
            var nearestIndex = Enumerable.Range(0, mesh.vertexCount)
                .OrderBy(index => Vector3.Distance(
                    rightPalm, musket.transform.TransformPoint(mesh.vertices[index]))).First();
            var nearest = musket.transform.TransformPoint(mesh.vertices[nearestIndex]);
            var muzzleWorld = musket.transform.TransformDirection(muzzleLocal).normalized;
            var characterForwardWorld = model.TransformDirection(Vector3.forward).normalized;
            report.AppendLine(
                label +
                "|RightHandModel=" + Vec(ModelPosition(model, rightHand)) +
                "|RightPalmModel=" + Vec(model.InverseTransformPoint(rightPalm)) +
                "|NearestMusketModel=" + Vec(model.InverseTransformPoint(nearest)) +
                "|RightDistance=" + Num(Vector3.Distance(rightPalm, nearest)) +
                "|LeftHandModel=" + Vec(ModelPosition(model, leftHand)) +
                "|LeftPalmModel=" + Vec(model.InverseTransformPoint(leftPalm)) +
                "|MuzzleModel=" + Vec(model.InverseTransformDirection(muzzleWorld).normalized) +
                "|ForwardAngle=" + Num(Vector3.Angle(muzzleWorld, characterForwardWorld)));
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Diagnose Slot 06 Retarget Samples")]
        public static void DiagnoseIspant06RetargetSamples()
        {
            var scene = RequireScene(true);
            var current = RequireCurrentModel(RequireSlot(scene));
            var sourcePrefab = RequireAsset<GameObject>(SheathSourcePath);
            var source = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject ??
                throw new InvalidOperationException("Could not instantiate the legacy sample model.");
            source.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var report = new StringBuilder("Ispant06RetargetSampleDiagnosis Result=PASS\n");
                AppendClipSamples(report, "LegacySheath", source.transform,
                    RequireImportedClip(SheathSourcePath), true);
                AppendClipSamples(report, "LegacyBridge", source.transform,
                    RequireAsset<AnimationClip>(LegacyBridgePath), true);
                AppendClipSamples(report, "LegacyRifle", source.transform,
                    RequireAsset<AnimationClip>(LegacyRiflePath), true);
                AppendClipSamples(report, "CurrentSheath", current.transform,
                    RequireAsset<AnimationClip>(NewSheathPath), false);
                AppendClipSamples(report, "CurrentBridge", current.transform,
                    RequireAsset<AnimationClip>(NewBridgePath), false);
                AppendClipSamples(report, "CurrentRifle", current.transform,
                    RequireAsset<AnimationClip>(NewRiflePath), false);
                Debug.Log(report.ToString());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static void AppendClipSamples(
            StringBuilder report,
            string label,
            Transform model,
            AnimationClip clip,
            bool legacyNames)
        {
            var prefix = legacyNames ? "mixamorig:" : string.Empty;
            var names = new[] { "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand",
                "RightShoulder", "RightArm", "RightForeArm", "RightHand" };
            var bones = names.ToDictionary(name => name,
                name => RequireBone(model, prefix + name), StringComparer.Ordinal);
            var states = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var times = new[] { 0f, clip.length * 0.25f, clip.length * 0.5f,
                clip.length * 0.75f, clip.length };
            var baseline = new Dictionary<string, Quaternion>(StringComparer.Ordinal);
            try
            {
                AnimationMode.StartAnimationMode();
                for (var timeIndex = 0; timeIndex < times.Length; timeIndex++)
                {
                    Restore(states);
                    AnimationMode.SampleAnimationClip(model.gameObject, clip, times[timeIndex]);
                    if (timeIndex == 0)
                    {
                        foreach (var item in bones) baseline[item.Key] = item.Value.localRotation;
                    }
                    report.Append(label + "@" + Num(times[timeIndex]));
                    foreach (var item in bones)
                        report.Append("|" + item.Key + "=" +
                                      Num(Quaternion.Angle(baseline[item.Key], item.Value.localRotation)));
                    report.Append("|LeftHandModel=" + Vec(ModelPosition(model, bones["LeftHand"])));
                    report.Append("|RightHandModel=" + Vec(ModelPosition(model, bones["RightHand"])));
                    report.AppendLine();
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(states);
            }
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Legacy Motion Transfer")]
        public static void ApplyIspant06LegacyMotionTransfer()
        {
            var scene = RequireScene(true);
            var slot = RequireSlot(scene);
            var model = RequireCurrentModel(slot);
            var slotBefore = new TransformState(slot.transform);
            var otherSlotsBefore = OtherSlotSignatures(slot.transform.parent, slot.transform);
            var body = RequireBody(model.transform);
            var directBody = RequireBody(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath).transform);
            var originalMesh = directBody.sharedMesh;
            var materialsBefore = body.sharedMaterials.ToArray();
            if (body.sharedMesh != originalMesh &&
                AssetDatabase.GetAssetPath(body.sharedMesh) != BodyWithoutMusketPath)
                throw new InvalidOperationException(
                    "Slot 6 must use either the intact direct mesh or the approved slot-6 derived body mesh before transfer.");
            if (!materialsBefore.SequenceEqual(directBody.sharedMaterials))
                throw new InvalidOperationException("Slot 6 direct-model materials differ before transfer.");

            EnsureFolder(ControllerFolder);
            var legacySheath = RequireImportedClip(SheathSourcePath);
            var legacyHold = RequireAsset<AnimationClip>(LegacyHoldPath);
            var legacyBridge = RequireAsset<AnimationClip>(LegacyBridgePath);
            var legacyRifle = RequireAsset<AnimationClip>(LegacyRiflePath);
            var sourcePrefab = RequireAsset<GameObject>(SheathSourcePath);
            var targetPrefab = RequireAsset<GameObject>(ModelPath);
            var source = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject ??
                throw new InvalidOperationException("Could not instantiate the legacy slot-6 source.");
            var target = PrefabUtility.InstantiatePrefab(targetPrefab) as GameObject ??
                throw new InvalidOperationException("Could not instantiate the direct target model.");
            source.hideFlags = HideFlags.HideAndDontSave;
            target.hideFlags = HideFlags.HideAndDontSave;
            WaistSwordPose waistPose;
            try
            {
                var newSheath = RetargetClip(source.transform, target.transform, legacySheath,
                    NewSheathPath, "Ispant_06_New_SheathSword", null, out waistPose);
                var newHold = RetargetClip(source.transform, target.transform, legacyHold,
                    NewHoldPath, "Ispant_06_New_SheathHold", waistPose, out _);
                var newBridge = RetargetClip(source.transform, target.transform, legacyBridge,
                    NewBridgePath, "Ispant_06_New_SheathToRifleBridge", waistPose, out _);
                var newRifle = RetargetClip(source.transform, target.transform, legacyRifle,
                    NewRiflePath, "Ispant_06_New_ChangeToRifle", waistPose, out _);
                var controller = CreateController(newSheath, newHold, newBridge, newRifle);
                var animator = model.GetComponent<Animator>();
                if (animator == null)
                    animator = model.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.Normal;
                EditorUtility.SetDirty(animator);

                var partition = CreateOrUpdateMusketMeshPartition(originalMesh, directBody);
                body.sharedMesh = partition.BodyMesh;
                body.sharedMaterials = materialsBefore;
                var backMusket = CreateOrUpdateBackMusketRenderer(
                    model.transform, body, partition.MusketMesh, materialsBefore);
                ConfigureMusketAnimation(
                    model.transform, body, backMusket, newSheath, newHold, newBridge,
                    newRifle, legacyRifle);
                ConfigureLegacySwordControl(
                    model.transform, body, newSheath, newHold, newBridge, newRifle,
                    legacySheath, legacyHold);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
                UnityEngine.Object.DestroyImmediate(target);
            }

            if (!slotBefore.Matches(slot.transform, Tolerance))
                throw new InvalidOperationException("The slot-6 anchor changed during transfer.");
            if (AssetDatabase.GetAssetPath(body.sharedMesh) != BodyWithoutMusketPath ||
                !body.sharedMaterials.SequenceEqual(materialsBefore))
                throw new InvalidOperationException(
                    "The slot-6 derived body mesh or original materials differ after transfer.");
            RequireSame(otherSlotsBefore, OtherSlotSignatures(slot.transform.parent, slot.transform),
                "An Ispant slot outside slot 6 changed.");
            EditorUtility.SetDirty(model);
            EditorUtility.SetDirty(slot);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("CargoRunMvp could not be saved after slot-6 transfer.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = slot;
            Debug.Log(
                "Ispant06LegacyMotionTransferAppliedForVisualReview" +
                ", Sequence=LegacySheath->LegacyHold->LegacyBridge->LegacyChangeToRifle->Repeat" +
                ", CurrentModelPreserved=True, OriginalBodyMeshChanged=False, MaterialsChanged=False" +
                ", DerivedBodyWithoutBackMusket=True, RigidBackAndHandMusket=True" +
                ", Sword=LegacyRightHandMountThenWaistMount, Musket=LegacyTrajectory, RootMotion=False" +
                ", OtherSlotsChanged=False, SceneSaved=True, VisualVerdict=PendingUserReview.");
        }

        [MenuItem("Bellerophon/Scenes/Optimize CargoRun MVP Shadow Lights")]
        public static void OptimizeCargoRunMvpShadowLights()
        {
            var scene = RequireScene(true);
            var changed = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Light>(true))
                .Where(light => light.shadows != LightShadows.None)
                .ToArray();
            foreach (var light in changed)
            {
                light.shadows = LightShadows.None;
                EditorUtility.SetDirty(light);
            }
            if (changed.Length > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new InvalidOperationException(
                        "CargoRunMvp could not be saved after shadow-light optimization.");
            }
            Debug.Log(
                "CargoRunMvpShadowLightsOptimizedForEditorReview" +
                ", RealtimeShadowLightsDisabled=" + changed.Length +
                ", LightNames=" + string.Join("|", changed.Select(light => light.name)) +
                ", LightBrightnessColorAndRangePreserved=True, VisualVerdict=PendingUserReview.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Sword Grip Only")]
        public static void ApplyIspant06SwordGripOnly()
        {
            var scene = RequireScene(true);
            var model = RequireCurrentModel(RequireSlot(scene)).transform;
            var rightHand = RequireBone(model, "RightHand");
            var handSword = model.GetComponentsInChildren<MeshRenderer>(true)
                .Single(item => item.name == CurrentHandSwordName);
            if (handSword.transform.parent != rightHand)
                throw new InvalidOperationException(
                    "The current slot-6 hand sword is not parented to RightHand.");
            var sheath = RequireAsset<AnimationClip>(NewSheathPath);
            var handPath = Path(model, handSword.transform);
            ClearTransformCurves(sheath, handPath);
            var gripLocal = PalmAnchorLocal(RequireBody(model), rightHand);
            var swordGripLocal = CalculateGripCenter(
                handSword.GetComponent<MeshFilter>().sharedMesh);
            handSword.transform.localPosition = gripLocal -
                                                handSword.transform.localRotation * Vector3.Scale(
                                                    swordGripLocal,
                                                    handSword.transform.localScale);
            BakeRightHandSwordGripPositionCurves(
                model, RequireBody(model), sheath, handSword.transform, rightHand, handPath);
            EditorUtility.SetDirty(sheath);
            EditorUtility.SetDirty(handSword);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the slot-6 sword-grip-only update.");
            Debug.Log(
                "Ispant06SwordGripOnlyAppliedForVisualReview" +
                ", FullMotionRetargetRegenerated=False, NumericGripVerdictUsed=False" +
                ", SceneSaved=True, VisualVerdict=PendingUserReview.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Sheath Left Arm Static Pose")]
        public static void ApplyIspant06SheathLeftArmStaticPose()
        {
            var scene = RequireScene(true);
            var model = RequireCurrentModel(RequireSlot(scene)).transform;
            var staticModel = RequireStaticModel(scene);
            var sheath = RequireAsset<AnimationClip>(NewSheathPath);
            var pose = RetargetStaticLeftArmPose(staticModel, model);

            foreach (var name in StaticLeftArmBoneNames)
            {
                var bone = RequireBone(model, name);
                var path = Path(model, bone);
                var position = new VectorCurves();
                var rotation = new QuaternionCurves();
                position.Add(0f, pose[name].LocalPosition);
                position.Add(sheath.length, pose[name].LocalPosition);
                rotation.Add(0f, pose[name].LocalRotation);
                rotation.Add(sheath.length, pose[name].LocalRotation);
                position.Write(sheath, path);
                rotation.Write(sheath, path);
            }

            EditorUtility.SetDirty(sheath);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Ispant06SheathLeftArmStaticPoseAppliedForVisualReview" +
                ", Clip=Ispant_06_New_SheathSword" +
                ", Bones=LeftShoulder|LeftArm|LeftForeArm|LeftHand" +
                ", RightArmAndSwordCurvesChanged=False" +
                ", VisualVerdict=PendingUserReview.");
        }

        private static void ConfigureLegacySwordControl(
            Transform model,
            SkinnedMeshRenderer body,
            AnimationClip sheath,
            AnimationClip hold,
            AnimationClip bridge,
            AnimationClip rifle,
            AnimationClip legacySheath,
            AnimationClip legacyHold)
        {
            var currentScene = model.gameObject.scene;
            var recovery = EditorSceneManager.OpenScene(
                LegacyRecoveryScenePath, OpenSceneMode.Additive);
            try
            {
                var legacySlot = recovery.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Single(item => item.name == SlotName);
                if (legacySlot.childCount != 1)
                    throw new InvalidOperationException(
                        "The finalized legacy recovery slot-6 model differs.");
                var legacyModel = legacySlot.GetChild(0);
                var legacyHandSword = legacyModel.GetComponentsInChildren<MeshRenderer>(true)
                    .Single(item => item.name == LegacyHandSwordName);
                var legacyWaistSword = legacyModel.GetComponentsInChildren<MeshRenderer>(true)
                    .Single(item => item.name == LegacyWaistSwordName);
                var originalSword = RequireSword(model);
                _ = originalSword.GetComponent<MeshFilter>() ??
                    throw new InvalidOperationException("The current slot-6 sword mesh is missing.");
                var handSwordGrip = CalculateGripCenter(
                    originalSword.GetComponent<MeshFilter>().sharedMesh);
                var waistSwordGrip = ApprovedCurrentSwordGripLocal;
                var rightHand = RequireBone(model, "RightHand");
                var hips = RequireBone(model, "Hips");
                var sourceBones = BonePairs.ToDictionary(
                    item => item.Target,
                    item => RequireBone(legacyModel, item.Source), StringComparer.Ordinal);
                var targetBones = BonePairs.ToDictionary(
                    item => item.Target,
                    item => RequireBone(model, item.Target), StringComparer.Ordinal);
                var sourceBody = legacyModel.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Single(item => item.name == "Ispant_Armed_Body");
                var sourceBind = sourceBones.ToDictionary(
                    item => item.Key,
                    item => BindModelMatrix(legacyModel, sourceBody, item.Value),
                    StringComparer.Ordinal);
                var targetBind = targetBones.ToDictionary(
                    item => item.Key,
                    item => BindModelMatrix(model, body, item.Value),
                    StringComparer.Ordinal);
                var sourceHeight = Vector3.Distance(
                    sourceBind["Hips"].GetPosition(), sourceBind["Head"].GetPosition());
                var targetHeight = Vector3.Distance(
                    targetBind["Hips"].GetPosition(), targetBind["Head"].GetPosition());
                var scale = targetHeight / sourceHeight;
                var frameMap = CharacterFrame(targetBind) *
                               Quaternion.Inverse(CharacterFrame(sourceBind));
                var sourceStates = legacyModel.GetComponentsInChildren<Transform>(true)
                    .Select(item => new TransformState(item)).ToArray();
                var targetStates = model.GetComponentsInChildren<Transform>(true)
                    .Select(item => new TransformState(item)).ToArray();
                Vector3 handGripLocal;
                Quaternion handLocalRotation;
                Vector3 waistPosition;
                Quaternion waistRotation;
                try
                {
                    AnimationMode.StartAnimationMode();

                    Restore(sourceStates);
                    Restore(targetStates);
                    AnimationMode.SampleAnimationClip(
                        legacyModel.gameObject, legacySheath, 0f);
                    AnimationMode.SampleAnimationClip(model.gameObject, sheath, 0f);
                    var handWorldRotation = MapLegacySwordRotation(
                        legacyModel, model, legacyHandSword.transform, frameMap);
                    handLocalRotation =
                        Quaternion.Inverse(rightHand.rotation) * handWorldRotation;
                    handGripLocal = PalmAnchorLocal(body, rightHand);

                    Restore(sourceStates);
                    Restore(targetStates);
                    AnimationMode.SampleAnimationClip(
                        legacyModel.gameObject, legacyHold, 0f);
                    AnimationMode.SampleAnimationClip(model.gameObject, hold, 0f);
                    waistRotation = MapLegacySwordRotation(
                        legacyModel, model, legacyWaistSword.transform, frameMap);
                    var sourceGrip = LegacySwordGripCenter *
                                     (legacyWaistSword.GetComponent<MeshFilter>().sharedMesh.bounds.size.z /
                                      LegacySwordExpectedLength);
                    var sourceGripModel = legacyModel.InverseTransformPoint(
                        legacyWaistSword.transform.TransformPoint(sourceGrip));
                    var sourceHipsModel = ModelPosition(legacyModel, sourceBones["Hips"]);
                    var targetHipsModel = ModelPosition(model, targetBones["Hips"]);
                    var targetGripModel = targetHipsModel +
                                          frameMap * (sourceGripModel - sourceHipsModel) * scale;
                    var targetGripWorld = model.TransformPoint(targetGripModel);
                    waistPosition = targetGripWorld - waistRotation * Vector3.Scale(
                        waistSwordGrip, originalSword.transform.lossyScale);
                }
                finally
                {
                    if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                    Restore(sourceStates);
                    Restore(targetStates);
                }

                RemoveExistingSwordClone(model, CurrentHandSwordName);
                RemoveExistingSwordClone(model, CurrentWaistSwordName);
                var handSword = CreateCurrentHandSwordClone(
                    CurrentHandSwordName, rightHand, originalSword, body,
                    handGripLocal, handSwordGrip, handLocalRotation, true);
                foreach (var swordFollower in model.GetComponents<
                             Bellerophon.Enemies.Ispant.IspantRigidSwordFollower>())
                    UnityEngine.Object.DestroyImmediate(swordFollower);
                var waistSword = CreateCurrentSwordClone(
                    CurrentWaistSwordName, hips, originalSword, body,
                    waistPosition, waistRotation, false);
                originalSword.enabled = false;
                EditorUtility.SetDirty(originalSword);

                var originalPath = Path(model, originalSword.transform);
                foreach (var clip in new[] { sheath, hold, bridge, rifle })
                {
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                                 .Where(item => item.path == originalPath &&
                                                item.type == typeof(Transform)).ToArray())
                        AnimationUtility.SetEditorCurve(clip, binding, null);
                }

                var handPath = Path(model, handSword.transform);
                var waistPath = Path(model, waistSword.transform);
                ClearTransformCurves(sheath, handPath);
                BakeRightHandSwordGripPositionCurves(
                    model, body, sheath, handSword.transform, rightHand, handPath);
                SetConstantRendererEnabledCurve(sheath, handPath, true, sheath.length);
                SetConstantRendererEnabledCurve(sheath, waistPath, false, sheath.length);
                foreach (var clip in new[] { hold, bridge, rifle })
                {
                    SetConstantRendererEnabledCurve(clip, handPath, false, clip.length);
                    SetConstantRendererEnabledCurve(clip, waistPath, true, clip.length);
                    EditorUtility.SetDirty(clip);
                }
                EditorUtility.SetDirty(sheath);
                EditorUtility.SetDirty(handSword);
                EditorUtility.SetDirty(waistSword);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorSceneManager.CloseScene(recovery, true);
                SceneManager.SetActiveScene(currentScene);
            }
        }

        private static void ClearTransformCurves(AnimationClip clip, string path)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                         .Where(item => item.path == path && item.type == typeof(Transform))
                         .ToArray())
                AnimationUtility.SetEditorCurve(clip, binding, null);
        }

        private static void BakeRightHandSwordGripPositionCurves(
            Transform model,
            SkinnedMeshRenderer body,
            AnimationClip sheath,
            Transform handSword,
            Transform rightHand,
            string handPath)
        {
            var transforms = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var position = new VectorCurves();
            var swordGripLocal = CalculateGripCenter(
                handSword.GetComponent<MeshFilter>().sharedMesh);
            try
            {
                AnimationMode.StartAnimationMode();
                var frameCount = Mathf.RoundToInt(sheath.length * sheath.frameRate);
                for (var frame = 0; frame <= frameCount; frame++)
                {
                    Restore(transforms);
                    var time = frame / sheath.frameRate;
                    AnimationMode.SampleAnimationClip(model.gameObject, sheath, time);
                    var visiblePalm = rightHand.TransformPoint(PalmAnchorLocal(body, rightHand));
                    handSword.position = visiblePalm - handSword.TransformVector(swordGripLocal);
                    position.Add(time, handSword.localPosition);
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(transforms);
            }
            position.Write(sheath, handPath);
            EditorUtility.SetDirty(sheath);
        }

        private static Quaternion MapLegacySwordRotation(
            Transform legacyModel,
            Transform model,
            Transform legacySword,
            Quaternion frameMap)
        {
            var sourceBladeModel = legacyModel.InverseTransformDirection(
                legacySword.TransformDirection(Vector3.forward)).normalized;
            var sourceRollModel = legacyModel.InverseTransformDirection(
                legacySword.TransformDirection(Vector3.up)).normalized;
            var bladeWorld = model.TransformDirection(frameMap * sourceBladeModel).normalized;
            var rollWorld = Vector3.ProjectOnPlane(
                model.TransformDirection(frameMap * sourceRollModel), bladeWorld).normalized;
            var localBasis = Quaternion.LookRotation(Vector3.right, Vector3.up);
            var worldBasis = Quaternion.LookRotation(bladeWorld, rollWorld);
            return worldBasis * Quaternion.Inverse(localBasis);
        }

        private static MeshRenderer CreateCurrentSwordClone(
            string name,
            Transform parent,
            MeshRenderer source,
            Renderer settingsSource,
            Vector3 position,
            Quaternion rotation,
            bool enabled)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            item.transform.SetPositionAndRotation(position, rotation);
            var parentScale = parent.lossyScale;
            var sourceScale = source.transform.lossyScale;
            item.transform.localScale = new Vector3(
                sourceScale.x / parentScale.x,
                sourceScale.y / parentScale.y,
                sourceScale.z / parentScale.z);
            var filter = item.AddComponent<MeshFilter>();
            var renderer = item.AddComponent<MeshRenderer>();
            filter.sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
            renderer.sharedMaterials = source.sharedMaterials;
            renderer.enabled = enabled;
            CopyRendererSettings(settingsSource, renderer);
            return renderer;
        }

        private static MeshRenderer CreateCurrentHandSwordClone(
            string name,
            Transform parent,
            MeshRenderer source,
            Renderer settingsSource,
            Vector3 handGripLocal,
            Vector3 swordGripLocal,
            Quaternion localRotation,
            bool enabled)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            item.transform.localRotation = localRotation;
            var parentScale = parent.lossyScale;
            var sourceScale = source.transform.lossyScale;
            item.transform.localScale = new Vector3(
                sourceScale.x / parentScale.x,
                sourceScale.y / parentScale.y,
                sourceScale.z / parentScale.z);
            item.transform.localPosition = handGripLocal -
                                           localRotation * Vector3.Scale(
                                               swordGripLocal, item.transform.localScale);
            var filter = item.AddComponent<MeshFilter>();
            var renderer = item.AddComponent<MeshRenderer>();
            filter.sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
            renderer.sharedMaterials = source.sharedMaterials;
            renderer.enabled = enabled;
            CopyRendererSettings(settingsSource, renderer);
            return renderer;
        }

        private static void RemoveExistingSwordClone(Transform model, string name)
        {
            var existing = model.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(item => item.name == name);
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Slot 06 Legacy Motion Transfer")]
        public static void InspectIspant06LegacyMotionTransfer()
        {
            var scene = RequireScene(true);
            var wasDirty = scene.isDirty;
            var slot = RequireSlot(scene);
            var model = RequireCurrentModel(slot).transform;
            var clips = new[]
            {
                RequireAsset<AnimationClip>(NewSheathPath),
                RequireAsset<AnimationClip>(NewHoldPath),
                RequireAsset<AnimationClip>(NewBridgePath),
                RequireAsset<AnimationClip>(NewRiflePath)
            };
            var legacy = new[]
            {
                RequireImportedClip(SheathSourcePath),
                RequireAsset<AnimationClip>(LegacyHoldPath),
                RequireAsset<AnimationClip>(LegacyBridgePath),
                RequireAsset<AnimationClip>(LegacyRiflePath)
            };
            var animator = model.GetComponent<Animator>() ??
                throw new InvalidOperationException("Slot 6 Animator is missing.");
            if (AssetDatabase.GetAssetPath(animator.runtimeAnimatorController) != NewControllerPath ||
                animator.applyRootMotion)
                throw new InvalidOperationException("Slot 6 controller or root-motion setting differs.");
            for (var index = 0; index < clips.Length; index++)
            {
                if (Mathf.Abs(clips[index].length - legacy[index].length) > 0.0001f)
                    throw new InvalidOperationException("Retargeted clip duration differs at index " + index + ".");
            }
            var body = RequireBody(model);
            var directBody = RequireBody(RequireAsset<GameObject>(ModelPath).transform);
            var bodyWithoutMusket = RequireAsset<Mesh>(BodyWithoutMusketPath);
            var rigidMusket = RequireAsset<Mesh>(RigidMusketPath);
            if (body.sharedMesh != bodyWithoutMusket ||
                !body.sharedMaterials.SequenceEqual(directBody.sharedMaterials))
                throw new InvalidOperationException("The derived slot-6 body mesh or original materials differ.");
            var selectedVertices = SelectMusketVertices(directBody.sharedMesh, directBody);
            var originalTriangles = TriangleCount(directBody.sharedMesh);
            var bodyTriangles = TriangleCount(bodyWithoutMusket);
            var musketTriangles = TriangleCount(rigidMusket);
            if (directBody.sharedMesh.vertexCount != bodyWithoutMusket.vertexCount ||
                rigidMusket.vertexCount != selectedVertices.Count ||
                originalTriangles != bodyTriangles + musketTriangles)
                throw new InvalidOperationException(
                    "The slot-6 body and rigid-musket derived meshes do not exactly partition the original triangles.");
            var backMusket = model.GetComponentsInChildren<MeshRenderer>(true)
                .Single(item => item.name == BackMusketName);
            var handMusket = model.GetComponentsInChildren<MeshRenderer>(true)
                .Single(item => item.name == HandMusketRendererName);
            if (backMusket.GetComponent<MeshFilter>().sharedMesh != rigidMusket ||
                handMusket.GetComponent<MeshFilter>().sharedMesh != rigidMusket ||
                !backMusket.sharedMaterials.SequenceEqual(directBody.sharedMaterials) ||
                !handMusket.sharedMaterials.SequenceEqual(directBody.sharedMaterials) ||
                backMusket.transform.parent == null || backMusket.transform.parent.name != "LeftShoulder" ||
                handMusket.transform.parent == null ||
                handMusket.transform.parent.name != HandMusketRootName ||
                handMusket.transform.parent.parent == null ||
                handMusket.transform.parent.parent.name != "RightHand")
                throw new InvalidOperationException("The slot-6 back/hand musket renderer structure differs.");
            var sword = RequireSword(model);
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var rendererStates = model.GetComponentsInChildren<Renderer>(true)
                .Select(item => new RendererState(item)).ToArray();
            var maximumGripError = 0f;
            var maximumVertexMagnitude = 0f;
            var baked = new Mesh();
            try
            {
                AnimationMode.StartAnimationMode();
                for (var clipIndex = 0; clipIndex < clips.Length; clipIndex++)
                {
                    var frameCount = Mathf.Max(1, Mathf.RoundToInt(clips[clipIndex].length * FrameRate));
                    for (var frame = 0; frame <= frameCount; frame++)
                    {
                        Restore(snapshots);
                        AnimationMode.SampleAnimationClip(model.gameObject, clips[clipIndex],
                            clips[clipIndex].length * frame / frameCount);
                        body.BakeMesh(baked);
                        foreach (var vertex in baked.vertices)
                        {
                            if (!Finite(vertex))
                                throw new InvalidOperationException("Slot-6 body contains a non-finite vertex.");
                            maximumVertexMagnitude = Mathf.Max(maximumVertexMagnitude, vertex.magnitude);
                        }
                        if (clipIndex == 0)
                        {
                            var hand = RequireBone(model, "RightHand");
                            var palm = hand.TransformPoint(PalmAnchorLocal(body, hand));
                            var grip = sword.transform.TransformPoint(CalculateGripCenter(
                                sword.GetComponent<MeshFilter>().sharedMesh));
                            maximumGripError = Mathf.Max(maximumGripError, Vector3.Distance(palm, grip));
                        }
                    }
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(snapshots);
                foreach (var state in rendererStates) state.Restore();
                UnityEngine.Object.DestroyImmediate(baked);
            }
            if (maximumGripError > 0.005f || maximumVertexMagnitude > 10f)
                throw new InvalidOperationException(
                    "Slot-6 grip or body bounds inspection failed. Grip=" + maximumGripError +
                    ", VertexMagnitude=" + maximumVertexMagnitude + ".");
            var shoulderMetrics = InspectShoulderPositionRetarget(model, clips, legacy);
            var musketMetrics = InspectMusketAnimation(
                model, body, backMusket, handMusket, clips, legacy[3]);
            var report = new[]
            {
                "Ispant06WeaponSplitInspection InternalDiagnosticsCompleted=True, VisualVerdict=NotEvaluated",
                "Target=" + PlacementName + "/" + SlotName + "/" + CurrentModelName,
                "LegacyDurations=" + string.Join(",", legacy.Select(item => Num(item.length))),
                "RetargetDurations=" + string.Join(",", clips.Select(item => Num(item.length))),
                "MappedBones=" + BonePairs.Length,
                "MaximumSwordGripErrorMeters=" + Num(maximumGripError),
                "MaximumBakedVertexMagnitudeMeters=" + Num(maximumVertexMagnitude),
                "MaximumShoulderPositionRetargetErrorMeters=" +
                Num(shoulderMetrics.MaximumRetargetError),
                "FinalLeftShoulderTranslationMeters=" +
                Num(shoulderMetrics.FinalLeftTranslation),
                "FinalRightShoulderTranslationMeters=" +
                Num(shoulderMetrics.FinalRightTranslation),
                "ShoulderPositionCurvesPresent=True",
                "OriginalBodyVertices=" + directBody.sharedMesh.vertexCount,
                "DerivedBodyVertices=" + bodyWithoutMusket.vertexCount,
                "RigidMusketVertices=" + rigidMusket.vertexCount,
                "OriginalTriangles=" + originalTriangles,
                "DerivedBodyTriangles=" + bodyTriangles,
                "RigidMusketTriangles=" + musketTriangles,
                "MaximumBackToHandContinuityErrorMeters=" + Num(musketMetrics.ContinuityError),
                "MaximumRightGripSurfaceDistanceMeters=" + Num(musketMetrics.MaximumRightGripDistance),
                "FinalLeftForegripSurfaceDistanceMeters=" + Num(musketMetrics.FinalLeftForegripDistance),
                "MaximumLegacyTrajectoryAngleErrorDegrees=" + Num(musketMetrics.MaximumTrajectoryAngleError),
                "FinalMuzzleForwardAngleDegrees=" + Num(musketMetrics.FinalMuzzleForwardAngle),
                "OriginalBodyMeshUnchanged=True",
                "DerivedBodyWithoutBackMusket=True",
                "BackToHandVisibilityExclusive=True",
                "RigidMusketMeshSharedByBackAndHand=True",
                "MaterialsUnchanged=True",
                "RootMotion=False",
                "ControllerSequence=Sheath->Hold->Bridge->ChangeToRifle->ImmediateRepeat",
                "SceneChanged=False"
            };
            var absolute = Absolute(InspectionPath);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(absolute));
            File.WriteAllLines(absolute, report);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("The slot-6 inspection changed the scene dirty state.");
            Debug.Log(string.Join(", ", report));
        }

        private static ShoulderPositionMetrics InspectShoulderPositionRetarget(
            Transform target,
            IReadOnlyList<AnimationClip> clips,
            IReadOnlyList<AnimationClip> legacy)
        {
            var sourcePrefab = RequireAsset<GameObject>(SheathSourcePath);
            var sourceObject = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject ??
                throw new InvalidOperationException("Could not instantiate the legacy slot-6 source for inspection.");
            sourceObject.hideFlags = HideFlags.HideAndDontSave;
            var source = sourceObject.transform;
            var translatedBoneNames = new[] { "LeftShoulder", "RightShoulder" };
            var sourceBones = translatedBoneNames.ToDictionary(
                name => name,
                name => RequireBone(source, BonePairs.Single(pair => pair.Target == name).Source),
                StringComparer.Ordinal);
            var targetBones = translatedBoneNames.ToDictionary(
                name => name, name => RequireBone(target, name), StringComparer.Ordinal);
            var sourceRestLocalPositions = translatedBoneNames.ToDictionary(
                name => name, name => sourceBones[name].localPosition, StringComparer.Ordinal);
            var targetRestLocalPositions = translatedBoneNames.ToDictionary(
                name => name, name => targetBones[name].localPosition, StringComparer.Ordinal);
            var sourceBody = source.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "Ispant_Armed_Body");
            var targetBody = RequireBody(target);
            var sourceBind = BonePairs.ToDictionary(
                item => item.Target,
                item => BindModelMatrix(source, sourceBody, RequireBone(source, item.Source)),
                StringComparer.Ordinal);
            var targetBind = BonePairs.ToDictionary(
                item => item.Target,
                item => BindModelMatrix(target, targetBody, RequireBone(target, item.Target)),
                StringComparer.Ordinal);
            var sourceHeight = Vector3.Distance(
                sourceBind["Hips"].GetPosition(), sourceBind["Head"].GetPosition());
            var targetHeight = Vector3.Distance(
                targetBind["Hips"].GetPosition(), targetBind["Head"].GetPosition());
            var scale = targetHeight / sourceHeight;
            var frameMap = CharacterFrame(targetBind) *
                           Quaternion.Inverse(CharacterFrame(sourceBind));
            var sourceStates = source.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var targetStates = target.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var maximumError = 0f;
            var finalTranslations = translatedBoneNames.ToDictionary(
                name => name, _ => 0f, StringComparer.Ordinal);
            try
            {
                for (var clipIndex = 0; clipIndex < clips.Count; clipIndex++)
                {
                    foreach (var name in translatedBoneNames)
                    {
                        var path = Path(target, targetBones[name]);
                        var properties = AnimationUtility.GetCurveBindings(clips[clipIndex])
                            .Where(binding => binding.path == path &&
                                              binding.propertyName.StartsWith("m_LocalPosition.",
                                                  StringComparison.Ordinal))
                            .Select(binding => binding.propertyName)
                            .ToHashSet(StringComparer.Ordinal);
                        if (!properties.SetEquals(new[]
                            {
                                "m_LocalPosition.x",
                                "m_LocalPosition.y",
                                "m_LocalPosition.z"
                            }))
                            throw new InvalidOperationException(
                                "Slot-6 shoulder position curves are missing for " +
                                clips[clipIndex].name + "/" + name + ".");
                    }

                    var frameCount = Mathf.Max(1,
                        Mathf.RoundToInt(clips[clipIndex].length * FrameRate));
                    for (var frame = 0; frame <= frameCount; frame++)
                    {
                        Restore(sourceStates);
                        Restore(targetStates);
                        var time = clips[clipIndex].length * frame / frameCount;
                        AnimationMode.StartAnimationMode();
                        AnimationMode.SampleAnimationClip(
                            source.gameObject, legacy[clipIndex], time);
                        AnimationMode.SampleAnimationClip(
                            target.gameObject, clips[clipIndex], time);
                        foreach (var name in translatedBoneNames)
                        {
                            var sourceLocalDelta = sourceBones[name].localPosition -
                                                   sourceRestLocalPositions[name];
                            var sourceModelDelta = source.InverseTransformVector(
                                sourceBones[name].parent.TransformVector(sourceLocalDelta));
                            var targetModelDelta = frameMap * sourceModelDelta * scale;
                            var expectedTargetDelta =
                                targetBones[name].parent.InverseTransformVector(
                                    target.TransformVector(targetModelDelta));
                            var actualTargetDelta = targetBones[name].localPosition -
                                                    targetRestLocalPositions[name];
                            maximumError = Mathf.Max(maximumError,
                                Vector3.Distance(expectedTargetDelta, actualTargetDelta));
                            if (clipIndex == clips.Count - 1 && frame == frameCount)
                                finalTranslations[name] = actualTargetDelta.magnitude;
                        }
                        AnimationMode.StopAnimationMode();
                    }
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(sourceStates);
                Restore(targetStates);
                UnityEngine.Object.DestroyImmediate(sourceObject);
            }
            if (maximumError > 0.0001f)
                throw new InvalidOperationException(
                    "Slot-6 shoulder-position retarget inspection failed. Error=" +
                    Num(maximumError) + ".");
            return new ShoulderPositionMetrics(
                maximumError,
                finalTranslations["LeftShoulder"],
                finalTranslations["RightShoulder"]);
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 06 Legacy Motion Comparison")]
        public static void CaptureIspant06LegacyMotionComparison()
        {
            var scene = RequireScene(true);
            var wasDirty = scene.isDirty;
            var model = RequireCurrentModel(RequireSlot(scene)).transform;
            var body = RequireBody(model);
            var sheath = RequireAsset<AnimationClip>(NewSheathPath);
            var hold = RequireAsset<AnimationClip>(NewHoldPath);
            var bridge = RequireAsset<AnimationClip>(NewBridgePath);
            var rifle = RequireAsset<AnimationClip>(NewRiflePath);
            var destination = Absolute(CapturePath);
            // A failed visual comparison is replaced in place; only one final evidence file remains.
            var referenceBytes = File.ReadAllBytes(Absolute(LegacyReviewPath));
            var reference = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!reference.LoadImage(referenceBytes, false) || reference.width != 5632 || reference.height != 768)
                throw new InvalidOperationException("The legacy final review dimensions differ.");

            const int panelWidth = 512;
            const int panelHeight = 768;
            const int panelCount = 11;
            var targetTexture = new RenderTexture(panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelWidth, panelHeight, TextureFormat.RGB24, false);
            var comparison = new Texture2D(
                panelWidth * panelCount, panelHeight * 2, TextureFormat.RGB24, false);
            comparison.SetPixels32(0, panelHeight, reference.width, reference.height, reference.GetPixels32());
            var cameraObject = new GameObject("Ispant06LegacyMotionComparisonCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.085f, 0.1f, 1f);
            camera.fieldOfView = 34f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.targetTexture = targetTexture;
            var oldActive = RenderTexture.active;
            var transforms = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var renderers = model.GetComponentsInChildren<Renderer>(true)
                .Select(item => new RendererState(item)).ToArray();
            try
            {
                AnimationMode.StartAnimationMode();
                var samples = new[]
                {
                    new ClipSample(null, 0f),
                    new ClipSample(sheath, sheath.length),
                    new ClipSample(hold, hold.length * 0.5f),
                    new ClipSample(bridge, bridge.length * 0.25f),
                    new ClipSample(bridge, bridge.length * 0.5f),
                    new ClipSample(bridge, bridge.length * 0.75f),
                    new ClipSample(bridge, bridge.length),
                    new ClipSample(rifle, Mathf.Max(0f, 1.327044f - 0.5f / FrameRate)),
                    new ClipSample(rifle, 1.327044f),
                    new ClipSample(rifle, 2.073506f),
                    new ClipSample(rifle, rifle.length)
                };
                for (var index = 0; index < samples.Length; index++)
                {
                    Restore(transforms);
                    if (samples[index].Clip != null)
                        AnimationMode.SampleAnimationClip(
                            model.gameObject, samples[index].Clip, samples[index].Time);
                    FrameCamera(camera, body.bounds.center, body.bounds.size.y);
                    RenderPanel(camera, panel, comparison, targetTexture, index, 0,
                        panelWidth, panelHeight);
                }
                comparison.Apply();
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, comparison.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(transforms);
                foreach (var renderer in renderers) renderer.Restore();
                UnityEngine.Object.DestroyImmediate(reference);
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(comparison);
                targetTexture.Release();
                UnityEngine.Object.DestroyImmediate(targetTexture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("The slot-6 comparison changed the scene dirty state.");
            Debug.Log(
                "Ispant06LegacyMotionComparisonCaptured" +
                ", Top=LegacyFinal11Phases, Bottom=CurrentRetargetSame11Phases" +
                ", Image=" + CapturePath +
                ", SceneChanged=False, VisualVerdict=PendingUserReview.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 06 Right-Hand Sword Grip Review")]
        public static void CaptureIspant06SwordGripReview()
        {
            var scene = RequireScene(true);
            var wasDirty = scene.isDirty;
            var model = RequireCurrentModel(RequireSlot(scene)).transform;
            var body = RequireBody(model);
            var rightHand = RequireBone(model, "RightHand");
            var handSword = model.GetComponentsInChildren<MeshRenderer>(true)
                .Single(item => item.name == CurrentHandSwordName);
            var sheath = RequireAsset<AnimationClip>(NewSheathPath);
            var destination = Absolute(SwordGripReviewPath);
            const int panelSize = 768;
            const int panelCount = 4;
            var targetTexture = new RenderTexture(
                panelSize, panelSize, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelSize, panelSize, TextureFormat.RGB24, false);
            var strip = new Texture2D(
                panelSize * panelCount, panelSize, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("Ispant06SwordGripReviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.085f, 0.1f, 1f);
            camera.fieldOfView = 24f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.targetTexture = targetTexture;
            const int gripCaptureLayer = 30;
            camera.cullingMask = 1 << gripCaptureLayer;
            var oldActive = RenderTexture.active;
            var transforms = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var layerStates = model.GetComponentsInChildren<Transform>(true)
                .ToDictionary(item => item, item => item.gameObject.layer);
            foreach (var item in layerStates.Keys)
                item.gameObject.layer = gripCaptureLayer;
            var renderers = model.GetComponentsInChildren<Renderer>(true)
                .Select(item => new RendererState(item)).ToArray();
            try
            {
                AnimationMode.StartAnimationMode();
                var samples = new[]
                {
                    sheath.length * 0.2f,
                    sheath.length * 0.45f,
                    sheath.length * 0.7f,
                    sheath.length * 0.95f
                };
                for (var index = 0; index < samples.Length; index++)
                {
                    Restore(transforms);
                    AnimationMode.SampleAnimationClip(
                        model.gameObject, sheath, samples[index]);
                    var palm = rightHand.TransformPoint(PalmAnchorLocal(body, rightHand));
                    FrameSwordGripCamera(camera, palm, body.bounds.size.y * 0.28f);
                    camera.Render();
                    RenderTexture.active = targetTexture;
                    panel.ReadPixels(new Rect(0f, 0f, panelSize, panelSize), 0, 0);
                    panel.Apply();
                    strip.SetPixels32(
                        index * panelSize, 0, panelSize, panelSize, panel.GetPixels32());
                }
                strip.Apply();
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, strip.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(transforms);
                foreach (var item in layerStates)
                    item.Key.gameObject.layer = item.Value;
                foreach (var renderer in renderers) renderer.Restore();
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(strip);
                targetTexture.Release();
                UnityEngine.Object.DestroyImmediate(targetTexture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The slot-6 sword-grip review capture changed the scene dirty state.");
            Debug.Log(
                "Ispant06SwordGripReviewCaptured" +
                ", Image=" + SwordGripReviewPath + ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 06 Sheath Left Arm Static Pose Review")]
        public static void CaptureIspant06SheathLeftArmReview()
        {
            var scene = RequireScene(true);
            var wasDirty = scene.isDirty;
            var model = RequireCurrentModel(RequireSlot(scene)).transform;
            var staticModel = RequireStaticModel(scene);
            var body = RequireBody(model);
            var staticBody = RequireBody(staticModel);
            var sheath = RequireAsset<AnimationClip>(NewSheathPath);
            var destination = Absolute(SheathLeftArmReviewPath);
            const int panelWidth = 512;
            const int panelHeight = 640;
            const int panelCount = 4;
            const int captureLayer = 30;
            var targetTexture = new RenderTexture(
                panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelWidth, panelHeight, TextureFormat.RGB24, false);
            var comparison = new Texture2D(
                panelWidth * panelCount, panelHeight * 2, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("Ispant06SheathLeftArmReviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.085f, 0.1f, 1f);
            camera.fieldOfView = 28f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.targetTexture = targetTexture;
            camera.cullingMask = 1 << captureLayer;
            var oldActive = RenderTexture.active;
            var modelStates = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var layerStates = staticModel.GetComponentsInChildren<Transform>(true)
                .Concat(model.GetComponentsInChildren<Transform>(true))
                .Distinct()
                .ToDictionary(item => item, item => item.gameObject.layer);
            foreach (var item in layerStates.Keys)
                item.gameObject.layer = 29;
            try
            {
                AnimationMode.StartAnimationMode();
                foreach (var item in staticModel.GetComponentsInChildren<Transform>(true))
                    item.gameObject.layer = captureLayer;
                FrameLeftArmCamera(camera, staticBody.bounds);
                camera.Render();
                RenderTexture.active = targetTexture;
                panel.ReadPixels(new Rect(0f, 0f, panelWidth, panelHeight), 0, 0);
                panel.Apply();
                var staticPixels = panel.GetPixels32();
                for (var index = 0; index < panelCount; index++)
                    comparison.SetPixels32(
                        index * panelWidth, panelHeight,
                        panelWidth, panelHeight, staticPixels);

                foreach (var item in staticModel.GetComponentsInChildren<Transform>(true))
                    item.gameObject.layer = 29;
                foreach (var item in model.GetComponentsInChildren<Transform>(true))
                    item.gameObject.layer = captureLayer;
                var samples = new[]
                {
                    sheath.length * 0.15f,
                    sheath.length * 0.4f,
                    sheath.length * 0.65f,
                    sheath.length * 0.9f
                };
                for (var index = 0; index < samples.Length; index++)
                {
                    Restore(modelStates);
                    AnimationMode.SampleAnimationClip(model.gameObject, sheath, samples[index]);
                    FrameLeftArmCamera(camera, body.bounds);
                    camera.Render();
                    RenderTexture.active = targetTexture;
                    panel.ReadPixels(new Rect(0f, 0f, panelWidth, panelHeight), 0, 0);
                    panel.Apply();
                    comparison.SetPixels32(
                        index * panelWidth, 0,
                        panelWidth, panelHeight, panel.GetPixels32());
                }
                comparison.Apply();
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, comparison.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(modelStates);
                foreach (var item in layerStates)
                    item.Key.gameObject.layer = item.Value;
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(comparison);
                targetTexture.Release();
                UnityEngine.Object.DestroyImmediate(targetTexture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The slot-6 sheath left-arm review capture changed the scene dirty state.");
            Debug.Log(
                "Ispant06SheathLeftArmReviewCaptured" +
                ", Top=Ispant_01_StaticLeftArm" +
                ", Bottom=Slot06SheathFourPhases" +
                ", Image=" + SheathLeftArmReviewPath +
                ", SceneChanged=False, VisualVerdict=PendingUserReview.");
        }

        private static void FrameSwordGripCamera(Camera camera, Vector3 center, float height)
        {
            camera.aspect = 1f;
            var vertical = (height * 0.5f) /
                           Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            camera.transform.position = center + Vector3.back * vertical * 1.1f;
            camera.transform.rotation = Quaternion.LookRotation(
                center - camera.transform.position, Vector3.up);
        }

        private static void FrameLeftArmCamera(Camera camera, Bounds bounds)
        {
            camera.aspect = 4f / 5f;
            var height = bounds.size.y * 0.82f;
            var center = bounds.center + Vector3.up * bounds.size.y * 0.08f;
            var vertical = (height * 0.5f) /
                           Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            camera.transform.position = center + Vector3.back * vertical * 1.12f;
            camera.transform.rotation = Quaternion.LookRotation(
                center - camera.transform.position, Vector3.up);
        }

        private static MeshPartition CreateOrUpdateMusketMeshPartition(
            Mesh original,
            SkinnedMeshRenderer directBody)
        {
            var selectedVertices = SelectMusketVertices(original, directBody);
            var bodyTriangles = new int[original.subMeshCount][];
            var musketTriangles = new int[original.subMeshCount][];
            var usedMusketVertices = new SortedSet<int>();
            var originalIndexCount = 0;
            var bodyIndexCount = 0;
            var musketIndexCount = 0;
            for (var subMesh = 0; subMesh < original.subMeshCount; subMesh++)
            {
                var source = original.GetTriangles(subMesh);
                originalIndexCount += source.Length;
                var bodyValues = new List<int>();
                var musketValues = new List<int>();
                for (var index = 0; index < source.Length; index += 3)
                {
                    var selectedCount = 0;
                    if (selectedVertices.Contains(source[index])) selectedCount++;
                    if (selectedVertices.Contains(source[index + 1])) selectedCount++;
                    if (selectedVertices.Contains(source[index + 2])) selectedCount++;
                    if (selectedCount != 0 && selectedCount != 3)
                        throw new InvalidOperationException(
                            "A slot-6 triangle crosses the confirmed musket-component boundary.");
                    var destination = selectedCount == 3 ? musketValues : bodyValues;
                    destination.Add(source[index]);
                    destination.Add(source[index + 1]);
                    destination.Add(source[index + 2]);
                    if (selectedCount == 3)
                    {
                        usedMusketVertices.Add(source[index]);
                        usedMusketVertices.Add(source[index + 1]);
                        usedMusketVertices.Add(source[index + 2]);
                    }
                }
                bodyTriangles[subMesh] = bodyValues.ToArray();
                musketTriangles[subMesh] = musketValues.ToArray();
                bodyIndexCount += bodyValues.Count;
                musketIndexCount += musketValues.Count;
            }
            if (bodyIndexCount + musketIndexCount != originalIndexCount ||
                musketIndexCount == 0 || usedMusketVertices.Count != selectedVertices.Count)
                throw new InvalidOperationException(
                    "The confirmed slot-6 musket partition is incomplete or overlaps the body.");

            var bodyMesh = UnityEngine.Object.Instantiate(original);
            bodyMesh.name = "Ispant_06_BodyWithoutBackMusket";
            for (var subMesh = 0; subMesh < bodyMesh.subMeshCount; subMesh++)
                bodyMesh.SetTriangles(bodyTriangles[subMesh], subMesh, false);
            bodyMesh.RecalculateBounds();
            bodyMesh = CreateOrUpdateMeshAsset(bodyMesh, BodyWithoutMusketPath);

            var oldIndices = usedMusketVertices.ToArray();
            var remap = oldIndices.Select((value, index) => new { value, index })
                .ToDictionary(item => item.value, item => item.index);
            var musketMesh = new Mesh
            {
                name = "Ispant_06_RigidMusket",
                indexFormat = original.indexFormat,
                subMeshCount = original.subMeshCount,
                vertices = oldIndices.Select(index => original.vertices[index]).ToArray()
            };
            if (original.colors32.Length == original.vertexCount)
                musketMesh.colors32 = oldIndices.Select(index => original.colors32[index]).ToArray();
            for (var channel = 0; channel < 8; channel++)
            {
                var values = new List<Vector4>();
                original.GetUVs(channel, values);
                if (values.Count == original.vertexCount)
                    musketMesh.SetUVs(channel, oldIndices.Select(index => values[index]).ToList());
            }
            for (var subMesh = 0; subMesh < original.subMeshCount; subMesh++)
                musketMesh.SetTriangles(
                    musketTriangles[subMesh].Select(index => remap[index]).ToArray(),
                    subMesh,
                    false);
            musketMesh.RecalculateBounds();
            musketMesh.RecalculateNormals();
            musketMesh.RecalculateTangents();
            musketMesh = CreateOrUpdateMeshAsset(musketMesh, RigidMusketPath);

            return new MeshPartition(
                bodyMesh,
                musketMesh,
                original.vertexCount,
                originalIndexCount / 3,
                bodyIndexCount / 3,
                musketIndexCount / 3,
                selectedVertices.Count);
        }

        private static Vector3 SkinVertexToRendererLocal(
            Vector3 vertex,
            BoneWeight weight,
            SkinnedMeshRenderer renderer,
            Mesh mesh)
        {
            var value = Vector3.zero;
            AddSkinAtPose(ref value, vertex, weight.boneIndex0, weight.weight0, renderer, mesh);
            AddSkinAtPose(ref value, vertex, weight.boneIndex1, weight.weight1, renderer, mesh);
            AddSkinAtPose(ref value, vertex, weight.boneIndex2, weight.weight2, renderer, mesh);
            AddSkinAtPose(ref value, vertex, weight.boneIndex3, weight.weight3, renderer, mesh);
            return value;
        }

        private static void AddSkinAtPose(
            ref Vector3 value,
            Vector3 vertex,
            int boneIndex,
            float weight,
            SkinnedMeshRenderer renderer,
            Mesh mesh)
        {
            if (weight <= 0f) return;
            if (boneIndex < 0 || boneIndex >= renderer.bones.Length ||
                boneIndex >= mesh.bindposes.Length || renderer.bones[boneIndex] == null)
                throw new InvalidOperationException("A confirmed musket vertex has invalid skinning data.");
            value += renderer.transform.worldToLocalMatrix.MultiplyPoint3x4(
                         renderer.bones[boneIndex].localToWorldMatrix.MultiplyPoint3x4(
                             mesh.bindposes[boneIndex].MultiplyPoint3x4(vertex))) * weight;
        }

        private static HashSet<int> SelectMusketVertices(
            Mesh mesh,
            SkinnedMeshRenderer renderer)
        {
            if (mesh.vertexCount != 8755 || mesh.subMeshCount != 1)
                throw new InvalidOperationException(
                    "The direct slot-6 source mesh topology differs from the diagnosed 8755-vertex source.");
            var leftShoulderIndex = Array.FindIndex(
                renderer.bones, item => item != null && item.name == "LeftShoulder");
            if (leftShoulderIndex < 0)
                throw new InvalidOperationException("The direct source LeftShoulder bone is missing.");
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var adjacency = Enumerable.Range(0, vertices.Length)
                .Select(_ => new List<int>()).ToArray();
            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                var triangles = mesh.GetTriangles(subMesh);
                for (var index = 0; index < triangles.Length; index += 3)
                {
                    Connect(adjacency, triangles[index], triangles[index + 1]);
                    Connect(adjacency, triangles[index + 1], triangles[index + 2]);
                    Connect(adjacency, triangles[index + 2], triangles[index]);
                }
            }

            var expectedSeeds = new HashSet<int>(ExpectedMusketComponentSeeds);
            var actualSeeds = new HashSet<int>();
            var selected = new HashSet<int>();
            var seen = new bool[vertices.Length];
            for (var seed = 0; seed < vertices.Length; seed++)
            {
                if (seen[seed]) continue;
                var stack = new Stack<int>();
                var members = new List<int>();
                stack.Push(seed);
                seen[seed] = true;
                while (stack.Count > 0)
                {
                    var vertex = stack.Pop();
                    members.Add(vertex);
                    foreach (var neighbor in adjacency[vertex])
                    {
                        if (seen[neighbor]) continue;
                        seen[neighbor] = true;
                        stack.Push(neighbor);
                    }
                }
                if (!expectedSeeds.Contains(seed)) continue;
                var bounds = new Bounds(vertices[members[0]], Vector3.zero);
                var leftWeight = 0f;
                foreach (var vertex in members)
                {
                    bounds.Encapsulate(vertices[vertex]);
                    leftWeight += WeightForBone(weights[vertex], leftShoulderIndex);
                }
                var ratio = leftWeight / members.Count;
                var lineDistance = DistanceToSegment(
                    bounds.center,
                    new Vector3(-0.69f, 0.56f, -0.156f),
                    new Vector3(-0.04f, 1.43f, -0.156f));
                if (ratio < 0.45f || bounds.center.z < -0.19f || bounds.center.z > -0.12f ||
                    lineDistance > 0.1f)
                    throw new InvalidOperationException(
                        "A confirmed slot-6 musket component no longer matches its rigid back-plane diagnosis: " +
                        seed + ".");
                actualSeeds.Add(seed);
                foreach (var vertex in members) selected.Add(vertex);
            }
            if (!actualSeeds.SetEquals(expectedSeeds))
                throw new InvalidOperationException(
                    "The confirmed slot-6 musket component seed set differs from diagnosis.");
            return selected;
        }

        private static Mesh CreateOrUpdateMeshAsset(Mesh generated, string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (asset == null)
            {
                AssetDatabase.CreateAsset(generated, path);
                asset = generated;
            }
            else
            {
                EditorUtility.CopySerialized(generated, asset);
                asset.name = generated.name;
                UnityEngine.Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(asset);
            }
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static MeshRenderer CreateOrUpdateBackMusketRenderer(
            Transform model,
            SkinnedMeshRenderer body,
            Mesh musketMesh,
            Material[] materials)
        {
            foreach (var existing in model.GetComponentsInChildren<Transform>(true)
                         .Where(item => item.name == BackMusketName ||
                                        item.name == HandMusketRootName)
                         .OrderByDescending(item => Path(model, item).Length)
                         .ToArray())
                if (existing != model) UnityEngine.Object.DestroyImmediate(existing.gameObject);

            var leftShoulder = RequireBone(model, "LeftShoulder");
            var root = new GameObject(BackMusketName);
            root.transform.SetParent(leftShoulder, false);
            SetLocalMatrix(root.transform,
                leftShoulder.worldToLocalMatrix * body.transform.localToWorldMatrix);
            var filter = root.AddComponent<MeshFilter>();
            var renderer = root.AddComponent<MeshRenderer>();
            filter.sharedMesh = musketMesh;
            renderer.sharedMaterials = materials;
            renderer.enabled = true;
            CopyRendererSettings(body, renderer);
            EditorUtility.SetDirty(root);
            return renderer;
        }

        private static void ConfigureMusketAnimation(
            Transform model,
            SkinnedMeshRenderer body,
            MeshRenderer backMusket,
            AnimationClip sheath,
            AnimationClip hold,
            AnimationClip bridge,
            AnimationClip rifle,
            AnimationClip legacyRifle)
        {
            var currentScene = model.gameObject.scene;
            var recovery = EditorSceneManager.OpenScene(
                LegacyRecoveryScenePath, OpenSceneMode.Additive);
            try
            {
                var legacySlot = recovery.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Single(item => item.name == SlotName);
                if (legacySlot.childCount != 1)
                    throw new InvalidOperationException("The finalized legacy recovery slot-6 model differs.");
                var legacyModel = legacySlot.GetChild(0);
                var legacyHandMusket = legacyModel.GetComponentsInChildren<MeshRenderer>(true)
                    .Single(item => item.name == "Ispant_ChangeToRifle_HandMusket_Renderer");
                var legacyMesh = legacyHandMusket.GetComponent<MeshFilter>().sharedMesh;
                var legacyMuzzleLocal = DetermineMusketLocalMuzzleAxis(legacyMesh);
                var legacyDownLocal = DetermineMusketLocalDownAxis(legacyMuzzleLocal);
                var currentMesh = backMusket.GetComponent<MeshFilter>().sharedMesh;
                var currentMuzzleLocal = DetermineMusketLocalMuzzleAxis(currentMesh);
                var currentDownLocal = DetermineMusketLocalDownAxis(currentMuzzleLocal);
                var grabTime = FindLegacyGrabTime(legacyRifle);
                var rightHand = RequireBone(model, "RightHand");
                var currentStates = model.GetComponentsInChildren<Transform>(true)
                    .Select(item => new TransformState(item)).ToArray();
                var legacyStates = legacyModel.GetComponentsInChildren<Transform>(true)
                    .Select(item => new TransformState(item)).ToArray();
                Vector3 rootLocalPosition;
                Matrix4x4 rendererLocal;
                Quaternion currentGrabFrame;
                Quaternion legacyGrabFrame;
                Quaternion orientationCorrection;
                Vector3 alignedBackLocalPosition;
                Quaternion alignedBackLocalRotation;
                Vector3 alignedBackLocalScale;
                int gripIndex;
                try
                {
                    AnimationMode.StartAnimationMode();
                    AnimationMode.SampleAnimationClip(model.gameObject, rifle, grabTime);
                    AnimationMode.SampleAnimationClip(legacyModel.gameObject, legacyRifle, grabTime);
                    var palm = rightHand.position;
                    var vertices = currentMesh.vertices;
                    var projections = vertices.Select(vertex =>
                        Vector3.Dot(vertex, currentMuzzleLocal)).ToArray();
                    var stockLimit = Mathf.Lerp(projections.Min(), projections.Max(), 0.35f);
                    gripIndex = Enumerable.Range(0, vertices.Length)
                        .Where(index => projections[index] <= stockLimit)
                        .OrderBy(index => Vector3.Distance(
                            palm, backMusket.transform.TransformPoint(vertices[index])))
                        .First();
                    var gripWorld = backMusket.transform.TransformPoint(vertices[gripIndex]);
                    backMusket.transform.position += palm - gripWorld;
                    gripWorld = backMusket.transform.TransformPoint(vertices[gripIndex]);
                    alignedBackLocalPosition = backMusket.transform.localPosition;
                    alignedBackLocalRotation = backMusket.transform.localRotation;
                    alignedBackLocalScale = backMusket.transform.localScale;
                    rootLocalPosition = rightHand.InverseTransformPoint(gripWorld);
                    var rootWorld = rightHand.localToWorldMatrix *
                                    Matrix4x4.TRS(rootLocalPosition, Quaternion.identity, Vector3.one);
                    rendererLocal = rootWorld.inverse * backMusket.transform.localToWorldMatrix;
                    currentGrabFrame = WeaponFrameInModel(
                        model, backMusket.transform, currentMuzzleLocal, currentDownLocal);
                    legacyGrabFrame = WeaponFrameInModel(
                        legacyModel, legacyHandMusket.transform, legacyMuzzleLocal, legacyDownLocal);
                    orientationCorrection = Quaternion.Inverse(legacyGrabFrame) *
                                            currentGrabFrame;
                }
                finally
                {
                    if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                    Restore(currentStates);
                    Restore(legacyStates);
                }
                backMusket.transform.localPosition = alignedBackLocalPosition;
                backMusket.transform.localRotation = alignedBackLocalRotation;
                backMusket.transform.localScale = alignedBackLocalScale;

                var handRoot = new GameObject(HandMusketRootName);
                handRoot.transform.SetParent(rightHand, false);
                handRoot.transform.localPosition = rootLocalPosition;
                handRoot.transform.localRotation = Quaternion.identity;
                handRoot.transform.localScale = Vector3.one;
                var handObject = new GameObject(HandMusketRendererName);
                handObject.transform.SetParent(handRoot.transform, false);
                SetLocalMatrix(handObject.transform, rendererLocal);
                var handFilter = handObject.AddComponent<MeshFilter>();
                var handRenderer = handObject.AddComponent<MeshRenderer>();
                handFilter.sharedMesh = currentMesh;
                handRenderer.sharedMaterials = backMusket.sharedMaterials;
                handRenderer.enabled = false;
                CopyRendererSettings(body, handRenderer);

                var rootRotation = new QuaternionCurves();
                var rootPosition = new VectorCurves();
                var leftArmRotation = new QuaternionCurves();
                var leftForeArmRotation = new QuaternionCurves();
                var currentLocalWeaponFrame = Quaternion.LookRotation(
                    currentMuzzleLocal, -currentDownLocal);
                var leftArm = RequireBone(model, "LeftArm");
                var leftForeArm = RequireBone(model, "LeftForeArm");
                var leftHand = RequireBone(model, "LeftHand");
                var leftPalmLocal = Vector3.zero;
                var supportProjections = currentMesh.vertices.Select(vertex =>
                    Vector3.Dot(vertex, currentMuzzleLocal)).ToArray();
                var supportLength = supportProjections.Max() - supportProjections.Min();
                var gripProjection = supportProjections[gripIndex];
                var supportIndices = Enumerable.Range(0, currentMesh.vertexCount)
                    .Where(index => supportProjections[index] >=
                                    gripProjection + supportLength * 0.2f &&
                                    supportProjections[index] <= supportProjections.Max() -
                                    supportLength * 0.05f)
                    .ToArray();
                if (supportIndices.Length == 0)
                    throw new InvalidOperationException(
                        "The current rigid musket has no forward support surface for left-arm correction.");
                var sampleCount = Mathf.Max(1, Mathf.RoundToInt(rifle.length * FrameRate));
                currentStates = model.GetComponentsInChildren<Transform>(true)
                    .Select(item => new TransformState(item)).ToArray();
                legacyStates = legacyModel.GetComponentsInChildren<Transform>(true)
                    .Select(item => new TransformState(item)).ToArray();
                try
                {
                    AnimationMode.StartAnimationMode();
                    for (var frame = 0; frame <= sampleCount; frame++)
                    {
                        Restore(currentStates);
                        Restore(legacyStates);
                        var time = rifle.length * frame / sampleCount;
                        AnimationMode.SampleAnimationClip(model.gameObject, rifle, time);
                        AnimationMode.SampleAnimationClip(legacyModel.gameObject, legacyRifle, time);
                        var desiredLocalRotation = Quaternion.identity;
                        if (time >= grabTime - 0.5f / FrameRate)
                        {
                            var legacyFrame = WeaponFrameInModel(
                                legacyModel, legacyHandMusket.transform,
                                legacyMuzzleLocal, legacyDownLocal);
                            var progress = Mathf.InverseLerp(grabTime, rifle.length, time);
                            var desiredWeaponFrame = legacyFrame * Quaternion.Slerp(
                                orientationCorrection, Quaternion.identity, progress);
                            var desiredRendererModelRotation = desiredWeaponFrame *
                                                               Quaternion.Inverse(currentLocalWeaponFrame);
                            var desiredRendererWorldRotation = model.rotation *
                                                               desiredRendererModelRotation;
                            var desiredRootWorldRotation = desiredRendererWorldRotation *
                                                           Quaternion.Inverse(
                                                               handRenderer.transform.localRotation);
                            desiredLocalRotation = Quaternion.Inverse(rightHand.rotation) *
                                                   desiredRootWorldRotation;
                        }
                        handRoot.transform.localRotation = desiredLocalRotation;
                        if (time >= grabTime)
                        {
                            var leftPalm = leftHand.TransformPoint(leftPalmLocal);
                            var targetSurface = supportIndices
                                .Select(index => handRenderer.transform.TransformPoint(
                                    currentMesh.vertices[index]))
                                .OrderBy(point => Vector3.Distance(leftPalm, point))
                                .First();
                            var weight = Mathf.SmoothStep(
                                0f,
                                1f,
                                Mathf.InverseLerp(grabTime, rifle.length * 0.8f, time));
                            SolveLeftForegrip(
                                leftArm, leftForeArm, leftHand, leftPalmLocal,
                                targetSurface, weight);
                        }
                        rootRotation.Add(time, desiredLocalRotation);
                        rootPosition.Add(time, rootLocalPosition);
                        leftArmRotation.Add(time, leftArm.localRotation);
                        leftForeArmRotation.Add(time, leftForeArm.localRotation);
                    }
                }
                finally
                {
                    if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                    Restore(currentStates);
                    Restore(legacyStates);
                }

                var rootPath = Path(model, handRoot.transform);
                rootRotation.Write(rifle, rootPath);
                rootPosition.Write(rifle, rootPath);
                leftArmRotation.Write(rifle, Path(model, leftArm));
                leftForeArmRotation.Write(rifle, Path(model, leftForeArm));
                var backPath = Path(model, backMusket.transform);
                var handPath = Path(model, handRenderer.transform);
                foreach (var clip in new[] { sheath, hold, bridge })
                {
                    SetConstantRendererEnabledCurve(clip, backPath, true, clip.length);
                    SetConstantRendererEnabledCurve(clip, handPath, false, clip.length);
                }
                SetStepRendererEnabledCurve(rifle, backPath, grabTime, true, false);
                SetStepRendererEnabledCurve(rifle, handPath, grabTime, false, true);
                foreach (var clip in new[] { sheath, hold, bridge, rifle })
                    EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();
                EditorUtility.SetDirty(handRoot);
                EditorUtility.SetDirty(handObject);
            }
            finally
            {
                EditorSceneManager.CloseScene(recovery, true);
                SceneManager.SetActiveScene(currentScene);
            }
        }

        private static float FindLegacyGrabTime(AnimationClip legacyRifle)
        {
            var binding = AnimationUtility.GetCurveBindings(legacyRifle)
                .Single(item => item.type == typeof(MeshRenderer) &&
                                item.propertyName == "m_Enabled" &&
                                item.path.EndsWith("Ispant_Sheath_RigidMusket",
                                    StringComparison.Ordinal));
            var curve = AnimationUtility.GetEditorCurve(legacyRifle, binding) ??
                        throw new InvalidOperationException("The legacy back-musket visibility curve is missing.");
            var key = curve.keys.FirstOrDefault(item => item.time > 0f && item.value < 0.5f);
            if (key.time <= 0f || key.time >= legacyRifle.length)
                throw new InvalidOperationException("The legacy back-to-hand musket switch time differs.");
            return key.time;
        }

        private static MusketInspectionMetrics InspectMusketAnimation(
            Transform model,
            SkinnedMeshRenderer body,
            MeshRenderer backMusket,
            MeshRenderer handMusket,
            IReadOnlyList<AnimationClip> clips,
            AnimationClip legacyRifle)
        {
            var transforms = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var rendererStates = model.GetComponentsInChildren<Renderer>(true)
                .Select(item => new RendererState(item)).ToArray();
            try
            {
                AnimationMode.StartAnimationMode();
                for (var clipIndex = 0; clipIndex < 3; clipIndex++)
                foreach (var time in new[] { 0f, clips[clipIndex].length * 0.5f, clips[clipIndex].length })
                {
                    Restore(transforms);
                    AnimationMode.SampleAnimationClip(model.gameObject, clips[clipIndex], time);
                    if (!backMusket.enabled || handMusket.enabled)
                        throw new InvalidOperationException(
                            "The slot-6 sheath/hold/bridge musket visibility differs.");
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(transforms);
                foreach (var state in rendererStates) state.Restore();
            }

            var currentScene = model.gameObject.scene;
            var recovery = EditorSceneManager.OpenScene(
                LegacyRecoveryScenePath, OpenSceneMode.Additive);
            try
            {
                var legacySlot = recovery.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Single(item => item.name == SlotName);
                var legacyModel = legacySlot.GetChild(0);
                var legacyHandMusket = legacyModel.GetComponentsInChildren<MeshRenderer>(true)
                    .Single(item => item.name == "Ispant_ChangeToRifle_HandMusket_Renderer");
                var legacyMesh = legacyHandMusket.GetComponent<MeshFilter>().sharedMesh;
                var legacyMuzzleLocal = DetermineMusketLocalMuzzleAxis(legacyMesh);
                var legacyDownLocal = DetermineMusketLocalDownAxis(legacyMuzzleLocal);
                var currentMesh = handMusket.GetComponent<MeshFilter>().sharedMesh;
                var currentMuzzleLocal = DetermineMusketLocalMuzzleAxis(currentMesh);
                var currentDownLocal = DetermineMusketLocalDownAxis(currentMuzzleLocal);
                var rifle = clips[3];
                var grabTime = FindLegacyGrabTime(legacyRifle);
                var rightHand = RequireBone(model, "RightHand");
                var leftHand = RequireBone(model, "LeftHand");
                transforms = model.GetComponentsInChildren<Transform>(true)
                    .Select(item => new TransformState(item)).ToArray();
                rendererStates = model.GetComponentsInChildren<Renderer>(true)
                    .Select(item => new RendererState(item)).ToArray();
                var legacyTransforms = legacyModel.GetComponentsInChildren<Transform>(true)
                    .Select(item => new TransformState(item)).ToArray();
                Quaternion currentGrabFrame;
                Quaternion legacyGrabFrame;
                Quaternion orientationCorrection;
                Vector3 rightPalmLocal;
                Vector3 leftPalmLocal;
                var continuityError = 0f;
                var grabRightGripDistance = 0f;
                try
                {
                    AnimationMode.StartAnimationMode();
                    AnimationMode.SampleAnimationClip(model.gameObject, rifle, grabTime);
                    AnimationMode.SampleAnimationClip(legacyModel.gameObject, legacyRifle, grabTime);
                    currentGrabFrame = WeaponFrameInModel(
                        model, backMusket.transform, currentMuzzleLocal, currentDownLocal);
                    legacyGrabFrame = WeaponFrameInModel(
                        legacyModel, legacyHandMusket.transform, legacyMuzzleLocal, legacyDownLocal);
                    orientationCorrection = Quaternion.Inverse(legacyGrabFrame) *
                                            currentGrabFrame;
                    rightPalmLocal = Vector3.zero;
                    leftPalmLocal = Vector3.zero;
                    var grabPalm = rightHand.TransformPoint(rightPalmLocal);
                    grabRightGripDistance = currentMesh.vertices.Min(vertex => Vector3.Distance(
                        grabPalm, handMusket.transform.TransformPoint(vertex)));
                    foreach (var vertex in currentMesh.vertices)
                        continuityError = Mathf.Max(
                            continuityError,
                            Vector3.Distance(
                                backMusket.transform.TransformPoint(vertex),
                                handMusket.transform.TransformPoint(vertex)));
                }
                finally
                {
                    if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                    Restore(transforms);
                    Restore(legacyTransforms);
                    foreach (var state in rendererStates) state.Restore();
                }

                var maximumRightGripDistance = 0f;
                var maximumRightGripTime = 0f;
                var maximumTrajectoryAngleError = 0f;
                var finalLeftForegripDistance = float.PositiveInfinity;
                var finalMuzzleForwardAngle = 180f;
                var vertices = currentMesh.vertices;
                var projections = vertices.Select(vertex => Vector3.Dot(vertex, currentMuzzleLocal)).ToArray();
                var projectionLength = projections.Max() - projections.Min();
                var sampleCount = Mathf.Max(1, Mathf.RoundToInt(rifle.length * FrameRate));
                try
                {
                    AnimationMode.StartAnimationMode();
                    for (var frame = 0; frame <= sampleCount; frame++)
                    {
                        Restore(transforms);
                        Restore(legacyTransforms);
                        var time = rifle.length * frame / sampleCount;
                        AnimationMode.SampleAnimationClip(model.gameObject, rifle, time);
                        AnimationMode.SampleAnimationClip(legacyModel.gameObject, legacyRifle, time);
                        var afterGrab = time >= grabTime - 0.25f / FrameRate;
                        if (afterGrab ? (backMusket.enabled || !handMusket.enabled) :
                            (!backMusket.enabled || handMusket.enabled))
                            throw new InvalidOperationException(
                                "The slot-6 back/hand musket visibility is missing or duplicated at " +
                                Num(time) + " seconds.");
                        if (!afterGrab) continue;

                        var legacyFrame = WeaponFrameInModel(
                            legacyModel, legacyHandMusket.transform,
                            legacyMuzzleLocal, legacyDownLocal);
                        var progress = Mathf.InverseLerp(grabTime, rifle.length, time);
                        var expectedFrame = legacyFrame * Quaternion.Slerp(
                            orientationCorrection, Quaternion.identity, progress);
                        var currentFrame = WeaponFrameInModel(
                            model, handMusket.transform, currentMuzzleLocal, currentDownLocal);
                        maximumTrajectoryAngleError = Mathf.Max(
                            maximumTrajectoryAngleError,
                            Quaternion.Angle(expectedFrame, currentFrame));

                        var rightPalm = rightHand.TransformPoint(rightPalmLocal);
                        var distances = vertices.Select(vertex => Vector3.Distance(
                            rightPalm, handMusket.transform.TransformPoint(vertex))).ToArray();
                        var rightGripDistance = distances.Min();
                        if (rightGripDistance > maximumRightGripDistance)
                        {
                            maximumRightGripDistance = rightGripDistance;
                            maximumRightGripTime = time;
                        }
                        if (frame != sampleCount) continue;

                        var gripIndex = Enumerable.Range(0, vertices.Length)
                            .OrderBy(index => distances[index]).First();
                        var gripProjection = projections[gripIndex];
                        var supportIndices = Enumerable.Range(0, vertices.Length)
                            .Where(index => projections[index] >=
                                            gripProjection + projectionLength * 0.2f &&
                                            projections[index] <= projections.Max() -
                                            projectionLength * 0.05f)
                            .ToArray();
                        if (supportIndices.Length == 0)
                            throw new InvalidOperationException(
                                "The current rigid musket has no measurable forward support surface.");
                        var leftPalm = leftHand.TransformPoint(leftPalmLocal);
                        finalLeftForegripDistance = supportIndices.Min(index => Vector3.Distance(
                            leftPalm, handMusket.transform.TransformPoint(vertices[index])));
                        var currentMuzzleModel = currentFrame * Vector3.forward;
                        var characterForward = Vector3.forward;
                        finalMuzzleForwardAngle = Vector3.Angle(
                            currentMuzzleModel, characterForward);
                    }
                }
                finally
                {
                    if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                    Restore(transforms);
                    Restore(legacyTransforms);
                    foreach (var state in rendererStates) state.Restore();
                }

                if (continuityError > 0.0025f ||
                    maximumRightGripDistance > 0.06f ||
                    finalLeftForegripDistance > 0.18f ||
                    maximumTrajectoryAngleError > 0.25f ||
                    finalMuzzleForwardAngle > 5f)
                    throw new InvalidOperationException(
                        "The slot-6 musket continuity, grip, legacy trajectory, or forward aim inspection failed. " +
                        "Continuity=" + Num(continuityError) +
                        ", GrabRightGrip=" + Num(grabRightGripDistance) +
                        ", RightGrip=" + Num(maximumRightGripDistance) +
                        "@" + Num(maximumRightGripTime) +
                        ", LeftForegrip=" + Num(finalLeftForegripDistance) +
                        ", Trajectory=" + Num(maximumTrajectoryAngleError) +
                        ", MuzzleForward=" + Num(finalMuzzleForwardAngle) + ".");
                return new MusketInspectionMetrics(
                    continuityError,
                    maximumRightGripDistance,
                    finalLeftForegripDistance,
                    maximumTrajectoryAngleError,
                    finalMuzzleForwardAngle);
            }
            finally
            {
                EditorSceneManager.CloseScene(recovery, true);
                SceneManager.SetActiveScene(currentScene);
            }
        }

        private static int TriangleCount(Mesh mesh)
        {
            var indices = 0;
            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                indices += mesh.GetTriangles(subMesh).Length;
            if (indices % 3 != 0)
                throw new InvalidOperationException("A slot-6 mesh index count is not triangular.");
            return indices / 3;
        }

        private static void SetConstantRendererEnabledCurve(
            AnimationClip clip,
            string path,
            bool enabled,
            float duration)
        {
            var value = enabled ? 1f : 0f;
            var curve = new AnimationCurve(new Keyframe(0f, value), new Keyframe(duration, value));
            SetConstantTangents(curve);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(MeshRenderer), "m_Enabled"),
                curve);
        }

        private static void SetStepRendererEnabledCurve(
            AnimationClip clip,
            string path,
            float transitionTime,
            bool before,
            bool after)
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, before ? 1f : 0f),
                new Keyframe(transitionTime, after ? 1f : 0f),
                new Keyframe(clip.length, after ? 1f : 0f));
            SetConstantTangents(curve);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(MeshRenderer), "m_Enabled"),
                curve);
        }

        private static void SetConstantTangents(AnimationCurve curve)
        {
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve, index, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(
                    curve, index, AnimationUtility.TangentMode.Constant);
            }
        }

        private static Vector3 DetermineMusketLocalMuzzleAxis(Mesh mesh)
        {
            var vertices = mesh.vertices;
            if (vertices.Length < 4)
                throw new InvalidOperationException("The slot-6 musket has too few vertices.");
            var first = 0;
            var second = 1;
            var maximumSquaredDistance = 0f;
            for (var left = 0; left < vertices.Length; left++)
            for (var right = left + 1; right < vertices.Length; right++)
            {
                var squaredDistance = (vertices[right] - vertices[left]).sqrMagnitude;
                if (squaredDistance <= maximumSquaredDistance) continue;
                maximumSquaredDistance = squaredDistance;
                first = left;
                second = right;
            }
            var axis = (vertices[second] - vertices[first]).normalized;
            var length = Mathf.Sqrt(maximumSquaredDistance);
            var projections = vertices.Select(vertex =>
                Vector3.Dot(vertex - vertices[first], axis)).ToArray();
            var range = length * 0.2f;
            var firstSpread = vertices.Where((vertex, index) => projections[index] <= range)
                .Average(vertex => Vector3.Cross(vertex - vertices[first], axis).magnitude);
            var secondSpread = vertices.Where((vertex, index) => projections[index] >= length - range)
                .Average(vertex => Vector3.Cross(vertex - vertices[second], axis).magnitude);
            if (Mathf.Abs(firstSpread - secondSpread) < 0.00001f)
                throw new InvalidOperationException("The slot-6 musket stock and muzzle ends are ambiguous.");
            return secondSpread < firstSpread ? axis : -axis;
        }

        private static Vector3 DetermineMusketLocalDownAxis(Vector3 muzzleAxis)
        {
            var down = Vector3.ProjectOnPlane(Vector3.down, muzzleAxis);
            if (down.sqrMagnitude < 0.000001f)
                down = Vector3.ProjectOnPlane(Vector3.back, muzzleAxis);
            if (down.sqrMagnitude < 0.000001f)
                throw new InvalidOperationException("The slot-6 musket cannot establish a stable down axis.");
            return down.normalized;
        }

        private static Quaternion WeaponFrameInModel(
            Transform model,
            Transform weapon,
            Vector3 localMuzzle,
            Vector3 localDown)
        {
            var muzzle = model.worldToLocalMatrix.MultiplyVector(
                weapon.TransformDirection(localMuzzle)).normalized;
            var down = model.worldToLocalMatrix.MultiplyVector(
                weapon.TransformDirection(localDown)).normalized;
            return Quaternion.LookRotation(muzzle, -down);
        }

        private static Quaternion CharacterFrameFromTransforms(Transform model, bool legacy)
        {
            var prefix = legacy ? "mixamorig:" : string.Empty;
            var hips = ModelPosition(model, RequireBone(model, prefix + "Hips"));
            var head = ModelPosition(model, RequireBone(model, prefix + "Head"));
            var left = ModelPosition(model, RequireBone(model, prefix + "LeftShoulder"));
            var right = ModelPosition(model, RequireBone(model, prefix + "RightShoulder"));
            var up = (head - hips).normalized;
            var lateral = (right - left).normalized;
            var forward = Vector3.Cross(lateral, up).normalized;
            return Quaternion.LookRotation(forward, up);
        }

        private static void CopyRendererSettings(Renderer source, Renderer destination)
        {
            destination.shadowCastingMode = source.shadowCastingMode;
            destination.receiveShadows = source.receiveShadows;
            destination.lightProbeUsage = source.lightProbeUsage;
            destination.reflectionProbeUsage = source.reflectionProbeUsage;
            destination.renderingLayerMask = source.renderingLayerMask;
        }

        private static void SetLocalMatrix(Transform target, Matrix4x4 matrix)
        {
            var position = matrix.GetColumn(3);
            var x = matrix.GetColumn(0);
            var y = matrix.GetColumn(1);
            var z = matrix.GetColumn(2);
            var scale = new Vector3(x.magnitude, y.magnitude, z.magnitude);
            if (Vector3.Dot(Vector3.Cross(x, y), z) < 0f) scale.x = -scale.x;
            var rotation = Quaternion.LookRotation(z / scale.z, y / scale.y);
            target.localPosition = position;
            target.localRotation = rotation;
            target.localScale = scale;
        }

        private static void SolveLeftForegrip(
            Transform upperArm,
            Transform foreArm,
            Transform hand,
            Vector3 palmLocal,
            Vector3 targetSurface,
            float weight)
        {
            if (weight <= 0f) return;
            var originalUpper = upperArm.localRotation;
            var originalFore = foreArm.localRotation;
            for (var iteration = 0; iteration < 3; iteration++)
            {
                var palm = hand.TransformPoint(palmLocal);
                var desiredHandPosition = hand.position + targetSurface - palm;
                SolveTwoBonePosition(upperArm, foreArm, hand, desiredHandPosition);
            }
            var solvedUpper = upperArm.localRotation;
            var solvedFore = foreArm.localRotation;
            upperArm.localRotation = Quaternion.Slerp(originalUpper, solvedUpper, weight);
            foreArm.localRotation = Quaternion.Slerp(originalFore, solvedFore, weight);
        }

        private static void SolveTwoBonePosition(
            Transform upperArm,
            Transform foreArm,
            Transform hand,
            Vector3 target)
        {
            var shoulder = upperArm.position;
            var elbow = foreArm.position;
            var wrist = hand.position;
            var upperLength = Vector3.Distance(shoulder, elbow);
            var foreLength = Vector3.Distance(elbow, wrist);
            var toTarget = target - shoulder;
            if (upperLength <= 0.0001f || foreLength <= 0.0001f ||
                toTarget.sqrMagnitude <= 0.0000001f)
                throw new InvalidOperationException("The slot-6 left-arm chain cannot solve the foregrip.");
            var direction = toTarget.normalized;
            var distance = Mathf.Clamp(
                toTarget.magnitude,
                Mathf.Abs(upperLength - foreLength) + 0.0001f,
                upperLength + foreLength - 0.0001f);
            var currentBend = Vector3.ProjectOnPlane(elbow - shoulder, direction);
            if (currentBend.sqrMagnitude <= 0.0000001f)
            {
                var normal = Vector3.Cross(elbow - shoulder, wrist - elbow);
                currentBend = Vector3.Cross(normal, direction);
            }
            if (currentBend.sqrMagnitude <= 0.0000001f)
                currentBend = Vector3.Cross(direction, Vector3.up);
            if (currentBend.sqrMagnitude <= 0.0000001f)
                currentBend = Vector3.Cross(direction, Vector3.right);
            currentBend.Normalize();
            var along = (upperLength * upperLength + distance * distance -
                         foreLength * foreLength) / (2f * distance);
            var perpendicular = Mathf.Sqrt(Mathf.Max(
                0f, upperLength * upperLength - along * along));
            var desiredElbow = shoulder + direction * along + currentBend * perpendicular;
            upperArm.rotation = Quaternion.FromToRotation(
                                    foreArm.position - upperArm.position,
                                    desiredElbow - upperArm.position) *
                                upperArm.rotation;
            foreArm.rotation = Quaternion.FromToRotation(
                                   hand.position - foreArm.position,
                                   target - foreArm.position) *
                               foreArm.rotation;
        }

        private static void FrameCamera(Camera camera, Vector3 center, float height)
        {
            camera.aspect = 2f / 3f;
            var vertical = (height * 0.5f) /
                           Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            camera.transform.position = center + Vector3.back * vertical * 1.25f +
                                        Vector3.up * height * 0.01f;
            camera.transform.rotation = Quaternion.LookRotation(
                center - camera.transform.position, Vector3.up);
        }

        private static void RenderPanel(
            Camera camera,
            Texture2D panel,
            Texture2D strip,
            RenderTexture target,
            int panelIndex,
            int row,
            int width,
            int height)
        {
            camera.Render();
            RenderTexture.active = target;
            panel.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            panel.Apply();
            var pixels = panel.GetPixels32();
            if (pixels.Any(pixel => pixel.r >= 240 && pixel.b >= 240 && pixel.g <= 24))
                throw new InvalidOperationException("The slot-6 comparison contains magenta fallback.");
            strip.SetPixels32(panelIndex * width, row * height, width, height, pixels);
        }

        private static AnimationClip RetargetClip(
            Transform source,
            Transform target,
            AnimationClip legacy,
            string outputPath,
            string outputName,
            WaistSwordPose? fixedWaistPose,
            out WaistSwordPose endWaistPose)
        {
            var sourceBones = BonePairs.ToDictionary(
                item => item.Target,
                item => RequireBone(source, item.Source), StringComparer.Ordinal);
            var targetBones = BonePairs.ToDictionary(
                item => item.Target,
                item => RequireBone(target, item.Target), StringComparer.Ordinal);
            var sourceStates = source.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var targetStates = target.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var sourceBody = source.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "Ispant_Armed_Body");
            var targetBody = RequireBody(target);
            var sourceBind = sourceBones.ToDictionary(
                item => item.Key,
                item => BindModelMatrix(source, sourceBody, item.Value), StringComparer.Ordinal);
            var targetBind = targetBones.ToDictionary(
                item => item.Key,
                item => BindModelMatrix(target, targetBody, item.Value), StringComparer.Ordinal);
            var sourceRestRotations = sourceBind.ToDictionary(
                item => item.Key, item => item.Value.rotation, StringComparer.Ordinal);
            var targetRestRotations = targetBind.ToDictionary(
                item => item.Key, item => item.Value.rotation, StringComparer.Ordinal);
            var translatedBoneNames = new[] { "LeftShoulder", "RightShoulder" };
            var sourceRestLocalPositions = translatedBoneNames.ToDictionary(
                name => name, name => sourceBones[name].localPosition, StringComparer.Ordinal);
            var targetRestLocalPositions = translatedBoneNames.ToDictionary(
                name => name, name => targetBones[name].localPosition, StringComparer.Ordinal);
            var sourceHipsRestPosition = sourceBind["Hips"].GetPosition();
            var targetHipsRestPosition = targetBind["Hips"].GetPosition();
            var sourceHeight = Vector3.Distance(
                sourceHipsRestPosition, sourceBind["Head"].GetPosition());
            var targetHeight = Vector3.Distance(
                targetHipsRestPosition, targetBind["Head"].GetPosition());
            var scale = targetHeight / sourceHeight;
            var frameMap = CharacterFrame(targetBind) *
                           Quaternion.Inverse(CharacterFrame(sourceBind));
            var sword = RequireSword(target);
            var swordMesh = sword.GetComponent<MeshFilter>().sharedMesh;
            var swordGrip = CalculateGripCenter(swordMesh);
            var body = RequireBody(target);
            var rightHand = targetBones["RightHand"];
            var curves = BonePairs.ToDictionary(
                item => item.Target, _ => new QuaternionCurves(), StringComparer.Ordinal);
            var hipsPosition = new VectorCurves();
            var translatedPositions = translatedBoneNames.ToDictionary(
                name => name, _ => new VectorCurves(), StringComparer.Ordinal);
            var swordRotation = new QuaternionCurves();
            var swordPosition = new VectorCurves();
            var frameCount = Mathf.Max(1, Mathf.RoundToInt(legacy.length * FrameRate));
            WaistSwordPose lastPose = default;
            try
            {
                AnimationMode.StartAnimationMode();
                for (var frame = 0; frame <= frameCount; frame++)
                {
                    Restore(sourceStates);
                    Restore(targetStates);
                    var time = legacy.length * frame / frameCount;
                    AnimationMode.SampleAnimationClip(source.gameObject, legacy, time);
                    var desiredModelRotations = new Dictionary<string, Quaternion>(StringComparer.Ordinal);
                    foreach (var pair in BonePairs)
                    {
                        var sample = ModelRotation(source, sourceBones[pair.Target]);
                        if (DirectionChildren.TryGetValue(pair.Target, out var childName))
                        {
                            var sourceRestDirection =
                                (sourceBind[childName].GetPosition() -
                                 sourceBind[pair.Target].GetPosition()).normalized;
                            var sourceSampleDirection =
                                (ModelPosition(source, sourceBones[childName]) -
                                 ModelPosition(source, sourceBones[pair.Target])).normalized;
                            var sourceDirectionDelta = Quaternion.FromToRotation(
                                sourceRestDirection, sourceSampleDirection);
                            var mappedDirectionDelta = frameMap * sourceDirectionDelta *
                                                       Quaternion.Inverse(frameMap);
                            var targetRestDirection =
                                (targetBind[childName].GetPosition() -
                                 targetBind[pair.Target].GetPosition()).normalized;
                            var targetDesiredDirection = mappedDirectionDelta * targetRestDirection;
                            var targetSwing = Quaternion.FromToRotation(
                                targetRestDirection, targetDesiredDirection);
                            desiredModelRotations[pair.Target] =
                                targetSwing * targetRestRotations[pair.Target];
                        }
                        else
                        {
                            var sourceWorldDelta = sample *
                                                   Quaternion.Inverse(sourceRestRotations[pair.Target]);
                            var mappedDelta = frameMap * sourceWorldDelta *
                                              Quaternion.Inverse(frameMap);
                            desiredModelRotations[pair.Target] =
                                mappedDelta * targetRestRotations[pair.Target];
                        }
                    }
                    foreach (var pair in BonePairs)
                    {
                        var bone = targetBones[pair.Target];
                        var desiredWorld = target.rotation * desiredModelRotations[pair.Target];
                        bone.localRotation = Quaternion.Inverse(bone.parent.rotation) * desiredWorld;
                    }
                    var sourceHipsDelta = ModelPosition(source, sourceBones["Hips"]) -
                                          sourceHipsRestPosition;
                    var desiredHipsModelPosition = targetHipsRestPosition +
                                                   frameMap * sourceHipsDelta * scale;
                    targetBones["Hips"].position = target.TransformPoint(desiredHipsModelPosition);
                    foreach (var name in translatedBoneNames)
                    {
                        var sourceLocalDelta = sourceBones[name].localPosition -
                                               sourceRestLocalPositions[name];
                        var sourceModelDelta = source.InverseTransformVector(
                            sourceBones[name].parent.TransformVector(sourceLocalDelta));
                        var targetModelDelta = frameMap * sourceModelDelta * scale;
                        var targetLocalDelta = targetBones[name].parent.InverseTransformVector(
                            target.TransformVector(targetModelDelta));
                        targetBones[name].localPosition = targetRestLocalPositions[name] +
                                                          targetLocalDelta;
                    }

                    foreach (var pair in BonePairs)
                        curves[pair.Target].Add(time, targetBones[pair.Target].localRotation);
                    hipsPosition.Add(time, targetBones["Hips"].localPosition);
                    foreach (var name in translatedBoneNames)
                        translatedPositions[name].Add(time, targetBones[name].localPosition);

                    Quaternion desiredSwordWorldRotation;
                    Vector3 desiredSwordWorldPosition;
                    if (fixedWaistPose.HasValue)
                    {
                        var waist = fixedWaistPose.Value;
                        var hips = targetBones["Hips"];
                        desiredSwordWorldRotation = hips.rotation * waist.LocalRotation;
                        desiredSwordWorldPosition = hips.TransformPoint(waist.LocalPosition);
                    }
                    else
                    {
                        desiredSwordWorldRotation = rightHand.rotation * LegacyHandSwordLocalRotation;
                        var gripOffset = desiredSwordWorldRotation *
                                         Vector3.Scale(swordGrip, sword.transform.lossyScale);
                        var palmAnchor = PalmAnchorLocal(body, rightHand);
                        desiredSwordWorldPosition = rightHand.TransformPoint(palmAnchor) - gripOffset;
                    }
                    sword.transform.rotation = desiredSwordWorldRotation;
                    sword.transform.position = desiredSwordWorldPosition;
                    swordRotation.Add(time, sword.transform.localRotation);
                    swordPosition.Add(time, sword.transform.localPosition);
                    if (frame == frameCount)
                    {
                        var hips = targetBones["Hips"];
                        lastPose = new WaistSwordPose(
                            hips.InverseTransformPoint(sword.transform.position),
                            Quaternion.Inverse(hips.rotation) * sword.transform.rotation);
                    }
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(sourceStates);
                Restore(targetStates);
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(outputPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = outputName };
                AssetDatabase.CreateAsset(clip, outputPath);
            }
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, binding, null);
            foreach (var pair in BonePairs)
            {
                var path = Path(target, targetBones[pair.Target]);
                curves[pair.Target].Write(clip, path);
            }
            hipsPosition.Write(clip, Path(target, targetBones["Hips"]));
            foreach (var name in translatedBoneNames)
                translatedPositions[name].Write(clip, Path(target, targetBones[name]));
            var swordPath = Path(target, sword.transform);
            swordRotation.Write(clip, swordPath);
            swordPosition.Write(clip, swordPath);
            clip.name = outputName;
            clip.frameRate = FrameRate;
            clip.wrapMode = WrapMode.ClampForever;
            AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            settings.loopBlend = false;
            settings.stopTime = legacy.length;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            if (Mathf.Abs(clip.length - legacy.length) > 0.0001f)
                throw new InvalidOperationException(
                    "Retargeted duration differs for " + outputName +
                    ". Actual=" + Num(clip.length) + ", Expected=" + Num(legacy.length) + ".");
            endWaistPose = fixedWaistPose ?? lastPose;
            return clip;
        }

        private static Quaternion CharacterFrame(
            IReadOnlyDictionary<string, Matrix4x4> bones)
        {
            var hips = bones["Hips"].GetPosition();
            var head = bones["Head"].GetPosition();
            var left = bones["LeftShoulder"].GetPosition();
            var right = bones["RightShoulder"].GetPosition();
            var up = (head - hips).normalized;
            var lateral = (right - left).normalized;
            var forward = Vector3.Cross(lateral, up).normalized;
            if (up.sqrMagnitude < 0.99f || lateral.sqrMagnitude < 0.99f || forward.sqrMagnitude < 0.99f)
                throw new InvalidOperationException("A slot-6 character frame could not be derived.");
            return Quaternion.LookRotation(forward, up);
        }

        private static Matrix4x4 BindModelMatrix(
            Transform model,
            SkinnedMeshRenderer renderer,
            Transform bone)
        {
            var index = Array.IndexOf(renderer.bones, bone);
            if (index < 0 || index >= renderer.sharedMesh.bindposes.Length)
                throw new InvalidOperationException("A mapped slot-6 bone has no matching bindpose: " + bone.name + ".");
            var rendererModel = model.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            return rendererModel * renderer.sharedMesh.bindposes[index].inverse;
        }

        private static Avatar BuildAvatar(Transform model, bool source)
        {
            var human = HumanMaps.Select(map => new HumanBone
            {
                humanName = HumanTrait.BoneName[(int)map.HumanBone],
                boneName = source ? map.Source : map.Target,
                limit = new HumanLimit { useDefaultValues = true }
            }).ToArray();
            var skeleton = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new SkeletonBone
                {
                    name = item.name,
                    position = item.localPosition,
                    rotation = item.localRotation,
                    scale = item.localScale
                }).ToArray();
            var description = new HumanDescription
            {
                human = human,
                skeleton = skeleton,
                upperArmTwist = 0.5f,
                lowerArmTwist = 0.5f,
                upperLegTwist = 0.5f,
                lowerLegTwist = 0.5f,
                armStretch = 0.05f,
                legStretch = 0.05f,
                feetSpacing = 0f,
                hasTranslationDoF = false
            };
            var avatar = AvatarBuilder.BuildHumanAvatar(model.gameObject, description);
            avatar.name = source ? "Ispant06LegacyTemporaryAvatar" : "Ispant06CurrentTemporaryAvatar";
            if (!avatar.isValid || !avatar.isHuman)
            {
                UnityEngine.Object.DestroyImmediate(avatar);
                throw new InvalidOperationException(
                    (source ? "Legacy" : "Current") + " temporary Humanoid avatar is invalid.");
            }
            return avatar;
        }

        private static AnimatorController CreateController(params AnimationClip[] clips)
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(NewControllerPath);
            if (existing != null)
                AssetDatabase.DeleteAsset(NewControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(NewControllerPath);
            var machine = controller.layers.Single().stateMachine;
            var states = clips.Select(clip =>
            {
                var state = machine.AddState(clip.name);
                state.motion = clip;
                state.speed = 1f;
                state.writeDefaultValues = true;
                return state;
            }).ToArray();
            machine.defaultState = states[0];
            for (var index = 0; index < states.Length; index++)
            {
                var transition = states[index].AddTransition(states[(index + 1) % states.Length]);
                transition.hasExitTime = true;
                transition.exitTime = 1f;
                transition.hasFixedDuration = true;
                transition.duration = 0f;
                transition.offset = 0f;
                transition.interruptionSource = TransitionInterruptionSource.None;
            }
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static Scene RequireScene(bool clean)
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (clean && scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp must be clean before this operation.");
            return scene;
        }

        private static GameObject RequireSlot(Scene scene)
        {
            var placement = scene.GetRootGameObjects().Single(item => item.name == PlacementName);
            return placement.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == SlotName).gameObject;
        }

        private static GameObject RequireCurrentModel(GameObject slot)
        {
            if (slot.transform.childCount != 1 || slot.transform.GetChild(0).name != CurrentModelName)
                throw new InvalidOperationException("Slot 6 does not contain the expected direct model.");
            return slot.transform.GetChild(0).gameObject;
        }

        private static Transform RequireStaticModel(Scene scene)
        {
            var placement = scene.GetRootGameObjects().Single(item => item.name == PlacementName);
            var slot = placement.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == StaticSlotName);
            if (slot.childCount != 1 || slot.GetChild(0).name != StaticModelName)
                throw new InvalidOperationException(
                    "Ispant_01_Static does not contain the expected static model.");
            return slot.GetChild(0);
        }

        private static IReadOnlyDictionary<string, StaticLeftArmPose> RetargetStaticLeftArmPose(
            Transform source,
            Transform target)
        {
            var result = new Dictionary<string, StaticLeftArmPose>(StringComparer.Ordinal);
            foreach (var name in StaticLeftArmBoneNames)
            {
                var sourceBone = RequireBone(source, name);
                _ = RequireBone(target, name);
                result[name] = new StaticLeftArmPose(
                    sourceBone.localPosition, sourceBone.localRotation);
            }
            return result;
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ??
            throw new InvalidOperationException("Required asset is missing: " + path + ".");

        private static AnimationClip RequireImportedClip(string path)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            if (clips.Length != 1)
                throw new InvalidOperationException("Expected one imported clip at " + path + ". Count=" + clips.Length + ".");
            return clips[0];
        }

        private static Transform RequireBone(Transform root, string name)
        {
            var values = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name).ToArray();
            if (values.Length != 1)
                throw new InvalidOperationException("Expected one bone named " + name + ". Count=" + values.Length + ".");
            return values[0];
        }

        private static SkinnedMeshRenderer RequireBody(Transform model) =>
            model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(item => item.name == "char1");

        private static MeshRenderer RequireSword(Transform model) =>
            model.GetComponentsInChildren<MeshRenderer>(true)
                .Single(item => item.name == "Ispant_Approved_LongSword_10K");

        private static Quaternion ModelRotation(Transform model, Transform item) =>
            (model.worldToLocalMatrix * item.localToWorldMatrix).rotation;

        private static Vector3 ModelPosition(Transform model, Transform item) =>
            model.InverseTransformPoint(item.position);

        private static Vector3 CalculateGripCenter(Mesh mesh)
        {
            var gripX = Mathf.Lerp(mesh.bounds.min.x, mesh.bounds.max.x, GripDistanceFromPommelRatio);
            var halfWidth = mesh.bounds.size.x * GripHalfWidthRatio;
            var values = mesh.vertices.Where(item => Mathf.Abs(item.x - gripX) <= halfWidth).ToArray();
            if (values.Length < 16)
                throw new InvalidOperationException("The current long-sword grip region differs.");
            var center = values.Aggregate(Vector3.zero, (sum, value) => sum + value) / values.Length;
            center.x = gripX;
            return center;
        }

        private static Vector3 PalmAnchorLocal(SkinnedMeshRenderer body, Transform hand)
        {
            var values = HandWeightedWorldVertices(body, hand).Select(hand.InverseTransformPoint).ToArray();
            var minimum = values.Min(item => item.y);
            var maximum = values.Max(item => item.y);
            var end = Mathf.Lerp(minimum, maximum, 1f - GripHandLongitudinalStartRatio);
            var fist = values.Where(item => item.y <= end).ToArray();
            if (fist.Length < 16)
                throw new InvalidOperationException("Too few current right-hand fist vertices were found.");
            return fist.Aggregate(Vector3.zero, (sum, value) => sum + value) / fist.Length;
        }

        private static Vector3 BakedNearestVerticesCenterWorld(
            SkinnedMeshRenderer body,
            Vector3 referenceWorld,
            int vertexCount)
        {
            var baked = new Mesh();
            try
            {
                body.BakeMesh(baked);
                var nearest = baked.vertices
                    .Select(body.transform.TransformPoint)
                    .OrderBy(vertex => (vertex - referenceWorld).sqrMagnitude)
                    .Take(vertexCount)
                    .ToArray();
                return nearest.Aggregate(Vector3.zero, (sum, vertex) => sum + vertex) /
                       nearest.Length;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static int[] FindNearestVertexIndicesWorld(
            SkinnedMeshRenderer body,
            Vector3 referenceWorld,
            int vertexCount)
        {
            var baked = new Mesh();
            try
            {
                body.BakeMesh(baked);
                return Enumerable.Range(0, baked.vertexCount)
                    .OrderBy(index =>
                    {
                        var world = body.transform.TransformPoint(baked.vertices[index]);
                        return (world - referenceWorld).sqrMagnitude;
                    })
                    .Take(vertexCount)
                    .ToArray();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static Vector3 VisibleRightHandReference(SkinnedMeshRenderer body)
        {
            var bounds = body.bounds;
            return new Vector3(
                bounds.min.x + bounds.size.x * 0.12f,
                bounds.min.y + bounds.size.y * 0.43f,
                bounds.center.z);
        }

        private static Vector3 BakedVertexCenterWorld(
            SkinnedMeshRenderer body,
            IReadOnlyCollection<int> vertexIndices)
        {
            var baked = new Mesh();
            try
            {
                body.BakeMesh(baked);
                var vertices = baked.vertices;
                return vertexIndices.Aggregate(
                           Vector3.zero,
                           (sum, index) => sum + body.transform.TransformPoint(vertices[index])) /
                       vertexIndices.Count;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static Vector3[] HandWeightedWorldVertices(SkinnedMeshRenderer body, Transform hand)
        {
            var mesh = body.sharedMesh;
            var handIndex = Array.IndexOf(body.bones, hand);
            if (handIndex < 0 || mesh.boneWeights.Length != mesh.vertexCount)
                throw new InvalidOperationException("The current RightHand skinning data differs.");
            var values = Enumerable.Range(0, mesh.vertexCount)
                .Where(index => WeightForBone(mesh.boneWeights[index], handIndex) >= 0.1f)
                .Select(index =>
                {
                    var value = Vector3.zero;
                    var weight = mesh.boneWeights[index];
                    AddSkin(ref value, mesh.vertices[index], weight.boneIndex0, weight.weight0, body);
                    AddSkin(ref value, mesh.vertices[index], weight.boneIndex1, weight.weight1, body);
                    AddSkin(ref value, mesh.vertices[index], weight.boneIndex2, weight.weight2, body);
                    AddSkin(ref value, mesh.vertices[index], weight.boneIndex3, weight.weight3, body);
                    return body.transform.TransformPoint(value);
                }).ToArray();
            if (values.Length < 4)
                throw new InvalidOperationException("Too few RightHand-weighted vertices were found.");
            return values;
        }

        private static void AddSkin(ref Vector3 value, Vector3 vertex, int boneIndex, float weight,
            SkinnedMeshRenderer body)
        {
            if (weight <= 0f) return;
            value += body.transform.worldToLocalMatrix.MultiplyPoint3x4(
                         body.bones[boneIndex].localToWorldMatrix.MultiplyPoint3x4(
                             body.sharedMesh.bindposes[boneIndex].MultiplyPoint3x4(vertex))) * weight;
        }

        private static float WeightForBone(BoneWeight value, int bone)
        {
            var result = 0f;
            if (value.boneIndex0 == bone) result += value.weight0;
            if (value.boneIndex1 == bone) result += value.weight1;
            if (value.boneIndex2 == bone) result += value.weight2;
            if (value.boneIndex3 == bone) result += value.weight3;
            return result;
        }

        private static void Restore(IEnumerable<TransformState> states)
        {
            foreach (var state in states) state.Restore();
        }

        private static bool Finite(Vector3 value) =>
            !(float.IsNaN(value.x) || float.IsInfinity(value.x) ||
              float.IsNaN(value.y) || float.IsInfinity(value.y) ||
              float.IsNaN(value.z) || float.IsInfinity(value.z));

        private static string OtherSlotSignatures(Transform placement, Transform excluded)
        {
            return string.Join("\n", placement.Cast<Transform>()
                .Where(item => item != excluded)
                .Select(item => item.name + "|" + item.childCount + "|" +
                                Vec(item.localPosition) + "|" + Vec(item.localEulerAngles) + "|" +
                                string.Join("/", item.GetComponentsInChildren<Transform>(true)
                                    .Select(child => child.name))));
        }

        private static void RequireSame(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(message);
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        private static string Absolute(string relative) =>
            System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", relative));

        private static string Num(float value) => value.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture);

        private static void AppendAssetModel(StringBuilder report, string label, string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path) ??
                throw new InvalidOperationException("Model asset is missing: " + path + ".");
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject ??
                throw new InvalidOperationException("Could not instantiate model asset: " + path + ".");
            instance.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                AppendModel(report, label, instance);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void AppendModel(StringBuilder report, string label, GameObject model)
        {
            var transforms = model.GetComponentsInChildren<Transform>(true);
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            var animator = model.GetComponent<Animator>();
            report.AppendLine(label + "Model=" + model.name);
            report.AppendLine(label + "TransformCount=" + transforms.Length);
            report.AppendLine(label + "Animator=" + (animator != null));
            report.AppendLine(label + "Controller=" +
                (animator != null && animator.runtimeAnimatorController != null
                    ? AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)
                    : "None"));
            foreach (var renderer in renderers)
            {
                var mesh = renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh :
                    renderer is MeshRenderer ? renderer.GetComponent<MeshFilter>()?.sharedMesh : null;
                report.AppendLine(
                    label + "Renderer=" + Path(model.transform, renderer.transform) +
                    "|Type=" + renderer.GetType().Name +
                    "|Enabled=" + renderer.enabled +
                    "|Mesh=" + (mesh != null ? mesh.name : "None") +
                    "|Vertices=" + (mesh != null ? mesh.vertexCount : 0) +
                    "|SubMeshes=" + (mesh != null ? mesh.subMeshCount : 0) +
                    "|Materials=" + renderer.sharedMaterials.Length +
                    "|Bones=" + (renderer is SkinnedMeshRenderer smr ? smr.bones.Length : 0));
                if (renderer is SkinnedMeshRenderer body && body.name == "char1" && mesh != null)
                    AppendConnectedComponents(report, label, body, mesh);
            }
            foreach (var transform in transforms.Where(IsRelevantTransform))
            {
                report.AppendLine(
                    label + "Transform=" + Path(model.transform, transform) +
                    "|LocalPosition=" + Vec(transform.localPosition) +
                    "|LocalRotation=" + Vec(transform.localEulerAngles) +
                    "|LocalScale=" + Vec(transform.localScale));
            }
        }

        private static void AppendConnectedComponents(
            StringBuilder report,
            string label,
            SkinnedMeshRenderer renderer,
            Mesh mesh)
        {
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var adjacency = Enumerable.Range(0, vertices.Length)
                .Select(_ => new List<int>()).ToArray();
            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                var triangles = mesh.GetTriangles(subMesh);
                for (var index = 0; index < triangles.Length; index += 3)
                {
                    Connect(adjacency, triangles[index], triangles[index + 1]);
                    Connect(adjacency, triangles[index + 1], triangles[index + 2]);
                    Connect(adjacency, triangles[index + 2], triangles[index]);
                }
            }
            var seen = new bool[vertices.Length];
            var components = new List<ComponentInfo>();
            for (var seed = 0; seed < vertices.Length; seed++)
            {
                if (seen[seed]) continue;
                var stack = new Stack<int>();
                var members = new List<int>();
                stack.Push(seed);
                seen[seed] = true;
                while (stack.Count > 0)
                {
                    var vertex = stack.Pop();
                    members.Add(vertex);
                    foreach (var neighbor in adjacency[vertex])
                    {
                        if (seen[neighbor]) continue;
                        seen[neighbor] = true;
                        stack.Push(neighbor);
                    }
                }
                var bounds = new Bounds(vertices[members[0]], Vector3.zero);
                var boneTotals = new Dictionary<int, float>();
                foreach (var vertex in members)
                {
                    bounds.Encapsulate(vertices[vertex]);
                    AddWeight(boneTotals, weights[vertex].boneIndex0, weights[vertex].weight0);
                    AddWeight(boneTotals, weights[vertex].boneIndex1, weights[vertex].weight1);
                    AddWeight(boneTotals, weights[vertex].boneIndex2, weights[vertex].weight2);
                    AddWeight(boneTotals, weights[vertex].boneIndex3, weights[vertex].weight3);
                }
                var dominant = boneTotals.OrderByDescending(item => item.Value).FirstOrDefault();
                var boneName = dominant.Key >= 0 && dominant.Key < renderer.bones.Length &&
                               renderer.bones[dominant.Key] != null
                    ? renderer.bones[dominant.Key].name
                    : "None";
                components.Add(new ComponentInfo(seed, members.Count, bounds, boneName, dominant.Value));
            }
            report.AppendLine(label + "ConnectedComponentCount=" + components.Count);
            foreach (var component in components
                         .Where(item => item.VertexCount >= 20)
                         .OrderByDescending(item => item.VertexCount))
            {
                report.AppendLine(
                    label + "Component=Seed:" + component.Seed +
                    "|Vertices=" + component.VertexCount +
                    "|Center=" + Vec(component.Bounds.center) +
                    "|Size=" + Vec(component.Bounds.size) +
                    "|DominantBone=" + component.DominantBone +
                    "|DominantWeight=" + component.DominantWeight.ToString("0.###"));
            }
        }

        private static void AppendMusketComponentCandidates(
            StringBuilder report,
            SkinnedMeshRenderer renderer,
            Mesh mesh)
        {
            var leftShoulderIndex = Array.FindIndex(
                renderer.bones, item => item != null && item.name == "LeftShoulder");
            if (leftShoulderIndex < 0)
                throw new InvalidOperationException("The current LeftShoulder bone is missing.");

            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var adjacency = Enumerable.Range(0, vertices.Length)
                .Select(_ => new List<int>()).ToArray();
            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                var triangles = mesh.GetTriangles(subMesh);
                for (var index = 0; index < triangles.Length; index += 3)
                {
                    Connect(adjacency, triangles[index], triangles[index + 1]);
                    Connect(adjacency, triangles[index + 1], triangles[index + 2]);
                    Connect(adjacency, triangles[index + 2], triangles[index]);
                }
            }

            var seen = new bool[vertices.Length];
            var candidates = new List<MusketComponentCandidate>();
            var spatialCandidates = new List<MusketSpatialCandidate>();
            var lineStart = new Vector3(-0.69f, 0.56f, -0.156f);
            var lineEnd = new Vector3(-0.04f, 1.43f, -0.156f);
            for (var seed = 0; seed < vertices.Length; seed++)
            {
                if (seen[seed]) continue;
                var stack = new Stack<int>();
                var members = new List<int>();
                stack.Push(seed);
                seen[seed] = true;
                while (stack.Count > 0)
                {
                    var vertex = stack.Pop();
                    members.Add(vertex);
                    foreach (var neighbor in adjacency[vertex])
                    {
                        if (seen[neighbor]) continue;
                        seen[neighbor] = true;
                        stack.Push(neighbor);
                    }
                }

                var bounds = new Bounds(vertices[members[0]], Vector3.zero);
                var leftWeight = 0f;
                var boneTotals = new Dictionary<int, float>();
                foreach (var vertex in members)
                {
                    bounds.Encapsulate(vertices[vertex]);
                    leftWeight += WeightForBone(weights[vertex], leftShoulderIndex);
                    AddWeight(boneTotals, weights[vertex].boneIndex0, weights[vertex].weight0);
                    AddWeight(boneTotals, weights[vertex].boneIndex1, weights[vertex].weight1);
                    AddWeight(boneTotals, weights[vertex].boneIndex2, weights[vertex].weight2);
                    AddWeight(boneTotals, weights[vertex].boneIndex3, weights[vertex].weight3);
                }
                var ratio = leftWeight / members.Count;
                var dominant = boneTotals.OrderByDescending(item => item.Value).FirstOrDefault();
                var dominantBone = dominant.Key >= 0 && dominant.Key < renderer.bones.Length &&
                                   renderer.bones[dominant.Key] != null
                    ? renderer.bones[dominant.Key].name
                    : "None";
                var lineDistance = DistanceToSegment(bounds.center, lineStart, lineEnd);
                if (bounds.center.x >= -0.8f && bounds.center.x <= 0.12f &&
                    bounds.center.y >= 0.42f && bounds.center.y <= 1.55f &&
                    bounds.center.z >= -0.3f && bounds.center.z <= 0.02f &&
                    lineDistance <= 0.18f)
                    spatialCandidates.Add(new MusketSpatialCandidate(
                        seed, members.Count, bounds, dominantBone,
                        dominant.Value / members.Count, lineDistance,
                        PrincipalAxis(members, vertices)));
                if (ratio < 0.95f || members.Count < 4 || bounds.size.magnitude < 0.02f)
                    continue;
                candidates.Add(new MusketComponentCandidate(
                    seed, members.Count, bounds, ratio, PrincipalAxis(members, vertices)));
            }

            report.AppendLine("CurrentMesh=" + mesh.name);
            report.AppendLine("CurrentVertices=" + mesh.vertexCount);
            report.AppendLine("LeftShoulderIndex=" + leftShoulderIndex);
            report.AppendLine("RigidLeftShoulderCandidateCount=" + candidates.Count);
            foreach (var candidate in candidates
                         .OrderByDescending(item => item.Bounds.size.magnitude)
                         .ThenBy(item => item.Seed))
            {
                report.AppendLine(
                    "Candidate=Seed:" + candidate.Seed +
                    "|Vertices=" + candidate.VertexCount +
                    "|Center=" + Vec(candidate.Bounds.center) +
                    "|Size=" + Vec(candidate.Bounds.size) +
                    "|LeftShoulderRatio=" + Num(candidate.LeftShoulderRatio) +
                    "|PrincipalAxis=" + Vec(candidate.PrincipalAxis));
            }
            report.AppendLine("BackDiagonalSpatialCandidateCount=" + spatialCandidates.Count);
            foreach (var candidate in spatialCandidates
                         .OrderBy(item => item.Bounds.center.y)
                         .ThenBy(item => item.Bounds.center.x)
                         .ThenBy(item => item.Seed))
                report.AppendLine(
                    "BackDiagonal=Seed:" + candidate.Seed +
                    "|Vertices=" + candidate.VertexCount +
                    "|Center=" + Vec(candidate.Bounds.center) +
                    "|Size=" + Vec(candidate.Bounds.size) +
                    "|DominantBone=" + candidate.DominantBone +
                    "|DominantRatio=" + Num(candidate.DominantRatio) +
                    "|LineDistance=" + Num(candidate.LineDistance) +
                    "|Axis=" + Vec(candidate.PrincipalAxis));
        }

        private static float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            var segment = end - start;
            var t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / segment.sqrMagnitude);
            return Vector3.Distance(point, start + segment * t);
        }

        private static Vector3 PrincipalAxis(IReadOnlyList<int> members, IReadOnlyList<Vector3> vertices)
        {
            var center = members.Aggregate(Vector3.zero, (sum, index) => sum + vertices[index]) /
                         members.Count;
            var axis = Vector3.right;
            for (var iteration = 0; iteration < 16; iteration++)
            {
                var next = Vector3.zero;
                foreach (var index in members)
                {
                    var offset = vertices[index] - center;
                    next += offset * Vector3.Dot(offset, axis);
                }
                if (next.sqrMagnitude <= 0.0000000001f) break;
                axis = next.normalized;
            }
            if (axis.x < 0f || (Mathf.Approximately(axis.x, 0f) && axis.y < 0f)) axis = -axis;
            return axis;
        }

        private static void Connect(IList<int>[] adjacency, int left, int right)
        {
            adjacency[left].Add(right);
            adjacency[right].Add(left);
        }

        private static void AddWeight(IDictionary<int, float> totals, int bone, float weight)
        {
            if (weight <= 0f) return;
            totals[bone] = totals.TryGetValue(bone, out var current) ? current + weight : weight;
        }

        private static bool IsRelevantTransform(Transform item)
        {
            var name = item.name;
            return name.IndexOf("hips", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("spine", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("shoulder", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("arm", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("hand", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("sword", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("musket", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("rifle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("char", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string Path(Transform root, Transform item)
        {
            if (item == root)
                return string.Empty;
            var parts = new System.Collections.Generic.List<string>();
            for (var cursor = item; cursor != null && cursor != root; cursor = cursor.parent)
                parts.Add(cursor.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string Vec(Vector3 value)
        {
            return value.x.ToString("0.######") + "," +
                   value.y.ToString("0.######") + "," +
                   value.z.ToString("0.######");
        }

        private readonly struct ComponentInfo
        {
            public readonly int Seed;
            public readonly int VertexCount;
            public readonly Bounds Bounds;
            public readonly string DominantBone;
            public readonly float DominantWeight;

            public ComponentInfo(int seed, int vertexCount, Bounds bounds, string dominantBone, float dominantWeight)
            {
                Seed = seed;
                VertexCount = vertexCount;
                Bounds = bounds;
                DominantBone = dominantBone;
                DominantWeight = dominantWeight;
            }
        }

        private readonly struct MusketComponentCandidate
        {
            public readonly int Seed;
            public readonly int VertexCount;
            public readonly Bounds Bounds;
            public readonly float LeftShoulderRatio;
            public readonly Vector3 PrincipalAxis;

            public MusketComponentCandidate(
                int seed,
                int vertexCount,
                Bounds bounds,
                float leftShoulderRatio,
                Vector3 principalAxis)
            {
                Seed = seed;
                VertexCount = vertexCount;
                Bounds = bounds;
                LeftShoulderRatio = leftShoulderRatio;
                PrincipalAxis = principalAxis;
            }
        }

        private readonly struct MusketSpatialCandidate
        {
            public readonly int Seed;
            public readonly int VertexCount;
            public readonly Bounds Bounds;
            public readonly string DominantBone;
            public readonly float DominantRatio;
            public readonly float LineDistance;
            public readonly Vector3 PrincipalAxis;

            public MusketSpatialCandidate(
                int seed,
                int vertexCount,
                Bounds bounds,
                string dominantBone,
                float dominantRatio,
                float lineDistance,
                Vector3 principalAxis)
            {
                Seed = seed;
                VertexCount = vertexCount;
                Bounds = bounds;
                DominantBone = dominantBone;
                DominantRatio = dominantRatio;
                LineDistance = lineDistance;
                PrincipalAxis = principalAxis;
            }
        }

        private readonly struct MeshPartition
        {
            public readonly Mesh BodyMesh;
            public readonly Mesh MusketMesh;
            public readonly int OriginalVertexCount;
            public readonly int OriginalTriangleCount;
            public readonly int BodyTriangleCount;
            public readonly int MusketTriangleCount;
            public readonly int MusketVertexCount;

            public MeshPartition(
                Mesh bodyMesh,
                Mesh musketMesh,
                int originalVertexCount,
                int originalTriangleCount,
                int bodyTriangleCount,
                int musketTriangleCount,
                int musketVertexCount)
            {
                BodyMesh = bodyMesh;
                MusketMesh = musketMesh;
                OriginalVertexCount = originalVertexCount;
                OriginalTriangleCount = originalTriangleCount;
                BodyTriangleCount = bodyTriangleCount;
                MusketTriangleCount = musketTriangleCount;
                MusketVertexCount = musketVertexCount;
            }
        }

        private readonly struct MusketInspectionMetrics
        {
            public readonly float ContinuityError;
            public readonly float MaximumRightGripDistance;
            public readonly float FinalLeftForegripDistance;
            public readonly float MaximumTrajectoryAngleError;
            public readonly float FinalMuzzleForwardAngle;

            public MusketInspectionMetrics(
                float continuityError,
                float maximumRightGripDistance,
                float finalLeftForegripDistance,
                float maximumTrajectoryAngleError,
                float finalMuzzleForwardAngle)
            {
                ContinuityError = continuityError;
                MaximumRightGripDistance = maximumRightGripDistance;
                FinalLeftForegripDistance = finalLeftForegripDistance;
                MaximumTrajectoryAngleError = maximumTrajectoryAngleError;
                FinalMuzzleForwardAngle = finalMuzzleForwardAngle;
            }
        }

        private readonly struct ShoulderPositionMetrics
        {
            public readonly float MaximumRetargetError;
            public readonly float FinalLeftTranslation;
            public readonly float FinalRightTranslation;

            public ShoulderPositionMetrics(
                float maximumRetargetError,
                float finalLeftTranslation,
                float finalRightTranslation)
            {
                MaximumRetargetError = maximumRetargetError;
                FinalLeftTranslation = finalLeftTranslation;
                FinalRightTranslation = finalRightTranslation;
            }
        }

        private readonly struct ComponentGroup
        {
            public readonly string Name;
            public readonly int[] Seeds;

            public ComponentGroup(string name, int[] seeds)
            {
                Name = name;
                Seeds = seeds;
            }
        }

        private readonly struct BonePair
        {
            public readonly string Source;
            public readonly string Target;

            public BonePair(string source, string target)
            {
                Source = source;
                Target = target;
            }
        }

        private readonly struct HumanMap
        {
            public readonly HumanBodyBones HumanBone;
            public readonly string Source;
            public readonly string Target;

            public HumanMap(HumanBodyBones humanBone, string source, string target)
            {
                HumanBone = humanBone;
                Source = source;
                Target = target;
            }
        }

        private readonly struct WaistSwordPose
        {
            public readonly Vector3 LocalPosition;
            public readonly Quaternion LocalRotation;

            public WaistSwordPose(Vector3 localPosition, Quaternion localRotation)
            {
                LocalPosition = localPosition;
                LocalRotation = localRotation;
            }
        }

        private sealed class QuaternionCurves
        {
            private readonly List<Keyframe> x = new List<Keyframe>();
            private readonly List<Keyframe> y = new List<Keyframe>();
            private readonly List<Keyframe> z = new List<Keyframe>();
            private readonly List<Keyframe> w = new List<Keyframe>();
            private Quaternion previous;
            private bool hasPrevious;

            public void Add(float time, Quaternion value)
            {
                value.Normalize();
                if (hasPrevious && Quaternion.Dot(previous, value) < 0f)
                    value = new Quaternion(-value.x, -value.y, -value.z, -value.w);
                previous = value;
                hasPrevious = true;
                x.Add(new Keyframe(time, value.x));
                y.Add(new Keyframe(time, value.y));
                z.Add(new Keyframe(time, value.z));
                w.Add(new Keyframe(time, value.w));
            }

            public void Write(AnimationClip clip, string path)
            {
                SetLinearCurve(clip, path, "m_LocalRotation.x", x);
                SetLinearCurve(clip, path, "m_LocalRotation.y", y);
                SetLinearCurve(clip, path, "m_LocalRotation.z", z);
                SetLinearCurve(clip, path, "m_LocalRotation.w", w);
            }
        }

        private sealed class VectorCurves
        {
            private readonly List<Keyframe> x = new List<Keyframe>();
            private readonly List<Keyframe> y = new List<Keyframe>();
            private readonly List<Keyframe> z = new List<Keyframe>();

            public void Add(float time, Vector3 value)
            {
                x.Add(new Keyframe(time, value.x));
                y.Add(new Keyframe(time, value.y));
                z.Add(new Keyframe(time, value.z));
            }

            public void Write(AnimationClip clip, string path)
            {
                SetLinearCurve(clip, path, "m_LocalPosition.x", x);
                SetLinearCurve(clip, path, "m_LocalPosition.y", y);
                SetLinearCurve(clip, path, "m_LocalPosition.z", z);
            }
        }

        private static void SetLinearCurve(
            AnimationClip clip,
            string path,
            string property,
            IList<Keyframe> keys)
        {
            var curve = new AnimationCurve(keys.ToArray());
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
            }
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property),
                curve);
        }

        private sealed class TransformState
        {
            private readonly Transform target;
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            public TransformState(Transform target)
            {
                this.target = target;
                position = target.localPosition;
                rotation = target.localRotation;
                scale = target.localScale;
            }

            public void Restore()
            {
                if (target == null) return;
                target.localPosition = position;
                target.localRotation = rotation;
                target.localScale = scale;
            }

            public bool Matches(Transform item, float tolerance)
            {
                return target == item &&
                       Vector3.Distance(position, item.localPosition) <= tolerance &&
                       Quaternion.Angle(rotation, item.localRotation) <= tolerance &&
                       Vector3.Distance(scale, item.localScale) <= tolerance;
            }
        }

        private sealed class RendererState
        {
            private readonly Renderer target;
            private readonly bool enabled;

            public RendererState(Renderer target)
            {
                this.target = target;
                enabled = target.enabled;
            }

            public void Restore()
            {
                if (target != null) target.enabled = enabled;
            }
        }

        private readonly struct ClipSample
        {
            public readonly AnimationClip Clip;
            public readonly float Time;

            public ClipSample(AnimationClip clip, float time)
            {
                Clip = clip;
                Time = time;
            }
        }

        private readonly struct StaticLeftArmPose
        {
            public readonly Vector3 LocalPosition;
            public readonly Quaternion LocalRotation;

            public StaticLeftArmPose(Vector3 localPosition, Quaternion localRotation)
            {
                LocalPosition = localPosition;
                LocalRotation = localRotation;
            }
        }
    }
}
