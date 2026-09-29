using System.Collections.Generic;
using System.Linq;
using Bellerophon.Core.Session;
using Bellerophon.Enemies.Parvum;
using Bellerophon.Enemies.Seed;
using UnityEngine;
using UnityEngine.AI;

namespace Bellerophon.Enemies.LongaArma
{
    public enum LongaArmaBehaviour { Search, ApproachMetal, Consume, PursueAttacker, Attack, ApproachSpeaker, RelocateRoom, AdvanceIntoRoom, Dead }

    [RequireComponent(typeof(Rigidbody),typeof(CapsuleCollider),typeof(LongaArmaAnimationView))]
    public sealed class LongaArmaBrain : MonoBehaviour
    {
        [SerializeField] int navigationAgentType;
        [SerializeField] float metalApproach=.48f;
        [SerializeField] float mouthContactApproach=.025f;
        [SerializeField] float combatApproach=.08f;
        Rigidbody body;
        CapsuleCollider hull;
        LongaArmaAnimationView view;
        ParvumTarget target,attacker,occupied,sourceRoom,destinationRoom;
        readonly SeedRoomEntry entry=new SeedRoomEntry();
        readonly Dictionary<ParvumTarget,float> blocked=new Dictionary<ParvumTarget,float>();
        readonly HashSet<UnityEngine.Object> support=new HashSet<UnityEngine.Object>();
        CombatStatusEffectState[] effects=new CombatStatusEffectState[0];
        Vector3[] route;
        int corner, sampledCycle=-1, lastStrikeCycle=-1;
        // Relocation failures retry at a bounded cadence so unreachable rooms cannot stall the editor.
        float scan, stuck, deathAt, mouthContactAt, consumeStartedAt, relocationRetryAt;
        bool wasConsuming;
        bool returnFromCorridor;
        Vector3 previousPosition, previousBladeTip, initialBladeTip;
        bool raised;
        Mesh contactMesh;
        Vector3[] previousVertices;
        SkinnedMeshRenderer previousRenderer;
        SkinnedMeshRenderer cachedBladeRenderer;
        Mesh cachedBladeMesh;
        readonly List<Vector3> bakedVertices=new List<Vector3>();
        readonly List<int> bladeIndices=new List<int>();
        Vector3[] bladePositions;
        Vector3[] bladeHistory;
        float mouthReach,mouthSide,mouthHeight; // Stable resting geometry; attack lift must not change food feasibility.
        Vector3 metalFacing;
        readonly Collider[] clearanceHits=new Collider[96];
        readonly RaycastHit[] steeringHits=new RaycastHit[64]; // Reused live hull sweep; no per-step allocation.
        // Retain steering across replans; changing wall triangles must not snap the body's heading.
        Vector3 movementHeading;
        Vector3 plannedTargetPosition;
        int progressCorner=-1;
        float progressDistance,progressAt; // Progress toward the route, not mere motion in a circle.
        float foodSettleSince=-1; // Bound a failed final contact without treating intentional alignment as stuck movement.
        readonly RaycastHit[] groundHits=new RaycastHit[32];
        float navigationMargin; // Additional clearance beyond the smaller baked agent radius.

        public float Health { get; private set; }=SeedIntruderRules.LongaArmaHealth;
        public LongaArmaBehaviour Behaviour { get; private set; }
        public ParvumTarget CurrentTarget => target;
        public IReadOnlyList<SeedRoomDoor> Doorways => entry.Doors;
        public string ActiveEntryName => entry.Pending ? entry.Door.Name : "";
        public bool EntryCrossed => entry.Crossed;
        public float EntryDepth => entry.Pending ? entry.Depth(body.position) : 0;
        public Vector3 EntryGoal => entry.Pending ? entry.Goal : Vector3.zero;
        public string EntryDiagnostic => entry.Diagnostic;
        public string LastCompletedEntryName => entry.LastCompletedName;
        public float LastCompletedEntryAdvance => entry.LastCompletedAdvance;
        public float BladeSurfaceDistance { get; private set; }
        public string Diagnostic { get; private set; }
        public string ApproachDiagnostic { get; private set; }
        public float AttackMultiplier => support.Count>0 ? 1+SeedIntruderRules.UrzereSeedAttackBonusPercent/100f : 1;
        float Radius => hull.radius*Mathf.Max(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.z));
        float Height => hull.height*Mathf.Abs(transform.lossyScale.y);
        Vector3 MouthOrigin => view.MouthTip ? view.MouthTip.position : body.position+Vector3.up*.45f;

        public void Configure(int agentType) { navigationAgentType=agentType; }
        public void SetUrzereSupport(UnityEngine.Object owner,bool enabled)
        { if (!owner) return; if (enabled) support.Add(owner); else support.Remove(owner); }

        void Awake()
        {
            body=GetComponent<Rigidbody>();hull=GetComponent<CapsuleCollider>();view=GetComponent<LongaArmaAnimationView>();
            body.useGravity=true;body.isKinematic=false;body.constraints=RigidbodyConstraints.FreezeRotation;
            body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            body.interpolation=RigidbodyInterpolation.Interpolate;
            navigationMargin=Mathf.Max(0,Radius+.12f-NavMesh.GetSettingsByID(navigationAgentType).agentRadius);
            previousPosition=body.position;entry.Initialize(body.position);
            mouthReach=view.MouthTip ? Mathf.Max(Radius,Vector3.Dot(view.MouthTip.position-body.position,transform.forward)) : Radius;
            mouthSide=view.MouthTip ? Vector3.Dot(view.MouthTip.position-body.position,transform.right) : 0;
            mouthHeight=view.MouthTip ? view.MouthTip.position.y-body.position.y : Height*.6f;
            CacheBlade(view.Surface);
        }

        public void ReceiveDamage(float damage,ParvumTarget from,CombatStatusEffectApplication status=default)
        {
            if (Health<=0) return;
            Health=Mathf.Max(0,Health-Mathf.Max(0,damage));
            if (Health<=0) { BeginDeath(); return; }
            // Unlike Parvum/Fuga, every new hit changes the pursued attacker.
            if (from && from.IsAlive)
            {
                suppressSpeakers=Behaviour==LongaArmaBehaviour.ApproachSpeaker;
                attacker=from;Choose(from,LongaArmaBehaviour.PursueAttacker);scan=0;
            }
            if (status.HasEffect) effects=CombatStatusEffectRules.ApplyEffect(effects,status);
            if (damage>0) view.PlayHit();
        }

