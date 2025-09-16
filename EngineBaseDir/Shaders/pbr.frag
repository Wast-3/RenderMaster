// H:\Google Drive Sync\dev\clones\RenderMaster\EngineBaseDir\Shaders\pbr.frag

// Input from the vertex shader
in VS_OUT 
{ 
    vec3 P;      // Fragment position in World Space
    vec3 N;      // Fragment normal in World Space
    vec2 UV;     // Texture coordinates
} fs;

// Output color
layout(location=0) out vec4 outColor;

// Samplers for PBR textures
uniform sampler2D uBaseColorTex;
uniform sampler2D uNormalTex;
uniform sampler2D uMetalRoughTex;

// --- PBR Constants & Functions ---
const float PI = 3.14159265359;

// Calculates the distribution of microfacets using the Trowbridge-Reitz GGX formula.
float DistributionGGX(vec3 N, vec3 H, float roughness)
{
    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH * NdotH;

    float nom   = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;

    return nom / denom;
}

// Calculates how much the microfacets shadow and mask each other.
float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r * r) / 8.0;
    float nom = NdotV;
    float denom = NdotV * (1.0 - k) + k;
    return nom / denom;
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2 = GeometrySchlickGGX(NdotV, roughness);
    float ggx1 = GeometrySchlickGGX(NdotL, roughness);
    return ggx1 * ggx2;
}

// Describes the reflectivity of a surface at different angles using the Fresnel-Schlick approximation.
vec3 fresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}
// --- End PBR Functions ---

vec3 EvaluateLight(
    vec3 N,
    vec3 V,
    vec3 L,
    vec3 radiance,
    vec3 F0,
    vec3 albedo,
    float metallic,
    float roughness)
{
    vec3 H = normalize(V + L);
    float NDF = DistributionGGX(N, H, roughness);
    float G = GeometrySmith(N, V, L, roughness);
    vec3 F = fresnelSchlick(max(dot(H, V), 0.0), F0);

    vec3 numerator = NDF * G * F;
    float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.0001;
    vec3 specular = numerator / denominator;

    vec3 kS = F;
    vec3 kD = (vec3(1.0) - kS) * (1.0 - metallic);
    float NdotL = max(dot(N, L), 0.0);

    return (kD * albedo / PI + specular) * radiance * NdotL;
}

void main()
{
    // --- Material Properties ---
    // Sample textures to get material properties for this fragment
    vec4 albedo = texture(uBaseColorTex, fs.UV) * uBaseColorFactor;
    float metallic = texture(uMetalRoughTex, fs.UV).b * uMetallic;
    float roughness = texture(uMetalRoughTex, fs.UV).g * uRoughness;
    metallic = clamp(metallic, 0.0, 1.0);
    roughness = clamp(roughness, 0.04, 1.0);

    // --- Vectors ---
    vec3 N = normalize(fs.N);
    vec3 V = normalize(uCameraWS - fs.P);

    // F0 is the base reflectivity for a surface at a 0-degree angle.
    // For metals, F0 is the albedo color. For dielectrics (non-metals), it's a constant.
    vec3 F0 = vec3(0.04);
    F0 = mix(F0, albedo.rgb, metallic);

    vec3 Lo = vec3(0.0); // Outgoing radiance (the final color)
    // Camera-attached helper light (legacy behavior)
    vec3 cameraToLight = uCameraWS - fs.P;
    float cameraDist = length(cameraToLight);
    if (cameraDist > 0.0001)
    {
        vec3 lightColor = 150.0 * (0.5 + 0.5 * cos(2.0 * PI * uTime + vec3(0.0, 2.0*PI/3.0, 4.0*PI/3.0)));
        vec3 L = cameraToLight / cameraDist;
        float attenuation = 1.0 / max(cameraDist * cameraDist, 0.0001);
        vec3 radiance = lightColor * attenuation;
        Lo += EvaluateLight(N, V, L, radiance, F0, albedo.rgb, metallic, roughness);
    }

    int pointCount = int(uLightCounts.x + 0.5);
    for (int i = 0; i < pointCount; ++i)
    {
        GPUPointLight light = uPointLights[i];
        vec3 toLight = light.positionRange.xyz - fs.P;
        float dist = length(toLight);
        if (dist <= 0.0001) continue;

        vec3 L = toLight / dist;
        float attenuation = 1.0 / max(dist * dist, 0.0001);
        float range = light.positionRange.w;
        if (range > 0.0)
        {
            float rangeFactor = clamp(1.0 - dist / range, 0.0, 1.0);
            attenuation *= rangeFactor * rangeFactor;
        }

        vec3 radiance = light.colorIntensity.rgb * attenuation;
        Lo += EvaluateLight(N, V, L, radiance, F0, albedo.rgb, metallic, roughness);
    }

    int spotCount = int(uLightCounts.y + 0.5);
    for (int i = 0; i < spotCount; ++i)
    {
        GPUSpotLight light = uSpotLights[i];
        vec3 toLight = light.positionRange.xyz - fs.P;
        float dist = length(toLight);
        if (dist <= 0.0001) continue;

        vec3 L = toLight / dist;
        float attenuation = 1.0 / max(dist * dist, 0.0001);
        float range = light.positionRange.w;
        if (range > 0.0)
        {
            float rangeFactor = clamp(1.0 - dist / range, 0.0, 1.0);
            attenuation *= rangeFactor * rangeFactor;
        }

        vec3 dir = light.directionInner.xyz;
        float dirLen = length(dir);
        if (dirLen <= 0.0001) continue;
        dir /= dirLen;

        float cosTheta = dot(dir, -L);
        float inner = light.directionInner.w;
        float outer = light.colorOuter.w;
        float spotFactor = smoothstep(outer, inner, cosTheta);
        attenuation *= spotFactor;

        vec3 radiance = light.colorOuter.rgb * attenuation;
        Lo += EvaluateLight(N, V, L, radiance, F0, albedo.rgb, metallic, roughness);
    }

    // --- Final Color ---
    // Add a simple ambient term and apply tone mapping
    vec3 ambient = vec3(0.03) * albedo.rgb;
    vec3 color = ambient + Lo;
    color = color / (color + vec3(1.0)); // Basic Reinhard tone mapping
    outColor = vec4(color, albedo.a);
}