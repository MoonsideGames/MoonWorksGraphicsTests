﻿using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks.Math.Float;
using System.Runtime.InteropServices;

namespace MoonWorksGraphicsTests;

class DrawIndirectExample : Example
{
	private GraphicsPipeline GraphicsPipeline;
	private Buffer VertexBuffer;
	private Buffer DrawBuffer;

    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
		Window = window;
		GraphicsDevice = graphicsDevice;

		Window.SetTitle("DrawIndirect");

		// Load the shaders
		Shader vertShader = Shader.Create(
			GraphicsDevice,
			TestUtils.GetShaderPath("PositionColor.vert"),
			"main",
			new ShaderCreateInfo
			{
				Stage = ShaderStage.Vertex,
				Format = ShaderFormat.SPIRV
			}
		);

		Shader fragShader = Shader.Create(
			GraphicsDevice,
			TestUtils.GetShaderPath("SolidColor.frag"),
			"main",
			new ShaderCreateInfo
			{
				Stage = ShaderStage.Fragment,
				Format = ShaderFormat.SPIRV
			}
		);

		// Create the graphics pipeline
		GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			vertShader,
			fragShader
		);
		pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionColorVertex>();
		GraphicsPipeline = GraphicsPipeline.Create(GraphicsDevice, pipelineCreateInfo);

		// Create and populate the vertex buffer
		var resourceUploader = new ResourceUploader(GraphicsDevice);

		VertexBuffer = resourceUploader.CreateBuffer(
			[
				new PositionColorVertex(new Vector3(-0.5f,  1, 0), Color.Blue),
				new PositionColorVertex(new Vector3(  -1f, -1, 0), Color.Green),
				new PositionColorVertex(new Vector3(   0f, -1, 0), Color.Red),

				new PositionColorVertex(new Vector3(0.5f,  1, 0), Color.Blue),
				new PositionColorVertex(new Vector3(  1f, -1, 0), Color.Green),
				new PositionColorVertex(new Vector3(  0f, -1, 0), Color.Red),
			],
			BufferUsageFlags.Vertex
		);

		DrawBuffer = resourceUploader.CreateBuffer(
			[
				new IndirectDrawCommand
				{
					NumVertices = 3,
					NumInstances = 1,
					FirstVertex = 3
				},
				new IndirectDrawCommand
				{
					NumVertices = 3,
					NumInstances = 1
				}
			],
			BufferUsageFlags.Indirect
		);

		resourceUploader.Upload();
		resourceUploader.Dispose();
	}

	public override void Update(System.TimeSpan delta) { }

	public override void Draw(double alpha)
	{
		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
		if (swapchainTexture != null)
		{
			var renderPass = cmdbuf.BeginRenderPass(
				new ColorTargetInfo(swapchainTexture, Color.Black)
			);
			renderPass.BindGraphicsPipeline(GraphicsPipeline);
			renderPass.BindVertexBuffer(VertexBuffer);
			renderPass.DrawPrimitivesIndirect(DrawBuffer, 0, 2);
			cmdbuf.EndRenderPass(renderPass);
		}
		GraphicsDevice.Submit(cmdbuf);
	}

    public override void Destroy()
    {
        GraphicsPipeline.Dispose();
		VertexBuffer.Dispose();
		DrawBuffer.Dispose();
    }
}
