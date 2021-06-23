using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Simulation.Liquids.Graphics
{
    public class CreateTempDepthBufferPass : ScriptableRenderPass
    {
        private readonly int _depthRenderTargetId;
        private RenderTargetIdentifier _depthRenderTargetIdentifier;
        private RenderTargetIdentifier _cameraDepthAttachmentIdentifier;

        public CreateTempDepthBufferPass(int depthRenderTargetId) => _depthRenderTargetId = depthRenderTargetId;

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var depthTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            depthTextureDescriptor.colorFormat = RenderTextureFormat.Depth;
            cmd.GetTemporaryRT(_depthRenderTargetId, depthTextureDescriptor, FilterMode.Point);

            _cameraDepthAttachmentIdentifier = renderingData.cameraData.renderer.cameraDepthTarget;
            _depthRenderTargetIdentifier = new RenderTargetIdentifier(_depthRenderTargetId);
            ConfigureTarget(_depthRenderTargetIdentifier);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!renderingData.SupportsTransparentWater()) return;

            var cmd = CommandBufferPool.Get();
            cmd.CopyTexture(_cameraDepthAttachmentIdentifier, _depthRenderTargetIdentifier);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_depthRenderTargetId);
        }
    }
}