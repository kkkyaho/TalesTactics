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
        _ArtSaturation("Art saturation",Range(0,1))=1
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
            #include "../EnvironmentLight.cginc"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_MeadowTex); SAMPLER(sampler_MeadowTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _AtlasRect, _BaseColor, _KeyColor, _FillColor;
            float _Water, _Unlit,_WorldFloor,_WorldCliff,_DetailContrast;float4 _FloorExtent;
            float4 _TerrainShadows[24];int _ShadowCount;
            float _LivingFloor,_SurfaceRelief,_Dapple,_ArtSaturation;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; float4 color:COLOR;};
            struct Varyings {float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float2 uv:TEXCOORD1; float3 world:TEXCOORD2; float4 color:COLOR;};
            Varyings vert(Attributes v)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS=TransformObjectToWorldNormal(v.normalOS); o.world=TransformObjectToWorld(v.positionOS.xyz);
                o.uv=v.uv; o.color=v.color; return o;
            }
            // Stable, low-frequency variation joins adjacent cells without moving the authored paving.
            float SurfaceHash(float2 p)
            {
                float3 q=frac(float3(p.x,p.y,p.x)*.1031);
                q+=dot(q,q.yzx+33.33);
                return frac((q.x+q.y)*q.z);
            }
            float SurfaceNoise(float2 p)
            {
                float2 cell=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(SurfaceHash(cell),SurfaceHash(cell+float2(1,0)),f.x),
                            lerp(SurfaceHash(cell+float2(0,1)),SurfaceHash(cell+1),f.x),f.y);
            }
            float2 MirroredSurfaceUV(float2 p)
            {
                // Atlas regions are illustrations, not seamless tiles. Reflection keeps their edges joined.
                return abs(frac(p*.5)*2-1);
            }
            half4 frag(Varyings i):SV_Target
            {
                float2 local=frac(i.uv);
                if(_WorldFloor>.5) local=saturate((i.world.xz+.5)/_FloorExtent.xy);
                if(_WorldCliff>.5) local=frac(float2((i.world.x+i.world.z)*.17,(i.world.y+4.5)*.18));
                float2 waterPlane=i.world.xz;
                float waterNoise=0;
                if(_Water>.5)
                {
                    // One coordinate field spans the river; vertical curtains flow down their world height.
                    float vertical=1-step(.5,abs(i.normalWS.y));
                    float across=abs(i.normalWS.z)>abs(i.normalWS.x)?i.world.x:i.world.z;
                    waterPlane=lerp(i.world.xz,float2(across,i.world.y),vertical);
                    waterNoise=SurfaceNoise(waterPlane*.34+float2(8.7,2.1));
                    float2 drift=lerp(float2(.025,-.019),float2(0,.12),vertical)*_Time.y;
                    local=MirroredSurfaceUV(waterPlane*.21+drift+(waterNoise-.5)*.13);
                }
                float2 uv=_AtlasRect.xy+local*_AtlasRect.zw;
                half3 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).rgb;
                float relief=0;
                if(_Water<.5)
                    relief=dot(c-SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv+float2(.0015,.0015)).rgb,float3(.3,.5,.2));
                if(_WorldFloor>.5&&i.normalWS.y>.7)
                {
                    float broad=SurfaceNoise(i.world.xz*.27+float2(4.1,9.7));
                    float broken=SurfaceNoise(i.world.xz*.79+float2(17.3,2.9));
                    // Preserve stone joints and tile readability; only broad weathering varies across the map.
                    c*=lerp(.94,1.04,broad*.75+broken*.25);
                    if(_LivingFloor>.5)
                    {
                        float edge=min(min(i.world.x+.5,_FloorExtent.x-.5-i.world.x),min(i.world.z+.5,_FloorExtent.y-.5-i.world.z));
                        float patch=broad*.78+broken*.22;
                        float moss=smoothstep(.44,.72,patch)*.54;
                        // Broken edge pockets replace the uniform green strip along the whole rectangle.
                        moss+=(1-smoothstep(0,1.25,edge))*smoothstep(.32,.72,broken)*.22;
                        float2 meadowUV=MirroredSurfaceUV(i.world.xz*.19+float2(broad,broken)*.12);
                        half3 grass=SAMPLE_TEXTURE2D(_MeadowTex,sampler_MeadowTex,meadowUV).rgb;
                        c=lerp(c,grass,saturate(moss));
                    }
                }
                if(_WorldCliff>.5) {
                    half3 top=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,frac(i.world.xz*.17)).rgb;
                    c=lerp(c,top,smoothstep(.4,.85,abs(normalize(i.normalWS).y)));
                }
                if(_Water>.5)
                {
                    float vertical=1-step(.5,abs(i.normalWS.y));
                    float2 secondary=float2(waterPlane.x*.8+waterPlane.y*.6,-waterPlane.x*.6+waterPlane.y*.8);
                    secondary=lerp(secondary,waterPlane,vertical);
                    float2 drift=lerp(float2(-.014,.021),float2(0,.083),vertical)*_Time.y;
                    float2 uv2=_AtlasRect.xy+MirroredSurfaceUV(secondary*.163+drift+float2(.37,.69))*_AtlasRect.zw;
                    c=lerp(c,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv2).rgb,.38);
                    c*=.87+.10*waterNoise;
                    float wave=sin(dot(waterPlane,float2(2.3,1.1))+_Time.y*.7+waterNoise*2);
                    float glint=smoothstep(.82,.99,wave)*smoothstep(.28,.78,waterNoise);
                    // Quiet broad highlights leave shore edges and tactical overlays readable.
                    c+=half3(.12,.19,.19)*glint;
                }
                half key=saturate(dot(normalize(i.normalWS),normalize(float3(-.45,.85,-.35))));
                c=lerp(half3(.43,.44,.43),c,_DetailContrast);
                c=TalesPalette(c,_ArtSaturation,1);
                // Broad cel planes retain the new painted shapes instead of adding noisy relief.
                key=.35*step(.22,key)+.38*step(.56,key)+.27*step(.82,key);
                half3 illumination=lerp(_FillColor.rgb*.76,_KeyColor.rgb*1.02,key);
                illumination*=1+clamp(relief*2,-.08,.08)*_SurfaceRelief;
                illumination*=1-_Dapple*TalesCanopy(i.world.xz)*saturate(i.normalWS.y);
                illumination=lerp(illumination,1,_Unlit);
                // Terrain and sprites share canopy coordinates and chapter light colors.
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
