﻿using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;

namespace MoonWorksGraphicsTests;

class ComputeUniformsExample : Example
{
	private ComputePipeline GradientPipeline;
	private Texture RenderTexture;

	record struct GradientTextureComputeUniforms(float Time);
	private GradientTextureComputeUniforms Uniforms;

    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
		Window = window;
		GraphicsDevice = graphicsDevice;

		Window.SetTitle("ComputeUniforms");
		Uniforms.Time = 0;

		// Create the compute pipeline that writes texture data
		Shader gradientTextureComputeShader = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("GradientTexture.comp"),
			"main",
			ShaderStage.Compute,
			ShaderFormat.SPIRV
		);

		GradientPipeline = new ComputePipeline(
			GraphicsDevice,
			gradientTextureComputeShader,
			new ComputePipelineResourceInfo {
				ReadWriteStorageTextureCount = 1,
				UniformBufferCount = 1
			}
		);

		gradientTextureComputeShader.Dispose();

		RenderTexture = Texture.CreateTexture2D(
			GraphicsDevice,
			Window.Width,
			Window.Height,
			TextureFormat.R8G8B8A8,
			TextureUsageFlags.ComputeStorageWrite | TextureUsageFlags.Sampler
		);
	}

	public override void Update(System.TimeSpan delta)
	{
		Uniforms.Time += (float) delta.TotalSeconds;
	}

	public override void Draw(double alpha)
	{
		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
		if (swapchainTexture != null)
		{
			var computePass = cmdbuf.BeginComputePass(new StorageTextureReadWriteBinding
			{
				TextureSlice = RenderTexture,
				Cycle = true
			});

			computePass.BindComputePipeline(GradientPipeline);
			computePass.PushUniformData(Uniforms);
			computePass.Dispatch(RenderTexture.Width / 8, RenderTexture.Height / 8, 1);
			cmdbuf.EndComputePass(computePass);

			cmdbuf.Blit(RenderTexture, swapchainTexture, Filter.Linear, false);
		}
		GraphicsDevice.Submit(cmdbuf);
	}

    public override void Destroy()
    {
		GradientPipeline.Dispose();
		RenderTexture.Dispose();
    }
}