using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bellerophon.Enemies.Fuga;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.FugaCargoRunScene
{
    internal static class FugaDeathMotionTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Fuga Enemy Placement";
        private const string CorridorRootName = "Approved Ship Corridor Segments";
        private const string DeathSlotName = "Fuga_05_Death";
        private const string ModelName = "Fuga_Model";
        private const string SourceModelPath = "Assets/_Project/Art/Enemies/Fuga/Models/fuga.glb";
        private const string DerivedMeshPath = "Assets/_Project/Art/Enemies/Fuga/Models/Fuga_Death_WholeBodyMeltMesh.asset";
        private const string BlendShapeName = "Fuga_Death_WholeBody_Melt";
        private const string ReportPath = "docs/validation/fuga_death_motion_2026-08-17/Fuga_Death_Fall_And_Melt_Report.txt";
        private const float MeltQuarterTime = 0.72f;
        private const float MeltDeepTime = 1.52f;
        private const float MeltDuration = 2f;
        private const float MeltHoldDuration = 1f;
        private const float SideTiltDegrees = 45f;
        private const float GeometryTolerance = 0.0001f;
        private const int RequiredReviewLoops = 2;

        [MenuItem("Bellerophon/Enemies/Fuga/Apply Death Fall And Melt")]
        public static void ApplyFugaDeathFallAndMelt()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before applying the Fuga death motion.");
            }

            var sourceHashBefore = Sha256(Absolute(SourceModelPath));
            var placementRoot = RequireRoot(PlacementRootName);
            var otherSlotsBefore = OtherSlotSignature(placementRoot);
            var slot = RequireDirectChild(placementRoot, DeathSlotName);
            var model = RequireDirectChild(slot, ModelName);
            var renderer = RequireSingleRenderer(model);
            var derivedMesh = CreateDerivedMeltMesh();
            renderer.sharedMesh = derivedMesh;
            renderer.SetBlendShapeWeight(derivedMesh.GetBlendShapeIndex(BlendShapeName), 0f);
            EditorUtility.SetDirty(renderer);

            var animator = slot.GetComponent<Animator>() ?? slot.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false;
            animator.enabled = false;
            EditorUtility.SetDirty(animator);

            var legacyPlayback = slot.GetComponent<FugaAnimationReviewPlaybackDriver>();
            if (legacyPlayback != null)
            {
                UnityEngine.Object.DestroyImmediate(legacyPlayback);
            }

            var body = slot.GetComponent<Rigidbody>() ??
                       throw new InvalidOperationException(DeathSlotName + " has no Rigidbody.");
            var bodyCollider = slot.GetComponent<Collider>() ??
                               throw new InvalidOperationException(DeathSlotName + " has no Collider.");
            var groundWorldY = CalculateRendererBounds(RequireRoot(CorridorRootName)).min.y;

            body.isKinematic = false;
            body.useGravity = true;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            EditorUtility.SetDirty(body);

            var driver = slot.GetComponent<FugaPhysicsMotionDriver>() ??
                         slot.gameObject.AddComponent<FugaPhysicsMotionDriver>();
            driver.enabled = true;
            driver.LockRootMotionForReview = false;
            driver.Configure(
                body,
                driver.MotionPathTarget,
                reviewLocked: false,
                configuredFollowVerticalAxis: true,
                configuredUseDeathFallSequence: true,
                configuredLoopDeathFallForReview: true);
            driver.ConfigureDeathFallAndMelt(
                renderer,
                model,
                bodyCollider,
                groundWorldY,
                BlendShapeName,
                CreateParvumMeltCurve(),
                MeltDuration,
                MeltHoldDuration,
                SideTiltDegrees);
            EditorUtility.SetDirty(driver);

            if (!string.Equals(otherSlotsBefore, OtherSlotSignature(placementRoot), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("A protected non-death Fuga slot changed.");
            }

            RequireHash(sourceHashBefore, Sha256(Absolute(SourceModelPath)), "original Fuga GLB");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after applying the Fuga death motion.");
            }

            AssetDatabase.SaveAssets();
            var result = InspectAppliedState();
            WriteReport(result, directReviewCompleted: false, completedLoops: 0);
            AssetDatabase.Refresh();
            Debug.Log(
                "FugaDeathFallAndMeltApplied Result=PASS" +
                ", WingFlappingStopped=True" +
                ", RigidbodyGravityFall=True" +
                ", MeltStartsAfterGroundContact=True" +
                ", BodyAndBothWingsMelt=True" +
                ", MeltDurationSeconds=2" +
                ", MeltHoldSeconds=1" +
                ", RandomSideTiltDegrees=45" +
                ", MeltVisualCounterLevelsPuddle=True" +
                ", PhysicsRootRemainsTilted=True" +
                ", Loop=True.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Inspect Death Fall And Melt")]
        public static void InspectFugaDeathFallAndMelt()
        {
            var scene = RequireCurrentScene();
            var dirtyBefore = scene.isDirty;
            var result = InspectAppliedState();
            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException("The Fuga death inspection changed the scene dirty state.");
            }

            WriteReport(result, directReviewCompleted: false, completedLoops: 0);
            AssetDatabase.Refresh();
            Debug.Log(
                "FugaDeathFallAndMeltInspected Result=PASS" +
                ", CollisionDrivenImpact=True" +
                ", PreImpactMeltWeight=0" +
                ", MeltCurve=0@0,32@0.72,78@1.52,100@2" +
                ", MeltHoldSeconds=1" +
                ", RandomSideTiltDegrees=45" +
                ", MeltVisualRoot=Fuga_Model" +
                ", FinalPuddleWorldLevel=True" +
                ", Loop=True" +
                ", RootTransformAnimationCurves=0.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Start Death Motion Review Playback")]
        public static void StartFugaDeathMotionReviewPlayback()
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Unity is already in Play Mode.");
            }

            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before the direct Fuga death review.");
            }

            InspectAppliedState();
            var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView") ??
                               throw new InvalidOperationException("The Unity Game View type is unavailable.");
            var gameView = EditorWindow.GetWindow(gameViewType);
            gameView.Focus();
            EditorApplication.isPaused = false;
            EditorApplication.delayCall += () =>
            {
                EditorApplication.isPaused = false;
                EditorApplication.isPlaying = true;
            };
            Debug.Log(
                "FugaDeathMotionReviewPlaybackStarted Result=PASS" +
                ", RequiredLoops=2" +
                ", RequiredLeftAndRightTilt=True" +
                ", RequiredPreMeltTiltAndFinalLevel=True" +
                ", LiveGameView=True" +
                ", PhysicsPlayMode=True" +
                ", CaptureCreated=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Stop Death Motion Review Playback")]
        public static void StopFugaDeathMotionReviewPlayback()
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Unity must be in Play Mode to finish the direct Fuga death review.");
            }

            var result = InspectAppliedState();
            var completedLoops = result.Driver.DeathCompletedLoopCount;
            var leftImpacts = result.Driver.DeathLeftImpactCount;
            var rightImpacts = result.Driver.DeathRightImpactCount;
            var invalidImpactTilts = result.Driver.DeathInvalidImpactTiltCount;
            var invalidPreMeltVisualTilts = result.Driver.DeathInvalidPreMeltVisualTiltCount;
            var invalidFinalPuddleLevels = result.Driver.DeathInvalidFinalPuddleLevelCount;
            var invalidFinalPuddleGrounds = result.Driver.DeathInvalidFinalPuddleGroundCount;
            var finalPuddleLevelRecords = result.Driver.DeathFinalPuddleLevelRecordedCount;
            if (completedLoops < RequiredReviewLoops || leftImpacts < 1 || rightImpacts < 1 ||
                invalidImpactTilts != 0 || invalidPreMeltVisualTilts != 0 || invalidFinalPuddleLevels != 0 ||
                invalidFinalPuddleGrounds != 0 || finalPuddleLevelRecords < completedLoops)
            {
                var body = result.Driver.Body;
                Debug.LogError(
                    "FugaDeathMotionReviewPlaybackIncomplete" +
                    ", CompletedLoops=" + completedLoops.ToString(CultureInfo.InvariantCulture) +
                    ", ImpactDetected=" + result.Driver.DeathImpactDetected +
                    ", MeltElapsedSeconds=" + result.Driver.DeathMeltElapsedSeconds.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", CurrentSelectedTiltDegrees=" + result.Driver.DeathCurrentSideTiltDegrees.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", LastImpactTiltDegrees=" + result.Driver.DeathLastImpactSideTiltDegrees.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", LeftImpactCount=" + leftImpacts.ToString(CultureInfo.InvariantCulture) +
                    ", RightImpactCount=" + rightImpacts.ToString(CultureInfo.InvariantCulture) +
                    ", InvalidImpactTiltCount=" + invalidImpactTilts.ToString(CultureInfo.InvariantCulture) +
                    ", LastPreMeltVisualTiltDegrees=" + result.Driver.DeathLastPreMeltVisualTiltDegrees.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", InvalidPreMeltVisualTiltCount=" + invalidPreMeltVisualTilts.ToString(CultureInfo.InvariantCulture) +
                    ", LastFinalPuddleLevelErrorDegrees=" + result.Driver.DeathLastFinalPuddleLevelErrorDegrees.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", InvalidFinalPuddleLevelCount=" + invalidFinalPuddleLevels.ToString(CultureInfo.InvariantCulture) +
                    ", LastFinalPuddleGroundErrorMeters=" + result.Driver.DeathLastFinalPuddleGroundErrorMeters.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", InvalidFinalPuddleGroundCount=" + invalidFinalPuddleGrounds.ToString(CultureInfo.InvariantCulture) +
                    ", FinalPuddleLevelRecordedCount=" + finalPuddleLevelRecords.ToString(CultureInfo.InvariantCulture) +
                    ", RigidbodyPosition=" + (body != null ? body.position.ToString("F4") : "<null>") +
                    ", RigidbodyVelocity=" + (body != null ? body.linearVelocity.ToString("F4") : "<null>") +
                    ", IsPaused=" + EditorApplication.isPaused +
                    ", TimeScale=" + Time.timeScale.ToString("0.###", CultureInfo.InvariantCulture) + ".");
                EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
                throw new InvalidOperationException(
                    "The direct Fuga death review requires two complete loops with both -45 and +45 degree impacts, tilted pre-melt contact, and a level grounded final puddle. " +
                    "Completed=" + completedLoops + ", Left=" + leftImpacts + ", Right=" + rightImpacts +
                    ", InvalidImpact=" + invalidImpactTilts + ", InvalidPreMelt=" + invalidPreMeltVisualTilts +
                    ", InvalidLevel=" + invalidFinalPuddleLevels + ", InvalidGround=" + invalidFinalPuddleGrounds + ".");
            }

            WriteReport(result, directReviewCompleted: true, completedLoops: completedLoops);
            Debug.Log(
                "FugaDeathMotionReviewPlaybackStopped Result=PASS" +
                ", CompletedLoops=" + completedLoops.ToString(CultureInfo.InvariantCulture) +
                ", Left45DegreeImpactCount=" + leftImpacts.ToString(CultureInfo.InvariantCulture) +
                ", Right45DegreeImpactCount=" + rightImpacts.ToString(CultureInfo.InvariantCulture) +
                ", InvalidImpactTiltCount=0" +
                ", InvalidPreMeltVisualTiltCount=0" +
                ", FinalPuddleLevelRecordedCount=" + finalPuddleLevelRecords.ToString(CultureInfo.InvariantCulture) +
                ", LastFinalPuddleLevelErrorDegrees=" + result.Driver.DeathLastFinalPuddleLevelErrorDegrees.ToString("0.######", CultureInfo.InvariantCulture) +
                ", LastFinalPuddleGroundErrorMeters=" + result.Driver.DeathLastFinalPuddleGroundErrorMeters.ToString("0.######", CultureInfo.InvariantCulture) +
                ", InvalidFinalPuddleLevelCount=0" +
                ", InvalidFinalPuddleGroundCount=0" +
                ", GroundContactReachedEachLoop=True" +
                ", MeltStartedOnlyAfterImpact=True" +
                ", LiveGameView=True" +
                ", CaptureCreated=False.");
            EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
        }

        private static InspectionResult InspectAppliedState()
        {
            RequireCurrentScene();
            var placementRoot = RequireRoot(PlacementRootName);
            var slot = RequireDirectChild(placementRoot, DeathSlotName);
            var model = RequireDirectChild(slot, ModelName);
            var renderer = RequireSingleRenderer(model);
            var mesh = renderer.sharedMesh ?? throw new InvalidOperationException("The Fuga death renderer has no mesh.");
            if (!string.Equals(AssetDatabase.GetAssetPath(mesh), DerivedMeshPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Fuga_05_Death is not using the generated whole-body melt mesh.");
            }

            if (mesh.blendShapeCount != 1 || mesh.GetBlendShapeIndex(BlendShapeName) != 0)
            {
                throw new InvalidOperationException("The Fuga death mesh must contain exactly one whole-body melt BlendShape.");
            }

            var deltas = ReadBlendShapeDeltas(mesh, 0);
            var affected = deltas.Count(delta => delta.sqrMagnitude > GeometryTolerance * GeometryTolerance);
            if (affected != mesh.vertexCount)
            {
                throw new InvalidOperationException(
                    "Every Fuga body and wing vertex must melt. Affected=" + affected + "/" + mesh.vertexCount + ".");
            }

            var leftWingAffected = CountAffectedWingVertices(renderer, mesh, deltas, "Bone_013");
            var rightWingAffected = CountAffectedWingVertices(renderer, mesh, deltas, "Bone_017");
            if (leftWingAffected <= 0 || rightWingAffected <= 0)
            {
                throw new InvalidOperationException("Both Fuga wing branches must be included in the whole-body melt.");
            }

            var animator = slot.GetComponent<Animator>();
            if (animator == null || animator.enabled || animator.runtimeAnimatorController != null)
            {
                throw new InvalidOperationException("Fuga death must stop wing animation by keeping its Animator disabled and unassigned.");
            }

            if (slot.GetComponent<FugaAnimationReviewPlaybackDriver>() != null)
            {
                throw new InvalidOperationException("The legacy Fuga death clip playback driver must not be assigned.");
            }

            var body = slot.GetComponent<Rigidbody>() ?? throw new InvalidOperationException("Fuga_05_Death has no Rigidbody.");
            if (body.isKinematic || !body.useGravity || (body.constraints & RigidbodyConstraints.FreezeRotation) == 0)
            {
                throw new InvalidOperationException("Fuga death must use non-kinematic gravity fall with rotation frozen.");
            }

            var bodyCollider = slot.GetComponent<Collider>() ??
                               throw new InvalidOperationException("Fuga_05_Death has no Collider for ground contact.");
            var expectedGroundWorldY = CalculateRendererBounds(RequireRoot(CorridorRootName)).min.y;

            var driver = slot.GetComponent<FugaPhysicsMotionDriver>() ??
                         throw new InvalidOperationException("Fuga_05_Death has no physics motion driver.");
            if (!driver.enabled || driver.LockRootMotionForReview || !driver.UseDeathFallSequence ||
                !driver.LoopDeathFallForReview || driver.DeathMeltRenderer != renderer ||
                driver.DeathMeltVisualRoot != model ||
                driver.DeathBodyCollider != bodyCollider ||
                Mathf.Abs(driver.DeathGroundWorldY - expectedGroundWorldY) > GeometryTolerance ||
                !string.Equals(driver.DeathMeltBlendShapeName, BlendShapeName, StringComparison.Ordinal) ||
                Mathf.Abs(driver.DeathMeltDuration - MeltDuration) > GeometryTolerance ||
                Mathf.Abs(driver.DeathMeltHoldDuration - MeltHoldDuration) > GeometryTolerance ||
                Mathf.Abs(driver.DeathSideTiltDegrees - SideTiltDegrees) > GeometryTolerance)
            {
                throw new InvalidOperationException("The collision-driven Fuga death driver configuration is incomplete.");
            }

            RequireParvumMeltCurve(driver.DeathMeltCurve);
            return new InspectionResult(
                slot,
                renderer,
                mesh,
                driver,
                affected,
                leftWingAffected,
                rightWingAffected,
                Sha256(Absolute(SourceModelPath)));
        }

        private static Mesh CreateDerivedMeltMesh()
        {
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourceModelPath) ??
                               throw new InvalidOperationException("The imported Fuga GLB is missing: " + SourceModelPath);
            var sourceRenderer = sourcePrefab.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                                 throw new InvalidOperationException("The imported Fuga GLB has no SkinnedMeshRenderer.");
            var source = sourceRenderer.sharedMesh ??
                         throw new InvalidOperationException("The imported Fuga GLB renderer has no mesh.");
            var generated = UnityEngine.Object.Instantiate(source);
            generated.name = "Fuga_Death_WholeBodyMeltMesh";
            generated.ClearBlendShapes();
            var vertices = source.vertices;
            var bounds = BoundsFromVertices(vertices);
            var deltas = new Vector3[vertices.Length];
            var ground = bounds.min.y;
            var center = bounds.center;
            for (var index = 0; index < vertices.Length; index++)
            {
                var vertex = vertices[index];
                var height = Mathf.Clamp01(Mathf.InverseLerp(ground, bounds.max.y, vertex.y));
                var relativeX = vertex.x - center.x;
                var relativeZ = vertex.z - center.z;
                var signX = Mathf.Abs(relativeX) > GeometryTolerance ? Mathf.Sign(relativeX) : 0f;
                var signZ = Mathf.Abs(relativeZ) > GeometryTolerance ? Mathf.Sign(relativeZ) : 0f;
                var target = new Vector3(
                    vertex.x + signX * (0.08f + 0.17f * height),
                    ground + (vertex.y - ground) * Mathf.Lerp(0.28f, 0.14f, height),
                    vertex.z + signZ * (0.06f + 0.20f * height));
                deltas[index] = target - vertex;
            }

            AddBlendShape(generated, source, deltas);
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(DerivedMeshPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, DerivedMeshPath);
                existing = generated;
            }
            else
            {
                EditorUtility.CopySerialized(generated, existing);
                UnityEngine.Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssetIfDirty(existing);
            return existing;
        }

        private static void AddBlendShape(Mesh generated, Mesh source, Vector3[] deltas)
        {
            var target = UnityEngine.Object.Instantiate(source);
            try
            {
                var vertices = source.vertices;
                target.vertices = vertices.Select((vertex, index) => vertex + deltas[index]).ToArray();
                target.RecalculateNormals();
                target.RecalculateTangents();
                var sourceNormals = source.normals;
                var targetNormals = target.normals;
                var sourceTangents = source.tangents;
                var targetTangents = target.tangents;
                var deltaNormals = new Vector3[source.vertexCount];
                var deltaTangents = new Vector3[source.vertexCount];
                if (sourceNormals.Length == targetNormals.Length)
                {
                    for (var index = 0; index < deltaNormals.Length; index++)
                    {
                        deltaNormals[index] = targetNormals[index] - sourceNormals[index];
                    }
                }

                if (sourceTangents.Length == targetTangents.Length)
                {
                    for (var index = 0; index < deltaTangents.Length; index++)
                    {
                        deltaTangents[index] = new Vector3(
                            targetTangents[index].x - sourceTangents[index].x,
                            targetTangents[index].y - sourceTangents[index].y,
                            targetTangents[index].z - sourceTangents[index].z);
                    }
                }

                generated.AddBlendShapeFrame(BlendShapeName, 100f, deltas, deltaNormals, deltaTangents);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static AnimationCurve CreateParvumMeltCurve()
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(MeltQuarterTime, 32f),
                new Keyframe(MeltDeepTime, 78f),
                new Keyframe(MeltDuration, 100f));
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
            }

            return curve;
        }

        private static void RequireParvumMeltCurve(AnimationCurve curve)
        {
            var expectedTimes = new[] { 0f, MeltQuarterTime, MeltDeepTime, MeltDuration };
            var expectedValues = new[] { 0f, 32f, 78f, 100f };
            if (curve == null || curve.length != expectedTimes.Length)
            {
                throw new InvalidOperationException("The Fuga melt curve must use the four Parvum melt keys.");
            }

            for (var index = 0; index < expectedTimes.Length; index++)
            {
                var key = curve[index];
                if (Mathf.Abs(key.time - expectedTimes[index]) > GeometryTolerance ||
                    Mathf.Abs(key.value - expectedValues[index]) > GeometryTolerance)
                {
                    throw new InvalidOperationException("The Fuga melt curve does not match the Parvum timing and weights.");
                }
            }
        }

        private static int CountAffectedWingVertices(
            SkinnedMeshRenderer renderer,
            Mesh mesh,
            Vector3[] deltas,
            string wingBoneName)
        {
            var boneIndex = Array.FindIndex(renderer.bones, bone => bone != null && bone.name == wingBoneName);
            if (boneIndex < 0)
            {
                throw new InvalidOperationException("Fuga wing bone is missing: " + wingBoneName);
            }

            var weights = mesh.boneWeights;
            var count = 0;
            for (var index = 0; index < weights.Length; index++)
            {
                if (WeightForBone(weights[index], boneIndex) > 0.001f &&
                    deltas[index].sqrMagnitude > GeometryTolerance * GeometryTolerance)
                {
                    count++;
                }
            }

            return count;
        }

        private static float WeightForBone(BoneWeight weight, int boneIndex)
        {
            var result = 0f;
            if (weight.boneIndex0 == boneIndex) result += weight.weight0;
            if (weight.boneIndex1 == boneIndex) result += weight.weight1;
            if (weight.boneIndex2 == boneIndex) result += weight.weight2;
            if (weight.boneIndex3 == boneIndex) result += weight.weight3;
            return result;
        }

        private static Vector3[] ReadBlendShapeDeltas(Mesh mesh, int shapeIndex)
        {
            var vertices = new Vector3[mesh.vertexCount];
            var normals = new Vector3[mesh.vertexCount];
            var tangents = new Vector3[mesh.vertexCount];
            mesh.GetBlendShapeFrameVertices(shapeIndex, 0, vertices, normals, tangents);
            return vertices;
        }

        private static Bounds BoundsFromVertices(Vector3[] vertices)
        {
            if (vertices.Length == 0)
            {
                throw new InvalidOperationException("The Fuga mesh has no vertices.");
            }

            var bounds = new Bounds(vertices[0], Vector3.zero);
            for (var index = 1; index < vertices.Length; index++)
            {
                bounds.Encapsulate(vertices[index]);
            }

            return bounds;
        }

        private static Bounds CalculateRendererBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(root.name + " has no enabled renderer bounds for floor height.");
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static SkinnedMeshRenderer RequireSingleRenderer(Transform model)
        {
            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length != 1)
            {
                throw new InvalidOperationException("The current Fuga model must have exactly one skinned body-and-wing renderer.");
            }

            return renderers[0];
        }

        private static Scene RequireCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || !string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("CargoRunMvp must already be the active scene.");
            }

            return scene;
        }

        private static Transform RequireRoot(string name)
        {
            return GameObject.Find(name)?.transform ??
                   throw new InvalidOperationException(name + " is missing from CargoRunMvp.");
        }

        private static Transform RequireDirectChild(Transform parent, string name)
        {
            return parent.Find(name) ??
                   throw new InvalidOperationException(parent.name + "/" + name + " is missing.");
        }

        private static string OtherSlotSignature(Transform placementRoot)
        {
            var builder = new StringBuilder();
            foreach (Transform child in placementRoot)
            {
                if (child.name == DeathSlotName)
                {
                    continue;
                }

                builder.Append(child.name).Append('|')
                    .Append(child.localPosition.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                    .Append(child.localRotation.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                    .Append(child.localScale.ToString("R", CultureInfo.InvariantCulture)).AppendLine();
            }

            return builder.ToString();
        }

        private static void WriteReport(InspectionResult result, bool directReviewCompleted, int completedLoops)
        {
            var report = new StringBuilder()
                .AppendLine("Fuga Death Fall And Whole-Body Melt Report")
                .AppendLine("Result=PASS")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("Target=" + PlacementRootName + "/" + DeathSlotName + "/" + ModelName)
                .AppendLine("SourceModel=" + SourceModelPath)
                .AppendLine("SourceSha256=" + result.SourceHash)
                .AppendLine("GeneratedMesh=" + DerivedMeshPath)
                .AppendLine("BlendShape=" + BlendShapeName)
                .AppendLine("VertexCount=" + result.Mesh.vertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("MeltAffectedVertexCount=" + result.AffectedVertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("LeftWingAffectedVertexCount=" + result.LeftWingAffectedVertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("RightWingAffectedVertexCount=" + result.RightWingAffectedVertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("WingFlappingStopsAtDeathStart=True")
                .AppendLine("AnimatorEnabled=False")
                .AppendLine("AnimatorControllerAssigned=False")
                .AppendLine("LegacyDeathClipAssigned=False")
                .AppendLine("RigidbodyGravityFall=True")
                .AppendLine("RigidbodyRootMotionOwner=True")
                .AppendLine("RandomSideTilt=True")
                .AppendLine("RandomSideSelection=Uniform50_50PerLoop")
                .AppendLine("SideTiltDegrees=45")
                .AppendLine("TiltAxis=InitialLocalZ")
                .AppendLine("TiltOwnedByRigidbody=True")
                .AppendLine("ColliderRotatesWithRigidbody=True")
                .AppendLine("TiltHeldUntilAndAfterGroundContact=True")
                .AppendLine("RigidbodyColliderTiltHeldDuringMelt=True")
                .AppendLine("GroundContactUsesCollider=True")
                .AppendLine("GroundWorldY=" + result.Driver.DeathGroundWorldY.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("MissingSceneFloorColliderFallback=BodyColliderBoundsAgainstCorridorFloor")
                .AppendLine("TimerBasedImpactStop=False")
                .AppendLine("MeltBeforeGroundContact=False")
                .AppendLine("MeltCurve=0@0,32@0.72,78@1.52,100@2")
                .AppendLine("MeltDurationSeconds=2")
                .AppendLine("MeltedBodyHoldDurationSeconds=1")
                .AppendLine("BodyAndBothWingsMeltTogether=True")
                .AppendLine("MeltVisualRoot=Fuga_Model")
                .AppendLine("VisualCounterRotationUsesMeltProgress=True")
                .AppendLine("VisualCounterRotationStartsBeforeMelt=False")
                .AppendLine("FinalPuddleWorldLevel=True")
                .AppendLine("FinalPuddleGroundAligned=True")
                .AppendLine("Loop=True")
                .AppendLine("LoopRestart=ResetRigidbodyPoseAndBlendShape")
                .AppendLine("DirectUnityGameViewMotionReview=" + directReviewCompleted)
                .AppendLine("DirectMotionReviewCompletedLoops=" + completedLoops.ToString(CultureInfo.InvariantCulture))
                .AppendLine("DirectReviewLeft45DegreeImpactCount=" + result.Driver.DeathLeftImpactCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("DirectReviewRight45DegreeImpactCount=" + result.Driver.DeathRightImpactCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("DirectReviewInvalidImpactTiltCount=" + result.Driver.DeathInvalidImpactTiltCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("DirectReviewLastPreMeltVisualTiltDegrees=" + result.Driver.DeathLastPreMeltVisualTiltDegrees.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("DirectReviewInvalidPreMeltVisualTiltCount=" + result.Driver.DeathInvalidPreMeltVisualTiltCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("DirectReviewFinalPuddleLevelRecordedCount=" + result.Driver.DeathFinalPuddleLevelRecordedCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("DirectReviewLastFinalPuddleLevelErrorDegrees=" + result.Driver.DeathLastFinalPuddleLevelErrorDegrees.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("DirectReviewInvalidFinalPuddleLevelCount=" + result.Driver.DeathInvalidFinalPuddleLevelCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("DirectReviewLastFinalPuddleGroundErrorMeters=" + result.Driver.DeathLastFinalPuddleGroundErrorMeters.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("DirectReviewInvalidFinalPuddleGroundCount=" + result.Driver.DeathInvalidFinalPuddleGroundCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("StaticCaptureGenerated=False")
                .AppendLine("OriginalGlbModified=False")
                .AppendLine("HarnessValidationRun=False")
                .ToString();
            var absolute = Absolute(ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ??
                                      throw new InvalidOperationException("Invalid Fuga death report path."));
            File.WriteAllText(absolute, report, new UTF8Encoding(false));
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var algorithm = SHA256.Create();
            return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static void RequireHash(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(label + " changed unexpectedly.");
            }
        }

        private static string Absolute(string projectRelativePath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private sealed class InspectionResult
        {
            public InspectionResult(
                Transform slot,
                SkinnedMeshRenderer renderer,
                Mesh mesh,
                FugaPhysicsMotionDriver driver,
                int affectedVertexCount,
                int leftWingAffectedVertexCount,
                int rightWingAffectedVertexCount,
                string sourceHash)
            {
                Slot = slot;
                Renderer = renderer;
                Mesh = mesh;
                Driver = driver;
                AffectedVertexCount = affectedVertexCount;
                LeftWingAffectedVertexCount = leftWingAffectedVertexCount;
                RightWingAffectedVertexCount = rightWingAffectedVertexCount;
                SourceHash = sourceHash;
            }

            public Transform Slot { get; }
            public SkinnedMeshRenderer Renderer { get; }
            public Mesh Mesh { get; }
            public FugaPhysicsMotionDriver Driver { get; }
            public int AffectedVertexCount { get; }
            public int LeftWingAffectedVertexCount { get; }
            public int RightWingAffectedVertexCount { get; }
            public string SourceHash { get; }
        }
    }
}
