Shader "Bellerophon/Revolution/ApprovedAppearance"
{
    Properties
    {
        [MainTexture] _BaseMap("Approved Direct Crop", 2D) = "white" {}
        _DetailMap("Approved Wear Direct Crop", 2D) = "white" {}
        _SolidColor("Solid Color", Color) = (1, 1, 1, 1)
        _UseTexture("Use Texture", Float) = 1
        _UseDetail("Use Wear Detail", Float) = 0
        _DetailMix("Wear Mix", Range(0, 1)) = 0.35
        _Metallic("Metallic", Range(0, 1)) = 0.5
        _Roughness("Roughness", Range(0, 1)) = 0.5
        _BumpStrength("Bump Strength", Range(0, 1)) = 0.1
        _BumpDistance("Bump Distance", Range(0, 0.1)) = 0.025
        _EmissionStrength("Emission Strength", Range(0, 8)) = 0
        _BoundsMin("Generated Bounds Min", Vector) = (0, 0, 0, 0)
        _BoundsSize("Generated Bounds Size", Vector) = (1, 1, 1, 0)
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
            Name "ApprovedAppearanceForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            TEXTURE2D(_DetailMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _SolidColor;
                half _UseTexture;
                half _UseDetail;
                half _DetailMix;
                half _Metallic;
                half _Roughness;
                half _BumpStrength;
                half _BumpDistance;
                half _EmissionStrength;
                float4 _BoundsMin;
                float4 _BoundsSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                half3 normalOS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half3 normalWS : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(input.normalOS);
                output.positionCS = positionInputs.positionCS;
                output.positionOS = input.positionOS.xyz;
                output.normalOS = normalize(input.normalOS);
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.fogFactor =
                    ComputeFogFactor(positionInputs.positionCS.z);
                output.shadowCoord = GetShadowCoord(positionInputs);
                return output;
            }

            half3 BoxWeights(half3 normalOS, half projectionBlend)
            {
                half3 weights = abs(normalOS);
                half threshold = saturate(1.0h - projectionBlend);
                weights = saturate(
                    (weights - threshold) /
                    max(projectionBlend, 0.0001h));
                half total = weights.x + weights.y + weights.z;
                if (total < 0.0001h)
                {
                    weights = abs(normalOS);
                    total = weights.x + weights.y + weights.z;
                }

                return weights / max(total, 0.0001h);
            }

            half4 SampleBaseBox(float3 generated, half3 normalOS)
            {
                half3 weights = BoxWeights(normalOS, 0.12h);
                half4 xProjection = SAMPLE_TEXTURE2D(
                    _BaseMap, sampler_LinearClamp, generated.zy);
                half4 yProjection = SAMPLE_TEXTURE2D(
                    _BaseMap, sampler_LinearClamp, generated.xz);
                half4 zProjection = SAMPLE_TEXTURE2D(
                    _BaseMap, sampler_LinearClamp, generated.xy);
                return xProjection * weights.x +
                       yProjection * weights.y +
                       zProjection * weights.z;
            }

            half4 SampleDetailBox(float3 generated, half3 normalOS)
            {
                half3 weights = BoxWeights(normalOS, 0.18h);
                float3 detailCoordinates = generated * 3.0;
                half4 xProjection = SAMPLE_TEXTURE2D(
                    _DetailMap, sampler_LinearRepeat, detailCoordinates.zy);
                half4 yProjection = SAMPLE_TEXTURE2D(
                    _DetailMap, sampler_LinearRepeat, detailCoordinates.xz);
                half4 zProjection = SAMPLE_TEXTURE2D(
                    _DetailMap, sampler_LinearRepeat, detailCoordinates.xy);
                return xProjection * weights.x +
                       yProjection * weights.y +
                       zProjection * weights.z;
            }

            half WearRoughness(half wearLuminance)
            {
                half ramp = saturate(
                    (wearLuminance - 0.20h) / 0.60h);
                return lerp(0.66h, 0.42h, ramp);
            }

            half3 PerturbFromWear(
                half3 normalWS,
                float3 positionWS,
                half wearLuminance)
            {
                float3 positionDx = ddx(positionWS);
                float3 positionDy = ddy(positionWS);
                half heightDx = ddx(wearLuminance);
                half heightDy = ddy(wearLuminance);
                float3 surfaceGradient =
                    heightDx * cross(normalWS, positionDy) +
                    heightDy * cross(positionDx, normalWS);
                return normalize(
                    normalWS -
                    surfaceGradient *
                    (_BumpStrength * _BumpDistance));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 generated = saturate(
                    (input.positionOS - _BoundsMin.xyz) /
                    max(_BoundsSize.xyz, float3(0.00001, 0.00001, 0.00001)));
                generated.x = abs(2.0 * generated.x - 1.0);

                half4 baseSample = SampleBaseBox(
                    generated,
                    normalize(input.normalOS));
                half3 albedo = lerp(
                    _SolidColor.rgb,
                    baseSample.rgb,
                    saturate(_UseTexture));
                half roughness = _Roughness;
                half3 normalWS = normalize(input.normalWS);

                if (_UseDetail > 0.5h)
                {
                    half4 wear = SampleDetailBox(
                        generated,
                        normalize(input.normalOS));
                    albedo = lerp(
                        baseSample.rgb,
                        wear.rgb,
                        _DetailMix);
                    half wearLuminance = dot(
                        wear.rgb,
                        half3(0.2126h, 0.7152h, 0.0722h));
                    roughness = WearRoughness(wearLuminance);
                    normalWS = PerturbFromWear(
                        normalWS,
                        input.positionWS,
                        wearLuminance);
                }

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = NormalizeNormalPerPixel(normalWS);
                inputData.viewDirectionWS =
                    GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = input.shadowCoord;
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting =
                    VertexLighting(input.positionWS, inputData.normalWS);
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV =
                    GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.smoothness = saturate(1.0h - roughness);
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.occlusion = 1;
                surfaceData.emission =
                    albedo * _EmissionStrength;
                surfaceData.alpha = 1;
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 0;

                half4 color =
                    UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, input.fogFactor);
                color.a = 1;
                return color;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }

    FallBack "Universal Render Pipeline/Lit"
}
