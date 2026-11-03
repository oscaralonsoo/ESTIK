Shader "Custom/CartoonLavaMobile"
{
    Properties
    {
        _NoiseTex    ("Noise Texture (LavaNoise.png, sRGB off)", 2D) = "gray" {}

        [Header(Colors)]
        _ColorDark   ("Lava Dark", Color)   = (0.55, 0.05, 0.02, 1)
        _ColorMid    ("Lava Mid", Color)    = (1.0, 0.35, 0.05, 1)
        _ColorBright ("Lava Bright", Color) = (1.0, 0.85, 0.2, 1)
        _CrustColor  ("Crust Color", Color) = (0.12, 0.04, 0.03, 1)

        [Header(Pattern)]
        _Scale       ("Noise Scale", Float) = 2
        _Speed       ("Flow Speed (ciclos/seg, loop = 1/Speed seg)", Float) = 0.02
        _Steps       ("Color Steps (cartoon)", Range(2, 6)) = 3
        _CrustAmount ("Crust Amount", Range(0, 1)) = 0.3
        _CrustSoft   ("Crust Edge Softness", Range(0.001, 0.2)) = 0.03

        [Header(Foam)]
        _FoamColor      ("Foam Color", Color)      = (1.0, 0.85, 0.3, 1)
        _FoamCoreColor  ("Foam Core Color", Color) = (1.0, 1.0, 0.85, 1)
        _FoamWidth      ("Foam Width (world units)", Float) = 0.5
        _FoamCoreAmount ("Foam Core Amount", Range(0.3, 0.95)) = 0.65
        _FoamDistort    ("Foam Edge Distortion", Range(0, 0.4)) = 0.3
        _FoamFade       ("Foam Outer Fade (0 = corte duro)", Range(0.01, 1)) = 0.5
        _FoamSoft       ("Foam Core Edge Softness", Range(0.001, 0.1)) = 0.02

        [Header(Wobble)]
        _WobbleAmp   ("Vertex Wobble Amount (0 = off)", Float) = 0.05
        _WobbleFreq  ("Vertex Wobble Frequency", Float) = 2
        _WobbleSpeed ("Vertex Wobble Speed", Float) = 0.5

        _Emission    ("Brightness", Range(0.5, 3)) = 1.3
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                half4  _ColorDark;
                half4  _ColorMid;
                half4  _ColorBright;
                half4  _CrustColor;
                half4  _FoamColor;
                half4  _FoamCoreColor;
                float  _Scale;
                float  _Speed;
                half   _Steps;
                half   _CrustAmount;
                half   _CrustSoft;
                float  _FoamWidth;
                half   _FoamCoreAmount;
                half   _FoamDistort;
                half   _FoamSoft;
                half   _FoamFade;
                float  _WobbleAmp;
                float  _WobbleFreq;
                float  _WobbleSpeed;
                half   _Emission;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv         : TEXCOORD0;
                float  eyeDepth   : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 pos = IN.positionOS.xyz;

                float t = _Time.y * _WobbleSpeed;
                float wave = sin(pos.x * _WobbleFreq + t) * cos(pos.z * _WobbleFreq + t * 1.3);
                pos += IN.normalOS * wave * _WobbleAmp;

                float3 posWS = TransformObjectToWorld(pos);
                OUT.positionCS = TransformWorldToHClip(posWS);

                OUT.eyeDepth = -TransformWorldToView(posWS).z;

                float2 uv = IN.uv * _Scale;
                float  s  = frac(_Time.y * _Speed);
                OUT.uv.xy = uv + float2(s, 0.0);
                OUT.uv.zw = uv * 1.37 + float2(-s, s) + 0.5;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half n1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.uv.xy).r;
                half n2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.uv.zw).r;

                half n = saturate(((n1 + n2) * 0.5h - 0.5h) * 2.0h + 0.5h);

                half steps = max(_Steps, 2.0h);
                half band  = saturate(floor(n * steps) / (steps - 1.0h));

                half3 col = lerp(_ColorDark.rgb, _ColorMid.rgb, saturate(band * 2.0h));
                col = lerp(col, _ColorBright.rgb, saturate(band * 2.0h - 1.0h));

                half crust = 1.0h - smoothstep(_CrustAmount - _CrustSoft, _CrustAmount + _CrustSoft, n);
                col = lerp(col, _CrustColor.rgb, crust);

                col *= _Emission;

                float2 screenUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                float rawDepth  = SampleSceneDepth(screenUV);

                float sceneEye = (unity_OrthoParams.w < 0.5)
                    ? LinearEyeDepth(rawDepth, _ZBufferParams)
                    : LinearDepthToEyeDepth(rawDepth);

                float diff = max(sceneEye - IN.eyeDepth, 0.0);

                half mask = 1.0h - saturate(diff / max(_FoamWidth, 0.0001));

                half m = mask + (n - 0.5h) * _FoamDistort;

                half fadeStart = _FoamDistort * 0.5h + 0.001h;
                half foamOuter = smoothstep(fadeStart, fadeStart + _FoamFade, m);
                half foamCore  = smoothstep(_FoamCoreAmount - _FoamSoft, _FoamCoreAmount + _FoamSoft, m);

                col = lerp(col, _FoamColor.rgb, foamOuter);
                col = lerp(col, _FoamCoreColor.rgb, foamCore);

                return half4(col, 1.0h);
            }
            ENDHLSL
        }
    }
}
