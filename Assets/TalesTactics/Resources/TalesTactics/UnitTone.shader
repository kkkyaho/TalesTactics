Shader "TalesTactics/UnitTone"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Saturation ("Saturation", Range(0,1)) = 1
        _Brightness ("Brightness", Range(0,1)) = 1
        _EnvironmentTint ("Environment tint",Color)=(1,1,1,1)
        _ArtSaturation ("Art saturation",Range(0,1))=1
        _ArtContrast ("Art contrast",Range(0,1))=1
        _EnvironmentLight ("Environment light",Range(0,1))=0
        _KeyColor ("Sunlight",Color)=(1,1,1,1)
        _FillColor ("Ground bounce",Color)=(1,1,1,1)
        _CanopyShade ("Canopy shade",Range(0,1))=0
        _GroundShade ("Ground shade",Range(0,1))=1
        _GroundAnchor ("Ground anchor",Vector)=(0,0,0,0)
        _SpriteSpan ("Sprite vertical span",Vector)=(0,1,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "EnvironmentLight.cginc"
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            struct v2f {float4 position:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;float3 world:TEXCOORD1;float height:TEXCOORD2;};
            sampler2D _MainTex;fixed4 _Color,_EnvironmentTint;half _Saturation,_Brightness,_ArtSaturation,_ArtContrast,_EnvironmentLight,_CanopyShade,_GroundShade;
            half4 _KeyColor,_FillColor;float4 _GroundAnchor,_SpriteSpan;
            v2f vert(appdata v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.height=saturate((v.vertex.y-_SpriteSpan.x)/max(.01,_SpriteSpan.y));return o;}
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv);half gray=dot(c.rgb,half3(.2126,.7152,.0722));
                c.rgb=TalesPalette(c.rgb,_ArtSaturation,_ArtContrast);
                half3 light=lerp(_FillColor.rgb,_KeyColor.rgb,smoothstep(0,.75,i.height));
                float2 ground=lerp(i.world.xz,_GroundAnchor.xz,_GroundAnchor.w);
                light*=1-_CanopyShade*TalesCanopy(ground);
                light*=lerp(1,_GroundShade,.65);
                c.rgb*=lerp(half3(1,1,1),light,_EnvironmentLight);
                gray=dot(c.rgb,half3(.2126,.7152,.0722));
                c.rgb=lerp(gray.xxx,c.rgb,_Saturation)*_Brightness*_EnvironmentTint.rgb;return c*i.color;
            }
            ENDCG
        }
    }
}
