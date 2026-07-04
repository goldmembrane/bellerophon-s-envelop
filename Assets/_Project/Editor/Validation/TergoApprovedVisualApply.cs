using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.TergoCargoRunScene
{
    internal static class TergoApprovedVisualApply
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Tergo Enemy Placement";
        private const string EyeContainerName = "TergoApprovedEyes";

        private const string ApprovedSampleFbxPath =
            "Assets/_Project/Art/Enemies/Tergo/ApprovedSample/tergo_green_body_eyes_added.fbx";
        private const string MaterialFolderPath = "Assets/_Project/Art/Enemies/Tergo/Materials";
        private const string BodyMaterialPath = MaterialFolderPath + "/Tergo_Green_Translucent_Body.mat";
        private const string EyeLensMaterialPath = MaterialFolderPath + "/Tergo_EyeLens_Burning_Amber.mat";
        private const string EyeCoreMaterialPath = MaterialFolderPath + "/Tergo_EyeHotCore_Pale_Yellow.mat";

        private static readonly Color BodyColor = new(0.040f, 0.220f, 0.120f, 0.580f);
        private static readonly Color BodyEmissionColor = new(0.030f, 0.200f, 0.115f, 1.000f);
        private static readonly Color EyeLensColor = new(1.000f, 0.420f, 0.045f, 1.000f);
        private static readonly Color EyeCoreColor = new(1.000f, 0.860f, 0.320f, 1.000f);

        [MenuItem("Bellerophon/Enemies/Tergo/Apply Approved Visuals")]
        public static void ApplyApprovedVisualsToCurrentCargoRunScene()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var bodyMaterial = EnsureBodyMaterial();
            var eyeLensMaterial = EnsureEyeMaterial(EyeLensMaterialPath, "Tergo_EyeLens_Burning_Amber", EyeLensColor, 2.8f);
            var approvedEyeTemplates = CreateApprovedSampleEyeShapeTemplates(bodyMaterial, eyeLensMaterial);

            var tergoRoots = FindTergoRoots(placementRoot.transform);
            if (tergoRoots.Count == 0)
            {
                throw new InvalidOperationException("No Tergo children were found under " + PlacementRootName + ".");
            }

            var bodyRendererCount = 0;
            var eyeObjectCount = 0;
            var lightCount = 0;
            var animatorCountBefore = 0;
            var animatorCountAfter = 0;
            var skinnedRendererCountBefore = 0;
            var skinnedRendererCountAfter = 0;
            var eyePlacementSummaries = new List<string>();

            foreach (var tergoRoot in tergoRoots)
            {
                var transformState = TransformState.Capture(tergoRoot.transform);
                animatorCountBefore += tergoRoot.GetComponentsInChildren<Animator>(true).Length;
                skinnedRendererCountBefore += tergoRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;

                RemoveExistingEyeContainer(tergoRoot.transform);

                var bodyRenderers = FindBodyRenderers(tergoRoot.transform);
                if (bodyRenderers.Count == 0)
                {
                    throw new InvalidOperationException(tergoRoot.name + " has no body renderers to recolor.");
                }

                foreach (var renderer in bodyRenderers)
                {
                    AssignMaterialToAllSlots(renderer, bodyMaterial);
                    bodyRendererCount++;
                }

                eyeObjectCount += AddApprovedSampleEyes(
                    tergoRoot.transform,
                    approvedEyeTemplates,
                    out var lightsAdded,
                    out var eyePlacementSummary);
                lightCount += lightsAdded;
                eyePlacementSummaries.Add(eyePlacementSummary);

                if (!transformState.Matches(tergoRoot.transform))
                {
                    throw new InvalidOperationException(tergoRoot.name + " transform changed while applying Tergo visuals.");
                }

                animatorCountAfter += tergoRoot.GetComponentsInChildren<Animator>(true).Length;
                skinnedRendererCountAfter += tergoRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
                EditorUtility.SetDirty(tergoRoot);
            }

            if (animatorCountBefore != animatorCountAfter)
            {
                throw new InvalidOperationException(
                    "Tergo Animator count changed. Before=" + animatorCountBefore.ToString(CultureInfo.InvariantCulture) +
                    ", After=" + animatorCountAfter.ToString(CultureInfo.InvariantCulture));
            }

            if (skinnedRendererCountBefore != skinnedRendererCountAfter)
            {
                throw new InvalidOperationException(
                    "Tergo SkinnedMeshRenderer count changed. Before=" + skinnedRendererCountBefore.ToString(CultureInfo.InvariantCulture) +
                    ", After=" + skinnedRendererCountAfter.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after Tergo visual application.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("TergoApprovedEyeShapeFromApprovedSample, " + string.Join(" | ", eyePlacementSummaries));
            Debug.Log(
                "TergoApprovedVisualsApplied" +
                ", Root=" + PlacementRootName +
                ", TergoCount=" + tergoRoots.Count.ToString(CultureInfo.InvariantCulture) +
                ", BodyRenderers=" + bodyRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", EyeObjects=" + eyeObjectCount.ToString(CultureInfo.InvariantCulture) +
                ", EyeLights=" + lightCount.ToString(CultureInfo.InvariantCulture) +
                ", SampleFbx=" + ApprovedSampleFbxPath +
                ", ShapeSource=ApprovedArtSampleBuilderValues" +
                ", EyeLensScale=ApprovedSampleOriginal" +
                ", EyeOpacity=OpaqueAlpha1" +
                ", CoordinateMapping=ApprovedSampleLocalPositionRestored" +
                ", WhiteEyeCoreRemoved=True" +
                ", InternalWhiteSupportRemoved=True" +
                ", SampleEyeMeshTemplates=" + approvedEyeTemplates.MeshTemplates.Count.ToString(CultureInfo.InvariantCulture) +
                ", SampleEyeLightTemplates=" + approvedEyeTemplates.LightTemplates.Count.ToString(CultureInfo.InvariantCulture) +
                ", AnimatorCountPreserved=" + animatorCountBefore.ToString(CultureInfo.InvariantCulture) +
                ", SkinnedRendererCountPreserved=" + skinnedRendererCountBefore.ToString(CultureInfo.InvariantCulture) +
                ", BodyAlpha=" + BodyColor.a.ToString("0.###", CultureInfo.InvariantCulture) +
                ", BodyMaterial=" + BodyMaterialPath +
                ", RiggingUntouched=True" +
                ", TransformUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Capture Approved Eye Shape Comparison")]
        public static void CaptureApprovedEyeShapeComparison()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var target = placementRoot.transform.Find("Tergo_00_Static_Review");
            if (target == null)
            {
                throw new InvalidOperationException("Missing Tergo_00_Static_Review under " + PlacementRootName + ".");
            }

            var outputRoot = Path.Combine("docs", "validation", "tergo_eye_shape_20260704");
            Directory.CreateDirectory(outputRoot);

            var frontUnityPath = Path.Combine(outputRoot, "tergo_eye_shape_unity_front.png");
            var sideUnityPath = Path.Combine(outputRoot, "tergo_eye_shape_unity_side.png");
            var frontComparisonPath = Path.Combine(outputRoot, "tergo_eye_shape_front_sample_vs_unity.png");
            var sideComparisonPath = Path.Combine(outputRoot, "tergo_eye_shape_side_sample_vs_unity.png");
            var approvedFrontPath = Path.Combine(
                "artSample",
                "enemies",
                "tergo",
                "green_body_eyes_added",
                "renders",
                "tergo_green_eyes_front_large.png");
            var approvedSidePath = Path.Combine(
                "artSample",
                "enemies",
                "tergo",
                "green_body_eyes_added",
                "renders",
                "tergo_green_eyes_side_large.png");

            var bounds = CalculateWorldBounds(target);
            var eyeCenter = CalculateEyeCenter(target, bounds);
            CaptureTergoEyeShape(target, bounds, eyeCenter, frontUnityPath, true);
            CaptureTergoEyeShape(target, bounds, eyeCenter, sideUnityPath, false);
            BuildSideBySideImage(approvedFrontPath, frontUnityPath, frontComparisonPath);
            BuildSideBySideImage(approvedSidePath, sideUnityPath, sideComparisonPath);

            File.WriteAllText(
                Path.Combine(outputRoot, "README.md"),
                "# Tergo 눈 형태 샘플 비교\n\n" +
                "- 기준 샘플 정면: `tergo_green_eyes_front_large.png`\n" +
                "- Unity 정면 캡처: `tergo_eye_shape_unity_front.png`\n" +
                "- 정면 병렬 비교: `tergo_eye_shape_front_sample_vs_unity.png`\n" +
                "- 기준 샘플 측면: `tergo_green_eyes_side_large.png`\n" +
                "- Unity 측면 캡처: `tergo_eye_shape_unity_side.png`\n" +
                "- 측면 병렬 비교: `tergo_eye_shape_side_sample_vs_unity.png`\n",
                System.Text.Encoding.UTF8);

            Debug.Log(
                "TergoApprovedEyeShapeComparisonCaptured" +
                ", Target=" + target.name +
                ", OutputRoot=" + outputRoot.Replace("\\", "/") +
                ", Front=" + frontUnityPath.Replace("\\", "/") +
                ", Side=" + sideUnityPath.Replace("\\", "/") +
                ", ComparisonFront=" + frontComparisonPath.Replace("\\", "/") +
                ", ComparisonSide=" + sideComparisonPath.Replace("\\", "/"));
        }

        private static Material EnsureBodyMaterial()
        {
            var material = EnsureMaterialAsset(BodyMaterialPath, "Tergo_Green_Translucent_Body", true);
            material.name = "Tergo_Green_Translucent_Body";
            ConfigureTransparentMaterial(material, BodyColor);
            ConfigureMaterialFloat(material, "_Metallic", 0f);
            ConfigureMaterialFloat(material, "_Smoothness", 0.42f);
            ConfigureMaterialFloat(material, "_Glossiness", 0.42f);
            ConfigureEmission(material, BodyEmissionColor, 0.68f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureEyeMaterial(string path, string materialName, Color color, float emissionStrength)
        {
            var material = EnsureMaterialAsset(path, materialName, false);
            material.name = materialName;
            ConfigureOpaqueMaterial(material, color);
            ConfigureMaterialFloat(material, "_Metallic", 0f);
            ConfigureMaterialFloat(material, "_Smoothness", 0.18f);
            ConfigureMaterialFloat(material, "_Glossiness", 0.18f);
            ConfigureEmission(material, color, emissionStrength);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureMaterialAsset(string path, string materialName, bool preferLit)
        {
            Directory.CreateDirectory(MaterialFolderPath);

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            var shader = preferLit
                ? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")
                : Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");

            if (shader == null)
            {
                throw new InvalidOperationException("Could not find a usable shader for " + materialName + ".");
            }

            material = new Material(shader)
            {
                name = materialName
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void ConfigureTransparentMaterial(Material material, Color color)
        {
            SetColor(material, color);
            ConfigureMaterialFloat(material, "_Surface", 1f);
            ConfigureMaterialFloat(material, "_Blend", 0f);
            ConfigureMaterialFloat(material, "_AlphaClip", 0f);
            ConfigureMaterialFloat(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            ConfigureMaterialFloat(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            ConfigureMaterialFloat(material, "_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static void ConfigureOpaqueMaterial(Material material, Color color)
        {
            var opaqueColor = new Color(color.r, color.g, color.b, 1f);
            SetColor(material, opaqueColor);
            material.SetOverrideTag("RenderType", "Opaque");
            ConfigureMaterialFloat(material, "_Surface", 0f);
            ConfigureMaterialFloat(material, "_Blend", 0f);
            ConfigureMaterialFloat(material, "_AlphaClip", 0f);
            ConfigureMaterialFloat(material, "_AlphaToMask", 0f);
            ConfigureMaterialFloat(material, "_SrcBlend", (float)BlendMode.One);
            ConfigureMaterialFloat(material, "_DstBlend", (float)BlendMode.Zero);
            ConfigureMaterialFloat(material, "_SrcBlendAlpha", (float)BlendMode.One);
            ConfigureMaterialFloat(material, "_DstBlendAlpha", (float)BlendMode.Zero);
            ConfigureMaterialFloat(material, "_ZWrite", 1f);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = -1;
        }

        private static void ConfigureEmission(Material material, Color color, float strength)
        {
            var emission = new Color(color.r * strength, color.g * strength, color.b * strength, 1f);
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", emission);
                material.EnableKeyword("_EMISSION");
            }
        }

        private static void SetColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private static void ConfigureMaterialFloat(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static List<Transform> FindTergoRoots(Transform placementRoot)
        {
            var roots = new List<Transform>();
            for (var i = 0; i < placementRoot.childCount; i++)
            {
                var child = placementRoot.GetChild(i);
                if (child.name.StartsWith("Tergo_", StringComparison.Ordinal))
                {
                    roots.Add(child);
                }
            }

            roots.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            return roots;
        }

        private static List<Renderer> FindBodyRenderers(Transform tergoRoot)
        {
            var renderers = new List<Renderer>();
            foreach (var renderer in tergoRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.GetComponentInParent<Light>() != null)
                {
                    continue;
                }

                if (IsGeneratedEyeObject(renderer.transform))
                {
                    continue;
                }

                renderers.Add(renderer);
            }

            return renderers;
        }

        private static void AssignMaterialToAllSlots(Renderer renderer, Material material)
        {
            var materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                if (renderer.sharedMaterial == material)
                {
                    return;
                }

                renderer.sharedMaterial = material;
                EditorUtility.SetDirty(renderer);
                return;
            }

            var changed = false;
            for (var i = 0; i < materials.Length; i++)
            {
                if (materials[i] == material)
                {
                    continue;
                }

                materials[i] = material;
                changed = true;
            }

            if (!changed)
            {
                return;
            }

            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
        }

        private static ApprovedEyeTemplates CreateApprovedSampleEyeShapeTemplates(
            Material bodyMaterial,
            Material eyeLensMaterial)
        {
            if (!File.Exists(ApprovedSampleFbxPath))
            {
                throw new InvalidOperationException("Missing approved Tergo sample FBX: " + ApprovedSampleFbxPath);
            }

            AssetDatabase.ImportAsset(
                ApprovedSampleFbxPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            const float sampleHeight = 1.665f;
            const float eyeCenterY = 1.5584f;
            const float eyeSpacing = 0.0733f;
            const float leftLensZ = 0.1735f;
            const float leftLightZ = 0.25675f;
            const float rightLensZ = 0.1585f;
            const float rightLightZ = 0.24175f;

            var lensSphere = CreateApprovedUvSphereMesh("Tergo_ApprovedEyeLensSphere_Mesh", 32, 16);

            var templates = new ApprovedEyeTemplates();
            AddApprovedSampleEyeSide(
                templates,
                "L",
                eyeSpacing * 0.5f,
                leftLensZ,
                leftLightZ,
                eyeCenterY,
                sampleHeight,
                lensSphere,
                eyeLensMaterial);
            AddApprovedSampleEyeSide(
                templates,
                "R",
                -eyeSpacing * 0.5f,
                rightLensZ,
                rightLightZ,
                eyeCenterY,
                sampleHeight,
                lensSphere,
                eyeLensMaterial);

            return templates;
        }

        private static void AddApprovedSampleEyeSide(
            ApprovedEyeTemplates templates,
            string side,
            float localX,
            float lensZ,
            float lightZ,
            float eyeCenterY,
            float sampleHeight,
            Mesh lensSphere,
            Material eyeLensMaterial)
        {
            var lightY = eyeCenterY + sampleHeight * 0.005f;

            templates.MeshTemplates.Add(
                new EyeMeshTemplate(
                    $"Tergo_{side}_Glowing_Eye_Lens",
                    lensSphere,
                    new Vector3(localX, eyeCenterY, lensZ),
                    Quaternion.identity,
                    new Vector3(sampleHeight * 0.0047f, sampleHeight * 0.0032f, sampleHeight * 0.0030f),
                    eyeLensMaterial));
            templates.LightTemplates.Add(
                new EyeLightTemplate(
                    $"Tergo_{side}_Eye_Amber_Point_Light",
                    new Vector3(localX, lightY, lightZ),
                    Quaternion.identity,
                    new Color(1.0f, 0.62f, 0.24f, 1f),
                    0.005f,
                    0.05f));
        }

        private static Mesh CreateApprovedUvSphereMesh(string meshName, int segments, int rings)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();

            vertices.Add(Vector3.up);
            normals.Add(Vector3.up);

            for (var ring = 1; ring < rings; ring++)
            {
                var phi = Mathf.PI * ring / rings;
                var sinPhi = Mathf.Sin(phi);
                var cosPhi = Mathf.Cos(phi);
                for (var segment = 0; segment < segments; segment++)
                {
                    var theta = 2f * Mathf.PI * segment / segments;
                    var point = new Vector3(
                        Mathf.Cos(theta) * sinPhi,
                        cosPhi,
                        Mathf.Sin(theta) * sinPhi);
                    vertices.Add(point);
                    normals.Add(point.normalized);
                }
            }

            var bottomIndex = vertices.Count;
            vertices.Add(Vector3.down);
            normals.Add(Vector3.down);

            for (var segment = 0; segment < segments; segment++)
            {
                triangles.Add(0);
                triangles.Add(1 + segment);
                triangles.Add(1 + ((segment + 1) % segments));
            }

            for (var ring = 0; ring < rings - 2; ring++)
            {
                var rowStart = 1 + ring * segments;
                var nextRowStart = rowStart + segments;
                for (var segment = 0; segment < segments; segment++)
                {
                    var current = rowStart + segment;
                    var next = rowStart + ((segment + 1) % segments);
                    var below = nextRowStart + segment;
                    var belowNext = nextRowStart + ((segment + 1) % segments);

                    triangles.Add(current);
                    triangles.Add(below);
                    triangles.Add(next);

                    triangles.Add(next);
                    triangles.Add(below);
                    triangles.Add(belowNext);
                }
            }

            var lastRowStart = 1 + (rings - 2) * segments;
            for (var segment = 0; segment < segments; segment++)
            {
                triangles.Add(lastRowStart + ((segment + 1) % segments));
                triangles.Add(lastRowStart + segment);
                triangles.Add(bottomIndex);
            }

            var mesh = new Mesh
            {
                name = meshName
            };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateApprovedTorusMesh(
            string meshName,
            float majorRadius,
            float minorRadius,
            int majorSegments,
            int minorSegments)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();

            for (var major = 0; major < majorSegments; major++)
            {
                var theta = 2f * Mathf.PI * major / majorSegments;
                var radial = new Vector3(Mathf.Cos(theta), Mathf.Sin(theta), 0f);
                for (var minor = 0; minor < minorSegments; minor++)
                {
                    var phi = 2f * Mathf.PI * minor / minorSegments;
                    var normal = radial * Mathf.Cos(phi) + Vector3.forward * Mathf.Sin(phi);
                    vertices.Add(radial * (majorRadius + minorRadius * Mathf.Cos(phi)) + Vector3.forward * (minorRadius * Mathf.Sin(phi)));
                    normals.Add(normal.normalized);
                }
            }

            for (var major = 0; major < majorSegments; major++)
            {
                var nextMajor = (major + 1) % majorSegments;
                for (var minor = 0; minor < minorSegments; minor++)
                {
                    var nextMinor = (minor + 1) % minorSegments;
                    var current = major * minorSegments + minor;
                    var right = nextMajor * minorSegments + minor;
                    var currentNext = major * minorSegments + nextMinor;
                    var rightNext = nextMajor * minorSegments + nextMinor;

                    triangles.Add(current);
                    triangles.Add(right);
                    triangles.Add(currentNext);

                    triangles.Add(currentNext);
                    triangles.Add(right);
                    triangles.Add(rightNext);
                }
            }

            var mesh = new Mesh
            {
                name = meshName
            };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static ApprovedEyeTemplates LoadApprovedSampleEyeTemplates(
            Material bodyMaterial,
            Material eyeLensMaterial,
            Material eyeCoreMaterial)
        {
            AssetDatabase.ImportAsset(
                ApprovedSampleFbxPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            var sampleRoot = AssetDatabase.LoadAssetAtPath<GameObject>(ApprovedSampleFbxPath);
            if (sampleRoot == null)
            {
                throw new InvalidOperationException("Could not load approved Tergo sample FBX: " + ApprovedSampleFbxPath);
            }

            var templates = new ApprovedEyeTemplates();
            foreach (var transform in sampleRoot.GetComponentsInChildren<Transform>(true))
            {
                if (transform == sampleRoot.transform)
                {
                    continue;
                }

                var objectName = transform.name;
                if (!IsApprovedSampleEyeName(objectName))
                {
                    continue;
                }

                var meshFilter = transform.GetComponent<MeshFilter>();
                var meshRenderer = transform.GetComponent<MeshRenderer>();
                if (meshFilter != null && meshRenderer != null && meshFilter.sharedMesh != null)
                {
                    templates.MeshTemplates.Add(
                        new EyeMeshTemplate(
                            objectName,
                            meshFilter.sharedMesh,
                            sampleRoot.transform.InverseTransformPoint(transform.position),
                            Quaternion.Inverse(sampleRoot.transform.rotation) * transform.rotation,
                            DivideLossyScale(transform.lossyScale, sampleRoot.transform.lossyScale),
                            SelectApprovedEyeMaterial(objectName, bodyMaterial, eyeLensMaterial, eyeCoreMaterial)));
                }

                var light = transform.GetComponent<Light>();
                if (light != null && objectName.IndexOf("Eye_Amber_Point_Light", StringComparison.Ordinal) >= 0)
                {
                    templates.LightTemplates.Add(
                        new EyeLightTemplate(
                            objectName,
                            sampleRoot.transform.InverseTransformPoint(transform.position),
                            Quaternion.Inverse(sampleRoot.transform.rotation) * transform.rotation,
                            light.color,
                            light.intensity,
                            light.range));
                }
            }

            templates.MeshTemplates.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
            templates.LightTemplates.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));

            if (templates.MeshTemplates.Count == 0)
            {
                throw new InvalidOperationException(
                    "Approved Tergo sample FBX has no eye mesh templates: " + ApprovedSampleFbxPath);
            }

            return templates;
        }

        private static Transform ResolveEyeFollowParent(Transform tergoRoot, ApprovedEyeTemplates templates)
        {
            var desiredEyeCenter = CalculateTemplateWorldCenter(tergoRoot, templates.MeshTemplates);
            var candidates = CollectEyeFollowCandidates(tergoRoot);
            if (candidates.Count == 0)
            {
                return tergoRoot;
            }

            Transform best = null;
            var bestScore = float.NegativeInfinity;
            foreach (var candidate in candidates)
            {
                var score = ScoreEyeFollowCandidate(candidate, desiredEyeCenter);
                if (score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best != null ? best : tergoRoot;
        }

        private static Vector3 CalculateTemplateWorldCenter(Transform sourceRoot, IReadOnlyList<EyeMeshTemplate> meshTemplates)
        {
            var sum = Vector3.zero;
            var count = 0;
            foreach (var template in meshTemplates)
            {
                if (template.Name.IndexOf("Glowing_Eye_Lens", StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                sum += sourceRoot.TransformPoint(template.LocalPosition);
                count++;
            }

            return count > 0 ? sum / count : sourceRoot.position;
        }

        private static List<Transform> CollectEyeFollowCandidates(Transform tergoRoot)
        {
            var candidates = new List<Transform>();
            var seen = new HashSet<Transform>();

            foreach (var skinnedRenderer in tergoRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                foreach (var bone in skinnedRenderer.bones)
                {
                    AddEyeFollowCandidate(tergoRoot, bone, seen, candidates);
                }
            }

            foreach (var transform in tergoRoot.GetComponentsInChildren<Transform>(true))
            {
                if (!IsEyeFollowCandidateName(transform.name))
                {
                    continue;
                }

                if (transform.GetComponent<Renderer>() != null || transform.GetComponent<Light>() != null)
                {
                    continue;
                }

                AddEyeFollowCandidate(tergoRoot, transform, seen, candidates);
            }

            return candidates;
        }

        private static void AddEyeFollowCandidate(
            Transform tergoRoot,
            Transform candidate,
            HashSet<Transform> seen,
            List<Transform> candidates)
        {
            if (candidate == null ||
                candidate == tergoRoot ||
                !candidate.IsChildOf(tergoRoot) ||
                IsGeneratedEyeObject(candidate) ||
                seen.Contains(candidate))
            {
                return;
            }

            seen.Add(candidate);
            candidates.Add(candidate);
        }

        private static bool IsEyeFollowCandidateName(string objectName)
        {
            var lowerName = objectName.ToLowerInvariant();
            return lowerName.Contains("head") ||
                   lowerName.Contains("neck") ||
                   lowerName.Contains("chest") ||
                   lowerName.Contains("spine");
        }

        private static float ScoreEyeFollowCandidate(Transform candidate, Vector3 desiredEyeCenter)
        {
            var lowerName = candidate.name.ToLowerInvariant();
            var score = 0f;

            if (lowerName.Contains("def_head"))
            {
                score += 1000f;
            }
            else if (lowerName.Contains("head"))
            {
                score += 900f;
            }

            if (lowerName.Contains("def_neck"))
            {
                score += 700f;
            }
            else if (lowerName.Contains("neck"))
            {
                score += 650f;
            }

            if (lowerName.Contains("def_chest"))
            {
                score += 500f;
            }
            else if (lowerName.Contains("chest"))
            {
                score += 450f;
            }

            if (lowerName.Contains("spine"))
            {
                score += 200f;
            }

            return score - Vector3.Distance(candidate.position, desiredEyeCenter);
        }

        private static void ApplyRootLocalTemplateTransform(
            Transform transform,
            Transform sourceRoot,
            Transform targetParent,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale)
        {
            transform.SetParent(targetParent, false);

            var worldPosition = sourceRoot.TransformPoint(localPosition);
            var worldRotation = sourceRoot.rotation * localRotation;
            var worldScale = Vector3.Scale(sourceRoot.lossyScale, localScale);

            transform.localPosition = targetParent.InverseTransformPoint(worldPosition);
            transform.localRotation = Quaternion.Inverse(targetParent.rotation) * worldRotation;
            transform.localScale = DivideLossyScale(worldScale, targetParent.lossyScale);
        }

        private static string FormatRelativeTransformPath(Transform root, Transform transform)
        {
            if (transform == root)
            {
                return root.name;
            }

            var parts = new List<string>();
            var current = transform;
            while (current != null && current != root)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            parts.Reverse();
            return parts.Count > 0 ? string.Join("/", parts) : transform.name;
        }

        private static int AddApprovedSampleEyes(
            Transform tergoRoot,
            ApprovedEyeTemplates templates,
            out int lightsAdded,
            out string placementSummary)
        {
            var followParent = ResolveEyeFollowParent(tergoRoot, templates);
            var eyeRoot = new GameObject(EyeContainerName);
            eyeRoot.transform.SetParent(followParent, false);
            eyeRoot.transform.localPosition = Vector3.zero;
            eyeRoot.transform.localRotation = Quaternion.identity;
            eyeRoot.transform.localScale = Vector3.one;

            var added = 0;
            foreach (var template in templates.MeshTemplates)
            {
                var eyeObject = new GameObject(template.Name);
                ApplyRootLocalTemplateTransform(
                    eyeObject.transform,
                    tergoRoot,
                    eyeRoot.transform,
                    template.LocalPosition,
                    template.LocalRotation,
                    template.LocalScale);

                var meshFilter = eyeObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = template.Mesh;

                var meshRenderer = eyeObject.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = template.Material;

                EditorUtility.SetDirty(meshFilter);
                EditorUtility.SetDirty(meshRenderer);
                EditorUtility.SetDirty(eyeObject);
                added++;
            }

            lightsAdded = 0;
            if (templates.LightTemplates.Count > 0)
            {
                foreach (var template in templates.LightTemplates)
                {
                    AddApprovedSampleLight(eyeRoot.transform, tergoRoot, template);
                    lightsAdded++;
                    added++;
                }
            }
            else
            {
                lightsAdded = AddFallbackEyeLights(eyeRoot.transform, tergoRoot, templates.MeshTemplates);
                added += lightsAdded;
            }

            placementSummary =
                tergoRoot.name +
                "[Source=ApprovedSampleFbx" +
                ", MeshTemplates=" + templates.MeshTemplates.Count.ToString(CultureInfo.InvariantCulture) +
                ", LightTemplates=" + templates.LightTemplates.Count.ToString(CultureInfo.InvariantCulture) +
                ", EyeFollowParent=" + FormatRelativeTransformPath(tergoRoot, followParent) +
                ", FirstLens=" + FormatFirstLensSummary(templates.MeshTemplates) +
                "]";

            EditorUtility.SetDirty(eyeRoot);
            return added;
        }

        private static void AddApprovedSampleLight(Transform parent, Transform sourceRoot, EyeLightTemplate template)
        {
            var lightObject = new GameObject(template.Name);
            ApplyRootLocalTemplateTransform(
                lightObject.transform,
                sourceRoot,
                parent,
                template.LocalPosition,
                template.LocalRotation,
                Vector3.one);

            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = template.Color;
            light.intensity = template.Intensity;
            light.range = template.Range;
            light.shadows = LightShadows.None;

            EditorUtility.SetDirty(light);
            EditorUtility.SetDirty(lightObject);
        }

        private static int AddFallbackEyeLights(Transform parent, Transform sourceRoot, IReadOnlyList<EyeMeshTemplate> meshTemplates)
        {
            var added = 0;
            foreach (var template in meshTemplates)
            {
                if (template.Name.IndexOf("Glowing_Eye_Lens", StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                var lightObject = new GameObject(template.Name.Replace("Glowing_Eye_Lens", "Eye_Amber_Point_Light"));
                ApplyRootLocalTemplateTransform(
                    lightObject.transform,
                    sourceRoot,
                    parent,
                    template.LocalPosition,
                    template.LocalRotation,
                    Vector3.one);

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1.0f, 0.62f, 0.24f, 1f);
                light.intensity = 0.05f;
                light.range = Mathf.Max(template.LocalScale.magnitude * 5f, 0.05f);
                light.shadows = LightShadows.None;

                EditorUtility.SetDirty(light);
                EditorUtility.SetDirty(lightObject);
                added++;
            }

            return added;
        }

        private static string FormatFirstLensSummary(IReadOnlyList<EyeMeshTemplate> meshTemplates)
        {
            foreach (var template in meshTemplates)
            {
                if (template.Name.IndexOf("Glowing_Eye_Lens", StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                return template.Name + "@" + FormatVector(template.LocalPosition);
            }

            return "None";
        }

        private static string FormatVector(Vector3 value)
        {
            return "(" +
                   value.x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                   value.y.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                   value.z.ToString("0.###", CultureInfo.InvariantCulture) +
                   ")";
        }

        private static bool IsApprovedSampleEyeName(string objectName)
        {
            return objectName.IndexOf("EyeSocket_Depression", StringComparison.Ordinal) >= 0 ||
                   objectName.IndexOf("EyeSocket_Raised_Rim", StringComparison.Ordinal) >= 0 ||
                   objectName.IndexOf("Glowing_Eye_Lens", StringComparison.Ordinal) >= 0 ||
                   objectName.IndexOf("Eye_Amber_Point_Light", StringComparison.Ordinal) >= 0;
        }

        private static Material SelectApprovedEyeMaterial(
            string objectName,
            Material bodyMaterial,
            Material eyeLensMaterial,
            Material eyeCoreMaterial)
        {
            if (objectName.IndexOf("Glowing_Eye_Lens", StringComparison.Ordinal) >= 0)
            {
                return eyeLensMaterial;
            }

            return bodyMaterial;
        }

        private static Vector3 DivideLossyScale(Vector3 value, Vector3 divisor)
        {
            return new Vector3(
                SafeDivide(value.x, divisor.x),
                SafeDivide(value.y, divisor.y),
                SafeDivide(value.z, divisor.z));
        }

        private static float SafeDivide(float value, float divisor)
        {
            return Mathf.Abs(divisor) <= 0.00001f ? value : value / divisor;
        }

        private static int AddApprovedEyes(
            Transform tergoRoot,
            Bounds localBounds,
            IReadOnlyList<Vector3> localVertices,
            Material socketMaterial,
            Material lensMaterial,
            Material coreMaterial,
            out int lightsAdded,
            out string placementSummary)
        {
            var eyeRoot = new GameObject(EyeContainerName);
            eyeRoot.transform.SetParent(tergoRoot, false);
            eyeRoot.transform.localPosition = Vector3.zero;
            eyeRoot.transform.localRotation = Quaternion.identity;
            eyeRoot.transform.localScale = Vector3.one;

            var height = localBounds.size.y;
            var eyeY = localBounds.min.y + height * 0.928f;
            var centerX = EstimateEyeCenterX(localVertices, localBounds, eyeY, height);
            var eyeSpacing = height * 0.044f;
            var headFrontZ = EstimateHeadFrontZ(localVertices, localBounds, eyeY);
            var leftSurfaceZ = EstimateLocalFrontZ(
                localVertices,
                centerX - eyeSpacing * 0.5f,
                eyeY,
                height * 0.035f,
                height * 0.05f,
                headFrontZ);
            var rightSurfaceZ = EstimateLocalFrontZ(
                localVertices,
                centerX + eyeSpacing * 0.5f,
                eyeY,
                height * 0.035f,
                height * 0.05f,
                headFrontZ);

            var added = 0;
            lightsAdded = 0;
            added += AddEyeSide(eyeRoot.transform, "L", centerX - eyeSpacing * 0.5f, eyeY, Mathf.Min(headFrontZ, leftSurfaceZ), height, 0.018f, 0.033f, 0.036f, socketMaterial, lensMaterial, coreMaterial, out var leftLights, out var leftLensZ);
            added += AddEyeSide(eyeRoot.transform, "R", centerX + eyeSpacing * 0.5f, eyeY, Mathf.Min(headFrontZ, rightSurfaceZ), height, 0.012f, 0.024f, 0.027f, socketMaterial, lensMaterial, coreMaterial, out var rightLights, out var rightLensZ);
            lightsAdded = leftLights + rightLights;

            placementSummary =
                tergoRoot.name +
                "[EyeY=" + eyeY.ToString("0.###", CultureInfo.InvariantCulture) +
                ", HeadFrontZ=" + headFrontZ.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftLensZ=" + leftLensZ.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightLensZ=" + rightLensZ.ToString("0.###", CultureInfo.InvariantCulture) +
                ", EyeSpacing=" + eyeSpacing.ToString("0.###", CultureInfo.InvariantCulture) +
                "]";
            EditorUtility.SetDirty(eyeRoot);
            return added;
        }

        private static int AddEyeSide(
            Transform parent,
            string side,
            float x,
            float y,
            float surfaceZ,
            float height,
            float socketOffset,
            float lensOffset,
            float coreOffset,
            Material socketMaterial,
            Material lensMaterial,
            Material coreMaterial,
            out int lightsAdded,
            out float lensZ)
        {
            var socketZ = surfaceZ - height * socketOffset;
            lensZ = surfaceZ - height * lensOffset;
            var coreZ = surfaceZ - height * coreOffset;

            CreateSphere(
                parent,
                $"Tergo_{side}_EyeSocket_Depression",
                new Vector3(x, y - height * 0.001f, socketZ),
                new Vector3(height * 0.0136f, height * 0.0180f, height * 0.0100f),
                socketMaterial);
            CreateSphere(
                parent,
                $"Tergo_{side}_Glowing_Eye_Lens",
                new Vector3(x, y, lensZ),
                new Vector3(height * 0.0094f, height * 0.0060f, height * 0.0064f),
                lensMaterial);
            CreateSphere(
                parent,
                $"Tergo_{side}_Eye_Hot_Core",
                new Vector3(x, y + height * 0.0005f, coreZ),
                new Vector3(height * 0.0031f, height * 0.0024f, height * 0.0022f),
                coreMaterial);

            var lightObject = new GameObject($"Tergo_{side}_Eye_Amber_Point_Light");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = new Vector3(x, y + height * 0.005f, coreZ - height * 0.010f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.62f, 0.24f, 1f);
            light.intensity = 0.05f;
            light.range = Mathf.Max(height * 0.12f, 0.05f);
            light.shadows = LightShadows.None;
            EditorUtility.SetDirty(lightObject);

            lightsAdded = 1;
            return 4;
        }

        private static void CreateSphere(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.SetParent(parent, false);
            sphere.transform.localPosition = localPosition;
            sphere.transform.localRotation = Quaternion.identity;
            sphere.transform.localScale = localScale;

            var collider = sphere.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            var renderer = sphere.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                EditorUtility.SetDirty(renderer);
            }

            EditorUtility.SetDirty(sphere);
        }

        private static List<Vector3> CollectLocalVertices(Transform root, IReadOnlyList<Renderer> renderers)
        {
            var vertices = new List<Vector3>();

            foreach (var renderer in renderers)
            {
                if (renderer is SkinnedMeshRenderer skinnedRenderer)
                {
                    var bakedMesh = new Mesh
                    {
                        name = skinnedRenderer.name + "_BakedForTergoEyePlacement"
                    };

                    try
                    {
                        skinnedRenderer.BakeMesh(bakedMesh);
                        foreach (var vertex in bakedMesh.vertices)
                        {
                            vertices.Add(root.InverseTransformPoint(skinnedRenderer.transform.TransformPoint(vertex)));
                        }
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(bakedMesh);
                    }

                    continue;
                }

                var meshFilter = renderer.GetComponent<MeshFilter>();
                var sharedMesh = meshFilter != null ? meshFilter.sharedMesh : null;
                if (sharedMesh == null)
                {
                    continue;
                }

                foreach (var vertex in sharedMesh.vertices)
                {
                    vertices.Add(root.InverseTransformPoint(meshFilter.transform.TransformPoint(vertex)));
                }
            }

            if (vertices.Count == 0)
            {
                throw new InvalidOperationException(root.name + " has no vertices for Tergo eye placement.");
            }

            return vertices;
        }

        private static Bounds CalculateLocalBounds(IReadOnlyList<Vector3> localVertices, string objectName)
        {
            var bounds = new Bounds(localVertices[0], Vector3.zero);
            for (var i = 1; i < localVertices.Count; i++)
            {
                bounds.Encapsulate(localVertices[i]);
            }

            if (bounds.size.y <= 0.0001f)
            {
                throw new InvalidOperationException(objectName + " body bounds could not be calculated.");
            }

            return bounds;
        }

        private static float EstimateEyeCenterX(IReadOnlyList<Vector3> localVertices, Bounds localBounds, float eyeY, float height)
        {
            var found = false;
            var minX = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;

            foreach (var point in localVertices)
            {
                if (Mathf.Abs(point.y - eyeY) > height * 0.055f)
                {
                    continue;
                }

                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                found = true;
            }

            return found ? (minX + maxX) * 0.5f : localBounds.center.x;
        }

        private static float EstimateHeadFrontZ(IReadOnlyList<Vector3> localVertices, Bounds localBounds, float eyeY)
        {
            var height = localBounds.size.y;
            var yMin = localBounds.min.y + height * 0.80f;
            var yMax = localBounds.min.y + height * 0.97f;
            var centerX = localBounds.center.x;
            var xLimit = localBounds.size.x * 0.24f;
            var found = false;
            var frontZ = float.PositiveInfinity;

            foreach (var point in localVertices)
            {
                if (point.y < yMin || point.y > yMax || Mathf.Abs(point.x - centerX) > xLimit)
                {
                    continue;
                }

                frontZ = Mathf.Min(frontZ, point.z);
                found = true;
            }

            if (found)
            {
                return frontZ;
            }

            return localBounds.min.z;
        }

        private static float EstimateLocalFrontZ(
            IReadOnlyList<Vector3> localVertices,
            float x,
            float y,
            float radiusX,
            float radiusY,
            float fallback)
        {
            var found = false;
            var frontZ = float.PositiveInfinity;

            foreach (var point in localVertices)
            {
                if (Mathf.Abs(point.x - x) > radiusX || Mathf.Abs(point.y - y) > radiusY)
                {
                    continue;
                }

                frontZ = Mathf.Min(frontZ, point.z);
                found = true;
            }

            return found ? frontZ : fallback;
        }

        private static Bounds CalculateLocalBoundsFromRendererBounds(Transform root, IReadOnlyList<Renderer> renderers)
        {
            var hasBounds = false;
            var localBounds = new Bounds(Vector3.zero, Vector3.zero);

            foreach (var renderer in renderers)
            {
                var bounds = renderer.bounds;
                foreach (var corner in EnumerateBoundsCorners(bounds))
                {
                    var localCorner = root.InverseTransformPoint(corner);
                    if (!hasBounds)
                    {
                        localBounds = new Bounds(localCorner, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(localCorner);
                    }
                }
            }

            if (!hasBounds || localBounds.size.y <= 0.0001f)
            {
                throw new InvalidOperationException(root.name + " body bounds could not be calculated.");
            }

            return localBounds;
        }

        private static IEnumerable<Vector3> EnumerateBoundsCorners(Bounds bounds)
        {
            var min = bounds.min;
            var max = bounds.max;
            yield return new Vector3(min.x, min.y, min.z);
            yield return new Vector3(min.x, min.y, max.z);
            yield return new Vector3(min.x, max.y, min.z);
            yield return new Vector3(min.x, max.y, max.z);
            yield return new Vector3(max.x, min.y, min.z);
            yield return new Vector3(max.x, min.y, max.z);
            yield return new Vector3(max.x, max.y, min.z);
            yield return new Vector3(max.x, max.y, max.z);
        }

        private static void RemoveExistingEyeContainer(Transform tergoRoot)
        {
            var existingContainers = new List<GameObject>();
            foreach (var transform in tergoRoot.GetComponentsInChildren<Transform>(true))
            {
                if (transform != tergoRoot && string.Equals(transform.name, EyeContainerName, StringComparison.Ordinal))
                {
                    existingContainers.Add(transform.gameObject);
                }
            }

            foreach (var existing in existingContainers)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static bool IsGeneratedEyeObject(Transform transform)
        {
            var current = transform;
            while (current != null)
            {
                if (string.Equals(current.name, EyeContainerName, StringComparison.Ordinal))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static GameObject RequireSceneObject(string objectName)
        {
            var target = GameObject.Find(objectName);
            if (target != null)
            {
                return target;
            }

            foreach (var candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (!string.Equals(candidate.name, objectName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (EditorUtility.IsPersistent(candidate))
                {
                    continue;
                }

                if (!candidate.scene.IsValid() || !string.Equals(candidate.scene.path, CargoRunScenePath, StringComparison.Ordinal))
                {
                    continue;
                }

                return candidate;
            }

            throw new InvalidOperationException("Missing required object in CargoRunMvp scene: " + objectName);
        }

        private static Bounds CalculateWorldBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var hasBounds = false;
            var bounds = new Bounds(root.position, Vector3.zero);
            foreach (var renderer in renderers)
            {
                if (!renderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds)
            {
                throw new InvalidOperationException(root.name + " has no renderers for eye shape capture.");
            }

            return bounds;
        }

        private static Vector3 CalculateEyeCenter(Transform root, Bounds fallbackBounds)
        {
            var left = FindDescendantByName(root, "Tergo_L_Glowing_Eye_Lens");
            var right = FindDescendantByName(root, "Tergo_R_Glowing_Eye_Lens");
            if (left != null && right != null)
            {
                return (left.position + right.position) * 0.5f;
            }

            return new Vector3(fallbackBounds.center.x, fallbackBounds.max.y - fallbackBounds.size.y * 0.18f, fallbackBounds.center.z);
        }

        private static Transform FindDescendantByName(Transform root, string objectName)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(transform.name, objectName, StringComparison.Ordinal))
                {
                    return transform;
                }
            }

            return null;
        }

        private static void CaptureTergoEyeShape(
            Transform target,
            Bounds bounds,
            Vector3 eyeCenter,
            string outputPath,
            bool front)
        {
            var cameraObject = new GameObject(front ? "Tergo Eye Shape Front Capture Camera" : "Tergo Eye Shape Side Capture Camera");
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(bounds.size.y * 0.31f, 0.45f);
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 50f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f);
                camera.allowHDR = false;
                camera.allowMSAA = true;

                var direction = front
                    ? target.TransformDirection(Vector3.forward).normalized
                    : target.TransformDirection(Vector3.right).normalized;
                var distance = Mathf.Max(bounds.size.magnitude * 0.75f, 1.4f);
                camera.transform.position = eyeCenter + direction * distance;
                camera.transform.LookAt(eyeCenter, Vector3.up);

                CaptureCamera(camera, outputPath, 1920, 1080);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void CaptureCamera(Camera camera, string outputPath, int width, int height)
        {
            var previousActiveTexture = RenderTexture.active;
            var previousTargetTexture = camera.targetTexture;
            var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;

                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            finally
            {
                camera.targetTexture = previousTargetTexture;
                RenderTexture.active = previousActiveTexture;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static void BuildSideBySideImage(string leftPath, string rightPath, string outputPath)
        {
            var left = LoadTexture(leftPath);
            var right = LoadTexture(rightPath);
            try
            {
                var width = left.width + right.width;
                var height = Mathf.Max(left.height, right.height);
                var output = new Texture2D(width, height, TextureFormat.RGBA32, false);
                var background = new Color(0.08f, 0.08f, 0.08f, 1f);
                var fill = new Color[width * height];
                for (var i = 0; i < fill.Length; i++)
                {
                    fill[i] = background;
                }

                output.SetPixels(fill);
                output.SetPixels(0, height - left.height, left.width, left.height, left.GetPixels());
                output.SetPixels(left.width, height - right.height, right.width, right.height, right.GetPixels());
                output.Apply();
                File.WriteAllBytes(outputPath, output.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(output);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(left);
                UnityEngine.Object.DestroyImmediate(right);
            }
        }

        private static Texture2D LoadTexture(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Missing Tergo comparison image.", path);
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path)))
            {
                throw new InvalidOperationException("Could not load Tergo comparison image: " + path);
            }

            return texture;
        }

        private readonly struct TransformState
        {
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            private TransformState(Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
            {
                this.localPosition = localPosition;
                this.localRotation = localRotation;
                this.localScale = localScale;
            }

            public static TransformState Capture(Transform transform)
            {
                return new TransformState(transform.localPosition, transform.localRotation, transform.localScale);
            }

            public bool Matches(Transform transform)
            {
                return Vector3.Distance(localPosition, transform.localPosition) <= 0.0001f &&
                       Quaternion.Angle(localRotation, transform.localRotation) <= 0.001f &&
                       Vector3.Distance(localScale, transform.localScale) <= 0.0001f;
            }
        }

        private sealed class ApprovedEyeTemplates
        {
            public readonly List<EyeMeshTemplate> MeshTemplates = new();
            public readonly List<EyeLightTemplate> LightTemplates = new();
        }

        private readonly struct EyeMeshTemplate
        {
            public readonly string Name;
            public readonly Mesh Mesh;
            public readonly Vector3 LocalPosition;
            public readonly Quaternion LocalRotation;
            public readonly Vector3 LocalScale;
            public readonly Material Material;

            public EyeMeshTemplate(
                string name,
                Mesh mesh,
                Vector3 localPosition,
                Quaternion localRotation,
                Vector3 localScale,
                Material material)
            {
                Name = name;
                Mesh = mesh;
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
                Material = material;
            }
        }

        private readonly struct EyeLightTemplate
        {
            public readonly string Name;
            public readonly Vector3 LocalPosition;
            public readonly Quaternion LocalRotation;
            public readonly Color Color;
            public readonly float Intensity;
            public readonly float Range;

            public EyeLightTemplate(
                string name,
                Vector3 localPosition,
                Quaternion localRotation,
                Color color,
                float intensity,
                float range)
            {
                Name = name;
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                Color = color;
                Intensity = intensity;
                Range = range;
            }
        }
    }
}
