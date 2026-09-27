using Bellerophon.Core.Session;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

namespace Bellerophon.Enemies.Parvum
{
    [RequireComponent(typeof(Rigidbody),typeof(CapsuleCollider))]
    public sealed class ParvumBrain : MonoBehaviour
    {
        [SerializeField] private ParvumAnimationView animationView;
        [SerializeField] private int navigationAgentType;
        private Rigidbody body;
        private CapsuleCollider capsule;
        private Transform surfaceAlignment;
        private ParvumTarget target;
        private ParvumTarget attacker;
        private bool suppressSpeakers;
        private float health=SeedIntruderRules.ParvumHealth;
        private float scanIn, stuckTime;
        // Approach is calibrated against the live bite sweep, independently of visual scale.
        [SerializeField] private float biteApproachDistance=.45f;
        // Safety ceiling measured from the animated lip sweep; contact is still mandatory.
        [SerializeField] private float biteReach=SeedIntruderRules.ParvumAttackRange;
        private float BodyHeight => capsule.height*Mathf.Abs(transform.lossyScale.y);
        private float BodyRadius => capsule.radius*Mathf.Max(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.z));
        private Vector3 MouthOrigin => body.position+Vector3.up*(BodyHeight*.5f);
        private readonly Dictionary<ParvumTarget,float> blockedFoodUntil=new Dictionary<ParvumTarget,float>();
        private int lastBiteCycle=-1;
        private Vector3 previousPosition;
        private Vector3[] corners;
        private int corner;
        // Remember an exhausted source across corridor travel and combat interruptions.
        private ParvumTarget occupiedRoom, relocationSource, relocationDestination;
        private float relocationRetryAt;
        // Arrival remains pending through interruptions. Distance is forward displacement,
        // not elapsed time or accumulated pacing, and starts after the capsule clears the exit.
        private ParvumTarget entryRoom;
        private Vector3 entryDirection, entryStart;
        private bool entryCleared;
        private float completedEntryAdvance;
        public bool EntryPending => entryRoom;
        public bool EntryCleared => entryCleared;
        public Vector3 EntryStart => entryStart;
        public float EntryAdvance => entryRoom ? (entryCleared ? Vector3.Dot(body.position-entryStart,entryDirection) : 0f) : completedEntryAdvance;
        private readonly Dictionary<ShipRoomId,float> blockedRoomsUntil=new Dictionary<ShipRoomId,float>();
        public ShipRoomId? OccupiedRoom => occupiedRoom ? occupiedRoom.Room : (ShipRoomId?)null;
        public ShipRoomId? DestinationRoom => relocationDestination ? relocationDestination.Room : (ShipRoomId?)null;
        private CombatStatusEffectState[] effects=new CombatStatusEffectState[0];
        private NavMeshPath path;
        // External Urzere zone owners register/unregister support; sources never stack.
        private readonly HashSet<Object> urzereSources=new HashSet<Object>();
        public bool IsUrzereSupported => urzereSources.Count>0;
        public float AttackMultiplier => IsUrzereSupported ? 1f+SeedIntruderRules.UrzereSeedAttackBonusPercent/100f : 1f;
        public float MovementSpeed => SeedIntruderRules.ParvumMovementSpeed+(IsUrzereSupported?SeedIntruderRules.UrzereSeedMovementSpeedBonus:0);
        public ParvumBehaviour Behaviour { get; private set; }
        public float Health => health;
        public ParvumTarget CurrentTarget => target;
        public void Configure(ParvumAnimationView view,int agentType) { animationView=view;navigationAgentType=agentType; }
        public void SetUrzereSupport(Object source,bool supported)
        {if(!source)return;if(supported)urzereSources.Add(source);else urzereSources.Remove(source);}
        // Other faction controllers report a shared non-player objective; no faction rank is invented.
        public void NotifyObjectiveConflict(ParvumTarget other)
        {
            if(health<=0 || attacker || !other || !other.IsHostileToParvum || !other.IsAlive || !Visible(other))return;
            if(Vector3.Distance(body.position,other.ClosestPoint(body.position))>ParvumGameplayRules.PursuitRange)return;
            attacker=other;Choose(other,ParvumBehaviour.PursueAttacker);scanIn=0;
        }

