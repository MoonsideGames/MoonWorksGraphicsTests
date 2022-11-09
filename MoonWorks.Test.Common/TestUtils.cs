using MoonWorks.Graphics;

namespace MoonWorks.Test
{
	public static class TestUtils
	{
		public static WindowCreateInfo GetStandardWindowCreateInfo()
		{
			return new WindowCreateInfo(
				"Main Window",
				640,
				480,
				ScreenMode.Windowed,
				PresentMode.FIFO
			);
		}

		public static FrameLimiterSettings GetStandardFrameLimiterSettings()
		{
			return new FrameLimiterSettings(
				FrameLimiterMode.Capped,
				60
			);
		}

		public static GraphicsPipelineCreateInfo GetStandardGraphicsPipelineCreateInfo(
			ShaderModule vertShaderModule,
			ShaderModule fragShaderModule
		) {
			return new GraphicsPipelineCreateInfo
			{
				AttachmentInfo = new GraphicsPipelineAttachmentInfo(
					new ColorAttachmentDescription(
						TextureFormat.R8G8B8A8,
						ColorAttachmentBlendState.Opaque
					)
				),
				DepthStencilState = DepthStencilState.Disable,
				MultisampleState = MultisampleState.None,
				PrimitiveType = PrimitiveType.TriangleList,
				RasterizerState = RasterizerState.CW_CullNone,
				VertexInputState = VertexInputState.Empty,
				VertexShaderInfo = GraphicsShaderInfo.Create(vertShaderModule, "main", 0),
				FragmentShaderInfo = GraphicsShaderInfo.Create(fragShaderModule, "main", 0)
			};
		}

		public static string GetShaderPath(string shaderName)
		{
			return SDL2.SDL.SDL_GetBasePath() + "Content/Shaders/Compiled/" + shaderName;
		}
	}
}
