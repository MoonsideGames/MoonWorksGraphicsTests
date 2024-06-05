using System;
using MoonWorks;
using MoonWorks.Graphics;

namespace MoonWorksGraphicsTests;

public abstract class Example
{
	protected Window Window;
	protected GraphicsDevice GraphicsDevice;

	public abstract void Init(Window window, GraphicsDevice graphicsDevice);
	public abstract void Update(TimeSpan delta);
	public abstract void Draw(double alpha);
	public abstract void Destroy();
}
