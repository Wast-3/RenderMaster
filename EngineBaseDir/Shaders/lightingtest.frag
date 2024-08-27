#version 330 core
in vec3 VertexColor; // Received vertex color from vertex shader (not used in this code)
in vec3 Normal;      // The interpolated normals for the fragment
in vec3 FragPos;     // The interpolated fragment position
in vec2 TexCoord;

uniform vec3 viewPos;    // The position of the viewer or camera

out vec4 FragColor; // Output color

struct Light {
    vec3 position;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};
uniform Light light;

struct Material { 
    sampler2D diffuse;  // Diffuse texture map
    vec3 specular;      // Specular reflection color
    float shininess;    // Shininess factor for specular reflection
}; 

uniform Material material;

void main()
{
    // AMBIENT LIGHTING
    vec3 ambient = light.ambient * vec3(texture(material.diffuse, TexCoord)); // Product of the light's ambient color and the texture's color at the fragment's texture coordinates

    // DIFFUSE LIGHTING
    vec3 norm = normalize(Normal);
    vec3 lightDir = normalize(light.position - FragPos);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = light.diffuse * diff * vec3(texture(material.diffuse, TexCoord));

    // SPECULAR LIGHTING
    vec3 viewDir = normalize(viewPos - FragPos);
    vec3 reflectDir = reflect(-lightDir, norm);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    vec3 specular = light.specular * (material.specular * spec);

    // FINAL COLOR COMPUTATION
    vec3 result = (ambient + diffuse + specular); // Sum of the ambient, diffuse, and specular components
    FragColor = vec4(result, 1.0); // Final color with alpha set to 1.0
}
