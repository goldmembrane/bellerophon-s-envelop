Shader "Bellerophon/Items/ElectricMineArmedBlink"
{
    Properties
    {
        [HDR] _GlowColor ("Approved Red Glow", Color) = (2.5, 0.005, 0.002, 1)
        _BoundsCenter ("Mesh Bounds Center", Vector) = (0, 0, 0, 0)
        _DiscAxis ("Disc Normal Axis", Vector) = (0, 0, 1, 0)
        _DiscRadius ("Disc Radius", Float) = 1
        _ButtonRadiusRatio ("Button Radius Ratio", Range(0, 1)) = 0.26
        _EdgeFeatherRatio ("Edge Feather Ratio", Range(0, 0.2)) = 0.025
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+20"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "ApprovedArmedBlink"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalOS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _GlowColor;
                float4 _BoundsCenter;
                float4 _DiscAxis;
                float _DiscRadius;
                float _ButtonRadiusRatio;
                float _EdgeFeatherRatio;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                #if UNITY_REVERSED_Z
                    output.positionHCS.z += 0.00075 * output.positionHCS.w;
                #else
                    output.positionHCS.z -= 0.00075 * output.positionHCS.w;
                #endif
                output.positionOS = input.positionOS.xyz;
                output.normalOS = input.normalOS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 axis = normalize(_DiscAxis.xyz);
                float3 offsetFromCenter = input.positionOS - _BoundsCenter.xyz;
                float3 radialVector = offsetFromCenter -
                    axis * dot(offsetFromCenter, axis);
                float radialRatio = length(radialVector) / max(_DiscRadius, 0.00001);
                float radialMask = 1.0 - smoothstep(
                    _ButtonRadiusRatio,
                    _ButtonRadiusRatio + _EdgeFeatherRatio,
                    radialRatio);
                float mask = radialMask;
                clip(mask - 0.001);
                return half4(_GlowColor.rgb * mask, mask);
            }
            ENDHLSL
        }
    }
}
