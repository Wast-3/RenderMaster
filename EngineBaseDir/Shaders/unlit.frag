#version 450 core
in vec2 vUV;
out vec4 oColor;

#include "common_blocks.glsl"

void main()
{
    vec4 texColor = texture(uBaseColorTex, vUV);
    vec4 base = uBaseColorFactor;
    // If no texture is bound, texColor will be (0,0,0,1) or undefined; bias toward base factor:
    vec4 color = base;
    // If you want “use texture when present”, uncomment this line:
    // color = base * (texColor.a > 0.0 ? texColor : vec4(1.0));

    // simple alpha (no cutoff for now)
    oColor = color;
}

