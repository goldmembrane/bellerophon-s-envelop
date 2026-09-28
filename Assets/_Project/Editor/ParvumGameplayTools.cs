using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using Bellerophon.Enemies.Parvum;
using Bellerophon.Core.Ship;
using Bellerophon.Core.Session;
using Bellerophon.Core.Player;
using System.Collections.Generic;

namespace Bellerophon.Editor
{
    [InitializeOnLoad]
    internal static class ParvumGameplayTools
    {
        static ParvumGameplayTools(){EditorApplication.playModeStateChanged+=OnObservationPlayEntry;}
        internal const string DirectoryPath = "docs/validation/PegasusParvumAI";
        private const string SourcePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        [Serializable] private sealed class ReviewAction { public string mode; public string label; public float cargoDurability; public int startRoom; public int roomCount; public Vector3 cameraPosition; public Vector3 cameraLookAt; }
        internal static void Review()
        {
            Directory.CreateDirectory(DirectoryPath);
            var action=JsonUtility.FromJson<ReviewAction>(File.ReadAllText(DirectoryPath+"/Action.json"));
            switch(action.mode)
            {
                case "DoorRoomPathSurvey":
                    if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play required.");
                    var surveyActor=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>();
                    var surveyFilter=new NavMeshQueryFilter{agentTypeID=new SerializedObject(surveyActor).FindProperty("navigationAgentType").intValue,areaMask=NavMesh.AllAreas};
                    var roomBelowMethod=typeof(ParvumBrain).GetMethod("RoomBelow",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                    var clearMethod=typeof(ParvumBrain).GetMethod("RelocationRouteClear",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                    var pathSurvey=new StringBuilder($"Actor {surveyActor.transform.position:F4}\n");
                    NavMesh.SamplePosition(surveyActor.transform.position,out var surveyStart,.35f,surveyFilter);
                    foreach(var group in ParvumTarget.Active.Where(x=>x.IsRoomWall && x.Surface).GroupBy(x=>x.Room))
                    {
                        var bounds=group.First().Surface.bounds;foreach(var wall in group)bounds.Encapsulate(wall.Surface.bounds);
                        int probes=0,surfaces=0,floors=0,complete=0,clear=0;var failures=new Dictionary<string,int>();
                        var example=new StringBuilder();
                        for(float x=bounds.min.x+.6f;x<=bounds.max.x-.6f;x+=1.5f)
                        for(float z=bounds.min.z+.6f;z<=bounds.max.z-.6f;z+=1.5f)
                        {
                            probes++;var probe=new Vector3(x,bounds.min.y+.15f,z);
                            if(!NavMesh.SamplePosition(probe,out var hit,1f,surveyFilter))continue;surfaces++;
                            var floorRoom=roomBelowMethod.Invoke(surveyActor,new object[]{hit.position}) as ParvumTarget;
                            if(!floorRoom || floorRoom.Room!=group.Key)continue;floors++;
                            var route=new NavMeshPath();if(!NavMesh.CalculatePath(surveyStart.position,hit.position,surveyFilter,route) || route.status!=NavMeshPathStatus.PathComplete)continue;complete++;
                            string failure="";var valid=(bool)clearMethod.Invoke(surveyActor,new object[]{route.corners,(Action<string>)(s=>failure=s)});
                            if(valid)clear++;else {if(!failures.ContainsKey(failure))failures.Add(failure,0);failures[failure]++;}
                            if(complete<=3)example.AppendLine($"goal={hit.position:F4} clear={valid} reason={failure} corners="+string.Join(";",route.corners.Select(p=>p.ToString("F3"))));
                        }
                        pathSurvey.AppendLine($"ROOM {group.Key} bounds={bounds} probes={probes} nav={surfaces} roomFloors={floors} complete={complete} clear={clear}");pathSurvey.Append(example);
                        foreach(var failure in failures.OrderByDescending(p=>p.Value).Take(5))pathSurvey.AppendLine($"FAIL {failure.Value}: {failure.Key}");
                    }
                    File.WriteAllText(DirectoryPath+"/DoorRoomPathSurvey_"+action.label+".txt",pathSurvey.ToString());break;
                case "FinalDoorEntryCapture":
                    if(!EditorApplication.isPlaying || File.Exists(DirectoryPath+"/DoorEntryFinal.png"))throw new InvalidOperationException("Fresh final Play capture required.");
                    Capture(UnityEngine.Object.FindFirstObjectByType<ParvumBrain>().transform,DirectoryPath+"/DoorEntryFinal.png");ConsoleReport("DoorEntryFinal");break;
                case "RoomDoorContext":
                    CaptureDoorContext(action.cameraPosition,action.cameraLookAt,DirectoryPath+"/"+action.label+".png");break;
                case "RoomDoorPlaneSurvey":
                    var portalSurvey=new StringBuilder();
                    foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects().Where(x=>!x.name.Contains("Corridor")))
                    foreach(var mesh in root.GetComponentsInChildren<MeshFilter>().Where(x=>(root.name=="Approved Engine Room 01 Shell" && (x.transform.parent.name.StartsWith("Walls") || x.transform.parent.name.StartsWith("Entrances"))) || (root.name=="Approved Cargo Hold 01 Shell" && x.name.Contains("wall")) || System.Text.RegularExpressions.Regex.IsMatch(x.name,"doorway|threshold|entrance|Entrance|passage|corridor side|wall segment|bay wall|direction sign black|warehouse wall|door frame|inner.*ring|[Ff]rame|[Jj]amb")))
                    {
                        var renderer=mesh.GetComponent<Renderer>();if(!renderer)continue;
                        portalSurvey.AppendLine($"{root.name}/{mesh.transform.parent.name}/{mesh.name} position={mesh.transform.position:F3} bounds={renderer.bounds.center:F3}/{renderer.bounds.size:F3} right={mesh.transform.right:F3} forward={mesh.transform.forward:F3} mesh={mesh.sharedMesh.bounds}");
                    }
                    File.WriteAllText(DirectoryPath+"/RoomDoorPlaneSurvey.txt",portalSurvey.ToString());break;
                case "FinalEntryAdvanceTwoMetresCapture":
                    if(!EditorApplication.isPlaying || File.Exists(DirectoryPath+"/EntryAdvanceTwoMetresFinal.png"))throw new InvalidOperationException("Fresh final Play capture required.");
                    Capture(UnityEngine.Object.FindFirstObjectByType<ParvumBrain>().transform,DirectoryPath+"/EntryAdvanceTwoMetresFinal.png");ConsoleReport("EntryAdvanceTwoMetresFinal");break;
                case "FinalEntryAdvanceCapture":
                    if(!EditorApplication.isPlaying || File.Exists(DirectoryPath+"/EntryAdvanceFinal.png"))throw new InvalidOperationException("Fresh final Play capture required.");
                    Capture(UnityEngine.Object.FindFirstObjectByType<ParvumBrain>().transform,DirectoryPath+"/EntryAdvanceFinal.png");ConsoleReport("EntryAdvanceFinal");break;
                case "FinalBiteSizeCapture":
                    if(!EditorApplication.isPlaying || File.Exists(DirectoryPath+"/BiteSizeFinal.png"))throw new InvalidOperationException("Fresh final Play capture required.");
                    Capture(UnityEngine.Object.FindFirstObjectByType<ParvumBrain>().transform,DirectoryPath+"/BiteSizeFinal.png");ConsoleReport("BiteSizeFinal");break;
                case "ApplyBiteSize":
                    if(EditorApplication.isPlaying || SceneManager.GetActiveScene().name!="Pegasus")throw new InvalidOperationException("Pegasus edit mode required.");
                    var sizeRoot=SceneManager.GetActiveScene().GetRootGameObjects().Single(x=>x.name=="ParvumGameplay");
                    sizeRoot.transform.localScale=Vector3.one; // Navigation container retains its original scale.
                    var sizeActor=sizeRoot.GetComponentInChildren<ParvumBrain>(true).gameObject;
                    sizeActor.transform.localScale=Vector3.one*2.5f;
                    var sizeSettings=new SerializedObject(sizeActor.GetComponent<ParvumBrain>());sizeSettings.FindProperty("biteApproachDistance").floatValue=1.25f;sizeSettings.FindProperty("biteReach").floatValue=1.55f;sizeSettings.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(sizeActor.transform);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(sizeActor.GetComponent<ParvumBrain>());
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                    RebuildNavigation();ConsoleReport("BiteSizeApplied");break;
                case "ObserveBiteSize":
                    if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play required.");
                    foreach(var existing in UnityEngine.Object.FindObjectsByType<ParvumBrain>(FindObjectsSortMode.None))existing.gameObject.SetActive(false);
                    biteSizeActor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/Parvum/ParvumGameplay.prefab")).GetComponent<ParvumBrain>();
                    biteSizeActor.name="Parvum bite size runtime observation";
                    biteSizeActor.GetComponent<Rigidbody>().position=new Vector3(55.2f,2.68f,30.918f);
                    biteSizeLabel=action.label; biteSizeStart=Time.time;biteSizeNext=0;biteSizeFrame=0;biteSizeReport=new StringBuilder();
                    foreach(var food in ParvumTarget.Active.Where(x=>x.IsRoomWall))food.Bitten+=(actor,damage)=>{if(actor==biteSizeActor)biteSizeReport.AppendLine($"CONTACT t={Time.time-biteSizeStart:F4} target={food.name} damage={damage}");};
                    EditorApplication.update-=BiteSizeTick;EditorApplication.update+=BiteSizeTick;break;
                case "ObserveRelocation":
                    if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play required.");
                    relocationLabel=action.label??"RoomRelocation";relocationStart=Time.time;relocationNext=0;relocationFrame=0;relocationReport=new StringBuilder();entryStimulusSent=false;
                    EditorApplication.update-=RelocationTick;EditorApplication.update+=RelocationTick;break;
                case "RelocationBlockedStart":
                    if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play required.");
                    UnityEngine.Object.FindFirstObjectByType<ParvumBrain>().gameObject.SetActive(false);
                    var retryActor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/Parvum/ParvumGameplay.prefab"));
                    retryActor.name="Parvum relocation initial-condition observation";
                    retryActor.GetComponent<Rigidbody>().position=new Vector3(55.402f,-4.435f,8.002f);
                    var retryShip=UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
                    retryShip.SetShipState(retryShip.CurrentShipState.WithRoom(ShipRoomId.CargoHold,retryShip.CurrentShipState.GetRoom(ShipRoomId.CargoHold).WithDamage(500)));
                    break;
                case "RelocationGeometry":
                    var geometryActor=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>();var geometryReport=new StringBuilder();
                    var geometryFilter=new NavMeshQueryFilter{agentTypeID=new SerializedObject(geometryActor).FindProperty("navigationAgentType").intValue,areaMask=NavMesh.AllAreas};
                    var geometryMethod=typeof(ParvumBrain).GetMethod("RelocationRouteClear",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                    NavMesh.SamplePosition(geometryActor.transform.position,out var geometryStart,.35f,geometryFilter);
                    for(int direction=0;direction<16;direction++)
                    {
                        var probe=geometryActor.transform.position+Quaternion.Euler(0,direction*22.5f,0)*Vector3.forward*4;
                        var geometryPath=new NavMeshPath();
                        if(!NavMesh.SamplePosition(probe,out var goal,.8f,geometryFilter) || !NavMesh.CalculatePath(geometryStart.position,goal.position,geometryFilter,geometryPath))continue;
                        geometryReport.AppendLine($"direction={direction} goal={goal.position:F3} path={geometryPath.status} actor={geometryActor.transform.position:F3}");
                        var valid=geometryMethod.Invoke(geometryActor,new object[]{geometryPath.corners,(Action<string>)(s=>geometryReport.AppendLine(s))});geometryReport.AppendLine("clear="+valid);
                    }
                    File.WriteAllText(DirectoryPath+"/RelocationGeometry.txt",geometryReport.ToString());break;
                case "ExhaustOccupiedRoom":
                    if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play required.");
                    var relocationActor=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>();
                    if(!relocationActor.OccupiedRoom.HasValue)throw new InvalidOperationException("No observed occupied room.");
                    var relocationShip=UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
                    var exhaustedId=relocationActor.OccupiedRoom.Value;
                    relocationShip.SetShipState(relocationShip.CurrentShipState.WithRoom(exhaustedId,relocationShip.CurrentShipState.GetRoom(exhaustedId).WithDamage(500)));
                    break;
                case "RelocationCondition":
                    if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play required.");
                    var conditionActor=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>();
                    var conditionShip=UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
                    if(action.label=="DestroyDestination" && conditionActor.DestinationRoom.HasValue)
                    {
                        var id=conditionActor.DestinationRoom.Value;
                        conditionShip.SetShipState(conditionShip.CurrentShipState.WithRoom(id,conditionShip.CurrentShipState.GetRoom(id).WithDamage(500)));
                    }
                    else if(action.label=="SealSource" || action.label=="UnsealSource")
                    {
                        var id=conditionActor.OccupiedRoom.Value;
                        conditionShip.SetShipState(conditionShip.CurrentShipState.WithRoom(id,conditionShip.CurrentShipState.GetRoom(id).WithSealed(action.label=="SealSource")));
                    }
                    else if(action.label=="DoorDestinationOnly")
                    {
                        foreach(ShipRoomId id in Enum.GetValues(typeof(ShipRoomId)))
                            conditionShip.SetShipState(conditionShip.CurrentShipState.WithRoom(id,new ShipRoomState((int)id==action.startRoom?500:0,500)));
                    }
                    else if(action.label=="ExhaustAll" || action.label=="RestoreRooms")
                    {
                        foreach(ShipRoomId id in Enum.GetValues(typeof(ShipRoomId)))
                            conditionShip.SetShipState(conditionShip.CurrentShipState.WithRoom(id,new ShipRoomState(action.label=="ExhaustAll"?0:500,500)));
                    }
                    else if(action.label=="RemoveStimuli")
                    {
                        foreach(var stimulus in UnityEngine.Object.FindObjectsByType<ParvumTarget>(FindObjectsSortMode.None).Where(x=>x.name.StartsWith("Relocation runtime")))UnityEngine.Object.DestroyImmediate(stimulus.gameObject);
                    }
                    else if(action.label=="Speaker" || action.label=="Attacker")
                    {
                        var filter=new NavMeshQueryFilter{agentTypeID=new SerializedObject(conditionActor).FindProperty("navigationAgentType").intValue,areaMask=NavMesh.AllAreas};
                        if(!NavMesh.SamplePosition(conditionActor.transform.position+conditionActor.transform.forward*.8f,out var hit,.6f,filter))throw new InvalidOperationException("No stimulus location");
                        var stimulus=GameObject.CreatePrimitive(PrimitiveType.Cube);stimulus.name="Relocation runtime "+action.label;
                        var stimulusBase=hit.position;
                        if(Physics.Raycast(stimulusBase+Vector3.up*.05f,Vector3.down,out var stimulusFloor,1f,~0,QueryTriggerInteraction.Ignore))stimulusBase=stimulusFloor.point;
                        stimulus.transform.position=stimulusBase+Vector3.up*.3f;stimulus.transform.localScale=new Vector3(.3f,.6f,.3f);
                        var collider=stimulus.GetComponent<Collider>();collider.isTrigger=true;
                        var stimulusTarget=stimulus.AddComponent<ParvumTarget>();stimulusTarget.ConfigureCombatTarget(true,6,0,IntruderFaction.None);
                        stimulusTarget.Bitten+=(b,d)=>relocationReport?.AppendLine($"CONTACT {stimulus.name} damage={d} remaining={stimulusTarget.Health}");
                        if(action.label=="Speaker"){stimulusTarget.ConfigureSpeaker(null,collider);stimulusTarget.SetSpeakerAudible(true);}
                        else conditionActor.ReceiveDamage(1,stimulusTarget);
                    }
                    else throw new InvalidOperationException("Condition not applicable: "+action.label);
                    relocationReport?.AppendLine("CONDITION "+action.label+" at="+(Time.time-relocationStart).ToString("F3"));
                    break;
                case "FinalRelocationCapture":
                    if(!EditorApplication.isPlaying || File.Exists(DirectoryPath+"/RoomRelocationFinal.png"))throw new InvalidOperationException("Fresh final Play capture required.");
                    Capture(UnityEngine.Object.FindFirstObjectByType<ParvumBrain>().transform,DirectoryPath+"/RoomRelocationFinal.png");ConsoleReport("RoomRelocationFinal");break;
                case "ContactGeometry":
                    var contactActor=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>();
                    var contactSkin=contactActor.GetComponentInChildren<SkinnedMeshRenderer>();var contactReport=new StringBuilder();
                    contactReport.AppendLine($"scale={contactSkin.transform.lossyScale} local={contactSkin.transform.localScale} renderer={contactSkin.bounds} root={contactActor.transform.position} skin={contactSkin.transform.position}");
                    foreach(bool scale in new[]{false,true})
                    {
                        var mesh=new Mesh();contactSkin.BakeMesh(mesh,scale);
                        var points=mesh.vertices.Select(v=>contactActor.transform.InverseTransformPoint(contactSkin.transform.TransformPoint(v))).ToArray();
                        contactReport.AppendLine($"useScale={scale} bounds={mesh.bounds} localMinY={points.Min(v=>v.y)} localMaxY={points.Max(v=>v.y)} front={points.OrderByDescending(v=>v.z).First():F4}");
                        UnityEngine.Object.DestroyImmediate(mesh);
                    }
                    File.WriteAllText(DirectoryPath+"/ContactGeometry.txt",contactReport.ToString());
                    Capture(contactActor.transform,DirectoryPath+"/WallBiteGeometry.png");break;
                case "ApplyWallTargets":
                    ApplyWallTargets();break;
                case "ObserveBiteReach":
                    if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play required.");
                    reachOriginal=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>();reachOriginal.gameObject.SetActive(false);
                    reachLabel=action.label??"WallBiteReach";reachStage=-1;reachStart=Time.time-4;reachNext=0;reachFrame=0;reachReport=new StringBuilder();
                    EditorApplication.update-=ReachTick;EditorApplication.update+=ReachTick;break;
                case "FinalWallCapture":
                    if(!EditorApplication.isPlaying || File.Exists(DirectoryPath+"/WallBiteFinal.png"))throw new InvalidOperationException("Fresh final Play capture required.");
                    Capture(UnityEngine.Object.FindFirstObjectByType<ParvumBrain>().transform,DirectoryPath+"/WallBiteFinal.png");ConsoleReport("WallBiteFinal");break;
                case "ObserveWallRooms":
                    if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play required.");
                    originalWallActor=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>();originalWallActor.gameObject.SetActive(false);
                    wallRoomLabel=action.label??"WallBiteRoom";
                    wallRoomIndex=action.startRoom-1;wallRoomEnd=action.roomCount>0?Math.Min(6,action.startRoom+action.roomCount):6;wallRoomStart=Time.time-21;wallRoomNext=0;wallRoomFrame=0;wallRoomReport=new StringBuilder();
                    EditorApplication.update-=WallRoomTick;EditorApplication.update+=WallRoomTick;break;
                case "ObserveWallBite":
                    if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play required.");
                    wallObserveStart=Time.time;wallObserveNext=0;wallObserveFrame=0;wallObserveReport=new StringBuilder();
                    EditorApplication.update-=WallObserveTick;EditorApplication.update+=WallObserveTick;break;
                case "NavigationConnectivity":
                    if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode required.");
                    InspectNavigationConnectivity();break;
                case "PlayPlayerCombat":
                case "PlayRoom500Observe":
                case "PlayBiteSurvey":
                case "PlayFacilityObserve":
                case "PlayRouteObserve":
                case "PlayEntranceObserve":
                case "PlaySpeakerObserve":
                case "PlayDoorObserve":
                case "PlayContactRetry":
                case "PlayLossObserve":
                    if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode required.");
                    pendingEntryMode=action.mode;
                    SessionState.SetString("Bellerophon.ParvumObservationEntry",action.mode);
                    EditorApplication.playModeStateChanged-=OnObservationPlayEntry;EditorApplication.playModeStateChanged+=OnObservationPlayEntry;
                    EditorApplication.isPlaying=true;break;
                case "Activate":
                    if(EditorApplication.isPlaying || SceneManager.GetActiveScene().name!="Pegasus")throw new InvalidOperationException("Pegasus edit required.");
                    SceneManager.GetActiveScene().GetRootGameObjects().Single(x=>x.name=="ParvumGameplay").SetActive(true);
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());break;
                case "CombatObserve":
                    if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play required.");
                    Application.runInBackground=true;EditorApplication.isPaused=false;
                    scenarioBrain=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>();
                    scenarioRoot=new GameObject("Parvum combat controlled inputs");
                    combatStage=-1;combatStart=Time.time;combatNextFrame=0;combatFrame=0;combatReport=new StringBuilder();
                    EditorApplication.update-=CombatObserveTick;EditorApplication.update+=CombatObserveTick;break;
                case "PlayerCombatObserve":
                    if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play required.");
                    Application.runInBackground=true;EditorApplication.isPaused=false;
                    var playerStatus=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerStatus>();
                    playerStatus.SetVitalsForValidation(playerStatus.MaxHealth,2);
                    scenarioBrain=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>();
                    scenarioBrain.ReceiveDamage(1,playerStatus.GetComponent<ParvumTarget>());
                    combatStart=Time.time;combatNextFrame=0;combatFrame=0;combatReport=new StringBuilder();
                    EditorApplication.update-=PlayerCombatTick;EditorApplication.update+=PlayerCombatTick;break;
                case "CargoPathInspect":
                    if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode required.");
                    var navInspection=NavMesh.AddNavMeshData(AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/_Project/Navigation/PegasusParvum/ParvumNavMesh.asset"));
                    try
                    {
                        var food=UnityEngine.Object.FindObjectsByType<ParvumTarget>(FindObjectsSortMode.None).Single(x=>x.Kind==ParvumTargetKind.MetalCargo);
                        var origin=new Vector3(53.9395f,-4.1015f,4.9777f);
                        var contact=food.ClosestPoint(origin+Vector3.up*.2f);
                        var report=new StringBuilder($"Cargo transform={food.transform.position:F4} scale={food.transform.lossyScale:F4} contact={contact:F4}\n");
                        NavMesh.SamplePosition(origin,out var start,.35f,NavMesh.AllAreas);
                        for(int i=0;i<=20;i++)
                        {
                            var p=Vector3.Lerp(origin,contact,i/20f);
                            bool sampled=NavMesh.SamplePosition(p,out var navPoint,.7f,NavMesh.AllAreas);
                            var route=new NavMeshPath();bool found=sampled && NavMesh.CalculatePath(start.position,navPoint.position,NavMesh.AllAreas,route);
                            report.AppendLine($"p={p:F3} nav={sampled} at={navPoint.position:F3} path={found}/{route.status} corners={string.Join(";",route.corners.Select(x=>x.ToString("F3")))}");
                            foreach(var ground in Physics.RaycastAll(p+Vector3.up*2,Vector3.down,4,~0,QueryTriggerInteraction.Ignore).OrderBy(x=>x.distance))
                                report.AppendLine($"  physical={ground.point:F3} {ground.collider.name}");
                        }
                        Capture(food.transform,DirectoryPath+"/CargoContext.png");
                        File.WriteAllText(DirectoryPath+"/CargoPaths.txt",report.ToString());
                    }
                    finally{navInspection.Remove();}break;
                case "BindCargo":
                    var cargoScene=SceneManager.GetActiveScene();
                    if(EditorApplication.isPlaying || cargoScene.name!="Pegasus")throw new InvalidOperationException("Pegasus edit mode required.");
                    var cargoBody=cargoScene.GetRootGameObjects().Single(x=>x.name=="Approved Cargo Hold 01 Shell")
                        .GetComponentsInChildren<Transform>(true).Single(x=>x.name=="CH-03 single central cargo container body");
                    var cargoTarget=cargoBody.GetComponent<ParvumTarget>()??cargoBody.gameObject.AddComponent<ParvumTarget>();
                    cargoTarget.ConfigureCargo(cargoBody.GetComponent<Collider>(),UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>(),UnityEngine.Object.FindFirstObjectByType<TransportSettlementController>());
                    EditorUtility.SetDirty(cargoTarget);
                    EditorSceneManager.MarkSceneDirty(cargoScene);EditorSceneManager.SaveScene(cargoScene);
                    File.WriteAllText(DirectoryPath+"/CargoBinding.txt","Bound actual CH-03 cargo body to CurrentCargoState. Material comes from the active contract; unknown/nonmetal cargo is not food. Existing collider enabled state and all transforms preserved.");
                    ConsoleReport("CargoBinding");break;
                case "CargoRuleReview":
                    var cargoReport=new StringBuilder("Supplementary rule check only, NOT observed AI/cargo contact.\n");
                    foreach(float initial in new[]{1f,.5f,.17f,.001f})
                    {
                        var cargoLedger=new ParvumCargoLedger();float remaining=initial;
                        for(int bite=1;bite<=120;bite++)
                        {
                            remaining=cargoLedger.ConsumeBite(remaining);
                            if(bite<120 && remaining<=0)throw new InvalidOperationException("Premature cargo depletion.");
                            if(bite==60 || bite==119 || bite==120)cargoReport.AppendLine($"initial={initial:R} time={bite*.5f:F1} remaining={remaining:R}");
                        }
                        if(remaining!=0)throw new InvalidOperationException("Cargo did not deplete at 60 seconds.");
                    }
                    File.WriteAllText(DirectoryPath+"/CargoRuleReview.txt",cargoReport.ToString());ConsoleReport("CargoRuleReview");break;
                case "CargoSizeStart":
                    if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play required.");
                    foreach(var oldActor in UnityEngine.Object.FindObjectsByType<ParvumBrain>(FindObjectsSortMode.None))oldActor.gameObject.SetActive(false);
                    // Fresh same-prefab actor at the unchanged saved spawn; metal input is set
                    // before its first physics tick, not after it has already left detection range.
                    UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/Parvum/ParvumGameplay.prefab")).name="Parvum cargo initial-condition observation";
                    goto case "CargoObserve";
                case "CargoObserve":
                    if(!EditorApplication.isPlaying || SceneManager.GetActiveScene().name!="Pegasus")throw new InvalidOperationException("Pegasus Play required.");
                    Application.runInBackground=true;EditorApplication.isPaused=false;
                    var liveCargo=UnityEngine.Object.FindObjectsByType<ParvumTarget>(FindObjectsSortMode.None).Single(x=>x.Kind==ParvumTargetKind.MetalCargo);
                    // Explicit metal-cargo input for this runtime observation only. No actor, surface,
                    // path, geometry or competing food target is moved/removed to force success.
                    liveCargo.ConfigureCargo(liveCargo.Surface,UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>(),null,CargoMaterial.CommonMetal);
                    if(action.cargoDurability>0)
                    {
                        var cargoState=UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
                        cargoState.SetCargoState(cargoState.CurrentCargoState.WithDurabilityPercent(action.cargoDurability));
                    }
                    observationLabel=string.IsNullOrEmpty(action.label)?"CargoObserve":action.label;
                    cargoBiteCount=0;cargoFirstBiteTime=-1;
                    File.WriteAllText(DirectoryPath+"/"+observationLabel+"_Bites.txt","Direct observed cargo bite events; duration includes the initial 0.5-second windup.\n");
                    liveCargo.Bitten+=(source,damage)=>
                    {
                        if(cargoFirstBiteTime<0)cargoFirstBiteTime=Time.fixedTime;
                        cargoBiteCount++;
                        File.AppendAllText(DirectoryPath+"/"+observationLabel+"_Bites.txt",$"bite={cargoBiteCount} fixedTime={Time.fixedTime:F4} feedingSeconds={Time.fixedTime-cargoFirstBiteTime+.5f:F4} damage={damage:R}\n");
                    };
                    SceneManager.GetActiveScene().GetRootGameObjects().Single(x=>x.name=="ParvumGameplay").SetActive(true);
                    cargoObservationStart=Time.time;cargoNextFrame=0;cargoFrame=0;cargoObservationLog=new StringBuilder();
                    EditorApplication.update-=CargoObserveTick;EditorApplication.update+=CargoObserveTick;break;
                case "PreserveDraft":
                    if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode required.");
                    var draft=GameObject.Find("ParvumGameplay");if(draft)draft.SetActive(false);
                    var navigation=AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/_Project/Navigation/PegasusParvum/ParvumNavMesh.asset");
                    navigation.name="ParvumNavMesh";EditorUtility.SetDirty(navigation);AssetDatabase.SaveAssets();
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                    EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),DirectoryPath+"/Draft.unity",true);
                    ConsoleReport("Draft");break;
                case "NavigationAndExclusions":
                    if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode required.");
                    foreach(var candidate in UnityEngine.Object.FindObjectsByType<ParvumTarget>(FindObjectsSortMode.None))
                        if(candidate.Kind==ParvumTargetKind.Facility && (candidate.name.ToLowerInvariant().Contains("corridor") || candidate.name.ToLowerInvariant().Contains("glass")))
                            UnityEngine.Object.DestroyImmediate(candidate);
                    RebuildNavigation();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());break;
                case "CombineMotionMeshes":
                    if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode required.");
                    var view=UnityEngine.Object.FindFirstObjectByType<ParvumAnimationView>();
                    var original=view.Sources[0].mesh;
                    var combined=UnityEngine.Object.Instantiate(original);combined.name="Parvum Gameplay Motion Bindings";combined.ClearBlendShapes();
                    var baseVertices=original.vertices;
                    foreach(var motion in view.Sources)
                    {
                        var other=motion.mesh.vertices;
                        if(other.Length!=baseVertices.Length)throw new InvalidOperationException("Motion topology differs.");
                        for(int i=0;i<other.Length;i++)if((other[i]-baseVertices[i]).sqrMagnitude>1e-10f)throw new InvalidOperationException("Motion base vertices differ; no automatic shape rewrite.");
                        var dv=new Vector3[other.Length];var dn=new Vector3[other.Length];var dt=new Vector3[other.Length];
                        for(int shape=0;shape<motion.mesh.blendShapeCount;shape++)
                        for(int frame=0;frame<motion.mesh.GetBlendShapeFrameCount(shape);frame++)
                        {motion.mesh.GetBlendShapeFrameVertices(shape,frame,dv,dn,dt);combined.AddBlendShapeFrame(motion.mesh.GetBlendShapeName(shape),motion.mesh.GetBlendShapeFrameWeight(shape,frame),dv,dn,dt);}
                    }
                    AssetDatabase.CreateAsset(combined,"Assets/_Project/Prefabs/Enemies/Parvum/ParvumGameplayMesh.asset");
                    view.UseCombinedMesh(combined);EditorUtility.SetDirty(view);
                    PrefabUtility.SaveAsPrefabAsset(view.transform.parent.gameObject,"Assets/_Project/Prefabs/Enemies/Parvum/ParvumGameplay.prefab");
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();break;
                case "CacheSurfaces":
                    if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode required.");
                    foreach(var surface in UnityEngine.Object.FindObjectsByType<ParvumTarget>(FindObjectsSortMode.None)){surface.CacheSurface();EditorUtility.SetDirty(surface);}
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());break;
                case "Play":
                    ConsoleReport("BeforePlay");
                    typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries").GetMethod("Clear",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static).Invoke(null,null);
                    EditorApplication.isPlaying=true;break;
                case "Stop": SessionState.EraseString("Bellerophon.ParvumObservationEntry");EditorApplication.update-=ObserveTick;EditorApplication.isPlaying=false;break;
                case "Observe":
                    if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play mode required.");
                    Application.runInBackground=true;EditorApplication.isPaused=false;
                    observationLabel=action.label;observationStart=EditorApplication.timeSinceStartup;nextFrame=0;frameNumber=0;
                    observationLog=new StringBuilder();EditorApplication.update-=ObserveTick;EditorApplication.update+=ObserveTick;break;
                case "Motions":
                    if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play required.");
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/Parvum/ParvumGameplay.prefab");
                    var temporary=new GameObject("Parvum temporary motion comparisons");temporary.transform.SetParent(GameObject.Find("ParvumGameplay").transform);
                    for(int i=0;i<5;i++)
                    {
                        var copy=UnityEngine.Object.Instantiate(prefab,temporary.transform);copy.name="Comparison_"+(ParvumMotion)i;
                        copy.GetComponent<ParvumBrain>().enabled=false;copy.GetComponent<Rigidbody>().isKinematic=true;copy.GetComponent<Collider>().enabled=false;
                        copy.transform.position=new Vector3(50+i*5,-30,5);copy.transform.rotation=Quaternion.Euler(0,180,0);
                        copy.GetComponentInChildren<ParvumAnimationView>().SetMotion((ParvumMotion)i,true);
                    }
                    observationLabel="MotionComparison";observationStart=EditorApplication.timeSinceStartup;nextFrame=0;frameNumber=0;
                    EditorApplication.update-=MotionTick;EditorApplication.update+=MotionTick;break;
                case "State":
                    var scene=SceneManager.GetActiveScene();
                    var brain=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>();
                    File.WriteAllText(DirectoryPath+"/State.txt",$"Scene={scene.path} Play={EditorApplication.isPlaying} Paused={EditorApplication.isPaused} Time={Time.time} Frame={Time.frameCount} Background={Application.runInBackground} Dirty={scene.isDirty} Compiling={EditorApplication.isCompiling}\n"+(brain?$"position={brain.transform.position:F4} Health={brain.Health} Behaviour={brain.Behaviour} target={brain.CurrentTarget}":"No brain"));
                    ConsoleReport(action.label??"Current");break;
                case "WallBiteSurvey":
                    if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode required.");
                    EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),DirectoryPath+"/WallBiteBefore.unity",true);
                    var walls=new StringBuilder();
                    foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects().Where(x=>x.name.StartsWith("Approved ") && !x.name.Contains("Corridor")))
                    foreach(var c in root.GetComponentsInChildren<Collider>().Where(x=>x.enabled && !x.isTrigger))
                        walls.AppendLine($"{root.name}/{c.name} center={c.bounds.center:F3} size={c.bounds.size:F3} target={c.GetComponent<ParvumTarget>()?.Kind}");
                    File.WriteAllText(DirectoryPath+"/WallCandidates.txt",walls.ToString());break;
                case "FinalCapture":
                    if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play mode required.");
                    if(File.Exists(DirectoryPath+"/Final.png"))throw new InvalidOperationException("Final capture already exists; do not repeat automatically.");
                    Capture(UnityEngine.Object.FindFirstObjectByType<ParvumBrain>().transform,DirectoryPath+"/Final.png");
                    ConsoleReport("FinalCapture");break;
                case "EntranceSurvey":
                    var survey=new StringBuilder();
                    foreach(var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(x=>x.enabled && (x.name=="FittedFloor" || x.name.Contains("straight floor slab") || System.Text.RegularExpressions.Regex.IsMatch(x.name,@"(RoomContact|WarehouseContact) \d+ 0$"))))
                        survey.AppendLine($"{c.transform.root.name}/{c.transform.parent.name}/{c.name} center={c.bounds.center:F3} size={c.bounds.size:F3}");
                    File.WriteAllText(DirectoryPath+"/EntranceSurvey.txt",survey.ToString());break;
                case "DoorSurvey":
                    var doors=new StringBuilder();
                    foreach(var door in UnityEngine.Object.FindObjectsByType<PegasusEntranceDoor>(FindObjectsSortMode.None))
                    {
                        doors.AppendLine($"{door.name} {door.RoomA}/{door.RoomB} index={door.CorridorIndex} open={door.IsOpen}");
                        var parts=(PegasusEntranceDoor.Part[])typeof(PegasusEntranceDoor).GetField("parts",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(door);
                        foreach(var part in parts)foreach(var collider in part.colliders)if(collider)doors.AppendLine($"  {collider.name} enabled={collider.enabled} position={collider.transform.position:F3} bounds={collider.bounds}");
                    }
                    File.WriteAllText(DirectoryPath+"/DoorSurvey.txt",doors.ToString());break;
                case "Scenario":
                    if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play required.");
                    Application.runInBackground=true;EditorApplication.isPaused=false;
                    scenarioBrain=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>();
                    scenarioRoot=new GameObject("Parvum controlled stimuli - not player acceptance");
                    firstAttacker=CreateStimulus("Attacker A",1.5f,false);
                    secondAttacker=CreateStimulus("Attacker B",2,false);
                    soundTarget=CreateStimulus("Speaker stimulus",3.5f,true);soundTarget.SetSpeakerAudible(false);
                    scenarioStart=EditorApplication.timeSinceStartup;scenarioStage=0;nextScenarioFrame=0;scenarioFrame=0;scenarioReport=new StringBuilder();
                    EditorApplication.update-=ScenarioTick;EditorApplication.update+=ScenarioTick;break;
            }
        }
        private static ParvumBrain scenarioBrain;
        private static void InspectNavigationConnectivity()
        {
            var data=AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/_Project/Navigation/PegasusParvum/ParvumNavMesh.asset");
            data.name="ParvumNavMesh";EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
            var nav=NavMesh.AddNavMeshData(data);
            try
            {
                var report=new StringBuilder("Supplementary connectivity, not actual traversal.\n");
                foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects().Where(x=>x.name=="Approved Ship Corridor Segments" || (x.name.StartsWith("Cargo ") && x.name.EndsWith("Corridor and Contacts"))))
                {
                    foreach(var floor in root.GetComponentsInChildren<Collider>().Where(x=>x.enabled && (x.name.Contains("straight floor slab") || System.Text.RegularExpressions.Regex.IsMatch(x.name,@" Body18m \d+ 0$"))))
                    {
                        Vector3 center=floor.bounds.center;Vector3 axis=floor.transform.forward;float length=0;
                        if(floor is BoxCollider box)
                        {
                            var size=Vector3.Scale(box.size,box.transform.lossyScale);
                            bool x=Mathf.Abs(size.x)>Mathf.Abs(size.z);axis=x?floor.transform.right:floor.transform.forward;length=Mathf.Abs(x?size.x:size.z);
                        }
                        else if(floor is MeshCollider mesh)
                        {
                            var size=Vector3.Scale(mesh.sharedMesh.bounds.size,mesh.transform.lossyScale);
                            bool x=Mathf.Abs(size.x)>Mathf.Abs(size.z);axis=x?floor.transform.right:floor.transform.forward;length=Mathf.Abs(x?size.x:size.z);
                        }
                        Vector3 a=center-axis*Mathf.Max(0,length*.5f-.3f),b=center+axis*Mathf.Max(0,length*.5f-.3f);
                        bool sa=NavMesh.SamplePosition(a,out var na,.6f,NavMesh.AllAreas),sb=NavMesh.SamplePosition(b,out var nb,.6f,NavMesh.AllAreas);
                        var route=new NavMeshPath();bool connected=sa && sb && NavMesh.CalculatePath(na.position,nb.position,NavMesh.AllAreas,route) && route.status==NavMeshPathStatus.PathComplete;
                        report.AppendLine($"{root.name}/{floor.name}: sampled={sa}/{sb} complete={connected} a={na.position:F3} b={nb.position:F3}");
                    }
                }
                File.WriteAllText(DirectoryPath+"/NavigationConnectivity.txt",report.ToString());
            }
            finally{nav.Remove();}
        }
        private static string pendingEntryMode;
        private static float facilityStart,facilityNextFrame;
        private static int facilityStage,facilityFrame;
        private static ParvumTarget facilityObserved;
        private static StringBuilder facilityReport;
        private static void OnObservationPlayEntry(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode)return;
            EditorApplication.playModeStateChanged-=OnObservationPlayEntry;
            pendingEntryMode=SessionState.GetString("Bellerophon.ParvumObservationEntry",string.Empty);
            SessionState.EraseString("Bellerophon.ParvumObservationEntry");
            if(pendingEntryMode=="PlayBiteSurvey")
            {
                Application.runInBackground=true;EditorApplication.isPaused=false;
                biteSurveyStart=Time.time;biteSurveyNext=0;biteSurveyFrame=0;biteSurveyReport=new StringBuilder();
                EditorApplication.update-=BiteSurveyTick;EditorApplication.update+=BiteSurveyTick;return;
            }
            if(pendingEntryMode=="PlayRoom500Observe")
            {
                Application.runInBackground=true;EditorApplication.isPaused=false;
                room500Report=new StringBuilder("Actual fresh Pegasus state; no durability overrides.\n");
                var ship=UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
                foreach(ShipRoomId id in Enum.GetValues(typeof(ShipRoomId)))
                {
                    var room=ship.CurrentShipState.GetRoom(id);
                    room500Report.AppendLine($"INITIAL {id}={room.CurrentDurability}/{room.MaxDurability}");
                    if(room.CurrentDurability!=500 || room.MaxDurability!=500)throw new InvalidOperationException("Fresh room is not 500/500: "+id);
                }
                room500Start=Time.time;room500Next=0;
                EditorApplication.update-=Room500Tick;EditorApplication.update+=Room500Tick;
                return;
            }
            if(pendingEntryMode!="PlayPlayerCombat" && pendingEntryMode!="PlayFacilityObserve" && pendingEntryMode!="PlayRouteObserve" && pendingEntryMode!="PlayEntranceObserve" && pendingEntryMode!="PlayContactRetry" && pendingEntryMode!="PlayDoorObserve" && pendingEntryMode!="PlaySpeakerObserve" && pendingEntryMode!="PlayLossObserve")return;
            Application.runInBackground=true;EditorApplication.isPaused=false;
            scenarioBrain=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>();
            if(pendingEntryMode=="PlayPlayerCombat")
            {
                var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerStatus>();player.SetVitalsForValidation(player.MaxHealth,2);
                scenarioBrain.ReceiveDamage(1,player.GetComponent<ParvumTarget>());
                combatStart=Time.time;combatNextFrame=0;combatFrame=0;combatReport=new StringBuilder();
                EditorApplication.update-=PlayerCombatTick;EditorApplication.update+=PlayerCombatTick;
            }
            else if(pendingEntryMode=="PlaySpeakerObserve")
            {
                scenarioRoot=new GameObject("Parvum real AudioSource controlled input");
                firstAttacker=CreateStimulus("Near audio speaker",1.8f,true);
                secondAttacker=CreateStimulus("Far audio speaker",3.5f,true);
                speakerObservationClip=AudioClip.Create("Silent observation input - not production music",48000,1,48000,false);
                foreach(var speaker in new[]{firstAttacker,secondAttacker})
                {
                    var source=speaker.gameObject.AddComponent<AudioSource>();source.clip=speakerObservationClip;source.loop=true;source.playOnAwake=false;
                    speaker.ConfigureSpeaker(source,null);source.Play();
                }
                firstAttacker.GetComponent<AudioSource>().mute=true;
                scenarioStart=Time.time;scenarioStage=0;nextScenarioFrame=0;scenarioFrame=0;scenarioReport=new StringBuilder("Silent clip is a controlled AudioSource input, not installed-item/music acceptance.\n");
                EditorApplication.update-=SpeakerObserveTick;EditorApplication.update+=SpeakerObserveTick;
            }
            else if(pendingEntryMode=="PlayLossObserve")
            {
                scenarioRoot=new GameObject("Parvum sight and faction stimuli");firstAttacker=CreateStimulus("Sight target",1.5f,false);
                scenarioBrain.ReceiveDamage(1,firstAttacker);scenarioStart=Time.time;scenarioStage=0;nextScenarioFrame=0;scenarioFrame=0;scenarioReport=new StringBuilder();
                EditorApplication.update-=LossObserveTick;EditorApplication.update+=LossObserveTick;
            }
            else if(pendingEntryMode=="PlayRouteObserve" || pendingEntryMode=="PlayEntranceObserve" || pendingEntryMode=="PlayContactRetry" || pendingEntryMode=="PlayDoorObserve")
            {
                doorRoute=pendingEntryMode=="PlayDoorObserve";doorOpened=false;
                contactRetry=pendingEntryMode=="PlayContactRetry";
                entranceRoutes=pendingEntryMode=="PlayEntranceObserve" || contactRetry || doorRoute;
                routeMain=scenarioBrain.gameObject;routeMain.SetActive(false);
                routeCases=CollectRoutes();routeIndex=-1;routeReport=new StringBuilder("Temporary same-prefab AI, Rigidbody movement; only initial spawn is placed. Moving speaker is a controlled input, not player acceptance.\n");
                if(contactRetry)routeCases=routeCases.Where(x=>x.name.Contains("H02")).ToList();
                if(doorRoute)
                {
                    var approach=routeCases.First(x=>x.name.Contains("H01") && x.name.EndsWith("forward")).points.Take(12).Reverse().ToArray();
                    routeCases=new List<RouteCase>{new RouteCase{name="H01 closed then opened",points=approach}};
                    var ship=UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
                    ship.SetShipState(ship.CurrentShipState.WithRoom(ShipRoomId.EngineRoom,ship.CurrentShipState.GetRoom(ShipRoomId.EngineRoom).WithSealed(true)));
                }
                routeNextFrame=0;routeFrame=0;BeginNextRoute();
                EditorApplication.update-=RouteObserveTick;EditorApplication.update+=RouteObserveTick;
            }
            else
            {
                var ship=UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
                ship.SetShipState(ship.CurrentShipState.WithRoom(ShipRoomId.CargoHold,new ShipRoomState(500,500)));
                facilityStart=Time.time;facilityNextFrame=0;facilityStage=0;facilityFrame=0;facilityObserved=null;facilityReport=new StringBuilder("Runtime 500-durability condition only; saved room durability unchanged.\n");
                EditorApplication.update-=FacilityObserveTick;EditorApplication.update+=FacilityObserveTick;
            }
        }
        private static float biteSurveyStart,biteSurveyNext;
        private static int biteSurveyFrame;
        private static StringBuilder biteSurveyReport;
        private static void BiteSurveyTick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=BiteSurveyTick;return;}
            if(Time.time<biteSurveyNext)return;biteSurveyNext=Time.time+.05f;
            var actor=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>();
            if(actor.GetComponentInChildren<ParvumAnimationView>().Motion!=ParvumMotion.Attack){biteSurveyStart=Time.time;return;}
            var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();var baked=new Mesh();skin.BakeMesh(baked,true);
            var verts=baked.vertices;var materials=skin.sharedMaterials;
            biteSurveyReport.AppendLine($"t={Time.time-biteSurveyStart:F3} motion={actor.GetComponentInChildren<ParvumAnimationView>().Motion}");
            var localVertices=verts.Select(v=>actor.transform.InverseTransformPoint(skin.transform.TransformPoint(v))).ToArray();
            var tip=localVertices.OrderByDescending(v=>v.z).First();
            biteSurveyReport.AppendLine($"front={tip:F4} min={new Vector3(localVertices.Min(v=>v.x),localVertices.Min(v=>v.y),localVertices.Min(v=>v.z)):F4} max={new Vector3(localVertices.Max(v=>v.x),localVertices.Max(v=>v.y),localVertices.Max(v=>v.z)):F4}");
            for(int sub=0;sub<baked.subMeshCount;sub++)
            {
                if(!materials[sub].name.ToLowerInvariant().Contains("teeth") && !materials[sub].name.ToLowerInvariant().Contains("tooth"))continue;
                var indices=baked.GetIndices(sub);var b=new Bounds(actor.transform.InverseTransformPoint(skin.transform.TransformPoint(verts[indices[0]])),Vector3.zero);
                foreach(int index in indices)b.Encapsulate(actor.transform.InverseTransformPoint(skin.transform.TransformPoint(verts[index])));
                biteSurveyReport.AppendLine($"teeth slot={sub} material={materials[sub].name} center={b.center:F4} size={b.size:F4} min={b.min:F4} max={b.max:F4}");
            }
            UnityEngine.Object.DestroyImmediate(baked);
            Capture(actor.transform,DirectoryPath+"/BiteBefore_"+(biteSurveyFrame++).ToString("D3")+".png");
            File.WriteAllText(DirectoryPath+"/BiteBefore.txt",biteSurveyReport.ToString());
            if(Time.time-biteSurveyStart>=1){EditorApplication.update-=BiteSurveyTick;ConsoleReport("BiteBefore");}
        }
        private static float room500Start,room500Next;
        private static StringBuilder room500Report;
        private static void Room500Tick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=Room500Tick;return;}
            if(Time.time<room500Next)return;room500Next=Time.time+.5f;
            var ship=UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            var actor=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>();
            var room=ship.CurrentShipState.GetRoom(ShipRoomId.CargoHold);
            float elapsed=Time.time-room500Start;
            room500Report.AppendLine($"t={elapsed:F3} cargoHold={room.CurrentDurability}/{room.MaxDurability} state={actor.Behaviour} target={actor.CurrentTarget} animation={actor.GetComponentInChildren<ParvumAnimationView>().Motion}");
            File.WriteAllText(DirectoryPath+"/Room500.txt",room500Report.ToString());
            if(elapsed>=12)
            {
                EditorApplication.update-=Room500Tick;
                if(room.CurrentDurability<=0 || room.CurrentDurability>=500)throw new InvalidOperationException("Expected ongoing feeding with remaining room durability.");
                ConsoleReport("Room500");
                if(File.Exists(DirectoryPath+"/Room500Final.png"))throw new InvalidOperationException("Room500 final capture already exists.");
                Capture(actor.transform,DirectoryPath+"/Room500Final.png");
            }
        }
        private static AudioClip speakerObservationClip;
        private static void SpeakerObserveTick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=SpeakerObserveTick;return;}
            float elapsed=Time.time-(float)scenarioStart;
            if(scenarioStage==0 && elapsed>=.5f){firstAttacker.GetComponent<AudioSource>().mute=false;scenarioStage++;}
            if(Time.time>=nextScenarioFrame)
            {
                nextScenarioFrame=Time.time+.15f;
                scenarioReport.AppendLine($"t={elapsed:F3} state={scenarioBrain.Behaviour} target={scenarioBrain.CurrentTarget} nearAlive={firstAttacker.IsAlive} nearAudible={firstAttacker.Audible} farAlive={secondAttacker.IsAlive} farAudible={secondAttacker.Audible} nearDistance={Vector3.Distance(scenarioBrain.transform.position,firstAttacker.transform.position):F3} farDistance={Vector3.Distance(scenarioBrain.transform.position,secondAttacker.transform.position):F3}");
                Capture(scenarioBrain.transform,DirectoryPath+"/SpeakerAudio_"+(scenarioFrame++).ToString("D3")+".png");
                File.WriteAllText(DirectoryPath+"/SpeakerAudio.txt",scenarioReport.ToString());
            }
            if(elapsed>=9)
            {
                EditorApplication.update-=SpeakerObserveTick;UnityEngine.Object.Destroy(scenarioRoot);UnityEngine.Object.Destroy(speakerObservationClip);ConsoleReport("SpeakerAudio");
            }
        }
        private static void LossObserveTick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=LossObserveTick;return;}
            float elapsed=Time.time-(float)scenarioStart;
            if(scenarioStage==0 && elapsed>=1.5f)
            {
                bool found=false;var eye=scenarioBrain.transform.position+Vector3.up*.2f;
                for(int i=0;i<32 && !found;i++)
                {
                    var direction=new Vector3(Mathf.Cos(i*Mathf.PI/16),0,Mathf.Sin(i*Mathf.PI/16));
                    foreach(var hit in Physics.RaycastAll(eye,direction,7,~0,QueryTriggerInteraction.Ignore).OrderBy(x=>x.distance))
                    {
                        if(hit.transform.IsChildOf(scenarioBrain.transform) || Mathf.Abs(hit.normal.y)>.5f)continue;
                        var hidden=hit.point+direction*.5f;
                        if(Physics.CheckSphere(hidden,.03f,~0,QueryTriggerInteraction.Ignore))continue;
                        firstAttacker.transform.position=hidden;found=true;scenarioReport.AppendLine($"OCCLUSION wall={hit.collider.name} distance={Vector3.Distance(eye,hidden):F3}");break;
                    }
                }
                if(!found){EditorApplication.update-=LossObserveTick;throw new InvalidOperationException("No real wall occlusion stimulus found.");}
                scenarioStage++;
            }
            if(scenarioStage==1 && elapsed>=3)
            {
                secondAttacker=CreateStimulus("Seed collateral then hostile objective",.8f,false);
                secondAttacker.ConfigureCombatTarget(true,1000,0,IntruderFaction.SeedEntity);
                scenarioBrain.ReceiveDamage(1,secondAttacker);scenarioStage++;
            }
            if(scenarioStage==2 && elapsed>=4)
            {secondAttacker.ConfigureCombatTarget(true,1000,0,IntruderFaction.SpacePirate);scenarioBrain.NotifyObjectiveConflict(secondAttacker);scenarioStage++;}
            if(scenarioStage==3 && elapsed>=6)
            {secondAttacker.transform.position=scenarioBrain.transform.position+Vector3.up*11;scenarioStage++;}
            if(Time.time>=nextScenarioFrame)
            {
                nextScenarioFrame=Time.time+.15f;
                scenarioReport.AppendLine($"time={elapsed:F3} stage={scenarioStage} state={scenarioBrain.Behaviour} target={scenarioBrain.CurrentTarget} hp={scenarioBrain.Health:F3}");
                Capture(scenarioBrain.transform,DirectoryPath+"/LossObserve_"+(scenarioFrame++).ToString("D3")+".png");
                File.WriteAllText(DirectoryPath+"/LossObserve.txt",scenarioReport.ToString());
            }
            if(elapsed>=8){EditorApplication.update-=LossObserveTick;UnityEngine.Object.Destroy(scenarioRoot);ConsoleReport("LossObserve");}
        }
        private sealed class RouteCase {public string name;public Vector3[] points;}
        private static List<RouteCase> routeCases;
        private static bool entranceRoutes;
        private static bool doorRoute,doorOpened,contactRetry;
        private static string RoutePrefix => doorRoute?"DoorRoute":contactRetry?"ContactRetry":entranceRoutes?"Entrances":"Routes";
        private static int routeIndex,routeFrame;
        private static float routeStart,routeNextFrame;
        private static GameObject routeMain,routeRoot;
        private static ParvumBrain routeActor;
        private static ParvumTarget routeSound;
        private static StringBuilder routeReport;
        private static List<RouteCase> CollectRoutes()
        {
            var result=new List<RouteCase>();
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects().Where(x=>x.name=="Approved Ship Corridor Segments" || (x.name.StartsWith("Cargo ") && x.name.EndsWith("Corridor and Contacts"))))
            {
                if(root.name=="Approved Ship Corridor Segments")
                foreach(var floor in root.GetComponentsInChildren<BoxCollider>().Where(x=>x.enabled && x.name.Contains("straight floor slab")))
                {
                    var size=Vector3.Scale(floor.size,floor.transform.lossyScale);bool x=Mathf.Abs(size.x)>Mathf.Abs(size.z);
                    Vector3 axis=x?floor.transform.right:floor.transform.forward;float half=Mathf.Abs(x?size.x:size.z)*.5f-.4f;
                    var points=new List<Vector3>{floor.bounds.center-axis*half,floor.bounds.center+axis*half};
                    if(entranceRoutes)
                    {
                        string id=System.Text.RegularExpressions.Regex.Match(floor.name,@"H\d+").Value;
                        var contacts=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>c.enabled && c.name=="FittedFloor" && c.transform.parent.name.EndsWith(id)).ToArray();
                        for(int end=0;end<2;end++)
                        {
                            var endpoint=end==0?points[0]:points[points.Count-1];
                            var outward=end==0?-axis:axis;
                            var contact=contacts.OrderBy(c=>Vector3.Distance(c.bounds.center,endpoint)).FirstOrDefault();
                            var center=contact?contact.bounds.center:endpoint;
                            // H02's control-room contact turns east before the interior partition.
                            if(contact && id=="H02" && contact.transform.parent.name.Contains("ControlRoom"))outward=Vector3.right;
                            var inside=center+Vector3.ProjectOnPlane(outward,Vector3.up).normalized*4f;
                            if(end==0){points.Insert(0,center);points.Insert(0,inside);}else{points.Add(center);points.Add(inside);}
                        }
                    }
                    AddRoute(result,floor.name,points.ToArray());
                }
                else
                {
                    var floors=root.GetComponentsInChildren<MeshCollider>().Where(x=>x.enabled && System.Text.RegularExpressions.Regex.IsMatch(x.name,entranceRoutes?@" (RoomContact|Body18m|WarehouseContact) \d+ 0$":@" Body18m \d+ 0$"))
                        .OrderBy(x=>int.Parse(System.Text.RegularExpressions.Regex.Match(x.name,@" (\d+) 0$").Groups[1].Value)).ToArray();
                    var points=floors.Select(x=>x.bounds.center).ToList();
                    if(entranceRoutes && points.Count>1)
                    {
                        var first=points[0]+Vector3.ProjectOnPlane(points[0]-points[1],Vector3.up).normalized*3f;
                        var last=points[points.Count-1]+Vector3.ProjectOnPlane(points[points.Count-1]-points[points.Count-2],Vector3.up).normalized*3f;
                        points.Insert(0,first);points.Add(last);
                    }
                    AddRoute(result,root.name,points.ToArray());
                }
            }
            return result;
        }
        private static void AddRoute(List<RouteCase> cases,string name,Vector3[] points)
        {
            if(points.Length<2)return;
            if(entranceRoutes)
            {
                var dense=new List<Vector3>{points[0]};
                for(int i=1;i<points.Length;i++)
                {
                    int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(points[i-1],points[i])));
                    for(int step=1;step<=steps;step++)dense.Add(Vector3.Lerp(points[i-1],points[i],(float)step/steps));
                }
                points=dense.ToArray();
            }
            for(int i=0;i<points.Length;i++)
            {
                if(Physics.Raycast(points[i]+Vector3.up,Vector3.down,out var floor,2,~0,QueryTriggerInteraction.Ignore))points[i]=floor.point;
                else throw new InvalidOperationException($"Route point lacks physical floor: {name} #{i} {points[i]}");
            }
            cases.Add(new RouteCase{name=name+" forward",points=points});cases.Add(new RouteCase{name=name+" reverse",points=points.Reverse().ToArray()});
        }
        private static void BeginNextRoute()
        {
            if(routeRoot)UnityEngine.Object.DestroyImmediate(routeRoot);
            routeIndex++;
            if(routeIndex>=routeCases.Count){EditorApplication.update-=RouteObserveTick;routeMain.SetActive(true);ConsoleReport(RoutePrefix);return;}
            var path=routeCases[routeIndex].points;routeRoot=new GameObject("Parvum temporary route observation");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/Parvum/ParvumGameplay.prefab");
            var actor=UnityEngine.Object.Instantiate(prefab,path[0]+Vector3.up*.04f,Quaternion.LookRotation(Vector3.ProjectOnPlane(path[1]-path[0],Vector3.up).normalized),routeRoot.transform);
            routeActor=actor.GetComponent<ParvumBrain>();
            var source=new GameObject("Moving speaker input");source.transform.SetParent(routeRoot.transform);source.transform.position=path[1]+Vector3.up*.2f;
            routeSound=source.AddComponent<ParvumTarget>();routeSound.ConfigureSpeaker(null,null);routeSound.SetSpeakerAudible(true);
            routeStart=Time.time;routeReport.AppendLine("START "+routeCases[routeIndex].name);
        }
        private static void RouteObserveTick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=RouteObserveTick;return;}
            if(routeIndex>=routeCases.Count){EditorApplication.update-=RouteObserveTick;return;}
            var route=routeCases[routeIndex];var points=route.points;
            if(doorRoute && !doorOpened && Time.time-routeStart>=4)
            {
                var ship=UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
                ship.SetShipState(ship.CurrentShipState.WithRoom(ShipRoomId.EngineRoom,ship.CurrentShipState.GetRoom(ShipRoomId.EngineRoom).WithSealed(false)));
                doorOpened=true;routeReport.AppendLine("OPENING after four seconds of closed-door observation");
            }
            int closest=0;float nearest=float.PositiveInfinity;
            for(int i=0;i<points.Length;i++){float d=Vector3.Distance(routeActor.transform.position,points[i]);if(d<nearest){nearest=d;closest=i;}}
            Vector3 goal=points[points.Length-1];
            if(points.Length==2)
            {
                var delta=points[1]-points[0];float t=Mathf.Clamp01(Vector3.Dot(routeActor.transform.position-points[0],delta)/delta.sqrMagnitude+2.5f/delta.magnitude);goal=points[0]+delta*t;
            }
            else
            {
                int ahead=closest;float length=0;while(ahead<points.Length-1 && length<2.5f){length+=Vector3.Distance(points[ahead],points[ahead+1]);ahead++;}goal=points[ahead];
            }
            routeSound.transform.position=goal+Vector3.up*.2f;
            if(Time.time>=routeNextFrame)
            {
                routeNextFrame=Time.time+.5f;
                routeReport.AppendLine($"{route.name} t={Time.time-routeStart:F2} position={routeActor.transform.position:F3} state={routeActor.Behaviour} target={routeActor.CurrentTarget} remaining={Vector3.Distance(routeActor.transform.position,points[points.Length-1]):F3}"+(doorRoute?$" doorOpened={doorOpened} actualH01Open={UnityEngine.Object.FindObjectsByType<PegasusEntranceDoor>(FindObjectsSortMode.None).Where(x=>x.name.Contains("H01")).All(x=>x.IsOpen)}":""));
                Capture(routeActor.transform,DirectoryPath+"/"+RoutePrefix+"_"+routeIndex.ToString("D2")+"_"+(routeFrame++).ToString("D4")+".png");
                File.WriteAllText(DirectoryPath+"/"+RoutePrefix+".txt",routeReport.ToString());
            }
            float remaining=Vector3.Distance(routeActor.transform.position,points[points.Length-1]);
            if(remaining<1.05f || Time.time-routeStart>25)
            {
                routeReport.AppendLine((remaining<1.05f?"REACHED ":"FAILED ")+route.name+" endpoint tolerance=1.05m (bite range); "+(entranceRoutes?"includes contacts and room-side extensions.":"entrance contacts not included."));
                File.WriteAllText(DirectoryPath+"/"+RoutePrefix+".txt",routeReport.ToString());BeginNextRoute();
            }
        }
        private static void FacilityObserveTick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=FacilityObserveTick;return;}
            float elapsed=Time.time-facilityStart;
            if(!facilityObserved && scenarioBrain.CurrentTarget && scenarioBrain.CurrentTarget.Kind==ParvumTargetKind.Facility)facilityObserved=scenarioBrain.CurrentTarget;
            if(facilityStage==0 && elapsed>=2){scenarioBrain.ReceiveDamage(0,null,new CombatStatusEffectApplication(CombatStatusEffectKind.Stopped,3));facilityStage++;}
            if(facilityStage==1 && elapsed>=9){scenarioBrain.ReceiveDamage(0,null,new CombatStatusEffectApplication(CombatStatusEffectKind.Stopped,4));facilityStage++;}
            if(Time.time>=facilityNextFrame)
            {
                facilityNextFrame=Time.time+.1f;
                var ship=UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
                facilityReport.AppendLine($"time={elapsed:F3} animation={scenarioBrain.GetComponentInChildren<ParvumAnimationView>().Motion} accumulated={facilityObserved?.AccumulatedConsumptionSeconds:F3} wound={facilityObserved?.HasDamagePart} durability={ship.CurrentShipState.GetRoom(ShipRoomId.CargoHold).CurrentDurability}");
                if(facilityFrame%5==0)Capture(scenarioBrain.transform,DirectoryPath+"/FacilityObserve_"+facilityFrame.ToString("D3")+".png");
                facilityFrame++;File.WriteAllText(DirectoryPath+"/FacilityObserve.txt",facilityReport.ToString());
            }
            if(elapsed>=15){EditorApplication.update-=FacilityObserveTick;ConsoleReport("FacilityObserve");}
        }
        private static float combatStart,combatNextFrame;
        private static int combatStage,combatFrame;
        private static ParvumTarget combatTarget;
        private static StringBuilder combatReport;
        private static void PlayerCombatTick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=PlayerCombatTick;return;}
            if(Time.time<combatNextFrame)return;combatNextFrame=Time.time+.1f;
            var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerStatus>();
            float elapsed=Time.time-combatStart;
            combatReport.AppendLine($"t={elapsed:F3} state={scenarioBrain.Behaviour} position={scenarioBrain.transform.position:F3} HP={player.CurrentHealth} shield={player.CurrentShield} movement={player.MovementMultiplier:F3}");
            if(combatFrame%3==0)Capture(scenarioBrain.transform,DirectoryPath+"/PlayerCombat_"+combatFrame.ToString("D3")+".png");
            combatFrame++;File.WriteAllText(DirectoryPath+"/PlayerCombat.txt",combatReport.ToString());
            if(elapsed>=6){EditorApplication.update-=PlayerCombatTick;ConsoleReport("PlayerCombat");}
        }
        private static void CombatObserveTick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=CombatObserveTick;return;}
            float elapsed=Time.time-combatStart;int stage=Mathf.Min(4,(int)(elapsed/3));
            if(stage!=combatStage)
            {
                if(combatTarget)UnityEngine.Object.DestroyImmediate(combatTarget.gameObject);
                combatTarget=CreateStimulus("Combat stimulus "+stage,.65f,false);
                bool bio=stage==0 || stage==1 || stage==4;
                combatTarget.ConfigureCombatTarget(bio,100,stage==0 || stage==3 ? 20 : 0);
                if(stage==4)scenarioBrain.SetUrzereSupport(scenarioRoot,true);
                scenarioBrain.ReceiveDamage(1,combatTarget);combatStage=stage;
            }
            if(Time.time>=combatNextFrame)
            {
                combatNextFrame=Time.time+.1f;
                combatReport.AppendLine($"t={elapsed:F3} stage={stage} state={scenarioBrain.Behaviour} target={scenarioBrain.CurrentTarget} targetHP={combatTarget.Health:F3} shield={combatTarget.Shield:F3} slow={combatTarget.SlowRemaining:F3} multiplier={combatTarget.MovementMultiplier:F3} actorHP={scenarioBrain.Health:F3} speed={scenarioBrain.MovementSpeed:F3} buff={scenarioBrain.IsUrzereSupported}");
                if(combatFrame%3==0)Capture(scenarioBrain.transform,DirectoryPath+"/CombatObserve_"+combatFrame.ToString("D3")+".png");
                combatFrame++;File.WriteAllText(DirectoryPath+"/CombatObserve.txt",combatReport.ToString());
            }
            if(elapsed>=15)
            {EditorApplication.update-=CombatObserveTick;scenarioBrain.SetUrzereSupport(scenarioRoot,false);UnityEngine.Object.Destroy(scenarioRoot);ConsoleReport("CombatObserve");}
        }
        private static float cargoObservationStart,cargoNextFrame;
        private static float cargoFirstBiteTime;
        private static int cargoBiteCount;
        private static int cargoFrame;
        private static StringBuilder cargoObservationLog;
        private static void CargoObserveTick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=CargoObserveTick;return;}
            if(Time.time<cargoNextFrame)return;
            cargoNextFrame=Time.time+2;
            var actor=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>();
            var ship=UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            var cargo=UnityEngine.Object.FindObjectsByType<ParvumTarget>(FindObjectsSortMode.None).Single(x=>x.Kind==ParvumTargetKind.MetalCargo);
            float elapsed=Time.time-cargoObservationStart;
            var contact=cargo.ClosestPoint(actor.transform.position+Vector3.up*.2f);
            var diagnostics=new StringBuilder();
            var args=new object[]{cargo,Vector3.zero,(Action<string>)(line=>diagnostics.AppendLine(line))};
            bool accessible=(bool)typeof(ParvumBrain).GetMethod("CanApproach",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(actor,args);
            cargoObservationLog.AppendLine($"time={elapsed:F3} state={actor.Behaviour} target={actor.CurrentTarget} position={actor.transform.position:F4} cargo={ship.CurrentCargoState.DurabilityPercent:R} metal={cargo.IsMetal} contact={contact:F4} distance={Vector3.Distance(contact,actor.transform.position):F3} accessible={accessible}");
            cargoObservationLog.Append(diagnostics);
            Capture(actor.transform,DirectoryPath+"/"+observationLabel+"_"+(cargoFrame++).ToString("D3")+".png");
            File.WriteAllText(DirectoryPath+"/"+observationLabel+".txt",cargoObservationLog.ToString());
            if(elapsed>=85 || ship.CurrentCargoState.DurabilityPercent<=0)
            {EditorApplication.update-=CargoObserveTick;ConsoleReport("CargoObserve");}
        }
        private static void RebuildNavigation()
        {
            var names=new[]{"Approved Engine Room 01 Shell","Approved Cockpit 01 Structure","Approved Control Room 01 Shell","Approved Armory 01 Shell","Approved Supply Room 01 Shell","Approved Cargo Hold 01 Shell","Approved Ship Corridor Segments","Intermediate Connectors","Armory Supply Intermediate Connectors"};
            var sources=new List<NavMeshBuildSource>();var marks=new List<NavMeshBuildMarkup>();
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects().Where(x=>names.Contains(x.name)||(x.name.StartsWith("Cargo ")&&x.name.EndsWith("Corridor and Contacts"))))
            {var part=new List<NavMeshBuildSource>();NavMeshBuilder.CollectSources(root.transform,~0,NavMeshCollectGeometry.RenderMeshes,0,marks,part);sources.AddRange(part);}
            var settings=NavMesh.GetSettingsByIndex(0);
            var agentPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/Parvum/ParvumGameplay.prefab");var agentCapsule=agentPrefab.GetComponent<CapsuleCollider>();
            settings.agentRadius=agentCapsule.radius*agentPrefab.transform.localScale.x;settings.agentHeight=agentCapsule.height*agentPrefab.transform.localScale.y;
            settings.agentClimb=ParvumGameplayRules.MaximumStepHeight;settings.agentSlope=60;settings.overrideVoxelSize=true;settings.voxelSize=.055f;
            var data=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(new Vector3(60,0,10),new Vector3(100,40,110)),Vector3.zero,Quaternion.identity);
            var asset=AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/_Project/Navigation/PegasusParvum/ParvumNavMesh.asset");
            EditorUtility.CopySerialized(data,asset);asset.name="ParvumNavMesh";UnityEngine.Object.DestroyImmediate(data);EditorUtility.SetDirty(asset);AssetDatabase.SaveAssets();
            File.WriteAllText(DirectoryPath+"/Navigation.txt",$"Visible interior geometry sources={sources.Count}; no exterior/hidden legacy corridors; no geometry changed.");
        }
        private static GameObject scenarioRoot;
        private static ParvumTarget firstAttacker,secondAttacker,soundTarget;
        private static double scenarioStart,nextScenarioFrame;
        private static int scenarioStage,scenarioFrame;
        private static StringBuilder scenarioReport;
        private static ParvumTarget CreateStimulus(string name,float distance,bool speaker)
        {
            var go=new GameObject(name);go.transform.SetParent(scenarioRoot.transform);
            Vector3 origin=scenarioBrain.transform.position;
            Vector3 point=origin;
            bool found=false;
            var route=new NavMeshPath();
            if(!NavMesh.SamplePosition(origin,out var start,.35f,NavMesh.AllAreas))throw new InvalidOperationException("Stimulus origin has no navigation surface.");
            for(int i=0;i<16;i++)
            {
                var candidate=origin+new Vector3(Mathf.Cos(i*Mathf.PI/8),0,Mathf.Sin(i*Mathf.PI/8))*distance;
                if(NavMesh.SamplePosition(candidate,out var nav,.5f,NavMesh.AllAreas) &&
                   NavMesh.CalculatePath(start.position,nav.position,NavMesh.AllAreas,route) && route.status==NavMeshPathStatus.PathComplete &&
                   Physics.Raycast(nav.position+Vector3.up,Vector3.down,out var floor,2,~0,QueryTriggerInteraction.Ignore) && Mathf.Abs(floor.point.y-nav.position.y)<.12f &&
                   !Physics.Linecast(origin+Vector3.up*.25f,nav.position+Vector3.up*.25f,~0,QueryTriggerInteraction.Ignore)) {point=floor.point;found=true;break;}
            }
            if(!found){UnityEngine.Object.Destroy(go);throw new InvalidOperationException("No reachable physical-floor stimulus position: "+name);}
            go.transform.position=point+Vector3.up*.2f;
            var target=go.AddComponent<ParvumTarget>();
            if(speaker)target.ConfigureSpeaker(null,null);else target.ConfigureCombatTarget(true,1000,0);
            return target;
        }
        private static void ScenarioTick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=ScenarioTick;return;}
            double now=EditorApplication.timeSinceStartup,elapsed=now-scenarioStart;
            if(scenarioStage==0){scenarioBrain.ReceiveDamage(1,firstAttacker);scenarioStage++;}
            if(scenarioStage==1 && elapsed>.3){scenarioBrain.ReceiveDamage(1,secondAttacker);scenarioStage++;}
            if(scenarioStage==2 && elapsed>1){soundTarget.SetSpeakerAudible(true);scenarioStage++;}
            if(scenarioStage==3 && scenarioBrain.Behaviour==ParvumBehaviour.ApproachSpeaker)
            {scenarioBrain.ReceiveDamage(1,secondAttacker);scenarioStage++;}
            if(scenarioStage==4 && elapsed>3){secondAttacker.transform.position=scenarioBrain.transform.position+Vector3.up*11;scenarioStage++;}
            if(scenarioStage==5 && elapsed>4){soundTarget.SetSpeakerAudible(false);scenarioStage++;}
            if(elapsed>5 && scenarioStage<7){scenarioBrain.ReceiveDamage(1000,null);scenarioStage=7;}
            if(now>=nextScenarioFrame)
            {
                nextScenarioFrame=now+.15;
                scenarioReport.AppendLine($"t={elapsed:F3} gameTime={Time.time:F3} paused={EditorApplication.isPaused} stage={scenarioStage} state={scenarioBrain.Behaviour} target={scenarioBrain.CurrentTarget} targetPosition={(scenarioBrain.CurrentTarget?scenarioBrain.CurrentTarget.transform.position.ToString("F3"):"none")} position={scenarioBrain.transform.position:F4} health={scenarioBrain.Health}");
                Capture(scenarioBrain.transform,DirectoryPath+"/Scenario_"+(scenarioFrame++).ToString("D3")+".png");
                File.WriteAllText(DirectoryPath+"/Scenario.txt",scenarioReport.ToString());
            }
            if(elapsed>8.5)
            {EditorApplication.update-=ScenarioTick;UnityEngine.Object.Destroy(scenarioRoot);ConsoleReport("Scenario");}
        }
        private static double observationStart,nextFrame;
        private static int frameNumber;
        private static string observationLabel;
        private static StringBuilder observationLog;
        private static void MotionTick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=MotionTick;return;}
            var now=EditorApplication.timeSinceStartup;if(now<nextFrame)return;nextFrame=now+.2;
            var root=GameObject.Find("Parvum temporary motion comparisons");
            foreach(Transform item in root.transform)Capture(item,DirectoryPath+"/"+item.name+"_"+frameNumber.ToString("D3")+".png");
            frameNumber++;
            if(now-observationStart>3.4){EditorApplication.update-=MotionTick;UnityEngine.Object.Destroy(root);ConsoleReport("MotionComparison");}
        }
        private static void ObserveTick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=ObserveTick;return;}
            var now=EditorApplication.timeSinceStartup;
            if(now<nextFrame)return;
            nextFrame=now+.2;
            var brain=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>();
            Capture(brain.transform,DirectoryPath+"/"+observationLabel+"_"+(frameNumber++).ToString("D3")+".png");
            observationLog.AppendLine($"time={now-observationStart:F3} position={brain.transform.position:F4} state={brain.Behaviour} target={brain.CurrentTarget} health={brain.Health}");
            File.WriteAllText(DirectoryPath+"/"+observationLabel+".txt",observationLog.ToString());
            if(now-observationStart>=10){EditorApplication.update-=ObserveTick;ConsoleReport(observationLabel);}
        }
        private static void ConsoleReport(string label)
        {
            var flags=System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;
            var logs=typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries");
            var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntry");var entry=Activator.CreateInstance(type);
            var report=new StringBuilder($"Compiling={EditorApplication.isCompiling}\n");
            logs.GetMethod("StartGettingEntries",flags).Invoke(null,null);
            try
            {
                int count=(int)logs.GetMethod("GetCount",flags).Invoke(null,null);report.AppendLine("Entries="+count);
                for(int i=0;i<count;i++)
                {logs.GetMethod("GetEntryInternal",flags).Invoke(null,new[]{(object)i,entry});report.AppendLine(type.GetField("message",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic)?.GetValue(entry)?.ToString());}
            }
            finally{logs.GetMethod("EndGettingEntries",flags).Invoke(null,null);}
            File.WriteAllText(DirectoryPath+"/Console_"+label+".txt",report.ToString());
        }
        internal static void Apply()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying || scene.name!="Pegasus") throw new InvalidOperationException("Pegasus edit mode required.");
            if(scene.GetRootGameObjects().Any(x=>x.name=="ParvumGameplay")) throw new InvalidOperationException("Gameplay root already exists; inspect before updating.");
            Directory.CreateDirectory(DirectoryPath);
            EditorSceneManager.SaveScene(scene,DirectoryPath+"/Before.unity",true);
            var source=EditorSceneManager.OpenScene(SourcePath,OpenSceneMode.Additive);
            GameObject visual=null;
            var motions=new ParvumAnimationView.MotionSource[5];
            try
            {
                var root=source.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).First(x=>x.name=="Approved Parvum Enemy Placement");
                var names=new[]{"Parvum_01_Idle","Parvum_02_Move","Parvum_03_Attack","Parvum_04_Hit","Parvum_05_Death"};
                for(int i=0;i<5;i++)
                {
                    var entity=root.Find(names[i]);
                    motions[i]=new ParvumAnimationView.MotionSource{mesh=entity.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh,clip=entity.GetComponent<Animator>().runtimeAnimatorController.animationClips.Distinct().Single()};
                }
                visual=UnityEngine.Object.Instantiate(root.Find(names[0]).gameObject);
                visual.SetActive(false);SceneManager.MoveGameObjectToScene(visual,scene);
            }
            finally {SceneManager.SetActiveScene(scene);EditorSceneManager.CloseScene(source,true);}
            var gameplay=new GameObject("ParvumGameplay");
            var entityRoot=new GameObject("Parvum Gameplay 01");entityRoot.transform.SetParent(gameplay.transform);
            visual.name="Visual";visual.transform.SetParent(entityRoot.transform,false);
            visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;visual.transform.localScale=Vector3.one;
            foreach(var component in visual.GetComponentsInChildren<Component>(true).Reverse())
            {
                if(component is Transform || component is Renderer || component is MeshFilter || component is Animator)continue;
                UnityEngine.Object.DestroyImmediate(component);
            }
            var renderer=visual.GetComponentInChildren<SkinnedMeshRenderer>();
            var baked=new Mesh();renderer.BakeMesh(baked);
            var vertices=baked.vertices.Select(v=>visual.transform.InverseTransformPoint(renderer.transform.TransformPoint(v))).ToArray();
            var bounds=new Bounds(vertices[0],Vector3.zero);foreach(var v in vertices)bounds.Encapsulate(v);
            UnityEngine.Object.DestroyImmediate(baked);
            float scale=.4f/bounds.size.y;
            visual.transform.localScale=Vector3.one*scale;
            visual.transform.localPosition=new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z)*scale;
            var animator=visual.GetComponent<Animator>();animator.runtimeAnimatorController=null;
            var view=visual.AddComponent<ParvumAnimationView>();view.Configure(animator,renderer,visual.transform.Find("Parvum_Model"),motions);
            visual.SetActive(true);
            var rb=entityRoot.AddComponent<Rigidbody>();rb.mass=3;rb.constraints=RigidbodyConstraints.FreezeRotation;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            var collider=entityRoot.AddComponent<CapsuleCollider>();collider.radius=.175f;collider.height=.4f;collider.center=Vector3.up*.2f;

            // Bake only approved interior collision geometry, never exterior display art or the player.
            var sources=new List<NavMeshBuildSource>();var marks=new List<NavMeshBuildMarkup>();
            var interior=scene.GetRootGameObjects().Where(x=>x.name.StartsWith("Approved ") || x.name=="Intermediate Connectors" || x.name=="Armory Supply Intermediate Connectors" || (x.name.StartsWith("Cargo ") && x.name.EndsWith("Corridor and Contacts"))).ToArray();
            foreach(var root in interior)
            {
                var part=new List<NavMeshBuildSource>();NavMeshBuilder.CollectSources(root.transform,~0,NavMeshCollectGeometry.PhysicsColliders,0,marks,part);sources.AddRange(part);
            }
            var settings=NavMesh.GetSettingsByIndex(0);settings.agentRadius=.175f;settings.agentHeight=.4f;settings.agentClimb=.15f;settings.agentSlope=60;
            settings.overrideVoxelSize=true;settings.voxelSize=.055f;
            var data=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(new Vector3(60,0,10),new Vector3(100,40,110)),Vector3.zero,Quaternion.identity);
            if(!data)throw new InvalidOperationException("Navigation data creation failed.");
            const string navDir="Assets/_Project/Navigation/PegasusParvum";
            Directory.CreateDirectory(navDir);AssetDatabase.Refresh();AssetDatabase.CreateAsset(data,navDir+"/ParvumNavMesh.asset");
            gameplay.AddComponent<ParvumNavigation>().Configure(data);
            var nav=NavMesh.AddNavMeshData(data);
            try
            {
                if(!NavMesh.SamplePosition(new Vector3(54,-4.1f,5),out var spawn,2,NavMesh.AllAreas))throw new InvalidOperationException("No navigable cargo spawn.");
                entityRoot.transform.position=spawn.position+Vector3.up*.03f;
            }
            finally {nav.Remove();}
            entityRoot.AddComponent<ParvumBrain>().Configure(view,settings.agentTypeID);
            var state=UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            if(!state)throw new InvalidOperationException("Ship state missing.");
            var roomNames=new Dictionary<string,ShipRoomId>{{"Approved Engine Room 01 Shell",ShipRoomId.EngineRoom},{"Approved Cockpit 01 Structure",ShipRoomId.Cockpit},{"Approved Control Room 01 Shell",ShipRoomId.ControlRoom},{"Approved Armory 01 Shell",ShipRoomId.Armory},{"Approved Supply Room 01 Shell",ShipRoomId.SupplyRoom},{"Approved Cargo Hold 01 Shell",ShipRoomId.CargoHold}};
            int targetCount=0;
            foreach(var room in interior.Where(x=>roomNames.ContainsKey(x.name)))
                foreach(var surface in room.GetComponentsInChildren<Collider>().Where(x=>x.enabled && !x.isTrigger && IsRoomWall(x.name)))
                {surface.gameObject.AddComponent<ParvumTarget>().ConfigureFacility(surface,state,roomNames[room.name]);targetCount++;}
            var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerStatus>();
            player.gameObject.AddComponent<ParvumTarget>().ConfigurePlayer(player);
            PrefabUtility.SaveAsPrefabAsset(entityRoot,"Assets/_Project/Prefabs/Enemies/Parvum/ParvumGameplay.prefab");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText(DirectoryPath+"/Applied.txt",$"Source=CargoRunMvp actual instances; scale={scale}; spawn={entityRoot.transform.position}; facilities={targetCount}; navigationSources={sources.Count}; NOT VERIFIED");
        }
        private static bool IsRoomWall(string name)
        {
            return name.StartsWith("ER-01 smooth interior pressure wall liner lower sealed section ") ||
                name.StartsWith("left bay outer wall segment ") || name.StartsWith("right bay outer wall segment ") ||
                name.StartsWith("rear bay wall ") ||
                name=="CR-01 north solid future screen wall shell" ||
                name.StartsWith("CR-01 south attached cargo and weapon sealed ") ||
                name.StartsWith("CR-01 west wall separated sealed ") ||
                name=="CR-01 east solid control room wall with no corridor" ||
                name.StartsWith("CR-01 internal partition left wall ") || name.StartsWith("CR-01 internal partition right wall ") ||
                name=="AR-01 solid forward curved-screen wall" || name=="AR-01 west sealed armory wall" ||
                name.StartsWith("AR-07 AR-09 east adjacent supply and cargo sealed ") ||
                name=="AR-08 south control room left sealed wall" || name=="AR-08 south control room right sealed wall" ||
                name=="SR-02 north supply storage wall shell" || name=="SR-05 south ejection bay wall shell" ||
                name=="SR-01 west empty wall shell" || name.StartsWith("SR-09 SR-10 east shared corridor wall ") ||
                (name.StartsWith("Warehouse wall ") && name.Contains(" solid"));
        }
        private static ParvumBrain reachOriginal,reachActor;
        private static string reachLabel;
        private static ParvumTarget reachTarget;
        private static int reachStage,reachFrame;
        private static float reachStart,reachNext;
        private static StringBuilder reachReport;
        private static void ReachTick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=ReachTick;return;}
            if(Time.time-reachStart>=3)
            {
                if(reachActor)UnityEngine.Object.DestroyImmediate(reachActor.gameObject);
                if(reachTarget)UnityEngine.Object.DestroyImmediate(reachTarget.gameObject);
                reachStage++;reachStart=Time.time;reachFrame=0;
                if(reachStage>=6){reachOriginal.gameObject.SetActive(true);EditorApplication.update-=ReachTick;ConsoleReport(reachLabel);return;}
                if(!NavMesh.SamplePosition(new Vector3(51.5f,-4.5f,4.1f),out var spawn,.7f,NavMesh.AllAreas))throw new InvalidOperationException("No contact observation start.");
                var copy=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/Parvum/ParvumGameplay.prefab"));
                copy.name="Parvum reach observation";reachActor=copy.GetComponent<ParvumBrain>();
                copy.GetComponent<Rigidbody>().position=spawn.position+Vector3.up*.02f;copy.GetComponent<Rigidbody>().rotation=Quaternion.LookRotation(Vector3.right);
                var stimulus=GameObject.CreatePrimitive(PrimitiveType.Cube);stimulus.name="Temporary bite contact stimulus";stimulus.transform.localScale=new Vector3(.4f,1.4f,.4f);
                stimulus.transform.position=spawn.position+Vector3.up*.7f+Vector3.right*(reachStage==0?3f:reachStage==1?-2f:2f);
                if(reachStage==2)
                {
                    var wall=ParvumTarget.Active.First(x=>x.name=="Warehouse wall 0 solid end");
                    var probe=spawn.position+Vector3.up*.2f;var onWall=wall.ClosestPoint(probe);var away=(probe-onWall).normalized;
                    stimulus.transform.position=onWall-away*.5f;
                }
                Physics.SyncTransforms();reachTarget=stimulus.AddComponent<ParvumTarget>();reachTarget.ConfigureCombatTarget(true,100,0);
                if(reachStage==4){reachTarget.ConfigureSpeaker(null,stimulus.GetComponent<Collider>());reachTarget.SetSpeakerAudible(true);}
                reachTarget.Bitten+=(actor,damage)=>reachReport.AppendLine($"HIT stage={reachStage} time={Time.time-reachStart:F4} range={Vector3.Distance(actor.transform.position,reachTarget.ClosestPoint(actor.transform.position)):F4} phase={actor.GetComponentInChildren<ParvumAnimationView>().IsBiteContactPhase} damage={damage}");
                if(reachStage!=4)reachActor.ReceiveDamage(reachStage==5?1000:1,reachTarget);
            }
            if(Time.time<reachNext || !reachActor)return;reachNext=Time.time+.04f;
            reachReport.AppendLine($"stage={reachStage} t={Time.time-reachStart:F4} range={Vector3.Distance(reachActor.transform.position,reachTarget.ClosestPoint(reachActor.transform.position)):F4} hp={reachTarget.Health} state={reachActor.Behaviour} facing={Vector3.Dot(reachActor.transform.forward,(reachTarget.transform.position-reachActor.transform.position).normalized):F3}");
            if(reachFrame%6==0)Capture(reachActor.transform,DirectoryPath+"/"+reachLabel+"_"+reachStage+"_"+reachFrame.ToString("D3")+".png");
            reachFrame++;File.WriteAllText(DirectoryPath+"/"+reachLabel+".txt",reachReport.ToString());
        }
        private static void ApplyWallTargets()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying || scene.name!="Pegasus")throw new InvalidOperationException("Pegasus edit required.");
            var names=new Dictionary<string,ShipRoomId>{{"Approved Engine Room 01 Shell",ShipRoomId.EngineRoom},{"Approved Cockpit 01 Structure",ShipRoomId.Cockpit},{"Approved Control Room 01 Shell",ShipRoomId.ControlRoom},{"Approved Armory 01 Shell",ShipRoomId.Armory},{"Approved Supply Room 01 Shell",ShipRoomId.SupplyRoom},{"Approved Cargo Hold 01 Shell",ShipRoomId.CargoHold}};
            var state=UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();var report=new StringBuilder();
            foreach(var root in scene.GetRootGameObjects().Where(x=>names.ContainsKey(x.name)))
            {
                foreach(var old in root.GetComponentsInChildren<ParvumTarget>(true).Where(x=>x.Kind==ParvumTargetKind.Facility).ToArray())UnityEngine.Object.DestroyImmediate(old);
                foreach(var collider in root.GetComponentsInChildren<Collider>().Where(x=>x.enabled && !x.isTrigger && IsRoomWall(x.name)))
                {collider.gameObject.AddComponent<ParvumTarget>().ConfigureFacility(collider,state,names[root.name]);report.AppendLine(root.name+"/"+collider.name);}
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText(DirectoryPath+"/WallTargetsApplied.txt",report.ToString());
        }
        private static ParvumBrain biteSizeActor;
        private static string biteSizeLabel;
        private static float biteSizeStart,biteSizeNext;
        private static int biteSizeFrame;
        private static StringBuilder biteSizeReport;
        private static void BiteSizeTick()
        {
            if(!EditorApplication.isPlaying || !biteSizeActor){EditorApplication.update-=BiteSizeTick;return;}
            if(Time.time<biteSizeNext)return;biteSizeNext=Time.time+.07f;
            var view=biteSizeActor.GetComponentInChildren<ParvumAnimationView>();var food=biteSizeActor.CurrentTarget;
            var body=biteSizeActor.GetComponent<CapsuleCollider>();
            biteSizeReport.AppendLine($"t={Time.time-biteSizeStart:F4} pos={biteSizeActor.transform.position:F4} state={biteSizeActor.Behaviour} target={food?.name} motion={view.Motion} cycle={view.BiteCycle} phase={view.IsBiteContactPhase} mouth={view.MouthTip:F4} localMouth={biteSizeActor.transform.InverseTransformPoint(view.MouthTip):F4} gap={(food?Vector3.Distance(view.MouthTip,food.ClosestPoint(view.MouthTip)):-1):F4} capsule={body.bounds.size:F4}");
            if(biteSizeFrame%3==0)Capture(biteSizeActor.transform,DirectoryPath+"/"+biteSizeLabel+"_"+biteSizeFrame.ToString("D4")+".png");
            biteSizeFrame++;File.WriteAllText(DirectoryPath+"/"+biteSizeLabel+".txt",biteSizeReport.ToString());
            if(Time.time-biteSizeStart>15){EditorApplication.update-=BiteSizeTick;ConsoleReport(biteSizeLabel);}
        }
        private static float wallObserveStart,wallObserveNext;
        private static string relocationLabel;
        private static float relocationStart,relocationNext;
        private static int relocationFrame;
        private static StringBuilder relocationReport;
        private static ParvumTarget entryStimulus;
        private static bool entryStimulusSent;
        private static float entryStimulusAt;
        private static void RelocationTick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=RelocationTick;return;}
            if(Time.time<relocationNext)return;relocationNext=Time.time+(relocationLabel.StartsWith("EntryAdvance")?.08f:.5f);
            var actor=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>();
            var ship=UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            if(relocationLabel.StartsWith("EntryAdvance"))relocationReport.AppendLine($"ENTRY pending={actor.EntryPending} cleared={actor.EntryCleared} start={actor.EntryStart:F4} advance={actor.EntryAdvance:F4} door={actor.EntryDoorName} plane={actor.EntryDoorPoint:F4} inward={actor.EntryInward:F4}");
            if(relocationLabel.Contains("Interrupt") && !entryStimulusSent && actor.EntryPending && actor.EntryAdvance>.15f && actor.EntryAdvance<.9f)
            {
                entryStimulusSent=true;entryStimulusAt=Time.time;
                var stimulus=GameObject.CreatePrimitive(PrimitiveType.Cube);stimulus.name="Relocation runtime Entry stimulus";
                stimulus.transform.position=actor.transform.position+actor.transform.forward*1.4f+Vector3.up*.3f;stimulus.transform.localScale=new Vector3(.3f,.6f,.3f);
                var collider=stimulus.GetComponent<Collider>();collider.isTrigger=true;
                entryStimulus=stimulus.AddComponent<ParvumTarget>();entryStimulus.ConfigureCombatTarget(true,6,0,IntruderFaction.None);
                if(relocationLabel.Contains("Speaker")){entryStimulus.ConfigureSpeaker(null,collider);entryStimulus.SetSpeakerAudible(true);}
                else actor.ReceiveDamage(1,entryStimulus);
                relocationReport.AppendLine("ENTRY STIMULUS at="+Time.time);
            }
            if(entryStimulus && Time.time-entryStimulusAt>2){UnityEngine.Object.Destroy(entryStimulus.gameObject);relocationReport.AppendLine("ENTRY STIMULUS removed at="+Time.time);}
            relocationReport.AppendLine($"t={Time.time-relocationStart:F2} position={actor.transform.position:F3} room={actor.OccupiedRoom} destination={actor.DestinationRoom} state={actor.Behaviour} target={actor.CurrentTarget?.name} motion={actor.GetComponentInChildren<ParvumAnimationView>().Motion} durability="+string.Join(",",Enum.GetValues(typeof(ShipRoomId)).Cast<ShipRoomId>().Select(id=>id+":"+ship.CurrentShipState.GetRoom(id).CurrentDurability)));
            if(actor.CurrentTarget && actor.CurrentTarget.name.StartsWith("Relocation runtime"))
            {var mouth=actor.GetComponentInChildren<ParvumAnimationView>().MouthTip;relocationReport.AppendLine($"STIMULUS health={actor.CurrentTarget.Health} mouth={mouth:F4} point={actor.CurrentTarget.ClosestPoint(mouth):F4} bounds={actor.CurrentTarget.Surface.bounds}");}
            if(relocationFrame%4==0 || (relocationLabel.StartsWith("EntryAdvance") && actor.Behaviour==ParvumBehaviour.AdvanceIntoRoom))Capture(actor.transform,DirectoryPath+"/"+relocationLabel+"_"+relocationFrame.ToString("D4")+".png");
            if(relocationLabel.Contains("Door") && actor.EntryDoorName!=null && relocationFrame%2==0)
            {
                var doorway=actor.EntryDoorPoint;doorway.y=actor.transform.position.y+.8f;
                var side=Vector3.Cross(Vector3.up,actor.EntryInward);
                CaptureDoorContext(doorway+actor.EntryInward*4.5f+side*1.6f+Vector3.up*.8f,doorway,DirectoryPath+"/"+relocationLabel+"_Door_"+relocationFrame.ToString("D4")+".png");
            }
            relocationFrame++;File.WriteAllText(DirectoryPath+"/"+relocationLabel+".txt",relocationReport.ToString());
            if(Time.time-relocationStart>120){EditorApplication.update-=RelocationTick;ConsoleReport(relocationLabel);}
        }
        private static ParvumBrain originalWallActor,wallRoomActor;
        private static string wallRoomLabel;
        private static int wallRoomIndex,wallRoomFrame,wallRoomEnd;
        private static float wallRoomStart,wallRoomNext;
        private static StringBuilder wallRoomReport;
        private static readonly string[] WallRoomNames={"Approved Engine Room 01 Shell","Approved Cockpit 01 Structure","Approved Control Room 01 Shell","Approved Armory 01 Shell","Approved Supply Room 01 Shell","Approved Cargo Hold 01 Shell"};
        private static void WallRoomTick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=WallRoomTick;return;}
            if(Time.time-wallRoomStart>=(wallRoomIndex==0?20:8))
            {
                if(wallRoomActor)UnityEngine.Object.DestroyImmediate(wallRoomActor.gameObject);
                wallRoomIndex++;wallRoomStart=Time.time;wallRoomFrame=0;
                if(wallRoomIndex==wallRoomEnd)
                {originalWallActor.gameObject.SetActive(true);EditorApplication.update-=WallRoomTick;ConsoleReport("WallRooms");return;}
                var root=SceneManager.GetActiveScene().GetRootGameObjects().Single(x=>x.name==WallRoomNames[wallRoomIndex]);
                var targets=root.GetComponentsInChildren<ParvumTarget>().Where(x=>x.Kind==ParvumTargetKind.Facility).ToArray();
                foreach(var food in targets)food.Bitten+=(actor,damage)=>wallRoomReport.AppendLine($"CONTACT time={Time.time:F4} room={root.name} target={food.name} damage={damage} mouth={actor.GetComponentInChildren<ParvumAnimationView>().MouthTip:F4}");
                var center=targets.Select(x=>x.Surface.bounds.center).Aggregate(Vector3.zero,(a,b)=>a+b)/targets.Length;
                bool placed=false;Vector3 start=default;ParvumTarget selected=null;
                foreach(var candidate in targets)
                {
                    var probe=center;probe.y=candidate.Surface.bounds.min.y+.5f;
                    var point=candidate.ClosestPoint(probe);var away=probe-point;away.y=0;
                    if(away.sqrMagnitude<.01f)continue;
                    if(!NavMesh.SamplePosition(point+away.normalized*1.2f,out var spot,1.5f,NavMesh.AllAreas))continue;
                    if(Vector3.Distance(spot.position,point)>2.5f)continue;
                    start=spot.position+Vector3.up*.03f;selected=candidate;placed=true;break;
                }
                if(!placed){wallRoomReport.AppendLine("NO START "+root.name);wallRoomStart=Time.time-8;return;}
                var copy=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/Parvum/ParvumGameplay.prefab"));
                copy.name="Parvum wall room observation";wallRoomActor=copy.GetComponent<ParvumBrain>();
                copy.GetComponent<Rigidbody>().position=start;
                wallRoomReport.AppendLine("ROOM "+root.name+" initial="+start+" reference="+selected.name);
            }
            if(Time.time<wallRoomNext || !wallRoomActor)return;wallRoomNext=Time.time+.08f;
            var view=wallRoomActor.GetComponentInChildren<ParvumAnimationView>();var target=wallRoomActor.CurrentTarget;
            var ship=UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            wallRoomReport.AppendLine($"room={wallRoomIndex} t={Time.time-wallRoomStart:F3} position={wallRoomActor.transform.position:F3} state={wallRoomActor.Behaviour} target={target?.name} cycle={view.BiteCycle} gap={(target?Vector3.Distance(view.MouthTip,target.ClosestPoint(view.MouthTip)):-1):F4} durability="+string.Join(",",Enum.GetValues(typeof(ShipRoomId)).Cast<ShipRoomId>().Select(id=>id+":"+ship.CurrentShipState.GetRoom(id).CurrentDurability)));
            if(wallRoomFrame%3==0)Capture(wallRoomActor.transform,DirectoryPath+"/"+wallRoomLabel+"_"+wallRoomIndex+"_"+wallRoomFrame.ToString("D3")+".png");
            wallRoomFrame++;File.WriteAllText(DirectoryPath+"/"+wallRoomLabel+".txt",wallRoomReport.ToString());
        }
        private static int wallObserveFrame;
        private static StringBuilder wallObserveReport;
        private static void WallObserveTick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=WallObserveTick;return;}
            if(Time.time<wallObserveNext)return;wallObserveNext=Time.time+.1f;
            var actor=UnityEngine.Object.FindFirstObjectByType<ParvumBrain>();var view=actor.GetComponentInChildren<ParvumAnimationView>();
            var target=actor.CurrentTarget;
            wallObserveReport.AppendLine($"t={Time.time-wallObserveStart:F3} position={actor.transform.position:F4} state={actor.Behaviour} target={target?.name} motion={view.Motion} cycle={view.BiteCycle} mouth={view.MouthTip:F4} gap={(target?Vector3.Distance(view.MouthTip,target.ClosestPoint(view.MouthTip)):-1):F4}");
            if(wallObserveFrame%3==0)Capture(actor.transform,DirectoryPath+"/WallBite_"+wallObserveFrame.ToString("D3")+".png");
            wallObserveFrame++;File.WriteAllText(DirectoryPath+"/WallBiteObserve.txt",wallObserveReport.ToString());
            if(Time.time-wallObserveStart>20){EditorApplication.update-=WallObserveTick;ConsoleReport("WallBite");}
        }
        internal static void Inspect()
        {
            Directory.CreateDirectory(DirectoryPath);
            var active = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || active.name != "Pegasus")
                throw new InvalidOperationException("Inspect requires Pegasus in edit mode; no scene replacement permitted.");
            var source = SceneManager.GetSceneByPath(SourcePath);
            bool opened = !source.isLoaded;
            if (opened) source = EditorSceneManager.OpenScene(SourcePath, OpenSceneMode.Additive);
            try
            {
                var report = new StringBuilder();
                var root = source.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true))
                    .First(x => x.name == "Approved Parvum Enemy Placement");
                foreach (Transform entity in root)
                {
                    report.AppendLine($"ENTITY {entity.name} position={entity.position:F4} scale={entity.lossyScale:F4}");
                    Capture(entity, DirectoryPath+"/Source_"+entity.name+".png");
                    foreach (var c in entity.GetComponentsInChildren<Component>(true))
                    {
                        if (c == null) { report.AppendLine("MISSING SCRIPT"); continue; }
                        report.AppendLine($"  {AnimationUtility.CalculateTransformPath(c.transform,entity)} : {c.GetType().FullName}");
                        if (c is SkinnedMeshRenderer r)
                        {
                            report.AppendLine($"    mesh={AssetDatabase.GetAssetPath(r.sharedMesh)} bounds={r.bounds} shapes={r.sharedMesh.blendShapeCount} vertices={r.sharedMesh.vertexCount} local={r.transform.localToWorldMatrix}");
                            for(int i=0;i<r.sharedMesh.blendShapeCount;i++) report.AppendLine("    shape="+r.sharedMesh.GetBlendShapeName(i));
                        }
                        if (c is Animator a && a.runtimeAnimatorController)
                        {
                            report.AppendLine("    controller="+AssetDatabase.GetAssetPath(a.runtimeAnimatorController));
                            foreach(var clip in a.runtimeAnimatorController.animationClips.Distinct())
                            {
                                report.AppendLine($"    clip={AssetDatabase.GetAssetPath(clip)} length={clip.length} loop={clip.isLooping}");
                                foreach(var binding in AnimationUtility.GetCurveBindings(clip))
                                    report.AppendLine($"      {binding.path} : {binding.propertyName}");
                            }
                        }
                    }
                }
                File.WriteAllText(DirectoryPath+"/SourceBindings.txt",report.ToString());
            }
            finally
            {
                SceneManager.SetActiveScene(active);
                if(opened) EditorSceneManager.CloseScene(source,true);
            }
        }
        private static void CaptureDoorContext(Vector3 position,Vector3 lookAt,string output)
        {
            var go=new GameObject("Parvum temporary doorway observation camera");var camera=go.AddComponent<Camera>();
            var texture=new RenderTexture(960,720,24);var previous=RenderTexture.active;
            try
            {
                camera.fieldOfView=75;camera.nearClipPlane=.02f;camera.transform.position=position;camera.transform.LookAt(lookAt);camera.targetTexture=texture;
                camera.Render();RenderTexture.active=texture;var image=new Texture2D(960,720,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,960,720),0,0);image.Apply();File.WriteAllBytes(output,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
            }
            finally{RenderTexture.active=previous;camera.targetTexture=null;texture.Release();UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(go);}
        }
        private static void Capture(Transform target, string path)
        {
            var renderers=target.GetComponentsInChildren<Renderer>();
            var bounds=renderers[0].bounds;
            foreach(var r in renderers) bounds.Encapsulate(r.bounds);
            if(target.GetComponent<ParvumBrain>())
            {
                var skin=target.GetComponentInChildren<SkinnedMeshRenderer>();var pose=new Mesh();skin.BakeMesh(pose,true);
                var vertices=pose.vertices;bounds=new Bounds(skin.transform.TransformPoint(vertices[0]),Vector3.zero);
                foreach(var vertex in vertices)bounds.Encapsulate(skin.transform.TransformPoint(vertex));
                UnityEngine.Object.DestroyImmediate(pose);
            }
            var go=new GameObject("Parvum temporary observation camera");
            var camera=go.AddComponent<Camera>();
            var texture=new RenderTexture(960,720,24);
            var previous=RenderTexture.active;
            try
            {
                camera.fieldOfView=40; camera.nearClipPlane=.01f;
                var direction=target.GetComponent<ParvumBrain>() ? target.forward*.3f+target.right*.9f+Vector3.up*.35f : new Vector3(.5f,.25f,-1f);
                if(path.Contains("WallBite"))direction=target.right-target.forward*.08f+Vector3.up*.12f;
                if(path.Contains("WallBiteSupplyTop"))direction=-target.right*.3f-target.forward*.3f+Vector3.up*1.5f;
                float maximumDistance=bounds.size.magnitude*1.55f;
                float distance=0;
                var disabledBoxes=UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None)
                    .Where(x=>!x.enabled && x.GetComponent<Renderer>() && !x.transform.IsChildOf(target)).ToArray();
                var preferredDirection=direction;
                for(int angle=0;angle<8;angle++)
                {
                    var candidate=(Quaternion.AngleAxis(angle*45,Vector3.up)*preferredDirection).normalized;
                    float clear=maximumDistance;
                    foreach(var obstruction in Physics.RaycastAll(bounds.center,candidate,clear,~0,QueryTriggerInteraction.Ignore))
                        if(!obstruction.transform.IsChildOf(target) && obstruction.distance>.01f)clear=Mathf.Min(clear,Mathf.Max(.03f,obstruction.distance-.08f));
                    // A disabled physics box can still describe opaque cargo that hides the observation camera.
                    foreach(var box in disabledBoxes)
                    {
                        var localDirection=box.transform.InverseTransformVector(candidate);
                        var ray=new Ray(box.transform.InverseTransformPoint(bounds.center),localDirection);
                        if(new Bounds(box.center,box.size).IntersectRay(ray,out float hit) && hit>.01f)
                            clear=Mathf.Min(clear,Mathf.Max(.03f,hit/localDirection.magnitude-.08f));
                    }
                    if(clear>distance){distance=clear;direction=candidate;}
                    if(clear>=(path.Contains("WallBite")?bounds.size.magnitude*.9f:maximumDistance-.001f))break;
                }
                if(distance<bounds.size.magnitude*1.55f)camera.fieldOfView=65;
                camera.transform.position=bounds.center+direction.normalized*distance;
                camera.transform.LookAt(bounds.center);camera.targetTexture=texture;
                camera.Render();RenderTexture.active=texture;
                var image=new Texture2D(960,720,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,960,720),0,0);image.Apply();
                File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
            }
            finally { RenderTexture.active=previous;camera.targetTexture=null;texture.Release();UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
