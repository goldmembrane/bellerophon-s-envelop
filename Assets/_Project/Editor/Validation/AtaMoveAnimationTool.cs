using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.AtaCargoRunScene
{
    internal static class AtaMoveAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ata Enemy Placement";
        private const string MoveSlotName = "Ata_03_Move";
        private const string ModelName = "Ata_Model";
        private const string ModelPath =
            "Assets/_Project/Art/Enemies/Ata/Models/Ata.fbx";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Ata_03_Move.controller";
        private const string ExpectedClipName =
            "Armature|Armature|Armature|Armature|walking_man|baselayer";
        private const string DiagnosticPath =
            "docs/validation/ata_move_2026-08-11/Ata_03_Move_Diagnostic.png";
        private const string FinalPath =
            "docs/validation/ata_move_2026-08-11/Ata_03_Move_Final.png";
        private const string ReportPath =
            "docs/validation/ata_move_2026-08-11/Ata_03_Move_Report.txt";
        private const float TransformTolerance = 0.0002f;

        [MenuItem("Bellerophon/Enemies/Ata/Apply Move Animation")]
        public static void ApplyAtaMoveAnimation()
        {
            var scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            var moveSlot = RequireDirectChild(placement.transform, MoveSlotName);
            var model = RequireDirectChild(moveSlot, ModelName);
            var slotBefore = new TransformSnapshot(moveSlot);
            var modelBefore = new TransformSnapshot(model);
            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var otherSlotsBefore = OtherSlotSignatures(placement.transform, moveSlot);

            ConfigureImporterForLoop();
            var clip = RequireEmbeddedClip();
            var controller = CreateController(clip);
            ConfigureAnimator(model, controller);

            if (!slotBefore.Matches() || !modelBefore.Matches())
            {
                throw new InvalidOperationException(
                    "Ata_03_Move slot or model transform changed while applying the embedded move clip.");
            }

            RequireEqual(
                otherSlotsBefore,
                OtherSlotSignatures(placement.transform, moveSlot),
                "An Ata slot outside Ata_03_Move changed.");
            RequireEqual(
                otherRootsBefore,
                OtherRootSignatures(scene, placement),
                "A scene root outside the Ata placement changed.");
            RequireAppliedState(model, clip, controller);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after applying Ata move animation.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "AtaMoveAnimationApplied Result=PASS" +
                ", Slot=" + MoveSlotName +
                ", EmbeddedClip=" + clip.name +
                ", Duration=" + Num(clip.length) +
                ", Loop=True" +
                ", RootMotion=False" +
                ", SlotPositionFixed=True" +
                ", OtherAtaSlotsUnchanged=True" +
                ", OtherSceneRootsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Move Diagnostic")]
        public static void CaptureAtaMoveAnimationDiagnostic()
        {
            CaptureReview(DiagnosticPath, "AtaMoveAnimationDiagnosticCaptured");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Move Final")]
        public static void CaptureAtaMoveAnimationFinal()
        {
            CaptureReview(FinalPath, "AtaMoveAnimationFinalCaptured");
        }

        private static void ConfigureImporterForLoop()
        {
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter ??
                           throw new InvalidOperationException(
                               "Ata FBX importer is unavailable.");
            importer.importAnimation = true;
            var clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1)
            {
                throw new InvalidOperationException(
                    "Ata FBX must expose exactly one embedded animation take.");
            }

            clips[0].name = ExpectedClipName;
            clips[0].loopTime = true;
            clips[0].loopPose = false;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip RequireEmbeddedClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<AnimationClip>()
                .Where(clip =>
                    !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1 || clips[0].name != ExpectedClipName)
            {
                throw new InvalidOperationException(
                    "Ata FBX embedded move clip differs from the supplied source.");
            }

            return clips[0];
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null &&
                !AssetDatabase.DeleteAsset(ControllerPath))
            {
                throw new InvalidOperationException(
                    "Existing Ata move controller could not be replaced.");
            }

            var controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var state = controller.layers[0].stateMachine.AddState("AtaMove");
            state.motion = clip;
            state.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ConfigureAnimator(
            Transform model,
            AnimatorController controller)
        {
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length > 1)
            {
                throw new InvalidOperationException(
                    "Ata_03_Move contains multiple Animators.");
            }

            var animator = animators.Length == 0
                ? model.gameObject.AddComponent<Animator>()
                : animators[0];
            if (animator.transform != model)
            {
                throw new InvalidOperationException(
                    "Ata_03_Move Animator must be on Ata_Model.");
            }

            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            EditorUtility.SetDirty(animator);
        }

        private static void RequireAppliedState(
            Transform model,
            AnimationClip clip,
            AnimatorController controller)
        {
            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_03_Move Animator is missing.");
            if (animator.transform != model || !animator.enabled ||
                animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(
                    "Ata_03_Move Animator configuration differs.");
            }

            var serializedClip = new SerializedObject(clip);
            var loop = serializedClip.FindProperty(
                "m_AnimationClipSettings.m_LoopTime");
            if (loop == null || !loop.boolValue)
            {
                throw new InvalidOperationException(
                    "Ata embedded move clip is not configured to loop.");
            }

            var state = controller.layers[0].stateMachine.defaultState;
            if (state == null || state.motion != clip)
            {
                throw new InvalidOperationException(
                    "Ata move controller does not directly reference the embedded clip.");
            }
        }

        private static void CaptureReview(string relativePath, string logPrefix)
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var moveSlot = RequireDirectChild(placement.transform, MoveSlotName);
            var model = RequireDirectChild(moveSlot, ModelName);
            var clip = RequireEmbeddedClip();
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException("Ata move controller is missing.");
            RequireAppliedState(model, clip, controller);
            var destination = Absolute(relativePath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time Ata move capture already exists: " + relativePath);
            }

            var result = CaptureStrip(model, moveSlot, clip, destination);
            WriteReport(clip, result);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Ata move capture changed the scene dirty state.");
            }

            Debug.Log(
                logPrefix + " Result=PASS" +
                ", EmbeddedClip=" + clip.name +
                ", Duration=" + Num(clip.length) +
                ", Times=0,25%,50%,75%,100%" +
                ", MaximumSlotPositionError=" +
                Num(result.MaximumSlotPositionError) +
                ", Image=" + relativePath +
                ", SceneChanged=False.");
        }

        private static CaptureResult CaptureStrip(
            Transform model,
            Transform moveSlot,
            AnimationClip clip,
            string destination)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Invalid Ata move capture folder."));
            var reviewTimes = new[]
            {
                0f,
                clip.length * 0.25f,
                clip.length * 0.5f,
                clip.length * 0.75f,
                clip.length
            };
            var modelSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(transform => new TransformSnapshot(transform))
                .ToArray();
            var slotPosition = moveSlot.position;
            var modelLocalPosition = model.localPosition;
            var modelLocalRotation = model.localRotation;
            var modelLocalScale = model.localScale;
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
            var cameraObject = new GameObject(
                "AtaMoveReviewCamera",
                typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            const int width = 384;
            const int height = 640;
            var strip = new Texture2D(
                width * reviewTimes.Length,
                height,
                TextureFormat.RGB24,
                false);
            var target = new RenderTexture(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32);
            var panel = new Texture2D(width, height, TextureFormat.RGB24, false);
            var oldActive = RenderTexture.active;
            var graph = default(PlayableGraph);
            var maximumSlotPositionError = 0f;
            try
            {
                foreach (var snapshot in otherRenderers)
                {
                    snapshot.Renderer.enabled = false;
                }

                animator.enabled = true;
                animator.Rebind();
                graph = PlayableGraph.Create("AtaMoveReview");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var playable = AnimationClipPlayable.Create(graph, clip);
                playable.SetApplyFootIK(false);
                var output = AnimationPlayableOutput.Create(
                    graph,
                    "AtaMoveClip",
                    animator);
                output.SetSourcePlayable(playable);
                graph.Play();
                playable.SetTime(0f);
                graph.Evaluate(0f);

                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = ~0;
                camera.fieldOfView = 34f;
                camera.targetTexture = target;
                FrameCamera(camera, model, width / (float)height);
                for (var index = 0; index < reviewTimes.Length; index++)
                {
                    playable.SetTime(reviewTimes[index]);
                    graph.Evaluate(0f);
                    maximumSlotPositionError = Mathf.Max(
                        maximumSlotPositionError,
                        Vector3.Distance(moveSlot.position, slotPosition));
                    if (Vector3.Distance(model.localPosition, modelLocalPosition) >
                            TransformTolerance ||
                        Quaternion.Angle(model.localRotation, modelLocalRotation) > 0.01f ||
                        Vector3.Distance(model.localScale, modelLocalScale) >
                            TransformTolerance)
                    {
                        throw new InvalidOperationException(
                            "Ata embedded move clip changed the scene model root transform.");
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
                            "Ata move review contains Unity magenta shader fallback.");
                    }

                    strip.SetPixels32(index * width, 0, width, height, pixels);
                }

                strip.Apply();
                File.WriteAllBytes(destination, strip.EncodeToPNG());
                return new CaptureResult(maximumSlotPositionError);
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                if (graph.IsValid())
                {
                    graph.Destroy();
                }

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
                throw new InvalidOperationException("Ata move model has no renderer.");
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            var playerCamera = GameObject.Find("Player")?
                                   .GetComponentInChildren<Camera>(true) ??
                               throw new InvalidOperationException(
                                   "Player camera is missing.");
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
            var distance = Mathf.Max(vertical, horizontal) * 1.18f;
            camera.transform.position =
                bounds.center + direction * distance + Vector3.up * bounds.extents.y * 0.02f;
            camera.transform.rotation = Quaternion.LookRotation(
                bounds.center - camera.transform.position,
                Vector3.up);
        }

        private static void WriteReport(
            AnimationClip clip,
            CaptureResult result)
        {
            var absolute = Absolute(ReportPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(absolute) ??
                throw new InvalidOperationException("Invalid Ata move report folder."));
            File.WriteAllLines(
                absolute,
                new[]
                {
                    "Target=Approved Ata Enemy Placement/Ata_03_Move",
                    "Source=Embedded Ata.fbx animation clip",
                    "ClipName=" + clip.name,
                    "DurationSeconds=" + Num(clip.length),
                    "Loop=True",
                    "RootMotion=False",
                    "MaximumSlotPositionError=" +
                    Num(result.MaximumSlotPositionError),
                    "NewMotionCreated=False",
                    "OtherAtaSlotsChanged=False",
                    "PlayerOrCameraChanged=False"
                },
                Encoding.UTF8);
        }

        private static Scene RequireScene(bool requireClean)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Ata move animation work requires Edit Mode.");
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
                   throw new InvalidOperationException(
                       "Approved Ata placement is missing.");
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

        private static string[] OtherSlotSignatures(
            Transform placement,
            Transform moveSlot)
        {
            return Enumerable.Range(0, placement.childCount)
                .Select(placement.GetChild)
                .Where(slot => slot != moveSlot)
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

        private static string[] OtherRootSignatures(
            Scene scene,
            GameObject placement)
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

        private static void RequireEqual(
            string[] before,
            string[] after,
            string message)
        {
            if (!before.SequenceEqual(after, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Absolute(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                relativePath));
        }

        private static string Num(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" + Num(value.x) + "," + Num(value.y) + "," +
                   Num(value.z) + ")";
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
                       Vector3.Distance(position, transform.localPosition) <=
                       TransformTolerance &&
                       Quaternion.Angle(rotation, transform.localRotation) <= 0.01f &&
                       Vector3.Distance(scale, transform.localScale) <=
                       TransformTolerance;
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
    }
}
