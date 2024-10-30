using MoonWorks.Graphics;
using MoonWorks.Math.Float;
using MoonWorks.Math;
using MoonWorks;
using MoonWorks.Input;

namespace MoonWorksGraphicsTests;

class RenderTextureCubeExample : Example
{
	private GraphicsPipeline pipeline;
	private Buffer vertexBuffer;
	private Buffer indexBuffer;
	private Texture cubemap;
	private Sampler sampler;

	private Vector3 camPos = new Vector3(0, 0, 4f);

	private Color[] colors = new Color[]
	{
		Color.Red,
		Color.Green,
		Color.Blue,
		Color.Orange,
		Color.Yellow,
		Color.Purple,
	};

    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
		Window = window;
		GraphicsDevice = graphicsDevice;
		Inputs = inputs;

		Window.SetTitle("RenderTextureCube");

		Logger.LogInfo("Press Down to view the other side of the cubemap");

		// Load the shaders
		Shader vertShader = ShaderCross.Create(
			GraphicsDevice,
			TestUtils.GetHLSLPath("Skybox.vert"),
			"main",
			new ShaderCross.ShaderCreateInfo
			{
				Format = ShaderCross.ShaderFormat.HLSL,
				HLSLShaderModel = ShaderCross.HLSLShaderModel.Six,
				Stage = ShaderStage.Vertex,
				NumUniformBuffers = 1
			}
		);

		Shader fragShader = ShaderCross.Create(
			GraphicsDevice,
			TestUtils.GetHLSLPath("Skybox.frag"),
			"main",
			new ShaderCross.ShaderCreateInfo
			{
				Format = ShaderCross.ShaderFormat.HLSL,
				HLSLShaderModel = ShaderCross.HLSLShaderModel.Six,
				Stage = ShaderStage.Fragment,
				NumSamplers = 1
			}
		);

		// Create the graphics pipeline
		GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			vertShader,
			fragShader
		);
		pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionVertex>();

		pipeline = GraphicsPipeline.Create(GraphicsDevice, pipelineCreateInfo);

		// Create samplers
		sampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.PointClamp);

		// Create and populate the GPU resources
		var resourceUploader = new ResourceUploader(GraphicsDevice);

		vertexBuffer = resourceUploader.CreateBuffer(
			[
				new PositionVertex(new Vector3(-10, -10, -10)),
				new PositionVertex(new Vector3(10, -10, -10)),
				new PositionVertex(new Vector3(10, 10, -10)),
				new PositionVertex(new Vector3(-10, 10, -10)),

				new PositionVertex(new Vector3(-10, -10, 10)),
				new PositionVertex(new Vector3(10, -10, 10)),
				new PositionVertex(new Vector3(10, 10, 10)),
				new PositionVertex(new Vector3(-10, 10, 10)),

				new PositionVertex(new Vector3(-10, -10, -10)),
				new PositionVertex(new Vector3(-10, 10, -10)),
				new PositionVertex(new Vector3(-10, 10, 10)),
				new PositionVertex(new Vector3(-10, -10, 10)),

				new PositionVertex(new Vector3(10, -10, -10)),
				new PositionVertex(new Vector3(10, 10, -10)),
				new PositionVertex(new Vector3(10, 10, 10)),
				new PositionVertex(new Vector3(10, -10, 10)),

				new PositionVertex(new Vector3(-10, -10, -10)),
				new PositionVertex(new Vector3(-10, -10, 10)),
				new PositionVertex(new Vector3(10, -10, 10)),
				new PositionVertex(new Vector3(10, -10, -10)),

				new PositionVertex(new Vector3(-10, 10, -10)),
				new PositionVertex(new Vector3(-10, 10, 10)),
				new PositionVertex(new Vector3(10, 10, 10)),
				new PositionVertex(new Vector3(10, 10, -10))
			],
			BufferUsageFlags.Vertex
		);

		indexBuffer = resourceUploader.CreateBuffer<ushort>(
			[
				0,  1,  2,  0,  2,  3,
				6,  5,  4,  7,  6,  4,
				8,  9, 10,  8, 10, 11,
				14, 13, 12, 15, 14, 12,
				16, 17, 18, 16, 18, 19,
				22, 21, 20, 23, 22, 20
			],
			BufferUsageFlags.Index
		);

		resourceUploader.Upload();
		resourceUploader.Dispose();

		cubemap = Texture.CreateCube(
			GraphicsDevice,
			16,
			TextureFormat.R8G8B8A8Unorm,
			TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler
		);

		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();

		// Clear each slice of the cubemap to a different color
		for (uint i = 0; i < 6; i += 1)
		{
            var renderPass = cmdbuf.BeginRenderPass(new ColorTargetInfo
            {
                Texture = cubemap.Handle,
                LayerOrDepthPlane = i,
                LoadOp = LoadOp.Clear,
                ClearColor = colors[i],
                StoreOp = StoreOp.Store
            });
			cmdbuf.EndRenderPass(renderPass);
		}

		GraphicsDevice.Submit(cmdbuf);
	}

	public override void Update(System.TimeSpan delta)
	{
		if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Bottom))
		{
			camPos.Z *= -1;
		}
	}

	public override void Draw(double alpha)
	{
		Matrix4x4 proj = Matrix4x4.CreatePerspectiveFieldOfView(
			MathHelper.ToRadians(75f),
			(float) Window.Width / Window.Height,
			0.01f,
			100f
		);
		Matrix4x4 view = Matrix4x4.CreateLookAt(
			camPos,
			Vector3.Zero,
			Vector3.Up
		);
		TransformVertexUniform vertUniforms = new TransformVertexUniform(view * proj);

		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
		if (swapchainTexture != null)
		{
			var renderPass = cmdbuf.BeginRenderPass(
				new ColorTargetInfo(swapchainTexture, Color.Black)
			);
			renderPass.BindGraphicsPipeline(pipeline);
			renderPass.BindVertexBuffer(vertexBuffer);
			renderPass.BindIndexBuffer(indexBuffer, IndexElementSize.Sixteen);
			renderPass.BindFragmentSampler(new TextureSamplerBinding(cubemap, sampler));
			cmdbuf.PushVertexUniformData(vertUniforms);
			renderPass.DrawIndexedPrimitives(36, 1, 0, 0, 0);
			cmdbuf.EndRenderPass(renderPass);
		}
		GraphicsDevice.Submit(cmdbuf);
	}

    public override void Destroy()
    {

    }
}