        bool suppressSpeakers;
        public void NotifyObjectiveConflict(ParvumTarget other)
        {
            if (Health<=0 || !other || !other.IsHostileToParvum || !other.IsAlive || attacker ||
                Vector3.Distance(body.position,other.ClosestPoint(body.position))>12 || !Visible(other)) return;
            attacker=other;Choose(other,LongaArmaBehaviour.PursueAttacker);scan=0;
        }

        void FixedUpdate()
        {
            float dt=Time.fixedDeltaTime;
            if (Health<=0) return;
            support.RemoveWhere(x=>!x || (x is Behaviour b && !b.isActiveAndEnabled));
            if (support.Count>0) Health=Mathf.Min(SeedIntruderRules.LongaArmaHealth,Health+SeedIntruderRules.UrzereSeedHealthRegenPerSecond*dt);
            var tick=CombatStatusEffectRules.TickEffects(effects,dt);effects=tick.Effects;
            if (tick.HealthDamage>0) { ReceiveDamage(tick.HealthDamage,null); if (Health<=0) return; }
            if (CombatStatusEffectRules.BlocksActions(effects)) { Stop();view.Request(LongaArmaMotion.Idle);return; }
            // Complete the committed swing even when its target leaves range/sight.
            // Contact still checks the live target; committing never grants a guaranteed hit.
            if(view.AttackInProgress) { Stop();Behaviour=LongaArmaBehaviour.Attack;return; }
            if (attacker && (!attacker.IsAlive || Vector3.Distance(body.position,attacker.ClosestPoint(body.position))>12 || !Visible(attacker)))
            { returnFromCorridor=InCorridor(body.position);destinationRoom=null;relocationRetryAt=0;
              attacker=null;target=null;route=null;suppressSpeakers=false;scan=0;Diagnostic="Pursuit ended: death, range or sight"; }

            UpdateRoom();scan-=dt;
            if (scan<=0)
            {
                scan=.25f;
                var speaker=suppressSpeakers ? null : NearestSpeaker();
                if (speaker) { attacker=null;Choose(speaker,LongaArmaBehaviour.ApproachSpeaker); }
                else if (attacker) Choose(attacker,LongaArmaBehaviour.PursueAttacker);
                else if (!Relocate() && (!target || !target.IsMetal || !target.IsAlive)) ChooseMetal();
                if (target && (!target.IsMetal || route==null)) PlanTarget();
            }

            // Physical arrival is not a millimetre-exact NavMesh corner. Turn toward the food
            // before the last few centimetres; otherwise friction triggers a false blocked route.
            bool settlingAtFood=target && target.IsMetal && route!=null && route.Length>0 &&
                corner>=route.Length-1 && Vector3.ProjectOnPlane(route[route.Length-1]-body.position,Vector3.up).magnitude<.18f;
            if(!settlingAtFood)foodSettleSince=-1;
            if(settlingAtFood && view.Motion!=LongaArmaMotion.Consume && metalFacing.sqrMagnitude>.1f)
                Face(body.position+metalFacing);
            if (target && target.IsAlive && !(entry.Pending && target.IsMetal) && Visible(target))
            {
                var point=target.ClosestPoint(MouthOrigin);
                float gap=target.IsMetal ? Vector3.Distance(MouthOrigin,point)
                    : Vector3.Distance(body.position+Vector3.up*.45f,target.ClosestPoint(body.position+Vector3.up*.45f));
                float threshold=target.IsMetal ? mouthContactApproach : combatApproach;
                bool feeding=target.IsMetal && view.Motion==LongaArmaMotion.Consume &&
                    Vector3.Distance(body.position,target.ClosestPoint(body.position))<=mouthReach+.25f &&
                    (Time.time-consumeStartedAt<view.ConsumeDuration*2 || mouthContactAt>0 && Time.time-mouthContactAt<view.ConsumeDuration*2);
                bool atMetalGoal=settlingAtFood && gap<=.1f;
                // The mouth briefly enters the wall during the authored bite. Validate visibility
                // from the body at mouth height, not from inside the wall collider.
                if ((feeding || atMetalGoal || gap<=threshold) && (!target.IsMetal || SeedMetalApproach.HasSurface(target,new Vector3(body.position.x,MouthOrigin.y,body.position.z),transform)))
                {
                    Stop();
                    var direction=target.IsMetal && metalFacing.sqrMagnitude>.1f ? metalFacing
                        : Vector3.ProjectOnPlane(target.ClosestPoint(body.position+Vector3.up*.45f)-body.position,Vector3.up);
                    // The observed downstroke is ~19 degrees to the rig's forward axis.
                    // Align before committing, never magnetically track during the swing.
                    if(!target.IsMetal)direction=Quaternion.AngleAxis(-19f,Vector3.up)*direction;
                    if(!feeding) Face(body.position+direction);
                    if (!feeding && direction.sqrMagnitude>.0001f && Vector3.Dot(transform.forward,direction.normalized)<.96f)
                    { view.Request(LongaArmaMotion.Idle); return; }
                    Behaviour=target.IsMetal ? LongaArmaBehaviour.Consume : LongaArmaBehaviour.Attack;
                    if (target.IsMetal && view.Motion!=LongaArmaMotion.Consume) consumeStartedAt=Time.time;
                    view.Request(target.IsMetal ? LongaArmaMotion.Consume : LongaArmaMotion.Attack);
                    return;
                }
            }

            if(settlingAtFood && view.Motion!=LongaArmaMotion.Consume)
            {
                // Do not overwrite the food-facing rotation with the path tangent. The mouth
                // still has to touch the real surface before any consumption damage is applied.
                Face(body.position+metalFacing);
                view.Request(LongaArmaMotion.Idle);
                if(foodSettleSince<0)foodSettleSince=Time.time;
                if(Time.time-foodSettleSince>Mathf.Max(4,view.ConsumeDuration*2))
                {
                    blocked[target]=Time.time+5;target=null;route=null;scan=0;foodSettleSince=-1;
                    Diagnostic="Final food contact obstructed; choose another reachable surface";Stop();return;
                }
                if(Vector3.Dot(transform.forward,metalFacing)<.98f){Stop();return;}
                var remaining=Vector3.ProjectOnPlane(route[route.Length-1]-body.position,Vector3.up);
                var settleVelocity=Vector3.ClampMagnitude(remaining*3f,.5f);
                if(FindSupport(out var settleGround))settleVelocity=Vector3.ProjectOnPlane(settleVelocity,settleGround.normal);
                body.linearVelocity=new Vector3(settleVelocity.x,body.linearVelocity.y,settleVelocity.z);
                return;
            }

            if (!target && !sourceRoom && !returnFromCorridor && !entry.Pending) Behaviour=LongaArmaBehaviour.Search;
            if (route==null || corner>=route.Length) { Stop();view.Request(LongaArmaMotion.Idle);return; }
            if(target && target.IsMetal && corner<route.Length-1 && corner>=route.Length-2 && ClearFinalApproach(route[route.Length-1]))
                corner=route.Length-1;
            // Progress along the path segment, not a large radius around successive corners.
            // Radius-skipping cut curved paths and alternated the two walls' avoidance normals.
            var projected=body.position;
            while(corner<route.Length-1)
            {
                var start=route[Mathf.Max(0,corner-1)];var segment=Vector3.ProjectOnPlane(route[corner]-start,Vector3.up);
                float t=segment.sqrMagnitude>.0001f ? Mathf.Clamp01(Vector3.Dot(body.position-start,segment)/segment.sqrMagnitude) : 1;
                projected=Vector3.Lerp(start,route[corner],t);
                if(t<.98f && Vector3.ProjectOnPlane(route[corner]-body.position,Vector3.up).magnitude>.12f)break;
                corner++;
            }
            var delta=route[corner]-body.position;delta.y=0;
            if(progressCorner!=corner || delta.magnitude<progressDistance-.05f)
            {progressCorner=corner;progressDistance=delta.magnitude;progressAt=Time.time;}
            else if(target && target.IsMetal && Time.time-progressAt>3f)
            {blocked[target]=Time.time+5;target=null;route=null;scan=0;Diagnostic="No progress toward food approach; choose another reachable surface";Stop();return;}
            float arrival=Behaviour==LongaArmaBehaviour.AdvanceIntoRoom || target && target.IsMetal ? .003f : target ? .06f : .15f;
            if (delta.magnitude<arrival) { corner++;Stop();return; }
            // Follow a short arc ahead, not each densely sampled/clearance-adjusted node.
            var aim=route[corner];float lookAhead=.8f-Vector3.ProjectOnPlane(aim-projected,Vector3.up).magnitude;
            if(lookAhead<0 && corner<route.Length-1)aim=Vector3.MoveTowards(projected,aim,.8f);
            for(int i=corner+1;i<route.Length && lookAhead>0;i++)
            {
                float segment=Vector3.ProjectOnPlane(route[i]-aim,Vector3.up).magnitude;
                if(segment>lookAhead){aim=Vector3.Lerp(aim,route[i],lookAhead/segment);break;}
                aim=route[i];lookAhead-=segment;
            }
            var directionMove=Vector3.ProjectOnPlane(aim-body.position,Vector3.up).normalized;
            bool finalMetalApproach=target && target.IsMetal && corner==route.Length-1;
            var navigationFilter=new NavMeshQueryFilter{agentTypeID=navigationAgentType,areaMask=NavMesh.AllAreas};
            if(!finalMetalApproach)
            {
                // The small-seed NavMesh may cut a concave wall between otherwise clear nodes.
                // Steer tangentially before contact while retaining the full physical hull.
                int hits=Physics.CapsuleCastNonAlloc(body.position+Vector3.up*Radius,
                    body.position+Vector3.up*(Height-Radius),Mathf.Max(.01f,Radius-.02f),directionMove,steeringHits,.65f,~0,QueryTriggerInteraction.Ignore);
                float nearest=float.PositiveInfinity;var normal=Vector3.zero;
                for(int i=0;i<hits;i++)
                {
                    var hit=steeringHits[i];
                    if(hit.transform.IsChildOf(transform) || target && hit.collider==target.Surface || Mathf.Abs(hit.normal.y)>.35f ||
                        Vector3.Dot(directionMove,hit.normal)>-.05f || hit.distance>=nearest)continue;
                    nearest=hit.distance;normal=Vector3.ProjectOnPlane(hit.normal,Vector3.up).normalized;
                }
                if(normal.sqrMagnitude>.1f)
                {
                    var tangent=Vector3.ProjectOnPlane(directionMove,normal);
                    if(tangent.sqrMagnitude>.01f)directionMove=Vector3.Lerp(directionMove,tangent.normalized,Mathf.Clamp01((.65f-nearest)/.65f)).normalized;
                }
            }
            var desiredHeading=directionMove;
            if(movementHeading.sqrMagnitude<.1f)movementHeading=body.rotation*Vector3.forward;
            var filteredHeading=Vector3.Slerp(movementHeading,directionMove,1-Mathf.Exp(-8f*dt)).normalized;
            movementHeading=Vector3.RotateTowards(movementHeading,filteredHeading,120f*Mathf.Deg2Rad*dt,0).normalized;
            directionMove=movementHeading;
            Face(body.position+directionMove);
            float alignment=Vector3.Dot(body.rotation*Vector3.forward,directionMove);
            if(alignment<.25f) { Stop();view.Request(LongaArmaMotion.Idle);return; }
            float speed=(SeedIntruderRules.LongaArmaMovementSpeed+(support.Count>0 ? SeedIntruderRules.UrzereSeedMovementSpeedBonus : 0))*CombatStatusEffectRules.CalculateMovementMultiplier(effects);
            speed*=Mathf.Lerp(.35f,1,Mathf.InverseLerp(.25f,.98f,alignment));
            // Body/filtered-heading alignment alone stays high even when the goal is behind.
            // Turn without advancing away, then brake into the endpoint instead of orbiting it.
            if(Behaviour==LongaArmaBehaviour.ApproachMetal || Behaviour==LongaArmaBehaviour.Search)
            {
                speed*=Mathf.Clamp01(Vector3.Dot(directionMove,desiredHeading));
                if(corner==route.Length-1)speed=Mathf.Min(speed,delta.magnitude*3f);
            }
            speed=Mathf.Min(speed,delta.magnitude/dt);
            if (Behaviour==LongaArmaBehaviour.AdvanceIntoRoom && entry.Crossed)
                speed=Mathf.Min(speed,Mathf.Max(.25f,Mathf.Max(0,SeedRoomEntry.AdvanceDistance-entry.Depth(body.position))/dt));
            var velocity=directionMove*speed;
            // Look-ahead and gradual turning must not cut through a navigation-only fixture.
            // Keep physical movement tangential at its clearance boundary while the body turns.
            if(NavMesh.SamplePosition(body.position,out var stepStart,.6f,navigationFilter) &&
                NavMesh.Raycast(stepStart.position,stepStart.position+velocity*dt,out var stepEdge,navigationFilter))
            {
                var inward=Vector3.ProjectOnPlane(stepEdge.normal,Vector3.up).normalized;
                if(Vector3.Dot(velocity,inward)<0)velocity=Vector3.ProjectOnPlane(velocity,inward);
            }
            speed=velocity.magnitude;
            bool grounded=FindSupport(out var ground);
            if (grounded)
                velocity=Vector3.ProjectOnPlane(velocity,ground.normal).normalized*speed;
            float vertical=grounded ? velocity.y : body.linearVelocity.y;
            // A walkable slope is not a step: injecting step lift here makes a sawtooth hop.
            if ((!grounded || ground.normal.y>.995f) && TryStep(directionMove,out float rise)) vertical=Mathf.Max(vertical,Mathf.Min(speed,rise/dt));
            body.linearVelocity=new Vector3(velocity.x,vertical,velocity.z);
            view.SetLocomotionSpeed(velocity.magnitude);
            view.Request(speed>.01f ? LongaArmaMotion.Move : LongaArmaMotion.Idle);
            stuck=speed>.05f && Vector3.Distance(previousPosition,body.position)<.002f ? stuck+dt : 0;previousPosition=body.position;
            if (stuck>1.5f)
            { if (target && target.IsMetal) blocked[target]=Time.time+5;target=null;route=null;scan=0;stuck=0;Diagnostic="Replan blocked route"; }
        }

