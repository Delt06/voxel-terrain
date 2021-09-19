using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Simulation.Liquids.Graphics
{
    public class VoxelWaterRenderPass : ScriptableRenderPass
    {
        private readonly List<ShaderTagId> _shaderTagIds = new List<ShaderTagId>();
        private FilteringSettings _filteringSettings;
        private RenderStateBlock _renderStateBlock;
        private readonly int _renderTargetId;
        private readonly int _depthRenderTargetId;
        private RenderTargetIdentifier _renderTargetIdentifier;
        private RenderTargetIdentifier _depthRenderTargetIdentifier;
        private readonly Material _blitMaterial;

        public VoxelWaterRenderPass(int renderTargetId, int depthRenderTargetId, LayerMask layerMask,
            Material blitMaterial)
        {
            _renderTargetId = renderTargetId;
            _depthRenderTargetId = depthRenderTargetId;
            _blitMaterial = blitMaterial;
            _filteringSettings = new FilteringSettings(null, layerMask);
            _renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            _shaderTagIds.Add(new ShaderTagId("SRPDefaultUnlit"));
            _shaderTagIds.Add(new ShaderTagId("UniversalForward"));
            _shaderTagIds.Add(new ShaderTagId("UniversalForwardOnly"));
            _shaderTagIds.Add(new ShaderTagId("LightweightForward"));
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            blitTargetDescriptor.colorFormat = RenderTextureFormat.ARGB32;

            cmd.GetTemporaryRT(_renderTargetId, blitTargetDescriptor);
            _renderTargetIdentifier = new RenderTargetIdentifier(_renderTargetId);
            _depthRenderTargetIdentifier = new RenderTargetIdentifier(_depthRenderTargetId);
            if (renderingData.SupportsTransparentWater())
            {
                ConfigureTarget(_renderTargetIdentifier, _depthRenderTargetIdentifier);
                ConfigureClear(ClearFlag.Color, Color.clear);
            }
            else
            {
                var renderer = renderingData.cameraData.renderer;
                ConfigureTarget(renderer.cameraColorTarget, renderer.cameraDepthTarget);
                ConfigureClear(ClearFlag.None, Color.clear);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            const SortingCriteria sortingCriteria = SortingCriteria.CommonOpaque;
            var drawingSettings = CreateDrawingSettings(_shaderTagIds, ref renderingData, sortingCriteria);

            var cmd = CommandBufferPool.Get();

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings,
                ref _renderStateBlock
            );

            if (renderingData.SupportsTransparentWater())
            {
                cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTarget);
                cmd.Blit(_renderTargetIdentifier, renderingData.cameraData.renderer.cameraColorTarget, _blitMaterial);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_renderTargetId);
        }
    }
}