        private void Awake()
        {
            path=new NavMeshPath();
            body=GetComponent<Rigidbody>();capsule=GetComponent<CapsuleCollider>();
            body.useGravity=true;body.isKinematic=false;body.constraints=RigidbodyConstraints.FreezeRotation;
            body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            previousPosition=body.position;
            // Keep animation bindings unchanged. Tilt only an outer visual pivot about the feet.
            if(animationView && animationView.transform.parent==transform)
            {
                surfaceAlignment=new GameObject("Parvum ground alignment").transform;
                surfaceAlignment.SetParent(transform,false);animationView.transform.SetParent(surfaceAlignment,false);
            }
        }
        public void ReceiveDamage(float damage,ParvumTarget source,CombatStatusEffectApplication status=default)
        {
            if(health<=0) return;
            health=Mathf.Max(0,health-Mathf.Max(0,damage));
            if(health<=0) { Behaviour=ParvumBehaviour.Dead;target=null;attacker=null;corners=null;animationView.SetMotion(ParvumMotion.Death);return; }
            if(source && source.Faction!=IntruderFaction.SeedEntity && !attacker)
            {
                suppressSpeakers=Behaviour==ParvumBehaviour.ApproachSpeaker || Behaviour==ParvumBehaviour.DestroySpeaker;
                attacker=source;target=source;Behaviour=ParvumBehaviour.PursueAttacker;corners=null;scanIn=0;
            }
            if(damage>0) animationView.SetMotion(ParvumMotion.Hit,true);
            if(status.HasEffect) effects=CombatStatusEffectRules.ApplyEffect(effects,status);
        }

