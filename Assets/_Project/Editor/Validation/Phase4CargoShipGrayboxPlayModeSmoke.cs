using System;
using System.Globalization;
using System.IO;
using System.Text;
using Bellerophon.Core.Player;
using Bellerophon.Core.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellerophon.Editor.Validation
{
    [InitializeOnLoad]
    internal static class Phase4CargoShipGrayboxPlayModeSmoke
    {
        private const string RequestFileName = "Phase4CargoShipGrayboxSmoke.request";
        private const string ActiveFileName = "Phase4CargoShipGrayboxSmoke.active";
        private const string ErrorsFileName = "Phase4CargoShipGrayboxSmoke.errors";
        private const string CargoRunSceneName = "CargoRunMvp";
        private const double PollIntervalSeconds = 0.1d;
        private const double MaxRunSeconds = 30d;
        private const int RequiredPlayFrames = 2;

        private static double nextPollTime;

        static Phase4CargoShipGrayboxPlayModeSmoke()
        {
            EditorApplication.update += Poll;
            Application.logMessageReceived += CaptureLog;
        }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPollTime)
            {
                return;
            }

            nextPollTime = EditorApplication.timeSinceStartup + PollIntervalSeconds;

            try
            {
                if (TryContinueActiveRequest())
                {
                    return;
                }

                TryStartRequest();
            }
            catch (Exception exception)
            {
                FailCurrentRequest(exception);
            }
        }

        private static void TryStartRequest()
        {
            if (!File.Exists(RequestPath) || File.Exists(ActivePath))
            {
                return;
            }

            var request = SmokeRequest.Read(RequestPath);
            TryDelete(RequestPath);
            if (!request.IsValid)
            {
                return;
            }

            request.Phase = SmokePhase.Prepare;
            request.StartUtcTicks = DateTime.UtcNow.Ticks;
            request.PlayFrameCount = 0;
            TryDelete(ErrorsPath);
            request.Write(ActivePath);
        }

        private static bool TryContinueActiveRequest()
        {
            if (!File.Exists(ActivePath))
            {
                return false;
            }

            var request = SmokeRequest.Read(ActivePath);
            if (!request.IsValid)
            {
                TryDelete(ActivePath);
                return false;
            }

            if (IsExpired(request))
            {
                throw new TimeoutException($"Phase 4 cargo ship graybox smoke exceeded {MaxRunSeconds:0} seconds.");
            }

            switch (request.Phase)
            {
                case SmokePhase.Prepare:
                    PrepareAndEnterPlayMode(request);
                    break;
                case SmokePhase.WaitForPlayMode:
                    WaitForPlayMode(request);
                    break;
                case SmokePhase.ValidateRuntime:
                    ValidateRuntimeWhenReady(request);
                    break;
                case SmokePhase.ExitPlayMode:
                    FinishAfterPlayModeExit(request);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown phase 4 smoke phase: {request.Phase}");
            }

            return true;
        }

        private static void PrepareAndEnterPlayMode(SmokeRequest request)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Phase 4 smoke must start from Edit mode.");
            }

            Phase4CargoShipGrayboxBootstrap.EnsurePhase4Assets();
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            EditorSceneManager.playModeStartScene = sceneAsset;
            Phase4CargoShipGrayboxEditorValidation.Run();

            request.Phase = SmokePhase.WaitForPlayMode;
            request.Write(ActivePath);
            EditorApplication.EnterPlaymode();
        }

        private static void WaitForPlayMode(SmokeRequest request)
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            request.Phase = SmokePhase.ValidateRuntime;
            request.PlayFrameCount = 0;
            request.Write(ActivePath);
        }

        private static void ValidateRuntimeWhenReady(SmokeRequest request)
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            request.PlayFrameCount++;
            if (request.PlayFrameCount < RequiredPlayFrames)
            {
                request.Write(ActivePath);
                return;
            }

            request.Details = ValidateRuntime();
            request.Phase = SmokePhase.ExitPlayMode;
            request.Write(ActivePath);
            EditorApplication.ExitPlaymode();
        }

        private static void FinishAfterPlayModeExit(SmokeRequest request)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (SceneManager.GetActiveScene().path != Phase4CargoShipGrayboxBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            if (File.Exists(ErrorsPath))
            {
                WriteLog(request, true, new InvalidOperationException("Phase 4 smoke captured Unity errors."));
                TryDelete(ActivePath);
                TryDelete(ErrorsPath);
                return;
            }

            WriteLog(request, false, null);
            TryDelete(ActivePath);
            TryDelete(ErrorsPath);
        }

        private static string ValidateRuntime()
        {
            if (SceneManager.GetActiveScene().name != CargoRunSceneName)
            {
                throw new InvalidOperationException($"Expected active scene {CargoRunSceneName}, got {SceneManager.GetActiveScene().name}.");
            }

            var playerMotor = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            var playerInput = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            var interaction = UnityEngine.Object.FindFirstObjectByType<FirstPersonInteractionController>();
            var hud = UnityEngine.Object.FindFirstObjectByType<FirstPersonHud>();

            if (playerMotor == null || playerInput == null || interaction == null || hud == null)
            {
                throw new InvalidOperationException("Runtime scene must contain player motor/input/interaction and HUD.");
            }

            var root = GameObject.Find(Phase4CargoShipGrayboxBootstrap.GrayboxRootName);
            if (root == null)
            {
                throw new InvalidOperationException("Runtime scene must contain the Phase 4 graybox root.");
            }

            RequireObject("Room - Cargo Hold");
            RequireObject("Room - Cockpit");
            RequireObject("Room - Engine Room");
            RequireObject("Room - Control Room");
            RequireObject("Room - Armory");
            RequireObject("Room - Supply Room");
            RequireObject("Corridor - Cargo Hold to Cockpit");
            RequireObject("Corridor - Cargo Hold to Engine Room");
            RequireObject("Corridor - Cargo Hold to Control Room");
            RequireObject("Corridor - Cargo Hold to Armory");
            RequireObject("Corridor - Cargo Hold to Supply Room");

            CompleteBlockingStartFlowIfPresent();

            if (Cursor.lockState != CursorLockMode.Locked || Cursor.visible)
            {
                throw new InvalidOperationException($"Runtime cursor must be locked and hidden. LockState={Cursor.lockState}, Visible={Cursor.visible}");
            }

            var camera = Camera.main;
            if (camera == null || !camera.isActiveAndEnabled)
            {
                throw new InvalidOperationException("Runtime scene must have an active MainCamera.");
            }

            var renderedPixels = CountRenderedScenePixels(camera);
            if (renderedPixels < 600)
            {
                throw new InvalidOperationException($"Runtime camera rendered too few visible pixels: {renderedPixels}.");
            }

            var visibleRenderers = CountVisibleRenderers(camera);
            if (visibleRenderers < 5)
            {
                throw new InvalidOperationException($"Runtime camera frustum has too few visible graybox renderers: {visibleRenderers}.");
            }

            playerMotor.transform.rotation = Quaternion.identity;
            camera.transform.localRotation = Quaternion.identity;
            Physics.SyncTransforms();

            if (!interaction.TryInteract())
            {
                throw new InvalidOperationException($"Runtime interaction detection failed. Failure={interaction.LastFailureReason}");
            }

            var currentTarget = interaction.LastInteractable as DebugInteractable;
            if (currentTarget == null)
            {
                throw new InvalidOperationException("Runtime graybox interaction target must be a DebugInteractable.");
            }

            if (currentTarget.InteractionCount < 1)
            {
                throw new InvalidOperationException("Runtime graybox interaction target did not record interaction.");
            }

            var promptText = FindHudText(hud, "Interaction Prompt Text");
            if (promptText == null)
            {
                throw new InvalidOperationException("Runtime HUD must include an interaction prompt label.");
            }

            var controller = playerMotor.GetComponent<CharacterController>();
            if (controller == null)
            {
                throw new InvalidOperationException("Runtime player must have a CharacterController.");
            }

            var armoryCargoRouteDistance = ValidateArmoryCargoRoute(playerMotor, controller);

            var start = playerMotor.transform.position;
            controller.Move(Vector3.forward * 1.0f);
            Physics.SyncTransforms();
            var moved = Vector3.Distance(start, playerMotor.transform.position);
            if (moved < 0.5f)
            {
                throw new InvalidOperationException($"Runtime player could not move on graybox floor. Moved={moved:0.00}");
            }

            return $"Scene={CargoRunSceneName}; RenderedPixels={renderedPixels}; VisibleRenderers={visibleRenderers}; Rooms=6; Corridors=10; InteractionTarget={currentTarget.DisplayName}; Moved={moved.ToString("0.00", CultureInfo.InvariantCulture)}; ArmoryCargoRoute={armoryCargoRouteDistance.ToString("0.00", CultureInfo.InvariantCulture)}";
        }

        private static void CompleteBlockingStartFlowIfPresent()
        {
            var startFlow = UnityEngine.Object.FindFirstObjectByType<NewGameStartFlowController>();
            if (startFlow == null || !startFlow.gameObject.activeInHierarchy)
            {
                return;
            }

            if (startFlow.FlowState.Phase == NewGameStartFlowPhase.ContractPrompt)
            {
                startFlow.AcceptAssociationContract();
            }

            if (startFlow.FlowState.Phase == NewGameStartFlowPhase.AssociationPlanet)
            {
                startFlow.AcceptTutorialContract();
            }
        }

        private static float ValidateArmoryCargoRoute(FirstPersonPlayerMotor playerMotor, CharacterController controller)
        {
            var savedPosition = playerMotor.transform.position;
            var savedRotation = playerMotor.transform.rotation;
            var motorWasEnabled = playerMotor.enabled;

            try
            {
                playerMotor.enabled = false;

                var cargoToArmoryRoute = Phase4CargoShipGrayboxBootstrap.ArmoryCargoCorridorRoute();
                if (cargoToArmoryRoute.Length < 2)
                {
                    throw new InvalidOperationException("Armory to Cargo Hold route must contain at least two points.");
                }

                var armoryToCargoRoute = new Vector3[cargoToArmoryRoute.Length];
                for (var i = 0; i < cargoToArmoryRoute.Length; i++)
                {
                    armoryToCargoRoute[i] = cargoToArmoryRoute[cargoToArmoryRoute.Length - i - 1];
                }

                controller.enabled = false;
                playerMotor.transform.SetPositionAndRotation(armoryToCargoRoute[0] + Vector3.up * 0.05f, Quaternion.identity);
                controller.enabled = true;
                Physics.SyncTransforms();

                var traveledDistance = 0f;
                for (var i = 1; i < armoryToCargoRoute.Length; i++)
                {
                    traveledDistance += MoveControllerAlongRouteSegment(controller, armoryToCargoRoute[i], i);
                }

                return traveledDistance;
            }
            finally
            {
                controller.enabled = false;
                playerMotor.transform.SetPositionAndRotation(savedPosition, savedRotation);
                controller.enabled = true;
                playerMotor.enabled = motorWasEnabled;
                Physics.SyncTransforms();
            }
        }

        private static float MoveControllerAlongRouteSegment(CharacterController controller, Vector3 target, int segmentIndex)
        {
            const float stepDistance = 0.2f;
            const float arrivalTolerance = 0.45f;
            const int maxSteps = 220;
            const int maxStuckSteps = 8;

            var traveledDistance = 0f;
            var stuckSteps = 0;
            for (var step = 0; step < maxSteps; step++)
            {
                var current = controller.transform.position;
                var planarToTarget = new Vector3(target.x - current.x, 0f, target.z - current.z);
                var remainingDistance = planarToTarget.magnitude;
                if (remainingDistance <= arrivalTolerance)
                {
                    return traveledDistance;
                }

                var desiredPlanarMove = planarToTarget.normalized * Mathf.Min(stepDistance, remainingDistance);
                controller.Move(desiredPlanarMove + Vector3.down * 0.04f);
                Physics.SyncTransforms();

                var nextPosition = controller.transform.position;
                var nextPlanarToTarget = new Vector3(target.x - nextPosition.x, 0f, target.z - nextPosition.z);
                var progress = remainingDistance - nextPlanarToTarget.magnitude;
                traveledDistance += Mathf.Max(0f, progress);

                if (progress < 0.03f)
                {
                    stuckSteps++;
                    if (stuckSteps >= maxStuckSteps)
                    {
                        throw new InvalidOperationException(
                            $"Armory to Cargo Hold route is blocked at segment {segmentIndex}. Position={nextPosition}, Remaining={nextPlanarToTarget.magnitude:0.00}");
                    }

                    continue;
                }

                stuckSteps = 0;
            }

            throw new InvalidOperationException($"Armory to Cargo Hold route did not reach segment {segmentIndex} target within {maxSteps} steps.");
        }

        private static void RequireObject(string objectName)
        {
            if (GameObject.Find(objectName) == null)
            {
                throw new InvalidOperationException("Missing runtime graybox object: " + objectName);
            }
        }

        private static Text FindHudText(FirstPersonHud hud, string name)
        {
            var labels = hud.GetComponentsInChildren<Text>(true);
            for (var i = 0; i < labels.Length; i++)
            {
                if (labels[i].name == name)
                {
                    return labels[i];
                }
            }

            return null;
        }

        private static void CaptureLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                return;
            }

            if (!File.Exists(ActivePath))
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ErrorsPath));
                File.AppendAllText(ErrorsPath, $"{type}: {condition}{Environment.NewLine}{stackTrace}{Environment.NewLine}");
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static int CountRenderedScenePixels(Camera camera)
        {
            var previousTargetTexture = camera.targetTexture;
            var previousActiveTexture = RenderTexture.active;
            var renderTexture = new RenderTexture(160, 90, 24, RenderTextureFormat.ARGB32);
            var readableTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);

            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                readableTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                readableTexture.Apply();

                var background = camera.backgroundColor;
                var pixels = readableTexture.GetPixels();
                var visiblePixelCount = 0;
                for (var i = 0; i < pixels.Length; i++)
                {
                    if (ColorDistance(pixels[i], background) > 0.08f)
                    {
                        visiblePixelCount++;
                    }
                }

                return visiblePixelCount;
            }
            finally
            {
                camera.targetTexture = previousTargetTexture;
                RenderTexture.active = previousActiveTexture;
                DestroyTexture(renderTexture);
                DestroyTexture(readableTexture);
            }
        }

        private static void DestroyTexture(UnityEngine.Object texture)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(texture);
                return;
            }

            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static int CountVisibleRenderers(Camera camera)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            var visibleRendererCount = 0;

            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
                {
                    visibleRendererCount++;
                }
            }

            return visibleRendererCount;
        }

        private static float ColorDistance(Color left, Color right)
        {
            var red = left.r - right.r;
            var green = left.g - right.g;
            var blue = left.b - right.b;
            return Mathf.Sqrt((red * red) + (green * green) + (blue * blue));
        }

        private static bool IsExpired(SmokeRequest request)
        {
            if (request.StartUtcTicks <= 0)
            {
                return false;
            }

            var elapsed = DateTime.UtcNow - new DateTime(request.StartUtcTicks, DateTimeKind.Utc);
            return elapsed.TotalSeconds > MaxRunSeconds;
        }

        private static void FailCurrentRequest(Exception exception)
        {
            if (!File.Exists(ActivePath))
            {
                return;
            }

            var request = SmokeRequest.Read(ActivePath);
            if (request.IsValid)
            {
                WriteLog(request, true, exception);
            }

            TryDelete(ActivePath);
            TryDelete(ErrorsPath);
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
            }
        }

        private static void WriteLog(SmokeRequest request, bool failed, Exception exception)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Phase 4 cargo ship graybox smoke completed: {request.Id}");
            builder.AppendLine("Unity editor smoke mode: open editor quick playmode");
            builder.AppendLine($"Result: {(failed ? "Failed" : "Passed")}");

            if (!string.IsNullOrWhiteSpace(request.Details))
            {
                builder.AppendLine(request.Details);
            }

            if (failed && exception != null)
            {
                builder.AppendLine(exception.ToString());
            }

            if (File.Exists(ErrorsPath))
            {
                builder.AppendLine();
                builder.AppendLine("Captured Unity errors:");
                builder.Append(File.ReadAllText(ErrorsPath));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(request.LogPath));
            File.WriteAllText(request.LogPath, builder.ToString());
        }

        private static void TryDelete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static string RequestPath =>
            Path.Combine(ProjectRoot, "Logs", RequestFileName);

        private static string ActivePath =>
            Path.Combine(ProjectRoot, "Logs", ActiveFileName);

        private static string ErrorsPath =>
            Path.Combine(ProjectRoot, "Logs", ErrorsFileName);

        private static string ProjectRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private enum SmokePhase
        {
            Prepare,
            WaitForPlayMode,
            ValidateRuntime,
            ExitPlayMode
        }

        private sealed class SmokeRequest
        {
            public string Id { get; private set; }
            public string LogPath { get; private set; }
            public SmokePhase Phase { get; set; }
            public long StartUtcTicks { get; set; }
            public int PlayFrameCount { get; set; }
            public string Details { get; set; }

            public bool IsValid =>
                !string.IsNullOrWhiteSpace(Id) &&
                !string.IsNullOrWhiteSpace(LogPath);

            public static SmokeRequest Read(string path)
            {
                var request = new SmokeRequest();
                foreach (var rawLine in File.ReadAllLines(path))
                {
                    var line = rawLine.Trim().TrimStart('\uFEFF');
                    if (line.Length == 0)
                    {
                        continue;
                    }

                    var separatorIndex = line.IndexOf('=');
                    if (separatorIndex < 0)
                    {
                        continue;
                    }

                    var key = line.Substring(0, separatorIndex);
                    var value = line.Substring(separatorIndex + 1);
                    switch (key)
                    {
                        case "id":
                            request.Id = value;
                            break;
                        case "logPath":
                            request.LogPath = value;
                            break;
                        case "phase":
                            if (Enum.TryParse(value, out SmokePhase phase))
                            {
                                request.Phase = phase;
                            }
                            break;
                        case "startUtcTicks":
                            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
                            {
                                request.StartUtcTicks = ticks;
                            }
                            break;
                        case "playFrameCount":
                            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count))
                            {
                                request.PlayFrameCount = count;
                            }
                            break;
                        case "details":
                            request.Details = value;
                            break;
                    }
                }

                return request;
            }

            public void Write(string path)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllLines(
                    path,
                    new[]
                    {
                        $"id={Id}",
                        $"logPath={LogPath}",
                        $"phase={Phase}",
                        $"startUtcTicks={StartUtcTicks}",
                        $"playFrameCount={PlayFrameCount}",
                        $"details={Details ?? string.Empty}"
                    });
            }
        }
    }
}
