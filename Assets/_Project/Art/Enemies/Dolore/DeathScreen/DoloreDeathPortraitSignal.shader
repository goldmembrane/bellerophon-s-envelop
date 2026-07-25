Shader "Bellerophon/Dolore Death Portrait Signal"
{
    Properties
    {
        _BaseMap("Portrait", 2D) = "white" {}
        _BaseColor("Portrait Tint", Color) = (1, 1, 1, 1)
        _SignalPhase("Signal Phase", Float) = 0
        _NoiseScale("Noise Scale", Float) = 190
        _NoiseSpeed("Noise Speed", Float) = 26
        _ScanlineStrength("Scanline Strength", Range(0, 1)) = 0.24
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("Depth Test", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Overlay"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite Off
            ZTest [_ZTest]
            Offset -1, -1
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _SignalPhase;
                float _NoiseScale;
                float _NoiseSpeed;
                float _ScanlineStrength;
                float _ZTest;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 signalUv : TEXCOORD1;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 signalUv : TEXCOORD1;
                float signalMask : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.signalUv = input.signalUv;
                output.signalMask = input.color.a;
                return output;
            }

            float Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 456.21));
                value += dot(value, value + 45.32);
                return frac(value.x * value.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 portrait = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                if (_SignalPhase < 0.5)
                {
                    clip(-1.0);
                    return half4(portrait.rgb, 0.0);
                }

                clip(input.signalMask - 0.45);

                if (_SignalPhase >= 1.5)
                {
                    return half4(0.0, 0.0, 0.0, 1.0);
                }

                float frame = floor(_Time.y * _NoiseSpeed);
                float2 cell = floor(input.signalUv * _NoiseScale);
                float fineNoise = Hash21(cell + float2(frame * 17.0, frame * 31.0));
                float coarseNoise = Hash21(floor(cell * 0.13) + float2(frame * 7.0, frame * 11.0));
                float scanline = 0.5 + 0.5 * sin((input.signalUv.y * 900.0) + frame * 0.8);
                float tearCenter = frac(frame * 0.071);
                float tear = 1.0 - saturate(abs(input.signalUv.y - tearCenter) * 170.0);
                float signal = saturate(
                    0.20 + fineNoise * 0.72 + (coarseNoise - 0.5) * 0.28 +
                    scanline * _ScanlineStrength + tear * 0.40);
                return half4(signal.xxx, 1.0);
            }
            ENDHLSL
        }
    }
}
