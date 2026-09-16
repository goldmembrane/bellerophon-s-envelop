using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Items
{
    /// <summary>
    /// Adds the approved armed-state red blink without changing the imported mine mesh,
    /// textures, or the material shared by the other electric-mine states.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ElectricMineArmedBlink : MonoBehaviour
    {
        public const float HalfCycleSeconds = 0.5f;

        private const string OverlayRootName = "ElectricMine_ArmedBlinkOverlay";
        private const string ShaderName = "Bellerophon/Items/ElectricMineArmedBlink";
        private const float ButtonRadiusRatio = 0.26f;
        private const float EdgeFeatherRatio = 0.025f;
        private static readonly Color ApprovedGlowColor =
            new Color(2.5f, 0.005f, 0.002f, 1f);

        private readonly List<Renderer> overlayRenderers = new List<Renderer>();
        private readonly List<Material> runtimeMaterials = new List<Material>();
        private float phaseStartedAt;

        public bool IsLit { get; private set; }

        private void OnEnable()
        {
            BuildOverlay();
            phaseStartedAt = Time.unscaledTime;
            SetLit(true);
        }

        private void Update()
        {
            bool shouldBeLit = Mathf.Repeat(
                Time.unscaledTime - phaseStartedAt,
                HalfCycleSeconds * 2f) < HalfCycleSeconds;
            if (shouldBeLit != IsLit)
                SetLit(shouldBeLit);
        }

        private void OnDestroy()
        {
            foreach (Material material in runtimeMaterials)
            {
                if (material != null)
                    Destroy(material);
            }

            runtimeMaterials.Clear();
            overlayRenderers.Clear();
        }

        private void BuildOverlay()
        {
            if (overlayRenderers.Count > 0)
                return;

            Transform oldOverlay = transform.Find(OverlayRootName);
            if (oldOverlay != null)
                Destroy(oldOverlay.gameObject);

            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
                throw new InvalidOperationException(
                    "Electric mine armed blink shader is missing: " + ShaderName);

            var sources = new List<(MeshFilter Filter, MeshRenderer Renderer)>();
            foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || filter.transform.name == OverlayRootName ||
                    (oldOverlay != null && filter.transform.IsChildOf(oldOverlay)))
                    continue;

                MeshRenderer sourceRenderer = filter.GetComponent<MeshRenderer>();
                if (sourceRenderer != null && sourceRenderer.enabled)
                    sources.Add((filter, sourceRenderer));
            }

            if (sources.Count == 0)
                throw new InvalidOperationException(
                    "ElectricMine_Armed_Idle has no static mine mesh to illuminate.");

            var overlayRoot = new GameObject(OverlayRootName);
            overlayRoot.transform.SetParent(transform, false);
            overlayRoot.layer = gameObject.layer;

            foreach ((MeshFilter sourceFilter, MeshRenderer sourceRenderer) in sources)
                CreateOverlay(overlayRoot.transform, sourceFilter, sourceRenderer, shader);
        }

        private void CreateOverlay(
            Transform overlayRoot,
            MeshFilter sourceFilter,
            MeshRenderer sourceRenderer,
            Shader shader)
        {
            var overlayObject = new GameObject(sourceFilter.name + "_ArmedBlink");
            Transform overlay = overlayObject.transform;
            overlay.SetParent(overlayRoot, false);
            Matrix4x4 relativeTransform =
                transform.worldToLocalMatrix * sourceFilter.transform.localToWorldMatrix;
            overlay.localPosition = relativeTransform.GetColumn(3);
            overlay.localRotation = relativeTransform.rotation;
            overlay.localScale = relativeTransform.lossyScale;
            overlayObject.layer = sourceRenderer.gameObject.layer;

            Mesh mesh = sourceFilter.sharedMesh;
            overlayObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer overlayRenderer = overlayObject.AddComponent<MeshRenderer>();
            overlayRenderer.shadowCastingMode = ShadowCastingMode.Off;
            overlayRenderer.receiveShadows = false;
            overlayRenderer.lightProbeUsage = LightProbeUsage.Off;
            overlayRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            overlayRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            Material material = CreateOverlayMaterial(shader, mesh.bounds);
            int slotCount = Mathf.Max(1, mesh.subMeshCount);
            var materials = new Material[slotCount];
            for (int index = 0; index < slotCount; index++)
                materials[index] = material;
            overlayRenderer.sharedMaterials = materials;

            runtimeMaterials.Add(material);
            overlayRenderers.Add(overlayRenderer);
        }

        private static Material CreateOverlayMaterial(Shader shader, Bounds bounds)
        {
            var material = new Material(shader)
            {
                name = "ElectricMine_ArmedBlink_Runtime",
                hideFlags = HideFlags.DontSave
            };

            Vector3 size = bounds.size;
            int discAxisIndex = 0;
            if (size.y < size.x && size.y <= size.z)
                discAxisIndex = 1;
            else if (size.z < size.x && size.z < size.y)
                discAxisIndex = 2;

            Vector3 discAxis = discAxisIndex == 0
                ? Vector3.right
                : discAxisIndex == 1
                    ? Vector3.up
                    : Vector3.forward;
            float radius = discAxisIndex == 0
                ? Mathf.Max(size.y, size.z) * 0.5f
                : discAxisIndex == 1
                    ? Mathf.Max(size.x, size.z) * 0.5f
                    : Mathf.Max(size.x, size.y) * 0.5f;

            material.SetColor("_GlowColor", ApprovedGlowColor);
            material.SetVector("_BoundsCenter", bounds.center);
            material.SetVector("_DiscAxis", discAxis);
            material.SetFloat("_DiscRadius", Mathf.Max(radius, 0.00001f));
            material.SetFloat("_ButtonRadiusRatio", ButtonRadiusRatio);
            material.SetFloat("_EdgeFeatherRatio", EdgeFeatherRatio);
            return material;
        }

        private void SetLit(bool lit)
        {
            IsLit = lit;
            foreach (Renderer renderer in overlayRenderers)
            {
                if (renderer != null)
                    renderer.enabled = lit;
            }
        }
    }

    internal static class ElectricMineArmedBlinkInstaller
    {
        private const string TargetName = "ElectricMine_Armed_Idle";
        private const string ModelName = "ElectricCurrentMine_Model";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            Transform target = FindUnique(scene, TargetName);
            if (target == null)
                return;

            Transform model = FindUnique(target, ModelName);
            if (model == null)
                throw new InvalidOperationException(
                    TargetName + " is missing " + ModelName + ".");

            if (model.GetComponent<ElectricMineArmedBlink>() == null)
                model.gameObject.AddComponent<ElectricMineArmedBlink>();
        }

        private static Transform FindUnique(Scene scene, string objectName)
        {
            Transform match = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                if (!string.Equals(item.name, objectName, StringComparison.Ordinal))
                    continue;
                if (match != null)
                    throw new InvalidOperationException(
                        "Multiple scene objects are named " + objectName + ".");
                match = item;
            }

            return match;
        }

        private static Transform FindUnique(Transform root, string objectName)
        {
            Transform match = null;
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                if (!string.Equals(item.name, objectName, StringComparison.Ordinal))
                    continue;
                if (match != null)
                    throw new InvalidOperationException(
                        root.name + " has multiple descendants named " + objectName + ".");
                match = item;
            }

            return match;
        }
    }
}
