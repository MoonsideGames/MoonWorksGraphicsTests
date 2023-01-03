Tests include:

**ClearScreen**

Clears the screen to CornflowerBlue. Mostly useful as a smoke test to make sure basic device init and render passes are working as intended.

**ClearScreen_MultiWindow**

Similar to above, but with two windows. Useful for testing window claim/unclaim logic and presenting to multiple swapchains.

**BasicTriangle**

Sets up a graphics pipeline and draws a triangle without vertex buffers. (The vertices are manually positioned in the vertex shader.) Also tests some basic rasterizer state with custom viewports, scissor rects, and fill/wireframe modes.

**TriangleVertexBuffer**

Similar to above, but using a MoonWorks vertex buffer and custom vertex structs.

**TexturedQuad**

Draws a textured quad to the screen. Tests texture binding, index buffers, and sampler states.

**AnimatedTexturedQuad**

Similar to above, but with rotating and color-blending animations. Tests vertex and fragment uniforms.

**MSAA**

Draws a basic triangle with varying MSAA sample counts.

**CullMode**

Draws several triangles with various culling modes and winding orders.

**GetBufferData**

Sets buffer data, gets the data back from the GPU, and prints the results to the console.

**Cube**

Renders a cubemap skybox and a spinning cube. Tests depth textures, sampling from depth textures, depth-only render passes, cube textures, and 32-bit index buffers.

**BasicCompute**

Uses compute pipelines to (1) fill a texture with yellow pixels then display it to the screen and (2) calculate the squares of the first 64 integers. Tests compute pipeline creation, compute dispatching, compute textures, and compute buffers.

**ComputeUniforms**

Uses a compute pipeline to fill a texture with a color gradient. Tests compute uniforms.

**DrawIndirect**

Draws two triangles via indirect commands. Tests DrawPrimitivesIndirect.
