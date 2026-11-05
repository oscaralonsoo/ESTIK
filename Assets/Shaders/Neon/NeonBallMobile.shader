
Shader "Custom/NeonBallMobile"
{
    Properties
    {
        _BaseColor ("Fondo", Color) = (0.01, 0.01, 0.04, 1)
        [HDR] _ColorA ("Neón A (abajo)", Color) = (0.0, 1.5, 2.0, 1)
        [HDR] _ColorB ("Neón B (arriba)", Color) = (2.0, 0.2, 1.5, 1)
        _GridCount ("Líneas de rejilla", Range(1, 12)) = 4
        _LineWidth ("Grosor de línea", Range(0.005, 0.1)) = 0.03
        _Glow ("Halo", Range(0, 1)) = 0.35
        _PulseSpeed ("Velocidad del pulso", Float) = 2
        _Speed ("Velocidad de giro", Float) = 0.3
        [HDR] _RimColor ("Color borde", Color) = (0.3, 0.8, 2.0, 1)
        _RimPower ("Fuerza borde", Range(0.5, 8)) = 3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ColorA;
                half4 _ColorB;
                half4 _RimColor;
                half _GridCount;
                half _LineWidth;
                half _Glow;
                half _PulseSpeed;
                half _Speed;
                half _RimPower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dir        : TEXCOORD0;
                half3  normalWS   : TEXCOORD1;
                half3  viewDirWS  : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.normalWS = (half3)TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = (half3)(GetCameraPositionWS() - posWS);

                float t = _Time.y * _Speed;
                float s = sin(t), c = cos(t);
                float3 p = IN.positionOS.xyz;
                OUT.dir = float3(c * p.x - s * p.z, p.y, s * p.x + c * p.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 p = normalize(IN.dir);

                float3 g = p * _GridCount;
                float3 d = abs(frac(g + 0.5) - 0.5);
                float3 aa = fwidth(g);

                float3 l3 = 1.0 - smoothstep(_LineWidth - aa, _LineWidth + aa, d);
                float lineMask = max(max(l3.x, l3.y), l3.z);

                float3 h3 = saturate(1.0 - d / (_LineWidth * 6.0));
                h3 *= h3;
                float halo = max(max(h3.x, h3.y), h3.z);

                half pulse = 0.5h + 0.5h * (half)sin(p.y * 6.0 - _Time.y * _PulseSpeed);
                half intensity = 1.0h + pulse;

                half3 neon = lerp(_ColorA.rgb, _ColorB.rgb, (half)(p.y * 0.5 + 0.5));

                half3 col = _BaseColor.rgb + neon * ((half)lineMask + halo * _Glow) * intensity;

                half rim = 1.0h - saturate(dot(normalize(IN.normalWS), normalize(IN.viewDirWS)));
                col += _RimColor.rgb * pow(rim, _RimPower);

                return half4(col, 1.0h);
            }
            ENDHLSL
        }
    }
}
