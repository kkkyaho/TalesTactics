Shader "TalesTactics/Atmospheric Panorama"
{
    Properties {
        _MainTex("Panorama",2D)="white"{}
        _Atmosphere("Atmosphere",Color)=(.13,.21,.27,1)
    }
    SubShader {
        Tags {"RenderType"="Opaque" "Queue"="Background"}
        Cull Off ZWrite Off
        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; fixed4 _Atmosphere;
            fixed4 frag(v2f_img i):SV_Target {
                fixed3 c=tex2D(_MainTex,i.uv).rgb;
                half l=dot(c,half3(.2126,.7152,.0722));
                c=lerp(l.xxx,c,.72);
                c=(c-.35)*.68+.30;
                return fixed4(lerp(c*.80,_Atmosphere.rgb,.26),1);
            }
            ENDCG
        }
    }
}
