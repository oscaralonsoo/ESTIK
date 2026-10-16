
Shader "Custom/Water"
{
    Properties
    {
        _Color ("Water Color", Color) = (0.05, 0.65, 0.8, 1)
        _Speed ("Wave Speed", Float) = 0.15
        _Scale ("Wave Scale", Float) = 3
        _Strength ("Wave Strength", Float) = 0.08
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Speed;
            float _Scale;
            float _Strength;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;

                float3 world = mul(unity_ObjectToWorld,
                                   v.vertex).xyz;

                float wave =
                    sin(world.x * _Scale + _Time.y * _Speed)
                    * cos(world.z * _Scale + _Time.y * _Speed);

                world.y += wave * _Strength;

                o.vertex = mul(UNITY_MATRIX_VP,
                               float4(world, 1));

                o.worldPos = world;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float wave = sin(
                    (i.worldPos.x + i.worldPos.z) * _Scale
                    + _Time.y * _Speed
                );

                fixed4 col = _Color;
                col.rgb += wave * 0.025;

                return col;
            }
            ENDCG
        }
    }
}