        void Choose(ParvumTarget next,LongaArmaBehaviour state)
        {
            if (target!=next) { target=next;route=null;previousVertices=null;lastStrikeCycle=-1;mouthContactAt=0;wasConsuming=false;foodSettleSince=-1; }
            Behaviour=state;
        }

        ParvumTarget RoomBelow(Vector3 p)
        {
            var hit=Physics.RaycastAll(p+Vector3.up*.45f,Vector3.down,1.6f,~0,QueryTriggerInteraction.Ignore)
                .Where(x=>!x.transform.IsChildOf(transform) && x.normal.y>.45f).OrderBy(x=>x.distance).FirstOrDefault();
            if (!hit.collider) return null;
            return ParvumTarget.Active.FirstOrDefault(x=>x.IsRoomWall && x.transform.root==hit.transform.root);
        }

        bool InCorridor(Vector3 position)
        {
            var room=RoomBelow(position);
            if(!room)return true;
            // Room-owned entrance liners can extend outside the actual door boundary.
            foreach(var door in entry.Doors)
            {
                if(door.Room.Room!=room.Room || door.Room.Ship!=room.Ship)continue;
                var offset=position-door.Point;float depth=Vector3.Dot(offset,door.Inward);
                if(depth<-.02f && depth>-10 && Mathf.Abs(Vector3.Dot(offset,Vector3.Cross(Vector3.up,door.Inward)))<2.5f &&
                    position.y<door.Point.y+1 && position.y>door.Point.y-8)return true;
            }
            return false;
        }

