#version 330 core
layout (location = 0) in vec3 aPosition; // Position from vertex attributes
layout (location = 1) in vec3 aColor; // Color from vertex attributes
layout (location = 2) in vec2 aTexCoord; // Texture Coordinate from vertex attributes

out vec2 TexCoord; // Output texture coordinate to the fragment shader
out vec3 VertexColor; // Output vertex color to the fragment shader

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    gl_Position = projection * view * model * vec4(aPosition, 1.0); // Transform the position into clip space
    TexCoord = aTexCoord; // Pass the texture coordinate to the fragment shader
    VertexColor = aColor; // Pass the vertex color to the fragment shader
}
