using System;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks.Math.Float;
using Buffer = MoonWorks.Graphics.Buffer;

namespace MoonWorksGraphicsTests;

/*
 * This example builds the sprite batch on the CPU.
 * Compare with ComputeSpriteBatchExample.
 * This example should be MUCH slower.
 *
 * For speed comparisons, make sure to set the framerate to Uncapped
 * and present mode to Immediate.
 */
class CPUSpriteBatchExample : Example
{
    GraphicsPipeline RenderPipeline;
    Sampler Sampler;
	Texture SpriteTexture;
    TransferBuffer SpriteVertexTransferBuffer;
	Buffer SpriteVertexBuffer;
	Buffer SpriteIndexBuffer;

    const int SPRITE_COUNT = 8192;

    struct SpriteInstanceData
    {
        public Vector3 Position;
        public float Rotation;
        public Vector2 Size;
        public Vector4 Color;
    }

    SpriteInstanceData[] InstanceData = new SpriteInstanceData[SPRITE_COUNT];

	Random Random = new Random();

    public override unsafe void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
        Window = window;
        GraphicsDevice = graphicsDevice;

        Window.SetTitle("CPUSpriteBatch");

        Shader vertShader = Shader.CreateFromFile(
			GraphicsDevice,
			TestUtils.GetShaderPath("TexturedQuadColorWithMatrix.vert"),
			"main",
			new ShaderCreateInfo
			{
				Stage = ShaderStage.Vertex,
				Format = ShaderFormat.SPIRV,
				NumUniformBuffers = 1
			}
		);

		Shader fragShader = Shader.CreateFromFile(
			GraphicsDevice,
			TestUtils.GetShaderPath("TexturedQuadColor.frag"),
			"main",
			new ShaderCreateInfo
			{
				Stage = ShaderStage.Fragment,
				Format = ShaderFormat.SPIRV,
				NumSamplers = 1
			}
		);

