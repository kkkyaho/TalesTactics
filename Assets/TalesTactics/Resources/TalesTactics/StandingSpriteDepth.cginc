#ifndef TALES_STANDING_SPRITE_DEPTH
#define TALES_STANDING_SPRITE_DEPTH
float _StandingDepth;
float4 StandingSpriteClip(float4 vertex)
{
    float4 clip=UnityObjectToClipPos(vertex);
    float3 world=mul(unity_ObjectToWorld,vertex).xyz;
    float3 pivot=mul(unity_ObjectToWorld,float4(0,0,0,1)).xyz;
    float3 view=mul(UNITY_MATRIX_V,float4(world,1)).xyz;
    // A camera-facing quad leans into scenery as it rises. Project the same
    // screen height onto a vertical body at its foot, changing depth only.
    float height=dot(UNITY_MATRIX_V[1].xyz,world-pivot)/max(.25,UNITY_MATRIX_V[1].y);
    view.z+=UNITY_MATRIX_V[2].y*height;
    float4 standing=mul(UNITY_MATRIX_P,float4(view,1));
    clip.z=lerp(clip.z,standing.z,saturate(_StandingDepth)*unity_OrthoParams.w);
    return clip;
}
#endif