using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.LongaArmaCargoRunScene
{
    internal static class LongaArmaWalkingFbxApply
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string WalkingFbxPath = "Assets/_Project/Art/Enemies/LongaArma/Models/longa_arma_walking.fbx";
        private const string WalkingControllerPath =
            "Assets/_Project/Art/Enemies/LongaArma/AnimatorControllers/LongaArma_Walking_FromFbx.controller";
        private const string BodyMaterialPath =
            "Assets/_Project/Art/Enemies/LongaArma/Materials/M_LongaLowPoly_WetMottledBody.mat";
        private const string DarkMaterialPath =
            "Assets/_Project/Art/Enemies/LongaArma/Materials/M_LongaLowPoly_DarkCrescentBlade.mat";
        private const string SlimeMaterialPath =
            "Assets/_Project/Art/Enemies/LongaArma/Materials/M_LongaLowPoly_GlossySlimeDrips.mat";
        private const string DeathPuddleFbxPath =
            "Assets/_Project/Art/Enemies/LongaArma/Models/dead.fbx";
        private const string IdleBodyMorphClipPath =
            "Assets/_Project/Art/Enemies/LongaArma/Animations/LongaArma_Idle_BodyMorph.anim";
        private const string IdleBodyMorphControllerPath =
            "Assets/_Project/Art/Enemies/LongaArma/AnimatorControllers/LongaArma_Idle_BodyMorph.controller";
        private const string AttackSlamDragClipPath =
            "Assets/_Project/Art/Enemies/LongaArma/Animations/LongaArma_Attack_SlamDrag.anim";
        private const string AttackSlamDragControllerPath =
            "Assets/_Project/Art/Enemies/LongaArma/AnimatorControllers/LongaArma_Attack_SlamDrag.controller";
        private const string HitRecoilClipPath =
            "Assets/_Project/Art/Enemies/LongaArma/Animations/LongaArma_Hit_Recoil.anim";
        private const string HitRecoilControllerPath =
            "Assets/_Project/Art/Enemies/LongaArma/AnimatorControllers/LongaArma_Hit_Recoil.controller";
        private const string ConsumePeckClipPath =
            "Assets/_Project/Art/Enemies/LongaArma/Animations/LongaArma_Consume_Peck.anim";
        private const string ConsumePeckControllerPath =
            "Assets/_Project/Art/Enemies/LongaArma/AnimatorControllers/LongaArma_Consume_Peck.controller";
        private const string DeathMeltPuddleClipPath =
            "Assets/_Project/Art/Enemies/LongaArma/Animations/LongaArma_Death_MeltPuddle.anim";
        private const string DeathMeltPuddleControllerPath =
            "Assets/_Project/Art/Enemies/LongaArma/AnimatorControllers/LongaArma_Death_MeltPuddle.controller";

        private const string PlacementRootName = "Approved Longa Arma Enemy Placement";
        private const string IdleRootName = "LongaArma_01_Idle";
        private const string MoveRootName = "LongaArma_02_Move_Crawl";
        private const string AttackRootName = "LongaArma_03_Attack_SlamDrag";
        private const string HitRootName = "LongaArma_04_Hit_Recoil";
        private const string ConsumeRootName = "LongaArma_05_Consume_Peck";
        private const string DeathRootName = "LongaArma_06_Death_MeltPuddle";
        private const string ApprovedModelName = "LongaArmaApproved_Model";
        private const string StaticModelName = "LongaArmaLowPolyFromOriginal_Model";
        private const string WalkingInstanceName = "LongaArmaWalkingFbx_Model";

        private static readonly string[] ApprovedStateRootNames =
        {
            "LongaArma_00_Static_Review",
            IdleRootName,
            MoveRootName,
            AttackRootName,
            HitRootName,
            ConsumeRootName,
            DeathRootName
        };

        private readonly struct DeathSlimeProxyVisuals
        {
            public DeathSlimeProxyVisuals(
                Transform bodyMass,
                Transform chestFlow,
                Transform headFlow,
                Transform bladeFlow,
                Transform finalPuddle)
            {
                BodyMass = bodyMass;
                ChestFlow = chestFlow;
                HeadFlow = headFlow;
                BladeFlow = bladeFlow;
                FinalPuddle = finalPuddle;
            }

            public Transform BodyMass { get; }

            public Transform ChestFlow { get; }

            public Transform HeadFlow { get; }

            public Transform BladeFlow { get; }

            public Transform FinalPuddle { get; }
        }

        public static void ApplyWalkingFbxToMoveOnly()
        {
            ConfigureWalkingFbxImporter();

            var walkingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WalkingFbxPath);
            if (walkingPrefab == null)
            {
                throw new FileNotFoundException("Walking FBX prefab was not found.", WalkingFbxPath);
            }

            var walkingClip = LoadWalkingClip();
            var walkingController = EnsureWalkingController(walkingClip);
            var walkingAvatar = AssetDatabase.LoadAllAssetsAtPath(WalkingFbxPath).OfType<Avatar>().FirstOrDefault();

            var scene = OpenCargoRunScene();
            var placementRoot = FindRoot(scene, PlacementRootName);
            var moveRoot = FindChildRecursive(placementRoot.transform, MoveRootName);
            if (moveRoot == null)
            {
                throw new InvalidOperationException("Move state root was not found: " + MoveRootName);
            }

            var approvedModel = FindChildRecursive(moveRoot.transform, ApprovedModelName);
            if (approvedModel == null)
            {
                throw new InvalidOperationException("Approved model root was not found under move state.");
            }

            var existingWalking = FindChildRecursive(approvedModel, WalkingInstanceName);
            if (existingWalking != null)
            {
                UnityEngine.Object.DestroyImmediate(existingWalking.gameObject);
            }

            var staticModel = FindChildRecursive(approvedModel, StaticModelName);
            var staticBounds = TryGetRendererBounds(staticModel != null ? staticModel.gameObject : null, out var bounds, includeInactive: true)
                ? bounds
                : default;

            var walkingInstance = (GameObject)PrefabUtility.InstantiatePrefab(walkingPrefab, scene);
            walkingInstance.name = WalkingInstanceName;
            walkingInstance.transform.SetParent(approvedModel, false);
            walkingInstance.transform.localPosition = Vector3.zero;
            walkingInstance.transform.localRotation = Quaternion.identity;
            walkingInstance.transform.localScale = Vector3.one;

            DisableImportedReviewObjects(walkingInstance.transform);
            var animator = walkingInstance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = walkingInstance.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = walkingController;
            animator.avatar = walkingAvatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            AssignWalkingMaterials(walkingInstance);

            if (staticBounds.size != Vector3.zero &&
                TryGetRendererBounds(walkingInstance, out var walkingBounds) &&
                walkingBounds.size != Vector3.zero)
            {
                FitToReferenceBounds(walkingInstance.transform, approvedModel, staticBounds, walkingBounds);
            }

            if (staticModel != null)
            {
                staticModel.gameObject.SetActive(false);
            }

            var parentAnimator = approvedModel.GetComponent<Animator>();
            if (parentAnimator != null)
            {
                parentAnimator.enabled = false;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp scene could not be saved after applying walking FBX.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Applied Longa Arma walking FBX to move state only. " +
                "Clip=" + walkingClip.name +
                ", ClipLength=" + walkingClip.length.ToString("0.###") +
                ", LoopTime=" + IsLoopingClip(walkingClip) +
                ", Avatar=" + (walkingAvatar != null ? walkingAvatar.name : "<none>") +
                ", MaterialsApplied=" + CountVisibleRenderers(walkingInstance) +
                ", Controller=" + WalkingControllerPath +
                ", Target=" + PlacementRootName + "/" + MoveRootName);
        }

        public static void ReplaceRemainingApprovedStatesWithMoveWalkingFbxCopy()
        {
            ConfigureWalkingFbxImporter();

            var walkingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WalkingFbxPath);
            if (walkingPrefab == null)
            {
                throw new FileNotFoundException("Walking FBX prefab was not found.", WalkingFbxPath);
            }

            var walkingClip = LoadWalkingClip();
            var walkingController = EnsureWalkingController(walkingClip);
            var walkingAvatar = AssetDatabase.LoadAllAssetsAtPath(WalkingFbxPath).OfType<Avatar>().FirstOrDefault();

            var scene = OpenCargoRunScene();
            var placementRoot = FindRoot(scene, PlacementRootName);
            var moveRoot = FindChildRecursive(placementRoot.transform, MoveRootName);
            if (moveRoot == null)
            {
                throw new InvalidOperationException("Move state root was not found: " + MoveRootName);
            }

            var moveApprovedModel = FindChildRecursive(moveRoot, ApprovedModelName);
            if (moveApprovedModel == null)
            {
                throw new InvalidOperationException("Approved model root was not found under move state.");
            }

            var sourceWalking = FindChildRecursive(moveApprovedModel, WalkingInstanceName);
            if (sourceWalking == null)
            {
                throw new InvalidOperationException(
                    "Move state walking model was not found. Apply " + nameof(ApplyWalkingFbxToMoveOnly) + " first.");
            }

            var targetPairs = ApprovedStateRootNames
                .Where(name => name != MoveRootName)
                .Select(stateRootName =>
                {
                    var stateRoot = FindChildRecursive(placementRoot.transform, stateRootName);
                    if (stateRoot == null)
                    {
                        throw new InvalidOperationException(
                            "Approved Longa Arma state root was not found: " + stateRootName);
                    }

                    var approvedModel = FindChildRecursive(stateRoot, ApprovedModelName);
                    if (approvedModel == null)
                    {
                        throw new InvalidOperationException(
                            "Approved model root was not found under state: " + stateRootName);
                    }

                    return new
                    {
                        Name = stateRootName,
                        StateRoot = stateRoot,
                        ApprovedModel = approvedModel
                    };
                })
                .ToArray();

            var replaced = 0;
            var targets = string.Empty;
            foreach (var target in targetPairs)
            {
                var beforePosition = target.StateRoot.localPosition;
                var beforeRotation = target.StateRoot.localRotation;
                var beforeScale = target.StateRoot.localScale;

                ReplaceApprovedModelChildrenWithWalkingCopy(
                    scene,
                    walkingPrefab,
                    sourceWalking,
                    target.ApprovedModel,
                    walkingController,
                    walkingAvatar);

                if (target.StateRoot.localPosition != beforePosition ||
                    target.StateRoot.localRotation != beforeRotation ||
                    target.StateRoot.localScale != beforeScale)
                {
                    throw new InvalidOperationException("State root transform changed unexpectedly: " + target.Name);
                }

                replaced++;
                targets += (targets.Length > 0 ? ", " : string.Empty) + target.Name;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp scene could not be saved after replacing Longa Arma models.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Replaced remaining approved Longa Arma states with move walking FBX copy. " +
                "Replaced=" + replaced +
                ", Source=" + PlacementRootName + "/" + MoveRootName + "/" + WalkingInstanceName +
                ", Targets=" + targets +
                ", Clip=" + walkingClip.name +
                ", Controller=" + WalkingControllerPath);
        }

        public static void RemoveAnimationComponentsFromNonMoveApprovedStates()
        {
            var scene = OpenCargoRunScene();
            var placementRoot = FindRoot(scene, PlacementRootName);
            var moveRoot = FindChildRecursive(placementRoot.transform, MoveRootName);
            if (moveRoot == null)
            {
                throw new InvalidOperationException("Move state root was not found: " + MoveRootName);
            }

            var moveApprovedModel = FindChildRecursive(moveRoot, ApprovedModelName);
            if (moveApprovedModel == null)
            {
                throw new InvalidOperationException("Approved model root was not found under move state.");
            }

            var moveWalking = FindChildRecursive(moveApprovedModel, WalkingInstanceName);
            if (moveWalking == null)
            {
                throw new InvalidOperationException("Move state walking model was not found.");
            }

            var moveAnimatorCount = moveWalking.GetComponentsInChildren<Animator>(true).Length;
            if (moveAnimatorCount == 0)
            {
                throw new InvalidOperationException("Move state walking model has no Animator to preserve.");
            }

            var targetPairs = ApprovedStateRootNames
                .Where(name => name != MoveRootName)
                .Select(stateRootName =>
                {
                    var stateRoot = FindChildRecursive(placementRoot.transform, stateRootName);
                    if (stateRoot == null)
                    {
                        throw new InvalidOperationException(
                            "Approved Longa Arma state root was not found: " + stateRootName);
                    }

                    var approvedModel = FindChildRecursive(stateRoot, ApprovedModelName);
                    if (approvedModel == null)
                    {
                        throw new InvalidOperationException(
                            "Approved model root was not found under state: " + stateRootName);
                    }

                    var walking = FindChildRecursive(approvedModel, WalkingInstanceName);
                    if (walking == null)
                    {
                        throw new InvalidOperationException(
                            "Walking model was not found under state: " + stateRootName);
                    }

                    return new
                    {
                        Name = stateRootName,
                        StateRoot = stateRoot,
                        Walking = walking
                    };
                })
                .ToArray();

            var removedAnimators = 0;
            var removedAnimations = 0;
            var skinnedRendererCount = 0;
            var skinnedBoneCount = 0;
            var targets = string.Empty;

            foreach (var target in targetPairs)
            {
                var beforePosition = target.StateRoot.localPosition;
                var beforeRotation = target.StateRoot.localRotation;
                var beforeScale = target.StateRoot.localScale;
                var walkingPosition = target.Walking.localPosition;
                var walkingRotation = target.Walking.localRotation;
                var walkingScale = target.Walking.localScale;

                var renderers = target.Walking.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var bonesBefore = renderers.Sum(renderer => renderer.bones != null ? renderer.bones.Length : 0);
                if (renderers.Length == 0 || bonesBefore == 0)
                {
                    throw new InvalidOperationException("Rigged walking model was not found under state: " + target.Name);
                }

                removedAnimators += DestroyComponentsInChildren<Animator>(target.Walking);
                removedAnimations += DestroyComponentsInChildren<UnityEngine.Animation>(target.Walking);

                var afterRenderers = target.Walking.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var bonesAfter = afterRenderers.Sum(renderer => renderer.bones != null ? renderer.bones.Length : 0);
                if (afterRenderers.Length != renderers.Length || bonesAfter != bonesBefore)
                {
                    throw new InvalidOperationException("Rigging changed unexpectedly under state: " + target.Name);
                }

                if (target.StateRoot.localPosition != beforePosition ||
                    target.StateRoot.localRotation != beforeRotation ||
                    target.StateRoot.localScale != beforeScale ||
                    target.Walking.localPosition != walkingPosition ||
                    target.Walking.localRotation != walkingRotation ||
                    target.Walking.localScale != walkingScale)
                {
                    throw new InvalidOperationException("Transform changed unexpectedly under state: " + target.Name);
                }

                skinnedRendererCount += afterRenderers.Length;
                skinnedBoneCount += bonesAfter;
                targets += (targets.Length > 0 ? ", " : string.Empty) + target.Name;
            }

            if (moveWalking.GetComponentsInChildren<Animator>(true).Length != moveAnimatorCount)
            {
                throw new InvalidOperationException("Move state Animator count changed unexpectedly.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp scene could not be saved after removing non-move Longa Arma animation components.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Removed animation components from non-move approved Longa Arma states. " +
                "MoveStateAnimatorsPreserved=" + moveAnimatorCount +
                ", RemovedAnimators=" + removedAnimators +
                ", RemovedAnimations=" + removedAnimations +
                ", RigSkinnedRenderersPreserved=" + skinnedRendererCount +
                ", RigBonesPreserved=" + skinnedBoneCount +
                ", Targets=" + targets);
        }

        public static void ApplyIdleBodyMorphToIdleState()
        {
            var scene = OpenCargoRunScene();
            var placementRoot = FindRoot(scene, PlacementRootName);
            var idleRoot = FindChildRecursive(placementRoot.transform, IdleRootName);
            if (idleRoot == null)
            {
                throw new InvalidOperationException("Idle state root was not found: " + IdleRootName);
            }

            var idleApprovedModel = FindChildRecursive(idleRoot, ApprovedModelName);
            if (idleApprovedModel == null)
            {
                throw new InvalidOperationException("Approved model root was not found under idle state.");
            }

            var idleWalking = FindChildRecursive(idleApprovedModel, WalkingInstanceName);
            if (idleWalking == null)
            {
                throw new InvalidOperationException("Idle walking model was not found.");
            }

            var beforeIdlePosition = idleRoot.localPosition;
            var beforeIdleRotation = idleRoot.localRotation;
            var beforeIdleScale = idleRoot.localScale;
            var beforeWalkingPosition = idleWalking.localPosition;
            var beforeWalkingRotation = idleWalking.localRotation;
            var beforeWalkingScale = idleWalking.localScale;

            var skinnedRenderers = idleWalking.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var rigBoneCount = skinnedRenderers.Sum(renderer => renderer.bones != null ? renderer.bones.Length : 0);
            if (skinnedRenderers.Length == 0 || rigBoneCount == 0)
            {
                throw new InvalidOperationException("Idle walking model has no preserved rigged mesh.");
            }

            var clip = EnsureIdleBodyMorphClip(idleWalking.gameObject);
            var controller = EnsureIdleBodyMorphController(clip);
            var walkingAvatar = AssetDatabase.LoadAllAssetsAtPath(WalkingFbxPath).OfType<Avatar>().FirstOrDefault();

            var animator = idleWalking.GetComponent<Animator>();
            if (animator == null)
            {
                animator = idleWalking.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = walkingAvatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);

            if (idleRoot.localPosition != beforeIdlePosition ||
                idleRoot.localRotation != beforeIdleRotation ||
                idleRoot.localScale != beforeIdleScale ||
                idleWalking.localPosition != beforeWalkingPosition ||
                idleWalking.localRotation != beforeWalkingRotation ||
                idleWalking.localScale != beforeWalkingScale)
            {
                throw new InvalidOperationException("Idle root or walking model transform changed unexpectedly.");
            }

            var moveAnimatorCount = CountWalkingAnimatorsForState(placementRoot.transform, MoveRootName);
            if (moveAnimatorCount == 0)
            {
                throw new InvalidOperationException("Move state walking Animator was not preserved.");
            }

            var otherNonMoveAnimatorCount = ApprovedStateRootNames
                .Where(name => name != IdleRootName && name != MoveRootName)
                .Sum(name => CountWalkingAnimatorsForState(placementRoot.transform, name));
            if (otherNonMoveAnimatorCount != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Animator exists on non-idle, non-move Longa Arma states: " + otherNonMoveAnimatorCount);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp scene could not be saved after applying idle body morph.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Applied Longa Arma idle body morph to idle state only. " +
                "Clip=" + IdleBodyMorphClipPath +
                ", Controller=" + IdleBodyMorphControllerPath +
                ", Target=" + PlacementRootName + "/" + IdleRootName + "/" + WalkingInstanceName +
                ", IdleAnimators=" + idleWalking.GetComponentsInChildren<Animator>(true).Length +
                ", MoveStateAnimatorsPreserved=" + moveAnimatorCount +
                ", OtherNonMoveAnimators=" + otherNonMoveAnimatorCount +
                ", RigSkinnedRenderersPreserved=" + skinnedRenderers.Length +
                ", RigBonesPreserved=" + rigBoneCount);
        }

        public static void ApplyAttackSlamDragToAttackState()
        {
            var scene = OpenCargoRunScene();
            var placementRoot = FindRoot(scene, PlacementRootName);
            var attackRoot = FindChildRecursive(placementRoot.transform, AttackRootName);
            if (attackRoot == null)
            {
                throw new InvalidOperationException("Attack state root was not found: " + AttackRootName);
            }

            var attackApprovedModel = FindChildRecursive(attackRoot, ApprovedModelName);
            if (attackApprovedModel == null)
            {
                throw new InvalidOperationException("Approved model root was not found under attack state.");
            }

            var attackWalking = FindChildRecursive(attackApprovedModel, WalkingInstanceName);
            if (attackWalking == null)
            {
                throw new InvalidOperationException("Attack walking model was not found.");
            }

            var beforeAttackPosition = attackRoot.localPosition;
            var beforeAttackRotation = attackRoot.localRotation;
            var beforeAttackScale = attackRoot.localScale;
            var beforeWalkingPosition = attackWalking.localPosition;
            var beforeWalkingRotation = attackWalking.localRotation;
            var beforeWalkingScale = attackWalking.localScale;

            var skinnedRenderers = attackWalking.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var rigBoneCount = skinnedRenderers.Sum(renderer => renderer.bones != null ? renderer.bones.Length : 0);
            if (skinnedRenderers.Length == 0 || rigBoneCount == 0)
            {
                throw new InvalidOperationException("Attack walking model has no preserved rigged mesh.");
            }

            var clip = EnsureAttackSlamDragClip(attackWalking.gameObject);
            var controller = EnsureSingleClipController(
                AttackSlamDragControllerPath,
                "LongaArma_Attack_SlamDrag",
                clip);
            var walkingAvatar = AssetDatabase.LoadAllAssetsAtPath(WalkingFbxPath).OfType<Avatar>().FirstOrDefault();

            var animator = attackWalking.GetComponent<Animator>();
            if (animator == null)
            {
                animator = attackWalking.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = walkingAvatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);

            if (attackRoot.localPosition != beforeAttackPosition ||
                attackRoot.localRotation != beforeAttackRotation ||
                attackRoot.localScale != beforeAttackScale ||
                attackWalking.localPosition != beforeWalkingPosition ||
                attackWalking.localRotation != beforeWalkingRotation ||
                attackWalking.localScale != beforeWalkingScale)
            {
                throw new InvalidOperationException("Attack root or walking model transform changed unexpectedly.");
            }

            var idleAnimatorCount = CountWalkingAnimatorsForState(placementRoot.transform, IdleRootName);
            var moveAnimatorCount = CountWalkingAnimatorsForState(placementRoot.transform, MoveRootName);
            if (idleAnimatorCount == 0)
            {
                throw new InvalidOperationException("Idle state Animator was not preserved.");
            }

            if (moveAnimatorCount == 0)
            {
                throw new InvalidOperationException("Move state walking Animator was not preserved.");
            }

            var otherNonMoveAnimatorCount = ApprovedStateRootNames
                .Where(name => name != IdleRootName && name != MoveRootName && name != AttackRootName)
                .Sum(name => CountWalkingAnimatorsForState(placementRoot.transform, name));
            if (otherNonMoveAnimatorCount != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Animator exists on non-idle, non-move, non-attack Longa Arma states: " +
                    otherNonMoveAnimatorCount);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp scene could not be saved after applying attack slam drag.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Applied Longa Arma attack slam-drag to attack state only. " +
                "Clip=" + AttackSlamDragClipPath +
                ", Controller=" + AttackSlamDragControllerPath +
                ", Target=" + PlacementRootName + "/" + AttackRootName + "/" + WalkingInstanceName +
                ", AttackAnimators=" + attackWalking.GetComponentsInChildren<Animator>(true).Length +
                ", IdleAnimatorsPreserved=" + idleAnimatorCount +
                ", MoveStateAnimatorsPreserved=" + moveAnimatorCount +
                ", OtherNonTargetAnimators=" + otherNonMoveAnimatorCount +
                ", RigSkinnedRenderersPreserved=" + skinnedRenderers.Length +
                ", RigBonesPreserved=" + rigBoneCount);
        }

        public static void ApplyHitRecoilToHitState()
        {
            var scene = OpenCargoRunScene();
            var placementRoot = FindRoot(scene, PlacementRootName);
            var hitRoot = FindChildRecursive(placementRoot.transform, HitRootName);
            if (hitRoot == null)
            {
                throw new InvalidOperationException("Hit state root was not found: " + HitRootName);
            }

            var hitApprovedModel = FindChildRecursive(hitRoot, ApprovedModelName);
            if (hitApprovedModel == null)
            {
                throw new InvalidOperationException("Approved model root was not found under hit state.");
            }

            var hitWalking = FindChildRecursive(hitApprovedModel, WalkingInstanceName);
            if (hitWalking == null)
            {
                throw new InvalidOperationException("Hit walking model was not found.");
            }

            var beforeHitPosition = hitRoot.localPosition;
            var beforeHitRotation = hitRoot.localRotation;
            var beforeHitScale = hitRoot.localScale;
            var beforeWalkingPosition = hitWalking.localPosition;
            var beforeWalkingRotation = hitWalking.localRotation;
            var beforeWalkingScale = hitWalking.localScale;

            var skinnedRenderers = hitWalking.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var rigBoneCount = skinnedRenderers.Sum(renderer => renderer.bones != null ? renderer.bones.Length : 0);
            if (skinnedRenderers.Length == 0 || rigBoneCount == 0)
            {
                throw new InvalidOperationException("Hit walking model has no preserved rigged mesh.");
            }

            var clip = EnsureHitRecoilClip(hitWalking.gameObject);
            var controller = EnsureSingleClipController(
                HitRecoilControllerPath,
                "LongaArma_Hit_Recoil",
                clip);
            var walkingAvatar = AssetDatabase.LoadAllAssetsAtPath(WalkingFbxPath).OfType<Avatar>().FirstOrDefault();

            var animator = hitWalking.GetComponent<Animator>();
            if (animator == null)
            {
                animator = hitWalking.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = walkingAvatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);

            if (hitRoot.localPosition != beforeHitPosition ||
                hitRoot.localRotation != beforeHitRotation ||
                hitRoot.localScale != beforeHitScale ||
                hitWalking.localPosition != beforeWalkingPosition ||
                hitWalking.localRotation != beforeWalkingRotation ||
                hitWalking.localScale != beforeWalkingScale)
            {
                throw new InvalidOperationException("Hit root or walking model transform changed unexpectedly.");
            }

            var idleAnimatorCount = CountWalkingAnimatorsForState(placementRoot.transform, IdleRootName);
            var moveAnimatorCount = CountWalkingAnimatorsForState(placementRoot.transform, MoveRootName);
            var attackAnimatorCount = CountWalkingAnimatorsForState(placementRoot.transform, AttackRootName);
            if (idleAnimatorCount == 0)
            {
                throw new InvalidOperationException("Idle state Animator was not preserved.");
            }

            if (moveAnimatorCount == 0)
            {
                throw new InvalidOperationException("Move state walking Animator was not preserved.");
            }

            if (attackAnimatorCount == 0)
            {
                throw new InvalidOperationException("Attack state Animator was not preserved.");
            }

            var otherNonTargetAnimatorCount = ApprovedStateRootNames
                .Where(name => name != IdleRootName && name != MoveRootName && name != AttackRootName && name != HitRootName)
                .Sum(name => CountWalkingAnimatorsForState(placementRoot.transform, name));
            if (otherNonTargetAnimatorCount != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Animator exists on non-idle, non-move, non-attack, non-hit Longa Arma states: " +
                    otherNonTargetAnimatorCount);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp scene could not be saved after applying hit recoil.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Applied Longa Arma hit recoil to hit state only. " +
                "Clip=" + HitRecoilClipPath +
                ", Controller=" + HitRecoilControllerPath +
                ", Target=" + PlacementRootName + "/" + HitRootName + "/" + WalkingInstanceName +
                ", HitAnimators=" + hitWalking.GetComponentsInChildren<Animator>(true).Length +
                ", IdleAnimatorsPreserved=" + idleAnimatorCount +
                ", MoveStateAnimatorsPreserved=" + moveAnimatorCount +
                ", AttackAnimatorsPreserved=" + attackAnimatorCount +
                ", OtherNonTargetAnimators=" + otherNonTargetAnimatorCount +
                ", RigSkinnedRenderersPreserved=" + skinnedRenderers.Length +
                ", RigBonesPreserved=" + rigBoneCount);
        }

        public static void ApplyConsumePeckToConsumeState()
        {
            var scene = OpenCargoRunScene();
            var placementRoot = FindRoot(scene, PlacementRootName);
            var consumeRoot = FindChildRecursive(placementRoot.transform, ConsumeRootName);
            if (consumeRoot == null)
            {
                throw new InvalidOperationException("Consume state root was not found: " + ConsumeRootName);
            }

            var consumeApprovedModel = FindChildRecursive(consumeRoot, ApprovedModelName);
            if (consumeApprovedModel == null)
            {
                throw new InvalidOperationException("Approved model root was not found under consume state.");
            }

            var consumeWalking = FindChildRecursive(consumeApprovedModel, WalkingInstanceName);
            if (consumeWalking == null)
            {
                throw new InvalidOperationException("Consume walking model was not found.");
            }

            var beforeConsumePosition = consumeRoot.localPosition;
            var beforeConsumeRotation = consumeRoot.localRotation;
            var beforeConsumeScale = consumeRoot.localScale;
            var beforeWalkingPosition = consumeWalking.localPosition;
            var beforeWalkingRotation = consumeWalking.localRotation;
            var beforeWalkingScale = consumeWalking.localScale;

            var skinnedRenderers = consumeWalking.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var rigBoneCount = skinnedRenderers.Sum(renderer => renderer.bones != null ? renderer.bones.Length : 0);
            if (skinnedRenderers.Length == 0 || rigBoneCount == 0)
            {
                throw new InvalidOperationException("Consume walking model has no preserved rigged mesh.");
            }

            var clip = EnsureConsumePeckClip(consumeWalking.gameObject);
            var controller = EnsureSingleClipController(
                ConsumePeckControllerPath,
                "LongaArma_Consume_Peck",
                clip);
            var walkingAvatar = AssetDatabase.LoadAllAssetsAtPath(WalkingFbxPath).OfType<Avatar>().FirstOrDefault();

            var animator = consumeWalking.GetComponent<Animator>();
            if (animator == null)
            {
                animator = consumeWalking.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = walkingAvatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);

            if (consumeRoot.localPosition != beforeConsumePosition ||
                consumeRoot.localRotation != beforeConsumeRotation ||
                consumeRoot.localScale != beforeConsumeScale ||
                consumeWalking.localPosition != beforeWalkingPosition ||
                consumeWalking.localRotation != beforeWalkingRotation ||
                consumeWalking.localScale != beforeWalkingScale)
            {
                throw new InvalidOperationException("Consume root or walking model transform changed unexpectedly.");
            }

            var idleAnimatorCount = CountWalkingAnimatorsForState(placementRoot.transform, IdleRootName);
            var moveAnimatorCount = CountWalkingAnimatorsForState(placementRoot.transform, MoveRootName);
            var attackAnimatorCount = CountWalkingAnimatorsForState(placementRoot.transform, AttackRootName);
            var hitAnimatorCount = CountWalkingAnimatorsForState(placementRoot.transform, HitRootName);
            if (idleAnimatorCount == 0)
            {
                throw new InvalidOperationException("Idle state Animator was not preserved.");
            }

            if (moveAnimatorCount == 0)
            {
                throw new InvalidOperationException("Move state walking Animator was not preserved.");
            }

            if (attackAnimatorCount == 0)
            {
                throw new InvalidOperationException("Attack state Animator was not preserved.");
            }

            if (hitAnimatorCount == 0)
            {
                throw new InvalidOperationException("Hit state Animator was not preserved.");
            }

            var otherNonTargetAnimatorCount = ApprovedStateRootNames
                .Where(name => name != IdleRootName &&
                    name != MoveRootName &&
                    name != AttackRootName &&
                    name != HitRootName &&
                    name != ConsumeRootName)
                .Sum(name => CountWalkingAnimatorsForState(placementRoot.transform, name));
            if (otherNonTargetAnimatorCount != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Animator exists on non-idle, non-move, non-attack, non-hit, non-consume Longa Arma states: " +
                    otherNonTargetAnimatorCount);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp scene could not be saved after applying consume peck.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Applied Longa Arma consume peck to consume state only. " +
                "Clip=" + ConsumePeckClipPath +
                ", Controller=" + ConsumePeckControllerPath +
                ", Target=" + PlacementRootName + "/" + ConsumeRootName + "/" + WalkingInstanceName +
                ", ConsumeAnimators=" + consumeWalking.GetComponentsInChildren<Animator>(true).Length +
                ", IdleAnimatorsPreserved=" + idleAnimatorCount +
                ", MoveStateAnimatorsPreserved=" + moveAnimatorCount +
                ", AttackAnimatorsPreserved=" + attackAnimatorCount +
                ", HitAnimatorsPreserved=" + hitAnimatorCount +
                ", OtherNonTargetAnimators=" + otherNonTargetAnimatorCount +
                ", RigSkinnedRenderersPreserved=" + skinnedRenderers.Length +
                ", RigBonesPreserved=" + rigBoneCount);
        }

        public static void ApplyDeathMeltPuddleToDeathState()
        {
            var scene = OpenCargoRunScene();
            var placementRoot = FindRoot(scene, PlacementRootName);
            var deathRoot = FindChildRecursive(placementRoot.transform, DeathRootName);
            if (deathRoot == null)
            {
                throw new InvalidOperationException("Death state root was not found: " + DeathRootName);
            }

            var deathApprovedModel = FindChildRecursive(deathRoot, ApprovedModelName);
            if (deathApprovedModel == null)
            {
                throw new InvalidOperationException("Approved model root was not found under death state.");
            }

            var deathWalking = FindChildRecursive(deathApprovedModel, WalkingInstanceName);
            if (deathWalking == null)
            {
                throw new InvalidOperationException("Death walking model was not found.");
            }

            var beforeDeathPosition = deathRoot.localPosition;
            var beforeDeathRotation = deathRoot.localRotation;
            var beforeDeathScale = deathRoot.localScale;
            var beforeWalkingPosition = deathWalking.localPosition;
            var beforeWalkingRotation = deathWalking.localRotation;
            var beforeWalkingScale = deathWalking.localScale;

            var skinnedRenderers = deathWalking.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var rigBoneCount = skinnedRenderers.Sum(renderer => renderer.bones != null ? renderer.bones.Length : 0);
            if (skinnedRenderers.Length == 0 || rigBoneCount == 0)
            {
                throw new InvalidOperationException("Death walking model has no preserved rigged mesh.");
            }

            var clip = EnsureDeathMeltPuddleClip(deathWalking.gameObject);
            var controller = EnsureSingleClipController(
                DeathMeltPuddleControllerPath,
                "LongaArma_Death_MeltPuddle",
                clip);
            var walkingAvatar = AssetDatabase.LoadAllAssetsAtPath(WalkingFbxPath).OfType<Avatar>().FirstOrDefault();

            var animator = deathWalking.GetComponent<Animator>();
            if (animator == null)
            {
                animator = deathWalking.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = walkingAvatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);

            if (deathRoot.localPosition != beforeDeathPosition ||
                deathRoot.localRotation != beforeDeathRotation ||
                deathRoot.localScale != beforeDeathScale ||
                deathWalking.localPosition != beforeWalkingPosition ||
                deathWalking.localRotation != beforeWalkingRotation ||
                deathWalking.localScale != beforeWalkingScale)
            {
                throw new InvalidOperationException("Death root or walking model transform changed unexpectedly.");
            }

            var idleAnimatorCount = CountWalkingAnimatorsForState(placementRoot.transform, IdleRootName);
            var moveAnimatorCount = CountWalkingAnimatorsForState(placementRoot.transform, MoveRootName);
            var attackAnimatorCount = CountWalkingAnimatorsForState(placementRoot.transform, AttackRootName);
            var hitAnimatorCount = CountWalkingAnimatorsForState(placementRoot.transform, HitRootName);
            var consumeAnimatorCount = CountWalkingAnimatorsForState(placementRoot.transform, ConsumeRootName);
            if (idleAnimatorCount == 0)
            {
                throw new InvalidOperationException("Idle state Animator was not preserved.");
            }

            if (moveAnimatorCount == 0)
            {
                throw new InvalidOperationException("Move state walking Animator was not preserved.");
            }

            if (attackAnimatorCount == 0)
            {
                throw new InvalidOperationException("Attack state Animator was not preserved.");
            }

            if (hitAnimatorCount == 0)
            {
                throw new InvalidOperationException("Hit state Animator was not preserved.");
            }

            if (consumeAnimatorCount == 0)
            {
                throw new InvalidOperationException("Consume state Animator was not preserved.");
            }

            var otherNonTargetAnimatorCount = ApprovedStateRootNames
                .Where(name => name != IdleRootName &&
                    name != MoveRootName &&
                    name != AttackRootName &&
                    name != HitRootName &&
                    name != ConsumeRootName &&
                    name != DeathRootName)
                .Sum(name => CountWalkingAnimatorsForState(placementRoot.transform, name));
            if (otherNonTargetAnimatorCount != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Animator exists on non-idle, non-move, non-attack, non-hit, non-consume, non-death Longa Arma states: " +
                    otherNonTargetAnimatorCount);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp scene could not be saved after applying death melt puddle.");
            }

            AssetDatabase.SaveAssets();
            var deathPuddleFbxRoot = FindChildRecursive(deathWalking, "LongaArmaDeathMelt_FinalPuddle");
            Debug.Log(
                "Applied Longa Arma death melt-puddle to death state only. " +
                "Clip=" + DeathMeltPuddleClipPath +
                ", Controller=" + DeathMeltPuddleControllerPath +
                ", Target=" + PlacementRootName + "/" + DeathRootName + "/" + WalkingInstanceName +
                ", DeathAnimators=" + deathWalking.GetComponentsInChildren<Animator>(true).Length +
                ", DeathBlendShapeCurves=" + CountCurveBindings(clip, "blendShape.") +
                ", DeathRendererEnabledCurves=" + CountCurveBindings(clip, "m_Enabled") +
                ", DeathLoopTime=" + IsLoopingClip(clip) +
                ", DeathPuddleFbxRenderers=" +
                    (deathPuddleFbxRoot != null ? deathPuddleFbxRoot.GetComponentsInChildren<Renderer>(true).Length : 0) +
                ", IdleAnimatorsPreserved=" + idleAnimatorCount +
                ", MoveStateAnimatorsPreserved=" + moveAnimatorCount +
                ", AttackAnimatorsPreserved=" + attackAnimatorCount +
                ", HitAnimatorsPreserved=" + hitAnimatorCount +
                ", ConsumeAnimatorsPreserved=" + consumeAnimatorCount +
                ", OtherNonTargetAnimators=" + otherNonTargetAnimatorCount +
                ", RigSkinnedRenderersPreserved=" + skinnedRenderers.Length +
                ", RigBonesPreserved=" + rigBoneCount);
        }

        private static AnimationClip EnsureAttackSlamDragClip(GameObject walkingInstance)
        {
            var root = walkingInstance.transform;
            var hips = RequireChild(root, "Hips");
            var chest = RequireChild(root, "chest");
            var head = RequireChild(root, "head");
            var frontLeg = RequireChild(root, "frontleg");
            var frontLeg0 = RequireChild(root, "frontleg0");
            var frontLeg1 = RequireChild(root, "frontleg1");
            var frontLeg2 = RequireChild(root, "frontleg2");
            var bladeLeg = RequireChild(root, "R_frontleg");
            var bladeLeg0 = RequireChild(root, "R_frontleg0");
            var bladeLeg1 = RequireChild(root, "R_frontleg1");
            var bladeLeg2 = RequireChild(root, "R_frontleg2");

            Directory.CreateDirectory(Path.GetDirectoryName(AttackSlamDragClipPath) ?? string.Empty);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AttackSlamDragClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = "LongaArma_Attack_SlamDrag",
                    frameRate = 30f
                };
                AssetDatabase.CreateAsset(clip, AttackSlamDragClipPath);
            }

            clip.ClearCurves();
            clip.name = "LongaArma_Attack_SlamDrag";
            clip.frameRate = 30f;
            clip.wrapMode = WrapMode.Loop;

            var times = new[] { 0.00f, 0.35f, 0.75f, 1.05f, 1.42f, 1.78f, 2.20f };

            AddLocalRotationOffsetCurves(
                clip,
                root,
                hips,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(-18f, 0f, 0f),
                    new Vector3(-34f, 0f, 0f),
                    new Vector3(22f, 0f, 0f),
                    new Vector3(12f, 0f, 0f),
                    new Vector3(-8f, 0f, 0f),
                    Vector3.zero
                });
            AddLocalRotationOffsetCurves(
                clip,
                root,
                chest,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(-40f, 0f, 0f),
                    new Vector3(-86f, 0f, 0f),
                    new Vector3(54f, 0f, 0f),
                    new Vector3(36f, 0f, 0f),
                    new Vector3(-18f, 0f, 0f),
                    Vector3.zero
                });
            AddLocalRotationOffsetCurves(
                clip,
                root,
                head,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(6f, -2f, 0f),
                    new Vector3(16f, -2f, 0f),
                    new Vector3(26f, 4f, 0f),
                    new Vector3(14f, 5f, 0f),
                    new Vector3(6f, -2f, 0f),
                    Vector3.zero
                });

            AddFrontSupportAttackCurves(clip, root, frontLeg, frontLeg0, frontLeg1, frontLeg2, times);
            AddBladeArmAttackCurves(clip, root, bladeLeg, bladeLeg0, bladeLeg1, bladeLeg2, times);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimationClip EnsureHitRecoilClip(GameObject walkingInstance)
        {
            var root = walkingInstance.transform;
            var hips = RequireChild(root, "Hips");
            var chest = RequireChild(root, "chest");
            var head = RequireChild(root, "head");

            Directory.CreateDirectory(Path.GetDirectoryName(HitRecoilClipPath) ?? string.Empty);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(HitRecoilClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = "LongaArma_Hit_Recoil",
                    frameRate = 30f
                };
                AssetDatabase.CreateAsset(clip, HitRecoilClipPath);
            }

            clip.ClearCurves();
            clip.name = "LongaArma_Hit_Recoil";
            clip.frameRate = 30f;
            clip.wrapMode = WrapMode.Loop;

            var times = new[] { 0.00f, 0.12f, 0.24f, 0.42f, 0.63f, 0.90f, 1.08f };

            AddLocalRotationOffsetCurves(
                clip,
                root,
                hips,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(12f, 0f, -3f),
                    new Vector3(-4f, 0f, 2f),
                    new Vector3(7f, 0f, -2f),
                    new Vector3(2f, 0f, 0f),
                    new Vector3(-1f, 0f, 0f),
                    Vector3.zero
                });
            AddLocalRotationOffsetCurves(
                clip,
                root,
                chest,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(18f, 0f, -6f),
                    new Vector3(-6f, 0f, 4f),
                    new Vector3(10f, 0f, -3f),
                    new Vector3(3f, 0f, 1f),
                    new Vector3(-2f, 0f, 0f),
                    Vector3.zero
                });
            AddLocalRotationOffsetCurves(
                clip,
                root,
                head,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(-8f, -24f, 10f),
                    new Vector3(4f, 30f, -12f),
                    new Vector3(2f, -20f, 7f),
                    new Vector3(-2f, 10f, -4f),
                    new Vector3(1f, -4f, 2f),
                    Vector3.zero
                });
            AddLocalScaleCurves(
                clip,
                root,
                hips,
                times,
                new[]
                {
                    Vector3.one,
                    new Vector3(1.06f, 0.94f, 1.04f),
                    new Vector3(0.99f, 1.03f, 0.99f),
                    new Vector3(1.03f, 0.98f, 1.02f),
                    Vector3.one,
                    new Vector3(1.01f, 0.995f, 1.00f),
                    Vector3.one
                });
            AddLocalScaleCurves(
                clip,
                root,
                chest,
                times,
                new[]
                {
                    Vector3.one,
                    new Vector3(1.10f, 0.90f, 1.06f),
                    new Vector3(0.98f, 1.05f, 0.98f),
                    new Vector3(1.05f, 0.97f, 1.03f),
                    Vector3.one,
                    new Vector3(1.01f, 0.995f, 1.00f),
                    Vector3.one
                });

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimationClip EnsureConsumePeckClip(GameObject walkingInstance)
        {
            var root = walkingInstance.transform;
            var hips = RequireChild(root, "Hips");
            var chest = RequireChild(root, "chest");
            var head = RequireChild(root, "head");

            Directory.CreateDirectory(Path.GetDirectoryName(ConsumePeckClipPath) ?? string.Empty);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ConsumePeckClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = "LongaArma_Consume_Peck",
                    frameRate = 30f
                };
                AssetDatabase.CreateAsset(clip, ConsumePeckClipPath);
            }

            clip.ClearCurves();
            clip.name = "LongaArma_Consume_Peck";
            clip.frameRate = 30f;
            clip.wrapMode = WrapMode.Loop;

            var times = new[] { 0.00f, 0.25f, 0.45f, 0.58f, 0.78f, 0.95f, 1.20f };

            AddLocalRotationOffsetCurves(
                clip,
                root,
                hips,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(-4f, 0f, 0f),
                    new Vector3(8f, 0f, 0f),
                    new Vector3(2f, 0f, 0f),
                    new Vector3(6f, 0f, 0f),
                    new Vector3(1f, 0f, 0f),
                    Vector3.zero
                });
            AddLocalRotationOffsetCurves(
                clip,
                root,
                chest,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(-16f, 0f, 0f),
                    new Vector3(34f, 0f, -4f),
                    new Vector3(-6f, 0f, 2f),
                    new Vector3(28f, 0f, 4f),
                    new Vector3(8f, 0f, -2f),
                    Vector3.zero
                });
            AddLocalRotationOffsetCurves(
                clip,
                root,
                head,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(-32f, 2f, 0f),
                    new Vector3(58f, -4f, -5f),
                    new Vector3(-10f, 3f, 3f),
                    new Vector3(48f, 5f, 5f),
                    new Vector3(12f, -6f, -3f),
                    Vector3.zero
                });
            AddLocalScaleCurves(
                clip,
                root,
                hips,
                times,
                new[]
                {
                    Vector3.one,
                    new Vector3(1.01f, 0.995f, 1.00f),
                    new Vector3(1.04f, 0.97f, 1.03f),
                    new Vector3(0.995f, 1.01f, 0.995f),
                    new Vector3(1.03f, 0.98f, 1.02f),
                    new Vector3(1.01f, 0.995f, 1.00f),
                    Vector3.one
                });
            AddLocalScaleCurves(
                clip,
                root,
                chest,
                times,
                new[]
                {
                    Vector3.one,
                    new Vector3(1.02f, 0.99f, 1.01f),
                    new Vector3(1.09f, 0.91f, 1.06f),
                    new Vector3(0.98f, 1.03f, 0.99f),
                    new Vector3(1.06f, 0.94f, 1.04f),
                    new Vector3(1.02f, 0.99f, 1.01f),
                    Vector3.one
                });

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimationClip EnsureDeathMeltPuddleClip(GameObject walkingInstance)
        {
            var root = walkingInstance.transform;
            var hips = RequireChild(root, "Hips");
            var chest = RequireChild(root, "chest");
            var head = RequireChild(root, "head");
            var frontLeg = RequireChild(root, "frontleg");
            var frontLeg0 = RequireChild(root, "frontleg0");
            var frontLeg1 = RequireChild(root, "frontleg1");
            var frontLeg2 = RequireChild(root, "frontleg2");
            var bladeArm = RequireChild(root, "R_frontleg");
            var bladeArm0 = RequireChild(root, "R_frontleg0");
            var bladeArm1 = RequireChild(root, "R_frontleg1");
            var bladeArm2 = RequireChild(root, "R_frontleg2");
            var backLeg = RequireChild(root, "backleg");
            var backLeg0 = RequireChild(root, "backleg0");
            var backLeg1 = RequireChild(root, "backleg1");
            var backLeg2 = RequireChild(root, "backleg2");
            var oppositeBackLeg = RequireChild(root, "R_backleg");
            var oppositeBackLeg0 = RequireChild(root, "R_backleg0");
            var oppositeBackLeg1 = RequireChild(root, "R_backleg1");
            var oppositeBackLeg2 = RequireChild(root, "R_backleg2");
            var bodyPuddleVisual = EnsureDeathBodyPuddleVisual(walkingInstance);

            Directory.CreateDirectory(Path.GetDirectoryName(DeathMeltPuddleClipPath) ?? string.Empty);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DeathMeltPuddleClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = "LongaArma_Death_MeltPuddle",
                    frameRate = 30f
                };
                AssetDatabase.CreateAsset(clip, DeathMeltPuddleClipPath);
            }

            clip.ClearCurves();
            clip.name = "LongaArma_Death_MeltPuddle";
            clip.frameRate = 30f;
            clip.wrapMode = WrapMode.Loop;

            var times = new[] { 0.00f, 0.25f, 0.55f, 0.85f, 1.20f, 1.65f };

            AddLocalRotationOffsetCurves(
                clip,
                root,
                hips,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(5f, 0f, -3f),
                    new Vector3(14f, 0f, 5f),
                    new Vector3(22f, 0f, 2f),
                    new Vector3(30f, 0f, -3f),
                    new Vector3(34f, 0f, -4f)
                });
            AddLocalRotationOffsetCurves(
                clip,
                root,
                chest,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(10f, 0f, -4f),
                    new Vector3(26f, 0f, 6f),
                    new Vector3(42f, 0f, 2f),
                    new Vector3(58f, 0f, 0f),
                    new Vector3(62f, 0f, 0f)
                });
            AddLocalRotationOffsetCurves(
                clip,
                root,
                head,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(8f, -8f, 5f),
                    new Vector3(28f, 6f, -6f),
                    new Vector3(52f, 0f, 0f),
                    new Vector3(70f, 0f, 0f),
                    new Vector3(78f, 0f, 0f)
                });

            AddDeathLegCollapseCurves(clip, root, frontLeg, frontLeg0, frontLeg1, frontLeg2, times, 1f);
            AddDeathBladeArmCollapseCurves(clip, root, bladeArm, bladeArm0, bladeArm1, bladeArm2, times);
            AddDeathLegCollapseCurves(clip, root, backLeg, backLeg0, backLeg1, backLeg2, times, -1f);
            AddDeathLegCollapseCurves(
                clip,
                root,
                oppositeBackLeg,
                oppositeBackLeg0,
                oppositeBackLeg1,
                oppositeBackLeg2,
                times,
                1f);
            AddDeathRigMeltCollapseCurves(clip, root, walkingInstance, hips, chest, head, times);
            AddDeathBodyRendererMeltCurves(clip, root, walkingInstance, times);
            AddDeathBodyPuddleCurves(clip, root, bodyPuddleVisual, times);
            AddDeathBlendShapeCurves(clip, root, walkingInstance, times);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimationClip EnsureIdleBodyMorphClip(GameObject walkingInstance)
        {
            var hips = FindChildRecursive(walkingInstance.transform, "Hips");
            var chest = FindChildRecursive(walkingInstance.transform, "chest");
            if (hips == null || chest == null)
            {
                throw new InvalidOperationException("Idle body morph target bones were not found: Hips/chest.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(IdleBodyMorphClipPath) ?? string.Empty);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleBodyMorphClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = "LongaArma_Idle_BodyMorph",
                    frameRate = 30f
                };
                AssetDatabase.CreateAsset(clip, IdleBodyMorphClipPath);
            }

            clip.ClearCurves();
            clip.name = "LongaArma_Idle_BodyMorph";
            clip.frameRate = 30f;
            clip.wrapMode = WrapMode.Loop;

            AddScalePulseCurves(
                clip,
                walkingInstance.transform,
                hips,
                new Vector3(1.025f, 0.985f, 1.02f),
                new Vector3(0.992f, 1.015f, 0.995f));
            AddScalePulseCurves(
                clip,
                walkingInstance.transform,
                chest,
                new Vector3(1.07f, 0.955f, 1.045f),
                new Vector3(0.975f, 1.045f, 0.985f));

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimatorController EnsureIdleBodyMorphController(AnimationClip clip)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(IdleBodyMorphControllerPath) ?? string.Empty);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(IdleBodyMorphControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(IdleBodyMorphControllerPath);
            }

            if (controller.layers == null || controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(candidate => candidate.name == "LongaArma_Idle_BodyMorph");

            if (state == null)
            {
                state = stateMachine.AddState("LongaArma_Idle_BodyMorph");
            }

            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimatorController EnsureSingleClipController(
            string controllerPath,
            string stateName,
            AnimationClip clip)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(controllerPath) ?? string.Empty);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

            if (controller.layers == null || controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(candidate => candidate.name == stateName);

            if (state == null)
            {
                state = stateMachine.AddState(stateName);
            }

            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void AddFrontSupportAttackCurves(
            AnimationClip clip,
            Transform root,
            Transform upper,
            Transform mid,
            Transform lower,
            Transform tip,
            float[] times)
        {
            AddLocalRotationOffsetCurves(
                clip,
                root,
                upper,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(-22f, 0f, 6f),
                    new Vector3(-78f, 0f, 12f),
                    new Vector3(58f, 0f, -8f),
                    new Vector3(48f, 0f, -8f),
                    new Vector3(-10f, 0f, 3f),
                    Vector3.zero
                });
            AddLocalRotationOffsetCurves(
                clip,
                root,
                mid,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(34f, 0f, -4f),
                    new Vector3(96f, 0f, -8f),
                    new Vector3(-78f, 0f, 10f),
                    new Vector3(-58f, 0f, 10f),
                    new Vector3(14f, 0f, -3f),
                    Vector3.zero
                });
            AddLocalRotationOffsetCurves(
                clip,
                root,
                lower,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(-24f, 0f, 0f),
                    new Vector3(-62f, 0f, 0f),
                    new Vector3(76f, 0f, 0f),
                    new Vector3(62f, 0f, -4f),
                    new Vector3(-10f, 0f, 0f),
                    Vector3.zero
                });
            AddLocalRotationOffsetCurves(
                clip,
                root,
                tip,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(-16f, 0f, 0f),
                    new Vector3(-40f, 0f, 0f),
                    new Vector3(58f, 0f, 0f),
                    new Vector3(48f, 0f, -3f),
                    new Vector3(-8f, 0f, 0f),
                    Vector3.zero
                });
        }

        private static void AddBladeArmAttackCurves(
            AnimationClip clip,
            Transform root,
            Transform upper,
            Transform mid,
            Transform lower,
            Transform tip,
            float[] times)
        {
            AddLocalRotationOffsetCurves(
                clip,
                root,
                upper,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(-26f, -8f, -6f),
                    new Vector3(-88f, -14f, -10f),
                    new Vector3(66f, 8f, 8f),
                    new Vector3(54f, 10f, 10f),
                    new Vector3(-12f, -4f, -3f),
                    Vector3.zero
                });
            AddLocalRotationOffsetCurves(
                clip,
                root,
                mid,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(38f, -8f, 3f),
                    new Vector3(112f, -12f, 6f),
                    new Vector3(-78f, 6f, -14f),
                    new Vector3(-58f, 8f, -16f),
                    new Vector3(16f, -3f, 3f),
                    Vector3.zero
                });
            AddLocalRotationOffsetCurves(
                clip,
                root,
                lower,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(-26f, 0f, 0f),
                    new Vector3(-76f, -8f, 0f),
                    new Vector3(118f, 4f, -18f),
                    new Vector3(104f, 4f, -20f),
                    new Vector3(-12f, -3f, 0f),
                    Vector3.zero
                });
            AddLocalRotationOffsetCurves(
                clip,
                root,
                tip,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(-20f, 0f, 0f),
                    new Vector3(-54f, -6f, 0f),
                    new Vector3(128f, 2f, -24f),
                    new Vector3(112f, 2f, -26f),
                    new Vector3(-10f, -3f, 0f),
                    Vector3.zero
                });
        }

        private static void AddDeathLegCollapseCurves(
            AnimationClip clip,
            Transform root,
            Transform upper,
            Transform mid,
            Transform lower,
            Transform tip,
            float[] times,
            float sideSign)
        {
            AddLocalRotationOffsetCurves(
                clip,
                root,
                upper,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(8f, 0f, 10f * sideSign),
                    new Vector3(22f, 0f, 24f * sideSign),
                    new Vector3(42f, 0f, 38f * sideSign),
                    new Vector3(66f, 0f, 52f * sideSign),
                    new Vector3(76f, 0f, 58f * sideSign)
                });
            AddLocalRotationOffsetCurves(
                clip,
                root,
                mid,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(-12f, 0f, -6f * sideSign),
                    new Vector3(-28f, 0f, -18f * sideSign),
                    new Vector3(-48f, 0f, -28f * sideSign),
                    new Vector3(-68f, 0f, -38f * sideSign),
                    new Vector3(-76f, 0f, -42f * sideSign)
                });
            AddLocalRotationOffsetCurves(
                clip,
                root,
                lower,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(10f, 0f, 4f * sideSign),
                    new Vector3(26f, 0f, 12f * sideSign),
                    new Vector3(44f, 0f, 18f * sideSign),
                    new Vector3(58f, 0f, 26f * sideSign),
                    new Vector3(64f, 0f, 30f * sideSign)
                });
            AddLocalRotationOffsetCurves(
                clip,
                root,
                tip,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(8f, 0f, 5f * sideSign),
                    new Vector3(18f, 0f, 12f * sideSign),
                    new Vector3(28f, 0f, 20f * sideSign),
                    new Vector3(36f, 0f, 27f * sideSign),
                    new Vector3(40f, 0f, 30f * sideSign)
                });
        }

        private static void AddDeathBladeArmCollapseCurves(
            AnimationClip clip,
            Transform root,
            Transform upper,
            Transform mid,
            Transform lower,
            Transform tip,
            float[] times)
        {
            AddLocalRotationOffsetCurves(
                clip,
                root,
                upper,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(14f, -6f, -8f),
                    new Vector3(34f, -14f, -20f),
                    new Vector3(58f, -20f, -32f),
                    new Vector3(78f, -26f, -42f),
                    new Vector3(86f, -30f, -48f)
                });
            AddLocalRotationOffsetCurves(
                clip,
                root,
                mid,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(-16f, 4f, 6f),
                    new Vector3(-38f, 10f, 14f),
                    new Vector3(-62f, 16f, 22f),
                    new Vector3(-82f, 20f, 30f),
                    new Vector3(-90f, 22f, 34f)
                });
            AddLocalRotationOffsetCurves(
                clip,
                root,
                lower,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(18f, -4f, -6f),
                    new Vector3(42f, -8f, -16f),
                    new Vector3(72f, -12f, -26f),
                    new Vector3(96f, -16f, -34f),
                    new Vector3(104f, -18f, -38f)
                });
            AddLocalRotationOffsetCurves(
                clip,
                root,
                tip,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(20f, 0f, -8f),
                    new Vector3(48f, 0f, -18f),
                    new Vector3(82f, 0f, -32f),
                    new Vector3(108f, 0f, -44f),
                    new Vector3(118f, 0f, -48f)
                });
        }

        private static Transform EnsureDeathBodyPuddleVisual(GameObject walkingInstance)
        {
            var root = walkingInstance.transform;
            RemoveDeathMeltVisuals(root);

            var puddlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DeathPuddleFbxPath);
            if (puddlePrefab == null)
            {
                throw new FileNotFoundException("Death puddle FBX prefab was not found.", DeathPuddleFbxPath);
            }

            var rendererBounds = TryGetSkinnedRendererBounds(walkingInstance, out var bounds)
                ? bounds
                : new Bounds(root.position, Vector3.one);
            var groundCenter = new Vector3(
                rendererBounds.center.x,
                rendererBounds.min.y + 0.018f,
                rendererBounds.center.z);

            var puddle = (GameObject)PrefabUtility.InstantiatePrefab(puddlePrefab, walkingInstance.scene);
            puddle.name = "LongaArmaDeathMelt_FinalPuddle";
            puddle.transform.SetParent(root, false);
            puddle.transform.localRotation = Quaternion.Inverse(root.rotation);
            puddle.transform.localScale = Vector3.one;
            puddle.SetActive(true);

            var renderers = puddle.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Death puddle FBX has no renderers: " + DeathPuddleFbxPath);
            }

            EnsureDeathPuddleRendererMaterials(renderers);
            AlignDeathPuddleFbxToGround(root, puddle, rendererBounds, groundCenter);

            foreach (var renderer in renderers)
            {
                renderer.enabled = false;
                EditorUtility.SetDirty(renderer);
            }

            EditorUtility.SetDirty(puddle);
            return puddle.transform;
        }

        private static void AlignDeathPuddleFbxToGround(
            Transform root,
            GameObject puddle,
            Bounds referenceBounds,
            Vector3 targetGroundCenter)
        {
            var worldRotationCandidates = new[]
            {
                Quaternion.identity,
                Quaternion.Euler(90f, 0f, 0f),
                Quaternion.Euler(-90f, 0f, 0f),
                Quaternion.Euler(0f, 0f, 90f),
                Quaternion.Euler(0f, 0f, -90f),
                Quaternion.Euler(180f, 0f, 0f),
                Quaternion.Euler(0f, 180f, 0f),
                Quaternion.Euler(0f, 0f, 180f)
            };
            var bestLocalRotation = Quaternion.Inverse(root.rotation);
            var bestScore = float.PositiveInfinity;
            foreach (var worldRotation in worldRotationCandidates)
            {
                puddle.transform.localRotation = Quaternion.Inverse(root.rotation) * worldRotation;
                puddle.transform.localScale = Vector3.one;
                puddle.transform.localPosition = Vector3.zero;
                if (!TryGetRendererBounds(puddle, out var candidateBounds, includeInactive: true))
                {
                    continue;
                }

                var footprint = Mathf.Max(candidateBounds.size.x, candidateBounds.size.z, 0.0001f);
                var thinness = candidateBounds.size.y / footprint;
                var score = candidateBounds.size.y + (thinness * 0.25f);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestLocalRotation = puddle.transform.localRotation;
                }
            }

            puddle.transform.localRotation = bestLocalRotation;
            puddle.transform.localScale = Vector3.one;
            puddle.transform.localPosition = Vector3.zero;

            if (TryGetRendererBounds(puddle, out var puddleBounds, includeInactive: true))
            {
                var referenceFootprint = Mathf.Max(referenceBounds.size.x, referenceBounds.size.z, 0.0001f);
                var puddleFootprint = Mathf.Max(puddleBounds.size.x, puddleBounds.size.z, 0.0001f);
                var scaleFactor = Mathf.Clamp(referenceFootprint / puddleFootprint, 0.01f, 100f);
                puddle.transform.localScale *= scaleFactor;

                if (TryGetRendererBounds(puddle, out var scaledPuddleBounds, includeInactive: true))
                {
                    var currentGroundCenter = new Vector3(
                        scaledPuddleBounds.center.x,
                        scaledPuddleBounds.min.y,
                        scaledPuddleBounds.center.z);
                    puddle.transform.localPosition += root.InverseTransformVector(targetGroundCenter - currentGroundCenter);
                }
            }
            else
            {
                puddle.transform.localPosition = root.InverseTransformPoint(targetGroundCenter);
            }
        }

        private static void EnsureDeathPuddleRendererMaterials(Renderer[] renderers)
        {
            var fallbackMaterial = LoadMaterial(SlimeMaterialPath);
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    renderer.sharedMaterial = fallbackMaterial;
                    EditorUtility.SetDirty(renderer);
                    continue;
                }

                var changed = false;
                for (var index = 0; index < materials.Length; index++)
                {
                    if (materials[index] == null)
                    {
                        materials[index] = fallbackMaterial;
                        changed = true;
                    }
                }

                if (changed)
                {
                    renderer.sharedMaterials = materials;
                    EditorUtility.SetDirty(renderer);
                }
            }
        }

        private static void RemoveDeathMeltVisuals(Transform walkingRoot)
        {
            var oldVisuals = walkingRoot.GetComponentsInChildren<Transform>(true)
                .Where(child =>
                    child != walkingRoot &&
                    (child.name == "LongaArmaDeathPuddle_Visual" ||
                        child.name.StartsWith("LongaArmaDeathMelt_", StringComparison.Ordinal)))
                .ToArray();
            foreach (var oldVisual in oldVisuals)
            {
                UnityEngine.Object.DestroyImmediate(oldVisual.gameObject);
            }
        }

        private static Mesh CreateDeathBodyMeltPuddleMesh(Vector3 referenceSize)
        {
            const int segmentCount = 40;
            var radiusX = Mathf.Max(referenceSize.x * 0.38f, 0.24f);
            var radiusZ = Mathf.Max(referenceSize.z * 0.30f, 0.18f);
            var vertices = new Vector3[segmentCount + 1];
            var triangles = new int[segmentCount * 3];
            vertices[0] = Vector3.zero;

            for (var index = 0; index < segmentCount; index++)
            {
                var angle = (Mathf.PI * 2f * index) / segmentCount;
                var frontSmear = Mathf.Max(0f, Mathf.Cos(angle - 0.20f)) * 0.20f;
                var shoulderLobe = Mathf.Max(0f, Mathf.Sin(angle + 1.55f)) * 0.12f;
                var wobble =
                    1f +
                    frontSmear +
                    shoulderLobe +
                    (Mathf.Sin(angle * 3.0f) * 0.09f) +
                    (Mathf.Cos(angle * 5.0f) * 0.06f);
                vertices[index + 1] = new Vector3(
                    Mathf.Cos(angle) * radiusX * wobble,
                    0f,
                    Mathf.Sin(angle) * radiusZ * wobble);
            }

            for (var index = 0; index < segmentCount; index++)
            {
                var triangleIndex = index * 3;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = index + 1;
                triangles[triangleIndex + 2] = index == segmentCount - 1 ? 1 : index + 2;
            }

            var mesh = new Mesh
            {
                name = "LongaArmaDeathBodyMeltPuddleMesh",
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static DeathSlimeProxyVisuals EnsureDeathSlimeProxyVisuals(GameObject walkingInstance)
        {
            RemoveLegacyDeathPuddleVisuals(walkingInstance.transform);

            var rendererBounds = TryGetSkinnedRendererBounds(walkingInstance, out var bounds)
                ? bounds
                : new Bounds(walkingInstance.transform.position, Vector3.one);
            var root = walkingInstance.transform;
            var center = rendererBounds.center;
            var groundY = rendererBounds.min.y + 0.018f;
            var height = Mathf.Max(rendererBounds.size.y, 0.5f);
            var material = LoadMaterial(SlimeMaterialPath);
            var childWorldRotation = Quaternion.Inverse(root.rotation);

            var bodyMass = EnsureDeathSlimeProxyVisual(
                root,
                "LongaArmaDeathMelt_BodyMass",
                new Vector3(center.x, groundY + (height * 0.30f), center.z),
                childWorldRotation,
                CreateDeathSlimeDomeMesh(
                    "LongaArmaDeathMelt_BodyMassMesh",
                    rendererBounds.size,
                    radiusXMultiplier: 0.33f,
                    radiusZMultiplier: 0.29f,
                    heightMultiplier: 0.18f,
                    wobblePhase: 0.35f),
                material);
            var chestFlow = EnsureDeathSlimeProxyVisual(
                root,
                "LongaArmaDeathMelt_ChestFlow",
                center + (root.forward * rendererBounds.size.z * 0.10f) + (Vector3.up * height * 0.24f),
                childWorldRotation,
                CreateDeathSlimeDomeMesh(
                    "LongaArmaDeathMelt_ChestFlowMesh",
                    rendererBounds.size,
                    radiusXMultiplier: 0.18f,
                    radiusZMultiplier: 0.38f,
                    heightMultiplier: 0.11f,
                    wobblePhase: 1.15f),
                material);
            var headFlow = EnsureDeathSlimeProxyVisual(
                root,
                "LongaArmaDeathMelt_HeadFlow",
                center + (root.forward * rendererBounds.size.z * 0.34f) + (Vector3.up * height * 0.38f),
                childWorldRotation,
                CreateDeathSlimeDomeMesh(
                    "LongaArmaDeathMelt_HeadFlowMesh",
                    rendererBounds.size,
                    radiusXMultiplier: 0.14f,
                    radiusZMultiplier: 0.26f,
                    heightMultiplier: 0.09f,
                    wobblePhase: 2.10f),
                material);
            var bladeFlow = EnsureDeathSlimeProxyVisual(
                root,
                "LongaArmaDeathMelt_BladeFlow",
                center + (root.right * rendererBounds.size.x * 0.28f) + (root.forward * rendererBounds.size.z * 0.08f) +
                    (Vector3.up * height * 0.28f),
                childWorldRotation,
                CreateDeathSlimeDomeMesh(
                    "LongaArmaDeathMelt_BladeFlowMesh",
                    rendererBounds.size,
                    radiusXMultiplier: 0.12f,
                    radiusZMultiplier: 0.43f,
                    heightMultiplier: 0.08f,
                    wobblePhase: 2.85f),
                material);
            var finalPuddle = EnsureDeathSlimeProxyVisual(
                root,
                "LongaArmaDeathMelt_FinalPuddle",
                new Vector3(center.x, groundY, center.z),
                childWorldRotation,
                CreateDeathIrregularPuddleMesh("LongaArmaDeathMelt_FinalPuddleMesh", rendererBounds.size),
                material);

            return new DeathSlimeProxyVisuals(bodyMass, chestFlow, headFlow, bladeFlow, finalPuddle);
        }

        private static void RemoveLegacyDeathPuddleVisuals(Transform walkingRoot)
        {
            while (true)
            {
                var legacyPuddle = FindChildRecursive(walkingRoot, "LongaArmaDeathPuddle_Visual");
                if (legacyPuddle == null)
                {
                    return;
                }

                UnityEngine.Object.DestroyImmediate(legacyPuddle.gameObject);
            }
        }

        private static Transform EnsureDeathSlimeProxyVisual(
            Transform root,
            string name,
            Vector3 worldPosition,
            Quaternion localRotation,
            Mesh mesh,
            Material material)
        {
            var existing = FindChildRecursive(root, name);
            var proxy = existing != null ? existing.gameObject : new GameObject(name);
            proxy.name = name;
            proxy.transform.SetParent(root, false);
            proxy.transform.localPosition = root.InverseTransformPoint(worldPosition);
            proxy.transform.localRotation = localRotation;
            proxy.transform.localScale = Vector3.one;

            var meshFilter = proxy.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = proxy.AddComponent<MeshFilter>();
            }

            meshFilter.sharedMesh = mesh;

            var meshRenderer = proxy.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = proxy.AddComponent<MeshRenderer>();
            }

            meshRenderer.sharedMaterial = material;
            meshRenderer.enabled = false;
            proxy.SetActive(true);
            EditorUtility.SetDirty(meshFilter);
            EditorUtility.SetDirty(meshRenderer);
            EditorUtility.SetDirty(proxy);
            return proxy.transform;
        }

        private static Mesh CreateDeathSlimeDomeMesh(
            string meshName,
            Vector3 referenceSize,
            float radiusXMultiplier,
            float radiusZMultiplier,
            float heightMultiplier,
            float wobblePhase)
        {
            const int segmentCount = 28;
            var radiusX = Mathf.Max(referenceSize.x * radiusXMultiplier, 0.12f);
            var radiusZ = Mathf.Max(referenceSize.z * radiusZMultiplier, 0.12f);
            var height = Mathf.Max(referenceSize.y * heightMultiplier, 0.05f);
            var vertices = new Vector3[segmentCount + 2];
            var triangles = new int[segmentCount * 6];

            vertices[0] = new Vector3(0f, height, 0f);
            vertices[segmentCount + 1] = Vector3.zero;
            for (var index = 0; index < segmentCount; index++)
            {
                var angle = (Mathf.PI * 2f * index) / segmentCount;
                var wobble =
                    1f +
                    (Mathf.Sin((angle * 2.1f) + wobblePhase) * 0.14f) +
                    (Mathf.Cos((angle * 4.7f) - wobblePhase) * 0.08f);
                vertices[index + 1] = new Vector3(
                    Mathf.Cos(angle) * radiusX * wobble,
                    0f,
                    Mathf.Sin(angle) * radiusZ * wobble);
            }

            for (var index = 0; index < segmentCount; index++)
            {
                var next = index == segmentCount - 1 ? 1 : index + 2;
                var triangleIndex = index * 6;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = index + 1;
                triangles[triangleIndex + 2] = next;
                triangles[triangleIndex + 3] = segmentCount + 1;
                triangles[triangleIndex + 4] = next;
                triangles[triangleIndex + 5] = index + 1;
            }

            var mesh = new Mesh
            {
                name = meshName,
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateDeathIrregularPuddleMesh(string meshName, Vector3 referenceSize)
        {
            const int segmentCount = 40;
            var radiusX = Mathf.Max(referenceSize.x * 0.44f, 0.26f);
            var radiusZ = Mathf.Max(referenceSize.z * 0.34f, 0.20f);
            var vertices = new Vector3[segmentCount + 1];
            var triangles = new int[segmentCount * 3];
            vertices[0] = Vector3.zero;

            for (var index = 0; index < segmentCount; index++)
            {
                var angle = (Mathf.PI * 2f * index) / segmentCount;
                var frontLobe = Mathf.Max(0f, Mathf.Cos(angle - 0.35f)) * 0.22f;
                var sideLobe = Mathf.Max(0f, Mathf.Sin(angle + 1.10f)) * 0.16f;
                var wobble =
                    1f +
                    frontLobe +
                    sideLobe +
                    (Mathf.Sin(angle * 3.0f) * 0.10f) +
                    (Mathf.Cos(angle * 6.0f) * 0.07f);
                vertices[index + 1] = new Vector3(
                    Mathf.Cos(angle) * radiusX * wobble,
                    0f,
                    Mathf.Sin(angle) * radiusZ * wobble);
            }

            for (var index = 0; index < segmentCount; index++)
            {
                var triangleIndex = index * 3;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = index + 1;
                triangles[triangleIndex + 2] = index == segmentCount - 1 ? 1 : index + 2;
            }

            var mesh = new Mesh
            {
                name = meshName,
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddDeathRigMeltCollapseCurves(
            AnimationClip clip,
            Transform root,
            GameObject walkingInstance,
            Transform hips,
            Transform chest,
            Transform head,
            float[] times)
        {
            var rendererBounds = TryGetSkinnedRendererBounds(walkingInstance, out var bounds)
                ? bounds
                : new Bounds(root.position, Vector3.one);
            var sinkDistance = Mathf.Max(rendererBounds.size.y * 0.50f, 0.26f);

            AddLocalPositionOffsetCurves(
                clip,
                root,
                hips,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(0f, -sinkDistance * 0.10f, 0f),
                    new Vector3(0f, -sinkDistance * 0.26f, 0f),
                    new Vector3(0f, -sinkDistance * 0.48f, 0f),
                    new Vector3(0f, -sinkDistance * 0.68f, 0f),
                    new Vector3(0f, -sinkDistance * 0.76f, 0f)
                });
            AddLocalPositionOffsetCurves(
                clip,
                root,
                chest,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(0f, -sinkDistance * 0.08f, 0f),
                    new Vector3(0f, -sinkDistance * 0.24f, 0f),
                    new Vector3(0f, -sinkDistance * 0.46f, 0f),
                    new Vector3(0f, -sinkDistance * 0.66f, 0f),
                    new Vector3(0f, -sinkDistance * 0.74f, 0f)
                });
            AddLocalPositionOffsetCurves(
                clip,
                root,
                head,
                times,
                new[]
                {
                    Vector3.zero,
                    new Vector3(0f, -sinkDistance * 0.05f, 0f),
                    new Vector3(0f, -sinkDistance * 0.20f, 0f),
                    new Vector3(0f, -sinkDistance * 0.42f, 0f),
                    new Vector3(0f, -sinkDistance * 0.64f, 0f),
                    new Vector3(0f, -sinkDistance * 0.72f, 0f)
                });

            AddLocalScaleCurves(
                clip,
                root,
                hips,
                times,
                new[]
                {
                    Vector3.one,
                    new Vector3(1.08f, 0.72f, 1.08f),
                    new Vector3(1.22f, 0.36f, 1.18f),
                    new Vector3(1.38f, 0.16f, 1.30f),
                    new Vector3(1.48f, 0.05f, 1.42f),
                    new Vector3(0.12f, 0.02f, 0.12f)
                });
            AddLocalScaleCurves(
                clip,
                root,
                chest,
                times,
                new[]
                {
                    Vector3.one,
                    new Vector3(1.06f, 0.68f, 1.06f),
                    new Vector3(1.18f, 0.30f, 1.14f),
                    new Vector3(1.32f, 0.12f, 1.26f),
                    new Vector3(1.42f, 0.04f, 1.34f),
                    new Vector3(0.10f, 0.015f, 0.10f)
                });
            AddLocalScaleCurves(
                clip,
                root,
                head,
                times,
                new[]
                {
                    Vector3.one,
                    new Vector3(1.04f, 0.72f, 1.04f),
                    new Vector3(1.12f, 0.34f, 1.10f),
                    new Vector3(1.22f, 0.14f, 1.18f),
                    new Vector3(1.28f, 0.045f, 1.24f),
                    new Vector3(0.08f, 0.012f, 0.08f)
                });
        }

        private static void AddDeathBodyRendererMeltCurves(
            AnimationClip clip,
            Transform root,
            GameObject walkingInstance,
            float[] times)
        {
            var rendererBounds = TryGetSkinnedRendererBounds(walkingInstance, out var bounds)
                ? bounds
                : new Bounds(root.position, Vector3.one);
            var sinkDistance = Mathf.Max(rendererBounds.size.y * 0.72f, 0.36f);
            foreach (var renderer in walkingInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var target = renderer.transform;
                AddLocalPositionOffsetCurves(
                    clip,
                    root,
                    target,
                    times,
                    new[]
                    {
                        Vector3.zero,
                        new Vector3(0f, -sinkDistance * 0.16f, 0f),
                        new Vector3(0f, -sinkDistance * 0.42f, 0f),
                        new Vector3(0f, -sinkDistance * 0.68f, 0f),
                        new Vector3(0f, -sinkDistance * 0.90f, 0f),
                        new Vector3(0f, -sinkDistance * 0.96f, 0f)
                    });
                AddLocalScaleCurves(
                    clip,
                    root,
                    target,
                    times,
                    new[]
                    {
                        Vector3.one,
                        new Vector3(1.10f, 0.55f, 1.08f),
                        new Vector3(1.34f, 0.18f, 1.28f),
                        new Vector3(1.62f, 0.045f, 1.50f),
                        new Vector3(1.85f, 0.008f, 1.72f),
                        new Vector3(0.01f, 0.003f, 0.01f)
                    });
                AddRendererEnabledCurve(clip, root, target, times, new[] { 1f, 1f, 1f, 1f, 1f, 0f });
            }
        }

        private static void AddDeathBodyPuddleCurves(
            AnimationClip clip,
            Transform root,
            Transform puddleVisual,
            float[] times)
        {
            AddLocalScaleCurves(
                clip,
                root,
                puddleVisual,
                times,
                new[]
                {
                    new Vector3(0.01f, 0.01f, 0.01f),
                    new Vector3(0.01f, 0.01f, 0.01f),
                    new Vector3(0.08f, 0.08f, 0.08f),
                    new Vector3(0.35f, 0.35f, 0.35f),
                    new Vector3(0.78f, 0.78f, 0.78f),
                    Vector3.one
                });
            foreach (var renderer in puddleVisual.GetComponentsInChildren<Renderer>(true))
            {
                AddRendererEnabledCurve(clip, root, renderer.transform, times, new[] { 0f, 0f, 0f, 1f, 1f, 1f });
            }
        }

        private static void AddDeathSlimeProxyCurves(
            AnimationClip clip,
            Transform root,
            DeathSlimeProxyVisuals visuals,
            float[] times)
        {
            var rendererBounds = TryGetSkinnedRendererBounds(root.gameObject, out var bounds)
                ? bounds
                : new Bounds(root.position, Vector3.one);
            var center = rendererBounds.center;
            var groundY = rendererBounds.min.y + 0.018f;
            var height = Mathf.Max(rendererBounds.size.y, 0.5f);
            var width = Mathf.Max(rendererBounds.size.x, 0.5f);
            var depth = Mathf.Max(rendererBounds.size.z, 0.5f);
            var groundCenter = new Vector3(center.x, groundY, center.z);
            var front = root.forward;
            var right = root.right;

            AddDeathSlimeProxyMotion(
                clip,
                root,
                visuals.BodyMass,
                times,
                new[]
                {
                    center + (Vector3.up * height * 0.30f),
                    center + (Vector3.up * height * 0.26f),
                    center + (Vector3.up * height * 0.16f),
                    groundCenter + (Vector3.up * height * 0.08f),
                    groundCenter + (Vector3.up * height * 0.03f),
                    groundCenter + (Vector3.up * height * 0.01f)
                },
                new[]
                {
                    new Vector3(0.01f, 0.01f, 0.01f),
                    new Vector3(0.42f, 0.55f, 0.42f),
                    new Vector3(0.95f, 0.85f, 0.86f),
                    new Vector3(0.82f, 0.42f, 0.96f),
                    new Vector3(0.32f, 0.12f, 0.48f),
                    new Vector3(0.01f, 0.01f, 0.01f)
                },
                new[] { 0f, 1f, 1f, 1f, 0f, 0f });
            AddDeathSlimeProxyMotion(
                clip,
                root,
                visuals.ChestFlow,
                times,
                new[]
                {
                    center + (front * depth * 0.08f) + (Vector3.up * height * 0.24f),
                    center + (front * depth * 0.12f) + (Vector3.up * height * 0.20f),
                    center + (front * depth * 0.16f) + (Vector3.up * height * 0.13f),
                    groundCenter + (front * depth * 0.18f) + (right * width * -0.08f) + (Vector3.up * height * 0.035f),
                    groundCenter + (front * depth * 0.22f) + (right * width * -0.14f) + (Vector3.up * height * 0.015f),
                    groundCenter + (front * depth * 0.20f) + (right * width * -0.16f) + (Vector3.up * height * 0.010f)
                },
                new[]
                {
                    new Vector3(0.01f, 0.01f, 0.01f),
                    new Vector3(0.10f, 0.14f, 0.18f),
                    new Vector3(0.44f, 0.70f, 0.72f),
                    new Vector3(0.64f, 0.28f, 1.10f),
                    new Vector3(0.30f, 0.08f, 0.82f),
                    new Vector3(0.01f, 0.01f, 0.01f)
                },
                new[] { 0f, 0f, 1f, 1f, 1f, 0f });
            AddDeathSlimeProxyMotion(
                clip,
                root,
                visuals.HeadFlow,
                times,
                new[]
                {
                    center + (front * depth * 0.34f) + (Vector3.up * height * 0.38f),
                    center + (front * depth * 0.36f) + (Vector3.up * height * 0.31f),
                    center + (front * depth * 0.38f) + (Vector3.up * height * 0.19f),
                    groundCenter + (front * depth * 0.42f) + (Vector3.up * height * 0.040f),
                    groundCenter + (front * depth * 0.46f) + (Vector3.up * height * 0.015f),
                    groundCenter + (front * depth * 0.46f) + (Vector3.up * height * 0.010f)
                },
                new[]
                {
                    new Vector3(0.01f, 0.01f, 0.01f),
                    new Vector3(0.08f, 0.12f, 0.12f),
                    new Vector3(0.34f, 0.62f, 0.52f),
                    new Vector3(0.46f, 0.22f, 0.96f),
                    new Vector3(0.20f, 0.07f, 0.58f),
                    new Vector3(0.01f, 0.01f, 0.01f)
                },
                new[] { 0f, 0f, 1f, 1f, 1f, 0f });
            AddDeathSlimeProxyMotion(
                clip,
                root,
                visuals.BladeFlow,
                times,
                new[]
                {
                    center + (right * width * 0.28f) + (front * depth * 0.08f) + (Vector3.up * height * 0.28f),
                    center + (right * width * 0.30f) + (front * depth * 0.10f) + (Vector3.up * height * 0.22f),
                    center + (right * width * 0.31f) + (front * depth * 0.10f) + (Vector3.up * height * 0.13f),
                    groundCenter + (right * width * 0.35f) + (front * depth * 0.12f) + (Vector3.up * height * 0.035f),
                    groundCenter + (right * width * 0.40f) + (front * depth * 0.16f) + (Vector3.up * height * 0.015f),
                    groundCenter + (right * width * 0.40f) + (front * depth * 0.16f) + (Vector3.up * height * 0.010f)
                },
                new[]
                {
                    new Vector3(0.01f, 0.01f, 0.01f),
                    new Vector3(0.08f, 0.10f, 0.16f),
                    new Vector3(0.30f, 0.54f, 0.76f),
                    new Vector3(0.42f, 0.18f, 1.05f),
                    new Vector3(0.18f, 0.06f, 0.68f),
                    new Vector3(0.01f, 0.01f, 0.01f)
                },
                new[] { 0f, 0f, 1f, 1f, 1f, 0f });
            AddDeathSlimeProxyMotion(
                clip,
                root,
                visuals.FinalPuddle,
                times,
                Enumerable.Repeat(groundCenter, times.Length).ToArray(),
                new[]
                {
                    new Vector3(0.01f, 0.01f, 0.01f),
                    new Vector3(0.01f, 0.01f, 0.01f),
                    new Vector3(0.20f, 0.03f, 0.18f),
                    new Vector3(0.50f, 0.04f, 0.46f),
                    new Vector3(0.86f, 0.05f, 0.82f),
                    new Vector3(1.00f, 0.05f, 1.00f)
                },
                new[] { 0f, 0f, 1f, 1f, 1f, 1f });
        }

        private static void AddDeathSlimeProxyMotion(
            AnimationClip clip,
            Transform root,
            Transform proxy,
            float[] times,
            Vector3[] worldPositions,
            Vector3[] scaleFactors,
            float[] enabledValues)
        {
            if (times.Length != worldPositions.Length ||
                times.Length != scaleFactors.Length ||
                times.Length != enabledValues.Length)
            {
                throw new ArgumentException("Death slime proxy curve arrays must have the same length.");
            }

            AddLocalPositionAbsoluteCurves(
                clip,
                root,
                proxy,
                times,
                worldPositions.Select(position => root.InverseTransformPoint(position)).ToArray());
            AddLocalScaleCurves(
                clip,
                root,
                proxy,
                times,
                scaleFactors);
            AddRendererEnabledCurve(clip, root, proxy, times, enabledValues);
        }

        private static void AddRendererEnabledCurve(
            AnimationClip clip,
            Transform root,
            Transform target,
            float[] times,
            float[] values)
        {
            if (times.Length != values.Length)
            {
                throw new ArgumentException("Renderer enabled curve times and values must have the same length.");
            }

            var path = AnimationUtility.CalculateTransformPath(target, root);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Renderer), "m_Enabled"),
                CreateStepCurve(times, values));
        }

        private static bool TryGetSkinnedRendererBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                return false;
            }

            bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return true;
        }

        private static int AddDeathBlendShapeCurves(
            AnimationClip clip,
            Transform root,
            GameObject walkingInstance,
            float[] times)
        {
            var addedCurves = 0;
            foreach (var renderer in walkingInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = renderer.sharedMesh;
                if (mesh == null || mesh.blendShapeCount == 0)
                {
                    continue;
                }

                if (TryFindBlendShapeName(mesh, "Death_Melt_FlatLiquidSpread", out var meltShapeName))
                {
                    AddBlendShapeCurve(
                        clip,
                        root,
                        renderer.transform,
                        meltShapeName,
                        times,
                        new[] { 0f, 35f, 100f, 82f, 30f, 0f });
                    addedCurves++;
                }

                if (TryFindBlendShapeName(mesh, "Death_Puddle_Final", out var puddleShapeName))
                {
                    AddBlendShapeCurve(
                        clip,
                        root,
                        renderer.transform,
                        puddleShapeName,
                        times,
                        new[] { 0f, 0f, 8f, 55f, 88f, 100f });
                    addedCurves++;
                }
            }

            return addedCurves;
        }

        private static void AddBlendShapeCurve(
            AnimationClip clip,
            Transform root,
            Transform rendererTransform,
            string blendShapeName,
            float[] times,
            float[] values)
        {
            if (times.Length != values.Length)
            {
                throw new ArgumentException("BlendShape curve times and values must have the same length.");
            }

            var path = AnimationUtility.CalculateTransformPath(rendererTransform, root);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(SkinnedMeshRenderer),
                    "blendShape." + blendShapeName),
                CreateValueCurve(times, values));
        }

        private static bool TryFindBlendShapeName(Mesh mesh, string expectedName, out string blendShapeName)
        {
            for (var index = 0; index < mesh.blendShapeCount; index++)
            {
                var candidate = mesh.GetBlendShapeName(index);
                if (string.Equals(candidate, expectedName, StringComparison.Ordinal))
                {
                    blendShapeName = candidate;
                    return true;
                }
            }

            for (var index = 0; index < mesh.blendShapeCount; index++)
            {
                var candidate = mesh.GetBlendShapeName(index);
                if (candidate.IndexOf(expectedName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    blendShapeName = candidate;
                    return true;
                }
            }

            blendShapeName = string.Empty;
            return false;
        }

        private static int CountCurveBindings(AnimationClip clip, string attributePrefix)
        {
            return AnimationUtility.GetCurveBindings(clip)
                .Count(binding => binding.propertyName.StartsWith(attributePrefix, StringComparison.Ordinal));
        }

        private static void AddLocalRotationOffsetCurves(
            AnimationClip clip,
            Transform root,
            Transform target,
            float[] times,
            Vector3[] eulerOffsets)
        {
            if (times.Length != eulerOffsets.Length)
            {
                throw new ArgumentException("Attack rotation curve times and offsets must have the same length.");
            }

            var path = AnimationUtility.CalculateTransformPath(target, root);
            var baseRotation = target.localRotation;
            var rotations = eulerOffsets
                .Select(offset => baseRotation * Quaternion.Euler(offset))
                .ToArray();

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.x"),
                CreateRotationComponentCurve(times, rotations.Select(rotation => rotation.x).ToArray()));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.y"),
                CreateRotationComponentCurve(times, rotations.Select(rotation => rotation.y).ToArray()));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.z"),
                CreateRotationComponentCurve(times, rotations.Select(rotation => rotation.z).ToArray()));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.w"),
                CreateRotationComponentCurve(times, rotations.Select(rotation => rotation.w).ToArray()));
        }

        private static AnimationCurve CreateRotationComponentCurve(float[] times, float[] values)
        {
            var keyframes = new Keyframe[times.Length];
            for (var index = 0; index < times.Length; index++)
            {
                keyframes[index] = new Keyframe(times[index], values[index]);
            }

            var curve = new AnimationCurve(keyframes);
            for (var index = 0; index < curve.length; index++)
            {
                curve.SmoothTangents(index, 0f);
            }

            return curve;
        }

        private static void AddScalePulseCurves(
            AnimationClip clip,
            Transform root,
            Transform target,
            Vector3 expandScale,
            Vector3 contractScale)
        {
            var path = AnimationUtility.CalculateTransformPath(target, root);
            var baseScale = target.localScale;

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalScale.x"),
                CreateLoopCurve(baseScale.x, expandScale.x, contractScale.x));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalScale.y"),
                CreateLoopCurve(baseScale.y, expandScale.y, contractScale.y));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalScale.z"),
                CreateLoopCurve(baseScale.z, expandScale.z, contractScale.z));
        }

        private static void AddLocalScaleCurves(
            AnimationClip clip,
            Transform root,
            Transform target,
            float[] times,
            Vector3[] scaleFactors)
        {
            if (times.Length != scaleFactors.Length)
            {
                throw new ArgumentException("Scale curve times and factors must have the same length.");
            }

            var path = AnimationUtility.CalculateTransformPath(target, root);
            var baseScale = target.localScale;
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalScale.x"),
                CreateScaleCurve(baseScale.x, times, scaleFactors.Select(scale => scale.x).ToArray()));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalScale.y"),
                CreateScaleCurve(baseScale.y, times, scaleFactors.Select(scale => scale.y).ToArray()));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalScale.z"),
                CreateScaleCurve(baseScale.z, times, scaleFactors.Select(scale => scale.z).ToArray()));
        }

        private static void AddLocalPositionOffsetCurves(
            AnimationClip clip,
            Transform root,
            Transform target,
            float[] times,
            Vector3[] offsets)
        {
            if (times.Length != offsets.Length)
            {
                throw new ArgumentException("Attack position curve times and offsets must have the same length.");
            }

            var path = AnimationUtility.CalculateTransformPath(target, root);
            var basePosition = target.localPosition;
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.x"),
                CreatePositionOffsetCurve(basePosition.x, times, offsets.Select(offset => offset.x).ToArray()));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.y"),
                CreatePositionOffsetCurve(basePosition.y, times, offsets.Select(offset => offset.y).ToArray()));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.z"),
                CreatePositionOffsetCurve(basePosition.z, times, offsets.Select(offset => offset.z).ToArray()));
        }

        private static void AddLocalPositionAbsoluteCurves(
            AnimationClip clip,
            Transform root,
            Transform target,
            float[] times,
            Vector3[] positions)
        {
            if (times.Length != positions.Length)
            {
                throw new ArgumentException("Absolute position curve times and values must have the same length.");
            }

            var path = AnimationUtility.CalculateTransformPath(target, root);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.x"),
                CreateValueCurve(times, positions.Select(position => position.x).ToArray()));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.y"),
                CreateValueCurve(times, positions.Select(position => position.y).ToArray()));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.z"),
                CreateValueCurve(times, positions.Select(position => position.z).ToArray()));
        }

        private static AnimationCurve CreatePositionOffsetCurve(float baseValue, float[] times, float[] offsets)
        {
            var keyframes = new Keyframe[times.Length];
            for (var index = 0; index < times.Length; index++)
            {
                keyframes[index] = new Keyframe(times[index], baseValue + offsets[index]);
            }

            var curve = new AnimationCurve(keyframes);
            for (var index = 0; index < curve.length; index++)
            {
                curve.SmoothTangents(index, 0f);
            }

            return curve;
        }

        private static AnimationCurve CreateValueCurve(float[] times, float[] values)
        {
            var keyframes = new Keyframe[times.Length];
            for (var index = 0; index < times.Length; index++)
            {
                keyframes[index] = new Keyframe(times[index], values[index]);
            }

            var curve = new AnimationCurve(keyframes);
            for (var index = 0; index < curve.length; index++)
            {
                curve.SmoothTangents(index, 0f);
            }

            return curve;
        }

        private static AnimationCurve CreateStepCurve(float[] times, float[] values)
        {
            var keyframes = new Keyframe[times.Length];
            for (var index = 0; index < times.Length; index++)
            {
                keyframes[index] = new Keyframe(times[index], values[index])
                {
                    inTangent = float.PositiveInfinity,
                    outTangent = float.PositiveInfinity
                };
            }

            return new AnimationCurve(keyframes);
        }

        private static AnimationCurve CreateScaleCurve(float baseValue, float[] times, float[] scaleFactors)
        {
            var keyframes = new Keyframe[times.Length];
            for (var index = 0; index < times.Length; index++)
            {
                keyframes[index] = new Keyframe(times[index], baseValue * scaleFactors[index]);
            }

            var curve = new AnimationCurve(keyframes);
            for (var index = 0; index < curve.length; index++)
            {
                curve.SmoothTangents(index, 0f);
            }

            return curve;
        }

        private static AnimationCurve CreateLoopCurve(float baseValue, float expandFactor, float contractFactor)
        {
            var curve = new AnimationCurve(
                new Keyframe(0.00f, baseValue),
                new Keyframe(0.50f, baseValue * expandFactor),
                new Keyframe(1.00f, baseValue),
                new Keyframe(1.50f, baseValue * contractFactor),
                new Keyframe(2.00f, baseValue));
            for (var index = 0; index < curve.length; index++)
            {
                curve.SmoothTangents(index, 0f);
            }

            return curve;
        }

        private static Transform RequireChild(Transform root, string name)
        {
            var child = FindChildRecursive(root, name);
            if (child == null)
            {
                throw new InvalidOperationException("Required Longa Arma rig bone was not found: " + name);
            }

            return child;
        }

        private static int CountWalkingAnimatorsForState(Transform placementRoot, string stateRootName)
        {
            var stateRoot = FindChildRecursive(placementRoot, stateRootName);
            if (stateRoot == null)
            {
                throw new InvalidOperationException("Approved Longa Arma state root was not found: " + stateRootName);
            }

            var approvedModel = FindChildRecursive(stateRoot, ApprovedModelName);
            if (approvedModel == null)
            {
                throw new InvalidOperationException("Approved model root was not found under state: " + stateRootName);
            }

            var walking = FindChildRecursive(approvedModel, WalkingInstanceName);
            if (walking == null)
            {
                throw new InvalidOperationException("Walking model was not found under state: " + stateRootName);
            }

            return walking.GetComponentsInChildren<Animator>(true).Length;
        }

        private static void ConfigureWalkingFbxImporter()
        {
            var importer = AssetImporter.GetAtPath(WalkingFbxPath) as ModelImporter;
            if (importer == null)
            {
                throw new FileNotFoundException("Walking FBX importer was not found.", WalkingFbxPath);
            }

            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.animationWrapMode = WrapMode.Loop;

            var clips = importer.defaultClipAnimations;
            if (clips != null && clips.Length > 0)
            {
                for (var index = 0; index < clips.Length; index++)
                {
                    if (string.IsNullOrWhiteSpace(clips[index].name))
                    {
                        clips[index].name = "LongaArma_Walking_FromFbx";
                    }

                    clips[index].wrapMode = WrapMode.Loop;
                    clips[index].loopTime = true;
                    clips[index].loopPose = true;
                    clips[index].lockRootRotation = true;
                    clips[index].lockRootHeightY = true;
                    clips[index].lockRootPositionXZ = true;
                }

                importer.clipAnimations = clips;
            }

            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(
                WalkingFbxPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static Scene OpenCargoRunScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.path == CargoRunScenePath)
            {
                return scene;
            }

            return EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
        }

        private static AnimationClip LoadWalkingClip()
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(WalkingFbxPath)
                .OfType<AnimationClip>()
                .Where(candidate => !candidate.name.StartsWith("__preview__", StringComparison.Ordinal))
                .OrderByDescending(candidate => candidate.length)
                .FirstOrDefault();

            if (clip == null)
            {
                throw new InvalidOperationException("Walking FBX has no imported animation clip.");
            }

            return clip;
        }

        private static bool IsLoopingClip(AnimationClip clip)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            return settings.loopTime;
        }

        private static AnimatorController EnsureWalkingController(AnimationClip clip)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(WalkingControllerPath) ?? string.Empty);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(WalkingControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(WalkingControllerPath);
            }

            if (controller.layers == null || controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(candidate => candidate.name == "LongaArma_Walking_FromFbx");

            if (state == null)
            {
                state = stateMachine.AddState("LongaArma_Walking_FromFbx");
            }

            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            throw new InvalidOperationException("Scene root was not found: " + name);
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    return child;
                }

                var nested = FindChildRecursive(child, name);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static void DisableImportedReviewObjects(Transform root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child == root)
                {
                    continue;
                }

                if (child.name == "Cube" || child.name == "Light" || child.name == "Camera")
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private static void ReplaceApprovedModelChildrenWithWalkingCopy(
            Scene scene,
            GameObject walkingPrefab,
            Transform sourceWalking,
            Transform approvedModel,
            RuntimeAnimatorController walkingController,
            Avatar walkingAvatar)
        {
            var existingWalking = FindChildRecursive(approvedModel, WalkingInstanceName);
            if (existingWalking != null)
            {
                UnityEngine.Object.DestroyImmediate(existingWalking.gameObject);
            }

            foreach (Transform child in approvedModel)
            {
                child.gameObject.SetActive(false);
            }

            var walkingInstance = (GameObject)PrefabUtility.InstantiatePrefab(walkingPrefab, scene);
            walkingInstance.name = WalkingInstanceName;
            walkingInstance.transform.SetParent(approvedModel, false);
            walkingInstance.transform.localPosition = sourceWalking.localPosition;
            walkingInstance.transform.localRotation = sourceWalking.localRotation;
            walkingInstance.transform.localScale = sourceWalking.localScale;
            walkingInstance.SetActive(true);

            DisableImportedReviewObjects(walkingInstance.transform);

            var animator = walkingInstance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = walkingInstance.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = walkingController;
            animator.avatar = walkingAvatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;

            AssignWalkingMaterials(walkingInstance);

            var parentAnimator = approvedModel.GetComponent<Animator>();
            if (parentAnimator != null)
            {
                parentAnimator.enabled = false;
            }
        }

        private static int DestroyComponentsInChildren<T>(Transform root) where T : Component
        {
            var components = root.GetComponentsInChildren<T>(true);
            foreach (var component in components)
            {
                UnityEngine.Object.DestroyImmediate(component);
            }

            return components.Length;
        }

        private static void AssignWalkingMaterials(GameObject walkingInstance)
        {
            var bodyMaterial = LoadMaterial(BodyMaterialPath);
            var darkMaterial = LoadMaterial(DarkMaterialPath);
            var slimeMaterial = LoadMaterial(SlimeMaterialPath);
            var replacement = new[] { bodyMaterial, darkMaterial, slimeMaterial };

            foreach (var renderer in walkingInstance.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.name.Equals("char1", StringComparison.OrdinalIgnoreCase) &&
                    !renderer.gameObject.name.Equals("char1", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    continue;
                }

                var applied = new Material[Mathf.Max(materials.Length, replacement.Length)];
                for (var index = 0; index < applied.Length; index++)
                {
                    applied[index] = replacement[Mathf.Min(index, replacement.Length - 1)];
                }

                renderer.sharedMaterials = applied;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static Material LoadMaterial(string path)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                throw new FileNotFoundException("Longa Arma walking material was not found.", path);
            }

            return material;
        }

        private static int CountVisibleRenderers(GameObject root)
        {
            return root.GetComponentsInChildren<Renderer>(true)
                .Count(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy);
        }

        private static bool TryGetRendererBounds(GameObject root, out Bounds bounds, bool includeInactive = false)
        {
            bounds = default;
            if (root == null)
            {
                return false;
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled && (includeInactive || renderer.gameObject.activeInHierarchy))
                .ToArray();
            if (renderers.Length == 0)
            {
                return false;
            }

            bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return true;
        }

        private static void FitToReferenceBounds(
            Transform walkingInstance,
            Transform parent,
            Bounds referenceBounds,
            Bounds walkingBounds)
        {
            var referenceHeight = Mathf.Max(referenceBounds.size.y, 0.0001f);
            var walkingHeight = Mathf.Max(walkingBounds.size.y, 0.0001f);
            var scaleFactor = referenceHeight / walkingHeight;
            if (float.IsFinite(scaleFactor) && scaleFactor > 0f)
            {
                walkingInstance.localScale *= scaleFactor;
            }

            if (TryGetRendererBounds(walkingInstance.gameObject, out var scaledBounds))
            {
                var worldOffset = referenceBounds.center - scaledBounds.center;
                walkingInstance.localPosition += parent.InverseTransformVector(worldOffset);
            }
        }
    }
}
