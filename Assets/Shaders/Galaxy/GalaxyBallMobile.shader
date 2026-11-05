Shader "Custom/GalaxyBallMobile"
{
    Properties
    {
        [NoScaleOffset] _Cube ("Galaxia (Cubemap)", Cube) = "" {}
        _Tint ("Tinte", Color) = (1, 1, 1, 1)
        _Speed ("Velocidad de giro", Float) = 0.15
        _Twinkle ("Parpadeo estrellas", Range(0, 1)) = 0.3
        _RimColor ("Color borde", Color) = (0.5, 0.7, 1, 1)
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

            TEXTURECUBE(_Cube);
            SAMPLER(sampler_Cube);

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half4 _RimColor;
                half _Speed;
                half _Twinkle;
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
                half3 col = SAMPLE_TEXTURECUBE(_Cube, sampler_Cube, IN.dir).rgb * _Tint.rgb;

                half lum = dot(col, half3(0.299h, 0.587h, 0.114h));
                half starMask = smoothstep(0.5h, 0.9h, lum);
                float phase = dot(normalize(IN.dir), float3(127.1, 311.7, 74.7));
                col *= 1.0h + _Twinkle * starMask * (half)sin(_Time.y * 3.0 + phase);

                half rim = 1.0h - saturate(dot(normalize(IN.normalWS), normalize(IN.viewDirWS)));
                col += _RimColor.rgb * pow(rim, _RimPower);

                return half4(col, 1.0h);
            }
            ENDHLSL
        }
    }
}
