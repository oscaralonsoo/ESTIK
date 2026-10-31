// Stylized Mud (Simple) - Unlit URP
// Diseno minimo y 100% plano, sin degradados ni transparencia:
//   - Silueta organica (recorte duro) -> forma de charco
//   - 1 banda de borde oscuro         -> como la banda oscura del cesped
//   - Pocas manchas grandes           -> se mueven muy despacio
Shader "Stylized/Mud Simple Unlit URP"
{
    Properties
    {
        [Header(Colors)]
        _ColorBase   ("Base",        Color) = (0.42, 0.29, 0.18, 1)
        _ColorBlotch ("Blotches",    Color) = (0.35, 0.24, 0.15, 1)
        _ColorRim    ("Edge band",   Color) = (0.26, 0.17, 0.10, 1)

        [Header(Shape)]
        [KeywordEnum(Radial, VertexColor, None)] _Shape ("Shape source", Float) = 0
        _OutlineBumps ("Outline irregularity", Range(0, 0.4)) = 0.15
        _OutlineScale ("Outline detail",       Range(0.5, 8)) = 2.5
        _Seed         ("Shape seed",           Float) = 0
        _RimWidth     ("Edge band width",      Range(0, 0.5)) = 0.12

        [Header(Blotches)]
        [Toggle(_WORLD_UV)] _WorldUV ("Blotches in World XZ", Float) = 1
        _BlotchScale  ("Blotch scale (smaller = bigger)", Range(0.1, 3)) = 0.6
        _BlotchAmount ("Blotch amount", Range(0, 1)) = 0.35
        _MoveSpeed    ("Movement speed (0 = still)", Range(0, 3)) = 1

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
        _OffsetFactor ("Depth offset factor", Float) = -1
        _OffsetUnits  ("Depth offset units",  Float) = -1
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float4 _ColorBase;
        float4 _ColorBlotch;
        float4 _ColorRim;
        float  _Shape;
        float  _OutlineBumps;
        float  _OutlineScale;
        float  _Seed;
        float  _RimWidth;
        float  _WorldUV;
        float  _BlotchScale;
        float  _BlotchAmount;
        float  _MoveSpeed;
        float  _Cull;
        float  _OffsetFactor;
        float  _OffsetUnits;
    CBUFFER_END
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry+1"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual
            Offset [_OffsetFactor], [_OffsetUnits]   // evita z-fighting con el cesped

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma shader_feature_local_fragment _WORLD_UV
            #pragma shader_feature_local_fragment _SHAPE_RADIAL _SHAPE_VERTEXCOLOR _SHAPE_NONE

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 color      : TEXCOORD2;
                half   fogCoord   : TEXCOORD3;
            };

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
                return lerp(lerp(hash21(i),                hash21(i + float2(1, 0)), f.x),
                            lerp(hash21(i + float2(0, 1)), hash21(i + float2(1, 1)), f.x), f.y);
            }

            // Corte duro con 1 pixel de anti-aliasing: 0 si v < t, 1 si v > t
            float Cut(float v, float t)
            {
                float w = max(fwidth(v), 1e-5);
                return smoothstep(t - w, t + w, v);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vi = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = vi.positionCS;
                OUT.positionWS = vi.positionWS;
                OUT.uv         = IN.uv;
                OUT.color      = IN.color;
                OUT.fogCoord   = ComputeFogFactor(vi.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // ---------- Manchas (planas, en movimiento lento) ----------
                #if defined(_WORLD_UV)
                    float2 pw = IN.positionWS.xz;
                #else
                    float2 pw = IN.uv;
                #endif

                float t = _Time.y * _MoveSpeed;
                float2 p = pw * _BlotchScale;
                float2 warp = float2(valueNoise(p * 0.8 + t * 0.15),
                                     valueNoise(p * 0.8 + 11.3 - t * 0.15)) - 0.5;
                p += warp * 0.6 + float2(1.0, 0.6) * (t * 0.04);

                float n = valueNoise(p);
                float3 col = lerp(_ColorBase.rgb, _ColorBlotch.rgb,
                                  Cut(n, 1.0 - _BlotchAmount));

                // ---------- Silueta organica + banda de borde ----------
                // e = distancia al borde (0 en el borde, crece hacia dentro)
                #if defined(_SHAPE_RADIAL)
                    float e = 1.0 - length(IN.uv * 2.0 - 1.0);   // charco a partir de cualquier quad/plane
                #elif defined(_SHAPE_VERTEXCOLOR)
                    float e = IN.color.r;                         // negro en los vertices del borde
                #else
                    float e = 1.0;
                #endif

                #if !defined(_SHAPE_NONE)
                    // El ruido solo "muerde" hacia dentro: nunca se ve el borde recto de la malla
                    e -= valueNoise(IN.uv * _OutlineScale + _Seed * 13.7) * _OutlineBumps;

                    const float cutoff = 0.02;
                    clip(e - cutoff);                              // silueta dura, sin transparencia

                    float rim = 1.0 - Cut(e, cutoff + _RimWidth);  // banda de borde plana
                    col = lerp(col, _ColorRim.rgb, rim);
                #endif

                col = MixFog(col, IN.fogCoord);
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
