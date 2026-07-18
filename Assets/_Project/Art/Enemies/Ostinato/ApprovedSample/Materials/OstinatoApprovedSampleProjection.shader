Shader "Bellerophon/Ostinato Approved Sample Projection"
{
    Properties
    {
        _FrontTex("Approved Front", 2D) = "white" {}
        _SideTex("Approved Side", 2D) = "white" {}
        _BackTex("Approved Back", 2D) = "white" {}
        _ChitinTex("Approved Insect Chitin", 2D) = "white" {}
        _FrontRect("Front Foreground Rect", Vector) = (0, 0, 1, 1)
        _SideRect("Side Foreground Rect", Vector) = (0, 0, 1, 1)
        _BackRect("Back Foreground Rect", Vector) = (0, 0, 1, 1)
        _BoundsMin("Model Bounds Min", Vector) = (0, 0, 0, 0)
        _BoundsSize("Model Bounds Size", Vector) = (1, 1, 1, 0)
        _ProjectionSharpness("View Projection Sharpness", Range(1, 32)) = 16
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ApprovedSampleProjection"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 screenPosition : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_FrontTex);
            SAMPLER(sampler_FrontTex);
            TEXTURE2D(_SideTex);
            SAMPLER(sampler_SideTex);
            TEXTURE2D(_BackTex);
            SAMPLER(sampler_BackTex);
            TEXTURE2D(_ChitinTex);
            SAMPLER(sampler_ChitinTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _FrontRect;
                float4 _SideRect;
                float4 _BackRect;
                float4 _BoundsMin;
                float4 _BoundsSize;
                float _ProjectionSharpness;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionOS = input.positionOS.xyz;
                output.positionWS = positions.positionWS;
                output.screenPosition = ComputeScreenPos(positions.positionCS);
                return output;
            }

            void AccumulateProjectedCorner(float3 cornerOS, inout float2 minimumUv, inout float2 maximumUv)
            {
                float4 cornerScreen = ComputeScreenPos(TransformObjectToHClip(cornerOS));
                float2 cornerUv = cornerScreen.xy / max(cornerScreen.w, 0.0001);
                minimumUv = min(minimumUv, cornerUv);
                maximumUv = max(maximumUv, cornerUv);
            }

            bool IsApprovedPaper(half3 color)
            {
                const half3 approvedPaperLinear = half3(0.8879, 0.8632, 0.7835);
                half3 difference = abs(color - approvedPaperLinear);
                return max(difference.r, max(difference.g, difference.b)) < 0.22;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 boundsMinimum = _BoundsMin.xyz;
                float3 boundsMaximum = _BoundsMin.xyz + _BoundsSize.xyz;
                float2 projectedMinimum = float2(10.0, 10.0);
                float2 projectedMaximum = float2(-10.0, -10.0);
                AccumulateProjectedCorner(float3(boundsMinimum.x, boundsMinimum.y, boundsMinimum.z), projectedMinimum, projectedMaximum);
                AccumulateProjectedCorner(float3(boundsMaximum.x, boundsMinimum.y, boundsMinimum.z), projectedMinimum, projectedMaximum);
                AccumulateProjectedCorner(float3(boundsMinimum.x, boundsMaximum.y, boundsMinimum.z), projectedMinimum, projectedMaximum);
                AccumulateProjectedCorner(float3(boundsMaximum.x, boundsMaximum.y, boundsMinimum.z), projectedMinimum, projectedMaximum);
                AccumulateProjectedCorner(float3(boundsMinimum.x, boundsMinimum.y, boundsMaximum.z), projectedMinimum, projectedMaximum);
                AccumulateProjectedCorner(float3(boundsMaximum.x, boundsMinimum.y, boundsMaximum.z), projectedMinimum, projectedMaximum);
                AccumulateProjectedCorner(float3(boundsMinimum.x, boundsMaximum.y, boundsMaximum.z), projectedMinimum, projectedMaximum);
                AccumulateProjectedCorner(float3(boundsMaximum.x, boundsMaximum.y, boundsMaximum.z), projectedMinimum, projectedMaximum);

                float2 screenUv = input.screenPosition.xy / max(input.screenPosition.w, 0.0001);
                float2 projectedSize = max(projectedMaximum - projectedMinimum, float2(0.0001, 0.0001));
                float2 approvedPosition = saturate((screenUv - projectedMinimum) / projectedSize);
                approvedPosition.y = 1.0 - approvedPosition.y;
                float3 cameraPositionOS = TransformWorldToObject(GetCameraPositionWS());
                float3 modelCenterOS = _BoundsMin.xyz + _BoundsSize.xyz * 0.5;
                float3 viewDirectionOS = SafeNormalize(cameraPositionOS - modelCenterOS);

                float2 frontUv = _FrontRect.xy + approvedPosition * _FrontRect.zw;
                float2 sideUv = _SideRect.xy + approvedPosition * _SideRect.zw;
                float2 backUv = _BackRect.xy + approvedPosition * _BackRect.zw;

                float2 chitinUv = frac(approvedPosition * 1.75);
                half4 chitinColor = SAMPLE_TEXTURE2D(_ChitinTex, sampler_ChitinTex, chitinUv);
                half4 frontColor = SAMPLE_TEXTURE2D(_FrontTex, sampler_FrontTex, frontUv);
                half4 sideColor = SAMPLE_TEXTURE2D(_SideTex, sampler_SideTex, sideUv);
                half4 backColor = SAMPLE_TEXTURE2D(_BackTex, sampler_BackTex, backUv);
                if (IsApprovedPaper(frontColor.rgb)) frontColor = chitinColor;
                if (IsApprovedPaper(sideColor.rgb)) sideColor = chitinColor;
                if (IsApprovedPaper(backColor.rgb)) backColor = chitinColor;
                half4 depthColor = viewDirectionOS.z >= 0.0 ? frontColor : backColor;

                float depthWeight = pow(max(abs(viewDirectionOS.z), 0.0001), _ProjectionSharpness);
                float sideWeight = pow(max(abs(viewDirectionOS.x), 0.0001), _ProjectionSharpness);
                float weightSum = max(depthWeight + sideWeight, 0.0001);
                return (depthColor * depthWeight + sideColor * sideWeight) / weightSum;
            }
            ENDHLSL
        }
    }
}
