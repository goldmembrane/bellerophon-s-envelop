using System.Collections;
using Bellerophon.Core.Player;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Bellerophon.Tests.PlayMode
{
    public sealed class Phase2PlayerMvpTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator CargoRunMvp_SpawnsFirstPersonPlayer()
        {
            yield return LoadCargoRunMvp();

            var player = Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            Assert.That(player, Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<FirstPersonPlayerInput>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<FirstPersonHud>(), Is.Not.Null);
            Assert.That(Camera.main, Is.Not.Null);

            Assert.That(ShipInteriorMapRules.FindCurrentRoom(player.transform.position), Is.EqualTo(ShipRoomId.Cockpit));
            Assert.That(player.transform.position.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(player.transform.position.y, Is.EqualTo(0f).Within(0.01f));
            Assert.That(player.transform.position.z, Is.EqualTo(20.6f).Within(0.01f));
            Assert.That(Quaternion.Angle(player.transform.rotation, Quaternion.Euler(0f, 180f, 0f)), Is.LessThan(0.1f));
        }

        [UnityTest]
        public IEnumerator CargoRunMvp_IdlesForOneFrameWithoutUnexpectedLogs()
        {
            yield return LoadCargoRunMvp();

            yield return null;

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TryInteract_InteractsWithTargetInFrontOfPlayer()
        {
            yield return LoadCargoRunMvp();
            yield return PlacePlayerAtInteractionTestStart();

            var interaction = Object.FindFirstObjectByType<FirstPersonInteractionController>();
            Assert.That(interaction, Is.Not.Null);
            yield return null;

            Assert.That(interaction.HasCurrentTarget, Is.True, DescribeInteractionState(interaction));
            Assert.That(interaction.CurrentTargetCanInteract, Is.True, DescribeInteractionState(interaction));

            var interacted = interaction.TryInteract();

            Assert.That(interacted, Is.True);
            Assert.That(interaction.LastInteractable, Is.Not.Null);
            Assert.That(interaction.LastFailureReason, Is.Empty);
        }

        [UnityTest]
        public IEnumerator BootstrapPlayPath_LoadsCargoRunMvpWithRenderableFirstPersonView()
        {
            SceneManager.LoadScene("Bootstrap");

            for (var frame = 0; frame < 10 && !SceneManager.GetSceneByName("CargoRunMvp").isLoaded; frame++)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetSceneByName("CargoRunMvp").isLoaded, Is.True);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("CargoRunMvp"));

            var camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            Assert.That(camera.isActiveAndEnabled, Is.True);
            Assert.That(RenderedScenePixelCount(camera), Is.GreaterThan(200));
        }

        [UnityTest]
        public IEnumerator CargoRunMvp_HasOverlayHudContent()
        {
            yield return LoadCargoRunMvp();

            var hud = Object.FindFirstObjectByType<FirstPersonHud>();
            Assert.That(hud, Is.Not.Null);

            var canvas = hud.GetComponent<Canvas>();
            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.isActiveAndEnabled, Is.True);
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));

            var labels = hud.GetComponentsInChildren<Text>(true);
            Assert.That(labels.Length, Is.GreaterThanOrEqualTo(4));
        }

        [UnityTest]
        public IEnumerator CargoRunMvp_EditorPlaytestFreeMovementAllowsVerticalTravel()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadCargoRunMvp();

            var player = Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            var input = Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            Assert.That(player, Is.Not.Null);
            Assert.That(input, Is.Not.Null);

            input.SetCursorLockSuppressed(false);
            input.SetGameplayInputSuppressed(false);
            var start = player.transform.position;
            var forward = player.PlayerCamera != null ? player.PlayerCamera.forward : player.transform.forward;

            Press(keyboard.wKey);
            Press(keyboard.spaceKey);
            yield return null;
            yield return null;
            Release(keyboard.spaceKey);
            Release(keyboard.wKey);

            var delta = player.transform.position - start;
            Assert.That(delta.y, Is.GreaterThan(0.01f));
            Assert.That(Vector3.Dot(delta, forward), Is.GreaterThan(0.01f));
        }

        [UnityTest]
        public IEnumerator CargoRunMvp_ShowsInteractionPromptForTarget()
        {
            yield return LoadCargoRunMvp();
            yield return PlacePlayerAtInteractionTestStart();

            var interaction = Object.FindFirstObjectByType<FirstPersonInteractionController>();
            var target = Object.FindFirstObjectByType<DebugInteractable>();
            var hud = Object.FindFirstObjectByType<FirstPersonHud>();
            Assert.That(interaction, Is.Not.Null);
            Assert.That(target, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);

            yield return null;

            Assert.That(interaction.HasCurrentTarget, Is.True, DescribeInteractionState(interaction));
            Assert.That(interaction.CurrentTargetCanInteract, Is.True, DescribeInteractionState(interaction));

            var promptText = FindHudText(hud, "Interaction Prompt Text");
            Assert.That(promptText, Is.Not.Null);
            Assert.That(promptText.enabled, Is.True);
            Assert.That(promptText.text, Does.Contain(interaction.CurrentTargetDisplayName));
        }

        private static IEnumerator LoadCargoRunMvp()
        {
            SceneManager.LoadScene("CargoRunMvp");
            yield return null;
            yield return null;
        }

        private static IEnumerator PlacePlayerAtInteractionTestStart()
        {
            var player = Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            var input = Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            var target = FindInteractionTestTarget();
            var camera = Camera.main;
            Assert.That(player, Is.Not.Null);
            Assert.That(target, Is.Not.Null);
            Assert.That(camera, Is.Not.Null);

            input?.SetCursorLockSuppressed(false);
            input?.SetGameplayInputSuppressed(false);

            var targetCollider = target.GetComponent<Collider>();
            Assert.That(targetCollider, Is.Not.Null);

            var cameraOffset = camera.transform.position - player.transform.position;
            var targetPoint = targetCollider.bounds.center;
            player.transform.SetPositionAndRotation(targetPoint - cameraOffset - (Vector3.forward * 0.8f), Quaternion.identity);
            camera.transform.localRotation = Quaternion.identity;

            Physics.SyncTransforms();
            yield return null;
        }

        private static string DescribeInteractionState(FirstPersonInteractionController interaction)
        {
            var player = Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            var camera = Camera.main;
            return "Failure='" +
                   interaction.CurrentTargetFailureReason +
                   "' Player=" +
                   (player == null ? "<null>" : player.transform.position.ToString("F3")) +
                   " Camera=" +
                   (camera == null ? "<null>" : camera.transform.position.ToString("F3")) +
                   " Forward=" +
                   (camera == null ? "<null>" : camera.transform.forward.ToString("F3"));
        }

        private static DebugInteractable FindInteractionTestTarget()
        {
            var targets = Object.FindObjectsByType<DebugInteractable>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (var i = 0; i < targets.Length; i++)
            {
                if (targets[i].DisplayName == "Cargo Hold Cargo Status")
                {
                    return targets[i];
                }
            }

            return targets.Length > 0 ? targets[0] : null;
        }

        private static int RenderedScenePixelCount(Camera camera)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                return RenderableSceneObjectScore();
            }

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
                Object.Destroy(renderTexture);
                Object.Destroy(readableTexture);
            }
        }

        private static float ColorDistance(Color left, Color right)
        {
            var red = left.r - right.r;
            var green = left.g - right.g;
            var blue = left.b - right.b;
            return Mathf.Sqrt((red * red) + (green * green) + (blue * blue));
        }

        private static int RenderableSceneObjectScore()
        {
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var activeRendererCount = 0;
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].enabled && renderers[i].gameObject.activeInHierarchy)
                {
                    activeRendererCount++;
                }
            }

            return activeRendererCount * 100;
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
    }
}
