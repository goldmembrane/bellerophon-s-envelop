using System;
using System.Linq;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using Bellerophon.Enemies.Parvum;
using UnityEngine;

namespace Bellerophon.Core.Player
{
    // Scene-local adapter: original rig/clips remain shared and unchanged.
    public sealed class PegasusStickController : MonoBehaviour
    {
        public enum Phase { Carry, Grip, Strike, Return, Ready, Aim, Release, Cancel, Empty }
        [SerializeField] Animator animator;
        [SerializeField] Transform visual;
        [SerializeField] Transform heldStick;
        [SerializeField] Camera view;
        [SerializeField] AnimationClip grip, strike, returnClip, ready, release, cancel;
        [SerializeField] float releaseTime;
        FirstPersonPlayerInput input;
        ShipDeviceInteractionState ship;
        ParvumTarget attacker;
        float elapsed, nextAttack;
        bool impacted, released;
        bool throwRequested; // Preserve a click during the original preparation motion; cancel clears it.
        bool throwModeRequested; // User-selected mode persists across attack/return/cancel motions.
        bool pendingUseInput, pendingAimInput; // Resolve same-frame mode selection before use.
        int reservedSlot;
        Vector3 gripPosition, gripScale;
        Quaternion gripRotation;
        Vector3 previousBase, previousTip;
        float previousTipHeight, lowestTipHeight, highestTipHeight;
        bool sweepReady, raisedForStrike;
        StickAnimatedSurface[] surfaces=Array.Empty<StickAnimatedSurface>();
        public Vector3 ContactPoint { get; private set; }
        public bool ContactWindow { get; private set; }
        public float LastContactTime { get; private set; }
        public Phase CurrentPhase { get; private set; }
        public bool ThrowMode => throwModeRequested;
        public bool HasStick { get; private set; } = true;
        public int HitCount { get; private set; }
        public string LastHit { get; private set; }
        public float PhaseTime => elapsed;
        public PegasusThrownStick Projectile { get; private set; }
        const float GripSeconds = .32f;
        const float ReturnSeconds = .32f;
        float StrikeSeconds => EquipmentRules.StickUseDelaySeconds - GripSeconds - ReturnSeconds;
        public void Configure(Animator anim, Transform prop, Camera camera, AnimationClip[] clips, float releaseAt)
        { animator=anim;visual=anim.transform;heldStick=prop;view=camera;grip=clips[0];strike=clips[1];returnClip=clips[2];ready=clips[3];release=clips[4];cancel=clips[5];releaseTime=releaseAt; }
        void Awake()
        {
            input=GetComponent<FirstPersonPlayerInput>();attacker=GetComponent<ParvumTarget>();
            ship=FindFirstObjectByType<ShipDeviceInteractionState>();
            gripPosition=heldStick.localPosition;gripRotation=heldStick.localRotation;gripScale=heldStick.localScale;
        }
        void OnEnable()
        {
            if(!input)input=GetComponent<FirstPersonPlayerInput>();
            input.UsePressed+=QueueUse;input.AimPressed+=QueueAim;input.DropPressed+=Drop;
        }
        void OnDisable()
        { if(input){input.UsePressed-=QueueUse;input.AimPressed-=QueueAim;input.DropPressed-=Drop;}pendingUseInput=pendingAimInput=false; }
        void QueueUse(){pendingUseInput=true;}
        void QueueAim(){pendingAimInput=!pendingAimInput;}
        void OnDestroy(){ClearSurfaces();}
        void ClearSurfaces(){foreach(var surface in surfaces)surface.Dispose();surfaces=Array.Empty<StickAnimatedSurface>();}
        void Start()
        {
            if(ship){reservedSlot=ship.CurrentEquipmentState.ActiveHandSlotIndex;ship.SetEquipmentState(ship.CurrentEquipmentState.WithHandSlot(reservedSlot,EquipmentSlotState.One(EquipmentItemKind.Stick)));}
            SetPhase(Phase.Carry);
        }
        public void Use()
        {
            if(!HasStick || input.GameplayActionInputSuppressed)return;
            if(ThrowMode){throwRequested=true;TryBeginThrow();return;}
            if(CurrentPhase==Phase.Carry && Time.time>=nextAttack){PocketOtherHandEquipment();impacted=false;SetPhase(Phase.Grip);nextAttack=Time.time+EquipmentRules.StickUseDelaySeconds;}
        }
        void TryBeginThrow()
        {
            // Accept a click before readiness/cooldown ends; execution still obeys both gates.
            if(!throwRequested || CurrentPhase!=Phase.Aim || !HasStick || Time.time<nextAttack || input.GameplayActionInputSuppressed)return;
            throwRequested=false;throwModeRequested=false;released=false;SetPhase(Phase.Release);nextAttack=Time.time+EquipmentRules.StickUseDelaySeconds;
        }
        void PocketOtherHandEquipment()
        {
            // Existing left-hand equipment stays owned; hide/store it before the two-hand strike.
            var hand=visual.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="LeftHand");
            if(!hand)return;
            foreach(var item in hand.Cast<Transform>().ToArray())
            {
                if(!item.gameObject.activeSelf || (!item.GetComponentInChildren<Renderer>() && !item.GetComponentInChildren<Light>()))continue;
                var pocket=transform.Find("Equipment Pocket Storage");
                if(!pocket){pocket=new GameObject("Equipment Pocket Storage").transform;pocket.SetParent(transform,false);pocket.localPosition=Vector3.up*.8f;}
                item.SetParent(pocket,false);item.gameObject.SetActive(false);
            }
        }
        public void ToggleThrow()
        {
            if(!HasStick || input.GameplayActionInputSuppressed || CurrentPhase==Phase.Release)return;
            throwModeRequested=!throwModeRequested;throwRequested=false;
            if(CurrentPhase==Phase.Carry && throwModeRequested)SetPhase(Phase.Ready);
            else if(!throwModeRequested && (CurrentPhase==Phase.Ready || CurrentPhase==Phase.Aim))SetPhase(Phase.Cancel);
        }
        public void Drop()
        { if(HasStick && CurrentPhase==Phase.Carry && !input.GameplayActionInputSuppressed){SpawnProjectile(false);SetPhase(Phase.Empty);} }
        void Update()
        {
            bool aim=pendingAimInput,use=pendingUseInput;pendingAimInput=pendingUseInput=false;
            if(aim)ToggleThrow();
            if(use)Use();
            elapsed+=Time.deltaTime;
            switch(CurrentPhase)
            {
                case Phase.Grip: if(elapsed>=GripSeconds)SetPhase(Phase.Strike);break;
                case Phase.Strike:
                    if(elapsed>=StrikeSeconds)SetPhase(Phase.Return);break;
                case Phase.Return: if(elapsed>=ReturnSeconds)SetPhase(throwModeRequested?Phase.Ready:Phase.Carry);break;
                case Phase.Ready: if(elapsed>=ready.length){SetPhase(Phase.Aim);TryBeginThrow();}break;
                case Phase.Aim: TryBeginThrow();break;
                case Phase.Release:
                    if(!released && elapsed>=releaseTime){released=true;SpawnProjectile(true);}
                    if(elapsed>=release.length)SetPhase(Phase.Empty);break;
                case Phase.Cancel: if(elapsed>=cancel.length)SetPhase(throwModeRequested?Phase.Ready:Phase.Carry);break;
            }
        }
        void LateUpdate()
        {
            if(CurrentPhase==Phase.Carry)
            {heldStick.localPosition=gripPosition;heldStick.localRotation=gripRotation;heldStick.localScale=gripScale;}
            heldStick.GetComponent<Renderer>().enabled=HasStick;
            if(CurrentPhase==Phase.Strike)SweepStrike();
            // Original flight curves stop driving the visible prop after physical release.
        }
        void SetPhase(Phase phase)
        {
            CurrentPhase=phase;elapsed=0;animator.speed=1;
            if(phase==Phase.Strike)
            {
                sweepReady=false;raisedForStrike=false;ContactWindow=false;ClearSurfaces();
                surfaces=FindObjectsByType<ParvumBrain>(FindObjectsSortMode.None).Where(e=>e.Health>0).Select(e=>new StickAnimatedSurface(e))
                    .Concat(FindObjectsByType<Bellerophon.Enemies.Fuga.FugaBrain>(FindObjectsSortMode.None).Where(e=>e.Health>0).Select(e=>new StickAnimatedSurface(e))).ToArray();
            }
            else ContactWindow=false;
            animator.SetLayerWeight(2,phase==Phase.Carry || phase==Phase.Empty ? 0:1);
            visual.gameObject.SetActive(phase!=Phase.Empty);
            if(phase==Phase.Carry){heldStick.localPosition=gripPosition;heldStick.localRotation=gripRotation;heldStick.localScale=gripScale;return;}
            if(phase==Phase.Empty)return;
            string state=phase.ToString();
            if(phase==Phase.Grip)animator.speed=grip.length/GripSeconds;
            if(phase==Phase.Strike)animator.speed=strike.length/StrikeSeconds;
            if(phase==Phase.Return)animator.speed=returnClip.length/ReturnSeconds;
            if(phase==Phase.Aim){animator.Play("Ready",2,.9999f);animator.Update(0);animator.speed=0;return;}
            animator.CrossFadeInFixedTime(state,.06f,2,0);
        }
        void StickSegment(out Vector3 start,out Vector3 end,out float radius)
        {
            var bounds=heldStick.GetComponent<MeshFilter>().sharedMesh.bounds;
            int axis=bounds.size.x>bounds.size.y?0:1;if(bounds.size.z>bounds.size[axis])axis=2;
            var a=bounds.center;var b=a;a[axis]-=bounds.extents[axis];b[axis]+=bounds.extents[axis];
            start=heldStick.TransformPoint(a);end=heldStick.TransformPoint(b);
            var hand=heldStick.parent.position;
            if((start-hand).sqrMagnitude>(end-hand).sqrMagnitude){var swap=start;start=end;end=swap;}
            radius=0;for(int i=0;i<3;i++)if(i!=axis)radius=Mathf.Max(radius,bounds.extents[i]*Mathf.Abs(heldStick.lossyScale[i]));
        }
        void SweepStrike()
        {
            StickSegment(out var start,out var end,out float radius);
            foreach(var surface in surfaces)surface.Capture();
            // Measure the animated tip relative to its camera-mounted rig, not camera motion.
            float height=visual.InverseTransformPoint(end).y;
            if(!sweepReady){previousBase=start;previousTip=end;previousTipHeight=lowestTipHeight=highestTipHeight=height;sweepReady=true;return;}
            lowestTipHeight=Mathf.Min(lowestTipHeight,height);highestTipHeight=Mathf.Max(highestTipHeight,height);
            if(height-lowestTipHeight>.15f)raisedForStrike=true;
            ContactWindow=raisedForStrike && height<previousTipHeight-.0001f;
            if(ContactWindow && !impacted)
            {
                var bounds=new Bounds(start,Vector3.zero);bounds.Encapsulate(end);bounds.Encapsulate(previousBase);bounds.Encapsulate(previousTip);bounds.Expand(radius*2);
                var candidates=surfaces.Where(s=>s.Intersects(bounds)).ToArray();
                float bodyMovement=candidates.Length>0?candidates.Max(s=>s.Movement):0;
                // Sweep both the shaft and moving/deforming body over the same time interval.
                int steps=Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(Vector3.Distance(start,previousBase),Vector3.Distance(end,previousTip))+bodyMovement)/Mathf.Max(.015f,radius)),1,96);
                for(int i=1;i<=steps && !impacted;i++)
                {
                    var a=Vector3.Lerp(previousBase,start,(float)i/steps);var b=Vector3.Lerp(previousTip,end,(float)i/steps);
                    Collider solid=null;StickAnimatedSurface enemy=null;Vector3 point=default;float nearest=float.PositiveInfinity;
                    foreach(var collider in Physics.OverlapCapsule(a,b,radius,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                    {
                        if(collider.transform.IsChildOf(transform) || collider.GetComponentInParent<ParvumBrain>() || collider.GetComponentInParent<Bellerophon.Enemies.Fuga.FugaBrain>())continue;
                        // Non-convex room meshes do not support Collider.ClosestPoint. The overlap
                        // already proves a solid shaft contact; keep that sample conservatively blocked.
                        var contact=collider is MeshCollider mesh && !mesh.convex?a:collider.ClosestPoint((a+b)*.5f);float distance=(contact-a).sqrMagnitude;
                        if(distance<nearest){nearest=distance;solid=collider;point=contact;}
                    }
                    foreach(var surface in candidates)
                    {
                        if(!surface.Touches(a,b,radius,(float)i/steps,out var contact))continue;
                        float distance=(contact-a).sqrMagnitude;
                        if(distance<nearest){nearest=distance;enemy=surface;solid=null;point=contact;}
                    }
                    if(!solid && enemy==null)continue;
                    // Keep world occlusion: accurate body contact is not permission to hit through walls.
                    bool blocked=Physics.RaycastAll(view.transform.position,point-view.transform.position,Vector3.Distance(view.transform.position,point),Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)
                        .Any(h=>!h.collider.transform.IsChildOf(transform) && h.collider!=solid && (enemy==null || !h.collider.transform.IsChildOf(enemy.Enemy.transform)));
                    ContactPoint=point;LastContactTime=Time.time;LastHit=blocked?"Blocked":enemy!=null?enemy.Enemy.name:solid.name;
                    // A ceiling/wall brush blocks this sample, not the rest of the animated swing.
                    // Only a delivered body hit consumes the attack; every later sample still
                    // checks solid overlap and line of sight before it can damage anything.
                    if(enemy!=null && !blocked){impacted=true;enemy.Damage(EquipmentRules.StickDamage,attacker);HitCount++;}
                }
            }
            previousBase=start;previousTip=end;previousTipHeight=height;
        }
        void SpawnProjectile(bool thrown)
        {
            // Aim within the specified band; the long prop contacts the deck before its centre.
            float range=Mathf.Lerp(EquipmentRules.StickThrowMinRange,EquipmentRules.StickThrowMaxRange,.65f);
            var target=view.transform.position+view.transform.forward*range;
            if(Physics.Raycast(view.transform.position,view.transform.forward,out var obstacle,range,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))target=obstacle.point;
            else if(Physics.Raycast(target,Vector3.down,out var landing,8,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))target=landing.point+Vector3.up*.12f;
            var prop=new GameObject("Recoverable Stick");prop.transform.SetPositionAndRotation(heldStick.position,heldStick.rotation);prop.transform.localScale=heldStick.lossyScale;
            prop.AddComponent<MeshFilter>().sharedMesh=heldStick.GetComponent<MeshFilter>().sharedMesh;
            prop.AddComponent<MeshRenderer>().sharedMaterials=heldStick.GetComponent<Renderer>().sharedMaterials;
            var mesh=prop.GetComponent<MeshFilter>().sharedMesh;var box=prop.AddComponent<BoxCollider>();box.center=mesh.bounds.center;box.size=mesh.bounds.size;
            var body=prop.AddComponent<Rigidbody>();body.mass=.6f;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.interpolation=RigidbodyInterpolation.Interpolate;
            foreach(var collider in GetComponentsInChildren<Collider>())Physics.IgnoreCollision(box,collider);
            Projectile=prop.AddComponent<PegasusThrownStick>();Projectile.Configure(this,attacker,thrown);
            // Solve a ballistic arc to the camera-centre point (4.5m on level aim).
            float duration=.75f;
            body.linearVelocity=thrown?(target-body.worldCenterOfMass-Physics.gravity*(.5f*duration*duration))/duration:view.transform.forward*.6f;
            body.angularVelocity=thrown?view.transform.right*5:Vector3.zero;
            HasStick=false;if(ship)ship.SetEquipmentState(ship.CurrentEquipmentState.WithHandSlot(reservedSlot,EquipmentSlotState.Empty));
        }
        public bool Recover(PegasusThrownStick prop)
        {
            if(!CanRecover(prop))return false;
            HasStick=true;Projectile=null;throwRequested=false;throwModeRequested=false;if(ship)ship.SetEquipmentState(ship.CurrentEquipmentState.WithHandSlot(reservedSlot,EquipmentSlotState.One(EquipmentItemKind.Stick)));
            SetPhase(Phase.Carry);return true;
        }
        public bool CanRecover(PegasusThrownStick prop) => !HasStick && prop==Projectile && (!ship || ship.CurrentEquipmentState.GetHandSlot(reservedSlot).IsEmpty);
    }
}