		GraphicsPipelineCreateInfo renderPipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			vertShader,
			fragShader
		);
		renderPipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureColorVertex>();

		RenderPipeline = GraphicsPipeline.Create(GraphicsDevice, renderPipelineCreateInfo);

		Sampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.PointClamp);

		// Create and populate the sprite texture
		var resourceUploader = new ResourceUploader(GraphicsDevice);

		SpriteTexture = resourceUploader.CreateTexture2DFromCompressed(
			TestUtils.GetTexturePath("ravioli.png"),
			TextureFormat.R8G8B8A8Unorm,
			TextureUsageFlags.Sampler
		);

		resourceUploader.Upload();
		resourceUploader.Dispose();

        SpriteVertexBuffer = Buffer.Create<PositionTextureColorVertex>(
			GraphicsDevice,
			BufferUsageFlags.Vertex,
			SPRITE_COUNT * 4
		);

		SpriteIndexBuffer = Buffer.Create<uint>(
			GraphicsDevice,
			BufferUsageFlags.Index,
			SPRITE_COUNT * 6
		);

        SpriteVertexTransferBuffer = TransferBuffer.Create<PositionTextureColorVertex>(
            GraphicsDevice,
            TransferBufferUsage.Upload,
            SPRITE_COUNT * 4
        );

		TransferBuffer spriteIndexTransferBuffer = TransferBuffer.Create<uint>(
			GraphicsDevice,
			TransferBufferUsage.Upload,
			SPRITE_COUNT * 6
		);

        var indexSpan = spriteIndexTransferBuffer.Map<uint>(false);
		for (int i = 0, j = 0; i < SPRITE_COUNT * 6; i += 6, j += 4)
		{
			indexSpan[i]     =  (uint) j;
			indexSpan[i + 1] =  (uint) j + 1;
			indexSpan[i + 2] =  (uint) j + 2;
			indexSpan[i + 3] =  (uint) j + 3;
			indexSpan[i + 4] =  (uint) j + 2;
			indexSpan[i + 5] =  (uint) j + 1;
		}
		spriteIndexTransferBuffer.Unmap();

		var cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		var copyPass = cmdbuf.BeginCopyPass();
		copyPass.UploadToBuffer(spriteIndexTransferBuffer, SpriteIndexBuffer, false);
		cmdbuf.EndCopyPass(copyPass);
		GraphicsDevice.Submit(cmdbuf);
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

		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
		if (swapchainTexture != null)
		{
            // generate sprite instance data
            for (var i = 0; i < SPRITE_COUNT; i += 1)
            {
                InstanceData[i] = new SpriteInstanceData
                {
					Position = new Vector3(Random.Next(640), Random.Next(480), 0),
					Rotation = (float) (Random.NextDouble() * System.Math.PI * 2),
					Size = new Vector2(32, 32),
					Color = new Vector4(1f, 1f, 1f, 1f)
                };
            }

            // transform vertex data
            var dataSpan = SpriteVertexTransferBuffer.Map<PositionTextureColorVertex>(true);
            for (var i = 0; i < SPRITE_COUNT; i += 1)
            {
                var transform =
                    Matrix4x4.CreateScale(InstanceData[i].Size.X, InstanceData[i].Size.Y, 1) *
                    Matrix4x4.CreateRotationZ(InstanceData[i].Rotation) *
                    Matrix4x4.CreateTranslation(InstanceData[i].Position);

                dataSpan[i*4] = new PositionTextureColorVertex
                {
                    Position = new Vector4(Vector3.Transform(new Vector3(0, 0, 0), transform), 1),
                    TexCoord = new Vector2(0, 0),
                    Color = InstanceData[i].Color
                };

                dataSpan[i*4 + 1] = new PositionTextureColorVertex
                {
                    Position = new Vector4(Vector3.Transform(new Vector3(1, 0, 0), transform), 1),
                    TexCoord = new Vector2(1, 0),
                    Color = InstanceData[i].Color
                };

                dataSpan[i*4 + 2] = new PositionTextureColorVertex
                {
                    Position = new Vector4(Vector3.Transform(new Vector3(0, 1, 0), transform), 1),
                    TexCoord = new Vector2(0, 1),
                    Color = InstanceData[i].Color
                };

                dataSpan[i*4 + 3] = new PositionTextureColorVertex
                {
                    Position = new Vector4(Vector3.Transform(new Vector3(1, 1, 0), transform), 1),
                    TexCoord = new Vector2(1, 1),
                    Color = InstanceData[i].Color
                };
            }
            SpriteVertexTransferBuffer.Unmap();

            // Upload vertex data
            var copyPass = cmdbuf.BeginCopyPass();
            copyPass.UploadToBuffer(SpriteVertexTransferBuffer, SpriteVertexBuffer, true);
            cmdbuf.EndCopyPass(copyPass);

            // Render sprites using vertex buffer
			var renderPass = cmdbuf.BeginRenderPass(
				new ColorTargetInfo
				{
					Texture = swapchainTexture.Handle,
					LoadOp = LoadOp.Clear,
					ClearColor = Color.Black
				}
			);

			renderPass.BindGraphicsPipeline(RenderPipeline);
			renderPass.BindVertexBuffer(SpriteVertexBuffer);
			renderPass.BindIndexBuffer(SpriteIndexBuffer, IndexElementSize.ThirtyTwo);
			renderPass.BindFragmentSampler(new TextureSamplerBinding(SpriteTexture, Sampler));
			cmdbuf.PushVertexUniformData(cameraMatrix);
			renderPass.DrawIndexedPrimitives(SPRITE_COUNT * 6, 1, 0, 0, 0);

			cmdbuf.EndRenderPass(renderPass);
        }

        GraphicsDevice.Submit(cmdbuf);
    }

    public override void Destroy()
    {
        RenderPipeline.Dispose();
        Sampler.Dispose();
        SpriteTexture.Dispose();
        SpriteVertexTransferBuffer.Dispose();
        SpriteVertexBuffer.Dispose();
        SpriteIndexBuffer.Dispose();
    }
}
