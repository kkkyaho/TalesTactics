#ifndef TALES_CHARACTER_IDLE
#define TALES_CHARACTER_IDLE
float4 _IdleMotion,_IdleFace,_IdleEyeA,_IdleEyeB,_IdleUvX,_IdleUvY,_IdleShape,_IdleBounds;
float2 IdleUvOffset(float2 localOffset){return _IdleUvX.xy*localOffset.x+_IdleUvY.xy*localOffset.y;}
float2 IdleSourcePosition(float2 position)
{
    float h=saturate(position.y/max(.1,_IdleShape.x));
    // The feet and the lower boot area remain exactly at the authored foot pivot.
    float upper=smoothstep(.06,.65,h);
    float breath=smoothstep(.15,.58,h)*(1-smoothstep(.7,1,h)*.65);
    float arm=smoothstep(.15,.34,h)*(1-smoothstep(.67,.82,h))*smoothstep(.12,.30,abs(position.x-_IdleShape.y));
    float2 shift=float2(_IdleMotion.x*upper+_IdleMotion.z*sign(position.x-_IdleShape.y)*arm,
        _IdleMotion.y*breath+_IdleMotion.z*.3*arm);
    return position-shift*_IdleMotion.w;
}
fixed4 IdleEye(sampler2D art,float2 uv,float2 localPosition,float4 eye,fixed4 original)
{
    fixed4 result=original;
    if(eye.z>0&&eye.w>0&&_IdleMotion.w>0)
    {
        // Keep absent-eye radii safe even if the shader compiler flattens this branch.
        float2 q=(localPosition-eye.xy)/max(eye.zw,float2(.00001,.00001));
        float mask=1-smoothstep(.62,1.05,length(q));
        if(mask>0)
        {
            // Restrained gaze and eye tension sample only the current painted eye. We never
            // reconstruct skin, draw new lids or replace the artist's glasses/hair contours.
            float openness=1-saturate(_IdleFace.x)*.16-saturate(_IdleFace.w)*.12;
            float2 mapped=float2(q.x-_IdleFace.y,q.y/max(.70,openness)-_IdleFace.z);
            fixed4 changed=tex2D(art,uv+IdleUvOffset((mapped-q)*eye.zw*mask));
            result=fixed4(lerp(original.rgb,changed.rgb,mask),original.a);
        }
    }
    return result;
}
fixed4 CharacterIdlePixel(sampler2D art,float2 uv,float2 position)
{
    // Explicitly initialize the return value on every DX11 branch, including opt-out.
    fixed4 result=fixed4(0,0,0,0);
    if(_IdleMotion.w>0)
    {
        float2 sourcePosition=IdleSourcePosition(position);
        // Never sample a neighbouring packed frame when a moving edge reaches this rect.
        if(all(sourcePosition>=_IdleBounds.xy)&&all(sourcePosition<=_IdleBounds.zw))
        {
            float2 sourceUv=uv+IdleUvOffset(sourcePosition-position);
            result=tex2D(art,sourceUv);
            result=IdleEye(art,sourceUv,sourcePosition,_IdleEyeA,result);
            result=IdleEye(art,sourceUv,sourcePosition,_IdleEyeB,result);
        }
    }
    else result=tex2D(art,uv);
    return result;
}
#endif
