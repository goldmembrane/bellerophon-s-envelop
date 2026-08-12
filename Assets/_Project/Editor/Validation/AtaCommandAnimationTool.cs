using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Bellerophon.Enemies.Ata;

namespace Bellerophon.Editor.AtaCargoRunScene
{
    internal static class AtaCommandAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ata Enemy Placement";
        private const string SlotName = "Ata_05_Command";
        private const string ModelName = "Ata_Model";
        private const string SourcePath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Sources/Ata_Pointing.fbx";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Ata_05_Command.controller";
        private const string ShieldEffectName = "Kursa_ShieldStanceIcon";
        private const string BreakthroughEffectName = "Ata_BreakthroughStanceEffect";
        private const string ShieldSpritePath =
            "Assets/_Project/Art/Enemies/Kursa/Effects/Kursa_ShieldStanceIcon.png";
        private const string BreakthroughSpritePath =
            "Assets/_Project/Art/Enemies/Ata/Effects/Ata_BreakthroughStanceEffect.png";
        private const string ShieldReviewPath =
            "docs/validation/ata05_command_stance_alternation_2026-08-12/Ata_05_CommandStanceAlternation_ThreeLoops.png";
        // Reuse Kursa's approved head-center offset, world size, and render order unchanged.
        private const float ShieldHeadOffset = 0.18f;
        private const float ShieldWorldSize = 0.42f;
        private const int ShieldSortingOrder = 100;

