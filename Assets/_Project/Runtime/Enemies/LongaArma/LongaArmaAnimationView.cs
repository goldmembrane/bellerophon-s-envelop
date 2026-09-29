using Bellerophon.Core.Session;
using UnityEngine;

namespace Bellerophon.Enemies.LongaArma
{
    public enum LongaArmaMotion { Idle, Move, Attack, Hit, Death, Consume }

    // Each slot is a copy of the corresponding existing CargoRunMvp presentation state.
    public sealed class LongaArmaAnimationView : MonoBehaviour
    {
        [SerializeField] GameObject[] slots;
        float started, hitUntil;
        LongaArmaMotion requested;
        Animator[] drivers;
        SkinnedMeshRenderer[] surfaces;
        Transform[] mouths, blades;
        float[] lengths;
        int attackSequence;
        float locomotionRate=1;
        public LongaArmaMotion Motion { get; private set; }
        public float MotionTime => Time.time-started;
        public GameObject ActiveSlot => slots[(int)Motion];
        public SkinnedMeshRenderer Surface { get { Cache(); return surfaces[(int)Motion]; } }
        public Transform BladeTip { get { Cache(); return blades[(int)Motion]; } }
        public Transform MouthTip { get { Cache(); return mouths[(int)Motion]; } }
        public Animator Driver { get { Cache(); return drivers[(int)Motion]; } }
        public float ConsumeDuration => ClipLength(LongaArmaMotion.Consume);
        // Existing clip first becomes liquid around 1.95 s, then re-forms before a second melt.
        public float DeathDuration => Mathf.Min(1.95f,ClipLength(LongaArmaMotion.Death));
        public int AttackCycle => attackSequence;
        public bool AttackInProgress => Motion==LongaArmaMotion.Attack && MotionTime<SeedIntruderRules.LongaArmaAttackDelaySeconds;
        public void SetLocomotionSpeed(float speed)
        {
            // Original gait at 3 m/s; doubled limbs cover twice the distance per cycle.
            locomotionRate=Mathf.Max(0,speed)/(3f*Mathf.Max(.001f,transform.lossyScale.z));
            if(Motion==LongaArmaMotion.Move && Driver)Driver.speed=locomotionRate;
        }

        public void Configure(GameObject[] states) { slots=states; drivers=null; }

        // Ignore the slot's own activation, but never select its hidden legacy rig.
        public static bool VisibleWithin(Transform child,Transform slot)
        {
            for(var item=child;item && item!=slot;item=item.parent)
                if(!item.gameObject.activeSelf) return false;
            return true;
        }

        void Cache()
        {
            if(drivers!=null) return;
            drivers=new Animator[slots.Length];surfaces=new SkinnedMeshRenderer[slots.Length];
            mouths=new Transform[slots.Length];blades=new Transform[slots.Length];lengths=new float[slots.Length];
            for(int i=0;i<slots.Length;i++)
            {
                foreach(var skin in slots[i].GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if(skin.enabled && VisibleWithin(skin.transform,slots[i].transform)) { surfaces[i]=skin;break; }
                var driver=surfaces[i] ? surfaces[i].GetComponentInParent<Animator>(true) : null;
                drivers[i]=driver;
                if(driver)
                {
                    // The source disables the parent legacy Animator. It must not animate this child rig.
                    foreach(var other in slots[i].GetComponentsInChildren<Animator>(true)) other.enabled=other==driver;
                    driver.applyRootMotion=false;driver.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                    var clips=driver.runtimeAnimatorController ? driver.runtimeAnimatorController.animationClips : null;
                    lengths[i]=clips!=null && clips.Length>0 ? clips[0].length : 1;
                }
                mouths[i]=FindBone(surfaces[i],"headend");
                blades[i]=FindBone(surfaces[i],"R_frontleg2");
            }
        }

        void Awake()
        {
            Cache();
            foreach (var slot in slots) slot.SetActive(false);
            Set(LongaArmaMotion.Idle,true);
            if(!GetComponent<LongaArmaGrounding>())gameObject.AddComponent<LongaArmaGrounding>();
        }

        public void Request(LongaArmaMotion motion)
        {
            requested=motion;
            if (motion==LongaArmaMotion.Death) { hitUntil=0; Set(motion); }
            else if (Time.time>=hitUntil && !(AttackInProgress && motion!=LongaArmaMotion.Attack))
                Set(motion,motion==LongaArmaMotion.Attack && Motion==motion && !AttackInProgress);
        }

        public void PlayHit()
        {
            if (Motion==LongaArmaMotion.Death) return;
            hitUntil=Time.time+ClipLength(LongaArmaMotion.Hit);
            Set(LongaArmaMotion.Hit,true);
        }

        void Update()
        {
            if(Motion==LongaArmaMotion.Death && MotionTime>=DeathDuration && Driver && Driver.speed!=0)
            {
                Driver.Play(0,0,DeathDuration/ClipLength(LongaArmaMotion.Death));Driver.Update(0);Driver.speed=0;
            }
            if (Motion==LongaArmaMotion.Hit && Time.time>=hitUntil) Set(requested);
            if(Motion==LongaArmaMotion.Move)
            {
                var body=GetComponent<Rigidbody>();
                if(body)SetLocomotionSpeed(Vector3.ProjectOnPlane(body.linearVelocity,Vector3.up).magnitude);
            }
        }

        float ClipLength(LongaArmaMotion motion)
        {
            Cache();return lengths[(int)motion];
        }

        void Set(LongaArmaMotion motion,bool restart=false)
        {
            if (!restart && Motion==motion && slots[(int)motion].activeSelf) return;
            foreach (var slot in slots) slot.SetActive(false);
            Motion=motion;started=Time.time;
            if(motion==LongaArmaMotion.Attack)attackSequence++;
            ActiveSlot.SetActive(true);
            var animator=Driver;
            if (!animator || !animator.runtimeAnimatorController) return;
            animator.applyRootMotion=false;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            animator.enabled=true;
            animator.Play(0,0,0);animator.Update(0);
            // The authored slam is 2.2 s; preserve the motion while meeting the 2.5 s attack cadence.
            animator.speed=motion==LongaArmaMotion.Attack
                ? ClipLength(motion)/SeedIntruderRules.LongaArmaAttackDelaySeconds : motion==LongaArmaMotion.Move ? locomotionRate : 1f;
        }

        static Transform FindBone(SkinnedMeshRenderer renderer,string name)
        {
            if (!renderer) return null;
            foreach (var bone in renderer.bones)
                if (bone && bone.name==name) return bone;
            return null;
        }
    }
}
