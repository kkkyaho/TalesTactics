Shader "TalesTactics/Impact"
{
    Properties { _MainTex("Effect atlas",2D)="white"{} _UseTexture("Use texture",Float)=0 _SoftParticles("Depth feather",Float)=0 _CellFeather("Atlas cell feather",Float)=0 _AtlasGrid("Atlas columns",Float)=4 _CelStyle("Cel ink and shading",Float)=1 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_TexelSize;
            float _UseTexture, _SoftParticles, _CellFeather, _AtlasGrid, _CelStyle;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float3 next:TEXCOORD1; };
            struct Varyings { float4 positionCS:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float3 next:TEXCOORD1; float3 world:TEXCOORD2; };
            Varyings vert(Attributes v){Varyings o;o.world=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.color=v.color;o.uv=v.uv;o.next=v.next;return o;}
            half4 CelSample(float2 uv)
            {
                half4 tex=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv);
                if(_CelStyle<.5)return tex;
                float2 cell=floor(uv*_AtlasGrid)/_AtlasGrid;
                float2 lo=cell+_MainTex_TexelSize.xy,hi=cell+1/_AtlasGrid-_MainTex_TexelSize.xy;
                // Screen-space width keeps the ink readable at tactical zoom, while
                // clamping every tap prevents a neighbouring atlas cell bleeding in.
                float2 stroke=max(_MainTex_TexelSize.xy*2.5,fwidth(uv)*.9);
                half edge=0;
                edge=max(edge,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,clamp(uv+float2(stroke.x,0),lo,hi)).a);
                edge=max(edge,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,clamp(uv-float2(stroke.x,0),lo,hi)).a);
                edge=max(edge,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,clamp(uv+float2(0,stroke.y),lo,hi)).a);
                edge=max(edge,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,clamp(uv-float2(0,stroke.y),lo,hi)).a);
                half body=smoothstep(.11,.24,tex.a),outline=smoothstep(.13,.27,edge);
                half luminance=max(tex.r,max(tex.g,tex.b));
                half band=luminance<.28?.24:luminance<.55?.49:luminance<.81?.75:1;
                half3 painted=saturate(tex.rgb/max(luminance,.001)*band);
                half3 ink=half3(.07,.045,.14)+painted*.08;
                return half4(lerp(ink,painted,body),max(body,outline)*saturate(tex.a*4+outline));
            }
            half4 frag(Varyings i):SV_Target
            {
                half4 tex=half4(1,1,1,1);
                if(_UseTexture>.5)tex=lerp(CelSample(i.uv),CelSample(i.next.xy),i.next.z);
                half4 result=i.color*lerp(half4(1,1,1,1),tex,_UseTexture);
                if(_CellFeather>.5)
                {
                    float2 local=frac(i.uv*4);float2 edge=min(local,1-local);
                    result.a*=smoothstep(.004,.025,min(edge.x,edge.y));
                }
                if(_SoftParticles>.5)
                {
                    float raw=SampleSceneDepth(GetNormalizedScreenSpaceUV(i.positionCS));
                    float eye=LinearEyeDepth(raw,_ZBufferParams);
                    #if UNITY_REVERSED_Z
                    raw=1-raw;
                    #endif
                    if(unity_OrthoParams.w>.5)eye=lerp(_ProjectionParams.y,_ProjectionParams.z,raw);
                    float particle=-TransformWorldToView(i.world).z;
                    result.a*=saturate((eye-particle)/.12);
                }
                return result;
            }
            ENDHLSL
        }
    }
}