        private void FixedUpdate()
        {
            float dt=Time.fixedDeltaTime;
            if(health<=0) {Stop();return;}
            if(surfaceAlignment && Physics.Raycast(body.position+Vector3.up*.15f,Vector3.down,out var visualGround,.25f,~0,QueryTriggerInteraction.Ignore) && visualGround.normal.y>.45f)
            {
                var tilt=Quaternion.FromToRotation(Vector3.up,transform.InverseTransformDirection(visualGround.normal));
                surfaceAlignment.localRotation=Quaternion.Slerp(surfaceAlignment.localRotation,tilt,dt*12);
            }
            urzereSources.RemoveWhere(x=>!x || (x is Behaviour behaviour && !behaviour.isActiveAndEnabled) || (x is GameObject obj && !obj.activeInHierarchy));
            if(IsUrzereSupported)health=Mathf.Min(SeedIntruderRules.ParvumHealth,health+SeedIntruderRules.UrzereSeedHealthRegenPerSecond*dt);
            var tick=CombatStatusEffectRules.TickEffects(effects,dt);effects=tick.Effects;
            if(tick.HealthDamage>0) {ReceiveDamage(tick.HealthDamage,null);if(health<=0){Stop();return;}}
            if(CombatStatusEffectRules.BlocksActions(effects)) {Stop();animationView.SetMotion(ParvumMotion.Idle);return;}

            if(attacker && (!attacker.IsAlive || Vector3.Distance(body.position,attacker.ClosestPoint(body.position))>ParvumGameplayRules.PursuitRange || !Visible(attacker)))
            {attacker=null;target=null;suppressSpeakers=false;corners=null;scanIn=0;}
            scanIn-=dt;
            UpdateOccupiedRoom();
            if(Behaviour==ParvumBehaviour.AdvanceIntoRoom && entryCleared && EntryAdvance>=ParvumGameplayRules.RoomEntryAdvanceDistance-.0001f)scanIn=0;
            if(scanIn<=0)
            {
                scanIn=.25f;
                var speaker=suppressSpeakers ? null : ClosestSpeaker();
                if(speaker)
                {
                    // Ordinary pursuit is abandoned for sound; speaker retaliation remains locked.
                    attacker=null;Choose(speaker,ParvumBehaviour.ApproachSpeaker);
                }
                else if(attacker) Choose(attacker,ParvumBehaviour.PursueAttacker);
                else if(TryRelocateRoom()) { }
                else if(!target || !target.IsAlive || target.IsMetal || target.Kind==ParvumTargetKind.Speaker)
                    ChooseMetal();
                if(target) PlanApproach(target);
            }
            if(target && target.IsAlive && Vector3.Distance(MouthOrigin,target.ClosestPoint(MouthOrigin))<=biteApproachDistance && Visible(target))
            {
                Stop();Face(target.ClosestPoint(body.position));
                var toward=target.ClosestPoint(MouthOrigin)-body.position;toward.y=0;
                if(toward.sqrMagnitude>.0001f && Vector3.Dot(transform.forward,toward.normalized)<.98f)
                {animationView.SetMotion(ParvumMotion.Idle);lastBiteCycle=-1;return;}
                Behaviour=target.IsMetal ? ParvumBehaviour.Consume : target.Kind==ParvumTargetKind.Speaker ? ParvumBehaviour.DestroySpeaker : ParvumBehaviour.Bite;
                animationView.SetMotion(ParvumMotion.Attack);
                if(!target.IsAlive){target=null;attacker=null;suppressSpeakers=false;scanIn=0;corners=null;}
                return;
            }
            lastBiteCycle=-1;
            if(!target && !relocationSource) Behaviour=ParvumBehaviour.Search;
            if(corners==null || corner>=corners.Length)
            {
                if(!target && !relocationSource && scanIn<=.05f) PlanSearch();
                Stop();animationView.SetMotion(ParvumMotion.Idle);return;
            }
            Vector3 delta=corners[corner]-body.position;delta.y=0;
            if(target && corner==corners.Length-1 && delta.magnitude<.3f && Visible(target))
            {delta=target.ClosestPoint(MouthOrigin)-body.position;delta.y=0;}
            if(delta.magnitude<(Behaviour==ParvumBehaviour.AdvanceIntoRoom ? .001f : target ? .03f : .12f)){corner++;Stop();return;}
            var direction=delta.normalized;
            Vector3 velocity=direction*MovementSpeed*CombatStatusEffectRules.CalculateMovementMultiplier(effects);
            if(Behaviour==ParvumBehaviour.AdvanceIntoRoom && entryCleared)
                // Do not asymptotically stall below one metre against floor friction.
                // Final fixed-step overshoot is bounded by 0.25 m/s * fixedDeltaTime.
                velocity=direction*Mathf.Min(velocity.magnitude,Mathf.Max(.25f,Mathf.Max(0,ParvumGameplayRules.RoomEntryAdvanceDistance-EntryAdvance)/dt));
            if(Physics.Raycast(body.position+Vector3.up*.15f,Vector3.down,out var ground,.4f,~0,QueryTriggerInteraction.Ignore))
                velocity=Vector3.ProjectOnPlane(velocity,ground.normal).normalized*velocity.magnitude;
            // Follow a grounded incline with its tangent velocity. Preserve falling velocity in air.
            float vertical=ground.collider && ground.distance<=.22f ? velocity.y : body.linearVelocity.y;
            if(TryStep(direction,out float stepRise))vertical=Mathf.Max(vertical,Mathf.Min(MovementSpeed,stepRise/Time.fixedDeltaTime));
            body.linearVelocity=new Vector3(velocity.x,vertical,velocity.z);
            Face(body.position+direction);animationView.SetMotion(ParvumMotion.Move);
            if(Vector3.Distance(previousPosition,body.position)<.002f) stuckTime+=dt;else stuckTime=0;
            previousPosition=body.position;
            if(stuckTime>1.5f){if(target && target.IsMetal)blockedFoodUntil[target]=Time.time+5;if(Behaviour==ParvumBehaviour.RelocateRoom && relocationDestination){blockedRoomsUntil[relocationDestination.Room]=Time.time+5;relocationDestination=null;}target=null;corners=null;scanIn=0;stuckTime=0;}
        }

