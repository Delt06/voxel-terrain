using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Simulation.Liquids.Graphics
{
    public class VoxelWaterRenderFeature : ScriptableRendererFeature
    {
        [SerializeField] private LayerMask _layerMask = default;
        [SerializeField] private string _renderTargetName = "_RTName";
        [SerializeField] private string _depthRenderTargetName = "_DepthRTName";
        [SerializeField] private Material _blitMaterial = default;
        [SerializeField] private RenderPassEvent _event = RenderPassEvent.BeforeRenderingTransparents;

        private CreateTempDepthBufferPass _depthPass;
        private VoxelWaterRenderPass _waterPass;

        public override void Create()
        {
            var renderTargetId = Shader.PropertyToID(_renderTargetName);
            var depthRenderTargetId = Shader.PropertyToID(_depthRenderTargetName);
            _depthPass = new CreateTempDepthBufferPass(depthRenderTargetId)
            {
                renderPassEvent = _event,
            };
            _waterPass = new VoxelWaterRenderPass(renderTargetId, depthRenderTargetId, _layerMask, _blitMaterial
            )
            {
                renderPassEvent = _event,
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_depthPass);
            renderer.EnqueuePass(_waterPass);
        }
    }
}