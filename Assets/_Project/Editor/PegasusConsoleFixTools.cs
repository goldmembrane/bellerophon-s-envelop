using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace Bellerophon.Editor
{
    [InitializeOnLoad]
    internal static class PegasusConsoleFixTools
    {
        const string Dir="docs/validation/PegasusExterior/ConsoleFix/";
        const string Key="PegasusConsoleFix";
        const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
        static PegasusConsoleFixTools(){Application.logMessageReceived+=Record;EditorApplication.playModeStateChanged+=s=>Record("PLAY_STATE "+s,"",LogType.Log);}
        static void Record(string message,string stack,LogType type)
        {
            if(!SessionState.GetBool(Key,false))return;
            Directory.CreateDirectory(Dir);
            File.AppendAllText(Dir+"LiveLog.txt",DateTime.UtcNow.ToString("O")+" "+type+" "+message+"\n"+stack+"\n");
        }
        static PropertyInfo LeakMode()=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Collections.NativeLeakDetection")).First(t=>t!=null).GetProperty("Mode",Flags);
        static string Path(Transform t)=>t.parent?Path(t.parent)+"/"+t.name:t.name;
        static void Snapshot(string name)
        {
            var asm=typeof(EditorWindow).Assembly;var logs=asm.GetType("UnityEditor.LogEntries");var et=asm.GetType("UnityEditor.LogEntry");var entry=Activator.CreateInstance(et);
            var sb=new StringBuilder("Play="+EditorApplication.isPlaying+" scene="+SceneManager.GetActiveScene().path+" dirty="+SceneManager.GetActiveScene().isDirty+"\n");
            sb.AppendLine("NativeLeakDetection="+LeakMode().GetValue(null));
            var jiggle=Type.GetType("GatorDragonGames.JigglePhysics.JigglePhysics, com.gator-dragon-games.jigglephysics");
            sb.AppendLine("JigglePackageLoaded="+(jiggle!=null)+" GlobalJobsAllocated="+(jiggle?.GetField("jobs",Flags)?.GetValue(null)!=null));
            logs.GetMethod("StartGettingEntries",Flags).Invoke(null,null);
            try{int count=(int)logs.GetMethod("GetCount",Flags).Invoke(null,null);for(int i=0;i<count;i++){logs.GetMethod("GetEntryInternal",Flags).Invoke(null,new[]{(object)i,entry});foreach(string field in new[]{"mode","message"})sb.AppendLine(field+"="+et.GetField(field,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(entry));}}
            finally{logs.GetMethod("EndGettingEntries",Flags).Invoke(null,null);}
            File.WriteAllText(Dir+name+".txt",sb.ToString());
        }
        internal static void Run(string json)
        {
            Directory.CreateDirectory(Dir);var op=JsonUtility.FromJson<PegasusExteriorTransferTools.Op>(json);
            if(op.action=="consoleFixBegin")
            {
                if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Saved edit scene required");
                Snapshot("BaselineConsole");
                foreach(var file in new[]{"Assets/_Project/Scenes/Pegasus.unity","Assets/Settings/PC_RPAsset.asset"})
                {var dest=Dir+System.IO.Path.GetFileName(file)+".before.txt";if(!File.Exists(dest))File.Copy(file,dest);}
                var p=LeakMode();if(!SessionState.GetBool(Key,false))SessionState.SetInt(Key+"Mode",Convert.ToInt32(p.GetValue(null)));
                SessionState.SetBool(Key,true);p.SetValue(null,Enum.Parse(p.PropertyType,"EnabledWithStackTrace"));
                Record("Diagnostic stack tracing enabled; original="+SessionState.GetInt(Key+"Mode",-1),"",LogType.Log);
            }
            if(op.action=="consoleFixEnd"){var p=LeakMode();p.SetValue(null,Enum.ToObject(p.PropertyType,SessionState.GetInt(Key+"Mode",1)));SessionState.SetBool(Key,false);}
            if(op.action=="consoleFixSnapshot")Snapshot(op.name);
            if(op.action=="consoleFixShadows")
            {
                var scene=SceneManager.GetActiveScene();
                if(EditorApplication.isPlaying||scene.isDirty||scene.path!=PegasusExteriorSampleTools.SourcePath)throw new InvalidOperationException("Saved Pegasus required");
                var lights=scene.GetRootGameObjects().Where(r=>r.name.StartsWith("Cargo ")&&r.name.EndsWith(" Corridor and Contacts")).SelectMany(r=>r.GetComponentsInChildren<Light>(true)).Where(l=>l.type==LightType.Point && l.shadows!=LightShadows.None).ToArray();
                if(lights.Length!=25)throw new InvalidOperationException("Expected 25 corridor shadow lights");
                foreach(var l in lights)
                {
                    var data=l.GetComponents<Component>().Single(c=>c && c.GetType().Name=="UniversalAdditionalLightData");
                    var so=new SerializedObject(data);so.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=-1;so.ApplyModifiedPropertiesWithoutUndo();
                    // URP custom tiers read shadowResolution, not the built-in shadowCustomResolution field.
                    var lightSettings=new SerializedObject(l);
                    lightSettings.FindProperty("m_Shadows.m_Resolution").intValue=256;
                    lightSettings.FindProperty("m_Shadows.m_CustomResolution").intValue=-1;
                    lightSettings.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(l);
                }
                var pipelineSettings=new SerializedObject(GraphicsSettings.currentRenderPipeline);
                pipelineSettings.FindProperty("m_AdditionalLightsShadowmapResolution").intValue=4096;
                pipelineSettings.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssetIfDirty(GraphicsSettings.currentRenderPipeline);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                Record("APPLIED 25 point-light custom shadow faces=256 atlas=4096 shadows retained","",LogType.Log);
            }
            if(op.action=="consoleFixInventory")
            {
                var sb=new StringBuilder("Pipeline="+AssetDatabase.GetAssetPath(GraphicsSettings.currentRenderPipeline)+"\n");
                foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    foreach(var c in root.GetComponentsInChildren<Component>(true))if(c && (c.GetType().FullName.Contains("Rig")||c.GetType().FullName.Contains("Jiggle")))sb.AppendLine("MOTION "+Path(c.transform)+" "+c.GetType().AssemblyQualifiedName+" active="+c.gameObject.activeInHierarchy);
                    foreach(var l in root.GetComponentsInChildren<Light>(true))
                    {sb.AppendLine("LIGHT "+Path(l.transform)+" type="+l.type+" enabled="+l.isActiveAndEnabled+" shadows="+l.shadows+" range="+l.range+" resolution="+l.shadowResolution+" custom="+l.shadowCustomResolution);
                    foreach(var c in l.GetComponents<Component>())if(c && c.GetType().Name=="UniversalAdditionalLightData")sb.AppendLine(EditorJsonUtility.ToJson(c));}
                }
                File.WriteAllText(Dir+"Inventory.txt",sb.ToString());
            }
        }
    }
}
