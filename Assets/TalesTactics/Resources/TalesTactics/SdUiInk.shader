Shader "TalesTactics/SD UI Ink"
{
    Properties
    {
        [PerRendererData] _MainTex ("Illustrated art", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            half4 _Color, _TextureSampleAdd;
            float4 _ClipRect;
            struct Attributes { float4 positionOS:POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; float2 local:TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.uv=input.uv;output.color=input.color*_Color;output.local=input.positionOS.xy;return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half4 art=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv)+_TextureSampleAdd;
                float2 stepUV=_MainTex_TexelSize.xy*1.2;
                half neighbour=min(min(SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv+float2(stepUV.x,0)).a,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv-float2(stepUV.x,0)).a),min(SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv+float2(0,stepUV.y)).a,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv-float2(0,stepUV.y)).a));
                // Soft cel grouping keeps fine illustration detail; ink only darkens inside the existing silhouette.
                half3 grouped=floor(saturate(art.rgb)*8+.5)/8;
                art.rgb=lerp(art.rgb,grouped,.20);
                half edge=saturate((art.a-neighbour)*1.35);
                art.rgb=lerp(art.rgb,half3(.025,.045,.07),edge*.58);
                art*=input.color;
                #ifdef UNITY_UI_CLIP_RECT
                half2 inside=step(_ClipRect.xy,input.local)*step(input.local,_ClipRect.zw);art.a*=inside.x*inside.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(art.a-.001);
                #endif
                return art;
            }
            ENDHLSL
        }
    }
}
