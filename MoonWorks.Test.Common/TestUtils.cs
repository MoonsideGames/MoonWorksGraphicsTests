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
			TextureFormat swapchainFormat,
			ShaderModule vertShaderModule,
			ShaderModule fragShaderModule
		) {
			return new GraphicsPipelineCreateInfo
			{
				AttachmentInfo = new GraphicsPipelineAttachmentInfo(
					new ColorAttachmentDescription(
						swapchainFormat,
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
			return SDL2.SDL.SDL_GetBasePath() + "Content/Shaders/Compiled/" + shaderName + ".refresh";
		}

		public static string GetTexturePath(string textureName)
		{
			return SDL2.SDL.SDL_GetBasePath() + "Content/Textures/" + textureName;
		}

        public enum ButtonType
        {
            Left,	// A/left arrow on keyboard, left face button on gamepad
            Bottom,	// S/down arrow on keyboard, bottom face button on gamepad
            Right	// D/right arrow on keyboard, right face button on gamepad
        }

        public static bool CheckButtonPressed(Input.Inputs inputs, ButtonType buttonType)
        {
            bool pressed = false;

            if (buttonType == ButtonType.Left)
            {
                pressed = (
                    (inputs.GamepadExists(0) && inputs.GetGamepad(0).DpadLeft.IsPressed) ||
                    inputs.Keyboard.IsPressed(Input.KeyCode.A) ||
                    inputs.Keyboard.IsPressed(Input.KeyCode.Left)
                );
            }
            else if (buttonType == ButtonType.Bottom)
            {
                pressed = (
                    (inputs.GamepadExists(0) && inputs.GetGamepad(0).DpadDown.IsPressed) ||
                    inputs.Keyboard.IsPressed(Input.KeyCode.S) ||
                    inputs.Keyboard.IsPressed(Input.KeyCode.Down)
                );
            }
            else if (buttonType == ButtonType.Right)
            {
                pressed = (
                    (inputs.GamepadExists(0) && inputs.GetGamepad(0).DpadRight.IsPressed) ||
                    inputs.Keyboard.IsPressed(Input.KeyCode.D) ||
                    inputs.Keyboard.IsPressed(Input.KeyCode.Right)
                );
            }

            return pressed;
        }

        public static bool CheckButtonDown(Input.Inputs inputs, ButtonType buttonType)
        {
            bool down = false;

            if (buttonType == ButtonType.Left)
            {
                down = (
                    (inputs.GamepadExists(0) && inputs.GetGamepad(0).DpadLeft.IsDown) ||
                    inputs.Keyboard.IsDown(Input.KeyCode.A) ||
                    inputs.Keyboard.IsDown(Input.KeyCode.Left)
                );
            }
            else if (buttonType == ButtonType.Bottom)
            {
                down = (
                    (inputs.GamepadExists(0) && inputs.GetGamepad(0).DpadDown.IsDown) ||
                    inputs.Keyboard.IsDown(Input.KeyCode.S) ||
                    inputs.Keyboard.IsDown(Input.KeyCode.Down)
                );
            }
            else if (buttonType == ButtonType.Right)
            {
                down = (
                    (inputs.GamepadExists(0) && inputs.GetGamepad(0).DpadRight.IsDown) ||
                    inputs.Keyboard.IsDown(Input.KeyCode.D) ||
                    inputs.Keyboard.IsDown(Input.KeyCode.Right)
                );
            }

            return down;
        }
    }
}
