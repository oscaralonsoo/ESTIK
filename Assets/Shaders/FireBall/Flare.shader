// Bola de fuego para URP, sin partículas: el efecto va en el propio material de la bola.
// Interior rojo oscuro con vetas naranjas que fluyen, borde difuminado (semitransparente)
// y brillo amarillo/blanco en el contorno.
// Coste: ~2 llamadas a ruido 3D por píxel (unos 16 hashes). Una sola bola con transparencia es barato.
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
        // Transparent-10 = se dibuja justo ANTES que lo transparente normal (partículas, capas de fuera...)
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Tags { "LightMode"="UniversalForward" }

            // Alpha premultiplicado: el color base se atenúa con el alpha y el brillo se suma encima
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorCore;
                half4 _ColorMid;
                half4 _ColorHot;
                half4 _ColorWhite;
                half _Scale;
                half _FlowSpeed;
                half _Streaks;
                half _Softness;
                half _Opacity;
                half _Glow;
                half _RimPower;
                half _Expand;
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
                // Inflamos un poco la malla para que el difuminado quede por fuera y el núcleo conserve su tamaño
                float3 posOS = IN.positionOS.xyz + normalize(IN.normalOS) * _Expand;
                float3 posWS = TransformObjectToWorld(posOS);
                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.dir = IN.positionOS.xyz;
                OUT.normalWS = (half3)TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = (half3)(GetCameraPositionWS() - posWS);
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
                float t = _Time.y;

                // Fuego que fluye hacia arriba, con una deformación barata hecha con senos
                float3 q = p * _Scale;
                q.y -= t * _FlowSpeed;
                q += 0.35 * sin(q.zxy * 1.7 + t * 0.6);
                half n = (half)(noise3(q) * 0.65 + noise3(q * 2.1 + 5.0) * 0.35);

                // Calor: late un poco para que el fuego "respire"
                half heat = saturate(n * 1.5 - 0.1h + 0.06h * (half)sin(t * 3.0));

                // Rampa de color: rojo oscuro -> naranja -> amarillo
                half3 col = lerp(_ColorCore.rgb, _ColorMid.rgb, smoothstep(0.2h, 0.6h, heat));
                col = lerp(col, _ColorHot.rgb, smoothstep(0.55h, 0.95h, heat));

                // Vetas brillantes, donde el ruido cruza 0.5
                half vein = 1.0h - abs(n * 2.0h - 1.0h);
                vein = vein * vein * vein;
                col += _ColorMid.rgb * vein * _Streaks;

                // Borde: el color se calienta hacia el amarillo/blanco, a manchas
                half ndv = saturate(dot(normalize(IN.normalWS), normalize(IN.viewDirWS)));
                half rim = pow(1.0h - ndv, _RimPower);
                col = lerp(col, _ColorWhite.rgb, rim * (0.25h + 0.6h * n));

                // Difuminado: el alpha cae hacia el contorno
                half alpha = smoothstep(0.0h, _Softness, ndv) * _Opacity;

                // Brillo del contorno: más fuerte en una banda cerca del borde y a manchas
                half band = smoothstep(0.02h, 0.2h, ndv) * rim;
                half3 glow = _ColorHot.rgb * band * (0.3h + 0.7h * n) * _Glow;

                return half4(col * alpha + glow * _Opacity, alpha);
            }
            ENDHLSL
        }
    }
}
