using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace Bellerophon.Editor
{
    // Sample-only optimization. Shared prefabs/materials and original Pegasus are never saved.
    internal static class PegasusExteriorOptimizationTools
    {
        const string Dir="docs/validation/PegasusExterior/Performance/Optimization/";
        static string PathOf(Transform t){return t.parent?PathOf(t.parent)+"/"+t.name:t.name;}
        static Scene Sample()
        {var s=SceneManager.GetActiveScene();if(s.path!=PegasusExteriorSampleTools.SamplePath)throw new InvalidOperationException("Exterior sample only.");return s;}
        internal static void State(string name)
        {
            var scene=Sample();var text=new StringBuilder("Play="+Application.isPlaying+" Dirty="+scene.isDirty+"\n");
            foreach(var root in scene.GetRootGameObjects())
            {
                foreach(var c in root.GetComponentsInChildren<Camera>(true))text.AppendLine("CAMERA|"+PathOf(c.transform)+"|"+c.isActiveAndEnabled+"|"+c.transform.position+"|"+c.pixelWidth+"x"+c.pixelHeight);
                foreach(var l in root.GetComponentsInChildren<Light>(true))text.AppendLine("LIGHT|"+PathOf(l.transform)+"|"+l.enabled+"|"+l.shadows+"|"+l.range+"|"+l.intensity);
                foreach(var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if(root.name=="SamplePresentation"||t.name.StartsWith("armory shell camera ")||root.name=="ExteriorReviewObserver")continue;
                    var r=t.GetComponent<Renderer>();var m=t.GetComponent<MeshFilter>();
                    text.AppendLine("GEOMETRY|"+PathOf(t)+"|"+t.localPosition.ToString("F7")+"|"+t.localRotation.ToString("F7")+"|"+t.localScale.ToString("F7")+"|"+t.gameObject.activeSelf+"|renderer="+(r?r.enabled.ToString():"none")+"|mesh="+(m&&m.sharedMesh?AssetDatabase.GetAssetPath(m.sharedMesh)+":"+m.sharedMesh.name:"none")+"|colliders="+t.GetComponents<Collider>().Length);
                    if(r)text.AppendLine("SHADOW|"+PathOf(t)+"|"+r.shadowCastingMode+"|"+r.receiveShadows);
                }
            }
            File.WriteAllText(Dir+name+".txt",text.ToString());
        }
        internal static void Apply()
        {
            var scene=Sample();if(Application.isPlaying)throw new InvalidOperationException("Edit Mode only.");
            var roots=scene.GetRootGameObjects();var interior=roots.Single(x=>x.name=="PreservedInterior");
            var log=new StringBuilder();
            var cameras=interior.GetComponentsInChildren<Camera>(true).Where(c=>c.name.StartsWith("armory shell camera ")).ToArray();
            foreach(var c in cameras){log.AppendLine("DELETE_CAMERA "+PathOf(c.transform));UnityEngine.Object.DestroyImmediate(c.gameObject);}
            foreach(var r in roots.Single(x=>x.name=="ShipExterior").GetComponentsInChildren<Renderer>(true))
            {r.enabled=false;r.shadowCastingMode=ShadowCastingMode.Off;log.AppendLine("EXTERIOR_OFF "+PathOf(r.transform));}
            var presentation=roots.Single(x=>x.name=="SamplePresentation");
            foreach(var l in presentation.GetComponentsInChildren<Light>(true).Where(l=>l.name.StartsWith("Exterior ")))
            {l.enabled=false;log.AppendLine("PRESENTATION_LIGHT_OFF "+PathOf(l.transform));}
            foreach(var l in interior.GetComponentsInChildren<Light>(true).Where(l=>l.name.StartsWith("Recessed corridor light ")))
            {
                l.shadows=LightShadows.Hard;
                log.AppendLine("CORRIDOR_SHADOW_HARD "+PathOf(l.transform));
            }
            foreach(var r in interior.GetComponentsInChildren<Renderer>(true))
            {
                var path=PathOf(r.transform);
                // Keep corridor renderers/mesh geometry and structural shadow casters intact.
                if(r.name.EndsWith("red closure warning strip")||r.name=="ER-09 asset screen corner bolt"||r.name=="ER-09 asset screen corner bolt slot"||r.name=="ER-15 charging dock corner bolt"||r.name=="ER-15 charging dock corner bolt slot")
                {r.shadowCastingMode=ShadowCastingMode.Off;log.AppendLine("DETAIL_SHADOW_OFF "+path);}
            }
            var camera=presentation.GetComponentInChildren<Camera>(true);
            camera.transform.position=new Vector3(88,4.2f,24);camera.transform.LookAt(new Vector3(82,4.2f,19));camera.fieldOfView=85;
            // Persist scene-instance overrides without modifying any source prefab.
            foreach(var root in roots)
            foreach(var component in root.GetComponentsInChildren<Component>(true))
                if(component && PrefabUtility.IsPartOfPrefabInstance(component))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText(Dir+"Changes.txt",log.ToString());
        }
        internal static void SaveInternalBatch()
        {
            var scene=Sample();
            if(Application.isPlaying || scene.isDirty)throw new InvalidOperationException("Saved Edit Mode sample required.");
            var interior=scene.GetRootGameObjects().Single(x=>x.name=="PreservedInterior");
            var report=new StringBuilder();
            foreach(Transform route in interior.transform)
            {
                if(!route.name.StartsWith("Cargo ")||!route.name.EndsWith(" Corridor and Contacts"))continue;
                var parts=route.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy
                    && (r.name.Contains(" Body18m ")||r.name.Contains(" WarehouseContact "))).ToArray();
                if(parts.Any(r=>r.GetComponentInParent<Rigidbody>()||r.GetComponentInParent<Animator>()))
                    throw new InvalidOperationException("Moving hierarchy cannot be statically batched.");
                var component=route.GetComponent<Bellerophon.Rendering.PegasusInteriorRenderBudget>();
                if(!component)component=route.gameObject.AddComponent<Bellerophon.Rendering.PegasusInteriorRenderBudget>();
                var serialized=new SerializedObject(component);var list=serialized.FindProperty("stationaryParts");list.arraySize=parts.Length;
                for(var i=0;i<parts.Length;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=parts[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                report.AppendLine(route.name+" explicit stationary renderers="+parts.Length);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText("docs/validation/PegasusExterior/Performance/InternalOptimization/Saved.txt",report.ToString());
        }
        internal static void InternalBatch(bool apply)
        {
            var scene=Sample();
            var report=new StringBuilder();
            var interior=scene.GetRootGameObjects().Single(x=>x.name=="PreservedInterior");
            foreach(Transform route in interior.transform)
            {
                if(!route.name.StartsWith("Cargo ")||!route.name.EndsWith(" Corridor and Contacts"))continue;
                var parts=route.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy
                    && (r.name.Contains(" Body18m ")||r.name.Contains(" WarehouseContact "))).ToArray();
                var candidates=parts.Where(r=>r.GetComponent<MeshFilter>()&&r.GetComponent<MeshFilter>().sharedMesh
                    &&r.GetComponent<MeshFilter>().sharedMesh.isReadable&&!r.isPartOfStaticBatch).Select(r=>r.gameObject).ToArray();
                report.AppendLine(route.name+" parts="+parts.Length+" readableCandidates="+candidates.Length+" materials="+parts.SelectMany(r=>r.sharedMaterials).Distinct().Count());
                if(apply)
                {
                    if(!Application.isPlaying)throw new InvalidOperationException("Batch experiment requires Play Mode.");
                    StaticBatchingUtility.Combine(candidates,route.gameObject);
                }
                report.AppendLine("batched="+parts.Count(r=>r.isPartOfStaticBatch));
            }
            File.WriteAllText("docs/validation/PegasusExterior/Performance/InternalOptimization/"+(apply?"BatchExperiment":"Inventory")+".txt",report.ToString());
        }
    }
}
