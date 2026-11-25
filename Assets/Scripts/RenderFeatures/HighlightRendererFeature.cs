
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class HighlightRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Header("Highlight")]
        public LayerMask highlightLayer;
        public Material highlightMaterial;

        [Header("Foreground")]
        public LayerMask foregroundLayer;

        [Header("Rendering")]
        public bool renderInSceneView = true;
    }

    [SerializeField]
    private Settings settings = new Settings();

    private HighlightPass highlightPass;
    private ForegroundPass foregroundPass;

    public override void Create()
    {
        ShaderTags = new List<ShaderTagId>
        {
            new("UniversalForward"),
            new("UniversalForwardOnly"),
            new("SRPDefaultUnlit")
        };

        highlightPass = new HighlightPass(settings);
        foregroundPass = new ForegroundPass(settings);

        // Both execute after the normal scene rendering.
        highlightPass.renderPassEvent =
            RenderPassEvent.AfterRenderingTransparents;

        // Ensure foreground executes after highlight.
        foregroundPass.renderPassEvent =
            (RenderPassEvent)(
                (int)RenderPassEvent.AfterRenderingTransparents + 1
            );
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        CameraData cameraData = renderingData.cameraData;

        if (cameraData.cameraType == CameraType.Preview ||
            cameraData.cameraType == CameraType.Reflection)
            return;

        if (!settings.renderInSceneView &&
            cameraData.cameraType == CameraType.SceneView)
            return;

        if (settings.highlightMaterial == null)
            return;

        renderer.EnqueuePass(highlightPass);
        renderer.EnqueuePass(foregroundPass);
    }

    // ------------------------------------------------------------
    // Shared functionality
    // ------------------------------------------------------------

    private static List<ShaderTagId> ShaderTags;

    private static RendererListHandle CreateRendererList(
        RenderGraph renderGraph,
        UniversalRenderingData renderingData,
        UniversalCameraData cameraData,
        UniversalLightData lightData,
        LayerMask layerMask,
        RenderQueueRange queueRange,
        SortingCriteria sortingCriteria,
        Material overrideMaterial = null)
    {
        FilteringSettings filteringSettings =
            new(queueRange, layerMask);

        DrawingSettings drawingSettings =
            RenderingUtils.CreateDrawingSettings(
                ShaderTags,
                renderingData,
                cameraData,
                lightData,
                sortingCriteria
            );

        if (overrideMaterial != null)
            drawingSettings.overrideMaterial = overrideMaterial;

        RendererListParams parameters = new RendererListParams(
            renderingData.cullResults,
            drawingSettings,
            filteringSettings
        );

        return renderGraph.CreateRendererList(parameters);
    }

    // ------------------------------------------------------------
    // PASS 2: Highlight
    // ------------------------------------------------------------

    private class HighlightPass : ScriptableRenderPass
    {
        private readonly Settings settings;

        private class PassData
        {
            public RendererListHandle opaque;
            public RendererListHandle transparent;
        }

        public HighlightPass(Settings settings)
        {
            this.settings = settings;
        }

        public override void RecordRenderGraph(
            RenderGraph renderGraph,
            ContextContainer frameData)
        {

            UniversalRenderingData renderingData =
                frameData.Get<UniversalRenderingData>();

            UniversalCameraData cameraData =
                frameData.Get<UniversalCameraData>();

            UniversalLightData lightData =
                frameData.Get<UniversalLightData>();

            UniversalResourceData resourceData =
                frameData.Get<UniversalResourceData>();

            using var builder =
                renderGraph.AddRasterRenderPass<PassData>(
                    "Highlight Pass",
                    out var passData);
            passData.opaque = CreateRendererList(
                renderGraph,
                renderingData,
                cameraData,
                lightData,
                settings.highlightLayer,
                RenderQueueRange.opaque,
                cameraData.defaultOpaqueSortFlags,
                settings.highlightMaterial
            );

            passData.transparent = CreateRendererList(
                renderGraph,
                renderingData,
                cameraData,
                lightData,
                settings.highlightLayer,
                RenderQueueRange.transparent,
                SortingCriteria.CommonTransparent,
                settings.highlightMaterial
            );

            builder.UseRendererList(passData.opaque);
            builder.UseRendererList(passData.transparent);

            builder.SetRenderAttachment(
                resourceData.activeColorTexture,
                0,
                AccessFlags.ReadWrite
            );

            // Highlight shader uses ZTest Always and ZWrite Off.
            // Depth is therefore only needed as a read attachment.
            builder.SetRenderAttachmentDepth(
                resourceData.activeDepthTexture,
                AccessFlags.Read
            );

            builder.SetRenderFunc(
                static (PassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.opaque);
                    context.cmd.DrawRendererList(data.transparent);
                }
            );
        }
    }

    // ------------------------------------------------------------
    // PASS 3: Foreground
    // ------------------------------------------------------------

    private class ForegroundPass : ScriptableRenderPass
    {
        private readonly Settings settings;

        private class PassData
        {
            public RendererListHandle opaque;
            public RendererListHandle transparent;
        }

        public ForegroundPass(Settings settings)
        {
            this.settings = settings;
        }

        public override void RecordRenderGraph(
            RenderGraph renderGraph,
            ContextContainer frameData)
        {

            UniversalRenderingData renderingData =
                frameData.Get<UniversalRenderingData>();

            UniversalCameraData cameraData =
                frameData.Get<UniversalCameraData>();

            UniversalLightData lightData =
                frameData.Get<UniversalLightData>();

            UniversalResourceData resourceData =
                frameData.Get<UniversalResourceData>();

            using var builder =
                renderGraph.AddRasterRenderPass<PassData>(
                    "Foreground Pass",
                    out var passData);
            passData.opaque = CreateRendererList(
                renderGraph,
                renderingData,
                cameraData,
                lightData,
                settings.foregroundLayer,
                RenderQueueRange.opaque,
                cameraData.defaultOpaqueSortFlags
            );

            passData.transparent = CreateRendererList(
                renderGraph,
                renderingData,
                cameraData,
                lightData,
                settings.foregroundLayer,
                RenderQueueRange.transparent,
                SortingCriteria.CommonTransparent
            );

            builder.UseRendererList(passData.opaque);
            builder.UseRendererList(passData.transparent);

            builder.SetRenderAttachment(
                resourceData.activeColorTexture,
                0,
                AccessFlags.ReadWrite
            );

            builder.SetRenderAttachmentDepth(
                resourceData.activeDepthTexture,
                AccessFlags.Write
            );

            builder.SetRenderFunc(
                static (PassData data, RasterGraphContext context) =>
                {
                    // Clear depth ONLY. Preserve scene color.
                    context.cmd.ClearRenderTarget(
                        RTClearFlags.Depth,
                        Color.clear,
                        1.0f,
                        0
                    );

                    // First render opaque objects.
                    context.cmd.DrawRendererList(data.opaque);

                    // Then transparent objects.
                    context.cmd.DrawRendererList(data.transparent);
                }
            );
        }
    }
}
