using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor
{
    internal static class PegasusExteriorSampleTools
    {
        internal const string SourcePath = "Assets/_Project/Scenes/Pegasus.unity";
        internal const string SampleDir = "Assets/_Project/ArtSamples/PegasusExterior";
        internal const string SamplePath = SampleDir + "/PegasusExterior.unity";
        internal const string ReportDir = "docs/validation/PegasusExterior";
        [Serializable] private class Item { public string name; public Vector3 position, rotation, scale, min, max; public bool active; public int renderers; }
        [Serializable] private class Layout { public string scene; public bool dirty; public Item[] roots; }
        internal static void Inspect()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Read source in Edit Mode only.");
            var scene = SceneManager.GetSceneByPath(SourcePath);
            if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Pegasus must already be open; source will not be opened or saved implicitly.");
            Directory.CreateDirectory(ReportDir);
            var items = new List<Item>();
            foreach (var root in scene.GetRootGameObjects())
            {
                var rs = root.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray();
                var b = new Bounds(root.transform.position, Vector3.zero);
                if (rs.Length > 0) { b = rs[0].bounds; foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds); }
                items.Add(new Item { name=root.name, position=root.transform.position, rotation=root.transform.eulerAngles, scale=root.transform.lossyScale, min=b.min, max=b.max, active=root.activeSelf, renderers=rs.Length });
            }
            File.WriteAllText(ReportDir+"/SourceLayout.json", JsonUtility.ToJson(new Layout { scene=scene.path, dirty=scene.isDirty, roots=items.ToArray() }, true));
        }
        [Serializable] private class Dimensions { public Vector3 center; public float halfWidth,halfLength,halfHeight; }
        [Serializable] private class Operation { public string action; public string name,room; public int index; public bool reverse; public Vector3 position,target; public float fov=45; }
        private static Camera observer;
        private static string Fingerprint(Scene scene)
        {
            var s=new StringBuilder();
            foreach(var root in scene.GetRootGameObjects().OrderBy(o=>o.name))
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
                s.AppendLine(root.name+"/"+PathOf(t,root.transform)+"|"+t.localPosition.ToString("F7")+"|"+t.localRotation.ToString("F7")+"|"+t.localScale.ToString("F7")+"|"+t.gameObject.activeSelf);
            return s.ToString();
        }
        private static string PathOf(Transform t,Transform root)
        { if(t==root)return t.name;return PathOf(t.parent,root)+"/"+t.GetSiblingIndex()+":"+t.name; }
        private static Material ExteriorMaterial(string name)
        {
            var path=SampleDir+"/Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null) { m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path); }
            var colors=new Dictionary<string,Color>{
                {"Hull",new Color(.19f,.225f,.245f)}, {"Panel",new Color(.34f,.37f,.38f)},
                {"Cargo",new Color(.245f,.29f,.315f)}, {"Tank",new Color(.3f,.335f,.35f)},
                {"Bronze",new Color(.48f,.29f,.12f)}, {"Black",new Color(.025f,.036f,.043f)},
                {"Glass",new Color(.026f,.1f,.145f)}, {"Solar",new Color(.035f,.07f,.12f)},
                {"Red",new Color(.8f,.025f,.012f)}, {"Cyan",new Color(.03f,.5f,.9f)},
                {"White",new Color(.9f,.77f,.5f)}, {"Marking",new Color(.65f,.7f,.69f)}};
            m.SetColor("_BaseColor",colors[name]);m.SetFloat("_Metallic",name=="Marking"?.05f:.65f);m.SetFloat("_Smoothness",name=="Glass"?.82f:.36f);
            if(name=="Red"||name=="Cyan"||name=="White") {m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",colors[name]*3);}
            else if(name!="Glass"&&name!="Solar"&&name!="Black")
            {
                m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(SampleDir+"/Textures/HullWear_Albedo.png"));
                m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(SampleDir+"/Textures/HullWear_Normal.png"));m.SetFloat("_BumpScale",.35f);m.EnableKeyword("_NORMALMAP");
                m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(SampleDir+"/Textures/HullWear_MetallicSmoothness.png"));m.EnableKeyword("_METALLICSPECGLOSSMAP");m.SetFloat("_Smoothness",.85f);
            }
            EditorUtility.SetDirty(m);return m;
        }
        internal static void Create()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Create only in Edit Mode.");
            var source=SceneManager.GetSceneByPath(SourcePath);
            if(!source.IsValid()||!source.isLoaded)throw new InvalidOperationException("Source Pegasus must remain loaded.");
            if(File.Exists(SamplePath))throw new InvalidOperationException("Sample already exists. Use scoped exterior refresh instead.");
            var before=Fingerprint(source);var dirty=source.isDirty;
            var layout=JsonUtility.FromJson<Layout>(File.ReadAllText(ReportDir+"/SourceLayout.json"));
            var names=new HashSet<string>(layout.roots.Select(i=>i.name));
            var originals=source.GetRootGameObjects().Where(o=>names.Contains(o.name)).ToArray();
            if(originals.Length!=14)throw new InvalidOperationException("Expected inspected 14 spatial roots.");
            Directory.CreateDirectory(SampleDir+"/Materials");AssetDatabase.Refresh();
            var metalMap=new Texture2D(2,2,TextureFormat.RGBA32,false,true);metalMap.LoadImage(File.ReadAllBytes(SampleDir+"/Textures/HullWear_Metallic.png"));
            var roughMap=new Texture2D(2,2,TextureFormat.RGBA32,false,true);roughMap.LoadImage(File.ReadAllBytes(SampleDir+"/Textures/HullWear_Roughness.png"));
            var mp=metalMap.GetPixels();var rp=roughMap.GetPixels();for(var i=0;i<mp.Length;i++)mp[i]=new Color(mp[i].r,mp[i].r,mp[i].r,1-rp[i].r);metalMap.SetPixels(mp);metalMap.Apply();
            var packedPath=SampleDir+"/Textures/HullWear_MetallicSmoothness.png";File.WriteAllBytes(packedPath,metalMap.EncodeToPNG());UnityEngine.Object.DestroyImmediate(metalMap);UnityEngine.Object.DestroyImmediate(roughMap);AssetDatabase.ImportAsset(packedPath);
            var packedImporter=(TextureImporter)AssetImporter.GetAtPath(packedPath);packedImporter.sRGBTexture=false;packedImporter.SaveAndReimport();
            var tex=AssetImporter.GetAtPath(SampleDir+"/Textures/HullWear_Normal.png") as TextureImporter;
            if(tex!=null){tex.textureType=TextureImporterType.NormalMap;tex.SaveAndReimport();}
            var sample=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(sample);
            var interior=new GameObject("PreservedInterior");
            var mapping=new Dictionary<UnityEngine.Object,UnityEngine.Object>();
            foreach(var original in originals)
            {
                var clone=UnityEngine.Object.Instantiate(original,interior.transform,false);clone.name=original.name;
                clone.transform.SetPositionAndRotation(original.transform.position,original.transform.rotation);clone.transform.localScale=original.transform.lossyScale;
                var from=original.GetComponentsInChildren<Transform>(true);var to=clone.GetComponentsInChildren<Transform>(true);
                if(from.Length!=to.Length)throw new InvalidOperationException("Clone hierarchy changed.");
                for(var i=0;i<from.Length;i++)
                {
                    mapping[from[i].gameObject]=to[i].gameObject;
                    var a=from[i].GetComponents<Component>();var b=to[i].GetComponents<Component>();
                    for(var j=0;j<a.Length;j++)if(a[j]!=null&&b[j]!=null)mapping[a[j]]=b[j];
                }
            }
            foreach(var c in interior.GetComponentsInChildren<Component>(true).Where(c=>c!=null&&!(c is Transform)))
            {
                var so=new SerializedObject(c);var p=so.GetIterator();bool any=false;
                while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference&&p.objectReferenceValue!=null&&mapping.TryGetValue(p.objectReferenceValue,out var replacement)){p.objectReferenceValue=replacement;any=true;}
                if(any)so.ApplyModifiedPropertiesWithoutUndo();
            }
            AddExterior(sample);
            var presentation=new GameObject("SamplePresentation");
            AddSun(presentation.transform,"Exterior key",new Vector3(42,-35,0),new Color(.85f,.92f,1),2.4f,true);
            AddSun(presentation.transform,"Exterior fill",new Vector3(18,125,0),new Color(.45f,.63f,1),1.1f,false);
            AddSun(presentation.transform,"Exterior rim",new Vector3(-15,180,0),new Color(1,.7f,.43f),1.2f,false);
            RenderSettings.skybox=null;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.26f,.3f,.36f);RenderSettings.fog=false;
            var cameraGo=new GameObject("SampleCamera");cameraGo.transform.SetParent(presentation.transform,false);var camera=cameraGo.AddComponent<Camera>();
            var d=JsonUtility.FromJson<Dimensions>(File.ReadAllText(ReportDir+"/ExteriorDimensions.json"));
            cameraGo.transform.position=d.center+new Vector3(160,100,150);cameraGo.transform.LookAt(d.center+Vector3.back*12);camera.fieldOfView=48;camera.nearClipPlane=.1f;camera.farClipPlane=600;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.009f,.014f,.021f);
            if(before!=Fingerprint(source)||source.isDirty!=dirty)throw new InvalidOperationException("Source state changed unexpectedly; sample is not accepted.");
            EditorSceneManager.SaveScene(sample,SamplePath);AssetDatabase.SaveAssets();
            File.WriteAllText(ReportDir+"/SourcePreservation.txt","Source scene not saved. Hierarchy transform/activity fingerprint unchanged. Source dirty before="+dirty+" after="+source.isDirty+". Copied roots="+originals.Length+"\n");
            File.WriteAllText(ReportDir+"/SourceFingerprint.txt",before);
            FrameSample();
        }
        private static void AddSun(Transform parent,string name,Vector3 rotation,Color color,float intensity,bool shadows)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.rotation=Quaternion.Euler(rotation);var l=go.AddComponent<Light>();l.type=LightType.Directional;l.color=color;l.intensity=intensity;l.shadows=shadows?LightShadows.Soft:LightShadows.None;l.shadowBias=.03f;l.shadowNormalBias=.15f;}
        private static void AddExterior(Scene scene)
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(SampleDir+"/Models/PegasusExterior.fbx");if(model==null)throw new InvalidOperationException("Exterior FBX is not imported.");
            var d=JsonUtility.FromJson<Dimensions>(File.ReadAllText(ReportDir+"/ExteriorDimensions.json"));
            var go=(GameObject)PrefabUtility.InstantiatePrefab(model,scene);go.name="ShipExterior";go.transform.position=d.center;go.transform.rotation=Quaternion.Euler(0,180,0);
            foreach(var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var key=r.name.Replace("Exterior_","");var material=AssetDatabase.LoadAssetAtPath<Material>(SampleDir+"/Materials/"+key+".mat") ?? ExteriorMaterial(key);r.sharedMaterials=Enumerable.Repeat(material,r.sharedMaterials.Length).ToArray();
            }
        }
        private static void FrameSample()
        {
            var d=JsonUtility.FromJson<Dimensions>(File.ReadAllText(ReportDir+"/ExteriorDimensions.json"));var view=SceneView.lastActiveSceneView;
            if(view!=null)view.LookAt(d.center+Vector3.back*12,Quaternion.Euler(25,225,0),120,false,true);
        }
        internal static void Review()
        {
            var op=JsonUtility.FromJson<Operation>(File.ReadAllText(ReportDir+"/Operation.json"));var scene=SceneManager.GetActiveScene();
            if(op.action.StartsWith("consoleFix")){PegasusConsoleFixTools.Run(File.ReadAllText(ReportDir+"/Operation.json"));return;}
            if(op.action.StartsWith("transfer")){PegasusExteriorTransferTools.Run(File.ReadAllText(ReportDir+"/Operation.json"));return;}
            if(scene.path!=SamplePath)throw new InvalidOperationException("Only exterior sample may be reviewed.");
            if(op.action=="showExterior")
            {
                if(EditorApplication.isPlaying || scene.isDirty)throw new InvalidOperationException("Saved Edit Mode sample required.");
                var exterior=scene.GetRootGameObjects().Single(o=>o.name=="ShipExterior");
                var renderers=exterior.GetComponentsInChildren<Renderer>(true);
                if(renderers.Any(r=>!r.gameObject.activeInHierarchy))throw new InvalidOperationException("Inactive exterior hierarchy requires separate inspection.");
                foreach(var renderer in renderers)
                {
                    renderer.enabled=true;
                    if(PrefabUtility.IsPartOfPrefabInstance(renderer))PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                FrameSample();return;
            }
            if(op.action=="optimize"){PegasusExteriorOptimizationTools.Apply();return;}
            if(op.action=="internalInventory"){PegasusExteriorOptimizationTools.InternalBatch(false);return;}
            if(op.action=="internalBatch"){PegasusExteriorOptimizationTools.InternalBatch(true);return;}
            if(op.action=="saveInternalBatch"){PegasusExteriorOptimizationTools.SaveInternalBatch();return;}
            if(op.action=="optimizationState"){PegasusExteriorOptimizationTools.State(op.name);return;}
            if(op.action=="optimizationReload")
            {
                if(EditorApplication.isPlaying || scene.isDirty)throw new InvalidOperationException("Reload requires saved Edit Mode sample.");
                EditorSceneManager.OpenScene(SamplePath,OpenSceneMode.Single);return;
            }
            if(op.action=="enter")
            {
                var source=SceneManager.GetSceneByPath(SourcePath);
                if(source.IsValid()&&source.isLoaded)
                {if(source.isDirty)throw new InvalidOperationException("Unsaved original: preserve it, do not close.");EditorSceneManager.CloseScene(source,true);}
                EditorApplication.isPlaying=true;return;
            }
            if(op.action=="exit"){EditorApplication.isPlaying=false;observer=null;return;}
            if(op.action=="console")
            {
                var asm=typeof(EditorWindow).Assembly;var logs=asm.GetType("UnityEditor.LogEntries");var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic;
                var entryType=asm.GetType("UnityEditor.LogEntry");var entry=Activator.CreateInstance(entryType);var message=entryType.GetField("message",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
                var report=new StringBuilder("Scene="+scene.path+" Play="+EditorApplication.isPlaying+" Dirty="+scene.isDirty+"\n");logs.GetMethod("StartGettingEntries",flags).Invoke(null,null);
                try{var count=(int)logs.GetMethod("GetCount",flags).Invoke(null,null);for(var i=0;i<count;i++){logs.GetMethod("GetEntryInternal",flags).Invoke(null,new[]{(object)i,entry});report.AppendLine(Convert.ToString(message.GetValue(entry)));}}
                finally{logs.GetMethod("EndGettingEntries",flags).Invoke(null,null);}File.WriteAllText(ReportDir+"/Console.txt",report.ToString());return;
            }
            if(op.action=="frame"){FrameSample();return;}
            if(op.action=="preservation")
            {
                var interior=scene.GetRootGameObjects().Single(o=>o.name=="PreservedInterior");var current=new StringBuilder();
                foreach(var root in interior.transform.Cast<Transform>().OrderBy(t=>t.name))
                foreach(var t in root.GetComponentsInChildren<Transform>(true))
                    current.AppendLine(root.name+"/"+PathOf(t,root)+"|"+t.localPosition.ToString("F7")+"|"+t.localRotation.ToString("F7")+"|"+t.localScale.ToString("F7")+"|"+t.gameObject.activeSelf);
                var same=current.ToString()==File.ReadAllText(ReportDir+"/SourceFingerprint.txt");
                File.WriteAllText(ReportDir+"/InteriorPreservation.txt","Copied interior transform/activity hierarchy equals source snapshot: "+same+"\n");
                if(!same)throw new InvalidOperationException("Copied interior hierarchy differs from source snapshot.");return;
            }
            if(op.action=="refreshExterior")
            {
                if(EditorApplication.isPlaying)throw new InvalidOperationException("Refresh sample exterior only in Edit Mode.");
                foreach(var old in scene.GetRootGameObjects().Where(o=>o.name=="ShipExterior").ToArray())UnityEngine.Object.DestroyImmediate(old);
                AddExterior(scene);
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();return;
            }
            if(op.action=="inspectSample")
            {
                var report=new StringBuilder();foreach(var root in scene.GetRootGameObjects())foreach(var r in root.GetComponentsInChildren<Renderer>(true).Where(r=>r.name.StartsWith("Exterior_")))report.AppendLine(r.name+" bounds="+r.bounds);
                File.WriteAllText(ReportDir+"/ExteriorBounds.txt",report.ToString());return;
            }
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Observe in actual Play Mode.");
            if(op.action=="cargoView")
            {
                var interior=scene.GetRootGameObjects().Single(o=>o.name=="PreservedInterior");
                var route=interior.transform.Find("Cargo "+op.room+" Corridor and Contacts");
                Func<int,Vector3> point=j=>route.Find(op.room+" Body18m "+j+" 0").GetComponent<Renderer>().bounds.center+Vector3.up*1.7f;
                op.position=point(op.index);op.target=point(op.index+(op.reverse?-12:12));
            }
            if(observer==null){var go=new GameObject("ExteriorReviewObserver"){hideFlags=HideFlags.DontSave};observer=go.AddComponent<Camera>();observer.depth=100;observer.clearFlags=CameraClearFlags.SolidColor;observer.backgroundColor=new Color(.009f,.014f,.021f);observer.nearClipPlane=.04f;observer.farClipPlane=700;}
            observer.transform.position=op.position;observer.transform.LookAt(op.target);observer.fieldOfView=op.fov;
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            var due=EditorApplication.timeSinceStartup+1;EditorApplication.CallbackFunction capture=null;
            capture=()=>{if(EditorApplication.timeSinceStartup<due)return;EditorApplication.update-=capture;ScreenCapture.CaptureScreenshot(ReportDir+"/"+op.name+".png");File.WriteAllText(ReportDir+"/"+op.name+".txt","Actual PlayMode="+EditorApplication.isPlaying+" scene="+scene.path+" camera="+op.position+" target="+op.target);};EditorApplication.update+=capture;
        }
    }
}
