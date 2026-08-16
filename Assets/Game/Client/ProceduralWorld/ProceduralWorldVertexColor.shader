Shader "MyGameWorld/Procedural World/Vertex Color Lit"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _InstanceColor("Instance Color", Color) = (1, 1, 1, 1)
        _WindResponse("Wind Response", Range(0, 1)) = 0
        _WindHeightStart("Wind Height Start", Range(0, 1)) = 0
        _ReflectionStrength("Stylized Reflection", Range(0, 1)) = 0.15
        _SurfaceSmoothness("Surface Smoothness", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        // Source foliage is authored double-sided; procedural volumes keep that
        // readable silhouette while remaining far below the source mesh budget.
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _WindResponse;
                half _WindHeightStart;
                half _ReflectionStrength;
                half _SurfaceSmoothness;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(ProceduralPerInstance)
                UNITY_DEFINE_INSTANCED_PROP(float4, _InstanceColor)
            UNITY_INSTANCING_BUFFER_END(ProceduralPerInstance)

            float4 _WorldWindDirectionStrength;
            float4 _WorldWindParameters;
            float4 _WorldTimeTint;
            float4 _WorldTimeRimColor;
            float4 _ProceduralShaderLayers;
            float4 _ProceduralLightingParameters;
            float4 _ProceduralReflectionColor;
            float4 _ProceduralShadowColor;
            float4 _ProceduralShadowParameters;
            float _WorldAtmosphericVisibility;
            float _WorldAtmosphereDisabled;

            half SmoothToonBand(half value, half bandCount, half softness)
            {
                half intervals = max(1.0h, bandCount - 1.0h);
                half scaled = saturate(value) * intervals;
                half lowerBand = floor(scaled);
                half fractionInBand = frac(scaled);
                half halfWidth = saturate(softness) * 0.5h;
                half blend = smoothstep(0.5h - halfWidth, 0.5h + halfWidth, fractionInBand);
                return saturate((lowerBand + blend) / intervals);
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 color : COLOR;
                half fogFactor : TEXCOORD2;
                float cameraDistance : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 baseWS = TransformObjectToWorld(input.positionOS.xyz);
                float heightWeight = saturate(input.positionOS.y - _WindHeightStart);
                float spatialA = sin(dot(baseWS.xz, float2(0.071, 0.113)) + _WorldWindParameters.w * (1.7 + _WorldWindParameters.x * 0.08));
                float spatialB = sin(dot(baseWS.xz, float2(-0.137, 0.053)) - _WorldWindParameters.w * 2.31);
                float organic = spatialA * 0.65 + spatialB * 0.35;
                float response = _WindResponse * heightWeight * _WorldWindDirectionStrength.w;
                float3 displacedWS = baseWS + _WorldWindDirectionStrength.xyz * response * (0.12 + _WorldWindParameters.x * 0.055) * organic;
                displacedWS.y += abs(organic) * response * _WorldWindParameters.y * 0.08;
                VertexPositionInputs positionInputs = (VertexPositionInputs)0;
                positionInputs.positionWS = displacedWS;
                positionInputs.positionCS = TransformWorldToHClip(displacedWS);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);
                output.color = input.color * _BaseColor * UNITY_ACCESS_INSTANCED_PROP(ProceduralPerInstance, _InstanceColor);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                output.cameraDistance = distance(_WorldSpaceCameraPos.xyz, displacedWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half wrappedDiffuse = saturate(ndotl * 0.82h + 0.18h);
                half diffuseBands = max(2.0h, _ProceduralLightingParameters.x);
                half bandSoftness = max(0.02h, _ProceduralLightingParameters.w);
                half toonDiffuse = SmoothToonBand(wrappedDiffuse, diffuseBands, bandSoftness);
                half diffuse = lerp(wrappedDiffuse, saturate(toonDiffuse), _ProceduralShaderLayers.x);
                half attenuation = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                half shadowBands = max(1.0h, _ProceduralLightingParameters.y);
                half toonShadow = SmoothToonBand(saturate(attenuation), shadowBands + 1.0h, bandSoftness);
                half physicalShadow = lerp(attenuation, toonShadow, _ProceduralShaderLayers.x);
                half shadow = lerp(1.0h, lerp(0.30h, 1.0h, physicalShadow), _ProceduralShadowParameters.x * _ProceduralShaderLayers.w);
                half3 ambient = max(SampleSH(normalWS) * 0.82h, 0.12h);
                half3 lighting = ambient + mainLight.color * (0.16h + diffuse * 0.78h) * shadow;
                half facetTone = lerp(0.88h, 1.12h, saturate(normalWS.y * 0.5h + 0.5h));
                half3 color = input.color.rgb * min(lighting, 1.35h) * facetTone * _WorldTimeTint.rgb * _WorldTimeTint.a;
                half shadowOcclusion = 1.0h - shadow;
                half3 bouncedShadowTint = lerp(1.0h.xxx, _ProceduralShadowColor.rgb, 0.55h);
                color *= lerp(1.0h.xxx, bouncedShadowTint, shadowOcclusion);
                half3 viewDirection = SafeNormalize(_WorldSpaceCameraPos.xyz - input.positionWS);
                half rim = pow(saturate(1.0h - abs(dot(normalWS, viewDirection))), 2.6h);
                color += _WorldTimeRimColor.rgb * rim * _WorldTimeRimColor.a * _ProceduralShaderLayers.z;
                half3 reflectedLight = reflect(-mainLight.direction, normalWS);
                half specularPower = lerp(10.0h, 72.0h, _SurfaceSmoothness);
                half specular = pow(saturate(dot(reflectedLight, viewDirection)), specularPower);
                half toonSpecular = smoothstep(0.42h, 0.72h, specular);
                half fresnel = pow(saturate(1.0h - dot(normalWS, viewDirection)), 3.0h);
                half reflectionResponse = _ReflectionStrength * _ProceduralShaderLayers.y;
                half litReflection = saturate(ndotl * 1.4h) * shadow;
                color += _ProceduralReflectionColor.rgb * (toonSpecular * 0.72h + fresnel * 0.18h) * reflectionResponse * litReflection;
                color = MixFog(color, input.fogFactor);
                // Geometry remains rendered independently of weather visibility. This is a presentation layer,
                // intentionally disabled by the clear-atmosphere debug mode.
                half atmosphericAmount = (half)(1.0 - exp(-input.cameraDistance / max(1.0, _WorldAtmosphericVisibility)));
                atmosphericAmount *= (half)(1.0 - saturate(_WorldAtmosphereDisabled));
                color = lerp(color, unity_FogColor.rgb, atmosphericAmount * 0.82h);
                return half4(color, input.color.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            float3 _LightPosition;
            struct ShadowAttributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct ShadowVaryings { float4 positionCS : SV_POSITION; };
            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                ShadowVaryings output; float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE * output.positionCS.w);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE * output.positionCS.w);
                #endif
                return output;
            }
            half4 ShadowFrag(ShadowVaryings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
