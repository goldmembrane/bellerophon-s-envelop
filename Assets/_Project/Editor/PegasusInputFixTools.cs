using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Bellerophon.Core.Player;

namespace Bellerophon.Editor
{
    [InitializeOnLoad]
    internal static class PegasusInputFixTools
    {
        const string Dir = "docs/validation/PegasusInputFix";
        static PegasusInputFixTools()
        {
            AssemblyReloadEvents.beforeAssemblyReload += ClearObservers;
            EditorApplication.playModeStateChanged += state => { if (state == PlayModeStateChange.EnteredEditMode) ClearObservers(); };
        }
        static void ClearObservers()
        {
            foreach (var c in Resources.FindObjectsOfTypeAll<Camera>().Where(c =>
                (c.name == "ExteriorTransferObserver" || c.name == "ExteriorReviewObserver") &&
                (c.gameObject.hideFlags & HideFlags.DontSave) != 0).ToArray())
                UnityEngine.Object.DestroyImmediate(c.gameObject);
        }
        internal static void Run()
        {
            var action = File.ReadAllText(Dir + "/Action.txt").Trim();
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/_Project/Scenes/Pegasus.unity") throw new InvalidOperationException("Pegasus must be active.");
            if (action == "install")
            {
                if (Application.isPlaying || scene.isDirty) throw new InvalidOperationException("Requires clean Edit Mode.");
                if (scene.GetRootGameObjects().Any(r => r.GetComponentInChildren<FirstPersonPlayerInput>(true))) throw new InvalidOperationException("Player already exists.");
                File.Copy(scene.path, Dir + "/PegasusBefore.unity.txt", false);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/Player.prefab");
                var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                player.transform.SetPositionAndRotation(new Vector3(56.08f, 3.05f, 34.79f), Quaternion.identity);
                PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Inspect();
            }
            else if (action == "enter") { ClearObservers(); EditorApplication.isPlaying = true; }
            else if (action == "exit") { Application.runInBackground = PlayerSettings.runInBackground; EditorApplication.isPlaying = false; }
            else if (action == "startPosition")
            {
                if (Application.isPlaying || scene.isDirty) throw new InvalidOperationException("Requires clean Edit Mode.");
                var player = scene.GetRootGameObjects().Single(r => r.GetComponent<FirstPersonPlayerInput>());
                player.transform.position = new Vector3(56.08f, 3.05f, 32.0f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            else if (action == "capture")
            {
                Application.runInBackground = true;
                Capture("Current");
            }
            else if (action == "exercise") Exercise();
            else Inspect();
        }
        static void Capture(string name)
        {
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            ScreenCapture.CaptureScreenshot(Dir + "/" + name + ".png");
        }
        static double until;
        static Keyboard keyboard;
        static Mouse mouse;
        static void Exercise()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Requires Play Mode.");
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            until = EditorApplication.timeSinceStartup + 1.0;
            EditorApplication.update += Drive;
        }
        static void Drive()
        {
            if (!Application.isPlaying || EditorApplication.timeSinceStartup >= until)
            {
                EditorApplication.update -= Drive;
                InputSystem.RemoveDevice(keyboard);
                InputSystem.RemoveDevice(mouse);
                if (Application.isPlaying) { Inspect(); Capture("AfterInput"); }
                return;
            }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(8, 0) });
        }
        internal static void Inspect()
        {
            Directory.CreateDirectory(Dir);
            var s = SceneManager.GetActiveScene();
            var b = new StringBuilder($"Scene={s.path} Play={Application.isPlaying} Dirty={s.isDirty} Paused={EditorApplication.isPaused} TimeScale={Time.timeScale} Focus={Application.isFocused} Frame={Time.frameCount}\n");
            foreach (var r in s.GetRootGameObjects()) b.AppendLine($"ROOT {r.name} active={r.activeSelf} pos={r.transform.position}");
            foreach (var c in Camera.allCameras) b.AppendLine($"OUTPUT {c.name} scene={c.gameObject.scene.name} depth={c.depth} pos={c.transform.position} target={c.targetTexture}");
            foreach (var c in s.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>(true)))
                b.AppendLine($"CAM {c.name} active={c.isActiveAndEnabled} pos={c.transform.position} rot={c.transform.eulerAngles} depth={c.depth}");
            foreach (var p in s.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<FirstPersonPlayerInput>(true)))
                b.AppendLine($"INPUT {p.name} active={p.isActiveAndEnabled} suppressed={p.GameplayActionInputSuppressed}");
            foreach (var c in s.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Collider>(true)).Where(c => c.enabled && c.gameObject.activeInHierarchy && c.bounds.size.x > 3 && c.bounds.size.z > 3 && c.bounds.size.y < 1))
                b.AppendLine($"FLOOR {c.name} center={c.bounds.center} size={c.bounds.size}");
            File.WriteAllText(Dir + "/Inspection.txt", b.ToString());
            var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
            var logs = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries");
            var entryType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntry");
            var entry = Activator.CreateInstance(entryType);
            var report = new StringBuilder();
            logs.GetMethod("StartGettingEntries", flags).Invoke(null, null);
            try
            {
                var count = (int)logs.GetMethod("GetCount", flags).Invoke(null, null);
                report.AppendLine("Entries=" + count);
                for (var i=0; i<count; i++)
                {
                    logs.GetMethod("GetEntryInternal", flags).Invoke(null, new object[]{i, entry});
                    report.AppendLine(entryType.GetField("message", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)?.GetValue(entry)?.ToString());
                }
            }
            finally { logs.GetMethod("EndGettingEntries", flags).Invoke(null, null); }
            File.WriteAllText(Dir + "/Console.txt", report.ToString());
        }
    }
}
