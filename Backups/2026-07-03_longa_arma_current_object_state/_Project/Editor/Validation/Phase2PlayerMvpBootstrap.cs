using System.IO;
using System.Linq;
using Bellerophon.Core;
using Bellerophon.Core.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellerophon.Editor.Validation
{
    public static class Phase2PlayerMvpBootstrap
    {
        private const string SettingsDirectory = "Assets/_Project/Settings/Player";
        private const string PrefabPlayerDirectory = "Assets/_Project/Prefabs/Player";
        private const string PrefabUiDirectory = "Assets/_Project/Prefabs/UI";
        private const string SceneDirectory = "Assets/_Project/Scenes";

        private const string PlayerSettingsPath = SettingsDirectory + "/DefaultFirstPersonPlayerSettings.asset";
        private const string PlayerPrefabPath = PrefabPlayerDirectory + "/Player.prefab";
        private const string HudPrefabPath = PrefabUiDirectory + "/Hud.prefab";
        private const string BootstrapScenePath = SceneDirectory + "/Bootstrap.unity";
        private const string CargoRunScenePath = SceneDirectory + "/CargoRunMvp.unity";

        // Phase 2 MVP visual materials keep the generated first-person scene readable in Game view.
        private const string FloorMaterialPath = SettingsDirectory + "/CargoBayFloorMaterial.mat";
        private const string WallMaterialPath = SettingsDirectory + "/CargoBayWallMaterial.mat";
        private const string TargetMaterialPath = SettingsDirectory + "/InteractionTargetMaterial.mat";
        private const float DefaultCrouchTransitionDuration = 0.22f;

        [MenuItem("Bellerophon/Bootstrap/Ensure Phase 2 Player MVP")]
        public static void EnsurePhase2Assets()
        {
            EnsureDirectories();
            var settings = EnsurePlayerSettings();
            var floorMaterial = EnsureMaterial(FloorMaterialPath, new Color(0.2f, 0.23f, 0.24f, 1f));
            var wallMaterial = EnsureMaterial(WallMaterialPath, new Color(0.32f, 0.42f, 0.5f, 1f));
            var targetMaterial = EnsureMaterial(TargetMaterialPath, new Color(0.94f, 0.63f, 0.24f, 1f));
            var playerPrefab = EnsurePlayerPrefab(settings);
            var hudPrefab = EnsureHudPrefab();
            EnsureCargoRunScene(playerPrefab, hudPrefab, floorMaterial, wallMaterial, targetMaterial);
            EnsureBootstrapLoadsCargoRunMvp();
            EnsureEditorPlayModeStartScene(CargoRunScenePath);
            EnsureSceneInBuildSettings(CargoRunScenePath);
            OpenCargoRunSceneForInspection();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Phase 2 player MVP assets are ready.");
        }

        private static void EnsureDirectories()
        {
            Directory.CreateDirectory(SettingsDirectory);
            Directory.CreateDirectory(PrefabPlayerDirectory);
            Directory.CreateDirectory(PrefabUiDirectory);
            Directory.CreateDirectory(SceneDirectory);
        }

        private static FirstPersonPlayerSettings EnsurePlayerSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<FirstPersonPlayerSettings>(PlayerSettingsPath);
            if (settings != null)
            {
                EnsurePlayerSettingsDefaults(settings);
                return settings;
            }

            settings = ScriptableObject.CreateInstance<FirstPersonPlayerSettings>();
            AssetDatabase.CreateAsset(settings, PlayerSettingsPath);
            EnsurePlayerSettingsDefaults(settings);
            return settings;
        }

        private static void EnsurePlayerSettingsDefaults(FirstPersonPlayerSettings settings)
        {
            var serializedSettings = new SerializedObject(settings);
            var crouchTransitionDuration = serializedSettings.FindProperty("crouchTransitionDuration");
            if (crouchTransitionDuration != null && crouchTransitionDuration.floatValue < 0.1f)
            {
                crouchTransitionDuration.floatValue = DefaultCrouchTransitionDuration;
                serializedSettings.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
            }
        }

        private static GameObject EnsurePlayerPrefab(FirstPersonPlayerSettings settings)
        {
            var player = new GameObject("Player");
            var characterController = player.AddComponent<CharacterController>();
            var input = player.AddComponent<FirstPersonPlayerInput>();
            var motor = player.AddComponent<FirstPersonPlayerMotor>();
            var status = player.AddComponent<FirstPersonPlayerStatus>();
            var inventory = player.AddComponent<FirstPersonHandInventory>();
            var interaction = player.AddComponent<FirstPersonInteractionController>();

            var cameraObject = new GameObject("Player Camera");
            cameraObject.transform.SetParent(player.transform);
            cameraObject.transform.localPosition = new Vector3(0f, settings.CameraStandingHeight, 0f);
            cameraObject.transform.localRotation = Quaternion.identity;
            cameraObject.tag = "MainCamera";

            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.13f, 1f);
            cameraObject.AddComponent<AudioListener>();

            characterController.height = settings.StandingHeight;
            characterController.radius = settings.CharacterRadius;
            characterController.center = new Vector3(0f, settings.StandingHeight * 0.5f, 0f);
            motor.Configure(settings, input, cameraObject.transform);
            status.Configure(settings);
            inventory.Configure(settings, input);
            interaction.Configure(settings, input, cameraObject.transform);

            var prefab = PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            Object.DestroyImmediate(player);
            return prefab;
        }

        private static GameObject EnsureHudPrefab()
        {
            var hud = new GameObject("Hud", typeof(RectTransform));
            NormalizeHudRootTransform(hud.GetComponent<RectTransform>());

            var canvas = hud.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = hud.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            hud.AddComponent<GraphicRaycaster>();

            var healthText = CreateHudText("Health Text", hud.transform, new Vector2(24f, -24f), "HP 100/100");
            var shieldText = CreateHudText("Shield Text", hud.transform, new Vector2(24f, -56f), "SH 50/50");
            CreateCrosshairText(hud.transform);
            var interactionPromptText = CreateInteractionPromptText(hud.transform);
            var hudComponent = hud.AddComponent<FirstPersonHud>();
            hudComponent.Configure(null, healthText, shieldText, null, interactionPromptText);

            var prefab = PrefabUtility.SaveAsPrefabAsset(hud, HudPrefabPath);
            Object.DestroyImmediate(hud);
            return prefab;
        }

        private static void NormalizeHudRootTransform(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }

        private static Text CreateHudText(string name, Transform parent, Vector2 anchoredPosition, string text)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            var rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(360f, 28f);

            var label = textObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 20;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = new Color(0.88f, 0.94f, 0.92f, 1f);
            label.text = text;
            return label;
        }

        private static Text CreateInteractionPromptText(Transform parent)
        {
            var textObject = new GameObject("Interaction Prompt Text");
            textObject.transform.SetParent(parent, false);

            var rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, -64f);
            rectTransform.sizeDelta = new Vector2(560f, 40f);

            var label = textObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 22;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.95f, 0.98f, 0.9f, 1f);
            label.text = string.Empty;
            label.enabled = false;
            return label;
        }

        private static void CreateCrosshairText(Transform parent)
        {
            var textObject = new GameObject("Crosshair Text");
            textObject.transform.SetParent(parent, false);

            var rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(48f, 48f);

            var label = textObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 28;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.92f, 0.98f, 0.96f, 1f);
            label.text = "+";
        }

        private static void EnsureCargoRunScene(
            GameObject playerPrefab,
            GameObject hudPrefab,
            Material floorMaterial,
            Material wallMaterial,
            Material targetMaterial)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "CargoRunMvp";

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.transform.position = new Vector3(0f, 0f, 0f);

            var hud = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab, scene);
            hud.GetComponent<FirstPersonHud>().Configure(
                player.GetComponent<FirstPersonPlayerStatus>(),
                hud.transform.Find("Health Text").GetComponent<Text>(),
                hud.transform.Find("Shield Text").GetComponent<Text>(),
                player.GetComponent<FirstPersonInteractionController>(),
                hud.transform.Find("Interaction Prompt Text").GetComponent<Text>());

            CreateCargoBayFloor(floorMaterial, wallMaterial);
            CreateInteractionTarget(targetMaterial);
            CreateLighting();

            EditorSceneManager.SaveScene(scene, CargoRunScenePath);
        }

        private static void EnsureBootstrapLoadsCargoRunMvp()
        {
            if (!File.Exists(BootstrapScenePath))
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            var loader = Object.FindFirstObjectByType<BootstrapSceneLoader>();
            if (loader == null)
            {
                var loaderObject = new GameObject("Bootstrap Scene Loader");
                loaderObject.AddComponent<BootstrapSceneLoader>();
            }

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        }

        private static void EnsureEditorPlayModeStartScene(string scenePath)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset != null)
            {
                EditorSceneManager.playModeStartScene = sceneAsset;
            }
        }

        private static void OpenCargoRunSceneForInspection()
        {
            if (!Application.isBatchMode)
            {
                EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            }
        }

        private static Material EnsureMaterial(string path, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateCargoBayFloor(Material floorMaterial, Material wallMaterial)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Cargo Bay Test Floor";
            floor.transform.position = new Vector3(0f, -0.05f, 2f);
            floor.transform.localScale = new Vector3(10f, 0.1f, 10f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;

            var backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "Cargo Bay Back Wall";
            backWall.transform.position = new Vector3(0f, 1.5f, 6.8f);
            backWall.transform.localScale = new Vector3(10f, 3f, 0.1f);
            backWall.GetComponent<MeshRenderer>().sharedMaterial = wallMaterial;
        }

        private static void CreateInteractionTarget(Material targetMaterial)
        {
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "Phase 2 Interaction Target";
            target.transform.position = new Vector3(0f, 1.45f, 2.6f);
            target.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            target.GetComponent<MeshRenderer>().sharedMaterial = targetMaterial;
            target.AddComponent<DebugInteractable>().Configure("Test Cargo Console", "Inspect", true);
        }

        private static void CreateLighting()
        {
            var lightObject = new GameObject("Cargo Bay Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientLight = new Color(0.08f, 0.09f, 0.1f);
        }

        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            var existingScenes = EditorBuildSettings.scenes.ToList();
            if (existingScenes.Any(scene => scene.path == scenePath))
            {
                EditorBuildSettings.scenes = existingScenes
                    .Select(scene => scene.path == scenePath ? new EditorBuildSettingsScene(scene.path, true) : scene)
                    .ToArray();
                return;
            }

            existingScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = existingScenes.ToArray();
        }
    }
}
