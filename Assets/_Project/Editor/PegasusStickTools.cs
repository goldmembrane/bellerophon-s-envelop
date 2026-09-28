using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEditor.Animations;
using Bellerophon.Core.Player;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Bellerophon.Enemies.Parvum;

namespace Bellerophon.Editor
{
    [InitializeOnLoad]
    internal static class PegasusStickTools
    {
        static PegasusStickTools()
        {
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state==PlayModeStateChange.ExitingPlayMode){ReleaseReviewDevices();return;}
                if(state!=PlayModeStateChange.EnteredPlayMode || !File.Exists(Output+"/Action.json"))return;
                var request=JsonUtility.FromJson<Request>(File.ReadAllText(Output+"/Action.json"));
                if(request.mode=="PlayDrive")StartDrive(request.label);
                else if(request.mode=="PlayAudit")StartAudit(request.label);
            };
            AssemblyReloadEvents.beforeAssemblyReload+=ReleaseReviewDevices;
        }
        static Mouse reviewMouse,originalMouse;
        static Keyboard reviewKeyboard,originalKeyboard;
        static InputSettings originalInputSettings,reviewInputSettings;
        static void AcquireReviewDevices()
        {
            if(reviewMouse!=null)return;
            originalMouse=Mouse.current;originalKeyboard=Keyboard.current;
            // Unsaved observation-only routing; gameplay action locks remain in the player code.
            originalInputSettings=InputSystem.settings;
            reviewInputSettings=UnityEngine.Object.Instantiate(originalInputSettings);
            reviewInputSettings.hideFlags=HideFlags.HideAndDontSave;
            reviewInputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            reviewInputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings=reviewInputSettings;
            InputSystem.RegisterLayout("{\"name\":\"StickReviewMouse\",\"extend\":\"Mouse\",\"canRunInBackground\":true}");
            InputSystem.RegisterLayout("{\"name\":\"StickReviewKeyboard\",\"extend\":\"Keyboard\",\"canRunInBackground\":true}");
            reviewMouse=(Mouse)InputSystem.AddDevice("StickReviewMouse");reviewKeyboard=(Keyboard)InputSystem.AddDevice("StickReviewKeyboard");
            reviewMouse.MakeCurrent();reviewKeyboard.MakeCurrent();
        }
        static void ReleaseReviewDevices()
        {
            if(reviewMouse!=null){InputSystem.RemoveDevice(reviewMouse);reviewMouse=null;}
            if(reviewKeyboard!=null){InputSystem.RemoveDevice(reviewKeyboard);reviewKeyboard=null;}
            if(originalMouse!=null && originalMouse.added)originalMouse.MakeCurrent();
            if(originalKeyboard!=null && originalKeyboard.added)originalKeyboard.MakeCurrent();
            if(originalInputSettings){InputSystem.settings=originalInputSettings;originalInputSettings=null;}
            if(reviewInputSettings){UnityEngine.Object.DestroyImmediate(reviewInputSettings);reviewInputSettings=null;}
        }
        static void EnableReviewDevices()
        {
            if(reviewMouse!=null){if(!reviewMouse.enabled)InputSystem.EnableDevice(reviewMouse);reviewMouse.MakeCurrent();}
            if(reviewKeyboard!=null){if(!reviewKeyboard.enabled)InputSystem.EnableDevice(reviewKeyboard);reviewKeyboard.MakeCurrent();}
        }
        internal const string Output = "docs/validation/PegasusStick";
        const string Source = "Assets/_Project/Scenes/CargoRunMvp.unity";
        static readonly string[] Names = { "Stick_Carry", "Stick_Grip_TwoHand", "Stick_Attack_Forward", "Stick_Grip_OneHand", "Stick_Throw_Ready", "Stick_Throw_Release", "Stick_Throw_Cancel" };
        [Serializable] class Request { public string mode; public string label; public float yaw,pitch,seconds; public string key; }
        internal static void Review()
        {
            Directory.CreateDirectory(Output);
            var request = JsonUtility.FromJson<Request>(File.ReadAllText(Output + "/Action.json"));
            if(request.mode == "Inspect") Inspect();
            else if(request.mode == "Apply") Apply();
            else if(request.mode == "Play" || request.mode=="PlayDrive" || request.mode=="PlayAudit") { EditorApplication.isPlaying=true; }
            else if(request.mode == "Stop") { EditorApplication.isPlaying=false; }
            else if(request.mode == "Capture") Capture(request.label);
            else if(request.mode == "State") State(request.label);
            else if(request.mode == "ConsoleBaseline")
            {State(request.label);typeof(EditorApplication).Assembly.GetType("UnityEditor.LogEntries").GetMethod("Clear").Invoke(null,null);}
            else if(request.mode == "Observe") StartObservation(request.label);
            else if(request.mode == "Audit") StartAudit(request.label);
            else if(request.mode == "Drive") StartDrive(request.label);
            else if(request.mode == "ConnectPrompt") ConnectPrompt();
            else if(request.mode == "Input") StartInput(request);
            else if(request.mode == "SourceViews") SourceViews();
            else if(request.mode == "NearHands")
            {
                if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit only.");
                var player=SceneManager.GetActiveScene().GetRootGameObjects().Single(x=>x.name=="Player");
                var camera=player.GetComponentInChildren<Camera>();camera.nearClipPlane=.03f;
                var visual=player.GetComponentInChildren<Animator>().transform;visual.localPosition=new Vector3(0,-1.62f,.2f);PrefabUtility.RecordPrefabInstancePropertyModifications(visual);
                PrefabUtility.RecordPrefabInstancePropertyModifications(camera);EditorSceneManager.MarkSceneDirty(player.scene);EditorSceneManager.SaveScene(player.scene);
            }
            else if(request.mode == "FirstPersonArms")
            {
                if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit only.");
                var player=SceneManager.GetActiveScene().GetRootGameObjects().Single(x=>x.name=="Player");
                var skin=player.GetComponentInChildren<SkinnedMeshRenderer>();
                var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/PegasusStick/PegasusStickVisual.prefab").GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;var mesh=UnityEngine.Object.Instantiate(original);mesh.name="Pegasus original arms view";
                var armBones=skin.bones.Select(b=>b.name.Contains("Arm") || b.name.Contains("Hand") || b.name.Contains("Thumb") || b.name.Contains("Index") || b.name.Contains("Middle") || b.name.Contains("Ring") || b.name.Contains("Little")).ToArray();
                var weights=original.boneWeights;
                bool InArm(int v){var w=weights[v];float sum=(armBones[w.boneIndex0]?w.weight0:0)+(armBones[w.boneIndex1]?w.weight1:0)+(armBones[w.boneIndex2]?w.weight2:0)+(armBones[w.boneIndex3]?w.weight3:0);return sum>.5f;}
                for(int sub=0;sub<mesh.subMeshCount;sub++)
                {var triangles=original.GetTriangles(sub);var kept=new System.Collections.Generic.List<int>();for(int i=0;i<triangles.Length;i+=3)if(InArm(triangles[i]) && InArm(triangles[i+1]) && InArm(triangles[i+2])){kept.Add(triangles[i]);kept.Add(triangles[i+1]);kept.Add(triangles[i+2]);}mesh.SetTriangles(kept,sub);}
                const string meshPath="Assets/_Project/Prefabs/Player/PegasusStick/FirstPersonArms.asset";
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);}else AssetDatabase.CreateAsset(mesh,meshPath);
                skin.sharedMesh=mesh;skin.updateWhenOffscreen=true;
                PrefabUtility.RecordPrefabInstancePropertyModifications(skin);
                AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(player.scene);EditorSceneManager.SaveScene(player.scene);
                File.WriteAllText(Output+"/ArmsView.txt",$"Original source unchanged: {AssetDatabase.GetAssetPath(original)}. Positions, weights, UVs, normals, bind poses unchanged. Owner view uses original arm triangles only; triangles {original.triangles.Length/3} -> {mesh.triangles.Length/3}.");
            }
            else if(request.mode == "AlignVisual")
            {
                if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit only.");
                var player=SceneManager.GetActiveScene().GetRootGameObjects().Single(x=>x.name=="Player");
                var visual=player.GetComponentInChildren<Animator>().transform;visual.localRotation=Quaternion.identity;visual.localPosition=new Vector3(0,-1.62f,.2f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(visual);
                EditorSceneManager.MarkSceneDirty(player.scene);EditorSceneManager.SaveScene(player.scene);
            }
            else throw new InvalidOperationException("Unknown stick operation: " + request.mode);
        }
        static void SourceViews()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit only.");
            var active=SceneManager.GetActiveScene();var source=EditorSceneManager.OpenScene(Source,OpenSceneMode.Additive);
            try
            {
                var all=source.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToArray();
                foreach(var name in new[]{"Stick_Carry","Stick_Attack_Forward","Stick_Throw_Ready"})
                {
                    var root=all.Single(x=>x.name==name);var go=new GameObject("Temporary original stick view");var camera=go.AddComponent<Camera>();camera.enabled=false;camera.nearClipPlane=.03f;
                    try{camera.transform.position=root.TransformPoint(new Vector3(.9f,1.35f,2.3f));camera.transform.LookAt(root.position+Vector3.up*1.1f);Render(camera,"Source_"+name);}
                    finally{UnityEngine.Object.DestroyImmediate(go);}
                }
            }
            finally{EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(active);}
        }
        static void ConnectPrompt()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying || scene.name!="Pegasus" || scene.isDirty)throw new InvalidOperationException("Clean Pegasus edit scene required.");
            var player=scene.GetRootGameObjects().Single(x=>x.name=="Player");
            if(player.transform.Find("Stick Interaction HUD"))return;
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/Hud.prefab");
            var original=source.GetComponentsInChildren<UnityEngine.UI.Text>(true).Single(x=>x.name=="Interaction Prompt Text");
            var root=new GameObject("Stick Interaction HUD",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));root.transform.SetParent(player.transform,false);
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=player.GetComponentInChildren<Camera>();canvas.planeDistance=.1f;canvas.sortingOrder=100;
            var scaler=root.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(960,720);
            var label=UnityEngine.Object.Instantiate(original,root.transform,false);label.name=original.name;label.raycastTarget=false;
            var hud=root.AddComponent<FirstPersonHud>();hud.Configure(null,null,null,player.GetComponent<FirstPersonInteractionController>(),label);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        static float observationStart,nextFrame;
        static string auditLabel;
        static float auditStart,auditCapture;
        static int auditStep,auditFrame,auditTick;
        static bool auditRelease;
        static void StartAudit(string label)
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play required.");
            auditLabel=label;auditStart=Time.time;auditCapture=0;auditStep=0;auditFrame=0;auditTick=-1;auditRelease=false;
            AcquireReviewDevices();
            InputSystem.onActionChange-=AuditAction;InputSystem.onActionChange+=AuditAction;
            EditorWindow.GetWindow(typeof(EditorApplication).Assembly.GetType("UnityEditor.GameView")).Focus();
            InputSystem.onBeforeUpdate-=AuditTick;InputSystem.onBeforeUpdate+=AuditTick;
        }
        static void AuditTick()
        {
            if(!EditorApplication.isPlaying){InputSystem.onBeforeUpdate-=AuditTick;InputSystem.onActionChange-=AuditAction;return;}
            if(InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
            EnableReviewDevices();
            if(auditTick==Time.frameCount)return;auditTick=Time.frameCount;
            if(auditRelease){InputSystem.QueueStateEvent(Mouse.current,new MouseState());InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());auditRelease=false;}
            float t=Time.time-auditStart;
            float[] times={.2f,.201f};string[] actions={"R","L"};
            if(auditLabel.Contains("Attack")){times=new[]{.2f,2.45f,2.5f};actions=new[]{"L","R","L"};}
            if(auditLabel.Contains("Cancel")){times=new[]{.2f,.5f,.8f,.85f};actions=new[]{"R","R","R","L"};}
            if(auditLabel.Contains("Escape")){times=new[]{.2f,4.2f,4.5f};actions=new[]{"R","Escape","L"};}
            if(auditLabel.Contains("EscapeResume")){times=new[]{.2f,4.2f,4.5f,5.2f};actions=new[]{"R","Escape","L","L"};}
            if(auditLabel.Contains("Together")){times=new[]{.2f};actions=new[]{"RL"};}
            if(auditLabel.Contains("CancelOnly")){times=new[]{.2f,.35f,.5f};actions=new[]{"R","L","R"};}
            if(auditLabel.Contains("WallMiss"))
            {
                times=new[]{2f};actions=new[]{"L"};
                if(t<1f)
                {
                    var motor=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
                    var settings=(FirstPersonPlayerSettings)new SerializedObject(motor).FindProperty("settings").objectReferenceValue;
                    InputSystem.QueueStateEvent(Mouse.current,new MouseState{delta=new Vector2(30f*Time.deltaTime/settings.MouseSensitivity,0)});
                }
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
            }
            // Same-frame events preserve actual input event order; no controller methods invoked.
            while(auditStep<times.Length && t>=times[auditStep])
            {
                string action=actions[auditStep++];
                File.AppendAllText(Output+"/Input_"+auditLabel+".txt",$"{Time.time:F4} {action}\n");
                if(action=="Escape")InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.Escape));
                else InputSystem.QueueStateEvent(Mouse.current,action=="RL"?new MouseState().WithButton(MouseButton.Right).WithButton(MouseButton.Left):new MouseState().WithButton(action=="R"?MouseButton.Right:MouseButton.Left));
                auditRelease=true;
            }
            if(t>=auditCapture){Capture(auditLabel+"_"+(auditFrame++).ToString("D4"));auditCapture=t+.16f;}
            if(t>10){InputSystem.onBeforeUpdate-=AuditTick;InputSystem.onActionChange-=AuditAction;State(auditLabel+"End");ReleaseReviewDevices();}
        }
        static void AuditAction(object value,InputActionChange change)
        {
            if(change!=InputActionChange.ActionPerformed || !(value is InputAction action) || (action.name!="Use" && action.name!="Aim"))return;
            var player=UnityEngine.Object.FindFirstObjectByType<PegasusStickController>();
            File.AppendAllText(Output+"/Input_"+auditLabel+".txt",$"{Time.time:F4} performed={action.name} phase={player.CurrentPhase} suppressed={player.GetComponent<FirstPersonPlayerInput>().GameplayActionInputSuppressed}\n");
        }
        static Request inputRequest;
        static float inputStart,inputLast;
        static int inputFrame;
        static void StartInput(Request request)
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play required.");
            inputRequest=request;inputStart=Time.time;inputLast=Time.time;inputFrame=-1;
            EditorWindow.GetWindow(typeof(EditorApplication).Assembly.GetType("UnityEditor.GameView")).Focus();
            EditorApplication.update-=InputTick;EditorApplication.update+=InputTick;
        }
        static void InputTick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=InputTick;return;}
            if(inputFrame==Time.frameCount)return;inputFrame=Time.frameCount;
            float elapsed=Time.time-inputStart;
            if(elapsed>=inputRequest.seconds){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());InputSystem.QueueStateEvent(Mouse.current,new MouseState());EditorApplication.update-=InputTick;Capture(inputRequest.label);return;}
            var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();var settings=(FirstPersonPlayerSettings)new SerializedObject(player).FindProperty("settings").objectReferenceValue;
            float fraction=(Time.time-inputLast)/Mathf.Max(.01f,inputRequest.seconds);inputLast=Time.time;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{delta=new Vector2(inputRequest.yaw,inputRequest.pitch)*fraction/settings.MouseSensitivity});
            InputSystem.QueueStateEvent(Keyboard.current,string.IsNullOrEmpty(inputRequest.key)?new KeyboardState():new KeyboardState((Key)Enum.Parse(typeof(Key),inputRequest.key)));
        }
        static int observationStep,frame;
        static string observationLabel;
        static bool buttonRelease;
        static int lastObservationFrame=-1;
        static string driveLabel;
        static float driveStart,driveClick,driveCapture;
        static int driveFrame,lastDriveFrame;
        static readonly System.Collections.Generic.List<Vector3> driveTrail=new System.Collections.Generic.List<Vector3>();
        static Vector3 sightRetreat;
        static Vector3[] sightRoute;
        static int sightCorner;
        static RaycastHit GroundUnder(Transform actor)
        {
            return Physics.RaycastAll(actor.position+Vector3.up*.3f,Vector3.down,1.5f,~0,QueryTriggerInteraction.Ignore)
                .Where(h=>!h.transform.IsChildOf(actor)).OrderBy(h=>h.distance).FirstOrDefault();
        }
        static bool CorridorAttackPosition(ParvumBrain enemy,PegasusStickController player)
        {
            var floor=GroundUnder(enemy.transform);
            if(!floor.collider)return false;
            bool slope=floor.transform.root.name.Contains("Corridor") && floor.collider.name.Contains("Body18m");
            bool flat=floor.collider.name.StartsWith("SC-H") && floor.collider.name.Contains("floor");
            if(driveLabel.Contains("Flat"))slope=false;
            return (slope || flat) && Vector3.Dot(enemy.transform.forward,(player.transform.position-enemy.transform.position).normalized)<-.5f;
        }
        static void StartDrive(string label)
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play required.");
            driveLabel=label;driveStart=Time.time;driveClick=0;driveCapture=0;driveFrame=0;lastDriveFrame=-1;
            driveTrail.Clear();
            sightRetreat=Vector3.zero;
            sightRoute=null;sightCorner=0;
            AcquireReviewDevices();
            EditorWindow.GetWindow(typeof(EditorApplication).Assembly.GetType("UnityEditor.GameView")).Focus();
            InputSystem.onBeforeUpdate-=DriveTick;InputSystem.onBeforeUpdate+=DriveTick;
        }
        static void DriveTick()
        {
            if(!EditorApplication.isPlaying){InputSystem.onBeforeUpdate-=DriveTick;return;}
            if(InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
            EnableReviewDevices();
            if(lastDriveFrame==Time.frameCount)return;lastDriveFrame=Time.frameCount;
            var player=UnityEngine.Object.FindFirstObjectByType<PegasusStickController>();
            var camera=player.GetComponentInChildren<Camera>();var enemy=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>();
            bool recovery=driveLabel.StartsWith("Recovery");
            bool throwing=driveLabel.StartsWith("ThrowCombat");
            bool movingCombat=driveLabel.StartsWith("MovingCombat");
            bool follow=movingCombat && driveLabel.Contains("Follow");
            bool pursuit=driveLabel.Contains("Pursuit");
            var target=recovery ? (player.Projectile?player.Projectile.GetComponent<Collider>():null) : (enemy?enemy.GetComponent<Collider>():null);
            if((!target && !(enemy && enemy.Health<=0 && !recovery)) || Time.time-driveStart>(follow?115:20) || (recovery && player.HasStick) || (movingCombat && player.HitCount>0 && Time.time-player.LastContactTime>(pursuit?22:8)))
            {InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());InputSystem.QueueStateEvent(Mouse.current,new MouseState());InputSystem.onBeforeUpdate-=DriveTick;Capture(driveLabel+"End");ReleaseReviewDevices();return;}
            var aim=target?target.bounds.center:enemy.transform.position+Vector3.up*.4f;
            if(movingCombat && driveLabel.Contains("High"))aim+=Vector3.up*(driveLabel.Contains("Ceiling")?1f:.5f);
            if(movingCombat && driveLabel.Contains("Side"))aim+=camera.transform.right*(driveLabel.Contains("Edge")?.55f:.35f);
            if(throwing && enemy && enemy.Health>0)aim+=Vector3.up*.3f;
            if(recovery && driveLabel.Contains("Offset"))aim+=camera.transform.right*.25f;
            var local=camera.transform.InverseTransformDirection(aim-camera.transform.position);
            float yaw=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;
            float pitch=Mathf.Atan2(local.y,new Vector2(local.x,local.z).magnitude)*Mathf.Rad2Deg;
            var settings=(FirstPersonPlayerSettings)new SerializedObject(player.GetComponent<FirstPersonPlayerMotor>()).FindProperty("settings").objectReferenceValue;
            bool attacking=!recovery && !throwing && !movingCombat && player.CurrentPhase!=PegasusStickController.Phase.Carry;
            var mouse=new MouseState{delta=attacking?Vector2.zero:new Vector2(Mathf.Clamp(yaw,-3,3),Mathf.Clamp(pitch,-3,3))/settings.MouseSensitivity};
            float distance=Vector3.Distance(camera.transform.position,aim);
            bool moving=!attacking && (!throwing || player.CurrentPhase==PegasusStickController.Phase.Carry) && distance>(recovery?2.2f:throwing?3.8f:1.8f) && Mathf.Abs(yaw)<25 && (!enemy || enemy.Health>0);
            var keyboard=moving?new KeyboardState(Key.W):new KeyboardState();
            if(follow && enemy && player.HitCount==0)
            {
                if(driveTrail.Count==0 || Vector3.Distance(driveTrail[driveTrail.Count-1],enemy.transform.position)>.2f)driveTrail.Add(enemy.transform.position);
                while(driveTrail.Count>1 && Vector3.ProjectOnPlane(driveTrail[0]-player.transform.position,Vector3.up).magnitude<.65f)driveTrail.RemoveAt(0);
                var delta=driveTrail[0]-player.transform.position;delta.y=0;
                var localMove=player.transform.InverseTransformDirection(delta.normalized);var keys=new System.Collections.Generic.List<Key>();
                if(Vector3.ProjectOnPlane(enemy.transform.position-player.transform.position,Vector3.up).magnitude>1.05f)
                {if(localMove.z>.3f)keys.Add(Key.W);if(localMove.z<-.3f)keys.Add(Key.S);if(localMove.x>.3f)keys.Add(Key.D);if(localMove.x<-.3f)keys.Add(Key.A);}
                keyboard=new KeyboardState(keys.ToArray());
            }
            if(movingCombat && player.HitCount>0)keyboard=new KeyboardState();
            // Actual input-driven retreat: no enemy state or actor transform changes.
            if(pursuit && player.HitCount>0 && Time.time-player.LastContactTime<14 && distance<7 && Time.time%1f<.65f)
                keyboard=new KeyboardState(Key.S);
            if(pursuit && driveLabel.Contains("Loss") && player.HitCount>0 && Time.time-player.LastContactTime<14)
                keyboard=new KeyboardState(Key.S);
            if(pursuit && driveLabel.Contains("Sight") && player.HitCount>0)
            {
                if(sightRetreat==Vector3.zero)sightRetreat=-player.transform.right;
                keyboard=new KeyboardState();
                if(Time.time-player.LastContactTime<2.5f)keyboard=new KeyboardState(Key.S);
                else if(Time.time-player.LastContactTime<8)
                {
                    var movement=player.transform.InverseTransformDirection(sightRetreat);
                    var keys=new System.Collections.Generic.List<Key>();
                    if(movement.z>.3f)keys.Add(Key.W);if(movement.z<-.3f)keys.Add(Key.S);
                    if(movement.x>.3f)keys.Add(Key.D);if(movement.x<-.3f)keys.Add(Key.A);
                    keyboard=new KeyboardState(keys.ToArray());
                }
            }
            if(pursuit && driveLabel.Contains("Occlusion") && player.HitCount>0 && Time.time-player.LastContactTime>=3)
            {
                keyboard=new KeyboardState();
                if(sightRoute==null && UnityEngine.AI.NavMesh.SamplePosition(player.transform.position,out var start,1,UnityEngine.AI.NavMesh.AllAreas))
                {
                    float best=float.PositiveInfinity;
                    var route=new UnityEngine.AI.NavMeshPath();
                    for(int i=0;i<48;i++)
                    {
                        float angle=i*Mathf.PI/12;float radius=i<24?4:7;
                        var probe=player.transform.position+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;
                        if(!UnityEngine.AI.NavMesh.SamplePosition(probe,out var endpoint,1,UnityEngine.AI.NavMesh.AllAreas) || Mathf.Abs(endpoint.position.y-start.position.y)>2)continue;
                        var eye=enemy.GetComponent<Collider>().bounds.center;
                        var offset=endpoint.position+Vector3.up-eye;
                        if(offset.magnitude>9 || !Physics.RaycastAll(eye,offset.normalized,offset.magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.transform.IsChildOf(enemy.transform) && !h.transform.IsChildOf(player.transform)))continue;
                        if(!UnityEngine.AI.NavMesh.CalculatePath(start.position,endpoint.position,UnityEngine.AI.NavMesh.AllAreas,route) || route.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete)continue;
                        float length=0;for(int j=1;j<route.corners.Length;j++)length+=Vector3.Distance(route.corners[j-1],route.corners[j]);
                        if(length<best){best=length;sightRoute=route.corners;sightCorner=sightRoute.Length>1?1:0;}
                    }
                }
                if(sightRoute!=null && sightCorner<sightRoute.Length)
                {
                    var offset=Vector3.ProjectOnPlane(sightRoute[sightCorner]-player.transform.position,Vector3.up);
                    if(offset.magnitude<.35f)sightCorner++;
                    else
                    {
                        var movement=player.transform.InverseTransformDirection(offset.normalized);
                        var keys=new System.Collections.Generic.List<Key>();
                        if(movement.z>.3f)keys.Add(Key.W);if(movement.z<-.3f)keys.Add(Key.S);
                        if(movement.x>.3f)keys.Add(Key.D);if(movement.x<-.3f)keys.Add(Key.A);
                        keyboard=new KeyboardState(keys.ToArray());
                    }
                }
            }
            if(pursuit && driveLabel.Contains("Occlusion") && player.HitCount>0 && Time.time-player.LastContactTime<3)keyboard=new KeyboardState(Key.S);
            if(driveLabel.StartsWith("DeathCross") && enemy && enemy.Health<=0 && Time.time-enemy.DeathStartedAt<1f)
            {keyboard=new KeyboardState(Key.W);mouse.delta=Vector2.zero;}
            if(throwing && player.CurrentPhase==PegasusStickController.Phase.Carry && distance<3.5f)keyboard=new KeyboardState(Key.S);
            if(recovery && player.GetComponent<FirstPersonInteractionController>().HasCurrentTarget && Time.time>=driveClick){keyboard=new KeyboardState(Key.F);driveClick=Time.time+.25f;}
            if(throwing && enemy && enemy.Health>0 && (player.ThrowMode || (distance>=3.5f && distance<4.2f)) && Mathf.Abs(yaw)<4 && Mathf.Abs(pitch)<4 && Time.time>=driveClick)
            {
                if(player.CurrentPhase==PegasusStickController.Phase.Carry){mouse=mouse.WithButton(MouseButton.Right);driveClick=Time.time+.3f;}
                else if(player.CurrentPhase==PegasusStickController.Phase.Aim){mouse=mouse.WithButton(MouseButton.Left);driveClick=Time.time+10;}
            }
            else if(!throwing && !recovery && enemy && enemy.Health>0 && (!driveLabel.Contains("Corridor") || CorridorAttackPosition(enemy,player)) && (!follow || enemy.Behaviour==ParvumBehaviour.RelocateRoom || enemy.Behaviour==ParvumBehaviour.AdvanceIntoRoom) && (!movingCombat || (player.HitCount==0 && enemy.GetComponent<Rigidbody>().linearVelocity.sqrMagnitude>.25f)) && distance<2.3f && Mathf.Abs(yaw)<4 && Mathf.Abs(pitch)<4 && Time.time>=driveClick){mouse=mouse.WithButton(MouseButton.Left);driveClick=Time.time+2.6f;}
            InputSystem.QueueStateEvent(Mouse.current,mouse);InputSystem.QueueStateEvent(Keyboard.current,keyboard);
            if(movingCombat && enemy && enemy.Health>0 && player.CurrentPhase==PegasusStickController.Phase.Strike)AuditSurface(player,enemy);
            if(Time.time>=driveCapture){Capture(driveLabel+"_"+(driveFrame++).ToString("D4"));driveCapture=Time.time+(player.CurrentPhase==PegasusStickController.Phase.Strike && player.PhaseTime>1.35f && player.PhaseTime<1.6f?.016f:.16f);}
        }
        static void AuditSurface(PegasusStickController player,ParvumBrain enemy)
        {
            var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            object[] segment={null,null,0f};typeof(PegasusStickController).GetMethod("StickSegment",flags).Invoke(player,segment);
            var a=(Vector3)segment[0];var b=(Vector3)segment[1];float radius=(float)segment[2];
            var skin=enemy.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();
            try
            {
                skin.BakeMesh(mesh);var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]=skin.transform.TransformPoint(vertices[i]);
                object[] args={a,b,radius,vertices,mesh.triangles,Vector3.zero};
                var contact=typeof(PegasusStickController).Assembly.GetType("Bellerophon.Core.Player.StickSurfaceContact").GetMethod("Touches");
                bool touches=(bool)contact.Invoke(null,args);
                bool broad=Physics.OverlapCapsule(a,b,radius,~0,QueryTriggerInteraction.Ignore).Any(c=>c.GetComponentInParent<ParvumBrain>()==enemy);
                File.AppendAllText(Output+"/Surface_"+driveLabel+".txt",$"{Time.time:F4} phase={player.PhaseTime:F4} window={player.ContactWindow} visible={touches} broad={broad} speed={enemy.GetComponent<Rigidbody>().linearVelocity.magnitude:F3} health={enemy.Health} last={player.LastHit} a={a:F3} b={b:F3} radius={radius:F4}\n");
            }
            finally{UnityEngine.Object.DestroyImmediate(mesh);}
        }
        static void StartObservation(string label)
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play required.");
            observationStart=Time.time;nextFrame=0;observationStep=0;frame=0;observationLabel=label;
            lastObservationFrame=-1;buttonRelease=false;
            EditorWindow.GetWindow(typeof(EditorApplication).Assembly.GetType("UnityEditor.GameView")).Focus();
            EditorApplication.update-=ObserveTick;EditorApplication.update+=ObserveTick;
        }
        static void ObserveTick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=ObserveTick;return;}
            if(lastObservationFrame==Time.frameCount)return;lastObservationFrame=Time.frameCount;
            if(buttonRelease){InputSystem.QueueStateEvent(Mouse.current,new MouseState());InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());buttonRelease=false;}
            float t=Time.time-observationStart;
            float[] times={1,4,8,11,15};MouseButton[] buttons={MouseButton.Left,MouseButton.Right,MouseButton.Right,MouseButton.Right,MouseButton.Left};
            if(observationLabel.StartsWith("Quick")){times=observationLabel.Contains("Cancel")?new[]{.2f,.35f,.5f}:new[]{.2f,.35f};buttons=observationLabel.Contains("Cancel")?new[]{MouseButton.Right,MouseButton.Left,MouseButton.Right}:new[]{MouseButton.Right,MouseButton.Left};}
            if(observationLabel.StartsWith("RecoverRetry")){times=new[]{.2f,.35f,5.1f,5.4f,5.55f};buttons=new[]{MouseButton.Right,MouseButton.Left,MouseButton.Middle,MouseButton.Right,MouseButton.Left};}
            if(observationLabel.StartsWith("AimLate")){times=new[]{.2f,5.2f};buttons=new[]{MouseButton.Right,MouseButton.Left};}
            if(observationStep<times.Length && t>=times[observationStep])
            {if(buttons[observationStep]==MouseButton.Middle)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.F));else InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(buttons[observationStep]));buttonRelease=true;observationStep++;}
            if(t>=nextFrame)
            {Capture(observationLabel+"_"+(frame++).ToString("D4"));nextFrame=t+.16f;}
            if(t>(observationLabel.StartsWith("Quick")?8:19)){EditorApplication.update-=ObserveTick;State(observationLabel+"End");}
        }
        static void Apply()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying || scene.name!="Pegasus" || scene.isDirty)throw new InvalidOperationException("Clean Pegasus edit scene required.");
            var player=scene.GetRootGameObjects().Single(x=>x.name=="Player");
            if(player.GetComponent<PegasusStickController>())throw new InvalidOperationException("Stick already applied; inspect existing wiring.");
            const string destination="Assets/_Project/Prefabs/Player/PegasusStick";
            Directory.CreateDirectory(destination);AssetDatabase.Refresh();
            var source=EditorSceneManager.OpenScene(Source,OpenSceneMode.Additive);
            GameObject copy=null;
            try
            {
                var rig=source.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).Single(x=>x.name=="Stick_Carry");
                copy=UnityEngine.Object.Instantiate(rig.gameObject);copy.name="Pegasus Stick Visual";
                SceneManager.MoveGameObjectToScene(copy,scene);
                copy.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                var anim=copy.GetComponent<Animator>();
                string controllerPath=destination+"/PegasusStick.controller";
                if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(anim.runtimeAnimatorController),controllerPath))throw new InvalidOperationException("Controller destination must be new.");
                var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                controller.AddLayer("Stick Action");var layers=controller.layers;layers[2].defaultWeight=0;controller.layers=layers;
                string[] clipNames={"Stick_Grip_TwoHand_Transition","Stick_Attack_Forward_AttackingWithStickMotion","Stick_Grip_OneHand_Transition_Reverse","Stick_Throw_Ready","Stick_Throw_Release","Stick_Throw_Cancel"};
                string[] states={"Grip","Strike","Return","Ready","Release","Cancel"};
                var clips=clipNames.Select(n=>AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Art/Player/Animations/"+n+".anim")).ToArray();
                for(int i=0;i<clips.Length;i++){if(!clips[i])throw new InvalidOperationException(clipNames[i]);var s=layers[2].stateMachine.AddState(states[i]);s.motion=clips[i];s.writeDefaultValues=false;}
                anim.runtimeAnimatorController=controller;anim.applyRootMotion=false;anim.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                var prefab=PrefabUtility.SaveAsPrefabAsset(copy,destination+"/PegasusStickVisual.prefab");
                UnityEngine.Object.DestroyImmediate(copy);copy=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
                var camera=player.GetComponentInChildren<Camera>();copy.transform.SetParent(camera.transform,false);copy.transform.localPosition=new Vector3(0,-1.62f,-.1f);copy.transform.localRotation=Quaternion.identity;
                var prop=copy.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="Stick_Carry_Item");
                // Reuse the source's existing release-frame analysis, not a guessed hand-off time.
                var releaseRig=source.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).Single(x=>x.name=="Stick_Throw_Release");
                var method=typeof(PlayerHandsObjectAnimationTools).GetMethod("FindStickThrowReleaseFrame",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
                int releaseFrame=(int)method.Invoke(null,new object[]{releaseRig,clips[4]});
                var runtime=player.AddComponent<PegasusStickController>();runtime.Configure(copy.GetComponent<Animator>(),prop,camera,clips,releaseFrame/clips[4].frameRate);
                EditorUtility.SetDirty(runtime);AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                File.WriteAllText(Output+"/Apply.txt",$"Player unchanged location={player.transform.position:F4}; camera unchanged={camera.transform.localPosition:F4}; release={releaseFrame/clips[4].frameRate:F4}; source rig and clip assets unchanged.");
            }
            finally {EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(scene);}
        }
        static void State(string label)
        {
            var state=new StringBuilder($"Scene={SceneManager.GetActiveScene().path} Play={EditorApplication.isPlaying} Dirty={SceneManager.GetActiveScene().isDirty} Compiling={EditorApplication.isCompiling}\n");
            var controller=UnityEngine.Object.FindFirstObjectByType<PegasusStickController>();
            if(controller)
            {
                state.AppendLine($"Time={Time.time:F4} Phase={controller.CurrentPhase} elapsed={controller.PhaseTime:F3} held={controller.HasStick} hits={controller.HitCount} last={controller.LastHit} player={controller.transform.position:F4} window={controller.ContactWindow} contactAt={controller.LastContactTime:F4} contact={controller.ContactPoint:F4}");
                var camera=controller.GetComponentInChildren<Camera>();
                state.AppendLine($"Camera position={camera.transform.position:F4} forward={camera.transform.forward:F4} look={controller.GetComponent<FirstPersonPlayerInput>().Look:F4}");
                var animator=controller.GetComponentInChildren<Animator>(true);var animState=animator.GetCurrentAnimatorStateInfo(2);
                state.AppendLine($"Animator speed={animator.speed} state={animState.shortNameHash} normalized={animState.normalizedTime:F4} transition={animator.IsInTransition(2)} focused={Application.isFocused} cursor={Cursor.lockState} left={Mouse.current.leftButton.isPressed} right={Mouse.current.rightButton.isPressed}");
                state.AppendLine("Devices="+string.Join(",",InputSystem.devices.Select(d=>$"{d.name}:enabled={d.enabled}:background={d.canRunInBackground}")));
                foreach(var r in controller.GetComponentsInChildren<Renderer>(true))state.AppendLine($"Renderer {r.name} active={r.gameObject.activeInHierarchy} enabled={r.enabled} bounds={r.bounds} cameraLocal={camera.transform.InverseTransformPoint(r.bounds.center)} screen={camera.WorldToViewportPoint(r.bounds.center)}");
                if(controller.Projectile)state.AppendLine($"Projectile={controller.Projectile.transform.position:F4} velocity={controller.Projectile.GetComponent<Rigidbody>().linearVelocity:F3} firstContact={controller.Projectile.FirstContact} flightDistance={controller.Projectile.FlightDistance:F3} damagePending={controller.Projectile.DamagePending} hits={controller.Projectile.HitCount} hitTarget={controller.Projectile.HitTarget} hitAt={controller.Projectile.HitTime:F4}");
                var enemy=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>();if(enemy)
                {
                    state.AppendLine($"Enemy health={enemy.Health} state={enemy.Behaviour} position={enemy.transform.position:F4} colliders={enemy.GetComponentsInChildren<Collider>().Length} diedAt={enemy.DeathStartedAt:F4} endedAt={enemy.DeathAnimationEndedAt:F4}");
                    var motion=enemy.GetComponentInChildren<ParvumAnimationView>();state.AppendLine($"Animation={motion.Motion} time={motion.MotionTime:F4} deathDuration={motion.DeathDuration:F4} completed={motion.IsDeathPlaybackComplete}");
                    state.AppendLine($"Enemy velocity={enemy.GetComponent<Rigidbody>().linearVelocity:F4} target={(enemy.CurrentTarget?enemy.CurrentTarget.name:"null")} forward={enemy.transform.forward:F4}");
                    state.AppendLine($"Approach={enemy.ApproachDiagnostic} pursuitLoss={enemy.PursuitLossDiagnostic}");
                    var floor=GroundUnder(enemy.transform);var playerFloor=GroundUnder(controller.transform);
                    state.AppendLine($"Ground enemy={(floor.collider?floor.collider.name:"none")} root={(floor.collider?floor.transform.root.name:"none")} normal={floor.normal:F3} player={(playerFloor.collider?playerFloor.collider.name:"none")} rearDot={Vector3.Dot(enemy.transform.forward,(controller.transform.position-enemy.transform.position).normalized):F3}");
                    state.AppendLine($"Enemy visible bounds={enemy.GetComponentInChildren<SkinnedMeshRenderer>().bounds} collider={(enemy.GetComponent<Collider>()?enemy.GetComponent<Collider>().bounds.ToString():"none")}");
                    var target=controller.GetComponent<ParvumTarget>();state.AppendLine($"Player target alive={target.IsAlive} faction={target.Faction} surface={(target.Surface?target.Surface.name:"null")}");
                    if(enemy.Health>0)
                    {
                        var capsule=enemy.GetComponent<CapsuleCollider>();var origin=enemy.transform.position+Vector3.up*capsule.height*Mathf.Abs(enemy.transform.lossyScale.y)*.5f;var end=target.ClosestPoint(origin);
                        state.AppendLine("Sight blockers="+string.Join(",",Physics.RaycastAll(origin,end-origin,Vector3.Distance(origin,end),~0,QueryTriggerInteraction.Ignore).Where(h=>!h.transform.IsChildOf(enemy.transform) && h.collider!=target.Surface && !h.transform.IsChildOf(target.transform)).Select(h=>h.collider.name)));
                    }
                }
                state.AppendLine($"Player health={controller.GetComponent<FirstPersonPlayerStatus>().CurrentHealth} shield={controller.GetComponent<FirstPersonPlayerStatus>().CurrentShield} interact={controller.GetComponent<FirstPersonInteractionController>().CurrentTargetDisplayName} throwMode={controller.ThrowMode}");
                var input=controller.GetComponent<FirstPersonPlayerInput>();state.AppendLine($"Input suppressed={input.GameplayActionInputSuppressed} escape={input.CursorUnlockedByEscape} cursor={input.CursorLockSuppressed}");
                var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
                state.AppendLine($"NextAttack={typeof(PegasusStickController).GetField("nextAttack",flags).GetValue(controller)} Requested={typeof(PegasusStickController).GetField("throwRequested",flags).GetValue(controller)}");
                foreach(var hud in controller.GetComponentsInChildren<FirstPersonHud>())state.AppendLine($"Prompt={hud.InteractionPromptText.text} enabled={hud.InteractionPromptText.enabled}");
                if(Physics.Raycast(camera.transform.position,camera.transform.forward,out var rayHit,3,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))state.AppendLine("Exact centre ray="+rayHit.collider.name);
            }
            var entries=typeof(EditorApplication).Assembly.GetType("UnityEditor.LogEntries");
            var count=entries.GetMethod("GetCountsByType",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);
            object[] values={0,0,0};count.Invoke(null,values);state.AppendLine($"Console errors={values[0]} warnings={values[1]} logs={values[2]}");
            var entryType=typeof(EditorApplication).Assembly.GetType("UnityEditor.LogEntry");var entry=Activator.CreateInstance(entryType);
            entries.GetMethod("StartGettingEntries").Invoke(null,null);
            try{int total=(int)entries.GetMethod("GetCount").Invoke(null,null);for(int i=0;i<Math.Min(total,10);i++){entries.GetMethod("GetEntryInternal").Invoke(null,new[]{(object)i,entry});state.AppendLine("Console: "+entryType.GetField("message").GetValue(entry));}}
            finally{entries.GetMethod("EndGettingEntries").Invoke(null,null);}
            File.WriteAllText(Output+"/State_"+label+".txt",state.ToString());
        }
        static void Capture(string label)
        {
            var player=SceneManager.GetActiveScene().GetRootGameObjects().Single(x=>x.name=="Player");var camera=player.GetComponentInChildren<Camera>();
            Render(camera,label);State(label);
        }
        static void Render(Camera camera,string label)
        {
            var target=new RenderTexture(960,720,24);var previous=camera.targetTexture;var active=RenderTexture.active;var image=new Texture2D(960,720,TextureFormat.RGB24,false);
            try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,960,720),0,0);image.Apply();File.WriteAllBytes(Output+"/"+label+".png",image.EncodeToPNG());}
            finally{camera.targetTexture=previous;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);}
        }
        static void Inspect()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before source inspection.");
            var current = SceneManager.GetActiveScene();
            var source = SceneManager.GetSceneByPath(Source);
            bool opened = !source.isLoaded;
            if(opened) source = EditorSceneManager.OpenScene(Source, OpenSceneMode.Additive);
            try
            {
                var report = new StringBuilder();
                var all = source.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
                foreach(var name in Names)
                {
                    var matches = all.Where(x => x.name == name).ToArray();
                    report.AppendLine("SOURCE " + name + " count=" + matches.Length);
                    foreach(var root in matches) Dump(root, report);
                }
                var player = current.GetRootGameObjects().Single(x => x.name == "Player");
                report.AppendLine("PEGASUS PLAYER"); Dump(player.transform, report);
                File.WriteAllText(Output + "/Sources.txt", report.ToString());
            }
            finally { if(opened) EditorSceneManager.CloseScene(source, true); SceneManager.SetActiveScene(current); }
        }
        static void Dump(Transform root, StringBuilder report)
        {
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                report.AppendLine($"{AnimationUtility.CalculateTransformPath(t, root)} active={t.gameObject.activeSelf} p={t.localPosition:F4} r={t.localEulerAngles:F2} s={t.localScale:F4} components=" + string.Join(",", t.GetComponents<Component>().Select(x => x ? x.GetType().Name : "MISSING")));
                if(t.TryGetComponent<Animator>(out var animator))
                {
                    report.AppendLine($"ANIM {AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)} avatar={AssetDatabase.GetAssetPath(animator.avatar)} rootMotion={animator.applyRootMotion}");
                    if(animator.runtimeAnimatorController) foreach(var clip in animator.runtimeAnimatorController.animationClips.Distinct())report.AppendLine($"CLIP {clip.name} path={AssetDatabase.GetAssetPath(clip)} length={clip.length:F4}");
                }
                if(t.TryGetComponent<Renderer>(out var renderer)) report.AppendLine($"RENDER bounds={renderer.bounds} materials="+string.Join(",",renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath)));
            }
        }
    }
}
