using UnityEngine;

namespace Bellerophon.ArtSamples
{
    // Sample-local font atlas binding; source fonts and their shared materials remain unchanged.
    [ExecuteAlways]
    public sealed class CargoSampleDepthText : MonoBehaviour
    {
        private MaterialPropertyBlock block;
        private void OnEnable() { Font.textureRebuilt += Rebuilt; Apply(); }
        private void OnDisable() { Font.textureRebuilt -= Rebuilt; }
        private void Rebuilt(Font font) { Apply(); }
        private void LateUpdate() { Apply(); }
        private void Apply()
        {
            if(block==null)block=new MaterialPropertyBlock();
            foreach(var text in GetComponentsInChildren<TextMesh>(true))
            {
                if(text.font==null||text.font.material==null)continue;
                var renderer=text.GetComponent<Renderer>();if(renderer==null)continue;
                renderer.GetPropertyBlock(block);block.SetTexture("_MainTex",text.font.material.mainTexture);renderer.SetPropertyBlock(block);
            }
        }
    }
}
