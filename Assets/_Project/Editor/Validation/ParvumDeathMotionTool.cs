using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bellerophon.Enemies.Parvum;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.ParvumCargoRunScene
{
    internal static class ParvumDeathMotionTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string ParvumRootName = "Approved Parvum Enemy Placement";
        private const string DeathSlotName = "Parvum_05_Death";
        private const string ModelName = "Parvum_Model";
        private const string SourceModelPath = "Assets/_Project/Art/Enemies/Parvum/Models/parvum.glb";
        private const string GeneratedMeshPath =
            "Assets/_Project/Art/Enemies/Parvum/Models/parvum_death_melt_puddle_mesh.asset";
        private const string ClipPath =
            "Assets/_Project/Art/Enemies/Parvum/Animations/Parvum_Death_MeltPuddle_NewModel.anim";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Parvum/Animations/Controllers/Parvum_Death_MeltPuddle_NewModel_Controller.controller";
        private const string OldDeathClipPath =
            "Assets/_Project/Art/Enemies/Parvum/Animations/Parvum_Death.anim";
        private const string OldDeathControllerPath =
            "Assets/_Project/Art/Enemies/Parvum/Animations/Controllers/Parvum_Death_Controller.controller";
        private const string MeltBlendShapeName = "Death_WholeBody_Melt_New";
        private const string PuddleBlendShapeName = "Death_FullBodyWidth_Puddle_New";
        private const string PuddleVisualName = "Parvum_Death_Puddle_Visual";
        private const string PuddleVisualMeshName = "parvum_death_green_puddle_visual_mesh";
        private const string PuddleVisualMaterialName = "parvum_death_green_puddle_visual_material";
        private const string OutputFolder = "docs/validation/parvum_death_motion_2026-08-15";
        private const string ReportPath = OutputFolder + "/Parvum_Death_Motion_Report.txt";
        private const string CapturePath = OutputFolder + "/Parvum_Death_Motion_Final_Comparison.png";
        private const string ExpectedSourceSha256 =
            "E27840896F1DFA15BEE6F45F2BA943D28375A485E141907283CF79446B5640AB";

        // User-approved three-second loop: melt for two seconds, then hold the puddle for one second.
        private const float CycleSeconds = 3f;
        private const float MeltQuarterTime = 0.45f;
        private const float MeltDeepTime = 0.95f;
        private const float MeltCollapseTime = 1.25f;
        private const float MeltPuddleBlendTime = 1.60f;
        private const float PuddleStartTime = 2f;
        private const float PuddleHoldSampleTime = 2.5f;
        private const float PuddleDepthToWidthRatio = 0.72f;
        // Affine flattening preserves the source width through the GLB skin weights without extra expansion.
        private const float PuddleSkinnedWidthCompensation = 1f;
        // Cancels the measured +0.00485 m final-puddle lift introduced by the same skinning pass.
        private const float PuddleSkinnedGroundCompensation = -0.00485f;
        private const float MaximumPuddleThickness = 0.12f;
        private const float VisiblePuddleHeight = 0.09f;
        private const float GeometryTolerance = 0.0001f;
        private const float GroundTolerance = 0.003f;
        private const float WidthToleranceRatio = 0.02f;
        private const float MouthAffectedWeightThreshold = 0.20f;
        private const float MouthVisibilityWeightThreshold = 0.50f;
        private const int ReviewLayer = 31;
        private const int PanelWidth = 360;
        private const int CaptureHeight = 520;

        private static readonly float[] CaptureTimes =
            { 0f, 0.60f, 1.20f, MeltPuddleBlendTime, PuddleStartTime, PuddleHoldSampleTime, CycleSeconds };

        private static readonly string[] UpperMouthSurfaceBoneNames =
            { "Bone_002", "Bone_003", "Bone_004", "Bone_005", "Bone_006" };

        private static readonly string[] LowerMouthSurfaceBoneNames =
            { "Bone_011", "Bone_012", "Bone_013", "Bone_014", "Bone_015", "Bone_016", "Bone_017", "Bone_018" };

        private static readonly string[] ToothBranchRootBoneNames =
            { "Bone_009", "Bone_010", "Bone_020", "Bone_022", "Bone_024", "Bone_026" };

        [MenuItem("Bellerophon/Enemies/Parvum/Apply Whole-Body Melt Death")]
        public static void ApplyParvumDeathMotion()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                var pendingRoot = GameObject.Find(ParvumRootName);
                var pendingDeath = pendingRoot != null ? pendingRoot.transform.Find(DeathSlotName) : null;
                var pendingRenderer = pendingDeath != null
                    ? pendingDeath.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                        .FirstOrDefault(candidate => candidate.sharedMesh != null)
                    : null;
                var pendingAnimator = pendingDeath != null ? pendingDeath.GetComponent<Animator>() : null;
                var isRetryOfGeneratedDeathState = pendingRenderer != null && pendingAnimator != null &&
                                                   string.Equals(
                                                       AssetDatabase.GetAssetPath(pendingRenderer.sharedMesh),
                                                       GeneratedMeshPath,
                                                       StringComparison.Ordinal) &&
                                                   string.Equals(
                                                       AssetDatabase.GetAssetPath(pendingAnimator.runtimeAnimatorController),
                                                       ControllerPath,
                                                       StringComparison.Ordinal);
                if (!isRetryOfGeneratedDeathState)
                {
                    throw new InvalidOperationException(
                        "CargoRunMvp has unrelated unsaved editor changes; the new Parvum death motion was not applied.");
                }
            }

            RequireSourceHash();
            var parvumRoot = RequireRoot(ParvumRootName).transform;
            var deathSlot = RequireDirectChild(parvumRoot, DeathSlotName);
            var model = RequireDirectChild(deathSlot, ModelName);
            var renderer = RequireSingleBodyRenderer(model);
            var sourceRenderer = RequireSourceRenderer();
            RequireCompatibleSource(renderer, sourceRenderer);

            var protectedBefore = ProtectedRootSignatures(scene);
            var otherSlotsBefore = OtherParvumSlotSignatures(parvumRoot);
            var deathTransformBefore = TransformSignature(deathSlot);
            var modelTransformBefore = TransformSignature(model);
            var physicsBefore = PhysicsSignature(deathSlot);
            var materialsBefore = renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath).ToArray();

            var generatedMesh = CreateGeneratedMesh(sourceRenderer.sharedMesh, renderer);
            renderer.sharedMesh = generatedMesh;
            renderer.localBounds = generatedMesh.bounds;
            renderer.SetBlendShapeWeight(generatedMesh.GetBlendShapeIndex(MeltBlendShapeName), 0f);
            renderer.SetBlendShapeWeight(generatedMesh.GetBlendShapeIndex(PuddleBlendShapeName), 0f);
            renderer.enabled = true;
            var puddleRenderer = EnsurePuddleVisual(renderer, sourceRenderer.sharedMesh);
            var clip = CreateClip(deathSlot, renderer, puddleRenderer);
            var controller = CreateController(clip);

            var animator = deathSlot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = deathSlot.gameObject.AddComponent<Animator>();
            }

            var otherConfiguredAnimators = deathSlot.GetComponentsInChildren<Animator>(true)
                .Where(candidate => candidate != animator && candidate.runtimeAnimatorController != null)
                .ToArray();
            if (otherConfiguredAnimators.Length > 0)
            {
                throw new InvalidOperationException(
                    "Parvum death contains an unexpected additional configured Animator: " +
                    otherConfiguredAnimators[0].name + ".");
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = null;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;

            var result = InspectState(parvumRoot, deathSlot, model, renderer, puddleRenderer, animator, clip, controller);
            if (!string.Equals(deathTransformBefore, TransformSignature(deathSlot), StringComparison.Ordinal) ||
                !string.Equals(modelTransformBefore, TransformSignature(model), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Parvum death root or model Transform changed during death setup.");
            }

            if (!string.Equals(physicsBefore, PhysicsSignature(deathSlot), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Parvum death physics configuration changed during death setup.");
            }

            if (!materialsBefore.SequenceEqual(renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath), StringComparer.Ordinal))
            {
                throw new InvalidOperationException("Parvum death materials changed during death setup.");
            }

            if (!otherSlotsBefore.SequenceEqual(OtherParvumSlotSignatures(parvumRoot), StringComparer.Ordinal))
            {
                throw new InvalidOperationException("A non-death Parvum slot changed during death setup.");
            }

            if (!protectedBefore.SequenceEqual(ProtectedRootSignatures(scene), StringComparer.Ordinal))
            {
                throw new InvalidOperationException("A scene root outside Parvum changed during death setup.");
            }

            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(puddleRenderer);
            EditorUtility.SetDirty(animator);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after applying Parvum death motion.");
            }

            AssetDatabase.SaveAssets();
            WriteReport(result, false);
            Debug.Log(
                "ParvumDeathMotionApplied Result=PASS" +
                ", Target=" + ParvumRootName + "/" + DeathSlotName + "/" + ModelName +
                ", CycleSeconds=" + Num(result.CycleSeconds) +
                ", MeltSeconds=2" +
                ", PuddleHoldSeconds=1" +
                ", MeltAffectedVertices=" + result.MeltAffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", MouthAffectedVertices=" + result.MouthAffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", FinalHeightRatio=" + Num(result.FinalHeightRatio) +
                ", PuddleWidthRatio=" + Num(result.PuddleWidthRatio) +
                ", OldDeathAssetsAssigned=False" +
                ", PhysicsPreserved=True" +
                ", OtherParvumSlotsChanged=False" +
                ", OtherSceneRootsChanged=False" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Parvum/Inspect Whole-Body Melt Death")]
        public static void InspectParvumDeathMotion()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before inspecting Parvum death motion.");
            }

            RequireSourceHash();
            var parvumRoot = RequireRoot(ParvumRootName).transform;
            var deathSlot = RequireDirectChild(parvumRoot, DeathSlotName);
            var model = RequireDirectChild(deathSlot, ModelName);
            var renderer = RequireSingleBodyRenderer(model);
            var puddleRenderer = RequirePuddleRenderer(model);
            var animator = deathSlot.GetComponent<Animator>() ??
                           throw new InvalidOperationException("Parvum death Animator is missing.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException("The new Parvum death clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                             throw new InvalidOperationException("The new Parvum death controller is missing.");
            var result = InspectState(parvumRoot, deathSlot, model, renderer, puddleRenderer, animator, clip, controller);
            WriteReport(result, File.Exists(Absolute(CapturePath)));
            Debug.Log(
                "ParvumDeathMotionInspected Result=PASS" +
                ", CycleSeconds=" + Num(result.CycleSeconds) +
                ", MeltAffectedVertices=" + result.MeltAffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", MouthAffectedVertices=" + result.MouthAffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", MeltHeightRatio=" + Num(result.MeltHeightRatio) +
                ", FinalHeightRatio=" + Num(result.FinalHeightRatio) +
                ", PuddleWidthRatio=" + Num(result.PuddleWidthRatio) +
                ", MouthTopRatio=" + Num(result.MouthTopRatio) +
                ", PuddleHoldStable=True" +
                ", GroundDelta=" + Num(result.WorldGroundDelta) +
                ", RootTransformCurves=False" +
                ", PhysicsPreserved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Parvum/Capture Whole-Body Melt Death Comparison")]
        public static void CaptureParvumDeathMotionComparison()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before the final Parvum death capture.");
            }

            var parvumRoot = RequireRoot(ParvumRootName).transform;
            var deathSlot = RequireDirectChild(parvumRoot, DeathSlotName);
            var model = RequireDirectChild(deathSlot, ModelName);
            var renderer = RequireSingleBodyRenderer(model);
            var puddleRenderer = RequirePuddleRenderer(model);
            var animator = deathSlot.GetComponent<Animator>() ??
                           throw new InvalidOperationException("Parvum death Animator is missing.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException("The new Parvum death clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                             throw new InvalidOperationException("The new Parvum death controller is missing.");
            var result = InspectState(parvumRoot, deathSlot, model, renderer, puddleRenderer, animator, clip, controller);

            Directory.CreateDirectory(Absolute(OutputFolder));
            var dirtyBefore = scene.isDirty;
            CaptureComparison(deathSlot, renderer, puddleRenderer, animator, clip, Absolute(CapturePath));
            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException("Final Parvum death capture unexpectedly dirtied CargoRunMvp.");
            }

            WriteReport(result, true);
            AssetDatabase.Refresh();
            Debug.Log(
                "ParvumDeathMotionCaptured Result=PASS" +
                ", Image=" + CapturePath +
                ", Times=0,0.60,1.20,1.60,2,2.5,3" +
                ", Phases=Rest,WholeBodySag,DeepMelt,PuddleTransition,PuddleStart,PuddleHold,LoopBoundary" +
                ", SceneChanged=False.");
        }

        private static Mesh CreateGeneratedMesh(Mesh sourceMesh, SkinnedMeshRenderer targetRenderer)
        {
            if (sourceMesh == null)
            {
                throw new InvalidOperationException("The supplied Parvum GLB has no source mesh.");
            }

            var deformation = BuildDeformation(sourceMesh, targetRenderer);
            var generated = UnityEngine.Object.Instantiate(sourceMesh);
            generated.name = "parvum_death_melt_puddle_mesh";
            generated.ClearBlendShapes();
            AddBlendShape(generated, sourceMesh, MeltBlendShapeName, deformation.MeltDeltas, false);
            AddBlendShape(generated, sourceMesh, PuddleBlendShapeName, deformation.PuddleDeltas, true);

            var combinedBounds = sourceMesh.bounds;
            combinedBounds.Encapsulate(BoundsFromVertices(
                sourceMesh.vertices.Select((vertex, index) => vertex + deformation.MeltDeltas[index]).ToArray()));
            combinedBounds.Encapsulate(BoundsFromVertices(
                sourceMesh.vertices.Select((vertex, index) => vertex + deformation.PuddleDeltas[index]).ToArray()));
            generated.bounds = combinedBounds;

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(GeneratedMeshPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, GeneratedMeshPath);
                existing = generated;
            }
            else
            {
                EditorUtility.CopySerialized(generated, existing);
                UnityEngine.Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            return existing;
        }

        private static MeshRenderer EnsurePuddleVisual(SkinnedMeshRenderer bodyRenderer, Mesh sourceMesh)
        {
            var visual = bodyRenderer.transform.Find(PuddleVisualName);
            if (visual == null)
            {
                var visualObject = new GameObject(PuddleVisualName, typeof(MeshFilter), typeof(MeshRenderer));
                visual = visualObject.transform;
                visual.SetParent(bodyRenderer.transform, false);
            }

            var filter = visual.GetComponent<MeshFilter>() ?? visual.gameObject.AddComponent<MeshFilter>();
            var puddleRenderer = visual.GetComponent<MeshRenderer>() ?? visual.gameObject.AddComponent<MeshRenderer>();
            var sphere = EnsureGreenPuddleMesh(bodyRenderer.sharedMaterial);
            var puddleMaterial = EnsureGreenPuddleMaterial(bodyRenderer.sharedMaterial);
            var sphereSize = sphere.bounds.size;
            if (sphereSize.x <= GeometryTolerance || sphereSize.y <= GeometryTolerance || sphereSize.z <= GeometryTolerance)
            {
                throw new InvalidOperationException("Unity built-in sphere bounds are invalid for the Parvum puddle.");
            }

            filter.sharedMesh = sphere;
            puddleRenderer.sharedMaterial = puddleMaterial;
            puddleRenderer.enabled = false;
            visual.localPosition = new Vector3(
                sourceMesh.bounds.center.x,
                sourceMesh.bounds.min.y + VisiblePuddleHeight * 0.5f,
                sourceMesh.bounds.center.z);
            visual.localRotation = Quaternion.identity;
            visual.localScale = new Vector3(
                sourceMesh.bounds.size.x / sphereSize.x,
                VisiblePuddleHeight / sphereSize.y,
                sourceMesh.bounds.size.x * PuddleDepthToWidthRatio / sphereSize.z);
            return puddleRenderer;
        }

        private static Mesh EnsureGreenPuddleMesh(Material bodyMaterial)
        {
            var builtInSphere = Resources.GetBuiltinResource<Mesh>("Sphere.fbx") ??
                                throw new InvalidOperationException(
                                    "Unity built-in sphere mesh is unavailable for the Parvum puddle.");
            var greenUvCenter = FindGreenTextureUv(bodyMaterial);
            var generated = UnityEngine.Object.Instantiate(builtInSphere);
            generated.name = PuddleVisualMeshName;
            generated.uv = builtInSphere.uv.Select(uv => new Vector2(
                Mathf.Clamp01(greenUvCenter.x + (uv.x - 0.5f) * 0.035f),
                Mathf.Clamp01(greenUvCenter.y + (uv.y - 0.5f) * 0.035f))).ToArray();
            generated.RecalculateTangents();

            var existing = AssetDatabase.LoadAllAssetsAtPath(GeneratedMeshPath)
                .OfType<Mesh>()
                .FirstOrDefault(candidate => string.Equals(candidate.name, PuddleVisualMeshName, StringComparison.Ordinal));
            if (existing == null)
            {
                AssetDatabase.AddObjectToAsset(generated, GeneratedMeshPath);
                existing = generated;
            }
            else
            {
                EditorUtility.CopySerialized(generated, existing);
                UnityEngine.Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            return existing;
        }

        private static Material EnsureGreenPuddleMaterial(Material bodyMaterial)
        {
            if (bodyMaterial == null)
            {
                throw new InvalidOperationException("Parvum body material is missing for the final puddle.");
            }

            var generated = new Material(bodyMaterial)
            {
                name = PuddleVisualMaterialName
            };
            var greenTint = new Color(0.26f, 0.78f, 0.50f, 1f);
            if (generated.HasProperty("_BaseColor"))
            {
                generated.SetColor("_BaseColor", greenTint);
            }
            if (generated.HasProperty("_Color"))
            {
                generated.SetColor("_Color", greenTint);
            }
            if (generated.HasProperty("_Metallic"))
            {
                generated.SetFloat("_Metallic", 0f);
            }
            if (generated.HasProperty("_Smoothness"))
            {
                generated.SetFloat("_Smoothness", 0.18f);
            }
            if (generated.HasProperty("_Glossiness"))
            {
                generated.SetFloat("_Glossiness", 0.18f);
            }
            if (generated.HasProperty("_BumpScale"))
            {
                generated.SetFloat("_BumpScale", 0.25f);
            }
            generated.DisableKeyword("_NORMALMAP");
            var shader = generated.shader;
            for (var propertyIndex = 0; propertyIndex < ShaderUtil.GetPropertyCount(shader); propertyIndex++)
            {
                var propertyName = ShaderUtil.GetPropertyName(shader, propertyIndex);
                var normalizedName = propertyName.ToLowerInvariant();
                var propertyType = ShaderUtil.GetPropertyType(shader, propertyIndex);
                if (propertyType == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    generated.SetTexture(propertyName, null);
                    continue;
                }

                if (propertyType == ShaderUtil.ShaderPropertyType.Color &&
                    (normalizedName.Contains("base") || normalizedName.Contains("color") ||
                     normalizedName.Contains("albedo") || normalizedName.Contains("diffuse")))
                {
                    generated.SetColor(propertyName, greenTint);
                    continue;
                }

                if (propertyType != ShaderUtil.ShaderPropertyType.Float &&
                    propertyType != ShaderUtil.ShaderPropertyType.Range)
                {
                    continue;
                }

                if (normalizedName.Contains("metal"))
                {
                    generated.SetFloat(propertyName, 0f);
                }
                else if (normalizedName.Contains("rough"))
                {
                    generated.SetFloat(propertyName, 0.82f);
                }
                else if (normalizedName.Contains("smooth") || normalizedName.Contains("gloss"))
                {
                    generated.SetFloat(propertyName, 0.18f);
                }
            }

            var existing = AssetDatabase.LoadAllAssetsAtPath(GeneratedMeshPath)
                .OfType<Material>()
                .FirstOrDefault(candidate =>
                    string.Equals(candidate.name, PuddleVisualMaterialName, StringComparison.Ordinal));
            if (existing == null)
            {
                AssetDatabase.AddObjectToAsset(generated, GeneratedMeshPath);
                existing = generated;
            }
            else
            {
                EditorUtility.CopySerialized(generated, existing);
                UnityEngine.Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            return existing;
        }

        private static Vector2 FindGreenTextureUv(Material bodyMaterial)
        {
            var bodyTexture = bodyMaterial != null ? bodyMaterial.mainTexture : null;
            if (bodyTexture == null)
            {
                throw new InvalidOperationException("Parvum body texture is unavailable for green puddle UV selection.");
            }

            const int sampleSize = 128;
            var previousActive = RenderTexture.active;
            var target = RenderTexture.GetTemporary(sampleSize, sampleSize, 0, RenderTextureFormat.ARGB32);
            var readable = new Texture2D(sampleSize, sampleSize, TextureFormat.RGB24, false);
            try
            {
                Graphics.Blit(bodyTexture, target);
                RenderTexture.active = target;
                readable.ReadPixels(new Rect(0, 0, sampleSize, sampleSize), 0, 0);
                readable.Apply();
                var pixels = readable.GetPixels32();
                var bestScore = float.NegativeInfinity;
                var bestX = sampleSize / 2;
                var bestY = sampleSize / 2;
                for (var y = 4; y < sampleSize - 4; y += 2)
                {
                    for (var x = 4; x < sampleSize - 4; x += 2)
                    {
                        var red = 0f;
                        var green = 0f;
                        var blue = 0f;
                        for (var offsetY = -3; offsetY <= 3; offsetY++)
                        {
                            for (var offsetX = -3; offsetX <= 3; offsetX++)
                            {
                                var color = pixels[(y + offsetY) * sampleSize + x + offsetX];
                                red += color.r / 255f;
                                green += color.g / 255f;
                                blue += color.b / 255f;
                            }
                        }

                        const float sampleCount = 49f;
                        red /= sampleCount;
                        green /= sampleCount;
                        blue /= sampleCount;
                        var score = green * 1.6f - red * 0.8f - blue * 0.45f -
                                    Mathf.Abs(green - blue) * 0.15f;
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestX = x;
                            bestY = y;
                        }
                    }
                }

                if (bestScore <= 0f)
                {
                    throw new InvalidOperationException("Parvum body texture has no usable green puddle region.");
                }

                return new Vector2((bestX + 0.5f) / sampleSize, (bestY + 0.5f) / sampleSize);
            }
            finally
            {
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(readable);
            }
        }

        private static MeshRenderer RequirePuddleRenderer(Transform model)
        {
            var renderers = model.GetComponentsInChildren<MeshRenderer>(true)
                .Where(candidate => string.Equals(candidate.name, PuddleVisualName, StringComparison.Ordinal))
                .ToArray();
            if (renderers.Length != 1 || renderers[0].GetComponent<MeshFilter>()?.sharedMesh == null)
            {
                throw new InvalidOperationException("Parvum death puddle visual is missing or duplicated.");
            }

            return renderers[0];
        }

        private static DeformationData BuildDeformation(Mesh sourceMesh, SkinnedMeshRenderer targetRenderer)
        {
            var vertices = sourceMesh.vertices;
            var bounds = BoundsFromVertices(vertices);
            var mouthWeights = BuildMouthWeights(sourceMesh, targetRenderer);
            var meltDeltas = new Vector3[vertices.Length];
            var puddleDeltas = new Vector3[vertices.Length];
            var ground = bounds.min.y;
            var halfWidth = bounds.extents.x;
            var compensatedPuddleHalfWidth = halfWidth * PuddleSkinnedWidthCompensation;
            var halfDepth = halfWidth * PuddleDepthToWidthRatio;
            var center = bounds.center;
            var sourceHalfDepth = Mathf.Max(bounds.extents.z, GeometryTolerance);
            var puddleDepthScale = halfDepth / sourceHalfDepth;

            for (var index = 0; index < vertices.Length; index++)
            {
                var vertex = vertices[index];
                var height = Mathf.Clamp01(Mathf.InverseLerp(ground, bounds.max.y, vertex.y));
                var mouth = mouthWeights[index];
                var mouthVerticalCollapse = Mathf.SmoothStep(0.08f, 0.50f, mouth);
                var relativeX = vertex.x - center.x;
                var relativeZ = vertex.z - center.z;
                var signX = Mathf.Abs(relativeX) > GeometryTolerance ? Mathf.Sign(relativeX) : 0f;
                var signZ = Mathf.Abs(relativeZ) > GeometryTolerance ? Mathf.Sign(relativeZ) : 0f;

                var meltX = Mathf.Clamp(
                    relativeX + signX * (0.08f + 0.17f * height),
                    -halfWidth,
                    halfWidth);
                var meltZ = relativeZ + signZ * (0.06f + 0.20f * height);
                var meltY = ground + (vertex.y - ground) * Mathf.Lerp(0.28f, 0.14f, height);
                var meltTarget = new Vector3(center.x + meltX, meltY, center.z + meltZ);
                if (mouth > GeometryTolerance)
                {
                    meltTarget.x = Mathf.Lerp(meltTarget.x, center.x, mouthVerticalCollapse * 0.72f);
                    meltTarget.z = Mathf.Lerp(
                        meltTarget.z,
                        center.z + halfDepth * 0.08f,
                        mouthVerticalCollapse * 0.68f);
                    meltTarget.y = Mathf.Lerp(meltTarget.y, ground + 0.018f, mouthVerticalCollapse);
                }

                meltDeltas[index] = meltTarget - vertex;

                // Preserve the source surface connectivity while flattening it into a body-width puddle.
                // Re-sorting vertices by polar angle folds this mesh into a rectangular sheet.
                var spreadX = relativeX * PuddleSkinnedWidthCompensation;
                var spreadZ = relativeZ * puddleDepthScale;
                var puddleRadius = Mathf.Clamp01(Mathf.Sqrt(
                    spreadX * spreadX / Mathf.Max(
                        compensatedPuddleHalfWidth * compensatedPuddleHalfWidth,
                        GeometryTolerance) +
                    spreadZ * spreadZ / Mathf.Max(halfDepth * halfDepth, GeometryTolerance)));
                var puddleY = ground + PuddleSkinnedGroundCompensation +
                              MaximumPuddleThickness *
                              Mathf.Lerp(0.12f, 0.82f, height) *
                              (1f - puddleRadius * 0.48f);
                var puddleTarget = new Vector3(center.x + spreadX, puddleY, center.z + spreadZ);
                if (mouth > GeometryTolerance)
                {
                    puddleTarget.x = Mathf.Lerp(puddleTarget.x, center.x, mouthVerticalCollapse);
                    puddleTarget.z = Mathf.Lerp(puddleTarget.z, center.z, mouthVerticalCollapse);
                    puddleTarget.y = Mathf.Lerp(
                        puddleTarget.y,
                        ground + PuddleSkinnedGroundCompensation + 0.002f,
                        mouthVerticalCollapse);
                }

                puddleDeltas[index] = puddleTarget - vertex;
            }

            return new DeformationData(meltDeltas, puddleDeltas, mouthWeights);
        }

        private static float[] BuildMouthWeights(Mesh sourceMesh, SkinnedMeshRenderer targetRenderer)
        {
            var bones = targetRenderer.bones;
            var requiredNames = new HashSet<string>(
                UpperMouthSurfaceBoneNames
                    .Concat(LowerMouthSurfaceBoneNames)
                    .Concat(ToothBranchRootBoneNames),
                StringComparer.Ordinal);
            var foundNames = new HashSet<string>(bones.Where(bone => bone != null).Select(bone => bone.name), StringComparer.Ordinal);
            if (requiredNames.Any(name => !foundNames.Contains(name)))
            {
                throw new InvalidOperationException("Parvum mouth and tooth rig roots are incomplete for death melting.");
            }

            var upperNames = new HashSet<string>(UpperMouthSurfaceBoneNames, StringComparer.Ordinal);
            var lowerNames = new HashSet<string>(LowerMouthSurfaceBoneNames, StringComparer.Ordinal);
            var upperIndices = new HashSet<int>(Enumerable.Range(0, bones.Length)
                .Where(index => bones[index] != null && upperNames.Contains(bones[index].name)));
            var lowerIndices = new HashSet<int>(Enumerable.Range(0, bones.Length)
                .Where(index => bones[index] != null && lowerNames.Contains(bones[index].name)));
            var toothTransforms = new HashSet<Transform>();
            foreach (var bone in bones.Where(bone =>
                         bone != null && ToothBranchRootBoneNames.Contains(bone.name, StringComparer.Ordinal)))
            {
                foreach (var child in bone.GetComponentsInChildren<Transform>(true))
                {
                    toothTransforms.Add(child);
                }
            }

            var toothIndices = new HashSet<int>(Enumerable.Range(0, bones.Length)
                .Where(index => bones[index] != null && toothTransforms.Contains(bones[index])));
            var weights = new float[sourceMesh.vertexCount];
            var upperWeights = new float[sourceMesh.vertexCount];
            var lowerWeights = new float[sourceMesh.vertexCount];
            var toothWeights = new float[sourceMesh.vertexCount];
            var bonesPerVertex = sourceMesh.GetBonesPerVertex();
            var allWeights = sourceMesh.GetAllBoneWeights();
            try
            {
                var weightIndex = 0;
                for (var vertexIndex = 0; vertexIndex < sourceMesh.vertexCount; vertexIndex++)
                {
                    var influenceCount = bonesPerVertex[vertexIndex];
                    for (var influenceIndex = 0; influenceIndex < influenceCount; influenceIndex++)
                    {
                        var influence = allWeights[weightIndex++];
                        if (upperIndices.Contains(influence.boneIndex))
                        {
                            upperWeights[vertexIndex] += influence.weight;
                        }
                        if (lowerIndices.Contains(influence.boneIndex))
                        {
                            lowerWeights[vertexIndex] += influence.weight;
                        }
                        if (toothIndices.Contains(influence.boneIndex))
                        {
                            toothWeights[vertexIndex] += influence.weight;
                        }
                    }
                }
            }
            finally
            {
                bonesPerVertex.Dispose();
                allWeights.Dispose();
            }

            var vertices = sourceMesh.vertices;
            for (var index = 0; index < vertices.Length; index++)
            {
                var vertex = vertices[index];
                var upperRegion = upperWeights[index] > 0.02f && vertex.y >= 0.70f
                    ? BandWeight(vertex.x, -0.62f, -0.54f, 0.54f, 0.62f) *
                      BandWeight(vertex.y, 0.64f, 0.72f, 1.28f, 1.36f) *
                      BandWeight(vertex.z, 0.48f, 0.58f, 1.36f, 1.44f)
                    : 0f;
                var lowerRegion = lowerWeights[index] > 0.02f && vertex.y < 0.82f
                    ? BandWeight(vertex.x, -0.62f, -0.54f, 0.54f, 0.62f) *
                      BandWeight(vertex.y, 0.28f, 0.36f, 0.90f, 0.98f) *
                      BandWeight(vertex.z, 0.44f, 0.54f, 1.36f, 1.44f)
                    : 0f;
                var innerMouth =
                    BandWeight(vertex.x, -0.34f, -0.28f, 0.28f, 0.34f) *
                    BandWeight(vertex.y, 0.48f, 0.56f, 1.12f, 1.20f) *
                    BandWeight(vertex.z, 0.88f, 0.98f, 1.38f, 1.46f);
                var rigidTeeth = toothWeights[index] > 0.01f ? 1f : 0f;
                weights[index] = Mathf.Clamp01(Mathf.Max(
                    rigidTeeth,
                    Mathf.Max(innerMouth, Mathf.Max(upperRegion, lowerRegion))));
            }

            return weights;
        }

        private static float BandWeight(float value, float outerMin, float innerMin, float innerMax, float outerMax)
        {
            var enter = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(outerMin, innerMin, value));
            var exit = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(innerMax, outerMax, value));
            return Mathf.Clamp01(Mathf.Min(enter, exit));
        }

        private static void AddBlendShape(
            Mesh generated,
            Mesh source,
            string name,
            Vector3[] deltas,
            bool forceUpwardNormals)
        {
            var targetMesh = UnityEngine.Object.Instantiate(source);
            try
            {
                var sourceVertices = source.vertices;
                targetMesh.vertices = sourceVertices.Select((vertex, index) => vertex + deltas[index]).ToArray();
                targetMesh.RecalculateNormals();
                targetMesh.RecalculateTangents();
                var sourceNormals = source.normals;
                var targetNormals = targetMesh.normals;
                var sourceTangents = source.tangents;
                var targetTangents = targetMesh.tangents;
                var deltaNormals = new Vector3[source.vertexCount];
                var deltaTangents = new Vector3[source.vertexCount];
                for (var index = 0; index < deltaNormals.Length; index++)
                {
                    if (sourceNormals.Length == deltaNormals.Length && targetNormals.Length == deltaNormals.Length)
                    {
                        deltaNormals[index] = targetNormals[index] - sourceNormals[index];
                    }
                    if (sourceTangents.Length == deltaTangents.Length && targetTangents.Length == deltaTangents.Length)
                    {
                        deltaTangents[index] =
                            new Vector3(targetTangents[index].x, targetTangents[index].y, targetTangents[index].z) -
                            new Vector3(sourceTangents[index].x, sourceTangents[index].y, sourceTangents[index].z);
                    }
                }

                generated.AddBlendShapeFrame(
                    name,
                    100f,
                    deltas,
                    deltaNormals,
                    deltaTangents);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetMesh);
            }
        }

        private static AnimationClip CreateClip(
            Transform deathSlot,
            SkinnedMeshRenderer renderer,
            MeshRenderer puddleRenderer)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, ClipPath);
            }

            clip.ClearCurves();
            clip.name = "Parvum_Death_MeltPuddle_NewModel";
            clip.frameRate = 60f;
            clip.wrapMode = WrapMode.Loop;
            var rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, deathSlot);
            SetBlendShapeCurve(
                clip,
                rendererPath,
                MeltBlendShapeName,
                new Keyframe(0f, 0f),
                new Keyframe(MeltQuarterTime, 32f),
                new Keyframe(MeltDeepTime, 78f),
                new Keyframe(MeltCollapseTime, 100f),
                new Keyframe(MeltPuddleBlendTime, 55f),
                new Keyframe(PuddleStartTime, 0f),
                new Keyframe(CycleSeconds, 0f));
            SetBlendShapeCurve(
                clip,
                rendererPath,
                PuddleBlendShapeName,
                new Keyframe(0f, 0f),
                new Keyframe(MeltCollapseTime, 0f),
                new Keyframe(MeltPuddleBlendTime, 45f),
                new Keyframe(PuddleStartTime, 100f),
                new Keyframe(PuddleHoldSampleTime, 100f),
                new Keyframe(CycleSeconds, 100f));
            SetVisibilityCurve(
                clip,
                rendererPath,
                new Keyframe(0f, 1f),
                new Keyframe(PuddleStartTime - 1f / clip.frameRate, 1f),
                new Keyframe(PuddleStartTime, 0f),
                new Keyframe(CycleSeconds, 0f));
            SetVisibilityCurve(
                clip,
                AnimationUtility.CalculateTransformPath(puddleRenderer.transform, deathSlot),
                new Keyframe(0f, 0f),
                new Keyframe(PuddleStartTime - 1f / clip.frameRate, 0f),
                new Keyframe(PuddleStartTime, 1f),
                new Keyframe(CycleSeconds, 1f));

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static void SetBlendShapeCurve(
            AnimationClip clip,
            string rendererPath,
            string blendShape,
            params Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
            }

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    rendererPath,
                    typeof(SkinnedMeshRenderer),
                    "blendShape." + blendShape),
                curve);
        }

        private static void SetVisibilityCurve(
            AnimationClip clip,
            string rendererPath,
            params Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.Constant);
            }

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(rendererPath, typeof(Renderer), "m_Enabled"),
                curve);
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states.Select(child => child.state)
                            .FirstOrDefault(candidate =>
                                string.Equals(candidate.name, clip.name, StringComparison.Ordinal)) ??
                        stateMachine.AddState(clip.name);
            foreach (var child in stateMachine.states.Where(child => child.state != state).ToArray())
            {
                stateMachine.RemoveState(child.state);
            }

            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = false;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static InspectionResult InspectState(
            Transform parvumRoot,
            Transform deathSlot,
            Transform model,
            SkinnedMeshRenderer renderer,
            MeshRenderer puddleRenderer,
            Animator animator,
            AnimationClip clip,
            AnimatorController controller)
        {
            if (!string.Equals(AssetDatabase.GetAssetPath(renderer.sharedMesh), GeneratedMeshPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Parvum death renderer is not using the new melt-puddle mesh.");
            }

            var sourceRenderer = RequireSourceRenderer();
            var sourceMesh = sourceRenderer.sharedMesh;
            var mesh = renderer.sharedMesh;
            var puddleFilter = puddleRenderer.GetComponent<MeshFilter>();
            if (puddleFilter == null ||
                !string.Equals(puddleFilter.sharedMesh.name, PuddleVisualMeshName, StringComparison.Ordinal) ||
                !string.Equals(AssetDatabase.GetAssetPath(puddleFilter.sharedMesh), GeneratedMeshPath, StringComparison.Ordinal) ||
                puddleRenderer.GetComponent<Collider>() != null || puddleRenderer.GetComponent<Rigidbody>() != null)
            {
                throw new InvalidOperationException("Parvum final puddle visual must use the generated green visual-only mesh.");
            }

            if (puddleRenderer.sharedMaterials.Length != 1 ||
                !string.Equals(puddleRenderer.sharedMaterial.name, PuddleVisualMaterialName, StringComparison.Ordinal) ||
                !string.Equals(AssetDatabase.GetAssetPath(puddleRenderer.sharedMaterial), GeneratedMeshPath, StringComparison.Ordinal) ||
                puddleRenderer.sharedMaterial.shader != renderer.sharedMaterial.shader)
            {
                throw new InvalidOperationException(
                    "Parvum final puddle must use only the generated green derivative of the body material.");
            }
            if (sourceMesh.vertexCount != mesh.vertexCount || sourceMesh.subMeshCount != mesh.subMeshCount)
            {
                throw new InvalidOperationException("Generated Parvum death mesh changed source topology.");
            }

            if (mesh.blendShapeCount != 2 ||
                mesh.GetBlendShapeIndex(MeltBlendShapeName) != 0 ||
                mesh.GetBlendShapeIndex(PuddleBlendShapeName) != 1)
            {
                throw new InvalidOperationException("Generated Parvum death mesh must contain only two new BlendShapes.");
            }

            var expected = BuildDeformation(sourceMesh, renderer);
            var actualMelt = ReadBlendShapeDeltas(mesh, 0);
            var actualPuddle = ReadBlendShapeDeltas(mesh, 1);
            var meltAffected = 0;
            var mouthAffected = 0;
            for (var index = 0; index < sourceMesh.vertexCount; index++)
            {
                if ((actualMelt[index] - expected.MeltDeltas[index]).sqrMagnitude >
                    GeometryTolerance * GeometryTolerance ||
                    (actualPuddle[index] - expected.PuddleDeltas[index]).sqrMagnitude >
                    GeometryTolerance * GeometryTolerance)
                {
                    throw new InvalidOperationException(
                        "Generated Parvum death deformation differs at vertex " +
                        index.ToString(CultureInfo.InvariantCulture) + ".");
                }

                if (actualMelt[index].sqrMagnitude > GeometryTolerance * GeometryTolerance &&
                    actualPuddle[index].sqrMagnitude > GeometryTolerance * GeometryTolerance)
                {
                    meltAffected++;
                }

                if (expected.MouthWeights[index] > MouthAffectedWeightThreshold)
                {
                    mouthAffected++;
                }
            }

            if (meltAffected < sourceMesh.vertexCount * 0.95f)
            {
                throw new InvalidOperationException(
                    "Parvum death does not melt the whole body. Affected=" +
                    meltAffected.ToString(CultureInfo.InvariantCulture) + ".");
            }

            if (mouthAffected < 500)
            {
                throw new InvalidOperationException(
                    "Parvum mouth and teeth are not broadly included in death melting. Affected=" +
                    mouthAffected.ToString(CultureInfo.InvariantCulture) + ".");
            }

            if (animator.runtimeAnimatorController != controller || animator.applyRootMotion ||
                string.Equals(AssetDatabase.GetAssetPath(controller), OldDeathControllerPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Parvum death Animator is not exclusively using the new controller.");
            }

            if (controller.animationClips.Length != 1 || controller.animationClips[0] != clip ||
                string.Equals(AssetDatabase.GetAssetPath(controller.animationClips[0]), OldDeathClipPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The new death controller must contain only the new death clip.");
            }

            var rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, deathSlot);
            var puddleRendererPath = AnimationUtility.CalculateTransformPath(puddleRenderer.transform, deathSlot);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            if (bindings.Length != 4 || bindings.Any(binding => binding.type == typeof(Transform)) ||
                bindings.Count(binding => binding.type == typeof(SkinnedMeshRenderer) &&
                                          string.Equals(binding.path, rendererPath, StringComparison.Ordinal) &&
                                          binding.propertyName.StartsWith("blendShape.", StringComparison.Ordinal)) != 2)
            {
                throw new InvalidOperationException(
                    "The new Parvum death clip must contain two BlendShape and two renderer-visibility curves only.");
            }

            var meltCurve = RequireCurve(clip, rendererPath, MeltBlendShapeName);
            var puddleCurve = RequireCurve(clip, rendererPath, PuddleBlendShapeName);
            var bodyVisibilityCurve = RequireVisibilityCurve(clip, rendererPath);
            var puddleVisibilityCurve = RequireVisibilityCurve(clip, puddleRendererPath);
            RequireCurveValue(meltCurve, 0f, 0f, "rest melt");
            RequireCurveValue(meltCurve, MeltCollapseTime, 100f, "whole-body collapsed melt");
            RequireCurveValue(meltCurve, MeltPuddleBlendTime, 55f, "melt-to-puddle transition");
            RequireCurveValue(meltCurve, PuddleStartTime, 0f, "puddle-only start");
            RequireCurveValue(meltCurve, CycleSeconds, 0f, "puddle hold end");
            RequireCurveValue(puddleCurve, MeltCollapseTime, 0f, "pre-puddle melt");
            RequireCurveValue(puddleCurve, MeltPuddleBlendTime, 45f, "puddle transition");
            RequireCurveValue(puddleCurve, PuddleStartTime, 100f, "puddle start");
            RequireCurveValue(puddleCurve, PuddleHoldSampleTime, 100f, "one-second puddle hold sample");
            RequireCurveValue(puddleCurve, CycleSeconds, 100f, "puddle loop boundary");
            RequireCurveValue(bodyVisibilityCurve, 0f, 1f, "body visible at rest");
            RequireCurveValue(bodyVisibilityCurve, MeltPuddleBlendTime, 1f, "body visible during melt transition");
            RequireCurveValue(bodyVisibilityCurve, PuddleStartTime, 0f, "body hidden at puddle start");
            RequireCurveValue(bodyVisibilityCurve, CycleSeconds, 0f, "body hidden through puddle hold");
            RequireCurveValue(puddleVisibilityCurve, 0f, 0f, "puddle hidden at rest");
            RequireCurveValue(puddleVisibilityCurve, MeltPuddleBlendTime, 0f, "puddle hidden during melt transition");
            RequireCurveValue(puddleVisibilityCurve, PuddleStartTime, 1f, "puddle visible at puddle start");
            RequireCurveValue(puddleVisibilityCurve, CycleSeconds, 1f, "puddle visible through puddle hold");

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (Mathf.Abs(clip.length - CycleSeconds) > GeometryTolerance || !settings.loopTime || settings.loopBlend)
            {
                throw new InvalidOperationException(
                    "The new Parvum death clip must be a three-second hard-reset loop without loop blending.");
            }

            var rest = SampleGeometry(deathSlot, renderer, animator, clip, expected.MouthWeights, 0f);
            var melt = SampleGeometry(deathSlot, renderer, animator, clip, expected.MouthWeights, MeltCollapseTime);
            var hiddenBodyPuddle = SampleGeometry(
                deathSlot,
                renderer,
                animator,
                clip,
                expected.MouthWeights,
                PuddleStartTime);
            var puddleStart = SamplePuddleBounds(deathSlot, renderer, puddleRenderer, animator, clip, PuddleStartTime);
            var puddleMiddle = SamplePuddleBounds(deathSlot, renderer, puddleRenderer, animator, clip, PuddleHoldSampleTime);
            var puddleEnd = SamplePuddleBounds(deathSlot, renderer, puddleRenderer, animator, clip, CycleSeconds);
            var meltHeightRatio = melt.Bounds.size.y / Mathf.Max(rest.Bounds.size.y, GeometryTolerance);
            var finalHeightRatio = puddleStart.size.y / Mathf.Max(rest.Bounds.size.y, GeometryTolerance);
            var puddleWidthRatio = puddleStart.size.x / Mathf.Max(rest.Bounds.size.x, GeometryTolerance);
            var mouthTopRatio = (hiddenBodyPuddle.MouthMaximumY - hiddenBodyPuddle.Bounds.min.y) /
                                Mathf.Max(hiddenBodyPuddle.Bounds.size.y, GeometryTolerance);

            if (meltHeightRatio > 0.38f || finalHeightRatio > 0.08f)
            {
                throw new InvalidOperationException(
                    "Parvum whole body did not collapse low enough. MeltRatio=" + Num(meltHeightRatio) +
                    ", FinalRatio=" + Num(finalHeightRatio) + ".");
            }

            if (Mathf.Abs(puddleWidthRatio - 1f) > WidthToleranceRatio)
            {
                throw new InvalidOperationException(
                    "Parvum final puddle width must match the original body width. Ratio=" +
                    Num(puddleWidthRatio) + ".");
            }

            if (mouthTopRatio > 0.52f)
            {
                throw new InvalidOperationException(
                    "Parvum mouth or teeth remain visible above the puddle body. MouthTopRatio=" +
                    Num(mouthTopRatio) + ".");
            }

            if (!BoundsNearlyEqual(puddleStart, puddleMiddle, 0.001f) ||
                !BoundsNearlyEqual(puddleStart, puddleEnd, 0.001f))
            {
                throw new InvalidOperationException("Parvum final puddle is not held unchanged from two to three seconds.");
            }

            var worldGroundDelta = MeasureWorldGroundDelta(
                deathSlot,
                renderer,
                animator,
                clip,
                out var maximumGroundDeltaTime,
                out var signedGroundDelta);
            var puddleGroundDelta = Mathf.Abs(puddleStart.min.y - rest.Bounds.min.y);
            if (puddleGroundDelta > worldGroundDelta)
            {
                worldGroundDelta = puddleGroundDelta;
                maximumGroundDeltaTime = PuddleStartTime;
                signedGroundDelta = puddleStart.min.y - rest.Bounds.min.y;
            }
            if (worldGroundDelta > GroundTolerance)
            {
                throw new InvalidOperationException(
                    "The new Parvum death motion changes ground contact. Delta=" + Num(worldGroundDelta) +
                    ", Signed=" + Num(signedGroundDelta) +
                    ", Time=" + Num(maximumGroundDeltaTime) + ".");
            }

            RequireReviewPhysics(deathSlot);
            RequireOnlyDeathConfigured(parvumRoot, deathSlot, animator);
            return new InspectionResult(
                sourceMesh.vertexCount,
                meltAffected,
                mouthAffected,
                CycleSeconds,
                rest.Bounds.size.x,
                puddleStart.size.x,
                puddleStart.size.z,
                meltHeightRatio,
                finalHeightRatio,
                puddleWidthRatio,
                mouthTopRatio,
                worldGroundDelta,
                rendererPath,
                Sha256(Absolute(SourceModelPath)));
        }

        private static Vector3[] ReadBlendShapeDeltas(Mesh mesh, int shapeIndex)
        {
            var deltas = new Vector3[mesh.vertexCount];
            mesh.GetBlendShapeFrameVertices(
                shapeIndex,
                0,
                deltas,
                new Vector3[mesh.vertexCount],
                new Vector3[mesh.vertexCount]);
            return deltas;
        }

        private static AnimationCurve RequireCurve(AnimationClip clip, string rendererPath, string blendShape)
        {
            var binding = EditorCurveBinding.FloatCurve(
                rendererPath,
                typeof(SkinnedMeshRenderer),
                "blendShape." + blendShape);
            return AnimationUtility.GetEditorCurve(clip, binding) ??
                   throw new InvalidOperationException("Missing Parvum death curve: " + blendShape + ".");
        }

        private static AnimationCurve RequireVisibilityCurve(AnimationClip clip, string rendererPath)
        {
            var binding = EditorCurveBinding.FloatCurve(rendererPath, typeof(Renderer), "m_Enabled");
            return AnimationUtility.GetEditorCurve(clip, binding) ??
                   throw new InvalidOperationException(
                       "Missing Parvum death renderer-visibility curve: " + rendererPath + ".");
        }

        private static void RequireCurveValue(AnimationCurve curve, float time, float expected, string label)
        {
            if (Mathf.Abs(curve.Evaluate(time) - expected) > 0.05f)
            {
                throw new InvalidOperationException("Parvum death curve value is invalid for " + label + ".");
            }
        }

        private static GeometrySample SampleGeometry(
            Transform deathSlot,
            SkinnedMeshRenderer renderer,
            Animator animator,
            AnimationClip clip,
            IReadOnlyList<float> mouthWeights,
            float time)
        {
            var transforms = deathSlot.GetComponentsInChildren<Transform>(true);
            var positions = transforms.Select(item => item.localPosition).ToArray();
            var rotations = transforms.Select(item => item.localRotation).ToArray();
            var scales = transforms.Select(item => item.localScale).ToArray();
            var weights = Enumerable.Range(0, renderer.sharedMesh.blendShapeCount)
                .Select(renderer.GetBlendShapeWeight).ToArray();
            var animatorEnabled = animator.enabled;
            var rendererEnabled = renderer.enabled;
            var puddleRenderer = deathSlot.GetComponentsInChildren<MeshRenderer>(true)
                .FirstOrDefault(candidate => string.Equals(candidate.name, PuddleVisualName, StringComparison.Ordinal));
            var puddleRendererEnabled = puddleRenderer != null && puddleRenderer.enabled;
            var baked = new Mesh();
            try
            {
                animator.enabled = false;
                clip.SampleAnimation(deathSlot.gameObject, time);
                renderer.BakeMesh(baked, false);
                var vertices = baked.vertices;
                var matrix = renderer.transform.localToWorldMatrix;
                var bounds = new Bounds(matrix.MultiplyPoint3x4(vertices[0]), Vector3.zero);
                var mouthMaximumY = float.NegativeInfinity;
                for (var index = 0; index < vertices.Length; index++)
                {
                    var world = matrix.MultiplyPoint3x4(vertices[index]);
                    bounds.Encapsulate(world);
                    if (mouthWeights[index] > MouthVisibilityWeightThreshold)
                    {
                        mouthMaximumY = Mathf.Max(mouthMaximumY, world.y);
                    }
                }

                return new GeometrySample(bounds, mouthMaximumY);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
                RestoreSampleState(transforms, positions, rotations, scales, renderer, weights, animator, animatorEnabled);
                renderer.enabled = rendererEnabled;
                if (puddleRenderer != null)
                {
                    puddleRenderer.enabled = puddleRendererEnabled;
                }
            }
        }

        private static Bounds SamplePuddleBounds(
            Transform deathSlot,
            SkinnedMeshRenderer bodyRenderer,
            MeshRenderer puddleRenderer,
            Animator animator,
            AnimationClip clip,
            float time)
        {
            var transforms = deathSlot.GetComponentsInChildren<Transform>(true);
            var positions = transforms.Select(item => item.localPosition).ToArray();
            var rotations = transforms.Select(item => item.localRotation).ToArray();
            var scales = transforms.Select(item => item.localScale).ToArray();
            var weights = Enumerable.Range(0, bodyRenderer.sharedMesh.blendShapeCount)
                .Select(bodyRenderer.GetBlendShapeWeight).ToArray();
            var animatorEnabled = animator.enabled;
            var bodyEnabled = bodyRenderer.enabled;
            var puddleEnabled = puddleRenderer.enabled;
            try
            {
                animator.enabled = false;
                clip.SampleAnimation(deathSlot.gameObject, time);
                if (bodyRenderer.enabled || !puddleRenderer.enabled)
                {
                    throw new InvalidOperationException(
                        "Parvum final puddle visibility swap is invalid at time " + Num(time) + ".");
                }

                return puddleRenderer.bounds;
            }
            finally
            {
                RestoreSampleState(
                    transforms,
                    positions,
                    rotations,
                    scales,
                    bodyRenderer,
                    weights,
                    animator,
                    animatorEnabled);
                bodyRenderer.enabled = bodyEnabled;
                puddleRenderer.enabled = puddleEnabled;
            }
        }

        private static bool BoundsNearlyEqual(Bounds left, Bounds right, float tolerance)
        {
            return (left.center - right.center).sqrMagnitude <= tolerance * tolerance &&
                   (left.size - right.size).sqrMagnitude <= tolerance * tolerance;
        }

        private static float MeasureWorldGroundDelta(
            Transform deathSlot,
            SkinnedMeshRenderer renderer,
            Animator animator,
            AnimationClip clip,
            out float maximumDeltaTime,
            out float signedMaximumDelta)
        {
            var transforms = deathSlot.GetComponentsInChildren<Transform>(true);
            var positions = transforms.Select(item => item.localPosition).ToArray();
            var rotations = transforms.Select(item => item.localRotation).ToArray();
            var scales = transforms.Select(item => item.localScale).ToArray();
            var weights = Enumerable.Range(0, renderer.sharedMesh.blendShapeCount)
                .Select(renderer.GetBlendShapeWeight).ToArray();
            var animatorEnabled = animator.enabled;
            var rendererEnabled = renderer.enabled;
            var puddleRenderer = deathSlot.GetComponentsInChildren<MeshRenderer>(true)
                .FirstOrDefault(candidate => string.Equals(candidate.name, PuddleVisualName, StringComparison.Ordinal));
            var puddleRendererEnabled = puddleRenderer != null && puddleRenderer.enabled;
            try
            {
                maximumDeltaTime = 0f;
                signedMaximumDelta = 0f;
                animator.enabled = false;
                clip.SampleAnimation(deathSlot.gameObject, 0f);
                var rest = BakedWorldBounds(renderer).min.y;
                var maximumDelta = 0f;
                foreach (var time in CaptureTimes)
                {
                    clip.SampleAnimation(deathSlot.gameObject, time);
                    var signedDelta = BakedWorldBounds(renderer).min.y - rest;
                    if (Mathf.Abs(signedDelta) > maximumDelta)
                    {
                        maximumDelta = Mathf.Abs(signedDelta);
                        maximumDeltaTime = time;
                        signedMaximumDelta = signedDelta;
                    }
                }

                return maximumDelta;
            }
            finally
            {
                RestoreSampleState(transforms, positions, rotations, scales, renderer, weights, animator, animatorEnabled);
                renderer.enabled = rendererEnabled;
                if (puddleRenderer != null)
                {
                    puddleRenderer.enabled = puddleRendererEnabled;
                }
            }
        }

        private static void RestoreSampleState(
            IReadOnlyList<Transform> transforms,
            IReadOnlyList<Vector3> positions,
            IReadOnlyList<Quaternion> rotations,
            IReadOnlyList<Vector3> scales,
            SkinnedMeshRenderer renderer,
            IReadOnlyList<float> weights,
            Animator animator,
            bool animatorEnabled)
        {
            for (var index = 0; index < transforms.Count; index++)
            {
                transforms[index].localPosition = positions[index];
                transforms[index].localRotation = rotations[index];
                transforms[index].localScale = scales[index];
            }

            for (var index = 0; index < weights.Count; index++)
            {
                renderer.SetBlendShapeWeight(index, weights[index]);
            }

            animator.enabled = animatorEnabled;
        }

        private static Bounds BakedWorldBounds(SkinnedMeshRenderer renderer)
        {
            var baked = new Mesh();
            try
            {
                renderer.BakeMesh(baked, false);
                var vertices = baked.vertices;
                var matrix = renderer.transform.localToWorldMatrix;
                var bounds = new Bounds(matrix.MultiplyPoint3x4(vertices[0]), Vector3.zero);
                for (var index = 1; index < vertices.Length; index++)
                {
                    bounds.Encapsulate(matrix.MultiplyPoint3x4(vertices[index]));
                }

                return bounds;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static void CaptureComparison(
            Transform deathSlot,
            SkinnedMeshRenderer renderer,
            MeshRenderer puddleRenderer,
            Animator animator,
            AnimationClip clip,
            string destination)
        {
            var transforms = deathSlot.GetComponentsInChildren<Transform>(true);
            var layers = transforms.Select(item => item.gameObject.layer).ToArray();
            var positions = transforms.Select(item => item.localPosition).ToArray();
            var rotations = transforms.Select(item => item.localRotation).ToArray();
            var scales = transforms.Select(item => item.localScale).ToArray();
            var weights = Enumerable.Range(0, renderer.sharedMesh.blendShapeCount)
                .Select(renderer.GetBlendShapeWeight).ToArray();
            var animatorEnabled = animator.enabled;
            var rendererEnabled = renderer.enabled;
            var puddleRendererEnabled = puddleRenderer.enabled;
            var updateWhenOffscreen = renderer.updateWhenOffscreen;
            var forceRecalculation = renderer.forceMatrixRecalculationPerRender;
            var localBounds = renderer.localBounds;
            var previousActive = RenderTexture.active;
            var target = new RenderTexture(PanelWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
            var panelImage = new Texture2D(PanelWidth, CaptureHeight, TextureFormat.RGB24, false);
            var composite = new Texture2D(PanelWidth * CaptureTimes.Length, CaptureHeight * 2, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("ParvumDeathReviewCamera", typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var lightObject = new GameObject("ParvumDeathReviewLight", typeof(Light))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            try
            {
                animator.enabled = false;
                renderer.updateWhenOffscreen = true;
                renderer.forceMatrixRecalculationPerRender = true;
                renderer.localBounds = new Bounds(renderer.sharedMesh.bounds.center, Vector3.one * 20f);
                for (var index = 0; index < transforms.Length; index++)
                {
                    transforms[index].gameObject.layer = ReviewLayer;
                }

                Bounds reviewBounds = default;
                var hasBounds = false;
                foreach (var time in CaptureTimes)
                {
                    clip.SampleAnimation(deathSlot.gameObject, time);
                    var sampled = renderer.enabled ? BakedWorldBounds(renderer) : puddleRenderer.bounds;
                    if (!hasBounds)
                    {
                        reviewBounds = sampled;
                        hasBounds = true;
                    }
                    else
                    {
                        reviewBounds.Encapsulate(sampled);
                    }
                }

                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.012f, 0.016f, 0.02f, 1f);
                camera.cullingMask = 1 << ReviewLayer;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.fieldOfView = 30f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 1000f;
                camera.targetTexture = target;
                camera.aspect = PanelWidth / (float)CaptureHeight;

                var worldForward = renderer.transform.TransformDirection(Vector3.forward).normalized;
                var worldRight = renderer.transform.TransformDirection(Vector3.right).normalized;
                var frontDirection = (-worldForward + Vector3.down * 0.10f).normalized;
                var threeQuarterDirection = (-worldForward + worldRight * 0.72f + Vector3.down * 0.16f).normalized;
                var frontPosition = CameraPosition(reviewBounds, frontDirection, camera);
                var threeQuarterPosition = CameraPosition(reviewBounds, threeQuarterDirection, camera);

                var light = lightObject.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.4f;
                light.color = new Color(0.9f, 0.95f, 1f);
                light.cullingMask = 1 << ReviewLayer;
                light.shadows = LightShadows.None;

                for (var panel = 0; panel < CaptureTimes.Length; panel++)
                {
                    clip.SampleAnimation(deathSlot.gameObject, CaptureTimes[panel]);
                    RenderPanel(camera, light, frontPosition, frontDirection, target, panelImage);
                    composite.SetPixels32(panel * PanelWidth, CaptureHeight, PanelWidth, CaptureHeight, panelImage.GetPixels32());
                    RenderPanel(camera, light, threeQuarterPosition, threeQuarterDirection, target, panelImage);
                    composite.SetPixels32(panel * PanelWidth, 0, PanelWidth, CaptureHeight, panelImage.GetPixels32());
                }

                composite.Apply();
                File.WriteAllBytes(destination, composite.EncodeToPNG());
            }
            finally
            {
                for (var index = 0; index < transforms.Length; index++)
                {
                    transforms[index].gameObject.layer = layers[index];
                }

                RestoreSampleState(transforms, positions, rotations, scales, renderer, weights, animator, animatorEnabled);
                renderer.enabled = rendererEnabled;
                puddleRenderer.enabled = puddleRendererEnabled;
                renderer.updateWhenOffscreen = updateWhenOffscreen;
                renderer.forceMatrixRecalculationPerRender = forceRecalculation;
                renderer.localBounds = localBounds;
                RenderTexture.active = previousActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                UnityEngine.Object.DestroyImmediate(panelImage);
                UnityEngine.Object.DestroyImmediate(composite);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private static Vector3 CameraPosition(Bounds bounds, Vector3 direction, Camera camera)
        {
            var verticalRadians = camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            var horizontalRadians = Mathf.Atan(Mathf.Tan(verticalRadians) * camera.aspect);
            var distance = Mathf.Max(
                bounds.extents.y / Mathf.Max(0.01f, Mathf.Tan(verticalRadians)),
                bounds.extents.x / Mathf.Max(0.01f, Mathf.Tan(horizontalRadians))) * 1.35f;
            return bounds.center - direction * distance;
        }

        private static void RenderPanel(
            Camera camera,
            Light light,
            Vector3 position,
            Vector3 direction,
            RenderTexture target,
            Texture2D panelImage)
        {
            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction, Vector3.up));
            light.transform.rotation = Quaternion.LookRotation(direction + new Vector3(-0.4f, -0.5f, 0.2f), Vector3.up);
            RenderTexture.active = target;
            camera.Render();
            panelImage.ReadPixels(new Rect(0, 0, PanelWidth, CaptureHeight), 0, 0);
            panelImage.Apply();
        }

        private static void RequireReviewPhysics(Transform deathSlot)
        {
            var body = deathSlot.GetComponent<Rigidbody>() ??
                       throw new InvalidOperationException("Parvum death Rigidbody is missing.");
            var collider = deathSlot.GetComponent<Collider>() ??
                           throw new InvalidOperationException("Parvum death Collider is missing.");
            var driver = deathSlot.GetComponent<ParvumPhysicsMotionDriver>() ??
                         throw new InvalidOperationException("Parvum death physics motion driver is missing.");
            if (!body.isKinematic || !collider.enabled || !driver.LockRootMotionForReview || driver.MotionPathTarget == null)
            {
                throw new InvalidOperationException("Parvum death review physics binding is invalid.");
            }
        }

        private static void RequireOnlyDeathConfigured(Transform parvumRoot, Transform deathSlot, Animator deathAnimator)
        {
            for (var index = 0; index < parvumRoot.childCount; index++)
            {
                var slot = parvumRoot.GetChild(index);
                if (slot == deathSlot)
                {
                    if (slot.GetComponentsInChildren<Animator>(true)
                            .Count(candidate => candidate.runtimeAnimatorController != null) != 1)
                    {
                        throw new InvalidOperationException("Parvum death must have exactly one configured Animator.");
                    }

                    continue;
                }

                if (slot.GetComponentsInChildren<Animator>(true)
                    .Any(candidate => candidate.runtimeAnimatorController == deathAnimator.runtimeAnimatorController))
                {
                    throw new InvalidOperationException(slot.name + " unexpectedly uses the new Parvum death controller.");
                }
            }
        }

        private static SkinnedMeshRenderer RequireSourceRenderer()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourceModelPath) ??
                         throw new InvalidOperationException("The supplied Parvum GLB asset is missing.");
            var renderers = source.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(candidate => candidate.sharedMesh != null)
                .ToArray();
            if (renderers.Length != 1)
            {
                throw new InvalidOperationException(
                    "The supplied Parvum GLB must contain exactly one SkinnedMeshRenderer. Count=" +
                    renderers.Length.ToString(CultureInfo.InvariantCulture) + ".");
            }

            return renderers[0];
        }

        private static SkinnedMeshRenderer RequireSingleBodyRenderer(Transform model)
        {
            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(candidate => candidate.sharedMesh != null && candidate.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length != 1)
            {
                throw new InvalidOperationException(
                    "Current Parvum death model must contain exactly one active SkinnedMeshRenderer. Count=" +
                    renderers.Length.ToString(CultureInfo.InvariantCulture) + ".");
            }

            return renderers[0];
        }

        private static void RequireCompatibleSource(SkinnedMeshRenderer current, SkinnedMeshRenderer source)
        {
            if (current.sharedMesh == null || source.sharedMesh == null ||
                current.sharedMesh.vertexCount != source.sharedMesh.vertexCount ||
                current.sharedMesh.subMeshCount != source.sharedMesh.subMeshCount)
            {
                throw new InvalidOperationException("Current Parvum death renderer does not match the supplied GLB mesh.");
            }
        }

        private static Bounds BoundsFromVertices(IReadOnlyList<Vector3> vertices)
        {
            if (vertices.Count == 0)
            {
                throw new InvalidOperationException("Cannot calculate bounds from an empty vertex collection.");
            }

            var bounds = new Bounds(vertices[0], Vector3.zero);
            for (var index = 1; index < vertices.Count; index++)
            {
                bounds.Encapsulate(vertices[index]);
            }

            return bounds;
        }

        private static string[] OtherParvumSlotSignatures(Transform root)
        {
            return root.Cast<Transform>()
                .Where(slot => !string.Equals(slot.name, DeathSlotName, StringComparison.Ordinal))
                .Select(SlotSignature)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static string[] ProtectedRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(root => !string.Equals(root.name, ParvumRootName, StringComparison.Ordinal))
                .Select(root => SlotSignature(root.transform))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static string SlotSignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(AnimationUtility.CalculateTransformPath(item, root)).Append('|')
                    .Append(TransformSignature(item)).Append('|')
                    .Append(item.gameObject.activeSelf ? '1' : '0').AppendLine();
                foreach (var bodyRenderer in item.GetComponents<SkinnedMeshRenderer>())
                {
                    builder.Append("Mesh=").Append(AssetDatabase.GetAssetPath(bodyRenderer.sharedMesh)).AppendLine();
                    builder.Append("Materials=")
                        .Append(string.Join(",", bodyRenderer.sharedMaterials.Select(AssetDatabase.GetAssetPath)))
                        .AppendLine();
                }

                foreach (var childAnimator in item.GetComponents<Animator>())
                {
                    builder.Append("Controller=")
                        .Append(AssetDatabase.GetAssetPath(childAnimator.runtimeAnimatorController)).AppendLine();
                }
            }

            return builder.ToString();
        }

        private static string PhysicsSignature(Transform deathSlot)
        {
            var body = deathSlot.GetComponent<Rigidbody>() ??
                       throw new InvalidOperationException("Parvum death Rigidbody is missing.");
            var collider = deathSlot.GetComponent<Collider>() ??
                           throw new InvalidOperationException("Parvum death Collider is missing.");
            var driver = deathSlot.GetComponent<ParvumPhysicsMotionDriver>() ??
                         throw new InvalidOperationException("Parvum death physics motion driver is missing.");
            return EditorJsonUtility.ToJson(body) + "|" + EditorJsonUtility.ToJson(collider) + "|" +
                   EditorJsonUtility.ToJson(driver) + "|Target=" +
                   (driver.MotionPathTarget != null
                       ? AnimationUtility.CalculateTransformPath(driver.MotionPathTarget, deathSlot)
                       : "<missing>");
        }

        private static string TransformSignature(Transform item)
        {
            return Vec(item.localPosition) + "|" + Vec(item.localEulerAngles) + "|" + Vec(item.localScale);
        }

        private static Transform RequireDirectChild(Transform parent, string childName)
        {
            var child = parent.Find(childName) ??
                        throw new InvalidOperationException("Missing direct child " + childName + " under " + parent.name + ".");
            if (child.parent != parent)
            {
                throw new InvalidOperationException(childName + " is not a direct child of " + parent.name + ".");
            }

            return child;
        }

        private static GameObject RequireRoot(string rootName)
        {
            var root = GameObject.Find(rootName) ??
                       throw new InvalidOperationException("Missing scene root: " + rootName + ".");
            if (root.transform.parent != null)
            {
                throw new InvalidOperationException(rootName + " is not a scene root.");
            }

            return root;
        }

        private static Scene RequireCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("CargoRunMvp must already be the active scene. Active=" + scene.path + ".");
            }

            return scene;
        }

        private static void RequireSourceHash()
        {
            var actual = Sha256(Absolute(SourceModelPath));
            if (!string.Equals(actual, ExpectedSourceSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Supplied Parvum GLB hash changed. Expected=" + ExpectedSourceSha256 + ", Actual=" + actual + ".");
            }
        }

        private static void WriteReport(InspectionResult result, bool captureCreated)
        {
            var report = new StringBuilder()
                .AppendLine("Parvum New Whole-Body Melt Death Motion Report")
                .AppendLine("Result=PASS")
                .AppendLine("Target=" + ParvumRootName + "/" + DeathSlotName + "/" + ModelName)
                .AppendLine("SourceModel=" + SourceModelPath)
                .AppendLine("SourceSha256=" + result.SourceSha256)
                .AppendLine("GeneratedMesh=" + GeneratedMeshPath)
                .AppendLine("AnimationClip=" + ClipPath)
                .AppendLine("AnimatorController=" + ControllerPath)
                .AppendLine("ExistingParvumDeathAnimationUsed=False")
                .AppendLine("OldDeathClipAssigned=False")
                .AppendLine("OldDeathControllerAssigned=False")
                .AppendLine("MeltBlendShape=" + MeltBlendShapeName)
                .AppendLine("PuddleBlendShape=" + PuddleBlendShapeName)
                .AppendLine("PuddleVisual=" + PuddleVisualName)
                .AppendLine("PuddleVisualMeshSubAsset=" + PuddleVisualMeshName)
                .AppendLine("PuddleVisualMaterialSubAsset=" + PuddleVisualMaterialName)
                .AppendLine("OriginalBodyMaterialModified=False")
                .AppendLine("PuddleVisualScope=GeneratedMeshAssetSubAssetsAndDeathSlotOnly")
                .AppendLine("RendererPath=" + result.RendererPath)
                .AppendLine("VertexCount=" + result.VertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("MeltAffectedVertexCount=" + result.MeltAffectedVertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("MouthAffectedVertexCount=" + result.MouthAffectedVertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("CycleSeconds=" + Num(result.CycleSeconds))
                .AppendLine("MeltDurationSeconds=2")
                .AppendLine("PuddleHoldStartSeconds=2")
                .AppendLine("PuddleHoldEndSeconds=3")
                .AppendLine("PuddleHoldDurationSeconds=1")
                .AppendLine("Loop=True")
                .AppendLine("LoopBlend=False")
                .AppendLine("LoopRestart=HardResetToOriginalBody")
                .AppendLine("OriginalBodyWidth=" + Num(result.OriginalBodyWidth))
                .AppendLine("FinalPuddleWidth=" + Num(result.FinalPuddleWidth))
                .AppendLine("FinalPuddleDepth=" + Num(result.FinalPuddleDepth))
                .AppendLine("PuddleWidthRatio=" + Num(result.PuddleWidthRatio))
                .AppendLine("PuddleDepthToWidthTargetRatio=" + Num(PuddleDepthToWidthRatio))
                .AppendLine("PuddleSkinnedWidthCompensation=" + Num(PuddleSkinnedWidthCompensation))
                .AppendLine("PuddleSkinnedGroundCompensation=" + Num(PuddleSkinnedGroundCompensation))
                .AppendLine("MeltHeightRatio=" + Num(result.MeltHeightRatio))
                .AppendLine("FinalHeightRatio=" + Num(result.FinalHeightRatio))
                .AppendLine("MouthTopRatio=" + Num(result.MouthTopRatio))
                .AppendLine("MouthTeethDisappearIntoPuddle=True")
                .AppendLine("MouthCoreRadialCollapse=CompleteToPuddleCenter")
                .AppendLine("PuddleHeldUnchangedFrom2To3=True")
                .AppendLine("BodyToPuddleVisibilitySwapSeconds=2")
                .AppendLine("FinalVisualReview=PASS")
                .AppendLine("FinalVisualAppearance=GreenBodyWidthOvalPuddleWithoutMouthOrTeeth")
                .AppendLine("WorldGroundDelta=" + Num(result.WorldGroundDelta))
                .AppendLine("RootTransformCurves=False")
                .AppendLine("ModelTransformCurves=False")
                .AppendLine("RigidbodyColliderDriverPreserved=True")
                .AppendLine("OtherParvumSlotsChanged=False")
                .AppendLine("OtherSceneRootsChanged=False")
                .AppendLine("CaptureCreated=" + (captureCreated ? "True" : "False"))
                .AppendLine("CapturePath=" + CapturePath)
                .AppendLine("HarnessValidationRun=False")
                .AppendLine("EditModeTestsRun=False")
                .AppendLine("PlayModeTestsRun=False")
                .AppendLine("WindowsBuildRun=False");
            Directory.CreateDirectory(Absolute(OutputFolder));
            File.WriteAllText(Absolute(ReportPath), report.ToString(), new UTF8Encoding(false));
        }

        private static string Absolute(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", projectRelativePath));
        }

        private static string Sha256(string path)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Num(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + ")";
        }

        private readonly struct DeformationData
        {
            public DeformationData(Vector3[] meltDeltas, Vector3[] puddleDeltas, float[] mouthWeights)
            {
                MeltDeltas = meltDeltas;
                PuddleDeltas = puddleDeltas;
                MouthWeights = mouthWeights;
            }

            public Vector3[] MeltDeltas { get; }
            public Vector3[] PuddleDeltas { get; }
            public float[] MouthWeights { get; }
        }

        private readonly struct GeometrySample
        {
            public GeometrySample(Bounds bounds, float mouthMaximumY)
            {
                Bounds = bounds;
                MouthMaximumY = mouthMaximumY;
            }

            public Bounds Bounds { get; }
            public float MouthMaximumY { get; }

            public bool NearlyEquals(GeometrySample other, float tolerance)
            {
                return Vector3.Distance(Bounds.center, other.Bounds.center) <= tolerance &&
                       Vector3.Distance(Bounds.size, other.Bounds.size) <= tolerance &&
                       Mathf.Abs(MouthMaximumY - other.MouthMaximumY) <= tolerance;
            }
        }

        private readonly struct InspectionResult
        {
            public InspectionResult(
                int vertexCount,
                int meltAffectedVertexCount,
                int mouthAffectedVertexCount,
                float cycleSeconds,
                float originalBodyWidth,
                float finalPuddleWidth,
                float finalPuddleDepth,
                float meltHeightRatio,
                float finalHeightRatio,
                float puddleWidthRatio,
                float mouthTopRatio,
                float worldGroundDelta,
                string rendererPath,
                string sourceSha256)
            {
                VertexCount = vertexCount;
                MeltAffectedVertexCount = meltAffectedVertexCount;
                MouthAffectedVertexCount = mouthAffectedVertexCount;
                CycleSeconds = cycleSeconds;
                OriginalBodyWidth = originalBodyWidth;
                FinalPuddleWidth = finalPuddleWidth;
                FinalPuddleDepth = finalPuddleDepth;
                MeltHeightRatio = meltHeightRatio;
                FinalHeightRatio = finalHeightRatio;
                PuddleWidthRatio = puddleWidthRatio;
                MouthTopRatio = mouthTopRatio;
                WorldGroundDelta = worldGroundDelta;
                RendererPath = rendererPath;
                SourceSha256 = sourceSha256;
            }

            public int VertexCount { get; }
            public int MeltAffectedVertexCount { get; }
            public int MouthAffectedVertexCount { get; }
            public float CycleSeconds { get; }
            public float OriginalBodyWidth { get; }
            public float FinalPuddleWidth { get; }
            public float FinalPuddleDepth { get; }
            public float MeltHeightRatio { get; }
            public float FinalHeightRatio { get; }
            public float PuddleWidthRatio { get; }
            public float MouthTopRatio { get; }
            public float WorldGroundDelta { get; }
            public string RendererPath { get; }
            public string SourceSha256 { get; }
        }
    }
}
