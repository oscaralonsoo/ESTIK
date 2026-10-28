// Shader SOLO para hornear. Guarda tres máscaras en el cubemap:
//   R = grietas de superficie (angulares, Voronoi)
//   G = grietas profundas
//   B = turbidez del interior + cristalitos para los destellos
Shader "Custom/IceBake"
{
    Properties
    {
        _CrackScale ("Escala grietas", Float) = 3
        _CrackWidth ("Grosor grietas", Range(0.01, 0.3)) = 0.07
        _CrackKeep ("Aristas que se quedan", Range(0, 1)) = 0.55
        _HairlineAmount ("Grietas finas", Range(0, 1)) = 0.6
        _DeepScale ("Escala grietas profundas", Float) = 2
        _DeepWidth ("Grosor grietas profundas", Range(0.01, 0.5)) = 0.18
        _DeepKeep ("Aristas profundas que se quedan", Range(0, 1)) = 0.75
        _CloudScale ("Escala turbidez", Float) = 5
        _Warp ("Irregularidad", Range(0, 0.6)) = 0.25
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

            float _CrackScale, _CrackWidth, _CrackKeep, _HairlineAmount;
            float _DeepScale, _DeepWidth, _DeepKeep, _CloudScale, _Warp, _Seed;

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
                o.dir = v.vertex.xyz;
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
                for (int i = 0; i < 4; i++)
                {
                    v += a * noise3(p);
                    p *= 2.0;
                    a *= 0.5;
                }
                return v;
            }

            // Voronoi 3D: devuelve (F2 - F1), que vale ~0 en las aristas entre celdas.
            // keepH es un valor 0..1 que identifica la arista (igual para las dos celdas que la comparten),
            // así podemos quedarnos solo con algunas aristas y que no parezca un suelo de baldosas.
            float voronoiEdge(float3 p, out float keepH)
            {
                float3 ip = floor(p);
                float3 fp = frac(p);
                float d1 = 8.0, d2 = 8.0;
                float h1 = 0.0, h2 = 0.0;

                for (int z = -1; z <= 1; z++)
                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    float3 g = float3(x, y, z);
                    float3 cell = ip + g;
                    float3 o = float3(hash31(cell), hash31(cell + 17.1), hash31(cell + 41.7));
                    float3 r = g + o - fp;
                    float d = dot(r, r);
                    float h = hash31(cell + 3.3);
                    if (d < d1) { d2 = d1; h2 = h1; d1 = d; h1 = h; }
                    else if (d < d2) { d2 = d; h2 = h; }
                }

                keepH = frac((h1 + h2) * 13.7);
                return sqrt(d2) - sqrt(d1);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 p = normalize(i.dir);
                float s = _Seed * 13.37;

                // Deformamos un poco la posición para que las grietas no sean rectas perfectas
                float3 w = float3(noise3(p * 4.0 + s),
                                  noise3(p * 4.0 + 31.4 + s),
                                  noise3(p * 4.0 + 71.9 + s)) - 0.5;
                float3 pw = p + w * _Warp;

                // R: grietas de superficie (principales + finas)
                float k1, k2, k3;
                float e1 = voronoiEdge(pw * _CrackScale + s, k1);
                float crack = (1.0 - smoothstep(0.0, _CrackWidth, e1)) * step(k1, _CrackKeep);

                float e3 = voronoiEdge(pw * _CrackScale * 2.7 + s + 9.1, k3);
                float hair = (1.0 - smoothstep(0.0, _CrackWidth * 0.5, e3)) * step(k3, _CrackKeep * 0.5) * _HairlineAmount;
                crack = saturate(max(crack, hair));

                // G: grietas profundas, más anchas y suaves
                float e2 = voronoiEdge(pw * _DeepScale + s + 50.0, k2);
                float deep = (1.0 - smoothstep(0.0, _DeepWidth, e2)) * step(k2, _DeepKeep);
                deep *= deep;

                // B: turbidez (máx. 0.75) y cristalitos puntuales (llegan a 1.0)
                float cloud = fbm(p * _CloudScale + s) * 0.75;
                float3 sp = p * 90.0 + s;
                float hh = hash31(floor(sp));
                float dd = length(frac(sp) - 0.5);
                float fleck = step(0.985, hh) * (1.0 - smoothstep(0.0, 0.3, dd));
                cloud = max(cloud, fleck);

                return fixed4(crack, deep, saturate(cloud), 1);
            }
            ENDCG
        }
    }
}
