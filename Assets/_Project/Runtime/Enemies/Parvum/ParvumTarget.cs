using System;
using System.Collections.Generic;
using Bellerophon.Core.Player;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using UnityEngine;

namespace Bellerophon.Enemies.Parvum
{
    public enum ParvumTargetKind { Facility, MetalCargo, Creature, Speaker }

    // Attach to the actual collider/part. Never turn a corridor root into a food target.
    public sealed class ParvumTarget : MonoBehaviour
    {
        public static readonly HashSet<ParvumTarget> Active = new HashSet<ParvumTarget>();
        [SerializeField] private ParvumTargetKind kind;
        [SerializeField] private Collider surface;
        [SerializeField] private ShipDeviceInteractionState ship;
        [SerializeField] private ShipRoomId room;
        // Explicit room-wall qualification; a generic facility collider is not food.
        [SerializeField] private bool roomWall;
        [SerializeField] private FirstPersonPlayerStatus player;
        [SerializeField] private TransportSettlementController transport;
        [SerializeField] private CargoMaterial cargoMaterial;
        [SerializeField] private IntruderFaction faction;
        [SerializeField] private bool biological;
        [SerializeField] private float health=100;
        [SerializeField] private float shield;
        [SerializeField] private bool speakerAudible;
        [SerializeField] private AudioSource speakerAudio;
        [SerializeField] private Vector3[] surfaceVertices;
        [SerializeField] private int[] surfaceTriangles;
        private Vector3[] worldVertices;
        private Bounds[] worldTriangleBounds;
        private bool[] consumableTriangles;
        private readonly ParvumFacilityLedger ledger=new ParvumFacilityLedger();
        private ParvumCargoLedger cargoLedger=new ParvumCargoLedger();
        private readonly ParvumSlowWindow slow=new ParvumSlowWindow();
        private double playerDamageRemainder;
        // Separate feeding ledger: Fuga consumes initial cargo durability over 50 active seconds.
        private double fugaFacilityRemainder;
        private float fugaCargoInitial=-1;
        // Longa Arma has its own 23 s facility / 48 s cargo feeding cadence.
        private double longaFacilityRemainder;
        private float longaCargoInitial=-1;
        private string resolvedContractId;
        private int resolvedTransportNumber=-1;
        private CargoMaterial resolvedCargoMaterial;
        public float Health => player ? player.CurrentHealth : health;
        public float Shield => player ? player.CurrentShield : shield;
        public float SlowRemaining => slow.Remaining;
        public bool IsHostileToParvum => kind==ParvumTargetKind.Creature && !player && faction!=IntruderFaction.None &&
            SeedIntruderRules.DetermineSeedRelation(SeedIntruderKind.Parvum,faction).CanDirectlyAttack;
        public event Action<ParvumTarget> DamagePartCreated;
        public event Action<ParvumBrain,float> Bitten;
        public ParvumTargetKind Kind => kind;
        public ShipRoomId Room => room;
        public ShipDeviceInteractionState Ship => ship;
        public bool IsRoomWall => kind==ParvumTargetKind.Facility && roomWall;
        public IntruderFaction Faction => faction;
        public Collider Surface => surface;
        public bool Audible => kind==ParvumTargetKind.Speaker && (speakerAudio ? speakerAudio.isPlaying && !speakerAudio.mute && speakerAudio.volume>0 : speakerAudible) && IsAlive;
        public float MovementMultiplier => slow.Remaining>0 ? .7f : 1f;
        public bool IsAlive => kind==ParvumTargetKind.MetalCargo ? ship && IsMetal && ship.CurrentCargoState.DurabilityPercent>0 :
            kind==ParvumTargetKind.Facility ? ship && ship.CurrentShipState.GetRoom(room).CurrentDurability>0 : player ? !player.IsDead : health>0;
        public bool IsMetal => (kind==ParvumTargetKind.Facility && roomWall) || (kind==ParvumTargetKind.MetalCargo && SeedIntruderRules.IsMetalCargo(ResolveCargoMaterial()));
        private CargoMaterial ResolveCargoMaterial()
        {
            var session=transport ? transport.CurrentSession : null;
            if(session==null || !session.ActiveTransportContract.HasValue)return cargoMaterial;
            string id=session.ActiveTransportContract.Value.Id;
            if(resolvedContractId!=id || resolvedTransportNumber!=session.CompletedTransportCount)
            {
                resolvedContractId=id;resolvedTransportNumber=session.CompletedTransportCount;
                resolvedCargoMaterial=SeedIntruderRules.ResolveCargoMaterial(session);
                cargoLedger=new ParvumCargoLedger();fugaCargoInitial=-1;longaCargoInitial=-1; // A new cargo run has its own consumption ledger.
            }
            return resolvedCargoMaterial;
        }
        public bool HasDamagePart => ledger.HasDamagePart;
        public double AccumulatedConsumptionSeconds => ledger.AccumulatedAttackSeconds;
        public bool IntersectsMouthSweep(Vector3 from,Vector3 to)
        {
            var delta=to-from;
            if(delta.sqrMagnitude<.000001f)return false;
            if(surface is MeshCollider mesh && !mesh.convex && worldVertices!=null && surfaceTriangles!=null)
            {
                var sweepBounds=new Bounds(from,Vector3.zero);sweepBounds.Encapsulate(to);sweepBounds.Expand(.0001f);
                // Room liners can expose their back face to the room. Contact must not depend on
                // Physics.queriesHitBackfaces; intersect the unchanged wall triangles from either side.
                for(int i=0;i<surfaceTriangles.Length;i+=3)
                {
                    if(!consumableTriangles[i/3] || !worldTriangleBounds[i/3].Intersects(sweepBounds))continue;
                    var a=worldVertices[surfaceTriangles[i]];var e1=worldVertices[surfaceTriangles[i+1]]-a;var e2=worldVertices[surfaceTriangles[i+2]]-a;
                    var p=Vector3.Cross(delta,e2);float determinant=Vector3.Dot(e1,p);
                    if(Mathf.Abs(determinant)<1e-9f)continue;
                    float inverse=1/determinant;var offset=from-a;float u=Vector3.Dot(offset,p)*inverse;
                    if(u<0 || u>1)continue;
                    var q=Vector3.Cross(offset,e1);float v=Vector3.Dot(delta,q)*inverse;
                    if(v<0 || u+v>1)continue;
                    float t=Vector3.Dot(e2,q)*inverse;if(t>=0 && t<=1)return true;
                }
                return false;
            }
            return surface && surface.Raycast(new Ray(from,delta.normalized),out _,delta.magnitude);
        }
        public Vector3 ClosestPoint(Vector3 origin)
        {
            if(!surface)return transform.position;
            if(surface is BoxCollider box)
            {
                var local=box.transform.InverseTransformPoint(origin)-box.center;
                var half=box.size*.5f;
                return box.transform.TransformPoint(box.center+new Vector3(Mathf.Clamp(local.x,-half.x,half.x),Mathf.Clamp(local.y,-half.y,half.y),Mathf.Clamp(local.z,-half.z,half.z)));
            }
            if(surface is CharacterController character)
            {
                var center=character.transform.TransformPoint(character.center);
                float radius=character.radius;float half=Mathf.Max(0,character.height*.5f-radius);
                var axis=center+Vector3.up*Mathf.Clamp(origin.y-center.y,-half,half);
                var delta=origin-axis;return delta.sqrMagnitude<=radius*radius?origin:axis+delta.normalized*radius;
            }
            if(!(surface is MeshCollider mesh) || mesh.convex)return surface.ClosestPoint(origin);
            if(worldVertices==null || surfaceTriangles==null || surfaceTriangles.Length==0)return surface.bounds.ClosestPoint(origin);
            Vector3 nearest=worldVertices[0];float distance=float.PositiveInfinity;
            for(int i=0;i<surfaceTriangles.Length;i+=3)
            {
                if(!consumableTriangles[i/3] || worldTriangleBounds[i/3].SqrDistance(origin)>=distance)continue;
                var p=OnTriangle(origin,worldVertices[surfaceTriangles[i]],worldVertices[surfaceTriangles[i+1]],worldVertices[surfaceTriangles[i+2]]);
                float d=(p-origin).sqrMagnitude;if(d<distance){distance=d;nearest=p;}
            }
            return nearest;
        }
        public void ConfigureFacility(Collider collider,ShipDeviceInteractionState state,ShipRoomId id)
        { kind=ParvumTargetKind.Facility;roomWall=true;surface=collider;ship=state;room=id;CacheSurface(); }
        public void CacheSurface()
        {
            if(surface is MeshCollider mesh && !mesh.convex)
            {surfaceVertices=mesh.sharedMesh.vertices;surfaceTriangles=mesh.sharedMesh.triangles;}
            RefreshWorldVertices();
        }
        private void RefreshWorldVertices()
        {
            if(surfaceVertices==null || surfaceTriangles==null)return;
            worldVertices=new Vector3[surfaceVertices.Length];
            for(int i=0;i<worldVertices.Length;i++)worldVertices[i]=(surface?surface.transform:transform).TransformPoint(surfaceVertices[i]);
            worldTriangleBounds=new Bounds[surfaceTriangles.Length/3];consumableTriangles=new bool[worldTriangleBounds.Length];
            for(int i=0;i<surfaceTriangles.Length;i+=3)
            {
                var a=worldVertices[surfaceTriangles[i]];var b=worldVertices[surfaceTriangles[i+1]];var c=worldVertices[surfaceTriangles[i+2]];
                var bounds=new Bounds(a,Vector3.zero);bounds.Encapsulate(b);bounds.Encapsulate(c);worldTriangleBounds[i/3]=bounds;
                consumableTriangles[i/3]=!(kind==ParvumTargetKind.Facility && roomWall && Mathf.Abs(Vector3.Cross(b-a,c-a).normalized.y)>.6f);
            }
        }
        public void ConfigurePlayer(FirstPersonPlayerStatus status)
        { kind=ParvumTargetKind.Creature;player=status;biological=true;surface=status.GetComponent<Collider>(); }
        public void ConfigureCargo(Collider collider,ShipDeviceInteractionState state,TransportSettlementController controller,CargoMaterial material=CargoMaterial.Unspecified)
        {kind=ParvumTargetKind.MetalCargo;surface=collider;ship=state;transport=controller;cargoMaterial=material;resolvedContractId=null;resolvedTransportNumber=-1;cargoLedger=new ParvumCargoLedger();CacheSurface();}
        public void SetSpeakerAudible(bool audible) => speakerAudible=audible;
        public void ConfigureSpeaker(AudioSource audio,Collider collider)
        {kind=ParvumTargetKind.Speaker;speakerAudio=audio;surface=collider;biological=false;}
        public void ConfigureCombatTarget(bool isBiological,float hitPoints,float shieldPoints,IntruderFaction targetFaction=IntruderFaction.None)
        {kind=ParvumTargetKind.Creature;biological=isBiological;health=hitPoints;shield=shieldPoints;faction=targetFaction;surface=GetComponent<Collider>();}
        private void OnEnable() { RefreshWorldVertices();Active.Add(this); }
        private void OnDisable() { Active.Remove(this); }
        private void Update()
        {
            slow.Tick(Time.deltaTime);
            if(kind==ParvumTargetKind.Facility) ApplyFacilityDamage(ledger.TickDamagePart(Time.deltaTime));
        }
        public void AccumulateConsumption(float seconds)
        {
            if(kind!=ParvumTargetKind.Facility || !roomWall || !IsAlive) return;
            bool existed=ledger.HasDamagePart;
            ledger.AccumulateAttack(seconds);
            if(!existed && ledger.HasDamagePart) DamagePartCreated?.Invoke(this);
        }
        public void ReceiveBite(ParvumBrain attacker)
        {
            if(!IsAlive || (kind==ParvumTargetKind.Facility && !roomWall)) return;
            if(kind==ParvumTargetKind.Facility) { int facilityDamage=ledger.ConsumeBite();ApplyFacilityDamage(facilityDamage);Bitten?.Invoke(attacker,facilityDamage);return; }
            if(kind==ParvumTargetKind.MetalCargo)
            {
                var cargo=ship.CurrentCargoState;
                float remaining=cargoLedger.ConsumeBite(cargo.DurabilityPercent);
                ship.SetCargoState(cargo.WithDurabilityPercent(remaining));
                Bitten?.Invoke(attacker,cargo.DurabilityPercent-remaining);
                return;
            }
            if(kind==ParvumTargetKind.Speaker)
            {health=0;speakerAudible=false;if(speakerAudio)speakerAudio.Stop();Bitten?.Invoke(attacker,3);gameObject.SetActive(false);return;}
            bool shielded=player ? player.CurrentShield>0 : shield>0;
            float damage=ParvumGameplayRules.BiteDamage(shielded,biological)*(attacker?attacker.AttackMultiplier:1f);
            float duration=ParvumGameplayRules.SlowDuration(shielded,biological);
            if(player)
            {
                // Player vitals are integers; retain fractional buff damage rather than rounding each bite up.
                playerDamageRemainder+=damage;
                int wholeDamage=(int)Math.Floor(playerDamageRemainder+1e-7);
                playerDamageRemainder-=wholeDamage;
                player.ApplyDamage(wholeDamage);
                player.ApplyParvumBiteSlow(duration);
            }
            else
            {
                if(shielded) shield=Mathf.Max(0,shield-damage); else health=Mathf.Max(0,health-damage);
                slow.TryApply(duration);
            }
            Bitten?.Invoke(attacker,damage);
        }
        public void ReceiveFugaConsumption(float seconds)
        {
            if(!IsAlive || !IsMetal || seconds<=0)return;
            if(IsRoomWall)
            {
                AccumulateConsumption(seconds);fugaFacilityRemainder+=20d*seconds;
                int damage=(int)System.Math.Floor(fugaFacilityRemainder+1e-7);fugaFacilityRemainder-=damage;ApplyFacilityDamage(damage);
            }
            else if(kind==ParvumTargetKind.MetalCargo)
            {
                var cargo=ship.CurrentCargoState;if(fugaCargoInitial<0)fugaCargoInitial=cargo.DurabilityPercent;
                ship.SetCargoState(cargo.WithDurabilityPercent(Mathf.Max(0,cargo.DurabilityPercent-fugaCargoInitial*seconds/50f)));
            }
        }
        public void ReceiveLongaArmaConsumption(float seconds)
        {
            if(!IsAlive || !IsMetal || seconds<=0)return;
            if(IsRoomWall)
            {
                AccumulateConsumption(seconds);longaFacilityRemainder+=(500d/23d)*seconds;
                int damage=(int)System.Math.Floor(longaFacilityRemainder+1e-7);
                longaFacilityRemainder-=damage;ApplyFacilityDamage(damage);
            }
            else if(kind==ParvumTargetKind.MetalCargo)
            {
                var cargo=ship.CurrentCargoState;if(longaCargoInitial<0)longaCargoInitial=cargo.DurabilityPercent;
                ship.SetCargoState(cargo.WithDurabilityPercent(Mathf.Max(0,cargo.DurabilityPercent-longaCargoInitial*seconds/48f)));
            }
        }
        public void ReceiveLongaArmaStrike(float damage) => ReceiveFugaStrike(damage);
        public void ReceiveFugaStrike(float damage)
        {
            if(!IsAlive)return;
            if(kind==ParvumTargetKind.Speaker){health=0;speakerAudible=false;if(speakerAudio)speakerAudio.Stop();gameObject.SetActive(false);return;}
            if(kind!=ParvumTargetKind.Creature)return;
            if(player){playerDamageRemainder+=damage;int whole=(int)System.Math.Floor(playerDamageRemainder+1e-7);playerDamageRemainder-=whole;player.ApplyDamage(whole);}
            else if(shield>0)shield=Mathf.Max(0,shield-damage);else health=Mathf.Max(0,health-damage);
        }
        private void ApplyFacilityDamage(int damage)
        {
            if(damage<=0 || !ship) return;
            var state=ship.CurrentShipState;
            ship.SetShipState(state.WithRoom(room,state.GetRoom(room).WithDamage(damage)));
        }
        private static Vector3 OnTriangle(Vector3 p,Vector3 a,Vector3 b,Vector3 c)
        {
            var ab=b-a;var ac=c-a;var ap=p-a;float d1=Vector3.Dot(ab,ap),d2=Vector3.Dot(ac,ap);
            if(d1<=0 && d2<=0)return a;
            var bp=p-b;float d3=Vector3.Dot(ab,bp),d4=Vector3.Dot(ac,bp);
            if(d3>=0 && d4<=d3)return b;
            float vc=d1*d4-d3*d2;if(vc<=0 && d1>=0 && d3<=0)return a+ab*(d1/(d1-d3));
            var cp=p-c;float d5=Vector3.Dot(ab,cp),d6=Vector3.Dot(ac,cp);
            if(d6>=0 && d5<=d6)return c;
            float vb=d5*d2-d1*d6;if(vb<=0 && d2>=0 && d6<=0)return a+ac*(d2/(d2-d6));
            float va=d3*d6-d5*d4;if(va<=0 && d4-d3>=0 && d5-d6>=0)return b+(c-b)*((d4-d3)/((d4-d3)+(d5-d6)));
            float sum=va+vb+vc;if(Mathf.Abs(sum)<1e-12f)return a;
            return a+ab*(vb/sum)+ac*(vc/sum);
        }
    }
}
