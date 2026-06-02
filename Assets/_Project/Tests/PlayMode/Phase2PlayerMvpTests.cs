using System.Collections;
using Bellerophon.Core.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
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

            Assert.That(Object.FindFirstObjectByType<FirstPersonPlayerMotor>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<FirstPersonPlayerInput>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<FirstPersonHud>(), Is.Not.Null);
            Assert.That(Camera.main, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator CargoRunMvp_IdlesForOneFrameWithoutUnexpectedLogs()
        {
            yield return LoadCargoRunMvp();

            yield return null;

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PressingF_InteractsWithTargetInFrontOfPlayer()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadCargoRunMvp();

            var target = Object.FindFirstObjectByType<DebugInteractable>();
            Assert.That(target, Is.Not.Null);
            var initialCount = target.InteractionCount;

            Press(keyboard.fKey);
            yield return null;
            Release(keyboard.fKey);
            yield return null;

            Assert.That(target.InteractionCount, Is.EqualTo(initialCount + 1));
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
        public IEnumerator CargoRunMvp_ShowsInteractionPromptForTarget()
        {
            yield return LoadCargoRunMvp();

            var interaction = Object.FindFirstObjectByType<FirstPersonInteractionController>();
            var target = Object.FindFirstObjectByType<DebugInteractable>();
            var hud = Object.FindFirstObjectByType<FirstPersonHud>();
            Assert.That(interaction, Is.Not.Null);
            Assert.That(target, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);

            yield return null;

            Assert.That(interaction.HasCurrentTarget, Is.True);
            Assert.That(interaction.CurrentTargetCanInteract, Is.True);

            var promptText = FindHudText(hud, "Interaction Prompt Text");
            Assert.That(promptText, Is.Not.Null);
            Assert.That(promptText.enabled, Is.True);
            Assert.That(promptText.text, Does.Contain(target.DisplayName));
        }

        private static IEnumerator LoadCargoRunMvp()
        {
            SceneManager.LoadScene("CargoRunMvp");
            yield return null;
            yield return null;
        }

        private static int RenderedScenePixelCount(Camera camera)
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
