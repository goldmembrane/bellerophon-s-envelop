using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Bellerophon.Rendering;

namespace Bellerophon.Editor
{
    internal static class PegasusExteriorTransferTools
    {
        const string Sample=PegasusExteriorSampleTools.SamplePath;
        const string Target=PegasusExteriorSampleTools.SourcePath;
        const string Dir="docs/validation/PegasusExterior/Transfer/";
        [Serializable] internal class Op { public string action,name,room; public int index; public bool reverse; public Vector3 position,target; public float fov=85; }
        static Camera observer;
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        static void Clean()
        {
            Require(!EditorApplication.isPlaying,"Edit Mode required.");
            for(int i=0;i<SceneManager.sceneCount;i++)Require(!SceneManager.GetSceneAt(i).isDirty,"Unsaved scene: preserve user changes.");
        }
        static Scene Loaded(string path)
        {
            var s=SceneManager.GetSceneByPath(path);
            return s.IsValid()&&s.isLoaded?s:EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        }
        static GameObject Root(Scene s,string name)=>s.GetRootGameObjects().Single(x=>x.name==name);
        static Transform Find(Scene s,string path)
        {
            var first=path.IndexOf('/');var root=Root(s,first<0?path:path.Substring(0,first)).transform;
            var t=first<0?root:root.Find(path.Substring(first+1));
            Require(t!=null,"Missing exact path: "+path);return t;
        }
        static string Relative(string line)=>line.Substring(line.IndexOf(' ')+1).Replace("PreservedInterior/","");
        static string[] Changes()=>File.ReadAllLines("docs/validation/PegasusExterior/Performance/Optimization/FirstChanges.txt");
        static bool SameMaterials(Renderer a,Renderer b)
        {
            var aa=a.sharedMaterials;var bb=b.sharedMaterials;
            return aa.Length==bb.Length && aa.Zip(bb,(x,y)=>x==y || (x&&y&&EditorJsonUtility.ToJson(x)==EditorJsonUtility.ToJson(y))).All(x=>x);
        }
        static Dictionary<UnityEngine.Object,UnityEngine.Object> Correspond(Scene a,Scene b)
        {
            var map=new Dictionary<UnityEngine.Object,UnityEngine.Object>();
            var deletes=new HashSet<string>(Changes().Where(x=>x.StartsWith("DELETE_CAMERA ")).Select(Relative));
            foreach(Transform source in Root(a,"PreservedInterior").transform)Walk(source,Root(b,source.name).transform,source.name,map,deletes);
            return map;
        }
        static void Walk(Transform a,Transform b,string path,Dictionary<UnityEngine.Object,UnityEngine.Object> map,HashSet<string> deletes)
        {
            Require((a.position-b.position).sqrMagnitude<.000001f && Quaternion.Angle(a.rotation,b.rotation)<.01f && (a.lossyScale-b.lossyScale).sqrMagnitude<.000001f,"Transform mismatch: "+path);
            Require(a.gameObject.activeSelf==b.gameObject.activeSelf,"Activity mismatch: "+path);
            map[a]=b;map[a.gameObject]=b.gameObject;
            var ar=a.GetComponents<Component>().Where(c=>c && !(c is PegasusInteriorRenderBudget)).ToArray();
            var br=b.GetComponents<Component>().Where(c=>c && !(c is PegasusInteriorRenderBudget)).ToArray();
            Require(ar.Length==br.Length,"Component count mismatch: "+path);
            for(int i=0;i<ar.Length;i++)
            {
                Require(ar[i].GetType()==br[i].GetType(),"Component type mismatch: "+path);map[ar[i]]=br[i];
                if(ar[i] is MeshFilter am && br[i] is MeshFilter bm)
                    Require(am.sharedMesh==bm.sharedMesh || (am.sharedMesh && bm.sharedMesh && am.sharedMesh.name==bm.sharedMesh.name && am.sharedMesh.vertexCount==bm.sharedMesh.vertexCount && am.sharedMesh.vertices.SequenceEqual(bm.sharedMesh.vertices) && am.sharedMesh.triangles.SequenceEqual(bm.sharedMesh.triangles)),"Mesh mismatch: "+path);
                if(ar[i] is Renderer ra && br[i] is Renderer rb)
                {
                    if(ra.enabled!=rb.enabled || !SameMaterials(ra,rb))
                    {
                        var diagnostic=new StringBuilder(path+"\nSample enabled="+ra.enabled+" target enabled="+rb.enabled+"\n");
                        foreach(var m in ra.sharedMaterials)diagnostic.AppendLine("SAMPLE "+(m?m.name+" asset="+AssetDatabase.GetAssetPath(m)+"\n"+EditorJsonUtility.ToJson(m,true):"null"));
                        foreach(var m in rb.sharedMaterials)diagnostic.AppendLine("TARGET "+(m?m.name+" asset="+AssetDatabase.GetAssetPath(m)+"\n"+EditorJsonUtility.ToJson(m,true):"null"));
                        File.WriteAllText(Dir+"Mismatch.txt",diagnostic.ToString());
                    }
                    Require(ra.enabled==rb.enabled && SameMaterials(ra,rb),"Renderer/material mismatch: "+path);
                }
            }
            var aa=a.Cast<Transform>().ToArray();
            var bb=b.Cast<Transform>().Where(t=>!deletes.Contains(path+"/"+t.name)).ToArray();
            Require(aa.Length==bb.Length,"Child count mismatch: "+path);
            for(int i=0;i<aa.Length;i++){Require(aa[i].name==bb[i].name,"Child order mismatch: "+path);Walk(aa[i],bb[i],path+"/"+aa[i].name,map,deletes);}
        }
        static void Transfer(bool apply)
        {
            Clean();var source=Loaded(Sample);var target=Loaded(Target);
            var map=Correspond(source,target);var log=new StringBuilder();
            var changes=Changes();var deletes=new List<GameObject>();var lights=new List<KeyValuePair<Light,Light>>();var renderers=new List<KeyValuePair<Renderer,Renderer>>();
            foreach(var line in changes)
            {
                if(line.StartsWith("DELETE_CAMERA "))
                {
                    var p=Relative(line);var go=Find(target,p).gameObject;
                    Require(go.GetComponent<Camera>() && go.transform.childCount==0,"Not a leaf camera: "+p);
                    deletes.Add(go);log.AppendLine("DELETE "+p);
                }
                if(line.StartsWith("CORRIDOR_SHADOW_HARD ") || line.StartsWith("DETAIL_SHADOW_OFF "))
                {
                    var p=Relative(line);var a=Root(source,"PreservedInterior").transform.Find(p);Require(a!=null,"Missing sample path: "+p);
                    if(line.StartsWith("CORRIDOR_SHADOW_HARD "))lights.Add(new KeyValuePair<Light,Light>(a.GetComponent<Light>(),(Light)map[a.GetComponent<Light>()]));
                    else renderers.Add(new KeyValuePair<Renderer,Renderer>(a.GetComponent<Renderer>(),(Renderer)map[a.GetComponent<Renderer>()]));
                    log.AppendLine("COPY_SETTING "+p);
                }
            }
            var batches=Root(source,"PreservedInterior").GetComponentsInChildren<PegasusInteriorRenderBudget>(true);
            Require(deletes.Count==6 && lights.Count==25 && renderers.Count==46 && batches.Length==5,"Approved manifest counts differ.");
            foreach(var batch in batches)
            {
                Require(!((Transform)map[batch.transform]).GetComponent<PegasusInteriorRenderBudget>(),"Target already batched; inspect before reapply.");
                var list=new SerializedObject(batch).FindProperty("stationaryParts");
                Require(list.arraySize==740,"Sample batching membership changed.");
                for(int i=0;i<list.arraySize;i++)Require(map.ContainsKey(list.GetArrayElementAtIndex(i).objectReferenceValue),"Missing batch reference.");
                log.AppendLine("COPY_BATCH "+batch.name+" parts="+list.arraySize);
            }
            Require(!target.GetRootGameObjects().Any(o=>o.name=="ShipExterior"),"Existing target exterior: do not overwrite.");
            log.AppendLine("COPY ShipExterior (exact source instance; existing model/material references)");
            File.WriteAllText(Dir+"Plan.txt",log.ToString());
            if(!apply)return;
            File.Copy(Target,Dir+"PegasusBefore.unity.txt",false);
            SceneManager.SetActiveScene(target);
            var exterior=UnityEngine.Object.Instantiate(Root(source,"ShipExterior"));
            exterior.name="ShipExterior";SceneManager.MoveGameObjectToScene(exterior,target);
            foreach(var pair in lights){pair.Value.shadows=pair.Key.shadows;Record(pair.Value);}
            foreach(var pair in renderers){pair.Value.shadowCastingMode=pair.Key.shadowCastingMode;Record(pair.Value);}
            foreach(var batch in batches)
            {
                var dest=((Transform)map[batch.transform]).gameObject.AddComponent<PegasusInteriorRenderBudget>();
                var from=new SerializedObject(batch).FindProperty("stationaryParts");
                var so=new SerializedObject(dest);var to=so.FindProperty("stationaryParts");to.arraySize=from.arraySize;
                for(int i=0;i<from.arraySize;i++)to.GetArrayElementAtIndex(i).objectReferenceValue=map[from.GetArrayElementAtIndex(i).objectReferenceValue];
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach(var go in deletes)UnityEngine.Object.DestroyImmediate(go);
            EditorSceneManager.MarkSceneDirty(target);EditorSceneManager.SaveScene(target);
            File.WriteAllText(Dir+"Applied.txt",log.ToString());
            EditorSceneManager.CloseScene(source,true);
        }
        static void Record(Component c){if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
        static void CompleteDetailSettings()
        {
            Clean();var source=Loaded(Sample);var target=Loaded(Target);
            try
            {
                var map=Correspond(source,target);var paths=new HashSet<string>(Changes().Where(x=>x.StartsWith("DETAIL_SHADOW_OFF ")).Select(Relative));
                var pairs=new List<KeyValuePair<Renderer,Renderer>>();
                foreach(var pair in map)if(pair.Key is Renderer a && pair.Value is Renderer b)
                {
                    string path=a.name;var parent=a.transform.parent;
                    while(parent && parent.name!="PreservedInterior"){path=parent.name+"/"+path;parent=parent.parent;}
                    if(paths.Contains(path))pairs.Add(new KeyValuePair<Renderer,Renderer>(a,b));
                }
                Require(pairs.Count==46,"Exact approved detail membership required");
                var log=new StringBuilder();
                foreach(var pair in pairs)if(pair.Key.shadowCastingMode!=pair.Value.shadowCastingMode)
                {log.AppendLine(pair.Value.name+" sibling="+pair.Value.transform.GetSiblingIndex()+" "+pair.Value.shadowCastingMode+" -> "+pair.Key.shadowCastingMode);pair.Value.shadowCastingMode=pair.Key.shadowCastingMode;Record(pair.Value);}
                EditorSceneManager.MarkSceneDirty(target);EditorSceneManager.SaveScene(target);File.WriteAllText(Dir+"DetailCorrection.txt",log.ToString());
            }
            finally{EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(target);}
        }
        static void CompareSaved()
        {
            Clean();var source=Loaded(Sample);var target=Loaded(Target);
            try
            {
                var map=Correspond(source,target);
                Walk(Root(source,"ShipExterior").transform,Root(target,"ShipExterior").transform,"ShipExterior",map,new HashSet<string>());
                foreach(var pair in map)
                {
                    if(pair.Key is Renderer a && pair.Value is Renderer b)Require(a.shadowCastingMode==b.shadowCastingMode,"Shadow mismatch: "+a.name);
                    if(pair.Key is Light la && pair.Value is Light lb)Require(la.shadows==lb.shadows,"Light shadow mismatch: "+la.name);
                }
                foreach(var batch in Root(source,"PreservedInterior").GetComponentsInChildren<PegasusInteriorRenderBudget>(true))
                {
                    var dest=((Transform)map[batch.transform]).GetComponent<PegasusInteriorRenderBudget>();Require(dest,"Missing batch: "+batch.name);
                    var a=new SerializedObject(batch).FindProperty("stationaryParts");var b=new SerializedObject(dest).FindProperty("stationaryParts");
                    Require(a.arraySize==b.arraySize,"Batch size mismatch");
                    for(int i=0;i<a.arraySize;i++)Require(map[a.GetArrayElementAtIndex(i).objectReferenceValue]==b.GetArrayElementAtIndex(i).objectReferenceValue,"Batch member mismatch");
                }
                File.WriteAllText(Dir+"SavedComparison.txt","Saved sample and Pegasus: matching hierarchy, transforms, activity, meshes, materials, renderer enable/shadow settings, light shadow settings and exact batching membership.\n");
            }
            finally{EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(target);}
        }
        internal static void Run(string json)
        {
            Directory.CreateDirectory(Dir);var op=JsonUtility.FromJson<Op>(json);
            if(op.action=="transferCompleteDetails"){CompleteDetailSettings();return;}
            if(op.action=="transferCompare"){CompareSaved();return;}
            if(op.action=="transferInspect"){Transfer(false);return;}
            if(op.action=="transferApply"){Transfer(true);return;}
            if(op.action=="transferOpenSample"||op.action=="transferOpenTarget")
            {Clean();EditorSceneManager.OpenScene(op.action=="transferOpenSample"?Sample:Target,OpenSceneMode.Single);return;}
            var scene=SceneManager.GetActiveScene();Require(scene.path==Sample||scene.path==Target,"Unexpected review scene.");
            if(op.action=="transferEnter"){EditorApplication.isPlaying=true;return;}
            if(op.action=="transferExit"){EditorApplication.isPlaying=false;observer=null;return;}
            if(op.action=="transferFrame")
            {
                var view=SceneView.lastActiveSceneView;
                if(view!=null){view.LookAt(new Vector3(62,3,-5),Quaternion.Euler(25,225,0),120,false,true);view.Focus();}
                return;
            }
            if(op.action=="transferConsole")
            {
                var asm=typeof(EditorWindow).Assembly;var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic;
                var logs=asm.GetType("UnityEditor.LogEntries");var entryType=asm.GetType("UnityEditor.LogEntry");var entry=Activator.CreateInstance(entryType);
                var message=entryType.GetField("message",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
                var report=new StringBuilder("Scene="+scene.path+" Play="+EditorApplication.isPlaying+" Dirty="+scene.isDirty+"\n");logs.GetMethod("StartGettingEntries",flags).Invoke(null,null);
                try{var count=(int)logs.GetMethod("GetCount",flags).Invoke(null,null);for(int i=0;i<count;i++){logs.GetMethod("GetEntryInternal",flags).Invoke(null,new[]{(object)i,entry});report.AppendLine(Convert.ToString(message.GetValue(entry)));}}
                finally{logs.GetMethod("EndGettingEntries",flags).Invoke(null,null);}
                File.WriteAllText(Dir+op.name+".txt",report.ToString());return;
            }
            if(op.action=="transferState")
            {
                var text=new StringBuilder();
                foreach(var root in scene.GetRootGameObjects())foreach(var batch in root.GetComponentsInChildren<PegasusInteriorRenderBudget>(true))
                {
                    var list=new SerializedObject(batch).FindProperty("stationaryParts");int combined=0;
                    for(int i=0;i<list.arraySize;i++)if(((Renderer)list.GetArrayElementAtIndex(i).objectReferenceValue).isPartOfStaticBatch)combined++;
                    text.AppendLine(batch.name+" references="+list.arraySize+" batched="+combined);
                }
                File.WriteAllText(Dir+op.name+".txt",text.ToString());return;
            }
            Require(EditorApplication.isPlaying,"Play Mode required.");
            if(op.room!=null && op.room.Length>0)
            {
                var route=scene.path==Sample?Root(scene,"PreservedInterior").transform.Find("Cargo "+op.room+" Corridor and Contacts"):Root(scene,"Cargo "+op.room+" Corridor and Contacts").transform;
                Func<int,Vector3> point=i=>route.Find(op.room+" Body18m "+i+" 0").GetComponent<Renderer>().bounds.center+Vector3.up*1.7f;
                op.position=point(op.index);op.target=point(op.index+(op.reverse?-12:12));
            }
            if(!observer){var go=new GameObject("ExteriorTransferObserver"){hideFlags=HideFlags.DontSave};observer=go.AddComponent<Camera>();observer.depth=100;observer.clearFlags=CameraClearFlags.SolidColor;observer.backgroundColor=new Color(.009f,.014f,.021f);observer.nearClipPlane=.04f;observer.farClipPlane=700;}
            observer.transform.position=op.position;observer.transform.LookAt(op.target);observer.fieldOfView=op.fov;
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            var due=EditorApplication.timeSinceStartup+1;EditorApplication.CallbackFunction capture=null;
            capture=()=>{if(EditorApplication.timeSinceStartup<due)return;EditorApplication.update-=capture;ScreenCapture.CaptureScreenshot(Dir+op.name+".png");File.WriteAllText(Dir+op.name+".txt","Play="+EditorApplication.isPlaying+" scene="+scene.path+" camera="+op.position+" target="+op.target+" FOV="+op.fov);};EditorApplication.update+=capture;
        }
    }
}
