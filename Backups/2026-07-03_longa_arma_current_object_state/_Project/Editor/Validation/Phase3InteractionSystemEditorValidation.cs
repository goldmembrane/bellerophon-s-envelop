using System;
using Bellerophon.Core.Player;
using UnityEngine;

namespace Bellerophon.Editor.Validation
{
    public static class Phase3InteractionSystemEditorValidation
    {
        public static void Run()
        {
            var settings = ScriptableObject.CreateInstance<FirstPersonPlayerSettings>();
            var player = new GameObject("Phase 3 Validation Player");
            var origin = new GameObject("Phase 3 Validation Interaction Origin");
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);

            try
            {
                origin.transform.SetParent(player.transform, false);
                origin.transform.position = Vector3.zero;
                origin.transform.rotation = Quaternion.identity;

                target.name = "Phase 3 Validation Console";
                target.transform.position = new Vector3(0f, 0f, 2f);
                var interactable = target.AddComponent<DebugInteractable>();
                interactable.Configure("Phase 3 Console", "Inspect", true);

                var controller = player.AddComponent<FirstPersonInteractionController>();
                controller.Configure(settings, null, origin.transform);

                Physics.SyncTransforms();
                if (!controller.TryInteract())
                {
                    throw new InvalidOperationException(
                        "Phase 3 interaction controller failed to interact: " + controller.LastFailureReason);
                }

                if (!controller.HasCurrentTarget ||
                    controller.LastInteractable != interactable ||
                    interactable.InteractionCount != 1)
                {
                    throw new InvalidOperationException("Phase 3 interaction target detection or dispatch failed.");
                }

                if (controller.CurrentTargetDisplayName != "Phase 3 Console" ||
                    controller.CurrentTargetPrompt != "Inspect")
                {
                    throw new InvalidOperationException("Phase 3 interaction prompt metadata was not exposed.");
                }

                Debug.Log("Phase 3 interaction system editor validation passed.");
                Debug.Log("Phase 3 interaction details: Target=Phase 3 Console; Prompt=Inspect; Count=1");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(origin);
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }
    }
}
