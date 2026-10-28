// Shader SOLO para hornear. Se usa como skybox temporal y se renderiza a un cubemap.
// Calidad alta (5 octavas + domain warping) porque se ejecuta una sola vez, en el editor.
Shader "Custom/GalaxyBake"
{
    Properties
    {
        _ColorDark ("Fondo", Color) = (0.01, 0.0, 0.08, 1)
        _ColorA ("Nebulosa 1", Color) = (0.45, 0.1, 0.9, 1)
        _ColorB ("Nebulosa 2", Color) = (0.1, 0.5, 1.0, 1)
        _NebulaScale ("Escala nebulosa", Float) = 2.0
        _StarDensity ("Densidad estrellas", Float) = 40
        _StarThreshold ("Cantidad estrellas", Range(0.9, 0.999)) = 0.95
        _Seed ("Semilla", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            fixed4 _ColorDark, _ColorA, _ColorB;
            float _NebulaScale, _StarDensity, _StarThreshold, _Seed;

            struct appdata { float4 vertex : POSITION; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz; // en un skybox, la posición del vértice es la dirección
                return o;
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

            float fbm(float3 p)
            {
                float v = 0.0, a = 0.5;
                for (int i = 0; i < 5; i++)
                {
                    v += a * noise3(p);
                    p *= 2.0;
                    a *= 0.5;
                }
                return v;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 p = normalize(i.dir);

                // Nebulosa con domain warping
                float3 q = p * _NebulaScale + _Seed * 13.37;
                float w = fbm(q);
                float n = fbm(q + w * 1.5);

                float3 col = _ColorDark.rgb;
                col = lerp(col, _ColorA.rgb, smoothstep(0.25, 0.7, n));
                col = lerp(col, _ColorB.rgb, smoothstep(0.45, 0.9, w) * 0.8);

                // Estrellas
                float3 sp = p * _StarDensity + _Seed * 7.77;
                float3 id = floor(sp);
                float h = hash31(id);
                float d = length(frac(sp) - 0.5);
                float size = lerp(0.15, 0.35, frac(h * 17.0));
                float star = step(_StarThreshold, h) * (1.0 - smoothstep(0.0, size, d));
                col += star * star * 1.5;

                return fixed4(saturate(col), 1);
            }
            ENDCG
        }
    }
}
