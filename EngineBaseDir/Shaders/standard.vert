layout(location=0) in vec3 inPos;
layout(location=1) in vec3 inNormal;
layout(location=2) in vec4 inTangent; // xyz + handedness
layout(location=3) in vec2 inUV;

out VS_OUT { vec3 P; vec3 N; vec2 UV; } vs;
void main()
{
    vec4 wp = uWorld * vec4(inPos, 1.0);
    vs.P  = wp.xyz;
    vs.N  = normalize((uNormalWorld * vec4(inNormal, 0.0)).xyz);
    vs.UV = inUV;
    gl_Position = uViewProj * wp;
}
