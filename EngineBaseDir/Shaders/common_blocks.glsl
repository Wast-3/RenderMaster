layout(std140, binding = 0) uniform FrameBlock {
    mat4 uViewProj;
    vec3 uCameraWS; float uTime;
};
layout(std140, binding = 1) uniform ObjectBlock {
    mat4 uWorld;
    mat4 uNormalWorld;
};
layout(std140, binding = 2) uniform MaterialBlock {
    vec4  uBaseColorFactor;
    float uMetallic;
    float uRoughness;
    float uAlphaCutoff;
    float uFlags;
};

struct GPUPointLight {
    vec4 positionRange;
    vec4 colorIntensity;
};

struct GPUSpotLight {
    vec4 positionRange;
    vec4 directionInner;
    vec4 colorOuter;
};

layout(std140, binding = 3) uniform LightBlock {
    vec4 uLightCounts; // x = point, y = spot
    GPUPointLight uPointLights[MAX_POINT_LIGHTS];
    GPUSpotLight uSpotLights[MAX_SPOT_LIGHTS];
};

