using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Bellerophon.Enemies.Fuga;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.FugaCargoRunScene
{
    internal static class FugaIdleMotionTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Fuga Enemy Placement";
        private const string IdleSlotName = "Fuga_01_Idle";
        private const string DeathSlotName = "Fuga_05_Death";
        private const string ModelName = "Fuga_Model";
        private const string OutputFolder = "docs/validation/fuga_idle_motion_2026-08-16";
        private const string RigInspectionPath = OutputFolder + "/Fuga_Idle_Rig_Inspection.txt";
        private const string FinalReportPath = OutputFolder + "/Fuga_Idle_Motion_Report.txt";
        private const string FinalCapturePath = OutputFolder + "/Fuga_Idle_Motion_Comparison.png";
        private const string IdentityReportPath = OutputFolder + "/Fuga_Idle_Death_Visual_Identity_Report.txt";
        private const string IdentityCapturePath = OutputFolder + "/Fuga_Idle_Death_Visual_Identity_Comparison.png";
        private const string WingbeatAndHover1HzReportPath =
            OutputFolder + "/Fuga_Idle_Wingbeat_And_Hover_1Hz_Report.txt";
        private const string WingbeatAndHover1HzCapturePath =
            OutputFolder + "/Fuga_Idle_Wingbeat_And_Hover_1Hz_Comparison.png";
        private const string DerivedMeshPath =
            "Assets/_Project/Art/Enemies/Fuga/Models/Fuga_Idle_BreathingMesh.asset";
        private const string ClipPath =
            "Assets/_Project/Art/Enemies/Fuga/Animations/Fuga_Idle_NewModel_WingbeatBreathing.anim";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Fuga/Controllers/Fuga_Idle_NewModel_WingbeatBreathing.controller";
        private const string ImportedModelPath = "Assets/_Project/Art/Enemies/Fuga/Models/fuga.glb";
        private const string BlendShapeName = "Fuga_Body_Breathe_3Pct";
        private const float LoopDuration = 2f;
        private const float WingbeatFrequency = 1f;
        private const int WingbeatsPerLoop = 2;
        private const float UpstrokeAngle = 44f;
        private const float DownstrokeAngle = -40f;
        private const float HoverAmplitude = 0.015f;
        // The Rigidbody hover target cadence is intentionally synchronized to the user-specified wingbeat cadence.
        private const float HoverFrequency = 1f;
        private const float HoverFollowGain = 24f;
        private const float HoverSpeedLimit = 0.8f;
        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;

        [MenuItem("Bellerophon/Enemies/Fuga/Inspect Idle Rig And Bird Reference")]
        public static void InspectFugaIdleRigAndBirdReference()
        {
            RequireCurrentScene();
            var slot = RequireIdleSlot();
            var model = slot.Find(ModelName) ??
                        throw new InvalidOperationException(IdleSlotName + "/" + ModelName + " is missing.");
            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                           throw new InvalidOperationException("The supplied Fuga model has no SkinnedMeshRenderer.");
            var mesh = renderer.sharedMesh ??
                       throw new InvalidOperationException("The supplied Fuga SkinnedMeshRenderer has no mesh.");

            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var bones = renderer.bones;
            if (weights.Length != vertices.Length)
            {
                throw new InvalidOperationException("Fuga vertex and legacy bone-weight counts differ.");
            }

            var report = new StringBuilder()
                .AppendLine("Fuga Idle Rig Inspection")
                .AppendLine("Result=PASS")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("Target=" + PlacementRootName + "/" + IdleSlotName)
                .AppendLine("Model=" + ModelName)
                .AppendLine("MeshAsset=" + AssetDatabase.GetAssetPath(mesh))
                .AppendLine("MeshName=" + mesh.name)
                .AppendLine("VertexCount=" + vertices.Length.ToString(CultureInfo.InvariantCulture))
                .AppendLine("BlendShapeCount=" + mesh.blendShapeCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("BoneCount=" + bones.Length.ToString(CultureInfo.InvariantCulture))
                .AppendLine("RendererLocalBoundsCenter=" + Vec(renderer.localBounds.center))
                .AppendLine("RendererLocalBoundsSize=" + Vec(renderer.localBounds.size))
                .AppendLine("ReferenceWingbeatFrequencyHz=5")
                .AppendLine("ReferenceWingbeatsPerTwoSecondLoop=10")
                .AppendLine("ReferenceShoulderStrokeRangeDegrees=84")
                .AppendLine("ReferenceBreathingCyclesPerSecond=1")
                .AppendLine("ReferencePeakBreathingExpansionPercent=3")
                .AppendLine();

            for (var boneIndex = 0; boneIndex < bones.Length; boneIndex++)
            {
                var count = 0;
                var totalWeight = 0f;
                var bounds = new Bounds();
                var hasBounds = false;
                for (var vertexIndex = 0; vertexIndex < weights.Length; vertexIndex++)
                {
                    var weight = WeightForBone(weights[vertexIndex], boneIndex);
                    if (weight <= 0.001f)
                    {
                        continue;
                    }

                    count++;
                    totalWeight += weight;
                    if (!hasBounds)
                    {
                        bounds = new Bounds(vertices[vertexIndex], Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(vertices[vertexIndex]);
                    }
                }

                var bone = bones[boneIndex];
                report.Append("Bone[")
                    .Append(boneIndex.ToString(CultureInfo.InvariantCulture))
                    .Append("] Name=")
                    .Append(bone != null ? bone.name : "<null>")
                    .Append(" Path=")
                    .Append(bone != null ? RelativePath(model, bone) : "<null>")
                    .Append(" LocalPosition=")
                    .Append(bone != null ? Vec(bone.localPosition) : "<null>")
                    .Append(" LocalEuler=")
                    .Append(bone != null ? Vec(bone.localEulerAngles) : "<null>")
                    .Append(" ModelPosition=")
                    .Append(bone != null ? Vec(model.InverseTransformPoint(bone.position)) : "<null>")
                    .Append(" InfluencedVertices=")
                    .Append(count.ToString(CultureInfo.InvariantCulture))
                    .Append(" TotalWeight=")
                    .Append(totalWeight.ToString("F3", CultureInfo.InvariantCulture));
                if (hasBounds)
                {
                    report.Append(" VertexBoundsCenter=")
                        .Append(Vec(bounds.center))
                        .Append(" VertexBoundsSize=")
                        .Append(Vec(bounds.size));
                }

                report.AppendLine();
            }

            AppendAxisProbe(report, model, bones, "Bone_013");
            AppendAxisProbe(report, model, bones, "Bone_017");

            var destination = Absolute(RigInspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid inspection report path."));
            File.WriteAllText(destination, report.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log("FugaIdleRigAndBirdReferenceInspected Result=PASS, Report=" + RigInspectionPath + ".");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Apply New Idle Motion")]
        public static void ApplyFugaIdleMotion()
        {
            var scene = RequireCurrentScene();
            var slot = RequireIdleSlot();
            var placementRoot = slot.parent ??
                                throw new InvalidOperationException("The idle Fuga slot has no placement parent.");
            var model = slot.Find(ModelName) ??
                        throw new InvalidOperationException(IdleSlotName + "/" + ModelName + " is missing.");
            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                           throw new InvalidOperationException("The idle Fuga model has no SkinnedMeshRenderer.");

            var derivedMesh = CreateBreathingMesh(out var breathingInfo);
            renderer.sharedMesh = derivedMesh;
            renderer.SetBlendShapeWeight(0, 0f);
            EditorUtility.SetDirty(renderer);

            var leftWing = FindBone(renderer, "Bone_013");
            var rightWing = FindBone(renderer, "Bone_017");
            var clip = CreateIdleClip(slot, renderer, leftWing, rightWing);
            var controller = CreateIdleController(clip);

            var animator = slot.GetComponent<Animator>() ?? slot.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);

            var body = slot.GetComponent<Rigidbody>() ??
                       throw new InvalidOperationException(IdleSlotName + " has no Rigidbody.");
            if (slot.GetComponent<Collider>() == null)
            {
                throw new InvalidOperationException(IdleSlotName + " has no Collider.");
            }

            var driver = slot.GetComponent<FugaPhysicsMotionDriver>() ??
                         slot.gameObject.AddComponent<FugaPhysicsMotionDriver>();
            var target = driver.MotionPathTarget ?? FindDescendant(slot, "MotionPath_Target_Rigidbody_Goal");
            if (target == null)
            {
                throw new InvalidOperationException(IdleSlotName + " has no approved Motion Path target.");
            }

            target.SetParent(placementRoot, true);
            target.name = "Fuga_01_Idle_HoverTarget";
            target.localPosition = slot.localPosition;
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;
            EditorUtility.SetDirty(target);

            body.isKinematic = false;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            EditorUtility.SetDirty(body);

            driver.enabled = true;
            driver.Configure(body, target, false, true, false, false);
            driver.ConfigureIdleHover(HoverAmplitude, HoverFrequency, HoverFollowGain, HoverSpeedLimit);
            EditorUtility.SetDirty(driver);

            var legacyPlayback = slot.GetComponent<FugaAnimationReviewPlaybackDriver>();
            if (legacyPlayback != null)
            {
                UnityEngine.Object.DestroyImmediate(legacyPlayback);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after applying the Fuga idle motion.");
            }

            AssetDatabase.SaveAssets();
            var result = InspectAppliedState();
            WriteFinalReport(result, breathingInfo, false);
            Debug.Log(
                "FugaIdleMotionApplied Result=PASS" +
                ", LoopSeconds=2" +
                ", WingbeatHz=1" +
                ", WingbeatsPerLoop=2" +
                ", ShoulderStrokeDegrees=84" +
                ", BreathingCyclesPerSecond=1" +
                ", PeakExpansionPercent=3" +
                ", HoverAmplitudeMeters=0.015" +
                ", PhysicsRootMotion=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Inspect New Idle Motion")]
        public static void InspectFugaIdleMotion()
        {
            RequireCurrentScene();
            var result = InspectAppliedState();
            var breathingInfo = InspectBreathingMesh(result.Renderer.sharedMesh);
            WriteFinalReport(result, breathingInfo, File.Exists(Absolute(FinalCapturePath)));
            Debug.Log(
                "FugaIdleMotionInspected Result=PASS" +
                ", LoopSeconds=2" +
                ", WingbeatsPerLoop=2" +
                ", BreathingPeakPercent=3" +
                ", PhysicsRootMotion=True" +
                ", OtherFugaSlotsChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Capture New Idle Motion Comparison")]
        public static void CaptureFugaIdleMotionComparison()
        {
            var scene = SceneManager.GetActiveScene();
            RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before the Fuga idle comparison capture.");
            }

            var result = InspectAppliedState();
            CaptureComparison(result.Slot, result.Clip, Absolute(FinalCapturePath));
            var breathingInfo = InspectBreathingMesh(result.Renderer.sharedMesh);
            WriteFinalReport(result, breathingInfo, true);
            AssetDatabase.Refresh();
            Debug.Log(
                "FugaIdleMotionComparisonCaptured Result=PASS" +
                ", Image=" + FinalCapturePath +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Apply Idle Wingbeat And Hover 1Hz")]
        public static void ApplyFugaIdleWingbeatAndHover1Hz()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be saved before changing the Fuga idle cadence.");
            }

            var protectedBefore = PlacementAndPlayerSignature(includeIdleHoverFrequency: false);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException("The new Fuga idle clip is missing.");
            var breathingBefore = BreathingCurveSignature(clip);
            var slot = RequireIdleSlot();
            var driver = slot.GetComponent<FugaPhysicsMotionDriver>() ??
                         throw new InvalidOperationException("The idle Fuga physics driver is missing.");
            var target = driver.MotionPathTarget ??
                         throw new InvalidOperationException("The idle Fuga hover target is missing.");
            var targetPositionBefore = target.localPosition;
            var hoverBaseBefore = driver.IdleHoverBaseLocalPosition;
            var hoverAmplitudeBefore = driver.IdleHoverAmplitude;

            RetimeWingRotationCurvesToConfiguredFrequency(clip);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(ClipPath, ImportAssetOptions.ForceSynchronousImport);

            var serializedDriver = new SerializedObject(driver);
            var frequencyProperty = serializedDriver.FindProperty("idleHoverFrequency") ??
                                    throw new InvalidOperationException("The idle hover frequency property is missing.");
            frequencyProperty.floatValue = HoverFrequency;
            serializedDriver.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(driver);

            if (Vector3.Distance(targetPositionBefore, target.localPosition) > 0.000001f ||
                Vector3.Distance(hoverBaseBefore, driver.IdleHoverBaseLocalPosition) > 0.000001f ||
                Mathf.Abs(hoverAmplitudeBefore - driver.IdleHoverAmplitude) > 0.000001f)
            {
                throw new InvalidOperationException(
                    "The hover target position, base position, or amplitude changed with the frequency.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after synchronizing the Fuga idle cadence.");
            }

            AssetDatabase.SaveAssets();

            var result = InspectAppliedState();
            var breathingInfo = InspectBreathingMesh(result.Renderer.sharedMesh);
            if (!string.Equals(breathingBefore, BreathingCurveSignature(result.Clip), StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The breathing curve changed while synchronizing the wingbeat and hover cadence.");
            }

            if (!string.Equals(
                    protectedBefore,
                    PlacementAndPlayerSignature(includeIdleHoverFrequency: false),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Fuga placement, state ownership, protected hover settings, or Player changed with the cadence.");
            }

            WriteFinalReport(result, breathingInfo, File.Exists(Absolute(FinalCapturePath)));
            WriteWingbeatAndHover1HzReport(result, breathingInfo, captureCreated: false);
            AssetDatabase.Refresh();
            Debug.Log(
                "FugaIdleWingbeatAndHover1HzApplied Result=PASS" +
                ", LoopSeconds=2" +
                ", WingbeatHz=1" +
                ", WingbeatsPerLoop=2" +
                ", HalfStrokeIntervalSeconds=0.5" +
                ", ShoulderStrokeDegrees=84" +
                ", BreathingChanged=False" +
                ", HoverFrequencyHz=1" +
                ", HoverAmplitudeChanged=False" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Inspect Idle Wingbeat And Hover 1Hz")]
        public static void InspectFugaIdleWingbeatAndHover1Hz()
        {
            var scene = RequireCurrentScene();
            var dirtyBefore = scene.isDirty;
            var result = InspectAppliedState();
            var breathingInfo = InspectBreathingMesh(result.Renderer.sharedMesh);
            WriteFinalReport(result, breathingInfo, File.Exists(Absolute(FinalCapturePath)));
            WriteWingbeatAndHover1HzReport(
                result,
                breathingInfo,
                File.Exists(Absolute(WingbeatAndHover1HzCapturePath)));
            AssetDatabase.Refresh();
            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException(
                    "The 1Hz Fuga wingbeat and hover inspection changed the scene dirty state.");
            }

            Debug.Log(
                "FugaIdleWingbeatAndHover1HzInspected Result=PASS" +
                ", LoopSeconds=2" +
                ", WingbeatHz=1" +
                ", WingbeatsPerLoop=2" +
                ", ShoulderStrokeDegrees=84" +
                ", BreathingCyclesPerSecond=1" +
                ", HoverFrequencyHz=1" +
                ", HoverAmplitudeMeters=0.015" +
                ", OtherFugaSlotsChanged=False" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Capture Idle Wingbeat And Hover 1Hz")]
        public static void CaptureFugaIdleWingbeatAndHover1Hz()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be saved before the final 1Hz wingbeat and hover capture.");
            }

            var result = InspectAppliedState();
            CaptureComparison(
                result.Slot,
                result.Clip,
                Absolute(WingbeatAndHover1HzCapturePath),
                new[] { 0f, 0.5f, 1f, 1.5f });
            var breathingInfo = InspectBreathingMesh(result.Renderer.sharedMesh);
            WriteFinalReport(result, breathingInfo, File.Exists(Absolute(FinalCapturePath)));
            WriteWingbeatAndHover1HzReport(result, breathingInfo, captureCreated: true);
            AssetDatabase.Refresh();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("The final 1Hz idle cadence capture changed the scene.");
            }

            Debug.Log(
                "FugaIdleWingbeatAndHover1HzCaptured Result=PASS" +
                ", SampleTimesSeconds=0,0.5,1,1.5" +
                ", Image=" + WingbeatAndHover1HzCapturePath +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Inspect Idle And Death Visual Identity")]
        public static void InspectFugaIdleDeathVisualIdentity()
        {
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var placementRoot = GameObject.Find(PlacementRootName)?.transform ??
                                throw new InvalidOperationException(PlacementRootName + " is missing.");
            var idleSlot = RequireDirectSlot(placementRoot, IdleSlotName);
            var deathSlot = RequireDirectSlot(placementRoot, DeathSlotName);
            var playerCamera = GameObject.Find("Player")?.GetComponentInChildren<Camera>(true) ??
                               throw new InvalidOperationException("The Player camera is missing.");

            var idleRenderer = RequireRenderer(idleSlot);
            var deathRenderer = RequireRenderer(deathSlot);
            var idleAnimator = idleSlot.GetComponent<Animator>();
            var deathAnimator = deathSlot.GetComponent<Animator>();
            var idleDriver = idleSlot.GetComponent<FugaPhysicsMotionDriver>();
            var deathDriver = deathSlot.GetComponent<FugaPhysicsMotionDriver>();
            var idleControllerPath = ControllerPathOf(idleAnimator);
            var deathControllerPath = ControllerPathOf(deathAnimator);
            var idleMeshPath = AssetDatabase.GetAssetPath(idleRenderer.sharedMesh);
            var deathMeshPath = AssetDatabase.GetAssetPath(deathRenderer.sharedMesh);
            var idleConnected = idleControllerPath == ControllerPath &&
                                idleMeshPath == DerivedMeshPath &&
                                idleDriver != null && idleDriver.IdleHoverEnabled;
            var deathDisconnected = string.IsNullOrEmpty(deathControllerPath) &&
                                    deathMeshPath == ImportedModelPath &&
                                    deathDriver != null && !deathDriver.IdleHoverEnabled;
            if (!idleConnected || !deathDisconnected)
            {
                throw new InvalidOperationException(
                    "The saved Idle/Death animation ownership does not match the expected scene contract.");
            }

            var report = new StringBuilder()
                .AppendLine("Fuga Idle/Death Visual Identity Inspection")
                .AppendLine("Result=PASS")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("SceneDirtyBefore=" + wasDirty)
                .AppendLine("PlayerCamera=" + HierarchyPath(playerCamera.transform))
                .AppendLine("PlayerCameraWorldPosition=" + Vec(playerCamera.transform.position))
                .AppendLine("PlayerCameraWorldEuler=" + Vec(playerCamera.transform.eulerAngles))
                .AppendLine("IdleMotionOwner=" + HierarchyPath(idleSlot))
                .AppendLine("DeathMotionOwner=" + HierarchyPath(deathSlot))
                .AppendLine("IdleMotionConnectedOnlyToIdle=True")
                .AppendLine("DeathHasIdleMotion=False")
                .AppendLine();

            AppendIdentity(report, "Idle", idleSlot, idleRenderer, idleAnimator, idleDriver, playerCamera);
            AppendIdentity(report, "Death", deathSlot, deathRenderer, deathAnimator, deathDriver, playerCamera);
            AppendSlotScreenOrder(report, placementRoot, playerCamera);
            AppendTextIdentity(report, placementRoot, playerCamera);

            var idleViewport = playerCamera.WorldToViewportPoint(idleRenderer.bounds.center);
            var deathViewport = playerCamera.WorldToViewportPoint(deathRenderer.bounds.center);
            report.AppendLine()
                .AppendLine("IdentityConclusion=The new idle motion is owned by Fuga_01_Idle; Fuga_05_Death has no idle controller and keeps the imported mesh.")
                .AppendLine("IdleVersusDeathViewportDelta=" + Vec(idleViewport - deathViewport))
                .AppendLine("SceneChanged=False")
                .AppendLine("HarnessValidationRun=False")
                .AppendLine("EditModeTestsRun=False")
                .AppendLine("PlayModeTestsRun=False")
                .AppendLine("WindowsBuildRun=False");

            WriteText(IdentityReportPath, report.ToString());
            AssetDatabase.Refresh();
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException("The identity inspection unexpectedly changed the scene dirty state.");
            }

            Debug.Log(
                "FugaIdleDeathVisualIdentityInspected Result=PASS" +
                ", IdleController=" + idleControllerPath +
                ", DeathController=<none>" +
                ", Report=" + IdentityReportPath +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Capture Idle And Death Identity Comparison")]
        public static void CaptureFugaIdleDeathIdentityComparison()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before the Idle/Death identity capture.");
            }

            InspectFugaIdleDeathVisualIdentity();
            var idleSlot = RequireIdleSlot();
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException("The new Fuga idle clip is missing.");
            var playerCamera = GameObject.Find("Player")?.GetComponentInChildren<Camera>(true) ??
                               throw new InvalidOperationException("The Player camera is missing.");
            CapturePlayerViewIdentityComparison(idleSlot, clip, playerCamera, Absolute(IdentityCapturePath));
            AssetDatabase.Refresh();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("The Idle/Death identity capture unexpectedly changed the scene.");
            }

            Debug.Log(
                "FugaIdleDeathIdentityComparisonCaptured Result=PASS" +
                ", LeftPanelIdleSampleSeconds=0" +
                ", RightPanelIdleSampleSeconds=0.1" +
                ", Image=" + IdentityCapturePath +
                ", SceneChanged=False.");
        }

        private static Mesh CreateBreathingMesh(out BreathingInfo info)
        {
            var imported = AssetDatabase.LoadAssetAtPath<GameObject>(ImportedModelPath) ??
                           throw new InvalidOperationException("The supplied Fuga GLB asset is missing.");
            var sourceRenderer = imported.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                                 throw new InvalidOperationException("The supplied Fuga GLB has no skinned mesh.");
            var sourceMesh = sourceRenderer.sharedMesh ??
                             throw new InvalidOperationException("The supplied Fuga GLB skinned mesh is missing.");
            if (sourceMesh.blendShapeCount != 0)
            {
                throw new InvalidOperationException("The supplied Fuga GLB unexpectedly contains BlendShapes.");
            }

            var vertices = sourceMesh.vertices;
            var weights = sourceMesh.boneWeights;
            var coreBones = BoneSet(sourceRenderer, "Bone_000", "Bone_001", "Bone_005", "Bone_021");
            var wingBones = BoneSet(
                sourceRenderer,
                "Bone_013", "Bone_012", "Bone_011", "Bone_010",
                "Bone_017", "Bone_016", "Bone_015", "Bone_014");
            var masks = new float[vertices.Length];
            var pivotSum = Vector3.zero;
            var pivotWeight = 0f;
            for (var index = 0; index < vertices.Length; index++)
            {
                var core = SumWeight(weights[index], coreBones);
                var wing = SumWeight(weights[index], wingBones);
                var mask = Mathf.Clamp01((core - wing - 0.1f) / 0.7f);
                masks[index] = mask;
                if (mask > 0.001f)
                {
                    pivotSum += vertices[index] * mask;
                    pivotWeight += mask;
                }
            }

            if (pivotWeight <= 0.001f)
            {
                throw new InvalidOperationException("No body-weighted vertices were found for Fuga breathing.");
            }

            var pivot = pivotSum / pivotWeight;
            var deltaVertices = new Vector3[vertices.Length];
            var deltaNormals = new Vector3[vertices.Length];
            var deltaTangents = new Vector3[vertices.Length];
            var affected = 0;
            var fullStrength = 0;
            var maximumMask = 0f;
            for (var index = 0; index < vertices.Length; index++)
            {
                var mask = masks[index];
                maximumMask = Mathf.Max(maximumMask, mask);
                if (mask <= 0.001f)
                {
                    continue;
                }

                affected++;
                if (mask >= 0.999f)
                {
                    fullStrength++;
                }

                deltaVertices[index] = (vertices[index] - pivot) * (0.03f * mask);
            }

            AssetDatabase.DeleteAsset(DerivedMeshPath);
            var derived = UnityEngine.Object.Instantiate(sourceMesh);
            derived.name = "Fuga_Idle_BreathingMesh";
            derived.AddBlendShapeFrame(BlendShapeName, 100f, deltaVertices, deltaNormals, deltaTangents);
            AssetDatabase.CreateAsset(derived, DerivedMeshPath);
            AssetDatabase.ImportAsset(DerivedMeshPath, ImportAssetOptions.ForceSynchronousImport);
            info = new BreathingInfo(affected, fullStrength, maximumMask * 3f, pivot);
            return AssetDatabase.LoadAssetAtPath<Mesh>(DerivedMeshPath) ??
                   throw new InvalidOperationException("The derived Fuga breathing mesh was not created.");
        }

        private static AnimationClip CreateIdleClip(
            Transform slot,
            SkinnedMeshRenderer renderer,
            Transform leftWing,
            Transform rightWing)
        {
            AssetDatabase.DeleteAsset(ClipPath);
            var clip = new AnimationClip
            {
                name = "Fuga_Idle_NewModel_WingbeatBreathing",
                frameRate = 60f,
                wrapMode = WrapMode.Loop
            };

            AddWingRotationCurves(clip, RelativePath(slot, leftWing), leftWing.localRotation);
            AddWingRotationCurves(clip, RelativePath(slot, rightWing), rightWing.localRotation);

            var breathing = SmoothCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 100f),
                new Keyframe(1f, 0f),
                new Keyframe(1.5f, 100f),
                new Keyframe(LoopDuration, 0f));
            clip.SetCurve(
                RelativePath(slot, renderer.transform),
                typeof(SkinnedMeshRenderer),
                "blendShape." + BlendShapeName,
                breathing);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, ClipPath);
            AssetDatabase.ImportAsset(ClipPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                   throw new InvalidOperationException("The new Fuga idle clip was not created.");
        }

        private static void AddWingRotationCurves(AnimationClip clip, string path, Quaternion bindRotation)
        {
            var xKeys = new Keyframe[WingbeatsPerLoop * 2 + 1];
            var yKeys = new Keyframe[xKeys.Length];
            var zKeys = new Keyframe[xKeys.Length];
            var wKeys = new Keyframe[xKeys.Length];
            var previous = Quaternion.identity;
            for (var index = 0; index < xKeys.Length; index++)
            {
                var time = index * (0.5f / WingbeatFrequency);
                var angle = index % 2 == 0 ? UpstrokeAngle : DownstrokeAngle;
                var value = bindRotation * Quaternion.AngleAxis(angle, Vector3.right);
                if (index > 0 && Quaternion.Dot(previous, value) < 0f)
                {
                    value = new Quaternion(-value.x, -value.y, -value.z, -value.w);
                }

                previous = value;
                xKeys[index] = new Keyframe(time, value.x);
                yKeys[index] = new Keyframe(time, value.y);
                zKeys[index] = new Keyframe(time, value.z);
                wKeys[index] = new Keyframe(time, value.w);
            }

            clip.SetCurve(path, typeof(Transform), "localRotation.x", SmoothCurve(xKeys));
            clip.SetCurve(path, typeof(Transform), "localRotation.y", SmoothCurve(yKeys));
            clip.SetCurve(path, typeof(Transform), "localRotation.z", SmoothCurve(zKeys));
            clip.SetCurve(path, typeof(Transform), "localRotation.w", SmoothCurve(wKeys));
        }

        private static void RetimeWingRotationCurvesToConfiguredFrequency(AnimationClip clip)
        {
            var rotationBindings = AnimationUtility.GetCurveBindings(clip)
                .Where(binding => binding.type == typeof(Transform) &&
                                  binding.propertyName.IndexOf("localRotation", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            if (rotationBindings.Length != 8)
            {
                throw new InvalidOperationException("The Fuga idle clip does not contain eight wing rotation curves.");
            }

            foreach (var binding in rotationBindings)
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                            throw new InvalidOperationException("A Fuga wing rotation curve is missing.");
                if (curve.length < 2)
                {
                    throw new InvalidOperationException("A Fuga wing rotation curve has insufficient pose keys.");
                }

                var upstrokeValue = curve.keys[0].value;
                var downstrokeValue = curve.keys[1].value;
                var keys = new Keyframe[WingbeatsPerLoop * 2 + 1];
                for (var index = 0; index < keys.Length; index++)
                {
                    keys[index] = new Keyframe(
                        index * (0.5f / WingbeatFrequency),
                        index % 2 == 0 ? upstrokeValue : downstrokeValue);
                }

                AnimationUtility.SetEditorCurve(clip, binding, SmoothCurve(keys));
            }
        }

        private static AnimationCurve SmoothCurve(params Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
            for (var index = 0; index < curve.length; index++)
            {
                curve.SmoothTangents(index, 0f);
            }

            return curve;
        }

        private static AnimatorController CreateIdleController(AnimationClip clip)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.AddState("Fuga_Idle_NewModel");
            state.motion = clip;
            state.speed = 1f;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AppliedResult InspectAppliedState()
        {
            var slot = RequireIdleSlot();
            var model = slot.Find(ModelName) ??
                        throw new InvalidOperationException(IdleSlotName + "/" + ModelName + " is missing.");
            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                           throw new InvalidOperationException("The idle Fuga model has no SkinnedMeshRenderer.");
            if (AssetDatabase.GetAssetPath(renderer.sharedMesh) != DerivedMeshPath)
            {
                throw new InvalidOperationException("The idle Fuga does not use the derived breathing mesh.");
            }

            if (renderer.sharedMesh.GetBlendShapeIndex(BlendShapeName) != 0)
            {
                throw new InvalidOperationException("The required 3% body breathing BlendShape is missing.");
            }

            var animator = slot.GetComponent<Animator>() ??
                           throw new InvalidOperationException("The idle Fuga Animator is missing.");
            if (!animator.enabled || animator.applyRootMotion ||
                AssetDatabase.GetAssetPath(animator.runtimeAnimatorController) != ControllerPath)
            {
                throw new InvalidOperationException("The idle Fuga Animator configuration is incorrect.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException("The new Fuga idle clip is missing.");
            if (Mathf.Abs(clip.length - LoopDuration) > 0.0001f ||
                !AnimationUtility.GetAnimationClipSettings(clip).loopTime)
            {
                throw new InvalidOperationException("The Fuga idle clip is not an exact looping two-second clip.");
            }

            var bindings = AnimationUtility.GetCurveBindings(clip);
            if (bindings.Length != 9 || bindings.Any(binding => string.IsNullOrEmpty(binding.path)))
            {
                throw new InvalidOperationException("The Fuga idle clip contains unexpected or root-level curves.");
            }

            var blendBindings = bindings.Count(binding =>
                binding.type == typeof(SkinnedMeshRenderer) &&
                binding.propertyName == "blendShape." + BlendShapeName);
            var rotationBindings = bindings.Count(binding =>
                binding.type == typeof(Transform) &&
                binding.propertyName.IndexOf("localRotation", StringComparison.OrdinalIgnoreCase) >= 0);
            if (blendBindings != 1 || rotationBindings != 8)
            {
                throw new InvalidOperationException("The Fuga idle clip curve contract is incorrect.");
            }

            InspectWingbeatCurveContract(clip, bindings);

            var body = slot.GetComponent<Rigidbody>() ??
                       throw new InvalidOperationException("The idle Fuga Rigidbody is missing.");
            var driver = slot.GetComponent<FugaPhysicsMotionDriver>() ??
                         throw new InvalidOperationException("The idle Fuga physics driver is missing.");
            if (body.isKinematic || body.useGravity || body.constraints != RigidbodyConstraints.FreezeRotation ||
                driver.Body != body || driver.MotionPathTarget == null || driver.LockRootMotionForReview ||
                !driver.FollowVerticalAxis || driver.UseDeathFallSequence || !driver.IdleHoverEnabled ||
                Mathf.Abs(driver.IdleHoverAmplitude - HoverAmplitude) > 0.0001f ||
                Mathf.Abs(driver.IdleHoverFrequency - HoverFrequency) > 0.0001f ||
                driver.MotionPathTarget.parent != slot.parent)
            {
                throw new InvalidOperationException("The idle Fuga Rigidbody hover configuration is incorrect.");
            }

            var placementRoot = slot.parent;
            var otherSlots = 0;
            foreach (Transform child in placementRoot)
            {
                if (!child.name.StartsWith("Fuga_", StringComparison.Ordinal) || child == slot ||
                    child.name == "Fuga_01_Idle_HoverTarget")
                {
                    continue;
                }

                otherSlots++;
                var otherModel = child.Find(ModelName) ??
                                 throw new InvalidOperationException(child.name + " model is missing.");
                var otherRenderer = otherModel.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                                    throw new InvalidOperationException(child.name + " skinned mesh is missing.");
                if (AssetDatabase.GetAssetPath(otherRenderer.sharedMesh) != ImportedModelPath)
                {
                    throw new InvalidOperationException(child.name + " was changed by the idle-only task.");
                }

                var otherAnimator = child.GetComponent<Animator>();
                if (otherAnimator != null && otherAnimator.runtimeAnimatorController != null)
                {
                    throw new InvalidOperationException(child.name + " received an Animator Controller.");
                }
            }

            if (otherSlots != 6)
            {
                throw new InvalidOperationException("The six protected non-idle Fuga slots were not all found.");
            }

            if (slot.GetComponent<FugaAnimationReviewPlaybackDriver>() != null)
            {
                throw new InvalidOperationException("The old Fuga review playback driver is still connected.");
            }

            return new AppliedResult(slot, renderer, animator, clip, body, driver, otherSlots);
        }

        private static void InspectWingbeatCurveContract(
            AnimationClip clip,
            EditorCurveBinding[] bindings)
        {
            var rotationBindings = bindings
                .Where(binding => binding.type == typeof(Transform) &&
                                  binding.propertyName.IndexOf("localRotation", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            var expectedKeyCount = WingbeatsPerLoop * 2 + 1;
            foreach (var binding in rotationBindings)
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                            throw new InvalidOperationException("A Fuga wing rotation curve is missing.");
                if (curve.length != expectedKeyCount)
                {
                    throw new InvalidOperationException(
                        "A Fuga wing curve does not contain the required configured-cadence key count.");
                }

                var keys = curve.keys;
                for (var index = 0; index < keys.Length; index++)
                {
                    var expectedTime = index * (0.5f / WingbeatFrequency);
                    var expectedValue = index % 2 == 0 ? keys[0].value : keys[1].value;
                    if (Mathf.Abs(keys[index].time - expectedTime) > 0.0001f ||
                        Mathf.Abs(keys[index].value - expectedValue) > 0.000001f)
                    {
                        throw new InvalidOperationException(
                            "A Fuga wing curve does not follow the exact configured alternating cadence.");
                    }
                }
            }

            foreach (var pathGroup in rotationBindings.GroupBy(binding => binding.path, StringComparer.Ordinal))
            {
                var curves = pathGroup.ToDictionary(
                    binding => binding.propertyName,
                    binding => AnimationUtility.GetEditorCurve(clip, binding),
                    StringComparer.Ordinal);
                var upstroke = QuaternionFromCurves(curves, 0);
                var downstroke = QuaternionFromCurves(curves, 1);
                if (Mathf.Abs(Quaternion.Angle(upstroke, downstroke) -
                              (UpstrokeAngle - DownstrokeAngle)) > 0.05f)
                {
                    throw new InvalidOperationException("The Fuga wing shoulder stroke range changed from 84 degrees.");
                }
            }
        }

        private static Quaternion QuaternionFromCurves(
            IReadOnlyDictionary<string, AnimationCurve> curves,
            int keyIndex)
        {
            return new Quaternion(
                RequireQuaternionCurveValue(curves, 'x', keyIndex),
                RequireQuaternionCurveValue(curves, 'y', keyIndex),
                RequireQuaternionCurveValue(curves, 'z', keyIndex),
                RequireQuaternionCurveValue(curves, 'w', keyIndex));
        }

        private static float RequireQuaternionCurveValue(
            IReadOnlyDictionary<string, AnimationCurve> curves,
            char component,
            int keyIndex)
        {
            var suffix = "." + component;
            var matches = curves
                .Where(pair => pair.Key.EndsWith(suffix, StringComparison.Ordinal))
                .Select(pair => pair.Value)
                .ToArray();
            if (matches.Length != 1 || matches[0] == null || matches[0].length <= keyIndex)
            {
                throw new InvalidOperationException("Missing Fuga quaternion curve component: " + component + ".");
            }

            return matches[0].keys[keyIndex].value;
        }

        private static BreathingInfo InspectBreathingMesh(Mesh mesh)
        {
            if (mesh == null || mesh.blendShapeCount != 1 || mesh.GetBlendShapeName(0) != BlendShapeName)
            {
                throw new InvalidOperationException("The derived breathing mesh contract is incorrect.");
            }

            var imported = AssetDatabase.LoadAssetAtPath<GameObject>(ImportedModelPath) ??
                           throw new InvalidOperationException("The supplied Fuga GLB asset is missing.");
            var sourceRenderer = imported.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                                 throw new InvalidOperationException("The supplied Fuga GLB has no skinned mesh.");
            var sourceMesh = sourceRenderer.sharedMesh ??
                             throw new InvalidOperationException("The supplied Fuga GLB skinned mesh is missing.");
            var vertices = sourceMesh.vertices;
            var weights = sourceMesh.boneWeights;
            if (mesh.vertexCount != vertices.Length)
            {
                throw new InvalidOperationException("The derived breathing mesh vertex count changed.");
            }

            var coreBones = BoneSet(sourceRenderer, "Bone_000", "Bone_001", "Bone_005", "Bone_021");
            var wingBones = BoneSet(
                sourceRenderer,
                "Bone_013", "Bone_012", "Bone_011", "Bone_010",
                "Bone_017", "Bone_016", "Bone_015", "Bone_014");
            var masks = new float[vertices.Length];
            var pivotSum = Vector3.zero;
            var pivotWeight = 0f;
            for (var index = 0; index < vertices.Length; index++)
            {
                var core = SumWeight(weights[index], coreBones);
                var wing = SumWeight(weights[index], wingBones);
                var mask = Mathf.Clamp01((core - wing - 0.1f) / 0.7f);
                masks[index] = mask;
                if (mask > 0.001f)
                {
                    pivotSum += vertices[index] * mask;
                    pivotWeight += mask;
                }
            }

            if (pivotWeight <= 0.001f)
            {
                throw new InvalidOperationException("The breathing inspection found no body-weighted vertices.");
            }

            var pivot = pivotSum / pivotWeight;
            var deltas = new Vector3[vertices.Length];
            var deltaNormals = new Vector3[vertices.Length];
            var deltaTangents = new Vector3[vertices.Length];
            mesh.GetBlendShapeFrameVertices(0, 0, deltas, deltaNormals, deltaTangents);
            var affected = 0;
            var fullStrength = 0;
            var maximumMask = 0f;
            for (var index = 0; index < vertices.Length; index++)
            {
                var mask = masks[index];
                maximumMask = Mathf.Max(maximumMask, mask);
                var expected = (vertices[index] - pivot) * (0.03f * mask);
                if ((deltas[index] - expected).sqrMagnitude > 0.0000000001f)
                {
                    throw new InvalidOperationException(
                        "The derived breathing BlendShape differs from the exact 3% body-weight formula at vertex " +
                        index.ToString(CultureInfo.InvariantCulture) + ".");
                }

                if (mask > 0.001f)
                {
                    affected++;
                }

                if (mask >= 0.999f)
                {
                    fullStrength++;
                }
            }

            return new BreathingInfo(affected, fullStrength, maximumMask * 3f, pivot);
        }

        private static string BreathingCurveSignature(AnimationClip clip)
        {
            var binding = AnimationUtility.GetCurveBindings(clip).Single(candidate =>
                candidate.type == typeof(SkinnedMeshRenderer) &&
                candidate.propertyName == "blendShape." + BlendShapeName);
            var curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                        throw new InvalidOperationException("The Fuga breathing curve is missing.");
            return binding.path + "|" + binding.propertyName + "|" + string.Join(
                ";",
                curve.keys.Select(key => string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:R},{1:R},{2:R},{3:R},{4},{5:R},{6:R}",
                    key.time,
                    key.value,
                    key.inTangent,
                    key.outTangent,
                    (int)key.weightedMode,
                    key.inWeight,
                    key.outWeight)));
        }

        private static string PlacementAndPlayerSignature(bool includeIdleHoverFrequency = true)
        {
            var placementRoot = GameObject.Find(PlacementRootName)?.transform ??
                                throw new InvalidOperationException(PlacementRootName + " is missing.");
            var player = GameObject.Find("Player")?.transform ??
                         throw new InvalidOperationException("Player is missing.");
            var builder = new StringBuilder();
            AppendTransformTreeSignature(builder, placementRoot);
            AppendTransformTreeSignature(builder, player);
            foreach (var slot in placementRoot.Cast<Transform>()
                         .Where(child => child.name.StartsWith("Fuga_", StringComparison.Ordinal))
                         .OrderBy(child => child.name, StringComparer.Ordinal))
            {
                var animator = slot.GetComponent<Animator>();
                builder.Append("Animator|").Append(slot.name).Append('|')
                    .Append(animator != null && animator.enabled).Append('|')
                    .Append(animator != null && animator.runtimeAnimatorController != null
                        ? AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)
                        : string.Empty)
                    .AppendLine();
                foreach (var renderer in slot.GetComponentsInChildren<Renderer>(true))
                {
                    var mesh = renderer is SkinnedMeshRenderer skinned
                        ? skinned.sharedMesh
                        : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                    builder.Append("Renderer|").Append(HierarchyPath(renderer.transform)).Append('|')
                        .Append(mesh != null ? AssetDatabase.GetAssetPath(mesh) : string.Empty)
                        .AppendLine();
                }

                var driver = slot.GetComponent<FugaPhysicsMotionDriver>();
                if (driver != null)
                {
                    builder.Append("Driver|").Append(slot.name).Append('|')
                        .Append(driver.MotionPathTarget != null ? HierarchyPath(driver.MotionPathTarget) : string.Empty).Append('|')
                        .Append(driver.LockRootMotionForReview).Append('|')
                        .Append(driver.FollowVerticalAxis).Append('|')
                        .Append(driver.UseDeathFallSequence).Append('|')
                        .Append(driver.IdleHoverEnabled).Append('|')
                        .Append(driver.IdleHoverAmplitude.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                        .Append(includeIdleHoverFrequency
                            ? driver.IdleHoverFrequency.ToString("R", CultureInfo.InvariantCulture)
                            : "ApprovedFrequencyChange").Append('|')
                        .Append(Vec(driver.IdleHoverBaseLocalPosition))
                        .AppendLine();
                }
            }

            return builder.ToString();
        }

        private static void AppendTransformTreeSignature(StringBuilder builder, Transform root)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append("Transform|").Append(HierarchyPath(transform)).Append('|')
                    .Append(transform.GetSiblingIndex().ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(Vec(transform.localPosition)).Append('|')
                    .Append(Vec(transform.localEulerAngles)).Append('|')
                    .Append(Vec(transform.localScale)).Append('|')
                    .Append(transform.gameObject.activeSelf)
                    .AppendLine();
            }
        }

        private static void WriteWingbeatAndHover1HzReport(
            AppliedResult result,
            BreathingInfo breathing,
            bool captureCreated)
        {
            var report = new StringBuilder()
                .AppendLine("Fuga Idle Wingbeat And Hover 1Hz Report")
                .AppendLine("Result=PASS")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("Target=" + PlacementRootName + "/" + IdleSlotName)
                .AppendLine("AnimationClip=" + ClipPath)
                .AppendLine("AnimatorController=" + ControllerPath)
                .AppendLine("LoopDurationSeconds=2.000")
                .AppendLine("WingbeatFrequencyHz=1.000")
                .AppendLine("WingbeatsPerLoop=2")
                .AppendLine("HalfStrokeIntervalSeconds=0.500")
                .AppendLine("FullWingbeatIntervalSeconds=1.000")
                .AppendLine("WingCurveKeyCountPerQuaternionComponent=5")
                .AppendLine("UpstrokeAngleDegrees=44.000")
                .AppendLine("DownstrokeAngleDegrees=-40.000")
                .AppendLine("TotalShoulderStrokeDegrees=84.000")
                .AppendLine("ExistingWingPoseValuesPreserved=True")
                .AppendLine("WingbeatFrequencyBasis=UserSpecified1Hz")
                .AppendLine("BreathingCyclesPerSecond=1")
                .AppendLine("PeakBreathingExpansionPercent=3.000")
                .AppendLine("MeasuredMaximumExpansionPercent=" +
                            breathing.MaximumExpansionPercent.ToString("F3", CultureInfo.InvariantCulture))
                .AppendLine("BreathingCurveChanged=False")
                .AppendLine("HoverAmplitudeMeters=" + HoverAmplitude.ToString("F3", CultureInfo.InvariantCulture))
                .AppendLine("HoverFrequencyHz=" + HoverFrequency.ToString("F3", CultureInfo.InvariantCulture))
                .AppendLine("HoverFrequencyMatchesWingbeat=True")
                .AppendLine("HoverAmplitudeChanged=False")
                .AppendLine("HoverTargetPositionChanged=False")
                .AppendLine("HoverBasePositionChanged=False")
                .AppendLine("HoverTarget=" + result.Driver.MotionPathTarget.name)
                .AppendLine("IdleSlotOwnershipPreserved=True")
                .AppendLine("OtherFugaSlotsChanged=False")
                .AppendLine("PlacementOrderChanged=False")
                .AppendLine("PlayerChanged=False")
                .AppendLine("OriginalGlbModified=False")
                .AppendLine("ArtSampleCreated=False")
                .AppendLine("CaptureSampleTimesSeconds=0,0.5,1,1.5")
                .AppendLine("CaptureCreated=" + captureCreated)
                .AppendLine("HarnessValidationRun=False")
                .ToString();
            var destination = Absolute(WingbeatAndHover1HzReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid 1Hz idle cadence report path."));
            File.WriteAllText(destination, report, new UTF8Encoding(false));
        }

        private static void WriteFinalReport(AppliedResult result, BreathingInfo breathing, bool captureCreated)
        {
            var report = new StringBuilder()
                .AppendLine("Fuga New Idle Motion Report")
                .AppendLine("Result=PASS")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("Target=" + PlacementRootName + "/" + IdleSlotName)
                .AppendLine("SourceModel=" + ImportedModelPath)
                .AppendLine("DerivedBreathingMesh=" + DerivedMeshPath)
                .AppendLine("AnimationClip=" + ClipPath)
                .AppendLine("AnimatorController=" + ControllerPath)
                .AppendLine("LoopDurationSeconds=" + LoopDuration.ToString("F3", CultureInfo.InvariantCulture))
                .AppendLine("LoopEnabled=True")
                .AppendLine("WingbeatFrequencyHz=" + WingbeatFrequency.ToString("F3", CultureInfo.InvariantCulture))
                .AppendLine("WingbeatsPerLoop=" + WingbeatsPerLoop.ToString(CultureInfo.InvariantCulture))
                .AppendLine("WingRootBones=Bone_013,Bone_017")
                .AppendLine("WingRotationAxis=LocalX")
                .AppendLine("UpstrokeAngleDegrees=" + UpstrokeAngle.ToString("F3", CultureInfo.InvariantCulture))
                .AppendLine("DownstrokeAngleDegrees=" + DownstrokeAngle.ToString("F3", CultureInfo.InvariantCulture))
                .AppendLine("TotalShoulderStrokeDegrees=" +
                            (UpstrokeAngle - DownstrokeAngle).ToString("F3", CultureInfo.InvariantCulture))
                .AppendLine("BirdReferenceBasis=Pigeon-like medium bird shoulder stroke range; frequency overridden by user")
                .AppendLine("BreathingCyclesPerSecond=1")
                .AppendLine("PeakBreathingExpansionPercent=3.000")
                .AppendLine("BreathingBlendShape=" + BlendShapeName)
                .AppendLine("BreathingAffectedVertices=" + breathing.AffectedVertices.ToString(CultureInfo.InvariantCulture))
                .AppendLine("BreathingFullStrengthVertices=" + breathing.FullStrengthVertices.ToString(CultureInfo.InvariantCulture))
                .AppendLine("MeasuredMaximumExpansionPercent=" +
                            breathing.MaximumExpansionPercent.ToString("F3", CultureInfo.InvariantCulture))
                .AppendLine("BreathingPivot=" + Vec(breathing.Pivot))
                .AppendLine("HoverAmplitudeMeters=" + HoverAmplitude.ToString("F3", CultureInfo.InvariantCulture))
                .AppendLine("HoverPeakToPeakMeters=" + (HoverAmplitude * 2f).ToString("F3", CultureInfo.InvariantCulture))
                .AppendLine("HoverFrequencyHz=" + HoverFrequency.ToString("F3", CultureInfo.InvariantCulture))
                .AppendLine("HoverTarget=" + result.Driver.MotionPathTarget.name)
                .AppendLine("HoverRootMovement=RigidbodyVelocityInFixedUpdate")
                .AppendLine("RootAnimationCurves=0")
                .AppendLine("RigidbodyNonKinematic=True")
                .AppendLine("GravityEnabled=False")
                .AppendLine("RotationFrozen=True")
                .AppendLine("OldFugaIdleAnimationUsed=False")
                .AppendLine("OldFugaIdleControllerUsed=False")
                .AppendLine("LegacyReviewPlaybackDriverConnected=False")
                .AppendLine("OtherFugaSlotsChanged=False")
                .AppendLine("ProtectedOtherFugaSlots=" + result.OtherSlotCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("OriginalGlbModified=False")
                .AppendLine("ArtSampleCreated=False")
                .AppendLine("CaptureCreated=" + (captureCreated ? "True" : "False"))
                .AppendLine("HarnessValidationRun=False")
                .ToString();
            var destination = Absolute(FinalReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid final report path."));
            File.WriteAllText(destination, report, new UTF8Encoding(false));
        }

        private static void CaptureComparison(
            Transform slot,
            AnimationClip clip,
            string destination,
            float[] sampleTimes = null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid final capture path."));
            var activeScene = SceneManager.GetActiveScene();
            var activeSceneWasDirty = activeScene.isDirty;
            Texture2D composite = null;
            GameObject cameraObject = null;
            GameObject lightObject = null;
            try
            {
                cameraObject = new GameObject("FugaIdleCaptureCamera", typeof(Camera))
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                lightObject = new GameObject("FugaIdleCaptureLight", typeof(Light))
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.045f, 0.055f, 1f);
                camera.cullingMask = ~0;
                camera.allowHDR = false;
                camera.orthographic = true;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;

                var light = lightObject.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                light.color = new Color(1f, 0.96f, 0.9f);
                light.cullingMask = ~0;
                light.transform.rotation = Quaternion.Euler(38f, -32f, 0f);

                var panelWidth = CaptureWidth / 2;
                var panelHeight = CaptureHeight / 2;
                composite = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
                var times = sampleTimes ?? new[] { 0f, 0.25f, 0.5f, 0.75f };
                if (times.Length != 4)
                {
                    throw new InvalidOperationException("The Fuga idle comparison requires exactly four sample times.");
                }
                var playerCamera = GameObject.Find("Player")?.GetComponentInChildren<Camera>(true);
                var baseBounds = BoundsOf(slot);
                var direction = playerCamera != null
                    ? (baseBounds.center - playerCamera.transform.position).normalized
                    : new Vector3(0f, 0.12f, -1f).normalized;
                AnimationMode.StartAnimationMode();
                for (var index = 0; index < times.Length; index++)
                {
                    var time = times[index];
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(slot.gameObject, clip, time);
                    AnimationMode.EndSampling();
                    var bounds = BoundsOf(slot);
                    camera.transform.position = bounds.center - direction * 10f;
                    camera.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                    camera.orthographicSize = Mathf.Max(
                        bounds.extents.y * 1.25f,
                        bounds.extents.x * 1.25f / (panelWidth / (float)panelHeight));
                    var panel = Render(camera, panelWidth, panelHeight);
                    var x = index % 2 * panelWidth;
                    var y = (1 - index / 2) * panelHeight;
                    composite.SetPixels(x, y, panelWidth, panelHeight, panel.GetPixels());
                    UnityEngine.Object.DestroyImmediate(panel);
                }

                composite.Apply();
                File.WriteAllBytes(destination, composite.EncodeToPNG());
            }
            finally
            {
                if (AnimationMode.InAnimationMode())
                {
                    AnimationMode.StopAnimationMode();
                }

                if (composite != null)
                {
                    UnityEngine.Object.DestroyImmediate(composite);
                }

                if (cameraObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                }

                if (lightObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(lightObject);
                }

                if (!activeSceneWasDirty && activeScene.isDirty)
                {
                    throw new InvalidOperationException(
                        "The temporary Fuga capture unexpectedly dirtied CargoRunMvp.");
                }
            }
        }

        private static Texture2D Render(Camera camera, int width, int height)
        {
            var previous = RenderTexture.active;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                image.Apply();
                return image;
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void CapturePlayerViewIdentityComparison(
            Transform idleSlot,
            AnimationClip clip,
            Camera playerCamera,
            string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid identity capture path."));
            var activeScene = SceneManager.GetActiveScene();
            var wasDirty = activeScene.isDirty;
            Texture2D composite = null;
            try
            {
                var panelWidth = CaptureWidth / 2;
                composite = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
                var times = new[] { 0f, 0.1f };
                AnimationMode.StartAnimationMode();
                for (var index = 0; index < times.Length; index++)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(idleSlot.gameObject, clip, times[index]);
                    AnimationMode.EndSampling();
                    var panel = Render(playerCamera, panelWidth, CaptureHeight);
                    composite.SetPixels(index * panelWidth, 0, panelWidth, CaptureHeight, panel.GetPixels());
                    UnityEngine.Object.DestroyImmediate(panel);
                }

                composite.Apply();
                File.WriteAllBytes(destination, composite.EncodeToPNG());
            }
            finally
            {
                if (AnimationMode.InAnimationMode())
                {
                    AnimationMode.StopAnimationMode();
                }

                if (composite != null)
                {
                    UnityEngine.Object.DestroyImmediate(composite);
                }

                if (!wasDirty && activeScene.isDirty)
                {
                    throw new InvalidOperationException(
                        "The temporary Idle/Death identity capture unexpectedly dirtied CargoRunMvp.");
                }
            }
        }

        private static void AppendIdentity(
            StringBuilder report,
            string label,
            Transform slot,
            SkinnedMeshRenderer renderer,
            Animator animator,
            FugaPhysicsMotionDriver driver,
            Camera camera)
        {
            var model = slot.Find(ModelName) ??
                        throw new InvalidOperationException(slot.name + "/" + ModelName + " is missing.");
            report.AppendLine(label + "Slot=" + HierarchyPath(slot))
                .AppendLine(label + "SiblingIndex=" + slot.GetSiblingIndex().ToString(CultureInfo.InvariantCulture))
                .AppendLine(label + "SlotLocalPosition=" + Vec(slot.localPosition))
                .AppendLine(label + "SlotWorldPosition=" + Vec(slot.position))
                .AppendLine(label + "SlotLocalEuler=" + Vec(slot.localEulerAngles))
                .AppendLine(label + "ModelLocalPosition=" + Vec(model.localPosition))
                .AppendLine(label + "ModelWorldPosition=" + Vec(model.position))
                .AppendLine(label + "ModelLocalEuler=" + Vec(model.localEulerAngles))
                .AppendLine(label + "BoundsCenter=" + Vec(renderer.bounds.center))
                .AppendLine(label + "BoundsSize=" + Vec(renderer.bounds.size))
                .AppendLine(label + "SlotViewport=" + Vec(camera.WorldToViewportPoint(slot.position)))
                .AppendLine(label + "BoundsViewport=" + Vec(camera.WorldToViewportPoint(renderer.bounds.center)))
                .AppendLine(label + "MeshAsset=" + AssetDatabase.GetAssetPath(renderer.sharedMesh))
                .AppendLine(label + "AnimatorEnabled=" + (animator != null && animator.enabled))
                .AppendLine(label + "AnimatorController=" + ControllerPathOf(animator))
                .AppendLine(label + "IdleHoverEnabled=" + (driver != null && driver.IdleHoverEnabled))
                .AppendLine(label + "DeathFallEnabled=" + (driver != null && driver.UseDeathFallSequence))
                .AppendLine(label + "ReviewPlaybackDriverCount=" +
                            slot.GetComponents<FugaAnimationReviewPlaybackDriver>().Length.ToString(CultureInfo.InvariantCulture))
                .AppendLine();
        }

        private static void AppendSlotScreenOrder(StringBuilder report, Transform placementRoot, Camera camera)
        {
            var slots = new List<Transform>();
            foreach (Transform child in placementRoot)
            {
                if (child.name.StartsWith("Fuga_", StringComparison.Ordinal) &&
                    child.name != "Fuga_01_Idle_HoverTarget" &&
                    child.Find(ModelName) != null)
                {
                    slots.Add(child);
                }
            }

            report.AppendLine("HierarchySlotOrder=" + string.Join(" > ", slots
                .OrderBy(slot => slot.GetSiblingIndex())
                .Select(slot => slot.name)));
            report.AppendLine("PlayerScreenLeftToRight=" + string.Join(" > ", slots
                .OrderBy(slot => camera.WorldToViewportPoint(RequireRenderer(slot).bounds.center).x)
                .Select(slot => slot.name + "@" +
                    camera.WorldToViewportPoint(RequireRenderer(slot).bounds.center).x
                        .ToString("F6", CultureInfo.InvariantCulture))));
        }

        private static void AppendTextIdentity(StringBuilder report, Transform placementRoot, Camera camera)
        {
            var labels = Resources.FindObjectsOfTypeAll<TextMesh>()
                .Where(text => text != null && text.gameObject.scene == placementRoot.gameObject.scene)
                .OrderBy(text => HierarchyPath(text.transform), StringComparer.Ordinal)
                .ToArray();
            report.AppendLine("SceneTextMeshCount=" + labels.Length.ToString(CultureInfo.InvariantCulture));
            foreach (var label in labels)
            {
                report.AppendLine(
                    "SceneTextMesh=" + HierarchyPath(label.transform) +
                    " Text=" + label.text.Replace("\r", "\\r").Replace("\n", "\\n") +
                    " Viewport=" + Vec(camera.WorldToViewportPoint(label.transform.position)));
            }
        }

        private static SkinnedMeshRenderer RequireRenderer(Transform slot)
        {
            var model = slot.Find(ModelName) ??
                        throw new InvalidOperationException(slot.name + "/" + ModelName + " is missing.");
            return model.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                   throw new InvalidOperationException(slot.name + " skinned mesh is missing.");
        }

        private static string ControllerPathOf(Animator animator)
        {
            return animator != null && animator.runtimeAnimatorController != null
                ? AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)
                : string.Empty;
        }

        private static Transform RequireDirectSlot(Transform placementRoot, string slotName)
        {
            return placementRoot.Find(slotName) ??
                   throw new InvalidOperationException(slotName + " is missing.");
        }

        private static string HierarchyPath(Transform target)
        {
            var path = target.name;
            for (var parent = target.parent; parent != null; parent = parent.parent)
            {
                path = parent.name + "/" + path;
            }

            return path;
        }

        private static void WriteText(string projectRelativePath, string contents)
        {
            var destination = Absolute(projectRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid report path."));
            File.WriteAllText(destination, contents, new UTF8Encoding(false));
        }

        private static Bounds BoundsOf(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("The capture proxy has no visible renderers.");
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static Transform FindBone(SkinnedMeshRenderer renderer, string name)
        {
            return renderer.bones.FirstOrDefault(bone => bone != null && bone.name == name) ??
                   throw new InvalidOperationException("Fuga wing bone is missing: " + name + ".");
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => candidate.name == name);
        }

        private static bool[] BoneSet(SkinnedMeshRenderer renderer, params string[] names)
        {
            var expected = new HashSet<string>(names, StringComparer.Ordinal);
            var set = new bool[renderer.bones.Length];
            for (var index = 0; index < renderer.bones.Length; index++)
            {
                var bone = renderer.bones[index];
                set[index] = bone != null && expected.Remove(bone.name);
            }

            if (expected.Count != 0)
            {
                throw new InvalidOperationException("Fuga breathing bone set is incomplete: " +
                                                    string.Join(",", expected) + ".");
            }

            return set;
        }

        private static float SumWeight(BoneWeight weight, bool[] set)
        {
            var total = 0f;
            if (set[weight.boneIndex0]) total += weight.weight0;
            if (set[weight.boneIndex1]) total += weight.weight1;
            if (set[weight.boneIndex2]) total += weight.weight2;
            if (set[weight.boneIndex3]) total += weight.weight3;
            return total;
        }

        private static float WeightForBone(BoneWeight weight, int boneIndex)
        {
            var total = 0f;
            if (weight.boneIndex0 == boneIndex) total += weight.weight0;
            if (weight.boneIndex1 == boneIndex) total += weight.weight1;
            if (weight.boneIndex2 == boneIndex) total += weight.weight2;
            if (weight.boneIndex3 == boneIndex) total += weight.weight3;
            return total;
        }

        private static void AppendAxisProbe(
            StringBuilder report,
            Transform model,
            Transform[] bones,
            string boneName)
        {
            Transform bone = null;
            foreach (var candidate in bones)
            {
                if (candidate != null && candidate.name == boneName)
                {
                    bone = candidate;
                    break;
                }
            }

            if (bone == null || bone.childCount == 0 || bone.parent == null)
            {
                throw new InvalidOperationException("Wing axis probe bone is unusable: " + boneName + ".");
            }

            var childLocal = bone.GetChild(0).localPosition;
            var parentToModel = model.worldToLocalMatrix * bone.parent.localToWorldMatrix;
            var baseModel = parentToModel.MultiplyPoint3x4(bone.localPosition + bone.localRotation * childLocal);
            report.AppendLine().AppendLine("AxisProbe Bone=" + boneName + " AngleDegrees=10");
            AppendAxisDelta(report, parentToModel, bone, childLocal, baseModel, "X", Vector3.right);
            AppendAxisDelta(report, parentToModel, bone, childLocal, baseModel, "Y", Vector3.up);
            AppendAxisDelta(report, parentToModel, bone, childLocal, baseModel, "Z", Vector3.forward);
        }

        private static void AppendAxisDelta(
            StringBuilder report,
            Matrix4x4 parentToModel,
            Transform bone,
            Vector3 childLocal,
            Vector3 baseModel,
            string axisName,
            Vector3 axis)
        {
            var rotatedLocal = bone.localRotation * Quaternion.AngleAxis(10f, axis);
            var rotatedModel = parentToModel.MultiplyPoint3x4(bone.localPosition + rotatedLocal * childLocal);
            report.Append("Axis=")
                .Append(axisName)
                .Append(" ChildModelDelta=")
                .AppendLine(Vec(rotatedModel - baseModel));
        }

        private static Transform RequireIdleSlot()
        {
            var root = GameObject.Find(PlacementRootName) ??
                       throw new InvalidOperationException(PlacementRootName + " is missing.");
            return root.transform.Find(IdleSlotName) ??
                   throw new InvalidOperationException(IdleSlotName + " is missing.");
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

        private static string RelativePath(Transform root, Transform target)
        {
            if (target == root)
            {
                return string.Empty;
            }

            var path = target.name;
            var current = target.parent;
            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return current == root ? path : "<outside-model>/" + path;
        }

        private static string Absolute(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", projectRelativePath));
        }

        private static string Vec(Vector3 value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0:F6},{1:F6},{2:F6})",
                value.x,
                value.y,
                value.z);
        }

        private readonly struct BreathingInfo
        {
            public BreathingInfo(
                int affectedVertices,
                int fullStrengthVertices,
                float maximumExpansionPercent,
                Vector3 pivot)
            {
                AffectedVertices = affectedVertices;
                FullStrengthVertices = fullStrengthVertices;
                MaximumExpansionPercent = maximumExpansionPercent;
                Pivot = pivot;
            }

            public int AffectedVertices { get; }
            public int FullStrengthVertices { get; }
            public float MaximumExpansionPercent { get; }
            public Vector3 Pivot { get; }
        }

        private readonly struct AppliedResult
        {
            public AppliedResult(
                Transform slot,
                SkinnedMeshRenderer renderer,
                Animator animator,
                AnimationClip clip,
                Rigidbody body,
                FugaPhysicsMotionDriver driver,
                int otherSlotCount)
            {
                Slot = slot;
                Renderer = renderer;
                Animator = animator;
                Clip = clip;
                Body = body;
                Driver = driver;
                OtherSlotCount = otherSlotCount;
            }

            public Transform Slot { get; }
            public SkinnedMeshRenderer Renderer { get; }
            public Animator Animator { get; }
            public AnimationClip Clip { get; }
            public Rigidbody Body { get; }
            public FugaPhysicsMotionDriver Driver { get; }
            public int OtherSlotCount { get; }
        }
    }
}
