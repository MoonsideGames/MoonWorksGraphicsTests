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
