Shader "Custom/ToonURP"
{
    Properties
    {
        [Header(Base)]
        [MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor ("Color", Color) = (1,1,1,1)

        [Header(Toon Lighting)]
        [Header(Shadow Layer 1 (mid))]
        _ShadowColor ("Shadow 1 Color", Color) = (0.62, 0.55, 0.78, 1)
        _Threshold ("Shadow 1 Threshold", Range(-1, 1)) = 0.1
        _Smooth ("Shadow 1 Smoothness", Range(0.001, 0.5)) = 0.02

        [Header(Shadow Layer 2 (core))]
        _Shadow2Color ("Shadow 2 Color", Color) = (0.38, 0.32, 0.55, 1)
        _Shadow2Threshold ("Shadow 2 Threshold", Range(-1, 1)) = -0.35
        _Shadow2Smooth ("Shadow 2 Smoothness", Range(0.001, 0.5)) = 0.02

        [Header(Cast Shadows)]
        _ShadowEdge ("Cast Shadow Smoothness", Range(0.001, 0.5)) = 0.03
        _CastToCore ("Cast Shadow Depth (0 = layer 1, 1 = layer 2)", Range(0, 1)) = 0.0
        _AmbientStrength ("Ambient in Shadow", Range(0, 1)) = 0.25

        [Header(Specular (independent of camera))]
        _SpecColor ("Specular Color", Color) = (1,1,1,1)
        _Glossiness ("Glossiness", Range(1, 256)) = 32
        _SpecThreshold ("Specular Threshold", Range(0, 1)) = 0.5
        _SpecSmooth ("Specular Smoothness", Range(0.001, 0.5)) = 0.02

        [Header(Rim)]
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimSize ("Rim Size", Range(0, 1)) = 0.5
        _RimSmooth ("Rim Smoothness", Range(0.001, 0.5)) = 0.05

        [Header(Emission)]
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,1)

        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width (px)", Range(0, 10)) = 1.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // Mismo CBUFFER para todos los passes -> compatible con SRP Batcher
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4  _BaseColor;
            half4  _ShadowColor;
            half   _Threshold;
            half   _Smooth;
            half4  _Shadow2Color;
            half   _Shadow2Threshold;
            half   _Shadow2Smooth;
            half   _CastToCore;
            half   _ShadowEdge;
            half   _AmbientStrength;
            half4  _SpecColor;
            half   _Glossiness;
            half   _SpecThreshold;
            half   _SpecSmooth;
            half4  _RimColor;
            half   _RimSize;
            half   _RimSmooth;
            half4  _EmissionColor;
            half4  _OutlineColor;
            half   _OutlineWidth;
        CBUFFER_END

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        ENDHLSL

        // ------------------------------------------------------------
        // Pass principal
        // ------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _FORWARD_PLUS _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                half   fogFactor   : TEXCOORD4;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   vn = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS  = vp.positionCS;
                OUT.positionWS  = vp.positionWS;
                OUT.normalWS    = vn.normalWS;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.shadowCoord = GetShadowCoord(vp);
                OUT.fogFactor   = ComputeFogFactor(vp.positionCS.z);
                return OUT;
            }

            // Capa 1: luz vs sombra media (terminador): 0 = sombra, 1 = luz
            half FormStep(half NdotL)
            {
                return smoothstep(_Threshold - _Smooth, _Threshold + _Smooth, NdotL);
            }

            // Capa 2: sombra media vs sombra de núcleo: 0 = núcleo, 1 = fuera del núcleo
            half CoreStep(half NdotL)
            {
                return smoothstep(_Shadow2Threshold - _Shadow2Smooth, _Shadow2Threshold + _Shadow2Smooth, NdotL);
            }

            // Sombra proyectada (shadow map) cuantizada: borde nítido en vez de degradado PCF
            half CastStep(half shadowAtten)
            {
                return smoothstep(0.5h - _ShadowEdge, 0.5h + _ShadowEdge, shadowAtten);
            }

            // Especular toon SIN dependencia de la cámara: solo luz + normal
            half ToonSpecular(half NdotL)
            {
                half s = pow(saturate(NdotL), _Glossiness);
                return smoothstep(_SpecThreshold, _SpecThreshold + _SpecSmooth, s);
            }

            // Acumula una luz adicional (puntual/spot/direccional extra)
            void AddToonLight(Light l, float3 normalWS, inout half3 extraDiffuse, inout half3 extraSpec)
            {
                half NdotL = dot(normalWS, l.direction);
                half t = FormStep(NdotL) * CastStep(l.shadowAttenuation) * l.distanceAttenuation;
                extraDiffuse += l.color * t;
                extraSpec    += ToonSpecular(NdotL) * t * _SpecColor.rgb * l.color;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 albedoTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half3 albedo = albedoTex.rgb * _BaseColor.rgb;

                float3 normalWS  = normalize(IN.normalWS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);

                // Con screen-space shadows la coord viene del vértice; si no, por píxel (cascadas correctas)
                float4 shadowCoord = IN.shadowCoord;
                #if !defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                #endif

                half4 shadowMask = half4(1, 1, 1, 1);

                // ---- Luz principal ----
                Light mainLight = GetMainLight(shadowCoord, IN.positionWS, shadowMask);
                half NdotL = dot(normalWS, mainLight.direction);

                half form = FormStep(NdotL);
                half cast = CastStep(mainLight.shadowAttenuation);
                half lit  = form * cast;                       // 1 = zona iluminada

                // Núcleo: la sombra proyectada puede empujar hacia la capa 2 según _CastToCore
                half core = CoreStep(NdotL) * lerp(1.0h, cast, _CastToCore);

                // Capas: luz (1) -> sombra 1 (media) -> sombra 2 (núcleo)
                half3 shadeTint = lerp(_Shadow2Color.rgb, _ShadowColor.rgb, core);
                half3 lighting  = lerp(shadeTint, half3(1, 1, 1), lit) * mainLight.color;

                // Un poco de ambiente solo dentro de la sombra (evita sombras negras sin sobreexponer la luz)
                half3 ambient = SampleSH(normalWS) * _AmbientStrength;
                lighting += ambient * shadeTint * (1.0h - lit);

                // Especular: solo donde hay luz, no depende de la cámara
                half3 specular = ToonSpecular(NdotL) * lit * _SpecColor.rgb * mainLight.color;

                // ---- Luces adicionales ----
                half3 extraDiffuse = 0;
                half3 extraSpec = 0;

                #if defined(_ADDITIONAL_LIGHTS)
                    InputData inputData = (InputData)0;
                    inputData.positionWS = IN.positionWS;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                    uint pixelLightCount = GetAdditionalLightsCount();

                    // Forward+ / Cluster: las direccionales extra van aparte del bucle por clusters
                    #if USE_FORWARD_PLUS || USE_CLUSTER_LIGHT_LOOP
                    [loop] for (uint dirIndex = 0; dirIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); dirIndex++)
                    {
                        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
                        Light l = GetAdditionalLight(dirIndex, IN.positionWS, shadowMask);
                        AddToonLight(l, normalWS, extraDiffuse, extraSpec);
                    }
                    #endif

                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light l = GetAdditionalLight(lightIndex, IN.positionWS, shadowMask);
                        AddToonLight(l, normalWS, extraDiffuse, extraSpec);
                    LIGHT_LOOP_END
                #endif

                // ---- Rim (solo en el lado iluminado) ----
                half rim = 1.0h - saturate(dot(normalWS, viewDirWS));
                rim = smoothstep(1.0h - _RimSize - _RimSmooth, 1.0h - _RimSize + _RimSmooth, rim);
                rim *= saturate(NdotL * 0.5h + 0.5h);
                half3 rimCol = rim * _RimColor.rgb * mainLight.color;

                // ---- Composición ----
                half3 color = albedo * (lighting + extraDiffuse);
                color += specular + extraSpec + rimCol;
                color += _EmissionColor.rgb;

                color = MixFog(color, IN.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------
        // Outline (inverted hull, grosor constante en píxeles)
        // ------------------------------------------------------------
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vertOutline
            #pragma fragment fragOutline
            #pragma multi_compile_fog

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half   fogFactor  : TEXCOORD0;
            };

            Varyings vertOutline(Attributes IN)
            {
                Varyings OUT;
                float4 posCS = TransformObjectToHClip(IN.positionOS.xyz);

                // Dirección de la normal en pantalla, corrigiendo el aspect ratio
                float3 normalVS = TransformWorldToViewDir(TransformObjectToWorldNormal(IN.normalOS));
                float2 n = mul((float3x3)UNITY_MATRIX_P, normalVS).xy;
                n.x *= _ScreenParams.x / _ScreenParams.y;
                float2 dir = n / max(length(n), 1e-5);

                // Offset en clip space -> mismo grosor en píxeles a cualquier distancia
                posCS.xy += dir * (_OutlineWidth * 2.0 / _ScreenParams.xy) * posCS.w;

                // Grosor 0 -> triángulos degenerados (se descarta el pass entero)
                if (_OutlineWidth < 0.01)
                    posCS = float4(0, 0, 0, 0);

                OUT.positionCS = posCS;
                OUT.fogFactor = ComputeFogFactor(posCS.z);
                return OUT;
            }

            half4 fragOutline(Varyings IN) : SV_Target
            {
                half3 col = MixFog(_OutlineColor.rgb, IN.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------
        // ShadowCaster
        // ------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vertShadow(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                OUT.positionCS = positionCS;
                return OUT;
            }

            half4 fragShadow(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------
        // DepthOnly
        // ------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            Varyings vertDepth(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half fragDepth(Varyings IN) : SV_Target
            {
                return IN.positionCS.z;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------
        // DepthNormals (SSAO, decals, etc.)
        // ------------------------------------------------------------
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vertDN
            #pragma fragment fragDN
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_fragment _ _WRITE_RENDERING_LAYERS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
            };

            Varyings vertDN(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            void fragDN(Varyings IN,
                        out half4 outNormalWS : SV_Target0
                        #ifdef _WRITE_RENDERING_LAYERS
                        , out float4 outRenderingLayers : SV_Target1
                        #endif
                        )
            {
                float3 normalWS = NormalizeNormalPerPixel(IN.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                    float2 remapped = saturate(octNormalWS * 0.5 + 0.5);
                    outNormalWS = half4(PackFloat2To888(remapped), 0.0);
                #else
                    outNormalWS = half4(normalWS, 0.0);
                #endif

                #ifdef _WRITE_RENDERING_LAYERS
                    outRenderingLayers = float4(EncodeMeshRenderingLayer(GetMeshRenderingLayer()), 0, 0, 0);
                #endif
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
