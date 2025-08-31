out VS_OUT { vec3 P; vec3 N; vec2 UV; } vs;
void main()
{
    vec4 wp = vec4(inPos, 1.0) * uWorld;
    vs.P  = wp.xyz;
    vs.N  = normalize((vec4(inNormal, 0.0) * uNormalWorld).xyz);
    vs.UV = inUV;
    gl_Position = wp * uViewProj;
}
