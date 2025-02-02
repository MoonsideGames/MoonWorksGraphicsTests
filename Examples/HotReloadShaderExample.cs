using System;
using System.IO;
using System.Numerics;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;

namespace MoonWorksGraphicsTests;

record struct Uniforms(Vector2 Resolution, float Time);

class HotReloadShaderExample : Example
{
    GraphicsPipeline Pipeline;
    Shader VertexShader;
    Shader FragmentShader;
    float Time;

    FileSystemWatcher Watcher;
    bool NeedReload;

    public override void Init()
    {
        Window.SetTitle("HotReloadShader");

        VertexShader = ShaderCross.Create(
            GraphicsDevice,
            RootTitleStorage,
            TestUtils.GetHLSLPath("Fullscreen.vert"),
            "main",
            ShaderCross.ShaderFormat.HLSL,
            ShaderStage.Vertex
        );

        LoadPipeline();

        Logger.LogInfo("Edit HotReload.frag.hlsl in the Content directory to reload the shader!");

        Watcher = new FileSystemWatcher(Path.Combine(SDL3.SDL.SDL_GetBasePath(), "Content", "Shaders", "HLSL"));
        Watcher.Filter = "HotReload.frag.hlsl";
        Watcher.NotifyFilter = NotifyFilters.LastWrite;
        Watcher.EnableRaisingEvents = true;
        Watcher.Changed += OnChanged;
    }

    public override void Update(TimeSpan delta)
    {
        Time += (float) delta.TotalSeconds;

        if (NeedReload)
        {
            Logger.LogInfo("File change detected, reloading pipeline...");
            LoadPipeline();
            Logger.LogInfo("Done!");

            NeedReload = false;
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
                    new Vector2(swapchainTexture.Width, swapchainTexture.Height),
                    Time
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
        VertexShader.Dispose();
        FragmentShader.Dispose();
    }

    private void LoadPipeline()
    {
        var fragmentShader = ShaderCross.Create(
            GraphicsDevice,
            RootTitleStorage,
            TestUtils.GetHLSLPath("HotReload.frag"),
            "main",
            ShaderCross.ShaderFormat.HLSL,
            ShaderStage.Fragment
        );

        if (fragmentShader == null)
        {
            Logger.LogError("Failed to compile fragment shader!");
            Logger.LogError(SDL3.SDL.SDL_GetError());
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
                            Format = Window.SwapchainFormat,
                            BlendState = ColorTargetBlendState.NoBlend
                        }
                    ]
                },
                DepthStencilState = DepthStencilState.Disable,
                VertexShader = VertexShader,
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
            Logger.LogError(SDL3.SDL.SDL_GetError());
            return;
        }

        Pipeline?.Dispose();
        FragmentShader?.Dispose();

        FragmentShader = fragmentShader;
        Pipeline = pipeline;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType != WatcherChangeTypes.Changed)
        {
            return;
        }

        NeedReload = true;
    }
}
