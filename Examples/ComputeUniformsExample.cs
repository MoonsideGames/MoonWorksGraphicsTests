using MoonWorks;
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
		GradientPipeline = ComputePipeline.Create(
			GraphicsDevice,
			TestUtils.GetShaderPath("GradientTexture.comp"),
			"main",
			new ComputePipelineCreateInfo
			{
				Format = ShaderFormat.SPIRV,
				NumReadWriteStorageTextures = 1,
				NumUniformBuffers = 1,
				ThreadCountX = 8,
				ThreadCountY = 8,
				ThreadCountZ = 1
			}
		);

		RenderTexture = Texture.Create2D(
			GraphicsDevice,
			Window.Width,
			Window.Height,
			TextureFormat.R8G8B8A8Unorm,
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
				Texture = RenderTexture.Handle,
				Cycle = true
			});

			cmdbuf.PushComputeUniformData(Uniforms);

			computePass.BindComputePipeline(GradientPipeline);
			computePass.Dispatch(RenderTexture.Width / 8, RenderTexture.Height / 8, 1);
			cmdbuf.EndComputePass(computePass);

			cmdbuf.Blit(new BlitInfo
			{
				LoadOp = LoadOp.DontCare,
				Source = new BlitRegion
				{
					Texture = RenderTexture.Handle,
					W = RenderTexture.Width,
					H = RenderTexture.Height
				},
				Destination = new BlitRegion
				{
					Texture = swapchainTexture.Handle,
					W = swapchainTexture.Width,
					H = swapchainTexture.Height
				},
				Filter = Filter.Linear
			});
		}

		GraphicsDevice.Submit(cmdbuf);
	}

    public override void Destroy()
    {
		GradientPipeline.Dispose();
		RenderTexture.Dispose();
    }
}
