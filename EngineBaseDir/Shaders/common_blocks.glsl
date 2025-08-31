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

uniform sampler2D uBaseColorTex;
uniform sampler2D uNormalTex;
uniform sampler2D uMetalRoughTex;

