using System;
using System.Collections;
using System.IO;
using System.Linq;
using Bellerophon.Core.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor
{
    internal static class PegasusPlayerHitFlashTools
    {
        internal const string Evidence = "docs/validation/player_hit_flash";
        static FirstPersonPlayerStatus Player() => UnityEngine.Object.FindObjectsByType<FirstPersonPlayerStatus>(FindObjectsSortMode.None)
            .Single(x => x.GetComponent<FirstPersonPlayerMotor>());

        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != "Assets/_Project/Scenes/Pegasus.unity" || scene.isDirty)
                throw new InvalidOperationException("Requires clean Pegasus in Edit mode; existing unsaved changes will not be saved.");
            var player = Player();
            if (!player.GetComponent<PlayerHitFlash>()) Undo.AddComponent<PlayerHitFlash>(player.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Approved hit flash connected only to Pegasus player: " + player.name);
        }

        public static void Observe()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play mode required.");
            Directory.CreateDirectory(Evidence);
            var player = Player();
            if (!player.GetComponent<PlayerHitFlash>()) throw new InvalidOperationException("Player flash missing.");
            if (!player.GetComponent<PlayerHitFlashObservation>()) player.gameObject.AddComponent<PlayerHitFlashObservation>();
            EditorWindow.GetWindow(typeof(EditorApplication).Assembly.GetType("UnityEditor.GameView")).Focus();
            Debug.Log("Actual damage / rendered Game view observation started; no damage or pose injection.");
        }

        public static void Review()
        {
            var player = Player(); var flash = player.GetComponent<PlayerHitFlash>();
            Debug.Log($"Hit flash: play={EditorApplication.isPlaying} scene={player.gameObject.scene.path} present={flash != null} alpha={(flash ? flash.Alpha : 0):F4} health={player.CurrentHealth} shield={player.CurrentShield}");
            if (File.Exists(Evidence + "/observations.txt")) Debug.Log(File.ReadAllText(Evidence + "/observations.txt"));
            PegasusLongaArmaTools.ReviewDeathPresence();
        }

        public static void CaptureFinal()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play mode required.");
            var observer = Player().GetComponent<PlayerHitFlashObservation>();
            if (!observer) throw new InvalidOperationException("Observation not started.");
            observer.StartCoroutine(observer.Capture("final"));
        }
    }

    [DefaultExecutionOrder(1000)]
    public sealed class PlayerHitFlashObservation : MonoBehaviour
    {
        FirstPersonPlayerStatus status;
        PlayerHitFlash flash;
        int hits;
        float nextMovementRecord;
        void Start()
        {
            status = GetComponent<FirstPersonPlayerStatus>(); flash = GetComponent<PlayerHitFlash>();
            File.AppendAllText(PegasusPlayerHitFlashTools.Evidence + "/observations.txt", "New run: actual damage events; Game view end-of-frame captures.\n");
            status.DamageTaken += Damaged;
            StartCoroutine(Capture("baseline"));
        }
        void OnDestroy() { if (status) status.DamageTaken -= Damaged; }
        void LateUpdate()
        {
            if (Time.unscaledTime < nextMovementRecord) return;
            nextMovementRecord = Time.unscaledTime + .5f;
            File.AppendAllText(PegasusPlayerHitFlashTools.Evidence + "/observations.txt", $"Observe time={Time.unscaledTime:F3} hits={hits} alpha={flash.Alpha:F4} position={transform.position:F3} yaw={transform.eulerAngles.y:F2}\n");
        }
        void Damaged()
        {
            hits++;
            File.AppendAllText(PegasusPlayerHitFlashTools.Evidence + "/observations.txt", $"Hit {hits} time={Time.unscaledTime:F3} health={status.CurrentHealth} shield={status.CurrentShield}\n");
            if (hits <= 4) StartCoroutine(Sequence(hits));
        }
        IEnumerator Sequence(int hit)
        {
            yield return new WaitForSecondsRealtime(.03f);
            yield return Capture("hit_" + hit);
            yield return new WaitForSecondsRealtime(.28f);
            yield return Capture("clear_" + hit);
        }
        public IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var pixels = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(PegasusPlayerHitFlashTools.Evidence + "/" + name + ".png", pixels.EncodeToPNG());
            Destroy(pixels);
            File.AppendAllText(PegasusPlayerHitFlashTools.Evidence + "/observations.txt", $"Frame {name} time={Time.unscaledTime:F3} elapsed={flash.Elapsed:F3} alpha={flash.Alpha:F4} position={transform.position:F3}\n");
        }
    }
}
