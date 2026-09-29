using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Bellerophon.Enemies.Parvum;
using Bellerophon.Enemies.Fuga;

namespace Bellerophon.Editor
{
    internal static class PegasusSeedPlacementTools
    {
        const string Output = "docs/validation/pegasus_seed_placement";
        static readonly string[] Names = { "Parvum Gameplay 01", "Fuga Gameplay 01", "Longa Arma Gameplay 01" };
        static GameObject Root(string name) => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(x => x.GetComponentsInChildren<Transform>(true)).Single(x => x.name == name).gameObject;
        public static void Inspect()
        {
            var report = new StringBuilder();
            foreach (var name in Names.Concat(new[] { "Player", "Approved Cargo Hold 01 Shell" }))
            {
                var root = Root(name);
                report.AppendLine($"ROOT {name} active={root.activeSelf} position={root.transform.position:F3} scale={root.transform.lossyScale:F3}");
                foreach (var collider in root.GetComponents<Collider>())
                    report.AppendLine($"HULL {collider.GetType().Name} {EditorJsonUtility.ToJson(collider)}");
                if (name.Contains("Cargo Hold"))
                    foreach (var collider in root.GetComponentsInChildren<Collider>().Where(x => x.name.IndexOf("floor",StringComparison.OrdinalIgnoreCase)>=0 || x.name.IndexOf("header",StringComparison.OrdinalIgnoreCase)>=0))
                        report.AppendLine($"ROOM {collider.name} bounds={collider.bounds} position={collider.transform.position:F3}");
            }
            Debug.Log(report.ToString());
        }
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != "Assets/_Project/Scenes/Pegasus.unity" || scene.isDirty)
                throw new InvalidOperationException("Clean Pegasus Edit mode required.");
            var parvum = Root(Names[0]); var fuga = Root(Names[1]);
            var occupied = new System.Collections.Generic.List<Vector3> { Root("Player").transform.position, Root(Names[2]).transform.position };
            var doors = Root("Approved Cargo Hold 01 Shell").GetComponentsInChildren<Collider>()
                .Where(x => x.name.Contains("header")).Select(x => x.bounds.center).ToArray();
            var origin = Root(Names[2]).transform.position;
            var proposed = new Vector3[2];
            // Search only the same warehouse side as the existing test actor, preserving all room geometry.
            for (int actor = 0; actor < 2; actor++)
            {
                var candidates = new System.Collections.Generic.List<Vector3>();
                for (float x = origin.x - 6; x <= origin.x + 5; x += 1)
                for (float z = origin.z - 7; z <= origin.z + 7; z += 1)
                {
                    var p = new Vector3(x, origin.y + .5f, z);
                    var floor = Physics.RaycastAll(p, Vector3.down, 2, ~0, QueryTriggerInteraction.Ignore)
                        .Where(h => h.normal.y > .85f && h.collider.transform.IsChildOf(Root("Approved Cargo Hold 01 Shell").transform))
                        .OrderBy(h => h.distance).FirstOrDefault();
                    if (!floor.collider) continue;
                    p = floor.point;
                    if (occupied.Any(q => Vector3.ProjectOnPlane(p-q,Vector3.up).magnitude < 3.5f) ||
                        doors.Any(q => Vector3.ProjectOnPlane(p-q,Vector3.up).magnitude < 4)) continue;
                    if (Physics.OverlapCapsule(p+Vector3.up*1.05f,p+Vector3.up*2,1,~0,QueryTriggerInteraction.Ignore).Length > 0) continue;
                    candidates.Add(p);
                }
                if (candidates.Count == 0) throw new InvalidOperationException("No clear warehouse placement; scene unchanged.");
                proposed[actor] = candidates.OrderBy(p => Vector3.ProjectOnPlane(p-origin,Vector3.up).sqrMagnitude).First();
                occupied.Add(proposed[actor]);
            }
            Undo.RecordObjects(new UnityEngine.Object[] { parvum, parvum.transform, fuga, fuga.transform }, "Activate existing Pegasus seeds");
            parvum.transform.position = proposed[0] + Vector3.up*.02f;
            fuga.transform.position = proposed[1] + Vector3.up*fuga.GetComponent<FugaBrain>().FlightHeight;
            parvum.SetActive(true); fuga.SetActive(true);
            Physics.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log($"Activated existing actors only: Parvum={parvum.transform.position:F3}, Fuga={fuga.transform.position:F3}; Player/Longa unchanged.");
        }

        public static void Observe() => Capture(EditorApplication.isPlaying ? "live" : "placement");
        public static void Final() => Capture("final");
        public static void ReviewConsole()
        {
            var assembly=typeof(EditorApplication).Assembly;
            var entries=assembly.GetType("UnityEditor.LogEntries");var type=assembly.GetType("UnityEditor.LogEntry");
            var flags=System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;
            var count=(int)entries.GetMethod("StartGettingEntries",flags).Invoke(null,null);
            var lines=new System.Collections.Generic.HashSet<string>();
            try { for(int i=0;i<count;i++){var entry=Activator.CreateInstance(type);entries.GetMethod("GetEntryInternal",flags).Invoke(null,new[]{(object)i,entry});var field=type.GetField("message")??type.GetField("condition");if(field!=null)lines.Add((string)field.GetValue(entry));} }
            finally {entries.GetMethod("EndGettingEntries",flags).Invoke(null,null);}
            Directory.CreateDirectory(Output);File.WriteAllLines(Output+"/console.txt",lines);
            Debug.Log(string.Join("\n",lines.Where(x=>x.Contains("Warning")||x.Contains("shadow")||x.Contains("Shadow")||x.Contains("Render")||x.Contains("leak")).Take(12)));
        }
        public static void SavePlacement()
        {
            if(EditorApplication.isPlaying || SceneManager.GetActiveScene().path!="Assets/_Project/Scenes/Pegasus.unity")throw new InvalidOperationException("Pegasus Edit mode required.");
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        public static void HideParvumFuga()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying || scene.path!="Assets/_Project/Scenes/Pegasus.unity" || scene.isDirty)throw new InvalidOperationException("Clean Pegasus Edit mode required.");
            foreach(var name in Names.Take(2)){var actor=Root(name);Undo.RecordObject(actor,"Hide previous seed test actors");actor.SetActive(false);}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("Existing Parvum and Fuga disabled, not deleted; Longa and player unchanged.");
        }
        static void Capture(string label)
        {
            Directory.CreateDirectory(Output);
            var report = new StringBuilder();
            foreach (var name in Names)
            {
                var actor = Root(name);
                var renderers = actor.GetComponentsInChildren<Renderer>().Where(r => r.enabled && r.gameObject.activeInHierarchy && !(r is ParticleSystemRenderer)).ToArray();
                if (!actor.activeInHierarchy || renderers.Length == 0) throw new InvalidOperationException(name+" not visibly active");
                var bounds = renderers[0].bounds; foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
                var center = bounds.center;
                var directions = new[] {actor.transform.forward,-actor.transform.forward,actor.transform.right,-actor.transform.right};
                float distance = Mathf.Max(2.5f,bounds.size.magnitude*.9f);
                var direction = directions.OrderBy(d => Physics.RaycastAll(center,(d+Vector3.up*.2f).normalized,distance,~0,QueryTriggerInteraction.Ignore).Count(h=>!h.transform.IsChildOf(actor.transform))).First();
                var go = new GameObject("Temporary seed placement observation camera");
                var cam = go.AddComponent<Camera>(); cam.nearClipPlane=.03f;cam.farClipPlane=80;cam.fieldOfView=65;
                go.transform.position=center+direction*distance+Vector3.up*.65f;go.transform.LookAt(center);
                var rt = new RenderTexture(1000,750,24);var pixels=new Texture2D(1000,750,TextureFormat.RGB24,false);var previous=RenderTexture.active;
                try { cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1000,750),0,0);pixels.Apply();File.WriteAllBytes(Output+"/"+label+"_"+name.Split(' ')[0]+".png",pixels.EncodeToPNG()); }
                finally { RenderTexture.active=previous;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go); }
                report.AppendLine($"{name} active={actor.activeInHierarchy} position={actor.transform.position:F3} renderers={renderers.Length} bounds={bounds}");
                var p=actor.GetComponent<ParvumBrain>();var f=actor.GetComponent<FugaBrain>();
                if(p)report.AppendLine("Parvum state="+p.Behaviour);
                if(f)report.AppendLine("Fuga state="+f.Behaviour);
            }
            File.WriteAllText(Output+"/"+label+".txt",report.ToString());Debug.Log(report.ToString());
            PegasusLongaArmaTools.ReviewDeathPresence();
        }
    }
}
