﻿using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks.Math.Float;

namespace MoonWorksGraphicsTests;

class Texture3DCopyExample : Example
{
	private GraphicsPipeline Pipeline;
	private Buffer VertexBuffer;
	private Buffer IndexBuffer;
	private Texture RenderTexture;
	private Texture Texture3D;
	private Sampler Sampler;

	private float t;
	private Color[] colors =
    [
        Color.Red,
		Color.Green,
		Color.Blue,
	];

	struct FragUniform
	{
		public float Depth;

		public FragUniform(float depth)
		{
			Depth = depth;
		}
	}

    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
		Window = window;
		GraphicsDevice = graphicsDevice;

		Window.SetTitle("Texture3DCopy");

		// Load the shaders
		Shader vertShader = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("TexturedQuad.vert"),
			"main",
			new ShaderCreateInfo
			{
				ShaderStage = ShaderStage.Vertex,
				ShaderFormat = ShaderFormat.SPIRV
			}
		);
		Shader fragShader = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("TexturedQuad3D.frag"),
			"main",
			new ShaderCreateInfo
			{
				ShaderStage = ShaderStage.Fragment,
				ShaderFormat = ShaderFormat.SPIRV,
				SamplerCount = 1,
				UniformBufferCount = 1
			}
		);

		// Create the graphics pipeline
		GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			vertShader,
			fragShader
		);
		pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();

		Pipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

		// Create samplers
		Sampler = new Sampler(GraphicsDevice, SamplerCreateInfo.LinearWrap);

		// Create and populate the GPU resources
		var resourceUploader = new ResourceUploader(GraphicsDevice);

		VertexBuffer = resourceUploader.CreateBuffer(
			[
				new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 0)),
				new PositionTextureVertex(new Vector3(1, -1, 0), new Vector2(1, 0)),
				new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(1, 1)),
				new PositionTextureVertex(new Vector3(-1, 1, 0), new Vector2(0, 1)),
			],
			BufferUsageFlags.Vertex
		);

		IndexBuffer = resourceUploader.CreateBuffer<ushort>(
			[
				0, 1, 2,
				0, 2, 3,
			],
			BufferUsageFlags.Index
		);

		resourceUploader.Upload();
		resourceUploader.Dispose();

		RenderTexture = Texture.CreateTexture2DArray(
			GraphicsDevice,
			16,
			16,
			(uint) colors.Length,
			TextureFormat.R8G8B8A8,
			TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler
		);

		Texture3D = new Texture(GraphicsDevice, new TextureCreateInfo
		{
			Width = 16,
			Height = 16,
			Depth = 3,
			IsCube = false,
			LayerCount = 1,
			LevelCount = 1,
			SampleCount = SampleCount.One,
			Format = TextureFormat.R8G8B8A8,
			UsageFlags = TextureUsageFlags.Sampler
		});

		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();

		// Clear each layer slice of the RT to a different color
		for (uint i = 0; i < colors.Length; i += 1)
		{
			ColorAttachmentInfo attachmentInfo = new ColorAttachmentInfo
			{
				TextureSlice = new TextureSlice
				{
					Texture = RenderTexture,
					Layer = i,
					MipLevel = 0
				},
				ClearColor = colors[i],
				LoadOp = LoadOp.Clear,
				StoreOp = StoreOp.Store
			};
			var renderPass = cmdbuf.BeginRenderPass(attachmentInfo);
			cmdbuf.EndRenderPass(renderPass);
		}

		// Copy each layer slice to a different 3D depth
		var copyPass = cmdbuf.BeginCopyPass();
		for (var i = 0; i < 3; i += 1)
		{
			copyPass.CopyTextureToTexture(
				new TextureLocation(new TextureSlice(RenderTexture, 0, (uint) i)),
				new TextureLocation(Texture3D, 0, 0, (uint) i),
				16,
				16,
				1,
				false
			);
		}
		cmdbuf.EndCopyPass(copyPass);

		GraphicsDevice.Submit(cmdbuf);
	}

	public override void Update(System.TimeSpan delta)
	{
		t += (float) delta.TotalSeconds;
	}

	public override void Draw(double alpha)
	{
		FragUniform fragUniform = new FragUniform(t);

		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
		if (swapchainTexture != null)
		{
			var renderPass = cmdbuf.BeginRenderPass(
				new ColorAttachmentInfo(
					swapchainTexture,
					false,
					Color.Black
				)
			);
			renderPass.BindGraphicsPipeline(Pipeline);
			renderPass.BindVertexBuffer(VertexBuffer);
			renderPass.BindIndexBuffer(IndexBuffer, IndexElementSize.Sixteen);
			renderPass.BindFragmentSampler(new TextureSamplerBinding(Texture3D, Sampler));
			cmdbuf.PushFragmentUniformData(fragUniform);
			renderPass.DrawIndexedPrimitives(0, 0, 2);
			cmdbuf.EndRenderPass(renderPass);
		}
		GraphicsDevice.Submit(cmdbuf);
	}

    public override void Destroy()
    {
        Pipeline.Dispose();
		VertexBuffer.Dispose();
		IndexBuffer.Dispose();
		RenderTexture.Dispose();
		Texture3D.Dispose();
		Sampler.Dispose();
    }
}
