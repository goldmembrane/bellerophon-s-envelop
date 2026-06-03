using System;
using Bellerophon.Core.Player;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellerophon.Editor.Validation
{
    public static class Phase16HudMapAtmosphereEditorValidation
    {
        public static void Run()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase16HudMapAtmosphereBootstrap.CargoRunScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException("Missing CargoRunMvp scene for Phase 16 validation.");
            }

            if (SceneManager.GetActiveScene().path != Phase16HudMapAtmosphereBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase16HudMapAtmosphereBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            var root = GameObject.Find(Phase16HudMapAtmosphereBootstrap.Phase16RootName);
            var uiRoot = GameObject.Find(Phase16HudMapAtmosphereBootstrap.Phase16UiRootName);
            var hud = UnityEngine.Object.FindFirstObjectByType<FirstPersonHud>();
            var map = UnityEngine.Object.FindFirstObjectByType<ShipInteriorMapHud>();
            var atmosphere = UnityEngine.Object.FindFirstObjectByType<ShipInteriorAtmosphereController>();
            var audioHooks = UnityEngine.Object.FindFirstObjectByType<ShipSignalAudioHooks>();
            var equipmentController = UnityEngine.Object.FindFirstObjectByType<PlayerEquipmentController>();
            if (root == null ||
                uiRoot == null ||
                hud == null ||
                map == null ||
                atmosphere == null ||
                audioHooks == null ||
                equipmentController == null)
            {
                throw new InvalidOperationException("Phase 16 HUD, map, atmosphere, audio, or equipment wiring is incomplete.");
            }

            hud.ResolveGeneratedHudReferencesForValidation();
            if (hud.HealthText == null ||
                hud.ShieldText == null ||
                hud.HealthFillImage == null ||
                hud.ShieldFillImage == null)
            {
                throw new InvalidOperationException("Phase 16 health and shield bar references are missing.");
            }

            if (hud.HealthFillImage.type != Image.Type.Filled ||
                hud.ShieldFillImage.type != Image.Type.Filled ||
                Mathf.Abs(hud.HealthFillImage.fillAmount - 1f) > 0.001f ||
                Mathf.Abs(hud.ShieldFillImage.fillAmount - 1f) > 0.001f)
            {
                throw new InvalidOperationException("Phase 16 health and shield bars must start full and use filled images.");
            }

            AssertTextNonBlocking(hud.HealthText, "health percent");
            AssertTextNonBlocking(hud.ShieldText, "shield percent");
            AssertTextAlignedWithBar(hud.HealthText, hud.HealthFillImage, "health");
            AssertTextAlignedWithBar(hud.ShieldText, hud.ShieldFillImage, "shield");
            AssertDefaultCrosshairHidden();

            if (equipmentController.PrecisionAimReticleText == null ||
                equipmentController.PrecisionAimReticleText.enabled ||
                !string.IsNullOrEmpty(equipmentController.PrecisionAimReticleText.text))
            {
                throw new InvalidOperationException("Phase 16 must keep precision reticle hidden until musket precision mode is toggled on.");
            }

            if (map.MapRoot == null ||
                map.CurrentRoomText == null ||
                map.CurrentRoomMarker == null ||
                Mathf.Abs(map.MapRoot.localScale.x - ShipInteriorMapRules.ShipInteriorMapScale) > 0.001f ||
                Mathf.Abs(map.MapRoot.localScale.y - ShipInteriorMapRules.ShipInteriorMapScale) > 0.001f)
            {
                throw new InvalidOperationException("Phase 16 ship map must be wired and scaled to 80%.");
            }

            map.RefreshForValidation();
            if (map.CurrentRoom != ShipRoomId.CargoHold ||
                !map.CurrentRoomText.text.Contains("Cargo Hold"))
            {
                throw new InvalidOperationException("Phase 16 map must show the Cargo Hold as the player start room.");
            }

            var cargoMapRoom = ShipInteriorMapRules.GetRoom(ShipRoomId.CargoHold);
            if (Vector2.Distance(map.CurrentRoomMarker.sizeDelta, cargoMapRoom.MapSize * ShipInteriorMapRules.ShipInteriorMapScale) > 0.01f)
            {
                throw new InvalidOperationException("Phase 16 current room marker must use the 80% room size.");
            }

            if (!RenderSettings.fog ||
                RenderSettings.fogMode != FogMode.ExponentialSquared ||
                RenderSettings.fogDensity < ShipInteriorAtmosphereController.TargetFogDensity * 0.9f)
            {
                throw new InvalidOperationException("Phase 16 atmosphere must enable dense dark fog.");
            }

            var camera = Camera.main;
            if (camera == null ||
                camera.farClipPlane > ShipInteriorAtmosphereController.TargetCameraFarClip + 0.01f)
            {
                throw new InvalidOperationException("Phase 16 atmosphere must limit camera visibility distance.");
            }

            audioHooks.TriggerShipDamageSignal();
            audioHooks.TriggerExternalDangerSignal();
            audioHooks.TriggerIntruderSignal();
            if (audioHooks.ShipDamageSignalCount < 1 ||
                audioHooks.ExternalDangerSignalCount < 1 ||
                audioHooks.IntruderSignalCount < 1 ||
                audioHooks.LastCue != ShipSignalAudioCue.IntruderSignal)
            {
                throw new InvalidOperationException("Phase 16 audio hooks must expose ship damage, external danger, and intruder signal triggers.");
            }

            AssertHudDoesNotOverlapPrompt(hud, map.MapRoot);
            Debug.Log("Phase 16 HUD map atmosphere editor validation passed.");
            Debug.Log("Phase 16 HUD map atmosphere details: Vitals=lower-left; Map=lower-right 80%; Crosshair=hidden; FogDensity=" + RenderSettings.fogDensity.ToString("0.000"));
        }

        private static void AssertDefaultCrosshairHidden()
        {
            var labels = UnityEngine.Object.FindObjectsByType<Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < labels.Length; i++)
            {
                if (labels[i].name != "Crosshair Text")
                {
                    continue;
                }

                if (labels[i].enabled || !string.IsNullOrEmpty(labels[i].text))
                {
                    throw new InvalidOperationException("Phase 16 default center crosshair must be hidden.");
                }
            }
        }

        private static void AssertTextNonBlocking(Text text, string label)
        {
            if (text == null)
            {
                throw new InvalidOperationException("Missing Phase 16 " + label + " text.");
            }

            if (text.raycastTarget)
            {
                throw new InvalidOperationException("Phase 16 " + label + " text must not block UI raycasts.");
            }
        }

        private static void AssertTextAlignedWithBar(Text text, Image fillImage, string label)
        {
            if (text == null || fillImage == null)
            {
                throw new InvalidOperationException("Phase 16 " + label + " HUD alignment check is missing references.");
            }

            var textRect = GetWorldRect(text.GetComponent<RectTransform>());
            var barRect = GetWorldRect(fillImage.GetComponent<RectTransform>());
            var textCenterY = textRect.yMin + (textRect.height * 0.5f);
            var barCenterY = barRect.yMin + (barRect.height * 0.5f);
            if (Mathf.Abs(textCenterY - barCenterY) > 1f)
            {
                throw new InvalidOperationException(
                    "Phase 16 " + label + " percent text must be vertically aligned with its bar.");
            }
        }

        private static void AssertHudDoesNotOverlapPrompt(FirstPersonHud hud, RectTransform mapRoot)
        {
            if (hud.InteractionPromptText == null)
            {
                throw new InvalidOperationException("Phase 16 requires the interaction prompt text reference.");
            }

            var promptRect = GetWorldRect(hud.InteractionPromptText.GetComponent<RectTransform>());
            var mapRect = GetWorldRect(mapRoot);
            var healthRect = GetWorldRect(hud.HealthFillImage.GetComponent<RectTransform>());
            var shieldRect = GetWorldRect(hud.ShieldFillImage.GetComponent<RectTransform>());

            if (Overlaps(promptRect, mapRect) || Overlaps(promptRect, healthRect) || Overlaps(promptRect, shieldRect))
            {
                throw new InvalidOperationException("Phase 16 HUD map and vitals must not overlap the interaction prompt.");
            }
        }

        private static bool Overlaps(Rect first, Rect second)
        {
            return first.xMin < second.xMax &&
                   first.xMax > second.xMin &&
                   first.yMin < second.yMax &&
                   first.yMax > second.yMin;
        }

        private static Rect GetWorldRect(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return Rect.zero;
            }

            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
    }
}
