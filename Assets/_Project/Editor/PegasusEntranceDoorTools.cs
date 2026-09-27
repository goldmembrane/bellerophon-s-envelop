using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Bellerophon.Core.Ship;
using Bellerophon.Core.Session;
using Bellerophon.Rendering;
using Bellerophon.Core.Player;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Bellerophon.Editor
{
    internal static class PegasusEntranceDoorTools
    {
        const string Dir = "docs/validation/PegasusEntranceDoors";
        const string WalkDir = "docs/validation/PegasusWalkability";
        const string SpawnDir = "docs/validation/PegasusPlayerSpawn";
        const string ClearanceDir = "docs/validation/PegasusStandingClearance";
        const string WallDir = "docs/validation/PegasusWallCollision";
        const string PartitionDir = "docs/validation/PegasusControlPartition";
        [Serializable] private class ActionData { public string action; public int index; public int side = 1; public float lateral; public int durability = 500; public int room; public string label; public Vector3[] points; }
        internal static void Run()
        {
            var action = JsonUtility.FromJson<ActionData>(File.ReadAllText(File.Exists(PartitionDir+"/Action.json")?PartitionDir+"/Action.json":File.Exists(WallDir+"/Action.json")?WallDir+"/Action.json":File.Exists(ClearanceDir+"/Action.json")?ClearanceDir+"/Action.json":File.Exists(SpawnDir+"/Action.json")?SpawnDir+"/Action.json":File.Exists(WalkDir + "/Action.json") ? WalkDir + "/Action.json" : Dir + "/Action.json"));
            if (SceneManager.GetActiveScene().path != "Assets/_Project/Scenes/Pegasus.unity") throw new InvalidOperationException("Pegasus required.");
            switch (action.action)
            {
                case "enter": EditorApplication.isPlaying = true; break;
                case "exit": StopWalk(); Application.runInBackground = PlayerSettings.runInBackground; EditorApplication.isPlaying = false; break;
                case "observe": Observe(); break;
                case "overclock":
                    RequirePlay();
                    UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>().ActivateDevice(ShipDeviceType.EngineRoomPowerScreen);
                    break;
                case "purify":
                    RequirePlay();
                    var purification = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
                    purification.ActivateDevice(ShipDeviceType.ControlRoomMainScreen);
                    purification.SetControlRoomScreenMode(ShipControlRoomScreenMode.VerticalRoomList);
                    if (!purification.SelectControlRoomPurificationTarget((ShipRoomId)action.room)) throw new InvalidOperationException("Purification rejected.");
                    break;
                case "walk": BeginWalk(action); break;
                case "damage":
                    RequirePlay();
                    var state = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
                    var ship = state.CurrentShipState;
                    var room = ship.GetRoom(ShipRoomId.ControlRoom);
                    state.SetShipState(ship.WithRoom(ShipRoomId.ControlRoom, new ShipRoomState(action.durability, room.MaxDurability)));
                    break;
                case "inspect": Inspect(); break;
                case "partitionSurvey": PartitionSurvey(); break;
                case "partitionRepair": PartitionRepair(); break;
                case "partitionHeadroom":
                    if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required.");
                    var partitionRoot=SceneManager.GetActiveScene().GetRootGameObjects().Single(r=>r.name=="Approved Control Room 01 Shell").transform.Find("Internal Partition - individually editable");
                    foreach(var r in partitionRoot.GetComponentsInChildren<Renderer>().Where(r=>r.name.Contains("doorway")))
                    {
                        var old=r.bounds;var bottom=r.name.EndsWith("header")?5.13f:old.min.y;var top=r.name.EndsWith("header")?old.max.y:5.13f;
                        Undo.RecordObject(r.transform,"Finish standing doorway clearance");var scale=r.transform.localScale;scale.y*=(top-bottom)/old.size.y;r.transform.localScale=scale;
                        var p=r.transform.position;p.y+=(bottom+top)*.5f-r.bounds.center.y;r.transform.position=p;PrefabUtility.RecordPrefabInstancePropertyModifications(r.transform);
                    }
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());PartitionSurvey();break;
                case "partitionSnapshot":
                    if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required.");
                    EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),PartitionDir+"/After.unity",true);Inspect();break;
                case "partitionView": PartitionView(action); break;
                case "partitionRoute": BeginRoute(action); break;
                case "wallInventory": WallInventory(); break;
                case "wallRepair": WallRepair(); break;
                case "wallFinal":
                    RequirePlay();walker=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();CapturePlayerView(WallDir+"/Final.png");Inspect();break;
                case "wallSnapshot":
                    if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required.");
                    EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),WallDir+"/After.unity",true);Inspect();break;
                case "wallProbe": case "wallTransit": BeginRoute(action); break;
                case "clearanceRoute": BeginRoute(action); break;
                case "clearanceInventory": ClearanceInventory(); break;
                case "clearanceRepair": ClearanceRepair(); break;
                case "clearanceSweep": BeginClearanceSweep(action); break;
                case "clearanceFinal":
                    walker=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();CapturePlayerView(ClearanceDir+"/Final.png");break;
                case "clearanceSnapshot":
                    if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required.");
                    EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ClearanceDir+"/AfterSnapshot.unity",true);Inspect();break;
                case "spawnSurvey": SpawnSurvey(); break;
                case "spawnApply": SpawnApply(action.index); break;
                case "restoreSurveyCamera":
                    if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required.");
                    var cameraData=new SerializedObject(UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>().PlayerCamera);
                    foreach(var path in new[]{"m_LocalPosition.y","m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z"})
                        PrefabUtility.RevertPropertyOverride(cameraData.FindProperty(path),InteractionMode.AutomatedAction);
                    break;
                case "spawnView":
                    walker=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
                    CapturePlayerView(SpawnDir+"/"+(action.label??"View")+".png"); SpawnState(); break;
                case "spawnState": SpawnState(); break;
                case "spawnInput": BeginSpawnInput(); break;
                case "spawnSnapshot":
                    if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required.");
                    if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),SpawnDir+"/AfterSnapshot.unity",true))throw new InvalidOperationException("Snapshot failed.");
                    SpawnState();Inspect();break;
                case "floorInventory": FloorInventory(); break;
                case "repairFloors": RepairFloors(); break;
                case "route": BeginRoute(action); break;
                case "finalWalkView":
                    RequirePlay(); walker=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
                    CapturePlayerView(WalkDir+"/Final.png"); break;
                case "walkSnapshot":
                    if(Application.isPlaying) throw new InvalidOperationException("Edit Mode required.");
                    if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),WalkDir+"/AfterSnapshot.unity",true)) throw new InvalidOperationException("Snapshot failed.");
                    Inspect(); break;
                case "snapshot":
                    if (Application.isPlaying) throw new InvalidOperationException("Edit Mode required.");
                    if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), Dir + "/UnsavedSnapshot.unity", true))
                        throw new InvalidOperationException("Snapshot failed; original scene not overwritten.");
                    Inspect();
                    break;
                case "doorColliders":
                    var scene = SceneManager.GetActiveScene();
                    if (Application.isPlaying) throw new InvalidOperationException("Edit Mode required.");
                    if (scene.isDirty)
                    {
                        var snapshot = Dir + "/BeforeColliderSnapshot.unity";
                        if (!EditorSceneManager.SaveScene(scene, snapshot, true) ||
                            File.ReadAllText(snapshot) != File.ReadAllText(scene.path))
                            throw new InvalidOperationException("Unsaved content differs from disk; preserve it and inspect.");
                    }
                    foreach (var door in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PegasusEntranceDoor>()))
                    {
                        var data = new SerializedObject(door);
                        var panel = data.FindProperty("parts").GetArrayElementAtIndex(0);
                        var flags = panel.FindPropertyRelative("closedColliderEnabled");
                        for (var i = 0; i < flags.arraySize; i++) flags.GetArrayElementAtIndex(i).boolValue = true;
                        data.ApplyModifiedProperties();
                        foreach (var col in door.GetComponents<Collider>()) { Undo.RecordObject(col, "Door blocks only when closed"); col.isTrigger = false; }
                    }
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    break;
                default: throw new InvalidOperationException("Unknown door operation.");
            }
        }
        [Serializable] private class SpawnCandidates { public Vector3[] positions; public float yaw; }
        private static string ObjectPath(Transform t)
        {var path=t.name;for(var p=t.parent;p;p=p.parent)path=p.name+"/"+path;return path;}
        private static void PartitionSurvey()
        {
            Directory.CreateDirectory(PartitionDir);var b=new StringBuilder();
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects().Where(r=>r.name=="Approved Control Room 01 Shell"||r.name=="Approved Engine Room 01 Shell"))
            foreach(var r in root.GetComponentsInChildren<Renderer>())
            {
                var p=ObjectPath(r.transform);
                if(root.name.Contains("Engine")||p.Contains("Partition")||p.Contains("Ceiling"))
                    b.AppendLine(p+"|"+r.bounds.ToString("F3")+"|"+string.Join(",",r.GetComponents<Component>().Select(c=>c.GetType().Name)));
            }
            File.WriteAllText(PartitionDir+"/Survey.txt",b.ToString());Inspect();
        }
        private static void PartitionRepair()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required.");
            var scene=SceneManager.GetActiveScene();var backup=PartitionDir+"/Before.unity";
            if(!EditorSceneManager.SaveScene(scene,backup,true)||File.ReadAllText(backup)!=File.ReadAllText(scene.path))throw new InvalidOperationException("Unsaved scene differs; preserve user edits.");
            var roots=scene.GetRootGameObjects();var control=roots.Single(r=>r.name=="Approved Control Room 01 Shell");
            var partition=control.transform.Find("Internal Partition - individually editable");
            var ceiling=control.GetComponentsInChildren<Renderer>().Single(r=>r.name=="ShipSpaceCeiling_ControlRoom_Slab_05").bounds.min.y;
            var header=partition.GetComponentsInChildren<Renderer>().Single(r=>r.name.EndsWith("doorway header"));
            var doorTop=header.bounds.min.y+.30f;var report=new StringBuilder();
            foreach(var r in partition.GetComponentsInChildren<Renderer>())
            {
                var old=r.bounds;var bottom=old.min.y;var top=ceiling+.01f;
                if(r.name.EndsWith("doorway header"))bottom=doorTop;
                else if(r.name.Contains("jamb"))top=doorTop;
                else if(!r.name.Contains("wall between"))throw new InvalidOperationException("Unexpected partition member: "+r.name);
                Undo.RecordObject(r.transform,"Raise partition and doorway");
                var scale=r.transform.localScale;scale.y*= (top-bottom)/old.size.y;r.transform.localScale=scale;
                var position=r.transform.position;position.y+=(bottom+top)*.5f-r.bounds.center.y;r.transform.position=position;
                PrefabUtility.RecordPrefabInstancePropertyModifications(r.transform);
                report.AppendLine(ObjectPath(r.transform)+" | before="+old.ToString("F3")+" | after="+r.bounds.ToString("F3"));
            }
            var engine=roots.Single(r=>r.name=="Approved Engine Room 01 Shell");
            var labels=engine.transform.Find("Labels - individually editable");
            var targets=labels.Cast<Transform>().Where(t=>t.name=="ER-01 1시 Cockpit wall label plate"||t.name=="ER-01 3시 Control wall label plate"||t.name=="ER-01 5시 Cargo wall label plate").ToArray();
            if(targets.Length!=3)throw new InvalidOperationException("Expected exactly three identified floating plates.");
            foreach(var t in targets){report.AppendLine("REMOVED "+ObjectPath(t));Undo.DestroyObjectImmediate(t.gameObject);}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText(PartitionDir+"/Changes.txt",report.ToString());PartitionSurvey();
        }
        private static void PartitionView(ActionData action)
        {
            walker=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            var t=walker.PlayerCamera;var p=t.localPosition;var q=t.localRotation;
            try{t.position=action.points[0];t.rotation=Quaternion.LookRotation(action.points[1]-action.points[0]);CapturePlayerView(PartitionDir+"/"+action.label+".png");}
            finally{t.localPosition=p;t.localRotation=q;}
        }
        private static void WallInventory()
        {
            Directory.CreateDirectory(WallDir);
            var scene=SceneManager.GetActiveScene();var b=new StringBuilder();
            walker=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            var cc=walker.GetComponent<CharacterController>();
            b.AppendLine($"Play={Application.isPlaying} Player={walker.transform.position:F3} enabled={cc.enabled} detectCollisions={cc.detectCollisions} layer={cc.gameObject.layer} freeMove={walker.Settings.EditorPlaytestFreeMovementEnabled}");
            foreach(var root in scene.GetRootGameObjects().Where(r=>r.name.StartsWith("Approved ")||r.name.StartsWith("Cargo ")||r.name.Contains("Intermediate Connectors")))
            {
                b.AppendLine("ROOT "+root.name);
                foreach(var mf in root.GetComponentsInChildren<MeshFilter>())
                {
                    var n=mf.name.ToLowerInvariant();var path=ObjectPath(mf.transform);
                    if(!n.Contains("wall")&&!n.Contains("partition")&&!n.Contains("glass")&&!n.Contains("hull")&&!n.Contains("bulkhead")&&!n.Contains("closure")&&!n.Contains("jamb")&&!path.Contains("structure only"))continue;
                    var r=mf.GetComponent<Renderer>();if(!r)continue;
                    b.AppendLine($"{path}|bounds={r.bounds.ToString("F3")}|layer={mf.gameObject.layer}|playerLayerIgnored={Physics.GetIgnoreLayerCollision(cc.gameObject.layer,mf.gameObject.layer)}|colliders={string.Join(",",mf.GetComponents<Collider>().Select(c=>c.GetType().Name+":"+c.enabled+":"+c.isTrigger))}|mesh={AssetDatabase.GetAssetPath(mf.sharedMesh)}");
                }
            }
            File.WriteAllText(WallDir+"/Walls.txt",b.ToString());Inspect();
        }
        private static bool IsStructuralWall(MeshFilter mf)
        {
            var p=ObjectPath(mf.transform);var n=mf.name.ToLowerInvariant();
            if(p.Contains("/Walls - individually editable/"))return true;
            if(p.Contains("/Entrances - individually editable/")&&n.Contains("side wall"))return true;
            if(p.Contains("/Cockpit 01 - structure only/")&&(n.Contains("wall")||n.Contains("shoulder")||n.Contains("return")||n.Contains("lintel")))return true;
            if(p.Contains("/Corridors - individually editable/")&&n.Contains("side wall"))return true;
            if(p.Contains("/Internal Partition - individually editable/"))return true;
            if(n.Contains("separation wall pier"))return true;
            if(p.Contains("/AR-01 armory shell Blender sample/")&&n.StartsWith("ar-")&&n.Contains("wall"))return true;
            if(p.StartsWith("Approved Supply Room 01 Shell/")&&(n.Contains("wall shell")||n.Contains("shared corridor wall")||n.Contains("corridor upper side wall")||n.Contains("corridor lower side wall")))return true;
            if(p.Contains("/Cargo Adjusted Entrances/")&&n.StartsWith("warehouse wall"))return true;
            if(p.StartsWith("Approved Ship Corridor Segments/SC-H")&&n.EndsWith("armored wall"))return true;
            if(p.Contains("Intermediate Connectors/")&&(n.StartsWith("fittedwall")||n=="walla"||n=="wallb"))return true;
            if(p.StartsWith("Cargo ")&&System.Text.RegularExpressions.Regex.IsMatch(mf.name,@" (RoomContact|Body18m|WarehouseContact) \d+ [23]$"))return true;
            return n.Contains("entrance_wallinfill");
        }
        private static void WallRepair()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required.");
            var scene=SceneManager.GetActiveScene();
            var snapshot=WallDir+"/Before.unity";
            if(!EditorSceneManager.SaveScene(scene,snapshot,true)||File.ReadAllText(scene.path)!=File.ReadAllText(snapshot))throw new InvalidOperationException("Preserve unsaved scene changes before repair.");
            var report=new StringBuilder();
            foreach(var mf in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MeshFilter>()).Where(IsStructuralWall))
            {
                if(!mf.sharedMesh)throw new InvalidOperationException("Missing wall mesh: "+ObjectPath(mf.transform));
                var existing=mf.GetComponents<Collider>();
                if(existing.Any(c=>c.enabled&&!c.isTrigger)){report.AppendLine("UNCHANGED "+ObjectPath(mf.transform));continue;}
                // Use the actual concave wall surface, never a bounding box across an opening.
                var collider=mf.GetComponent<MeshCollider>();
                if(!collider)collider=Undo.AddComponent<MeshCollider>(mf.gameObject);
                Undo.RecordObject(collider,"Restore structural wall collision");
                collider.sharedMesh=mf.sharedMesh;collider.convex=false;collider.isTrigger=false;collider.enabled=true;
                PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
                report.AppendLine("RESTORED "+ObjectPath(mf.transform));
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText(WallDir+"/Repairs.txt",report.ToString());WallInventory();
        }
        private static double sweepStart,sweepPrevious,sweepCapture;
        private static string sweepOutput;
        private static void BeginClearanceSweep(ActionData action)
        {
            RequirePlay();StopWalk();walker=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            var cc=walker.GetComponent<CharacterController>();cc.enabled=false;
            walker.transform.SetPositionAndRotation(action.index==0?new Vector3(38.82f,3.34f,22.526f):new Vector3(43.532f,3.34f,14.506f),Quaternion.Euler(0,action.index==0?30:90,0));
            walker.PlayerCamera.localRotation=Quaternion.identity;walker.Configure(walker.Settings,walker.GetComponent<FirstPersonPlayerInput>(),walker.PlayerCamera);cc.enabled=true;
            reviewBackgroundBehavior=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;Application.runInBackground=true;
            reviewMouse=InputSystem.AddDevice<Mouse>();sweepStart=sweepPrevious=EditorApplication.timeSinceStartup;sweepCapture=0;frameIndex=0;
            sweepOutput=ClearanceDir+"/SeamSweep_"+action.index;Directory.CreateDirectory(sweepOutput);
            EditorApplication.update+=DriveClearanceSweep;
        }
        private static void DriveClearanceSweep()
        {
            if(!Application.isPlaying||EditorApplication.timeSinceStartup-sweepStart>7){StopWalk();return;}
            var now=EditorApplication.timeSinceStartup;var dt=(float)(now-sweepPrevious);sweepPrevious=now;
            InputSystem.QueueStateEvent(reviewMouse,new MouseState{delta=new Vector2(55*dt/walker.Settings.MouseSensitivity,now-sweepStart<1?20*dt/walker.Settings.MouseSensitivity:0)});
            if(now>=sweepCapture){CapturePlayerView(sweepOutput+"/"+(frameIndex++).ToString("D2")+".png");sweepCapture=now+.35;}
        }
        private static void ClearanceRepair()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required.");
            var scene=SceneManager.GetActiveScene();
            if(!EditorSceneManager.SaveScene(scene,ClearanceDir+"/PreRepairSnapshot.unity",true)||File.ReadAllText(scene.path)!=File.ReadAllText(ClearanceDir+"/PreRepairSnapshot.unity"))throw new InvalidOperationException("Preserve unsaved user changes.");
            const string folder="Assets/_Project/Art/PegasusStandingClearance";
            if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/_Project/Art","PegasusStandingClearance");
            var roots=scene.GetRootGameObjects();
            var engine=roots.Single(r=>r.name=="Approved Engine Room 01 Shell");
            var ceilings=engine.GetComponentsInChildren<MeshFilter>().Where(m=>m.name=="ShipSpaceCeiling_EngineRoom_Entrance_Slab_01"||m.name=="ShipSpaceCeiling_EngineRoom_Entrance_Slab_02").ToArray();
            var targets=engine.GetComponentsInChildren<MeshFilter>().Where(m=>ceilings.Contains(m)||m.name.Contains("Cockpit corridor side wall")||m.name.Contains("Control corridor side wall")||m.name.Contains("continuous upper doorway header")).ToList();
            foreach(var root in roots.Single(r=>r.name=="Intermediate Connectors").transform.Cast<Transform>().Where(t=>t.name=="Connector_EngineRoom_H01"||t.name=="Connector_EngineRoom_H05"))
                targets.AddRange(root.GetComponentsInChildren<MeshFilter>().Where(m=>m.name=="FittedCeiling"||m.name.StartsWith("FittedWall")||(m.name=="HeaderSeal"&&m.GetComponent<Renderer>().bounds.center.y<5.2f)));
            var report=new StringBuilder();int index=0;
            foreach(var mf in targets)
            {
                var original=mf.sharedMesh;var vertices=original.vertices;bool changed=false;
                for(int i=0;i<vertices.Length;i++)
                {
                    var p=mf.transform.TransformPoint(vertices[i]);
                    if(mf.name.Contains("continuous upper doorway header")&&!ceilings.Any(c=>{var q=c.transform.InverseTransformPoint(p);return Mathf.Abs(q.x)<.56f&&Mathf.Abs(q.z)<.56f;}))continue;
                    // Continuous vertical deformation preserves shared wall/roof seam coordinates.
                    var lift=.4f*Mathf.Clamp01((p.y-3.25f)/(5.083f-3.25f))*Mathf.Clamp01((5.86f-p.y)/(5.86f-5.25f));
                    if(lift<.0001f)continue;p.y+=lift;vertices[i]=mf.transform.InverseTransformPoint(p);changed=true;
                }
                if(!changed)continue;
                var mesh=UnityEngine.Object.Instantiate(original);mesh.name="StandingClearance_"+(index++).ToString("D2");mesh.vertices=vertices;mesh.RecalculateBounds();mesh.RecalculateNormals();
                var path=folder+"/"+mesh.name+".asset";
                if(AssetDatabase.LoadAssetAtPath<Mesh>(path))throw new InvalidOperationException("Repair already exists; do not apply twice.");
                AssetDatabase.CreateAsset(mesh,path);Undo.RecordObject(mf,"Standing entrance clearance");mf.sharedMesh=mesh;
                foreach(var mc in mf.GetComponents<MeshCollider>()){Undo.RecordObject(mc,"Matching entrance collision");mc.sharedMesh=mesh;}
                foreach(var box in mf.GetComponents<BoxCollider>()){Undo.RecordObject(box,"Matching entrance collision");box.center=mesh.bounds.center;box.size=mesh.bounds.size;}
                report.AppendLine(mf.transform.root.name+"/"+mf.name+" -> "+path);
            }
            File.WriteAllText(ClearanceDir+"/Repairs.txt",report.ToString());
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Save failed.");
        }
        private static void ClearanceInventory()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required.");
            var scene=SceneManager.GetActiveScene();
            if(!File.Exists(ClearanceDir+"/Before.unity.txt"))File.Copy(scene.path,ClearanceDir+"/Before.unity.txt");
            EditorSceneManager.SaveScene(scene,ClearanceDir+"/BeforeSnapshot.unity",true);
            var b=new StringBuilder();
            foreach(var root in scene.GetRootGameObjects().Where(r=>r.name=="Approved Engine Room 01 Shell"||r.name=="Intermediate Connectors"||r.name=="Approved Control Room 01 Shell"))
            foreach(var mf in root.GetComponentsInChildren<MeshFilter>())
            {
                var path=mf.name;for(var p=mf.transform.parent;p;p=p.parent)path=p.name+"/"+path;
                var renderer=mf.GetComponent<Renderer>();
                b.AppendLine($"{path} position={mf.transform.position:F3} scale={mf.transform.lossyScale:F3} mesh={AssetDatabase.GetAssetPath(mf.sharedMesh)} bounds={renderer.bounds.ToString("F3")}");
                if(mf.name.Contains("Fitted")||mf.name.Contains("Seal"))b.AppendLine(string.Join(";",mf.sharedMesh.vertices.Take(8).Select(v=>mf.transform.TransformPoint(v).ToString("F3"))));
            }
            File.WriteAllText(ClearanceDir+"/Geometry.txt",b.ToString());Inspect();
        }
        private static void SpawnSurvey()
        {
            if(Application.isPlaying) throw new InvalidOperationException("Edit Mode required.");
            Directory.CreateDirectory(SpawnDir);
            var root=SceneManager.GetActiveScene().GetRootGameObjects().Single(r=>r.name=="Approved Cargo Hold 01 Shell");
            var floor=root.GetComponentsInChildren<BoxCollider>().Single(c=>c.name=="CH-01 sealed cargo hold deck floor");
            walker=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            var positions=new System.Collections.Generic.List<Vector3>(); var report=new StringBuilder();
            var renderers=root.GetComponentsInChildren<Renderer>();
            foreach(var x in new[]{0f,-.2f,.2f,-.35f,.35f}) foreach(var z in new[]{0f,-.2f,.2f,-.35f,.35f})
            {
                var point=floor.transform.TransformPoint(new Vector3(x,.5f,z));
                var hits=Physics.RaycastAll(point+Vector3.up*.5f,Vector3.down,1f,~0,QueryTriggerInteraction.Ignore).Where(h=>h.normal.y>.8f).OrderByDescending(h=>h.point.y).ToArray();
                if(hits.Length==0)continue;
                point.y=hits[0].point.y+.04f;
                var occupied=Physics.OverlapCapsule(point+Vector3.up*.45f,point+Vector3.up*1.55f,.4f,~0,QueryTriggerInteraction.Ignore).Where(c=>!c.transform.IsChildOf(walker.transform)).ToArray();
                var volume=new Bounds(point+Vector3.up*1f,new Vector3(1.2f,1.7f,1.2f));
                var visual=renderers.Where(r=>r.enabled&&!r.transform.IsChildOf(walker.transform)&&r.bounds.Intersects(volume)).Select(r=>r.name).ToArray();
                report.AppendLine($"{point:F3} colliders={string.Join(",",occupied.Select(c=>c.name))} visual={string.Join(",",visual)}");
                if(occupied.Length==0 && visual.Length==0)positions.Add(point);
            }
            File.WriteAllText(SpawnDir+"/Survey.txt",report.ToString());
            if(positions.Count==0)throw new InvalidOperationException("No clear candidate; inspect rather than overlap props.");
            var yaw=root.transform.eulerAngles.y;
            File.WriteAllText(SpawnDir+"/Candidates.json",JsonUtility.ToJson(new SpawnCandidates{positions=positions.ToArray(),yaw=yaw},true));
            File.WriteAllText(SpawnDir+"/Survey.txt",report.ToString());
            var camera=walker.PlayerCamera; var oldPosition=camera.localPosition; var oldRotation=camera.localRotation;
            try { for(var i=0;i<Math.Min(3,positions.Count);i++) for(var side=0;side<2;side++) {
                camera.SetPositionAndRotation(positions[i]+Vector3.up*walker.Settings.CameraStandingHeight,Quaternion.Euler(0,yaw+side*180,0));
                CapturePlayerView(SpawnDir+"/Candidate_"+i+"_"+side+".png");
            }} finally {camera.localPosition=oldPosition;camera.localRotation=oldRotation;}
        }
        private static void SpawnApply(int index)
        {
            if(Application.isPlaying)throw new InvalidOperationException("Edit Mode required.");
            var scene=SceneManager.GetActiveScene();
            if(scene.isDirty && (!EditorSceneManager.SaveScene(scene,SpawnDir+"/BeforeSnapshot.unity",true)||File.ReadAllText(SpawnDir+"/BeforeSnapshot.unity")!=File.ReadAllText(scene.path)))throw new InvalidOperationException("Unsaved changes differ; preserve them.");
            if(!File.Exists(SpawnDir+"/Before.unity.txt"))File.Copy(scene.path,SpawnDir+"/Before.unity.txt");
            var candidates=JsonUtility.FromJson<SpawnCandidates>(File.ReadAllText(SpawnDir+"/Candidates.json"));
            walker=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            const string settingsPath="Assets/_Project/Settings/Player/PegasusFirstPersonPlayerSettings.asset";
            var settings=AssetDatabase.LoadAssetAtPath<FirstPersonPlayerSettings>(settingsPath);
            if(!settings) {settings=UnityEngine.Object.Instantiate(walker.Settings); settings.name="PegasusFirstPersonPlayerSettings"; AssetDatabase.CreateAsset(settings,settingsPath);}
            var data=new SerializedObject(settings);data.FindProperty("editorPlaytestFreeMovementEnabled").boolValue=false;data.ApplyModifiedProperties();
            Undo.RecordObject(walker.transform,"Cargo hold player start");
            walker.transform.SetPositionAndRotation(candidates.positions[index],Quaternion.Euler(0,candidates.yaw+180,0));
            var playerData=new SerializedObject(walker);playerData.FindProperty("settings").objectReferenceValue=settings;playerData.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(walker.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(walker);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Scene save failed.");
            SpawnState();
        }
        private static void SpawnState()
        {
            walker=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            var cc=walker.GetComponent<CharacterController>();
            File.WriteAllText(SpawnDir+"/State.txt",$"Play={Application.isPlaying} PlayerCount={UnityEngine.Object.FindObjectsByType<FirstPersonPlayerMotor>(FindObjectsSortMode.None).Length} Position={walker.transform.position:F3} Yaw={walker.transform.eulerAngles.y} Camera={walker.PlayerCamera.eulerAngles} Grounded={cc.isGrounded} FreeMove={walker.Settings.EditorPlaytestFreeMovementEnabled} Settings={AssetDatabase.GetAssetPath(walker.Settings)}");
        }
        private static Mouse reviewMouse;
        private static double spawnInputStart, spawnInputCapture;
        private static StringBuilder spawnInputLog;
        private static void BeginSpawnInput()
        {
            RequirePlay();StopWalk();walker=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            if(walker.Settings.EditorPlaytestFreeMovementEnabled)throw new InvalidOperationException("Saved grounded settings required.");
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            keyboard=InputSystem.AddDevice<Keyboard>();reviewMouse=InputSystem.AddDevice<Mouse>();
            spawnInputStart=EditorApplication.timeSinceStartup;spawnInputCapture=0;frameIndex=0;spawnInputLog=new StringBuilder();
            EditorApplication.update+=DriveSpawnInput;
        }
        private static void DriveSpawnInput()
        {
            var elapsed=EditorApplication.timeSinceStartup-spawnInputStart;
            if(!Application.isPlaying || elapsed>5)
            {
                File.WriteAllText(SpawnDir+"/InputReview.txt",spawnInputLog.ToString());StopWalk();SpawnState();return;
            }
            var keys=elapsed<.4?new KeyboardState(Key.W):elapsed<.8?new KeyboardState(Key.S):elapsed<1.2?new KeyboardState(Key.A):elapsed<1.6?new KeyboardState(Key.D):new KeyboardState();
            InputSystem.QueueStateEvent(keyboard,keys);
            InputSystem.QueueStateEvent(reviewMouse,new MouseState{delta=elapsed>1.8&&elapsed<4.2?new Vector2(16,0):Vector2.zero});
            if(EditorApplication.timeSinceStartup>=spawnInputCapture)
            {
                CapturePlayerView(SpawnDir+"/Input_"+(frameIndex++).ToString("D2")+".png");
                spawnInputLog.AppendLine($"time={elapsed:F2} pos={walker.transform.position:F3} camera={walker.PlayerCamera.eulerAngles:F2} grounded={walker.GetComponent<CharacterController>().isGrounded}");
                spawnInputCapture=EditorApplication.timeSinceStartup+.4;
            }
        }
        private static void FloorInventory()
        {
            Directory.CreateDirectory(WalkDir);
            var scene = SceneManager.GetActiveScene();
            var b = new StringBuilder($"Play={Application.isPlaying} Dirty={scene.isDirty}\n");
            foreach (var root in scene.GetRootGameObjects())
            {
                b.AppendLine("ROOT " + root.name);
                foreach (var t in root.GetComponentsInChildren<Transform>())
                {
                    if (root.name.StartsWith("Cargo ") && t.GetComponent<MeshFilter>())
                        b.AppendLine($"CARGO {t.name} mesh={t.GetComponent<MeshFilter>().sharedMesh?.name} colliders={string.Join(",", t.GetComponents<Collider>().Select(c => c.GetType().Name + ":" + c.enabled))}");
                    if (!t.name.ToLowerInvariant().Contains("floor") && !t.name.Contains("Fitted")) continue;
                    var path = t.name;
                    for (var p = t.parent; p; p = p.parent) path = p.name + "/" + path;
                    b.AppendLine(path + " pos=" + t.position.ToString("F3") + " scale=" + t.lossyScale.ToString("F3"));
                    foreach (var c in t.GetComponents<Collider>()) b.AppendLine($"  {c.GetType().Name} enabled={c.enabled} trigger={c.isTrigger} bounds={c.bounds}");
                    var mesh = t.GetComponent<MeshFilter>();
                    if (mesh && mesh.sharedMesh) b.AppendLine($"  mesh={AssetDatabase.GetAssetPath(mesh.sharedMesh)} bounds={mesh.sharedMesh.bounds}");
                }
            }
            var player = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            if (player) b.AppendLine($"PLAYER scale={player.transform.lossyScale} cameraLocal={player.PlayerCamera.localPosition} parent={player.PlayerCamera.parent.name} settings={AssetDatabase.GetAssetPath(player.Settings)} cameraWorld={player.PlayerCamera.position}");
            foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) b.AppendLine($"CAMERA {camera.name} enabled={camera.enabled} depth={camera.depth} pos={camera.transform.position} rot={camera.transform.eulerAngles}");
            File.WriteAllText(WalkDir + "/Floors.txt", b.ToString());
        }
        private static void RepairFloors()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Edit Mode required.");
            var scene = SceneManager.GetActiveScene();
            Directory.CreateDirectory(WalkDir);
            if (!File.Exists(WalkDir + "/Before.unity.txt")) File.Copy(scene.path, WalkDir + "/Before.unity.txt");
            if (scene.isDirty && (!EditorSceneManager.SaveScene(scene, WalkDir + "/BeforeSnapshot.unity", true) || File.ReadAllText(WalkDir + "/BeforeSnapshot.unity") != File.ReadAllText(scene.path)))
                throw new InvalidOperationException("Unsaved content differs; preserve it.");
            var root = scene.GetRootGameObjects().Single(r => r.name == "Approved Ship Corridor Segments");
            var floors = root.GetComponentsInChildren<BoxCollider>().Where(c => c.name.EndsWith(" straight floor slab")).ToArray();
            if (floors.Length != 5) throw new InvalidOperationException("Expected five active authored floor slabs.");
            foreach (var c in floors) { Undo.RecordObject(c, "Restore walking floor collision"); c.enabled = true; c.isTrigger = false; }
            var roomNames = new[] { "Approved Engine Room 01 Shell", "Approved Cockpit 01 Structure", "Approved Control Room 01 Shell", "Approved Armory 01 Shell", "Approved Supply Room 01 Shell" };
            var repaired = new StringBuilder();
            foreach (var room in scene.GetRootGameObjects().Where(r=>roomNames.Contains(r.name)))
            foreach (var mesh in room.GetComponentsInChildren<MeshFilter>().Where(m=>m.name.EndsWith("floor continuation") || m.name.EndsWith("deck floor") || m.name == "ER-01 sealed full circular floor deck" || m.name == "front wide bay floor slab" || m.name == "rear center stem floor slab" || m.name == "AR-09 cargo hold descending ramp floor"))
            {
                var cols=mesh.GetComponents<Collider>();
                if(cols.Length==0) { var col=Undo.AddComponent<MeshCollider>(mesh.gameObject); col.sharedMesh=mesh.sharedMesh; cols=new Collider[]{col}; }
                foreach(var col in cols) { Undo.RecordObject(col,"Restore room entrance floor collision"); col.enabled=true; col.isTrigger=false; }
                repaired.AppendLine(room.name+"/"+mesh.name);
            }
            File.WriteAllText(WalkDir+"/RepairedRoomFloors.txt",repaired.ToString());
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Save failed.");
            FloorInventory();
        }
        private static void RequirePlay()
        {
            if (!Application.isPlaying || EditorApplication.isPaused) throw new InvalidOperationException("Running, unpaused Play Mode required.");
            Application.runInBackground = true;
        }
        private static Keyboard keyboard;
        private static Vector3[] route;
        private static int routePoint;
        private static StringBuilder routeLog;
        private static double routeDeadline, routeCapture, routeArrived;
        private static string routeOutput;
        private static InputSettings.BackgroundBehavior? reviewBackgroundBehavior;
        private static void BeginRoute(ActionData action)
        {
            RequirePlay(); StopWalk();
            reviewBackgroundBehavior=InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            Application.runInBackground=true;
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            var points = new System.Collections.Generic.List<Vector3>();
            if (action.index < 5)
            {
                var parent = roots.Single(r => r.name == "Approved Ship Corridor Segments").transform.Find("SC-H0" + (action.index + 1) + " horizontal corridor sample");
                var floor = parent.GetComponentsInChildren<BoxCollider>().Single(c => c.name.EndsWith(" straight floor slab"));
                var a = floor.transform.TransformPoint(new Vector3(-.5f, .5f, 0));
                var b = floor.transform.TransformPoint(new Vector3(.5f, .5f, 0));
                var along = (b-a).normalized;
                points.Add(a-along*1.4f); points.Add(a); points.Add(b); points.Add(b+along*1.4f);
            }
            else
            {
                var ids = new[] { "Engine", "Cockpit", "Control", "Armory", "Supply" };
                var root = roots.Single(r => r.name == "Cargo " + ids[action.index-5] + " Corridor and Contacts");
                var segments = root.GetComponentsInChildren<MeshFilter>().Where(m => System.Text.RegularExpressions.Regex.IsMatch(m.name, @" (RoomContact|Body18m|WarehouseContact) \d+ 0$"))
                    .OrderBy(m => int.Parse(m.name.Split(' ')[2])).ToArray();
                foreach (var segment in segments)
                {
                    var v = segment.GetComponent<MeshCollider>().sharedMesh.vertices;
                    points.Add(segment.transform.TransformPoint((v[0]+v[3])*.5f));
                }
                var last = segments.Last(); var end = last.GetComponent<MeshCollider>().sharedMesh.vertices;
                points.Add(last.transform.TransformPoint((end[1]+end[2])*.5f));
            }
            // Offset along the existing visible floor; geometry is never moved for the review.
            if(action.action=="clearanceRoute"||action.action=="wallTransit")
            {
                var first=(points[1]-points[0]);first.y=0;first.Normalize();
                var lastDirection=points[points.Count-1]-points[points.Count-2];lastDirection.y=0;lastDirection.Normalize();
                points.Insert(0,points[0]-first*4f);
                points.Add(points[points.Count-1]+lastDirection*4f);
                if(action.index==0) {points[0]=new Vector3(36.3f,3.25f,19.7f);points[points.Count-1]=new Vector3(51.2f,2.65f,34.79f);}
                if(action.index==1) {points[0]=new Vector3(60.8f,2.65f,34.79f);points[points.Count-2]=new Vector3(76.4f,2.727f,21.68f);points[points.Count-1]=new Vector3(82f,2.75f,21.68f);}
                if(action.index==4) {points[0]=new Vector3(40.2f,3.25f,14.52f);points[points.Count-2]=new Vector3(76.5f,2.727f,17.698f);points[points.Count-1]=new Vector3(82f,2.75f,17.698f);}
            }
            if(action.points!=null&&action.points.Length>1)points=action.points.ToList();
            route = points.Select((p,i) => {
                var along = points[Math.Min(i+1,points.Count-1)]-points[Math.Max(i-1,0)];
                along.y=0; return p+Vector3.Cross(Vector3.up,along.normalized)*action.lateral;
            }).ToArray();
            if(action.side == -1) Array.Reverse(route);
            walker = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            originalSettings = walker.Settings;
            walkSettings = UnityEngine.Object.Instantiate(originalSettings); walkSettings.hideFlags=HideFlags.HideAndDontSave;
            var settings = new SerializedObject(walkSettings);
            settings.FindProperty("editorPlaytestFreeMovementEnabled").boolValue=false;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var start = route[0];
            var hits = Physics.RaycastAll(start+Vector3.up*.6f, Vector3.down, 1.5f, ~0, QueryTriggerInteraction.Ignore).Where(h=>h.normal.y>.5f).OrderByDescending(h=>h.point.y).ToArray();
            if(hits.Length>0) start.y=hits[0].point.y;
            var cc=walker.GetComponent<CharacterController>(); cc.enabled=false;
            var direction=route[1]-route[0]; direction.y=0;
            walker.transform.SetPositionAndRotation(start+Vector3.up*.04f,Quaternion.LookRotation(direction));
            walker.PlayerCamera.localRotation=Quaternion.identity;
            walker.Configure(walkSettings,walker.GetComponent<FirstPersonPlayerInput>(),walker.PlayerCamera); cc.enabled=true;
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            keyboard=InputSystem.AddDevice<Keyboard>();
            routePoint=1; routeDeadline=EditorApplication.timeSinceStartup+30; routeCapture=0; routeArrived=0; frameIndex=0;
            routeOutput=WalkDir+"/Route_"+action.index+"_"+action.side+"_"+action.lateral.ToString("F1",System.Globalization.CultureInfo.InvariantCulture);
            if(action.action=="clearanceRoute")routeOutput=ClearanceDir+"/Route_"+action.index+"_"+action.side+"_"+action.lateral.ToString("F1",System.Globalization.CultureInfo.InvariantCulture);
            if(action.action=="wallProbe"||action.action=="wallTransit")routeOutput=WallDir+"/"+action.action+"_"+action.index+"_"+action.side+"_"+action.lateral.ToString("F1",System.Globalization.CultureInfo.InvariantCulture);
            if(action.action=="partitionRoute")routeOutput=PartitionDir+"/Route_"+action.side+"_"+action.lateral.ToString("F1",System.Globalization.CultureInfo.InvariantCulture);
            if(action.action=="wallProbe")routeDeadline=EditorApplication.timeSinceStartup+4;
            if(!string.IsNullOrEmpty(action.label)) routeOutput+="_"+action.label;
            if(action.label=="Closed") routeDeadline=EditorApplication.timeSinceStartup+4;
            Directory.CreateDirectory(routeOutput);
            File.WriteAllLines(routeOutput+"/Path.txt",route.Select(p=>p.ToString("F3")));
            routeLog=new StringBuilder($"START {start:F3} freeMove={walkSettings.EditorPlaytestFreeMovementEnabled}\n");
            EditorApplication.update+=DriveRoute;
        }
        private static void DriveRoute()
        {
            if(!Application.isPlaying || !walker) { StopWalk(); return; }
            var position=walker.transform.position;
            var delta=route[routePoint]-position; delta.y=0;
            while(delta.magnitude<.3f && routePoint<route.Length-1) { routePoint++; delta=route[routePoint]-position; delta.y=0; }
            var finished=routePoint==route.Length-1 && delta.magnitude<.3f;
            if(finished && routeArrived==0) routeArrived=EditorApplication.timeSinceStartup;
            var fallen=position.y<route[routePoint].y-1.5f;
            var expired=EditorApplication.timeSinceStartup>routeDeadline;
            if(EditorApplication.timeSinceStartup>=routeCapture || fallen || expired)
            {
                CapturePlayerView(routeOutput+"/"+(frameIndex++).ToString("D3")+".png");
                var input=walker.GetComponent<FirstPersonPlayerInput>();
                var cc=walker.GetComponent<CharacterController>();
                routeLog.AppendLine($"point={routePoint}/{route.Length} player={position:F3} camera={walker.PlayerCamera.position:F3} rotation={walker.PlayerCamera.eulerAngles:F2} grounded={cc.isGrounded} move={input.Move} suppressed={input.GameplayActionInputSuppressed} height={cc.height} flags={cc.collisionFlags} focused={Application.isFocused}");
                foreach(var hit in Physics.CapsuleCastAll(position+cc.center+Vector3.up*(cc.height*.5f-cc.radius),position+cc.center-Vector3.up*(cc.height*.5f-cc.radius),cc.radius*.95f,delta.normalized,.4f,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider!=cc))
                    routeLog.AppendLine($"BLOCK {hit.collider.transform.root.name}/{hit.collider.name} point={hit.point:F3} normal={hit.normal:F3}");
                routeCapture=EditorApplication.timeSinceStartup+.7;
            }
            if((finished && EditorApplication.timeSinceStartup-routeArrived>.7) || fallen || expired)
            {
                routeLog.AppendLine($"END reached={finished} fallen={fallen} timeout={expired}");
                if(expired || fallen)
                {
                    foreach(var col in Physics.OverlapSphere(position+Vector3.up*.9f,2.5f,~0,QueryTriggerInteraction.Ignore))
                    {
                        var path=col.name;for(var p=col.transform.parent;p;p=p.parent)path=p.name+"/"+path;
                        routeLog.AppendLine($"CONTACT {path} type={col.GetType().Name} bounds={col.bounds.ToString("F3")}");
                    }
                }
                File.WriteAllText(routeOutput+"/Result.txt",routeLog.ToString()); StopWalk(); return;
            }
            if(!finished) walker.transform.rotation=Quaternion.LookRotation(delta.normalized);
            InputSystem.QueueStateEvent(keyboard,finished?new KeyboardState():new KeyboardState(Key.W));
        }
        private static void CapturePlayerView(string path)
        {
            var cam=walker.PlayerCamera.GetComponent<Camera>();
            var rt=RenderTexture.GetTemporary(800,450,24); var previous=cam.targetTexture; var active=RenderTexture.active;
            var tex=new Texture2D(800,450,TextureFormat.RGB24,false);
            try { cam.targetTexture=rt; cam.Render(); RenderTexture.active=rt; tex.ReadPixels(new Rect(0,0,800,450),0,0); tex.Apply(); File.WriteAllBytes(path,tex.EncodeToPNG()); }
            finally { cam.targetTexture=previous; RenderTexture.active=active; UnityEngine.Object.DestroyImmediate(tex); RenderTexture.ReleaseTemporary(rt); }
        }
        private static FirstPersonPlayerSettings walkSettings;
        private static FirstPersonPlayerSettings originalSettings;
        private static FirstPersonPlayerMotor walker;
        private static double walkUntil, nextFrame;
        private static string walkName;
        private static int frameIndex;
        private static Vector3 walkStart;
        private static void BeginWalk(ActionData action)
        {
            RequirePlay();
            StopWalk();
            walker = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            if (!walker) throw new InvalidOperationException("Existing Player required.");
            originalSettings = walker.Settings;
            walkSettings = UnityEngine.Object.Instantiate(originalSettings);
            walkSettings.hideFlags = HideFlags.HideAndDontSave;
            var settings = new SerializedObject(walkSettings);
            settings.FindProperty("editorPlaytestFreeMovementEnabled").boolValue = false;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var doors = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PegasusEntranceDoor>()).ToArray();
            var door = doors[action.index];
            var serialized = new SerializedObject(door);
            var closed = serialized.FindProperty("parts").GetArrayElementAtIndex(0).FindPropertyRelative("closedPosition").vector3Value;
            var closedScale = serialized.FindProperty("parts").GetArrayElementAtIndex(0).FindPropertyRelative("closedScale").vector3Value;
            var center = door.transform.parent.TransformPoint(closed);
            var direction = door.transform.right * action.side;
            var lateral = door.transform.forward * action.lateral;
            var bottom = center.y - closedScale.y * door.transform.parent.lossyScale.y * .5f;
            var position = center - direction * 1.4f + lateral;
            position.y = bottom + .3f;
            var ground = Physics.RaycastAll(new Vector3(position.x, center.y + 2, position.z), Vector3.down, 6, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => h.point.y < center.y - .3f).OrderByDescending(h => h.point.y).ToArray();
            if (ground.Length > 0) position.y = ground[0].point.y + .03f;
            // Position only the starting point; traversal itself uses InputAction -> motor -> CharacterController.
            var controller = walker.GetComponent<CharacterController>();
            controller.enabled = false;
            walker.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
            walker.PlayerCamera.localRotation = Quaternion.identity;
            walker.Configure(walkSettings, walker.GetComponent<FirstPersonPlayerInput>(), walker.PlayerCamera);
            controller.enabled = true;
            walkStart = position;
            Application.runInBackground = true;
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            keyboard = InputSystem.AddDevice<Keyboard>();
            walkUntil = EditorApplication.timeSinceStartup + .85;
            nextFrame = 0; frameIndex = 0;
            walkName = "Walk_" + action.index.ToString("D2") + "_" + action.side + "_" + action.lateral.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
            EditorApplication.update += DriveWalk;
        }
        private static void DriveWalk()
        {
            if (!Application.isPlaying || EditorApplication.timeSinceStartup >= walkUntil)
            {
                if (walker) File.WriteAllText(Dir + "/" + walkName + ".txt", $"Start={walkStart:F3} End={walker.transform.position:F3} Grounded={walker.GetComponent<CharacterController>().isGrounded} FreeMove={walker.Settings.EditorPlaytestFreeMovementEnabled}");
                StopWalk(); return;
            }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            if (EditorApplication.timeSinceStartup >= nextFrame)
            {
                ScreenCapture.CaptureScreenshot(Dir + "/" + walkName + "_" + (frameIndex++).ToString("D2") + ".png");
                nextFrame = EditorApplication.timeSinceStartup + .25;
            }
        }
        private static void StopWalk()
        {
            EditorApplication.update-=DriveClearanceSweep;
            if(reviewBackgroundBehavior.HasValue){InputSystem.settings.backgroundBehavior=reviewBackgroundBehavior.Value;reviewBackgroundBehavior=null;}
            EditorApplication.update -= DriveSpawnInput;
            if(reviewMouse!=null && reviewMouse.added)InputSystem.RemoveDevice(reviewMouse);
            reviewMouse=null;
            EditorApplication.update -= DriveRoute;
            EditorApplication.update -= DriveWalk;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            keyboard = null;
            if (walker && originalSettings)
                walker.Configure(originalSettings, walker.GetComponent<FirstPersonPlayerInput>(), walker.PlayerCamera);
            if (walkSettings) UnityEngine.Object.DestroyImmediate(walkSettings);
            walkSettings = null; originalSettings = null;
        }
        internal static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/_Project/Scenes/Pegasus.unity" || scene.isDirty || Application.isPlaying)
                throw new InvalidOperationException("Clean Pegasus Edit Mode required; user changes will not be discarded.");
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var states = all.Select(t => t.GetComponent<ShipDeviceInteractionState>()).Where(s => s).ToArray();
            if (states.Length != 1) throw new InvalidOperationException("Expected one existing ShipDeviceInteractionState, found " + states.Length);
            var panels = all.Where(t => t.gameObject.activeInHierarchy && t.name.EndsWith("lowered full height closure shutter")).ToArray();
            if (panels.Length != 20 || all.Any(t => t.GetComponent<PegasusEntranceDoor>()))
                throw new InvalidOperationException("Unexpected doors or previously applied door setup; inspect before changes.");
            var preparations = panels.Select(panel => Prepare(panel, states[0])).ToArray();
            Directory.CreateDirectory(Dir);
            File.Copy(scene.path, Dir + "/PegasusBefore.unity.txt", false);
            foreach (var setup in preparations)
            {
                var door = Undo.AddComponent<PegasusEntranceDoor>(setup.panel.gameObject);
                foreach (var part in setup.parts)
                {
                    Undo.RecordObject(part.transform, "Open internal entrance shutter");
                    Undo.RecordObject(part.transform.gameObject, "Remove moving shutter static flags");
                    GameObjectUtility.SetStaticEditorFlags(part.transform.gameObject, 0);
                    foreach (var col in part.colliders) Undo.RecordObject(col, "Open door collider");
                }
                door.Configure(states[0], setup.a, setup.b, setup.index, setup.parts);
                EditorUtility.SetDirty(door);
            }
            var moving = preparations.SelectMany(s => s.parts).Select(p => p.transform.GetComponent<MeshRenderer>()).ToHashSet();
            foreach (var budget in all.Select(t => t.GetComponent<PegasusInteriorRenderBudget>()).Where(b => b))
            {
                var serialized = new SerializedObject(budget);
                var refs = serialized.FindProperty("stationaryParts");
                for (var i = refs.arraySize - 1; i >= 0; i--)
                {
                    if (!moving.Contains(refs.GetArrayElementAtIndex(i).objectReferenceValue as MeshRenderer)) continue;
                    refs.GetArrayElementAtIndex(i).objectReferenceValue = null;
                    refs.DeleteArrayElementAtIndex(i);
                }
                serialized.ApplyModifiedProperties();
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
            Inspect();
        }

        private sealed class Setup
        {
            public Transform panel;
            public ShipRoomId a, b;
            public int index;
            public PegasusEntranceDoor.Part[] parts;
        }

        private static Setup Prepare(Transform panel, ShipDeviceInteractionState state)
        {
            var parent = panel.parent;
            var prefix = panel.name.Substring(0, panel.name.Length - "lowered full height closure shutter".Length);
            var suffixes = new[] { "lowered full height closure shutter", "shutter center armor face", "red closure warning strip" };
            var transforms = suffixes.Select(s => parent.Cast<Transform>().Single(t => t.name == prefix + s)).ToArray();
            var housing = parent.Cast<Transform>().Single(t => t.name == prefix + "overhead shutter housing").GetComponent<Renderer>().bounds;
            var bounds = panel.GetComponent<Renderer>().bounds;
            // Compress the existing shutter into its housing, never lift a full-height slab through the roof.
            var factor = Mathf.Min(.06f, housing.size.y * .7f / bounds.size.y);
            var parts = transforms.Select(t =>
            {
                var position = t.position;
                position.y = housing.center.y + (position.y - bounds.center.y) * factor;
                var scale = t.localScale;
                scale.y *= factor;
                var colliders = t.GetComponents<Collider>();
                return new PegasusEntranceDoor.Part
                {
                    transform = t, closedPosition = t.localPosition, closedScale = t.localScale,
                    openPosition = parent.InverseTransformPoint(position), openScale = scale,
                    colliders = colliders, closedColliderEnabled = colliders.Select(c => t == panel || c.enabled).ToArray()
                };
            }).ToArray();
            var setup = new Setup { panel = panel, parts = parts };
            var root = panel.root.name;
            if (root.StartsWith("Cargo ") && root.EndsWith(" Corridor and Contacts"))
            {
                setup.a = ShipRoomId.CargoHold;
                switch (root)
                {
                    case "Cargo Cockpit Corridor and Contacts": setup.b = ShipRoomId.Cockpit; setup.index = 0; break;
                    case "Cargo Engine Corridor and Contacts": setup.b = ShipRoomId.EngineRoom; setup.index = 1; break;
                    case "Cargo Control Corridor and Contacts": setup.b = ShipRoomId.ControlRoom; setup.index = 2; break;
                    case "Cargo Armory Corridor and Contacts": setup.b = ShipRoomId.Armory; setup.index = 3; break;
                    case "Cargo Supply Corridor and Contacts": setup.b = ShipRoomId.SupplyRoom; setup.index = 4; break;
                    default: throw new InvalidOperationException("Unmapped cargo route.");
                }
            }
            else if (root == "Approved Ship Corridor Segments")
            {
                switch (parent.name)
                {
                    case "SC-H01 horizontal corridor sample": setup.a = ShipRoomId.Cockpit; setup.b = ShipRoomId.EngineRoom; setup.index = 6; break;
                    case "SC-H02 horizontal corridor sample": setup.a = ShipRoomId.Cockpit; setup.b = ShipRoomId.ControlRoom; setup.index = 7; break;
                    case "SC-H03 horizontal corridor sample": setup.a = ShipRoomId.ControlRoom; setup.b = ShipRoomId.Armory; setup.index = 9; break;
                    case "SC-H04 horizontal corridor sample": setup.a = ShipRoomId.SupplyRoom; setup.b = ShipRoomId.Armory; setup.index = 5; break;
                    case "SC-H05 horizontal corridor sample": setup.a = ShipRoomId.EngineRoom; setup.b = ShipRoomId.ControlRoom; setup.index = 8; break;
                    default: throw new InvalidOperationException("Hidden/unknown corridor must not be changed.");
                }
            }
            else throw new InvalidOperationException("Door outside approved roots: " + root);
            return setup;
        }
        internal static void Observe()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/_Project/Scenes/Pegasus.unity") throw new InvalidOperationException("Pegasus required.");
            var doors = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>())
                .Where(t => t.name.EndsWith("lowered full height closure shutter")).ToArray();
            var view = SceneView.lastActiveSceneView;
            if (!view) throw new InvalidOperationException("Open Scene view required.");
            Directory.CreateDirectory(Dir);
            var previousPosition = view.pivot;
            var previousRotation = view.rotation;
            var previousSize = view.size;
            var action = JsonUtility.FromJson<ActionData>(File.ReadAllText(Dir + "/Action.json"));
            var side = action.side == -1 ? -1 : 1;
            var label = string.IsNullOrEmpty(action.label) ? "Review" : action.label;
            var output = Dir + "/" + label;
            Directory.CreateDirectory(output);
            try
            {
                for (var i = 0; i < doors.Length; i++)
                {
                    var door = doors[i];
                    var direction = door.right * side;
                    var center = door.position;
                    var installed = door.GetComponent<PegasusEntranceDoor>();
                    if (installed)
                        center = door.parent.TransformPoint(new SerializedObject(installed).FindProperty("parts").GetArrayElementAtIndex(0).FindPropertyRelative("closedPosition").vector3Value);
                    var cameraPosition = center - direction * 2 + door.forward * action.lateral;
                    view.LookAtDirect(cameraPosition + direction * 3, Quaternion.LookRotation(direction), 3);
                    var cam = Application.isPlaying ? UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>().PlayerCamera.GetComponent<Camera>() : view.camera;
                    var target = RenderTexture.GetTemporary(800, 600, 24);
                    var previousTarget = cam.targetTexture;
                    var savedPosition = cam.transform.position;
                    var savedRotation = cam.transform.rotation;
                    var savedProjection = cam.projectionMatrix;
                    var previousActive = RenderTexture.active;
                    var texture = new Texture2D(800, 600, TextureFormat.RGB24, false);
                    try
                    {
                        cam.targetTexture = target;
                        cam.transform.SetPositionAndRotation(cameraPosition, Quaternion.LookRotation(center - cameraPosition));
                        cam.projectionMatrix = Matrix4x4.Perspective(65, 800f / 600, .03f, 300);
                        cam.Render();
                        RenderTexture.active = target;
                        texture.ReadPixels(new Rect(0, 0, 800, 600), 0, 0);
                        texture.Apply();
                        File.WriteAllBytes(output + "/" + i.ToString("D2") + ".png", texture.EncodeToPNG());
                    }
                    finally
                    {
                        cam.targetTexture = previousTarget;
                        cam.transform.SetPositionAndRotation(savedPosition, savedRotation);
                        cam.projectionMatrix = savedProjection;
                        RenderTexture.active = previousActive;
                        UnityEngine.Object.DestroyImmediate(texture);
                        RenderTexture.ReleaseTemporary(target);
                    }
                }
            }
            finally { view.LookAtDirect(previousPosition, previousRotation, previousSize); }
            Inspect();
            File.Copy(Dir + "/Inventory.txt", output + "/Inventory.txt", true);
        }
        internal static void Inspect()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/_Project/Scenes/Pegasus.unity") throw new InvalidOperationException("Pegasus must already be active.");
            Directory.CreateDirectory(Dir);
            var b = new StringBuilder($"Scene={scene.path} Play={Application.isPlaying} Dirty={scene.isDirty} Paused={EditorApplication.isPaused} Background={Application.runInBackground} Frame={Time.frameCount}\n");
            foreach (var source in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ShipDeviceInteractionState>()))
                if (Application.isPlaying) b.AppendLine($"STATE {source.GetInstanceID()} ControlDurability={source.CurrentShipState.GetRoom(ShipRoomId.ControlRoom).CurrentDurability} Overclock={source.EngineOverclockActivationCount} Purification={source.CurrentControlRoomPurification.IsActive}");
            foreach (var door in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PegasusEntranceDoor>()))
                b.AppendLine($"DOOR {door.name} index={door.CorridorIndex} rooms={door.RoomA}/{door.RoomB} open={door.IsOpen}");
            var near = new Vector3(40.693f, 3.25f, 25.327f);
            foreach (var col in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Collider>()))
                if (Vector3.Distance(col.transform.position, near) < 8)
                    b.AppendLine($"NEAR COLLIDER {col.name} type={col.GetType().Name} enabled={col.enabled} trigger={col.isTrigger} layer={col.gameObject.layer} position={col.transform.position:F3}");
            foreach (var hit in Physics.RaycastAll(near + Vector3.up * 2, Vector3.down, 20, ~0, QueryTriggerInteraction.Ignore))
                b.AppendLine($"GROUND HIT {hit.collider.name} point={hit.point:F3}");
            foreach (var t in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)))
            {
                if (!t.name.Contains("shutter") && !t.name.Contains("Shutter")) continue;
                var path = t.name;
                for (var p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;
                var renderer = t.GetComponent<Renderer>();
                b.AppendLine($"{path} active={t.gameObject.activeInHierarchy} pos={t.position:F3} scale={t.lossyScale:F3} rot={t.eulerAngles:F2} bounds={(renderer ? renderer.bounds.ToString("F3") : "none")} colliders={t.GetComponents<Collider>().Length}");
            }
            var inspectionDir=File.Exists(PartitionDir+"/Action.json")?PartitionDir:File.Exists(WallDir+"/Action.json")?WallDir:File.Exists(ClearanceDir+"/Action.json")?ClearanceDir:File.Exists(SpawnDir+"/Action.json")?SpawnDir:File.Exists(WalkDir+"/Action.json")?WalkDir:Dir;
            File.WriteAllText(inspectionDir + "/Inventory.txt", b.ToString());
            var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
            var logs = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries");
            var entryType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntry");
            var entry = Activator.CreateInstance(entryType);
            var console = new StringBuilder($"Compiling={EditorApplication.isCompiling}\n");
            logs.GetMethod("StartGettingEntries", flags).Invoke(null, null);
            try
            {
                var count = (int)logs.GetMethod("GetCount", flags).Invoke(null, null);
                console.AppendLine("Entries=" + count);
                for (var i = 0; i < count; i++)
                {
                    logs.GetMethod("GetEntryInternal", flags).Invoke(null, new object[] { i, entry });
                    console.AppendLine(entryType.GetField("message", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)?.GetValue(entry)?.ToString());
                }
            }
            finally { logs.GetMethod("EndGettingEntries", flags).Invoke(null, null); }
            File.WriteAllText(inspectionDir + "/Console.txt", console.ToString());
        }
    }
}
