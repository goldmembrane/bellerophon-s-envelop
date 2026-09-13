using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bellerophon.PlayerAnimation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class MarkerSprayIdleSetupTools
    {
        internal const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        internal const string TargetName = "MarkerSpray_Idle";
        internal const string FinalImagePath =
            "Assets/_Project/Animation/MarkerSpray/Review/final.png";

        private const string HoloTargetName = "HoloSpray_Idle";
        private const string ExternalModelPath = "item model/marking spray.fbx";
        private const string ItemFolder = "Assets/_Project/Art/Items/MarkerSpray";
        private const string ModelPath = ItemFolder + "/MarkingSpray.fbx";
        private const string TextureFolder = ItemFolder + "/Textures";
        private const string MaterialFolder = ItemFolder + "/Materials";
        private const string MaterialPath = MaterialFolder + "/MarkingSpray.mat";
        private const string MetallicSmoothnessPath =
            TextureFolder + "/MarkingSpray_MetallicSmoothness.png";
        private const string AnimationFolder = "Assets/_Project/Animation/MarkerSpray";
        private const string ProfilePath =
            AnimationFolder + "/MarkerSprayIdleCarryProfile.asset";
        private const string ControllerPath =
            AnimationFolder + "/MarkerSprayIdle_Locomotion.controller";
        private const string HoloProfilePath =
            "Assets/_Project/Art/Player/Animations/HoloSprayIdle/HoloSprayIdleCarryProfile.asset";
        private const string HoloControllerPath =
            "Assets/_Project/Art/Player/Animations/HoloSprayIdleLocomotion/HoloSprayIdle_Locomotion.controller";
        private const string StateName = "MarkerSprayIdleLocomotion2D";
        private const string RootTreeName = "MarkerSprayIdleSixMotion2D";
        private const string DiagonalTreeName = "MarkerSprayIdleWalkDiagonalSourceExact";

        private static readonly string[] ClipNames =
        {
            "Idle", "WalkForward", "WalkBackward", "Sidestep", "RunForward"
        };

        private static readonly string[] ClipPaths =
        {
            AnimationFolder + "/MarkerSprayIdle_Idle.anim",
            AnimationFolder + "/MarkerSprayIdle_WalkForward.anim",
            AnimationFolder + "/MarkerSprayIdle_WalkBackward.anim",
            AnimationFolder + "/MarkerSprayIdle_Sidestep.anim",
            AnimationFolder + "/MarkerSprayIdle_RunForward.anim"
        };

        private static readonly Vector2[] RequiredPositions =
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, -1f),
            new Vector2(1f, 0f),
            new Vector2(0.70710677f, 0.70710677f),
            new Vector2(0f, 2f)
        };

        internal static string FinalAbsolutePath => Absolute(FinalImagePath);

        internal static void InspectSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject marker = FindUnique(scene, TargetName);
            GameObject holo = FindUnique(scene, HoloTargetName);
            HoloSprayRightHandFollowBehaviour holoCarry = RequireHoloCarry(holo);
            AnimatorController holoController = RequireController(
                RequireAnimator(holo), HoloTargetName);
            RequireEqual(HoloProfilePath, AssetDatabase.GetAssetPath(holoCarry.Profile),
                "HoloSpray carry profile path");
            RequireEqual(HoloControllerPath, AssetDatabase.GetAssetPath(holoController),
                "HoloSpray controller path");
            RequireHoloController(holoController);
            string source = ExternalAbsolute();
            if (!File.Exists(source))
                throw new FileNotFoundException("Marking spray source FBX is missing.", source);
            if (marker.GetComponent<MarkerSprayRightHandFollowBehaviour>() != null)
                throw new InvalidOperationException(
                    "MarkerSpray_Idle already has a MarkerSpray follow component.");
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log(
                "[MarkerSprayIdle] Sources inspected read-only. sourceSha256=" +
                Sha256File(source) + ", holoProfile=" + ProfileSignature(holoCarry.Profile) +
                ", sceneDirty=" + scene.isDirty + ".");
        }

        internal static void ImportAssets()
        {
            RequireEditMode();
            EnsureFolder(ItemFolder);
            EnsureFolder(TextureFolder);
            EnsureFolder(MaterialFolder);
            string source = ExternalAbsolute();
            string destination = Absolute(ModelPath);
            if (!File.Exists(source))
                throw new FileNotFoundException("Marking spray source FBX is missing.", source);
            File.Copy(source, destination, true);
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);

            ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter ??
                throw new InvalidOperationException("Marking spray ModelImporter is unavailable.");
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
            if (!importer.ExtractTextures(TextureFolder))
                throw new InvalidOperationException(
                    "The marking spray FBX did not expose its embedded textures.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            List<Texture2D> textures = LoadTextures();
            Texture2D baseColor = FindTexture(textures, "base", "color");
            Texture2D normal = FindTexture(textures, "normal");
            Texture2D metallic = FindTexture(textures, "metallic");
            Texture2D roughness = FindTexture(textures, "roughness");
            Texture2D metallicSmoothness = CreateMetallicSmoothness(metallic, roughness);
            ConfigureTextureImporters(baseColor, normal, metallic, roughness,
                metallicSmoothness);

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                throw new InvalidOperationException("URP Lit shader is unavailable.");
            if (material == null)
            {
                material = new Material(shader) { name = "MarkingSpray" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else
            {
                material.shader = shader;
            }
            material.SetTexture("_BaseMap", baseColor);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", 1f);
            material.SetTexture("_MetallicGlossMap", metallicSmoothness);
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_Smoothness", 1f);
            material.SetFloat("_SmoothnessTextureChannel", 0f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter ??
                throw new InvalidOperationException("Marking spray ModelImporter was lost.");
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            Material[] embeddedMaterials = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<Material>().ToArray();
            if (embeddedMaterials.Length == 0)
                throw new InvalidOperationException("The FBX has no embedded material slot.");
            foreach (Material embedded in embeddedMaterials)
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(embedded), material);
            importer.SaveAndReimport();
            AssetDatabase.SaveAssets();

            RequireEqual(Sha256File(source), Sha256File(destination),
                "source/imported FBX hash");
            RequireImportedAppearance(material, textures);
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log(
                "[MarkerSprayIdle] Embedded textures/material imported without changing " +
                "the source FBX. textureCount=" + textures.Count + ".");
        }

        internal static void Apply()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            GameObject holo = FindUnique(scene, HoloTargetName);
            Animator animator = RequireAnimator(target);
            Animator holoAnimator = RequireAnimator(holo);
            HoloSprayRightHandFollowBehaviour holoCarry = RequireHoloCarry(holo);
            AnimatorController holoController = RequireController(holoAnimator, HoloTargetName);
            GameObject prefab = RequireAsset<GameObject>(ModelPath);
            Material material = RequireAsset<Material>(MaterialPath);
            RequireImportedAppearance(material, LoadTextures());

            string holoHierarchyBefore = HierarchySignature(holo.transform, "HoloSpray_Prop");
            string holoProfileBefore = ProfileSignature(holoCarry.Profile);
            string holoControllerBefore = ComputeAssetHash(HoloControllerPath);
            string sourceModelHash = Sha256File(ExternalAbsolute());

            EnsureFolder(AnimationFolder);
            MarkerSprayIdleCarryProfile profile =
                AssetDatabase.LoadAssetAtPath<MarkerSprayIdleCarryProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<MarkerSprayIdleCarryProfile>();
                profile.name = "MarkerSprayIdleCarryProfile";
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            profile.Configure(
                prefab,
                holoCarry.Profile.RightHandPath,
                holoCarry.Profile.PositionOffsetInHandSpace,
                holoCarry.Profile.RotationOffsetFromHand,
                holoCarry.Profile.HolderScale,
                holoCarry.Profile.ModelLocalPosition,
                holoCarry.Profile.ModelLocalRotation,
                holoCarry.Profile.ModelLocalScale,
                ConvertPose(holoCarry.Profile.SourceRightArmPose),
                ConvertPose(holoCarry.Profile.RightArmPose));
            EditorUtility.SetDirty(profile);

            MarkerSprayRightHandFollowBehaviour carry =
                target.GetComponent<MarkerSprayRightHandFollowBehaviour>();
            if (carry == null)
                carry = Undo.AddComponent<MarkerSprayRightHandFollowBehaviour>(target);
            carry.Configure(profile);
            EditorUtility.SetDirty(carry);

            AnimatorController controller = CopyHoloController(holoController);
            Undo.RecordObject(animator, "Configure MarkerSpray_Idle HoloSpray locomotion copy");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(carry);
            EditorUtility.SetDirty(animator);
            AssetDatabase.SaveAssets();

            RequireEqual(holoHierarchyBefore,
                HierarchySignature(holo.transform, "HoloSpray_Prop"), "HoloSpray hierarchy");
            RequireEqual(holoProfileBefore, ProfileSignature(holoCarry.Profile),
                "HoloSpray carry profile");
            RequireEqual(holoControllerBefore, ComputeAssetHash(HoloControllerPath),
                "HoloSpray controller");
            RequireEqual(sourceModelHash, Sha256File(ExternalAbsolute()),
                "marking spray source FBX");
            RequireProfileEquivalent(holoCarry.Profile, profile);
            RequireControllerEquivalent(holoController, controller);
            carry.RefreshPreview();

            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene could not be saved.");
            AssetDatabase.SaveAssets();
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log(
                "[MarkerSprayIdle] Exact HoloSpray grip, right-hand follow and six-motion " +
                "Blend Tree applied. Preexisting scene dirty state was preserved=" +
                sceneWasDirty + ". DoorOpener documentation is handled separately.");
        }

        internal static void Inspect()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject holo = FindUnique(scene, HoloTargetName);
            Animator animator = RequireAnimator(target);
            MarkerSprayRightHandFollowBehaviour carry = RequireMarkerCarry(target);
            HoloSprayRightHandFollowBehaviour holoCarry = RequireHoloCarry(holo);
            AnimatorController controller = RequireController(animator, TargetName);
            AnimatorController holoController = RequireController(
                RequireAnimator(holo), HoloTargetName);
            RequireEqual(ControllerPath, AssetDatabase.GetAssetPath(controller),
                "MarkerSpray controller path");
            RequireEqual(ProfilePath, AssetDatabase.GetAssetPath(carry.Profile),
                "MarkerSpray profile path");
            RequireEqual(Sha256File(ExternalAbsolute()), Sha256File(Absolute(ModelPath)),
                "source/imported FBX hash");
            RequireProfileEquivalent(holoCarry.Profile, carry.Profile);
            RequireControllerEquivalent(holoController, controller);
            RequireImportedAppearance(RequireAsset<Material>(MaterialPath), LoadTextures());
            if (animator.applyRootMotion || !animator.enabled ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                throw new InvalidOperationException("MarkerSpray Animator settings are invalid.");
            carry.RefreshPreview();
            if (carry.SprayHolder == null || carry.SprayModel == null)
                throw new InvalidOperationException("MarkerSpray preview model is missing.");
            if (Vector3.Distance(carry.SprayHolder.transform.position,
                    carry.ExpectedWorldPosition()) > 0.00001f ||
                Quaternion.Angle(carry.SprayHolder.transform.rotation,
                    carry.ExpectedWorldRotation()) > 0.01f)
                throw new InvalidOperationException("MarkerSpray right-hand follow differs.");
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log(
                "[MarkerSprayIdle] Inspection passed: exact HoloSpray pose/controller copy, " +
                "source-identical FBX, PBR textures/material and right-hand follow.");
        }

        internal static void CaptureFinal()
        {
            RequireEditMode();
            Inspect();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            AnimatorController controller = RequireController(RequireAnimator(target), TargetName);
            BlendTree rootTree = RequireRootTree(controller);
            ChildMotion[] children = rootTree.children;
            var fullPanels = new List<Texture2D>();
            var gripPanels = new List<Texture2D>();
            try
            {
                for (int phase = 0; phase < MarkerSprayIdleLocomotionCycleBehaviour.MotionCount;
                    phase++)
                {
                    GameObject clone = UnityEngine.Object.Instantiate(target);
                    clone.name = "MarkerSprayIdle_FinalReviewClone";
                    clone.hideFlags = HideFlags.HideAndDontSave;
                    SceneManager.MoveGameObjectToScene(clone, scene);
                    clone.transform.SetPositionAndRotation(
                        new Vector3(10000f, -10000f, 10000f), target.transform.rotation);
                    try
                    {
                        DisableUnrelatedBehaviours(clone);
                        Animator cloneAnimator = RequireAnimator(clone);
                        Motion motion = children[phase].motion;
                        SampleMotion(cloneAnimator, motion, 0.35);
                        MarkerSprayRightHandFollowBehaviour cloneCarry =
                            RequireMarkerCarry(clone);
                        RemoveUntrackedPreviewProps(clone.transform, cloneCarry);
                        cloneCarry.RefreshAnimatedPreviewForValidation();
                        fullPanels.Add(CapturePanel(clone.transform, cloneCarry, false));
                        gripPanels.Add(CapturePanel(clone.transform, cloneCarry, true));
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(clone);
                    }
                }
                ComposeReview(fullPanels, gripPanels, FinalAbsolutePath);
            }
            finally
            {
                foreach (Texture2D image in fullPanels) UnityEngine.Object.DestroyImmediate(image);
                foreach (Texture2D image in gripPanels) UnityEngine.Object.DestroyImmediate(image);
            }
            AssetDatabase.ImportAsset(FinalImagePath, ImportAssetOptions.ForceSynchronousImport);
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log(
                "[MarkerSprayIdle] Final direct six-motion/full-grip comparison captured once: " +
                FinalImagePath);
        }

        private static AnimatorController CopyHoloController(AnimatorController source)
        {
            BlendTree sourceRoot = RequireRootTree(source);
            ChildMotion[] sourceChildren = sourceRoot.children;
            var copied = new AnimationClip[5];
            int[] sourceIndices = { 0, 1, 2, 3, 5 };
            for (int i = 0; i < copied.Length; i++)
                copied[i] = CopyClip((AnimationClip)sourceChildren[sourceIndices[i]].motion,
                    ClipPaths[i], "MarkerSprayIdle_" + ClipNames[i]);

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller != null &&
                (controller.layers.Length == 0 || controller.layers[0].stateMachine == null))
            {
                AssetDatabase.DeleteAsset(ControllerPath);
                controller = null;
            }
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            foreach (AnimatorControllerParameter parameter in source.parameters)
                controller.AddParameter(new AnimatorControllerParameter
                {
                    name = parameter.name,
                    type = parameter.type,
                    defaultFloat = parameter.defaultFloat,
                    defaultInt = parameter.defaultInt,
                    defaultBool = parameter.defaultBool
                });
            AnimatorControllerLayer layer = controller.layers[0];
            layer.name = source.layers[0].name;
            layer.defaultWeight = source.layers[0].defaultWeight;
            layer.blendingMode = source.layers[0].blendingMode;
            layer.avatarMask = source.layers[0].avatarMask;
            AnimatorStateMachine machine = layer.stateMachine;
            foreach (AnimatorState state in machine.states.Select(item => item.state).ToArray())
                machine.RemoveState(state);
            foreach (AnimatorStateMachine nested in machine.stateMachines
                .Select(item => item.stateMachine).ToArray())
                machine.RemoveStateMachine(nested);

            var map = new Dictionary<AnimationClip, AnimationClip>
            {
                [(AnimationClip)sourceChildren[0].motion] = copied[0],
                [(AnimationClip)sourceChildren[1].motion] = copied[1],
                [(AnimationClip)sourceChildren[2].motion] = copied[2],
                [(AnimationClip)sourceChildren[3].motion] = copied[3],
                [(AnimationClip)sourceChildren[5].motion] = copied[4]
            };
            BlendTree diagonal = CloneTree(
                (BlendTree)sourceChildren[4].motion, DiagonalTreeName, controller, map);
            var root = new BlendTree
            {
                name = RootTreeName,
                blendType = sourceRoot.blendType,
                blendParameter = sourceRoot.blendParameter,
                blendParameterY = sourceRoot.blendParameterY,
                useAutomaticThresholds = sourceRoot.useAutomaticThresholds,
                minThreshold = sourceRoot.minThreshold,
                maxThreshold = sourceRoot.maxThreshold
            };
            AssetDatabase.AddObjectToAsset(root, controller);
            root.children = new[]
            {
                CopyChild(sourceChildren[0], copied[0]),
                CopyChild(sourceChildren[1], copied[1]),
                CopyChild(sourceChildren[2], copied[2]),
                CopyChild(sourceChildren[3], copied[3]),
                CopyChild(sourceChildren[4], diagonal),
                CopyChild(sourceChildren[5], copied[4])
            };
            AnimatorState sourceState = source.layers[0].stateMachine.defaultState;
            AnimatorState targetState = machine.AddState(StateName);
            targetState.motion = root;
            targetState.speed = sourceState.speed;
            targetState.cycleOffset = sourceState.cycleOffset;
            targetState.mirror = sourceState.mirror;
            targetState.writeDefaultValues = sourceState.writeDefaultValues;
            targetState.AddStateMachineBehaviour<MarkerSprayIdleLocomotionCycleBehaviour>();
            machine.defaultState = targetState;
            controller.layers = new[] { layer };
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(machine);
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(diagonal);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip CopyClip(AnimationClip source, string path, string name)
        {
            AnimationClip copy = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (copy == null)
            {
                copy = new AnimationClip();
                EditorUtility.CopySerialized(source, copy);
                copy.name = name;
                AssetDatabase.CreateAsset(copy, path);
            }
            else
            {
                EditorUtility.CopySerialized(source, copy);
                copy.name = name;
                EditorUtility.SetDirty(copy);
            }
            return copy;
        }

        private static BlendTree CloneTree(
            BlendTree source,
            string name,
            AnimatorController owner,
            IReadOnlyDictionary<AnimationClip, AnimationClip> clipMap)
        {
            var copy = new BlendTree
            {
                name = name,
                blendType = source.blendType,
                blendParameter = source.blendParameter,
                blendParameterY = source.blendParameterY,
                useAutomaticThresholds = source.useAutomaticThresholds,
                minThreshold = source.minThreshold,
                maxThreshold = source.maxThreshold
            };
            AssetDatabase.AddObjectToAsset(copy, owner);
            copy.children = source.children.Select((child, index) =>
            {
                Motion motion;
                if (child.motion is AnimationClip sourceClip)
                    motion = clipMap.TryGetValue(sourceClip, out AnimationClip mapped)
                        ? mapped
                        : throw new InvalidOperationException(
                            "HoloSpray diagonal clip mapping is missing.");
                else if (child.motion is BlendTree nested)
                    motion = CloneTree(nested, name + "_" + index, owner, clipMap);
                else
                    throw new InvalidOperationException("Unsupported diagonal motion.");
                return CopyChild(child, motion);
            }).ToArray();
            return copy;
        }

        private static ChildMotion CopyChild(ChildMotion source, Motion motion)
        {
            return new ChildMotion
            {
                motion = motion,
                threshold = source.threshold,
                position = source.position,
                timeScale = source.timeScale,
                cycleOffset = source.cycleOffset,
                mirror = source.mirror,
                directBlendParameter = source.directBlendParameter
            };
        }

        private static MarkerSprayBoneRotation[] ConvertPose(HoloSprayBoneRotation[] source)
        {
            return source.Select(item =>
                new MarkerSprayBoneRotation(item.Path, item.LocalRotation)).ToArray();
        }

        private static void RequireProfileEquivalent(
            HoloSprayIdleCarryProfile source,
            MarkerSprayIdleCarryProfile copy)
        {
            if (copy.SprayPrefab != RequireAsset<GameObject>(ModelPath) ||
                source.RightHandPath != copy.RightHandPath ||
                source.PositionOffsetInHandSpace != copy.PositionOffsetInHandSpace ||
                Quaternion.Angle(source.RotationOffsetFromHand,
                    copy.RotationOffsetFromHand) > 0.0001f ||
                source.HolderScale != copy.HolderScale ||
                source.ModelLocalPosition != copy.ModelLocalPosition ||
                Quaternion.Angle(source.ModelLocalRotation,
                    copy.ModelLocalRotation) > 0.0001f ||
                source.ModelLocalScale != copy.ModelLocalScale)
                throw new InvalidOperationException("MarkerSpray carry transform differs from HoloSpray.");
            RequirePoseEquivalent(source.SourceRightArmPose, copy.SourceRightArmPose,
                "source right-arm pose");
            RequirePoseEquivalent(source.RightArmPose, copy.RightArmPose, "right-arm grip pose");
        }

        private static void RequirePoseEquivalent(
            HoloSprayBoneRotation[] source,
            MarkerSprayBoneRotation[] copy,
            string label)
        {
            if (source.Length != copy.Length)
                throw new InvalidOperationException(label + " count differs.");
            for (int i = 0; i < source.Length; i++)
                if (source[i].Path != copy[i].Path ||
                    Quaternion.Angle(source[i].LocalRotation,
                        copy[i].LocalRotation) > 0.0001f)
                    throw new InvalidOperationException(label + " differs at " + i + ".");
        }

        private static void RequireControllerEquivalent(
            AnimatorController source,
            AnimatorController copy)
        {
            RequireHoloController(source);
            BlendTree a = RequireRootTree(source);
            BlendTree b = RequireRootTree(copy);
            if (a.blendType != b.blendType || a.blendParameter != b.blendParameter ||
                a.blendParameterY != b.blendParameterY ||
                a.children.Length != b.children.Length)
                throw new InvalidOperationException("MarkerSpray root Blend Tree differs.");
            for (int i = 0; i < a.children.Length; i++)
            {
                ChildMotion sourceChild = a.children[i];
                ChildMotion copyChild = b.children[i];
                if (sourceChild.position != copyChild.position ||
                    !Mathf.Approximately(sourceChild.timeScale, copyChild.timeScale) ||
                    !Mathf.Approximately(sourceChild.cycleOffset, copyChild.cycleOffset) ||
                    sourceChild.mirror != copyChild.mirror)
                    throw new InvalidOperationException("Blend Tree child differs at " + i + ".");
                if (sourceChild.motion is AnimationClip sourceClip)
                {
                    AnimationClip copyClip = copyChild.motion as AnimationClip ??
                        throw new InvalidOperationException("Copied clip is missing at " + i + ".");
                    RequireEqual(ClipSignature(sourceClip), ClipSignature(copyClip),
                        "clip " + i);
                }
                else
                {
                    RequireTreeEquivalent(
                        (BlendTree)sourceChild.motion, (BlendTree)copyChild.motion);
                }
            }
            AnimatorState copyState = copy.layers[0].stateMachine.defaultState;
            if (copyState == null || copyState.name != StateName ||
                copyState.behaviours
                    .OfType<MarkerSprayIdleLocomotionCycleBehaviour>().Count() != 1)
                throw new InvalidOperationException("MarkerSpray 1-second loop behaviour is invalid.");
        }

        private static void RequireHoloController(AnimatorController controller)
        {
            BlendTree tree = RequireRootTree(controller);
            if (tree.blendType != BlendTreeType.FreeformCartesian2D ||
                tree.children.Length != RequiredPositions.Length)
                throw new InvalidOperationException("HoloSpray 2D Blend Tree changed.");
            for (int i = 0; i < RequiredPositions.Length; i++)
                if ((tree.children[i].position - RequiredPositions[i]).sqrMagnitude > 0.00000001f)
                    throw new InvalidOperationException("HoloSpray child position changed at " + i + ".");
        }

        private static BlendTree RequireRootTree(AnimatorController controller)
        {
            AnimatorState state = controller.layers[0].stateMachine.defaultState ??
                throw new InvalidOperationException("Default animation state is missing.");
            return state.motion as BlendTree ??
                throw new InvalidOperationException("Root Blend Tree is missing.");
        }

        private static void RequireTreeEquivalent(BlendTree source, BlendTree copy)
        {
            if (copy == null || source.blendType != copy.blendType ||
                source.blendParameter != copy.blendParameter ||
                source.blendParameterY != copy.blendParameterY ||
                source.children.Length != copy.children.Length)
                throw new InvalidOperationException("Diagonal Blend Tree differs.");
            for (int i = 0; i < source.children.Length; i++)
            {
                ChildMotion a = source.children[i];
                ChildMotion b = copy.children[i];
                if (!Mathf.Approximately(a.threshold, b.threshold) ||
                    a.position != b.position || !Mathf.Approximately(a.timeScale, b.timeScale) ||
                    !Mathf.Approximately(a.cycleOffset, b.cycleOffset) || a.mirror != b.mirror)
                    throw new InvalidOperationException("Diagonal child differs at " + i + ".");
                RequireEqual(ClipSignature((AnimationClip)a.motion),
                    ClipSignature((AnimationClip)b.motion), "diagonal clip " + i);
            }
        }

        private static string ClipSignature(AnimationClip clip)
        {
            var result = new StringBuilder()
                .AppendLine("length=" + F(clip.length))
                .AppendLine("frameRate=" + F(clip.frameRate))
                .AppendLine("legacy=" + clip.legacy)
                .AppendLine("wrapMode=" + clip.wrapMode)
                .AppendLine("settings=" + EditorJsonUtility.ToJson(
                    AnimationUtility.GetAnimationClipSettings(clip)));
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip)
                .OrderBy(item => item.path, StringComparer.Ordinal)
                .ThenBy(item => item.type.FullName, StringComparer.Ordinal)
                .ThenBy(item => item.propertyName, StringComparer.Ordinal))
            {
                result.Append(binding.path).Append('|').Append(binding.type.FullName)
                    .Append('|').AppendLine(binding.propertyName);
                foreach (Keyframe key in AnimationUtility.GetEditorCurve(clip, binding).keys)
                    result.AppendLine(string.Join(",", new[]
                    {
                        F(key.time), F(key.value), F(key.inTangent), F(key.outTangent),
                        F(key.inWeight), F(key.outWeight), key.weightedMode.ToString()
                    }));
            }
            foreach (AnimationEvent item in AnimationUtility.GetAnimationEvents(clip))
                result.AppendLine("event|" + F(item.time) + "|" + item.functionName + "|" +
                    item.stringParameter + "|" + item.intParameter + "|" +
                    F(item.floatParameter) + "|" + item.messageOptions);
            return Sha256Text(result.ToString());
        }

        private static Texture2D CreateMetallicSmoothness(
            Texture2D metallic,
            Texture2D roughness)
        {
            SetReadable(metallic, true);
            SetReadable(roughness, true);
            if (metallic.width != roughness.width || metallic.height != roughness.height)
                throw new InvalidOperationException("Metallic and roughness texture sizes differ.");
            Color32[] metalPixels = metallic.GetPixels32();
            Color32[] roughPixels = roughness.GetPixels32();
            var output = new Texture2D(
                metallic.width, metallic.height, TextureFormat.RGBA32, false, true);
            var pixels = new Color32[metalPixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                byte metal = metalPixels[i].r;
                byte smoothness = (byte)(255 - roughPixels[i].r);
                pixels[i] = new Color32(metal, metal, metal, smoothness);
            }
            output.SetPixels32(pixels);
            output.Apply(false, false);
            File.WriteAllBytes(Absolute(MetallicSmoothnessPath), output.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(output);
            AssetDatabase.ImportAsset(MetallicSmoothnessPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            return RequireAsset<Texture2D>(MetallicSmoothnessPath);
        }

        private static void ConfigureTextureImporters(
            Texture2D baseColor,
            Texture2D normal,
            Texture2D metallic,
            Texture2D roughness,
            Texture2D metallicSmoothness)
        {
            SetSrgb(baseColor, true);
            SetNormal(normal);
            SetSrgb(metallic, false);
            SetSrgb(roughness, false);
            SetSrgb(metallicSmoothness, false);
            SetReadable(metallic, false);
            SetReadable(roughness, false);
        }

        private static void SetReadable(Texture2D texture, bool value)
        {
            TextureImporter importer = AssetImporter.GetAtPath(
                AssetDatabase.GetAssetPath(texture)) as TextureImporter ??
                throw new InvalidOperationException("TextureImporter is unavailable: " + texture.name);
            if (importer.isReadable == value) return;
            importer.isReadable = value;
            importer.SaveAndReimport();
        }

        private static void SetSrgb(Texture2D texture, bool value)
        {
            TextureImporter importer = AssetImporter.GetAtPath(
                AssetDatabase.GetAssetPath(texture)) as TextureImporter ??
                throw new InvalidOperationException("TextureImporter is unavailable: " + texture.name);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = value;
            importer.SaveAndReimport();
        }

        private static void SetNormal(Texture2D texture)
        {
            TextureImporter importer = AssetImporter.GetAtPath(
                AssetDatabase.GetAssetPath(texture)) as TextureImporter ??
                throw new InvalidOperationException("Normal TextureImporter is unavailable.");
            importer.textureType = TextureImporterType.NormalMap;
            importer.sRGBTexture = false;
            importer.SaveAndReimport();
        }

        private static List<Texture2D> LoadTextures()
        {
            return AssetDatabase.FindAssets("t:Texture2D", new[] { TextureFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path != MetallicSmoothnessPath)
                .Select(AssetDatabase.LoadAssetAtPath<Texture2D>)
                .Where(item => item != null).ToList();
        }

        private static Texture2D FindTexture(
            IEnumerable<Texture2D> textures,
            params string[] terms)
        {
            Texture2D match = textures.SingleOrDefault(texture =>
            {
                string name = texture.name.ToLowerInvariant();
                return terms.All(term => name.Contains(term));
            });
            return match ?? throw new MissingReferenceException(
                "Embedded texture is missing: " + string.Join("+", terms));
        }

        private static void RequireImportedAppearance(
            Material material,
            IReadOnlyCollection<Texture2D> textures)
        {
            if (textures.Count < 4)
                throw new InvalidOperationException("Not all embedded textures were extracted.");
            if (material.shader == null ||
                material.shader.name != "Universal Render Pipeline/Lit" ||
                material.GetTexture("_BaseMap") == null ||
                material.GetTexture("_BumpMap") == null ||
                material.GetTexture("_MetallicGlossMap") == null)
                throw new InvalidOperationException("Marking spray PBR material is incomplete.");
            GameObject prefab = RequireAsset<GameObject>(ModelPath);
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0 || renderers.Any(renderer =>
                    renderer.sharedMaterials.Any(item => item != material)))
                throw new InvalidOperationException("Marking spray renderer material remap is invalid.");
        }

        private static void DisableUnrelatedBehaviours(GameObject root)
        {
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (!(behaviour is MarkerSprayRightHandFollowBehaviour)) behaviour.enabled = false;
        }

        private static void RemoveUntrackedPreviewProps(
            Transform root,
            MarkerSprayRightHandFollowBehaviour carry)
        {
            GameObject tracked = carry.SprayHolder;
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == "MarkerSpray_Prop" &&
                    item.gameObject != tracked).ToArray())
                UnityEngine.Object.DestroyImmediate(item.gameObject);
        }

        private static void SampleMotion(Animator animator, Motion motion, double time)
        {
            PlayableGraph graph = PlayableGraph.Create("MarkerSprayIdle_FinalReviewGraph");
            try
            {
                AnimationPlayableOutput output = AnimationPlayableOutput.Create(
                    graph, "MarkerSprayIdle_FinalReviewOutput", animator);
                Playable playable = CreatePlayable(graph, motion, time);
                output.SetSourcePlayable(playable);
                graph.Play();
                graph.Evaluate(0f);
            }
            finally
            {
                graph.Destroy();
            }
        }

        private static Playable CreatePlayable(PlayableGraph graph, Motion motion, double time)
        {
            if (motion is AnimationClip clip)
            {
                AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);
                playable.SetTime(time % Math.Max(clip.length, 0.0001f));
                return playable;
            }
            BlendTree tree = motion as BlendTree ??
                throw new InvalidOperationException("Unsupported review motion.");
            ChildMotion[] children = tree.children;
            if (tree.blendType != BlendTreeType.Simple1D || children.Length != 2)
                throw new InvalidOperationException("Unexpected diagonal review tree.");
            AnimationMixerPlayable mixer = AnimationMixerPlayable.Create(graph, 2);
            for (int i = 0; i < 2; i++)
            {
                AnimationClip childClip = children[i].motion as AnimationClip ??
                    throw new InvalidOperationException("Diagonal child is not an AnimationClip.");
                AnimationClipPlayable child = AnimationClipPlayable.Create(graph, childClip);
                child.SetTime(time % Math.Max(childClip.length, 0.0001f));
                graph.Connect(child, 0, mixer, i);
                mixer.SetInputWeight(i, 0.5f);
            }
            return mixer;
        }

        private static Texture2D CapturePanel(
            Transform target,
            MarkerSprayRightHandFollowBehaviour carry,
            bool gripCloseup)
        {
            Bounds bounds = gripCloseup ? GripBounds(target, carry) : FullBounds(target, carry);
            Vector3 direction = (target.forward + target.right * 0.72f).normalized;
            var cameraObject = new GameObject("MarkerSprayIdle_FinalCamera");
            var lightObject = new GameObject("MarkerSprayIdle_FinalLight");
            RenderTexture render = RenderTexture.GetTemporary(
                512, 512, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = bounds.extents.magnitude * 1.08f;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 8f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.025f, 0.03f, 0.04f, 1f);
                Vector3 cameraUp = Vector3.ProjectOnPlane(target.up, direction).normalized;
                camera.transform.SetPositionAndRotation(
                    bounds.center + direction * 4f,
                    Quaternion.LookRotation(-direction, cameraUp));
                camera.targetTexture = render;
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.15f;
                light.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
                camera.Render();
                RenderTexture.active = render;
                var image = new Texture2D(512, 512, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void ComposeReview(
            IReadOnlyList<Texture2D> fullPanels,
            IReadOnlyList<Texture2D> gripPanels,
            string destination)
        {
            var composite = new Texture2D(3072, 1024, TextureFormat.RGB24, false);
            try
            {
                for (int i = 0; i < 6; i++)
                {
                    composite.SetPixels(i * 512, 512, 512, 512, fullPanels[i].GetPixels());
                    composite.SetPixels(i * 512, 0, 512, 512, gripPanels[i].GetPixels());
                }
                composite.Apply(false, false);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, composite.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(composite);
            }
        }

        private static Bounds FullBounds(
            Transform target,
            MarkerSprayRightHandFollowBehaviour carry)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true)
                .Concat(carry.SprayHolder.GetComponentsInChildren<Renderer>(true)).ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException("MarkerSpray_Idle has no renderers.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            bounds.Expand(0.15f);
            return bounds;
        }

        private static Bounds GripBounds(
            Transform target,
            MarkerSprayRightHandFollowBehaviour carry)
        {
            Transform upper = RequireDescendant(target, "RightArm");
            Transform fore = RequireDescendant(target, "RightForeArm");
            Transform hand = RequireDescendant(target, "RightHand");
            Bounds bounds = new Bounds(upper.position, Vector3.zero);
            bounds.Encapsulate(fore.position);
            bounds.Encapsulate(hand.position);
            foreach (Renderer renderer in carry.SprayHolder.GetComponentsInChildren<Renderer>(true))
                bounds.Encapsulate(renderer.bounds);
            bounds.Expand(0.13f);
            return bounds;
        }

        private static string ProfileSignature(HoloSprayIdleCarryProfile profile)
        {
            var text = new StringBuilder()
                .Append(profile.RightHandPath).Append('|')
                .Append(profile.PositionOffsetInHandSpace).Append('|')
                .Append(profile.RotationOffsetFromHand).Append('|')
                .Append(profile.HolderScale).Append('|')
                .Append(profile.ModelLocalPosition).Append('|')
                .Append(profile.ModelLocalRotation).Append('|')
                .Append(profile.ModelLocalScale);
            foreach (HoloSprayBoneRotation pose in profile.SourceRightArmPose)
                text.Append('|').Append(pose.Path).Append('|').Append(pose.LocalRotation);
            foreach (HoloSprayBoneRotation pose in profile.RightArmPose)
                text.Append('|').Append(pose.Path).Append('|').Append(pose.LocalRotation);
            return Sha256Text(text.ToString());
        }

        private static string HierarchySignature(Transform root, string ignoredName)
        {
            var text = new StringBuilder();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name != ignoredName && !HasAncestor(item, root, ignoredName))
                .OrderBy(item => AnimationUtility.CalculateTransformPath(item, root),
                    StringComparer.Ordinal))
                text.Append(AnimationUtility.CalculateTransformPath(item, root)).Append('|')
                    .Append(item.localPosition).Append('|').Append(item.localRotation).Append('|')
                    .AppendLine(item.localScale.ToString());
            return Sha256Text(text.ToString());
        }

        private static bool HasAncestor(Transform item, Transform root, string name)
        {
            for (Transform current = item.parent; current != null && current != root;
                current = current.parent)
                if (current.name == name) return true;
            return false;
        }

        private static HoloSprayRightHandFollowBehaviour RequireHoloCarry(GameObject target)
        {
            HoloSprayRightHandFollowBehaviour carry =
                target.GetComponent<HoloSprayRightHandFollowBehaviour>();
            if (carry == null || carry.Profile == null)
                throw new MissingReferenceException("HoloSpray right-hand follow is missing.");
            return carry;
        }

        private static MarkerSprayRightHandFollowBehaviour RequireMarkerCarry(GameObject target)
        {
            MarkerSprayRightHandFollowBehaviour carry =
                target.GetComponent<MarkerSprayRightHandFollowBehaviour>();
            if (carry == null || carry.Profile == null)
                throw new MissingReferenceException("MarkerSpray right-hand follow is missing.");
            return carry;
        }

        private static Animator RequireAnimator(GameObject root)
        {
            return root.GetComponent<Animator>() ??
                throw new MissingReferenceException(root.name + " Animator is missing.");
        }

        private static AnimatorController RequireController(Animator animator, string label)
        {
            return animator.runtimeAnimatorController as AnimatorController ??
                throw new InvalidOperationException(label + " does not use an AnimatorController.");
        }

        private static Transform RequireDescendant(Transform root, string name)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected one " + name + " below " + root.name + ".");
            return matches[0];
        }

        private static GameObject FindUnique(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name).Select(item => item.gameObject).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected one " + name + "; found " + matches.Length + ".");
            return matches[0];
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must be active. ActiveScene=" + scene.path);
            return scene;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("MarkerSpray operation requires Edit Mode.");
        }

        private static void EnsureFolder(string folder)
        {
            string[] segments = folder.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[i]);
                current = next;
            }
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset != null ? asset : throw new MissingReferenceException(
                "Asset is missing: " + path);
        }

        private static string ExternalAbsolute()
        {
            return Absolute(ExternalModelPath);
        }

        private static string Absolute(string relative)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relative));
        }

        private static string ComputeAssetHash(string assetPath)
        {
            return Sha256File(Absolute(assetPath));
        }

        private static string Sha256File(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Sha256Text(string value)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(
                    sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", string.Empty);
        }

        private static string F(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (expected != actual)
                throw new InvalidOperationException(
                    label + " differs. expected=" + expected + " actual=" + actual);
        }
    }
}
