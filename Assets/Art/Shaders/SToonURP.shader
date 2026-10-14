Shader "Universal Render Pipeline/Simple Toon/SToon Default"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        [Header(Colorize)][Space(5)]
        _Color ("Color", COLOR) = (1,1,1,1)

        [HideInInspector] _ColIntense ("Intensity", Range(0,3)) = 1
        [HideInInspector] _ColBright ("Brightness", Range(-1,1)) = 0
        _AmbientCol ("Ambient", Range(0,1)) = 0

        [Header(Detail)][Space(5)]
        [Toggle] _Segmented ("Segmented", Float) = 1
        _Steps ("Steps", Range(1,25)) = 3
        _StpSmooth ("Smoothness", Range(0,1)) = 0
        _Offset ("Lit Offset", Range(-1,1.1)) = 0

        [Header(Light)][Space(5)]
        [Toggle] _Clipped ("Clipped", Float) = 0
        _MinLight ("Min Light", Range(0,1)) = 0
        _MaxLight ("Max Light", Range(0,1)) = 1
        _Lumin ("Luminocity", Range(0,2)) = 0

        [Header(Shine)][Space(5)]
        [HDR] _ShnColor ("Color", COLOR) = (1,1,0,1)
        [Toggle] _ShnOverlap ("Overlap", Float) = 0

        _ShnIntense ("Intensity", Range(0,1)) = 0
        _ShnRange ("Range", Range(0,1)) = 0.15
        _ShnSmooth ("Smoothness", Range(0,1)) = 0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Geometry" 
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // 1. DEFINICIÓN DE PROPIEDADES (CBUFFER PARA SRP BATCHER)
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _ColIntense;
                float _ColBright;
                float _AmbientCol;

                float _Segmented;
                float _Steps;
                float _StpSmooth;
                float _Offset;

                float _Clipped;
                float _MinLight;
                float _MaxLight;
                float _Lumin;

                float4 _ShnColor;
                float _ShnOverlap;
                float _ShnIntense;
                float _ShnRange;
                float _ShnSmooth;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // 2. RECONSTRUCCIÓN DE "STFunctions"
            float invLerp01(float a, float b, float v) { return saturate((v - a) / (b - a)); }
            float smoothlerp(float a, float b, float t) { return lerp(a, b, t); }
            float posz(float v) { return max(0.0, v); }
            float rev(float v) { return 1.0 - v; }
            float4 ColorBlend(float4 c1, float4 c2, float blend) { return lerp(c1, c2, blend); }

            // 3. RECONSTRUCCIÓN DE "STCore"
            static float4 _DarkColor = float4(0, 0, 0, 1);
            static float _MaxAtten = 1.0;

            float Toon(float NdotL, float atten)
            {
                float offset = clamp(_Offset, -1.0, 1.0);
                float delta = max(0.0001, _MaxLight - _MinLight); // Evitar división por 0

                // intense
                float ints_pls = NdotL + offset;
                float ints_max = 1.0 + offset;
                float intense = saturate(ints_pls / ints_max);

                // lit
                float steps = _Segmented ? _Steps : 1.0;
                float stepVal = 1.0 / floor(steps);
                int lit_num = ceil(intense / stepVal);
                float lit = lit_num * stepVal;

                // smooth
                float stpSmooth = _Segmented ? _StpSmooth : 1.0;
                float reduce_v = _Offset - 1.0;
                float reduce_res = 1.0 - saturate(reduce_v / 0.1); 
                float reduce = (lit_num == 1) ? reduce_res : 1.0;

                float smth_start = lit - stepVal;
                float smth_end = smth_start + stepVal * stpSmooth;

                float smth_lrp = invLerp01(smth_end, smth_start, intense);
                float smth_stp = smoothstep(smth_end, smth_start, intense);

                float smooth_v = smoothlerp(smth_stp, smth_lrp, stpSmooth);
                float smooth_val = saturate(lit - smooth_v * reduce * stepVal);

                // shadow
                float atten_inv = clamp(atten, 1.0 - _MaxAtten, 1.0);
                float dimLit = smooth_val * atten_inv;
                float dim_dlt = dimLit - _MinLight;

                // luminocity
                float lumLight = _MaxLight + _Lumin;
                float lum_dlt = lumLight - _MinLight;

                // clipped
                float litd_clmp = saturate(dim_dlt);
                float clip_cf = litd_clmp / delta;

                float clip_uncl = _MinLight + clip_cf * lum_dlt;
                float clip_v = clamp(clip_uncl, _MinLight, lumLight);

                // relative limits
                float lerp_v = lum_dlt * dimLit;
                float relate_v = _MinLight + lerp_v;

                return _Clipped ? clip_v : relate_v;
            }

            void PostShine(inout float4 col, float NdotL, float atten)
            {
                float pos = abs(NdotL - 1.0);
                float len = _ShnRange * 2.0;

                float smth_inv = 1.0 - _ShnSmooth;
                float smth_end = len * smth_inv;

                float shine = posz(len - pos);
                float smooth_val = smoothstep(len, smth_end, pos);
                float dim = 1.0 - _MaxAtten * rev(atten) * rev(_ShnOverlap);

                float blend = _ShnIntense * shine * smooth_val * dim;
                col = ColorBlend(col, _ShnColor, blend);
            }

            float4 PostEffects(float4 col, float toon, float atten, float NdotL)
            {
                PostShine(col, NdotL, atten);
                return col;
            }

            // 4. VERTEX & FRAGMENT SHADER DE URP
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : NORMAL;
                float3 viewDirWS : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(output.positionWS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);

                // URP Lighting
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                
                float3 lightDir = normalize(mainLight.direction);
                float NdotL = dot(normal, lightDir);
                float atten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;

                // Toon Calculation
                float toon = Toon(NdotL, atten);

                // Colors
                float4 lightColor = float4(mainLight.color, 1.0);
                float4 litcol = ColorBlend(_Color, lightColor, _AmbientCol);
                
                float4 texData = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float4 texcol = texData * litcol * _ColIntense + _ColBright;

                float4 blendCol = ColorBlend(_DarkColor, texcol, toon);
                
                // Post Effects (Shine)
                float4 postCol = PostEffects(blendCol, toon, atten, NdotL);
                postCol.a = 1.0;

                return postCol;
            }
            ENDHLSL
        }

        // 5. SHADOWCASTER PASS (Para proyectar sombras)
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, GetMainLight().direction));
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}