Shader "Bellerophon/Pahur/StopAppearance"
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
        _FaceOverlay("Face Eye Overlay", 2D) = "black" {}
        _FaceEmission("Face Eye Emission", 2D) = "black" {}
        _HoodDecal("Hood Flame Decal", 2D) = "black" {}
        _EmissionMap("Emission", 2D) = "black" {}
        _EmissionStrength("Emission Strength", Float) = 0
        _MachineScale("Machine Band Scale", Float) = 0
        _MachineThreshold("Machine Band Threshold", Float) = 0
        _MachineStrength("Machine Band Strength", Float) = 0
        _MachineColor("Machine Band Color", Color) = (0, 0, 0, 1)
        _ApprovedAmbientStrength("Approved Studio Ambient", Range(0, 3)) = 0.88
        _ApprovedKeyStrength("Approved Studio Key", Range(0, 5)) = 3.00
        _ApprovedFillStrength("Approved Studio Fill", Range(0, 5)) = 1.35
        _ApprovedRimStrength("Approved Studio Rim", Range(0, 5)) = 2.10
        _GeneratedBoundsMin("Generated Bounds Min", Vector) = (-64.637962, 0, -54.07045, 0)
        _GeneratedBoundsInvSize("Generated Bounds Inverse Size", Vector) = (0.007735, 0.005556, 0.009247, 0)
        [HideInInspector] _PreviewUnlit("Preview Unlit", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        _ShutdownBlend("Eye Shutdown Blend", Range(0, 1)) = 0
        _ShutdownColor("Eye Shutdown Color", Color) = (0.011764706, 0.011764706, 0.011764706, 1)
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
            TEXTURE2D(_FaceOverlay);
            SAMPLER(sampler_FaceOverlay);
            TEXTURE2D(_FaceEmission);
            SAMPLER(sampler_FaceEmission);
            TEXTURE2D(_HoodDecal);
            SAMPLER(sampler_HoodDecal);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _NormalStrength;
                half _TextureScale;
                half _FeatureMode;
                half _EmissionStrength;
                half _MachineScale;
                half _MachineThreshold;
                half _MachineStrength;
                half4 _MachineColor;
                half _ApprovedAmbientStrength;
                half _ApprovedKeyStrength;
                half _ApprovedFillStrength;
                half _ApprovedRimStrength;
                float4 _GeneratedBoundsMin;
                float4 _GeneratedBoundsInvSize;
                half _PreviewUnlit;
                half _Cutoff;
                half _ShutdownBlend;
                half4 _ShutdownColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float3 approvedSamplePosition : TEXCOORD3;
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
                float3 approvedSamplePosition : TEXCOORD7;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = positionInputs.positionCS;
                output.positionOS = input.positionOS.xyz;
                output.approvedSamplePosition = input.approvedSamplePosition;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                half tangentSign = input.tangentOS.w * GetOddNegativeScale();
                output.tangentWS = half4(normalInputs.tangentWS, tangentSign);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                output.shadowCoord = GetShadowCoord(positionInputs);
                return output;
            }

            half InUnitSquare(float2 uv)
            {
                return step(0.0, uv.x) * step(uv.x, 1.0) *
                       step(0.0, uv.y) * step(uv.y, 1.0);
            }

            half3 ShutdownColorize(half3 source)
            {
                half intensity = max(source.r, max(source.g, source.b));
                return lerp(
                    source,
                    _ShutdownColor.rgb * intensity,
                    saturate(_ShutdownBlend));
            }

            void ApplyEye(
                float3 samplePosition,
                float3 origin,
                float3 baseU,
                float3 baseV,
                float width,
                float height,
                float rotationDegrees,
                inout half3 albedo,
                inout half3 emission)
            {
                float rotation = radians(rotationDegrees);
                float3 rotatedU = baseU * cos(rotation) + baseV * sin(rotation);
                float3 rotatedV = -baseU * sin(rotation) + baseV * cos(rotation);
                float3 delta = samplePosition - origin;
                float2 projectionUv = float2(
                    dot(delta, rotatedU) / width + 0.5,
                    dot(delta, rotatedV) / height + 0.5);
                half inside = InUnitSquare(projectionUv);
                half4 overlay = SAMPLE_TEXTURE2D(
                    _FaceOverlay,
                    sampler_FaceOverlay,
                    saturate(projectionUv));
                overlay.rgb = ShutdownColorize(overlay.rgb);
                half3 eyeEmission = SAMPLE_TEXTURE2D(
                    _FaceEmission,
                    sampler_FaceEmission,
                    saturate(projectionUv)).rgb;
                eyeEmission = ShutdownColorize(eyeEmission);
                half locationLimit = 1.0h - step(width * 0.72, length(delta));
                half factor = overlay.a * inside * locationLimit;
                albedo = lerp(albedo, overlay.rgb, factor);
                emission += eyeEmission * factor;
            }

            half3 ApplyApprovedFeatures(
                float3 positionOS,
                float2 baseUv,
                inout half3 emission)
            {
                float2 tiledUv = baseUv * _TextureScale;
                half3 albedo =
                    SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, tiledUv).rgb *
                    _BaseColor.rgb;
                float3 samplePosition = positionOS;

                if (_FeatureMode > 0.5h && _FeatureMode < 1.5h)
                {
                    ApplyEye(
                        samplePosition,
                        float3(14.129982, 162.225969, 0.949688),
                        float3(0.786136, 0.018001, 0.617791),
                        float3(-0.172078, 0.966427, 0.190809),
                        5.0,
                        4.5,
                        -16.0,
                        albedo,
                        emission);
                    ApplyEye(
                        samplePosition,
                        float3(21.768437, 162.193593, 1.748115),
                        float3(-0.960798, 0.027385, 0.275895),
                        float3(0.063822, 0.990231, 0.123971),
                        5.0,
                        4.5,
                        -14.0,
                        albedo,
                        emission);
                    emission *= 1.55h;
                }
                else if (_FeatureMode > 1.5h && _FeatureMode < 2.5h)
                {
                    float2 decalUv = float2(
                        (samplePosition.x - 7.0) / 25.0,
                        (samplePosition.y - 150.0) / 31.0);
                    half4 decal = SAMPLE_TEXTURE2D(
                        _HoodDecal,
                        sampler_HoodDecal,
                        saturate(decalUv));
                    half factor =
                        decal.a * InUnitSquare(decalUv) *
                        step(0.0, samplePosition.z);
                    albedo = lerp(albedo, decal.rgb, factor);
                }
                else if (_FeatureMode > 2.5h && _FeatureMode < 3.5h)
                {
                    float3 generated =
                        (samplePosition - _GeneratedBoundsMin.xyz) *
                        _GeneratedBoundsInvSize.xyz;
                    float distortion =
                        sin(dot(generated.xz, float2(31.17, 47.53))) * 0.20;
                    float wave =
                        0.5 + 0.5 *
                        sin((generated.y * _MachineScale + distortion) *
                            6.28318530718);
                    half band =
                        (1.0h - step(_MachineThreshold, wave)) *
                        _MachineStrength;
                    albedo = lerp(albedo, _MachineColor.rgb, band);
                }
                else if (_FeatureMode > 3.5h && _FeatureMode < 4.5h)
                {
                    albedo = ShutdownColorize(albedo);
                }

                half3 mappedEmission =
                    SAMPLE_TEXTURE2D(
                        _EmissionMap,
                        sampler_EmissionMap,
                        tiledUv).rgb * _EmissionStrength;
                if (_FeatureMode > 3.5h && _FeatureMode < 4.5h)
                {
                    mappedEmission = ShutdownColorize(mappedEmission);
                }
                emission += mappedEmission;
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
                right = normalize(
                    lerp(
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

                half hemisphere = saturate(normalWS.y * 0.5h + 0.5h);
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
                half fresnel = Pow4(
                    1.0h - saturate(dot(normalWS, view)));
                half3 indirect =
                    brdfData.diffuse * neutralWorld *
                        _ApprovedAmbientStrength *
                        lerp(0.76h, 1.0h, hemisphere) +
                    brdfData.specular * neutralWorld *
                        _ApprovedAmbientStrength *
                        lerp(0.62h, 1.0h, fresnel);

                half3 key = LightingPhysicallyBased(
                    brdfData,
                    half3(1.0h, 0.90h, 0.78h) *
                        _ApprovedKeyStrength,
                    keyDirection,
                    1.0,
                    normalWS,
                    view);
                half3 fill = LightingPhysicallyBased(
                    brdfData,
                    half3(0.58h, 0.76h, 1.0h) *
                        _ApprovedFillStrength,
                    fillDirection,
                    1.0,
                    normalWS,
                    view);
                half3 rim = LightingPhysicallyBased(
                    brdfData,
                    half3(0.55h, 0.72h, 1.0h) *
                        _ApprovedRimStrength,
                    rimDirection,
                    1.0,
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
                half3 albedo =
                    ApplyApprovedFeatures(
                        input.approvedSamplePosition,
                        input.uv,
                        emission);
                half roughness =
                    SAMPLE_TEXTURE2D(
                        _RoughnessMap,
                        sampler_RoughnessMap,
                        tiledUv).r;
                half metallic =
                    SAMPLE_TEXTURE2D(
                        _MetallicMap,
                        sampler_MetallicMap,
                        tiledUv).r;
                half3 normalTS =
                    UnpackNormalScale(
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
                half3 bitangentWS =
                    normalize(cross(normalWS, tangentWS) * input.tangentWS.w);
                half3x3 tangentToWorld =
                    half3x3(tangentWS, bitangentWS, normalWS);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS =
                    NormalizeNormalPerPixel(
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
                surfaceData.occlusion = 1;
                surfaceData.emission = emission;
                surfaceData.alpha = 1;
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 0;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                half3 approvedStudio =
                    EvaluateApprovedStudioResponse(
                        albedo,
                        metallic,
                        roughness,
                        inputData.normalWS,
                        inputData.viewDirectionWS) +
                    emission;
                color.rgb = max(color.rgb, approvedStudio);
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
