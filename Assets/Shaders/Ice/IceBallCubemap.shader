// Skin de hielo para URP. Lee el cubemap horneado con Custom/IceBake:
//   R = grietas de superficie, G = grietas profundas, B = turbidez + cristalitos.
// 3 lecturas de cubemap por píxel, sin ruido en tiempo real. Opaca (barata en móvil).
Shader "Custom/IceBallCubemap"
{
    Properties
    {
        [NoScaleOffset] _Cube ("Hielo (Cubemap horneado)", Cube) = "" {}
        _ColorDeep ("Azul profundo", Color) = (0.03, 0.2, 0.5, 1)
        _ColorLight ("Azul claro", Color) = (0.6, 0.88, 1.0, 1)
        _CrackColor ("Grietas superficie", Color) = (0.95, 1.0, 1.0, 1)
        _DeepCrackColor ("Grietas profundas", Color) = (0.75, 0.95, 1.0, 1)
        _DeepStrength ("Intensidad grietas profundas", Range(0, 1)) = 0.6
        _Depth ("Profundidad falsa", Range(0, 0.6)) = 0.25
        _Cloud ("Turbidez lechosa", Range(0, 1)) = 0.6
        _Glint ("Destellos", Range(0, 3)) = 1.5
        _LightDir ("Dirección luz (mundo)", Vector) = (0.5, 0.8, 0.3, 0)
        _SpecColor ("Color brillo", Color) = (1, 1, 1, 1)
        _SpecPower ("Dureza brillo", Range(4, 256)) = 80
        _RimColor ("Color borde", Color) = (0.7, 0.93, 1.0, 1)
        _RimPower ("Fuerza borde", Range(0.5, 8)) = 2.5
        _Speed ("Velocidad de giro", Float) = 0
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
                half4 _ColorDeep;
                half4 _ColorLight;
                half4 _CrackColor;
                half4 _DeepCrackColor;
                half4 _SpecColor;
                half4 _RimColor;
                float4 _LightDir;
                half _DeepStrength;
                half _Depth;
                half _Cloud;
                half _Glint;
                half _SpecPower;
                half _RimPower;
                half _Speed;
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
                float3 viewOS     : TEXCOORD1;
                half3  normalWS   : TEXCOORD2;
                half3  viewDirWS  : TEXCOORD3;
            };

            float3 RotY(float3 p, float s, float c)
            {
                return float3(c * p.x - s * p.z, p.y, s * p.x + c * p.z);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.normalWS = (half3)TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = (half3)(GetCameraPositionWS() - posWS);

                float t = _Time.y * _Speed;
                float s = sin(t), c = cos(t);
                float3 viewOS = TransformWorldToObject(GetCameraPositionWS()) - IN.positionOS.xyz;
                OUT.dir = RotY(IN.positionOS.xyz, s, c);
                OUT.viewOS = RotY(viewOS, s, c);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 p = normalize(IN.dir);
                float3 vOS = normalize(IN.viewOS);
                half3 N = normalize(IN.normalWS);
                half3 V = normalize(IN.viewDirWS);

                // Tres lecturas: superficie y dos capas profundas desplazadas según la vista
                half4 s1 = SAMPLE_TEXTURECUBE(_Cube, sampler_Cube, p);
                half4 s2 = SAMPLE_TEXTURECUBE(_Cube, sampler_Cube, normalize(p - vOS * _Depth));
                half4 s3 = SAMPLE_TEXTURECUBE(_Cube, sampler_Cube, normalize(p - vOS * _Depth * 2.2));

                // Cuerpo: más profundo en el centro, más claro hacia el borde
                half ndv = saturate(dot(N, V));
                half fres = pow(1.0h - ndv, 3.0h);
                half3 col = lerp(_ColorDeep.rgb, _ColorLight.rgb, 0.2h + 0.8h * fres);

                // Turbidez lechosa del interior
                half cloud = smoothstep(0.3h, 0.7h, s1.b) * _Cloud;
                col = lerp(col, _ColorLight.rgb * 1.1h, cloud * 0.55h);

                // Grietas profundas: claras (dispersan luz), con dos capas a distinta profundidad
                half deep = saturate(s2.g * 0.8h + s3.g * 0.45h);
                col = lerp(col, _DeepCrackColor.rgb, deep * _DeepStrength);

                // Grietas de superficie: blancas y nítidas
                col = lerp(col, _CrackColor.rgb, s1.r);
                col += _CrackColor.rgb * s1.r * 0.15h;

                // Destellos: cristalitos que centellean
                half glintMask = smoothstep(0.85h, 0.98h, s1.b);
                float phase = dot(p, float3(127.1, 311.7, 74.7));
                half tw = saturate((half)sin(_Time.y * 2.5 + phase));
                tw = tw * tw * tw;
                col += glintMask * tw * _Glint;

                // Brillo especular duro + un halo suave
                half3 L = normalize((half3)_LightDir.xyz);
                half3 H = normalize(L + V);
                half nh = saturate(dot(N, H));
                col += _SpecColor.rgb * (pow(nh, _SpecPower) + 0.25h * pow(nh, _SpecPower * 0.15h));

                // Borde
                col += _RimColor.rgb * pow(1.0h - ndv, _RimPower) * 0.6h;

                return half4(col, 1.0h);
            }
            ENDHLSL
        }
    }
}
