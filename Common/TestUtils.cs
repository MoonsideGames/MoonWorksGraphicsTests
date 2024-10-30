using MoonWorks;
using MoonWorks.Graphics;

namespace MoonWorksGraphicsTests;

public static class TestUtils
{
	public static GraphicsPipelineCreateInfo GetStandardGraphicsPipelineCreateInfo(
		TextureFormat swapchainFormat,
		Shader vertShader,
		Shader fragShader
	) {
		return new GraphicsPipelineCreateInfo
		{
			TargetInfo = new GraphicsPipelineTargetInfo
			{
				ColorTargetDescriptions = [
					new ColorTargetDescription
					{
						Format = swapchainFormat,
						BlendState = ColorTargetBlendState.Opaque
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
	}

	public static string GetShaderPath(string shaderName)
	{
		return SDL3.SDL.SDL_GetBasePath() + "Content/Shaders/Compiled/" + shaderName + ".spv";
	}

	public static string GetHLSLPath(string shaderName)
	{
		return SDL3.SDL.SDL_GetBasePath() + "Content/Shaders/HLSL/" + shaderName + ".hlsl";
	}

	public static string GetTexturePath(string textureName)
	{
		return SDL3.SDL.SDL_GetBasePath() + "Content/Textures/" + textureName;
	}

	public static string GetVideoPath(string videoName)
	{
		return SDL3.SDL.SDL_GetBasePath() + "Content/Videos/" + videoName;
	}

	public static string GetFontPath(string fontName)
	{
		return SDL3.SDL.SDL_GetBasePath() + "Content/Fonts/" + fontName;
	}

	public enum ButtonType
	{
		Left,	  // A/left arrow on keyboard, left d-pad on gamepad
		Bottom,	  // S/down arrow on keyboard, bottom d-pad on gamepad
		Right,	  // D/right arrow on keyboard, right d-pad on gamepad
		Previous, // Q on keyboard, left shoulder on gamepad
		Next      // E on keyboard, right shoulder on gamepad
	}

	public static bool CheckButtonPressed(MoonWorks.Input.Inputs inputs, ButtonType buttonType)
	{
		bool pressed = false;

		if (buttonType == ButtonType.Left)
		{
			pressed = (
				inputs.GetGamepad(0).DpadLeft.IsPressed ||
				inputs.Keyboard.IsPressed(MoonWorks.Input.KeyCode.Left) ||
				inputs.Keyboard.IsPressed(MoonWorks.Input.KeyCode.A)
			);
		}
		else if (buttonType == ButtonType.Bottom)
		{
			pressed = (
				inputs.GetGamepad(0).DpadDown.IsPressed ||
				inputs.Keyboard.IsPressed(MoonWorks.Input.KeyCode.Down) ||
				inputs.Keyboard.IsPressed(MoonWorks.Input.KeyCode.S)
			);
		}
		else if (buttonType == ButtonType.Right)
		{
			pressed = (
				inputs.GetGamepad(0).DpadRight.IsPressed ||
				inputs.Keyboard.IsPressed(MoonWorks.Input.KeyCode.Right) ||
				inputs.Keyboard.IsPressed(MoonWorks.Input.KeyCode.D)
			);
		}
		else if (buttonType == ButtonType.Previous)
		{
			pressed = (
				inputs.GetGamepad(0).LeftShoulder.IsPressed ||
				inputs.Keyboard.IsPressed(MoonWorks.Input.KeyCode.Q)
			);
		}
		else if (buttonType == ButtonType.Next)
		{
			pressed = (
				inputs.GetGamepad(0).RightShoulder.IsPressed ||
				inputs.Keyboard.IsPressed(MoonWorks.Input.KeyCode.E)
			);
		}

		return pressed;
	}

	public static bool CheckButtonDown(MoonWorks.Input.Inputs inputs, ButtonType buttonType)
	{
		bool down = false;

		if (buttonType == ButtonType.Left)
		{
			down = (
				inputs.GetGamepad(0).DpadLeft.IsDown ||
				inputs.Keyboard.IsDown(MoonWorks.Input.KeyCode.Left) ||
				inputs.Keyboard.IsDown(MoonWorks.Input.KeyCode.A)
			);
		}
		else if (buttonType == ButtonType.Bottom)
		{
			down = (
				inputs.GetGamepad(0).DpadDown.IsDown ||
				inputs.Keyboard.IsDown(MoonWorks.Input.KeyCode.Down) ||
				inputs.Keyboard.IsDown(MoonWorks.Input.KeyCode.S)
			);
		}
		else if (buttonType == ButtonType.Right)
		{
			down = (
				inputs.GetGamepad(0).DpadRight.IsDown ||
				inputs.Keyboard.IsDown(MoonWorks.Input.KeyCode.Right) ||
				inputs.Keyboard.IsDown(MoonWorks.Input.KeyCode.D)
			);
		}
		else if (buttonType == ButtonType.Previous)
		{
			down = (
				inputs.GetGamepad(0).LeftShoulder.IsDown ||
				inputs.Keyboard.IsDown(MoonWorks.Input.KeyCode.Q)
			);
		}
		else if (buttonType == ButtonType.Next)
		{
			down = (
				inputs.GetGamepad(0).RightShoulder.IsDown ||
				inputs.Keyboard.IsDown(MoonWorks.Input.KeyCode.E)
			);
		}

		return down;
	}
}
