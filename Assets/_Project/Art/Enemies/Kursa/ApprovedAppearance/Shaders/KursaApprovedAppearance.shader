Shader "Bellerophon/Kursa/ApprovedAppearance"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (1, 1, 1, 1)
        _RoughnessMap("Roughness", 2D) = "white" {}
        _MetallicMap("Metallic", 2D) = "black" {}
        [Normal] _NormalMap("Normal", 2D) = "bump" {}
        _NormalStrength("Normal Strength", Range(0, 2)) = 0.1
        _TextureScale("Texture Scale", Float) = 1
        _FeatureMode("Approved Feature Mode", Float) = 0
        _TorsoGlyph("Torso Reference Glyph", 2D) = "black" {}
        _HoodDecal("Hood Reference Decal", 2D) = "black" {}
        _ScarfDecal("Scarf Reference Decal", 2D) = "black" {}
        _EyeLeft("Left Eye Reference Overlay", 2D) = "black" {}
        _EyeRight("Right Eye Reference Overlay", 2D) = "black" {}
        _ApprovedAmbientStrength("Approved Studio Ambient", Range(0, 3)) = 0.88
        _ApprovedKeyStrength("Approved Studio Key", Range(0, 5)) = 3.00
        _ApprovedFillStrength("Approved Studio Fill", Range(0, 5)) = 1.35
        _ApprovedRimStrength("Approved Studio Rim", Range(0, 5)) = 2.10
        [HideInInspector] _PreviewUnlit("Preview Unlit", Float) = 0
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
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_RoughnessMap);
            SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_MetallicMap);
            SAMPLER(sampler_MetallicMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_TorsoGlyph);
            SAMPLER(sampler_TorsoGlyph);
            TEXTURE2D(_HoodDecal);
            SAMPLER(sampler_HoodDecal);
            TEXTURE2D(_ScarfDecal);
            SAMPLER(sampler_ScarfDecal);
            TEXTURE2D(_EyeLeft);
            SAMPLER(sampler_EyeLeft);
            TEXTURE2D(_EyeRight);
            SAMPLER(sampler_EyeRight);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _NormalStrength;
                half _TextureScale;
                half _FeatureMode;
                half _ApprovedAmbientStrength;
                half _ApprovedKeyStrength;
                half _ApprovedFillStrength;
                half _ApprovedRimStrength;
                half _PreviewUnlit;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float2 eyeLeftProjection : TEXCOORD1;
                float2 eyeRightProjection : TEXCOORD2;
                float2 eyeSignedDepth : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half3 normalWS : TEXCOORD3;
                half4 tangentWS : TEXCOORD4;
                half fogFactor : TEXCOORD5;
                float4 shadowCoord : TEXCOORD6;
                float4 eyeProjectionUv : TEXCOORD7;
                float2 eyeSignedDepth : TEXCOORD8;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positions =
                    GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normals =
                    GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = positions.positionCS;
                output.positionOS = input.positionOS.xyz;
                output.positionWS = positions.positionWS;
                output.normalWS = normals.normalWS;
                output.tangentWS = half4(
                    normals.tangentWS,
                    input.tangentOS.w * GetOddNegativeScale());
                output.uv = input.uv;
                output.eyeProjectionUv = float4(
                    input.eyeLeftProjection,
                    input.eyeRightProjection);
                output.eyeSignedDepth = input.eyeSignedDepth;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                output.shadowCoord = GetShadowCoord(positions);
                return output;
            }

            half InUnitSquare(float2 uv)
            {
                return step(0.0, uv.x) * step(uv.x, 1.0) *
                       step(0.0, uv.y) * step(uv.y, 1.0);
            }

            half3 ApprovedBlueTint(half3 color)
            {
                return lerp(
                    color,
                    color * half3(0.34h, 0.67h, 1.0h),
                    0.72h);
            }

            void ApplyPlanarOverlay(
                float3 positionOS,
                float4 bounds,
                half opacity,
                half textureIndex,
                inout half3 albedo,
                inout half3 emission,
                half emissionStrength)
            {
                float2 overlayUv = float2(
                    (positionOS.x - bounds.x) / (bounds.y - bounds.x),
                    (positionOS.y - bounds.z) / (bounds.w - bounds.z));
                half4 overlay;
                if (textureIndex < 0.5h)
                {
                    overlay = SAMPLE_TEXTURE2D(
                        _TorsoGlyph,
                        sampler_TorsoGlyph,
                        saturate(overlayUv));
                }
                else if (textureIndex < 1.5h)
                {
                    overlay = SAMPLE_TEXTURE2D(
                        _HoodDecal,
                        sampler_HoodDecal,
                        saturate(overlayUv));
                }
                else
                {
                    overlay = SAMPLE_TEXTURE2D(
                        _ScarfDecal,
                        sampler_ScarfDecal,
                        saturate(overlayUv));
                }

                half factor = overlay.a * opacity *
                    InUnitSquare(overlayUv) * step(0.00001, positionOS.z);
                half3 tint = ApprovedBlueTint(overlay.rgb);
                albedo = lerp(albedo, tint, factor);
                emission += tint * factor * emissionStrength;
            }

            void ApplyEye(
                float2 overlayUv,
                float signedDepth,
                half isRight,
                inout half3 albedo,
                inout half3 emission)
            {
                half4 overlay = isRight < 0.5h
                    ? SAMPLE_TEXTURE2D(
                        _EyeLeft,
                        sampler_EyeLeft,
                        saturate(overlayUv))
                    : SAMPLE_TEXTURE2D(
                        _EyeRight,
                        sampler_EyeRight,
                        saturate(overlayUv));
                // Signed depth is normalized by the exact approved depth 2.05
                // when the frame-1 projection channels are exported from Blender.
                half depthMask = 1.0h - step(1.0, abs(signedDepth));
                half factor = overlay.a * InUnitSquare(overlayUv) * depthMask;
                half3 tint = ApprovedBlueTint(overlay.rgb);
                albedo = lerp(albedo, tint, factor);
                emission += tint * factor * 2.2h;
            }

            half3 ApplyApprovedFeatures(
                float3 positionOS,
                float2 uv,
                float4 eyeProjectionUv,
                float2 eyeSignedDepth,
                inout half3 emission)
            {
                half3 albedo = SAMPLE_TEXTURE2D(
                    _BaseMap,
                    sampler_BaseMap,
                    uv * _TextureScale).rgb * _BaseColor.rgb;

                // The approved Blender sample is authored in 100 units per Unity metre
                // (the source armature import scale is 0.01). Projection constants stay
                // in their approved sample coordinates; only the Unity mesh position is converted.
                const float approvedSampleUnitsPerUnityUnit = 100.0;
                float3 approvedSamplePosition =
                    positionOS * approvedSampleUnitsPerUnityUnit;

                if (_FeatureMode > 0.5h && _FeatureMode < 1.5h)
                {
                    ApplyPlanarOverlay(
                        approvedSamplePosition,
                        float4(-20.5, 6.0, 116.0, 142.5),
                        0.27h,
                        0.0h,
                        albedo,
                        emission,
                        0.0h);
                }
                else if (_FeatureMode > 1.5h && _FeatureMode < 2.5h)
                {
                    ApplyPlanarOverlay(
                        approvedSamplePosition,
                        float4(-15.5, 4.0, 156.0, 170.2),
                        0.46h,
                        1.0h,
                        albedo,
                        emission,
                        0.20h);
                    ApplyPlanarOverlay(
                        approvedSamplePosition,
                        float4(-22.0, 8.0, 128.0, 150.0),
                        0.38h,
                        2.0h,
                        albedo,
                        emission,
                        0.20h);
                }
                else if (_FeatureMode > 2.5h && _FeatureMode < 3.5h)
                {
                    ApplyEye(
                        eyeProjectionUv.xy,
                        eyeSignedDepth.x,
                        0.0h,
                        albedo,
                        emission);
                    ApplyEye(
                        eyeProjectionUv.zw,
                        eyeSignedDepth.y,
                        1.0h,
                        albedo,
                        emission);
                }

                return albedo;
            }

            half3 EvaluateApprovedStudioResponse(
                half3 albedo,
                half metallic,
                half roughness,
                half3 normalWS,
                half3 viewDirectionWS)
            {
                half3 view = normalize(viewDirectionWS);
                half3 up = half3(0.0h, 1.0h, 0.0h);
                half3 right = cross(up, view);
                right = normalize(lerp(
                    half3(1.0h, 0.0h, 0.0h),
                    right,
                    step(0.0001h, dot(right, right))));
                half3 cameraUp = normalize(cross(view, right));

                half3 keyDirection = normalize(
                    view - right * 0.58h + cameraUp * 0.62h);
                half3 fillDirection = normalize(
                    view + right * 0.72h + cameraUp * 0.24h);
                half3 rimDirection = normalize(
                    -view + right * 0.12h + cameraUp * 0.58h);

                half alpha = 1.0h;
                BRDFData brdfData;
                InitializeBRDFData(
                    albedo,
                    metallic,
                    half3(0.0h, 0.0h, 0.0h),
                    saturate(1.0h - roughness),
                    alpha,
                    brdfData);

                half3 neutralWorld = half3(0.86h, 0.89h, 0.90h);
                half hemisphere = saturate(normalWS.y * 0.5h + 0.5h);
                half fresnel = Pow4(1.0h - saturate(dot(normalWS, view)));
                half3 indirect =
                    brdfData.diffuse * neutralWorld *
                        _ApprovedAmbientStrength *
                        lerp(0.76h, 1.0h, hemisphere) +
                    brdfData.specular * neutralWorld *
                        _ApprovedAmbientStrength *
                        lerp(0.62h, 1.0h, fresnel);

                half3 key = LightingPhysicallyBased(
                    brdfData,
                    half3(1.0h, 0.90h, 0.78h) * _ApprovedKeyStrength,
                    keyDirection,
                    1.0h,
                    normalWS,
                    view);
                half3 fill = LightingPhysicallyBased(
                    brdfData,
                    half3(0.58h, 0.76h, 1.0h) * _ApprovedFillStrength,
                    fillDirection,
                    1.0h,
                    normalWS,
                    view);
                half3 rim = LightingPhysicallyBased(
                    brdfData,
                    half3(0.55h, 0.72h, 1.0h) * _ApprovedRimStrength,
                    rimDirection,
                    1.0h,
                    normalWS,
                    view);
                return indirect + key + fill + rim;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 tiledUv = input.uv * _TextureScale;
                half3 emission = half3(0, 0, 0);
                half3 albedo = ApplyApprovedFeatures(
                    input.positionOS,
                    input.uv,
                    input.eyeProjectionUv,
                    input.eyeSignedDepth,
                    emission);
                half roughness = SAMPLE_TEXTURE2D(
                    _RoughnessMap,
                    sampler_RoughnessMap,
                    tiledUv).r;
                half metallic = SAMPLE_TEXTURE2D(
                    _MetallicMap,
                    sampler_MetallicMap,
                    tiledUv).r;
                half3 normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(
                        _NormalMap,
                        sampler_NormalMap,
                        tiledUv),
                    _NormalStrength);

                if (_PreviewUnlit > 0.5h)
                {
                    return half4(albedo + emission, 1.0h);
                }

                half3 normalWS = normalize(input.normalWS);
                half3 tangentWS = normalize(input.tangentWS.xyz);
                half3 bitangentWS = normalize(
                    cross(normalWS, tangentWS) * input.tangentWS.w);
                half3x3 tangentToWorld =
                    half3x3(tangentWS, bitangentWS, normalWS);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = NormalizeNormalPerPixel(
                    TransformTangentToWorld(normalTS, tangentToWorld));
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
                surfaceData.metallic = metallic;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.smoothness = saturate(1.0h - roughness);
                surfaceData.normalTS = normalTS;
                surfaceData.occlusion = 1.0h;
                surfaceData.emission = emission;
                surfaceData.alpha = 1.0h;
                surfaceData.clearCoatMask = 0.0h;
                surfaceData.clearCoatSmoothness = 0.0h;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                half3 approvedStudio = EvaluateApprovedStudioResponse(
                    albedo,
                    metallic,
                    roughness,
                    inputData.normalWS,
                    inputData.viewDirectionWS) + emission;
                color.rgb = max(color.rgb, approvedStudio);
                color.rgb = MixFog(color.rgb, input.fogFactor);
                color.a = 1.0h;
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
