#ifndef TALES_ENVIRONMENT_LIGHT
#define TALES_ENVIRONMENT_LIGHT
float TalesCanopy(float2 p)
{
    float d=sin(p.x*1.37+sin(p.y*1.8))*sin(p.y*1.63+cos(p.x*2.2));
    return smoothstep(-.4,.6,d);
}
half3 TalesPalette(half3 color,half saturation,half contrast)
{
    half l=dot(color,half3(.2126,.7152,.0722));
    return max(0,lerp(l.xxx,color,saturation)*contrast+(.5-.5*contrast));
}
#endif