        private ParvumTarget RoomBelow(Vector3 position)
        {
            if(!Physics.Raycast(position+Vector3.up*.4f,Vector3.down,out var floor,1f,~0,QueryTriggerInteraction.Ignore))return null;
            foreach(var wall in ParvumTarget.Active)
                if(wall.IsRoomWall && wall.Ship && floor.transform.root==wall.transform.root)return wall;
            return null;
        }
        private void UpdateOccupiedRoom()
        {
            var room=RoomBelow(body.position);
            if(room)occupiedRoom=room;
            if(entryRoom && !entryRoom.IsAlive){entryRoom=null;entryCleared=false;relocationDestination=null;corners=null;}
            if(!entryRoom && room && room.IsAlive && relocationSource && room.Room!=relocationSource.Room && Behaviour==ParvumBehaviour.RelocateRoom)
            {
                entryRoom=room;entryCleared=false;completedEntryAdvance=0;scanIn=0;
                entryDirection=Vector3.ProjectOnPlane(body.linearVelocity,Vector3.up).normalized;
                if(entryDirection.sqrMagnitude<.1f)entryDirection=Vector3.ProjectOnPlane(transform.forward,Vector3.up).normalized;
            }
            if(entryRoom && !entryCleared && (Behaviour==ParvumBehaviour.RelocateRoom || Behaviour==ParvumBehaviour.AdvanceIntoRoom))
            {
                var rear=RoomBelow(body.position-entryDirection*(BodyRadius+.02f));
                if(room && rear && room.Room==entryRoom.Room && rear.Room==entryRoom.Room)
                {entryCleared=true;entryStart=body.position;scanIn=0;}
            }
            if(room && !room.IsAlive && !relocationSource)relocationSource=room;
        }
        private bool TryRelocateRoom()
        {
            if(!relocationSource)return false;
            if(entryRoom)
            {
                Choose(null,ParvumBehaviour.AdvanceIntoRoom);
                var room=RoomBelow(body.position);
                if(entryCleared && EntryAdvance>=ParvumGameplayRules.RoomEntryAdvanceDistance-.0001f && room && room.Room==entryRoom.Room)
                {completedEntryAdvance=EntryAdvance;entryRoom=null;relocationSource=null;relocationDestination=null;corners=null;return false;}
                // Keep the goal just beyond the threshold so corner tolerance cannot finish early.
                var goal=entryCleared ? entryStart+entryDirection*(ParvumGameplayRules.RoomEntryAdvanceDistance+.02f) : body.position+entryDirection*.5f;
                corners=new[]{body.position,goal};corner=1;
                return true;
            }
            // A repaired source permits normal local feeding again.
            if(occupiedRoom && occupiedRoom.Room==relocationSource.Room && relocationSource.IsAlive)
            {relocationSource=null;relocationDestination=null;corners=null;return false;}
            bool interrupted=Behaviour!=ParvumBehaviour.RelocateRoom;
            Choose(null,ParvumBehaviour.RelocateRoom);
            if(relocationDestination && (!relocationDestination.IsAlive || interrupted))
            {relocationDestination=null;corners=null;}
            if(relocationDestination && corners!=null && corner<corners.Length)return true;
            if(Time.time<relocationRetryAt)return true;
            relocationRetryAt=Time.time+1;
            var filter=new NavMeshQueryFilter{agentTypeID=navigationAgentType,areaMask=NavMesh.AllAreas};
            if(!NavMesh.SamplePosition(body.position,out var start,.35f,filter))return true;
            float best=float.PositiveInfinity;Vector3[] bestRoute=null;ParvumTarget bestRoom=null;
            var fallbackPoints=new Dictionary<ParvumTarget,List<Vector3>>();
            var rooms=new Dictionary<ShipRoomId,Bounds>();var representatives=new Dictionary<ShipRoomId,ParvumTarget>();
            foreach(var wall in ParvumTarget.Active)
            {
                if(!wall.IsRoomWall || !wall.Surface || !wall.IsAlive || wall.Room==relocationSource.Room || wall.Ship!=relocationSource.Ship)continue;
                if(blockedRoomsUntil.TryGetValue(wall.Room,out float until) && Time.time<until)continue;
                var bounds=wall.Surface.bounds;
                if(rooms.TryGetValue(wall.Room,out var combined)){combined.Encapsulate(bounds);rooms[wall.Room]=combined;}
                else {rooms.Add(wall.Room,bounds);representatives.Add(wall.Room,wall);}
            }
            foreach(var pair in rooms)
            {
                var bounds=pair.Value;
                var closestGoals=new List<Vector3>();var closestLengths=new List<float>();
                fallbackPoints.Add(representatives[pair.Key],closestGoals);
                for(float x=bounds.min.x+.6f;x<=bounds.max.x-.6f;x+=1.5f)
                for(float z=bounds.min.z+.6f;z<=bounds.max.z-.6f;z+=1.5f)
                {
                    var probe=new Vector3(x,bounds.min.y+.15f,z);
                    if(!NavMesh.SamplePosition(probe,out var hit,1f,filter))continue;
                    var floorRoom=RoomBelow(hit.position);
                    if(!floorRoom || floorRoom.Room!=pair.Key)continue;
                    if(!NavMesh.CalculatePath(start.position,hit.position,filter,path) || path.status!=NavMeshPathStatus.PathComplete)continue;
                    var route=path.corners;float length=0;
                    for(int i=1;i<route.Length;i++)length+=Vector3.Distance(route[i-1],route[i]);
                    int rank=closestLengths.FindIndex(x=>x>length);if(rank<0)rank=closestLengths.Count;
                    if(rank<3){closestLengths.Insert(rank,length);closestGoals.Insert(rank,hit.position);if(closestGoals.Count>3){closestGoals.RemoveAt(3);closestLengths.RemoveAt(3);}}
                    if(length>=best || !RelocationRouteClear(route))continue;
                    best=length;bestRoute=route;bestRoom=representatives[pair.Key];
                }
            }
            // The baked shortest route can cross an unwalkable decorative deck. Try real,
            // collision-clear local waypoints before declaring every other room unreachable.
            if(fallbackPoints.Count>0)
            {
                for(float radius=2;radius<=6;radius+=2)
                for(int direction=0;direction<16;direction++)
                {
                    var offset=Quaternion.Euler(0,direction*22.5f,0)*Vector3.forward*radius;
                    if(!NavMesh.SamplePosition(body.position+offset,out var via,.8f,filter))continue;
                    if(!NavMesh.CalculatePath(start.position,via.position,filter,path) || path.status!=NavMeshPathStatus.PathComplete)continue;
                    var first=path.corners;if(first.Length<1 || !RelocationRouteClear(first))continue;
                    foreach(var roomGoals in fallbackPoints)
                    foreach(var goal in roomGoals.Value)
                    {
                        if(Vector3.Distance(body.position,via.position)+Vector3.Distance(via.position,goal)>=best)continue;
                        if(!NavMesh.CalculatePath(via.position,goal,filter,path) || path.status!=NavMeshPathStatus.PathComplete)continue;
                        var second=path.corners;if(second.Length<1)continue;var combined=new Vector3[first.Length+second.Length-1];
                        System.Array.Copy(first,combined,first.Length);System.Array.Copy(second,1,combined,first.Length,second.Length-1);
                        float length=0;for(int i=1;i<combined.Length;i++)length+=Vector3.Distance(combined[i-1],combined[i]);
                        if(length>=best || !RelocationRouteClear(combined))continue;
                        best=length;bestRoute=combined;bestRoom=roomGoals.Key;
                    }
                }
            }
            relocationDestination=bestRoom;corners=bestRoute;corner=bestRoute!=null && bestRoute.Length>1?1:0;
            return true;
        }
        private bool RelocationRouteClear(Vector3[] route,System.Action<string> trace=null)
        {
            Vector3 previous=body.position;
            for(int i=1;i<route.Length;i++)
            {
                var delta=route[i]-route[i-1];
                int samples=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/.25f));
                for(int sample=1;sample<=samples;sample++)
                {
                    var next=Vector3.Lerp(route[i-1],route[i],(float)sample/samples);
                    // Query the real floor, not a bake surface sitting above a decorative deck.
                    if(!Physics.Raycast(next+Vector3.up*.1f,Vector3.down,out var floor,1f,~0,QueryTriggerInteraction.Ignore)){trace?.Invoke($"No floor {next:F3}");return false;}
                    next.y=floor.point.y;
                    if(next.y-previous.y>ParvumGameplayRules.MaximumStepHeight+.02f || previous.y-next.y>.5f){trace?.Invoke($"Floor step {previous:F3} -> {next:F3} {floor.collider.name}");return false;}
                    var step=next-previous;
                    foreach(var hit in Physics.CapsuleCastAll(previous+Vector3.up*(BodyRadius+ParvumGameplayRules.MaximumStepHeight+.02f),previous+Vector3.up*(BodyHeight-BodyRadius+ParvumGameplayRules.MaximumStepHeight+.02f),BodyRadius-.005f,step.normalized,step.magnitude,~0,QueryTriggerInteraction.Ignore))
                        if(!hit.transform.IsChildOf(transform) && hit.normal.y<.6f){trace?.Invoke($"Upper obstruction {hit.collider.name} at {previous:F3} -> {next:F3}");return false;}
                    previous=next;
                }
            }
            return true;
        }

        private void Choose(ParvumTarget next,ParvumBehaviour state)
        {if(target!=next){target=next;corners=null;lastBiteCycle=-1;animationView.SetMotion(ParvumMotion.Idle);}Behaviour=state;}
        internal void CommitMouthContact(Vector3 mouth,Vector3 previousMouth,bool sameCycle)
        {
            if(health<=0 || !target || !target.IsAlive || !animationView.IsBiteContactPhase || animationView.BiteCycle==lastBiteCycle)return;
            var point=target.ClosestPoint(mouth);
            bool touching=Vector3.Distance(mouth,point)<=.015f;
            var sweep=mouth-previousMouth;
            if(!touching && sameCycle && target.Surface && sweep.sqrMagnitude>.000001f)
                touching=target.IntersectsMouthSweep(previousMouth,mouth);
            if(!touching)return;
            // Aim at the surface, not the laterally moving lip. Perform visibility queries
            // only after a real contact candidate, not for every vertex in empty space.
            var toward=target.ClosestPoint(MouthOrigin)-MouthOrigin;toward.y=0;
            if(toward.magnitude>biteReach ||
                (toward.sqrMagnitude>.0001f && Vector3.Dot(transform.forward,toward.normalized)<.98f) || !VisibleFrom(MouthOrigin,target))return;
            lastBiteCycle=animationView.BiteCycle;
            target.AccumulateConsumption(.5f); // Only completed contact bites count toward facility wounds.
            target.ReceiveBite(this);
        }
        private void ChooseMetal()
        {
            ParvumTarget nearest=null;float distance=ParvumGameplayRules.MetalRange;
            foreach(var candidate in ParvumTarget.Active)
            {
                if(!candidate.IsMetal || !candidate.IsAlive)continue;
                if(blockedFoodUntil.TryGetValue(candidate,out float retryAt) && Time.time<retryAt)continue;
                if(candidate.Surface && candidate.Surface.enabled && candidate.Surface.bounds.SqrDistance(body.position)>distance*distance)continue;
                float d=Vector3.Distance(body.position,candidate.ClosestPoint(body.position));
                if(d<distance && CanApproach(candidate,out _)){nearest=candidate;distance=d;}
            }
            Choose(nearest,nearest ? ParvumBehaviour.ApproachMetal : ParvumBehaviour.Search);
            if(!nearest && (corners==null || corner>=corners.Length)) PlanSearch();
        }
        private ParvumTarget ClosestSpeaker()
        {
            ParvumTarget nearest=null;float distance=ParvumGameplayRules.SpeakerRange;
            foreach(var candidate in ParvumTarget.Active)
            {
                if(!candidate.Audible)continue;
                float d=Vector3.Distance(body.position,candidate.transform.position);
                if(d<distance && CanApproach(candidate,out _)){nearest=candidate;distance=d;}
            }
            return nearest;
        }
        private bool CanApproach(ParvumTarget candidate,out Vector3 point,System.Action<string> trace=null)
        {
            var contact=candidate.ClosestPoint(MouthOrigin);
            var filter=new NavMeshQueryFilter{agentTypeID=navigationAgentType,areaMask=NavMesh.AllAreas};
            if(!NavMesh.SamplePosition(body.position,out var start,.35f,filter)){trace?.Invoke("No start surface");point=default;return false;}
            var awayFromWall=body.position-contact;awayFromWall.y=0;
            var tangent=Vector3.Cross(Vector3.up,awayFromWall.normalized);
            for(int i=0;i<13;i++)
            {
                float along=i==0?0:((i+1)/2)*.5f*(i%2==0?-1:1);
                var probe=MouthOrigin+tangent*along;
                var wallPoint=candidate.ClosestPoint(probe);var away=probe-wallPoint;away.y=0;
                if(away.sqrMagnitude<.0001f)continue;
                if(candidate.IsMetal && Vector3.Distance(body.position,wallPoint)>ParvumGameplayRules.MetalRange)continue;
                if(!NavMesh.SamplePosition(wallPoint+away.normalized*biteApproachDistance,out var hit,Mathf.Max(.4f,BodyHeight*.6f),filter)){trace?.Invoke($"{i}: no approach surface");continue;}
                var mouthHeight=hit.position+Vector3.up*(BodyHeight*.5f);
                if(Vector3.Distance(mouthHeight,candidate.ClosestPoint(mouthHeight))>biteApproachDistance+.2f){trace?.Invoke($"{i}: out of mouth approach reach {hit.position:F3}");continue;}
                if(!VisibleFrom(mouthHeight,candidate)){trace?.Invoke($"{i}: sight blocked {hit.position:F3}");continue;}
                if(NavMesh.CalculatePath(start.position,hit.position,filter,path) && path.status==NavMeshPathStatus.PathComplete)
                {
                    bool clear=true;var route=path.corners;
                    for(int segment=1;segment<route.Length && clear;segment++)
                    {
                        var delta=route[segment]-route[segment-1];
                        foreach(var obstruction in Physics.CapsuleCastAll(route[segment-1]+Vector3.up*(BodyRadius+.02f),route[segment-1]+Vector3.up*(BodyHeight-BodyRadius+.02f),BodyRadius-.005f,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
                            if(!obstruction.transform.IsChildOf(transform) && obstruction.collider!=candidate.Surface && obstruction.normal.y<.6f)
                            {
                                bool upperBlocked=false;
                                foreach(var upper in Physics.CapsuleCastAll(route[segment-1]+Vector3.up*(BodyRadius+.02f+ParvumGameplayRules.MaximumStepHeight),route[segment-1]+Vector3.up*(BodyHeight-BodyRadius+.02f+ParvumGameplayRules.MaximumStepHeight),BodyRadius-.005f,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
                                    if(!upper.transform.IsChildOf(transform) && upper.collider!=candidate.Surface && upper.normal.y<.6f){upperBlocked=true;break;}
                                if(upperBlocked){trace?.Invoke($"{i}: blocked by {obstruction.collider.name}");clear=false;break;}
                            }
                    }
                    if(clear){point=hit.position;return true;}
                }
                else trace?.Invoke($"{i}: incomplete path");
            }
            point=default;return false;
        }
        private void PlanApproach(ParvumTarget candidate)
        {if(CanApproach(candidate,out _)){corners=path.corners;corner=corners.Length>1?1:0;}else{corners=null;}}
        private void PlanSearch()
        {
            var filter=new NavMeshQueryFilter{agentTypeID=navigationAgentType,areaMask=NavMesh.AllAreas};
            if(!NavMesh.SamplePosition(body.position,out var start,.35f,filter))return;
            for(int i=0;i<12;i++)
            {
                var random=Random.insideUnitCircle*8f;
                if(!NavMesh.SamplePosition(body.position+new Vector3(random.x,0,random.y),out var hit,2f,filter))continue;
                if(Vector3.Distance(body.position,hit.position)<1f)continue;
                if(NavMesh.CalculatePath(start.position,hit.position,filter,path) && path.status==NavMeshPathStatus.PathComplete)
                {corners=path.corners;corner=corners.Length>1?1:0;return;}
            }
        }
        private bool Visible(ParvumTarget candidate)
            => VisibleFrom(MouthOrigin,candidate);
        private bool VisibleFrom(Vector3 origin,ParvumTarget candidate)
        {
            var destination=candidate.ClosestPoint(origin);
            foreach(var hit in Physics.RaycastAll(origin,destination-origin,Vector3.Distance(origin,destination),~0,QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(transform) && hit.collider!=candidate.Surface && !hit.transform.IsChildOf(candidate.transform))return false;
            return true;
        }
        private void Stop(){body.linearVelocity=new Vector3(0,body.linearVelocity.y,0);}
        private bool TryStep(Vector3 direction,out float rise)
        {
            rise=0;
            float max=ParvumGameplayRules.MaximumStepHeight;
            // Raised deck plates can have a gap beneath them: feet miss the rim while the
            // capsule's middle hits it. Both probes retain the same maximum landing height.
            float stepProbe=BodyRadius+.15f;
            if(!Physics.Raycast(body.position+Vector3.up*.04f,direction,out var obstacle,stepProbe,~0,QueryTriggerInteraction.Ignore) &&
               !Physics.Raycast(body.position+Vector3.up*.2f,direction,out obstacle,stepProbe,~0,QueryTriggerInteraction.Ignore) &&
               !Physics.Raycast(MouthOrigin,direction,out obstacle,stepProbe,~0,QueryTriggerInteraction.Ignore))return false;
            if(obstacle.normal.y>.6f)return false;
            if(!Physics.Raycast(body.position+direction*stepProbe+Vector3.up*(max+.04f),Vector3.down,out var landing,max+.02f,~0,QueryTriggerInteraction.Ignore))return false;
            rise=landing.point.y-body.position.y;
            if(rise<=.015f || rise>max || landing.normal.y<.5f)return false;
            foreach(var ceiling in Physics.CapsuleCastAll(body.position+Vector3.up*BodyRadius,body.position+Vector3.up*(BodyHeight-BodyRadius),BodyRadius-.005f,Vector3.up,rise+.02f,~0,QueryTriggerInteraction.Ignore))
                if(!ceiling.transform.IsChildOf(transform))return false;
            return true;
        }
        private void Face(Vector3 point)
        {var d=point-body.position;d.y=0;if(d.sqrMagnitude>.001f)body.MoveRotation(Quaternion.RotateTowards(body.rotation,Quaternion.LookRotation(d),Time.fixedDeltaTime*360));}
    }
}
