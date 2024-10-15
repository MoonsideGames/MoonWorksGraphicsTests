﻿using System.Runtime.InteropServices;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks.Math.Float;

namespace MoonWorksGraphicsTests;

class TexturedAnimatedQuadExample : Example
{
	private GraphicsPipeline Pipeline;
	private Buffer VertexBuffer;
	private Buffer IndexBuffer;
	private Texture Texture;
	private Sampler Sampler;

	private float t;

	[StructLayout(LayoutKind.Sequential)]
	private struct FragmentUniforms
	{
		public Vector4 MultiplyColor;

		public FragmentUniforms(Vector4 multiplyColor)
		{
			MultiplyColor = multiplyColor;
		}
	}

    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
		Window = window;
		GraphicsDevice = graphicsDevice;

		Window.SetTitle("TexturedAnimatedQuad");

		// Load the shaders
		Shader vertShader = Shader.Create(
			GraphicsDevice,
			TestUtils.GetShaderPath("TexturedQuadWithMatrix.vert"),
			"main",
			new ShaderCreateInfo
			{
				Stage = ShaderStage.Vertex,
				Format = ShaderFormat.SPIRV,
				NumUniformBuffers = 1
			}
		);

		Shader fragShader = Shader.Create(
			GraphicsDevice,
			TestUtils.GetShaderPath("TexturedQuadWithMultiplyColor.frag"),
			"main",
			new ShaderCreateInfo
			{
				Stage = ShaderStage.Fragment,
				Format = ShaderFormat.SPIRV,
				NumSamplers = 1,
				NumUniformBuffers = 1
			}
		);

		// Create the graphics pipeline
		GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			vertShader,
			fragShader
		);
		pipelineCreateInfo.TargetInfo.ColorTargetDescriptions[0].BlendState = ColorTargetBlendState.PremultipliedAlphaBlend;
		pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();

		Pipeline = GraphicsPipeline.Create(GraphicsDevice, pipelineCreateInfo);

		Sampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.PointClamp);

		// Create and populate the GPU resources
		var resourceUploader = new ResourceUploader(GraphicsDevice);

		VertexBuffer = resourceUploader.CreateBuffer(
			[
				new PositionTextureVertex(new Vector3(-0.5f, -0.5f, 0), new Vector2(0, 0)),
				new PositionTextureVertex(new Vector3(0.5f, -0.5f, 0), new Vector2(1, 0)),
				new PositionTextureVertex(new Vector3(0.5f, 0.5f, 0), new Vector2(1, 1)),
				new PositionTextureVertex(new Vector3(-0.5f, 0.5f, 0), new Vector2(0, 1)),
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

		Texture = resourceUploader.CreateTexture2DFromCompressed(TestUtils.GetTexturePath("ravioli.png"), TextureFormat.R8G8B8A8Unorm, TextureUsageFlags.Sampler);

		resourceUploader.Upload();
		resourceUploader.Dispose();
	}

	public override void Update(System.TimeSpan delta)
	{
		t += (float) delta.TotalSeconds;
	}

	public override void Draw(double alpha)
	{
		TransformVertexUniform vertUniforms;
		FragmentUniforms fragUniforms;

		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
		if (swapchainTexture != null)
		{
			var renderPass = cmdbuf.BeginRenderPass(
				new ColorTargetInfo(swapchainTexture, Color.Black)
			);
			renderPass.BindGraphicsPipeline(Pipeline);
			renderPass.BindVertexBuffer(VertexBuffer);
			renderPass.BindIndexBuffer(IndexBuffer, IndexElementSize.Sixteen);
			renderPass.BindFragmentSampler(new TextureSamplerBinding(Texture, Sampler));

			// Top-left
			vertUniforms = new TransformVertexUniform(Matrix4x4.CreateRotationZ(t) * Matrix4x4.CreateTranslation(new Vector3(-0.5f, -0.5f, 0)));
			fragUniforms = new FragmentUniforms(new Vector4(1f, 0.5f + System.MathF.Sin(t) * 0.5f, 1f, 1f));
			cmdbuf.PushVertexUniformData(vertUniforms);
			cmdbuf.PushFragmentUniformData(fragUniforms);
			renderPass.DrawIndexedPrimitives(6, 1, 0, 0, 0);

			// Top-right
			vertUniforms = new TransformVertexUniform(Matrix4x4.CreateRotationZ((2 * System.MathF.PI) - t) * Matrix4x4.CreateTranslation(new Vector3(0.5f, -0.5f, 0)));
			fragUniforms = new FragmentUniforms(new Vector4(1f, 0.5f + System.MathF.Cos(t) * 0.5f, 1f, 1f));
			cmdbuf.PushVertexUniformData(vertUniforms);
			cmdbuf.PushFragmentUniformData(fragUniforms);
			renderPass.DrawIndexedPrimitives(6, 1, 0, 0, 0);

			// Bottom-left
			vertUniforms = new TransformVertexUniform(Matrix4x4.CreateRotationZ(t) * Matrix4x4.CreateTranslation(new Vector3(-0.5f, 0.5f, 0)));
			fragUniforms = new FragmentUniforms(new Vector4(1f, 0.5f + System.MathF.Sin(t) * 0.2f, 1f, 1f));
			cmdbuf.PushVertexUniformData(vertUniforms);
			cmdbuf.PushFragmentUniformData(fragUniforms);
			renderPass.DrawIndexedPrimitives(6, 1, 0, 0, 0);

			// Bottom-right
			vertUniforms = new TransformVertexUniform(Matrix4x4.CreateRotationZ(t) * Matrix4x4.CreateTranslation(new Vector3(0.5f, 0.5f, 0)));
			fragUniforms = new FragmentUniforms(new Vector4(1f, 0.5f + System.MathF.Cos(t) * 1f, 1f, 1f));
			cmdbuf.PushVertexUniformData(vertUniforms);
			cmdbuf.PushFragmentUniformData(fragUniforms);
			renderPass.DrawIndexedPrimitives(6, 1, 0, 0, 0);

			cmdbuf.EndRenderPass(renderPass);
		}

		GraphicsDevice.Submit(cmdbuf);
	}

    public override void Destroy()
    {
        Pipeline.Dispose();
		VertexBuffer.Dispose();
		IndexBuffer.Dispose();
		Texture.Dispose();
		Sampler.Dispose();
    }
}
