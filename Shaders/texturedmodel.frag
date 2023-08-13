#version 330 core
in vec2 TexCoord; // Received texture coordinate from vertex shader
in vec3 VertexColor; // Received vertex color from vertex shader

out vec4 FragColor; // Output color

uniform sampler2D ourTexture; // Texture to sample

void main()
{
    FragColor = texture(ourTexture, TexCoord) * vec4(VertexColor, 1.0); // Sample the texture and multiply by the vertex color
}
