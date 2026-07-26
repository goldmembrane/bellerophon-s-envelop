Shader "Bellerophon/Resistance/ApprovedAppearance"
{
    Properties
    {
        _SilverTex ("Worn Silver", 2D) = "white" {}
        _DarkTex ("Dark Mechanics", 2D) = "black" {}
        _CyanTex ("Cyan Emission", 2D) = "cyan" {}
        _BronzeTex ("Bronze Accents", 2D) = "white" {}
        _OliveTex ("Olive Bandana", 2D) = "white" {}
        _RoughnessTex ("Surface Roughness", 2D) = "gray" {}
        _BumpTex ("Surface Micro Bump", 2D) = "gray" {}
        _FaceMaterialMask ("Approved Face Material Map", 2D) = "black" {}
        _ApprovedAlbedoTex ("Approved Blender Albedo", 2D) = "white" {}
        _ApprovedEmissionTex ("Approved Blender Emission", 2D) = "black" {}
        _TriangleAtlasAlbedo ("Approved Triangle Albedo Atlas", 2D) = "white" {}
        _TriangleAtlasEmission ("Approved Triangle Emission Atlas", 2D) = "black" {}
        _TriangleMapA ("Triangle Atlas Lookup A", 2D) = "black" {}
        _TriangleMapB ("Triangle Atlas Lookup B", 2D) = "black" {}
        _TriangleMapC ("Triangle Atlas Lookup C", 2D) = "black" {}
        _TriangleMaterialMap ("Approved Triangle Material Map", 2D) = "black" {}
        _TrianglePanelAtlas ("Approved Triangle Panel Atlas", 2D) = "black" {}
        _EmissionStrength ("Cyan Emission Strength", Range(0, 4)) = 0.65
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
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_SilverTex);
            SAMPLER(sampler_SilverTex);
            TEXTURE2D(_DarkTex);
            SAMPLER(sampler_DarkTex);
            TEXTURE2D(_CyanTex);
            SAMPLER(sampler_CyanTex);
            TEXTURE2D(_BronzeTex);
            SAMPLER(sampler_BronzeTex);
            TEXTURE2D(_OliveTex);
            SAMPLER(sampler_OliveTex);
            TEXTURE2D(_RoughnessTex);
            SAMPLER(sampler_RoughnessTex);
            TEXTURE2D(_BumpTex);
            SAMPLER(sampler_BumpTex);
            TEXTURE2D(_FaceMaterialMask);
            SAMPLER(sampler_FaceMaterialMask);
            TEXTURE2D(_ApprovedAlbedoTex);
            SAMPLER(sampler_ApprovedAlbedoTex);
            TEXTURE2D(_ApprovedEmissionTex);
            SAMPLER(sampler_ApprovedEmissionTex);
            TEXTURE2D(_TriangleAtlasAlbedo);
            SAMPLER(sampler_TriangleAtlasAlbedo);
            TEXTURE2D(_TriangleAtlasEmission);
            SAMPLER(sampler_TriangleAtlasEmission);
            TEXTURE2D(_TriangleMapA);
            TEXTURE2D(_TriangleMapB);
            TEXTURE2D(_TriangleMapC);
            TEXTURE2D(_TriangleMaterialMap);
            TEXTURE2D(_TrianglePanelAtlas);
            SAMPLER(sampler_TrianglePanelAtlas);

            CBUFFER_START(UnityPerMaterial)
                float4 _SilverTex_ST;
                float _EmissionStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half3 normalOS : TEXCOORD3;
                float2 uv : TEXCOORD4;
                half fogFactor : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float RangeMask(float value, float minimum, float maximum)
            {
                return step(minimum, value) * step(value, maximum);
            }

            float RectMask(
                float x,
                float y,
                float centerX,
                float centerY,
                float halfWidth,
                float halfHeight)
            {
                return RangeMask(
                           x,
                           centerX - halfWidth,
                           centerX + halfWidth) *
                       RangeMask(
                           y,
                           centerY - halfHeight,
                           centerY + halfHeight);
            }

            float SlopedRectMask(
                float x,
                float y,
                float centerX,
                float centerY,
                float halfWidth,
                float halfHeight,
                float slope)
            {
                float tiltedX = x + (y - centerY) * slope;
                return RectMask(
                    tiltedX,
                    y,
                    centerX,
                    centerY,
                    halfWidth,
                    halfHeight);
            }

            float EllipseMask(
                float x,
                float y,
                float centerX,
                float centerY,
                float radiusX,
                float radiusY)
            {
                float2 normalized = float2(
                    (x - centerX) / radiusX,
                    (y - centerY) / radiusY);
                return step(dot(normalized, normalized), 1.0);
            }

            float3 TriangleBarycentric(
                float2 sampleUv,
                float2 cornerA,
                float2 cornerB,
                float2 cornerC)
            {
                float2 edge0 = cornerB - cornerA;
                float2 edge1 = cornerC - cornerA;
                float2 relative = sampleUv - cornerA;
                float denominator =
                    edge0.x * edge1.y -
                    edge1.x * edge0.y;
                denominator =
                    abs(denominator) < 0.0000001
                        ? 0.0000001
                        : denominator;
                float secondWeight =
                    (relative.x * edge1.y -
                     edge1.x * relative.y) /
                    denominator;
                float thirdWeight =
                    (edge0.x * relative.y -
                     relative.x * edge0.y) /
                    denominator;
                float3 weights = saturate(float3(
                    1.0 - secondWeight - thirdWeight,
                    secondWeight,
                    thirdWeight));
                return weights /
                    max(
                        weights.x + weights.y + weights.z,
                        0.000001);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(input.normalOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.positionOS = input.positionOS.xyz;
                output.normalWS = normalInputs.normalWS;
                output.normalOS = normalize(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _SilverTex);
                output.fogFactor =
                    ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 Frag(
                Varyings input,
                uint primitiveId : SV_PrimitiveID) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 positionOS = input.positionOS;
                float3 normalOS = normalize(input.normalOS);
                float horizontal = abs(positionOS.x);
                float vertical = positionOS.y;
                float blenderDepth = -positionOS.z;
                float front = step(0.16, normalOS.z);
                float panelFacing = step(-0.10, normalOS.z);

                half3 silver = SAMPLE_TEXTURE2D(
                    _SilverTex,
                    sampler_SilverTex,
                    input.uv).rgb;
                half3 dark = SAMPLE_TEXTURE2D(
                    _DarkTex,
                    sampler_DarkTex,
                    input.uv).rgb;
                half3 cyan = SAMPLE_TEXTURE2D(
                    _CyanTex,
                    sampler_CyanTex,
                    input.uv).rgb;
                half3 bronze = SAMPLE_TEXTURE2D(
                    _BronzeTex,
                    sampler_BronzeTex,
                    input.uv).rgb;
                half3 olive = SAMPLE_TEXTURE2D(
                    _OliveTex,
                    sampler_OliveTex,
                    input.uv).rgb;
                half roughness = SAMPLE_TEXTURE2D(
                    _RoughnessTex,
                    sampler_RoughnessTex,
                    input.uv).r;
                half micro = SAMPLE_TEXTURE2D(
                    _BumpTex,
                    sampler_BumpTex,
                    input.uv).r;
                half3 approvedAlbedo = SAMPLE_TEXTURE2D(
                    _ApprovedAlbedoTex,
                    sampler_ApprovedAlbedoTex,
                    input.uv).rgb;
                half3 approvedEmission = SAMPLE_TEXTURE2D(
                    _ApprovedEmissionTex,
                    sampler_ApprovedEmissionTex,
                    input.uv).rgb;
                half approvedEmissionMask = step(
                    0.001,
                    max(
                        approvedEmission.r,
                        max(
                            approvedEmission.g,
                            approvedEmission.b)));
                float4 triangleMapA = LOAD_TEXTURE2D(
                    _TriangleMapA,
                    int2(primitiveId, 0));
                float4 triangleMapB = LOAD_TEXTURE2D(
                    _TriangleMapB,
                    int2(primitiveId, 0));
                float4 triangleMapC = LOAD_TEXTURE2D(
                    _TriangleMapC,
                    int2(primitiveId, 0));
                float3 triangleBarycentric =
                    TriangleBarycentric(
                        input.uv,
                        triangleMapA.xy,
                        triangleMapA.zw,
                        triangleMapB.xy);
                float2 triangleAtlasUv =
                    triangleMapB.zw *
                        triangleBarycentric.x +
                    triangleMapC.xy *
                        triangleBarycentric.y +
                    triangleMapC.zw *
                        triangleBarycentric.z;
                half3 triangleAlbedo =
                    SAMPLE_TEXTURE2D_LOD(
                        _TriangleAtlasAlbedo,
                        sampler_TriangleAtlasAlbedo,
                        triangleAtlasUv,
                        0).rgb;
                half3 triangleEmission =
                    SAMPLE_TEXTURE2D_LOD(
                        _TriangleAtlasEmission,
                        sampler_TriangleAtlasEmission,
                        triangleAtlasUv,
                        0).rgb;
                half2 approvedPanelMasks =
                    SAMPLE_TEXTURE2D_LOD(
                        _TrianglePanelAtlas,
                        sampler_TrianglePanelAtlas,
                        triangleAtlasUv,
                        0).rg;
                approvedEmissionMask = step(
                    0.001,
                    max(
                        triangleEmission.r,
                        max(
                            triangleEmission.g,
                            triangleEmission.b)));

                float faceMaterialValue = LOAD_TEXTURE2D(
                    _TriangleMaterialMap,
                    int2(primitiveId, 0)).r;
                float darkMask = RangeMask(
                    faceMaterialValue,
                    0.125,
                    0.375);
                float bronzeMask = RangeMask(
                    faceMaterialValue,
                    0.3751,
                    0.625);
                float oliveMask = RangeMask(
                    faceMaterialValue,
                    0.6251,
                    0.875);

                float frameMask = 0.0;
                float cyanMask = 0.0;

                frameMask = max(
                    frameMask,
                    EllipseMask(
                        positionOS.x,
                        vertical,
                        -0.235,
                        1.425,
                        0.042,
                        0.040) * panelFacing);
                cyanMask = max(
                    cyanMask,
                    EllipseMask(
                        positionOS.x,
                        vertical,
                        -0.235,
                        1.425,
                        0.024,
                        0.022) * panelFacing);

                frameMask = max(
                    frameMask,
                    RectMask(
                        horizontal,
                        vertical,
                        0.115,
                        1.292,
                        0.040,
                        0.020) * front);
                cyanMask = max(
                    cyanMask,
                    RectMask(
                        horizontal,
                        vertical,
                        0.115,
                        1.292,
                        0.030,
                        0.010) * front);

                const float abdomenLevels[4] =
                {
                    1.205,
                    1.170,
                    1.135,
                    1.100
                };
                [unroll]
                for (int index = 0; index < 4; index++)
                {
                    frameMask = max(
                        frameMask,
                        RectMask(
                            positionOS.x,
                            vertical,
                            0.0,
                            abdomenLevels[index],
                            0.031,
                            0.012) * front);
                    cyanMask = max(
                        cyanMask,
                        RectMask(
                            positionOS.x,
                            vertical,
                            0.0,
                            abdomenLevels[index],
                            0.021,
                            0.005) * front);
                }

                float forearmFrame =
                    RectMask(
                        horizontal,
                        vertical,
                        0.325,
                        1.070,
                        0.075,
                        0.080) *
                    RangeMask(blenderDepth, -0.18, 0.02);
                float forearmCore =
                    RectMask(
                        horizontal,
                        vertical,
                        0.325,
                        1.070,
                        0.065,
                        0.055) *
                    RangeMask(blenderDepth, -0.18, -0.08);
                float nearForearmSide =
                    RangeMask(positionOS.x, -0.40, -0.25);
                float nearForearmFrame =
                    nearForearmSide *
                    RangeMask(vertical, 0.99, 1.15) *
                    RangeMask(blenderDepth, 0.02, 0.16);
                float nearForearmCore =
                    nearForearmSide *
                    RangeMask(vertical, 1.015, 1.125) *
                    RangeMask(blenderDepth, 0.08, 0.16);
                frameMask = max(
                    frameMask,
                    max(forearmFrame, nearForearmFrame));
                cyanMask = max(
                    cyanMask,
                    max(forearmCore, nearForearmCore));

                frameMask = max(
                    frameMask,
                    SlopedRectMask(
                        horizontal,
                        vertical,
                        0.170,
                        0.765,
                        0.034,
                        0.092,
                        0.10) * front);
                const float thighLevels[3] =
                {
                    0.720,
                    0.765,
                    0.810
                };
                [unroll]
                for (int index = 0; index < 3; index++)
                {
                    cyanMask = max(
                        cyanMask,
                        SlopedRectMask(
                            horizontal,
                            vertical,
                            0.170,
                            thighLevels[index],
                            0.019,
                            0.006,
                            0.10) * front);
                }

                frameMask = max(
                    frameMask,
                    RectMask(
                        horizontal,
                        vertical,
                        0.205,
                        0.410,
                        0.026,
                        0.105) * front);
                cyanMask = max(
                    cyanMask,
                    RectMask(
                        horizontal,
                        vertical,
                        0.205,
                        0.410,
                        0.011,
                        0.078) * front);

                half3 baseColor = silver;
                half metallic = 0.38;
                baseColor = lerp(baseColor, dark, darkMask);
                metallic = lerp(metallic, 0.48, darkMask);
                baseColor = lerp(baseColor, bronze, bronzeMask);
                metallic = lerp(metallic, 0.58, bronzeMask);
                baseColor = lerp(baseColor, olive, oliveMask);
                metallic = lerp(metallic, 0.0, oliveMask);
                frameMask *= 1.0 - oliveMask;
                cyanMask *= 1.0 - oliveMask;
                baseColor = lerp(
                    baseColor,
                    half3(0.008, 0.014, 0.020),
                    frameMask);
                baseColor = lerp(
                    baseColor,
                    half3(0.005, 0.55, 0.80),
                    cyanMask);

                half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS =
                    GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord =
                    TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = VertexLighting(
                    input.positionWS,
                    normalWS);
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV =
                    GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1.0, 1.0, 1.0, 1.0);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = baseColor;
                surfaceData.metallic = metallic;
                surfaceData.specular = half3(0.0, 0.0, 0.0);
                surfaceData.smoothness = saturate(1.0 - roughness);
                surfaceData.normalTS = half3(0.0, 0.0, 1.0);
                surfaceData.emission =
                    half3(0.005, 0.55, 0.80) *
                    cyanMask *
                    0.65;
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = 1.0;
                surfaceData.clearCoatMask = 0.0;
                surfaceData.clearCoatSmoothness = 0.0;

                half4 color = UniversalFragmentPBR(
                    inputData,
                    surfaceData);
                color.rgb = MixFog(color.rgb, input.fogFactor);
                return color;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
