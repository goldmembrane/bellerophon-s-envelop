using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    /// <summary>
    /// Keeps the approved neutral point selected in the 2D lower-body tree.
    /// The independent upper layer continues to play the imported knockback clip.
    /// </summary>
    public sealed class KnockbackReactionCycleBehaviour : StateMachineBehaviour
    {
        public const string MoveXParameter = "KnockbackMoveX";
        public const string MoveYParameter = "KnockbackMoveY";

        public override void OnStateEnter(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            animator.SetFloat(MoveXParameter, 0f);
            animator.SetFloat(MoveYParameter, 0f);
        }

        public override void OnStateUpdate(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            animator.SetFloat(MoveXParameter, 0f);
            animator.SetFloat(MoveYParameter, 0f);
        }
    }
}