        [MenuItem("Bellerophon/Enemies/Ata/Apply Command Animation")]
        public static void ApplyAtaCommandAnimation()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active scene before applying Ata command animation.");
            }

            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes.");
            }

            var placement = scene.GetRootGameObjects()
                                .SingleOrDefault(root => root.name == PlacementRootName) ??
                            throw new InvalidOperationException(
                                "Approved Ata enemy placement is missing.");
            var slot = placement.transform.Find(SlotName) ??
                       throw new InvalidOperationException("Ata_05_Command is missing.");
            var model = slot.Find(ModelName) ??
                        throw new InvalidOperationException(
                            "Ata_05_Command/Ata_Model is missing.");

            ConfigureMixamoClipLoop();
            ConfigureBreakthroughSpriteImporter();
            var availableClips = AssetDatabase.LoadAllAssetsAtPath(SourcePath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            var clips = availableClips
                .Where(clip => clip.name.IndexOf(
                    "mixamo",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            if (clips.Length != 1)
            {
                throw new InvalidOperationException(
                    "attas pointing.fbx must expose exactly one mixamo-named animation clip. Found=" +
                    clips.Length +
                    ", AvailableClips=" + string.Join(",", availableClips.Select(clip =>
                        clip.name + "[" + clip.length.ToString("0.######") + "s]")));
            }

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null &&
                !AssetDatabase.DeleteAsset(ControllerPath))
            {
                throw new InvalidOperationException(
                    "Existing Ata_05 command controller could not be replaced.");
            }

            var controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var state = controller.layers[0].stateMachine.AddState("AtaCommand");
            state.motion = clips[0];
            state.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);

            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length > 1)
            {
                throw new InvalidOperationException(
                    "Ata_05_Command contains multiple Animators.");
            }

            var animator = animators.Length == 0
                ? model.gameObject.AddComponent<Animator>()
                : animators[0];
            if (animator.transform != model)
            {
                throw new InvalidOperationException(
                    "Ata_05_Command Animator must be on Ata_Model.");
            }

            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            EditorUtility.SetDirty(animator);

            var correctedRightArmComponents =
                AtaOtherSlotsRightArmMeshTool.CorrectModelForClips(
                    SlotName,
                    model,
                    clips);
            var stanceEffects = ConfigureStanceEffects(model, animator, clips[0]);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after applying Ata command animation.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "AtaCommandAnimationApplied Result=PASS" +
                ", Slot=" + SlotName +
                ", Source=" + SourcePath +
                ", EmbeddedClip=" + clips[0].name +
                ", Duration=" + clips[0].length.ToString("0.######") +
                ", CorrectedRightArmComponents=" + correctedRightArmComponents +
                ", GuardianEffect=" + stanceEffects.Guardian.name +
                ", ShieldSprite=" + ShieldSpritePath +
                ", BreakthroughEffect=" + stanceEffects.Breakthrough.name +
                ", BreakthroughSprite=" + BreakthroughSpritePath +
                ", StanceOrder=GuardianThenBreakthroughAlternatingEachLoop" +
                ", RootMotion=False" +
                ", SceneSaved=True.");
        }

        public static void CaptureAtaCommandShieldReview()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath || scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be active and clean before capturing Ata command shield review.");
            }

            var placement = scene.GetRootGameObjects()
                                .SingleOrDefault(root => root.name == PlacementRootName) ??
                            throw new InvalidOperationException(
                                "Approved Ata enemy placement is missing.");
            var slot = placement.transform.Find(SlotName) ??
                       throw new InvalidOperationException("Ata_05_Command is missing.");
            var model = slot.Find(ModelName) ??
                        throw new InvalidOperationException("Ata_05_Command/Ata_Model is missing.");
            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_05_Command must contain one Animator.");
            var clip = animator.runtimeAnimatorController?.animationClips
                .Where(item => item != null)
                .Distinct()
                .SingleOrDefault() ?? throw new InvalidOperationException(
                "Ata_05_Command must contain one animation clip.");
            var stanceEffects = RequireStanceEffects(model);
            var alternator = model.GetComponents<AtaCommandStanceEffectAlternator>()
                .SingleOrDefault() ?? throw new InvalidOperationException(
                "Ata_05_Command is missing the stance-effect alternator.");
            var allRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var rendererStates = allRenderers
                .Select(item => (renderer: item, enabled: item.enabled))
                .ToArray();
            var transforms = model.GetComponentsInChildren<Transform>(true);
            var snapshots = transforms
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var cameraObject = new GameObject("Ata Command Shield Review Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            const int panelWidth = 480;
            const int panelHeight = 540;
            var target = new RenderTexture(panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelWidth, panelHeight, TextureFormat.RGBA32, false);
            var sheet = new Texture2D(panelWidth * 3, panelHeight * 3, TextureFormat.RGBA32, false);
            var normalizedTimes = new[]
            {
                0f, 0.5f, 0.99f,
                1.01f, 1.5f, 1.99f,
                2.01f, 2.5f, 2.99f
            };
            try
            {
                foreach (var item in allRenderers)
                {
                    item.enabled = item.transform.IsChildOf(model);
                }

                camera.targetTexture = target;
                for (var index = 0; index < normalizedTimes.Length; index++)
                {
                    foreach (var snapshot in snapshots)
                    {
                        snapshot.Restore();
                    }

                    clip.SampleAnimation(
                        model.gameObject,
                        clip.length * (normalizedTimes[index] % 1f));
                    animator.Play("AtaCommand", 0, normalizedTimes[index]);
                    animator.Update(0f);
                    alternator.SendMessage("LateUpdate");
                    var expectedGuardian = Mathf.FloorToInt(normalizedTimes[index]) % 2 == 0;
                    if (stanceEffects.Guardian.enabled != expectedGuardian ||
                        stanceEffects.Breakthrough.enabled == expectedGuardian ||
                        stanceEffects.Guardian.gameObject.activeInHierarchy == false ||
                        stanceEffects.Breakthrough.gameObject.activeInHierarchy == false)
                    {
                        throw new InvalidOperationException(
                            "Ata command stance effects differ at normalized time " +
                            normalizedTimes[index].ToString("0.##") + ".");
                    }

                    var bounds = BoundsOf(model);
                    var direction = Quaternion.AngleAxis(24f, model.up) * model.forward;
                    var targetPoint = bounds.center + model.up * bounds.extents.y * 0.05f;
                    var distance = bounds.extents.magnitude /
                                   Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * 1.1f;
                    camera.transform.position = targetPoint - direction.normalized * distance;
                    camera.transform.rotation = Quaternion.LookRotation(
                        targetPoint - camera.transform.position,
                        model.up);
                    camera.Render();
                    RenderTexture.active = target;
                    panel.ReadPixels(new Rect(0f, 0f, panelWidth, panelHeight), 0, 0);
                    panel.Apply();
                    sheet.SetPixels(
                        (index % 3) * panelWidth,
                        (2 - index / 3) * panelHeight,
                        panelWidth,
                        panelHeight,
                        panel.GetPixels());
                }

                sheet.Apply();
                var absolute = Path.GetFullPath(Path.Combine(
                    Application.dataPath,
                    "..",
                    ShieldReviewPath));
                Directory.CreateDirectory(Path.GetDirectoryName(absolute));
                File.WriteAllBytes(absolute, sheet.EncodeToPNG());
                Debug.Log(
                    "AtaCommandShieldReviewCaptured Result=PASS" +
                    ", Path=" + ShieldReviewPath +
                    ", Samples=9" +
                    ", Loops=3" +
                    ", GuardianInstances=1" +
                    ", BreakthroughInstances=1" +
                    ", SimultaneousVisibleEffects=0" +
                    ", Order=Guardian,Breakthrough,Guardian" +
                    ", SceneChanged=False.");
            }
            finally
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }

                foreach (var state in rendererStates)
                {
                    if (state.renderer != null)
                    {
                        state.renderer.enabled = state.enabled;
                    }
                }

                RenderTexture.active = null;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static StanceEffects ConfigureStanceEffects(
            Transform model,
            Animator animator,
            AnimationClip clip)
        {
            foreach (var existing in model.GetComponentsInChildren<SpriteRenderer>(true)
                         .Where(item => item.name == ShieldEffectName ||
                                        item.name == BreakthroughEffectName)
                         .ToArray())
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            clip.SampleAnimation(model.gameObject, 0f);
            var bodyRenderers = model.GetComponentsInChildren<Renderer>(true)
                .Where(item => !(item is SpriteRenderer))
                .ToArray();
            if (bodyRenderers.Length == 0)
            {
                throw new InvalidOperationException("Ata_05_Command has no body renderer.");
            }

            var bodyBounds = bodyRenderers[0].bounds;
            foreach (var renderer in bodyRenderers.Skip(1))
            {
                bodyBounds.Encapsulate(renderer.bounds);
            }

            var guardianSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShieldSpritePath) ??
                         throw new InvalidOperationException(
                             "The Kursa shield icon sprite is missing.");
            var breakthroughSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                BreakthroughSpritePath) ?? throw new InvalidOperationException(
                "The approved Ata breakthrough stance sprite is missing.");
            var effectObject = new GameObject(ShieldEffectName);
            effectObject.transform.SetParent(model, false);
            var worldPosition = bodyBounds.center +
                                model.up * (bodyBounds.extents.y + ShieldHeadOffset);
            foreach (var snapshot in snapshots)
            {
                snapshot.Restore();
            }

            effectObject.transform.localPosition = model.InverseTransformPoint(worldPosition);
            effectObject.transform.localRotation = Quaternion.identity;
            var worldScale = model.lossyScale;
            effectObject.transform.localScale = new Vector3(
                ShieldWorldSize / Mathf.Abs(worldScale.x),
                ShieldWorldSize / Mathf.Abs(worldScale.y),
                ShieldWorldSize / Mathf.Abs(worldScale.z));
            var guardian = effectObject.AddComponent<SpriteRenderer>();
            guardian.sprite = guardianSprite;
            guardian.color = Color.white;
            guardian.sortingOrder = ShieldSortingOrder;
            EditorUtility.SetDirty(guardian);

            var breakthroughObject = new GameObject(BreakthroughEffectName);
            breakthroughObject.transform.SetParent(model, false);
            breakthroughObject.transform.SetLocalPositionAndRotation(
                effectObject.transform.localPosition,
                effectObject.transform.localRotation);
            breakthroughObject.transform.localScale = effectObject.transform.localScale;
            var breakthrough = breakthroughObject.AddComponent<SpriteRenderer>();
            breakthrough.sprite = breakthroughSprite;
            breakthrough.color = Color.white;
            breakthrough.sortingOrder = ShieldSortingOrder;
            EditorUtility.SetDirty(breakthrough);

            foreach (var existing in model.GetComponents<AtaCommandStanceEffectAlternator>())
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }

            var alternator = model.gameObject.AddComponent<AtaCommandStanceEffectAlternator>();
            alternator.Configure(animator, guardian, breakthrough);
            EditorUtility.SetDirty(alternator);
            return RequireStanceEffects(model);
        }

        private static StanceEffects RequireStanceEffects(Transform model)
        {
            var guardian = model.GetComponentsInChildren<SpriteRenderer>(true)
                .Where(item => item.name == ShieldEffectName)
                .SingleOrDefault();
            var breakthrough = model.GetComponentsInChildren<SpriteRenderer>(true)
                .Where(item => item.name == BreakthroughEffectName)
                .SingleOrDefault();
            if (guardian == null || breakthrough == null)
            {
                throw new InvalidOperationException(
                    "Ata_05_Command must contain one guardian and one breakthrough stance effect.");
            }

            if (guardian.sprite == null ||
                AssetDatabase.GetAssetPath(guardian.sprite) != ShieldSpritePath ||
                breakthrough.sprite == null ||
                AssetDatabase.GetAssetPath(breakthrough.sprite) != BreakthroughSpritePath ||
                guardian.color.a < 0.999f || breakthrough.color.a < 0.999f ||
                guardian.sortingOrder != ShieldSortingOrder ||
                breakthrough.sortingOrder != ShieldSortingOrder ||
                guardian.transform.localPosition != breakthrough.transform.localPosition ||
                guardian.transform.localRotation != breakthrough.transform.localRotation)
            {
                throw new InvalidOperationException(
                    "Ata_05_Command stance-effect configuration differs.");
            }

            return new StanceEffects(guardian, breakthrough);
        }

        private readonly struct StanceEffects
        {
            public StanceEffects(
                SpriteRenderer guardian,
                SpriteRenderer breakthrough)
            {
                Guardian = guardian;
                Breakthrough = breakthrough;
            }

            public SpriteRenderer Guardian { get; }
            public SpriteRenderer Breakthrough { get; }
        }

        private static Bounds BoundsOf(Transform model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled && item.gameObject.activeSelf)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Ata command review has no renderer.");
            }

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        private readonly struct TransformSnapshot
        {
            private readonly Transform transform;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            public TransformSnapshot(Transform transform)
            {
                this.transform = transform;
                localPosition = transform.localPosition;
                localRotation = transform.localRotation;
                localScale = transform.localScale;
            }

            public void Restore()
            {
                if (transform == null)
                {
                    return;
                }

                transform.localPosition = localPosition;
                transform.localRotation = localRotation;
                transform.localScale = localScale;
            }
        }

        private static void ConfigureMixamoClipLoop()
        {
            var importer = AssetImporter.GetAtPath(SourcePath) as ModelImporter ??
                           throw new InvalidOperationException(
                               "Ata pointing FBX importer is unavailable.");
            var clips = importer.defaultClipAnimations;
            var mixamoIndices = clips
                .Select((clip, index) => (clip, index))
                .Where(item => item.clip.name.IndexOf(
                    "mixamo",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(item => item.index)
                .ToArray();
            if (mixamoIndices.Length != 1)
            {
                throw new InvalidOperationException(
                    "attas pointing.fbx must expose exactly one mixamo-named default clip.");
            }

            var selected = clips[mixamoIndices[0]];
            selected.loopTime = true;
            clips[mixamoIndices[0]] = selected;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static void ConfigureBreakthroughSpriteImporter()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BreakthroughSpritePath) ??
                          throw new InvalidOperationException(
                              "The approved Ata breakthrough stance texture is missing.");
            var importer = AssetImporter.GetAtPath(BreakthroughSpritePath) as TextureImporter ??
                           throw new InvalidOperationException(
                               "The approved Ata breakthrough stance texture importer is missing.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = texture.width;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
    }
}
