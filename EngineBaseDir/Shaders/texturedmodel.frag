#version 330 core
in vec2 TexCoord; // Received texture coordinate from vertex shader
in vec3 VertexColor; // Received vertex color from vertex shader

out vec4 FragColor; // Output color

uniform sampler2D ourTexture; // Texture to sample

void main()
{
    //had to flip because blender fucking sucks????
    FragColor = texture(ourTexture, vec2(TexCoord.x, 1.0 - TexCoord.y)); // Sample the texture, flipping the y-coordinate
}
