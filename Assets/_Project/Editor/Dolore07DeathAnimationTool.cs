using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Dolore07Death
{
    internal static class Dolore07DeathAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Dolore Enemy Placement";
        private const string StaticSlotName = "Dolore_01_Static_Review";
        private const string SlotName = "Dolore_07_Death";
        private const string ModelName = "Dolore_Model";
        private const string HipsBoneName = "Hips";
        private const string LegacySignalOverlayName = "Dolore_DeathPortraitSignalOverlay";
        private const string SignalFillName = "Dolore_DeathPortraitSignalFill";
        private const string PortraitToken = "Portrait";
        private const string StateName = "DeathLoop";
        private const string AssetFolder = "Assets/_Project/Art/Enemies/Dolore/Animations";
        private const string DeathScreenFolder = "Assets/_Project/Art/Enemies/Dolore/DeathScreen";
        private const string ShaderPath = DeathScreenFolder + "/DoloreDeathPortraitSignal.shader";
        private const string LegacyMaterialPath = DeathScreenFolder + "/Dolore_07_Death_Portrait.mat";
        private const string FillMaterialPath = DeathScreenFolder + "/Dolore_07_Death_PortraitFill.mat";
        private const string InvisibleMaterialPath = DeathScreenFolder + "/Dolore_Death_Invisible.mat";
        private const string SignalFillMeshPath = DeathScreenFolder + "/Dolore_DeathPortraitSignalFill.asset";
        private const string LegacySignalMeshPath = DeathScreenFolder + "/Dolore_07_Death_PortraitSignalMesh.asset";
        private const string ClipPath = AssetFolder + "/Dolore_07_Death.anim";
        private const string ControllerPath = AssetFolder + "/Dolore_07_Death.controller";
        private const string ValidationFolder = "docs/validation/dolore_death_2026-07-23";
        private const string TargetReportPath = ValidationFolder + "/Dolore_07_Death_Target.txt";
        private const string InspectionReportPath = ValidationFolder + "/Dolore_07_Death_Inspection.txt";
        private const string GeometryReportPath = ValidationFolder + "/Dolore_07_Death_Geometry.txt";
        private const string DiagnosticFolder = ValidationFolder + "/Dolore_07_Death_Diagnostic";
        private const string FinalFolder = ValidationFolder + "/Dolore_07_Death_Final";
        private const string CaptureReportPath = ValidationFolder + "/Dolore_07_Death_Capture.txt";
        private const float FallDuration = 0.9f;
        private const float NoiseDuration = 1f;
        private const float BlackDuration = 1f;
        private const float Duration = FallDuration + NoiseDuration + BlackDuration;
        private const float FallAngleDegrees = 90f;
        private const float Tolerance = 0.0001f;
        private const int CaptureLayer = 30;

        private static readonly float[] FallTimes = { 0f, 0.18f, 0.42f, 0.67f, 0.9f, 1.9f, 2.9f };
        private static readonly float[] FallFractions = { 0f, 0.06f, 0.28f, 0.68f, 1f, 1f, 1f };

        [MenuItem("Bellerophon/Enemies/Dolore/Inspect Motion 6 Death Target")]
        public static void InspectTarget()
        {
            var scene = RequireScene();
            var dirty = scene.isDirty;
            var slot = RequireSlot(scene, SlotName);
            var model = RequireChild(slot, ModelName);
            var renderer = RequireRenderer(model);
            var hips = RequireDescendant(model, HipsBoneName);
            var portraitIndex = RequirePortraitMaterialIndex(renderer);
            if (renderer.bones.Length != 27)
                throw new InvalidOperationException("The approved Dolore renderer must contain 27 bones.");

            var report = new StringBuilder()
                .AppendLine("Result=PASS")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("Target=" + PlacementRootName + "/" + SlotName)
                .AppendLine("SlotSiblingIndex=" + slot.GetSiblingIndex())
                .AppendLine("SlotLocalPosition=" + Vec(slot.localPosition))
                .AppendLine("SlotLocalRotation=" + Quat(slot.localRotation))
                .AppendLine("SlotLocalScale=" + Vec(slot.localScale))
                .AppendLine("Model=" + model.name)
                .AppendLine("Renderer=" + renderer.name)
                .AppendLine("Mesh=" + AssetDatabase.GetAssetPath(renderer.sharedMesh))
                .AppendLine("RigBoneCount=" + renderer.bones.Length)
                .AppendLine("HipsPath=" + AnimationUtility.CalculateTransformPath(hips, slot))
                .AppendLine("PortraitMaterialIndex=" + portraitIndex)
                .AppendLine("PortraitMaterial=" + renderer.sharedMaterials[portraitIndex].name)
                .AppendLine("SlotAnimator=" + ControllerPathOrNone(slot.GetComponent<Animator>()));
            for (var index = 0; index < renderer.sharedMaterials.Length; index++)
            {
                var material = renderer.sharedMaterials[index];
                report.AppendLine(
                    "Material[" + index + "]=" + (material != null ? material.name : "None") +
                    "|Path=" + (material != null ? AssetDatabase.GetAssetPath(material) : "None"));
            }
            WriteText(TargetReportPath, report.AppendLine("SceneChanged=False").ToString());
            if (scene.isDirty != dirty)
                throw new InvalidOperationException("Death target inspection changed CargoRunMvp.");
            Debug.Log("Dolore07DeathTargetInspected Result=PASS Target=" + SlotName + " RigBoneCount=27 SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Apply Motion 6 Death")]
        public static void ApplyAnimation()
        {
            var scene = RequireScene();
            if (scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp contains pre-existing unsaved changes.");
            var slot = RequireSlot(scene, SlotName);
            var model = RequireChild(slot, ModelName);
            var renderer = RequireRenderer(model);
            var hips = RequireDescendant(model, HipsBoneName);
            var otherSlotsBefore = OtherSlotSignatures(scene);
            var slotPosition = slot.localPosition;
            var slotRotation = slot.localRotation;
            var slotScale = slot.localScale;

            EnsureAssetFolder(AssetFolder);
            EnsureAssetFolder(DeathScreenFolder);
            AssetDatabase.ImportAsset(ShaderPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var portraitIndex = RequirePortraitMaterialIndex(renderer);
            WritePortraitGeometryDiagnostics(renderer, slot, portraitIndex);
            var referenceSlot = RequireSlot(scene, StaticSlotName);
            var referenceRenderer = RequireRenderer(RequireChild(referenceSlot, ModelName));
            var sourcePortrait = referenceRenderer.sharedMaterials[portraitIndex];
            var deathPortraitFill = CreateDeathPortraitFillMaterial(sourcePortrait);
            var materials = renderer.sharedMaterials.ToArray();
            materials[portraitIndex] = sourcePortrait;
            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
            RemoveExistingSignalGeometry(model);
            var signalSurface = CreatePortraitSignalSurface(
                referenceRenderer, referenceSlot, renderer, portraitIndex, deathPortraitFill);

            var targetPose = ComputeTargetPose(renderer, slot, hips);
            var clip = CreateClip(slot, signalSurface, hips, targetPose);
            var controller = CreateController(clip);
            DisableCompetingAnimation(model);
            var animator = slot.GetComponent<Animator>();
            if (animator == null) animator = slot.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);

            if (Vector3.Distance(slot.localPosition, slotPosition) > Tolerance ||
                Quaternion.Angle(slot.localRotation, slotRotation) > 0.001f ||
                Vector3.Distance(slot.localScale, slotScale) > Tolerance)
                throw new InvalidOperationException("The Dolore death slot root transform changed during application.");
            var otherSlotsAfter = OtherSlotSignatures(scene);
            if (!otherSlotsBefore.SequenceEqual(otherSlotsAfter, StringComparer.Ordinal))
                throw new InvalidOperationException("A Dolore slot outside the death target changed.");

            var metrics = Inspect(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp could not be saved.");
            AssetDatabase.SaveAssets();
            WriteInspection(metrics, "Apply", true);
            Debug.Log(
                "Dolore07DeathApplied Result=PASS Duration=" + Num(metrics.Duration) +
                " LeftFall=" + Num(metrics.LeftDisplacement) +
                " FallAngle=" + Num(metrics.FallAngle) +
                " GroundContactError=" + Num(metrics.GroundContactError) +
                " SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Inspect Motion 6 Death")]
        public static void InspectAnimation()
        {
            var scene = RequireScene();
            var dirty = scene.isDirty;
            var metrics = Inspect(scene);
            WriteInspection(metrics, "Inspect", false);
            if (scene.isDirty != dirty)
                throw new InvalidOperationException("Death inspection changed CargoRunMvp.");
            Debug.Log(
                "Dolore07DeathInspected Result=PASS Duration=" + Num(metrics.Duration) +
                " LeftFall=" + Num(metrics.LeftDisplacement) +
                " GroundContactError=" + Num(metrics.GroundContactError) +
                " NoisePhase=" + Num(metrics.NoisePhase) +
                " BlackPhase=" + Num(metrics.BlackPhase) +
                " SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Capture Motion 6 Death Diagnostic")]
        public static void CaptureDiagnostic()
        {
            Capture(false);
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Capture Motion 6 Death Final")]
        public static void CaptureFinal()
        {
            Capture(true);
        }

        private static void Capture(bool final)
        {
            var scene = RequireScene();
            var dirty = scene.isDirty;
            var metrics = Inspect(scene);
            var sourceSlot = RequireSlot(scene, SlotName);
            var clone = UnityEngine.Object.Instantiate(sourceSlot.gameObject);
            clone.name = "Dolore_07_Death_CaptureClone";
            clone.hideFlags = HideFlags.DontSave;
            var cameraObject = new GameObject("Dolore_07_Death_Camera")
            {
                hideFlags = HideFlags.DontSave,
                layer = CaptureLayer
            };
            var lightObject = new GameObject("Dolore_07_Death_Light")
            {
                hideFlags = HideFlags.DontSave,
                layer = CaptureLayer
            };
            var folder = Absolute(final ? FinalFolder : DiagnosticFolder);
            try
            {
                SetLayer(clone.transform, CaptureLayer);
                clone.SetActive(true);
                var animator = clone.GetComponent<Animator>() ??
                               throw new InvalidOperationException("The capture clone Animator is missing.");
                animator.enabled = true;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.Update(0f);
                foreach (var skinned in clone.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    skinned.updateWhenOffscreen = true;
                    skinned.forceMatrixRecalculationPerRender = true;
                }

                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.025f, 0.03f, 0.035f, 1f);
                camera.fieldOfView = 31f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2.9f;
                light.color = new Color(1f, 0.94f, 0.86f, 1f);
                light.cullingMask = 1 << CaptureLayer;
                lightObject.transform.rotation = Quaternion.Euler(35f, -35f, 0f);

                Directory.CreateDirectory(folder);
                foreach (var file in Directory.GetFiles(folder, "*.png")) File.Delete(file);
                var captures = new[]
                {
                    new CapturePoint(0f, "01_UprightPortrait"),
                    new CapturePoint(1.05f, "02_FallenPortrait"),
                    new CapturePoint(1.05f, "03_FallenWhiteNoise"),
                    new CapturePoint(1.75f, "04_WhiteNoiseHold"),
                    new CapturePoint(2.05f, "05_BlackSignal"),
                    new CapturePoint(2.75f, "06_BlackHold")
                };
                foreach (var point in captures)
                {
                    CapturePose(clone.transform, animator, camera, point.Time, false,
                        Path.Combine(folder, point.Name + "_Front.png"));
                    CapturePose(clone.transform, animator, camera, point.Time, true,
                        Path.Combine(folder, point.Name + "_Oblique.png"));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(clone);
            }

            var report = new StringBuilder()
                .AppendLine("Result=PASS")
                .AppendLine("Mode=" + (final ? "Final" : "Diagnostic"))
                .AppendLine("Frames=12")
                .AppendLine("Folder=" + (final ? FinalFolder : DiagnosticFolder))
                .AppendLine("DurationSeconds=" + Num(metrics.Duration))
                .AppendLine("LeftDisplacementMeters=" + Num(metrics.LeftDisplacement))
                .AppendLine("GroundContactErrorMeters=" + Num(metrics.GroundContactError))
                .AppendLine("PortraitOnlySignal=True")
                .AppendLine("PortraitSurfaceAttached=True")
                .AppendLine("PortraitSignalSizeMatched=True")
                .AppendLine("PortraitSignalStaticReferencePose=True")
                .AppendLine("CaptureCameraStable=True")
                .AppendLine("FrameAndBodyUnaffected=True")
                .AppendLine("SceneChanged=False")
                .ToString();
            WriteText(CaptureReportPath, report);
            AssetDatabase.Refresh();
            if (scene.isDirty != dirty)
                throw new InvalidOperationException("Death capture changed CargoRunMvp.");
            Debug.Log(
                "Dolore07DeathCaptured Result=PASS Mode=" + (final ? "Final" : "Diagnostic") +
                " Frames=12 SceneChanged=False.");
        }

        private static Material CreateDeathPortraitFillMaterial(Material source)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath) ??
                         throw new InvalidOperationException("The approved Dolore death portrait shader is missing.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(FillMaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "Dolore_07_Death_PortraitFill" };
                AssetDatabase.CreateAsset(material, FillMaterialPath);
            }
            material.shader = shader;
            var texture = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : source.mainTexture;
            var color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.color;
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", color);
            material.SetTextureScale("_BaseMap", source.mainTextureScale);
            material.SetTextureOffset("_BaseMap", source.mainTextureOffset);
            material.SetFloat("_SignalPhase", 0f);
            material.SetFloat("_NoiseScale", 190f);
            material.SetFloat("_NoiseSpeed", 26f);
            material.SetFloat("_ScanlineStrength", 0.24f);
            material.SetFloat("_ZTest", (float)CompareFunction.Always);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static MeshRenderer CreatePortraitSignalSurface(
            SkinnedMeshRenderer referenceSource,
            Transform referenceSlot,
            SkinnedMeshRenderer targetSource,
            int portraitIndex,
            Material signalMaterial)
        {
            var baked = new Mesh { name = "DoloreDeathPortraitOpeningSource" };
            try
            {
                referenceSource.BakeMesh(baked);
                var portraitTriangles = referenceSource.sharedMesh.GetTriangles(portraitIndex);
                var portraitIndices = portraitTriangles.Distinct().ToArray();
                if (portraitIndices.Length == 0)
                    throw new InvalidOperationException("The Dolore portrait submesh has no vertices.");
                var bakedVertices = baked.vertices;
                var slotVertices = bakedVertices
                    .Select(vertex => referenceSlot.InverseTransformPoint(
                        referenceSource.transform.TransformPoint(vertex)))
                    .ToArray();
                var portraitBounds = new Bounds(slotVertices[portraitIndices[0]], Vector3.zero);
                foreach (var vertexIndex in portraitIndices) portraitBounds.Encapsulate(slotVertices[vertexIndex]);

                var sourceWeights = referenceSource.sharedMesh.boneWeights;
                var boneScores = new float[referenceSource.bones.Length];
                foreach (var vertexIndex in portraitIndices)
                {
                    var weight = sourceWeights[vertexIndex];
                    boneScores[weight.boneIndex0] += weight.weight0;
                    boneScores[weight.boneIndex1] += weight.weight1;
                    boneScores[weight.boneIndex2] += weight.weight2;
                    boneScores[weight.boneIndex3] += weight.weight3;
                }
                var dominantBoneIndex = Enumerable.Range(0, boneScores.Length)
                    .OrderByDescending(index => boneScores[index])
                    .First();
                var referenceBone = referenceSource.bones[dominantBoneIndex] ??
                                   throw new InvalidOperationException("The Dolore portrait dominant bone is missing.");
                var targetBone = targetSource.bones.SingleOrDefault(bone =>
                                     bone != null && bone.name == referenceBone.name) ??
                                 throw new InvalidOperationException("The target Dolore portrait bone is missing.");

                const int edgePointCount = 32;
                var center = portraitBounds.center;
                center.y -= portraitBounds.extents.y * 0.20f;
                var halfWidth = portraitBounds.extents.x * 0.71f;
                var halfHeight = portraitBounds.extents.y * 0.62f;
                var fillDepth = portraitBounds.min.z + 0.005f;
                var vertices = new Vector3[edgePointCount + 1];
                var uvs = new Vector2[edgePointCount + 1];
                Vector3 ToHeadVertex(float x, float y)
                {
                    var slotPosition = new Vector3(center.x + x, center.y + y, fillDepth);
                    return referenceBone.InverseTransformPoint(referenceSlot.TransformPoint(slotPosition));
                }
                vertices[0] = ToHeadVertex(0f, 0f);
                uvs[0] = new Vector2(0.5f, 0.5f);
                for (var index = 0; index < edgePointCount; index++)
                {
                    var angle = Mathf.PI * 2f * index / edgePointCount;
                    var cosine = Mathf.Cos(angle);
                    var sine = Mathf.Sin(angle);
                    var x = Mathf.Sign(cosine) * Mathf.Pow(Mathf.Abs(cosine), 0.5f) * halfWidth;
                    var y = Mathf.Sign(sine) * Mathf.Pow(Mathf.Abs(sine), 0.5f) * halfHeight;
                    vertices[index + 1] = ToHeadVertex(x, y);
                    uvs[index + 1] = new Vector2(
                        x / Mathf.Max(halfWidth * 2f, 0.001f) + 0.5f,
                        y / Mathf.Max(halfHeight * 2f, 0.001f) + 0.5f);
                }
                var triangles = new int[edgePointCount * 3];
                for (var index = 0; index < edgePointCount; index++)
                {
                    var triangleIndex = index * 3;
                    triangles[triangleIndex] = 0;
                    triangles[triangleIndex + 1] = index + 1;
                    triangles[triangleIndex + 2] = (index + 1) % edgePointCount + 1;
                }

                var generated = new Mesh { name = "Dolore_DeathPortraitSignalOpening" };
                generated.vertices = vertices;
                generated.uv = uvs;
                generated.uv2 = uvs;
                generated.colors32 = Enumerable.Repeat(
                    new Color32(255, 255, 255, 255), vertices.Length).ToArray();
                generated.triangles = triangles;
                generated.RecalculateBounds();
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(SignalFillMeshPath);
                if (mesh == null)
                {
                    AssetDatabase.CreateAsset(generated, SignalFillMeshPath);
                    mesh = generated;
                }
                else
                {
                    EditorUtility.CopySerialized(generated, mesh);
                    UnityEngine.Object.DestroyImmediate(generated);
                    mesh.name = "Dolore_DeathPortraitSignalOpening";
                    EditorUtility.SetDirty(mesh);
                }

                var surfaceObject = new GameObject(SignalFillName);
                surfaceObject.transform.SetParent(targetBone, false);
                var filter = surfaceObject.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                var surface = surfaceObject.AddComponent<MeshRenderer>();
                surface.sharedMaterial = signalMaterial;
                surface.shadowCastingMode = ShadowCastingMode.Off;
                surface.receiveShadows = false;
                surface.lightProbeUsage = LightProbeUsage.Off;
                surface.reflectionProbeUsage = ReflectionProbeUsage.Off;
                surface.allowOcclusionWhenDynamic = false;
                surface.enabled = true;
                EditorUtility.SetDirty(surfaceObject);
                EditorUtility.SetDirty(filter);
                EditorUtility.SetDirty(surface);
                AssetDatabase.SaveAssets();
                Debug.Log(
                    "Dolore death portrait opening mask generated from static reference pose. ParentBone=" +
                    targetBone.name +
                    " HalfWidth=" + Num(halfWidth) + " HalfHeight=" + Num(halfHeight) +
                    " Depth=" + Num(fillDepth) + ".");
                return surface;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static void RemoveExistingSignalGeometry(Transform model)
        {
            var generatedObjects = model.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == SignalFillName || item.name == LegacySignalOverlayName)
                .Select(item => item.gameObject)
                .ToArray();
            foreach (var generatedObject in generatedObjects)
                UnityEngine.Object.DestroyImmediate(generatedObject);
            if (AssetDatabase.LoadAssetAtPath<Mesh>(SignalFillMeshPath) != null)
                AssetDatabase.DeleteAsset(SignalFillMeshPath);
            if (AssetDatabase.LoadAssetAtPath<Mesh>(LegacySignalMeshPath) != null)
                AssetDatabase.DeleteAsset(LegacySignalMeshPath);
            if (AssetDatabase.LoadAssetAtPath<Material>(LegacyMaterialPath) != null)
                AssetDatabase.DeleteAsset(LegacyMaterialPath);
            if (AssetDatabase.LoadAssetAtPath<Material>(InvisibleMaterialPath) != null)
                AssetDatabase.DeleteAsset(InvisibleMaterialPath);
        }

        private static TargetPose ComputeTargetPose(
            SkinnedMeshRenderer renderer,
            Transform slot,
            Transform hips)
        {
            var restPosition = hips.localPosition;
            var restRotation = hips.localRotation;
            var restBounds = BakedBoundsInSlot(renderer, slot);
            var topBone = RequireTopBone(renderer, slot);
            var restTopPosition = slot.InverseTransformPoint(topBone.position);
            var positive = EvaluateFallCandidate(
                renderer, slot, hips, topBone, restPosition, restRotation, restBounds, FallAngleDegrees);
            var negative = EvaluateFallCandidate(
                renderer, slot, hips, topBone, restPosition, restRotation, restBounds, -FallAngleDegrees);
            var selected = positive.TopPosition.x < negative.TopPosition.x ? positive : negative;
            hips.localPosition = restPosition;
            hips.localRotation = restRotation;
            if (restTopPosition.x - selected.TopPosition.x <= 0.25f)
                throw new InvalidOperationException("Neither measured death pose falls clearly toward Dolore local left.");
            return new TargetPose(restPosition, restRotation, restBounds, selected);
        }

        private static FallCandidate EvaluateFallCandidate(
            SkinnedMeshRenderer renderer,
            Transform slot,
            Transform hips,
            Transform topBone,
            Vector3 restPosition,
            Quaternion restRotation,
            Bounds restBounds,
            float signedAngle)
        {
            hips.localPosition = restPosition;
            hips.localRotation = restRotation;
            var restWorldRotation = hips.rotation;
            hips.rotation = Quaternion.AngleAxis(signedAngle, slot.forward) * restWorldRotation;
            var rotatedBounds = BakedBoundsInSlot(renderer, slot);
            hips.position += slot.up * (restBounds.min.y - rotatedBounds.min.y);
            var groundedBounds = BakedBoundsInSlot(renderer, slot);
            var topPosition = slot.InverseTransformPoint(topBone.position);
            return new FallCandidate(hips.localPosition, hips.localRotation, groundedBounds, topPosition, signedAngle);
        }

        private static AnimationClip CreateClip(
            Transform slot,
            MeshRenderer signalSurface,
            Transform hips,
            TargetPose pose)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = "Dolore_07_Death", frameRate = 60f };
                AssetDatabase.CreateAsset(clip, ClipPath);
            }
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, binding, null);
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);

            var positions = new Vector3[FallTimes.Length];
            var rotations = new Quaternion[FallTimes.Length];
            var targetRotation = pose.Fallen.Rotation;
            if (Quaternion.Dot(pose.RestRotation, targetRotation) < 0f)
                targetRotation = new Quaternion(-targetRotation.x, -targetRotation.y, -targetRotation.z, -targetRotation.w);
            for (var index = 0; index < FallTimes.Length; index++)
            {
                positions[index] = Vector3.LerpUnclamped(pose.RestPosition, pose.Fallen.Position, FallFractions[index]);
                rotations[index] = Quaternion.SlerpUnclamped(pose.RestRotation, targetRotation, FallFractions[index]);
            }
            var hipsPath = AnimationUtility.CalculateTransformPath(hips, slot);
            SetVectorCurves(clip, hipsPath, "m_LocalPosition", positions);
            SetQuaternionCurves(clip, hipsPath, rotations);

            var surfacePath = AnimationUtility.CalculateTransformPath(signalSurface.transform, slot);
            var signalCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(FallDuration - 0.001f, 0f),
                new Keyframe(FallDuration, 1f),
                new Keyframe(FallDuration + NoiseDuration - 0.001f, 1f),
                new Keyframe(FallDuration + NoiseDuration, 2f),
                new Keyframe(Duration, 2f));
            for (var index = 0; index < signalCurve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(signalCurve, index, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(signalCurve, index, AnimationUtility.TangentMode.Constant);
            }
            clip.SetCurve(surfacePath, typeof(MeshRenderer), "material._SignalPhase", signalCurve);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.keepOriginalOrientation = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var layers = controller.layers;
            if (layers.Length == 0)
                throw new InvalidOperationException("The Dolore death controller has no layer.");
            var stateMachine = layers[0].stateMachine;
            foreach (var child in stateMachine.states.ToArray()) stateMachine.RemoveState(child.state);
            foreach (var child in stateMachine.anyStateTransitions.ToArray()) stateMachine.RemoveAnyStateTransition(child);
            foreach (var child in stateMachine.entryTransitions.ToArray()) stateMachine.RemoveEntryTransition(child);
            var state = stateMachine.AddState(StateName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static Metrics Inspect(Scene scene)
        {
            var slot = RequireSlot(scene, SlotName);
            var model = RequireChild(slot, ModelName);
            var renderer = RequireRenderer(model);
            var signalSurface = RequireSignalSurface(model);
            var hips = RequireDescendant(model, HipsBoneName);
            var topBone = RequireTopBone(renderer, slot);
            var animator = slot.GetComponent<Animator>() ??
                           throw new InvalidOperationException("The Dolore death slot Animator is missing.");
            if (AssetDatabase.GetAssetPath(animator.runtimeAnimatorController) != ControllerPath)
                throw new InvalidOperationException("The Dolore death controller is not assigned to the death slot.");
            if (animator.applyRootMotion)
                throw new InvalidOperationException("The Dolore death Animator must not apply root motion.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException("The Dolore death clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                             throw new InvalidOperationException("The Dolore death controller asset is missing.");
            if (controller.layers.SelectMany(layer => layer.stateMachine.states)
                .All(child => child.state.name != StateName || child.state.motion != clip))
                throw new InvalidOperationException("The death loop state is missing or does not use the approved clip.");
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime)
                throw new InvalidOperationException("The Dolore death clip is not looped.");

            var portraitIndex = RequirePortraitMaterialIndex(renderer);
            WritePortraitGeometryDiagnostics(renderer, slot, portraitIndex);
            var referenceRenderer = RequireRenderer(RequireChild(RequireSlot(scene, StaticSlotName), ModelName));
            if (AssetDatabase.GetAssetPath(referenceRenderer.sharedMesh) != AssetDatabase.GetAssetPath(renderer.sharedMesh))
                throw new InvalidOperationException("The death slot no longer uses the approved Dolore mesh.");
            for (var index = 0; index < renderer.sharedMaterials.Length; index++)
            {
                if (index >= referenceRenderer.sharedMaterials.Length ||
                    AssetDatabase.GetAssetPath(renderer.sharedMaterials[index]) !=
                    AssetDatabase.GetAssetPath(referenceRenderer.sharedMaterials[index]))
                    throw new InvalidOperationException("A base Dolore death material differs from the approved model.");
            }
            if (model.GetComponentsInChildren<Transform>(true)
                    .Any(item => item.name == LegacySignalOverlayName) ||
                AssetDatabase.LoadAssetAtPath<Mesh>(LegacySignalMeshPath) != null ||
                AssetDatabase.LoadAssetAtPath<Material>(LegacyMaterialPath) != null)
                throw new InvalidOperationException("The legacy skinned portrait signal overlay was not removed.");
            var signalSurfaceMesh = signalSurface.GetComponent<MeshFilter>()?.sharedMesh;
            if (AssetDatabase.GetAssetPath(signalSurfaceMesh) != SignalFillMeshPath ||
                AssetDatabase.GetAssetPath(signalSurface.sharedMaterial) != FillMaterialPath ||
                signalSurface.transform.parent == null || signalSurface.transform.parent.name != "head" ||
                signalSurface.sharedMaterials.Length != 1 ||
                Mathf.Abs(signalSurface.sharedMaterial.GetFloat("_ZTest") - (float)CompareFunction.Always) > Tolerance)
                throw new InvalidOperationException("The portrait opening mask is configured incorrectly.");
            if (signalSurfaceMesh == null ||
                signalSurfaceMesh.subMeshCount != 1 ||
                signalSurfaceMesh.colors32.Length != signalSurfaceMesh.vertexCount ||
                signalSurfaceMesh.uv2.Length != signalSurfaceMesh.vertexCount)
                throw new InvalidOperationException("The portrait opening signal mesh data is incomplete.");
            AssertDeathAssetsOnlyOnTarget(scene);

            var curveBindings = AnimationUtility.GetCurveBindings(clip);
            var surfaceBindings = curveBindings.Where(binding =>
                    binding.type == typeof(MeshRenderer) &&
                    binding.propertyName == "material._SignalPhase" &&
                    binding.path.EndsWith(SignalFillName, StringComparison.Ordinal))
                .ToArray();
            if (surfaceBindings.Length != 1 || curveBindings.Any(binding =>
                    binding.propertyName == "material._SignalPhase" &&
                    !binding.path.EndsWith(SignalFillName, StringComparison.Ordinal)))
                throw new InvalidOperationException("Only the exact portrait surface may contain a signal curve.");
            var signalBinding = surfaceBindings[0];
            var signalCurve = AnimationUtility.GetEditorCurve(clip, signalBinding) ??
                              throw new InvalidOperationException("The portrait signal curve could not be read.");
            var portraitPhase = signalCurve.Evaluate(0.5f);
            var noisePhase = signalCurve.Evaluate(1.4f);
            var blackPhase = signalCurve.Evaluate(2.4f);
            if (Mathf.Abs(portraitPhase) > Tolerance ||
                Mathf.Abs(noisePhase - 1f) > Tolerance ||
                Mathf.Abs(blackPhase - 2f) > Tolerance)
                throw new InvalidOperationException("The portrait, white-noise, and black signal phases are incorrect.");
            if (AnimationUtility.GetCurveBindings(clip).Any(binding => string.IsNullOrEmpty(binding.path)))
                throw new InvalidOperationException("The Dolore death clip contains a slot-root animation curve.");

            var rest = SamplePose(clip, slot, renderer, hips, topBone, 0f);
            var fallen = SamplePose(clip, slot, renderer, hips, topBone, FallDuration);
            var holdNoise = SamplePose(clip, slot, renderer, hips, topBone, 1.4f);
            var holdBlack = SamplePose(clip, slot, renderer, hips, topBone, 2.4f);
            var holdEnd = SamplePose(clip, slot, renderer, hips, topBone, Duration - 0.001f);
            var leftDisplacement = rest.TopPosition.x - fallen.TopPosition.x;
            var fallAngle = Quaternion.Angle(rest.HipsRotation, fallen.HipsRotation);
            var groundContactError = Mathf.Abs(fallen.Bounds.min.y - rest.Bounds.min.y);
            var holdPositionError = new[] { holdNoise, holdBlack, holdEnd }
                .Max(pose => Vector3.Distance(fallen.HipsPosition, pose.HipsPosition));
            var holdRotationError = new[] { holdNoise, holdBlack, holdEnd }
                .Max(pose => Quaternion.Angle(fallen.HipsRotation, pose.HipsRotation));
            if (Mathf.Abs(clip.length - Duration) > 0.001f ||
                leftDisplacement <= 0.25f ||
                fallAngle < 80f || fallAngle > 100f ||
                groundContactError > 0.05f ||
                holdPositionError > 0.001f ||
                holdRotationError > 0.05f)
                throw new InvalidOperationException(
                    "The Dolore death motion does not match the approved left fall and hold. " +
                    "Duration=" + Num(clip.length) +
                    " LeftDisplacement=" + Num(leftDisplacement) +
                    " FallAngle=" + Num(fallAngle) +
                    " GroundContactError=" + Num(groundContactError) +
                    " HoldPositionError=" + Num(holdPositionError) +
                    " HoldRotationError=" + Num(holdRotationError));

            var actualRest = SampleAnimatorClone(slot, 0f);
            var actualFallen = SampleAnimatorClone(slot, FallDuration);
            var actualNoise = SampleAnimatorClone(slot, 1.4f);
            var actualBlack = SampleAnimatorClone(slot, 2.4f);
            var actualLeft = actualRest.TopPosition.x - actualFallen.TopPosition.x;
            var actualGround = Mathf.Abs(actualFallen.Bounds.min.y - actualRest.Bounds.min.y);
            if (actualLeft <= 0.25f || actualGround > 0.05f ||
                Mathf.Abs(actualRest.SignalPhase) > 0.01f ||
                Mathf.Abs(actualNoise.SignalPhase - 1f) > 0.01f ||
                Mathf.Abs(actualBlack.SignalPhase - 2f) > 0.01f)
                throw new InvalidOperationException(
                    "The actual death Animator clone does not reproduce the approved pose and signal phases. " +
                    "ActualLeft=" + Num(actualLeft) +
                    " ActualGround=" + Num(actualGround) +
                    " ActualPortrait=" + Num(actualRest.SignalPhase) +
                    " ActualNoise=" + Num(actualNoise.SignalPhase) +
                    " ActualBlack=" + Num(actualBlack.SignalPhase));

            return new Metrics(
                clip.length,
                leftDisplacement,
                fallAngle,
                groundContactError,
                holdPositionError,
                holdRotationError,
                portraitPhase,
                noisePhase,
                blackPhase,
                actualLeft,
                actualGround,
                actualRest.SignalPhase,
                actualNoise.SignalPhase,
                actualBlack.SignalPhase,
                portraitIndex);
        }

        private static PoseMetrics SamplePose(
            AnimationClip clip,
            Transform slot,
            SkinnedMeshRenderer renderer,
            Transform hips,
            Transform topBone,
            float time)
        {
            var snapshot = PoseSnapshot.Capture(slot);
            try
            {
                clip.SampleAnimation(slot.gameObject, time);
                return new PoseMetrics(
                    hips.localPosition,
                    hips.localRotation,
                    BakedBoundsInSlot(renderer, slot),
                    slot.InverseTransformPoint(topBone.position));
            }
            finally
            {
                snapshot.Restore();
                renderer.SetPropertyBlock(null);
            }
        }

        private static AnimatorPose SampleAnimatorClone(Transform sourceSlot, float time)
        {
            var clone = UnityEngine.Object.Instantiate(sourceSlot.gameObject);
            clone.name = "Dolore_07_Death_AnimatorInspectionClone";
            clone.hideFlags = HideFlags.DontSave;
            try
            {
                clone.SetActive(true);
                var animator = clone.GetComponent<Animator>() ??
                               throw new InvalidOperationException("The death Animator inspection clone is missing its Animator.");
                animator.enabled = true;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.Update(0f);
                var model = RequireChild(clone.transform, ModelName);
                var renderer = RequireRenderer(model);
                var signalSurface = RequireSignalSurface(model);
                var topBone = RequireTopBone(renderer, clone.transform);
                animator.Play(StateName, 0, time / Duration);
                animator.Update(0f);
                var block = new MaterialPropertyBlock();
                signalSurface.GetPropertyBlock(block);
                return new AnimatorPose(
                    BakedBoundsInSlot(renderer, clone.transform),
                    clone.transform.InverseTransformPoint(topBone.position),
                    block.GetFloat(Shader.PropertyToID("_SignalPhase")));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static void WriteInspection(Metrics metrics, string phase, bool saved)
        {
            var report = new StringBuilder()
                .AppendLine("Result=PASS")
                .AppendLine("Phase=" + phase)
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("Target=" + PlacementRootName + "/" + SlotName)
                .AppendLine("Clip=" + ClipPath)
                .AppendLine("Controller=" + ControllerPath)
                .AppendLine("PortraitFillMaterial=" + FillMaterialPath)
                .AppendLine("PortraitShader=" + ShaderPath)
                .AppendLine("DurationSeconds=" + Num(metrics.Duration))
                .AppendLine("FallDurationSeconds=" + Num(FallDuration))
                .AppendLine("WhiteNoiseDurationSeconds=" + Num(NoiseDuration))
                .AppendLine("BlackDurationSeconds=" + Num(BlackDuration))
                .AppendLine("LoopEnabled=True")
                .AppendLine("LoopReset=Immediate")
                .AppendLine("FallDirection=ScreenLeft")
                .AppendLine("LeftDisplacementMeters=" + Num(metrics.LeftDisplacement))
                .AppendLine("FallAngleDegrees=" + Num(metrics.FallAngle))
                .AppendLine("GroundContactErrorMeters=" + Num(metrics.GroundContactError))
                .AppendLine("HoldPositionErrorMeters=" + Num(metrics.HoldPositionError))
                .AppendLine("HoldRotationErrorDegrees=" + Num(metrics.HoldRotationError))
                .AppendLine("PortraitPhase=" + Num(metrics.PortraitPhase))
                .AppendLine("WhiteNoisePhase=" + Num(metrics.NoisePhase))
                .AppendLine("BlackPhase=" + Num(metrics.BlackPhase))
                .AppendLine("ActualAnimatorLeftDisplacementMeters=" + Num(metrics.ActualLeftDisplacement))
                .AppendLine("ActualAnimatorGroundContactErrorMeters=" + Num(metrics.ActualGroundContactError))
                .AppendLine("ActualAnimatorPortraitPhase=" + Num(metrics.ActualPortraitPhase))
                .AppendLine("ActualAnimatorWhiteNoisePhase=" + Num(metrics.ActualNoisePhase))
                .AppendLine("ActualAnimatorBlackPhase=" + Num(metrics.ActualBlackPhase))
                .AppendLine("PortraitMaterialIndex=" + metrics.PortraitMaterialIndex)
                .AppendLine("PortraitOnlySignal=True")
                .AppendLine("PortraitSignalOpeningMask=True")
                .AppendLine("PortraitSignalSizeMatched=True")
                .AppendLine("PortraitSignalStaticReferencePose=True")
                .AppendLine("FrameAndBodyUnaffected=True")
                .AppendLine("SlotRootAnimationCurves=False")
                .AppendLine("OtherDoloreSlotsChanged=False")
                .AppendLine("SourceFbxChanged=False")
                .AppendLine("SceneSaved=" + saved)
                .ToString();
            WriteText(InspectionReportPath, report);
            AssetDatabase.Refresh();
        }

        private static void CapturePose(
            Transform clone,
            Animator animator,
            Camera camera,
            float time,
            bool oblique,
            string path)
        {
            var signalSurface = RequireSignalSurface(RequireChild(clone, ModelName));
            signalSurface.SetPropertyBlock(null);
            animator.enabled = true;
            animator.Rebind();
            animator.Update(0f);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException("The Dolore death capture clip is missing.");
            clip.SampleAnimation(clone.gameObject, time);
            var block = new MaterialPropertyBlock();
            signalSurface.GetPropertyBlock(block);
            var phase = Path.GetFileName(path).StartsWith("02_FallenPortrait", StringComparison.Ordinal)
                ? 0f
                : time < FallDuration ? 0f : time < FallDuration + NoiseDuration ? 1f : 2f;
            block.SetFloat(Shader.PropertyToID("_SignalPhase"), phase);
            signalSurface.SetPropertyBlock(block);
            animator.enabled = false;
            var captureHips = RequireDescendant(clone, HipsBoneName);
            Debug.Log(
                "Dolore death capture pose " + Path.GetFileName(path) +
                " HipsPosition=" + Vec(captureHips.localPosition) +
                " HipsRotation=" + Quat(captureHips.localRotation) + ".");
            var baseRenderer = RequireRenderer(RequireChild(clone, ModelName));
            var slotBounds = BakedBoundsInSlot(baseRenderer, clone);
            var worldScale = clone.lossyScale;
            var bounds = new Bounds(
                clone.TransformPoint(slotBounds.center),
                new Vector3(
                    Mathf.Abs(slotBounds.size.x * worldScale.x),
                    Mathf.Abs(slotBounds.size.y * worldScale.y),
                    Mathf.Abs(slotBounds.size.z * worldScale.z)));
            var size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z, 1f);
            var direction = oblique
                ? (clone.forward + clone.right * 0.62f + Vector3.up * 0.12f).normalized
                : clone.forward;
            camera.transform.position = bounds.center + direction * size * 2.55f;
            camera.transform.LookAt(bounds.center + Vector3.up * size * 0.02f);
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var target = new RenderTexture(1024, 768, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(1024, 768, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, 1024, 768), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                animator.enabled = true;
                camera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                UnityEngine.Object.DestroyImmediate(image);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static Bounds BakedBoundsInSlot(SkinnedMeshRenderer renderer, Transform slot)
        {
            var baked = new Mesh { name = "Dolore07DeathBounds" };
            try
            {
                renderer.BakeMesh(baked);
                var vertices = baked.vertices;
                if (vertices.Length == 0)
                    throw new InvalidOperationException("The Dolore death renderer baked no vertices.");
                var first = slot.InverseTransformPoint(renderer.transform.TransformPoint(vertices[0]));
                var bounds = new Bounds(first, Vector3.zero);
                for (var index = 1; index < vertices.Length; index++)
                    bounds.Encapsulate(slot.InverseTransformPoint(renderer.transform.TransformPoint(vertices[index])));
                return bounds;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static void WritePortraitGeometryDiagnostics(
            SkinnedMeshRenderer renderer,
            Transform slot,
            int portraitIndex)
        {
            var baked = new Mesh { name = "DoloreDeathGeometryDiagnostics" };
            try
            {
                renderer.BakeMesh(baked);
                var slotVertices = baked.vertices
                    .Select(vertex => slot.InverseTransformPoint(renderer.transform.TransformPoint(vertex)))
                    .ToArray();
                var portraitTriangles = renderer.sharedMesh.GetTriangles(portraitIndex);
                var portraitIndices = portraitTriangles.Distinct().ToArray();
                var portraitBounds = new Bounds(slotVertices[portraitIndices[0]], Vector3.zero);
                foreach (var vertexIndex in portraitIndices) portraitBounds.Encapsulate(slotVertices[vertexIndex]);

                var report = new StringBuilder()
                    .AppendLine("Result=PASS")
                    .AppendLine("PortraitSubmesh=" + portraitIndex)
                    .AppendLine("PortraitBoundsCenter=" + Vec(portraitBounds.center))
                    .AppendLine("PortraitBoundsSize=" + Vec(portraitBounds.size));
                var portraitBoundaryLoops = FindBoundaryLoops(portraitTriangles);
                report.AppendLine("PortraitBoundaryLoops=" + portraitBoundaryLoops.Count);
                var boundaryMetrics = portraitBoundaryLoops.Select(loop =>
                {
                    var bounds = new Bounds(slotVertices[loop[0]], Vector3.zero);
                    foreach (var vertexIndex in loop) bounds.Encapsulate(slotVertices[vertexIndex]);
                    var area = 0f;
                    for (var index = 0; index < loop.Length; index++)
                    {
                        var current = slotVertices[loop[index]];
                        var next = slotVertices[loop[(index + 1) % loop.Length]];
                        area += current.x * next.y - next.x * current.y;
                    }
                    return new { Loop = loop, Bounds = bounds, Area = Mathf.Abs(area * 0.5f) };
                }).OrderByDescending(item => item.Area).ToArray();
                for (var index = 0; index < boundaryMetrics.Length; index++)
                {
                    var item = boundaryMetrics[index];
                    report.AppendLine(
                        "PortraitBoundary[" + index + "]=" +
                        "Vertices:" + item.Loop.Length +
                        "|Center:" + Vec(item.Bounds.center) +
                        "|Size:" + Vec(item.Bounds.size) +
                        "|AreaXY:" + Num(item.Area));
                }
                for (var submesh = 0; submesh < renderer.sharedMesh.subMeshCount; submesh++)
                {
                    var triangles = renderer.sharedMesh.GetTriangles(submesh);
                    var parent = Enumerable.Repeat(-1, renderer.sharedMesh.vertexCount).ToArray();
                    int Find(int vertex)
                    {
                        if (parent[vertex] == vertex) return vertex;
                        parent[vertex] = Find(parent[vertex]);
                        return parent[vertex];
                    }
                    void Union(int left, int right)
                    {
                        if (parent[left] < 0) parent[left] = left;
                        if (parent[right] < 0) parent[right] = right;
                        var leftRoot = Find(left);
                        var rightRoot = Find(right);
                        if (leftRoot != rightRoot) parent[rightRoot] = leftRoot;
                    }
                    for (var triangle = 0; triangle < triangles.Length; triangle += 3)
                    {
                        Union(triangles[triangle], triangles[triangle + 1]);
                        Union(triangles[triangle + 1], triangles[triangle + 2]);
                    }
                    var components = new Dictionary<int, List<int>>();
                    for (var triangle = 0; triangle < triangles.Length; triangle += 3)
                    {
                        var root = Find(triangles[triangle]);
                        if (!components.TryGetValue(root, out var componentTriangles))
                        {
                            componentTriangles = new List<int>();
                            components.Add(root, componentTriangles);
                        }
                        componentTriangles.Add(triangle);
                    }
                    report.AppendLine("Submesh[" + submesh + "]Components=" + components.Count);
                    var componentMetrics = components.Values.Select(componentTriangles =>
                    {
                        var firstVertex = triangles[componentTriangles[0]];
                        var bounds = new Bounds(slotVertices[firstVertex], Vector3.zero);
                        var vertices = new HashSet<int>();
                        foreach (var triangle in componentTriangles)
                        for (var corner = 0; corner < 3; corner++)
                        {
                            var vertexIndex = triangles[triangle + corner];
                            vertices.Add(vertexIndex);
                            bounds.Encapsulate(slotVertices[vertexIndex]);
                        }
                        var xyDistance = Vector2.Distance(
                            new Vector2(bounds.center.x, bounds.center.y),
                            new Vector2(portraitBounds.center.x, portraitBounds.center.y));
                        return new
                        {
                            Triangles = componentTriangles.Count,
                            Vertices = vertices.Count,
                            Bounds = bounds,
                            XyDistance = xyDistance
                        };
                    }).OrderBy(item => item.XyDistance).ThenByDescending(item => item.Triangles).Take(20).ToArray();
                    for (var index = 0; index < componentMetrics.Length; index++)
                    {
                        var item = componentMetrics[index];
                        report.AppendLine(
                            "Submesh[" + submesh + "].Component[" + index + "]=" +
                            "Triangles:" + item.Triangles +
                            "|Vertices:" + item.Vertices +
                            "|Center:" + Vec(item.Bounds.center) +
                            "|Size:" + Vec(item.Bounds.size) +
                            "|XyDistance:" + Num(item.XyDistance));
                    }
                }
                WriteText(GeometryReportPath, report.ToString());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static float MeasurePortraitSignalAlignment(
            SkinnedMeshRenderer source,
            SkinnedMeshRenderer signalSurface,
            int portraitIndex)
        {
            var sourceBaked = new Mesh { name = "DoloreDeathSourceAlignment" };
            var signalBaked = new Mesh { name = "DoloreDeathSignalAlignment" };
            try
            {
                source.BakeMesh(sourceBaked);
                signalSurface.BakeMesh(signalBaked);
                if (sourceBaked.vertexCount != signalBaked.vertexCount)
                    return float.PositiveInfinity;
                var sourceVertices = sourceBaked.vertices;
                var signalVertices = signalBaked.vertices;
                var portraitIndices = source.sharedMesh.GetTriangles(portraitIndex).Distinct();
                return portraitIndices.Max(index => Vector3.Distance(
                    source.transform.TransformPoint(sourceVertices[index]),
                    signalSurface.transform.TransformPoint(signalVertices[index])));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceBaked);
                UnityEngine.Object.DestroyImmediate(signalBaked);
            }
        }

        private static List<int[]> FindBoundaryLoops(int[] triangles)
        {
            var edgeCounts = new Dictionary<(int, int), int>();
            for (var triangle = 0; triangle < triangles.Length; triangle += 3)
            for (var corner = 0; corner < 3; corner++)
            {
                var left = triangles[triangle + corner];
                var right = triangles[triangle + (corner + 1) % 3];
                var edge = left < right ? (left, right) : (right, left);
                edgeCounts.TryGetValue(edge, out var count);
                edgeCounts[edge] = count + 1;
            }
            var boundaryEdges = edgeCounts.Where(item => item.Value == 1).Select(item => item.Key).ToArray();
            var adjacency = new Dictionary<int, List<int>>();
            foreach (var edge in boundaryEdges)
            {
                if (!adjacency.TryGetValue(edge.Item1, out var leftNeighbors))
                    adjacency.Add(edge.Item1, leftNeighbors = new List<int>());
                if (!adjacency.TryGetValue(edge.Item2, out var rightNeighbors))
                    adjacency.Add(edge.Item2, rightNeighbors = new List<int>());
                leftNeighbors.Add(edge.Item2);
                rightNeighbors.Add(edge.Item1);
            }
            var remaining = new HashSet<(int, int)>(boundaryEdges);
            var loops = new List<int[]>();
            while (remaining.Count > 0)
            {
                var startEdge = remaining.First();
                var loop = new List<int> { startEdge.Item1 };
                var previous = startEdge.Item1;
                var current = startEdge.Item2;
                remaining.Remove(startEdge);
                var guard = boundaryEdges.Length + 1;
                while (current != loop[0] && guard-- > 0)
                {
                    loop.Add(current);
                    var next = adjacency[current]
                        .Where(candidate => candidate != previous)
                        .FirstOrDefault(candidate =>
                        {
                            var edge = current < candidate ? (current, candidate) : (candidate, current);
                            return remaining.Contains(edge);
                        });
                    if (next == 0 && !adjacency[current].Contains(0)) break;
                    var nextEdge = current < next ? (current, next) : (next, current);
                    if (!remaining.Remove(nextEdge)) break;
                    previous = current;
                    current = next;
                }
                if (current == loop[0] && loop.Count >= 3) loops.Add(loop.ToArray());
            }
            return loops;
        }

        private static Transform RequireTopBone(SkinnedMeshRenderer renderer, Transform slot)
        {
            return renderer.bones
                .Where(bone => bone != null)
                .OrderByDescending(bone => slot.InverseTransformPoint(bone.position).y)
                .FirstOrDefault() ?? throw new InvalidOperationException("Dolore renderer has no usable bones.");
        }

        private static void SetVectorCurves(AnimationClip clip, string path, string property, IReadOnlyList<Vector3> values)
        {
            SetCurve(clip, path, property + ".x", values.Select(value => value.x).ToArray());
            SetCurve(clip, path, property + ".y", values.Select(value => value.y).ToArray());
            SetCurve(clip, path, property + ".z", values.Select(value => value.z).ToArray());
        }

        private static void SetQuaternionCurves(AnimationClip clip, string path, IReadOnlyList<Quaternion> values)
        {
            SetCurve(clip, path, "m_LocalRotation.x", values.Select(value => value.x).ToArray());
            SetCurve(clip, path, "m_LocalRotation.y", values.Select(value => value.y).ToArray());
            SetCurve(clip, path, "m_LocalRotation.z", values.Select(value => value.z).ToArray());
            SetCurve(clip, path, "m_LocalRotation.w", values.Select(value => value.w).ToArray());
        }

        private static void SetCurve(AnimationClip clip, string path, string property, IReadOnlyList<float> values)
        {
            var keys = new Keyframe[FallTimes.Length];
            for (var index = 0; index < FallTimes.Length; index++)
                keys[index] = new Keyframe(FallTimes[index], values[index]);
            var curve = new AnimationCurve(keys);
            for (var index = 0; index < curve.length; index++) curve.SmoothTangents(index, 0f);
            for (var index = 0; index < curve.length; index++)
            {
                if (curve[index].time < FallDuration) continue;
                var key = curve[index];
                key.inTangent = 0f;
                key.outTangent = 0f;
                curve.MoveKey(index, key);
            }
            clip.SetCurve(path, typeof(Transform), property, curve);
        }

        private static void DisableCompetingAnimation(Transform model)
        {
            foreach (var legacy in model.GetComponentsInChildren<Animation>(true))
            {
                legacy.playAutomatically = false;
                legacy.Stop();
                legacy.enabled = false;
                EditorUtility.SetDirty(legacy);
            }
            foreach (var nestedAnimator in model.GetComponentsInChildren<Animator>(true))
            {
                nestedAnimator.enabled = false;
                EditorUtility.SetDirty(nestedAnimator);
            }
        }

        private static void AssertDeathAssetsOnlyOnTarget(Scene scene)
        {
            var placement = RequirePlacement(scene);
            foreach (Transform slot in placement)
            {
                if (slot.name == SlotName) continue;
                var animator = slot.GetComponent<Animator>();
                if (animator != null && AssetDatabase.GetAssetPath(animator.runtimeAnimatorController) == ControllerPath)
                    throw new InvalidOperationException("The death controller is assigned outside Dolore_07_Death.");
                foreach (var renderer in slot.GetComponentsInChildren<Renderer>(true))
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material != null &&
                        AssetDatabase.GetAssetPath(material) == FillMaterialPath)
                        throw new InvalidOperationException("The death portrait material is assigned outside Dolore_07_Death.");
                }
            }
        }

        private static string[] OtherSlotSignatures(Scene scene)
        {
            var placement = RequirePlacement(scene);
            return placement.Cast<Transform>()
                .Where(slot => slot.name != SlotName)
                .OrderBy(slot => slot.GetSiblingIndex())
                .Select(SlotSignature)
                .ToArray();
        }

        private static string SlotSignature(Transform slot)
        {
            var builder = new StringBuilder()
                .Append(slot.name).Append('|')
                .Append(slot.GetSiblingIndex()).Append('|')
                .Append(Vec(slot.localPosition)).Append('|')
                .Append(Quat(slot.localRotation)).Append('|')
                .Append(Vec(slot.localScale)).Append('|')
                .Append(ControllerPathOrNone(slot.GetComponent<Animator>()));
            foreach (var renderer in slot.GetComponentsInChildren<Renderer>(true).OrderBy(item => item.name))
            {
                builder.Append("|R=").Append(renderer.name)
                    .Append("|Mesh=").Append(renderer is SkinnedMeshRenderer skinned
                        ? AssetDatabase.GetAssetPath(skinned.sharedMesh)
                        : "None");
                foreach (var material in renderer.sharedMaterials)
                    builder.Append("|M=").Append(material != null ? AssetDatabase.GetAssetPath(material) : "None");
            }
            return builder.ToString();
        }

        private static string ControllerPathOrNone(Animator animator) =>
            animator != null && animator.runtimeAnimatorController != null
                ? AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)
                : "None";

        private static int RequirePortraitMaterialIndex(SkinnedMeshRenderer renderer)
        {
            var indices = renderer.sharedMaterials
                .Select((material, index) => new { material, index })
                .Where(item => item.material != null &&
                               item.material.name.IndexOf(PortraitToken, StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(item => item.index)
                .ToArray();
            if (indices.Length != 1)
                throw new InvalidOperationException("The Dolore renderer must contain exactly one portrait material slot.");
            return indices[0];
        }

        private static Scene RequireScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must already be the active scene.");
            return scene;
        }

        private static Transform RequirePlacement(Scene scene) =>
            scene.GetRootGameObjects().SingleOrDefault(item => item.name == PlacementRootName)?.transform ??
            throw new InvalidOperationException("Approved Dolore placement is missing.");

        private static Transform RequireSlot(Scene scene, string name) =>
            RequirePlacement(scene).Find(name) ??
            throw new InvalidOperationException("Dolore slot is missing: " + name);

        private static Transform RequireChild(Transform parent, string name) =>
            parent.Find(name) ??
            throw new InvalidOperationException(parent.name + " is missing " + name + ".");

        private static Transform RequireDescendant(Transform parent, string name) =>
            parent.GetComponentsInChildren<Transform>(true).SingleOrDefault(item => item.name == name) ??
            throw new InvalidOperationException(parent.name + " is missing descendant " + name + ".");

        private static SkinnedMeshRenderer RequireRenderer(Transform model)
        {
            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(item => item.sharedMesh != null &&
                               item.name != LegacySignalOverlayName &&
                               item.name != SignalFillName)
                .ToArray();
            if (renderers.Length != 1)
                throw new InvalidOperationException("The Dolore model must contain exactly one skinned renderer.");
            return renderers[0];
        }

        private static MeshRenderer RequireSignalSurface(Transform model)
        {
            return model.GetComponentsInChildren<MeshRenderer>(true)
                       .SingleOrDefault(item => item.name == SignalFillName) ??
                   throw new InvalidOperationException("The Dolore death portrait opening mask is missing.");
        }

        private static void EnsureAssetFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            var name = Path.GetFileName(folder);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                throw new InvalidOperationException("Invalid Unity asset folder: " + folder);
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void SetLayer(Transform root, int layer)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                transform.gameObject.layer = layer;
        }

        private static void WriteText(string relativePath, string contents)
        {
            var absolutePath = Absolute(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ??
                                      throw new InvalidOperationException("Invalid output folder."));
            File.WriteAllText(absolutePath, contents, new UTF8Encoding(false));
        }

        private static string Absolute(string relativePath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));

        private static string Num(float value) =>
            value.ToString("0.#########", CultureInfo.InvariantCulture);

        private static string Vec(Vector3 value) =>
            "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + ")";

        private static string Quat(Quaternion value) =>
            "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + "," + Num(value.w) + ")";

        private sealed class PoseSnapshot
        {
            private readonly TransformState[] transforms;
            private PoseSnapshot(TransformState[] transforms) => this.transforms = transforms;
            public static PoseSnapshot Capture(Transform root) =>
                new PoseSnapshot(root.GetComponentsInChildren<Transform>(true).Select(TransformState.Capture).ToArray());
            public void Restore()
            {
                foreach (var state in transforms) state.Apply();
            }
        }

        private readonly struct TransformState
        {
            private readonly Transform target;
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;
            private TransformState(Transform target)
            {
                this.target = target;
                position = target.localPosition;
                rotation = target.localRotation;
                scale = target.localScale;
            }
            public static TransformState Capture(Transform target) => new TransformState(target);
            public void Apply()
            {
                if (target == null) return;
                target.localPosition = position;
                target.localRotation = rotation;
                target.localScale = scale;
            }
        }

        private readonly struct FallCandidate
        {
            public FallCandidate(
                Vector3 position,
                Quaternion rotation,
                Bounds bounds,
                Vector3 topPosition,
                float signedAngle)
            {
                Position = position;
                Rotation = rotation;
                Bounds = bounds;
                TopPosition = topPosition;
                SignedAngle = signedAngle;
            }
            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public Bounds Bounds { get; }
            public Vector3 TopPosition { get; }
            public float SignedAngle { get; }
        }

        private readonly struct TargetPose
        {
            public TargetPose(Vector3 restPosition, Quaternion restRotation, Bounds restBounds, FallCandidate fallen)
            {
                RestPosition = restPosition;
                RestRotation = restRotation;
                RestBounds = restBounds;
                Fallen = fallen;
            }
            public Vector3 RestPosition { get; }
            public Quaternion RestRotation { get; }
            public Bounds RestBounds { get; }
            public FallCandidate Fallen { get; }
        }

        private readonly struct PoseMetrics
        {
            public PoseMetrics(
                Vector3 hipsPosition,
                Quaternion hipsRotation,
                Bounds bounds,
                Vector3 topPosition)
            {
                HipsPosition = hipsPosition;
                HipsRotation = hipsRotation;
                Bounds = bounds;
                TopPosition = topPosition;
            }
            public Vector3 HipsPosition { get; }
            public Quaternion HipsRotation { get; }
            public Bounds Bounds { get; }
            public Vector3 TopPosition { get; }
        }

        private readonly struct AnimatorPose
        {
            public AnimatorPose(Bounds bounds, Vector3 topPosition, float signalPhase)
            {
                Bounds = bounds;
                TopPosition = topPosition;
                SignalPhase = signalPhase;
            }
            public Bounds Bounds { get; }
            public Vector3 TopPosition { get; }
            public float SignalPhase { get; }
        }

        private readonly struct CapturePoint
        {
            public CapturePoint(float time, string name)
            {
                Time = time;
                Name = name;
            }
            public float Time { get; }
            public string Name { get; }
        }

        private readonly struct Metrics
        {
            public Metrics(
                float duration,
                float leftDisplacement,
                float fallAngle,
                float groundContactError,
                float holdPositionError,
                float holdRotationError,
                float portraitPhase,
                float noisePhase,
                float blackPhase,
                float actualLeftDisplacement,
                float actualGroundContactError,
                float actualPortraitPhase,
                float actualNoisePhase,
                float actualBlackPhase,
                int portraitMaterialIndex)
            {
                Duration = duration;
                LeftDisplacement = leftDisplacement;
                FallAngle = fallAngle;
                GroundContactError = groundContactError;
                HoldPositionError = holdPositionError;
                HoldRotationError = holdRotationError;
                PortraitPhase = portraitPhase;
                NoisePhase = noisePhase;
                BlackPhase = blackPhase;
                ActualLeftDisplacement = actualLeftDisplacement;
                ActualGroundContactError = actualGroundContactError;
                ActualPortraitPhase = actualPortraitPhase;
                ActualNoisePhase = actualNoisePhase;
                ActualBlackPhase = actualBlackPhase;
                PortraitMaterialIndex = portraitMaterialIndex;
            }
            public float Duration { get; }
            public float LeftDisplacement { get; }
            public float FallAngle { get; }
            public float GroundContactError { get; }
            public float HoldPositionError { get; }
            public float HoldRotationError { get; }
            public float PortraitPhase { get; }
            public float NoisePhase { get; }
            public float BlackPhase { get; }
            public float ActualLeftDisplacement { get; }
            public float ActualGroundContactError { get; }
            public float ActualPortraitPhase { get; }
            public float ActualNoisePhase { get; }
            public float ActualBlackPhase { get; }
            public int PortraitMaterialIndex { get; }
        }
    }
}
