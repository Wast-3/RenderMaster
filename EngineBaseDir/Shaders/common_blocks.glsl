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

const int MAX_LIGHTS = 16;
struct Light {
    vec4 positionType;
    vec4 directionRange;
    vec4 colorIntensity;
    vec4 spotAngles;
};
layout(std140, binding = 3) uniform LightsBlock {
    int uLightCount;
    vec3 _padLights;
    Light uLights[MAX_LIGHTS];
};

