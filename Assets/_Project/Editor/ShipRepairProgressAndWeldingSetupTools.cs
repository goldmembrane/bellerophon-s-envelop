using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bellerophon.Repair;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class ShipRepairProgressAndWeldingSetupTools
    {
        internal const string ScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        internal const string ApprovedSourcePath =
            "artSample/ShipRepairProgressAndWelding/WeldingVFX_TransparentSource.png";
        internal const string WeldingSpritePath =
            "Assets/_Project/Art/VFX/RepairWelding/WeldingVFX_Approved.png";
        internal const string WeldingAnimatedShaderPath =
            "Assets/_Project/Art/VFX/RepairWelding/WeldingApprovedSampleAnimated.shader";
        internal const string WeldingAnimatedMaterialPath =
            "Assets/_Project/Art/VFX/RepairWelding/WeldingApprovedSampleAnimated.mat";
        internal const string ShipTargetName = "ShipRepair";
        internal const string SabotageTargetName = "SabotageRepair";
        internal const string ProgressObjectName = "ApprovedRepairProgressBar";
        internal const string WeldingObjectName = "ApprovedRepairWeldingVfx";
        internal const string ValidationDirectory =
            "docs/validation/ShipRepairProgressAndWelding";
        internal const string FinalImageRelativePath =
            ValidationDirectory + "/Final.png";
        internal const string FinalReportRelativePath =
            ValidationDirectory + "/Final.txt";

        internal static string FinalImageAbsolutePath =>
            Absolute(FinalImageRelativePath);

        internal static void InspectSources()
        {
            RequireFile(ApprovedSourcePath);
            RequireFile(WeldingSpritePath);
            RequireFile(WeldingAnimatedShaderPath);
            RequireFile(ScenePath);
            RequireEqual(
                Sha256(Absolute(ApprovedSourcePath)),
                Sha256(Absolute(WeldingSpritePath)),
                "approved welding source binary copy");

            Scene scene = RequireScene();
            RequireTarget(scene, ShipTargetName);
            RequireTarget(scene, SabotageTargetName);
        }

        internal static void Apply()
        {
            InspectSources();
            InvalidatePreviousFinalEvidence();
            DeleteLegacyParticleAssets();
            ConfigureSpriteImporter();
            Material animatedMaterial = CreateOrUpdateApprovedSampleMaterial();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WeldingSpritePath) ??
                throw new InvalidOperationException(
                    "Approved welding sprite did not import as a Sprite.");

            Scene scene = RequireScene();
            GameObject ship = RequireTarget(scene, ShipTargetName);
            GameObject sabotage = RequireTarget(scene, SabotageTargetName);
            TargetSnapshot shipBefore = TargetSnapshot.Capture(ship);
            TargetSnapshot sabotageBefore = TargetSnapshot.Capture(sabotage);

            ApplyProgressBar(ship);
            ApplyProgressBar(sabotage);
            ApplyWeldingVfx(
                ship,
                sprite,
                animatedMaterial);
            RemoveGeneratedChild(sabotage.transform, WeldingObjectName);

            shipBefore.RequireUnchanged(ship);
            sabotageBefore.RequireUnchanged(sabotage);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after repair presentation setup.");
            }

            AssetDatabase.SaveAssets();
        }

        internal static void Inspect()
        {
            InspectSources();
            Scene scene = RequireScene();
            GameObject ship = RequireTarget(scene, ShipTargetName);
            GameObject sabotage = RequireTarget(scene, SabotageTargetName);

            RequireProgressBar(ship);
            RequireProgressBar(sabotage);
            RepairWeldingVfxPresenter welding = RequireSingleComponent<RepairWeldingVfxPresenter>(ship);
            if (welding.OwnerRoot != ship.transform)
            {
                throw new InvalidOperationException(
                    "ShipRepair welding VFX owner does not reference ShipRepair.");
            }

            if (!welding.HasFixedWorldPosition)
            {
                throw new InvalidOperationException(
                    "ShipRepair welding VFX has no fixed world position.");
            }

            if (!welding.HasAnimatedVfx)
            {
                throw new InvalidOperationException(
                    "ShipRepair welding VFX animation layers are incomplete.");
            }

            if (Vector3.Distance(
                    welding.transform.position,
                    welding.FixedWorldPosition) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "ShipRepair welding VFX is not at its fixed world position.");
            }

            RequireApproximately(
                welding.ForwardDistanceMeters,
                RepairWeldingVfxPresenter.DesignForwardDistanceMeters,
                0.0001f,
                "ShipRepair welding forward distance");
            RequireApproximately(
                welding.CurrentForwardDistance(),
                RepairWeldingVfxPresenter.DesignForwardDistanceMeters,
                0.002f,
                "ShipRepair welding current world forward distance");
            RequireApproximately(
                welding.WorldHeightMeters,
                RepairWeldingVfxPresenter.DesignWorldHeightMeters,
                0.0001f,
                "ShipRepair welding approved world height");
            if (welding.GetComponent<SpriteRenderer>() != null)
            {
                throw new InvalidOperationException(
                    "ShipRepair welding VFX root must not render the static approved PNG.");
            }

            RequireApprovedSampleLayerSetup(welding);

            if (SabotageHasWelding(sabotage))
            {
                throw new InvalidOperationException(
                    "SabotageRepair must not contain the ShipRepair-only welding VFX.");
            }
        }

        internal static Texture2D RenderTarget(GameObject target)
        {
            const int width = 500;
            const int height = 620;
            GameObject cameraObject = new GameObject(
                "RepairPresentation_ReviewCamera", typeof(Camera));
            GameObject lightObject = new GameObject(
                "RepairPresentation_ReviewLight", typeof(Light));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = cameraObject.GetComponent<Camera>();
            Light light = lightObject.GetComponent<Light>();
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            try
            {
                Bounds bounds = BoundsOf(target.transform);
                Vector3 front = target.transform.forward.normalized;
                float distance = Mathf.Max(3f, bounds.extents.magnitude * 3f);
                camera.transform.position = bounds.center + front * distance;
                camera.transform.rotation = Quaternion.LookRotation(
                    bounds.center - camera.transform.position,
                    Vector3.up);

                RepairProgressBarPresenter progress =
                    RequireSingleComponent<RepairProgressBarPresenter>(target);
                progress.RefreshForCamera(camera);
                RepairWeldingVfxPresenter welding =
                    target.GetComponentInChildren<RepairWeldingVfxPresenter>(true);
                if (welding != null)
                {
                    welding.RefreshForCamera(camera);
                }

                bounds = BoundsOf(target.transform);
                distance = Mathf.Max(3f, bounds.extents.magnitude * 3f);
                camera.transform.position = bounds.center + front * distance;
                camera.transform.rotation = Quaternion.LookRotation(
                    bounds.center - camera.transform.position,
                    Vector3.up);
                progress.RefreshForCamera(camera);
                if (welding != null)
                {
                    welding.RefreshForCamera(camera);
                }

                camera.orthographic = true;
                float aspect = width / (float)height;
                camera.orthographicSize = Mathf.Max(
                    bounds.extents.y * 1.22f,
                    bounds.extents.x / aspect * 1.22f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = distance + bounds.extents.magnitude * 4f;
                camera.allowHDR = false;
                camera.allowMSAA = true;

                light.type = LightType.Directional;
                light.intensity = 1.1f;
                light.color = Color.white;
                light.transform.rotation = Quaternion.Euler(35f, 150f, 0f);

                renderTexture = RenderTexture.GetTemporary(
                    width, height, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                RenderTexture previous = RenderTexture.active;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture = new Texture2D(
                    width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply(false, false);
                RenderTexture.active = previous;
                camera.targetTexture = null;
                return texture;
            }
            catch
            {
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                throw;
            }
            finally
            {
                if (renderTexture != null)
                {
                    RenderTexture.ReleaseTemporary(renderTexture);
                }

                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        internal static Texture2D CombinePanels(Texture2D ship, Texture2D sabotage)
        {
            const int gap = 8;
            Texture2D sheet = new Texture2D(
                ship.width + sabotage.width + gap,
                Mathf.Max(ship.height, sabotage.height),
                TextureFormat.RGBA32,
                false);
            Color32[] black = Enumerable.Repeat(
                (Color32)Color.black,
                sheet.width * sheet.height).ToArray();
            sheet.SetPixels32(black);
            sheet.SetPixels(0, 0, ship.width, ship.height, ship.GetPixels());
            sheet.SetPixels(
                ship.width + gap,
                0,
                sabotage.width,
                sabotage.height,
                sabotage.GetPixels());
            sheet.Apply(false, false);
            return sheet;
        }

        internal static Texture2D CombineAnimationPanels(
            Texture2D phaseA,
            Texture2D phaseB,
            Texture2D phaseC,
            Texture2D sabotage)
        {
            const int gap = 8;
            int panelWidth = phaseA.width;
            int panelHeight = phaseA.height;
            Texture2D sheet = new Texture2D(
                panelWidth * 2 + gap,
                panelHeight * 2 + gap,
                TextureFormat.RGBA32,
                false);
            Color32[] black = Enumerable.Repeat(
                (Color32)Color.black,
                sheet.width * sheet.height).ToArray();
            sheet.SetPixels32(black);
            sheet.SetPixels(
                0,
                panelHeight + gap,
                panelWidth,
                panelHeight,
                phaseA.GetPixels());
            sheet.SetPixels(
                panelWidth + gap,
                panelHeight + gap,
                panelWidth,
                panelHeight,
                phaseB.GetPixels());
            sheet.SetPixels(0, 0, panelWidth, panelHeight, phaseC.GetPixels());
            sheet.SetPixels(
                panelWidth + gap,
                0,
                panelWidth,
                panelHeight,
                sabotage.GetPixels());
            sheet.Apply(false, false);
            return sheet;
        }

        internal static void WriteFinalEvidence(
            Texture2D sheet,
            float shipProgress,
            float sabotageProgress,
            float maximumProgressDifference,
            float maximumWeldingWorldPositionDrift,
            Vector3 weldingFixedWorldPosition,
            float minimumArcIntensity,
            float maximumArcIntensity,
            float maximumSparkTravelNormalized,
            float maximumSmokeTravelNormalized,
            int approvedSampleLayerCount,
            int consoleErrorsBefore)
        {
            string imagePath = FinalImageAbsolutePath;
            string reportPath = Absolute(FinalReportRelativePath);
            if (File.Exists(imagePath) || File.Exists(reportPath))
            {
                throw new InvalidOperationException(
                    "Repair presentation one-time final evidence already exists.");
            }

            int consoleErrorsAfter = LightsaberSetupTools.ConsoleErrorCount();
            if (consoleErrorsAfter > consoleErrorsBefore)
            {
                throw new InvalidOperationException(
                    "Unity console gained errors during repair presentation review.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(imagePath) ??
                throw new InvalidOperationException(
                    "Repair presentation validation directory is unavailable."));
            File.WriteAllBytes(imagePath, sheet.EncodeToPNG());
            File.WriteAllText(
                reportPath,
                new StringBuilder()
                    .AppendLine("ShipRepair/SabotageRepair approved progress and welding final review")
                    .AppendLine("naturalPlayMode=True")
                    .AppendLine("verificationTargetsManipulated=False")
                    .AppendLine("approvedArtSampleReproduced=True")
                    .AppendLine("targets=ShipRepair,SabotageRepair")
                    .AppendLine("progressDurationSeconds=5")
                    .AppendLine("fullCycleAndRestartObserved=True")
                    .AppendLine("shipProgressAtCapture=" + Num(shipProgress))
                    .AppendLine("sabotageProgressAtCapture=" + Num(sabotageProgress))
                    .AppendLine("maximumProgressDifference=" + Num(maximumProgressDifference))
                    .AppendLine("weldingTarget=ShipRepair")
                    .AppendLine("weldingForwardDistanceMeters=0.5")
                    .AppendLine("weldingWorldPositionFixed=True")
                    .AppendLine("weldingMaximumWorldPositionDriftMeters=" +
                        Num(maximumWeldingWorldPositionDrift))
                    .AppendLine("weldingFixedWorldPosition=" +
                        Num(weldingFixedWorldPosition.x) + "," +
                        Num(weldingFixedWorldPosition.y) + "," +
                        Num(weldingFixedWorldPosition.z))
                    .AppendLine("weldingAnimatedVfx=True")
                    .AppendLine("weldingAnimationLoopSeconds=" +
                        Num(RepairWeldingVfxPresenter.DesignAnimationLoopSeconds))
                    .AppendLine("weldingNaturalVisualPhaseCaptures=3")
                    .AppendLine("weldingSourceOnlyApprovedPng=True")
                    .AppendLine("weldingGeneratedParticleAssets=False")
                    .AppendLine("weldingParticleSystems=0")
                    .AppendLine("weldingApprovedSampleSpriteLayers=" +
                        approvedSampleLayerCount)
                    .AppendLine("weldingArcIntensityRange=" +
                        Num(minimumArcIntensity) + ".." +
                        Num(maximumArcIntensity))
                    .AppendLine("weldingMaximumSparkTravelNormalized=" +
                        Num(maximumSparkTravelNormalized))
                    .AppendLine("weldingMaximumSmokeTravelNormalized=" +
                        Num(maximumSmokeTravelNormalized))
                    .AppendLine("approvedWeldingSourceSha256=" +
                        Sha256(Absolute(WeldingSpritePath)))
                    .AppendLine("unityConsoleNewErrors=0")
                    .AppendLine("directVisualReviewPrimary=True")
                    .AppendLine("numericMetricsSecondary=True")
                    .ToString(),
                new UTF8Encoding(false));
        }

        internal static void RequireRuntimeSetup(
            out GameObject ship,
            out GameObject sabotage,
            out RepairProgressBarPresenter shipProgress,
            out RepairProgressBarPresenter sabotageProgress,
            out RepairWeldingVfxPresenter welding)
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(
                    "Repair presentation runtime review requires Play Mode.");
            }

            Scene scene = RequireScene();
            ship = RequireTarget(scene, ShipTargetName);
            sabotage = RequireTarget(scene, SabotageTargetName);
            shipProgress = RequireSingleComponent<RepairProgressBarPresenter>(ship);
            sabotageProgress = RequireSingleComponent<RepairProgressBarPresenter>(sabotage);
            welding = RequireSingleComponent<RepairWeldingVfxPresenter>(ship);
        }

        private static void ApplyProgressBar(GameObject target)
        {
            RemoveGeneratedChild(target.transform, ProgressObjectName);
            Animator animator = RequireAnimator(target);
            Transform head = RequireRigTransform(
                target,
                animator,
                HumanBodyBones.Head,
                "Head");
            float headTop = VisibleBoundsOf(target.transform).max.y;
            GameObject barObject = new GameObject(
                ProgressObjectName,
                typeof(MeshFilter),
                typeof(MeshRenderer),
                typeof(RepairProgressBarPresenter));
            barObject.layer = target.layer;
            barObject.transform.SetParent(target.transform, false);
            RepairProgressBarPresenter presenter =
                barObject.GetComponent<RepairProgressBarPresenter>();
            presenter.Configure(
                head,
                headTop,
                RepairProgressBarPresenter.DesignDurationSeconds,
                true);
            EditorUtility.SetDirty(presenter);
        }

        private static void ApplyWeldingVfx(
            GameObject ship,
            Sprite sprite,
            Material animatedMaterial)
        {
            Animator animator = RequireAnimator(ship);
            Transform leftHand = RequireRigTransform(
                ship,
                animator,
                HumanBodyBones.LeftHand,
                "LeftHand");
            Transform rightHand = RequireRigTransform(
                ship,
                animator,
                HumanBodyBones.RightHand,
                "RightHand");
            RepairWeldingVfxPresenter existing =
                ship.GetComponentInChildren<RepairWeldingVfxPresenter>(true);
            Vector3 fixedWorldPosition;
            if (existing != null)
            {
                fixedWorldPosition = existing.HasFixedWorldPosition
                    ? existing.FixedWorldPosition
                    : existing.transform.position;
            }
            else
            {
                Vector3 handMidpoint = (leftHand.position + rightHand.position) * 0.5f;
                Vector3 ownerLocalMidpoint = ship.transform.InverseTransformPoint(handMidpoint);
                ownerLocalMidpoint.z = RepairWeldingVfxPresenter.DesignForwardDistanceMeters;
                fixedWorldPosition = ship.transform.TransformPoint(ownerLocalMidpoint);
            }

            RemoveGeneratedChild(ship.transform, WeldingObjectName);
            GameObject vfxObject = new GameObject(
                WeldingObjectName,
                typeof(RepairWeldingVfxPresenter));
            vfxObject.layer = ship.layer;
            vfxObject.transform.SetParent(ship.transform, false);
            RepairWeldingVfxPresenter presenter =
                vfxObject.GetComponent<RepairWeldingVfxPresenter>();
            SpriteRenderer arcLayer = CreateApprovedSampleLayer(
                vfxObject.transform,
                "ApprovedSample_Arc",
                sprite,
                animatedMaterial);
            SpriteRenderer[] sparkLayers =
            {
                CreateApprovedSampleLayer(
                    vfxObject.transform,
                    "ApprovedSample_Sparks_A",
                    sprite,
                    animatedMaterial),
                CreateApprovedSampleLayer(
                    vfxObject.transform,
                    "ApprovedSample_Sparks_B",
                    sprite,
                    animatedMaterial)
            };
            SpriteRenderer[] smokeLayers =
            {
                CreateApprovedSampleLayer(
                    vfxObject.transform,
                    "ApprovedSample_Smoke_A",
                    sprite,
                    animatedMaterial),
                CreateApprovedSampleLayer(
                    vfxObject.transform,
                    "ApprovedSample_Smoke_B",
                    sprite,
                    animatedMaterial)
            };
            presenter.Configure(
                ship.transform,
                sprite,
                fixedWorldPosition,
                arcLayer,
                sparkLayers,
                smokeLayers,
                RepairWeldingVfxPresenter.DesignForwardDistanceMeters,
                RepairWeldingVfxPresenter.DesignWorldHeightMeters);
            EditorUtility.SetDirty(presenter);
        }

        private static SpriteRenderer CreateApprovedSampleLayer(
            Transform parent,
            string name,
            Sprite sprite,
            Material material)
        {
            GameObject layerObject = new GameObject(name, typeof(SpriteRenderer));
            layerObject.transform.SetParent(parent, false);
            SpriteRenderer renderer = layerObject.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sharedMaterial = material;
            return renderer;
        }


        private static void DeleteLegacyParticleAssets()
        {
            string[] legacyPaths =
            {
                "Assets/_Project/Art/VFX/RepairWelding/WeldingParticleStar.png",
                "Assets/_Project/Art/VFX/RepairWelding/WeldingParticleSoft.png",
                "Assets/_Project/Art/VFX/RepairWelding/WeldingAdditive.mat",
                "Assets/_Project/Art/VFX/RepairWelding/WeldingSmoke.mat"
            };
            foreach (string path in legacyPaths)
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
        }

        private static Material CreateOrUpdateApprovedSampleMaterial()
        {
            Shader shader = Shader.Find(
                "Bellerophon/Repair/WeldingApprovedSampleAnimated") ??
                throw new InvalidOperationException(
                    "Approved welding sample animation shader is unavailable.");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                WeldingAnimatedMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, WeldingAnimatedMaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.name = "WeldingApprovedSampleAnimated";
            material.SetColor("_Color", Color.white);
            material.SetFloat(
                "_LoopSeconds",
                RepairWeldingVfxPresenter.DesignAnimationLoopSeconds);
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureSpriteImporter()
        {
            AssetDatabase.ImportAsset(
                WeldingSpritePath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(WeldingSpritePath) as
                TextureImporter ?? throw new InvalidOperationException(
                    "Approved welding texture importer is unavailable.");
            bool changed = false;
            changed |= SetIfDifferent(
                importer.textureType,
                TextureImporterType.Sprite,
                value => importer.textureType = value);
            changed |= SetIfDifferent(
                importer.spriteImportMode,
                SpriteImportMode.Single,
                value => importer.spriteImportMode = value);
            changed |= SetIfDifferent(
                importer.spritePixelsPerUnit,
                1000f,
                value => importer.spritePixelsPerUnit = value);
            changed |= SetIfDifferent(
                importer.mipmapEnabled,
                false,
                value => importer.mipmapEnabled = value);
            changed |= SetIfDifferent(
                importer.alphaIsTransparency,
                true,
                value => importer.alphaIsTransparency = value);
            changed |= SetIfDifferent(
                importer.sRGBTexture,
                true,
                value => importer.sRGBTexture = value);
            changed |= SetIfDifferent(
                importer.filterMode,
                FilterMode.Bilinear,
                value => importer.filterMode = value);
            changed |= SetIfDifferent(
                importer.textureCompression,
                TextureImporterCompression.Uncompressed,
                value => importer.textureCompression = value);
            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static void InvalidatePreviousFinalEvidence()
        {
            string imagePath = FinalImageAbsolutePath;
            string reportPath = Absolute(FinalReportRelativePath);
            if (File.Exists(imagePath))
            {
                File.Delete(imagePath);
            }

            if (File.Exists(reportPath))
            {
                File.Delete(reportPath);
            }
        }

        private static bool SetIfDifferent<T>(T actual, T expected, Action<T> setter)
        {
            if (EqualityComparer<T>.Default.Equals(actual, expected))
            {
                return false;
            }

            setter(expected);
            return true;
        }

        private static void RequireProgressBar(GameObject target)
        {
            RepairProgressBarPresenter progress =
                RequireSingleComponent<RepairProgressBarPresenter>(target);
            if (progress.HeadAnchor == null)
            {
                throw new InvalidOperationException(
                    target.name + " repair progress head anchor is missing.");
            }

            RequireApproximately(
                progress.DurationSeconds,
                RepairProgressBarPresenter.DesignDurationSeconds,
                0.0001f,
                target.name + " progress duration");
            RequireApproximately(
                progress.EditModePreviewProgress,
                0.5f,
                0.0001f,
                target.name + " approved preview progress");
            if (!progress.RestartOnCompletion)
            {
                throw new InvalidOperationException(
                    target.name + " repair progress must restart after five seconds.");
            }
        }

        private static void RequireApprovedSampleLayerSetup(
            RepairWeldingVfxPresenter welding)
        {
            ParticleSystem[] particles =
                welding.GetComponentsInChildren<ParticleSystem>(true);
            if (particles.Length != 0)
            {
                throw new InvalidOperationException(
                    "ShipRepair welding VFX still contains inferred particle systems.");
            }

            SpriteRenderer[] layers =
                welding.GetComponentsInChildren<SpriteRenderer>(true);
            if (welding.GetComponent<SpriteRenderer>() != null)
            {
                throw new InvalidOperationException(
                    "ShipRepair welding VFX still renders the static approved PNG on its root.");
            }
            if (layers.Length != 5 || welding.ApprovedSampleLayerCount != 5)
            {
                throw new InvalidOperationException(
                    "ShipRepair welding VFX requires five dynamic approved-sample layers; actual=" +
                    layers.Length);
            }

            foreach (SpriteRenderer layer in layers)
            {
                if (layer.sprite == null ||
                    AssetDatabase.GetAssetPath(layer.sprite) != WeldingSpritePath)
                {
                    throw new InvalidOperationException(
                        layer.name + " does not source pixels from the approved welding image.");
                }
                if (AssetDatabase.GetAssetPath(layer.sharedMaterial) !=
                    WeldingAnimatedMaterialPath)
                {
                    throw new InvalidOperationException(
                        layer.name + " does not use the approved-sample animation material.");
                }
            }

            string[] names = layers.Select(layer => layer.name).OrderBy(name => name).ToArray();
            string[] expectedNames =
            {
                "ApprovedSample_Arc",
                "ApprovedSample_Smoke_A",
                "ApprovedSample_Smoke_B",
                "ApprovedSample_Sparks_A",
                "ApprovedSample_Sparks_B"
            };
            if (!names.SequenceEqual(expectedNames))
            {
                throw new InvalidOperationException(
                    "ShipRepair welding approved-sample layer names are incomplete.");
            }

            string[] forbiddenAssets =
            {
                "Assets/_Project/Art/VFX/RepairWelding/WeldingParticleStar.png",
                "Assets/_Project/Art/VFX/RepairWelding/WeldingParticleSoft.png",
                "Assets/_Project/Art/VFX/RepairWelding/WeldingAdditive.mat",
                "Assets/_Project/Art/VFX/RepairWelding/WeldingSmoke.mat"
            };
            if (forbiddenAssets.Any(path => AssetDatabase.LoadMainAssetAtPath(path) != null))
            {
                throw new InvalidOperationException(
                    "Legacy inferred welding particle assets still exist.");
            }
        }

        private static bool SabotageHasWelding(GameObject sabotage)
        {
            return sabotage.GetComponentsInChildren<RepairWeldingVfxPresenter>(true)
                .Length != 0;
        }

        private static Bounds VisibleBoundsOf(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer =>
                    renderer.enabled &&
                    renderer.gameObject.activeInHierarchy &&
                    renderer.GetComponentInParent<RepairProgressBarPresenter>() == null &&
                    renderer.GetComponentInParent<RepairWeldingVfxPresenter>() == null)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(root.name + " has no visible renderer.");
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static Bounds BoundsOf(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(root.name + " has no visible renderer.");
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static T RequireSingleComponent<T>(GameObject target)
            where T : Component
        {
            T[] components = target.GetComponentsInChildren<T>(true);
            if (components.Length != 1)
            {
                throw new InvalidOperationException(
                    target.name + " requires exactly one " + typeof(T).Name +
                    "; actual=" + components.Length);
            }

            return components[0];
        }

        private static Animator RequireAnimator(GameObject target)
        {
            Animator animator = target.GetComponent<Animator>() ??
                target.GetComponentInChildren<Animator>(true) ??
                throw new InvalidOperationException(target.name + " Animator is missing.");
            return animator;
        }

        private static Transform RequireRigTransform(
            GameObject target,
            Animator animator,
            HumanBodyBones humanBone,
            string exactName)
        {
            if (animator.avatar != null && animator.avatar.isHuman)
            {
                Transform humanoidBone = animator.GetBoneTransform(humanBone);
                if (humanoidBone != null)
                {
                    return humanoidBone;
                }
            }

            Transform[] candidates = target.GetComponentsInChildren<Transform>(true)
                .Where(transform =>
                    string.Equals(transform.name, exactName, StringComparison.Ordinal) ||
                    string.Equals(
                        transform.name,
                        "mixamorig:" + exactName,
                        StringComparison.Ordinal))
                .OrderByDescending(transform => RigPath(transform, target.transform)
                    .IndexOf("Armature/Hips/", StringComparison.OrdinalIgnoreCase) >= 0)
                .ThenBy(transform => RigPath(transform, target.transform).Length)
                .ToArray();
            if (candidates.Length == 0)
            {
                throw new InvalidOperationException(
                    target.name + " " + exactName + " rig transform is missing.");
            }

            return candidates[0];
        }

        private static string RigPath(Transform transform, Transform root)
        {
            List<string> names = new List<string>();
            Transform current = transform;
            while (current != null && current != root)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }

        private static void RemoveGeneratedChild(Transform parent, string childName)
        {
            List<GameObject> matches = new List<GameObject>();
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                if (child.name == childName)
                {
                    matches.Add(child.gameObject);
                }
            }

            foreach (GameObject match in matches)
            {
                UnityEngine.Object.DestroyImmediate(match);
            }
        }

        private static Scene RequireScene()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.IsValid() && active.isLoaded && active.path == ScenePath)
            {
                return active;
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static GameObject RequireTarget(Scene scene, string targetName)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(candidate => candidate.name == targetName)
                .Select(candidate => candidate.gameObject)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one " + targetName + "; actual=" + matches.Length);
            }

            return matches[0];
        }

        private static void RequireFile(string relativePath)
        {
            string fullPath = Absolute(relativePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException(relativePath + " is missing.", fullPath);
            }
        }

        private static void RequireApproximately(
            float actual,
            float expected,
            float tolerance,
            string label)
        {
            if (Mathf.Abs(actual - expected) > tolerance)
            {
                throw new InvalidOperationException(
                    label + " mismatch. expected=" + Num(expected) +
                    "; actual=" + Num(actual));
            }
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    label + " mismatch. expected=" + expected + "; actual=" + actual);
            }
        }

        private static string Sha256(string path)
        {
            using FileStream stream = File.OpenRead(path);
            using SHA256 hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Absolute(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string Num(float value)
        {
            return value.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
        }

        private readonly struct TargetSnapshot
        {
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;
            private readonly RuntimeAnimatorController controller;
            private readonly Dictionary<Renderer, Material[]> materials;

            private TargetSnapshot(
                Vector3 position,
                Quaternion rotation,
                Vector3 scale,
                RuntimeAnimatorController animatorController,
                Dictionary<Renderer, Material[]> rendererMaterials)
            {
                localPosition = position;
                localRotation = rotation;
                localScale = scale;
                controller = animatorController;
                materials = rendererMaterials;
            }

            internal static TargetSnapshot Capture(GameObject target)
            {
                Animator animator = RequireAnimator(target);
                Dictionary<Renderer, Material[]> rendererMaterials =
                    target.GetComponentsInChildren<Renderer>(true)
                        .Where(renderer =>
                            renderer.GetComponentInParent<RepairProgressBarPresenter>() == null &&
                            renderer.GetComponentInParent<RepairWeldingVfxPresenter>() == null)
                        .ToDictionary(
                            renderer => renderer,
                            renderer => renderer.sharedMaterials.ToArray());
                return new TargetSnapshot(
                    target.transform.localPosition,
                    target.transform.localRotation,
                    target.transform.localScale,
                    animator.runtimeAnimatorController,
                    rendererMaterials);
            }

            internal void RequireUnchanged(GameObject target)
            {
                if (Vector3.Distance(localPosition, target.transform.localPosition) > 0.000001f ||
                    Quaternion.Angle(localRotation, target.transform.localRotation) > 0.0001f ||
                    Vector3.Distance(localScale, target.transform.localScale) > 0.000001f)
                {
                    throw new InvalidOperationException(
                        target.name + " root Transform changed during presentation setup.");
                }

                Animator animator = RequireAnimator(target);
                if (animator.runtimeAnimatorController != controller)
                {
                    throw new InvalidOperationException(
                        target.name + " Animator controller changed during presentation setup.");
                }

                foreach (KeyValuePair<Renderer, Material[]> pair in materials)
                {
                    if (pair.Key == null || !pair.Value.SequenceEqual(pair.Key.sharedMaterials))
                    {
                        throw new InvalidOperationException(
                            target.name + " existing renderer materials changed during presentation setup.");
                    }
                }
            }
        }
    }

    [InitializeOnLoad]
    internal static class ShipRepairProgressAndWeldingPlayModeCapture
    {
        private const string PendingKey =
            "Bellerophon.RepairPresentation.Pending";
        private const string StateKey =
            "Bellerophon.RepairPresentation.State";
        private const string FailureKey =
            "Bellerophon.RepairPresentation.Failure";
        private const string ConsoleErrorsBeforeKey =
            "Bellerophon.RepairPresentation.ConsoleErrorsBefore";
        private const int WaitingForPlayMode = 0;
        private const int ObservingFullCycle = 1;
        private const int WaitingForEditModeAfterSuccess = 2;
        private const int WaitingForEditModeAfterFailure = 3;
        private static Action<string> complete;
        private static Action<Exception> fail;
        private static GameObject ship;
        private static GameObject sabotage;
        private static RepairProgressBarPresenter shipProgress;
        private static RepairProgressBarPresenter sabotageProgress;
        private static RepairWeldingVfxPresenter welding;
        private static bool observedNearFull;
        private static bool observedRestart;
        private static float maximumProgressDifference;
        private static Vector3 weldingInitialWorldPosition;
        private static float maximumWeldingWorldPositionDrift;
        private static float minimumArcIntensity;
        private static float maximumArcIntensity;
        private static float maximumSparkTravelNormalized;
        private static float maximumSmokeTravelNormalized;
        private static readonly double[] WeldingVisualCaptureTimes =
        {
            1.05d,
            1.29d,
            1.53d
        };
        private static Texture2D[] weldingVisualPanels;
        private static int weldingVisualCaptureIndex;
        private static double playStartedAt;

        static ShipRepairProgressAndWeldingPlayModeCapture()
        {
            if (HasPendingCapture)
            {
                Subscribe();
            }
        }

        internal static bool HasPendingCapture =>
            SessionState.GetBool(PendingKey, false);

        internal static void Start(Action<string> onComplete, Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Repair presentation final review must start in Edit Mode.");
            }

            ShipRepairProgressAndWeldingSetupTools.Inspect();
            if (File.Exists(
                ShipRepairProgressAndWeldingSetupTools.FinalImageAbsolutePath))
            {
                throw new InvalidOperationException(
                    "Repair presentation final image already exists.");
            }

            complete = onComplete;
            fail = onFail;
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.SetInt(
                ConsoleErrorsBeforeKey,
                LightsaberSetupTools.ConsoleErrorCount());
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        internal static void Resume(Action<string> onComplete, Action<Exception> onFail)
        {
            complete = onComplete;
            fail = onFail;
            if (!HasPendingCapture)
            {
                throw new InvalidOperationException(
                    "Repair presentation Play Mode capture has no pending state.");
            }

            Subscribe();
        }

        private static void Subscribe()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!HasPendingCapture)
            {
                EditorApplication.update -= Tick;
                return;
            }

            int state = SessionState.GetInt(StateKey, WaitingForPlayMode);
            try
            {
                if (state == WaitingForPlayMode)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        if (!EditorApplication.isPlayingOrWillChangePlaymode)
                        {
                            EditorApplication.EnterPlaymode();
                        }

                        return;
                    }

                    InitializeRuntime();
                    SessionState.SetInt(StateKey, ObservingFullCycle);
                    return;
                }

                if (state == ObservingFullCycle)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Play Mode ended before repair presentation review completed.");
                    }

                    ObserveAndCapture();
                    return;
                }

                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                if (state == WaitingForEditModeAfterFailure)
                {
                    FinishFailure();
                    return;
                }

                ShipRepairProgressAndWeldingSetupTools.Inspect();
                Action<string> callback = complete;
                Cleanup();
                const string success =
                    "ShipRepair/SabotageRepair five-second progress loop and approved welding VFX captured once in natural Play Mode.";
                if (callback != null)
                {
                    callback(success);
                }
                else
                {
                    Debug.Log(success);
                }
            }
            catch (Exception exception)
            {
                SessionState.SetString(FailureKey, exception.ToString());
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetInt(StateKey, WaitingForEditModeAfterFailure);
                    if (EditorApplication.isPlaying)
                    {
                        EditorApplication.ExitPlaymode();
                    }

                    return;
                }

                FinishFailure();
            }
        }

        private static void InitializeRuntime()
        {
            ShipRepairProgressAndWeldingSetupTools.RequireRuntimeSetup(
                out ship,
                out sabotage,
                out shipProgress,
                out sabotageProgress,
                out welding);
            observedNearFull = false;
            observedRestart = false;
            maximumProgressDifference = 0f;
            weldingInitialWorldPosition = welding.transform.position;
            maximumWeldingWorldPositionDrift = 0f;
            minimumArcIntensity = float.MaxValue;
            maximumArcIntensity = float.MinValue;
            maximumSparkTravelNormalized = 0f;
            maximumSmokeTravelNormalized = 0f;
            DestroyVisualPanels();
            weldingVisualPanels = new Texture2D[WeldingVisualCaptureTimes.Length];
            weldingVisualCaptureIndex = 0;
            playStartedAt = EditorApplication.timeSinceStartup;
        }

        private static void ObserveAndCapture()
        {
            float shipValue = shipProgress.NormalizedProgress;
            float sabotageValue = sabotageProgress.NormalizedProgress;
            maximumProgressDifference = Mathf.Max(
                maximumProgressDifference,
                Mathf.Abs(shipValue - sabotageValue));
            if (maximumProgressDifference > 0.035f)
            {
                throw new InvalidOperationException(
                    "Repair progress bars are not advancing together. difference=" +
                    maximumProgressDifference.ToString("0.######"));
            }

            maximumWeldingWorldPositionDrift = Mathf.Max(
                maximumWeldingWorldPositionDrift,
                Vector3.Distance(
                    welding.transform.position,
                    weldingInitialWorldPosition));
            if (maximumWeldingWorldPositionDrift > 0.0001f)
            {
                throw new InvalidOperationException(
                    "ShipRepair welding VFX moved away from its fixed world point. drift=" +
                    maximumWeldingWorldPositionDrift.ToString("0.######"));
            }

            minimumArcIntensity = Mathf.Min(
                minimumArcIntensity,
                welding.CurrentArcIntensity);
            maximumArcIntensity = Mathf.Max(
                maximumArcIntensity,
                welding.CurrentArcIntensity);
            maximumSparkTravelNormalized = Mathf.Max(
                maximumSparkTravelNormalized,
                welding.CurrentSparkTravelNormalized);
            maximumSmokeTravelNormalized = Mathf.Max(
                maximumSmokeTravelNormalized,
                welding.CurrentSmokeTravelNormalized);

            double elapsed = EditorApplication.timeSinceStartup - playStartedAt;
            if (weldingVisualCaptureIndex < WeldingVisualCaptureTimes.Length &&
                elapsed >= WeldingVisualCaptureTimes[weldingVisualCaptureIndex])
            {
                weldingVisualPanels[weldingVisualCaptureIndex] =
                    ShipRepairProgressAndWeldingSetupTools.RenderTarget(ship);
                weldingVisualCaptureIndex++;
            }

            float distance = welding.CurrentForwardDistance();
            if (Mathf.Abs(
                    distance - RepairWeldingVfxPresenter.DesignForwardDistanceMeters) >
                0.002f)
            {
                throw new InvalidOperationException(
                    "ShipRepair welding VFX left the approved 0.5 m forward plane. actual=" +
                    distance.ToString("0.######"));
            }

            if (shipValue >= 0.97f && sabotageValue >= 0.97f)
            {
                observedNearFull = true;
            }

            if (observedNearFull && shipValue <= 0.08f && sabotageValue <= 0.08f)
            {
                observedRestart = true;
            }

            if (observedRestart &&
                shipValue >= 0.49f && shipValue <= 0.53f &&
                sabotageValue >= 0.49f && sabotageValue <= 0.53f)
            {
                if (weldingVisualCaptureIndex != WeldingVisualCaptureTimes.Length ||
                    weldingVisualPanels.Any(panel => panel == null))
                {
                    throw new InvalidOperationException(
                        "Three natural welding VFX visual phases were not captured.");
                }
                if (maximumArcIntensity - minimumArcIntensity < 0.20f)
                {
                    throw new InvalidOperationException(
                        "ShipRepair welding arc flicker range is too small.");
                }
                if (maximumSparkTravelNormalized < 0.145f ||
                    maximumSmokeTravelNormalized < 0.075f ||
                    welding.ApprovedSampleLayerCount != 5)
                {
                    throw new InvalidOperationException(
                        "ShipRepair approved-sample animation layers did not traverse their designed range. " +
                        "spark=" + maximumSparkTravelNormalized.ToString("0.######") +
                        "; smoke=" + maximumSmokeTravelNormalized.ToString("0.######") +
                        "; layers=" + welding.ApprovedSampleLayerCount);
                }

                int errorsBefore = SessionState.GetInt(ConsoleErrorsBeforeKey, 0);
                if (LightsaberSetupTools.ConsoleErrorCount() > errorsBefore)
                {
                    throw new InvalidOperationException(
                        "Unity console gained errors before final repair presentation capture.");
                }

                Texture2D sabotagePanel =
                    ShipRepairProgressAndWeldingSetupTools.RenderTarget(sabotage);
                Texture2D sheet =
                    ShipRepairProgressAndWeldingSetupTools.CombineAnimationPanels(
                        weldingVisualPanels[0],
                        weldingVisualPanels[1],
                        weldingVisualPanels[2],
                        sabotagePanel);
                try
                {
                    ShipRepairProgressAndWeldingSetupTools.WriteFinalEvidence(
                        sheet,
                        shipValue,
                        sabotageValue,
                        maximumProgressDifference,
                        maximumWeldingWorldPositionDrift,
                        weldingInitialWorldPosition,
                        minimumArcIntensity,
                        maximumArcIntensity,
                        maximumSparkTravelNormalized,
                        maximumSmokeTravelNormalized,
                        welding.ApprovedSampleLayerCount,
                        errorsBefore);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(sheet);
                    UnityEngine.Object.DestroyImmediate(sabotagePanel);
                    DestroyVisualPanels();
                }

                SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
                EditorApplication.ExitPlaymode();
                return;
            }

            if (EditorApplication.timeSinceStartup - playStartedAt > 14d)
            {
                throw new TimeoutException(
                    "Repair presentation did not complete and restart its five-second cycle within 14 seconds.");
            }
        }

        private static void FinishFailure()
        {
            Exception exception = new InvalidOperationException(
                SessionState.GetString(
                    FailureKey,
                    "Repair presentation Play Mode review failed."));
            Action<Exception> callback = fail;
            Cleanup();
            if (callback != null)
            {
                callback(exception);
            }
            else
            {
                Debug.LogException(exception);
            }
        }

        private static void Cleanup()
        {
            DestroyVisualPanels();
            EditorApplication.update -= Tick;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseInt(StateKey);
            SessionState.EraseString(FailureKey);
            SessionState.EraseInt(ConsoleErrorsBeforeKey);
            complete = null;
            fail = null;
            ship = null;
            sabotage = null;
            shipProgress = null;
            sabotageProgress = null;
            welding = null;
        }

        private static void DestroyVisualPanels()
        {
            if (weldingVisualPanels == null)
            {
                return;
            }

            foreach (Texture2D panel in weldingVisualPanels)
            {
                if (panel != null)
                {
                    UnityEngine.Object.DestroyImmediate(panel);
                }
            }

            weldingVisualPanels = null;
            weldingVisualCaptureIndex = 0;
        }
    }
}
