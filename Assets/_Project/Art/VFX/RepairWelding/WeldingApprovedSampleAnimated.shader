Shader "Bellerophon/Repair/WeldingApprovedSampleAnimated"
{
    Properties
    {
        [PerRendererData] _MainTex ("Approved Welding Sample", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _LayerMode ("Layer Mode", Float) = 1
        _PhaseOffset ("Phase Offset", Float) = 0
        _LoopSeconds ("Loop Seconds", Float) = 0.72
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "CanUseSpriteAtlas"="True"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "ApprovedWeldingSample"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _LayerMode;
                float _PhaseOffset;
                float _LoopSeconds;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            half4 SampleApproved(float2 uv)
            {
                if (any(uv < 0.0) || any(uv > 1.0))
                {
                    return half4(0, 0, 0, 0);
                }
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
            }

            float OrangeMask(half4 sampleColor, float2 uv)
            {
                float warmOverBlue = sampleColor.r - sampleColor.b * 1.18;
                float warmOverGreen = sampleColor.r - sampleColor.g * 0.94;
                float chroma = smoothstep(0.035, 0.24, warmOverBlue) *
                               smoothstep(0.018, 0.16, warmOverGreen);
                float lowerRegion = 1.0 - smoothstep(0.66, 0.76, uv.y);
                return saturate(chroma * lowerRegion * sampleColor.a * 2.0);
            }

            float ArcMask(half4 sampleColor, float2 uv, float orangeMask)
            {
                float cyan = smoothstep(0.01, 0.19,
                    sampleColor.b - sampleColor.r * 0.68);
                float brightness = sampleColor.r + sampleColor.g + sampleColor.b;
                float whiteCore = smoothstep(1.78, 2.62, brightness);
                float lower = smoothstep(0.34, 0.46, uv.y);
                float upper = 1.0 - smoothstep(0.69, 0.79, uv.y);
                return saturate(max(cyan, whiteCore) * lower * upper *
                    (1.0 - orangeMask) * sampleColor.a * 1.8);
            }

            float SmokeMask(
                half4 sampleColor,
                float2 uv,
                float orangeMask,
                float arcMask)
            {
                float upperRegion = smoothstep(0.49, 0.61, uv.y);
                return saturate(sampleColor.a * upperRegion *
                    (1.0 - orangeMask) * (1.0 - arcMask));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float loopSeconds = max(_LoopSeconds, 0.01);
                float phase = frac(_Time.y / loopSeconds + _PhaseOffset);
                float2 sourceUv = input.uv;
                float envelope = 1.0;
                if (_LayerMode < 1.5)
                {
                    sourceUv += float2(
                        sin(phase * 6.2831853) * 0.008,
                        phase * 0.045);
                    envelope = smoothstep(0.0, 0.12, phase) *
                               (1.0 - smoothstep(0.72, 1.0, phase));
                }
                else if (_LayerMode < 2.5)
                {
                    sourceUv += float2(
                        sin((phase + _PhaseOffset) * 6.2831853) * 0.004,
                        phase * 0.16);
                    envelope = smoothstep(0.0, 0.10, phase) *
                               (1.0 - smoothstep(0.72, 1.0, phase));
                }
                else if (_LayerMode > 2.5)
                {
                    sourceUv += float2(
                        sin((phase + _PhaseOffset) * 6.2831853) * 0.012,
                        -phase * 0.085);
                    envelope = smoothstep(0.0, 0.16, phase) *
                               (1.0 - smoothstep(0.76, 1.0, phase));
                }

                half4 sampleColor = SampleApproved(sourceUv);
                float spark = OrangeMask(sampleColor, sourceUv);
                float arc = ArcMask(sampleColor, sourceUv, spark);
                float smoke = SmokeMask(sampleColor, sourceUv, spark, arc);
                float mask;
                if (_LayerMode < 1.5)
                {
                    float pulseA = sin(phase * 31.4159265) * 0.5 + 0.5;
                    float pulseB = sin(phase * 69.1150384 + 0.7) * 0.5 + 0.5;
                    float intensity = lerp(0.58, 1.0,
                        saturate(pulseA * 0.72 + pulseB * 0.28));
                    mask = arc * intensity * envelope;
                }
                else if (_LayerMode < 2.5)
                {
                    mask = spark * envelope;
                }
                else
                {
                    mask = smoke * envelope;
                }

                half4 outputColor = sampleColor * input.color;
                outputColor.a *= saturate(mask);
                outputColor.rgb *= outputColor.a;
                return outputColor;
            }
            ENDHLSL
        }
    }
}
