Shader "TalesTactics/Architectural Terrain"
{
    Properties
    {
        _MainTex("Painted material atlas",2D)="white"{}
        _AtlasRect("Atlas rectangle",Vector)=(0,0,1,1)
        _BaseColor("Tint",Color)=(1,1,1,1)
        _KeyColor("Sunlight",Color)=(1,.96,.88,1)
        _FillColor("Ambient",Color)=(.78,.84,.9,1)
        _Water("Flowing surface",Float)=0
        _Unlit("Unlit",Float)=0
        _WorldFloor("Continuous floor",Float)=0
        _WorldCliff("Continuous cliff",Float)=0
        _DetailContrast("Detail contrast",Range(0,1))=1
        _FloorExtent("Floor extent",Vector)=(12,12,0,0)
        _MeadowTex("Moss and meadow",2D)="white"{}
        _LivingFloor("Vegetation blend",Float)=0
        _SurfaceRelief("Material relief",Float)=0
        _Dapple("Canopy light",Float)=0
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        Pass
        {
            Tags {"LightMode"="SRPDefaultUnlit"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_MeadowTex); SAMPLER(sampler_MeadowTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _AtlasRect, _BaseColor, _KeyColor, _FillColor;
            float _Water, _Unlit,_WorldFloor,_WorldCliff,_DetailContrast;float4 _FloorExtent;
            float4 _TerrainShadows[24];int _ShadowCount;
            float _LivingFloor,_SurfaceRelief,_Dapple;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; float4 color:COLOR;};
            struct Varyings {float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float2 uv:TEXCOORD1; float3 world:TEXCOORD2; float4 color:COLOR;};
            Varyings vert(Attributes v)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS=TransformObjectToWorldNormal(v.normalOS); o.world=TransformObjectToWorld(v.positionOS.xyz);
                o.uv=v.uv; o.color=v.color; return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                float2 local=frac(i.uv);
                if(_WorldFloor>.5) local=saturate((i.world.xz+.5)/_FloorExtent.xy);
                if(_WorldCliff>.5) local=frac(float2((i.world.x+i.world.z)*.17,(i.world.y+4.5)*.18));
                if(_Water>0.5) local=frac(i.uv+float2(sin(i.world.z*2+_Time.y*.7)*.012,_Time.y*.035));
                float2 uv=_AtlasRect.xy+local*_AtlasRect.zw;
                half3 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).rgb;
                float relief=dot(c-SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv+float2(.0015,.0015)).rgb,float3(.3,.5,.2));
                if(_LivingFloor>.5&&i.normalWS.y>.7)
                {
                    float patch=sin(i.world.x*.63+sin(i.world.z*.91))*sin(i.world.z*.57+cos(i.world.x*.75));
                    float edge=min(min(i.world.x+.5,_FloorExtent.x-.5-i.world.x),min(i.world.z+.5,_FloorExtent.y-.5-i.world.z));
                    float moss=smoothstep(.22,.70,patch)*.58+(1-smoothstep(0,1.5,edge))*.23;
                    half3 grass=SAMPLE_TEXTURE2D(_MeadowTex,sampler_MeadowTex,frac(i.world.xz*.23)).rgb;
                    c=lerp(c,grass,saturate(moss));
                }
                if(_WorldCliff>.5) {
                    half3 top=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,frac(i.world.xz*.17)).rgb;
                    c=lerp(c,top,smoothstep(.4,.85,abs(normalize(i.normalWS).y)));
                }
                if(_Water>0.5) {
                    float2 uv2=_AtlasRect.xy+frac(i.uv*1.09+float2(_Time.y*.018,-_Time.y*.026))*_AtlasRect.zw;
                    c=lerp(c,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv2).rgb,.32);
                    c*=.83+.12*sin(i.world.x*2.7+i.world.z*1.4+_Time.y*.8);
                    float glint=pow(saturate(sin(i.world.x*7.1+i.world.z*3.7+_Time.y*1.1)*sin(i.world.z*5.3-_Time.y*.7)),12);
                    c+=half3(.32,.55,.55)*glint;
                }
                half key=saturate(dot(normalize(i.normalWS),normalize(float3(-.45,.85,-.35))));
                c=lerp(half3(.43,.44,.43),c,_DetailContrast);
                // Broad cel planes retain the new painted shapes instead of adding noisy relief.
                key=.35*step(.22,key)+.38*step(.56,key)+.27*step(.82,key);
                half3 illumination=lerp(_FillColor.rgb*.76,_KeyColor.rgb*1.02,key);
                illumination*=1+clamp(relief*2,-.08,.08)*_SurfaceRelief;
                float dapple=sin(i.world.x*1.37+sin(i.world.z*1.8))*sin(i.world.z*1.63+cos(i.world.x*2.2));
                illumination*=1-_Dapple*smoothstep(-.4,.6,dapple)*saturate(i.normalWS.y);
                illumination=lerp(illumination,1,_Unlit);
                // Architectural depth is shaded independently of the unlit character art.
                half depth=lerp(.58,1,saturate((i.world.y+4.5)/4.5));
                float occlusion=1;
                for(int s=0;s<_ShadowCount;s++)
                {
                    float4 caster=_TerrainShadows[s];float2 delta=i.world.xz-caster.xy-float2(.20,.15)*caster.z;
                    float contact=exp(-dot(delta,delta)/(caster.z*caster.z*.22));
                    float sameLevel=saturate(1-abs(i.world.y-caster.w)*4);
                    occlusion*=1-contact*.38*sameLevel*saturate(i.normalWS.y);
                }
                return half4(c*illumination*occlusion*lerp(depth,1,_Unlit)*_BaseColor.rgb*i.color.rgb,1);
            }
            ENDHLSL
        }
    }
}
