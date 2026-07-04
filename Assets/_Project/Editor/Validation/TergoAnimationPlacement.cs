using System;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.TergoCargoRunScene
{
    internal static class TergoAnimationPlacement
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string TergoModelAssetPath = "Assets/_Project/Art/Enemies/Tergo/Models/tergo.fbx";

        private const string CorridorRootName = "Approved Ship Corridor Segments";
        private const string ParvumPlacementRootName = "Approved Parvum Enemy Placement";
        private const string FugaPlacementRootName = "Approved Fuga Enemy Placement";
        private const string LongaPlacementRootName = "Approved Longa Arma Enemy Placement";
        private const string PlacementRootName = "Approved Tergo Enemy Placement";
        private const string PlayerRootName = "Player";

        private const float MinimumFugaParvumZGap = 0.30f;
        private const float TergoTargetHeightMeters = 1.50f;
        private const float TergoPlacementSpacing = 1.45f;
        private const float TergoPlayerFrontDistance = 8.00f;
        private const float TergoFallbackYawDegrees = 180f;

        private static readonly PlacementSpec[] PlacementSpecs =
        {
            new("Tergo_00_Static_Review", "Static comparison"),
            new("Tergo_01_Idle", "Idle"),
            new("Tergo_02_Walk_Wander", "Walk_Wander"),
            new("Tergo_03_Detect_User", "Detect_User"),
            new("Tergo_04_BackRush", "BackRush"),
            new("Tergo_05_Pierce_Attack", "Pierce_Attack"),
            new("Tergo_06_Pierce_Recovery", "Pierce_Recovery"),
            new("Tergo_07_Downed_Pounce", "Downed_Pounce"),
            new("Tergo_08_Downed_Drill_Attack_Loop", "Downed_Drill_Attack_Loop"),
            new("Tergo_09_Interrupt_Stagger", "Interrupt_Stagger"),
            new("Tergo_10_Crouch_Tremble_5s", "Crouch_Tremble_5s"),
            new("Tergo_11_Hit_Normal", "Hit_Normal"),
            new("Tergo_12_Death", "Death")
        };

        [MenuItem("Bellerophon/Enemies/Tergo/Apply Animation Placement")]
        public static void ApplyTergoAnimationPlacement()
        {
            AssetDatabase.ImportAsset(TergoModelAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TergoModelAssetPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Missing Tergo model asset: " + TergoModelAssetPath);
            }

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var corridorBounds = FindRendererBounds(CorridorRootName, new Bounds(Vector3.zero, new Vector3(16f, 3f, 12f)));
            var parvumRoot = RequireObject(ParvumPlacementRootName);
            var fugaRoot = RequireObject(FugaPlacementRootName);
            var longaRoot = RequireObject(LongaPlacementRootName);

            var targetPosition = CalculateTergoPlacementPosition(corridorBounds, parvumRoot, fugaRoot, longaRoot);
            var facingYaw = CalculateLongaFacingYaw(longaRoot.transform);
            var sceneScale = CalculateTergoSceneScale(prefab);

            var existingRoot = FindSceneObject(PlacementRootName);
            if (existingRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(existingRoot);
            }

            var placementRoot = new GameObject(PlacementRootName);
            placementRoot.transform.SetPositionAndRotation(targetPosition, Quaternion.identity);
            placementRoot.transform.localScale = Vector3.one;

            for (var i = 0; i < PlacementSpecs.Length; i++)
            {
                var spec = PlacementSpecs[i];
                var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (instance == null)
                {
                    instance = UnityEngine.Object.Instantiate(prefab);
                    SceneManager.MoveGameObjectToScene(instance, scene);
                }

                instance.name = spec.ObjectName;
                instance.transform.SetParent(placementRoot.transform, false);
                instance.transform.localPosition = new Vector3(
                    (i - (PlacementSpecs.Length - 1) * 0.5f) * TergoPlacementSpacing,
                    0f,
                    0f);
                instance.transform.localRotation = Quaternion.Euler(0f, facingYaw, 0f);
                instance.transform.localScale = Vector3.one * sceneScale;
                instance.SetActive(true);

                AlignObjectBottomToY(instance.transform, corridorBounds.min.y);
                RemoveReviewAnimator(instance);
                EditorUtility.SetDirty(instance);
            }

            ConfigureInitialPlayerStart(placementRoot.transform, facingYaw);
            InspectTergoPlacement(placementRoot.transform, parvumRoot, fugaRoot, longaRoot, facingYaw);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after Tergo placement.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoAnimationPlacementApplied" +
                ", Root=" + PlacementRootName +
                ", Model=" + TergoModelAssetPath +
                ", Count=" + PlacementSpecs.Length.ToString(CultureInfo.InvariantCulture) +
                ", StaticCount=1" +
                ", MotionSlotCount=12" +
                ", FacingYaw=" + facingYaw.ToString("0.###", CultureInfo.InvariantCulture) +
                ", SceneScale=" + sceneScale.ToString("0.###", CultureInfo.InvariantCulture) +
                ", ParvumZ=" + parvumRoot.transform.position.z.ToString("0.###", CultureInfo.InvariantCulture) +
                ", FugaZ=" + fugaRoot.transform.position.z.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LongaZ=" + longaRoot.transform.position.z.ToString("0.###", CultureInfo.InvariantCulture) +
                ", TergoZ=" + placementRoot.transform.position.z.ToString("0.###", CultureInfo.InvariantCulture) +
                ", PlayerFacesTergo=True" +
                ", AnimationClipsCreated=False");
        }

        private static Vector3 CalculateTergoPlacementPosition(
            Bounds corridorBounds,
            GameObject parvumRoot,
            GameObject fugaRoot,
            GameObject longaRoot)
        {
            var fugaParvumGap = Mathf.Max(
                Mathf.Abs(fugaRoot.transform.position.z - parvumRoot.transform.position.z),
                MinimumFugaParvumZGap);
            var directionFromFugaToLonga = Mathf.Sign(longaRoot.transform.position.z - fugaRoot.transform.position.z);
            if (Mathf.Abs(directionFromFugaToLonga) < 0.001f)
            {
                directionFromFugaToLonga = Mathf.Sign(fugaRoot.transform.position.z - parvumRoot.transform.position.z);
            }

            if (Mathf.Abs(directionFromFugaToLonga) < 0.001f)
            {
                directionFromFugaToLonga = -1f;
            }

            return new Vector3(
                longaRoot.transform.position.x,
                corridorBounds.min.y,
                longaRoot.transform.position.z + directionFromFugaToLonga * fugaParvumGap);
        }

        private static float CalculateLongaFacingYaw(Transform longaRoot)
        {
            var reference =
                longaRoot.Find("LongaArma_00_Static_Review") ??
                longaRoot.Find("LongaArma_02_Move_Crawl") ??
                (longaRoot.childCount > 0 ? longaRoot.GetChild(0) : null);

            return reference != null ? reference.eulerAngles.y : TergoFallbackYawDegrees;
        }

        private static float CalculateTergoSceneScale(GameObject prefab)
        {
            var preview = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (preview == null)
            {
                throw new InvalidOperationException("Could not instantiate Tergo prefab for scale calculation.");
            }

            try
            {
                preview.hideFlags = HideFlags.HideAndDontSave;
                preview.transform.position = new Vector3(10000f, 10000f, 10000f);
                preview.transform.rotation = Quaternion.identity;
                preview.transform.localScale = Vector3.one;
                var bounds = CalculateRendererBounds(preview.transform, new Bounds(preview.transform.position, Vector3.one));
                var measuredHeight = bounds.size.y;
                if (measuredHeight <= 0.0001f || float.IsNaN(measuredHeight) || float.IsInfinity(measuredHeight))
                {
                    return 1f;
                }

                return Mathf.Clamp(TergoTargetHeightMeters / measuredHeight, 0.001f, 500f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        private static void AlignObjectBottomToY(Transform root, float floorY)
        {
            var bounds = CalculateRendererBounds(root, new Bounds(root.position, Vector3.one));
            root.position += new Vector3(0f, floorY - bounds.min.y, 0f);
        }

        private static void RemoveReviewAnimator(GameObject instance)
        {
            foreach (var animator in instance.GetComponentsInChildren<Animator>(true))
            {
                animator.runtimeAnimatorController = null;
                EditorUtility.SetDirty(animator);
            }
        }

        private static void ConfigureInitialPlayerStart(Transform placementRoot, float facingYaw)
        {
            var player = FindPlayerStartTransform();
            if (player == null)
            {
                throw new InvalidOperationException("Could not find Player start transform in CargoRunMvp scene.");
            }

            var bounds = CalculateRendererBounds(placementRoot, new Bounds(placementRoot.position, Vector3.one));
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.10f, 0.08f, 0.28f);
            var frontDirection = CalculateFrontDirection(facingYaw);
            var startPosition = new Vector3(
                lookAt.x + frontDirection.x * TergoPlayerFrontDistance,
                0f,
                lookAt.z + frontDirection.z * TergoPlayerFrontDistance);

            player.SetPositionAndRotation(startPosition, CalculateYawRotationToward(startPosition, lookAt));
            EditorUtility.SetDirty(player);
        }

        private static void InspectTergoPlacement(
            Transform placementRoot,
            GameObject parvumRoot,
            GameObject fugaRoot,
            GameObject longaRoot,
            float facingYaw)
        {
            if (placementRoot.childCount != PlacementSpecs.Length)
            {
                throw new InvalidOperationException(
                    $"Tergo placement must contain {PlacementSpecs.Length} children, but found {placementRoot.childCount}.");
            }

            for (var i = 0; i < PlacementSpecs.Length; i++)
            {
                var spec = PlacementSpecs[i];
                var child = placementRoot.Find(spec.ObjectName);
                if (child == null)
                {
                    throw new InvalidOperationException("Missing Tergo placement child: " + spec.ObjectName);
                }

                var renderers = child.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    throw new InvalidOperationException(spec.ObjectName + " has no renderers.");
                }

                var yaw = child.eulerAngles.y;
                if (Mathf.Abs(Mathf.DeltaAngle(yaw, facingYaw)) > 0.5f)
                {
                    throw new InvalidOperationException(
                        $"{spec.ObjectName} yaw must match Longa Arma yaw {facingYaw:0.###}, but was {yaw:0.###}.");
                }
            }

            var fugaParvumGap = Mathf.Max(
                Mathf.Abs(fugaRoot.transform.position.z - parvumRoot.transform.position.z),
                MinimumFugaParvumZGap);
            var tergoLongaGap = Mathf.Abs(placementRoot.position.z - longaRoot.transform.position.z);
            if (Mathf.Abs(tergoLongaGap - fugaParvumGap) > 0.05f)
            {
                throw new InvalidOperationException(
                    $"Tergo is not using the Parvum/Fuga Z gap from Longa Arma. Tergo/Longa={tergoLongaGap:0.###}, Parvum/Fuga={fugaParvumGap:0.###}.");
            }

            var sequenceDirection = Mathf.Sign(longaRoot.transform.position.z - fugaRoot.transform.position.z);
            var tergoDirection = Mathf.Sign(placementRoot.position.z - longaRoot.transform.position.z);
            if (Mathf.Abs(sequenceDirection) > 0.001f &&
                Mathf.Abs(tergoDirection) > 0.001f &&
                Mathf.Sign(sequenceDirection) != Mathf.Sign(tergoDirection))
            {
                throw new InvalidOperationException("Tergo must be placed below Longa Arma along the same Z sequence direction.");
            }

            InspectPlayerStart(placementRoot, facingYaw);
        }

        private static void InspectPlayerStart(Transform placementRoot, float facingYaw)
        {
            var player = FindPlayerStartTransform();
            if (player == null)
            {
                throw new InvalidOperationException("Player start transform is missing.");
            }

            var bounds = CalculateRendererBounds(placementRoot, new Bounds(placementRoot.position, Vector3.one));
            var lookAt = bounds.center;
            var expectedFront = CalculateFrontDirection(facingYaw);
            var playerFromFocus = player.position - lookAt;
            playerFromFocus.y = 0f;

            if (playerFromFocus.sqrMagnitude < 0.001f || Vector3.Dot(playerFromFocus.normalized, expectedFront) < 0.94f)
            {
                throw new InvalidOperationException("Player start is not placed in front of Tergo.");
            }

            var toFocus = lookAt - player.position;
            toFocus.y = 0f;
            if (toFocus.sqrMagnitude < 0.001f || Vector3.Dot(player.forward, toFocus.normalized) < 0.94f)
            {
                throw new InvalidOperationException("Player start is not facing Tergo.");
            }
        }

        private static Vector3 CalculateFrontDirection(float facingYaw)
        {
            var front = Quaternion.Euler(0f, facingYaw, 0f) * Vector3.forward;
            front.y = 0f;
            return front.sqrMagnitude > 0.001f ? front.normalized : Vector3.back;
        }

        private static Quaternion CalculateYawRotationToward(Vector3 position, Vector3 target)
        {
            var facing = target - position;
            facing.y = 0f;
            return facing.sqrMagnitude > 0.001f ? Quaternion.LookRotation(facing.normalized, Vector3.up) : Quaternion.identity;
        }

        private static Transform FindPlayerStartTransform()
        {
            var player = FindSceneObject(PlayerRootName);
            if (player != null)
            {
                return player.transform;
            }

            var characterController = UnityEngine.Object.FindFirstObjectByType<CharacterController>();
            return characterController != null ? characterController.transform : null;
        }

        private static Bounds FindRendererBounds(string objectName, Bounds fallback)
        {
            var root = FindSceneObject(objectName);
            return root != null ? CalculateRendererBounds(root.transform, fallback) : fallback;
        }

        private static Bounds CalculateRendererBounds(Transform root, Bounds fallback)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return fallback;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static GameObject RequireObject(string objectName)
        {
            var target = FindSceneObject(objectName);
            if (target == null)
            {
                throw new InvalidOperationException("Missing required object in CargoRunMvp scene: " + objectName);
            }

            return target;
        }

        private static GameObject FindSceneObject(string objectName)
        {
            var active = GameObject.Find(objectName);
            if (active != null)
            {
                return active;
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

            return null;
        }

        private readonly struct PlacementSpec
        {
            public PlacementSpec(string objectName, string motionId)
            {
                ObjectName = objectName;
                MotionId = motionId;
            }

            public string ObjectName { get; }

            public string MotionId { get; }
        }
    }
}
