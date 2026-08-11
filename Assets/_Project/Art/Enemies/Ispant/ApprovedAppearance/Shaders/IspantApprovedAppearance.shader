Shader "Bellerophon/Ispant/ApprovedAppearance"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Color", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (1, 1, 1, 1)
        _RoughnessMap("Roughness", 2D) = "black" {}
        _MetallicMap("Metallic", 2D) = "black" {}
        [Normal] _NormalMap("Normal", 2D) = "bump" {}
        _NormalStrength("Normal Strength", Range(0, 2)) = 0.5
        _UseMaps("Use Approved Maps", Float) = 1
        _UseUv1("Use Mechanical UV", Float) = 0
        _RoughnessBias("Roughness Bias", Range(0, 1)) = 0
        _MetallicBias("Metallic Bias", Range(0, 1)) = 0
        _CoatWeight("Paint Coat Weight", Range(0, 1)) = 0
        _CoatRoughness("Paint Coat Roughness", Range(0, 1)) = 0.34
        _FeatureMode("Approved Feature Mode", Float) = 0
        _ApprovedYFlip("Approved Blender Y Flip", Float) = 0
        _EyeDesaturation("Eye Desaturation", Range(0, 1)) = 0
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
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
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

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _NormalStrength;
                half _UseMaps;
                half _UseUv1;
                half _RoughnessBias;
                half _MetallicBias;
                half _CoatWeight;
                half _CoatRoughness;
                half _FeatureMode;
                half _ApprovedYFlip;
                half _EyeDesaturation;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 approvedPositionXY : TEXCOORD2;
                float2 approvedPositionZ : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                float2 uv0 : TEXCOORD3;
                float2 uv1 : TEXCOORD4;
                float2 approvedPositionXY : TEXCOORD5;
                float approvedPositionZ : TEXCOORD6;
                half fogFactor : TEXCOORD7;
                float4 shadowCoord : TEXCOORD8;
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
                output.positionWS = positions.positionWS;
                output.normalWS = normals.normalWS;
                output.tangentWS = half4(
                    normals.tangentWS,
                    input.tangentOS.w * GetOddNegativeScale());
                output.uv0 = input.uv0;
                output.uv1 = input.uv1;
                output.approvedPositionXY = input.approvedPositionXY;
                output.approvedPositionZ = input.approvedPositionZ.x;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                output.shadowCoord = GetShadowCoord(positions);
                return output;
            }

            half ApprovedHelmetEyeMask(Varyings input)
            {
                if (_FeatureMode < 0.5h || _FeatureMode > 1.5h)
                    return 0.0h;

                float blenderX = input.approvedPositionXY.x;
                float blenderY = lerp(
                    input.approvedPositionXY.y,
                    1.0 - input.approvedPositionXY.y,
                    step(0.5h, _ApprovedYFlip));
                float blenderZ = input.approvedPositionZ;
                float absoluteX = abs(blenderX);
                return step(0.024, absoluteX) *
                       step(absoluteX, 0.078) *
                       step(1.795, blenderZ) *
                       step(blenderZ, 1.818) *
                       (1.0h - step(-0.018, blenderY));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = lerp(input.uv0, input.uv1, step(0.5h, _UseUv1));
                // Feature mode 2 reproduces the approved face texture through Blender's third UV channel.
                uv = lerp(uv, input.approvedPositionXY, step(1.5h, _FeatureMode));
                half useMaps = step(0.5h, _UseMaps);
                half3 mappedAlbedo = SAMPLE_TEXTURE2D(
                    _BaseMap,
                    sampler_BaseMap,
                    uv).rgb;
                half3 albedo = lerp(half3(1, 1, 1), mappedAlbedo, useMaps) *
                               _BaseColor.rgb;
                half roughness = saturate(
                    _RoughnessBias +
                    SAMPLE_TEXTURE2D(
                        _RoughnessMap,
                        sampler_RoughnessMap,
                        uv).r * useMaps);
                half metallic = saturate(
                    _MetallicBias +
                    SAMPLE_TEXTURE2D(
                        _MetallicMap,
                        sampler_MetallicMap,
                        uv).r * useMaps);
                half3 normalTS = lerp(
                    half3(0, 0, 1),
                    UnpackNormalScale(
                        SAMPLE_TEXTURE2D(
                            _NormalMap,
                            sampler_NormalMap,
                            uv),
                        _NormalStrength),
                    useMaps);

                half eyeMask = ApprovedHelmetEyeMask(input);
                half3 eyeColor = half3(0.015h, 0.65h, 1.0h);
                half eyeLuminance = dot(eyeColor, half3(0.2126h, 0.7152h, 0.0722h));
                eyeColor = lerp(
                    eyeColor,
                    half3(eyeLuminance, eyeLuminance, eyeLuminance),
                    saturate(_EyeDesaturation));
                albedo = lerp(albedo, eyeColor * 0.42h, eyeMask);
                // Feature mode 3 is reserved for the approved explicit cyan eye mesh.
                half eyeSurface = step(2.5h, _FeatureMode);
                half albedoLuminance = dot(albedo, half3(0.2126h, 0.7152h, 0.0722h));
                albedo = lerp(
                    albedo,
                    half3(albedoLuminance, albedoLuminance, albedoLuminance),
                    eyeSurface * saturate(_EyeDesaturation));
                half3 emission = eyeColor * eyeMask * 5.0h +
                                 half3(0.015h, 0.48h, 0.70h) * 2.4h * eyeSurface;
                half emissionLuminance = dot(
                    emission,
                    half3(0.2126h, 0.7152h, 0.0722h));
                emission = lerp(
                    emission,
                    half3(emissionLuminance, emissionLuminance, emissionLuminance),
                    eyeSurface * saturate(_EyeDesaturation));

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
                surfaceData.clearCoatMask = _CoatWeight;
                surfaceData.clearCoatSmoothness = saturate(1.0h - _CoatRoughness);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
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
