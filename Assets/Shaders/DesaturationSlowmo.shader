
Shader "Custom/DesaturationSlowmo"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Desaturation ("Desaturation", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Desaturation;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                float gray = dot(
                    col.rgb,
                    float3(0.2126, 0.7152, 0.0722)
                );

                col.rgb = lerp(
                    col.rgb,
                    float3(gray, gray, gray),
                    _Desaturation
                );

                return col;
            }

            ENDCG
        }
    }
}
