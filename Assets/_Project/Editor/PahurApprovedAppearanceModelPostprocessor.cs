using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Bellerophon.Editor.PahurCargoRunScene
{
    internal sealed class PahurApprovedAppearanceModelPostprocessor :
        AssetPostprocessor
    {
        private const string ModelPath =
            "Assets/_Project/Art/Enemies/Pahur/Models/Pahur.fbx";

        private void OnPostprocessModel(GameObject importedRoot)
        {
            if (!string.Equals(
                    assetPath,
                    ModelPath,
                    StringComparison.Ordinal))
            {
                return;
            }

            foreach (var renderer in
                     importedRoot.GetComponentsInChildren<SkinnedMeshRenderer>(
                         true))
            {
                var mesh = renderer.sharedMesh;
                if (mesh == null)
                {
                    throw new InvalidOperationException(
                        "Imported approved Pahur renderer has no mesh.");
                }

                var approvedSamplePositions =
                    new List<Vector3>(mesh.vertexCount);
                foreach (var position in mesh.vertices)
                {
                    approvedSamplePositions.Add(
                        new Vector3(
                            -position.x * 100f,
                            position.y * 100f,
                            position.z * 100f));
                }

                mesh.SetUVs(3, approvedSamplePositions);
            }
        }
    }
}