        void UpdateRoom()
        {
            var room=RoomBelow(body.position);if (room) occupied=room;
            if (room && !room.IsAlive && !sourceRoom) sourceRoom=room;
            if (entry.Observe(body.position))
            { route=null;scan=0;if (entry.Pending && !attacker && (!target || target.IsMetal)) Choose(null,LongaArmaBehaviour.AdvanceIntoRoom); }
        }

        bool Relocate()
        {
            if (entry.Pending)
            {
                if (entry.TryComplete(body.position))
                { if (entry.Door.Room && entry.Door.Room.IsAlive) { sourceRoom=null;destinationRoom=null;returnFromCorridor=false; } route=null; }
                else
                {
                    Choose(null,LongaArmaBehaviour.AdvanceIntoRoom);
                    var goal=entry.Goal;
                    var floor=Physics.RaycastAll(goal+Vector3.up*.2f,Vector3.down,9,~0,QueryTriggerInteraction.Ignore)
                        .Where(x=>x.normal.y>.45f && x.point.y<entry.Door.Point.y-.5f && entry.Door.Room && x.transform.root==entry.Door.Room.transform.root)
                        .OrderBy(x=>x.distance).FirstOrDefault();
                    if (floor.collider) goal.y=floor.point.y;
                    if (PathTo(goal,out route)) { corner=route.Length>1 ? 1 : 0;Diagnostic=$"Entry route to {goal:F2} corners={route.Length}"; }
                    else Diagnostic=$"No entry route to {goal:F2}";
                    return true;
                }
            }
            if (!sourceRoom && !returnFromCorridor) return false;
            if (!returnFromCorridor && occupied && occupied.Room==sourceRoom.Room && sourceRoom.IsAlive)
            { sourceRoom=null;destinationRoom=null;return false; }
            Choose(null,LongaArmaBehaviour.RelocateRoom);
            if (destinationRoom && destinationRoom.IsAlive && route!=null && corner<route.Length) return true;
            if (Time.time<relocationRetryAt) return true;
            relocationRetryAt=Time.time+2f;
            float best=float.PositiveInfinity;Vector3[] bestPath=null;destinationRoom=null;
            var ship=sourceRoom ? sourceRoom.Ship : occupied ? occupied.Ship : null;
            foreach (var group in ParvumTarget.Active.Where(x=>x.IsRoomWall && x.IsAlive &&
                (returnFromCorridor || x.Room!=sourceRoom.Room) && (!ship || x.Ship==ship)).GroupBy(x=>x.Room))
            {
                var walls=group.ToArray();var bounds=walls[0].Surface.bounds;
                foreach (var wall in walls) bounds.Encapsulate(wall.Surface.bounds);
                foreach (var door in entry.Doors.Where(x=>x.Room.Room==group.Key && (!ship || x.Room.Ship==ship)))
                {
                    var point=door.Point+door.Inward*(SeedRoomEntry.AdvanceDistance+.05f);
                    var floor=Physics.RaycastAll(point+Vector3.up*.2f,Vector3.down,9,~0,QueryTriggerInteraction.Ignore)
                        .Where(x=>x.normal.y>.45f && x.transform.root==walls[0].transform.root)
                        .OrderBy(x=>x.distance).FirstOrDefault();
                    point.y=floor.collider ? floor.point.y : bounds.min.y+.2f;
                    if (!PathTo(point,out var path) || RoomBelow(path[path.Length-1]+Vector3.up*.3f)?.Room!=group.Key) continue;
                    float length=0;for (int i=1;i<path.Length;i++) length+=Vector3.Distance(path[i-1],path[i]);
                    if (length>=best) continue;
                    best=length;bestPath=path;destinationRoom=walls[0];
                }
                if(returnFromCorridor)continue; // Return through a real doorway, never aim at an arbitrary room point.
                for (int xi=1;xi<=3;xi++) for (int zi=1;zi<=3;zi++)
                {
                    var point=new Vector3(Mathf.Lerp(bounds.min.x,bounds.max.x,xi*.25f),bounds.min.y+.2f,
                        Mathf.Lerp(bounds.min.z,bounds.max.z,zi*.25f));
                    if (!PathTo(point,out var path) || RoomBelow(path[path.Length-1]+Vector3.up*.3f)?.Room!=group.Key) continue;
                    float length=0;for (int i=1;i<path.Length;i++) length+=Vector3.Distance(path[i-1],path[i]);
                    if (length>=best) continue;
                    best=length;bestPath=path;destinationRoom=walls[0];
                }
            }
            route=bestPath;corner=route!=null && route.Length>1 ? 1 : 0;
            return true;
        }

