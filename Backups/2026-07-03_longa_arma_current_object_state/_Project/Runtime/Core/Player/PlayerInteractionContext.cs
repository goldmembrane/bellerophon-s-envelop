using UnityEngine;

namespace Bellerophon.Core.Player
{
    public readonly struct PlayerInteractionContext
    {
        public PlayerInteractionContext(GameObject actor, Transform origin, RaycastHit hit)
        {
            Actor = actor;
            Origin = origin;
            Hit = hit;
        }

        public GameObject Actor { get; }

        public Transform Origin { get; }

        public RaycastHit Hit { get; }
    }
}
