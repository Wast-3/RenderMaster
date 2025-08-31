layout(std140, row_major, binding = 0) uniform FrameBlock {
    mat4 uViewProj;
    vec3 uCameraWS; float uTime;
};
layout(std140, row_major, binding = 1) uniform ObjectBlock {
    mat4 uWorld;
    mat4 uNormalWorld;
};
layout(std140, row_major, binding = 2) uniform MaterialBlock {
    vec4  uBaseColorFactor;
    float uMetallic;
    float uRoughness;
    float uAlphaCutoff;
    float uFlags;
};

layout(location=0) in vec3 inPos;
layout(location=1) in vec3 inNormal;
layout(location=2) in vec4 inTangent; // xyz + handedness
layout(location=3) in vec2 inUV;