        void ChooseMetal()
        {
            ParvumTarget best=null;float nearest=ParvumGameplayRules.MetalRange;
            foreach (var food in ParvumTarget.Active)
            {
                if (!food.IsMetal || !food.IsAlive || blocked.TryGetValue(food,out var until) && Time.time<until) continue;
                float distance=Vector3.Distance(body.position,food.ClosestPoint(body.position));
                if (distance>=nearest || !CanApproach(food,out _)) continue;
                nearest=distance;best=food;
            }
            if (entry.DeferMetal(best,body.position)) { Choose(null,LongaArmaBehaviour.AdvanceIntoRoom);route=null;scan=0;return; }
            Choose(best,best ? LongaArmaBehaviour.ApproachMetal : LongaArmaBehaviour.Search);
            if (!best && (route==null || corner>=route.Length)) PlanSearch();
        }

        ParvumTarget NearestSpeaker()
        { return ParvumTarget.Active.Where(x=>x.Audible && Vector3.Distance(body.position,x.transform.position)<ParvumGameplayRules.SpeakerRange && CanApproach(x,out _))
                .OrderBy(x=>Vector3.Distance(body.position,x.transform.position)).FirstOrDefault(); }

        bool CanApproach(ParvumTarget candidate,out Vector3[] result,bool rememberFacing=false)
        {
            result=null;if (!candidate || !candidate.Surface) return false;
            // A stand-off point across a curved doorway is not a safe straight approach.
            // Follow the creature's floor path; the existing live melee gap stops the body.
            if(candidate.Kind==ParvumTargetKind.Creature)
            {
                var feet=candidate.Surface.bounds.center;feet.y=candidate.Surface.bounds.min.y+.1f;
                bool reachable=PathTo(feet,out result,true);
                ApproachDiagnostic=candidate.name+(reachable ? ": follow walkable feet path" : ": no feet path");
                return reachable;
            }
            string rejected="no candidate surface";
            var origin=body.position+Vector3.up*(candidate.IsMetal ? mouthHeight : .45f);
            var surface=candidate.ClosestPoint(origin);var away=Vector3.ProjectOnPlane(body.position-surface,Vector3.up).normalized;
            if (away==Vector3.zero) away=-transform.forward;
            var tangent=Vector3.Cross(Vector3.up,away);
            float gap=candidate.IsMetal ? metalApproach : combatApproach;
            for (int i=0;i<11;i++)
            {
                float side=i==0 ? 0 : ((i+1)/2)*.4f*(i%2==0 ? -1 : 1);
                var probe=origin+tangent*side;var point=candidate.ClosestPoint(probe);
                var normal=Vector3.ProjectOnPlane(probe-point,Vector3.up).normalized;
                if(candidate.IsMetal)
                {
                    // At a curved section's end, closest-point direction can run along the wall.
                    // Stand perpendicular to the actual contact face, not that edge direction.
                    var cast=point-probe;
                    if(cast.sqrMagnitude<.000001f){rejected="no wall direction";continue;}
                    if(!candidate.Surface.Raycast(new Ray(probe,cast.normalized),out var face,cast.magnitude+.05f) &&
                        !candidate.Surface.Raycast(new Ray(point+cast.normalized*.1f,-cast.normalized),out face,cast.magnitude+.15f))
                    {rejected="no facing wall surface at candidate";continue;}
                    point=face.point;normal=Vector3.ProjectOnPlane(face.normal,Vector3.up).normalized;
                    if(Vector3.Dot(normal,probe-point)<0)normal=-normal;
                }
                if (normal==Vector3.zero) { rejected="no outward normal";continue; }
                // The mouth projects ahead of the rigidbody. Place the body so the
                // animated mouth, rather than the capsule centre, reaches the wall.
                var standOff=candidate.IsMetal ? Mathf.Max(Radius+.02f,mouthReach+mouthContactApproach) : gap;
                var goal=point+normal*standOff;
                if(candidate.IsMetal)goal-=Quaternion.LookRotation(-normal)*Vector3.right*mouthSide;
                if (candidate.Kind==ParvumTargetKind.Creature) goal.y=candidate.Surface.bounds.min.y;
                else
                {
                    // Wall closest-points can be high above the walkable deck. Route to its floor,
                    // then check the actual mouth-to-wall surface separately.
                    var floor=Physics.RaycastAll(goal+Vector3.up*.5f,Vector3.down,6,~0,QueryTriggerInteraction.Ignore)
                        .Where(x=>!x.transform.IsChildOf(transform) && x.normal.y>.45f && x.transform.root==candidate.transform.root)
                        .OrderBy(x=>x.distance).FirstOrDefault();
                    goal.y=floor.collider ? floor.point.y : body.position.y;
                }
                if (!PathTo(goal,out var path)) { rejected=$"no nav path to {goal:F2}";continue; }
                if(candidate.IsMetal)
                {
                    var bodyGoal=path[path.Length-1];bodyGoal.y=goal.y;
                    if(!ClearFeedingBody(bodyGoal,Quaternion.LookRotation(-normal)))
                    {rejected="feeding stance is occupied";continue;}
                    var expectedMouth=path[path.Length-1]+Quaternion.LookRotation(-normal)*new Vector3(mouthSide,0,mouthReach);
                    // NavMesh height includes bake clearance; the physical body stands on the real deck.
                    expectedMouth.y=goal.y+mouthHeight;
                    if(Vector3.Distance(expectedMouth,candidate.ClosestPoint(expectedMouth))>.045f)
                    {rejected=$"mouth endpoint gap={Vector3.Distance(expectedMouth,candidate.ClosestPoint(expectedMouth)):F3} expected={expectedMouth:F3} contact={candidate.ClosestPoint(expectedMouth):F3} goal={goal:F3} nav={path[path.Length-1]:F3}";continue;}
                }
                var at=path[path.Length-1]+Vector3.up*.45f;
                if (candidate.IsMetal && !SeedMetalApproach.HasSurface(candidate,at,transform)) { rejected=$"no contact surface from {at:F2}";continue; }
                if (!VisibleFrom(at,candidate)) { rejected=$"surface blocked from {at:F2}";continue; }
                result=path;if(rememberFacing && candidate.IsMetal)metalFacing=-normal;
                ApproachDiagnostic=candidate.name+": reachable";return true;
            }
            ApproachDiagnostic=candidate.name+": "+rejected;
            // A horizontal stand-off can fall off a sloped corridor. Follow the target's
            // walkable foot position instead; FixedUpdate still stops at melee range.
            if(candidate.Kind==ParvumTargetKind.Creature)
            {
                var feet=candidate.Surface.bounds.center;feet.y=candidate.Surface.bounds.min.y+.1f;
                if(PathTo(feet,out result)) { ApproachDiagnostic=candidate.name+": follow feet on slope";return true; }
            }
            return false;
        }

