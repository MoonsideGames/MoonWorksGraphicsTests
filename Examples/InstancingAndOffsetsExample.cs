﻿using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks.Math.Float;

namespace MoonWorksGraphicsTests;

class InstancingAndOffsetsExample : Example
{
	private GraphicsPipeline Pipeline;
	private GpuBuffer VertexBuffer;
	private GpuBuffer IndexBuffer;

	private bool useVertexOffset;
	private bool useIndexOffset;

    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
		Window = window;
		GraphicsDevice = graphicsDevice;
		Inputs = inputs;

		Window.SetTitle("InstancingAndOffsets");

		Logger.LogInfo("Press Left to toggle vertex offset\nPress Right to toggle index offset");

		// Load the shaders
		Shader vertShader = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("PositionColorInstanced.vert"),
			"main",
			ShaderStage.Vertex,
			ShaderFormat.SPIRV
		);

		Shader fragShader = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("SolidColor.frag"),
			"main",
			ShaderStage.Fragment,
			ShaderFormat.SPIRV
		);

		// Create the graphics pipeline
		GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			vertShader,
			fragShader
		);
		pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionColorVertex>();
		Pipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

		// Create and populate the vertex and index buffers
		var resourceUploader = new ResourceUploader(GraphicsDevice);

		VertexBuffer = resourceUploader.CreateBuffer(
			[
				new PositionColorVertex(new Vector3(-1, -1, 0), Color.Red),
				new PositionColorVertex(new Vector3( 1, -1, 0), Color.Lime),
				new PositionColorVertex(new Vector3( 0,  1, 0), Color.Blue),

				new PositionColorVertex(new Vector3(-1, -1, 0), Color.Orange),
				new PositionColorVertex(new Vector3( 1, -1, 0), Color.Green),
				new PositionColorVertex(new Vector3( 0,  1, 0), Color.Aqua),

				new PositionColorVertex(new Vector3(-1, -1, 0), Color.White),
				new PositionColorVertex(new Vector3( 1, -1, 0), Color.White),
				new PositionColorVertex(new Vector3( 0,  1, 0), Color.White),
			],
			BufferUsageFlags.Vertex
		);

		IndexBuffer = resourceUploader.CreateBuffer<ushort>(
			[
				0, 1, 2,
				3, 4, 5,
			],
			BufferUsageFlags.Index
		);

		resourceUploader.Upload();
		resourceUploader.Dispose();
	}

	public override void Update(System.TimeSpan delta)
	{
		if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Left))
		{
			useVertexOffset = !useVertexOffset;
			Logger.LogInfo("Using vertex offset: " + useVertexOffset);
		}

		if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Right))
		{
			useIndexOffset = !useIndexOffset;
			Logger.LogInfo("Using index offset: " + useIndexOffset);
		}
	}

	public override void Draw(double alpha)
	{
		uint vertexOffset = useVertexOffset ? 3u : 0;
		uint indexOffset = useIndexOffset ? 3u : 0;

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
			renderPass.DrawIndexedPrimitives(vertexOffset, indexOffset, 1, 16);
			cmdbuf.EndRenderPass(renderPass);
		}
		GraphicsDevice.Submit(cmdbuf);
	}

    public override void Destroy()
    {
        Pipeline.Dispose();
		VertexBuffer.Dispose();
		IndexBuffer.Dispose();
    }
}