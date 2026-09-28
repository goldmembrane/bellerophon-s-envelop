using System.Collections.Generic;
using System.Linq;
using Bellerophon.Enemies.Parvum;
using Bellerophon.Enemies.Seed;
using Bellerophon.Core.Session;
using UnityEngine;
using UnityEngine.AI;

namespace Bellerophon.Enemies.Fuga
{
    public enum FugaBehaviour { Search, ApproachMetal, Consume, PursueAttacker, Attack, ApproachSpeaker, RelocateRoom, AdvanceIntoRoom, Dead }
    [RequireComponent(typeof(Rigidbody),typeof(SphereCollider),typeof(FugaAnimationView))]
    public sealed class FugaBrain : MonoBehaviour
    {
        [SerializeField] float flightHeight=1.62f;
        [SerializeField] int navigationAgentType;
        // Relative to the approved original Fuga; keeps world-space clearances consistent with its visual size.
        [SerializeField] float bodySize=1f;
        Rigidbody body;SphereCollider hull;FugaAnimationView view;
        ParvumTarget target,attacker,occupied,sourceRoom,destination;
        bool suppressSpeakers;
        readonly SeedRoomEntry entry=new SeedRoomEntry();
        public IReadOnlyList<SeedRoomDoor> Doorways=>entry.Doors;
        public string ActiveEntryName=>entry.Pending?entry.Door.Name:"";
        public string LastCompletedEntryName=>entry.LastCompletedName;
        public string WarehouseEntryDiagnostic=>entry.Diagnostic;
        float scan,attackAt,stuck,settled,deathAt;Vector3 lastPosition;
        Vector3[] route;int corner;
        readonly Dictionary<ParvumTarget,float> blocked=new Dictionary<ParvumTarget,float>();
        readonly HashSet<Object> support=new HashSet<Object>();
        CombatStatusEffectState[] effects=new CombatStatusEffectState[0];
        Mesh contactMesh;Vector3[] previousVertices;Mesh previousSource;SkinnedMeshRenderer previousRenderer;float contactTime;
        Rigidbody[] ragdoll;Collider[] ragdollColliders;
        public float Health {get;private set;}=SeedIntruderRules.FugaHealth;
        public FugaBehaviour Behaviour {get;private set;}
        public ParvumTarget CurrentTarget=>target;
        public float FlightHeight=>flightHeight;
        public float BodySize=>bodySize;
        public float EntryAdvance=>entry.Crossed?entry.Depth(body.position):0;
        public string Diagnostic {get;private set;}
        public float StableAt {get;private set;}=-1;
        public float DeathAt=>deathAt;
        public float SurfaceDistance {get;private set;}
        public string LastPursuitEnd {get;private set;}
        public float LastCompletedEntryAdvance=>entry.LastCompletedAdvance;
        public float AttackMultiplier=>support.Count>0?1+SeedIntruderRules.UrzereSeedAttackBonusPercent/100f:1;
        public void Configure(float height,int agent){flightHeight=height;navigationAgentType=agent;}
        public void SetUrzereSupport(Object owner,bool enabled){if(!owner)return;if(enabled)support.Add(owner);else support.Remove(owner);}
        void Awake()
        {
            body=GetComponent<Rigidbody>();hull=GetComponent<SphereCollider>();view=GetComponent<FugaAnimationView>();
            body.useGravity=false;body.isKinematic=false;body.constraints=RigidbodyConstraints.FreezeRotation;
            body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;lastPosition=body.position;entry.Initialize(body.position);
        }
        public void ReceiveDamage(float damage,ParvumTarget from,CombatStatusEffectApplication status=default)
        {
            if(Health<=0)return;Health=Mathf.Max(0,Health-Mathf.Max(0,damage));
            if(Health<=0){BeginDeath();return;}
            if(from && from.Faction!=IntruderFaction.SeedEntity && !attacker)
            {suppressSpeakers=Behaviour==FugaBehaviour.ApproachSpeaker;attacker=from;target=from;route=null;scan=0;Behaviour=FugaBehaviour.PursueAttacker;}
            if(status.HasEffect)effects=CombatStatusEffectRules.ApplyEffect(effects,status);
            if(damage>0)view.PlayHit();
        }
        public void NotifyObjectiveConflict(ParvumTarget other)
        {if(Health>0 && !attacker && other && other.IsHostileToParvum && other.IsAlive && Vector3.Distance(body.position,other.ClosestPoint(body.position))<=12 && Visible(other)){attacker=other;target=other;scan=0;}}
        void FixedUpdate()
        {
            float dt=Time.fixedDeltaTime;
            if(Health<=0){TickDeath(dt);return;}
            support.RemoveWhere(x=>!x || (x is UnityEngine.Behaviour behaviour && !behaviour.isActiveAndEnabled));
            if(support.Count>0)Health=Mathf.Min(SeedIntruderRules.FugaHealth,Health+SeedIntruderRules.UrzereSeedHealthRegenPerSecond*dt);
            var tick=CombatStatusEffectRules.TickEffects(effects,dt);effects=tick.Effects;
            if(tick.HealthDamage>0){ReceiveDamage(tick.HealthDamage,null);if(Health<=0)return;}
            if(CombatStatusEffectRules.BlocksActions(effects)){body.linearVelocity=Vector3.zero;view.Request(FugaMotion.Idle);return;}
            if(attacker && (!attacker.IsAlive || Vector3.Distance(body.position,attacker.ClosestPoint(body.position))>12 || !Visible(attacker)))
            {LastPursuitEnd=$"time={Time.time:F3} distance={Vector3.Distance(body.position,attacker.ClosestPoint(body.position)):F3} alive={attacker.IsAlive} visible={Visible(attacker)}";Diagnostic="Pursuit ended: death/range/sight";attacker=null;target=null;route=null;suppressSpeakers=false;scan=0;}
            UpdateRoom();scan-=dt;
            if(scan<=0)
            {
                scan=.25f;
                var speaker=suppressSpeakers?null:NearestSpeaker();
                if(speaker){attacker=null;Choose(speaker,FugaBehaviour.ApproachSpeaker);}
                else if(attacker)Choose(attacker,FugaBehaviour.PursueAttacker);
                else if(!Relocate())ChooseMetal();
                if(target)PlanTarget();
            }
            if(target && target.IsAlive && Visible(target))
            {
                var point=target.ClosestPoint(body.position);var delta=point-body.position;
                float approach=(target.IsMetal?.26f:.25f)*bodySize;
                if(delta.magnitude<=approach+.01f && (!target.IsMetal || SeedMetalApproach.HasSurface(target,body.position,transform)))
                {
                    body.linearVelocity=Vector3.zero;Face(point);
                    if(Vector3.Dot(transform.forward,Vector3.ProjectOnPlane(delta,Vector3.up).normalized)<.94f){view.Request(FugaMotion.Idle);return;}
                    Behaviour=target.IsMetal?FugaBehaviour.Consume:target.Kind==ParvumTargetKind.Speaker?FugaBehaviour.ApproachSpeaker:FugaBehaviour.Attack;
                    view.Request(target.IsMetal?FugaMotion.Consume:FugaMotion.Attack);
                    if(target.IsMetal && view.MotionTime>4 && Time.time-contactTime>4){blocked[target]=Time.time+5;Choose(null,FugaBehaviour.Search);scan=0;}
                    return;
                }
            }
            if(route==null || corner>=route.Length){Hover(dt);view.Request(FugaMotion.Idle);return;}
            Vector3 goal=FlightPoint(route[corner]);var offset=goal-body.position;
            float arrival=Behaviour==FugaBehaviour.AdvanceIntoRoom?.025f:.15f;
            if(Vector3.ProjectOnPlane(offset,Vector3.up).magnitude<arrival && Mathf.Abs(offset.y)<.2f && !(target && corner==route.Length-1)){corner++;Hover(dt);return;}
            if(target && !target.IsMetal && corner==route.Length-1 && Visible(target))
            {
                var towards=target.ClosestPoint(body.position)-body.position;
                if(towards.magnitude<1.1f*bodySize)
                    offset=towards.normalized*Mathf.Max(0,towards.magnitude-(target.IsMetal?.26f:.25f)*bodySize);
            }
            float speed=(SeedIntruderRules.FugaMovementSpeed+(support.Count>0?SeedIntruderRules.UrzereSeedMovementSpeedBonus:0))*CombatStatusEffectRules.CalculateMovementMultiplier(effects);
            var velocity=Vector3.ClampMagnitude(offset/Mathf.Max(dt,.001f),speed);
            // Rigidbody collision remains authoritative; never reposition through an obstruction.
            foreach(var hit in Physics.SphereCastAll(body.position,hull.radius,velocity.normalized,velocity.magnitude*dt+.025f,~0,QueryTriggerInteraction.Ignore))
            {
                if(hit.transform.IsChildOf(transform) || (target && hit.transform.IsChildOf(target.transform)))continue;
                var normal=hit.normal;
                if(hit.distance<=.0001f)
                {
                    // Initial-overlap sphere casts return -direction, which must not trap a flyer
                    // trying to leave the cargo/wall it just finished consuming.
                    if(!Physics.ComputePenetration(hull,body.position,body.rotation,hit.collider,hit.transform.position,hit.transform.rotation,out normal,out _) || Vector3.Dot(velocity,normal)>=0)continue;
                }
                if(Vector3.Dot(velocity,normal)<0)velocity=Vector3.ProjectOnPlane(velocity,normal);
            }
            body.linearVelocity=velocity;Face(body.position+Vector3.ProjectOnPlane(offset,Vector3.up));view.Request(FugaMotion.Move);
            stuck=Vector3.Distance(lastPosition,body.position)<.002f?stuck+dt:0;lastPosition=body.position;
            if(stuck>1.5f){if(target && target.IsMetal)blocked[target]=Time.time+5;route=null;target=null;scan=0;stuck=0;Diagnostic="Replan blocked route";}
        }
        void Choose(ParvumTarget next,FugaBehaviour state){if(target!=next){target=next;route=null;contactTime=0;previousVertices=null;}Behaviour=state;}
        bool Visible(ParvumTarget candidate)=>ClearRay(body.position,candidate.ClosestPoint(body.position),candidate);
        bool ClearRay(Vector3 a,Vector3 b,ParvumTarget candidate=null)
        {return !Physics.RaycastAll(a,b-a,Vector3.Distance(a,b),~0,QueryTriggerInteraction.Ignore).Any(h=>!h.transform.IsChildOf(transform) && !h.collider.GetComponentInParent<Bellerophon.Core.Player.PegasusThrownStick>() && (!candidate || !h.transform.IsChildOf(candidate.transform)));}
        bool Ground(Vector3 point,out RaycastHit ground)
        {
            ground=Physics.RaycastAll(point+Vector3.up*.3f,Vector3.down,flightHeight+3,~0,QueryTriggerInteraction.Ignore)
                .Where(h=>!h.transform.IsChildOf(transform) && h.normal.y>.45f).OrderBy(h=>h.distance).FirstOrDefault();return ground.collider;
        }
        Vector3 Foot=>Ground(body.position,out var floor)?floor.point:body.position-Vector3.up*flightHeight;
        Vector3 FlightPoint(Vector3 floor)
        {
            var p=floor+Vector3.up*flightHeight;
            var ceiling=Physics.RaycastAll(floor+Vector3.up*.15f,Vector3.up,flightHeight+.5f,~0,QueryTriggerInteraction.Ignore).Where(h=>!h.transform.IsChildOf(transform)).OrderBy(h=>h.distance).FirstOrDefault();
            if(ceiling.collider)p.y=Mathf.Min(p.y,ceiling.point.y-.42f*bodySize);return p;
        }
        void Hover(float dt){var p=FlightPoint(Foot);body.linearVelocity=Vector3.up*Mathf.Clamp((p.y-body.position.y)*6,-SeedIntruderRules.FugaMovementSpeed,SeedIntruderRules.FugaMovementSpeed);}
        void Face(Vector3 p){var delta=p-body.position;delta.y=0;if(delta.sqrMagnitude>.0001f)body.MoveRotation(Quaternion.RotateTowards(body.rotation,Quaternion.LookRotation(delta),360*Time.fixedDeltaTime));}
        bool PathTo(Vector3 point,out Vector3[] result)
        {
            result=null;var filter=new NavMeshQueryFilter{agentTypeID=navigationAgentType,areaMask=NavMesh.AllAreas};
            if(!NavMesh.SamplePosition(Foot,out var start,.8f,filter) || !NavMesh.SamplePosition(point,out var end,.8f,filter))return false;
            var path=new NavMeshPath();if(!NavMesh.CalculatePath(start.position,end.position,filter,path) || path.status!=NavMeshPathStatus.PathComplete)return false;
            result=path.corners;return true;
        }
        bool Approach(ParvumTarget candidate,out Vector3[] result)
        {
            result=null;var surface=candidate.ClosestPoint(body.position);var away=Vector3.ProjectOnPlane(body.position-surface,Vector3.up).normalized;
            if(away==Vector3.zero)away=-transform.forward;
            var tangent=Vector3.Cross(Vector3.up,away);
            for(int i=0;i<9;i++)
            {
                float side=i==0?0:((i+1)/2)*.35f*(i%2==0?-1:1);
                var probe=body.position+tangent*side;
                var wall=candidate.ClosestPoint(probe);
                var normal=Vector3.ProjectOnPlane(probe-wall,Vector3.up).normalized;
                var point=wall+normal*(candidate.IsMetal?.26f:.45f)*bodySize;
                point.y=candidate.Kind==ParvumTargetKind.Creature && candidate.Surface?candidate.Surface.bounds.min.y:Foot.y;
                var airGoal=FlightPoint(point);var airDelta=airGoal-body.position;
                // A flying actor can cross clear air above gaps without requiring floor navigation.
                if((!candidate.IsMetal || SeedMetalApproach.HasSurface(candidate,airGoal,transform)) && SeedMetalApproach.ClearFlightLeg(hull,body.position,airGoal,candidate))
                {result=new[]{Foot,point};return true;}
                if(!PathTo(point,out var path))continue;
                var destination=FlightPoint(path[path.Length-1]);
                if(Vector3.Distance(destination,candidate.ClosestPoint(destination))>1 || !ClearRay(destination,candidate.ClosestPoint(destination),candidate))continue;
                if(!candidate.IsMetal){result=path;return true;}
                // Use the destination room's navigable floor, never the current ramp/support height.
                point.y=path[path.Length-1].y;airGoal=FlightPoint(point);
                if(!SeedMetalApproach.HasSurface(candidate,airGoal,transform))continue;
                bool clear=true;var previous=body.position;
                for(int j=1;j<path.Length && clear;j++)
                {var next=FlightPoint(path[j]);clear=SeedMetalApproach.ClearFlightLeg(hull,previous,next,candidate);previous=next;}
                if(!clear || !SeedMetalApproach.ClearFlightLeg(hull,previous,airGoal,candidate))continue;
                // Keep the selected clear approach point; do not steer back into the nearest doorway jamb.
                var complete=new List<Vector3>(path);complete.Add(point);result=complete.ToArray();return true;
            }
            return false;
        }
        void PlanTarget(){if(Approach(target,out var path)){route=path;corner=path.Length>1?1:0;Diagnostic="Path complete";}else{route=null;Diagnostic="No approach path";}}
        void ChooseMetal()
        {
            ParvumTarget best=null;Vector3[] bestPath=null;float nearest=ParvumGameplayRules.MetalRange;
            foreach(var food in ParvumTarget.Active)
            {
                if(!food.IsMetal || !food.IsAlive || (blocked.TryGetValue(food,out float until) && Time.time<until))continue;
                float distance=Vector3.Distance(body.position,food.ClosestPoint(body.position));
                if(distance>=nearest || !Approach(food,out var path))continue;nearest=distance;best=food;bestPath=path;
            }
            if(entry.DeferMetal(best,body.position)){Choose(null,FugaBehaviour.AdvanceIntoRoom);route=null;scan=0;return;}
            Choose(best,best?FugaBehaviour.ApproachMetal:FugaBehaviour.Search);
            if(best){route=bestPath;corner=route.Length>1?1:0;}
            else if(route==null || corner>=route.Length)
                for(int i=0;i<12;i++)
                {
                    var offset=Random.insideUnitCircle*8;if(offset.magnitude<1)continue;var candidate=body.position+new Vector3(offset.x,0,offset.y);
                    if(Ground(candidate,out var floor))
                    {
                        var goal=FlightPoint(floor.point);var delta=goal-body.position;
                        if(!Physics.SphereCastAll(body.position,hull.radius,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.transform.IsChildOf(transform)))
                        {route=new[]{Foot,floor.point};corner=1;break;}
                    }
                    if(PathTo(Foot+new Vector3(offset.x,0,offset.y),out var path)){route=path;corner=path.Length>1?1:0;break;}
                }
        }
        ParvumTarget NearestSpeaker()
        {return ParvumTarget.Active.Where(x=>x.Audible && Vector3.Distance(body.position,x.transform.position)<ParvumGameplayRules.SpeakerRange && Approach(x,out _)).OrderBy(x=>Vector3.Distance(body.position,x.transform.position)).FirstOrDefault();}
        ParvumTarget RoomAt(Vector3 point)
        {if(!Ground(point,out var floor))return null;return ParvumTarget.Active.FirstOrDefault(x=>x.IsRoomWall && x.transform.root==floor.transform.root);}
        void UpdateRoom()
        {
            var room=RoomAt(body.position);if(room)occupied=room;
            if(room && !room.IsAlive && !sourceRoom)sourceRoom=room;
            if(entry.Observe(body.position))
            {route=null;scan=0;if(entry.Pending && !attacker && (!target || target.IsMetal))Choose(null,FugaBehaviour.AdvanceIntoRoom);}
        }
        bool Relocate()
        {
            if(entry.Pending)
            {
                if(entry.TryComplete(body.position))
                {
                    if(entry.Door.Room && entry.Door.Room.IsAlive){sourceRoom=null;destination=null;}
                    route=null;
                }
                else
                {
                    Choose(null,FugaBehaviour.AdvanceIntoRoom);
                    var goal=entry.Goal;
                    // The destination is inside the room, not at the approach ramp's elevation.
                    goal.y=body.position.y;
                    if(Ground(goal,out var entryFloor))goal=entryFloor.point;else goal.y=Foot.y;
                    if(PathTo(goal,out var path)){route=path;corner=path.Length>1?1:0;}return true;
                }
            }
            if(!sourceRoom)return false;
            if(occupied && occupied.Room==sourceRoom.Room && sourceRoom.IsAlive){sourceRoom=null;destination=null;return false;}
            Choose(null,FugaBehaviour.RelocateRoom);
            if(destination && destination.IsAlive && route!=null && corner<route.Length)return true;
            float best=float.PositiveInfinity;Vector3[] chosen=null;destination=null;
            foreach(var group in ParvumTarget.Active.Where(x=>x.IsRoomWall && x.IsAlive && x.Room!=sourceRoom.Room).GroupBy(x=>x.Room))
            {
                var walls=group.ToArray();var bounds=walls[0].Surface.bounds;foreach(var wall in walls)bounds.Encapsulate(wall.Surface.bounds);
                for(float x=bounds.min.x+.6f;x<bounds.max.x;x+=2)
                for(float z=bounds.min.z+.6f;z<bounds.max.z;z+=2)
                {
                    var goal=new Vector3(x,bounds.min.y+.15f,z);if(!PathTo(goal,out var path))continue;
                    var actual=RoomAt(path[path.Length-1]+Vector3.up);if(!actual || actual.Room!=group.Key)continue;
                    float length=0;for(int i=1;i<path.Length;i++)length+=Vector3.Distance(path[i-1],path[i]);
                    if(length<best){best=length;chosen=path;destination=walls[0];}
                }
            }
            route=chosen;corner=route!=null && route.Length>1?1:0;return true;
        }
        void LateUpdate()
        {
            if(Health<=0 || !target || !target.IsAlive || !Visible(target) || (entry.Pending && target.IsMetal))return;
            if(view.Motion!=FugaMotion.Attack && view.Motion!=FugaMotion.Consume){previousVertices=null;contactTime=0;return;}
            var renderer=view.Surface;if(!contactMesh)contactMesh=new Mesh{name="Fuga contact surface"};renderer.BakeMesh(contactMesh,true);
            var vertices=contactMesh.vertices;bool same=previousRenderer==renderer && previousSource==renderer.sharedMesh && previousVertices!=null && previousVertices.Length==vertices.Length;
            bool contact=false;SurfaceDistance=float.PositiveInfinity;
            for(int i=0;i<vertices.Length;i++)vertices[i]=renderer.transform.TransformPoint(vertices[i]);
            var weights=renderer.sharedMesh.boneWeights;var bones=renderer.bones;
            bool Relevant(int index)
            {
                var bone=bones[index];
                if(view.Motion==FugaMotion.Consume)return bone && bone.name.Contains("Lip");
                while(bone && bone!=renderer.transform){if(bone.name=="Bone_013" || bone.name=="Bone_017")return true;bone=bone.parent;}return false;
            }
            for(int i=0;i<vertices.Length;i++)
            {
                var w=weights[i];if(!(w.weight0>.05f && Relevant(w.boneIndex0)) && !(w.weight1>.05f && Relevant(w.boneIndex1)) && !(w.weight2>.05f && Relevant(w.boneIndex2)) && !(w.weight3>.05f && Relevant(w.boneIndex3)))continue;
                float separation=Vector3.Distance(vertices[i],target.ClosestPoint(vertices[i]));SurfaceDistance=Mathf.Min(SurfaceDistance,separation);
                if(separation<.02f || (same && target.IntersectsMouthSweep(previousVertices[i],vertices[i]))){contact=true;break;}
            }
            previousVertices=vertices;previousSource=renderer.sharedMesh;previousRenderer=renderer;
            if(view.Motion==FugaMotion.Consume)
            {
                if(contact)contactTime=Time.time;
                // Feeding time advances only while the mouth remains at the wall through its chew cycle.
                if(contactTime>0 && Time.time-contactTime<2.05f)target.ReceiveFugaConsumption(Time.deltaTime);
            }
            else if(contact && Time.time>=attackAt && Vector3.Distance(body.position,target.ClosestPoint(body.position))<=SeedIntruderRules.FugaAttackRange)
            {attackAt=Time.time+SeedIntruderRules.FugaAttackDelaySeconds;target.ReceiveFugaStrike(SeedIntruderRules.FugaDamage*AttackMultiplier);}
        }
        void BeginDeath()
        {
            Behaviour=FugaBehaviour.Dead;deathAt=Time.time;target=null;attacker=null;route=null;body.linearVelocity=Vector3.zero;
            hull.enabled=false;body.isKinematic=true;view.Request(FugaMotion.Death);
            // Source death starts falling on its first physics tick. Hand off that moment to joints.
            var model=view.ActiveSlot.transform.Find("Fuga_Model");var skin=view.Surface;
            var main=model.gameObject.AddComponent<Rigidbody>();main.mass=1;main.useGravity=true;main.linearDamping=.5f;main.angularDamping=3;main.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            var shape=model.gameObject.AddComponent<SphereCollider>();shape.radius=.8f;shape.center=model.InverseTransformPoint(body.position);
            var bodies=new List<Rigidbody>{main};var colliders=new List<Collider>{shape};
            foreach(var wing in skin.bones.Where(x=>x && (x.name=="Bone_013" || x.name=="Bone_017")))
            {
                var rb=wing.gameObject.AddComponent<Rigidbody>();rb.mass=.1f;rb.useGravity=true;rb.linearDamping=.5f;rb.angularDamping=3;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                var wingShape=wing.gameObject.AddComponent<SphereCollider>();wingShape.radius=.35f;
                var joint=wing.gameObject.AddComponent<CharacterJoint>();joint.connectedBody=main;joint.enableCollision=false;
                joint.lowTwistLimit=new SoftJointLimit{limit=-45};joint.highTwistLimit=new SoftJointLimit{limit=45};joint.swing1Limit=new SoftJointLimit{limit=65};joint.swing2Limit=new SoftJointLimit{limit=45};
                bodies.Add(rb);colliders.Add(wingShape);
            }
            ragdoll=bodies.ToArray();ragdollColliders=colliders.ToArray();
            foreach(var collider in ragdollColliders)
            foreach(var other in FindObjectsByType<Collider>(FindObjectsSortMode.None))
                if(other && other!=collider && (other.GetComponentInParent<Bellerophon.Core.Player.FirstPersonPlayerMotor>() || other.GetComponentInParent<FugaBrain>() || other.GetComponentInParent<ParvumBrain>()))Physics.IgnoreCollision(collider,other);
        }
        void TickDeath(float dt)
        {
            if(ragdoll==null)return;
            bool grounded=ragdollColliders.Any(c=>c && Physics.Raycast(c.bounds.center,Vector3.down,c.bounds.extents.y+.06f,~0,QueryTriggerInteraction.Ignore));
            bool stable=grounded && ragdoll.All(x=>x && x.linearVelocity.sqrMagnitude<.0025f && x.angularVelocity.sqrMagnitude<.01f);
            settled=stable?settled+dt:0;
            if(settled>=.35f && StableAt<0){StableAt=Time.time;Destroy(gameObject,1.5f);}
        }
        void OnDestroy(){if(contactMesh)Destroy(contactMesh);}
    }
}
