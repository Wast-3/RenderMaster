#version 450 core
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
    float uMetallic; float uRoughness; float uAlphaCutoff; float uFlags;
};
