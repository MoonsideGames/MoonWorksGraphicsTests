﻿using MoonWorks;
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

	public static string GetTexturePath(string textureName)
	{
		return SDL3.SDL.SDL_GetBasePath() + "Content/Textures/" + textureName;
	}

	public static string GetVideoPath(string videoName)
	{
		return SDL3.SDL.SDL_GetBasePath() + "Content/Videos/" + videoName;
	}

	public enum ButtonType
	{
		Left,	// A/left arrow on keyboard, left face button on gamepad
		Bottom,	// S/down arrow on keyboard, bottom face button on gamepad
		Right	// D/right arrow on keyboard, right face button on gamepad
	}

	public static bool CheckButtonPressed(MoonWorks.Input.Inputs inputs, ButtonType buttonType)
	{
		bool pressed = false;

		if (buttonType == ButtonType.Left)
		{
			pressed = (
				(inputs.GamepadExists(0) && inputs.GetGamepad(0).DpadLeft.IsPressed) ||
				inputs.Keyboard.IsPressed(MoonWorks.Input.KeyCode.Left) ||
				inputs.Keyboard.IsPressed(MoonWorks.Input.KeyCode.A)
			);
		}
		else if (buttonType == ButtonType.Bottom)
		{
			pressed = (
				(inputs.GamepadExists(0) && inputs.GetGamepad(0).DpadDown.IsPressed) ||
				inputs.Keyboard.IsPressed(MoonWorks.Input.KeyCode.Down) ||
				inputs.Keyboard.IsPressed(MoonWorks.Input.KeyCode.S)
			);
		}
		else if (buttonType == ButtonType.Right)
		{
			pressed = (
				(inputs.GamepadExists(0) && inputs.GetGamepad(0).DpadRight.IsPressed) ||
				inputs.Keyboard.IsPressed(MoonWorks.Input.KeyCode.Right) ||
				inputs.Keyboard.IsPressed(MoonWorks.Input.KeyCode.D)
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
				(inputs.GamepadExists(0) && inputs.GetGamepad(0).DpadLeft.IsDown) ||
				inputs.Keyboard.IsDown(MoonWorks.Input.KeyCode.Left) ||
				inputs.Keyboard.IsDown(MoonWorks.Input.KeyCode.A)
			);
		}
		else if (buttonType == ButtonType.Bottom)
		{
			down = (
				(inputs.GamepadExists(0) && inputs.GetGamepad(0).DpadDown.IsDown) ||
				inputs.Keyboard.IsDown(MoonWorks.Input.KeyCode.Down) ||
				inputs.Keyboard.IsDown(MoonWorks.Input.KeyCode.S)
			);
		}
		else if (buttonType == ButtonType.Right)
		{
			down = (
				(inputs.GamepadExists(0) && inputs.GetGamepad(0).DpadRight.IsDown) ||
				inputs.Keyboard.IsDown(MoonWorks.Input.KeyCode.Right) ||
				inputs.Keyboard.IsDown(MoonWorks.Input.KeyCode.D)
			);
		}

		return down;
	}
}
