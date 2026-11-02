Shader "Custom/CartoonLava"
{
    Properties
    {
        [Header(Colors)]
        _ColorDark   ("Lava Dark (roca/borde)", Color) = (0.55, 0.05, 0.02, 1)
        _ColorMid    ("Lava Mid (naranja)", Color)     = (1.0, 0.35, 0.05, 1)
        _ColorBright ("Lava Bright (amarillo)", Color) = (1.0, 0.85, 0.2, 1)
        _CrustColor  ("Crust Color (costra)", Color)   = (0.12, 0.04, 0.03, 1)

        [Header(Pattern)]
        _Scale       ("Noise Scale", Float) = 4
        _Speed       ("Flow Speed", Float) = 0.15
        _Steps       ("Color Steps (cartoon)", Range(2, 6)) = 3
        _CrustAmount ("Crust Amount", Range(0, 1)) = 0.35
        _CrustSoft   ("Crust Edge Softness", Range(0.001, 0.2)) = 0.03

        [Header(Wobble)]
        _WobbleAmp   ("Vertex Wobble Amount", Float) = 0.05
        _WobbleFreq  ("Vertex Wobble Frequency", Float) = 2
        _WobbleSpeed ("Vertex Wobble Speed", Float) = 1

        [Header(Glow)]
        _Emission    ("Brightness", Range(0.5, 3)) = 1.3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _ColorDark;
                half4  _ColorMid;
                half4  _ColorBright;
                half4  _CrustColor;
                float  _Scale;
                float  _Speed;
                float  _Steps;
                float  _CrustAmount;
                float  _CrustSoft;
                float  _WobbleAmp;
                float  _WobbleFreq;
                float  _WobbleSpeed;
                float  _Emission;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            // ---------- Noise ----------
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int i = 0; i < 3; i++)
                {
                    v += a * valueNoise(p);
                    p *= 2.0;
                    a *= 0.5;
                }
                return v;
            }

            // ---------- Vertex ----------
            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 pos = IN.positionOS.xyz;

                // Ondulación suave a lo largo de la normal (efecto "burbujeo")
                float t = _Time.y * _WobbleSpeed;
                float wave = sin(pos.x * _WobbleFreq + t) * cos(pos.z * _WobbleFreq + t * 1.3);
                pos += IN.normalOS * wave * _WobbleAmp;

                OUT.positionCS = TransformObjectToHClip(pos);
                OUT.uv = IN.uv;
                return OUT;
            }

            // ---------- Fragment ----------
            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv * _Scale;
                float t = _Time.y * _Speed;

                // Dos capas de ruido moviéndose en direcciones distintas, con distorsión
                float2 warp = float2(
                    fbm(uv + float2(t, 0.0)),
                    fbm(uv + float2(0.0, -t) + 7.3)
                );

                float n = fbm(uv + warp * 1.5 + float2(t * 0.5, t * 0.3));

                // Posterización: bandas de color planas (look cartoon)
                float steps = max(_Steps, 2.0);
                float band = floor(n * steps) / (steps - 1.0);
                band = saturate(band);

                // Gradiente de 3 colores sobre la banda
                half3 col = lerp(_ColorDark.rgb, _ColorMid.rgb, saturate(band * 2.0));
                col = lerp(col, _ColorBright.rgb, saturate(band * 2.0 - 1.0));

                // Costra oscura donde el ruido es bajo (bordes duros, estilo cartoon)
                float crust = 1.0 - smoothstep(_CrustAmount - _CrustSoft, _CrustAmount + _CrustSoft, n);
                col = lerp(col, _CrustColor.rgb, crust);

                col *= _Emission;

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
