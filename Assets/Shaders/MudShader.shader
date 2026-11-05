
Shader "Stylized/MudShader"
{
    Properties
    {
        [Header(Colors)]
        _ColorBase   ("Base",     Color) = (0.0582, 0.0356, 0.0189, 1)
        _ColorBlotch ("Blotches", Color) = (0.0399, 0.0242, 0.0127, 1)
        _ColorRim    ("Edge",     Color) = (0.0254, 0.0152, 0.0078, 1)
        _ColorShine  ("Shine",    Color) = (0.0759, 0.0461, 0.0231, 1)
        _ColorFringe ("Fringe",   Color) = (0.0084, 0.0546, 0.0254, 1)
        _MudOpacity  ("Mud opacity (<1 deja ver el suelo)", Range(0.7, 1)) = 0.92

        [Header(Shape)]
        _OutlineBumps ("Outline irregularity", Range(0, 0.4)) = 0.24
        _OutlineScale ("Outline detail",       Range(0.5, 8)) = 4
        _Seed         ("Shape seed",           Float) = 2
        _RimWidth     ("Edge width",           Range(0, 0.3)) = 0.05

        [Header(Outer Fringe)]
        _FringeWidth  ("Fringe width",     Range(0, 0.25)) = 0.08
        _FringeWobble ("Fringe irregularity", Range(0, 0.25)) = 0.08
        _FringeAlpha  ("Fringe opacity",   Range(0, 1)) = 0.5

        [Header(Shine)]
        _ShineOpacity ("Shine opacity (0 = off)", Range(0, 1)) = 1
        _Shine        ("Shine pos XY / size XY", Vector) = (-0.28, 0.30, 0.22, 0.09)
        _ShineAngle   ("Shine angle (rad)", Range(0, 3.14)) = 0.7

        [Header(Blotches)]
        [Toggle(_WORLD_UV)] _WorldUV ("Blotches in World XZ", Float) = 1
        _BlotchScale  ("Blotch scale (smaller = bigger)", Range(0.1, 6)) = 0.6
        _BlotchAmount ("Blotch amount", Range(0, 1)) = 0.3
        _MoveSpeed    ("Movement speed (0 = still)", Range(0, 3)) = 0.5

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
        float4 _ColorShine;
        float4 _ColorFringe;
        float  _MudOpacity;
        float  _OutlineBumps;
        float  _OutlineScale;
        float  _Seed;
        float  _RimWidth;
        float  _FringeWidth;
        float  _FringeWobble;
        float  _FringeAlpha;
        float  _ShineOpacity;
        float4 _Shine;
        float  _ShineAngle;
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
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull [_Cull]
            ZWrite Off
            ZTest LEqual
            Offset [_OffsetFactor], [_OffsetUnits]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma shader_feature_local_fragment _WORLD_UV

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half   fogCoord   : TEXCOORD2;
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
                OUT.fogCoord   = ComputeFogFactor(vi.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
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
                float3 mud = lerp(_ColorBase.rgb, _ColorBlotch.rgb, Cut(n, 1.0 - _BlotchAmount));

                float2 q = IN.uv * 2.0 - 1.0;
                float e = 1.0 - length(q);
                e -= valueNoise(IN.uv * _OutlineScale + _Seed * 13.7) * _OutlineBumps;

                float ef = e + (valueNoise(IN.uv * 3.1 + _Seed * 7.3 + float2(5.2, 1.7)) - 0.5) * _FringeWobble;

                const float outerEdge = 0.02;
                float cutoff = outerEdge + _FringeWidth;

                float inside = Cut(e, cutoff);
                float rim    = 1.0 - Cut(e, cutoff + _RimWidth);
                float3 col = lerp(mud, _ColorRim.rgb, rim);

                float2 s = q - _Shine.xy;
                float ca = cos(_ShineAngle);
                float sa = sin(_ShineAngle);
                float2 r = float2(s.x * ca + s.y * sa, -s.x * sa + s.y * ca) / max(_Shine.zw, 1e-4);
                float shineSD = 1.0 - length(r);
                float shine = Cut(shineSD, 0.0) * Cut(e, cutoff + _RimWidth + 0.05);
                col = lerp(col, _ColorShine.rgb, shine * _ShineOpacity);

                col = lerp(_ColorFringe.rgb, col, inside);
                float fringeA = Cut(ef, outerEdge) * _FringeAlpha;
                float alpha = lerp(fringeA, _MudOpacity, inside);

                clip(alpha - 0.001);

                col = MixFog(col, IN.fogCoord);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
