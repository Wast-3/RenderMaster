in VS_OUT { vec3 P; vec3 N; vec2 UV; } fs;
layout(location=0) out vec4 outColor;

uniform sampler2D uBaseColorTex;

void main()
{
#ifdef VAR_ALPHABLEND
    if (texture(uBaseColorTex, fs.UV).a < uAlphaCutoff) discard;
#endif
    vec4 bc = texture(uBaseColorTex, fs.UV) * uBaseColorFactor;
    outColor = bc;
}
