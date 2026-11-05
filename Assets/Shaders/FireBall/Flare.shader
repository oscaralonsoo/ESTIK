Shader "Custom/FireBallMobile"
{
    Properties
    {
        _ColorCore ("Núcleo (rojo oscuro)", Color) = (0.35, 0.02, 0.0, 1)
        [HDR] _ColorMid ("Naranja", Color) = (1.6, 0.45, 0.03, 1)
        [HDR] _ColorHot ("Amarillo", Color) = (2.2, 1.5, 0.35, 1)
        [HDR] _ColorWhite ("Borde blanco", Color) = (2.0, 1.8, 1.4, 1)
        _Scale ("Escala del fuego", Range(0.5, 6)) = 2.2
        _FlowSpeed ("Velocidad del flujo", Float) = 0.5
        _Streaks ("Vetas brillantes", Range(0, 2)) = 0.8
        _Softness ("Difuminado del borde", Range(0.05, 1)) = 0.45
        _Opacity ("Opacidad general", Range(0, 1)) = 1
        _Glow ("Brillo del contorno", Range(0, 2)) = 0.8
        _RimPower ("Fuerza del contorno", Range(0.5, 6)) = 2.5
        _Expand ("Agrandar la malla", Range(0, 0.3)) = 0.05
    }

    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "FireDepth"
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 vertDepth(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half4 fragDepth() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "FireColor"
            Tags { "LightMode"="UniversalForward" }

            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorCore;
                float4 _ColorMid;
                float4 _ColorHot;
                float4 _ColorWhite;
                float _Scale;
                float _FlowSpeed;
                float _Streaks;
                float _Softness;
                float _Opacity;
                float _Glow;
                float _RimPower;
                float _Expand;
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
                float3 normalWS   : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posOS = IN.positionOS.xyz + normalize(IN.normalOS) * _Expand;
                float3 posWS = TransformObjectToWorld(posOS);
                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.dir = IN.positionOS.xyz;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetCameraPositionWS() - posWS;
                return OUT;
            }

            float hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float noise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(lerp(hash31(i), hash31(i + float3(1,0,0)), f.x),
                         lerp(hash31(i + float3(0,1,0)), hash31(i + float3(1,1,0)), f.x), f.y),
                    lerp(lerp(hash31(i + float3(0,0,1)), hash31(i + float3(1,0,1)), f.x),
                         lerp(hash31(i + float3(0,1,1)), hash31(i + float3(1,1,1)), f.x), f.y),
                    f.z);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 p = normalize(IN.dir);
                float t = fmod(_Time.y, 1000.0);

                float3 q = p * _Scale;
                q.y -= t * _FlowSpeed;
                q += 0.35 * sin(q.zxy * 1.7 + t * 0.6);
                float n = noise3(q) * 0.65 + noise3(q * 2.1 + 5.0) * 0.35;

                float heat = saturate(n * 1.5 - 0.1 + 0.06 * sin(t * 3.0));

                float3 col = lerp(_ColorCore.rgb, _ColorMid.rgb, smoothstep(0.2, 0.6, heat));
                col = lerp(col, _ColorHot.rgb, smoothstep(0.55, 0.95, heat));

                float vein = 1.0 - abs(n * 2.0 - 1.0);
                vein = vein * vein * vein;
                col += _ColorMid.rgb * vein * _Streaks;

                float3 nrm = normalize(IN.normalWS);
                float3 vd  = IN.viewDirWS * rsqrt(max(dot(IN.viewDirWS, IN.viewDirWS), 1e-6));
                float ndv = saturate(dot(nrm, vd));
                float rim = pow(max(1.0 - ndv, 1e-4), _RimPower);
                col = lerp(col, _ColorWhite.rgb, rim * (0.25 + 0.6 * n));

                float alpha = smoothstep(0.0, _Softness, ndv) * _Opacity;

                float band = smoothstep(0.02, 0.2, ndv) * rim;
                float3 glow = _ColorHot.rgb * band * (0.3 + 0.7 * n) * _Glow;

                return half4((half3)(col * alpha + glow * _Opacity), (half)alpha);
            }
            ENDHLSL
        }
    }
}
