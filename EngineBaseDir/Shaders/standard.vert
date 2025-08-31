#version 450 core

layout(location=0) in vec3 aPos;
layout(location=1) in vec3 aNormal;
layout(location=2) in vec4 aTangent;
layout(location=3) in vec2 aUV;

#include "common_blocks.glsl"

out vec2 vUV;

void main()
{
    vUV = aUV;
    // CPU provided uViewProj and uWorld already transposed for column-major:
    gl_Position = uViewProj * uWorld * vec4(aPos, 1.0);
}

