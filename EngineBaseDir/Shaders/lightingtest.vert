#version 330 core
layout (location = 0) in vec3 aPosition; // Position from vertex attributes
layout (location = 1) in vec3 aColor; // Color from vertex attributes
layout (location = 2) in vec3 aNormal; // Texture Coordinate from vertex attributes

out vec3 VertexColor; // Output vertex color to the fragment shader
out vec3 Normal;
out vec3 FragPos;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;


void main()
{
    gl_Position = projection * view * model * vec4(aPosition, 1.0); // Transform the position into clip space
    VertexColor = aColor; // Pass the vertex color to the fragment shader
    Normal = mat3(transpose(inverse(model))) * aNormal; // Transform the normal's orientation into view space by multiplying the normal by the inverse transpose of the model matrix
    FragPos = vec3(model * vec4(aPosition, 1.0));
}
