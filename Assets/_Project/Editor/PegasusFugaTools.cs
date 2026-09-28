using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Bellerophon.Enemies.Fuga;
using Bellerophon.Enemies.Seed;
using Bellerophon.Enemies.Parvum;
using Bellerophon.Core.Player;

namespace Bellerophon.Editor
{
    [InitializeOnLoad] internal static class PegasusFugaTools
    {
        const string Output="docs/validation/PegasusFugaAI";
        [Serializable] class Request { public string mode="Inspect"; public string label="Inspect"; }
        static string label;static float nextCapture,start,click;static int frame,lastFrame=-1;static bool driving;
        static int warehouseStage,warehouseHitCount;static float warehouseStageTime;static bool warehouseAligned;
        static string warehousePath;
        static SeedRoomDoor[] sweepDoors;static int sweepIndex,sweepPhase;static float sweepSince,sweepFinished;
        static ParvumTarget observedFood;static double observedConsumption;
        static GameObject acoustic;static Vector3 acousticGoal;static string sweepStatus;static float acousticCheck;
        // Observation adapter only: both species use their own live AI and physics.
        sealed class EntryActor
        {
            public Component Component;
            FugaBrain F=>Component as FugaBrain;
            ParvumBrain P=>Component as ParvumBrain;
            public Transform transform=>Component.transform;
            public float FlightHeight=>F?F.FlightHeight:0;
            public float BodySize=>F?F.BodySize:1;
            public bool Walking=>P;
            public System.Collections.Generic.IReadOnlyList<SeedRoomDoor> Doorways=>F?F.Doorways:P.Doorways;
            public string LastCompletedEntryName=>F?F.LastCompletedEntryName:P.LastCompletedEntryName;
            public float LastCompletedEntryAdvance=>F?F.LastCompletedEntryAdvance:P.LastCompletedEntryAdvance;
            public ParvumTarget CurrentTarget=>F?F.CurrentTarget:P.CurrentTarget;
            public string ActiveEntryName=>F?F.ActiveEntryName:P.ActiveEntryName;
            public bool Consuming=>F?F.Behaviour==FugaBehaviour.Consume:P.Behaviour==ParvumBehaviour.Consume;
        }
        static PegasusFugaTools()
        {
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state==PlayModeStateChange.ExitingPlayMode){InputSystem.onBeforeUpdate-=ObserveTick;Devices("ReleaseReviewDevices");if(acoustic)UnityEngine.Object.Destroy(acoustic);}
                if(state==PlayModeStateChange.EnteredPlayMode && File.Exists(Output+"/Action.json"))
                {var r=JsonUtility.FromJson<Request>(File.ReadAllText(Output+"/Action.json"));if(r.mode=="Play")Start(r.label);}
            };
        }
        public static void Review()
        {
            Directory.CreateDirectory(Output);
            var request=File.Exists(Output+"/Action.json")?JsonUtility.FromJson<Request>(File.ReadAllText(Output+"/Action.json")):new Request();
            if(request.mode=="Inspect")Inspect();
            else if(request.mode=="Apply")Apply();
            else if(request.mode=="Resize")Resize();
            else if(request.mode=="WarehouseInspect")WarehouseInspect();
            else if(request.mode=="WarehouseApply")WarehouseApply();
            else if(request.mode=="DoorsInspect")DoorsInspect();
            else if(request.mode=="FoodInspect")FoodInspect();
            else if(request.mode=="Play")EditorApplication.isPlaying=true;
            else if(request.mode=="Stop")EditorApplication.isPlaying=false;
            else if(request.mode=="Observe")Start(request.label);
            else if(request.mode=="Capture")Capture(request.label);
            else if(request.mode=="State")State(request.label);
        }
        static void DoorsInspect()
        {
            var text=new StringBuilder();
            string[] names={"Approved Cockpit 01 Structure","Approved Engine Room 01 Shell","Approved Control Room 01 Shell","Approved Armory 01 Shell","Approved Supply Room 01 Shell","Approved Cargo Hold 01 Shell"};
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects().Where(x=>names.Contains(x.name)))
            {
                text.AppendLine("ROOT "+root.name);
                var type=typeof(ParvumBrain).Assembly.GetType("Bellerophon.Enemies.Parvum.ParvumRoomEntrance");
                foreach(var door in (System.Collections.IEnumerable)type.GetMethod("Read").Invoke(null,new object[]{root.transform}))
                    text.AppendLine($"DOOR {type.GetField("Name").GetValue(door)} point={(Vector3)type.GetField("Point").GetValue(door):F4} inward={(Vector3)type.GetField("Inward").GetValue(door):F4}");
                foreach(var r in root.GetComponentsInChildren<Renderer>().Where(x=>x.name.Contains("header") || x.name.Contains("Entrance_Slab") || x.name.Contains("doorway")))
                    text.AppendLine($"MESH {r.name} bounds={r.bounds}");
            }
            File.WriteAllText(Output+"/DoorsInspect.txt",text.ToString());
        }
        static void FoodInspect()
        {
            var text=new StringBuilder();
            foreach(var t in UnityEngine.Object.FindObjectsByType<ParvumTarget>(FindObjectsSortMode.None).Where(x=>x.IsRoomWall))
            {
                var c=t.Surface;var renderers=t.GetComponents<Renderer>();
                text.AppendLine($"{t.transform.root.name}/{t.name} surface={(c?c.GetType().Name:"NONE")} enabled={(c && c.enabled)} renderers={string.Join(";",renderers.Select(r=>$"{r.name}:{r.enabled}:{r.bounds}"))} colliderBounds={(c?c.bounds:default)}");
                if(c is MeshCollider mesh)
                {
                    var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                    var vertices=(Vector3[])typeof(ParvumTarget).GetField("surfaceVertices",flags).GetValue(t);
                    var triangles=(int[])typeof(ParvumTarget).GetField("surfaceTriangles",flags).GetValue(t);
                    var cachedBounds=new Bounds();if(vertices!=null && vertices.Length>0){cachedBounds=new Bounds(mesh.transform.TransformPoint(vertices[0]),Vector3.zero);foreach(var v in vertices)cachedBounds.Encapsulate(mesh.transform.TransformPoint(v));}
                    text.AppendLine($"  CACHE vertices={vertices?.Length} triangles={triangles?.Length} meshVertices={mesh.sharedMesh.vertexCount} cachedBounds={cachedBounds} readable={mesh.sharedMesh.isReadable}");
                }
            }
            File.WriteAllText(Output+"/AirBiteFoodInspect.txt",text.ToString());
        }
        static void WarehouseInspect()
        {
            var root=SceneManager.GetActiveScene().GetRootGameObjects().Single(x=>x.name=="Approved Cargo Hold 01 Shell");
            var text=new StringBuilder();
            foreach(var r in root.GetComponentsInChildren<Renderer>(true).Where(x=>x.name.Contains("CH-03") || x.name.Contains("Warehouse wall") || x.name.Contains("deck floor")))
            {text.AppendLine($"{r.name} active={r.gameObject.activeInHierarchy} bounds={r.bounds} pos={r.transform.position:F3}");foreach(var c in r.GetComponents<Collider>())text.AppendLine($" collider {c.GetType().Name} enabled={c.enabled} trigger={c.isTrigger} bounds={c.bounds}");}
            foreach(var c in root.GetComponentsInChildren<Collider>(true).Where(x=>!x.GetComponent<Renderer>()))text.AppendLine($"EXTRA {c.name} {c.bounds} enabled={c.enabled} trigger={c.isTrigger}");
            File.WriteAllText(Output+"/WarehouseInspect.txt",text.ToString());
        }
        static void WarehouseApply()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying || scene.name!="Pegasus" || scene.isDirty)throw new InvalidOperationException("Clean Pegasus edit scene required.");
            var root=scene.GetRootGameObjects().Single(x=>x.name=="Approved Cargo Hold 01 Shell");
            foreach(var c in root.GetComponentsInChildren<Collider>(true).Where(x=>x.name.StartsWith("CH-03 single central cargo container"))){c.enabled=true;c.isTrigger=false;}
            var box=root.GetComponentsInChildren<BoxCollider>().Single(x=>x.name=="CH-03 single central cargo container body");
            var obstacle=box.GetComponent<UnityEngine.AI.NavMeshObstacle>();if(!obstacle)obstacle=box.gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            var bounds=new Bounds(box.center,box.size);
            foreach(var part in root.GetComponentsInChildren<BoxCollider>().Where(x=>x.name.StartsWith("CH-03 single central cargo container")))
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                    bounds.Encapsulate(box.transform.InverseTransformPoint(part.transform.TransformPoint(part.center+Vector3.Scale(part.size*.5f,new Vector3(x,y,z)))));
            var scale=box.transform.lossyScale;bounds.Expand(new Vector3(.3f/Mathf.Abs(scale.x),0,.3f/Mathf.Abs(scale.z)));
            obstacle.shape=UnityEngine.AI.NavMeshObstacleShape.Box;obstacle.center=bounds.center;obstacle.size=bounds.size;obstacle.carving=true;obstacle.carveOnlyStationary=true;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        static void Resize()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying || scene.name!="Pegasus" || scene.isDirty)throw new InvalidOperationException("Clean Pegasus edit scene required.");
            var brain=UnityEngine.Object.FindFirstObjectByType<FugaBrain>();
            if(!brain || brain.name!="Fuga Gameplay 01")throw new InvalidOperationException("Approved Fuga required.");
            var serialized=new SerializedObject(brain);var size=serialized.FindProperty("bodySize");
            float ratio=1.5f/size.floatValue;
            foreach(Transform slot in brain.transform){slot.localScale*=ratio;slot.localPosition*=ratio;}
            brain.GetComponent<SphereCollider>().radius*=ratio;
            size.floatValue=1.5f;serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(brain.gameObject,"Assets/_Project/Prefabs/Enemies/Fuga/Gameplay/FugaGameplay.prefab");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        static void Apply()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying || scene.name!="Pegasus" || scene.isDirty)throw new InvalidOperationException("Clean Pegasus edit scene required.");
            if(UnityEngine.Object.FindFirstObjectByType<FugaBrain>(FindObjectsInactive.Include))throw new InvalidOperationException("Fuga already exists; no duplicate apply.");
            var parvum=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>(FindObjectsInactive.Include);
            var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            if(!parvum || !player)throw new InvalidOperationException("Original actors required.");
            var source=EditorSceneManager.OpenScene("Assets/_Project/Scenes/CargoRunMvp.unity",OpenSceneMode.Additive);
            GameObject actor=null;
            try
            {
                var original=source.GetRootGameObjects().Single(x=>x.name=="Approved Fuga Enemy Placement").transform;
                actor=new GameObject("Fuga Gameplay 01");actor.SetActive(false);SceneManager.MoveGameObjectToScene(actor,scene);
                var names=new[]{"Fuga_01_Idle","Fuga_02_Move","Fuga_03_Attack","Fuga_04_Hit","Fuga_05_Death","Fuga_06_Consume"};
                var slots=new GameObject[names.Length];
                for(int i=0;i<slots.Length;i++)
                {
                    var originalSlot=original.Find(names[i]);var copy=UnityEngine.Object.Instantiate(originalSlot.gameObject);copy.SetActive(false);
                    SceneManager.MoveGameObjectToScene(copy,scene);copy.transform.SetParent(actor.transform,false);
                    copy.name=names[i];copy.transform.localPosition=Vector3.zero;copy.transform.localRotation=Quaternion.identity;copy.transform.localScale=originalSlot.lossyScale;
                    foreach(var motion in copy.GetComponentsInChildren<FugaPhysicsMotionDriver>(true))UnityEngine.Object.DestroyImmediate(motion);
                    foreach(var review in copy.GetComponentsInChildren<FugaAnimationReviewPlaybackDriver>(true))UnityEngine.Object.DestroyImmediate(review);
                    foreach(var collider in copy.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
                    foreach(var rb in copy.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(rb);
                    var animator=copy.GetComponent<Animator>();if(animator){animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;}
                    var renderer=copy.GetComponentInChildren<SkinnedMeshRenderer>(true);renderer.updateWhenOffscreen=true;
                    // Preserve original scale and place the common torso bone at the gameplay origin.
                    var torso=renderer.bones.FirstOrDefault(x=>x && x.name=="Bone_001");
                    var center=torso?torso.position:renderer.bounds.center;copy.transform.position-=center-actor.transform.position;
                    slots[i]=copy;
                }
                var body=actor.AddComponent<Rigidbody>();body.mass=2;body.useGravity=false;body.constraints=RigidbodyConstraints.FreezeRotation;
                body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                var hull=actor.AddComponent<SphereCollider>();hull.radius=.20f;
                var view=actor.AddComponent<FugaAnimationView>();view.Configure(slots);
                var brain=actor.AddComponent<FugaBrain>();
                float face=player.GetComponentInChildren<Camera>().transform.position.y-player.transform.position.y;
                brain.Configure(face,new SerializedObject(parvum).FindProperty("navigationAgentType").intValue);
                var position=parvum.transform.position;var floor=Physics.RaycastAll(position+Vector3.up*.3f,Vector3.down,2,~0,QueryTriggerInteraction.Ignore).Where(h=>!h.transform.IsChildOf(parvum.transform) && !h.transform.IsChildOf(player.transform)).OrderBy(h=>h.distance).First();
                position.y=floor.point.y+face;
                if(Vector3.ProjectOnPlane(position-player.transform.position,Vector3.up).magnitude<.9f)
                    position+=Vector3.ProjectOnPlane(position-player.transform.position,Vector3.up).normalized*.9f;
                actor.transform.position=position;actor.transform.rotation=parvum.transform.rotation;
                slots[0].SetActive(true);actor.SetActive(true);
                const string prefab="Assets/_Project/Prefabs/Enemies/Fuga/Gameplay";Directory.CreateDirectory(prefab);AssetDatabase.Refresh();
                PrefabUtility.SaveAsPrefabAsset(actor,prefab+"/FugaGameplay.prefab");
                parvum.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(parvum.gameObject);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                File.WriteAllText(Output+"/Applied.txt",$"Original source unchanged. Fuga={position:F4}, face={face:F3}; Parvum inactive. Direct verification pending.");
            }
            finally{SceneManager.SetActiveScene(scene);EditorSceneManager.CloseScene(source,true);}
        }
        static void Devices(string method)
        {typeof(PegasusStickTools).GetMethod(method,System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)?.Invoke(null,null);}
        static void Start(string name)
        {
            if(name.Contains("SeedParvum"))
            {
                if(!EditorApplication.isPlaying)throw new InvalidOperationException("Runtime-only species switch required.");
                var flyer=UnityEngine.Object.FindFirstObjectByType<FugaBrain>(FindObjectsInactive.Include);
                var walker=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>(FindObjectsInactive.Include);
                flyer.gameObject.SetActive(false);walker.gameObject.SetActive(true);
            }
            sweepDoors=null;sweepIndex=0;sweepPhase=0;sweepSince=Time.time;sweepFinished=0;sweepStatus="";warehouseAligned=false;
            if(name.Contains("From") && int.TryParse(name.Substring(name.IndexOf("From",StringComparison.Ordinal)+4),out var firstDoor))sweepIndex=firstDoor;
            label=name;start=Time.time;nextCapture=0;frame=0;lastFrame=-1;click=0;warehouseStage=0;warehouseHitCount=0;warehouseStageTime=Time.time;driving=name.Contains("Combat") || name.Contains("Throw") || name.Contains("Pursuit") || name.Contains("WarehouseWalk") || name.Contains("WarehouseReturn");
            if(name.Contains("Cargo") || name.Contains("Speaker"))
            {
                var enemy=UnityEngine.Object.FindFirstObjectByType<FugaBrain>();
                var fixture=GameObject.CreatePrimitive(PrimitiveType.Cube);fixture.name="Runtime only Fuga "+name;
                fixture.transform.position=enemy.transform.position+enemy.transform.forward*1.2f;
                fixture.transform.localScale=Vector3.one*.4f;
                var target=fixture.AddComponent<ParvumTarget>();
                if(name.Contains("Cargo"))target.ConfigureCargo(fixture.GetComponent<Collider>(),UnityEngine.Object.FindFirstObjectByType<Bellerophon.Core.Ship.ShipDeviceInteractionState>(),null,Bellerophon.Core.Session.CargoMaterial.CommonMetal);
                else{target.ConfigureSpeaker(null,fixture.GetComponent<Collider>());target.SetSpeakerAudible(true);}
                if(name.Contains("Multiple"))
                {
                    var other=GameObject.CreatePrimitive(PrimitiveType.Cube);other.name="Runtime only farther speaker";other.transform.position=enemy.transform.position+enemy.transform.forward*3+enemy.transform.right;other.transform.localScale=Vector3.one*.4f;
                    var sound=other.AddComponent<ParvumTarget>();sound.ConfigureSpeaker(null,other.GetComponent<Collider>());sound.SetSpeakerAudible(true);
                }
            }
            if(driving){Devices("AcquireReviewDevices");EditorWindow.GetWindow(typeof(EditorApplication).Assembly.GetType("UnityEditor.GameView")).Focus();}
            InputSystem.onBeforeUpdate-=ObserveTick;InputSystem.onBeforeUpdate+=ObserveTick;
        }
        static void ObserveTick()
        {
            if(!EditorApplication.isPlaying){InputSystem.onBeforeUpdate-=ObserveTick;return;}
            if(InputState.currentUpdateType!=InputUpdateType.Dynamic || lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
            var enemy=UnityEngine.Object.FindFirstObjectByType<FugaBrain>();
            if(Time.time-start>(label.Contains("DoorSweep")?3600:label.Contains("Warehouse")?200:110) || (!enemy && !label.Contains("SeedParvum")))
            {InputSystem.onBeforeUpdate-=ObserveTick;if(driving){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());InputSystem.QueueStateEvent(Mouse.current,new MouseState());Devices("ReleaseReviewDevices");}State(label+"End");return;}
            if(label.Contains("DoorSweep"))
            {
                DoorSweep(new EntryActor{Component=label.Contains("SeedParvum")?(Component)UnityEngine.Object.FindFirstObjectByType<ParvumBrain>():enemy});
                if(Time.time>=nextCapture){Capture(label+"_D"+sweepIndex.ToString("D2")+"_P"+sweepPhase+"_"+(frame++).ToString("D4"));nextCapture=Time.time+(sweepPhase==0?1:.15f);}
                if(sweepDoors!=null && sweepIndex>=sweepDoors.Length){InputSystem.onBeforeUpdate-=ObserveTick;State(label+"End");}
                return;
            }
            if(driving)
            {
                if(label.Contains("SeedParvum"))
                {
                    Devices("EnableReviewDevices");
                    var walker=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>();
                    var carrier=UnityEngine.Object.FindFirstObjectByType<PegasusStickController>();
                    var controls=(FirstPersonPlayerSettings)new SerializedObject(carrier.GetComponent<FirstPersonPlayerMotor>()).FindProperty("settings").objectReferenceValue;
                    WarehouseInput(carrier,walker,controls,out var keys,out var pointer);
                    InputSystem.QueueStateEvent(Mouse.current,pointer);InputSystem.QueueStateEvent(Keyboard.current,keys);
                    if(Time.time>=nextCapture){Capture(label+"_"+(frame++).ToString("D4"));nextCapture=Time.time+.25f;}
                    return;
                }
                Devices("EnableReviewDevices");var player=UnityEngine.Object.FindFirstObjectByType<PegasusStickController>();var camera=player.GetComponentInChildren<Camera>();
                var aim=enemy.transform.position;var local=camera.transform.InverseTransformDirection(aim-camera.transform.position);
                float yaw=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;float pitch=Mathf.Atan2(local.y,new Vector2(local.x,local.z).magnitude)*Mathf.Rad2Deg;
                var settings=(FirstPersonPlayerSettings)new SerializedObject(player.GetComponent<FirstPersonPlayerMotor>()).FindProperty("settings").objectReferenceValue;
                var mouse=new MouseState{delta=new Vector2(Mathf.Clamp(yaw,-3,3),Mathf.Clamp(pitch,-3,3))/settings.MouseSensitivity};
                float distance=Vector3.Distance(camera.transform.position,aim);var keyboard=distance>1.5f && Mathf.Abs(yaw)<25 && enemy.Health>0?new KeyboardState(Key.W):new KeyboardState();
                if(label.Contains("Pursuit") && player.HitCount>0)keyboard=Time.time-player.LastContactTime<10 && distance<8 && Time.time%1<.8f?new KeyboardState(Key.S):new KeyboardState();
                if(label.Contains("Range") && player.HitCount>0)
                {
                    var filter=new UnityEngine.AI.NavMeshQueryFilter{agentTypeID=new SerializedObject(enemy).FindProperty("navigationAgentType").intValue,areaMask=UnityEngine.AI.NavMesh.AllAreas};
                    var path=new UnityEngine.AI.NavMeshPath();
                    if(UnityEngine.AI.NavMesh.SamplePosition(player.transform.position,out var from,1,filter) && UnityEngine.AI.NavMesh.SamplePosition(new Vector3(55.8f,2.65f,34),out var to,1,filter) && UnityEngine.AI.NavMesh.CalculatePath(from.position,to.position,filter,path) && path.corners.Length>1)
                    {
                        var direction=Vector3.ProjectOnPlane(path.corners[1]-player.transform.position,Vector3.up).normalized;
                        var keys=new System.Collections.Generic.List<Key>{Key.LeftShift};float f=Vector3.Dot(player.transform.forward,direction),r=Vector3.Dot(player.transform.right,direction);
                        if(f>.25f)keys.Add(Key.W);if(f<-.25f)keys.Add(Key.S);if(r>.25f)keys.Add(Key.D);if(r<-.25f)keys.Add(Key.A);keyboard=new KeyboardState(keys.ToArray());
                    }
                }
                if(!label.Contains("Warehouse") && enemy.Health>0 && Time.time>=click && Mathf.Abs(yaw)<4 && Mathf.Abs(pitch)<4)
                {
                    if(label.Contains("Throw"))
                    {keyboard=distance<3.6f?new KeyboardState(Key.S):new KeyboardState();if(distance>3.5f && distance<4.3f){if(player.CurrentPhase==PegasusStickController.Phase.Carry){mouse=mouse.WithButton(MouseButton.Right);click=Time.time+.3f;}else if(player.CurrentPhase==PegasusStickController.Phase.Aim){mouse=mouse.WithButton(MouseButton.Left);click=Time.time+10;}}}
                    else if(distance<2 && (!label.Contains("Pursuit") || player.HitCount==0)){mouse=mouse.WithButton(MouseButton.Left);click=Time.time+2.6f;}
                }
                if(label.Contains("WarehouseWalk") || label.Contains("WarehouseReturn"))WarehouseInput(player,enemy,settings,out keyboard,out mouse);
                InputSystem.QueueStateEvent(Mouse.current,mouse);InputSystem.QueueStateEvent(Keyboard.current,keyboard);
            }
            if(Time.time>=nextCapture){Capture(label+"_"+(frame++).ToString("D4"));nextCapture=Time.time+(label.Contains("Throw") && Time.time-start<10?.1f:.25f);}
        }
        static Vector3 SweepFloor(Vector3 p,EntryActor enemy)
        {
            var hits=Physics.RaycastAll(p+Vector3.up*(enemy.Walking?.1f:-1),Vector3.down,20,~0,QueryTriggerInteraction.Ignore).Where(h=>h.normal.y>.45f && !h.transform.IsChildOf(enemy.transform) && (!acoustic || !h.transform.IsChildOf(acoustic.transform))).OrderBy(h=>h.distance).ToArray();
            var filter=new UnityEngine.AI.NavMeshQueryFilter{agentTypeID=new SerializedObject(enemy.Component).FindProperty("navigationAgentType").intValue,areaMask=UnityEngine.AI.NavMesh.AllAreas};
            var under=Physics.RaycastAll(enemy.transform.position+Vector3.up*.3f,Vector3.down,enemy.FlightHeight+3,~0,QueryTriggerInteraction.Ignore).Where(h=>h.normal.y>.45f && !h.transform.IsChildOf(enemy.transform) && (!acoustic || !h.transform.IsChildOf(acoustic.transform))).OrderBy(h=>h.distance).FirstOrDefault();
            if(under.collider && UnityEngine.AI.NavMesh.SamplePosition(under.point,out var from,1.5f,filter))
                foreach(var hit in hits)
                {
                    var path=new UnityEngine.AI.NavMeshPath();
                    if(UnityEngine.AI.NavMesh.SamplePosition(hit.point,out var to,.4f,filter) && UnityEngine.AI.NavMesh.CalculatePath(from.position,to.position,filter,path) && path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete)return hit.point;
                }
            return hits.Length>0?hits[0].point:p-Vector3.up*2.5f;
        }
        static Vector3 SweepAir(Vector3 p,EntryActor enemy)
        {
            var roof=Physics.RaycastAll(p+Vector3.up*.15f,Vector3.up,enemy.FlightHeight+.5f,~0,QueryTriggerInteraction.Ignore).Where(h=>!h.transform.IsChildOf(enemy.transform) && (!acoustic || !h.transform.IsChildOf(acoustic.transform))).OrderBy(h=>h.distance).FirstOrDefault();
            var air=p+Vector3.up*enemy.FlightHeight;if(roof.collider)air.y=Mathf.Min(air.y,roof.point.y-.42f*enemy.BodySize);return air;
        }
        static void DoorSweep(EntryActor enemy)
        {
            if(sweepDoors==null)
            {
                if(enemy.Doorways.Count==0)return;
                sweepDoors=enemy.Doorways.OrderBy(d=>d.Room.transform.root.name).ThenBy(d=>d.Name).ToArray();
                File.WriteAllText(Output+"/"+label+"_Manifest.txt",string.Join("\n",sweepDoors.Select((d,i)=>$"D{i:D2} {d.Name} inward={d.Inward:F4}"))+"\n");
            }
            if(sweepIndex>=sweepDoors.Length)return;
            var door=sweepDoors[sweepIndex];float depth=Vector3.Dot(enemy.transform.position-door.Point,door.Inward);
            if(enemy.Walking && sweepPhase<=1 && depth>0 && enemy.ActiveEntryName==door.Name)
            {if(acoustic)UnityEngine.Object.Destroy(acoustic);acoustic=null;sweepPhase=2;sweepSince=Time.time;}
            if(sweepPhase==0 && depth<-.6f && depth>-3 && Mathf.Abs(Vector3.Dot(enemy.transform.position-door.Point,Vector3.Cross(Vector3.up,door.Inward)))<.85f && Mathf.Abs(enemy.transform.position.y-door.Point.y)<8)
            {if(acoustic)UnityEngine.Object.Destroy(acoustic);acoustic=null;sweepPhase=1;sweepSince=Time.time;return;}
            if(sweepPhase>=2)
            {
                if(sweepPhase==2 && enemy.LastCompletedEntryName==door.Name && enemy.LastCompletedEntryAdvance>=2){sweepPhase=3;sweepFinished=Time.time;}
                if(sweepPhase!=3 || observedFood!=enemy.CurrentTarget)
                {observedFood=enemy.CurrentTarget;observedConsumption=observedFood?observedFood.AccumulatedConsumptionSeconds:0;}
                if(sweepPhase==3 && enemy.Consuming && enemy.CurrentTarget && enemy.CurrentTarget.Room==door.Room.Room && enemy.CurrentTarget.AccumulatedConsumptionSeconds-observedConsumption>.5f && Time.time-sweepFinished>3)
                {File.AppendAllText(Output+"/"+label+"_Manifest.txt",$"OBSERVED D{sweepIndex:D2} {Time.time:F3} advance={enemy.LastCompletedEntryAdvance:F4} target={enemy.CurrentTarget.name}\n");sweepIndex++;sweepPhase=0;sweepSince=Time.time;}
                sweepStatus=$"Observe depth={depth:F4}, {enemy.ActiveEntryName}";return;
            }
            var end=SweepFloor(door.Point+door.Inward*(sweepPhase==0?-2.2f:enemy.Walking?2.7f:1.15f),enemy);
            if(acoustic && !enemy.Walking){acousticGoal.y=enemy.transform.position.y;acoustic.transform.position=acousticGoal;}
            if(acoustic && Vector3.ProjectOnPlane(enemy.transform.position-acoustic.transform.position,Vector3.up).magnitude<(enemy.Walking?1.4f:.5f))
            {
                UnityEngine.Object.Destroy(acoustic);acoustic=null;
                if(Vector3.ProjectOnPlane(enemy.transform.position-end,Vector3.up).magnitude<(enemy.Walking?1.5f:.85f) && (sweepPhase==0?depth<-.6f:depth>0))
                {sweepPhase++;sweepSince=Time.time;return;}
            }
            if(acoustic && enemy.Walking && Time.time>acousticCheck)
            {
                acousticCheck=Time.time+1;
                object[] args={acoustic.GetComponent<ParvumTarget>(),Vector3.zero,null};
                if(!(bool)typeof(ParvumBrain).GetMethod("CanApproach",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(enemy.Component,args))
                {UnityEngine.Object.DestroyImmediate(acoustic);acoustic=null;}
            }
            if(acoustic){sweepStatus=$"Transit goal={acousticGoal:F3} depth={depth:F3}";return;}
            var filter=new UnityEngine.AI.NavMeshQueryFilter{agentTypeID=new SerializedObject(enemy.Component).FindProperty("navigationAgentType").intValue,areaMask=UnityEngine.AI.NavMesh.AllAreas};
            var path=new UnityEngine.AI.NavMeshPath();var fromPoint=SweepFloor(enemy.transform.position+Vector3.up,enemy);
            bool hasFrom=UnityEngine.AI.NavMesh.SamplePosition(fromPoint,out var from,1.5f,filter),hasTo=UnityEngine.AI.NavMesh.SamplePosition(end,out var to,6f,filter);
            if(!hasFrom || !hasTo || !UnityEngine.AI.NavMesh.CalculatePath(from.position,to.position,filter,path) || path.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete)
            {sweepStatus=$"No complete route from {fromPoint:F3} ({hasFrom} {from.position:F3}) to {end:F3} ({hasTo} {to.position:F3}) status={path.status}; floor probes="+string.Join("; ",Physics.RaycastAll(door.Point+door.Inward*(sweepPhase==0?-2.2f:1.15f),Vector3.down,15,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).Select(h=>$"{h.collider.name} {h.point:F3}"));return;}
            var next=path.corners.Skip(1).FirstOrDefault(p=>Vector3.ProjectOnPlane(p-enemy.transform.position,Vector3.up).magnitude>(enemy.Walking?1.5f:.6f));
            if(next==Vector3.zero)next=end;
            var delta=next-fromPoint;float step=enemy.Walking?2.8f:1.2f;if(delta.magnitude>step)next=fromPoint+delta.normalized*step;
            var origin=next;origin.y=enemy.transform.position.y+.4f;
            var floors=Physics.RaycastAll(origin,Vector3.down,10,~0,QueryTriggerInteraction.Ignore).Where(h=>h.normal.y>.45f && !h.transform.IsChildOf(enemy.transform) && (!acoustic || !h.transform.IsChildOf(acoustic.transform)) && h.collider.name.IndexOf("ceiling",StringComparison.OrdinalIgnoreCase)<0 && h.collider.name.IndexOf("roof",StringComparison.OrdinalIgnoreCase)<0 && h.collider.name.IndexOf("header",StringComparison.OrdinalIgnoreCase)<0).OrderBy(h=>h.distance).ToArray();
            if(floors.Length>0)next=floors[0].point;
            acousticGoal=SweepAir(next,enemy);
            // Keep the temporary audible lure at the flyer's current height; floor/header
            // overlap must not put the review fixture above or below its reachable air lane.
            acousticGoal.y=enemy.Walking?next.y+.3f:enemy.transform.position.y;
            acoustic=GameObject.CreatePrimitive(PrimitiveType.Cube);acoustic.name="Runtime door review acoustic stimulus";acoustic.transform.position=acousticGoal;acoustic.transform.localScale=Vector3.one*.15f;
            if(enemy.Walking)acoustic.transform.localScale=new Vector3(.15f,2,.15f);
            var sound=acoustic.AddComponent<ParvumTarget>();sound.ConfigureSpeaker(null,acoustic.GetComponent<Collider>());sound.SetSpeakerAudible(true);
            if(enemy.Walking)
            {
                // The lure is a review fixture, not an AI goal override. Select an accessible
                // height using the actor's existing approach query; do not relax that query.
                var method=typeof(ParvumBrain).GetMethod("CanApproach",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                foreach(float height in new[]{.3f,.1f,-.2f,-.5f,-.8f})
                {
                    acousticGoal.y=next.y+height;acoustic.transform.position=acousticGoal;Physics.SyncTransforms();
                    object[] args={sound,Vector3.zero,null};if((bool)method.Invoke(enemy.Component,args))break;
                }
            }
            sweepStatus=$"New acoustic goal={acousticGoal:F3}";
        }
        static void WarehouseInput(PegasusStickController player,Component enemy,FirstPersonPlayerSettings settings,out KeyboardState keyboard,out MouseState mouse)
        {
            keyboard=new KeyboardState();mouse=new MouseState();var camera=player.GetComponentInChildren<Camera>();
            Vector3 goal;bool direct=false,hit=false;
            if(label.Contains("WarehouseWalk"))
            {
                if(warehouseStage>=3)return;
                if(warehouseStage==0 && Time.time-warehouseStageTime>8){warehouseStage++;warehouseStageTime=Time.time;}
                goal=warehouseStage==0?new Vector3(61,-4.5f,6):warehouseStage==1?new Vector3(69,-4.5f,8):new Vector3(53.55f,-4.1f,3.29f);
                direct=warehouseStage==0;
                if(warehouseStage>0 && Vector3.ProjectOnPlane(player.transform.position-goal,Vector3.up).magnitude<.5f){warehouseStage++;warehouseStageTime=Time.time;}
                if(warehouseStage>=3)return;
            }
            else if(label.Contains("Short"))
            {
                if(enemy is ParvumBrain walker && warehouseStage==1 && Time.time-warehouseStageTime>2 && walker.Behaviour!=ParvumBehaviour.PursueAttacker && walker.Behaviour!=ParvumBehaviour.Bite)
                {warehouseStage=0;warehouseHitCount=player.HitCount;warehouseStageTime=Time.time;}
                if(label.Contains("OtherRoom") && enemy is FugaBrain flyer && flyer.Health>30 && warehouseStage==1 && Time.time-warehouseStageTime>2 && flyer.Behaviour!=FugaBehaviour.PursueAttacker && flyer.Behaviour!=FugaBehaviour.Attack)
                {warehouseStage=0;warehouseHitCount=player.HitCount;warehouseStageTime=Time.time;}
                if(warehouseStage==0 && player.HitCount>warehouseHitCount){warehouseStage=1;warehouseStageTime=Time.time;}
                if(!label.Contains("OtherRoom") && warehouseStage==1 && player.transform.position.z>17 && enemy.transform.position.z>14){warehouseStage=2;warehouseStageTime=Time.time;}
                goal=warehouseStage==0?enemy.transform.position:warehouseStage==1?(label.Contains("Cross")?new Vector3(69,-4.5f,8):new Vector3(50.3f,-1.5f,18)):new Vector3(60,-4.5f,-2);
                if(label.Contains("OtherRoom") && warehouseStage>0)
                {
                    var roomGoal=label.Contains("Control")?new Vector3(88,2.8f,17.3f):label.Contains("Engine")?new Vector3(35,2.8f,13):label.Contains("Armory")?new Vector3(86,2.8f,-16):label.Contains("Supply")?new Vector3(56,2.7f,-17):new Vector3(56,2.65f,35);
                    if(Vector3.ProjectOnPlane(player.transform.position-roomGoal,Vector3.up).magnitude<1 && Vector3.ProjectOnPlane(enemy.transform.position-roomGoal,Vector3.up).magnitude<3)warehouseStage=2;
                    goal=warehouseStage==1?roomGoal:player.transform.position;
                }
                if(enemy is ParvumBrain && warehouseStage==1 && !warehouseAligned)
                {
                    var waypoint=new Vector3(49.4f,-4.4f,10.14f);
                    warehouseAligned=Vector3.ProjectOnPlane(player.transform.position-waypoint,Vector3.up).magnitude<.5f;
                    if(!warehouseAligned)goal=waypoint;
                }
                hit=warehouseStage==0;
            }
            else
            {
                goal=warehouseStage<2?new Vector3(55.8f,2.65f,34):new Vector3(60,-4.5f,-2);
                if(warehouseStage==0 && Vector3.ProjectOnPlane(player.transform.position-goal,Vector3.up).magnitude<1){warehouseStage=1;warehouseStageTime=Time.time;}
                if(warehouseStage==1){goal=enemy.transform.position;hit=enemy.transform.position.z>26;}
                if(player.HitCount>0 && warehouseStage<2){warehouseStage=2;warehouseStageTime=Time.time;}
            }
            var direction=Vector3.ProjectOnPlane(goal-player.transform.position,Vector3.up);
            if(!direct)
            {
                var filter=new UnityEngine.AI.NavMeshQueryFilter{agentTypeID=new SerializedObject(enemy).FindProperty("navigationAgentType").intValue,areaMask=UnityEngine.AI.NavMesh.AllAreas};
                var path=new UnityEngine.AI.NavMeshPath();
                if(UnityEngine.AI.NavMesh.SamplePosition(player.transform.position,out var from,1,filter) && UnityEngine.AI.NavMesh.SamplePosition(goal,out var to,2,filter) && UnityEngine.AI.NavMesh.CalculatePath(from.position,to.position,filter,path) && path.corners.Length>1)
                {
                    var next=path.corners.Skip(1).FirstOrDefault(p=>Vector3.ProjectOnPlane(p-player.transform.position,Vector3.up).magnitude>.35f);
                    if(next==Vector3.zero)next=goal;
                    direction=Vector3.ProjectOnPlane(next-player.transform.position,Vector3.up);
                    warehousePath=$"status={path.status} goal={goal:F3} from={from.position:F3} next={next:F3} corners="+string.Join(";",path.corners.Select(p=>p.ToString("F3")));
                }
                else warehousePath=$"No ground route to {goal:F3}";
            }
            var aim=label.Contains("WarehouseReturn")?enemy.transform.position:camera.transform.position+direction.normalized*5;
            if(label.Contains("WarehouseReturn") && enemy is ParvumBrain)aim+=Vector3.up*.6f;
            var local=camera.transform.InverseTransformDirection(aim-camera.transform.position);float yaw=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg,pitch=Mathf.Atan2(local.y,new Vector2(local.x,local.z).magnitude)*Mathf.Rad2Deg;
            mouse.delta=new Vector2(Mathf.Clamp(yaw,-3,3),Mathf.Clamp(pitch,-3,3))/settings.MouseSensitivity;
            float distance=Vector3.Distance(camera.transform.position,enemy.transform.position);
            if(enemy is ParvumBrain)distance=Vector3.ProjectOnPlane(player.transform.position-enemy.transform.position,Vector3.up).magnitude;
            bool move=direction.magnitude>.2f && !(hit && distance<1.3f) && !(label.Contains("WarehouseReturn") && warehouseStage>0 && distance>(enemy is ParvumBrain?2:label.Contains("OtherRoom")?1.8f:3));
            if(move){var keys=new System.Collections.Generic.List<Key>();var n=direction.normalized;float f=Vector3.Dot(player.transform.forward,n),r=Vector3.Dot(player.transform.right,n);if(f>.3f)keys.Add(Key.W);if(f<-.3f)keys.Add(Key.S);if(r>.3f)keys.Add(Key.D);if(r<-.3f)keys.Add(Key.A);keyboard=new KeyboardState(keys.ToArray());}
            if(hit && distance<2 && Mathf.Abs(yaw)<4 && Mathf.Abs(pitch)<4 && Time.time>=click){mouse=mouse.WithButton(MouseButton.Left);click=Time.time+2.6f;}
        }
        static void State(string name)
        {
            var enemy=UnityEngine.Object.FindFirstObjectByType<FugaBrain>(FindObjectsInactive.Include);var scene=SceneManager.GetActiveScene();var text=new StringBuilder($"Scene={scene.path} Play={EditorApplication.isPlaying} Dirty={scene.isDirty} Compiling={EditorApplication.isCompiling} Time={Time.time:F3}\n");
            text.AppendLine($"WarehouseReviewStage={warehouseStage}");
            text.AppendLine($"WarehouseReviewPath={warehousePath}");
            text.AppendLine($"DoorSweep index={sweepIndex} phase={sweepPhase} status={sweepStatus}");
            if(enemy){text.AppendLine($"Fuga health={enemy.Health} state={enemy.Behaviour} position={enemy.transform.position:F3} velocity={enemy.GetComponent<Rigidbody>().linearVelocity:F3} target={(enemy.CurrentTarget?enemy.CurrentTarget.name:"null")} motion={enemy.GetComponent<FugaAnimationView>().Motion} entryAdvance={enemy.EntryAdvance} stableAt={enemy.StableAt} deathAt={enemy.DeathAt} diagnostic={enemy.Diagnostic}");}
            if(enemy && enemy.CurrentTarget){var t=enemy.CurrentTarget;text.AppendLine($"Target distance={Vector3.Distance(enemy.transform.position,t.ClosestPoint(enemy.transform.position)):F4} consumption={t.AccumulatedConsumptionSeconds:F3} roomDurability={(t.Ship?t.Ship.CurrentShipState.GetRoom(t.Room).CurrentDurability:-1)}");}
            if(enemy && label!=null && label.Contains("SurfaceDiag") && enemy.Behaviour==FugaBehaviour.Search)
            {
                var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                var foot=(Vector3)typeof(FugaBrain).GetProperty("Foot",flags).GetValue(enemy);
                text.AppendLine($"ProbeFoot={foot:F3}");
                foreach(var food in ParvumTarget.Active.Where(t=>t.IsMetal && t.IsAlive).OrderBy(t=>Vector3.Distance(enemy.transform.position,t.ClosestPoint(enemy.transform.position))).Take(3))
                {
                    var p=enemy.transform.position;var wall=food.ClosestPoint(p);var away=Vector3.ProjectOnPlane(p-wall,Vector3.up).normalized;
                    var goal=wall+away*.26f*enemy.BodySize;goal.y=foot.y;
                    var air=(Vector3)typeof(FugaBrain).GetMethod("FlightPoint",flags).Invoke(enemy,new object[]{goal});
                    var delta=food.ClosestPoint(air)-air;
                    bool front=food.Surface.Raycast(new Ray(air,delta.normalized),out var hit,delta.magnitude+.04f);
                    bool back=food.Surface.Raycast(new Ray(air+delta+delta.normalized*.04f,-delta.normalized),out var reverse,delta.magnitude+.04f);
                    text.AppendLine($"Probe {food.name} distance={Vector3.Distance(p,wall):F3} air={air:F3} wall={food.ClosestPoint(air):F3} front={front}:{hit.point:F3} back={back}:{reverse.point:F3} hasSurface={SeedMetalApproach.HasSurface(food,air,enemy.transform)} clearLeg={SeedMetalApproach.ClearFlightLeg(enemy.GetComponent<SphereCollider>(),p,air,food)}");
                    text.AppendLine("Surface ray blockers="+string.Join(";",Physics.RaycastAll(air,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore).Select(h=>$"{h.collider.name} {h.distance:F3}")));
                    text.AppendLine("Leg blockers="+string.Join(";",Physics.SphereCastAll(p,enemy.GetComponent<SphereCollider>().radius,(air-p).normalized,(air-p).magnitude+.025f,~0,QueryTriggerInteraction.Ignore).Select(h=>$"{h.collider.name} {h.distance:F3}")));
                    object[] pathArgs={goal,null};var hasPath=(bool)typeof(FugaBrain).GetMethod("PathTo",flags).Invoke(enemy,pathArgs);
                    text.AppendLine("Probe ground path="+hasPath+" "+(hasPath?string.Join(";",((Vector3[])pathArgs[1]).Select(v=>v.ToString("F3"))):""));
                }
            }
            if(enemy && enemy.CurrentTarget && label!=null && label.Contains("AirBite"))
            {
                var t=enemy.CurrentTarget;var c=t.Surface;var r=t.GetComponent<Renderer>();
                text.AppendLine($"Food surface={c} enabled={(c && c.enabled)} trigger={(c && c.isTrigger)} root={t.transform.root.name} closest={t.ClosestPoint(enemy.transform.position):F4} colliderBounds={(c?c.bounds:default)} rendererBounds={(r?r.bounds:default)}");
                if(c is MeshCollider mesh)
                {
                    var cached=(Vector3[])typeof(ParvumTarget).GetField("surfaceVertices",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(t);
                    var cachedBounds=new Bounds();if(cached!=null && cached.Length>0){cachedBounds=new Bounds(mesh.transform.TransformPoint(cached[0]),Vector3.zero);foreach(var v in cached)cachedBounds.Encapsulate(mesh.transform.TransformPoint(v));}
                    text.AppendLine($"Food mesh={mesh.sharedMesh.name} convex={mesh.convex} readable={mesh.sharedMesh.isReadable} cached={cached?.Length} current={mesh.sharedMesh.vertexCount} cachedBounds={cachedBounds}");
                }
            }
            if(enemy)
            {
                text.AppendLine($"LastPursuitEnd={enemy.LastPursuitEnd} LastCompletedEntryAdvance={enemy.LastCompletedEntryAdvance:F4}");
                text.AppendLine($"WarehouseEntry={enemy.WarehouseEntryDiagnostic}");
                var active=enemy.GetComponent<FugaAnimationView>().ActiveSlot;var consume=active.GetComponent<FugaConsumeMotionDriver>();
                if(consume)text.AppendLine($"Consume enabled={consume.enabled} body={consume.Body} time={consume.CurrentLoopTime} lip={consume.UpperLipRoot} tilt={consume.CurrentBodyTiltDegrees} offset={consume.CurrentForwardOffsetMeters}");
                var animator=active.GetComponent<Animator>();text.AppendLine($"Animator enabled={animator.enabled} time={(animator.enabled && animator.runtimeAnimatorController?animator.GetCurrentAnimatorStateInfo(0).normalizedTime:0)} surfaceDistance={enemy.SurfaceDistance:F4}");
                if(enemy.Health<=0)foreach(var rb in enemy.GetComponentsInChildren<Rigidbody>())text.AppendLine($"Ragdoll {rb.name} velocity={rb.linearVelocity:F4} angular={rb.angularVelocity:F4}");
            }
            var parvum=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>(FindObjectsInactive.Include);text.AppendLine($"Parvum active={(parvum && parvum.gameObject.activeInHierarchy)}");
            if(parvum && parvum.gameObject.activeInHierarchy)text.AppendLine($"Parvum state={parvum.Behaviour} position={parvum.transform.position:F3} entry={parvum.ActiveEntryName} advance={parvum.EntryAdvance:F4} completed={parvum.LastCompletedEntryName} target={(parvum.CurrentTarget?parvum.CurrentTarget.name:"null")}");
            if(parvum && parvum.gameObject.activeInHierarchy && acoustic)
            {
                object[] args={acoustic.GetComponent<ParvumTarget>(),Vector3.zero,(Action<string>)(s=>text.AppendLine("Acoustic approach: "+s))};
                text.AppendLine("Acoustic reachable="+typeof(ParvumBrain).GetMethod("CanApproach",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(parvum,args));
            }
            var ship=UnityEngine.Object.FindFirstObjectByType<Bellerophon.Core.Ship.ShipDeviceInteractionState>();if(ship)text.AppendLine($"CargoDurability={ship.CurrentCargoState.DurabilityPercent:F4} Speakers={ParvumTarget.Active.Count(t=>t.Audible)}");
            if(ship)text.AppendLine($"ControlRoomDurability={ship.CurrentShipState.GetRoom(Bellerophon.Core.Session.ShipRoomId.ControlRoom).CurrentDurability} ClosedCorridorRule={Bellerophon.Core.Session.IntruderRules.CalculateClosedCorridorCount(ship.CurrentShipState)}");
            foreach(var door in UnityEngine.Object.FindObjectsByType<Bellerophon.Core.Ship.PegasusEntranceDoor>(FindObjectsSortMode.None).OrderBy(d=>d.CorridorIndex).ThenBy(d=>d.name))
                text.AppendLine($"Door {door.name} corridor={door.CorridorIndex} open={door.IsOpen} position={door.transform.position:F3}");
            var player=UnityEngine.Object.FindFirstObjectByType<PegasusStickController>();if(player)text.AppendLine($"Player position={player.transform.position:F3} health={player.GetComponent<FirstPersonPlayerStatus>().CurrentHealth} shield={player.GetComponent<FirstPersonPlayerStatus>().CurrentShield} hits={player.HitCount} phase={player.CurrentPhase}");
            if(player && player.Projectile)text.AppendLine($"Projectile hits={player.Projectile.HitCount} target={player.Projectile.HitTarget} time={player.Projectile.HitTime:F3}");
            var entries=typeof(EditorApplication).Assembly.GetType("UnityEditor.LogEntries");object[] counts={0,0,0};entries.GetMethod("GetCountsByType").Invoke(null,counts);text.AppendLine($"Console errors={counts[0]} warnings={counts[1]} logs={counts[2]}");
            File.WriteAllText(Output+"/State_"+name+".txt",text.ToString());
        }
        static void Capture(string name)
        {
            State(name);Component actor=UnityEngine.Object.FindFirstObjectByType<FugaBrain>();if(!actor)actor=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>();if(!actor)return;
            var obj=new GameObject("Fuga temporary observation camera");var camera=obj.AddComponent<Camera>();
            var centre=actor is FugaBrain?actor.GetComponent<FugaAnimationView>().Surface.bounds.center:actor.GetComponentInChildren<SkinnedMeshRenderer>().bounds.center;
            var offsets=new[]{-actor.transform.forward*.15f+actor.transform.right*1.5f,-actor.transform.forward*.15f-actor.transform.right*1.5f,-actor.transform.forward*1.6f};
            if(actor is ParvumBrain)offsets=offsets.Select(v=>v*2.2f).ToArray();
            var offset=offsets.OrderBy(v=>Physics.RaycastAll(centre,v+Vector3.up*.2f,(v+Vector3.up*.2f).magnitude,~0,QueryTriggerInteraction.Ignore).Count(h=>!h.transform.IsChildOf(actor.transform))).First();
            camera.transform.position=centre+offset+Vector3.up*.2f;camera.transform.LookAt(centre);
            camera.nearClipPlane=.03f;camera.farClipPlane=200;camera.fieldOfView=55;
            if(name.Contains("Throw") || name.Contains("WarehouseWalk") || name.Contains("WarehouseReturn")){var playerCamera=UnityEngine.Object.FindFirstObjectByType<PegasusStickController>().GetComponentInChildren<Camera>();camera.CopyFrom(playerCamera);camera.transform.SetPositionAndRotation(playerCamera.transform.position,playerCamera.transform.rotation);}
            var target=new RenderTexture(960,720,24);var texture=new Texture2D(960,720,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,960,720),0,0);texture.Apply();File.WriteAllBytes(Output+"/"+name+".png",texture.EncodeToPNG());}
            finally{RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(obj);}
        }
        static void Inspect()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode required for source inspection.");
            var active=SceneManager.GetActiveScene();
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/CargoRunMvp.unity",OpenSceneMode.Additive);
            try
            {
                var root=scene.GetRootGameObjects().Single(x=>x.name=="Approved Fuga Enemy Placement");
                var text=new StringBuilder();
                foreach(Transform slot in root.transform)
                {
                    text.AppendLine($"SLOT {slot.name} position={slot.position:F3} scale={slot.lossyScale:F3}");
                    foreach(var component in slot.GetComponentsInChildren<Component>(true))
                    {
                        if(component is Transform)continue;
                        text.AppendLine($" {AnimationUtility.CalculateTransformPath(component.transform,slot)} : {component.GetType().Name}");
                        if(component is Renderer renderer)text.AppendLine($" bounds={renderer.bounds} mesh={(renderer is SkinnedMeshRenderer sk?AssetDatabase.GetAssetPath(sk.sharedMesh):"")}");
                        if(component is Animator animator && animator.runtimeAnimatorController)
                        {
                            foreach(var clip in animator.runtimeAnimatorController.animationClips.Distinct())
                            {text.AppendLine($" CLIP {AssetDatabase.GetAssetPath(clip)} length={clip.length}");foreach(var binding in AnimationUtility.GetCurveBindings(clip))text.AppendLine($"  {binding.path} {binding.propertyName}");}
                        }
                    }
                }
                File.WriteAllText(Output+"/Source.txt",text.ToString());
            }
            finally {EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(active);}
        }
    }
}
