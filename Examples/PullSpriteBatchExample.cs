using System;
using System.Runtime.InteropServices;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using System.Numerics;
using Buffer = MoonWorks.Graphics.Buffer;

namespace MoonWorksGraphicsTests;

class PullSpriteBatchExample : Example
{
    GraphicsPipeline RenderPipeline;
    Sampler Sampler;
    Texture SpriteAtlasTexture;
    TransferBuffer SpriteDataTransferBuffer; 
    Buffer SpriteDataBuffer;

    const int MAX_SPRITE_COUNT = 8192;

    Random Random = new Random();

    [StructLayout(LayoutKind.Explicit, Size = 64)]
    struct SpriteInstance
    {
        [FieldOffset(0)]
        public Vector3 Position;

        [FieldOffset(12)]
        public float Rotation;

        [FieldOffset(16)]
        public Vector2 Size;

        [FieldOffset(24)]
        public Vector2 Padding;

        [FieldOffset(32)]
        public float TexU;

        [FieldOffset(36)]
        public float TexV;

        [FieldOffset(40)]
        public float TexW;

        [FieldOffset(44)]
        public float TexH;

        [FieldOffset(48)]
        public Vector4 Color;
    }

    public override unsafe void Init()
    {
        Window.SetTitle("PullSpriteBatch");

        Shader vertShader = ShaderCross.Create(
            GraphicsDevice,
            RootTitleStorage,
            TestUtils.GetHLSLPath("PullSpriteBatch.vert"),
            "main",
            ShaderCross.ShaderFormat.HLSL,
            ShaderStage.Vertex
        );

        Shader fragShader = ShaderCross.Create(
            GraphicsDevice,
            RootTitleStorage,
            TestUtils.GetHLSLPath("TexturedQuadColor.frag"),
            "main",
            ShaderCross.ShaderFormat.HLSL,
            ShaderStage.Fragment
        );

        var renderPipelineCreateInfo = new GraphicsPipelineCreateInfo
        {
            TargetInfo = new GraphicsPipelineTargetInfo
            {
                ColorTargetDescriptions = [
                    new ColorTargetDescription
                    {
                        Format = Window.SwapchainFormat,
                        BlendState = ColorTargetBlendState.NonPremultipliedAlphaBlend
                    }
                ]
            },
            DepthStencilState = DepthStencilState.Disable,
            MultisampleState = MultisampleState.None,
            PrimitiveType = PrimitiveType.TriangleList,
            RasterizerState = RasterizerState.CCW_CullNone,
            VertexInputState = VertexInputState.Empty,
            VertexShader = vertShader,
            FragmentShader = fragShader
        };
                
        RenderPipeline = GraphicsPipeline.Create(GraphicsDevice, renderPipelineCreateInfo);

        Sampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.PointClamp);

        var resourceUploader = new ResourceUploader(GraphicsDevice);

        SpriteAtlasTexture = resourceUploader.CreateTexture2DFromCompressed(
            RootTitleStorage,
            TestUtils.GetTexturePath("ravioli_atlas.png"),
            TextureFormat.R8G8B8A8Unorm,
            TextureUsageFlags.Sampler
        );

        resourceUploader.Upload();
        resourceUploader.Dispose();

        SpriteDataTransferBuffer = TransferBuffer.Create<SpriteInstance>(
            GraphicsDevice,
            TransferBufferUsage.Upload,
            MAX_SPRITE_COUNT
        );

        SpriteDataBuffer = Buffer.Create<SpriteInstance>(
            GraphicsDevice,
            BufferUsageFlags.GraphicsStorageRead,
            MAX_SPRITE_COUNT
        );
    }

    public override void Update(TimeSpan delta)
    {

    }

    public override unsafe void Draw(double alpha)
    {
        Matrix4x4 cameraMatrix =
            Matrix4x4.CreateOrthographicOffCenter(
                0,
                640,
                480,
                0,
                0,
                -1f
            );

        float[] uCoords = [ 0.0f, 0.5f, 0.0f, 0.5f ];
        float[] vCoords = [ 0.0f, 0.0f, 0.5f, 0.5f ];

        CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
        Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
        if (swapchainTexture != null)
        {
            // Build sprite data transfer
            var data = SpriteDataTransferBuffer.Map<SpriteInstance>(true);
            for (var i = 0; i < MAX_SPRITE_COUNT; i += 1)
            {
                int ravioli = Random.Next(4);
                data[i].Position = new Vector3(Random.Next(640), Random.Next(480), 0);
                data[i].Rotation = 0;
                data[i].Size = new Vector2(32, 32);
                data[i].TexU = uCoords[ravioli];
                data[i].TexV = vCoords[ravioli];
                data[i].TexW = 0.5f;
                data[i].TexH = 0.5f;
                data[i].Color = new Vector4(1f, 1f, 1f, 1f);
            }
            SpriteDataTransferBuffer.Unmap();

            // Upload sprite data to buffer
            var copyPass = cmdbuf.BeginCopyPass();
            copyPass.UploadToBuffer(SpriteDataTransferBuffer, SpriteDataBuffer, true);
            cmdbuf.EndCopyPass(copyPass);

            var renderPass = cmdbuf.BeginRenderPass(
                new ColorTargetInfo(swapchainTexture, Color.Black)
            );

            cmdbuf.PushVertexUniformData(cameraMatrix);

            renderPass.BindGraphicsPipeline(RenderPipeline);
            renderPass.BindFragmentSamplers(new TextureSamplerBinding(SpriteAtlasTexture, Sampler));
            renderPass.BindVertexStorageBuffers(SpriteDataBuffer);
            renderPass.DrawPrimitives(MAX_SPRITE_COUNT * 6, 1, 0, 0);

            cmdbuf.EndRenderPass(renderPass);
        }

        GraphicsDevice.Submit(cmdbuf);
    }

    public override void Destroy()
    {
        RenderPipeline.Dispose();
        Sampler.Dispose();
        SpriteAtlasTexture.Dispose();
        SpriteDataTransferBuffer.Dispose(); 
        SpriteDataBuffer.Dispose();
    }
}
