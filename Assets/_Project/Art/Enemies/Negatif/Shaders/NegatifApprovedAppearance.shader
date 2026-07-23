Shader "Bellerophon/Negatif/ApprovedAppearance"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (1, 1, 1, 1)
        _RoughnessMap("Roughness", 2D) = "white" {}
        _HeightMap("Height", 2D) = "gray" {}
        _Metallic("Metallic", Range(0, 1)) = 0
        _BumpStrength("Height Strength", Range(0, 2)) = 0.42
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionStrength("Emission Strength", Range(0, 12)) = 0
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
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
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma instancing_options renderinglayer
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_RoughnessMap);
            SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_HeightMap);
            SAMPLER(sampler_HeightMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Metallic;
                half _BumpStrength;
                half4 _EmissionColor;
                half _EmissionStrength;
                half _Cutoff;
                float4 _HeightMap_TexelSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 tangentWS : TEXCOORD3;
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

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                half tangentSign = input.tangentOS.w * GetOddNegativeScale();
                output.tangentWS = half4(normalInputs.tangentWS, tangentSign);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                output.shadowCoord = GetShadowCoord(positionInputs);
                return output;
            }

            half3 HeightNormal(float2 uv)
            {
                float2 offset = _HeightMap_TexelSize.xy;
                half left = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, uv - float2(offset.x, 0)).r;
                half right = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, uv + float2(offset.x, 0)).r;
                half down = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, uv - float2(0, offset.y)).r;
                half up = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, uv + float2(0, offset.y)).r;
                return normalize(half3((left - right) * _BumpStrength, (down - up) * _BumpStrength, 1));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half roughness = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, input.uv).r;
                half3 normalTS = HeightNormal(input.uv);
                half3 normalWS = normalize(input.normalWS);
                half3 tangentWS = normalize(input.tangentWS.xyz);
                half3 bitangentWS = normalize(cross(normalWS, tangentWS) * input.tangentWS.w);
                half3x3 tangentToWorld = half3x3(tangentWS, bitangentWS, normalWS);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, tangentToWorld));
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = input.shadowCoord;
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = VertexLighting(input.positionWS, inputData.normalWS);
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.smoothness = saturate(1 - roughness);
                surfaceData.normalTS = normalTS;
                surfaceData.occlusion = 1;
                surfaceData.emission = _EmissionColor.rgb * _EmissionStrength;
                surfaceData.alpha = 1;
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 0;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
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
