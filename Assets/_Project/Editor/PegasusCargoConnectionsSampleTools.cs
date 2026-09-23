using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Linq;

namespace Bellerophon.Editor
{
    internal static class PegasusCargoConnectionsSampleTools
    {
        private const string ReportDirectory = "docs/validation/PegasusCargoConnections";
        private const string SampleDirectory = "Assets/_Project/ArtSamples/PegasusCargoConnections";
        private const string SamplePath = SampleDirectory + "/PegasusCargoConnections.unity";
        [Serializable] private class Port { public string id; public int side; public float shift; public Vector3[] points; }
        [Serializable] private class Plan { public float x,z,yaw,ceiling,floor; public Port[] ports; }
        private static Plan plan;
        private static Material wallMaterial, floorMaterial;
        private static Transform buildRoot;
        private static int meshIndex;
        private static Vector3 Bezier(Vector3[] c,float t)
        {
            var u=1-t;return u*u*u*c[0]+3*u*u*t*c[1]+3*u*t*t*c[2]+t*t*t*c[3];
        }
        private static List<Vector3> Path(Port port)
        {
            // Equal sloped walking length, not horizontal projection. Grade follows planar arc length.
            var c=(Vector3[])port.points.Clone();
            var lastDirection=c[2]-c[3];lastDirection.y=0;lastDirection.Normalize();
            var dy=c[3].y-c[0].y;
            var horizontal=Mathf.Sqrt(19*19-dy*dy);
            float lo=.1f,hi=18;
            for(var iteration=0;iteration<32;iteration++)
            {
                var h=(lo+hi)*.5f;c[2]=c[3]+lastDirection*h;
                var total=0f;var prev=c[0];
                for(var j=1;j<=1000;j++){var v=Bezier(c,j/1000f);total+=Vector2.Distance(new Vector2(v.x,v.z),new Vector2(prev.x,prev.z));prev=v;}
                if(total<horizontal)lo=h;else hi=h;
            }
            var raw=new List<Vector3>();var distances=new List<float>();float length=0;var previous=c[0];
            for(var j=0;j<=1000;j++)
            {
                var v=Bezier(c,j/1000f);length+=Vector2.Distance(new Vector2(v.x,v.z),new Vector2(previous.x,previous.z));
                raw.Add(v);distances.Add(length);previous=v;
            }
            var result=new List<Vector3>();
            for(var j=0;j<=190;j++)
            {
                var f=j/190f;var target=f*length;var k=distances.FindIndex(d=>d>=target);
                if(k<1)k=1;
                var v=Vector3.Lerp(raw[k-1],raw[k],Mathf.InverseLerp(distances[k-1],distances[k],target));
                v.y=Mathf.Lerp(c[0].y,c[3].y,f);result.Add(v);
            }
            return result;
        }
        private static void Solid(string name,Vector3[] v,Material material)
        {
            var mesh=new Mesh{name=name};
            var verts=new List<Vector3>();var tris=new List<int>();var uv=new List<Vector2>();
            var faces=new[]{new[]{0,3,2,1},new[]{4,5,6,7},new[]{0,1,5,4},new[]{3,7,6,2},new[]{0,4,7,3},new[]{1,2,6,5}};
            foreach(var face in faces)
            {
                var start=verts.Count;foreach(var ix in face)verts.Add(v[ix]);
                uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});
                tris.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
            }
            mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(SampleDirectory+"/Meshes/Part_"+(meshIndex++).ToString("D5")+".asset"));
            var go=new GameObject(name);go.transform.SetParent(buildRoot,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
            go.AddComponent<MeshCollider>().sharedMesh=mesh;
        }
        private static void Box(string name,Vector3 center,Vector3 size,Quaternion rotation,Material material)
        {
            var v=new Vector3[8];var offsets=new[]{new Vector3(-1,-1,-1),new Vector3(1,-1,-1),new Vector3(1,1,-1),new Vector3(-1,1,-1),new Vector3(-1,-1,1),new Vector3(1,-1,1),new Vector3(1,1,1),new Vector3(-1,1,1)};
            for(var i=0;i<8;i++)v[i]=center+rotation*Vector3.Scale(offsets[i],size*.5f);
            Solid(name,v,material);
        }
        private static Vector3 CargoPoint(float x,float y,float z)
        {return new Vector3(plan.x,y,plan.z)+Quaternion.Euler(0,plan.yaw,0)*new Vector3(x,0,z);}
        private static void WarehouseWall(int side,float from,float to,float low,float high,string suffix)
        {
            if(to-from<.001f)return;
            var vertical=side%2==0;var mid=(from+to)*.5f;
            var x=vertical?(side==0?-11.319f:11.319f):mid;
            var z=vertical?mid:(side==1?10.0485f:-10.0485f);
            Box("Warehouse wall "+side+" "+suffix,CargoPoint(x,(low+high)*.5f,z),
                vertical?new Vector3(.36f,high-low,to-from):new Vector3(to-from,high-low,.36f),Quaternion.Euler(0,plan.yaw,0),wallMaterial);
        }
        private static void BuildWarehouse(GameObject cargo)
        {
            wallMaterial=cargo.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.Contains("sealed wall segment")).sharedMaterial;
            floorMaterial=cargo.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.Contains("deck floor")).sharedMaterial;
            var deltaRotation=Quaternion.Euler(0,plan.yaw,0);
            cargo.transform.position=new Vector3(plan.x,cargo.transform.position.y+plan.ceiling-5.5f,plan.z);
            cargo.transform.rotation=deltaRotation*cargo.transform.rotation;
            // Replace only the sample warehouse doorway walls/stubs; keep the deck, roof and interior.
            var remove=cargo.GetComponentsInChildren<Transform>(true).Where(t=>t!=cargo.transform &&
                ((t.parent==cargo.transform && (t.name.Contains("connection north wall")||t.name.Contains("connection wall")||t.name.Contains("connection west wall")||t.name.Contains("connection east wall")||t.name.Contains("corridor at")))||
                t.name.StartsWith("ShipSpaceCeiling_CargoHold_Entrance_Slab"))).ToArray();
            foreach(var t in remove)if(t!=null)UnityEngine.Object.DestroyImmediate(t.gameObject);
            var markerNames=new[]{"control 3 oclock","cockpit 12 oclock","engine 9 oclock","supply 7 oclock","armory 5 oclock"};
            var oldShift=new[]{0f,0f,0f,8.2698f,-8.2698f};
            foreach(var pair in plan.ports.Select((port,index)=>new{port,index}))
            {
                var marker=cargo.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.parent==cargo.transform&&t.name.Contains(markerNames[pair.index])&&t.name.EndsWith("direction marker"));
                if(marker!=null)marker.position+=deltaRotation*(pair.port.side%2==0?Vector3.forward:Vector3.right)*(pair.port.shift-oldShift[pair.index]);
            }
            buildRoot=new GameObject("Cargo Adjusted Entrances").transform;buildRoot.SetParent(cargo.transform,true);
            // Geometry is stored in world coordinates under an identity world transform.
            buildRoot.position=Vector3.zero;buildRoot.rotation=Quaternion.identity;buildRoot.localScale=new Vector3(1/cargo.transform.lossyScale.x,1/cargo.transform.lossyScale.y,1/cargo.transform.lossyScale.z);
            for(var side=0;side<4;side++)
            {
                var limit=side%2==0?10.0485f:11.319f;
                var doors=plan.ports.Where(p=>p.side==side).OrderBy(p=>p.shift).ToArray();var cursor=-limit-.18f;
                foreach(var door in doors)
                {
                    WarehouseWall(side,cursor,door.shift-1.35f,plan.floor-.4f,plan.ceiling,"solid");
                    WarehouseWall(side,door.shift-1.35f,door.shift+1.35f,plan.floor+4.45f,plan.ceiling,"header");
                    cursor=door.shift+1.35f;
                }
                WarehouseWall(side,cursor,limit+.18f,plan.floor-.4f,plan.ceiling,"solid end");
            }
        }
        private static void BuildTunnel(Port port,int index)
        {
            var points=Path(port);
            var startWidths=new[]{2.414f,4.948f,2.874f,2.866f,2.779f};
            var startHeights=new[]{5.083f-port.points[0].y,3.211f,2.033f,2.05f,1.96f};
            buildRoot=new GameObject("Cargo "+port.id+" Corridor and Contacts").transform;
            var rings=new List<Vector3[]>();
            for(var j=0;j<points.Count;j++)
            {
                var p=points[j];var tangent=points[Math.Min(j+1,points.Count-1)]-points[Math.Max(j-1,0)];tangent.y=0;tangent.Normalize();
                if(j==0){tangent=port.points[1]-port.points[0];tangent.y=0;tangent.Normalize();}
                if(j==points.Count-1){tangent=port.points[3]-port.points[2];tangent.y=0;tangent.Normalize();}
                var right=Vector3.Cross(Vector3.up,tangent).normalized;
                var width=j<5?Mathf.Lerp(startWidths[index],2.7f,j/5f):2.7f;
                var height=j<5?Mathf.Lerp(startHeights[index],2.7f,j/5f):j>185?Mathf.Lerp(2.7f,4.45f,(j-185)/5f):2.7f;
                rings.Add(new[]{p-right*width*.5f,p+right*width*.5f,p+right*width*.5f+Vector3.up*height,p-right*width*.5f+Vector3.up*height});
            }
            for(var j=0;j<points.Count-1;j++)
            {
                var zone=j<5?"RoomContact":j>=185?"WarehouseContact":"Body18m";
                for(var side=0;side<4;side++)
                {
                    var a=rings[j][side];var b=rings[j][(side+1)%4];var c=rings[j+1][(side+1)%4];var d=rings[j+1][side];
                    Vector3 offset;
                    if(side==0)offset=Vector3.down*.2f;else if(side==2)offset=Vector3.up*.2f;
                    else {offset=Vector3.Cross(b-a,d-a).normalized*.2f;var mid=(a+b+c+d)*.25f;var center=(points[j]+points[j+1])*.5f;if(Vector3.Dot(offset,mid-center)<0)offset=-offset;}
                    Solid(port.id+" "+zone+" "+j+" "+side,new[]{a,b,c,d,a+offset,b+offset,c+offset,d+offset},side==0?floorMaterial:wallMaterial);
                }
            }
            File.WriteAllText(ReportDirectory+"/"+port.id+"Path.json",JsonUtility.ToJson(new PathData{points=points.ToArray()},true));
        }
        [Serializable] private class PathData {public Vector3[] points;}
        private static void FinishConstruction(Scene scene)
        {
            if(scene.path!=SamplePath||EditorApplication.isPlaying)throw new InvalidOperationException("Sample Edit Mode only.");
            var cargo=scene.GetRootGameObjects().First(o=>o.name=="Approved Cargo Hold 01 Shell");
            var floorReport=new System.Text.StringBuilder();
            foreach(var filter in cargo.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name.Contains("floor")||f.name.Contains("walkway")))
            {
                floorReport.AppendLine(filter.name+" previous collider="+(filter.GetComponent<Collider>()!=null));
                foreach(var col in filter.GetComponents<Collider>())
                    floorReport.AppendLine("Collider="+col.GetType().Name+" enabled="+col.enabled+" trigger="+col.isTrigger+" bounds="+col.bounds+" meshBounds="+filter.GetComponent<Renderer>().bounds+" layer="+col.gameObject.layer);
                if(filter.GetComponent<Collider>()==null)filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
                foreach(var col in filter.GetComponents<Collider>())col.enabled=true;
            }
            if(cargo.transform.Find("Cargo Entry Panel Relocated")==null)
            {
                foreach(var t in cargo.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("CH-11")&&t.parent==cargo.transform))
                    t.position+=Quaternion.Euler(0,-20,0)*Vector3.forward*6;
                new GameObject("Cargo Entry Panel Relocated").transform.SetParent(cargo.transform,false);
            }
            foreach(var r in cargo.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.Contains("letter")))
                floorReport.AppendLine(r.name+" shader="+r.sharedMaterial.shader.name+" material="+AssetDatabase.GetAssetPath(r.sharedMaterial));
            File.WriteAllText(ReportDirectory+"/CargoFloorAndLabels.txt",floorReport.ToString());
            var textMaterial=AssetDatabase.LoadAssetAtPath<Material>(SampleDirectory+"/CargoDepthText.mat");
            if(textMaterial==null)
            {
                textMaterial=new Material(Shader.Find("Bellerophon/ArtSamples/CargoDepthText"));
                AssetDatabase.CreateAsset(textMaterial,SampleDirectory+"/CargoDepthText.mat");
            }
            foreach(var text in cargo.GetComponentsInChildren<TextMesh>(true))text.GetComponent<Renderer>().sharedMaterial=textMaterial;
            var depthScript = AssetDatabase.LoadAssetAtPath<MonoScript>(SampleDirectory + "/CargoSampleDepthText.cs");
            var depthType = depthScript == null ? null : depthScript.GetClass();
            if (depthType == null) throw new InvalidOperationException("Sample depth-text component has not compiled.");
            if(cargo.GetComponent(depthType)==null)cargo.AddComponent(depthType);
            var engine=scene.GetRootGameObjects().First(o=>o.name=="Cargo Engine Corridor and Contacts");
            if(engine.transform.Find("Engine entrance missing floor closure")==null)
            {
                buildRoot=engine.transform;
                floorMaterial=engine.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.EndsWith(" 0")).sharedMaterial;
                var outward=new Vector3(.5f,0,-.8660254f);
                var right=Vector3.Cross(Vector3.up,outward);var near=new Vector3(39.713161f,3.15f,4.554907f)+outward*.02f;
                var far=near-outward*4.34f;far.y=3.25f;
                var a=far-right*1.4f;var b=far+right*1.4f;var c=near+right*1.4f;var d=near-right*1.4f;
                Solid("Engine entrance missing floor closure",new[]{a,b,c,d,a-Vector3.up*.2f,b-Vector3.up*.2f,c-Vector3.up*.2f,d-Vector3.up*.2f},floorMaterial);
            }
            // The landing belongs to the NEW connector, never to the existing room.
            // Its upper face sits just below the untouched entrance floor to avoid z-fighting.
            var contactPlan=JsonUtility.FromJson<Plan>(File.ReadAllText(ReportDirectory+"/RoutePlan.json"));
            var contactWidths=new[]{2.414f,4.948f,2.874f,2.866f,2.779f};
            var contactHeights=new[]{1.933f,3.211f,2.033f,2.05f,1.96f};
            for(var i=0;i<contactPlan.ports.Length;i++)
            {
                var port=contactPlan.ports[i];
                var root=scene.GetRootGameObjects().First(o=>o.name=="Cargo "+port.id+" Corridor and Contacts");
                if(root.transform.Find("Room seam left return")!=null)continue;
                buildRoot=root.transform;
                wallMaterial=root.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.EndsWith(" 1")).sharedMaterial;
                var direction=port.points[1]-port.points[0];direction.y=0;direction.Normalize();
                var rotation=Quaternion.LookRotation(direction);var right=rotation*Vector3.right;
                var center=port.points[0]-direction*.05f;var width=contactWidths[i];var height=contactHeights[i];
                Box("Room seam left return",center-right*(width*.5f+.10f)+Vector3.up*(height*.5f),new Vector3(.20f,height+.2f,.3f),rotation,wallMaterial);
                Box("Room seam right return",center+right*(width*.5f+.10f)+Vector3.up*(height*.5f),new Vector3(.20f,height+.2f,.3f),rotation,wallMaterial);
                Box("Room seam overhead return",center+Vector3.up*(height+.10f),new Vector3(width+.4f,.20f,.3f),rotation,wallMaterial);
            }
            for(var i=1;i<contactPlan.ports.Length;i++)
            {
                var port=contactPlan.ports[i];
                var root=scene.GetRootGameObjects().First(o=>o.name=="Cargo "+port.id+" Corridor and Contacts");
                if(root.transform.Find("Room-side connector landing")!=null)continue;
                var outward=port.points[1]-port.points[0];outward.y=0;outward.Normalize();
                buildRoot=root.transform;
                floorMaterial=root.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.EndsWith(" 0")).sharedMaterial;
                var center=port.points[0]-outward*.75f-Vector3.up*.103f;
                Box("Room-side connector landing",center,new Vector3(contactWidths[i],.2f,1.5f),Quaternion.LookRotation(outward),floorMaterial);
            }
            foreach(var root in scene.GetRootGameObjects().Where(o=>o.name.StartsWith("Cargo ")&&o.name.EndsWith("Corridor and Contacts")))
            {
                if(root.transform.Find("Authored Corridor Lights")!=null)continue;
                var lightRoot=new GameObject("Authored Corridor Lights").transform;lightRoot.SetParent(root.transform,false);
                var id=root.name.Split(' ')[1];var data=JsonUtility.FromJson<PathData>(File.ReadAllText(ReportDirectory+"/"+id+"Path.json"));
                for(var j=25;j<data.points.Length-10;j+=35)
                {
                    var go=new GameObject("Recessed corridor light "+j);go.transform.SetParent(lightRoot,false);go.transform.position=data.points[j]+Vector3.up*2.2f;
                    var light=go.AddComponent<Light>();light.type=LightType.Point;light.range=7;light.intensity=2.4f;light.color=new Color(.62f,.82f,1);light.shadows=LightShadows.Soft;
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,SamplePath);
        }
        internal static void Create()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode.");
            if(File.Exists(SamplePath))throw new InvalidOperationException("Existing sample preserved; do not overwrite.");
            var source=SceneManager.GetActiveScene();
            if(source.path!="Assets/_Project/Scenes/Pegasus.unity"||source.isDirty)throw new InvalidOperationException("Requires clean Pegasus source; no production save.");
            plan=JsonUtility.FromJson<Plan>(File.ReadAllText(ReportDirectory+"/RoutePlan.json"));
            Directory.CreateDirectory(SampleDirectory+"/Meshes");AssetDatabase.Refresh();
            var sample=EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects,NewSceneMode.Additive);SceneManager.SetActiveScene(sample);
            var names=new[]{"Approved Engine Room 01 Shell","Approved Cockpit 01 Structure","Approved Control Room 01 Shell","Approved Armory 01 Shell","Approved Supply Room 01 Shell","Approved Cargo Hold 01 Shell","Intermediate Connectors","Armory Supply Intermediate Connectors","Approved Ship Corridor Segments"};
            foreach(var name in names)
            {
                var original=source.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name==name);
                if(original==null)throw new InvalidOperationException("Missing source "+name);
                var clone=UnityEngine.Object.Instantiate(original.gameObject);clone.name=name;clone.transform.SetParent(null,true);
                clone.transform.SetPositionAndRotation(original.position,original.rotation);clone.transform.localScale=original.lossyScale;
                SceneManager.MoveGameObjectToScene(clone,sample);
            }
            var cargo=sample.GetRootGameObjects().First(o=>o.name=="Approved Cargo Hold 01 Shell");
            meshIndex=0;BuildWarehouse(cargo);
            for(var i=0;i<plan.ports.Length;i++)BuildTunnel(plan.ports[i],i);
            AssetDatabase.SaveAssets();
            if(!EditorSceneManager.SaveScene(sample,SamplePath))throw new InvalidOperationException("Sample save failed.");
            EditorSceneManager.CloseScene(source,true);
            File.WriteAllText(ReportDirectory+"/Construction.txt","Sample only. Direct visual and movement review PENDING.\nFive body centerlines 18m; contacts .5m at each end.\nPegasus not saved.");
        }
        [Serializable] private class Item
        {
            public string path;
            public Vector3 position, rotation, scale, min, max;
            public Vector3[] vertices;
        }
        [Serializable] private class Geometry { public List<Item> items = new List<Item>(); }
        internal static void Inspect()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/_Project/Scenes/Pegasus.unity" || EditorApplication.isPlaying)
                throw new InvalidOperationException("Read-only inspection requires Pegasus in Edit Mode.");
            var geometry = new Geometry();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (!(root.name.Contains("Shell") || root.name == "Approved Cockpit 01 Structure" || root.name.StartsWith("ShipSpaceCeiling"))) continue;
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    var r = t.GetComponent<Renderer>();
                    if (r == null && t != root.transform) continue;
                    var path = t.name; var parent = t.parent;
                    while (parent != null) { path = parent.name + "/" + path; parent = parent.parent; }
                    var mesh=t.GetComponent<MeshFilter>();
                    Vector3[] vertices=null;
                    if(mesh!=null && mesh.sharedMesh!=null && (t.name.Contains("Cargo ramp side wall") || t.name.Contains("floor continuation") || t.name.Contains("descending ramp floor") || t.name=="rear center stem floor slab"))
                    {
                        vertices=mesh.sharedMesh.vertices;
                        for(var i=0;i<vertices.Length;i++)vertices[i]=t.TransformPoint(vertices[i]);
                    }
                    geometry.items.Add(new Item { path=path, position=t.position, rotation=t.eulerAngles, scale=t.lossyScale,
                        min=r==null?t.position:r.bounds.min, max=r==null?t.position:r.bounds.max,vertices=vertices });
                }
            }
            Directory.CreateDirectory(ReportDirectory);
            File.WriteAllText(ReportDirectory + "/Geometry.json", JsonUtility.ToJson(geometry,true));
            File.WriteAllText(ReportDirectory + "/State.txt", "Scene="+scene.path+" Dirty="+scene.isDirty+"\nRead only; no geometry changed or scene saved.");
        }
        [Serializable] private class Operation { public string action; public int index; public Vector3 position, rotation; public string room; public bool reverse; public float offset; }
        private static Camera observer;
        private static CharacterController walker;
        private static EditorApplication.CallbackFunction walking;
        private static void Walk(Operation op)
        {
            if(walking!=null)throw new InvalidOperationException("A direct walk review is still running.");
            var data=JsonUtility.FromJson<PathData>(File.ReadAllText(ReportDirectory+"/"+op.room+"Path.json"));
            var path=data.points.ToList();if(op.reverse)path.Reverse();
            var firstDirection=(path[1]-path[0]).normalized;var lastDirection=(path[path.Count-1]-path[path.Count-2]).normalized;
            var initial=path[0]-new Vector3(firstDirection.x,0,firstDirection.z).normalized;
            path.Insert(0,initial);path.Add(path[path.Count-1]+new Vector3(lastDirection.x,0,lastDirection.z).normalized);
            var referencePath=path.ToArray();
            for(var i=0;i<path.Count;i++)
            {
                var direction=referencePath[Math.Min(i+1,path.Count-1)]-referencePath[Math.Max(i-1,0)];direction.y=0;
                path[i]+=Vector3.Cross(Vector3.up,direction.normalized)*op.offset;
            }
            foreach(var old in Resources.FindObjectsOfTypeAll<GameObject>().Where(o=>o.name=="__CargoWalkObserver").ToArray())
                UnityEngine.Object.DestroyImmediate(old);
            Physics.SyncTransforms();
            var go=new GameObject("__CargoWalkObserver"){hideFlags=HideFlags.DontSave};
            go.transform.position=path[0]+Vector3.up*.05f;walker=go.AddComponent<CharacterController>();
            walker.height=1.8f;walker.radius=.35f;walker.center=Vector3.up*.9f;walker.stepOffset=.3f;walker.slopeLimit=45;walker.skinWidth=.03f;
            var folder=ReportDirectory+"/Walk_"+op.room+"_"+(op.reverse?"Back":"Forward")+"_"+op.offset.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+"_"+op.index;
            Directory.CreateDirectory(folder);
            var started=EditorApplication.timeSinceStartup;var last=started;var lastProgress=started;var nextCapture=started;var target=1;var frame=0;var gravity=0f;
            walking=()=>
            {
                if(!EditorApplication.isPlaying||walker==null){EditorApplication.update-=walking;walking=null;return;}
                var now=EditorApplication.timeSinceStartup;var dt=Mathf.Min(.25f,(float)(now-last));last=now;
                var delta=Vector3.zero;var steps=Mathf.Max(1,Mathf.CeilToInt(dt/.015f));
                for(var step=0;step<steps;step++)
                {
                    delta=path[target]-walker.transform.position;delta.y=0;
                    while(delta.magnitude<.12f)
                    {
                        target++;lastProgress=now;
                        if(target>=path.Count)
                        {
                            var heightOK=Mathf.Abs(walker.transform.position.y-path[path.Count-1].y)<.25f;
                            File.WriteAllText(folder+"/Result.txt",(heightOK?"Actual PlayMode CharacterController route completed. Direct image review still required.":"NOT PASSED: destination walking height differs.")+"\nElapsed="+(now-started)+"\nPosition="+walker.transform.position);
                            EditorApplication.update-=walking;walking=null;return;
                        }
                        delta=path[target]-walker.transform.position;delta.y=0;
                    }
                    var subDt=dt/steps;gravity=walker.isGrounded?-2:gravity-18*subDt;
                    walker.Move(delta.normalized*Mathf.Min(4*subDt,delta.magnitude)+Vector3.up*gravity*subDt);
                }
                var look=path[Math.Min(path.Count-1,target+15)]-walker.transform.position;
                var pan=Mathf.Sin((float)(now-started)*1.7f)*60;
                observer.transform.SetPositionAndRotation(walker.transform.position+Vector3.up*1.62f,Quaternion.LookRotation(look.normalized)*Quaternion.Euler(12*Mathf.Sin((float)(now-started)),pan,0));
                if(now>=nextCapture)
                {
                    var frameName=folder+"/Frame_"+(frame++).ToString("D3");
                    ScreenCapture.CaptureScreenshot(frameName+".png");File.WriteAllText(frameName+".txt","Actual PlayMode="+EditorApplication.isPlaying+" Position="+walker.transform.position+" Target="+target);nextCapture=now+.3;
                }
                if(now-lastProgress>3||now-started>120)
                {
                    var details=new System.Text.StringBuilder("NOT PASSED: "+(now-lastProgress>3?"movement stalled":"observation time limit")+"\nTarget="+target+" Position="+walker.transform.position+" Expected="+path[target]+" Flags="+walker.collisionFlags+"\n");
                    foreach(var hit in Physics.CapsuleCastAll(walker.transform.position+Vector3.up*.35f,walker.transform.position+Vector3.up*1.45f,.35f,delta.normalized,1))
                    {
                        var t=hit.collider.transform;var name=t.name;while(t.parent!=null){t=t.parent;name=t.name+"/"+name;}
                        details.AppendLine(name+" distance="+hit.distance+" point="+hit.point+" normal="+hit.normal+" bounds="+hit.collider.bounds);
                    }
                    File.WriteAllText(folder+"/Result.txt",details.ToString());
                    EditorApplication.update-=walking;walking=null;
                }
            };
            EditorApplication.update+=walking;
        }
        private static void PlaceStaticShutters(Scene scene)
        {
            if(scene.path!=SamplePath||EditorApplication.isPlaying||scene.isDirty)
                throw new InvalidOperationException("Clean sample in Edit Mode required.");
            var source=scene.GetRootGameObjects().First(o=>o.name=="Approved Ship Corridor Segments")
                .GetComponentsInChildren<Transform>(true).First(t=>t.name=="SC-S01 sloped corridor sample");
            var suffixes=new[]{"overhead shutter housing","lowered full height closure shutter","shutter center armor face","red closure warning strip"};
            var originals=suffixes.Select(s=>source.Cast<Transform>().First(t=>t.name.EndsWith("low end "+s))).ToArray();
            var routePlan=JsonUtility.FromJson<Plan>(File.ReadAllText(ReportDirectory+"/RoutePlan.json"));
            // Reuse the source mesh/materials, with endpoint fitting confined to the new corridor.
            foreach(var port in routePlan.ports)
            {
                var parent=scene.GetRootGameObjects().First(o=>o.name=="Cargo "+port.id+" Corridor and Contacts").transform;
                if(parent.Find("Static Endpoint Shutters")!=null)throw new InvalidOperationException("Shutters already placed; preserve current edits.");
                var group=new GameObject("Static Endpoint Shutters").transform;group.SetParent(parent,false);
                var path=JsonUtility.FromJson<PathData>(File.ReadAllText(ReportDirectory+"/"+port.id+"Path.json")).points;
                for(var end=0;end<2;end++)
                {
                    var ix=end==0?5:185;var sign=end==0?1:-1;
                    var direction=(path[ix+sign]-path[ix-sign]);direction.y=0;direction.Normalize();
                    var gate=new GameObject(end==0?"Room End Static Shutter":"Cargo End Static Shutter").transform;
                    gate.SetParent(group,false);gate.SetPositionAndRotation(path[ix],Quaternion.LookRotation(direction));
                    var centers=new[]{new Vector3(0,2.88f,0),new Vector3(0,1.35f,0),new Vector3(0,1.42f,-.225f),new Vector3(0,.61f,-.275f)};
                    // Source X is thickness and Z is width. Keep the original warning-face arrangement.
                    var sizes=new[]{new Vector3(1.012f,.396f,2.86f),new Vector3(.418f,2.96f,2.86f),new Vector3(.0704f,2.432f,2.508f),new Vector3(.077f,.165f,2.64f)};
                    for(var part=0;part<originals.Length;part++)
                    {
                        var clone=UnityEngine.Object.Instantiate(originals[part].gameObject,gate,false);
                        clone.name=suffixes[part];clone.transform.localPosition=centers[part];
                        clone.transform.localRotation=Quaternion.Euler(0,-90,0);clone.transform.localScale=sizes[part];
                    }
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene,SamplePath))throw new InvalidOperationException("Sample save failed.");
            File.WriteAllText(ReportDirectory+"/ShutterPlacement.txt","Ten static shutter assemblies cloned from SC-S01. No animation or interaction added. Closed source appearance retained. Direct review pending.");
        }
        private const string TransferDirectory = ReportDirectory + "/TransferReview";
        private const string ProductionPath = "Assets/_Project/Scenes/Pegasus.unity";
        private static GameObject Unique(Scene scene,string name)
        {
            var found=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Where(t=>t.name==name).ToArray();
            if(found.Length!=1)throw new InvalidOperationException("Expected one scoped object: "+name+" found "+found.Length);
            return found[0].gameObject;
        }
        private static void ApplyApprovedCargo()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit Mode required.");
            for(var i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Unsaved scene; preserve user changes.");
            var target=EditorSceneManager.OpenScene(ProductionPath,OpenSceneMode.Single);
            var sample=EditorSceneManager.OpenScene(SamplePath,OpenSceneMode.Additive);
            try
            {
                var source=Unique(sample,"Approved Cargo Hold 01 Shell");
                var cargo=Unique(target,source.name);
                var routes=new[]{"Engine","Cockpit","Control","Armory","Supply"};
                var originals=routes.Select(id=>Unique(sample,"Cargo "+id+" Corridor and Contacts")).ToArray();
                if(target.GetRootGameObjects().Any(r=>originals.Any(o=>o.name==r.name)))throw new InvalidOperationException("Cargo routes already exist; do not duplicate.");
                var oldModules=Enumerable.Range(1,5).Select(i=>Unique(target,"SC-S0"+i+" sloped corridor sample")).ToArray();
                var oldRoofs=Enumerable.Range(1,5).Select(i=>Unique(target,"ShipSpaceCeiling_SC_S0"+i+"_sloped_corridor_sample_Sloped_Slab_01")).ToArray();
                // Keep the warehouse root identity for scene references; replace only its approved contents.
                if(cargo.GetComponents<Component>().Length!=1)throw new InvalidOperationException("Unexpected warehouse root components; inspect before replacement.");
                var descendants=new HashSet<UnityEngine.Object>();
                foreach(var t in cargo.GetComponentsInChildren<Transform>(true).Where(t=>t!=cargo.transform))
                {descendants.Add(t.gameObject);foreach(var component in t.GetComponents<Component>())descendants.Add(component);}
                foreach(var root in target.GetRootGameObjects())foreach(var component in root.GetComponentsInChildren<Component>(true))
                {
                    if(component==null||component is Transform||descendants.Contains(component))continue;
                    using(var serialized=new SerializedObject(component))
                    {
                        var p=serialized.GetIterator();while(p.Next(true))
                            if(p.propertyType==SerializedPropertyType.ObjectReference&&descendants.Contains(p.objectReferenceValue))
                                throw new InvalidOperationException("External warehouse child reference: "+component.name+"/"+p.propertyPath);
                    }
                }
                var report=new System.Text.StringBuilder("Approved cargo sample transfer. Direct review pending.\n");
                foreach(var child in cargo.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
                cargo.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
                var parentScale=cargo.transform.parent==null?Vector3.one:cargo.transform.parent.lossyScale;
                var sourceScale=source.transform.lossyScale;
                cargo.transform.localScale=new Vector3(sourceScale.x/parentScale.x,sourceScale.y/parentScale.y,sourceScale.z/parentScale.z);
                foreach(Transform child in source.transform)UnityEngine.Object.Instantiate(child.gameObject,cargo.transform,false).name=child.name;
                foreach(var component in source.GetComponents<Component>().Where(c=>!(c is Transform)))
                    EditorUtility.CopySerialized(component,cargo.AddComponent(component.GetType()));
                cargo.SetActive(source.activeSelf);
                foreach(var original in originals)
                {
                    var clone=UnityEngine.Object.Instantiate(original);clone.name=original.name;
                    SceneManager.MoveGameObjectToScene(clone,target);report.AppendLine("Applied "+clone.name);
                }
                foreach(var old in oldModules.Concat(oldRoofs)){old.SetActive(false);report.AppendLine("Hidden, not deleted: "+old.name);}
                SceneManager.SetActiveScene(target);EditorSceneManager.MarkSceneDirty(target);
                if(!EditorSceneManager.SaveScene(target,ProductionPath))throw new InvalidOperationException("Pegasus save failed.");
                File.WriteAllText(TransferDirectory+"/Application.txt",report.ToString());
            }
            finally{EditorSceneManager.CloseScene(sample,true);SceneManager.SetActiveScene(target);}
        }
        internal static void Review()
        {
            var scene=SceneManager.GetActiveScene();
            if(scene.path!="Assets/_Project/Scenes/Pegasus.unity"&&scene.path!=SamplePath)throw new InvalidOperationException("Requires scoped observation scene.");
            var reviewDirectory=File.Exists(TransferDirectory+"/Operation.json")?TransferDirectory:ReportDirectory;
            var op=JsonUtility.FromJson<Operation>(File.ReadAllText(reviewDirectory+"/Operation.json"));
            if(op.action=="applyApprovedCargo"){ApplyApprovedCargo();return;}
            if(op.action=="openApprovedSample"||op.action=="openProduction")
            {
                if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before switching scenes.");
                for(var i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Unsaved user scene.");
                EditorSceneManager.OpenScene(op.action=="openApprovedSample"?SamplePath:ProductionPath,OpenSceneMode.Single);return;
            }
            if(op.action=="placeStaticShutters"){PlaceStaticShutters(scene);return;}
            if(op.action=="sourceShutterView")
            {
                var source=scene.GetRootGameObjects().First(o=>o.name=="Approved Ship Corridor Segments")
                    .GetComponentsInChildren<Transform>(true).First(t=>t.name=="SC-S01 sloped corridor sample low end lowered full height closure shutter");
                var direction=source.parent.right;
                op.position=source.position-direction*3+Vector3.up*.5f;
                op.rotation=Quaternion.LookRotation(source.position-op.position).eulerAngles;
            }
            if(op.action=="shutterView")
            {
                var routePlan=JsonUtility.FromJson<Plan>(File.ReadAllText(ReportDirectory+"/RoutePlan.json"));
                var port=routePlan.ports.First(p=>p.id==op.room);
                var root=scene.GetRootGameObjects().First(o=>o.name=="Cargo "+port.id+" Corridor and Contacts");
                var gate=root.transform.Find("Static Endpoint Shutters/"+(op.reverse?"Cargo End Static Shutter":"Room End Static Shutter"));
                var target=gate.position+Vector3.up*1.4f;
                op.position=target-gate.forward*2.8f+gate.right*op.offset;
                if(op.index>=700&&op.index<800)op.position=target+gate.forward*2.8f+gate.right*op.offset;
                if(op.index>=800&&op.index<900)op.position=gate.position-gate.forward*1.4f+gate.right*op.offset+Vector3.up*.35f;
                op.rotation=Quaternion.LookRotation(target-op.position).eulerAngles;
            }
            if(op.action=="repairEngineContact")
            {
                if(scene.path!=SamplePath||EditorApplication.isPlaying)throw new InvalidOperationException("Sample Edit Mode only.");
                plan=JsonUtility.FromJson<Plan>(File.ReadAllText(ReportDirectory+"/RoutePlan.json"));
                plan.ports[0].points[0].y=3.15f;
                var previous=scene.GetRootGameObjects().First(o=>o.name=="Cargo Engine Corridor and Contacts");
                floorMaterial=previous.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.EndsWith(" 0")).sharedMaterial;
                wallMaterial=previous.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.EndsWith(" 1")).sharedMaterial;
                AssetDatabase.StartAssetEditing();
                try{BuildTunnel(plan.ports[0],0);}finally{AssetDatabase.StopAssetEditing();}
                UnityEngine.Object.DestroyImmediate(previous);
                FinishConstruction(scene);
                File.WriteAllText(ReportDirectory+"/RoutePlan.json",JsonUtility.ToJson(plan,true));return;
            }
            if(op.action=="inspectShutters")
            {
                var report=new System.Text.StringBuilder();
                foreach(var root in scene.GetRootGameObjects())
                foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("closure shutter")))
                {
                    report.AppendLine("SHUTTER "+root.name+" / "+t.name+" position="+t.position);
                    for(var ancestor=t;ancestor!=null;ancestor=ancestor.parent)
                        report.AppendLine("  "+ancestor.name+" components="+string.Join(", ",ancestor.GetComponents<Component>().Select(c=>c==null?"MISSING":c.GetType().FullName)));
                    foreach(var sibling in t.parent.GetComponentsInChildren<Transform>(true).Where(s=>s.name.Contains("shutter")||s.name.Contains("warning strip")))
                        report.AppendLine("  PART "+sibling.name+" pos="+sibling.position+" scale="+sibling.lossyScale);
                }
                File.WriteAllText(ReportDirectory+"/ShutterInspection.txt",report.ToString());return;
            }
            if(op.action=="finishConstruction"){FinishConstruction(scene);return;}
            if(op.action=="inspectContactFloors")
            {
                var report=new System.Text.StringBuilder();
                foreach(var root in scene.GetRootGameObjects().Where(o=>o.name.StartsWith("Approved ")&&!o.name.Contains("Cargo")))
                foreach(var mesh in root.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.name.ToLowerInvariant().Contains("floor")))
                {
                    var renderer=mesh.GetComponent<Renderer>();if(renderer==null)continue;
                    report.AppendLine(root.name+" / "+mesh.name+" bounds="+renderer.bounds);
                    foreach(var col in mesh.GetComponents<Collider>())report.AppendLine("  "+col.GetType().Name+" enabled="+col.enabled+" active="+col.gameObject.activeInHierarchy+" bounds="+col.bounds);
                }
                File.WriteAllText(ReportDirectory+"/ContactFloors.txt",report.ToString());return;
            }
            if(op.action=="inspectSourcePhysics")
            {
                if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit Mode read-only source inspection.");
                var source=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Pegasus.unity",OpenSceneMode.Additive);
                try
                {
                    var report=new System.Text.StringBuilder();
                    foreach(var root in source.GetRootGameObjects())
                    {
                        report.AppendLine("ROOT "+root.name);
                        foreach(var col in root.GetComponentsInChildren<Collider>(true))
                        {
                            var p=col.transform.position;
                            if(Mathf.Abs(p.x-56.08f)>9||Mathf.Abs(p.z-28f)>8)continue;
                            report.AppendLine("  "+col.name+" "+col.GetType().Name+" enabled="+col.enabled+" active="+col.gameObject.activeInHierarchy+" position="+p+" bounds="+col.bounds);
                        }
                    }
                    File.WriteAllText(ReportDirectory+"/SourcePhysics.txt",report.ToString());
                }
                finally{EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(scene);}
                return;
            }
            if(op.action=="console")
            {
                var assembly=typeof(EditorWindow).Assembly;var logs=assembly.GetType("UnityEditor.LogEntries");
                var entryType=assembly.GetType("UnityEditor.LogEntry");var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic;
                var report=new System.Text.StringBuilder("Scene="+scene.path+" PlayMode="+EditorApplication.isPlaying+" Dirty="+scene.isDirty+"\n");
                logs.GetMethod("StartGettingEntries",flags).Invoke(null,null);
                try
                {
                    var count=(int)logs.GetMethod("GetCount",flags).Invoke(null,null);var entry=Activator.CreateInstance(entryType);
                    var message=entryType.GetField("message",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
                    for(var i=0;i<count;i++){logs.GetMethod("GetEntryInternal",flags).Invoke(null,new[]{(object)i,entry});report.AppendLine(Convert.ToString(message.GetValue(entry)));}
                }
                finally{logs.GetMethod("EndGettingEntries",flags).Invoke(null,null);}
                File.WriteAllText(reviewDirectory+"/Console.txt",report.ToString());return;
            }
            if(op.action=="enter"){EditorApplication.isPlaying=true;return;}
            if(op.action=="exit")
            {
                if(walking!=null){EditorApplication.update-=walking;walking=null;}
                foreach(var old in Resources.FindObjectsOfTypeAll<GameObject>().Where(o=>o.name=="__CargoWalkObserver"||o.name=="__CargoConnectionObserver").ToArray())
                    UnityEngine.Object.DestroyImmediate(old);
                walker=null;observer=null;EditorApplication.isPlaying=false;return;
            }
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Actual Play Mode required for observation.");
            if(observer==null)
            {
                foreach(var old in Resources.FindObjectsOfTypeAll<GameObject>().Where(o=>o.name=="__CargoConnectionObserver").ToArray())
                    UnityEngine.Object.DestroyImmediate(old);
                var go=new GameObject("__CargoConnectionObserver"){hideFlags=HideFlags.DontSave};
                observer=go.AddComponent<Camera>();observer.depth=100;observer.nearClipPlane=.03f;observer.farClipPlane=500;observer.fieldOfView=65;
            }
            if(op.action=="walk"){Walk(op);return;}
            observer.transform.SetPositionAndRotation(op.position,Quaternion.Euler(op.rotation));
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            var due=EditorApplication.timeSinceStartup+.8;EditorApplication.CallbackFunction capture=null;
            capture=()=>{if(EditorApplication.timeSinceStartup<due)return;EditorApplication.update-=capture;
                ScreenCapture.CaptureScreenshot(reviewDirectory+"/Entrance_"+op.index+".png");
                File.WriteAllText(reviewDirectory+"/Entrance_"+op.index+".txt","Scene="+scene.path+" Actual PlayMode="+EditorApplication.isPlaying+" Camera="+op.position+" Rotation="+op.rotation);};
            EditorApplication.update+=capture;
        }
    }
}
