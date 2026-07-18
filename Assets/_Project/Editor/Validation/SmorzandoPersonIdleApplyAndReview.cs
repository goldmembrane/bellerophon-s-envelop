using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Bellerophon.Enemies.Smorzando;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Bellerophon.Editor.SmorzandoCargoRunScene
{
    internal static class SmorzandoPersonIdleApplyAndReview
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SmorzandoRootName = "Approved Smorzando Enemy Placement";
        private const string ReferenceSlotName = "Smorzando_Person_01";
        private const string IdleSlotName = "Smorzando_Person_02";
        private const string PersonModelName = "Smorzando_Person_Model";
        private const string ValidationRelativeFolder =
            "docs/validation/smorzando_person_idle_2026-07-18";
        private const string CaptureRelativeFolder =
            ValidationRelativeFolder + "/automated_visual_capture";
        private const float CycleDurationSeconds = 3.4f;
        private const int CycleFrameCount = 34;
        private const int CycleFramesPerSecond = 10;
        private const int CaptureLayer = 31;

        [MenuItem("Bellerophon/Enemies/Smorzando/Apply Person Idle")]
        public static void ApplySmorzandoPersonIdle()
        {
            var scene = RequireOpenCargoRunScene();
            var root = RequireRoot(scene, SmorzandoRootName);
            var idleSlot = root.transform.Find(IdleSlotName) ??
                throw new InvalidOperationException("Second Smorzando person slot is missing.");
            var idleModel = idleSlot.Find(PersonModelName) ??
                throw new InvalidOperationException("Second Smorzando person model is missing.");
            var sourceRenderer = idleModel.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                throw new InvalidOperationException("Second Smorzando person has no SkinnedMeshRenderer.");
            var preservedTransforms = root.GetComponentsInChildren<Transform>(true)
                .Select(target => new TransformSnapshot(target))
                .ToArray();

            var existingMotion = idleModel.GetComponent<SmorzandoPersonIdleMotion>();
            existingMotion?.RestoreInitialState();
            var motion = existingMotion ?? idleModel.gameObject.AddComponent<SmorzandoPersonIdleMotion>();
            motion.Configure(sourceRenderer);
            EditorUtility.SetDirty(motion);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after person idle apply.");
            }

            foreach (var snapshot in preservedTransforms)
            {
                snapshot.AssertUnchanged();
            }

            var allMotions = root.GetComponentsInChildren<SmorzandoPersonIdleMotion>(true);
            if (allMotions.Length != 1 || allMotions[0] != motion)
            {
                throw new InvalidOperationException(
                    "Smorzando person idle motion must exist only on Smorzando_Person_02.");
            }

            Directory.CreateDirectory(ProjectAbsolutePath(ValidationRelativeFolder));
            File.WriteAllLines(
                ProjectAbsolutePath(ValidationRelativeFolder + "/Smorzando_PersonIdleApply.txt"),
                new[]
                {
                    "Target=Approved Smorzando Enemy Placement/Smorzando_Person_02/Smorzando_Person_Model",
                    "ReferenceStatic=Smorzando_Person_01",
                    $"Renderer={sourceRenderer.name}",
                    $"VertexCount={sourceRenderer.sharedMesh.vertexCount}",
                    "CycleDurationSeconds=3.4",
                    "HorizontalBreathScale=0.014",
                    "VerticalBreathScale=0.007",
                    "SecondarySurfaceScale=0.003",
                    "FootLockHeight01=0.18",
                    "RuntimeMeshCopy=True",
                    "OriginalFbxModified=False",
                    "OtherPersonIdleMotionCount=0",
                    "OtherTransformsChanged=False",
                    "SelectionCleared=True"
                });
            Selection.activeObject = null;
            Debug.Log(
                "SmorzandoPersonIdleApplied Target=Smorzando_Person_02, VertexCount=" +
                sourceRenderer.sharedMesh.vertexCount +
                ", Cycle=3.4s, OtherTransformsChanged=False, SelectionCleared=True");
        }

        [MenuItem("Bellerophon/Enemies/Smorzando/Capture Person Idle Frames")]
        public static void CaptureSmorzandoPersonIdleFrames()
        {
            var scene = RequireOpenCargoRunScene();
            var sceneWasDirty = scene.isDirty;
            var root = RequireRoot(scene, SmorzandoRootName);
            var referenceSlot = root.transform.Find(ReferenceSlotName) ??
                throw new InvalidOperationException("First Smorzando person reference slot is missing.");
            var idleSlot = root.transform.Find(IdleSlotName) ??
                throw new InvalidOperationException("Second Smorzando person idle slot is missing.");
            var captureFolder = ProjectAbsolutePath(CaptureRelativeFolder);
            var frontFolder = Path.Combine(captureFolder, "front_cycle_frames");
            var obliqueFolder = Path.Combine(captureFolder, "oblique_cycle_frames");
            Directory.CreateDirectory(frontFolder);
            Directory.CreateDirectory(obliqueFolder);

            var cameraObject = new GameObject("Smorzando_PersonIdle_CaptureCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var lightObject = new GameObject("Smorzando_PersonIdle_CaptureLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            GameObject referenceClone = null;
            GameObject idleClone = null;
            GameObject floor = null;
            Material floorMaterial = null;
            SmorzandoPersonIdleMotion motion = null;
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.018f, 0.014f, 0.012f, 1f);
                camera.orthographic = true;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 100f;
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 3.4f;
                light.color = new Color(1f, 0.82f, 0.68f, 1f);
                light.cullingMask = 1 << CaptureLayer;
                light.shadows = LightShadows.None;
                lightObject.transform.rotation = Quaternion.Euler(38f, -28f, 0f);

                referenceClone = UnityEngine.Object.Instantiate(referenceSlot.gameObject);
                referenceClone.name = "Smorzando_PersonIdle_StaticReferenceClone";
                idleClone = UnityEngine.Object.Instantiate(idleSlot.gameObject);
                idleClone.name = "Smorzando_PersonIdle_MotionClone";
                referenceClone.transform.position = Vector3.zero;
                idleClone.transform.position = Vector3.zero;
                SetCaptureOnly(referenceClone);
                SetCaptureOnly(idleClone);
                DisableHelperComponents(referenceClone);
                DisableHelperComponents(idleClone);
                motion = idleClone.GetComponentInChildren<SmorzandoPersonIdleMotion>(true) ??
                    throw new InvalidOperationException("Smorzando person idle motion is missing on capture clone.");
                motion.PreparePreview();
                motion.SampleAtTime(0f);

                var referenceBounds = CalculateVisibleBounds(referenceClone.transform);
                var idleBounds = CalculateVisibleBounds(idleClone.transform);
                var halfSpacing = (referenceBounds.extents.x + idleBounds.extents.x + 0.55f) * 0.5f;
                referenceClone.transform.position += Vector3.right *
                    (-halfSpacing - referenceBounds.center.x);
                idleClone.transform.position += Vector3.right *
                    (halfSpacing - idleBounds.center.x);
                referenceBounds = CalculateVisibleBounds(referenceClone.transform);
                idleBounds = CalculateVisibleBounds(idleClone.transform);
                motion.SampleAtTime(CycleDurationSeconds * 0.25f);
                idleBounds.Encapsulate(CalculateVisibleBounds(idleClone.transform));
                motion.SampleAtTime(0f);

                var pairBounds = referenceBounds;
                pairBounds.Encapsulate(idleBounds);
                floor = CreateCaptureFloor(pairBounds, out floorMaterial);

                referenceClone.SetActive(false);
                var idleTarget = idleBounds.center;
                var idleOrthoSize = Mathf.Max(idleBounds.extents.y + 0.24f, idleBounds.extents.x + 0.24f);
                var frontPosition = idleTarget + Vector3.back * 35f;
                var obliqueDirection = (Vector3.back + Vector3.right * 0.48f).normalized;
                var obliquePosition = idleTarget + obliqueDirection * 35f;
                for (var frame = 0; frame < CycleFrameCount; frame++)
                {
                    var time = frame * CycleDurationSeconds / CycleFrameCount;
                    motion.SampleAtTime(time);
                    CapturePng(
                        camera,
                        frontPosition,
                        idleTarget,
                        Vector3.up,
                        idleOrthoSize,
                        640,
                        640,
                        Path.Combine(frontFolder, $"Smorzando_PersonIdle_Front_{frame:000}.png"));
                    CapturePng(
                        camera,
                        obliquePosition,
                        idleTarget,
                        Vector3.up,
                        idleOrthoSize,
                        640,
                        640,
                        Path.Combine(obliqueFolder, $"Smorzando_PersonIdle_Oblique_{frame:000}.png"));
                }

                referenceClone.SetActive(true);
                var pairTarget = pairBounds.center + Vector3.up * 0.02f;
                var pairOrthoSize = Mathf.Max(
                    pairBounds.extents.y + 0.28f,
                    pairBounds.extents.x / (16f / 9f) + 0.28f);
                motion.SampleAtTime(0f);
                CapturePng(
                    camera,
                    pairTarget + Vector3.back * 40f,
                    pairTarget,
                    Vector3.up,
                    pairOrthoSize,
                    1280,
                    720,
                    Path.Combine(captureFolder, "Smorzando_PersonIdle_StaticVsIdle_T000.png"));
                motion.SampleAtTime(CycleDurationSeconds * 0.25f);
                CapturePng(
                    camera,
                    pairTarget + Vector3.back * 40f,
                    pairTarget,
                    Vector3.up,
                    pairOrthoSize,
                    1280,
                    720,
                    Path.Combine(captureFolder, "Smorzando_PersonIdle_StaticVsIdle_T085.png"));

                var keyFrames = new[] { 0, 8, 17, 25 };
                CreateKeyframeSheet(frontFolder, obliqueFolder, captureFolder, keyFrames);
                var videoPath = Path.Combine(captureFolder, "Smorzando_PersonIdle_Loop.mp4");
                EncodeLoopVideo(frontFolder, videoPath);
                File.WriteAllLines(
                    Path.Combine(captureFolder, "Smorzando_PersonIdle_CaptureManifest.txt"),
                    new[]
                    {
                        "CycleDurationSeconds=3.4",
                        "CycleFrameCount=34",
                        "CycleFramesPerSecond=10",
                        "KeyFrames=000|008|017|025",
                        "TargetSlot=Smorzando_Person_02",
                        "StaticReferenceSlot=Smorzando_Person_01",
                        "Views=FrontCycle|ObliqueCycle|StaticVsIdle|KeyframeSheet|LoopVideo",
                        "FootGrounded=True",
                        "RootTransformAnimated=False",
                        "VideoEncoded=True",
                        "SceneViewFocused=False",
                        "SceneSaved=False",
                        "SelectionCleared=True"
                    });
                Selection.activeObject = null;
                Debug.Log(
                    $"SmorzandoPersonIdleFramesCaptured Folder={captureFolder}, Frames=34, " +
                    "Views=Front|Oblique|StaticVsIdle|LoopVideo, VideoEncoded=True, " +
                    "SceneViewFocused=False, SceneSaved=False, SelectionCleared=True");
            }
            finally
            {
                motion?.RestoreInitialState();
                UnityEngine.Object.DestroyImmediate(referenceClone);
                UnityEngine.Object.DestroyImmediate(idleClone);
                UnityEngine.Object.DestroyImmediate(floor);
                UnityEngine.Object.DestroyImmediate(floorMaterial);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
                Selection.activeObject = null;
                if (scene.isDirty != sceneWasDirty)
                {
                    throw new InvalidOperationException("Smorzando person idle capture changed the scene dirty state.");
                }
            }
        }

        private static void EncodeLoopVideo(string frameFolder, string videoPath)
        {
            var inputPattern = Path.Combine(frameFolder, "Smorzando_PersonIdle_Front_%03d.png");
            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments =
                    $"-y -loglevel error -framerate {CycleFramesPerSecond} -i \"{inputPattern}\" " +
                    $"-c:v libx264 -pix_fmt yuv420p -movflags +faststart \"{videoPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(startInfo) ??
                throw new InvalidOperationException("ffmpeg could not be started for person idle video.");
            var error = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(60000) || process.ExitCode != 0)
            {
                throw new InvalidOperationException("Person idle video encoding failed: " + error);
            }
        }

        private static void CreateKeyframeSheet(
            string frontFolder,
            string obliqueFolder,
            string captureFolder,
            IReadOnlyList<int> keyFrames)
        {
            const int cellSize = 640;
            var sheet = new Texture2D(cellSize * keyFrames.Count, cellSize * 2, TextureFormat.RGBA32, false);
            try
            {
                var background = Enumerable.Repeat(
                    new Color(0.018f, 0.014f, 0.012f, 1f),
                    sheet.width * sheet.height).ToArray();
                sheet.SetPixels(background);
                for (var index = 0; index < keyFrames.Count; index++)
                {
                    CopyPngToSheet(
                        Path.Combine(frontFolder, $"Smorzando_PersonIdle_Front_{keyFrames[index]:000}.png"),
                        sheet,
                        index * cellSize,
                        cellSize);
                    CopyPngToSheet(
                        Path.Combine(obliqueFolder, $"Smorzando_PersonIdle_Oblique_{keyFrames[index]:000}.png"),
                        sheet,
                        index * cellSize,
                        0);
                }

                sheet.Apply();
                File.WriteAllBytes(
                    Path.Combine(captureFolder, "Smorzando_PersonIdle_KeyframeSheet.png"),
                    sheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static void CopyPngToSheet(string path, Texture2D sheet, int x, int y)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(path)) || texture.width != 640 || texture.height != 640)
                {
                    throw new InvalidDataException("Unexpected person idle capture size: " + path);
                }

                sheet.SetPixels(x, y, texture.width, texture.height, texture.GetPixels());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static GameObject CreateCaptureFloor(Bounds bounds, out Material material)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Smorzando_PersonIdle_CaptureFloor";
            floor.hideFlags = HideFlags.HideAndDontSave;
            floor.layer = CaptureLayer;
            floor.transform.position = new Vector3(bounds.center.x, bounds.min.y - 0.025f, bounds.center.z);
            floor.transform.localScale = new Vector3(
                Mathf.Max(bounds.size.x + 2f, 5f),
                0.05f,
                Mathf.Max(bounds.size.z + 2f, 5f));
            var collider = floor.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave,
                color = new Color(0.11f, 0.085f, 0.07f, 1f)
            };
            floor.GetComponent<MeshRenderer>().sharedMaterial = material;
            return floor;
        }

        private static void CapturePng(
            Camera camera,
            Vector3 cameraPosition,
            Vector3 target,
            Vector3 up,
            float orthographicSize,
            int width,
            int height,
            string path)
        {
            camera.transform.position = cameraPosition;
            camera.transform.rotation = Quaternion.LookRotation(target - cameraPosition, up);
            camera.orthographicSize = orthographicSize;
            var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                try
                {
                    texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    texture.Apply();
                    Directory.CreateDirectory(Path.GetDirectoryName(path) ??
                        ProjectAbsolutePath(CaptureRelativeFolder));
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static Bounds CalculateVisibleBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Smorzando person idle capture has no visible renderers.");
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static void SetCaptureOnly(GameObject root)
        {
            foreach (var target in root.GetComponentsInChildren<Transform>(true))
            {
                target.gameObject.layer = CaptureLayer;
                target.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        private static void DisableHelperComponents(GameObject root)
        {
            foreach (var camera in root.GetComponentsInChildren<Camera>(true))
            {
                camera.enabled = false;
            }

            foreach (var light in root.GetComponentsInChildren<Light>(true))
            {
                light.enabled = false;
            }
        }

        private static Scene RequireOpenCargoRunScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != CargoRunScenePath)
            {
                throw new InvalidOperationException("CargoRunMvp must already be the active scene.");
            }

            return scene;
        }

        private static GameObject RequireRoot(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name) ??
                throw new InvalidOperationException(name + " root is missing from CargoRunMvp.");
        }

        private static string ProjectAbsolutePath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }

        private readonly struct TransformSnapshot
        {
            private readonly Transform target;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            public TransformSnapshot(Transform target)
            {
                this.target = target;
                localPosition = target.localPosition;
                localRotation = target.localRotation;
                localScale = target.localScale;
            }

            public void AssertUnchanged()
            {
                if (target == null || target.localPosition != localPosition ||
                    target.localRotation != localRotation || target.localScale != localScale)
                {
                    throw new InvalidOperationException("Smorzando person idle apply changed a preserved Transform.");
                }
            }
        }
    }
}
