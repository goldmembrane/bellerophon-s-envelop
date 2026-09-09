using System;
using System.Collections;
using UnityEngine;

namespace Bellerophon.PlayerHands
{
    /// <summary>Observes normal Animator playback after skin rendering. Does not author a pose.</summary>
    public sealed class PlayerHandRigReviewObserver : MonoBehaviour
    {
        public static event Action<PlayerHandRigReviewObserver, float> PoseRendered;
        public string ReviewKind;
        public Animator Animator;
        private IEnumerator Start()
        {
            var endOfFrame = new WaitForEndOfFrame();
            while (enabled)
            {
                yield return endOfFrame;
                if (Animator != null && Animator.isActiveAndEnabled)
                {
                    float time = Mathf.Repeat(Animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f) * 6f;
                    PoseRendered?.Invoke(this, time);
                }
            }
        }
    }
}
