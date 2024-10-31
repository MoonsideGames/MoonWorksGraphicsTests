using System;
using System.Numerics;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;

namespace MoonWorksGraphicsTests;

record struct Uniforms(float Time, Vector2 Resolution);

class HotReloadShaderExample : Example
{
    GraphicsPipeline Pipeline;
    Shader FragmentShader;
    float Time;

    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
        Window = window;
        GraphicsDevice = graphicsDevice;
        Inputs = inputs;

        Window.SetTitle("HotReloadShader");

        LoadPipeline();

        Logger.LogInfo("Edit HotReload.frag.hlsl in the Content directory and press Down to reload the shader!");
    }

    public override void Update(TimeSpan delta)
    {
        Time += (float) delta.TotalSeconds;

        if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Bottom))
        {
            Logger.LogInfo("Reloading pipeline...");
            LoadPipeline();
            Logger.LogInfo("Done!");
        }
    }

    public override void Draw(double alpha)
    {
        CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
        Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
        if (swapchainTexture != null)
        {
            var renderPass = cmdbuf.BeginRenderPass(
                new ColorTargetInfo(swapchainTexture, LoadOp.DontCare)
            );
            renderPass.BindGraphicsPipeline(Pipeline);
            cmdbuf.PushFragmentUniformData(
                new Uniforms(
                    Time,
                    new Vector2(swapchainTexture.Width, swapchainTexture.Height)
                )
            );
            renderPass.DrawPrimitives(3, 1, 0, 0);
            cmdbuf.EndRenderPass(renderPass);
        }
        GraphicsDevice.Submit(cmdbuf);
    }

    public override void Destroy()
    {
        Pipeline.Dispose();
        FragmentShader.Dispose();
    }

    private void LoadPipeline()
    {
        var fragmentShader = ShaderCross.Create(
            GraphicsDevice,
            TestUtils.GetHLSLPath("HotReload.frag"),
            "main",
            new ShaderCross.ShaderCreateInfo
            {
                Format = ShaderCross.ShaderFormat.HLSL,
                Stage = ShaderStage.Fragment,
                NumUniformBuffers = 1
            }
        );

        if (fragmentShader == null)
        {
            Logger.LogError("Failed to compile fragment shader!");
            return;
        }

        var pipeline = GraphicsPipeline.Create(
            GraphicsDevice,
            new GraphicsPipelineCreateInfo
            {
                TargetInfo = new GraphicsPipelineTargetInfo
                {
                    ColorTargetDescriptions =
                    [
                        new ColorTargetDescription
                        {
                            Format = TextureFormat.R8G8B8A8Unorm,
                            BlendState = ColorTargetBlendState.NoBlend
                        }
                    ]
                },
                DepthStencilState = DepthStencilState.Disable,
                VertexShader = GraphicsDevice.FullscreenVertexShader,
                FragmentShader = fragmentShader,
                VertexInputState = VertexInputState.Empty,
                RasterizerState = RasterizerState.CCW_CullNone,
                PrimitiveType = PrimitiveType.TriangleList,
                MultisampleState = MultisampleState.None
            }
        );

        if (pipeline == null)
        {
            Logger.LogError("Failed to compile pipeline!");
            return;
        }

        Pipeline?.Dispose();
        FragmentShader?.Dispose();

        FragmentShader = fragmentShader;
        Pipeline = pipeline;
    }
}
