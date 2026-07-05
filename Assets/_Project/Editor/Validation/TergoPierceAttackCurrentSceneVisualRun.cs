using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.TergoCargoRunScene
{
    [InitializeOnLoad]
    internal static class TergoPierceAttackCurrentSceneVisualRun
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Tergo Enemy Placement";
        private const string PierceAttackRootName = "Tergo_05_Pierce_Attack";
        private const string StateFileName = "TergoPierceAttackCurrentSceneVisualRun.state";
        private const string ResultFileName = "TergoPierceAttackCurrentSceneVisualRun.result";
        private const string StateEnteringPlayMode = "EnteringPlayMode";
        private const string StateExitingPlayMode = "ExitingPlayMode";
        private const string StateFailedExitingPlayMode = "FailedExitingPlayMode";
        private const int CaptureWidth = 1280;
        private const int CaptureHeight = 720;
        private const bool IncludeDebugMarkers = false;
        private const bool IncludeRotationCandidateCaptures = false;
        private static readonly string[] ArmBonePaths =
        {
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder",
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm",
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm",
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand",
            "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder",
            "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm",
            "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm",
            "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm/LeftHand"
        };

        private static Action<string> complete;
        private static Action<Exception> fail;

        static TergoPierceAttackCurrentSceneVisualRun()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        public static void Start(Action<string> completeCallback, Action<Exception> failCallback)
        {
            complete = completeCallback;
            fail = failCallback;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Cannot start Tergo visual run while Unity is already entering or running Play Mode.");
            }

            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.path != CargoRunScenePath)
            {
                throw new InvalidOperationException(
                    "Current active scene must be CargoRunMvp. ActiveScene=" + activeScene.path);
            }

            DeleteStateFiles();
            TergoRunChaseAnimation.ApplyTergoPierceAttackStraightPunchCurrentSceneOnly();

            var captureDir = Path.Combine(
                ProjectRoot,
                "Logs",
                "TergoPierceAttackCurrentSceneVisualRun_20260705");
            if (Directory.Exists(captureDir))
            {
                foreach (var file in Directory.GetFiles(captureDir, "*.png"))
                {
                    File.Delete(file);
                }
            }

            Directory.CreateDirectory(captureDir);
            WriteState(new VisualRunState
            {
                Phase = StateEnteringPlayMode,
                CaptureDir = captureDir,
                StartedUtcTicks = DateTime.UtcNow.Ticks
            });

            EditorApplication.EnterPlaymode();
        }

        public static void Resume(Action<string> completeCallback, Action<Exception> failCallback)
        {
            complete = completeCallback;
            fail = failCallback;
            Tick();
        }

        private static void Tick()
        {
            if (complete == null && fail == null)
            {
                return;
            }

            var state = ReadState();
            if (state == null)
            {
                return;
            }

            try
            {
                if (state.Phase == StateEnteringPlayMode)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        return;
                    }

                    CaptureCurrentPlayModeFrames(state);
                    state.Phase = StateExitingPlayMode;
                    WriteState(state);
                    EditorApplication.ExitPlaymode();
                    return;
                }

                if (state.Phase == StateExitingPlayMode)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        return;
                    }

                    CompleteFromState(state);
                    return;
                }

                if (state.Phase == StateFailedExitingPlayMode)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        return;
                    }

                    FailFromState(state);
                }
            }
            catch (Exception exception)
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    state.Phase = StateFailedExitingPlayMode;
                    state.Error = exception.ToString();
                    WriteState(state);
                    if (EditorApplication.isPlaying)
                    {
                        EditorApplication.ExitPlaymode();
                    }
                }
                else
                {
                    var callback = fail;
                    CleanupCallbacks();
                    DeleteStateFiles();
                    callback?.Invoke(exception);
                }
            }
        }

        private static void CaptureCurrentPlayModeFrames(VisualRunState state)
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.path != CargoRunScenePath)
            {
                throw new InvalidOperationException(
                    "Play Mode active scene must stay CargoRunMvp. ActiveScene=" + activeScene.path);
            }

            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException("Missing placement root in Play Mode: " + PlacementRootName);
            }

            var pierceAttackRoot = placementRoot.transform.Find(PierceAttackRootName);
            if (pierceAttackRoot == null)
            {
                throw new InvalidOperationException("Missing Tergo pierce attack root in Play Mode: " + PierceAttackRootName);
            }

            var siblingStates = HideOtherPlacementChildrenForCapture(placementRoot.transform, pierceAttackRoot);
            var animator = pierceAttackRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                throw new InvalidOperationException(PierceAttackRootName + " must have a configured Animator during Play Mode visual run.");
            }

            Debug.Log("TergoPierceAttackSkinWeightSummary, " + BuildSkinWeightSummary(pierceAttackRoot));
            Debug.Log("TergoPierceAttackTopBoneWeightSummary, " + BuildTopBoneWeightSummary(pierceAttackRoot));
            Debug.Log("TergoPierceAttackBoneReferenceSummary, " + BuildBoneReferenceSummary(pierceAttackRoot));
            Debug.Log("TergoPierceAttackRendererSummary, " + BuildRendererSummary(pierceAttackRoot));

            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = false;
            animator.speed = 1f;
            Debug.Log("TergoPierceAttackPositionSensitivity, " + BuildPositionSensitivitySummary(pierceAttackRoot, animator));

            var cameraObject = new GameObject("Tergo Pierce Attack Visual Run Camera");
            cameraObject.hideFlags = HideFlags.DontSave;
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.06f, 0.065f, 1f);
            camera.fieldOfView = 34f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            var lightObject = new GameObject("Tergo Pierce Attack Visual Run Light");
            lightObject.hideFlags = HideFlags.DontSave;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2.8f;
            light.color = new Color(1f, 0.96f, 0.9f, 1f);
            lightObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
            var markerRoot = IncludeDebugMarkers ? CreateDebugMarkerRoot() : null;

            var captures = new List<string>();
            var metrics = new VisualPoseMetrics();
            try
            {
                CapturePose(
                    camera,
                    pierceAttackRoot,
                    animator,
                    0f,
                    "00_ready",
                    state.CaptureDir,
                    captures,
                    markerRoot,
                    ref metrics);
                CapturePose(
                    camera,
                    pierceAttackRoot,
                    animator,
                    0.38f / 1.2f,
                    "01_windup",
                    state.CaptureDir,
                    captures,
                    markerRoot,
                    ref metrics);
                CapturePose(
                    camera,
                    pierceAttackRoot,
                    animator,
                    0.82f / 1.2f,
                    "02_thrust",
                    state.CaptureDir,
                    captures,
                    markerRoot,
                    ref metrics);
                CapturePose(
                    camera,
                    pierceAttackRoot,
                    animator,
                    1.02f / 1.2f,
                    "03_hold",
                    state.CaptureDir,
                    captures,
                    markerRoot,
                    ref metrics);
                if (IncludeRotationCandidateCaptures)
                {
                    CaptureRotationCandidatePoses(
                        camera,
                        pierceAttackRoot,
                        animator,
                        state.CaptureDir,
                        captures,
                        markerRoot);
                }
            }
            finally
            {
                RestorePlacementChildrenAfterCapture(siblingStates);
                if (markerRoot != null)
                {
                    UnityEngine.Object.Destroy(markerRoot);
                }

                UnityEngine.Object.Destroy(lightObject);
                UnityEngine.Object.Destroy(cameraObject);
            }

            state.ImagePaths = string.Join("|", captures);
            state.Summary =
                "CaptureDir=" + state.CaptureDir +
                ", Images=" + state.ImagePaths +
                ", TargetIsolatedInPlayMode=True" +
                ", WindupHandLocal=" + FormatVector3(metrics.WindupRightHandLocal) +
                ", ThrustHandLocal=" + FormatVector3(metrics.ThrustRightHandLocal) +
                ", HoldHandLocal=" + FormatVector3(metrics.HoldRightHandLocal) +
                ", ThrustMinusWindup=" + FormatVector3(metrics.ThrustRightHandLocal - metrics.WindupRightHandLocal) +
                ", HoldMinusThrust=" + FormatVector3(metrics.HoldRightHandLocal - metrics.ThrustRightHandLocal) +
                ", ThrustElbowAngle=" + metrics.ThrustElbowAngle.ToString("0.###", CultureInfo.InvariantCulture);
            File.WriteAllText(ResultPath, state.Summary);

            Debug.Log(
                "TergoPierceAttackCurrentSceneVisualFramesCaptured, " +
                state.Summary);
        }

        private static SiblingActiveState[] HideOtherPlacementChildrenForCapture(Transform placementRoot, Transform target)
        {
            var states = new List<SiblingActiveState>();
            for (var index = 0; index < placementRoot.childCount; index++)
            {
                var child = placementRoot.GetChild(index);
                if (child == target)
                {
                    continue;
                }

                states.Add(new SiblingActiveState(child.gameObject, child.gameObject.activeSelf));
                child.gameObject.SetActive(false);
            }

            return states.ToArray();
        }

        private static void RestorePlacementChildrenAfterCapture(SiblingActiveState[] states)
        {
            foreach (var state in states)
            {
                if (state.GameObject != null)
                {
                    state.GameObject.SetActive(state.ActiveSelf);
                }
            }
        }

        private static void CapturePose(
            Camera camera,
            Transform target,
            Animator animator,
            float normalizedTime,
            string label,
            string captureDir,
            List<string> captures,
            GameObject markerRoot,
            ref VisualPoseMetrics metrics)
        {
            animator.speed = 1f;
            animator.Play("Tergo_Pierce_Attack", 0, normalizedTime);
            animator.Update(0f);
            animator.speed = 0f;

            RecordPoseMetrics(target, label, ref metrics);
            if (label == "02_thrust")
            {
                Debug.Log("TergoPierceAttackWeightedRegionSummary_" + label + ", " + BuildWeightedRegionSummary(target));
            }

            UpdateDebugMarkers(target, markerRoot);
            CaptureView(camera, target, label + "_front3q", captureDir, captures, -target.forward + target.right * 0.65f);
            CaptureView(camera, target, label + "_rightSide", captureDir, captures, target.right);
            CaptureView(camera, target, label + "_leftSide", captureDir, captures, -target.right);
            CaptureView(camera, target, label + "_rear3q", captureDir, captures, target.forward + target.right * 0.65f);
        }

        private static void CaptureRotationCandidatePoses(
            Camera camera,
            Transform target,
            Animator animator,
            string captureDir,
            List<string> captures,
            GameObject markerRoot)
        {
            var candidates = new[]
            {
                new RotationCandidate("candidate_arm_x90", new Vector3(90f, 0f, 0f), Vector3.zero, Vector3.zero),
                new RotationCandidate("candidate_arm_x-90", new Vector3(-90f, 0f, 0f), Vector3.zero, Vector3.zero),
                new RotationCandidate("candidate_arm_y90", new Vector3(0f, 90f, 0f), Vector3.zero, Vector3.zero),
                new RotationCandidate("candidate_arm_y-90", new Vector3(0f, -90f, 0f), Vector3.zero, Vector3.zero),
                new RotationCandidate("candidate_arm_z90", new Vector3(0f, 0f, 90f), Vector3.zero, Vector3.zero),
                new RotationCandidate("candidate_arm_z-90", new Vector3(0f, 0f, -90f), Vector3.zero, Vector3.zero),
                new RotationCandidate("candidate_forearm_x90", Vector3.zero, new Vector3(90f, 0f, 0f), Vector3.zero),
                new RotationCandidate("candidate_forearm_x-90", Vector3.zero, new Vector3(-90f, 0f, 0f), Vector3.zero),
                new RotationCandidate("candidate_forearm_y90", Vector3.zero, new Vector3(0f, 90f, 0f), Vector3.zero),
                new RotationCandidate("candidate_forearm_y-90", Vector3.zero, new Vector3(0f, -90f, 0f), Vector3.zero),
                new RotationCandidate("candidate_forearm_z90", Vector3.zero, new Vector3(0f, 0f, 90f), Vector3.zero),
                new RotationCandidate("candidate_forearm_z-90", Vector3.zero, new Vector3(0f, 0f, -90f), Vector3.zero),
                new RotationCandidate("candidate_hand_x90", Vector3.zero, Vector3.zero, new Vector3(90f, 0f, 0f)),
                new RotationCandidate("candidate_hand_x-90", Vector3.zero, Vector3.zero, new Vector3(-90f, 0f, 0f)),
                new RotationCandidate("candidate_hand_y90", Vector3.zero, Vector3.zero, new Vector3(0f, 90f, 0f)),
                new RotationCandidate("candidate_hand_y-90", Vector3.zero, Vector3.zero, new Vector3(0f, -90f, 0f)),
                new RotationCandidate("candidate_hand_z90", Vector3.zero, Vector3.zero, new Vector3(0f, 0f, 90f)),
                new RotationCandidate("candidate_hand_z-90", Vector3.zero, Vector3.zero, new Vector3(0f, 0f, -90f)),
                new RotationCandidate("candidate_hand_x180", Vector3.zero, Vector3.zero, new Vector3(180f, 0f, 0f)),
                new RotationCandidate("candidate_hand_y180", Vector3.zero, Vector3.zero, new Vector3(0f, 180f, 0f)),
                new RotationCandidate("candidate_hand_z180", Vector3.zero, Vector3.zero, new Vector3(0f, 0f, 180f)),
                new RotationCandidate("candidate_arm_y-45_forearm_x90", new Vector3(0f, -45f, 0f), new Vector3(90f, 0f, 0f), Vector3.zero),
                new RotationCandidate("candidate_arm_y45_forearm_x-90", new Vector3(0f, 45f, 0f), new Vector3(-90f, 0f, 0f), Vector3.zero)
            };

            var rightArm = RequireChild(target, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm");
            var rightForeArm = RequireChild(target, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm");
            var rightHand = RequireChild(target, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand");

            foreach (var candidate in candidates)
            {
                animator.speed = 1f;
                animator.Play("Tergo_Pierce_Attack", 0, 0.82f / 1.2f);
                animator.Update(0f);
                animator.speed = 0f;

                rightArm.localRotation *= Quaternion.Euler(candidate.RightArmEuler);
                rightForeArm.localRotation *= Quaternion.Euler(candidate.RightForeArmEuler);
                rightHand.localRotation *= Quaternion.Euler(candidate.RightHandEuler);
                UpdateDebugMarkers(target, markerRoot);
                CaptureView(camera, target, candidate.Name + "_front3q", captureDir, captures, -target.forward + target.right * 0.65f);
            }
        }

        private static GameObject CreateDebugMarkerRoot()
        {
            var root = new GameObject("Tergo Pierce Attack Visual Bone Markers");
            root.hideFlags = HideFlags.DontSave;
            CreateMarker(root.transform, "RightHandMarker", Color.red);
            CreateMarker(root.transform, "RightForeArmMarker", Color.yellow);
            CreateMarker(root.transform, "RightArmMarker", new Color(1f, 0.45f, 0f, 1f));
            CreateMarker(root.transform, "LeftHandMarker", Color.cyan);
            CreateMarker(root.transform, "LeftForeArmMarker", Color.blue);
            CreateMarker(root.transform, "LeftArmMarker", new Color(0.45f, 0f, 1f, 1f));
            return root;
        }

        private static void CreateMarker(Transform root, string markerName, Color color)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = markerName;
            marker.hideFlags = HideFlags.DontSave;
            marker.transform.SetParent(root, false);
            marker.transform.localScale = Vector3.one * 0.085f;
            var collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.Destroy(collider);
            }

            var renderer = marker.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateMarkerMaterial(color);
        }

        private static Material CreateMarkerMaterial(Color color)
        {
            var shader = Shader.Find("Unlit/Color");
            var material = new Material(shader != null ? shader : Shader.Find("Universal Render Pipeline/Lit"));
            material.hideFlags = HideFlags.DontSave;
            material.color = color;
            return material;
        }

        private static void UpdateDebugMarkers(Transform target, GameObject markerRoot)
        {
            if (markerRoot == null)
            {
                return;
            }

            SetMarkerPosition(
                markerRoot,
                "RightHandMarker",
                RequireChild(target, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand").position);
            SetMarkerPosition(
                markerRoot,
                "RightForeArmMarker",
                RequireChild(target, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm").position);
            SetMarkerPosition(
                markerRoot,
                "RightArmMarker",
                RequireChild(target, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm").position);
            SetMarkerPosition(
                markerRoot,
                "LeftHandMarker",
                RequireChild(target, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm/LeftHand").position);
            SetMarkerPosition(
                markerRoot,
                "LeftForeArmMarker",
                RequireChild(target, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm").position);
            SetMarkerPosition(
                markerRoot,
                "LeftArmMarker",
                RequireChild(target, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm").position);
        }

        private static void SetMarkerPosition(GameObject markerRoot, string markerName, Vector3 position)
        {
            var marker = markerRoot.transform.Find(markerName);
            if (marker != null)
            {
                marker.position = position;
            }
        }

        private static void RecordPoseMetrics(Transform target, string label, ref VisualPoseMetrics metrics)
        {
            var rightArm = RequireChild(target, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm");
            var rightForeArm = RequireChild(target, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm");
            var rightHand = RequireChild(target, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand");
            var handLocal = target.InverseTransformPoint(rightHand.position);

            if (label == "01_windup")
            {
                metrics.WindupRightHandLocal = handLocal;
            }
            else if (label == "02_thrust")
            {
                metrics.ThrustRightHandLocal = handLocal;
                metrics.ThrustElbowAngle = Vector3.Angle(
                    rightArm.position - rightForeArm.position,
                    rightHand.position - rightForeArm.position);
            }
            else if (label == "03_hold")
            {
                metrics.HoldRightHandLocal = handLocal;
            }
        }

        private static void CaptureView(
            Camera camera,
            Transform target,
            string label,
            string captureDir,
            List<string> captures,
            Vector3 viewDirection)
        {
            var bakedStates = CreateBakedSkinnedRendererPreview(target);
            try
            {
                var bounds = CalculateBounds(target);
                var size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z, 0.5f);
                var center = bounds.center + Vector3.up * (size * 0.08f);
                var direction = viewDirection.sqrMagnitude > 0.001f ? viewDirection.normalized : -target.forward;

                camera.transform.position = center + direction * (size * 2.75f) + Vector3.up * (size * 0.28f);
                camera.transform.LookAt(center);

                var renderTexture = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
                var previousActive = RenderTexture.active;
                try
                {
                    camera.targetTexture = renderTexture;
                    RenderTexture.active = renderTexture;
                    camera.Render();

                    var texture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
                    texture.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
                    texture.Apply();

                    var path = Path.Combine(captureDir, label + ".png");
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                    captures.Add(path);
                    UnityEngine.Object.DestroyImmediate(texture);
                }
                finally
                {
                    camera.targetTexture = null;
                    RenderTexture.active = previousActive;
                    renderTexture.Release();
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                }
            }
            finally
            {
                RestoreBakedSkinnedRendererPreview(bakedStates);
            }
        }

        private static BakedRendererState[] CreateBakedSkinnedRendererPreview(Transform target)
        {
            var states = new List<BakedRendererState>();
            foreach (var renderer in target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var bakedMesh = new Mesh
                {
                    name = renderer.name + "_BakedPose"
                };
                renderer.BakeMesh(bakedMesh);

                var preview = new GameObject(renderer.name + "_BakedPosePreview");
                preview.hideFlags = HideFlags.DontSave;
                preview.transform.SetParent(renderer.transform.parent, false);
                preview.transform.localPosition = renderer.transform.localPosition;
                preview.transform.localRotation = renderer.transform.localRotation;
                preview.transform.localScale = renderer.transform.localScale;

                var filter = preview.AddComponent<MeshFilter>();
                filter.sharedMesh = bakedMesh;
                var meshRenderer = preview.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterials = renderer.sharedMaterials;
                meshRenderer.shadowCastingMode = renderer.shadowCastingMode;
                meshRenderer.receiveShadows = renderer.receiveShadows;

                states.Add(new BakedRendererState(renderer, preview, bakedMesh, renderer.enabled));
                renderer.enabled = false;
            }

            return states.ToArray();
        }

        private static void RestoreBakedSkinnedRendererPreview(BakedRendererState[] states)
        {
            foreach (var state in states)
            {
                if (state.Renderer != null)
                {
                    state.Renderer.enabled = state.WasEnabled;
                }

                if (state.PreviewObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(state.PreviewObject);
                }

                if (state.Mesh != null)
                {
                    UnityEngine.Object.DestroyImmediate(state.Mesh);
                }
            }
        }

        private static Bounds CalculateBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                return new Bounds(root.position, Vector3.one);
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static string BuildSkinWeightSummary(Transform target)
        {
            var summaries = new List<string>();
            foreach (var renderer in target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = renderer.sharedMesh;
                if (mesh == null || renderer.bones == null || renderer.bones.Length == 0)
                {
                    continue;
                }

                var totals = new float[renderer.bones.Length];
                var counts = new int[renderer.bones.Length];
                foreach (var weight in mesh.boneWeights)
                {
                    AddWeight(totals, counts, weight.boneIndex0, weight.weight0);
                    AddWeight(totals, counts, weight.boneIndex1, weight.weight1);
                    AddWeight(totals, counts, weight.boneIndex2, weight.weight2);
                    AddWeight(totals, counts, weight.boneIndex3, weight.weight3);
                }

                var armSummaries = renderer.bones
                    .Select((bone, index) => new
                    {
                        Bone = bone,
                        Index = index,
                        Total = totals[index],
                        Count = counts[index]
                    })
                    .Where(item =>
                        item.Bone != null &&
                        (item.Bone.name.IndexOf("Arm", StringComparison.Ordinal) >= 0 ||
                         item.Bone.name.IndexOf("Hand", StringComparison.Ordinal) >= 0 ||
                         item.Bone.name.IndexOf("Shoulder", StringComparison.Ordinal) >= 0))
                    .OrderByDescending(item => item.Total)
                    .Select(item =>
                        item.Bone.name +
                        ":Weight=" + item.Total.ToString("0.###", CultureInfo.InvariantCulture) +
                        ":Verts=" + item.Count.ToString(CultureInfo.InvariantCulture))
                    .ToArray();

                summaries.Add(renderer.name + "[" + string.Join("|", armSummaries) + "]");
            }

            return summaries.Count == 0 ? "NoSkinnedMeshRenderer" : string.Join(";", summaries);
        }

        private static string BuildRendererSummary(Transform target)
        {
            var renderers = target.GetComponentsInChildren<Renderer>(true)
                .Select(renderer =>
                    renderer.name +
                    ":Type=" + renderer.GetType().Name +
                    ":Enabled=" + renderer.enabled +
                    ":Active=" + renderer.gameObject.activeInHierarchy +
                    ":Path=" + GetRelativePath(target, renderer.transform))
                .ToArray();
            return renderers.Length == 0 ? "NoRenderer" : string.Join("|", renderers);
        }

        private static string BuildTopBoneWeightSummary(Transform target)
        {
            var summaries = new List<string>();
            foreach (var renderer in target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = renderer.sharedMesh;
                if (mesh == null || renderer.bones == null || renderer.bones.Length == 0)
                {
                    continue;
                }

                var totals = new float[renderer.bones.Length];
                var counts = new int[renderer.bones.Length];
                foreach (var weight in mesh.boneWeights)
                {
                    AddWeight(totals, counts, weight.boneIndex0, weight.weight0);
                    AddWeight(totals, counts, weight.boneIndex1, weight.weight1);
                    AddWeight(totals, counts, weight.boneIndex2, weight.weight2);
                    AddWeight(totals, counts, weight.boneIndex3, weight.weight3);
                }

                var topSummaries = renderer.bones
                    .Select((bone, index) => new
                    {
                        Bone = bone,
                        Index = index,
                        Total = totals[index],
                        Count = counts[index]
                    })
                    .Where(item => item.Bone != null && item.Total > 0.001f)
                    .OrderByDescending(item => item.Total)
                    .Take(32)
                    .Select(item =>
                        item.Bone.name +
                        ":Weight=" + item.Total.ToString("0.###", CultureInfo.InvariantCulture) +
                        ":Verts=" + item.Count.ToString(CultureInfo.InvariantCulture) +
                        ":Path=" + GetRelativePath(target, item.Bone))
                    .ToArray();

                summaries.Add(renderer.name + "[" + string.Join("|", topSummaries) + "]");
            }

            return summaries.Count == 0 ? "NoSkinnedMeshRenderer" : string.Join(";", summaries);
        }

        private static string BuildPositionSensitivitySummary(Transform target, Animator animator)
        {
            animator.speed = 1f;
            animator.Play("Tergo_Pierce_Attack", 0, 0.82f / 1.2f);
            animator.Update(0f);
            animator.speed = 0f;

            var transforms = target.GetComponentsInChildren<Transform>(true);
            var positions = transforms.Select(transform => transform.localPosition).ToArray();
            var rotations = transforms.Select(transform => transform.localRotation).ToArray();
            var scales = transforms.Select(transform => transform.localScale).ToArray();
            var rightArm = RequireChild(target, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm");
            var rightForeArm = RequireChild(target, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm");
            var rightHand = RequireChild(target, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand");
            var baseHandLocal = target.InverseTransformPoint(rightHand.position);
            var probes = new[]
            {
                new PositionProbe("RightArm+x", rightArm, new Vector3(0.2f, 0f, 0f)),
                new PositionProbe("RightArm-x", rightArm, new Vector3(-0.2f, 0f, 0f)),
                new PositionProbe("RightArm+y", rightArm, new Vector3(0f, 0.2f, 0f)),
                new PositionProbe("RightArm-y", rightArm, new Vector3(0f, -0.2f, 0f)),
                new PositionProbe("RightArm+z", rightArm, new Vector3(0f, 0f, 0.2f)),
                new PositionProbe("RightArm-z", rightArm, new Vector3(0f, 0f, -0.2f)),
                new PositionProbe("RightForeArm+x", rightForeArm, new Vector3(0.2f, 0f, 0f)),
                new PositionProbe("RightForeArm-x", rightForeArm, new Vector3(-0.2f, 0f, 0f)),
                new PositionProbe("RightForeArm+y", rightForeArm, new Vector3(0f, 0.2f, 0f)),
                new PositionProbe("RightForeArm-y", rightForeArm, new Vector3(0f, -0.2f, 0f)),
                new PositionProbe("RightForeArm+z", rightForeArm, new Vector3(0f, 0f, 0.2f)),
                new PositionProbe("RightForeArm-z", rightForeArm, new Vector3(0f, 0f, -0.2f)),
                new PositionProbe("RightHand+x", rightHand, new Vector3(0.2f, 0f, 0f)),
                new PositionProbe("RightHand-x", rightHand, new Vector3(-0.2f, 0f, 0f)),
                new PositionProbe("RightHand+y", rightHand, new Vector3(0f, 0.2f, 0f)),
                new PositionProbe("RightHand-y", rightHand, new Vector3(0f, -0.2f, 0f)),
                new PositionProbe("RightHand+z", rightHand, new Vector3(0f, 0f, 0.2f)),
                new PositionProbe("RightHand-z", rightHand, new Vector3(0f, 0f, -0.2f))
            };

            try
            {
                var summaries = new List<string>
                {
                    "Base=" + FormatVector3(baseHandLocal)
                };

                foreach (var probe in probes)
                {
                    RestoreTransforms(transforms, positions, rotations, scales);
                    probe.Bone.localPosition += probe.Offset;
                    var handLocal = target.InverseTransformPoint(rightHand.position);
                    summaries.Add(probe.Name + "=" + FormatVector3(handLocal - baseHandLocal));
                }

                return string.Join("|", summaries);
            }
            finally
            {
                RestoreTransforms(transforms, positions, rotations, scales);
            }
        }

        private static void RestoreTransforms(
            Transform[] transforms,
            Vector3[] positions,
            Quaternion[] rotations,
            Vector3[] scales)
        {
            for (var index = 0; index < transforms.Length; index++)
            {
                transforms[index].localPosition = positions[index];
                transforms[index].localRotation = rotations[index];
                transforms[index].localScale = scales[index];
            }
        }

        private static string BuildBoneReferenceSummary(Transform target)
        {
            var summaries = new List<string>();
            foreach (var renderer in target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.bones == null || renderer.bones.Length == 0)
                {
                    summaries.Add(renderer.name + "[NoBones]");
                    continue;
                }

                var rendererSummary = new List<string>();
                foreach (var path in ArmBonePaths)
                {
                    var targetBone = RequireChild(target, path);
                    var matches = renderer.bones
                        .Where(bone => bone != null && bone.name == targetBone.name)
                        .Select(bone =>
                            (ReferenceEquals(bone, targetBone) ? "same:" : "diff:") +
                            GetRelativePath(target, bone))
                        .ToArray();
                    rendererSummary.Add(targetBone.name + "=" + (matches.Length == 0 ? "missing" : string.Join(",", matches)));
                }

                summaries.Add(renderer.name + "[" + string.Join("|", rendererSummary) + "]");
            }

            return summaries.Count == 0 ? "NoSkinnedMeshRenderer" : string.Join(";", summaries);
        }

        private static string BuildWeightedRegionSummary(Transform target)
        {
            var summaries = new List<string>();
            foreach (var renderer in target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = renderer.sharedMesh;
                if (mesh == null || renderer.bones == null || renderer.bones.Length == 0)
                {
                    continue;
                }

                var bakedMesh = new Mesh();
                try
                {
                    renderer.BakeMesh(bakedMesh);
                    var vertices = bakedMesh.vertices;
                    var weights = mesh.boneWeights;
                    var rendererSummary = new List<string>();

                    foreach (var path in ArmBonePaths)
                    {
                        var targetBone = RequireChild(target, path);
                        var boneIndex = Array.FindIndex(renderer.bones, bone => bone == targetBone);
                        if (boneIndex < 0)
                        {
                            rendererSummary.Add(targetBone.name + "=missing");
                            continue;
                        }

                        var weightedSum = Vector3.zero;
                        var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
                        var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
                        var totalWeight = 0f;
                        var count = 0;
                        var vertexCount = Mathf.Min(vertices.Length, weights.Length);
                        for (var index = 0; index < vertexCount; index++)
                        {
                            var weight = GetBoneWeight(weights[index], boneIndex);
                            if (weight <= 0.001f)
                            {
                                continue;
                            }

                            weightedSum += vertices[index] * weight;
                            totalWeight += weight;
                            count++;
                            var targetLocalVertex = target.InverseTransformPoint(renderer.transform.TransformPoint(vertices[index]));
                            min = Vector3.Min(min, targetLocalVertex);
                            max = Vector3.Max(max, targetLocalVertex);
                        }

                        if (totalWeight <= 0.001f)
                        {
                            rendererSummary.Add(targetBone.name + "=noWeightedVerts");
                            continue;
                        }

                        var worldCenter = renderer.transform.TransformPoint(weightedSum / totalWeight);
                        var targetLocalCenter = target.InverseTransformPoint(worldCenter);
                        rendererSummary.Add(
                            targetBone.name +
                            ":Count=" + count.ToString(CultureInfo.InvariantCulture) +
                            ":Weight=" + totalWeight.ToString("0.###", CultureInfo.InvariantCulture) +
                            ":Center=" + FormatVector3(targetLocalCenter) +
                            ":Min=" + FormatVector3(min) +
                            ":Max=" + FormatVector3(max));
                    }

                    summaries.Add(renderer.name + "[" + string.Join("|", rendererSummary) + "]");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(bakedMesh);
                }
            }

            return summaries.Count == 0 ? "NoSkinnedMeshRenderer" : string.Join(";", summaries);
        }

        private static float GetBoneWeight(BoneWeight weight, int boneIndex)
        {
            var total = 0f;
            if (weight.boneIndex0 == boneIndex)
            {
                total += weight.weight0;
            }

            if (weight.boneIndex1 == boneIndex)
            {
                total += weight.weight1;
            }

            if (weight.boneIndex2 == boneIndex)
            {
                total += weight.weight2;
            }

            if (weight.boneIndex3 == boneIndex)
            {
                total += weight.weight3;
            }

            return total;
        }

        private static void AddWeight(float[] totals, int[] counts, int index, float weight)
        {
            if (index < 0 || index >= totals.Length || weight <= 0f)
            {
                return;
            }

            totals[index] += weight;
            counts[index]++;
        }

        private static Transform RequireChild(Transform root, string path)
        {
            var child = root.Find(path);
            if (child == null)
            {
                throw new InvalidOperationException("Missing visual run child under " + root.name + ": " + path);
            }

            return child;
        }

        private static string GetRelativePath(Transform root, Transform child)
        {
            if (child == root)
            {
                return root.name;
            }

            var names = new List<string>();
            var current = child;
            while (current != null && current != root)
            {
                names.Add(current.name);
                current = current.parent;
            }

            if (current != root)
            {
                return child.name + "@outsideTarget";
            }

            names.Reverse();
            return string.Join("/", names);
        }

        private static void CompleteFromState(VisualRunState state)
        {
            var summary = File.Exists(ResultPath) ? File.ReadAllText(ResultPath) : state.Summary;
            var callback = complete;
            CleanupCallbacks();
            DeleteStateFiles();
            callback?.Invoke("Tergo pierce attack current scene visual run completed. " + summary);
        }

        private static void FailFromState(VisualRunState state)
        {
            var error = string.IsNullOrWhiteSpace(state.Error)
                ? "Tergo pierce attack current scene visual run failed."
                : state.Error;
            var callback = fail;
            CleanupCallbacks();
            DeleteStateFiles();
            callback?.Invoke(new InvalidOperationException(error));
        }

        private static void CleanupCallbacks()
        {
            complete = null;
            fail = null;
        }

        private static void DeleteStateFiles()
        {
            TryDelete(StatePath);
            TryDelete(ResultPath);
        }

        private static VisualRunState ReadState()
        {
            if (!File.Exists(StatePath))
            {
                return null;
            }

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var rawLine in File.ReadAllLines(StatePath))
            {
                var index = rawLine.IndexOf('=');
                if (index < 0)
                {
                    continue;
                }

                values[rawLine.Substring(0, index)] = rawLine.Substring(index + 1);
            }

            return new VisualRunState
            {
                Phase = Get(values, "phase"),
                CaptureDir = Get(values, "captureDir"),
                ImagePaths = Get(values, "imagePaths"),
                Summary = Get(values, "summary"),
                Error = Get(values, "error"),
                StartedUtcTicks = long.TryParse(Get(values, "startedUtcTicks"), out var startedUtcTicks)
                    ? startedUtcTicks
                    : 0L
            };
        }

        private static void WriteState(VisualRunState state)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StatePath));
            File.WriteAllLines(
                StatePath,
                new[]
                {
                    "phase=" + state.Phase,
                    "captureDir=" + state.CaptureDir,
                    "imagePaths=" + state.ImagePaths,
                    "summary=" + state.Summary,
                    "error=" + state.Error,
                    "startedUtcTicks=" + state.StartedUtcTicks.ToString(CultureInfo.InvariantCulture)
                });
        }

        private static string Get(IDictionary<string, string> values, string key)
        {
            return values.TryGetValue(key, out var value) ? value : string.Empty;
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static string FormatVector3(Vector3 value)
        {
            return value.x.ToString("0.######", CultureInfo.InvariantCulture) + "," +
                   value.y.ToString("0.######", CultureInfo.InvariantCulture) + "," +
                   value.z.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string ProjectRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static string StatePath =>
            Path.Combine(ProjectRoot, "Logs", StateFileName);

        private static string ResultPath =>
            Path.Combine(ProjectRoot, "Logs", ResultFileName);

        private sealed class VisualRunState
        {
            public string Phase;
            public string CaptureDir;
            public string ImagePaths;
            public string Summary;
            public string Error;
            public long StartedUtcTicks;
        }

        private struct VisualPoseMetrics
        {
            public Vector3 WindupRightHandLocal;
            public Vector3 ThrustRightHandLocal;
            public Vector3 HoldRightHandLocal;
            public float ThrustElbowAngle;
        }

        private sealed class BakedRendererState
        {
            public readonly SkinnedMeshRenderer Renderer;
            public readonly GameObject PreviewObject;
            public readonly Mesh Mesh;
            public readonly bool WasEnabled;

            public BakedRendererState(
                SkinnedMeshRenderer renderer,
                GameObject previewObject,
                Mesh mesh,
                bool wasEnabled)
            {
                Renderer = renderer;
                PreviewObject = previewObject;
                Mesh = mesh;
                WasEnabled = wasEnabled;
            }
        }

        private readonly struct SiblingActiveState
        {
            public readonly GameObject GameObject;
            public readonly bool ActiveSelf;

            public SiblingActiveState(GameObject gameObject, bool activeSelf)
            {
                GameObject = gameObject;
                ActiveSelf = activeSelf;
            }
        }

        private readonly struct RotationCandidate
        {
            public readonly string Name;
            public readonly Vector3 RightArmEuler;
            public readonly Vector3 RightForeArmEuler;
            public readonly Vector3 RightHandEuler;

            public RotationCandidate(
                string name,
                Vector3 rightArmEuler,
                Vector3 rightForeArmEuler,
                Vector3 rightHandEuler)
            {
                Name = name;
                RightArmEuler = rightArmEuler;
                RightForeArmEuler = rightForeArmEuler;
                RightHandEuler = rightHandEuler;
            }
        }

        private readonly struct PositionProbe
        {
            public readonly string Name;
            public readonly Transform Bone;
            public readonly Vector3 Offset;

            public PositionProbe(string name, Transform bone, Vector3 offset)
            {
                Name = name;
                Bone = bone;
                Offset = offset;
            }
        }
    }
}
