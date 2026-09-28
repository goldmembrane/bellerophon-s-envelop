using Bellerophon.Core.Session;
using Bellerophon.Enemies.Parvum;
using UnityEngine;

namespace Bellerophon.Core.Player
{
    public sealed class PegasusThrownStick : MonoBehaviour,IPlayerInteractable
    {
        PegasusStickController owner;
        ParvumTarget attacker;
        bool damagePending;
        float settledTime;
        public bool DamagePending => damagePending;
        public int HitCount { get; private set; }
        public string HitTarget { get; private set; }
        public float HitTime { get; private set; }
        Vector3 launchOrigin;
        public bool HasContact { get; private set; }
        public string FirstContact { get; private set; }
        public float FlightDistance { get; private set; }
        public string DisplayName => "막대기";
        public string InteractionPrompt => "F를 눌러서 회수";
        public void Configure(PegasusStickController controller,ParvumTarget source,bool thrown){owner=controller;attacker=source;damagePending=thrown;launchOrigin=controller.transform.position;}
        void OnCollisionEnter(Collision collision)
        {
            if(!HasContact){HasContact=true;FirstContact=collision.collider.name;FlightDistance=Vector3.ProjectOnPlane(collision.GetContact(0).point-launchOrigin,Vector3.up).magnitude;}
            CommitHit(collision);
        }
        void OnCollisionStay(Collision collision){CommitHit(collision);}
        void CommitHit(Collision collision)
        {
            if(!damagePending)return;
            var enemy=collision.collider.GetComponentInParent<ParvumBrain>();
            var fuga=collision.collider.GetComponentInParent<Bellerophon.Enemies.Fuga.FugaBrain>();
            if(fuga && fuga.Health>0){damagePending=false;HitCount++;HitTarget=fuga.name;HitTime=Time.time;fuga.ReceiveDamage(EquipmentRules.StickDamage,attacker);return;}
            if(!enemy || enemy.Health<=0)return;
            damagePending=false;HitCount++;HitTarget=enemy.name;HitTime=Time.time;
            enemy.ReceiveDamage(EquipmentRules.StickDamage,attacker);
        }
        void FixedUpdate()
        {
            if(!damagePending || !HasContact)return;
            var body=GetComponent<Rigidbody>();
            // A floor/wall contact must not consume an in-flight hit. A settled item is harmless.
            bool resting=body.IsSleeping() || (body.linearVelocity.sqrMagnitude<.0225f && body.angularVelocity.sqrMagnitude<.09f);
            settledTime=resting?settledTime+Time.fixedDeltaTime:0;
            if(settledTime>=.3f)damagePending=false;
        }
        public bool CanInteract(PlayerInteractionContext context,out string failureReason)
        { bool allowed=owner && context.Actor==owner.gameObject && owner.CanRecover(this);failureReason=allowed?string.Empty:"회수할 수 없습니다.";return allowed; }
        public void Interact(PlayerInteractionContext context)
        { if(CanInteract(context,out _) && owner.Recover(this))Destroy(gameObject); }
    }
}
