using System;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;

namespace MoonWorksGraphicsTests;

public abstract class Example
{
	protected Window Window;
	public GraphicsDevice GraphicsDevice;
	public Inputs Inputs;

	public abstract void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs);
	public abstract void Update(TimeSpan delta);
	public abstract void Draw(double alpha);
	public abstract void Destroy();
}
