Shader "TalesTactics/Impact"
{
    Properties { _MainTex("Effect atlas",2D)="white"{} _UseTexture("Use texture",Float)=0 _SoftParticles("Depth feather",Float)=0 _CellFeather("Atlas cell feather",Float)=0 }
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
            float _UseTexture, _SoftParticles, _CellFeather;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float3 next:TEXCOORD1; };
            struct Varyings { float4 positionCS:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float3 next:TEXCOORD1; float3 world:TEXCOORD2; };
            Varyings vert(Attributes v){Varyings o;o.world=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.color=v.color;o.uv=v.uv;o.next=v.next;return o;}
            half4 frag(Varyings i):SV_Target
            {
                half4 tex=lerp(SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv),SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.next.xy),i.next.z);
                half4 result=i.color*lerp(half4(1,1,1,1),tex,_UseTexture);
                if(_CellFeather>.5)
                {
                    float2 local=frac(i.uv*4);float2 edge=min(local,1-local);
                    result.a*=smoothstep(.004,.085,min(edge.x,edge.y));
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
                    result.a*=saturate((eye-particle)/.24);
                }
                return result;
            }
            ENDHLSL
        }
    }
}
