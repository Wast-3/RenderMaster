#version 330 core
in vec3 VertexColor; // Received vertex color from vertex shader
in vec3 Normal;      // The interpolated normals for the fragment
in vec3 FragPos;     // The interpolated fragment position

uniform vec3 viewPos;    // The position of the viewer or camera

out vec4 FragColor; // Output color

// a note about structs in GLSL:

// We can set the material of the object in the application by setting the appropriate uniforms. 
// A struct in GLSL however is not special in any regard when setting uniforms; 
// a struct only really acts as a namespace of uniform variables. 
// If we want to fill the struct we will have to set the individual uniforms, but prefixed with the struct's name


struct Light {
    vec3 position;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};
uniform Light light;  

struct Material { 
//Basic material system
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
    float shininess;
}; 

uniform Material material;

void main()
{
    // AMBIENT LIGHTING
    // ----------------
    float ambientStrength = 0.2; // A scaling factor for the ambient component
    vec3 ambient = light.ambient * material.ambient; // Ambient light is constant and is scaled by both ambientStrength and lightColor

    // DIFFUSE LIGHTING
    // ----------------
    vec3 norm = normalize(Normal); // Normalizing the interpolated normal vector
    vec3 lightDir = normalize(light.position - FragPos); // Direction from the fragment to the light source
    float diff = max(dot(norm, lightDir), 0.0); // The cosine of the angle between the normal and the light direction; zero if the angle is greater than 90°
    vec3 diffuse = light.diffuse * (diff * material.diffuse); // Scaling the light color by the cosine of the angle

    // SPECULAR LIGHTING
    // -----------------
    vec3 viewDir = normalize(viewPos - FragPos); // Direction from the fragment to the viewer
    vec3 reflectDir = reflect(-lightDir, norm); // Reflection direction of the light, calculated using the incoming light direction and the normal
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess); // Raised to the power of shininess (here 32); the cosine of the angle between the reflected light direction and the view direction
    vec3 specular = light.specular * (material.specular * spec); // Scales the light color by both the specular strength and spec
	
    // FINAL COLOR COMPUTATION
    // -----------------------
    vec3 result = (ambient + diffuse + specular); // Summing ambient, diffuse, and specular components, then multiplying by object color
    FragColor = vec4(result, 1.0); // Final color with alpha set to 1.0
}
