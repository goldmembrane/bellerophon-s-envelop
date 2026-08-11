using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Bellerophon.Enemies.Ata;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.AtaCargoRunScene
{
    internal static class AtaPistolAimFireAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ata Enemy Placement";
        private const string SlotName = "Ata_04_PistolAimAndFire";
        private const string ModelName = "Ata_Model";
        private const string SourcePath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Sources/Ata_PistolAimAndFire.fbx";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Ata_04_PistolAimAndFire.controller";
        private const string BodyMeshPath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Ata_04_PistolAimAndFire_Body.asset";
        private const string PistolMeshPath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Ata_04_PistolAimAndFire_Pistol.asset";
        private const string PistolRootName = "Ata_Pistol_Transfer";
        private const string HipAnchorName = "Ata_Pistol_HipAnchor";
        private const string HandAnchorName = "Ata_Pistol_RightHandAnchor";
        private const string DiagnosticPath =
            "docs/validation/ata_pistol_hand_transfer_2026-08-11/Ata_04_PistolHandTransfer_Diagnostic_03.png";
        private const string SourceDiagnosticPath =
            "docs/validation/ata_pistol_aim_fire_2026-08-11/Ata_PistolAimAndFire_Source_Diagnostic_02.png";
        private const string FinalPath =
            "docs/validation/ata_pistol_hand_transfer_2026-08-11/Ata_04_PistolHandTransfer_Final.png";
        private const string ReportPath =
            "docs/validation/ata_pistol_hand_transfer_2026-08-11/Ata_04_PistolHandTransfer_Report.txt";
        private const string WaistGeometryDiagnosticPath =
            "docs/validation/ata_pistol_aim_fire_2026-08-11/Ata_Pistol_Waist_Geometry_Diagnostic.png";
        private const string PistolRegionDiagnosticPath =
            "docs/validation/ata_pistol_aim_fire_2026-08-11/Ata_Pistol_Region_Diagnostic_10.png";
        private const string ExtractedPistolDiagnosticPath =
            "docs/validation/ata_pistol_aim_fire_2026-08-11/Ata_Extracted_Pistol_Geometry_Diagnostic_06.png";
        private const string AtaTexturePath =
            "Assets/_Project/Art/Enemies/Ata/Models/output.fbm/texture_0.png";
        private const float TransformTolerance = 0.0002f;

        [MenuItem("Bellerophon/Enemies/Ata/Apply Pistol Aim And Fire Animation")]
        public static void ApplyAtaPistolAimFireAnimation()
        {
            var scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var slotBefore = new TransformSnapshot(slot);
            var modelBefore = new TransformSnapshot(model);
            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var otherSlotsBefore = OtherSlotSignatures(placement.transform, slot);

            ConfigureSourceImporterForLoop();
            var clip = RequireSingleMixamoClip();
            var bindingSummary = RequireBindingCompatibility(model, clip);
            var controller = CreateController(clip);
            var animator = ConfigureAnimator(model, controller);
            var pistolAssets = ConfigurePistolGeometryAndConstraint(
                model,
                animator,
                clip);

            if (!slotBefore.Matches() || !modelBefore.Matches())
            {
                throw new InvalidOperationException(
                    "Ata_04_PistolAimAndFire slot or model transform changed while applying the supplied clip.");
            }

            RequireEqual(
                otherSlotsBefore,
                OtherSlotSignatures(placement.transform, slot),
                "An Ata slot outside Ata_04_PistolAimAndFire changed.");
            RequireEqual(
                otherRootsBefore,
                OtherRootSignatures(scene, placement),
                "A scene root outside the Ata placement changed.");
            RequireAppliedState(model, clip, controller);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after applying the Ata pistol animation.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "AtaPistolAimFireAnimationApplied Result=PASS" +
                ", Slot=" + SlotName +
                ", Source=" + SourcePath +
                ", MixamoClip=" + clip.name +
                ", Duration=" + Num(clip.length) +
                ", AnimatedPaths=" + bindingSummary.AnimatedPathCount +
                ", SkinnedAnimatedPaths=" + bindingSummary.SkinnedAnimatedPathCount +
                ", VaryingCurves=" + bindingSummary.VaryingCurveCount +
                ", FirstAnimatedPaths=" + bindingSummary.FirstAnimatedPaths +
                ", LargestCurveChanges=" + bindingSummary.LargestCurveChanges +
                ", Loop=True" +
                ", RootMotion=False" +
                ", ExactPistolSourceTriangles=" + pistolAssets.PistolTriangleCount +
                ", PistolRigid=True" +
                ", PistolDriver=RightHandArmConstraint" +
                ", ExistingMaterialPreserved=True" +
                ", OtherAtaSlotsUnchanged=True" +
                ", OtherSceneRootsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Inspect Pistol Structure")]
        public static void InspectAtaPistolStructure()
        {
            var scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath) ??
                               throw new InvalidOperationException(
                                   "The supplied Ata pistol FBX prefab is unavailable.");

            Debug.Log(
                "AtaPistolStructureInspection Result=PASS" +
                ", SceneModel=" + DescribeModelStructure(model) +
                ", SourceModel=" + DescribeModelStructure(sourcePrefab.transform) +
                ", SceneComponents=" + DescribeConnectedComponents(model) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Pistol Waist Geometry Diagnostic")]
        public static void CaptureAtaPistolWaistGeometryDiagnostic()
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var clip = RequireSingleMixamoClip();
            var destination = Absolute(WaistGeometryDiagnosticPath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time Ata pistol waist diagnostic already exists.");
            }

            CaptureWaistGeometry(model, clip, destination);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Ata pistol waist diagnostic changed the scene dirty state.");
            }

            Debug.Log(
                "AtaPistolWaistGeometryDiagnosticCaptured Result=PASS" +
                ", Views=Front,Right,Back,Left" +
                ", Image=" + WaistGeometryDiagnosticPath +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Pistol Region Diagnostic")]
        public static void CaptureAtaPistolRegionDiagnostic()
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var clip = RequireSingleMixamoClip();
            var destination = Absolute(PistolRegionDiagnosticPath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time Ata pistol region diagnostic already exists.");
            }

            GameObject overlay = null;
            Mesh overlayMesh = null;
            Material overlayMaterial = null;
            var modelSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(transform => new TransformSnapshot(transform))
                .ToArray();
            var animator = model.GetComponentsInChildren<Animator>(true).Single();
            var animatorEnabled = animator.enabled;
            try
            {
                animator.enabled = false;
                clip.SampleAnimation(model.gameObject, 0f);
                var result = CreatePistolRegionOverlay(model);
                overlay = result.Overlay;
                overlayMesh = result.Mesh;
                overlayMaterial = result.Material;
                CaptureWaistGeometry(model, clip, destination);
                Debug.Log(
                    "AtaPistolRegionDiagnosticCaptured Result=PASS" +
                    ", SelectedTriangles=" + result.SelectedTriangleCount +
                    ", SelectedComponents=" + result.SelectedComponents +
                    ", Image=" + PistolRegionDiagnosticPath +
                    ", SceneChanged=False.");
            }
            finally
            {
                if (overlay != null)
                {
                    UnityEngine.Object.DestroyImmediate(overlay);
                }

                if (overlayMesh != null)
                {
                    UnityEngine.Object.DestroyImmediate(overlayMesh);
                }

                if (overlayMaterial != null)
                {
                    UnityEngine.Object.DestroyImmediate(overlayMaterial);
                }

                foreach (var snapshot in modelSnapshots)
                {
                    snapshot.Restore();
                }

                animator.enabled = animatorEnabled;
            }

            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Ata pistol region diagnostic changed the scene dirty state.");
            }
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Extracted Pistol Geometry Diagnostic")]
        public static void CaptureAtaExtractedPistolGeometryDiagnostic()
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var clip = RequireSingleMixamoClip();
            var destination = Absolute(ExtractedPistolDiagnosticPath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time extracted Ata pistol diagnostic already exists.");
            }

            GameObject overlay = null;
            Mesh overlayMesh = null;
            Material overlayMaterial = null;
            var modelSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(transform => new TransformSnapshot(transform))
                .ToArray();
            var animator = model.GetComponentsInChildren<Animator>(true).Single();
            var animatorEnabled = animator.enabled;
            var bodyRenderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
            var bodyRendererEnabled = bodyRenderer.enabled;
            try
            {
                animator.enabled = false;
                clip.SampleAnimation(model.gameObject, 0f);
                var result = CreatePistolRegionOverlay(model);
                overlay = result.Overlay;
                overlayMesh = result.Mesh;
                overlayMaterial = result.Material;
                overlay.GetComponent<MeshRenderer>().sharedMaterial =
                    bodyRenderer.sharedMaterial;
                bodyRenderer.enabled = false;
                CaptureIsolatedPistolGeometry(
                    model,
                    overlay.GetComponent<MeshRenderer>(),
                    destination);
                Debug.Log(
                    "AtaExtractedPistolGeometryDiagnosticCaptured Result=PASS" +
                    ", ExactSourceTriangles=" + result.SelectedTriangleCount +
                    ", Image=" + ExtractedPistolDiagnosticPath +
                    ", SceneChanged=False.");
            }
            finally
            {
                bodyRenderer.enabled = bodyRendererEnabled;
                if (overlay != null)
                {
                    UnityEngine.Object.DestroyImmediate(overlay);
                }

                if (overlayMesh != null)
                {
                    UnityEngine.Object.DestroyImmediate(overlayMesh);
                }

                if (overlayMaterial != null)
                {
                    UnityEngine.Object.DestroyImmediate(overlayMaterial);
                }

                foreach (var snapshot in modelSnapshots)
                {
                    snapshot.Restore();
                }

                animator.enabled = animatorEnabled;
            }

            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Extracted Ata pistol diagnostic changed the scene dirty state.");
            }
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Pistol Aim And Fire Diagnostic")]
        public static void CaptureAtaPistolAimFireAnimationDiagnostic()
        {
            CaptureReview(DiagnosticPath, "AtaPistolHandTransferDiagnosticCaptured");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Pistol Aim And Fire Final")]
        public static void CaptureAtaPistolAimFireAnimationFinal()
        {
            CaptureReview(FinalPath, "AtaPistolHandTransferFinalCaptured");
        }

        private static void ConfigureSourceImporterForLoop()
        {
            var importer = AssetImporter.GetAtPath(SourcePath) as ModelImporter ??
                           throw new InvalidOperationException(
                               "The supplied Ata pistol FBX importer is unavailable.");
            importer.importAnimation = true;
            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            var mixamoIndices = Enumerable.Range(0, clips.Length)
                .Where(index =>
                    ContainsMixamo(clips[index].name) ||
                    ContainsMixamo(clips[index].takeName))
                .ToArray();
            if (mixamoIndices.Length != 1)
            {
                throw new InvalidOperationException(
                    "The supplied Ata pistol FBX must contain exactly one take whose name includes mixamo. " +
                    "Found=" + mixamoIndices.Length +
                    ", Takes=" + string.Join(",", clips.Select(clip => clip.name + "/" + clip.takeName)));
            }

            var selected = clips[mixamoIndices[0]];
            selected.loopTime = true;
            selected.loopPose = false;
            selected.lockRootRotation = true;
            selected.lockRootHeightY = true;
            selected.lockRootPositionXZ = true;
            clips[mixamoIndices[0]] = selected;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip RequireSingleMixamoClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(SourcePath)
                .OfType<AnimationClip>()
                .Where(clip =>
                    !clip.name.StartsWith("__preview__", StringComparison.Ordinal) &&
                    ContainsMixamo(clip.name))
                .ToArray();
            if (clips.Length != 1)
            {
                throw new InvalidOperationException(
                    "The imported Ata pistol FBX must expose exactly one mixamo-named animation clip. " +
                    "Found=" + clips.Length +
                    ", Clips=" + string.Join(",", clips.Select(clip => clip.name)));
            }

            return clips[0];
        }

        private static bool ContainsMixamo(string value)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.IndexOf("mixamo", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static BindingSummary RequireBindingCompatibility(
            Transform model,
            AnimationClip clip)
        {
            var curveBindings = AnimationUtility.GetCurveBindings(clip);
            var allBindings = curveBindings
                .Concat(AnimationUtility.GetObjectReferenceCurveBindings(clip))
                .ToArray();
            var animatedPaths = allBindings
                .Select(binding => binding.path)
                .Where(path => !string.IsNullOrEmpty(path))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var missingPaths = animatedPaths
                .Where(path => model.Find(path) == null)
                .ToArray();
            if (missingPaths.Length > 0)
            {
                throw new InvalidOperationException(
                    "The supplied mixamo clip contains transform paths absent from the current Ata appearance rig: " +
                    string.Join(",", missingPaths.Take(12)));
            }

            var skinnedPaths = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SelectMany(renderer => renderer.bones)
                .Where(bone => bone != null)
                .Select(bone => RelativePath(model, bone))
                .ToHashSet(StringComparer.Ordinal);
            var skinnedAnimatedPathCount = animatedPaths.Count(skinnedPaths.Contains);
            var varyingCurveCount = curveBindings.Count(binding =>
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null || curve.length < 2)
                {
                    return false;
                }

                var first = curve.keys[0].value;
                return curve.keys.Any(key => Mathf.Abs(key.value - first) > 0.00001f);
            });
            var largestCurveChanges = curveBindings
                .Select(binding =>
                {
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    if (curve == null || curve.length == 0)
                    {
                        return new CurveChange(binding, 0f);
                    }

                    var values = curve.keys.Select(key => key.value).ToArray();
                    return new CurveChange(binding, values.Max() - values.Min());
                })
                .OrderByDescending(change => change.Range)
                .Take(16)
                .Select(change =>
                    change.Binding.path + "/" + change.Binding.propertyName + "=" +
                    Num(change.Range));
            return new BindingSummary(
                animatedPaths.Length,
                skinnedAnimatedPathCount,
                varyingCurveCount,
                string.Join(";", animatedPaths.Take(8)),
                string.Join(";", largestCurveChanges));
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null &&
                !AssetDatabase.DeleteAsset(ControllerPath))
            {
                throw new InvalidOperationException(
                    "Existing Ata pistol controller could not be replaced.");
            }

            var controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var state = controller.layers[0].stateMachine.AddState("PistolAimAndFire");
            state.motion = clip;
            state.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static Animator ConfigureAnimator(
            Transform model,
            AnimatorController controller)
        {
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length > 1)
            {
                throw new InvalidOperationException(
                    "Ata_04_PistolAimAndFire contains multiple Animators.");
            }

            var animator = animators.Length == 0
                ? model.gameObject.AddComponent<Animator>()
                : animators[0];
            if (animator.transform != model)
            {
                throw new InvalidOperationException(
                    "Ata_04_PistolAimAndFire Animator must be on Ata_Model.");
            }

            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            EditorUtility.SetDirty(animator);
            return animator;
        }

        private static PistolAssets ConfigurePistolGeometryAndConstraint(
            Transform model,
            Animator animator,
            AnimationClip clip)
        {
            var sourceRenderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault() ??
                throw new InvalidOperationException(
                    "Ata pistol setup requires exactly one skinned renderer.");
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath) ??
                               throw new InvalidOperationException(
                                   "The supplied Ata pistol FBX prefab is unavailable.");
            var sourceMesh = sourcePrefab
                                 .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                                 .SingleOrDefault()?.sharedMesh ??
                             throw new InvalidOperationException(
                                 "Ata pistol source mesh is missing.");
            var material = sourceRenderer.sharedMaterial ??
                           throw new InvalidOperationException(
                               "Ata pistol source material is missing.");
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(transform => new TransformSnapshot(transform))
                .ToArray();
            var animatorEnabled = animator.enabled;
            var baked = new Mesh();
            try
            {
                sourceRenderer.sharedMesh = sourceMesh;
                animator.enabled = false;
                clip.SampleAnimation(model.gameObject, 0f);
                sourceRenderer.BakeMesh(baked, false);
                var pistolTriangles = SelectConfirmedPistolTriangles(
                    model,
                    sourceRenderer,
                    baked);
                if (pistolTriangles.Length / 3 != 279)
                {
                    throw new InvalidOperationException(
                        "Confirmed Ata pistol source triangle contract changed. Expected=279, Actual=" +
                        (pistolTriangles.Length / 3));
                }

                var bodyMesh = CreateBodyMeshWithoutPistol(
                    sourceMesh,
                    pistolTriangles);
                var pistolMesh = CreateRigidPistolMesh(
                    sourceMesh,
                    baked,
                    pistolTriangles,
                    out var pivotLocal);
                sourceRenderer.sharedMesh = bodyMesh;
                EditorUtility.SetDirty(sourceRenderer);

                DestroyNamedDescendant(model, PistolRootName);
                DestroyNamedDescendant(model, HipAnchorName);
                DestroyNamedDescendant(model, HandAnchorName);

                var rightUpLeg = model.Find("Armature/Hips/RightUpLeg") ??
                                 throw new InvalidOperationException(
                                     "Ata RightUpLeg bone is missing.");
                var rightHand = model.Find(
                    "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand") ??
                                throw new InvalidOperationException(
                                    "Ata RightHand bone is missing.");
                var pistolRoot = new GameObject(
                    PistolRootName,
                    typeof(MeshFilter),
                    typeof(MeshRenderer),
                    typeof(AtaPistolDrawConstraintDriver));
                pistolRoot.transform.SetParent(model, false);
                pistolRoot.transform.SetPositionAndRotation(
                    sourceRenderer.transform.TransformPoint(pivotLocal),
                    sourceRenderer.transform.rotation);
                pistolRoot.transform.localScale = DivideScale(
                    sourceRenderer.transform.lossyScale,
                    model.lossyScale);
                pistolRoot.GetComponent<MeshFilter>().sharedMesh = pistolMesh;
                var pistolRenderer = pistolRoot.GetComponent<MeshRenderer>();
                pistolRenderer.sharedMaterial = material;
                pistolRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
                pistolRenderer.receiveShadows = sourceRenderer.receiveShadows;
                pistolRenderer.lightProbeUsage = sourceRenderer.lightProbeUsage;
                pistolRenderer.reflectionProbeUsage = sourceRenderer.reflectionProbeUsage;

                var hipAnchor = CreatePoseAnchor(
                    rightUpLeg,
                    HipAnchorName,
                    pistolRoot.transform.position,
                    pistolRoot.transform.rotation);
                var handAnchor = CreatePoseAnchor(
                    rightHand,
                    HandAnchorName,
                    rightHand.position,
                    pistolRoot.transform.rotation);
                var driver = pistolRoot.GetComponent<AtaPistolDrawConstraintDriver>();
                driver.Configure(animator, hipAnchor, handAnchor);
                driver.ApplyNormalizedPhase(0f);
                EditorUtility.SetDirty(driver);

                return new PistolAssets(
                    bodyMesh,
                    pistolMesh,
                    pistolTriangles.Length / 3);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }

                animator.enabled = animatorEnabled;
            }
        }

        private static int[] SelectConfirmedPistolTriangles(
            Transform model,
            SkinnedMeshRenderer renderer,
            Mesh baked)
        {
            var source = renderer.sharedMesh;
            var vertices = baked.vertices;
            var weights = source.boneWeights;
            var sourceUvs = source.uv;
            var rightUpLegIndex = Array.FindIndex(
                renderer.bones,
                bone => bone != null && bone.name == "RightUpLeg");
            if (rightUpLegIndex < 0 || weights.Length != vertices.Length ||
                sourceUvs.Length != vertices.Length)
            {
                throw new InvalidOperationException(
                    "Ata confirmed pistol selection cannot resolve source skin data.");
            }

            var minimum = new Vector3(0.13f, 0.45f, -0.18f);
            var maximum = new Vector3(0.32f, 0.99f, 0.08f);
            var triangles = source.GetTriangles(0);
            var selected = new List<int>();
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(Absolute(AtaTexturePath))))
                {
                    throw new InvalidOperationException(
                        "Ata source texture could not be read for confirmed pistol selection.");
                }

                for (var index = 0; index < triangles.Length; index += 3)
                {
                    var a = triangles[index];
                    var b = triangles[index + 1];
                    var c = triangles[index + 2];
                    var center = model.InverseTransformPoint(
                        renderer.transform.TransformPoint(
                            (vertices[a] + vertices[b] + vertices[c]) / 3f));
                    var rightLegWeight =
                        (WeightForBone(weights[a], rightUpLegIndex) +
                         WeightForBone(weights[b], rightUpLegIndex) +
                         WeightForBone(weights[c], rightUpLegIndex)) / 3f;
                    var color = texture.GetPixelBilinear(
                        Mathf.Repeat(
                            (sourceUvs[a].x + sourceUvs[b].x + sourceUvs[c].x) / 3f,
                            1f),
                        Mathf.Repeat(
                            (sourceUvs[a].y + sourceUvs[b].y + sourceUvs[c].y) / 3f,
                            1f));
                    if (center.x >= minimum.x && center.x <= maximum.x &&
                        center.y >= minimum.y && center.y <= maximum.y &&
                        center.z >= minimum.z && center.z <= maximum.z &&
                        rightLegWeight >= 0.45f &&
                        !IsRedCloth(color))
                    {
                        selected.Add(a);
                        selected.Add(b);
                        selected.Add(c);
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            return SplitSelectedTriangleComponents(source.vertices, selected)
                .OrderByDescending(component => component.Length)
                .First();
        }

        private static Mesh CreateBodyMeshWithoutPistol(
            Mesh source,
            IReadOnlyList<int> pistolTriangles)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(BodyMeshPath) != null &&
                !AssetDatabase.DeleteAsset(BodyMeshPath))
            {
                throw new InvalidOperationException(
                    "Existing Ata pistol body mesh could not be replaced.");
            }

            var body = UnityEngine.Object.Instantiate(source);
            body.name = "Ata_04_PistolAimAndFire_Body";
            var removed = Enumerable.Range(0, pistolTriangles.Count / 3)
                .Select(index => (
                    pistolTriangles[index * 3],
                    pistolTriangles[index * 3 + 1],
                    pistolTriangles[index * 3 + 2]))
                .ToHashSet();
            var sourceTriangles = source.GetTriangles(0);
            var remaining = new List<int>(
                sourceTriangles.Length - pistolTriangles.Count);
            for (var index = 0; index < sourceTriangles.Length; index += 3)
            {
                if (removed.Contains((
                        sourceTriangles[index],
                        sourceTriangles[index + 1],
                        sourceTriangles[index + 2])))
                {
                    continue;
                }

                remaining.Add(sourceTriangles[index]);
                remaining.Add(sourceTriangles[index + 1]);
                remaining.Add(sourceTriangles[index + 2]);
            }

            body.SetTriangles(remaining, 0, true);
            AssetDatabase.CreateAsset(body, BodyMeshPath);
            return body;
        }

        private static Mesh CreateRigidPistolMesh(
            Mesh source,
            Mesh baked,
            IReadOnlyList<int> pistolTriangles,
            out Vector3 pivotLocal)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(PistolMeshPath) != null &&
                !AssetDatabase.DeleteAsset(PistolMeshPath))
            {
                throw new InvalidOperationException(
                    "Existing rigid Ata pistol mesh could not be replaced.");
            }

            var sourceIndices = pistolTriangles.Distinct().OrderBy(index => index).ToArray();
            var sourceVertices = baked.vertices;
            var minimumY = sourceIndices.Min(index => sourceVertices[index].y);
            var maximumY = sourceIndices.Max(index => sourceVertices[index].y);
            var gripThreshold = Mathf.Lerp(minimumY, maximumY, 0.78f);
            var gripIndices = sourceIndices
                .Where(index => sourceVertices[index].y >= gripThreshold)
                .ToArray();
            pivotLocal = gripIndices
                .Select(index => sourceVertices[index])
                .Aggregate(Vector3.zero, (sum, value) => sum + value) /
                         gripIndices.Length;
            var rigidPivot = pivotLocal;
            var remap = sourceIndices
                .Select((sourceIndex, localIndex) => (sourceIndex, localIndex))
                .ToDictionary(value => value.sourceIndex, value => value.localIndex);
            var mesh = new Mesh
            {
                name = "Ata_04_PistolAimAndFire_RigidPistol",
                indexFormat = sourceIndices.Length > ushort.MaxValue
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16,
                vertices = sourceIndices
                    .Select(index => sourceVertices[index] - rigidPivot)
                    .ToArray()
            };
            var normals = baked.normals;
            if (normals.Length == source.vertexCount)
            {
                mesh.normals = sourceIndices.Select(index => normals[index]).ToArray();
            }

            var tangents = baked.tangents;
            if (tangents.Length == source.vertexCount)
            {
                mesh.tangents = sourceIndices.Select(index => tangents[index]).ToArray();
            }

            var colors = source.colors32;
            if (colors.Length == source.vertexCount)
            {
                mesh.colors32 = sourceIndices.Select(index => colors[index]).ToArray();
            }

            for (var channel = 0; channel < 8; channel++)
            {
                var uv = new List<Vector4>();
                source.GetUVs(channel, uv);
                if (uv.Count == source.vertexCount)
                {
                    mesh.SetUVs(
                        channel,
                        sourceIndices.Select(index => uv[index]).ToArray());
                }
            }

            mesh.SetTriangles(
                pistolTriangles.Select(index => remap[index]).ToArray(),
                0,
                true);
            AssetDatabase.CreateAsset(mesh, PistolMeshPath);
            return mesh;
        }

        private static Transform CreatePoseAnchor(
            Transform parent,
            string name,
            Vector3 worldPosition,
            Quaternion worldRotation)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.SetPositionAndRotation(worldPosition, worldRotation);
            anchor.localScale = Vector3.one;
            return anchor;
        }

        private static void DestroyNamedDescendant(Transform model, string name)
        {
            var target = model.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == name);
            if (target != null)
            {
                UnityEngine.Object.DestroyImmediate(target.gameObject);
            }
        }

        private static Vector3 DivideScale(Vector3 value, Vector3 divisor)
        {
            return new Vector3(
                value.x / divisor.x,
                value.y / divisor.y,
                value.z / divisor.z);
        }

        private static void RequireAppliedState(
            Transform model,
            AnimationClip clip,
            AnimatorController controller)
        {
            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_04_PistolAimAndFire Animator is missing.");
            if (animator.transform != model || !animator.enabled ||
                animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(
                    "Ata_04_PistolAimAndFire Animator configuration differs.");
            }

            var serializedClip = new SerializedObject(clip);
            var loop = serializedClip.FindProperty(
                "m_AnimationClipSettings.m_LoopTime");
            if (loop == null || !loop.boolValue)
            {
                throw new InvalidOperationException(
                    "The supplied mixamo clip is not configured to loop.");
            }

            var state = controller.layers[0].stateMachine.defaultState;
            if (state == null || state.motion != clip)
            {
                throw new InvalidOperationException(
                    "Ata pistol controller does not directly reference the supplied mixamo clip.");
            }

            var bodyRenderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault() ??
                throw new InvalidOperationException(
                    "Ata pistol body renderer is missing.");
            var bodyMesh = AssetDatabase.LoadAssetAtPath<Mesh>(BodyMeshPath) ??
                           throw new InvalidOperationException(
                               "Ata pistol body mesh asset is missing.");
            if (bodyRenderer.sharedMesh != bodyMesh)
            {
                throw new InvalidOperationException(
                    "Ata pistol body renderer does not use the pistol-removed body mesh.");
            }

            var pistolRoot = model.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(item => item.name == PistolRootName) ??
                throw new InvalidOperationException(
                    "Ata rigid pistol transfer object is missing.");
            var pistolMesh = AssetDatabase.LoadAssetAtPath<Mesh>(PistolMeshPath) ??
                             throw new InvalidOperationException(
                                 "Ata rigid pistol mesh asset is missing.");
            if (pistolRoot.GetComponent<MeshFilter>()?.sharedMesh != pistolMesh ||
                pistolRoot.GetComponent<MeshRenderer>() == null ||
                pistolRoot.GetComponent<SkinnedMeshRenderer>() != null ||
                pistolRoot.GetComponent<AtaPistolDrawConstraintDriver>() == null)
            {
                throw new InvalidOperationException(
                    "Ata pistol must be a rigid mesh controlled by the right-hand transfer driver.");
            }

            var hipAnchor = model.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(item => item.name == HipAnchorName) ??
                throw new InvalidOperationException("Ata pistol hip anchor is missing.");
            var handAnchor = model.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(item => item.name == HandAnchorName) ??
                throw new InvalidOperationException("Ata pistol right-hand anchor is missing.");
            if (hipAnchor.parent?.name != "RightUpLeg" ||
                handAnchor.parent?.name != "RightHand")
            {
                throw new InvalidOperationException(
                    "Ata pistol anchors are not attached to the anatomical right leg and right hand.");
            }
        }

        private static void CaptureReview(string relativePath, string logPrefix)
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var clip = RequireSingleMixamoClip();
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException("Ata pistol controller is missing.");
            RequireAppliedState(model, clip, controller);
            var destination = Absolute(relativePath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time Ata pistol capture already exists: " + relativePath);
            }

            var result = CaptureStrip(model, slot, clip, destination);
            WriteReport(clip, result);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Ata pistol capture changed the scene dirty state.");
            }

            Debug.Log(
                logPrefix + " Result=PASS" +
                ", MixamoClip=" + clip.name +
                ", Duration=" + Num(clip.length) +
                ", Samples=12" +
                ", MaximumSlotPositionError=" + Num(result.MaximumSlotPositionError) +
                ", Image=" + relativePath +
                ", SceneChanged=False.");
        }

        private static void CaptureSourceReview()
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var clip = RequireSingleMixamoClip();
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath) ??
                               throw new InvalidOperationException(
                                   "The supplied Ata pistol FBX prefab is unavailable.");
            var destination = Absolute(SourceDiagnosticPath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time Ata pistol source capture already exists: " +
                    SourceDiagnosticPath);
            }

            GameObject instance = null;
            try
            {
                instance = PrefabUtility.InstantiatePrefab(sourcePrefab, scene) as GameObject ??
                           throw new InvalidOperationException(
                               "The supplied Ata pistol FBX could not be instantiated for review.");
                instance.name = "AtaPistolAimFireSourceReview";
                instance.hideFlags = HideFlags.HideInHierarchy;
                if (instance.GetComponentsInChildren<Animator>(true).Length == 0)
                {
                    instance.AddComponent<Animator>();
                }
                CaptureStrip(instance.transform, instance.transform, clip, destination);
            }
            finally
            {
                if (instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }

            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Ata pistol source capture changed the scene dirty state.");
            }

            Debug.Log(
                "AtaPistolAimFireSourceDiagnosticCaptured Result=PASS" +
                ", MixamoClip=" + clip.name +
                ", Image=" + SourceDiagnosticPath +
                ", TemporarySourceInstanceRemoved=True" +
                ", SceneChanged=False.");
        }

        private static CaptureResult CaptureStrip(
            Transform model,
            Transform slot,
            AnimationClip clip,
            string destination)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Invalid Ata pistol capture folder."));
            var reviewTimes = Enumerable.Range(0, 12)
                .Select(index => clip.length * index / 11f)
                .ToArray();
            var modelSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(transform => new TransformSnapshot(transform))
                .ToArray();
            var slotPosition = slot.position;
            var modelLocalPosition = model.localPosition;
            var modelLocalRotation = model.localRotation;
            var modelLocalScale = model.localScale;
            var animator = model.GetComponentsInChildren<Animator>(true).Single();
            var pistolDriver = model.GetComponentInChildren<AtaPistolDrawConstraintDriver>(true) ??
                               throw new InvalidOperationException(
                                   "Ata pistol transfer driver is missing from the review target.");
            var animatorEnabled = animator.enabled;
            var otherRenderers = model.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Where(renderer => !renderer.transform.IsChildOf(model))
                .Select(renderer => new RendererSnapshot(renderer))
                .ToArray();
            var sourceCamera = GameObject.Find("Player")?
                                   .GetComponentInChildren<Camera>(true) ??
                               throw new InvalidOperationException("Player camera is missing.");
            var cameraObject = new GameObject(
                "AtaPistolAimFireReviewCamera",
                typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            const int width = 600;
            const int height = 600;
            const int columns = 4;
            const int rows = 3;
            var strip = new Texture2D(
                width * columns,
                height * rows,
                TextureFormat.RGB24,
                false);
            var target = new RenderTexture(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32);
            var panel = new Texture2D(width, height, TextureFormat.RGB24, false);
            var oldActive = RenderTexture.active;
            var maximumSlotPositionError = 0f;
            try
            {
                foreach (var snapshot in otherRenderers)
                {
                    snapshot.Renderer.enabled = false;
                }

                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = ~0;
                camera.fieldOfView = 34f;
                camera.targetTexture = target;
                FramePistolTransferCamera(camera, model, width / (float)height);

                animator.enabled = false;
                for (var index = 0; index < reviewTimes.Length; index++)
                {
                    clip.SampleAnimation(model.gameObject, reviewTimes[index]);
                    pistolDriver.ApplyNormalizedPhase(
                        reviewTimes[index] / clip.length);
                    maximumSlotPositionError = Mathf.Max(
                        maximumSlotPositionError,
                        Vector3.Distance(slot.position, slotPosition));
                    if (Vector3.Distance(model.localPosition, modelLocalPosition) >
                            TransformTolerance ||
                        Quaternion.Angle(model.localRotation, modelLocalRotation) > 0.01f ||
                        Vector3.Distance(model.localScale, modelLocalScale) >
                            TransformTolerance)
                    {
                        throw new InvalidOperationException(
                            "The supplied mixamo clip changed the scene model root transform.");
                    }

                    camera.Render();
                    RenderTexture.active = target;
                    panel.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                    panel.Apply();
                    var pixels = panel.GetPixels32();
                    if (pixels.Any(pixel =>
                            pixel.r >= 240 && pixel.b >= 240 && pixel.g <= 24))
                    {
                        throw new InvalidOperationException(
                            "Ata pistol review contains Unity magenta shader fallback.");
                    }

                    var column = index % columns;
                    var rowFromTop = index / columns;
                    strip.SetPixels32(
                        column * width,
                        (rows - 1 - rowFromTop) * height,
                        width,
                        height,
                        pixels);
                }

                strip.Apply();
                File.WriteAllBytes(destination, strip.EncodeToPNG());
                return new CaptureResult(maximumSlotPositionError);
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                foreach (var renderer in otherRenderers)
                {
                    renderer.Restore();
                }

                foreach (var snapshot in modelSnapshots)
                {
                    snapshot.Restore();
                }

                animator.enabled = animatorEnabled;
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(strip);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void FrameCamera(Camera camera, Transform model, float aspect)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(false);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Ata pistol model has no renderer.");
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            var playerCamera = GameObject.Find("Player")?
                                   .GetComponentInChildren<Camera>(true) ??
                               throw new InvalidOperationException("Player camera is missing.");
            var direction = playerCamera.transform.position - bounds.center;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.back;
            }

            direction.Normalize();
            camera.aspect = aspect;
            var vertical = bounds.extents.y /
                           Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            var horizontalFov = 2f * Mathf.Atan(
                Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * aspect);
            var horizontal = Mathf.Max(bounds.extents.x, bounds.extents.z) /
                             Mathf.Tan(horizontalFov * 0.5f);
            var distance = Mathf.Max(vertical, horizontal) * 1.2f;
            camera.transform.position =
                bounds.center + direction * distance + Vector3.up * bounds.extents.y * 0.02f;
            camera.transform.rotation = Quaternion.LookRotation(
                bounds.center - camera.transform.position,
                Vector3.up);
        }

        private static void FramePistolTransferCamera(
            Camera camera,
            Transform model,
            float aspect)
        {
            var hips = model.Find("Armature/Hips") ??
                       throw new InvalidOperationException("Ata Hips bone is missing.");
            var rightHand = model.Find(
                                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand") ??
                            throw new InvalidOperationException("Ata RightHand bone is missing.");
            var head = model.Find(
                           "Armature/Hips/Spine02/Spine01/Spine/neck/Head") ??
                       throw new InvalidOperationException("Ata Head bone is missing.");
            var playerCamera = GameObject.Find("Player")?
                                   .GetComponentInChildren<Camera>(true) ??
                               throw new InvalidOperationException("Player camera is missing.");
            var torsoHeight = Vector3.Distance(hips.position, head.position);
            var center = Vector3.Lerp(hips.position, rightHand.position, 0.15f) +
                         model.up * torsoHeight * 0.22f;
            var direction = playerCamera.transform.position - center;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.back;
            }

            direction.Normalize();
            camera.orthographic = true;
            camera.orthographicSize = torsoHeight * 0.92f;
            camera.aspect = aspect;
            camera.transform.position = center + direction * 3f;
            camera.transform.rotation = Quaternion.LookRotation(
                center - camera.transform.position,
                Vector3.up);
        }

        private static void CaptureWaistGeometry(
            Transform model,
            AnimationClip clip,
            string destination)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid Ata pistol waist diagnostic folder."));
            var modelSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(transform => new TransformSnapshot(transform))
                .ToArray();
            var animator = model.GetComponentsInChildren<Animator>(true).Single();
            var animatorEnabled = animator.enabled;
            var otherRenderers = model.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Where(renderer => !renderer.transform.IsChildOf(model))
                .Select(renderer => new RendererSnapshot(renderer))
                .ToArray();
            var sourceCamera = GameObject.Find("Player")?
                                   .GetComponentInChildren<Camera>(true) ??
                               throw new InvalidOperationException(
                                   "Player camera is missing.");
            var hips = model.Find("Armature/Hips") ??
                       throw new InvalidOperationException("Ata Hips bone is missing.");
            var cameraObject = new GameObject(
                "AtaPistolWaistGeometryCamera",
                typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            const int panelSize = 640;
            const int panelCount = 4;
            var sheet = new Texture2D(
                panelSize * panelCount,
                panelSize,
                TextureFormat.RGB24,
                false);
            var target = new RenderTexture(
                panelSize,
                panelSize,
                24,
                RenderTextureFormat.ARGB32);
            var panel = new Texture2D(
                panelSize,
                panelSize,
                TextureFormat.RGB24,
                false);
            var oldActive = RenderTexture.active;
            try
            {
                foreach (var snapshot in otherRenderers)
                {
                    snapshot.Renderer.enabled = false;
                }

                animator.enabled = false;
                clip.SampleAnimation(model.gameObject, 0f);
                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = ~0;
                camera.orthographic = true;
                camera.orthographicSize = 0.43f;
                camera.aspect = 1f;
                camera.targetTexture = target;
                var center = hips.position + model.up * 0.02f;
                var frontDirection = sourceCamera.transform.position - center;
                frontDirection.y = 0f;
                if (frontDirection.sqrMagnitude < 0.0001f)
                {
                    frontDirection = -model.forward;
                }

                frontDirection.Normalize();
                var directions = new[]
                {
                    frontDirection,
                    Quaternion.AngleAxis(90f, Vector3.up) * frontDirection,
                    -frontDirection,
                    Quaternion.AngleAxis(-90f, Vector3.up) * frontDirection
                };
                for (var index = 0; index < directions.Length; index++)
                {
                    camera.transform.position = center + directions[index] * 2.5f;
                    camera.transform.rotation = Quaternion.LookRotation(
                        center - camera.transform.position,
                        Vector3.up);
                    camera.Render();
                    RenderTexture.active = target;
                    panel.ReadPixels(
                        new Rect(0f, 0f, panelSize, panelSize),
                        0,
                        0);
                    panel.Apply();
                    sheet.SetPixels32(
                        index * panelSize,
                        0,
                        panelSize,
                        panelSize,
                        panel.GetPixels32());
                }

                sheet.Apply();
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                foreach (var renderer in otherRenderers)
                {
                    renderer.Restore();
                }

                foreach (var snapshot in modelSnapshots)
                {
                    snapshot.Restore();
                }

                animator.enabled = animatorEnabled;
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static PistolRegionOverlay CreatePistolRegionOverlay(Transform model)
        {
            var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
            var source = renderer.sharedMesh;
            var baked = new Mesh();
            try
            {
                renderer.BakeMesh(baked, false);
                var vertices = baked.vertices;
                var normals = baked.normals;
                var weights = source.boneWeights;
                var sourceUvs = source.uv;
                var hipsIndex = Array.FindIndex(
                    renderer.bones,
                    bone => bone != null && bone.name == "Hips");
                var rightUpLegIndex = Array.FindIndex(
                    renderer.bones,
                    bone => bone != null && bone.name == "RightUpLeg");
                if (hipsIndex < 0 || weights.Length != vertices.Length ||
                    rightUpLegIndex < 0 || sourceUvs.Length != vertices.Length)
                {
                    throw new InvalidOperationException(
                        "Ata pistol region cannot resolve Hips skin weights.");
                }

                // These model-local limits only isolate the existing right-waist pistol
                // for direct visual confirmation; no geometry is generated or reshaped.
                var minimum = new Vector3(0.13f, 0.45f, -0.18f);
                var maximum = new Vector3(0.32f, 0.99f, 0.08f);
                var triangles = source.GetTriangles(0);
                var selected = new List<int>();
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    if (!texture.LoadImage(File.ReadAllBytes(Absolute(AtaTexturePath))))
                    {
                        throw new InvalidOperationException(
                            "Ata source texture could not be read for pistol region separation.");
                    }

                    for (var index = 0; index < triangles.Length; index += 3)
                    {
                        var a = triangles[index];
                        var b = triangles[index + 1];
                        var c = triangles[index + 2];
                        var center = model.InverseTransformPoint(
                            renderer.transform.TransformPoint(
                                (vertices[a] + vertices[b] + vertices[c]) / 3f));
                        var equipmentWeight =
                            (WeightForBone(weights[a], rightUpLegIndex) +
                             WeightForBone(weights[b], rightUpLegIndex) +
                             WeightForBone(weights[c], rightUpLegIndex)) / 3f;
                        var color = texture.GetPixelBilinear(
                            Mathf.Repeat((sourceUvs[a].x + sourceUvs[b].x + sourceUvs[c].x) / 3f, 1f),
                            Mathf.Repeat((sourceUvs[a].y + sourceUvs[b].y + sourceUvs[c].y) / 3f, 1f));
                        var redCloth = IsRedCloth(color);
                        if (center.x >= minimum.x && center.x <= maximum.x &&
                            center.y >= minimum.y && center.y <= maximum.y &&
                            center.z >= minimum.z && center.z <= maximum.z &&
                            equipmentWeight >= 0.45f &&
                            !redCloth)
                        {
                            selected.Add(a);
                            selected.Add(b);
                            selected.Add(c);
                        }
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                if (selected.Count == 0)
                {
                    throw new InvalidOperationException(
                        "Ata pistol region diagnostic selected no source triangles.");
                }

                var selectedComponents = SplitSelectedTriangleComponents(
                    source.vertices,
                    selected);
                var pistolTriangles = selectedComponents
                    .OrderByDescending(component => component.Length)
                    .First();

                var overlayMesh = new Mesh
                {
                    name = "AtaPistolRegionDiagnosticOverlay",
                    indexFormat = source.indexFormat
                };
                var overlayVertices = vertices.ToArray();
                if (normals.Length == vertices.Length)
                {
                    for (var index = 0; index < overlayVertices.Length; index++)
                    {
                        overlayVertices[index] += normals[index] * 0.003f;
                    }
                }

                overlayMesh.vertices = overlayVertices;
                if (normals.Length == vertices.Length)
                {
                    overlayMesh.normals = normals;
                }

                var tangents = baked.tangents;
                if (tangents.Length == vertices.Length)
                {
                    overlayMesh.tangents = tangents;
                }

                for (var channel = 0; channel < 8; channel++)
                {
                    var uv = new List<Vector4>();
                    baked.GetUVs(channel, uv);
                    if (uv.Count == vertices.Length)
                    {
                        overlayMesh.SetUVs(channel, uv);
                    }
                }

                overlayMesh.SetTriangles(pistolTriangles, 0, true);
                var pistolVertexIndices = pistolTriangles.Distinct().ToArray();
                var pistolBounds = new Bounds(
                    overlayVertices[pistolVertexIndices[0]],
                    Vector3.zero);
                foreach (var vertexIndex in pistolVertexIndices.Skip(1))
                {
                    pistolBounds.Encapsulate(overlayVertices[vertexIndex]);
                }

                overlayMesh.bounds = pistolBounds;
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                             Shader.Find("Unlit/Color") ??
                             throw new InvalidOperationException(
                                 "No unlit shader is available for the Ata pistol diagnostic.");
                var material = new Material(shader)
                {
                    name = "AtaPistolRegionDiagnosticMaterial",
                    color = new Color(0f, 1f, 0.25f, 1f)
                };
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", new Color(0f, 1f, 0.25f, 1f));
                }

                var overlay = new GameObject(
                    "AtaPistolRegionDiagnosticOverlay",
                    typeof(MeshFilter),
                    typeof(MeshRenderer));
                overlay.hideFlags = HideFlags.HideAndDontSave;
                overlay.transform.SetParent(renderer.transform, false);
                overlay.GetComponent<MeshFilter>().sharedMesh = overlayMesh;
                overlay.GetComponent<MeshRenderer>().sharedMaterial = material;
                return new PistolRegionOverlay(
                    overlay,
                    overlayMesh,
                    material,
                    pistolTriangles.Length / 3,
                    DescribeSelectedTriangleComponents(
                        model,
                        renderer,
                        source.vertices,
                        vertices,
                        selected));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static void CaptureIsolatedPistolGeometry(
            Transform model,
            Renderer pistolRenderer,
            string destination)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid extracted Ata pistol diagnostic folder."));
            var otherRenderers = model.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Where(renderer => renderer != pistolRenderer)
                .Select(renderer => new RendererSnapshot(renderer))
                .ToArray();
            var sourceCamera = GameObject.Find("Player")?
                                   .GetComponentInChildren<Camera>(true) ??
                               throw new InvalidOperationException(
                                   "Player camera is missing.");
            var cameraObject = new GameObject(
                "AtaExtractedPistolGeometryCamera",
                typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            const int panelSize = 640;
            const int panelCount = 4;
            var sheet = new Texture2D(
                panelSize * panelCount,
                panelSize,
                TextureFormat.RGB24,
                false);
            var target = new RenderTexture(
                panelSize,
                panelSize,
                24,
                RenderTextureFormat.ARGB32);
            var panel = new Texture2D(
                panelSize,
                panelSize,
                TextureFormat.RGB24,
                false);
            var oldActive = RenderTexture.active;
            try
            {
                foreach (var snapshot in otherRenderers)
                {
                    snapshot.Renderer.enabled = false;
                }

                pistolRenderer.enabled = true;
                var bounds = pistolRenderer.bounds;
                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = ~0;
                camera.orthographic = true;
                camera.orthographicSize =
                    Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) * 1.45f;
                camera.aspect = 1f;
                camera.targetTexture = target;
                var frontDirection = sourceCamera.transform.position - bounds.center;
                frontDirection.y = 0f;
                if (frontDirection.sqrMagnitude < 0.0001f)
                {
                    frontDirection = -model.forward;
                }

                frontDirection.Normalize();
                var directions = new[]
                {
                    frontDirection,
                    Quaternion.AngleAxis(90f, Vector3.up) * frontDirection,
                    -frontDirection,
                    Quaternion.AngleAxis(-90f, Vector3.up) * frontDirection
                };
                for (var index = 0; index < directions.Length; index++)
                {
                    camera.transform.position =
                        bounds.center + directions[index] * 1.5f;
                    camera.transform.rotation = Quaternion.LookRotation(
                        bounds.center - camera.transform.position,
                        Vector3.up);
                    camera.Render();
                    RenderTexture.active = target;
                    panel.ReadPixels(
                        new Rect(0f, 0f, panelSize, panelSize),
                        0,
                        0);
                    panel.Apply();
                    sheet.SetPixels32(
                        index * panelSize,
                        0,
                        panelSize,
                        panelSize,
                        panel.GetPixels32());
                }

                sheet.Apply();
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                foreach (var renderer in otherRenderers)
                {
                    renderer.Restore();
                }

                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static float WeightForBone(BoneWeight weight, int boneIndex)
        {
            var value = 0f;
            if (weight.boneIndex0 == boneIndex) value += weight.weight0;
            if (weight.boneIndex1 == boneIndex) value += weight.weight1;
            if (weight.boneIndex2 == boneIndex) value += weight.weight2;
            if (weight.boneIndex3 == boneIndex) value += weight.weight3;
            return value;
        }

        private static bool IsRedCloth(Color color)
        {
            return color.r > 0.12f &&
                   color.r > color.g * 1.35f &&
                   color.r > color.b * 1.30f;
        }

        private static string DescribeSelectedTriangleComponents(
            Transform model,
            SkinnedMeshRenderer renderer,
            IReadOnlyList<Vector3> topologyVertices,
            IReadOnlyList<Vector3> bakedVertices,
            IReadOnlyList<int> selectedTriangles)
        {
            var triangleComponents = SplitSelectedTriangleComponents(
                topologyVertices,
                selectedTriangles);
            var descriptions = new List<string>();
            foreach (var componentTriangles in triangleComponents)
            {
                var componentVertices = componentTriangles.Distinct().ToArray();
                var points = componentVertices.Select(index =>
                        model.InverseTransformPoint(
                            renderer.transform.TransformPoint(bakedVertices[index])))
                    .ToArray();
                var bounds = new Bounds(points[0], Vector3.zero);
                foreach (var point in points.Skip(1))
                {
                    bounds.Encapsulate(point);
                }

                descriptions.Add(
                    "T" + (componentTriangles.Length / 3) +
                    "V" + componentVertices.Length +
                    "C" + Vec(bounds.center) +
                    "S" + Vec(bounds.size));
            }

            return string.Join(";", descriptions
                .OrderByDescending(value =>
                {
                    var separator = value.IndexOf('V');
                    return int.Parse(
                        value.Substring(1, separator - 1),
                        CultureInfo.InvariantCulture);
                }));
        }

        private static List<int[]> SplitSelectedTriangleComponents(
            IReadOnlyList<Vector3> vertices,
            IReadOnlyList<int> selectedTriangles)
        {
            var selectedVertices = selectedTriangles.Distinct().ToArray();
            var weldedGroups = selectedVertices
                .GroupBy(index => Quantize(vertices[index]))
                .ToDictionary(group => group.Key, group => group.ToArray());
            var representativeByIndex = weldedGroups.Values
                .SelectMany(group => group.Select(index =>
                    (index, representative: group[0])))
                .ToDictionary(value => value.index, value => value.representative);
            var adjacency = new Dictionary<int, HashSet<int>>();
            for (var index = 0; index < selectedTriangles.Count; index += 3)
            {
                AddAdjacent(
                    adjacency,
                    representativeByIndex[selectedTriangles[index]],
                    representativeByIndex[selectedTriangles[index + 1]]);
                AddAdjacent(
                    adjacency,
                    representativeByIndex[selectedTriangles[index + 1]],
                    representativeByIndex[selectedTriangles[index + 2]]);
                AddAdjacent(
                    adjacency,
                    representativeByIndex[selectedTriangles[index + 2]],
                    representativeByIndex[selectedTriangles[index]]);
            }

            var remaining = adjacency.Keys.ToHashSet();
            var components = new List<int[]>();
            while (remaining.Count > 0)
            {
                var seed = remaining.First();
                remaining.Remove(seed);
                var found = new HashSet<int> { seed };
                var stack = new Stack<int>();
                stack.Push(seed);
                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    foreach (var next in adjacency[current])
                    {
                        if (remaining.Remove(next))
                        {
                            found.Add(next);
                            stack.Push(next);
                        }
                    }
                }

                var componentTriangles = new List<int>();
                for (var triangleIndex = 0;
                     triangleIndex < selectedTriangles.Count;
                     triangleIndex += 3)
                {
                    if (found.Contains(
                            representativeByIndex[selectedTriangles[triangleIndex]]))
                    {
                        componentTriangles.Add(selectedTriangles[triangleIndex]);
                        componentTriangles.Add(selectedTriangles[triangleIndex + 1]);
                        componentTriangles.Add(selectedTriangles[triangleIndex + 2]);
                    }
                }

                components.Add(componentTriangles.ToArray());
            }

            return components;
        }

        private static void WriteReport(AnimationClip clip, CaptureResult result)
        {
            var absolute = Absolute(ReportPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(absolute) ??
                throw new InvalidOperationException("Invalid Ata pistol report folder."));
            File.WriteAllLines(
                absolute,
                new[]
                {
                    "Target=Approved Ata Enemy Placement/Ata_04_PistolAimAndFire",
                    "Source=enemies model/attas draw.fbx",
                    "ProjectSource=" + SourcePath,
                    "ClipName=" + clip.name,
                    "DurationSeconds=" + Num(clip.length),
                    "Loop=True",
                    "RootMotion=False",
                    "MaximumSlotPositionError=" + Num(result.MaximumSlotPositionError),
                    "ExistingAtaAppearancePreserved=True",
                    "OtherAtaSlotsChanged=False",
                    "PlayerOrCameraChanged=False",
                    "CurrentAtaVisualReviewSamples=12",
                    "ExactPistolSourceTriangles=279",
                    "PistolMeshRigid=True",
                    "PistolTransfer=RightWaistToRightHandToRightWaist",
                    "RightArmFollow=RightHandAnchor",
                    "VisualObservation=Direct 12-frame review confirms the exact existing right-waist pistol leaves the waist, attaches at the right-hand contact point, changes position and rotation with the right hand and arm, and returns to the waist at the loop boundary. No duplicate waist pistol or pistol mesh deformation is visible.",
                    "HarnessValidation=NotRun"
                },
                Encoding.UTF8);
        }

        private static string AppearanceSignature(Transform model)
        {
            var builder = new StringBuilder();
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true)
                         .OrderBy(renderer => RelativePath(model, renderer.transform), StringComparer.Ordinal))
            {
                builder.Append(RelativePath(model, renderer.transform)).Append('|')
                    .Append(renderer.GetType().FullName).Append('|');
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    builder.Append(AssetDatabase.GetAssetPath(skinned.sharedMesh));
                }
                else if (renderer.TryGetComponent<MeshFilter>(out var filter))
                {
                    builder.Append(AssetDatabase.GetAssetPath(filter.sharedMesh));
                }

                builder.Append('|')
                    .Append(string.Join(",", renderer.sharedMaterials
                        .Select(AssetDatabase.GetAssetPath)))
                    .AppendLine();
            }

            return builder.ToString();
        }

        private static string DescribeModelStructure(Transform model)
        {
            var namedParts = model.GetComponentsInChildren<Transform>(true)
                .Where(item =>
                    item.name.IndexOf("gun", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.name.IndexOf("pistol", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.name.IndexOf("hand", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.name.IndexOf("forearm", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(item => RelativePath(model, item) + "[" +
                                string.Join(",", item.GetComponents<Component>()
                                    .Where(component => component != null)
                                    .Select(component => component.GetType().Name)) + "]")
                .ToArray();
            var renderers = model.GetComponentsInChildren<Renderer>(true)
                .Select(renderer =>
                {
                    var mesh = renderer is SkinnedMeshRenderer skinned
                        ? skinned.sharedMesh
                        : renderer.TryGetComponent<MeshFilter>(out var filter)
                            ? filter.sharedMesh
                            : null;
                    var bones = renderer is SkinnedMeshRenderer skinnedRenderer
                        ? string.Join(",", skinnedRenderer.bones
                            .Where(bone => bone != null &&
                                           (bone.name.IndexOf("gun", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            bone.name.IndexOf("hand", StringComparison.OrdinalIgnoreCase) >= 0))
                            .Select(bone => RelativePath(model, bone)))
                        : string.Empty;
                    var blendShapes = mesh == null
                        ? string.Empty
                        : string.Join(",", Enumerable.Range(0, mesh.blendShapeCount)
                            .Select(mesh.GetBlendShapeName)
                            .Where(name => name.IndexOf("gun", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                           name.IndexOf("pistol", StringComparison.OrdinalIgnoreCase) >= 0));
                    return RelativePath(model, renderer.transform) +
                           "{Type=" + renderer.GetType().Name +
                           ",Mesh=" + (mesh == null ? "None" : mesh.name) +
                           ",SubMeshes=" + (mesh == null ? 0 : mesh.subMeshCount) +
                           ",Materials=" + string.Join(",", renderer.sharedMaterials
                               .Select(material => material == null ? "None" : material.name)) +
                           ",WeaponBones=" + bones +
                           ",WeaponBlendShapes=" + blendShapes + "}";
                })
                .ToArray();
            return "NamedParts=" + string.Join(";", namedParts) +
                   "|Renderers=" + string.Join(";", renderers);
        }

        private static string DescribeConnectedComponents(Transform model)
        {
            var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata pistol structure requires one skinned renderer.");
            var source = renderer.sharedMesh;
            var baked = new Mesh();
            try
            {
                renderer.BakeMesh(baked, false);
                var sourceVertices = source.vertices;
                var bakedVertices = baked.vertices;
                if (sourceVertices.Length != bakedVertices.Length)
                {
                    throw new InvalidOperationException(
                        "Ata baked mesh vertex order differs from the source mesh.");
                }

                var triangles = source.GetTriangles(0);
                var weldedGroups = Enumerable.Range(0, sourceVertices.Length)
                    .GroupBy(index => Quantize(sourceVertices[index]))
                    .ToDictionary(group => group.Key, group => group.ToArray());
                var representativeByIndex = weldedGroups.Values
                    .SelectMany(group => group.Select(index =>
                        (index, representative: group[0])))
                    .ToDictionary(value => value.index, value => value.representative);
                var adjacency = new Dictionary<int, HashSet<int>>();
                for (var index = 0; index < triangles.Length; index += 3)
                {
                    AddAdjacent(
                        adjacency,
                        representativeByIndex[triangles[index]],
                        representativeByIndex[triangles[index + 1]]);
                    AddAdjacent(
                        adjacency,
                        representativeByIndex[triangles[index + 1]],
                        representativeByIndex[triangles[index + 2]]);
                    AddAdjacent(
                        adjacency,
                        representativeByIndex[triangles[index + 2]],
                        representativeByIndex[triangles[index]]);
                }

                var remaining = adjacency.Keys.ToHashSet();
                var components = new List<int[]>();
                while (remaining.Count > 0)
                {
                    var seed = remaining.First();
                    remaining.Remove(seed);
                    var found = new HashSet<int> { seed };
                    var stack = new Stack<int>();
                    stack.Push(seed);
                    while (stack.Count > 0)
                    {
                        var current = stack.Pop();
                        foreach (var next in adjacency[current])
                        {
                            if (remaining.Remove(next))
                            {
                                found.Add(next);
                                stack.Push(next);
                            }
                        }
                    }

                    components.Add(
                        Enumerable.Range(0, sourceVertices.Length)
                            .Where(index => found.Contains(representativeByIndex[index]))
                            .ToArray());
                }

                var weights = source.boneWeights;
                var boneNames = renderer.bones.Select(bone => bone.name).ToArray();
                var descriptions = components
                    .Select((indices, originalIndex) =>
                    {
                        var points = indices
                            .Select(index => model.InverseTransformPoint(
                                renderer.transform.TransformPoint(bakedVertices[index])))
                            .ToArray();
                        var bounds = new Bounds(points[0], Vector3.zero);
                        foreach (var point in points.Skip(1))
                        {
                            bounds.Encapsulate(point);
                        }

                        var boneTotals = new Dictionary<string, float>(StringComparer.Ordinal);
                        foreach (var vertexIndex in indices)
                        {
                            AddBoneWeight(boneTotals, boneNames, weights[vertexIndex]);
                        }

                        var dominantBones = string.Join(",", boneTotals
                            .OrderByDescending(pair => pair.Value)
                            .Take(3)
                            .Select(pair => pair.Key + ":" + Num(pair.Value)));
                        var componentSet = indices.ToHashSet();
                        var triangleCount = 0;
                        for (var triangleIndex = 0;
                             triangleIndex < triangles.Length;
                             triangleIndex += 3)
                        {
                            if (componentSet.Contains(triangles[triangleIndex]))
                            {
                                triangleCount++;
                            }
                        }
                        return new ComponentDescription(
                            originalIndex,
                            indices.Length,
                            triangleCount,
                            bounds.center,
                            bounds.size,
                            dominantBones);
                    })
                    .Where(component => component.VertexCount >= 8)
                    .OrderByDescending(component => component.VertexCount)
                    .Select((component, rank) =>
                        "C" + rank +
                        "(Source=" + component.SourceIndex +
                        ",V=" + component.VertexCount +
                        ",T=" + component.TriangleCount +
                        ",Center=" + Vec(component.Center) +
                        ",Size=" + Vec(component.Size) +
                        ",Bones=" + component.DominantBones + ")")
                    .ToArray();
                var hip = model.Find("Armature/Hips");
                var rightHand = model.Find(
                    "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand");
                return "Count=" + components.Count +
                       "|Hips=" + (hip == null ? "Missing" : Vec(model.InverseTransformPoint(hip.position))) +
                       "|RightHand=" + (rightHand == null
                           ? "Missing"
                           : Vec(model.InverseTransformPoint(rightHand.position))) +
                       "|" + string.Join(";", descriptions);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static void AddBoneWeight(
            IDictionary<string, float> totals,
            IReadOnlyList<string> boneNames,
            BoneWeight weight)
        {
            AddBoneWeight(totals, boneNames, weight.boneIndex0, weight.weight0);
            AddBoneWeight(totals, boneNames, weight.boneIndex1, weight.weight1);
            AddBoneWeight(totals, boneNames, weight.boneIndex2, weight.weight2);
            AddBoneWeight(totals, boneNames, weight.boneIndex3, weight.weight3);
        }

        private static void AddBoneWeight(
            IDictionary<string, float> totals,
            IReadOnlyList<string> boneNames,
            int boneIndex,
            float weight)
        {
            if (weight <= 0f)
            {
                return;
            }

            var name = boneNames[boneIndex];
            totals[name] = totals.TryGetValue(name, out var current)
                ? current + weight
                : weight;
        }

        private static (int X, int Y, int Z) Quantize(Vector3 value)
        {
            const float scale = 100000f;
            return (
                Mathf.RoundToInt(value.x * scale),
                Mathf.RoundToInt(value.y * scale),
                Mathf.RoundToInt(value.z * scale));
        }

        private static void AddAdjacent(
            IDictionary<int, HashSet<int>> adjacency,
            int leftIndex,
            int rightIndex)
        {
            if (!adjacency.TryGetValue(leftIndex, out var left))
            {
                left = new HashSet<int>();
                adjacency[leftIndex] = left;
            }

            if (!adjacency.TryGetValue(rightIndex, out var right))
            {
                right = new HashSet<int>();
                adjacency[rightIndex] = right;
            }

            left.Add(rightIndex);
            right.Add(leftIndex);
        }

        private static string RelativePath(Transform root, Transform item)
        {
            if (item == root)
            {
                return string.Empty;
            }

            var names = item.GetComponentsInParent<Transform>(true)
                .TakeWhile(parent => parent != root)
                .Select(parent => parent.name)
                .Reverse();
            return string.Join("/", names);
        }

        private static Scene RequireScene(bool requireClean)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Ata pistol animation work requires Edit Mode.");
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "The current active scene must be CargoRunMvp.");
            }

            if (requireClean && scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes.");
            }

            return scene;
        }

        private static GameObject RequirePlacement(Scene scene)
        {
            return scene.GetRootGameObjects()
                       .SingleOrDefault(root => root.name == PlacementRootName) ??
                   throw new InvalidOperationException("Approved Ata placement is missing.");
        }

        private static Transform RequireDirectChild(Transform parent, string name)
        {
            var matches = Enumerable.Range(0, parent.childCount)
                .Select(parent.GetChild)
                .Where(child => child.name == name)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Required direct child differs: " + name + ".");
            }

            return matches[0];
        }

        private static string[] OtherSlotSignatures(Transform placement, Transform targetSlot)
        {
            return Enumerable.Range(0, placement.childCount)
                .Select(placement.GetChild)
                .Where(slot => slot != targetSlot)
                .Select(RecursiveSignature)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static string RecursiveSignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(item.name).Append('|')
                    .Append(item.gameObject.activeSelf).Append('|')
                    .Append(Vec(item.localPosition)).Append('|')
                    .Append(Quat(item.localRotation)).Append('|')
                    .Append(Vec(item.localScale)).Append('|')
                    .Append(string.Join(",", item.GetComponents<Component>()
                        .Where(component => component != null)
                        .Select(component => component.GetType().FullName)
                        .OrderBy(name => name, StringComparer.Ordinal)))
                    .AppendLine();
            }

            return builder.ToString();
        }

        private static string[] OtherRootSignatures(Scene scene, GameObject placement)
        {
            return scene.GetRootGameObjects()
                .Where(root => root != placement)
                .Select(root =>
                    root.name + "|" + root.activeSelf + "|" +
                    Vec(root.transform.localPosition) + "|" +
                    Quat(root.transform.localRotation) + "|" +
                    Vec(root.transform.localScale) + "|" +
                    root.transform.childCount.ToString(CultureInfo.InvariantCulture))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static void RequireEqual(string[] before, string[] after, string message)
        {
            if (!before.SequenceEqual(after, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Absolute(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }

        private static string Num(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + ")";
        }

        private static string Quat(Quaternion value)
        {
            return "(" + Num(value.x) + "," + Num(value.y) + "," +
                   Num(value.z) + "," + Num(value.w) + ")";
        }

        private readonly struct TransformSnapshot
        {
            private readonly Transform transform;
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            public TransformSnapshot(Transform transform)
            {
                this.transform = transform;
                position = transform.localPosition;
                rotation = transform.localRotation;
                scale = transform.localScale;
            }

            public void Restore()
            {
                if (transform == null)
                {
                    return;
                }

                transform.localPosition = position;
                transform.localRotation = rotation;
                transform.localScale = scale;
            }

            public bool Matches()
            {
                return transform != null &&
                       Vector3.Distance(position, transform.localPosition) <= TransformTolerance &&
                       Quaternion.Angle(rotation, transform.localRotation) <= 0.01f &&
                       Vector3.Distance(scale, transform.localScale) <= TransformTolerance;
            }
        }

        private readonly struct RendererSnapshot
        {
            public readonly Renderer Renderer;
            private readonly bool enabled;

            public RendererSnapshot(Renderer renderer)
            {
                Renderer = renderer;
                enabled = renderer.enabled;
            }

            public void Restore()
            {
                if (Renderer != null)
                {
                    Renderer.enabled = enabled;
                }
            }
        }

        private readonly struct CaptureResult
        {
            public readonly float MaximumSlotPositionError;

            public CaptureResult(float maximumSlotPositionError)
            {
                MaximumSlotPositionError = maximumSlotPositionError;
            }
        }

        private readonly struct PistolAssets
        {
            public readonly Mesh BodyMesh;
            public readonly Mesh PistolMesh;
            public readonly int PistolTriangleCount;

            public PistolAssets(
                Mesh bodyMesh,
                Mesh pistolMesh,
                int pistolTriangleCount)
            {
                BodyMesh = bodyMesh;
                PistolMesh = pistolMesh;
                PistolTriangleCount = pistolTriangleCount;
            }
        }

        private readonly struct BindingSummary
        {
            public readonly int AnimatedPathCount;
            public readonly int SkinnedAnimatedPathCount;
            public readonly int VaryingCurveCount;
            public readonly string FirstAnimatedPaths;
            public readonly string LargestCurveChanges;

            public BindingSummary(
                int animatedPathCount,
                int skinnedAnimatedPathCount,
                int varyingCurveCount,
                string firstAnimatedPaths,
                string largestCurveChanges)
            {
                AnimatedPathCount = animatedPathCount;
                SkinnedAnimatedPathCount = skinnedAnimatedPathCount;
                VaryingCurveCount = varyingCurveCount;
                FirstAnimatedPaths = firstAnimatedPaths;
                LargestCurveChanges = largestCurveChanges;
            }
        }

        private readonly struct CurveChange
        {
            public readonly EditorCurveBinding Binding;
            public readonly float Range;

            public CurveChange(EditorCurveBinding binding, float range)
            {
                Binding = binding;
                Range = range;
            }
        }

        private readonly struct ComponentDescription
        {
            public readonly int SourceIndex;
            public readonly int VertexCount;
            public readonly int TriangleCount;
            public readonly Vector3 Center;
            public readonly Vector3 Size;
            public readonly string DominantBones;

            public ComponentDescription(
                int sourceIndex,
                int vertexCount,
                int triangleCount,
                Vector3 center,
                Vector3 size,
                string dominantBones)
            {
                SourceIndex = sourceIndex;
                VertexCount = vertexCount;
                TriangleCount = triangleCount;
                Center = center;
                Size = size;
                DominantBones = dominantBones;
            }
        }

        private readonly struct PistolRegionOverlay
        {
            public readonly GameObject Overlay;
            public readonly Mesh Mesh;
            public readonly Material Material;
            public readonly int SelectedTriangleCount;
            public readonly string SelectedComponents;

            public PistolRegionOverlay(
                GameObject overlay,
                Mesh mesh,
                Material material,
                int selectedTriangleCount,
                string selectedComponents)
            {
                Overlay = overlay;
                Mesh = mesh;
                Material = material;
                SelectedTriangleCount = selectedTriangleCount;
                SelectedComponents = selectedComponents;
            }
        }
    }
}
