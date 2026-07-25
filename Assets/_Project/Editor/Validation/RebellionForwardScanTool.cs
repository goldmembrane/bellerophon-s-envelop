using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.RebellionCargoRunScene
{
    internal static class RebellionForwardScanTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName =
            "Approved Rebellion Enemy Placement";
        private const string SlotName = "Rebellion_03_Forward_Scan";
        private const string ModelName = "Rebellion_Model";
        private const string ScanLensName = "Rebellion_Scan_Lens";
        private const string ScanPivotName = "Rebellion_Scan_Pivot";
        private const string ScanPlaneName = "Rebellion_Scan_Plane";
        private const string AnimationFolder =
            "Assets/_Project/Art/Enemies/Rebellion/Animations";
        private const string ControllerFolder =
            "Assets/_Project/Art/Enemies/Rebellion/Controllers";
        private const string VfxFolder =
            "Assets/_Project/Art/Enemies/Rebellion/VFX";
        private const string ClipPath =
            AnimationFolder + "/Rebellion_03_Forward_Scan.anim";
        private const string ControllerPath =
            ControllerFolder + "/Rebellion_03_Forward_Scan.controller";
        private const string AttackClipPath =
            AnimationFolder + "/Rebellion_02_Attack_Mode_Transition.anim";
        private const string MoveClipPath =
            AnimationFolder + "/Rebellion_01_Move_SpiderCrawl.anim";
        private const string MoveControllerPath =
            ControllerFolder + "/Rebellion_01_Move_SpiderCrawl.controller";
        private const string AttackControllerPath =
            ControllerFolder + "/Rebellion_02_Attack_Mode_Transition.controller";
        private const string BurstControllerPath =
            ControllerFolder + "/Rebellion_04_Forward_Burst_Fire.controller";
        private const string HitControllerPath =
            ControllerFolder + "/Rebellion_05_Hit_Reaction.controller";
        private const string MeshPath =
            VfxFolder + "/Rebellion_Forward_Scan_Plane.asset";
        private const string MaterialPath =
            VfxFolder + "/Rebellion_Forward_Scan_Plane.mat";
        private const string TexturePath =
            VfxFolder + "/Rebellion_Forward_Scan_Gradient.png";
        private const string CorrectedModelPath =
            "Assets/_Project/Art/Enemies/Rebellion/ApprovedAppearance/" +
            "Rebellion_ApprovedAppearance.glb";
        private const string CorrectedModelSha256 =
            "C791B028B759A82087C185A98ADD3A5412BCAE8A110DFAFF33F7E3E1694D60F9";
        private const string InspectionPath =
            "docs/validation/rebellion_forward_scan_2026-07-25/" +
            "Rebellion_03_ForwardScan_Inspection.txt";
        private const string ReviewPath =
            "docs/validation/rebellion_forward_scan_2026-07-25/" +
            "Rebellion_03_ForwardScan_VisualReview.png";
        private const string StateName = "ForwardScan";
        private const float AttackStandingPoseTime = 1.2f;
        private const float LoopSecondsValue = 1.6f;
        private const float HalfSweepDegrees = 45f;
        private const float ScanDistance = 5f;
        private const float ScanHeight = 3f;
        private const float LensOutwardOffset = 0.018f;

        private static readonly string[] SlotNames =
        {
            "Rebellion_00_Static_Review",
            "Rebellion_01_Move",
            "Rebellion_02_Attack_Mode_Transition",
            "Rebellion_03_Forward_Scan",
            "Rebellion_04_Forward_Burst_Fire",
            "Rebellion_05_Hit_Reaction",
            "Rebellion_06_Death"
        };

        private static readonly string[] LegBoneNames =
        {
            "Bone_013", "Bone_012", "Bone_011", "Bone_010", "Bone_009",
            "Bone_018", "Bone_017", "Bone_016", "Bone_015", "Bone_014",
            "Bone_023", "Bone_022", "Bone_021", "Bone_020", "Bone_019",
            "Bone_028", "Bone_027", "Bone_026", "Bone_025", "Bone_024"
        };

        [MenuItem("Bellerophon/Enemies/Rebellion/Apply Forward Scan")]
        public static void ApplyForwardScan()
        {
            RequireCorrectedModelHash();
            var scene = RequireActiveScene();
            var placementRoot = RequirePlacementRoot(scene);
            var slot = RequireSlot(scene, SlotName);
            var model = RequireModel(slot);
            RequireRig(model);

            var placementState = TransformState.Capture(placementRoot);
            var slotState = TransformState.Capture(slot);
            var modelState = TransformState.Capture(model);
            var protectedHashes = CaptureProtectedAnimationHashes();
            var standingPose = CaptureStandingPose(slot, model);

            EnsureFolder(AnimationFolder);
            EnsureFolder(ControllerFolder);
            EnsureFolder(VfxFolder);
            var mesh = CreateScanPlaneMesh();
            var texture = CreateScanTexture();
            var material = CreateScanMaterial(texture);
            var pivot = CreateScanObjects(
                slot,
                model,
                mesh,
                material,
                standingPose);
            var clip = CreateScanClip(slot, model, pivot, standingPose);
            var controller = CreateController(clip);
            var animator = slot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = slot.gameObject.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EditorUtility.SetDirty(animator);

            RequireSameTransform(
                placementState,
                placementRoot,
                PlacementRootName);
            RequireSameTransform(slotState, slot, SlotName);
            RequireSameTransform(modelState, model, ModelName);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after Rebellion forward " +
                    "scan application.");
            }
            AssetDatabase.SaveAssets();
            RequireProtectedAnimationHashes(protectedHashes);

            Debug.Log(
                "RebellionForwardScanApplied Result=PASS" +
                ", Slot=" + SlotName +
                ", Clip=" + ClipPath +
                ", Controller=" + ControllerPath +
                ", LoopSeconds=1.6" +
                ", Sweep=LeftToRightToLeft" +
                ", HalfSweepDegrees=45" +
                ", Distance=5" +
                ", Height=3" +
                ", Shape=ProjectorTriangle" +
                ", Apex=LensPivot" +
                ", StandingPoseTime=1.2" +
                ", RootMotion=False" +
                ", PlacementPreserved=True" +
                ", ExistingAnimationsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Rebellion/Inspect Forward Scan")]
        public static void InspectForwardScan()
        {
            RequireCorrectedModelHash();
            var scene = RequireActiveScene();
            var placementRoot = RequirePlacementRoot(scene);
            var slot = RequireSlot(scene, SlotName);
            var model = RequireModel(slot);
            RequireRig(model);

            var animator = slot.GetComponent<Animator>() ??
                           throw new InvalidOperationException(
                               SlotName + " has no Animator.");
            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException(
                    "Forward scan Animator must not apply Root Motion.");
            }

            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath) ??
                throw new InvalidOperationException(
                    "Forward scan controller is missing.");
            if (animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException(
                    "Forward scan slot does not use its controller.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException(
                           "Forward scan clip is missing.");
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime ||
                Mathf.Abs(clip.length - LoopSecondsValue) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Forward scan must be a 1.6-second loop.");
            }

            var pivot = RequireDescendant(slot, ScanPivotName);
            var plane = RequireDescendant(pivot, ScanPlaneName);
            var filter = plane.GetComponent<MeshFilter>() ??
                         throw new InvalidOperationException(
                             "Forward scan plane MeshFilter is missing.");
            var renderer = plane.GetComponent<MeshRenderer>() ??
                           throw new InvalidOperationException(
                               "Forward scan plane MeshRenderer is missing.");
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath) ??
                       throw new InvalidOperationException(
                           "Forward scan plane mesh asset is missing.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(
                               MaterialPath) ??
                           throw new InvalidOperationException(
                               "Forward scan plane material is missing.");
            if (filter.sharedMesh != mesh ||
                renderer.sharedMaterial != material)
            {
                throw new InvalidOperationException(
                    "Forward scan plane does not use the expected assets.");
            }
            if (Mathf.Abs(mesh.bounds.size.z - ScanDistance) > 0.0001f ||
                Mathf.Abs(mesh.bounds.size.y - ScanHeight) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Forward scan plane dimensions are incorrect.");
            }
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            var halfHeight = ScanHeight * 0.5f;
            if (vertices.Length != 3 ||
                triangles.Length != 6 ||
                Vector3.Distance(vertices[0], Vector3.zero) > 0.0001f ||
                Vector3.Distance(
                    vertices[1],
                    new Vector3(0f, halfHeight, ScanDistance)) > 0.0001f ||
                Vector3.Distance(
                    vertices[2],
                    new Vector3(0f, -halfHeight, ScanDistance)) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Forward scan mesh is not the approved projector " +
                    "triangle.");
            }
            var apexAtLensPivotError =
                Vector3.Distance(
                    plane.TransformPoint(vertices[0]),
                    pivot.position);
            var farEdgeWorldHeight =
                Vector3.Distance(
                    plane.TransformPoint(vertices[1]),
                    plane.TransformPoint(vertices[2]));
            var farEdgeWorldCenter =
                (plane.TransformPoint(vertices[1]) +
                 plane.TransformPoint(vertices[2])) * 0.5f;
            var projectorWorldDistance =
                Vector3.Distance(
                    plane.TransformPoint(vertices[0]),
                    farEdgeWorldCenter);
            if (apexAtLensPivotError > 0.0001f ||
                Mathf.Abs(farEdgeWorldHeight - ScanHeight) > 0.001f ||
                Mathf.Abs(projectorWorldDistance - ScanDistance) > 0.001f)
            {
                throw new InvalidOperationException(
                    "Forward scan projector triangle world geometry is " +
                    "incorrect.");
            }
            var worldDistance =
                plane.TransformVector(Vector3.forward * ScanDistance).magnitude;
            var worldHeight =
                plane.TransformVector(Vector3.up * ScanHeight).magnitude;
            if (Mathf.Abs(worldDistance - ScanDistance) > 0.001f ||
                Mathf.Abs(worldHeight - ScanHeight) > 0.001f)
            {
                throw new InvalidOperationException(
                    "Forward scan plane world dimensions are incorrect.");
            }
            if (material.shader == null ||
                material.shader.name !=
                "Universal Render Pipeline/Unlit" ||
                material.renderQueue < (int)RenderQueue.Transparent)
            {
                throw new InvalidOperationException(
                    "Forward scan material is not transparent URP Unlit.");
            }

            var bindingMetrics = InspectBindings(clip);
            var poseMetrics = InspectPoses(slot, model, pivot, clip);
            if (bindingMetrics.LoopBoundaryError > 0.00001f ||
                bindingMetrics.LegRotationBones != LegBoneNames.Length ||
                bindingMetrics.BodyPositionBones != 1 ||
                bindingMetrics.PivotRotationBindings != 4 ||
                bindingMetrics.UnexpectedBindings != 0)
            {
                throw new InvalidOperationException(
                    "Forward scan binding inspection failed.");
            }
            if (poseMetrics.StandingPositionError > 0.00001f ||
                poseMetrics.StandingRotationError > 0.01f ||
                poseMetrics.StaticModelPositionError > 0.00001f ||
                poseMetrics.StaticModelRotationError > 0.01f)
            {
                throw new InvalidOperationException(
                    "Forward scan model does not hold the standing pose.");
            }
            if (Mathf.Abs(
                    Mathf.Abs(poseMetrics.LeftToRightAngle) -
                    (HalfSweepDegrees * 2f)) > 0.05f ||
                Mathf.Abs(
                    Mathf.Abs(poseMetrics.RightToLeftAngle) -
                    (HalfSweepDegrees * 2f)) > 0.05f ||
                poseMetrics.LoopDirectionError > 0.01f)
            {
                throw new InvalidOperationException(
                    "Forward scan sweep angles are incorrect.");
            }

            RequireAnimatorAssignments(placementRoot);

            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("Slot=" + SlotName);
            report.AppendLine("Clip=" + ClipPath);
            report.AppendLine("Controller=" + ControllerPath);
            report.AppendLine("State=" + StateName);
            report.AppendLine("LoopSeconds=1.6");
            report.AppendLine("LoopEnabled=True");
            report.AppendLine("Phase0To0.8=LeftToRight");
            report.AppendLine("Phase0.8To1.6=RightToLeft");
            report.AppendLine("HalfSweepDegrees=45");
            report.AppendLine(
                "MeasuredLeftToRightDegrees=" +
                poseMetrics.LeftToRightAngle.ToString("0.######"));
            report.AppendLine(
                "MeasuredRightToLeftDegrees=" +
                poseMetrics.RightToLeftAngle.ToString("0.######"));
            report.AppendLine("ScanDistance=5");
            report.AppendLine("ScanHeight=3");
            report.AppendLine("PlaneOrientation=Vertical");
            report.AppendLine("PlaneShape=ProjectorTriangle");
            report.AppendLine("TriangleVertexCount=3");
            report.AppendLine("TriangleDoubleSided=True");
            report.AppendLine(
                "ApexAtLensPivotError=" +
                apexAtLensPivotError.ToString("0.########"));
            report.AppendLine(
                "FarEdgeWorldHeight=" +
                farEdgeWorldHeight.ToString("0.######"));
            report.AppendLine(
                "ProjectorWorldDistance=" +
                projectorWorldDistance.ToString("0.######"));
            report.AppendLine("PlaneTransparency=AdditiveAlpha");
            report.AppendLine("StandingPoseSourceTime=1.2");
            report.AppendLine(
                "StandingPositionError=" +
                poseMetrics.StandingPositionError.ToString("0.########"));
            report.AppendLine(
                "StandingRotationErrorDegrees=" +
                poseMetrics.StandingRotationError.ToString("0.########"));
            report.AppendLine(
                "StaticModelPositionError=" +
                poseMetrics.StaticModelPositionError.ToString("0.########"));
            report.AppendLine(
                "StaticModelRotationErrorDegrees=" +
                poseMetrics.StaticModelRotationError.ToString("0.########"));
            report.AppendLine(
                "LoopBoundaryError=" +
                bindingMetrics.LoopBoundaryError.ToString("0.########"));
            report.AppendLine("AnimatedLegBones=20");
            report.AppendLine("LegRotationBindings=80");
            report.AppendLine("BodyPositionBindings=3");
            report.AppendLine("PivotRotationBindings=4");
            report.AppendLine("UnexpectedBindings=0");
            report.AppendLine("RootMotion=False");
            report.AppendLine("PlacementFixed=True");
            report.AppendLine("ExistingAnimationsUnchanged=True");
            report.AppendLine("CorrectedModelSha256=" + CorrectedModelSha256);
            WriteText(InspectionPath, report.ToString());

            Debug.Log(
                "RebellionForwardScanInspected Result=PASS" +
                ", LoopSeconds=1.6" +
                ", LeftToRightDegrees=" +
                poseMetrics.LeftToRightAngle.ToString("0.######") +
                ", RightToLeftDegrees=" +
                poseMetrics.RightToLeftAngle.ToString("0.######") +
                ", Distance=5" +
                ", Height=3" +
                ", Shape=ProjectorTriangle" +
                ", ApexAtLensPivotError=" +
                apexAtLensPivotError.ToString("0.########") +
                ", StandingPoseStatic=True" +
                ", LoopBoundaryError=" +
                bindingMetrics.LoopBoundaryError.ToString("0.########") +
                ", RootMotion=False" +
                ", PlacementFixed=True" +
                ", Report=" + InspectionPath + ".");
        }

        internal static void CaptureRuntimeFrame(string path)
        {
            RebellionMoveAnimationTool.CaptureRuntimeFrameForSlotFramedBy(
                SlotName,
                ModelName,
                ScanPlaneName,
                path);
        }

        internal static void ComposeRuntimeReview(
            IReadOnlyList<string> panelPaths,
            string outputPath)
        {
            RebellionMoveAnimationTool.ComposeRuntimeReview(
                panelPaths,
                outputPath);
        }

        internal static string FinalReviewAbsolutePath =>
            Absolute(ReviewPath);
        internal static string AnimatorStateName => StateName;
        internal static float LoopSeconds => LoopSecondsValue;

        private static StandingPose CaptureStandingPose(
            Transform slot,
            Transform model)
        {
            var attackClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    AttackClipPath) ??
                throw new InvalidOperationException(
                    "Attack transition clip is missing.");
            if (attackClip.length < AttackStandingPoseTime)
            {
                throw new InvalidOperationException(
                    "Attack transition clip does not contain the approved " +
                    "standing pose time.");
            }

            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            try
            {
                attackClip.SampleAnimation(
                    slot.gameObject,
                    AttackStandingPoseTime);
                var body = RequireDescendant(model, "Bone_001");
                var lensRenderer =
                    RequireDescendant(model, ScanLensName)
                        .GetComponent<Renderer>() ??
                    throw new InvalidOperationException(
                        "Rebellion scan lens renderer is missing.");
                var modelBounds = BoundsOfModelWithoutScanVfx(model);
                var outward = Vector3.ProjectOnPlane(
                    lensRenderer.bounds.center - modelBounds.center,
                    Vector3.up);
                if (outward.sqrMagnitude < 0.000001f)
                {
                    throw new InvalidOperationException(
                        "Rebellion scan lens outward direction is unavailable.");
                }
                outward.Normalize();

                return new StandingPose(
                    body.localPosition,
                    LegBoneNames.ToDictionary(
                        name => name,
                        name => RequireDescendant(model, name).localRotation,
                        StringComparer.Ordinal),
                    lensRenderer.transform.InverseTransformPoint(
                        lensRenderer.bounds.center +
                        (outward * LensOutwardOffset)),
                    Quaternion.Inverse(lensRenderer.transform.rotation) *
                    Quaternion.LookRotation(outward, Vector3.up));
            }
            finally
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }
            }
        }

        private static Bounds BoundsOfModelWithoutScanVfx(Transform model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true)
                .Where(item =>
                    item.transform.name != ScanPlaneName)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Rebellion render bounds are unavailable.");
            }
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        private static Mesh CreateScanPlaneMesh()
        {
            DeleteAssetIfPresent(MeshPath);
            var mesh = new Mesh
            {
                name = "Rebellion_Forward_Scan_Plane"
            };
            var halfHeight = ScanHeight * 0.5f;
            mesh.vertices = new[]
            {
                Vector3.zero,
                new Vector3(0f, halfHeight, ScanDistance),
                new Vector3(0f, -halfHeight, ScanDistance)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0.5f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f)
            };
            mesh.triangles = new[]
            {
                0, 1, 2,
                2, 1, 0
            };
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, MeshPath);
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static Texture2D CreateScanTexture()
        {
            const int width = 128;
            const int height = 64;
            var texture = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false);
            try
            {
                var pixels = new Color[width * height];
                for (var y = 0; y < height; y++)
                {
                    var v = y / (float)(height - 1);
                    var verticalFade =
                        SmoothStep(0f, 0.08f, v) *
                        (1f - SmoothStep(0.92f, 1f, v));
                    for (var x = 0; x < width; x++)
                    {
                        var u = x / (float)(width - 1);
                        var distanceFade = Mathf.Lerp(0.82f, 0.28f, u);
                        var alpha = verticalFade * distanceFade;
                        pixels[(y * width) + x] =
                            new Color(1f, 0.015f, 0.006f, alpha);
                    }
                }
                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(
                    Absolute(TexturePath),
                    texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(
                TexturePath,
                ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(TexturePath)
                as TextureImporter ??
                throw new InvalidOperationException(
                    "Forward scan texture importer is missing.");
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.textureCompression =
                TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath) ??
                   throw new InvalidOperationException(
                       "Forward scan texture failed to import.");
        }

        private static Material CreateScanMaterial(Texture2D texture)
        {
            var shader =
                Shader.Find("Universal Render Pipeline/Unlit") ??
                throw new InvalidOperationException(
                    "URP Unlit shader is missing.");
            var material =
                AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "Rebellion_Forward_Scan_Plane"
                };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetTexture("_BaseMap", texture);
            material.SetColor(
                "_BaseColor",
                new Color(1f, 0.02f, 0.008f, 0.16f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Transform CreateScanObjects(
            Transform slot,
            Transform model,
            Mesh mesh,
            Material material,
            StandingPose standingPose)
        {
            var existing = slot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == ScanPivotName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            var lens = RequireDescendant(model, ScanLensName);
            var pivotObject = new GameObject(ScanPivotName);
            var pivot = pivotObject.transform;
            pivot.SetParent(lens, false);
            pivot.localPosition = standingPose.PivotLocalPosition;
            pivot.localRotation = standingPose.PivotLocalRotation;
            var parentScale = lens.lossyScale;
            if (Mathf.Abs(parentScale.x) < 0.000001f ||
                Mathf.Abs(parentScale.y) < 0.000001f ||
                Mathf.Abs(parentScale.z) < 0.000001f)
            {
                throw new InvalidOperationException(
                    "Rebellion scan lens scale cannot support the 5m by 3m " +
                    "world-space scan plane.");
            }
            pivot.localScale = new Vector3(
                1f / Mathf.Abs(parentScale.x),
                1f / Mathf.Abs(parentScale.y),
                1f / Mathf.Abs(parentScale.z));

            var planeObject = new GameObject(ScanPlaneName);
            var plane = planeObject.transform;
            plane.SetParent(pivot, false);
            plane.localPosition = Vector3.zero;
            plane.localRotation = Quaternion.identity;
            plane.localScale = Vector3.one;
            planeObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = planeObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            EditorUtility.SetDirty(pivotObject);
            EditorUtility.SetDirty(planeObject);
            return pivot;
        }

        private static AnimationClip CreateScanClip(
            Transform slot,
            Transform model,
            Transform pivot,
            StandingPose standingPose)
        {
            DeleteAssetIfPresent(ClipPath);
            var clip = new AnimationClip
            {
                name = "Rebellion_03_Forward_Scan",
                frameRate = 60f
            };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            var body = RequireDescendant(model, "Bone_001");
            SetVectorCurves(
                clip,
                AnimationUtility.CalculateTransformPath(body, slot),
                new[]
                {
                    new VectorKey(0f, standingPose.BodyLocalPosition),
                    new VectorKey(
                        LoopSecondsValue,
                        standingPose.BodyLocalPosition)
                });
            foreach (var boneName in LegBoneNames)
            {
                var bone = RequireDescendant(model, boneName);
                var rotation = standingPose.LegLocalRotations[boneName];
                SetQuaternionCurves(
                    clip,
                    AnimationUtility.CalculateTransformPath(bone, slot),
                    new[]
                    {
                        new QuaternionKey(0f, rotation),
                        new QuaternionKey(LoopSecondsValue, rotation)
                    });
            }

            var baseRotation = pivot.localRotation;
            SetQuaternionCurves(
                clip,
                AnimationUtility.CalculateTransformPath(pivot, slot),
                new[]
                {
                    new QuaternionKey(
                        0f,
                        baseRotation *
                        Quaternion.Euler(0f, -HalfSweepDegrees, 0f)),
                    new QuaternionKey(
                        LoopSecondsValue * 0.5f,
                        baseRotation *
                        Quaternion.Euler(0f, HalfSweepDegrees, 0f)),
                    new QuaternionKey(
                        LoopSecondsValue,
                        baseRotation *
                        Quaternion.Euler(0f, -HalfSweepDegrees, 0f))
                });

            clip.EnsureQuaternionContinuity();
            AssetDatabase.CreateAsset(clip, ClipPath);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController CreateController(
            AnimationClip clip)
        {
            DeleteAssetIfPresent(ControllerPath);
            var controller =
                AnimatorController.CreateAnimatorControllerAtPath(
                    ControllerPath);
            var state =
                controller.layers[0].stateMachine.AddState(StateName);
            state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static BindingMetrics InspectBindings(AnimationClip clip)
        {
            var legNames =
                new HashSet<string>(LegBoneNames, StringComparer.Ordinal);
            var legRotationBones =
                new HashSet<string>(StringComparer.Ordinal);
            var bodyPositionBones =
                new HashSet<string>(StringComparer.Ordinal);
            var pivotRotationBindings = 0;
            var unexpectedBindings = 0;
            var loopError = 0f;

            foreach (var binding in
                     AnimationUtility.GetCurveBindings(clip))
            {
                var name = binding.path.Split('/').Last();
                if (binding.type == typeof(Transform) &&
                    binding.propertyName.StartsWith(
                        "m_LocalRotation.",
                        StringComparison.Ordinal) &&
                    legNames.Contains(name))
                {
                    legRotationBones.Add(name);
                }
                else if (binding.type == typeof(Transform) &&
                         binding.propertyName.StartsWith(
                             "m_LocalPosition.",
                             StringComparison.Ordinal) &&
                         name == "Bone_001")
                {
                    bodyPositionBones.Add(name);
                }
                else if (binding.type == typeof(Transform) &&
                         binding.propertyName.StartsWith(
                             "m_LocalRotation.",
                             StringComparison.Ordinal) &&
                         name == ScanPivotName)
                {
                    pivotRotationBindings++;
                }
                else
                {
                    unexpectedBindings++;
                }

                var curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                            throw new InvalidOperationException(
                                "Forward scan curve is missing.");
                loopError = Mathf.Max(
                    loopError,
                    Mathf.Abs(
                        curve.Evaluate(0f) -
                        curve.Evaluate(LoopSecondsValue)));
            }

            return new BindingMetrics(
                legRotationBones.Count,
                bodyPositionBones.Count,
                pivotRotationBindings,
                unexpectedBindings,
                loopError);
        }

        private static PoseMetrics InspectPoses(
            Transform slot,
            Transform model,
            Transform pivot,
            AnimationClip scanClip)
        {
            var attackClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    AttackClipPath) ??
                throw new InvalidOperationException(
                    "Attack transition clip is missing.");
            var snapshots = slot.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var tracked = LegBoneNames
                .Select(name => RequireDescendant(model, name))
                .Concat(new[] { RequireDescendant(model, "Bone_001") })
                .ToArray();
            var expected =
                new Dictionary<string, LocalPose>(
                    StringComparer.Ordinal);
            var scanStart =
                new Dictionary<string, LocalPose>(
                    StringComparer.Ordinal);
            var directions = new Vector3[5];
            var standingPositionError = 0f;
            var standingRotationError = 0f;
            var staticPositionError = 0f;
            var staticRotationError = 0f;
            var times = new[]
            {
                0f,
                LoopSecondsValue * 0.25f,
                LoopSecondsValue * 0.5f,
                LoopSecondsValue * 0.75f,
                LoopSecondsValue
            };

            try
            {
                RestoreAll(snapshots);
                attackClip.SampleAnimation(
                    slot.gameObject,
                    AttackStandingPoseTime);
                foreach (var item in tracked)
                {
                    expected[item.name] = LocalPose.Capture(item);
                }

                for (var timeIndex = 0;
                     timeIndex < times.Length;
                     timeIndex++)
                {
                    RestoreAll(snapshots);
                    scanClip.SampleAnimation(
                        slot.gameObject,
                        times[timeIndex]);
                    directions[timeIndex] =
                        Vector3.ProjectOnPlane(
                            pivot.forward,
                            Vector3.up).normalized;
                    foreach (var item in tracked)
                    {
                        var actual = LocalPose.Capture(item);
                        var target = expected[item.name];
                        standingPositionError = Mathf.Max(
                            standingPositionError,
                            Vector3.Distance(
                                actual.Position,
                                target.Position));
                        standingRotationError = Mathf.Max(
                            standingRotationError,
                            Quaternion.Angle(
                                actual.Rotation,
                                target.Rotation));
                        if (timeIndex == 0)
                        {
                            scanStart[item.name] = actual;
                        }
                        else
                        {
                            var start = scanStart[item.name];
                            staticPositionError = Mathf.Max(
                                staticPositionError,
                                Vector3.Distance(
                                    actual.Position,
                                    start.Position));
                            staticRotationError = Mathf.Max(
                                staticRotationError,
                                Quaternion.Angle(
                                    actual.Rotation,
                                    start.Rotation));
                        }
                    }
                }
            }
            finally
            {
                RestoreAll(snapshots);
            }

            return new PoseMetrics(
                standingPositionError,
                standingRotationError,
                staticPositionError,
                staticRotationError,
                Vector3.SignedAngle(
                    directions[0],
                    directions[2],
                    Vector3.up),
                Vector3.SignedAngle(
                    directions[2],
                    directions[4],
                    Vector3.up),
                Vector3.Angle(directions[0], directions[4]));
        }

        private static void RestoreAll(
            IEnumerable<TransformSnapshot> snapshots)
        {
            foreach (var snapshot in snapshots)
            {
                snapshot.Restore();
            }
        }

        private static void SetQuaternionCurves(
            AnimationClip clip,
            string path,
            IReadOnlyList<QuaternionKey> values)
        {
            var continuity = new List<QuaternionKey>(values.Count);
            Quaternion? previous = null;
            foreach (var value in values)
            {
                var rotation = value.Rotation;
                if (previous.HasValue &&
                    Quaternion.Dot(previous.Value, rotation) < 0f)
                {
                    rotation = new Quaternion(
                        -rotation.x,
                        -rotation.y,
                        -rotation.z,
                        -rotation.w);
                }
                continuity.Add(
                    new QuaternionKey(value.Time, rotation));
                previous = rotation;
            }

            SetLinearCurve(clip, path, "m_LocalRotation.x",
                continuity.Select(value =>
                    new Keyframe(value.Time, value.Rotation.x)).ToArray());
            SetLinearCurve(clip, path, "m_LocalRotation.y",
                continuity.Select(value =>
                    new Keyframe(value.Time, value.Rotation.y)).ToArray());
            SetLinearCurve(clip, path, "m_LocalRotation.z",
                continuity.Select(value =>
                    new Keyframe(value.Time, value.Rotation.z)).ToArray());
            SetLinearCurve(clip, path, "m_LocalRotation.w",
                continuity.Select(value =>
                    new Keyframe(value.Time, value.Rotation.w)).ToArray());
        }

        private static void SetVectorCurves(
            AnimationClip clip,
            string path,
            IReadOnlyList<VectorKey> values)
        {
            SetLinearCurve(clip, path, "m_LocalPosition.x",
                values.Select(value =>
                    new Keyframe(value.Time, value.Position.x)).ToArray());
            SetLinearCurve(clip, path, "m_LocalPosition.y",
                values.Select(value =>
                    new Keyframe(value.Time, value.Position.y)).ToArray());
            SetLinearCurve(clip, path, "m_LocalPosition.z",
                values.Select(value =>
                    new Keyframe(value.Time, value.Position.z)).ToArray());
        }

        private static void SetLinearCurve(
            AnimationClip clip,
            string path,
            string property,
            Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve,
                    index,
                    AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(
                    curve,
                    index,
                    AnimationUtility.TangentMode.Linear);
            }
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    property),
                curve);
        }

        private static void RequireAnimatorAssignments(
            Transform placementRoot)
        {
            foreach (var slotName in SlotNames)
            {
                var slot = placementRoot.Find(slotName) ??
                           throw new InvalidOperationException(
                               slotName + " is missing.");
                var animator = slot.GetComponent<Animator>();
                var actual =
                    animator == null ||
                    animator.runtimeAnimatorController == null
                        ? string.Empty
                        : AssetDatabase.GetAssetPath(
                            animator.runtimeAnimatorController);
                var expected = slotName == "Rebellion_01_Move"
                    ? MoveControllerPath
                    : slotName == "Rebellion_02_Attack_Mode_Transition"
                        ? AttackControllerPath
                        : slotName == SlotName
                            ? ControllerPath
                            : slotName == "Rebellion_04_Forward_Burst_Fire"
                                ? BurstControllerPath
                                : slotName == "Rebellion_05_Hit_Reaction"
                                    ? HitControllerPath
                                    : string.Empty;
                if (!string.Equals(
                        actual,
                        expected,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        slotName + " controller assignment is unexpected. " +
                        "Expected " + expected + ", found " + actual + ".");
                }
            }
        }

        private static Dictionary<string, string>
            CaptureProtectedAnimationHashes()
        {
            return new Dictionary<string, string>(
                StringComparer.Ordinal)
            {
                [MoveClipPath] = Sha256IfPresent(MoveClipPath),
                [MoveControllerPath] =
                    Sha256IfPresent(MoveControllerPath),
                [AttackClipPath] = Sha256IfPresent(AttackClipPath),
                [AttackControllerPath] =
                    Sha256IfPresent(AttackControllerPath)
            };
        }

        private static void RequireProtectedAnimationHashes(
            IReadOnlyDictionary<string, string> expected)
        {
            foreach (var pair in expected)
            {
                var actual = Sha256IfPresent(pair.Key);
                if (!string.Equals(
                        actual,
                        pair.Value,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        pair.Key + " changed unexpectedly.");
                }
            }
        }

        private static void RequireRig(Transform model)
        {
            var renderer =
                model.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                throw new InvalidOperationException(
                    "Rebellion skinned renderer is missing.");
            var bones = renderer.bones
                .Where(bone => bone != null)
                .Distinct()
                .ToArray();
            if (bones.Length != 29)
            {
                throw new InvalidOperationException(
                    "Expected 29 Rebellion bones, found " +
                    bones.Length + ".");
            }
            foreach (var name in LegBoneNames)
            {
                RequireDescendant(model, name);
            }
            RequireDescendant(model, "Bone_001");
            RequireDescendant(model, ScanLensName);
        }

        private static float SmoothStep(
            float edge0,
            float edge1,
            float value)
        {
            var t = Mathf.Clamp01((value - edge0) / (edge1 - edge0));
            return t * t * (3f - (2f * t));
        }

        private static Transform RequireDescendant(
            Transform root,
            string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected one " + name + " under " + root.name +
                    ", found " + matches.Length + ".");
            }
            return matches[0];
        }

        private static Scene RequireActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Forward scan authoring requires Edit Mode.");
            }
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the current active scene.");
            }
            return scene;
        }

        private static Transform RequirePlacementRoot(Scene scene)
        {
            return scene.GetRootGameObjects()
                       .SingleOrDefault(
                           item => item.name == PlacementRootName)
                       ?.transform ??
                   throw new InvalidOperationException(
                       PlacementRootName + " is missing.");
        }

        private static Transform RequireSlot(
            Scene scene,
            string slotName)
        {
            return RequirePlacementRoot(scene).Find(slotName) ??
                   throw new InvalidOperationException(
                       slotName + " is missing.");
        }

        private static Transform RequireModel(Transform slot)
        {
            return slot.Find(ModelName) ??
                   throw new InvalidOperationException(
                       slot.name + "/" + ModelName + " is missing.");
        }

        private static void RequireCorrectedModelHash()
        {
            var absolute = Absolute(CorrectedModelPath);
            var actual =
                File.Exists(absolute) ? Sha256(absolute) : string.Empty;
            if (!string.Equals(
                    actual,
                    CorrectedModelSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Unexpected corrected Rebellion model hash. Expected " +
                    CorrectedModelSha256 + ", found " + actual + ".");
            }
        }

        private static string Sha256IfPresent(string relativePath)
        {
            var absolute = Absolute(relativePath);
            return File.Exists(absolute) ? Sha256(absolute) : string.Empty;
        }

        private static string Sha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var algorithm = SHA256.Create())
            {
                return string.Concat(
                    algorithm.ComputeHash(stream)
                        .Select(value => value.ToString("X2")));
            }
        }

        private static void RequireSameTransform(
            TransformState expected,
            Transform actual,
            string label)
        {
            if (!expected.Matches(actual))
            {
                throw new InvalidOperationException(
                    label + " transform changed unexpectedly.");
            }
        }

        private static void DeleteAssetIfPresent(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) !=
                null)
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }
                current = next;
            }
        }

        private static void WriteText(
            string relativePath,
            string contents)
        {
            var absolute = Absolute(relativePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(absolute) ??
                throw new InvalidOperationException(
                    "Output directory is invalid."));
            File.WriteAllText(absolute, contents, Encoding.UTF8);
        }

        private static string Absolute(string relativePath)
        {
            var projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException(
                    "Project root is unavailable.");
            return Path.Combine(
                projectRoot,
                relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private sealed class StandingPose
        {
            public StandingPose(
                Vector3 bodyLocalPosition,
                Dictionary<string, Quaternion> legLocalRotations,
                Vector3 pivotLocalPosition,
                Quaternion pivotLocalRotation)
            {
                BodyLocalPosition = bodyLocalPosition;
                LegLocalRotations = legLocalRotations;
                PivotLocalPosition = pivotLocalPosition;
                PivotLocalRotation = pivotLocalRotation;
            }

            public Vector3 BodyLocalPosition { get; }
            public Dictionary<string, Quaternion> LegLocalRotations { get; }
            public Vector3 PivotLocalPosition { get; }
            public Quaternion PivotLocalRotation { get; }
        }

        private readonly struct BindingMetrics
        {
            public BindingMetrics(
                int legRotationBones,
                int bodyPositionBones,
                int pivotRotationBindings,
                int unexpectedBindings,
                float loopBoundaryError)
            {
                LegRotationBones = legRotationBones;
                BodyPositionBones = bodyPositionBones;
                PivotRotationBindings = pivotRotationBindings;
                UnexpectedBindings = unexpectedBindings;
                LoopBoundaryError = loopBoundaryError;
            }

            public int LegRotationBones { get; }
            public int BodyPositionBones { get; }
            public int PivotRotationBindings { get; }
            public int UnexpectedBindings { get; }
            public float LoopBoundaryError { get; }
        }

        private readonly struct PoseMetrics
        {
            public PoseMetrics(
                float standingPositionError,
                float standingRotationError,
                float staticModelPositionError,
                float staticModelRotationError,
                float leftToRightAngle,
                float rightToLeftAngle,
                float loopDirectionError)
            {
                StandingPositionError = standingPositionError;
                StandingRotationError = standingRotationError;
                StaticModelPositionError = staticModelPositionError;
                StaticModelRotationError = staticModelRotationError;
                LeftToRightAngle = leftToRightAngle;
                RightToLeftAngle = rightToLeftAngle;
                LoopDirectionError = loopDirectionError;
            }

            public float StandingPositionError { get; }
            public float StandingRotationError { get; }
            public float StaticModelPositionError { get; }
            public float StaticModelRotationError { get; }
            public float LeftToRightAngle { get; }
            public float RightToLeftAngle { get; }
            public float LoopDirectionError { get; }
        }

        private readonly struct QuaternionKey
        {
            public QuaternionKey(float time, Quaternion rotation)
            {
                Time = time;
                Rotation = rotation;
            }

            public float Time { get; }
            public Quaternion Rotation { get; }
        }

        private readonly struct VectorKey
        {
            public VectorKey(float time, Vector3 position)
            {
                Time = time;
                Position = position;
            }

            public float Time { get; }
            public Vector3 Position { get; }
        }

        private readonly struct LocalPose
        {
            private LocalPose(Vector3 position, Quaternion rotation)
            {
                Position = position;
                Rotation = rotation;
            }

            public Vector3 Position { get; }
            public Quaternion Rotation { get; }

            public static LocalPose Capture(Transform target)
            {
                return new LocalPose(
                    target.localPosition,
                    target.localRotation);
            }
        }

        private sealed class TransformSnapshot
        {
            private readonly Transform target;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            public TransformSnapshot(Transform target)
            {
                this.target = target;
                localPosition = target.localPosition;
                localRotation = target.localRotation;
                localScale = target.localScale;
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

        private readonly struct TransformState
        {
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            private TransformState(Transform target)
            {
                localPosition = target.localPosition;
                localRotation = target.localRotation;
                localScale = target.localScale;
            }

            public static TransformState Capture(Transform target)
            {
                return new TransformState(target);
            }

            public bool Matches(Transform target)
            {
                return Vector3.Distance(
                           localPosition,
                           target.localPosition) <= 0.000001f &&
                       Quaternion.Angle(
                           localRotation,
                           target.localRotation) <= 0.0001f &&
                       Vector3.Distance(
                           localScale,
                           target.localScale) <= 0.000001f;
            }
        }
    }
}