        bool PathTo(Vector3 point,out Vector3[] path,bool followReachableEdge=false)
        {
            path=null;var filter=new NavMeshQueryFilter { agentTypeID=navigationAgentType,areaMask=NavMesh.AllAreas };
            if (!NavMesh.SamplePosition(body.position,out var from,.6f,filter) || !NavMesh.SamplePosition(point,out var to,.75f,filter)) return false;
            var calculated=new NavMeshPath();
            if (!NavMesh.CalculatePath(from.position,to.position,filter,calculated) ||
                calculated.status==NavMeshPathStatus.PathInvalid ||
                calculated.status==NavMeshPathStatus.PathPartial && !followReachableEdge) return false;
            // A creature can stand on an isolated navigation island near room fixtures.
            // Pursue along the connected floor up to its edge, never bridge the gap directly.
            // Actual visibility/range and animated blade contact still decide any attack.
            var original=calculated.corners;
            var dense=new List<Vector3>();
            if(original.Length>0)dense.Add(original[0]);
            for(int i=1;i<original.Length;i++)
            {
                int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(original[i-1],original[i])/.65f));
                for(int j=1;j<=steps;j++)dense.Add(Vector3.Lerp(original[i-1],original[i],(float)j/steps));
            }
            path=dense.ToArray();
            // Existing navigation was baked for smaller seeds. Keep this enlarged hull
            // off doorway corners without changing shared navigation or room geometry.
            for(int i=1;i<path.Length-1;i++)
            {
                var clearPoint=(path[i-1]+path[i]*2+path[i+1])*.25f;
                for(int pass=0;pass<4;pass++)
                {
                    var correction=Vector3.zero;
                    int count=Physics.OverlapSphereNonAlloc(clearPoint+Vector3.up*(Height*.5f),Height+Radius,clearanceHits,~0,QueryTriggerInteraction.Ignore);
                    for(int j=0;j<count;j++)
                    {
                        var obstacle=clearanceHits[j];
                        if(!obstacle || obstacle.transform.IsChildOf(transform) || target && obstacle==target.Surface)continue;
                        if(Physics.ComputePenetration(hull,clearPoint+Vector3.up*.04f,body.rotation,obstacle,obstacle.transform.position,obstacle.transform.rotation,out var direction,out float distance) && Mathf.Abs(direction.y)<.5f)
                            correction+=Vector3.ProjectOnPlane(direction,Vector3.up)*(distance+.025f);
                    }
                    // Horizontal rays support concave corridor meshes as well as primitives.
                    // Average the wall margins so tessellated surfaces cannot multiply the push.
                    var margin=Vector3.zero;int marginCount=0;
                    for(int directionIndex=0;directionIndex<8;directionIndex++)
                    {
                        var rayDirection=Quaternion.AngleAxis(directionIndex*45,Vector3.up)*Vector3.forward;
                        if(!Physics.Raycast(clearPoint+Vector3.up*(Height*.5f),rayDirection,out var wall,Radius+.18f,~0,QueryTriggerInteraction.Ignore) ||
                            wall.transform.IsChildOf(transform) || target && wall.collider==target.Surface || Mathf.Abs(wall.normal.y)>.35f)continue;
                        margin+=Vector3.ProjectOnPlane(wall.normal,Vector3.up).normalized*(Radius+.18f-wall.distance);marginCount++;
                    }
                    if(marginCount>0)correction+=margin/marginCount;
                    if(NavMesh.SamplePosition(clearPoint,out var onFloor,.3f,filter) &&
                        NavMesh.FindClosestEdge(onFloor.position,out var edge,filter))
                    {
                        var inward=Vector3.ProjectOnPlane(onFloor.position-edge.position,Vector3.up);
                        if(inward.magnitude>.001f && inward.magnitude<navigationMargin)
                            correction+=inward.normalized*(navigationMargin-inward.magnitude);
                    }
                    if(correction.sqrMagnitude<.000001f)break;
                    clearPoint+=correction;
                }
                if(NavMesh.SamplePosition(clearPoint,out var clear,.3f,filter))path[i]=clear.position;
            }
            return path.Length>0;
        }

        bool ClearFinalApproach(Vector3 point)
        {
            var delta=Vector3.ProjectOnPlane(point-body.position,Vector3.up);
            if(delta.magnitude>1.5f)return false;
            var filter=new NavMeshQueryFilter{agentTypeID=navigationAgentType,areaMask=NavMesh.AllAreas};
            if(!NavMesh.SamplePosition(body.position,out var start,.6f,filter) || NavMesh.Raycast(start.position,point,out _,filter))return false;
            int count=Physics.CapsuleCastNonAlloc(body.position+Vector3.up*Radius,body.position+Vector3.up*(Height-Radius),
                Radius-.01f,delta.normalized,steeringHits,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
                if(!steeringHits[i].transform.IsChildOf(transform) && Mathf.Abs(steeringHits[i].normal.y)<.5f &&
                    Vector3.Dot(delta,steeringHits[i].normal)<-.001f)return false;
            return true;
        }

        bool ClearFeedingBody(Vector3 point,Quaternion rotation)
        {
            // Open concave wall meshes do not always report penetration from their back face.
            // Reject a stance crossing any neighbouring wall, not only the selected food surface.
            var center=point+Vector3.up*(Height*.5f);
            for(int ray=0;ray<16;ray++)
            {
                var direction=Quaternion.AngleAxis(ray*22.5f,Vector3.up)*Vector3.forward;
                int hits=Physics.RaycastNonAlloc(center,direction,steeringHits,Radius+.025f,~0,QueryTriggerInteraction.Ignore);
                for(int i=0;i<hits;i++)
                    if(!steeringHits[i].transform.IsChildOf(transform) && Mathf.Abs(steeringHits[i].normal.y)<.5f)return false;
                hits=Physics.RaycastNonAlloc(center+direction*(Radius+.025f),-direction,steeringHits,Radius+.025f,~0,QueryTriggerInteraction.Ignore);
                for(int i=0;i<hits;i++)
                    if(!steeringHits[i].transform.IsChildOf(transform) && Mathf.Abs(steeringHits[i].normal.y)<.5f)return false;
            }
            int count=Physics.OverlapCapsuleNonAlloc(point+Vector3.up*Radius,point+Vector3.up*(Height-Radius),Radius,clearanceHits,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var other=clearanceHits[i];if(other.transform.IsChildOf(transform))continue;
                if(other is CharacterController character)
                {
                    var characterCenter=character.transform.TransformPoint(character.center);
                    if(point.y+Height>character.bounds.min.y && point.y<character.bounds.max.y &&
                        Vector3.ProjectOnPlane(point-characterCenter,Vector3.up).magnitude<Radius+character.radius+.04f)return false;
                }
                else if(Physics.ComputePenetration(hull,point+Vector3.up*.02f,rotation,other,other.transform.position,other.transform.rotation,out var direction,out float depth) && depth>.025f && Mathf.Abs(direction.y)<.5f)return false;
            }
            return true;
        }

        void PlanTarget()
        {
            if(!target)return;
            var point=target.Surface ? target.Surface.bounds.center : target.transform.position;
            if(!target.IsMetal && route!=null && corner<route.Length && Vector3.Distance(point,plannedTargetPosition)<.6f)return;
            if (CanApproach(target,out var path,true)) { route=path;corner=path.Length>1 ? 1 : 0;progressCorner=-1;plannedTargetPosition=point;Diagnostic="Walkable approach path"; }
            else { route=null;Diagnostic="No approach path"; }
        }
        void PlanSearch()
        {
            for (int i=0;i<12;i++)
            {
                var offset=Random.insideUnitCircle*8;if (offset.magnitude<1) continue;
                if (PathTo(body.position+new Vector3(offset.x,0,offset.y),out var path))
                { route=path;corner=path.Length>1 ? 1 : 0;break; }
            }
        }

        bool Visible(ParvumTarget candidate)
        {
            if(!candidate || !candidate.Surface)return false;
            if(candidate.Kind!=ParvumTargetKind.Creature)return VisibleFrom(body.position+Vector3.up*.45f,candidate);
            // Use the actual head height, not the old 45 cm ray which hits ramps/thresholds.
            var origin=view.MouthTip ? view.MouthTip.position : body.position+Vector3.up*(Height*.75f);
            var bounds=candidate.Surface.bounds;
            return ClearSight(origin,bounds.center,candidate) ||
                ClearSight(origin,bounds.center+Vector3.up*(bounds.extents.y*.65f),candidate) ||
                ClearSight(origin,bounds.center-Vector3.up*(bounds.extents.y*.5f),candidate);
        }
        bool VisibleFrom(Vector3 origin,ParvumTarget candidate)
        {
            return ClearSight(origin,candidate.ClosestPoint(origin),candidate);
        }
        bool ClearSight(Vector3 origin,Vector3 endpoint,ParvumTarget candidate)
        {
            var delta=endpoint-origin;
            if (delta.sqrMagnitude<.000001f) return true;
            return !Physics.RaycastAll(origin,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore)
                .Any(x=>!x.transform.IsChildOf(transform) && x.collider!=candidate.Surface && !x.transform.IsChildOf(candidate.transform));
        }

        bool FindSupport(out RaycastHit supportHit)
        {
            supportHit=default;float nearest=float.PositiveInfinity;
            // Match the capsule's support footprint rather than a point ray at its centre.
            int count=Physics.SphereCastNonAlloc(body.position+Vector3.up*(Radius+.3f),Radius-.03f,
                Vector3.down,groundHits,.5f,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var hit=groundHits[i];
                if(hit.transform.IsChildOf(transform)||hit.normal.y<.5f||hit.distance>=nearest)continue;
                supportHit=hit;nearest=hit.distance;
            }
            return supportHit.collider && nearest<.43f;
        }

        bool TryStep(Vector3 direction,out float rise)
        {
            rise=0;float max=ParvumGameplayRules.MaximumStepHeight;
            bool obstacle=false;
            foreach (var height in new[] { .04f,.2f,Height*.5f })
                if (Physics.RaycastAll(body.position+Vector3.up*height,direction,Radius+.15f,~0,QueryTriggerInteraction.Ignore)
                    .Any(x=>!x.transform.IsChildOf(transform) && x.normal.y<=.6f)) { obstacle=true;break; }
            if (!obstacle || !Physics.Raycast(body.position+direction*(Radius+.15f)+Vector3.up*(max+.04f),Vector3.down,out var landing,max+.02f,~0,QueryTriggerInteraction.Ignore)) return false;
            rise=landing.point.y-body.position.y;
            if (rise<=.015f || rise>max || landing.normal.y<.995f) return false;
            return !Physics.CapsuleCastAll(body.position+Vector3.up*Radius,body.position+Vector3.up*(Height-Radius),Radius-.005f,Vector3.up,rise+.02f,~0,QueryTriggerInteraction.Ignore)
                .Any(x=>!x.transform.IsChildOf(transform));
        }

        void Face(Vector3 point)
        { var delta=Vector3.ProjectOnPlane(point-body.position,Vector3.up);if (delta.sqrMagnitude>.0001f) body.MoveRotation(Quaternion.RotateTowards(body.rotation,Quaternion.LookRotation(delta),360*Time.fixedDeltaTime)); }
        void Stop() { movementHeading=Vector3.zero;if (body && !body.isKinematic) body.linearVelocity=new Vector3(0,body.linearVelocity.y,0); }

        static bool BladeBone(Transform bone,Transform root)
        { while (bone && bone!=root) { if (bone.name=="R_frontleg1" || bone.name=="R_frontleg2") return true;bone=bone.parent; } return false; }

        // Bone membership is immutable for this renderer. Preserve every qualifying vertex,
        // but resolve its hierarchy once rather than thousands of times per attack frame.
        void CacheBlade(SkinnedMeshRenderer renderer)
        {
            if(!renderer || !renderer.sharedMesh)return;
            if(cachedBladeRenderer==renderer && cachedBladeMesh==renderer.sharedMesh)return;
            cachedBladeRenderer=renderer;cachedBladeMesh=renderer.sharedMesh;
            var bones=renderer.bones;var weights=cachedBladeMesh.boneWeights;
            var bladeBones=new bool[bones.Length];
            for(int i=0;i<bones.Length;i++)bladeBones[i]=BladeBone(bones[i],renderer.transform);
            bladeIndices.Clear();
            for(int i=0;i<weights.Length && i<cachedBladeMesh.vertexCount;i++)
            {
                var w=weights[i];
                if(w.weight0>.05f && w.boneIndex0<bones.Length && bladeBones[w.boneIndex0] ||
                   w.weight1>.05f && w.boneIndex1<bones.Length && bladeBones[w.boneIndex1] ||
                   w.weight2>.05f && w.boneIndex2<bones.Length && bladeBones[w.boneIndex2] ||
                   w.weight3>.05f && w.boneIndex3<bones.Length && bladeBones[w.boneIndex3])bladeIndices.Add(i);
            }
            bakedVertices.Capacity=Mathf.Max(bakedVertices.Capacity,cachedBladeMesh.vertexCount);
            bladePositions=new Vector3[bladeIndices.Count];bladeHistory=new Vector3[bladeIndices.Count];
            previousVertices=null;
        }

        // Editor-only observations measure the live attack, never substitute a forced pose.
#if UNITY_EDITOR
        public readonly List<Vector3> AttackTimings=new List<Vector3>(4096);
#endif
        void LateUpdate()
        {
#if UNITY_EDITOR
            bool attacking=view && view.Motion==LongaArmaMotion.Attack;
            long started=System.Diagnostics.Stopwatch.GetTimestamp();
            long allocated=System.GC.GetAllocatedBytesForCurrentThread();
            try { UpdateAnimatedContact(); }
            finally
            {
                if(attacking && AttackTimings.Count<4096)
                    AttackTimings.Add(new Vector3((float)((System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000.0/System.Diagnostics.Stopwatch.Frequency),
                        System.GC.GetAllocatedBytesForCurrentThread()-allocated,Time.unscaledDeltaTime*1000));
            }
#else
            UpdateAnimatedContact();
#endif
        }

        void UpdateAnimatedContact()
        {
            if (Health<=0 || !target || !target.IsAlive || entry.Pending && target.IsMetal) return;
            if (view.Motion==LongaArmaMotion.Consume)
            {
                var mouth=view.MouthTip;if (!mouth) return;
                var point=target.ClosestPoint(mouth.position);
                if (Vector3.Distance(mouth.position,point)<.045f || wasConsuming && target.IntersectsMouthSweep(previousBladeTip,mouth.position)) mouthContactAt=Time.time;
                previousBladeTip=mouth.position;
                wasConsuming=true;
                if (mouthContactAt>0 && Time.time-mouthContactAt<1.25f && SeedMetalApproach.HasSurface(target,new Vector3(body.position.x,mouth.position.y,body.position.z),transform))
                    target.ReceiveLongaArmaConsumption(Time.deltaTime);
                return;
            }
            wasConsuming=false;
            if (view.Motion!=LongaArmaMotion.Attack) { previousVertices=null;sampledCycle=-1;return; }
            var renderer=view.Surface;var tip=view.BladeTip;
            if (!renderer || !tip) return;
            int cycle=view.AttackCycle;
            if (cycle!=sampledCycle)
            { sampledCycle=cycle;initialBladeTip=tip.position;previousBladeTip=tip.position;raised=false;previousVertices=null; }
            if (tip.position.y>initialBladeTip.y+.08f) raised=true;
            previousBladeTip=tip.position;
            // Live source observation: lift/hold precedes 0.92 s; the first slam ends
            // near 1.10 s in the unchanged 2.5 s cycle. Never sweep lift vertices into it.
            if(view.MotionTime<.92f || view.MotionTime>1.10f)
            { previousVertices=null;BladeSurfaceDistance=float.PositiveInfinity;return; }
            if (cycle==lastStrikeCycle ||
                Vector3.Distance(body.position,target.ClosestPoint(body.position))>SeedIntruderRules.LongaArmaAttackRange || !Visible(target)) return;
            if (!contactMesh) contactMesh=new Mesh { name="Longa Arma live blade contact" };
            CacheBlade(renderer);
            renderer.BakeMesh(contactMesh,true);
            contactMesh.GetVertices(bakedVertices);
            bool same=previousRenderer==renderer && previousVertices!=null;
            BladeSurfaceDistance=float.PositiveInfinity;
            var matrix=renderer.localToWorldMatrix;
            for(int i=0;i<bladeIndices.Count;i++)bladePositions[i]=matrix.MultiplyPoint3x4(bakedVertices[bladeIndices[i]]);
            for (int i=0;i<bladeIndices.Count;i++)
            {
                float separation=Vector3.Distance(bladePositions[i],target.ClosestPoint(bladePositions[i]));
                BladeSurfaceDistance=Mathf.Min(BladeSurfaceDistance,separation);
                if (separation>.025f && !(same && target.IntersectsMouthSweep(previousVertices[i],bladePositions[i]))) continue;
                lastStrikeCycle=cycle;target.ReceiveLongaArmaStrike(SeedIntruderRules.LongaArmaDamage*AttackMultiplier);break;
            }
            previousVertices=bladePositions;bladePositions=bladeHistory;bladeHistory=previousVertices;previousRenderer=renderer;
        }

        void BeginDeath()
        {
            Behaviour=LongaArmaBehaviour.Dead;deathAt=Time.time;target=null;attacker=null;route=null;
            body.linearVelocity=Vector3.zero;body.isKinematic=true;hull.enabled=false;
            view.Request(LongaArmaMotion.Death);
            Destroy(gameObject,view.DeathDuration+1.5f);
        }

        void OnDestroy() { if (contactMesh) Destroy(contactMesh); }
    }
}
