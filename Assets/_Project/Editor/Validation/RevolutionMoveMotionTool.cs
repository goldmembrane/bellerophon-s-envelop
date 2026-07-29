using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.RevolutionCargoRunScene
{
    internal static class RevolutionMoveMotionTool
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName =
            "Approved Revolution Enemy Placement";
        private const string MoveSlotName = "Revolution_03";
        private const string ModelPath =
            "Assets/_Project/Art/Enemies/Revolution/Models/Revolution.fbx";
        private const string EmbeddedClipMarker = "walking_man|baselayer";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Revolution/Controllers/" +
            "Revolution_03_Move.controller";

        [MenuItem(
            "Bellerophon/Enemies/Revolution/Apply Embedded Move Motion")]
        public static void ApplyRevolutionMoveMotion()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Save or discard them before applying the Revolution move motion.");
            }

            ConfigureEmbeddedMoveImporter();

            var placementRoot =
                GameObject.Find(PlacementRootName) ??
                throw new InvalidOperationException(
                    "The approved Revolution placement root is missing.");
            var moveSlot =
                FindDirectChild(placementRoot.transform, MoveSlotName) ??
                throw new InvalidOperationException(
                    PlacementRootName + "/" + MoveSlotName +
                    " is missing.");
            if (moveSlot.childCount != 1)
            {
                throw new InvalidOperationException(
                    MoveSlotName +
                    " must contain exactly one model.");
            }

            var model = moveSlot.GetChild(0);
            var renderers =
                model.GetComponentsInChildren<Renderer>(true);
            var rendererSnapshots =
                renderers.Select(item => new RendererSnapshot(item))
                    .ToArray();
            var transformSnapshots =
                model.GetComponentsInChildren<Transform>(true)
                    .Select(item => new TransformSnapshot(item))
                    .ToArray();
            var otherSlotsBefore =
                placementRoot.transform.Cast<Transform>()
                    .Where(item => item != moveSlot)
                    .Select(SlotSignature)
                    .ToArray();

            var clip = RequireEmbeddedMoveClip();
            var controller = CreateController(clip);
            var animator = model.GetComponent<Animator>();
            if (animator == null)
            {
                animator = model.gameObject.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(
                animator);

            RequireUnchangedAppearance(rendererSnapshots);
            RequireUnchangedTransforms(transformSnapshots);
            var otherSlotsAfter =
                placementRoot.transform.Cast<Transform>()
                    .Where(item => item != moveSlot)
                    .Select(SlotSignature)
                    .ToArray();
            if (!otherSlotsBefore.SequenceEqual(
                    otherSlotsAfter,
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "A Revolution slot outside Revolution_03 changed.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after assigning the embedded Revolution move motion.");
            }

            AssetDatabase.SaveAssets();
            Selection.activeGameObject = moveSlot.gameObject;
            Debug.Log(
                "RevolutionMoveMotionApplied" +
                ", Slot=" + MoveSlotName +
                ", Source=" + ModelPath +
                ", Clip=" + clip.name +
                ", Length=" +
                clip.length.ToString(
                    "0.######",
                    System.Globalization.CultureInfo.InvariantCulture) +
                ", RootMotion=False" +
                ", AppearanceChanged=False" +
                ", MeshChanged=False" +
                ", OtherSlotsChanged=False.");

            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.EnterPlaymode();
                }
            };
        }

        private static Scene RequireCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() ||
                !scene.isLoaded ||
                !string.Equals(
                    scene.path,
                    ScenePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active scene.");
            }

            return scene;
        }

        private static void ConfigureEmbeddedMoveImporter()
        {
            var importer =
                AssetImporter.GetAtPath(ModelPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "The Revolution ModelImporter is missing.");
            if (!importer.importAnimation)
            {
                importer.importAnimation = true;
                importer.SaveAndReimport();
                importer =
                    AssetImporter.GetAtPath(ModelPath) as ModelImporter ??
                    throw new InvalidOperationException(
                        "The Revolution ModelImporter was lost after enabling embedded animation import.");
            }

            var sourceClips = importer.defaultClipAnimations;
            var moveClip = sourceClips.SingleOrDefault(item =>
                item.name.IndexOf(
                    EmbeddedClipMarker,
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                item.takeName.IndexOf(
                    EmbeddedClipMarker,
                    StringComparison.OrdinalIgnoreCase) >= 0);
            if (moveClip == null)
            {
                throw new InvalidOperationException(
                    "The Revolution FBX does not expose the embedded walking_man|baselayer take. Available=" +
                    string.Join(
                        "|",
                        sourceClips.Select(item =>
                            item.name + "[" + item.takeName + "]")));
            }

            importer.importAnimation = true;
            importer.animationWrapMode = WrapMode.Loop;
            moveClip.wrapMode = WrapMode.Loop;
            moveClip.loopTime = true;
            moveClip.loopPose = true;
            importer.clipAnimations = new[] { moveClip };
            importer.SaveAndReimport();
        }

        private static AnimationClip RequireEmbeddedMoveClip()
        {
            var clips =
                AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                    .OfType<AnimationClip>()
                    .Where(item =>
                        !item.name.StartsWith(
                            "__preview__",
                            StringComparison.Ordinal))
                    .Where(item =>
                        item.name.IndexOf(
                            EmbeddedClipMarker,
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToArray();
            if (clips.Length != 1)
            {
                throw new InvalidOperationException(
                    "The Revolution FBX must contain exactly one embedded walking_man|baselayer clip. Found=" +
                    string.Join("|", clips.Select(item => item.name)));
            }

            if (!clips[0].isLooping ||
                !AnimationUtility
                    .GetAnimationClipSettings(clips[0])
                    .loopTime)
            {
                throw new InvalidOperationException(
                    "The embedded Revolution move clip is not configured to loop.");
            }

            return clips[0];
        }

        private static AnimatorController CreateController(
            AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                    ControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            var controller =
                AnimatorController.CreateAnimatorControllerAtPath(
                    ControllerPath);
            var stateMachine =
                controller.layers[0].stateMachine;
            var state =
                stateMachine.AddState("RevolutionEmbeddedMove");
            state.motion = clip;
            state.speed = 1f;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static Transform FindDirectChild(
            Transform parent,
            string name)
        {
            return parent.Cast<Transform>()
                .FirstOrDefault(item =>
                    string.Equals(
                        item.name,
                        name,
                        StringComparison.Ordinal));
        }

        private static string SlotSignature(Transform slot)
        {
            return string.Join(
                "|",
                slot.GetComponentsInChildren<Transform>(true)
                    .Select(item =>
                        item.name + ":" +
                        item.localPosition.ToString("R") + ":" +
                        item.localRotation.ToString("R") + ":" +
                        item.localScale.ToString("R")));
        }

        private static void RequireUnchangedAppearance(
            RendererSnapshot[] snapshots)
        {
            foreach (var snapshot in snapshots)
            {
                snapshot.RequireUnchanged();
            }
        }

        private static void RequireUnchangedTransforms(
            TransformSnapshot[] snapshots)
        {
            foreach (var snapshot in snapshots)
            {
                snapshot.RequireUnchanged();
            }
        }

        private sealed class RendererSnapshot
        {
            private readonly Renderer renderer;
            private readonly Material[] materials;
            private readonly Mesh mesh;

            public RendererSnapshot(Renderer source)
            {
                renderer = source;
                materials = source.sharedMaterials;
                mesh =
                    source is SkinnedMeshRenderer skinned
                        ? skinned.sharedMesh
                        : source.GetComponent<MeshFilter>()?.sharedMesh;
            }

            public void RequireUnchanged()
            {
                var currentMesh =
                    renderer is SkinnedMeshRenderer skinned
                        ? skinned.sharedMesh
                        : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                if (currentMesh != mesh ||
                    !renderer.sharedMaterials.SequenceEqual(materials))
                {
                    throw new InvalidOperationException(
                        "Revolution_03 appearance, mesh, or materials changed while assigning the embedded move motion.");
                }
            }
        }

        private sealed class TransformSnapshot
        {
            private readonly Transform transform;
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            public TransformSnapshot(Transform source)
            {
                transform = source;
                position = source.localPosition;
                rotation = source.localRotation;
                scale = source.localScale;
            }

            public void RequireUnchanged()
            {
                if (transform.localPosition != position ||
                    transform.localRotation != rotation ||
                    transform.localScale != scale)
                {
                    throw new InvalidOperationException(
                        "A Revolution_03 transform changed while assigning the embedded move motion.");
                }
            }
        }
    }
}
