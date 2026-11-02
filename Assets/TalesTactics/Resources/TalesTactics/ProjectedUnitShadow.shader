Shader "TalesTactics/ProjectedUnitShadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Silhouette alpha",2D)="white"{}
        _Color ("Ground shadow",Color)=(.09,.14,.16,.22)
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True"}
        Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            struct v2f {float4 position:SV_POSITION;float2 uv:TEXCOORD0;fixed alpha:COLOR;};
            sampler2D _MainTex;fixed4 _Color;
            v2f vert(appdata v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.alpha=v.color.a;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                fixed alpha=tex2D(_MainTex,i.uv).a*_Color.a*i.alpha;
                clip(alpha-.001);
                return fixed4(_Color.rgb,alpha);
            }
            ENDCG
        }
    }
}
