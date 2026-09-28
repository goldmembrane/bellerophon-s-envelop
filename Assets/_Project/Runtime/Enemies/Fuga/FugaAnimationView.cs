using System;
using System.Linq;
using UnityEngine;

namespace Bellerophon.Enemies.Fuga
{
    public enum FugaMotion { Idle, Move, Attack, Hit, Death, Consume }
    public sealed class FugaAnimationView : MonoBehaviour
    {
        [SerializeField] GameObject[] slots;
        float started,hitUntil;
        FugaMotion requested;
        public FugaMotion Motion {get;private set;}
        public float MotionTime=>Time.time-started;
        public SkinnedMeshRenderer Surface=>slots[(int)Motion].GetComponentInChildren<SkinnedMeshRenderer>();
        public GameObject ActiveSlot=>slots[(int)Motion];
        public void Configure(GameObject[] sources){slots=sources;}
        void Awake()
        {
            foreach(var slot in slots)slot.SetActive(false);
            foreach(var consume in GetComponentsInChildren<FugaConsumeMotionDriver>(true))consume.ConfigureGameplay(GetComponent<Rigidbody>());
            Set(FugaMotion.Idle,true);
        }
        public void PlayHit(){if(Motion==FugaMotion.Death)return;hitUntil=Time.time+1.1f;Set(FugaMotion.Hit,true);}
        public void Request(FugaMotion motion)
        {requested=motion;if(motion==FugaMotion.Death){hitUntil=0;Set(motion);}else if(Time.time>=hitUntil)Set(motion);}
        void Update(){if(Motion==FugaMotion.Hit && Time.time>=hitUntil)Set(requested);}
        void Set(FugaMotion motion,bool restart=false)
        {
            if(!restart && Motion==motion && slots[(int)motion].activeSelf)return;
            foreach(var slot in slots)slot.SetActive(false);
            Motion=motion;started=Time.time;ActiveSlot.SetActive(true);
            var animator=ActiveSlot.GetComponent<Animator>();
            if(animator && animator.runtimeAnimatorController){animator.enabled=true;animator.Rebind();animator.Update(0);}
            if(motion==FugaMotion.Consume){var consume=ActiveSlot.GetComponent<FugaConsumeMotionDriver>();if(consume)consume.enabled=true;}
            if(motion==FugaMotion.Attack)ActiveSlot.GetComponent<FugaAttackAlternationDriver>()?.StartAttackSequence();
            if(motion==FugaMotion.Hit)ActiveSlot.GetComponent<FugaHitReactionRandomDriver>()?.PlayHitReaction();
        }
    }
}
